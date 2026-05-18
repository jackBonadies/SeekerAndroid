using System;

namespace Seeker
{
    public enum UserListChangeType
    {
        Added,
        Removed
    }

    public class UserListChangedEventArgs : EventArgs
    {
        public string Username { get; }
        public UserListItem Item { get; }
        public UserListChangeType ChangeType { get; }

        public UserListChangedEventArgs(string username, UserListItem item, UserListChangeType changeType)
        {
            Username = username;
            Item = item;
            ChangeType = changeType;
        }
    }
}
