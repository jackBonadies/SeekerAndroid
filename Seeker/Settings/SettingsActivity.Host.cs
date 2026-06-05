using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using Common;
using Seeker.Managers;
using Seeker.Settings.Rows;
using System;

namespace Seeker
{
    public partial class SettingsActivity
    {
        private const string STATE_SEARCH_QUERY = "settings.search.query";

        private SettingsAdapter _settingsAdapter;
        private RecyclerView _settingsRecyclerView;
        private string _pendingSearchQuery;

        // Drives the live "Currently parsing (X files)" count while a share is parsing. The parse
        // emits no periodic event, so we self-tick on the main looper until parsing finishes.
        private Android.OS.Handler _parsingTickHandler;
        private bool _parsingTickScheduled;

        private void SetUpSettingsRecyclerView(Bundle savedInstanceState)
        {
            _pendingSearchQuery = savedInstanceState?.GetString(STATE_SEARCH_QUERY);

            _settingsRecyclerView = FindViewById<RecyclerView>(Resource.Id.settingsRecyclerView);
            _settingsRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _settingsRecyclerView.SetItemAnimator(new DefaultItemAnimator());
            _settingsRecyclerView.AddItemDecoration(new SettingsDividerDecoration(this));
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(_settingsRecyclerView, new Seeker.Helpers.BottomOnlyInsetsListener());

            var rows = SettingsCatalog.Build(this);
            _settingsAdapter = new SettingsAdapter(this, rows);
            _settingsRecyclerView.SetAdapter(_settingsAdapter);

            if (!string.IsNullOrEmpty(_pendingSearchQuery))
            {
                _settingsAdapter.Filter(_pendingSearchQuery);
            }

            if (Intent != null &&
                Intent.GetIntExtra(SettingsActivity.SCROLL_TO_SHARING_SECTION_STRING, -1) != -1)
            {
                var lm = _settingsRecyclerView.GetLayoutManager() as LinearLayoutManager;
                int idx = _settingsAdapter.IndexOfRow("h.sharing");
                if (lm != null && idx >= 0)
                {
                    _settingsRecyclerView.Post(new Action(() => lm.ScrollToPositionWithOffset(idx, 0)));
                }
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            if (_settingsAdapter != null && !string.IsNullOrEmpty(_settingsAdapter.CurrentQuery))
            {
                outState.PutString(STATE_SEARCH_QUERY, _settingsAdapter.CurrentQuery);
            }
            base.OnSaveInstanceState(outState);
        }


        private void WireSearchMenu(IMenu menu)
        {
            var searchItem = menu?.FindItem(Resource.Id.action_search_settings);
            if (searchItem == null) return;

            var searchView = searchItem.ActionView.JavaCast<AndroidX.AppCompat.Widget.SearchView>();
            if (searchView == null) return;

            searchView.QueryHint = GetString(Resource.String.search_settings);

            if (!string.IsNullOrEmpty(_pendingSearchQuery))
            {
                searchItem.ExpandActionView();
                searchView.SetQuery(_pendingSearchQuery, false);
                searchView.ClearFocus();
            }

            searchView.QueryTextChange += (s, e) =>
            {
                _settingsAdapter?.Filter(e.NewText ?? string.Empty);
            };
            searchView.QueryTextSubmit += (s, e) =>
            {
                _settingsAdapter?.Filter(e.NewText ?? string.Empty);
                e.Handled = true;
            };
        }


        Activity ISettingsHost.Activity => this;
        SettingsAdapter ISettingsHost.Adapter => _settingsAdapter;

        void ISettingsHost.NotifyRowChanged(string rowId) => _settingsAdapter?.NotifyRowChanged(rowId);
        void ISettingsHost.NotifyParentToggled(string parentId) => _settingsAdapter?.NotifyParentToggled(parentId);

        internal void NotifyRowChanged(string rowId) => _settingsAdapter?.NotifyRowChanged(rowId);

        void ISettingsHost.LaunchDownloadFolderPicker() => ChangeDownloadDirectory();
        void ISettingsHost.LaunchIncompleteFolderPicker() => ChangeIncompleteDirectory();
        void ISettingsHost.LaunchAddSharedFolderPicker() => AddUploadDirectory();

        void ISettingsHost.LaunchChangePassword() => ChangePassword();
        void ISettingsHost.LaunchEditUserInfo() => EditUserInfo();
        void ISettingsHost.LaunchImportClientData() => ImportData();
        void ISettingsHost.LaunchExportClientData() => ExportClientData();
        void ISettingsHost.LaunchRestoreDefaults()
        {
            new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this)
                .SetTitle(Resource.String.restore_default_settings)
                .SetMessage("This will reset settings to their defaults. Continue?")
                .SetNegativeButton(Android.Resource.String.Cancel, (System.EventHandler<Android.Content.DialogClickEventArgs>)((s, e) => { }))
                .SetPositiveButton(Android.Resource.String.Ok, (System.EventHandler<Android.Content.DialogClickEventArgs>)((s, e) =>
                {
                    PreferencesState.NumberSearchResults = Constants.DefaultSearchResults;
                    PreferencesState.AutoClearCompleteDownloads = false;
                    PreferencesState.AutoClearCompleteUploads = false;
                    PreferencesState.RememberSearchHistory = true;
                    PreferencesState.ShowRecentUsers = true;
                    PreferencesState.SharingOn = false;
                    PreferencesState.FreeUploadSlotsOnly = true;
                    PreferencesState.DisableDownloadToastNotification = true;
                    PreferencesState.MemoryBackedDownload = false;
                    PreferencesState.DayNightMode = AndroidX.AppCompat.App.AppCompatDelegate.ModeNightFollowSystem;
                    PreferencesState.HideLockedResultsInBrowse = true;
                    PreferencesState.HideLockedResultsInSearch = true;
                    Seeker.PreferencesManager.SaveOnPauseState(null);
                    _settingsAdapter?.NotifyDataSetChanged();
                }))
                .Show();
        }
        void ISettingsHost.LaunchForceFilesystemPermission() => ForceFilesystemPermission();

