/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */

using Seeker.Extensions.SearchResponseExtensions;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Common;
using Google.Android.Material.Snackbar;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log = Android.Util.Log;
using Seeker.Helpers;
using Seeker.Transfers;

namespace Seeker
{
    class DownloadDialog : AndroidX.Fragment.App.DialogFragment, PopupMenu.IOnMenuItemClickListener
    {
        private int searchPosition = -1;
        private SearchResponse searchResponse = null;
        DownloadCustomAdapter customAdapter = null;
        static SearchResponse SearchResponseTemp = null; //These are for when the DownloadDialog gets recreated by the system.
        static int SearchPositionTemp = -1; //The system NEEDS a default fragment constructor to call. So we re-
        //private bool diagFirstTime = true;                                                 //use these arguments.
        private Activity activity = null;
        //private List<int> selectedPositions = new List<int>();
        public DownloadDialog(int pos, SearchResponse resp)
        {
            MainActivity.LogDebug("DownloadDialog create");
            searchResponse = resp;
            searchPosition = pos;
            SearchResponseTemp = resp;
            SearchPositionTemp = pos;
        }

        public DownloadDialog()
        {
            MainActivity.LogDebug("DownloadDialog create (default constructor)"); //this gets called on recreate i.e. phone tilt, etc.
            searchResponse = SearchResponseTemp;
            searchPosition = SearchPositionTemp;
        }

        private void UpdateSearchResponseWithFullDirectory(Soulseek.Directory d)
        {
            //normally files are like this "@@ynkmv\\Albums\\albumname (2012)\\02 - songname.mp3"
            //but when we get a dir response the files are just the end file names i.e. "02 - songname.mp3" so they cannot be downloaded like that...
            //can be fixed with d.Name + "\\" + f.Filename
            //they also do not come with any attributes.. , just the filenames (and sizes) you need if you want to download them...
            bool hideLocked = SeekerState.HideLockedResultsInSearch;
            List<File> fullFilenameCollection = new List<File>();
            foreach (File f in d.Files)
            {
                string fName = d.Name + "\\" + f.Filename;
                bool extraAttr = false;
                //if it existed in the old folder then we can get some extra attributes
                foreach (File fullFileInfo in searchResponse.GetFiles(hideLocked))
                {
                    if (fName == fullFileInfo.Filename)
                    {
                        fullFilenameCollection.Add(new File(f.Code, fName, f.Size, f.Extension, fullFileInfo.Attributes, f.IsLatin1Decoded, d.DecodedViaLatin1));
                        extraAttr = true;
                        break;
                    }
                }
                if (!extraAttr)
                {
                    fullFilenameCollection.Add(new File(f.Code, fName, f.Size, f.Extension, f.Attributes, f.IsLatin1Decoded, d.DecodedViaLatin1));
                }
            }
            SearchResponseTemp = searchResponse = new SearchResponse(searchResponse.Username, searchResponse.Token, searchResponse.FreeUploadSlots, searchResponse.UploadSpeed, searchResponse.QueueLength, fullFilenameCollection);
        }

        public override void OnResume()
        {
            base.OnResume();
            MainActivity.LogDebug("OnResume Start");

            Dialog?.SetSizeProportional(.9, .9);

            MainActivity.LogDebug("OnResume End");
        }

        public override void OnAttach(Context context)
        {
            MainActivity.LogDebug("DownloadDialog OnAttach");
            base.OnAttach(context);
            if (context is Activity)
            {
                this.activity = context as Activity;
            }
            else
            {
                throw new Exception("Custom");
            }
        }

        public override void OnDetach()
        {
            MainActivity.LogDebug("DownloadDialog OnDetach");
            SearchFragment.dlDialogShown = false;
            base.OnDetach();
            this.activity = null;
        }

        public override void OnDestroy()
        {
            SearchFragment.dlDialogShown = false;
            base.OnDestroy();
            this.activity = null;
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);
            SearchFragment.dlDialogShown = false;
        }

        public static DownloadDialog CreateNewInstance(int pos, SearchResponse resp)
        {
            DownloadDialog downloadDialog = new DownloadDialog(pos, resp);
            return downloadDialog;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MainActivity.LogDebug("DownloadDialog OnCreateView");
            return inflater.Inflate(Resource.Layout.downloaddialog, container); //container is parent
        }

        private class OnRefreshListenerGetFolder : Java.Lang.Object, AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout.IOnRefreshListener
        {
            private DownloadDialog diagParent;

