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
                        string path = SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, bytes, null, null, true, e.dlInfo.Depth, noSubfolder, out finalUri);
                        SaveFileToMediaStore(path);
                    }
                    else if (e.dlInfo.TransferItemReference?.IncompleteUri != null)
                    {
                        //move file from incomplete to final location...
                        string path = SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, null, Android.Net.Uri.Parse(e.dlInfo.TransferItemReference.IncompleteUri), Android.Net.Uri.Parse(e.dlInfo.TransferItemReference.IncompleteParentUri), false, e.dlInfo.Depth, noSubfolder, out finalUri);
                        SaveFileToMediaStore(path);
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

        public static object lock_toplevel_ifexist_create = new object();
        public static object lock_album_ifexist_create = new object();

        public static void GetOrCreateIncompleteLocation(string username, string fullfilename, int depth, out Android.Net.Uri incompleteUri, out Android.Net.Uri parentUri, out long partialLength)
        {
            string name = SimpleHelpers.GetFileNameFromFile(fullfilename);
            //string dir = Helpers.GetFolderNameFromFile(fullfilename);
            string filePath = string.Empty;

            bool useDownloadDir = false;
            if (PreferencesState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
            {
                useDownloadDir = true;
            }
            bool useTempDir = false;
            if (SettingsActivity.UseTempDirectory())
            {
                useTempDir = true;
            }
            bool useCustomDir = false;
            if (SettingsActivity.UseIncompleteManualFolder())
            {
                useCustomDir = true;
            }

            bool fileExists = false;
            if (SeekerState.UseLegacyStorage() && (SeekerState.RootDocumentFile == null && useDownloadDir))
            {
                Java.IO.File incompleteDir = null;
                Java.IO.File musicDir = null;
                try
                {
                    string rootdir = string.Empty;
                    //if (SeekerState.RootDocumentFile==null)
                    //{
                    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //}
                    //else
                    //{
                    //    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //    rootdir = SeekerState.RootDocumentFile.Uri.Path; //returns junk...
                    //}

                    if (!(new Java.IO.File(rootdir)).Exists())
                    {
                        (new Java.IO.File(rootdir)).Mkdirs();
                    }
                    //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                    string incompleteDirString = rootdir + @"/Soulseek Incomplete/";
                    lock (lock_toplevel_ifexist_create)
                    {
                        incompleteDir = new Java.IO.File(incompleteDirString);
                        if (!incompleteDir.Exists())
                        {
                            //make it and add nomedia...
                            incompleteDir.Mkdirs();
                            CreateNoMediaFileLegacy(incompleteDirString);
                        }
                    }

                    string fullDir = rootdir + @"/Soulseek Incomplete/" + SimpleHelpers.GenerateIncompleteFolderName(username, fullfilename, depth); //+ @"/" + name;
                    musicDir = new Java.IO.File(fullDir);
                    lock (lock_album_ifexist_create)
                    {
                        if (!musicDir.Exists())
                        {
                            musicDir.Mkdirs();
                            CreateNoMediaFileLegacy(fullDir);
                        }
                    }
                    parentUri = Android.Net.Uri.Parse(new Java.IO.File(fullDir).ToURI().ToString());
                    filePath = fullDir + @"/" + name;
                    Java.IO.File f = new Java.IO.File(filePath);
                    if (f.Exists())
                    {
                        fileExists = true;
                        partialLength = f.Length();
                    }
                    else
                    {
                        partialLength = 0;
                    }
                    incompleteUri = Android.Net.Uri.Parse(new Java.IO.File(filePath).ToURI().ToString()); //using incompleteUri.Path gives you filePath :)
                }
                catch (Exception e)
                {
                    Logger.Firebase("Legacy Filesystem Issue: " + e.Message + e.StackTrace + System.Environment.NewLine + incompleteDir.Exists() + musicDir.Exists() + fileExists);
                    throw;
                }
            }
            else
            {
                DocumentFile folderDir1 = null; //this is the desired location.
                DocumentFile rootdir = null;

                bool diagRootDirExistsAndCanWrite = false;
                bool diagDidWeCreateSoulSeekDir = false;
                bool diagSlskDirExistsAfterCreation = false;
                bool rootDocumentFileIsNull = SeekerState.RootDocumentFile == null;

                if (rootDocumentFileIsNull)
                {
                    throw new DownloadDirectoryNotSetException();
                }

                //Logger.Debug("rootDocumentFileIsNull: " + rootDocumentFileIsNull);
                try
                {
                    if (useDownloadDir)
                    {
                        rootdir = SeekerState.RootDocumentFile;
                        Logger.Debug("using download dir" + rootdir.Uri.LastPathSegment);
                    }
                    else if (useTempDir)
                    {
                        Java.IO.File appPrivateExternal = SeekerState.ActiveActivityRef.GetExternalFilesDir(null);
                        rootdir = DocumentFile.FromFile(appPrivateExternal);
                        Logger.Debug("using temp incomplete dir");
                    }
                    else if (useCustomDir)
                    {
                        rootdir = SeekerState.RootIncompleteDocumentFile;
                        Logger.Debug("using custom incomplete dir" + rootdir.Uri.LastPathSegment);
                    }
                    else
                    {
                        Logger.Firebase("!! should not get here, no dirs");
                    }

                    if (!rootdir.Exists())
                    {
                        Logger.Firebase("rootdir (nonnull) does not exist: " + rootdir.Uri);
                        diagRootDirExistsAndCanWrite = false;
                        //dont know how to create self
                        //rootdir.CreateDirectory();
                    }
                    else if (!rootdir.CanWrite())
                    {
                        diagRootDirExistsAndCanWrite = false;
                        Logger.Firebase("rootdir (nonnull) exists but cant write: " + rootdir.Uri);
                    }
                    else
                    {
                        diagRootDirExistsAndCanWrite = true;
                    }


                    //string diagMessage10 = CheckPermissions(rootdir.Uri); //TEST CODE REMOVE

                    //var slskCompleteUri = rootdir.Uri + @"/Soulseek Complete/";
                    DocumentFile slskDir1 = null;
                    lock (lock_toplevel_ifexist_create)
                    {
                        slskDir1 = rootdir.FindFile("Soulseek Incomplete"); //does Soulseek Complete folder exist
                        if (slskDir1 == null || !slskDir1.Exists())
                        {
                            slskDir1 = rootdir.CreateDirectory("Soulseek Incomplete");
                            //slskDir1 = rootdir.FindFile("Soulseek Incomplete");
                            if (slskDir1 == null)
                            {
                                string diagMessage = CheckPermissions(rootdir.Uri);
                                Logger.Firebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                                Logger.InfoFirebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                            }
                            else if (!slskDir1.Exists())
                            {
                                Logger.Firebase("slskDir1 does not exist" + rootdir.Uri);
                            }
                            else if (!slskDir1.CanWrite())
                            {
                                Logger.Firebase("slskDir1 cannot write" + rootdir.Uri);
                            }
                            CreateNoMediaFile(slskDir1);
                        }
                    }
                    //slskDir1 = rootdir.FindFile("Soulseek Incomplete"); //if it does not exist you then have to get it again!!
                    //                                                  //else first time the song will be outside the directory
                    if (slskDir1 == null)
                    {
                        diagSlskDirExistsAfterCreation = false;
                        Logger.Firebase("slskDir1 is null");
                        Logger.InfoFirebase("slskDir1 is null");
                    }
                    else
                    {
                        diagSlskDirExistsAfterCreation = true;
                    }


                    string album_folder_name = SimpleHelpers.GenerateIncompleteFolderName(username, fullfilename, depth);
                    lock (lock_album_ifexist_create)
                    {
                        folderDir1 = slskDir1.FindFile(album_folder_name); //does the folder we want to save to exist
                        if (folderDir1 == null || !folderDir1.Exists())
                        {
                            folderDir1 = slskDir1.CreateDirectory(album_folder_name);
                            //folderDir1 = slskDir1.FindFile(album_folder_name); //if it does not exist you then have to get it again!!
                            if (folderDir1 == null)
                            {
                                string rootUri = string.Empty;
                                if (SeekerState.RootDocumentFile != null)
                                {
                                    rootUri = SeekerState.RootDocumentFile.Uri.ToString();
                                }
                                bool slskDirExistsWriteable = false;
                                if (slskDir1 != null)
                                {
                                    slskDirExistsWriteable = slskDir1.Exists() && slskDir1.CanWrite();
                                }
                                string diagMessage = CheckPermissions(slskDir1.Uri);
                                Logger.InfoFirebase("folderDir1 is null:" + album_folder_name + "root: " + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                                Logger.Firebase("folderDir1 is null:" + album_folder_name + "root: " + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                            }
                            else if (!folderDir1.Exists())
                            {
                                Logger.Firebase("folderDir1 does not exist:" + album_folder_name);
                            }
                            else if (!folderDir1.CanWrite())
                            {
                                Logger.Firebase("folderDir1 cannot write:" + album_folder_name);
                            }
                            CreateNoMediaFile(folderDir1);
                        }
                    }

                    //THE VALID GOOD flags for a directory is Supports Dir Create.  The write is off in all of my cases and not necessary..

                    //string diagMessage2 = CheckPermissions(slskDir1.Uri); //TEST CODE DELETE
                    //string diagMessage3 = CheckPermissions(folderDir1.Uri); //TEST CODE DELETE


                }
                catch (Exception e)
                {
                    string rootDirUri = SeekerState.RootDocumentFile?.Uri == null ? "null" : SeekerState.RootDocumentFile.Uri.ToString();
                    Logger.Firebase("Filesystem Issue: " + rootDirUri + " " + e.Message + diagSlskDirExistsAfterCreation + diagRootDirExistsAndCanWrite + diagDidWeCreateSoulSeekDir + rootDocumentFileIsNull + e.StackTrace);
                }

                if (rootdir == null && !SeekerState.UseLegacyStorage())
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.seeker_cannot_access_files), ToastLength.Long);
                }

                //BACKUP IF FOLDER DIR IS NULL
                if (folderDir1 == null)
                {
                    folderDir1 = rootdir; //use the root instead..
                }

                parentUri = folderDir1.Uri;

                filePath = folderDir1.Uri + @"/" + name;

                DocumentFile potentialFile = folderDir1.FindFile(name); //this will return null if does not exist!!
                if (potentialFile != null && potentialFile.Exists())  //dont do a check for length 0 because then it will go to else and create another identical file (2)
                {
                    partialLength = potentialFile.Length();
                    incompleteUri = potentialFile.Uri;
                }
                else
                {
                    partialLength = 0;
                    DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name); //on samsung api 19 it renames song.mp3 to song.mp3.mp3. //TODO fix this! (tho below api 29 doesnt use this path anymore)
                    //String: name of new document, without any file extension appended; the underlying provider may choose to append the extension.. Whoops...
                    incompleteUri = mFile.Uri; //nullref TODO TODO: if null throw custom exception so you can better handle it later on in DL continuation action
                }
            }
        }

        /// <summary>
        /// Opens a stream for writing to the incomplete file. Call after GetOrCreateIncompleteLocation.
        /// </summary>
        public static System.IO.Stream OpenIncompleteStream(Android.Net.Uri incompleteUri, long partialLength)
        {
            if (SeekerState.UseLegacyStorage() && incompleteUri.Scheme == "file")
            {
                string filePath = incompleteUri.Path;
                if (partialLength > 0)
                {
                    return new System.IO.FileStream(filePath, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.None);
                }
                else
                {
                    return System.IO.File.Create(filePath);
                }
            }
            else
            {
                System.IO.Stream stream;
                if (partialLength > 0)
                {
                    stream = SeekerState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri, "wa");
                }
                else
                {
                    stream = SeekerState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri);
                }

                return new PositionTrackingOutputStream(stream, 0);
            }
        }

        /// <summary>
        /// Check Permissions if things dont go right for better diagnostic info
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static string CheckPermissions(Android.Net.Uri folder)
        {
            if (SeekerState.ActiveActivityRef != null)
            {
                var cursor = SeekerState.ActiveActivityRef.ContentResolver.Query(folder, new string[] { DocumentsContract.Document.ColumnFlags }, null, null, null);
                int flags = 0;
                if (cursor.MoveToFirst())
                {
                    flags = cursor.GetInt(0);
                }
                cursor.Close();
                bool canWrite = (flags & (int)DocumentContractFlags.SupportsWrite) != 0;
                bool canDirCreate = (flags & (int)DocumentContractFlags.DirSupportsCreate) != 0;
                if (canWrite && canDirCreate)
                {
                    return "Can Write and DirSupportsCreate";
                }
                else if (canWrite)
                {
                    return "Can Write and not DirSupportsCreate";
                }
                else if (canDirCreate)
                {
                    return "Can not Write and can DirSupportsCreate";
                }
                else
                {
                    return "No permissions";
                }
            }
            return string.Empty;
        }

        public static string SaveToFile(
            string fullfilename, 
            string username, 
            byte[] bytes, 
            Android.Net.Uri uriOfIncomplete, 
            Android.Net.Uri parentUriOfIncomplete, 
            bool memoryMode, 
            int depth, 
            bool noSubFolder,
            out string finalUri)
        {
            string name = SimpleHelpers.GetFileNameFromFile(fullfilename);
            string dir = Common.Helpers.GetFolderNameFromFile(fullfilename, depth);
            string filePath = string.Empty;

            if (memoryMode && (bytes == null || bytes.Length == 0))
            {
                Logger.Firebase("EMPTY or NULL BYTE ARRAY in mem mode");
            }

            if (!memoryMode && uriOfIncomplete == null)
            {
                Logger.Firebase("no URI in file mode");
            }
            finalUri = string.Empty;
            if (SeekerState.UseLegacyStorage() &&
                (SeekerState.RootDocumentFile == null && !SettingsActivity.UseIncompleteManualFolder())) //if the user didnt select a complete OR incomplete directory. i.e. pure java files.  
            {

                //this method works just fine if coming from a temp dir.  just not a open doc tree dir.

                string rootdir = string.Empty;
                //if (PreferencesState.SaveDataDirectoryUri ==null || PreferencesState.SaveDataDirectoryUri == string.Empty)
                //{
                rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //}
                //else
                //{
                //    rootdir = PreferencesState.SaveDataDirectoryUri;
                //}
                if (!(new Java.IO.File(rootdir)).Exists())
                {
                    (new Java.IO.File(rootdir)).Mkdirs();
                }
                //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                string intermediateFolder = @"/";
                if (PreferencesState.CreateCompleteAndIncompleteFolders)
                {
                    intermediateFolder = @"/Soulseek Complete/";
                }
                if (PreferencesState.CreateUsernameSubfolders)
                {
                    intermediateFolder = intermediateFolder + username + @"/"; //TODO: escape? slashes? etc... can easily test by just setting username to '/' in debugger
                }
                string fullDir = rootdir + intermediateFolder + (noSubFolder ? "" : dir); //+ @"/" + name;
                Java.IO.File musicDir = new Java.IO.File(fullDir);
                musicDir.Mkdirs();
                filePath = fullDir + @"/" + name;
                Java.IO.File musicFile = new Java.IO.File(filePath);
                FileOutputStream stream = new FileOutputStream(musicFile);
                finalUri = musicFile.ToURI().ToString();
                if (memoryMode)
                {
                    stream.Write(bytes);
                    stream.Close();
                }
                else
                {
                    Java.IO.File inFile = new Java.IO.File(uriOfIncomplete.Path);
                    Java.IO.File inDir = new Java.IO.File(parentUriOfIncomplete.Path);
                    MoveFile(new FileInputStream(inFile), stream, inFile, inDir);
                }
            }
            else
            {
                bool useLegacyDocFileToJavaFileOverride = false;
                DocumentFile legacyRootDir = null;
                if (SeekerState.UseLegacyStorage() && SeekerState.RootDocumentFile == null && SettingsActivity.UseIncompleteManualFolder())
                {
                    //this means that even though rootfile is null, manual folder is set and is a docfile.
                    //so we must wrap the default root doc file.
                    string legacyRootdir = string.Empty;
                    //if (PreferencesState.SaveDataDirectoryUri ==null || PreferencesState.SaveDataDirectoryUri == string.Empty)
                    //{
                    legacyRootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //}
                    //else
                    //{
                    //    rootdir = PreferencesState.SaveDataDirectoryUri;
                    //}
                    Java.IO.File legacyRoot = (new Java.IO.File(legacyRootdir));
                    if (!legacyRoot.Exists())
                    {
                        legacyRoot.Mkdirs();
                    }

                    legacyRootDir = DocumentFile.FromFile(legacyRoot);

                    useLegacyDocFileToJavaFileOverride = true;

                }

                DocumentFile folderDir1 = null; //this is the desired location.
                DocumentFile rootdir = null;

                bool diagRootDirExists = true;
                bool diagDidWeCreateSoulSeekDir = false;
                bool diagSlskDirExistsAfterCreation = true;
                bool rootDocumentFileIsNull = SeekerState.RootDocumentFile == null;
                try
                {
                    rootdir = SeekerState.RootDocumentFile;

                    if (useLegacyDocFileToJavaFileOverride)
                    {
                        rootdir = legacyRootDir;
                    }

                    if (!rootdir.Exists())
                    {
                        diagRootDirExists = false;
                        //dont know how to create self
                        //rootdir.CreateDirectory();
                    }
                    //var slskCompleteUri = rootdir.Uri + @"/Soulseek Complete/";

                    DocumentFile slskDir1 = null;
                    if (PreferencesState.CreateCompleteAndIncompleteFolders)
                    {
                        slskDir1 = rootdir.FindFile("Soulseek Complete"); //does Soulseek Complete folder exist
                        if (slskDir1 == null || !slskDir1.Exists())
                        {
                            slskDir1 = rootdir.CreateDirectory("Soulseek Complete");
                            Logger.Debug("Creating Soulseek Complete");
                            diagDidWeCreateSoulSeekDir = true;
                        }

                        if (slskDir1 == null)
                        {
                            diagSlskDirExistsAfterCreation = false;
                        }
                        else if (!slskDir1.Exists())
                        {
                            diagSlskDirExistsAfterCreation = false;
                        }
                    }
                    else
                    {
                        slskDir1 = rootdir;
                    }

                    bool diagUsernameDirExistsAfterCreation = false;
                    bool diagDidWeCreateUsernameDir = false;
                    if (PreferencesState.CreateUsernameSubfolders)
                    {
                        DocumentFile tempUsernameDir1 = null;
                        lock (string.Intern("IfNotExistCreateAtomic_1"))
                        {
                            tempUsernameDir1 = slskDir1.FindFile(username); //does username folder exist
                            if (tempUsernameDir1 == null || !tempUsernameDir1.Exists())
                            {
                                tempUsernameDir1 = slskDir1.CreateDirectory(username);
                                Logger.Debug(string.Format("Creating {0} dir", username));
                                diagDidWeCreateUsernameDir = true;
                            }
                        }

                        if (tempUsernameDir1 == null)
                        {
                            diagUsernameDirExistsAfterCreation = false;
                        }
                        else if (!slskDir1.Exists())
                        {
                            diagUsernameDirExistsAfterCreation = false;
                        }
                        else
                        {
                            diagUsernameDirExistsAfterCreation = true;
                        }
                        slskDir1 = tempUsernameDir1;
                    }

                    if (depth == 1)
                    {
                        if(noSubFolder)
                        {
                            folderDir1 = slskDir1;
                        }
                        else
                        {
                            lock (string.Intern("IfNotExistCreateAtomic_2"))
                            {
                                folderDir1 = slskDir1.FindFile(dir); //does the folder we want to save to exist
                                if (folderDir1 == null || !folderDir1.Exists())
                                {
                                    Logger.Debug("Creating " + dir);
                                    folderDir1 = slskDir1.CreateDirectory(dir);
                                }
                                if (folderDir1 == null || !folderDir1.Exists())
                                {
                                    Logger.Firebase("folderDir is null or does not exists");
                                }
                            }
                        }
                    }
                    else
                    {
                        DocumentFile folderDirNext = null;
                        folderDir1 = slskDir1;
                        int _depth = depth;
                        while (_depth > 0)
                        {
                            var parts = dir.Split('\\');
                            string singleDir = parts[parts.Length - _depth];
                            lock (string.Intern("IfNotExistCreateAtomic_3"))
                            {
                                folderDirNext = folderDir1.FindFile(singleDir); //does the folder we want to save to exist
                                if (folderDirNext == null || !folderDirNext.Exists())
                                {
                                    Logger.Debug("Creating " + dir);
                                    folderDirNext = folderDir1.CreateDirectory(singleDir);
                                }
                                if (folderDirNext == null || !folderDirNext.Exists())
                                {
                                    Logger.Firebase("folderDir is null or does not exists, depth" + _depth);
                                }
                            }
                            folderDir1 = folderDirNext;
                            _depth--;
                        }
                    }
                    //folderDir1 = slskDir1.FindFile(dir); //if it does not exist you then have to get it again!!
                }
                catch (Exception e)
                {
                    Logger.Firebase("Filesystem Issue: " + e.Message + diagSlskDirExistsAfterCreation + diagRootDirExists + diagDidWeCreateSoulSeekDir + rootDocumentFileIsNull + PreferencesState.CreateUsernameSubfolders);
                }

                if (rootdir == null && !SeekerState.UseLegacyStorage())
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.seeker_cannot_access_files), ToastLength.Long);
                }

                //BACKUP IF FOLDER DIR IS NULL
                if (folderDir1 == null)
                {
                    folderDir1 = rootdir; //use the root instead..
                }

                filePath = folderDir1.Uri + @"/" + name;

                //Java.IO.File musicFile = new Java.IO.File(filePath);
                //FileOutputStream stream = new FileOutputStream(mFile);
                if (memoryMode)
                {
                    DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name);
                    finalUri = mFile.Uri.ToString();
                    System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                    stream.Write(bytes);
                    stream.Close();
                }
                else
                {

                    //106ms for 32mb
                    Android.Net.Uri uri = null;
                    if (SeekerState.PreMoveDocument() ||
                        SettingsActivity.UseTempDirectory() || //i.e. if use temp dir which is file: // rather than content: //
                        (SeekerState.UseLegacyStorage() && SettingsActivity.UseIncompleteManualFolder() && SeekerState.RootDocumentFile == null) || //i.e. if use complete dir is file: // rather than content: // but Incomplete is content: //
                        CommonHelpers.CompleteIncompleteDifferentVolume() || !PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree || !PreferencesState.SaveDataDirectoryUriIsFromTree)
                    {
                        try
                        {
                            DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name);
                            uri = mFile.Uri;
                            finalUri = mFile.Uri.ToString();
                            System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                            MoveFile(SeekerState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);
                        }
                        catch (Exception e)
                        {
                            Logger.Firebase("CRITICAL FILESYSTEM ERROR pre" + e.Message);
                            SeekerApplication.Toaster.ShowToast("Error Saving File", ToastLength.Long);
                            Logger.Debug(e.Message + " " + uriOfIncomplete.Path);
                        }
                    }
                    else
                    {
                        try
                        {
                            string realName = string.Empty;
                            if (SettingsActivity.UseIncompleteManualFolder()) //fix due to above^  otherwise "Play File" silently fails
                            {
                                var df = DocumentFile.FromSingleUri(SeekerState.ActiveActivityRef, uriOfIncomplete); //dont use name!!! in my case the name was .m4a but the actual file was .mp3!!
                                realName = df.Name;
                            }

                            uri = DocumentsContract.MoveDocument(SeekerState.ActiveActivityRef.ContentResolver, uriOfIncomplete, parentUriOfIncomplete, folderDir1.Uri); //ADDED IN API 24!!
                            DeleteParentIfEmpty(DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, parentUriOfIncomplete));
                            //"/tree/primary:musictemp/document/primary:music2/J when two different uri trees the uri returned from move document is a mismash of the two... even tho it actually moves it correctly.
                            //folderDir1.FindFile(name).Uri.Path is right uri and IsFile returns true...
                            if (SettingsActivity.UseIncompleteManualFolder()) //fix due to above^  otherwise "Play File" silently fails
                            {
                                uri = folderDir1.FindFile(realName).Uri; //dont use name!!! in my case the name was .m4a but the actual file was .mp3!!
                            }
                        }
                        catch (Exception e)
                        {
                            //move document fails if two different volumes:
                            //"Failed to move to /storage/1801-090D/Music/Soulseek Complete/folder/song.mp3"
                            //{content://com.android.externalstorage.documents/tree/primary%3A/document/primary%3ASoulseek%20Incomplete%2F/****.mp3}
                            //content://com.android.externalstorage.documents/tree/1801-090D%3AMusic/document/1801-090D%3AMusic%2FSoulseek%20Complete%2F/****}
                            if (e.Message.ToLower().Contains("already exists"))
                            {
                                try
                                {
                                    //set the uri to the existing file...
                                    var df = DocumentFile.FromSingleUri(SeekerState.ActiveActivityRef, uriOfIncomplete);
                                    string realName = df.Name;
                                    uri = folderDir1.FindFile(realName).Uri;

                                    if (folderDir1.Uri == parentUriOfIncomplete)
                                    {
                                        //case where SDCARD was full - all files were 0 bytes, folders could not be created, documenttree.CreateDirectory() returns null.
                                        //no errors until you tried to move it. then you would get "alreay exists" since (if Create Complete and Incomplete folders is checked and 
                                        //the incomplete dir isnt changed) then the destination is the same as the incomplete file (since the incomplete and complete folders
                                        //couldnt be created.  This error is misleading though so do a more generic error.
                                        SeekerApplication.Toaster.ShowToast($"Filesystem Error for file {realName}.", ToastLength.Long);
                                        Logger.Debug("complete and incomplete locations are the same");
                                    }
                                    else
                                    {
                                        SeekerApplication.Toaster.ShowToast(string.Format("File {0} already exists at {1}.  Delete it and try again if you want to overwrite it.", realName, uri.LastPathSegment.ToString()), ToastLength.Long);
                                    }
                                }
                                catch (Exception e2)
                                {
                                    Logger.Firebase("CRITICAL FILESYSTEM ERROR errorhandling " + e2.Message);
                                }

                            }
                            else
                            {
                                if (uri == null) //this means doc file failed (else it would be after)
                                {
                                    Logger.InfoFirebase("uri==null");
                                    //lets try with the non MoveDocument way.
                                    //this case can happen (for a legitimate reason) if:
                                    //  the user is on api <29.  they start downloading an album.  then while its downloading they set the download directory.  the manual one will be file:\\ but the end location will be content:\\
                                    try
                                    {

                                        DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name);
                                        uri = mFile.Uri;
                                        finalUri = mFile.Uri.ToString();
                                        Logger.InfoFirebase("retrying: incomplete: " + uriOfIncomplete + " complete: " + finalUri + " parent: " + parentUriOfIncomplete);
                                        //                                        Logger.InfoFirebase("using temp: " + 
                                        System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                                        MoveFile(SeekerState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);
                                    }
                                    catch (Exception secondTryErr)
                                    {
                                        Logger.Firebase("Legacy backup failed - CRITICAL FILESYSTEM ERROR pre" + secondTryErr.Message);
                                        SeekerApplication.Toaster.ShowToast("Error Saving File", ToastLength.Long);
                                        Logger.Debug(secondTryErr.Message + " " + uriOfIncomplete.Path);
                                    }
                                }
                                else
                                {
                                    Logger.InfoFirebase("uri!=null");
                                    Logger.Firebase("CRITICAL FILESYSTEM ERROR " + e.Message + " path child: " + Android.Net.Uri.Decode(uriOfIncomplete.ToString()) + " path parent: " + Android.Net.Uri.Decode(parentUriOfIncomplete.ToString()) + " path dest: " + Android.Net.Uri.Decode(folderDir1?.Uri?.ToString()));
                                    SeekerApplication.Toaster.ShowToast("Error Saving File", ToastLength.Long);
                                    Logger.Debug(e.Message + " " + uriOfIncomplete.Path); //Unknown Authority happens when source is file :/// storage/emulated/0/Android/data/com.companyname.andriodapp1/files/Soulseek%20Incomplete/
                                }
                            }
                        }
                        //throws "no static method with name='moveDocument' signature='(Landroid/content/ContentResolver;Landroid/net/Uri;Landroid/net/Uri;Landroid/net/Uri;)Landroid/net/Uri;' in class Landroid/provider/DocumentsContract;"
                    }

                    if (uri == null)
                    {
                        Logger.Firebase("DocumentsContract MoveDocument FAILED, override incomplete: " + PreferencesState.OverrideDefaultIncompleteLocations);
                    }

                    finalUri = uri.ToString();

                    //1220ms for 35mb so 10x slower
                    //DocumentFile mFile = folderDir1.CreateFile(@"audio/mp3", name);
                    //System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                    //MoveFile(SeekerState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);


                    //stopwatch.Stop();
                    //Logger.Debug("DocumentsContract.MoveDocument took: " + stopwatch.ElapsedMilliseconds);

                }
            }

            return filePath;
        }

        public static void MoveFile(System.IO.Stream from, System.IO.Stream to, Android.Net.Uri toDelete, Android.Net.Uri parentToDelete)
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = from.Read(buffer)) != 0) //C# does 0 for you've reached the end!
            {
                to.Write(buffer, 0, read);
            }
            from.Close();
            to.Flush();
            to.Close();

            if (SeekerState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || toDelete.Scheme == "file")
            {
                try
                {
                    if (!(new Java.IO.File(toDelete.Path)).Delete())
                    {
                        Logger.Firebase("Java.IO.File.Delete() failed to delete");
                    }
                }
                catch (Exception e)
                {
                    Logger.Firebase("Java.IO.File.Delete() threw" + e.Message + e.StackTrace);
                }
            }
            else
            {
                DocumentFile df = DocumentFile.FromSingleUri(SeekerState.ActiveActivityRef, toDelete); //this returns a file that doesnt exist with file ://

                if (!df.Delete()) //on API 19 this seems to always fail..
                {
                    Logger.Firebase("df.Delete() failed to delete");
                }
            }

            DocumentFile parent = null;
            if (SeekerState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || parentToDelete.Scheme == "file")
            {
                parent = DocumentFile.FromFile(new Java.IO.File(parentToDelete.Path));
            }
            else
            {
                parent = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, parentToDelete); //if from single uri then listing files will give unsupported operation exception...  //if temp (file: //)this will throw (which makes sense as it did not come from open tree uri)
            }
            DeleteParentIfEmpty(parent);
        }

        public static void DeleteParentIfEmpty(DocumentFile parent)
        {
            if (parent == null)
            {
                Logger.Firebase("null parent");
                return;
            }
            //Logger.Debug(parent.Name + "p name");
            //Logger.Debug(parent.ListFiles().ToString());
            //Logger.Debug(parent.ListFiles().Length.ToString());
            //foreach (var f in parent.ListFiles())
            //{
            //    Logger.Debug("child: " + f.Name);
            //}
            try
            {
                if (parent.ListFiles().Length == 1 && parent.ListFiles()[0].Name == ".nomedia")
                {
                    if (!parent.ListFiles()[0].Delete())
                    {
                        Logger.Firebase("parent.Delete() failed to delete .nomedia child...");
                    }
                    if (!parent.Delete())
                    {
                        Logger.Firebase("parent.Delete() failed to delete parent");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was outside"))
                {
                    //race condition between checking length of ListFiles() and indexing [0] (twice)
                }
                else
                {
                    throw ex; //this might be important..
                }
            }
        }


        public static void DeleteParentIfEmpty(Java.IO.File parent)
        {
            if (parent.ListFiles().Length == 1 && parent.ListFiles()[0].Name == ".nomedia")
            {
                if (!parent.ListFiles()[0].Delete())
                {
                    Logger.Firebase("LEGACY parent.Delete() failed to delete .nomedia child...");
                }
                if (!parent.Delete()) //this returns false... maybe delete .nomedia child??? YUP.  cannot delete non empty dir...
                {
                    Logger.Firebase("LEGACY parent.Delete() failed to delete parent");
                }
            }
        }


        public static void MoveFile(Java.IO.FileInputStream from, Java.IO.FileOutputStream to, Java.IO.File toDelete, Java.IO.File parent)
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = from.Read(buffer)) != -1) //unlike C# this method does -1 for no more bytes left..
            {
                to.Write(buffer, 0, read);
            }
            from.Close();
            to.Flush();
            to.Close();
            if (!toDelete.Delete())
            {
                Logger.Firebase("LEGACY df.Delete() failed to delete ()");
            }
            DeleteParentIfEmpty(parent);
            //Logger.Debug(toDelete.ParentFile.Name + ":" + toDelete.ParentFile.ListFiles().Length.ToString());
        }

        private static void SaveFileToMediaStore(string path)
        {
            //ContentValues contentValues = new ContentValues();
            //contentValues.Put(MediaStore.MediaColumns.DateAdded, Helpers.GetDateTimeNowSafe().Ticks / TimeSpan.TicksPerMillisecond);
            //contentValues.Put(MediaStore.Audio.AudioColumns.Data,path);
            //ContentResolver.Insert(MediaStore.Audio.Media.ExternalContentUri, contentValues);from


            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            Java.IO.File f = new Java.IO.File(path);
            Android.Net.Uri contentUri = Android.Net.Uri.FromFile(f);
            mediaScanIntent.SetData(contentUri);
            SeekerState.ActiveActivityRef.ApplicationContext.SendBroadcast(mediaScanIntent);
        }

        private static void CreateNoMediaFileLegacy(string atDirectory)
        {
            Java.IO.File noMediaRootFile = new Java.IO.File(atDirectory + @"/.nomedia");
            if (!noMediaRootFile.Exists())
            {
                noMediaRootFile.CreateNewFile();
            }
        }

        private static void CreateNoMediaFile(DocumentFile atDirectory)
        {
            atDirectory.CreateFile("nomedia/customnomedia", ".nomedia");
        }
    }
}
