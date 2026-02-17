using NUnit.Framework;
using Seeker;
using Seeker.Serialization;
using Soulseek;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

namespace UnitTestCommon
{
    public class SerializationTests
    {
        [Test]
        public void UserListItem_RoundTrip_MessagePack()
        {
            var list = new List<UserListItem>
            {
                new UserListItem("user1", Seeker.UserRole.Friend)
                {
                    DoesNotExist = true,
                    UserStatus = new UserStatus(UserPresence.Offline, true),
                    UserData = new UserData("user1", UserPresence.Online, 100, 11, 12, 14, "en", 4),
                    UserInfo = new UserInfo("testing", true, new byte[] { 0x10, 0x11 }, 1, 3, true)
                },
                new UserListItem("user2", Seeker.UserRole.Ignored)
                {
                    DoesNotExist = false,
                    UserStatus = null,
                    UserData = new UserData("user2", UserPresence.Online, 200, 5, 6, 7, "fr")
                },
                new UserListItem("user3", Seeker.UserRole.Friend)
                {
                    DoesNotExist = true,
                    UserStatus = null,
                    UserData = null,
                    UserInfo = null
                }
            };

            var serialized = SerializationHelper.SaveUserListToString(list);
            Assert.IsNotEmpty(serialized);

            var restored = SerializationHelper.RestoreUserListFromString(serialized);
            Assert.AreEqual(list.Count, restored.Count);

            Assert.AreEqual("user1", restored[0].Username);
            Assert.AreEqual(Seeker.UserRole.Friend, restored[0].Role);
            Assert.IsTrue(restored[0].DoesNotExist);
            Assert.IsNotNull(restored[0].UserStatus);
            Assert.IsTrue(restored[0].UserStatus.IsPrivileged);
            Assert.AreEqual(UserPresence.Offline, restored[0].UserStatus.Presence);
            Assert.IsNotNull(restored[0].UserData);
            Assert.IsNotNull(restored[0].UserInfo);
            Assert.AreEqual("testing", restored[0].UserInfo.Description);

            Assert.AreEqual("user2", restored[1].Username);
            Assert.AreEqual(Seeker.UserRole.Ignored, restored[1].Role);
            Assert.IsFalse(restored[1].DoesNotExist);
            Assert.IsNull(restored[1].UserStatus);

            Assert.AreEqual("user3", restored[2].Username);
            Assert.IsNull(restored[2].UserData);
            Assert.IsNull(restored[2].UserInfo);
        }

        [Test]
        public void UserListItem_EmptyList_RoundTrip()
        {
            var serialized = SerializationHelper.SaveUserListToString(new List<UserListItem>());
            Assert.AreEqual(string.Empty, serialized);

            var restored = SerializationHelper.RestoreUserListFromString(string.Empty);
            Assert.AreEqual(0, restored.Count);
        }

        [Test]
        public void Message_RoundTrip_MessagePack()
        {
            var messages = new ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>>();
            var innerDict = new ConcurrentDictionary<string, List<Message>>();
            var msgList = new List<Message>
            {
                new Message("userX", 1, false, DateTime.Now, DateTime.UtcNow, "hello", false),
                new Message("userY", 2, true, DateTime.Now, DateTime.UtcNow, "hi there", true, SentStatus.Success),
                new Message(DateTime.Now, DateTime.UtcNow, SpecialMessageCode.Disconnect, "Disconnected at 12:00")
            };
            innerDict["userX"] = msgList;
            messages["user1"] = innerDict;

            var serialized = SerializationHelper.SaveMessagesToString(messages);
            Assert.IsNotEmpty(serialized);

            var restored = SerializationHelper.RestoreMessagesFromString(serialized);
            Assert.AreEqual(1, restored.Count);
            Assert.IsTrue(restored.ContainsKey("user1"));
            Assert.IsTrue(restored["user1"].ContainsKey("userX"));

            var restoredList = restored["user1"]["userX"];
            Assert.AreEqual(3, restoredList.Count);

            Assert.AreEqual("userX", restoredList[0].Username);
            Assert.AreEqual(1, restoredList[0].Id);
            Assert.AreEqual("hello", restoredList[0].MessageText);
            Assert.IsFalse(restoredList[0].FromMe);

            Assert.AreEqual("userY", restoredList[1].Username);
            Assert.AreEqual(SentStatus.Success, restoredList[1].SentMsgStatus);
            Assert.IsTrue(restoredList[1].FromMe);

            Assert.AreEqual(SpecialMessageCode.Disconnect, restoredList[2].SpecialCode);
            Assert.AreEqual("Disconnected at 12:00", restoredList[2].MessageText);
            Assert.AreEqual(-2, restoredList[2].Id);
        }

