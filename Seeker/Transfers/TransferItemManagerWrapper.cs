using AndroidX.DocumentFile.Provider;
using Seeker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Seeker.Helpers;

namespace Seeker
{
    /// <summary>
    /// for both uploads and downloads
    /// </summary>
    public class TransferItemManagerWrapper
    {
        private TransferItemManager Uploads;
        private TransferItemManager Downloads;
        public TransferItemManagerWrapper(TransferItemManager up, TransferItemManager down)
        {
            Uploads = up;
            Downloads = down;
        }

        public static void CleanupEntry(IEnumerable<TransferItem> tis)
        {
            Logger.Debug("launching cleanup entry");
            System.Threading.ThreadPool.QueueUserWorkItem(PeformCleanup, tis);
        }

        public static void CleanupEntry(TransferItem ti)
        {
            Logger.Debug("launching cleanup entry");
            System.Threading.ThreadPool.QueueUserWorkItem(PeformCleanup, ti);
        }

        static void PeformCleanup(object state)
        {
            try
            {
                Logger.Debug("in cleanup entry");
                if (state is IEnumerable<TransferItem> tis)
                {
                    PerfomCleanupItems(tis.ToList()); //added tolist() due to enumerable exception.
                }
                else
                {
                    PerformCleanupItem(state as TransferItem);
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("PeformCleanup: " + e.Message + e.StackTrace);
            }
        }

        public IEnumerable<TransferItem> GetTransferItemsForUser(string username)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetTransferItemsForUser(username);
            }
            else
            {
                return Downloads.GetTransferItemsForUser(username);
            }
        }

