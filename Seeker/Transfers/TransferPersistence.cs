using Android.Content;
using Seeker.Helpers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

using Common;

namespace Seeker
{
    public static class TransferPersistence
    {
        private static DateTime transfersLastSavedTime = DateTime.MinValue;

        public static void RestoreUploadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferListv2 = string.Empty;//sharedPreferences.GetString(KeyConsts.M_Upload_TransferList_v2, string.Empty); //TODO !!! replace !!!
            if (transferListv2 == string.Empty)
            {
                RestoreUploadTransferItemsLegacy(sharedPreferences);
            }
            else
            {
                //restore the simple way via deserializing...
                TransferItems.TransferItemManagerUploads = new TransferItemManager(true);
                using (var stream = new System.IO.StringReader(transferListv2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItems.TransferItemManagerUploads.GetType());
                    TransferItems.TransferItemManagerUploads = serializer.Deserialize(stream) as TransferItemManager;
                    TransferItems.TransferItemManagerUploads.OnRelaunch();
                }
            }
        }

        public static void RestoreUploadTransferItemsLegacy(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(KeyConsts.M_TransferListUpload, string.Empty);
            if (transferList == string.Empty)
            {
                TransferItems.TransferItemManagerUploads = new TransferItemManager(true);
            }
            else
            {
                var transferItemsLegacy = new List<TransferItem>();
                using (var stream = new System.IO.StringReader(transferList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(transferItemsLegacy.GetType());
                    transferItemsLegacy = serializer.Deserialize(stream) as List<TransferItem>;
                }

                TransferItems.TransferItemManagerUploads = new TransferItemManager(true);
                foreach (var ti in transferItemsLegacy)
                {
                    TransferItems.TransferItemManagerUploads.Add(ti);
                }
                TransferItems.TransferItemManagerUploads.OnRelaunch();
            }
        }

        public static void RestoreDownloadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferListv2 = string.Empty;//sharedPreferences.GetString(KeyConsts.M_TransferList_v2, string.Empty);
            if (transferListv2 == string.Empty)
            {
                RestoreDownloadTransferItemsLegacy(sharedPreferences);
            }
            else
            {
                //restore the simple way via deserializing...
                TransferItems.TransferItemManagerDL = new TransferItemManager();
                using (var stream = new System.IO.StringReader(transferListv2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItems.TransferItemManagerDL.GetType());
                    TransferItems.TransferItemManagerDL = serializer.Deserialize(stream) as TransferItemManager;
                    TransferItems.TransferItemManagerDL.OnRelaunch();
                }
            }
        }

        public static void RestoreDownloadTransferItemsLegacy(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(KeyConsts.M_TransferList, string.Empty);
            if (transferList == string.Empty)
            {
                TransferItems.TransferItemManagerDL = new TransferItemManager();
            }
            else
            {
                var transferItemsLegacy = new List<TransferItem>();
                using (var stream = new System.IO.StringReader(transferList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(transferItemsLegacy.GetType());
                    transferItemsLegacy = serializer.Deserialize(stream) as List<TransferItem>;
                }

                TransferItems.TransferItemManagerDL = new TransferItemManager();
                foreach (var ti in transferItemsLegacy)
                {
                    TransferItems.TransferItemManagerDL.Add(ti);
                }
                TransferItems.TransferItemManagerDL.OnRelaunch();
            }
        }

        public static void SaveTransferItems(bool force = false, int maxSecondsUpdate = 0)
        {
            Logger.Debug("---- saving transfer items enter ----");
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            sw.Start();
#endif

            if (force || (TransferItemManager.TransfersDirty && DateTime.UtcNow.Subtract(transfersLastSavedTime).TotalSeconds > maxSecondsUpdate)) //dirty and we havent updated too recently..
            {
                Logger.Debug("---- saving transfer items actual save ----");
                if (TransferItems.TransferItemManagerDL?.AllTransferItems == null)
                {
                    return;
                }
                List<TransferItem> dlSnapshot;
                lock (TransferItems.TransferItemManagerDL.AllTransferItems)
                {
                    dlSnapshot = TransferItems.TransferItemManagerDL.AllTransferItems.ToList();
                }

                List<TransferItem> ulSnapshot;
                lock (TransferItems.TransferItemManagerUploads.AllTransferItems)
                {
                    ulSnapshot = TransferItems.TransferItemManagerUploads.AllTransferItems.ToList();
                }

                string listOfDownloadItems = string.Empty;
                string listOfUploadItems = string.Empty;
                using (var writer = new System.IO.StringWriter())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(dlSnapshot.GetType());
                    serializer.Serialize(writer, dlSnapshot);
                    listOfDownloadItems = writer.ToString();
                }
                using (var writer = new System.IO.StringWriter())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(ulSnapshot.GetType());
                    serializer.Serialize(writer, ulSnapshot);
                    listOfUploadItems = writer.ToString();
                }
                PreferencesManager.SaveTransferItems(listOfDownloadItems, listOfUploadItems);

                TransferItemManager.TransfersDirty = false;
                transfersLastSavedTime = DateTime.UtcNow;
            }

#if DEBUG
            sw.Stop();
            Logger.Debug("saving time: " + sw.ElapsedMilliseconds);
#endif
        }
    }
}
