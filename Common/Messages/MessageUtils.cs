using Seeker;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Messages
{
    
        public class RoomCountComparer : IComparer<Soulseek.RoomInfo>
        {
            // Compares by UserCount then Name
            public int Compare(Soulseek.RoomInfo x, Soulseek.RoomInfo y)
            {
                if (x.UserCount.CompareTo(y.UserCount) != 0)
                {
                    return y.UserCount.CompareTo(x.UserCount); //high to low
                }
                else if (x.Name.CompareTo(y.Name) != 0)
                {
                    return x.Name.CompareTo(y.Name);
                }
                else
                {
                    return 0;
                }
            }
        }

        public class ChatroomUserDataComparer : IComparer<Soulseek.UserData>
        {
            // Compares by UserCount then Name
            public int Compare(Soulseek.UserData x, Soulseek.UserData y)
            {
                //always put owners and operators first in private rooms. this is the primary condition.
                if (x is ChatroomUserData xData && y is ChatroomUserData yData)
                {
                    if ((int)yData.ChatroomUserRole != (int)xData.ChatroomUserRole)
                    {
                        return (int)yData.ChatroomUserRole - (int)xData.ChatroomUserRole;
                    }
                }

                if (putFriendsOnTop)
                {
                    bool xFriend = userListService.ContainsUser(x.Username);
                    bool yFriend = userListService.ContainsUser(y.Username);
                    if (xFriend && !yFriend)
                    {
                        return -1; //x is better
                    }
                    else if (yFriend && !xFriend)
                    {
                        return 1; //y is better
                    }
                    //else we continue on to the next criteria
                }

                switch (sortCriteria)
                {
                    case SortOrderChatroomUsers.Alphabetical:
                        return x.Username.CompareTo(y.Username);
                    case SortOrderChatroomUsers.OnlineStatus:
                        if (x.Status == Soulseek.UserPresence.Online && y.Status != Soulseek.UserPresence.Online)
                        {
                            return -1;
                        }
                        else if (x.Status != Soulseek.UserPresence.Online && y.Status == Soulseek.UserPresence.Online)
                        {
                            return 1;
                        }
                        else
                        {
                            return x.Username.CompareTo(y.Username);
                        }
                }

                return 0;
            }
            private readonly bool putFriendsOnTop = false;
            private readonly SortOrderChatroomUsers sortCriteria = SortOrderChatroomUsers.Alphabetical;
            private readonly IUserListService userListService; 
            public ChatroomUserDataComparer(IUserListService userListService, bool putFriendsOnTop, SortOrderChatroomUsers sortCriteria)
            {
                this.userListService = userListService;
                this.putFriendsOnTop = putFriendsOnTop;
                this.sortCriteria = sortCriteria;
            }
        }
}
