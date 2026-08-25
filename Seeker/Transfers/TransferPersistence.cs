using Android.Content;
using Common;
using Seeker.Helpers;

namespace Seeker
{
    /// <summary>
    /// Android-specific wrappers around TransferPersistence (in Common) that handle
    /// reading from / writing to ISharedPreferences.
    /// </summary>
    public static class TransferPersistenceWrapper
    {
        public static void RestoreDownloadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(KeyConsts.M_TransferList, string.Empty);
            TransferPersistence.RestoreDownloadTransferItems(transferList);
        }

        public static void RestoreUploadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(KeyConsts.M_TransferListUpload, string.Empty);
            TransferPersistence.RestoreUploadTransferItems(transferList);
        }

        public static void SaveTransferItems(bool force = false, int maxSecondsUpdate = 0, bool commit = false)
        {
            var result = TransferPersistence.SaveTransferItems(force, maxSecondsUpdate);
            if (result.HasValue)
            {
                PreferencesManager.SaveTransferItems(result.Value.downloads, result.Value.uploads, commit);
            }
        }
    }
}
