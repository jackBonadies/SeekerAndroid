using Android.Content;
using Common;

namespace Seeker
{
    public static class PreferencesManager
    {
        /// <summary>
        /// Restores all persisted preferences from the given shared preferences.
        /// Side-effect restores (UploadDirectoryManager, SearchTabHelper, TransfersFragment, etc.)
        /// are NOT handled here and must be called separately from the platform layer.
        /// </summary>
        public static void RestoreAll(ISharedPreferences prefs)
        {
            if (prefs == null)
                return;

            RestoreAccountState(prefs);
            RestoreUIPreferences(prefs);
            RestoreTransferSettings(prefs);
            RestoreSpeedLimitSettings(prefs);
            RestoreSearchSettings(prefs);
            RestoreSocialSettings(prefs);
            RestoreMiscSettings(prefs);
        }

        public static void RestoreAccountState(ISharedPreferences prefs)
        {
            PreferencesState.CurrentlyLoggedIn = prefs.GetBoolean(KeyConsts.M_CurrentlyLoggedIn, false);
            PreferencesState.Username = prefs.GetString(KeyConsts.M_Username, "");
            PreferencesState.Password = prefs.GetString(KeyConsts.M_Password, "");
        }

        public static void RestoreUIPreferences(ISharedPreferences prefs)
        {
            PreferencesState.DayNightMode = prefs.GetInt(KeyConsts.M_DayNightMode, -1); // ModeNightFollowSystem
            PreferencesState.Language = prefs.GetString(KeyConsts.M_Lanuage, PreferencesState.FieldLangAuto);
            PreferencesState.LegacyLanguageMigrated = prefs.GetBoolean(KeyConsts.M_LegacyLanguageMigrated, false);
            PreferencesState.NightModeVarient = (NightThemeType)(prefs.GetInt(KeyConsts.M_NightVarient, (int)NightThemeType.ClassicPurple));
            PreferencesState.DayModeVarient = (DayThemeType)(prefs.GetInt(KeyConsts.M_DayVarient, (int)DayThemeType.ClassicPurple));
            PreferencesState.ShowSmartFilters = prefs.GetBoolean(KeyConsts.M_ShowSmartFilters, false);
            RestoreSmartFilterState(prefs);
        }

        public static void RestoreTransferSettings(ISharedPreferences prefs)
        {
            PreferencesState.AutoClearCompleteDownloads = prefs.GetBoolean(KeyConsts.M_AutoClearComplete, false);
            PreferencesState.AutoClearCompleteUploads = prefs.GetBoolean(KeyConsts.M_AutoClearCompleteUploads, false);
            PreferencesState.TransferViewShowSizes = prefs.GetBoolean(KeyConsts.M_TransfersShowSizes, true);
            PreferencesState.TransferViewShowSpeed = prefs.GetBoolean(KeyConsts.M_TransfersShowSpeed, true);
            PreferencesState.DisableDownloadToastNotification = prefs.GetBoolean(KeyConsts.M_DisableToastNotifications, true);
            PreferencesState.MemoryBackedDownload = prefs.GetBoolean(KeyConsts.M_MemoryBackedDownload, false);
            PreferencesState.NoSubfolderForSingle = prefs.GetBoolean(KeyConsts.M_NoSubfolderForSingle, false);
            PreferencesState.NotifyOnFolderCompleted = prefs.GetBoolean(KeyConsts.M_NotifyFolderComplete, true);
            PreferencesState.AutoRetryBackOnline = prefs.GetBoolean(KeyConsts.M_AutoRetryBackOnline, true);
            PreferencesState.SaveDataDirectoryUri = prefs.GetString(KeyConsts.M_SaveDataDirectoryUri, "");
            PreferencesState.SaveDataDirectoryUriIsFromTree = prefs.GetBoolean(KeyConsts.M_SaveDataDirectoryUriIsFromTree, true);
        }

