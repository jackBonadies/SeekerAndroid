using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Common;
using Seeker.Helpers;
using Seeker.Search;
using Soulseek;
using System;
using System.Collections.Generic;

namespace Seeker
{
    public partial class SearchFragment
    {
        public class SearchAdapterRecyclerVersion : RecyclerView.Adapter
        {
            public List<int> oppositePositions = new List<int>();



            public List<SearchResponse> localDataSet;
            public override int ItemCount => localDataSet.Count;
            private int position = -1;

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var item = localDataSet[position];
                var view = (holder as SearchViewHolder).getSearchItemView();
                view.setItem(item, position);
                if (view is SearchItemViewUnifiedBase rowView)
                {
                    var fragment = GetSearchFragment();
                    bool selectionMode = fragment?.IsSelectionMode ?? false;
                    rowView.ApplySelectionState(selectionMode, selectionMode && fragment.IsResultSelected(item));
                    if (wishlistUnseenSet != null)
                    {
                        rowView.ApplyNewIndicator(wishlistUnseenSet.Contains(item));
                    }
                    // if we reuse a view from a wishlist tab, then set it to unseen
                    else if (rowView.HasNewIndicator)
                    {
                        rowView.ApplyNewIndicator(false);
                    }
                }
                //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            }

            public void setPosition(int position)
            {
                this.position = position;
            }

            public int getPosition()
            {
                return this.position;
            }

            //public override void OnViewRecycled(Java.Lang.Object holder)
            //{
            //    base.OnViewRecycled(holder);
            //}

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var family = this.searchResultStyle & ~SearchResultStyleEnum.Expandable;
                SearchItemViewUnifiedBase view;
                switch (family)
                {
                    case SearchResultStyleEnum.SimpleBottom:
                        view = new SearchItemViewSimpleBottom(parent.Context);
                        break;
                    case SearchResultStyleEnum.SimpleTop:
                        view = new SearchItemViewSimpleTop(parent.Context);
                        break;
                    case SearchResultStyleEnum.ModernBottom:
                        view = new SearchItemViewModernBottom(parent.Context);
                        break;
                    case SearchResultStyleEnum.ModernTop:
                        view = new SearchItemViewModernTop(parent.Context);
                        break;
                    case SearchResultStyleEnum.Compact:
                        view = new SearchItemViewCompact(parent.Context);
                        break;
                    default:
                        throw new System.InvalidOperationException(
                            $"Unknown search result style family {family}");
                }
                view.LayoutParameters = new RecyclerView.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                // Compact never expands, even if Expandable was somehow left set.
                view.IsExpandable = !this.searchResultStyle.HasFlag(SearchResultStyleEnum.Compact)
                    && this.searchResultStyle.HasFlag(SearchResultStyleEnum.Expandable);
                view.AdapterRef = this;
                view.ApplyExpandableMode();
                var mainLayout = view.FindViewById<LinearLayout>(Resource.Id.relativeLayout1);
                mainLayout.Click += UnifiedRowClick;
                mainLayout.LongClickable = true;
                mainLayout.LongClick += UnifiedRowLongClick;
                // The primary row also owns the selection checkbox, which sits beside relativeLayout1.
                // A checkbox tap therefore reaches this same toggle/open handler.
                var primaryRow = view.FindViewById<LinearLayout>(Resource.Id.searchItemPrimaryRow);
                if (primaryRow != null && !ReferenceEquals(primaryRow, mainLayout))
                {
                    primaryRow.Click += UnifiedRowClick;
                    primaryRow.LongClickable = true;
                    primaryRow.LongClick += UnifiedRowLongClick;
                }
                if (view.DownloadActionButton != null)
                {
                    view.DownloadActionButton.Click += UnifiedDownloadClick;
                }
                if (view.IsExpandable)
                {
                    view.FindViewById<FrameLayout>(Resource.Id.expandClickArea).Click += UnifiedChevronClick;
                    if (this.searchResultStyle.HasFlag(SearchResultStyleEnum.Modern))
                    {
                        int padEnd = (int)(2 * mainLayout.Resources.DisplayMetrics.Density);
                        mainLayout.SetPaddingRelative(mainLayout.PaddingStart, mainLayout.PaddingTop, padEnd, mainLayout.PaddingBottom);
                    }
                }
                return new SearchViewHolder(view);
            }

            // Row click: relativeLayout1 fills the content area (chevron column has width=0 when hidden),
            // so this catches taps anywhere on the row that aren't on the chevron itself.
            // Parent chain in the unified XMLs: relativeLayout1 -> horizontal row -> item root (a SearchItemViewUnifiedBase).
            private void UnifiedRowClick(object sender, EventArgs e)
            {
                if (!TryGetBoundPosition((sender as View).FindAncestor<ISearchItemViewBase>(), out int position))
                {
                    return;
                }
                var fragment = GetSearchFragment();
                if (fragment == null)
                {
                    return;
                }
                if (fragment.IsSelectionMode)
                {
                    fragment.ToggleSelectionAt(position);
                }
                else
                {
                    fragment.ShowDownloadDialog(position);
                }
            }

