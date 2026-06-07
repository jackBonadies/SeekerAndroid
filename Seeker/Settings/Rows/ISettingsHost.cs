using Android.App;

namespace Seeker.Settings.Rows
{
    /// <summary>
    /// Implemented by SettingsActivity. Keeps row declarations free of direct Activity references.
    /// </summary>
    public interface ISettingsHost
    {
        Activity Activity { get; }
        SettingsAdapter Adapter { get; }

        void NotifyRowChanged(string rowId);
        void NotifyParentToggled(string parentId);

        void LaunchDownloadFolderPicker();
        void LaunchIncompleteFolderPicker();
        void LaunchAddSharedFolderPicker();

        void LaunchChangePassword();
        void LaunchEditUserInfo();
        void LaunchImportClientData();
        void LaunchExportClientData();
        void LaunchRestoreDefaults();
        void UpdateSimulataneousDownloadsLimit(bool enabled, int limit);
        void LaunchForceFilesystemPermission();

        void CheckPrivileges();
        void GetPrivileges();
        void CheckPortStatus();
        void RescanShares();
        void BrowseSelf();
        void ClearIncompleteFolder();
        void ClearSearchHistory();
        void ClearRecentUsers();
        void StartStopBackgroundService();

        void ConfigureSmartFilters();

        void EditSharedFolder(UploadDirectoryEntry entry);
        void RemoveSharedFolder(UploadDirectoryEntry entry);
        /// <summary>Rebuild the settings rows (e.g. after a folder is added/removed or a parse
        /// changes folder counts/errors) and re-apply the current search query.</summary>
        void RefreshSharingFolders();
    }
}
