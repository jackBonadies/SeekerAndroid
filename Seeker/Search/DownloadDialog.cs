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
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Common;
using Common.Browse;
using Common.Search;
using Google.Android.Material.BottomSheet;
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
    class DownloadDialog : BottomSheetDialogFragment, PopupMenu.IOnMenuItemClickListener
    {
        public const string DOWNLOAD_DIALOG_FRAGMENT = "download_dialog_fragment";

        // this is view state
        private static SearchResponse SearchResponse = null;

        private DownloadCustomAdapter customAdapter = null;
        public DownloadDialog(int pos, SearchResponse resp)
        {
            Logger.Debug("DownloadDialog create");
            SearchResponse = resp;
        }

        public DownloadDialog()
        {
            Logger.Debug("DownloadDialog create (default constructor)"); //this gets called on recreate i.e. phone tilt, etc.
        }

        public override void OnAttach(Context context)
        {
            Logger.Debug("DownloadDialog OnAttach");
            base.OnAttach(context);
        }

        public override void OnDetach()
        {
            Logger.Debug("DownloadDialog OnDetach");
            SearchFragment.dlDialogShown = false;
            base.OnDetach();
        }

        public override void OnDestroy()
        {
            SearchFragment.dlDialogShown = false;
            base.OnDestroy();
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

        private class SheetStateCallback : BottomSheetBehavior.BottomSheetCallback
        {
            private readonly View contentView;
            private readonly float density;
            private readonly int peekHeight;

            public SheetStateCallback(View contentView, float density, int peekHeight)
            {
                this.contentView = contentView;
                this.density = density;
                this.peekHeight = peekHeight;
            }

            public override void OnStateChanged(View bottomSheetView, int newState)
            {
                var bg = contentView.Background?.Mutate() as GradientDrawable;
                if (bg != null)
                {
                    float radius = newState == BottomSheetBehavior.StateExpanded ? 0f : 28f * density;
                    bg.SetCornerRadii(new float[] { radius, radius, radius, radius, 0, 0, 0, 0 });
                    contentView.Background = bg;
                }
            }

            public override void OnSlide(View bottomSheetView, float slideOffset)
            {
                if (slideOffset >= 0f && bottomSheetView.Height > 0)
                {
                    int hiddenPortion = (int)((1f - slideOffset) * (bottomSheetView.Height - peekHeight));
                    contentView.SetPadding(
                        contentView.PaddingLeft,
                        contentView.PaddingTop,
                        contentView.PaddingRight,
                        Math.Max(hiddenPortion, 0));
                }
            }
        }

        private Button downloadButton = null;
        private AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout swipeRefreshLayout = null;

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var dialog = (BottomSheetDialog)Dialog;
            var bottomSheet = dialog.FindViewById<View>(Resource.Id.design_bottom_sheet);
            if (bottomSheet != null)
            {
                var behavior = BottomSheetBehavior.From(bottomSheet);
                behavior.State = BottomSheetBehavior.StateCollapsed;
                int peekHeightPx = (int)(Resources.DisplayMetrics.HeightPixels * 0.85);
                behavior.PeekHeight = peekHeightPx;
                behavior.SkipCollapsed = false;

                var layoutParams = bottomSheet.LayoutParameters;
                layoutParams.Height = ViewGroup.LayoutParams.MatchParent;
                bottomSheet.LayoutParameters = layoutParams;

                behavior.AddBottomSheetCallback(new SheetStateCallback(view, Resources.DisplayMetrics.Density, peekHeightPx));

                // Set initial bottom padding so footer is visible at peek height
                int initialPadding = Resources.DisplayMetrics.HeightPixels - peekHeightPx;
                view.SetPadding(view.PaddingLeft, view.PaddingTop, view.PaddingRight, initialPadding);
            }

            downloadButton = view.FindViewById<Button>(Resource.Id.buttonDownload);
            downloadButton.Click += Download_Click;

            Button browseButton = view.FindViewById<Button>(Resource.Id.buttonBrowse);
            browseButton.Click += Browse_Click;

            Button closeButton = view.FindViewById<Button>(Resource.Id.buttonClose);
            closeButton.Click += Close_Click;

            TextView folderNameHeader = view.FindViewById<TextView>(Resource.Id.folderNameHeader);
            TextView userHeader = view.FindViewById<TextView>(Resource.Id.userHeader);
            TextView subHeader = view.FindViewById<TextView>(Resource.Id.userHeaderSub);

            swipeRefreshLayout = view.FindViewById<AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout>(Resource.Id.swipeToRefreshLayout);
            swipeRefreshLayout.SetProgressBackgroundColorSchemeColor(UiHelpers.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.swipeToRefreshBackground).ToArgb());
            swipeRefreshLayout.SetColorSchemeColors(UiHelpers.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.swipeToRefreshProgress).ToArgb());
            swipeRefreshLayout.SetOnRefreshListener(new OnRefreshListenerGetFolder(this));

            ViewGroup headerLayout = view.FindViewById<ViewGroup>(Resource.Id.header1);

            if (SearchResponse == null)
            {
                Logger.Firebase("DownloadDialog search response is null");
                this.Dismiss();
                return;
            }

            folderNameHeader.Text = SimpleHelpers.GetFolderNameForSearchResult(SearchResponse);
            userHeader.Text = SearchResponse.Username;
            subHeader.Text = SimpleHelpers.GetSubHeaderText(SearchResponse);
            headerLayout.Click += UserHeader_Click;
            Logger.Debug("Is searchResponse.Files null: " + (SearchResponse.Files == null).ToString());

            ListView listView = view.FindViewById<ListView>(Resource.Id.listView1);
            listView.ItemClick += ListView_ItemClick;
            listView.ChoiceMode = ChoiceMode.Multiple;
            UpdateListView();
            UpdateDownloadButtonText();
        }

        private void UpdateListView()
        {
            ListView listView = this.View.FindViewById<ListView>(Resource.Id.listView1);
            List<FileLockedUnlockedWrapper> adapterList = new List<FileLockedUnlockedWrapper>();
            adapterList.AddRange(SearchResponse.Files.ToList().Select(x => new FileLockedUnlockedWrapper(x, false)));
            if (!PreferencesState.HideLockedResultsInSearch)
            {
                adapterList.AddRange(SearchResponse.LockedFiles.ToList().Select(x => new FileLockedUnlockedWrapper(x, true)));
            }
            this.customAdapter = new DownloadCustomAdapter(SeekerState.MainActivityRef, adapterList);
            this.customAdapter.Owner = this;
            listView.Adapter = (customAdapter);
        }

        private void UpdateSubHeader()
        {
            TextView subHeader = this.View.FindViewById<TextView>(Resource.Id.userHeaderSub);
            subHeader.Text = SeekerApplication.GetString(Resource.String.Total_) + " " + SimpleHelpers.GetSubHeaderText(SearchResponse);
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

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            bool alreadySelected = this.customAdapter.SelectedPositions.Contains<int>(e.Position);
            if (!alreadySelected)
            {
#pragma warning disable 0618
                e.View.Background = Resources.GetDrawable(Resource.Color.batchSelectHighlight, this.Activity.Theme);
                e.View.FindViewById(Resource.Id.mainDlLayout).Background = Resources.GetDrawable(Resource.Color.batchSelectHighlight, this.Activity.Theme);
#pragma warning restore 0618
                e.View.FindViewById(Resource.Id.selectionCheck).Visibility = ViewStates.Visible;
                this.customAdapter.SelectedPositions.Add(e.Position);
            }
            else
            {
                e.View.Background = null;
                e.View.FindViewById(Resource.Id.mainDlLayout).Background = null;
                e.View.FindViewById(Resource.Id.selectionCheck).Visibility = ViewStates.Gone;
                this.customAdapter.SelectedPositions.Remove(e.Position);
            }
            UpdateDownloadButtonText();
        }

        private void UpdateDownloadButtonText()
        {
            if (this.customAdapter != null && this.customAdapter.SelectedPositions.Count > 0)
            {
                downloadButton.Text = SeekerApplication.GetString(Resource.String.download_selected) + " (" + this.customAdapter.SelectedPositions.Count + ")";
            }
            else
            {
                downloadButton.Text = SeekerApplication.GetString(Resource.String.download_folder);
            }
        }

        private void Download_Click(object sender, EventArgs e)
        {
            bool hasSelection = this.customAdapter != null && this.customAdapter.SelectedPositions.Count > 0;
            DownloadWithContinuation(GetFilesToDownload(hasSelection), SearchResponse.Username);
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            Action<View> browseAction = new Action<View>((v) =>
            {
                this.Dismiss();
                ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
            });
            Browse.BrowseService.RequestFilesApi(SearchResponse.Username, this.View, browseAction, null);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Dismiss();
        }

        private void DownloadWithContinuation(FullFileInfo[] filesToDownload, string username)
        {
            if (SessionService.Instance.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!SessionService.Instance.ShowMessageAndCreateReconnectTask(false, out t))
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
                    var file = SearchResponse.GetElementAtAdapterPosition(PreferencesState.HideLockedResultsInSearch, position);
                    selectedFiles.Add(file);
                }
                return BrowseUtils.GetFullFileInfos(selectedFiles.ToArray());
            }
            else
            {
                return BrowseUtils.GetFullFileInfos(SearchResponse.GetFiles(PreferencesState.HideLockedResultsInSearch));
            }
        }

        private void DownloadFiles(FullFileInfo[] files, string username, bool queuePaused)
        {
            var task = DownloadService.Instance.CreateDownloadAllTask(files, queuePaused, username);
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
                else if (dirTask.Result.Count == 0)
                {
                    Logger.Firebase("User returned an empty folder response");
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.folder_request_user_returned_empty_response), ToastLength.Short);
                    return;
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
                    SearchResponse = SearchUtil.CreateSearchResponseFromDirectory(SearchResponse, directory, PreferencesState.HideLockedResultsInSearch);
                    this.UpdateListView();
                    this.UpdateSubHeader();
                }
            });
        }

        public void GetFolderContents()
        {
            try
            {
                var file = SearchResponse.GetElementAtAdapterPosition(PreferencesState.HideLockedResultsInSearch, 0);
                string dirname = SimpleHelpers.GetDirectoryRequestFolderName(file.Filename);
                if (dirname == string.Empty)
                {
                    Logger.Firebase("The dirname is empty!!");
                    stopRefreshing();
                    return;
                }
                if (!PreferencesState.HideLockedResultsInSearch && SearchResponse.FileCount == 0 && SearchResponse.LockedFileCount > 0)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.GetFolderDoesntWorkForLockedShares), ToastLength.Short);
                    stopRefreshing();
                    return;
                }
                Logger.InfoFirebase("requesting " + dirname + " from " + SearchResponse.Username);
                Browse.BrowseService.GetFolderContentsAPI(SearchResponse.Username, dirname, file.IsDirectoryLatin1Decoded, DirectoryReceivedContAction);
            }
            catch (Exception ex)
            {
                UiHelpers.ShowReportErrorDialog(SeekerState.ActiveActivityRef, "Get Folder Contents Issue");
                Logger.FirebaseError($"{PreferencesState.HideLockedResultsInSearch} {SearchResponse.FileCount} {SearchResponse.LockedFileCount}", ex);
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
                    string startingDir = SimpleHelpers.GetDirectoryRequestFolderName(SearchResponse.GetElementAtAdapterPosition(PreferencesState.HideLockedResultsInSearch, 0).Filename);
                    Action<View> action = new Action<View>((v) =>
                    {
                        this.Dismiss();
                        ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    if (!PreferencesState.HideLockedResultsInSearch && PreferencesState.HideLockedResultsInBrowse && SearchResponse.IsLockedOnly())
                    {
                        //this is if the user has show locked in search results but hide in browse results, then we cannot go to the folder if it is locked.
                        startingDir = null;
                    }
                    Browse.BrowseService.RequestFilesApi(SearchResponse.Username, this.View, action, startingDir);
                    return true;
                case Resource.Id.moreInfo:
                    //TransferItem[] tempArry = new TransferItem[transferItems.Count]();
                    //transferItems.CopyTo(tempArry);
                    //TODOASAP - hasfreeupload slots is now a boolean, fix the string.
                    var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this.Context);
                    var diag = builder.SetMessage(this.Context.GetString(Resource.String.queue_length_) +
                        SearchResponse.QueueLength +
                        System.Environment.NewLine +
                        System.Environment.NewLine +
                        this.Context.GetString(Resource.String.upload_slots_) +
                        SearchResponse.HasFreeUploadSlot).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
                    diag.Show();
                    //System.Threading.Thread.Sleep(100); Is this required?
                    //diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(new Android.Graphics.Color(9804764)); makes the whole button invisible...
                    //if(InNightMode(this.Context))
                    //{
                    //    diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(new Android.Graphics.Color(Android.Graphics.Color.ParseColor("#bcc1f7")));
                    //}
                    //else
                    //{
                    diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(UiHelpers.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));
                    //}
                    return true;
                case Resource.Id.getUserInfo:
                    RequestedUserInfoHelper.RequestUserInfoApi(SearchResponse.Username);
                    return true;
                case Resource.Id.download_folder_as_queued:
                    {
                        var filesToDownload = GetFilesToDownload(false);
                        DownloadFiles(filesToDownload, SearchResponse.Username, true);
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
                        DownloadFiles(filesToDownload, SearchResponse.Username, true);
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
                var highlight = Owner.Resources.GetDrawable(Resource.Color.batchSelectHighlight, SeekerState.ActiveActivityRef.Theme);
                itemView.Background = highlight;
                itemView.FindViewById<View>(Resource.Id.mainDlLayout).Background = highlight;
#pragma warning restore 0618
                itemView.FindViewById<View>(Resource.Id.selectionCheck).Visibility = ViewStates.Visible;
            }
            else //views get reused, hence we need to reset the color so that when we scroll the resused views arent still highlighted.
            {
                itemView.Background = null;
                itemView.FindViewById<View>(Resource.Id.mainDlLayout).Background = null;
                itemView.FindViewById<View>(Resource.Id.selectionCheck).Visibility = ViewStates.Gone;
            }
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