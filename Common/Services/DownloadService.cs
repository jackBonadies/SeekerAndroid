using Seeker.Helpers;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Common;
namespace Seeker.Services
{
    // Owns the entire download lifecycle: initiate, queue-poll, complete/retry/save
    public class DownloadService
    {
        public static DownloadService Instance { get; set; }

        private readonly IToaster toaster;
        private readonly IFileSystemService fileSystemService;
        private readonly ISessionService sessionService;
        private readonly IMainThreadRunner mainThreadRunner;
        private readonly Func<ISoulseekClient> soulseekClientFactory;
        private readonly ILoggerBackend logger;
        private readonly INetworkStatus networkStatus;
        private long taskWasCancelledToastDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

        public event EventHandler<int> TransferItemChanged;
        public event EventHandler<Action> TransferListRefreshRequested;

        public DownloadService(IToaster toaster, IFileSystemService fileSystemService, ISessionService sessionService, IMainThreadRunner mainThreadRunner, Func<ISoulseekClient> soulseekClientFactory, ILoggerBackend logger, INetworkStatus networkStatus)
        {
            this.toaster = toaster ?? throw new ArgumentNullException(nameof(toaster));
            this.fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            this.sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            this.mainThreadRunner = mainThreadRunner ?? throw new ArgumentNullException(nameof(mainThreadRunner));
            this.soulseekClientFactory = soulseekClientFactory ?? throw new ArgumentNullException(nameof(soulseekClientFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.networkStatus = networkStatus ?? throw new ArgumentNullException(nameof(networkStatus));
        }

        public event EventHandler<DownloadAddedEventArgs> DownloadAddedUINotify;


        public void ClearDownloadAddedEventsFromTarget(object target)
        {
            if (DownloadAddedUINotify == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in DownloadAddedUINotify.GetInvocationList())
                {
                    if (d.Target == null) //i.e. static
                    {
                        continue;
                    }
                    if (d.Target.GetType() == target.GetType())
                    {
                        DownloadAddedUINotify -= (EventHandler<DownloadAddedEventArgs>)d;
                    }
                }
            }
        }

        public Task CreateDownloadAllTask(FullFileInfo[] files, bool queuePaused, string username)
        {
            if (username == PreferencesState.Username)
            {
                toaster.ShowToastLong(StringKey.cannot_download_from_self);
                return new Task(() => { }); //since we call start on the task, if we call Task.Completed or Task.Delay(0) it will crash...
            }

            Task task = new Task(() =>
            {
                EnqueueFiles(files, queuePaused, username);
            });

            return task;
        }

        public async Task EnqueueFiles(FullFileInfo[] files, bool queuePaused, string username)
        {
            bool allExist = true; //only show the transfer exists if all transfers in question do already exist
            var isSingle = files.Count() == 1;
            List<DownloadInfo> downloadInfos = new List<DownloadInfo>();
            foreach (FullFileInfo file in files)
            {
                var dlInfo = AddTransfer(username, file.FullFileName, file.Size, int.MaxValue, file.Depth, queuePaused, file.wasFilenameLatin1Decoded, file.wasFolderLatin1Decoded, isSingle, out bool transferExists);
                downloadInfos.Add(dlInfo);
                if (!transferExists)
                {
                    allExist = false;
                }
            }

            if (allExist)
            {
                toaster.ShowToastShort(StringKey.error_duplicate);
            }
            else
            {
                if (queuePaused)
                {
                    toaster.ShowToastShort(StringKey.QueuedForDownload);
                }
                else
                {
                    toaster.ShowToastShort(StringKey.download_is_starting);
                }
            }

            if (!allExist && !queuePaused)
            {
                await DownloadFiles(downloadInfos, files, username);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="files"></param>
        /// <param name="username"></param>
        /// <remarks>
        /// Previously we would fireoff DownloadFileAsync tasks one after another.
        /// This would cause files do download out of order and other side effects.
        /// Update the logic to be more similar to slskd.
        /// </remarks>
        private async Task DownloadFiles(List<DownloadInfo> dlInfos, FullFileInfo[] files, string username)
        {
            for (int i = 0; i < dlInfos.Count; i++)
            {
                var dlInfo = dlInfos[i];
                var file = files[i];
                var dlTask = DownloadFileAsync(username, file.FullFileName, file.Size, dlInfo.CancellationTokenSource, out Task waitForNext, dlInfo, file.Depth, file.wasFilenameLatin1Decoded, file.wasFolderLatin1Decoded);
                var e = new DownloadAddedEventArgs(dlInfo);
                Action<Task> continuationActionSaveFile = DownloadContinuationActionUI(e);
                dlTask.ContinueWith(continuationActionSaveFile);
                // wait for current download to update to queued / initialized or dltask to throw exception before kicking off next
                await waitForNext;
            }
        }


        /// <summary>
        /// Adds a transfer to the database. Does not
        /// </summary>
        public DownloadInfo AddTransfer(string username, string fname, long size, int queueLength, int depth, bool queuePaused, bool wasLatin1Decoded, bool wasFolderLatin1Decoded, bool isSingle, out bool errorExists)
        {
            errorExists = false;
            Task dlTask = null;
            System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource();
            bool exists = false;
            TransferItem transferItem = null;
            DownloadInfo downloadInfo = null;
            System.Threading.CancellationTokenSource oldCts = null;
            try
            {

                downloadInfo = new DownloadInfo(username, fname, size, dlTask, cancellationTokenSource, queueLength, 0, depth);

                transferItem = new TransferItem();
                transferItem.Filename = SimpleHelpers.GetFileNameFromFile(downloadInfo.fullFilename);
                transferItem.FolderName = Common.Helpers.GetFolderNameFromFile(downloadInfo.fullFilename, depth);
                transferItem.Username = downloadInfo.username;
                transferItem.FullFilename = downloadInfo.fullFilename;
                transferItem.Size = downloadInfo.Size;
                transferItem.QueueLength = downloadInfo.QueueLength;
                transferItem.WasFilenameLatin1Decoded = wasLatin1Decoded;
                transferItem.WasFolderLatin1Decoded = wasFolderLatin1Decoded;
                if (isSingle && PreferencesState.NoSubfolderForSingle)
                {
                    transferItem.TransferItemExtra = Transfers.TransferItemExtras.NoSubfolder;
                }

                if (!queuePaused)
                {
                    try
                    {
                        TransferState.SetupCancellationToken(transferItem, downloadInfo.CancellationTokenSource, out oldCts); //if its already there we dont add it..
                    }
                    catch (Exception errr)
                    {
                        logger.Firebase("concurrency issue: " + errr); //I think this is fixed by changing to concurrent dict but just in case...
                    }
                }
                transferItem = TransferItems.TransferItemManagerDL.AddIfNotExistAndReturnTransfer(transferItem, out exists);
                logger.Debug($"Adding Transfer To Database: {transferItem.Filename}");
                downloadInfo.TransferItemReference = transferItem;

                if (queuePaused)
                {
                    transferItem.State = TransferStates.Cancelled;
                    DownloadAddedUINotify?.Invoke(null, new DownloadAddedEventArgs(null));
                }
                else
                {
                    var e = new DownloadAddedEventArgs(downloadInfo);
                    DownloadAddedUINotify?.Invoke(null, e);
                }
            }
            catch (Exception e)
            {
                if (!exists)
                {
                    TransferItems.TransferItemManagerDL.Remove(transferItem); //if it did not previously exist then remove it..
                }
                else
                {
                    errorExists = exists;
                }
                if (oldCts != null)
                {
                    TransferState.SetupCancellationToken(transferItem, oldCts, out _); //put it back..
                }
            }
            return downloadInfo;
        }

        /// <summary>
        /// takes care of resuming incomplete downloads, switching between mem and file backed, creating the incompleteUri dir.
        /// its the same as the old SeekerState.SoulseekClient.DownloadAsync but with a few bells and whistles...
        /// </summary>
        public Task DownloadFileAsync(string username, string fullfilename, long? size, CancellationTokenSource cts, out Task waitForNext, DownloadInfo dlInfo, int depth = 1, bool isFileDecodedLegacy = false, bool isFolderDecodedLegacy = false) //an indicator for how much of the full filename to use...
        {
            var waitUntilEnqueue = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            logger.Debug($"DownloadFileAsync: {fullfilename}");
            Task dlTask = null;
            Action<(TransferStates PreviousState, Transfer Transfer)> updateForEnqueue = new Action<(TransferStates PreviousState, Transfer Transfer)>( (args) =>
            {
                if (args.Transfer.State.HasFlag(TransferStates.Queued) || args.Transfer.State == TransferStates.Initializing)
                {
                    logger.Debug($"Queued / Init: {fullfilename} We can proceed to download next file.");
                    waitUntilEnqueue.TrySetResult(true);
                }
            });
            if (PreferencesState.MemoryBackedDownload)
            {
                var memStream = new MemoryStream();
                if (dlInfo != null)
                {
                    dlInfo.OutputMemoryStream = memStream;
                }
                dlTask =
                    soulseekClientFactory().DownloadAsync(
                        username: username,
                        remoteFilename: fullfilename,
                        outputStreamFactory: () => Task.FromResult<Stream>((Stream)memStream),
                        size: size,
                        options: new TransferOptions(governor: SpeedLimitHelper.OurDownloadGovernor, stateChanged: updateForEnqueue),
                        cancellationToken: cts.Token);
            }
            else
            {
                long partialLength = 0;
                string incompleteUri = null;
                string incompleteUriDirectory = null;

                // documentFile work - run this on background thread
                dlTask = Task.Run(() =>
                {
                    fileSystemService.GetOrCreateIncompleteLocation(username, fullfilename, depth,
                        out incompleteUri, out incompleteUriDirectory, out partialLength);
                }).ContinueWith(setupTask =>
                {
                    // if GetOrCreateIncompleteLocation threw, rethrow
                    setupTask.GetAwaiter().GetResult();

                    if (dlInfo?.TransferItemReference != null)
                    {
                        dlInfo.TransferItemReference.IncompleteUri = incompleteUri;
                        dlInfo.TransferItemReference.IncompleteParentUri = incompleteUriDirectory;
                    }

                    return soulseekClientFactory().DownloadAsync(
                        username: username,
                        remoteFilename: fullfilename,
                        outputStreamFactory: () => Task.FromResult<System.IO.Stream>(
                            fileSystemService.OpenIncompleteStream(incompleteUri, partialLength)),
                        size: size,
                        startOffset: partialLength,
                        options: new TransferOptions(disposeOutputStreamOnCompletion: true,
                            governor: SpeedLimitHelper.OurDownloadGovernor, stateChanged: updateForEnqueue),
                        cancellationToken: cts.Token);
                }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
            }
            waitForNext = Task.WhenAny(waitUntilEnqueue.Task, dlTask);
            return dlTask;
        }


        public void MarkTransferItemAsDirNotSet(TransferItem item)
        {
            item.Failed = true;
            item.State = Soulseek.TransferStates.Errored;
            item.TransferItemExtra |= TransferItemExtras.DirNotSet;
            item.InProcessing = false;
        }

        public void GetDownloadPlaceInQueueBatch(List<TransferItem> transferItems, bool addIfNotAdded)
        {
            sessionService.RunWithReconnect(() => GetDownloadPlaceInQueueBatchLogic(transferItems, addIfNotAdded), silent: true);
        }


        public void GetDownloadPlaceInQueueBatchLogic(List<TransferItem> transferItems, bool addIfNotAdded, Func<TransferItem, object> actionOnComplete = null)
        {
            foreach (TransferItem transferItem in transferItems)
            {
                GetDownloadPlaceInQueueLogic(transferItem.Username, transferItem.FullFilename, addIfNotAdded, true, transferItem, null);
            }
        }


        public void GetDownloadPlaceInQueue(string username, string fullFileName, bool addIfNotAdded, bool silent, TransferItem transferItemInQuestion = null, Func<TransferItem, object> actionOnComplete = null)
        {

            sessionService.RunWithReconnect(() => GetDownloadPlaceInQueueLogic(username, fullFileName, addIfNotAdded, silent, transferItemInQuestion, actionOnComplete), silent: true);
        }

        private void GetDownloadPlaceInQueueLogic(string username, string fullFileName, bool addIfNotAdded, bool silent, TransferItem transferItemInQuestion = null, Func<TransferItem, object> actionOnComplete = null)
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
                                toaster.ShowToastDebounced(string.Format(toaster.GetString(StringKey.UserXIsOffline), username), "_6_", username);
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
                                toaster.ShowToastDebounced(string.Format(toaster.GetString(StringKey.CannotConnectUserX), username), "_7_", username);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException is System.TimeoutException)
                        {
                            transitionToNextState = false; //they may just not be sending queue position messages.  that is okay, we can still connect to them just fine for download time.
                            if (!silent)
                            {
                                toaster.ShowToastDebounced(string.Format(toaster.GetString(StringKey.TimeoutQueueUserX), username), "_8_", username, 6);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException.Message.Contains("underlying Tcp connection is closed"))
                        {
                            //can be server connection (get user endpoint) or peer connection.
                            transitionToNextState = false;
                            if (!silent)
                            {
                                toaster.ShowToastDebounced(string.Format("Failed to get queue position for {0}: Connection was unexpectedly closed.", username), "_9_", username, 6);
                            }
                        }
                        else
                        {
                            if (!silent)
                            {
                                toaster.ShowToastDebounced($"Error getting queue position from {username}", "_9_", username);
                            }
                            logger.Firebase("GetDownloadPlaceInQueue" + t.Exception.ToString());
                        }

                        // 
                        if (transitionToNextState)
                        {
                            //update the transferItem array
                            if (transferItemInQuestion == null)
                            {
                                transferItemInQuestion = TransferItems.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
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
                                logger.Firebase("cancellation token src issue: " + err.Message);
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
                            transferItemInQuestion = TransferItems.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
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
                                logger.Debug($"Queue Position of {fullFileName} has changed to {t.Result}");
                            }
                            else
                            {
                                logger.Debug($"Queue Position of {fullFileName} is still {t.Result}");
                            }
                        }

                        if (actionOnComplete != null)
                        {
                            mainThreadRunner.RunOnUiThread(() => { actionOnComplete(transferItemInQuestion); });
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
                getDownloadPlace = soulseekClientFactory().GetDownloadPlaceInQueueAsync(username, fullFileName, null, transferItemInQuestion.ShouldEncodeFileLatin1(), transferItemInQuestion.ShouldEncodeFolderLatin1());
            }
            catch (TransferNotFoundException)
            {
                if (addIfNotAdded)
                {
                    //it is not downloading... therefore retry the download...
                    if (transferItemInQuestion == null)
                    {
                        transferItemInQuestion = TransferItems.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
                    }
                    //TransferItem item1 = transferItems[info.Position];  
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    try
                    {
                        transferItemInQuestion.QueueLength = int.MaxValue;
                        TransferState.SetupCancellationToken(transferItemInQuestion, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                        var dlInfo = new DownloadInfo(transferItemInQuestion.Username, transferItemInQuestion.FullFilename, transferItemInQuestion.Size, null, cancellationTokenSource, transferItemInQuestion.QueueLength, 0, transferItemInQuestion.GetDirectoryLevel()) { TransferItemReference = transferItemInQuestion };
                        Task task = DownloadFileAsync(transferItemInQuestion.Username, transferItemInQuestion.FullFilename, transferItemInQuestion.GetSizeForDL(), cancellationTokenSource, out _, dlInfo, isFileDecodedLegacy: transferItemInQuestion.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: transferItemInQuestion.ShouldEncodeFolderLatin1());
                        task.ContinueWith(DownloadContinuationActionUI(new DownloadAddedEventArgs(dlInfo)));
                    }
                    catch (DuplicateTransferException)
                    {
                        //happens due to button mashing...
                        return;
                    }
                    catch (System.Exception error)
                    {
                        Action a = new Action(() => { toaster.ShowToastLong(toaster.GetString(StringKey.error_) + error.Message); });
                        if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                        {

                        }
                        else
                        {
                            logger.Firebase(error.Message + " OnContextItemSelected");
                        }
                        if (!silent)
                        {
                            mainThreadRunner.RunOnUiThread(a);
                        }
                        return; //otherwise null ref with task!
                    }
                    //TODO: THIS OCCURS TO SOON, ITS NOT gaurentted for the transfer to be in downloads yet...
                    try
                    {
                        getDownloadPlace = soulseekClientFactory().GetDownloadPlaceInQueueAsync(username, fullFileName, null, transferItemInQuestion.ShouldEncodeFileLatin1(), transferItemInQuestion.ShouldEncodeFolderLatin1());
                        getDownloadPlace.ContinueWith(updateTask);
                    }
                    catch (Exception e)
                    {
                        logger.Firebase("you likely called getdownloadplaceinqueueasync too soon..." + e.Message);
                    }
                    return;
                }
                else
                {
                    logger.Debug("Transfer Item we are trying to get queue position of is not currently being downloaded.");
                    return;
                }


            }
            catch (System.Exception e)
            {
                //logger.Firebase("GetDownloadPlaceInQueue" + e.Message);
                return;
            }
            getDownloadPlace.ContinueWith(updateTask);
        }

        public EventHandler<TransferItem> TransferItemQueueUpdated; //for transferItemPage to update its recyclerView

        /// <summary>
        /// This RETURNS the task for Continuewith
        /// This task does everything (show error, retry when applicable, save file to disk)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public Action<Task> DownloadContinuationActionUI(DownloadAddedEventArgs e)
        {
            Action<Task> continuationActionSaveFile = new Action<Task>(
            task =>
            {
                logger.Debug("DownloadContinuationActionUI started for " + e.dlInfo?.fullFilename + " with status: " + task.Status);
                try
                {
                    Action action = null;
                    if (task.IsCanceled)
                    {
                        logger.Debug((DateTimeOffset.Now.ToUnixTimeMilliseconds() - taskWasCancelledToastDebouncer).ToString());
                        if ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - taskWasCancelledToastDebouncer) > 1000)
                        {
                            taskWasCancelledToastDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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
                                Task retryTask = DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.TransferItemReference.Size, cancellationTokenSource, out _, retryDlInfo, 1, e.dlInfo.TransferItemReference.ShouldEncodeFileLatin1(), e.dlInfo.TransferItemReference.ShouldEncodeFolderLatin1());
                                retryTask.ContinueWith(DownloadContinuationActionUI(new DownloadAddedEventArgs(retryDlInfo)));
                            }
                            catch (System.Exception e)
                            {
                                //disconnected error
                                if (e is System.InvalidOperationException && e.Message.ToLower().Contains("server connection must be connected and logged in"))
                                {
                                    action = () => { toaster.ShowToastDebounced(StringKey.MustBeLoggedInToRetryDL, "_16_"); };
                                }
                                else
                                {
                                    logger.Firebase("cancel and retry creation failed: " + e.Message + e.StackTrace);
                                }
                                if (action != null)
                                {
                                    mainThreadRunner.RunOnUiThread(action);
                                }
                            }
                        }

                        if (e.dlInfo.TransferItemReference.CancelAndClearFlag)
                        {
                            logger.Debug("continue with cleanup activity: " + e.dlInfo.fullFilename);
                            e.dlInfo.TransferItemReference.CancelAndRetryFlag = false;
                            e.dlInfo.TransferItemReference.InProcessing = false;
                            TransferItems.TransferItemManagerWrapped.PerformCleanup(e.dlInfo.TransferItemReference); //this way we are sure that the stream is closed.
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
                            action = () => { toaster.ShowToastLong(StringKey.timeout_peer); };
                        }
                        else if (task.Exception.InnerException is TransferSizeMismatchException sizeException)
                        {
                            // THIS SHOULD NEVER HAPPEN. WE FIX THE TRANSFER SIZE MISMATCH INLINE.

                            // update the size and rerequest.
                            // if we have partially downloaded the file already we need to delete it to prevent corruption.
                            logger.Debug($"OLD SIZE {transferItem.Size} NEW SIZE {sizeException.RemoteSize}");
                            transferItem.Size = sizeException.RemoteSize;
                            e.dlInfo.Size = sizeException.RemoteSize;
                            retriable = true;
                            forceRetry = true;
                            resetRetryCount = true;
                            if (!string.IsNullOrEmpty(transferItem.IncompleteParentUri)/* && transferItem.Progress > 0*/)
                            {
                                try
                                {
                                    TransferItems.TransferItemManagerWrapped.PerformCleanup(transferItem);
                                }
                                catch (Exception ex)
                                {
                                    string exceptionString = "Failed to delete incomplete file on TransferSizeMismatchException: " + ex.ToString();
                                    logger.Debug(exceptionString);
                                    logger.Firebase(exceptionString);
                                }
                            }
                        }
                        else if (task.Exception.InnerException is DownloadDirectoryNotSetException || task.Exception?.InnerException?.InnerException is DownloadDirectoryNotSetException)
                        {
                            MarkTransferItemAsDirNotSet(transferItem);
                            action = () => { toaster.ShowToastDebounced(StringKey.FailedDownloadDirectoryNotSet, "_17_"); };
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
                                action = () => { toaster.ShowToastDebounced(StringKey.transfer_rejected_file_not_shared, "_2_"); }; //needed
                            }
                            else
                            {
                                action = () => { toaster.ShowToastDebounced(StringKey.transfer_rejected, "_2_"); }; //needed
                            }
                            logger.Debug("rejected. is not shared: " + isFileNotShared);
                        }
                        else if (task.Exception.InnerException is Soulseek.TransferException)
                        {
                            action = () => { toaster.ShowToastDebounced(string.Format(toaster.GetString(StringKey.failed_to_establish_connection_to_peer), e.dlInfo.username), "_1_", e?.dlInfo?.username ?? string.Empty); };
                        }
                        else if (task.Exception.InnerException is Soulseek.UserOfflineException)
                        {
                            action = () => { toaster.ShowToastDebounced(task.Exception.InnerException.Message, "_3_", e?.dlInfo?.username ?? string.Empty); }; //needed. "User x appears to be offline"
                        }
                        else if (task.Exception.InnerException is Soulseek.SoulseekClientException &&
                                task.Exception.InnerException.Message != null &&
                                task.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            logger.Debug("Task Exception: " + task.Exception.InnerException.Message);
                            action = () => { toaster.ShowToastDebounced(StringKey.failed_to_establish_direct_or_indirect, "_4_"); };
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("read error: remote connection closed"))
                        {
                            retriable = true;
                            //logger.Firebase("read error: remote connection closed"); //this is if someone cancels the upload on their end.
                            logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { toaster.ShowToastLong(StringKey.remote_conn_closed); };
                            if (networkStatus.HasHandoffOccuredRecently())
                            {
                                resetRetryCount = true;
                            }
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("network subsystem is down"))
                        {
                            //logger.Firebase("Network Subsystem is Down");
                            if (networkStatus.DoWeHaveInternet())//if we have internet again by the time we get here then its retriable. this is often due to handoff. handoff either causes this or "remote connection closed"
                            {
                                logger.Debug("we do have internet");
                                action = () => { toaster.ShowToastLong(StringKey.remote_conn_closed); };
                                retriable = true;
                                if (networkStatus.HasHandoffOccuredRecently())
                                {
                                    resetRetryCount = true;
                                }
                            }
                            else
                            {
                                action = () => { toaster.ShowToastLong(StringKey.network_down); };
                            }
                            logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);

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
                            //logger.Firebase("Reported as failed by uploader");
                            logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { toaster.ShowToastLong(StringKey.reported_as_failed); };
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            //logger.Firebase("failed to establish a direct or indirect message connection");
                            logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { toaster.ShowToastDebounced(StringKey.failed_to_establish_direct_or_indirect, "_5_"); };
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


                                logger.Debug("Unhandled task exception: " + task.Exception.InnerException.Message);
                                if (task.Exception.InnerException.Message.StartsWith("Disk full.")) //is thrown by Stream.Close()
                                {
                                    action = () => { toaster.ShowToastLong(StringKey.error_no_space); };
                                    unknownException = false;
                                }



                                if (task.Exception.InnerException.InnerException != null && unknownException)
                                {

                                    if (task.Exception.InnerException.InnerException.Message.Contains("ENOSPC (No space left on device)") || task.Exception.InnerException.InnerException.Message.Contains("Read error: Disk full."))
                                    {
                                        action = () => { toaster.ShowToastLong(StringKey.error_no_space); };
                                        unknownException = false;
                                    }

                                    //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                    if (task.Exception.InnerException.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                                    {
                                        unknownException = false;
                                    }

                                    if (unknownException)
                                    {
                                        logger.Firebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);
                                    }



                                    //this is to help with the collection was modified
                                    if (task.Exception.InnerException.InnerException.InnerException != null && unknownException)
                                    {
                                        logger.InfoFirebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);
                                        var innerInner = task.Exception.InnerException.InnerException.InnerException;
                                        //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                        logger.Firebase("Innerx3_Exception: " + innerInner.Message + innerInner.StackTrace);
                                        //this is to help with the collection was modified
                                    }
                                }

                                if (unknownException)
                                {
                                    if (task.Exception.InnerException.StackTrace.Contains("System.Xml.Serialization.XmlSerializationWriterInterpreter"))
                                    {
                                        if (task.Exception.InnerException.StackTrace.Length > 1201)
                                        {
                                            logger.Firebase("xml Unhandled task exception 2nd part: " + task.Exception.InnerException.StackTrace.Skip(1000).ToString());
                                        }
                                        logger.Firebase("xml Unhandled task exception: " + task.Exception.InnerException.Message + task.Exception.InnerException.StackTrace);
                                    }
                                    else
                                    {
                                        logger.Firebase("dlcontaction Unhandled task exception: " + task.Exception.InnerException.Message + task.Exception.InnerException.StackTrace);
                                    }
                                }
                            }
                            else if (task.Exception != null && unknownException)
                            {
                                logger.Firebase("Unhandled task exception (little info): " + task.Exception.Message);
                                logger.Debug("Unhandled task exception (little info):" + task.Exception.Message);
                            }
                        }


