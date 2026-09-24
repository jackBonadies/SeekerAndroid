using Android.App;
using AndroidX.Core.App;
using Common;
using Seeker.Helpers;
using Seeker.Services;
using Soulseek;
using System;

namespace Seeker.Transfers
{
    public class ItemRemovedEventArgs : EventArgs
    {
        public TransferItem Item;
        // Pre-computed via GetUserIndexForTransferItem on the active tab. -1 if the item
        // belongs to the other tab (downloads vs uploads) or otherwise isn't visible.
        public int UserIndex;
    }

    // Perform UI Agnostic Actions before routing to UI specific event handlers.
    internal static class TransferEventRouter
    {
        public static EventHandler<TransferItem> StateChangedForItem;
        public static EventHandler<ProgressUpdatedUIEventArgs> ProgressUpdated;
        public static EventHandler<ItemRemovedEventArgs> ItemRemoved;

        public static void Wire(ISoulseekClient client)
        {
            client.TransferStateChanged += OnUploadTransferStateChanged;
            client.TransferStateChanged += OnTransferStateChanged;
            client.TransferProgressUpdated += OnTransferProgressUpdated;
        }

        // kick keepalive timer, update dl/ul count, if failed due to user offline put it in the dictionary, trigger state changed for item event
        private static void OnTransferStateChanged(object sender, TransferStateChangedEventArgs e)
        {
            SeekerApplication.KickKeepAliveTimer();

            bool isUpload = e.Transfer.Direction == TransferDirection.Upload;

            // Track active transfer counts via atomic counters.
            // Increment when a transfer first appears (previous state is None).
            // Decrement when a transfer reaches a terminal state (Completed flag set).
            if (e.PreviousState == TransferStates.None)
            {
                TransferItemManager.MarkTransfersDirty();
                if (isUpload)
                {
                    int count = System.Threading.Interlocked.Increment(ref SeekerApplication._activeUploadCount);
                    SeekerApplication.NotifyUploadCountChanged(count);
                }
                else
                {
                    int count = System.Threading.Interlocked.Increment(ref SeekerApplication._activeDownloadCount);
                    SeekerApplication.NotifyDownloadCountChanged(count);
                }
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Completed) && !e.PreviousState.HasFlag(TransferStates.Completed))
            {
                TransferItemManager.MarkTransfersDirty();
                if (isUpload)
                {
                    int count = Math.Max(0, System.Threading.Interlocked.Decrement(ref SeekerApplication._activeUploadCount));
                    SeekerApplication.NotifyUploadCountChanged(count);
                }
                else
                {
                    int count = Math.Max(0, System.Threading.Interlocked.Decrement(ref SeekerApplication._activeDownloadCount));
                    SeekerApplication.NotifyDownloadCountChanged(count);
                }
            }

            // add useroffline flag if appilcable (slsk.net no longer adds it)
            TransferStates state = e.Transfer.State | GetPeerFailureFlags(e.Transfer, isUpload);

            if (state.HasFlag(TransferStates.UserOffline))
            {
                //user offline.
                Seeker.Services.DownloadService.Instance.AddToUserOffline(e.Transfer.Username);
            }

            TransferItem relevantItem = TransferItems.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(e.Transfer?.Filename, e.Transfer?.Username, isUpload, out _);
            if (relevantItem == null)
            {
                Logger.InfoFirebase("relevantItem==null. state: " + e.Transfer.State.ToString());
            }
            Logger.Debug("TransferStateChanged for user: " + e.Transfer.Username + " file: " + e.Transfer.Filename + " new state: " + e.Transfer.State.ToString()
                + (e.Transfer.Exception != null ? " reason: " + SimpleHelpers.DescribeException(e.Transfer.Exception) : string.Empty));
            TransferItemManager.MarkTransfersDirty();
            TransferPersistenceWrapper.SaveTransferItems(false, 30);
            if (relevantItem != null)
            {
                //if the incoming transfer is not canclled, i.e. requested, then we replace the state (the user retried).
                if (e.Transfer.State.HasFlag(TransferStates.Cancelled) && relevantItem.State.HasFlag(TransferStates.FallenFromQueue))
                {
                    Logger.Debug("fallen from queue: cancelled " + relevantItem.State.ToString());
                    //the state is good as is.  do not add cancelled to it, since we used cancelled to mean "user cancelled" i.e. paused.
                    relevantItem.Failed = true;
                    relevantItem.Progress = 100;
                }
                else
                {
                    relevantItem.State = state;
                }
                // this comes from speed, which if we just changed state we do not know yet
                relevantItem.RemainingTime = null;
                // IncompleteParentUri and IncompleteUri are now set directly by DownloadFileAsync
                if (!relevantItem.State.HasFlag(TransferStates.Requested))
                {
                    relevantItem.InProcessing = true;
                }
            }

