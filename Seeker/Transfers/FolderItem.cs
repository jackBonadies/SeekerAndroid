using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    [Serializable]
    public class FolderItem : ITransferItem
    {
        public bool IsUpload()
        {
            if (TransferItems.Count == 0)
            {
                return false; //usually this is if we are in the process of clearing the folder....
            }
            return TransferItems[0].IsUpload();
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public TimeSpan? RemainingFolderTime; //this should never be serialized

        public TimeSpan? GetRemainingTime()
        {
            return RemainingFolderTime;
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public double AvgSpeed; //this could one day be serialized if you want say speed history (like QT does)

        public double GetAvgSpeed()
        {
            return AvgSpeed;
        }

        public string GetDisplayName()
        {
            return FolderName;
        }

        public string GetFolderName()
        {
            return FolderName;
        }

        public string GetUsername()
        {
            return Username;
        }

        public string GetDisplayFolderName()
        {
            //this is similar to QT (in the case of Seeker multiple subdirectories)
            //but not quite.
            //QT will show subdirs/complete/Soulseek Downloads/Music/H: (where everything after subdirs is your download folder)
            //whereas we just show subdirs
            //subdirs is folder name in both cases for single folder, 
            // and say (01 / 2020 / test_folder) for nested.
            if (GetDirectoryLevel() == 1)
            {
                //they are the same
                return FolderName;
            }
            else
            {
                //split reverse.
                var reversedArray = this.FolderName.Split('\\').Reverse();
                return string.Join('\\', reversedArray);
            }
        }

        public int GetDirectoryLevel()
        {
            //just parent folder = level 1 (search result and browse single dir case)
            //grandparent = level 2 (browse download subdirs case - i.e. Album, Album > covers)
            //etc.
            if (this.FolderName == null || !this.FolderName.Contains('\\'))
            {
                return 1;
            }
            return this.FolderName.Split('\\').Count();
        }

        /// <summary>
        /// int - percent.
        /// </summary>
        /// <returns></returns>
        public int GetFolderProgress(out long totalBytes, out long bytesCompleted)
        {
            lock (TransferItems)
            {
                long folderBytesComplete = 0;
                long totalFolderBytes = 0;
                foreach (TransferItem ti in TransferItems)
                {
                    folderBytesComplete += (long)((ti.Progress / 100.0) * ti.Size);
                    totalFolderBytes += ti.Size;
                }
                totalBytes = totalFolderBytes;
                bytesCompleted = folderBytesComplete;
                //error "System.OverflowException: Value was either too large or too small for an Int32." can occur for example when totalFolderBytes is 0
                if (totalFolderBytes == 0)
                {
                    MainActivity.LogInfoFirebase("total folder bytes == 0");
                    return 100;
                }
                else
                {
                    return Convert.ToInt32((folderBytesComplete * 100.0 / totalFolderBytes));
                }
            }
        }

        /// <summary>
        /// Get the overall queue of the folder (the lowest queued track)
        /// </summary>
        /// <returns></returns>
        public int GetQueueLength()
        {
            lock (TransferItems)
            {
                int queueLen = int.MaxValue;
                foreach (TransferItem ti in TransferItems)
                {
                    if (ti.State == TransferStates.Queued)
                    {
                        queueLen = Math.Min(ti.QueueLength, queueLen);
                    }
                }
                return queueLen;
            }
        }

        public TransferItem GetLowestQueuedTransferItem()
        {
            lock (TransferItems)
            {
                int queueLen = int.MaxValue;
                TransferItem curLowest = null;
                foreach (TransferItem ti in TransferItems)
                {
                    if (ti.State == TransferStates.Queued)
                    {
                        queueLen = Math.Min(ti.QueueLength, queueLen);
                        if (queueLen == ti.QueueLength)
                        {
                            curLowest = ti;
                        }
                    }
                }
                return curLowest;
            }
        }


        /// <summary>
        /// Get the overall state of the folder.
        /// </summary>
        /// <returns></returns>
        public TransferStates GetState(out bool isFailed, out bool anyOffline)
        {
            //top priority - In Progress
            //if ANY are InProgress then this is considered in progress
            //if not then if ANY initialized.
            //if not then if ANY queued its considered queued.  And the queue number is that of the lowest transfer.
            //if not then if ANY failed its considered failed
            //if not then its cancelled (i.e. paused)
            //if not then its Succeeded.

            isFailed = false;
            anyOffline = false;
            //if not then none...
            lock (TransferItems)
            {
                TransferStates folderState = TransferStates.None;
                foreach (TransferItem ti in TransferItems)
                {
                    TransferStates state = ti.State;
                    if (state == TransferStates.InProgress)
                    {
                        isFailed = false;
                        return TransferStates.InProgress;
                    }
                    else
                    {
                        if (ti.Failed)
                        {
                            isFailed = true;
                            if (ti.State.HasFlag(TransferStates.UserOffline))
                            {
                                anyOffline = true;
                            }
                        }
                        //do priority
                        if (state.HasFlag(TransferStates.Initializing) || state.HasFlag(TransferStates.Requested) || state.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if (state.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if ((state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut)) && !folderState.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if (state.HasFlag(TransferStates.Cancelled) && !folderState.HasFlag(TransferStates.Rejected) && !folderState.HasFlag(TransferStates.TimedOut) && !folderState.HasFlag(TransferStates.Errored) && !folderState.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if (state.HasFlag(TransferStates.Succeeded) && !folderState.HasFlag(TransferStates.Rejected) && !folderState.HasFlag(TransferStates.TimedOut) && !folderState.HasFlag(TransferStates.Cancelled) && !folderState.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Errored) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                    }
                }
                return folderState;
            }
        }

        public string FolderName; //this is always ex "Album Name" or for depth > 1 "GrandParent/Parent".  Display Folder name is reversed.
        public string Username;
        public List<TransferItem> TransferItems;

        public FolderItem(string folderName, string username, TransferItem initialTransferItem)
        {
            TransferItems = new List<TransferItem>();
            Add(initialTransferItem);
            if (folderName == null)
            {
                folderName = Common.Helpers.GetFolderNameFromFile(initialTransferItem.FullFilename);
            }
            FolderName = folderName;
            Username = username;
        }

        /// <summary>
        /// default public constructor for serialization.
        /// </summary>
        public FolderItem()
        {
            TransferItems = new List<TransferItem>();
        }

        public void ClearAllComplete()
        {
            lock (TransferItems)
            {
                TransferItems.RemoveAll((TransferItem ti) => { return ti.Progress > 99; });
                if (IsUpload())
                {
                    TransferItems.RemoveAll((TransferItem i) => { return CommonHelpers.IsUploadCompleteOrAborted(i.State); });
                }
            }
        }

        public bool HasTransferItem(TransferItem ti)
        {
            lock (TransferItems)
            {
                return TransferItems.Contains(ti);
            }
        }

        public bool IsEmpty()
        {
            return TransferItems.Count == 0;
        }

        public void Remove(TransferItem ti)
        {
            lock (TransferItems)
            {
                TransferItems.Remove(ti);
            }
        }


        public void Add(TransferItem ti)
        {
            lock (TransferItems)
            {
                TransferItems.Add(ti);
            }
        }
    }
}