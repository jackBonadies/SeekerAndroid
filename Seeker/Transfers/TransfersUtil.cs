using Android.Widget;
using Seeker.Services;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Seeker.Helpers;

using Common;
namespace Seeker.Transfers
{
    public static class TransfersUtil
    {    
        public static Task CreateDownloadAllTask(FullFileInfo[] files, bool queuePaused, string username)
        {
            if (username == PreferencesState.Username)
            {
                SeekerApplication.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.cannot_download_from_self), ToastLength.Long);
                return new Task(() => { }); //since we call start on the task, if we call Task.Completed or Task.Delay(0) it will crash...
            }

            Task task = new Task(() =>
            {
                EnqueueFiles(files, queuePaused, username);
            });

            return task;
        }

        public static async Task EnqueueFiles(FullFileInfo[] files, bool queuePaused, string username)
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
                SeekerApplication.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.error_duplicate), ToastLength.Short);
            }
            else
            {
                if (queuePaused)
                {
                    SeekerApplication.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.QueuedForDownload), ToastLength.Short);
                }
                else
                {
                    SeekerApplication.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.download_is_starting), ToastLength.Short);
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
        private static async Task DownloadFiles(List<DownloadInfo> dlInfos, FullFileInfo[] files, string username)
        {
            for (int i = 0; i < dlInfos.Count; i++)
            {
                var dlInfo = dlInfos[i];
                var file = files[i];
                var dlTask = DownloadFileAsync(username, file.FullFileName, file.Size, dlInfo.CancellationTokenSource, out Task waitForNext, file.Depth, file.wasFilenameLatin1Decoded, file.wasFolderLatin1Decoded);
                var e = new DownloadAddedEventArgs(dlInfo);
                Action<Task> continuationActionSaveFile = DownloadService.DownloadContinuationActionUI(e);
                dlTask.ContinueWith(continuationActionSaveFile);
                // wait for current download to update to queued / initialized or dltask to throw exception before kicking off next 
                await waitForNext;
            }
        }


        /// <summary>
        /// Adds a transfer to the database. Does not 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="fname"></param>
        /// <param name="size"></param>
        /// <param name="queueLength"></param>
        /// <param name="depth"></param>
        /// <param name="queuePaused"></param>
        /// <param name="wasLatin1Decoded"></param>
        /// <param name="wasFolderLatin1Decoded"></param>
        /// <param name="isSingle"></param>
        /// <param name="errorExists"></param>
        /// <returns></returns>
        public static DownloadInfo AddTransfer(string username, string fname, long size, int queueLength, int depth, bool queuePaused, bool wasLatin1Decoded, bool wasFolderLatin1Decoded, bool isSingle, out bool errorExists)
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
                        Logger.Firebase("concurrency issue: " + errr); //I think this is fixed by changing to concurrent dict but just in case...
                    }
                }
                transferItem = TransfersFragment.TransferItemManagerDL.AddIfNotExistAndReturnTransfer(transferItem, out exists);
                Logger.Debug($"Adding Transfer To Database: {transferItem.Filename}");
                downloadInfo.TransferItemReference = transferItem;

                if (queuePaused)
                {
                    transferItem.State = TransferStates.Cancelled;
                    MainActivity.InvokeDownloadAddedUINotify(new DownloadAddedEventArgs(null)); //otherwise the ui will not refresh.
                }
                else
                {
                    var e = new DownloadAddedEventArgs(downloadInfo);
                    MainActivity.InvokeDownloadAddedUINotify(e);
                }
            }
            catch (Exception e)
            {
                if (!exists)
                {
                    TransfersFragment.TransferItemManagerDL.Remove(transferItem); //if it did not previously exist then remove it..
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
        /// <param name="username"></param>
        /// <param name="fullfilename"></param>
        /// <param name="size"></param>
        /// <param name="cts"></param>
        /// <param name="incompleteUri"></param>
        /// <returns></returns>
        public static Task DownloadFileAsync(string username, string fullfilename, long? size, CancellationTokenSource cts, out Task waitForNext, int depth = 1, bool isFileDecodedLegacy = false, bool isFolderDecodedLegacy = false) //an indicator for how much of the full filename to use...
        {
            var waitUntilEnqueue = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Logger.Debug($"DownloadFileAsync: {fullfilename}");
            Task dlTask = null;
            Action<(TransferStates PreviousState, Transfer Transfer)> updateForEnqueue = new Action<(TransferStates PreviousState, Transfer Transfer)>( (args) =>
            {
                if (args.Transfer.State.HasFlag(TransferStates.Queued) || args.Transfer.State == TransferStates.Initializing)
                {
                    Logger.Debug($"Queued / Init: {fullfilename} We can proceed to download next file.");
                    waitUntilEnqueue.TrySetResult(true);
                }
            });
            if (PreferencesState.MemoryBackedDownload)
            {
                dlTask =
                    SeekerState.SoulseekClient.DownloadAsync(
                        username: username,
                        filename: fullfilename,
                        size: size,
                        options: new TransferOptions(governor: SpeedLimitHelper.OurDownloadGovernor, stateChanged: updateForEnqueue),
                        cancellationToken: cts.Token,
                        isLegacy: isFileDecodedLegacy,
                        isFolderDecodedLegacy: isFolderDecodedLegacy);
            }
            else
            {
                long partialLength = 0;

                dlTask = SeekerState.SoulseekClient.DownloadAsync(
                        username: username,
                        filename: fullfilename,
                        null,
                        size: size,
                        startOffset: partialLength, //this will get populated
                        options: new TransferOptions(disposeOutputStreamOnCompletion: true, governor: SpeedLimitHelper.OurDownloadGovernor, stateChanged: updateForEnqueue),
                        cancellationToken: cts.Token,
                        streamTask: GetStreamTask(username, fullfilename, depth),
                        isFilenameDecodedLegacy: isFileDecodedLegacy,
                        isFolderDecodedLegacy: isFolderDecodedLegacy);
            }
            waitForNext = Task.WhenAny(waitUntilEnqueue.Task, dlTask);
            return dlTask;
        }

        public static Task<Tuple<System.IO.Stream, long, string, string>> GetStreamTask(string username, string fullfilename, int depth = 1) //there has to be something extra here for args, bc we need to denote just how much of the fullFilename to use....
        {
            Task<Tuple<System.IO.Stream, long, string, string>> task = new Task<Tuple<System.IO.Stream, long, string, string>>(
                () =>
                {
                    long partialLength = 0;
                    Android.Net.Uri incompleteUri = null;
                    Android.Net.Uri incompleteUriDirectory = null;
                    System.IO.Stream streamToWriteTo = DownloadService.GetIncompleteStream(username, fullfilename, depth, out incompleteUri, out incompleteUriDirectory, out partialLength); //something here to denote...
                    return new Tuple<System.IO.Stream, long, string, string>(streamToWriteTo, partialLength, incompleteUri.ToString(), incompleteUriDirectory.ToString());
                });
            return task;
        }

    }
}
