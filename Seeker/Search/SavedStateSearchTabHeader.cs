using Seeker.Search;
using Soulseek;
using System.Collections.Generic;

namespace Seeker
{
    /// <summary>
    /// helpers for SavedStateSearchTabHeader that depend on SearchTab/SearchFragment.
    /// </summary>
    public static class SavedStateSearchTabHeaderHelper
    {
        /// <summary>
        /// Get what you need to display the tab (i.e. result count, term, last ran)
        /// </summary>
        public static SavedStateSearchTabHeader GetSavedStateHeaderFromTab(SearchTab searchTab)
        {
            return SavedStateSearchTabHeader.GetSavedStateHeaderFromTab(
                searchTab.LastSearchTerm,
                searchTab.LastSearchResultsCount,
                searchTab.LastRanTime.Ticks);
        }

        /// <summary>
        /// these by definition will always be wishlist tabs...
        /// this restores the wishlist tabs, optionally with the search results, otherwise they will be added later.
        /// </summary>
        public static SearchTab GetTabFromSavedState(SavedStateSearchTabHeader savedStateHeader, List<SearchResponse> responses)
        {
            SearchTab searchTab = new SearchTab();
            searchTab.SearchResponses = responses;
            searchTab.LastSearchTerm = savedStateHeader.LastSearchTerm;
            searchTab.LastRanTime = new System.DateTime(savedStateHeader.LastRanTime);
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
}
