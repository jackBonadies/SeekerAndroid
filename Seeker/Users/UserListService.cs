/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */
using Soulseek;
using System.Linq;

namespace Seeker
{
    public static class UserListService
    {
        public static bool ContainsUser(string username)
        {
            lock (SeekerState.UserList)
            {
                if (SeekerState.UserList == null)
                {
                    return false;
                }
                return SeekerState.UserList.FirstOrDefault((userlistinfo) => { return userlistinfo.Username == username; }) != null;
            }
        }

        public static bool SetDoesNotExist(string username)
        {
            bool found = false;
            lock (SeekerState.UserList)
            {
                foreach (UserListItem item in SeekerState.UserList)
                {
                    if (item.Username == username)
                    {
                        found = true;
                        item.DoesNotExist = true;
                        break;
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// This is for adding new users...
        /// </summary>
        /// <returns>true if user was already added</returns>
        public static bool AddUser(UserData userData, UserPresence? status = null)
        {
            lock (SeekerState.UserList)
            {
                bool found = false;
                foreach (UserListItem item in SeekerState.UserList)
                {
                    if (item.Username == userData.Username)
                    {
                        found = true;
                        if (userData != null)
                        {
                            if (status != null)
                            {
                                var oldStatus = item.UserStatus;
                                item.UserStatus = new UserStatus(status.Value, oldStatus?.IsPrivileged ?? false);
                            }
                            item.UserData = userData;
                            item.DoesNotExist = false;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    var item = new UserListItem(userData.Username);
                    item.UserData = userData;

                    if (SeekerApplication.IsUserInIgnoreList(userData.Username))
                    {
                        SeekerApplication.RemoveFromIgnoreList(userData.Username);
                    }

                    SeekerState.UserList.Add(item);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Remove user from user list.
        /// </summary>
        /// <returns>true if user was found (if false then bad..)</returns>
        public static bool RemoveUser(string username)
        {
            lock (SeekerState.UserList)
            {
                UserListItem itemToRemove = null;
                foreach (UserListItem item in SeekerState.UserList)
                {
                    if (item.Username == username)
                    {
                        itemToRemove = item;
                        break;
                    }
                }
                if (itemToRemove == null)
                {
                    return false;
                }
                SeekerState.UserList.Remove(itemToRemove);
                return true;
            }
        }
    }
}