        public static void RestoreSpeedLimitSettings(ISharedPreferences prefs)
        {
            PreferencesState.SpeedLimitUploadOn = prefs.GetBoolean(KeyConsts.M_UploadLimitEnabled, false);
            PreferencesState.SpeedLimitDownloadOn = prefs.GetBoolean(KeyConsts.M_DownloadLimitEnabled, false);
            PreferencesState.SpeedLimitUploadIsPerTransfer = prefs.GetBoolean(KeyConsts.M_UploadPerTransfer, true);
            PreferencesState.SpeedLimitDownloadIsPerTransfer = prefs.GetBoolean(KeyConsts.M_DownloadPerTransfer, true);
            PreferencesState.SpeedLimitUploadBytesSec = prefs.GetInt(KeyConsts.M_UploadSpeedLimitBytes, 4 * 1024 * 1024);
            PreferencesState.SpeedLimitDownloadBytesSec = prefs.GetInt(KeyConsts.M_DownloadSpeedLimitBytes, 4 * 1024 * 1024);
        }

        public static void RestoreSearchSettings(ISharedPreferences prefs)
        {
            PreferencesState.NumberSearchResults = prefs.GetInt(KeyConsts.M_NumberSearchResults, 250);
            PreferencesState.RememberSearchHistory = prefs.GetBoolean(KeyConsts.M_RememberSearchHistory, true);
            PreferencesState.ShowRecentUsers = prefs.GetBoolean(KeyConsts.M_RememberUserHistory, true);
            PreferencesState.FreeUploadSlotsOnly = prefs.GetBoolean(KeyConsts.M_OnlyFreeUploadSlots, true);
            PreferencesState.HideLockedResultsInBrowse = prefs.GetBoolean(KeyConsts.M_HideLockedBrowse, true);
            PreferencesState.HideLockedResultsInSearch = prefs.GetBoolean(KeyConsts.M_HideLockedSearch, true);
            PreferencesState.FilterSticky = prefs.GetBoolean(KeyConsts.M_FilterSticky, false);
            PreferencesState.FilterStickyString = prefs.GetString(KeyConsts.M_FilterStickyString, string.Empty);
            PreferencesState.SearchResultStyle = prefs.GetInt(KeyConsts.M_SearchResultStyle, 1);
            PreferencesState.DefaultSearchResultSortAlgorithm = (SearchResultSorting)(prefs.GetInt(KeyConsts.M_DefaultSearchResultSortAlgorithm, 0));
        }

        public static void RestoreSocialSettings(ISharedPreferences prefs)
        {
            PreferencesState.AllowPrivateRoomInvitations = prefs.GetBoolean(KeyConsts.M_AllowPrivateRooomInvitations, false);
            PreferencesState.StartServiceOnStartup = prefs.GetBoolean(KeyConsts.M_ServiceOnStartup, true);
            PreferencesState.AutoAwayOnInactivity = prefs.GetBoolean(KeyConsts.M_AutoSetAwayOnInactivity, false);
            PreferencesState.UserInfoBio = prefs.GetString(KeyConsts.M_UserInfoBio, string.Empty);
            PreferencesState.UserInfoPictureName = prefs.GetString(KeyConsts.M_UserInfoPicture, string.Empty);
            PreferencesState.ShowStatusesView = prefs.GetBoolean(KeyConsts.M_ShowStatusesView, true);
            PreferencesState.ShowTickerView = prefs.GetBoolean(KeyConsts.M_ShowTickerView, false);
            PreferencesState.SortChatroomUsersBy = prefs.GetInt(KeyConsts.M_RoomUserListSortOrder, 2); // Alphabetical
            PreferencesState.PutFriendsOnTop = prefs.GetBoolean(KeyConsts.M_RoomUserListShowFriendsAtTop, false);
        }

        public static void RestoreMiscSettings(ISharedPreferences prefs)
        {
            PreferencesState.SharingOn = prefs.GetBoolean(KeyConsts.M_SharingOn, false);
            PreferencesState.UploadSpeed = prefs.GetInt(KeyConsts.M_UploadSpeed, -1);
            PreferencesState.AllowUploadsOnMetered = prefs.GetBoolean(KeyConsts.M_AllowUploadsOnMetered, true);
            PreferencesState.UserListSortOrder = prefs.GetInt(KeyConsts.M_UserListSortOrder, 0);
            PreferencesState.LogDiagnostics = prefs.GetBoolean(KeyConsts.M_LOG_DIAGNOSTICS, false);
            PreferencesState.LimitSimultaneousDownloads = prefs.GetBoolean(KeyConsts.M_LimitSimultaneousDownloads, false);
            PreferencesState.MaxSimultaneousLimit = prefs.GetInt(KeyConsts.M_MaxSimultaneousLimit, 1);
        }

