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
            string transferListV2 = string.Empty; //sharedPreferences.GetString(KeyConsts.M_TransferList_v2, string.Empty);
            string transferListLegacy = sharedPreferences.GetString(KeyConsts.M_TransferList, string.Empty);
            TransferPersistence.RestoreDownloadTransferItems(transferListLegacy, transferListV2);
        }

        public static void RestoreUploadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferListV2 = string.Empty; //sharedPreferences.GetString(KeyConsts.M_Upload_TransferList_v2, string.Empty); //TODO !!! replace !!!
            string transferListLegacy = sharedPreferences.GetString(KeyConsts.M_TransferListUpload, string.Empty);
            TransferPersistence.RestoreUploadTransferItems(transferListLegacy, transferListV2);
        }

        public static void SaveTransferItems(bool force = false, int maxSecondsUpdate = 0)
        {
            var result = TransferPersistence.SaveTransferItems(force, maxSecondsUpdate);
            if (result.HasValue)
            {
                PreferencesManager.SaveTransferItems(result.Value.downloads, result.Value.uploads);
            }
        }
    }
}
