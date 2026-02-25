using Android.Content;
using Android.Views;
using Android.Widget;
using Common;
using Common.Browse;
using Google.Android.Material.Snackbar;
using Seeker.Helpers;
using Seeker.Search;
using Seeker.Services;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Seeker.Browse
{
    public static class BrowseService
    {
        public static void GetFolderContentsAPI(string username, string dirname, bool isLegacy, Action<Task<IReadOnlyCollection<Directory>>> continueWithAction)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_in_to_get_dir_contents), ToastLength.Short);
                return;
            }

            Action<Task> actualActionToPerform = new Action<Task>((Task connectionTask) =>
            {

                if (connectionTask.IsFaulted)
                {
                    if (!(connectionTask.Exception.InnerException is FaultPropagationException)) //i.e. only show it once.
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                    }
                    throw new FaultPropagationException();
                }
                else
                {
                    //the original logic...
                    Task<IReadOnlyCollection<Directory>> t = SeekerState.SoulseekClient.GetDirectoryContentsAsync(username, dirname, null, null, isLegacy);
                    t.ContinueWith(continueWithAction);
                }

            });


            if (SoulseekService.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task conTask;
                if (!SoulseekService.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out conTask))
                {
                    return;
                }
                SeekerApplication.OurCurrentLoginTask = conTask.ContinueWith(actualActionToPerform);
            }
            else
            {
                if (SoulseekService.IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(actualActionToPerform, null, null))
                {
                    Logger.Debug("on finish log in we will do it");
                    return;
                }
                else
                {
                    Task<IReadOnlyCollection<Directory>> t = SeekerState.SoulseekClient.GetDirectoryContentsAsync(username, dirname, isLegacy: isLegacy);
                    t.ContinueWith(continueWithAction);
                }
            }
        }

        public static void RequestFilesApi(string username, View viewForSnackBar, Action<View> goSnackBarAction, string atLocation = null)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short);
                return;
            }
            if (SoulseekService.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!SoulseekService.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { RequestFilesLogic(username, viewForSnackBar, goSnackBarAction, atLocation); }));
                }));
            }
            else
            {
                RequestFilesLogic(username, viewForSnackBar, goSnackBarAction, atLocation);
            }
        }

        private static void RequestFilesLogic(string username, View viewForSnackBar, Action<View> goSnackBarAction, string atLocation)
        {
            try
            {
                Snackbar.Make(SeekerApplication.GetViewForSnackbar(), SeekerState.ActiveActivityRef.GetString(Resource.String.browse_user_contacting), Snackbar.LengthShort).Show();
            }
            catch (Exception e)
            {
                Logger.Firebase("RequestFilesLogic: " + e.Message + e.StackTrace);
            }
            Task<BrowseResponse> browseResponseTask = null;
            try
            {
                browseResponseTask = SeekerState.SoulseekClient.BrowseAsync(username);
            }
            catch (InvalidOperationException)
            {   //this can still happen on ReqFiles_Click.. maybe for the first check we were logged in but for the second we somehow were not..
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short);
                return;
            }
            Action<Task<BrowseResponse>> continueWithAction = new Action<Task<BrowseResponse>>((br) =>
            {
                Logger.Debug($"RequestFilesLogic {username} completed");

                if (br.IsFaulted && br.Exception?.InnerException is TimeoutException)
                {
                    //timeout
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.browse_user_timeout), ToastLength.Short);
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is ConnectionException && br.Exception?.InnerException?.InnerException is TimeoutException)
                {
                    //timeout - this time when the connection was established, but the user has not written to us in over 15 (timeout) seconds. I tested and generally this is fixed by simply retrying.
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.browse_user_timeout), ToastLength.Short);
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is ConnectionException && br.Exception?.InnerException?.InnerException != null && br.Exception.InnerException.InnerException.ToString().ToLower().Contains("network subsystem is down"))
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.network_down), ToastLength.Short);
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException != null && br.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.browse_user_nodirectconnection), ToastLength.Short);
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is UserOfflineException)
                {
                    SeekerApplication.Toaster.ShowToast(String.Format(SeekerApplication.GetString(Resource.String.CannotBrowseUsernameOffline), username), ToastLength.Short);
                    return;
                }
                else if (br.IsFaulted)
                {
                    //shouldnt get here
                    SeekerApplication.Toaster.ShowToast(String.Format(SeekerApplication.GetString(Resource.String.FailedToBrowseUsernameUnspecifiedError), username), ToastLength.Short);
                    Logger.Firebase("browse response faulted: " + username + br.Exception?.Message);
                    return;
                }

                string errorString = string.Empty;
                var tree = CreateTree(br.Result, false, null, null, username, out errorString);
                if (tree != null)
                {
                    SeekerState.OnBrowseResponseReceived(br.Result, tree, username, atLocation);
                }

                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    if (tree == null)
                    {
                        //error case
                        if (errorString != null && errorString != string.Empty)
                        {
                            SeekerApplication.Toaster.ShowToast(errorString, ToastLength.Long);
                        }
                        else
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.browse_user_wefailedtoparse), ToastLength.Long);
                        }
                        return;
                    }
                    if (SeekerState.MainActivityRef != null && ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).CurrentItem == 3) //AND it is our current activity...
                    {
                        if (SeekerState.MainActivityRef.Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Started))
                        {
                            return; //they are already there... they see it populating, no need to show them notification...
                        }
                    }

                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        SeekerState.ActiveActivityRef.StartActivity(intent);
                    });

                    try
                    {
                        Snackbar sb = Snackbar.Make(SeekerApplication.GetViewForSnackbar(), SeekerState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SeekerState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                        (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));
                        sb.Show();
                    }
                    catch
                    {
                        try
                        {
                            Snackbar sb = Snackbar.Make(SeekerState.MainActivityRef.CurrentFocus, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SeekerState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                            (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));
                            sb.Show();
                        }
                        catch
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.browse_response_received), ToastLength.Short);
                        }
                    }


                });
            });
            browseResponseTask.ContinueWith(continueWithAction);
        }

        // TODO2026 create ILogger and migrate
        public static TreeNode<Directory> CreateTree(BrowseResponse b, bool filter, List<string> wordsToAvoid, List<string> wordsToInclude, string username, out string errorMsgToToast)
        {
            bool hideLocked = PreferencesState.HideLockedResultsInBrowse;
            if (b.DirectoryCount == 0 && b.LockedDirectoryCount != 0 && hideLocked)
            {
                errorMsgToToast = SeekerState.ActiveActivityRef.GetString(Resource.String.browse_onlylocked);
                return null;
            }
            else if (b.DirectoryCount == 0 && b.LockedDirectoryCount == 0)
            {
                errorMsgToToast = SeekerState.ActiveActivityRef.GetString(Resource.String.browse_none);
                return null;
            }

            //if the user is sharing only 1 empty directory, then show a message.
            //previously we let it through, but if they are sharing just 1 empty dir, that becomes the root dir
            //and it looks strange. if 2+ empty dirs the same problem does not occur.
            if (hideLocked && b.DirectoryCount == 1 && b.Directories.First().FileCount == 0)
            {
                errorMsgToToast = String.Format(SeekerApplication.GetString(Resource.String.BrowseOnlyEmptyDir), username);
                return null;
            }
            else if (!hideLocked && (b.DirectoryCount + b.LockedDirectoryCount == 1)) //if just 1 dir total
            {
                if (b.DirectoryCount == 1 && b.Directories.First().FileCount == 0)
                {
                    errorMsgToToast = String.Format(SeekerApplication.GetString(Resource.String.BrowseOnlyEmptyDir), username);
                    return null;
                }
                else if (b.LockedDirectoryCount == 1 && b.LockedDirectories.First().FileCount == 0)
                {
                    errorMsgToToast = String.Format(SeekerApplication.GetString(Resource.String.BrowseOnlyEmptyDir), username);
                    return null;
                }
            }

            TreeNode<Directory> rootNode = null;
            try
            {
                errorMsgToToast = String.Empty;
                rootNode = Common.Algorithms.CreateTreeCore(b, filter, wordsToAvoid, wordsToInclude, username, hideLocked);
            }
            catch (Exception e)
            {
                Logger.Firebase("CreateTree " + username + "  " + hideLocked + " " + e.Message + e.StackTrace);
                throw e;
            }

            errorMsgToToast = "";
            return rootNode;
        }

        public static void DownloadListOfFiles(List<FullFileInfo> slskFiles, bool queuePaused, string _username)
        {
            if (SoulseekService.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!SoulseekService.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }

                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    DownloadService.CreateDownloadAllTask(slskFiles.ToArray(), queuePaused, _username).Start();
                }));
            }
            else
            {
                DownloadService.CreateDownloadAllTask(slskFiles.ToArray(), queuePaused, _username).Start();
            }
        }

        public static void DownloadFilesLogic(Task<IReadOnlyCollection<Directory>> dirTask, string _uname, string thisFileOnly = null)
        {
            if (dirTask.IsFaulted)
            {
                //failed to follow link..
                if (dirTask.Exception?.InnerException?.Message != null)
                {
                    string msgToToast = string.Empty;
                    if (dirTask.Exception.InnerException.Message.ToLower().Contains("timed out"))
                    {
                        msgToToast = "Failed to Add Download - Request timed out";
                    }
                    else if (dirTask.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                    {
                        msgToToast = $"Failed to Add Download - Cannot establish connection to user {_uname}";
                    }
                    else if (dirTask.Exception.InnerException is Soulseek.UserOfflineException)
                    {
                        msgToToast = $"Failed to Add Download - User {_uname} is offline";
                    }
                    else
                    {
                        msgToToast = "Failed to follow link";
                    }
                    Logger.Debug(dirTask.Exception.InnerException.Message);
                    SeekerApplication.Toaster.ShowToast(msgToToast, ToastLength.Short);
                }
                Logger.Debug("DirectoryReceivedContAction faulted");
            }
            else
            {
                List<FullFileInfo> fullFileInfos = new List<FullFileInfo>();

                //the filenames for these files are NOT the fullname.
                //the fullname is dirTask.Result.Name "\\" f.Filename
                var directory = dirTask.Result.First();
                foreach (var f in directory.Files)
                {
                    string fullFilename = directory.Name + "\\" + f.Filename;
                    if (thisFileOnly == null)
                    {
                        fullFileInfos.Add(new FullFileInfo() { Depth = 1, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                    }
                    else
                    {
                        if (fullFilename == thisFileOnly)
                        {
                            //add
                            fullFileInfos.Add(new FullFileInfo() { Depth = 1, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                            break;
                        }
                    }
                }


                if (fullFileInfos.Count == 0)
                {
                    if (thisFileOnly == null)
                    {
                        SeekerApplication.Toaster.ShowToast("Nothing to download. Browse at this location to ensure that the file exists and is not locked.", ToastLength.Short);
                    }
                    else
                    {
                        SeekerApplication.Toaster.ShowToast("Nothing to download. Browse at this location to ensure that the directory contains files and they are not locked.", ToastLength.Short);
                    }
                    return;
                }

                DownloadListOfFiles(fullFileInfos, false, _uname);


            }
        }
    }
}
