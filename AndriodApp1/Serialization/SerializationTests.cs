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
using static Java.Util.Jar.Attributes;
using System.Xml;
using System.Text;


namespace AndriodApp1.Serialization
{
    /// <summary>
    /// TODO move PreferenceHelper to Common. Requires Moving UserListItem to common and then fixing the binary resolver
    /// </summary>
    public class SerializationTests
    {
        public static void Test()
        {
            List<Message> messages = new List<Message>();
            for (int i = 0; i < 100; i++)
            {
                messages.Add(new Message($"myusername{i}", i, true, DateTime.Now, DateTime.UtcNow, $"my message test {i}", false));
            }

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var test = SerializationHelper.LegacyBinarySerializeToString(messages);
            var time1 = sw.ElapsedMilliseconds;
            log.Error("SEEKER", $"TIMER Binary Native Ser {time1}");

            sw.Start();
            var obj12 = SerializationHelper.LegacyBinaryDeserializeFromString<List<Message>>(test);
            var time132 = sw.ElapsedMilliseconds;
            log.Error("SEEKER", $"TIMER Binary Native Deser {time132}");

            sw.Restart();
            var test1 = MessagePack.MessagePackSerializer.Serialize(messages);
            var finalString = Convert.ToBase64String(test1);
            var time2 = sw.ElapsedMilliseconds;
            log.Error("SEEKER", $"TIMER message pack Ser {time2}");

            sw.Restart();
            var obj9 = MessagePack.MessagePackSerializer.Deserialize<List<Message>>(test1);
            var time42 = sw.ElapsedMilliseconds;
            log.Error("SEEKER", $"TIMER message pack Deser {time42}");

            // typeless serializer..

            sw.Restart();
            var test3 = SerializationHelper.JsonSerializeToString(messages);
            var time4 = sw.ElapsedMilliseconds;

            //var result13 = MessagePack.MessagePackSerializer.Deserialize<List<Message>>(test1);
            log.Error("SEEKER", $"TIMER json Ser {time4}");

            sw.Restart();
            var obj4 = SerializationHelper.DeserializeFromString<List<Message>>(test3);
            var time44 = sw.ElapsedMilliseconds;

            log.Error("SEEKER", $"TIMER json Deser {time4}");

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

            TestCustomSearchResponseSerialization();

            TestCustomBrowseResponseSerialization();

            TestCustomDirListSerialization();
        }

        public static void TestCustomSearchResponseSerialization()
        {
            var listSearchResponse = GetSearchResponses();

            //var x1 = MessagePack.MessagePackSerializer.Serialize(searchResponse, options: MessagePack.Resolvers.TypelessContractlessStandardResolver.Options.WithResolver(resolver));
            //var ser = MessagePack.MessagePackSerializer.Deserialize<SearchResponse>(x1, options: MessagePack.Resolvers.TypelessContractlessStandardResolver.Options.WithResolver(resolver));

            var searchResponsesBytes = MessagePack.MessagePackSerializer.Serialize(listSearchResponse, options: SerializationHelper.SearchResponseOptions);//MessagePack.Resolvers.TypelessContractlessStandardResolver.Options.WithResolver(SerializationHelper.SearchResponseOptions));
            var respones1 = MessagePack.MessagePackSerializer.Deserialize<List<SearchResponse>>(searchResponsesBytes, options: SerializationHelper.SearchResponseOptions);

            var dir1 = new Soulseek.Directory("dirname", null, true);
            var resp123 = SerializationHelper.LegacyBinarySerializeToString<List<SearchResponse>>(listSearchResponse);
            var resDir = SerializationHelper.LegacyBinaryDeserializeFromString<List<SearchResponse>>(resp123);
        }

        public static void TestCustomBrowseResponseSerialization()
        {
            var browseResponse = GetBrowseResponse();

            var dirBytes = MessagePack.MessagePackSerializer.Serialize(browseResponse, options: SerializationHelper.BrowseResponseOptions);
            var dirDeser1 = MessagePack.MessagePackSerializer.Deserialize<BrowseResponse>(dirBytes, options: SerializationHelper.BrowseResponseOptions);

            var dir123 = SerializationHelper.LegacyBinarySerializeToString<BrowseResponse>(browseResponse);
            var dir1234 = SerializationHelper.LegacyBinaryDeserializeFromString<BrowseResponse>(dir123);
        }


        public static void TestCustomDirListSerialization()
        {
            var dirList = GetDirectories(false);

            var dirBytes = MessagePack.MessagePackSerializer.Serialize(dirList, options: SerializationHelper.BrowseResponseOptions);
            var dirDeser1 = MessagePack.MessagePackSerializer.Deserialize<List<Soulseek.Directory>>(dirBytes, options: SerializationHelper.BrowseResponseOptions);

            var dir123 = SerializationHelper.LegacyBinarySerializeToString<List<Soulseek.Directory>>(dirList);
            var dir1234 = SerializationHelper.LegacyBinaryDeserializeFromString<List<Soulseek.Directory>>(dir123);
        }

