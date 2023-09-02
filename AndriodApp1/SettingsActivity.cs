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
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.Provider;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Google.Android.Material.Snackbar;
using System.Threading.Tasks;
using Android.Content.PM;
//using Android.Support.V7.Widget;
//using Android.Support.V7.Widget.Helper;
using Android.Util;
using AndroidX.RecyclerView.Widget;
using System.Xml;
using System.Xml.Serialization;
using Android.Support.V4.Content;
using Android.Text;

namespace AndriodApp1
{
    [Activity(Label = "SettingsActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false)]
    public class SettingsActivity : ThemeableActivity //AppCompatActivity is needed to support chaning light / dark mode programmatically...
    {
        private const int CHANGE_WRITE_EXTERNAL = 0x909;
        private const int CHANGE_WRITE_EXTERNAL_LEGACY = 0x910;
        private const int CHANGE_WRITE_EXTERNAL_LEGACY_Settings = 0x930; //+32

        private const int UPLOAD_DIR_ADD_WRITE_EXTERNAL = 0x911;
        private const int UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case = 0x834;
        private const int UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY = 0x912;
        private const int UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY_Reselect_Case = 0x835;
        private const int UPLOAD_DIR_CHANGE_WRITE_EXTERNAL_LEGACY_Settings = 0x932;
        private const int UPLOAD_DIR_CHANGE_WRITE_EXTERNAL_LEGACY_Settings_Reselect_Case = 0x855;

        private const int SAVE_SEEKER_SETTINGS = 0x856;

        private const int READ_EXTERNAL_FOR_MEDIA_STORE = 1182021;

        private const int CHANGE_INCOMPLETE_EXTERNAL = 0x913;
        private const int CHANGE_INCOMPLETE_EXTERNAL_LEGACY = 0x914;
        private const int CHANGE_INCOMPLETE_EXTERNAL_LEGACY_Settings = 0x934;

        private const int FORCE_REQUEST_STORAGE_MANAGER = 0x434;

        public const int SCROLL_TO_SHARING_SECTION = 10;
        public const string SCROLL_TO_SHARING_SECTION_STRING = "SCROLL_TO_SHARING_SECTION";

        private List<Tuple<int,int>> positionNumberPairs = new List<Tuple<int, int>>();
        private CheckBox allowPrivateRoomInvitations;

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            //MenuInflater.Inflate(Resource.Menu.account_menu, menu); //no menu for us..
            return base.OnCreateOptionsMenu(menu);
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        protected override void OnResume()
        {
            base.OnResume();

            UPnpManager.Instance.SearchFinished += UpnpSearchFinished;
            UPnpManager.Instance.SearchStarted += UpnpSearchStarted;
            UPnpManager.Instance.DeviceSuccessfullyMapped += UpnpDeviceMapped;
            PrivilegesManager.Instance.PrivilegesChecked += PrivilegesChecked;
            SettingsActivity.UploadDirectoryChanged += DirectoryViewsChanged;

            //when you open up the directory selection with OpenDocumentTree the SettingsActivity is paused
            this.UpdateDirectoryViews();

            //however with the api<21 it is not paused and so an event is needed.
            SoulSeekState.DirectoryUpdatedEvent += DirectoryUpdated;
            SoulSeekState.SharingStatusChangedEvent += SharingStatusUpdated;
            
            // moved to OnResume from OnCreate
            // this fixes an issue where, when the settings activity is up but one 
            // goes to system settings to change per app language, it triggers 
            // ItemSelected with the old values (resetting the language preference)
            // ("onItemSelected method is also invoked when the view is being build")
            Spinner languageSpinner = FindViewById<Spinner>(Resource.Id.languageSpinner);
            languageSpinner.ItemSelected -= LanguageSpinner_ItemSelected;
            String[] languageSpinnerOptionsStrings = new String[] { SeekerApplication.GetString(Resource.String.Automatic), "English", "Português (Brazil)", "Français", "ру́сский язы́к", "Español", "украї́нська мо́ва", "Nederlands", "čeština" };
            ArrayAdapter<String> languageSpinnerOptions = new ArrayAdapter<string>(this, Resource.Layout.support_simple_spinner_dropdown_item, languageSpinnerOptionsStrings);
            languageSpinner.Adapter = languageSpinnerOptions;
            SetSpinnerPositionLangauge(languageSpinner);
            languageSpinner.ItemSelected += LanguageSpinner_ItemSelected;
            //languageSpinner.Post(new Action(() => { languageSpinner.ItemSelected += LanguageSpinner_ItemSelected; }));

        }

        public void DirectoryViewsChanged(object sender, EventArgs e)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                recyclerViewFoldersAdapter?.NotifyDataSetChanged();
            });
        }

