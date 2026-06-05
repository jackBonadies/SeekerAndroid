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
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
//using AndroidX.AppCompat.Widget;
//using AndroidX.AppCompat.Widget.Helper;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using AndroidX.DocumentFile.Provider;
using AndroidX.RecyclerView.Widget;
using Common;
using Common.Share;
using Google.Android.Material.Divider;
using Google.Android.Material.TextField;
using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Managers;
using Seeker.Services;
using Seeker.UPnP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Seeker
{
    [Activity(Label = "SettingsActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false, ParentActivity = typeof(MainActivity))]
    public partial class SettingsActivity : ThemeableActivity, Seeker.Settings.Rows.ISettingsHost //AppCompatActivity is needed to support chaning light / dark mode programmatically...
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

        private static readonly int[] searchResultOptions = { 25, 50, 100, 250, 500, 1000, 2000 };
        internal CheckBox allowPrivateRoomInvitations;

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.settings_menu, menu);
            WireSearchMenu(menu);
            return base.OnCreateOptionsMenu(menu);
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OnBackPressedDispatcher.OnBackPressed();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        protected override void OnResume()
        {
            base.OnResume();

            UPnpManager.Instance.SearchFinished += UpnpSearchFinished;
            PrivilegesManager.Instance.PrivilegesChecked += OnPrivilegesChecked;

            //when you open up the directory selection with OpenDocumentTree the SettingsActivity is paused
            this.UpdateDirectoryViews();

            StorageState.DirectoryUpdatedEvent += DirectoryUpdated;
            SharedFileService.SharingStatusChangedEvent += SharingStatusUpdated;
            EnsureParsingTicker(); // resume the live count if a parse is still running

        }

        private void OnPrivilegesChecked(object sender, EventArgs e)
        {
            SeekerState.ActiveActivityRef?.RunOnUiThread(() =>
                _settingsAdapter?.NotifyRowChanged("account.privileges"));
        }

        private void SharingStatusUpdated(object sender, EventArgs e)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                RefreshModernSharingRows(true);
                EnsureParsingTicker();
            });
        }

        private void UpnpSearchFinished(object sender, EventArgs e)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                if (PreferencesState.ListenerEnabled && PreferencesState.ListenerUPnpEnabled && UPnpManager.Instance.RunningStatus == UPnPRunningStatus.Finished && UPnpManager.Instance.DiagStatus != UPnPDiagStatus.Success)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.upnp_search_finished), ToastLength.Short);
                }
            });
        }

        private void DirectoryUpdated(object sender, EventArgs e)
        {
            UpdateDirectoryViews();
        }

        private void UpdateDirectoryViews()
        {
            _settingsAdapter?.NotifyRowChanged("downloads.folder");
            _settingsAdapter?.NotifyRowChanged("downloads.incomplete_path");
            RefreshModernSharingRows(false);
        }

        protected override void OnPause()
        {

            UPnpManager.Instance.SearchFinished -= UpnpSearchFinished;
            PrivilegesManager.Instance.PrivilegesChecked -= OnPrivilegesChecked;
            StorageState.DirectoryUpdatedEvent -= DirectoryUpdated;
            SharedFileService.SharingStatusChangedEvent -= SharingStatusUpdated;
            StopParsingTicker();
            SettingsActivity.SaveAdditionalDirectorySettingsToSharedPreferences();
            base.OnPause();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Logger.Debug("Settings Created");
            base.OnCreate(savedInstanceState);
            SeekerState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.settings_layout);

            AndroidX.AppCompat.Widget.Toolbar myToolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.setting_toolbar);
            myToolbar.Title = this.GetString(Resource.String.settings);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            SetUpSettingsRecyclerView(savedInstanceState);
        }



        private const string DefaultDocumentsUri = "content://com.android.externalstorage.documents/tree/primary%3ADocuments";

        private void ExportClientData()
        {
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionCreateDocument);
            intent.SetType("application/xml");
            intent.PutExtra(Android.Content.Intent.ExtraTitle, "seeker_data.xml");
            intent.AddCategory(Android.Content.Intent.CategoryOpenable);
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                intent.PutExtra(Android.Provider.DocumentsContract.ExtraInitialUri, Android.Net.Uri.Parse(DefaultDocumentsUri));
            }
            this.StartActivityForResult(intent, SAVE_SEEKER_SETTINGS);
        }

        private void ClearAllFolders()
        {
            UploadDirectoryManager.UploadDirectories.Clear();
            UploadDirectoryManager.SaveToSharedPreferences(SeekerState.SharedPreferences);
            SharedFileService.ClearFileCache();
        }

        //private static AndroidX.AppCompat.App.AlertDialog configSmartFilters = null;
        private void ConfigSmartFilters()
        {
            Logger.InfoFirebase("ConfigSmartFilters");
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
            builder.SetTitle(Resource.String.ConfigureSmartFilters);
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.smart_filter_config_layout, (ViewGroup)this.FindViewById(Android.Resource.Id.Content), false);
            // Set up the input
            RecyclerView recyclerViewFiltersConfig = (RecyclerView)viewInflated.FindViewById<RecyclerView>(Resource.Id.recyclerViewFiltersConfig);
            builder.SetView(viewInflated);

            RecyclerListAdapter adapter = new RecyclerListAdapter(this, null, PreferencesState.SmartFilterOptions.GetAdapterItems());

            recyclerViewFiltersConfig.HasFixedSize = (true);
            recyclerViewFiltersConfig.SetAdapter(adapter);
            recyclerViewFiltersConfig.SetLayoutManager(new LinearLayoutManager(this));

            ItemTouchHelper.Callback callback = new DragDropItemTouchHelper(adapter);
            var mItemTouchHelper = new ItemTouchHelper(callback);
            mItemTouchHelper.AttachToRecyclerView(recyclerViewFiltersConfig);
            adapter.ItemTouchHelper = mItemTouchHelper;

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                PreferencesState.SmartFilterOptions.FromAdapterItems(adapter.GetAdapterItems());
                SeekerApplication.SaveSmartFilterState();
                _settingsAdapter?.NotifyRowChanged("search.smart_filter_configure");
            });

            EventHandler<DialogClickEventArgs> cancelHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
            });

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.cancel, cancelHandler);

            AndroidX.AppCompat.App.AlertDialog diag = builder.Create();
            diag.Show();

        }

        private void ImportData()
        {
            if (!PreferencesState.CurrentlyLoggedIn || !SeekerState.SoulseekClient.State.HasFlag(Soulseek.SoulseekClientStates.LoggedIn))
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.MustBeLoggedInToImport), ToastLength.Long);
                return;
            }
            Intent intent = new Intent(this, typeof(ImportWizardActivity));
            StartActivity(intent);
        }

        private void ClearRecentUserHistory()
        {
            //set to just the added users....
            int count = CommonState.UserList?.Count ?? 0;
            if (count > 0)
            {
                lock (CommonState.UserList)
                {
                    UserMetadataService.RecentUsersManager.SetRecentUserList(CommonState.UserList.Select(uli => uli.Username).ToList());
                }
            }
            else
            {
                UserMetadataService.RecentUsersManager.SetRecentUserList(new List<string>());
            }
            SeekerApplication.SaveRecentUsers();
        }

        private void RescanShares()
        {
            //for rescan=true, we use the previous parse to get metadata if there is a match...
            //so that we do not have to read the file again to get things like bitrate, samples, etc.
            //if the presentable name is in the last parse, and the size matches, then use those attributes we previously had to read the file to get..
            Rescan(null, -1, false, true);
        }

        private static string GetFriendlyDownloadDirectoryName()
        {
            if (StorageState.RootDocumentFile == null)            
            {
                if (PlatformInfo.UseLegacyStorage())
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
                return StorageState.RootDocumentFile.Uri.LastPathSegment;
            }
        }

        public static bool UseIncompleteManualFolder()
        {
            return (PreferencesState.OverrideDefaultIncompleteLocations && StorageState.RootIncompleteDocumentFile != null);
        }

        private static string GetFriendlyIncompleteDirectoryName()
        {
            if (PreferencesState.MemoryBackedDownload)
            {
                return SeekerApplication.GetString(Resource.String.NotInUse);
            }
            if (PreferencesState.OverrideDefaultIncompleteLocations && StorageState.RootIncompleteDocumentFile != null) //if doc file is null that means we could not write to it.
            {
                return StorageState.RootIncompleteDocumentFile.Uri.LastPathSegment;
            }
            else
            {
                if (!PreferencesState.CreateCompleteAndIncompleteFolders)
                {
                    return SeekerApplication.GetString(Resource.String.AppLocalStorage);
                }
                //if not override then its whatever the download directory is...
                if (StorageState.RootDocumentFile == null)                
                {
                    if (PlatformInfo.UseLegacyStorage())
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
                    return StorageState.RootDocumentFile.Uri.LastPathSegment;
                }
            }
        }




        private void CheckPriv()
        {
            PrivilegesManager.Instance.GetPrivilegesAPI(true);
        }

        private void GetPriv()
        {
            if (SessionService.Instance.IsNotLoggedIn())
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_in_to_get_privileges), ToastLength.Long);
                return;
            }
            //note: it seems that the Uri.Encode is not strictly necessary.  that is both "dog gone it" and "dog%20gone%20it" work just fine...
            Android.Net.Uri uri = Android.Net.Uri.Parse("https://www.slsknet.org/userlogin.php?username=" + Android.Net.Uri.Encode(PreferencesState.Username)); // missing 'http://' will cause crash.
            CommonHelpers.ViewUri(uri, this);
        }

        private void EditUserInfo()
        {
            Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(EditUserInfoActivity));
            this.StartActivity(intent);
        }

        private void ChangePassword()
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_in_to_change_password), ToastLength.Short);
                return;
            }

            // show dialog
            Logger.InfoFirebase("ChangePasswordDialog" + this.IsFinishing + this.IsDestroyed);

            void OkayAction(object sender, string textInput)
            {
                SessionService.Instance.RunWithReconnect(() => CommonHelpers.ChangePasswordLogic(textInput));
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    UiHelpers._dialogInstance?.Dismiss(); // todo: why?
                }
            }

            UiHelpers.ShowSimpleDialog(
                this,
                Resource.Layout.edit_text_password_dialog_content,
                this.Resources.GetString(Resource.String.change_password),
                OkayAction,
                this.Resources.GetString(Resource.String.okay),
                null,
                this.Resources.GetString(Resource.String.new_password),
                this.Resources.GetString(Resource.String.cancel),
                this.Resources.GetString(Resource.String.cannot_be_empty),
                true);
        }



        private void CheckStatus()
        {
            Android.Net.Uri uri = Android.Net.Uri.Parse("http://www.slsknet.org/porttest.php?port=" + PreferencesState.ListenerPort); // missing 'http://' will cause crashed. //an https for this link does not exist
            CommonHelpers.ViewUri(uri, this);
        }
        public void ClearIncompleteFolder()
        {
            List<string> doNotDelete = TransferItems.TransferItemManagerDL.GetInUseIncompleteFolderNames();

            bool useDownloadDir = false;
            if (PreferencesState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
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
            if (PlatformInfo.UseLegacyStorage() && (StorageState.RootDocumentFile == null && useDownloadDir))
            {
                string rootdir = string.Empty;
                //if (StorageState.RootDocumentFile==null)
                //{
                rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //}
                //else
                //{
                //    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //    rootdir = StorageState.RootDocumentFile.Uri.Path; //returns junk...
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
                    if (StorageState.RootDocumentFile == null)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.ErrorDownloadDirNotProperlySet), ToastLength.Long);
                        return;
                    }
                    rootdir = StorageState.RootDocumentFile;
                    Logger.Debug("using download dir" + rootdir.Uri.LastPathSegment);
                }
                else if (useTempDir)
                {
                    Java.IO.File appPrivateExternal = SeekerState.ActiveActivityRef.GetExternalFilesDir(null);
                    rootdir = DocumentFile.FromFile(appPrivateExternal);
                    Logger.Debug("using temp incomplete dir");
                }
                else if (useCustomDir)
                {
                    rootdir = StorageState.RootIncompleteDocumentFile;
                    Logger.Debug("using custom incomplete dir" + rootdir.Uri.LastPathSegment);
                }

                folderExists = CleanIncompleteFolder(rootdir.FindFile("Soulseek Incomplete"), doNotDelete, out folderCount);
            }

            if (!folderExists)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.IncompleteFolderEmpty), ToastLength.Long);
            }
            else if (folderExists && folderCount == 0)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.NoEligibleToClear), ToastLength.Long);
            }
            else
            {
                string plural = String.Empty;
                if (folderCount > 1)
                {
                    plural = "s";
                }
                SeekerApplication.Toaster.ShowToast($"Cleared {folderCount} folder" + plural, ToastLength.Long);
            }
        }

        public bool CleanIncompleteFolder(DocumentFile incompleteDirectory, List<string> incompleteFoldersToNotDelete, out int folderCount)
        {
            folderCount = 0;
            if (incompleteDirectory == null || !incompleteDirectory.Exists())
            {
                return false;
            }
            else
            {
                foreach (DocumentFile f in incompleteDirectory.ListFiles())
                {
                    // we dont create files at the root level other than .nomedia which stays.
                    if (f.IsDirectory)
                    {
                        if (!incompleteFoldersToNotDelete.Contains(f.Name))
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
            if (!folder.Delete())
            {
                foreach (DocumentFile f in folder.ListFiles())
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

        private static bool HasManageStoragePermission(Context context)
        {
            bool hasExternalStoragePermissions = false;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                hasExternalStoragePermissions = Android.OS.Environment.IsExternalStorageManager;
            }
            else
            {
                hasExternalStoragePermissions = ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.ManageExternalStorage) != Android.Content.PM.Permission.Denied;
            }
            return hasExternalStoragePermissions;
        }

        private void ForceFilesystemPermission()
        {
            bool hasExternalStoragePermissions = HasManageStoragePermission(this);

            if (hasExternalStoragePermissions)
            {
                SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.permission_already_successfully_granted), ToastLength.Long);
            }
            else
            {
                Intent allFilesPermission = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                Android.Net.Uri packageUri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                allFilesPermission.SetData(packageUri);
                this.StartActivityForResult(allFilesPermission, FORCE_REQUEST_STORAGE_MANAGER);
            }
        }

        private void BrowseSelf()
        {
            BrowseSelf(false, false);
        }

        private void BrowseSelf(bool forcePublic, bool forceFriend)
        {
            if (!PreferencesState.SharingOn || SharedFileService.SharedFileCache == null || UploadDirectoryManager.UploadDirectories.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.not_sharing), ToastLength.Short);
                return;
            }
            if (SharedFileService.ParseStatus.IsParsing)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.WaitForParsing), ToastLength.Short);
                return;
            }
            if (!SharedFileService.SharedFileCache.SuccessfullyInitialized || SharedFileService.SharedFileCache.GetBrowseResponseForUser(PreferencesState.Username) == null)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_parse_shares_post), ToastLength.Short);
                return;
            }
            string errorMsgToToast = string.Empty;

            Soulseek.BrowseResponse browseResponseToShow = null;
            if (forcePublic)
            {
                browseResponseToShow = SharedFileService.SharedFileCache.GetBrowseResponseForUser(null);
            }
            else if (forceFriend)
            {
                browseResponseToShow = SharedFileService.SharedFileCache.GetBrowseResponseForUser(null, true);
            }
            else
            {
                browseResponseToShow = SharedFileService.SharedFileCache.GetBrowseResponseForUser(PreferencesState.Username);
            }

            TreeNode<Soulseek.Directory> tree = BrowseService.CreateTree(browseResponseToShow, false, null, null, PreferencesState.Username, out errorMsgToToast);
            if (errorMsgToToast != null && errorMsgToToast != string.Empty)
            {
                SeekerApplication.Toaster.ShowToast(errorMsgToToast, ToastLength.Short);
                return;
            }
            if (tree != null)
            {
                BrowseService.OnBrowseResponseReceived(SharedFileService.SharedFileCache.GetBrowseResponseForUser(PreferencesState.Username), tree, PreferencesState.Username, null);
            }

            Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop);
            intent.PutExtra(MainActivity.GoToBrowseSelfExtra, true);
            this.StartActivity(intent);
        }

        private void ToggleStartupService()
        {
            if (ServiceLifecycle.IsStartUpServiceCurrentlyRunning)
            {
                Intent seekerKeepAliveService = new Intent(this, typeof(SeekerKeepAliveService));
                this.StopService(seekerKeepAliveService);
                ServiceLifecycle.IsStartUpServiceCurrentlyRunning = false;
            }
            else
            {
                Intent seekerKeepAliveService = new Intent(this, typeof(SeekerKeepAliveService));
                this.StartService(seekerKeepAliveService);
                ServiceLifecycle.IsStartUpServiceCurrentlyRunning = true;
            }
        }

        private void OnCloseClick(object sender, DialogClickEventArgs e)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            return true;
        }

        private void RemoveUploadDirFolder(UploadDirectoryEntry uploadDirEntry)
        {
            if (UploadDirectoryManager.UploadDirectories.Count == 1)
            {
                this.ClearAllFolders(); //since now we have 0 this will just properly clear everything.
                RefreshModernSharingRows(false);
            }
            else
            {
                UploadDirectoryManager.UploadDirectories.Remove(uploadDirEntry);
                RefreshModernSharingRows(false);
                Rescan(null, -1, UploadDirectoryManager.AreAnyFromLegacy(), false);
            }
        }





        private void ClearHistory()
        {
            PreferencesManager.ClearSearchHistory();
            SearchFragment.RaiseSearchHistoryCleared();
        }

        private void ChangeDownloadDirectory()
        {
            ShowDirSettings(PreferencesState.SaveDataDirectoryUri, DirectoryType.Download);
        }

        private bool needsMediaStorePermission()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return AndroidX.Core.Content.ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadMediaAudio) == Android.Content.PM.Permission.Denied;
            }
            else
            {
                return AndroidX.Core.Content.ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadExternalStorage) == Android.Content.PM.Permission.Denied;
            }
        }

        private void requestMediaStorePermission()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                AndroidX.Core.App.ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.ReadMediaAudio }, READ_EXTERNAL_FOR_MEDIA_STORE);
            }
            else
            {
                AndroidX.Core.App.ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.ReadExternalStorage }, READ_EXTERNAL_FOR_MEDIA_STORE);
            }
        }

        private void AddUploadDirectory()
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

        private void ChangeIncompleteDirectory()
        {
            ShowDirSettings(PreferencesState.ManualIncompleteDataDirectoryUri, DirectoryType.Incomplete);
        }

        private void UseInternalFilePicker(int requestCode)
        {
            //Create FolderOpenDialog
            SimpleFileDialog fileDialog = new SimpleFileDialog(this, SimpleFileDialog.FileSelectionMode.FolderChoose);
            fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath).ContinueWith(
                (Task<string> t) =>
                {
                    if (t.Result == null || t.Result == string.Empty)
                    {
                        return;
                    }
                    else
                    {
                        Android.Net.Uri uri = Android.Net.Uri.FromFile(new Java.IO.File(t.Result));
                        DocumentFile f = DocumentFile.FromFile(new Java.IO.File(t.Result)); //from tree uri not added til 21 also.  from single uri returns a f.Exists=false file.
                        if (f == null)
                        {
                            Logger.Firebase("legacy f is null");
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_reading_dir), ToastLength.Long);
                            return;
                        }
                        else if (!f.Exists())
                        {
                            Logger.Firebase("legacy f does not exist");
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_reading_dir), ToastLength.Long);
                            return;
                        }
                        else if (!f.IsDirectory)
                        {
                            Logger.Firebase("legacy NOT A DIRECTORY");
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_not_a_dir), ToastLength.Long);
                            return;
                        }

                        if (requestCode == CHANGE_WRITE_EXTERNAL_LEGACY)
                        {
                            this.SuccessfulWriteExternalLegacyCallback(uri, true);
                        }
                        else if (requestCode == UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY)
                        {
                            this.Rescan(uri, requestCode, true);
                        }
                        else if (requestCode == CHANGE_INCOMPLETE_EXTERNAL_LEGACY)
                        {
                            this.SuccessfulIncompleteExternalLegacyCallback(uri, true);
                        }
                    }


                });
        }



        private void ShowDirSettings(string startingDirectory, DirectoryType directoryType, bool errorReselectCase = false)
        {
            int requestCode = -1;
            if (PlatformInfo.UseLegacyStorage())
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
                    if (errorReselectCase)
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
                    if (e.Message.Contains(SimpleHelpers.NoDocumentOpenTreeToHandle))
                    {
                        FallbackFileSelectionEntry(requestCode);
                    }
                    else
                    {
                        Logger.Firebase("showDirSettings: " + e.Message + e.StackTrace);
                        throw;
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
                else if (directoryType == DirectoryType.Upload)
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
                else if (directoryType == DirectoryType.Incomplete)
                {
                    requestCode = CHANGE_INCOMPLETE_EXTERNAL;
                }
                try
                {
                    this.StartActivityForResult(intent, requestCode);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains(SimpleHelpers.NoDocumentOpenTreeToHandle))
                    {
                        FallbackFileSelectionEntry(requestCode);
                    }
                    else
                    {
                        Logger.Firebase("showDirSettings: " + e.Message + e.StackTrace);
                        throw;
                    }
                }
            }
        }

        private int ConvertRequestCodeIntoLegacyVersion(int requestCodeNotLegacy)
        {
            switch (requestCodeNotLegacy)
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
            if (PlatformInfo.RequiresEitherOpenDocumentTreeOrManageAllFiles())
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

            if (PlatformInfo.RequiresEitherOpenDocumentTreeOrManageAllFiles() && hasManageAllFilesManisfestPermission && !Android.OS.Environment.IsExternalStorageManager) //this is "step 1"
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
                if (PlatformInfo.RequiresEitherOpenDocumentTreeOrManageAllFiles() && !hasManageAllFilesManisfestPermission)
                {
                    UiHelpers.ShowSimpleAlertDialog(this, Resource.String.error_no_file_manager_dir_manage_storage, Resource.String.okay);
                }
                else
                {
                    SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.error_no_file_manager_dir), ToastLength.Long);
                }
            }
        }



        private void SuccessfulWriteExternalLegacyCallback(Android.Net.Uri uri, bool fromLegacyPicker = false)
        {
            this.RunOnUiThread(new Action(() =>
            {
                StorageState.SetRootDownloadDirectory(this, uri, isFromTree: !fromLegacyPicker, raiseUpdatedEvent: true);
                SeekerApplication.Toaster.ShowToast(string.Format(this.GetString(Resource.String.successfully_changed_dl_dir), uri.Path), ToastLength.Long);
            }));
        }

        public static bool UseTempDirectory()
        {
            return !UseIncompleteManualFolder() && !PreferencesState.CreateCompleteAndIncompleteFolders;
        }

        private void SuccessfulIncompleteExternalLegacyCallback(Android.Net.Uri uri, bool fromLegacyPicker = false)
        {
            this.RunOnUiThread(new Action(() =>
            {
                StorageState.SetRootIncompleteDirectory(this, uri, isFromTree: !fromLegacyPicker, raiseUpdatedEvent: true);
                SeekerApplication.Toaster.ShowToast(string.Format(this.GetString(Resource.String.successfully_changed_incomplete_dir), uri.Path), ToastLength.Long);
            }));
        }

        public void ShowDialogForUploadDir(UploadDirectoryEntry uploadInfo)
        {
            if (uploadInfo.Info.HasError())
            {
                ShowUploadDirectoryErrorDialog(uploadInfo);
            }
            else
            {
                ShowUploadDirectoryOptionsDialog(uploadInfo);
            }
        }
        private static UploadDirectoryEntry UploadDirToReplaceOnReselect = null;
        public void ShowUploadDirectoryErrorDialog(UploadDirectoryEntry uploadInfo)
        {
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
            builder.SetTitle(Resource.String.FolderError);
            string diagMessage = SeekerApplication.GetString(Resource.String.ErrorForFolder) + uploadInfo.GetLastPathSegment() + System.Environment.NewLine + UploadDirectoryManager.GetErrorString(uploadInfo.Info.ErrorState) + System.Environment.NewLine;
            var diag = builder.SetMessage(diagMessage)
                .SetNegativeButton(Resource.String.RemoveFolder, (object sender, DialogClickEventArgs e) =>
                { //puts it slightly right
                    this.RemoveUploadDirFolder(uploadInfo);
                    this.OnCloseClick(sender, e);
                })
                .SetPositiveButton(Resource.String.Reselect, (object sender, DialogClickEventArgs e) =>
                { //puts it rightmost
                    UploadDirToReplaceOnReselect = uploadInfo;
                    this.ShowDirSettings(uploadInfo.Info.UploadDataDirectoryUri, DirectoryType.Upload, true);
                    this.OnCloseClick(sender, e);
                })
                .SetNeutralButton(Resource.String.cancel, OnCloseClick) //puts it leftmost
                .Create();
            diag.Show();
        }

        public void ShowUploadDirectoryOptionsDialog(UploadDirectoryEntry uploadDirEntry)
        {
            bool overrideEnabled = !string.IsNullOrEmpty(uploadDirEntry.Info.DisplayNameOverride);
            string initialName = overrideEnabled ? uploadDirEntry.Info.DisplayNameOverride : string.Empty;

            Seeker.Settings.Rows.UploadFolderOptionsBottomSheet.Show(
                this,
                uploadDirEntry.GetLastPathSegment(),
                uploadDirEntry.Info.IsLocked,
                uploadDirEntry.Info.IsHidden,
                overrideEnabled,
                initialName,
                result => ApplyUploadDirectoryOptions(uploadDirEntry, result));
        }

        private void ApplyUploadDirectoryOptions(UploadDirectoryEntry uploadDirEntry, Seeker.Settings.Rows.UploadFolderOptionsBottomSheet.Result result)
        {
            //any changed?
            bool hiddenChanged = uploadDirEntry.Info.IsHidden != result.Hidden;
            bool lockedChanged = uploadDirEntry.Info.IsLocked != result.Locked;
            bool overrideNameChanged =
                (string.IsNullOrEmpty(uploadDirEntry.Info.DisplayNameOverride) && result.OverrideName && !string.IsNullOrEmpty(result.CustomName)) ||
                ((!result.OverrideName || string.IsNullOrEmpty(result.CustomName)) && !string.IsNullOrEmpty(uploadDirEntry.Info.DisplayNameOverride)) ||
                (result.OverrideName && uploadDirEntry.Info.DisplayNameOverride != result.CustomName);

            uploadDirEntry.Info.IsHidden = result.Hidden;
            uploadDirEntry.Info.IsLocked = result.Locked;
            string displayNameOld = uploadDirEntry.Info.DisplayNameOverride;

            if (result.OverrideName && !string.IsNullOrEmpty(result.CustomName))
            {
                if (uploadDirEntry.Info.DisplayNameOverride != result.CustomName)
                {
                    //make sure that we CAN change it.
                    uploadDirEntry.Info.DisplayNameOverride = result.CustomName;
                    if (!UploadDirectoryManager.DoesNewDirectoryHaveUniqueRootName(uploadDirEntry, false))
                    {
                        uploadDirEntry.Info.DisplayNameOverride = displayNameOld;
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.CannotChangeNameNotUnique), ToastLength.Long);
                        overrideNameChanged = false; //we prevented it
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(uploadDirEntry.Info.DisplayNameOverride))
                {
                    //make sure that we CAN change it.
                    uploadDirEntry.Info.DisplayNameOverride = null;
                    if (!UploadDirectoryManager.DoesNewDirectoryHaveUniqueRootName(uploadDirEntry, false))
                    {
                        uploadDirEntry.Info.DisplayNameOverride = displayNameOld;
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.CannotChangeNameNotUnique), ToastLength.Long);
                        overrideNameChanged = false; //we prevented it
                    }
                }
            }

            RefreshModernSharingRows(false);
            if (hiddenChanged || lockedChanged || overrideNameChanged)
            {
                Logger.Debug("things changed re: folder options..");
                Rescan(null, -1, UploadDirectoryManager.AreAnyFromLegacy(), false);
            }
        }

        public static EventHandler<EventArgs> UploadDirectoryChanged;
        public static volatile bool MoreChangesHaveBeenMadeSoRescanWhenDone = false;
        public static volatile List<Android.Net.Uri> NewlyAddedUrisWeHaveToAddAfter = new List<Android.Net.Uri>();

        public void ParseDatabaseAndUpdateUI(Android.Net.Uri newlyAddedUriIfApplicable, int requestCode, bool fromLegacyPicker = false, bool rescanClicked = false, bool reselectCase = false)
        {

            if (rescanClicked)
            {
                if (SharedFileService.ParseStatus.IsParsing)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.AlreadyParsing), ToastLength.Long);
                    return;
                }
                if (UploadDirectoryManager.UploadDirectories.Count == 0)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.DirectoryNotSet), ToastLength.Long);
                    return;
                }
            }

            UploadDirectoryEntry newlyAddedDirectory = null;
            if (newlyAddedUriIfApplicable != null)
            {
                //RESELECT CASE
                if (reselectCase)
                {
                    newlyAddedDirectory = new UploadDirectoryEntry(new UploadDirectoryInfo(newlyAddedUriIfApplicable.ToString(), !fromLegacyPicker, UploadDirToReplaceOnReselect.Info.IsLocked, UploadDirToReplaceOnReselect.Info.IsHidden, UploadDirToReplaceOnReselect.Info.DisplayNameOverride));
                    newlyAddedDirectory.UploadDirectory = fromLegacyPicker ? DocumentFile.FromFile(new Java.IO.File(newlyAddedUriIfApplicable.Path)) : DocumentFile.FromTreeUri(this, newlyAddedUriIfApplicable);
                    UploadDirectoryManager.UploadDirectories.Remove(UploadDirToReplaceOnReselect);
                }
                else
                {
                    newlyAddedDirectory = new UploadDirectoryEntry(new UploadDirectoryInfo(newlyAddedUriIfApplicable.ToString(), !fromLegacyPicker, false, false, null));
                    newlyAddedDirectory.UploadDirectory = fromLegacyPicker ? DocumentFile.FromFile(new Java.IO.File(newlyAddedUriIfApplicable.Path)) : DocumentFile.FromTreeUri(this, newlyAddedUriIfApplicable);
                }



                if (UploadDirectoryManager.UploadDirectories.Where(up => up.Info.UploadDataDirectoryUri == newlyAddedUriIfApplicable.ToString()).Count() != 0)
                {
                    //error!!
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.ErrorAlreadyAdded), ToastLength.Long);
                    return;
                    //throw new Exception("Directory is already added!");
                }

                UploadDirectoryManager.UploadDirectories.Add(newlyAddedDirectory);
            }

            UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
            if (UploadDirectoryManager.AreAllFailed())
            {
                throw new DirectoryAccessFailure("All Failed");
            }

            if (newlyAddedDirectory != null)
            {
                bool isUnqiue = UploadDirectoryManager.DoesNewDirectoryHaveUniqueRootName(newlyAddedDirectory, true);
                if (!isUnqiue)
                {
                    Logger.Debug("Root name was not unique. Updated it to be unique.");
                }
                UploadDirectoryChanged?.Invoke(null, new EventArgs());
            }


            if (SharedFileService.ParseStatus.IsParsing)
            {
                Logger.Debug("We are already parsing!!! so after this parse, lets parse again with our cached results to pick up our new changes");
                MoreChangesHaveBeenMadeSoRescanWhenDone = true;
                return;
            }

            try
            {
                Logger.Debug("Parsing now......");

                SharedFileService.SetParsing(true);
                int prevFiles = -1;
                bool success = false;
                if (rescanClicked && SharedFileService.SharedFileCache != null)
                {
                    prevFiles = SharedFileService.SharedFileCache.FileCount;
                }
                this.RunOnUiThread(new Action(() =>
                {
                    RefreshModernSharingRows(false); 
                    EnsureParsingTicker(); 
                }));
                try
                {

                    success = SharedFileService.InitializeDatabase(null, false, out string errorMessage);
                    if (!success)
                    {
                        throw new Exception("Failed to parse shared files: " + errorMessage);
                    }
                    SharedFileService.SetParsing(false);
                }
                catch (Exception e)
                {
                    SharedFileService.SetParsing(false);
                    //SeekerState.UploadDataDirectoryUri = null;
                    //SeekerState.UploadDataDirectoryUriIsFromTree = true;
                    SharedFileService.ClearLegacyParsedCacheResults();
                    SharedFileService.ClearParsedCacheResults(SeekerState.ActiveActivityRef);
                    SharingService.SetUnsetSharingBasedOnConditions(true);
                    if (!(e is DirectoryAccessFailure))
                    {
                        Logger.Firebase("error parsing: " + e.Message + "  " + e.StackTrace);
                    }
                    this.RunOnUiThread(new Action(() =>
                    {
                        RefreshModernSharingRows(false);
                        if (!(e is DirectoryAccessFailure))
                        {
                            SeekerApplication.Toaster.ShowToast(e.Message, ToastLength.Long);
                        }
                        else
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.FailedGettingAccess), ToastLength.Long); //TODO get error from UploadManager..
                        }

                    }));
                    UploadDirectoryChanged?.Invoke(null, new EventArgs());
                    return;
                }
                //SeekerState.UploadDataDirectoryUri = uri.ToString();
                //SeekerState.UploadDataDirectoryUriIsFromTree = !fromLegacyPicker;
                if ((UPLOAD_DIR_ADD_WRITE_EXTERNAL == requestCode || UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case == requestCode) && newlyAddedUriIfApplicable != null)
                {
                    this.ContentResolver.TakePersistableUriPermission(newlyAddedUriIfApplicable, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                }
                //setup soulseek client with handlers if all conditions met
                SharingService.SetUnsetSharingBasedOnConditions(true, true);
                this.RunOnUiThread(new Action(() =>
                {
                    RefreshModernSharingRows(false);
                    int dirs = SharedFileService.SharedFileCache.DirectoryCount; //TODO: nullref here... U318AA, LG G7 ThinQ, both android 10
                    int files = SharedFileService.SharedFileCache.FileCount;
                    string msg = string.Empty;
                    if (rescanClicked)
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
                    if (!string.IsNullOrEmpty(msg))
                    {
                        SeekerApplication.Toaster.ShowToast(msg, ToastLength.Long);
                    }
                }));
            }
            finally
            {
                SharedFileService.SetParsing(false);
                if (MoreChangesHaveBeenMadeSoRescanWhenDone)
                {
                    Logger.Debug("okay now lets pick up our new changes");
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
            Action parseDatabaseAndUpdateUiAction = new Action(() =>
            {
                try
                {
                    ParseDatabaseAndUpdateUI(newlyAddedUriIfApplicable, requestCode, fromLegacyPicker, rescanClicked, reselectCase);
                }
                catch (DirectoryAccessFailure)
                {
                    if (rescanClicked || reselectCase)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.SharedFolderIssuesAllFailed), ToastLength.Long);
                    }
                }
                catch (Exception ex)
                {
                    Logger.FirebaseError("Rescan Error", ex);
                    SeekerApplication.Toaster.ShowToast("Error Parsing Shared Files", ToastLength.Long); //TODO clean up error message
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
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.NoMediaStore), ToastLength.Short);
                    ShowDirSettings(null, DirectoryType.Upload);
                }
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            //if from manage external settings
            if (CHANGE_WRITE_EXTERNAL_LEGACY == requestCode - 32 || CHANGE_INCOMPLETE_EXTERNAL_LEGACY == requestCode - 32 || UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY == requestCode - 32)
            {
                if (SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
                {
                    //phase 2 - actually pick a file.
                    UseInternalFilePicker(requestCode - 32);
                }
                else
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.NoPermissionsForDir), ToastLength.Long);
                }
            }


            if (CHANGE_WRITE_EXTERNAL == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    this.RunOnUiThread(new Action(() =>
                    {
                        StorageState.SetRootDownloadDirectory(this, data.Data, isFromTree: true, raiseUpdatedEvent: true);
                        SeekerApplication.Toaster.ShowToast(string.Format(this.GetString(Resource.String.successfully_changed_dl_dir), data.Data), ToastLength.Long);
                    }));
                }
            }
            if (CHANGE_WRITE_EXTERNAL_LEGACY == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    SuccessfulWriteExternalLegacyCallback(data.Data);
                }
            }


            if (CHANGE_INCOMPLETE_EXTERNAL == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    this.RunOnUiThread(new Action(() =>
                    {
                        StorageState.SetRootIncompleteDirectory(this, data.Data, isFromTree: true, raiseUpdatedEvent: true);
                        SeekerApplication.Toaster.ShowToast(string.Format(this.GetString(Resource.String.successfully_changed_incomplete_dir), data.Data), ToastLength.Long);
                    }));
                }
            }
            if (CHANGE_INCOMPLETE_EXTERNAL_LEGACY == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    SuccessfulIncompleteExternalLegacyCallback(data.Data);
                }
            }


            if (UPLOAD_DIR_ADD_WRITE_EXTERNAL == requestCode ||
                UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY == requestCode ||
                UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY_Reselect_Case == requestCode ||
                UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case == requestCode)
            {
                if (resultCode != Result.Ok)
                {
                    return;
                }

                bool reselectCase = false;
                if (UPLOAD_DIR_ADD_WRITE_EXTERNAL_Reselect_Case == requestCode || UPLOAD_DIR_ADD_WRITE_EXTERNAL_LEGACY_Reselect_Case == requestCode)
                {
                    reselectCase = true;
                }
                //make sure you can parse the files before setting the directory..

                //this takes 5+ seconds in Debug mode (with 20-30 albums) which means that this MUST be done on a separate thread..
                Rescan(data.Data, requestCode, false, false, reselectCase);

            }

            if (SAVE_SEEKER_SETTINGS == requestCode)
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

                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.successfully_exported), ToastLength.Short);
                }
            }

            if (FORCE_REQUEST_STORAGE_MANAGER == requestCode)
            {
                bool hasPermision = HasManageStoragePermission(this);
                if (hasPermision)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.permission_successfully_granted), ToastLength.Short);
                }
                else
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.permission_failed), ToastLength.Short);
                }
            }
        }

        private SeekerImportExportData GetCurrentExportData()
        {
            var seekerImportExportData = new SeekerImportExportData();
            seekerImportExportData.Userlist = CommonState.UserList.Select(uli => uli.Username).ToList();
            seekerImportExportData.BanIgnoreList = CommonState.IgnoreUserList.Select(uli => uli.Username).ToList();
            seekerImportExportData.Wishlist = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget == SearchTarget.Wishlist).Select((pair1) => pair1.Value.LastSearchTerm).ToList();
            List<KeyValueEl> userNotes = new List<KeyValueEl>();
            foreach (KeyValuePair<string, string> pair in UserMetadataService.UserNotes)
            {
                userNotes.Add(new KeyValueEl() { Key = pair.Key, Value = pair.Value });
            }
            seekerImportExportData.UserNotes = userNotes;
            return seekerImportExportData;
        }

        public static void RestoreAdditionalDirectorySettingsFromSharedPreferences()
        {
            PreferencesManager.RestoreAdditionalDirectorySettings();
        }

        public static void SaveAdditionalDirectorySettingsToSharedPreferences()
        {
            PreferencesManager.SaveAdditionalDirectorySettings();
        }

        public static void SaveMaxConcurrentDownloadsSettings()
        {
            PreferencesManager.SaveMaxConcurrentDownloadsSettings(
                PreferencesState.LimitSimultaneousDownloads,
                PreferencesState.MaxSimultaneousLimit);
        }

        public static void SaveManualIncompleteDirToSharedPreferences()
        {
            PreferencesManager.SaveManualIncompleteDir();
        }

    }

}

