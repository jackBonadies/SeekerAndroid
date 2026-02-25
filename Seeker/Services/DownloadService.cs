using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Java.IO;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.DocumentFile.Provider;
using Seeker.Helpers;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Android.Provider.DocumentsContract;

using Common;
namespace Seeker.Services
{
    // Download queue, enqueue, and continuation logic
    public static class DownloadService
    {
        public static void GetDownloadPlaceInQueueBatch(List<TransferItem> transferItems, bool addIfNotAdded)
        {

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    t.ContinueWith(new Action<Task>((Task t) =>
                    {
                        if (t.IsFaulted)
                        {
                            //if(!silent) //always silent..
                            //{
                            //    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                            //    {
                            //        if (SeekerState.ActiveActivityRef != null)
                            //        {
                            //            Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show();
                            //        }
                            //    });
                            //}
                            return;
                        }
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { GetDownloadPlaceInQueueBatchLogic(transferItems, addIfNotAdded); });
                    }));
                }
            }
            else
            {
                GetDownloadPlaceInQueueBatchLogic(transferItems, addIfNotAdded);
            }
        }


        public static void GetDownloadPlaceInQueueBatchLogic(List<TransferItem> transferItems, bool addIfNotAdded, Func<TransferItem, object> actionOnComplete = null)
        {
            foreach (TransferItem transferItem in transferItems)
            {
                GetDownloadPlaceInQueueLogic(transferItem.Username, transferItem.FullFilename, addIfNotAdded, true, transferItem, null);
            }
        }


        public static void GetDownloadPlaceInQueue(string username, string fullFileName, bool addIfNotAdded, bool silent, TransferItem transferItemInQuestion = null, Func<TransferItem, object> actionOnComplete = null)
        {

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    t.ContinueWith(new Action<Task>((Task t) =>
                    {
                        if (t.IsFaulted)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                if (SeekerState.ActiveActivityRef != null)
                                {
                                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                                }
                            });
                            return;
                        }
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { GetDownloadPlaceInQueueLogic(username, fullFileName, addIfNotAdded, silent, transferItemInQuestion, actionOnComplete); });
                    }));
                }
            }
            else
            {
                GetDownloadPlaceInQueueLogic(username, fullFileName, addIfNotAdded, silent, transferItemInQuestion, actionOnComplete);
            }
        }

        private static void GetDownloadPlaceInQueueLogic(string username, string fullFileName, bool addIfNotAdded, bool silent, TransferItem transferItemInQuestion = null, Func<TransferItem, object> actionOnComplete = null)
        {

            Action<Task<int>> updateTask = new Action<Task<int>>(
                (Task<int> t) =>
                {
                    if (t.IsFaulted)
                    {
                        bool transitionToNextState = false;
                        Soulseek.TransferStates state = TransferStates.Errored;
                        if (t.Exception?.InnerException is Soulseek.UserOfflineException uoe)
                        {
                            //Nicotine always immediately transitions from queued to user offline the second the user goes offline. We dont do it immediately but on next check.
                            //for QT you always are in "Queued" no matter what.
                            transitionToNextState = true;
                            state = TransferStates.Errored | TransferStates.UserOffline | TransferStates.FallenFromQueue;
                            if (!silent)
                            {
                                SeekerApplication.Toaster.ShowToastDebounced(string.Format(SeekerApplication.GetString(Resource.String.UserXIsOffline), username), "_6_", username);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            //Nicotine transitions from Queued to Cannot Connect IF you pause and resume. Otherwise you stay in Queued. Here if someone explicitly retries (i.e. silent = false) then we will transition states.
                            // otherwise, its okay, lets just stay in Queued.
                            //for QT you always are in "Queued" no matter what.
                            transitionToNextState = !silent;
                            state = TransferStates.Errored | TransferStates.CannotConnect | TransferStates.FallenFromQueue;
                            if (!silent)
                            {
                                SeekerApplication.Toaster.ShowToastDebounced(string.Format(SeekerApplication.GetString(Resource.String.CannotConnectUserX), username), "_7_", username);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException is System.TimeoutException)
                        {
                            transitionToNextState = false; //they may just not be sending queue position messages.  that is okay, we can still connect to them just fine for download time.
                            if (!silent)
                            {
                                SeekerApplication.Toaster.ShowToastDebounced(string.Format(SeekerApplication.GetString(Resource.String.TimeoutQueueUserX), username), "_8_", username, 6);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException.Message.Contains("underlying Tcp connection is closed"))
                        {
                            //can be server connection (get user endpoint) or peer connection.
                            transitionToNextState = false;
                            if (!silent)
                            {
                                SeekerApplication.Toaster.ShowToastDebounced(string.Format("Failed to get queue position for {0}: Connection was unexpectedly closed.", username), "_9_", username, 6);
                            }
                        }
                        else
                        {
                            if (!silent)
                            {
                                SeekerApplication.Toaster.ShowToastDebounced($"Error getting queue position from {username}", "_9_", username);
                            }
                            Logger.Firebase("GetDownloadPlaceInQueue" + t.Exception.ToString());
                        }

                        // 
                        if (transitionToNextState)
                        {
                            //update the transferItem array
                            if (transferItemInQuestion == null)
                            {
                                transferItemInQuestion = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
                            }

                            if (transferItemInQuestion == null)
                            {
                                return;
                            }
                            try
                            {
                                transferItemInQuestion.CancellationTokenSource.Cancel();
                            }
                            catch (Exception err)
                            {
                                Logger.Firebase("cancellation token src issue: " + err.Message);
                            }
                            transferItemInQuestion.State = state;
                            //let the Cancel() update it.
                            //TransferItemQueueUpdated?.Invoke(null, transferItemInQuestion); //if the transfer item fragment is bound then we update it..
                        }
                    }
                    else
                    {
                        bool queuePositionChanged = false;

                        //update the transferItem array
                        if (transferItemInQuestion == null)
                        {
                            transferItemInQuestion = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
                        }

                        if (transferItemInQuestion == null)
                        {
                            return;
                        }
                        else
                        {
                            queuePositionChanged = transferItemInQuestion.QueueLength != t.Result;

                            if (t.Result >= 0)
                            {
                                transferItemInQuestion.QueueLength = t.Result;
                            }
                            else
                            {
                                transferItemInQuestion.QueueLength = int.MaxValue;
                            }

                            if (queuePositionChanged)
                            {
                                Logger.Debug($"Queue Position of {fullFileName} has changed to {t.Result}");
                            }
                            else
                            {
                                Logger.Debug($"Queue Position of {fullFileName} is still {t.Result}");
                            }
                        }

                        if (actionOnComplete != null)
                        {
                            SeekerState.ActiveActivityRef?.RunOnUiThread(() => { actionOnComplete(transferItemInQuestion); });
                        }
                        else
                        {
                            if (queuePositionChanged)
                            {
                                TransferItemQueueUpdated?.Invoke(null, transferItemInQuestion); //if the transfer item fragment is bound then we update it..
                            }
                        }

                    }
                }
            );

            Task<int> getDownloadPlace = null;
            try
            {
                getDownloadPlace = SeekerState.SoulseekClient.GetDownloadPlaceInQueueAsync(username, fullFileName, null, transferItemInQuestion.ShouldEncodeFileLatin1(), transferItemInQuestion.ShouldEncodeFolderLatin1());
            }
            catch (TransferNotFoundException)
            {
                if (addIfNotAdded)
                {
                    //it is not downloading... therefore retry the download...
                    if (transferItemInQuestion == null)
                    {
                        transferItemInQuestion = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
                    }
                    //TransferItem item1 = transferItems[info.Position];  
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    try
                    {
                        transferItemInQuestion.QueueLength = int.MaxValue;
                        TransferState.SetupCancellationToken(transferItemInQuestion, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                        var dlInfo = new DownloadInfo(transferItemInQuestion.Username, transferItemInQuestion.FullFilename, transferItemInQuestion.Size, null, cancellationTokenSource, transferItemInQuestion.QueueLength, 0, transferItemInQuestion.GetDirectoryLevel()) { TransferItemReference = transferItemInQuestion };
                        Task task = TransfersUtil.DownloadFileAsync(transferItemInQuestion.Username, transferItemInQuestion.FullFilename, transferItemInQuestion.GetSizeForDL(), cancellationTokenSource, out _, dlInfo, isFileDecodedLegacy: transferItemInQuestion.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: transferItemInQuestion.ShouldEncodeFolderLatin1());
                        task.ContinueWith(DownloadContinuationActionUI(new DownloadAddedEventArgs(dlInfo)));
                    }
                    catch (DuplicateTransferException)
                    {
                        //happens due to button mashing...
                        return;
                    }
                    catch (System.Exception error)
                    {
                        Action a = new Action(() => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_) + error.Message, ToastLength.Long); });
                        if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                        {

                        }
                        else
                        {
                            Logger.Firebase(error.Message + " OnContextItemSelected");
                        }
                        if (!silent)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(a);
                        }
                        return; //otherwise null ref with task!
                    }
                    //TODO: THIS OCCURS TO SOON, ITS NOT gaurentted for the transfer to be in downloads yet...
                    try
                    {
                        getDownloadPlace = SeekerState.SoulseekClient.GetDownloadPlaceInQueueAsync(username, fullFileName, null, transferItemInQuestion.ShouldEncodeFileLatin1(), transferItemInQuestion.ShouldEncodeFolderLatin1());
                        getDownloadPlace.ContinueWith(updateTask);
                    }
                    catch (Exception e)
                    {
                        Logger.Firebase("you likely called getdownloadplaceinqueueasync too soon..." + e.Message);
                    }
                    return;
                }
                else
                {
                    Logger.Debug("Transfer Item we are trying to get queue position of is not currently being downloaded.");
                    return;
                }


            }
            catch (System.Exception e)
            {
                //Logger.Firebase("GetDownloadPlaceInQueue" + e.Message);
                return;
            }
            getDownloadPlace.ContinueWith(updateTask);
        }

        public static EventHandler<TransferItem> TransferItemQueueUpdated; //for transferItemPage to update its recyclerView

        public static event EventHandler<TransferItem> TransferAddedUINotify;
        public static Notification CreateUploadNotification(Context context, String username, List<String> directories, int numFiles)
        {
            string fileS = numFiles == 1 ? SeekerState.ActiveActivityRef.GetString(Resource.String.file) : SeekerState.ActiveActivityRef.GetString(Resource.String.files);
            string titleText = string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.upload_f_string), numFiles, fileS, username);
            string directoryString = string.Empty;
            if (directories.Count == 1)
            {
                directoryString = SeekerState.ActiveActivityRef.GetString(Resource.String.from_directory) + ": " + directories[0];
            }
            else
            {
                directoryString = SeekerState.ActiveActivityRef.GetString(Resource.String.from_directories) + ": " + directories[0];
                for (int i = 0; i < directories.Count; i++)
                {
                    if (i == 0)
                    {
                        continue;
                    }
                    directoryString += ", " + directories[i];
                }
            }
            string contextText = directoryString;
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            notifIntent.AddFlags(ActivityFlags.SingleTop);
            notifIntent.PutExtra(MainActivity.UPLOADS_NOTIF_EXTRA, 2);
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, username.GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            Notification notification = null;
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                notification =
                      new Notification.Builder(context, MainActivity.UPLOADS_CHANNEL_ID)
                      .SetContentTitle(titleText)
                      .SetContentText(contextText)
                      .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                      .SetContentIntent(pendingIntent)
                      .SetOnlyAlertOnce(true) //maybe
                      .SetTicker(titleText).Build();
            }
            else
            {
                notification =
#pragma warning disable CS0618 // Type or member is obsolete
                  new Notification.Builder(context)
#pragma warning restore CS0618 // Type or member is obsolete
                  .SetContentTitle(titleText)
                  .SetContentText(contextText)
                  .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                  .SetContentIntent(pendingIntent)
                  .SetOnlyAlertOnce(true) //maybe
                  .SetTicker(titleText).Build();
            }

            return notification;
        }

        /// <summary>
        ///     Invoked upon a remote request to download a file.    THE ORIGINAL BUT WITHOUT ITRANSFERTRACKER!!!!
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="endpoint">The IP endpoint of the requesting user.</param>
        /// <param name="filename">The filename of the requested file.</param>
      //  /// <param name="tracker">(for example purposes) the ITransferTracker used to track progress.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="DownloadEnqueueException">Thrown when the download is rejected.  The Exception message will be passed to the remote user.</exception>
        /// <exception cref="Exception">Thrown on any other Exception other than a rejection.  A generic message will be passed to the remote user for security reasons.</exception>
        public static Task EnqueueDownloadAction(string username, IPEndPoint endpoint, string filename)
        {
            if (SeekerApplication.IsUserInIgnoreList(username))
            {
                return Task.CompletedTask;
            }

            //if a user tries to download a file from our browseResponse then their filename will be
            //  "Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\y\\x\\09 Between Songs 4.mp3" 
            //so check if it contains the uploadDataDirectoryUri

            //if(filename.Contains(uploadDirfolderName))
            //{
            //    string newFolderName = Helpers.GetFolderNameFromFile(filename);
            //    string newFileName = Helpers.GetFileNameFromFile(filename);
            //    keyFilename = newFolderName + @"\" + newFileName;
            //}

            //the filename is basically "the key"
            _ = endpoint;
            string errorMsg = null;
            Tuple<long, string, Tuple<int, int, int, int>, bool, bool> ourFileInfo = SeekerState.SharedFileCache.GetFullInfoFromSearchableName(filename, out errorMsg);//SeekerState.SharedFileCache.FullInfo.Where((Tuple<string,string,long> fullInfoTuple) => {return fullInfoTuple.Item1 == keyFilename; }).FirstOrDefault(); //make this a method call GetFullInfo and check Aux dict
            if (ourFileInfo == null)
            {
                Logger.Firebase("ourFileInfo is null: " + ourFileInfo + " " + errorMsg);
                throw new DownloadEnqueueException($"File not found.");
            }

            DocumentFile ourFile = null;
            Android.Net.Uri ourUri = Android.Net.Uri.Parse(ourFileInfo.Item2);

            if (ourFileInfo.Item4 || ourFileInfo.Item5)
            {
                //locked or hidden (hidden shouldnt happen but just in case, it should still be userlist only)
                //CHECK USER LIST
                if (!SimpleHelpers.UserListService.ContainsUser(username))
                {
                    throw new DownloadEnqueueException($"File not shared");
                }
            }

            if (SeekerState.PreOpenDocumentTree() || !UploadDirectoryManager.IsFromTree(filename)) //IsFromTree method!
            {
                ourFile = DocumentFile.FromFile(new Java.IO.File(ourUri.Path));
            }
            else
            {
                ourFile = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, ourUri);
            }
            //var localFilename = filename.ToLocalOSPath();
            //var fileInfo = new FileInfo(localFilename);



            if (!ourFile.Exists())
            {
                //Console.WriteLine($"[UPLOAD REJECTED] File {localFilename} not found.");
                throw new DownloadEnqueueException($"File not found.");
            }

            //if (tracker.TryGet(TransferDirection.Upload, username, filename, out _))
            //{
            //    // in this case, a re-requested file is a no-op.  normally we'd want to respond with a 
            //    // PlaceInQueueResponse
            //    //Console.WriteLine($"[UPLOAD RE-REQUESTED] [{username}/{filename}]");
            //    return Task.CompletedTask;
            //}

            // create a new cancellation token source so that we can cancel the upload from the UI.
            var cts = new CancellationTokenSource();
            //var topts = new TransferOptions(stateChanged: (e) => tracker.AddOrUpdate(e, cts), progressUpdated: (e) => tracker.AddOrUpdate(e, cts), governor: (t, c) => Task.Delay(1, c));

            TransferItem transferItem = new TransferItem();
            transferItem.Username = username;
            transferItem.FullFilename = filename;
            transferItem.Filename = SimpleHelpers.GetFileNameFromFile(filename);
            transferItem.FolderName = Common.Helpers.GetFolderNameFromFile(filename);
            transferItem.CancellationTokenSource = cts;
            transferItem.Size = ourFile.Length();
            transferItem.isUpload = true;
            transferItem = TransfersFragment.TransferItemManagerUploads.AddIfNotExistAndReturnTransfer(transferItem, out bool exists);

            if (!exists) //else the state will simply be updated a bit later. 
            {
                TransferAddedUINotify?.Invoke(null, transferItem);
            }
            // accept all download requests, and begin the upload immediately.
            // normally there would be an internal queue, and uploads would be handled separately.
            Task.Run(async () =>
            {
                CancellationTokenSource oldCts = null;
                try
                {
                    //using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);

                    TransferState.SetupCancellationToken(transferItem, cts, out oldCts);

                    var uploadUri = ourFile.Uri;
                    await SeekerState.SoulseekClient.UploadAsync(username, filename, transferItem.Size,
                        inputStreamFactory: (_) => Task.FromResult<System.IO.Stream>(SeekerState.MainActivityRef.ContentResolver.OpenInputStream(uploadUri)),
                        options: new TransferOptions(governor: SpeedLimitHelper.OurUploadGovernor), cancellationToken: cts.Token); //THE FILENAME THAT YOU PASS INTO HERE MUST MATCH EXACTLY
                                                                                                                                   //ELSE THE CLIENT WILL REJECT IT.  //MUST MATCH EXACTLY THE ONE THAT WAS REQUESTED THAT IS..

                }
                catch (DuplicateTransferException dup) //not tested
                {
                    Logger.Debug("UPLOAD DUPL - " + dup.Message);
                    TransferState.SetupCancellationToken(transferItem, oldCts, out _); //if there is a duplicate you do not want to overwrite the good cancellation token with a meaningless one. so restore the old one.
                }
                catch (DuplicateTokenException dup)
                {
                    Logger.Debug("UPLOAD DUPL - " + dup.Message);
                    TransferState.SetupCancellationToken(transferItem, oldCts, out _); //if there is a duplicate you do not want to overwrite the good cancellation token with a meaningless one. so restore the old one.
                }
            }).ContinueWith(t =>
            {
                //Console.WriteLine($"[UPLOAD FAILED] {t.Exception}");
            }, TaskContinuationOptions.NotOnRanToCompletion); // fire and forget

            // return a completed task so that the invoking code can respond to the remote client.
            return Task.CompletedTask;
        }

        /// <summary>
        /// This RETURNS the task for Continuewith
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Action<Task> DownloadContinuationActionUI(DownloadAddedEventArgs e)
        {
            Action<Task> continuationActionSaveFile = new Action<Task>(
            task =>
            {
                Logger.Debug("DownloadContinuationActionUI started for " + e.dlInfo?.fullFilename + " with status: " + task.Status);
                try
                {
                    Action action = null;
                    if (task.IsCanceled)
                    {
                        Logger.Debug((DateTimeOffset.Now.ToUnixTimeMilliseconds() - SeekerState.TaskWasCancelledToastDebouncer).ToString());
                        if ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - SeekerState.TaskWasCancelledToastDebouncer) > 1000)
                        {
                            SeekerState.TaskWasCancelledToastDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        }

                        if (e.dlInfo.TransferItemReference.CancelAndRetryFlag) //if we pressed "Retry Download" and it was in progress so we first had to cancel...
                        {
                            e.dlInfo.TransferItemReference.CancelAndRetryFlag = false;
                            try
                            {
                                //retry download.
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                                TransferState.SetupCancellationToken(e.dlInfo.TransferItemReference, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                                var retryDlInfo = new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.TransferItemReference.Size, null, cancellationTokenSource, e.dlInfo.QueueLength, 0, task.Exception, e.dlInfo.Depth) { TransferItemReference = e.dlInfo.TransferItemReference };
                                Task retryTask = TransfersUtil.DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.TransferItemReference.Size, cancellationTokenSource, out _, retryDlInfo, 1, e.dlInfo.TransferItemReference.ShouldEncodeFileLatin1(), e.dlInfo.TransferItemReference.ShouldEncodeFolderLatin1());
                                retryTask.ContinueWith(DownloadService.DownloadContinuationActionUI(new DownloadAddedEventArgs(retryDlInfo)));
                            }
                            catch (System.Exception e)
                            {
                                //disconnected error
                                if (e is System.InvalidOperationException && e.Message.ToLower().Contains("server connection must be connected and logged in"))
                                {
                                    action = () => { SeekerApplication.Toaster.ShowToastDebounced(SeekerApplication.GetString(Resource.String.MustBeLoggedInToRetryDL), "_16_"); };
                                }
                                else
                                {
                                    Logger.Firebase("cancel and retry creation failed: " + e.Message + e.StackTrace);
                                }
                                if (action != null)
                                {
                                    SeekerState.ActiveActivityRef.RunOnUiThread(action);
                                }
                            }
                        }

                        if (e.dlInfo.TransferItemReference.CancelAndClearFlag)
                        {
                            Logger.Debug("continue with cleanup activity: " + e.dlInfo.fullFilename);
                            e.dlInfo.TransferItemReference.CancelAndRetryFlag = false;
                            e.dlInfo.TransferItemReference.InProcessing = false;
                            TransferItemManagerWrapper.PerformCleanupItem(e.dlInfo.TransferItemReference); //this way we are sure that the stream is closed.
                        }

                        return;
                    }
                    else if (task.Status == TaskStatus.Faulted)
                    {
                        bool retriable = false;
                        bool forceRetry = false;

                        // in the cases where there is mojibake, and you undo it, you still cannot download from Nicotine older client.
                        // reason being: the shared cache and disk do not match.
                        // so if you send them the filename on disk they will say it is not in the cache.
                        // and if you send them the filename from cache they will say they could not find it on disk.

                        //bool tryUndoMojibake = false; //this is still needed even with keeping track of encodings.
                        bool resetRetryCount = false;
                        var transferItem = e.dlInfo.TransferItemReference;
                        //bool wasTriedToUndoMojibake = transferItem.TryUndoMojibake;
                        //transferItem.TryUndoMojibake = false;
                        if (task.Exception.InnerException is System.TimeoutException)
                        {
                            action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.timeout_peer), ToastLength.Long); };
                        }
                        else if (task.Exception.InnerException is TransferSizeMismatchException sizeException)
                        {
                            // THIS SHOULD NEVER HAPPEN. WE FIX THE TRANSFER SIZE MISMATCH INLINE.

                            // update the size and rerequest.
                            // if we have partially downloaded the file already we need to delete it to prevent corruption.
                            Logger.Debug($"OLD SIZE {transferItem.Size} NEW SIZE {sizeException.RemoteSize}");
                            transferItem.Size = sizeException.RemoteSize;
                            e.dlInfo.Size = sizeException.RemoteSize;
                            retriable = true;
                            forceRetry = true;
                            resetRetryCount = true;
                            if (!string.IsNullOrEmpty(transferItem.IncompleteParentUri)/* && transferItem.Progress > 0*/)
                            {
                                try
                                {
                                    TransferItemManagerWrapper.PerformCleanupItem(transferItem);
                                }
                                catch (Exception ex)
                                {
                                    string exceptionString = "Failed to delete incomplete file on TransferSizeMismatchException: " + ex.ToString();
                                    Logger.Debug(exceptionString);
                                    Logger.Firebase(exceptionString);
                                }
                            }
                        }
                        else if (task.Exception.InnerException is DownloadDirectoryNotSetException || task.Exception?.InnerException?.InnerException is DownloadDirectoryNotSetException)
                        {
                            Transfers.TransfersUtil.MarkTransferItemAsDirNotSet(transferItem);
                            action = () => { SeekerApplication.Toaster.ShowToastDebounced(SeekerApplication.GetString(Resource.String.FailedDownloadDirectoryNotSet), "_17_"); };
                        }
                        else if (task.Exception.InnerException is Soulseek.TransferRejectedException tre) //derived class of TransferException...
                        {
                            //we go here when trying to download a locked file... (the exception only gets thrown on rejected with "not shared")
                            bool isFileNotShared = tre.Message.ToLower().Contains("file not shared");
                            // if we request a file from a soulseek NS client such as eÌe.jpg which when encoded in UTF fails to be decoded by Latin1
                            // soulseek NS will send TransferRejectedException "File Not Shared." with our filename (the filename will be identical).
                            // when we retry lets try a Latin1 encoding.  If no special characters this will not make any difference and it will be just a normal retry.
                            // we only want to try this once. and if it fails reset it to normal and do not try it again.
                            // if we encode the same way we decode, then such a thing will not occur.

                            // in the nicotine 3.1.1 and earlier, if we request a file such as "fÃ¶r", nicotine will encode it in Latin1.  We will
                            // decode it as UTF8, encode it back as UTF8 and then they will decode it as UTF-8 resulting in för".  So even though we encoded and decoded
                            // in the same way there can still be an issue.  If we force legacy it will be fixed.

                            //if (!wasTriedToUndoMojibake && isFileNotShared && HasNonASCIIChars(transferItem.FullFilename))
                            //{
                            //    tryUndoMojibake = true;
                            //    transferItem.TryUndoMojibake = true;
                            //    retriable = true;
                            //}


                            // always set this since it only shows if we DO NOT retry
                            if (isFileNotShared)
                            {
                                action = () => { SeekerApplication.Toaster.ShowToastDebounced(SeekerApplication.GetString(Resource.String.transfer_rejected_file_not_shared), "_2_"); }; //needed
                            }
                            else
                            {
                                action = () => { SeekerApplication.Toaster.ShowToastDebounced(SeekerApplication.GetString(Resource.String.transfer_rejected), "_2_"); }; //needed
                            }
                            Logger.Debug("rejected. is not shared: " + isFileNotShared);
                        }
                        else if (task.Exception.InnerException is Soulseek.TransferException)
                        {
                            action = () => { SeekerApplication.Toaster.ShowToastDebounced(string.Format(SeekerApplication.GetString(Resource.String.failed_to_establish_connection_to_peer), e.dlInfo.username), "_1_", e?.dlInfo?.username ?? string.Empty); };
                        }
                        else if (task.Exception.InnerException is Soulseek.UserOfflineException)
                        {
                            action = () => { SeekerApplication.Toaster.ShowToastDebounced(task.Exception.InnerException.Message, "_3_", e?.dlInfo?.username ?? string.Empty); }; //needed. "User x appears to be offline"
                        }
                        else if (task.Exception.InnerException is Soulseek.SoulseekClientException &&
                                task.Exception.InnerException.Message != null &&
                                task.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            Logger.Debug("Task Exception: " + task.Exception.InnerException.Message);
                            action = () => { SeekerApplication.Toaster.ShowToastDebounced(SeekerApplication.GetString(Resource.String.failed_to_establish_direct_or_indirect), "_4_"); };
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("read error: remote connection closed"))
                        {
                            retriable = true;
                            //Logger.Firebase("read error: remote connection closed"); //this is if someone cancels the upload on their end.
                            Logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.remote_conn_closed), ToastLength.Long); };
                            if (NetworkHandoffDetector.HasHandoffOccuredRecently())
                            {
                                resetRetryCount = true;
                            }
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("network subsystem is down"))
                        {
                            //Logger.Firebase("Network Subsystem is Down");
                            if (ConnectionReceiver.DoWeHaveInternet())//if we have internet again by the time we get here then its retriable. this is often due to handoff. handoff either causes this or "remote connection closed"
                            {
                                Logger.Debug("we do have internet");
                                action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.remote_conn_closed), ToastLength.Long); };
                                retriable = true;
                                if (NetworkHandoffDetector.HasHandoffOccuredRecently())
                                {
                                    resetRetryCount = true;
                                }
                            }
                            else
                            {
                                action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.network_down), ToastLength.Long); };
                            }
                            Logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);

                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("reported as failed by"))
                        {
                            // if we request a file from a soulseek NS client such as eÌÌÌe.jpg which when encoded in UTF fails to be decoded by Latin1
                            // soulseek NS will send UploadFailed with our filename (the filename will be identical).
                            // when we retry lets try a Latin1 encoding.  If no special characters this will not make any difference and it will be just a normal retry.
                            // we only want to try this once. and if it fails reset it to normal and do not try it again.
                            //if(!wasTriedToUndoMojibake && HasNonASCIIChars(transferItem.FullFilename))
                            //{
                            //    tryUndoMojibake = true;
                            //    transferItem.TryUndoMojibake = true;
                            //    retriable = true;
                            //}
                            retriable = true;
                            //Logger.Firebase("Reported as failed by uploader");
                            Logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.reported_as_failed), ToastLength.Long); };
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            //Logger.Firebase("failed to establish a direct or indirect message connection");
                            Logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { SeekerApplication.Toaster.ShowToastDebounced(SeekerApplication.GetString(Resource.String.failed_to_establish_direct_or_indirect), "_5_"); };
                        }
                        else
                        {
                            retriable = true;
                            //the server connection task.Exception.InnerException.Message.Contains("The server connection was closed unexpectedly") //this seems to be retry able
                            //or task.Exception.InnerException.InnerException.Message.Contains("The server connection was closed unexpectedly""
                            //or task.Exception.InnerException.Message.Contains("Transfer failed: Read error: Object reference not set to an instance of an object
                            bool unknownException = true;
                            if (task.Exception != null && task.Exception.InnerException != null)
                            {
                                //I get a lot of null refs from task.Exception.InnerException.Message


                                Logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                                if (task.Exception.InnerException.Message.StartsWith("Disk full.")) //is thrown by Stream.Close()
                                {
                                    action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_no_space), ToastLength.Long); };
                                    unknownException = false;
                                }



                                if (task.Exception.InnerException.InnerException != null && unknownException)
                                {

                                    if (task.Exception.InnerException.InnerException.Message.Contains("ENOSPC (No space left on device)") || task.Exception.InnerException.InnerException.Message.Contains("Read error: Disk full."))
                                    {
                                        action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_no_space), ToastLength.Long); };
                                        unknownException = false;
                                    }

                                    //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                    if (task.Exception.InnerException.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                                    {
                                        unknownException = false;
                                    }

                                    if (unknownException)
                                    {
                                        Logger.Firebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);
                                    }



                                    //this is to help with the collection was modified
                                    if (task.Exception.InnerException.InnerException.InnerException != null && unknownException)
                                    {
                                        Logger.InfoFirebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);
                                        var innerInner = task.Exception.InnerException.InnerException.InnerException;
                                        //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                        Logger.Firebase("Innerx3_Exception: " + innerInner.Message + innerInner.StackTrace);
                                        //this is to help with the collection was modified
                                    }
                                }

                                if (unknownException)
                                {
                                    if (task.Exception.InnerException.StackTrace.Contains("System.Xml.Serialization.XmlSerializationWriterInterpreter"))
                                    {
                                        if (task.Exception.InnerException.StackTrace.Length > 1201)
                                        {
                                            Logger.Firebase("xml Unhandled task exception 2nd part: " + task.Exception.InnerException.StackTrace.Skip(1000).ToString());
                                        }
                                        Logger.Firebase("xml Unhandled task exception: " + task.Exception.InnerException.Message + task.Exception.InnerException.StackTrace);
                                    }
                                    else
                                    {
                                        Logger.Firebase("dlcontaction Unhandled task exception: " + task.Exception.InnerException.Message + task.Exception.InnerException.StackTrace);
                                    }
                                }
                            }
                            else if (task.Exception != null && unknownException)
                            {
                                Logger.Firebase("Unhandled task exception (little info): " + task.Exception.Message);
                                Logger.Debug("Unhandled task exception (little info):" + task.Exception.Message);
                            }
                        }


                        if (forceRetry || ((resetRetryCount || e.dlInfo.RetryCount == 0) && (SeekerState.AutoRetryDownload) && retriable))
                        {
                            Logger.Debug("Retrying the Download" + e.dlInfo.fullFilename);
                            //Logger.Debug("!!! try undo mojibake " + tryUndoMojibake);
                            try
                            {
                                //retry download.
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                                TransferState.SetupCancellationToken(e.dlInfo.TransferItemReference, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                                var retryDlInfo = new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, null, cancellationTokenSource, e.dlInfo.QueueLength, resetRetryCount ? 0 : 1, task.Exception, e.dlInfo.Depth) { TransferItemReference = e.dlInfo.TransferItemReference };
                                Task retryTask = TransfersUtil.DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, cancellationTokenSource, out _, retryDlInfo, 1, e.dlInfo.TransferItemReference.ShouldEncodeFileLatin1(), e.dlInfo.TransferItemReference.ShouldEncodeFolderLatin1());
                                retryTask.ContinueWith(DownloadService.DownloadContinuationActionUI(new DownloadAddedEventArgs(retryDlInfo)));
                                return; //i.e. dont toast anything just retry.
                            }
                            catch (System.Exception e)
                            {
                                Logger.Firebase("retry creation failed: " + e.Message + e.StackTrace);
                                //if this happens at least log the normal message....
                            }

                        }

                        if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                        {
                            Logger.Firebase("auto retry failed: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString() + "new exception: " + task.Exception?.InnerException?.Message?.ToString());
                        }

                        //Action action2 = () => { MainActivity.ToastUI(task.Exception.ToString());};
                        //this.RunOnUiThread(action2);
                        if (action == null)
                        {
                            //action = () => { MainActivity.ToastUI(msgDebug1); MainActivity.ToastUI(msgDebug2); };
                            action = () => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_unspecified), ToastLength.Long); };
                        }
                        SeekerState.ActiveActivityRef.RunOnUiThread(action);
                        //System.Console.WriteLine(task.Exception.ToString());
                        return;
                    }
                    //failed downloads return before getting here...

                    if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                    {
                        Logger.Firebase("auto retry succeeded: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString());
                    }

                    if (!PreferencesState.DisableDownloadToastNotification)
                    {
                        action = () => { SeekerApplication.Toaster.ShowToast(SimpleHelpers.GetFileNameFromFile(e.dlInfo.fullFilename) + " " + SeekerApplication.GetString(Resource.String.FinishedDownloading), ToastLength.Long); };
                        SeekerState.ActiveActivityRef.RunOnUiThread(action);
                    }
                    string finalUri = string.Empty;
                    bool noSubfolder = e.dlInfo.TransferItemReference.TransferItemExtra.HasFlag(Transfers.TransferItemExtras.NoSubfolder);
                    if (e.dlInfo.OutputMemoryStream != null)
                    {
                        byte[] bytes = e.dlInfo.OutputMemoryStream.ToArray();
                        e.dlInfo.OutputMemoryStream.Dispose();
                        e.dlInfo.OutputMemoryStream = null;
                        string path = FileSystemService.SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, bytes, null, null, true, e.dlInfo.Depth, noSubfolder, out finalUri);
                        FileSystemService.SaveFileToMediaStore(path);
                    }
                    else if (e.dlInfo.TransferItemReference?.IncompleteUri != null)
                    {
                        //move file from incomplete to final location...
                        string path = FileSystemService.SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, null, Android.Net.Uri.Parse(e.dlInfo.TransferItemReference.IncompleteUri), Android.Net.Uri.Parse(e.dlInfo.TransferItemReference.IncompleteParentUri), false, e.dlInfo.Depth, noSubfolder, out finalUri);
                        FileSystemService.SaveFileToMediaStore(path);
                    }
                    else
                    {
                        Logger.Firebase("Very bad. No memory stream or incomplete URI available for saving file.");
                    }
                    e.dlInfo.TransferItemReference.IncompleteParentUri = null; //not needed anymore.
                    e.dlInfo.TransferItemReference.IncompleteUri = null;
                    e.dlInfo.TransferItemReference.FinalUri = finalUri;
                }
                finally
                {
                    e.dlInfo.TransferItemReference.InProcessing = false;
                }
            });
            return continuationActionSaveFile;
        }

    }
}
