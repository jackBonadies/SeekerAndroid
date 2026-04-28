using Android.Content;
using Common;
using Common.Messages;
using System;
using System.Collections.Generic;

namespace Seeker
{
    public static class PreferencesManager
    {
        private static object SharedPrefLock = new object();
        private static object TransferStateSaveLock = new object();

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
            PreferencesState.NightModeVariant = (NightThemeType)(prefs.GetInt(KeyConsts.M_NightVariant, (int)NightThemeType.ClassicPurple));
            PreferencesState.DayModeVariant = (DayThemeType)(prefs.GetInt(KeyConsts.M_DayVariant, (int)DayThemeType.ClassicPurple));
            PreferencesState.ShowSmartFilters = prefs.GetBoolean(KeyConsts.M_ShowSmartFilters, true);
            PreferencesState.SmartFilterStyle = (Seeker.SmartFilterStyle)prefs.GetInt(KeyConsts.M_SmartFilterStyle, (int)Seeker.SmartFilterStyle.Flat);
            RestoreSmartFilterState(prefs);
        }

        public static void RestoreTransferSettings(ISharedPreferences prefs)
        {
            PreferencesState.AutoClearCompleteDownloads = prefs.GetBoolean(KeyConsts.M_AutoClearComplete, false);
            PreferencesState.AutoClearCompleteUploads = prefs.GetBoolean(KeyConsts.M_AutoClearCompleteUploads, false);
            PreferencesState.TransferViewShowSizes = prefs.GetBoolean(KeyConsts.M_TransfersShowSizes, true);
            PreferencesState.TransferViewShowSpeed = prefs.GetBoolean(KeyConsts.M_TransfersShowSpeed, true);
            PreferencesState.TransferViewGroupByFolder = prefs.GetBoolean(KeyConsts.M_TransfersGroupByFolder, false);
            PreferencesState.TransferViewInUploadsMode = prefs.GetBoolean(KeyConsts.M_TransfersInUploadsMode, false);
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
            PreferencesState.NumberSearchResults = prefs.GetInt(KeyConsts.M_NumberSearchResults, 500);
            PreferencesState.RememberSearchHistory = prefs.GetBoolean(KeyConsts.M_RememberSearchHistory, true);
            PreferencesState.ShowRecentUsers = prefs.GetBoolean(KeyConsts.M_RememberUserHistory, true);
            PreferencesState.FreeUploadSlotsOnly = prefs.GetBoolean(KeyConsts.M_OnlyFreeUploadSlots, true);
            PreferencesState.HideLockedResultsInBrowse = prefs.GetBoolean(KeyConsts.M_HideLockedBrowse, true);
            PreferencesState.HideLockedResultsInSearch = prefs.GetBoolean(KeyConsts.M_HideLockedSearch, true);
            PreferencesState.FilterSticky = prefs.GetBoolean(KeyConsts.M_FilterSticky, false);
            PreferencesState.FilterStickyString = prefs.GetString(KeyConsts.M_FilterStickyString, string.Empty);
            PreferencesState.SearchResultStyle = LoadSearchResultStyle(prefs);
            PreferencesState.ExpandAllResults = prefs.GetBoolean(KeyConsts.M_ExpandAllResults, false);
            PreferencesState.DefaultSearchResultSortAlgorithm = (SearchResultSorting)(prefs.GetInt(KeyConsts.M_DefaultSearchResultSortAlgorithm, 0));

            PreferencesState.SearchHistory = GetSearchHistory(prefs);

            PreferencesState.FilterFormat = (FormatFilterType)prefs.GetInt(KeyConsts.M_FilterFormat, 0);
            PreferencesState.FilterMinBitrateKbs = prefs.GetInt(KeyConsts.M_FilterMinBitrateKbs, 0);
        }

        private static SearchResultStyleEnum LoadSearchResultStyle(ISharedPreferences prefs)
        {
            const int sentinel = int.MinValue;
            int v2 = prefs.GetInt(KeyConsts.M_SearchResultStyleV2, sentinel);
            if (v2 != sentinel)
            {
                return (SearchResultStyleEnum)v2;
            }
            int legacy = prefs.GetInt(KeyConsts.M_SearchResultStyle, sentinel);
            if (legacy == sentinel)
            {
                return SearchResultStyleEnum.ModernBottom;
            }
            return MigrateLegacyStyle(legacy);
        }