            if (e.Transfer.State.HasFlag(TransferStates.Errored) || e.Transfer.State.HasFlag(TransferStates.TimedOut) || e.Transfer.State.HasFlag(TransferStates.Rejected))
            {
                SpeedLimitHelper.RemoveDownloadUser(e.Transfer.Username);
                if (relevantItem == null)
                {
                    return;
                }
                relevantItem.Failed = true;
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Queued))
            {
                if (relevantItem == null)
                {
                    return;
                }
                if (!relevantItem.IsUpload() && e.Transfer.State.HasFlag(TransferStates.Remotely))
                {
                    Seeker.Services.DownloadService.Instance.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, true, true, relevantItem, null);
                }
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Initializing))
            {
                if (relevantItem == null)
                {
                    return;
                }
                //clear queued flag...
                relevantItem.QueueLength = int.MaxValue;
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Completed))
            {
                SpeedLimitHelper.RemoveDownloadUser(e.Transfer.Username);
                if (relevantItem == null)
                {
                    return;
                }
                if (!e.Transfer.State.HasFlag(TransferStates.Cancelled))
                {
                    //clear queued flag...
                    relevantItem.Progress = 100;
                    relevantItem.BytesTransferred = relevantItem.Size;
                    StateChangedForItem?.Invoke(null, relevantItem);
                }
                else //if it does have state cancelled we still want to update UI! (assuming we arent also clearing it)
                {
                    if (!relevantItem.CancelAndClearFlag)
                    {
                        StateChangedForItem?.Invoke(null, relevantItem);
                    }
                }

                if (e.Transfer.State.HasFlag(TransferStates.Succeeded))
                {
                    if (PreferencesState.NotifyOnFolderCompleted && !isUpload)
                    {
                        if (TransferItems.TransferItemManagerDL.IsFolderNowComplete(relevantItem, false))
                        {
                            //relevantItem.TransferItemExtra // if single then change the notif text.
                            // RetryDL is on completed Succeeded dl?
                            AppNotifications.ShowNotificationForCompletedFolder(relevantItem.FolderName, relevantItem.Username);
                        }
                    }

                    bool shouldAutoClear =
                        (!isUpload && PreferencesState.AutoClearCompleteDownloads) ||
                        (isUpload && PreferencesState.AutoClearCompleteUploads);
                    if (shouldAutoClear)
                    {
                        // Pre-compute the on-screen position BEFORE removal; after Remove,
                        // GetUserIndexForTransferItem can't find it.
                        int idx = TransferItems.TransferItemManagerWrapped.GetUserIndexForTransferItem(relevantItem);
                        Action action = () =>
                        {
                            TransferItems.TransferItemManagerWrapped.Remove(relevantItem);
                            TransfersFragment.UpdateBatchSelectedItemsIfApplicable(relevantItem);
                            ItemRemoved?.Invoke(null, new ItemRemovedEventArgs { Item = relevantItem, UserIndex = idx });
                        };
                        if (SeekerState.ActiveActivityRef != null)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(action);
                        }
                        else
                        {
                            action();
                        }
                    }
                }
            }
            else
            {
                if (relevantItem == null && (e.Transfer.State == TransferStates.Requested || e.Transfer.State == TransferStates.Aborted))
                {
                    return; //TODO sometimes this can happen too fast.  this is okay thouugh bc it will soon go to another state.
                }
                if (relevantItem == null && e.Transfer.State == TransferStates.InProgress)
                {
                    //THIS SHOULD NOT HAPPEN now that the race condition is resolved....
                    Logger.Firebase("relevantItem==null. state: " + e.Transfer.State.ToString());
                    return;
                }
                StateChangedForItem?.Invoke(null, relevantItem);
            }
        }

        // gets useroffline or cannot connect flags since slsk.net no longer adds it
        private static TransferStates GetPeerFailureFlags(Transfer transfer, bool isUpload)
        {
            if (isUpload || !transfer.State.HasFlag(TransferStates.Errored))
            {
                return TransferStates.None;
            }
            switch (DownloadFailureClassifier.Classify(transfer.Exception))
            {
                case DownloadFailureKind.UserOffline:
                    return TransferStates.UserOffline;
                case DownloadFailureKind.CannotConnect:
                    return TransferStates.CannotConnect;
                default:
                    return TransferStates.None;
            }
        }

        // Saves periodically. Republishes a UI-friendly ProgressUpdated event.
        private static void OnTransferProgressUpdated(object sender, TransferProgressUpdatedEventArgs e)
        {
            //Its possible to get a nullref here IF the system orientation changes..
            //throttle this maybe...

            //Logger.Debug("TRANSFER PROGRESS UPDATED"); //this typically happens once every 10 ms or even less and thats in debug mode.  in fact sometimes it happens 4 times in 1 ms.
            SeekerApplication.KickKeepAliveTimer();

            TransferItem relevantItem = null;
            if (TransferItems.TransferItemManagerDL == null)
            {
                Logger.Debug("transferItems Null " + e.Transfer.Filename);
                return;
            }

            TransferPersistenceWrapper.SaveTransferItems(false, 30);
            bool isUpload = e.Transfer.Direction == TransferDirection.Upload;
            relevantItem = TransferItems.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(e.Transfer.Filename, e.Transfer.Username, e.Transfer.Direction == TransferDirection.Upload, out _);

            if (relevantItem == null)
            {
                //this happens on Clear and Cancel All.
                Logger.Debug("Relevant Item Null " + e.Transfer.Filename);
                Logger.Debug("transferItems.IsEmpty " + TransferItems.TransferItemManagerDL.IsEmpty());
                return;
            }
            else
            {
                TransferItemManager.MarkTransfersDirty();
                double percentComplete = e.Transfer.PercentComplete;
                relevantItem.Progress = (int)percentComplete;
                relevantItem.BytesTransferred = e.Transfer.BytesTransferred;
                relevantItem.RemainingTime = e.Transfer.RemainingTime;
                relevantItem.AvgSpeed = e.Transfer.AverageSpeed;
                // a fresh transfer reports speed 0 for its first second; only a real sample is "recent"
                if (e.Transfer.AverageSpeed > 0)
                {
                    relevantItem.AvgSpeedSampledUtc = DateTime.UtcNow;
                }

                bool wasFailed = false;
                if (percentComplete != 0)
                {
                    wasFailed = false;
                    if (relevantItem.Failed)
                    {
                        wasFailed = true;
                        relevantItem.Failed = false;
                    }

                }

                ProgressUpdated?.Invoke(null, new ProgressUpdatedUIEventArgs(relevantItem, wasFailed, percentComplete));
            }
        }

        // update upload speed and create notification on success
        private static void OnUploadTransferStateChanged(object sender, TransferStateChangedEventArgs e)
        {
            if (e.Transfer == null || e.Transfer.Direction == TransferDirection.Download)
            {
                return;
            }
            TransferItemManager.MarkTransfersDirty();
            TransferPersistenceWrapper.SaveTransferItems(false, 30);
            if (e.Transfer.State == TransferStates.InProgress)
            {
                Logger.Debug("transfer state changed to in progress" + e.Transfer.Filename);
                //uploading file to user...
            }

            if (e.Transfer.State.HasFlag(TransferStates.Succeeded)) //todo rethink upload notifications....
            {
                Logger.Debug("transfer state changed to completed" + e.Transfer.Filename);
                //send notif successfully uploading file to user..
                //e.Transfer.AverageSpeed - speed in bytes/second
                if (e.Transfer.AverageSpeed <= 0 || ((int)(e.Transfer.AverageSpeed)) == 0)
                {
                    Logger.Debug("avg speed <= 0" + e.Transfer.Filename);
                    return;
                }
                Logger.Debug("sending avg speed of " + e.Transfer.AverageSpeed.ToString());
                try
                {
                    SeekerState.SoulseekClient.SendUploadSpeedAsync((int)(e.Transfer.AverageSpeed));
                }
                catch (Exception speedException)
                {
                    //throws synchronously if the server connection dropped while the upload was finishing
                    Logger.Debug("failed to send avg speed: " + speedException.Message);
                }
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, AppNotifications.CHANNEL_ID_UPLOAD_COMPLETED, AppNotifications.CHANNEL_NAME_UPLOAD_COMPLETED, NotificationImportance.High);
                    string directory = SimpleHelpers.GetFolderNameFromFile(e.Transfer.Filename.Replace("/", @"\")).ToString();
                    var notifInfo = Seeker.Services.UploadNotificationTracker.GetOrCreate(e.Transfer.Username, directory);

                    Notification n = Seeker.Services.UploadService.CreateUploadNotification(SeekerState.ActiveActivityRef, e.Transfer.Username, notifInfo.DirNames, notifInfo.FilesUploadedToUser);
                    NotificationManagerCompat nmc = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    nmc.Notify(e.Transfer.Username.GetHashCode(), n);
                }
                catch (Exception err)
                {
                    Logger.Firebase("Upload Notification Failed" + err.Message + err.StackTrace);
                }
            }
        }
    }
}
