using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using Common;
using Common.Messages;
using Seeker.Helpers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Seeker.Chatroom
{
    public class ChatroomController
    {
        public static Soulseek.RoomList RoomList = null;
        public static List<Soulseek.RoomInfo> RoomListParsed = null;
        public static List<Tuple<bool, DateTime>> ConnectionLapse = new List<Tuple<bool, DateTime>>(); //true = connected
        public static EventHandler<EventArgs> RoomListReceived;
        /// <summary>
        /// Invoked whenever moderators are added or removed
        /// </summary>
        public static EventHandler<UserJoinedOrLeftEventArgs> RoomModeratorsChanged; //could be updated to give the user that left or joined....
        public static EventHandler<EventArgs> RoomDataReceived;


        public static EventHandler<MessageReceivedArgs> MessageReceived;
        public static EventHandler<UserJoinedOrLeftEventArgs> UserJoinedOrLeft;
        public static EventHandler<UserRoomStatusChangedEventArgs> UserRoomStatusChanged;
        public static EventHandler<Soulseek.RoomTickerListReceivedEventArgs> RoomTickerListReceived;
        public static EventHandler<Soulseek.RoomTickerAddedEventArgs> RoomTickerAdded;
        public static EventHandler<Soulseek.RoomTickerRemovedEventArgs> RoomTickerRemoved;
        public static EventHandler<string> RoomMembershipRemoved;
        public static EventHandler<string> RoomNowHasUnreadMessages;
        public static EventHandler<string> CurrentlyJoinedRoomHasUpdated;
        public static EventHandler<List<string>> CurrentlyJoinedRoomsCleared;
        public static EventHandler<EventArgs> JoinedRoomsHaveUpdated;


        public static bool IsInitialized;

        //these are the rooms that we are currnetly joined and connected to.  These clear on disconnect and get readded.  These will always be a subset of JoinedRoomNames.
        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte> CurrentlyJoinedRoomNames = null;

        public static List<string> JoinedRoomNames = null; //these are the ones that the user joined.
        public static List<string> AutoJoinRoomNames = null; //we automatically join these at startup.  if all goes well then JoinedRoomNames should contain all of these...

        public static List<string> NotifyRoomNames = null; //!!these are ones we are currently joined!! so autojoins after we actually join them and joined but that are not set to autojoin...
        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RootNotifyRoomNames = null; //we automatically join these at startup.  if all goes well then JoinedRoomNames should contain all of these...


        //this is for all users that one may log in as...
        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RootAutoJoinRoomNames = null;
        public static string CurrentUsername = null;

        public static System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomData> JoinedRoomData = new System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomData>();

        public static System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomInfo> ModeratedRoomData = new System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomInfo>();


        public static System.Collections.Concurrent.ConcurrentDictionary<string, Queue<StatusMessageUpdate>> JoinedRoomStatusUpdateMessages = new System.Collections.Concurrent.ConcurrentDictionary<string, Queue<StatusMessageUpdate>>();

        public static System.Collections.Concurrent.ConcurrentDictionary<string, Queue<Message>> JoinedRoomMessages = new System.Collections.Concurrent.ConcurrentDictionary<string, Queue<Message>>();

        public static System.Collections.Concurrent.ConcurrentDictionary<string, string> JoinedRoomMessagesLastUserHelper = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<Soulseek.RoomTicker>> JoinedRoomTickers = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Soulseek.RoomTicker>>();

        public static bool SortByPopular = true;

        private static bool FirstConnect = true;

        public static void UpdateForCurrentChanged(string roomName)
        {
            CurrentlyJoinedRoomHasUpdated?.Invoke(null, roomName);
        }

        private static void SetConnectionLapsedMessage(bool reconnect)
        {
            if (reconnect && FirstConnect)
            {
                FirstConnect = false;
                return;
            }

            if (JoinedRoomNames == null || JoinedRoomNames.Count == 0)
            {
                //nothing we need to do...
            }
            else
            {
                SpecialMessageCode code = reconnect ? SpecialMessageCode.Reconnect : SpecialMessageCode.Disconnect;
                List<string> noLongerConnectedRooms = JoinedRoomNames.ToList();
                foreach (string room in noLongerConnectedRooms)
                {
                    DateTime localNow = SimpleHelpers.GetDateTimeNowSafe();
                    Message m = new Message(localNow, DateTime.UtcNow, code, getSpecialStatusMessageText(code, localNow));
                    ChatroomController.AddMessage(room, m); //background thread
                    ChatroomController.MessageReceived?.Invoke(null, new MessageReceivedArgs(room, m));
                }
                ChatroomController.NoLongerCurrentlyConnected(noLongerConnectedRooms);
            }
        }

        private static String getSpecialStatusMessageText(SpecialMessageCode code, DateTime localNow)
        {
            switch (code)
            {
                case SpecialMessageCode.Disconnect:
                    return string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.chatroom_disconnected_at), CommonHelpers.GetNiceDateTime(localNow));
                case SpecialMessageCode.Reconnect:
                    return string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.chatroom_reconnected_at), CommonHelpers.GetNiceDateTime(localNow));
            }
            return null;
        }

        public static bool IsPrivate(string roomName)
        {
            if (RoomList.Private.Any(privRoom => { return privRoom.Name == roomName; }))
            {
                return true;
            }
            else if (RoomList.Owned.Any(privRoom => { return privRoom.Name == roomName; }))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsOwnedByUs(Soulseek.RoomInfo roomInfo)
        {
            return RoomList.Owned.Any(ownedRoom => { return ownedRoom.Name == roomInfo.Name; }); //use AreWeOwner instead maybe...
        }

        public static bool IsAutoJoinOn(Soulseek.RoomInfo autoJoinOn)
        {
            return AutoJoinRoomNames.Any(autoJoin => { return autoJoinOn.Name == autoJoin; });
        }

        public static bool IsNotifyOn(Soulseek.RoomInfo notifyOn)
        {
            return NotifyRoomNames.Any(notifyOnName => { return notifyOn.Name == notifyOnName; });
        }

        public static bool AreWeMod(string roomname)
        {
            return ChatroomController.JoinedRoomData[roomname].Operators.Contains(PreferencesState.Username);
        }

        public static bool AreWeOwner(string roomname)
        {
            return ChatroomController.JoinedRoomData[roomname].Owner == PreferencesState.Username;
        }

        public static List<Soulseek.UserData> GetWrappedUserData(string roomName, bool isPrivate, string filterString = "")
        {
            List<Soulseek.UserData> chatroomUserData = new List<Soulseek.UserData>();

            var userlist = ChatroomController.JoinedRoomData[roomName].Users.ToList();

            var opList = ChatroomController.JoinedRoomData[roomName].Operators?.ToList();
            var owner = ChatroomController.JoinedRoomData[roomName].Owner;
            //int timeJoined = 0;
            foreach (Soulseek.UserData user in userlist) //userlist is in the order that they joined
            {
                Soulseek.UserRole userRole = Soulseek.UserRole.Normal;
                if (isPrivate)
                {
                    if (user.Username == owner)
                    {
                        userRole = Soulseek.UserRole.Owner;
                    }
                    else if (opList.Contains(user.Username))
                    {
                        userRole = Soulseek.UserRole.Operator;
                    }
                }
                if (user.Username.Contains(filterString))
                {
                    chatroomUserData.Add(GetChatroomUserData(user, userRole));
                }
            }
            chatroomUserData.Sort(new ChatroomUserDataComparer(UserListService.Instance, PreferencesState.PutFriendsOnTop, PreferencesState.SortChatroomUsersBy));
            return chatroomUserData;
        }


        public static ChatroomUserData GetChatroomUserData(Soulseek.UserData ud, Soulseek.UserRole role)
        {
            var wrappedUser = new ChatroomUserData(ud.Username, ud.Status, ud.AverageSpeed, ud.UploadCount, ud.FileCount, ud.DirectoryCount, ud.CountryCode, ud.SlotsFree);
            wrappedUser.ChatroomUserRole = role;
            return wrappedUser;
        }


        public static void SendChatroomMessageLogic(string roomName, Message msg) //you can start out with a message...
        {

            ChatroomController.AddMessage(roomName, msg); //ui thread.

            //MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
            ChatroomController.MessageReceived?.Invoke(null, new MessageReceivedArgs(roomName, true, false, msg));
            Action<Task> continueWithAction = new Action<Task>((Task t) =>
            {
                //#if DEBUG
                //System.Threading.Thread.Sleep(3000);
                //#endif 
                if (t.IsFaulted)
                {
                    msg.SentMsgStatus = SentStatus.Failed;
                    SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.failed_to_send_message), ToastLength.Long);
                }
                else
                {
                    msg.SentMsgStatus = SentStatus.Success;
                }
                //MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
                ChatroomController.MessageReceived?.Invoke(null, new MessageReceivedArgs(roomName, false, true, msg));
            });
            SeekerState.SoulseekClient.SendRoomMessageAsync(roomName, msg.MessageText).ContinueWith(continueWithAction);
        }



        public static bool HasRoomData(string name)
        {
            return JoinedRoomData.ContainsKey(name);
        }

        public static Soulseek.RoomData GetRoomData(string name)
        {
            if (JoinedRoomData.ContainsKey(name))
            {
                return JoinedRoomData[name];
            }
            else
            {
                return null;
            }
        }

        //TODO2026 move to lower
        public static List<Soulseek.RoomInfo> GetParsedList(Soulseek.RoomList roomList)
        {
            List<Soulseek.RoomInfo> ownedList = roomList.Owned.ToList();
            List<Soulseek.RoomInfo> publicList = roomList.Public.ToList();
            List<Soulseek.RoomInfo> privateList = roomList.Private.ToList();

            List<Soulseek.RoomInfo> allRooms = new List<Soulseek.RoomInfo>();

            if (JoinedRoomNames.Count != 0)
            {
                allRooms.Add(new RoomInfoCategory(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.joined)));
                //find the rooms and add them...
                foreach (string roomName in JoinedRoomNames)
                {
                    Soulseek.RoomInfo foundRoom = ownedList.FirstOrDefault((room) => { return room.Name == roomName; });
                    if (foundRoom != null)
                    {
                        allRooms.Add(foundRoom);
                        continue;
                    }
                    foundRoom = publicList.FirstOrDefault((room) => { return room.Name == roomName; });
                    if (foundRoom != null)
                    {
                        allRooms.Add(foundRoom);
                        continue;
                    }
                    foundRoom = privateList.FirstOrDefault((room) => { return room.Name == roomName; });
                    if (foundRoom != null)
                    {
                        allRooms.Add(foundRoom);
                        continue;
                    }
                }
            }

            if (roomList.OwnedCount != 0)
            {
                List<Soulseek.RoomInfo> filteredOwned = ownedList.Where((roomInfo) => { return !JoinedRoomNames.Contains(roomInfo.Name); }).ToList();
                if (filteredOwned.Count > 0)
                {
                    allRooms.Add(new RoomInfoCategory(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.owned)));
                    filteredOwned.Sort(new RoomCountComparer());
                    allRooms.AddRange(filteredOwned);
                }
            }

            if (roomList.PrivateCount != 0)
            {
                List<Soulseek.RoomInfo> filtered = privateList.Where((roomInfo) => { return !JoinedRoomNames.Contains(roomInfo.Name); }).ToList();
                if (filtered.Count > 0)
                {
                    allRooms.Add(new RoomInfoCategory(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.private_room)));
                    filtered.Sort(new RoomCountComparer());
                    allRooms.AddRange(filtered);
                }
            }

            if (roomList.PublicCount != 0)
            {
                allRooms.Add(new RoomInfoCategory(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.public_room)));
                List<Soulseek.RoomInfo> noSpam = publicList.Where((roomInfo) => { return !JoinedRoomNames.Contains(roomInfo.Name); }).ToList();
                noSpam.Sort(new RoomCountComparer());
                allRooms.AddRange(noSpam);
            }

            return allRooms;
        }


        public static void ToggleAutoJoin(string roomName, bool feedback, Context c)
        {
            if (AutoJoinRoomNames.Contains(roomName))
            {
                if (feedback)
                {
                    SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.startup_room_off), roomName), ToastLength.Short);
                }
                AutoJoinRoomNames.Remove(roomName);
            }
            else
            {
                if (feedback)
                {
                    SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.startup_room_on), roomName), ToastLength.Short);
                }
                AutoJoinRoomNames.Add(roomName);
            }
            SaveAutoJoinRoomsToSharedPrefs();
        }

        public static void ToggleNotifyRoom(string roomName, bool feedback, Context c)
        {
            if (NotifyRoomNames.Contains(roomName))
            {
                if (feedback)
                {
                    SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.notif_room_off), roomName), ToastLength.Short);
                }
                NotifyRoomNames.Remove(roomName);
            }
            else
            {
                if (feedback)
                {
                    SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.notif_room_on), roomName), ToastLength.Short);
                }
                NotifyRoomNames.Add(roomName);
            }
            SaveNotifyRoomsToSharedPrefs();
        }

        public static void SaveAutoJoinRoomsToSharedPrefs()
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            if (RootAutoJoinRoomNames == null || AutoJoinRoomNames == null)
            {
                return;
            }
            RootAutoJoinRoomNames[PreferencesState.Username] = AutoJoinRoomNames;

            var joinedRoomsString = SerializationHelper.SaveAutoJoinRoomsListToString(RootAutoJoinRoomNames);

            if (joinedRoomsString != null && joinedRoomsString != string.Empty)
            {
                PreferencesManager.SaveAutoJoinRooms(joinedRoomsString);
            }
        }

        public static void RestoreAutoJoinRoomsFromSharedPrefs(ISharedPreferences sharedPreferences)
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            string joinedRooms = sharedPreferences.GetString(KeyConsts.M_AutoJoinRooms, string.Empty);
            if (joinedRooms == string.Empty)
            {
                RootAutoJoinRoomNames = new System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>();
                AutoJoinRoomNames = new List<string>();
            }
            else
            {
                RootAutoJoinRoomNames = SerializationHelper.RestoreAutoJoinRoomsListFromString(joinedRooms);
                if (PreferencesState.Username != null && PreferencesState.Username != string.Empty && RootAutoJoinRoomNames.ContainsKey(PreferencesState.Username))
                {
                    AutoJoinRoomNames = RootAutoJoinRoomNames[PreferencesState.Username];
                    CurrentUsername = PreferencesState.Username;
                }
                else
                {
                    AutoJoinRoomNames = new List<string>();
                    CurrentUsername = PreferencesState.Username;
                }
            }
        }

        //TODO2026 move to lower
        public static void SaveNotifyRoomsToSharedPrefs()
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            if (RootNotifyRoomNames == null || NotifyRoomNames == null)
            {
                return;
            }
            RootNotifyRoomNames[PreferencesState.Username] = NotifyRoomNames;

            string notifyRoomsString = SerializationHelper.SaveNotifyRoomsListToString(RootNotifyRoomNames);

            if (notifyRoomsString != null && notifyRoomsString != string.Empty)
            {
                PreferencesManager.SaveNotifyRooms(notifyRoomsString);
            }
        }

        public static void RestoreNotifyRoomsToSharedPrefs(ISharedPreferences sharedPreferences)
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            string notifyRooms = sharedPreferences.GetString(KeyConsts.M_chatroomsToNotify, string.Empty);
            if (notifyRooms == string.Empty)
            {
                RootNotifyRoomNames = new System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>();
                NotifyRoomNames = new List<string>();
            }
            else
            {
                RootNotifyRoomNames = SerializationHelper.RestoreNotifyRoomsListFromString(notifyRooms);
                if (PreferencesState.Username != null && PreferencesState.Username != string.Empty && RootNotifyRoomNames.ContainsKey(PreferencesState.Username))
                {
                    NotifyRoomNames = RootNotifyRoomNames[PreferencesState.Username];
                    CurrentUsername = PreferencesState.Username;
                }
                else
                {
                    NotifyRoomNames = new List<string>();
                    CurrentUsername = PreferencesState.Username;
                }
            }
        }

        public static void Initialize()
        {
            //SerializationHelper.MigrateAutoJoinRoomsIfApplicable(SeekerState.SharedPreferences, KeyConsts.M_AutoJoinRooms_Legacy, KeyConsts.M_AutoJoinRooms);
            RestoreAutoJoinRoomsFromSharedPrefs(SeekerState.SharedPreferences);
            //SerializationHelper.MigrateNotifyRoomsIfApplicable(SeekerState.SharedPreferences, KeyConsts.M_chatroomsToNotify_Legacy, KeyConsts.M_chatroomsToNotify);
            RestoreNotifyRoomsToSharedPrefs(SeekerState.SharedPreferences);
            //if auto join rooms list...
            SeekerState.SoulseekClient.PrivateRoomMembershipAdded += SoulseekClient_PrivateRoomMembershipAdded;
            SeekerState.SoulseekClient.PrivateRoomMembershipRemoved += SoulseekClient_PrivateRoomMembershipRemoved;
            SeekerState.SoulseekClient.PrivateRoomModeratedUserListReceived += SoulseekClient_PrivateRoomModeratedUserListReceived;
            SeekerState.SoulseekClient.PrivateRoomModerationAdded += SoulseekClient_PrivateRoomModerationAdded;
            SeekerState.SoulseekClient.PrivateRoomModerationRemoved += SoulseekClient_PrivateRoomModerationRemoved;
            SeekerState.SoulseekClient.PrivateRoomUserListReceived += SoulseekClient_PrivateRoomUserListReceived;
            // SeekerState.SoulseekClient.
            SeekerState.SoulseekClient.RoomJoined += SoulseekClient_RoomJoined;
            SeekerState.SoulseekClient.RoomLeft += SoulseekClient_RoomLeft;
            //SeekerState.SoulseekClient.RoomListReceived
            SeekerState.SoulseekClient.RoomMessageReceived += SoulseekClient_RoomMessageReceived;
            SeekerState.SoulseekClient.RoomTickerAdded += SoulseekClient_RoomTickerAdded;
            SeekerState.SoulseekClient.OperatorInPrivateRoomAddedRemoved += SoulseekClient_OperatorInPrivateRoomAddedRemoved;
            SeekerState.SoulseekClient.RoomTickerRemoved += SoulseekClient_RoomTickerRemoved;
            SeekerState.SoulseekClient.RoomTickerListReceived += SoulseekClient_RoomTickerListReceived;

            SeekerApplication.UserStatusChangedDeDuplicated += SoulseekClient_UserStatusChanged;

            JoinedRoomTickers = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Soulseek.RoomTicker>>();
            JoinedRoomNames = new List<string>();
            CurrentlyJoinedRoomNames = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
            JoinedRoomData = new System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomData>();

            IsInitialized = true;
        }

        private static void SoulseekClient_UserStatusChanged(object sender, UserStatus e)
        {
            if (ChatroomController.JoinedRoomData != null && !ChatroomController.JoinedRoomData.IsEmpty)
            {
                //its threadsafe to enumerate a concurrent dictionary values, use get enumerator, etc.
                foreach (var kvp in ChatroomController.JoinedRoomData)
                {
                    bool roomUserFound = false;
                    var oldRoomData = kvp.Value;
                    foreach (var uData in oldRoomData.Users)
                    {
                        if (uData.Username == e.Username)
                        {
                            var updatedUsers = oldRoomData.Users.Select(u =>
                                u.Username == e.Username ? u.WithStatus(e.Presence) : u);
                            JoinedRoomData[kvp.Key] = new Soulseek.RoomData(oldRoomData.Name, updatedUsers, oldRoomData.IsPrivate, oldRoomData.Owner, oldRoomData.Operators);
                            roomUserFound = true;
                            break;
                        }
                    }
                    if (roomUserFound)
                    {
                        //do event.. room user status updated..
                        //add the message and also possibly do the UI event...
                        StatusMessageUpdate statusMessageUpdate = new StatusMessageUpdate(e.Presence == Soulseek.UserPresence.Away ? StatusMessageType.WentAway : StatusMessageType.CameBack, e.Username, DateTime.UtcNow);
                        ChatroomController.AddStatusMessage(kvp.Key, statusMessageUpdate);
                        UserRoomStatusChanged?.Invoke(sender, new UserRoomStatusChangedEventArgs(kvp.Key, e.Username, e.Presence, statusMessageUpdate));
                        Logger.Debug("room user status updated: " + e.Username + " " + e.Presence.ToString() + " " + kvp.Key);
                    }
                }
            }
        }

        private static void SoulseekClient_OperatorInPrivateRoomAddedRemoved(object sender, Soulseek.OperatorAddedRemovedEventArgs e)
        {
            Logger.Debug("SoulseekClient_OperatorInPrivateRoomAddedRemoved " + e.RoomName + " " + e.Username + " " + e.Added);

            if (JoinedRoomData.ContainsKey(e.RoomName))
            {
                var oldRoomData = JoinedRoomData[e.RoomName];
                IEnumerable<string> newOperatorList = null;
                if (e.Added)
                {
                    newOperatorList = oldRoomData.Operators.Append(e.Username);
                }
                else
                {
                    newOperatorList = oldRoomData.Operators.Where((string username) => { return username != e.Username; });
                }
                JoinedRoomData[e.RoomName] = new Soulseek.RoomData(oldRoomData.Name, oldRoomData.Users, oldRoomData.IsPrivate, oldRoomData.Owner, newOperatorList);
            }
            else
            {
                //bad
            }
            RoomModeratorsChanged?.Invoke(null, new UserJoinedOrLeftEventArgs(e.RoomName, e.Added, e.Username, null, null, true));


        }

        private static void SoulseekClient_PrivateRoomUserListReceived(object sender, Soulseek.RoomInfo e)
        {
            Logger.Debug("SoulseekClient_PrivateRoomModerationRemoved " + e.UserCount); //this is the same as the normal user list received event as far as I can tell...
        }

        private static void SoulseekClient_PrivateRoomModerationRemoved(object sender, string e)
        {
            Logger.Debug("SoulseekClient_PrivateRoomModerationRemoved " + e); //this only happens on change... not useful I dont think...
        }

        private static void SoulseekClient_PrivateRoomModerationAdded(object sender, string e)
        {
            Logger.Debug("SoulseekClient_PrivateRoomModerationAdded " + e); //this only happens on change... not useful I dont think...
        }

        private static void SoulseekClient_PrivateRoomModeratedUserListReceived(object sender, Soulseek.RoomInfo e)
        {
            Logger.Debug("SoulseekClient_PrivateRoomModeratedUserListReceived " + e.UserCount);
            ModeratedRoomData[e.Name] = e; //this is WHO ARE THE OPERATORS. and it will show everyone who is an OPERATOR but not an OWNER. So if your name is here you are an operator.. also this gets called every change.
            //update the room data
            if (JoinedRoomData.ContainsKey(e.Name))
            {
                var oldRoomData = JoinedRoomData[e.Name];
                JoinedRoomData[e.Name] = new Soulseek.RoomData(oldRoomData.Name, oldRoomData.Users, oldRoomData.IsPrivate, oldRoomData.Owner, e.Users);
            }
            RoomModeratorsChanged?.Invoke(null, new UserJoinedOrLeftEventArgs(e.Name, false, null));
        }

        private static void SoulseekClient_PrivateRoomMembershipRemoved(object sender, string e)
        {
            Logger.Debug("SoulseekClient_PrivateRoomMembershipRemoved " + e);
            //if we remove ourselves or someone else removes us, then we need to back out of the room (and also refresh the list for good feedback).

            //removing should go here, that way we will not autojoin a room we are no longer part of.
            //if we get kicked.
            if (JoinedRoomNames.Contains(e))
            {
                JoinedRoomNames.Remove(e);
                CurrentlyJoinedRoomNames.TryRemove(e, out _);
                JoinedRoomData.Remove(e, out _); //that way when we go to inner, we wont think we have already joined...
                if (AutoJoinRoomNames != null && AutoJoinRoomNames.Contains(e))
                {
                    AutoJoinRoomNames.Remove(e);
                    SaveAutoJoinRoomsToSharedPrefs();
                }
            }

            RoomMembershipRemoved?.Invoke(null, e);
        }

        private static void SoulseekClient_PrivateRoomMembershipAdded(object sender, string e)
        {
            Logger.Debug("SoulseekClient_PrivateRoomMembershipAdded " + e);
        }

        //public static void UpdateSameUserFlagIfApplicable(string roomName, Message msg)
        //{
        //    if (JoinedRoomMessages.ContainsKey(roomName))
        //    {
        //        JoinedRoomMessages[roomName].
        //    }

        public static void AddMessage(string roomName, Message msg)
        {
            if (NotifyRoomNames.Contains(roomName) && msg.SpecialCode == SpecialMessageCode.None) //i.e. do not show the disconnect or reconnect messages..
            {
                ShowNotification(msg, roomName);
            }
            FlagLastUsernameViaHelper(roomName, msg);
            if (JoinedRoomMessages.ContainsKey(roomName))
            {
                //check last name structure
                //if last name is this then set msg.SpecialSameUserFlag = true;
                JoinedRoomMessages[roomName].Enqueue(msg);
                if (JoinedRoomMessages[roomName].Count > 100)
                {
                    JoinedRoomMessages[roomName].Dequeue();
                }
            }
            else
            {
                JoinedRoomMessages[roomName] = new Queue<Message>();
                JoinedRoomMessages[roomName].Enqueue(msg);
                if (JoinedRoomMessages[roomName].Count > 100)
                {
                    JoinedRoomMessages[roomName].Dequeue();
                }
            }
            if (ChatroomController.currentlyInsideRoomName != roomName
                && msg.SpecialCode == SpecialMessageCode.None) //i.e. do not set to unread if just disconnect or reconnect messages.
            {
                if (!UnreadRooms.ContainsKey(roomName))
                {
                    UnreadRooms.TryAdd(roomName, 0);

                    RoomNowHasUnreadMessages?.Invoke(null, roomName);
                }
            }
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte> UnreadRooms = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();//basically a concurrent hashset.

        public static void AddStatusMessage(string roomName, StatusMessageUpdate statusMessageUpdate)
        {
            if (JoinedRoomStatusUpdateMessages.ContainsKey(roomName))
            {
                //check last name structure
                //if last name is this then set msg.SpecialSameUserFlag = true;
                JoinedRoomStatusUpdateMessages[roomName].Enqueue(statusMessageUpdate);
                if (JoinedRoomStatusUpdateMessages[roomName].Count > 100)
                {
                    JoinedRoomStatusUpdateMessages[roomName].Dequeue();
                }
            }
            else
            {
                JoinedRoomStatusUpdateMessages[roomName] = new Queue<StatusMessageUpdate>();
                JoinedRoomStatusUpdateMessages[roomName].Enqueue(statusMessageUpdate);
                if (JoinedRoomStatusUpdateMessages[roomName].Count > 100)
                {
                    JoinedRoomStatusUpdateMessages[roomName].Dequeue();
                }
            }
        }

        private static void FlagLastUsernameViaHelper(string roomName, Message msg)
        {
            string lastUser = JoinedRoomMessagesLastUserHelper.GetValueOrDefault(roomName);
            if (lastUser == msg.Username)
            {
                msg.SameAsLastUser = true;
            }
            else
            {
                JoinedRoomMessagesLastUserHelper[roomName] = msg.Username;
            }
        }

        private static void SoulseekClient_RoomMessageReceived(object sender, Soulseek.RoomMessageReceivedEventArgs e)
        {
            if (SeekerApplication.IsUserInIgnoreList(e.Username))
            {
                Logger.Debug("IGNORED room msg received: r:" + e.RoomName + " u: " + e.Username);
                return;
            }

            Logger.Debug("room msg received: r:" + e.RoomName + " u: " + e.Username);

            Message msg = new Message(e.Username, -1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, e.Message, false);
            if (e.Username == PreferencesState.Username)
            {
                //we already logged it..
                return;
            }
            AddMessage(e.RoomName, msg); //background thread
            MessageReceived?.Invoke(null, new MessageReceivedArgs(e.RoomName, msg));
        }

        private static void SoulseekClient_RoomLeft(object sender, Soulseek.RoomLeftEventArgs e)
        {
            if (JoinedRoomData.ContainsKey(e.RoomName))
            {
                var oldRoomData = JoinedRoomData[e.RoomName];
                var newUserList = oldRoomData.Users.Where((Soulseek.UserData userData) => { return userData.Username != e.Username; });
                JoinedRoomData[e.RoomName] = new Soulseek.RoomData(oldRoomData.Name, newUserList, oldRoomData.IsPrivate, oldRoomData.Owner, oldRoomData.Operators);
            }
            else
            {
                //bad
            }
            StatusMessageUpdate statusMessageUpdate = new StatusMessageUpdate(StatusMessageType.Left, e.Username, DateTime.UtcNow);
            ChatroomController.AddStatusMessage(e.RoomName, statusMessageUpdate);
            UserJoinedOrLeft?.Invoke(null, new UserJoinedOrLeftEventArgs(e.RoomName, false, e.Username, statusMessageUpdate, null, false));
        }

        private static void SoulseekClient_RoomJoined(object sender, Soulseek.RoomJoinedEventArgs e)
        {
            Logger.Debug("User Joined" + e.Username);
            if (JoinedRoomData.ContainsKey(e.RoomName))
            {
                var oldRoomData = JoinedRoomData[e.RoomName];
                JoinedRoomData[e.RoomName] = new Soulseek.RoomData(oldRoomData.Name, oldRoomData.Users.Append(e.UserData), oldRoomData.IsPrivate, oldRoomData.Owner, oldRoomData.Operators);
            }
            else if (e.Username == PreferencesState.Username)
            {
                //this is when we first join..
            }
            else
            {
                //bad
            }

            StatusMessageUpdate statusMessageUpdate = new StatusMessageUpdate(StatusMessageType.Joined, e.Username, DateTime.UtcNow);
            ChatroomController.AddStatusMessage(e.RoomName, statusMessageUpdate);
            UserJoinedOrLeft?.Invoke(null, new UserJoinedOrLeftEventArgs(e.RoomName, true, e.Username, statusMessageUpdate, e.UserData, false));
        }

        private static void SoulseekClient_RoomTickerAdded(object sender, Soulseek.RoomTickerAddedEventArgs e)
        {
            Logger.Debug("SoulseekClient_RoomTickerAdded");
            if (JoinedRoomTickers.ContainsKey(e.RoomName))
            {
                JoinedRoomTickers[e.RoomName].Add(e.Ticker);
            }
            else
            {
                //I dont know if this gets hit or not...
            }
            RoomTickerAdded?.Invoke(null, e);
        }

        private static void SoulseekClient_RoomTickerRemoved(object sender, Soulseek.RoomTickerRemovedEventArgs e)
        {
            Logger.Debug("RoomTickerRemovedEventArgs");
            //idk what to do here
            RoomTickerRemoved?.Invoke(null, e);
        }

        private static void SoulseekClient_RoomTickerListReceived(object sender, Soulseek.RoomTickerListReceivedEventArgs e)
        {
            Logger.Debug("SoulseekClient_RoomTickerListReceived");
            JoinedRoomTickers[e.RoomName] = e.Tickers.ToList();
            RoomTickerListReceived?.Invoke(null, e);
        }
        public static string StartingState = null; //this is if we get killed in the inner fragment.
        public static void ClearAndCacheJoined()
        {
            if (CurrentlyJoinedRoomNames == null || CurrentlyJoinedRoomNames.Count == 0)
            {
                return;
            }

            CurrentlyJoinedRoomNames.Clear();
            SetConnectionLapsedMessage(false);
        }

        public static bool AttemptedToJoinAutoJoins = false;
        public static void JoinAutoJoinRoomsAndPreviousJoined()
        {
            GetRoomListApi();
            ChatroomController.SetConnectionLapsedMessage(true);
            //techncially we should only do the AutoJoinRoomNames the first time.
            //otherwise they will be part of Joined.
            if (AutoJoinRoomNames != null && AutoJoinRoomNames.Count > 0)
            {
                foreach (string roomName in AutoJoinRoomNames)
                {
                    JoinRoomApi(roomName, true, false, false, true);
                }
            }

            //if connect and reconnect, this will always need to be done..
            if (JoinedRoomNames != null && JoinedRoomNames.Count > 0)
            {
                foreach (string roomName in JoinedRoomNames)
                {
                    if (!CurrentlyJoinedRoomNames.ContainsKey(roomName)) //just in case.
                    {
                        JoinRoomApi(roomName, true, false, false, false);
                    }
                }
            }

            //if we got killed.
            if (StartingState != null && StartingState != string.Empty)
            {
                Logger.Debug("starting state is not null " + StartingState);
                JoinRoomApi(StartingState, true, false, false, false);
                StartingState = null;
            }

            AttemptedToJoinAutoJoins = true;
        }


        /// <summary>
        /// When we get logged out.
        /// </summary>
        /// <param name="prevJoinedRooms"></param>
        public static void NoLongerCurrentlyConnected(List<string> prevJoinedRooms)
        {
            ChatroomController.CurrentlyJoinedRoomsCleared?.Invoke(null, prevJoinedRooms);
        }



        public const string CHANNEL_ID = "Chatroom Messages ID";
        public const string CHANNEL_NAME = "Chatroom Messages";
        public const string FromRoomName = "FromThisRoom";
        public const string ComingFromMessageTapped = "FromAMessage";
        public static string currentlyInsideRoomName = string.Empty;

        public static void ShowNotification(Message msg, string roomName)
        {
            if (msg.Username == PreferencesState.Username)
            {
                return;
            }
            Logger.Debug("currently in room: " + currentlyInsideRoomName);
            if (roomName == currentlyInsideRoomName)
            {
                return;
            }
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SeekerState.ActiveActivityRef, typeof(ChatroomActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop);
                    notifIntent.PutExtra(FromRoomName, roomName); //so we can go to this user..
                    notifIntent.PutExtra(ComingFromMessageTapped, true); //so we can go to this user..
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SeekerState.ActiveActivityRef, msg.Username.GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = CommonHelpers.CreateNotification(SeekerState.ActiveActivityRef, pendingIntent, CHANNEL_ID, string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.new_room_message_received), roomName), msg.Username + ": " + msg.MessageText, false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify(roomName.GetHashCode(), n);
                }
                catch (System.Exception e)
                {
                    Logger.Firebase("ShowNotification failed: " + e.Message + e.StackTrace);
                }
            });
        }

        public static void GetRoomListApi(bool feedback = false)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                if (feedback)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_get_room_list), ToastLength.Short);
                }
                return;
            }
            if (feedback)
            {
                if (SeekerState.ActiveActivityRef != null)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.requesting_room_list), ToastLength.Short);
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { GetRoomListLogic(feedback); }));
                }));
            }
            else
            {
                GetRoomListLogic(feedback);
            }
        }

        public static void GetRoomListLogic(bool feedback)
        {
            Task<Soulseek.RoomList> task = null;
            try
            {
                task = SeekerState.SoulseekClient.GetRoomListAsync();
            }
            catch (Exception e)
            {
                return;
            }
            task.ContinueWith((Task<Soulseek.RoomList> task) =>
            {
                if (task.IsFaulted)
                {

                }
                else
                {
                    RoomList = task.Result;
                    RoomListParsed = GetParsedList(RoomList);
                    if (feedback)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.room_list_received), ToastLength.Short);
                    }
                    RoomListReceived?.Invoke(null, new EventArgs());
                }
            });
        }

        public static void UpdateJoinedRooms()
        {
            JoinedRoomsHaveUpdated?.Invoke(null, new EventArgs());
        }

        public static void CreateRoomApi(string roomName, bool isPrivate, bool feedback)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_create_room), ToastLength.Short);
                return;
            }
            if (feedback)
            {
                if (SeekerState.ActiveActivityRef != null)
                {
                    if (isPrivate)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.privateRoomCreation), ToastLength.Short);
                    }
                    else
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.publicRoomCreation), ToastLength.Short);
                    }
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { CreateRoomLogic(roomName, isPrivate, feedback); }));
                }));
            }
            else
            {
                CreateRoomLogic(roomName, isPrivate, feedback);
            }
        }

        public static void AddRemoveUserToPrivateRoomAPI(string roomName, string userToAdd, bool feedback, bool asMod, bool removeInstead = false)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_add_or_remove_user), ToastLength.Short);
                return;
            }
            if (feedback)
            {
                if (SeekerState.ActiveActivityRef != null)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                    {


                        string msg = string.Empty;
                        if (asMod && removeInstead)
                        {
                            msg = SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.removing_mod);
                        }
                        else if (asMod && !removeInstead)
                        {
                            msg = SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.adding_mod);
                        }
                        else if (!asMod && !removeInstead)
                        {
                            msg = SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.inviting_user_to);
                        }
                        else if (!asMod && removeInstead)
                        {
                            msg = SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.removing_user_from);
                        }
                        SeekerApplication.Toaster.ShowToast(string.Format(msg, roomName), ToastLength.Short);

                    });
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { AddUserToPrivateRoomLogic(roomName, userToAdd, feedback, asMod, removeInstead); }));
                }));
            }
            else
            {
                AddUserToPrivateRoomLogic(roomName, userToAdd, feedback, asMod, removeInstead);
            }
        }


        public static void AddUserToPrivateRoomLogic(string roomName, string userToAdd, bool feedback, bool asMod, bool removeInstead)
        {
            Task task = null;
            string failureMsg = string.Empty;
            string successMsg = string.Empty;
            try
            {
                if (asMod && !removeInstead)
                {
                    successMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.success_added_mod);
                    failureMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.failed_added_mod);
                    task = SeekerState.SoulseekClient.AddPrivateRoomModeratorAsync(roomName, userToAdd);
                }
                else if (!asMod && !removeInstead)
                {
                    successMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.success_invite_user);
                    failureMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.failed_invite_user);
                    task = SeekerState.SoulseekClient.AddPrivateRoomMemberAsync(roomName, userToAdd);
                }
                else if (asMod && removeInstead)
                {
                    successMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.success_remove_mod);
                    failureMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.failed_remove_mod);
                    task = SeekerState.SoulseekClient.RemovePrivateRoomModeratorAsync(roomName, userToAdd);
                }
                else if (!asMod && removeInstead)
                {
                    successMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.success_removed_user);
                    failureMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.failed_removed_user);
                    task = SeekerState.SoulseekClient.RemovePrivateRoomMemberAsync(roomName, userToAdd);
                }
            }
            catch (Exception e)
            {
                SeekerApplication.Toaster.ShowToast(failureMsg, ToastLength.Short);
                return;
            }
            task.ContinueWith((Task task) =>
            {
                if (task.IsFaulted)
                {
                    //TODO

                    SeekerApplication.Toaster.ShowToast(failureMsg, ToastLength.Short);

                }
                else
                {
                    //add to joined list and save joined list...

                    if (feedback)
                    {
                        SeekerApplication.Toaster.ShowToast(successMsg, ToastLength.Short);
                    }

                }
            });
        }




        public static void CreateRoomLogic(string roomName, bool isPrivate, bool feedback)
        {
            Task<Soulseek.RoomData> task = null;
            try
            {
                task = SeekerState.SoulseekClient.JoinRoomAsync(roomName, isPrivate); //this will create it if it does not exist..
            }
            catch (Exception e)
            {
                return;
            }
            task.ContinueWith((Task<Soulseek.RoomData> task) =>
            {
                if (task.IsFaulted)
                {

                }
                else
                {
                    //add to joined list and save joined list...

                    if (feedback)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.create_and_refresh), ToastLength.Short);
                    }
                    if (!JoinedRoomNames.Contains(roomName))
                    {
                        JoinedRoomNames.Add(roomName);
                        //TODO: SAVE
                    }
                    if (!CurrentlyJoinedRoomNames.ContainsKey(roomName))
                    {
                        CurrentlyJoinedRoomNames.TryAdd(roomName, (byte)0x0);
                    }
                    JoinedRoomData[roomName] = task.Result;
                    GetRoomListApi();

                }
            });
        }


        public static void DropMembershipOrOwnershipApi(string roomName, bool ownership, bool feedback)
        {
            string ownershipString = SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.ownership);
            string membershipString = SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.membership);
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                string membership = ownership ? ownershipString : membershipString;
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_drop_private), ToastLength.Short);
                return;
            }
            if (feedback)
            {
                if (SeekerState.ActiveActivityRef != null)
                {
                    string membership = ownership ? ownershipString : membershipString;
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.dropping_MEMBERSHIP_of_ROOMNAME), ToastLength.Short);
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { DropMembershipOrOwnershipLogic(roomName, ownership, feedback); }));
                }));
            }
            else
            {
                DropMembershipOrOwnershipLogic(roomName, ownership, feedback);
            }
        }

        public static void DropMembershipOrOwnershipLogic(string roomName, bool ownership, bool feedback)
        {
            Task task = null;
            try
            {
                if (ownership)
                {
                    task = SeekerState.SoulseekClient.DropPrivateRoomOwnershipAsync(roomName); //this will create it if it does not exist..
                }
                else
                {
                    task = SeekerState.SoulseekClient.DropPrivateRoomMembershipAsync(roomName); //this will create it if it does not exist..
                }
            }
            catch (Exception e)
            {
                if (feedback)
                {
                    string ownershipString = SeekerState.ActiveActivityRef.GetString(Resource.String.ownership);
                    string membershipString = SeekerState.ActiveActivityRef.GetString(Resource.String.membership);
                    string membership = ownership ? ownershipString : membershipString;
                    SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.failed_to_remove), membership), ToastLength.Short);
                    Logger.Firebase("DropMembershipOrOwnershipLogic " + membership + e.Message + e.StackTrace);
                }
                return;
            }
            task.ContinueWith((Task task) =>
            {
                string ownershipString = SeekerState.ActiveActivityRef.GetString(Resource.String.ownership);
                string membershipString = SeekerState.ActiveActivityRef.GetString(Resource.String.membership);
                string membership = ownership ? ownershipString : membershipString;
                if (task.IsFaulted)
                {
                    if (feedback)
                    {
                        SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.failed_to_remove), membership), ToastLength.Short);
                    }
                    Logger.Firebase("DropMembershipOrOwnershipLogic " + task.Exception);
                }
                else
                {
                    //I dont think there is anything we need to do... I think that our event will tell us about our new ticker...
                    if (feedback)
                    {
                        SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.successfully_removed), membership), ToastLength.Short);
                    }

                }
            });
        }

        public static void SetTickerApi(string roomName, string tickerMessage, bool feedback)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_set_ticker), ToastLength.Short);
                return;
            }
            if (feedback)
            {
                if (SeekerState.ActiveActivityRef != null)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.setting_ticker), ToastLength.Short);
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { SetTickerLogic(roomName, tickerMessage, feedback); }));
                }));
            }
            else
            {
                SetTickerLogic(roomName, tickerMessage, feedback);
            }
        }

        public static void SetTickerLogic(string roomName, string tickerMessage, bool feedback)
        {
            Task task = null;
            try
            {
                task = SeekerState.SoulseekClient.SetRoomTickerAsync(roomName, tickerMessage); //this will create it if it does not exist..
            }
            catch (Exception e)
            {
                if (feedback)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_set_ticker), ToastLength.Short);
                }
                return;
            }
            task.ContinueWith((Task task) =>
            {
                if (task.IsFaulted)
                {
                    if (feedback)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_set_ticker), ToastLength.Short);
                    }
                }
                else
                {
                    //I dont think there is anything we need to do... I think that our event will tell us about our new ticker...
                    if (feedback)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.successfully_set_ticker), ToastLength.Short);
                    }
                }
            });
        }



        public static void JoinRoomApi(string roomName, bool joining, bool refreshViewAfter, bool feedback, bool fromAutoJoin)
        {
            Logger.Debug("JOINING ROOM" + roomName);
            if (!PreferencesState.CurrentlyLoggedIn)
            {   //since this happens on startup its no good to have this logic...
                Logger.Debug("CANT JOIN NOT LOGGED IN:" + roomName);
                return;
            }
            if (feedback && !joining)
            {
                if (SeekerState.ActiveActivityRef != null)
                {
                    SeekerApplication.Toaster.ShowToast(string.Format(SeekerApplication.GetString(Resource.String.leaving_room), roomName), ToastLength.Short);
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { JoinRoomLogic(roomName, joining, refreshViewAfter, feedback, fromAutoJoin); }));
                }));
            }
            else
            {
                JoinRoomLogic(roomName, joining, refreshViewAfter, feedback, fromAutoJoin);
            }
        }

        public static void JoinRoomLogic(string roomName, bool joining, bool refreshViewAfter, bool feedback, bool fromAutoJoin)
        {
            Task task = null;
            try
            {
                if (joining)
                {
                    task = SeekerState.SoulseekClient.JoinRoomAsync(roomName); //this will create it if it does not exist..
                }
                else
                {
                    task = SeekerState.SoulseekClient.LeaveRoomAsync(roomName); //this will create it if it does not exist..
                }

            }
            catch (Exception e)
            {
                return;
            }
            task.ContinueWith((Task task) =>
            {
                if (task.IsFaulted)
                {
                    Logger.Debug(task.Exception.GetType().Name);
                    Logger.Debug(task.Exception.Message);
                    if (fromAutoJoin)
                    {
                        if (task.Exception != null && task.Exception.InnerException != null && task.Exception.InnerException.InnerException != null)
                        {
                            if (task.Exception.InnerException.InnerException is Soulseek.RoomJoinForbiddenException)
                            {
                                Logger.Debug("forbidden room exception!! remove it from autojoin.." + joining);
                                Logger.Firebase("forbidden room exception!! remove it from autojoin.." + joining + "room name" + roomName); //these should only be private rooms else we are doing something wrong...
                                if (AutoJoinRoomNames != null && AutoJoinRoomNames.Contains(roomName))
                                {
                                    AutoJoinRoomNames.Remove(roomName);
                                    SaveAutoJoinRoomsToSharedPrefs();
                                }
                            }
                        }
                        else
                        {
                            Logger.Debug("failed to join autojoin... join?" + joining);
                        }
                    }
                    Logger.Debug("join / leave task failed... join?" + joining);
                }
                else
                {
                    bool isJoinedChanged = false;
                    bool isCurrentChanged = false;
                    if (task is Task<Soulseek.RoomData> taskRoomData)
                    {
                        //add to joined list and save joined list...
                        if (!JoinedRoomNames.Contains(roomName))
                        {
                            JoinedRoomNames.Add(roomName);
                            isJoinedChanged = true;
                            //TODO: SAVE
                        }
                        if (!CurrentlyJoinedRoomNames.ContainsKey(roomName))
                        {
                            CurrentlyJoinedRoomNames.TryAdd(roomName, (byte)0x0);
                            isCurrentChanged = true;
                        }
                        //we will be part of the room data!!! we also get this AFTER we get the user joined event for ourself.
                        JoinedRoomData[roomName] = taskRoomData.Result;
                        RoomDataReceived?.Invoke(null, new EventArgs());
                    }
                    else
                    {
                        if (joining)
                        {
                            Logger.Debug("WRONG TASK TYPE");
                        }
                        else
                        {
                            isJoinedChanged = RemoveRoomFromJoinedAndOthers(roomName);
                        }
                    }
                    if (refreshViewAfter)
                    {
                        ChatroomController.GetRoomListApi(false);
                    }
                    else if (isJoinedChanged)
                    {
                        //this one will just update the existing list 
                        // marking the now joined rooms as such.
                        Logger.Debug("FULL JOINED CHANGED");
                        ChatroomController.UpdateJoinedRooms();
                    }
                    else if (isCurrentChanged)
                    {
                        //if the user already joined but we are reconnected.
                        ChatroomController.UpdateForCurrentChanged(roomName);
                    }
                }
            });
        }

        public static bool RemoveRoomFromJoinedAndOthers(string roomName)
        {
            //add to joined list and save joined list...
            bool isChanged = false;
            if (JoinedRoomNames.Contains(roomName))
            {
                JoinedRoomNames.Remove(roomName);
                JoinedRoomData.Remove(roomName, out _); //that way when we go to inner, we wont think we have already joined...
                if (AutoJoinRoomNames != null && AutoJoinRoomNames.Contains(roomName))
                {
                    AutoJoinRoomNames.Remove(roomName);
                    SaveAutoJoinRoomsToSharedPrefs();
                }
                isChanged = true;
                //TODO: SAVE
            }

            if (CurrentlyJoinedRoomNames.ContainsKey(roomName))
            {
                CurrentlyJoinedRoomNames.TryRemove(roomName, out _);
                isChanged = true;
            }
            return isChanged;
        }



    }


}