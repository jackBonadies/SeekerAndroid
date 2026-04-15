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

            var close = root.FindViewById<ImageButton>(Resource.Id.categoryFilterSheetClose);
            close.Click += (s, e) => Dismiss();

            var rv = root.FindViewById<RecyclerView>(Resource.Id.categoryFilterSheetRecycler);
            rv.SetLayoutManager(new LinearLayoutManager(root.Context));

            var searchTab = SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab];
            var categoryItems = (searchTab.ChipDataItems ?? new List<ChipDataItem>())
                .Where(i => i.ChipType == category)
                .ToList();
            var counts = ComputeRowCounts(searchTab, categoryItems);
            bool anyFilterActive = SearchFragment.AreFilterControlsActive()
                || SearchFragment.AreChipsFiltering()
                || SearchTabHelper.TextFilter.IsFiltered;

            var rows = category == ChipType.FileType
                ? BuildFileTypeRows(categoryItems)
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
            public ChipDataItem Chip;
            public ChipDataItem ParentChip;
            public string DisplayBold;
            public string DisplayLight;
            public bool ShowTopSeparator;
            public bool IndentAsChild;
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
                    DisplayBold = chip.DisplayText,
                });
            }
            return rows;
        }

        private static List<SheetRow> BuildFileTypeRows(List<ChipDataItem> chips)
        {
            var rows = new List<SheetRow>();
            int i = 0;
            bool seenTopLevel = false;
            while (i < chips.Count)
            {
                var chip = chips[i];
                bool isNewTopLevelGroup = false;

                if (chip.DisplayText.EndsWith(" - all"))
                {
                    string baseName = chip.DisplayText.Substring(0, chip.DisplayText.Length - " - all".Length);
                    rows.Add(new SheetRow
                    {
                        Kind = RowKind.Parent,
                        Chip = chip,
                        DisplayBold = PrettyBase(baseName),
                        ShowTopSeparator = seenTopLevel,
                    });
                    isNewTopLevelGroup = true;
                    i++;
                    while (i < chips.Count && IsChildOf(chips[i], baseName))
                    {
                        var child = chips[i];
                        rows.Add(new SheetRow
                        {
                            Kind = RowKind.Child,
                            Chip = child,
                            ParentChip = chip,
                            DisplayLight = StripBasePrefix(child.DisplayText, baseName),
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
                    int paren = chip.DisplayText.IndexOf(" (", System.StringComparison.Ordinal);
                    if (paren > 0 && chip.DisplayText.EndsWith(")"))
                    {
                        row.DisplayBold = PrettyBase(chip.DisplayText.Substring(0, paren));
                        row.DisplayLight = chip.DisplayText.Substring(paren + 1); // includes leading space + "(…)"
                    }
                    else
                    {
                        row.DisplayBold = chip.DisplayText;
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
            return chip.DisplayText.Contains(baseName) && chip.DisplayText != baseName + " - all";
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
                    bool newChecked = !row.Chip.IsChecked;
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
                                sibling.Chip.IsEnabled = true;
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

                int x = counts.TryGetValue(row.Chip, out var cnt) ? cnt : 0;
                if (filterActive)
                {
                    h.Count.Text = string.Format(ctx.GetString(Resource.String.filter_sheet_count_filtered), x, total);
                }
                else
                {
                    h.Count.Text = string.Format(ctx.GetString(Resource.String.filter_sheet_count), x);
                }

                h.Check.Checked = row.Chip.IsChecked;

                bool enabled = row.Chip.IsEnabled;
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

            private static Java.Lang.ICharSequence BuildRowText(SheetRow row, Context ctx)
            {
                string bold = row.DisplayBold ?? string.Empty;
                string light = row.DisplayLight ?? string.Empty;
                string full = bold + light;
                var ss = new SpannableString(full);
                if (bold.Length > 0)
                {
                    ss.SetSpan(new StyleSpan(Android.Graphics.TypefaceStyle.Bold), 0, bold.Length, SpanTypes.ExclusiveExclusive);
                }
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
            public CheckBox Check;
            public System.EventHandler ClickHandler;

            public RowHolder(View view) : base(view)
            {
                TopSeparator = view.FindViewById<View>(Resource.Id.categoryFilterRowTopSeparator);
                Horizontal = view.FindViewById<ViewGroup>(Resource.Id.categoryFilterRowHorizontal);
                Text = view.FindViewById<TextView>(Resource.Id.categoryFilterRowText);
                Count = view.FindViewById<TextView>(Resource.Id.categoryFilterRowCount);
                Check = view.FindViewById<CheckBox>(Resource.Id.categoryFilterRowCheck);
            }
        }
    }
}
