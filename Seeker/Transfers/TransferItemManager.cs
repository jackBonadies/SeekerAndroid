using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Seeker
{
    [Serializable]
    public class TransferItemManager
    {
        private bool isUploads;
        /// <summary>
        /// Do not use directly.  This is public only for default serialization.
        /// </summary>
        public List<TransferItem> AllTransferItems;

        /// <summary>
        /// Do not use directly.  This is public only for default serialization.
        /// </summary>
        public List<FolderItem> AllFolderItems;

        public TransferItemManager()
        {
            AllTransferItems = new List<TransferItem>();
            AllFolderItems = new List<FolderItem>();
        }

        public TransferItemManager(bool _isUploads)
        {
            isUploads = _isUploads;
            AllTransferItems = new List<TransferItem>();
            AllFolderItems = new List<FolderItem>();
        }

        public IEnumerable<TransferItem> GetTransferItemsForUser(string username)
        {
            lock (AllTransferItems)
            {
                return AllTransferItems.Where((item) => item.Username == username).ToList();
            }
        }

        /// <summary>
        /// transfers that were previously InProgress before we shut down should now be considered paused (cancelled)
        /// add users where failure occured due to them being offline to dict so we can efficiently check it in response
        /// to status changed events AND we can AddUser to get their status updates.
        /// </summary>
        public void OnRelaunch()
        {
            lock (AllTransferItems)
            {
                foreach (var ti in AllTransferItems)
                {
                    if (ti.State.HasFlag(TransferStates.InProgress))
                    {
                        ti.State = TransferStates.Cancelled;
                        ti.RemainingTime = null;
                    }

                    if (ti.State.HasFlag(TransferStates.Aborted))
                    {
                        ti.State = TransferStates.Cancelled;
                        ti.RemainingTime = null;
                    }

                    if (ti.State.HasFlag(TransferStates.UserOffline))
                    {
                        TransfersFragment.UsersWhereDownloadFailedDueToOffline[ti.Username] = 0x0;
                    }
                }
            }
        }

        public List<Tuple<TransferItem, int>> GetListOfPausedFromFolder(FolderItem fi)
        {
            List<Tuple<TransferItem, int>> transferItemConditionList = new List<Tuple<TransferItem, int>>();
            lock (fi.TransferItems)
            {
                for (int i = 0; i < fi.TransferItems.Count; i++)
                {
                    var item = fi.TransferItems[i];

                    if (item.State.HasFlag(TransferStates.Cancelled) || item.State.HasFlag(TransferStates.Queued))
                    {
                        transferItemConditionList.Add(new Tuple<TransferItem, int>(item, i));
                    }

                }
            }
            return transferItemConditionList;
        }

        public List<Tuple<TransferItem, int, int>> GetListOfPaused()
        {
            List<Tuple<TransferItem, int, int>> transferItemConditionList = new List<Tuple<TransferItem, int, int>>();
            lock (AllTransferItems)
            {
                lock (AllFolderItems)
                {
                    for (int i = 0; i < AllTransferItems.Count; i++)
                    {
                        var item = AllTransferItems[i];

                        if (item.State.HasFlag(TransferStates.Cancelled) || item.State.HasFlag(TransferStates.Queued))
                        {
                            int folderIndex = -1;
                            for (int fi = 0; fi < AllFolderItems.Count; fi++)
                            {
                                if (AllFolderItems[fi].HasTransferItem(item))
                                {
                                    folderIndex = fi;
                                    break;
                                }

                            }
                            transferItemConditionList.Add(new Tuple<TransferItem, int, int>(AllTransferItems[i], i, folderIndex));
                        }

                    }
                }
            }
            return transferItemConditionList;
        }

        public List<Tuple<TransferItem, int>> GetListOfFailedFromFolder(FolderItem fi)
        {
            List<Tuple<TransferItem, int>> transferItemConditionList = new List<Tuple<TransferItem, int>>();
            lock (fi.TransferItems)
            {
                for (int i = 0; i < fi.TransferItems.Count; i++)
                {
                    var item = fi.TransferItems[i];

                    if (item.Failed)
                    {
                        transferItemConditionList.Add(new Tuple<TransferItem, int>(item, i));
                    }

                }
            }
            return transferItemConditionList;
        }

        public List<Tuple<TransferItem, int, int>> GetListOfFailed()
        {
            List<Tuple<TransferItem, int, int>> transferItemConditionList = new List<Tuple<TransferItem, int, int>>();
            lock (AllTransferItems)
            {
                lock (AllFolderItems)
                {
                    for (int i = 0; i < AllTransferItems.Count; i++)
                    {
                        var item = AllTransferItems[i];

                        if (item.Failed)
                        {
                            int folderIndex = -1;
                            for (int fi = 0; fi < AllFolderItems.Count; fi++)
                            {
                                if (AllFolderItems[fi].HasTransferItem(item))
                                {
                                    folderIndex = fi;
                                    break;
                                }

                            }
                            transferItemConditionList.Add(new Tuple<TransferItem, int, int>(AllTransferItems[i], i, folderIndex));
                        }

                    }
                }
            }
            return transferItemConditionList;
        }

        public List<TransferItem> GetListOfCondition(TransferStates state)
        {
            List<TransferItem> transferItemConditionList = new List<TransferItem>();
            lock (AllTransferItems)
            {
                for (int i = 0; i < AllTransferItems.Count; i++)
                {
                    var item = AllTransferItems[i];

                    if (item.State.HasFlag(state))
                    {
                        transferItemConditionList.Add(item);
                    }

                }
            }
            return transferItemConditionList;
        }

        public List<TransferItem> GetTransferItemsFromUser(string username, bool failedOnly, bool failedAndOfflineOnly)
        {
            List<TransferItem> transferItemConditionList = new List<TransferItem>();
            lock (AllTransferItems)
            {
                foreach (var item in AllTransferItems)
                {
                    if (item.Username == username)
                    {
                        if (failedAndOfflineOnly && !item.State.HasFlag(TransferStates.UserOffline))
                        {
                            continue;
                        }
                        if (failedOnly && !item.Failed)
                        {
                            continue;
                        }
                        transferItemConditionList.Add(item);
                    }
                }
            }
            return transferItemConditionList;
        }

        public object GetUICurrentList()
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems;
                }
                else
                {
                    return AllFolderItems;
                }
            }
            else
            {
                return AllTransferItems;
            }
        }

        /// <summary>
        /// Returns the removed object (either TransferItem or List of TransferItem)
        /// </summary>
        /// <param name="indexOfItem"></param>
        /// <returns></returns>
        public object RemoveAtUserIndex(int indexOfItem)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    var ti = TransfersFragment.GetCurrentlySelectedFolder().TransferItems[indexOfItem];
                    Remove(ti);
                    return ti;
                }
                else
                {
                    List<TransferItem> transferItemsToRemove = new List<TransferItem>();
                    lock (AllFolderItems[indexOfItem].TransferItems)
                    {
                        foreach (var ti in AllFolderItems[indexOfItem].TransferItems)
                        {
                            transferItemsToRemove.Add(ti);
                        }
                    }
                    foreach (var ti in transferItemsToRemove)
                    {
                        Remove(ti);
                    }
                    return transferItemsToRemove;
                }
            }
            else
            {
                var ti = AllTransferItems[indexOfItem];
                Remove(ti);
                return ti;
            }
        }

        public ITransferItem GetItemAtUserIndex(int indexOfItem)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems[indexOfItem];
                }
                else
                {
                    return AllFolderItems[indexOfItem];
                }
            }
            else
            {
                return AllTransferItems[indexOfItem];
            }
        }

        /// <summary>
        /// The index in the folder, the folder, or the overall index
        /// </summary>
        /// <param name="indexOfItem"></param>
        /// <returns></returns>
        public int GetUserIndexForTransferItem(TransferItem ti)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems.IndexOf(ti);
                }
                else
                {
                    string foldername = ti.FolderName;
                    if (foldername == null)
                    {
                        foldername = Common.Helpers.GetFolderNameFromFile(ti.FullFilename);
                    }
                    return AllFolderItems.FindIndex((FolderItem fi) => { return fi.FolderName == foldername && fi.Username == ti.Username; });
                }
            }
            else
            {
                return AllTransferItems.IndexOf(ti);
            }
        }

        public int GetIndexForFolderItem(FolderItem ti)
        {
            lock (AllFolderItems)
            {
                return AllFolderItems.IndexOf(ti);
            }
        }

        public int GetUserIndexForITransferItem(ITransferItem iti)
        {
            if (iti is TransferItem ti)
            {
                return GetUserIndexForTransferItem(ti);
            }
            else if (iti is FolderItem fi)
            {
                return GetIndexForFolderItem(fi);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// The index in the folder, the folder, or the overall index
        /// </summary>
        /// <param name="indexOfItem"></param>
        /// <returns></returns>
        public int GetUserIndexForTransferItem(string fullfilename)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems.FindIndex((ti) => ti.FullFilename == fullfilename);
                }
                else
                {
                    TransferItem ti;
                    lock (AllTransferItems)
                    {
                        ti = AllTransferItems.Find((ti) => ti.FullFilename == fullfilename);
                    }
                    string foldername = ti.FolderName;
                    if (foldername == null)
                    {
                        foldername = Common.Helpers.GetFolderNameFromFile(ti.FullFilename);
                    }
                    return AllFolderItems.FindIndex((FolderItem fi) => { return fi.FolderName == foldername && fi.Username == ti.Username; });
                }
            }
            else
            {
                return AllTransferItems.FindIndex((ti) => ti.FullFilename == fullfilename);
            }
        }


        //public TransferItem GetTransferItemWithUserIndex(string fullFileName, out int indexOfItem)
        //{
        //    if (fullFileName == null)
        //    {
        //        indexOfItem = -1;
        //        return null;
        //    }
        //    lock (AllTransferItems)
        //    {
        //        foreach (TransferItem item in AllTransferItems)
        //        {
        //            if (item.FullFilename.Equals(fullFileName)) //fullfilename includes dir so that takes care of any ambiguity...
        //            {
        //                indexOfItem = AllTransferItems.IndexOf(item);
        //                return item;
        //            }
        //        }
        //    }
        //    indexOfItem = -1;
        //    return null;
        //}


        public TransferItem GetTransferItemWithIndexFromAll(string fullFileName, string username, out int indexOfItem)
        {
            if (fullFileName == null || username == null)
            {
                indexOfItem = -1;
                return null;
            }
            lock (AllTransferItems)
            {
                foreach (TransferItem item in AllTransferItems)
                {
                    if (item.FullFilename.Equals(fullFileName) && item.Username.Equals(username)) //fullfilename includes dir so that takes care of any ambiguity...
                    {
                        indexOfItem = AllTransferItems.IndexOf(item);
                        return item;
                    }
                }
            }
            indexOfItem = -1;
            return null;
        }

        public bool Exists(string fullFilename, string username, long size)
        {
            lock (AllTransferItems)
            {
                return AllTransferItems.Exists((TransferItem ti) =>
                {
                    return (ti.FullFilename == fullFilename &&
                           ti.Size == size &&
                           ti.Username == username
                       );
                });
            }
        }

        public bool ExistsAndInProcessing(string fullFilename, string username, long size)
        {
            lock (AllTransferItems)
            {
                return AllTransferItems.Where((TransferItem ti) =>
                {
                    return (ti.FullFilename == fullFilename &&
                           ti.Size == size &&
                           ti.Username == username
                       );
                }).Any((item) => item.InProcessing);
            }
        }

        public bool IsEmpty()
        {
            return AllTransferItems.Count == 0;
        }

        public TransferItem GetTransferItem(string fullfilename)
        {
            lock (AllTransferItems)
            {
                foreach (TransferItem item in AllTransferItems) //THIS is where those enumeration exceptions are all coming from...
                {
                    if (item.FullFilename.Equals(fullfilename))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        public bool IsFolderNowComplete(TransferItem ti, bool noSingleItemFolders = false) //!!!!! no single item logic will not work with AutoClearComplete
        {
            if (ti == null)
            {
                MainActivity.LogDebug("IsFolderNowComplete: transferitem is null");
                return false;
            }
            else
            {
                FolderItem folder = null;
                lock (AllFolderItems)
                {
                    folder = GetMatchingFolder(ti).FirstOrDefault();
                }
                if (folder == null)
                {
                    MainActivity.LogDebug("IsFolderNowComplete: folder is null");
                    return false;
                }
                lock (folder.TransferItems)
                {
                    foreach (TransferItem item in folder.TransferItems)
                    {
                        if (!item.State.HasFlag(TransferStates.Succeeded))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private IEnumerable<FolderItem> GetMatchingFolder(TransferItem ti)
        {
            lock (AllFolderItems)
            {
                string foldername = string.Empty;
                if (string.IsNullOrEmpty(ti.FolderName))
                {
                    foldername = Common.Helpers.GetFolderNameFromFile(ti.FullFilename);
                }
                else
                {
                    foldername = ti.FolderName;
                }
                return AllFolderItems.Where((folder) => folder.FolderName == foldername && folder.Username == ti.Username);
            }
        }

        private int GetMatchingFolderIndex(TransferItem ti)
        {
            lock (AllFolderItems)
            {
                for (int i = 0; i < AllFolderItems.Count; i++)
                {
                    if (AllFolderItems[i].HasTransferItem(ti))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// This way we will have the right reference
        /// </summary>
        /// <param name="ti"></param>
        /// <returns></returns>
        public TransferItem AddIfNotExistAndReturnTransfer(TransferItem ti, out bool exists)
        {
            lock (AllTransferItems)
            {
                var linq = AllTransferItems.Where((existingTi) => { return existingTi.Username == ti.Username && existingTi.FullFilename == ti.FullFilename; });
                if (linq.Count() > 0)
                {
                    exists = true;
                    return linq.First();
                }
                else
                {
                    Add(ti);
                    exists = false;
                    return ti;
                }
            }
        }

        public void Add(TransferItem ti)
        {
            lock (AllTransferItems)
            {
                AllTransferItems.Add(ti);
            }
            lock (AllFolderItems)
            {
                var matchingFolder = GetMatchingFolder(ti);
                if (matchingFolder.Count() == 0)
                {
                    AllFolderItems.Add(new FolderItem(ti.FolderName, ti.Username, ti));
                }
                else
                {
                    var folderItem = matchingFolder.First();
                    folderItem.Add(ti);
                }
            }
        }

        public void ClearAllComplete()
        {
            lock (AllTransferItems)
            {
                AllTransferItems.RemoveAll((TransferItem i) => { return i.Progress > 99; });
                if (isUploads)
                {
                    AllTransferItems.RemoveAll((TransferItem i) => { return CommonHelpers.IsUploadCompleteOrAborted(i.State); });
                }
            }
            lock (AllFolderItems)
            {
                foreach (FolderItem f in AllFolderItems)
                {
                    f.ClearAllComplete();
                }
                AllFolderItems.RemoveAll((FolderItem f) => { return f.IsEmpty(); });
            }
        }

        public void ClearAllCompleteFromFolder(FolderItem fi)
        {
            lock (AllTransferItems)
            {
                AllTransferItems.RemoveAll((TransferItem i) => { return i.Progress > 99 && fi.Username == i.Username && GetFolderNameFromTransferItem(i) == fi.FolderName; });
            }
            fi.ClearAllComplete();
            if (fi.IsEmpty())
            {
                AllFolderItems.Remove(fi);
            }
        }

        private static string GetFolderNameFromTransferItem(TransferItem ti)
        {
            if (string.IsNullOrEmpty(ti.FolderName)) //this wont happen with the latest code.  so no need to worry about depth.
            {
                return Common.Helpers.GetFolderNameFromFile(ti.FullFilename);
            }
            else
            {
                return ti.FolderName;
            }
        }

        public void ClearAllAndClean()
        {
            lock (AllTransferItems)
            {
                List<TransferItem> tisNeedingCleanup = AllTransferItems.Where((item) => { return TransferItemManagerWrapper.NeedsCleanUp(item); }).ToList();
                if (tisNeedingCleanup.Any())
                {
                    TransferItemManagerWrapper.CleanupEntry(tisNeedingCleanup);
                }
                AllTransferItems.Clear();
            }
            lock (AllFolderItems)
            {
                AllFolderItems.Clear();
            }
        }

        public void ClearSelectedItemsAndClean()
        {
            lock (AllTransferItems)
            {
                bool isFolderItems = false;
                if (TransfersFragment.GroupByFolder && TransfersFragment.GetCurrentlySelectedFolder() == null)
                {
                    isFolderItems = true;
                }


                if (isFolderItems)
                {
                    List<FolderItem> toClear = new List<FolderItem>();
                    foreach (int pos in TransfersFragment.BatchSelectedItems)
                    {
                        toClear.Add(GetItemAtUserIndex(pos) as FolderItem);
                    }
                    foreach (FolderItem item in toClear)
                    {
                        ClearAllFromFolderAndClean(item);
                    }

                }
                else
                {
                    List<TransferItem> toCleanUp = new List<TransferItem>();
                    List<TransferItem> toClear = new List<TransferItem>();
                    TransfersFragment.BatchSelectedItems.Sort();
                    TransfersFragment.BatchSelectedItems.Reverse();
                    foreach (int pos in TransfersFragment.BatchSelectedItems)
                    {
                        if (TransferItemManagerWrapper.NeedsCleanUp(GetItemAtUserIndex(pos) as TransferItem))
                        {
                            toCleanUp.Add(GetItemAtUserIndex(pos) as TransferItem);
                        }
                        this.RemoveAtUserIndex(pos);
                    }
                }
            }
        }

        public void ClearAll()
        {
            lock (AllTransferItems)
            {
                AllTransferItems.Clear();
            }
            lock (AllFolderItems)
            {
                AllFolderItems.Clear();
            }
        }

        public void ClearAllFromFolder(FolderItem fi)
        {
            lock (AllTransferItems)
            {
                foreach (TransferItem ti in fi.TransferItems)
                {
                    AllTransferItems.Remove(ti);
                }
            }
            fi.TransferItems.Clear();
            AllFolderItems.Remove(fi);
        }

        public void ClearAllFromFolderAndClean(FolderItem fi)
        {
            IEnumerable<TransferItem> tisNeedingCleanup = fi.TransferItems.Where((item) => { return TransferItemManagerWrapper.NeedsCleanUp(item); });
            if (tisNeedingCleanup.Any())
            {
                TransferItemManagerWrapper.CleanupEntry(tisNeedingCleanup);
            }
            lock (AllTransferItems)
            {
                foreach (TransferItem ti in fi.TransferItems)
                {
                    AllTransferItems.Remove(ti);
                }
            }
            fi.TransferItems.Clear();
            AllFolderItems.Remove(fi);
        }

        public void CancelAll(bool prepareForClear = false)
        {
            lock (AllTransferItems)
            {
                for (int i = 0; i < AllTransferItems.Count; i++)
                {
                    //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                    TransferItem ti = AllTransferItems[i];
                    if (prepareForClear)
                    {
                        if (ti.InProcessing) //let continuation action clear this guy
                        {
                            ti.CancelAndClearFlag = true;
                        }
                    }
                    TransfersFragment.CancellationTokens.TryGetValue(TransfersFragment.ProduceCancellationTokenKey(ti), out CancellationTokenSource token);
                    token?.Cancel();
                    //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
                }
                TransfersFragment.CancellationTokens.Clear();
            }
        }

        public void CancelSelectedItems(bool prepareForClear = false)
        {
            lock (AllTransferItems)
            {
                bool isFolderItems = false;
                if (TransfersFragment.GroupByFolder && TransfersFragment.GetCurrentlySelectedFolder() == null)
                {
                    isFolderItems = true;
                }

                for (int i = 0; i < TransfersFragment.BatchSelectedItems.Count; i++)
                {
                    //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                    if (isFolderItems)
                    {
                        FolderItem fi = this.GetItemAtUserIndex(TransfersFragment.BatchSelectedItems[i]) as FolderItem;
                        CancelFolder(fi, prepareForClear);
                    }
                    else
                    {
                        TransferItem ti = this.GetItemAtUserIndex(TransfersFragment.BatchSelectedItems[i]) as TransferItem;
                        if (prepareForClear)
                        {
                            if (ti.InProcessing) //let continuation action clear this guy
                            {
                                ti.CancelAndClearFlag = true;
                            }
                        }
                        TransfersFragment.CancellationTokens.TryRemove(TransfersFragment.ProduceCancellationTokenKey(ti), out CancellationTokenSource token);
                        token?.Cancel();
                    }
                    //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
                }
            }
        }

        public void CancelFolder(FolderItem fi, bool prepareForClear = false)
        {
            lock (fi.TransferItems)
            {
                for (int i = 0; i < fi.TransferItems.Count; i++)
                {
                    //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                    var ti = fi.TransferItems[i];
                    if (prepareForClear && ti.InProcessing)
                    {
                        ti.CancelAndClearFlag = true;
                    }
                    var key = TransfersFragment.ProduceCancellationTokenKey(ti);
                    TransfersFragment.CancellationTokens.TryGetValue(key, out CancellationTokenSource token);
                    if (token != null)
                    {
                        token.Cancel();
                        TransfersFragment.CancellationTokens.Remove(key, out _);
                    }
                }
            }
        }

        /// <summary>
        /// If its the folders last transfer then we remove the folder
        /// </summary>
        /// <param name="ti"></param>
        public void Remove(TransferItem ti)
        {
            lock (AllTransferItems)
            {
                AllTransferItems.Remove(ti);
            }
            lock (AllFolderItems)
            {
                var matchingFolder = GetMatchingFolder(ti);
                if (matchingFolder.Count() == 0)
                {
                    //error folder not found...
                }
                else
                {
                    var folderItem = matchingFolder.First();
                    folderItem.Remove(ti);
                    if (folderItem.IsEmpty())
                    {
                        AllFolderItems.Remove(folderItem);
                    }
                }
            }
        }

        /// <summary>
        /// i.e. the folders to NOT delete
        /// </summary>
        /// <returns></returns>
        public List<string> GetInUseIncompleteFolderNames()
        {
            List<string> foldersToNotDelete = new List<string>();
            lock (AllFolderItems)
            {
                foreach (FolderItem fi in AllFolderItems)
                {
                    foldersToNotDelete.Add(CommonHelpers.GenerateIncompleteFolderName(fi.Username, fi.TransferItems.First().FullFilename, fi.GetDirectoryLevel()));
                }
            }
            return foldersToNotDelete;
        }
    }
}