        // Maps the pre-V2 persisted ints to the new orthogonal flags layout.
        private static SearchResultStyleEnum MigrateLegacyStyle(int legacy)
        {
            switch (legacy)
            {
                case 0: return SearchResultStyleEnum.Compact;                 // MinimalLegacy → new Compact
                case 1: return SearchResultStyleEnum.ModernBottom;            // MediumModernBitrateBottom
                case 2: return SearchResultStyleEnum.SimpleBottomExpandable;  // ExpandableLegacy
                case 3:                                                       // legacy ExpandedAll
                    PreferencesState.ExpandAllResults = true;
                    return SearchResultStyleEnum.SimpleBottomExpandable;
                case 4: return SearchResultStyleEnum.ModernBottomExpandable;  // ExpandableModern
                case 5: return SearchResultStyleEnum.ModernTop;               // MediumModernBitrateTop
                case 11: return SearchResultStyleEnum.SimpleBottom;           // MediumLegacy
                default: return SearchResultStyleEnum.ModernBottom;
            }
        }

        private static List<string> GetSearchHistory(ISharedPreferences prefs)
        {
            string searchHistoryXml = prefs.GetString(KeyConsts.M_SearchHistory, string.Empty);
            if (!string.IsNullOrEmpty(searchHistoryXml))
            {
                using (var stream = new System.IO.StringReader(searchHistoryXml))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<string>));
                    return serializer.Deserialize(stream) as List<string> ?? new List<string>();
                }
            }
            return new List<string>();
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
            PreferencesState.SortChatroomUsersBy = (SortOrderChatroomUsers)prefs.GetInt(KeyConsts.M_RoomUserListSortOrder, (int)SortOrderChatroomUsers.Alphabetical);
            PreferencesState.PutFriendsOnTop = prefs.GetBoolean(KeyConsts.M_RoomUserListShowFriendsAtTop, false);
        }

        public static void RestoreMiscSettings(ISharedPreferences prefs)
        {
            PreferencesState.SharingOn = prefs.GetBoolean(KeyConsts.M_SharingOn, false);
            PreferencesState.UploadSpeed = prefs.GetInt(KeyConsts.M_UploadSpeed, -1);
            PreferencesState.AllowUploadsOnMetered = prefs.GetBoolean(KeyConsts.M_AllowUploadsOnMetered, true);
            PreferencesState.UserListSortOrder = (SortOrder)prefs.GetInt(KeyConsts.M_UserListSortOrder, 0);
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

        // Save Methods
        public static void SaveListeningState()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_ListenerEnabled, PreferencesState.ListenerEnabled);
                editor.PutInt(KeyConsts.M_ListenerPort, PreferencesState.ListenerPort);
                editor.PutBoolean(KeyConsts.M_ListenerUPnpEnabled, PreferencesState.ListenerUPnpEnabled);
                editor.Apply();
            }
        }

        public static void SaveSpeedLimitState()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_DownloadLimitEnabled, PreferencesState.SpeedLimitDownloadOn);
                editor.PutBoolean(KeyConsts.M_DownloadPerTransfer, PreferencesState.SpeedLimitDownloadIsPerTransfer);
                editor.PutInt(KeyConsts.M_DownloadSpeedLimitBytes, PreferencesState.SpeedLimitDownloadBytesSec);
                editor.PutBoolean(KeyConsts.M_UploadLimitEnabled, PreferencesState.SpeedLimitUploadOn);
                editor.PutBoolean(KeyConsts.M_UploadPerTransfer, PreferencesState.SpeedLimitUploadIsPerTransfer);
                editor.PutInt(KeyConsts.M_UploadSpeedLimitBytes, PreferencesState.SpeedLimitUploadBytesSec);
                editor.Apply();
            }
        }

        public static void SaveSmartFilterState()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_SmartFilter_KeywordsEnabled, PreferencesState.SmartFilterOptions.KeywordsEnabled);
                editor.PutBoolean(KeyConsts.M_SmartFilter_TypesEnabled, PreferencesState.SmartFilterOptions.FileTypesEnabled);
                editor.PutBoolean(KeyConsts.M_SmartFilter_CountsEnabled, PreferencesState.SmartFilterOptions.NumFilesEnabled);
                editor.PutInt(KeyConsts.M_SmartFilter_KeywordsOrder, PreferencesState.SmartFilterOptions.KeywordsOrder);
                editor.PutInt(KeyConsts.M_SmartFilter_TypesOrder, PreferencesState.SmartFilterOptions.FileTypesOrder);
                editor.PutInt(KeyConsts.M_SmartFilter_CountsOrder, PreferencesState.SmartFilterOptions.NumFilesOrder);
                editor.Apply();
            }
        }

        public static void SaveShowTickerView()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_ShowTickerView, PreferencesState.ShowTickerView);
                editor.Apply();
            }
        }

        public static void SaveTransferViewGroupByFolder()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_TransfersGroupByFolder, PreferencesState.TransferViewGroupByFolder);
                editor.Apply();
            }
        }

        public static void SaveTransferViewInUploadsMode()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_TransfersInUploadsMode, PreferencesState.TransferViewInUploadsMode);
                editor.Apply();
            }
        }

        public static void SaveShowStatusesView()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_ShowStatusesView, PreferencesState.ShowStatusesView);
                editor.Apply();
            }
        }

        public static void SavePutFriendsOnTop()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_RoomUserListShowFriendsAtTop, PreferencesState.PutFriendsOnTop);
                editor.Apply();
            }
        }

        public static void SaveSortChatroomUsersBy()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_RoomUserListSortOrder, (int)PreferencesState.SortChatroomUsersBy);
                editor.Apply();
            }
        }

        public static void SavePassword()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_Password, PreferencesState.Password);
                editor.Apply();
            }
        }

        public static void SaveDefaultSearchResultSortAlgorithm()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_DefaultSearchResultSortAlgorithm, (int)PreferencesState.DefaultSearchResultSortAlgorithm);
                editor.Apply();
            }
        }

        public static void SaveLanguage()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_Lanuage, PreferencesState.Language);
                editor.Apply();
            }
        }

        public static void SaveAllowUploadsOnMetered()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_AllowUploadsOnMetered, PreferencesState.AllowUploadsOnMetered);
                editor.Apply();
            }
        }

        public static void SaveNotifyOnFolderCompleted()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_NotifyFolderComplete, PreferencesState.NotifyOnFolderCompleted);
                editor.Apply();
            }
        }

        public static void SaveAutoRetryBackOnline()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_AutoRetryBackOnline, PreferencesState.AutoRetryBackOnline);
                editor.Apply();
            }
        }

        public static void SaveAutoAwayOnInactivity()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_AutoSetAwayOnInactivity, PreferencesState.AutoAwayOnInactivity);
                editor.Apply();
            }
        }

        public static void SaveShowSmartFilters()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_ShowSmartFilters, PreferencesState.ShowSmartFilters);
                editor.Apply();
            }
        }

        public static void SaveSmartFilterStyle()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_SmartFilterStyle, (int)PreferencesState.SmartFilterStyle);
                editor.Apply();
            }
        }

        public static void SaveFilterControlsState()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_FilterFormat, (int)PreferencesState.FilterFormat);
                editor.PutInt(KeyConsts.M_FilterMinBitrateKbs, PreferencesState.FilterMinBitrateKbs);
                editor.Apply();
            }
        }

        public static void SaveLogDiagnostics()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_LOG_DIAGNOSTICS, PreferencesState.LogDiagnostics);
                editor.Apply();
            }
        }

        public static void SaveStartServiceOnStartup()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_ServiceOnStartup, PreferencesState.StartServiceOnStartup);
                editor.Apply();
            }
        }

        public static void SaveAllowPrivateRoomInvitations()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_AllowPrivateRooomInvitations, PreferencesState.AllowPrivateRoomInvitations);
                editor.Apply();
            }
        }

        public static void SaveDayModeVariant()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_DayVariant, (int)PreferencesState.DayModeVariant);
                editor.Apply();
            }
        }

        public static void SaveNightModeVariant()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_NightVariant, (int)PreferencesState.NightModeVariant);
                editor.Apply();
            }
        }

        public static void SaveDayNightMode()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_DayNightMode, PreferencesState.DayNightMode);
                editor.Apply();
            }
        }

        public static void SaveUserInfoPictureName()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_UserInfoPicture, PreferencesState.UserInfoPictureName);
                editor.Apply();
            }
        }

        public static void SaveUserListSortOrder()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_UserListSortOrder, (int)PreferencesState.UserListSortOrder);
                editor.Apply();
            }
        }

        public static void SaveLegacyLanguageMigrated()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_LegacyLanguageMigrated, PreferencesState.LegacyLanguageMigrated);
                editor.Apply();
            }
        }

        public static void SaveManualIncompleteDir()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_ManualIncompleteDirectoryUri, PreferencesState.ManualIncompleteDataDirectoryUri);
                editor.PutBoolean(KeyConsts.M_ManualIncompleteDirectoryUriIsFromTree, PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree);
                editor.Apply();
            }
        }

        public static void SaveAdditionalDirectorySettings()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_CreateCompleteAndIncompleteFolders, PreferencesState.CreateCompleteAndIncompleteFolders);
                editor.PutBoolean(KeyConsts.M_UseManualIncompleteDirectoryUri, PreferencesState.OverrideDefaultIncompleteLocations);
                editor.PutBoolean(KeyConsts.M_AdditionalUsernameSubdirectories, PreferencesState.CreateUsernameSubfolders);
                editor.PutBoolean(KeyConsts.M_NoSubfolderForSingle, PreferencesState.NoSubfolderForSingle);
                editor.PutString(KeyConsts.M_ManualIncompleteDirectoryUri, PreferencesState.ManualIncompleteDataDirectoryUri);
                editor.PutBoolean(KeyConsts.M_ManualIncompleteDirectoryUriIsFromTree, PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree);
                editor.Apply();
            }
        }

        public static void RestoreAdditionalDirectorySettings()
        {
            lock (SharedPrefLock)
            {
                PreferencesState.CreateCompleteAndIncompleteFolders = SeekerState.SharedPreferences.GetBoolean(KeyConsts.M_CreateCompleteAndIncompleteFolders, true);
                PreferencesState.OverrideDefaultIncompleteLocations = SeekerState.SharedPreferences.GetBoolean(KeyConsts.M_UseManualIncompleteDirectoryUri, false);
                PreferencesState.CreateUsernameSubfolders = SeekerState.SharedPreferences.GetBoolean(KeyConsts.M_AdditionalUsernameSubdirectories, false);
                PreferencesState.ManualIncompleteDataDirectoryUri = SeekerState.SharedPreferences.GetString(KeyConsts.M_ManualIncompleteDirectoryUri, string.Empty);
                PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree = SeekerState.SharedPreferences.GetBoolean(KeyConsts.M_ManualIncompleteDirectoryUriIsFromTree, true);
            }
        }

        public static void SaveMaxConcurrentDownloadsSettings(bool restrict, int max)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_LimitSimultaneousDownloads, restrict);
                editor.PutInt(KeyConsts.M_MaxSimultaneousLimit, max);
                editor.Apply();
            }
        }

        public static void SaveUPnPState(long ticks, int lifetime, int port, string localIP)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutLong(KeyConsts.M_LastSetUpnpRuleTicks, ticks);
                editor.PutInt(KeyConsts.M_LifetimeSeconds, lifetime);
                editor.PutInt(KeyConsts.M_PortMapped, port);
                editor.PutString(KeyConsts.M_LastSetLocalIP, localIP);
                editor.Apply();
            }
        }

        public static void RestoreUPnPState(out long ticks, out int lifetime, out int port, out string localIP)
        {
            lock (SharedPrefLock)
            {
                ticks = SeekerState.SharedPreferences.GetLong(KeyConsts.M_LastSetUpnpRuleTicks, 0);
                lifetime = SeekerState.SharedPreferences.GetInt(KeyConsts.M_LifetimeSeconds, -1);
                port = SeekerState.SharedPreferences.GetInt(KeyConsts.M_PortMapped, -1);
                localIP = SeekerState.SharedPreferences.GetString(KeyConsts.M_LastSetLocalIP, string.Empty);
            }
        }

        // TODO: refactor these? why are they not in preferences state?
        public static void SaveUserInfoBio(string bio)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_UserInfoBio, bio);
                editor.Apply();
            }
        }

        public static void SaveUserList(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_UserList, serialized);
                editor.Apply();
            }
        }

        public static void SaveIgnoreUserList(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_IgnoreUserList, serialized);
                editor.Apply();
            }
        }

        public static void SaveUserNotes(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_UserNotes, serialized);
                editor.Apply();
            }
        }

        public static void SaveUserOnlineAlerts(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_UserOnlineAlerts, serialized);
                editor.Apply();
            }
        }

        public static void SaveAutoJoinRooms(string joinedRoomsString)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_AutoJoinRooms, joinedRoomsString);
                editor.Apply();
            }
        }

        public static void SaveNotifyRooms(string notifyRoomsString)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_chatroomsToNotify, notifyRoomsString);
                editor.Apply();
            }
        }

        public static void SaveSearchTabHeaders(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_SearchTabsState_Headers, serialized);
                editor.Apply();
            }
        }

        public static void SaveMessages(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_Messages, serialized);
                editor.Apply();
            }
        }

        public static void SaveLastReadMessageCounts(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_LastReadMessageCounts, serialized);
                editor.Apply();
            }
        }

        public static void SaveRecentUsers(string serialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_RecentUsersList, serialized);
                editor.Apply();
            }
        }

        public static void ClearSearchHistory()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_SearchHistory, string.Empty);
                editor.Apply();
            }
        }

        public static void SaveSearchHistory()
        {
            lock (SharedPrefLock)
            {
                string serialized;
                using (var writer = new System.IO.StringWriter())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<string>));
                    serializer.Serialize(writer, PreferencesState.SearchHistory);
                    serialized = writer.ToString();
                }
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_SearchHistory, serialized);
                editor.Apply();
            }
        }

        public static void SavePostNotificationShown()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_PostNotificationRequestAlreadyShown, true);
                editor.Apply();
            }
        }

        public static void ClearLegacyCachedResults()
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.Remove(KeyConsts.M_CACHE_stringUriPairs);
                editor.Remove(KeyConsts.M_CACHE_browseResponse);
                editor.Remove(KeyConsts.M_CACHE_friendlyDirNameToUriMapping);
                editor.Remove(KeyConsts.M_CACHE_auxDupList);
                editor.Remove(KeyConsts.M_CACHE_stringUriPairs_v2);
                editor.Remove(KeyConsts.M_CACHE_stringUriPairs_v3);
                editor.Remove(KeyConsts.M_CACHE_browseResponse_v2);
                editor.Remove(KeyConsts.M_CACHE_friendlyDirNameToUriMapping_v2);
                editor.Remove(KeyConsts.M_CACHE_tokenIndex_v2);
                editor.Remove(KeyConsts.M_CACHE_intHelperIndex_v2);
                editor.Apply();
            }
        }

        public static void SaveCachedFileCount(int count)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_CACHE_nonHiddenFileCount_v3, count);
                editor.Apply();
            }
        }

        public static void RestoreListeningStateLocked()
        {
            lock (SharedPrefLock)
            {
                RestoreListeningState(SeekerState.SharedPreferences);
            }
        }

        /// <summary>
        /// Saves the bulk state from MainActivity.OnPause — all PreferencesState fields
        /// plus a pre-serialized user list string (null to skip).
        /// </summary>
        public static void SaveOnPauseState(string userListSerialized)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_CurrentlyLoggedIn, PreferencesState.CurrentlyLoggedIn);
                editor.PutString(KeyConsts.M_Username, PreferencesState.Username);
                editor.PutString(KeyConsts.M_Password, PreferencesState.Password);
                editor.PutString(KeyConsts.M_SaveDataDirectoryUri, PreferencesState.SaveDataDirectoryUri);
                editor.PutBoolean(KeyConsts.M_SaveDataDirectoryUriIsFromTree, PreferencesState.SaveDataDirectoryUriIsFromTree);
                editor.PutInt(KeyConsts.M_NumberSearchResults, PreferencesState.NumberSearchResults);
                editor.PutInt(KeyConsts.M_DayNightMode, PreferencesState.DayNightMode);
                editor.PutBoolean(KeyConsts.M_AutoClearComplete, PreferencesState.AutoClearCompleteDownloads);
                editor.PutBoolean(KeyConsts.M_AutoClearCompleteUploads, PreferencesState.AutoClearCompleteUploads);
                editor.PutBoolean(KeyConsts.M_RememberSearchHistory, PreferencesState.RememberSearchHistory);
                editor.PutBoolean(KeyConsts.M_RememberUserHistory, PreferencesState.ShowRecentUsers);
                editor.PutBoolean(KeyConsts.M_TransfersShowSizes, PreferencesState.TransferViewShowSizes);
                editor.PutBoolean(KeyConsts.M_TransfersShowSpeed, PreferencesState.TransferViewShowSpeed);
                editor.PutBoolean(KeyConsts.M_TransfersGroupByFolder, PreferencesState.TransferViewGroupByFolder);
                editor.PutBoolean(KeyConsts.M_TransfersInUploadsMode, PreferencesState.TransferViewInUploadsMode);
                editor.PutBoolean(KeyConsts.M_OnlyFreeUploadSlots, PreferencesState.FreeUploadSlotsOnly);
                editor.PutBoolean(KeyConsts.M_HideLockedSearch, PreferencesState.HideLockedResultsInSearch);
                editor.PutBoolean(KeyConsts.M_HideLockedBrowse, PreferencesState.HideLockedResultsInBrowse);
                editor.PutBoolean(KeyConsts.M_FilterSticky, PreferencesState.FilterSticky);
                editor.PutString(KeyConsts.M_FilterStickyString, PreferencesState.FilterStickyString);
                editor.PutBoolean(KeyConsts.M_MemoryBackedDownload, PreferencesState.MemoryBackedDownload);
                editor.PutInt(KeyConsts.M_SearchResultStyleV2, (int)PreferencesState.SearchResultStyle);
                editor.PutBoolean(KeyConsts.M_ExpandAllResults, PreferencesState.ExpandAllResults);
                editor.PutBoolean(KeyConsts.M_DisableToastNotifications, PreferencesState.DisableDownloadToastNotification);
                editor.PutInt(KeyConsts.M_UploadSpeed, PreferencesState.UploadSpeed);
                editor.PutBoolean(KeyConsts.M_SharingOn, PreferencesState.SharingOn);
                editor.PutBoolean(KeyConsts.M_AllowPrivateRooomInvitations, PreferencesState.AllowPrivateRoomInvitations);

                if (userListSerialized != null)
                {
                    editor.PutString(KeyConsts.M_UserList, userListSerialized);
                }

                editor.Apply();
            }
        }

        /// <summary>
        /// Saves the sticky filter state and search result style from SearchFragment.OnPause.
        /// </summary>
        public static void SaveSearchFragmentFilterState(bool filterSticky, string filterStickyString, int searchResultStyle, bool expandAllResults)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                if (filterSticky)
                {
                    editor.PutBoolean(KeyConsts.M_FilterSticky, filterSticky);
                    editor.PutString(KeyConsts.M_FilterStickyString, filterStickyString);
                }
                editor.PutInt(KeyConsts.M_SearchResultStyleV2, searchResultStyle);
                editor.PutBoolean(KeyConsts.M_ExpandAllResults, expandAllResults);
                editor.Apply();
            }
        }

        /// <summary>
        /// Saves serialized transfer items, acquiring both SharedPrefLock and TransferStateSaveLock.
        /// </summary>
        public static void SaveTransferItems(string downloads, string uploads)
        {
            lock (SharedPrefLock)
                lock (TransferStateSaveLock)
                {
                    var editor = SeekerState.SharedPreferences.Edit();
                    editor.PutString(KeyConsts.M_TransferList, downloads);
                    editor.PutString(KeyConsts.M_TransferListUpload, uploads);
                    editor.Apply();
                }
        }

        /// <summary>
        /// Migrates cache from v2 to v3 format: clears v2 key and saves v3 data.
        /// </summary>
        public static void SaveCacheV2ToV3Migration(string v3Data)
        {
            lock (SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_CACHE_stringUriPairs_v2, string.Empty);
                editor.PutString(KeyConsts.M_CACHE_stringUriPairs_v3, v3Data);
                editor.Apply();
            }
        }
    }
}
