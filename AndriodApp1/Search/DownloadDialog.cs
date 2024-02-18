﻿/*
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

using AndriodApp1.Extensions.SearchResponseExtensions;
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

namespace AndriodApp1
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
            bool hideLocked = SoulSeekState.HideLockedResultsInSearch;
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

            Window window = Dialog.Window;//  getDialog().getWindow();
            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;
            int height = size.Y;

            window.SetLayout((int)(width * 0.90), (int)(height * 0.90));//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
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
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable_dl_dialog_specific));

            this.SetStyle((int)DialogFragmentStyle.NoTitle, 0);
            Button dl = view.FindViewById<Button>(Resource.Id.buttonDownload);
            log.Debug(MainActivity.logCatTag, "Is dl null: " + (dl == null).ToString());
            dl.Click += DlAll_Click;
            Button cancel = view.FindViewById<Button>(Resource.Id.buttonCancel);
            cancel.Click += Cancel_Click;
            downloadSelectedButton = view.FindViewById<Button>(Resource.Id.buttonDownloadSelected);
            downloadSelectedButton.Click += DlSelected_Click;
            Button reqFiles = view.FindViewById<Button>(Resource.Id.buttonRequestDirectories);
            reqFiles.Click += ReqFiles_Click;
            //selectedPositions.Clear();
            TextView userHeader = view.FindViewById<TextView>(Resource.Id.userHeader);
            TextView subHeader = view.FindViewById<TextView>(Resource.Id.userHeaderSub);

            swipeRefreshLayout = view.FindViewById<AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout>(Resource.Id.swipeToRefreshLayout);
            swipeRefreshLayout.SetProgressBackgroundColorSchemeColor(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.swipeToRefreshBackground).ToArgb());
            swipeRefreshLayout.SetColorSchemeColors(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.swipeToRefreshProgress).ToArgb());
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
            if (!SoulSeekState.HideLockedResultsInSearch)
            {
                adapterList.AddRange(searchResponse.LockedFiles.ToList().Select(x => new FileLockedUnlockedWrapper(x, true)));
            }
            this.customAdapter = new DownloadCustomAdapter(SoulSeekState.MainActivityRef, adapterList);
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
                PopupMenu popup = new PopupMenu(SoulSeekState.MainActivityRef, sender as View, GravityFlags.Right);
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
                Snackbar.Make(SeekerApplication.GetViewForSnackbar(), SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_contacting), Snackbar.LengthShort).Show();
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("RequestFilesLogic: " + e.Message + e.StackTrace);
            }
            Task<BrowseResponse> browseResponseTask = null;
            try
            {
                browseResponseTask = SoulSeekState.SoulseekClient.BrowseAsync(username);
            }
            catch (InvalidOperationException)
            {   //this can still happen on ReqFiles_Click.. maybe for the first check we were logged in but for the second we somehow were not..
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show(); });
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
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_timeout), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is ConnectionException && br.Exception?.InnerException?.InnerException is TimeoutException)
                {
                    //timeout - this time when the connection was established, but the user has not written to us in over 15 (timeout) seconds. I tested and generally this is fixed by simply retrying.
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_timeout), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is ConnectionException && br.Exception?.InnerException?.InnerException != null && br.Exception.InnerException.InnerException.ToString().ToLower().Contains("network subsystem is down"))
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_down), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException != null && br.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_nodirectconnection), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted && br.Exception?.InnerException is UserOfflineException)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, String.Format(SeekerApplication.GetString(Resource.String.CannotBrowseUsernameOffline), username), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted)
                {
                    //shouldnt get here
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, String.Format(SeekerApplication.GetString(Resource.String.FailedToBrowseUsernameUnspecifiedError), username), ToastLength.Short).Show(); });
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
                    SoulSeekState.OnBrowseResponseReceived(br.Result, tree, username, atLocation);
                }

                SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    if (tree == null)
                    {
                        //error case
                        if (errorString != null && errorString != string.Empty)
                        {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, errorString, ToastLength.Long).Show();
                        }
                        else
                        {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_wefailedtoparse), ToastLength.Long).Show();
                        }
                        return;
                    }
                    if (SoulSeekState.MainActivityRef != null && ((AndroidX.ViewPager.Widget.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).CurrentItem == 3) //AND it is our current activity...
                    {
                        if (SoulSeekState.MainActivityRef.Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Started))
                        {
                            return; //they are already there... they see it populating, no need to show them notification...
                        }
                    }

                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        SoulSeekState.ActiveActivityRef.StartActivity(intent);
                        //((AndroidX.ViewPager.Widget.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });

                    try
                    {
                        Snackbar sb = Snackbar.Make(SeekerApplication.GetViewForSnackbar(), SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SoulSeekState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                        (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainTextColor));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                        sb.Show();
                    }
                    catch
                    {
                        try
                        {
                            Snackbar sb = Snackbar.Make(SoulSeekState.MainActivityRef.CurrentFocus, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SoulSeekState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                            (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainTextColor));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                            sb.Show();
                        }
                        catch
                        {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_response_received), ToastLength.Short).Show();
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
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_be_logged_in_to_get_dir_contents, ToastLength.Short).Show();
                return;
            }

            Action<Task> actualActionToPerform = new Action<Task>((Task connectionTask) =>
            {

                if (connectionTask.IsFaulted)
                {
                    if (!(connectionTask.Exception.InnerException is FaultPropagationException)) //i.e. only show it once.
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                        {
                            Toast tst2 = Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                            tst2.Show();
                        }));
                    }
                    throw new FaultPropagationException();
                }
                else
                {
                    //the original logic...
                    Task<Directory> t = SoulSeekState.SoulseekClient.GetDirectoryContentsAsync(username, dirname, null, null, isLegacy);
                    t.ContinueWith(continueWithAction);
                }

            });


            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task conTask;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out conTask))
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
                    Task<Directory> t = SoulSeekState.SoulseekClient.GetDirectoryContentsAsync(username, dirname, isLegacy: isLegacy);
                    t.ContinueWith(continueWithAction);
                }
            }
        }

        private void ReqFiles_Click(object sender, EventArgs e)
        {
            Action<View> action = new Action<View>((v) =>
            {
                this.Dismiss();
                ((AndroidX.ViewPager.Widget.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
            });
            RequestFilesApi(searchResponse.Username, this.View, action, null);
        }


        public static void RequestFilesApi(string username, View viewForSnackBar, Action<View> goSnackBarAction, string atLocation = null)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show();
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() => { RequestFilesLogic(username, viewForSnackBar, goSnackBarAction, atLocation); }));
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
            //var root = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, Android.Net.Uri.Parse( SoulSeekState.SaveDataDirectoryUri) );
            //DocumentFile exists = root.FindFile(username + "_dir_response");
            ////save:
            //if(exists==null || !exists.Exists())
            //{
            //    DocumentFile f = root.CreateFile(@"custom\binary",username + "_dir_response");

            //    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(f.Uri);
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
            //var str = SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(exists.Uri);

            //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //b = formatter.Deserialize(str) as BrowseResponse;

            ////write to binary..

            //str.Close();
            //end logging code
            bool hideLocked = SoulSeekState.HideLockedResultsInBrowse;
            if (b.DirectoryCount == 0 && b.LockedDirectoryCount != 0 && hideLocked)
            {
                errorMsgToToast = SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_onlylocked);
                return null;
            }
            else if (b.DirectoryCount == 0 && b.LockedDirectoryCount == 0)
            {
                errorMsgToToast = SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_none);
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
            //var root2 = DocumentFile.FromTreeUri(SoulSeekState.MainActivityRef, Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri));
            //DocumentFile exists2 = root.FindFile(username + "_parsed_answer");
            //if (exists2 == null || !exists2.Exists())
            //{
            //    DocumentFile f = root2.CreateFile(@"custom\binary", username + "_parsed_answer");

            //    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(f.Uri);
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


        private void DownloadSelectedLogic_NotQueued()
        {
            DownloadSelectedLogic(false);
        }


        private void DownloadSelectedLogic(bool queuePaused)
        {
            bool hideLocked = SoulSeekState.HideLockedResultsInSearch;
            try
            {
                List<Task> tsks = new List<Task>();
                foreach (int position in this.customAdapter.SelectedPositions) //nullref?
                {
                    try
                    {
                        Task tsk = CreateDownloadTask(searchResponse.GetElementAtAdapterPosition(hideLocked, position), queuePaused);
                        if (tsk == null)
                        {
                            Action a = new Action(() => { Toast.MakeText(this.activity, this.activity.GetString(Resource.String.error_duplicate), ToastLength.Long).Show(); });
                            this.activity?.RunOnUiThread(a);
                            return;
                        }
                        tsk.Start();
                        tsks.Add(tsk);
                    }
                    catch (Exception error)
                    {
                        Action a = new Action(() => { Toast.MakeText(this.activity, this.activity.GetString(Resource.String.error_) + error.Message, ToastLength.Long).Show(); });
                        MainActivity.LogFirebase(error.Message + " DlSelected_Click");
                        this.activity.RunOnUiThread(a);
                    }

                }
                if (!queuePaused)
                {
                    Toast.MakeText(Context, Resource.String.download_is_starting, ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(Context, Resource.String.DownloadIsQueued, ToastLength.Short).Show();
                }
                foreach (Task tsk in tsks)
                {
                    tsk.Wait();
                }
                Dismiss();

            }
            catch (DuplicateTransferException)
            {
                string dupMsg = this.activity.GetString(Resource.String.error_duplicates);
                Action a = new Action(() => { Toast.MakeText(this.activity, dupMsg, ToastLength.Long).Show(); });
                MainActivity.LogFirebase(dupMsg + " DlSelected_Click");
                this.activity.RunOnUiThread(a);
            }
            catch (AggregateException age)
            {
                if (age.InnerException is DuplicateTransferException)
                {
                    string dupMsg = this.activity.GetString(Resource.String.error_duplicates);
                    Action a = new Action(() => { Toast.MakeText(this.activity, dupMsg, ToastLength.Long).Show(); });
                    MainActivity.LogFirebase(dupMsg + " DlSelected_Click");
                    this.activity.RunOnUiThread(a);
                }
                else
                {
                    Action a = new Action(() => { Toast.MakeText(this.activity, age.Message, ToastLength.Long).Show(); });
                    MainActivity.LogFirebase(age.Message + " DlSelected_Click");
                    this.activity.RunOnUiThread(a);
                }
            }
        }

        private void DlSelected_Click(object sender, EventArgs e)
        {
            MainActivity.LogDebug("DownloadDialog DlSelected_Click");
            MainActivity.LogDebug("this.customAdapter.SelectedPositions.Count = " + this.customAdapter.SelectedPositions.Count);
            if (this.customAdapter.SelectedPositions.Count == 0)
            {
                Toast.MakeText(Context, Context.GetString(Resource.String.nothing_selected_extra), ToastLength.Short).Show();
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(DownloadSelectedLogic_NotQueued);
                }));
            }
            else
            {
                DownloadSelectedLogic(false);
            }

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
                    e.View.Background = SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.cell_shape_end_dldiag);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.cell_shape_end_dldiag);
                }
                else
                {
                    e.View.Background = new Android.Graphics.Drawables.ColorDrawable(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.cellback));
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = new Android.Graphics.Drawables.ColorDrawable(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.cellback));
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
                    Color mainColor = SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainPurple);
                    Color backgroundColor = SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.cellback);
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

        private void DlAll_Click(object sender, EventArgs e)
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
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    MainActivity.LogDebug("DownloadDialog Dl_Click");
                    DownloadAll(false);

                }));
                try
                {
                    t.Wait(); //errors will propagate on WAIT.  They will not propagate on ContinueWith.  So you can get an exception thrown here if there is no network.
                    //we dont need to do anything if there is an exception thrown here.  Since the ContinueWith actually takes care of it by checking if task faulted..
                }
                catch (Exception exx)
                {
                    MainActivity.LogDebug("DownloadDialog DlAll_Click: " + exx.Message);
                    return; //dont dismiss dialog.  that only happens on success..
                }
                Dismiss();
            }
            else
            {
                MainActivity.LogDebug("DownloadDialog Dl_Click");
                DownloadAll(false);
                Dismiss();
            }
        }

        private void DownloadAll(bool queuePaused)
        {
            var task = CreateDownloadAllTask(queuePaused);
            task.Start(); //start task immediately
            SoulSeekState.MainActivityRef.RunOnUiThread(() =>
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

        private Task CreateDownloadTask(Soulseek.File file, bool queuePaused)
        {
            //TODO TODO downloadInfoList is stale..... not what you want to use....
            //TransfersFragment frag = (StaticHacks.TransfersFrag as TransfersFragment);
            if (TransfersFragment.TransferItemManagerDL != null)
            {
                bool dup = TransfersFragment.TransferItemManagerDL.Exists(file.Filename, searchResponse.Username, file.Size);
                if (dup)
                {
                    string msg = "Duplicate Detected: user:" + searchResponse.Username + "filename: " + file.Filename; //internal
                    MainActivity.LogDebug("CreateDownloadTask " + msg);
                    MainActivity.LogFirebase(msg);
                    Action a = new Action(() => { Toast.MakeText(this.activity, this.activity.GetString(Resource.String.error_duplicate), ToastLength.Long); });
                    SoulSeekState.MainActivityRef.RunOnUiThread(a);
                    return null;
                }
            }

            MainActivity.LogDebug("CreateDownloadTask");
            Task task = new Task(() =>
            {
                SetupAndDownloadFile(searchResponse.Username, file.Filename, file.Size, GetQueueLength(searchResponse), 1, queuePaused, file.IsLatin1Decoded, file.IsDirectoryLatin1Decoded, out _);

            });
            return task;
        }


        public static void SetupAndDownloadFile(string username, string fname, long size, int queueLength, int depth, bool queuePaused, bool wasLatin1Decoded, bool wasFolderLatin1Decoded, out bool errorExists)
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
                transferItem.Filename = CommonHelpers.GetFileNameFromFile(downloadInfo.fullFilename);
                transferItem.FolderName = CommonHelpers.GetFolderNameFromFile(downloadInfo.fullFilename, depth);
                transferItem.Username = downloadInfo.username;
                transferItem.FullFilename = downloadInfo.fullFilename;
                transferItem.Size = downloadInfo.Size;
                transferItem.QueueLength = downloadInfo.QueueLength;
                transferItem.WasFilenameLatin1Decoded = wasLatin1Decoded;
                transferItem.WasFolderLatin1Decoded = wasFolderLatin1Decoded;

                if (!queuePaused)
                {
                    try
                    {
                        TransfersFragment.SetupCancellationToken(transferItem, downloadInfo.CancellationTokenSource, out oldCts); //if its already there we dont add it..
                    }
                    catch (Exception errr)
                    {
                        MainActivity.LogFirebase("concurrency issue: " + errr); //I think this is fixed by changing to concurrent dict but just in case...
                    }
                }
                transferItem = TransfersFragment.TransferItemManagerDL.AddIfNotExistAndReturnTransfer(transferItem, out exists);

                if (queuePaused)
                {
                    transferItem.State = TransferStates.Cancelled;
                    MainActivity.InvokeDownloadAddedUINotify(new DownloadAddedEventArgs(null)); //otherwise the ui will not refresh.
                    return;
                }

                downloadInfo.TransferItemReference = transferItem;




                dlTask = DownloadFileAsync(username, fname, size, cancellationTokenSource, depth, wasLatin1Decoded, wasFolderLatin1Decoded);

                var e = new DownloadAddedEventArgs(downloadInfo);
                downloadInfo.downloadTask = dlTask;
                Action<Task> continuationActionSaveFile = MainActivity.DownloadContinuationActionUI(e);
                dlTask.ContinueWith(continuationActionSaveFile);
                MainActivity.InvokeDownloadAddedUINotify(e);




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
                    TransfersFragment.SetupCancellationToken(transferItem, oldCts, out _); //put it back..
                }
            }
        }

        /// <summary>
        /// takes care of resuming incomplete downloads, switching between mem and file backed, creating the incompleteUri dir.
        /// its the same as the old SoulSeekState.SoulseekClient.DownloadAsync but with a few bells and whistles...
        /// </summary>
        /// <param name="username"></param>
        /// <param name="fullfilename"></param>
        /// <param name="size"></param>
        /// <param name="cts"></param>
        /// <param name="incompleteUri"></param>
        /// <returns></returns>
        public static Task DownloadFileAsync(string username, string fullfilename, long? size, CancellationTokenSource cts, int depth = 1, bool isFileDecodedLegacy = false, bool isFolderDecodedLegacy = false) //an indicator for how much of the full filename to use...
        {
            MainActivity.LogDebug("DownloadFileAsync - " + fullfilename);
            Task dlTask = null;
            if (SoulSeekState.MemoryBackedDownload)
            {
                dlTask =
                    SoulSeekState.SoulseekClient.DownloadAsync(
                        username: username,
                        filename: fullfilename,
                        size: size,
                        options: new TransferOptions(governor: SeekerApplication.SpeedLimitHelper.OurDownloadGoverner),
                        cancellationToken: cts.Token,
                        isLegacy: isFileDecodedLegacy,
                        isFolderDecodedLegacy: isFolderDecodedLegacy);
            }
            else
            {



                long partialLength = 0;

                dlTask = SoulSeekState.SoulseekClient.DownloadAsync(
                        username: username,
                        filename: fullfilename,
                        null,
                        size: size,
                        startOffset: partialLength, //this will get populated
                        options: new TransferOptions(disposeOutputStreamOnCompletion: true, governor: SeekerApplication.SpeedLimitHelper.OurDownloadGoverner),
                        cancellationToken: cts.Token,
                        streamTask: GetStreamTask(username, fullfilename, depth),
                        isFilenameDecodedLegacy: isFileDecodedLegacy,
                        isFolderDecodedLegacy: isFolderDecodedLegacy);


                //System.IO.Stream streamToWriteTo = MainActivity.GetIncompleteStream(username, fullfilename, out incompleteUri, out partialLength);


                //dlTask = SoulSeekState.SoulseekClient.DownloadAsync(
                //        username: username,
                //        filename: fullfilename,
                //        streamToWriteTo,
                //        size: size,
                //        startOffset:partialLength, //this will get populated
                //        options: new TransferOptions(disposeOutputStreamOnCompletion: true),
                //        cancellationToken: cts.Token);



            }
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
                    System.IO.Stream streamToWriteTo = MainActivity.GetIncompleteStream(username, fullfilename, depth, out incompleteUri, out incompleteUriDirectory, out partialLength); //something here to denote...
                    return new Tuple<System.IO.Stream, long, string, string>(streamToWriteTo, partialLength, incompleteUri.ToString(), incompleteUriDirectory.ToString());
                });
            return task;
        }


        ///// <summary>
        ///// takes care of resuming incomplete downloads, switching between mem and file backed, creating the incompleteUri dir.
        ///// its the same as the old SoulSeekState.SoulseekClient.DownloadAsync but with a few bells and whistles...
        ///// </summary>
        ///// <param name="username"></param>
        ///// <param name="fullfilename"></param>
        ///// <param name="size"></param>
        ///// <param name="cts"></param>
        ///// <param name="incompleteUri"></param>
        ///// <returns></returns>
        //public static async Task DownloadFileAsync2(string username, string fullfilename, long size, CancellationTokenSource cts)
        //{
        //    Task dlTask = null;
        //    if (SoulSeekState.MemoryBackedDownload)
        //    {
        //        dlTask =
        //            SoulSeekState.SoulseekClient.DownloadAsync(
        //                username: username,
        //                filename: fullfilename,
        //                size: size,
        //                cancellationToken: cts.Token);
        //        //incompleteUri = null;
        //    }
        //    else
        //    {
        //        long partialLength = 0;
        //        System.IO.Stream streamToWriteTo = MainActivity.GetIncompleteStream(username, fullfilename, out _, out partialLength);

        //        dlTask = SoulSeekState.SoulseekClient.DownloadAsync(
        //                username: username,
        //                filename: fullfilename,
        //                streamToWriteTo,
        //                size: size,
        //                startOffset: partialLength,
        //                options: new TransferOptions(disposeOutputStreamOnCompletion: true),
        //                cancellationToken: cts.Token);
        //    }
        //    return dlTask;
        //}


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

        private Task CreateDownloadAllTask(bool queuePaused)
        {
            MainActivity.LogDebug("CreateDownloadAllTask");
            Task task = new Task(() =>
            {
                foreach (Soulseek.File file in searchResponse.GetFiles(SoulSeekState.HideLockedResultsInSearch))
                {
                    SetupAndDownloadFile(searchResponse.Username, file.Filename, file.Size, GetQueueLength(searchResponse), 1, queuePaused, file.IsLatin1Decoded, file.IsDirectoryLatin1Decoded, out _);
                }
            });
            return task;
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

        public void DirectoryReceivedContAction(Task<Directory> dirTask)
        {

            //if we have since closed the dialog, then this.View will be null
            if (this.View == null)
            {
                return;
            }
            SoulSeekState.MainActivityRef.RunOnUiThread(() =>
            {

                if (this.swipeRefreshLayout != null)
                {
                    this.swipeRefreshLayout.Refreshing = false;
                }

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
                            Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.folder_request_timed_out), ToastLength.Short).Show();
                        }
                        MainActivity.LogDebug(dirTask.Exception.InnerException.Message);
                    }
                    Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.folder_request_failed), ToastLength.Short).Show();
                    MainActivity.LogDebug("DirectoryReceivedContAction faulted");
                }
                else
                {
                    MainActivity.LogDebug("DirectoryReceivedContAction successful!");
                    ListView listView = this.View.FindViewById<ListView>(Resource.Id.listView1);
                    if (listView.Count == dirTask.Result.Files.Count)
                    {
                        Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.folder_request_already_have), ToastLength.Short).Show();
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
            var file = searchResponse.GetElementAtAdapterPosition(SoulSeekState.HideLockedResultsInSearch, 0);
            string dirname = CommonHelpers.GetDirectoryRequestFolderName(file.Filename);
            if (dirname == string.Empty)
            {
                MainActivity.LogFirebase("The dirname is empty!!");
                return;
            }
            if (!SoulSeekState.HideLockedResultsInSearch && searchResponse.FileCount == 0 && searchResponse.LockedFileCount > 0)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, SeekerApplication.GetString(Resource.String.GetFolderDoesntWorkForLockedShares), ToastLength.Short).Show();
                return;
            }
            GetFolderContentsAPI(searchResponse.Username, dirname, file.IsDirectoryLatin1Decoded, DirectoryReceivedContAction);
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.getFolderContents:
                    GetFolderContents();
                    return true;
                case Resource.Id.browseAtLocation:
                    string startingDir = CommonHelpers.GetDirectoryRequestFolderName(searchResponse.GetElementAtAdapterPosition(SoulSeekState.HideLockedResultsInSearch, 0).Filename);
                    Action<View> action = new Action<View>((v) =>
                    {
                        this.Dismiss();
                        ((AndroidX.ViewPager.Widget.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    if (!SoulSeekState.HideLockedResultsInSearch && SoulSeekState.HideLockedResultsInBrowse && searchResponse.IsLockedOnly())
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
                    diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainTextColor));
                    //}
                    return true;
                case Resource.Id.getUserInfo:
                    RequestedUserInfoHelper.RequestUserInfoApi(searchResponse.Username);
                    return true;
                case Resource.Id.download_folder_as_queued:
                    DownloadAll(true);
                    Dismiss();
                    return true;
                case Resource.Id.download_selected_as_queued:
                    if (this.customAdapter.SelectedPositions.Count == 0)
                    {
                        Toast.MakeText(Context, Context.GetString(Resource.String.nothing_selected_extra), ToastLength.Short).Show();
                        return true;
                    }
                    DownloadSelectedLogic(true);
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
                    var cellbackSelected = Owner.Resources.GetDrawable(Resource.Color.cellbackSelected, SoulSeekState.ActiveActivityRef.Theme);
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
                    var cellbackNormal = SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.cell_shape_end_dldiag);
                    itemView.Background = cellbackNormal;
                    itemView.FindViewById<View>(Resource.Id.mainDlLayout).Background = cellbackNormal;
                }
                else
                {
                    var cellbackNormal = new Android.Graphics.Drawables.ColorDrawable(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.cellback));
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