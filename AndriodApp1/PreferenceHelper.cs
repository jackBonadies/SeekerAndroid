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
        public static string SerializeToString<T>(T objectToSerialize)
        {
            using (System.IO.MemoryStream userNotesStream = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(userNotesStream, objectToSerialize);
                return Convert.ToBase64String(userNotesStream.ToArray());
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
                return SerializeToString(userNotes);
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
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(base64userNotes)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, string>;
                }
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
                return SerializeToString(onlineAlertsDict);
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
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(base64onlineAlerts)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, byte>;
                }
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
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(base64userList)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(mem) as List<UserListItem>;
                }
            }
            else
            {
                return null;
            }
        }


        public static string SaveAutoJoinRoomsListToString(System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> autoJoinRoomNames)
        {
            return SerializeToString(autoJoinRoomNames);
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RestoreAutoJoinRoomsListFromString(string joinedRooms)
        {
            if (isBinaryFormatterSerialized(joinedRooms))
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(joinedRooms)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>;
                }
            }
            else
            {
                return null;
            }
        }


        public static string SaveNotifyRoomsListToString(System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> notifyRoomsList)
        {
            return SerializeToString(notifyRoomsList);
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RestoreNotifyRoomsListFromString(string notifyRoomsListString)
        {
            if (isBinaryFormatterSerialized(notifyRoomsListString))
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(notifyRoomsListString)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    return binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>;
                }
            }
            else
            {
                return null;
            }
        }

        public static string SaveUnreadUsernamesToString(System.Collections.Concurrent.ConcurrentDictionary<string, byte> unreadUsernames)
        {
            return SerializeToString(unreadUsernames);
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
                    using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(unreadUsernames)))
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        return binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, byte>;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}