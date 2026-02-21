using Android.Widget;
using AndroidX.DocumentFile.Provider;
using Seeker.Helpers;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Common;
namespace Seeker.Services
{
    /// <summary>
    /// Handles Soulseek sharing response resolvers (search, browse, directory contents)
    /// and controls enabling/disabling sharing.
    /// </summary>
    public static class SharingService
    {
        private static bool _isActive;

        private static readonly Func<string, int, SearchQuery, Task<SearchResponse>> NoOpSearchResolver =
            (u, t, q) => Task.FromResult<SearchResponse>(null);

        private static readonly Func<string, IPEndPoint, Task<BrowseResponse>> NoOpBrowseResolver =
            (u, i) => Task.FromResult(new BrowseResponse(Enumerable.Empty<Soulseek.Directory>()));

        private static readonly Func<string, IPEndPoint, int, string, Task<IEnumerable<Soulseek.Directory>>> NoOpDirectoryResolver =
            (u, i, t, d) => Task.FromResult(Enumerable.Empty<Soulseek.Directory>());

        private static readonly Func<string, IPEndPoint, string, Task> NoOpEnqueueDownload =
            (u, i, f) => Task.CompletedTask;

        public static void TurnOnSharing()
        {
            SeekerState.SoulseekClient.ReconfigureOptionsAsync(new SoulseekClientOptionsPatch(
                searchResponseResolver: SearchResponseResolver,
                browseResponseResolver: BrowseResponseResolver,
                directoryContentsResolver: DirectoryContentsResponseResolver,
                enqueueDownload: DownloadService.EnqueueDownloadAction));
            _isActive = true;
        }

        public static void TurnOffSharing()
        {
            SeekerState.SoulseekClient.ReconfigureOptionsAsync(new SoulseekClientOptionsPatch(
                searchResponseResolver: NoOpSearchResolver,
                browseResponseResolver: NoOpBrowseResolver,
                directoryContentsResolver: NoOpDirectoryResolver,
                enqueueDownload: NoOpEnqueueDownload));
            _isActive = false;
        }

        /// <summary>
        /// Check if sharing is currently active (i.e. handlers are set).
        /// </summary>
        public static bool IsSharingActive() => _isActive;

        private static Task<BrowseResponse> BrowseResponseResolver(string username, IPEndPoint endpoint)
        {
            if (SeekerApplication.IsUserInIgnoreList(username))
            {
                return Task.FromResult(new BrowseResponse(Enumerable.Empty<Directory>()));
            }
            return Task.FromResult(SeekerState.SharedFileCache.GetBrowseResponseForUser(username));
        }

        /// <summary>
        ///     Creates and returns a <see cref="SearchResponse"/> in response to the given <paramref name="query"/>.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The search token.</param>
        /// <param name="query">The search query.</param>
        /// <returns>A Task resolving a SearchResponse, or null.</returns>
        private static Task<SearchResponse> SearchResponseResolver(string username, int token, SearchQuery query)
        {
            var defaultResponse = Task.FromResult<SearchResponse>(null);

            // some bots continually query for very common strings.  blacklist known names here.
            var blacklist = new[] { "Lola45", "Lolo51", "rajah" };
            if (blacklist.Contains(username))
            {
                return defaultResponse;
            }
            if (SeekerApplication.IsUserInIgnoreList(username))
            {
                return defaultResponse;
            }
            // some bots and perhaps users search for very short terms.  only respond to queries >= 3 characters.  sorry, U2 fans.
            if (query.Query.Length < 5)
            {
                return defaultResponse;
            }

            if (PreferencesState.Username == null || PreferencesState.Username == string.Empty || SeekerState.SharedFileCache == null)
            {
                return defaultResponse;
            }

            var results = SeekerState.SharedFileCache.Search(query, username, out IEnumerable<Soulseek.File> lockedResults);

            if (results.Any() || lockedResults.Any())
            {
                //Console.WriteLine($"[SENDING SEARCH RESULTS]: {results.Count()} records to {username} for query {query.SearchText}");
                int ourUploadSpeed = 1024 * 256;
                if (PreferencesState.UploadSpeed > 0)
                {
                    ourUploadSpeed = PreferencesState.UploadSpeed;
                }
                return Task.FromResult(new SearchResponse(
                    PreferencesState.Username,
                    token,
                    hasFreeUploadSlot: true,
                    uploadSpeed: ourUploadSpeed,
                    queueLength: 0,
                    fileList: results,
                    lockedFileList: lockedResults));
            }

            // if no results, either return null or an instance of SearchResponse with a fileList of length 0
            // in either case, no response will be sent to the requestor.
            return Task.FromResult<SearchResponse>(null);
        }

