/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */

using AndriodApp1.Extensions.SearchResponseExtensions;
using AndriodApp1.Helpers;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Java.Lang;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AndriodApp1
{
    public class TabsPagerAdapter : FragmentPagerAdapter
    {
        Fragment login = null;
        Fragment search = null;
        Fragment transfer = null;
        Fragment browse = null;
        public TabsPagerAdapter(FragmentManager fm) : base(fm)
        {
            login = new LoginFragment();
            search = new SearchFragment();
            transfer = new TransfersFragment();
            browse = new BrowseFragment();
        }

        public override int Count => 4;

        public override Fragment GetItem(int position)
        {
            Fragment frag = null;
            switch (position)
            {
                case 0:
                    frag = login;
                    break;
                case 1:
                    frag = search;
                    break;
                case 2:
                    frag = transfer;
                    break;
                case 3:
                    frag = browse;
                    break;
                default:
                    throw new System.Exception("Invalid Tab");
            }
            return frag;
        }

        public override int GetItemPosition(Java.Lang.Object @object)
        {
            return PositionNone;
        }

        public override ICharSequence GetPageTitleFormatted(int position)
        {
            ICharSequence title;
            switch (position)
            {
                case 0:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.account_tab));
                    break;
                case 1:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.searches_tab));
                    break;
                case 2:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.transfer_tab));
                    break;
                case 3:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_tab));
                    break;
                default:
                    throw new System.Exception("Invalid Tab");
            }
            return title;
        }
    }

    public class PageFragment : Fragment
    {
        private static System.String ARG_PAGE_NUMBER = "page_number";

        public static PageFragment newInstance(int page)
        {
            PageFragment fragment = new PageFragment();
            Bundle args = new Bundle();
            args.PutInt(ARG_PAGE_NUMBER, page);
            fragment.Arguments = args;
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container,
                                 Bundle savedInstanceState)
        {
            int position = Arguments.GetInt(ARG_PAGE_NUMBER);
            int resId = int.MinValue;
            //switch (position)
            //{
            //    case 0:
            resId = Resource.Layout.login;
            //        break;
            //    case 1:
            //        resId = Resource.Layout.searches;
            //        break;
            //    case 2:
            //        resId = Resource.Layout.transfers;
            //        break;
            //    default:
            //        throw new System.Exception("Invalid Position");
            //}
            View rootView = inflater.Inflate(Resource.Layout.login, container, false);
            var txt = rootView.FindViewById<TextView>(Resource.Id.textView);
            txt.Text = "pos " + position;
            return rootView;
        }
    }




    public class SearchResultComparableWishlist : SearchResultComparable
    {
        public SearchResultComparableWishlist(SearchResultSorting _searchResultSorting) : base(_searchResultSorting)
        {

        }

        public override int Compare(SearchResponse x, SearchResponse y)
        {
            if (x.Username == y.Username)
            {
                if ((x.FileCount == y.FileCount) && (x.LockedFileCount == y.LockedFileCount))
                {
                    if (x.FileCount != 0 && (x.Files.First().Filename == y.Files.First().Filename))
                    {
                        return 0;
                    }
                    if (x.LockedFileCount != 0 && (x.LockedFiles.First().Filename == y.LockedFiles.First().Filename))
                    {
                        return 0;
                    }
                }
            }
            return base.Compare(x, y); //the actual comparison for which is "better"
        }

        //public override int GetHashCode()
        //{
        //    return base.GetHashCode();
        //}
    }

    public class SearchResultComparable : IComparer<SearchResponse>
    {
        private readonly SearchResultSorting searchResultSorting;
        public SearchResultComparable(SearchResultSorting _searchResultSorting)
        {
            searchResultSorting = _searchResultSorting;
        }

        private int CompareByAvailable(SearchResponse x, SearchResponse y)
        {
            //highest precedence. locked files.
            //so if any of the search responses have 0 unlocked files, they are considered the worst.
            if ((x.FileCount != 0 && y.FileCount == 0) || (x.FileCount == 0 && y.FileCount != 0))
            {
                if (y.FileCount == 0)
                {
                    //x is better
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            //next highest - free upload slots. for now just they are free or not.
            if ((x.FreeUploadSlots == 0 && y.FreeUploadSlots != 0) || (x.FreeUploadSlots != 0 && y.FreeUploadSlots == 0))
            {
                if (x.FreeUploadSlots == 0)
                {
                    //x is worse
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            //next highest - queue length
            if (x.QueueLength != y.QueueLength)
            {
                if (x.QueueLength > y.QueueLength)
                {
                    //x is worse
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            //next speed (MOST should fall here, from my testing at least).
            if (x.UploadSpeed != y.UploadSpeed)
            {
                if (x.UploadSpeed > y.UploadSpeed)
                {
                    //x is better
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            //VERY FEW, should go here
            if (x.Files.Count != 0 && y.Files.Count != 0)
            {
                return x.Files.First().Filename.CompareTo(y.Files.First().Filename);
            }
            if (x.LockedFiles.Count != 0 && y.LockedFiles.Count != 0)
            {
                return x.LockedFiles.First().Filename.CompareTo(y.LockedFiles.First().Filename);
            }
            return 0;
        }

        public virtual int Compare(SearchResponse x, SearchResponse y)
        {
            if (searchResultSorting == SearchResultSorting.Available)
            {
                return CompareByAvailable(x, y);
            }
            else if (searchResultSorting == SearchResultSorting.Fastest)
            {
                //for fastest, only speed matters. if they pick this then even locked files are in the running.
                if (x.UploadSpeed != y.UploadSpeed)
                {
                    if (x.UploadSpeed > y.UploadSpeed)
                    {
                        //x is better
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                if (x.Files.Count != 0 && y.Files.Count != 0)
                {
                    return x.Files.First().Filename.CompareTo(y.Files.First().Filename);
                }
                if (x.LockedFiles.Count != 0 && y.LockedFiles.Count != 0)
                {
                    return x.LockedFiles.First().Filename.CompareTo(y.LockedFiles.First().Filename);
                }
                return 0;
            }
            else if (searchResultSorting == SearchResultSorting.BitRate)
            {
                //for fastest, only speed matters. if they pick this then even locked files are in the running.
                x.GetDominantFileType(SoulSeekState.HideLockedResultsInSearch, out double xbitRate);
                y.GetDominantFileType(SoulSeekState.HideLockedResultsInSearch, out double ybitRate);
                if (xbitRate != ybitRate)
                {
                    if (xbitRate > ybitRate)
                    {
                        //x is better
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                // known issue (though more an an issue with the GetDominantFileType call) is when
                // the first 2 files are mp3 - no info, and later files are mp3 (320).
                // then it is considered mp3 - no info.
                // if someone sends a flac without length, bitrate, or sample rate info, then 
                // we treat that as no info and its at the bottom of the sort. I can see this 
                // being user unfriendly or counterintuitive.

                return CompareByAvailable(x, y);
            }
            else if (searchResultSorting == SearchResultSorting.FolderAlphabetical)
            {
                string xFolder = null;
                string yFolder = null;
                if (x.Files.Count != 0)
                {
                    xFolder = CommonHelpers.GetFolderNameFromFile(x.Files.First().Filename);
                }
                else if (x.LockedFiles.Count != 0)
                {
                    xFolder = CommonHelpers.GetFolderNameFromFile(x.LockedFiles.First().Filename);
                }

                if (y.Files.Count != 0)
                {
                    yFolder = CommonHelpers.GetFolderNameFromFile(y.Files.First().Filename);
                }
                else if (y.LockedFiles.Count != 0)
                {
                    yFolder = CommonHelpers.GetFolderNameFromFile(y.LockedFiles.First().Filename);
                }

                if (xFolder != null && yFolder != null)
                {
                    int ret = xFolder.CompareTo(yFolder);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }
                else
                {
                    // should not happen
                }

                //if its a tie (which is probably pretty common)
                //both username and foldername cant be same, so we are safe doing this..
                int userRet = x.Username.CompareTo(y.Username);
                if (userRet != 0)
                {
                    return userRet;
                }
                return 0;
            }
            else
            {
                throw new System.Exception("Unknown sorting algorithm");
            }
        }
    }

    public class FilterSpecialFlags
    {
        public bool ContainsSpecialFlags = false;
        public int MinFoldersInFile = 0;
        public int MinFileSizeMB = 0;
        public int MinBitRateKBS = 0;
        public bool IsVBR = false;
        public bool IsCBR = false;
        public void Clear()
        {
            ContainsSpecialFlags = false;
            MinFoldersInFile = 0;
            MinFileSizeMB = 0;
            MinBitRateKBS = 0;
            IsVBR = false;
            IsCBR = false;
        }
    }

    [Serializable]
    public class SavedStateSearchTabHeader
    {
        string LastSearchTerm;
        long LastRanTime;
        int LastSearchResultsCount;

        /// <summary>
        /// Get what you need to display the tab (i.e. result count, term, last ran)
        /// </summary>
        /// <param name="searchTab"></param>
        /// <returns></returns>
        public static SavedStateSearchTabHeader GetSavedStateHeaderFromTab(SearchTab searchTab)
        {
            SavedStateSearchTabHeader searchTabState = new SavedStateSearchTabHeader();
            searchTabState.LastSearchResultsCount = searchTab.LastSearchResultsCount;
            searchTabState.LastSearchTerm = searchTab.LastSearchTerm;
            searchTabState.LastRanTime = searchTab.LastRanTime.Ticks;
            return searchTabState;
        }

        /// <summary>
        /// these by definition will always be wishlist tabs...
        /// this restores the wishlist tabs, optionally with the search results, otherwise they will be added later.
        /// </summary>
        /// <param name="savedState"></param>
        /// <returns></returns>
        public static SearchTab GetTabFromSavedState(SavedStateSearchTabHeader savedStateHeader, List<SearchResponse> responses)
        {
            SearchTab searchTab = new SearchTab();
            searchTab.SearchResponses = responses;
            searchTab.LastSearchTerm = savedStateHeader.LastSearchTerm;
            searchTab.LastRanTime = new DateTime(savedStateHeader.LastRanTime);
            searchTab.SearchTarget = SearchTarget.Wishlist;
            searchTab.LastSearchResultsCount = responses != null ? responses.Count : savedStateHeader.LastSearchResultsCount;
            if (SearchFragment.FilterSticky)
            {
                searchTab.FilterSticky = SearchFragment.FilterSticky;
                searchTab.FilterString = SearchFragment.FilterStickyString;
                SearchFragment.ParseFilterString(searchTab);
            }
            searchTab.SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(searchTab.SortHelperSorting));
            if (responses != null)
            {
                foreach (SearchResponse resp in searchTab.SearchResponses)
                {
                    if (!searchTab.SortHelper.ContainsKey(resp))
                    {
                        //bool isItActuallyNotThere = true;
                        //foreach(var key in searchTab.SortHelper.Keys)
                        //{
                        //    if (key.Username == resp.Username)
                        //    {
                        //        if ((key.FileCount == resp.FileCount) && (key.LockedFileCount == resp.LockedFileCount))
                        //        {
                        //            if (key.FileCount != 0 && (key.Files.First().Filename == resp.Files.First().Filename))
                        //            {
                        //                isItActuallyNotThere = false;
                        //            }
                        //            if (key.LockedFileCount != 0 && (key.LockedFiles.First().Filename == resp.LockedFiles.First().Filename))
                        //            {
                        //                isItActuallyNotThere = false;
                        //            }
                        //        }
                        //    }
                        //}

                        searchTab.SortHelper.Add(resp, null);
                    }
                    else
                    {

                    }
                }
            }
            return searchTab;

        }
    }

    [Serializable]
    public class SavedStateSearchTab
    {
        //there are all of the things we must save in order to later restore a SearchTab
        public List<SearchResponse> searchResponses;
        public string LastSearchTerm;
        public long LastRanTime;
        public static SavedStateSearchTab GetSavedStateFromTab(SearchTab searchTab)
        {
            SavedStateSearchTab searchTabState = new SavedStateSearchTab();
            searchTabState.searchResponses = searchTab.SearchResponses.ToList();
            searchTabState.LastSearchTerm = searchTab.LastSearchTerm;
            searchTabState.LastRanTime = searchTab.LastRanTime.Ticks;
            return searchTabState;
        }

        /// <summary>
        /// these by definition will always be wishlist tabs...
        /// </summary>
        /// <param name="savedState"></param>
        /// <returns></returns>
        public static SearchTab GetTabFromSavedState(SavedStateSearchTab savedState, bool searchResponsesOnly = false, SearchTab oldTab = null)
        {
            SearchTab searchTab = new SearchTab();
            searchTab.SearchResponses = savedState.searchResponses;
            if (!searchResponsesOnly)
            {
                searchTab.LastSearchTerm = savedState.LastSearchTerm;
                searchTab.LastRanTime = new DateTime(savedState.LastRanTime);
            }
            else
            {
                searchTab.LastSearchTerm = oldTab.LastSearchTerm;
                searchTab.LastRanTime = oldTab.LastRanTime;
            }
            searchTab.SearchTarget = SearchTarget.Wishlist;
            searchTab.LastSearchResultsCount = searchTab.SearchResponses.Count;
            if (SearchFragment.FilterSticky)
            {
                searchTab.FilterSticky = SearchFragment.FilterSticky;
                searchTab.FilterString = SearchFragment.FilterStickyString;
                SearchFragment.ParseFilterString(searchTab);
            }
            searchTab.SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(searchTab.SortHelperSorting));
            foreach (SearchResponse resp in searchTab.SearchResponses)
            {
                if (!searchTab.SortHelper.ContainsKey(resp))
                {
                    //bool isItActuallyNotThere = true;
                    //foreach(var key in searchTab.SortHelper.Keys)
                    //{
                    //    if (key.Username == resp.Username)
                    //    {
                    //        if ((key.FileCount == resp.FileCount) && (key.LockedFileCount == resp.LockedFileCount))
                    //        {
                    //            if (key.FileCount != 0 && (key.Files.First().Filename == resp.Files.First().Filename))
                    //            {
                    //                isItActuallyNotThere = false;
                    //            }
                    //            if (key.LockedFileCount != 0 && (key.LockedFiles.First().Filename == resp.LockedFiles.First().Filename))
                    //            {
                    //                isItActuallyNotThere = false;
                    //            }
                    //        }
                    //    }
                    //}

                    searchTab.SortHelper.Add(resp, null);
                }
                else
                {

                }
            }
            return searchTab;
        }
    }


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
                SearchTabHelper.RemoveTabFromSharedPrefs(tabToRemove, SoulSeekState.ActiveActivityRef);
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


    public class SearchTabDialog : AndroidX.Fragment.App.DialogFragment, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private RecyclerView recyclerViewSearches = null;
        private RecyclerView recyclerViewWishlists = null;

        private LinearLayoutManager recycleSearchesLayoutManager = null;
        private LinearLayoutManager recycleWishlistsLayoutManager = null;

        public SearchTabItemRecyclerAdapter recycleSearchesAdapter = null;
        public SearchTabItemRecyclerAdapter recycleWishesAdapter = null;

        private Button newSearch = null;
        private Button newWishlist = null;

        private TextView wishlistTitle = null;

        public static SearchTabDialog Instance = null;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.search_tab_layout, container); //error inflating MaterialButton
        }

        public void OnGlobalLayout()
        {
            ////onresume the dialog isnt drawn so you dont know the height.
            ////this is to give the dialog a max height of 95%
            //Window window = Dialog.Window;
            //this.View.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
            ////if (Build.VERSION.SDK_INT < Build.VERSION_CODES.JELLY_BEAN)
            ////{
            ////    yourView.getViewTreeObserver().removeGlobalOnLayoutListener(this);
            ////}
            ////else
            ////{
            ////    yourView.getViewTreeObserver().removeOnGlobalLayoutListener(this);
            ////}
            //Point size = new Point();

            //Display display = window.WindowManager.DefaultDisplay;
            //display.GetSize(size);

            //int width = size.X;
            //int height = size.Y;

            //if(this.View.Height > (height * .95))
            //{
            //    window.SetLayout((int)(width * 0.90), (int)(height * 0.95));//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            //    window.SetGravity(GravityFlags.Center);
            //}

        }

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            Instance = this;
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            //Dialog.SetTitle("File Info"); //is this needed in any way??
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));
            this.SetStyle((int)Android.App.DialogFragmentStyle.NoTitle, 0);
            //this.Dialog.SetTitle("Search Tab");
            recyclerViewSearches = view.FindViewById<RecyclerView>(Resource.Id.searchesRecyclerView);
            recyclerViewSearches.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recyclerViewWishlists = view.FindViewById<RecyclerView>(Resource.Id.wishlistsRecyclerView);
            recyclerViewWishlists.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleSearchesLayoutManager = new LinearLayoutManager(this.Activity);
            recyclerViewSearches.SetLayoutManager(recycleSearchesLayoutManager);
            recycleWishlistsLayoutManager = new LinearLayoutManager(this.Activity);
            recyclerViewWishlists.SetLayoutManager(recycleWishlistsLayoutManager);
            recycleSearchesAdapter = new SearchTabItemRecyclerAdapter(GetSearchTabIds());
            var wishTabIds = GetWishesTabIds();
            recycleWishesAdapter = new SearchTabItemRecyclerAdapter(wishTabIds);
            recycleWishesAdapter.ForWishlist = true;

            wishlistTitle = view.FindViewById<TextView>(Resource.Id.wishlistTitle);
            if (wishTabIds.Count == 0)
            {
                wishlistTitle.SetText(Resource.String.wishlist_empty_bold);
            }
            else
            {
                wishlistTitle.SetText(Resource.String.wishlist_bold);
            }
            recyclerViewSearches.SetAdapter(recycleSearchesAdapter);
            recyclerViewWishlists.SetAdapter(recycleWishesAdapter);
            newSearch = view.FindViewById<Button>(Resource.Id.createNewSearch);
            newSearch.Click += NewSearch_Click;
            //newSearch.CompoundDrawablePadding = 6;
            Android.Graphics.Drawables.Drawable drawable = null;
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                drawable = this.Context.Resources.GetDrawable(Resource.Drawable.ic_add_black_24dp, this.Context.Theme);
            }
            else
            {
                drawable = this.Context.Resources.GetDrawable(Resource.Drawable.ic_add_black_24dp);
            }
            newSearch.SetCompoundDrawablesWithIntrinsicBounds(drawable, null, null, null);

        }

        //private void NewWishlist_Click(object sender, EventArgs e)
        //{
        //    SearchTabHelper.AddWishlistSearchTab();
        //}

        private void NewSearch_Click(object sender, EventArgs e)
        {
            int tabID = SearchTabHelper.AddSearchTab();
            SearchFragment.Instance.GoToTab(tabID, false);
            SearchTabDialog.Instance.Dismiss();
            SearchFragment.Instance.SetCustomViewTabNumberImageViewState();
        }

        public override void OnResume()
        {
            base.OnResume();

            MainActivity.LogDebug("OnResume ran");
            //this.View.ViewTreeObserver.AddOnGlobalLayoutListener(this);
            //Window window = Dialog.Window;//  getDialog().getWindow();

            //int currentWindowHeight = window.DecorView.Height;
            //int currentWindowWidth = window.DecorView.Width;

            //int xxx = this.View.RootView.Width;
            //int xxxxx = this.View.Width;

            Point size = new Point();
            Window window = Dialog.Window;
            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;
            //int height = size.Y;

            window.SetLayout((int)(width * 0.90), Android.Views.WindowManagerLayoutParams.WrapContent);//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
            MainActivity.LogDebug("OnResume End");
        }

        private List<int> GetSearchTabIds()
        {
            var listOFIds = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget != SearchTarget.Wishlist).Select((pair1) => pair1.Key).ToList();
            listOFIds.Sort();
            return listOFIds;
        }

        public static List<int> GetWishesTabIds()
        {
            var listOFIds = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget == SearchTarget.Wishlist).Select((pair1) => pair1.Key).ToList();
            listOFIds.Sort();
            listOFIds.Reverse();
            return listOFIds;
        }
    }

    //    private void Search_Click(object sender, EventArgs e)
    //    {
    //        MainActivity.LogDebug("Search_Click");
    //        if (!SoulSeekState.currentlyLoggedIn)
    //        {
    //            Toast tst = Toast.MakeText(Context, "Must log in before searching", ToastLength.Long);
    //            tst.Show();
    //        }
    //        else if (MainActivity.CurrentlyLoggedInButDisconnectedState())
    //        {
    //            Task t;
    //            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
    //            {
    //                return;
    //            }
    //            t.ContinueWith(new Action<Task>((Task t) =>
    //            {
    //                if (t.IsFaulted)
    //                {
    //                    SoulSeekState.MainActivityRef.RunOnUiThread(() => { 

    //                        Toast.MakeText(SoulSeekState.MainActivityRef, "Failed to connect.", ToastLength.Short).Show(); 

    //                        });
    //                    return;
    //                }
    //                SoulSeekState.MainActivityRef.RunOnUiThread(SearchLogic);

    //            }));
    //        }
    //        else
    //        {

    //            SearchLogic();
    //        }
    //    }
    //}

    //public class SearchAdapter : ArrayAdapter<SearchResponse>
    //{
    //    List<int> oppositePositions = new List<int>();
    //    public SearchAdapter(Context c, List<SearchResponse> items) : base(c, 0, items)
    //    {
    //        oppositePositions = new List<int>();
    //    }

    //    public override View GetView(int position, View convertView, ViewGroup parent)
    //    {
    //        ISearchItemViewBase itemView = (ISearchItemViewBase)convertView;
    //        if (null == itemView)
    //        {
    //            switch (SearchFragment.SearchResultStyle)
    //            {
    //                case SearchResultStyleEnum.ExpandedAll:
    //                case SearchResultStyleEnum.CollapsedAll:
    //                    itemView = SearchItemViewExpandable.inflate(parent);
    //                    (itemView as View).FindViewById<ImageView>(Resource.Id.expandableClick).Click += CustomAdapter_Click;
    //                    (itemView as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1;
    //                    break;
    //                case SearchResultStyleEnum.Medium:
    //                    itemView = SearchItemViewMedium.inflate(parent);
    //                    break;
    //                case SearchResultStyleEnum.Minimal:
    //                    itemView = SearchItemViewMinimal.inflate(parent);
    //                    break;
    //            }
    //        }
    //        bool opposite = oppositePositions.Contains(position);
    //        itemView.setItem(GetItem(position), opposite); //this will do the right thing no matter what...


    //        //if(SearchFragment.SearchResultStyle==SearchResultStyleEnum.CollapsedAll)
    //        //{
    //        //    (itemView as IExpandable).Collapse();
    //        //}
    //        //else if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.ExpandedAll)
    //        //{
    //        //    (itemView as IExpandable).Expand();
    //        //}

    //        //SETTING TOOLTIPTEXT does not allow list view item click!!! 
    //        //itemView.TooltipText = "Queue Length: " + GetItem(position).QueueLength + System.Environment.NewLine + "Free Upload Slots: " + GetItem(position).FreeUploadSlots;
    //        return itemView as View;
    //        //return base.GetView(position, convertView, parent);
    //    }

    //    private void CustomAdapter_Click1(object sender, EventArgs e)
    //    {
    //        MainActivity.LogInfoFirebase("CustomAdapter_Click1");
    //        int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
    //        SearchFragment.Instance.showEditDialog(position);
    //    }

    //    private void CustomAdapter_Click(object sender, EventArgs e)
    //    {
    //        //throw new NotImplementedException();
    //        int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
    //        var v = ((sender as View).Parent.Parent as View).FindViewById<View>(Resource.Id.detailsExpandable);
    //        var img = ((sender as View).Parent.Parent as View).FindViewById<ImageView>(Resource.Id.expandableClick);
    //        if (v.Visibility == ViewStates.Gone)
    //        {
    //            img.Animate().RotationBy((float)(180.0)).SetDuration(350).Start();
    //            v.Visibility = ViewStates.Visible;
    //            SearchItemViewExpandable.PopulateFilesListView(v as LinearLayout, GetItem(position));
    //            if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
    //            {
    //                oppositePositions.Add(position);
    //                oppositePositions.Sort();
    //            }
    //            else
    //            {
    //                oppositePositions.Remove(position);
    //            }
    //        }
    //        else
    //        {
    //            img.Animate().RotationBy((float)(-180.0)).SetDuration(350).Start();
    //            v.Visibility = ViewStates.Gone;
    //            if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
    //            {
    //                oppositePositions.Remove(position);
    //            }
    //            else
    //            {
    //                oppositePositions.Add(position);
    //                oppositePositions.Sort();
    //            }
    //        }
    //    }
    //}

    public interface ISearchItemViewBase
    {
        void setupChildren();
        SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }
        void setItem(SearchResponse item, int opposite);
    }

    public class SearchItemViewMinimal : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        //private TextView viewQueue;
        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }

        public SearchItemViewMinimal(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.test_row, this, true);
            setupChildren();
        }
        public SearchItemViewMinimal(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.test_row, this, true);
            setupChildren();
        }

        public static SearchItemViewMinimal inflate(ViewGroup parent)
        {
            SearchItemViewMinimal itemView = (SearchItemViewMinimal)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewminimal_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textView1);
            viewFoldername = FindViewById<TextView>(Resource.Id.textView2);
            viewSpeed = FindViewById<TextView>(Resource.Id.textView3);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = CommonHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString(); //kb/s

            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }
    }

    public class SearchItemViewMedium : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewQueue;
        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }
        public SearchItemViewMedium(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium, this, true);
            setupChildren();
        }
        public SearchItemViewMedium(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium, this, true);
            setupChildren();
        }

        public static SearchItemViewMedium inflate(ViewGroup parent)
        {
            SearchItemViewMedium itemView = (SearchItemViewMedium)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmedium_dummy, parent, false);
            return itemView;
        }
        private bool hideLocked = false;
        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
            hideLocked = SoulSeekState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = CommonHelpers.GetFolderNameForSearchResult(item); //todo maybe also cache this...
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + SlskHelp.CommonHelpers.STRINGS_KBS; //kbs
            viewFileType.Text = item.GetDominantFileType(hideLocked, out _);
            if (item.FreeUploadSlots > 0)
            {
                viewQueue.Text = "";
            }
            else
            {
                viewQueue.Text = item.QueueLength.ToString();
            }
            //line separated..
            //viewUsername.Text = item.Username + "  |  " + Helpers.GetDominantFileType(item) + "  |  " + (item.UploadSpeed / 1024).ToString() + "kbs";

        }


    }

    public interface IExpandable
    {
        void Expand();
        void Collapse();
    }

    public class ExpandableSearchItemFilesAdapter : ArrayAdapter<Soulseek.File>
    {
        public ExpandableSearchItemFilesAdapter(Context c, List<Soulseek.File> files) : base(c, 0, files.ToArray())
        {

        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView itemView = (TextView)convertView;
            if (null == itemView)
            {
                itemView = new TextView(this.Context);//ItemView.inflate(parent);
            }
            itemView.Text = GetItem(position).Filename;
            return itemView;
        }
    }

    public class SearchItemViewExpandable : RelativeLayout, ISearchItemViewBase, IExpandable
    {
        private TextView viewQueue;
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private ImageView imageViewExpandable;
        private LinearLayout viewToHideShow;

        public SearchFragment.SearchAdapterRecyclerVersion AdapterRef;


        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }

        //private TextView viewQueue;
        public SearchItemViewExpandable(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable, this, true);
            setupChildren();
        }
        public SearchItemViewExpandable(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable, this, true);
            setupChildren();
        }

        public static SearchItemViewExpandable inflate(ViewGroup parent)
        {
            SearchItemViewExpandable itemView = (SearchItemViewExpandable)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_result_exampandable_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewToHideShow = FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
            imageViewExpandable = FindViewById<ImageView>(Resource.Id.expandableClick);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
            hideLocked = SoulSeekState.HideLockedResultsInSearch;
        }
        private bool hideLocked = false;
        public static void PopulateFilesListView(LinearLayout viewToHideShow, SearchResponse item)
        {
            viewToHideShow.RemoveAllViews();
            foreach (Soulseek.File f in item.GetFiles(SoulSeekState.HideLockedResultsInSearch))
            {
                TextView tv = new TextView(SoulSeekState.MainActivityRef);
                SetTextColor(tv, SoulSeekState.MainActivityRef);
                tv.Text = CommonHelpers.GetFileNameFromFile(f.Filename);
                viewToHideShow.AddView(tv);
            }
        }

        public void setItem(SearchResponse item, int position)
        {
            bool opposite = this.AdapterRef.oppositePositions.Contains(position);
            viewUsername.Text = item.Username;
            viewFoldername.Text = CommonHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + "kbs"; //kb/s
            if (item.FreeUploadSlots > 0)
            {
                viewQueue.Text = "";
            }
            else
            {
                viewQueue.Text = item.QueueLength.ToString();
            }
            viewFileType.Text = item.GetDominantFileType(hideLocked, out _);

            if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll && opposite ||
                SearchFragment.SearchResultStyle == SearchResultStyleEnum.ExpandedAll && !opposite)
            {
                viewToHideShow.Visibility = ViewStates.Visible;
                PopulateFilesListView(viewToHideShow, item);
                //imageViewExpandable.ClearAnimation();
                imageViewExpandable.Rotation = 0;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_less_white_32_dp);
                //viewToHideShow.Adapter = new ExpandableSearchItemFilesAdapter(this.Context,item.Files.ToList());
            }
            else
            {
                viewToHideShow.Visibility = ViewStates.Gone;
                imageViewExpandable.Rotation = 0;
                //imageViewExpandable.ClearAnimation(); //THIS DOES NOT CLEAR THE ROTATE.
                //AFTER doing a rotation animation, the rotation is still there in the 
                //imageview state.  just check float rot = imageViewExpandable.Rotation;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_more_black_32dp);
            }
            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }

        public void Expand()
        {
            viewToHideShow.Visibility = ViewStates.Visible;
        }

        public void Collapse()
        {
            viewToHideShow.Visibility = ViewStates.Gone;
        }

        public static Color GetColorFromAttribute(Context c, int attr, Resources.Theme overrideTheme = null)
        {
            var typedValue = new TypedValue();
            if (overrideTheme != null)
            {
                overrideTheme.ResolveAttribute(attr, typedValue, true);
            }
            else
            {
                c.Theme.ResolveAttribute(attr, typedValue, true);
            }

            if (typedValue.ResourceId == 0)
            {
                return GetColorFromInteger(typedValue.Data);
            }
            else
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 23)
                {
                    return GetColorFromInteger(ContextCompat.GetColor(c, typedValue.ResourceId));
                }
                else
                {
                    return c.Resources.GetColor(typedValue.ResourceId);
                }
            }
        }

        public static Color GetColorFromInteger(int color)
        {
            return Color.Rgb(Color.GetRedComponent(color), Color.GetGreenComponent(color), Color.GetBlueComponent(color));
        }

        public static void SetTextColor(TextView tv, Context c)
        {
            tv.SetTextColor(GetColorFromAttribute(c, Resource.Attribute.cellTextColor));
        }
    }

    /**
    Notes on Queue Position:
    The default queue position should be int.MaxValue (which we display as not known) not 0.  
    This is the case on QT where we download from an offline user, 
      or in general when we are queued by a user that does not send a queue position (slskd?).
    Both QT and Nicotine display it as "Queued" and then without a queue position (rather than queue position of 0).

    If we are downloading from a user with queue and they then go offline, the QT behavior is to still show "Queued" (nothing changes),
      the nicotine behavior is to change it to "User Logged Off".  I think nicotine behavior is more descriptive and helpful.
    **/

    public interface ITransferItemView
    {
        public ITransferItem InnerTransferItem { get; set; }

        public void setupChildren();

        public void setItem(ITransferItem ti, bool isInBatchMode);

        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }

        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView();

        public TextView GetProgressSizeTextView();

        public bool GetShowProgressSize();

        public bool GetShowSpeed();
    }

    public class TransferItemViewFolder : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewCurrentFilename;
        private TextView viewNumRemaining;

        private TextView viewProgressSize;
        private TextView viewStatus; //In Queue, Failed, Done, In Progress
        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public TextView GetProgressSizeTextView()
        {
            return viewProgressSize;
        }

        public bool showSize;
        public bool showSpeed;

        public bool GetShowProgressSize()
        {
            return showSize;
        }

        public bool GetShowSpeed()
        {
            return showSpeed;
        }

        public TransferItemViewFolder(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder, this, true);
            }

            setupChildren();
        }
        public TransferItemViewFolder(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder, this, true);
            }

            setupChildren();
        }

        public static TransferItemViewFolder inflate(ViewGroup parent, bool _showSize, bool _showSpeed)
        {
            TransferItemViewFolder itemView = null;
            if (_showSize)
            {
                itemView = (TransferItemViewFolder)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_view_folder_dummy_showSizeProgress, parent, false);
            }
            else
            {
                itemView = (TransferItemViewFolder)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_view_folder_dummy, parent, false);
            }
            itemView.showSpeed = _showSpeed;
            itemView.showSize = _showSize;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFoldername = FindViewById<TextView>(Resource.Id.textViewFoldername);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);
            viewProgressSize = FindViewById<TextView>(Resource.Id.textViewProgressSize);

            viewStatus = FindViewById<TextView>(Resource.Id.textViewStatus);
            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);
            viewNumRemaining = FindViewById<TextView>(Resource.Id.filesRemaining);
            viewCurrentFilename = FindViewById<TextView>(Resource.Id.currentFile);
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu);
        }

        public void setItem(ITransferItem item, bool isInBatchMode)
        {
            InnerTransferItem = item;
            FolderItem folderItem = item as FolderItem;
            viewFoldername.Text = folderItem.GetDisplayFolderName();
            var state = folderItem.GetState(out bool isFailed, out _);


            TransferViewHelper.SetViewStatusText(viewStatus, state, item.IsUpload(), true);
            TransferViewHelper.SetAdditionalStatusText(viewStatusAdditionalInfo, item, state, true); //TODOTODO
            TransferViewHelper.SetAdditionalFolderInfoState(viewNumRemaining, viewCurrentFilename, folderItem, state);
            int prog = folderItem.GetFolderProgress(out long totalBytes, out _);
            progressBar.Progress = prog;
            if (this.showSize)
            {
                (viewProgressSize as TransfersFragment.ProgressSizeTextView).Progress = prog;
                TransferViewHelper.SetSizeText(viewProgressSize, prog, totalBytes);
            }


            viewUsername.Text = folderItem.Username;
            if (item.IsUpload() && state.HasFlag(TransferStates.Cancelled))
            {
                isFailed = true;
            }
            if (isFailed)//state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut))
            {
                progressBar.Progress = 100;
                if (this.showSize)
                {
                    (viewProgressSize as AndriodApp1.TransfersFragment.ProgressSizeTextView).Progress = 100;
                }
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.Red);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.Red, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            if (isInBatchMode && TransfersFragment.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition))
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
            else
            {
                //this.Background
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                                           //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Color.cellback);
                                           //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
        }
    }




    public class TransferViewHelper
    {
        /// <summary>
        /// In Progress = InProgress proper, initializing, requested. 
        /// If In Progress or Queued you should be able to pause it (the official client lets you).
        /// </summary>
        /// <param name="transferItems"></param>
        /// <param name="numInProgress"></param>
        /// <param name="numFailed"></param>
        /// <param name="numPaused"></param>
        /// <param name="numSucceeded"></param>
        public static void GetStatusNumbers(IEnumerable<TransferItem> transferItems, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued)
        {
            numInProgress = 0;
            numFailed = 0;
            numPaused = 0;
            numSucceeded = 0;
            numQueued = 0;
            lock (transferItems)
            {
                foreach (var ti in transferItems)
                {
                    if (ti.State.HasFlag(TransferStates.Queued))
                    {
                        numQueued++;
                    }
                    else if (ti.State.HasFlag(TransferStates.InProgress) || ti.State.HasFlag(TransferStates.Initializing) || ti.State.HasFlag(TransferStates.Requested) || ti.State.HasFlag(TransferStates.Aborted))
                    {
                        numInProgress++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Errored) || ti.State.HasFlag(TransferStates.Rejected) || ti.State.HasFlag(TransferStates.TimedOut))
                    {
                        numFailed++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Cancelled))
                    {
                        numPaused++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Succeeded))
                    {
                        numSucceeded++;
                    }
                }
            }
        }


        public static void SetAdditionalFolderInfoState(TextView filesLongStatus, TextView currentFile, FolderItem fi, TransferStates folderState)
        {
            //if in progress, X files remaining, Current File:
            //if in queue, ^ ^ (or initializing, requesting, basically if in progress in the literal sense)
            //if completed, X files suceeded - hide p2
            //if failed, X files suceeded (if applicable), X files failed. - hide p2
            //if paused, X files suceeded, X failed, X paused. - hide p2
            if (folderState.HasFlag(TransferStates.InProgress) || folderState.HasFlag(TransferStates.Queued) || folderState.HasFlag(TransferStates.Initializing) || folderState.HasFlag(TransferStates.Requested) || folderState.HasFlag(TransferStates.Aborted))
            {
                int numRemaining = 0;
                string currentFilename = string.Empty;
                int total = 0;
                lock (fi.TransferItems)
                {
                    foreach (var ti in fi.TransferItems)
                    {
                        total++;
                        if (!(ti.State.HasFlag(TransferStates.Completed)))
                        {
                            numRemaining++;
                        }
                        if (ti.State.HasFlag(TransferStates.InProgress))
                        {
                            currentFilename = ti.Filename;
                        }
                    }
                    if (currentFilename == string.Empty) //init or requested case
                    {
                        currentFilename = fi.TransferItems.First().Filename;
                    }
                }

                filesLongStatus.Text = string.Format(SeekerApplication.GetString(Resource.String.X_of_Y_Remaining), numRemaining, total);
                currentFile.Visibility = ViewStates.Visible;
                currentFile.Text = string.Format("Current: {0}", currentFilename);
            }
            else if (folderState.HasFlag(TransferStates.Succeeded))
            {
                int numSucceeded = fi.TransferItems.Count;

                filesLongStatus.Text = string.Format("{0} {1} {2}", SeekerApplication.GetString(Resource.String.all), numSucceeded, SeekerApplication.GetString(Resource.String.Succeeded).ToLower());
                currentFile.Visibility = ViewStates.Gone;
            }
            else if (folderState.HasFlag(TransferStates.Errored) || folderState.HasFlag(TransferStates.Rejected) || folderState.HasFlag(TransferStates.TimedOut))
            {
                int numFailed = 0;
                int numSucceeded = 0;
                int numPaused = 0;
                lock (fi.TransferItems)
                {

                    foreach (var ti in fi.TransferItems)
                    {
                        if (ti.State.HasFlag(TransferStates.Succeeded))
                        {
                            numSucceeded++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Errored) || ti.State.HasFlag(TransferStates.Rejected) || ti.State.HasFlag(TransferStates.TimedOut))
                        {
                            numFailed++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Cancelled))
                        {
                            numPaused++;
                        }
                    }
                }

                SetFilesLongStatusIfNotInProgress(filesLongStatus, fi, numFailed, numSucceeded, numPaused);
                currentFile.Visibility = ViewStates.Gone;
                //set views + visi
            }
            else if (folderState.HasFlag(TransferStates.Cancelled))
            {
                int numFailed = 0;
                int numSucceeded = 0;
                int numPaused = 0;
                lock (fi.TransferItems)
                {

                    foreach (var ti in fi.TransferItems)
                    {
                        if (ti.State.HasFlag(TransferStates.Succeeded))
                        {
                            numSucceeded++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Cancelled))
                        {
                            numPaused++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Errored))
                        {
                            numFailed++;
                        }
                    }
                }

                if (numPaused == 0)
                {
                    //error
                }

                SetFilesLongStatusIfNotInProgress(filesLongStatus, fi, numFailed, numSucceeded, numPaused);
                currentFile.Visibility = ViewStates.Gone;
                //set views + visi
            }
            else
            {
                //i.e. None, can be due to uploading 0 byte files. or for transfers that never got initialized.
                //     dont leave this as is bc it will display "3 files remaining and Current: filename..." always.
                currentFile.Visibility = ViewStates.Gone;
                filesLongStatus.Text = string.Format(SeekerApplication.GetString(Resource.String.Num_FilesRemaining), fi.TransferItems.Count);
            }

        }

        private static void SetFilesLongStatusIfNotInProgress(TextView filesLongStatus, FolderItem fi, int numFailed, int numSucceeded, int numPaused)
        {
            string failedString = SeekerApplication.GetString(Resource.String.failed).ToLower();
            string succeededString = SeekerApplication.GetString(Resource.String.Succeeded).ToLower();
            string AllString = SeekerApplication.GetString(Resource.String.all);
            string cancelledString = fi.IsUpload() ? SeekerApplication.GetString(Resource.String.Aborted).ToLower() : SeekerApplication.GetString(Resource.String.paused).ToLower();
            // 0 0 0 isnt one.
            if (numSucceeded == 0 && numFailed == 0 && numPaused != 0) //all paused
            {
                filesLongStatus.Text = string.Format(AllString + " {0} {1}", numPaused, cancelledString);
            }
            else if (numSucceeded == 0 && numFailed != 0 && numPaused == 0) //all failed
            {
                filesLongStatus.Text = string.Format(AllString + " {0} {1}", numFailed, failedString);
            }
            else if (numSucceeded == 0 && numFailed != 0 && numPaused != 0) //all failed or paused
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}", numPaused, cancelledString, numFailed, failedString);
            }
            else if (numSucceeded != 0 && numFailed == 0 && numPaused == 0) //all succeeded
            {
                filesLongStatus.Text = string.Format(AllString + " {0} {1}", numSucceeded, succeededString);
            }
            else if (numSucceeded != 0 && numFailed == 0 && numPaused != 0) //all succeeded or paused
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}", numPaused, cancelledString, numSucceeded, succeededString);
            }
            else if (numSucceeded != 0 && numFailed != 0 && numPaused == 0) //all succeeded or failed
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}", numFailed, failedString, numSucceeded, succeededString);
            }
            else //all
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} {3}, {4} {5}", numPaused, cancelledString, numSucceeded, succeededString, numFailed, failedString);
            }
        }



        public static void SetSizeText(TextView size, int progress, long sizeBytes)
        {
            if (progress == 100)
            {
                if (sizeBytes > 1024 * 1024)
                {
                    size.Text = System.String.Format("{0:F1}mb", sizeBytes / 1048576.0);
                }
                else if (sizeBytes >= 0)
                {
                    size.Text = System.String.Format("{0:F1}kb", sizeBytes / 1024.0);
                }
                else
                {
                    size.Text = "??";
                }
            }
            else
            {
                long bytesTransferred = progress * sizeBytes;
                if (sizeBytes > 1024 * 1024)
                {
                    size.Text = System.String.Format("{0:F1}/{1:F1}mb", bytesTransferred / (1048576.0 * 100.0), sizeBytes / 1048576.0);
                }
                else if (sizeBytes >= 0)
                {
                    size.Text = System.String.Format("{0:F1}/{1:F1}kb", bytesTransferred / (1024.0 * 100.0), sizeBytes / 1024.0);
                }
                else
                {
                    size.Text = "??";
                }
            }
        }


        public static void SetViewStatusText(TextView viewStatus, TransferStates state, bool isUpload, bool isFolder)
        {
            if (state.HasFlag(TransferStates.Queued))
            {
                viewStatus.SetText(Resource.String.in_queue);
            }
            else if (state.HasFlag(TransferStates.Cancelled))
            {
                if (isUpload)
                {
                    viewStatus.Text = SeekerApplication.GetString(Resource.String.Aborted);
                }
                else
                {
                    viewStatus.SetText(Resource.String.paused);
                }
            }
            else if (isFolder && state.HasFlag(TransferStates.Rejected)) //if is folder we put the extra info here, else we put it in the additional status TextView
            {
                if (isUpload)
                {
                    viewStatus.Text = System.String.Format("{0} - {1}", SeekerApplication.GetString(Resource.String.failed), SeekerApplication.GetString(Resource.String.Cancelled));//if the user on the other end cancelled / paused / removed it.
                }
                else
                {
                    viewStatus.SetText(Resource.String.failed_denied);
                }
            }
            else if (isFolder && state.HasFlag(TransferStates.UserOffline))
            {
                viewStatus.SetText(Resource.String.failed_user_offline);
            }
            else if (isFolder && state.HasFlag(TransferStates.CannotConnect))
            {
                viewStatus.Text = System.String.Format("{0} - {1}", SeekerApplication.GetString(Resource.String.failed), SeekerApplication.GetString(Resource.String.CannotConnect));
                //"cannot connect" is too long for average screen. but the root problem needs to be fixed (for folder combine two TextView into one with padding???? TODO)
            }
            else if (state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut) || state.HasFlag(TransferStates.Errored))
            {
                viewStatus.SetText(Resource.String.failed);
            }
            else if (state.HasFlag(TransferStates.Initializing) || state.HasFlag(TransferStates.Requested))  //item.State.HasFlag(TransferStates.None) captures EVERYTHING!!
            {
                viewStatus.SetText(Resource.String.not_started);
            }
            else if (state.HasFlag(TransferStates.InProgress))
            {
                viewStatus.SetText(Resource.String.in_progress);
            }
            else if (state.HasFlag(TransferStates.Succeeded))
            {
                viewStatus.SetText(Resource.String.completed);
            }
            else if (state.HasFlag(TransferStates.Aborted))
            {
                // this is the case that the filesize is wrong. In that case we always immediately re-request.
                viewStatus.SetText(Resource.String.re_requesting);
            }
            else
            {
                //these views are recycled, so NEVER dont set them.
                //otherwise they will be whatever the view they recycled was.
                //so they may end up being Failed, Completed, etc.
                viewStatus.Text = "None";
            }
        }


        public static string GetTimeRemainingString(TimeSpan? timeSpan)
        {
            if (timeSpan == null)
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.unknown);
            }
            else
            {
                string[] hms = timeSpan.ToString().Split(':');
                string h = hms[0].TrimStart('0');
                if (h == string.Empty)
                {
                    h = "0";
                }
                string m = hms[1].TrimStart('0');
                if (m == string.Empty)
                {
                    m = "0";
                }
                string s = hms[2].TrimStart('0');
                if (s.Contains('.'))
                {
                    s = s.Substring(0, s.IndexOf('.'));
                }
                if (s == string.Empty)
                {
                    s = "0";
                }
                //it will always be length 3.  if the seconds is more than a day it will be like "[13.21:53:20]" and if just 2 it will be like "[00:00:02]"
                if (h != "0")
                {
                    //we have hours
                    return h + "h:" + m + "m:" + s + "s";
                }
                else if (m != "0")
                {
                    return m + "m:" + s + "s";
                }
                else
                {
                    return s + "s";
                }
            }
        }

        public static void SetAdditionalStatusText(TextView viewStatusAdditionalInfo, ITransferItem item, TransferStates state, bool showSpeed)
        {
            if (state.HasFlag(TransferStates.InProgress))
            {
                //Helpers.GetTransferSpeedString(avgSpeedBytes);
                if (showSpeed)
                {
                    viewStatusAdditionalInfo.Text = CommonHelpers.GetTransferSpeedString(item.GetAvgSpeed()) + "  •  " + GetTimeRemainingString(item.GetRemainingTime());
                }
                else
                {
                    viewStatusAdditionalInfo.Text = GetTimeRemainingString(item.GetRemainingTime());
                }
            }
            else if (state.HasFlag(TransferStates.Queued) && !(item.IsUpload()))
            {
                int queueLen = item.GetQueueLength();
                if (queueLen == int.MaxValue) //i.e. unknown
                {
                    viewStatusAdditionalInfo.Text = string.Empty;
                }
                else
                {
                    viewStatusAdditionalInfo.Text = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.position_), queueLen.ToString());
                }
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.Rejected))
            {
                if (item.IsUpload())
                {
                    viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.Cancelled);
                }
                else
                {
                    viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.denied);
                }
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.TimedOut))
            {
                viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.TimedOut);
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.UserOffline))
            {
                viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.UserIsOffline);
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.CannotConnect))
            {
                viewStatusAdditionalInfo.Text = SeekerApplication.GetString(Resource.String.CannotConnect);
            }
            else
            {
                viewStatusAdditionalInfo.Text = "";
            }
        }
    }


    public class TransferItemViewDetails : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewFilename;

        private TextView viewStatus; //In Queue, Failed, Done, In Progress
        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.
        private TextView progressSize; //if in Queue then show position, if In Progress show time remaining.

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public TextView GetProgressSizeTextView()
        {
            return progressSize;
        }

        public bool GetShowProgressSize()
        {
            return showSizes;
        }
        public bool GetShowSpeed()
        {
            return showSpeed;
        }


        public bool showSpeed;
        public bool showSizes;
        public TransferItemViewDetails(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            }

            setupChildren();
        }
        public TransferItemViewDetails(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            }

            setupChildren();
        }

        public static TransferItemViewDetails inflate(ViewGroup parent, bool _showSizes, bool _showSpeed)
        {

            TransferItemViewDetails itemView = null;
            if (_showSizes)
            {
                itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy_showProgressSize, parent, false);
            }
            else
            {
                itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy, parent, false);
            }
            itemView.showSpeed = _showSpeed;
            itemView.showSizes = _showSizes;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFilename = FindViewById<TextView>(Resource.Id.textViewFileName);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);

            viewStatus = FindViewById<TextView>(Resource.Id.textViewStatus);
            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);

            progressSize = FindViewById<TextView>(Resource.Id.textViewProgressSize);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);

        }




        public void setItem(ITransferItem item, bool isInBatchMode)
        {
            InnerTransferItem = item;
            TransferItem ti = item as TransferItem;
            viewFilename.Text = ti.Filename;
            progressBar.Progress = ti.Progress;
            if (this.showSizes)
            {
                TransferViewHelper.SetSizeText(progressSize, ti.Progress, ti.Size);
            }
            TransferViewHelper.SetViewStatusText(viewStatus, ti.State, ti.IsUpload(), false);
            TransferViewHelper.SetAdditionalStatusText(viewStatusAdditionalInfo, ti, ti.State, this.showSpeed);
            viewUsername.Text = ti.Username;
            bool isFailedOrAborted = ti.Failed;
            if (item.IsUpload() && ti.State.HasFlag(TransferStates.Cancelled))
            {
                isFailedOrAborted = true;
            }
            if (isFailedOrAborted)
            {
                progressBar.Progress = 100;
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.Red);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.Red, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618

            }

            if (isInBatchMode && TransfersFragment.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition))
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
            else
            {
                //this.Background
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                                           //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Color.cellback);
                                           //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu);
            //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo) menuInfo;
            menu.Add(0, 0, 0, Resource.String.retry_dl);
            menu.Add(1, 1, 1, Resource.String.clear_from_list);
            menu.Add(2, 2, 2, Resource.String.cancel_and_clear);
        }
    }




    public class CustomLinearLayoutManager : LinearLayoutManager
    {
        public CustomLinearLayoutManager(Context c) : base(c)
        {

        }
        //Generate constructors

        public override bool SupportsPredictiveItemAnimations()
        {
            bool old = base.SupportsPredictiveItemAnimations();
            return false;
        }

    }




}