            public OnRefreshListenerGetFolder(DownloadDialog _diagParent)
            {
                diagParent = _diagParent;
            }

            public void OnRefresh()
            {
                diagParent.GetFolderContents();
            }
        }

        private Button downloadSelectedButton = null;
        private AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout swipeRefreshLayout = null;
        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            log.Debug(MainActivity.logCatTag, "Is View null: " + (view == null).ToString());

            log.Debug(MainActivity.logCatTag, "Is savedInstanceState null: " + (savedInstanceState == null).ToString()); //this is null and it is fine..
            base.OnViewCreated(view, savedInstanceState);
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable_dl_dialog_specific));

            this.SetStyle((int)DialogFragmentStyle.NoTitle, 0);
            Button dl = view.FindViewById<Button>(Resource.Id.buttonDownload);
            log.Debug(MainActivity.logCatTag, "Is dl null: " + (dl == null).ToString());
            dl.Click += DownloadAll_Click;
            Button cancel = view.FindViewById<Button>(Resource.Id.buttonCancel);
            cancel.Click += Cancel_Click;
            downloadSelectedButton = view.FindViewById<Button>(Resource.Id.buttonDownloadSelected);
            downloadSelectedButton.Click += DownloadSelected_Click;
            Button reqFiles = view.FindViewById<Button>(Resource.Id.buttonRequestDirectories);
            reqFiles.Click += ReqFiles_Click;
            //selectedPositions.Clear();
            TextView userHeader = view.FindViewById<TextView>(Resource.Id.userHeader);
            TextView subHeader = view.FindViewById<TextView>(Resource.Id.userHeaderSub);

            swipeRefreshLayout = view.FindViewById<AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout>(Resource.Id.swipeToRefreshLayout);
            swipeRefreshLayout.SetProgressBackgroundColorSchemeColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.swipeToRefreshBackground).ToArgb());
            swipeRefreshLayout.SetColorSchemeColors(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.swipeToRefreshProgress).ToArgb());
            swipeRefreshLayout.SetOnRefreshListener(new OnRefreshListenerGetFolder(this));

            ViewGroup headerLayout = view.FindViewById<ViewGroup>(Resource.Id.header1);

            if (searchResponse == null)
            {
                log.Debug(MainActivity.logCatTag, "Is searchResponse null");
                MainActivity.LogFirebase("DownloadDialog search response is null");
                this.Dismiss(); //this is honestly pretty good behavior...
                return;
            }
            userHeader.Text = SeekerApplication.GetString(Resource.String.user_) + " " + searchResponse.Username;
            subHeader.Text = SeekerApplication.GetString(Resource.String.Total_) + " " + CommonHelpers.GetSubHeaderText(searchResponse);
            headerLayout.Click += UserHeader_Click;
            log.Debug(MainActivity.logCatTag, "Is searchResponse.Files null: " + (searchResponse.Files == null).ToString());

            ListView listView = view.FindViewById<ListView>(Resource.Id.listView1);
            listView.ItemClick += ListView_ItemClick;
            listView.ChoiceMode = ChoiceMode.Multiple;
            UpdateListView();
            SetDownloadSelectedButtonState();
        }

        private void UpdateListView()
        {
            ListView listView = this.View.FindViewById<ListView>(Resource.Id.listView1);
            List<FileLockedUnlockedWrapper> adapterList = new List<FileLockedUnlockedWrapper>();
            adapterList.AddRange(searchResponse.Files.ToList().Select(x => new FileLockedUnlockedWrapper(x, false)));
            if (!SeekerState.HideLockedResultsInSearch)
            {
                adapterList.AddRange(searchResponse.LockedFiles.ToList().Select(x => new FileLockedUnlockedWrapper(x, true)));
            }
            this.customAdapter = new DownloadCustomAdapter(SeekerState.MainActivityRef, adapterList);
            this.customAdapter.Owner = this;
            listView.Adapter = (customAdapter);
        }

        private void UpdateSubHeader()
        {
            TextView subHeader = this.View.FindViewById<TextView>(Resource.Id.userHeaderSub);
            subHeader.Text = SeekerApplication.GetString(Resource.String.Total_) + " " + CommonHelpers.GetSubHeaderText(searchResponse);
        }

        private void UserHeader_Click(object sender, EventArgs e)
        {
            try
            {
                PopupMenu popup = new PopupMenu(SeekerState.MainActivityRef, sender as View, GravityFlags.Right);
                popup.SetOnMenuItemClickListener(this);//  setOnMenuItemClickListener(MainActivity.this);
                popup.Inflate(Resource.Menu.download_diag_options);

                if (customAdapter.SelectedPositions.Count > 0)
                {
                    popup.Menu.FindItem(Resource.Id.download_selected_as_queued).SetVisible(true);
                }
                else
                {
                    popup.Menu.FindItem(Resource.Id.download_selected_as_queued).SetVisible(false);
                }

                popup.Show();
            }
            catch (System.Exception error)
            {
                //in response to a crash android.view.WindowManager.BadTokenException
                //This crash is usually caused by your app trying to display a dialog using a previously-finished Activity as a context.
                //in this case not showing it is probably best... as opposed to a crash...
                MainActivity.LogFirebase(error.Message + " POPUP BAD ERROR");
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
                MainActivity.LogFirebase("RequestFilesLogic: " + e.Message + e.StackTrace);
            }
            Task<BrowseResponse> browseResponseTask = null;
            try
            {
                browseResponseTask = SeekerState.SoulseekClient.BrowseAsync(username);
            }
            catch (InvalidOperationException)
            {   //this can still happen on ReqFiles_Click.. maybe for the first check we were logged in but for the second we somehow were not..
                SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show(); });
                return;
            }
            Action<Task<BrowseResponse>> continueWithAction = new Action<Task<BrowseResponse>>((br) =>
            {
                //var arrayOfDir = br.Result.Directories.ToArray();
                //for(int i=0;i<arrayOfDir.Length;i++)
                //{
                //    Console.WriteLine(arrayOfDir[i].DirectoryName);
                //    Console.WriteLine(arrayOfDir[i].FileCount);
                //    if(i>100)
                //    {
                //        break;
                //    }
                //}
                //Console.WriteLine(arrayOfDir.ToString());
                MainActivity.LogDebug($"RequestFilesLogic {username} completed");

                if (br.IsFaulted && br.Exception?.InnerException is TimeoutException)
                {
                    //timeout
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_user_timeout), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is ConnectionException && br.Exception?.InnerException?.InnerException is TimeoutException)
                {
                    //timeout - this time when the connection was established, but the user has not written to us in over 15 (timeout) seconds. I tested and generally this is fixed by simply retrying.
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_user_timeout), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is ConnectionException && br.Exception?.InnerException?.InnerException != null && br.Exception.InnerException.InnerException.ToString().ToLower().Contains("network subsystem is down"))
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.network_down), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException != null && br.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_user_nodirectconnection), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is UserOfflineException)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, String.Format(SeekerApplication.GetString(Resource.String.CannotBrowseUsernameOffline), username), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted)
                {
                    //shouldnt get here
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, String.Format(SeekerApplication.GetString(Resource.String.FailedToBrowseUsernameUnspecifiedError), username), ToastLength.Short).Show(); });
                    MainActivity.LogFirebase("browse response faulted: " + username + br.Exception?.Message);
                    return;
                }
                //TODO there is a case due to like button mashing or if you keep requesting idk. but its a SoulseekClient InnerException and it says peer disconnected unexpectedly and timeout.

                //List<string> terms = new List<string>();
                //terms.Add("Collective");
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
                            Toast.MakeText(SeekerState.ActiveActivityRef, errorString, ToastLength.Long).Show();
                        }
                        else
                        {
                            Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_user_wefailedtoparse), ToastLength.Long).Show();
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
                        //((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });

                    try
                    {
                        Snackbar sb = Snackbar.Make(SeekerApplication.GetViewForSnackbar(), SeekerState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SeekerState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                        (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                        sb.Show();
                    }
                    catch
                    {
                        try
                        {
                            Snackbar sb = Snackbar.Make(SeekerState.MainActivityRef.CurrentFocus, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SeekerState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                            (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                            sb.Show();
                        }
                        catch
                        {
                            Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_response_received), ToastLength.Short).Show();
                        }
                    }


                });
            });
            browseResponseTask.ContinueWith(continueWithAction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="dirname"></param>
        /// <param name="isLegacy"></param>
        /// <param name="continueWithAction"></param>
        /// <exception cref="FaultPropagationException"></exception>
        /// <remarks>
        /// Older versions of Nicotine do not send us the token we sent them (the token we get is always 1).
        /// This will result in a timeout error.
        /// Regarding fixing the case where older Nicotine and older slsk.net send us a Latin1 encoded string
        /// that is ambigious (i.e. fÃ¶r) if we sent it back properly we get a timeout.  I dont think its worth
        /// retrying since the versions of Nicotine that send us a Latin1 string are the same versions that send
        /// the token = 1.  Also, even if it did work, the the user would only get the folder after a full 30 second timeout.  
        /// </remarks>
        public static void GetFolderContentsAPI(string username, string dirname, bool isLegacy, Action<Task<Directory>> continueWithAction)
        {
            if (!SeekerState.currentlyLoggedIn)
            {
                Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.must_be_logged_in_to_get_dir_contents, ToastLength.Short).Show();
                return;
            }

            Action<Task> actualActionToPerform = new Action<Task>((Task connectionTask) =>
            {

                if (connectionTask.IsFaulted)
                {
                    if (!(connectionTask.Exception.InnerException is FaultPropagationException)) //i.e. only show it once.
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                        {
                            Toast tst2 = Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                            tst2.Show();
                        }));
                    }
                    throw new FaultPropagationException();
                }
                else
                {
                    //the original logic...
                    Task<Directory> t = SeekerState.SoulseekClient.GetDirectoryContentsAsync(username, dirname, null, null, isLegacy);
                    t.ContinueWith(continueWithAction);
                }

            });


            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task conTask;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out conTask))
                {
                    return;
                }
                SeekerApplication.OurCurrentLoginTask = conTask.ContinueWith(actualActionToPerform);
            }
            else
            {
                if (MainActivity.IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(actualActionToPerform, null, null))
                {
                    MainActivity.LogDebug("on finish log in we will do it");
                    return;
                }
                else
                {
                    Task<Directory> t = SeekerState.SoulseekClient.GetDirectoryContentsAsync(username, dirname, isLegacy: isLegacy);
                    t.ContinueWith(continueWithAction);
                }
            }
        }

        private void ReqFiles_Click(object sender, EventArgs e)
        {
            Action<View> action = new Action<View>((v) =>
            {
                this.Dismiss();
                ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
            });
            RequestFilesApi(searchResponse.Username, this.View, action, null);
        }


        public static void RequestFilesApi(string username, View viewForSnackBar, Action<View> goSnackBarAction, string atLocation = null)
        {
            if (!SeekerState.currentlyLoggedIn)
            {
                Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show();
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
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




        public static TreeNode<Directory> CreateTree(BrowseResponse b, bool filter, List<string> wordsToAvoid, List<string> wordsToInclude, string username, out string errorMsgToToast)
        {
            //logging code for unit tests / diagnostic.. //TODO comment out always
            //#if DEBUG
            //var root = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, Android.Net.Uri.Parse( SeekerState.SaveDataDirectoryUri) );
            //DocumentFile exists = root.FindFile(username + "_dir_response");
            ////save:
            //if(exists==null || !exists.Exists())
            //{
            //    DocumentFile f = root.CreateFile(@"custom\binary",username + "_dir_response");

            //    System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(f.Uri);
            //    //Java.IO.File musicFile = new Java.IO.File(filePath);
            //    //FileOutputStream stream = new FileOutputStream(mFile);
            //    using (System.IO.MemoryStream userListStream = new System.IO.MemoryStream())
            //    {
            //        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //        formatter.Serialize(userListStream, b);

            //    //write to binary..

            //        stream.Write(userListStream.ToArray());
            //        stream.Close();
            //    }
            //}
            //#endif
            //load
            //string username_to_load = "x";
            //exists = root.FindFile(username_to_load + "_dir_response");
            //var str = SeekerState.ActiveActivityRef.ContentResolver.OpenInputStream(exists.Uri);

            //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //b = formatter.Deserialize(str) as BrowseResponse;

            ////write to binary..

            //str.Close();
            //end logging code
            bool hideLocked = SeekerState.HideLockedResultsInBrowse;
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
                MainActivity.LogFirebase("CreateTree " + username + "  " + hideLocked + " " + e.Message + e.StackTrace);
                throw e;
            }


            //logging code for unit tests / diagnostic..
            //var root2 = DocumentFile.FromTreeUri(SeekerState.MainActivityRef, Android.Net.Uri.Parse(SeekerState.SaveDataDirectoryUri));
            //DocumentFile exists2 = root.FindFile(username + "_parsed_answer");
            //if (exists2 == null || !exists2.Exists())
            //{
            //    DocumentFile f = root2.CreateFile(@"custom\binary", username + "_parsed_answer");

            //    System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(f.Uri);
            //    //Java.IO.File musicFile = new Java.IO.File(filePath);
            //    //FileOutputStream stream = new FileOutputStream(mFile);
            //    using (System.IO.MemoryStream userListStream = new System.IO.MemoryStream())
            //    {
            //        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //        formatter.Serialize(userListStream, rootNode);

            //        //write to binary..

            //        stream.Write(userListStream.ToArray());
            //        stream.Close();
            //    }
            //}
            //end logging code for unit tests / diagnostic..


            errorMsgToToast = "";
            return rootNode;
        }



        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            bool alreadySelected = this.customAdapter.SelectedPositions.Contains<int>(e.Position);
            if (!alreadySelected)
            {

#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    e.View.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, this.Activity.Theme);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = Resources.GetDrawable(Resource.Color.cellbackSelected, this.Activity.Theme);
                }
                else
                {
                    e.View.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                }
#pragma warning restore 0618
                this.customAdapter.SelectedPositions.Add(e.Position);
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    e.View.Background = SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cell_shape_end_dldiag);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cell_shape_end_dldiag);
                }
                else
                {
                    e.View.Background = new Android.Graphics.Drawables.ColorDrawable(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cellback));
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = new Android.Graphics.Drawables.ColorDrawable(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cellback));
                }
