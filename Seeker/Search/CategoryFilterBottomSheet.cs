using Android.Content;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Common;
using Google.Android.Material.BottomSheet;
using Seeker.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    /// <summary>
    /// Gmail-style bottom sheet listing the selectable options for one chip category.
    /// Filetype is hierarchical. Keyword and FileCount are flat.
    /// </summary>
    public class CategoryFilterBottomSheet : BottomSheetDialogFragment
    {
        public const string TAG = "CategoryFilterBottomSheet";
        private const string ArgCategory = "category";

        private ChipType category;

        public static CategoryFilterBottomSheet NewInstance(ChipType category)
        {
            var sheet = new CategoryFilterBottomSheet();
            var args = new Bundle();
            args.PutInt(ArgCategory, (int)category);
            sheet.Arguments = args;
            return sheet;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            category = (ChipType)(Arguments?.GetInt(ArgCategory, 0) ?? 0);
            var root = inflater.Inflate(Resource.Layout.category_filter_bottom_sheet, container, false);

            var title = root.FindViewById<TextView>(Resource.Id.categoryFilterSheetTitle);
            title.Text = GroupedChipsItemRecyclerAdapter.CategoryDisplayName(root.Context, category);
            if (category != ChipType.FileType)
            {
                title.SetTypeface(title.Typeface, Android.Graphics.TypefaceStyle.Normal);
            }

            var close = root.FindViewById<ImageButton>(Resource.Id.categoryFilterSheetClose);
            close.Click += (s, e) => Dismiss();

            var rv = root.FindViewById<RecyclerView>(Resource.Id.categoryFilterSheetRecycler);
            rv.SetLayoutManager(new LinearLayoutManager(root.Context));

            var searchTab = SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab];
            var categoryItems = (searchTab.ChipDataItems ?? new List<ChipDataItem>())
                .Where(i => i.ChipType == category)
                .ToList();

            // For FileType, lazy-promote the orphan "other" catch-all chip (created at
            // ChipsAdapter.cs:341 with a Children list of per-variant display strings)
            // into real per-variant ChipDataItems so the sheet can render them as an
            // indented group under a synthetic "Other" parent. This mutates
            // searchTab.ChipDataItems permanently until the next search regenerates
            // the chip list — a deliberate tradeoff so the filter pipeline keeps
            // reading from one source of truth.
            var orphanChildren = new HashSet<ChipDataItem>();
            if (category == ChipType.FileType && searchTab.ChipDataItems != null)
            {
                foreach (var promoted in PromoteOrphanOtherChips(searchTab.ChipDataItems, categoryItems))
                {
                    orphanChildren.Add(promoted);
                }
            }

            var counts = ComputeRowCounts(searchTab, categoryItems);
            bool anyFilterActive = SearchFragment.AreFilterControlsActive()
                || SearchFragment.AreChipsFiltering()
                || SearchTabHelper.TextFilter.IsFiltered;

            var rows = category == ChipType.FileType
                ? BuildFileTypeRows(categoryItems, orphanChildren, counts)
                : BuildFlatRows(categoryItems);

            rv.SetAdapter(new RowAdapter(rows, counts, anyFilterActive, SearchTabHelper.SearchResponses?.Count ?? 0, OnRowTapped));

            return root;
        }

        /// <summary>
        /// For each option in the category, temporarily rewrites the IsChecked state so that only
        /// that option is selected within this category (other categories untouched) and runs the
        /// full filter pipeline against SearchResponses to count hits.
        /// </summary>
        private static Dictionary<ChipDataItem, int> ComputeRowCounts(SearchTab searchTab, List<ChipDataItem> categoryItems)
        {
            var counts = new Dictionary<ChipDataItem, int>();
            var responses = searchTab.SearchResponses;
            if (responses == null || responses.Count == 0)
            {
                foreach (var item in categoryItems)
                {
                    counts[item] = 0;
                }
                return counts;
            }

            bool hideLocked = PreferencesState.HideLockedResultsInSearch;
            var mergedFlags = SearchFragment.MergeFilterFlags(searchTab.TextFilter.FilterSpecialFlags);
            var wordsToAvoid = searchTab.TextFilter.WordsToAvoid;
            var wordsToInclude = searchTab.TextFilter.WordsToInclude;

            var originalChecks = categoryItems.Select(i => i.IsChecked).ToList();
            try
            {
                foreach (var option in categoryItems)
                {
                    foreach (var i in categoryItems)
                    {
                        i.IsChecked = false;
                    }
                    option.IsChecked = true;
                    var hypotheticalFilter = SearchFilter.ParseChips(searchTab.ChipDataItems);
                    int x = responses.Count(s => SearchFilter.MatchesAllCriteria(
                        s, hypotheticalFilter, mergedFlags, wordsToAvoid, wordsToInclude, hideLocked));
                    counts[option] = x;
                }
            }
            finally
            {
                for (int i = 0; i < categoryItems.Count; i++)
                {
                    categoryItems[i].IsChecked = originalChecks[i];
                }
            }
            return counts;
        }

        private enum RowKind { Leaf, Parent, Child }

        private class SheetRow
        {
            public RowKind Kind;
            public ChipDataItem Chip;       // null for the synthetic "Other" parent
            public ChipDataItem ParentChip; // null for synthetic-parent's children and for leaves
            public string DisplayBold;
            public string DisplayLight;
            public string DisplayNormal;
            public bool ShowTopSeparator;
            public bool IndentAsChild;
            public bool IsOrphanChild;      // true for children under the synthetic "Other" parent
            public bool IsSyntheticOtherParent; // true for the synthetic "Other" parent row
            public int SyntheticCount;      // aggregate count for the synthetic parent (sum of children)
        }

        /// <summary>
        /// Finds any orphan "other" catch-all chip (has a Children list, no " (" attrs,
        /// not a " - all" parent) in the FileType list and replaces it with one
        /// ChipDataItem per child string. Returns the new chips so the sheet can group
        /// them under a synthetic parent row. Mutates both the master ChipDataItems list
        /// and the local categoryItems list in place.
        /// </summary>
        private static List<ChipDataItem> PromoteOrphanOtherChips(List<ChipDataItem> allItems, List<ChipDataItem> categoryItems)
        {
            var promoted = new List<ChipDataItem>();
            for (int i = categoryItems.Count - 1; i >= 0; i--)
            {
                var c = categoryItems[i];
                if (c.ChipType != ChipType.FileType)
                {
                    continue;
                }
                if (!c.HasTag())
                {
                    continue;
                }
                if (c.BaseDisplayText.Contains("(") || c.IsAllCase)
                {
                    continue;
                }

                categoryItems.RemoveAt(i);
                allItems.Remove(c);

                int insertAt = i;
                foreach (var childName in c.Children)
                {
                    var newChip = new ChipDataItem(ChipType.FileType, false, childName);
                    categoryItems.Insert(insertAt, newChip);
                    allItems.Add(newChip);
                    promoted.Add(newChip);
                    insertAt++;
                }
            }
            return promoted;
        }

        private static List<SheetRow> BuildFlatRows(List<ChipDataItem> items)
        {
            var rows = new List<SheetRow>();
            foreach (var chip in items)
            {
                rows.Add(new SheetRow
                {
                    Kind = RowKind.Leaf,
                    Chip = chip,
                    DisplayNormal = chip.BaseDisplayText,
                });
            }
            return rows;
        }

        private static List<SheetRow> BuildFileTypeRows(List<ChipDataItem> chips, HashSet<ChipDataItem> orphanChildren, Dictionary<ChipDataItem, int> counts)
        {
            var rows = new List<SheetRow>();
            int i = 0;
            bool seenTopLevel = false;
            bool emittedSyntheticOtherParent = false;
            while (i < chips.Count)
            {
                var chip = chips[i];
                bool isNewTopLevelGroup = false;

                if (orphanChildren.Contains(chip))
                {
                    // Emit one synthetic "Other" parent the first time we hit a promoted chip,
                    // then stream all promoted chips as its indented children.
                    if (!emittedSyntheticOtherParent)
                    {
                        int aggregate = 0;
                        foreach (var promoted in chips)
                        {
                            if (orphanChildren.Contains(promoted) && counts.TryGetValue(promoted, out var pc))
                            {
                                // FileType responses are exclusive (each response has one dominant
                                // type), so summing per-child counts is exact.
                                aggregate += pc;
                            }
                        }
                        rows.Add(new SheetRow
                        {
                            Kind = RowKind.Parent,
                            Chip = null,
                            DisplayBold = "Other",
                            ShowTopSeparator = seenTopLevel,
                            IsSyntheticOtherParent = true,
                            SyntheticCount = aggregate,
                        });
                        emittedSyntheticOtherParent = true;
                        isNewTopLevelGroup = true;
                    }

                    rows.Add(new SheetRow
                    {
                        Kind = RowKind.Child,
                        Chip = chip,
                        ParentChip = null,
                        DisplayLight = chip.BaseDisplayText,
                        IndentAsChild = true,
                        IsOrphanChild = true,
                    });
                    i++;
                }
                else if (chip.IsAllCase)
                {
                    string baseName = chip.BaseDisplayText;
                    rows.Add(new SheetRow
                    {
                        Kind = RowKind.Parent,
                        Chip = chip,
                        DisplayBold = PrettyBase(baseName),
                        ShowTopSeparator = seenTopLevel,
                    });
                    isNewTopLevelGroup = true;
                    i++;
                    while (i < chips.Count && !orphanChildren.Contains(chips[i]) && IsChildOf(chips[i], baseName))
                    {
                        var child = chips[i];
                        rows.Add(new SheetRow
                        {
                            Kind = RowKind.Child,
                            Chip = child,
                            ParentChip = chip,
                            DisplayLight = StripBasePrefix(child.BaseDisplayText, baseName),
                            IndentAsChild = true,
                        });
                        i++;
                    }
                }
                else
                {
                    var row = new SheetRow
                    {
                        Kind = RowKind.Leaf,
                        Chip = chip,
                        ShowTopSeparator = seenTopLevel,
                    };
                    int paren = chip.BaseDisplayText.IndexOf(" (", System.StringComparison.Ordinal);
                    if (paren > 0 && chip.BaseDisplayText.EndsWith(")"))
                    {
                        row.DisplayBold = PrettyBase(chip.BaseDisplayText.Substring(0, paren));
                        row.DisplayLight = " " + chip.BaseDisplayText.Substring(paren + 1);
                    }
                    else
                    {
                        row.DisplayBold = chip.BaseDisplayText;
                    }
                    rows.Add(row);
                    isNewTopLevelGroup = true;
                    i++;
                }

                if (isNewTopLevelGroup)
                {
                    seenTopLevel = true;
                }
            }
            return rows;
        }

        private static bool IsChildOf(ChipDataItem chip, string baseName)
        {
            return chip.BaseDisplayText.StartsWith(baseName) && chip.GetFullDisplayText() != baseName + ChipsHelper.ALL_SUFFIX;
        }

        private static string StripBasePrefix(string displayText, string baseName)
        {
            // "mp3 (320kbps)" -> "320kbps", "mp3 (other)" -> "other"
            string needle = baseName + " (";
            if (displayText.StartsWith(needle) && displayText.EndsWith(")"))
            {
                return displayText.Substring(needle.Length, displayText.Length - needle.Length - 1);
            }
            // Fallback: return the original text.
            return displayText;
        }

        private static string PrettyBase(string baseName)
        {
            if (baseName.Length > 0 && baseName.Length <= 5 && IsAlphanumericLower(baseName))
            {
                return baseName.ToUpperInvariant();
            }
            return baseName;
        }

        private static bool IsAlphanumericLower(string s)
        {
            foreach (var c in s)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }

        // ---------- tap handling ----------

        private void OnRowTapped(SheetRow row, RowAdapter adapter, int bindPos)
        {
            switch (row.Kind)
            {
                case RowKind.Leaf:
                    row.Chip.IsChecked = !row.Chip.IsChecked;
                    CommitAndNotify();
                    adapter.NotifyItemChanged(bindPos);
                    break;

                case RowKind.Parent:
                {
                    // Toggle on VISUAL state — if the parent checkbox currently renders fully
                    // checked (from row.Chip.IsChecked OR every child being individually checked),
                    // the tap means "clear the whole group". Otherwise the tap means "select all".
                    bool wasFullyChecked = adapter.IsParentFullyChecked(row);
                    if (row.IsSyntheticOtherParent)
                    {
                        var promotedKids = adapter.GetRowsSnapshot().Where(r => r.IsOrphanChild).ToList();
                        bool newChecked = !wasFullyChecked;
                        foreach (var r in promotedKids)
                        {
                            r.Chip.IsChecked = newChecked;
                        }
                    }
                    else
                    {
                        bool newChecked = !wasFullyChecked;
                        row.Chip.IsChecked = newChecked;
                        foreach (var sibling in adapter.GetRowsSnapshot())
                        {
                            if (sibling.Kind == RowKind.Child && sibling.ParentChip == row.Chip)
                            {
                                if (newChecked)
                                {
                                    sibling.Chip.IsChecked = false;
                                    sibling.Chip.IsEnabled = false;
                                }
                                else
                                {
                                    sibling.Chip.IsChecked = false;
                                    sibling.Chip.IsEnabled = true;
                                }
                            }
                        }
                    }
                    CommitAndNotify();
                    adapter.NotifyDataSetChanged();
                    break;
                }

                case RowKind.Child:
                    if (!row.Chip.IsEnabled)
                    {
                        return;
                    }
                    row.Chip.IsChecked = !row.Chip.IsChecked;
                    CommitAndNotify();
                    adapter.NotifyItemChanged(bindPos);
                    break;
            }
        }

        private void CommitAndNotify()
        {
            var searchTab = SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab];
            searchTab.ChipsFilter = SearchFilter.ParseChips(searchTab.ChipDataItems);
            SearchFragment.Instance?.RefreshOnChipChanged();

            var recyclerChips = SearchFragment.Instance?.recyclerViewChips;
            var groupedAdapter = recyclerChips?.GetAdapter() as GroupedChipsItemRecyclerAdapter;
            groupedAdapter?.NotifyCategoryChanged(category);
        }

        private class RowAdapter : RecyclerView.Adapter
        {
            // Mirrors Material's MaterialCheckBox.STATE_* constants. Using local copies
            // because the Xamarin binding surface for these constants varies by version;
            // Java source guarantees these exact int values.
            private const int STATE_UNCHECKED = 0;
            private const int STATE_CHECKED = 1;
            private const int STATE_INDETERMINATE = 2;

            private readonly List<SheetRow> rows;
            private readonly Dictionary<ChipDataItem, int> counts;
            private readonly bool filterActive;
            private readonly int total;
            private readonly System.Action<SheetRow, RowAdapter, int> onTap;

            public RowAdapter(List<SheetRow> rows, Dictionary<ChipDataItem, int> counts, bool filterActive, int total, System.Action<SheetRow, RowAdapter, int> onTap)
            {
                this.rows = rows;
                this.counts = counts;
                this.filterActive = filterActive;
                this.total = total;
                this.onTap = onTap;
            }

            public List<SheetRow> GetRowsSnapshot() => rows;

            public bool IsParentFullyChecked(SheetRow row) => ComputeCheckedState(row, rows) == STATE_CHECKED;

            public override int ItemCount => rows.Count;

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.category_filter_bottom_sheet_row, parent, false);
                return new RowHolder(view);
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var h = (RowHolder)holder;
                var row = rows[position];
                var ctx = h.ItemView.Context;

                h.TopSeparator.Visibility = row.ShowTopSeparator ? ViewStates.Visible : ViewStates.Gone;

                float density = ctx.Resources.DisplayMetrics.Density;
                int basePad = (int)(20 * density);
                int childPad = (int)(40 * density);
                int vertPad = (int)(8 * density);
                int start = row.IndentAsChild ? childPad : basePad;
                h.Horizontal.SetPaddingRelative(start, vertPad, basePad, vertPad);

                h.Text.TextFormatted = BuildRowText(row, ctx);

                int x;
                if (row.IsSyntheticOtherParent)
                {
                    x = row.SyntheticCount;
                }
                else
                {
                    x = counts.TryGetValue(row.Chip, out var cnt) ? cnt : 0;
                }
                if (filterActive)
                {
                    h.Count.Text = string.Format(ctx.GetString(Resource.String.filter_sheet_count_filtered), x, total);
                }
                else
                {
                    h.Count.Text = string.Format(ctx.GetString(Resource.String.filter_sheet_count), x);
                }

                int checkedState = ComputeCheckedState(row, rows);
                h.Check.CheckedState = checkedState;

                bool enabled = row.Chip == null || row.Chip.IsEnabled;
                h.ItemView.Alpha = enabled ? 1f : 0.4f;
                h.ItemView.Clickable = enabled;
                h.Check.Enabled = enabled;

                h.ItemView.Click -= h.ClickHandler;
                h.ClickHandler = (s, e) =>
                {
                    int bindPos = h.BindingAdapterPosition;
                    if (bindPos == RecyclerView.NoPosition)
                    {
                        return;
                    }
                    onTap(rows[bindPos], this, bindPos);
                };
                h.ItemView.Click += h.ClickHandler;
            }

            private static int ComputeCheckedState(SheetRow row, List<SheetRow> allRows)
            {
                // For non-parent rows, mirror chip.IsChecked as StateChecked/StateUnchecked.
                if (row.Kind != RowKind.Parent)
                {
                    return row.Chip != null && row.Chip.IsChecked
                        ? STATE_CHECKED
                        : STATE_UNCHECKED;
                }

                // Real parent whose backing " - all" chip is checked → fully checked. Children
                // are disabled in this state and their individual IsChecked flags are ignored.
                if (row.Chip != null && row.Chip.IsChecked)
                {
                    return STATE_CHECKED;
                }

                // Otherwise derive the state from children: no child checked → unchecked,
                // all children checked → checked, any mix → indeterminate.
                List<SheetRow> children;
                if (row.IsSyntheticOtherParent)
                {
                    children = allRows.Where(r => r.IsOrphanChild).ToList();
                }
                else
                {
                    children = allRows.Where(r => r.Kind == RowKind.Child && r.ParentChip == row.Chip).ToList();
                }

                if (children.Count == 0)
                {
                    return STATE_UNCHECKED;
                }
                int checkedCount = children.Count(c => c.Chip != null && c.Chip.IsChecked);
                if (checkedCount == 0)
                {
                    return STATE_UNCHECKED;
                }
                if (checkedCount == children.Count)
                {
                    return STATE_CHECKED;
                }
                return STATE_INDETERMINATE;
            }

            private static Java.Lang.ICharSequence BuildRowText(SheetRow row, Context ctx)
            {
                string bold = row.DisplayBold ?? string.Empty;
                string light = row.DisplayLight ?? string.Empty;
                string normal = row.DisplayNormal ?? string.Empty;
                // TODO remove bold
                string full = bold + normal + light;
                var ss = new SpannableString(full);
                //if (bold.Length > 0)
                //{
                //    ss.SetSpan(new StyleSpan(Android.Graphics.TypefaceStyle.Bold), 0, bold.Length, SpanTypes.ExclusiveExclusive);
                //}
                if (light.Length > 0)
                {
                    var subdued = UiHelpers.GetColorFromAttribute(ctx, Resource.Attribute.cellTextColorSubdued);
                    ss.SetSpan(new ForegroundColorSpan(subdued), bold.Length, full.Length, SpanTypes.ExclusiveExclusive);
                }
                return ss;
            }
        }

        private class RowHolder : RecyclerView.ViewHolder
        {
            public View TopSeparator;
            public ViewGroup Horizontal;
            public TextView Text;
            public TextView Count;
            public Google.Android.Material.CheckBox.MaterialCheckBox Check;
            public System.EventHandler ClickHandler;

            public RowHolder(View view) : base(view)
            {
                TopSeparator = view.FindViewById<View>(Resource.Id.categoryFilterRowTopSeparator);
                Horizontal = view.FindViewById<ViewGroup>(Resource.Id.categoryFilterRowHorizontal);
                Text = view.FindViewById<TextView>(Resource.Id.categoryFilterRowText);
                Count = view.FindViewById<TextView>(Resource.Id.categoryFilterRowCount);
                Check = view.FindViewById<Google.Android.Material.CheckBox.MaterialCheckBox>(Resource.Id.categoryFilterRowCheck);
            }
        }
    }
}
