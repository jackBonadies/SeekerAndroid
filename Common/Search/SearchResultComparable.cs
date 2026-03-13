using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common;
using Seeker.Extensions.SearchResponseExtensions;
namespace Seeker.Search
{

    public class SearchResultComparableWishlist : SearchResultComparable
    {
        public SearchResultComparableWishlist(SearchResultSorting _searchResultSorting) : base(_searchResultSorting)
        {

        }

        // discard if responses are essentially the same (i.e. the users upload speed / queue length maybe have changed (and thats okay)
        //   but if its the same user with the same files -> its the same)
        private bool areResponsesEssentiallyTheSame(SearchResponse x, SearchResponse y)
        {
            if (x.Username == y.Username && x.FileCount == y.FileCount && x.LockedFileCount == y.LockedFileCount)
            {
                if (x.FileCount != 0 && x.Files.First().Filename == y.Files.First().Filename)
                {
                    return true;
                }
                if (x.LockedFileCount != 0 && x.LockedFiles.First().Filename == y.LockedFiles.First().Filename)
                {
                    return true;
                }
            }
            return false;
        }

        public override int Compare(SearchResponse x, SearchResponse y)
        {
            if (areResponsesEssentiallyTheSame(x, y))
            {
                return 0;
            }
            return base.Compare(x, y); //the actual comparison for which is "better"
        }
    }


    public class SearchResultComparable : IComparer<SearchResponse>
    {
        private readonly SearchResultSorting searchResultSorting;
        public SearchResultComparable(SearchResultSorting _searchResultSorting)
        {
            searchResultSorting = _searchResultSorting;
        }

        private static int CompareBySpeed(SearchResponse x, SearchResponse y)
        {
            // descending — faster is better
            return -x.UploadSpeed.CompareTo(y.UploadSpeed);
        }

        private static int CompareByFiles(SearchResponse x, SearchResponse y)
        {
            if (x.Files.Count != 0 && y.Files.Count != 0)
            {
                return x.Files.First().Filename.CompareTo(y.Files.First().Filename);
            }
            if (x.LockedFiles.Count != 0 && y.LockedFiles.Count != 0)
            {
                return x.LockedFiles.First().Filename.CompareTo(y.LockedFiles.First().Filename);
            }
            if (x.Files.Count != 0 && y.LockedFiles.Count != 0)
            {
                return x.Files.First().Filename.CompareTo(y.LockedFiles.First().Filename);
            }
            if (x.LockedFiles.Count != 0 && y.Files.Count != 0)
            {
                return x.LockedFiles.First().Filename.CompareTo(y.Files.First().Filename);
            }
            return 0;
        }

        private int CompareByAvailable(SearchResponse x, SearchResponse y)
        {
            int cmp;

            // having unlocked files beats locked-only (descending)
            cmp = (x.FileCount != 0).CompareTo(y.FileCount != 0);
            if (cmp != 0) return -cmp;

            // free upload slot (descending)
            cmp = x.HasFreeUploadSlot.CompareTo(y.HasFreeUploadSlot);
            if (cmp != 0) return -cmp;

            // queue length (ascending — shorter is better)
            cmp = x.QueueLength.CompareTo(y.QueueLength);
            if (cmp != 0) return cmp;

            // speed (most results differentiate here)
            cmp = CompareBySpeed(x, y);
            if (cmp != 0) return cmp;

            cmp = CompareByFiles(x, y);
            if (cmp != 0) return cmp;
            return x.Username.CompareTo(y.Username);
        }

        private string getFolderName(SearchResponse searchResponse)
        {
            if (searchResponse.Files.Count != 0)
            {
                return Common.Helpers.GetFolderNameFromFile(searchResponse.Files.First().Filename);
            }
            else if (searchResponse.LockedFiles.Count != 0)
            {
                return Common.Helpers.GetFolderNameFromFile(searchResponse.LockedFiles.First().Filename);
            }
            else
            {
                return string.Empty;
            }
        }

        public virtual int Compare(SearchResponse x, SearchResponse y)
        {
            if (searchResultSorting == SearchResultSorting.Available)
            {
                return CompareByAvailable(x, y);
            }
            else if (searchResultSorting == SearchResultSorting.Fastest)
            {
                int cmp = CompareBySpeed(x, y);
                if (cmp != 0) return cmp;
                return CompareByAvailable(x, y);
            }
            else if (searchResultSorting == SearchResultSorting.BitRate)
            {
                x.GetDominantFileTypeAndBitRate(PreferencesState.HideLockedResultsInSearch, out double xbitRate);
                y.GetDominantFileTypeAndBitRate(PreferencesState.HideLockedResultsInSearch, out double ybitRate);
                // descending — higher bitrate is better
                int cmp = -xbitRate.CompareTo(ybitRate);
                if (cmp != 0) return cmp;
                return CompareByAvailable(x, y);
            }
            else if (searchResultSorting == SearchResultSorting.FolderAlphabetical)
            {
                string xFolder = getFolderName(x);
                string yFolder = getFolderName(y);

                if (!string.IsNullOrEmpty(xFolder) && !string.IsNullOrEmpty(yFolder))
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
                return CompareByAvailable(x, y);
            }
            else
            {
                throw new System.Exception("Unknown sorting algorithm");
            }
        }
    }

}