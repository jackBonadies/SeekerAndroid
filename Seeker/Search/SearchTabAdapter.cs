using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Seeker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    public class SearchTabItemRecyclerAdapter : RecyclerView.Adapter
    {
        private List<int> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public bool ForWishlist = false;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            SearchTabView view = SearchTabView.inflate(parent);
            view.setupChildren();
            // .inflate(R.layout.text_row_item, viewGroup, false);
            (view as SearchTabView).searchTabLayout.Click += SearchTabLayout_Click;
            (view as SearchTabView).removeSearch.Click += RemoveSearch_Click;
            return new SearchTabViewHolder(view as View);


        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as SearchTabViewHolder).searchTabView.setItem(localDataSet[position]);
        }

        private void RemoveSearch_Click(object sender, EventArgs e)
        {
            position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
            if (position == -1) //in my case this happens if you delete too fast...
            {
                return;
            }
            int tabToRemove = localDataSet[position];
            bool isWishlist = (SearchTabHelper.SearchTabCollection[tabToRemove].SearchTarget == SearchTarget.Wishlist);
            SearchTabHelper.SearchTabCollection[tabToRemove].CancellationTokenSource?.Cancel();
            if (isWishlist)
            {
                if (tabToRemove == SearchTabHelper.CurrentTab)
                {
                    //remove it for real
                    SearchTabHelper.SearchTabCollection.Remove(tabToRemove, out _);
                    localDataSet.RemoveAt(position);
                    SearchTabDialog.Instance.recycleWishesAdapter.NotifyItemRemoved(position);


                    //go to search tab instead (there is always one)
                    string listOfKeys2 = System.String.Join(",", SearchTabHelper.SearchTabCollection.Keys);
                    MainActivity.LogInfoFirebase("list of Keys: " + listOfKeys2);
                    int tabToGoTo = SearchTabHelper.SearchTabCollection.Keys.Where(key => key >= 0).First();
                    SearchFragment.Instance.GoToTab(tabToGoTo, true);
                }
                else
                {
                    //remove it for real
                    SearchTabHelper.SearchTabCollection.Remove(tabToRemove, out _);
                    localDataSet.RemoveAt(position);
                    SearchTabDialog.Instance.recycleWishesAdapter.NotifyItemRemoved(position);
                }
            }
            else
            {
                if (tabToRemove == SearchTabHelper.CurrentTab)
                {
                    SearchTabHelper.SearchTabCollection[tabToRemove] = new SearchTab(); //clear it..
                    SearchFragment.Instance.GoToTab(tabToRemove, true);
                    SearchTabDialog.Instance.recycleSearchesAdapter.NotifyItemChanged(position);
                }
                else
                {

                    if (SearchTabHelper.SearchTabCollection.Keys.Where(key => key >= 0).Count() == 1)
                    {
                        //it is the only non wishlist tab, so just clear it...  this can happen if we are on a wishlist tab and we clear all the normal tabs.
                        SearchTabHelper.SearchTabCollection[tabToRemove] = new SearchTab();
                        SearchTabDialog.Instance.recycleSearchesAdapter.NotifyItemChanged(position);
                    }
                    else
                    {
                        //remove it for real
                        SearchTabHelper.SearchTabCollection.Remove(tabToRemove, out _);
                        localDataSet.RemoveAt(position);
                        SearchTabDialog.Instance.recycleSearchesAdapter.NotifyItemRemoved(position);
                    }
                }
            }
            if (isWishlist)
            {
                SearchTabHelper.SaveHeadersToSharedPrefs();
                SearchTabHelper.RemoveTabFromSharedPrefs(tabToRemove, SeekerState.ActiveActivityRef);
            }
            SearchFragment.Instance.SetCustomViewTabNumberImageViewState();
        }

        private void SearchTabLayout_Click(object sender, EventArgs e)
        {
            position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
            int tabToGoTo = localDataSet[position];
            SearchFragment.Instance.GoToTab(tabToGoTo, false);
            SearchTabDialog.Instance.Dismiss();
        }

        public SearchTabItemRecyclerAdapter(List<int> ti)
        {
            localDataSet = ti;
        }

    }

    public class SearchTabViewHolder : RecyclerView.ViewHolder
    {
        public SearchTabView searchTabView;


        public SearchTabViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            searchTabView = (SearchTabView)view;
            searchTabView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public SearchTabView getUnderlyingView()
        {
            return searchTabView;
        }
    }

    public class SearchTabView : LinearLayout
    {
        public LinearLayout searchTabLayout;
        public ImageView removeSearch;
        private TextView lastSearchTerm;
        private TextView numResults;
        public SearchTabViewHolder ViewHolder;
        public int SearchId = int.MaxValue;
        public SearchTabView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.tab_page_item, this, true);
            setupChildren();
        }
        public SearchTabView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.tab_page_item, this, true);
            setupChildren();
        }
        public static SearchTabView inflate(ViewGroup parent)
        {
            SearchTabView itemView = (SearchTabView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.tab_page_item_dummy, parent, false);
            return itemView;
        }
        public void setupChildren()
        {
            lastSearchTerm = FindViewById<TextView>(Resource.Id.lastSearchTerm);
            numResults = FindViewById<TextView>(Resource.Id.resultsText);
            removeSearch = FindViewById<ImageView>(Resource.Id.searchTabItemRemove);
            searchTabLayout = FindViewById<LinearLayout>(Resource.Id.searchTabItemMain);
        }

        public void setItem(int i)
        {
            SearchTab searchTab = SearchTabHelper.SearchTabCollection[i];
            if (searchTab.SearchTarget == SearchTarget.Wishlist)
            {
                string timeString = "-";
                if (searchTab.LastRanTime != DateTime.MinValue)
                {
                    timeString = CommonHelpers.GetNiceDateTime(searchTab.LastRanTime);
                }
                numResults.Text = searchTab.LastSearchResultsCount.ToString() + " Results, Last Ran: " + timeString;
            }
            else
            {
                numResults.Text = searchTab.LastSearchResultsCount.ToString() + " Results";
            }
            string lastTerm = searchTab.LastSearchTerm;
            if (lastTerm != string.Empty && lastTerm != null)
            {
                lastSearchTerm.Text = searchTab.LastSearchTerm;
            }
            else
            {
                lastSearchTerm.Text = "[No Search]";
            }
        }
    }

}