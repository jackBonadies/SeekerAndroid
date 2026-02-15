using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
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
                ISearchItemViewBase view = null;
                switch (this.searchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandedAll:
                    case SearchResultStyleEnum.CollapsedAll:
                        view = SearchItemViewExpandable.inflate(parent);
                        (view as SearchItemViewExpandable).AdapterRef = this;
                        (view as View).FindViewById<ImageView>(Resource.Id.expandableClick).Click += CustomAdapter_Click;
                        (view as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1;
                        break;
                    case SearchResultStyleEnum.Medium:
                        view = SearchItemViewMedium.inflate(parent);
                        break;
                    case SearchResultStyleEnum.Minimal:
                        view = SearchItemViewMinimal.inflate(parent);
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
                GetSearchFragment().showEditDialog((sender as ISearchItemViewBase).ViewHolder.AdapterPosition);
            }

            private SearchResultStyleEnum searchResultStyle;

            public SearchAdapterRecyclerVersion(List<SearchResponse> ti)
            {
                oldList = null; // no longer valid...
                localDataSet = ti;
                searchResultStyle = SearchFragment.SearchResultStyle;
                oppositePositions = new List<int>();
            }

            private void CustomAdapter_Click1(object sender, EventArgs e)
            {
                //Logger.InfoFirebase("CustomAdapter_Click1");
                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View);
                SearchFragment.Instance.showEditDialog(position);
            }


            private void CustomAdapter_Click(object sender, EventArgs e)
            {
                //throw new NotImplementedException();


                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View);

                //int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
                var v = ((sender as View).Parent.Parent as View).FindViewById<View>(Resource.Id.detailsExpandable);
                var img = ((sender as View).Parent.Parent as View).FindViewById<ImageView>(Resource.Id.expandableClick);
                if (v.Visibility == ViewStates.Gone)
                {
                    img.Animate().RotationBy((float)(180.0)).SetDuration(350).Start();
                    v.Visibility = ViewStates.Visible;
                    SearchItemViewExpandable.PopulateFilesListView(v as LinearLayout, this.localDataSet[position]);
                    if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
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
                    if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
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
