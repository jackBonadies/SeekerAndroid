using Android.App;
using Android.Content;
using AndroidX.Core.App;
using AndroidX.DocumentFile.Provider;
using Seeker.Helpers;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Common;
namespace Seeker.Services
{
    // Upload enqueue + notification
    public static class UploadService
    {
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
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="DownloadEnqueueException">Thrown when the download is rejected.  The Exception message will be passed to the remote user.</exception>
        /// <exception cref="Exception">Thrown on any other Exception other than a rejection.  A generic message will be passed to the remote user for security reasons.</exception>
        public static Task EnqueueDownloadAction(string username, IPEndPoint endpoint, string filename)
        {
            if (SeekerApplication.IsUserInIgnoreList(username))
            {
                return Task.CompletedTask;
            }

            //the filename is basically "the key"
            _ = endpoint;
            string errorMsg = null;
            Tuple<long, string, Tuple<int, int, int, int>, bool, bool> ourFileInfo = SeekerState.SharedFileCache.GetFullInfoFromSearchableName(filename, out errorMsg);
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

            if (!ourFile.Exists())
            {
                throw new DownloadEnqueueException($"File not found.");
            }

            // create a new cancellation token source so that we can cancel the upload from the UI.
            var cts = new CancellationTokenSource();

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
                    TransferState.SetupCancellationToken(transferItem, cts, out oldCts);

                    var uploadUri = ourFile.Uri;
                    await SeekerState.SoulseekClient.UploadAsync(username, filename, transferItem.Size,
                        inputStreamFactory: (_) => Task.FromResult<System.IO.Stream>(SeekerState.MainActivityRef.ContentResolver.OpenInputStream(uploadUri)),
                        options: new TransferOptions(governor: SpeedLimitHelper.OurUploadGovernor), cancellationToken: cts.Token);

                }
                catch (DuplicateTransferException dup) //not tested
                {
                    Logger.Debug("UPLOAD DUPL - " + dup.Message);
                    TransferState.SetupCancellationToken(transferItem, oldCts, out _);
                }
                catch (DuplicateTokenException dup)
                {
                    Logger.Debug("UPLOAD DUPL - " + dup.Message);
                    TransferState.SetupCancellationToken(transferItem, oldCts, out _);
                }
            }).ContinueWith(t =>
            {
            }, TaskContinuationOptions.NotOnRanToCompletion); // fire and forget

            // return a completed task so that the invoking code can respond to the remote client.
            return Task.CompletedTask;
        }
    }
}
