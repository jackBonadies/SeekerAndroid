using NUnit.Framework;
using Seeker;
using Soulseek;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferPersistenceTests
    {
        [SetUp]
        public void Setup()
        {
            TransferItems.TransferItemManagerDL = null;
            TransferItems.TransferItemManagerUploads = null;
            TransferItemManager.TransfersDirty = false;
        }

        private TransferItem CreateTransferItem(string username, string fullFilename, string folderName = "folder1", long size = 1000)
        {
            return new TransferItem
            {
                Username = username,
                FullFilename = fullFilename,
                FolderName = folderName,
                Filename = fullFilename.Split('\\').Last(),
                Size = size,
            };
        }

        private string SerializeTransferItems(List<TransferItem> items)
        {
            using (var writer = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(List<TransferItem>));
                serializer.Serialize(writer, items);
                return writer.ToString();
            }
        }

        // --- RestoreDownloadTransferItems ---

        [Test]
        public void RestoreDownloadTransferItems_EmptyStrings_CreatesEmptyManager()
        {
            TransferPersistence.RestoreDownloadTransferItems(string.Empty, string.Empty);

            Assert.IsNotNull(TransferItems.TransferItemManagerDL);
            Assert.AreEqual(0, TransferItems.TransferItemManagerDL.AllTransferItems.Count);
        }

        [Test]
        public void RestoreDownloadTransferItems_LegacyData_RestoresItems()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("user1", "\\dir\\folder1\\song.mp3"),
                CreateTransferItem("user2", "\\dir\\folder2\\track.mp3", "folder2"),
            };
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreDownloadTransferItems(xml, string.Empty);

            Assert.IsNotNull(TransferItems.TransferItemManagerDL);
            Assert.AreEqual(2, TransferItems.TransferItemManagerDL.AllTransferItems.Count);
            Assert.AreEqual("user1", TransferItems.TransferItemManagerDL.AllTransferItems[0].Username);
            Assert.AreEqual("user2", TransferItems.TransferItemManagerDL.AllTransferItems[1].Username);
        }

        [Test]
        public void RestoreDownloadTransferItems_LegacyData_PreservesFilenames()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("user1", "\\dir\\folder1\\song.mp3"),
            };
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreDownloadTransferItems(xml, string.Empty);

            var restored = TransferItems.TransferItemManagerDL.AllTransferItems[0];
            Assert.AreEqual("\\dir\\folder1\\song.mp3", restored.FullFilename);
            Assert.AreEqual("song.mp3", restored.Filename);
            Assert.AreEqual("folder1", restored.FolderName);
        }

        [Test]
        public void RestoreDownloadTransferItems_LegacyData_CallsOnRelaunch_ResetsInProgress()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("user1", "\\dir\\folder1\\song.mp3"),
            };
            items[0].State = TransferStates.InProgress;
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreDownloadTransferItems(xml, string.Empty);

            Assert.AreEqual(TransferStates.Cancelled, TransferItems.TransferItemManagerDL.AllTransferItems[0].State);
        }

        [Test]
        public void RestoreDownloadTransferItems_LegacyData_PreservesSize()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("user1", "\\dir\\folder1\\song.mp3", size: 54321),
            };
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreDownloadTransferItems(xml, string.Empty);

            Assert.AreEqual(54321, TransferItems.TransferItemManagerDL.AllTransferItems[0].Size);
        }

        // --- RestoreUploadTransferItems ---

        [Test]
        public void RestoreUploadTransferItems_EmptyStrings_CreatesEmptyManager()
        {
            TransferPersistence.RestoreUploadTransferItems(string.Empty, string.Empty);

            Assert.IsNotNull(TransferItems.TransferItemManagerUploads);
            Assert.AreEqual(0, TransferItems.TransferItemManagerUploads.AllTransferItems.Count);
        }

        [Test]
        public void RestoreUploadTransferItems_LegacyData_RestoresItems()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("upUser1", "\\share\\music\\file.mp3"),
            };
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreUploadTransferItems(xml, string.Empty);

            Assert.AreEqual(1, TransferItems.TransferItemManagerUploads.AllTransferItems.Count);
            Assert.AreEqual("upUser1", TransferItems.TransferItemManagerUploads.AllTransferItems[0].Username);
        }

        [Test]
        public void RestoreUploadTransferItems_LegacyData_CallsOnRelaunch_ResetsInProgress()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("user1", "\\share\\music\\file.mp3"),
            };
            items[0].State = TransferStates.InProgress;
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreUploadTransferItems(xml, string.Empty);

            Assert.AreEqual(TransferStates.Cancelled, TransferItems.TransferItemManagerUploads.AllTransferItems[0].State);
        }

        [Test]
        public void RestoreTransferItems_InflightSetToCancelledOnRestore()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("user1", "\\share\\music\\file1.mp3"),
                CreateTransferItem("user1", "\\share\\music\\file2.mp3"),
                CreateTransferItem("user1", "\\share\\music\\file3.mp3"),
                CreateTransferItem("user1", "\\share\\music\\file4.mp3"),
                CreateTransferItem("user1", "\\share\\music\\file5.mp3"),
            };
            items[0].State = TransferStates.InProgress;
            items[1].State = TransferStates.Initializing;
            items[2].State = TransferStates.Queued;
            items[3].State = TransferStates.Requested;
            items[4].State = TransferStates.Aborted;
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreDownloadTransferItems(xml, string.Empty);

            Assert.AreEqual(true, TransferItems.TransferItemManagerDL.AllTransferItems.All(t=>t.State == TransferStates.Cancelled));
        }

        // --- SaveTransferItems ---

        [Test]
        public void SaveTransferItems_NotDirty_ReturnsNull()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);
            TransferItemManager.TransfersDirty = false;

            var result = TransferPersistence.SaveTransferItems();

            Assert.IsNull(result);
        }

        [Test]
        public void SaveTransferItems_Force_ReturnsSerialized()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var ti = CreateTransferItem("user1", "\\dir\\folder1\\song.mp3");
            TransferItems.TransferItemManagerDL.Add(ti);

            var result = TransferPersistence.SaveTransferItems(force: true);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.downloads.Contains("song.mp3"));
        }

        [Test]
        public void SaveTransferItems_Dirty_ReturnsSerialized()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            TransferItems.TransferItemManagerDL.Add(CreateTransferItem("user1", "\\dir\\folder1\\song.mp3"));
            TransferItems.TransferItemManagerUploads.Add(CreateTransferItem("user2", "\\share\\folder1\\track.mp3"));
            TransferItemManager.TransfersDirty = true;

            var result = TransferPersistence.SaveTransferItems();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.downloads.Contains("song.mp3"));
            Assert.IsTrue(result.Value.uploads.Contains("track.mp3"));
        }

        [Test]
        public void SaveTransferItems_ClearsDirtyFlag()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);
            TransferItemManager.TransfersDirty = true;

            TransferPersistence.SaveTransferItems();

            Assert.IsFalse(TransferItemManager.TransfersDirty);
        }

        [Test]
        public void SaveTransferItems_NullDLManager_ReturnsNull()
        {
            TransferItems.TransferItemManagerDL = null;
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var result = TransferPersistence.SaveTransferItems(force: true);

            Assert.IsNull(result);
        }

        [Test]
        public void SaveTransferItems_Force_IgnoresMaxSecondsUpdate()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            // First save
            TransferPersistence.SaveTransferItems(force: true);

            // Force ignores timing
            var result = TransferPersistence.SaveTransferItems(force: true);
            Assert.IsNotNull(result);
        }

        // --- Roundtrip tests ---

        [Test]
        public void Roundtrip_Download_SaveAndRestorePreservesItems()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\blue.mp3", "jazz", 12345);
            var ti2 = CreateTransferItem("bob", "\\music\\rock\\loud.mp3", "rock", 67890);
            TransferItems.TransferItemManagerDL.Add(ti1);
            TransferItems.TransferItemManagerDL.Add(ti2);

            var saved = TransferPersistence.SaveTransferItems(force: true);
            Assert.IsNotNull(saved);

            // Now restore from saved data
            TransferItems.TransferItemManagerDL = null;
            TransferPersistence.RestoreDownloadTransferItems(saved.Value.downloads, string.Empty);

            Assert.AreEqual(2, TransferItems.TransferItemManagerDL.AllTransferItems.Count);

            var r1 = TransferItems.TransferItemManagerDL.AllTransferItems[0];
            Assert.AreEqual("alice", r1.Username);
            Assert.AreEqual("\\music\\jazz\\blue.mp3", r1.FullFilename);
            Assert.AreEqual("blue.mp3", r1.Filename);
            Assert.AreEqual("jazz", r1.FolderName);
            Assert.AreEqual(12345, r1.Size);

            var r2 = TransferItems.TransferItemManagerDL.AllTransferItems[1];
            Assert.AreEqual("bob", r2.Username);
            Assert.AreEqual(67890, r2.Size);
        }

        [Test]
        public void Roundtrip_Upload_SaveAndRestorePreservesItems()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var ti = CreateTransferItem("charlie", "\\shared\\album\\track.mp3", "album", 99999);
            ti.isUpload = true;
            TransferItems.TransferItemManagerUploads.Add(ti);

            var saved = TransferPersistence.SaveTransferItems(force: true);
            Assert.IsNotNull(saved);

            TransferItems.TransferItemManagerUploads = null;
            TransferPersistence.RestoreUploadTransferItems(saved.Value.uploads, string.Empty);

            Assert.AreEqual(1, TransferItems.TransferItemManagerUploads.AllTransferItems.Count);
            var restored = TransferItems.TransferItemManagerUploads.AllTransferItems[0];
            Assert.AreEqual("charlie", restored.Username);
            Assert.AreEqual(99999, restored.Size);
            Assert.IsTrue(restored.isUpload);
        }

        [Test]
        public void Roundtrip_EmptyManagers_SaveAndRestoreSucceeds()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var saved = TransferPersistence.SaveTransferItems(force: true);
            Assert.IsNotNull(saved);

            TransferItems.TransferItemManagerDL = null;
            TransferItems.TransferItemManagerUploads = null;

            TransferPersistence.RestoreDownloadTransferItems(saved.Value.downloads, string.Empty);
            TransferPersistence.RestoreUploadTransferItems(saved.Value.uploads, string.Empty);

            Assert.AreEqual(0, TransferItems.TransferItemManagerDL.AllTransferItems.Count);
            Assert.AreEqual(0, TransferItems.TransferItemManagerUploads.AllTransferItems.Count);
        }

        [Test]
        public void Roundtrip_PreservesTransferState()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.State = TransferStates.Completed | TransferStates.Succeeded;
            ti.Progress = 100;
            ti.Failed = false;
            TransferItems.TransferItemManagerDL.Add(ti);

            var saved = TransferPersistence.SaveTransferItems(force: true);

            TransferItems.TransferItemManagerDL = null;
            TransferPersistence.RestoreDownloadTransferItems(saved.Value.downloads, string.Empty);

            var restored = TransferItems.TransferItemManagerDL.AllTransferItems[0];
            Assert.IsTrue(restored.State.HasFlag(TransferStates.Succeeded));
            Assert.AreEqual(100, restored.Progress);
            Assert.IsFalse(restored.Failed);
        }

        [Test]
        public void Roundtrip_PreservesLatin1Flags()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.WasFilenameLatin1Decoded = true;
            ti.WasFolderLatin1Decoded = true;
            TransferItems.TransferItemManagerDL.Add(ti);

            var saved = TransferPersistence.SaveTransferItems(force: true);

            TransferItems.TransferItemManagerDL = null;
            TransferPersistence.RestoreDownloadTransferItems(saved.Value.downloads, string.Empty);

            var restored = TransferItems.TransferItemManagerDL.AllTransferItems[0];
            Assert.IsTrue(restored.WasFilenameLatin1Decoded);
            Assert.IsTrue(restored.WasFolderLatin1Decoded);
        }

        [Test]
        public void Roundtrip_XmlIgnoredFieldsAreReset()
        {
            TransferItems.TransferItemManagerDL = new TransferItemManager();
            TransferItems.TransferItemManagerUploads = new TransferItemManager(true);

            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.InProcessing = true;
            TransferItems.TransferItemManagerDL.Add(ti);

            var saved = TransferPersistence.SaveTransferItems(force: true);

            TransferItems.TransferItemManagerDL = null;
            TransferPersistence.RestoreDownloadTransferItems(saved.Value.downloads, string.Empty);

            var restored = TransferItems.TransferItemManagerDL.AllTransferItems[0];
            // XmlIgnore fields should be default values after deserialization
            Assert.IsFalse(restored.InProcessing);
        }

        // --- Legacy restore edge cases ---

        [Test]
        public void RestoreDownloadTransferItemsLegacy_PopulatesFolderItems()
        {
            var items = new List<TransferItem>
            {
                CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3", "folder1"),
                CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3", "folder1"),
                CreateTransferItem("user1", "\\dir\\folder2\\file3.mp3", "folder2"),
            };
            string xml = SerializeTransferItems(items);

            TransferPersistence.RestoreDownloadTransferItems(xml, string.Empty);

            Assert.AreEqual(3, TransferItems.TransferItemManagerDL.AllTransferItems.Count);
            Assert.AreEqual(2, TransferItems.TransferItemManagerDL.AllFolderItems.Count);
        }
    }
}
