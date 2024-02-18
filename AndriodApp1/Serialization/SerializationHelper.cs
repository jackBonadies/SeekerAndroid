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
using log = Android.Util.Log;
using System.Text.Json.Serialization;
using Android.Util;
using System.IO;
using Soulseek;
using Android.Preferences;
using Android.Content;
using AndroidX.ConstraintLayout.Core.Parser;
using AndroidX.Core.Content;
using Java.Security.Interfaces;
using Java.IO;
using AndriodApp1.Helpers;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using AndriodApp1.Serialization;

namespace AndriodApp1
{
    public class SerializationHelper
    {
        
        private static readonly bool useBinarySerializer = false;

        public static MessagePackSerializerOptions BrowseResponseOptions
        {
            get
            {
                var browseResponseResolver = MessagePack.Resolvers.CompositeResolver.Create(
                    new IMessagePackFormatter[]
                    {
                        new BrowseResponseFormatter(),
                        new DirectoryItemFormatter(),
                        new FileItemFormatter(),
                        MessagePack.Formatters.TypelessFormatter.Instance

                    },
                    new IFormatterResolver[]
                    {
                        ContractlessStandardResolver.Instance
                    });
                return MessagePackSerializerOptions.Standard.WithResolver(browseResponseResolver);
            }
        }

        public static MessagePackSerializerOptions SearchResponseOptions
        {
            get
            {
                var searchResponseResolver = MessagePack.Resolvers.CompositeResolver.Create(
                    new IMessagePackFormatter[]
                    {
                        new SearchResponseFormatter(),
                        new FileItemFormatter(),
                        MessagePack.Formatters.TypelessFormatter.Instance
                    },
                    new IFormatterResolver[]
                    {
                        ContractlessStandardResolver.Instance
                    });
                return MessagePackSerializerOptions.Standard.WithResolver(searchResponseResolver);
            }
        }

        private static bool isBinaryFormatterSerialized(string base64string) 
        {
            return base64string.StartsWith(@"AAEAAAD/////");
        }

        public static string SerializeToString<T>(T objectToSerialize)
        {
            if(useBinarySerializer)
            {
                return LegacyBinarySerializeToString<T>(objectToSerialize);
            }
            else
            {
                return JsonSerializeToString<T>(objectToSerialize);

            }
        }

        public static T DeserializeFromString<T>(string serializedString, bool legacy = false) where T : class
        {
            if (legacy)
            {
                return LegacyBinaryDeserializeFromString<T>(serializedString);
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

        public static string JsonSerializeToString<T>(T objectToSerialize)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            return System.Text.Json.JsonSerializer.Serialize<T>(objectToSerialize, options);
        }

        public static string LegacyBinarySerializeToString<T>(T objectToSerialize)
        {
            using (System.IO.MemoryStream memStream = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memStream, objectToSerialize);
                return Convert.ToBase64String(memStream.ToArray());
            }
        }

        public static T LegacyBinaryDeserializeFromString<T>(string base64String) where T : class
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
        public static ConcurrentDictionary<string, string> RestoreUserNotesFromString(string base64userNotes, bool legacy = false)
        {
            if (base64userNotes == string.Empty)
            {
                return new ConcurrentDictionary<string, string>();
            }

            return DeserializeFromString<ConcurrentDictionary<string, string>>(base64userNotes, legacy);
        }



        public static string SaveUserOnlineAlertsToString(ConcurrentDictionary<string, byte> onlineAlertsDict)
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

        public static ConcurrentDictionary<string, byte> RestoreUserOnlineAlertsFromString(string base64onlineAlerts, bool legacy = false)
        {
            if (string.IsNullOrEmpty(base64onlineAlerts))
            {
                return new ConcurrentDictionary<string, byte>();
            }

            return DeserializeFromString<ConcurrentDictionary<string, byte>>(base64onlineAlerts, legacy);
        }



