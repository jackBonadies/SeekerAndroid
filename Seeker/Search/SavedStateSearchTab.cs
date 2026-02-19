using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Common;
using Seeker.Search;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    /// <summary>
    /// Saved info for the full search tab (i.e. all search responses)
    /// </summary>
    //TODO2026 - move to seperate file
    public class SearchTabUtil
    {
        /// <summary>
        /// these by definition will always be wishlist tabs...
        /// </summary>
        /// <param name="savedState"></param>
        /// <returns></returns>
        public static SearchTab GetTabFromSavedState(List<SearchResponse> searchResponses, SearchTab oldTab = null)
        {
            SearchTab searchTab = new SearchTab();
            searchTab.SearchResponses = searchResponses;
            searchTab.LastSearchTerm = oldTab.LastSearchTerm;
            searchTab.LastRanTime = oldTab.LastRanTime;
            searchTab.SearchTarget = SearchTarget.Wishlist;
            searchTab.LastSearchResultsCount = searchTab.SearchResponses.Count;
            if (PreferencesState.FilterSticky)
            {
                searchTab.FilterSticky = PreferencesState.FilterSticky;
                searchTab.TextFilter.Set(PreferencesState.FilterStickyString);
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
}