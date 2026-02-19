using Seeker;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Messages
{
    public class UserRoomStatusChangedEventArgs
    {
        public string User;
        public string RoomName;
        public StatusMessageUpdate StatusMessageUpdate;
        public Soulseek.UserPresence Status;
        public UserRoomStatusChangedEventArgs(string roomName, string user, Soulseek.UserPresence status, StatusMessageUpdate statusMessageUpdate)
        {
            User = user;
            RoomName = roomName;
            StatusMessageUpdate = statusMessageUpdate;
            Status = status;
        }
    }

    public class UserJoinedOrLeftEventArgs
    {
        public bool Joined;
        public string User;
        public string RoomName;
        public StatusMessageUpdate? StatusMessageUpdate;
        public Soulseek.UserData UserData;
        public UserJoinedOrLeftEventArgs(string roomName, bool joined, string user, StatusMessageUpdate? statusMessageUpdate = null, Soulseek.UserData uData = null, bool isOperator = false)
        {
            Joined = joined;
            User = user;
            RoomName = roomName;
            StatusMessageUpdate = statusMessageUpdate;
            UserData = uData;
        }
    }

    public class MessageReceivedArgs
    {
        public MessageReceivedArgs(string roomName, Message m)
        {
            RoomName = roomName;
            Message = m;
        }
        public MessageReceivedArgs(string roomName, bool fromUsPending, bool fromUsCon, Message m)
        {
            RoomName = roomName;
            FromUsPending = fromUsPending;
            FromUsConfirmation = fromUsCon;
            Message = m;
        }
        public string RoomName;
        public bool FromUsPending;
        public bool FromUsConfirmation;
        public Message Message;
    }
}