        public static string SaveUserListToString(List<UserListItem> userList)
        {
            if (userList == null || userList.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                var bytes = MessagePack.MessagePackSerializer.Serialize(userList, options: MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
                return Convert.ToBase64String(bytes);
            }
        }

        public static List<UserListItem> RestoreUserListFromString(string base64userList, bool restoreLegacy = false)
        {
            if (base64userList == string.Empty)
            {
                return new List<UserListItem>();
            }
            if (restoreLegacy)
            {
                return LegacyBinaryDeserializeFromString<List<UserListItem>>(base64userList);
            }
            else
            {
                return MessagePack.MessagePackSerializer.Deserialize<List<UserListItem>>(
                    Convert.FromBase64String(base64userList), 
                    options: MessagePack.Resolvers.TypelessContractlessStandardResolver.Options);
            }
        }


        public static string SaveSavedStateHeaderDictToString(Dictionary<int, SavedStateSearchTabHeader> savedTabHeaderStates)
        {
            return SerializeToString(savedTabHeaderStates);
        }

        public static Dictionary<int, SavedStateSearchTabHeader> RestoreSavedStateHeaderDictFromString(string savedTabHeaderString, bool legacy = false)
        {
            return DeserializeFromString<Dictionary<int, SavedStateSearchTabHeader>>(savedTabHeaderString, legacy);
        }


        public static string SaveAutoJoinRoomsListToString(ConcurrentDictionary<string, List<string>> autoJoinRoomNames)
        {
            return SerializeToString(autoJoinRoomNames);
        }

        public static ConcurrentDictionary<string, List<string>> RestoreAutoJoinRoomsListFromString(string joinedRooms, bool legacy = false)
        {
            return DeserializeFromString<ConcurrentDictionary<string, List<string>>>(joinedRooms, legacy);
        }


        public static string SaveNotifyRoomsListToString(ConcurrentDictionary<string, List<string>> notifyRoomsList)
        {
            return SerializeToString(notifyRoomsList);
        }

        public static ConcurrentDictionary<string, List<string>> RestoreNotifyRoomsListFromString(string notifyRoomsListString, bool legacy = false)
        {
            return DeserializeFromString<ConcurrentDictionary<string, List<string>>>(notifyRoomsListString, legacy);
        }

        public static string SaveUnreadUsernamesToString(ConcurrentDictionary<string, byte> unreadUsernames)
        {
            return SerializeToString(unreadUsernames);
        }

        public static ConcurrentDictionary<string, byte> RestoreUnreadUsernamesFromString(string unreadUsernames, bool legacy = false)
        {
            if (string.IsNullOrEmpty(unreadUsernames))
            {
                return new ConcurrentDictionary<string, byte>();
            }
            else
            {
                return DeserializeFromString<ConcurrentDictionary<string, byte>>(unreadUsernames, legacy);
            }
        }

        public static bool MigrateUnreadUsernames(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var items = RestoreUnreadUsernamesFromString(oldKeyValue, true);
                var newString = SaveUnreadUsernamesToString(items);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }


        public static string SaveMessagesToString(ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>> rootMessages)
        {
            var byteArray = MessagePack.MessagePackSerializer.Serialize(rootMessages);
            return Convert.ToBase64String(byteArray);
        }

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>> RestoreMessagesFromString(string rootMessagesString, bool useLegacy = false)
        {
            if (useLegacy)
            {
                return LegacyBinaryDeserializeFromString<ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>>>(rootMessagesString);
            }
            else
            {
                var bytesArray = Convert.FromBase64String(rootMessagesString);
                return MessagePack.MessagePackSerializer.Deserialize<ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>>>(bytesArray);
            }
        }


        public static byte[] SaveSearchResponsesToByteArray(List<SearchResponse> responses)
        {

            var byteArray = MessagePack.MessagePackSerializer.Serialize(responses, options: SearchResponseOptions);
            return byteArray;
        }

        public static List<SearchResponse> RestoreSearchResponsesFromStream(System.IO.Stream inputStream, bool legacy = false)
        {
            if(legacy)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(inputStream) as List<SearchResponse>;
            }
            else
            {
                return MessagePack.MessagePackSerializer.Deserialize<List<SearchResponse>>(inputStream, options: SearchResponseOptions);
            }
        }

        private static bool AnythingToMigrate(ISharedPreferences sharedPreferences, string oldKey)
        {
            if (!sharedPreferences.Contains(oldKey))
            {
                return false;
            }
            var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
            if (string.IsNullOrEmpty(oldKeyValue))
            {
                var editor = sharedPreferences.Edit();
                editor.Remove(oldKey);
                editor.Commit();
                return false;
            }
            return true;
        }

        private static void SaveToSharedPrefs(ISharedPreferences sharedPreferences, string newKey, string stringToSave)
        {
            var editor = sharedPreferences.Edit();
            editor.PutString(newKey, stringToSave);
            editor.Commit();
        }

        public static bool MigrateUserListIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if(AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var userListItems = RestoreUserListFromString(oldKeyValue, true);
                var newString = SaveUserListToString(userListItems);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        public static bool MigrateUserNotesIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var userListItems = RestoreUserNotesFromString(oldKeyValue, true);
                var newString = SaveUserNotesToString(userListItems);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        public static bool MigrateOnlineAlertsIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var userListItems = RestoreUserOnlineAlertsFromString(oldKeyValue, true);
                var newString = SaveUserOnlineAlertsToString(userListItems);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        private static void RemoveOldKey(ISharedPreferences sharedPreferences, string oldKey)
        {
            var editor = sharedPreferences.Edit();
            editor.Remove(oldKey);
            editor.Commit();
        }

        internal static bool MigrateAutoJoinRoomsIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var autoJoinRooms = RestoreAutoJoinRoomsListFromString(oldKeyValue, true);
                var newString = SaveAutoJoinRoomsListToString(autoJoinRooms);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        internal static bool MigrateNotifyRoomsIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var autoJoinRooms = RestoreNotifyRoomsListFromString(oldKeyValue, true);
                var newString = SaveNotifyRoomsListToString(autoJoinRooms);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        public static void MigrateWishlistTabs(Context context)
        {
            SearchTabHelper.MigrateAllSearchTabsFromDisk(context);
        }

        internal static bool MigrateHeaderState(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var items = RestoreSavedStateHeaderDictFromString(oldKeyValue, true);
                var newString = SaveSavedStateHeaderDictToString(items);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        internal static bool MigratedMessages(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var autoJoinRooms = RestoreMessagesFromString(oldKeyValue, true);
                var newString = SaveMessagesToString(autoJoinRooms);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        public static bool MigrateUploadDirectoryInfoIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                string sharedDirInfo = sharedPreferences.GetString(oldKey, string.Empty);
                var infos = SerializationHelper.DeserializeFromString<List<UploadDirectoryInfo>>(sharedDirInfo, true);
                var newString = SerializationHelper.SerializeToString(infos);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            
            return false;
        }
    }
}