        public static void RestoreListeningState(ISharedPreferences prefs)
        {
            PreferencesState.ListenerEnabled = prefs.GetBoolean(KeyConsts.M_ListenerEnabled, true);
            PreferencesState.ListenerPort = prefs.GetInt(KeyConsts.M_ListenerPort, 33939);
            PreferencesState.ListenerUPnpEnabled = prefs.GetBoolean(KeyConsts.M_ListenerUPnpEnabled, true);
        }

        public static void RestoreSmartFilterState(ISharedPreferences prefs)
        {
            PreferencesState.SmartFilterOptions = new PreferencesState.SmartFilterState();
            PreferencesState.SmartFilterOptions.KeywordsEnabled = prefs.GetBoolean(KeyConsts.M_SmartFilter_KeywordsEnabled, true);
            PreferencesState.SmartFilterOptions.KeywordsOrder = prefs.GetInt(KeyConsts.M_SmartFilter_KeywordsOrder, 0);
            PreferencesState.SmartFilterOptions.FileTypesEnabled = prefs.GetBoolean(KeyConsts.M_SmartFilter_TypesEnabled, true);
            PreferencesState.SmartFilterOptions.FileTypesOrder = prefs.GetInt(KeyConsts.M_SmartFilter_TypesOrder, 1);
            PreferencesState.SmartFilterOptions.NumFilesEnabled = prefs.GetBoolean(KeyConsts.M_SmartFilter_CountsEnabled, true);
            PreferencesState.SmartFilterOptions.NumFilesOrder = prefs.GetInt(KeyConsts.M_SmartFilter_CountsOrder, 2);
        }

        // Save methods

        public static void SaveListeningState(ISharedPreferencesEditor editor)
        {
            editor.PutBoolean(KeyConsts.M_ListenerEnabled, PreferencesState.ListenerEnabled);
            editor.PutInt(KeyConsts.M_ListenerPort, PreferencesState.ListenerPort);
            editor.PutBoolean(KeyConsts.M_ListenerUPnpEnabled, PreferencesState.ListenerUPnpEnabled);
            editor.Commit();
        }

        public static void SaveSpeedLimitState(ISharedPreferencesEditor editor)
        {
            editor.PutBoolean(KeyConsts.M_DownloadLimitEnabled, PreferencesState.SpeedLimitDownloadOn);
            editor.PutBoolean(KeyConsts.M_DownloadPerTransfer, PreferencesState.SpeedLimitDownloadIsPerTransfer);
            editor.PutInt(KeyConsts.M_DownloadSpeedLimitBytes, PreferencesState.SpeedLimitDownloadBytesSec);
            editor.PutBoolean(KeyConsts.M_UploadLimitEnabled, PreferencesState.SpeedLimitUploadOn);
            editor.PutBoolean(KeyConsts.M_UploadPerTransfer, PreferencesState.SpeedLimitUploadIsPerTransfer);
            editor.PutInt(KeyConsts.M_UploadSpeedLimitBytes, PreferencesState.SpeedLimitUploadBytesSec);
            editor.Commit();
        }

        public static void SaveSmartFilterState(ISharedPreferencesEditor editor)
        {
            editor.PutBoolean(KeyConsts.M_SmartFilter_KeywordsEnabled, PreferencesState.SmartFilterOptions.KeywordsEnabled);
            editor.PutBoolean(KeyConsts.M_SmartFilter_TypesEnabled, PreferencesState.SmartFilterOptions.FileTypesEnabled);
            editor.PutBoolean(KeyConsts.M_SmartFilter_CountsEnabled, PreferencesState.SmartFilterOptions.NumFilesEnabled);
            editor.PutInt(KeyConsts.M_SmartFilter_KeywordsOrder, PreferencesState.SmartFilterOptions.KeywordsOrder);
            editor.PutInt(KeyConsts.M_SmartFilter_TypesOrder, PreferencesState.SmartFilterOptions.FileTypesOrder);
            editor.PutInt(KeyConsts.M_SmartFilter_CountsOrder, PreferencesState.SmartFilterOptions.NumFilesOrder);
            editor.Commit();
        }
    }
}
