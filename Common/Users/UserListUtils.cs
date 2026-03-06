using Common;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public enum SortOrder
    {
        DateAddedAsc = 0,
        DateAddedDesc = 1,
        Alphabetical = 2,
        OnlineStatus = 3
    }

    public class UserListAlphabeticalComparer : IComparer<UserListItem>
    {
        public int Compare(UserListItem x, UserListItem y)
        {
            if (x is UserListItem xData && y is UserListItem yData)
            {
                return xData.Username.CompareTo(yData.Username);
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// This will do it like QT does it, where ties will be broken by Alphabet.
    /// </summary>
    public class UserListOnlineStatusComparer : IComparer<UserListItem>
    {
        // Compares by UserCount then Name
        public int Compare(UserListItem x, UserListItem y)
        {
            int xStatus = x.DoesNotExist ? -1 : (int)x.GetStatusFromItem(out _);
            int yStatus = y.DoesNotExist ? -1 : (int)y.GetStatusFromItem(out _);

            if (xStatus == yStatus)
            {
                //tie breaker is alphabet
                return x.Username.CompareTo(y.Username);
            }
            else
            {
                return yStatus - xStatus;
            }
        }
    }

    public static class UserListUtils
    {
        public static List<UserListItem> GetSortedUserList(List<UserListItem> userlistOrig, bool isIgnoreList)
    {
        //always copy so the original does not get messed up, since it stores info on date added.
        List<UserListItem> userlist = userlistOrig.ToList();
        if (!isIgnoreList)
        {
            switch (PreferencesState.UserListSortOrder)
            {
                case SortOrder.DateAddedAsc:
                    return userlist;
                case SortOrder.DateAddedDesc:
                    userlist.Reverse();
                    return userlist;
                case SortOrder.Alphabetical:
                    userlist.Sort(new UserListAlphabeticalComparer());
                    return userlist;
                case SortOrder.OnlineStatus:
                    userlist.Sort(new UserListOnlineStatusComparer());
                    return userlist;
                default:
                    return userlist;
            }
        }
        else
        {
            switch (PreferencesState.UserListSortOrder)
            {
                case SortOrder.DateAddedAsc:
                    return userlist;
                case SortOrder.DateAddedDesc:
                    userlist.Reverse();
                    return userlist;
                case SortOrder.Alphabetical:
                    userlist.Sort(new UserListAlphabeticalComparer());
                    return userlist;
                case SortOrder.OnlineStatus:
                    //we do not keep any data on ignored users online status.
                    userlist.Sort(new UserListAlphabeticalComparer());
                    return userlist;
                default:
                    return userlist;
            }
        }
    }

    public static List<UserListItem> ParseUserListForPresentation(string friendsString, string ignoredString)
    {
        List<UserListItem> forAdapter = new List<UserListItem>();
        if (CommonState.UserList.Count != 0)
        {
            forAdapter.Add(new UserListItem(friendsString, UserRole.Category));
            forAdapter.AddRange(GetSortedUserList(CommonState.UserList, false));
        }
        if (CommonState.IgnoreUserList.Count != 0)
        {
            forAdapter.Add(new UserListItem(ignoredString, UserRole.Category));
            forAdapter.AddRange(GetSortedUserList(CommonState.IgnoreUserList, true));
        }
        return forAdapter;
        }
    }
}
