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
using Android.Views;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Seeker.Helpers;
using Seeker.Search;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Common;
namespace Seeker
{
    /// <summary>
    /// When getting a users info for their User Info Activity, we need their peer UserInfo and their server UserData.  We add them to a list so that when the UserData comes in from the server, we know to save it.
    /// </summary>
    public static class RequestedUserInfoHelper
    {
        private static object picturesStoredUsersLock = new object();
        private static List<string> picturesStoredUsers = new List<string>();
        public static volatile List<UserListItem> RequestedUserList = new List<UserListItem>();

        public static UserListItem GetInfoForUser(string uname)
        {
            lock (RequestedUserList)
            {
                return RequestedUserList.Where((userListItem) => { return userListItem.Username == uname; }).FirstOrDefault();
            }
        }

        private static bool ContainsUserInfo(string uname)
        {
            var uinfo = GetInfoForUser(uname);
            if (uinfo == null)
            {
                return false;
            }
            if (uinfo.UserInfo == null || uinfo.UserData == null)
            {
                return false;
            }
            return true;
        }

        public static void RequestUserInfoApi(string uname)
        {
            if (uname == string.Empty)
            {
                Toast.MakeText(SeekerApplication.ApplicationContext, Resource.String.request_user_error_empty, ToastLength.Short).Show();
                return;
            }
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                Toast.MakeText(SeekerApplication.ApplicationContext, Resource.String.must_be_logged_to_request_user_info, ToastLength.Short).Show();
                return;
            }

            if (ContainsUserInfo(uname))
            {
                LaunchUserInfoView(uname);
                return;
            }

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show(); });
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                    {
                        RequestUserInfoLogic(uname);
                    }));
                }));
            }
            else
            {
                RequestUserInfoLogic(uname);
            }
        }

        public static void RequestUserInfoLogic(string uname)
        {
            Toast.MakeText(SeekerApplication.ApplicationContext, Resource.String.requesting_user_info, ToastLength.Short).Show();
            lock (RequestedUserList)
            {
                RequestedUserList.Add(new UserListItem(uname));
            }
            SeekerState.SoulseekClient.GetUserDataAsync(uname);
            SeekerState.SoulseekClient.GetUserInfoAsync(uname).ContinueWith(new Action<Task<UserInfo>>(
                (Task<UserInfo> userInfoTask) =>
                {
                    if (userInfoTask.IsCompletedSuccessfully)
                    {
                        if (!AddIfRequestedUser(uname, null, null, userInfoTask.Result))
                        {
                            Logger.Firebase("requested user info logic yet could not find in list!!");
                        }
                        else
                        {
                            Action<View> action = new Action<View>((View v) =>
                            {
                                LaunchUserInfoView(uname);
                            });

                            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Snackbar sb = Snackbar.Make(SeekerApplication.GetViewForSnackbar(), string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.user_info_received), uname), Snackbar.LengthLong).SetAction(Resource.String.view, action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                                (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));
                                sb.Show();
                            });
                        }
                    }
                    else
                    {
                        Exception e = userInfoTask.Exception;

                        if (e.InnerException is SoulseekClientException && e.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SeekerState.ActiveActivityRef, string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.user_info_failed_conn), uname), ToastLength.Long).Show();
                            });
                        }
                        else if (e.InnerException is UserOfflineException)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SeekerState.ActiveActivityRef, string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.user_info_failed_offline), uname), ToastLength.Long).Show();
                            });
                        }
                        else if (e.InnerException is TimeoutException)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SeekerState.ActiveActivityRef, string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.user_info_failed_timeout), uname), ToastLength.Long).Show();
                            });
                        }
                        else
                        {
                            string msg = e.Message;
                            string innerMsg = e.InnerException.Message;
                            Type t = e.InnerException.GetType();
                            Logger.Firebase("unexpected exception: " + msg + t.Name);
                        }
                    }
                }
            ));
        }

        public static void LaunchUserInfoView(string uname)
        {
            Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(ViewUserInfoActivity));
            intent.PutExtra(ViewUserInfoActivity.USERNAME_TO_VIEW, uname);
            SeekerState.ActiveActivityRef.StartActivity(intent);
        }

        /// <summary>
        /// This event is used if the userinfo comes late and one is on the ViewUserInfoActivity to load it...
        /// </summary>
        public static EventHandler<UserData> UserDataReceivedUI;

        public static bool AddIfRequestedUser(string uname, UserData userData, UserStatus userStatus, UserInfo userInfo)
        {
            bool found = false;
            lock (RequestedUserList)
            {
                bool removeOldestPic = false;
                foreach (UserListItem item in RequestedUserList)
                {
                    if (item.Username == uname)
                    {
                        found = true;
                        if (userData != null)
                        {
                            Logger.Debug("Requested server UserData received");
                            item.UserData = userData;
                            UserDataReceivedUI?.Invoke(null, userData);
                        }
                        if (userStatus != null)
                        {
                            Logger.Debug("Requested server UserStatus received");
                            item.UserStatus = userStatus;
                        }
                        if (userInfo != null)
                        {
                            Logger.Debug("Requested peer UserInfo received");
                            item.UserInfo = userInfo;
                            if (userInfo.HasPicture)
                            {
                                Logger.Debug("peer has pic");
                                lock (picturesStoredUsersLock)
                                {
                                    picturesStoredUsers.Add(uname);
                                    picturesStoredUsers = picturesStoredUsers.Distinct().ToList();
                                    if (picturesStoredUsers.Count > int.MaxValue)
                                    {
                                        removeOldestPic = true;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                if (!found)
                {
                }
                if (removeOldestPic)
                {
                    Logger.InfoFirebase("Remove oldest picture");
                    lock (picturesStoredUsersLock)
                    {
                        string userToRemovePic = picturesStoredUsers[0];
                        picturesStoredUsers.RemoveAt(0);
                    }
                    foreach (UserListItem item in RequestedUserList)
                    {
                        if (item.Username == uname)
                        {
                            item.UserInfo = null;
                        }
                    }
                }
            }
            return found;
        }
    }
}
