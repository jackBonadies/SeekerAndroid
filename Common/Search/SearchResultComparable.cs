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
                x.GetDominantFileTypeAndBitRate(PreferencesState.HideLockedResultsInSearch, out double xbitRate);
                y.GetDominantFileTypeAndBitRate(PreferencesState.HideLockedResultsInSearch, out double ybitRate);
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
                    xFolder = Common.Helpers.GetFolderNameFromFile(x.Files.First().Filename);
                }
                else if (x.LockedFiles.Count != 0)
                {
                    xFolder = Common.Helpers.GetFolderNameFromFile(x.LockedFiles.First().Filename);
                }

                if (y.Files.Count != 0)
                {
                    yFolder = Common.Helpers.GetFolderNameFromFile(y.Files.First().Filename);
                }
                else if (y.LockedFiles.Count != 0)
                {
                    yFolder = Common.Helpers.GetFolderNameFromFile(y.LockedFiles.First().Filename);
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

}