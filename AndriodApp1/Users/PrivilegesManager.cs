using Android.Content;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndriodApp1.Managers
{
    public class PrivilegesManager
    {
        public static object PrivilegedUsersLock = new object();
        public IReadOnlyCollection<string> PrivilegedUsers = null;
        public bool IsPrivileged = false; //are we privileged

        public static Context Context = null;
        private static PrivilegesManager instance = null;
        public static PrivilegesManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PrivilegesManager();
                }
                return instance;
            }
        }
        /// <summary>
        /// Set Privileged Users List, this will also check if we have privileges and if so, get our remaining time..
        /// </summary>
        /// <param name="privUsers"></param>
        public void SetPrivilegedList(IReadOnlyCollection<string> privUsers)
        {
            lock (PrivilegedUsersLock)
            {
                PrivilegedUsers = privUsers;
                if (SoulSeekState.Username != null && SoulSeekState.Username != string.Empty)
                {
                    IsPrivileged = CheckIfPrivileged(SoulSeekState.Username);
                    if (IsPrivileged)
                    {
                        GetPrivilegesAPI(false);
                    }
                }
            }
        }

        public void SubtractDays(int days)
        {
            SecondsRemainingAtLastCheck -= (days * 24 * 3600);
        }

        private volatile int SecondsRemainingAtLastCheck = int.MinValue;
        private DateTime LastCheckTime = DateTime.MinValue;
        public int GetRemainingSeconds()
        {
            if (SecondsRemainingAtLastCheck == 0 || SecondsRemainingAtLastCheck == int.MinValue)
            {
                return 0;
            }
            else if (LastCheckTime == DateTime.MinValue)
            {
                //shouldnt go here
                return 0;
            }
            else
            {
                int secondsSinceLastCheck = (int)Math.Floor(LastCheckTime.Subtract(DateTime.UtcNow).TotalSeconds);
                int remainingSeconds = SecondsRemainingAtLastCheck - secondsSinceLastCheck;
                return Math.Max(remainingSeconds, 0);
            }
        }

        /// <summary>
        /// Get Remaining Days (rounded down)
        /// </summary>
        /// <returns></returns>
        public int GetRemainingDays()
        {
            return GetRemainingSeconds() / (24 * 3600);
        }

        public string GetPrivilegeStatus()
        {
            if (SecondsRemainingAtLastCheck == 0 || SecondsRemainingAtLastCheck == int.MinValue || GetRemainingSeconds() <= 0)
            {
                if (IsPrivileged)
                {
                    return SeekerApplication.GetString(Resource.String.yes); //this is if we are in the privileged list but our actual amount has not yet been returned.
                }
                else
                {
                    return SeekerApplication.GetString(Resource.String.no_image_chosen); //"None"
                }
            }
            else
            {
                int seconds = GetRemainingSeconds();
                if (seconds > 3600 * 24)
                {
                    int days = seconds / (3600 * 24);
                    if (days == 1)
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.day_left), days);
                    }
                    else
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.days_left), days);
                    }
                }
                else if (seconds > 3600)
                {
                    int hours = seconds / 3600;
                    if (hours == 1)
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.hour_left), hours);
                    }
                    else
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.hours_left), hours);
                    }
                }
                else if (seconds > 60)
                {
                    int mins = seconds / 60;
                    if (mins == 1)
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.minute_left), mins);
                    }
                    else
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.minutes_left), mins);
                    }
                }
                else
                {
                    if (seconds == 1)
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.second_left), seconds);
                    }
                    else
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.seconds_left), seconds);
                    }
                }
            }
        }

        public EventHandler PrivilegesChecked;

        private void GetPrivilegesLogic(bool feedback)
        {
            SoulSeekState.SoulseekClient.GetPrivilegesAsync().ContinueWith(new Action<Task<int>>
                ((Task<int> t) =>
                {
                    if (t.IsFaulted)
                    {
                        if (feedback)
                        {
                            if (t.Exception.InnerException is TimeoutException)
                            {
                                SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.priv_failed) + ": " + SeekerApplication.GetString(Resource.String.timeout), ToastLength.Long);
                            }
                            else
                            {
                                MainActivity.LogFirebase("Failed to get privileges" + t.Exception.InnerException.Message);
                                SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.priv_failed), ToastLength.Long);
                            }
                        }
                        return;
                    }
                    else
                    {
                        SecondsRemainingAtLastCheck = t.Result;
                        if (t.Result > 0)
                        {
                            IsPrivileged = true;
                        }
                        else if (t.Result == 0)
                        {
                            IsPrivileged = false;
                        }
                        LastCheckTime = DateTime.UtcNow;
                        if (feedback)
                        {
                            SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.priv_success) + ". " + SeekerApplication.GetString(Resource.String.status) + ": " + GetPrivilegeStatus(), ToastLength.Long);
                        }
                        PrivilegesChecked?.Invoke(null, new EventArgs());
                    }
                }));
        }

        public void GetPrivilegesAPI(bool feedback)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_be_logged_in_to_check_privileges, ToastLength.Short).Show();
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                        {

                            Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();

                        });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { GetPrivilegesLogic(feedback); });

                }));
            }
            else
            {
                GetPrivilegesLogic(feedback);
            }
        }

        public bool CheckIfPrivileged(string username)
        {
            lock (PrivilegedUsersLock)
            {
                if (PrivilegedUsers != null)
                {
                    return PrivilegedUsers.Contains(username);
                }
                return false;
            }
        }
    }

}