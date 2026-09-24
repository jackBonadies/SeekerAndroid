using Seeker.Helpers;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Concurrent;
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

        // item -> the request that owns it. full lifecycle from queued|locally (before slsk.net has knowledge) to continuation action
        private readonly ConcurrentDictionary<TransferItem, DownloadInfo> activeRequests = new ConcurrentDictionary<TransferItem, DownloadInfo>();

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

        /// <summary>
        /// Adds the files to the transfer list and kicks off the downloads.
        /// The returned task completes once the last file has been handed to the library.
        /// </summary>
        public Task EnqueueFilesAsync(FullFileInfo[] files, bool queuePaused, string username)
        {
            if (username == PreferencesState.Username)
            {
                toaster.ShowToastLong(StringKey.cannot_download_from_self);
                return Task.CompletedTask;
            }

            return Task.Run(() => EnqueueFiles(files, queuePaused, username));
        }

        /// <summary>
        /// Fire and forget entry point for the UI call sites. Completely asynchronous (including adding DLs to transfer list).
        /// </summary>
        public void EnqueueFilesFireAndForget(FullFileInfo[] files, bool queuePaused, string username)
        {
            EnqueueFilesAsync(files, queuePaused, username).ContinueWith(
                t => logger.Debug("EnqueueFiles failed: " + t.Exception?.InnerException),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task EnqueueFiles(FullFileInfo[] files, bool queuePaused, string username)
        {
            var isSingle = files.Count() == 1;
            List<DownloadInfo> downloadInfos = new List<DownloadInfo>();
            foreach (FullFileInfo file in files)
            {
                var dlInfo = AddTransfer(username, file.FullFileName, file.Size, int.MaxValue, file.Depth, queuePaused, file.wasFilenameLatin1Decoded, file.wasFolderLatin1Decoded, isSingle);
                if (dlInfo != null)
                {
                    downloadInfos.Add(dlInfo);
                }
            }

            // if every file already exists
            if (downloadInfos.Count == 0)
            {
                toaster.ShowToastShort(isSingle ? StringKey.error_duplicate : StringKey.error_duplicate_multiple);
                return;
            }

            if (queuePaused)
            {
                toaster.ShowToastShort(StringKey.QueuedForDownload);
                return;
            }

            toaster.ShowToastShort(StringKey.download_is_starting);
            await StartDownloads(downloadInfos);
        }

        // common entrypoint for downloads - parallel dl loops per user
        private Task StartDownloads(IEnumerable<DownloadInfo> dlInfos)
        {
            var byUser = new List<Task>();
            foreach (var group in dlInfos.GroupBy(d => d.username))
            {
                var userInfos = new List<DownloadInfo>();
                foreach (var dlInfo in group)
                {
                    if (dlInfo.TransferItemReference == null)
                    {
                        logger.Firebase($"StartDownloads: no transfer item for {dlInfo.fullFilename}, skipping");
                        continue;
                    }
                    if (!TryClaim(dlInfo))
                    {
                        logger.Debug($"StartDownloads: {dlInfo.fullFilename} is already being requested, skipping");
                        continue;
                    }
                    userInfos.Add(dlInfo);
                }
                if (userInfos.Count > 0)
                {
                    byUser.Add(Task.Run(() => DownloadFiles(userInfos, group.Key)));
                }
            }
            return Task.WhenAll(byUser);
        }

        // if doesnt exist, claims it and returns true (also returns true if we already own it)
        private bool TryClaim(DownloadInfo dlInfo)
        {
            var item = dlInfo.TransferItemReference;
            return activeRequests.TryAdd(item, dlInfo)
                || (activeRequests.TryGetValue(item, out var owner) && owner == dlInfo);
        }

        private void ReleaseClaim(DownloadInfo? dlInfo)
        {
            var item = dlInfo?.TransferItemReference;
            if (dlInfo == null || item == null)
            {
                return;
            }
            // still a dictionary lookup, just that it only removes if the value is the same
            ((ICollection<KeyValuePair<TransferItem, DownloadInfo>>)activeRequests).Remove(new KeyValuePair<TransferItem, DownloadInfo>(item, dlInfo));
        }

        private void StartDownloadsFireAndForget(IEnumerable<DownloadInfo> dlInfos)
        {
            StartDownloads(dlInfos).ContinueWith(
                t => logger.Info("StartDownloads failed: " + t.Exception?.InnerException),
                TaskContinuationOptions.OnlyOnFaulted);
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
        private const int SlotPending = 0;
        private const int SlotHandedOff = 1;
        private const int SlotCancelledWhilePending = 2;

        private async Task DownloadFiles(List<DownloadInfo> dlInfos, string username)
        {
            // rows waiting their turn unknown to both the library and not waiting on any cancellable task (they are waiting on
            // previous transfers to be queued), so we must register lambda here.
            // whoever moves a slot off SlotPending first owns that row.
            var slots = new int[dlInfos.Count];
            var registrations = new CancellationTokenRegistration[dlInfos.Count];
            for (int j = 0; j < dlInfos.Count; j++)
            {
                int idx = j;
                var pending = dlInfos[idx];
                // runs synchronously inside Cancel() on the UI thread and under the locks
                registrations[idx] = pending.CancellationTokenSource.Token.Register(() =>
                {
                    if (Interlocked.CompareExchange(ref slots[idx], SlotCancelledWhilePending, SlotPending) != SlotPending)
                    {
                        return;
                    }
                    // we got cancelled before we were started
                    try
                    {
                        CompleteUnstartedAsCancelled(pending);
                    }
                    catch (Exception ex)
                    {
                        logger.Firebase("cancel of pending download failed: " + ex);
                    }
                });
            }
            if (Array.IndexOf(slots, SlotCancelledWhilePending) >= 0)
            {
                // cancelled before we registered, the callback ran inline above
                mainThreadRunner.RunOnUiThread(() => TransferListRefreshRequested?.Invoke(null, null!));
            }

            Exception? peerFailure = null;
            bool anyUpdatedHere = false;
            for (int i = 0; i < dlInfos.Count; i++)
            {
                var dlInfo = dlInfos[i];
                Task dlTask;
                Task waitForNext;
                if (Interlocked.CompareExchange(ref slots[i], SlotHandedOff, SlotPending) != SlotPending)
                {
                    // paused/cancelled while waiting, its cancel already finished it
                    continue;
                }
                registrations[i].Dispose(); // we got to the transfer in question, now we handle lifecycle
                if (dlInfo.CancellationTokenSource.IsCancellationRequested)
                {
                    // cancelled between the claim and here - dont go ahead and create incomplete location, hand to library
                    CompleteUnstartedAsCancelled(dlInfo);
                    anyUpdatedHere = true;
                    continue;
                }
                if (peerFailure != null)
                {
                    // the last download failed with peer offline / unreachable. dont wait for every 
                    // other download to timeout, mark them offline here
                    MarkTransferItemPeerUnavailable(dlInfo.TransferItemReference, peerFailure);
                    anyUpdatedHere = true;
                    dlTask = Task.FromException(peerFailure);
                    waitForNext = Task.CompletedTask;
                }
                else
                {
                    try
                    {
                        dlTask = DownloadFileAsync(dlInfo, out waitForNext);
                    }
                    catch (Exception ex)
                    {
                        // we throw synchrnously in memory mode case when no longer connected to server.
                        // by catching we treat it like any other error
                        logger.Debug($"DownloadFileAsync threw synchronously for {dlInfo.fullFilename}: {ex.Message}");
                        dlTask = Task.FromException(ex);
                        waitForNext = Task.CompletedTask;
                    }
                }
                var e = new DownloadAddedEventArgs(dlInfo);
                Action<Task> continuationActionSaveFile = GetDownloadContinuationAction(e);
                dlTask.ContinueWith(continuationActionSaveFile);
                if (peerFailure != null)
                {
                    continue;
                }
                // wait for the remote client to acknowledge the request or for the dl to complete (i.e. faulted)
                await waitForNext;
                // if the previous download failed because the peer is offline / unreachable then dont wait for the
                //   timeout serially, otherwise if we download say 20 files we will have to wait a full 200s for the
                //   final one to have their status set properly. fail the rest with the same error instead.
                if (dlTask.IsFaulted && TryGetPeerFailure(dlTask.Exception, out peerFailure))
                {
                    logger.Debug($"{username} unavailable, failing the remaining {dlInfos.Count - i - 1} downloads");
                }
            }
            if (anyUpdatedHere)
            {
                TransferItemManager.MarkTransfersDirty();
                mainThreadRunner.RunOnUiThread(() => TransferListRefreshRequested?.Invoke(null, null!));
            }
        }

        private static bool TryGetPeerFailure(AggregateException ex, out Exception? inner)
        {
            inner = null;
            var kind = DownloadFailureClassifier.Classify(ex);
            if (kind != DownloadFailureKind.UserOffline && kind != DownloadFailureKind.CannotConnect)
            {
                return false;
            }
            inner = ex?.InnerException;
            return inner != null;
        }

        // mirrors what TransferEventRouter does for the library's Completed | Errored state, including the
        // UserOffline / CannotConnect flag it derives from Transfer.Exception - the same state the real attempt
        // for the first file in the batch produced.
        private static void MarkTransferItemPeerUnavailable(TransferItem? item, Exception peerFailure)
        {
            if (item == null)
            {
                return;
            }
            item.State = TransferStates.Completed | TransferStates.Errored;
            if (peerFailure is UserOfflineException)
            {
                item.State |= TransferStates.UserOffline;
            }
            else
            {
                item.State |= TransferStates.CannotConnect;
            }
            item.Failed = true;
            item.InProcessing = false;
            item.RemainingTime = null;
        }

        private static void MarkTransferItemCancelled(TransferItem item)
        {
            item.State = TransferStates.Completed | TransferStates.Cancelled;
            item.InProcessing = false;
            item.RemainingTime = null;
        }

        // never handed to the library, so TransferStateChanged not handle it
        private void CompleteUnstartedAsCancelled(DownloadInfo dlInfo)
        {
            MarkTransferItemCancelled(dlInfo.TransferItemReference);
            TransferItemManager.MarkTransfersDirty();
            Task.FromCanceled(dlInfo.CancellationTokenSource.Token)
                .ContinueWith(GetDownloadContinuationAction(new DownloadAddedEventArgs(dlInfo)), TaskScheduler.Default);
        }

        /// <summary>
        /// Adds a transfer to the list (i.e. for NEW items). Null if there is already a transfer either in motion or succeeded (and therefore we should not do anything)
        /// </summary>
        public DownloadInfo? AddTransfer(string username, string fname, long size, int queueLength, int depth, bool queuePaused, bool wasLatin1Decoded, bool wasFolderLatin1Decoded, bool isSingle)
        {
            var newItem = new TransferItem();
            newItem.Filename = SimpleHelpers.GetFileNameFromFile(fname).ToString();
            newItem.FolderName = SimpleHelpers.GetFolderNameFromFile(fname, depth).ToString();
            newItem.Username = username;
            newItem.FullFilename = fname;
            newItem.Size = size;
            newItem.QueueLength = queueLength;
            newItem.WasFilenameLatin1Decoded = wasLatin1Decoded;
            newItem.WasFolderLatin1Decoded = wasFolderLatin1Decoded;
            if (isSingle && PreferencesState.NoSubfolderForSingle)
            {
                newItem.TransferItemExtra = Transfers.TransferItemExtras.NoSubfolder;
            }
            newItem.State = queuePaused ? TransferStates.Cancelled : TransferStates.Queued | TransferStates.Locally;

            var transferItem = TransferItems.TransferItemManagerDL.AddIfNotExistAndReturnTransfer(newItem, out bool exists);
            DownloadInfo? downloadInfo;
            if (exists)
            {
                // If succeeded, dont re download just to fail (file already exists).
                if (queuePaused || transferItem.State.HasFlag(TransferStates.Succeeded))
                {
                    logger.Debug($"AddTransfer: {transferItem.Filename} already exists ({transferItem.State}), skipping");
                    return null;
                }
                // re-request of a paused / failed / finished row is a retry of that row. null if a request already owns it (i.e. it is already in motion)
                downloadInfo = PrepareRetry(transferItem, restartActive: false);
                if (downloadInfo == null)
                {
                    logger.Debug($"AddTransfer: {transferItem.Filename} is already being requested, skipping");
                    return null;
                }
            }
            else
            {
                downloadInfo = new DownloadInfo(username, fname, size, null, new CancellationTokenSource(), queueLength, 0, depth) { TransferItemReference = transferItem };
                if (!queuePaused)
                {
                    // a concurrent add of the same file saw our row as existing and got there first
                    if (!TryClaim(downloadInfo))
                    {
                        return null;
                    }
                    try
                    {
                        TransferState.SetupCancellationToken(transferItem, downloadInfo.CancellationTokenSource, out _);
                    }
                    catch (Exception errr)
                    {
                        logger.Firebase("concurrency issue: " + errr);
                    }
                }
            }
            logger.Debug($"Adding Transfer To Database: {transferItem.Filename}");
            DownloadAddedUINotify?.Invoke(null, new DownloadAddedEventArgs(queuePaused ? null : downloadInfo));
            return downloadInfo;
        }

        /// <summary>
        /// takes care of resuming incomplete downloads, switching between mem and file backed, creating the incompleteUri dir.
        /// its the same as the old SeekerState.SoulseekClient.DownloadAsync but with a few bells and whistles...
        /// </summary>
        private Task DownloadFileAsync(DownloadInfo dlInfo, out Task waitForNext)
        {
            string username = dlInfo.username;
            string fullfilename = dlInfo.fullFilename;
            long? size = dlInfo.TransferItemReference.GetSizeForDL();
            CancellationTokenSource cts = dlInfo.CancellationTokenSource;
            int depth = dlInfo.Depth;

            var waitUntilEnqueue = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            logger.Debug($"DownloadFileAsync: {fullfilename}");
            Task dlTask = null;
            Action<(TransferStates PreviousState, Transfer Transfer)> updateForEnqueue = new Action<(TransferStates PreviousState, Transfer Transfer)>( (args) =>
            {
                if (args.Transfer.State.HasFlag(TransferStates.Queued) && args.Transfer.State.HasFlag(TransferStates.Remotely))
                {
                    logger.Debug($"Queued | Remotely: {fullfilename} We can proceed to download next file.");
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
                            governor: SpeedLimitHelper.OurDownloadGovernor, stateChanged: updateForEnqueue,
                            // we are not seekable, however, we already set our streams to start writing
                            // at partialLength/startOffset.  This way we tell the library we are handling it.
                            seekOutputStreamAutomatically: false),
                        cancellationToken: cts.Token);
                }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();
            }
            waitForNext = Task.WhenAny(waitUntilEnqueue.Task, dlTask);
            return dlTask;
        }


        // only once the library is done with the item, i.e. its output stream is closed
        private void DeleteIncompleteFile(TransferItem item, string reason)
        {
            if (string.IsNullOrEmpty(item.IncompleteParentUri))
            {
                // memory backed or never created the incomplete location (i.e. was stuck behind a not yet started transfer)
                return;
            }
            try
            {
                TransferItems.TransferItemManagerWrapped.PerformCleanup(item);
            }
            catch (Exception ex)
            {
                string exceptionString = "Failed to delete incomplete file " + reason + ": " + ex.ToString();
                logger.Debug(exceptionString);
                logger.Firebase(exceptionString);
            }
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
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException.Message.Contains(SimpleHelpers.FailedToEstablishDirectOrIndirectString, StringComparison.OrdinalIgnoreCase))
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

                        logger.Debug($"queue position check for {fullFileName} from {username} failed: {SimpleHelpers.DescribeException(t.Exception)}"
                            + (transitionToNextState ? $" -> cancelling the download and marking it {state}" : " -> leaving the download as is"));

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
                    try
                    {
                        // null if a request for it is already pending
                        var dlInfo = PrepareRetry(transferItemInQuestion, restartActive: false);
                        if (dlInfo != null)
                        {
                            dlInfo.RetryCount = 0;
                            StartDownloadsFireAndForget(new[] { dlInfo });
                        }
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
                logger.Debug($"queue position check for {fullFileName} from {username} not sent: {SimpleHelpers.DescribeException(e)}");
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
        public Action<Task> GetDownloadContinuationAction(DownloadAddedEventArgs e)
        {
            Action<Task> continuationActionSaveFile = new Action<Task>(
            task =>
            {
                // before any retry below re-claims it
                ReleaseClaim(e.dlInfo);
                // protects against rare edge case where we own a transfer which faulted but has yet to reach the continuation action,
                //   and so it is still in activeRequests.  We go to retry it (and so we cancel it and set the retry flag).  It will
                //   then have the retry flag set but will never hit the cancelled branch (and so never retry).  Then next time we pause or
                //   cancel and clear it, the stale CancelAndRetryFlag will cause it to redownload.
                bool retryRequested = e.dlInfo.TransferItemReference.CancelAndRetryFlag;
                e.dlInfo.TransferItemReference.CancelAndRetryFlag = false;
                logger.Debug("DownloadContinuationActionUI started for " + e.dlInfo?.fullFilename + " with status: " + task.Status
                    + (task.IsFaulted ? " reason: " + SimpleHelpers.DescribeException(task.Exception) : string.Empty));
                var failureKind = task.IsFaulted ? DownloadFailureClassifier.Classify(task.Exception) : DownloadFailureKind.Unknown;
                if (failureKind == DownloadFailureKind.Duplicate)
                {
                    // nothing for us to do - the other transfer is currently processing
                    logger.Debug($"{e.dlInfo?.fullFilename} is already being processed");
                    mainThreadRunner.RunOnUiThread(() => { toaster.ShowToastDebounced(StringKey.error_duplicate, "duplicate"); });
                    return;
                }
                try
                {
                    Action action = null;
                    if (task.IsCanceled)
                    {
                        logger.Debug("Cancelled Delta: " + (DateTimeOffset.Now.ToUnixTimeMilliseconds() - taskWasCancelledToastDebouncer).ToString());
                        if ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - taskWasCancelledToastDebouncer) > 1000)
                        {
                            taskWasCancelledToastDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        }

                        if (e.dlInfo.TransferItemReference.CancelAndClearFlag)
                        {
                            // takes precedence over CancelAndRetry
                            logger.Debug("continue with cleanup activity: " + e.dlInfo.fullFilename);
                            DeleteIncompleteFile(e.dlInfo.TransferItemReference, "on cancel and clear");
                        }
                        else if (retryRequested) //if we pressed "Retry Download" and it was in progress so we first had to cancel...
                        {
                            try
                            {
                                //retry download.
                                var retryDlInfo = PrepareRetry(e.dlInfo.TransferItemReference, restartActive: false);
                                if (retryDlInfo != null)
                                {
                                    StartDownloadsFireAndForget(new[] { retryDlInfo });
                                }
                            }
                            catch (System.Exception e)
                            {
                                //disconnected error
                                if (e is System.InvalidOperationException && e.Message.Contains("server connection must be connected and logged in", StringComparison.OrdinalIgnoreCase))
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

                        return;
                    }
                    else if (task.Status == TaskStatus.Faulted)
                    {
                        HandleDownloadFaultAndRetryIfApplicable(e, task, failureKind);
                        return;
                    }
                    //failed downloads return before getting here...

                    if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                    {
                        logger.Firebase("auto retry succeeded: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString());
                    }

                    if (!PreferencesState.DisableDownloadToastNotification)
                    {
                        action = () => { toaster.ShowToastLong(SimpleHelpers.GetFileNameFromFile(e.dlInfo.fullFilename).ToString() + " " + toaster.GetString(StringKey.FinishedDownloading)); };
                        mainThreadRunner.RunOnUiThread(action);
                    }
                    string finalUri = string.Empty;
                    bool noSubfolder = e.dlInfo.TransferItemReference.TransferItemExtra.HasFlag(Transfers.TransferItemExtras.NoSubfolder);
                    if (e.dlInfo.OutputMemoryStream != null)
                    {
                        e.dlInfo.OutputMemoryStream.TryGetBuffer(out ArraySegment<byte> bytes);
                        string path = fileSystemService.SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, bytes, null, null, true, e.dlInfo.Depth, noSubfolder, out finalUri);
                        e.dlInfo.OutputMemoryStream.Dispose();
                        e.dlInfo.OutputMemoryStream = null;
                        fileSystemService.SaveFileToMediaStore(path);
                    }
                    else if (e.dlInfo.TransferItemReference?.IncompleteUri != null)
                    {
                        //move file from incomplete to final location...
                        string path = fileSystemService.SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, default, e.dlInfo.TransferItemReference.IncompleteUri, e.dlInfo.TransferItemReference.IncompleteParentUri, false, e.dlInfo.Depth, noSubfolder, out finalUri);
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

        private void HandleDownloadFaultAndRetryIfApplicable(DownloadAddedEventArgs e, Task task, DownloadFailureKind kind)
        {
            var transferItem = e.dlInfo.TransferItemReference;
            string username = e.dlInfo.username;
            Action? action = null;
            bool retriable = false;
            bool forceRetry = false;
            bool resetRetryCount = false;
            switch (kind)
            {
                case DownloadFailureKind.TimedOut:
                    action = () => { toaster.ShowToastLong(StringKey.timeout_peer); };
                    break;
                case DownloadFailureKind.SizeMismatch:
                {
                    // update the size and rerequest. delete any partial file to prevent corruption.
                    var sizeException = (TransferSizeMismatchException)DownloadFailureClassifier.GetCause(task.Exception)!;
                    logger.Debug($"OLD SIZE {transferItem.Size} NEW SIZE {sizeException.RemoteSize}");
                    transferItem.Size = sizeException.RemoteSize;
                    e.dlInfo.Size = sizeException.RemoteSize;
                    forceRetry = true;
                    resetRetryCount = true;
                    DeleteIncompleteFile(transferItem, "on TransferSizeMismatchException");
                    break;
                }
                case DownloadFailureKind.DirectoryNotSet:
                    MarkTransferItemAsDirNotSet(transferItem);
                    action = () => { toaster.ShowToastDebounced(StringKey.FailedDownloadDirectoryNotSet, "_17_"); };
                    break;
                case DownloadFailureKind.RejectedNotShared:
                    // can be due to locked file or mojibake
                    action = () => { toaster.ShowToastDebounced(StringKey.transfer_rejected_file_not_shared, "_2_"); };
                    break;
                case DownloadFailureKind.Rejected:
                    action = () => { toaster.ShowToastDebounced(StringKey.transfer_rejected, "_2_"); };
                    break;
                case DownloadFailureKind.UserOffline:
                    action = () => { toaster.ShowToastDebounced(string.Format(toaster.GetString(StringKey.UserXIsOffline), username), "_3_", username); };
                    break;
                case DownloadFailureKind.CannotConnect:
                    action = () => { toaster.ShowToastDebounced(StringKey.failed_to_establish_direct_or_indirect, "_4_"); };
                    break;
                case DownloadFailureKind.NotLoggedIn:
                    action = () => { toaster.ShowToastDebounced(StringKey.must_be_logged_to_download, "_18_"); };
                    break;
                case DownloadFailureKind.ReportedFailed:
                    retriable = true;
                    action = () => { toaster.ShowToastLong(StringKey.reported_as_failed); };
                    break;
                case DownloadFailureKind.ConnectionClosed:
                    // also if someone cancels the upload on their end
                    retriable = true;
                    resetRetryCount = networkStatus.HasHandoffOccuredRecently();
                    action = () => { toaster.ShowToastLong(StringKey.remote_conn_closed); };
                    break;
                case DownloadFailureKind.NetworkDown:
                    // if we have internet again by the time we get here then its retriable. this is often due to handoff.
                    if (networkStatus.DoWeHaveInternet())
                    {
                        retriable = true;
                        resetRetryCount = networkStatus.HasHandoffOccuredRecently();
                        action = () => { toaster.ShowToastLong(StringKey.remote_conn_closed); };
                    }
                    else
                    {
                        action = () => { toaster.ShowToastLong(StringKey.network_down); };
                    }
                    break;
                case DownloadFailureKind.DiskFull:
                    action = () => { toaster.ShowToastLong(StringKey.error_no_space); };
                    break;
                default:
                {
                    retriable = true;
                    Exception? innermost = DownloadFailureClassifier.GetCause(task.Exception);
                    while (innermost?.InnerException != null)
                    {
                        innermost = innermost.InnerException;
                    }
                    logger.Firebase("dlcontaction Unhandled task exception: " + SimpleHelpers.DescribeException(task.Exception) + " " + innermost?.StackTrace);
                    break;
                }
            }

            if (forceRetry || ((resetRetryCount || e.dlInfo.RetryCount == 0) && PreferencesState.AutoRetryDownload && retriable))
            {
                logger.Debug("Retrying the Download" + e.dlInfo.fullFilename);
                try
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    var retryDlInfo = new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, null, cancellationTokenSource, e.dlInfo.QueueLength, resetRetryCount ? 0 : 1, task.Exception, e.dlInfo.Depth) { TransferItemReference = transferItem };
                    if (!TryClaim(retryDlInfo))
                    {
                        // the user already re-requested it
                        logger.Debug($"auto retry of {e.dlInfo.fullFilename} skipped, it is already being requested");
                        return;
                    }
                    transferItem.ClearStateForRetry();
                    TransferState.SetupCancellationToken(transferItem, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                    StartDownloadsFireAndForget(new[] { retryDlInfo });
                    return; //i.e. dont toast anything just retry.
                }
                catch (Exception ex)
                {
                    logger.Firebase("retry creation failed: " + ex.Message + ex.StackTrace);
                }
            }

            if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
            {
                logger.Firebase("auto retry failed: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString() + "new exception: " + task.Exception?.InnerException?.Message?.ToString());
            }

            if (action == null)
            {
                action = () => { toaster.ShowToastLong(StringKey.error_unspecified); };
            }
            mainThreadRunner.RunOnUiThread(action);
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

        /// <summary>
        /// When a user transitions from offline, retries any downloads that previously failed
        /// because they were offline. Tracked via <see cref="TransferState.UsersWhereDownloadFailedDueToOffline"/>.
        /// </summary>
        public void RetryDownloadsIfUserBackOnline(string username, UserPresence status)
        {
            if (status == UserPresence.Offline)
            {
                return;
            }
            if (!PreferencesState.AutoRetryBackOnline)
            {
                return;
            }
            if (!TransferState.UsersWhereDownloadFailedDueToOffline.ContainsKey(username))
            {
                return;
            }
            logger.Debug("the user came back who we previously dl from " + username);
            List<TransferItem> items = TransferItems.TransferItemManagerDL.GetTransferItemsFromUser(username, true, true);
            if (items.Count == 0)
            {
                lock (TransferState.UsersWhereDownloadFailedDueToOffline)
                {
                    TransferState.UsersWhereDownloadFailedDueToOffline.Remove(username);
                }
                return;
            }
            try
            {
                DownloadRetryAll(items);
            }
            catch (Exception e)
            {
                logger.Debug("RetryDownloadsIfUserBackOnline" + e.Message);
            }
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
        /// Initiates a retry for a single transfer item. If a request already owns it (pending or in the library),
        /// sets CancelAndRetryFlag and cancels it (the continuation will re-download).
        /// Returns true if a fresh download was initiated, false if cancel-and-retry.
        /// </summary>
        public bool RetryDownloadItem(TransferItem item)
        {
            var dlInfo = PrepareRetry(item, restartActive: true);
            if (dlInfo == null)
            {
                return false;
            }
            StartDownloadsFireAndForget(new[] { dlInfo });
            return true;
        }

        // claims the item for a new request. null if a request already owns it - restartActive cancels that one
        //   and its continuation re-requests it (CancelAndRetryFlag), bulk paths leave it alone.
        private DownloadInfo? PrepareRetry(TransferItem item, bool restartActive)
        {
            if (activeRequests.TryGetValue(item, out var owner))
            {
                if (!restartActive)
                {
                    return null;
                }
                item.CancelAndRetryFlag = true;
                if (!owner.CancellationTokenSource.IsCancellationRequested)
                {
                    owner.CancellationTokenSource.Cancel();
                }
                if (activeRequests.TryGetValue(item, out var current) && current == owner)
                {
                    // its still the owner, it will see the flag and be good, we can return
                    return null;
                }
                // it finished before it could see the flag, create a new download
                item.CancelAndRetryFlag = false;
            }

            var cts = new CancellationTokenSource();
            var dlInfo = new DownloadInfo(item.Username, item.FullFilename, item.Size, null, cts, item.QueueLength, item.Failed ? 1 : 0, item.GetDirectoryLevel()) { TransferItemReference = item };
            if (!TryClaim(dlInfo))
            {
                return null;
            }
            TransferState.SetupCancellationToken(item, cts, out _);
            item.ClearStateForRetry();
            // same initial state as AddTransfer
            item.State = TransferStates.Queued | TransferStates.Locally;
            return dlInfo;
        }

        public void DownloadRetryAll(IEnumerable<TransferItem> transferItemConditionList)
        {
            var TransferItemManagerDL = TransferItems.TransferItemManagerDL;
            var ViewState = TransfersViewState.Instance;
            var dlInfos = new List<DownloadInfo>();
            foreach (TransferItem item in transferItemConditionList)
            {
                var dlInfo = PrepareRetry(item, restartActive: false);
                if (dlInfo != null)
                {
                    dlInfos.Add(dlInfo);
                }
            }
            StartDownloadsFireAndForget(dlInfos);

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
                            logger.Debug($"retry: refreshing transfer row {i}");
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