#pragma warning restore 0618
                this.customAdapter.SelectedPositions.Remove(e.Position);
            }
            SetDownloadSelectedButtonState();
        }

        private void SetDownloadSelectedButtonState()
        {
            //backgroundtintlist is api 21+ so lower than this, there is no disabled state change which is fine.
            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            {
                if (this.customAdapter == null || this.customAdapter.SelectedPositions.Count == 0)
                {
                    //get backed in disabled color.
                    Color mainColor = SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainPurple);
                    Color backgroundColor = SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cellback);
                    int disableColor = AndroidX.Core.Graphics.ColorUtils.BlendARGB(mainColor.ToArgb(), backgroundColor.ToArgb(), 0.5f);

                    int red = Color.GetRedComponent(disableColor);
                    int green = Color.GetGreenComponent(disableColor);
                    int blue = Color.GetBlueComponent(disableColor);

                    int disableTextColor = AndroidX.Core.Graphics.ColorUtils.BlendARGB(Color.White.ToArgb(), backgroundColor.ToArgb(), 0.5f);

                    int redtc = Color.GetRedComponent(disableTextColor);
                    int greentc = Color.GetGreenComponent(disableTextColor);
                    int bluetc = Color.GetBlueComponent(disableTextColor);

                    downloadSelectedButton.SetTextColor(ColorStateList.ValueOf(Color.Argb(255, redtc, greentc, bluetc)));
                    downloadSelectedButton.BackgroundTintList = ColorStateList.ValueOf(Color.Argb(255, red, green, blue));
                    downloadSelectedButton.Clickable = false;
                }
                else
                {
                    downloadSelectedButton.SetTextColor(ColorStateList.ValueOf(Color.White));
                    downloadSelectedButton.BackgroundTintList = null;
                    downloadSelectedButton.Clickable = true;
                }
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Dismiss();
        }

        private void DownloadAll_Click(object sender, EventArgs e)
        {
            DownloadWithContinuation(GetFilesToDownload(false), this.searchResponse.Username);
        }

        private void DownloadSelected_Click(object sender, EventArgs e)
        {
            if (this.customAdapter.SelectedPositions.Count == 0)
            {
                Toast.MakeText(Context, Context.GetString(Resource.String.nothing_selected_extra), ToastLength.Short).Show();
                return;
            }

            DownloadWithContinuation(GetFilesToDownload(true), this.searchResponse.Username);
        }

        private void DownloadWithContinuation(FullFileInfo[] filesToDownload, string username)
        {
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.MainActivityRef, SeekerState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    MainActivity.LogDebug("DownloadDialog Dl_Click");
                    DownloadFiles(filesToDownload, username, false);

                }));
                try
                {
                    t.Wait(); //errors will propagate on WAIT.  They will not propagate on ContinueWith.  So you can get an exception thrown here if there is no network.
                    //we dont need to do anything if there is an exception thrown here.  Since the ContinueWith actually takes care of it by checking if task faulted..
                }
                catch (Exception exx)
                {
                    MainActivity.LogDebug("DownloadDialog DownloadWithContinuation: " + exx.Message);
                    return; //dont dismiss dialog.  that only happens on success..
                }
                Dismiss();
            }
            else
            {
                MainActivity.LogDebug("DownloadDialog Dl_Click");
                DownloadFiles(filesToDownload, username, false);
                Dismiss();
            }
        }

        private FullFileInfo[] GetFullFileInfos(IEnumerable<Soulseek.File> files)
        {
            return files.Select(it=>new FullFileInfo() { Size = it.Size, FullFileName = it.Filename, Depth = 1, wasFilenameLatin1Decoded = it.IsLatin1Decoded, wasFolderLatin1Decoded = it.IsDirectoryLatin1Decoded }).ToArray();
        }

        private FullFileInfo[] GetFilesToDownload(bool selectedOnly)
        {
            if (selectedOnly)
            {
                List<File> selectedFiles = new List<File>();
                foreach (int position in this.customAdapter.SelectedPositions)
                {
                    var file = searchResponse.GetElementAtAdapterPosition(SeekerState.HideLockedResultsInSearch, position);
                    selectedFiles.Add(file);
                }
                return GetFullFileInfos(selectedFiles.ToArray());
            }
            else
            {
                return GetFullFileInfos(searchResponse.GetFiles(SeekerState.HideLockedResultsInSearch));
            }
        }

        private void DownloadFiles(FullFileInfo[] files, string username, bool queuePaused)
        {
            var task = TransfersUtil.CreateDownloadAllTask(files, queuePaused, username);
            task.Start(); //start task immediately
            SeekerState.MainActivityRef.RunOnUiThread(() =>
            {
                if (!queuePaused)
                {
                    Toast.MakeText(Context, Resource.String.download_is_starting, ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(Context, Resource.String.DownloadIsQueued, ToastLength.Short).Show();
                }

            });
            task.Wait(); //it only waits for the downloadasync (and optionally connectasync tasks).
        }

        public static int GetQueueLength(SearchResponse s)
        {
            if (s.FreeUploadSlots > 0)
            {
                return 0;
            }
            else
            {
                return (int)(s.QueueLength);
            }
        }

        public void OnCloseClick(object sender, DialogClickEventArgs d)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
        }

        public static bool InNightMode(Context c)
        {
            try
            {
                Android.Content.Res.UiMode nightModeFlags = (c.Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask);
                switch (nightModeFlags)
                {
                    case Android.Content.Res.UiMode.NightNo:
                        return false;
                    case Android.Content.Res.UiMode.NightYes:
                        return true;
                    case Android.Content.Res.UiMode.NightUndefined:
                        return false;
                    default:
                        return false;
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase(e.Message + " InNightMode");
                return false;
            }
        }

        private void stopRefreshing()
        {
            if (this.swipeRefreshLayout != null)
            {
                this.swipeRefreshLayout.Refreshing = false;
            }
        }


        public void DirectoryReceivedContAction(Task<Directory> dirTask)
        {

            //if we have since closed the dialog, then this.View will be null
            if (this.View == null)
            {
                return;
            }
            SeekerState.MainActivityRef.RunOnUiThread(() =>
            {
                stopRefreshing();

                if (this.View == null)
                {
                    return;
                }
                if (dirTask.IsFaulted)
                {
                    if (dirTask.Exception?.InnerException?.Message != null)
                    {
                        if (dirTask.Exception.InnerException.Message.ToLower().Contains("timed out"))
                        {
                            Toast.MakeText(SeekerState.MainActivityRef, SeekerState.MainActivityRef.GetString(Resource.String.folder_request_timed_out), ToastLength.Short).Show();
                        }
                        MainActivity.LogDebug(dirTask.Exception.InnerException.Message);
                    }
                    Toast.MakeText(SeekerState.MainActivityRef, SeekerState.MainActivityRef.GetString(Resource.String.folder_request_failed), ToastLength.Short).Show();
                    MainActivity.LogDebug("DirectoryReceivedContAction faulted");
                }
                else
                {
                    MainActivity.LogDebug("DirectoryReceivedContAction successful!");
                    ListView listView = this.View.FindViewById<ListView>(Resource.Id.listView1);
                    if (listView.Count == dirTask.Result.Files.Count)
                    {
                        Toast.MakeText(SeekerState.MainActivityRef, SeekerState.MainActivityRef.GetString(Resource.String.folder_request_already_have), ToastLength.Short).Show();
                        return;
                    }
                    this.UpdateSearchResponseWithFullDirectory(dirTask.Result);
                    this.UpdateListView();
                    this.UpdateSubHeader();

                    //this.customAdapter = new DownloadCustomAdapter(Context, dirTask.Result.Files.ToList());
                    //this.customAdapter.Owner = this;
                    //listView.Adapter = (customAdapter);
                    ////listView.ItemClick += ListView_ItemClick; //already hooked up!
                    //listView.ChoiceMode = ChoiceMode.Multiple;
                }
            });
        }

        public void GetFolderContents()
        {
            try
            {
                var file = searchResponse.GetElementAtAdapterPosition(SeekerState.HideLockedResultsInSearch, 0);
                string dirname = CommonHelpers.GetDirectoryRequestFolderName(file.Filename);
                if (dirname == string.Empty)
                {
                    MainActivity.LogFirebase("The dirname is empty!!");
                    stopRefreshing();
                    return;
                }
                if (!SeekerState.HideLockedResultsInSearch && searchResponse.FileCount == 0 && searchResponse.LockedFileCount > 0)
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, SeekerApplication.GetString(Resource.String.GetFolderDoesntWorkForLockedShares), ToastLength.Short).Show();
                    stopRefreshing();
                    return;
                }
                GetFolderContentsAPI(searchResponse.Username, dirname, file.IsDirectoryLatin1Decoded, DirectoryReceivedContAction);
            }
            catch (Exception ex)
            {
                CommonHelpers.ShowReportErrorDialog(SeekerState.ActiveActivityRef, "Get Folder Contents Issue");
                MainActivity.LogFirebaseError($"{SeekerState.HideLockedResultsInSearch} {searchResponse.FileCount} {searchResponse.LockedFileCount}", ex);
            }
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.getFolderContents:
                    GetFolderContents();
                    return true;
                case Resource.Id.browseAtLocation:
                    string startingDir = CommonHelpers.GetDirectoryRequestFolderName(searchResponse.GetElementAtAdapterPosition(SeekerState.HideLockedResultsInSearch, 0).Filename);
                    Action<View> action = new Action<View>((v) =>
                    {
                        this.Dismiss();
                        ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    if (!SeekerState.HideLockedResultsInSearch && SeekerState.HideLockedResultsInBrowse && searchResponse.IsLockedOnly())
                    {
                        //this is if the user has show locked in search results but hide in browse results, then we cannot go to the folder if it is locked.
                        startingDir = null;
                    }
                    RequestFilesApi(searchResponse.Username, this.View, action, startingDir);
                    return true;
                case Resource.Id.moreInfo:
                    //TransferItem[] tempArry = new TransferItem[transferItems.Count]();
                    //transferItems.CopyTo(tempArry);
                    var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this.Context, Resource.Style.MyAlertDialogTheme);
                    var diag = builder.SetMessage(this.Context.GetString(Resource.String.queue_length_) +
                        searchResponse.QueueLength +
                        System.Environment.NewLine +
                        System.Environment.NewLine +
                        this.Context.GetString(Resource.String.upload_slots_) +
                        searchResponse.FreeUploadSlots).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
                    diag.Show();
                    //System.Threading.Thread.Sleep(100); Is this required?
                    //diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(new Android.Graphics.Color(9804764)); makes the whole button invisible...
                    //if(InNightMode(this.Context))
                    //{
                    //    diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(new Android.Graphics.Color(Android.Graphics.Color.ParseColor("#bcc1f7")));
                    //}
                    //else
                    //{
                    diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));
                    //}
                    return true;
                case Resource.Id.getUserInfo:
                    RequestedUserInfoHelper.RequestUserInfoApi(searchResponse.Username);
                    return true;
                case Resource.Id.download_folder_as_queued:
                    {
                        var filesToDownload = GetFilesToDownload(false);
                        DownloadFiles(filesToDownload, this.searchResponse.Username, true);
                        Dismiss();
                    }
                    return true;
                case Resource.Id.download_selected_as_queued:
                    {
                        if (this.customAdapter.SelectedPositions.Count == 0)
                        {
                            Toast.MakeText(Context, Context.GetString(Resource.String.nothing_selected_extra), ToastLength.Short).Show();
                            return true;
                        }
                        var filesToDownload = GetFilesToDownload(true);
                        DownloadFiles(filesToDownload, this.searchResponse.Username, true);
                        Dismiss();
                    }
                    return true;
                default:
                    return false;

            }
        }
    }

    public class FileLockedUnlockedWrapper
    {
        public Soulseek.File File;
        public bool IsLocked;
        public FileLockedUnlockedWrapper(Soulseek.File _file, bool _isLocked)
        {
            File = _file;
            IsLocked = _isLocked;
        }
    }


    public class DownloadCustomAdapter : ArrayAdapter<FileLockedUnlockedWrapper>
    {
        public List<int> SelectedPositions = new List<int>();
        public AndroidX.Fragment.App.DialogFragment Owner = null;
        public DownloadCustomAdapter(Context c, List<FileLockedUnlockedWrapper> items) : base(c, 0, items)
        {
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            DownloadItemView itemView = (DownloadItemView)convertView;
            if (null == itemView)
            {
                itemView = DownloadItemView.inflate(parent);
            }
            itemView.setItem(GetItem(position));

            if (SelectedPositions.Contains(position))
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    var cellbackSelected = Owner.Resources.GetDrawable(Resource.Color.cellbackSelected, SeekerState.ActiveActivityRef.Theme);
                    itemView.Background = cellbackSelected;
                    itemView.FindViewById<View>(Resource.Id.mainDlLayout).Background = cellbackSelected;
                }
                else
                {
                    var cellbackSelected = Owner.Resources.GetDrawable(Resource.Color.cellbackSelected);
                    itemView.Background = cellbackSelected;
                    itemView.FindViewById<View>(Resource.Id.mainDlLayout).Background = cellbackSelected;
                }
#pragma warning restore 0618
            }
            else //views get reused, hence we need to reset the color so that when we scroll the resused views arent still highlighted.
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    var cellbackNormal = SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cell_shape_end_dldiag);
                    itemView.Background = cellbackNormal;
                    itemView.FindViewById<View>(Resource.Id.mainDlLayout).Background = cellbackNormal;
                }
                else
                {
                    var cellbackNormal = new Android.Graphics.Drawables.ColorDrawable(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cellback));
                    itemView.Background = cellbackNormal;
                    itemView.FindViewById<View>(Resource.Id.mainDlLayout).Background = cellbackNormal;
                }
            }
