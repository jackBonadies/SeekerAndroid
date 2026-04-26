using Seeker;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Search;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Search
{
    public static class SearchUtil
    {
        public static SearchResponse CreateSearchResponseFromDirectory(SearchResponse originalSearchResponse, Soulseek.Directory newDirectory, bool hideLocked)
        {
            // normally files names are formatted as: "@@ynkmv\\Albums\\albumname (2012)\\02 - songname.mp3"
            // but when we get a directory response the files are just the end file names i.e. "02 - songname.mp3" (they cannot be referenced as such for download)
            // here we prepend the full dir name.
            // they also do not come with any attributes, so here we also augment with attributes.
            List<File> fullFilenameCollection = new List<File>();
            var originalFiles = originalSearchResponse.GetFiles(hideLocked);
            foreach (File f in newDirectory.Files)
            {
                string fullFilename = newDirectory.Name + "\\" + f.Filename;
                //if it existed in the old folder then we can get some extra attributes
                var attributes = GetAttributesForFile(originalFiles, fullFilename);
                fullFilenameCollection.Add(new File(f.Code, fullFilename, f.Size, f.Extension, attributes ?? f.Attributes, f.IsLatin1Decoded, newDirectory.DecodedViaLatin1));
            }
            return new SearchResponse(originalSearchResponse.Username, originalSearchResponse.Token, originalSearchResponse.HasFreeUploadSlot, originalSearchResponse.UploadSpeed, originalSearchResponse.QueueLength, fullFilenameCollection);
        }

        private static IReadOnlyCollection<FileAttribute>? GetAttributesForFile(IEnumerable<Soulseek.File> originalFiles, string filename)
        {
            foreach (File fullFileInfo in originalFiles)
            {
                if (filename == fullFileInfo.Filename)
                {
                    return fullFileInfo.Attributes;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Saved info for the full search tab (i.e. all search responses)
    /// </summary>
    public static class SearchTabUtil
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
                    searchTab.SortHelper.Add(resp, null);
                }
                else
                {

                }
            }
            searchTab.ChipDataItems = ChipsHelper.CalculateChipItems(searchResponses, oldTab.LastSearchTerm, PreferencesState.SmartFilterOptions, PreferencesState.HideLockedResultsInSearch);
            return searchTab;
        }
    }
}