            // Long press starts the result multi-selection action mode. The per-result download
            // dialog stays on the row tap and the dedicated download action.
            private void UnifiedRowLongClick(object sender, View.LongClickEventArgs e)
            {
                if (!TryGetBoundPosition((sender as View).FindAncestor<ISearchItemViewBase>(), out int position))
                {
                    return;
                }
                e.Handled = true;
                GetSearchFragment()?.EnterSelectionMode(position);
            }

            private void UnifiedDownloadClick(object sender, EventArgs e)
            {
                if (!TryGetBoundPosition((sender as View).FindAncestor<ISearchItemViewBase>(), out int position))
                {
                    return;
                }
                var fragment = GetSearchFragment();
                if (fragment == null)
                {
                    return;
                }
                // While selecting, the row's contextual download affordance is hidden; if a
                // recycled tap still reaches it, toggle the selection instead of opening the
                // single-result download dialog behind the action mode.
                if (fragment.IsSelectionMode)
                {
                    fragment.ToggleSelectionAt(position);
                }
                else
                {
                    fragment.ShowDownloadDialog(position);
                }
            }

            // A tap can land on a view that is being recycled or rebound (e.g. while the wishlist
            // results are replaced), where BindingAdapterPosition is NoPosition or already points
            // past the current data set. Validating it keeps those taps from indexing the data.
            private bool TryGetBoundPosition(ISearchItemViewBase itemRoot, out int position)
            {
                position = RecyclerView.NoPosition;
                var holder = itemRoot?.ViewHolder;
                if (holder == null || localDataSet == null)
                {
                    return false;
                }
                int adapterPosition = holder.BindingAdapterPosition;
                if (adapterPosition == RecyclerView.NoPosition
                    || adapterPosition < 0
                    || adapterPosition >= localDataSet.Count)
                {
                    return false;
                }
                position = adapterPosition;
                return true;
            }

            private SearchResultStyleEnum searchResultStyle;
            private readonly HashSet<SearchResponse> wishlistUnseenSet;

            public SearchAdapterRecyclerVersion(SearchTab tab, List<SearchResponse> ti)
            {
                oldList = null; // no longer valid...
                localDataSet = ti;
                searchResultStyle = PreferencesState.SearchResultStyle;
                oppositePositions = new List<int>();
                if (tab != null && tab.SearchTarget == SearchTarget.Wishlist)
                {
                    wishlistUnseenSet = tab.UnseenResults;
                }
            }

            // Chevron click: parent chain in unified XMLs is
            // expandClickArea -> horizontal row -> item root (a SearchItemViewUnifiedBase).
            private void UnifiedChevronClick(object sender, EventArgs e)
            {
                var itemRoot = (sender as View).FindAncestor<SearchItemViewUnifiedBase>();
                if (!TryGetBoundPosition(itemRoot, out int position))
                {
                    return;
                }
                var v = itemRoot.FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
                var img = itemRoot.FindViewById<ImageView>(Resource.Id.expandableClick);
                if (v.Visibility == ViewStates.Gone)
                {
                    img.Animate().RotationBy(180f).SetDuration(100).Start();
                    v.Visibility = ViewStates.Visible;
                    itemRoot.PopulateFiles(this.localDataSet[position]);
                    if (!SearchFragment.ExpandAllResults)
                    {
                        oppositePositions.Add(position);
                        oppositePositions.Sort();
                    }
                    else
                    {
                        oppositePositions.Remove(position);
                    }
                }
                else
                {
                    img.Animate().RotationBy(-180f).SetDuration(350).Start();
                    v.Visibility = ViewStates.Gone;
                    if (!SearchFragment.ExpandAllResults)
                    {
                        oppositePositions.Remove(position);
                    }
                    else
                    {
                        oppositePositions.Add(position);
                        oppositePositions.Sort();
                    }
                }
            }
        }

        public class SearchViewHolder : RecyclerView.ViewHolder
        {
            private ISearchItemViewBase searchItemView;

            public SearchViewHolder(View view) : base(view)
            {
                //super(view);
                // Define click listener for the ViewHolder's View

                searchItemView = (ISearchItemViewBase)view;
                searchItemView.ViewHolder = this;
                //searchItemView.SetOnCreateContextMenuListener(this);
            }

            public ISearchItemViewBase getSearchItemView()
            {
                return searchItemView;
            }
        }

