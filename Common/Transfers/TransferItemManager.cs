using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Seeker.Helpers;

namespace Seeker
{
    [Serializable]
    public class TransferItemManager
    {
        public static volatile bool TransfersDirty = false;

        public static void MarkTransfersDirty()
        {
            TransfersDirty = true;
        }

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
        /// On relaunch, cancel any transfers that were active or pending before shutdown,
        /// and track users whose transfers failed due to being offline.
        /// </summary>
        public void OnRelaunch()
        {
            lock (AllTransferItems)
            {
                foreach (var ti in AllTransferItems)
                {
                    const TransferStates activeOrAborted =
                        TransferStates.InProgress |
                        TransferStates.Initializing |
                        TransferStates.Queued |
                        TransferStates.Requested |
                        TransferStates.Aborted;

                    // "None" is equivalent to just being paused
                    if (ti.State == TransferStates.None || (ti.State & activeOrAborted) != 0)
                    {
                        ti.State = TransferStates.Cancelled;
                        ti.RemainingTime = null;
                    }

                    if (ti.State.HasFlag(TransferStates.UserOffline))
                    {
                        TransferState.UsersWhereDownloadFailedDueToOffline[ti.Username] = 0x0;
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

        public List<TransferItem> GetBatchSelectedForRetryCondition(TransferUIState uiState, bool selectFailed)
        {
            bool folderItems = uiState.GroupByFolder && uiState.CurrentlySelectedFolder == null;
            List<TransferItem> tis = new List<TransferItem>();
            foreach (int pos in uiState.BatchSelectedItems)
            {
                if (folderItems)
                {
                    var fi = GetItemAtUserIndex(pos, uiState) as FolderItem;
                    foreach (TransferItem ti in fi.TransferItems)
                    {
                        if (selectFailed && ti.Failed)
                        {
                            tis.Add(ti);
                        }
                        else if (!selectFailed && (ti.State.HasFlag(TransferStates.Cancelled) || ti.State.HasFlag(TransferStates.Queued)))
                        {
                            tis.Add(ti);
                        }
                    }
                }
                else
                {
                    var ti = GetItemAtUserIndex(pos, uiState) as TransferItem;
                    if (selectFailed && ti.Failed)
                    {
                        tis.Add(ti);
                    }
                    else if (!selectFailed && (ti.State.HasFlag(TransferStates.Cancelled) || ti.State.HasFlag(TransferStates.Queued)))
                    {
                        tis.Add(ti);
                    }
                }
            }
            return tis;
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

        public object GetUICurrentList(TransferUIState uiState)
        {
            if (uiState.GroupByFolder)
            {
                if (uiState.CurrentlySelectedFolder != null)
                {
                    return uiState.CurrentlySelectedFolder.TransferItems;
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
        public object RemoveAtUserIndex(int indexOfItem, TransferUIState uiState)
        {
            if (uiState.GroupByFolder)
            {
                if (uiState.CurrentlySelectedFolder != null)
                {
                    var ti = uiState.CurrentlySelectedFolder.TransferItems[indexOfItem];
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

        public ITransferItem GetItemAtUserIndex(int indexOfItem, TransferUIState uiState)
        {
            if (uiState.GroupByFolder)
            {
                if (uiState.CurrentlySelectedFolder != null)
                {
                    return uiState.CurrentlySelectedFolder.TransferItems[indexOfItem];
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
        public int GetUserIndexForTransferItem(TransferItem ti, TransferUIState uiState)
        {
            if (uiState.GroupByFolder)
            {
                if (uiState.CurrentlySelectedFolder != null)
                {
                    return uiState.CurrentlySelectedFolder.TransferItems.IndexOf(ti);
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

        public int GetUserIndexForITransferItem(ITransferItem iti, TransferUIState uiState)
        {
            if (iti is TransferItem ti)
            {
                return GetUserIndexForTransferItem(ti, uiState);
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
        public int GetUserIndexForTransferItem(string fullfilename, TransferUIState uiState)
        {
            if (uiState.GroupByFolder)
            {
                if (uiState.CurrentlySelectedFolder != null)
                {
                    return uiState.CurrentlySelectedFolder.TransferItems.FindIndex((ti) => ti.FullFilename == fullfilename);
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
                Logger.Debug("IsFolderNowComplete: transferitem is null");
                return false;
            }
            else
            {
                FolderItem? folder = null;
                lock (AllFolderItems)
                {
                    folder = GetMatchingFolder(ti);
                }
                if (folder == null)
                {
                    Logger.Debug("IsFolderNowComplete: folder is null");
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

        private FolderItem? GetMatchingFolder(TransferItem ti)
        {
            lock (AllFolderItems)
            {
                var foldername = string.IsNullOrEmpty(ti.FolderName)
                    ? Common.Helpers.GetFolderNameFromFile(ti.FullFilename)
                    : ti.FolderName;

                return AllFolderItems.FirstOrDefault(f =>
                    f.FolderName == foldername && f.Username == ti.Username);
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
                if (matchingFolder == null)
                {
                    AllFolderItems.Add(new FolderItem(ti.FolderName, ti.Username, ti));
                }
                else
                {
                    matchingFolder.Add(ti);
                }
            }
            MarkTransfersDirty();
        }

        public void ClearAllComplete()
        {
            lock (AllTransferItems)
            {
                AllTransferItems.RemoveAll((TransferItem i) => { return i.Progress > 99; });
                if (isUploads)
                {
                    AllTransferItems.RemoveAll((TransferItem i) => { return SimpleHelpers.IsUploadCompleteOrAborted(i.State); });
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
            MarkTransfersDirty();
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
            MarkTransfersDirty();
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

        public static bool NeedsCleanUp(TransferItem ti)
        {
            if (ti != null && ti.IncompleteParentUri != null && !ti.CancelAndClearFlag)
            {
                return true;
            }
            return false;
        }

        public List<TransferItem> ClearAllReturnCleanupItems()
        {
            List<TransferItem> tisNeedingCleanup;
            lock (AllTransferItems)
            {
                tisNeedingCleanup = AllTransferItems.Where(NeedsCleanUp).ToList();
                AllTransferItems.Clear();
            }
            lock (AllFolderItems)
            {
                AllFolderItems.Clear();
            }
            MarkTransfersDirty();
            return tisNeedingCleanup;
        }

        public List<TransferItem> ClearSelectedItemsReturnCleanupItems(TransferUIState uiState)
        {
            List<TransferItem> toCleanUp = new List<TransferItem>();
            lock (AllTransferItems)
            {
                bool isFolderItems = uiState.GroupByFolder && uiState.CurrentlySelectedFolder == null;

                if (isFolderItems)
                {
                    List<FolderItem> toClear = new List<FolderItem>();
                    foreach (int pos in uiState.BatchSelectedItems)
                    {
                        toClear.Add(GetItemAtUserIndex(pos, uiState) as FolderItem);
                    }
                    foreach (FolderItem item in toClear)
                    {
                        toCleanUp.AddRange(ClearAllFromFolderReturnCleanupItems(item));
                    }
                }
                else
                {
                    uiState.BatchSelectedItems.Sort();
                    uiState.BatchSelectedItems.Reverse();
                    foreach (int pos in uiState.BatchSelectedItems)
                    {
                        if (NeedsCleanUp(GetItemAtUserIndex(pos, uiState) as TransferItem))
                        {
                            toCleanUp.Add(GetItemAtUserIndex(pos, uiState) as TransferItem);
                        }
                        this.RemoveAtUserIndex(pos, uiState);
                    }
                }
            }
            MarkTransfersDirty();
            return toCleanUp;
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
            MarkTransfersDirty();
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
            MarkTransfersDirty();
        }

        public List<TransferItem> ClearAllFromFolderReturnCleanupItems(FolderItem fi)
        {
            var tisNeedingCleanup = fi.TransferItems.Where(NeedsCleanUp).ToList();
            lock (AllTransferItems)
            {
                foreach (TransferItem ti in fi.TransferItems)
                {
                    AllTransferItems.Remove(ti);
                }
            }
            fi.TransferItems.Clear();
            AllFolderItems.Remove(fi);
            MarkTransfersDirty();
            return tisNeedingCleanup;
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
                    TransferState.CancellationTokens.TryGetValue(TransferState.ProduceCancellationTokenKey(ti), out CancellationTokenSource token);
                    token?.Cancel();
                    //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
                }
                TransferState.CancellationTokens.Clear();
            }
            MarkTransfersDirty();
        }

        public void CancelSelectedItems(TransferUIState uiState, bool prepareForClear = false)
        {
            lock (AllTransferItems)
            {
                bool isFolderItems = false;
                if (uiState.GroupByFolder && uiState.CurrentlySelectedFolder == null)
                {
                    isFolderItems = true;
                }

                for (int i = 0; i < uiState.BatchSelectedItems.Count; i++)
                {
                    if (isFolderItems)
                    {
                        FolderItem fi = this.GetItemAtUserIndex(uiState.BatchSelectedItems[i], uiState) as FolderItem;
                        CancelFolder(fi, prepareForClear);
                    }
                    else
                    {
                        TransferItem ti = this.GetItemAtUserIndex(uiState.BatchSelectedItems[i], uiState) as TransferItem;
                        if (prepareForClear)
                        {
                            if (ti.InProcessing) //let continuation action clear this guy
                            {
                                ti.CancelAndClearFlag = true;
                            }
                        }
                        TransferState.CancellationTokens.TryRemove(TransferState.ProduceCancellationTokenKey(ti), out CancellationTokenSource token);
                        token?.Cancel();
                    }
                }
            }
            MarkTransfersDirty();
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
                    TransferState.CancelAndRemoveToken(ti);
                }
            }
            MarkTransfersDirty();
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
                if (matchingFolder == null)
                {
                    //error folder not found...
                }
                else
                {
                    matchingFolder.Remove(ti);
                    if (matchingFolder.IsEmpty())
                    {
                        AllFolderItems.Remove(matchingFolder);
                    }
                }
            }
            MarkTransfersDirty();
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
                    foldersToNotDelete.Add(SimpleHelpers.GenerateIncompleteFolderName(fi.Username, fi.TransferItems.First().FullFilename, fi.GetDirectoryLevel()));
                }
            }
            return foldersToNotDelete;
        }
    }
}