        [Test]
        public void UploadDirectoryInfo_RoundTrip_Json()
        {
            var infos = new List<UploadDirectoryInfo>
            {
                new UploadDirectoryInfo("content://some/uri", true, false, false, "My Music"),
                new UploadDirectoryInfo("content://other/uri", false, true, true, null)
            };

            var serialized = SerializationHelper.SerializeToString(infos);
            var restored = SerializationHelper.DeserializeFromString<List<UploadDirectoryInfo>>(serialized);

            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual("content://some/uri", restored[0].UploadDataDirectoryUri);
            Assert.IsTrue(restored[0].UploadDataDirectoryUriIsFromTree);
            Assert.IsFalse(restored[0].IsLocked);
            Assert.AreEqual("My Music", restored[0].DisplayNameOverride);

            Assert.AreEqual("content://other/uri", restored[1].UploadDataDirectoryUri);
            Assert.IsFalse(restored[1].UploadDataDirectoryUriIsFromTree);
            Assert.IsTrue(restored[1].IsLocked);
            Assert.IsTrue(restored[1].IsHidden);
            Assert.IsNull(restored[1].DisplayNameOverride);

            // ErrorState is NonSerialized, should default to NoError
            Assert.AreEqual(UploadDirectoryError.NoError, restored[0].ErrorState);
        }

        [Test]
        public void SavedStateSearchTabHeader_RoundTrip_Json()
        {
            var header = SavedStateSearchTabHeader.GetSavedStateHeaderFromTab("my search term", 42, 637500000000000000L);

            var dict = new Dictionary<int, SavedStateSearchTabHeader> { { 1, header } };
            var serialized = SerializationHelper.SaveSavedStateHeaderDictToString(dict);
            var restored = SerializationHelper.RestoreSavedStateHeaderDictFromString(serialized);

            Assert.AreEqual(1, restored.Count);
            Assert.IsTrue(restored.ContainsKey(1));
            Assert.AreEqual("my search term", restored[1].LastSearchTerm);
            Assert.AreEqual(42, restored[1].LastSearchResultsCount);
            Assert.AreEqual(637500000000000000L, restored[1].LastRanTime);
        }

        [Test]
        public void SearchResponse_RoundTrip_MessagePack()
        {
            var files = new List<Soulseek.File>
            {
                new Soulseek.File(1, "song.mp3", 5000000L, "mp3",
                    new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 320) },
                    false, false),
                new Soulseek.File(2, "album.flac", 30000000L, "flac", null, true, false)
            };
            var lockedFiles = new List<Soulseek.File>
            {
                new Soulseek.File(3, "locked.mp3", 4000000L, "mp3", null, false, false)
            };

            var responses = new List<SearchResponse>
            {
                new SearchResponse("testuser", 123, 2, 50000, 5L, files, lockedFiles)
            };

            var bytes = SerializationHelper.SaveSearchResponsesToByteArray(responses);
            Assert.IsNotNull(bytes);
            Assert.Greater(bytes.Length, 0);

