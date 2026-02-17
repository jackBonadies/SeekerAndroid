using Seeker.Search;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Common;
namespace Seeker
{
    public class SearchTab
    {
        public List<SearchResponse> SearchResponses = new List<SearchResponse>();
        public SortedDictionary<SearchResponse, object> SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparable(PreferencesState.DefaultSearchResultSortAlgorithm));
        public SearchResultSorting SortHelperSorting = PreferencesState.DefaultSearchResultSortAlgorithm;
        public object SortHelperLockObject = new object();
        public bool FilteredResults = false;
        public bool FilterSticky = false;
        public string FilterString = string.Empty;
        public List<string> WordsToAvoid = new List<string>();
        public List<string> WordsToInclude = new List<string>();
        public FilterSpecialFlags FilterSpecialFlags = new FilterSpecialFlags();
        public List<SearchResponse> UI_SearchResponses = new List<SearchResponse>();
        public SearchTarget SearchTarget = SearchTarget.AllUsers;
        public bool CurrentlySearching = false;
        public string SearchTargetChosenRoom = string.Empty;
        public string SearchTargetChosenUser = string.Empty;
        public int LastSearchResponseCount = -1; //this tell us how many we have filtered.  since we only filter when its the Current UI Tab.
        public CancellationTokenSource CancellationTokenSource = null;
        public DateTime LastRanTime = DateTime.MinValue;

        public string LastSearchTerm = string.Empty;
        public int LastSearchResultsCount = 0;

        public List<ChipDataItem> ChipDataItems;
        public ChipFilter ChipsFilter;

        public bool IsLoaded()
        {
            return this.SearchResponses != null;
        }


        public SearchTab Clone(bool forWishlist)
        {
            SearchTab clone = new SearchTab();
            clone.SearchResponses = this.SearchResponses.ToList();
            SortedDictionary<SearchResponse, object> cloned = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(clone.SortHelperSorting));
            //without lock, extremely easy to reproduce "collection was modified" exception if creating wishlist tab while searching.
            lock (this.SortHelperLockObject) //lock the sort helper we are copying from
            {
                foreach (var entry in SortHelper)
                {
                    if (!cloned.ContainsKey(entry.Key))
                    {
                        cloned.Add(entry.Key, entry.Value);
                    }
                }
            }
            clone.SortHelper = cloned;
            clone.FilteredResults = this.FilteredResults;
            clone.FilterSticky = this.FilterSticky;
            clone.FilterString = this.FilterString;
            clone.WordsToAvoid = this.WordsToAvoid.ToList();
            clone.WordsToInclude = this.WordsToInclude.ToList();
            clone.FilterSpecialFlags = this.FilterSpecialFlags;
            clone.UI_SearchResponses = this.UI_SearchResponses.ToList();
            clone.CurrentlySearching = this.CurrentlySearching;
            clone.SearchTarget = this.SearchTarget;
            clone.SearchTargetChosenRoom = this.SearchTargetChosenRoom;
            clone.SearchTargetChosenUser = this.SearchTargetChosenUser;
            clone.LastSearchResponseCount = this.LastSearchResponseCount;
            clone.LastSearchTerm = this.LastSearchTerm;
            clone.LastSearchResultsCount = this.LastSearchResultsCount;
            clone.LastRanTime = this.LastRanTime;
            clone.ChipDataItems = this.ChipDataItems;
            clone.ChipsFilter = this.ChipsFilter;
            return clone;
        }
    }
}