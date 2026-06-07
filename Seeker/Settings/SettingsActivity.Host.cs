using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
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

        private View _settingsSearchRow;
        private AndroidX.AppCompat.Widget.SearchView _settingsSearchView;
        private Seeker.Helpers.GenericOnBackPressedCallback _searchBackCallback;

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

            WireInlineSearch();

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

        protected override void OnDestroy()
        {
            StopParsingTicker();
            _settingsRecyclerView?.SetAdapter(null);
            base.OnDestroy();
        }


        private void WireInlineSearch()
        {
            _settingsSearchRow = FindViewById<View>(Resource.Id.settingsSearchRow);
            _settingsSearchView = FindViewById<AndroidX.AppCompat.Widget.SearchView>(Resource.Id.settingsSearchView);

            _settingsSearchView.QueryHint = GetString(Resource.String.search_settings);


            _settingsSearchView.QueryTextChange += (s, e) =>
            {
                _settingsAdapter?.Filter(e.NewText ?? string.Empty);
            };
            _settingsSearchView.QueryTextSubmit += (s, e) =>
            {
                _settingsSearchView.ClearFocus();
                e.Handled = true;
            };


            // Back closes the search row (instead of the activity) while it is open.
            _searchBackCallback = new Seeker.Helpers.GenericOnBackPressedCallback(false, cb => HideSearchRow());
            OnBackPressedDispatcher.AddCallback(_searchBackCallback);

            // Restore an in-progress search across rotation without popping the keyboard.
            if (!string.IsNullOrEmpty(_pendingSearchQuery))
            {
                _settingsSearchRow.Visibility = ViewStates.Visible;
                _settingsSearchView.SetQuery(_pendingSearchQuery, false);
                _settingsSearchView.ClearFocus();
                _searchBackCallback.Enabled = true;
            }
        }

        internal void ShowSearchRow()
        {
            if (_settingsSearchRow == null)
            {
                return;
            }
            _settingsSearchRow.Visibility = ViewStates.Visible;
            _settingsSearchView.Iconified = false;
            _settingsSearchView.RequestFocus();
            _searchBackCallback.Enabled = true;
        }

        private void HideSearchRow()
        {
            if (_settingsSearchRow == null)
            {
                return;
            }
            var imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
            imm?.HideSoftInputFromWindow(_settingsSearchView.WindowToken, 0);
            _settingsSearchView.SetQuery(string.Empty, false); // clears the filter via QueryTextChange
            _settingsSearchRow.Visibility = ViewStates.Gone;
            _searchBackCallback.Enabled = false;
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
                    RestoreModernSettingsDefaults();
                }))
                .Show();
        }

        private void RestoreModernSettingsDefaults()
        {
            bool prevListenerEnabled = PreferencesState.ListenerEnabled;
            bool prevLimitSimDownloads = PreferencesState.LimitSimultaneousDownloads;
            int prevMaxSimDownloads = PreferencesState.MaxSimultaneousLimit;

            PreferencesState.CreateCompleteAndIncompleteFolders = true;
            PreferencesState.CreateUsernameSubfolders = false;
            PreferencesState.NoSubfolderForSingle = false;
            PreferencesState.OverrideDefaultIncompleteLocations = false;
            PreferencesState.MemoryBackedDownload = false;
            PreferencesState.AutoClearCompleteDownloads = false;
            PreferencesState.AutoRetryBackOnline = true;

            PreferencesState.NumberSearchResults = Constants.DefaultSearchResults;
            PreferencesState.ShowSmartFilters = true;
            PreferencesState.SmartFilterStyle = SmartFilterStyle.Flat;
            PreferencesState.SmartFilterOptions = new PreferencesState.SmartFilterState
            {
                KeywordsEnabled = true,
                KeywordsOrder = 0,
                FileTypesEnabled = true,
                FileTypesOrder = 1,
                NumFilesEnabled = true,
                NumFilesOrder = 2,
            };
            PreferencesState.FreeUploadSlotsOnly = true;
            PreferencesState.HideLockedResultsInSearch = true;
            PreferencesState.HideLockedResultsInBrowse = true;
            PreferencesState.RememberSearchHistory = true;

            PreferencesState.StartServiceOnStartup = true;
            PreferencesState.NotifyOnFolderCompleted = true;
            PreferencesState.DisableDownloadToastNotification = true;
            PreferencesState.ShowRecentUsers = true;
            PreferencesState.AutoAwayOnInactivity = false;

            PreferencesState.DayNightMode = AndroidX.AppCompat.App.AppCompatDelegate.ModeNightFollowSystem;
            PreferencesState.DayModeVariant = DayThemeType.ClassicPurple;
            PreferencesState.NightModeVariant = NightThemeType.ClassicPurple;

            PreferencesState.SharingOn = false;
            PreferencesState.AllowUploadsOnMetered = true;
            PreferencesState.AutoClearCompleteUploads = false;

            PreferencesState.ListenerEnabled = true;
            PreferencesState.ListenerUPnpEnabled = true;
            PreferencesState.SpeedLimitDownloadOn = false;
            PreferencesState.SpeedLimitDownloadBytesSec = 4 * 1024 * 1024;
            PreferencesState.SpeedLimitDownloadIsPerTransfer = true;
            PreferencesState.SpeedLimitUploadOn = false;
            PreferencesState.SpeedLimitUploadBytesSec = 4 * 1024 * 1024;
            PreferencesState.SpeedLimitUploadIsPerTransfer = true;
            PreferencesState.LimitSimultaneousDownloads = false;
            PreferencesState.MaxSimultaneousLimit = 1;

            PreferencesState.LogDiagnostics = false;

            PreferencesManager.SaveAllModernSettings();

            // side effects
            AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = PreferencesState.DayNightMode;

            if (prevListenerEnabled != PreferencesState.ListenerEnabled)
            {
                Seeker.Services.SessionService.Instance.ReconfigureOptions(null, PreferencesState.ListenerEnabled, null);
            }
            if (PreferencesState.ListenerEnabled && PreferencesState.ListenerUPnpEnabled)
            {
                UPnP.UPnpManager.Instance.Feedback = true;
                UPnP.UPnpManager.Instance.SearchAndSetMappingIfRequired();
            }

            bool concurrentChanged = prevLimitSimDownloads != PreferencesState.LimitSimultaneousDownloads
                                  || prevMaxSimDownloads != PreferencesState.MaxSimultaneousLimit;
            if (concurrentChanged)
            {
                SeekerApplication.Toaster.ShowToastShort(
                    this.GetString(Resource.String.takes_effect_on_next_startup));
            }

            _settingsAdapter?.NotifyDataSetChanged();
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
        public void UpdateSimulataneousDownloadsLimit(bool enabled, int limit)
        {
            bool changed = PreferencesState.LimitSimultaneousDownloads != enabled
                        || (enabled && PreferencesState.MaxSimultaneousLimit != limit);
            PreferencesState.LimitSimultaneousDownloads = enabled;
            PreferencesState.MaxSimultaneousLimit = limit;
            PreferencesManager.SaveMaxConcurrentDownloadsSettings(enabled, limit);
            if (changed)
            {
                SeekerApplication.Toaster.ShowToastShort(
                    this.GetString(Resource.String.takes_effect_on_next_startup));
            }
        }

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
