using Seeker.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Soulseek;

namespace Seeker
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

        public static MessagePackSerializerOptions UserListOptions
        {
            get
            {
                var searchResponseResolver = MessagePack.Resolvers.CompositeResolver.Create(
                    new IMessagePackFormatter[]
                    {
                        new UserListItemFormatter(),
                        new UserStatusFormatter(),
                        new UserInfoFormatter(),
                        new UserDataFormatter(),
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
            #if BinaryFormatterAvailable

            if(useBinarySerializer)
            {
                return LegacyBinarySerializeToString<T>(objectToSerialize);
            }

            #endif

            return JsonSerializeToString<T>(objectToSerialize);
    }

        public static T DeserializeFromString<T>(string serializedString) where T : class
        {
                return JsonDeserializeFromString<T>(serializedString);
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

        public class UpdatedNamespaceSerializationBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                string currentAssembly = typeof(UpdatedNamespaceSerializationBinder).Assembly.FullName;

                typeName = typeName.Replace("AndriodApp1", "Seeker");
                assemblyName = assemblyName.Replace("AndriodApp1", "Seeker");
                var type = Type.GetType($"{typeName}, {assemblyName}");
                return type;
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
                var bytes = MessagePack.MessagePackSerializer.Serialize(userList, options: UserListOptions);
                return Convert.ToBase64String(bytes);
            }
        }

        public static List<UserListItem> RestoreUserListFromString(string base64userList)
        {
            if (base64userList == string.Empty)
            {
                return new List<UserListItem>();
            }
            return MessagePack.MessagePackSerializer.Deserialize<List<UserListItem>>(
                Convert.FromBase64String(base64userList),
                options: UserListOptions);
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
            var byteArray = MessagePack.MessagePackSerializer.Serialize(rootMessages);
            return Convert.ToBase64String(byteArray);
        }

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>> RestoreMessagesFromString(string rootMessagesString)
        {
            var bytesArray = Convert.FromBase64String(rootMessagesString);
            return MessagePack.MessagePackSerializer.Deserialize<ConcurrentDictionary<string, ConcurrentDictionary<string, List<Message>>>>(bytesArray);
        }


        public static byte[] SaveSearchResponsesToByteArray(List<SearchResponse> responses)
        {

            var byteArray = MessagePack.MessagePackSerializer.Serialize(responses, options: SearchResponseOptions);
            return byteArray;
        }

        public static List<SearchResponse> RestoreSearchResponsesFromStream(System.IO.Stream inputStream)
        {
                return MessagePack.MessagePackSerializer.Deserialize<List<SearchResponse>>(inputStream, options: SearchResponseOptions);
        }
    }
}