#pragma warning restore 0618
            return itemView;
            //return base.GetView(position, convertView, parent);
        }
    }

    public class DownloadItemView : RelativeLayout
    {
        private TextView viewFilename;
        //private TextView viewSize;
        private TextView viewAttributes;
        public DownloadItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.download_row, this, true);
            setupChildren();
        }
        public DownloadItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.download_row, this, true);
            setupChildren();
        }

        public static DownloadItemView inflate(ViewGroup parent)
        {
            DownloadItemView itemView = (DownloadItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.download_view_row_dummy, parent, false);
            return itemView;
        }

        private void setupChildren()
        {
            viewFilename = FindViewById<TextView>(Resource.Id.textView1);
            //viewSize = FindViewById<TextView>(Resource.Id.textView2);
            viewAttributes = FindViewById<TextView>(Resource.Id.textView3);
        }

        public void setItem(FileLockedUnlockedWrapper wrapper)
        {
            if (wrapper.IsLocked)
            {
                viewFilename.Text = new System.String(Java.Lang.Character.ToChars(0x1F512)) + CommonHelpers.GetFileNameFromFile(wrapper.File.Filename);
            }
            else
            {
                viewFilename.Text = CommonHelpers.GetFileNameFromFile(wrapper.File.Filename);
            }
            viewAttributes.Text = CommonHelpers.GetSizeLengthAttrString(wrapper.File);
        }

        /// <summary>
        /// The other one cuts off 1 character..... They both do
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        //public static string GetFileNameFromFile(string filename)
        //{
        //    int begin = filename.LastIndexOf("\\");
        //    string clipped = filename.Substring(begin + 1);
        //    return clipped;
        //}

        private string GetStringFromAttributes(IReadOnlyCollection<Soulseek.FileAttribute> fileAttributes)
        {
            if (fileAttributes.Count == 0)
            {
                return "";
            }
            StringBuilder stringBuilder = new StringBuilder();
            string attrString = "";
            foreach (FileAttribute attr in fileAttributes)
            {

                if (attr.Type == FileAttributeType.BitDepth)
                {
                    attrString = attr.Value.ToString();
                }
                else if (attr.Type == FileAttributeType.SampleRate)
                {
                    attrString = (attr.Value / 1000.0).ToString();
                }
                else if (attr.Type == FileAttributeType.BitRate)
                {
                    attrString = attr.Value.ToString() + "kbs";
                }
                else if (attr.Type == FileAttributeType.VariableBitRate)
                {
                    attrString = attr.Value.ToString() + "kbs";
                }
                else if (attr.Type == FileAttributeType.Length)
                {
                    continue;
                }
                stringBuilder.Append(attrString);
                stringBuilder.Append(", ");
            }
            if (stringBuilder.Length <= 3)
            {
                return "";
            }
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            return stringBuilder.ToString();
        }
    }


}