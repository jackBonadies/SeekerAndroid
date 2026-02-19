using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Messages
{
    public enum SortOrderChatroomUsers
    {
        //DateJoinedAsc = 0,  //the user list is NOT given to us in any order.  so cant do these.
        //DateJoinedDesc = 1,
        Alphabetical = 2,
        OnlineStatus = 3
    }
    public struct MessageNotifExtended
    {
        public bool IsSpecialMessage;
        public string Username;
        public bool IsOurMessage; //You
        public string MessageText;
    }
    public enum StatusMessageType
    {
        Joined = 1,
        Left = 2,
        WentAway = 3,
        CameBack = 4,
    }
    public struct StatusMessageUpdate
    {
        public StatusMessageType StatusType;
        public string Username;
        public DateTime DateTimeUtc;
        public StatusMessageUpdate(StatusMessageType statusType, string username, DateTime dateTimeUtc)
        {
            StatusType = statusType;
            Username = username;
            DateTimeUtc = dateTimeUtc;
        }
    }

}
