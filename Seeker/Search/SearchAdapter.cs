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


                (holder as SearchViewHolder).getSearchItemView().setItem(localDataSet[position], position);
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
                        view = SearchItemViewSimpleBottom.inflate(parent);
                        break;
                    case SearchResultStyleEnum.SimpleTop:
                        view = SearchItemViewSimpleTop.inflate(parent);
                        break;
                    case SearchResultStyleEnum.ModernBottom:
                        view = SearchItemViewModernBottom.inflate(parent);
                        break;
                    case SearchResultStyleEnum.ModernTop:
                        view = SearchItemViewModernTop.inflate(parent);
                        break;
                    case SearchResultStyleEnum.Compact:
                        view = SearchItemViewCompact.inflate(parent);
                        break;
                    default:
                        throw new System.InvalidOperationException(
                            $"Unknown search result style family {family}");
                }
                // Compact never expands, even if Expandable was somehow left set.
                view.IsExpandable = !this.searchResultStyle.HasFlag(SearchResultStyleEnum.Compact)
                    && this.searchResultStyle.HasFlag(SearchResultStyleEnum.Expandable);
                view.AdapterRef = this;
                view.setupChildren();
                view.ApplyExpandableMode();
                var mainLayout = view.FindViewById<LinearLayout>(Resource.Id.relativeLayout1);
                mainLayout.Click += UnifiedRowClick;
                if (view.IsExpandable)
                {
                    view.FindViewById<FrameLayout>(Resource.Id.expandClickArea).Click += UnifiedChevronClick;
                    if (this.searchResultStyle.HasFlag(SearchResultStyleEnum.Modern))
                    {
                        int padEnd = (int)(2 * mainLayout.Resources.DisplayMetrics.Density);
                        mainLayout.SetPaddingRelative(mainLayout.PaddingStart, mainLayout.PaddingTop, padEnd, mainLayout.PaddingBottom);
                    }
                    view.FindViewById<FrameLayout>(Resource.Id.expandClickArea).Click += UnifiedChevronClick;
                }
                return new SearchViewHolder(view);
            }

            // Row click: relativeLayout1 fills the content area (chevron column has width=0 when hidden),
            // so this catches taps anywhere on the row that aren't on the chevron itself.
            // Parent chain in the unified XMLs: relativeLayout1 -> horizontal row -> item root (a SearchItemViewUnifiedBase).
            private void UnifiedRowClick(object sender, EventArgs e)
            {
                var itemRoot = (sender as View).FindAncestor<ISearchItemViewBase>();
                GetSearchFragment().showEditDialog(itemRoot.ViewHolder.BindingAdapterPosition);
            }

            private SearchResultStyleEnum searchResultStyle;

            public SearchAdapterRecyclerVersion(List<SearchResponse> ti)
            {
                oldList = null; // no longer valid...
                localDataSet = ti;
                searchResultStyle = PreferencesState.SearchResultStyle;
                oppositePositions = new List<int>();
            }

            // Chevron click: parent chain in unified XMLs is
            // expandClickArea -> horizontal row -> item root (a SearchItemViewUnifiedBase).
            private void UnifiedChevronClick(object sender, EventArgs e)
            {
                var itemRoot = (sender as View).FindAncestor<SearchItemViewUnifiedBase>();
                int position = itemRoot.ViewHolder.BindingAdapterPosition;
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

        public class SearchDiffCallback : DiffUtil.Callback
        {
            private List<SearchResponse> oldList;
            private List<SearchResponse> newList;

            public SearchDiffCallback(List<SearchResponse> _oldList, List<SearchResponse> _newList)
            {
                oldList = _oldList;
                newList = _newList;
            }

            public override int NewListSize => newList.Count;

            public override int OldListSize => oldList.Count;

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                return oldList[oldItemPosition].Equals(newList[newItemPosition]); //my override
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                return oldList[oldItemPosition] == newList[newItemPosition];
            }
        }
    }
}
