using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    [System.Serializable]
    public enum UserRole
    {
        Friend = 0,
        Ignored = 1,
        Category = 2
    }

    // all fields are public ... including subclasses...
    /// <summary>
    /// This is the full user info...
    /// </summary>
    [System.Serializable]
    public class UserListItem
    {
        public string Username = string.Empty;
        public UserRole Role;
        public Soulseek.UserStatus UserStatus; //add user updates this..
        public Soulseek.UserData UserData; //add user updates this as well...
        public Soulseek.UserInfo UserInfo; //this is the "picture and everything" one that we have to explicitly request from the peer (not server)...
        public bool DoesNotExist; //we dont allow someone to add a user that does not exist, BUT if they add someone and their username expires or such, then this will happen...
        public UserListItem(string username, UserRole role)
        {
            Role = role;
            Username = username;
            UserStatus = null;
            UserData = null;
            UserInfo = null;
        }
        public UserListItem(string username)
        {
            Username = username;
            UserStatus = null;
            UserData = null;
            UserInfo = null;
        }
        public UserListItem()
        {
            UserStatus = null;
            UserData = null;
            UserInfo = null;
        }
    }
}