        /// <summary>
        ///     Creates and returns a <see cref="Soulseek.Directory"/> in response to a remote request.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="endpoint">The IP endpoint of the requesting user.</param>
        /// <param name="token">The unique token for the request, supplied by the requesting user.</param>
        /// <param name="directory">The requested directory.</param>
        /// <returns>A Task resolving an instance of Soulseek.Directory containing the contents of the requested directory.</returns>
        private static Task<IEnumerable<Soulseek.Directory>> DirectoryContentsResponseResolver(string username, IPEndPoint endpoint, int token, string directory)
        {
            //the directory is the presentable name.
            //the old EndsWith(dir) fails if the directory is not unique i.e. document structure of Soulseek Complete > some dirs and files, Soulseek Complete > more dirs and files..
            Tuple<string, string> fullDirUri = SeekerState.SharedFileCache.FriendlyDirNameToUriMapping.Where((Tuple<string, string> t) => { return t.Item1 == directory; }).FirstOrDefault(); //TODO DICTIONARY>>>>>

            if (fullDirUri == null)
            {
                //as fallback safety.  I dont think this will ever happen.....
                fullDirUri = SeekerState.SharedFileCache.FriendlyDirNameToUriMapping.Where((Tuple<string, string> t) => { return t.Item1.EndsWith(directory); }).FirstOrDefault();
            }
            if (fullDirUri == null)
            {
                //could not find...
            }
            DocumentFile fullDir = null;
            if (SeekerState.PreOpenDocumentTree() || !UploadDirectoryManager.IsFromTree(fullDirUri.Item2)) //todo
            {
                fullDir = DocumentFile.FromFile(new Java.IO.File(Android.Net.Uri.Parse(fullDirUri.Item2).Path));
            }
            else
            {
                fullDir = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, Android.Net.Uri.Parse(fullDirUri.Item2));
            }
            //Android.Net.Uri.Parse(SeekerState.UploadDataDirectoryUri).Path
            var slskDir = SharedFileService.SlskDirFromDocumentFile(fullDir, true, FileFilterHelper.GetVolumeName(fullDir.Uri.LastPathSegment, false, out _));
            slskDir = new Directory(directory, slskDir.Files);
            return Task.FromResult(Enumerable.Repeat(slskDir, 1));
        }

        /// <summary>
        /// Sets up shared file cache on a background thread.
        /// </summary>
        public static void SetUpSharing(Action uiUpdateAction = null)
        {
            Action setUpSharedFileCache = new Action(() =>
            {
                string errorMessage = string.Empty;
                bool success = false;
                Logger.Debug("We meet sharing conditions, lets set up the sharedFileCache for 1st time.");
                try
                {
                    success = SharedFileService.InitializeDatabase(null, true, out errorMessage); //we check the cache which has ALL of the parsed results in it. much different from rescanning.
                }
                catch (Exception e)
                {
                    Logger.Debug("Error setting up sharedFileCache for 1st time." + e.Message + e.StackTrace);
                    SetUnsetSharingBasedOnConditions(false, true);
                    if (!(e is DirectoryAccessFailure))
                    {
                        Logger.Firebase("MainActivity error parsing: " + e.Message + "  " + e.StackTrace);
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.error_sharing), ToastLength.Long).Show();
                    }));
                }

                if (success && SeekerState.SharedFileCache != null && SeekerState.SharedFileCache.SuccessfullyInitialized)
                {
                    Logger.Debug("database full initialized.");
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.success_sharing), ToastLength.Short).Show();
                    }));
                    try
                    {
                        //setup soulseek client with handlers if all conditions met
                        SetUnsetSharingBasedOnConditions(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Firebase("MainActivity error setting handlers: " + e.Message + "  " + e.StackTrace);
                    }
                }
                else if (!success)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                    {
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.error_sharing), ToastLength.Short).Show();
                        }
                        else
                        {
                            Toast.MakeText(SeekerState.ActiveActivityRef, errorMessage, ToastLength.Short).Show();
                        }
                    }));
                }

                if (uiUpdateAction != null)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateAction);
                }
                SeekerState.AttemptedToSetUpSharing = true;
            });
            System.Threading.ThreadPool.QueueUserWorkItem((object o) => { setUpSharedFileCache(); });
        }

        /// <summary>
        /// Do this on any changes (like in Settings) but also on Login.
        /// </summary>
        public static void SetUnsetSharingBasedOnConditions(bool informServerOfChangeIfThereIsAChange, bool force = false)
        {
            bool wasShared = IsSharingActive();
            if (SharedFileService.MeetsCurrentSharingConditions())
            {
                TurnOnSharing();
                if (!wasShared || force)
                {
                    Logger.Debug("sharing state changed to ON");
                    SharedFileService.InformServerOfSharedFiles();
                }
            }
            else
            {
                TurnOffSharing();
                if (wasShared)
                {
                    Logger.Debug("sharing state changed to OFF");
                    SharedFileService.InformServerOfSharedFiles();
                }
            }
        }

        public static Tuple<SharingIcons, string> GetSharingMessageAndIcon(out bool isParsing)
        {
            isParsing = false;
            if (SharedFileService.MeetsSharingConditions() && SharedFileService.IsSharingSetUpSuccessfully())
            {
                //try to parse this into a path: SeekerState.ShareDataDirectoryUri
                if (SharedFileService.MeetsCurrentSharingConditions())
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.On, SeekerState.ActiveActivityRef.GetString(Resource.String.success_sharing));
                }
                else
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.OffDueToNetwork, "Sharing disabled on metered connection");
                }
            }
            else if (SharedFileService.MeetsSharingConditions() && !SharedFileService.IsSharingSetUpSuccessfully())
            {
                if (SeekerState.SharedFileCache == null)
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.Off, "Not yet initialized.");
                }
                else
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.Error, SeekerState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_share_not_set));
                }
            }
            else if (!PreferencesState.SharingOn)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Off, SeekerState.ActiveActivityRef.GetString(Resource.String.sharing_off));
            }
            else if (SeekerState.IsParsing)
            {
                isParsing = true;
                return new Tuple<SharingIcons, string>(SharingIcons.CurrentlyParsing, SeekerState.ActiveActivityRef.GetString(Resource.String.sharing_currently_parsing));
            }
            else if (SeekerState.FailedShareParse)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SeekerState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_failure_parsing));
            }
            else if (UploadDirectoryManager.UploadDirectories.Count == 0)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SeekerState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_share_not_set));
            }
            else if (UploadDirectoryManager.AreAllFailed())
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SeekerState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_error)); //TODO get error
            }
            else
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SeekerState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_error));
            }
        }
    }
}