            using (var stream = new MemoryStream(bytes))
            {
                var restored = SerializationHelper.RestoreSearchResponsesFromStream(stream);
                Assert.AreEqual(1, restored.Count);
                Assert.AreEqual("testuser", restored[0].Username);
                Assert.AreEqual(123, restored[0].Token);
                Assert.AreEqual(2, restored[0].FreeUploadSlots);
                Assert.AreEqual(50000, restored[0].UploadSpeed);

                var restoredFiles = restored[0].Files.ToList();
                Assert.AreEqual(2, restoredFiles.Count);
                Assert.AreEqual("song.mp3", restoredFiles[0].Filename);
                Assert.AreEqual(5000000L, restoredFiles[0].Size);
                Assert.AreEqual(1, restoredFiles[0].Attributes.Count);
                Assert.AreEqual(320, restoredFiles[0].Attributes.First().Value);

                Assert.AreEqual("album.flac", restoredFiles[1].Filename);
                Assert.IsTrue(restoredFiles[1].IsLatin1Decoded);

                var restoredLocked = restored[0].LockedFiles.ToList();
                Assert.AreEqual(1, restoredLocked.Count);
                Assert.AreEqual("locked.mp3", restoredLocked[0].Filename);
            }
        }

        [Test]
        public void BrowseResponse_RoundTrip_MessagePack()
        {
            var files1 = new List<Soulseek.File>
            {
                new Soulseek.File(1, "track01.mp3", 3000000L, "mp3", null, false, false)
            };
            var files2 = new List<Soulseek.File>
            {
                new Soulseek.File(2, "track02.flac", 20000000L, "flac",
                    new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 1411) },
                    false, false)
            };

            var dirs = new List<Soulseek.Directory>
            {
                new Soulseek.Directory("Music/Album1", files1),
                new Soulseek.Directory("Music/Album2", files2)
            };
            var lockedDirs = new List<Soulseek.Directory>
            {
                new Soulseek.Directory("Private/Stuff", new List<Soulseek.File>())
            };

            var browseResponse = new BrowseResponse(dirs, lockedDirs);

            var bytes = MessagePackSerializer.Serialize(browseResponse, options: SerializationHelper.BrowseResponseOptions);
            var restored = MessagePackSerializer.Deserialize<BrowseResponse>(bytes, options: SerializationHelper.BrowseResponseOptions);

            var restoredDirs = restored.Directories.ToList();
            Assert.AreEqual(2, restoredDirs.Count);
            Assert.AreEqual("Music/Album1", restoredDirs[0].Name);
            Assert.AreEqual(1, restoredDirs[0].Files.Count);
            Assert.AreEqual("track01.mp3", restoredDirs[0].Files.First().Filename);

            Assert.AreEqual("Music/Album2", restoredDirs[1].Name);
            Assert.AreEqual(1, restoredDirs[1].Files.Count);
            Assert.AreEqual("track02.flac", restoredDirs[1].Files.First().Filename);
            Assert.AreEqual(1411, restoredDirs[1].Files.First().Attributes.First().Value);

            var restoredLockedDirs = restored.LockedDirectories.ToList();
            Assert.AreEqual(1, restoredLockedDirs.Count);
            Assert.AreEqual("Private/Stuff", restoredLockedDirs[0].Name);
            Assert.AreEqual(0, restoredLockedDirs[0].Files.Count);
        }

        [Test]
        public void SerializeToString_DeserializeFromString_Json_GenericRoundTrip()
        {
            var notes = new ConcurrentDictionary<string, string>();
            notes["user1"] = "irl friend";
            notes["user2"] = "shares rare albums";

            var serialized = SerializationHelper.SaveUserNotesToString(notes);
            var restored = SerializationHelper.RestoreUserNotesFromString(serialized);

            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual("irl friend", restored["user1"]);
            Assert.AreEqual("shares rare albums", restored["user2"]);
        }

        [Test]
        public void UnreadUsernames_RoundTrip()
        {
            var unread = new ConcurrentDictionary<string, byte>();
            unread["userX"] = 0;
            unread["userY"] = 1;

            var serialized = SerializationHelper.SaveUnreadUsernamesToString(unread);
            var restored = SerializationHelper.RestoreUnreadUsernamesFromString(serialized);

            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual((byte)0, restored["userX"]);
            Assert.AreEqual((byte)1, restored["userY"]);
        }

        [Test]
        public void UnreadUsernames_EmptyOrNull_ReturnsEmpty()
        {
            var restored1 = SerializationHelper.RestoreUnreadUsernamesFromString(string.Empty);
            Assert.AreEqual(0, restored1.Count);

            var restored2 = SerializationHelper.RestoreUnreadUsernamesFromString(null);
            Assert.AreEqual(0, restored2.Count);
        }

        [Test]
        public void AutoJoinRooms_RoundTrip()
        {
            var rooms = new ConcurrentDictionary<string, List<string>>();
            rooms["user1"] = new List<string> { "music", "jazz" };
            rooms["user2"] = new List<string> { "rock" };

            var serialized = SerializationHelper.SaveAutoJoinRoomsListToString(rooms);
            var restored = SerializationHelper.RestoreAutoJoinRoomsListFromString(serialized);

            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual(2, restored["user1"].Count);
            Assert.AreEqual("music", restored["user1"][0]);
            Assert.AreEqual("jazz", restored["user1"][1]);
        }

        [Test]
        public void OnlineAlerts_RoundTrip()
        {
            var alerts = new ConcurrentDictionary<string, byte>();
            alerts["friend1"] = 0;
            alerts["friend2"] = 0;

            var serialized = SerializationHelper.SaveUserOnlineAlertsToString(alerts);
            Assert.IsNotEmpty(serialized);

            var restored = SerializationHelper.RestoreUserOnlineAlertsFromString(serialized);
            Assert.AreEqual(2, restored.Count);
            Assert.IsTrue(restored.ContainsKey("friend1"));
        }

        [Test]
        public void OnlineAlerts_Empty_ReturnsEmpty()
        {
            var restored = SerializationHelper.RestoreUserOnlineAlertsFromString(null);
            Assert.AreEqual(0, restored.Count);
        }
    }
}
