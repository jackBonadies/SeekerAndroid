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
}