        private void SharingStatusUpdated(object sender, EventArgs e)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                UpdateShareImageView();
            });
        }

        private void PrivilegesChecked(object sender, EventArgs e)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                SetPrivStatusView(this.FindViewById<TextView>(Resource.Id.privStatusView));
            });
        }

        private void UpnpDeviceMapped(object sender, EventArgs e)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
            Toast.MakeText(this, Resource.String.upnp_success, ToastLength.Short).Show();
            SetUpnpStatusView(this.FindViewById<ImageView>(Resource.Id.UPnPStatus));
            });
        }

        private void UpnpSearchFinished(object sender, EventArgs e)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                if (SoulSeekState.ListenerEnabled && SoulSeekState.ListenerUPnpEnabled && UPnpManager.Instance.RunningStatus == UpnpRunningStatus.Finished && UPnpManager.Instance.DiagStatus != UpnpDiagStatus.Success)
                {
                    Toast.MakeText(this, Resource.String.upnp_search_finished, ToastLength.Short).Show();
                }
                SetUpnpStatusView(this.FindViewById<ImageView>(Resource.Id.UPnPStatus));
            });
        }

        private void DirectoryUpdated(object sender, EventArgs e)
        {
            UpdateDirectoryViews();
        }

        private void UpdateDirectoryViews()
        {
            this.SetIncompleteFolderView();
            this.SetCompleteFolderView();
            this.SetSharedFolderView();
        }

        private void UpnpSearchStarted(object sender, EventArgs e)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                SetUpnpStatusView(this.FindViewById<ImageView>(Resource.Id.UPnPStatus));
            });
        }

        protected override void OnPause()
        {

            UPnpManager.Instance.SearchFinished -= UpnpSearchFinished;
            UPnpManager.Instance.SearchStarted -= UpnpSearchStarted;
            UPnpManager.Instance.DeviceSuccessfullyMapped -= UpnpDeviceMapped;
            PrivilegesManager.Instance.PrivilegesChecked -= PrivilegesChecked;
            SoulSeekState.DirectoryUpdatedEvent -= DirectoryUpdated;
            SettingsActivity.UploadDirectoryChanged -= DirectoryViewsChanged;
            SoulSeekState.SharingStatusChangedEvent -= SharingStatusUpdated;
            SettingsActivity.SaveAdditionalDirectorySettingsToSharedPreferences();
            base.OnPause();
        }

        //private string SaveDataDirectoryUri = string.Empty;

        private CheckBox createUsernameSubfoldersView;
        private CheckBox createCompleteAndIncompleteFoldersView;
        private CheckBox manuallyChooseIncompleteFolderView;
        private TextView currentCompleteFolderView;
        private TextView currentIncompleteFolderView;
        private TextView currentSharedFolderView;

        private ViewGroup incompleteFolderViewLayout;
        private Button changeIncompleteDirectory;

        private ViewGroup sharingSubLayout1;
        private ViewGroup sharingSubLayout2;

        private ViewGroup listeningSubLayout2;
        private ViewGroup listeningSubLayout3;

        private ViewGroup limitDlSpeedSubLayout;
        Button changeDlSpeed;
        TextView dlSpeedTextView;
        Spinner dlLimitPerTransfer;


        private ViewGroup limitUlSpeedSubLayout;
        Button changeUlSpeed;
        TextView ulSpeedTextView;
        Spinner ulLimitPerTransfer;

        private ViewGroup concurrentDlSublayout;
        private TextView concurrentDlLabel;
        private Button concurrentDlButton;
        private CheckBox concurrentDlCheckbox;


        Button addFolderButton;
        Button clearAllFoldersButton;

        TextView noSharedFoldersView;
        RecyclerView recyclerViewFolders;
        LinearLayoutManager recyclerViewFoldersLayoutManager;
        ReyclerUploadsAdapter recyclerViewFoldersAdapter;

        Button browseSelfButton;
        Button rescanSharesButton;

        Button checkStatus;
        Button changePort;

        CheckBox useUPnPCheckBox;
        CheckBox showSmartFilters;


        public class ReyclerUploadsAdapter : RecyclerView.Adapter
        {
            public List<UploadDirectoryInfo> localDataSet;
            public override int ItemCount => localDataSet.Count;
            private int position = -1;
            public SettingsActivity settingsActivity;
            public ReyclerUploadsAdapter(SettingsActivity activity, List<UploadDirectoryInfo> ti)
            {
                this.settingsActivity = activity;
                localDataSet = ti;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                (holder as RecyclerViewFolderHolder).folderView.setItem(localDataSet[position]);
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
            {
                RecyclerViewFolderView view = RecyclerViewFolderView.inflate(parent);
                view.setupChildren();
                view.SettingsActivity = this.settingsActivity;
                (view as View).Click += view.FolderClick;
                (view as View).LongClick += view.FolderLongClick;
                return new RecyclerViewFolderHolder(view as View);
            }

            public void setPosition(int position)
            {
                this.position = position;
            }
        }
        private static UploadDirectoryInfo ContextMenuItem = null;
        public class RecyclerViewFolderHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
        {
            public RecyclerViewFolderView folderView;


            public RecyclerViewFolderHolder(View view) : base(view)
            {
                folderView = (RecyclerViewFolderView)view;
                folderView.ViewHolder = this;
                folderView.SetOnCreateContextMenuListener(this);
            }

            public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
            {
                RecyclerViewFolderView folderRowView = v as RecyclerViewFolderView;
                ContextMenuItem = folderRowView.BoundItem;
                if (ContextMenuItem.HasError())
                {
                    menu.Add(0, 1, 0, Resource.String.ViewErrorOptions);
                }
                else
                {
                    menu.Add(0, 1, 0, Resource.String.ViewFolderOptions);
                }
                menu.Add(0, 2, 1, Resource.String.Remove);
            }
        }
        public class RecyclerViewFolderView : RelativeLayout
        {
            public UploadDirectoryInfo BoundItem;

            public RecyclerViewFolderHolder ViewHolder;
            public SettingsActivity SettingsActivity = null;
            public TextView viewFolderName;
            public ImageView viewFolderStatus;

            public RecyclerViewFolderView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.upload_folder_row, this, true);
                setupChildren();
            }
            public RecyclerViewFolderView(Context context, IAttributeSet attrs) : base(context, attrs)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.upload_folder_row, this, true);
                setupChildren();
            }

            public void FolderLongClick(object sender, View.LongClickEventArgs e)
            {

                (ViewHolder.BindingAdapter as ReyclerUploadsAdapter).setPosition((sender as RecyclerViewFolderView).ViewHolder.AdapterPosition);
                (sender as View).ShowContextMenu();
            }

            public void FolderClick(object sender, EventArgs e)
            {

                (ViewHolder.BindingAdapter as ReyclerUploadsAdapter).setPosition((sender as RecyclerViewFolderView).ViewHolder.AdapterPosition);
                (ViewHolder.BindingAdapter as ReyclerUploadsAdapter).settingsActivity.ShowDialogForUploadDir((sender as RecyclerViewFolderView).ViewHolder.folderView.BoundItem);
            }

            public static RecyclerViewFolderView inflate(ViewGroup parent)
            {
                RecyclerViewFolderView itemView = (RecyclerViewFolderView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.upload_folder_row_dummy, parent, false);
                return itemView;
            }

            public void setupChildren()
            {
                viewFolderName = FindViewById<TextView>(Resource.Id.uploadFolderName);
                viewFolderStatus = FindViewById<ImageView>(Resource.Id.uploadFolderStatus);
               
            }

            public void setItem(UploadDirectoryInfo item)
            {
                this.Clickable = SoulSeekState.SharingOn;
                this.LongClickable = SoulSeekState.SharingOn;

                BoundItem = item;
                if(string.IsNullOrEmpty(item.DisplayNameOverride))
                {
                    viewFolderName.Text = item.GetLastPathSegment();
                }
                else
                {
                    viewFolderName.Text = item.GetLastPathSegment() + $" ({item.DisplayNameOverride})";
                }

                if(item.HasError())
                {
                    viewFolderStatus.Visibility = ViewStates.Visible;
                    viewFolderStatus.SetImageResource(Resource.Drawable.alert_circle_outline);
                }
                else if(item.IsHidden)
                {
                    viewFolderStatus.Visibility = ViewStates.Visible;
                    viewFolderStatus.SetImageResource(Resource.Drawable.hidden_lock_question);
                }
                else if(item.IsLocked)
                {
                    viewFolderStatus.Visibility = ViewStates.Visible;
                    viewFolderStatus.SetImageResource(Resource.Drawable.lock_icon);
                }
                else
                {
                    viewFolderStatus.Visibility = ViewStates.Gone;
                }
            }
        }




        private ScrollView mainScrollView;
        private View sharingLayoutParent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            MainActivity.LogDebug("Settings Created");


            base.OnCreate(savedInstanceState);



            SoulSeekState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.settings_layout);

            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.setting_toolbar);
            myToolbar.InflateMenu(Resource.Menu.search_menu);
            myToolbar.Title = this.GetString(Resource.String.settings);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);


            Button chbox = this.FindViewById<Button>(Resource.Id.addUploadDirectory);

            var progBar = this.FindViewById<ProgressBar>(Resource.Id.progressBarSharedStatus);
            progBar.IndeterminateDrawable.SetColorFilter(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainTextColor),Android.Graphics.PorterDuff.Mode.SrcIn);
            progBar.Click += ImageView_Click;
            Intent intent = Intent; //intent that started this activity
            //this.SaveDataDirectoryUri = intent.GetStringExtra("SaveDataDirectoryUri");
            Button changeDirSettings = FindViewById<Button>(Resource.Id.changeDirSettings);
            changeDirSettings.Click += ChangeDownloadDirectory;

            CheckBox autoClearComplete = FindViewById<CheckBox>(Resource.Id.autoClearComplete);
            autoClearComplete.Checked = SoulSeekState.AutoClearCompleteDownloads;
            autoClearComplete.CheckedChange += AutoClearComplete_CheckedChange;

            CheckBox autoClearCompleteUploads = FindViewById<CheckBox>(Resource.Id.autoClearCompleteUploads);
            autoClearCompleteUploads.Checked = SoulSeekState.AutoClearCompleteUploads;
            autoClearCompleteUploads.CheckedChange += AutoClearCompleteUploads_CheckedChange;

            CheckBox freeUploadSlotsOnly = FindViewById<CheckBox>(Resource.Id.freeUploadSlots);
            freeUploadSlotsOnly.Checked = SoulSeekState.FreeUploadSlotsOnly;
            freeUploadSlotsOnly.CheckedChange += FreeUploadSlotsOnly_CheckedChange;

            CheckBox showLockedSearch = FindViewById<CheckBox>(Resource.Id.showLockedInSearch);
            showLockedSearch.Checked = !SoulSeekState.HideLockedResultsInSearch;
            showLockedSearch.CheckedChange += ShowLockedSearch_CheckedChange;

            CheckBox showLockedBrowse = FindViewById<CheckBox>(Resource.Id.showLockedInBrowseResponse);
            showLockedBrowse.Checked = !SoulSeekState.HideLockedResultsInBrowse;
            showLockedBrowse.CheckedChange += ShowLockedBrowse_CheckedChange;

            allowPrivateRoomInvitations = FindViewById<CheckBox>(Resource.Id.allowPrivateRoomInvitations);
            allowPrivateRoomInvitations.Checked = SoulSeekState.AllowPrivateRoomInvitations;
            allowPrivateRoomInvitations.CheckedChange += AllowPrivateRoomInvitations_CheckedChange;

            CheckBox autoSetAwayStatusOnInactivity = FindViewById<CheckBox>(Resource.Id.autoSetAwayStatus);
            autoSetAwayStatusOnInactivity.Checked = SoulSeekState.AutoAwayOnInactivity;
            autoSetAwayStatusOnInactivity.CheckedChange += AutoSetAwayStatusOnInactivity_CheckedChange;

            CheckBox showDownloadNotification = FindViewById<CheckBox>(Resource.Id.showToastNotificationOnDownload);
            showDownloadNotification.Checked = !SoulSeekState.DisableDownloadToastNotification;
            showDownloadNotification.CheckedChange += ShowDownloadNotification_CheckedChange;

            CheckBox showFolderDownloadNotification = FindViewById<CheckBox>(Resource.Id.showNotificationOnFolderDownload);
            showFolderDownloadNotification.Checked = SoulSeekState.NotifyOnFolderCompleted;
            showFolderDownloadNotification.CheckedChange += ShowFolderDownloadNotification_CheckedChange;

            CheckBox memoryFileDownloadSwitchCheckBox = FindViewById<CheckBox>(Resource.Id.memoryFileDownloadSwitchCheckBox);
            memoryFileDownloadSwitchCheckBox.Checked = !SoulSeekState.MemoryBackedDownload;
            memoryFileDownloadSwitchCheckBox.CheckedChange += MemoryFileDownloadSwitchCheckBox_CheckedChange;


            CheckBox autoRetryBackOnline = FindViewById<CheckBox>(Resource.Id.autoRetryBackOnline);
            autoRetryBackOnline.Checked = SoulSeekState.AutoRetryBackOnline;
            autoRetryBackOnline.CheckedChange += AutoRetryBackOnline_CheckedChange; 

            ImageView memoryFileDownloadSwitchIcon = FindViewById<ImageView>(Resource.Id.memoryFileDownloadSwitchIcon);
            memoryFileDownloadSwitchIcon.Click += MemoryFileDownloadSwitchIcon_Click;

            ImageView moreInfoDiagnostics = FindViewById<ImageView>(Resource.Id.moreInfoDiagnostics);
            moreInfoDiagnostics.Click += MoreInfoDiagnostics_Click;

            //Button closeButton = FindViewById<Button>(Resource.Id.closeSettings);
            //closeButton.Click += CloseButton_Click;

            Button restoreDefaultsButton = FindViewById<Button>(Resource.Id.restoreDefaults);
            restoreDefaultsButton.Click += RestoreDefaults_Click;

            Button clearHistory = FindViewById<Button>(Resource.Id.clearHistory);
            clearHistory.Click += ClearHistory_Click;

            Button exportClientData = FindViewById<Button>(Resource.Id.exportDataButton);
            exportClientData.Click += ExportClientData_Click;

            ImageView moreInfoExport = FindViewById<ImageView>(Resource.Id.moreInfoExport);
            moreInfoExport.Click += MoreInfoExport_Click;

            CheckBox rememberSearchHistory = FindViewById<CheckBox>(Resource.Id.searchHistoryRemember);
            rememberSearchHistory.Checked = SoulSeekState.RememberSearchHistory;
            rememberSearchHistory.CheckedChange += RememberSearchHistory_CheckedChange;

            Button clearRecentUserHistory = FindViewById<Button>(Resource.Id.clearRecentUsers);
            clearRecentUserHistory.Click += ClearRecentUserHistory_Click;

            CheckBox rememberRecentUsers = FindViewById<CheckBox>(Resource.Id.rememberRecentUsers);
            rememberRecentUsers.Checked = SoulSeekState.ShowRecentUsers;
            rememberRecentUsers.CheckedChange += RememberRecentUsers_CheckedChange;


            CheckBox enableDiagnostics = FindViewById<CheckBox>(Resource.Id.enableDiagnostics);
            enableDiagnostics.Checked = SeekerApplication.LOG_DIAGNOSTICS;
            enableDiagnostics.CheckedChange += EnableDiagnostics_CheckedChange;


            Spinner searchNumSpinner = FindViewById<Spinner>(Resource.Id.searchNumberSpinner);
            positionNumberPairs.Add(new Tuple<int, int>(0,5));
            positionNumberPairs.Add(new Tuple<int, int>(1,10));
            positionNumberPairs.Add(new Tuple<int, int>(2,15));
            positionNumberPairs.Add(new Tuple<int, int>(3,30));
            positionNumberPairs.Add(new Tuple<int, int>(4,50));
            positionNumberPairs.Add(new Tuple<int, int>(5,100));
            positionNumberPairs.Add(new Tuple<int, int>(6,250));
            positionNumberPairs.Add(new Tuple<int, int>(7,1000));
            String[] options = new String[]{ positionNumberPairs[0].Item2.ToString(),
                                             positionNumberPairs[1].Item2.ToString(),
                                             positionNumberPairs[2].Item2.ToString(),
                                             positionNumberPairs[3].Item2.ToString(),
                                             positionNumberPairs[4].Item2.ToString(),
                                             positionNumberPairs[5].Item2.ToString(),
                                             positionNumberPairs[6].Item2.ToString(),
                                             positionNumberPairs[7].Item2.ToString(),
                };
            ArrayAdapter<String> searchNumOptions = new ArrayAdapter<string>(this,Resource.Layout.support_simple_spinner_dropdown_item, options);
            searchNumSpinner.Adapter = searchNumOptions;
            SetSpinnerPosition(searchNumSpinner);
            searchNumSpinner.ItemSelected += SearchNumSpinner_ItemSelected;

            Spinner dayNightMode = FindViewById<Spinner>(Resource.Id.nightModeSpinner);
            dayNightMode.ItemSelected -= DayNightMode_ItemSelected;
            String[] dayNightOptionsStrings = new String[]{ this.GetString(Resource.String.follow_system), this.GetString(Resource.String.always_light), this.GetString(Resource.String.always_dark) };
            ArrayAdapter<String> dayNightOptions = new ArrayAdapter<string>(this, Resource.Layout.support_simple_spinner_dropdown_item, dayNightOptionsStrings);
            dayNightMode.Adapter = dayNightOptions;
            SetSpinnerPositionDayNight(dayNightMode);
            dayNightMode.ItemSelected += DayNightMode_ItemSelected;


            Spinner dayVarientSpinner = FindViewById<Spinner>(Resource.Id.dayVarientSpinner);
            dayVarientSpinner.ItemSelected -= DayVarient_ItemSelected;
            String[] dayVarientSpinnerOptionsStrings = new String[] { ThemeHelper.ClassicPurple, ThemeHelper.Red, ThemeHelper.Blue };
            ArrayAdapter<String> dayVarientSpinnerOptions = new ArrayAdapter<string>(this, Resource.Layout.support_simple_spinner_dropdown_item, dayVarientSpinnerOptionsStrings);
            dayVarientSpinner.Adapter = dayVarientSpinnerOptions;
            SetSpinnerPositionDayVarient(dayVarientSpinner);
            dayVarientSpinner.ItemSelected += DayVarient_ItemSelected;


            Spinner nightVarientSpinner = FindViewById<Spinner>(Resource.Id.nightVarientSpinner);
            nightVarientSpinner.ItemSelected -= NightVarient_ItemSelected;
            String[] nightVarientSpinnerOptionsStrings = new String[] { ThemeHelper.ClassicPurple, ThemeHelper.Grey, ThemeHelper.Blue, ThemeHelper.AmoledClassicPurple, ThemeHelper.AmoledGrey };
            ArrayAdapter<String> nightVarientSpinnerOptions = new ArrayAdapter<string>(this, Resource.Layout.support_simple_spinner_dropdown_item, nightVarientSpinnerOptionsStrings);
            nightVarientSpinner.Adapter = nightVarientSpinnerOptions;
            SetSpinnerPositionNightVarient(nightVarientSpinner);
            nightVarientSpinner.ItemSelected += NightVarient_ItemSelected;





            ImageView imageView = this.FindViewById<ImageView>(Resource.Id.sharedStatus);
            imageView.Click += ImageView_Click;
            UpdateShareImageView();

            addFolderButton = FindViewById<Button>(Resource.Id.addUploadDirectory);
            addFolderButton.Click += AddUploadDirectory;
            clearAllFoldersButton = FindViewById<Button>(Resource.Id.clearAllDirectories);
            clearAllFoldersButton.Click += ClearAllFoldersButton_Click;

            noSharedFoldersView = FindViewById<TextView>(Resource.Id.noSharedFolders);
            recyclerViewFolders = FindViewById<RecyclerView>(Resource.Id.uploadFoldersRecyclerView);

            recyclerViewFoldersAdapter = new ReyclerUploadsAdapter(this, UploadDirectoryManager.UploadDirectories);

            var llm = new LinearLayoutManager(this);
            DividerItemDecoration dividerItemDecoration = new DividerItemDecoration(recyclerViewFolders.Context,
                llm.Orientation);
            recyclerViewFolders.AddItemDecoration(dividerItemDecoration);

            recyclerViewFolders.SetLayoutManager(llm);
            recyclerViewFolders.SetAdapter(recyclerViewFoldersAdapter);


            CheckBox shareCheckBox = FindViewById<CheckBox>(Resource.Id.enableSharing);
            shareCheckBox.Checked = SoulSeekState.SharingOn;
            shareCheckBox.CheckedChange += ShareCheckBox_CheckedChange;

            CheckBox unmeteredConnectionsOnlyCheckBox = FindViewById<CheckBox>(Resource.Id.shareOnlyOnUnmetered);
            unmeteredConnectionsOnlyCheckBox.Checked = !SoulSeekState.AllowUploadsOnMetered;
            unmeteredConnectionsOnlyCheckBox.CheckedChange += UnmeteredConnectionsOnlyCheckBox_CheckedChange;

            ImageView moreInfoButton = FindViewById<ImageView>(Resource.Id.moreInfoButton);
            moreInfoButton.Click += MoreInfoButton_Click;

            ImageView moreInfoButtonClearIncomplete = FindViewById<ImageView>(Resource.Id.moreInfoButtonClearIncomplete);
            moreInfoButtonClearIncomplete.Click += MoreInfoButtonClearIncomplete_Click;

            ImageView moreInfoConcurrent = FindViewById<ImageView>(Resource.Id.moreInfoConcurrent);
            moreInfoConcurrent.Click += MoreInfoConcurrent_Click;

            browseSelfButton = FindViewById<Button>(Resource.Id.browseSelfButton);
            browseSelfButton.Click += BrowseSelfButton_Click;
            browseSelfButton.LongClick += BrowseSelfButton_LongClick;

            rescanSharesButton = FindViewById<Button>(Resource.Id.rescanShares);
            rescanSharesButton.Click += RescanSharesButton_Click;

            ImageView startupServiceMoreInfo = FindViewById<ImageView>(Resource.Id.helpServiceOnStartup);
            startupServiceMoreInfo.Click += StartupServiceMoreInfo_Click;

            CheckBox startServiceOnStartupCheckBox = FindViewById<CheckBox>(Resource.Id.startServiceOnStartupCheckBox);
            startServiceOnStartupCheckBox.Checked = SoulSeekState.StartServiceOnStartup;
            startServiceOnStartupCheckBox.CheckedChange += StartServiceOnStartupCheckBox_CheckedChange;

            Button startupServiceButton= FindViewById<Button>(Resource.Id.startServiceOnStartupButton);
            startupServiceButton.Click += StartupServiceButton_Click;
            SetButtonText(startupServiceButton);

            CheckBox enableListening = FindViewById<CheckBox>(Resource.Id.enableListening);
            enableListening.Checked = SoulSeekState.ListenerEnabled;
            enableListening.CheckedChange += EnableListening_CheckedChange;

            CheckBox enableDlSpeedLimits = FindViewById<CheckBox>(Resource.Id.enable_dl_speed_limits);
            enableDlSpeedLimits.Checked = SoulSeekState.SpeedLimitDownloadOn;
            enableDlSpeedLimits.CheckedChange += EnableDlSpeedLimits_CheckedChange;

            CheckBox enableUlSpeedLimits = FindViewById<CheckBox>(Resource.Id.enable_ul_speed_limits);
            enableUlSpeedLimits.Checked = SoulSeekState.SpeedLimitUploadOn;
            enableUlSpeedLimits.CheckedChange += EnableUlSpeedLimits_CheckedChange;


            limitDlSpeedSubLayout = FindViewById<ViewGroup>(Resource.Id.dlSpeedSubLayout);
            dlSpeedTextView = FindViewById<TextView>(Resource.Id.downloadSpeed);
            changeDlSpeed = FindViewById<Button>(Resource.Id.changeDlSpeed);
            changeDlSpeed.Click += ChangeDlSpeed_Click;
            dlLimitPerTransfer = FindViewById<Spinner>(Resource.Id.dlPerTransfer);
            SetSpeedTextView(dlSpeedTextView, false);

            limitUlSpeedSubLayout = FindViewById<ViewGroup>(Resource.Id.ulSpeedSubLayout);
            ulSpeedTextView = FindViewById<TextView>(Resource.Id.uploadSpeed);
            changeUlSpeed = FindViewById<Button>(Resource.Id.changeUlSpeed);
            changeUlSpeed.Click += ChangeUlSpeed_Click;
            ulLimitPerTransfer = FindViewById<Spinner>(Resource.Id.ulPerTransfer);
            SetSpeedTextView(ulSpeedTextView, true);

            UpdateSpeedLimitsState();

            concurrentDlSublayout = FindViewById<ViewGroup>(Resource.Id.limitConcurrentDownloadsSublayout2);
            concurrentDlLabel = FindViewById<TextView>(Resource.Id.concurrentDownloadsLabel);
            concurrentDlCheckbox = FindViewById<CheckBox>(Resource.Id.limitConcurrentDownloadsCheckBox);
            concurrentDlCheckbox.Checked = Soulseek.SimultaneousDownloadsGatekeeper.RestrictConcurrentUsers;
            concurrentDlCheckbox.CheckedChange += ConcurrentDlCheckbox_CheckedChange;

            concurrentDlButton = FindViewById<Button>(Resource.Id.changeConcurrentDownloads);
            concurrentDlButton.Click += ConcurrentDlBottom_Click;
            concurrentDlLabel.Text = SeekerApplication.GetString(Resource.String.MaxConcurrentIs) + " " + Soulseek.SimultaneousDownloadsGatekeeper.MaxUsersConcurrent;

            

            UpdateConcurrentDownloadLimitsState();

            String[] dlOptions = new String[]{ SeekerApplication.GetString(Resource.String.PerTransfer), SeekerApplication.GetString(Resource.String.Global) };
            ArrayAdapter<String> dlOptionsStrings = new ArrayAdapter<string>(this, Resource.Layout.support_simple_spinner_dropdown_item, dlOptions);
            dlLimitPerTransfer.Adapter = dlOptionsStrings;
            SetSpinnerPositionSpeed(dlLimitPerTransfer, false);
            dlLimitPerTransfer.ItemSelected += DlLimitPerTransfer_ItemSelected;

            ulLimitPerTransfer.Adapter = dlOptionsStrings;
            SetSpinnerPositionSpeed(ulLimitPerTransfer, true);
            ulLimitPerTransfer.ItemSelected += UlLimitPerTransfer_ItemSelected;

            ImageView listeningMoreInfo  = FindViewById<ImageView>(Resource.Id.listeningHelp);
            listeningMoreInfo.Click += ListeningMoreInfo_Click;

            TextView portView = FindViewById<TextView>(Resource.Id.portView);
            SetPortViewText(portView);

            changePort = FindViewById<Button>(Resource.Id.changePort);
            changePort.Click += ChangePort_Click;

            checkStatus = FindViewById<Button>(Resource.Id.checkStatus);
            checkStatus.Click += CheckStatus_Click;

            Button getPriv = FindViewById<Button>(Resource.Id.getPriv);
            getPriv.Click += GetPriv_Click;

            Button checkPriv = FindViewById<Button>(Resource.Id.checkPriv);
            checkPriv.Click += CheckPriv_Click;

            TextView privStatus = FindViewById<TextView>(Resource.Id.privStatusView);
            SetPrivStatusView(privStatus);

            ImageView privHelp = FindViewById<ImageView>(Resource.Id.privHelp);
            privHelp.Click += PrivHelp_Click;

            Button forceFilesystemPermission = FindViewById<Button>(Resource.Id.forceFilesystemPermission);
            forceFilesystemPermission.Click += ForceFilesystemPermission_Click;

#if !IzzySoft

            forceFilesystemPermission.Enabled = false;
            forceFilesystemPermission.Alpha = 0.5f;
            forceFilesystemPermission.Clickable = false;

