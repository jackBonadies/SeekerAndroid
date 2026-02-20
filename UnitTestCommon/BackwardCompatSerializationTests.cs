using NUnit.Framework;
using Seeker;
using Seeker.Serialization;
using Soulseek;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using VerifyNUnit;

namespace UnitTestCommon
{
    public class BackwardCompatSerializationTests
    {
        private static string GetPreferenceString(string keyName)
        {
            var xmlPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Set1", "SoulSeekPrefs.xml");
            var doc = new XmlDocument();
            doc.Load(xmlPath);
            var node = doc.SelectSingleNode($"//string[@name='{keyName}']");
            Assert.IsNotNull(node, $"Key '{keyName}' not found in SoulSeekPrefs.xml");
            return node.InnerText;
        }

        [Test]
        public void UnreadMessageUsernames_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_UnreadMessageUsernames_v2");
            var result = SerializationHelper.RestoreUnreadUsernamesFromString(raw);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("user123"));
            Assert.AreEqual((byte)0, result["user123"]);
        }

        [Test]
        public void UserOnlineAlerts_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_UserOnlineAlerts_v2");
            var result = SerializationHelper.RestoreUserOnlineAlertsFromString(raw);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("testuser4"));
            Assert.AreEqual((byte)0, result["testuser4"]);
        }

        [Test]
        public void SharedDirectoryInfo_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_SharedDirectoryInfo_v2");
            var result = SerializationHelper.DeserializeFromString<List<UploadDirectoryInfo>>(raw);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].UploadDataDirectoryUri.Contains("Soulseek%20Complete"));
            Assert.IsTrue(result[0].UploadDataDirectoryUriIsFromTree);
            Assert.IsFalse(result[0].IsLocked);
            Assert.IsFalse(result[0].IsHidden);
            Assert.IsNull(result[0].DisplayNameOverride);
            Assert.AreEqual(UploadDirectoryError.NoError, result[0].ErrorState);
        }

        [Test]
        public void UserList_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Cache_UserList_v2");
            var result = SerializationHelper.RestoreUserListFromString(raw);

            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0].UserData);
            Assert.AreEqual(294836, result[0].UserData.AverageSpeed);
            Assert.AreEqual(6892, result[0].UserData.FileCount);
            Assert.AreEqual(152, result[0].UserData.UploadCount);
            Assert.AreEqual(UserPresence.Online, result[0].UserData.Status);
            Assert.AreEqual(Seeker.UserRole.Friend, result[0].Role);
        }

        [Test]
        public void Messages_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_Messages_v2");
            var result = SerializationHelper.RestoreMessagesFromString(raw);

            Assert.IsTrue(result.ContainsKey("testingclient123"), "Missing outer key 'testingclient123'");

            var messagesForUser = result["testingclient123"];

            Assert.IsTrue(messagesForUser.ContainsKey("xiuxiu4"), "No messages with user 'xiuxiu4'");
            var messages = messagesForUser["xiuxiu4"];
            Assert.AreEqual(3, messages.Count);
            Assert.AreEqual("hello", messages[0].MessageText);
            Assert.AreEqual(-1, messages[0].Id);
            Assert.AreEqual(true, messages[0].FromMe);
            Assert.AreEqual(2026, messages[0].LocalDateTime.Year);
            Assert.AreEqual(16, messages[0].LocalDateTime.Day);

            Assert.AreEqual("hi", messages[1].MessageText);
            Assert.AreEqual(457177, messages[1].Id);
            Assert.AreEqual(false, messages[1].FromMe);
            Assert.AreEqual(2026, messages[1].LocalDateTime.Year);
            Assert.AreEqual(16, messages[1].LocalDateTime.Day);
        }

        [Test]
        public void SearchHistory_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_SearchHistoryArray");
            var serializer = new XmlSerializer(typeof(List<string>));
            List<string> result;
            using (var reader = new StringReader(raw))
            {
                result = (List<string>)serializer.Deserialize(reader);
            }

            Assert.IsTrue(result.Contains("ArtistA"));
            Assert.IsTrue(result.Contains("ArtistB AlbumB"));
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void RecentUsersList_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_RecentUsersList");
            var serializer = new XmlSerializer(typeof(List<string>));
            List<string> result;
            using (var reader = new StringReader(raw))
            {
                result = (List<string>)serializer.Deserialize(reader);
            }

            Assert.IsTrue(result.Contains("testuser4"));
        }

        [Test]
        public async Task TransferListDownloads_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_List");
            var serializer = new XmlSerializer(typeof(List<TransferItem>));
            List<TransferItem> result;
            using (var reader = new StringReader(raw))
            {
                result = (List<TransferItem>)serializer.Deserialize(reader);
            }

            await Verifier.Verify(result.Select(t => new
            {
                t.Filename,
                t.Username,
                t.FolderName,
                t.FullFilename,
                t.Progress,
                t.Failed,
                State = t.State.ToString(),
                t.Size,
                t.isUpload,
                t.QueueLength,
                t.FinalUri,
            }));
        }

        [Test]
        public async Task TransferListUploads_DeserializesFromDisk()
        {
            var raw = GetPreferenceString("Momento_Upload_List");
            var serializer = new XmlSerializer(typeof(List<TransferItem>));
            List<TransferItem> result;
            using (var reader = new StringReader(raw))
            {
                result = (List<TransferItem>)serializer.Deserialize(reader);
            }

            await Verifier.Verify(result.Select(t => new
            {
                t.Filename,
                t.Username,
                t.FolderName,
                t.FullFilename,
                t.Progress,
                t.Failed,
                State = t.State.ToString(),
                t.Size,
                t.isUpload,
                t.QueueLength,
                t.FinalUri,
            }));
        }
    }
}
