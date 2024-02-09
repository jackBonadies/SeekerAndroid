using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace AndriodApp1
{
    public class PreferenceHelper
    {
        private static bool isBinaryFormatterSerialized(string base64string)
        {
            return base64string.StartsWith(@"AAEAAAD/////");
        }
        public static string BinarySerializeToString<T>(T objectToSerialize)
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

        public static string SaveUserNotesToString(System.Collections.Concurrent.ConcurrentDictionary<string, string> userNotes)
        {
            if (userNotes == null || userNotes.Keys.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                return BinarySerializeToString(userNotes);
            }
        }
        public static System.Collections.Concurrent.ConcurrentDictionary<string, string> RestoreUserNotesFromString(string base64userNotes)
        {
            if (base64userNotes == string.Empty)
            {
                return new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            }

            if (isBinaryFormatterSerialized(base64userNotes))
            {
                return BinaryDeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, string>>(base64userNotes);
            }
            else
            {
                //json method
                return null;
            }
        }



        public static string SaveUserOnlineAlertsFromString(System.Collections.Concurrent.ConcurrentDictionary<string, byte> onlineAlertsDict)
        {
            if (onlineAlertsDict == null || onlineAlertsDict.Keys.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                return BinarySerializeToString(onlineAlertsDict);
            }
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte> RestoreUserOnlineAlertsFromString(string base64onlineAlerts)
        {
            if (string.IsNullOrEmpty(base64onlineAlerts))
            {
                return new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
            }

            if (isBinaryFormatterSerialized(base64onlineAlerts))
            {
                return BinaryDeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, byte>>(base64onlineAlerts);
            }
            else
            {
                //json method
                return null;
            }
        }



        public static string SaveUserListToString(List<UserListItem> userList)
        {
            if (userList == null || userList.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                return BinarySerializeToString(userList);
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
                return null;
            }
        }


        public static string SaveAutoJoinRoomsListToString(System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> autoJoinRoomNames)
        {
            return BinarySerializeToString(autoJoinRoomNames);
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RestoreAutoJoinRoomsListFromString(string joinedRooms)
        {
            if (isBinaryFormatterSerialized(joinedRooms))
            {
                return BinaryDeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>>(joinedRooms);
            }
            else
            {
                return null;
            }
        }


        public static string SaveNotifyRoomsListToString(System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> notifyRoomsList)
        {
            return BinarySerializeToString(notifyRoomsList);
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RestoreNotifyRoomsListFromString(string notifyRoomsListString)
        {
            if (isBinaryFormatterSerialized(notifyRoomsListString))
            {
                return BinaryDeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>>(notifyRoomsListString);
            }
            else
            {
                return null;
            }
        }

        public static string SaveUnreadUsernamesToString(System.Collections.Concurrent.ConcurrentDictionary<string, byte> unreadUsernames)
        {
            return BinarySerializeToString(unreadUsernames);
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte> RestoreUnreadUsernamesFromString(string unreadUsernames)
        {
            if (string.IsNullOrEmpty(unreadUsernames))
            {
                return new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
            }
            else
            {
                if (isBinaryFormatterSerialized(unreadUsernames))
                {
                    return BinaryDeserializeFromString<System.Collections.Concurrent.ConcurrentDictionary<string, byte>>(unreadUsernames);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}