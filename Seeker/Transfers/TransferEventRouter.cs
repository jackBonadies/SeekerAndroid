using Android.App;
using AndroidX.Core.App;
using Common;
using Seeker.Helpers;
using Seeker.Services;
using Soulseek;
using System;

namespace Seeker.Transfers
{
    // Perform UI Agnostic Actions before routing to UI specific event handlers.
    internal static class TransferEventRouter
    {
        public static EventHandler<TransferItem> StateChangedForItem;
        public static EventHandler<ProgressUpdatedUIEventArgs> ProgressUpdated;

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

            if (!isUpload && e.Transfer.State.HasFlag(TransferStates.UserOffline))
            {
                //user offline.
                Seeker.Services.DownloadService.Instance.AddToUserOffline(e.Transfer.Username);
            }

            TransferItem relevantItem = TransferItems.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(e.Transfer?.Filename, e.Transfer?.Username, isUpload, out _);
            if (relevantItem == null)
            {
                Logger.InfoFirebase("relevantItem==null. state: " + e.Transfer.State.ToString());
            }
            Logger.Debug("TransferStateChanged for user: " + e.Transfer.Username + " file: " + e.Transfer.Filename + " new state: " + e.Transfer.State.ToString());
            TransferItemManager.MarkTransfersDirty();
            TransferPersistenceWrapper.SaveTransferItems(false, 30);
            if (relevantItem != null)
            {
                //if the incoming transfer is not canclled, i.e. requested, then we replace the state (the user retried).
                if (e.Transfer.State.HasFlag(TransferStates.Cancelled) && relevantItem.State.HasFlag(TransferStates.FallenFromQueue))
                {
                    Logger.Debug("fallen from queue");
                    //the state is good as is.  do not add cancelled to it, since we used cancelled to mean "user cancelled" i.e. paused.
                    relevantItem.Failed = true;
                    relevantItem.Progress = 100;
                }
                else
                {
                    relevantItem.State = e.Transfer.State;
                }
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
                if (!relevantItem.IsUpload())
                {
                    // TODO why is queue length max value
                    if (relevantItem.QueueLength != 0) //this means that it probably came from a search response where we know the users queuelength  ***BUT THAT IS NEVER THE ACTUAL QUEUE LENGTH*** its always much shorter...
                    {
                        Seeker.Services.DownloadService.Instance.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, true, true, relevantItem, null);
                    }
                    else //this means that it came from a browse response where we may not know the users initial queue length... or if its unexpectedly queued.
                    {
                        Seeker.Services.DownloadService.Instance.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, true, true, relevantItem, null);
                    }
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

        // Saves periodically. Autoclear if set. Call another ProgressUpdated Event
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
                bool fullRefresh = false;
                double percentComplete = e.Transfer.PercentComplete;
                relevantItem.Progress = (int)percentComplete;
                relevantItem.RemainingTime = e.Transfer.RemainingTime;
                relevantItem.AvgSpeed = e.Transfer.AverageSpeed;

                if (((PreferencesState.AutoClearCompleteDownloads && !isUpload) || (PreferencesState.AutoClearCompleteUploads && isUpload)) && System.Math.Abs(percentComplete - 100) < .001) //if 100% complete and autoclear //todo: autoclear on upload
                {

                    Action action = new Action(() =>
                    {
                        TransfersFragment.UpdateBatchSelectedItemsIfApplicable(relevantItem);
                        TransferItems.TransferItemManagerWrapped.Remove(relevantItem);//TODO: shouldnt we do the corresponding Adapter.NotifyRemoveAt. //this one doesnt need cleaning up, its successful..
                    });
                    if (SeekerState.ActiveActivityRef != null)
                    {
                        SeekerState.ActiveActivityRef?.RunOnUiThread(action);
                    }

                    fullRefresh = true;
                }
                else if (System.Math.Abs(percentComplete - 100) < .001)
                {
                    fullRefresh = true;
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

                ProgressUpdated?.Invoke(null, new ProgressUpdatedUIEventArgs(relevantItem, wasFailed, fullRefresh, percentComplete, e.Transfer.AverageSpeed));
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
            //if(e.Transfer.State == TransferStates.Completed) //this condition will NEVER be hit.  it is always completed | succeeded
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
                SeekerState.SoulseekClient.SendUploadSpeedAsync((int)(e.Transfer.AverageSpeed));
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, AppNotifications.CHANNEL_ID_UPLOAD_COMPLETED, AppNotifications.CHANNEL_NAME_UPLOAD_COMPLETED, NotificationImportance.High);
                    string directory = Common.Helpers.GetFolderNameFromFile(e.Transfer.Filename.Replace("/", @"\"));
                    var notifInfo = Seeker.Services.UploadNotificationTracker.GetOrCreate(e.Transfer.Username, directory);

                    Notification n = Seeker.Services.UploadService.CreateUploadNotification(SeekerState.ActiveActivityRef, e.Transfer.Username, notifInfo.DirNames, notifInfo.FilesUploadedToUser);
                    NotificationManagerCompat nmc = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    nmc.Notify(e.Transfer.Username.GetHashCode(), n);
                }
                catch (Exception err)
                {
                    Logger.Firebase("Upload Noficiation Failed" + err.Message + err.StackTrace);
                }
            }
        }
    }
}