        public static void RefreshSearchResultsHeader(View headerView)
        {
            if (headerView == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(SearchTabHelper.LastSearchTerm))
            {
                headerView.Visibility = ViewStates.Gone;
                return;
            }

            if ((SearchTabHelper.SearchResponses?.Count ?? 0) == 0)
            {
                headerView.Visibility = ViewStates.Gone;
                return;
            }

            var tv = headerView.FindViewById<TextView>(Resource.Id.searchResultsHeaderText);
            if (tv == null)
            {
                return;
            }

            int total = SearchTabHelper.SearchResponses?.Count ?? 0;
            int shown = SearchTabHelper.UI_SearchResponses?.Count ?? total;
            bool filterActive = SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive();

            headerView.Visibility = ViewStates.Visible;
            var ctx = tv.Context;
            if (total == 0)
            {
                tv.Text = ctx.GetString(Resource.String.search_results_count_none);
            }
            else if (filterActive && shown != total)
            {
                string shownStr = shown.ToString();
                string totalStr = total.ToString();
                string full = string.Format(ctx.GetString(Resource.String.search_results_count_filtered), shownStr, totalStr);
                tv.TextFormatted = BuildEmphasizedCount(ctx, full, shownStr, totalStr);
            }
            else
            {
                string totalStr = total.ToString();
                string full = string.Format(ctx.GetString(Resource.String.search_results_count), totalStr);
                tv.TextFormatted = BuildEmphasizedCount(ctx, full, totalStr);
            }

            BindHeaderActions(headerView, ctx);
        }

        /// <summary>
        /// Compact sort entry and active-filter summary in the result header. The sort button
        /// calls the existing dialog; filtering itself still runs in the existing pipeline.
        /// </summary>
        private static void BindHeaderActions(View headerView, Android.Content.Context ctx)
        {
            var sortButton = headerView.FindViewById<TextView>(Resource.Id.searchSortButton);
            if (sortButton != null)
            {
                sortButton.Text = string.Format(
                    ctx.GetString(Resource.String.search_sort_button),
                    SearchSortLabels.Label(ctx, SearchTabHelper.SortHelperSorting));
                sortButton.ContentDescription = sortButton.Text;
                if (!sortButton.HasOnClickListeners)
                {
                    sortButton.Click += (_, __) => SearchFragment.Instance?.ShowChangeSortOrderDialog();
                }
            }

            var filtersText = headerView.FindViewById<TextView>(Resource.Id.searchActiveFiltersText);
            if (filtersText != null)
            {
                string summary = BuildActiveFilterSummary(ctx);
                filtersText.Text = summary;
                filtersText.Visibility = string.IsNullOrEmpty(summary) ? ViewStates.Gone : ViewStates.Visible;
            }
        }

        private static string BuildActiveFilterSummary(Android.Content.Context ctx)
        {
            var parts = new List<string>();
            if (SearchTabHelper.TextFilter.IsFiltered)
            {
                parts.Add(string.Format(ctx.GetString(Resource.String.search_filter_text_prefix), SearchTabHelper.TextFilter.FilterString));
            }
            if (PreferencesState.FilterFormat != FormatFilterType.Any)
            {
                string format = PreferencesState.FilterFormat == FormatFilterType.Lossless
                    ? ctx.GetString(Resource.String.filter_format_lossless)
                    : ctx.GetString(Resource.String.filter_format_lossy);
                parts.Add(string.Format(ctx.GetString(Resource.String.search_filter_format_prefix), format));
            }
            if (PreferencesState.FilterMinBitrateKbs > 0)
            {
                parts.Add(string.Format(ctx.GetString(Resource.String.search_filter_bitrate_prefix), PreferencesState.FilterMinBitrateKbs));
            }
            if (AreChipsFiltering())
            {
                var chipItems = SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems;
                int checkedChips = chipItems == null ? 0 : System.Linq.Enumerable.Count(chipItems, i => i.IsChecked);
                parts.Add(string.Format(ctx.GetString(Resource.String.search_filter_chips), checkedChips));
            }
            if (parts.Count == 0)
            {
                return string.Empty;
            }
            return string.Format(ctx.GetString(Resource.String.search_active_filters), string.Join(" \u2022 ", parts));
        }

        private static Android.Text.SpannableString BuildEmphasizedCount(Android.Content.Context ctx, string full, params string[] tokens)
        {
            var ss = new Android.Text.SpannableString(full);
            var color = UiHelpers.GetColorFromAttribute(ctx, Resource.Attribute.cellTextColor);
            int searchFrom = 0;
            foreach (string token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }
                int idx = full.IndexOf(token, searchFrom, StringComparison.Ordinal);
                if (idx < 0)
                {
                    continue;
                }
                int end = idx + token.Length;
                ss.SetSpan(new Android.Text.Style.TypefaceSpan("sans-serif-medium"), idx, end, Android.Text.SpanTypes.ExclusiveExclusive);
                ss.SetSpan(new Android.Text.Style.ForegroundColorSpan(color), idx, end, Android.Text.SpanTypes.ExclusiveExclusive);
                searchFrom = end;
            }
            return ss;
        }
    }
}