        void ISettingsHost.CheckPrivileges() => CheckPriv();
        void ISettingsHost.GetPrivileges() => GetPriv();
        void ISettingsHost.CheckPortStatus() => CheckStatus();
        void ISettingsHost.RescanShares() => RescanShares();
        void ISettingsHost.BrowseSelf() => BrowseSelf();
        void ISettingsHost.ClearIncompleteFolder() => ClearIncompleteFolder();
        void ISettingsHost.ClearSearchHistory() => ClearHistory();
        void ISettingsHost.ClearRecentUsers() => ClearRecentUserHistory();
        void ISettingsHost.StartStopBackgroundService() => ToggleStartupService();

        void ISettingsHost.ConfigureSmartFilters() => ConfigSmartFilters();

        void ISettingsHost.EditSharedFolder(UploadDirectoryEntry entry) => ShowDialogForUploadDir(entry);

        void ISettingsHost.RemoveSharedFolder(UploadDirectoryEntry entry)
        {
            RemoveUploadDirFolder(entry); // also calls RefreshModernSharingRows() internally
        }

        void ISettingsHost.RefreshSharingFolders() => RefreshModernSharingRows();

        internal void RefreshModernSharingRows(bool suppressAnimation = true)
        {
            if (_settingsAdapter == null)
            {
                return;
            }
            _settingsAdapter.RebuildRows(SettingsCatalog.Build(this));
            _settingsAdapter.NotifySharingRowsChanged(suppressAnimation);
        }

        internal void EnsureParsingTicker()
        {
            if (_parsingTickScheduled || _settingsAdapter == null)
            {
                return;
            }
            if (!Seeker.Services.SharedFileService.ParseStatus.IsParsing)
            {
                return;
            }
            _parsingTickScheduled = true;
            _parsingTickHandler ??= new Android.OS.Handler(Android.OS.Looper.MainLooper);

            Action tick = null;
            tick = () =>
            {
                if (Seeker.Services.SharedFileService.ParseStatus.IsParsing)
                {
                    _settingsAdapter?.NotifySharingRowsChanged(true);
                    _parsingTickHandler.PostDelayed(tick, 500);
                }
                else
                {
                    _parsingTickScheduled = false;
                    RefreshModernSharingRows(false);
                }
            };
            _parsingTickHandler.PostDelayed(tick, 250);
        }

        private void StopParsingTicker()
        {
            _parsingTickHandler?.RemoveCallbacksAndMessages(null);
            _parsingTickScheduled = false;
        }
    }
}
