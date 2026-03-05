using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public static class TransferPersistence
    {
        private static DateTime transfersLastSavedTime = DateTime.MinValue;

        public static void RestoreUploadTransferItems(string transferListLegacy, string transferListV2)
        {
            if (transferListV2 == string.Empty)
            {
                RestoreUploadTransferItemsLegacy(transferListLegacy);
            }
            else
            {
                TransferItems.TransferItemManagerUploads = new TransferItemManager(true);
                using (var stream = new System.IO.StringReader(transferListV2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItems.TransferItemManagerUploads.GetType());
                    TransferItems.TransferItemManagerUploads = serializer.Deserialize(stream) as TransferItemManager;
                    TransferItems.TransferItemManagerUploads.OnRelaunch();
                }
            }
        }

        public static void RestoreUploadTransferItemsLegacy(string transferList)
        {
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

        public static void RestoreDownloadTransferItems(string transferListLegacy, string transferListV2)
        {
            if (transferListV2 == string.Empty)
            {
                RestoreDownloadTransferItemsLegacy(transferListLegacy);
            }
            else
            {
                TransferItems.TransferItemManagerDL = new TransferItemManager();
                using (var stream = new System.IO.StringReader(transferListV2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItems.TransferItemManagerDL.GetType());
                    TransferItems.TransferItemManagerDL = serializer.Deserialize(stream) as TransferItemManager;
                    TransferItems.TransferItemManagerDL.OnRelaunch();
                }
            }
        }

        public static void RestoreDownloadTransferItemsLegacy(string transferList)
        {
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

        /// <summary>
        /// Serializes transfer items if dirty and enough time has elapsed.
        /// Returns (downloads, uploads) XML strings if a save was performed, or null if skipped.
        /// </summary>
        public static (string downloads, string uploads)? SaveTransferItems(bool force = false, int maxSecondsUpdate = 0)
        {
            if (force || (TransferItemManager.TransfersDirty && DateTime.UtcNow.Subtract(transfersLastSavedTime).TotalSeconds > maxSecondsUpdate))
            {
                if (TransferItems.TransferItemManagerDL?.AllTransferItems == null)
                {
                    return null;
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

                string listOfDownloadItems;
                string listOfUploadItems;
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

                TransferItemManager.TransfersDirty = false;
                transfersLastSavedTime = DateTime.UtcNow;

                return (listOfDownloadItems, listOfUploadItems);
            }

            return null;
        }
    }
}
