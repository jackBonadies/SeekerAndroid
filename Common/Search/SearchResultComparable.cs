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

        /// <summary>
        /// The row's representative file: first unlocked file, falling back to the first locked
        /// file. Result rows are folder groups, so this is only a stable ordering key, never an
        /// aggregate. O(1) per call (the collections are already materialised on the response).
        /// </summary>
        private static Soulseek.File GetRepresentativeFile(SearchResponse response)
        {
            if (response.Files.Count != 0)
            {
                return response.Files.First();
            }
            if (response.LockedFiles.Count != 0)
            {
                return response.LockedFiles.First();
            }
            return null;
        }

        private static int CompareByFiles(SearchResponse x, SearchResponse y)
        {
            var xFile = GetRepresentativeFile(x);
            var yFile = GetRepresentativeFile(y);
            if (xFile == null || yFile == null)
            {
                return (yFile != null).CompareTo(xFile != null);
            }
            return string.Compare(xFile.Filename, yFile.Filename, StringComparison.Ordinal);
        }

        /// <summary>
        /// File-name ordering compares the representative file's name only, not the whole path.
        /// Case-insensitive first so "Alpha" and "alpha" are adjacent; an ordinal tie-break keeps
        /// the order deterministic regardless of the current culture.
        /// </summary>
        private static int CompareByFilename(SearchResponse x, SearchResponse y)
        {
            var xFile = GetRepresentativeFile(x);
            var yFile = GetRepresentativeFile(y);
            if (xFile == null || yFile == null)
            {
                // Empty responses sort last: a row without a file cannot meaningfully be named.
                return (yFile != null).CompareTo(xFile != null);
            }

            ReadOnlySpan<char> xName = SimpleHelpers.GetFileNameFromFile(xFile.Filename);
            ReadOnlySpan<char> yName = SimpleHelpers.GetFileNameFromFile(yFile.Filename);
            int cmp = xName.CompareTo(yName, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0)
            {
                return cmp;
            }
            return xName.CompareTo(yName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Descending size of the representative file. No sum is computed, so this stays O(1)
        /// per comparison; an empty response only contributes its availability tie-breaks.
        /// </summary>
        private static int CompareByFileSize(SearchResponse x, SearchResponse y)
        {
            var xFile = GetRepresentativeFile(x);
            var yFile = GetRepresentativeFile(y);
            long xSize = xFile?.Size ?? -1;
            long ySize = yFile?.Size ?? -1;
            // descending - larger representative file first
            return -xSize.CompareTo(ySize);
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

        private ReadOnlySpan<char> getFolderName(SearchResponse searchResponse)
        {
            if (searchResponse.Files.Count != 0)
            {
                return SimpleHelpers.GetFolderNameFromFile(searchResponse.Files.First().Filename);
            }
            else if (searchResponse.LockedFiles.Count != 0)
            {
                return SimpleHelpers.GetFolderNameFromFile(searchResponse.LockedFiles.First().Filename);
            }
            else
            {
                return ReadOnlySpan<char>.Empty;
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
                ReadOnlySpan<char> xFolder = getFolderName(x);
                ReadOnlySpan<char> yFolder = getFolderName(y);

                if (!xFolder.IsEmpty && !yFolder.IsEmpty)
                {
                    int ret = xFolder.CompareTo(yFolder, StringComparison.CurrentCulture);
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
            else if (searchResultSorting == SearchResultSorting.Filename)
            {
                int cmp = CompareByFilename(x, y);
                if (cmp != 0) return cmp;
                return CompareByAvailable(x, y);
            }
            else if (searchResultSorting == SearchResultSorting.FileSize)
            {
                int cmp = CompareByFileSize(x, y);
                if (cmp != 0) return cmp;
                return CompareByAvailable(x, y);
            }
            else
            {
                throw new System.Exception("Unknown sorting algorithm");
            }
        }
    }

}