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

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Common;
using Common.Browse;
using Google.Android.Material.Snackbar;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Seeker.Services;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log = Android.Util.Log;

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
            Logger.Debug("DownloadDialog create");
            searchResponse = resp;
            searchPosition = pos;
            SearchResponseTemp = resp;
            SearchPositionTemp = pos;
        }

        public DownloadDialog()
        {
            Logger.Debug("DownloadDialog create (default constructor)"); //this gets called on recreate i.e. phone tilt, etc.
            searchResponse = SearchResponseTemp;
            searchPosition = SearchPositionTemp;
        }

        // TODO2026 move
        private void UpdateSearchResponseWithFullDirectory(Soulseek.Directory d)
        {
            //normally files are like this "@@ynkmv\\Albums\\albumname (2012)\\02 - songname.mp3"
            //but when we get a dir response the files are just the end file names i.e. "02 - songname.mp3" so they cannot be downloaded like that...
            //can be fixed with d.Name + "\\" + f.Filename
            //they also do not come with any attributes.. , just the filenames (and sizes) you need if you want to download them...
            bool hideLocked = PreferencesState.HideLockedResultsInSearch;
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
            SearchResponseTemp = searchResponse = new SearchResponse(searchResponse.Username, searchResponse.Token, searchResponse.HasFreeUploadSlot, searchResponse.UploadSpeed, searchResponse.QueueLength, fullFilenameCollection);
        }

        public override void OnResume()
        {
            base.OnResume();
            Logger.Debug("OnResume Start");

            Dialog?.SetSizeProportional(.9, .9);

            Logger.Debug("OnResume End");
        }

        public override void OnAttach(Context context)
        {
            Logger.Debug("DownloadDialog OnAttach");
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
            Logger.Debug("DownloadDialog OnDetach");
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
            Logger.Debug("DownloadDialog OnCreateView");
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
            base.OnViewCreated(view, savedInstanceState);
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable_dl_dialog_specific));

            this.SetStyle((int)DialogFragmentStyle.NoTitle, 0);
            Button dl = view.FindViewById<Button>(Resource.Id.buttonDownload);
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
                Logger.Firebase("DownloadDialog search response is null");
                this.Dismiss(); //this is honestly pretty good behavior...
                return;
            }
            userHeader.Text = SeekerApplication.GetString(Resource.String.user_) + " " + searchResponse.Username;
            subHeader.Text = SeekerApplication.GetString(Resource.String.Total_) + " " + SimpleHelpers.GetSubHeaderText(searchResponse);
            headerLayout.Click += UserHeader_Click;
            Logger.Debug("Is searchResponse.Files null: " + (searchResponse.Files == null).ToString());

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
            if (!PreferencesState.HideLockedResultsInSearch)
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
            subHeader.Text = SeekerApplication.GetString(Resource.String.Total_) + " " + SimpleHelpers.GetSubHeaderText(searchResponse);
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
                Logger.Firebase(error.Message + " POPUP BAD ERROR");
            }
        }

        private void ReqFiles_Click(object sender, EventArgs e)
        {
            Action<View> action = new Action<View>((v) =>
            {
                this.Dismiss();
                ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
            });
            Browse.BrowseService.RequestFilesApi(searchResponse.Username, this.View, action, null);
        }



        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            bool alreadySelected = this.customAdapter.SelectedPositions.Contains<int>(e.Position);
            if (!alreadySelected)
            {

#pragma warning disable 0618
                if (OperatingSystem.IsAndroidVersionAtLeast(21))
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
                if (OperatingSystem.IsAndroidVersionAtLeast(21))
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
            if (OperatingSystem.IsAndroidVersionAtLeast(21))
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
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.nothing_selected_extra), ToastLength.Short);
                return;
            }

            DownloadWithContinuation(GetFilesToDownload(true), this.searchResponse.Username);
        }

        private void DownloadWithContinuation(FullFileInfo[] filesToDownload, string username)
        {
            if (SessionService.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!SessionService.ShowMessageAndCreateReconnectTask(false, out t))
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
                    Logger.Debug("DownloadDialog Dl_Click");
                    DownloadFiles(filesToDownload, username, false);

                }));
                try
                {
                    t.Wait(); //errors will propagate on WAIT.  They will not propagate on ContinueWith.  So you can get an exception thrown here if there is no network.
                    //we dont need to do anything if there is an exception thrown here.  Since the ContinueWith actually takes care of it by checking if task faulted..
                }
                catch (Exception exx)
                {
                    Logger.Debug("DownloadDialog DownloadWithContinuation: " + exx.Message);
                    return; //dont dismiss dialog.  that only happens on success..
                }
                Dismiss();
            }
            else
            {
                Logger.Debug("DownloadDialog Dl_Click");
                DownloadFiles(filesToDownload, username, false);
                Dismiss();
            }
        }

        private FullFileInfo[] GetFilesToDownload(bool selectedOnly)
        {
            if (selectedOnly)
            {
                List<File> selectedFiles = new List<File>();
                foreach (int position in this.customAdapter.SelectedPositions)
                {
                    var file = searchResponse.GetElementAtAdapterPosition(PreferencesState.HideLockedResultsInSearch, position);
                    selectedFiles.Add(file);
                }
                return BrowseUtils.GetFullFileInfos(selectedFiles.ToArray());
            }
            else
            {
                return BrowseUtils.GetFullFileInfos(searchResponse.GetFiles(PreferencesState.HideLockedResultsInSearch));
            }
        }

        private void DownloadFiles(FullFileInfo[] files, string username, bool queuePaused)
        {
            var task = DownloadService.CreateDownloadAllTask(files, queuePaused, username);
            task.Start(); //start task immediately
            task.Wait(); //it only waits for the downloadasync (and optionally connectasync tasks).
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
                Logger.Firebase(e.Message + " InNightMode");
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


        public void DirectoryReceivedContAction(Task<IReadOnlyCollection<Directory>> dirTask)
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
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.folder_request_timed_out), ToastLength.Short);
                        }
                        Logger.Debug(dirTask.Exception.InnerException.Message);
                    }
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.folder_request_failed), ToastLength.Short);
                    Logger.Debug("DirectoryReceivedContAction faulted");
                }
                else
                {
                    Logger.Debug("DirectoryReceivedContAction successful!");
                    ListView listView = this.View.FindViewById<ListView>(Resource.Id.listView1);
                    var directory = dirTask.Result.First();
                    if (listView.Count == directory.Files.Count)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.folder_request_already_have), ToastLength.Short);
                        return;
                    }
                    this.UpdateSearchResponseWithFullDirectory(directory);
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
                var file = searchResponse.GetElementAtAdapterPosition(PreferencesState.HideLockedResultsInSearch, 0);
                string dirname = SimpleHelpers.GetDirectoryRequestFolderName(file.Filename);
                if (dirname == string.Empty)
                {
                    Logger.Firebase("The dirname is empty!!");
                    stopRefreshing();
                    return;
                }
                if (!PreferencesState.HideLockedResultsInSearch && searchResponse.FileCount == 0 && searchResponse.LockedFileCount > 0)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.GetFolderDoesntWorkForLockedShares), ToastLength.Short);
                    stopRefreshing();
                    return;
                }
                Browse.BrowseService.GetFolderContentsAPI(searchResponse.Username, dirname, file.IsDirectoryLatin1Decoded, DirectoryReceivedContAction);
            }
            catch (Exception ex)
            {
                UiHelpers.ShowReportErrorDialog(SeekerState.ActiveActivityRef, "Get Folder Contents Issue");
                Logger.FirebaseError($"{PreferencesState.HideLockedResultsInSearch} {searchResponse.FileCount} {searchResponse.LockedFileCount}", ex);
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
                    string startingDir = SimpleHelpers.GetDirectoryRequestFolderName(searchResponse.GetElementAtAdapterPosition(PreferencesState.HideLockedResultsInSearch, 0).Filename);
                    Action<View> action = new Action<View>((v) =>
                    {
                        this.Dismiss();
                        ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    if (!PreferencesState.HideLockedResultsInSearch && PreferencesState.HideLockedResultsInBrowse && searchResponse.IsLockedOnly())
                    {
                        //this is if the user has show locked in search results but hide in browse results, then we cannot go to the folder if it is locked.
                        startingDir = null;
                    }
                    Browse.BrowseService.RequestFilesApi(searchResponse.Username, this.View, action, startingDir);
                    return true;
                case Resource.Id.moreInfo:
                    //TransferItem[] tempArry = new TransferItem[transferItems.Count]();
                    //transferItems.CopyTo(tempArry);
                    //TODOASAP - hasfreeupload slots is now a boolean, fix the string.
                    var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this.Context);
                    var diag = builder.SetMessage(this.Context.GetString(Resource.String.queue_length_) +
                        searchResponse.QueueLength +
                        System.Environment.NewLine +
                        System.Environment.NewLine +
                        this.Context.GetString(Resource.String.upload_slots_) +
                        searchResponse.HasFreeUploadSlot).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
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
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.nothing_selected_extra), ToastLength.Short);
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
                if (OperatingSystem.IsAndroidVersionAtLeast(21))
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
                if (OperatingSystem.IsAndroidVersionAtLeast(21))
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
                viewFilename.Text = SimpleHelpers.LOCK_EMOJI + SimpleHelpers.GetFileNameFromFile(wrapper.File.Filename);
            }
            else
            {
                viewFilename.Text = SimpleHelpers.GetFileNameFromFile(wrapper.File.Filename);
            }
            viewAttributes.Text = SimpleHelpers.GetSizeLengthAttrString(wrapper.File);
        }
    }
}