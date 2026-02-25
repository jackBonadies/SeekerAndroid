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
using Android.Content;
using Android.Widget;
using Common;
using Seeker.Helpers;
using Seeker.Services;
using Soulseek;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Seeker
{
    public class UserListService : IUserListService
    {
        public static UserListService Instance { get; } = new UserListService();
        private UserListService() { }

        public bool ContainsUser(string username)
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

        public bool SetDoesNotExist(string username)
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
        public bool AddUser(UserData userData, UserPresence? status = null)
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
                                item.UserStatus = new UserStatus(item.Username, status.Value, oldStatus?.IsPrivileged ?? false);
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

        public static void AddUserLogic(Context c, string username, Action UIaction, bool massImportCase = false)
        {
            if (!massImportCase)
            {
                SeekerApplication.Toaster.ShowToast(string.Format(SeekerApplication.GetString(Resource.String.adding_user_), username), ToastLength.Short);
            }

            Action<Task<Soulseek.UserData>> continueWithAction = (Task<Soulseek.UserData> t) =>
            {
                if (t == null || t.IsFaulted)
                {
                    //failed to add user
                    if (t.Exception != null && t.Exception.Message != null && t.Exception.Message.ToLower().Contains("the wait timed out"))
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_adding_user_timeout), ToastLength.Short);
                    }
                    else if (t.Exception != null && t.Exception != null && t.Exception.InnerException is Soulseek.UserNotFoundException)
                    {
                        if (!massImportCase)
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_adding_user_not_found), ToastLength.Short);
                        }
                        else
                        {
                            SeekerApplication.Toaster.ShowToast(String.Format("Error adding {0}: user not found", username), ToastLength.Short);
                        }
                    }
                }
                else
                {
                    Instance.AddUser(t.Result);
                    if (!massImportCase)
                    {
                        if (SeekerState.SharedPreferences != null && SeekerState.UserList != null)
                        {
                            PreferencesManager.SaveUserList(SerializationHelper.SaveUserListToString(SeekerState.UserList));
                        }
                    }
                    if (UIaction != null)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(UIaction);
                    }
                }
            };

            //Add User Logic...
            SeekerState.SoulseekClient.WatchUserAsync(username).ContinueWith(continueWithAction);
        }

        public static void AddUserAPI(Context c, string username, Action UIaction, bool massImportCase = false)
        {

            if (username == string.Empty || username == null)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_type_a_username_to_add), ToastLength.Short);
                return;
            }

            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_add_or_remove_user), ToastLength.Short);
                return;
            }

            if (Instance.ContainsUser(username))
            {
                SeekerApplication.Toaster.ShowToast(string.Format(SeekerApplication.GetString(Resource.String.already_added_user_), username), ToastLength.Short);
                return;
            }

            Action<Task> actualActionToPerform = new Action<Task>((Task t) =>
            {
                if (t.IsFaulted)
                {
                    //only show once for the original fault.
                    Logger.Debug("task is faulted, prop? " + (t.Exception.InnerException is FaultPropagationException)); //t.Exception is always Aggregate Exception..
                    if (!(t.Exception.InnerException is FaultPropagationException))
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                    }
                    throw new FaultPropagationException();
                }
                SeekerState.ActiveActivityRef.RunOnUiThread(() => { AddUserLogic(c, username, UIaction, massImportCase); });
            });

            if (SessionService.CurrentlyLoggedInButDisconnectedState())
            {
                Logger.Debug("CurrentlyLoggedInButDisconnectedState");
                Task t;
                if (!SessionService.ShowMessageAndCreateReconnectTask(false, out t))
                {
                    return;
                }
                SeekerApplication.OurCurrentLoginTask = t.ContinueWith(actualActionToPerform);
            }
            else if (SessionService.IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(actualActionToPerform, "User will be added once login is complete."))
            {
                Logger.Debug("IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction");
                return;
            }
            else
            {
                AddUserLogic(c, username, UIaction, massImportCase);
            }
        }

        /// <summary>
        /// Remove user from user list.
        /// </summary>
        /// <returns>true if user was found (if false then bad..)</returns>
        public bool RemoveUser(string username)
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