        public static void PopulateSharedPreferencesFromFile(Context c, ISharedPreferences sharedPrefs)
        {

            var contents = sharedPrefs.All;
            var cnt = contents.Count;
            sharedPrefs.All.Clear();
            var editor1 = sharedPrefs.Edit();
            editor1.Clear();
            editor1.Apply();
            editor1.Commit();


            XmlDocument doc = null;
            var javaFile = new Java.IO.File(c.FilesDir, "SoulSeekPrefs.xml"); // this is not the same as the actual shared preferences location
            using (System.IO.Stream inputStream = c.ContentResolver.OpenInputStream(AndroidX.DocumentFile.Provider.DocumentFile.FromFile(javaFile).Uri))
            {
                string text = null;
                using (StreamReader reader = new StreamReader(inputStream, Encoding.UTF8, true, 1024, true))
                {
                    text = reader.ReadToEnd();
                }
                doc = new XmlDocument();
                doc.LoadXml(text);
            }


            XmlNodeList nameAttributes = doc.SelectNodes("//@name");
            if (nameAttributes != null)
            {
                foreach (XmlAttribute attr in nameAttributes)
                {
                    switch (attr.OwnerElement.Name)
                    {
                        case "boolean":
                            editor1.PutBoolean(attr.Value, bool.Parse(attr.OwnerElement.Attributes[1].Value));
                            break;
                        case "string":
                            if (attr.OwnerElement.Attributes.Count > 1)
                            {
                                editor1.PutString(attr.Value, attr.OwnerElement.Attributes[1].Value);
                            }
                            else
                            {
                                editor1.PutString(attr.Value, attr.OwnerElement.InnerText);
                            }
                            break;
                        case "int":
                            editor1.PutInt(attr.Value, int.Parse(attr.OwnerElement.Attributes[1].Value));
                            break;
                        case "long":
                            editor1.PutLong(attr.Value, long.Parse(attr.OwnerElement.Attributes[1].Value));
                            break;
                    }
                }
            }
            editor1.Apply();
            editor1.Commit();
        }

        public static List<FileAttribute> GetFileAttributes()
        {
            var numAttrs = new Random().Next(0, 4);
            if (numAttrs == 3)
            {
                return null;
            }
            List<FileAttribute> fileAttr = new List<FileAttribute>();
            for (int i = 0; i < numAttrs; i++)
            {
                var attr = new Random().Next(0, 5);
                fileAttr.Add(new FileAttribute((FileAttributeType)attr, new Random().Next(0, 4000)));
            }
            return fileAttr;
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            if(new Random().Next(0, 100) == 0)
            {
                return null;
            }
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static List<Soulseek.File> GetFiles()
        {
            var numFiles = new Random().Next(0, 12);
            if (numFiles == 11)
            {
                return null;
            }
            List<Soulseek.File> files = new List<Soulseek.File>();
            for (int i = 0; i < numFiles; i++)
            {
                var attr = new Random().Next(0, 5);
                files.Add(new Soulseek.File(new Random().Next(0, 10), RandomString(new Random().Next(0, 100)), 100L, "mp3", GetFileAttributes(), true, false));
            }
            return files;
        }

        static SearchResponse GetSearchResponse()
        {
            return new SearchResponse(RandomString(new Random().Next(0, 100)), new Random().Next(0, 10), new Random().Next(0, 10), new Random().Next(0, 10000), 100L, GetFiles(), GetFiles());
        }

        static List<SearchResponse> GetSearchResponses()
        {
            var length = new Random().Next(0, 10000);
            var resp = new List<SearchResponse>();
            for (int i = 0; i < length; i++)
            {
                resp.Add(GetSearchResponse());
            }
            return resp;
        }

        static Soulseek.Directory GetDirectory()
        {
            var dirName = RandomString(new Random().Next(0, 100));
            var fileList = GetFiles();
            return new Soulseek.Directory(dirName, fileList, true);
        }

        static List<Soulseek.Directory> GetDirectories(bool allowNull = true)
        {
            var length = new Random().Next(0, 1000);
            if(new Random().Next(0, 100) == 0 && allowNull)
            {
                return null;
            }
            List<Soulseek.Directory> dirs = new List<Soulseek.Directory>();
            for (int i = 0; i < length; i++)
            {
                var dir = GetDirectory();
                dirs.Add(dir);
            }
            return dirs;
        }

        static BrowseResponse GetBrowseResponse()
        {
            return new BrowseResponse(GetDirectories(), GetDirectories());
        }
    }

}