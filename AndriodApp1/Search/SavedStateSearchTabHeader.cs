using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace AndriodApp1
{
    /// <summary>
    /// Small struct containing saved info on search tab header (i.e. search term, num results, last searched)
    /// </summary>
    [Serializable]
    public class SavedStateSearchTabHeader : ISerializable
    {
        [JsonInclude]
        public string LastSearchTerm { get; private set; }

        [JsonInclude]
        public long LastRanTime { get; private set; }

        [JsonInclude]
        public int LastSearchResultsCount { get; private set; }

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

        public SavedStateSearchTabHeader()
        {

        }

        /// <summary>
        /// Used for binary serializer, since members switched to properties from fields.
        /// Otherwise, properties will not be written to (default values)
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SavedStateSearchTabHeader(SerializationInfo info,  StreamingContext context)
        {
            LastSearchTerm = info.GetString("LastSearchTerm");
            LastRanTime = info.GetInt64("LastRanTime");
            LastSearchResultsCount = info.GetInt32("LastSearchResultsCount");
        }

        /// <summary>
        /// Used for binary serializer
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("LastSearchTerm", LastSearchTerm); 
            info.AddValue("LastRanTime", LastRanTime); 
            info.AddValue("LastSearchResultsCount", LastSearchResultsCount); 
        }
    }
}