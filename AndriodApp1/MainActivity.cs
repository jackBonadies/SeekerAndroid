﻿/*
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
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Drm;
using Android.OS;
using Android.Provider;
using Soulseek;
using Android.Runtime;
using AndroidX.ViewPager.Widget;
using AndroidX.Fragment.App;
using Android.Views;
using Android.Widget;
using Common;
using Java.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using log = Android.Util.Log;
using System.Linq;
using SlskHelp;

using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using Android.Animation;
using System.Collections.ObjectModel;
using Android.Net.Wifi;
using static Android.Provider.DocumentsContract;
using Android.Util;
using SearchResponseExtensions;
using Android.Net;
using System.Linq.Expressions;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Tabs;
using AndroidX.Core.App;
using AndroidX.ViewPager.Widget;
using AndroidX.DocumentFile.Provider;
using AndroidX.Core.Content;
using Google.Android.Material.Snackbar;

//using System.IO;
//readme:
//dotnet add package Soulseek --version 1.0.0-rc3.1
//xamarin
//Had to rewrite this one from .csproj
//<Import Project="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild
//\Xamarin\Android\Xamarin.Android.CSharp.targets" />
namespace AndriodApp1
{
    public class ForegroundLifecycleTracker : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    {
        public static bool HasAppEverStarted = false;
        //basically this is for the very first time the app gets started so that we 
        //can launch a foreground service while we are in the foreground...
        void Application.IActivityLifecycleCallbacks.OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {

        }

        void Application.IActivityLifecycleCallbacks.OnActivityDestroyed(Activity activity)
        {

        }

        void Application.IActivityLifecycleCallbacks.OnActivityPaused(Activity activity)
        {

        }

        void Application.IActivityLifecycleCallbacks.OnActivityResumed(Activity activity)
        {
            if (!HasAppEverStarted)
            {
                HasAppEverStarted = true;
                try
                {
                    if (SoulSeekState.StartServiceOnStartup)
                    {
                        Intent seekerKeepAliveService = new Intent(activity, typeof(SeekerKeepAliveService));
                        activity.StartService(seekerKeepAliveService); //so so so many people are in background when this starts....
                    }
                }
                catch (Exception e) //this still happened if started from visual studio.. i dont know how since resume is literally the indicator of foreground...
                {                   //that being said, the OnResume logic typically does work even if started from vis studio on locked phone.
                                    //its just that sometimes this almost gets like forced.... but not sure how to reproudce...
                    try
                    {
                        if (activity is AppCompatActivity)
                        {
                            bool foreground = (activity as AppCompatActivity).Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Resumed);
                            if (foreground)
                            {
                                MainActivity.LogFirebase("FOREGROUND seeker keep alive cannot be started: " + e.Message + e.StackTrace);
                            }
                            else
                            {
                                MainActivity.LogFirebase("BACKGROUND seeker keep alive cannot be started: " + e.Message + e.StackTrace);
                            }
                        }
                        else
                        {
                            MainActivity.LogFirebase("seeker keep alive cannot be started: " + e.Message + e.StackTrace);
                        }
                    }
                    catch
                    {

                    }
                    //if started from background, i.e. my phone was locked and I started it from visual studio...
                    //Java.Lang.IllegalStateException: 'Not allowed to start service Intent { cmp=com.companyname.andriodapp1/.SeekerKeepAliveService }: app is in background uid UidRecord{3736c1b u0a228 TPSL idle change:idle|cached procs:1 seq(0,0,0)}'
                }
            }
        }

        void Application.IActivityLifecycleCallbacks.OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {

        }

        void Application.IActivityLifecycleCallbacks.OnActivityStarted(Activity activity)
        {
            SoulSeekState.ActiveActivityRef = activity as FragmentActivity;
            if (SoulSeekState.ActiveActivityRef == null)
            {
                MainActivity.LogFirebase("OnActivityStarted activity is null!");
            }
            DiagLastStarted = activity.GetType().Name.ToString();
            MainActivity.LogDebug("OnActivityStarted " + DiagLastStarted);

            NumberOfActiveActivities++;
            //we are just coming back alive.
            if(NumberOfActiveActivities==1)
            {
                MainActivity.LogDebug("We are back!");
                if (AutoAwayTimer != null)
                {
                    AutoAwayTimer.Stop();
                }

                //if(SeekerApplication.ShouldWeTryToConnect())
                //{
                //    TryToReconnect();
                //}
            }

            if(SoulSeekState.PendingStatusChangeToAwayOnline == SoulSeekState.PendingStatusChange.AwayPending)
            {
                SoulSeekState.PendingStatusChangeToAwayOnline = SoulSeekState.PendingStatusChange.NothingPending;
            }

            if (SoulSeekState.OurCurrentStatusIsAway)
            {
                MainActivity.LogDebug("Our current status is away, lets set it back to online!");
                //set back to online
                MainActivity.SetStatusApi(false);
            }
        }

        private void TryToReconnect()
        {
            try
            {
                MainActivity.LogDebug("! TryToReconnect (on app resume) !");

                if (SeekerApplication.ReconnectSteppedBackOffThreadIsRunning)
                {
                    //set and let it run.
                    MainActivity.LogDebug("In progress, so .Set to let the next one run.");
                    SeekerApplication.ReconnectAutoResetEvent.Set();
                }
                else
                {
                    System.Threading.ThreadPool.QueueUserWorkItem( (object o) => {

                    Task t = SeekerApplication.ConnectAndPerformPostConnectTasks(SoulSeekState.Username, SoulSeekState.Password);
#if DEBUG
                    t.ContinueWith((Task t) => {

                        if(t.IsFaulted)
                        {
                            MainActivity.LogDebug("TryToReconnect FAILED");
                        }
                        else
                        {
                            MainActivity.LogDebug("TryToReconnect SUCCESSFUL");
                        }
                    
                    });
#endif
                    });
                }
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("TryToReconnect Failed " + e.Message + e.StackTrace);
            }
        }

        void Application.IActivityLifecycleCallbacks.OnActivityStopped(Activity activity)
        {
            DiagLastStopped = activity.GetType().Name.ToString();
            MainActivity.LogDebug("OnActivityStopped " + DiagLastStopped);

            NumberOfActiveActivities--;
            //if this is 0 then app is in background, or screen is locked, user at homescreen, other app in front, etc.
            if(NumberOfActiveActivities == 0 && SoulSeekState.AutoAwayOnInactivity)
            {
                MainActivity.LogDebug("We are away!");
                if (AutoAwayTimer == null)
                {
                    AutoAwayTimer = new System.Timers.Timer(1000 * 60 * 5); //5 mins
                    AutoAwayTimer.AutoReset = false; //raise event just once.
                    AutoAwayTimer.Elapsed += AutoAwayTimer_Elapsed;
                }
                AutoAwayTimer.Start();
            }
        }

        private void AutoAwayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MainActivity.LogDebug("We were away for the interval specified.  time to set status to away.");
            //if(!SoulSeekState.currentlyLoggedIn)
            //{
            //    //if we are not supposed to be logged in then do nothing.
            //    return;
            //}
            //else if(!SoulSeekState.SoulseekClient.State.HasFlag(Soulseek.SoulseekClientStates.LoggedIn))
            //{
            //    //if we are currently disconnected then do nothing.
            //    //TODO - if we disconnect will the server set our offline status for us???
            //    return;
            //}
            //else
            //{
                MainActivity.SetStatusApi(true);
            //}
        }

        public volatile static string DiagLastStarted = string.Empty;
        public volatile static string DiagLastStopped = string.Empty;


        public static int NumberOfActiveActivities = 0;
        public static System.Timers.Timer AutoAwayTimer = null;
        

    }

    public enum UpnpDiagStatus
    {
        None = 0,
        UpnpDisabled = 1,
        WifiDisabled = 2,
        NoUpnpDevicesFound = 3,
        UpnpDeviceFoundButFailedToMap = 4,
        Success = 5,
        NoWifiConnection = 6, //wifi is enabled but not connected to any particular connection
        ErrorUnspecified = 10
    }//what about captive portal??

    public enum UpnpRunningStatus
    {
        NeverStarted = 0,
        CurrentlyRunning = 1,
        Finished = 2,
        AlreadyMapped = 3
    }

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

    public class UPnpManager
    {
        public static Context Context = null;
        private static UPnpManager instance = null;

        public volatile int DevicesFound = -1;
        public volatile int DevicesSuccessfullyMapped = -1;
        public volatile UpnpDiagStatus DiagStatus = UpnpDiagStatus.None;
        public volatile UpnpRunningStatus RunningStatus = UpnpRunningStatus.NeverStarted;
        public string LocalIP = string.Empty;
        public bool Feedback = false;

        public static DateTime LastSetTime = DateTime.MinValue;
        public static int LastSetLifeTime = -1; //sec
        public static int LastSetPort = -1;
        public static string LastSetLocalIP = string.Empty;

        public static void SaveUpnpState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutLong(SoulSeekState.M_LastSetUpnpRuleTicks, LastSetTime.Ticks);
                editor.PutInt(SoulSeekState.M_LifetimeSeconds, LastSetLifeTime);
                editor.PutInt(SoulSeekState.M_PortMapped, LastSetPort);
                editor.PutString(SoulSeekState.M_LastSetLocalIP, LastSetLocalIP);
                editor.Commit();
            }
        }

        public static void RestoreUpnpState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                LastSetTime = new DateTime(SoulSeekState.SharedPreferences.GetLong(SoulSeekState.M_LastSetUpnpRuleTicks, 0));
                LastSetLifeTime = SoulSeekState.SharedPreferences.GetInt(SoulSeekState.M_LifetimeSeconds, -1);
                LastSetPort = SoulSeekState.SharedPreferences.GetInt(SoulSeekState.M_PortMapped, -1);
                LastSetLocalIP = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_LastSetLocalIP, string.Empty);
            }
        }

        public static UPnpManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UPnpManager();
                }
                return instance;
            }
        }

        public EventHandler<EventArgs> SearchStarted; //this is if the actual search starts.  if there is an error early (no wifi, etc) then this wont get called, just finished will be called.
        public EventHandler<EventArgs> DeviceSuccessfullyMapped;  //these are mostly for UI events...
        public EventHandler<EventArgs> SearchFinished;            //so if someone is actively running mapping in settings...

        private void CancelSearchAfterTime() //SSDP
        {
            int timeout = 7; //seconds.  our MX value is 3 seconds.
            System.Timers.Timer finishSearchTimer = new System.Timers.Timer(timeout * 1000);
            finishSearchTimer.AutoReset = false;
            finishSearchTimer.Elapsed += FinishSearchTimer_Elapsed;
            finishSearchTimer.Start();
        }

        public enum ListeningIcon
        {
            OffIcon = 0,
            PendingIcon = 1,
            ErrorIcon = 2,
            SuccessIcon = 3
        }

        public Tuple<ListeningIcon, string> GetIconAndMessage()
        {
            if (!SoulSeekState.ListenerUPnpEnabled)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.upnp_off));
            }
            else if (!SoulSeekState.ListenerEnabled)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.listener_off));
            }
            else if (RunningStatus == UpnpRunningStatus.NeverStarted)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.upnp_not_ran));
            }
            else if (RunningStatus == UpnpRunningStatus.CurrentlyRunning)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.PendingIcon, Context.GetString(Resource.String.upnp_currently_running));
            }
            else if (RunningStatus == UpnpRunningStatus.Finished)
            {
                if (DiagStatus == UpnpDiagStatus.NoUpnpDevicesFound)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.no_upnp_devices_found));
                }
                else if (DiagStatus == UpnpDiagStatus.WifiDisabled)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.upnp_wifi_only));
                }
                else if (DiagStatus == UpnpDiagStatus.NoWifiConnection)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.upnp_no_wifi_conn));
                }
                else if (DiagStatus == UpnpDiagStatus.UpnpDeviceFoundButFailedToMap)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.failed_to_set));
                }
                else if (DiagStatus == UpnpDiagStatus.ErrorUnspecified)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.error));
                }
                else if (DiagStatus == UpnpDiagStatus.Success)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.SuccessIcon, Context.GetString(Resource.String.upnp_success));
                }
                else
                {
                    MainActivity.LogFirebase("GetIconAndMessage We should not get here");
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.error));
                }
            }
            else if (RunningStatus == UpnpRunningStatus.AlreadyMapped)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.SuccessIcon, Context.GetString(Resource.String.upnp_last_success));
            }
            else
            {
                MainActivity.LogFirebase("GetIconAndMessage We should not get here 2");
                return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.error));
            }
        }

        private void FinishSearchTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RunningStatus = UpnpRunningStatus.Finished;
            try
            {
                Mono.Nat.NatUtility.StopDiscovery();
            }
            catch (Exception ex)
            {
                MainActivity.LogFirebase("FinishSearchTimer_Elapsed " + ex.Message + ex.StackTrace);
            }
            if (DevicesSuccessfullyMapped > 0)
            {
                DiagStatus = UpnpDiagStatus.Success;
            }
            else if (DevicesSuccessfullyMapped == 0 && DevicesFound > 0)
            {
                DiagStatus = UpnpDiagStatus.UpnpDeviceFoundButFailedToMap;
            }
            else if (DevicesSuccessfullyMapped == 0 && DevicesFound == 0)
            {
                DiagStatus = UpnpDiagStatus.NoUpnpDevicesFound;
            }
            if (Feedback)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    if (DiagStatus == UpnpDiagStatus.NoUpnpDevicesFound)
                    {
                        Toast.MakeText(Context, Context.GetString(Resource.String.no_upnp_devices_found), ToastLength.Short).Show();
                    }
                    else if (DiagStatus == UpnpDiagStatus.UpnpDeviceFoundButFailedToMap)
                    {
                        Toast.MakeText(Context, Context.GetString(Resource.String.failed_to_set), ToastLength.Short).Show();
                    }
                });
            }
            Feedback = false;
            MainActivity.LogDebug("finished " + DiagStatus.ToString());

            SearchFinished?.Invoke(null, new EventArgs());
            if (DiagStatus == UpnpDiagStatus.Success)
            {
                //set up timer to run again...
                RenewMapping();
            }
        }

        public void SearchAndSetMappingIfRequired()
        {
            try
            {
                if (!SoulSeekState.ListenerEnabled || !SoulSeekState.ListenerUPnpEnabled)
                {
                    MainActivity.LogDebug("Upnp is off...");
                    SearchFinished?.Invoke(null, new EventArgs());
                    Feedback = false;
                    return;
                }
                if (LastSetLifeTime != -1 && LastSetTime.AddSeconds(LastSetLifeTime / 2.0) > DateTime.UtcNow && LastSetPort == SoulSeekState.ListenerPort && IsLocalIPsame())
                {
                    MainActivity.LogDebug("Renew Mapping Later... we already have a good one..");
                    RunningStatus = UpnpRunningStatus.AlreadyMapped;
                    SearchFinished?.Invoke(null, new EventArgs());
                    Feedback = false;
                    RenewMapping();
                }
                else
                {
                    MainActivity.LogDebug("search and set mapping...");
                    SearchAndSetMapping();
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("SearchAndSetMappingIfRequired" + e.Message + e.StackTrace);
                Feedback = false;
            }
        }


        public static System.Timers.Timer RenewMappingTimer = null;
        public void RenewMapping() //if new port
        {
            MainActivity.LogDebug("renewing mapping");
            try
            {
                if (LastSetLifeTime != -1 && LastSetPort != -1 && LastSetTime != DateTime.MinValue)
                {
                    if (RenewMappingTimer == null)
                    {
                        RenewMappingTimer = new System.Timers.Timer();
                        RenewMappingTimer.AutoReset = false;//since this function will get called again anyway.
                        RenewMappingTimer.Elapsed += RenewMappingTimer_Elapsed;
                    }
                    RenewMappingTimer.Interval = Math.Max(LastSetLifeTime * 1000 / 2, 3600 * 1000 * 2); //at least two hours (for now).  divided by 2!
                    RenewMappingTimer.Start();
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("RenewMapping" + e.Message + e.StackTrace);
            }
        }

        private void RenewMappingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MainActivity.LogDebug("renew timer elapsed");
            SearchAndSetMapping();
        }

        public bool IsLocalIPsame()
        {
            try
            {
                WifiManager wm = (WifiManager)Context.GetSystemService(Context.WifiService);
                if (wm.WifiState == Android.Net.WifiState.Disabled) //if just mobile is on and wifi is off.
                {
                    return false;
                }
                return Android.Text.Format.Formatter.FormatIpAddress(wm.ConnectionInfo.IpAddress) == LastSetLocalIP;
            }
            catch (Exception ex)
            {
                MainActivity.LogFirebase("IsLocalIPsame exception " + ex.Message + ex.StackTrace);
                return false;
            }
        }

        public void SearchAndSetMapping()
        {
            try
            {
                if (!SoulSeekState.ListenerEnabled || !SoulSeekState.ListenerUPnpEnabled)
                {
                    DiagStatus = UpnpDiagStatus.UpnpDisabled;
                    RunningStatus = UpnpRunningStatus.Finished;
                    SearchFinished?.Invoke(null, new EventArgs());
                    return;
                }
                if (Context == null)
                {
                    DiagStatus = UpnpDiagStatus.ErrorUnspecified;
                    RunningStatus = UpnpRunningStatus.Finished;
                    throw new Exception("SearchAndSetMapping Context is null");
                }
                WifiManager wm = (WifiManager)Context.GetSystemService(Context.WifiService);
                if (wm.WifiState == Android.Net.WifiState.Disabled) //if just mobile is on and wifi is off.
                {
                    //wifi is disabled.
                    DiagStatus = UpnpDiagStatus.WifiDisabled;
                    RunningStatus = UpnpRunningStatus.Finished;
                    SearchFinished?.Invoke(null, new EventArgs());
                    return;
                }
                if (wm.ConnectionInfo.SupplicantState == SupplicantState.Disconnected || wm.ConnectionInfo.IpAddress == 0)
                {
                    //wifi is disabled.
                    DiagStatus = UpnpDiagStatus.NoWifiConnection;
                    RunningStatus = UpnpRunningStatus.Finished;
                    SearchFinished?.Invoke(null, new EventArgs());
                    return;
                }
                LocalIP = Mono.Nat.NatUtility.LocalIpAddress = Android.Text.Format.Formatter.FormatIpAddress(wm.ConnectionInfo.IpAddress);
                //string gatewayAddress = Android.Text.Format.Formatter.FormatIpAddress(wm.DhcpInfo.Gateway);
                MainActivity.LogDebug(LocalIP);

                DevicesFound = 0;
                DevicesSuccessfullyMapped = 0;
                RunningStatus = UpnpRunningStatus.CurrentlyRunning;
                SearchStarted?.Invoke(null, new EventArgs());
                if (Feedback)
                {
                    Toast.MakeText(Context, Context.GetString(Resource.String.attempting_to_find_and_open), ToastLength.Short).Show();
                }

                CancelSearchAfterTime();
                Mono.Nat.NatUtility.StartDiscovery(new Mono.Nat.NatProtocol[] { Mono.Nat.NatProtocol.Upnp });

            }
            catch (Exception e)
            {
                DiagStatus = UpnpDiagStatus.ErrorUnspecified;
                MainActivity.LogFirebase("SearchAndSetMapping: " + e.Message + e.StackTrace);
                SearchFinished?.Invoke(null, new EventArgs());
            }
        }

        public UPnpManager()
        {
            Mono.Nat.NatUtility.UnknownDeviceFound += NatUtility_UnknownDeviceFound;
            Mono.Nat.NatUtility.DeviceFound += NatUtility_DeviceFound;
        }

        private void NatUtility_DeviceFound(object sender, Mono.Nat.DeviceEventArgs e)
        {
            try
            {
                MainActivity.LogDebug("Device Found");
                Interlocked.Increment(ref DevicesFound); //not sure if this will ever be greater than one....
                if (DevicesFound > 1)
                {
                    MainActivity.LogFirebase("more than 1 device found");
                }
                bool ipOurs = (e.Device as Mono.Nat.Upnp.UpnpNatDevice).LocalAddress.ToString() == Mono.Nat.NatUtility.LocalIpAddress; //I think this will always be true
                if (ipOurs)
                {
                    int oneWeek = 60 * 60 * 24 * 7; // == 604800.  on my home router I request 1 week, I get back 604800 in the mapping. but then on getting it again its 22 hours (which is probably the real time)
                    System.Threading.Tasks.Task<Mono.Nat.Mapping> t = e.Device.CreatePortMapAsync(new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp, SoulSeekState.ListenerPort, SoulSeekState.ListenerPort, oneWeek, "Android Seeker"));
                    try
                    {
                        bool timeOutCreateMapping = !(t.Wait(5000));
                        if (timeOutCreateMapping)
                        {
                            MainActivity.LogFirebase("CreatePortMapAsync timeout");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        //the task can throw (in which case the task.Wait throws)

                        if (ex.InnerException is Mono.Nat.MappingException && ex.InnerException.Message != null && ex.InnerException.Message.Contains("Error 725: OnlyPermanentLeasesSupported")) //happened on my tablet... connected to my other 192.168.1.1 router
                        {
                            System.Threading.Tasks.Task<Mono.Nat.Mapping> t0 = e.Device.CreatePortMapAsync(new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp, SoulSeekState.ListenerPort, SoulSeekState.ListenerPort, 0, "Android Seeker"));
                            try
                            {
                                bool timeOutCreateMapping0lease = !(t0.Wait(5000));
                                if (timeOutCreateMapping0lease)
                                {
                                    MainActivity.LogFirebase("CreatePortMapAsync timeout try with 0 lease");
                                    return;
                                }
                                t = t0; // use this good task instead. bc t.Result is gonna throw heh
                            }
                            catch (Exception ex0)
                            {
                                MainActivity.LogFirebase("CreatePortMapAsync try with 0 lease " + ex0.Message + ex0.StackTrace);
                                return;
                            }
                        }
                        else
                        {
                            //common errors are:
                            //Error ConflictInMappingEntry: ConflictInMappingEntry
                            //Error 403: Not Available Action
                            //Unexpected error sending a message to the device
                            //Error 714: NoSuchEntryInArray
                            //Error ActionFailed: Action Failed
                            //InvalidArgs

                            MainActivity.LogDebug("CreatePortMapAsync " + ex.Message + ex.StackTrace);
                            return;
                        }
                    }
                    Mono.Nat.Mapping mapping = t.Result;
                    int seconds = mapping.Lifetime;
                    int privatePort = mapping.PrivatePort;
                    int publicPort = mapping.PublicPort;

                    System.Threading.Tasks.Task<Mono.Nat.Mapping> t2 = e.Device.GetSpecificMappingAsync(Mono.Nat.Protocol.Tcp, SoulSeekState.ListenerPort);
                    try
                    {
                        bool timeOutGetMapping = !(t2.Wait(5000));
                        if (timeOutGetMapping)
                        {
                            MainActivity.LogFirebase("GetSpecificMappingAsync timeout");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        //the task can throw (in which case the task.Wait throws)
                        MainActivity.LogFirebase("GetSpecificMappingAsync " + ex.Message + ex.StackTrace);
                        return;
                    }
                    Mono.Nat.Mapping actualMapping = t2.Result;

                    //set lifetime and last set.
                    LastSetTime = DateTime.UtcNow;
                    LastSetLifeTime = actualMapping.Lifetime; //TODO: if two devices found get the min...

                    //since we use the lifetime value to make decisions and schedule remapping we need to deal with very low values or 0.
                    //0 means indeterminate, but still may want to remap occasionally...
                    if (LastSetLifeTime == 0)
                    {
                        LastSetLifeTime = 4 * 3600;
                    }
                    else if (LastSetLifeTime < 2 * 3600)
                    {
                        MainActivity.LogFirebase("less than 2 hours: " + LastSetLifeTime); //20 mins
                        LastSetLifeTime = 2 * 3600;
                    }

                    LastSetLocalIP = LocalIP;
                    LastSetPort = actualMapping.PublicPort;
                    SaveUpnpState();

                    Interlocked.Increment(ref DevicesSuccessfullyMapped);
                    MainActivity.LogDebug("successfully mapped");
                    DiagStatus = UpnpDiagStatus.Success;
                    DeviceSuccessfullyMapped?.Invoke(null, new EventArgs());
                }
                else
                {
                    MainActivity.LogFirebase("ip is not ours");
                }
            }
            catch (Exception ex)
            {
                MainActivity.LogFirebase("NatUtility_DeviceFound " + ex.Message + ex.StackTrace);
            }
            //e.Device.CreatePortMapAsync(new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp,3000,3000)).Wait();
        }
        private void NatUtility_UnknownDeviceFound(object sender, Mono.Nat.DeviceEventUnknownArgs e)
        {
            System.Console.WriteLine(e.Data);
            //nothing to do here...
        }
    }
    public interface ITransferItem
    {
        public string GetDisplayName();
        public string GetFolderName();
        public string GetUsername();
        public TimeSpan? GetRemainingTime();
        public double GetAvgSpeed();
        public int GetQueueLength();
        public bool IsUpload();
    }

    [Serializable]
    public class FolderItem : ITransferItem
    {
        public bool IsUpload()
        {
            if (TransferItems.Count == 0)
            {
                return false; //usually this is if we are in the process of clearing the folder....
            }
            return TransferItems[0].IsUpload();
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public TimeSpan? RemainingFolderTime; //this should never be serialized

        public TimeSpan? GetRemainingTime()
        {
            return RemainingFolderTime;
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public double AvgSpeed; //this could one day be serialized if you want say speed history (like QT does)

        public double GetAvgSpeed()
        {
            return AvgSpeed;
        }

        public string GetDisplayName()
        {
            return FolderName;
        }

        public string GetFolderName()
        {
            return FolderName;
        }

        public string GetUsername()
        {
            return Username;
        }

        public string GetDisplayFolderName()
        {
            //this is similar to QT (in the case of Seeker multiple subdirectories)
            //but not quite.
            //QT will show subdirs/complete/Soulseek Downloads/Music/H: (where everything after subdirs is your download folder)
            //whereas we just show subdirs
            //subdirs is folder name in both cases for single folder, 
            // and say (01 / 2020 / test_folder) for nested.
            if (GetDirectoryLevel() == 1)
            {
                //they are the same
                return FolderName;
            }
            else
            {
                //split reverse.
                var reversedArray = this.FolderName.Split('\\').Reverse();
                return string.Join('\\', reversedArray);
            }
        }

        public int GetDirectoryLevel()
        {
            //just parent folder = level 1 (search result and browse single dir case)
            //grandparent = level 2 (browse download subdirs case - i.e. Album, Album > covers)
            //etc.
            if (this.FolderName == null || !this.FolderName.Contains('\\'))
            {
                return 1;
            }
            return this.FolderName.Split('\\').Count();
        }

        /// <summary>
        /// int - percent.
        /// </summary>
        /// <returns></returns>
        public int GetFolderProgress(out long totalBytes, out long bytesCompleted)
        {
            lock (TransferItems)
            {
                long folderBytesComplete = 0;
                long totalFolderBytes = 0;
                foreach (TransferItem ti in TransferItems)
                {
                    folderBytesComplete += (long)((ti.Progress / 100.0) * ti.Size);
                    totalFolderBytes += ti.Size;
                }
                totalBytes = totalFolderBytes;
                bytesCompleted = folderBytesComplete;
                //error "System.OverflowException: Value was either too large or too small for an Int32." can occur for example when totalFolderBytes is 0
                if (totalFolderBytes == 0)
                {
                    MainActivity.LogInfoFirebase("total folder bytes == 0");
                    return 100;
                }
                else
                {
                    return Convert.ToInt32((folderBytesComplete * 100.0 / totalFolderBytes));
                }
            }
        }

        /// <summary>
        /// Get the overall queue of the folder (the lowest queued track)
        /// </summary>
        /// <returns></returns>
        public int GetQueueLength()
        {
            lock (TransferItems)
            {
                int queueLen = int.MaxValue;
                foreach (TransferItem ti in TransferItems)
                {
                    if (ti.State == TransferStates.Queued)
                    {
                        queueLen = Math.Min(ti.QueueLength, queueLen);
                    }
                }
                return queueLen;
            }
        }

        public TransferItem GetLowestQueuedTransferItem()
        {
            lock (TransferItems)
            {
                int queueLen = int.MaxValue;
                TransferItem curLowest = null;
                foreach (TransferItem ti in TransferItems)
                {
                    if (ti.State == TransferStates.Queued)
                    {
                        queueLen = Math.Min(ti.QueueLength, queueLen);
                        if (queueLen == ti.QueueLength)
                        {
                            curLowest = ti;
                        }
                    }
                }
                return curLowest;
            }
        }


        /// <summary>
        /// Get the overall state of the folder.
        /// </summary>
        /// <returns></returns>
        public TransferStates GetState(out bool isFailed, out bool anyOffline)
        {
            //top priority - In Progress
            //if ANY are InProgress then this is considered in progress
            //if not then if ANY initialized.
            //if not then if ANY queued its considered queued.  And the queue number is that of the lowest transfer.
            //if not then if ANY failed its considered failed
            //if not then its cancelled (i.e. paused)
            //if not then its Succeeded.

            isFailed = false;
            anyOffline = false;
            //if not then none...
            lock (TransferItems)
            {
                TransferStates folderState = TransferStates.None;
                foreach (TransferItem ti in TransferItems)
                {
                    TransferStates state = ti.State;
                    if (state == TransferStates.InProgress)
                    {
                        isFailed = false;
                        return TransferStates.InProgress;
                    }
                    else
                    {
                        if (ti.Failed)
                        {
                            isFailed = true;
                            if(ti.State.HasFlag(TransferStates.UserOffline))
                            {
                                anyOffline = true;
                            }
                        }
                        //do priority
                        if (state.HasFlag(TransferStates.Initializing) || state.HasFlag(TransferStates.Requested) || state.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if (state.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if ((state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut)) && !folderState.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if (state.HasFlag(TransferStates.Cancelled) && !folderState.HasFlag(TransferStates.Rejected) && !folderState.HasFlag(TransferStates.TimedOut) && !folderState.HasFlag(TransferStates.Errored) && !folderState.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                        else if (state.HasFlag(TransferStates.Succeeded) && !folderState.HasFlag(TransferStates.Rejected) && !folderState.HasFlag(TransferStates.TimedOut) && !folderState.HasFlag(TransferStates.Cancelled) && !folderState.HasFlag(TransferStates.Queued) && !folderState.HasFlag(TransferStates.Errored) && !folderState.HasFlag(TransferStates.Initializing) && !folderState.HasFlag(TransferStates.Requested) && !folderState.HasFlag(TransferStates.Aborted))
                        {
                            folderState = state;
                        }
                    }
                }
                return folderState;
            }
        }

        public string FolderName; //this is always ex "Album Name" or for depth > 1 "GrandParent/Parent".  Display Folder name is reversed.
        public string Username;
        public List<TransferItem> TransferItems;

        public FolderItem(string folderName, string username, TransferItem initialTransferItem)
        {
            TransferItems = new List<TransferItem>();
            Add(initialTransferItem);
            if (folderName == null)
            {
                folderName = Helpers.GetFolderNameFromFile(initialTransferItem.FullFilename);
            }
            FolderName = folderName;
            Username = username;
        }

        /// <summary>
        /// default public constructor for serialization.
        /// </summary>
        public FolderItem()
        {
            TransferItems = new List<TransferItem>();
        }

        public void ClearAllComplete()
        {
            lock (TransferItems)
            {
                TransferItems.RemoveAll((TransferItem ti) => { return ti.Progress > 99; });
                if (IsUpload())
                {
                    TransferItems.RemoveAll((TransferItem i) => { return Helpers.IsUploadCompleteOrAborted(i.State); });
                }
            }
        }

        public bool HasTransferItem(TransferItem ti)
        {
            lock (TransferItems)
            {
                return TransferItems.Contains(ti);
            }
        }

        public bool IsEmpty()
        {
            return TransferItems.Count == 0;
        }

        public void Remove(TransferItem ti)
        {
            lock (TransferItems)
            {
                TransferItems.Remove(ti);
            }
        }


        public void Add(TransferItem ti)
        {
            lock (TransferItems)
            {
                TransferItems.Add(ti);
            }
        }
    }

    /// <summary>
    /// for both uploads and downloads
    /// </summary>
    public class TransferItemManagerWrapper
    {
        private TransferItemManager Uploads;
        private TransferItemManager Downloads;
        public TransferItemManagerWrapper(TransferItemManager up, TransferItemManager down)
        {
            Uploads = up;
            Downloads = down;
        }

        public static void CleanupEntry(IEnumerable<TransferItem> tis)
        {
            MainActivity.LogDebug("launching cleanup entry");
            System.Threading.ThreadPool.QueueUserWorkItem(PeformCleanup, tis);
        }

        public static void CleanupEntry(TransferItem ti)
        {
            MainActivity.LogDebug("launching cleanup entry");
            System.Threading.ThreadPool.QueueUserWorkItem(PeformCleanup, ti);
        }

        static void PeformCleanup(object state)
        {
            try
            {
                MainActivity.LogDebug("in cleanup entry");
                if (state is IEnumerable<TransferItem> tis)
                {
                    PerfomCleanupItems(tis.ToList()); //added tolist() due to enumerable exception.
                }
                else
                {
                    PerformCleanupItem(state as TransferItem);
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("PeformCleanup: " + e.Message + e.StackTrace);
            }
        }

        public IEnumerable<TransferItem> GetTransferItemsForUser(string username)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetTransferItemsForUser(username);
            }
            else
            {
                return Downloads.GetTransferItemsForUser(username);
            }
        }

        public void CancelSelectedItems(bool prepareForClean)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.CancelSelectedItems(prepareForClean);
            }
            else
            {
                Downloads.CancelSelectedItems(prepareForClean);
            }
        }

        public void ClearSelectedItemsAndClean()
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.ClearSelectedItemsAndClean();
            }
            else
            {
                Downloads.ClearSelectedItemsAndClean();
            }
        }

        public static void PerfomCleanupItems(IEnumerable<TransferItem> tis)
        {
            foreach (TransferItem ti in tis)
            {
                PerformCleanupItem(ti);
            }
        }

        public static void PerformCleanupItem(TransferItem ti)
        {
            MainActivity.LogDebug("cleaning up: " + ti.Filename);
            //if (TransfersFragment.TransferItemManagerDL.ExistsAndInProcessing(ti.FullFilename, ti.Username, ti.Size))
            //{
            //    //this should rarely happen. its a race condition if someone clears a download and then goes back to the person they downloaded from to re-download.
            //    return;
            //}
            //api 21+
            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            {
                DocumentFile parent = null;
                Android.Net.Uri parentIncompleteUri = Android.Net.Uri.Parse(ti.IncompleteParentUri);
                if (SoulSeekState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || parentIncompleteUri.Scheme == "file")
                {
                    parent = DocumentFile.FromFile(new Java.IO.File(parentIncompleteUri.Path));
                }
                else
                {
                    parent = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, parentIncompleteUri); //if from single uri then listing files will give unsupported operation exception...  //if temp (file: //)this will throw (which makes sense as it did not come from open tree uri)
                }

                DocumentFile df = parent.FindFile(ti.Filename);
                if (df == null || !df.Exists())
                {
                    MainActivity.LogDebug("delete failed - null or not exist");
                    MainActivity.LogInfoFirebase("df is null or not exist: " + parentIncompleteUri + " " + SoulSeekState.CreateCompleteAndIncompleteFolders + " " + parent.Uri + " " + SettingsActivity.UseIncompleteManualFolder());
                }
                if (!df.Delete()) //nullref
                {
                    MainActivity.LogDebug("delete failed");
                }
                MainActivity.DeleteParentIfEmpty(parent);
            }
            else
            {
                Java.IO.File parent = new Java.IO.File(Android.Net.Uri.Parse(ti.IncompleteParentUri).Path);
                Java.IO.File f = parent.ListFiles().First((file) => file.Name == ti.Filename);
                if (f == null || !f.Exists())
                {
                    MainActivity.LogDebug("delete failed LEGACY - null or not exist");
                }
                if (!f.Delete())
                {
                    MainActivity.LogDebug("delete failed LEGACY");
                }
                MainActivity.DeleteParentIfEmpty(parent);
            }
        }


        /// <summary>
        /// remove and spawn cleanup task if applicable
        /// </summary>
        /// <param name="ti"></param>
        public void RemoveAndCleanUp(TransferItem ti)
        {
            Remove(ti);
            if (NeedsCleanUp(ti))
            {
                CleanupEntry(ti);
            }
        }

        public static bool NeedsCleanUp(TransferItem ti)
        {
            if (ti != null && ti.IncompleteParentUri != null && !ti.CancelAndClearFlag) //if cancel and clear flag is set then it will be cleaned up on continuation. that way we are sure the stream is closed.
            {
                return true;
            }
            return false;
        }


        public void Remove(TransferItem ti)
        {
            if (ti.IsUpload())
            {
                Uploads.Remove(ti);
            }
            else
            {
                Downloads.Remove(ti);
            }
        }

        public object GetUICurrentList()
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetUICurrentList();
            }
            else
            {
                return Downloads.GetUICurrentList();
            }
        }

        public TransferItem GetTransferItemWithIndexFromAll(string fullFileName, string username, bool isUpload, out int indexOfItem)
        {
            if (isUpload)
            {
                return Uploads.GetTransferItemWithIndexFromAll(fullFileName, username, out indexOfItem);
            }
            else
            {
                return Downloads.GetTransferItemWithIndexFromAll(fullFileName, username, out indexOfItem);
            }
        }

        public int GetUserIndexForTransferItem(TransferItem ti) //todo null ti
        {
            if (TransfersFragment.InUploadsMode && ti.IsUpload())
            {
                return Uploads.GetUserIndexForTransferItem(ti);
            }
            else if (!TransfersFragment.InUploadsMode && !(ti.IsUpload()))
            {
                return Downloads.GetUserIndexForTransferItem(ti);
            }
            else
            {
                return -1; //this is okay. we arent on that page so ui events are irrelevant
            }
        }

        public ITransferItem GetItemAtUserIndex(int position)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetItemAtUserIndex(position);
            }
            else
            {
                return Downloads.GetItemAtUserIndex(position);
            }
        }

        public object RemoveAtUserIndex(int position)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.RemoveAtUserIndex(position);
            }
            else
            {
                return Downloads.RemoveAtUserIndex(position);
            }
        }

        /// <summary>
        /// remove and spawn cleanup task if applicable
        /// </summary>
        /// <param name="position"></param>
        public void RemoveAndCleanUpAtUserIndex(int position)
        {
            object objectRemoved = RemoveAtUserIndex(position);
            if (objectRemoved is TransferItem ti)
            {
                if (ti.InProcessing)
                {
                    ti.CancelAndClearFlag = true;
                }
                else
                {
                    if (NeedsCleanUp(ti))
                    {
                        CleanupEntry(ti);
                    }
                }
            }
            else
            {
                List<TransferItem> tis = objectRemoved as List<TransferItem>;
                IEnumerable<TransferItem> tisCleanUpOnComplete = tis.Where((item) => { return item.InProcessing; });
                foreach (var item in tisCleanUpOnComplete)
                {
                    item.CancelAndClearFlag = true;
                }
                IEnumerable<TransferItem> tisNeedingCleanup = tis.Where((item) => { return NeedsCleanUp(item); });
                if (tisNeedingCleanup.Any())
                {
                    CleanupEntry(tisNeedingCleanup);
                }

            }
        }

        public void CancelFolder(FolderItem fi)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.CancelFolder(fi);
            }
            else
            {
                Downloads.CancelFolder(fi);
            }
        }

        /// <summary>
        /// prepare for clear basically says, these guys are going to be cleared, so if they are currently being processed and they get in the download continuation action, clear their incomplete files...
        /// </summary>
        /// <param name="fi"></param>
        /// <param name="prepareForClear"></param>
        public void CancelFolder(FolderItem fi, bool prepareForClear = false)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.CancelFolder(fi);
            }
            else
            {
                Downloads.CancelFolder(fi, prepareForClear);
            }
        }

        public void ClearAllFromFolder(FolderItem fi)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.ClearAllFromFolder(fi);
            }
            else
            {
                Downloads.ClearAllFromFolder(fi);
            }
        }

        public void ClearAllFromFolderAndClean(FolderItem fi)
        {
            if (TransfersFragment.InUploadsMode)
            {
                Uploads.ClearAllFromFolder(fi);
            }
            else
            {
                Downloads.ClearAllFromFolderAndClean(fi);
            }
        }

        public int GetIndexForFolderItem(FolderItem folderItem)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetIndexForFolderItem(folderItem);
            }
            else
            {
                return Downloads.GetIndexForFolderItem(folderItem);
            }
        }

        public int GetUserIndexForITransferItem(ITransferItem iti)
        {
            if (TransfersFragment.InUploadsMode)
            {
                return Uploads.GetUserIndexForITransferItem(iti);
            }
            else
            {
                return Downloads.GetUserIndexForITransferItem(iti);
            }
        }
    }

    [Serializable]
    public class TransferItemManager
    {
        private bool isUploads;
        /// <summary>
        /// Do not use directly.  This is public only for default serialization.
        /// </summary>
        public List<TransferItem> AllTransferItems;

        /// <summary>
        /// Do not use directly.  This is public only for default serialization.
        /// </summary>
        public List<FolderItem> AllFolderItems;

        public TransferItemManager()
        {
            AllTransferItems = new List<TransferItem>();
            AllFolderItems = new List<FolderItem>();
        }

        public TransferItemManager(bool _isUploads)
        {
            isUploads = _isUploads;
            AllTransferItems = new List<TransferItem>();
            AllFolderItems = new List<FolderItem>();
        }

        public IEnumerable<TransferItem> GetTransferItemsForUser(string username)
        {
            lock (AllTransferItems)
            {
                return AllTransferItems.Where((item) => item.Username == username).ToList();
            }
        }

        /// <summary>
        /// transfers that were previously InProgress before we shut down should now be considered paused (cancelled)
        /// add users where failure occured due to them being offline to dict so we can efficiently check it in response
        /// to status changed events AND we can AddUser to get their status updates.
        /// </summary>
        public void OnRelaunch()
        {            
            lock (AllTransferItems)
            {
                foreach (var ti in AllTransferItems)
                {
                    if (ti.State.HasFlag(TransferStates.InProgress))
                    {
                        ti.State = TransferStates.Cancelled;
                        ti.RemainingTime = null;
                    }

                    if(ti.State.HasFlag(TransferStates.Aborted))
                    {
                        ti.State = TransferStates.Cancelled;
                        ti.RemainingTime = null;
                    }

                    if(ti.State.HasFlag(TransferStates.UserOffline))
                    {
                        TransfersFragment.UsersWhereDownloadFailedDueToOffline[ti.Username] = 0x0;
                    }
                }
            }
        }

        public List<Tuple<TransferItem, int>> GetListOfPausedFromFolder(FolderItem fi)
        {
            List<Tuple<TransferItem, int>> transferItemConditionList = new List<Tuple<TransferItem, int>>();
            lock (fi.TransferItems)
            {
                for (int i = 0; i < fi.TransferItems.Count; i++)
                {
                    var item = fi.TransferItems[i];

                    if (item.State.HasFlag(TransferStates.Cancelled) || item.State.HasFlag(TransferStates.Queued))
                    {
                        transferItemConditionList.Add(new Tuple<TransferItem, int>(item, i));
                    }

                }
            }
            return transferItemConditionList;
        }

        public List<Tuple<TransferItem, int, int>> GetListOfPaused()
        {
            List<Tuple<TransferItem, int, int>> transferItemConditionList = new List<Tuple<TransferItem, int, int>>();
            lock (AllTransferItems)
            {
                lock (AllFolderItems)
                {
                    for (int i = 0; i < AllTransferItems.Count; i++)
                    {
                        var item = AllTransferItems[i];

                        if (item.State.HasFlag(TransferStates.Cancelled) || item.State.HasFlag(TransferStates.Queued))
                        {
                            int folderIndex = -1;
                            for (int fi = 0; fi < AllFolderItems.Count; fi++)
                            {
                                if (AllFolderItems[fi].HasTransferItem(item))
                                {
                                    folderIndex = fi;
                                    break;
                                }

                            }
                            transferItemConditionList.Add(new Tuple<TransferItem, int, int>(AllTransferItems[i], i, folderIndex));
                        }

                    }
                }
            }
            return transferItemConditionList;
        }

        public List<Tuple<TransferItem, int>> GetListOfFailedFromFolder(FolderItem fi)
        {
            List<Tuple<TransferItem, int>> transferItemConditionList = new List<Tuple<TransferItem, int>>();
            lock (fi.TransferItems)
            {
                for (int i = 0; i < fi.TransferItems.Count; i++)
                {
                    var item = fi.TransferItems[i];

                    if (item.Failed)
                    {
                        transferItemConditionList.Add(new Tuple<TransferItem, int>(item, i));
                    }

                }
            }
            return transferItemConditionList;
        }

        public List<Tuple<TransferItem, int, int>> GetListOfFailed()
        {
            List<Tuple<TransferItem, int, int>> transferItemConditionList = new List<Tuple<TransferItem, int, int>>();
            lock (AllTransferItems)
            {
                lock (AllFolderItems)
                {
                    for (int i = 0; i < AllTransferItems.Count; i++)
                    {
                        var item = AllTransferItems[i];

                        if (item.Failed)
                        {
                            int folderIndex = -1;
                            for (int fi = 0; fi < AllFolderItems.Count; fi++)
                            {
                                if (AllFolderItems[fi].HasTransferItem(item))
                                {
                                    folderIndex = fi;
                                    break;
                                }

                            }
                            transferItemConditionList.Add(new Tuple<TransferItem, int, int>(AllTransferItems[i], i, folderIndex));
                        }

                    }
                }
            }
            return transferItemConditionList;
        }

        public List<TransferItem> GetListOfCondition(TransferStates state)
        {
            List<TransferItem> transferItemConditionList = new List<TransferItem>();
            lock (AllTransferItems)
            {
                for (int i = 0; i < AllTransferItems.Count; i++)
                {
                    var item = AllTransferItems[i];

                    if (item.State.HasFlag(state))
                    {
                        transferItemConditionList.Add(item);
                    }

                }
            }
            return transferItemConditionList;
        }

        public List<TransferItem> GetTransferItemsFromUser(string username, bool failedOnly, bool failedAndOfflineOnly)
        {
            List<TransferItem> transferItemConditionList = new List<TransferItem>();
            lock (AllTransferItems)
            {
                foreach(var item in AllTransferItems)
                {
                    if(item.Username==username)
                    {
                        if(failedAndOfflineOnly && !item.State.HasFlag(TransferStates.UserOffline))
                        {
                            continue;
                        }
                        if(failedOnly && !item.Failed)
                        {
                            continue;
                        }
                        transferItemConditionList.Add(item);
                    }
                }
            }
            return transferItemConditionList;
        }

        public object GetUICurrentList()
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems;
                }
                else
                {
                    return AllFolderItems;
                }
            }
            else
            {
                return AllTransferItems;
            }
        }

        /// <summary>
        /// Returns the removed object (either TransferItem or List of TransferItem)
        /// </summary>
        /// <param name="indexOfItem"></param>
        /// <returns></returns>
        public object RemoveAtUserIndex(int indexOfItem)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    var ti = TransfersFragment.GetCurrentlySelectedFolder().TransferItems[indexOfItem];
                    Remove(ti);
                    return ti;
                }
                else
                {
                    List<TransferItem> transferItemsToRemove = new List<TransferItem>();
                    lock (AllFolderItems[indexOfItem].TransferItems)
                    {
                        foreach (var ti in AllFolderItems[indexOfItem].TransferItems)
                        {
                            transferItemsToRemove.Add(ti);
                        }
                    }
                    foreach (var ti in transferItemsToRemove)
                    {
                        Remove(ti);
                    }
                    return transferItemsToRemove;
                }
            }
            else
            {
                var ti = AllTransferItems[indexOfItem];
                Remove(ti);
                return ti;
            }
        }

        public ITransferItem GetItemAtUserIndex(int indexOfItem)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems[indexOfItem];
                }
                else
                {
                    return AllFolderItems[indexOfItem];
                }
            }
            else
            {
                return AllTransferItems[indexOfItem];
            }
        }

        /// <summary>
        /// The index in the folder, the folder, or the overall index
        /// </summary>
        /// <param name="indexOfItem"></param>
        /// <returns></returns>
        public int GetUserIndexForTransferItem(TransferItem ti)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems.IndexOf(ti);
                }
                else
                {
                    string foldername = ti.FolderName;
                    if (foldername == null)
                    {
                        foldername = Helpers.GetFolderNameFromFile(ti.FullFilename);
                    }
                    return AllFolderItems.FindIndex((FolderItem fi) => { return fi.FolderName == foldername && fi.Username == ti.Username; });
                }
            }
            else
            {
                return AllTransferItems.IndexOf(ti);
            }
        }

        public int GetIndexForFolderItem(FolderItem ti)
        {
            lock (AllFolderItems)
            {
                return AllFolderItems.IndexOf(ti);
            }
        }

        public int GetUserIndexForITransferItem(ITransferItem iti)
        {
            if (iti is TransferItem ti)
            {
                return GetUserIndexForTransferItem(ti);
            }
            else if (iti is FolderItem fi)
            {
                return GetIndexForFolderItem(fi);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// The index in the folder, the folder, or the overall index
        /// </summary>
        /// <param name="indexOfItem"></param>
        /// <returns></returns>
        public int GetUserIndexForTransferItem(string fullfilename)
        {
            if (TransfersFragment.GroupByFolder)
            {
                if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                {
                    return TransfersFragment.GetCurrentlySelectedFolder().TransferItems.FindIndex((ti) => ti.FullFilename == fullfilename);
                }
                else
                {
                    TransferItem ti;
                    lock (AllTransferItems)
                    {
                        ti = AllTransferItems.Find((ti) => ti.FullFilename == fullfilename);
                    }
                    string foldername = ti.FolderName;
                    if (foldername == null)
                    {
                        foldername = Helpers.GetFolderNameFromFile(ti.FullFilename);
                    }
                    return AllFolderItems.FindIndex((FolderItem fi) => { return fi.FolderName == foldername && fi.Username == ti.Username; });
                }
            }
            else
            {
                return AllTransferItems.FindIndex((ti) => ti.FullFilename == fullfilename);
            }
        }


        //public TransferItem GetTransferItemWithUserIndex(string fullFileName, out int indexOfItem)
        //{
        //    if (fullFileName == null)
        //    {
        //        indexOfItem = -1;
        //        return null;
        //    }
        //    lock (AllTransferItems)
        //    {
        //        foreach (TransferItem item in AllTransferItems)
        //        {
        //            if (item.FullFilename.Equals(fullFileName)) //fullfilename includes dir so that takes care of any ambiguity...
        //            {
        //                indexOfItem = AllTransferItems.IndexOf(item);
        //                return item;
        //            }
        //        }
        //    }
        //    indexOfItem = -1;
        //    return null;
        //}


        public TransferItem GetTransferItemWithIndexFromAll(string fullFileName, string username, out int indexOfItem)
        {
            if (fullFileName == null || username == null)
            {
                indexOfItem = -1;
                return null;
            }
            lock (AllTransferItems)
            {
                foreach (TransferItem item in AllTransferItems)
                {
                    if (item.FullFilename.Equals(fullFileName) && item.Username.Equals(username)) //fullfilename includes dir so that takes care of any ambiguity...
                    {
                        indexOfItem = AllTransferItems.IndexOf(item);
                        return item;
                    }
                }
            }
            indexOfItem = -1;
            return null;
        }

        public bool Exists(string fullFilename, string username, long size)
        {
            lock (AllTransferItems)
            {
                return AllTransferItems.Exists((TransferItem ti) =>
                {
                    return (ti.FullFilename == fullFilename &&
                           ti.Size == size &&
                           ti.Username == username
                       );
                });
            }
        }

        public bool ExistsAndInProcessing(string fullFilename, string username, long size)
        {
            lock (AllTransferItems)
            {
                return AllTransferItems.Where((TransferItem ti) =>
                {
                    return (ti.FullFilename == fullFilename &&
                           ti.Size == size &&
                           ti.Username == username
                       );
                }).Any((item) => item.InProcessing);
            }
        }

        public bool IsEmpty()
        {
            return AllTransferItems.Count == 0;
        }

        public TransferItem GetTransferItem(string fullfilename)
        {
            lock (AllTransferItems)
            {
                foreach (TransferItem item in AllTransferItems) //THIS is where those enumeration exceptions are all coming from...
                {
                    if (item.FullFilename.Equals(fullfilename))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        public bool IsFolderNowComplete(TransferItem ti, bool noSingleItemFolders = false) //!!!!! no single item logic will not work with AutoClearComplete
        {
            if(ti==null)
            {
                MainActivity.LogDebug("IsFolderNowComplete: transferitem is null");
                return false;
            }
            else
            {
                FolderItem folder = null;
                lock (AllFolderItems)
                {
                    folder = GetMatchingFolder(ti).FirstOrDefault();
                }
                if(folder == null)
                {
                    MainActivity.LogDebug("IsFolderNowComplete: folder is null");
                    return false;
                }
                lock(folder.TransferItems)
                {
                    foreach(TransferItem item in folder.TransferItems)
                    {
                        if(!item.State.HasFlag(TransferStates.Succeeded))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private IEnumerable<FolderItem> GetMatchingFolder(TransferItem ti)
        {
            lock (AllFolderItems)
            {
                string foldername = string.Empty;
                if (string.IsNullOrEmpty(ti.FolderName))
                {
                    foldername = Helpers.GetFolderNameFromFile(ti.FullFilename);
                }
                else
                {
                    foldername = ti.FolderName;
                }
                return AllFolderItems.Where((folder) => folder.FolderName == foldername && folder.Username == ti.Username);
            }
        }

        private int GetMatchingFolderIndex(TransferItem ti)
        {
            lock (AllFolderItems)
            {
                for (int i = 0; i < AllFolderItems.Count; i++)
                {
                    if (AllFolderItems[i].HasTransferItem(ti))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// This way we will have the right reference
        /// </summary>
        /// <param name="ti"></param>
        /// <returns></returns>
        public TransferItem AddIfNotExistAndReturnTransfer(TransferItem ti, out bool exists)
        {
            lock (AllTransferItems)
            {
                var linq = AllTransferItems.Where((existingTi) => { return existingTi.Username == ti.Username && existingTi.FullFilename == ti.FullFilename; });
                if (linq.Count() > 0)
                {
                    exists = true;
                    return linq.First();
                }
                else
                {
                    Add(ti);
                    exists = false;
                    return ti;
                }
            }
        }

        public void Add(TransferItem ti)
        {
            lock (AllTransferItems)
            {
                AllTransferItems.Add(ti);
            }
            lock (AllFolderItems)
            {
                var matchingFolder = GetMatchingFolder(ti);
                if (matchingFolder.Count() == 0)
                {
                    AllFolderItems.Add(new FolderItem(ti.FolderName, ti.Username, ti));
                }
                else
                {
                    var folderItem = matchingFolder.First();
                    folderItem.Add(ti);
                }
            }
        }

        public void ClearAllComplete()
        {
            lock (AllTransferItems)
            {
                AllTransferItems.RemoveAll((TransferItem i) => { return i.Progress > 99; });
                if (isUploads)
                {
                    AllTransferItems.RemoveAll((TransferItem i) => { return Helpers.IsUploadCompleteOrAborted(i.State); });
                }
            }
            lock (AllFolderItems)
            {
                foreach (FolderItem f in AllFolderItems)
                {
                    f.ClearAllComplete();
                }
                AllFolderItems.RemoveAll((FolderItem f) => { return f.IsEmpty(); });
            }
        }

        public void ClearAllCompleteFromFolder(FolderItem fi)
        {
            lock (AllTransferItems)
            {
                AllTransferItems.RemoveAll((TransferItem i) => { return i.Progress > 99 && fi.Username == i.Username && GetFolderNameFromTransferItem(i) == fi.FolderName; });
            }
            fi.ClearAllComplete();
            if (fi.IsEmpty())
            {
                AllFolderItems.Remove(fi);
            }
        }

        private static string GetFolderNameFromTransferItem(TransferItem ti)
        {
            if (string.IsNullOrEmpty(ti.FolderName)) //this wont happen with the latest code.  so no need to worry about depth.
            {
                return Helpers.GetFolderNameFromFile(ti.FullFilename);
            }
            else
            {
                return ti.FolderName;
            }
        }

        public void ClearAllAndClean()
        {
            lock (AllTransferItems)
            {
                List<TransferItem> tisNeedingCleanup = AllTransferItems.Where((item) => { return TransferItemManagerWrapper.NeedsCleanUp(item); }).ToList();
                if (tisNeedingCleanup.Any())
                {
                    TransferItemManagerWrapper.CleanupEntry(tisNeedingCleanup);
                }
                AllTransferItems.Clear();
            }
            lock (AllFolderItems)
            {
                AllFolderItems.Clear();
            }
        }

        public void ClearSelectedItemsAndClean()
        {
            lock (AllTransferItems)
            {
                bool isFolderItems = false;
                if (TransfersFragment.GroupByFolder && TransfersFragment.GetCurrentlySelectedFolder() == null)
                {
                    isFolderItems = true;
                }


                if (isFolderItems)
                {
                    List<FolderItem> toClear = new List<FolderItem>();
                    foreach (int pos in TransfersFragment.BatchSelectedItems)
                    {
                        toClear.Add(GetItemAtUserIndex(pos) as FolderItem);
                    }
                    foreach (FolderItem item in toClear)
                    {
                        ClearAllFromFolderAndClean(item);
                    }

                }
                else
                {
                    List<TransferItem> toCleanUp = new List<TransferItem>();
                    List<TransferItem> toClear = new List<TransferItem>();
                    TransfersFragment.BatchSelectedItems.Sort();
                    TransfersFragment.BatchSelectedItems.Reverse();
                    foreach (int pos in TransfersFragment.BatchSelectedItems)
                    {
                        if (TransferItemManagerWrapper.NeedsCleanUp(GetItemAtUserIndex(pos) as TransferItem))
                        {
                            toCleanUp.Add(GetItemAtUserIndex(pos) as TransferItem);
                        }
                        this.RemoveAtUserIndex(pos);
                    }
                }
            }
        }

        public void ClearAll()
        {
            lock (AllTransferItems)
            {
                AllTransferItems.Clear();
            }
            lock (AllFolderItems)
            {
                AllFolderItems.Clear();
            }
        }

        public void ClearAllFromFolder(FolderItem fi)
        {
            lock(AllTransferItems)
            {
                foreach (TransferItem ti in fi.TransferItems)
                {
                    AllTransferItems.Remove(ti);
                }
            }
            fi.TransferItems.Clear();
            AllFolderItems.Remove(fi);
        }

        public void ClearAllFromFolderAndClean(FolderItem fi)
        {
            IEnumerable<TransferItem> tisNeedingCleanup = fi.TransferItems.Where((item) => { return TransferItemManagerWrapper.NeedsCleanUp(item); });
            if (tisNeedingCleanup.Any())
            {
                TransferItemManagerWrapper.CleanupEntry(tisNeedingCleanup);
            }
            lock(AllTransferItems)
            {
                foreach (TransferItem ti in fi.TransferItems)
                {
                    AllTransferItems.Remove(ti);
                }
            }
            fi.TransferItems.Clear();
            AllFolderItems.Remove(fi);
        }

        public void CancelAll(bool prepareForClear = false)
        {
            lock (AllTransferItems)
            {
                for (int i = 0; i < AllTransferItems.Count; i++)
                {
                    //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                    TransferItem ti = AllTransferItems[i];
                    if (prepareForClear)
                    {
                        if (ti.InProcessing) //let continuation action clear this guy
                        {
                            ti.CancelAndClearFlag = true;
                        }
                    }
                    TransfersFragment.CancellationTokens.TryGetValue(TransfersFragment.ProduceCancellationTokenKey(ti), out CancellationTokenSource token);
                    token?.Cancel();
                    //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
                }
                TransfersFragment.CancellationTokens.Clear();
            }
        }

        public void CancelSelectedItems(bool prepareForClear = false)
        {
            lock (AllTransferItems)
            {
                bool isFolderItems = false;
                if (TransfersFragment.GroupByFolder && TransfersFragment.GetCurrentlySelectedFolder() == null)
                {
                    isFolderItems = true;
                }

                for (int i = 0; i < TransfersFragment.BatchSelectedItems.Count; i++)
                {
                    //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                    if (isFolderItems)
                    {
                        FolderItem fi = this.GetItemAtUserIndex(TransfersFragment.BatchSelectedItems[i]) as FolderItem;
                        CancelFolder(fi, prepareForClear);
                    }
                    else
                    {
                        TransferItem ti = this.GetItemAtUserIndex(TransfersFragment.BatchSelectedItems[i]) as TransferItem;
                        if (prepareForClear)
                        {
                            if (ti.InProcessing) //let continuation action clear this guy
                            {
                                ti.CancelAndClearFlag = true;
                            }
                        }
                        TransfersFragment.CancellationTokens.TryRemove(TransfersFragment.ProduceCancellationTokenKey(ti), out CancellationTokenSource token);
                        token?.Cancel();
                    }
                    //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
                }
            }
        }

        public void CancelFolder(FolderItem fi, bool prepareForClear = false)
        {
            lock (fi.TransferItems)
            {
                for (int i = 0; i < fi.TransferItems.Count; i++)
                {
                    //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                    var ti = fi.TransferItems[i];
                    if (prepareForClear && ti.InProcessing)
                    {
                        ti.CancelAndClearFlag = true;
                    }
                    var key = TransfersFragment.ProduceCancellationTokenKey(ti);
                    TransfersFragment.CancellationTokens.TryGetValue(key, out CancellationTokenSource token);
                    if (token != null)
                    {
                        token.Cancel();
                        TransfersFragment.CancellationTokens.Remove(key, out _);
                    }
                }
            }
        }

        /// <summary>
        /// If its the folders last transfer then we remove the folder
        /// </summary>
        /// <param name="ti"></param>
        public void Remove(TransferItem ti)
        {
            lock (AllTransferItems)
            {
                AllTransferItems.Remove(ti);
            }
            lock (AllFolderItems)
            {
                var matchingFolder = GetMatchingFolder(ti);
                if (matchingFolder.Count() == 0)
                {
                    //error folder not found...
                }
                else
                {
                    var folderItem = matchingFolder.First();
                    folderItem.Remove(ti);
                    if (folderItem.IsEmpty())
                    {
                        AllFolderItems.Remove(folderItem);
                    }
                }
            }
        }

        /// <summary>
        /// i.e. the folders to NOT delete
        /// </summary>
        /// <returns></returns>
        public List<string> GetInUseIncompleteFolderNames()
        {
            List<string> foldersToNotDelete = new List<string>();
            lock (AllFolderItems)
            {
                foreach (FolderItem fi in AllFolderItems)
                {
                    foldersToNotDelete.Add(Helpers.GenerateIncompleteFolderName(fi.Username, fi.TransferItems.First().FullFilename, fi.GetDirectoryLevel()));
                }
            }
            return foldersToNotDelete;
        }
    }

    /// <summary>
    /// When we switch from wifi to data or vice versa, we want to try to continue our downloads and uploads seamlessly.
    /// We try to detect this event (as a netinfo disconnect (from old network) and then netinfo connect (with new network)).
    /// Then in the transfers failure we check if a recent* network handoff occured causing the remote connection to close
    /// And if so we retry the transfer.  *recent is tough to determine since you can still read from the pipe for a bit of time
    /// even if wifi is turned off.
    /// </summary>
    public static class NetworkHandoffDetector
    {
        public static bool NetworkSuccessfullyHandedOff = false;
        private static DateTime DisconnectedTime = DateTime.MinValue;
        private static DateTime NetworkHandOffTime = DateTime.MinValue;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="netInfo"></param>
        /// <returns>true if connected</returns>
        public static bool ProcessEvent(NetworkInfo netInfo)
        {
            if (netInfo == null)
            {

            }
            else
            {
                if (netInfo.IsConnected)
                {
                    if ((DateTime.UtcNow - DisconnectedTime).TotalSeconds < 2.0) //in practice .2s or less..
                    {
                        MainActivity.LogDebug("total seconds..." + (DateTime.UtcNow - DisconnectedTime).TotalSeconds);
                        NetworkHandOffTime = DateTime.UtcNow;
                        NetworkSuccessfullyHandedOff = true;
                    }
                    return true;
                }
                else
                {
                    NetworkSuccessfullyHandedOff = false;
                    DisconnectedTime = DateTime.UtcNow;
                }
            }
            return false;
        }

        public static bool HasHandoffOccuredRecently()
        {
            if (!NetworkSuccessfullyHandedOff)
            {
                return false;
            }
            else
            {
                MainActivity.LogDebug("total seconds..." + (DateTime.UtcNow - NetworkHandOffTime).TotalSeconds);
                return (DateTime.UtcNow - NetworkHandOffTime).TotalSeconds < 30.0; //in practice we can keep reading from the stream for a while so 30s is reasonable.
            }
        }
    }

    public class ConnectionReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            NetworkInfo netInfo = intent?.GetParcelableExtra("networkInfo") as NetworkInfo; //this will say Wifi Disconnected, and then Mobile Connected. so just wait for the "Connected" one.            
            bool isConnected = NetworkHandoffDetector.ProcessEvent(netInfo);

            MainActivity.LogDebug("ConnectionReceiver.OnReceive");
            //these are just toasts letting us know the status of the network..

            string action = intent?.Action;
            if (action != null && action == ConnectivityManager.ConnectivityAction)
            {
                bool changed = SeekerApplication.SetNetworkState(context);
                if(changed)
                {
                    MainActivity.LogDebug("metered state changed.. lets set up our handlers and inform server..");
                    MainActivity.SetUnsetSharingBasedOnConditions(true);
                    SoulSeekState.SharingStatusChangedEvent?.Invoke(null, new EventArgs());
                }

#if DEBUG

                ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);

                if (cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected)
                {
                    MainActivity.LogDebug("info: " + cm.ActiveNetworkInfo.GetDetailedState().ToString());
                    SeekerApplication.ShowToast("Is Connected", ToastLength.Long);
                    NetworkInfo info = cm.GetNetworkInfo(ConnectivityType.Wifi);
                    if (info.IsConnected)
                    {
                        SeekerApplication.ShowToast("Is Connected Wifi", ToastLength.Long);
                    }
                    info = cm.GetNetworkInfo(ConnectivityType.Mobile);
                    if (info.IsConnected)
                    {
                        SeekerApplication.ShowToast("Is Connected Mobile", ToastLength.Long);
                    }
                }
                else
                {
                    if (cm.ActiveNetworkInfo != null)
                    {
                        MainActivity.LogDebug("info: " + cm.ActiveNetworkInfo.GetDetailedState().ToString());
                        SeekerApplication.ShowToast("Is Disconnected", ToastLength.Long);
                    }
                    else
                    {
                        MainActivity.LogDebug("info: Is Disconnected(null)");
                        SeekerApplication.ShowToast("Is Disconnected (null)", ToastLength.Long);
                    }
                }

#endif


            }

        }

        public static bool DoWeHaveInternet()
        {
            ConnectivityManager cm = (ConnectivityManager)(SoulSeekState.ActiveActivityRef.GetSystemService(Context.ConnectivityService));
            return cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected;
        }
    }



    [Application]
    public class SeekerApplication : Application
    {
        public static Context ApplicationContext = null;
        public SeekerApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public const bool AUTO_CONNECT_ON = true;
        public static bool LOG_DIAGNOSTICS = false;

        public override void OnCreate()
        {
            base.OnCreate();
            ApplicationContext = this;
#if !IzzySoft
            Firebase.FirebaseApp app = Firebase.FirebaseApp.InitializeApp(this);
            if (app == null)
            {
                MainActivity.crashlyticsEnabled = false;
            }
#endif
            //MainActivity.LogFirebase("testing release");

            this.RegisterActivityLifecycleCallbacks(new ForegroundLifecycleTracker());
            this.RegisterReceiver(new ConnectionReceiver(), new IntentFilter(ConnectivityManager.ConnectivityAction));
            var sharedPrefs = this.GetSharedPreferences("SoulSeekPrefs", 0);
            SoulSeekState.SharedPreferences = sharedPrefs;
            RestoreSoulSeekState(sharedPrefs, this);
            RestoreListeningState();
            UPnpManager.RestoreUpnpState();

            SoulSeekState.OffsetFromUtcCached = Helpers.GetDateTimeNowSafe().Subtract(DateTime.UtcNow);

            SoulSeekState.SystemLanguage = LocaleToString(Resources.Configuration.Locale);
            SetLanguage(SoulSeekState.Language, false);


            //LogDebug("Default Night Mode: " + AppCompatDelegate.DefaultNightMode); //-100 = night mode unspecified, default on my Pixel 2. also on api22 emulator it is -100.
            //though setting it to -1 does not seem to recreate the activity or have any negative side effects..
            //this does not restart Android.App.Application. so putting it here is a much better place... in MainActivity.OnCreate it would restart the activity every time.
            if (AppCompatDelegate.DefaultNightMode != SoulSeekState.DayNightMode)
            {
                AppCompatDelegate.DefaultNightMode = SoulSeekState.DayNightMode;
            }

            //SoulSeekState.SharedPreferences = sharedPrefs;

            if (SeekerKeepAliveService.CpuKeepAlive_FullService == null)
            {
                SeekerKeepAliveService.CpuKeepAlive_FullService = ((PowerManager)this.GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Partial, "Seeker Keep Alive Service Cpu");
            }

            if (SeekerKeepAliveService.WifiKeepAlive_FullService == null)
            {
                SeekerKeepAliveService.WifiKeepAlive_FullService = ((Android.Net.Wifi.WifiManager)this.GetSystemService(Context.WifiService)).CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Seeker Keep Alive Service Wifi");
            }

            SeekerApplication.SetNetworkState(this);

            if (SoulSeekState.SoulseekClient == null)
            {
                //need search response and enqueue download action...
                //SoulSeekState.SoulseekClient = new SoulseekClient(new SoulseekClientOptions(messageTimeout: 30000, enableListener: false, autoAcknowledgePrivateMessages: false, acceptPrivateRoomInvitations:SoulSeekState.AllowPrivateRoomInvitations)); //Enable Listener is False.  Default is True.
                SoulSeekState.SoulseekClient = new SoulseekClient(new SoulseekClientOptions(minimumDiagnosticLevel: LOG_DIAGNOSTICS ? Soulseek.Diagnostics.DiagnosticLevel.Debug : Soulseek.Diagnostics.DiagnosticLevel.Info, messageTimeout: 30000, enableListener: SoulSeekState.ListenerEnabled, autoAcknowledgePrivateMessages: false, acceptPrivateRoomInvitations: SoulSeekState.AllowPrivateRoomInvitations, listenPort: SoulSeekState.ListenerPort, userInfoResponseResolver: UserInfoResponseHandler));
                SetDiagnosticState(LOG_DIAGNOSTICS);
                SoulSeekState.SoulseekClient.UserDataReceived += SoulseekClient_UserDataReceived;
                SoulSeekState.SoulseekClient.UserStatusChanged += SoulseekClient_UserStatusChanged_Deduplicator;
                SeekerApplication.UserStatusChangedDeDuplicated += SoulseekClient_UserStatusChanged;
                //SoulSeekState.SoulseekClient.TransferProgressUpdated += Upload_TransferProgressUpdated;
                SoulSeekState.SoulseekClient.TransferStateChanged += Upload_TransferStateChanged;

                SoulSeekState.SoulseekClient.TransferProgressUpdated += SoulseekClient_TransferProgressUpdated;
                SoulSeekState.SoulseekClient.TransferStateChanged += SoulseekClient_TransferStateChanged;

                SoulSeekState.SoulseekClient.Connected += SoulseekClient_Connected;
                SoulSeekState.SoulseekClient.StateChanged += SoulseekClient_StateChanged;
                SoulSeekState.SoulseekClient.LoggedIn += SoulseekClient_LoggedIn;
                SoulSeekState.SoulseekClient.Disconnected += SoulseekClient_Disconnected;
                SoulSeekState.SoulseekClient.ServerInfoReceived += SoulseekClient_ServerInfoReceived;
                SoulSeekState.BrowseResponseReceived += BrowseFragment.SoulSeekState_BrowseResponseReceived;

                SoulSeekState.SoulseekClient.PrivilegedUserListReceived += SoulseekClient_PrivilegedUserListReceived;

                MessageController.Initialize();
                ChatroomController.Initialize();


                SoulseekClient.OnTransferSizeMismatchFunc = OnTransferSizeMismatchFunc;
                SoulseekClient.ErrorLogHandler += MainActivity.SoulseekClient_ErrorLogHandler;

                SoulseekClient.DebugLogHandler += MainActivity.DebugLogHandler;

                SoulseekClient.DownloadAddedRemovedInternal += SoulseekClient_DownloadAddedRemovedInternal;
                SoulseekClient.UploadAddedRemovedInternal += SoulseekClient_UploadAddedRemovedInternal;
            }

            UPnpManager.Context = this;
            UPnpManager.Instance.SearchAndSetMappingIfRequired();
            CommonHelpers.STRINGS_KBS = this.Resources.GetString(Resource.String.kilobytes_per_second);
            CommonHelpers.STRINGS_KHZ = this.Resources.GetString(Resource.String.kilohertz);

            SlskHelp.CommonHelpers.UserListChecker = new UserListChecker();

            //shouldnt we also connect??? TODO TODO


        }

        public static Java.Util.Locale LocaleFromString(string localeString)
        {
            Java.Util.Locale locale = null;
            if (localeString.Contains("-r"))
            {
                var parts = localeString.Replace("-r", "-").Split('-');
                locale = new Java.Util.Locale(parts[0], parts[1]);
            }
            else
            {
                locale = new Java.Util.Locale(localeString);
            }
            return locale;
        }

        public void SetLanguage(string language, bool changed)
        {
            string localeString = language;
            var res = this.Resources;
            var config = res.Configuration;
            var displayMetrics = res.DisplayMetrics;

            var currentLocale = config.Locale;

            if(LocaleToString(currentLocale) == language)
            {
                return;
            }

            if(language == SoulSeekState.FieldLangAuto && SoulSeekState.SystemLanguage == LocaleToString(currentLocale))
            {
                return;
            }


            Java.Util.Locale locale = language != SoulSeekState.FieldLangAuto ? LocaleFromString(localeString) : LocaleFromString(SoulSeekState.SystemLanguage);

            Java.Util.Locale.Default = locale;
            config.SetLocale(locale);

            this.BaseContext.Resources.UpdateConfiguration(config, displayMetrics);

            if(changed)
            {
                RecreateActivies();
            }
        }

        public static string LocaleToString(Java.Util.Locale locale)
        {
            //"en" ""
            //"pt" "br"
            if(string.IsNullOrEmpty(locale.Variant))
            {
                return locale.Language;
            }
            else
            {
                return locale.Language + "-r" + locale.Variant.ToUpper();
            }
        }

        public static bool AreLocalesSame(Java.Util.Locale locale1, Java.Util.Locale locale2)
        {
            return LocaleToString(locale1) == LocaleToString(locale2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns>true if changed</returns>
        public static bool SetNetworkState(Context context)
        {
            try
            {
                ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);

                if(cm == null) //null if class is not a supported system service.
                {
                    return false;
                }

                if (cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected)
                {
                    bool oldState = SoulSeekState.CurrentConnectionIsUnmetered;
                    SoulSeekState.CurrentConnectionIsUnmetered = IsUnmetered(context, cm);
                    MainActivity.LogDebug("SetNetworkState is metered " + !SoulSeekState.CurrentConnectionIsUnmetered);
                    return oldState != SoulSeekState.CurrentConnectionIsUnmetered;
                }
                return false;
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("SetNetworkState" + e.Message + e.StackTrace);
                return false;
            }
        }

        private static bool IsUnmetered(Context context, ConnectivityManager cm)
        {

            if (!AndroidX.Core.Net.ConnectivityManagerCompat.IsActiveNetworkMetered(cm)) //api 16
            {
                return true;
            }
            else
            {
                return false;
            }

            //the below can fail if on VPN
            //var capabilities = cm.GetNetworkCapabilities(cm.ActiveNetwork);
            //cm.GetNetworkCapabilities(cm.ActiveNetwork).HasCapability(NetCapability.NotMetered);
            //AndroidX.Core.Net.ConnectivityManagerCompat.IsActiveNetworkMetered(cm);
            //bool isUnmetered = (capabilities != null && capabilities.HasCapability(NetCapability.NotMetered)) ||

        }

        public static void SetDiagnosticState(bool log_diagnostics)
        {
            if (log_diagnostics)
            {
                SoulSeekState.SoulseekClient.DiagnosticGenerated += SoulseekClient_DiagnosticGenerated;
                AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
            }
            else
            {
                SoulSeekState.SoulseekClient.DiagnosticGenerated -= SoulseekClient_DiagnosticGenerated;
                AndroidEnvironment.UnhandledExceptionRaiser -= AndroidEnvironment_UnhandledExceptionRaiser;
            }
        }

        private static void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            //by default e.Handled == false. and this does go on to crash the process (which is good imo, I only want this for logging purposes).
            MainActivity.LogDebug(e.Exception.Message);
            MainActivity.LogDebug(e.Exception.StackTrace);
        }

        private static string CreateMessage(Soulseek.Diagnostics.DiagnosticEventArgs e)
        {
            string timestamp = e.Timestamp.ToString("[MM_dd-hh:mm:ss] ");
            string body = null;
            if (e.IncludesException)
            {
                body = e.Message + System.Environment.NewLine + e.Exception.Message + System.Environment.NewLine + e.Exception.StackTrace;
            }
            else
            {
                body = e.Message;
            }
            return timestamp + body;
        }

        public static void AppendMessageToDiagFile(string msg)
        {
            //add the timestamp..
            AppendLineToDiagFile(CreateMessage(msg));
        }

        private static string CreateMessage(string line)
        {
            string timestamp = DateTime.UtcNow.ToString("[MM_dd-hh:mm:ss] ");
            return timestamp + line;
        }


        private static bool diagnosticFilesystemErrorShown = false; //so that we only show it once.
        private static void AppendLineToDiagFile(string line)
        {
            try
            {
            if (SoulSeekState.DiagnosticTextFile == null)
            {
                if(SoulSeekState.RootDocumentFile != null) //i.e. if api > 21 and they set it.
                {
                    SoulSeekState.DiagnosticTextFile = SoulSeekState.RootDocumentFile.FindFile("seeker_diagnostics.txt");
                    if (SoulSeekState.DiagnosticTextFile == null)
                    {
                        SoulSeekState.DiagnosticTextFile = SoulSeekState.RootDocumentFile.CreateFile("text/plain", "seeker_diagnostics");
                        if (SoulSeekState.DiagnosticTextFile == null)
                        {
                            return;
                        }
                    }
                }
                else if(SoulSeekState.UseLegacyStorage() || !SoulSeekState.SaveDataDirectoryUriIsFromTree) //if api < 30 and they did not set it. OR api <= 21 and they did set it.
                {
                    //when the directory is unset.
                    string fullPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    if (!string.IsNullOrEmpty(SoulSeekState.SaveDataDirectoryUri))
                    {
                        fullPath = Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri).Path;
                    }
                    
                    var containingDir = new Java.IO.File(fullPath);

                    var javaDiagFile = new Java.IO.File(fullPath + @"/" + "seeker_diagnostics.txt");
                    DocumentFile rootDir = DocumentFile.FromFile(new Java.IO.File(fullPath + @"/" + "seeker_diagnostics.txt"));
                        //DocumentFile.FromSingleUri(SoulSeekState.ActiveActivityRef, Android.Net.Uri.Parse(new Java.IO.File(fullPath).ToURI().ToString()));
                    //SoulSeekState.DiagnosticTextFile = rootDir;//.FindFile("seeker_diagnostics.txt");
                    if (!javaDiagFile.Exists())
                    {
                        if(containingDir.CanWrite())
                        {
                            bool success = javaDiagFile.CreateNewFile();
                            if(success)
                            {
                                SoulSeekState.DiagnosticTextFile = rootDir;
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        SoulSeekState.DiagnosticTextFile = rootDir;
                    }
                }
                else //if api >29 and they did not set it. nothing we can do.
                {
                    return;
                }
            }

            if (SoulSeekState.DiagnosticStreamWriter == null)
            {
                System.IO.Stream outputStream = SeekerApplication.ApplicationContext.ContentResolver.OpenOutputStream(SoulSeekState.DiagnosticTextFile.Uri, "wa");
                if (outputStream == null)
                {
                    return;
                }
                SoulSeekState.DiagnosticStreamWriter = new System.IO.StreamWriter(outputStream);
                if (SoulSeekState.DiagnosticStreamWriter == null)
                {
                    return;
                }
            }

            SoulSeekState.DiagnosticStreamWriter.WriteLine(line);
            SoulSeekState.DiagnosticStreamWriter.Flush();
            }
            catch(Exception ex)
            {
                if(!diagnosticFilesystemErrorShown)
                {
                    MainActivity.LogFirebase("failed to write to diagnostic file " + ex.Message + line + ex.StackTrace);
                    Toast.MakeText(SeekerApplication.ApplicationContext, "Failed to write to diagnostic file.", ToastLength.Long);
                    diagnosticFilesystemErrorShown = true;
                }
            }
        }

        public static void SoulseekClient_DiagnosticGenerated(object sender, Soulseek.Diagnostics.DiagnosticEventArgs e)
        {
            AppendLineToDiagFile(CreateMessage(e));
        }

        /// <summary>
        /// This is the one we should be hooking up to.
        /// This is due to the fact that the server sends us the same user update multiple times if they are of multiple interests.
        /// i.e. if we have Added Them, we are in Chatroom A, B, and C with them, then we get 4 status updates.
        /// </summary>
        public static EventHandler<UserStatusChangedEventArgs> UserStatusChangedDeDuplicated;

        private void SoulseekClient_Disconnected(object sender, SoulseekClientDisconnectedEventArgs e)
        {
            MainActivity.LogDebug("disconnected");
            lock (OurCurrentLoginTaskSyncObject)
            {
                OurCurrentLoginTask = null;
            }
        }

        public static void AcquireTransferLocksAndResetTimer()
        {
            if (MainActivity.CpuKeepAlive_Transfer != null && !MainActivity.CpuKeepAlive_Transfer.IsHeld)
            {
                MainActivity.CpuKeepAlive_Transfer.Acquire();
            }
            if (MainActivity.WifiKeepAlive_Transfer != null && !MainActivity.WifiKeepAlive_Transfer.IsHeld)
            {
                MainActivity.WifiKeepAlive_Transfer.Acquire();
            }

            if (MainActivity.KeepAliveInactivityKillTimer != null)
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop(); //can be null
                MainActivity.KeepAliveInactivityKillTimer.Start(); //reset the timer..
            }
            else
            {
                MainActivity.KeepAliveInactivityKillTimer = new System.Timers.Timer(60 * 1000 * 10); //kill after 10 mins of no activity..
                                                                                                     //remember that this is a fallback. for when foreground service is still running but nothing is happening otherwise.
                MainActivity.KeepAliveInactivityKillTimer.Elapsed += MainActivity.KeepAliveInactivityKillTimerEllapsed;
                MainActivity.KeepAliveInactivityKillTimer.AutoReset = false;
            }
        }

        public static void ReleaseTransferLocksIfServicesComplete()
        {
            //if all transfers are done..
            if (!SoulSeekState.UploadKeepAliveServiceRunning && !SoulSeekState.DownloadKeepAliveServiceRunning)
            {
                if (MainActivity.CpuKeepAlive_Transfer != null)
                {
                    MainActivity.CpuKeepAlive_Transfer.Release();
                }
                if (MainActivity.WifiKeepAlive_Transfer != null)
                {
                    MainActivity.WifiKeepAlive_Transfer.Release();
                }
                if (MainActivity.KeepAliveInactivityKillTimer != null)
                {
                    MainActivity.KeepAliveInactivityKillTimer.Stop();
                }
            }
        }

        public const string ACTION_SHUTDOWN = "SeekerApplication_AppShutDown";

        public static bool IsShuttingDown(Intent intent)
        {
            if (intent?.Action == null)
            {
                return false;
            }
            if (intent.Action == ACTION_SHUTDOWN)
            {
                return true;
            }
            return false;
        }

        public static bool TransfersDownloadsCompleteStale = false; //whether a dl completes since we have last saved transfers to disk.
        public static DateTime TransfersLastSavedTime = DateTime.MinValue; //whether a dl completes since we have last saved transfers to disk.


        public static volatile int UPLOAD_COUNT = -1; // a hack see below

        private void SoulseekClient_UploadAddedRemovedInternal(object sender, SoulseekClient.TransferAddedRemovedInternalEventArgs e)
        {
            bool abortAll = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - SoulSeekState.AbortAllWasPressedDebouncer) < 750;
            if (e.Count == 0 || abortAll)
            {
                UPLOAD_COUNT = -1;
                Intent uploadServiceIntent = new Intent(this, typeof(UploadForegroundService));
                MainActivity.LogDebug("Stop Service");
                this.StopService(uploadServiceIntent);
                SoulSeekState.UploadKeepAliveServiceRunning = false;
            }
            else if (!SoulSeekState.UploadKeepAliveServiceRunning)
            {
                UPLOAD_COUNT = e.Count;
                Intent uploadServiceIntent = new Intent(this, typeof(UploadForegroundService));
                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    bool isForeground = false;
                    try
                    {
                        if (SoulSeekState.ActiveActivityRef?.Lifecycle.CurrentState != null)
                        {
                            isForeground = SoulSeekState.ActiveActivityRef.Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Resumed);
                        }
                    }
                    catch
                    {
                        MainActivity.LogFirebase("Exception thrown while checking lifecycle");
                    }

                    //LogDebug("IsForeground: " + isForeground + " current state: " + this.Lifecycle.CurrentState.ToString()); //REMOVE THIS!!!
                    if (isForeground)
                    {
                        this.StartService(uploadServiceIntent); //this will throw if the app is in background.
                    }
                    else
                    {
                        //only do this if we absolutely must
                        //this will throw in api 31 if the app is in background. so now it is out of the question.  no way to start foreground service if in background.
                        //this.StartForegroundService(uploadServiceIntent);
                    }
                }
                else
                {
                    //even when targetting and compiling for api 31, old devices can still do this just fine.
                    this.StartService(uploadServiceIntent); //this will throw if the app is in background.
                }
                SoulSeekState.UploadKeepAliveServiceRunning = true;
            }
            else if (SoulSeekState.UploadKeepAliveServiceRunning && e.Count != 0)
            {
                UPLOAD_COUNT = e.Count;
                //for two downloads, this notification will go up before the service is started...

                //requires run on ui thread? NOPE
                string msg = string.Empty;
                if (e.Count == 1)
                {
                    msg = string.Format(UploadForegroundService.SingularUploadRemaining, e.Count);
                }
                else
                {
                    msg = string.Format(UploadForegroundService.PluralUploadsRemaining, e.Count);
                }
                var notif = UploadForegroundService.CreateNotification(this, msg);
                NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Notify(UploadForegroundService.NOTIF_ID, notif);
            }
            //});


        }


        public static volatile int DL_COUNT = -1; // a hack see below

        //it works in the case of successfully finished, cancellation token used, etc.
        private void SoulseekClient_DownloadAddedRemovedInternal(object sender, SoulseekClient.TransferAddedRemovedInternalEventArgs e)
        {
            //even with them all going onto same thread here you will still have (ex) a 16 count coming in after a 0 count sometimes.
            //SoulSeekState.MainActivityRef.RunOnUiThread(()=>
            //{
            MainActivity.LogDebug("SoulseekClient_DownloadAddedRemovedInternal with count:" + e.Count);
            MainActivity.LogDebug("the thread is: " + System.Threading.Thread.CurrentThread.ManagedThreadId);

            bool cancelAndClear = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - SoulSeekState.CancelAndClearAllWasPressedDebouncer) < 750;
            MainActivity.LogDebug("SoulseekClient_DownloadAddedRemovedInternal cancel and clear:" + cancelAndClear);
            if (e.Count == 0 || cancelAndClear)
            {
                DL_COUNT = -1;
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                MainActivity.LogDebug("Stop Service");
                this.StopService(downloadServiceIntent);
                SoulSeekState.DownloadKeepAliveServiceRunning = false;
            }
            else if (!SoulSeekState.DownloadKeepAliveServiceRunning)
            {
                DL_COUNT = e.Count;
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    bool isForeground = false;
                    try
                    {
                        if (SoulSeekState.ActiveActivityRef?.Lifecycle.CurrentState != null)
                        {
                            isForeground = SoulSeekState.ActiveActivityRef.Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Resumed);
                        }
                    }
                    catch
                    {
                        MainActivity.LogFirebase("Exception thrown while checking lifecycle");
                    }

                    //LogDebug("IsForeground: " + isForeground + " current state: " + this.Lifecycle.CurrentState.ToString()); //REMOVE THIS!!!
                    if (isForeground)
                    {
                        this.StartService(downloadServiceIntent); //this will throw if the app is in background.
                    }
                    else
                    {
                        //only do this if we absolutely must
                        //this will throw in api 31 if the app is in background. so now it is out of the question.  no way to start foreground service if in background.
                        //this.StartForegroundService(downloadServiceIntent);
                    }
                }
                else
                {
                    //even when targetting and compiling for api 31, old devices can still do this just fine.
                    this.StartService(downloadServiceIntent); //this will throw if the app is in background.
                }
                SoulSeekState.DownloadKeepAliveServiceRunning = true;
            }
            else if (SoulSeekState.DownloadKeepAliveServiceRunning && e.Count != 0)
            {
                DL_COUNT = e.Count;
                //for two downloads, this notification will go up before the service is started...

                //requires run on ui thread? NOPE
                string msg = string.Empty;
                if (e.Count == 1)
                {
                    msg = string.Format(DownloadForegroundService.SingularDownloadRemaining, e.Count);
                }
                else
                {
                    msg = string.Format(DownloadForegroundService.PluralDownloadsRemaining, e.Count);
                }
                var notif = DownloadForegroundService.CreateNotification(this, msg);
                NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Notify(DownloadForegroundService.NOTIF_ID, notif);
            }
            //});
        }

        public static class SpeedLimitHelper
        {

            public static void RemoveDownloadUser(string username)
            {
                DownloadUserDelays.TryRemove(username, out _);
                DownloadLastAvgSpeed.TryRemove(username, out _);
            }

            public static void RemoveUploadUser(string username)
            {
                UploadUserDelays.TryRemove(username, out _);
                UploadLastAvgSpeed.TryRemove(username, out _);
            }

            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> DownloadUserDelays = new System.Collections.Concurrent.ConcurrentDictionary<string, double>(); //we need the double precision bc sometimes 1.1 cast to int will be the same number i.e. (int)(4*1.1)==4
            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> DownloadLastAvgSpeed = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();

            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> UploadUserDelays = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();
            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> UploadLastAvgSpeed = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();
            public static Task OurDownloadGoverner(double currentSpeed, string username, CancellationToken cts)
            {
                try
                {
                    if (SoulSeekState.SpeedLimitDownloadOn)
                    {

                        if (DownloadUserDelays.TryGetValue(username, out double msDelay))
                        {
                            bool exists = DownloadLastAvgSpeed.TryGetValue(username, out double lastAvgSpeed); //this is here in the case of a race condition (due to RemoveUser)
                            if (exists && currentSpeed == lastAvgSpeed)
                            {
#if DEBUG
                                //System.Console.WriteLine("dont update");
#endif
                                //do not adjust as we have not yet recalculated the average speed
                                return Task.Delay((int)msDelay, cts);
                            }

                            DownloadLastAvgSpeed[username] = currentSpeed;

                            double avgSpeed = currentSpeed;
                            if (!SoulSeekState.SpeedLimitDownloadIsPerTransfer && DownloadLastAvgSpeed.Count > 1)
                            {

                                //its threadsafe when using linq on concurrent dict itself.
                                avgSpeed = DownloadLastAvgSpeed.Sum((p) => p.Value);//Values.ToArray().Sum();
#if DEBUG
                                //System.Console.WriteLine("multiple total speed " + avgSpeed);
#endif
                            }

                            if (avgSpeed > SoulSeekState.SpeedLimitDownloadBytesSec)
                            {
#if DEBUG
                                //System.Console.WriteLine("speed too high " + currentSpeed + "   " + msDelay);
#endif
                                DownloadUserDelays[username] = msDelay = msDelay * 1.04;

                            }
                            else
                            {
#if DEBUG
                                //System.Console.WriteLine("speed too low " + currentSpeed + "   " + msDelay);
#endif
                                DownloadUserDelays[username] = msDelay = msDelay * 0.96;
                            }

                            return Task.Delay((int)msDelay, cts);
                        }
                        else
                        {
#if DEBUG
                            //System.Console.WriteLine("first time guess");
#endif
                            //first time we need to guess a decent value
                            //wait time if the loop took 0s with buffer size of 16kB i.e. speed = 16kB / (delaytime). (delaytime in ms) = 1000 * 16,384 / (speed in bytes per second).
                            double msDelaySeed = 1000 * 16384.0 / SoulSeekState.SpeedLimitDownloadBytesSec;
                            DownloadUserDelays[username] = msDelaySeed;
                            DownloadLastAvgSpeed[username] = currentSpeed;
                            return Task.Delay((int)msDelaySeed, cts);
                        }

                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                }
                catch (Exception ex)
                {
                    MainActivity.LogFirebase("DL SPEED LIMIT EXCEPTION: " + ex.Message + ex.StackTrace);
                    return Task.CompletedTask;
                }
            }

            //this is duplicated for speed.
            public static Task OurUploadGoverner(double currentSpeed, string username, CancellationToken cts)
            {
                try
                {
                    if (SoulSeekState.SpeedLimitUploadOn)
                    {

                        if (UploadUserDelays.TryGetValue(username, out double msDelay))
                        {
                            bool exists = UploadLastAvgSpeed.TryGetValue(username, out double lastAvgSpeed); //this is here in the case of a race condition (due to RemoveUser)
                            if (exists && currentSpeed == lastAvgSpeed)
                            {
#if DEBUG
                                //System.Console.WriteLine("UL dont update");
#endif
                                //do not adjust as we have not yet recalculated the average speed
                                return Task.Delay((int)msDelay, cts);
                            }

                            UploadLastAvgSpeed[username] = currentSpeed;

                            double avgSpeed = currentSpeed;
                            if (!SoulSeekState.SpeedLimitUploadIsPerTransfer && UploadLastAvgSpeed.Count > 1)
                            {

                                //its threadsafe when using linq on concurrent dict itself.
                                avgSpeed = UploadLastAvgSpeed.Sum((p) => p.Value);//Values.ToArray().Sum();
#if DEBUG
                                //System.Console.WriteLine("UL multiple total speed " + avgSpeed);
#endif
                            }

                            if (avgSpeed > SoulSeekState.SpeedLimitUploadBytesSec)
                            {
#if DEBUG
                                //System.Console.WriteLine("UL speed too high " + currentSpeed + "   " + msDelay);
#endif
                                UploadUserDelays[username] = msDelay = msDelay * 1.04;

                            }
                            else
                            {
#if DEBUG
                                //System.Console.WriteLine("UL speed too low " + currentSpeed + "   " + msDelay);
#endif
                                UploadUserDelays[username] = msDelay = msDelay * 0.96;
                            }

                            return Task.Delay((int)msDelay, cts);
                        }
                        else
                        {
#if DEBUG
                            //System.Console.WriteLine("UL first time guess");
#endif
                            //first time we need to guess a decent value
                            //wait time if the loop took 0s with buffer size of 16kB i.e. speed = 16kB / (delaytime). (delaytime in ms) = 1000 * 16,384 / (speed in bytes per second).
                            double msDelaySeed = 1000 * 16384.0 / SoulSeekState.SpeedLimitUploadBytesSec;
                            UploadUserDelays[username] = msDelaySeed;
                            UploadLastAvgSpeed[username] = currentSpeed;
                            return Task.Delay((int)msDelaySeed, cts);
                        }

                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                }
                catch (Exception ex)
                {
                    MainActivity.LogFirebase("UL SPEED LIMIT EXCEPTION: " + ex.Message + ex.StackTrace);
                    return Task.CompletedTask;
                }
            }

        }


        public class ProgressUpdatedUI : EventArgs
        {
            public ProgressUpdatedUI(TransferItem _ti, bool _wasFailed, bool _fullRefresh, double _percentComplete, double _avgspeedBytes)
            {
                ti = _ti;
                wasFailed = _wasFailed;
                fullRefresh = _fullRefresh;
                percentComplete = _percentComplete;
                avgspeedBytes = _avgspeedBytes;
            }
            public TransferItem ti;
            public bool wasFailed;
            public bool fullRefresh;
            public double percentComplete;
            public double avgspeedBytes;
        }

        public static EventHandler<TransferItem> StateChangedForItem;
        public static EventHandler<int> StateChangedAtIndex;
        public static EventHandler<ProgressUpdatedUI> ProgressUpdated;

        private void SoulseekClient_TransferStateChanged(object sender, TransferStateChangedEventArgs e)
        {
            try
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop();
                MainActivity.KeepAliveInactivityKillTimer.Start();
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("timer issue2: " + err.Message + err.StackTrace); //remember at worst the locks will get released early which is fine.
            }

            bool isUpload = e.Transfer.Direction == TransferDirection.Upload;

            if(!isUpload && e.Transfer.State.HasFlag(TransferStates.UserOffline))
            {
                //user offline.
                TransfersFragment.AddToUserOffline(e.Transfer.Username);
            }

            TransferItem relevantItem = TransfersFragment.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(e.Transfer?.Filename, e.Transfer?.Username, isUpload, out _);
            if (relevantItem == null)
            {
                MainActivity.LogInfoFirebase("relevantItem==null. state: " + e.Transfer.State.ToString());
            }
            //TransferItem relevantItem = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(e.Transfer?.Filename, e.Transfer?.Username, out _);  //upload / download branch here
            if (relevantItem != null)
            {
                //if the incoming transfer is not canclled, i.e. requested, then we replace the state (the user retried).
                if(e.Transfer.State.HasFlag(TransferStates.Cancelled) && relevantItem.State.HasFlag(TransferStates.FallenFromQueue))
                {
                    MainActivity.LogDebug("fallen from queue");
                    //the state is good as is.  do not add cancelled to it, since we used cancelled to mean "user cancelled" i.e. paused.
                    relevantItem.Failed = true;
                    relevantItem.Progress = 100;
                }
                else
                {
                    relevantItem.State = e.Transfer.State;
                }
                relevantItem.IncompleteParentUri = e.IncompleteParentUri;
                if (!relevantItem.State.HasFlag(TransferStates.Requested))
                {
                    relevantItem.InProcessing = true;
                }
                if (relevantItem.State.HasFlag(TransferStates.Succeeded))
                {
                    relevantItem.IncompleteParentUri = null; //not needed anymore.
                }
                //if(relevantItem.Size==-1)
                //{
                //    if(e.Transfer.Size!=0)
                //    {
                //        relevantItem.Size = e.Transfer.Size;
                //    }
                //}
            }
            if (e.Transfer.State.HasFlag(TransferStates.Errored) || e.Transfer.State.HasFlag(TransferStates.TimedOut) || e.Transfer.State.HasFlag(TransferStates.Rejected))
            {
                SeekerApplication.SpeedLimitHelper.RemoveDownloadUser(e.Transfer.Username);
                if (relevantItem == null)
                {
                    return;
                }
                relevantItem.Failed = true;
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Queued))
            {
                if (relevantItem == null)
                {
                    return;
                }
                if (!relevantItem.IsUpload())
                {
                    if (relevantItem.QueueLength != 0) //this means that it probably came from a search response where we know the users queuelength  ***BUT THAT IS NEVER THE ACTUAL QUEUE LENGTH*** its always much shorter...
                    {
                        //nothing to do, bc its already set..
                        MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, true, true, null, null);
                    }
                    else //this means that it came from a browse response where we may not know the users initial queue length... or if its unexpectedly queued.
                    {
                        //GET QUEUE LENGTH AND UPDATE...
                        MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, true, true, null, null);
                    }
                }
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Initializing))
            {
                if (relevantItem == null)
                {
                    return;
                }
                //clear queued flag...
                relevantItem.QueueLength = int.MaxValue;
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Completed))
            {
                SeekerApplication.SpeedLimitHelper.RemoveDownloadUser(e.Transfer.Username);
                if (relevantItem == null)
                {
                    return;
                }
                if (!e.Transfer.State.HasFlag(TransferStates.Cancelled))
                {
                    //clear queued flag...
                    SeekerApplication.TransfersDownloadsCompleteStale = true;
                    TransfersFragment.SaveTransferItems(SoulSeekState.SharedPreferences, false, 120);
                    relevantItem.Progress = 100;
                    StateChangedForItem?.Invoke(null, relevantItem);
                }
                else //if it does have state cancelled we still want to update UI! (assuming we arent also clearing it)
                {
                    if (!relevantItem.CancelAndClearFlag)
                    {
                        StateChangedForItem?.Invoke(null, relevantItem);
                    }
                }

                if(e.Transfer.State.HasFlag(TransferStates.Succeeded))
                {
                    if(SoulSeekState.NotifyOnFolderCompleted && !isUpload)
                    {
                        if(TransfersFragment.TransferItemManagerDL.IsFolderNowComplete(relevantItem, false))
                        {
                            ShowNotificationForCompletedFolder(relevantItem.FolderName, relevantItem.Username);
                        }
                    }
                }
            }
            else
            {
                if (relevantItem == null && (e.Transfer.State == TransferStates.Requested || e.Transfer.State == TransferStates.Aborted))
                {
                    return; //TODO sometimes this can happen too fast.  this is okay thouugh bc it will soon go to another state.
                }
                if (relevantItem == null && e.Transfer.State == TransferStates.InProgress)
                {
                    //THIS SHOULD NOT HAPPEN now that the race condition is resolved....
                    MainActivity.LogFirebase("relevantItem==null. state: " + e.Transfer.State.ToString());
                    return;
                }
                StateChangedForItem?.Invoke(null, relevantItem);
            }
        }

        public static bool OnTransferSizeMismatchFunc(System.IO.Stream fileStream, string fullFilename, string username, long startOffset, long oldSize, long newSize, string incompleteUriString, out System.IO.Stream newStream)
        {
            newStream = null;
            try
            {
                var relevantItem = TransfersFragment.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(fullFilename, username, false, out _);
                if(startOffset == 0)
                {
                    // all we need to do is update the size.
                    relevantItem.Size = newSize;
                    MainActivity.LogDebug("updated the size");
                }
                else
                {
                    // we need to truncate the incomplete file and set our progress back to 0.
                    relevantItem.Size = newSize;
                    //fileStream.SetLength(0); //this is not supported. we cannot do seek.
                    //fileStream.Flush();

                    fileStream.Close();
                    bool useDownloadDir = false;
                    if (SoulSeekState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
                    {
                        useDownloadDir = true;
                    }
                    
                    var incompleteUri = Android.Net.Uri.Parse(incompleteUriString);

                        // this is the only time we do legacy.
                    bool isLegacyCase = SoulSeekState.UseLegacyStorage() && (SoulSeekState.RootDocumentFile == null && useDownloadDir);
                    if(isLegacyCase)
                    {
                        newStream = new System.IO.FileStream(incompleteUri.Path, System.IO.FileMode.Truncate, System.IO.FileAccess.Write, System.IO.FileShare.None);
                    }
                    else
                    {
                        
                        newStream = SoulSeekState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri, "wt");
                    }

                    relevantItem.Progress = 0;
                    MainActivity.LogDebug("truncated the file and updated the size");
                }

            }
            catch(Exception e)
            {
                MainActivity.LogDebug("OnTransferSizeMismatchFunc: " + e.ToString());
                return false;
            }
            return true;
        }


        private void SoulseekClient_TransferProgressUpdated(object sender, TransferProgressUpdatedEventArgs e)
        {
            //Its possible to get a nullref here IF the system orientation changes..
            //throttle this maybe...

            //MainActivity.LogDebug("TRANSFER PROGRESS UPDATED"); //this typically happens once every 10 ms or even less and thats in debug mode.  in fact sometimes it happens 4 times in 1 ms.
            try
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop(); //lot of nullref here...
                MainActivity.KeepAliveInactivityKillTimer.Start();
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("timer issue2: " + err.Message + err.StackTrace); //remember at worst the locks will get released early which is fine.
            }
            TransferItem relevantItem = null;
            if (TransfersFragment.TransferItemManagerDL == null)
            {
                MainActivity.LogDebug("transferItems Null " + e.Transfer.Filename);
                return;
            }

            bool isUpload = e.Transfer.Direction == TransferDirection.Upload;
            relevantItem = TransfersFragment.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(e.Transfer.Filename, e.Transfer.Username, e.Transfer.Direction == TransferDirection.Upload, out _);
            //relevantItem = TransfersFragment.TransferItemManagerDL.GetTransferItem(e.Transfer.Filename);

            if (relevantItem == null)
            {
                //this happens on Clear and Cancel All.
                MainActivity.LogDebug("Relevant Item Null " + e.Transfer.Filename);
                MainActivity.LogDebug("transferItems.IsEmpty " + TransfersFragment.TransferItemManagerDL.IsEmpty());
                return;
            }
            else
            {
                bool fullRefresh = false;
                double percentComplete = e.Transfer.PercentComplete;
                relevantItem.Progress = (int)percentComplete;
                relevantItem.RemainingTime = e.Transfer.RemainingTime;
                relevantItem.AvgSpeed = e.Transfer.AverageSpeed;

                // int indexRemoved = -1;
                if (((SoulSeekState.AutoClearCompleteDownloads && !isUpload) || (SoulSeekState.AutoClearCompleteUploads && isUpload)) && System.Math.Abs(percentComplete - 100) < .001) //if 100% complete and autoclear //todo: autoclear on upload
                {

                    Action action = new Action(() =>
                    {
                        //int before = TransfersFragment.transferItems.Count;
                        TransfersFragment.UpdateBatchSelectedItemsIfApplicable(relevantItem);
                        TransfersFragment.TransferItemManagerWrapped.Remove(relevantItem);//TODO: shouldnt we do the corresponding Adapter.NotifyRemoveAt. //this one doesnt need cleaning up, its successful..
                        //int after = TransfersFragment.transferItems.Count;
                        //MainActivity.LogDebug("transferItems.Remove(relevantItem): before: " + before + "after: " + after);
                    });
                    if (SoulSeekState.ActiveActivityRef != null)
                    {
                        SoulSeekState.ActiveActivityRef?.RunOnUiThread(action);
                    }

                    fullRefresh = true;
                }
                else if (System.Math.Abs(percentComplete - 100) < .001)
                {
                    fullRefresh = true;
                }

                bool wasFailed = false;
                if (percentComplete != 0)
                {
                    wasFailed = false;
                    if (relevantItem.Failed)
                    {
                        wasFailed = true;
                        relevantItem.Failed = false;
                    }

                }

                ProgressUpdated?.Invoke(null, new ProgressUpdatedUI(relevantItem, wasFailed, fullRefresh, percentComplete, e.Transfer.AverageSpeed));

            }
        }

        public static string GetVersionString()
        {
            try
            {
                PackageInfo pInfo = SoulSeekState.ActiveActivityRef.PackageManager.GetPackageInfo(SoulSeekState.ActiveActivityRef.PackageName, 0);
                return pInfo.VersionName;
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("GetVersionString: " + e.Message);
                return string.Empty;
            }
        }




        public static Task<UserInfo> UserInfoResponseHandler(string uname, IPEndPoint ipEndPoint)
        {
            if (IsUserInIgnoreList(uname))
            {
                return Task.FromResult(new UserInfo(string.Empty, 0, 0, false));
            }
            string bio = SoulSeekState.UserInfoBio ?? string.Empty;
            byte[] picture = GetUserInfoPicture();
            int uploadSlots = 1;
            int queueLength = 0;
            bool hasFreeSlots = true;
            if (!SoulSeekState.SharingOn) //in my experience even if someone is sharing nothing they say 1 upload slot and yes free slots.. but idk maybe 0 and no makes more sense??
            {
                uploadSlots = 0;
                queueLength = 0;
                hasFreeSlots = false;
            }

            return Task.FromResult(new UserInfo(bio, picture, uploadSlots, queueLength, hasFreeSlots));
        }

        private static byte[] GetUserInfoPicture()
        {
            if (SoulSeekState.UserInfoPictureName == null || SoulSeekState.UserInfoPictureName == string.Empty)
            {
                return null;
            }
            Java.IO.File userInfoPicDir = new Java.IO.File(ApplicationContext.FilesDir, EditUserInfoActivity.USER_INFO_PIC_DIR);
            if (!userInfoPicDir.Exists())
            {
                MainActivity.LogFirebase("!userInfoPicDir.Exists()");
                return null;
            }

            Java.IO.File userInfoPic = new Java.IO.File(userInfoPicDir, SoulSeekState.UserInfoPictureName);
            if (!userInfoPic.Exists())
            {
                //I could imagine a race condition causing this...
                MainActivity.LogFirebase("!userInfoPic.Exists()");
                return null;
            }
            DocumentFile documentFile = DocumentFile.FromFile(userInfoPic);
            System.IO.Stream imageStream = ApplicationContext.ContentResolver.OpenInputStream(documentFile.Uri);
            byte[] picFile = new byte[imageStream.Length];
            imageStream.Read(picFile, 0, (int)imageStream.Length);
            return picFile;
        }

        private void SoulseekClient_PrivilegedUserListReceived(object sender, IReadOnlyCollection<string> e)
        {
            PrivilegesManager.Instance.SetPrivilegedList(e);
        }

        private void SoulseekClient_ServerInfoReceived(object sender, ServerInfo e)
        {
            if (e.WishlistInterval.HasValue)
            {
                WishlistController.SearchIntervalMilliseconds = e.WishlistInterval.Value;
                WishlistController.Initialize();
            }
            else
            {
                MainActivity.LogDebug("wishlist interval is null");
            }
        }

        //I only care about the Connected, LoggedIn to Disconnecting case.  
        //the next case is Disconnecting to Disconnected.

        //then for failed retries you get
        //disconnected to connecting
        //connecting to disconnecting
        //disconnecting to disconnected.
        //so be wary of the disconnected event...

        private void SoulseekClient_StateChanged(object sender, SoulseekClientStateChangedEventArgs e)
        {
            MainActivity.LogDebug("Prev: " + e.PreviousState.ToString() + " Next: " + e.State.ToString());
            if (e.PreviousState.HasFlag(SoulseekClientStates.LoggedIn) && e.State.HasFlag(SoulseekClientStates.Disconnecting))
            {
                MainActivity.LogDebug("!! changing from connected to disconnecting");


                if (e.Exception is KickedFromServerException)
                {
                    MainActivity.LogDebug("Kicked Kicked Kicked");
                    if (SoulSeekState.ActiveActivityRef != null)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.kicked_due_to_other_client), ToastLength.Long).Show(); });
                    }
                    return; //DO NOT RETRY!!! or will do an infinite loop!
                }
                if (e.Exception is System.ObjectDisposedException)
                {
                    return; //DO NOT RETRY!!! we are shutting down
                }



                //this is a "true" connected to disconnected
                ChatroomController.ClearAndCacheJoined();
                MainActivity.LogDebug("disconnected " + DateTime.UtcNow.ToString());
                if (SoulSeekState.logoutClicked)
                {
                    SoulSeekState.logoutClicked = false;
                }
                else if (AUTO_CONNECT_ON && SoulSeekState.currentlyLoggedIn)
                {
                    Thread reconnectRetrier = new Thread(ReconnectSteppedBackOffThreadTask);
                    reconnectRetrier.Start();
                }
            }
            else if(e.PreviousState.HasFlag(SoulseekClientStates.Disconnected))
            {
                MainActivity.LogDebug("!! changing from disconnected to trying to connect");
            }
            else if(e.State.HasFlag(SoulseekClientStates.LoggedIn) && e.State.HasFlag(SoulseekClientStates.Connected))
            {
                MainActivity.LogDebug("!! changing trying to connect to successfully connected");
            }
        }

        private void SoulseekClient_LoggedIn(object sender, EventArgs e)
        {
            lock (OurCurrentLoginTaskSyncObject)
            {
                OurCurrentLoginTask = null;
            }
            ChatroomController.JoinAutoJoinRoomsAndPreviousJoined();
            //ChatroomController.ConnectionLapse.Add(new Tuple<bool, DateTime>(true, DateTime.UtcNow)); //just testing obv remove later...
            MainActivity.LogDebug("logged in " + DateTime.UtcNow.ToString());
            MainActivity.LogDebug("Listening State: " + SoulSeekState.SoulseekClient.GetListeningState());
            if (SoulSeekState.ListenerEnabled && !SoulSeekState.SoulseekClient.GetListeningState())
            {
                if (SoulSeekState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("SoulSeekState.ActiveActivityRef null SoulseekClient_LoggedIn");
                }
                else
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.port_already_in_use), ToastLength.Short).Show(); //todo is this supposed to be here...
                    });
                }
            }
        }

        private void SoulseekClient_Connected(object sender, EventArgs e)
        {
            //ChatroomController.SetConnectionLapsedMessage(true);
            MainActivity.LogDebug("connected " + DateTime.UtcNow.ToString());

        }

        //private void SoulseekClient_Disconnected(object sender, SoulseekClientDisconnectedEventArgs e)
        //{
        //    ChatroomController.ConnectionLapse.Add(new Tuple<bool,DateTime>(false,DateTime.UtcNow));
        //    MainActivity.LogDebug("disconnected " + DateTime.UtcNow.ToString());
        //    bool AUTO_CONNECT = true;
        //    if(AUTO_CONNECT && SoulSeekState.currentlyLoggedIn)
        //    {
        //        Thread reconnectRetrier = new Thread(ReconnectExponentialBackOffThreadTask);
        //        reconnectRetrier.Start();
        //    }
        //}

        public static void ShowToast(string msg, ToastLength toastLength)
        {
            if (SoulSeekState.ActiveActivityRef == null)
            {
                MainActivity.LogDebug("cant show toast, active activity ref is null");
            }
            else
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, toastLength).Show(); });
            }
        }

        public static string GetString(int resId)
        {
            return SeekerApplication.ApplicationContext.GetString(resId);
        }

        public static bool ShouldWeTryToConnect()
        {
            if(!SoulSeekState.currentlyLoggedIn)
            {
                //we logged out on purpose
                return false;
            }

            if (SoulSeekState.SoulseekClient == null)
            {
                //too early
                return false;
            }

            if(SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) && SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                //already connected
                return false;
            }
            return true;
        }

        /// <summary>
        /// This is the number of seconds after the last try.
        /// </summary>
        private static readonly int[] retrySeconds = new int[MAX_TRIES] { 1, 2, 4, 10, 20 };
        private const int MAX_TRIES = 5;
        // if the reconnect stepped backoff thread is in progress but a change ocurred that makes us
        // want to trigger it immediately, then we can just set this event.
        public static AutoResetEvent ReconnectAutoResetEvent = new AutoResetEvent(false);
        public static volatile bool ReconnectSteppedBackOffThreadIsRunning = false;
        public static void ReconnectSteppedBackOffThreadTask()
        {
            try
            {
                ReconnectSteppedBackOffThreadIsRunning = true;
                for (int i = 0; i < MAX_TRIES; i++)
                {
                    if (!ShouldWeTryToConnect())
                    {
                        return; //our work here is done
                    }

                    bool isDueToAutoReset = ReconnectAutoResetEvent.WaitOne(retrySeconds[i] * 1000);
                    if(isDueToAutoReset)
                    {
                        MainActivity.LogDebug("is woken due to auto reset");
                    }
                    //System.Threading.Thread.Sleep(retrySeconds[i] * 1000); //todo AutoResetEvent or WaitOne(ms) etc.

                    try
                    {
                        //a general note for connecting:
                        //whenever you reconnect if you want the server to tell you the status of users on your user list
                        //you have to re-AddUser them.  This is what SoulSeekQt does (wireshark message code 5 for each user in list).
                        //and what Nicotine does (userlist.server_login()).
                        //and reconnecting means every single time, including toggling from wifi to data / vice versa.
                        Task t = ConnectAndPerformPostConnectTasks(SoulSeekState.Username, SoulSeekState.Password);
                        t.Wait();
                        if (t.IsCompletedSuccessfully)
                        {
                            MainActivity.LogDebug("RETRY " + i + "SUCCEEDED");
                            return; //our work here is done
                        }
                    }
                    catch (Exception)
                    {

                    }
                    //if we got here we failed.. so try again shortly...
                    MainActivity.LogDebug("RETRY " + i + "FAILED");
                }
            }
            finally
            {
                ReconnectSteppedBackOffThreadIsRunning = false;
            }
        }

        public static void AddToIgnoreListFeedback(Context c, string username)
        {
            if (SeekerApplication.AddToIgnoreList(username))
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.added_to_ignore), username), ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.already_added_to_ignore), username), ToastLength.Short).Show();
            }
        }
        public static Task OurCurrentLoginTask = null;
        public static object OurCurrentLoginTaskSyncObject = new object();
        public static Task ConnectAndPerformPostConnectTasks(string username, string password)
        {
            Task t = SoulSeekState.SoulseekClient.ConnectAsync(username, password);
            OurCurrentLoginTask = t;
            t.ContinueWith(PerformPostConnectTasks);
            return t;
        }

        public static void PerformPostConnectTasks(Task t)
        {
            if (t.IsCompletedSuccessfully)
            {
                try
                {
                    lock (SoulSeekState.UserList)
                    {
                        foreach (UserListItem item in SoulSeekState.UserList)
                        {
                            MainActivity.LogDebug("adding user: " + item.Username);
                            SoulSeekState.SoulseekClient.AddUserAsync(item.Username).ContinueWith(UpdateUserInfo);
                        }
                    }

                    lock(TransfersFragment.UsersWhereDownloadFailedDueToOffline)
                    {
                        foreach (string userDownloadOffline in TransfersFragment.UsersWhereDownloadFailedDueToOffline.Keys)
                        {
                            MainActivity.LogDebug("adding user (due to a download we wanted from them when they were offline): " + userDownloadOffline);
                            SoulSeekState.SoulseekClient.AddUserAsync(userDownloadOffline).ContinueWith(UpdateUserOfflineDownload);
                        }
                    }

                    //this is if we wanted to change the status earlier but could not. note that when we first login, our status is Online by default.
                    //so no need to change it to online.
                    if(SoulSeekState.PendingStatusChangeToAwayOnline == SoulSeekState.PendingStatusChange.OnlinePending)
                    {
                        //we just did this by logging in...
                        MainActivity.LogDebug("online was pending");
                        SoulSeekState.PendingStatusChangeToAwayOnline = SoulSeekState.PendingStatusChange.NothingPending;
                    }
                    else if(((SoulSeekState.PendingStatusChangeToAwayOnline == SoulSeekState.PendingStatusChange.AwayPending || SoulSeekState.OurCurrentStatusIsAway)))
                    {
                        MainActivity.LogDebug("a change to away was pending / our status is away. lets set it now");

                        if(SoulSeekState.PendingStatusChangeToAwayOnline == SoulSeekState.PendingStatusChange.AwayPending)
                        {
                            MainActivity.LogDebug("pending that is....");
                        }
                        else
                        {
                            MainActivity.LogDebug("current that is...");
                        }
                        
                        if(ForegroundLifecycleTracker.NumberOfActiveActivities != 0)
                        {
                            MainActivity.LogDebug("There is a hole in our logic!!! the pendingstatus and/or current status should not be away!!!");
                        }
                        else
                        {
                            MainActivity.SetStatusApi(true);
                        }
                    }

                    //if the number of directories is stale (meaning it changing when we werent logged in and so we could not update the server)
                    //and we have not yet attempted to set up sharing (since after we attempt to set up sharing we will notify the server)
                    //then tell the server here.
                    //this makes it so that we tell the server once when Seeker first launches, and when things change, but not every time
                    //we log in.
                    if(SoulSeekState.NumberOfSharedDirectoriesIsStale && SoulSeekState.AttemptedToSetUpSharing)
                    {
                        MainActivity.LogDebug("stale and we already attempted to set up sharing, so lets do it here in post log in.");
                        MainActivity.InformServerOfSharedFiles();
                    }

                    TransfersController.InitializeService();
                }
                catch (Exception e)
                {
                    MainActivity.LogFirebase("PerformPostConnectTasks" + e.Message + e.StackTrace);
                }
            }
        }

        public static Android.Graphics.Drawables.Drawable? GetDrawableFromAttribute(Context c, int attr)
        {
            var typedValue = new TypedValue();
            c.Theme.ResolveAttribute(attr, typedValue, true);
            int drawableRes = (typedValue.ResourceId != 0) ? typedValue.ResourceId : typedValue.Data;
            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            {
                return c.Resources.GetDrawable(drawableRes, SoulSeekState.ActiveActivityRef.Theme);
            }
            else
            {
                return c.Resources.GetDrawable(drawableRes);
            }
        }

        /// <summary>
        /// UserStatusChanged will not get called until an actual change. hence this call..
        /// </summary>
        /// <param name="t"></param>
        private static void UpdateUserOfflineDownload(Task<UserData> t)
        {
            if(t.IsCompletedSuccessfully)
            {
                ProcessPotentialUserOfflineChangedEvent(t.Result.Username, t.Result.Status);
            }
        }

        /// <summary>
        /// UserStatusChanged will not get called until an actual change. hence this call..
        /// </summary>
        /// <param name="t"></param>
        private static void UpdateUserInfo(Task<UserData> t)
        {
            try
            {
                MainActivity.LogDebug("Update User Info Received");
                if (t.IsCompletedSuccessfully)
                {
                    string username = t.Result.Username;
                    MainActivity.LogDebug("Update User Info: " + username + " status: " + t.Result.Status.ToString());
                    if (MainActivity.UserListContainsUser(username))
                    {
                        MainActivity.UserListAddUser(t.Result, t.Result.Status);
                    }


                }
                else if (t.Exception?.InnerException is UserNotFoundException)
                {
                    if (t.Exception.InnerException.Message.Contains("User ") && t.Exception.InnerException.Message.Contains("does not exist"))
                    {
                        string username = t.Exception.InnerException.Message.Split(null)[1];
                        if (MainActivity.UserListContainsUser(username))
                        {
                            MainActivity.UserListSetDoesNotExist(username);
                        }
                    }
                    else
                    {
                        MainActivity.LogFirebase("unexcepted error message - " + t.Exception.InnerException.Message);
                    }
                }
                else
                {
                    //timeout
                    MainActivity.LogFirebase("UpdateUserInfo case 3 " + t.Exception.Message);
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("UpdateUserInfo" + e.Message + e.StackTrace);
            }
        }

        public static List<WeakReference<ThemeableActivity>> Activities = new List<WeakReference<ThemeableActivity>>();

        public static void RecreateActivies()
        {
            foreach (var weakRef in Activities)
            {
                if (weakRef.TryGetTarget(out var themeableActivity))
                {
                    themeableActivity.Recreate();
                }
            }
        }

        public static void SetActivityTheme(Activity a)
        {
            //useless returns the same thing every time
            //int curTheme = a.PackageManager.GetActivityInfo(a.ComponentName, 0).ThemeResource;
            if (a.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes))
            {
                a.SetTheme(ThemeHelper.ToNightThemeProper(SoulSeekState.NightModeVarient));
            }
            else
            {
                a.SetTheme(ThemeHelper.ToDayThemeProper(SoulSeekState.DayModeVarient));
            }
        }


        /// <summary>
        /// Add To User List and save user list to shared prefs.  false if already added
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static bool AddToIgnoreList(string username)
        {
            //typically its tough to add a user to ignore list from the UI if they are in the User List.
            //but for example if you ignore a user based on their message.
            //User List and Ignore List and mutually exclusive so if you ignore someone, they will be removed from user list.
            if (MainActivity.UserListContainsUser(username))
            {
                MainActivity.UserListRemoveUser(username);
            }

            lock (SoulSeekState.IgnoreUserList)
            {
                if (SoulSeekState.IgnoreUserList.Exists(userListItem => { return userListItem.Username == username; }))
                {
                    return false;
                }
                else
                {
                    SoulSeekState.IgnoreUserList.Add(new UserListItem(username, UserRole.Ignored));
                }
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_IgnoreUserList, SeekerApplication.SaveUserListToString(SoulSeekState.IgnoreUserList));
                editor.Commit();
            }
            return true;
        }

        public static void RemoveFromIgnoreListFeedback(Context c, string username)
        {
            if (RemoveFromIgnoreList(username))
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.removed_user_from_ignored_list), username), ToastLength.Short).Show();
            }
            else
            {
                //Toast.MakeText(c, string.Format(c.GetString(Resource.String.already_added_to_ignore), username), ToastLength.Short).Show();
            }
        }

        /// <summary>
        /// Remove From User List and save user list to shared prefs.  false if not found..
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static bool RemoveFromIgnoreList(string username)
        {
            lock (SoulSeekState.IgnoreUserList)
            {
                if (!SoulSeekState.IgnoreUserList.Exists(userListItem => { return userListItem.Username == username; }))
                {
                    return false;
                }
                else
                {
                    SoulSeekState.IgnoreUserList = SoulSeekState.IgnoreUserList.Where(userListItem => { return userListItem.Username != username; }).ToList();
                }
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_IgnoreUserList, SeekerApplication.SaveUserListToString(SoulSeekState.IgnoreUserList));
                editor.Commit();
            }
            return true;
        }

        public static bool IsUserInIgnoreList(string username)
        {
            lock (SoulSeekState.IgnoreUserList)
            {
                return SoulSeekState.IgnoreUserList.Exists(userListItem => { return userListItem.Username == username; });
            }
        }


        public class NotifInfo
        {
            public NotifInfo(string firstDir)
            {
                NOTIF_ID_FOR_USER = NotifIdCounter;
                NotifIdCounter++;
                FilesUploadedToUser = 1;
                DirNames = new List<string>();
                DirNames.Add(firstDir);
            }
            public int NOTIF_ID_FOR_USER;
            public int FilesUploadedToUser;
            public List<string> DirNames = new List<string>();
        }

        public static Dictionary<string, NotifInfo> NotificationUploadTracker = new Dictionary<string, NotifInfo>();
        public static int NotifIdCounter = 400;

        /// <summary>
        /// this is for global uploading event handling only.  the tabpageadapter is the one for downloading... and for upload tranferpage specific events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Upload_TransferStateChanged(object sender, TransferStateChangedEventArgs e)
        {
            if (e.Transfer == null || e.Transfer.Direction == TransferDirection.Download)
            {
                return;
            }
            if (e.Transfer.State == TransferStates.InProgress)
            {
                MainActivity.LogDebug("transfer state changed to in progress" + e.Transfer.Filename);
                //uploading file to user...
            }
            //if(e.Transfer.State == TransferStates.Completed) //this condition will NEVER be hit.  it is always completed | succeeded
            if (e.Transfer.State.HasFlag(TransferStates.Succeeded)) //todo rethink upload notifications....
            {
                MainActivity.LogDebug("transfer state changed to completed" + e.Transfer.Filename);
                //send notif successfully uploading file to user..
                //e.Transfer.AverageSpeed - speed in bytes/second
                if (e.Transfer.AverageSpeed <= 0 || ((int)(e.Transfer.AverageSpeed)) == 0)
                {
                    MainActivity.LogDebug("avg speed <= 0" + e.Transfer.Filename);
                    return;
                }
                MainActivity.LogDebug("sending avg speed of " + e.Transfer.AverageSpeed.ToString());
                SoulSeekState.SoulseekClient.SendUploadSpeedAsync((int)(e.Transfer.AverageSpeed));
                try
                {
                    Helpers.CreateNotificationChannel(SoulSeekState.MainActivityRef, MainActivity.UPLOADS_CHANNEL_ID, MainActivity.UPLOADS_CHANNEL_NAME, NotificationImportance.High);
                    NotifInfo notifInfo = null;
                    string directory = Helpers.GetFolderNameFromFile(e.Transfer.Filename.Replace("/", @"\"));
                    if (NotificationUploadTracker.ContainsKey(e.Transfer.Username))
                    {
                        notifInfo = NotificationUploadTracker[e.Transfer.Username];
                        if (!notifInfo.DirNames.Contains(directory))
                        {
                            notifInfo.DirNames.Add(directory);
                        }
                        notifInfo.FilesUploadedToUser++;
                    }
                    else
                    {
                        notifInfo = new NotifInfo(directory);
                        NotificationUploadTracker.Add(e.Transfer.Username, notifInfo);
                    }

                    Notification n = MainActivity.CreateUploadNotification(SoulSeekState.MainActivityRef, e.Transfer.Username, notifInfo.DirNames, notifInfo.FilesUploadedToUser);
                    NotificationManagerCompat nmc = NotificationManagerCompat.From(SoulSeekState.MainActivityRef);
                    nmc.Notify(e.Transfer.Username.GetHashCode(), n);
                }
                catch (Exception err)
                {
                    MainActivity.LogFirebase("Upload Noficiation Failed" + err.Message + err.StackTrace);
                }
            }
        }



        /// <summary>
        /// this is for getting additional information (status updates) from already added users 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="userData"></param>
        /// <param name="userStatus"></param>
        private static bool UserListAddIfContainsUser(string username, UserData userData, UserStatus userStatus)
        {
            UserPresence? prevStatus = UserPresence.Offline;
            bool found = false;
            lock (SoulSeekState.UserList)
            {

                foreach (UserListItem item in SoulSeekState.UserList)
                {
                    if (item.Username == username)
                    {
                        found = true;
                        if (userData != null)
                        {
                            item.UserData = userData;
                        }
                        if (userStatus != null)
                        {
                            prevStatus = item.UserStatus?.Presence ?? UserPresence.Offline;
                            item.UserStatus = userStatus;
                        }
                        break;
                    }
                }
            }
            //if user was previously offline and now they are not offline, then do the notification.
            //note - this method does not get called when first adding users. which I think is ideal for notifications.
            //if not in our user list, then this is likely a result of GetUserInfo!, so dont do any of this..
            if (found && (!prevStatus.HasValue || prevStatus.Value == UserPresence.Offline && (userStatus != null && userStatus.Presence != UserPresence.Offline)))
            {
                MainActivity.LogDebug("from offline to online " + username);
                if (SoulSeekState.UserOnlineAlerts != null && SoulSeekState.UserOnlineAlerts.ContainsKey(username))
                {
                    //show notification.
                    ShowNotificationForUserOnlineAlert(username);
                }
            }
            else
            {
                MainActivity.LogDebug("NOT from offline to online (or not in user list)" + username);
            }
            return found;
        }

        public static View GetViewForSnackbar()
        {
            bool useDownloadDialogFragment = false;
            View v = null;
            if (SoulSeekState.ActiveActivityRef is MainActivity mar)
            {
                var f = mar.SupportFragmentManager.FindFragmentByTag("tag_download_test");
                //this is the only one we have..  tho obv a more generic way would be to see if s/t is a dialog fragmnet.  but arent a lot of just simple alert dialogs etc dialog fragment?? maybe explicitly checking is the best way.
                if (f != null && f.IsVisible)
                {
                    useDownloadDialogFragment = true;
                    v = f.View;
                }
            }
            if (!useDownloadDialogFragment)
            {
                v = SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content);
            }
            return v;
        }

        public const string CHANNEL_ID_USER_ONLINE = "User Online Alerts ID";
        public const string CHANNEL_NAME_USER_ONLINE = "User Online Alerts";
        public const string FromUserOnlineAlert = "FromUserOnlineAlert";
        public static void ShowNotificationForUserOnlineAlert(string username)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    Helpers.CreateNotificationChannel(SoulSeekState.ActiveActivityRef, CHANNEL_ID_USER_ONLINE, CHANNEL_NAME_USER_ONLINE, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SoulSeekState.ActiveActivityRef, typeof(UserListActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop);
                    notifIntent.PutExtra(FromUserOnlineAlert, true);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SoulSeekState.ActiveActivityRef, username.GetHashCode(), notifIntent, Helpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = Helpers.CreateNotification(SoulSeekState.ActiveActivityRef, pendingIntent, CHANNEL_ID_USER_ONLINE, "User Online", string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.user_X_is_now_online), username), false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SoulSeekState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify(username.GetHashCode(), n);
                }
                catch (System.Exception e)
                {
                    MainActivity.LogFirebase("ShowNotificationForUserOnlineAlert failed: " + e.Message + e.StackTrace);
                }
            });
        }


        public const string CHANNEL_ID_FOLDER_ALERT = "Folder Finished Downloading Alerts ID";
        public const string CHANNEL_NAME_FOLDER_ALERT = "Folder Finished Downloading Alerts";
        public const string FromFolderAlert = "FromFolderAlert";
        public const string FromFolderAlertUsername = "FromFolderAlertUsername";
        public const string FromFolderAlertFoldername = "FromFolderAlertFoldername";
        public static void ShowNotificationForCompletedFolder(string foldername, string username)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    Helpers.CreateNotificationChannel(SoulSeekState.ActiveActivityRef, CHANNEL_ID_FOLDER_ALERT, CHANNEL_NAME_FOLDER_ALERT, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ReorderToFront); //otherwise if another activity is in front then this intent will do nothing...
                    notifIntent.PutExtra(FromFolderAlert, 2);
                    notifIntent.PutExtra(FromFolderAlertUsername, username);
                    notifIntent.PutExtra(FromFolderAlertFoldername, foldername);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SoulSeekState.ActiveActivityRef, (foldername + username).GetHashCode(), notifIntent, Helpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = Helpers.CreateNotification(SoulSeekState.ActiveActivityRef, pendingIntent, CHANNEL_ID_FOLDER_ALERT, SeekerApplication.GetString(Resource.String.FolderFinishedDownloading), string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.folder_X_from_user_Y_finished), foldername, username), false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SoulSeekState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify((foldername + username).GetHashCode(), n);
                }
                catch (System.Exception e)
                {
                    MainActivity.LogFirebase("ShowNotificationForCompletedFolder failed: " + e.Message + e.StackTrace);
                }
            });
        }


        public static void RestoreSoulSeekState(ISharedPreferences sharedPreferences, Context c) //the Bundle can be SLOWER than the SHARED PREFERENCES if SHARED PREFERENCES was saved in a different activity.  The best exapmle being DAYNIGHTMODE
        {   //day night mode sets the static, saves to shared preferences the new value, sets appcompat value, which recreates everything and calls restoreSoulSEekstate(bundle) where the bundle was older than shared prefs
            //because saveSoulSeekstate was not called in the meantime...
            if (sharedPreferences != null)
            {
                SoulSeekState.currentlyLoggedIn = sharedPreferences.GetBoolean(SoulSeekState.M_CurrentlyLoggedIn, false);
                SoulSeekState.Username = sharedPreferences.GetString(SoulSeekState.M_Username, "");
                SoulSeekState.Password = sharedPreferences.GetString(SoulSeekState.M_Password, "");
                SoulSeekState.SaveDataDirectoryUri = sharedPreferences.GetString(SoulSeekState.M_SaveDataDirectoryUri, "");
                SoulSeekState.SaveDataDirectoryUriIsFromTree = sharedPreferences.GetBoolean(SoulSeekState.M_SaveDataDirectoryUriIsFromTree, true);
                SoulSeekState.NumberSearchResults = sharedPreferences.GetInt(SoulSeekState.M_NumberSearchResults, MainActivity.DEFAULT_SEARCH_RESULTS);
                SoulSeekState.DayNightMode = sharedPreferences.GetInt(SoulSeekState.M_DayNightMode, (int)AppCompatDelegate.ModeNightFollowSystem);
                SoulSeekState.Language = sharedPreferences.GetString(SoulSeekState.M_Lanuage, SoulSeekState.FieldLangAuto);
                SoulSeekState.NightModeVarient = (ThemeHelper.NightThemeType)(sharedPreferences.GetInt(SoulSeekState.M_NightVarient, (int)ThemeHelper.NightThemeType.ClassicPurple));
                SoulSeekState.DayModeVarient = (ThemeHelper.DayThemeType)(sharedPreferences.GetInt(SoulSeekState.M_DayVarient, (int)ThemeHelper.DayThemeType.ClassicPurple));
                SoulSeekState.AutoClearCompleteDownloads = sharedPreferences.GetBoolean(SoulSeekState.M_AutoClearComplete, false);
                SoulSeekState.AutoClearCompleteUploads = sharedPreferences.GetBoolean(SoulSeekState.M_AutoClearCompleteUploads, false);
                SoulSeekState.RememberSearchHistory = sharedPreferences.GetBoolean(SoulSeekState.M_RememberSearchHistory, true);
                SoulSeekState.ShowRecentUsers = sharedPreferences.GetBoolean(SoulSeekState.M_RememberUserHistory, true);
                SoulSeekState.FreeUploadSlotsOnly = sharedPreferences.GetBoolean(SoulSeekState.M_OnlyFreeUploadSlots, true);
                SoulSeekState.HideLockedResultsInBrowse = sharedPreferences.GetBoolean(SoulSeekState.M_HideLockedBrowse, true);
                SoulSeekState.HideLockedResultsInSearch = sharedPreferences.GetBoolean(SoulSeekState.M_HideLockedSearch, true);

                SoulSeekState.TransferViewShowSizes = sharedPreferences.GetBoolean(SoulSeekState.M_TransfersShowSizes, false);
                SoulSeekState.TransferViewShowSpeed = sharedPreferences.GetBoolean(SoulSeekState.M_TransfersShowSpeed, false);

                SoulSeekState.SpeedLimitUploadOn = sharedPreferences.GetBoolean(SoulSeekState.M_UploadLimitEnabled, false);
                SoulSeekState.SpeedLimitDownloadOn = sharedPreferences.GetBoolean(SoulSeekState.M_DownloadLimitEnabled, false);
                SoulSeekState.SpeedLimitUploadIsPerTransfer = sharedPreferences.GetBoolean(SoulSeekState.M_UploadPerTransfer, true);
                SoulSeekState.SpeedLimitDownloadIsPerTransfer = sharedPreferences.GetBoolean(SoulSeekState.M_DownloadPerTransfer, true);
                SoulSeekState.SpeedLimitUploadBytesSec = sharedPreferences.GetInt(SoulSeekState.M_UploadSpeedLimitBytes, 4 * 1024 * 1024);
                SoulSeekState.SpeedLimitDownloadBytesSec = sharedPreferences.GetInt(SoulSeekState.M_DownloadSpeedLimitBytes, 4 * 1024 * 1024);

                SoulSeekState.DisableDownloadToastNotification = sharedPreferences.GetBoolean(SoulSeekState.M_DisableToastNotifications, true);
                SoulSeekState.MemoryBackedDownload = sharedPreferences.GetBoolean(SoulSeekState.M_MemoryBackedDownload, false);
                SearchFragment.FilterSticky = sharedPreferences.GetBoolean(SoulSeekState.M_FilterSticky, false);
                SearchFragment.FilterStickyString = sharedPreferences.GetString(SoulSeekState.M_FilterStickyString, string.Empty);
                SearchFragment.SetSearchResultStyle(sharedPreferences.GetInt(SoulSeekState.M_SearchResultStyle, 1));
                SoulSeekState.UploadSpeed = sharedPreferences.GetInt(SoulSeekState.M_UploadSpeed, -1);



                UploadDirectoryManager.RestoreFromSavedState(sharedPreferences);

                SoulSeekState.SharingOn = sharedPreferences.GetBoolean(SoulSeekState.M_SharingOn, false);
                SoulSeekState.UserList = RestoreUserListFromString(sharedPreferences.GetString(SoulSeekState.M_UserList, string.Empty));

                RestoreRecentUsersManagerFromString(sharedPreferences.GetString(SoulSeekState.M_RecentUsersList, string.Empty));
                SoulSeekState.IgnoreUserList = RestoreUserListFromString(sharedPreferences.GetString(SoulSeekState.M_IgnoreUserList, string.Empty));
                SoulSeekState.AllowPrivateRoomInvitations = sharedPreferences.GetBoolean(SoulSeekState.M_AllowPrivateRooomInvitations, false);
                SoulSeekState.StartServiceOnStartup = sharedPreferences.GetBoolean(SoulSeekState.M_ServiceOnStartup, true);

                SoulSeekState.ShowSmartFilters = sharedPreferences.GetBoolean(SoulSeekState.M_ShowSmartFilters, false);
                RestoreSmartFilterState(sharedPreferences);

                SoulSeekState.UserInfoBio = sharedPreferences.GetString(SoulSeekState.M_UserInfoBio, string.Empty);
                SoulSeekState.UserInfoPictureName = sharedPreferences.GetString(SoulSeekState.M_UserInfoPicture, string.Empty);

                SoulSeekState.UserNotes = RestoreUserNotesFromString(sharedPreferences.GetString(SoulSeekState.M_UserNotes, string.Empty));
                SoulSeekState.UserOnlineAlerts = RestoreUserOnlineAlertsFromString(sharedPreferences.GetString(SoulSeekState.M_UserOnlineAlerts, string.Empty));

                SoulSeekState.AutoAwayOnInactivity = sharedPreferences.GetBoolean(SoulSeekState.M_AutoSetAwayOnInactivity, false);
                SoulSeekState.AutoRetryBackOnline = sharedPreferences.GetBoolean(SoulSeekState.M_AutoRetryBackOnline, true);

                SoulSeekState.NotifyOnFolderCompleted = sharedPreferences.GetBoolean(SoulSeekState.M_NotifyFolderComplete, true);
                SoulSeekState.AllowUploadsOnMetered = sharedPreferences.GetBoolean(SoulSeekState.M_AllowUploadsOnMetered, true);

                UserListActivity.UserListSortOrder = (UserListActivity.SortOrder)(sharedPreferences.GetInt(SoulSeekState.M_UserListSortOrder, 0));
                SoulSeekState.DefaultSearchResultSortAlgorithm = (SearchResultSorting)(sharedPreferences.GetInt(SoulSeekState.M_DefaultSearchResultSortAlgorithm, 0));

                SimultaneousDownloadsGatekeeper.Initialize(sharedPreferences.GetBoolean(SoulSeekState.M_LimitSimultaneousDownloads, false), sharedPreferences.GetInt(SoulSeekState.M_MaxSimultaneousLimit, 1));

                //SearchTabHelper.RestoreStateFromSharedPreferencesLegacy();

                //SearchTabHelper.SaveHeadersToSharedPrefs();
                //SearchTabHelper.SaveAllSearchTabsToDisk(c);
                SearchTabHelper.ConvertLegacyWishlistsIfApplicable(c);
                SearchTabHelper.RestoreHeadersFromSharedPreferences();
                //SearchTabHelper.RestoreAllSearchTabsFromDisk(c);

                SettingsActivity.RestoreAdditionalDirectorySettingsFromSharedPreferences();

                ChatroomActivity.ShowStatusesView = sharedPreferences.GetBoolean(SoulSeekState.M_ShowStatusesView, true);
                ChatroomActivity.ShowTickerView = sharedPreferences.GetBoolean(SoulSeekState.M_ShowTickerView, false);
                ChatroomController.SortChatroomUsersBy = (ChatroomController.SortOrderChatroomUsers)(sharedPreferences.GetInt(SoulSeekState.M_RoomUserListSortOrder, 2)); //default is 2 = alphabetical..
                ChatroomController.PutFriendsOnTop = sharedPreferences.GetBoolean(SoulSeekState.M_RoomUserListShowFriendsAtTop, false);

                SeekerApplication.LOG_DIAGNOSTICS = sharedPreferences.GetBoolean(SoulSeekState.M_LOG_DIAGNOSTICS, false);


                if (TransfersFragment.TransferItemManagerDL == null)
                {
                    TransfersFragment.RestoreDownloadTransferItems(sharedPreferences);
                    TransfersFragment.RestoreUploadTransferItems(sharedPreferences);
                    TransfersFragment.TransferItemManagerWrapped = new TransferItemManagerWrapper(TransfersFragment.TransferItemManagerUploads, TransfersFragment.TransferItemManagerDL);
                }
            }
        }

        public static void RestoreListeningState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                SoulSeekState.ListenerEnabled = SoulSeekState.SharedPreferences.GetBoolean(SoulSeekState.M_ListenerEnabled, true);
                SoulSeekState.ListenerPort = SoulSeekState.SharedPreferences.GetInt(SoulSeekState.M_ListenerPort, 33939);
                SoulSeekState.ListenerUPnpEnabled = SoulSeekState.SharedPreferences.GetBoolean(SoulSeekState.M_ListenerUPnpEnabled, true);
            }
        }

        public static void SaveListeningState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutBoolean(SoulSeekState.M_ListenerEnabled, SoulSeekState.ListenerEnabled);
                editor.PutInt(SoulSeekState.M_ListenerPort, SoulSeekState.ListenerPort);
                editor.PutBoolean(SoulSeekState.M_ListenerUPnpEnabled, SoulSeekState.ListenerUPnpEnabled);
                editor.Commit();
            }
        }

        public static void SaveSpeedLimitState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutBoolean(SoulSeekState.M_DownloadLimitEnabled, SoulSeekState.SpeedLimitDownloadOn);
                editor.PutBoolean(SoulSeekState.M_DownloadPerTransfer, SoulSeekState.SpeedLimitDownloadIsPerTransfer);
                editor.PutInt(SoulSeekState.M_DownloadSpeedLimitBytes, SoulSeekState.SpeedLimitDownloadBytesSec);

                editor.PutBoolean(SoulSeekState.M_UploadLimitEnabled, SoulSeekState.SpeedLimitUploadOn);
                editor.PutBoolean(SoulSeekState.M_UploadPerTransfer, SoulSeekState.SpeedLimitUploadIsPerTransfer);
                editor.PutInt(SoulSeekState.M_UploadSpeedLimitBytes, SoulSeekState.SpeedLimitUploadBytesSec);

                editor.Commit();
            }
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, string> RestoreUserNotesFromString(string base64userNotes)
        {
            if (base64userNotes == string.Empty)
            {
                return new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            }
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(base64userNotes)))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, string>;
            }
        }

        public static string SaveUserNotesToString(System.Collections.Concurrent.ConcurrentDictionary<string, string> userNotes)
        {
            if (userNotes == null || userNotes.Keys.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                using (System.IO.MemoryStream userNotesStream = new System.IO.MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(userNotesStream, userNotes);
                    return Convert.ToBase64String(userNotesStream.ToArray());
                }
            }
        }

        public static string SaveUserOnlineAlertsFromString(System.Collections.Concurrent.ConcurrentDictionary<string, byte> onlineAlertsDict)
        {
            if (onlineAlertsDict == null || onlineAlertsDict.Keys.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                using (System.IO.MemoryStream onlineAlertsStream = new System.IO.MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(onlineAlertsStream, onlineAlertsDict);
                    return Convert.ToBase64String(onlineAlertsStream.ToArray());
                }
            }
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte> RestoreUserOnlineAlertsFromString(string base64onlineAlerts)
        {
            if (base64onlineAlerts == string.Empty)
            {
                return new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
            }
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(base64onlineAlerts)))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, byte>;
            }
        }

        public static List<UserListItem> RestoreUserListFromString(string base64userList)
        {
            if (base64userList == string.Empty)
            {
                return new List<UserListItem>();
            }
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(base64userList)))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(mem) as List<UserListItem>;
            }
        }

        public static void SetupRecentUserAutoCompleteTextView(AutoCompleteTextView actv, bool forAddingUser = false)
        {
            if (SoulSeekState.ShowRecentUsers)
            {
                if (forAddingUser)
                {
                    //dont show people that we have already added...
                    var recents = SoulSeekState.RecentUsersManager.GetRecentUserList();
                    lock (SoulSeekState.UserList)
                    {
                        foreach (var uli in SoulSeekState.UserList)
                        {
                            recents.Remove(uli.Username);
                        }
                    }
                    actv.Adapter = new ArrayAdapter<string>(SoulSeekState.ActiveActivityRef, Resource.Layout.autoSuggestionRow, recents);
                }
                else
                {
                    actv.Adapter = new ArrayAdapter<string>(SoulSeekState.ActiveActivityRef, Resource.Layout.autoSuggestionRow, SoulSeekState.RecentUsersManager.GetRecentUserList());
                }
            }
        }

        public static void RestoreSmartFilterState(ISharedPreferences sharedPreferences)
        {
            SoulSeekState.SmartFilterOptions = new SoulSeekState.SmartFilterState();
            SoulSeekState.SmartFilterOptions.KeywordsEnabled = sharedPreferences.GetBoolean(SoulSeekState.M_SmartFilter_KeywordsEnabled, true);
            SoulSeekState.SmartFilterOptions.KeywordsOrder = sharedPreferences.GetInt(SoulSeekState.M_SmartFilter_KeywordsOrder, 0);
            SoulSeekState.SmartFilterOptions.FileTypesEnabled = sharedPreferences.GetBoolean(SoulSeekState.M_SmartFilter_TypesEnabled, true);
            SoulSeekState.SmartFilterOptions.FileTypesOrder = sharedPreferences.GetInt(SoulSeekState.M_SmartFilter_TypesOrder, 1);
            SoulSeekState.SmartFilterOptions.NumFilesEnabled = sharedPreferences.GetBoolean(SoulSeekState.M_SmartFilter_CountsEnabled, true);
            SoulSeekState.SmartFilterOptions.NumFilesOrder = sharedPreferences.GetInt(SoulSeekState.M_SmartFilter_CountsOrder, 2);
        }

        public static void SaveSmartFilterState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutBoolean(SoulSeekState.M_SmartFilter_KeywordsEnabled, SoulSeekState.SmartFilterOptions.KeywordsEnabled);
                editor.PutBoolean(SoulSeekState.M_SmartFilter_TypesEnabled, SoulSeekState.SmartFilterOptions.FileTypesEnabled);
                editor.PutBoolean(SoulSeekState.M_SmartFilter_CountsEnabled, SoulSeekState.SmartFilterOptions.NumFilesEnabled);
                editor.PutInt(SoulSeekState.M_SmartFilter_KeywordsOrder, SoulSeekState.SmartFilterOptions.KeywordsOrder);
                editor.PutInt(SoulSeekState.M_SmartFilter_TypesOrder, SoulSeekState.SmartFilterOptions.FileTypesOrder);
                editor.PutInt(SoulSeekState.M_SmartFilter_CountsOrder, SoulSeekState.SmartFilterOptions.NumFilesOrder);
                editor.Commit();
            }
        }

        public static void RestoreRecentUsersManagerFromString(string xmlRecentUsersList)
        {
            //if empty then this is the first time creating it.  initialize it with our list of added users.
            SoulSeekState.RecentUsersManager = new RecentUserManager();
            if (xmlRecentUsersList == string.Empty)
            {
                int count = SoulSeekState.UserList?.Count ?? 0;
                if (count > 0)
                {
                    SoulSeekState.RecentUsersManager.SetRecentUserList(SoulSeekState.UserList.Select(uli => uli.Username).ToList());
                }
                else
                {
                    SoulSeekState.RecentUsersManager.SetRecentUserList(new List<string>());
                }
            }
            else
            {
                List<string> recentUsers = new List<string>();
                using (var stream = new System.IO.StringReader(xmlRecentUsersList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(recentUsers.GetType()); //this happens too often not allowing new things to be properly stored..
                    SoulSeekState.RecentUsersManager.SetRecentUserList(serializer.Deserialize(stream) as List<string>);
                }
            }
        }

        public static void SaveRecentUsers()
        {
            string recentUsersStr;
            List<string> recentUsers = SoulSeekState.RecentUsersManager.GetRecentUserList();
            using (var writer = new System.IO.StringWriter())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(recentUsers.GetType());
                serializer.Serialize(writer, recentUsers);
                recentUsersStr = writer.ToString();
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_RecentUsersList, recentUsersStr);
                editor.Commit();
            }
        }

        public static string SaveUserListToString(List<UserListItem> userList)
        {
            if (userList == null || userList.Count == 0)
            {
                return string.Empty;
            }
            else
            {
                using (System.IO.MemoryStream userListStream = new System.IO.MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(userListStream, userList);
                    return Convert.ToBase64String(userListStream.ToArray());
                }
            }
        }


        /// <summary>
        /// This is from the server after sending it a UserData request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoulseekClient_UserDataReceived(object sender, UserData e)
        {
            MainActivity.LogDebug("User Data Received: " + e.Username);
            if (e.Username == SoulSeekState.Username)
            {
                SoulSeekState.UploadSpeed = e.AverageSpeed; //bytes
            }
            else
            {
                if (SoulSeekState.UserList == null)
                {
                    MainActivity.LogFirebase("UserList is null on user data receive");
                }
                else
                {
                    UserListAddIfContainsUser(e.Username, e, null);
                }

                RequestedUserInfoHelper.AddIfRequestedUser(e.Username, e, null, null);
            }
        }

        private static string DeduplicateUsername = null;
        private static Soulseek.UserPresence DeduplicateStatus = Soulseek.UserPresence.Offline;
        private void SoulseekClient_UserStatusChanged_Deduplicator(object sender, UserStatusChangedEventArgs e)
        {

            if (DeduplicateUsername == e.Username && DeduplicateStatus == e.Status)
            {
                MainActivity.LogDebug($"throwing away {e.Username} status changed");
                return;
            }
            else
            {
                MainActivity.LogDebug($"handling {e.Username} status changed");
                DeduplicateUsername = e.Username;
                DeduplicateStatus = e.Status;
                SeekerApplication.UserStatusChangedDeDuplicated?.Invoke(sender, e);
            }
        }

        public static void ProcessPotentialUserOfflineChangedEvent(string username, UserPresence status)
        {
            if (status != UserPresence.Offline)
            {
                if (SoulSeekState.AutoRetryBackOnline)
                {
                    if (TransfersFragment.UsersWhereDownloadFailedDueToOffline.ContainsKey(username))
                    {
                        MainActivity.LogDebug("the user came back who we previously dl from " + username);
                        //retry all failed downloads from them..
                        List<TransferItem> items = TransfersFragment.TransferItemManagerDL.GetTransferItemsFromUser(username, true, true);
                        if (items.Count == 0)
                        {
                            //no offline, then remove this user.
                            lock (TransfersFragment.UsersWhereDownloadFailedDueToOffline)
                            {
                                TransfersFragment.UsersWhereDownloadFailedDueToOffline.Remove(username);
                            }
                        }
                        else
                        {
                            try
                            {
                                TransfersFragment.DownloadRetryAllConditionLogic(false, false, null, true, items);
                            }
                            catch(Exception e)
                            {
                                MainActivity.LogDebug("ProcessPotentialUserOfflineChangedEvent" + e.Message);
                            }
                        }

                    }
                }
            }
        }


        public static EventHandler<string> UserStatusChangedUIEvent;
        private void SoulseekClient_UserStatusChanged(object sender, UserStatusChangedEventArgs e)
        {
            if (e.Username == SoulSeekState.Username)
            {
                //not sure this will ever happen
            }
            else
            {
                //we get user status changed for those we are in the same room as us
                if (SoulSeekState.UserList != null)
                {
                    bool found = UserListAddIfContainsUser(e.Username, null, new UserStatus(e.Status, e.IsPrivileged));
                    if (found)
                    {
                        MainActivity.LogDebug("friend status changed " + e.Username);
                        SeekerApplication.UserStatusChangedUIEvent?.Invoke(null, e.Username);
                    }
                }

                ProcessPotentialUserOfflineChangedEvent(e.Username, e.Status);
            }

        }


    }


    public class SearchResponseComparer : IEqualityComparer<SearchResponse>
    {
        private bool hideLockedResults = true;

        public SearchResponseComparer(bool _hideLocked)
        {
            hideLockedResults = _hideLocked;
        }

        public bool Equals(SearchResponse s1, SearchResponse s2)
        {
            if (s1.Username == s2.Username)
            {
                if (s1.Files.Count == s2.Files.Count)
                {
                    if (s1.Files.Count == 0)
                    {
                        return s1.LockedFiles.First().Filename == s2.LockedFiles.First().Filename;
                    }
                    if (s1.Files.First().Filename == s2.Files.First().Filename)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            return false;
        }

        public int GetHashCode(SearchResponse s1)
        {
            return s1.Username.GetHashCode() + s1.GetElementAtAdapterPosition(hideLockedResults, 0).Filename.GetHashCode();
        }
    }

    public class DownloadDirectoryNotSetException : System.Exception
    {
    }

    public class FaultPropagationException : System.Exception
    {
    }

    public class TransfersController
    {
        private static System.Timers.Timer TransfersTimer = null;
        public static bool IsInitialized = false;
#if DEBUG
        private const int transfersInterval = 2 * 60 * 1000; //2 mins, faster for testing..
#else
        private const int transfersInterval = 5 * 60 * 1000;

#endif

        public static void InitializeService()
        {
            //we run this once after our first login.

            if (IsInitialized)
            {
                return;
            }

            if(SoulSeekState.AutoRequeueDownloadsAtStartup)
            {
                var queuedTransfers = TransfersFragment.TransferItemManagerDL.GetListOfCondition(TransferStates.Queued);
                if (queuedTransfers.Count > 0)
                {
                    MainActivity.LogDebug("TransfersTimerElapsed - Lets redownload and/or get position of queued transfers...");
                    MainActivity.GetDownloadPlaceInQueueBatch(queuedTransfers, true);
                }
            }

            MainActivity.LogDebug("TransfersController InitializeService");
            TransfersTimer = new System.Timers.Timer(transfersInterval);
            TransfersTimer.AutoReset = true;
            TransfersTimer.Elapsed += TransfersTimer_Elapsed;
            TransfersTimer.Start();
            IsInitialized = true;
        }

        private static void TransfersTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MainActivity.LogDebug("TransfersTimerElapsed");
            if(MainActivity.IsNotLoggedIn())
            {
                return;
            }
            var queuedTransfers = TransfersFragment.TransferItemManagerDL.GetListOfCondition(TransferStates.Queued);
            if(queuedTransfers.Count > 0)
            {
                MainActivity.LogDebug("TransfersTimerElapsed - Lets get position of queued transfers...");
                MainActivity.GetDownloadPlaceInQueueBatch(queuedTransfers, false);
            }

        }
    }

    public class WishlistController
    {
        private static int searchIntervalMilliseconds = -1;
        public static int SearchIntervalMilliseconds
        {
            get
            {
                return searchIntervalMilliseconds;
            }
            set
            {
                searchIntervalMilliseconds = value; //inverval of 0 means NOT ALLOWED.  In general we should also do a Min value of like 1 minute in case the server sends something not good.
                if (!IsInitialized)
                {
                    Initialize();
                }
            }
        }
        public static bool IsInitialized = false;

        private static System.Timers.Timer WishlistTimer = null;
        public static System.Collections.Concurrent.ConcurrentDictionary<int, HashSet<SearchResponse>> OldResultsToCompare = new System.Collections.Concurrent.ConcurrentDictionary<int, HashSet<SearchResponse>>();
        private static System.Collections.Concurrent.ConcurrentDictionary<int, int> OldNumResults = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();

        public static void Initialize() //we need the wishlist interval before we can init
        {
            if (IsInitialized)
            {
                return;
            }
            if (searchIntervalMilliseconds == 0)
            {
                IsInitialized = true;
                MainActivity.LogFirebase("Wishlist not allowed");
                return;
            }
            if (searchIntervalMilliseconds == -1)
            {
                IsInitialized = true;
                MainActivity.LogFirebase("Wishlist interval is -1");
                return;
            }
            if (searchIntervalMilliseconds < 1000 * 60 * 2)
            {
                MainActivity.LogFirebase("Wishlist interval is: " + searchIntervalMilliseconds);
                searchIntervalMilliseconds = 2 * 60 * 1000; //min of 2 mins...
            }

#if DEBUG
            searchIntervalMilliseconds = 1000 * 30; //turn off for now...
#endif

            WishlistTimer = new System.Timers.Timer(searchIntervalMilliseconds);
            WishlistTimer.AutoReset = true;
            WishlistTimer.Elapsed += WishlistTimer_Elapsed;
            WishlistTimer.Start();
            IsInitialized = true;
        }
        public const string CHANNEL_ID = "Wishlist Controller ID";
        public const string CHANNEL_NAME = "Wishlists";
        public const string FromWishlistString = "FromWishlistTabID";
        public const string FromWishlistStringID = "FromWishlistTabIDToGoTo";
        public static void SearchCompleted(int id)
        {
            //a search that we initiated completed...
            //var newResponses = SearchTabHelper.SearchTabCollection[id].SearchResponses.ToList();
            //var differenceNewResults = newResponses.Except(OldResultsToCompare[id],new SearchResponseComparer(SoulSeekState.HideLockedResultsInSearch)).ToList();
            OldResultsToCompare.TryRemove(id, out _); //save memory. wont always exist if the tab got deleted during the search.  exceptions thrown here dont crash anything tho.
            int newUniqueResults = SearchTabHelper.SearchTabCollection[id].SearchResponses.Count - OldNumResults[id];

            if (newUniqueResults >= 1)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    try
                    {
                        string description = string.Empty;
                        if (newUniqueResults > 1)
                        {
                            description = newUniqueResults + " " + SoulSeekState.ActiveActivityRef.GetString(Resource.String.new_results);
                        }
                        else
                        {
                            description = newUniqueResults + " " + SoulSeekState.ActiveActivityRef.GetString(Resource.String.new_result);
                        }
                        string lastTerm = SearchTabHelper.SearchTabCollection[id].LastSearchTerm;

                        Helpers.CreateNotificationChannel(SoulSeekState.ActiveActivityRef, CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High); //only high will "peek"
                        Intent notifIntent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                        notifIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ReorderToFront); //otherwise if another activity is in front then this intent will do nothing...
                        notifIntent.PutExtra(FromWishlistString, 1); //the tab to go to
                        notifIntent.PutExtra(FromWishlistStringID, id); //the tab to go to
                        PendingIntent pendingIntent =
                            PendingIntent.GetActivity(SoulSeekState.ActiveActivityRef, lastTerm.GetHashCode(), notifIntent, Helpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                        Notification n = Helpers.CreateNotification(SoulSeekState.ActiveActivityRef, pendingIntent, CHANNEL_ID, SoulSeekState.ActiveActivityRef.GetString(Resource.String.wishlist) + ": " + lastTerm, description, false);
                        NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SoulSeekState.ActiveActivityRef);
                        // notificationId is a unique int for each notification that you must define
                        notificationManager.Notify(lastTerm.GetHashCode(), n);
                    }
                    catch (System.Exception e)
                    {
                        MainActivity.LogFirebase("ShowNotification For Wishlist failed: " + e.Message + e.StackTrace);
                    }
                });
            }
            SearchTabHelper.SaveHeadersToSharedPrefs();
            SearchTabHelper.SaveSearchResultsToDisk(id, SoulSeekState.ActiveActivityRef);
        }

        private static void WishlistTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (SearchTabHelper.SearchTabCollection != null)
            {
                var wishlistPairs = SearchTabHelper.SearchTabCollection.Where(pair => pair.Value.SearchTarget == SearchTarget.Wishlist);
                if (wishlistPairs.Count() == 0)
                {
                    return;
                }
                else
                {
                    MainActivity.LogInfoFirebase("wishlist search ran " + searchIntervalMilliseconds);
                    DateTime oldest = DateTime.MaxValue;
                    int oldestId = int.MaxValue;
                    foreach (var pair in wishlistPairs)
                    {
                        if (pair.Value.LastRanTime < oldest)
                        {
                            oldest = pair.Value.LastRanTime;
                            oldestId = pair.Key;
                        }
                    }
                    //oldestId is the one we want to autosearch

                    //search response count: 4220 hashSet count: 1000 time 102 ms
                    //search response count: 13880 hashSet count: 1000 time 71 ms
                    //search response count: 14655 hashSet count: 1000 time 174 ms
                    //search response count: 2615 hashSet count: 999 time 21 ms
                    //search response count: 15758 hashSet count: 1000 time 124 ms
                    //search response count: 937 hashSet count: 937 time 4 ms

                    //this is incase someone is privileged (searching every 2 mins) and perhaps they only have 1 wishlist search.  we dont want the second to begin when the first hasnt even ended.
                    //there arent really any downsides if this happens actually....

                    if (!SearchTabHelper.SearchTabCollection[oldestId].IsLoaded())
                    {
                        SearchTabHelper.RestoreSearchResultsFromDisk(oldestId, SoulSeekState.ActiveActivityRef);
                    }


                    if (!OldResultsToCompare.ContainsKey(oldestId)) //this is better than setting currentlySearching bc currentlySearching changes UI components like the transition drawable, which I think is just too much happening for the user.
                    {
#if DEBUG
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        sw.Start();
#endif
                        OldNumResults[oldestId] = SearchTabHelper.SearchTabCollection[oldestId].SearchResponses.Count;
                        OldResultsToCompare[oldestId] = SearchTabHelper.SearchTabCollection[oldestId].SearchResponses.ToHashSet(new SearchResponseComparer(SoulSeekState.HideLockedResultsInSearch));
#if DEBUG
                        sw.Stop();
                        MainActivity.LogDebug($"search response count: {SearchTabHelper.SearchTabCollection[oldestId].SearchResponses.Count} hashSet count: {OldResultsToCompare[oldestId].Count} time {sw.ElapsedMilliseconds} ms");
                        MainActivity.LogDebug("now searching " + oldestId);
#endif
                        //SearchTabHelper.SearchTabCollection[oldestId].CurrentlySearching = true;
                        SearchFragment.SearchAPI((new CancellationTokenSource()).Token, null, SearchTabHelper.SearchTabCollection[oldestId].LastSearchTerm, oldestId, true);
                    }
                    else
                    {
                        MainActivity.LogDebug("was already searching " + oldestId);
                    }

                }
            }
        }
    }



    //Services are natural singletons. There will be 0 or 1 instance of your service at any given time.
    [Service(Name = "com.companyname.andriodapp1.DownloadService")]
    public class DownloadForegroundService : Service
    {
        public const int NOTIF_ID = 111;
        public const string CHANNEL_ID = "my channel id";
        public const string CHANNEL_NAME = "Foreground Download Service";
        public const string FromTransferString = "FromTransfer";
        public const int NonZeroRequestCode = 7672;
        public override IBinder OnBind(Intent intent)
        {
            return null; //does not allow binding. 
        }



        public static Notification CreateNotification(Context context, String contentText)
        {
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            notifIntent.AddFlags(ActivityFlags.SingleTop);
            notifIntent.PutExtra(FromTransferString, 2);
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, NonZeroRequestCode, notifIntent, Helpers.AppendMutabilityIfApplicable((PendingIntentFlags)0, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return Helpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.download_in_progress), contentText, true, true);
        }


        public static string PluralDownloadsRemaining
        {
            get
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.downloads_remaining);
            }
        }

        public static string SingularDownloadRemaining
        {
            get
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.download_remaining);
            }
        }


        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            if (SeekerApplication.IsShuttingDown(intent))
            {
                this.StopSelf();
                return StartCommandResult.NotSticky;
            }
            SoulSeekState.DownloadKeepAliveServiceRunning = true;

            Helpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
            Notification notification = null;
            int cnt = SeekerApplication.DL_COUNT;
            if (cnt == -1)
            {
                notification = CreateNotification(this, this.GetString(Resource.String.transfers_in_progress));
            }
            else
            {
                if (cnt == 1)
                {
                    notification = CreateNotification(this, string.Format(SingularDownloadRemaining, 1));
                }
                else
                {
                    notification = CreateNotification(this, string.Format(PluralDownloadsRemaining, cnt));
                }
            }

            try
            {
                SeekerApplication.AcquireTransferLocksAndResetTimer();
            }
            catch (System.Exception e)
            {
                MainActivity.LogFirebase("timer issue: " + e.Message + e.StackTrace);
            }
            //.setContentTitle(getText(R.string.notification_title))
            //.setContentText(getText(R.string.notification_message))
            //.setSmallIcon(R.drawable.icon)
            //.setContentIntent(pendingIntent)
            //.setTicker(getText(R.string.ticker_text))
            //.build();
            StartForeground(NOTIF_ID, notification);
            //runs indefinitely until stop.

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            SoulSeekState.DownloadKeepAliveServiceRunning = false;
            SeekerApplication.ReleaseTransferLocksIfServicesComplete();
            //save once complete
            TransfersFragment.SaveTransferItems(SoulSeekState.SharedPreferences, false, 0);
            base.OnDestroy();
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }


    //Services are natural singletons. There will be 0 or 1 instance of your service at any given time.
    [Service(Name = "com.companyname.andriodapp1.UploadService")]
    public class UploadForegroundService : Service
    {
        public const int NOTIF_ID = 1112;
        public const int NonZeroRequestCode = 7671;
        public const string CHANNEL_ID = "my channel id - upload";
        public const string CHANNEL_NAME = "Foreground Upload Service";
        public const string FromTransferUploadString = "FromTransfer_UPLOAD"; //todo update for onclick...
        public override IBinder OnBind(Intent intent)
        {
            return null; //does not allow binding. 
        }



        public static Notification CreateNotification(Context context, String contentText)
        {
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            //notifIntent.
            notifIntent.AddFlags(ActivityFlags.SingleTop);
            notifIntent.PutExtra(FromTransferUploadString, 2);

            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, NonZeroRequestCode, notifIntent, Helpers.AppendMutabilityIfApplicable((PendingIntentFlags)0, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return Helpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.uploads_in_progress), contentText, true, true);
        }


        public static string PluralUploadsRemaining
        {
            get
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.uploads_remaining);
            }
        }

        public static string SingularUploadRemaining
        {
            get
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.upload_remaining);
            }
        }


        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            if (SeekerApplication.IsShuttingDown(intent))
            {
                this.StopSelf();
                return StartCommandResult.NotSticky;
            }

            SoulSeekState.UploadKeepAliveServiceRunning = true;

            Helpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
            Notification notification = null;
            int cnt = SeekerApplication.UPLOAD_COUNT;
            if (cnt == -1)
            {
                notification = CreateNotification(this, this.GetString(Resource.String.transfers_in_progress));
            }
            else
            {
                if (cnt == 1)
                {
                    notification = CreateNotification(this, string.Format(SingularUploadRemaining, 1));
                }
                else
                {
                    notification = CreateNotification(this, string.Format(PluralUploadsRemaining, cnt));
                }
            }

            try
            {
                SeekerApplication.AcquireTransferLocksAndResetTimer();
            }
            catch (System.Exception e)
            {
                MainActivity.LogFirebase("timer issue: " + e.Message + e.StackTrace);
            }
            //.setContentTitle(getText(R.string.notification_title))
            //.setContentText(getText(R.string.notification_message))
            //.setSmallIcon(R.drawable.icon)
            //.setContentIntent(pendingIntent)
            //.setTicker(getText(R.string.ticker_text))
            //.build();
            StartForeground(NOTIF_ID, notification);
            //runs indefinitely until stop.

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            SoulSeekState.UploadKeepAliveServiceRunning = false;
            SeekerApplication.ReleaseTransferLocksIfServicesComplete();

            base.OnDestroy();
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }







    //Services are natural singletons. There will be 0 or 1 instance of your service at any given time.
    [Service(Name = "com.companyname.andriodapp1.SeekerKeepAliveService")]
    public class SeekerKeepAliveService : Service
    {
        public const int NOTIF_ID = 121;
        public const string CHANNEL_ID = "seeker keep alive id";
        public const string CHANNEL_NAME = "Seeker Keep Alive Service";

        public static Android.Net.Wifi.WifiManager.WifiLock WifiKeepAlive_FullService = null;
        public static PowerManager.WakeLock CpuKeepAlive_FullService = null;


        public override IBinder OnBind(Intent intent)
        {
            return null; //does not allow binding. 
        }



        public static Notification CreateNotification(Context context)
        {
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            notifIntent.AddFlags(ActivityFlags.SingleTop);
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, 0, notifIntent, Helpers.AppendMutabilityIfApplicable((PendingIntentFlags)0, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return Helpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.seeker_running), context.GetString(Resource.String.seeker_running_content), true, true, true);
        }


        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            if (SeekerApplication.IsShuttingDown(intent))
            {
                this.StopSelf();
                return StartCommandResult.NotSticky;
            }
            MainActivity.LogInfoFirebase("keep alive service started...");
            SoulSeekState.IsStartUpServiceCurrentlyRunning = true;

            Helpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
            Notification notification = CreateNotification(this);

            try
            {
                if (CpuKeepAlive_FullService != null && !CpuKeepAlive_FullService.IsHeld)
                {
                    CpuKeepAlive_FullService.Acquire();
                    MainActivity.LogInfoFirebase("CpuKeepAlive acquire");
                }
                if (WifiKeepAlive_FullService != null && !WifiKeepAlive_FullService.IsHeld)
                {
                    WifiKeepAlive_FullService.Acquire();
                    MainActivity.LogInfoFirebase("WifiKeepAlive acquire");
                }
            }
            catch (System.Exception e)
            {
                MainActivity.LogInfoFirebase("keepalive issue: " + e.Message + e.StackTrace);
                MainActivity.LogFirebase("keepalive issue: " + e.Message + e.StackTrace);
            }
            //.setContentTitle(getText(R.string.notification_title))
            //.setContentText(getText(R.string.notification_message))
            //.setSmallIcon(R.drawable.icon)
            //.setContentIntent(pendingIntent)
            //.setTicker(getText(R.string.ticker_text))
            //.build();
            StartForeground(NOTIF_ID, notification);
            //runs indefinitely until stop.

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            SoulSeekState.IsStartUpServiceCurrentlyRunning = false;
            if (CpuKeepAlive_FullService != null && CpuKeepAlive_FullService.IsHeld)
            {
                CpuKeepAlive_FullService.Release();
                MainActivity.LogInfoFirebase("CpuKeepAlive release");
            }
            else if (CpuKeepAlive_FullService == null)
            {
                MainActivity.LogFirebase("CpuKeepAlive is null");
            }
            else if (!CpuKeepAlive_FullService.IsHeld)
            {
                MainActivity.LogFirebase("CpuKeepAlive not held");
            }
            if (WifiKeepAlive_FullService != null && WifiKeepAlive_FullService.IsHeld)
            {
                WifiKeepAlive_FullService.Release();
                MainActivity.LogInfoFirebase("WifiKeepAlive release");
            }
            else if (WifiKeepAlive_FullService == null)
            {
                MainActivity.LogFirebase("WifiKeepAlive is null");
            }
            else if (!WifiKeepAlive_FullService.IsHeld)
            {
                MainActivity.LogFirebase("WifiKeepAlive not held");
            }

            base.OnDestroy();
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }




    [Activity(Label = "CloseActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false)]
    public class CloseActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            MainActivity.LogInfoFirebase("shutting down");

            //stop all soulseek connection.
            if (SoulSeekState.SoulseekClient != null)
            {
                //closes server socket, distributed connections, and peer connections. cancels searches, stops listener.
                //this shutdown cleanly closes tcp connections. 
                // - ex. say you are downloading from QT, by closing the tcp stream, the person uploading to you will immediately 
                //       know that you are no longer there and set the status to "Aborted".
                //       compared to just killing service and "swiping up" which will uncleanly close the connection, QT will continue
                //       writing bytes with no one receiving them for several seconds.
                SoulSeekState.SoulseekClient.Dispose();
                SoulSeekState.SoulseekClient = null;
            }

            //stop the 3 potential foreground services.
            Intent intent = new Intent(this, typeof(UploadForegroundService));
            intent.SetAction(SeekerApplication.ACTION_SHUTDOWN);
            StartService(intent);

            intent = new Intent(this, typeof(DownloadForegroundService));
            intent.SetAction(SeekerApplication.ACTION_SHUTDOWN);
            StartService(intent);

            intent = new Intent(this, typeof(SeekerKeepAliveService));
            intent.SetAction(SeekerApplication.ACTION_SHUTDOWN);
            StartService(intent);

            //remove this final "closing" activity from task list.
            if ((int)Android.OS.Build.VERSION.SdkInt < 21)
            {
                this.FinishAffinity();
            }
            else
            {
                this.FinishAndRemoveTask();
            }

            //actually unload all classes, statics, etc from JVM.
            //the process will still be a "cached background process" that is fine.
            Java.Lang.JavaSystem.Exit(0);
        }
    }


    public class ThemeableActivity : AppCompatActivity
    {
        private WeakReference<ThemeableActivity> ourWeakRef;
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SeekerApplication.Activities.Remove(ourWeakRef);
            if (SeekerApplication.Activities.Count == 0)
            {
                MainActivity.LogDebug("----- On Destory ------ Last Activity ------");
                TransfersFragment.SaveTransferItems(SoulSeekState.SharedPreferences, true);
            }
            else
            {
                MainActivity.LogDebug("----- On Destory ------ NOT Last Activity ------");
                TransfersFragment.SaveTransferItems(SoulSeekState.SharedPreferences, false, 0);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SeekerApplication.SetActivityTheme(this);
            ourWeakRef = new WeakReference<ThemeableActivity>(this, false);

            SeekerApplication.Activities.Add(ourWeakRef);
            base.OnCreate(savedInstanceState);
        }

        protected override void AttachBaseContext(Context @base)
        {
            if(SoulSeekState.Language != SoulSeekState.FieldLangAuto)
            {
                var config = new Android.Content.Res.Configuration();
                config.Locale = SeekerApplication.LocaleFromString(SoulSeekState.Language);
                var baseContext = @base.CreateConfigurationContext(config);
                base.AttachBaseContext(baseContext);
            }
            else
            {
                base.AttachBaseContext(@base);
            }

        }

    }


    public class SlskLinkMenuActivity : ThemeableActivity
    {
        public const int FromSlskLinkCopyLink = 78;
        public const int FromSlskLinkBrowseAtLocation = 79;
        //public const int FromSlskLinkDownloadFolder = 80;
        public const int FromSlskLinkDownloadFiles = 81;
        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (v is TextView && Helpers.ShowSlskLinkContextMenu)
            {
                if (!Helpers.ParseSlskLinkString(Helpers.SlskLinkClickedData, out _, out _, out _, out bool isFile))
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, "Failed to parse link", ToastLength.Long).Show();
                    base.OnCreateContextMenu(menu, v, menuInfo);
                    return;
                }

                if (isFile)
                {
                    //download file
                    menu.Add(FromSlskLinkDownloadFiles, FromSlskLinkDownloadFiles, 1, Resource.String.DownloadFile);
                    //show containing folder
                }
                else
                {
                    //download folder
                    menu.Add(FromSlskLinkDownloadFiles, FromSlskLinkDownloadFiles, 1, this.GetString(Resource.String.download_folder));
                    //show folder
                }
                menu.Add(FromSlskLinkBrowseAtLocation, FromSlskLinkBrowseAtLocation, 2, this.GetString(Resource.String.browse_at_location));
                menu.Add(FromSlskLinkCopyLink, FromSlskLinkCopyLink, 3, Resource.String.CopyLink);
            }
            base.OnCreateContextMenu(menu, v, menuInfo);
        }

        public override void OnContextMenuClosed(IMenu menu)
        {
            Helpers.ShowSlskLinkContextMenu = false;
            base.OnContextMenuClosed(menu);
        }

        //public static void DownloadFilesActionEntry(Task<Directory> dirTask)
        //{
        //    DownloadFilesLogic(dirTask,null);
        //}

        public static void DownloadFilesLogic(Task<Directory> dirTask, string _uname, string thisFileOnly = null)
        {
            if (dirTask.IsFaulted)
            {
                //failed to follow link..
                if (dirTask.Exception?.InnerException?.Message != null)
                {
                    string msgToToast = string.Empty;
                    if (dirTask.Exception.InnerException.Message.ToLower().Contains("timed out"))
                    {
                        msgToToast = "Failed to Add Download - Request timed out";
                    }
                    else if (dirTask.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                    {
                        msgToToast = $"Failed to Add Download - Cannot establish connection to user {_uname}";
                    }
                    else if(dirTask.Exception.InnerException is Soulseek.UserOfflineException)
                    {
                        msgToToast = $"Failed to Add Download - User {_uname} is offline";
                    }
                    else
                    {
                        msgToToast = "Failed to follow link";
                    }
                    MainActivity.LogDebug(dirTask.Exception.InnerException.Message);
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, msgToToast, ToastLength.Short).Show();
                    });
                }
                MainActivity.LogDebug("DirectoryReceivedContAction faulted");
            }
            else
            {
                List<BrowseFragment.FullFileInfo> fullFileInfos = new List<BrowseFragment.FullFileInfo>();

                //the filenames for these files are NOT the fullname.
                //the fullname is dirTask.Result.Name "\\" f.Filename
                var directory = dirTask.Result;
                foreach (var f in directory.Files)
                {
                    string fullFilename = directory.Name + "\\" + f.Filename;
                    if (thisFileOnly == null)
                    {
                        fullFileInfos.Add(new BrowseFragment.FullFileInfo() { Depth = 1, FileName = f.Filename, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                    }
                    else
                    {
                        if (fullFilename == thisFileOnly)
                        {
                            //add
                            fullFileInfos.Add(new BrowseFragment.FullFileInfo() { Depth = 1, FileName = f.Filename, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                            break;
                        }
                    }
                }


                if (fullFileInfos.Count == 0)
                {
                    if (thisFileOnly == null)
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, "Nothing to download. Browse at this location to ensure that the file exists and is not locked.", ToastLength.Short).Show();
                    }
                    else
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, "Nothing to download. Browse at this location to ensure that the directory contains files and they are not locked.", ToastLength.Short).Show();
                    }
                    return;
                }

                BrowseFragment.DownloadListOfFiles(fullFileInfos, false, _uname);


            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case FromSlskLinkBrowseAtLocation: //Browse At Location
                    Helpers.ParseSlskLinkString(Helpers.SlskLinkClickedData, out string username, out string dirPath, out _, out _);
                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                        //((AndroidX.ViewPager.Widget.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });

                    DownloadDialog.RequestFilesApi(username, null, action, dirPath);
                    return true;
                case FromSlskLinkCopyLink:
                    Helpers.CopyTextToClipboard(SoulSeekState.ActiveActivityRef, Helpers.SlskLinkClickedData);
                    return true;
                case FromSlskLinkDownloadFiles:
                    Helpers.ParseSlskLinkString(Helpers.SlskLinkClickedData, out string _username, out string _dirPath, out string fullFilePath, out bool isFile);
                    Action<Task<Directory>> ContAction = null;
                    if (isFile)
                    {
                        ContAction = (Task<Directory> t) => { DownloadFilesLogic(t, _username, fullFilePath); };
                    }
                    else
                    {
                        ContAction = (Task<Directory> t) => { DownloadFilesLogic(t, _username, null); };
                    }
                    DownloadDialog.GetFolderContentsAPI(_username, _dirPath, false, ContAction);
                    return true;
            }
            return base.OnContextItemSelected(item);
        }
    }




    //, WindowSoftInputMode = SoftInput.StateAlwaysHidden) didnt change anything..
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Exported = true/*, WindowSoftInputMode = SoftInput.AdjustNothing*/)]
    public class MainActivity : ThemeableActivity, AndriodApp1.MainActivity.DownloadCallback, AndroidX.Core.App.ActivityCompat.IOnRequestPermissionsResultCallback, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        public static object SHARED_PREF_LOCK = new object();
        public const string logCatTag = "seeker";
        public static bool crashlyticsEnabled = true;
        public static void LogDebug(string msg)
        {
            if (SeekerApplication.LOG_DIAGNOSTICS)
            {
                //write to file
                SeekerApplication.AppendMessageToDiagFile(msg);
            }
#if ADB_LOGCAT
            log.Debug(logCatTag, msg);
#endif
        }
        public static void LogFirebase(string msg)
        {
            if (SeekerApplication.LOG_DIAGNOSTICS)
            {
                //write to file
                SeekerApplication.AppendMessageToDiagFile(msg);
            }
#if !IzzySoft
            if (crashlyticsEnabled)
            {
                Firebase.Crashlytics.FirebaseCrashlytics.Instance.RecordException(new Java.Lang.Throwable(msg));
            }
#endif
#if ADB_LOGCAT
            log.Debug(logCatTag, msg);
#endif
        }
        public static void LogInfoFirebase(string msg)
        {
            if (SeekerApplication.LOG_DIAGNOSTICS)
            {
                //write to file
                SeekerApplication.AppendMessageToDiagFile(msg);
            }
#if !IzzySoft
            if (crashlyticsEnabled)
            {
                Firebase.Crashlytics.FirebaseCrashlytics.Instance.Log(msg);
            }
#endif
#if ADB_LOGCAT
            log.Debug(logCatTag, msg);
#endif
        }



        public static void createNotificationChannel(Context c, string id, string name)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                NotificationChannel serviceChannel = new NotificationChannel(
                        id,
                        name,
                        Android.App.NotificationImportance.Low
                );
                NotificationManager manager = c.GetSystemService(Context.NotificationService) as NotificationManager;
                manager.CreateNotificationChannel(serviceChannel);
            }
        }

        public class ListenerKeyboard : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
        {   //oh so it just needs the Java.Lang.Object and then you can make it like a Java Anon Class where you only implement that one thing that you truly need
            //Since C# doesn't support anonymous classes

            private bool alreadyOpen;
            private const int defaultKeyboardHeightDP = 100;
            private int EstimatedKeyboardDP = defaultKeyboardHeightDP + (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop ? 48 : 0); //api 21
            private View parentView;
            private Android.Graphics.Rect rect = new Android.Graphics.Rect();
            public ListenerKeyboard(View _parentView)
            {
                parentView = _parentView;
            }

            public void OnGlobalLayout() //this is technically overridden and it will be called, its just weird due to the java IJavaObject, IDisposable, IJavaPeerable stuff.
            {
                int estimatedKeyboardHeight = (int)Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, EstimatedKeyboardDP, parentView.Resources.DisplayMetrics);
                parentView.GetWindowVisibleDisplayFrame(rect);//getWindowVisibleDisplayFrame(rect);
                int heightDiff = parentView.RootView.Height - (rect.Bottom - rect.Top);
                bool isShown = heightDiff >= estimatedKeyboardHeight;

                if (isShown == alreadyOpen)
                {
                    LogDebug("Keyboard state - Ignoring global layout change...");
                    return;
                }
                alreadyOpen = isShown;
                KeyBoardVisibilityChanged?.Invoke(null, isShown);
                //onKeyboardVisibilityListener.onVisibilityChanged(isShown);
            }
        }

        public static EventHandler<bool> KeyBoardVisibilityChanged;


        public void KeyboardChanged(object sender, bool isShown)
        {
            if (isShown)
            {
                SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Animate().Alpha(0f).SetDuration(250).SetListener(new BottomNavigationViewAnimationListener());
                //it will be left at 0% opacity! even when unhiding it!

                //SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Gone;
            }
            else
            {
                //SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Animate().Alpha(100).SetDuration(250).SetListener(new BottomNavigationViewAnimationListener());
                SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Visible;
                SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Animate().Alpha(1f).SetDuration(300).SetListener(null);
                //SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Visible;
            }
        }

        public class BottomNavigationViewAnimationListener : Java.Lang.Object, Android.Animation.Animator.IAnimatorListener
        {
            public void OnAnimationCancel(Animator animation)
            {

            }

            public void OnAnimationEnd(Animator animation)
            {
                SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Gone;
            }

            public void OnAnimationRepeat(Animator animation)
            {
                //throw new NotImplementedException();
            }

            public void OnAnimationStart(Animator animation)
            {
                //throw new NotImplementedException();
            }
        }


        public class BottomNavigationViewAnimationListenerVis : Java.Lang.Object, Android.Animation.Animator.IAnimatorListener
        {
            public void OnAnimationCancel(Animator animation)
            {

            }

            public void OnAnimationEnd(Animator animation)
            {
                SoulSeekState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Visible;
            }

            public void OnAnimationRepeat(Animator animation)
            {
                //throw new NotImplementedException();
            }

            public void OnAnimationStart(Animator animation)
            {
                //throw new NotImplementedException();
            }
        }

        public static event EventHandler<TransferItem> TransferAddedUINotify;

        public static event EventHandler<DownloadAddedEventArgs> DownloadAddedUINotify;

        public static void InvokeDownloadAddedUINotify(DownloadAddedEventArgs e)
        {
            DownloadAddedUINotify?.Invoke(null, e);
        }

        public static void ClearDownloadAddedEventsFromTarget(object target)
        {
            if (DownloadAddedUINotify == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in DownloadAddedUINotify.GetInvocationList())
                {
                    if (d.Target == null) //i.e. static
                    {
                        continue;
                    }
                    if (d.Target.GetType() == target.GetType())
                    {
                        DownloadAddedUINotify -= (EventHandler<DownloadAddedEventArgs>)d;
                    }
                }
            }
        }

        private void setKeyboardVisibilityListener()
        {
            //this creates problems.  we dont really need this anymore with the new "adjust nothing if edittext is at top" fix.
            //the only down side is on clicking the two filters there will be the nav bar.  but thats worth it since
            //this incorrectly hides navbar all the time and its very tough to get it back...


            //View parentView = ((ViewGroup)this.FindViewById(Android.Resource.Id.Content)).GetChildAt(0);
            //KeyBoardVisibilityChanged -= KeyboardChanged;
            //KeyBoardVisibilityChanged += KeyboardChanged;
            //parentView.ViewTreeObserver.AddOnGlobalLayoutListener(new ListenerKeyboard(parentView));

        }
        //           View parentView = ((ViewGroup)this.FindViewById(android.R.id.content)).GetChildAt(0);
        //            parentView.ViewTreeObserver.AddOnGlobalLayoutListener()

        //                getViewTreeObserver().addOnGlobalLayoutListener
        //            {

        //        private boolean alreadyOpen;
        //        private final int defaultKeyboardHeightDP = 100;
        //        private final int EstimatedKeyboardDP = defaultKeyboardHeightDP + (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP ? 48 : 0);
        //        private final Rect rect = new Rect();

        //        @Override
        //        public void onGlobalLayout()
        //        {
        //            int estimatedKeyboardHeight = (int)TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, EstimatedKeyboardDP, parentView.getResources().getDisplayMetrics());
        //            parentView.getWindowVisibleDisplayFrame(rect);
        //            int heightDiff = parentView.getRootView().getHeight() - (rect.bottom - rect.top);
        //            boolean isShown = heightDiff >= estimatedKeyboardHeight;

        //            if (isShown == alreadyOpen)
        //            {
        //                Log.i("Keyboard state", "Ignoring global layout change...");
        //                return;
        //            }
        //            alreadyOpen = isShown;
        //            onKeyboardVisibilityListener.onVisibilityChanged(isShown);
        //        }
        //    });
        //}

        //private static void NatUtility_DeviceFound(object sender, Mono.Nat.DeviceEventArgs e)
        //{
        //    LogDebug("Device Found");
        //    INatDevice device = e.Device;
        //    LogDebug(e.Device.NatProtocol.ToString());
        //    LogDebug(e.Device.GetExternalIP().ToString());
        //    e.Device.CreatePortMap(new Mapping(Protocol.Tcp, 4367, 4367, 600, "android"));
        //    //Console.WriteLine(e.Device.Get)
        //    foreach (Mapping portMap in device.GetAllMappings())
        //    {
        //        LogDebug(portMap.ToString());
        //    }

        //    // Set the TcpListener on port 13000.
        //    //Int32 port = 4234;
        //    //IPAddress localAddr = IPAddress.Parse("192.168.0.105");

        //    //// TcpListener server = new TcpListener(port);
        //    //var server = new TcpListener(localAddr, port);
        //    //server.Start();
        //    //while (true)
        //    //{
        //    //    System.Console.Write("Waiting for a connection... ");

        //    //    // Perform a blocking call to accept requests.
        //    //    // You could also use server.AcceptSocket() here.
        //    //    TcpClient client = server.AcceptTcpClient();
        //    //    System.Console.WriteLine("Connected!");
        //    //}
        //}


        //public static String getIPAddress(bool useIPv4)
        //{
        //    try
        //    {

        //        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        //        foreach (NetworkInterface intf in interfaces)
        //        {
        //            var addrs = intf.GetIPProperties().
        //            addrs.
        //            foreach (InetAddress addr in addrs)
        //            {
        //                if (!addr.isLoopbackAddress())
        //                {
        //                    String sAddr = addr.getHostAddress();
        //                    //boolean isIPv4 = InetAddressUtils.isIPv4Address(sAddr);
        //                    boolean isIPv4 = sAddr.indexOf(':') < 0;

        //                    if (useIPv4)
        //                    {
        //                        if (isIPv4)
        //                            return sAddr;
        //                    }
        //                    else
        //                    {
        //                        if (!isIPv4)
        //                        {
        //                            int delim = sAddr.indexOf('%'); // drop ip6 zone suffix
        //                            return delim < 0 ? sAddr.toUpperCase() : sAddr.substring(0, delim).toUpperCase();
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ignored) { } // for now eat exceptions
        //    return "";
        //}
        /// <summary>
        /// Presentable Filename, Uri.ToString(), length
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="directoryCount"></param>
        /// <returns></returns>
        public static Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> ParseSharedDirectoryFastDocContract(UploadDirectoryInfo newlyAddedDirectoryIfApplicable, 
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse, ref int directoryCount, out BrowseResponse br, 
            out List<Tuple<string, string>> dirMappingFriendlyNameToUri, out Dictionary<int, string> index, out List<Soulseek.Directory> allHiddenDirs)
        {
            //searchable name (just folder/song), uri.ToString (to actually get it), size (for ID purposes and to send), presentablename (to send - this is the name that is supposed to show up as the folder that the QT and nicotine clients send)
            //so the presentablename should be FolderSelected/path to rest
            //there due to the way android separates the sdcard root (or primary:) and other OS.  wherewas other OS use path separators, Android uses primary:FolderName vs say C:\Foldername.  If primary: is part of the presentable name then I will change 
            //it to primary:\Foldername similar to C:\Foldername.  I think this makes most sense of the things I have tried.
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> pairs = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
            List<Soulseek.Directory> allDirs = new List<Soulseek.Directory>();
            List<Soulseek.Directory> allLockedDirs = new List<Soulseek.Directory>();
            allHiddenDirs = new List<Soulseek.Directory>();
            dirMappingFriendlyNameToUri = new List<Tuple<string, string>>();

            //UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
            //if (UploadDirectoryManager.AreAllFailed()) //the newly added one is always good.
            //{
            //    throw new DirectoryAccessFailure("All Failed");
            //}

            HashSet<string> volNames = UploadDirectoryManager.GetInterestedVolNames();

            Dictionary<string, List<Tuple<string, int, int>>> allMediaStoreInfo = new Dictionary<string, List<Tuple<string, int, int>>>();
            PopulateAllMediaStoreInfo(allMediaStoreInfo, volNames);


            index = new Dictionary<int, string>();
            int indexNum = 0;
            var tmpUploadDirs = UploadDirectoryManager.UploadDirectories.ToList(); //avoid race conditions and enumeration modified exceptions.
            foreach (var uploadDirectoryInfo in tmpUploadDirs)
            {
                if(uploadDirectoryInfo.IsSubdir || uploadDirectoryInfo.HasError())
                {
                    continue;
                }

                DocumentFile dir = uploadDirectoryInfo.UploadDirectory;
                GetAllFolderInfo(uploadDirectoryInfo, out bool overrideCase, out string volName, out string toStrip, out string rootFolderDisplayName, out _);

                traverseDirectoryEntriesInternal(SoulSeekState.ActiveActivityRef.ContentResolver, dir.Uri, DocumentsContract.GetTreeDocumentId(dir.Uri), dir.Uri, 
                    pairs, true, volName, allDirs, allLockedDirs, allHiddenDirs, dirMappingFriendlyNameToUri, toStrip, index, dir, allMediaStoreInfo, previousFileInfoToUse, overrideCase, overrideCase ? rootFolderDisplayName : null, 
                    ref directoryCount, ref indexNum);
            }


            br = new BrowseResponse(allDirs, allLockedDirs);
            return pairs;
        }

        public static string GetPresentableName(Android.Net.Uri uri, string folderToStripForPresentableNames, string volName)
        {
            string presentableName = uri.LastPathSegment.Replace('/', '\\');

            if (folderToStripForPresentableNames == null) //this means that the primary: is in the path so at least convert it from primary: to primary:\
            {
                if (volName != null && volName.Length != presentableName.Length) //i.e. if it has something after it.. primary: should be primary: not primary:\ but primary:Alarms should be primary:\Alarms
                {
                    presentableName = presentableName.Substring(0, volName.Length) + '\\' + presentableName.Substring(volName.Length);
                }
            }
            else
            {
                presentableName = presentableName.Substring(folderToStripForPresentableNames.Length);
            }
            return presentableName;
        }

        public static void GetAllFolderInfo(UploadDirectoryInfo uploadDirectoryInfo, out bool overrideCase, out string volName, out string toStrip, out string rootFolderDisplayName, out string presentableNameToUse)
        {
            DocumentFile dir = uploadDirectoryInfo.UploadDirectory;
            Android.Net.Uri uri = dir.Uri;//Android.Net.Uri.Parse(uploadDirectoryInfo.UploadDataDirectoryUri);
            MainActivity.LogInfoFirebase("case " + uri.ToString() + " - - - - " + uri.LastPathSegment);
            //string lastPathSegment = null;
            //bool msdCase = false;
            //if (uploadDirectoryInfo.UploadDirectory != null)
            //{
            string lastPathSegment = Helpers.GetLastPathSegmentWithSpecialCaseProtection(dir, out bool msdCase);
            //}
            //else
            //{

            //    lastPathSegment = uri.LastPathSegment.Replace('/', '\\');
            //}
            toStrip = string.Empty;
            //can be reproduced with pixel emulator API 28 (android 9). the last path segment for the downloads dir is "downloads" but the last path segment for its child is "raw:/storage/emulated/0/Download/Soulseek Complete" (note it is still a content scheme, raw: is the volume)
            volName = null;
            if (msdCase)
            {
                //in this case we assume the volume is primary..
            }
            else
            {
                volName = GetVolumeName(lastPathSegment, true, out _);
                //if(volName==null)
                //{
                //    MainActivity.LogFirebase("volName is null: " + dir.Uri.ToString());
                //}
                if (lastPathSegment.Contains('\\'))
                {
                    int stripIndex = lastPathSegment.LastIndexOf('\\');
                    toStrip = lastPathSegment.Substring(0, stripIndex + 1);
                }
                else if (volName != null && lastPathSegment.Contains(volName))
                {
                    if (lastPathSegment == volName)
                    {
                        toStrip = null;
                    }
                    else
                    {
                        toStrip = volName;
                    }
                }
                else
                {
                    MainActivity.LogFirebase("contains neither: " + lastPathSegment); //Download (on Android 9 emu)
                }
            }


            rootFolderDisplayName = uploadDirectoryInfo.DisplayNameOverride;
            overrideCase = false;

            if (msdCase)
            {
                overrideCase = true;
                if (string.IsNullOrEmpty(rootFolderDisplayName))
                {
                    rootFolderDisplayName = "downloads";
                }
                volName = null; //i.e. nothing to strip out!
                toStrip = string.Empty;
            }

            if (!string.IsNullOrEmpty(rootFolderDisplayName))
            {
                overrideCase = true;
                volName = null; //i.e. nothing to strip out!
                toStrip = string.Empty;
            }

            // Forcing Override Case
            // Basically there are two ways we construct the tree. One by appending each new name to the base as we go
            // (the 'Override' Case) the other by taking current.Uri minus root.Uri to get the difference.  
            // The latter does not work because sometimes current.Uri will be say "home:" and root will be say "primary:".
            overrideCase = true;

            if (!string.IsNullOrEmpty(rootFolderDisplayName))
            {
                presentableNameToUse = rootFolderDisplayName;
            }
            else
            {
                presentableNameToUse = GetPresentableName(uri, toStrip, volName);
                rootFolderDisplayName = presentableNameToUse;
            }
        }

        public static void PopulateAllMediaStoreInfo(Dictionary<string, List<Tuple<string, int, int>>> allMediaStoreInfo, HashSet<string> volumeNamesOfInterest)
        {

            bool hasAnyInfo = HasMediaStoreDurationColumn();
            if (hasAnyInfo)
            {
                bool hasBitRate = HasMediaStoreBitRateColumn();
                string[] selectionColumns = null;
                if (hasBitRate)
                {
                    selectionColumns = new string[] {
                        Android.Provider.MediaStore.IMediaColumns.Size,
                        Android.Provider.MediaStore.IMediaColumns.DisplayName,

                        Android.Provider.MediaStore.IMediaColumns.Data, //disambiguator if applicable
                                    Android.Provider.MediaStore.IMediaColumns.Duration,
                                    Android.Provider.MediaStore.IMediaColumns.Bitrate };
                }
                else //only has duration
                {
                    selectionColumns = new string[] {
                        Android.Provider.MediaStore.IMediaColumns.Size,
                        Android.Provider.MediaStore.IMediaColumns.DisplayName,

                        Android.Provider.MediaStore.IMediaColumns.Data, //disambiguator if applicable
                                    Android.Provider.MediaStore.IMediaColumns.Duration };
                }


                foreach(var chosenVolume in volumeNamesOfInterest)
                {
                    Android.Net.Uri mediaStoreUri = null;
                    if (!string.IsNullOrEmpty(chosenVolume))
                    {
                        mediaStoreUri = MediaStore.Audio.Media.GetContentUri(chosenVolume);
                    }
                    else
                    {
                        mediaStoreUri = MediaStore.Audio.Media.ExternalContentUri;
                    }

                    //metadata content resolver info
                    Android.Database.ICursor mediaStoreInfo = null;
                    try
                    {
                        mediaStoreInfo = SoulSeekState.ActiveActivityRef.ContentResolver.Query(mediaStoreUri, selectionColumns,
                            null, null, null);
                        while (mediaStoreInfo.MoveToNext())
                        {
                            string key = mediaStoreInfo.GetInt(0) + mediaStoreInfo.GetString(1);
                            if (!allMediaStoreInfo.ContainsKey(key))
                            {
                                var list = new List<Tuple<string, int, int>>();
                                list.Add(new Tuple<string, int, int>(mediaStoreInfo.GetString(2), mediaStoreInfo.GetInt(3), hasBitRate ? mediaStoreInfo.GetInt(4) : -1));
                                allMediaStoreInfo.Add(key, list);
                            }
                            else
                            {
                                allMediaStoreInfo[key].Add(new Tuple<string, int, int>(mediaStoreInfo.GetString(2), mediaStoreInfo.GetInt(3), hasBitRate ? mediaStoreInfo.GetInt(4) : -1));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MainActivity.LogFirebase("pre get all mediaStoreInfo: " + e.Message + e.StackTrace);
                    }
                    finally
                    {
                        if (mediaStoreInfo != null)
                        {
                            mediaStoreInfo.Close();
                        }
                    }
                }
            }
        }

        public class DirectoryAccessFailure : System.Exception
        {
            public DirectoryAccessFailure(string msg) : base(msg)
            {

            }
        }


        public static Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> ParseSharedDirectoryLegacy(
            UploadDirectoryInfo newlyAddedDirectoryIfApplicable, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse, 
            ref int directoryCount, out BrowseResponse br, out List<Tuple<string, string>> dirMappingFriendlyNameToUri, out Dictionary<int, string> index, out List<Soulseek.Directory> allHiddenDirs)
        {
            //searchable name (just folder/song), uri.ToString (to actually get it), size (for ID purposes and to send), presentablename (to send - this is the name that is supposed to show up as the folder that the QT and nicotine clients send)
            //so the presentablename should be FolderSelected/path to rest
            //there due to the way android separates the sdcard root (or primary:) and other OS.  wherewas other OS use path separators, Android uses primary:FolderName vs say C:\Foldername.  If primary: is part of the presentable name then I will change 
            //it to primary:\Foldername similar to C:\Foldername.  I think this makes most sense of the things I have tried.
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> pairs = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
            List<Soulseek.Directory> allDirs = new List<Soulseek.Directory>();
            List<Soulseek.Directory> allLockedDirs = new List<Soulseek.Directory>();
            allHiddenDirs = new List<Soulseek.Directory>();


            //UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
            //if(UploadDirectoryManager.AreAllFailed())
            //{
            //    throw new DirectoryAccessFailure("All Failed");
            //}

            dirMappingFriendlyNameToUri = new List<Tuple<string, string>>();
            index = new Dictionary<int, string>();
            int indexNum = 0;


            //string lastPathSegment = dir.Uri.Path.Replace('/', '\\');
            //string toStrip = string.Empty;
            //if (lastPathSegment.Contains('\\'))
            //{
            //    int stripIndex = lastPathSegment.LastIndexOf('\\');
            //    toStrip = lastPathSegment.Substring(0, stripIndex + 1);
            //}

            var tmpUploadDirs = UploadDirectoryManager.UploadDirectories.ToList(); //avoid race conditions and enumeration modified exceptions.
            foreach (var uploadDirectoryInfo in tmpUploadDirs)
            {
                if (uploadDirectoryInfo.IsSubdir || uploadDirectoryInfo.HasError())
                {
                    continue;
                }

                DocumentFile dir = uploadDirectoryInfo.UploadDirectory;
                GetAllFolderInfo(uploadDirectoryInfo, out bool overrideCase, out string volName, out string toStrip, out string rootFolderDisplayName, out _);

                traverseDirectoryEntriesLegacy(dir, pairs, true, allDirs, allLockedDirs, 
                    allHiddenDirs, dirMappingFriendlyNameToUri, toStrip, index, 
                    previousFileInfoToUse, overrideCase, overrideCase ? rootFolderDisplayName : null, 
                    ref directoryCount, ref indexNum);
            }

            br = new BrowseResponse(allDirs, allLockedDirs);
            return pairs;
        }


        public static string GetVolumeName(string lastPathSegment, bool alwaysReturn, out bool entireString)
        {
            entireString = false;
            //if the first part of the path has a colon in it, then strip it.
            int endOfFirstPart = lastPathSegment.IndexOf('\\');
            if (endOfFirstPart == -1)
            {
                endOfFirstPart = lastPathSegment.Length;
            }
            int volumeIndex = lastPathSegment.Substring(0, endOfFirstPart).IndexOf(':');
            if (volumeIndex == -1)
            {
                return null;
            }
            else
            {
                string volumeName = lastPathSegment.Substring(0, volumeIndex + 1);
                if (volumeName.Length == lastPathSegment.Length)
                {   //special case where root is primary:.  in this case we return null which gets treated as "dont strip out anything"
                    entireString = true;
                    if (alwaysReturn)
                    {
                        return volumeName;
                    }
                    return null;
                }
                else
                {
                    return volumeName;
                }
            }
        }


        private void traverseToGetDirectories(DocumentFile dir, List<Android.Net.Uri> dirUris)
        {
            if (dir.IsDirectory)
            {
                DocumentFile[] files = dir.ListFiles(); //doesnt need to be sorted
                for (int i = 0; i < files.Length; ++i)
                {
                    DocumentFile file = files[i];
                    if (file.IsDirectory)
                    {
                        dirUris.Add(file.Uri);
                        traverseToGetDirectories(file, dirUris);
                    }
                }
            }
        }

        private List<string> GetRootDirs(DocumentFile dir)
        {
            List<string> dirUris = new List<string>();
            DocumentFile[] files = dir.ListFiles(); //doesnt need to be sorted
            for (int i = 0; i < files.Length; ++i)
            {
                DocumentFile file = files[i];
                if (file.IsDirectory)
                {
                    dirUris.Add(file.Uri.ToString());
                }
            }
            return dirUris;
        }

        private static Soulseek.Directory SlskDirFromUri(ContentResolver contentResolver, Android.Net.Uri rootUri, Android.Net.Uri dirUri, string dirToStrip, bool diagFromDirectoryResolver, string volumePath)
        {


            string directoryPath = dirUri.LastPathSegment; //on the emulator this is /tree/downloads/document/docwonlowds but the dirToStrip is uppercase Downloads
            directoryPath = directoryPath.Replace("/", @"\");
            //try
            //{
            //    directoryPath = directoryPath.Substring(directoryPath.ToLower().IndexOf(dirToStrip.ToLower()));
            //    directoryPath = directoryPath.Replace("/", @"\"); //probably strip out the root shared dir...
            //}
            //catch(Exception e)
            //{
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: False\tree\msd:824\document\msd:825MusicStartIndex cannot be less than zero.
            //    //its possible for dirToStrip to be null
            //    //True\tree\0000-0000:Musica iTunes\document\0000-0000:Musica iTunesObject reference not set to an instance of an object 
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: True\tree\3061-6232:Musica\document\3061-6232:MusicaObject reference not set to an instance of an object  at AndriodApp1.MainActivity.SlskDirFromDocumentFile (AndroidX.DocumentFile.Provider.DocumentFile dirFile, System.String dirToStrip) [0x00024] in <778faaf2e13641b38ae2700aacc789af>:0 
            //    LogFirebase("directoryPath: " + (dirToStrip==null).ToString() + directoryPath + " from directory resolver: "+ diagFromDirectoryResolver+" toStrip: " + dirToStrip + e.Message + e.StackTrace);
            //}
            //friendlyDirNameToUriMapping.Add(new Tuple<string, string>(directoryPath, dirFile.Uri.ToString()));
            //strip out the shared root dir
            //directoryPath.Substring(directoryPath.IndexOf(dir.Name))
            Android.Net.Uri listChildrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(rootUri, DocumentsContract.GetDocumentId(dirUri));
            Android.Database.ICursor c = contentResolver.Query(listChildrenUri, new String[] { Document.ColumnDocumentId, Document.ColumnDisplayName, Document.ColumnMimeType, Document.ColumnSize }, null, null, null);
            List<Soulseek.File> files = new List<Soulseek.File>();
            try
            {
                while (c.MoveToNext())
                {
                    string docId = c.GetString(0);
                    string name = c.GetString(1);
                    string mime = c.GetString(2);
                    long size = c.GetLong(3);
                    var childUri = DocumentsContract.BuildDocumentUri(rootUri.Authority, docId);
                    //MainActivity.LogDebug("docId: " + docId + ", name: " + name + ", mime: " + mime);
                    if (isDirectory(mime))
                    {
                    }
                    else
                    {

                        string fname = Helpers.GetFileNameFromFile(childUri.Path.Replace("/", @"\"));
                        string folderName = Helpers.GetFolderNameFromFile(childUri.Path.Replace("/", @"\"));
                        string searchableName = /*folderName + @"\" + */fname; //for the brose response should only be the filename!!! 
                                                                               //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                                                                               //bc we get a path that is:
                                                                               //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\(2009.09.23) Sufjan Stevens - Live from Castaways\\09 Between Songs 4.mp3"
                                                                               //not quite a full URI but it does add quite a bit..

                        //if (searchableName.Length > 7 && searchableName.Substring(0, 8).ToLower() == "primary:")
                        //{
                        //    searchableName = searchableName.Substring(8);
                        //}
                        var slskFile = new Soulseek.File(1, searchableName.Replace("/", @"\"), size, System.IO.Path.GetExtension(childUri.Path));
                        files.Add(slskFile);
                    }
                }
            }
            catch (Exception e)
            {
                LogDebug("Parse error with " + dirUri.Path + e.Message + e.StackTrace);
                LogFirebase("Parse error with " + dirUri.Path + e.Message + e.StackTrace);
            }
            finally
            {
                closeQuietly(c);
            }
            Helpers.SortSlskDirFiles(files); //otherwise our browse response files will be way out of order

            if (volumePath != null)
            {
                if (directoryPath.Substring(0, volumePath.Length) == volumePath)
                {
                    //if (directoryPath.Length != volumePath.Length)
                    //{
                    directoryPath = directoryPath.Substring(volumePath.Length);
                    //}
                }
            }

            var slskDir = new Soulseek.Directory(directoryPath, files);
            return slskDir;
        }




        /// <summary>
        /// We only use this in Contents Response Resolver.
        /// </summary>
        /// <param name="dirFile"></param>
        /// <param name="dirToStrip"></param>
        /// <param name="diagFromDirectoryResolver"></param>
        /// <param name="volumePath"></param>
        /// <returns></returns>
        private static Soulseek.Directory SlskDirFromDocumentFile(DocumentFile dirFile, bool diagFromDirectoryResolver, string volumePath)
        {
            string directoryPath = dirFile.Uri.LastPathSegment; //on the emulator this is /tree/downloads/document/docwonlowds but the dirToStrip is uppercase Downloads
            directoryPath = directoryPath.Replace("/", @"\");
            //try
            //{
            //    directoryPath = directoryPath.Substring(directoryPath.ToLower().IndexOf(dirToStrip.ToLower()));
            //    directoryPath = directoryPath.Replace("/", @"\"); //probably strip out the root shared dir...
            //}
            //catch(Exception e)
            //{
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: False\tree\msd:824\document\msd:825MusicStartIndex cannot be less than zero.
            //    //its possible for dirToStrip to be null
            //    //True\tree\0000-0000:Musica iTunes\document\0000-0000:Musica iTunesObject reference not set to an instance of an object 
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: True\tree\3061-6232:Musica\document\3061-6232:MusicaObject reference not set to an instance of an object  at AndriodApp1.MainActivity.SlskDirFromDocumentFile (AndroidX.DocumentFile.Provider.DocumentFile dirFile, System.String dirToStrip) [0x00024] in <778faaf2e13641b38ae2700aacc789af>:0 
            //    LogFirebase("directoryPath: " + (dirToStrip==null).ToString() + directoryPath + " from directory resolver: "+ diagFromDirectoryResolver+" toStrip: " + dirToStrip + e.Message + e.StackTrace);
            //}
            //friendlyDirNameToUriMapping.Add(new Tuple<string, string>(directoryPath, dirFile.Uri.ToString()));
            //strip out the shared root dir
            //directoryPath.Substring(directoryPath.IndexOf(dir.Name))

            List<Soulseek.File> files = new List<Soulseek.File>();
            foreach (DocumentFile f in dirFile.ListFiles())
            {
                if (f.IsDirectory)
                {
                    continue;
                }
                try
                {
                    string fname = null;
                    string searchableName = null;

                    if (dirFile.Uri.Authority == "com.android.providers.downloads.documents" && !f.Uri.Path.Contains(dirFile.Uri.Path))
                    {
                        //msd, msf case
                        fname = f.Name;
                        searchableName = /*folderName + @"\" + */fname; //for the brose response should only be the filename!!! 
                    }
                    else
                    {
                        fname = Helpers.GetFileNameFromFile(f.Uri.Path.Replace("/", @"\"));
                        searchableName = /*folderName + @"\" + */fname; //for the brose response should only be the filename!!! 
                    }
                    //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                    //bc we get a path that is:
                    //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\(2009.09.23) Sufjan Stevens - Live from Castaways\\09 Between Songs 4.mp3"
                    //not quite a full URI but it does add quite a bit..

                    //{
                    //    searchableName = searchableName.Substring(8);
                    //}
                    var slskFile = new Soulseek.File(1, searchableName.Replace("/", @"\"), f.Length(), System.IO.Path.GetExtension(f.Uri.Path));
                    files.Add(slskFile);
                }
                catch (Exception e)
                {
                    LogDebug("Parse error with " + f.Uri.Path + e.Message + e.StackTrace);
                    LogFirebase("Parse error with " + f.Uri.Path + e.Message + e.StackTrace);
                }

            }
            Helpers.SortSlskDirFiles(files); //otherwise our browse response files will be way out of order

            if (volumePath != null)
            {
                if (directoryPath.Substring(0, volumePath.Length) == volumePath)
                {
                    //if(directoryPath.Length != volumePath.Length)
                    //{
                    directoryPath = directoryPath.Substring(volumePath.Length);
                    //}
                }
            }

            var slskDir = new Soulseek.Directory(directoryPath, files);
            return slskDir;
        }


        public class CachedParseResults
        {
            public Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> keys = null;
            public int direcotryCount = -1;
            public BrowseResponse browseResponse = null;
            public List<Soulseek.Directory> browseResponseHiddenPortion = null;
            public List<Tuple<string, string>> friendlyDirNameToUriMapping = null;
            public Dictionary<string, List<int>> tokenIndex = null;
            public Dictionary<int, string> helperIndex = null;
            public int nonHiddenFileCount = -1;
        }

        public static void ClearParsedCacheResults()
        {
            try
            {
                lock (SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutString(SoulSeekState.M_CACHE_stringUriPairs, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_browseResponse, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_friendlyDirNameToUriMapping, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_auxDupList, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_stringUriPairs_v2, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_stringUriPairs_v3, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_browseResponse_v2, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_friendlyDirNameToUriMapping_v2, string.Empty);
                    editor.PutString(SoulSeekState.M_CACHE_tokenIndex_v2, string.Empty);
                    editor.PutInt(SoulSeekState.M_CACHE_nonHiddenFileCount_v3, 0);
                    editor.PutString(SoulSeekState.M_CACHE_intHelperIndex_v2, string.Empty);
                    editor.Commit();
                }
            }
            catch (Exception e)
            {
                LogDebug("ClearParsedCacheResults " + e.Message + e.StackTrace);
                LogFirebase("ClearParsedCacheResults " + e.Message + e.StackTrace);
            }
        }

        public static CachedParseResults GetCachedParseResults()
        {


            bool convertFrom2to3 = false;

            string s_stringUriPairs = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_stringUriPairs_v3, string.Empty);
            if(s_stringUriPairs==string.Empty)
            {
                s_stringUriPairs = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_stringUriPairs_v2, string.Empty);
                convertFrom2to3 = true;
            }

            string s_BrowseResponse = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_browseResponse_v2, string.Empty);
            string s_FriendlyDirNameMapping = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_friendlyDirNameToUriMapping_v2, string.Empty);
            string s_intHelperIndex = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_intHelperIndex_v2, string.Empty);
            int nonHiddenFileCount = SoulSeekState.SharedPreferences.GetInt(SoulSeekState.M_CACHE_nonHiddenFileCount_v3, -1);
            string s_tokenIndex = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_tokenIndex_v2, string.Empty);
            string s_BrowseResponse_hiddenPortion = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_browseResponse_hidden_portion, string.Empty); //this one can be empty.
            if (s_intHelperIndex == string.Empty || s_tokenIndex == string.Empty || s_stringUriPairs == string.Empty || s_BrowseResponse == string.Empty || s_FriendlyDirNameMapping == string.Empty)
            {
                return null;
            }
            else
            {
                //deserialize..
                try
                {
                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    byte[] b_stringUriPairs = Convert.FromBase64String(s_stringUriPairs);
                    byte[] b_BrowseResponse = Convert.FromBase64String(s_BrowseResponse);
                    byte[] b_FriendlyDirNameMapping = Convert.FromBase64String(s_FriendlyDirNameMapping);
                    byte[] b_intHelperIndex = Convert.FromBase64String(s_intHelperIndex);
                    byte[] b_tokenIndex = Convert.FromBase64String(s_tokenIndex);
                    
                    using (System.IO.MemoryStream m_stringUriPairs = new System.IO.MemoryStream(b_stringUriPairs))
                    using (System.IO.MemoryStream m_BrowseResponse = new System.IO.MemoryStream(b_BrowseResponse))
                    using (System.IO.MemoryStream m_FriendlyDirNameMapping = new System.IO.MemoryStream(b_FriendlyDirNameMapping))
                    using (System.IO.MemoryStream m_intHelperIndex = new System.IO.MemoryStream(b_intHelperIndex))
                    
                    using (System.IO.MemoryStream m_tokenIndex = new System.IO.MemoryStream(b_tokenIndex))
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        CachedParseResults cachedParseResults = new CachedParseResults();

                        if(convertFrom2to3)
                        {
                            MainActivity.LogDebug("convert from v2 to v3");
                            var oldKeys = binaryFormatter.Deserialize(m_stringUriPairs) as Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>>>;
                            var newKeys = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
                            if(oldKeys!=null)
                            {
                                foreach (var oldkeyvaluepair in oldKeys)
                                {
                                    newKeys.Add(oldkeyvaluepair.Key, new Tuple<long, string, Tuple<int, int, int, int>, bool, bool>(oldkeyvaluepair.Value.Item1, oldkeyvaluepair.Value.Item2, oldkeyvaluepair.Value.Item3, false, false));
                                }
                            }
                            lock (SHARED_PREF_LOCK)
                            {
                                var editor = SoulSeekState.SharedPreferences.Edit();
                                editor.PutString(SoulSeekState.M_CACHE_stringUriPairs_v2, string.Empty);
                                using (System.IO.MemoryStream bstringUrimemoryStreamv3 = new System.IO.MemoryStream())
                                {
                                    BinaryFormatter formatter = new BinaryFormatter();
                                    formatter.Serialize(bstringUrimemoryStreamv3, newKeys);
                                    string stringUrimemoryStreamv3 = Convert.ToBase64String(bstringUrimemoryStreamv3.ToArray());
                                    editor.PutString(SoulSeekState.M_CACHE_stringUriPairs_v3, stringUrimemoryStreamv3);
                                    editor.Commit();
                                }
                            }
                            cachedParseResults.keys = newKeys;
                        }
                        else
                        {
                            MainActivity.LogDebug("v3");
                            cachedParseResults.keys = binaryFormatter.Deserialize(m_stringUriPairs) as Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>;
                        }

                        
                        cachedParseResults.browseResponse = binaryFormatter.Deserialize(m_BrowseResponse) as BrowseResponse;


                        if(!string.IsNullOrEmpty(s_BrowseResponse_hiddenPortion))
                        {
                            byte[] b_BrowseResponse_hiddenPortion = Convert.FromBase64String(s_BrowseResponse_hiddenPortion);
                            using (System.IO.MemoryStream m_BrowseResponse_hiddenPortion = new System.IO.MemoryStream(b_BrowseResponse_hiddenPortion))
                            {
                                cachedParseResults.browseResponseHiddenPortion = binaryFormatter.Deserialize(m_BrowseResponse_hiddenPortion) as List<Soulseek.Directory>;
                            }
                        }
                        else
                        {
                            cachedParseResults.browseResponseHiddenPortion = null;
                        }

                        
                        cachedParseResults.friendlyDirNameToUriMapping = binaryFormatter.Deserialize(m_FriendlyDirNameMapping) as List<Tuple<string, string>>;
                        cachedParseResults.direcotryCount = cachedParseResults.browseResponse.DirectoryCount;
                        cachedParseResults.helperIndex = binaryFormatter.Deserialize(m_intHelperIndex) as Dictionary<int, string>;
                        cachedParseResults.tokenIndex = binaryFormatter.Deserialize(m_tokenIndex) as Dictionary<string, List<int>>;
                        cachedParseResults.nonHiddenFileCount = nonHiddenFileCount;

                        if (cachedParseResults.keys == null || cachedParseResults.browseResponse == null || cachedParseResults.friendlyDirNameToUriMapping == null || cachedParseResults.helperIndex == null || cachedParseResults.tokenIndex == null)
                        {
                            return null;
                        }

                        sw.Stop();
                        MainActivity.LogDebug("time to deserialize all sharing helpers: " + sw.ElapsedMilliseconds);

                        return cachedParseResults;
                    }

                }
                catch (Exception e)
                {
                    LogDebug("error deserializing" + e.Message + e.StackTrace);
                    LogFirebase("error deserializing" + e.Message + e.StackTrace);
                    return null;
                }
            }
        }

        //Pretty much all clients send "attributes" or limited metadata.
        // if lossless - they send duration, bit rate, bit depth, and sample rate
        // if lossy - they send duration and bit rate.

        //notes:
        // for lossless - bit rate = sample rate * bit depth * num channels
        //                1411.2 kpbs = 44.1kHz * 16 * 2
        //  --if the formula is required note that typically sample rate is in (44.1, 48, 88.2, 96) with the last too being very rare (never seen it).
        //      and bit depth in (16, 24, 32) with 16 most common, sometimes 24, never seen 32.  
        // for both lossy and lossless - determining bit rate from file size and duration is a bit too imprecise.  
        //      for mp3 320kps cbr one will get 320.3, 314, 315, etc.

        //for the pre-indexed media store (note: its possible for one to revoke the photos&media permission and for seeker to work right in all places by querying mediastore)
        //  api 29+ we have duration
        //  api 30+ we have bit rate
        //  api 31+ (Android 12) we have sample rate and bit depth

        //for the built in media retreiver (which requires actually reading the file) we have duration, bit rate, with sample rate and bit depth for api31+

        //the library tag lib sharp can get us everything, tho it is 1 MB extra.


        private static bool HasMediaStoreDurationColumn()
        {
            return (int)Android.OS.Build.VERSION.SdkInt >= 29;
        }

        private static bool HasMediaStoreBitRateColumn()
        {
            return (int)Android.OS.Build.VERSION.SdkInt >= 30;
        }

        private static bool HasMediaStoreSampleRateBitDepthColumn()
        {
            return (int)Android.OS.Build.VERSION.SdkInt >= 31;
        }

        private static bool HasMediaRetreiverSampleRateBitDepth()
        {
            return (int)Android.OS.Build.VERSION.SdkInt >= 31;
        }

        private static bool IsUncompressed(string name)
        {
            string ext = System.IO.Path.GetExtension(name);
            switch (ext)
            {
                case ".wav":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsLossless(string name)
        {
            string ext = System.IO.Path.GetExtension(name);
            switch (ext)
            {
                case ".ape":
                case ".flac":
                case ".wav":
                case ".alac":
                case ".aiff":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSupportedAudio(string name)
        {
            string ext = System.IO.Path.GetExtension(name);
            switch (ext)
            {
                case ".ape":
                case ".flac":
                case ".wav":
                case ".alac":
                case ".aiff":
                case ".mp3":
                case ".m4a":
                case ".wma":
                case ".aac":
                case ".opus":
                case ".ogg":
                case ".oga":
                    return true;
                default:
                    return false;
            }
        }




        /// <summary>
        /// Any exceptions here get caught.  worst case, you just get no metadata...
        /// </summary>
        /// <param name="contentResolver"></param>
        /// <param name="displayName"></param>
        /// <param name="size"></param>
        /// <param name="presentableName"></param>
        /// <param name="childUri"></param>
        /// <param name="allMediaInfoDict"></param>
        /// <param name="prevInfoToUse"></param>
        /// <returns></returns>
        private static Tuple<int, int, int, int> GetAudioAttributes(ContentResolver contentResolver, string displayName, long size, string presentableName, Android.Net.Uri childUri, Dictionary<string, List<Tuple<string, int, int>>> allMediaInfoDict, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>,bool, bool>> prevInfoToUse)
        {
            try
            {
                if (prevInfoToUse != null)
                {
                    if (prevInfoToUse.ContainsKey(presentableName))
                    {
                        var tuple = prevInfoToUse[presentableName];
                        if (tuple.Item1 == size) //this is the file...
                        {
                            return tuple.Item3;
                        }
                    }
                }
                //get media attributes...
                bool supported = IsSupportedAudio(presentableName);
                if (!supported)
                {
                    return null;
                }
                bool lossless = IsLossless(presentableName);
                bool uncompressed = IsUncompressed(presentableName);
                int duration = -1;
                int bitrate = -1;
                int sampleRate = -1;
                int bitDepth = -1;
                bool useContentResolverQuery = HasMediaStoreDurationColumn();//else it has no more additional data for us..
                if (useContentResolverQuery)
                {
                    bool hasBitRate = HasMediaStoreBitRateColumn();
                    //querying it every time was slow...
                    //so now we query it all ahead of time (with 1 query request) and put it in a dict.
                    string key = size + displayName;
                    if (allMediaInfoDict.ContainsKey(key))
                    {
                        string nameToSearchFor = presentableName.Replace('\\', '/');
                        bool found = true;
                        var listInfo = allMediaInfoDict[key];
                        Tuple<string, int, int> infoItem = null;
                        if (listInfo.Count > 1)
                        {
                            found = false;
                            foreach (var item in listInfo)
                            {
                                if (item.Item1.Contains(nameToSearchFor))
                                {
                                    infoItem = item;
                                    found = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            infoItem = listInfo[0];
                        }
                        if (found)
                        {
                            duration = infoItem.Item2 / 1000; //in ms
                            if (hasBitRate)
                            {
                                bitrate = infoItem.Item3;
                            }
                        }
                    }
                }

                if ((SoulSeekState.PerformDeepMetadataSearch && (bitrate == -1 || duration == -1) && size != 0))
                {
                    try
                    {
                        Android.Media.MediaMetadataRetriever mediaMetadataRetriever = new Android.Media.MediaMetadataRetriever();
                        mediaMetadataRetriever.SetDataSource(SoulSeekState.ActiveActivityRef, childUri); //TODO: error file descriptor must not be null.
                        string? bitRateStr = mediaMetadataRetriever.ExtractMetadata(Android.Media.MetadataKey.Bitrate);
                        string? durationStr = mediaMetadataRetriever.ExtractMetadata(Android.Media.MetadataKey.Duration);
                        if (HasMediaStoreDurationColumn())
                        {
                            mediaMetadataRetriever.Close(); //added in api 29
                        }
                        else
                        {
                            mediaMetadataRetriever.Release();
                        }

                        if (bitRateStr != null)
                        {
                            bitrate = int.Parse(bitRateStr);
                        }
                        if (durationStr != null)
                        {
                            duration = int.Parse(durationStr) / 1000;
                        }
                    }
                    catch (Exception e)
                    {
                        //ape and aiff always fail with built in metadata retreiver.
                        if(System.IO.Path.GetExtension(presentableName).ToLower() == ".ape")
                        {
                            MicroTagReader.GetApeMetadata(contentResolver, childUri, out sampleRate, out bitDepth, out duration);
                        }
                        else if (System.IO.Path.GetExtension(presentableName).ToLower() == ".aiff")
                        {
                            MicroTagReader.GetAiffMetadata(contentResolver, childUri, out sampleRate, out bitDepth, out duration);
                        }

                        //if still not fixed
                        if(sampleRate==-1 || duration == -1 || bitDepth == -1)
                        {
                            MainActivity.LogFirebase("MediaMetadataRetriever: " + e.Message + e.StackTrace + " isnull" + (SoulSeekState.ActiveActivityRef == null) + childUri?.ToString());
                        }
                    }
                }

                //this is the mp3 vbr case, android meta data retriever and therefore also the mediastore cache fail
                //quite badly in this case.  they often return the min vbr bitrate of 32000.
                //if its under 128kbps then lets just double check it..
                //I did test .m4a vbr.  android meta data retriever handled it quite well.
                //on api 19 the vbr being reported at 32000 is reported as 128000.... both obviously quite incorrect...
                if (System.IO.Path.GetExtension(presentableName) == ".mp3" && (bitrate >= 0 && bitrate <= 128000) && size != 0)
                {
                    if (SoulSeekState.PerformDeepMetadataSearch)
                    {
                        MicroTagReader.GetMp3Metadata(contentResolver, childUri, duration, size, out bitrate);
                    }
                    else
                    {
                        bitrate = -1; //better to have nothing than for it to be so blatantly wrong..
                    }
                }




                if (SoulSeekState.PerformDeepMetadataSearch && System.IO.Path.GetExtension(presentableName) == ".flac" && size != 0)
                {
                    MicroTagReader.GetFlacMetadata(contentResolver, childUri, out sampleRate, out bitDepth);
                }

                //if uncompressed we can use this simple formula
                if (uncompressed)
                {
                    if (bitrate != -1)
                    {
                        //bitrate = 2 * sampleRate * depth
                        //so test pairs in order of precedence..
                        if ((bitrate) / (2 * 44100) == 16)
                        {
                            sampleRate = 44100;
                            bitDepth = 16;
                        }
                        else if ((bitrate) / (2 * 44100) == 24)
                        {
                            sampleRate = 44100;
                            bitDepth = 24;
                        }
                        else if ((bitrate) / (2 * 48000) == 16)
                        {
                            sampleRate = 48000;
                            bitDepth = 16;
                        }
                        else if ((bitrate) / (2 * 48000) == 24)
                        {
                            sampleRate = 48000;
                            bitDepth = 24;
                        }
                    }
                }
                if (duration == -1 && bitrate == -1 && bitDepth == -1 && sampleRate == -1)
                {
                    return null;
                }
                return new Tuple<int, int, int, int>(duration, (lossless || bitrate == -1) ? -1 : (bitrate / 1000), bitDepth, sampleRate); //for lossless do not send bitrate!! no other client does that!!
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("get audio attr failed: " + e.Message + e.StackTrace);
                return null;
            }
        }

        public static bool IsHiddenFolder(string presentableName)
        {
            if (IsHiddenFile(presentableName))
            {
                return true;
            }
            foreach (string hiddenDir in UploadDirectoryManager.PresentableNameHiddenDirectories)
            {
                if (presentableName == hiddenDir)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsLockedFolder(string presentableName)
        {
            if(IsLockedFile(presentableName))
            {
                return true;
            }
            foreach (string lockedDir in UploadDirectoryManager.PresentableNameLockedDirectories)
            {
                if (presentableName == lockedDir)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsLockedFile(string presentableName)
        {
            foreach(string lockedDir in UploadDirectoryManager.PresentableNameLockedDirectories)
            {
                if(presentableName.StartsWith($"{lockedDir}\\")) //no need for == bc files
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsHiddenFile(string presentableName)
        {
            foreach (string hiddenDir in UploadDirectoryManager.PresentableNameHiddenDirectories)
            {
                if (presentableName.StartsWith($"{hiddenDir}\\")) //no need for == bc files
                {
                    return true;
                }
            }
            return false;
        }


        public static void traverseDirectoryEntriesInternal(ContentResolver contentResolver, Android.Net.Uri rootUri, string parentDoc, Android.Net.Uri parentUri, 
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>,bool, bool>> pairs, bool isRootCase, string volName, List<Directory> listOfDirs, List<Directory> listOfLockedDirs, List<Directory> listOfHiddenDirs,
            List<Tuple<string, string>> dirMappingFriendlyNameToUri, string folderToStripForPresentableNames, Dictionary<int, string> index, DocumentFile rootDirCase, 
            Dictionary<string, List<Tuple<string, int, int>>> allMediaInfoDict, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse, 
            bool msdMsfOrOverrideCase, string msdMsfOrOverrideBuildParentName, ref int totalDirectoryCount, ref int indexNum)
        {
            //this should be the folder before the selected to strip away..



            Android.Net.Uri listChildrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(rootUri, parentDoc);
            //Log.d(TAG, "node uri: ", childrenUri);
            Android.Database.ICursor c = contentResolver.Query(listChildrenUri, new String[] { Document.ColumnDocumentId, Document.ColumnDisplayName, Document.ColumnMimeType, Document.ColumnSize }, null, null, null);
            //c can be null... reasons are fairly opaque - if remote exception return null. if underlying content provider is null.
            if (c == null)
            {
                //diagnostic code.

                //would a non /children uri work?
                bool nonChildrenWorks = contentResolver.Query(rootUri, new string[] { Document.ColumnSize }, null, null, null) != null;

                //would app context work?
                bool wouldActiveWork = SoulSeekState.ActiveActivityRef.ApplicationContext.ContentResolver.Query(listChildrenUri, new String[] { Document.ColumnDocumentId, Document.ColumnDisplayName, Document.ColumnMimeType, Document.ColumnSize }, null, null, null) != null;

                //would list files work?
                bool docFileLegacyWork = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, parentUri).Exists();

                MainActivity.LogFirebase("cursor is null: parentDoc" + parentDoc + " list children uri: " + listChildrenUri?.ToString() + "nonchildren: " + nonChildrenWorks + " activeContext: " + wouldActiveWork + " legacyWork: " + docFileLegacyWork);
            }

            List<Soulseek.File> files = new List<Soulseek.File>();
            try
            {
                while (c.MoveToNext())
                {
                    string docId = c.GetString(0);
                    string name = c.GetString(1);
                    string mime = c.GetString(2);
                    long size = c.GetLong(3);
                    var childUri = DocumentsContract.BuildDocumentUriUsingTree(rootUri, docId);
                    //MainActivity.LogDebug("docId: " + docId + ", name: " + name + ", mime: " + mime);
                    if (isDirectory(mime))
                    {
                        totalDirectoryCount++;
                        traverseDirectoryEntriesInternal(contentResolver, rootUri, docId, childUri, pairs, false, volName, listOfDirs, listOfLockedDirs, listOfHiddenDirs,
                            dirMappingFriendlyNameToUri, folderToStripForPresentableNames, index, null, allMediaInfoDict, previousFileInfoToUse, 
                            msdMsfOrOverrideCase, msdMsfOrOverrideCase ? msdMsfOrOverrideBuildParentName + '\\' + name : null, ref totalDirectoryCount, ref indexNum);
                    }
                    else
                    {
                        string presentableName = null;
                        if (msdMsfOrOverrideCase)
                        {
                            presentableName = msdMsfOrOverrideBuildParentName + '\\' + name;
                        }
                        else
                        {
                            presentableName = GetPresentableName(childUri, folderToStripForPresentableNames, volName);
                        }


                        string searchableName = Helpers.GetFolderNameFromFile(presentableName) + @"\" + Helpers.GetFileNameFromFile(presentableName);

                        Tuple<int, int, int, int> attributes = GetAudioAttributes(contentResolver, name, size, presentableName, childUri, allMediaInfoDict, previousFileInfoToUse);
                        if (attributes != null)
                        {
                            //MainActivity.LogDebug("fname: " + name + " attr: " + attributes.Item1 + "  " + attributes.Item2 + "  " + attributes.Item3 + "  " + attributes.Item4 + "  ");
                        }

                        pairs.Add(presentableName, new Tuple<long, string, Tuple<int, int, int, int>, bool, bool>(size, childUri.ToString(), attributes, IsLockedFile(presentableName), IsHiddenFile(presentableName))); 
                        index.Add(indexNum, presentableName); //throws on same key (the file in question ends with unicode EOT char (\u04)).
                        indexNum++;
                        if (indexNum % 50 == 0)
                        {
                            //update public status variable every so often
                            SoulSeekState.NumberParsed = indexNum;
                        }
                        //                        pairs.Add(new Tuple<string, string, long, string>(searchableName, childUri.ToString(), size, presentableName));

                        string fname = Helpers.GetFileNameFromFile(presentableName.Replace("/", @"\")); //use presentable name so that the filename will not be primary:file.mp3
                                                                                                        //for the brose response should only be the filename!!! 
                                                                                                        //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                                                                                                        //bc we get a path that is:
                                                                                                        //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\album\\09 Between Songs 4.mp3"
                                                                                                        //not quite a full URI but it does add quite a bit..

                        //if (searchableName.Length > 7 && searchableName.Substring(0, 8).ToLower() == "primary:")
                        //{
                        //    searchableName = searchableName.Substring(8);
                        //}

                        var slskFile = new Soulseek.File(1, fname, size, System.IO.Path.GetExtension(childUri.Path), SharedFileCache.GetFileAttributesFromTuple(attributes)); //soulseekQT does not show attributes in browse tab, but nicotine does.
                        files.Add(slskFile);
                    }
                }
                Helpers.SortSlskDirFiles(files);
                string lastPathSegment = null;
                if (msdMsfOrOverrideCase)
                {
                    lastPathSegment = msdMsfOrOverrideBuildParentName;
                }
                else if (isRootCase)
                {
                    lastPathSegment = Helpers.GetLastPathSegmentWithSpecialCaseProtection(rootDirCase, out _);
                }
                else
                {
                    lastPathSegment = parentUri.LastPathSegment;
                }
                string directoryPath = lastPathSegment.Replace("/", @"\");

                if(!msdMsfOrOverrideCase)
                {
                    if (folderToStripForPresentableNames == null) //this means that the primary: is in the path so at least convert it from primary: to primary:\
                    {
                        if (volName != null && volName.Length != directoryPath.Length) //i.e. if it has something after it.. primary: should be primary: not primary:\ but primary:Alarms should be primary:\Alarms
                        {
                            if(volName.Length > directoryPath.Length)
                            {
                                MainActivity.LogFirebase("volName > directoryPath" + volName + " -- " + directoryPath + " -- " + isRootCase);
                            }
                            directoryPath = directoryPath.Substring(0, volName.Length) + '\\' + directoryPath.Substring(volName.Length);
                        }
                    }
                    else
                    {
                        directoryPath = directoryPath.Substring(folderToStripForPresentableNames.Length);
                    }
                }

                var slskDir = new Soulseek.Directory(directoryPath, files);
                if(IsHiddenFolder(directoryPath))
                {
                    listOfHiddenDirs.Add(slskDir);
                }
                else if(IsLockedFolder(directoryPath))
                {
                    listOfLockedDirs.Add(slskDir);
                }
                else
                {
                    listOfDirs.Add(slskDir);
                }
                
                dirMappingFriendlyNameToUri.Add(new Tuple<string, string>(directoryPath, parentUri.ToString()));
            }
            finally
            {
                closeQuietly(c);
            }
        }


        public static void traverseDirectoryEntriesLegacy(DocumentFile parentDocFile, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> pairs, bool isRootCase, 
            List<Directory> listOfDirs, List<Directory> listOfLockedDirs, List<Directory> listOfHiddenDirs, List<Tuple<string, string>> dirMappingFriendlyNameToUri, 
            string folderToStripForPresentableNames, Dictionary<int, string> index, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse, 
            bool overrideCase, string msdMsfOrOverrideBuildParentName,
            ref int totalDirectoryCount, ref int indexNum)
        {
            //this should be the folder before the selected to strip away..
            List<Soulseek.File> files = new List<Soulseek.File>();
            foreach (var childDocFile in parentDocFile.ListFiles())
            {
                if (childDocFile.IsDirectory)
                {
                    totalDirectoryCount++;
                    traverseDirectoryEntriesLegacy(childDocFile, pairs, false, listOfDirs, listOfLockedDirs, listOfHiddenDirs, 
                        dirMappingFriendlyNameToUri, folderToStripForPresentableNames, index, previousFileInfoToUse, overrideCase,
                        overrideCase ? msdMsfOrOverrideBuildParentName + '\\' + childDocFile.Name : null, ref totalDirectoryCount, ref indexNum);
                }
                else
                {
                    //for subAPI21 last path segment is:
                    //".android_secure" so just the filename whereas Path is more similar to last part segment:
                    //"/storage/sdcard/.android_secure"
                    string presentableName = childDocFile.Uri.Path.Replace('/', '\\');
                    if(overrideCase)
                    {
                        presentableName = msdMsfOrOverrideBuildParentName + '\\' + childDocFile.Name;
                    }
                    else if (folderToStripForPresentableNames != null) //this means that the primary: is in the path so at least convert it from primary: to primary:\
                    {
                        presentableName = presentableName.Substring(folderToStripForPresentableNames.Length);
                    }

                    Tuple<int, int, int, int> attributes = GetAudioAttributes(SoulSeekState.ActiveActivityRef.ContentResolver, childDocFile.Name, childDocFile.Length(), presentableName, childDocFile.Uri, null, previousFileInfoToUse);
                    if (attributes != null)
                    {
                        //MainActivity.LogDebug("fname: " + childDocFile.Name + " attr: " + attributes.Item1 + "  " + attributes.Item2 + "  " + attributes.Item3 + "  " + attributes.Item4 + "  ");
                    }

                    pairs.Add(presentableName, new Tuple<long, string, Tuple<int, int, int, int>, bool, bool>(childDocFile.Length(), childDocFile.Uri.ToString(), attributes, IsLockedFile(presentableName), IsHiddenFile(presentableName))); //todo attributes was null here???? before
                    index.Add(indexNum, presentableName);
                    indexNum++;
                    if (indexNum % 50 == 0)
                    {
                        //update public status variable every so often
                        SoulSeekState.NumberParsed = indexNum;
                    }
                    string fname = Helpers.GetFileNameFromFile(presentableName.Replace("/", @"\")); //use presentable name so that the filename will not be primary:file.mp3
                                                                                                    //for the brose response should only be the filename!!! 
                                                                                                    //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                                                                                                    //bc we get a path that is:
                                                                                                    //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\album\\09 Between Songs 4.mp3"
                                                                                                    //not quite a full URI but it does add quite a bit..

                    //if (searchableName.Length > 7 && searchableName.Substring(0, 8).ToLower() == "primary:")
                    //{
                    //    searchableName = searchableName.Substring(8);
                    //}
                    var slskFile = new Soulseek.File(1, fname, childDocFile.Length(), System.IO.Path.GetExtension(childDocFile.Uri.Path));
                    files.Add(slskFile);
                }
            }

            Helpers.SortSlskDirFiles(files);
            string directoryPath = parentDocFile.Uri.Path.Replace("/", @"\");

            if(overrideCase)
            {
                directoryPath = msdMsfOrOverrideBuildParentName;
            }
            else if (folderToStripForPresentableNames != null)
            {
                directoryPath = directoryPath.Substring(folderToStripForPresentableNames.Length);
            }

            var slskDir = new Soulseek.Directory(directoryPath, files);
            if (IsHiddenFolder(directoryPath))
            {
                listOfHiddenDirs.Add(slskDir);
            }
            else if (IsLockedFolder(directoryPath))
            {
                listOfLockedDirs.Add(slskDir);
            }
            else
            {
                listOfDirs.Add(slskDir);
            }

            dirMappingFriendlyNameToUri.Add(new Tuple<string, string>(directoryPath, parentDocFile.Uri.ToString()));
        }



        // Util method to check if the mime type is a directory
        private static bool isDirectory(String mimeType)
        {
            return DocumentsContract.Document.MimeTypeDir.Equals(mimeType);
        }

        // Util method to close a closeable
        private static void closeQuietly(Android.Database.ICursor closeable)
        {
            if (closeable != null)
            {
                try
                {
                    closeable.Close();
                }
                catch
                {
                    // ignore exception
                }
            }
        }

        

        /// <summary>
        /// Check Cache should be false if setting a new dir.. true if on startup.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="checkCache"></param>
        public static bool InitializeDatabase(UploadDirectoryInfo newlyAddedDirectoryIfApplicable, bool checkCache, out string errorMsg)
        {
            errorMsg = string.Empty;
            bool success = false;
            try
            {
                CachedParseResults cachedParseResults = null;
                if (checkCache)
                {
                    cachedParseResults = GetCachedParseResults();
                }

                if (cachedParseResults == null)
                {
                    int directoryCount = 0;
                    System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
                    s.Start();
                    Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> stringUriPairs = null;
                    BrowseResponse browseResponse = null;
                    List<Tuple<string, string>> dirMappingFriendlyNameToUri = null;
                    List<Soulseek.Directory> hiddenDirectories = null;
                    Dictionary<int, string> index = null;


                    //optimization - if new directory is a subdir we can skip this part. !!!! but we still have things to do like make all files that start with said presentableDir to be locked / hidden. etc.

                    UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
                    if (UploadDirectoryManager.AreAllFailed())
                    {
                        throw new DirectoryAccessFailure("All Failed");
                    }
                    if (SoulSeekState.PreOpenDocumentTree() || UploadDirectoryManager.AreAnyFromLegacy())
                    {
                        stringUriPairs = ParseSharedDirectoryLegacy(null, SoulSeekState.SharedFileCache?.FullInfo, ref directoryCount, out browseResponse, out dirMappingFriendlyNameToUri, out index, out hiddenDirectories);
                    }
                    else
                    {
                        stringUriPairs = ParseSharedDirectoryFastDocContract(null, SoulSeekState.SharedFileCache?.FullInfo, ref directoryCount, out browseResponse, out dirMappingFriendlyNameToUri, out index, out hiddenDirectories);
                    }

                    int nonHiddenCountForServer = stringUriPairs.Count(pair1=>!pair1.Value.Item5);
                    MainActivity.LogDebug($"Non Hidden Count for Server: {nonHiddenCountForServer}");

                    SoulSeekState.NumberParsed = int.MaxValue; //our signal that we are finishing up...
                    s.Stop();
                    MainActivity.LogDebug(string.Format("{0} Files parsed in {1} milliseconds", stringUriPairs.Keys.Count, s.ElapsedMilliseconds));
                    s.Reset();
                    s.Start();

                    Dictionary<string, List<int>> tokenIndex = new Dictionary<string, List<int>>();
                    var reversed = index.ToDictionary(x => x.Value, x => x.Key);
                    foreach (string presentableName in stringUriPairs.Keys)
                    {
                        string searchableName = Helpers.GetFolderNameFromFile(presentableName) + " " + System.IO.Path.GetFileNameWithoutExtension(Helpers.GetFileNameFromFile(presentableName));
                        searchableName = SharedFileCache.MatchSpecialCharAgnostic(searchableName);
                        int code = reversed[presentableName];
                        foreach (string token in searchableName.ToLower().Split(null)) //null means whitespace
                        {
                            if (token == string.Empty)
                            {
                                continue;
                            }
                            if (tokenIndex.ContainsKey(token))
                            {
                                tokenIndex[token].Add(code);
                            }
                            else
                            {
                                tokenIndex[token] = new List<int>();
                                tokenIndex[token].Add(code);
                            }
                        }
                    }
                    s.Stop();

                    //foreach(string token in tokenIndex.Keys)
                    //{
                    //    MainActivity.LogDebug(token);
                    //}

                    MainActivity.LogDebug(string.Format("Token index created in {0} milliseconds", s.ElapsedMilliseconds));

                    //s.Stop();
                    //LogDebug("ParseSharedDirectory: " + s.ElapsedMilliseconds);

                    //put into cache.
                    using (System.IO.MemoryStream bResponsememoryStream = new System.IO.MemoryStream())
                    using (System.IO.MemoryStream bHiddenPortion = new System.IO.MemoryStream())
                    using (System.IO.MemoryStream stringUrimemoryStream = new System.IO.MemoryStream())
                    using (System.IO.MemoryStream friendlyDirNamememoryStream = new System.IO.MemoryStream())
                    using (System.IO.MemoryStream intIndexStream = new System.IO.MemoryStream())
                    using (System.IO.MemoryStream tokenIndexStream = new System.IO.MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(bResponsememoryStream, browseResponse);

                        MainActivity.LogDebug(string.Format("Browse Response is {0} bytes", bResponsememoryStream.Length));

                        string bResponse = Convert.ToBase64String(bResponsememoryStream.ToArray());

                        formatter.Serialize(bHiddenPortion, hiddenDirectories);
                        MainActivity.LogDebug(string.Format("Browse Response (hidden portion only) is {0} bytes", bHiddenPortion.Length));

                        string bHiddenPortionResponse = Convert.ToBase64String(bHiddenPortion.ToArray());

                        formatter.Serialize(stringUrimemoryStream, stringUriPairs);
                        string bstringUriPairs = Convert.ToBase64String(stringUrimemoryStream.ToArray());

                        MainActivity.LogDebug(string.Format("File Dictionary is {0} bytes", stringUrimemoryStream.Length));

                        formatter.Serialize(friendlyDirNamememoryStream, dirMappingFriendlyNameToUri);
                        string bfriendlyDirName = Convert.ToBase64String(friendlyDirNamememoryStream.ToArray());

                        MainActivity.LogDebug(string.Format("Directory Dictionary is {0} bytes", friendlyDirNamememoryStream.Length));

                        formatter.Serialize(intIndexStream, index);
                        string bIntIndex = Convert.ToBase64String(intIndexStream.ToArray());

                        MainActivity.LogDebug(string.Format("int (helper) index is {0} bytes", intIndexStream.Length));

                        formatter.Serialize(tokenIndexStream, tokenIndex);
                        string bTokenIndex = Convert.ToBase64String(tokenIndexStream.ToArray());

                        MainActivity.LogDebug(string.Format("token index is {0} bytes", tokenIndexStream.Length));


                        lock (SHARED_PREF_LOCK)
                        {
                            var editor = SoulSeekState.SharedPreferences.Edit();
                            editor.PutString(SoulSeekState.M_CACHE_stringUriPairs_v3, bstringUriPairs);
                            editor.PutString(SoulSeekState.M_CACHE_browseResponse_v2, bResponse);
                            editor.PutString(SoulSeekState.M_CACHE_browseResponse_hidden_portion, bHiddenPortionResponse);
                            editor.PutString(SoulSeekState.M_CACHE_friendlyDirNameToUriMapping_v2, bfriendlyDirName);
                            editor.PutString(SoulSeekState.M_CACHE_intHelperIndex_v2, bIntIndex);
                            editor.PutString(SoulSeekState.M_CACHE_tokenIndex_v2, bTokenIndex);
                            editor.PutInt(SoulSeekState.M_CACHE_nonHiddenFileCount_v3, nonHiddenCountForServer);
                            //editor.PutString(SoulSeekState.M_UploadDirectoryUri, SoulSeekState.UploadDataDirectoryUri);
                            //editor.PutBoolean(SoulSeekState.M_UploadDirectoryUriIsFromTree, SoulSeekState.UploadDataDirectoryUriIsFromTree);


                            //TODO TODO save upload dirs

                            //before this line ^ ,its possible for the saved UploadDirectoryUri and the actual browse response to be different.
                            //this is because upload data uri saves on MainActivity OnPause. and so one could set shared folder and then press home and then swipe up. never having saved uploadirectoryUri.
                            editor.Commit();

                            UploadDirectoryManager.SaveToSharedPreferences(SoulSeekState.SharedPreferences);
                        }
                    }

                    ////5 searches a second = 18,000 per hour.
                    //System.Random rand = new System.Random();
                    //List<string> searchTerms = new List<string>();
                    //for (int i = 0; i < 18000; i++)
                    //{
                    //    int a = rand.Next();
                    //    searchTerms.Add("item" + a.ToString() + " " + "item2" + a.ToString());
                    //}

                    //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    //sw.Reset();
                    //sw.Start();
                    //foreach (string search in searchTerms)
                    //{
                    //    foreach (string file in stringUriPairs.Keys)
                    //    {
                    //        if (file.Contains(search))
                    //        {
                    //            System.Console.WriteLine("true");
                    //        }
                    //    }
                    //}
                    //sw.Stop();
                    //MainActivity.LogDebug(string.Format("linear search .5 million: {0}", sw.ElapsedMilliseconds));
                    //sw.Reset();
                    //sw.Start();
                    ////5ms vs 27000ms for 100k searches over 10k files.
                    ////0ms vs 600ms for 5k searches over 2k files.

                    //foreach (string search in searchTerms)
                    //{
                    //    if (tokenIndex.ContainsKey(search))
                    //    {
                    //        System.Console.WriteLine("true");
                    //    }
                    //}

                    //sw.Stop();
                    //MainActivity.LogDebug(string.Format("term index search .5 million: {0}", sw.ElapsedMilliseconds));


                    SlskHelp.SharedFileCache sharedFileCache = new SlskHelp.SharedFileCache(stringUriPairs, directoryCount, browseResponse, dirMappingFriendlyNameToUri, tokenIndex, index, hiddenDirectories, nonHiddenCountForServer);//.Select(_=>_.Item1).ToList());
                    SharedFileCache_Refreshed(null, (sharedFileCache.DirectoryCount, nonHiddenCountForServer != -1 ? nonHiddenCountForServer : sharedFileCache.FileCount));
                    SoulSeekState.SharedFileCache = sharedFileCache;

                    //*********Profiling********* for 2252 files - 13s initial parsing, 1.9 MB total
                    //2552 Files parsed in 13,161 milliseconds  - if the phone is locked it takes twice as long
                    //Token index created in 370 milliseconds   - if the phone is locked it takes twice as long
                    //Browse Response is 379,963 bytes
                    //File Dictionary is 769,386 bytes
                    //Directory Dictionary is 137,518 bytes
                    //int(helper) index is 258,237 bytes
                    //token index is 393,354 bytes
                    //cache:
                    //time to deserialize all sharing helpers is 664 ms for 2k files...

                    //searching an hour (18,000) worth of terms
                    //linear - 22,765 ms
                    //dictionary based - 27ms

                    //*********Profiling********* for 807 files - 3s initial parsing, .66 MB total
                    //807 Files parsed in 2,935 milliseconds
                    //Token index created in 182 milliseconds
                    //Browse Response is 114,432 bytes
                    //File Dictionary is 281,610 bytes
                    //Directory Dictionary is 38,250 bytes
                    //int(helper) index is 78,589 bytes
                    //token index is 156,274 bytes

                    //searching an hour (18,000) worth of terms
                    //linear - 6,570 ms
                    //dictionary based - 22ms

                    //*********Profiling********* for 807 files -- deep metadata retreival off. (i.e. only whats indexed in MediaStore) - 
                    //*********Profiling********* for 807 files -- metadata for flac and those not in MediaStore - 12,234
                    //*********Profiling********* for 807 files -- mediaretreiver for everything.  metadata for flac and those not in MediaStore - 38,063



                }
                else
                {
                    LogDebug("Using cached results");
                    UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
                    if (UploadDirectoryManager.AreAllFailed())
                    {
                        throw new DirectoryAccessFailure("All Failed");
                    }
                    else
                    {
                        SlskHelp.SharedFileCache sharedFileCache = new SlskHelp.SharedFileCache(cachedParseResults.keys, 
                            cachedParseResults.direcotryCount, cachedParseResults.browseResponse, cachedParseResults.friendlyDirNameToUriMapping, 
                            cachedParseResults.tokenIndex, cachedParseResults.helperIndex, cachedParseResults.browseResponseHiddenPortion, 
                            cachedParseResults.nonHiddenFileCount);

                        SharedFileCache_Refreshed(null, (sharedFileCache.DirectoryCount, sharedFileCache.GetNonHiddenFileCountForServer() != -1 ? sharedFileCache.GetNonHiddenFileCountForServer() : sharedFileCache.FileCount));
                        SoulSeekState.SharedFileCache = sharedFileCache;
                    }
                }
                success = true;
                SoulSeekState.FailedShareParse = false;
                SoulSeekState.SharedFileCache.SuccessfullyInitialized = true;
            }
            catch (Exception e)
            {
                string defaultUnspecified = "Shared Folder Error - Unspecified Error";
                errorMsg = defaultUnspecified;
                if (e.GetType().FullName == "Java.Lang.SecurityException" || e is Java.Lang.SecurityException)
                {
                    errorMsg = SeekerApplication.GetString(Resource.String.PermissionsIssueShared);
                }
                success = false;
                LogDebug("Error parsing files: " + e.Message + e.StackTrace);


                if(e is DirectoryAccessFailure)
                {
                    errorMsg = "Shared Folder Error - " + UploadDirectoryManager.GetCompositeErrorString();
                }
                else
                {
                    LogFirebase("Error parsing files: " + e.Message + e.StackTrace);
                }

                if (e.Message.Contains("An item with the same key"))
                {
                    try
                    {
                        LogFirebase("Possible encoding issue: " + ShowCodePoints(e.Message.Substring(e.Message.Length - 7)));
                        errorMsg = "Path Conflict. Same Name?";
                    }
                    catch
                    {
                        //just in case
                    }
                }

                if(errorMsg == defaultUnspecified)
                {
                    MainActivity.LogFirebase("Error Parsing Files Unspecified Error" + e.Message + e.StackTrace);
                }
            }
            finally
            {
                if (!success)
                {
                    //if(newlyAddedDirectoryIfApplicable!=null)
                    //{
                    //    UploadDirectoryManager.UploadDirectories.Remove(newlyAddedDirectoryIfApplicable);
                    //    UploadDirectoryChanged?.Invoke(null, new EventArgs());
                    //}
                    //SoulSeekState.UploadDataDirectoryUri = null;
                    //SoulSeekState.UploadDataDirectoryUriIsFromTree = true;
                    SoulSeekState.FailedShareParse = true;
                    //if success if false then SoulSeekState.SharedFileCache might be null still causing a crash!
                    if (SoulSeekState.SharedFileCache != null)
                    {
                        SoulSeekState.SharedFileCache.SuccessfullyInitialized = false;
                    }
                }
            }
            return success;
            //SoulSeekState.SoulseekClient.SearchResponseDelivered += SoulseekClient_SearchResponseDelivered;
            //SoulSeekState.SoulseekClient.SearchResponseDeliveryFailed += SoulseekClient_SearchResponseDeliveryFailed;
        }

        private static string ShowCodePoints(string str)
        {
            string codePointString = string.Empty;
            foreach (char c in str)
            {
                codePointString = codePointString + ($"_{ (int)c:x4}");
            }
            return codePointString;
        }

        private void SoulseekClient_SearchResponseDeliveryFailed(object sender, SearchRequestResponseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void SoulseekClient_SearchResponseDelivered(object sender, SearchRequestResponseEventArgs e)
        {

        }

        public static void SharedFileCache_Refreshed(object sender, (int Directories, int Files) e)
        {
            if (SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                SoulSeekState.SoulseekClient.SetSharedCountsAsync(e.Directories, e.Files);
                SoulSeekState.NumberOfSharedDirectoriesIsStale = false;
            }
            else
            {
                SoulSeekState.NumberOfSharedDirectoriesIsStale = true;
            }
        }

        /// <summary>
        /// Inform server the number of files we are sharing or 0,0 if not sharing...
        /// it looks like people typically report all including locked files. lets not report hidden files though.
        /// </summary>
        public static void InformServerOfSharedFiles()
        {
            try
            {
                if (SoulSeekState.SoulseekClient != null && SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
                {
                    if (MeetsCurrentSharingConditions())
                    {
                        if(SoulSeekState.SharedFileCache != null)
                        {
                            MainActivity.LogDebug("Tell server we are sharing " + SoulSeekState.SharedFileCache.DirectoryCount + " dirs and " + SoulSeekState.SharedFileCache.GetNonHiddenFileCountForServer() + " files");
                            SoulSeekState.SoulseekClient.SetSharedCountsAsync(SoulSeekState.SharedFileCache.DirectoryCount, 
                                SoulSeekState.SharedFileCache.GetNonHiddenFileCountForServer() != -1 ? SoulSeekState.SharedFileCache.GetNonHiddenFileCountForServer() : SoulSeekState.SharedFileCache.FileCount);
                        }
                        else
                        {
                            MainActivity.LogDebug("We would tell server but we are not successfully set up yet.");
                        }
                    }
                    else
                    {
                        MainActivity.LogDebug("Tell server we are sharing 0 dirs and 0 files");
                        SoulSeekState.SoulseekClient.SetSharedCountsAsync(0, 0);
                    }
                    SoulSeekState.NumberOfSharedDirectoriesIsStale = false;
                }
                else
                {
                    if (MeetsCurrentSharingConditions())
                    {
                        if(SoulSeekState.SharedFileCache!=null)
                        {
                            MainActivity.LogDebug("We need to Tell server we are sharing " + SoulSeekState.SharedFileCache.DirectoryCount + " dirs and " + SoulSeekState.SharedFileCache.GetNonHiddenFileCountForServer() + " files on next log in");
                        }
                        else
                        {
                            MainActivity.LogDebug("we meet sharing conditions but our shared file cache is not successfully set up");
                        }
                    }
                    else
                    {
                        MainActivity.LogDebug("We need to Tell server we are sharing 0 dirs and 0 files on next log in");
                    }
                    SoulSeekState.NumberOfSharedDirectoriesIsStale = true;
                }
            }
            catch (Exception e)
            {
                MainActivity.LogDebug("Failed to InformServerOfSharedFiles " + e.Message + e.StackTrace);
                MainActivity.LogFirebase("Failed to InformServerOfSharedFiles " + e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// returns a list of searchable names (final dir + filename) for other's searches, and then the URI so android can actually get that file.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public void traverseDocumentFile(DocumentFile dir, List<Tuple<string, string, long, string>> pairs, Dictionary<string, List<Tuple<string, string, long>>> auxilaryDuplicatesList, bool isRootCase, string volName, ref int directoryCount)
        {
            if (dir.Exists())
            {
                DocumentFile[] files = dir.ListFiles();
                for (int i = 0; i < files.Length; ++i)
                {
                    DocumentFile file = files[i];
                    if (file.IsDirectory)
                    {
                        directoryCount++;
                        traverseDocumentFile(file, pairs, auxilaryDuplicatesList, false, volName, ref directoryCount);
                    }
                    else
                    {
                        // do something here with the file
                        //LogDebug(file.Uri.ToString()); //encoded string representation //content://com.android.externalstorage.documents/tree/primary%3ASoulseek%20Complete/document/primary%3ASoulseek%20Complete%2F41-60%2F14-B-181%20x.mp3
                        //LogDebug(file.Uri.Path.ToString()); //gets decoded path // /tree/primary:Soulseek Complete/document/primary:Soulseek Complete/41-60/14-B-181x.mp3
                        //LogDebug(Android.Net.Uri.Decode(file.Uri.ToString())); // content://com.android.externalstorage.documents/tree/primary:Soulseek Complete/document/primary:Soulseek Complete/41-60/14-B-181x.mp3
                        //LogDebug(file.Uri.EncodedPath);  // /tree/primary%3ASoulseek%20Complete/document/primary%3ASoulseek%20Complete%2F41-60%2F14-B-181%20x.mp3 
                        //LogDebug(file.Uri.LastPathSegment); // primary:Soulseek Complete/41-60/14-B-181 Welcome To New York-Taylor Swift.mp3

                        string fullPath = file.Uri.Path.ToString().Replace('/', '\\');
                        string presentableName = file.Uri.LastPathSegment.Replace('/', '\\');

                        string searchableName = Helpers.GetFolderNameFromFile(fullPath) + @"\" + Helpers.GetFileNameFromFile(fullPath);
                        if (isRootCase && (volName != null))
                        {
                            if (searchableName.Substring(0, volName.Length) == volName)
                            {
                                if (searchableName.Length != volName.Length) //i.e. if its just "primary:"
                                {
                                    searchableName = searchableName.Substring(volName.Length);
                                }
                            }
                        }
                        pairs.Add(new Tuple<string, string, long, string>(searchableName, file.Uri.ToString(), file.Length(), presentableName));
                    }
                }
            }
        }

        public void traverseFile(Java.IO.File dir)
        {
            if (dir.Exists())
            {
                Java.IO.File[] files = dir.ListFiles();
                for (int i = 0; i < files.Length; ++i)
                {
                    Java.IO.File file = files[i];
                    if (file.IsDirectory)
                    {
                        traverseFile(file);
                    }
                    else
                    {
                        // do something here with the file
                        LogDebug(file.Path);
                        LogDebug(file.AbsolutePath);
                        LogDebug(file.CanonicalPath);
                    }
                }
            }
        }


        public const string SETTINGS_INTENT = "com.example.seeker.SETTINGS";
        public const int SETTINGS_EXTERNAL = 0x430;
        public const int DEFAULT_SEARCH_RESULTS = 50;
        private const int WRITE_EXTERNAL = 9999;
        private const int NEW_WRITE_EXTERNAL = 0x428;
        private const int MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL = 0x429;
        private const int NEW_WRITE_EXTERNAL_VIA_LEGACY = 0x42A;
        private const int MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY = 0x42B;
        private const int NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen = 0x42C;
        private const int MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen = 0x42D;
        private const int POST_NOTIFICATION_PERMISSION = 0x42E;
        private AndroidX.ViewPager.Widget.ViewPager pager = null;

        public static PowerManager.WakeLock CpuKeepAlive_Transfer = null;
        public static Android.Net.Wifi.WifiManager.WifiLock WifiKeepAlive_Transfer = null;
        public static System.Timers.Timer KeepAliveInactivityKillTimer = null;

        public static void KeepAliveInactivityKillTimerEllapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (CpuKeepAlive_Transfer != null)
            {
                CpuKeepAlive_Transfer.Release();
            }
            if (WifiKeepAlive_Transfer != null)
            {
                WifiKeepAlive_Transfer.Release();
            }
            KeepAliveInactivityKillTimer.Stop();
        }

        public static void SetUpSharing(Action uiUpdateAction = null)
        {
            Action setUpSharedFileCache = new Action(() =>
            {
                string errorMessage = string.Empty;
                bool success = false;
                LogDebug("We meet sharing conditions, lets set up the sharedFileCache for 1st time.");
                try
                {
                    //DocumentFile docFile = null;
                    //if (SoulSeekState.PreOpenDocumentTree() || !SoulSeekState.UploadDataDirectoryUriIsFromTree)
                    //{
                    //    docFile = DocumentFile.FromFile(new Java.IO.File(Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri).Path));
                    //}
                    //else
                    //{
                    //    docFile = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri));
                    //}
                    success = InitializeDatabase(null, true, out errorMessage); //we check the cache which has ALL of the parsed results in it. much different from rescanning.
                }
                catch (Exception e)
                {
                    LogDebug("Error setting up sharedFileCache for 1st time." + e.Message + e.StackTrace);
                    //SoulSeekState.UploadDataDirectoryUri = null;
                    //SoulSeekState.UploadDataDirectoryUriIsFromTree = true;
                    SetUnsetSharingBasedOnConditions(false, true);
                    if(!(e is DirectoryAccessFailure))
                    {
                        MainActivity.LogFirebase("MainActivity error parsing: " + e.Message + "  " + e.StackTrace);
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_sharing), ToastLength.Long).Show();
                    }));
                }

                if (success && SoulSeekState.SharedFileCache != null && SoulSeekState.SharedFileCache.SuccessfullyInitialized)
                {
                    LogDebug("database full initialized.");
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.success_sharing), ToastLength.Short).Show();
                    }));
                    try
                    {
                        //setup soulseek client with handlers if all conditions met
                        SetUnsetSharingBasedOnConditions(false);
                    }
                    catch (Exception e)
                    {
                        MainActivity.LogFirebase("MainActivity error setting handlers: " + e.Message + "  " + e.StackTrace);
                    }
                }
                else if (!success)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                    {
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_sharing), ToastLength.Short).Show();
                        }
                        else
                        {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, errorMessage, ToastLength.Short).Show();
                        }
                    }));
                }

                if (uiUpdateAction != null)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateAction);
                }
                SoulSeekState.AttemptedToSetUpSharing = true;
            });
            System.Threading.ThreadPool.QueueUserWorkItem((object o) => { setUpSharedFileCache(); });
        }

        private ISharedPreferences sharedPreferences;
        private const string defaultMusicUri = "content://com.android.externalstorage.documents/tree/primary%3AMusic";
        protected override void OnCreate(Bundle savedInstanceState)
        {

            //basically if the Intent created the MainActivity, then we want to handle it (i.e. if from "Search Here")
            //however, if we say rotate the device or leave and come back to it (and the activity got destroyed in the mean time) then
            //it will re-handle the activity each time.  We can check if it is truly "new" by looking at the savedInstanceState.
            bool reborn = false;
            if (savedInstanceState == null)
            {
                LogDebug("Main Activity On Create NEW");
            }
            else
            {
                reborn = true;
                LogDebug("Main Activity On Create REBORN");
            }


            try
            {
                if (CpuKeepAlive_Transfer == null)
                {
                    CpuKeepAlive_Transfer = ((PowerManager)this.GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Partial, "Seeker Download CPU_Keep_Alive");
                    CpuKeepAlive_Transfer.SetReferenceCounted(false);
                }
                if (WifiKeepAlive_Transfer == null)
                {
                    WifiKeepAlive_Transfer = ((Android.Net.Wifi.WifiManager)this.GetSystemService(Context.WifiService)).CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Seeker Download Wifi_Keep_Alive");
                    WifiKeepAlive_Transfer.SetReferenceCounted(false);
                }
            }
            catch (Exception e)
            {
                LogFirebase("error init keepalives: " + e.Message + e.StackTrace);
            }
            if (KeepAliveInactivityKillTimer == null)
            {
                KeepAliveInactivityKillTimer = new System.Timers.Timer(60 * 1000 * 10); //kill after 10 mins of no activity..
                                                                                        //remember that this is a fallback. for when foreground service is still running but nothing is happening otherwise.
                KeepAliveInactivityKillTimer.Elapsed += KeepAliveInactivityKillTimerEllapsed;
                KeepAliveInactivityKillTimer.AutoReset = false;
            }

            //FirebaseCrash.Report();
            //MainActivity.LogFirebase("This happened......"));

            //this.Window.SetSoftInputMode();

            base.OnCreate(savedInstanceState);
            //System.Threading.Thread.CurrentThread.Name = "Main Activity Thread";
            Xamarin.Essentials.Platform.Init(this, savedInstanceState); //this is what you are supposed to do.
            SetContentView(Resource.Layout.activity_main);

            //AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById(Resource.Id.my_toolbar);
            //SetSupportActionBar(myToolbar);
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);


            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById(Resource.Id.toolbar);
            myToolbar.Title = this.GetString(Resource.String.home_tab);
            myToolbar.InflateMenu(Resource.Menu.account_menu);
            SetSupportActionBar(myToolbar);
            myToolbar.InflateMenu(Resource.Menu.account_menu); //twice??




            System.Console.WriteLine("Testing.....");

            sharedPreferences = this.GetSharedPreferences("SoulSeekPrefs", 0);

            //if (uiModeManager.NightMode == UiModeManager.ModeNightYes)
            //{
            //    // System is in Night mode
            //}
            //else if (uiModeManager.NightMode == Android.App.UiNightMode.Yes)
            //{
            //    // System is in Day mode
            //}


            //this.RequestPostNotificationPermissionsIfApplicable();

            //restoreSoulSeekState(savedInstanceState);

            TabLayout tabs = (TabLayout)FindViewById(Resource.Id.tabs);

            pager = (AndroidX.ViewPager.Widget.ViewPager)FindViewById(Resource.Id.pager);
            pager.PageSelected += Pager_PageSelected;
            TabsPagerAdapter adapter = new TabsPagerAdapter(SupportFragmentManager);

            tabs.TabSelected += Tabs_TabSelected;
            pager.Adapter = adapter;
            pager.AddOnPageChangeListener(new OnPageChangeLister1());
            //tabs.SetupWithViewPager(pager);
            //this is a relatively safe way that prevents rotates from redoing the intent.
            bool alreadyHandled = Intent.GetBooleanExtra("ALREADY_HANDLED", false);
            Intent = Intent.PutExtra("ALREADY_HANDLED", true);
            //Intent = i;
            if (Intent != null)
            {
                //if(Intent.Flags == (ActivityFlags.LaunchedFromHistory | ActivityFlags.NewTask))
                //{
                //    //FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY | FLAG_ACTIVITY_NEW_TASK
                //    //-back button then resumed from history
                //    //FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY
                //    //-home button then resumed from history
                //    //FLAG_ACTIVITY_NEW_TASK
                //    //-clicking app icon or intent filter
                //    MainActivity.LogDebug("new task | launched from history");
                //}


                if (Intent.GetIntExtra(DownloadForegroundService.FromTransferString, -1) == 2)
                {
                    pager.SetCurrentItem(2, false);
                }
                else if (Intent.GetIntExtra(SeekerApplication.FromFolderAlert, -1) == 2)
                {
                    pager.SetCurrentItem(2, false);
                }
                else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToBrowse, -1) == 3)
                {
                    pager.SetCurrentItem(3, false);
                }
                else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToSearch, -1) == 1)
                {
                    //var navigator = SoulSeekState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                    //navigator.NavigationItemReselected += Navigator_NavigationItemReselected;
                    //navigator.NavigationItemSelected += Navigator_NavigationItemSelected;
                    //navigator.ViewAttachedToWindow += Navigator_ViewAttachedToWindow;
                    pager.SetCurrentItem(1, false);
                }
                else if (Intent.GetIntExtra(UserListActivity.IntentSearchRoom, -1) == 1)
                {
                    pager.SetCurrentItem(1, false);
                }
                else if (Intent.GetIntExtra(WishlistController.FromWishlistString, -1) == 1 && !reborn) //if its not reborn then the OnNewIntent will handle it...
                {
                    SoulSeekState.MainActivityRef = this; //set these early. they are needed
                    SoulSeekState.ActiveActivityRef = this;

                    MainActivity.LogInfoFirebase("is resumed: " + (SearchFragment.Instance?.IsResumed ?? false).ToString());
                    MainActivity.LogInfoFirebase("from wishlist clicked");
                    int currentPage = pager.CurrentItem;
                    int tabID = Intent.GetIntExtra(WishlistController.FromWishlistStringID, int.MaxValue);
                    if (currentPage == 1) //this is the case even if process previously got am state killed.
                    {
                        MainActivity.LogInfoFirebase("from wishlist clicked - current page");
                        if (tabID == int.MaxValue)
                        {
                            LogFirebase("tabID == int.MaxValue");
                        }
                        else if (!SearchTabHelper.SearchTabCollection.ContainsKey(tabID))
                        {
                            LogFirebase("doesnt contain key");
                            Toast.MakeText(this, this.GetString(Resource.String.wishlist_tab_error), ToastLength.Long).Show();
                        }
                        else
                        {
                            if (SearchFragment.Instance?.IsResumed ?? false) //!??! this logic is backwards...
                            {
                                MainActivity.LogDebug("we are on the search page but we need to wait for OnResume search frag");
                                goToSearchTab = tabID; //we read this we resume
                            }
                            else
                            {
                                SearchFragment.Instance.GoToTab(tabID, false, true);
                            }
                        }
                    }
                    else
                    {
                        MainActivity.LogInfoFirebase("from wishlist clicked - different page");
                        //when we move to the page, lets move to our tab, if its not the current one..
                        goToSearchTab = tabID; //we read this when we move tab...
                        pager.SetCurrentItem(1, false);
                    }
                }
                else if (((Intent.GetIntExtra(UploadForegroundService.FromTransferUploadString, -1) == 2) || (Intent.GetIntExtra(UPLOADS_NOTIF_EXTRA, -1) == 2)) && !alreadyHandled) //else every rotation will change Downloads to Uploads.
                {
                    HandleFromNotificationUploadIntent();
                }
                else if (Intent.GetIntExtra(SettingsActivity.FromBrowseSelf, -1) == 3)
                {
                    MainActivity.LogInfoFirebase("from browse self");
                    pager.SetCurrentItem(3, false);
                }
                else if (SearchSendIntentHelper.IsFromActionSend(Intent) && !reborn) //this will always create a new instance, so if its reborn then its an old intent that we already followed.
                {
                    SoulSeekState.MainActivityRef = this;
                    SoulSeekState.ActiveActivityRef = this;
                    MainActivity.LogDebug("MainActivity action send intent");
                    //give us a new fresh tab if the current one has a search in it...
                    if (!string.IsNullOrEmpty(SearchTabHelper.LastSearchTerm))
                    {
                        MainActivity.LogDebug("lets go to a new fresh tab");
                        int newTabToGoTo = SearchTabHelper.AddSearchTab();

                        MainActivity.LogDebug("search fragment null? " + (SearchFragment.Instance == null).ToString());

                        if (SearchFragment.Instance?.IsResumed ?? false)
                        {
                            //if resumed is true
                            SearchFragment.Instance.GoToTab(newTabToGoTo, false, true);
                        }
                        else
                        {
                            MainActivity.LogDebug("we are on the search page but we need to wait for OnResume search frag");
                            goToSearchTab = newTabToGoTo; //we read this we resume
                        }
                    }

                    //go to search tab
                    MainActivity.LogDebug("prev search term: " + SearchDialog.SearchTerm);
                    SearchDialog.SearchTerm = Intent.GetStringExtra(Intent.ExtraText);
                    SearchDialog.IsFollowingLink = false;
                    pager.SetCurrentItem(1, false);
                    if (SearchSendIntentHelper.TryParseIntent(Intent, out string searchTermFound))
                    {
                        //we are done parsing the intent
                        SearchDialog.SearchTerm = searchTermFound;
                    }
                    else if (SearchSendIntentHelper.FollowLinkTaskIfApplicable(Intent))
                    {
                        SearchDialog.IsFollowingLink = true;
                    }
                    //close previous instance
                    if (SearchDialog.Instance != null)
                    {
                        MainActivity.LogDebug("previous instance exists");
                        //SearchDialog.Instance.Dismiss(); //throws exception, cannot perform this action after onSaveInstanceState
                    }
                    var searchDialog = new SearchDialog(SearchDialog.SearchTerm, SearchDialog.IsFollowingLink);
                    searchDialog.Show(SupportFragmentManager, "Search Dialog");
                }
            }


            SoulSeekState.MainActivityRef = this;
            SoulSeekState.ActiveActivityRef = this;


            //UploadDirectoryManager.UploadDirectories = new List<UploadDirectoryInfo>();
            //UploadDirectoryManager.UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic/document/864A-C3E8%3AMusic", true, false, false, "Music (1)"));
            //UploadDirectoryManager.UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic%2F%5B2000%5D%20Spirit%20x/document/864A-C3E8%3AMusic%2F%5B2000%5D%20Spirix", true, true, false, null));
            //UploadDirectoryManager.//UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/primary%3AMusic/document/primary%3AMusic", true, false, false, null));
            //UploadDirectoryManager.UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic/document/864A-C3E8%3AMusic", true, false, false, "Music (1)"));


            //if we have all the conditions to share, then set sharing up.
            if (MeetsSharingConditions() && !SoulSeekState.IsParsing && !MainActivity.IsSharingSetUpSuccessfully())
            {
                SetUpSharing();
            }
            else if (SoulSeekState.NumberOfSharedDirectoriesIsStale)
            {
                InformServerOfSharedFiles();
                SoulSeekState.AttemptedToSetUpSharing = true;
            }
            

            //this.DeleteSharedPreferences("SoulSeekPrefs");

            //Mono.Nat.NatUtility.DeviceFound += NatUtility_DeviceFound; //error bad message parsePAL_UPNP_SOAP_E_INVALID_ARGS. was able to look at all the current wifi port mappings however :)
            //Mono.Nat.NatUtility.StartDiscovery();


            //InternetGatewayDevice[] IGDs = InternetGatewayDevice.getDevices(60000);
            //if (IGDs != null)
            //{
            //    for (int i = 0; i < IGDs.Length; i++)
            //    {

            //    }
            //}
            //Android.Net.Wifi.WifiManager wm = this.GetSystemService(WifiService);
            //wm.
            //Mono.Nat.NatUtility.UnknownDeviceFound += NatUtility_DeviceFound;



            SoulSeekState.SharedPreferences = sharedPreferences;
            SoulSeekState.MainActivityRef = this;
            SoulSeekState.ActiveActivityRef = this;

            UpdateForScreenSize();

            //#if DEBUG //todo remove

            //WishlistController.SearchIntervalMilliseconds = 1000*30;
            //WishlistController.Initialize();

            //#endif


            if (SoulSeekState.UseLegacyStorage())
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Denied)
                {
                    ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage }, WRITE_EXTERNAL);
                }
                //file picker with legacy case
                if (!string.IsNullOrEmpty(SoulSeekState.SaveDataDirectoryUri))
                {
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    Android.Net.Uri chosenUri = Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri);
                    bool canWrite = false;
                    try
                    {
                        //a phone failed 4 times with //POCO X3 Pro
                        //Android 11(SDK 30)
                        //Caused by: java.lang.IllegalArgumentException: 
                        //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                        //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                        if (SoulSeekState.PreOpenDocumentTree() || !SoulSeekState.SaveDataDirectoryUriIsFromTree)
                        {
                            canWrite = DocumentFile.FromFile(new Java.IO.File(chosenUri.Path)).CanWrite();
                        }
                        else
                        {
                            //on changing the code and restarting for api 22 
                            //persistenduripermissions is empty
                            //and exists is false, cannot list files

                            //var list1 = this.ContentResolver.PersistedUriPermissions;
                            //foreach(var item1 in list1)
                            //{
                            //    string content1 = item1.Uri.Path;
                            //}


                            canWrite = DocumentFile.FromTreeUri(this, chosenUri).CanWrite();
                        }
                    }
                    catch (Exception e)
                    {
                        if (chosenUri != null)
                        {
                            //legacy DocumentFile.FromTreeUri failed with URI: /tree/2A6B-256B:Seeker/Soulseek Complete Invalid URI: /tree/2A6B-256B:Seeker/Soulseek Complete
                            //legacy DocumentFile.FromTreeUri failed with URI: /tree/raw:/storage/emulated/0/Download/Soulseek Downloads Invalid URI: /tree/raw:/storage/emulated/0/Download/Soulseek Downloads
                            LogFirebase("legacy DocumentFile.FromTreeUri failed with URI: " + chosenUri.ToString() + " " + e.Message + " scheme " + chosenUri.Scheme);
                        }
                        else
                        {
                            LogFirebase("legacy DocumentFile.FromTreeUri failed with null URI");
                        }
                    }
                    if (canWrite)
                    {
                        if (SoulSeekState.PreOpenDocumentTree())
                        {
                            SoulSeekState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(chosenUri.Path));
                        }
                        else
                        {
                            SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, chosenUri);
                        }
                    }
                    else
                    {
                        MainActivity.LogFirebase("cannot write" + chosenUri?.ToString() ?? "null");
                    }
                }

                //now for incomplete
                if (!string.IsNullOrEmpty(SoulSeekState.ManualIncompleteDataDirectoryUri))
                {
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    Android.Net.Uri chosenIncompleteUri = Android.Net.Uri.Parse(SoulSeekState.ManualIncompleteDataDirectoryUri);
                    bool canWrite = false;
                    try
                    {
                        //a phone failed 4 times with //POCO X3 Pro
                        //Android 11(SDK 30)
                        //Caused by: java.lang.IllegalArgumentException: 
                        //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                        //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                        if (SoulSeekState.PreOpenDocumentTree() || !SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree)
                        {
                            canWrite = DocumentFile.FromFile(new Java.IO.File(chosenIncompleteUri.Path)).CanWrite();
                        }
                        else
                        {
                            //on changing the code and restarting for api 22 
                            //persistenduripermissions is empty
                            //and exists is false, cannot list files

                            //var list1 = this.ContentResolver.PersistedUriPermissions;
                            //foreach(var item1 in list1)
                            //{
                            //    string content1 = item1.Uri.Path;
                            //}


                            canWrite = DocumentFile.FromTreeUri(this, chosenIncompleteUri).CanWrite();
                        }
                    }
                    catch (Exception e)
                    {
                        if (chosenIncompleteUri != null)
                        {
                            LogFirebase("legacy Incomplete DocumentFile.FromTreeUri failed with URI: " + chosenIncompleteUri.ToString() + " " + e.Message);
                        }
                        else
                        {
                            LogFirebase("legacy Incomplete DocumentFile.FromTreeUri failed with null URI");
                        }
                    }
                    if (canWrite)
                    {
                        if (SoulSeekState.PreOpenDocumentTree())
                        {
                            SoulSeekState.RootIncompleteDocumentFile = DocumentFile.FromFile(new Java.IO.File(chosenIncompleteUri.Path));
                        }
                        else
                        {
                            SoulSeekState.RootIncompleteDocumentFile = DocumentFile.FromTreeUri(this, chosenIncompleteUri);
                        }
                    }
                    else
                    {
                        MainActivity.LogFirebase("cannot write incomplete" + chosenIncompleteUri?.ToString() ?? "null");
                    }
                }


            }
            else
            {

                Android.Net.Uri res = null; //var y = MediaStore.Audio.Media.ExternalContentUri.ToString();
                if (string.IsNullOrEmpty(SoulSeekState.SaveDataDirectoryUri))
                {
                    //try
                    //{
                    //    //storage/emulated/0/music
                    //    Java.IO.File f = new Java.IO.File(@"/storage/emulated/0/Music");///Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic);
                    //    //res = f.ToURI();
                    //    res = Android.Net.Uri.FromFile(f);//Parse(f.ToURI().ToString());
                    //}
                    //catch
                    //{
                    //    res = Android.Net.Uri.Parse(defaultMusicUri);//TryCreate("content://com.android.externalstorage.documents/tree/primary%3AMusic", UriKind.Absolute,out res);
                    //}
                    res = Android.Net.Uri.Parse(defaultMusicUri);
                }
                else
                {
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    res = Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri);
                }

                //string path  = res.Path; 
                //string lastPath = res.LastPathSegment;
                //var segs = res.PathSegments;
                //DocumentFile f = DocumentFile.FromTreeUri(this, res);
                //string name = f.Name;
                //bool isEmulated = Android.OS.Environment.IsExternalStorageEmulated;
                //bool isRemovable = Android.OS.Environment.IsExternalStorageRemovable;

                bool canWrite = false;
                try
                {
                    //a phone failed 4 times with //POCO X3 Pro
                    //Android 11(SDK 30)
                    //Caused by: java.lang.IllegalArgumentException: 
                    //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                    //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                    if (SoulSeekState.PreOpenDocumentTree() || !SoulSeekState.SaveDataDirectoryUriIsFromTree) //this will never get hit..
                    {
                        canWrite = DocumentFile.FromFile(new Java.IO.File(res.Path)).CanWrite();
                    }
                    else
                    {
                        canWrite = DocumentFile.FromTreeUri(this, res).CanWrite();
                    }

                    // if canwrite is false then if we try to create a file we get null.
                    //if (SoulSeekState.SaveDataDirectoryUriIsFromTree)
                    //{
                    //    SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, res);

                    //}
                    //else
                    //{
                    //    SoulSeekState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(res.Path));
                    //}

                    //var file1 = SoulSeekState.RootDocumentFile.CreateFile("text/plain", "testing_1234.txt");


                }
                catch (Exception e)
                {
                    if (res != null)
                    {
                        LogFirebase("DocumentFile.FromTreeUri failed with URI: " + res.ToString() + " " + e.Message);
                    }
                    else
                    {
                        LogFirebase("DocumentFile.FromTreeUri failed with null URI");
                    }
                }
                //if (DocumentFile.FromTreeUri(this, Uri.TryCreate("",UriKind.Absolute)))
                if (!canWrite)
                {

                    var b = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
                    b.SetTitle(this.GetString(Resource.String.seeker_needs_dl_dir));
                    b.SetMessage(this.GetString(Resource.String.seeker_needs_dl_dir_content));
                    ManualResetEvent mre = new ManualResetEvent(false);
                    EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                    {
                        var storageManager = Android.OS.Storage.StorageManager.FromContext(this);
                        var intent = storageManager.PrimaryStorageVolume.CreateOpenDocumentTreeIntent();
                        intent.PutExtra(DocumentsContract.ExtraInitialUri, res);
                        intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPrefixUriPermission);
                        try
                        {
                            this.StartActivityForResult(intent, NEW_WRITE_EXTERNAL);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains(Helpers.NoDocumentOpenTreeToHandle))
                            {
                                FallbackFileSelectionEntry(false);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    });
                    b.SetPositiveButton(Resource.String.okay, eventHandler);
                    b.SetCancelable(false);
                    b.Show();


                    //this.SendBroadcast(storageManager.PrimaryStorageVolume.CreateOpenDocumentTreeIntent());
                }
                else
                {
                    if(SoulSeekState.SaveDataDirectoryUriIsFromTree)
                    {
                        SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, res);
                        
                    }
                    else
                    {
                        SoulSeekState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(res.Path));
                    }
                }

                bool manualSet = false;
                //for incomplete case
                Android.Net.Uri incompleteRes = null; //var y = MediaStore.Audio.Media.ExternalContentUri.ToString();
                if (!string.IsNullOrEmpty(SoulSeekState.ManualIncompleteDataDirectoryUri))
                {
                    manualSet = true;
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    incompleteRes = Android.Net.Uri.Parse(SoulSeekState.ManualIncompleteDataDirectoryUri);
                }
                else
                {
                    manualSet = false;
                }

                if (manualSet)
                {
                    bool canWriteIncomplete = false;
                    try
                    {
                        //a phone failed 4 times with //POCO X3 Pro
                        //Android 11(SDK 30)
                        //Caused by: java.lang.IllegalArgumentException: 
                        //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                        //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                        if (SoulSeekState.PreOpenDocumentTree() || !SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree)
                        {
                            canWriteIncomplete = DocumentFile.FromFile(new Java.IO.File(incompleteRes.Path)).CanWrite();
                        }
                        else
                        {
                            canWriteIncomplete = DocumentFile.FromTreeUri(this, incompleteRes).CanWrite();
                        }
                    }
                    catch (Exception e)
                    {
                        if (incompleteRes != null)
                        {
                            LogFirebase("DocumentFile.FromTreeUri failed with incomplete URI: " + incompleteRes.ToString() + " " + e.Message);
                        }
                        else
                        {
                            LogFirebase("DocumentFile.FromTreeUri failed with incomplete null URI");
                        }
                    }
                    if (canWriteIncomplete)
                    {
                        if (SoulSeekState.PreOpenDocumentTree() || !SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree)
                        {
                            SoulSeekState.RootIncompleteDocumentFile = DocumentFile.FromFile(new Java.IO.File(incompleteRes.Path));
                        }
                        else
                        {
                            SoulSeekState.RootIncompleteDocumentFile = DocumentFile.FromTreeUri(this, incompleteRes);
                        }
                    }
                }




            }



            //testing

            //var s = System.IO.Path.GetInvalidFileNameChars();
            //var s2 = System.IO.Path.GetInvalidPathChars();
            //DocumentFile d = DocumentFile.FromTreeUri(this, Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri)).CreateDirectory("-+\\/\0");
            //if(d==null)
            //{

            //}
            //else
            //{
            //    bool x = d.Exists();
            //}



            //    //logging code for unit tests / diagnostic..
            //    var root = DocumentFile.FromTreeUri(SoulSeekState.MainActivityRef , Android.Net.Uri.Parse( SoulSeekState.SaveDataDirectoryUri) );
            //    DocumentFile f = root.FindFile("" + "_dir_response");

            //    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(f.Uri);
            //    BrowseResponse br = null;

            //    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //    br = formatter.Deserialize(stream) as BrowseResponse;

            ////var br = SoulSeekState.SoulseekClient.BrowseAsync("");
            ////br.Wait();

            //    DownloadDialog.CreateTree(br,false, null, null, "",out _);

            //if(b.DirectoryCount==0&&b.LockedDirectoryCount!=0)
            //{
            //    errorMsgToToast = "User is only sharing locked directories";
            //    return null;
            //}
            //else if(b.DirectoryCount==0&&b.LockedDirectoryCount==0)
            //{
            //    errorMsgToToast = "User is not sharing any directories";
            //    return null;
            //}


            //setKeyboardVisibilityListener();

            //OneTimeWorkRequest otwr = OneTimeWorkRequest.Builder.From<DownloadWorkerTest>().Build();
            //WorkManager.GetInstance(this).Enqueue(otwr); //will this keep use alive for 30 mins....  FAILURE - does not keep alive connections.


            //TEST CODE //TEST CODE
            //SERVICE STARTED BUT THREAD IS STILL AN ACTIVITY THREAD: 36+ mins. :) - after killing it it went on for 20+ mins (ps -A shows it + you get notifications). after restarting device its gone :(


            //Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
            //this.StartService(downloadServiceIntent);


            //In the non service case it still goes on for 19+ mins..  but after you kill it by swiping up then its gone (no more notifications and ps -A shows nothing)
            //Thread t1 = new Thread(() => { 
            //    while(true)
            //    {
            //        this.RunOnUiThread(() => {Toast.MakeText(this,"we are running",ToastLength.Long).Show(); });
            //        System.Threading.Thread.Sleep(1000*30);
            //    }

            //    });
            //t1.Start();

            //END TEST
            //AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
            //string pkgname = this.ApplicationInfo.PackageName; //these all return com.companyname.androidapp1
            //string pkgnameunique = this.PackageName;
            //string pkgnameunique2 = Application.Context.PackageName;

            // GetIncompleteStream(@"testdir\testdir1\testfname.mp3", out Android.Net.Uri int2);
        }

        public void FallbackFileSelection(int requestCode)
        {
            //Create FolderOpenDialog
            SimpleFileDialog fileDialog = new SimpleFileDialog(SoulSeekState.ActiveActivityRef, SimpleFileDialog.FileSelectionMode.FolderChoose);
            fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath).ContinueWith(
                (Task<string> t) => {
                    if (t.Result == null || t.Result == string.Empty)
                    {
                        this.OnActivityResult(requestCode, Result.Canceled, new Intent());
                        return;
                    }
                    else
                    {
                        var intent = new Intent();
                        DocumentFile f = DocumentFile.FromFile(new Java.IO.File(t.Result));
                        intent.SetData(f.Uri);
                        this.OnActivityResult(requestCode, Result.Ok, intent);
                    }
                });
        }

        public static Action<Task> GetPostNotifPermissionTask()
        {
            return new Action<Task>((task) => {
                if (task.IsCompletedSuccessfully)
                {
                    RequestPostNotificationPermissionsIfApplicable();
                }
            });
            
        }

        private static bool postNotficationAlreadyRequestedInSession = false;
        /// <summary>
        /// As far as where to place this, doing it on launch is no good (as they will already
        ///   see yet another though more important permission in the background behind them).
        /// Doing this on login (i.e. first session login) seems decent.
        /// </summary>
        private static void RequestPostNotificationPermissionsIfApplicable()
        {
            if (postNotficationAlreadyRequestedInSession)
            {
                return;
            }
            postNotficationAlreadyRequestedInSession = true;

            if ((int)Android.OS.Build.VERSION.SdkInt < 33)
            {
                return;
            }

            try
            {
                if (ContextCompat.CheckSelfPermission(SoulSeekState.ActiveActivityRef, Manifest.Permission.PostNotifications) == Android.Content.PM.Permission.Denied)
                {
                    bool alreadyShown = SoulSeekState.SharedPreferences.GetBoolean(SoulSeekState.M_PostNotificationRequestAlreadyShown, false);
                    if (alreadyShown)
                    {
                        return;
                    }

                    if (OnUIthread())
                    {
                        RequestNotifPermissionsLogic();
                    }
                    else
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(RequestNotifPermissionsLogic);
                    }
                }
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("RequestPostNotificationPermissionsIfApplicable error: " + e.Message + e.StackTrace);
            }
        }

        // recommended way, if user only denies once then next time (ShouldShowRequestPermissionRationale lets us know this)
        //   then show a blurb on what the permissions are used for and ask a second (and last) time.
        private static void RequestNotifPermissionsLogic()
        {
            try
            {
                if (ActivityCompat.ShouldShowRequestPermissionRationale(SoulSeekState.ActiveActivityRef, Manifest.Permission.PostNotifications))
                {
                    var b = new AndroidX.AppCompat.App.AlertDialog.Builder(SoulSeekState.ActiveActivityRef, Resource.Style.MyAlertDialogTheme);
                    b.SetTitle("Allow Notifications?");
                    b.SetMessage("Seeker provides push notifications to keep you updated on file uploads/downloads and incoming user messages.");
                    ManualResetEvent mre = new ManualResetEvent(false);

                    // make sure we never prompt the user for these permissions again
                    void setAlreadyShown()
                    {
                        lock (MainActivity.SHARED_PREF_LOCK)
                        {
                            var editor = SoulSeekState.SharedPreferences.Edit();
                            editor.PutBoolean(SoulSeekState.M_PostNotificationRequestAlreadyShown, true);
                            editor.Commit();
                        }
                    }

                    EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                    {
                        ActivityCompat.RequestPermissions(SoulSeekState.ActiveActivityRef, new string[] { Manifest.Permission.PostNotifications }, POST_NOTIFICATION_PERMISSION);
                        setAlreadyShown();
                    });
                    b.SetPositiveButton(Resource.String.okay, eventHandler);

                    EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs noArgs) =>
                    {
                        setAlreadyShown();
                    });
                    b.SetNegativeButton("No thanks", eventHandlerCancel);

                    b.SetCancelable(false);
                    b.Show();
                }
                else
                {
                    // normal first time
                    ActivityCompat.RequestPermissions(SoulSeekState.ActiveActivityRef, new string[] { Manifest.Permission.PostNotifications }, POST_NOTIFICATION_PERMISSION);
                }
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("RequestPostNotificationPermissionsIfApplicable error: " + e.Message + e.StackTrace);
            }
        }

        protected override void OnStart()
        {
            //this fixes a bug as follows:
            //previously we only set MainActivityRef on Create.
            //therefore if one launches MainActivity via a new intent (i.e. go to user list, then search users files) it will be set with the new search user activity.
            //then if you press back twice you will see the original activity but the MainActivityRef will still be set to the now destroyed activity since it was last to call onCreate.
            //so then the FragmentManager will be null among other things...
            SoulSeekState.MainActivityRef = this;
            base.OnStart();
        }
        public static bool fromNotificationMoveToUploads = false;
        protected override void OnNewIntent(Intent intent)
        {
            MainActivity.LogDebug("OnNewIntent");
            base.OnNewIntent(intent);
            Intent = intent.PutExtra("ALREADY_HANDLED", true);
            if (Intent.GetIntExtra(WishlistController.FromWishlistString, -1) == 1)
            {
                MainActivity.LogInfoFirebase("is null: " + (SearchFragment.Instance?.Activity == null || (SearchFragment.Instance?.IsResumed ?? false)).ToString());
                MainActivity.LogInfoFirebase("from wishlist clicked");
                int currentPage = pager.CurrentItem;
                int tabID = Intent.GetIntExtra(WishlistController.FromWishlistStringID, int.MaxValue);
                if (currentPage == 1)
                {
                    if (tabID == int.MaxValue)
                    {
                        LogFirebase("tabID == int.MaxValue");
                    }
                    else if (!SearchTabHelper.SearchTabCollection.ContainsKey(tabID))
                    {
                        Toast.MakeText(this, this.GetString(Resource.String.wishlist_tab_error), ToastLength.Long).Show();
                    }
                    else
                    {
                        if (SearchFragment.Instance?.Activity == null || (SearchFragment.Instance?.IsResumed ?? false))
                        {
                            MainActivity.LogDebug("we are on the search page but we need to wait for OnResume search frag");
                            goToSearchTab = tabID; //we read this we resume
                        }
                        else
                        {
                            SearchFragment.Instance.GoToTab(tabID, false, true);
                        }
                    }
                }
                else
                {
                    //when we move to the page, lets move to our tab, if its not the current one..
                    goToSearchTab = tabID; //we read this when we move tab...
                    pager.SetCurrentItem(1, false);
                }
            }
            else if (((Intent.GetIntExtra(UploadForegroundService.FromTransferUploadString, -1) == 2) || (Intent.GetIntExtra(UPLOADS_NOTIF_EXTRA, -1) == 2))) //else every rotation will change Downloads to Uploads.
            {
                HandleFromNotificationUploadIntent();
            }
            else if (Intent.GetIntExtra(SettingsActivity.FromBrowseSelf, -1) == 3)
            {
                MainActivity.LogInfoFirebase("from browse self");
                pager.SetCurrentItem(3, false);
            }
            else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToBrowse, -1) == 3)
            {
                pager.SetCurrentItem(3, false);
            }
            else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToSearch, -1) == 1)
            {
                //var navigator = SoulSeekState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                //navigator.NavigationItemReselected += Navigator_NavigationItemReselected;
                //navigator.NavigationItemSelected += Navigator_NavigationItemSelected;
                //navigator.ViewAttachedToWindow += Navigator_ViewAttachedToWindow;
                pager.SetCurrentItem(1, false);
            }
            else if (Intent.GetIntExtra(DownloadForegroundService.FromTransferString, -1) == 2)
            {
                pager.SetCurrentItem(2, false);
            }
            else if (Intent.GetIntExtra(SeekerApplication.FromFolderAlert, -1) == 2)
            {
                pager.SetCurrentItem(2, false);
            }
        }

        private void HandleFromNotificationUploadIntent()
        {
            //either we change to uploads mode now (if resumed), or we wait for on resume to do it.

            MainActivity.LogInfoFirebase("from uploads clicked");
            int currentPage = pager.CurrentItem;
            if (currentPage == 2)
            {
                if (StaticHacks.TransfersFrag?.Activity == null || (StaticHacks.TransfersFrag?.IsResumed ?? false))
                {
                    MainActivity.LogInfoFirebase("we need to wait for on resume");
                    fromNotificationMoveToUploads = true; //we read this in onresume
                }
                else
                {
                    //we can change to uploads mode now
                    MainActivity.LogDebug("go to upload now");
                    StaticHacks.TransfersFrag.MoveToUploadForNotif();
                }
            }
            else
            {
                fromNotificationMoveToUploads = true; //we read this in onresume
                pager.SetCurrentItem(2, false);
            }
        }

        /// <summary>
        /// This is responsible for filing the PMs into the data structure...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoulseekClient_PrivateMessageReceived(object sender, PrivateMessageReceivedEventArgs e)
        {
            AddMessage(e);
            //string msg = e.Message;
            //throw new NotImplementedException();
        }

        private void AddMessage(PrivateMessageReceivedEventArgs messageEvent)
        {

        }

        private void Navigator_ViewAttachedToWindow(object sender, View.ViewAttachedToWindowEventArgs e)
        {
            // throw new NotImplementedException();
        }

        //private void Navigator_NavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        //private void Navigator_NavigationItemReselected(object sender, BottomNavigationView.NavigationItemReselectedEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        public static void GetDownloadPlaceInQueueBatch(List<TransferItem> transferItems, bool addIfNotAdded)
        {

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out t))
                {
                    t.ContinueWith(new Action<Task>((Task t) =>
                    {
                        if (t.IsFaulted)
                        {
                            //if(!silent) //always silent..
                            //{
                            //    SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                            //    {
                            //        if (SoulSeekState.ActiveActivityRef != null)
                            //        {
                            //            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show();
                            //        }
                            //    });
                            //}
                            return;
                        }
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { GetDownloadPlaceInQueueBatchLogic(transferItems, addIfNotAdded); });
                    }));
                }
            }
            else
            {
                GetDownloadPlaceInQueueBatchLogic(transferItems, addIfNotAdded);
            }
        }


        public static void GetDownloadPlaceInQueueBatchLogic(List<TransferItem> transferItems, bool addIfNotAdded, Func<TransferItem, object> actionOnComplete = null)
        {
            foreach(TransferItem transferItem in transferItems)
            {
                GetDownloadPlaceInQueueLogic(transferItem.Username, transferItem.FullFilename, addIfNotAdded, true, transferItem, null);
            }
        }


        public static void GetDownloadPlaceInQueue(string username, string fullFileName, bool addIfNotAdded, bool silent, TransferItem transferItemInQuestion = null, Func<TransferItem, object> actionOnComplete = null)
        {

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out t))
                {
                    t.ContinueWith(new Action<Task>((Task t) =>
                    {
                        if (t.IsFaulted)
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                if (SoulSeekState.ActiveActivityRef != null)
                                {
                                    Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show();
                                }
                            });
                            return;
                        }
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { GetDownloadPlaceInQueueLogic(username, fullFileName, addIfNotAdded, silent, transferItemInQuestion, actionOnComplete); });
                    }));
                }
            }
            else
            {
                GetDownloadPlaceInQueueLogic(username, fullFileName, addIfNotAdded, silent, transferItemInQuestion, actionOnComplete);
            }
        }

        private static void GetDownloadPlaceInQueueLogic(string username, string fullFileName, bool addIfNotAdded, bool silent, TransferItem transferItemInQuestion = null, Func<TransferItem, object> actionOnComplete = null)
        {

            Action<Task<int>> updateTask = new Action<Task<int>>(
                (Task<int> t) =>
                {
                    if (t.IsFaulted)
                    {
                        bool transitionToNextState = false;
                        Soulseek.TransferStates state = TransferStates.Errored;
                        if(t.Exception?.InnerException is Soulseek.UserOfflineException uoe)
                        {
                            //Nicotine always immediately transitions from queued to user offline the second the user goes offline. We dont do it immediately but on next check.
                            //for QT you always are in "Queued" no matter what.
                            transitionToNextState = true;
                            state = TransferStates.Errored | TransferStates.UserOffline | TransferStates.FallenFromQueue;
                            if (!silent)
                            {
                                ToastUIWithDebouncer(string.Format(SeekerApplication.GetString(Resource.String.UserXIsOffline), username), "_6_", username);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            //Nicotine transitions from Queued to Cannot Connect IF you pause and resume. Otherwise you stay in Queued. Here if someone explicitly retries (i.e. silent = false) then we will transition states.
                            // otherwise, its okay, lets just stay in Queued.
                            //for QT you always are in "Queued" no matter what.
                            transitionToNextState = !silent;
                            state = TransferStates.Errored | TransferStates.CannotConnect | TransferStates.FallenFromQueue;
                            if (!silent)
                            {
                                ToastUIWithDebouncer(string.Format(SeekerApplication.GetString(Resource.String.CannotConnectUserX), username), "_7_", username);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException is System.TimeoutException)
                        {
                            transitionToNextState = false; //they may just not be sending queue position messages.  that is okay, we can still connect to them just fine for download time.
                            if (!silent)
                            {
                                ToastUIWithDebouncer(string.Format(SeekerApplication.GetString(Resource.String.TimeoutQueueUserX), username), "_8_", username, 6);
                            }
                        }
                        else if (t.Exception?.InnerException?.Message != null && t.Exception.InnerException.Message.Contains("underlying Tcp connection is closed"))
                        {
                            //can be server connection (get user endpoint) or peer connection.
                            transitionToNextState = false;
                            if (!silent)
                            {
                                ToastUIWithDebouncer(string.Format("Failed to get queue position for {0}: Connection was unexpectedly closed.", username), "_9_", username, 6);
                            }
                        }
                        else
                        {
                            if (!silent)
                            {
                                ToastUIWithDebouncer($"Error getting queue position from {username}", "_9_", username);
                            }
                            LogFirebase("GetDownloadPlaceInQueue" + t.Exception.ToString());
                        }

                        // 
                        if(transitionToNextState)
                        {
                            //update the transferItem array
                            if (transferItemInQuestion == null)
                            {
                                transferItemInQuestion = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
                            }

                            if (transferItemInQuestion == null)
                            {
                                return;
                            }
                            try
                            {
                                transferItemInQuestion.CancellationTokenSource.Cancel();
                            }
                            catch(Exception err)
                            {
                                MainActivity.LogFirebase("cancellation token src issue: " + err.Message);
                            }
                            transferItemInQuestion.State = state;
                            //let the Cancel() update it.
                            //TransferItemQueueUpdated?.Invoke(null, transferItemInQuestion); //if the transfer item fragment is bound then we update it..
                        }
                    }
                    else
                    {
                        bool queuePositionChanged = false;

                        //update the transferItem array
                        if(transferItemInQuestion == null)
                        {
                            transferItemInQuestion = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
                        }

                        if (transferItemInQuestion == null)
                        {
                            return;
                        }
                        else
                        {
                            queuePositionChanged = transferItemInQuestion.QueueLength != t.Result;

                            if (t.Result >= 0)
                            {
                                transferItemInQuestion.QueueLength = t.Result;
                            }
                            else
                            {
                                transferItemInQuestion.QueueLength = int.MaxValue;
                            }

                            if(queuePositionChanged)
                            {
                                MainActivity.LogDebug($"Queue Position of {fullFileName} has changed to {t.Result}");
                            }
                            else
                            {
                                MainActivity.LogDebug($"Queue Position of {fullFileName} is still {t.Result}");
                            }
                        }

                        if (actionOnComplete != null)
                        {
                            SoulSeekState.ActiveActivityRef?.RunOnUiThread(() => { actionOnComplete(transferItemInQuestion); });
                        }
                        else
                        {
                            if(queuePositionChanged)
                            {
                                TransferItemQueueUpdated?.Invoke(null, transferItemInQuestion); //if the transfer item fragment is bound then we update it..
                            }
                        }

                    }
                }
            );

            Task<int> getDownloadPlace = null;
            try
            {
                getDownloadPlace = SoulSeekState.SoulseekClient.GetDownloadPlaceInQueueAsync(username, fullFileName, null, transferItemInQuestion.ShouldEncodeFileLatin1(), transferItemInQuestion.ShouldEncodeFolderLatin1());
            }
            catch (TransferNotFoundException)
            {
                if(addIfNotAdded)
                {
                    //it is not downloading... therefore retry the download...
                    if(transferItemInQuestion==null)
                    {
                        transferItemInQuestion = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(fullFileName, username, out int _);
                    }
                    //TransferItem item1 = transferItems[info.Position];  
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    try
                    {
                        transferItemInQuestion.QueueLength = int.MaxValue;
                        Android.Net.Uri incompleteUri = null;
                        TransfersFragment.SetupCancellationToken(transferItemInQuestion, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                        Task task = DownloadDialog.DownloadFileAsync(transferItemInQuestion.Username, transferItemInQuestion.FullFilename, transferItemInQuestion.GetSizeForDL(), cancellationTokenSource, isFileDecodedLegacy: transferItemInQuestion.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: transferItemInQuestion.ShouldEncodeFolderLatin1());
                        task.ContinueWith(DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(transferItemInQuestion.Username, transferItemInQuestion.FullFilename, transferItemInQuestion.Size, task, cancellationTokenSource, transferItemInQuestion.QueueLength, 0, transferItemInQuestion.GetDirectoryLevel()) { TransferItemReference = transferItemInQuestion })));
                    }
                    catch (DuplicateTransferException)
                    {
                        //happens due to button mashing...
                        return;
                    }
                    catch (System.Exception error)
                    {
                        Action a = new Action(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_) + error.Message, ToastLength.Long); });
                        if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                        {

                        }
                        else
                        {
                            MainActivity.LogFirebase(error.Message + " OnContextItemSelected");
                        }
                        if(!silent)
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(a);
                        }
                        return; //otherwise null ref with task!
                    }
                    //TODO: THIS OCCURS TO SOON, ITS NOT gaurentted for the transfer to be in downloads yet...
                    try
                    {
                        getDownloadPlace = SoulSeekState.SoulseekClient.GetDownloadPlaceInQueueAsync(username, fullFileName, null, transferItemInQuestion.ShouldEncodeFileLatin1(), transferItemInQuestion.ShouldEncodeFolderLatin1());
                        getDownloadPlace.ContinueWith(updateTask);
                    }
                    catch (Exception e)
                    {
                        LogFirebase("you likely called getdownloadplaceinqueueasync too soon..." + e.Message);
                    }
                    return;
                }
                else
                {
                    MainActivity.LogDebug("Transfer Item we are trying to get queue position of is not currently being downloaded.");
                    return;
                }


            }
            catch (System.Exception e)
            {
                //LogFirebase("GetDownloadPlaceInQueue" + e.Message);
                return;
            }
            getDownloadPlace.ContinueWith(updateTask);
        }

        public static EventHandler<TransferItem> TransferItemQueueUpdated; //for transferItemPage to update its recyclerView

        private void OnCloseClick(object sender, DialogClickEventArgs e)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.user_list_action:
                    // User chose the "Settings" item, show the app settings UI...
                    Intent intent = new Intent(SoulSeekState.MainActivityRef, typeof(UserListActivity));
                    //intent.PutExtra("SaveDataDirectoryUri", SoulSeekState.SaveDataDirectoryUri); //CURRENT SETTINGS - never necessary... static
                    SoulSeekState.MainActivityRef.StartActivityForResult(intent, 141);
                    return true;
                case Resource.Id.messages_action:
                    // User chose the "Settings" item, show the app settings UI...
                    Intent intentMessages = new Intent(SoulSeekState.MainActivityRef, typeof(MessagesActivity));
                    //intent.PutExtra("SaveDataDirectoryUri", SoulSeekState.SaveDataDirectoryUri); //CURRENT SETTINGS - never necessary... static
                    SoulSeekState.MainActivityRef.StartActivityForResult(intentMessages, 142);
                    return true;
                case Resource.Id.chatroom_action:
                    // User chose the "Settings" item, show the app settings UI...
                    Intent intentChatroom = new Intent(SoulSeekState.MainActivityRef, typeof(ChatroomActivity));
                    //intent.PutExtra("SaveDataDirectoryUri", SoulSeekState.SaveDataDirectoryUri); //CURRENT SETTINGS - never necessary... static
                    SoulSeekState.MainActivityRef.StartActivityForResult(intentChatroom, 143);
                    return true;
                case Resource.Id.settings_action:
                    Intent intent2 = new Intent(SoulSeekState.MainActivityRef, typeof(SettingsActivity));
                    //intent.PutExtra("SaveDataDirectoryUri", SoulSeekState.SaveDataDirectoryUri); //CURRENT SETTINGS - never necessary... static
                    SoulSeekState.MainActivityRef.StartActivityForResult(intent2, 140);
                    return true;
                case Resource.Id.shutdown_action:
                    Intent intent3 = new Intent(this, typeof(CloseActivity));
                    //Clear all activities and start new task
                    //ClearTask - causes any existing task that would be associated with the activity 
                    // to be cleared before the activity is started. can only be used in conjunction with NewTask.
                    // basically it clears all activities in the current task.
                    intent3.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    this.StartActivity(intent3);
                    if ((int)Android.OS.Build.VERSION.SdkInt < 21)
                    {
                        this.FinishAffinity();
                    }
                    else
                    {
                        this.FinishAndRemoveTask();
                    }
                    return true;
                case Resource.Id.about_action:
                    var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
                    //var diag = builder.SetMessage(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.about_body).TrimStart(' '), SeekerApplication.GetVersionString())).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
                    var diag = builder.SetMessage(Resource.String.about_body).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
                    diag.Show();
                    var origString = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.about_body), SeekerApplication.GetVersionString()); //this is a literal CDATA string.
                    if ((int)Android.OS.Build.VERSION.SdkInt >= 24)
                    {
                        ((TextView)diag.FindViewById(Android.Resource.Id.Message)).TextFormatted = Android.Text.Html.FromHtml(origString, Android.Text.FromHtmlOptions.ModeLegacy); //this can be slow so do NOT do it in loops...
                    }
                    else
                    {
                        ((TextView)diag.FindViewById(Android.Resource.Id.Message)).TextFormatted = Android.Text.Html.FromHtml(origString); //this can be slow so do NOT do it in loops...
                    }
                    ((TextView)diag.FindViewById(Android.Resource.Id.Message)).MovementMethod = (Android.Text.Method.LinkMovementMethod.Instance);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public static void ShowSimpleAlertDialog(Context c, int messageResourceString, int actionResourceString)
        {

            void OnCloseClick(object sender, DialogClickEventArgs e)
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            }

            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(c, Resource.Style.MyAlertDialogTheme);
            //var diag = builder.SetMessage(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.about_body).TrimStart(' '), SeekerApplication.GetVersionString())).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            var diag = builder.SetMessage(messageResourceString).SetPositiveButton(actionResourceString, OnCloseClick).Create();
            diag.Show();
        }


        public const string UPLOADS_CHANNEL_ID = "upload channel ID";
        public const string UPLOADS_CHANNEL_NAME = "Upload Notifications";
        public const string UPLOADS_NOTIF_EXTRA = "From Upload";

        public static Notification CreateUploadNotification(Context context, String username, List<String> directories, int numFiles)
        {
            string fileS = numFiles == 1 ? SoulSeekState.ActiveActivityRef.GetString(Resource.String.file) : SoulSeekState.ActiveActivityRef.GetString(Resource.String.files);
            string titleText = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.upload_f_string), numFiles, fileS, username);
            string directoryString = string.Empty;
            if (directories.Count == 1)
            {
                directoryString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.from_directory) + ": " + directories[0];
            }
            else
            {
                directoryString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.from_directories) + ": " + directories[0];
                for (int i = 0; i < directories.Count; i++)
                {
                    if (i == 0)
                    {
                        continue;
                    }
                    directoryString += ", " + directories[i];
                }
            }
            string contextText = directoryString;
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            notifIntent.AddFlags(ActivityFlags.SingleTop);
            notifIntent.PutExtra(UPLOADS_NOTIF_EXTRA, 2);
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, username.GetHashCode(), notifIntent, Helpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            Notification notification = null;
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                notification =
                      new Notification.Builder(context, UPLOADS_CHANNEL_ID)
                      .SetContentTitle(titleText)
                      .SetContentText(contextText)
                      .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                      .SetContentIntent(pendingIntent)
                      .SetOnlyAlertOnce(true) //maybe
                      .SetTicker(titleText).Build();
            }
            else
            {
                notification =
#pragma warning disable CS0618 // Type or member is obsolete
                  new Notification.Builder(context)
#pragma warning restore CS0618 // Type or member is obsolete
                  .SetContentTitle(titleText)
                  .SetContentText(contextText)
                  .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                  .SetContentIntent(pendingIntent)
                  .SetOnlyAlertOnce(true) //maybe
                  .SetTicker(titleText).Build();
            }

            return notification;
        }





        ///// <summary>
        ///// this is for global uploading event handling only.  the tabpageadapter is the one for downloading... and for upload tranferpage specific events
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void Upload_TransferProgressUpdated(object sender, TransferProgressUpdatedEventArgs e)
        //{
        //    if (e.Transfer.Direction == TransferDirection.Download)
        //    {
        //        return;
        //    }
        //    //the transfer has average speed already calcuated for us... it also has remainingtime for us too :)
        //}

        public static bool UserListContainsUser(string username)
        {
            lock (SoulSeekState.UserList)
            {
                if (SoulSeekState.UserList == null)
                {
                    return false;
                }
                return SoulSeekState.UserList.FirstOrDefault((userlistinfo) => { return userlistinfo.Username == username; }) != null;
            }
        }

        public static bool UserListSetDoesNotExist(string username)
        {
            bool found = false;
            lock (SoulSeekState.UserList)
            {
                foreach (UserListItem item in SoulSeekState.UserList)
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
        public static bool UserListAddUser(UserData userData, UserPresence? status = null)
        {
            lock (SoulSeekState.UserList)
            {
                bool found = false;
                foreach (UserListItem item in SoulSeekState.UserList)
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
                    //this is the normal case..
                    var item = new UserListItem(userData.Username);
                    item.UserData = userData;

                    //if added an ignored user, then unignore the user.  the two are mutually exclusive.....
                    if (SeekerApplication.IsUserInIgnoreList(userData.Username))
                    {
                        SeekerApplication.RemoveFromIgnoreList(userData.Username);
                    }

                    SoulSeekState.UserList.Add(item);
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
        public static bool UserListRemoveUser(string username)
        {
            lock (SoulSeekState.UserList)
            {
                UserListItem itemToRemove = null;
                foreach (UserListItem item in SoulSeekState.UserList)
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
                SoulSeekState.UserList.Remove(itemToRemove);
                return true;
            }

        }




        /// <summary>
        ///     Creates and returns an <see cref="IEnumerable{T}"/> of <see cref="Soulseek.Directory"/> in response to a remote request.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="endpoint">The IP endpoint of the requesting user.</param>
        /// <returns>A Task resolving an IEnumerable of Soulseek.Directory.</returns>
        private static Task<BrowseResponse> BrowseResponseResolver(string username, IPEndPoint endpoint)
        {
            if (SeekerApplication.IsUserInIgnoreList(username))
            {
                return Task.FromResult(new BrowseResponse(Enumerable.Empty<Directory>()));
            }
            return Task.FromResult(SoulSeekState.SharedFileCache.GetBrowseResponseForUser(username));
        }

        /// <summary>
        ///     Creates and returns a <see cref="Soulseek.Directory"/> in response to a remote request.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="endpoint">The IP endpoint of the requesting user.</param>
        /// <param name="token">The unique token for the request, supplied by the requesting user.</param>
        /// <param name="directory">The requested directory.</param>
        /// <returns>A Task resolving an instance of Soulseek.Directory containing the contents of the requested directory.</returns>
        private static Task<Soulseek.Directory> DirectoryContentsResponseResolver(string username, IPEndPoint endpoint, int token, string directory)
        {
            //the directory is the presentable name.
            //the old EndsWith(dir) fails if the directory is not unique i.e. document structure of Soulseek Complete > some dirs and files, Soulseek Complete > more dirs and files..
            Tuple<string, string> fullDirUri = SoulSeekState.SharedFileCache.FriendlyDirNameToUriMapping.Where((Tuple<string, string> t) => { return t.Item1 == directory; }).FirstOrDefault(); //TODO DICTIONARY>>>>>

            if (fullDirUri == null)
            {
                //as fallback safety.  I dont think this will ever happen.....
                fullDirUri = SoulSeekState.SharedFileCache.FriendlyDirNameToUriMapping.Where((Tuple<string, string> t) => { return t.Item1.EndsWith(directory); }).FirstOrDefault();
            }
            if (fullDirUri == null)
            {
                //could not find...
            }
            DocumentFile fullDir = null;
            if (SoulSeekState.PreOpenDocumentTree() || !UploadDirectoryManager.IsFromTree(fullDirUri.Item2)) //todo
            {
                fullDir = DocumentFile.FromFile(new Java.IO.File(Android.Net.Uri.Parse(fullDirUri.Item2).Path));
            }
            else
            {
                fullDir = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, Android.Net.Uri.Parse(fullDirUri.Item2));
            }
            //Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri).Path
            var slskDir = SlskDirFromDocumentFile(fullDir, true, GetVolumeName(fullDir.Uri.LastPathSegment, false, out _));
            slskDir = new Directory(directory, slskDir.Files);
            return Task.FromResult(slskDir);
        }

        public static void TurnOnSharing()
        {
            SoulSeekState.SoulseekClient.Options.SetSharedHandlers(BrowseResponseResolver, SearchResponseResolver, DirectoryContentsResponseResolver, EnqueueDownloadAction);
        }

        public static void TurnOffSharing()
        {
            SoulSeekState.SoulseekClient.Options.NullSharedHandlers();
        }

        /// <summary>
        /// Do this on any changes (like in Settings) but also on Login.
        /// </summary>
        /// <param name="informServerOfChangeIfThereIsAChange"></param>
        /// <param name="force">force if we are chaning the upload directory...</param>
        public static void SetUnsetSharingBasedOnConditions(bool informServerOfChangeIfThereIsAChange, bool force = false)
        {
            bool wasShared = SoulSeekState.SoulseekClient.Options.SearchResponseResolver != null; //when settings gets recreated can get nullref here.
            if (MeetsCurrentSharingConditions())
            {
                TurnOnSharing();
                if (!wasShared || force)
                {
                    MainActivity.LogDebug("sharing state changed to ON");
                    InformServerOfSharedFiles();
                }
            }
            else
            {
                TurnOffSharing();
                if (wasShared)
                {
                    MainActivity.LogDebug("sharing state changed to OFF");
                    InformServerOfSharedFiles();
                }
            }
        }

        /// <summary>
        /// Has set things up properly and has sharing on.
        /// </summary>
        /// <returns></returns>
        public static bool MeetsSharingConditions()
        {
            return SoulSeekState.SharingOn && UploadDirectoryManager.UploadDirectories.Count != 0 && !SoulSeekState.IsParsing && !UploadDirectoryManager.AreAllFailed();
        }

        /// <summary>
        /// Has set things up properly and has sharing on + their network settings currently allow it.
        /// </summary>
        /// <returns></returns>
        public static bool MeetsCurrentSharingConditions()
        {
            return MeetsSharingConditions() && SoulSeekState.IsNetworkPermitting();
        }

        public static bool IsSharingSetUpSuccessfully()
        {
            if (SoulSeekState.SharedFileCache == null || !SoulSeekState.SharedFileCache.SuccessfullyInitialized)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static Tuple<SharingIcons, string> GetSharingMessageAndIcon(out bool isParsing)
        {
            isParsing = false;
            if (MeetsSharingConditions() && IsSharingSetUpSuccessfully())
            {
                //try to parse this into a path: SoulSeekState.ShareDataDirectoryUri
                if(MeetsCurrentSharingConditions())
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.On, SoulSeekState.ActiveActivityRef.GetString(Resource.String.success_sharing));
                }
                else
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.OffDueToNetwork, "Sharing disabled on metered connection");
                }
            }
            else if (MeetsSharingConditions() && !IsSharingSetUpSuccessfully())
            {
                if (SoulSeekState.SharedFileCache == null)
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.Off, "Not yet initialized.");
                }
                else
                {
                    return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_share_not_set));
                }
            }
            else if (!SoulSeekState.SharingOn)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Off, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_off));
            }
            else if (SoulSeekState.IsParsing)
            {
                isParsing = true;
                return new Tuple<SharingIcons, string>(SharingIcons.CurrentlyParsing, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_currently_parsing));
            }
            else if (SoulSeekState.FailedShareParse)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_failure_parsing));
            }
            else if (UploadDirectoryManager.UploadDirectories.Count == 0)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_share_not_set));
            }
            else if (UploadDirectoryManager.AreAllFailed())
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_error)); //TODO get error
            }
            else
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_error));
            }
        }

        public void SetUpLoginContinueWith(Task t)
        {
            if (t == null)
            {
                return;
            }
            if (MeetsSharingConditions())
            {

                Action<Task> getAndSetLoggedInInfoAction = new Action<Task>((Task t) =>
                {
                    //we want to 
                    //UpdateStatus ??
                    //inform server if we are sharing..
                    //get our upload speed..
                    if (t.Status == TaskStatus.Faulted || t.IsFaulted || t.IsCanceled)
                    {
                        return;
                    }
                    InformServerOfSharedFiles(); //dont need to get the result of this one.
                    SoulSeekState.SoulseekClient.GetUserDataAsync(SoulSeekState.Username); //the result of this one if from an event handler..
                    //.ContinueWith(
                    //    (Task<UserData> userDataTask) =>
                    //    {
                    //        if(userDataTask.IsFaulted)
                    //        {
                    //            LogFirebase("userDataTask is faulted " + userDataTask.Exception);
                    //            return;
                    //        }
                    //        else
                    //        {
                    //            //userDataTask.Result.AverageSpeed;
                    //        }

                    //    });

                });
                t.ContinueWith(getAndSetLoggedInInfoAction);
            }
        }

        public bool OnBrowseTab()
        {
            try
            {
                var pager = (AndroidX.ViewPager.Widget.ViewPager)FindViewById(Resource.Id.pager);
                return pager.CurrentItem == 3;
            }
            catch
            {
                MainActivity.LogFirebase("OnBrowseTab failed");
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void OnBackPressed()
        {
            bool relevant = false;
            try
            {
                //TabLayout tabs = (TabLayout)FindViewById(Resource.Id.tabs); returns -1
                var pager = (AndroidX.ViewPager.Widget.ViewPager)FindViewById(Resource.Id.pager);
                if (pager.CurrentItem == 3) //browse tab
                {
                    relevant = BrowseFragment.Instance.BackButton();
                }
                else if (pager.CurrentItem == 2) //transfer tab
                {
                    if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                    {
                        if (TransfersFragment.InUploadsMode)
                        {
                            TransfersFragment.CurrentlySelectedUploadFolder = null;
                        }
                        else
                        {
                            TransfersFragment.CurrentlySelectedDLFolder = null;
                        }
                        SetTransferSupportActionBarState();
                        this.InvalidateOptionsMenu();
                        //((pager.Adapter as TabsPagerAdapter).GetItem(2) as TransfersFragment).SetRecyclerAdapter();  //if you go to transfers rotate phone and then OnBackPressed gets hit,. the fragment that getitem returns will be very old.
                        //((pager.Adapter as TabsPagerAdapter).GetItem(2) as TransfersFragment).RestoreScrollPosition();
                        StaticHacks.TransfersFrag.SetRecyclerAdapter();
                        StaticHacks.TransfersFrag.RestoreScrollPosition();
                        relevant = true;
                    }
                }
            }
            catch (Exception e)
            {
                //During Back Button: Attempt to invoke virtual method 'java.lang.Object android.content.Context.getSystemService(java.lang.String)' on a null object reference
                MainActivity.LogFirebase("During Back Button: " + e.Message);
            }
            if (!relevant)
            {
                base.OnBackPressed();
            }
        }

        public static void DebugLogHandler(object sender, SoulseekClient.ErrorLogEventArgs e)
        {
            MainActivity.LogDebug(e.Message);
        }

        public static void SoulseekClient_ErrorLogHandler(object sender, SoulseekClient.ErrorLogEventArgs e)
        {
            if (e?.Message != null)
            {
                if (e.Message.Contains("Operation timed out"))
                {
                    //this happens to me all the time and it is literally fine
                    return;
                }
            }
            MainActivity.LogFirebase(e.Message);
        }

        public static bool IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(Action<Task> action, string msg = null, Context contextToUseForMessage = null)
        {
            lock (SeekerApplication.OurCurrentLoginTaskSyncObject)
            {
                //old: SoulSeekState.SoulseekClient.State.HasFlag(Soulseek.SoulseekClientStates.Connecting) || SoulSeekState.SoulseekClient.State.HasFlag(Soulseek.SoulseekClientStates.LoggingIn). this is not good enough since you can still pass this if you are connected but not yet Logging In!
                if (!SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || !SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
                {
                    //MainActivity.LogDebug("IsLoggingInTaskCurrentlyBeingPerformed: TRUE");
                    SeekerApplication.OurCurrentLoginTask = SeekerApplication.OurCurrentLoginTask.ContinueWith(action, System.Threading.CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                    if (msg != null)
                    {
                        if (contextToUseForMessage == null)
                        {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, ToastLength.Short).Show();
                        }
                        else
                        {
                            Toast.MakeText(contextToUseForMessage, msg, ToastLength.Short).Show();
                        }
                    }
                    return true;
                }
                else
                {
                    //MainActivity.LogDebug("IsLoggingInTaskCurrentlyBeingPerformed: FALSE");
                    return false;
                }
            }
        }

        public static bool ShowMessageAndCreateReconnectTask(Context c, bool silent, out Task connectTask)
        {
            if (c == null)
            {
                c = SoulSeekState.MainActivityRef;
            }
            if (Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread()) //tested..
            {
                if(!silent)
                {
                    Toast tst = Toast.MakeText(c, c.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                    tst.Show();
                }
            }
            else
            {
                if(!silent)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(
                    () =>
                    {
                        Toast tst = Toast.MakeText(c, c.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                        tst.Show();
                    });
                }
            }
            //if we are still not connected then creating the task will throw. 
            //also if the async part of the task fails we will get task.faulted.
            try
            {
                connectTask = SeekerApplication.ConnectAndPerformPostConnectTasks(SoulSeekState.Username, SoulSeekState.Password);
                return true;
            }
            catch
            {
                if (!silent)
                {
                    Toast tst2 = Toast.MakeText(c, c.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                    tst2.Show();
                }
            }
            connectTask = null;
            return false;
        }

        public static bool CurrentlyLoggedInButDisconnectedState()
        {
            return (SoulSeekState.currentlyLoggedIn &&
                (SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnected) || SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnecting)));
        }

        public static void SetStatusApi(bool away)
        {
            if(IsNotLoggedIn())
            {
                return;
            }
            if(!SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || !SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                //dont log in just for this.
                //but if we later connect while still in the background, it may be best to set a flag.
                //do it when we log in... since we could not set it now...
                SoulSeekState.PendingStatusChangeToAwayOnline = away ? SoulSeekState.PendingStatusChange.AwayPending : SoulSeekState.PendingStatusChange.OnlinePending;
                return;
            }
            try
            {
                SoulSeekState.SoulseekClient.SetStatusAsync(away ? UserPresence.Away : UserPresence.Online).ContinueWith((Task t)=>
                {
                    if(t.IsCompletedSuccessfully)
                    {
                        SoulSeekState.PendingStatusChangeToAwayOnline = SoulSeekState.PendingStatusChange.NothingPending;
                        SoulSeekState.OurCurrentStatusIsAway = away;
                        string statusString = away ? "away" : "online"; //not user facing
                        MainActivity.LogDebug($"We successfully changed our status to {statusString}");
                    }
                    else
                    {
                        MainActivity.LogDebug("SetStatusApi FAILED " + t.Exception?.Message);
                    }
                });
            }
            catch(Exception e)
            {
                MainActivity.LogDebug("SetStatusApi FAILED " + e.Message + e.StackTrace);
            }
        }

        private void UpdateForScreenSize()
        {
            if (!SoulSeekState.IsLowDpi()) return;
            try
            {
                TabLayout tabs = (TabLayout)FindViewById(Resource.Id.tabs);
                LinearLayout vg = (LinearLayout)tabs.GetChildAt(0);
                int tabsCount = vg.ChildCount;
                for (int j = 0; j < tabsCount; j++)
                {
                    ViewGroup vgTab = (ViewGroup)vg.GetChildAt(j);
                    int tabChildsCount = vgTab.ChildCount;
                    for (int i = 0; i < tabChildsCount; i++)
                    {
                        View tabViewChild = vgTab.GetChildAt(i);
                        if (tabViewChild is TextView)
                        {
                            ((TextView)tabViewChild).SetAllCaps(false);
                        }
                    }
                }
            }
            catch
            {
                //not worth throwing over..
            }
        }

        public void RecreateFragment(AndroidX.Fragment.App.Fragment f)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)//Build.VERSION_CODES.N)
            {
                SupportFragmentManager.BeginTransaction().Detach(f).CommitNowAllowingStateLoss();//hisbeginTransaction().detach(fragment).commitNow()
                SupportFragmentManager.BeginTransaction().Attach(f).CommitNowAllowingStateLoss();
            }
            else
            {
                //SupportFragmentManager
                SupportFragmentManager.BeginTransaction().Detach(f).Attach(f).CommitNow();
                //supportFragmentManager.beginTransaction().detach(fragment).attach(fragment).commitNow()
            }
        }

        /// <summary>
        ///     Creates and returns a <see cref="SearchResponse"/> in response to the given <paramref name="query"/>.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The search token.</param>
        /// <param name="query">The search query.</param>
        /// <returns>A Task resolving a SearchResponse, or null.</returns>
        private static Task<SearchResponse> SearchResponseResolver(string username, int token, SearchQuery query)
        {
            var defaultResponse = Task.FromResult<SearchResponse>(null);

            // some bots continually query for very common strings.  blacklist known names here.
            var blacklist = new[] { "Lola45", "Lolo51", "rajah" };
            if (blacklist.Contains(username))
            {
                return defaultResponse;
            }
            if (SeekerApplication.IsUserInIgnoreList(username))
            {
                return defaultResponse;
            }
            // some bots and perhaps users search for very short terms.  only respond to queries >= 3 characters.  sorry, U2 fans.
            if (query.Query.Length < 5)
            {
                return defaultResponse;
            }

            if (SoulSeekState.Username == null || SoulSeekState.Username == string.Empty || SoulSeekState.SharedFileCache == null)
            {
                return defaultResponse;
            }

            var results = SoulSeekState.SharedFileCache.Search(query, username, out IEnumerable<Soulseek.File> lockedResults);

            if (results.Any() || lockedResults.Any())
            {
                //Console.WriteLine($"[SENDING SEARCH RESULTS]: {results.Count()} records to {username} for query {query.SearchText}");
                int ourUploadSpeed = 1024 * 256;
                if (SoulSeekState.UploadSpeed > 0)
                {
                    ourUploadSpeed = SoulSeekState.UploadSpeed;
                }
                return Task.FromResult(new SearchResponse(
                    SoulSeekState.Username,
                    token,
                    freeUploadSlots: 1,
                    uploadSpeed: ourUploadSpeed,
                    queueLength: 0,
                    fileList: results,
                    lockedFileList: lockedResults));
            }

            // if no results, either return null or an instance of SearchResponse with a fileList of length 0
            // in either case, no response will be sent to the requestor.
            return Task.FromResult<SearchResponse>(null);
        }


        public static string GetLastPathSegment(string uri)
        {
            return Android.Net.Uri.Parse(uri).LastPathSegment;
        }


        /// <summary>
        ///     Invoked upon a remote request to download a file.    THE ORIGINAL BUT WITHOUT ITRANSFERTRACKER!!!!
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="endpoint">The IP endpoint of the requesting user.</param>
        /// <param name="filename">The filename of the requested file.</param>
      //  /// <param name="tracker">(for example purposes) the ITransferTracker used to track progress.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="DownloadEnqueueException">Thrown when the download is rejected.  The Exception message will be passed to the remote user.</exception>
        /// <exception cref="Exception">Thrown on any other Exception other than a rejection.  A generic message will be passed to the remote user for security reasons.</exception>
        private static Task EnqueueDownloadAction(string username, IPEndPoint endpoint, string filename)
        {
            if (SeekerApplication.IsUserInIgnoreList(username))
            {
                return Task.CompletedTask;
            }

            //if a user tries to download a file from our browseResponse then their filename will be
            //  "Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\y\\x\\09 Between Songs 4.mp3" 
            //so check if it contains the uploadDataDirectoryUri

            //if(filename.Contains(uploadDirfolderName))
            //{
            //    string newFolderName = Helpers.GetFolderNameFromFile(filename);
            //    string newFileName = Helpers.GetFileNameFromFile(filename);
            //    keyFilename = newFolderName + @"\" + newFileName;
            //}

            //the filename is basically "the key"
            _ = endpoint;
            string errorMsg = null;
            Tuple<long, string, Tuple<int, int, int, int>, bool, bool> ourFileInfo = SoulSeekState.SharedFileCache.GetFullInfoFromSearchableName(filename, out errorMsg);//SoulSeekState.SharedFileCache.FullInfo.Where((Tuple<string,string,long> fullInfoTuple) => {return fullInfoTuple.Item1 == keyFilename; }).FirstOrDefault(); //make this a method call GetFullInfo and check Aux dict
            if (ourFileInfo == null)
            {
                LogFirebase("ourFileInfo is null: " + ourFileInfo + " " + errorMsg);
                throw new DownloadEnqueueException($"File not found.");
            }

            DocumentFile ourFile = null;
            Android.Net.Uri ourUri = Android.Net.Uri.Parse(ourFileInfo.Item2);

            if(ourFileInfo.Item4 || ourFileInfo.Item5)
            {
                //locked or hidden (hidden shouldnt happen but just in case, it should still be userlist only)
                //CHECK USER LIST
                if(!CommonHelpers.UserListChecker.IsInUserList(username))
                {
                    throw new DownloadEnqueueException($"File not shared");
                }
            }

            if (SoulSeekState.PreOpenDocumentTree() || !UploadDirectoryManager.IsFromTree(filename)) //IsFromTree method!
            {
                ourFile = DocumentFile.FromFile(new Java.IO.File(ourUri.Path));
            }
            else
            {
                ourFile = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, ourUri);
            }
            //var localFilename = filename.ToLocalOSPath();
            //var fileInfo = new FileInfo(localFilename);



            if (!ourFile.Exists())
            {
                //Console.WriteLine($"[UPLOAD REJECTED] File {localFilename} not found.");
                throw new DownloadEnqueueException($"File not found.");
            }

            //if (tracker.TryGet(TransferDirection.Upload, username, filename, out _))
            //{
            //    // in this case, a re-requested file is a no-op.  normally we'd want to respond with a 
            //    // PlaceInQueueResponse
            //    //Console.WriteLine($"[UPLOAD RE-REQUESTED] [{username}/{filename}]");
            //    return Task.CompletedTask;
            //}

            // create a new cancellation token source so that we can cancel the upload from the UI.
            var cts = new CancellationTokenSource();
            //var topts = new TransferOptions(stateChanged: (e) => tracker.AddOrUpdate(e, cts), progressUpdated: (e) => tracker.AddOrUpdate(e, cts), governor: (t, c) => Task.Delay(1, c));

            TransferItem transferItem = new TransferItem();
            transferItem.Username = username;
            transferItem.FullFilename = filename;
            transferItem.Filename = Helpers.GetFileNameFromFile(filename);
            transferItem.FolderName = Helpers.GetFolderNameFromFile(filename);
            transferItem.CancellationTokenSource = cts;
            transferItem.Size = ourFile.Length();
            transferItem.isUpload = true;
            transferItem = TransfersFragment.TransferItemManagerUploads.AddIfNotExistAndReturnTransfer(transferItem, out bool exists);

            if (!exists) //else the state will simply be updated a bit later. 
            {
                TransferAddedUINotify?.Invoke(null, transferItem);
            }
            // accept all download requests, and begin the upload immediately.
            // normally there would be an internal queue, and uploads would be handled separately.
            Task.Run(async () =>
            {
                CancellationTokenSource oldCts = null;
                try
                {
                    using var stream = SoulSeekState.MainActivityRef.ContentResolver.OpenInputStream(ourFile.Uri); //outputstream.CanRead is false...
                    //using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);

                    TransfersFragment.SetupCancellationToken(transferItem, cts, out oldCts);

                    await SoulSeekState.SoulseekClient.UploadAsync(username, filename, transferItem.Size, stream, options: new TransferOptions(governor: SeekerApplication.SpeedLimitHelper.OurUploadGoverner), cancellationToken: cts.Token); //THE FILENAME THAT YOU PASS INTO HERE MUST MATCH EXACTLY
                                                                                                                                                                                                                                               //ELSE THE CLIENT WILL REJECT IT.  //MUST MATCH EXACTLY THE ONE THAT WAS REQUESTED THAT IS..

                }
                catch (DuplicateTransferException dup) //not tested
                {
                    LogDebug("UPLOAD DUPL - " + dup.Message);
                    TransfersFragment.SetupCancellationToken(transferItem, oldCts, out _); //if there is a duplicate you do not want to overwrite the good cancellation token with a meaningless one. so restore the old one.
                }
                catch (DuplicateTokenException dup)
                {
                    LogDebug("UPLOAD DUPL - " + dup.Message);
                    TransfersFragment.SetupCancellationToken(transferItem, oldCts, out _); //if there is a duplicate you do not want to overwrite the good cancellation token with a meaningless one. so restore the old one.
                }
            }).ContinueWith(t =>
            {
                //Console.WriteLine($"[UPLOAD FAILED] {t.Exception}");
            }, TaskContinuationOptions.NotOnRanToCompletion); // fire and forget

            // return a completed task so that the invoking code can respond to the remote client.
            return Task.CompletedTask;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (NEW_WRITE_EXTERNAL == requestCode || NEW_WRITE_EXTERNAL_VIA_LEGACY == requestCode || NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
            {
                Action showDirectoryButton = new Action(() =>
                {
                    ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_error));
                    AddLoggedInLayout(StaticHacks.LoginFragment.View); //todo: nullref
                    if (!SoulSeekState.currentlyLoggedIn)
                    {
                        MainActivity.BackToLogInLayout(StaticHacks.LoginFragment.View, (StaticHacks.LoginFragment as LoginFragment).LogInClick);
                    }
                    if (StaticHacks.LoginFragment.View == null)//this can happen...
                    {   //.View is a method so it can return null.  I tested it on MainActivity.OnPause and it was in fact null.
                        ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_choose_settings));
                        LogFirebase("StaticHacks.LoginFragment.View is null");
                        return;
                    }
                    Button bttn = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.mustSelectDirectory);
                    Button bttnLogout = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.buttonLogout);
                    if (bttn != null)
                    {
                        bttn.Visibility = ViewStates.Visible;
                        bttn.Click += MustSelectDirectoryClick;
                    }
                });

                if(NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
                {
                    //the resultCode will always be Cancelled for this since you have to back out of it.
                    //so instead we check Android.OS.Environment.IsExternalStorageManager
                    if(SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
                    {
                        //phase 2 - actually pick a file.
                        FallbackFileSelection(NEW_WRITE_EXTERNAL_VIA_LEGACY);
                        return;
                    }
                    else
                    {
                        if (OnUIthread())
                        {
                            showDirectoryButton();
                        }
                        else
                        {
                            RunOnUiThread(showDirectoryButton);
                        }
                        return;
                    }
                }


                if (resultCode == Result.Ok)
                {
                    if(NEW_WRITE_EXTERNAL == requestCode)
                    {
                        var x = data.Data;
                        SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
                        SoulSeekState.SaveDataDirectoryUri = data.Data.ToString();
                        SoulSeekState.SaveDataDirectoryUriIsFromTree = true;
                        this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    }
                    else if(NEW_WRITE_EXTERNAL_VIA_LEGACY == requestCode)
                    {
                        SoulSeekState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(data.Data.Path));
                        SoulSeekState.SaveDataDirectoryUri = data.Data.ToString();
                        SoulSeekState.SaveDataDirectoryUriIsFromTree = false;
                    }
                }
                else
                {

                    if (OnUIthread())
                    {
                        showDirectoryButton();
                    }
                    else
                    {
                        RunOnUiThread(showDirectoryButton);
                    }

                    //throw new Exception("Seeker requires access to a directory where it can store files.");
                }
            }
            else if (MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL == requestCode || 
                MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY == requestCode || 
                MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
            {

                Action reiterate = new Action(() =>
                {
                    ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_error));
                });

                Action hideButton = new Action(() =>
                {
                    Button bttn = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.mustSelectDirectory);
                    bttn.Visibility = ViewStates.Gone;
                });

                if(MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
                {
                    //the resultCode will always be Cancelled for this since you have to back out of it.
                    //so instead we check Android.OS.Environment.IsExternalStorageManager
                    if (SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
                    {
                        //phase 2 - actually pick a file.
                        FallbackFileSelection(MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY);
                        return;
                    }
                    else
                    {
                        if (OnUIthread())
                        {
                            reiterate();
                        }
                        else
                        {
                            RunOnUiThread(reiterate);
                        }
                        return;
                    }
                }


                if (resultCode == Result.Ok)
                {
                    if(MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY == requestCode)
                    {
                        SoulSeekState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(data.Data.Path));
                        SoulSeekState.SaveDataDirectoryUri = data.Data.ToString();
                        SoulSeekState.SaveDataDirectoryUriIsFromTree = false;
                    }
                    else if(MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL == requestCode)
                    {
                        SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
                        SoulSeekState.SaveDataDirectoryUri = data.Data.ToString();
                        SoulSeekState.SaveDataDirectoryUriIsFromTree = true;
                        this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    }

                    //hide the button

                    if (OnUIthread())
                    {
                        hideButton();
                    }
                    else
                    {
                        RunOnUiThread(hideButton);
                    }
                }
                else
                {
                    if (OnUIthread())
                    {
                        reiterate();
                    }
                    else
                    {
                        RunOnUiThread(reiterate);
                    }

                    //throw new Exception("Seeker requires access to a directory where it can store files.");
                }
            }
            else if (SETTINGS_EXTERNAL == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    //get settings and set our things.
                }
                else if (resultCode == Result.Canceled)
                {
                    //do nothing...
                }
            }
            //else
            //{
            //    base.OnActivityResult(requestCode, resultCode, data);
            //}
        }

        private void MustSelectDirectoryClick(object sender, EventArgs e)
        {
            var storageManager = Android.OS.Storage.StorageManager.FromContext(this);
            var intent = storageManager.PrimaryStorageVolume.CreateOpenDocumentTreeIntent();
            intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPrefixUriPermission);
            Android.Net.Uri res = null; //var y = MediaStore.Audio.Media.ExternalContentUri.ToString();
            if (string.IsNullOrEmpty(SoulSeekState.SaveDataDirectoryUri))
            {
                //try
                //{
                //    //storage/emulated/0/music
                //    Java.IO.File f = new Java.IO.File(@"/storage/emulated/0/Music");///Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic);
                //    //res = f.ToURI();
                //    res = Android.Net.Uri.FromFile(f);//Parse(f.ToURI().ToString());
                //}
                //catch
                //{
                //    res = Android.Net.Uri.Parse(defaultMusicUri);//TryCreate("content://com.android.externalstorage.documents/tree/primary%3AMusic", UriKind.Absolute,out res);
                //}
                res = Android.Net.Uri.Parse(defaultMusicUri);
            }
            else
            {
                res = Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri);
            }
            intent.PutExtra(DocumentsContract.ExtraInitialUri, res);
            try
            {
                this.StartActivityForResult(intent, MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL);
                //if(1==1)
                //{
                //    throw new Exception(Helpers.NoDocumentOpenTreeToHandle);
                //}

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(Helpers.NoDocumentOpenTreeToHandle))
                {
                    FallbackFileSelectionEntry(true);
                }
                else
                {
                    throw ex;
                }
            }
        }

        private void FallbackFileSelectionEntry(bool mustSelectDirectoryButton)
        {
            bool hasManageAllFilesManisfestPermission = false;

#if IzzySoft
            hasManageAllFilesManisfestPermission = true;
#endif

            if (SoulSeekState.RequiresEitherOpenDocumentTreeOrManageAllFiles() && hasManageAllFilesManisfestPermission && !Android.OS.Environment.IsExternalStorageManager) //this is "step 1"
            {
                Intent allFilesPermission = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                Android.Net.Uri packageUri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                allFilesPermission.SetData(packageUri);
                this.StartActivityForResult(allFilesPermission, mustSelectDirectoryButton ? MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen : NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen);
            }
            else if(SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
            {
                FallbackFileSelection(mustSelectDirectoryButton ? MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY : NEW_WRITE_EXTERNAL_VIA_LEGACY);
            }
            else
            {


                if(SoulSeekState.RequiresEitherOpenDocumentTreeOrManageAllFiles() && !hasManageAllFilesManisfestPermission)
                {
                    ShowSimpleAlertDialog(this, Resource.String.error_no_file_manager_dir_manage_storage, Resource.String.okay);
                }
                else
                {
                    Toast.MakeText(this, SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_no_file_manager_dir), ToastLength.Long).Show();
                }


                //Note:
                //If your app targets Android 12 (API level 31) or higher, its toast is limited to two lines of text and shows the application icon next to the text.
                //Be aware that the line length of this text varies by screen size, so it's good to make the text as short as possible.
                //on Pixel 5 emulator this limit is around 78 characters.
                //^It must BOTH target Android 12 AND be running on Android 12^
            }
        }

        private void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            e.Handled = false; //make sure we still crash.. we just want to clean up..
            try
            {
                //save transfers state !!!
                TransfersFragment.SaveTransferItems(sharedPreferences);
            }
            catch
            {

            }
            try
            {
                //stop dl service..
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                MainActivity.LogDebug("Stop Service");
                this.StopService(downloadServiceIntent);
            }
            catch
            {

            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="e"></param>
        ///// <param name="c"></param>
        //public static void ShowAlert(Exception e, Context c)
        //{
        //    var b = new Android.App.AlertDialog.Builder(c);
        //    b.SetTitle("An Unhandled Exception Occured"); 
        //    b.SetMessage(e.Message + 
        //        System.Environment.NewLine + 
        //        System.Environment.NewLine + 
        //        e.StackTrace);
        //    b.Show();
        //}

        //public void LogWriter(string logMessage)
        //{
        //    LogWrite(logMessage);
        //}
        //public void LogWrite(string logMessage)
        //{
        //    var m_exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        //    try
        //    {
        //        using (System.IO.StreamWriter w = System.IO.File.AppendText(m_exePath + "\\" + "log.txt"))
        //        {
        //            w.WriteLine("Testing testing testing");
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        //public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        //{
        //    if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
        //    {
        //        return;
        //    }
        //    else
        //    {
        //        FinishAndRemoveTask();
        //    }
        //}

        //public static void SoulSeekState_DownloadAdded(object sender, DownloadAddedEventArgs e)
        //{
        //    MainActivity.LogDebug("SoulSeekState_DownloadAdded");
        //    TransferItem transferItem = new TransferItem();
        //    transferItem.Filename = Helpers.GetFileNameFromFile(e.dlInfo.fullFilename);
        //    transferItem.FolderName = Helpers.GetFolderNameFromFile(e.dlInfo.fullFilename);
        //    transferItem.Username = e.dlInfo.username;
        //    transferItem.FullFilename = e.dlInfo.fullFilename;
        //    transferItem.Size = e.dlInfo.Size;
        //    transferItem.QueueLength = e.dlInfo.QueueLength;
        //    e.dlInfo.TransferItemReference = transferItem;

        //    TransfersFragment.SetupCancellationToken(transferItem, e.dlInfo.CancellationTokenSource, out _);
        //    //transferItem.CancellationTokenSource = e.dlInfo.CancellationTokenSource;
        //    //if (!CancellationTokens.TryAdd(ProduceCancellationTokenKey(transferItem), e.dlInfo.CancellationTokenSource))
        //    //{
        //    //    //likely old already exists so just replace the old one
        //    //    CancellationTokens[ProduceCancellationTokenKey(transferItem)] = e.dlInfo.CancellationTokenSource;
        //    //}

        //    //once task completes, write to disk
        //    Action<Task> continuationActionSaveFile = DownloadContinuationActionUI(e);
        //    e.dlInfo.downloadTask.ContinueWith(continuationActionSaveFile);

        //    TransfersFragment.TransferItemManagerDL.Add(transferItem);
        //    MainActivity.DownloadAddedUINotify?.Invoke(null, e);
        //}

        /// <summary>
        /// This RETURNS the task for Continuewith
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Action<Task> DownloadContinuationActionUI(DownloadAddedEventArgs e)
        {
            Action<Task> continuationActionSaveFile = new Action<Task>(
            task =>
            {
                try
                {
                    Action action = null;
                    if (task.IsCanceled)
                    {
                        MainActivity.LogDebug((DateTimeOffset.Now.ToUnixTimeMilliseconds() - SoulSeekState.TaskWasCancelledToastDebouncer).ToString());
                        if ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - SoulSeekState.TaskWasCancelledToastDebouncer) > 1000)
                        {
                            SoulSeekState.TaskWasCancelledToastDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        }

                        if (e.dlInfo.TransferItemReference.CancelAndRetryFlag) //if we pressed "Retry Download" and it was in progress so we first had to cancel...
                        {
                            e.dlInfo.TransferItemReference.CancelAndRetryFlag = false;
                            try
                            {
                                //retry download.
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                                Android.Net.Uri incompleteUri = null;
                                TransfersFragment.SetupCancellationToken(e.dlInfo.TransferItemReference, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                                Task retryTask = DownloadDialog.DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.TransferItemReference.Size, cancellationTokenSource, 1, e.dlInfo.TransferItemReference.ShouldEncodeFileLatin1(), e.dlInfo.TransferItemReference.ShouldEncodeFolderLatin1());
                                retryTask.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.TransferItemReference.Size, retryTask, cancellationTokenSource, e.dlInfo.QueueLength, 0, task.Exception, e.dlInfo.Depth))));
                            }
                            catch (System.Exception e)
                            {
                                //disconnected error
                                if(e is System.InvalidOperationException && e.Message.ToLower().Contains("server connection must be connected and logged in"))
                                {
                                    action = () => { ToastUIWithDebouncer(SeekerApplication.GetString(Resource.String.MustBeLoggedInToRetryDL), "_16_"); };
                                }
                                else
                                {
                                    MainActivity.LogFirebase("cancel and retry creation failed: " + e.Message + e.StackTrace);
                                }
                                if(action != null)
                                {
                                    SoulSeekState.ActiveActivityRef.RunOnUiThread(action);
                                }
                            }
                        }

                        if (e.dlInfo.TransferItemReference.CancelAndClearFlag)
                        {
                            MainActivity.LogDebug("continue with cleanup activity: " + e.dlInfo.fullFilename);
                            e.dlInfo.TransferItemReference.CancelAndRetryFlag = false;
                            e.dlInfo.TransferItemReference.InProcessing = false;
                            TransferItemManagerWrapper.PerformCleanupItem(e.dlInfo.TransferItemReference); //this way we are sure that the stream is closed.
                        }

                        return;
                    }
                    else if (task.Status == TaskStatus.Faulted)
                    {
                        bool retriable = false;
                        bool forceRetry = false;

                        // in the cases where there is mojibake, and you undo it, you still cannot download from Nicotine older client.
                        // reason being: the shared cache and disk do not match.
                        // so if you send them the filename on disk they will say it is not in the cache.
                        // and if you send them the filename from cache they will say they could not find it on disk.

                        //bool tryUndoMojibake = false; //this is still needed even with keeping track of encodings.
                        bool resetRetryCount = false;
                        var transferItem = e.dlInfo.TransferItemReference;
                        //bool wasTriedToUndoMojibake = transferItem.TryUndoMojibake;
                        //transferItem.TryUndoMojibake = false;
                        if (task.Exception.InnerException is System.TimeoutException)
                        {
                            action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.timeout_peer)); };
                        }
                        else if(task.Exception.InnerException is TransferSizeMismatchException sizeException)
                        {
                            // THIS SHOULD NEVER HAPPEN. WE FIX THE TRANSFER SIZE MISMATCH INLINE.

                            // update the size and rerequest.
                            // if we have partially downloaded the file already we need to delete it to prevent corruption.
                            MainActivity.LogDebug($"OLD SIZE {transferItem.Size} NEW SIZE {sizeException.RemoteSize}");
                            transferItem.Size = sizeException.RemoteSize;
                            e.dlInfo.Size = sizeException.RemoteSize;
                            retriable = true;
                            forceRetry = true;
                            resetRetryCount = true;
                            if (!string.IsNullOrEmpty(transferItem.IncompleteParentUri)/* && transferItem.Progress > 0*/)
                            {
                                try
                                {
                                    TransferItemManagerWrapper.PerformCleanupItem(transferItem);
                                }
                                catch(Exception ex)
                                {
                                    string exceptionString = "Failed to delete incomplete file on TransferSizeMismatchException: " + ex.ToString();
                                    MainActivity.LogDebug(exceptionString);
                                    MainActivity.LogFirebase(exceptionString);
                                }
                            }
                        }
                        else if(task.Exception.InnerException is DownloadDirectoryNotSetException || task.Exception?.InnerException?.InnerException is DownloadDirectoryNotSetException)
                        {
                            action = () => { ToastUIWithDebouncer(SoulSeekState.ActiveActivityRef.GetString(Resource.String.FailedDownloadDirectoryNotSet), "_17_"); };
                        }
                        else if (task.Exception.InnerException is Soulseek.TransferRejectedException tre) //derived class of TransferException...
                        {
                            //we go here when trying to download a locked file... (the exception only gets thrown on rejected with "not shared")
                            bool isFileNotShared = tre.Message.ToLower().Contains("file not shared");
                            // if we request a file from a soulseek NS client such as eÌe.jpg which when encoded in UTF fails to be decoded by Latin1
                            // soulseek NS will send TransferRejectedException "File Not Shared." with our filename (the filename will be identical).
                            // when we retry lets try a Latin1 encoding.  If no special characters this will not make any difference and it will be just a normal retry.
                            // we only want to try this once. and if it fails reset it to normal and do not try it again.
                            // if we encode the same way we decode, then such a thing will not occur.

                            // in the nicotine 3.1.1 and earlier, if we request a file such as "fÃ¶r", nicotine will encode it in Latin1.  We will
                            // decode it as UTF8, encode it back as UTF8 and then they will decode it as UTF-8 resulting in för".  So even though we encoded and decoded
                            // in the same way there can still be an issue.  If we force legacy it will be fixed.

                            //if (!wasTriedToUndoMojibake && isFileNotShared && HasNonASCIIChars(transferItem.FullFilename))
                            //{
                            //    tryUndoMojibake = true;
                            //    transferItem.TryUndoMojibake = true;
                            //    retriable = true;
                            //}


                            // always set this since it only shows if we DO NOT retry
                            if (isFileNotShared)
                            {
                                action = () => { ToastUIWithDebouncer(SoulSeekState.ActiveActivityRef.GetString(Resource.String.transfer_rejected_file_not_shared), "_2_"); }; //needed
                            }
                            else
                            {
                                action = () => { ToastUIWithDebouncer(SoulSeekState.ActiveActivityRef.GetString(Resource.String.transfer_rejected), "_2_"); }; //needed
                            }
                            MainActivity.LogDebug("rejected. is not shared: " + isFileNotShared);
                        }
                        else if (task.Exception.InnerException is Soulseek.TransferException)
                        {
                            action = () => { ToastUIWithDebouncer(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_establish_connection_to_peer), e.dlInfo.username), "_1_", e?.dlInfo?.username ?? string.Empty); };
                        }
                        else if (task.Exception.InnerException is Soulseek.UserOfflineException)
                        {
                            action = () => { ToastUIWithDebouncer(task.Exception.InnerException.Message, "_3_", e?.dlInfo?.username ?? string.Empty); }; //needed. "User x appears to be offline"
                        }
                        else if (task.Exception.InnerException is Soulseek.SoulseekClientException &&
                                task.Exception.InnerException.Message != null &&
                                task.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            LogDebug("Task Exception: " + task.Exception.InnerException.Message);
                            action = () => { ToastUIWithDebouncer(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_establish_direct_or_indirect), "_4_"); };
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("read error: remote connection closed"))
                        {
                            retriable = true;
                            //MainActivity.LogFirebase("read error: remote connection closed"); //this is if someone cancels the upload on their end.
                            LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.remote_conn_closed)); };
                            if (NetworkHandoffDetector.HasHandoffOccuredRecently())
                            {
                                resetRetryCount = true;
                            }
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("network subsystem is down"))
                        {
                            //MainActivity.LogFirebase("Network Subsystem is Down");
                            if (ConnectionReceiver.DoWeHaveInternet())//if we have internet again by the time we get here then its retriable. this is often due to handoff. handoff either causes this or "remote connection closed"
                            {
                                LogDebug("we do have internet");
                                action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.remote_conn_closed)); };
                                retriable = true;
                                if (NetworkHandoffDetector.HasHandoffOccuredRecently())
                                {
                                    resetRetryCount = true;
                                }
                            }
                            else
                            {
                                action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_down)); };
                            }
                            LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);

                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("reported as failed by"))
                        {
                            // if we request a file from a soulseek NS client such as eÌÌÌe.jpg which when encoded in UTF fails to be decoded by Latin1
                            // soulseek NS will send UploadFailed with our filename (the filename will be identical).
                            // when we retry lets try a Latin1 encoding.  If no special characters this will not make any difference and it will be just a normal retry.
                            // we only want to try this once. and if it fails reset it to normal and do not try it again.
                            //if(!wasTriedToUndoMojibake && HasNonASCIIChars(transferItem.FullFilename))
                            //{
                            //    tryUndoMojibake = true;
                            //    transferItem.TryUndoMojibake = true;
                            //    retriable = true;
                            //}
                            retriable = true;
                            //MainActivity.LogFirebase("Reported as failed by uploader");
                            LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.reported_as_failed)); };
                        }
                        else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            //MainActivity.LogFirebase("failed to establish a direct or indirect message connection");
                            LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            action = () => { ToastUIWithDebouncer(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_establish_direct_or_indirect), "_5_"); };
                        }
                        else
                        {
                            retriable = true;
                            //the server connection task.Exception.InnerException.Message.Contains("The server connection was closed unexpectedly") //this seems to be retry able
                            //or task.Exception.InnerException.InnerException.Message.Contains("The server connection was closed unexpectedly""
                            //or task.Exception.InnerException.Message.Contains("Transfer failed: Read error: Object reference not set to an instance of an object
                            bool unknownException = true;
                            if (task.Exception != null && task.Exception.InnerException != null)
                            {
                                //I get a lot of null refs from task.Exception.InnerException.Message

                                
                                LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                                if(task.Exception.InnerException.Message.StartsWith("Disk full.")) //is thrown by Stream.Close()
                                {
                                    action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_no_space)); };
                                    unknownException = false;
                                }



                                if (task.Exception.InnerException.InnerException != null && unknownException)
                                {

                                    if (task.Exception.InnerException.InnerException.Message.Contains("ENOSPC (No space left on device)") || task.Exception.InnerException.InnerException.Message.Contains("Read error: Disk full."))
                                    {
                                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_no_space)); };
                                        unknownException = false;
                                    }

                                    //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                    if (task.Exception.InnerException.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                                    {
                                        unknownException = false;
                                    }
                                    
                                    if(unknownException)
                                    {
                                        MainActivity.LogFirebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);
                                    }



                                    //this is to help with the collection was modified
                                    if (task.Exception.InnerException.InnerException.InnerException != null && unknownException)
                                    {
                                        MainActivity.LogInfoFirebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);
                                        var innerInner = task.Exception.InnerException.InnerException.InnerException;
                                        //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                        MainActivity.LogFirebase("Innerx3_Exception: " + innerInner.Message + innerInner.StackTrace);
                                        //this is to help with the collection was modified
                                    }
                                }

                                if (unknownException)
                                {
                                    if(task.Exception.InnerException.StackTrace.Contains("System.Xml.Serialization.XmlSerializationWriterInterpreter"))
                                    {
                                        if(task.Exception.InnerException.StackTrace.Length > 1201)
                                        {
                                            MainActivity.LogFirebase("xml Unhandled task exception 2nd part: " + task.Exception.InnerException.StackTrace.Skip(1200).ToString());
                                        }
                                        MainActivity.LogFirebase("xml Unhandled task exception: " + task.Exception.InnerException.Message + task.Exception.InnerException.StackTrace);
                                    }
                                    else
                                    {
                                        MainActivity.LogFirebase("dlcontaction Unhandled task exception: " + task.Exception.InnerException.Message + task.Exception.InnerException.StackTrace);
                                    }
                                }
                            }
                            else if (task.Exception != null && unknownException)
                            {
                                MainActivity.LogFirebase("Unhandled task exception (little info): " + task.Exception.Message);
                                LogDebug("Unhandled task exception (little info):" + task.Exception.Message);
                            }
                        }


                        if (forceRetry || ((resetRetryCount || e.dlInfo.RetryCount == 0) && (SoulSeekState.AutoRetryDownload) && retriable))
                        {
                            MainActivity.LogDebug("!! retry the download " + e.dlInfo.fullFilename);
                            //MainActivity.LogDebug("!!! try undo mojibake " + tryUndoMojibake);
                            try
                            {
                                //retry download.
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                                Android.Net.Uri incompleteUri = null;
                                TransfersFragment.SetupCancellationToken(e.dlInfo.TransferItemReference, cancellationTokenSource, out _); //else when you go to cancel you are cancelling an already cancelled useless token!!
                                Task retryTask = DownloadDialog.DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, cancellationTokenSource, 1, e.dlInfo.TransferItemReference.ShouldEncodeFileLatin1(), e.dlInfo.TransferItemReference.ShouldEncodeFolderLatin1());
                                retryTask.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, retryTask, cancellationTokenSource, e.dlInfo.QueueLength, resetRetryCount ? 0 : 1, task.Exception, e.dlInfo.Depth))));
                                return; //i.e. dont toast anything just retry.
                            }
                            catch (System.Exception e)
                            {
                                MainActivity.LogFirebase("retry creation failed: " + e.Message + e.StackTrace);
                                //if this happens at least log the normal message....
                            }

                        }

                        if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                        {
                            LogFirebase("auto retry failed: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString() + "new exception: " + task.Exception?.InnerException?.Message?.ToString());
                        }

                        //Action action2 = () => { ToastUI(task.Exception.ToString());};
                        //this.RunOnUiThread(action2);
                        if (action == null)
                        {
                            //action = () => { ToastUI(msgDebug1); ToastUI(msgDebug2); };
                            action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_unspecified)); };
                        }
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(action);
                        //System.Console.WriteLine(task.Exception.ToString());
                        return;
                    }
                    //failed downloads return before getting here...

                    if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                    {
                        LogFirebase("auto retry succeeded: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString());
                    }

                    if (!SoulSeekState.DisableDownloadToastNotification)
                    {
                        action = () => { ToastUI(Helpers.GetFileNameFromFile(e.dlInfo.fullFilename) + " " + SeekerApplication.GetString(Resource.String.FinishedDownloading)); };
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(action);
                    }
                    string finalUri = string.Empty;
                    if (task is Task<byte[]> tbyte)
                    {
                        string path = SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, tbyte.Result, null, null, true, e.dlInfo.Depth, out finalUri);
                        SaveFileToMediaStore(path);
                    }
                    else if (task is Task<Tuple<string, string>> tString)
                    {
                        //move file...
                        string path = SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, null, Android.Net.Uri.Parse(tString.Result.Item1), Android.Net.Uri.Parse(tString.Result.Item2), false, e.dlInfo.Depth, out finalUri);
                        SaveFileToMediaStore(path);
                    }
                    else
                    {
                        LogFirebase("Very bad. Task is not the right type.....");
                    }
                    e.dlInfo.TransferItemReference.FinalUri = finalUri;
                }
                finally
                {
                    e.dlInfo.TransferItemReference.InProcessing = false;
                }
            });
            return continuationActionSaveFile;
        }


        public static void ToastUI(int msgCode)
        {
            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(msgCode), ToastLength.Long).Show();
        }

        public static void ToastUI_short(int msgCode)
        {
            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(msgCode), ToastLength.Short).Show();
        }

        private static bool HasNonASCIIChars(string str)
        {
            return (System.Text.Encoding.UTF8.GetByteCount(str) != str.Length);
        }


        /// <summary>
        /// This is to solve the problem of, are all the toasts part of the same session?  
        /// For example if you download a locked folder of 20 files, you will get immediately 20 toasts
        /// So our logic is, if you just did a message, wait a full second before showing anything more.
        /// </summary>
        /// <param name="msgToToast"></param>
        /// <param name="caseOrCode"></param>
        /// <param name="usernameIfApplicable"></param>
        /// <param name="seconds">might be useful to increase this if something has a lot of variance even if requested at the same time, like a timeout.</param>
        private static void ToastUIWithDebouncer(string msgToToast, string caseOrCode, string usernameIfApplicable = "", int seconds = 1)
        {
            long curTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            //if it does not exist then updatedTime will be curTime.  If it does exist but is older than a second then updated time will also be curTime.  In those two cases, show the toast.
            MainActivity.LogDebug("curtime " + curTime);
            bool stale = false;
            long updatedTime = ToastUIDebouncer.AddOrUpdate(caseOrCode + usernameIfApplicable, curTime, (key, oldValue) =>
            {

                MainActivity.LogDebug("key exists: " + (curTime - oldValue).ToString());

                stale = (curTime - oldValue) < (seconds * 1000);
                if (stale)
                {
                    MainActivity.LogDebug("stale");
                }

                return stale ? oldValue : curTime;
            });
            MainActivity.LogDebug("updatedTime " + updatedTime);
            if (!stale)
            {
                ToastUI(msgToToast);
            }
        }
        private static System.Collections.Concurrent.ConcurrentDictionary<string, long> ToastUIDebouncer = new System.Collections.Concurrent.ConcurrentDictionary<string, long>();

        public static void ToastUI(string msg)
        {
            if(OnUIthread())
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, ToastLength.Long).Show();
            }
            else
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, ToastLength.Long).Show();
                });
            }
        }

        public static void ToastUI_short(string msg)
        {
            if (OnUIthread())
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, ToastLength.Short).Show();
            }
            else
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(()=>
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, ToastLength.Short).Show();
                    });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool OnUIthread()
        {
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.M) //23
            {
                return Looper.MainLooper.IsCurrentThread;
            }
            else
            {
                return Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootView"></param>
        /// <param name="force">the log in layout is full of hacks. that being said force 
        ///   makes it so that if we are currently logged in to still add the logged in fragment
        ///   if not there, which makes sense. </param>
        public static void AddLoggedInLayout(View rootView = null, bool force = false)
        {
            View bttn = StaticHacks.RootView?.FindViewById<Button>(Resource.Id.buttonLogout);
            View bttnTryTwo = rootView?.FindViewById<Button>(Resource.Id.buttonLogout);
            bool bttnIsAttached = false;
            bool bttnTwoIsAttached = false;
            if (bttn != null && bttn.IsAttachedToWindow)
            {
                bttnIsAttached = true;
            }
            if (bttnTryTwo != null && bttnTryTwo.IsAttachedToWindow)
            {
                bttnTwoIsAttached = true;
            }

            if (!bttnIsAttached && !bttnTwoIsAttached && (!SoulSeekState.currentlyLoggedIn || force))
            {
                //THIS MEANS THAT WE STILL HAVE THE LOGINFRAGMENT NOT THE LOGGEDIN FRAGMENT
                //ViewGroup relLayout = SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, rootView as ViewGroup, false) as ViewGroup;
                //relLayout.LayoutParameters = new ViewGroup.LayoutParams(rootView.LayoutParameters);
                var action1 = new Action(() =>
                {
                    (rootView as ViewGroup).AddView(SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, rootView as ViewGroup, false));
                });
                if (OnUIthread())
                {
                    action1();
                }
                else
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(action1);
                }
            }
        }

        public static void UpdateUIForLoggedIn(View rootView = null, EventHandler BttnClick = null, View cWelcome = null, View cbttn = null, ViewGroup cLoading = null, EventHandler SettingClick = null)
        {
            var action = new Action(() =>
            {
                //this is the case where it already has the loggedin fragment loaded.
                Button bttn = null;
                TextView welcome = null;
                ViewGroup loggingInLayout = null;
                ViewGroup logInLayout = null;

                Button settings = null;
                try
                {
                    if (StaticHacks.RootView != null && StaticHacks.RootView.IsAttachedToWindow)
                    {
                        bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);

                        logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);

                        settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                    else
                    {
                        bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);

                        logInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);

                        settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                }
                catch
                {

                }
                if (welcome != null)
                {
                    //meanwhile: rootView.FindViewById<TextView>(Resource.Id.userNameView).  so I dont think that the welcome here is the right one.. I dont think it exists.
                    //try checking properties such as isAttachedToWindow, getWindowVisiblity etx...
                    welcome.Visibility = ViewStates.Visible;

                    bool isShown = welcome.IsShown;
                    bool isAttachedToWindow = welcome.IsAttachedToWindow;
                    bool isActivated = welcome.Activated;
                    ViewStates viewState = welcome.WindowVisibility;


                    //welcome = rootView.FindViewById(Resource.Id.userNameView) as Android.Widget.TextView;
                    //if(welcome!=null)
                    //{
                    //isShown = welcome.IsShown;
                    //isAttachedToWindow = welcome.IsAttachedToWindow;
                    //isActivated = welcome.Activated;
                    //viewState = welcome.WindowVisibility;
                    //}


                    bttn.Visibility = ViewStates.Visible;
                    settings.Visibility = ViewStates.Visible;


                    settings.Click -= SettingClick;
                    settings.Click += SettingClick;
                    AndroidX.Core.View.ViewCompat.SetTranslationZ(bttn, 90);
                    bttn.Click -= BttnClick;
                    bttn.Click += BttnClick;
                    loggingInLayout.Visibility = ViewStates.Gone;
                    welcome.Text = String.Format(SeekerApplication.GetString(Resource.String.welcome), SoulSeekState.Username);
                }
                else if (cWelcome != null)
                {
                    cWelcome.Visibility = ViewStates.Visible;
                    cbttn.Visibility = ViewStates.Visible;
                    AndroidX.Core.View.ViewCompat.SetTranslationZ(cbttn, 90);
                    cLoading.Visibility = ViewStates.Gone;
                }
                else
                {
                    StaticHacks.UpdateUI = true;//if we arent ready rn then do it when we are..
                }
                if (logInLayout != null)
                {
                    logInLayout.Visibility = ViewStates.Gone;
                    AndroidX.Core.View.ViewCompat.SetTranslationZ(logInLayout.FindViewById<Button>(Resource.Id.buttonLogin), 0);
                }

            });
            if (OnUIthread())
            {
                action();
            }
            else
            {
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
        }

        public static bool IsNotLoggedIn()
        {
            return (!SoulSeekState.currentlyLoggedIn) || SoulSeekState.Username == null || SoulSeekState.Password == null || SoulSeekState.Username == string.Empty;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootView"></param>
        public static void BackToLogInLayout(View rootView, EventHandler LogInClick, bool clearUserPass = true)
        {
            var action = new Action(() =>
            {
                //this is the case where it already has the loggedin fragment loaded.
                Button bttn = null;
                TextView welcome = null;
                TextView loading = null;
                //EditText editText = null;
                //EditText editText2 = null;
                //TextView textView = null;
                ViewGroup loggingInLayout = null;
                ViewGroup logInLayout = null;
                Button buttonLogin = null;
                //View noAccountHelp = null;
                Button settings = null;
                MainActivity.LogDebug("BackToLogInLayout");
                try
                {
                    if (StaticHacks.RootView != null && StaticHacks.RootView.IsAttachedToWindow)
                    {
                        MainActivity.LogDebug("StaticHacks.RootView != null");
                        bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);

                        //this is the case we have a bad SAVED user pass....
                        try
                        {
                            logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                            //editText2 = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.etPassword);
                            //textView = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.textView);
                            buttonLogin = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogin);
                            //noAccountHelp = StaticHacks.RootView.FindViewById(Resource.Id.noAccount);
                            if (logInLayout == null)
                            {
                                ViewGroup relLayout = SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.login, StaticHacks.RootView as ViewGroup, false) as ViewGroup;
                                relLayout.LayoutParameters = new ViewGroup.LayoutParams(StaticHacks.RootView.LayoutParameters);
                                //var action1 = new Action(() => {
                                (StaticHacks.RootView as ViewGroup).AddView(SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.login, StaticHacks.RootView as ViewGroup, false));
                                //});
                            }
                            //editText = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.etUsername);
                            //editText2 = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.etPassword);
                            //textView = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.textView);
                            settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                            buttonLogin = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogin);
                            //noAccountHelp = StaticHacks.RootView.FindViewById(Resource.Id.noAccount);
                            logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                            buttonLogin.Click -= LogInClick;
                            (StaticHacks.LoginFragment as AndriodApp1.LoginFragment).rootView = StaticHacks.RootView;
                            (StaticHacks.LoginFragment as AndriodApp1.LoginFragment).SetUpLogInLayout();
                            //buttonLogin.Click += LogInClick;
                        }
                        catch (Exception ex)
                        {
                            MainActivity.LogDebug("BackToLogInLayout" + ex.Message);
                        }

                    }
                    else
                    {
                        MainActivity.LogDebug("StaticHacks.RootView == null");
                        bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                        logInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                        buttonLogin = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
                        settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                }
                catch
                {

                }
                MainActivity.LogDebug("logInLayout is here? " + (logInLayout != null).ToString());
                if (logInLayout != null)
                {
                    logInLayout.Visibility = ViewStates.Visible;
                    if (!clearUserPass && !string.IsNullOrEmpty(SoulSeekState.Username))
                    {
                        logInLayout.FindViewById<EditText>(Resource.Id.etUsername).Text = SoulSeekState.Username;
                        logInLayout.FindViewById<EditText>(Resource.Id.etPassword).Text = SoulSeekState.Password;
                    }
                    AndroidX.Core.View.ViewCompat.SetTranslationZ(buttonLogin, 90);

                    if (loading == null)
                    {
                        MainActivity.AddLoggedInLayout(rootView);
                        if (rootView != null)
                        {
                            bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                            welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                            loggingInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                            settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                        }
                        if (rootView == null && loading == null && StaticHacks.RootView != null)
                        {
                            bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                            welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                            loggingInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                            settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                        }
                    }
                    loggingInLayout.Visibility = ViewStates.Gone; //can get nullref here!!! (at least before the .AddLoggedInLayout code..
                    welcome.Visibility = ViewStates.Gone;
                    settings.Visibility = ViewStates.Gone;
                    bttn.Visibility = ViewStates.Gone;
                    AndroidX.Core.View.ViewCompat.SetTranslationZ(bttn, 0);


                }

            });
            if (OnUIthread())
            {
                action();
            }
            else
            {
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootView"></param>
        public static void UpdateUIForLoggingInLoading(View rootView = null)
        {
            MainActivity.LogDebug("UpdateUIForLoggingInLoading");
            var action = new Action(() =>
            {
                //this is the case where it already has the loggedin fragment loaded.
                Button logoutButton = null;
                TextView welcome = null;
                ViewGroup loggingInView = null;
                ViewGroup logInLayout = null;
                Button settingsButton = null;
                try
                {
                    if (StaticHacks.RootView != null && rootView == null)
                    {
                        logoutButton = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        settingsButton = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInView = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                        logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);

                    }
                    else
                    {
                        logoutButton = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        settingsButton = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInView = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                        logInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                    }
                }
                catch
                {

                }
                if (logInLayout != null)
                {
                    logInLayout.Visibility = ViewStates.Gone; //todo change back.. //basically when we AddChild we add it UNDER the logInLayout.. so making it gone makes everything gone... we need a root layout for it...
                    AndroidX.Core.View.ViewCompat.SetTranslationZ(logInLayout.FindViewById<Button>(Resource.Id.buttonLogin), 0);
                    loggingInView.Visibility = ViewStates.Visible;
                    welcome.Visibility = ViewStates.Gone; //WE GET NULLREF HERE. FORCE connection already established exception and maybe see what is going on here...
                    logoutButton.Visibility = ViewStates.Gone;
                    settingsButton.Visibility = ViewStates.Gone;
                    AndroidX.Core.View.ViewCompat.SetTranslationZ(logoutButton, 0);
                }

            });
            if (OnUIthread())
            {
                action();
            }
            else
            {
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
        }

        private static void CreateNoMediaFileLegacy(string atDirectory)
        {
            Java.IO.File noMediaRootFile = new Java.IO.File(atDirectory + @"/.nomedia");
            if (!noMediaRootFile.Exists())
            {
                noMediaRootFile.CreateNewFile();
            }
        }

        private static void CreateNoMediaFile(DocumentFile atDirectory)
        {
            atDirectory.CreateFile("nomedia/customnomedia", ".nomedia");
        }





        public static object lock_toplevel_ifexist_create = new object();
        public static object lock_album_ifexist_create = new object();

        public static System.IO.Stream GetIncompleteStream(string username, string fullfilename, int depth, out Android.Net.Uri incompleteUri, out Android.Net.Uri parentUri, out long partialLength)
        {
            string name = Helpers.GetFileNameFromFile(fullfilename);
            //string dir = Helpers.GetFolderNameFromFile(fullfilename);
            string filePath = string.Empty;

            bool useDownloadDir = false;
            if (SoulSeekState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
            {
                useDownloadDir = true;
            }
            bool useTempDir = false;
            if (SettingsActivity.UseTempDirectory())
            {
                useTempDir = true;
            }
            bool useCustomDir = false;
            if (SettingsActivity.UseIncompleteManualFolder())
            {
                useCustomDir = true;
            }

            bool fileExists = false;
            if (SoulSeekState.UseLegacyStorage() && (SoulSeekState.RootDocumentFile == null && useDownloadDir))
            {
                System.IO.FileStream fs = null;
                Java.IO.File incompleteDir = null;
                Java.IO.File musicDir = null;
                try
                {
                    string rootdir = string.Empty;
                    //if (SoulSeekState.RootDocumentFile==null)
                    //{
                    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //}
                    //else
                    //{
                    //    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //    rootdir = SoulSeekState.RootDocumentFile.Uri.Path; //returns junk...
                    //}

                    if (!(new Java.IO.File(rootdir)).Exists())
                    {
                        (new Java.IO.File(rootdir)).Mkdirs();
                    }
                    //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                    string incompleteDirString = rootdir + @"/Soulseek Incomplete/";
                    lock (lock_toplevel_ifexist_create)
                    {
                        incompleteDir = new Java.IO.File(incompleteDirString);
                        if (!incompleteDir.Exists())
                        {
                            //make it and add nomedia...
                            incompleteDir.Mkdirs();
                            CreateNoMediaFileLegacy(incompleteDirString);
                        }
                    }

                    string fullDir = rootdir + @"/Soulseek Incomplete/" + Helpers.GenerateIncompleteFolderName(username, fullfilename, depth); //+ @"/" + name;
                    musicDir = new Java.IO.File(fullDir);
                    lock (lock_album_ifexist_create)
                    {
                        if (!musicDir.Exists())
                        {
                            musicDir.Mkdirs();
                            CreateNoMediaFileLegacy(fullDir);
                        }
                    }
                    parentUri = Android.Net.Uri.Parse(new Java.IO.File(fullDir).ToURI().ToString());
                    filePath = fullDir + @"/" + name;
                    Java.IO.File f = new Java.IO.File(filePath);
                    fs = null;
                    if (f.Exists())
                    {
                        fileExists = true;
                        fs = new System.IO.FileStream(filePath, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.None);
                        partialLength = f.Length();
                    }
                    else
                    {
                        fs = System.IO.File.Create(filePath);
                        partialLength = 0;
                    }
                    incompleteUri = Android.Net.Uri.Parse(new Java.IO.File(filePath).ToURI().ToString()); //using incompleteUri.Path gives you filePath :)
                }
                catch (Exception e)
                {
                    LogFirebase("Legacy Filesystem Issue: " + e.Message + e.StackTrace + System.Environment.NewLine + incompleteDir.Exists() + musicDir.Exists() + fileExists);
                    throw;
                }
                return fs;
            }
            else
            {
                DocumentFile folderDir1 = null; //this is the desired location.
                DocumentFile rootdir = null;

                bool diagRootDirExistsAndCanWrite = false;
                bool diagDidWeCreateSoulSeekDir = false;
                bool diagSlskDirExistsAfterCreation = false;
                bool rootDocumentFileIsNull = SoulSeekState.RootDocumentFile == null;

                if(rootDocumentFileIsNull)
                {
                    throw new DownloadDirectoryNotSetException();
                }

                //MainActivity.LogDebug("rootDocumentFileIsNull: " + rootDocumentFileIsNull);
                try
                {
                    if (useDownloadDir)
                    {
                        rootdir = SoulSeekState.RootDocumentFile;
                        MainActivity.LogDebug("using download dir" + rootdir.Uri.LastPathSegment);
                    }
                    else if (useTempDir)
                    {
                        Java.IO.File appPrivateExternal = SoulSeekState.ActiveActivityRef.GetExternalFilesDir(null);
                        rootdir = DocumentFile.FromFile(appPrivateExternal);
                        MainActivity.LogDebug("using temp incomplete dir");
                    }
                    else if (useCustomDir)
                    {
                        rootdir = SoulSeekState.RootIncompleteDocumentFile;
                        MainActivity.LogDebug("using custom incomplete dir" + rootdir.Uri.LastPathSegment);
                    }
                    else
                    {
                        MainActivity.LogFirebase("!! should not get here, no dirs");
                    }

                    if (!rootdir.Exists())
                    {
                        LogFirebase("rootdir (nonnull) does not exist: " + rootdir.Uri);
                        diagRootDirExistsAndCanWrite = false;
                        //dont know how to create self
                        //rootdir.CreateDirectory();
                    }
                    else if (!rootdir.CanWrite())
                    {
                        diagRootDirExistsAndCanWrite = false;
                        LogFirebase("rootdir (nonnull) exists but cant write: " + rootdir.Uri);
                    }
                    else
                    {
                        diagRootDirExistsAndCanWrite = true;
                    }


                    //string diagMessage10 = CheckPermissions(rootdir.Uri); //TEST CODE REMOVE

                    //var slskCompleteUri = rootdir.Uri + @"/Soulseek Complete/";
                    DocumentFile slskDir1 = null;
                    lock (lock_toplevel_ifexist_create)
                    {
                        slskDir1 = rootdir.FindFile("Soulseek Incomplete"); //does Soulseek Complete folder exist
                        if (slskDir1 == null || !slskDir1.Exists())
                        {
                            slskDir1 = rootdir.CreateDirectory("Soulseek Incomplete");
                            //slskDir1 = rootdir.FindFile("Soulseek Incomplete");
                            if (slskDir1 == null)
                            {
                                string diagMessage = CheckPermissions(rootdir.Uri);
                                LogFirebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                                LogInfoFirebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                            }
                            else if (!slskDir1.Exists())
                            {
                                LogFirebase("slskDir1 does not exist" + rootdir.Uri);
                            }
                            else if (!slskDir1.CanWrite())
                            {
                                LogFirebase("slskDir1 cannot write" + rootdir.Uri);
                            }
                            CreateNoMediaFile(slskDir1);
                        }
                    }
                    //slskDir1 = rootdir.FindFile("Soulseek Incomplete"); //if it does not exist you then have to get it again!!
                    //                                                  //else first time the song will be outside the directory
                    if (slskDir1 == null)
                    {
                        diagSlskDirExistsAfterCreation = false;
                        LogFirebase("slskDir1 is null");
                        LogInfoFirebase("slskDir1 is null");
                    }
                    else
                    {
                        diagSlskDirExistsAfterCreation = true;
                    }


                    string album_folder_name = Helpers.GenerateIncompleteFolderName(username, fullfilename, depth);
                    lock (lock_album_ifexist_create)
                    {
                        folderDir1 = slskDir1.FindFile(album_folder_name); //does the folder we want to save to exist
                        if (folderDir1 == null || !folderDir1.Exists())
                        {
                            folderDir1 = slskDir1.CreateDirectory(album_folder_name);
                            //folderDir1 = slskDir1.FindFile(album_folder_name); //if it does not exist you then have to get it again!!
                            if (folderDir1 == null)
                            {
                                string rootUri = string.Empty;
                                if (SoulSeekState.RootDocumentFile != null)
                                {
                                    rootUri = SoulSeekState.RootDocumentFile.Uri.ToString();
                                }
                                bool slskDirExistsWriteable = false;
                                if (slskDir1 != null)
                                {
                                    slskDirExistsWriteable = slskDir1.Exists() && slskDir1.CanWrite();
                                }
                                string diagMessage = CheckPermissions(slskDir1.Uri);
                                LogInfoFirebase("folderDir1 is null:" + album_folder_name + "root: " + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                                LogFirebase("folderDir1 is null:" + album_folder_name + "root: " + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                            }
                            else if (!folderDir1.Exists())
                            {
                                LogFirebase("folderDir1 does not exist:" + album_folder_name);
                            }
                            else if (!folderDir1.CanWrite())
                            {
                                LogFirebase("folderDir1 cannot write:" + album_folder_name);
                            }
                            CreateNoMediaFile(folderDir1);
                        }
                    }

                    //THE VALID GOOD flags for a directory is Supports Dir Create.  The write is off in all of my cases and not necessary..

                    //string diagMessage2 = CheckPermissions(slskDir1.Uri); //TEST CODE DELETE
                    //string diagMessage3 = CheckPermissions(folderDir1.Uri); //TEST CODE DELETE


                }
                catch (Exception e)
                {
                    string rootDirUri = SoulSeekState.RootDocumentFile?.Uri == null ? "null" : SoulSeekState.RootDocumentFile.Uri.ToString();
                    MainActivity.LogFirebase("Filesystem Issue: " + rootDirUri + " " + e.Message + diagSlskDirExistsAfterCreation + diagRootDirExistsAndCanWrite + diagDidWeCreateSoulSeekDir + rootDocumentFileIsNull + e.StackTrace);
                }

                if (rootdir == null && !SoulSeekState.UseLegacyStorage())
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(() => { ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_cannot_access_files)); });
                }

                //BACKUP IF FOLDER DIR IS NULL
                if (folderDir1 == null)
                {
                    folderDir1 = rootdir; //use the root instead..
                }

                parentUri = folderDir1.Uri;

                filePath = folderDir1.Uri + @"/" + name;

                System.IO.Stream stream = null;
                DocumentFile potentialFile = folderDir1.FindFile(name); //this will return null if does not exist!!
                if (potentialFile != null && potentialFile.Exists())  //dont do a check for length 0 because then it will go to else and create another identical file (2)
                {
                    partialLength = potentialFile.Length();
                    incompleteUri = potentialFile.Uri;
                    stream = SoulSeekState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri, "wa");
                }
                else
                {
                    partialLength = 0;
                    DocumentFile mFile = Helpers.CreateMediaFile(folderDir1, name); //on samsung api 19 it renames song.mp3 to song.mp3.mp3. //TODO fix this! (tho below api 29 doesnt use this path anymore)
                    //String: name of new document, without any file extension appended; the underlying provider may choose to append the extension.. Whoops...
                    incompleteUri = mFile.Uri; //nullref TODO TODO: if null throw custom exception so you can better handle it later on in DL continuation action
                    stream = SoulSeekState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri);
                }

                return stream;
                //string type1 = stream.GetType().ToString();
                //Java.IO.File musicFile = new Java.IO.File(filePath);
                //FileOutputStream stream = new FileOutputStream(mFile);
                //stream.Write(bytes);
                //stream.Close();
            }
            //return filePath;
        }




        /// <summary>
        /// Check Permissions if things dont go right for better diagnostic info
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private static string CheckPermissions(Android.Net.Uri folder)
        {
            if (SoulSeekState.ActiveActivityRef != null)
            {
                var cursor = SoulSeekState.ActiveActivityRef.ContentResolver.Query(folder, new string[] { DocumentsContract.Document.ColumnFlags }, null, null, null);
                int flags = 0;
                if (cursor.MoveToFirst())
                {
                    flags = cursor.GetInt(0);
                }
                cursor.Close();
                bool canWrite = (flags & (int)DocumentContractFlags.SupportsWrite) != 0;
                bool canDirCreate = (flags & (int)DocumentContractFlags.DirSupportsCreate) != 0;
                if (canWrite && canDirCreate)
                {
                    return "Can Write and DirSupportsCreate";
                }
                else if (canWrite)
                {
                    return "Can Write and not DirSupportsCreate";
                }
                else if (canDirCreate)
                {
                    return "Can not Write and can DirSupportsCreate";
                }
                else
                {
                    return "No permissions";
                }
            }
            return string.Empty;
        }






        private static string SaveToFile(string fullfilename, string username, byte[] bytes, Android.Net.Uri uriOfIncomplete, Android.Net.Uri parentUriOfIncomplete, bool memoryMode, int depth, out string finalUri)
        {
            string name = Helpers.GetFileNameFromFile(fullfilename);
            string dir = Helpers.GetFolderNameFromFile(fullfilename, depth);
            string filePath = string.Empty;

            if (memoryMode && (bytes == null || bytes.Length == 0))
            {
                LogFirebase("EMPTY or NULL BYTE ARRAY in mem mode");
            }

            if (!memoryMode && uriOfIncomplete == null)
            {
                LogFirebase("no URI in file mode");
            }
            finalUri = string.Empty;
            if (SoulSeekState.UseLegacyStorage() &&
                (SoulSeekState.RootDocumentFile == null && !SettingsActivity.UseIncompleteManualFolder())) //if the user didnt select a complete OR incomplete directory. i.e. pure java files.  
            {

                //this method works just fine if coming from a temp dir.  just not a open doc tree dir.

                string rootdir = string.Empty;
                //if (SoulSeekState.SaveDataDirectoryUri ==null || SoulSeekState.SaveDataDirectoryUri == string.Empty)
                //{
                rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //}
                //else
                //{
                //    rootdir = SoulSeekState.SaveDataDirectoryUri;
                //}
                if (!(new Java.IO.File(rootdir)).Exists())
                {
                    (new Java.IO.File(rootdir)).Mkdirs();
                }
                //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                string intermediateFolder = @"/";
                if (SoulSeekState.CreateCompleteAndIncompleteFolders)
                {
                    intermediateFolder = @"/Soulseek Complete/";
                }
                if (SoulSeekState.CreateUsernameSubfolders)
                {
                    intermediateFolder = intermediateFolder + username + @"/"; //TODO: escape? slashes? etc... can easily test by just setting username to '/' in debugger
                }
                string fullDir = rootdir + intermediateFolder + dir; //+ @"/" + name;
                Java.IO.File musicDir = new Java.IO.File(fullDir);
                musicDir.Mkdirs();
                filePath = fullDir + @"/" + name;
                Java.IO.File musicFile = new Java.IO.File(filePath);
                FileOutputStream stream = new FileOutputStream(musicFile);
                finalUri = musicFile.ToURI().ToString();
                if (memoryMode)
                {
                    stream.Write(bytes);
                    stream.Close();
                }
                else
                {
                    Java.IO.File inFile = new Java.IO.File(uriOfIncomplete.Path);
                    Java.IO.File inDir = new Java.IO.File(parentUriOfIncomplete.Path);
                    MoveFile(new FileInputStream(inFile), stream, inFile, inDir);
                }
            }
            else
            {
                bool useLegacyDocFileToJavaFileOverride = false;
                DocumentFile legacyRootDir = null;
                if (SoulSeekState.UseLegacyStorage() && SoulSeekState.RootDocumentFile == null && SettingsActivity.UseIncompleteManualFolder())
                {
                    //this means that even though rootfile is null, manual folder is set and is a docfile.
                    //so we must wrap the default root doc file.
                    string legacyRootdir = string.Empty;
                    //if (SoulSeekState.SaveDataDirectoryUri ==null || SoulSeekState.SaveDataDirectoryUri == string.Empty)
                    //{
                    legacyRootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //}
                    //else
                    //{
                    //    rootdir = SoulSeekState.SaveDataDirectoryUri;
                    //}
                    Java.IO.File legacyRoot = (new Java.IO.File(legacyRootdir));
                    if (!legacyRoot.Exists())
                    {
                        legacyRoot.Mkdirs();
                    }

                    legacyRootDir = DocumentFile.FromFile(legacyRoot);

                    useLegacyDocFileToJavaFileOverride = true;

                }













                DocumentFile folderDir1 = null; //this is the desired location.
                DocumentFile rootdir = null;

                bool diagRootDirExists = true;
                bool diagDidWeCreateSoulSeekDir = false;
                bool diagSlskDirExistsAfterCreation = true;
                bool rootDocumentFileIsNull = SoulSeekState.RootDocumentFile == null;
                try
                {





                    rootdir = SoulSeekState.RootDocumentFile;

                    if (useLegacyDocFileToJavaFileOverride)
                    {
                        rootdir = legacyRootDir;
                    }

                    if (!rootdir.Exists())
                    {
                        diagRootDirExists = false;
                        //dont know how to create self
                        //rootdir.CreateDirectory();
                    }
                    //var slskCompleteUri = rootdir.Uri + @"/Soulseek Complete/";

                    DocumentFile slskDir1 = null;
                    if (SoulSeekState.CreateCompleteAndIncompleteFolders)
                    {
                        slskDir1 = rootdir.FindFile("Soulseek Complete"); //does Soulseek Complete folder exist
                        if (slskDir1 == null || !slskDir1.Exists())
                        {
                            slskDir1 = rootdir.CreateDirectory("Soulseek Complete");
                            LogDebug("Creating Soulseek Complete");
                            diagDidWeCreateSoulSeekDir = true;
                        }

                        if (slskDir1 == null)
                        {
                            diagSlskDirExistsAfterCreation = false;
                        }
                        else if (!slskDir1.Exists())
                        {
                            diagSlskDirExistsAfterCreation = false;
                        }
                    }
                    else
                    {
                        slskDir1 = rootdir;
                    }

                    bool diagUsernameDirExistsAfterCreation = false;
                    bool diagDidWeCreateUsernameDir = false;
                    if (SoulSeekState.CreateUsernameSubfolders)
                    {
                        DocumentFile tempUsernameDir1 = null;
                        lock (string.Intern("IfNotExistCreateAtomic_1"))
                        {
                            tempUsernameDir1 = slskDir1.FindFile(username); //does username folder exist
                            if (tempUsernameDir1 == null || !tempUsernameDir1.Exists())
                            {
                                tempUsernameDir1 = slskDir1.CreateDirectory(username);
                                LogDebug(string.Format("Creating {0} dir", username));
                                diagDidWeCreateUsernameDir = true;
                            }
                        }

                        if (tempUsernameDir1 == null)
                        {
                            diagUsernameDirExistsAfterCreation = false;
                        }
                        else if (!slskDir1.Exists())
                        {
                            diagUsernameDirExistsAfterCreation = false;
                        }
                        else
                        {
                            diagUsernameDirExistsAfterCreation = true;
                        }
                        slskDir1 = tempUsernameDir1;
                    }

                    if (depth == 1)
                    {
                        lock(string.Intern("IfNotExistCreateAtomic_2"))
                        {
                            folderDir1 = slskDir1.FindFile(dir); //does the folder we want to save to exist
                            if (folderDir1 == null || !folderDir1.Exists())
                            {
                                LogDebug("Creating " + dir);
                                folderDir1 = slskDir1.CreateDirectory(dir);
                            }
                            if (folderDir1 == null || !folderDir1.Exists())
                            {
                                LogFirebase("folderDir is null or does not exists");
                            }
                        }
                    }
                    else
                    {
                        DocumentFile folderDirNext = null;
                        folderDir1 = slskDir1;
                        int _depth = depth;
                        while (_depth > 0)
                        {
                            var parts = dir.Split('\\');
                            string singleDir = parts[parts.Length - _depth];
                            lock (string.Intern("IfNotExistCreateAtomic_3"))
                            {
                                folderDirNext = folderDir1.FindFile(singleDir); //does the folder we want to save to exist
                                if (folderDirNext == null || !folderDirNext.Exists())
                                {
                                    LogDebug("Creating " + dir);
                                    folderDirNext = folderDir1.CreateDirectory(singleDir);
                                }
                                if (folderDirNext == null || !folderDirNext.Exists())
                                {
                                    LogFirebase("folderDir is null or does not exists, depth" + _depth);
                                }
                            }
                            folderDir1 = folderDirNext;
                            _depth--;
                        }
                    }
                    //folderDir1 = slskDir1.FindFile(dir); //if it does not exist you then have to get it again!!
                }
                catch (Exception e)
                {
                    MainActivity.LogFirebase("Filesystem Issue: " + e.Message + diagSlskDirExistsAfterCreation + diagRootDirExists + diagDidWeCreateSoulSeekDir + rootDocumentFileIsNull + SoulSeekState.CreateUsernameSubfolders);
                }

                if (rootdir == null && !SoulSeekState.UseLegacyStorage())
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_cannot_access_files)); });
                }

                //BACKUP IF FOLDER DIR IS NULL
                if (folderDir1 == null)
                {
                    folderDir1 = rootdir; //use the root instead..
                }

                filePath = folderDir1.Uri + @"/" + name;

                //Java.IO.File musicFile = new Java.IO.File(filePath);
                //FileOutputStream stream = new FileOutputStream(mFile);
                if (memoryMode)
                {
                    DocumentFile mFile = Helpers.CreateMediaFile(folderDir1, name);
                    finalUri = mFile.Uri.ToString();
                    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                    stream.Write(bytes);
                    stream.Close();
                }
                else
                {

                    //106ms for 32mb
                    Android.Net.Uri uri = null;
                    if (SoulSeekState.PreMoveDocument() ||
                        SettingsActivity.UseTempDirectory() || //i.e. if use temp dir which is file: // rather than content: //
                        (SoulSeekState.UseLegacyStorage() && SettingsActivity.UseIncompleteManualFolder() && SoulSeekState.RootDocumentFile == null) || //i.e. if use complete dir is file: // rather than content: // but Incomplete is content: //
                        Helpers.CompleteIncompleteDifferentVolume() || !SoulSeekState.ManualIncompleteDataDirectoryUriIsFromTree || !SoulSeekState.SaveDataDirectoryUriIsFromTree)
                    {
                        try
                        {
                            DocumentFile mFile = Helpers.CreateMediaFile(folderDir1, name);
                            uri = mFile.Uri;
                            finalUri = mFile.Uri.ToString();
                            System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                            MoveFile(SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);
                        }
                        catch (Exception e)
                        {
                            MainActivity.LogFirebase("CRITICAL FILESYSTEM ERROR pre" + e.Message);
                            SeekerApplication.ShowToast("Error Saving File", ToastLength.Long);
                            MainActivity.LogDebug(e.Message + " " + uriOfIncomplete.Path);
                        }
                    }
                    else
                    {
                        try
                        {
                            string realName = string.Empty;
                            if (SettingsActivity.UseIncompleteManualFolder()) //fix due to above^  otherwise "Play File" silently fails
                            {
                                var df = DocumentFile.FromSingleUri(SoulSeekState.ActiveActivityRef, uriOfIncomplete); //dont use name!!! in my case the name was .m4a but the actual file was .mp3!!
                                realName = df.Name;
                            }

                            uri = DocumentsContract.MoveDocument(SoulSeekState.ActiveActivityRef.ContentResolver, uriOfIncomplete, parentUriOfIncomplete, folderDir1.Uri); //ADDED IN API 24!!
                            DeleteParentIfEmpty(DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, parentUriOfIncomplete));
                            //"/tree/primary:musictemp/document/primary:music2/J when two different uri trees the uri returned from move document is a mismash of the two... even tho it actually moves it correctly.
                            //folderDir1.FindFile(name).Uri.Path is right uri and IsFile returns true...
                            if (SettingsActivity.UseIncompleteManualFolder()) //fix due to above^  otherwise "Play File" silently fails
                            {
                                uri = folderDir1.FindFile(realName).Uri; //dont use name!!! in my case the name was .m4a but the actual file was .mp3!!
                            }
                        }
                        catch (Exception e)
                        {
                            //move document fails if two different volumes:
                            //"Failed to move to /storage/1801-090D/Music/Soulseek Complete/folder/song.mp3"
                            //{content://com.android.externalstorage.documents/tree/primary%3A/document/primary%3ASoulseek%20Incomplete%2F/****.mp3}
                            //content://com.android.externalstorage.documents/tree/1801-090D%3AMusic/document/1801-090D%3AMusic%2FSoulseek%20Complete%2F/****}
                            if (e.Message.ToLower().Contains("already exists"))
                            {
                                try
                                {
                                    //set the uri to the existing file...
                                    var df = DocumentFile.FromSingleUri(SoulSeekState.ActiveActivityRef, uriOfIncomplete);
                                    string realName = df.Name;
                                    uri = folderDir1.FindFile(realName).Uri;

                                    if(folderDir1.Uri == parentUriOfIncomplete)
                                    {
                                        //case where SDCARD was full - all files were 0 bytes, folders could not be created, documenttree.CreateDirectory() returns null.
                                        //no errors until you tried to move it. then you would get "alreay exists" since (if Create Complete and Incomplete folders is checked and 
                                        //the incomplete dir isnt changed) then the destination is the same as the incomplete file (since the incomplete and complete folders
                                        //couldnt be created.  This error is misleading though so do a more generic error.
                                        SeekerApplication.ShowToast($"Filesystem Error for file {realName}.", ToastLength.Long);
                                        LogDebug("complete and incomplete locations are the same");
                                    }
                                    else
                                    {
                                        SeekerApplication.ShowToast(string.Format("File {0} already exists at {1}.  Delete it and try again if you want to overwrite it.", realName, uri.LastPathSegment.ToString()), ToastLength.Long);
                                    }
                                }
                                catch (Exception e2)
                                {
                                    MainActivity.LogFirebase("CRITICAL FILESYSTEM ERROR errorhandling " + e2.Message);
                                }

                            }
                            else
                            {
                                if (uri == null) //this means doc file failed (else it would be after)
                                {
                                    MainActivity.LogInfoFirebase("uri==null");
                                    //lets try with the non MoveDocument way.
                                    //this case can happen (for a legitimate reason) if:
                                    //  the user is on api <29.  they start downloading an album.  then while its downloading they set the download directory.  the manual one will be file:\\ but the end location will be content:\\
                                    try
                                    {

                                        DocumentFile mFile = Helpers.CreateMediaFile(folderDir1, name);
                                        uri = mFile.Uri;
                                        finalUri = mFile.Uri.ToString();
                                        MainActivity.LogInfoFirebase("retrying: incomplete: " + uriOfIncomplete + " complete: " + finalUri + " parent: " + parentUriOfIncomplete);
                                        //                                        MainActivity.LogInfoFirebase("using temp: " + 
                                        System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                                        MoveFile(SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);
                                    }
                                    catch (Exception secondTryErr)
                                    {
                                        MainActivity.LogFirebase("Legacy backup failed - CRITICAL FILESYSTEM ERROR pre" + secondTryErr.Message);
                                        SeekerApplication.ShowToast("Error Saving File", ToastLength.Long);
                                        MainActivity.LogDebug(secondTryErr.Message + " " + uriOfIncomplete.Path);
                                    }
                                }
                                else
                                {
                                    MainActivity.LogInfoFirebase("uri!=null");
                                    MainActivity.LogFirebase("CRITICAL FILESYSTEM ERROR " + e.Message + " path child: " + Android.Net.Uri.Decode(uriOfIncomplete.ToString()) + " path parent: " + Android.Net.Uri.Decode(parentUriOfIncomplete.ToString()) + " path dest: " + Android.Net.Uri.Decode(folderDir1?.Uri?.ToString()));
                                    SeekerApplication.ShowToast("Error Saving File", ToastLength.Long);
                                    MainActivity.LogDebug(e.Message + " " + uriOfIncomplete.Path); //Unknown Authority happens when source is file :/// storage/emulated/0/Android/data/com.companyname.andriodapp1/files/Soulseek%20Incomplete/
                                }
                            }
                        }
                        //throws "no static method with name='moveDocument' signature='(Landroid/content/ContentResolver;Landroid/net/Uri;Landroid/net/Uri;Landroid/net/Uri;)Landroid/net/Uri;' in class Landroid/provider/DocumentsContract;"
                    }

                    if (uri == null)
                    {
                        LogFirebase("DocumentsContract MoveDocument FAILED, override incomplete: " + SoulSeekState.OverrideDefaultIncompleteLocations);
                    }

                    finalUri = uri.ToString();

                    //1220ms for 35mb so 10x slower
                    //DocumentFile mFile = folderDir1.CreateFile(@"audio/mp3", name);
                    //System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                    //MoveFile(SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);


                    //stopwatch.Stop();
                    //LogDebug("DocumentsContract.MoveDocument took: " + stopwatch.ElapsedMilliseconds);

                }
            }

            return filePath;
        }

        private static void MoveFile(System.IO.Stream from, System.IO.Stream to, Android.Net.Uri toDelete, Android.Net.Uri parentToDelete)
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = from.Read(buffer)) != 0) //C# does 0 for you've reached the end!
            {
                to.Write(buffer, 0, read);
            }
            from.Close();
            to.Flush();
            to.Close();

            if (SoulSeekState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || toDelete.Scheme == "file")
            {
                try
                {
                    if (!(new Java.IO.File(toDelete.Path)).Delete())
                    {
                        LogFirebase("Java.IO.File.Delete() failed to delete");
                    }
                }
                catch (Exception e)
                {
                    LogFirebase("Java.IO.File.Delete() threw" + e.Message + e.StackTrace);
                }
            }
            else
            {
                DocumentFile df = DocumentFile.FromSingleUri(SoulSeekState.ActiveActivityRef, toDelete); //this returns a file that doesnt exist with file ://

                if (!df.Delete()) //on API 19 this seems to always fail..
                {
                    LogFirebase("df.Delete() failed to delete");
                }
            }

            DocumentFile parent = null;
            if (SoulSeekState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || parentToDelete.Scheme == "file")
            {
                parent = DocumentFile.FromFile(new Java.IO.File(parentToDelete.Path));
            }
            else
            {
                parent = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, parentToDelete); //if from single uri then listing files will give unsupported operation exception...  //if temp (file: //)this will throw (which makes sense as it did not come from open tree uri)
            }
            DeleteParentIfEmpty(parent);
        }

        public static void DeleteParentIfEmpty(DocumentFile parent)
        {
            if (parent == null)
            {
                MainActivity.LogFirebase("null parent");
                return;
            }
            //LogDebug(parent.Name + "p name");
            //LogDebug(parent.ListFiles().ToString());
            //LogDebug(parent.ListFiles().Length.ToString());
            //foreach (var f in parent.ListFiles())
            //{
            //    LogDebug("child: " + f.Name);
            //}
            try
            {
                if (parent.ListFiles().Length == 1 && parent.ListFiles()[0].Name == ".nomedia")
                {
                    if (!parent.ListFiles()[0].Delete())
                    {
                        LogFirebase("parent.Delete() failed to delete .nomedia child...");
                    }
                    if (!parent.Delete())
                    {
                        LogFirebase("parent.Delete() failed to delete parent");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was outside"))
                {
                    //race condition between checking length of ListFiles() and indexing [0] (twice)
                }
                else
                {
                    throw ex; //this might be important..
                }
            }
        }


        public static void DeleteParentIfEmpty(Java.IO.File parent)
        {
            if (parent.ListFiles().Length == 1 && parent.ListFiles()[0].Name == ".nomedia")
            {
                if (!parent.ListFiles()[0].Delete())
                {
                    LogFirebase("LEGACY parent.Delete() failed to delete .nomedia child...");
                }
                if (!parent.Delete()) //this returns false... maybe delete .nomedia child??? YUP.  cannot delete non empty dir...
                {
                    LogFirebase("LEGACY parent.Delete() failed to delete parent");
                }
            }
        }


        private static void MoveFile(Java.IO.FileInputStream from, Java.IO.FileOutputStream to, Java.IO.File toDelete, Java.IO.File parent)
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = from.Read(buffer)) != -1) //unlike C# this method does -1 for no more bytes left..
            {
                to.Write(buffer, 0, read);
            }
            from.Close();
            to.Flush();
            to.Close();
            if (!toDelete.Delete())
            {
                LogFirebase("LEGACY df.Delete() failed to delete ()");
            }
            DeleteParentIfEmpty(parent);
            //LogDebug(toDelete.ParentFile.Name + ":" + toDelete.ParentFile.ListFiles().Length.ToString());
        }

        private static void SaveFileToMediaStore(string path)
        {
            //ContentValues contentValues = new ContentValues();
            //contentValues.Put(MediaStore.MediaColumns.DateAdded, Helpers.GetDateTimeNowSafe().Ticks / TimeSpan.TicksPerMillisecond);
            //contentValues.Put(MediaStore.Audio.AudioColumns.Data,path);
            //ContentResolver.Insert(MediaStore.Audio.Media.ExternalContentUri, contentValues);from


            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            Java.IO.File f = new Java.IO.File(path);
            Android.Net.Uri contentUri = Android.Net.Uri.FromFile(f);
            mediaScanIntent.SetData(contentUri);
            SoulSeekState.ActiveActivityRef.ApplicationContext.SendBroadcast(mediaScanIntent);
        }

        //private void restoreSoulSeekState(Bundle savedInstanceState) //the Bundle can be SLOWER than the SHARED PREFERENCES if SHARED PREFERENCES was saved in a different activity.  The best exapmle being DAYNIGHTMODE
        //{   //day night mode sets the static, saves to shared preferences the new value, sets appcompat value, which recreates everything and calls restoreSoulSEekstate(bundle) where the bundle was older than shared prefs
        //    //because saveSoulSeekstate was not called in the meantime...
        //    if(sharedPreferences != null)
        //    {
        //        SoulSeekState.currentlyLoggedIn = sharedPreferences.GetBoolean(SoulSeekState.M_CurrentlyLoggedIn,false);
        //        SoulSeekState.Username = sharedPreferences.GetString(SoulSeekState.M_Username,"");
        //        SoulSeekState.Password = sharedPreferences.GetString(SoulSeekState.M_Password,"");
        //        SoulSeekState.SaveDataDirectoryUri = sharedPreferences.GetString(SoulSeekState.M_SaveDataDirectoryUri,"");
        //        SoulSeekState.NumberSearchResults = sharedPreferences.GetInt(SoulSeekState.M_NumberSearchResults, DEFAULT_SEARCH_RESULTS);
        //        SoulSeekState.DayNightMode = sharedPreferences.GetInt(SoulSeekState.M_DayNightMode, (int)AppCompatDelegate.ModeNightFollowSystem);
        //        SoulSeekState.AutoClearComplete = sharedPreferences.GetBoolean(SoulSeekState.M_AutoClearComplete, false);
        //        SoulSeekState.RememberSearchHistory = sharedPreferences.GetBoolean(SoulSeekState.M_RememberSearchHistory, true);
        //        SoulSeekState.FreeUploadSlotsOnly = sharedPreferences.GetBoolean(SoulSeekState.M_OnlyFreeUploadSlots, true);
        //        SoulSeekState.DisableDownloadToastNotification = sharedPreferences.GetBoolean(SoulSeekState.M_DisableToastNotifications, false);
        //        SoulSeekState.MemoryBackedDownload = sharedPreferences.GetBoolean(SoulSeekState.M_MemoryBackedDownload, false);
        //        SearchFragment.FilterSticky = sharedPreferences.GetBoolean(SoulSeekState.M_FilterSticky, false);
        //        SearchFragment.FilterString = sharedPreferences.GetString(SoulSeekState.M_FilterStickyString, string.Empty);
        //        SearchFragment.SetSearchResultStyle(sharedPreferences.GetInt(SoulSeekState.M_SearchResultStyle, 1));
        //        SoulSeekState.UploadSpeed = sharedPreferences.GetInt(SoulSeekState.M_UploadSpeed, -1);
        //        SoulSeekState.UploadDataDirectoryUri = sharedPreferences.GetString(SoulSeekState.M_UploadDirectoryUri, "");
        //        SoulSeekState.SharingOn = sharedPreferences.GetBoolean(SoulSeekState.M_SharingOn,false);
        //        SoulSeekState.UserList = RestoreUserListFromString(sharedPreferences.GetString(SoulSeekState.M_UserList, string.Empty));
        //    }
        //}



        protected override void OnPause()
        {
            //LogDebug(".view is null " + (StaticHacks.LoginFragment.View==null).ToString()); it is null
            base.OnPause();

            TransfersFragment.SaveTransferItems(sharedPreferences);
            lock (SHARED_PREF_LOCK)
            {
                var editor = sharedPreferences.Edit();
                editor.PutBoolean(SoulSeekState.M_CurrentlyLoggedIn, SoulSeekState.currentlyLoggedIn);
                editor.PutString(SoulSeekState.M_Username, SoulSeekState.Username);
                editor.PutString(SoulSeekState.M_Password, SoulSeekState.Password);
                editor.PutString(SoulSeekState.M_SaveDataDirectoryUri, SoulSeekState.SaveDataDirectoryUri);
                editor.PutBoolean(SoulSeekState.M_SaveDataDirectoryUriIsFromTree, SoulSeekState.SaveDataDirectoryUriIsFromTree);
                editor.PutInt(SoulSeekState.M_NumberSearchResults, SoulSeekState.NumberSearchResults);
                editor.PutInt(SoulSeekState.M_DayNightMode, SoulSeekState.DayNightMode);
                editor.PutBoolean(SoulSeekState.M_AutoClearComplete, SoulSeekState.AutoClearCompleteDownloads);
                editor.PutBoolean(SoulSeekState.M_AutoClearCompleteUploads, SoulSeekState.AutoClearCompleteUploads);
                editor.PutBoolean(SoulSeekState.M_RememberSearchHistory, SoulSeekState.RememberSearchHistory);
                editor.PutBoolean(SoulSeekState.M_RememberUserHistory, SoulSeekState.ShowRecentUsers);
                editor.PutBoolean(SoulSeekState.M_TransfersShowSizes, SoulSeekState.TransferViewShowSizes);
                editor.PutBoolean(SoulSeekState.M_TransfersShowSpeed, SoulSeekState.TransferViewShowSpeed);
                editor.PutBoolean(SoulSeekState.M_OnlyFreeUploadSlots, SoulSeekState.FreeUploadSlotsOnly);
                editor.PutBoolean(SoulSeekState.M_HideLockedSearch, SoulSeekState.HideLockedResultsInSearch);
                editor.PutBoolean(SoulSeekState.M_HideLockedBrowse, SoulSeekState.HideLockedResultsInBrowse);
                editor.PutBoolean(SoulSeekState.M_FilterSticky, SearchFragment.FilterSticky);
                editor.PutString(SoulSeekState.M_FilterStickyString, SearchTabHelper.FilterString);
                editor.PutBoolean(SoulSeekState.M_MemoryBackedDownload, SoulSeekState.MemoryBackedDownload);
                editor.PutInt(SoulSeekState.M_SearchResultStyle, (int)(SearchFragment.SearchResultStyle));
                editor.PutBoolean(SoulSeekState.M_DisableToastNotifications, SoulSeekState.DisableDownloadToastNotification);
                editor.PutInt(SoulSeekState.M_UploadSpeed, SoulSeekState.UploadSpeed);
                //editor.PutString(SoulSeekState.M_UploadDirectoryUri, SoulSeekState.UploadDataDirectoryUri);
                editor.PutBoolean(SoulSeekState.M_SharingOn, SoulSeekState.SharingOn);
                editor.PutBoolean(SoulSeekState.M_AllowPrivateRooomInvitations, SoulSeekState.AllowPrivateRoomInvitations);

                if (SoulSeekState.UserList != null)
                {
                    editor.PutString(SoulSeekState.M_UserList, SeekerApplication.SaveUserListToString(SoulSeekState.UserList));
                }



                //editor.Apply();
                editor.Commit();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean(SoulSeekState.M_CurrentlyLoggedIn, SoulSeekState.currentlyLoggedIn);
            outState.PutString(SoulSeekState.M_Username, SoulSeekState.Username);
            outState.PutString(SoulSeekState.M_Password, SoulSeekState.Password);
            outState.PutBoolean(SoulSeekState.M_SaveDataDirectoryUriIsFromTree, SoulSeekState.SaveDataDirectoryUriIsFromTree);
            outState.PutString(SoulSeekState.M_SaveDataDirectoryUri, SoulSeekState.SaveDataDirectoryUri);
            outState.PutInt(SoulSeekState.M_NumberSearchResults, SoulSeekState.NumberSearchResults);
            outState.PutInt(SoulSeekState.M_DayNightMode, SoulSeekState.DayNightMode);
            outState.PutBoolean(SoulSeekState.M_AutoClearComplete, SoulSeekState.AutoClearCompleteDownloads);
            outState.PutBoolean(SoulSeekState.M_AutoClearCompleteUploads, SoulSeekState.AutoClearCompleteUploads);
            outState.PutBoolean(SoulSeekState.M_RememberSearchHistory, SoulSeekState.RememberSearchHistory);
            outState.PutBoolean(SoulSeekState.M_RememberUserHistory, SoulSeekState.ShowRecentUsers);
            outState.PutBoolean(SoulSeekState.M_MemoryBackedDownload, SoulSeekState.MemoryBackedDownload);
            outState.PutBoolean(SoulSeekState.M_FilterSticky, SearchFragment.FilterSticky);
            outState.PutBoolean(SoulSeekState.M_OnlyFreeUploadSlots, SoulSeekState.FreeUploadSlotsOnly);
            outState.PutBoolean(SoulSeekState.M_HideLockedSearch, SoulSeekState.HideLockedResultsInSearch);
            outState.PutBoolean(SoulSeekState.M_HideLockedBrowse, SoulSeekState.HideLockedResultsInBrowse);
            outState.PutBoolean(SoulSeekState.M_DisableToastNotifications, SoulSeekState.DisableDownloadToastNotification);
            outState.PutInt(SoulSeekState.M_SearchResultStyle, (int)(SearchFragment.SearchResultStyle));
            outState.PutString(SoulSeekState.M_FilterStickyString, SearchTabHelper.FilterString);
            outState.PutInt(SoulSeekState.M_UploadSpeed, SoulSeekState.UploadSpeed);
            //outState.PutString(SoulSeekState.M_UploadDirectoryUri, SoulSeekState.UploadDataDirectoryUri);
            outState.PutBoolean(SoulSeekState.M_AllowPrivateRooomInvitations, SoulSeekState.AllowPrivateRoomInvitations);
            outState.PutBoolean(SoulSeekState.M_SharingOn, SoulSeekState.SharingOn);
            if (SoulSeekState.UserList != null)
            {
                outState.PutString(SoulSeekState.M_UserList, SeekerApplication.SaveUserListToString(SoulSeekState.UserList));
            }

        }

        private void Tabs_TabSelected(object sender, TabLayout.TabSelectedEventArgs e)
        {
            System.Console.WriteLine(e.Tab.Position);
            if (e.Tab.Position != 1) //i.e. if we are not the search tab
            {
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)this.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow((sender as View).WindowToken, 0);
                }
                catch
                {
                    //not worth throwing over
                }
            }
        }
        public static int goToSearchTab = int.MaxValue;
        private void Pager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            //if we are changing modes and the transfers action mode is not null (i.e. is active)
            //then we need to get out of it.
            if (TransfersFragment.TransfersActionMode != null)
            {
                TransfersFragment.TransfersActionMode.Finish();
            }
            //in addition each fragment is responsible for expanding their menu...
            if (e.Position == 0)
            {
                this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                this.SupportActionBar.SetHomeButtonEnabled(false);

                this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                this.SupportActionBar.SetDisplayShowTitleEnabled(true);
                this.SupportActionBar.Title = this.GetString(Resource.String.home_tab);
                this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.account_menu);
            }
            if (e.Position == 1) //search
            {
                this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                this.SupportActionBar.SetHomeButtonEnabled(false);

                //string initText = string.Empty;
                //if (this.SupportActionBar?.CustomView != null) //it is null on device rotation...
                //{
                //    AutoCompleteTextView v = this.SupportActionBar.CustomView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                //    if(v!=null)
                //    {
                //        initText = v.Text;
                //    }
                //}
                //the SupportActionBar.CustomView will still be here if we leave tabs and come back.. so just set the text on it again.

                this.SupportActionBar.SetDisplayShowCustomEnabled(true);
                this.SupportActionBar.SetDisplayShowTitleEnabled(false);
                this.SupportActionBar.SetCustomView(Resource.Layout.custom_menu_layout);
                SearchFragment.ConfigureSupportCustomView(this.SupportActionBar.CustomView/*, this*/);
                //this.SupportActionBar.CustomView.FindViewById<View>(Resource.Id.searchHere).FocusChange += MainActivity_FocusChange;
                this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.account_menu);
                if (goToSearchTab != int.MaxValue)
                {
                    //if(SearchFragment.Instance == null)
                    //{
                    //    MainActivity.LogDebug("Search Frag Instance is Null");
                    //}
                    if (SearchFragment.Instance?.Activity == null || !(SearchFragment.Instance.Activity.Lifecycle.CurrentState.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Started))) //this happens if we come from settings activity. Main Activity has NOT been started. SearchFragment has the .Actvity ref of an OLD activity.  so we are not ready yet. 
                    {
                        //let onresume go to the search tab..
                        MainActivity.LogDebug("Delay Go To Wishlist Search Fragment for OnResume");
                    }
                    else
                    {
                        //can we do this now??? or should we pass this down to the search fragment for when it gets created...  maybe we should put this in a like "OnResume"
                        MainActivity.LogDebug("Do Go To Wishlist in page selected");
                        SearchFragment.Instance.GoToTab(goToSearchTab, false, true);
                        goToSearchTab = int.MaxValue;
                    }
                }
            }
            else if (e.Position == 2)
            {


                this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                this.SupportActionBar.SetDisplayShowTitleEnabled(true);


                SetTransferSupportActionBarState();

                this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.browse_menu_empty);  //todo remove?
            }
            else if (e.Position == 3)
            {
                this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                this.SupportActionBar.SetHomeButtonEnabled(false);

                this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                this.SupportActionBar.SetDisplayShowTitleEnabled(true);
                if (string.IsNullOrEmpty(BrowseFragment.CurrentUsername))
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.browse_tab);
                }
                else
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.browse_tab) + ": " + BrowseFragment.CurrentUsername;
                }
                this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.transfers_menu);
            }
        }

        public void SetTransferSupportActionBarState()
        {
            if (TransfersFragment.InUploadsMode)
            {
                if (TransfersFragment.CurrentlySelectedUploadFolder == null)
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.Uploads);
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);
                }
                else
                {
                    this.SupportActionBar.Title = TransfersFragment.CurrentlySelectedUploadFolder.FolderName;
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    this.SupportActionBar.SetHomeButtonEnabled(true);
                }
            }
            else
            {
                if (TransfersFragment.CurrentlySelectedDLFolder == null)
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.Downloads);
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);
                }
                else
                {
                    this.SupportActionBar.Title = TransfersFragment.CurrentlySelectedDLFolder.FolderName;
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    this.SupportActionBar.SetHomeButtonEnabled(true);
                }
            }
        }

        //private void MainActivity_Click(object sender, EventArgs e)
        //{
        //    this.Window.SetSoftInputMode(SoftInput.AdjustNothing);
        //}

        private void B_Click(object sender, EventArgs e)
        {
            //Works Perfectly
            //SoulseekClient client = new SoulseekClient();
            //task.Wait();
            //var task1 = client.SearchAsync(SearchQuery.FromText("Danse Manatee"));
            //task1.Wait();
            //IEnumerable<SearchResponse> responses = task1.Result;


        }

        public class OnPageChangeLister1 : Java.Lang.Object, ViewPager.IOnPageChangeListener
        {
            //public IntPtr Handle => throw new NotImplementedException();

            //public void Dispose()
            //{
            //    //throw new NotImplementedException();
            //}

            public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
            {
                //throw new NotImplementedException();
            }

            public void OnPageScrollStateChanged(int state)
            {
                //throw new NotImplementedException();
            }

            public void OnPageSelected(int position)
            {
                //if page changes programmatically we have to do this...
                //int itemResource = -1;
                //switch(position)
                //{
                //    case 0:
                //        itemResource = Resource.Id.navigation_home;
                //        break;
                //    case 1:
                //        itemResource = Resource.Id.navigation_search;
                //        break;
                //    case 2:
                //        itemResource = Resource.Id.navigation_transfers;
                //        break;
                //    case 3:
                //        itemResource = Resource.Id.navigation_browse;
                //        break;
                //}
                BottomNavigationView navigator = SoulSeekState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if (position != -1 && navigator != null)
                {
                    //navigator.SelectedItemId = itemResource;
                    //navigator.Menu.GetItem(position).SetChecked(true);
                    AndroidX.AppCompat.View.Menu.MenuBuilder menu = navigator.Menu as AndroidX.AppCompat.View.Menu.MenuBuilder;

                    menu.GetItem(position).SetCheckable(true); //this is necessary if side scrolling...
                    menu.GetItem(position).SetChecked(true);
                }
            }
        }

        internal interface DownloadCallback
        {
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            //MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        //public override bool OnOptionsItemSelected(IMenuItem item)
        //{
        //    int id = item.ItemId;
        //    if (id == Resource.Id.action_settings)
        //    {
        //        return true;
        //    }

        //    return base.OnOptionsItemSelected(item);
        //}


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            switch(requestCode)
            {
                case POST_NOTIFICATION_PERMISSION:
                    break;
                default:
                    if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    {
                        return;
                    }
                    else
                    {
                        FinishAndRemoveTask(); //TODO - why?? this was added in initial commit. kills process if permission not granted?
                    }
                    break;
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    pager.CurrentItem = 0;
                    break;
                case Resource.Id.navigation_search:
                    pager.CurrentItem = 1;
                    break;
                case Resource.Id.navigation_transfers:
                    pager.CurrentItem = 2;
                    break;
                case Resource.Id.navigation_browse:
                    pager.CurrentItem = 3;
                    break;
            }
            return true;
        }
    }

    public class DownloadAddedEventArgs : EventArgs
    {
        public DownloadInfo dlInfo;
        public DownloadAddedEventArgs(DownloadInfo downloadInfo)
        {
            dlInfo = downloadInfo;
        }
    }

    public class DownloadInfo
    {
        public string username;
        public string fullFilename;
        public Task downloadTask;
        public long Size;
        public int QueueLength;
        public CancellationTokenSource CancellationTokenSource;
        public int RetryCount;
        public Exception PreviousFailureException; //used for diagnostic purposes.
        public Android.Net.Uri IncompleteLocation = null; //used in the file backed case..
        public TransferItem TransferItemReference = null; //reference to the associated transfer item that we create based on this dl info. we use this to store the complete uri for later playback option.
        public int Depth = 1;
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount, int depth)
        {
            username = usr; fullFilename = file; downloadTask = task; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; Depth = depth;
        }
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount, Exception previousFailureException, int depth)
        {
            username = usr; fullFilename = file; downloadTask = task; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; PreviousFailureException = previousFailureException; Depth = depth;
        }
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount, Android.Net.Uri incompleteLocation, int depth)
        {
            username = usr; fullFilename = file; downloadTask = task; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; IncompleteLocation = incompleteLocation; Depth = depth;
        }
    }

    /// <summary>
    /// When getting a users info for their User Info Activity, we need their peer UserInfo and their server UserData.  We add them to a list so that when the UserData comes in from the server, we know to save it.
    /// </summary>
    public static class RequestedUserInfoHelper
    {
        //private int picturesStored = 0;
        private static object picturesStoredUsersLock = new object();
        private static List<string> picturesStoredUsers = new List<string>();
        public static volatile List<UserListItem> RequestedUserList = new List<UserListItem>(); //these are people we have specifically requested. normally there are 0-4 people in here I would expect.

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
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SeekerApplication.ApplicationContext, Resource.String.must_be_logged_to_request_user_info, ToastLength.Short).Show();
                return;
            }

            //if we already have the username, then just do it
            if (ContainsUserInfo(uname))
            {
                //just do it.....
                LaunchUserInfoView(uname);
                return;
            }

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
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
            SoulSeekState.SoulseekClient.GetUserDataAsync(uname);
            SoulSeekState.SoulseekClient.GetUserInfoAsync(uname).ContinueWith(new Action<Task<UserInfo>>(
                (Task<UserInfo> userInfoTask) =>
                {
                    if (userInfoTask.IsCompletedSuccessfully)
                    {
                        if (!AddIfRequestedUser(uname, null, null, userInfoTask.Result))
                        {
                            MainActivity.LogFirebase("requested user info logic yet could not find in list!!");
                            //HANDLE ERROR TODO
                        }
                        else
                        {
                            Action<View> action = new Action<View>((View v) =>
                            {
                                LaunchUserInfoView(uname);
                            });

                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                //show snackbar (for active activity, active content view) so they can go to it... TODO
                                Snackbar sb = Snackbar.Make(SeekerApplication.GetViewForSnackbar(), string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.user_info_received), uname), Snackbar.LengthLong).SetAction(Resource.String.view, action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                                (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainTextColor));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                                sb.Show();
                            });
                        }
                    }
                    else
                    {
                        Exception e = userInfoTask.Exception;

                        if (e.InnerException is SoulseekClientException && e.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.user_info_failed_conn), uname), ToastLength.Long).Show();
                            });
                        }
                        else if (e.InnerException is UserOfflineException)
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.user_info_failed_offline), uname), ToastLength.Long).Show();
                            });
                        }
                        else if (e.InnerException is TimeoutException)
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.user_info_failed_timeout), uname), ToastLength.Long).Show();
                            });
                        }
                        else
                        {
                            string msg = e.Message;
                            string innerMsg = e.InnerException.Message;
                            Type t = e.InnerException.GetType();
                            MainActivity.LogFirebase("unexpected exception: " + msg + t.Name);
                        }
                        //toast timed out for user x, etc... user X is offline etc.
                    }
                }

                ));
        }

        public static void LaunchUserInfoView(string uname)
        {
            Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(ViewUserInfoActivity));
            intent.PutExtra(ViewUserInfoActivity.USERNAME_TO_VIEW, uname);
            SoulSeekState.ActiveActivityRef.StartActivity(intent);
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
                            MainActivity.LogDebug("Requested server UserData received");
                            item.UserData = userData;
                            UserDataReceivedUI?.Invoke(null, userData);
                        }
                        if (userStatus != null)
                        {
                            MainActivity.LogDebug("Requested server UserStatus received");
                            item.UserStatus = userStatus;
                        }
                        if (userInfo != null)
                        {
                            MainActivity.LogDebug("Requested peer UserInfo received");
                            item.UserInfo = userInfo;
                            if (userInfo.HasPicture)
                            {
                                MainActivity.LogDebug("peer has pic");
                                lock (picturesStoredUsersLock)
                                {
                                    picturesStoredUsers.Add(uname);
                                    picturesStoredUsers = picturesStoredUsers.Distinct().ToList();
                                    if (picturesStoredUsers.Count > int.MaxValue) //disabled for now
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
                    //this is normal
                }
                if (removeOldestPic)
                {
                    MainActivity.LogInfoFirebase("Remove oldest picture");
                    lock (picturesStoredUsersLock)
                    {
                        string userToRemovePic = picturesStoredUsers[0];
                        picturesStoredUsers.RemoveAt(0);
                    }
                    //lock (RequestedUserList)
                    //{
                    foreach (UserListItem item in RequestedUserList)
                    {
                        if (item.Username == uname)
                        {
                            item.UserInfo = null;
                        }
                    }
                    //}
                }
            }
            return found;
        }
    }
    /// <summary>
    /// Manages recent users
    /// </summary>
    public class RecentUserManager
    {
        private object recentUserLock = new object();
        private List<string> recentUsers;
        /// <summary>
        /// Called at startup
        /// </summary>
        /// <param name="_recentUsers"></param>
        public void SetRecentUserList(List<string> _recentUsers)
        {
            recentUsers = _recentUsers;
        }

        public List<string> GetRecentUserList()
        {
            lock (recentUserLock)
            {
                return recentUsers.ToList(); //a copy to avoid threading issues.
            }
        }

        public void AddUserToTop(string user, bool andSave)
        {
            lock (recentUserLock)
            {
                if (recentUsers.Contains(user))
                {
                    recentUsers.Remove(user);
                }
                recentUsers.Insert(0, user);
            }
            if (andSave)
            {
                SeekerApplication.SaveRecentUsers();
            }
        }
    }

    /// <summary>
    /// for the lower assembly
    /// </summary>
    public class UserListChecker : IUserListChecker
    {
        public bool IsInUserList(string user)
        {
            return MainActivity.UserListContainsUser(user);
        }
    }

    public enum UploadDirectoryError
    {
        NoError = 0,
        DoesNotExist = 1,
        CannotWrite = 2,
        Unknown = 3,
    }


    [Serializable]
    public class UploadDirectoryInfo
    {
        public string UploadDataDirectoryUri;
        public bool UploadDataDirectoryUriIsFromTree;
        public bool IsLocked;
        public bool IsHidden;
        public string DisplayNameOverride;

        public bool HasError()
        {
            return ErrorState != UploadDirectoryError.NoError;
        }

        public string GetLastPathSegment()
        {
            return Android.Net.Uri.Parse(this.UploadDataDirectoryUri).LastPathSegment;
        }

        [NonSerialized]
        public UploadDirectoryError ErrorState;
        [NonSerialized]
        public DocumentFile UploadDirectory;
        [NonSerialized]
        public bool IsSubdir;
        //[System.Xml.Serialization.XmlIgnoreAttribute]
        //public Android.Net.Uri UploadDirectoryUri;

        public void Reset()
        {
            UploadDataDirectoryUri = null;
            UploadDataDirectoryUriIsFromTree = true;
            IsLocked = false;
            IsHidden = false;
            DisplayNameOverride = null;
        }

        public UploadDirectoryInfo(string uploadDataDirUri, bool fromTree, bool isLocked, bool isHidden, string displayNameOverride)
        {
            UploadDataDirectoryUri = uploadDataDirUri;
            UploadDataDirectoryUriIsFromTree = fromTree;
            IsLocked = isLocked;
            IsHidden = isHidden;
            DisplayNameOverride = displayNameOverride;
            ErrorState = UploadDirectoryError.NoError;
            UploadDirectory = null;
            IsSubdir = false;
        }

        public string GetPresentableName()
        {
            if (string.IsNullOrEmpty(this.DisplayNameOverride))
            {
                MainActivity.GetAllFolderInfo(this, out _, out _, out _, out _, out string presentableName);
                return presentableName;
            }
            else
            {
                return this.DisplayNameOverride;
            }
        }

        public string GetPresentableName(UploadDirectoryInfo ourTopMostParent)
        {
            string parentLastPathSegment = Helpers.GetLastPathSegmentWithSpecialCaseProtection(ourTopMostParent.UploadDirectory, out bool msdCase);
            string ourLastPathSegment = Helpers.GetLastPathSegmentWithSpecialCaseProtection(this.UploadDirectory, out bool ourMsdCase);
            if (ourMsdCase || msdCase)
            {
                return ourLastPathSegment; //not great but no good solution for msd. TODO test
            }
            else
            {
                MainActivity.GetAllFolderInfo(ourTopMostParent, out bool overrideCase, out _, out _, out string rootOverrideName, out string parentPresentableName);
                return parentPresentableName + ourLastPathSegment.Substring(parentLastPathSegment.Length); //remove parent part and replace it with the parent presentable name.
            }
        }

    }


    public static class UploadDirectoryManager
    {
        public static string GetCompositeErrorString()
        {
            if (UploadDirectoryManager.UploadDirectories.Any(d => d.ErrorState == UploadDirectoryError.CannotWrite))
            {
                return GetErrorString(UploadDirectoryError.CannotWrite);
            }
            else if (UploadDirectoryManager.UploadDirectories.Any(d => d.ErrorState == UploadDirectoryError.DoesNotExist))
            {
                return GetErrorString(UploadDirectoryError.DoesNotExist);
            }
            else if (UploadDirectoryManager.UploadDirectories.Any(d => d.ErrorState == UploadDirectoryError.Unknown))
            {
                return GetErrorString(UploadDirectoryError.Unknown);
            }
            else
            {
                return null;
            }
        }

        public static string GetErrorString(UploadDirectoryError errorCode)
        {
            switch(errorCode)
            {
                case UploadDirectoryError.CannotWrite:
                    return SeekerApplication.GetString(Resource.String.PermissionErrorShared);
                case UploadDirectoryError.DoesNotExist:
                    return SeekerApplication.GetString(Resource.String.FolderNoExistShared);
                case UploadDirectoryError.Unknown:
                    return SeekerApplication.GetString(Resource.String.UnknownErrorShared);
                case UploadDirectoryError.NoError:
                default:
                    return "No Error.";
            }
        }

        public static void RestoreFromSavedState(ISharedPreferences sharedPreferences)
        {
            string sharedDirInfo = sharedPreferences.GetString(SoulSeekState.M_SharedDirectoryInfo, string.Empty);
            if(string.IsNullOrEmpty(sharedDirInfo))
            {
                string legacyUploadDataDirectory = sharedPreferences.GetString(SoulSeekState.M_UploadDirectoryUri, string.Empty);
                bool fromTree = sharedPreferences.GetBoolean(SoulSeekState.M_UploadDirectoryUriIsFromTree, true);

                if (!string.IsNullOrEmpty(legacyUploadDataDirectory))
                {
                    //legacy case. lets upgrade it.
                    UploadDirectoryInfo uploadDir = new UploadDirectoryInfo(legacyUploadDataDirectory, fromTree, false, false, null);
                    UploadDirectories = new List<UploadDirectoryInfo>();
                    UploadDirectories.Add(uploadDir);

                    //save new
                    SaveToSharedPreferences(sharedPreferences);
                    //clear old
                    var editor = sharedPreferences.Edit();
                    editor.PutString(SoulSeekState.M_UploadDirectoryUri, string.Empty);
                    editor.Commit();
                }
                else
                {
                    UploadDirectories = new List<UploadDirectoryInfo>();
                }
            }
            else
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(sharedDirInfo)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    UploadDirectories = binaryFormatter.Deserialize(mem) as List<UploadDirectoryInfo>;
                }
            }
        }

        public static void SaveToSharedPreferences(ISharedPreferences sharedPreferences)
        {
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(mem, UploadDirectories);
                string userDirsString = Convert.ToBase64String(mem.ToArray());
                lock(sharedPreferences)
                {
                    var editor = sharedPreferences.Edit();
                    editor.PutString(SoulSeekState.M_SharedDirectoryInfo, userDirsString);
                    editor.Commit();
                }
            }
        }

        public static String UploadDataDirectoryUri = null;
        public static bool UploadDataDirectoryUriIsFromTree = true;

        
        public static List<UploadDirectoryInfo> UploadDirectories;


        //public static void AddDirectory(UploadDirectoryInfo newDirInfo)
        //{
        //    foreach(UploadDirectoryInfo existingDir in UploadDirectories)
        //    {
        //        existingDir.GetPresentableName();
        //    }
        //}

        public static bool IsFromTree(string presentablePath)
        {
            if(UploadDirectories.All(dir=>dir.UploadDataDirectoryUriIsFromTree))
            {
                return true;
            }

            if (UploadDirectories.All(dir => !dir.UploadDataDirectoryUriIsFromTree))
            {
                return false;
            }

            return true; //todo
        }

        public static bool AreAnyFromLegacy()
        {
            return UploadDirectories.Where(dir => !dir.UploadDataDirectoryUriIsFromTree).Any();
        }

        /// <summary>
        /// If so then we turn off sharing. If only 1+ failed we let the user know, but keep sharing on.
        /// </summary>
        /// <returns></returns>
        public static bool AreAllFailed()
        {
            return UploadDirectories.All(dir => dir.HasError());
        }


        public static bool DoesNewDirectoryHaveUniqueRootName(UploadDirectoryInfo newDirInfo, bool updateItToHaveUniqueName)
        {
            bool isUnique = true;
            List<string> currentRootNames = new List<string>();
            foreach(UploadDirectoryInfo dirInfo in UploadDirectories)
            {
                if(dirInfo.IsSubdir || (dirInfo == newDirInfo))
                {
                    continue;
                }
                else
                {
                    MainActivity.GetAllFolderInfo(dirInfo, out _, out _, out _, out _, out string presentableName);
                    currentRootNames.Add(presentableName);
                }
            }
            MainActivity.GetAllFolderInfo(newDirInfo, out _, out _, out _, out _, out string presentableNameNew);
            if (currentRootNames.Contains(presentableNameNew))
            {
                isUnique = false;
                if(updateItToHaveUniqueName)
                {
                    while (currentRootNames.Contains(presentableNameNew))
                    {
                        presentableNameNew = presentableNameNew + " (1)";
                    }
                    newDirInfo.DisplayNameOverride = presentableNameNew;
                }
            }
            return isUnique;
        }

        /// <summary>
        /// If only 1+ failed we let the user know, but keep sharing on.
        /// </summary>
        /// <returns></returns>
        public static bool AreAnyFailed()
        {
            return UploadDirectories.Any(dir => dir.HasError());
        }

        /// <summary>
        /// I think this should just return "external" (TODO - implement and test)
        /// https://developer.android.google.cn/reference/android/provider/MediaStore#VOLUME_EXTERNAL
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetInterestedVolNames()
        {
            HashSet<string> interestedVolnames = new HashSet<string>();
            foreach (var uploadDir in UploadDirectories)
            {
                if (!uploadDir.IsSubdir && uploadDir.UploadDirectory != null)
                {
                    string lastPathSegment = Helpers.GetLastPathSegmentWithSpecialCaseProtection(uploadDir.UploadDirectory, out bool msdCase);
                    if (msdCase)
                    {
                        interestedVolnames.Add(string.Empty); //primary
                    }
                    else
                    {
                        string volName = MainActivity.GetVolumeName(lastPathSegment, true, out _);

                        //this is for if the chosen volume is not primary external
                        if((int)Android.OS.Build.VERSION.SdkInt < 29)
                        {
                            interestedVolnames.Add("external");
                            return interestedVolnames;
                        }
                        var volumeNames = MediaStore.GetExternalVolumeNames(SoulSeekState.ActiveActivityRef); //added in 29
                        string chosenVolume = null;
                        if (volName != null)
                        {
                            string volToCompare = volName.Replace(":", "");
                            foreach (string mediaStoreVolume in volumeNames)
                            {
                                if (mediaStoreVolume.ToLower() == volToCompare.ToLower())
                                {
                                    chosenVolume = mediaStoreVolume;
                                }
                            }
                        }

                        if (chosenVolume == null)
                        {
                            interestedVolnames.Add(string.Empty); //primary
                        }
                        else
                        {
                            interestedVolnames.Add(chosenVolume);
                        }
                    }
                }
            }
            return interestedVolnames;
        }



        public static List<string> PresentableNameLockedDirectories = new List<string>();
        public static List<string> PresentableNameHiddenDirectories = new List<string>();

        public static void UpdateWithDocumentFileAndErrorStates()
        {
            //TESTING 
            //UploadDirectories = new List<UploadDirectoryInfo>();
            //UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic%2F%5B2000%5D%20Spirit%20x%20/document/864A-C3E8%3AMusic%2F%5B2000%5D%20Spirit%20x", true, true, false, null));
            ////UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/primary%3AMusic/document/primary%3AMusic", true, false, false, null));
            //UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic/document/864A-C3E8%3AMusic", true, false, false, "Music (1)"));
            //UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic%2FMusic1/document/864A-C3E8%3AMusic%2FMusic1", true, false, false, null));

            for (int i = 0; i < UploadDirectories.Count; i++)
            {
                UploadDirectoryInfo uploadDirectoryInfo = UploadDirectories[i];

                Android.Net.Uri uploadDirUri = Android.Net.Uri.Parse(uploadDirectoryInfo.UploadDataDirectoryUri);
                //uploadDirectoryInfo.UploadDirectoryUri = uploadDirUri;
                try
                {
                    uploadDirectoryInfo.ErrorState = UploadDirectoryError.NoError;
                    if (SoulSeekState.PreOpenDocumentTree() || !uploadDirectoryInfo.UploadDataDirectoryUriIsFromTree)
                    {
                        uploadDirectoryInfo.UploadDirectory = DocumentFile.FromFile(new Java.IO.File(uploadDirUri.Path));
                    }
                    else
                    {
                        uploadDirectoryInfo.UploadDirectory = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, uploadDirUri);
                        if(!uploadDirectoryInfo.UploadDirectory.Exists())
                        {
                            uploadDirectoryInfo.UploadDirectory = null;
                            uploadDirectoryInfo.ErrorState = UploadDirectoryError.DoesNotExist;
                        }
                        else if (!uploadDirectoryInfo.UploadDirectory.CanWrite())
                        {
                            uploadDirectoryInfo.UploadDirectory = null;
                            uploadDirectoryInfo.ErrorState = UploadDirectoryError.CannotWrite;
                        }
                    }
                }
                catch (Exception e)
                {
                    uploadDirectoryInfo.ErrorState = UploadDirectoryError.Unknown;
                }
            }

            for (int i = 0; i < UploadDirectories.Count; i++)
            {
                UploadDirectoryInfo uploadDirectoryInfo = UploadDirectories[i];
                var ourUri = Android.Net.Uri.Parse(uploadDirectoryInfo.UploadDataDirectoryUri);

                for (int j = 0; j < UploadDirectories.Count; j++)
                {
                    if (i != j)
                    {
                        if (ourUri.LastPathSegment.Contains(Android.Net.Uri.Parse(UploadDirectories[j].UploadDataDirectoryUri).LastPathSegment))
                        {
                            uploadDirectoryInfo.IsSubdir = true;
                        }
                    }
                }
            }

            PresentableNameLockedDirectories.Clear();
            PresentableNameHiddenDirectories.Clear();
            for (int i = 0; i < UploadDirectories.Count; i++)
            {
                UploadDirectoryInfo uploadDirectoryInfo = UploadDirectories[i];
                if (!uploadDirectoryInfo.IsLocked && !uploadDirectoryInfo.IsHidden)
                {
                    continue;
                }

                if (!uploadDirectoryInfo.IsSubdir)
                {
                    if (uploadDirectoryInfo.IsLocked)
                    {
                        PresentableNameLockedDirectories.Add(uploadDirectoryInfo.GetPresentableName());
                    }

                    if (uploadDirectoryInfo.IsHidden)
                    {
                        PresentableNameHiddenDirectories.Add(uploadDirectoryInfo.GetPresentableName());
                    }
                }
                else
                {
                    //find our topmost parent so we can get the effective presentable name

                    var ourUri = Android.Net.Uri.Parse(uploadDirectoryInfo.UploadDataDirectoryUri);

                    UploadDirectoryInfo ourTopLevelParent = null;

                    for (int j = 0; j < UploadDirectories.Count; j++)
                    {
                        if (i != j)
                        {
                            if (!UploadDirectories[j].IsSubdir && ourUri.LastPathSegment.Contains(Android.Net.Uri.Parse(UploadDirectories[j].UploadDataDirectoryUri).LastPathSegment))
                            {
                                ourTopLevelParent = UploadDirectories[j];
                                break;
                            }
                        }
                    }

                    if(!uploadDirectoryInfo.HasError() && !ourTopLevelParent.HasError())  //otherwise pointless + causes nullref
                    {
                        if (uploadDirectoryInfo.IsLocked)
                        {
                            PresentableNameLockedDirectories.Add(uploadDirectoryInfo.GetPresentableName(ourTopLevelParent));
                        }

                        if (uploadDirectoryInfo.IsHidden)
                        {
                            PresentableNameHiddenDirectories.Add(uploadDirectoryInfo.GetPresentableName(ourTopLevelParent));
                        }
                    }
                }

            }
        }

    }


    public static class SoulSeekState
    {
        static SoulSeekState()
        {
            downloadInfoList = new List<DownloadInfo>();
        }

        public static bool InDarkModeCache = false;
        public static bool currentlyLoggedIn = false;
        public static bool AutoClearCompleteDownloads = false;
        public static bool AutoClearCompleteUploads = false;

        public static bool NotifyOnFolderCompleted = true;

        public static bool FreeUploadSlotsOnly = true;
        public static bool DisableDownloadToastNotification = true;
        public const bool AutoRetryDownload = true;

        public static bool HideLockedResultsInSearch = true;
        public static bool HideLockedResultsInBrowse = true;

        public static bool TransferViewShowSizes = false;
        public static bool TransferViewShowSpeed = false;

        public static bool MemoryBackedDownload = false;
        public static bool AutoRetryBackOnline = true; //this is for downloads that fail with the condition "User is Offline". this will also autodownload when we first log in as well.

        public static bool AutoRequeueDownloadsAtStartup = true;

        public static int NumberSearchResults = MainActivity.DEFAULT_SEARCH_RESULTS;
        public static int DayNightMode = (int)(AppCompatDelegate.ModeNightFollowSystem);
        public static ThemeHelper.NightThemeType NightModeVarient = ThemeHelper.NightThemeType.ClassicPurple;
        public static ThemeHelper.DayThemeType DayModeVarient = ThemeHelper.DayThemeType.ClassicPurple;
        public static bool RememberSearchHistory = true;
        public static SoulseekClient SoulseekClient = null;
        public static String Username = null;
        public static bool logoutClicked = false;
        public static String Password = null;
        public static bool SharingOn = false;
        public static bool AllowPrivateRoomInvitations = false;
        public static bool StartServiceOnStartup = true;
        public static bool IsStartUpServiceCurrentlyRunning = false;

        public static bool AllowUploadsOnMetered = true;
        public static bool CurrentConnectionIsUnmetered = true;
        public static bool IsNetworkPermitting()
        {
            return AllowUploadsOnMetered || CurrentConnectionIsUnmetered;
        }

        public static SearchResultSorting DefaultSearchResultSortAlgorithm = SearchResultSorting.Available;

        public static String SaveDataDirectoryUri = null;
        public static bool SaveDataDirectoryUriIsFromTree = true;

        public static string SystemLanguage;
        public static string Language = FieldLangAuto;
        public const string FieldLangAuto = "Auto";
        public const string FieldLangEn = "en";
        public const string FieldLangPtBr = "pt-rBR";
        public const string FieldLangFr = "fr";
        public const string FieldLangRu = "ru";
        public const string FieldLangEs = "es";
        public const string FieldLangUk = "uk"; //ukrainian



        public static String ManualIncompleteDataDirectoryUri = null;
        public static bool ManualIncompleteDataDirectoryUriIsFromTree = true;

        public static bool SpeedLimitDownloadOn = false;
        public static bool SpeedLimitUploadOn = false;
        public static int SpeedLimitDownloadBytesSec = 4 * 1024 * 1024;//1048576;
        public static int SpeedLimitUploadBytesSec = 4 * 1024 * 1024;
        public static bool SpeedLimitDownloadIsPerTransfer = true;
        public static bool SpeedLimitUploadIsPerTransfer = true;

        public static bool ShowSmartFilters = true;
        public static SmartFilterState SmartFilterOptions;

        public static volatile bool DownloadKeepAliveServiceRunning = false;
        public static volatile bool UploadKeepAliveServiceRunning = false;

        public static TimeSpan OffsetFromUtcCached = TimeSpan.Zero;


        public static SlskHelp.SharedFileCache SharedFileCache = null;
        public static int UploadSpeed = -1; //bytes
        public static bool FailedShareParse = false;
        private static volatile bool isParsing = false;

        public static bool NumberOfSharedDirectoriesIsStale = true;
        public static bool AttemptedToSetUpSharing = false;

        public static bool OurCurrentStatusIsAway = false; //bool because it can only be online or away. we set this after we successfully change the status.
        //NOTE: 
        //If we end the connection abruptly (i.e. airplane mode, kill app, turn phone off) then our status will not be changed to offline. (at least after waiting for 20 mins, not sure when it would have)
        //  only if we close the tcp connection properly (FIN, ACK) (i.e. menu > Shut Down) does the server update our status properly to offline.
        //The server does not remember your old status.  So if you log in again after setting your status to away, then your status will be online.  You must set it to away again if desired.
        //There is some weirdness where we only get "GetStatus" (7) messages when we go from online to away.  Otherwise, we dont get anything.  So its not reliable for determining what our status is.
        public enum PendingStatusChange
        {
            NothingPending = 0,
            AwayPending = 1,
            OnlinePending = 2,
        }
        public static PendingStatusChange PendingStatusChangeToAwayOnline = PendingStatusChange.NothingPending;

        public static bool IsParsing
        {
            get
            {
                return isParsing;
            }
            set
            {
                isParsing = value;
                NumberParsed = 0; //reset
            }
        }

        public static int NumberParsed = 0;

        public static List<UserListItem> IgnoreUserList = new List<UserListItem>();
        public static List<UserListItem> UserList = new List<UserListItem>();
        public static RecentUserManager RecentUsersManager = null;
        public static System.Collections.Concurrent.ConcurrentDictionary<string, string> UserNotes = null;
        /// <summary>
        /// There is no concurrent hashset so concurrent dictionary is used. the value is pointless so its only 1 byte.
        /// </summary>
        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte> UserOnlineAlerts = null;
        public static bool ShowRecentUsers = true;

        public static string UserInfoBio = string.Empty;
        public static string UserInfoPictureName = string.Empty; //filename only. The picture will be in (internal storage) FilesDir/user_info_pic/filename.

        public static bool ListenerEnabled = true;
        public static volatile int ListenerPort = 33939;
        public static bool ListenerUPnpEnabled = true;

        public static bool CreateCompleteAndIncompleteFolders = true;
        public static bool CreateUsernameSubfolders = false;
        public static bool OverrideDefaultIncompleteLocations = false;

        public static bool PerformDeepMetadataSearch = true;

        public static bool AutoAwayOnInactivity = false;

        public static EventHandler<EventArgs> DirectoryUpdatedEvent;
        public static EventHandler<EventArgs> SharingStatusChangedEvent;

        /// <summary>
        /// This is only for showing toasts.  The logic is as follows.  If we showed a cancelled toast 
        /// notification <1000ms ago then dont keep showing them. if >1s ago then its okay to show.
        /// They all come in super fast
        /// </summary>
        public static long TaskWasCancelledToastDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

        /// <summary>
        /// This is for when the cancelAndClear button was last pressed.  It is because of the massive amount of cancellation
        /// events all occuring on different threads that all go to affect the service.
        /// </summary>
        public static long CancelAndClearAllWasPressedDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
        public static long AbortAllWasPressedDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

        public struct SmartFilterState
        {
            public bool KeywordsEnabled;
            public int KeywordsOrder;
            public bool NumFilesEnabled;
            public int NumFilesOrder;
            public bool FileTypesEnabled;
            public int FileTypesOrder;
            public List<ChipType> GetEnabledOrder()
            {
                List<Tuple<ChipType, int>> tuples = new List<Tuple<ChipType, int>>();
                if (KeywordsEnabled)
                {
                    tuples.Add(new Tuple<ChipType, int>(ChipType.Keyword, KeywordsOrder));
                }
                if (NumFilesEnabled)
                {
                    tuples.Add(new Tuple<ChipType, int>(ChipType.FileCount, NumFilesOrder));
                }
                if (FileTypesEnabled)
                {
                    tuples.Add(new Tuple<ChipType, int>(ChipType.FileType, FileTypesOrder));
                }
                tuples.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
                return tuples.Select(t1 => t1.Item1).ToList();
            }

            public List<ConfigureChipItems> GetAdapterItems()
            {
                List<Tuple<string, int, bool>> tuples = new List<Tuple<string, int, bool>>();
                tuples.Add(new Tuple<string, int, bool>(GetNameFromEnum(ChipType.Keyword), KeywordsOrder, KeywordsEnabled));
                tuples.Add(new Tuple<string, int, bool>(GetNameFromEnum(ChipType.FileCount), NumFilesOrder, NumFilesEnabled));
                tuples.Add(new Tuple<string, int, bool>(GetNameFromEnum(ChipType.FileType), FileTypesOrder, FileTypesEnabled));
                tuples.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
                return tuples.Select(t1 => new ConfigureChipItems() { Name = t1.Item1, Enabled = t1.Item3 }).ToList();
            }

            public void FromAdapterItems(List<ConfigureChipItems> chipItems)
            {
                for (int i = 0; i < chipItems.Count; i++)
                {
                    ChipType ct = GetEnumFromName(chipItems[i].Name);
                    bool enabled = chipItems[i].Enabled;
                    switch (ct)
                    {
                        case ChipType.Keyword:
                            SoulSeekState.SmartFilterOptions.KeywordsEnabled = enabled;
                            SoulSeekState.SmartFilterOptions.KeywordsOrder = i;
                            break;
                        case ChipType.FileType:
                            SoulSeekState.SmartFilterOptions.FileTypesEnabled = enabled;
                            SoulSeekState.SmartFilterOptions.FileTypesOrder = i;
                            break;
                        case ChipType.FileCount:
                            SoulSeekState.SmartFilterOptions.NumFilesEnabled = enabled;
                            SoulSeekState.SmartFilterOptions.NumFilesOrder = i;
                            break;
                        default:
                            throw new Exception("unknown option");
                    }
                }
            }

            public const string DisplayNameKeyword = "Keywords";
            public const string DisplayNameType = "File Types";
            public const string DisplayNameCount = "# Files";

            public string GetNameFromEnum(ChipType chipType)
            {
                switch (chipType)
                {
                    case ChipType.Keyword:
                        return DisplayNameKeyword;
                    case ChipType.FileType:
                        return DisplayNameType;
                    case ChipType.FileCount:
                        return DisplayNameCount;
                    default:
                        throw new Exception("unknown enum");
                }
            }

            public ChipType GetEnumFromName(string name)
            {
                switch (name)
                {
                    case DisplayNameKeyword:
                        return ChipType.Keyword;
                    case DisplayNameType:
                        return ChipType.FileType;
                    case DisplayNameCount:
                        return ChipType.FileCount;
                    default:
                        throw new Exception("unknown enum");
                }
            }
        }


        public static void ClearSearchHistoryEventsFromTarget(object target)
        {
            if (ClearSearchHistory == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in ClearSearchHistory.GetInvocationList())
                {
                    if (d.Target.GetType() == target.GetType())
                    {
                        ClearSearchHistory -= (EventHandler<EventArgs>)d;
                    }
                }
            }
        }

        public static void ClearSearchHistoryInvoke()
        {
            ClearSearchHistory?.Invoke(null, null);
        }



        //public static event EventHandler<DownloadAddedEventArgs> DownloadAdded;
        /// <summary>
        /// Occurs after we set up the DownloadAdded transfer item.
        /// </summary>

        public static event EventHandler<EventArgs> ClearSearchHistory;
        public static List<DownloadInfo> downloadInfoList;
        /// <summary>
        /// Context of last created activity
        /// </summary>
        public static volatile FragmentActivity ActiveActivityRef = null;
        public static ISharedPreferences SharedPreferences;
        public static volatile MainActivity MainActivityRef;

        public static bool RequiresEitherOpenDocumentTreeOrManageAllFiles()
        {
            //29 does has the requestExternalStorage workaround.
            return Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.R;
        }

        public static bool UseLegacyStorage()
        {
            return Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.Q;
        }

        public static bool PreOpenDocumentTree()
        {
            return Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.Lollipop;
        }

        public static bool PreMoveDocument()
        {
            return Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.N;
        }

        public static bool IsLowDpi()
        {
            return Android.Content.Res.Resources.System.DisplayMetrics.WidthPixels < 768;
        }

        public static ManualResetEvent ManualResetEvent = new ManualResetEvent(false); //previously this was on the loginfragment but
                                                                                       //it would get recreated every time so there were lost instances with threads waiting forever....

        //public static void OnDownloadAdded(DownloadInfo dlInfo)
        //{
        //    DownloadAdded(null,new DownloadAddedEventArgs(dlInfo));
        //}

        public const string M_CurrentlyLoggedIn = "Momento_LoggedIn";
        public const string M_Username = "Momento_Username";
        public const string M_Password = "Momento_Password";
        public const string M_TransferList = "Momento_List";
        public const string M_TransferList_v2 = "Momento_UpdatedTransferListWithFolders";
        public const string M_TransferListUpload = "Momento_Upload_List";
        public const string M_TransferListUpload_v2 = "Momento_Upload_UpdatedTransferListWithFolders";
        public const string M_Messages = "Momento_Messages";
        public const string M_UnreadMessageUsernames = "Momento_UnreadMessageUsernames";
        public const string M_SearchHistory = "Momento_SearchHistoryArray";

        public const string M_DefaultSearchResultSortAlgorithm = "Momento_DefaultSearchResultSortAlgorithm";
        public const string M_UserListSortOrder = "Momento_UserListSortHistory";

        public const string M_AutoSetAwayOnInactivity = "Momento_AutoSetAwayOnInactivity";
        public const string M_AutoRetryBackOnline = "Momento_AutoRetryBackOnline";
        public const string M_NotifyFolderComplete = "Momento_NotifyFolderComplete";

        public const string M_AllowUploadsOnMetered = "Momento_AllowUploadsOnMetered";

        public const string M_LimitSimultaneousDownloads = "Momento_LimitSimultaneousDownloads";
        public const string M_MaxSimultaneousLimit = "Momento_MaxSimultaneousLimit";


        public const string M_RoomUserListSortOrder = "Momento_RoomUserListSortOrder";
        public const string M_RoomUserListShowFriendsAtTop = "Momento_RoomUserListShowFriendsAtTop";

        public const string M_ShowStatusesView = "Momento_ShowStatusesView";
        public const string M_ShowTickerView = "Momento_ShowTickerView";

        public const string M_LOG_DIAGNOSTICS = "Momento_LOG_DIAGNOSTICS";
        //public const string M_UserListSortOrder = "Momento_UserListSortHistory";


        public const string M_SaveDataDirectoryUri = "Momento_SaveDataDirectoryUri";
        public const string M_SaveDataDirectoryUriIsFromTree = "Momento_SaveDataDirectoryUriIsFromTree";
        public const string M_NumberSearchResults = "Momento_NumberSearchResults";
        public const string M_DayNightMode = "Momento_DayNightMode";
        public const string M_Lanuage = "Momento_Lanuage";
        public const string M_NightVarient = "Momento_NightModeVarient";
        public const string M_DayVarient = "Momento_DayModeVarient";
        public const string M_AutoClearComplete = "Momento_AutoClearComplete";
        public const string M_AutoClearCompleteUploads = "Momento_AutoClearCompleteUploads";
        public const string M_RememberSearchHistory = "Momento_RememberSearchHistory";
        public const string M_RememberUserHistory = "Momento_RememberUserHistory";
        public const string M_OnlyFreeUploadSlots = "Momento_FreeUploadSlots";
        public const string M_HideLockedBrowse = "Momento_HideLockedBrowse";
        public const string M_HideLockedSearch = "Momento_HideLockedSearch";
        public const string M_TransfersShowSizes = "Momento_TransfersShowSizes";
        public const string M_TransfersShowSpeed = "Momento_TransfersShowSpeed";
        public const string M_DisableToastNotifications = "Momento_DisableToastNotifications";
        public const string M_MemoryBackedDownload = "Momento_MemoryBackedDownload";
        public const string M_FilterSticky = "Momento_FilterSticky";
        public const string M_FilterStickyString = "Momento_FilterStickyString";
        public const string M_SearchResultStyle = "Momento_SearchResStyle";
        public const string M_UserList = "Cache_UserList";
        public const string M_RecentUsersList = "Momento_RecentUsersList";
        public const string M_IgnoreUserList = "Cache_IgnoreUserList";
        public const string M_JoinedRooms = "Cache_JoinedRooms";
        public const string M_AllowPrivateRooomInvitations = "Momento_AllowPrivateRoomInvitations";
        public const string M_ServiceOnStartup = "Momento_ServiceOnStartup";
        public const string M_ShowSmartFilters = "Momento_ShowSmartFilters";
        public const string M_chatroomsToNotify = "Momento_chatroomsToNotify";
        public const string M_SearchTabsState_LEGACY = "Momento_SearchTabsState";
        public const string M_SearchTabsState_Headers = "Momento_SearchTabsState_Headers";

        public const string M_UploadSpeed = "Momento_UploadSpeed";
        public const string M_UploadDirectoryUri = "Momento_UploadDirectoryUri";
        public const string M_UploadDirectoryUriIsFromTree = "Momento_UploadDirectoryUriIsFromTree";
        public const string M_SharingOn = "Momento_SharingOn";

        public const string M_PortMapped = "Momento_PortMapped";
        public const string M_LastSetUpnpRuleTicks = "Momento_LastSetUpnpRuleTicks";
        public const string M_LifetimeSeconds = "Momento_LifetimeSeconds";
        public const string M_LastSetLocalIP = "Momento_LastSetLocalIP";
        public const string M_SharedDirectoryInfo = "Momento_SharedDirectoryInfo";


        public const string M_CACHE_stringUriPairs = "Cache_stringUriPairs";
        public const string M_CACHE_browseResponse = "Cache_browseResponse";
        public const string M_CACHE_friendlyDirNameToUriMapping = "Cache_friendlyDirNameToUriMapping";
        public const string M_CACHE_auxDupList = "Cache_auxDupList";

        public const string M_CACHE_stringUriPairs_v3 = "Cache_stringUriPairs_v3";

        public const string M_CACHE_stringUriPairs_v2 = "Cache_stringUriPairs_v2";
        public const string M_CACHE_browseResponse_v2 = "Cache_browseResponse_v2";
        public const string M_CACHE_browseResponse_hidden_portion = "Cache_browseResponse_hidden_portion";
        public const string M_CACHE_friendlyDirNameToUriMapping_v2 = "Cache_friendlyDirNameToUriMapping_v2";
        public const string M_CACHE_intHelperIndex_v2 = "CACHE_intHelperIndex_v2";
        public const string M_CACHE_tokenIndex_v2 = "CACHE_tokenIndex_v2";
        public const string M_CACHE_nonHiddenFileCount_v3 = "CACHE_nonHiddenFileCountToReportToServer";


        public const string M_ListenerEnabled = "Momento_ListenerEnabled";
        public const string M_ListenerPort = "Momento_ListenerPort";
        public const string M_ListenerUPnpEnabled = "Momento_ListenerUPnpEnabled";

        public const string M_DownloadLimitEnabled = "M_DownloadLimitEnabled";
        public const string M_DownloadPerTransfer = "M_DownloadPerTransfer";
        public const string M_DownloadSpeedLimitBytes = "M_DownloadSpeedLimitBytes";

        public const string M_UploadLimitEnabled = "M_UploadLimitEnabled";
        public const string M_UploadPerTransfer = "M_UploadPerTransfer";
        public const string M_UploadSpeedLimitBytes = "M_UploadSpeedLimitBytes";


        public const string M_UserInfoBio = "Momento_UserInfoBio";
        public const string M_UserInfoPicture = "Momento_UserInfoPicture";
        public const string M_UserNotes = "Momento_UserNotes";
        public const string M_UserOnlineAlerts = "Momento_UserOnlineAlerts";

        public const string M_ManualIncompleteDirectoryUri = "Momento_ManualIncompleteDirectoryUri";
        public const string M_ManualIncompleteDirectoryUriIsFromTree = "Momento_ManualIncompleteDirectoryUriIsFromTree";
        public const string M_UseManualIncompleteDirectoryUri = "Momento_UseManualIncompleteDirectoryUri";
        public const string M_CreateCompleteAndIncompleteFolders = "Momento_CreateCompleteIncomplete";
        public const string M_AdditionalUsernameSubdirectories = "Momento_M_AdditionalUsernameSubdirectories";

        public const string M_PostNotificationRequestAlreadyShown = "Momento_M_PostNotificationRequestAlreadyShown";

        public const string M_SmartFilter_KeywordsEnabled = "Momento_SmartFilterKeywordsEnabled";
        public const string M_SmartFilter_CountsEnabled = "Momento_SmartFilterCountsEnabled";
        public const string M_SmartFilter_TypesEnabled = "Momento_SmartFilterTypesEnabled";
        public const string M_SmartFilter_KeywordsOrder = "Momento_SmartFilterKeywordsOrder";
        public const string M_SmartFilter_CountsOrder = "Momento_SmartFilterCountsOrder";
        public const string M_SmartFilter_TypesOrder = "Momento_SmartFilterTypesOrder";

        public static AndroidX.DocumentFile.Provider.DocumentFile DiagnosticTextFile = null;
        public static System.IO.StreamWriter DiagnosticStreamWriter = null;

        public static event EventHandler<BrowseResponseEvent> BrowseResponseReceived;
        public static AndroidX.DocumentFile.Provider.DocumentFile RootDocumentFile = null;
        public static AndroidX.DocumentFile.Provider.DocumentFile RootIncompleteDocumentFile = null; //only gets set if can write the dir...
        public static void OnBrowseResponseReceived(BrowseResponse origBR, TreeNode<Directory> rootTree, string fromUsername, string startingLocation)
        {
            BrowseResponseReceived(null, new BrowseResponseEvent(origBR, rootTree, fromUsername, startingLocation));
        }
        public static void ClearOnBrowseResponseReceivedEventsFromTarget(object target)
        {
            if (BrowseResponseReceived == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in BrowseResponseReceived.GetInvocationList())
                {
                    if (d.Target.GetType() == target.GetType())
                    {
                        BrowseResponseReceived -= (EventHandler<BrowseResponseEvent>)d;
                    }
                }
            }
        }
    }

    public class ViewPagerFixed : ViewPager
    {
        /// <summary>
        /// Fixes this:
        ///ava.lang.IllegalArgumentException: pointerIndex out of range
        ///at android.view.MotionEvent.nativeGetAxisValue(Native Method)
        ///at android.view.MotionEvent.getX(MotionEvent.java:1981)
        ///at AndroidX.Core.View.MotionEventCompatEclair.getX(MotionEventCompatEclair.java:32)
        ///at AndroidX.Core.View.MotionEventCompat$EclairMotionEventVersionImpl.getX(MotionEventCompat.java:86)
        ///at AndroidX.Core.View.MotionEventCompat.getX(MotionEventCompat.java:184)
        ///at AndroidX.ViewPager.Widget.ViewPager.onInterceptTouchEvent(ViewPager.java:1339)
        /// </summary>
        /// <param name="context"></param>

        public ViewPagerFixed(Context context) : base(context)
        {

        }

        public ViewPagerFixed(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs)
        {

        }

        public ViewPagerFixed(IntPtr intPtr, JniHandleOwnership handle) : base(intPtr, handle)
        {

        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            try
            {
                return base.OnTouchEvent(ev);
            }
            catch (Exception)
            {

            }
            return false;
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            try
            {
                return base.OnInterceptTouchEvent(ev); //this can throw a random 
            }
            catch (Exception)
            {

            }
            return false;
        }
    }

    public class BrowseResponseEvent
    {
        public TreeNode<Directory> BrowseResponseTree;
        public BrowseResponse OriginalBrowseResponse;
        public string Username;
        public string StartingLocation;
        public BrowseResponseEvent(BrowseResponse origBR, TreeNode<Directory> t, string u, string startingLocation)
        {
            OriginalBrowseResponse = origBR;
            Username = u;
            BrowseResponseTree = t;
            StartingLocation = startingLocation;
        }
    }

    public static class StaticHacks
    {
        public static bool LoggingIn = false;
        public static bool UpdateUI = false;
        public static View RootView = null;
        public static AndroidX.Fragment.App.Fragment LoginFragment = null;
        public static TransfersFragment TransfersFrag = null;
    }

    public static class Helpers
    {


        public static string AvoidLineBreaks(string orig)
        {
            return orig.Replace(' ', '\u00A0').Replace("\\", "\\\u2060");
        }
        /// <summary>
        /// This is necessary since DocumentFile.ListFiles() returns files in an incomprehensible order (not by name, size, modified, inode, etc.)
        /// </summary>
        /// <param name="files"></param>
        public static void SortSlskDirFiles(List<Soulseek.File> files)
        {
            files.Sort((x, y) => x.Filename.CompareTo(y.Filename));
        }

        public static bool CompleteIncompleteDifferentVolume()
        {
            if (SettingsActivity.UseIncompleteManualFolder() && SoulSeekState.RootIncompleteDocumentFile != null && SoulSeekState.RootDocumentFile != null)
            {
                //if(!SoulSeekState.UseLegacyStorage())
                //{
                //    //this method is only for API29+
                //    //var sm = (SoulSeekState.ActiveActivityRef.GetSystemService(Context.StorageService) as Android.OS.Storage.StorageManager);
                //    //Android.OS.Storage.StorageVolume sv1 = sm.GetStorageVolume(SoulSeekState.RootDocumentFile.Uri); //fails if not media store uri
                //    //string uuid1 = sv1.Uuid;
                //    //Android.OS.Storage.StorageVolume sv2 = sm.GetStorageVolume(SoulSeekState.RootIncompleteDocumentFile.Uri);
                //    //string uuid2 = sv2.Uuid;


                //    string volume1 = MainActivity.GetVolumeName(SoulSeekState.RootDocumentFile.Uri.LastPathSegment, out _);
                //    string volume2 = MainActivity.GetVolumeName(SoulSeekState.RootIncompleteDocumentFile.Uri.LastPathSegment, out _);

                //    return uuid1 != uuid2;
                //}
                //else
                //{
                try
                {
                    string volume1 = MainActivity.GetVolumeName(SoulSeekState.RootDocumentFile.Uri.LastPathSegment, false, out bool everything);
                    if (everything)
                    {
                        volume1 = SoulSeekState.RootDocumentFile.Uri.LastPathSegment;
                    }
                    string volume2 = MainActivity.GetVolumeName(SoulSeekState.RootIncompleteDocumentFile.Uri.LastPathSegment, false, out everything);
                    if (everything)
                    {
                        volume2 = SoulSeekState.RootIncompleteDocumentFile.Uri.LastPathSegment;
                    }
                    return volume1 != volume2;
                }
                catch (Exception e)
                {
                    MainActivity.LogFirebase("CompleteIncompleteDifferentVolume failed: " + e.Message + SoulSeekState.RootDocumentFile?.Uri?.LastPathSegment + " incomplete: " + SoulSeekState.RootIncompleteDocumentFile?.Uri?.LastPathSegment);
                    return false;
                }
                //}
            }
            else
            {
                return false;
            }
        }

        public static string GenerateIncompleteFolderName(string username, string fullFileName, int depth)
        {
            string albumFolderName = null;
            if (depth == 1)
            {
                albumFolderName = Helpers.GetFolderNameFromFile(fullFileName, depth);
            }
            else
            {
                albumFolderName = Helpers.GetFolderNameFromFile(fullFileName, depth);
                albumFolderName = albumFolderName.Replace('\\', '_');
            }
            string incompleteFolderName = username + "_" + albumFolderName;
            //Path.GetInvalidPathChars() doesnt seem like enough bc I still get failures on ''' and '&'
            foreach (char c in System.IO.Path.GetInvalidPathChars().Union(new[] { '&', '\'' }))
            {
                incompleteFolderName = incompleteFolderName.Replace(c, '_');
            }
            return incompleteFolderName;
        }

        public static bool IsFileUri(string uriString)
        {
            if (uriString.StartsWith("file:"))
            {
                return true;
            }
            else if (uriString.StartsWith("content:"))
            {
                return false;
            }
            else
            {
                throw new Exception("IsFileUri failed: " + uriString);
            }
        }


        public static string GetNiceDateTime(DateTime dt)
        {
            System.Globalization.CultureInfo cultureInfo = null;
            try
            {
                cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("CANNOT GET CURRENT CULTURE: " + e.Message + e.StackTrace);
            }
            if (dt.Date == Helpers.GetDateTimeNowSafe().Date)
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.today) + " " + dt.ToString("h:mm:ss tt", cultureInfo); //cultureInfo can be null without issue..
            }
            else
            {
                return dt.ToString("MMM d h:mm:ss tt", cultureInfo);
            }
        }

        [Flags]
        public enum SpecialMessageType : short
        {
            None = 0,
            SlashMe = 1,
            MagnetLink = 2,
            SlskLink = 4,
        }

        /// <summary>
        /// true if '/me ' message
        /// </summary>
        /// <returns>true if special message</returns>
        public static bool IsSpecialMessage(string msg, out SpecialMessageType specialMessageType)
        {
            specialMessageType = SpecialMessageType.None;
            if (string.IsNullOrEmpty(msg))
            {
                return false;
            }
            if (msg.StartsWith(@"/me "))
            {
                specialMessageType = SpecialMessageType.SlashMe;
                return true;
            }
            if (msg.Contains(@"magnet:?xt=urn:"))
            {
                specialMessageType = SpecialMessageType.MagnetLink;
                return true;
            }
            if (msg.Contains(@"slsk://"))
            {
                specialMessageType = SpecialMessageType.SlskLink;
                return true;
            }
            return false;
        }

        private readonly static System.Text.RegularExpressions.Regex MagnetLinkRegex = new System.Text.RegularExpressions.Regex(@"magnet:\?xt=urn:[^ ""]+");
        private readonly static System.Text.RegularExpressions.Regex SlskLinkRegex = new System.Text.RegularExpressions.Regex(@"slsk://[^ ""]+");

        public static void ConfigureSpecialLinks(TextView textView, string msgText, SpecialMessageType specialMessageType)
        {
            Android.Text.SpannableString messageText = new Android.Text.SpannableString(msgText);
            if (specialMessageType.HasFlag(SpecialMessageType.MagnetLink))
            {
                var matches = MagnetLinkRegex.Matches(msgText);
                //add in our spans.
                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                    {
                        var m = match as System.Text.RegularExpressions.Match;
                        var ourMagnetSpan = new MagnetLinkClickableSpan(m.Value);
                        messageText.SetSpan(ourMagnetSpan, m.Index, m.Index + m.Length, Android.Text.SpanTypes.InclusiveExclusive);
                    }
                }
            }
            if (specialMessageType.HasFlag(SpecialMessageType.SlskLink))
            {
                var matches = SlskLinkRegex.Matches(msgText);
                //add in our spans.
                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                    {
                        var m = match as System.Text.RegularExpressions.Match;
                        var ourSlskSpan = new SlskLinkClickableSpan(m.Value);
                        messageText.SetSpan(ourSlskSpan, m.Index, m.Index + m.Length, Android.Text.SpanTypes.InclusiveExclusive);
                    }
                }
            }
            textView.MovementMethod = Android.Text.Method.LinkMovementMethod.Instance; //needed for slsk:// not needed for magnet. weird.
            textView.TextFormatted = messageText;
        }

        public static string ParseSpecialMessage(string msg)
        {
            if (IsSpecialMessage(msg, out SpecialMessageType specialMessageType))
            {
                //if slash me dont include other special links, too excessive.
                if (specialMessageType == SpecialMessageType.SlashMe)
                {
                    //"/me goes to the store"
                    //"goes to the store" + style
                    return msg.Substring(4, msg.Length - 4);
                }
                else
                {
                    return msg;
                }
            }
            else
            {
                return msg;

            }
        }

        public static void AddUserNoteMenuItem(IMenu menu, int i, int j, int k, string username)
        {
            string title = null;
            if (SoulSeekState.UserNotes.ContainsKey(username))
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.edit_note);
            }
            else
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_note);
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static void AddUserOnlineAlertMenuItem(IMenu menu, int i, int j, int k, string username)
        {
            string title = null;
            if (SoulSeekState.UserOnlineAlerts.ContainsKey(username))
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_online_alert);
            }
            else
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.set_online_alert);
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static void SetIgnoreUnignoreTitle(IMenuItem menuItem, string username)
        {
            if (menuItem != null && !string.IsNullOrEmpty(username))
            {
                if (SeekerApplication.IsUserInIgnoreList(username)) //if we already have added said user, change title add to remove..
                {
                    if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.ignore_user))
                    {
                        menuItem.SetTitle(Resource.String.remove_from_ignored);
                    }
                }
                else
                {
                    if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_from_ignored))
                    {
                        menuItem.SetTitle(Resource.String.ignore_user);
                    }
                }
            }
        }

        private static void SetAddRemoveTitle(IMenuItem menuItem, string username)
        {
            if (menuItem != null && !string.IsNullOrEmpty(username))
            {
                if (MainActivity.UserListContainsUser(username)) //if we already have added said user, change title add to remove..
                {
                    if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_to_user_list))
                    {
                        menuItem.SetTitle(Resource.String.remove_from_user_list);
                    }
                    else if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_user))
                    {
                        menuItem.SetTitle(Resource.String.remove_user);
                    }
                }
                else
                {
                    if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_from_user_list))
                    {
                        menuItem.SetTitle(Resource.String.add_to_user_list);
                    }
                    else if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_user))
                    {
                        menuItem.SetTitle(Resource.String.add_user);
                    }
                }
            }
        }

        private static void SetAddNoteEditNoteTitle(IMenuItem menuItem, string username)
        {
            if (menuItem != null && !string.IsNullOrEmpty(username))
            {
                if (SoulSeekState.UserNotes.ContainsKey(username)) //if we already have added said user, change title add to remove..
                {
                    if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_note))
                    {
                        menuItem.SetTitle(Resource.String.edit_note);
                    }

                }
                else
                {
                    if (menuItem.TitleFormatted.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.edit_note))
                    {
                        menuItem.SetTitle(Resource.String.add_note);
                    }
                }
            }
        }

        public static void SetMenuTitles(IMenu menu, string username)
        {
            var menuItem = menu.FindItem(Resource.Id.action_add_to_user_list);
            SetAddRemoveTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.action_add_user);
            SetAddRemoveTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.addUser);
            SetAddRemoveTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.action_add_note);
            SetAddNoteEditNoteTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.action_ignore);
            SetIgnoreUnignoreTitle(menuItem, username);
        }

        public static void SetIgnoreAddExclusive(IMenu menu, string username)
        {
            // if we added this user as a friend do not show the option to ignore. they must be removed first.
            if(!string.IsNullOrEmpty(username))
            {
                bool isInUserList = MainActivity.UserListContainsUser(username);
                var menuItem = menu.FindItem(Resource.Id.action_ignore);
                menuItem?.SetVisible(!isInUserList);
            }


            // if we have this user in ignore, do not show the option to add as friend.
            if(!string.IsNullOrEmpty(username))
            {
                bool isInIgnoreList = SeekerApplication.IsUserInIgnoreList(username);
                var menuItem = menu.FindItem(Resource.Id.action_add_to_user_list);
                menuItem?.SetVisible(!isInIgnoreList);
                menuItem = menu.FindItem(Resource.Id.action_add_user);
                menuItem?.SetVisible(!isInIgnoreList);
                menuItem = menu.FindItem(Resource.Id.action_add_note);
                menuItem?.SetVisible(!isInIgnoreList);
            }
        }


        public static void AddAddRemoveUserMenuItem(IMenu menu, int i, int j, int k, string username, bool full_title = false)
        {
            string title = null;
            if (!MainActivity.UserListContainsUser(username))
            {
                if (full_title)
                {
                    title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_to_user_list);
                }
                else
                {
                    title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_user);
                }
            }
            else
            {
                if (full_title)
                {
                    title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_from_user_list);
                }
                else
                {
                    title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_user);
                }
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static void AddIgnoreUnignoreUserMenuItem(IMenu menu, int i, int j, int k, string username)
        {
            //ignored and added are mutually exclusive.  you cannot have a user be both ignored and added.
            if (MainActivity.UserListContainsUser(username))
            {
                return;
            }
            string title = null;
            if (!SeekerApplication.IsUserInIgnoreList(username))
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.ignore_user);
            }
            else
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_from_ignored);
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static DateTime GetDateTimeNowSafe()
        {
            try
            {
                return DateTime.Now;
            }
            catch(System.TimeZoneNotFoundException)
            {
                return DateTime.UtcNow;
            }
        }

        public static void AddGivePrivilegesIfApplicable(IMenu menu, int indexToUse)
        {
            if (PrivilegesManager.Instance.GetRemainingDays() >= 1)
            {
                if (indexToUse == -1)
                {
                    menu.Add(Resource.String.give_privileges);
                }
                else
                {
                    menu.Add(indexToUse, indexToUse, indexToUse, Resource.String.give_privileges);
                }
            }
        }

        public static PendingIntentFlags AppendMutabilityIfApplicable(PendingIntentFlags existingFlags, bool immutable)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 23)
            {
                if (immutable)
                {
                    return existingFlags | PendingIntentFlags.Immutable;
                }
                else
                {
                    return existingFlags | PendingIntentFlags.Mutable;
                }
            }
            else
            {
                //immutable flag was only introduced in 23 so if less than that we always need to OR with mutable (or we can just leave it alone). (remember mutable is the default)
                return existingFlags;
            }
        }

        public static void DoNotEnablePositiveUntilText(AndroidX.AppCompat.App.AlertDialog dialog, EditText input)
        {
            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
            // note: this will be null if .Show() has not been called.
            if (positiveButton == null)
            {
                // better to be safe.
                return;
            }
            positiveButton.Enabled = false;

            void Input_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
            {
                if (string.IsNullOrEmpty(input.Text))
                {
                    positiveButton.Enabled = false;
                }
                else
                {
                    positiveButton.Enabled = true;
                }
            }
            input.AfterTextChanged += Input_AfterTextChanged;
        }

        /// <summary>
        /// returns true if found and handled.  a time saver for the more generic context menu items..
        /// </summary>
        /// <returns></returns>
        public static bool HandleCommonContextMenuActions(string contextMenuTitle, string usernameInQuestion, Context activity, View browseSnackView, Action uiUpdateActionNote = null, Action uiUpdateActionAdded_Removed = null, Action uiUpdateActionIgnored_Unignored = null, Action uiUpdateSetResetOnlineAlert = null)
        {
            if (activity == null)
            {
                activity = SoulSeekState.ActiveActivityRef;
            }
            if (contextMenuTitle == activity.GetString(Resource.String.ignore_user))
            {
                SeekerApplication.AddToIgnoreListFeedback(activity, usernameInQuestion);
                SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_from_ignored))
            {
                SeekerApplication.RemoveFromIgnoreListFeedback(activity, usernameInQuestion);
                SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.msg_user))
            {
                Intent intentMsg = new Intent(activity, typeof(MessagesActivity));
                intentMsg.AddFlags(ActivityFlags.SingleTop);
                intentMsg.PutExtra(MessageController.FromUserName, usernameInQuestion); //so we can go to this user..
                intentMsg.PutExtra(MessageController.ComingFromMessageTapped, true); //so we can go to this user..
                activity.StartActivity(intentMsg);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.add_to_user_list) ||
                contextMenuTitle == activity.GetString(Resource.String.add_user))
            {
                UserListActivity.AddUserAPI(SoulSeekState.ActiveActivityRef, usernameInQuestion, uiUpdateActionAdded_Removed);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_from_user_list) ||
                contextMenuTitle == activity.GetString(Resource.String.remove_user))
            {
                MainActivity.ToastUI_short(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.removed_user), usernameInQuestion));
                MainActivity.UserListRemoveUser(usernameInQuestion);
                SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateActionAdded_Removed);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.search_user_files))
            {
                SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                SearchTabHelper.SearchTargetChosenUser = usernameInQuestion;
                //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                Intent intent = new Intent(activity, typeof(MainActivity));
                intent.PutExtra(UserListActivity.IntentUserGoToSearch, 1);
                intent.AddFlags(ActivityFlags.SingleTop); //??
                activity.StartActivity(intent);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.browse_user))
            {
                Action<View> action = new Action<View>((v) =>
                {
                    Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                    intent.AddFlags(ActivityFlags.SingleTop); //??
                    activity.StartActivity(intent);
                    //((AndroidX.ViewPager.Widget.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                });
                DownloadDialog.RequestFilesApi(usernameInQuestion, browseSnackView, action, null);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.get_user_info))
            {
                RequestedUserInfoHelper.RequestUserInfoApi(usernameInQuestion);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.give_privileges))
            {
                ShowGivePrilegesDialog(usernameInQuestion);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.edit_note) ||
                    contextMenuTitle == activity.GetString(Resource.String.add_note))
            {
                ShowEditAddNoteDialog(usernameInQuestion, uiUpdateActionNote);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.set_online_alert))
            {
                SoulSeekState.UserOnlineAlerts[usernameInQuestion] = 0;
                Helpers.SaveOnlineAlerts();
                uiUpdateSetResetOnlineAlert();
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_online_alert))
            {
                SoulSeekState.UserOnlineAlerts.TryRemove(usernameInQuestion, out _);
                Helpers.SaveOnlineAlerts();
                uiUpdateSetResetOnlineAlert();

            }
            return false;
        }

        public static void SetMessageTextView(TextView viewMessage, Message msg)
        {
            if (Helpers.IsSpecialMessage(msg.MessageText, out SpecialMessageType specialMessageType))
            {
                if (specialMessageType.HasFlag(SpecialMessageType.SlashMe))
                {
                    viewMessage.Text = Helpers.ParseSpecialMessage(msg.MessageText);
                    viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Italic);
                }
                else if (specialMessageType.HasFlag(SpecialMessageType.MagnetLink) || specialMessageType.HasFlag(SpecialMessageType.SlskLink))
                {
                    viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Normal);
                    Helpers.ConfigureSpecialLinks(viewMessage, msg.MessageText, specialMessageType);
                }
                else
                {
                    //fallback
                    viewMessage.Text = msg.MessageText;
                    viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Normal);
                }
            }
            else
            {
                viewMessage.Text = msg.MessageText;
                viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Normal);
            }
        }

        public static string GetNiceDateTimeGroupChat(DateTime dt)
        {
            System.Globalization.CultureInfo cultureInfo = null;
            try
            {
                cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("CANNOT GET CURRENT CULTURE: " + e.Message + e.StackTrace);
            }
            if (dt.Date == Helpers.GetDateTimeNowSafe().Date)
            {
                return dt.ToString("h:mm:ss tt", cultureInfo); //this is the only difference...
            }
            else
            {
                return dt.ToString("MMM d h:mm:ss tt", cultureInfo);
            }
        }

        public static void CopyTextToClipboard(Activity a, string txt)
        {
            var clipboardManager = a.GetSystemService(Context.ClipboardService) as ClipboardManager;
            ClipData clip = ClipData.NewPlainText("simple text", txt);
            clipboardManager.PrimaryClip = clip;
        }

        public static string GetFileNameFromFile(string filename) //is also used to get the last folder
        {
            int begin = filename.LastIndexOf("\\");
            string clipped = filename.Substring(begin + 1);
            return clipped;
        }

        public static string GetAllButLast(string path) //"raw:\\storage\\emulated\\0\\Download\\Soulseek Complete"
        {
            int end = path.LastIndexOf("\\");
            string clipped = path.Substring(0, end);
            return clipped; //"raw:\\storage\\emulated\\0\\Download"
        }


        //this is a helper for this issue:
        //var name1 = df.CreateFile("audio/m4a", "name1").Name;
        //var name2 = df.CreateFile("audio/x-m4a", "name2").Name;
        //  are both extensionless....
        public static DocumentFile CreateMediaFile(DocumentFile parent, string name)
        {
            if (Helpers.GetMimeTypeFromFilename(name) == M4A_MIME)
            {
                return parent.CreateFile(Helpers.GetMimeTypeFromFilename(name), name); //we use just name since it will not add the .m4a extension for us..
            }
            else if (Helpers.GetMimeTypeFromFilename(name) == APE_MIME)
            {
                return parent.CreateFile(Helpers.GetMimeTypeFromFilename(name), name); //we use just name since it will not add the .ape extension for us..
            }
            else if (Helpers.GetMimeTypeFromFilename(name) == null)
            {
                //a null mimetype is fine, it just defaults to application/octet-stream
                return parent.CreateFile(null, name); //we use just name since it will not add the extension for us..
            }
            else
            {
                return parent.CreateFile(Helpers.GetMimeTypeFromFilename(name), System.IO.Path.GetFileNameWithoutExtension(name));
            }
        }



        //examples..
        //Helpers.GetMimeTypeFromFilename("x.flac");//"audio/flac"
        //Helpers.GetMimeTypeFromFilename("x.mp3"); //"audio/mpeg"
        //Helpers.GetMimeTypeFromFilename("x.wmv"); //"video/x-ms-wmv"
        //Helpers.GetMimeTypeFromFilename("x.wma"); // good
        //Helpers.GetMimeTypeFromFilename("x.png"); //"image/png"
        //THIS FAILS MISERABLY FOR M4A FILES. it regards them as mp3, causing both android and windows foobar to deem them corrupted and refuse to play them!
        //[seeker] .wma === audio/x-ms-wma
        //[seeker] .flac === audio/flac
        //[seeker] .aac === audio/aac
        //[seeker] .m4a === audio/mpeg  --- miserable failure should be audio/m4a or audio/x-m4a
        //[seeker] .mp3 === audio/mpeg
        //[seeker] .oga === audio/ogg
        //[seeker] .ogg === audio/ogg
        //[seeker] .opus === audio/ogg
        //[seeker] .wav === audio/x-wav
        //[seeker] .mp4 === video/mp4

        //other problematic - 
        //        ".alac", -> null
        //        ".ape",  -> null  // audio/x-ape
        //        ".m4p" //aac with apple drm. similar to the drm free m4a. audio/m4p not mp4 which is reported. I am not sure...
        public const string M4A_MIME = "audio/m4a";
        public const string APE_MIME = "audio/x-ape";
        public static string GetMimeTypeFromFilename(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename).ToLower();
            string mimeType = @"audio/mpeg"; //default
            if (ext != null && ext != string.Empty)
            {
                switch (ext)
                {
                    case ".ape":
                        mimeType = APE_MIME;
                        break;
                    case ".m4a":
                        mimeType = M4A_MIME;
                        break;
                    default:
                        ext = ext.TrimStart('.');
                        mimeType = Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(ext);
                        break;
                }

            }
            return mimeType;
        }

        public static void ViewUri(Android.Net.Uri httpUri, Context c)
        {
            try
            {
                Intent intent = new Intent(Intent.ActionView, httpUri);
                c.StartActivity(intent);
            }
            catch (Exception e)
            {
                if (e.Message.Contains(Helpers.NoDocumentOpenTreeToHandle))
                {
                    MainActivity.LogFirebase("viewUri: " + e.Message + httpUri.ToString());
                    SeekerApplication.ShowToast(string.Format("No application found to handle url \"{0}\".  Please install or enable web browser.", httpUri.ToString()), ToastLength.Long);
                }
            }
        }

        public const string NoDocumentOpenTreeToHandle = "No Activity found to handle Intent";

        public static bool IsUploadCompleteOrAborted(TransferStates state)
        {
            return (state.HasFlag(TransferStates.Succeeded) || state.HasFlag(TransferStates.Cancelled) || state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.TimedOut) || state.HasFlag(TransferStates.Completed) || state.HasFlag(TransferStates.Rejected));
        }

        public static string GetLastPathSegmentWithSpecialCaseProtection(DocumentFile dir, out bool msdCase)
        {
            msdCase = false;
            if (dir.Uri.LastPathSegment == "downloads")
            {
                var dfs = dir.ListFiles();
                if (dfs.Length > 0)
                {
                    //if last path segment is downloads then its likely that this is the "com.android.providers.downloads.documents" authority rather than the "com.android.externalstorage.documents" authority
                    //on android 10 (reproducible on emulator), the providers.downloads.documents authority does not give any kind of paths.  The last encoded path will always be msd:uniquenumber and so is useless
                    //as far as a presentable name is concerned.

                    string lastPathSegmentChild = dfs[0].Uri.LastPathSegment.Replace('/', '\\');
                    //last path segment child will be "raw:/storage/emulated/0/Download/Soulseek Incomplete" for the reasonable case and "msd:24" for the bad case
                    if (lastPathSegmentChild.Contains("\\"))
                    {
                        if(lastPathSegmentChild.StartsWith("raw:")) //scheme says "content" even though it starts with "raw:"
                        {
                            MainActivity.LogInfoFirebase("soft msdcase (raw:) : " + lastPathSegmentChild); //should be raw: provider
                            msdCase = true;
                            return String.Empty;
                        }
                        else
                        {
                            return Helpers.GetAllButLast(lastPathSegmentChild);
                        }
                    }
                    else
                    {
                        MainActivity.LogInfoFirebase("msdcase: " + lastPathSegmentChild); //should be msd:int
                        msdCase = true;
                        return String.Empty;
                    }
                }
                else
                {
                    MainActivity.LogInfoFirebase("downloads without any files");
                    return dir.Uri.LastPathSegment.Replace('/', '\\');
                }
            }
            else
            {
                return dir.Uri.LastPathSegment.Replace('/', '\\');
            }
        }

        private static string GetUnlockedFileName(SearchResponse item)
        {
            try
            {
                Soulseek.File f = item.Files.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }

        public static string SlskLinkClickedData = null;
        public static bool ShowSlskLinkContextMenu = false;

        /// <summary>
        /// returns false if unable to parse
        /// </summary>
        /// <param name="linkStringToParse"></param>
        /// <param name="username"></param>
        /// <param name="dirPath"></param>
        /// <param name="fullFilePath"></param>
        /// <param name="isFile"></param>
        /// <returns></returns>
        public static bool ParseSlskLinkString(string linkStringToParse, out string username, out string dirPath, out string fullFilePath, out bool isFile)
        {
            try
            {
                if (linkStringToParse.EndsWith('/'))
                {
                    isFile = false;
                }
                else
                {
                    isFile = true;
                }

                linkStringToParse = linkStringToParse.Substring(7);
                linkStringToParse = Android.Net.Uri.Decode(linkStringToParse);
                username = linkStringToParse.Substring(0, linkStringToParse.IndexOf('/'));
                fullFilePath = linkStringToParse.Substring(linkStringToParse.IndexOf('/') + 1).TrimEnd('/').Replace('/', '\\');
                if (isFile)
                {
                    dirPath = Helpers.GetDirectoryRequestFolderName(fullFilePath);
                }
                else
                {
                    dirPath = fullFilePath;
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("failure to parse: " + linkStringToParse);
                username = dirPath = fullFilePath = null;
                isFile = false;
                return false;
            }
            return true;
        }

        public static string CreateSlskLink(bool isDirectory, string fullFileOrFolderName, string username)
        {
            string link = username + "/" + fullFileOrFolderName.Replace("\\", "/");
            if (isDirectory)
            {
                link = link + "/";
            }
            return "slsk://" + Android.Net.Uri.Encode(link, "/");
        }

        private static string GetLockedFileName(SearchResponse item)
        {
            try
            {
                Soulseek.File f = item.LockedFiles.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// This will prepend the lock when applicable..
        /// </summary>
        /// <returns></returns>
        public static string GetFolderNameForSearchResult(SearchResponse item)
        {
            if (item.FileCount > 0)
            {
                return Helpers.GetFolderNameFromFile(GetUnlockedFileName(item));
            }
            else if (item.LockedFileCount > 0)
            {
                return new System.String(Java.Lang.Character.ToChars(0x1F512)) + Helpers.GetFolderNameFromFile(GetLockedFileName(item));
            }
            else
            {
                return "\\Locked\\";
            }
        }

        public static string GetFolderNameFromFile(string filename, int levels = 1)
        {
            try
            {
                int folderCount = 0;
                int index = -1; //-1 is important.  i.e. in the case of Folder\test.mp3, it can be Folder.
                int firstIndex = int.MaxValue;
                for (int i = filename.Length - 1; i >= 0; i--)
                {
                    if (filename[i] == '\\')
                    {
                        folderCount++;
                        if (firstIndex == int.MaxValue)
                        {
                            //strip off the file name
                            firstIndex = i;
                        }
                        if (folderCount == (levels + 1))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                return filename.Substring(index + 1, firstIndex - index - 1);
            }
            catch
            {
                return "";
            }
        }

        public static string GetParentFolderNameFromFile(string filename)
        {
            try
            {
                string parent = filename.Substring(0, filename.LastIndexOf('\\'));
                parent = parent.Substring(0, parent.LastIndexOf('\\'));
                parent = parent.Substring(parent.LastIndexOf('\\') + 1);
                return parent;
            }
            catch
            {
                return "";
            }
        }

        public static string GetTransferSpeedString(double bytesPerSecond)
        {
            if (bytesPerSecond > 1048576) //more than 1MB
            {
                return string.Format("{0:F1}mbs", bytesPerSecond / 1048576.0);
            }
            else
            {
                return string.Format("{0:F1}kbs", bytesPerSecond / 1024.0);
            }
        }

        public static string GetDateTimeSinceAbbrev(DateTime dtThen)
        {
            var dtNow = Helpers.GetDateTimeNowSafe(); //2.5 microseconds
            if (dtNow.Day == dtThen.Day)
            {
                //if on same day then show time. 24 hour time? maybe option to change?
                //ex. 2:45, 20:34
                //hh:mm
                return dtThen.ToString("H:mm");
            }
            else if (dtNow.Year == dtThen.Year)
            {
                //if different day but same year show month day
                //ex. Jan 4
                return dtThen.ToString("MMM d"); // d = 7 or 17.
            }
            else
            {
                //if different year show full.
                //ex. Dec 30 2021
                return dtThen.ToString("MMM d yyyy");
            }
        }

        public static string GetSubHeaderText(SearchResponse searchResponse)
        {
            int numFiles = 0;
            long totalBytes = -1;
            if (SoulSeekState.HideLockedResultsInSearch)
            {
                numFiles = searchResponse.FileCount;
                totalBytes = searchResponse.Files.Sum(f => f.Size);
            }
            else
            {
                numFiles = searchResponse.FileCount + searchResponse.LockedFileCount;
                totalBytes = searchResponse.Files.Sum(f => f.Size) + searchResponse.LockedFiles.Sum(f => f.Size);
            }

            //if total bytes greater than 1GB 
            string sizeString = GetHumanReadableSize(totalBytes);

            var filesWithLength = searchResponse.Files.Where(f => f.Length.HasValue);
            if (!SoulSeekState.HideLockedResultsInSearch)
            {
                filesWithLength = filesWithLength.Concat(searchResponse.LockedFiles.Where(f => f.Length.HasValue));
            }
            string timeString = null;
            if (filesWithLength.Count() > 0)
            {
                //translate length into human readable
                timeString = GetHumanReadableTime(filesWithLength.Sum(f => f.Length.Value));
            }
            if (timeString == null)
            {
                return string.Format("{0} files • {1}", numFiles, sizeString);
            }
            else
            {
                return string.Format("{0} files • {1} • {2}", numFiles, sizeString, timeString);
            }


        }

        public static string GetSizeLengthAttrString(Soulseek.File f)
        {

            string sizeString = string.Format("{0:0.##} mb", f.Size / (1024.0 * 1024.0));
            string lengthString = f.Length.HasValue ? GetHumanReadableTime(f.Length.Value) : null;
            string attrString = GetHumanReadableAttributesForSingleItem(f);
            if (attrString == null && lengthString == null)
            {
                return sizeString;
            }
            else if (attrString == null)
            {
                return String.Format("{0} • {1}", sizeString, lengthString);
            }
            else if (lengthString == null)
            {
                return String.Format("{0} • {1}", sizeString, attrString);
            }
            else
            {
                return String.Format("{0} • {1} • {2}", sizeString, lengthString, attrString);
            }
        }


        public static string GetHumanReadableAttributesForSingleItem(Soulseek.File f)
        {

            int bitRate = -1;
            int bitDepth = -1;
            double sampleRate = double.NaN;
            foreach (var attr in f.Attributes)
            {
                switch (attr.Type)
                {
                    case FileAttributeType.BitRate:
                        bitRate = attr.Value;
                        break;
                    case FileAttributeType.BitDepth:
                        bitDepth = attr.Value;
                        break;
                    case FileAttributeType.SampleRate:
                        sampleRate = attr.Value / 1000.0;
                        break;
                }
            }
            if (bitRate == -1 && bitDepth == -1 && double.IsNaN(sampleRate))
            {
                return null; //nothing to add
            }
            else if (bitDepth != -1 && !double.IsNaN(sampleRate))
            {
                return bitDepth + ", " + sampleRate + SlskHelp.CommonHelpers.STRINGS_KHZ;
            }
            else if (!double.IsNaN(sampleRate))
            {
                return sampleRate + SlskHelp.CommonHelpers.STRINGS_KHZ;
            }
            else if (bitRate != -1)
            {
                return bitRate + SlskHelp.CommonHelpers.STRINGS_KBS;
            }
            else
            {
                return null;
            }
        }

        public static string GetHumanReadableSize(long totalBytes)
        {
            if (totalBytes > 1024 * 1024 * 1024)
            {
                return string.Format("{0:0.##} gb", totalBytes / (1024.0 * 1024.0 * 1024.0));
            }
            else
            {
                return string.Format("{0:0.##} mb", totalBytes / (1024.0 * 1024.0));
            }
        }


        public static string GetHumanReadableTime(int totalSeconds)
        {
            int sec = totalSeconds % 60;
            int minutes = (totalSeconds % 3600) / 60;
            int hours = (totalSeconds / 3600);
            if (minutes == 0 && hours == 0 && sec == 0)
            {
                return null;
            }
            else if (minutes == 0 && hours == 0)
            {
                return string.Format("{0}s", sec);
            }
            else if (hours == 0)
            {
                return string.Format("{0}m{1}s", minutes, sec);
            }
            else
            {
                return string.Format("{0}h{1}m{2}s", hours, minutes, sec);
            }
        }




        /// <summary>
        /// Get all BUT the filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetDirectoryRequestFolderName(string filename)
        {
            try
            {
                int end = filename.LastIndexOf("\\");
                string clipped = filename.Substring(0, end);
                return clipped;
            }
            catch
            {
                return "";
            }
        }

        public static void CreateNotificationChannel(Context c, string id, string name, Android.App.NotificationImportance importance = Android.App.NotificationImportance.Low)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                NotificationChannel serviceChannel = new NotificationChannel(
                        id,
                        name,
                        importance
                );
                NotificationManager manager = c.GetSystemService(Context.NotificationService) as NotificationManager;
                manager.CreateNotificationChannel(serviceChannel);
            }
        }

        public static void SetToolTipText(View v, string tip)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
            {
                v.TooltipText = tip; //api26+ otherwise crash...
            }
            else
            {
                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(v, tip);
            }
        }


        public static Notification CreateNotification(Context context, PendingIntent pendingIntent, string channelID, string titleText, string contentText, bool setOnlyAlertOnce = true, bool forForegroundService = false, bool shutdownAction = false)
        {
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            

            //here we use the non compat notif builder as we want the special SetForegroundServiceBehavior method to prevent the new 10 second foreground notification delay.
            Notification notification = null;
            if ((int)Android.OS.Build.VERSION.SdkInt >= 31 && forForegroundService)
            {
                var builder = new Notification.Builder(context, channelID)
                          .SetContentTitle(titleText)
                          .SetContentText(contentText)
                          .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                          .SetContentIntent(pendingIntent)
                          .SetOnlyAlertOnce(setOnlyAlertOnce) //maybe
                          .SetForegroundServiceBehavior((int)(Android.App.NotificationForegroundService.Immediate)) //new for api 31+
                          .SetTicker(titleText);
                if(shutdownAction)
                {
                    Intent intent3 = new Intent(context, typeof(CloseActivity));
                    intent3.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    var pi = PendingIntent.GetActivity(context, 7618, intent3, PendingIntentFlags.Immutable);

                    Notification.Action replyAction = new Notification.Action.Builder(Resource.Drawable.ic_cancel_black_24dp, "Shutdown", pi).Build();
                    builder.AddAction(replyAction);
                }
                notification = builder.Build();
            }
            else
            {
                var builder = new NotificationCompat.Builder(context, channelID)
                          .SetContentTitle(titleText)
                          .SetContentText(contentText)
                          .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                          .SetContentIntent(pendingIntent)
                          .SetOnlyAlertOnce(setOnlyAlertOnce) //maybe
                          .SetTicker(titleText);
                //for < 21 it is possible (must use png icon instead of xml) but the icon does look great 
                //  and it doesnt clear from recents..
                if (shutdownAction && (int)Android.OS.Build.VERSION.SdkInt >= 21) 
                {
                    Intent intent3 = new Intent(context, typeof(CloseActivity));
                    intent3.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    var pi = PendingIntent.GetActivity(context, 7618, intent3, 0);
                    NotificationCompat.Action replyAction = new NotificationCompat.Action.Builder(Resource.Drawable.ic_cancel_black_24dp, "Shutdown", pi).Build();
                    builder.AddAction(replyAction);
                }
                notification = builder.Build();
            }
            return notification;

        }

        /// <summary>
        /// Since this is always called by the UI it handles showing toasts etc.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="numDays"></param>
        /// <returns>false if operation could not be attempted, true if successfully met prereqs and was attempted</returns>
        public static bool GivePrilegesAPI(string username, string numDays)
        {
            int numDaysInt = int.MinValue;
            if (!int.TryParse(numDays, out numDaysInt))
            {
                MainActivity.ToastUI(Resource.String.error_days_entered_no_parse);
                return false;
            }
            if (numDaysInt <= 0)
            {
                MainActivity.ToastUI(Resource.String.error_days_entered_not_positive);
                return false;
            }
            if (PrivilegesManager.Instance.GetRemainingDays() < numDaysInt)
            {
                MainActivity.ToastUI(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_insufficient_days), numDaysInt));
                return false;
            }
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_be_logged_in_to_give_privileges, ToastLength.Short).Show();
                return false;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, false, out t))
                {
                    return false; //if we get here we already did a toast message.
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
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { GivePrivilegesLogic(username, numDaysInt); });
                }));
                return true;
            }
            else
            {
                GivePrivilegesLogic(username, numDaysInt);
                return true;
            }
        }


        private static void GivePrivilegesLogic(string username, int numDaysInt)
        {
            SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.GetString(Resource.String.sending__), ToastLength.Short);
            SoulSeekState.SoulseekClient.GrantUserPrivilegesAsync(username, numDaysInt).ContinueWith(new Action<Task>
                ((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception.InnerException is TimeoutException)
                        {
                            SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_give_priv) + ": " + SeekerApplication.GetString(Resource.String.timeout), ToastLength.Long);
                        }
                        else
                        {
                            MainActivity.LogFirebase(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_give_priv) + t.Exception.InnerException.Message);
                            SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_give_priv), ToastLength.Long);
                        }
                        return;
                    }
                    else
                    {
                        //now there is a chance the user does not exist or something happens.  in which case our days will be incorrect...
                        PrivilegesManager.Instance.SubtractDays(numDaysInt);

                        SeekerApplication.ShowToast(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.give_priv_success), numDaysInt, username), ToastLength.Long);

                        //it could be a good idea to then GET privileges to see if it actually went through... but I think this is good enough...
                        //in the rare case that it fails they do get a message so they can figure it out
                    }
                }));
        }

        public static void ShowEditAddNoteDialog(string username, Action uiUpdateAction = null)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(SoulSeekState.ActiveActivityRef, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.note_title), username));
            View viewInflated = LayoutInflater.From(SoulSeekState.ActiveActivityRef).Inflate(Resource.Layout.user_note_dialog, (ViewGroup)SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.editUserNote);

            string existingNote = null;
            SoulSeekState.UserNotes.TryGetValue(username, out existingNote);
            if (existingNote != null)
            {
                input.Text = existingNote;
            }


            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //in my testing the only "bad" input we can get is "0" or a number greater than what you have.  
                //you cannot input '.' or negative even with physical keyboard, etc.
                string newText = input.Text;
                bool isEmpty = string.IsNullOrEmpty(newText);
                bool wasEmpty = string.IsNullOrEmpty(existingNote);
                bool addedOrRemoved = isEmpty != wasEmpty;
                if (addedOrRemoved)
                {
                    //either we cleared an existing note or added a new note
                    if (!wasEmpty && isEmpty)
                    {
                        //we removed the note
                        SoulSeekState.UserNotes.TryRemove(username, out _);
                        SaveUserNotes();

                    }
                    else
                    {
                        //we added a note
                        SoulSeekState.UserNotes[username] = newText;
                        SaveUserNotes();
                    }
                    if (uiUpdateAction != null)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateAction);
                    }

                }
                else if (isEmpty && wasEmpty)
                {
                    //nothing was there and nothing is there now
                    return;
                }
                else //something was there and is there now
                {
                    if (newText == existingNote)
                    {
                        return;
                    }
                    else
                    {
                        //update note and save prefs..
                        SoulSeekState.UserNotes[username] = newText;
                        SaveUserNotes();
                    }
                }

            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            });

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.close, eventHandlerCancel);
            // Set up the buttons

            builder.Show();
        }

        public static void SaveUserNotes()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_UserNotes, SeekerApplication.SaveUserNotesToString(SoulSeekState.UserNotes));
                editor.Commit();
            }
        }


        public static void SaveOnlineAlerts()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_UserOnlineAlerts, SeekerApplication.SaveUserOnlineAlertsFromString(SoulSeekState.UserOnlineAlerts));
                editor.Commit();
            }
        }


        public static void ShowGivePrilegesDialog(string username)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(SoulSeekState.ActiveActivityRef, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.give_to_), username));
            View viewInflated = LayoutInflater.From(SoulSeekState.ActiveActivityRef).Inflate(Resource.Layout.give_privileges_layout, (ViewGroup)SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.givePrivilegesEditText);

            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //in my testing the only "bad" input we can get is "0" or a number greater than what you have.  
                //you cannot input '.' or negative even with physical keyboard, etc.
                GivePrilegesAPI(username, input.Text);
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Send || //in this case it is Send (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search)
                {
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(SoulSeekState.ActiveActivityRef.Window.DecorView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Browse Logic...
                    eventHandler(sender, null);
                }
            };

            input.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.send, eventHandler);
            builder.SetNegativeButton(Resource.String.close, eventHandlerCancel);
            // Set up the buttons

            builder.Show();
        }
    }

    public class MagnetLinkClickableSpan : Android.Text.Style.ClickableSpan
    {
        private string textClicked;
        public MagnetLinkClickableSpan(string _textClicked)
        {
            textClicked = _textClicked;
        }
        public override void OnClick(View widget)
        {
            MainActivity.LogDebug("magnet link click");
            try
            {
                Intent followLink = new Intent(Intent.ActionView);
                followLink.SetData(Android.Net.Uri.Parse(textClicked));
                SoulSeekState.ActiveActivityRef.StartActivity(followLink);
            }
            catch (Android.Content.ActivityNotFoundException e)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, "No Activity Found to handle Magnet Links.  Please Install a BitTorrent Client.", ToastLength.Long).Show();
            }
        }
    }

    public class SlskLinkClickableSpan : Android.Text.Style.ClickableSpan
    {
        private string textClicked;
        public SlskLinkClickableSpan(string _textClicked)
        {
            textClicked = _textClicked;
        }
        public override void OnClick(View widget)
        {
            MainActivity.LogDebug("slsk link click");
            Helpers.SlskLinkClickedData = textClicked;
            Helpers.ShowSlskLinkContextMenu = true;
            SoulSeekState.ActiveActivityRef.RegisterForContextMenu(widget);
            SoulSeekState.ActiveActivityRef.OpenContextMenu(widget);
            SoulSeekState.ActiveActivityRef.UnregisterForContextMenu(widget);
        }
    }

    public static class MicroTagReader
    {
        static List<List<int>> samplerates;
        static List<int> channels_per_mode;
        static List<List<List<int>>> bitrate_by_version_by_layer;
        static MicroTagReader()
        {
            samplerates = new List<List<int>>();
            var level1 = new List<int>() { 11025, 12000, 8000 };
            samplerates.Add(level1);
            var level2 = new List<int>();
            samplerates.Add(level2);
            var level3 = new List<int>() { 22050, 24000, 16000 };
            samplerates.Add(level3);
            var level4 = new List<int>() { 44100, 48000, 32000 };
            samplerates.Add(level4);

            bitrate_by_version_by_layer = new List<List<List<int>>>();

            List<int> v1l1 = new List<int>() { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 };
            List<int> v1l2 = new List<int>() { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0 };
            List<int> v1l3 = new List<int>() { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
            List<int> v2l1 = new List<int>() { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 };
            List<int> v2l2 = new List<int>() { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };
            List<int> v2l3 = v2l2;

            List<List<int>> v2_5 = new List<List<int>>() { null, v2l3, v2l2, v2l1 };
            //List<List<int>> v2_5 = new List<List<int>>() { null, v2l3, v2l2, v2l1 }
            List<List<int>> v2 = new List<List<int>>() { null, v2l3, v2l2, v2l1 };
            List<List<int>> v1 = new List<List<int>>() { null, v1l3, v1l2, v1l1 };
            bitrate_by_version_by_layer.Add(v2_5);
            bitrate_by_version_by_layer.Add(null);
            bitrate_by_version_by_layer.Add(v2);
            bitrate_by_version_by_layer.Add(v1);
            //samples_per_frame = 1152  # the default frame size for mp3

            channels_per_mode = new List<int>() { 2, 2, 2, 1 };
        }

        /// <summary>
        /// used since android messes up very badly when it comes to vbr mp3s
        /// </summary>
        /// <param name="contentResolver"></param>
        /// <param name="uri"></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitDepth"></param>
        public static void GetMp3Metadata(ContentResolver contentResolver, Android.Net.Uri uri, int true_duration, long true_size, out int bitrate)
        {
            bitrate = -1;
            System.IO.Stream fileStream = null;
            try
            {
                //int max_estimation_frames = 30 * 44100 / 1152;

                double bitrate_accumulator = 0;
                int frame_size_accu = 0;
                List<double> last_bitrates = new List<double>();
                //int audio_offset = -1;
                fileStream = contentResolver.OpenInputStream(uri);
                byte[] header = new byte[4];
                fileStream.Read(header, 0, 4);
                bool startsWithID3 = header.Take(3).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("ID3"));
                //{
                //its technically incorrect, but flac files can have ID3 tags.
                //I found the sample file to test in tinytag repo.  otherwise I think this is rare.
                byte[] id3Header = new byte[10];
                if ((fileStream.Read(id3Header, 0, 10) == 10))
                {
                    if (startsWithID3)
                    {
                        int size = id3Header[2] * 128 * 128 * 128 + id3Header[3] * 128 * 128 + id3Header[4] * 128 + id3Header[5];
                        fileStream.Seek(size, System.IO.SeekOrigin.Begin);
                    }
                    else
                    {
                        fileStream.Seek(0, System.IO.SeekOrigin.Begin);
                    }
                }
                int frames = 0;
                while (true)
                {
                    byte[] nextFour = new byte[4];
                    int read = fileStream.Read(nextFour, 0, 4);
                    if (read < 4)
                    {
                        return;
                    }
                    int br_id = (byte)(((nextFour[2] >> 4))) & 0x0F;
                    int sr_id = (byte)((nextFour[2] / 4)) & 0x03;
                    int padding = (nextFour[2] & 0x02) > 0 ? 1 : 0;
                    int mpeg_id = (byte)((nextFour[1] / 8)) & 0x03;
                    int layer_id = (byte)((nextFour[1] / 2)) & 0x03;
                    int channel_mode = (byte)((nextFour[3] / 64)) & 0x03;
                    int val = nextFour[0] * 256 + nextFour[1];
                    if (val <= (65504) || br_id > 14 || br_id == 0 || sr_id == 3 || layer_id == 0 || mpeg_id == 1)
                    {
                        int index = Array.IndexOf(nextFour, (byte)(0xFF));
                        if (index == -1)
                        {
                            index = nextFour.Length;
                        }
                        if (index == 0)
                        {
                            index = 1;
                        }
                        int amountToMove = index - 4;
                        fileStream.Seek(amountToMove, System.IO.SeekOrigin.Current); //we go backwards if need be.
                        continue;
                    }

                    int frame_bitrate = bitrate_by_version_by_layer[mpeg_id][layer_id][br_id];
                    int samplerate = samplerates[mpeg_id][sr_id];
                    int channels = channels_per_mode[channel_mode];


                    if (frames == 0)
                    {
                        byte[] lookForXing = new byte[1024];
                        fileStream.Read(lookForXing, 0, 1024);
                        fileStream.Seek(-1028, System.IO.SeekOrigin.Current);
                        byte[] toLookForXing = nextFour.Concat(lookForXing).ToArray();
                        int index = -1;
                        for (int i = 0; i < toLookForXing.Length - 4; i++)
                        {
                            if (toLookForXing[i] == (byte)(88) &&
                                toLookForXing[i + 1] == (byte)(105) &&
                                toLookForXing[i + 2] == (byte)(110) &&
                                toLookForXing[i + 3] == (byte)(103))
                            {
                                index = i;
                                break;
                            }
                        }
                        if (index != -1)
                        {
                            fileStream.Seek(index + 4, System.IO.SeekOrigin.Current);


                            fileStream.Read(nextFour, 0, 4);
                            var id3header = nextFour.ToArray();
                            int id3frames = -1;
                            int byte_count = -1;
                            if ((id3header[3] & 0x01) != 0)
                            {
                                fileStream.Read(nextFour, 0, 4);
                                id3frames = nextFour[0] * 256 * 256 * 256 + nextFour[1] * 256 * 256 + nextFour[2] * 256 + nextFour[3];
                            }
                            if ((id3header[3] & 0x02) != 0)
                            {
                                fileStream.Read(nextFour, 0, 4);
                                byte_count = nextFour[0] * 256 * 256 * 256 + nextFour[1] * 256 * 256 + nextFour[2] * 256 + nextFour[3];
                            }
                            if ((id3header[3] & 0x04) != 0)
                            {
                                byte[] next400 = new byte[400];
                                fileStream.Read(next400, 0, 400);
                            }
                            if ((id3header[3] & 0x08) != 0)
                            {
                                fileStream.Read(nextFour, 0, 4);
                            }
                            if (id3frames != -1 && byte_count != -1 && id3frames != 0)
                            {
                                double duration = id3frames * 1152 / (double)(samplerate);
                                bitrate = (int)(byte_count * 8 / duration / 1000) * 1000;
                            }
                            return;
                        }
                        else
                        {
                            fileStream.Seek(4, System.IO.SeekOrigin.Current);
                        }
                    }




                    frames += 1;
                    bitrate_accumulator += frame_bitrate;
                    if (frames <= 5)
                    {
                        last_bitrates.Add(frame_bitrate);
                    }


                    //if(frames==1)
                    //{
                    //    audio_offset = fileStream.Position;
                    //}

                    //fileStream.Seek(4, System.IO.SeekOrigin.Current) 

                    int frame_length = (144000 * frame_bitrate) / samplerate + padding;
                    frame_size_accu += frame_length;
                    //if bitrate does not change over time its probably CBR
                    bool is_cbr = (frames == 5 && last_bitrates.Distinct().Count() == 1);
                    if (is_cbr)
                    {
                        //int audio_stream_size = fileStream.Position - audio_offset;
                        //int est_frame_count = audio_stream_size / (frame_size_accu / float(frames))
                        //int samples = est_frame_count * 1152;
                        //double duration = samples / (double)(samplerate);
                        bitrate = (int)(bitrate_accumulator / frames) * 1000; //works especially great for cbr
                        return;
                    }

                    if (frames > 5)
                    {
                        //dont use this estimation method for vbr... its no more accurate than size / duration... and takes way longer.
                        bitrate = (true_duration != -1) ? (int)((true_size * 8 * 1000.0 / 1024.0) / true_duration) : -1;//todo test
                        return;
                    }

                    if (frame_length > 1)
                    {
                        fileStream.Seek(frame_length - header.Length, System.IO.SeekOrigin.Current);
                    }
                }
                //}
                //else
                //{
                //    return;
                //}
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("getMp3Metadata: " + e.Message + e.StackTrace);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }

        public static void GetAiffMetadata(ContentResolver contentResolver, Android.Net.Uri uri, out int sampleRate, out int bitDepth, out int durationSeconds)
        {
            sampleRate = -1;
            bitDepth = -1;
            durationSeconds = -1;
            System.IO.Stream fileStream = null;
            try
            {
                fileStream = contentResolver.OpenInputStream(uri); byte[] buffer = new byte[4];
                fileStream.Read(buffer, 0, 4); //FORM

                if (System.Text.Encoding.ASCII.GetString(buffer) != "FORM")
                {
                    throw new Exception("malformed FORM");
                }


                fileStream.Read(buffer, 0, 4); //filesize UINT.
                Array.Reverse(buffer, 0, buffer.Length); //big endian
                uint fileSize = System.BitConverter.ToUInt32(buffer);

                fileStream.Read(buffer, 0, 4); //AIFF
                if (System.Text.Encoding.ASCII.GetString(buffer) != "AIFF")
                {
                    throw new Exception("malformed AIFF");
                }


                long comm_chunk = long.MinValue;
                long cur_position = fileStream.Position;
                while (cur_position < fileSize)
                {
                    // Read 4-byte chunk name
                    fileStream.Read(buffer, 0, 4);

                    if (System.Text.Encoding.ASCII.GetString(buffer) == "COMM")
                    {
                        comm_chunk = fileStream.Position - 4;
                        break;
                    }

                    // chunk size
                    fileStream.Read(buffer, 0, 4);
                    Array.Reverse(buffer, 0, buffer.Length); //big endian
                    uint chunkSize = System.BitConverter.ToUInt32(buffer);
                    if (chunkSize % 2 != 0)
                    {
                        chunkSize++;
                    }
                    fileStream.Seek(chunkSize, System.IO.SeekOrigin.Current);
                    cur_position = fileStream.Position;

                    //if (chunkHeader == chunkName)
                    //{
                    //	// We found a matching chunk, return the position
                    //	// of the header start
                    //	return Tell - 4;
                    //}
                    //else
                    //{
                    //	// This chunk is not the one we are looking for
                    //	// Continue the search, seeking over the chunk
                    //	uint chunkSize = ReadBlock(4).ToUInt();
                    //	// Seek forward "chunkSize" bytes
                    //	Seek(chunkSize, System.IO.SeekOrigin.Current);
                    //}
                }

                if (comm_chunk == long.MinValue)
                {
                    throw new Exception("couldnt find comm block");
                }

                fileStream.Seek(comm_chunk, System.IO.SeekOrigin.Begin);
                byte[] comm_buffer = new byte[26];
                fileStream.Read(comm_buffer, 0, 26);

                uint totalFrames = System.BitConverter.ToUInt32(comm_buffer.Skip(10).Take(4).Reverse().ToArray());
                ushort bits_per_sample = System.BitConverter.ToUInt16(comm_buffer.Skip(14).Take(2).Reverse().ToArray());
                double sample_rate = ToExtendedPrecision(comm_buffer.Skip(16).Take(10).ToArray());
                int seconds = (int)(totalFrames / sample_rate);
                sampleRate = (int)sample_rate;
                bitDepth = (int)bits_per_sample;
                durationSeconds = seconds;
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("GetAiffMetadata: " + e.Message + e.StackTrace);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }

        /// <summary>
        /// From taglib-sharp:
        ///    Converts the first 10 bytes of the current instance to an IEEE
        ///    754 80-bit extended precision floating point number, expressed
        ///    as a <see cref="double"/>.
        /// </summary>
        /// <returns>
        ///    A <see cref="double"/> value containing the value read from the
        ///    current instance.
        /// </returns>
        private static double ToExtendedPrecision(byte[] bytesToConvert)
        {
            int exponent = ((bytesToConvert[0] & 0x7F) << 8) | bytesToConvert[1];
            ulong hiMantissa = ((ulong)bytesToConvert[2] << 24)
                               | ((ulong)bytesToConvert[3] << 16)
                               | ((ulong)bytesToConvert[4] << 8)
                               | bytesToConvert[5];
            ulong loMantissa = ((ulong)bytesToConvert[6] << 24)
                               | ((ulong)bytesToConvert[7] << 16)
                               | ((ulong)bytesToConvert[8] << 8)
                               | bytesToConvert[9];

            double f;
            if (exponent == 0 && hiMantissa == 0 && loMantissa == 0)
            {
                f = 0;
            }
            else
            {
                if (exponent == 0x7FFF)
                {
                    f = double.PositiveInfinity;
                }
                else
                {
                    exponent -= 16383;
                    f = hiMantissa * Math.Pow(2, exponent -= 31);
                    f += loMantissa * Math.Pow(2, exponent -= 32);
                }
            }

            return (bytesToConvert[0] & 0x80) != 0 ? -f : f;
        }

        public static void GetApeMetadata(ContentResolver contentResolver, Android.Net.Uri uri, out int sampleRate, out int bitDepth, out int durationSeconds)
        {
            sampleRate = -1;
            bitDepth = -1;
            durationSeconds = -1;
            System.IO.Stream fileStream = null;
            try
            {
                fileStream = contentResolver.OpenInputStream(uri);
                byte[] a = new byte[76];
                fileStream.Read(a, 0, 76); //ape header
                if((System.Text.Encoding.ASCII.GetString(a.Take(4).ToArray())!="MAC "))
                {
                    throw new Exception("MAC  not present");
                }

                byte[] b = a.Skip(68).Take(2).ToArray();
                bitDepth = (int)System.BitConverter.ToUInt16(b);
                byte[] c = a.Skip(72).Take(4).ToArray();
                sampleRate = (int)System.BitConverter.ToUInt32(c);
                byte[] d = a.Skip(64).Take(4).ToArray();
                uint total_frames = System.BitConverter.ToUInt32(d);
                uint blocks_per_frame = System.BitConverter.ToUInt32(a.Skip(56).Take(4).ToArray());
                uint final_frame_blocks = System.BitConverter.ToUInt32(a.Skip(60).Take(4).ToArray());

                durationSeconds = (int)(((total_frames - 1) *
                     blocks_per_frame + final_frame_blocks) /
                    (double)sampleRate);
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("GetApeMetadata: " + e.Message + e.StackTrace);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }

        public static void GetFlacMetadata(ContentResolver contentResolver, Android.Net.Uri uri, out int sampleRate, out int bitDepth)
        {
            sampleRate = -1;
            bitDepth = -1;
            System.IO.Stream fileStream = null;
            try
            {
                fileStream = contentResolver.OpenInputStream(uri);
                byte[] header = new byte[4];
                fileStream.Read(header, 0, 4);
                if (header.Take(3).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("ID3")))
                {
                    //its technically incorrect, but flac files can have ID3 tags.
                    //I found the sample file to test in tinytag repo.  otherwise I think this is rare.
                    //just skip over this
                    byte[] id3Header = new byte[10];
                    if ((fileStream.Read(id3Header, 0, 10) == 10))
                    {
                        int size = id3Header[2] * 128 * 128 * 128 + id3Header[3] * 128 * 128 + id3Header[4] * 128 + id3Header[5];
                        fileStream.Seek(size - 4, System.IO.SeekOrigin.Current);
                        fileStream.Read(header, 0, 4);
                    }
                    else
                    {
                        return;
                    }
                }
                if (!(header.SequenceEqual(System.Text.Encoding.ASCII.GetBytes("fLaC"))))
                {
                    throw new Exception("bad format");
                }
                //position is now after the fLaC

                while (fileStream.Read(header, 0, 4) == 4)
                {
                    int blockType = header[0] & (byte)(0x7f);
                    int isLastBlock = header[0] & (byte)(0x80);
                    int size = header[1] * 256 * 256 + header[2] * 256 + header[3];
                    if (blockType == 0)
                    {
                        byte[] stream_info_header = new byte[size];
                        if (fileStream.Read(stream_info_header, 0, size) != size)
                        {
                            return;
                        }
                        int offset_to_sample_rate = 10;
                        sampleRate = (stream_info_header[offset_to_sample_rate] * 256 * 256 + stream_info_header[offset_to_sample_rate + 1] * 256 + stream_info_header[offset_to_sample_rate + 2]) / 16;

                        bitDepth = ((stream_info_header[offset_to_sample_rate + 2] & (byte)(0x1)) * 16 + (stream_info_header[offset_to_sample_rate + 3] & (byte)(0xf0)) / 16) + 1;
                        return;
                    }
                    else if (isLastBlock != 0) //it will be 128
                    {
                        return;
                    }
                    else
                    {
                        //go to next block
                        fileStream.Seek(size, System.IO.SeekOrigin.Current);
                    }
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("getFlacMetadata: " + e.Message + e.StackTrace); //TODO: getFlacMetadata: FileDescriptor must not be null a
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }
    }


    public class MaterialProgressBarPassThrough : LinearLayout
    {
        private bool disposed = false;
        private bool init = false;
        public MaterialProgressBarPassThrough(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            //var c = new ContextThemeWrapper(context, Resource.Style.MaterialThemeForChip);
            //LayoutInflater.From(c).Inflate(Resource.Layout.material_progress_bar_pass_through, this, true);
        }
        public MaterialProgressBarPassThrough(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            //if(init)
            //{
            //    return;
            //}
            //init = true;
            MainActivity.LogDebug("MaterialProgressBarPassThrough disposed" + disposed);
            var c = new ContextThemeWrapper(context, Resource.Style.MaterialThemeForChip);
            LayoutInflater.From(c).Inflate(Resource.Layout.material_progress_bar_pass_through, this, true);
        }

        public static MaterialProgressBarPassThrough inflate(ViewGroup parent)
        {
            var c = new ContextThemeWrapper(parent.Context, Resource.Style.MaterialThemeForChip);
            MaterialProgressBarPassThrough itemView = (MaterialProgressBarPassThrough)LayoutInflater.From(c).Inflate(Resource.Layout.material_progress_bar_pass_through_dummy, parent, false);

            return itemView;
        }

        public MaterialProgressBarPassThrough(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
        public MaterialProgressBarPassThrough(Context context) : this(context, null)
        {
        }

        protected override void Dispose(bool disposing)
        {
            disposed = true;
            base.Dispose(disposing);
        }
    }

    public enum SharingIcons
    {
        Off = 0,
        Error = 1,
        On = 2,
        CurrentlyParsing = 3,
        OffDueToNetwork = 4,
    }



}


namespace SearchResponseExtensions
{
    public static class SearchResponseExtensions
    {
        public static IEnumerable<Soulseek.File> GetFiles(this SearchResponse searchResponse, bool hideLocked)
        {
            return hideLocked ? searchResponse.Files : searchResponse.Files.Concat(searchResponse.LockedFiles);
        }

        public static bool IsLockedOnly(this SearchResponse searchResponse)
        {
            return searchResponse.FileCount == 0 && searchResponse.LockedFileCount != 0;
        }

        public static Soulseek.File GetElementAtAdapterPosition(this SearchResponse searchResponse, bool hideLocked, int position)
        {
            if (hideLocked)
            {
                return searchResponse.Files.ElementAt(position);
            }
            else
            {
                //we always do Files, then LockedFiles
                if (position >= searchResponse.Files.Count)
                {
                    return searchResponse.LockedFiles.ElementAt(position - searchResponse.Files.Count);
                }
                else
                {
                    return searchResponse.Files.ElementAt(position);
                }

            }

        }
    }
}



#if DEBUG
public static class TestClient
{
    public static async Task<IReadOnlyCollection<SearchResponse>> SearchAsync(string searchString, Action<SearchResponseReceivedEventArgs> actionToInvoke, CancellationToken ct)
    {

        await Task.Delay(2).ConfigureAwait(false);
        var responseBag = new System.Collections.Concurrent.ConcurrentBag<SearchResponse>();
        Random r = new Random();
        int x = r.Next(0, 3);
        int maxSleep = 100;
        switch (x)
        {
            case 0:
                maxSleep = 1; //v fast case
                break;
            case 1:
                maxSleep = 10; //fast case
                break;
            case 2:
                maxSleep = 200; //trickling in case
                break;
        }
        AndriodApp1.MainActivity.LogDebug("max sleep: " + maxSleep);
        for (int i = 0; i < 1000; i++)
        {
            List<Soulseek.File> fs = new List<Soulseek.File>();
            for (int j = 0; j < 15; j++)
            {
                fs.Add(new Soulseek.File(1, searchString + i + "\\" + $"{j}. test filename " + i, 0, ".mp3", null));
            }
            //1 in 15 chance of being locked
            bool locked = false;
            if (r.Next(0, 15) == 0)
            {
                locked = true;
            }
            SearchResponse response = new SearchResponse("test" + i, r.Next(0, 100000), r.Next(0, 10), r.Next(0, 12345), (long)(r.Next(0, 14556)), locked ? null : fs, locked ? fs : null);
            var eventArgs = new SearchResponseReceivedEventArgs(response, null);
            responseBag.Add(response);
            actionToInvoke(eventArgs);
            ct.ThrowIfCancellationRequested();
            System.Threading.Thread.Sleep(r.Next(0, maxSleep));
        }
        return responseBag.ToList().AsReadOnly();
    }
}
#endif