#endif

            ImageView moreInfoForceFilesystem = FindViewById<ImageView>(Resource.Id.moreInfoButtonForceFilesystemPermission);
            moreInfoForceFilesystem.Click += MoreInfoForceFilesystem_Click;

            Button editUserInfo = FindViewById<Button>(Resource.Id.editUserInfoButton);
            editUserInfo.Click += EditUserInfo_Click;

            useUPnPCheckBox = FindViewById<CheckBox>(Resource.Id.useUPnPCheckBox);
            useUPnPCheckBox.Checked = SoulSeekState.ListenerUPnpEnabled;
            useUPnPCheckBox.CheckedChange += UseUPnPCheckBox_CheckedChange;

            ImageView UpnpStatusView = FindViewById<ImageView>(Resource.Id.UPnPStatus);
            SetUpnpStatusView(UpnpStatusView);
            UpnpStatusView.Click += ImageView_Click;

            sharingSubLayout1 = FindViewById<ViewGroup>(Resource.Id.dlChangeSharedDirectoryLayout);
            sharingSubLayout2 = FindViewById<ViewGroup>(Resource.Id.sharingSubLayout2);
            UpdateSharingViewState();

            listeningSubLayout2 = FindViewById<ViewGroup>(Resource.Id.listeningRow2);
            listeningSubLayout3 = FindViewById<ViewGroup>(Resource.Id.listeningRow3);
            UpdateListeningViewState();

            Button importData = this.FindViewById<Button>(Resource.Id.importDataButton);
            importData.Click += ImportData_Click;

            /*
            **NOTE**
            * 
            * Regarding Directory Options (Incomplete Folder Options, Complete Folder Options):
            * Incomplete Folder internal structure is always the same (folder is username concat file foldername),
            *   it is just the placement of it that differs
            * The automatic placement is "Soulseek Incomplete" in the same directory chosen for downloads (if "Create Folders for Downloads and Incomplete" is on.
            * Otherwise the placement is in AppData Local - what Android calls "Internal Storage"
            * 
            * The incomplete folder choices are used when the stream is created and saved in IncompleteUri.  Therefore, changing this on the fly is okay, it just wont 
            *   take effect until one starts a new download.
            * The complete folder choices are used when the file is actually saved / moved.  So changing this on the fly is okay, the transfer, once finished, will just go into its new place.
            * 
            * When one turns "Manual Selection for Incomplete" off, it reverts back to Automatic.  The user will have to reselect their folder if they choose to turn it back on.
            * 
            * To clear Incomplete, there cannot be any pending transfers.  Paused transfers are okay, they will just start from the top...
            * 
            * If "Use Manual Incomplete Folder" is checked, but no Manual Incomplete Folder is chosen, then it is as if it is not checked.
            * Also its fine if its null, or no longer writable, etc.  It will just get set back to default.  User will have to re-set it on their own.
            * 
            **NOTE**
            */

            createUsernameSubfoldersView = this.FindViewById<CheckBox>(Resource.Id.createUsernameSubfolders);
            createUsernameSubfoldersView.Checked = SoulSeekState.CreateUsernameSubfolders;
            createUsernameSubfoldersView.CheckedChange += CreateUsernameSubfoldersView_CheckedChange;
            createCompleteAndIncompleteFoldersView = this.FindViewById<CheckBox>(Resource.Id.createCompleteAndIncompleteDirectories);
            createCompleteAndIncompleteFoldersView.Checked = SoulSeekState.CreateCompleteAndIncompleteFolders;
            createCompleteAndIncompleteFoldersView.CheckedChange += CreateCompleteAndIncompleteFoldersView_CheckedChange;
            manuallyChooseIncompleteFolderView = this.FindViewById<CheckBox>(Resource.Id.manuallySetIncomplete);
            manuallyChooseIncompleteFolderView.Checked = SoulSeekState.OverrideDefaultIncompleteLocations;
            manuallyChooseIncompleteFolderView.CheckedChange += ManuallyChooseIncompleteFolderView_CheckedChange;
            currentCompleteFolderView = this.FindViewById<TextView>(Resource.Id.completeFolderPath);
            currentIncompleteFolderView = this.FindViewById<TextView>(Resource.Id.incompleteFolderPath);

            changeIncompleteDirectory = this.FindViewById<Button>(Resource.Id.changeIncompleteDirSettings);
            changeIncompleteDirectory.Click += ChangeIncompleteDirectory;
            incompleteFolderViewLayout = this.FindViewById<ViewGroup>(Resource.Id.incompleteDirectoryLayout);

            Button cleanUpIncompleteDirectory = this.FindViewById<Button>(Resource.Id.clearIncompleteFolder);
            cleanUpIncompleteDirectory.Click += CleanUpIncompleteDirectory_Click;

            SetIncompleteDirectoryState();
            SetCompleteFolderView();
            SetIncompleteFolderView();
            SetSharedFolderView();


            showSmartFilters = this.FindViewById<CheckBox>(Resource.Id.smartFilterEnable);
            showSmartFilters.Checked = SoulSeekState.ShowSmartFilters;
            showSmartFilters.CheckedChange += ShowSmartFilters_CheckedChange;

            Button configSmartFilters = FindViewById<Button>(Resource.Id.configureSmartFilters);
            configSmartFilters.Click += ConfigSmartFilters_Click;

            mainScrollView = FindViewById<ScrollView>(Resource.Id.mainScrollView);
            sharingLayoutParent = FindViewById<ViewGroup>(Resource.Id.sharingLayoutParent);
            if (Intent != null && Intent.GetIntExtra(SettingsActivity.SCROLL_TO_SHARING_SECTION_STRING, -1) != -1)
            {
                mainScrollView.Post(new Action(() => { mainScrollView.SmoothScrollTo(0, sharingLayoutParent.Top - 14); }));
            }

            UpdateLayoutParametersForScreenSize();
        }



        private void MoreInfoExport_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.export_more_info).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private const string DefaultDocumentsUri = "content://com.android.externalstorage.documents/tree/primary%3ADocuments";

        private void ExportClientData_Click(object sender, EventArgs e)
        {
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionCreateDocument);
            intent.SetType("application/xml");
            intent.PutExtra(Android.Content.Intent.ExtraTitle, "seeker_data.xml");
            intent.AddCategory(Android.Content.Intent.CategoryOpenable);
            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
            {
                intent.PutExtra(Android.Provider.DocumentsContract.ExtraInitialUri, Android.Net.Uri.Parse(DefaultDocumentsUri));
            }
            this.StartActivityForResult(intent, SAVE_SEEKER_SETTINGS);
        }

        private void LanguageSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            string selection = GetLanguageStringFromPosition(e.Position);
            if (SeekerApplication.GetLegacyLanguageString() == selection)
            {
                return;
            }

            SoulSeekState.Language = selection;
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = this.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                editor.PutString(SoulSeekState.M_Lanuage, SoulSeekState.Language);
                editor.Commit();
            }

            (SeekerApplication.ApplicationContext as SeekerApplication).SetLanguage(SoulSeekState.Language);
        }

        private void UpdateLayoutParametersForScreenSize()
        {
            try
            {
                if(this.Resources.DisplayMetrics.WidthPixels < 400) //320 is MDPI
                {
                    (concurrentDlSublayout as LinearLayout).Orientation = Orientation.Vertical;
                    (concurrentDlSublayout as LinearLayout).SetGravity(GravityFlags.Center);
                    ((LinearLayout.LayoutParams)concurrentDlButton.LayoutParameters).Gravity = GravityFlags.Center;
                }
                else
                {
                    (concurrentDlSublayout as LinearLayout).Orientation = Orientation.Horizontal;
                    (concurrentDlSublayout as LinearLayout).SetGravity(GravityFlags.CenterVertical);
                    ((LinearLayout.LayoutParams)concurrentDlButton.LayoutParameters).Gravity = GravityFlags.CenterVertical;
                }
            }
            catch(Exception ex)
            {
                MainActivity.LogFirebase("Unable to tweak layout " + ex);
            }
        }

        private void UnmeteredConnectionsOnlyCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            bool oldState = MainActivity.MeetsCurrentSharingConditions();
            SoulSeekState.AllowUploadsOnMetered = !e.IsChecked;
            bool newState = MainActivity.MeetsCurrentSharingConditions();
            if(oldState != newState)
            {
                MainActivity.SetUnsetSharingBasedOnConditions(true);
                UpdateShareImageView();
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.ActiveActivityRef.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                editor.PutBoolean(SoulSeekState.M_AllowUploadsOnMetered, SoulSeekState.AllowUploadsOnMetered);
                editor.Commit();
            }
        }

        private void ClearAllFoldersButton_Click(object sender, EventArgs e)
        {
            if(UploadDirectoryManager.UploadDirectories.Count > 1) //ask before doing.
            {
                var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
                var diag = builder.SetMessage(String.Format(SeekerApplication.GetString(Resource.String.AreYouSureClearAllDirectories), UploadDirectoryManager.UploadDirectories.Count))
                    .SetPositiveButton(Resource.String.yes, (object sender, DialogClickEventArgs e) => {
                        this.ClearAllFolders();
                        this.OnCloseClick(sender, e);
                    })
                    .SetNegativeButton(Resource.String.No, OnCloseClick)
                    .Create();
                diag.Show();
            }
            else
            {
                this.ClearAllFolders();
            }
        }

        private void ClearAllFolders()
        {
            UploadDirectoryManager.UploadDirectories.Clear();
            UploadDirectoryManager.SaveToSharedPreferences(SoulSeekState.SharedPreferences);
            this.recyclerViewFoldersAdapter.NotifyDataSetChanged();
            SetSharedFolderView();
            SoulSeekState.SharedFileCache = SlskHelp.SharedFileCache.GetEmptySharedFileCache();
            MainActivity.SharedFileCache_Refreshed(null, (0, 0));

        }

        private void ShowFolderDownloadNotification_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            bool changed = SoulSeekState.NotifyOnFolderCompleted != e.IsChecked;
            SoulSeekState.NotifyOnFolderCompleted = e.IsChecked;
            if (changed)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutBoolean(SoulSeekState.M_NotifyFolderComplete, SoulSeekState.NotifyOnFolderCompleted);
                    bool success = editor.Commit();
                }
            }
        }

        private void AutoRetryBackOnline_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            bool changed = SoulSeekState.AutoRetryBackOnline != e.IsChecked;
            SoulSeekState.AutoRetryBackOnline = e.IsChecked;
            if (changed)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutBoolean(SoulSeekState.M_AutoRetryBackOnline, SoulSeekState.AutoRetryBackOnline);
                    bool success = editor.Commit();
                }
            }
        }

        private void AutoSetAwayStatusOnInactivity_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            bool changed = SoulSeekState.AutoAwayOnInactivity != e.IsChecked;
            SoulSeekState.AutoAwayOnInactivity = e.IsChecked;
            if(changed)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutBoolean(SoulSeekState.M_AutoSetAwayOnInactivity, SoulSeekState.AutoAwayOnInactivity);
                    bool success = editor.Commit();
                }
            }
        }

        private void MoreInfoDiagnostics_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.diagnostics_more_info).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private void MoreInfoConcurrent_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.concurrent_dialog).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private void MoreInfoButtonClearIncomplete_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.clear_incomplete_dialog).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private void CleanUpIncompleteDirectory_Click(object sender, EventArgs e)
        {
            ClearIncompleteFolder();
        }

        private void ConcurrentDlCheckbox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked == Soulseek.SimultaneousDownloadsGatekeeper.RestrictConcurrentUsers)
            {
                return;
            }
            Soulseek.SimultaneousDownloadsGatekeeper.RestrictConcurrentUsers = e.IsChecked;
            this.UpdateConcurrentDownloadLimitsState();
            SaveMaxConcurrentDownloadsSettings();
        }

        private void ConcurrentDlBottom_Click(object sender, EventArgs e)
        {
            ShowChangeDialog(ChangeDialogType.ConcurrentDL);
        }

        private void UlLimitPerTransfer_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (e.Position == 0)
            {
                SoulSeekState.SpeedLimitUploadIsPerTransfer = true;
            }
            else
            {
                SoulSeekState.SpeedLimitUploadIsPerTransfer = false;
            }
        }

        private void ChangeUlSpeed_Click(object sender, EventArgs e)
        {
            ShowChangeDialog(ChangeDialogType.ChangeUL);
        }

        private void DlLimitPerTransfer_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if(e.Position == 0)
            {
                SoulSeekState.SpeedLimitDownloadIsPerTransfer = true;
            }
            else
            {
                SoulSeekState.SpeedLimitDownloadIsPerTransfer = false;
            }
        }

        private void ChangeDlSpeed_Click(object sender, EventArgs e)
        {
            ShowChangeDialog(ChangeDialogType.ChangeDL);
        }

        private void EnableDlSpeedLimits_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked == SoulSeekState.SpeedLimitDownloadOn)
            {
                return;
            }
            SoulSeekState.SpeedLimitDownloadOn = e.IsChecked;
            UpdateSpeedLimitsState();
            SeekerApplication.SaveSpeedLimitState();
        }

        private void EnableUlSpeedLimits_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked == SoulSeekState.SpeedLimitUploadOn)
            {
                return;
            }
            SoulSeekState.SpeedLimitUploadOn = e.IsChecked;
            UpdateSpeedLimitsState();
            SeekerApplication.SaveSpeedLimitState();
        }

        private void ShowLockedBrowse_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.HideLockedResultsInBrowse = !e.IsChecked;
        }

        private void ShowLockedSearch_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.HideLockedResultsInSearch = !e.IsChecked;
        }

        //private static AndroidX.AppCompat.App.AlertDialog configSmartFilters = null;
        private void ConfigSmartFilters_Click(object sender, EventArgs e)
        {
            MainActivity.LogInfoFirebase("ConfigSmartFilters_Click");
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme); //failed to bind....
            builder.SetTitle(Resource.String.ConfigureSmartFilters);
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.smart_filter_config_layout, (ViewGroup)this.FindViewById(Android.Resource.Id.Content), false);
            // Set up the input
            Android.Support.V7.Widget.RecyclerView recyclerViewFiltersConfig = (Android.Support.V7.Widget.RecyclerView)viewInflated.FindViewById<Android.Support.V7.Widget.RecyclerView>(Resource.Id.recyclerViewFiltersConfig);
            builder.SetView(viewInflated);



            RecyclerListAdapter adapter = new RecyclerListAdapter(this, null, SoulSeekState.SmartFilterOptions.GetAdapterItems());

            recyclerViewFiltersConfig.HasFixedSize = (true);
            recyclerViewFiltersConfig.SetAdapter(adapter);
            recyclerViewFiltersConfig.SetLayoutManager(new Android.Support.V7.Widget.LinearLayoutManager(this));

            Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new DragDropItemTouchHelper(adapter);
            var mItemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
            mItemTouchHelper.AttachToRecyclerView(recyclerViewFiltersConfig);
            adapter.ItemTouchHelper = mItemTouchHelper;

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                SoulSeekState.SmartFilterOptions.FromAdapterItems(adapter.GetAdapterItems());
                SeekerApplication.SaveSmartFilterState();

                //int portNum = -1;
                //if (!int.TryParse(input.Text, out portNum))
                //{
                //    Toast.MakeText(this, Resource.String.port_failed_parse, ToastLength.Long).Show();
                //    return;
                //}
                //if (portNum < 1024 || portNum > 65535)
                //{
                //    Toast.MakeText(this, Resource.String.port_out_of_range, ToastLength.Long).Show();
                //    return;
                //}
                //ReconfigureOptionsAPI(null, null, portNum);
                //SoulSeekState.ListenerPort = portNum;
                //UPnpManager.Instance.Feedback = true;
                //UPnpManager.Instance.SearchAndSetMappingIfRequired();
                //SeekerApplication.SaveListeningState();
                //SetPortViewText(FindViewById<TextView>(Resource.Id.portView));
                //changePortDialog.Dismiss();
            });

            EventHandler<DialogClickEventArgs> cancelHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                //{
                //    aDiag.Dismiss();
                //}
                //else
                //{
                //    changePortDialog.Dismiss();
                //}

            });

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.cancel, cancelHandler);

            AndroidX.AppCompat.App.AlertDialog diag = builder.Create();
            diag.Show();

        }

        private void ShowSmartFilters_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.ShowSmartFilters = e.IsChecked;
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.ActiveActivityRef.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                editor.PutBoolean(SoulSeekState.M_ShowSmartFilters, SoulSeekState.ShowSmartFilters);
                bool success = editor.Commit();
            }
        }

        private void ImportData_Click(object sender, EventArgs e)
        {
            if(!SoulSeekState.currentlyLoggedIn || !SoulSeekState.SoulseekClient.State.HasFlag(Soulseek.SoulseekClientStates.LoggedIn))
            {
                Toast.MakeText(this, Resource.String.MustBeLoggedInToImport, ToastLength.Long).Show();
                return;
            }
            Intent intent = new Intent(this, typeof(ImportWizardActivity));
            StartActivity(intent);
        }

        private void ClearRecentUserHistory_Click(object sender, EventArgs e)
        {
            //set to just the added users....
            int count = SoulSeekState.UserList?.Count ?? 0;
            if (count > 0)
            {
                lock(SoulSeekState.UserList)
                {
                    SoulSeekState.RecentUsersManager.SetRecentUserList(SoulSeekState.UserList.Select(uli => uli.Username).ToList());
                }
            }
            else
            {
                SoulSeekState.RecentUsersManager.SetRecentUserList(new List<string>());
            }
            SeekerApplication.SaveRecentUsers();
        }

        private void EnableDiagnostics_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if(SeekerApplication.LOG_DIAGNOSTICS != e.IsChecked)
            {
                SeekerApplication.LOG_DIAGNOSTICS = e.IsChecked;
                //if you do this without restarting, you have everything other than the diagnostics of slskclient set to Info+ rather than Debug+ 
                SeekerApplication.SetDiagnosticState(SeekerApplication.LOG_DIAGNOSTICS);
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.ActiveActivityRef.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                    editor.PutBoolean(SoulSeekState.M_LOG_DIAGNOSTICS, SeekerApplication.LOG_DIAGNOSTICS);
                    bool success = editor.Commit();
                }
            }
        }

        private void RememberRecentUsers_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.ShowRecentUsers = e.IsChecked;
        }

        private void RescanSharesButton_Click(object sender, EventArgs e)
        {
            //for rescan=true, we use the previous parse to get metadata if there is a match...
            //so that we do not have to read the file again to get things like bitrate, samples, etc.
            //if the presentable name is in the last parse, and the size matches, then use those attributes we previously had to read the file to get..
            if(SoulSeekState.PreOpenDocumentTree())
            {
                Rescan(null, -1, true, true);
            }
            else
            {
                Rescan(null, -1, false, true);
            }
        }

        private static string GetFriendlyDownloadDirectoryName()
        {
            if(SoulSeekState.RootDocumentFile==null) //even in API<21 we do set this RootDocumentFile
            {
                if(SoulSeekState.UseLegacyStorage())
                {
                    //if not set and legacy storage, then the directory is simple the default music
                    string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    return Android.Net.Uri.Parse(new Java.IO.File(path).ToURI().ToString()).LastPathSegment;
                }
                else
                {
                    //if not set and not legacy storage, then that is bad.  user must set it.
                    return SeekerApplication.GetString(Resource.String.NotSet);
                }
            }
            else
            {
                return SoulSeekState.RootDocumentFile.Uri.LastPathSegment;
            }
        }

        public static bool UseIncompleteManualFolder()
        {
            return (SoulSeekState.OverrideDefaultIncompleteLocations && SoulSeekState.RootIncompleteDocumentFile != null);
        }

        private static string GetFriendlyIncompleteDirectoryName()
        {
            if(SoulSeekState.MemoryBackedDownload)
            {
                return SeekerApplication.GetString(Resource.String.NotInUse);
            }
            if(SoulSeekState.OverrideDefaultIncompleteLocations && SoulSeekState.RootIncompleteDocumentFile != null) //if doc file is null that means we could not write to it.
            {
                return SoulSeekState.RootIncompleteDocumentFile.Uri.LastPathSegment;
            }
            else
            {
                if(!SoulSeekState.CreateCompleteAndIncompleteFolders)
                {
                    return SeekerApplication.GetString(Resource.String.AppLocalStorage);
                }
                //if not override then its whatever the download directory is...
                if (SoulSeekState.RootDocumentFile == null) //even in API<21 we do set this RootDocumentFile
                {
                    if (SoulSeekState.UseLegacyStorage())
                    {
                        //if not set and legacy storage, then the directory is simple the default music
                        string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                        return Android.Net.Uri.Parse(new Java.IO.File(path).ToURI().ToString()).LastPathSegment; //this is to prevent line breaks.
                    }
                    else
                    {
                        //if not set and not legacy storage, then that is bad.  user must set it.
                        return SeekerApplication.GetString(Resource.String.NotSet);
                    }
                }
                else
                {
                    return SoulSeekState.RootDocumentFile.Uri.LastPathSegment;
                }
            }
        }


        private void SetIncompleteDirectoryState()
        {
            if (SoulSeekState.OverrideDefaultIncompleteLocations)
            {
                incompleteFolderViewLayout.Enabled = true;
                changeIncompleteDirectory.Enabled = true;
                incompleteFolderViewLayout.Alpha = 1.0f;
                changeIncompleteDirectory.Alpha = 1.0f;
                changeIncompleteDirectory.Clickable = true;
                recyclerViewFolders.Clickable = true;
            }
            else
            {
                incompleteFolderViewLayout.Enabled = false;
                changeIncompleteDirectory.Enabled = false; //this make it not clickable
                incompleteFolderViewLayout.Alpha = 0.5f;
                changeIncompleteDirectory.Alpha = 0.5f;
                changeIncompleteDirectory.Clickable = false;
                recyclerViewFolders.Clickable = false;
            }
        }

        private void SetCompleteFolderView()
        {
            string friendlyName = Helpers.AvoidLineBreaks(GetFriendlyDownloadDirectoryName());
            currentCompleteFolderView.Text = friendlyName;
            Helpers.SetToolTipText(currentCompleteFolderView, friendlyName);
        }

        private void SetIncompleteFolderView()
        {
            string friendlyName = Helpers.AvoidLineBreaks(GetFriendlyIncompleteDirectoryName());
            currentIncompleteFolderView.Text = friendlyName;
            Helpers.SetToolTipText(currentIncompleteFolderView, friendlyName);
        }

        private void SetSharedFolderView()
        {
            if(UploadDirectoryManager.UploadDirectories.Count==0)
            {
                this.noSharedFoldersView.Visibility = ViewStates.Visible;
                this.recyclerViewFolders.Visibility = ViewStates.Gone;
                this.clearAllFoldersButton.Enabled = false;
                this.clearAllFoldersButton.Alpha = 0.5f;
            }
            else
            {
                this.noSharedFoldersView.Visibility = ViewStates.Gone;
                this.recyclerViewFolders.Visibility = ViewStates.Visible;
                this.clearAllFoldersButton.Enabled = true;
                this.clearAllFoldersButton.Alpha = 1.0f;
            }
        }



        //private static string GetIncompleteFolderLocation()
        //{
        //    if (SoulSeekState.OverrideDefaultIncompleteLocations)
        //    {

        //    }
        //    else
        //    {
        //        if (SoulSeekState.CreateCompleteAndIncompleteFolders)
        //        {

        //        }
        //        else
        //        {

        //        }
        //    }
        //}

        private void ManuallyChooseIncompleteFolderView_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.OverrideDefaultIncompleteLocations = e.IsChecked;
            SetIncompleteDirectoryState();
            SetIncompleteFolderView();
        }

        private void CreateCompleteAndIncompleteFoldersView_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.CreateCompleteAndIncompleteFolders = e.IsChecked;
            SetIncompleteFolderView();
        }

        private void CreateUsernameSubfoldersView_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.CreateUsernameSubfolders = e.IsChecked;
        }

        private void PrivHelp_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.privileges_more_info).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private void CheckPriv_Click(object sender, EventArgs e)
        {
            SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.checking_priv_), ToastLength.Short);
            PrivilegesManager.Instance.GetPrivilegesAPI(true);
        }

        private void GetPriv_Click(object sender, EventArgs e)
        {
            if(MainActivity.IsNotLoggedIn())
            {
                SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_in_to_get_privileges), ToastLength.Long);
                return;
            }
            //note: it seems that the Uri.Encode is not strictly necessary.  that is both "dog gone it" and "dog%20gone%20it" work just fine...
            Android.Net.Uri uri = Android.Net.Uri.Parse("https://www.slsknet.org/userlogin.php?username=" + Android.Net.Uri.Encode(SoulSeekState.Username)); // missing 'http://' will cause crash.
            Helpers.ViewUri(uri,this);
        }

        private void EditUserInfo_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(EditUserInfoActivity));
            this.StartActivity(intent);
        }

        private void UseUPnPCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if(e.IsChecked == SoulSeekState.ListenerUPnpEnabled)
            {
                return;
            }
            SoulSeekState.ListenerUPnpEnabled = e.IsChecked;
            SeekerApplication.SaveListeningState();

            if (e.IsChecked)
            {
                //open port...
                UPnpManager.Instance.Feedback = true;
                UPnpManager.Instance.SearchAndSetMappingIfRequired();
                
            }
            else
            {
                SetUpnpStatusView(this.FindViewById<ImageView>(Resource.Id.UPnPStatus)); //so that it shows not enabled...
            }
        }

        private void EnableListening_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if(e.IsChecked == SoulSeekState.ListenerEnabled)
            {
                return;
            }
            if(e.IsChecked)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef,Resource.String.enabling_listener, ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.disabling_listener, ToastLength.Short).Show();
            }
            SoulSeekState.ListenerEnabled = e.IsChecked;
            UpdateListeningViewState();
            SeekerApplication.SaveListeningState();
            ReconfigureOptionsAPI(null, e.IsChecked, null);
            if(e.IsChecked)
            {
                UPnpManager.Instance.Feedback = true;
                UPnpManager.Instance.SearchAndSetMappingIfRequired(); //bc it may not have been set before...
            }
        }


        private void CheckStatus_Click(object sender, EventArgs e)
        {
            Android.Net.Uri uri = Android.Net.Uri.Parse("http://tools.slsknet.org/porttest.php?port=" + SoulSeekState.ListenerPort); // missing 'http://' will cause crashed. //an https for this link does not exist
            Helpers.ViewUri(uri,this);
        }
        private static AndroidX.AppCompat.App.AlertDialog changeDialog = null;
        private void ChangePort_Click(object sender, EventArgs e)
        {
            ShowChangeDialog(ChangeDialogType.ChangePort);
        }

        public void ClearIncompleteFolder()
        {
            List<string> doNotDelete = TransfersFragment.TransferItemManagerDL.GetInUseIncompleteFolderNames();

            bool useDownloadDir = false;
            if (SoulSeekState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
            {
                useDownloadDir = true;
            }
            bool useTempDir = false;
            if (SettingsActivity.UseTempDirectory())
            {
                useTempDir = true;
            }
            bool useCustomDir = false;
            if (SettingsActivity.UseIncompleteManualFolder())
            {
                useCustomDir = true;
            }

            bool folderExists = false;
            int folderCount = 0;
            if (SoulSeekState.UseLegacyStorage() && (SoulSeekState.RootDocumentFile == null && useDownloadDir))
            {
                string rootdir = string.Empty;
                //if (SoulSeekState.RootDocumentFile==null)
                //{
                rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //}
                //else
                //{
                //    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //    rootdir = SoulSeekState.RootDocumentFile.Uri.Path; //returns junk...
                //}

                if (!(new Java.IO.File(rootdir)).Exists())
                {
                    (new Java.IO.File(rootdir)).Mkdirs();
                }
                //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                string incompleteDirString = rootdir + @"/Soulseek Incomplete/";
                Java.IO.File incompleteDir = new Java.IO.File(incompleteDirString);
                folderExists = CleanIncompleteFolder(incompleteDir, doNotDelete, out folderCount);
            }
            else
            {
                DocumentFile rootdir = null;
                if (useDownloadDir)
                {
                    if(SoulSeekState.RootDocumentFile==null)
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.ErrorDownloadDirNotProperlySet, ToastLength.Long).Show();
                        return;
                    }
                    rootdir = SoulSeekState.RootDocumentFile;
                    MainActivity.LogDebug("using download dir" + rootdir.Uri.LastPathSegment);
                }
                else if (useTempDir)
                {
                    Java.IO.File appPrivateExternal = SoulSeekState.ActiveActivityRef.GetExternalFilesDir(null);
                    rootdir = DocumentFile.FromFile(appPrivateExternal);
                    MainActivity.LogDebug("using temp incomplete dir");
                }
                else if (useCustomDir)
                {
                    rootdir = SoulSeekState.RootIncompleteDocumentFile;
                    MainActivity.LogDebug("using custom incomplete dir" + rootdir.Uri.LastPathSegment);
                }

                folderExists = CleanIncompleteFolder(rootdir.FindFile("Soulseek Incomplete"), doNotDelete, out folderCount);
            }

            if (!folderExists)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.IncompleteFolderEmpty, ToastLength.Long).Show();
            }
            else if (folderExists && folderCount == 0)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, SeekerApplication.GetString(Resource.String.NoEligibleToClear), ToastLength.Long).Show();
            }
            else
            {
                string plural = String.Empty;
                if (folderCount > 1)
                {
                    plural = "s";
                }
                Toast.MakeText(SoulSeekState.ActiveActivityRef, $"Cleared {folderCount} folder" + plural, ToastLength.Long).Show();
            }
        }

        public bool CleanIncompleteFolder(DocumentFile incompleteDirectory, List<string> incompleteFoldersToNotDelete, out int folderCount)
        {
            folderCount = 0;
            if(incompleteDirectory == null || !incompleteDirectory.Exists())
            {
                return false;
            }
            else
            {
                foreach(DocumentFile f in incompleteDirectory.ListFiles())
                {
                    // we dont create files at the root level other than .nomedia which stays.
                    if(f.IsDirectory)
                    {
                        if(!incompleteFoldersToNotDelete.Contains(f.Name))
                        {
                            folderCount++;
                            DeleteDocumentFolder(f);
                        }
                    }
                }
                return true;
            }
        }

        public void DeleteDocumentFolder(DocumentFile folder)
        {
            if(!folder.Delete())
            {
                foreach(DocumentFile f in folder.ListFiles())
                {
                    f.Delete();
                }
                folder.Delete();
            }
        }

        public bool CleanIncompleteFolder(Java.IO.File incompleteDirectory, List<string> incompleteFoldersToNotDelete, out int folderCount)
        {
            folderCount = 0;
            if (incompleteDirectory == null || !incompleteDirectory.Exists())
            {
                return false;
            }
            else
            {
                foreach (Java.IO.File f in incompleteDirectory.ListFiles())
                {
                    // we dont create files at the root level other than .nomedia which stays.
                    if (f.IsDirectory)
                    {
                        if (!incompleteFoldersToNotDelete.Contains(f.Name))
                        {
                            folderCount++;
                            DeleteLegacyFolder(f);
                        }
                    }
                }
                return true;
            }
        }

        public void DeleteLegacyFolder(Java.IO.File folder)
        {
            if (!folder.Delete())
            {
                foreach (Java.IO.File f in folder.ListFiles())
                {
                    f.Delete();
                }
                folder.Delete();
            }
        }

        public enum ChangeDialogType
        {
            ChangePort = 0,
            ChangeDL = 1,
            ChangeUL = 2,
            ConcurrentDL = 3,
        }

        private void ShowChangeDialog(ChangeDialogType changeDialogType)
        {
            MainActivity.LogInfoFirebase("ShowChangePortDialog");
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme); //failed to bind....
            if(changeDialogType == ChangeDialogType.ChangePort)
            {
                builder.SetTitle(this.GetString(Resource.String.change_port) + ":");
            }
            else if (changeDialogType == ChangeDialogType.ChangeDL)
            {
                builder.SetTitle(Resource.String.ChangeDownloadSpeed);
            }
            else if(changeDialogType == ChangeDialogType.ChangeUL)
            {
                builder.SetTitle(Resource.String.ChangeUploadSpeed);
            }
            else if(changeDialogType == ChangeDialogType.ConcurrentDL)
            {
                builder.SetTitle(Resource.String.MaxConcurrentIs);
            }
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.choose_port, (ViewGroup)this.FindViewById(Android.Resource.Id.Content), false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.chosePortEditText);
            if (changeDialogType == ChangeDialogType.ChangeDL)
            {
                input.Hint = SeekerApplication.GetString(Resource.String.EnterSpeed);
            }
            else if (changeDialogType == ChangeDialogType.ChangeUL)
            {
                input.Hint = SeekerApplication.GetString(Resource.String.EnterSpeed);
            }
            else if (changeDialogType == ChangeDialogType.ConcurrentDL)
            {
                input.Hint = SeekerApplication.GetString(Resource.String.EnterMaxDownloadSimultaneously);
            }
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                if(changeDialogType == ChangeDialogType.ChangePort)
                {
                    int portNum = -1;
                    if (!int.TryParse(input.Text, out portNum))
                    {
                        Toast.MakeText(this, Resource.String.port_failed_parse, ToastLength.Long).Show();
                        return;
                    }
                    if (portNum < 1024 || portNum > 65535)
                    {
                        Toast.MakeText(this, Resource.String.port_out_of_range, ToastLength.Long).Show();
                        return;
                    }
                    ReconfigureOptionsAPI(null, null, portNum);
                    SoulSeekState.ListenerPort = portNum;
                    UPnpManager.Instance.Feedback = true;
                    UPnpManager.Instance.SearchAndSetMappingIfRequired();
                    SeekerApplication.SaveListeningState();
                    SetPortViewText(FindViewById<TextView>(Resource.Id.portView));
                    changeDialog.Dismiss();
                }
                else if(changeDialogType == ChangeDialogType.ChangeUL || changeDialogType == ChangeDialogType.ChangeDL)
                {
                    int dlSpeedKbs = -1;
                    if (!int.TryParse(input.Text, out dlSpeedKbs))
                    {
                        Toast.MakeText(this, "Speed failed to parse", ToastLength.Long).Show();
                        return;
                    }
                    if (dlSpeedKbs < 64)
                    {
                        Toast.MakeText(this, "Minimum Speed is 64 kb/s", ToastLength.Long).Show();
                        return;
                    }
                    if(changeDialogType == ChangeDialogType.ChangeDL)
                    {
                        SoulSeekState.SpeedLimitDownloadBytesSec = 1024 * dlSpeedKbs;
                        SetSpeedTextView(FindViewById<TextView>(Resource.Id.downloadSpeed), false);
                    }
                    else
                    {
                        SoulSeekState.SpeedLimitUploadBytesSec = 1024 * dlSpeedKbs;
                        SetSpeedTextView(FindViewById<TextView>(Resource.Id.uploadSpeed), true);
                    }
                    
                    SeekerApplication.SaveSpeedLimitState();
                    changeDialog.Dismiss();
                }
                else if(changeDialogType == ChangeDialogType.ConcurrentDL)
                {
                    int concurrentDL = -1;
                    if (!int.TryParse(input.Text, out concurrentDL))
                    {
                        Toast.MakeText(this, "Failed to Parse Number", ToastLength.Long).Show();
                        return;
                    }
                    if (concurrentDL < 1)
                    {
                        Toast.MakeText(this, "Must be greater than 0", ToastLength.Long).Show();
                        return;
                    }

                    Soulseek.SimultaneousDownloadsGatekeeper.MaxUsersConcurrent = concurrentDL;
                    // always add space as the resource string will always trim trailing spaces.
                    FindViewById<TextView>(Resource.Id.concurrentDownloadsLabel).Text = SeekerApplication.GetString(Resource.String.MaxConcurrentIs) + " " + Soulseek.SimultaneousDownloadsGatekeeper.MaxUsersConcurrent;

                    SaveMaxConcurrentDownloadsSettings();
                    changeDialog.Dismiss();
                }

            });

            EventHandler<DialogClickEventArgs> cancelHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    changeDialog.Dismiss();
                }

            });


            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search) //ImeNull if being called due to the enter key being pressed. (MSDN) but ImeNull gets called all the time....
                {
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(this.FindViewById<ViewGroup>(Android.Resource.Id.Content).WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Browse Logic...
                    eventHandler(sender, null);
                }
            };

            input.EditorAction += editorAction;
            input.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.cancel, cancelHandler);

            changeDialog = builder.Create();
            changeDialog.Show();
        }

        private void Input_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SoulSeekState.ActiveActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("MainActivity_FocusChange" + err.Message);
            }
        }

        private static void SetPortViewText(TextView tv)
        {
            tv.Text = SoulSeekState.ActiveActivityRef.GetString(Resource.String.port) + ": " + SoulSeekState.ListenerPort.ToString();
        }

        private static void SetSpeedTextView(TextView tv, bool isUpload)
        {
            int speedKbs = isUpload ? (SoulSeekState.SpeedLimitUploadBytesSec / 1024) : (SoulSeekState.SpeedLimitDownloadBytesSec / 1024);
            tv.Text = speedKbs.ToString() + " kb/s";
        }

        private static void SetSpinnerPositionSpeed(Spinner spinner, bool isUpload)
        {
            if(isUpload)
            {
                if (SoulSeekState.SpeedLimitUploadIsPerTransfer)
                {
                    spinner.SetSelection(0);
                }
                else
                {
                    spinner.SetSelection(1);
                }
            }
            else
            {
                if(SoulSeekState.SpeedLimitDownloadIsPerTransfer)
                {
                    spinner.SetSelection(0);
                }
                else
                {
                    spinner.SetSelection(1);
                }
            }
        }

        private static void SetPrivStatusView(TextView tv)
        {
            string privileges = SeekerApplication.GetString(Resource.String.privileges) + ": ";
            tv.Text = privileges + PrivilegesManager.Instance.GetPrivilegeStatus();
        }

        private static void SetUpnpStatusView(ImageView iv)
        {
            //TODO
            Tuple< UPnpManager.ListeningIcon,string> info = UPnpManager.Instance.GetIconAndMessage();
            if(iv==null) return;
            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
            {
                iv.TooltipText = info.Item2; //api26+ otherwise crash...
            }
            else
            {
                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(iv, info.Item2);
            }
            switch (info.Item1)
            {
                case UPnpManager.ListeningIcon.ErrorIcon:
                    iv.SetImageResource(Resource.Drawable.lan_disconnect);
                    break;
                case UPnpManager.ListeningIcon.OffIcon:
                    iv.SetImageResource(Resource.Drawable.network_off_outline);
                    break;
                case UPnpManager.ListeningIcon.PendingIcon:
                    iv.SetImageResource(Resource.Drawable.lan_pending);
                    break;
                case UPnpManager.ListeningIcon.SuccessIcon:
                    iv.SetImageResource(Resource.Drawable.lan_connect);
                    break;
            }
        }

        private void ListeningMoreInfo_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.listening).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        public void MoreInfoForceFilesystem_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            //var diag = builder.SetMessage(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.about_body).TrimStart(' '), SeekerApplication.GetVersionString())).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            var diag = builder.SetMessage(Resource.String.force_filesystem_message).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
            var origString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.force_filesystem_message); //this is a literal CDATA string.
            if ((int)Android.OS.Build.VERSION.SdkInt >= 24)
            {
                ((TextView)diag.FindViewById(Android.Resource.Id.Message)).TextFormatted = Android.Text.Html.FromHtml(origString, Android.Text.FromHtmlOptions.ModeLegacy); //this can be slow so do NOT do it in loops...
            }
            else
            {
                ((TextView)diag.FindViewById(Android.Resource.Id.Message)).TextFormatted = Android.Text.Html.FromHtml(origString); //this can be slow so do NOT do it in loops...
            }
            ((TextView)diag.FindViewById(Android.Resource.Id.Message)).MovementMethod = (Android.Text.Method.LinkMovementMethod.Instance);
        }

        private static bool HasManageStoragePermission(Context context)
        {
            bool hasExternalStoragePermissions = false;
            if ((int)Android.OS.Build.VERSION.SdkInt >= 30)
            {
                hasExternalStoragePermissions = Android.OS.Environment.IsExternalStorageManager;
            }
            else
            {
                hasExternalStoragePermissions = ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.ManageExternalStorage) != Android.Content.PM.Permission.Denied;
            }
            return hasExternalStoragePermissions;
        }

        private void ForceFilesystemPermission_Click(object sender, EventArgs e)
        {
            bool hasExternalStoragePermissions = HasManageStoragePermission(this);

            if (hasExternalStoragePermissions)
            {
                Toast.MakeText(this, SoulSeekState.ActiveActivityRef.GetString(Resource.String.permission_already_successfully_granted), ToastLength.Long).Show();
            }
            else
            {
                Intent allFilesPermission = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                Android.Net.Uri packageUri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                allFilesPermission.SetData(packageUri);
                this.StartActivityForResult(allFilesPermission, FORCE_REQUEST_STORAGE_MANAGER);
            }
        }

        public const string FromBrowseSelf = "FromBrowseSelf";
        private void BrowseSelfButton_Click(object sender, EventArgs e)
        {
            BrowseSelf(false, false);
        }

        private void BrowseSelfButton_LongClick(object sender, View.LongClickEventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.BrowseWhich)
                .SetPositiveButton(Resource.String.public_room, (object sender, DialogClickEventArgs e) => { BrowseSelf(true, false); OnCloseClick(sender, e); })
                .SetNegativeButton(Resource.String.target_user_list, (object sender, DialogClickEventArgs e) => { BrowseSelf(false, true); OnCloseClick(sender, e); })
                .Create();
            diag.Show();
        }

        private void BrowseSelf(bool forcePublic, bool forceFriend)
        {
            if (!SoulSeekState.SharingOn || SoulSeekState.SharedFileCache == null || UploadDirectoryManager.UploadDirectories.Count == 0)
            {
                Toast.MakeText(this, Resource.String.not_sharing, ToastLength.Short).Show();
                return;
            }
            if (SoulSeekState.IsParsing)
            {
                Toast.MakeText(this, Resource.String.WaitForParsing, ToastLength.Short).Show();
                return;
            }
            if (!SoulSeekState.SharedFileCache.SuccessfullyInitialized || SoulSeekState.SharedFileCache.GetBrowseResponseForUser(SoulSeekState.Username) == null)
            {
                Toast.MakeText(this, Resource.String.failed_to_parse_shares_post, ToastLength.Short).Show();
                return;
            }
            string errorMsgToToast = string.Empty;

            Soulseek.BrowseResponse browseResponseToShow = null;
            if (forcePublic)
            {
                browseResponseToShow = SoulSeekState.SharedFileCache.GetBrowseResponseForUser(null);
            }
            else if(forceFriend)
            {
                browseResponseToShow = SoulSeekState.SharedFileCache.GetBrowseResponseForUser(null, true);
            }
            else
            {
                browseResponseToShow = SoulSeekState.SharedFileCache.GetBrowseResponseForUser(SoulSeekState.Username);
            }

            TreeNode<Soulseek.Directory> tree = DownloadDialog.CreateTree(browseResponseToShow, false, null, null, SoulSeekState.Username, out errorMsgToToast);
            if (errorMsgToToast != null && errorMsgToToast != string.Empty)
            {
                Toast.MakeText(this, errorMsgToToast, ToastLength.Short).Show();
                return;
            }
            if (tree != null)
            {
                SoulSeekState.OnBrowseResponseReceived(SoulSeekState.SharedFileCache.GetBrowseResponseForUser(SoulSeekState.Username), tree, SoulSeekState.Username, null);
            }

            Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop);
            intent.PutExtra(FromBrowseSelf, 3); //the tab to go to
            this.StartActivity(intent);
        }

        private void SetButtonText(Button btn)
        {
            if(SoulSeekState.IsStartUpServiceCurrentlyRunning)
            {
                btn.Text = this.GetString(Resource.String.stop);
            }
            else
            {
                btn.Text = this.GetString(Resource.String.start);
            }
        }

        private void StartupServiceButton_Click(object sender, EventArgs e)
        {
            if (SoulSeekState.IsStartUpServiceCurrentlyRunning)
            {
                Intent seekerKeepAliveService = new Intent(this, typeof(SeekerKeepAliveService));
                this.StopService(seekerKeepAliveService);
                SoulSeekState.IsStartUpServiceCurrentlyRunning = false;
            }
            else
            {
                Intent seekerKeepAliveService = new Intent(this, typeof(SeekerKeepAliveService));
                this.StartService(seekerKeepAliveService);
                SoulSeekState.IsStartUpServiceCurrentlyRunning = true;
            }
            SetButtonText(FindViewById<Button>(Resource.Id.startServiceOnStartupButton));
        }

        private void StartServiceOnStartupCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.StartServiceOnStartup = e.IsChecked;
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.ActiveActivityRef.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                editor.PutBoolean(SoulSeekState.M_ServiceOnStartup, SoulSeekState.StartServiceOnStartup);
                bool success = editor.Commit();
            }
        }

        private void StartupServiceMoreInfo_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.keep_alive_service).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private void AllowPrivateRoomInvitations_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if(e.IsChecked == SoulSeekState.AllowPrivateRoomInvitations)
            {
                MainActivity.LogDebug("allow private: nothing to do");
            }
            else
            {
                string newstate = e.IsChecked ? this.GetString(Resource.String.allowed) : this.GetString(Resource.String.denied);
                Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(this.GetString(Resource.String.setting_priv_invites),newstate), ToastLength.Short).Show();
                ReconfigureOptionsAPI(e.IsChecked, null, null);

            }
        }

        private void ReconfigureOptionsAPI(bool? allowPrivateInvites, bool? enableListener, int? newPort)
        {
            bool requiresConnection = allowPrivateInvites.HasValue;
            if (!SoulSeekState.currentlyLoggedIn && requiresConnection) //note: you CAN in fact change listening and port without being logged in...
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_be_logged_to_toggle_priv_invites, ToastLength.Short).Show();
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState() && requiresConnection)
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) => {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() => { ReconfigureOptionsLogic(allowPrivateInvites, enableListener, newPort); }));
                }));
            }
            else
            {
                ReconfigureOptionsLogic(allowPrivateInvites, enableListener, newPort);
            }
        }

        private static void ReconfigureOptionsLogic(bool? allowPrivateInvites, bool? enableTheListener, int? listenerPort)
        {
            //Toast.MakeText(this.Context, "Contacting user for directory list. Will appear in browse tab when complete", ToastLength.Short).Show();
            Task<bool> reconfigTask = null;
            try
            {
                Soulseek.SoulseekClientOptionsPatch patch = new Soulseek.SoulseekClientOptionsPatch(acceptPrivateRoomInvitations: allowPrivateInvites, enableListener: enableTheListener, listenPort: listenerPort);

                reconfigTask = SoulSeekState.SoulseekClient.ReconfigureOptionsAsync(patch);
            }
            catch(Exception e)
            {   //this can still happen on ReqFiles_Click.. maybe for the first check we were logged in but for the second we somehow were not..
                MainActivity.LogFirebase("reconfigure options: " + e.Message + e.StackTrace);
                MainActivity.LogDebug("reconfigure options FAILED" + e.Message + e.StackTrace);
                return;
            }
            Action<Task<bool>> continueWithAction = new Action<Task<bool>>((reconfigTask) => {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                    if(reconfigTask.IsFaulted)
                    {
                        MainActivity.LogDebug("reconfigure options FAILED");
                        if(allowPrivateInvites.HasValue)
                        {
                            string enabledDisabled = allowPrivateInvites.Value ? SoulSeekState.ActiveActivityRef.GetString(Resource.String.allowed) : SoulSeekState.ActiveActivityRef.GetString(Resource.String.denied);
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_setting_priv_invites),enabledDisabled), ToastLength.Long).Show();
                            if(SoulSeekState.ActiveActivityRef is SettingsActivity settingsActivity)
                            {
                                //set the check to false
                                settingsActivity.allowPrivateRoomInvitations.Checked = SoulSeekState.AllowPrivateRoomInvitations; //old value
                            }
                        }

                        if(enableTheListener.HasValue)
                        {
                            string enabledDisabled = enableTheListener.Value ? SoulSeekState.ActiveActivityRef.GetString(Resource.String.allowed) : SoulSeekState.ActiveActivityRef.GetString(Resource.String.denied);
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_error_setting_listener), enabledDisabled), ToastLength.Long).Show();
                        }

                        if(listenerPort.HasValue)
                        {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_error_setting_listener_port),listenerPort.Value), ToastLength.Long).Show();
                        }



                    }
                    else
                    {
                        if(allowPrivateInvites.HasValue)
                        {
                            MainActivity.LogDebug("reconfigure options SUCCESS, restart required? " + reconfigTask.Result);
                            SoulSeekState.AllowPrivateRoomInvitations = allowPrivateInvites.Value;
                            //set shared prefs...
                            lock (MainActivity.SHARED_PREF_LOCK)
                            {
                                var editor = SoulSeekState.ActiveActivityRef.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                                editor.PutBoolean(SoulSeekState.M_AllowPrivateRooomInvitations, allowPrivateInvites.Value);
                                bool success = editor.Commit();
                            }
                        }
                    }
                });
            });
            reconfigTask.ContinueWith(continueWithAction);
        }

        private void MemoryFileDownloadSwitchIcon_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.memory_file_backed).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private void MemoryFileDownloadSwitchCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.MemoryBackedDownload = !e.IsChecked;
            SetIncompleteFolderView();
        }

        private void ShowDownloadNotification_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.DisableDownloadToastNotification = !e.IsChecked;
        }

        private void FreeUploadSlotsOnly_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.FreeUploadSlotsOnly = e.IsChecked;
        }

        private void MoreInfoButton_Click(object sender, EventArgs e)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            var diag = builder.SetMessage(Resource.String.sharing_dialog).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
        }

        private void OnCloseClick(object sender, DialogClickEventArgs e)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
        }

        private void UpdateShareImageView()
        {
            Tuple<SharingIcons,string> info = MainActivity.GetSharingMessageAndIcon(out bool isParsing);
            ImageView imageView = this.FindViewById<ImageView>(Resource.Id.sharedStatus);
            ProgressBar progressBar = this.FindViewById<ProgressBar>(Resource.Id.progressBarSharedStatus);
            if(imageView==null || progressBar == null) return;
            string toolTip = info.Item2;
            int numParsed = SoulSeekState.NumberParsed;
            if (isParsing && numParsed != 0)
            {
                if(numParsed==int.MaxValue) //our signal we are finishing up (i.e. creating token index)
                {
                    toolTip = toolTip + $" ({SeekerApplication.GetString(Resource.String.finishingUp)})";
                }
                else
                {
                    toolTip = toolTip + String.Format( $" ({SeekerApplication.GetString(Resource.String.XFilesParsed)})", numParsed);
                }
            }
            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
            {
                imageView.TooltipText = toolTip; //api26+ otherwise crash...
                progressBar.TooltipText = toolTip;
            }
            else
            {
                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(imageView, toolTip);
                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(progressBar, toolTip);
            }
            switch(info.Item1)
            {
                case SharingIcons.On:
                    imageView.SetImageResource(Resource.Drawable.ic_file_upload_black_24dp);
                    break;
                case SharingIcons.Error:
                    imageView.SetImageResource(Resource.Drawable.ic_error_outline_white_24dp);
                    break;
                case SharingIcons.CurrentlyParsing:
                    imageView.SetImageResource(Resource.Drawable.exclamation_thick);
                    break;
                case SharingIcons.Off:
                    imageView.SetImageResource(Resource.Drawable.ic_sharing_off_black_24dp);
                    break;
                case SharingIcons.OffDueToNetwork:
                    imageView.SetImageResource(Resource.Drawable.network_strength_off_outline);
                    break;
            }
            
            switch(info.Item1)
            {
                case SharingIcons.CurrentlyParsing:
                    progressBar.Visibility = ViewStates.Visible;
                    break;
                default:
                    progressBar.Visibility = ViewStates.Invisible;
                    break;
            }

            //in case new errors to update.
            this.recyclerViewFoldersAdapter?.NotifyDataSetChanged();
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            if(item.ItemId == 1) //options
            {
                ShowDialogForUploadDir(ContextMenuItem);
            }
            else if(item.ItemId == 2) //remove
            {
                RemoveUploadDirFolder(ContextMenuItem);
            }
            return true;
        }

        private void RemoveUploadDirFolder(UploadDirectoryInfo uploadDirInfo)
        {
            if(UploadDirectoryManager.UploadDirectories.Count == 1)
            {
                this.ClearAllFolders(); //since now we have 0 this will just properly clear everything.
            }
            else
            {
                UploadDirectoryManager.UploadDirectories.Remove(uploadDirInfo);
                this.recyclerViewFoldersAdapter.NotifyDataSetChanged();
                SetSharedFolderView();
                Rescan(null, -1, UploadDirectoryManager.AreAnyFromLegacy(), false);
            }
        }


        private void ShareCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.SharingOn = e.IsChecked;
            MainActivity.SetUnsetSharingBasedOnConditions(true);
            if(MainActivity.MeetsSharingConditions() && !SoulSeekState.IsParsing && !MainActivity.IsSharingSetUpSuccessfully())
            {
                //try to set up sharing...
                MainActivity.SetUpSharing(UpdateShareImageView);
            }
            UpdateShareImageView();
            UpdateSharingViewState();
            SetSharedFolderView();
            this.recyclerViewFoldersAdapter?.NotifyDataSetChanged(); //so that the views rebind as unclickable.
        }

        private void UpdateSharingViewState()
        {
            //this isnt winforms where disabling parent, disables all children..

            if(SoulSeekState.SharingOn)
            {
                sharingSubLayout1.Enabled = true;
                sharingSubLayout1.Alpha = 1.0f;
                sharingSubLayout2.Enabled = true;
                sharingSubLayout2.Alpha = 1.0f;
                addFolderButton.Clickable = true;
                clearAllFoldersButton.Clickable = true;
                browseSelfButton.Clickable = true;
                browseSelfButton.LongClickable = true;
                rescanSharesButton.Clickable = true;
            }
            else
            {
                sharingSubLayout1.Enabled = false;
                sharingSubLayout1.Alpha = 0.5f;
                sharingSubLayout2.Enabled = false;
                sharingSubLayout2.Alpha = 0.5f;
                addFolderButton.Clickable = false;
                clearAllFoldersButton.Clickable = false;
                browseSelfButton.Clickable = false;
                browseSelfButton.LongClickable = false;
                rescanSharesButton.Clickable = false;
            }
        }

        private void UpdateListeningViewState()
        {
            if(SoulSeekState.ListenerEnabled)
            {
                listeningSubLayout2.Enabled = true;
                listeningSubLayout3.Enabled = true;
                listeningSubLayout2.Alpha = 1.0f;
                listeningSubLayout3.Alpha = 1.0f;
                useUPnPCheckBox.Clickable = true;
                changePort.Clickable = true;
                checkStatus.Clickable = true;
            }
            else
            {
                listeningSubLayout2.Enabled = false;
                listeningSubLayout3.Enabled = false;
                listeningSubLayout2.Alpha = 0.5f;
                listeningSubLayout3.Alpha = 0.5f;
                useUPnPCheckBox.Clickable = false;
                changePort.Clickable = false;
                checkStatus.Clickable = false;
            }
        }

        private void UpdateConcurrentDownloadLimitsState()
        {
            if (Soulseek.SimultaneousDownloadsGatekeeper.RestrictConcurrentUsers)
            {
                concurrentDlSublayout.Enabled = true;
                concurrentDlSublayout.Alpha = 1.0f;
                concurrentDlButton.Clickable = true;
                concurrentDlButton.Alpha = 1.0f;
            }
            else
            {
                concurrentDlSublayout.Enabled = false;
                concurrentDlSublayout.Alpha = 0.5f;
                concurrentDlButton.Clickable = false;
                concurrentDlButton.Alpha = 0.5f;
            }
        }

        private void UpdateSpeedLimitsState()
        {
            if (SoulSeekState.SpeedLimitDownloadOn)
            {
                limitDlSpeedSubLayout.Enabled = true;
                limitDlSpeedSubLayout.Alpha = 1.0f;
                dlSpeedTextView.Alpha = 1.0f;
                changeDlSpeed.Alpha = 1.0f;
                changeDlSpeed.Clickable = true;
                dlLimitPerTransfer.Alpha = 1.0f;
                dlLimitPerTransfer.Clickable = true;
                dlLimitPerTransfer.Enabled = true;
            }
            else
            {
                limitDlSpeedSubLayout.Enabled = false;
                limitDlSpeedSubLayout.Alpha = 0.5f;
                dlSpeedTextView.Alpha = 0.5f;
                changeDlSpeed.Alpha = 0.5f;
                changeDlSpeed.Clickable = false;
                dlLimitPerTransfer.Alpha = 0.5f;
                dlLimitPerTransfer.Clickable = false;
                dlLimitPerTransfer.Enabled = false;
            }

            if (SoulSeekState.SpeedLimitUploadOn)
            {
                limitUlSpeedSubLayout.Enabled = true;
                limitUlSpeedSubLayout.Alpha = 1.0f;
                ulSpeedTextView.Alpha = 1.0f;
                changeUlSpeed.Alpha = 1.0f;
                changeUlSpeed.Clickable = true;
                ulLimitPerTransfer.Alpha = 1.0f;
                ulLimitPerTransfer.Clickable = true;
                ulLimitPerTransfer.Enabled = true;
            }
            else
            {
                limitUlSpeedSubLayout.Enabled = false;
                limitUlSpeedSubLayout.Alpha = 0.5f;
                ulSpeedTextView.Alpha = 0.5f;
                changeUlSpeed.Alpha = 0.5f;
                changeUlSpeed.Clickable = false;
                ulLimitPerTransfer.Alpha = 0.5f;
                ulLimitPerTransfer.Clickable = false;
                ulLimitPerTransfer.Enabled = false;
            }
        }

        private void ImageView_Click(object sender, EventArgs e)
        {
            UpdateShareImageView();
            (sender as View).PerformLongClick();
        }


        private void DayVarient_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var oldVarient = SoulSeekState.DayModeVarient;
            SoulSeekState.DayModeVarient = (ThemeHelper.DayThemeType)(e.Position);
            if(oldVarient != SoulSeekState.DayModeVarient)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = this.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                    editor.PutInt(SoulSeekState.M_DayVarient, (int)(SoulSeekState.DayModeVarient));
                    bool success = editor.Commit();
                }
                SeekerApplication.SetActivityTheme(this);
                //if we are in day mode and the day varient is truly changed we need to recreate all activities
                if(!this.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes))
                {
                    SeekerApplication.RecreateActivies();
                }
            }
        }

        private void NightVarient_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var oldVarient = SoulSeekState.NightModeVarient;
            switch(e.Position)
            {
                case 3:
                    SoulSeekState.NightModeVarient = ThemeHelper.NightThemeType.AmoledClassicPurple;
                    break;
                case 4:
                    SoulSeekState.NightModeVarient = ThemeHelper.NightThemeType.AmoledGrey;
                    break;
                default:
                    SoulSeekState.NightModeVarient = (ThemeHelper.NightThemeType)(e.Position);
                    break;
            }
            if (oldVarient != SoulSeekState.NightModeVarient)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = this.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                    editor.PutInt(SoulSeekState.M_NightVarient, (int)(SoulSeekState.NightModeVarient));
                    bool success = editor.Commit();
                }
                SeekerApplication.SetActivityTheme(this);
                //if we are in day mode and the day varient is truly changed we need to recreate all activities
                if (this.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes))
                {
                    SeekerApplication.RecreateActivies();
                }
            }
        }



        private void DayNightMode_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {

            MainActivity.LogDebug("DayNightMode_ItemSelected: Pos:" + e.Position + "state: " + SoulSeekState.DayNightMode + "actual: " + AppCompatDelegate.DefaultNightMode);

            if(e.Position == 0)
            {
                SoulSeekState.DayNightMode = -1;
            }
            else
            {
                SoulSeekState.DayNightMode = e.Position;
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = this.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                editor.PutInt(SoulSeekState.M_DayNightMode, SoulSeekState.DayNightMode);
                bool success = editor.Commit();
                int x = this.GetSharedPreferences("SoulSeekPrefs", 0).GetInt(SoulSeekState.M_DayNightMode,-1);
                MainActivity.LogDebug("was commit successful: "  + success);
                MainActivity.LogDebug("after writing and immediately reading: " + x);
            }
            //auto = 0, light = 1, dark = 2.  //NO we do NOT want to do AUTO, that is follow time.  we want to do FOLLOW SYSTEM i.e. -1.
            switch (e.Position)
            {
                case 0:
                    if(AppCompatDelegate.DefaultNightMode == -1)
                    {
                        return;
                    }
                    else
                    {
                        AppCompatDelegate.DefaultNightMode = (int)(AppCompatDelegate.ModeNightFollowSystem);
                        //this.Recreate();
                    }
                    break;
                case 1:
                    if (AppCompatDelegate.DefaultNightMode == 1)
                    {
                        return;
                    }
                    else
                    {
                        AppCompatDelegate.DefaultNightMode = (int)(AppCompatDelegate.ModeNightNo);
                        //this.Recreate();
                    }
                    break;
                case 2:
                    if (AppCompatDelegate.DefaultNightMode == 2)
                    {
                        return;
                    }
                    else
                    {
                        AppCompatDelegate.DefaultNightMode = (int)(AppCompatDelegate.ModeNightYes);
                        //this.Recreate();
                    }
                    break;
                default:
                    return;
            }
        }

        private void RememberSearchHistory_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.RememberSearchHistory = e.IsChecked;
        }

        private void ClearHistory_Click(object sender, EventArgs e)
        {
            SoulSeekState.ClearSearchHistoryInvoke();
        }

        private void AutoClearCompleteUploads_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.AutoClearCompleteUploads = e.IsChecked;
        }

        private void AutoClearComplete_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SoulSeekState.AutoClearCompleteDownloads = e.IsChecked;
        }

        private void SetSpinnerPosition(Spinner s)
        {
            int selectionIndex = 3;
            foreach (var pair in positionNumberPairs)
            {
                if (pair.Item2 == SoulSeekState.NumberSearchResults)
                {
                    selectionIndex = pair.Item1;
                }
            }
            s.SetSelection(selectionIndex);
        }

        private void SetSpinnerPositionDayNight(Spinner s)
        {
            s.SetSelection(Math.Max(SoulSeekState.DayNightMode, 0)); //-1 -> 0
        }
        private void SetSpinnerPositionDayVarient(Spinner s)
        {
            s.SetSelection((int)(SoulSeekState.DayModeVarient));
        }

        private void SetSpinnerPositionLangauge(Spinner s)
        {
            switch (SeekerApplication.GetLegacyLanguageString())
            {
                case SoulSeekState.FieldLangAuto:
                    s.SetSelection(0);
                    break;
                case SoulSeekState.FieldLangEn:
                    s.SetSelection(1);
                    break;
                case SoulSeekState.FieldLangPtBr:
                    s.SetSelection(2);
                    break;
                case SoulSeekState.FieldLangFr:
                    s.SetSelection(3);
                    break;
                case SoulSeekState.FieldLangRu:
                    s.SetSelection(4);
                    break;
                case SoulSeekState.FieldLangEs:
                    s.SetSelection(5);
                    break;
                case SoulSeekState.FieldLangUk:
                    s.SetSelection(6);
                    break;
                case SoulSeekState.FieldLangNl:
                    s.SetSelection(7);
                    break;
                case SoulSeekState.FieldLangCs:
                    s.SetSelection(8);
                    break;
                default:
                    s.SetSelection(0);
                    break;
            }
        }

        private string GetLanguageStringFromPosition(int pos)
        {
            switch (pos)
            {
                case 0:
                    return SoulSeekState.FieldLangAuto;
                case 1:
                    return SoulSeekState.FieldLangEn;
                case 2:
                    return SoulSeekState.FieldLangPtBr;
                case 3:
                    return SoulSeekState.FieldLangFr;
                case 4:
                    return SoulSeekState.FieldLangRu;
                case 5:
                    return SoulSeekState.FieldLangEs;
                case 6:
                    return SoulSeekState.FieldLangUk;
                case 7:
                    return SoulSeekState.FieldLangNl;
                case 8:
                    return SoulSeekState.FieldLangCs;
                default:
                    return SoulSeekState.FieldLangAuto;
            }
        }

        private void SetSpinnerPositionNightVarient(Spinner s)
        {
            switch (SoulSeekState.NightModeVarient)
            {
                case ThemeHelper.NightThemeType.AmoledClassicPurple:
                    s.SetSelection(3);
                    break;
                case ThemeHelper.NightThemeType.AmoledGrey:
                    s.SetSelection(4);
                    break;
                default:
                    s.SetSelection((int)(SoulSeekState.NightModeVarient));
                    break;
            }

        }
        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void RestoreDefaults_Click(object sender, EventArgs e)
        {
            SoulSeekState.NumberSearchResults = MainActivity.DEFAULT_SEARCH_RESULTS;
            SoulSeekState.AutoClearCompleteDownloads = false;
            SoulSeekState.AutoClearCompleteUploads = false;
            SoulSeekState.RememberSearchHistory = true;
            SoulSeekState.ShowRecentUsers = true;
            SoulSeekState.SharingOn = false;
            SoulSeekState.FreeUploadSlotsOnly = true;
            SoulSeekState.DisableDownloadToastNotification = true;
            SoulSeekState.MemoryBackedDownload = false;
            SoulSeekState.DayNightMode = AppCompatDelegate.ModeNightFollowSystem;
            SoulSeekState.HideLockedResultsInBrowse = true;
            SoulSeekState.HideLockedResultsInSearch = true;
            (FindViewById<CheckBox>(Resource.Id.autoClearComplete) as CheckBox).Checked = SoulSeekState.AutoClearCompleteDownloads;
            (FindViewById<CheckBox>(Resource.Id.autoClearCompleteUploads) as CheckBox).Checked = SoulSeekState.AutoClearCompleteUploads;
            (FindViewById<CheckBox>(Resource.Id.searchHistoryRemember) as CheckBox).Checked = SoulSeekState.RememberSearchHistory;
            (FindViewById<CheckBox>(Resource.Id.rememberRecentUsers) as CheckBox).Checked = SoulSeekState.ShowRecentUsers;
            (FindViewById<CheckBox>(Resource.Id.enableSharing) as CheckBox).Checked = SoulSeekState.SharingOn;
            (FindViewById<CheckBox>(Resource.Id.freeUploadSlots) as CheckBox).Checked = SoulSeekState.FreeUploadSlotsOnly;
            (FindViewById<CheckBox>(Resource.Id.showLockedInBrowseResponse) as CheckBox).Checked = !SoulSeekState.HideLockedResultsInBrowse;
            (FindViewById<CheckBox>(Resource.Id.showLockedInSearch) as CheckBox).Checked = !SoulSeekState.HideLockedResultsInSearch;
            (FindViewById<CheckBox>(Resource.Id.showToastNotificationOnDownload) as CheckBox).Checked = SoulSeekState.DisableDownloadToastNotification;
            (FindViewById<CheckBox>(Resource.Id.memoryFileDownloadSwitchCheckBox) as CheckBox).Checked = !SoulSeekState.MemoryBackedDownload;
            Spinner searchNumSpinner = FindViewById<Spinner>(Resource.Id.searchNumberSpinner);
            SetSpinnerPosition(searchNumSpinner);
            Spinner daynightSpinner = FindViewById<Spinner>(Resource.Id.nightModeSpinner);
            SetSpinnerPositionDayNight(daynightSpinner);
        }

        private void SearchNumSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            SoulSeekState.NumberSearchResults = positionNumberPairs[e.Position].Item2;
        }

        private void ChangeDownloadDirectory(object sender, EventArgs e)
        {
            ShowDirSettings(SoulSeekState.SaveDataDirectoryUri,DirectoryType.Download);
        }

        private bool needsMediaStorePermission()
        {
            if((int)Android.OS.Build.VERSION.SdkInt >= 33)
            {
                return Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadMediaAudio) == Android.Content.PM.Permission.Denied;
            }
            else
            {
                return Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadExternalStorage) == Android.Content.PM.Permission.Denied;
            }
        }

        private void requestMediaStorePermission()
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 33)
            {
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.ReadMediaAudio }, READ_EXTERNAL_FOR_MEDIA_STORE);
            }
            else
            {
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.ReadExternalStorage }, READ_EXTERNAL_FOR_MEDIA_STORE);
            }
        }

        private void AddUploadDirectory(object sender, EventArgs e)
        {
            // We request ReadExternalStorage so that we can query the media store to get music attributes (duration, bitrate)
            //   quickly (i.e. without having to load the file from disk and read attributes).
            // API 33 (Android 13) target - this permission has no effect.  Instead use the granular ReadMediaAudio since we only 
            //   use the media store for audio anyway.  If we were previously granted ReadExternalStorage then we get ReadMedia* 
            //   automatically when upgrading.

            //you dont have this on api >= 29 because you never requested it, but it is NECESSARY to read media store
            if (needsMediaStorePermission())
            {
                //if they deny the permission twice and are on api >= 30, then it will auto deny (behavior is the same as if they manually clicked deny).
                requestMediaStorePermission();
            }
            else
            {
                ShowDirSettings(null, DirectoryType.Upload);
            }
        }

        private void ChangeIncompleteDirectory(object sender, EventArgs e)
        {
            ShowDirSettings(SoulSeekState.ManualIncompleteDataDirectoryUri, DirectoryType.Incomplete);
        }

        private void UseInternalFilePicker(int requestCode)
        {
            //Create FolderOpenDialog
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.FolderChoose);
            fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath).ContinueWith(
                (Task<string> t) => {
                    if (t.Result == null || t.Result == string.Empty)
                    {
                        return;
                    }
                    else
                    {
                        Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(t.Result));
                        DocumentFile f = DocumentFile.FromFile(new Java.IO.File(t.Result)); //from tree uri not added til 21 also.  from single uri returns a f.Exists=false file.
                        if(f==null)
                        {
                            MainActivity.LogFirebase("api<21 f is null");
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(this, Resource.String.error_reading_dir, ToastLength.Long).Show(); });
                            return;
                        }
                        else if(!f.Exists())
                        {
                            MainActivity.LogFirebase("api<21 f does not exist");
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(this, Resource.String.error_reading_dir, ToastLength.Long).Show(); });
                            return;
                        }
                        else if(!f.IsDirectory)
                        {
                            MainActivity.LogFirebase("api<21 NOT A DIRECTORY");
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {Toast.MakeText(this, Resource.String.error_not_a_dir, ToastLength.Long).Show(); });
                            return;
                        }

                        if (requestCode == CHANGE_WRITE_EXTERNAL_LEGACY)
                        {
                            this.SuccessfulWriteExternalLegacyCallback(uri, true);
                        }
                        else if(requestCode == UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY)
                        {
                            this.Rescan(uri, requestCode, true);
                        }
                        else if(requestCode == CHANGE_INCOMPLETE_EXTERNAL_LEGACY)
                        {
                            this.SuccessfulIncompleteExternalLegacyCallback(uri, true);
                        }
                    }


                });
        }



        private void ShowDirSettings(string startingDirectory, DirectoryType directoryType, bool errorReselectCase = false)
        {
            int requestCode = -1;
            if (SoulSeekState.UseLegacyStorage())
            {
                var legacyIntent = new Intent(Intent.ActionOpenDocumentTree);
                if (!string.IsNullOrEmpty(startingDirectory))
                {
                    Android.Net.Uri res = Android.Net.Uri.Parse(startingDirectory);
                    legacyIntent.PutExtra(DocumentsContract.ExtraInitialUri, res);
                }
                legacyIntent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPrefixUriPermission);
                if (directoryType == DirectoryType.Download)
                {
                    requestCode = CHANGE_WRITE_EXTERNAL_LEGACY;
                }
                else if (directoryType == DirectoryType.Upload)
                {
                    if(errorReselectCase)
                    {
                        requestCode = UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY_Reselect_Case;
                    }
                    else
                    {
                        requestCode = UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY;
                    }
                }
                else if (directoryType == DirectoryType.Incomplete)
                {
                    requestCode = CHANGE_INCOMPLETE_EXTERNAL_LEGACY;
                }
                try
                {
                    this.StartActivityForResult(legacyIntent, requestCode);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains(Helpers.NoDocumentOpenTreeToHandle))
                    {
                        FallbackFileSelectionEntry(requestCode);
                    }
                    else
                    {
                        MainActivity.LogFirebase("showDirSettings: " + e.Message + e.StackTrace);
                        throw e;
                    }
                }
            }
            else
            {
                var storageManager = Android.OS.Storage.StorageManager.FromContext(this);
                var intent = storageManager.PrimaryStorageVolume.CreateOpenDocumentTreeIntent();
                intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPrefixUriPermission);
                if (!string.IsNullOrEmpty(startingDirectory))
                {
                    Android.Net.Uri res = Android.Net.Uri.Parse(startingDirectory);
                    intent.PutExtra(DocumentsContract.ExtraInitialUri, res);
                }
                if (directoryType == DirectoryType.Download)
                {
                    requestCode = CHANGE_WRITE_EXTERNAL;
                }
                else if(directoryType == DirectoryType.Upload)
                {
                    if (errorReselectCase)
                    {
                        requestCode = UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case;
                    }
                    else
                    {
                        requestCode = UPLOAD_DIR_ADD_WRITE_EXTERNAL;
                    }
                }
                else if(directoryType == DirectoryType.Incomplete)
                {
                    requestCode = CHANGE_INCOMPLETE_EXTERNAL;
                }
                try
                {
                    this.StartActivityForResult(intent, requestCode);
                }
                catch(Exception e)
                {
                    if (e.Message.Contains(Helpers.NoDocumentOpenTreeToHandle))
                    {
                        FallbackFileSelectionEntry(requestCode);
                    }
                    else
                    {
                        MainActivity.LogFirebase("showDirSettings: " + e.Message + e.StackTrace);
                        throw e;
                    }
                }
            }
        }

        private int ConvertRequestCodeIntoLegacyVersion(int requestCodeNotLegacy)
        {
            switch(requestCodeNotLegacy)
            {
                case UPLOAD_DIR_ADD_WRITE_EXTERNAL:
                    return UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY;
                case CHANGE_INCOMPLETE_EXTERNAL:
                    return CHANGE_INCOMPLETE_EXTERNAL_LEGACY;
                case CHANGE_WRITE_EXTERNAL:
                    return CHANGE_WRITE_EXTERNAL_LEGACY;
                case UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case:
                    return UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY_Reselect_Case;
                default:
                    return requestCodeNotLegacy;
            }
        }

        public static bool DoWeHaveProperPermissionsForInternalFilePicker()
        {
            if(SoulSeekState.RequiresEitherOpenDocumentTreeOrManageAllFiles())
            {
                return Android.OS.Environment.IsExternalStorageManager;
            }
            else
            {
                return true; //since in this case its ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Denied. which we already request if user does not have it since its needed to download.
            }
        }

        private void FallbackFileSelectionEntry(int requestCode)
        {
            requestCode = ConvertRequestCodeIntoLegacyVersion(requestCode);

            bool hasManageAllFilesManisfestPermission = false;

#if IzzySoft
            hasManageAllFilesManisfestPermission = true;
#endif

            if (SoulSeekState.RequiresEitherOpenDocumentTreeOrManageAllFiles() && hasManageAllFilesManisfestPermission && !Android.OS.Environment.IsExternalStorageManager) //this is "step 1"
            {
                Intent allFilesPermission = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                Android.Net.Uri packageUri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                allFilesPermission.SetData(packageUri);
                this.StartActivityForResult(allFilesPermission, requestCode + 32);
            } 
            else if (DoWeHaveProperPermissionsForInternalFilePicker())  //isExternalStorageManager added in API30, but RequiresEitherOpenDocumentTreeOrManageAllFiles protects against that being called on pre 30 devices.
            {
                UseInternalFilePicker(requestCode);
            }
            else
            {
                //show error message...
                if (SoulSeekState.RequiresEitherOpenDocumentTreeOrManageAllFiles() && !hasManageAllFilesManisfestPermission)
                {
                    MainActivity.ShowSimpleAlertDialog(this, Resource.String.error_no_file_manager_dir_manage_storage, Resource.String.okay);
                }
                else
                {
                    Toast.MakeText(this, SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_no_file_manager_dir), ToastLength.Long).Show();
                }
            }
        }



        private void SuccessfulWriteExternalLegacyCallback(Android.Net.Uri uri, bool fromLegacyPicker=false)
        {
            var x = uri;
            //SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
            SoulSeekState.SaveDataDirectoryUri = uri.ToString();
            SoulSeekState.SaveDataDirectoryUriIsFromTree = !fromLegacyPicker;
            //this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission);
            DocumentFile docFile = null;
            if (fromLegacyPicker)
            {
                docFile = DocumentFile.FromFile(new Java.IO.File(uri.Path));
            }
            else
            {
                docFile = DocumentFile.FromTreeUri(this, uri);
            }
            SoulSeekState.RootDocumentFile = docFile; 
            this.RunOnUiThread(new Action(() =>
            {
                SoulSeekState.DirectoryUpdatedEvent?.Invoke(null, new EventArgs());
                Toast.MakeText(this, string.Format(this.GetString(Resource.String.successfully_changed_dl_dir), uri.Path), ToastLength.Long).Show();
            }));
        }

        public static bool UseTempDirectory()
        {
            return !UseIncompleteManualFolder() && !SoulSeekState.CreateCompleteAndIncompleteFolders;
        }

        private void SuccessfulIncompleteExternalLegacyCallback(Android.Net.Uri uri, bool fromLegacyPicker = false)
        {
            var x = uri;
            //SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
            SoulSeekState.ManualIncompleteDataDirectoryUri = uri.ToString();
            SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree = !fromLegacyPicker;
            //this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission);
            DocumentFile docFile = null;
            if (fromLegacyPicker)
            {
                docFile = DocumentFile.FromFile(new Java.IO.File(uri.Path));
            }
            else
            {
                docFile = DocumentFile.FromTreeUri(this, uri);
            }
            SoulSeekState.RootIncompleteDocumentFile = docFile;
            this.RunOnUiThread(new Action(() =>
            {
                SoulSeekState.DirectoryUpdatedEvent?.Invoke(null, new EventArgs());
                Toast.MakeText(this, string.Format(this.GetString(Resource.String.successfully_changed_incomplete_dir), uri.Path), ToastLength.Long).Show();
            }));
        }

        public void ShowDialogForUploadDir(UploadDirectoryInfo uploadInfo)
        {
            if(uploadInfo.HasError())
            {
                ShowUploadDirectoryErrorDialog(uploadInfo);
            }
            else
            {
                ShowUploadDirectoryOptionsDialog(uploadInfo);
            }
        }
        private static UploadDirectoryInfo UploadDirToReplaceOnReselect = null;
        public void ShowUploadDirectoryErrorDialog(UploadDirectoryInfo uploadInfo)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(Resource.String.FolderError);
            string diagMessage = SeekerApplication.GetString(Resource.String.ErrorForFolder) + uploadInfo.GetLastPathSegment() + System.Environment.NewLine + UploadDirectoryManager.GetErrorString(uploadInfo.ErrorState) + System.Environment.NewLine;
            var diag = builder.SetMessage(diagMessage)
                .SetNegativeButton(Resource.String.RemoveFolder, (object sender, DialogClickEventArgs e) => { //puts it slightly right
                    this.RemoveUploadDirFolder(uploadInfo);
                    this.OnCloseClick(sender, e);
                })
                .SetPositiveButton(Resource.String.Reselect, (object sender, DialogClickEventArgs e) => { //puts it rightmost
                    UploadDirToReplaceOnReselect = uploadInfo;
                    this.ShowDirSettings(uploadInfo.UploadDataDirectoryUri, DirectoryType.Upload, true);
                    this.OnCloseClick(sender, e);
                })
                .SetNeutralButton(Resource.String.cancel, OnCloseClick) //puts it leftmost
                .Create();
            diag.Show();
        }

        public void ShowUploadDirectoryOptionsDialog(UploadDirectoryInfo uploadDirInfo)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme); //used to be our cached main activity ref...
            builder.SetTitle(Resource.String.UploadFolderOptions);
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.upload_folder_options, this.FindViewById<ViewGroup>(Android.Resource.Id.Content) as ViewGroup, false);
            EditText custromFolderNameEditText = viewInflated.FindViewById<EditText>(Resource.Id.customFolderNameEditText);
            CheckBox overrideFolderName = viewInflated.FindViewById<CheckBox>(Resource.Id.overrideFolderName);
            CheckBox hiddenCheck = viewInflated.FindViewById<CheckBox>(Resource.Id.hiddenUserlistOnly);
            CheckBox lockedCheck = viewInflated.FindViewById<CheckBox>(Resource.Id.lockedUserlistOnly);
            overrideFolderName.CheckedChange += (object sender, CompoundButton.CheckedChangeEventArgs e) => {
                if(e.IsChecked)
                {
                    custromFolderNameEditText.Enabled = true;
                    custromFolderNameEditText.Alpha = 1.0f;
                }
                else
                {
                    custromFolderNameEditText.Enabled = false;
                    custromFolderNameEditText.Alpha = 0.5f;
                }


                };
            if (!string.IsNullOrEmpty(uploadDirInfo.DisplayNameOverride))
            {
                custromFolderNameEditText.Text = uploadDirInfo.DisplayNameOverride;
                overrideFolderName.Checked = true;
            }
            else
            {
                overrideFolderName.Checked = false;
            }
            hiddenCheck.Checked = uploadDirInfo.IsHidden;
            lockedCheck.Checked = uploadDirInfo.IsLocked;

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandlerOkay = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                //the okay case.


                //any changed?

                bool hiddenChanged = uploadDirInfo.IsHidden != hiddenCheck.Checked;
                bool lockedChanged = uploadDirInfo.IsLocked != lockedCheck.Checked;
                bool overrideNameChanged = 
                    (string.IsNullOrEmpty(uploadDirInfo.DisplayNameOverride) && overrideFolderName.Checked && !string.IsNullOrEmpty(custromFolderNameEditText.Text)) ||
                    ((!overrideFolderName.Checked || string.IsNullOrEmpty(custromFolderNameEditText.Text)) && !string.IsNullOrEmpty(uploadDirInfo.DisplayNameOverride)) ||
                    (overrideFolderName.Checked && uploadDirInfo.DisplayNameOverride != custromFolderNameEditText.Text);

                uploadDirInfo.IsHidden = hiddenCheck.Checked;
                uploadDirInfo.IsLocked = lockedCheck.Checked;
                string displayNameOld = uploadDirInfo.DisplayNameOverride;

                if (overrideFolderName.Checked && !string.IsNullOrEmpty(custromFolderNameEditText.Text))
                {
                    if(uploadDirInfo.DisplayNameOverride != custromFolderNameEditText.Text)
                    {
                        //make sure that we CAN change it.
                        uploadDirInfo.DisplayNameOverride = custromFolderNameEditText.Text;
                        if(!UploadDirectoryManager.DoesNewDirectoryHaveUniqueRootName(uploadDirInfo, false))
                        {
                            uploadDirInfo.DisplayNameOverride = displayNameOld;
                            Toast.MakeText(this, Resource.String.CannotChangeNameNotUnique, ToastLength.Long).Show();
                            overrideNameChanged = false; //we prevented it
                        }
                    }
                }
                else
                {
                    if(!string.IsNullOrEmpty(uploadDirInfo.DisplayNameOverride))
                    {
                        //make sure that we CAN change it.
                        uploadDirInfo.DisplayNameOverride = null;
                        if (!UploadDirectoryManager.DoesNewDirectoryHaveUniqueRootName(uploadDirInfo, false))
                        {
                            uploadDirInfo.DisplayNameOverride = displayNameOld;
                            Toast.MakeText(this, Resource.String.CannotChangeNameNotUnique, ToastLength.Long).Show();
                            overrideNameChanged = false; //we prevented it
                        }
                    }
                }

                this.recyclerViewFoldersAdapter.NotifyDataSetChanged();
                if(hiddenChanged || lockedChanged || overrideNameChanged)
                {
                    MainActivity.LogDebug("things changed re: folder options..");
                    Rescan(null, -1, UploadDirectoryManager.AreAnyFromLegacy(), false);
                }

                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {

                }
            });

            builder.SetPositiveButton(Resource.String.okay, eventHandlerOkay);
            var diag = builder.Create();
            diag.Show();
        }

        public static EventHandler<EventArgs> UploadDirectoryChanged;
        public static volatile bool MoreChangesHaveBeenMadeSoRescanWhenDone = false;
        public static volatile List<Android.Net.Uri> NewlyAddedUrisWeHaveToAddAfter = new List<Android.Net.Uri>();

        public void ParseDatabaseAndUpdateUI(Android.Net.Uri newlyAddedUriIfApplicable, int requestCode, bool fromLegacyPicker = false, bool rescanClicked = false, bool reselectCase = false)
        {

            if (rescanClicked)
            {
                if (SoulSeekState.IsParsing)
                {
                    this.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(this, Resource.String.AlreadyParsing, ToastLength.Long).Show();
                    }));
                    return;
                }
                if (UploadDirectoryManager.UploadDirectories.Count == 0)
                {
                    this.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(this, Resource.String.DirectoryNotSet, ToastLength.Long).Show();
                    }));
                    return;
                }
            }

            if (rescanClicked || newlyAddedUriIfApplicable != null)
            {
                this.RunOnUiThread(new Action(() =>
                {
                    Toast.MakeText(this, Resource.String.parsing_files_wait, ToastLength.Long).Show();
                }));
            }



            UploadDirectoryInfo newlyAddedDirectory = null;
            if (newlyAddedUriIfApplicable != null)
            {
                //RESELECT CASE
                if (reselectCase)
                {
                    newlyAddedDirectory = new UploadDirectoryInfo(newlyAddedUriIfApplicable.ToString(), !fromLegacyPicker, UploadDirToReplaceOnReselect.IsLocked, UploadDirToReplaceOnReselect.IsHidden, UploadDirToReplaceOnReselect.DisplayNameOverride);
                    newlyAddedDirectory.UploadDirectory = fromLegacyPicker ? DocumentFile.FromFile(new Java.IO.File(newlyAddedUriIfApplicable.Path)) : DocumentFile.FromTreeUri(this, newlyAddedUriIfApplicable);
                    UploadDirectoryManager.UploadDirectories.Remove(UploadDirToReplaceOnReselect);
                }
                else
                {
                    newlyAddedDirectory = new UploadDirectoryInfo(newlyAddedUriIfApplicable.ToString(), !fromLegacyPicker, false, false, null);
                    newlyAddedDirectory.UploadDirectory = fromLegacyPicker ? DocumentFile.FromFile(new Java.IO.File(newlyAddedUriIfApplicable.Path)) : DocumentFile.FromTreeUri(this, newlyAddedUriIfApplicable);
                }



                if (UploadDirectoryManager.UploadDirectories.Where(up => up.UploadDataDirectoryUri == newlyAddedUriIfApplicable.ToString()).Count() != 0)
                {
                    //error!!
                    this.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.ErrorAlreadyAdded , ToastLength.Long).Show();
                    }));
                    return;
                    //throw new Exception("Directory is already added!");
                }

                UploadDirectoryManager.UploadDirectories.Add(newlyAddedDirectory);
            }

            UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
            if (UploadDirectoryManager.AreAllFailed())
            {
                throw new MainActivity.DirectoryAccessFailure("All Failed");
            }

            if (newlyAddedDirectory != null)
            {
                bool isUnqiue = UploadDirectoryManager.DoesNewDirectoryHaveUniqueRootName(newlyAddedDirectory, true);
                if (!isUnqiue)
                {
                    MainActivity.LogDebug("Root name was not unique. Updated it to be unique.");
                }
                UploadDirectoryChanged?.Invoke(null, new EventArgs());
            }


            if (SoulSeekState.IsParsing)
            {
                MainActivity.LogDebug("We are already parsing!!! so after this parse, lets parse again with our cached results to pick up our new changes");
                MoreChangesHaveBeenMadeSoRescanWhenDone = true;
                return;
            }

            try
            {
                MainActivity.LogDebug("Parsing now......");

                SoulSeekState.IsParsing = true;
                int prevFiles = -1;
                bool success = false;
                if (rescanClicked && SoulSeekState.SharedFileCache != null)
                {
                    prevFiles = SoulSeekState.SharedFileCache.FileCount;
                }
                this.RunOnUiThread(new Action(() =>
                {
                    UpdateShareImageView(); //for is parsing..
                    SetSharedFolderView();
                }));
                try
                {

                    success = MainActivity.InitializeDatabase(null, false, out string errorMessage);
                    if (!success)
                    {
                        throw new Exception("Failed to parse shared files: " + errorMessage);
                    }
                    SoulSeekState.IsParsing = false;
                }
                catch (Exception e)
                {
                    SoulSeekState.IsParsing = false;
                    //SoulSeekState.UploadDataDirectoryUri = null;
                    //SoulSeekState.UploadDataDirectoryUriIsFromTree = true;
                    MainActivity.ClearParsedCacheResults();
                    MainActivity.SetUnsetSharingBasedOnConditions(true);
                    if (!(e is MainActivity.DirectoryAccessFailure))
                    {
                        MainActivity.LogFirebase("error parsing: " + e.Message + "  " + e.StackTrace);
                    }
                    this.RunOnUiThread(new Action(() =>
                    {
                        UpdateShareImageView();
                        SetSharedFolderView();
                        if (!(e is MainActivity.DirectoryAccessFailure))
                        {
                            Toast.MakeText(this, e.Message, ToastLength.Long).Show();
                        }
                        else
                        {
                            Toast.MakeText(this, Resource.String.FailedGettingAccess, ToastLength.Long).Show(); //TODO get error from UploadManager..
                        }

                    }));
                    UploadDirectoryChanged?.Invoke(null, new EventArgs());
                    return;
                }
                //SoulSeekState.UploadDataDirectoryUri = uri.ToString();
                //SoulSeekState.UploadDataDirectoryUriIsFromTree = !fromLegacyPicker;
                if ((UPLOAD_DIR_ADD_WRITE_EXTERNAL == requestCode || UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case == requestCode) && newlyAddedUriIfApplicable != null)
                {
                    this.ContentResolver.TakePersistableUriPermission(newlyAddedUriIfApplicable, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                }
                //setup soulseek client with handlers if all conditions met
                MainActivity.SetUnsetSharingBasedOnConditions(true, true);
                this.RunOnUiThread(new Action(() =>
                {
                    UpdateShareImageView();
                    SetSharedFolderView();
                    int dirs = SoulSeekState.SharedFileCache.DirectoryCount; //TODO: nullref here... U318AA, LG G7 ThinQ, both android 10
                    int files = SoulSeekState.SharedFileCache.FileCount;
                    string msg = string.Format(this.GetString(Resource.String.success_setting_shared_dir_fnum_dnum), dirs, files);
                    if (rescanClicked) //tack on additional message if applicable..
                    {
                        int diff = files - prevFiles;
                        if (diff > 0)
                        {
                            if (diff > 1)
                            {
                                msg = msg + String.Format(" " + SeekerApplication.GetString(Resource.String.AdditionalFiles), diff);
                            }
                            else
                            {
                                msg = msg + " " + SeekerApplication.GetString(Resource.String.OneAdditionalFile);
                            }
                        }
                    }
                    Toast.MakeText(this, msg, ToastLength.Long).Show();
                }));
            }
            finally
            {
                SoulSeekState.IsParsing = false;
                if(MoreChangesHaveBeenMadeSoRescanWhenDone)
                {
                    MainActivity.LogDebug("okay now lets pick up our new changes");
                    MoreChangesHaveBeenMadeSoRescanWhenDone = false;
                    ParseDatabaseAndUpdateUI(null, requestCode, fromLegacyPicker, false);
                }
            }
        }


        /// <summary>
        /// We always use the previous metadata info if its there. so we always kind of "rescan"
        /// </summary>
        /// <param name="newlyAddedUriIfApplicable"></param>
        /// <param name="requestCode"></param>
        /// <param name="fromLegacyPicker"></param>
        /// <param name="rescanClicked"></param>
        private void Rescan(Android.Net.Uri newlyAddedUriIfApplicable, int requestCode, bool fromLegacyPicker = false, bool rescanClicked = false, bool reselectCase = false)
        {
            Action parseDatabaseAndUpdateUiAction = new Action(() => {
                try
                {
                    ParseDatabaseAndUpdateUI(newlyAddedUriIfApplicable, requestCode, fromLegacyPicker, rescanClicked, reselectCase);
                }
                catch(MainActivity.DirectoryAccessFailure)
                {
                    if(rescanClicked)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {Toast.MakeText(this, Resource.String.SharedFolderIssuesAllFailed, ToastLength.Long).Show(); });
                    }
                    else
                    {
                        throw;
                    }
                }
            });

            System.Threading.ThreadPool.QueueUserWorkItem((object o) => { parseDatabaseAndUpdateUiAction(); });
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (READ_EXTERNAL_FOR_MEDIA_STORE == requestCode)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted) //still let them do it. important for auto-deny case.
                {
                    ShowDirSettings(null, DirectoryType.Upload);
                }
                else
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.NoMediaStore , ToastLength.Short).Show();
                    ShowDirSettings(null, DirectoryType.Upload);
                }
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            //if from manage external settings
            if(CHANGE_WRITE_EXTERNAL_LEGACY == requestCode - 32 || CHANGE_INCOMPLETE_EXTERNAL_LEGACY == requestCode - 32 || UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY == requestCode - 32)
            {
                if (SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
                {
                    //phase 2 - actually pick a file.
                    UseInternalFilePicker(requestCode-32);
                }
                else
                {
                    Toast.MakeText(this, Resource.String.NoPermissionsForDir, ToastLength.Long).Show();
                }
            }


            if (CHANGE_WRITE_EXTERNAL == requestCode)
            {
                if(resultCode == Result.Ok)
                {
                    var x = data.Data;
                    SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
                    SoulSeekState.SaveDataDirectoryUri = data.Data.ToString();
                    SoulSeekState.SaveDataDirectoryUriIsFromTree = true;
                    this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission| ActivityFlags.GrantReadUriPermission);
                    this.RunOnUiThread(new Action( ()=>
                    {
                        SoulSeekState.DirectoryUpdatedEvent?.Invoke(null, new EventArgs());
                        Toast.MakeText(this, string.Format(this.GetString(Resource.String.successfully_changed_dl_dir),data.Data), ToastLength.Long).Show();
                    }));
                }
            }
            if(CHANGE_WRITE_EXTERNAL_LEGACY == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    SuccessfulWriteExternalLegacyCallback(data.Data);
                }
            }


            if (CHANGE_INCOMPLETE_EXTERNAL == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    var x = data.Data;
                    SoulSeekState.RootIncompleteDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
                    SoulSeekState.ManualIncompleteDataDirectoryUri = data.Data.ToString();
                    SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree = true;
                    this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    this.RunOnUiThread(new Action(() =>
                    {
                        SoulSeekState.DirectoryUpdatedEvent?.Invoke(null, new EventArgs());
                        Toast.MakeText(this, string.Format(this.GetString(Resource.String.successfully_changed_incomplete_dir), data.Data), ToastLength.Long).Show();
                    }));
                }
            }
            if (CHANGE_INCOMPLETE_EXTERNAL_LEGACY == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    SuccessfulIncompleteExternalLegacyCallback(data.Data);
                }
            }


            if (UPLOAD_DIR_ADD_WRITE_EXTERNAL == requestCode || 
                UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY == requestCode ||
                UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY_Reselect_Case == requestCode ||
                UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case == requestCode)
            {
                if(resultCode != Result.Ok)
                {
                    return;
                }

                bool reselectCase = false;
                if(UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case == requestCode || UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY_Reselect_Case == requestCode)
                {
                    reselectCase = true;
                }
                //make sure you can parse the files before setting the directory..

                //this takes 5+ seconds in Debug mode (with 20-30 albums) which means that this MUST be done on a separate thread..
                Rescan(data.Data, requestCode, false, false, reselectCase);

            }

            if(SAVE_SEEKER_SETTINGS == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    var seekerImportExportData = GetCurrentExportData();

                    var stream = this.ContentResolver.OpenOutputStream(data.Data);
                    var xmlWriterSettings = new XmlWriterSettings() { Indent = true };
                    using (var writer = XmlWriter.Create(stream, xmlWriterSettings))
                    {
                        new XmlSerializer(typeof(SeekerImportExportData)).Serialize(writer, seekerImportExportData);
                    }

                    Toast.MakeText(this, Resource.String.successfully_exported, ToastLength.Short).Show();
                }
            }

            if(FORCE_REQUEST_STORAGE_MANAGER == requestCode)
            {
                bool hasPermision = HasManageStoragePermission(this);
                if(hasPermision)
                {
                    Toast.MakeText(this, Resource.String.permission_successfully_granted, ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, Resource.String.permission_failed, ToastLength.Short).Show();
                }
            }
        }

        private SeekerImportExportData GetCurrentExportData()
        {
            var seekerImportExportData = new SeekerImportExportData();
            seekerImportExportData.Userlist = SoulSeekState.UserList.Select(uli => uli.Username).ToList();
            seekerImportExportData.BanIgnoreList = SoulSeekState.IgnoreUserList.Select(uli => uli.Username).ToList();
            seekerImportExportData.Wishlist = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget == SearchTarget.Wishlist).Select((pair1) => pair1.Value.LastSearchTerm).ToList();
            List<KeyValueEl> userNotes = new List<KeyValueEl>();
            foreach (KeyValuePair<string, string> pair in SoulSeekState.UserNotes)
            {
                userNotes.Add(new KeyValueEl() { Key = pair.Key, Value = pair.Value });
            }
            seekerImportExportData.UserNotes = userNotes;
            return seekerImportExportData;
        }

        public static void RestoreAdditionalDirectorySettingsFromSharedPreferences()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                SoulSeekState.CreateCompleteAndIncompleteFolders = SoulSeekState.SharedPreferences.GetBoolean(SoulSeekState.M_CreateCompleteAndIncompleteFolders, true);
                SoulSeekState.OverrideDefaultIncompleteLocations = SoulSeekState.SharedPreferences.GetBoolean(SoulSeekState.M_UseManualIncompleteDirectoryUri, false);
                SoulSeekState.CreateUsernameSubfolders = SoulSeekState.SharedPreferences.GetBoolean(SoulSeekState.M_AdditionalUsernameSubdirectories, false);
                SoulSeekState.ManualIncompleteDataDirectoryUri = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_ManualIncompleteDirectoryUri, string.Empty);
                SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree = SoulSeekState.SharedPreferences.GetBoolean(SoulSeekState.M_ManualIncompleteDirectoryUriIsFromTree, true);
            }
        }

        public static void SaveAdditionalDirectorySettingsToSharedPreferences()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutBoolean(SoulSeekState.M_CreateCompleteAndIncompleteFolders, SoulSeekState.CreateCompleteAndIncompleteFolders);
                editor.PutBoolean(SoulSeekState.M_UseManualIncompleteDirectoryUri, SoulSeekState.OverrideDefaultIncompleteLocations);
                editor.PutBoolean(SoulSeekState.M_AdditionalUsernameSubdirectories, SoulSeekState.CreateUsernameSubfolders);
                editor.PutString(SoulSeekState.M_ManualIncompleteDirectoryUri, SoulSeekState.ManualIncompleteDataDirectoryUri);
                editor.PutBoolean(SoulSeekState.M_ManualIncompleteDirectoryUriIsFromTree, SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree);
                bool success = editor.Commit();
            }
        }

        public static void SaveMaxConcurrentDownloadsSettings()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutBoolean(SoulSeekState.M_LimitSimultaneousDownloads, Soulseek.SimultaneousDownloadsGatekeeper.RestrictConcurrentUsers);
                editor.PutInt(SoulSeekState.M_MaxSimultaneousLimit, Soulseek.SimultaneousDownloadsGatekeeper.MaxUsersConcurrent);
                bool success = editor.Commit();
            }
        }

        public static void SaveManualIncompleteDirToSharedPreferences()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_ManualIncompleteDirectoryUri, SoulSeekState.ManualIncompleteDataDirectoryUri);
                editor.PutBoolean(SoulSeekState.M_ManualIncompleteDirectoryUriIsFromTree, SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree);
                bool success = editor.Commit();
            }
        }

    }

    public static class ThemeHelper
    {
        public const string ClassicPurple = "Classic Purple";
        public const string Grey = "Grey";
        public const string Blue = "Blue";
        public const string Red = "Red";
        public const string AmoledClassicPurple = "Amoled - Classic Purple";
        public const string AmoledGrey = "Amoled - Grey";

        public enum DayThemeType : ushort
        {
            ClassicPurple = 0,
            Red = 1,
            Blue = 2,
            Grey = 3,
        }

        public static DayThemeType FromDayThemeTypeString(string themeTypeString)
        {
            switch (themeTypeString)
            {
                case ClassicPurple:
                    return DayThemeType.ClassicPurple;
                case Grey:
                    return DayThemeType.Grey;
                case Blue:  
                    return DayThemeType.Blue;
                case Red:
                    return DayThemeType.Red;
                default:
                    throw new Exception("unknown");
            }
        }

        public static string ToDayThemeString(DayThemeType dayTheme)
        {
            switch (dayTheme)
            {
                case DayThemeType.ClassicPurple:
                    return ClassicPurple;
                case DayThemeType.Grey:
                    return Grey;
                case DayThemeType.Blue:
                    return Blue;
                case DayThemeType.Red:
                    return Red;
                default:
                    throw new Exception("unknown");
            }
        }

        public static int ToDayThemeProper(DayThemeType dayTheme)
        {
            switch (dayTheme)
            {
                case DayThemeType.ClassicPurple:
                    return Resource.Style.DefaultLight;
                case DayThemeType.Grey:
                    return Resource.Style.DefaultDark_Grey; //TODO
                case DayThemeType.Blue:
                    return Resource.Style.DefaultLight_Blue;
                case DayThemeType.Red:
                    return Resource.Style.DefaultLight_Red;
                default:
                    throw new Exception("unknown");
            }
        }

        public enum NightThemeType : ushort
        {
            ClassicPurple = 0,
            Grey = 1,
            Blue = 2,
            Red = 3,
            AmoledClassicPurple = 4,
            AmoledGrey = 5
        }

        public static NightThemeType FromNightThemeTypeString(string themeTypeString)
        {
            switch (themeTypeString)
            {
                case ClassicPurple:
                    return NightThemeType.ClassicPurple;
                case Grey:
                    return NightThemeType.Grey;
                case Blue:
                    return NightThemeType.Blue;
                case Red:
                    return NightThemeType.Red;
                case AmoledClassicPurple:
                    return NightThemeType.ClassicPurple;
                case AmoledGrey:
                    return NightThemeType.AmoledGrey;
                default:
                    throw new Exception("unknown");
            }
        }


        public static string ToNightThemeString(NightThemeType nightTheme)
        {
            switch (nightTheme)
            {
                case NightThemeType.ClassicPurple:
                    return ClassicPurple;
                case NightThemeType.Grey:
                    return Grey;
                case NightThemeType.Blue:
                    return Blue;
                case NightThemeType.Red:
                    return Red;
                case NightThemeType.AmoledClassicPurple:
                    return ClassicPurple;
                case NightThemeType.AmoledGrey:
                    return AmoledGrey;
                default:
                    throw new Exception("unknown");
            }
        }

        public static int ToNightThemeProper(NightThemeType nightTheme)
        {
            switch (nightTheme)
            {
                case NightThemeType.ClassicPurple:
                    return Resource.Style.DefaultDark;
                case NightThemeType.Grey:
                    return Resource.Style.DefaultDark_Grey;
                case NightThemeType.Blue:
                    return Resource.Style.DefaultDark_Blue;
                case NightThemeType.Red:
                    return Resource.Style.DefaultDark_Blue; //doesnt exist
                case NightThemeType.AmoledClassicPurple:
                    return Resource.Style.Amoled;
                case NightThemeType.AmoledGrey:
                    return Resource.Style.Amoled_Grey;
                default:
                    throw new Exception("unknown");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isNightMode">This is for the SYSTEM</param>
        /// <returns></returns>
        public static int GetThemeInChosenDayNightMode(bool isNightMode, Context c)
        {
            Context contextToUse = c == null ? SoulSeekState.ActiveActivityRef : c;
            if (contextToUse.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes))
            {
                if(isNightMode)
                {
                    return ThemeHelper.ToNightThemeProper(SoulSeekState.NightModeVarient);
                }
                else
                {
                    switch (SoulSeekState.NightModeVarient)
                    {
                        case NightThemeType.ClassicPurple:
                            return ThemeHelper.ToDayThemeProper(ThemeHelper.DayThemeType.ClassicPurple);
                        case NightThemeType.Blue:
                            return ThemeHelper.ToDayThemeProper(ThemeHelper.DayThemeType.Blue);
                        default:
                            return ThemeHelper.ToDayThemeProper(ThemeHelper.DayThemeType.ClassicPurple);
                    }
                }
            }
            else
            {
                if (!isNightMode)
                {
                    return ThemeHelper.ToDayThemeProper(SoulSeekState.DayModeVarient);
                }
                else
                {
                    switch (SoulSeekState.DayModeVarient)
                    {
                        case DayThemeType.ClassicPurple:
                            return ThemeHelper.ToNightThemeProper(ThemeHelper.NightThemeType.ClassicPurple);
                        case DayThemeType.Blue:
                            return ThemeHelper.ToNightThemeProper(ThemeHelper.NightThemeType.Blue);
                        default:
                            return ThemeHelper.ToNightThemeProper(ThemeHelper.NightThemeType.ClassicPurple);
                    }
                }
            }
        }

    }

    enum DirectoryType : ushort
    {
        Download = 0,
        Upload = 1,
        Incomplete = 2
    }
}