                        if (forceRetry || ((resetRetryCount || e.dlInfo.RetryCount == 0) && (PreferencesState.AutoRetryDownload) && retriable))
                        {
                            logger.Debug("Retrying the Download" + e.dlInfo.fullFilename);
                            //logger.Debug("!!! try undo mojibake " + tryUndoMojibake);
                            try
                            {
                                //retry download.
                                e.dlInfo.TransferItemReference.ClearStateForRetry();
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                                TransferState.SetupCancellationToken(e.dlInfo.TransferItemReference, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                                var retryDlInfo = new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, null, cancellationTokenSource, e.dlInfo.QueueLength, resetRetryCount ? 0 : 1, task.Exception, e.dlInfo.Depth) { TransferItemReference = e.dlInfo.TransferItemReference };
                                Task retryTask = DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, cancellationTokenSource, out _, retryDlInfo, 1, e.dlInfo.TransferItemReference.ShouldEncodeFileLatin1(), e.dlInfo.TransferItemReference.ShouldEncodeFolderLatin1());
                                retryTask.ContinueWith(DownloadContinuationActionUI(new DownloadAddedEventArgs(retryDlInfo)));
                                return; //i.e. dont toast anything just retry.
                            }
                            catch (System.Exception e)
                            {
                                logger.Firebase("retry creation failed: " + e.Message + e.StackTrace);
                                //if this happens at least log the normal message....
                            }

                        }

                        if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                        {
                            logger.Firebase("auto retry failed: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString() + "new exception: " + task.Exception?.InnerException?.Message?.ToString());
                        }

                        //Action action2 = () => { MainActivity.ToastUI(task.Exception.ToString());};
                        //this.RunOnUiThread(action2);
                        if (action == null)
                        {
                            //action = () => { MainActivity.ToastUI(msgDebug1); MainActivity.ToastUI(msgDebug2); };
                            action = () => { toaster.ShowToastLong(StringKey.error_unspecified); };
                        }
                        mainThreadRunner.RunOnUiThread(action);
                        //System.Console.WriteLine(task.Exception.ToString());
                        return;
                    }
                    //failed downloads return before getting here...

                    if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                    {
                        logger.Firebase("auto retry succeeded: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString());
                    }

                    if (!PreferencesState.DisableDownloadToastNotification)
                    {
                        action = () => { toaster.ShowToastLong(SimpleHelpers.GetFileNameFromFile(e.dlInfo.fullFilename) + " " + toaster.GetString(StringKey.FinishedDownloading)); };
                        mainThreadRunner.RunOnUiThread(action);
                    }
                    string finalUri = string.Empty;
                    bool noSubfolder = e.dlInfo.TransferItemReference.TransferItemExtra.HasFlag(Transfers.TransferItemExtras.NoSubfolder);
                    if (e.dlInfo.OutputMemoryStream != null)
                    {
                        byte[] bytes = e.dlInfo.OutputMemoryStream.ToArray();
                        e.dlInfo.OutputMemoryStream.Dispose();
                        e.dlInfo.OutputMemoryStream = null;
                        string path = fileSystemService.SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, bytes, null, null, true, e.dlInfo.Depth, noSubfolder, out finalUri);
                        fileSystemService.SaveFileToMediaStore(path);
                    }
                    else if (e.dlInfo.TransferItemReference?.IncompleteUri != null)
                    {
                        //move file from incomplete to final location...
                        string path = fileSystemService.SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, null, e.dlInfo.TransferItemReference.IncompleteUri, e.dlInfo.TransferItemReference.IncompleteParentUri, false, e.dlInfo.Depth, noSubfolder, out finalUri);
                        fileSystemService.SaveFileToMediaStore(path);
                    }
                    else
                    {
                        logger.Firebase("Very bad. No memory stream or incomplete URI available for saving file.");
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

        public void AddToUserOffline(string username)
        {
            if (TransferState.UsersWhereDownloadFailedDueToOffline.ContainsKey(username))
            {
                return;
            }
            else
            {
                lock (TransferState.UsersWhereDownloadFailedDueToOffline)
                {
                    TransferState.UsersWhereDownloadFailedDueToOffline[username] = 0x0;
                }
                try
                {
                    soulseekClientFactory().WatchUserAsync(username);
                }
                catch (System.Exception)
                {
                    // noop
                    // if user is not logged in then next time they log in the user will be added...
                }
            }
        }

        public void RetryAllFailed()
        {
            DownloadRetryAll(TransferItems.TransferItemManagerDL.GetListOfFailed().Select(tup => tup.Item1));
        }

        public void ResumeAllPaused()
        {
            DownloadRetryAll(TransferItems.TransferItemManagerDL.GetListOfPaused().Select(tup => tup.Item1));
        }

        public void RetryFailedFromFolder(FolderItem folder)
        {
            DownloadRetryAll(TransferItems.TransferItemManagerDL.GetListOfFailedFromFolder(folder).Select(tup => tup.Item1));
        }

        public void ResumePausedFromFolder(FolderItem folder)
        {
            DownloadRetryAll(TransferItems.TransferItemManagerDL.GetListOfPausedFromFolder(folder).Select(tup => tup.Item1));
        }

        /// <summary>
        /// Initiates a retry for a single transfer item. If the transfer is currently in-flight,
        /// sets CancelAndRetryFlag and cancels it (the continuation will re-download).
        /// Returns true if a fresh download was initiated, false if cancel-and-retry.
        /// </summary>
        public bool RetryDownloadItem(TransferItem item)
        {
            if (soulseekClientFactory().IsTransferInDownloads(item.Username, item.FullFilename))
            {
                item.CancelAndRetryFlag = true;
                if (item.CancellationTokenSource != null)
                {
                    if (!item.CancellationTokenSource.IsCancellationRequested)
                    {
                        item.CancellationTokenSource.Cancel();
                    }
                }
                else
                {
                    logger.Firebase("CTS is null. this should not happen. we should always set it before downloading.");
                }
                return false;
            }

            var cts = new CancellationTokenSource();
            TransferState.SetupCancellationToken(item, cts, out _);
            var dlInfo = new DownloadInfo(item.Username, item.FullFilename, item.Size, null, cts, item.QueueLength, item.Failed ? 1 : 0, item.GetDirectoryLevel()) { TransferItemReference = item };
            Task task = DownloadFileAsync(item.Username, item.FullFilename, item.GetSizeForDL(), cts, out _, dlInfo, isFileDecodedLegacy: item.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: item.ShouldEncodeFolderLatin1());
            task.ContinueWith(DownloadContinuationActionUI(new DownloadAddedEventArgs(dlInfo)));
            return true;
        }

        public void DownloadRetryAll(IEnumerable<TransferItem> transferItemConditionList)
        {
            var TransferItemManagerDL = TransferItems.TransferItemManagerDL;
            var ViewState = TransfersViewState.Instance;
            bool exceptionShown = false;
            foreach (TransferItem item in transferItemConditionList)
            {
                try
                {
                    RetryDownloadItem(item);
                }
                catch (DuplicateTransferException)
                {
                    //happens due to button mashing...
                    return;
                }
                catch (System.Exception error)
                {
                    Action a = new Action(() => { toaster.ShowToastLong(toaster.GetString(StringKey.error_) + error.Message); });
                    if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                    {

                    }
                    else
                    {
                        logger.Firebase(error.Message + " OnContextItemSelected");
                    }
                    if (!exceptionShown)
                    {
                        mainThreadRunner.RunOnUiThread(a);
                        exceptionShown = true;
                    }
                    return; //otherwise null ref with task!
                }
                item.ClearStateForRetry();
            }

            var refreshOnlySelected = new Action(() =>
            {
                mainThreadRunner.RunOnUiThread(
                    () =>
                    {
                        var uiState = ViewState.CreateDLUIState();
                        HashSet<int> indicesToUpdate = new HashSet<int>();
                        foreach (TransferItem ti in transferItemConditionList)
                        {
                            int pos = TransferItemManagerDL.GetUserIndexForTransferItem(ti, uiState);
                            if (pos == -1)
                            {
                                logger.Debug("pos == -1!!");
                                continue;
                            }

                            if (indicesToUpdate.Contains(pos))
                            {
                                logger.Debug($"skipping same pos {pos}");
                            }
                            else
                            {
                                indicesToUpdate.Add(pos);
                            }
                        }
                        if (ViewState.InUploadsMode)
                        {
                            return;
                        }
                        foreach (int i in indicesToUpdate)
                        {
                            logger.Debug($"updating {i}");
                            TransferItemChanged?.Invoke(null, i);
                        }



                    });
            });
            lock (TransferItemManagerDL.GetUICurrentList(ViewState.CreateDLUIState())) //TODO: test
            { //also can update this to do a partial refresh...
                TransferListRefreshRequested?.Invoke(null, refreshOnlySelected);
            }
        }

    }
}
