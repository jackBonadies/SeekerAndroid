using AndriodApp1.Messages;
using Android.Hardware.Camera2;
using AndroidX.DocumentFile.Provider;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AndriodApp1
{
    public class SerializationHelper
    {
        private static readonly bool useBinarySerializer = false;
        private static bool isBinaryFormatterSerialized(string base64string)
        {
            return base64string.StartsWith(@"AAEAAAD/////");
        }

        public static string SerializeToString<T>(T objectToSerialize)
        {
            if(useBinarySerializer)
            {
                return BinarySerializeToString<T>(objectToSerialize);
            }
            else
            {
                return JsonSerializeToString<T>(objectToSerialize);

            }
        }

        public static T DeserializeFromString<T>(string serializedString) where T : class
        {
            if (isBinaryFormatterSerialized(serializedString))
            {
                return BinaryDeserializeFromString<T>(serializedString);
            }
            else
            {
                return JsonDeserializeFromString<T>(serializedString);
            }
        }

        private static T JsonDeserializeFromString<T>(string serializedString)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            return System.Text.Json.JsonSerializer.Deserialize<T>(serializedString, options);
        }

        private static string JsonSerializeToString<T>(T objectToSerialize)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            return System.Text.Json.JsonSerializer.Serialize<T>(objectToSerialize, options);
        }

        private static string BinarySerializeToString<T>(T objectToSerialize)
        {
            using (System.IO.MemoryStream userNotesStream = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(userNotesStream, objectToSerialize);
                return Convert.ToBase64String(userNotesStream.ToArray());
            }
        }

        public static T BinaryDeserializeFromString<T>(string base64String) where T : class
        {
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(base64String)))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(mem) as T;
            }
        }

        public static string SaveUserNotesToString(ConcurrentDictionary<string, string> userNotes)
        {
            if (userNotes == null || userNotes.Keys.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                return SerializeToString(userNotes);
            }
        }
        public static ConcurrentDictionary<string, string> RestoreUserNotesFromString(string base64userNotes)
        {
            if (base64userNotes == string.Empty)
            {
                return new ConcurrentDictionary<string, string>();
            }

            return DeserializeFromString<ConcurrentDictionary<string, string>>(base64userNotes);
        }



        public static string SaveUserOnlineAlertsFromString(ConcurrentDictionary<string, byte> onlineAlertsDict)
        {
            if (onlineAlertsDict == null || onlineAlertsDict.Keys.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                return SerializeToString(onlineAlertsDict);
            }
        }

        public static ConcurrentDictionary<string, byte> RestoreUserOnlineAlertsFromString(string base64onlineAlerts)
        {
            if (string.IsNullOrEmpty(base64onlineAlerts))
            {
                return new ConcurrentDictionary<string, byte>();
            }

            return DeserializeFromString<ConcurrentDictionary<string, byte>>(base64onlineAlerts);
        }



        public static string SaveUserListToString(List<UserListItem> userList)
        {
            if (userList == null || userList.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                return SerializeToString(userList);
            }
        }

        public static List<UserListItem> RestoreUserListFromString(string base64userList)
        {
            if (base64userList == string.Empty)
            {
                return new List<UserListItem>();
            }
            if (isBinaryFormatterSerialized(base64userList))
            {
                return BinaryDeserializeFromString<List<UserListItem>>(base64userList);
            }
            else
            {
                return null; // TODOSERIALIZE
            }
        }


        public static string SaveSavedStateHeaderDictToString(Dictionary<int, SavedStateSearchTabHeader> savedTabHeaderStates)
        {
            return SerializeToString(savedTabHeaderStates);
        }

        public static Dictionary<int, SavedStateSearchTabHeader> RestoreSavedStateHeaderDictFromString(string savedTabHeaderString)
        {
            return DeserializeFromString<Dictionary<int, SavedStateSearchTabHeader>>(savedTabHeaderString);
        }


        public static string SaveAutoJoinRoomsListToString(ConcurrentDictionary<string, List<string>> autoJoinRoomNames)
        {
            return SerializeToString(autoJoinRoomNames);
        }

        public static ConcurrentDictionary<string, List<string>> RestoreAutoJoinRoomsListFromString(string joinedRooms)
        {
            return DeserializeFromString<ConcurrentDictionary<string, List<string>>>(joinedRooms);
        }


        public static string SaveNotifyRoomsListToString(ConcurrentDictionary<string, List<string>> notifyRoomsList)
        {
            return SerializeToString(notifyRoomsList);
        }

        public static ConcurrentDictionary<string, List<string>> RestoreNotifyRoomsListFromString(string notifyRoomsListString)
        {
            return DeserializeFromString<ConcurrentDictionary<string, List<string>>>(notifyRoomsListString);
        }

        public static string SaveUnreadUsernamesToString(ConcurrentDictionary<string, byte> unreadUsernames)
        {
            return SerializeToString(unreadUsernames);
        }

        public static ConcurrentDictionary<string, byte> RestoreUnreadUsernamesFromString(string unreadUsernames)
        {
            if (string.IsNullOrEmpty(unreadUsernames))
            {
                return new ConcurrentDictionary<string, byte>();
            }
            else
            {
                return DeserializeFromString<ConcurrentDictionary<string, byte>>(unreadUsernames);
            }
        }


        public static string SaveMessagesToString(ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>> rootMessages)
        {
            return SerializeToString(rootMessages);
        }

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>> RestoreMessagesFromString(string rootMessagesString)
        {
            if (isBinaryFormatterSerialized(rootMessagesString))
            {
                return BinaryDeserializeFromString<ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>>>(rootMessagesString);
            }
            else
            {
                return null; // TODOSERIALIZE
            }
        }
    }

    /// <summary>
    /// TODO move PreferenceHelper to Common. Requires Moving UserListItem to common and then fixing the binary resolver
    /// </summary>
    public class SerializationHelperTests
    {
        public static void Test()
        {

            Dictionary<int, SavedStateSearchTabHeader> savedStates = new Dictionary<int, SavedStateSearchTabHeader>();
            var savedStateSearchTab = new SavedStateSearchTabHeader();
            savedStateSearchTab.GetType().GetProperty("LastSearchResultsCount").SetValue(savedStateSearchTab, 123);
            savedStateSearchTab.GetType().GetProperty("LastSearchTerm").SetValue(savedStateSearchTab, "fav artist");
            savedStateSearchTab.GetType().GetProperty("LastRanTime").SetValue(savedStateSearchTab, 123000000L);
            savedStates[1] = savedStateSearchTab;

            var ser12 = SerializationHelper.SaveSavedStateHeaderDictToString(savedStates);
            var restored12 = SerializationHelper.RestoreSavedStateHeaderDictFromString(ser12);

            Debug.Assert(savedStates[1].LastSearchTerm == restored12[1].LastSearchTerm);


            var uploadDir = new UploadDirectoryInfo("uploadUriTEST", true, false, false, "my fav folder");
            uploadDir.ErrorState = UploadDirectoryError.CannotWrite;
            uploadDir.IsSubdir = true;

            List<UploadDirectoryInfo> uploadDirectoryInfos = new List<UploadDirectoryInfo>();
            uploadDirectoryInfos.Add(uploadDir);

            var serInfos = SerializationHelper.SerializeToString(uploadDirectoryInfos);
            var infos = SerializationHelper.DeserializeFromString<List<UploadDirectoryInfo>>(serInfos);

            Debug.Assert(infos.Count == uploadDirectoryInfos.Count);
            Debug.Assert(infos[0].UploadDataDirectoryUri == uploadDirectoryInfos[0].UploadDataDirectoryUri);
            Debug.Assert(infos[0].ErrorState != uploadDirectoryInfos[0].ErrorState);

            ConcurrentDictionary<string, byte> unreadUsernames = new ConcurrentDictionary<string, byte>();
            unreadUsernames["testuser1"] = 0;
            unreadUsernames["testuser2"] = 0;
            unreadUsernames["testuser3"] = 1;
            unreadUsernames["testuser4"] = 0;

            var ser = SerializationHelper.SaveUnreadUsernamesToString(unreadUsernames);
            var restored = SerializationHelper.RestoreUnreadUsernamesFromString(ser);

            Debug.Assert(unreadUsernames.Count == restored.Count);
            Debug.Assert(unreadUsernames["testuser1"] == restored["testuser1"]);

            ConcurrentDictionary<string, List<string>> notifyRooms = new ConcurrentDictionary<string, List<string>>();
            notifyRooms["testuser1"] = new List<string>() { "music", "music2", "music3" };
            notifyRooms["testuser2"] = new List<string>() { "musically", "musically2", "musically3" };

            var ser1 = SerializationHelper.SaveNotifyRoomsListToString(notifyRooms);
            var restored1 = SerializationHelper.RestoreNotifyRoomsListFromString(ser1);

            Debug.Assert(restored1.Count == notifyRooms.Count);
            Debug.Assert(restored1["testuser1"].Count == notifyRooms["testuser1"].Count);
            Debug.Assert(restored1["testuser1"].First() == notifyRooms["testuser1"].First());

            List<UserListItem> list = new List<UserListItem>();
            list.Add(new UserListItem()
            {
                DoesNotExist = true,
                Role = UserRole.Friend,
                Username = "helloworld",
                UserStatus = new Soulseek.UserStatus(Soulseek.UserPresence.Offline, true),
                UserData = new Soulseek.UserData("hellowworld", Soulseek.UserPresence.Online, 100, 11, 12, 14, "en", 4),
                UserInfo = new Soulseek.UserInfo("testing", 1, 3, true)
            });
            list.Add(new UserListItem()
            {
                DoesNotExist = false,
                Role = UserRole.Ignored,
                Username = "helloworld1",
                UserStatus = null,
                UserData = new Soulseek.UserData("hellowworld", Soulseek.UserPresence.Online, 100, 11, 12, 14, "en")
            });
            list.Add(new UserListItem()
            {
                DoesNotExist = true,
                Role = UserRole.Friend,
                Username = "helloworld2",
                UserStatus = null,
                UserData = null,
            });

            var userSer = SerializationHelper.SaveUserListToString(list);
            var restoredList = SerializationHelper.RestoreUserListFromString(userSer);
        }

    }
}