        public void CancelSelectedItems(bool prepareForClean)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.CancelSelectedItems(prepareForClean);
            }
            else
            {
                Downloads.CancelSelectedItems(prepareForClean);
            }
        }

        public void ClearSelectedItemsAndClean()
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.ClearSelectedItemsAndClean();
            }
            else
            {
                Downloads.ClearSelectedItemsAndClean();
            }
        }

        public static void PerfomCleanupItems(IEnumerable<TransferItem> tis)
        {
            foreach (TransferItem ti in tis)
            {
                PerformCleanupItem(ti);
            }
        }

        public static void PerformCleanupItem(TransferItem ti)
        {
            Logger.Debug("cleaning up: " + ti.Filename);
            //if (TransfersFragment.TransferItemManagerDL.ExistsAndInProcessing(ti.FullFilename, ti.Username, ti.Size))
            //{
            //    //this should rarely happen. its a race condition if someone clears a download and then goes back to the person they downloaded from to re-download.
            //    return;
            //}
            //api 21+
            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            {
                DocumentFile parent = null;
                Android.Net.Uri parentIncompleteUri = Android.Net.Uri.Parse(ti.IncompleteParentUri);
                if (SeekerState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || parentIncompleteUri.Scheme == "file")
                {
                    parent = DocumentFile.FromFile(new Java.IO.File(parentIncompleteUri.Path));
                }
                else
                {
                    parent = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, parentIncompleteUri); //if from single uri then listing files will give unsupported operation exception...  //if temp (file: //)this will throw (which makes sense as it did not come from open tree uri)
                }

                DocumentFile df = parent.FindFile(ti.Filename);
                if (df == null || !df.Exists())
                {
                    Logger.Debug("delete failed - null or not exist");
                    Logger.InfoFirebase("df is null or not exist: " + parentIncompleteUri + " " + SeekerState.CreateCompleteAndIncompleteFolders + " " + parent.Uri + " " + SettingsActivity.UseIncompleteManualFolder());
                }
                if (!df.Delete()) //nullref
                {
                    Logger.Debug("delete failed");
                }
                DownloadService.DeleteParentIfEmpty(parent);
            }
            else
            {
                Java.IO.File parent = new Java.IO.File(Android.Net.Uri.Parse(ti.IncompleteParentUri).Path);
                Java.IO.File f = parent.ListFiles().First((file) => file.Name == ti.Filename);
                if (f == null || !f.Exists())
                {
                    Logger.Debug("delete failed LEGACY - null or not exist");
                }
                if (!f.Delete())
                {
                    Logger.Debug("delete failed LEGACY");
                }
                DownloadService.DeleteParentIfEmpty(parent);
            }
        }


        /// <summary>
        /// remove and spawn cleanup task if applicable
        /// </summary>
        /// <param name="ti"></param>
        public void RemoveAndCleanUp(TransferItem ti)
        {
            Remove(ti);
            if (NeedsCleanUp(ti))
            {
                CleanupEntry(ti);
            }
        }

        public static bool NeedsCleanUp(TransferItem ti)
        {
            if (ti != null && ti.IncompleteParentUri != null && !ti.CancelAndClearFlag) //if cancel and clear flag is set then it will be cleaned up on continuation. that way we are sure the stream is closed.
            {
                return true;
            }
            return false;
        }


        public void Remove(TransferItem ti)
        {
            if (ti.IsUpload())
            {
                Uploads.Remove(ti);
            }
            else
            {
                Downloads.Remove(ti);
            }
        }

        public object GetUICurrentList()
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetUICurrentList();
            }
            else
            {
                return Downloads.GetUICurrentList();
            }
        }

        public TransferItem GetTransferItemWithIndexFromAll(string fullFileName, string username, bool isUpload, out int indexOfItem)
        {
            if (isUpload)
            {
                return Uploads.GetTransferItemWithIndexFromAll(fullFileName, username, out indexOfItem);
            }
            else
            {
                return Downloads.GetTransferItemWithIndexFromAll(fullFileName, username, out indexOfItem);
            }
        }

        public int GetUserIndexForTransferItem(TransferItem ti) //todo null ti
        {
            if (TransfersFragment.InUploadsMode && ti.IsUpload())
            {
                return Uploads.GetUserIndexForTransferItem(ti);
            }
            else if (!TransfersFragment.InUploadsMode && !(ti.IsUpload()))
            {
                return Downloads.GetUserIndexForTransferItem(ti);
            }
            else
            {
                return -1; //this is okay. we arent on that page so ui events are irrelevant
            }
        }

        public ITransferItem GetItemAtUserIndex(int position)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetItemAtUserIndex(position);
            }
            else
            {
                return Downloads.GetItemAtUserIndex(position);
            }
        }

        public object RemoveAtUserIndex(int position)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.RemoveAtUserIndex(position);
            }
            else
            {
                return Downloads.RemoveAtUserIndex(position);
            }
        }

        /// <summary>
        /// remove and spawn cleanup task if applicable
        /// </summary>
        /// <param name="position"></param>
        public void RemoveAndCleanUpAtUserIndex(int position)
        {
            object objectRemoved = RemoveAtUserIndex(position);
            if (objectRemoved is TransferItem ti)
            {
                if (ti.InProcessing)
                {
                    ti.CancelAndClearFlag = true;
                }
                else
                {
                    if (NeedsCleanUp(ti))
                    {
                        CleanupEntry(ti);
                    }
                }
            }
            else
            {
                List<TransferItem> tis = objectRemoved as List<TransferItem>;
                IEnumerable<TransferItem> tisCleanUpOnComplete = tis.Where((item) => { return item.InProcessing; });
                foreach (var item in tisCleanUpOnComplete)
                {
                    item.CancelAndClearFlag = true;
                }
                IEnumerable<TransferItem> tisNeedingCleanup = tis.Where((item) => { return NeedsCleanUp(item); });
                if (tisNeedingCleanup.Any())
                {
                    CleanupEntry(tisNeedingCleanup);
                }

            }
        }

        public void CancelFolder(FolderItem fi)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.CancelFolder(fi);
            }
            else
            {
                Downloads.CancelFolder(fi);
            }
        }

        /// <summary>
        /// prepare for clear basically says, these guys are going to be cleared, so if they are currently being processed and they get in the download continuation action, clear their incomplete files...
        /// </summary>
        /// <param name="fi"></param>
        /// <param name="prepareForClear"></param>
        public void CancelFolder(FolderItem fi, bool prepareForClear = false)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.CancelFolder(fi);
            }
            else
            {
                Downloads.CancelFolder(fi, prepareForClear);
            }
        }

        public void ClearAllFromFolder(FolderItem fi)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.ClearAllFromFolder(fi);
            }
            else
            {
                Downloads.ClearAllFromFolder(fi);
            }
        }

        public void ClearAllFromFolderAndClean(FolderItem fi)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.ClearAllFromFolder(fi);
            }
            else
            {
                Downloads.ClearAllFromFolderAndClean(fi);
            }
        }

        public int GetIndexForFolderItem(FolderItem folderItem)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetIndexForFolderItem(folderItem);
            }
            else
            {
                return Downloads.GetIndexForFolderItem(folderItem);
            }
        }

        public int GetUserIndexForITransferItem(ITransferItem iti)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetUserIndexForITransferItem(iti);
            }
            else
            {
                return Downloads.GetUserIndexForITransferItem(iti);
            }
        }
    }
}