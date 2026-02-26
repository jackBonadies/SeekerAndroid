using System;
using System.Collections.Generic;
using System.Text;
using Soulseek;

namespace Common
{
    public static class SearchResponseUtil
    {   
        private static Dictionary<string, List<File>> separateInfoFolders(IReadOnlyCollection<File> files)
        {
            Dictionary<string, List<File>> folderFilePairs = new Dictionary<string, List<File>>();
            foreach (File file in files)
            {
                string folderName = Common.Helpers.GetFullPathFromFile(file.Filename);
                if (folderFilePairs.ContainsKey(folderName))
                {
                    //MainActivity.LogDebug("Split Foldername: " + folderName);
                    folderFilePairs[folderName].Add(file);
                }
                else
                {
                    List<File> fileListTemp = new List<File>()
                    { 
                        file
                    };
                    folderFilePairs.Add(folderName, fileListTemp);
                }
            }
            return folderFilePairs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="origResponse"></param>
        /// <returns>Whether we need to split, and if true then the split search responses</returns>
        public static Tuple<bool, List<SearchResponse>> SplitMultiDirResponse(bool hideLocked, SearchResponse origResponse)
        {
            if (origResponse.Files.Count != 0 || (!hideLocked && origResponse.LockedFiles.Count != 0))
            {
                var folderFilePairs = separateInfoFolders(origResponse.Files);

                //I'm not sure if locked files and unlocked files can appear in the same folder,
                //but regardless, split them up into separate folders.
                //even if they both have the same foldername, they will have the lock symbol to differentiate them.
                Dictionary<string, List<File>> lockedFolderFilePairs;
                if (hideLocked)
                {
                    lockedFolderFilePairs = new Dictionary<string, List<File>>();
                }
                else
                {
                    lockedFolderFilePairs = separateInfoFolders(origResponse.LockedFiles);
                }
 
                //we took the search response and split it into more than one folder.
                if ((folderFilePairs.Keys.Count + lockedFolderFilePairs.Keys.Count) > 1)
                {
                    //split them
                    List<SearchResponse> splitSearchResponses = new List<SearchResponse>();
                    foreach (var pair in folderFilePairs)
                    {
                        splitSearchResponses.Add(new SearchResponse(origResponse.Username, origResponse.Token, origResponse.HasFreeUploadSlot, origResponse.UploadSpeed, origResponse.QueueLength, pair.Value, null));
                    }
                    foreach (var pair in lockedFolderFilePairs)
                    {
                        splitSearchResponses.Add(new SearchResponse(origResponse.Username, origResponse.Token, origResponse.HasFreeUploadSlot, origResponse.UploadSpeed, origResponse.QueueLength, null, pair.Value));
                    }
                    //MainActivity.LogDebug("User: " + origResponse.Username + " got split into " + folderFilePairs.Keys.Count);
                    return new Tuple<bool, List<SearchResponse>>(true, splitSearchResponses);
                }
                else
                {
                    //no need to split it.
                    return new Tuple<bool, List<SearchResponse>>(false, null);
                }
            }
            else
            {
                return new Tuple<bool, List<SearchResponse>>(false, null);
            }
        }
    }
}
