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
            private const int HeaderOffset = 1;

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
                ISearchItemViewBase view = null;
                switch (this.searchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandableLegacy:
                        view = SearchItemViewExpandable.inflate(parent);
                        (view as SearchItemViewExpandable).AdapterRef = this;
                        (view as View).FindViewById<ImageView>(Resource.Id.expandableClick).Click += CustomAdapter_Click;
                        (view as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1;
                        break;
                    case SearchResultStyleEnum.ExpandableModern:
                        view = SearchItemViewExpandableModern.inflate(parent);
                        (view as SearchItemViewExpandableModern).AdapterRef = this;
                        (view as View).FindViewById(Resource.Id.expandClickArea).Click += CustomAdapter_ClickModern;
                        (view as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1Modern;
                        break;
                    case SearchResultStyleEnum.MediumLegacy:
                        view = SearchItemViewMedium.inflate(parent);
                        break;
                    case SearchResultStyleEnum.MinimalLegacy:
                        view = SearchItemViewMinimal.inflate(parent);
                        break;
                    case SearchResultStyleEnum.MediumModernBitrateBottom:
                        view = SearchItemViewMediumBadgeBitrateBottom.inflate(parent);
                        break;
                    case SearchResultStyleEnum.MediumModernBitrateTop:
                        view = SearchItemViewMediumBadgeBitrateTop.inflate(parent);
                        break;
                }
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //view.LongClick += TransferAdapterRecyclerVersion_LongClick;
                (view as View).Click += View_Click;
                return new SearchViewHolder(view as View);

            }

            private void View_Click(object sender, EventArgs e)
            {
                GetSearchFragment().showEditDialog((sender as ISearchItemViewBase).ViewHolder.BindingAdapterPosition);
            }

            private SearchResultStyleEnum searchResultStyle;

            public SearchAdapterRecyclerVersion(List<SearchResponse> ti)
            {
                oldList = null; // no longer valid...
                localDataSet = ti;
                searchResultStyle = PreferencesState.SearchResultStyle;
                oppositePositions = new List<int>();
            }

            private void CustomAdapter_Click1(object sender, EventArgs e)
            {
                //Logger.InfoFirebase("CustomAdapter_Click1");
                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View) - HeaderOffset;
                SearchFragment.Instance.showEditDialog(position);
            }

            private void CustomAdapter_Click1Modern(object sender, EventArgs e)
            {
                // sender = relativeLayout1, parent = header horizontal, parent.parent = root vertical, parent.parent.parent = item root
                var itemRoot = (sender as View).Parent.Parent.Parent as View;
                int position = ((itemRoot as IViewParent).Parent as RecyclerView).GetChildAdapterPosition(itemRoot) - HeaderOffset;
                SearchFragment.Instance.showEditDialog(position);
            }

            private void CustomAdapter_ClickModern(object sender, EventArgs e)
            {
                // sender = expandClickArea, parent = header horizontal, parent.parent = root vertical, parent.parent.parent = item root
                var itemRoot = (sender as View).Parent.Parent.Parent as View;
                int position = ((itemRoot as IViewParent).Parent as RecyclerView).GetChildAdapterPosition(itemRoot) - HeaderOffset;
                var v = itemRoot.FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
                var img = itemRoot.FindViewById<ImageView>(Resource.Id.expandableClick);
                if (v.Visibility == ViewStates.Gone)
                {
                    img.Animate().RotationBy((float)(180.0)).SetDuration(100).Start();
                    v.Visibility = ViewStates.Visible;
                    //sep.Visibility = ViewStates.Visible;
                    (itemRoot as SearchItemViewExpandableModern).PopulateFilesListView(v as LinearLayout, this.localDataSet[position]);
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
                    img.Animate().RotationBy((float)(-180.0)).SetDuration(350).Start();
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

            private void CustomAdapter_Click(object sender, EventArgs e)
            {
                //throw new NotImplementedException();


                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View) - HeaderOffset;

                //int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
                var v = ((sender as View).Parent.Parent as View).FindViewById<View>(Resource.Id.detailsExpandable);
                var img = ((sender as View).Parent.Parent as View).FindViewById<ImageView>(Resource.Id.expandableClick);
                if (v.Visibility == ViewStates.Gone)
                {
                    img.Animate().RotationBy((float)(180.0)).SetDuration(350).Start();
                    v.Visibility = ViewStates.Visible;
                    SearchItemViewExpandable.PopulateFilesListView(v as LinearLayout, this.localDataSet[position]);
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
                    img.Animate().RotationBy((float)(-180.0)).SetDuration(350).Start();
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

        public class SearchResultsHeaderAdapter : RecyclerView.Adapter
        {
            public override int ItemCount => 1;

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_results_header, parent, false);
                return new SearchResultsHeaderViewHolder(view);
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var tv = ((SearchResultsHeaderViewHolder)holder).Text;

                if (string.IsNullOrEmpty(SearchTabHelper.LastSearchTerm))
                {
                    holder.ItemView.Visibility = ViewStates.Gone;
                    return;
                }

                int total = SearchTabHelper.SearchResponses?.Count ?? 0;
                int shown = SearchTabHelper.UI_SearchResponses?.Count ?? total;
                bool filterActive = SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive();

                holder.ItemView.Visibility = ViewStates.Visible;
                var ctx = tv.Context;
                if (total == 0)
                {
                    tv.Text = ctx.GetString(Resource.String.search_results_count_none);
                }
                else if (filterActive && shown != total)
                {
                    string shownStr = shown.ToString();
                    string full = string.Format(ctx.GetString(Resource.String.search_results_count_filtered), shownStr, total);
                    tv.TextFormatted = BuildSemiboldCount(full, shownStr);
                }
                else
                {
                    string totalStr = total.ToString();
                    string full = string.Format(ctx.GetString(Resource.String.search_results_count), totalStr);
                    tv.TextFormatted = BuildSemiboldCount(full, totalStr);
                }
            }

            private static Android.Text.SpannableString BuildSemiboldCount(string full, string countToken)
            {
                var ss = new Android.Text.SpannableString(full);
                int idx = full.IndexOf(countToken);
                if (idx >= 0)
                {
                    ss.SetSpan(new Android.Text.Style.TypefaceSpan("sans-serif-medium"), idx, idx + countToken.Length, Android.Text.SpanTypes.ExclusiveExclusive);
                }
                return ss;
            }
        }

        public class SearchResultsHeaderViewHolder : RecyclerView.ViewHolder
        {
            public TextView Text;
            public SearchResultsHeaderViewHolder(View view) : base(view)
            {
                Text = view.FindViewById<TextView>(Resource.Id.searchResultsHeaderText);
            }
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
