using Android.Content;
using Seeker.Helpers;
using System.Runtime.Serialization.Formatters.Binary;

namespace Seeker
{
#if BinaryFormatterAvailable
    public class SerializationMigrationHelper
    {
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

        private static void RemoveOldKey(ISharedPreferences sharedPreferences, string oldKey)
        {
            var editor = sharedPreferences.Edit();
            editor.Remove(oldKey);
            editor.Commit();
        }


        public static BinaryFormatter GetLegacyBinaryFormatter()
        {
            var bf = new BinaryFormatter();
            bf.Binder = new SerializationHelper.UpdatedNamespaceSerializationBinder();
            return bf;
        }

        public static string LegacyBinarySerializeToString<T>(T objectToSerialize)
        {
            using (System.IO.MemoryStream memStream = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = GetLegacyBinaryFormatter();
                formatter.Serialize(memStream, objectToSerialize);
                return System.Convert.ToBase64String(memStream.ToArray());
            }
        }

        public static T LegacyBinaryDeserializeFromString<T>(string base64String) where T : class
        {
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream(System.Convert.FromBase64String(base64String)))
            {
                BinaryFormatter binaryFormatter = GetLegacyBinaryFormatter();
                return binaryFormatter.Deserialize(mem) as T;
            }
        }

        public static bool MigrateUnreadUsernames(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var items = RestoreUnreadUsernamesFromString(oldKeyValue, true);
                var newString = SerializationHelper.SaveUnreadUsernamesToString(items);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        public static bool MigrateUserListIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if(AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var userListItems = RestoreUserListFromString(oldKeyValue, true);
                var newString = SerializationHelper.SaveUserListToString(userListItems);
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
                var newString = SerializationHelper.SaveUserNotesToString(userListItems);
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
                var newString = SerializationHelper.SaveUserOnlineAlertsToString(userListItems);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }
            return false;
        }

        internal static bool MigrateAutoJoinRoomsIfApplicable(ISharedPreferences sharedPreferences, string oldKey, string newKey)
        {
            if (AnythingToMigrate(sharedPreferences, oldKey))
            {
                var oldKeyValue = sharedPreferences.GetString(oldKey, string.Empty);
                var autoJoinRooms = RestoreAutoJoinRoomsListFromString(oldKeyValue, true);
                var newString = SerializationHelper.SaveAutoJoinRoomsListToString(autoJoinRooms);
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
                var newString = SerializationHelper.SaveNotifyRoomsListToString(autoJoinRooms);
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
                var newString = SerializationHelper.SaveSavedStateHeaderDictToString(items);
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
                var newString = SerializationHelper.SaveMessagesToString(autoJoinRooms);
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
                var infos = DeserializeFromString<System.Collections.Generic.List<UploadDirectoryInfo>>(sharedDirInfo, true);
                var newString = SerializationHelper.SerializeToString(infos);
                SaveToSharedPrefs(sharedPreferences, newKey, newString);
                RemoveOldKey(sharedPreferences, oldKey);
                return true;
            }

            return false;
        }

        // Legacy migration helpers that use BinaryFormatter
        private static T DeserializeFromString<T>(string base64String, bool legacy) where T : class
        {
            if (legacy)
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(System.Convert.FromBase64String(base64String)))
                {
                    BinaryFormatter binaryFormatter = GetLegacyBinaryFormatter();
                    return binaryFormatter.Deserialize(mem) as T;
                }
            }
            return SerializationHelper.DeserializeFromString<T>(base64String);
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<string, byte> RestoreUnreadUsernamesFromString(string s, bool legacy)
        {
            return DeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, byte>>(s, legacy);
        }

        private static System.Collections.Generic.List<UserListItem> RestoreUserListFromString(string s, bool legacy)
        {
            if (legacy)
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(System.Convert.FromBase64String(s)))
                {
                    BinaryFormatter binaryFormatter = GetLegacyBinaryFormatter();
                    return binaryFormatter.Deserialize(mem) as System.Collections.Generic.List<UserListItem>;
                }
            }
            return SerializationHelper.RestoreUserListFromString(s);
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<string, string> RestoreUserNotesFromString(string s, bool legacy)
        {
            return DeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, string>>(s, legacy);
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<string, byte> RestoreUserOnlineAlertsFromString(string s, bool legacy)
        {
            return DeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, byte>>(s, legacy);
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>> RestoreAutoJoinRoomsListFromString(string s, bool legacy)
        {
            return DeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>>>(s, legacy);
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>> RestoreNotifyRoomsListFromString(string s, bool legacy)
        {
            return DeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>>>(s, legacy);
        }

        private static System.Collections.Generic.Dictionary<int, SavedStateSearchTabHeader> RestoreSavedStateHeaderDictFromString(string s, bool legacy)
        {
            return DeserializeFromString<System.Collections.Generic.Dictionary<int, SavedStateSearchTabHeader>>(s, legacy);
        }

        private static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<Message>>> RestoreMessagesFromString(string s, bool legacy)
        {
            return DeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<Message>>>>(s, legacy);
        }

    }
#endif
}
