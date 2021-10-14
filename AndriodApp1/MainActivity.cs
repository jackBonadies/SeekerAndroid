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

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Drm;
using Android.OS;
using Android.Provider;
using Soulseek;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Provider;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
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

using AndroidX.Work;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using Android.Animation;
using System.Collections.ObjectModel;
using Android.Net.Wifi;
//using System.IO;
//readme:
//dotnet add package Soulseek --version 1.0.0-rc3.1
//xamarin
//Had to rewrite this one from .csproj
//<Import Project="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild
//\Xamarin\Android\Xamarin.Android.CSharp.targets" />
namespace AndriodApp1
{

    //public class DownloadWorkerTest : Worker
    //{
    //    public DownloadWorkerTest(Context context, WorkerParameters workerParameters) : base(context, workerParameters)
    //    {

    //    }
    //    //runs on background thread provided by workmanager.
    //    public override Result DoWork()
    //    {
    //        var taxReturn = CalculateTaxes();
    //        System.Threading.Thread.Sleep(1000*60*30);
    //        Android.Util.Log.Debug("CalculatorWorker", $"Your Tax Return is: {taxReturn}");
    //        return Result.InvokeSuccess();
    //    }

    //    public double CalculateTaxes()
    //    {
    //        return 2000;
    //    }
    //}

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
                        if(activity is AppCompatActivity)
                        {
                            bool foreground = (activity as AppCompatActivity).Lifecycle.CurrentState.IsAtLeast(Android.Arch.Lifecycle.Lifecycle.State.Resumed);
                            if(foreground)
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
            SoulSeekState.ActiveActivityRef = activity;
            DiagLastStarted = activity.GetType().Name.ToString();
            MainActivity.LogDebug("OnActivityStarted " + DiagLastStarted);
        }

        void Application.IActivityLifecycleCallbacks.OnActivityStopped(Activity activity)
        {
            DiagLastStopped = activity.GetType().Name.ToString();
            MainActivity.LogDebug("OnActivityStopped " + DiagLastStopped);
        }

        public volatile static string DiagLastStarted = string.Empty;
        public volatile static string DiagLastStopped = string.Empty;
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
            lock(PrivilegedUsersLock)
            {
                PrivilegedUsers = privUsers;
                if(SoulSeekState.Username!=null && SoulSeekState.Username != string.Empty)
                {
                    IsPrivileged = CheckIfPrivileged(SoulSeekState.Username);
                    if(IsPrivileged)
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
            if(SecondsRemainingAtLastCheck==0 || SecondsRemainingAtLastCheck == int.MinValue)
            {
                return 0;
            }
            else if(LastCheckTime==DateTime.MinValue)
            {
                //shouldnt go here
                return 0;
            }
            else
            {
                int secondsSinceLastCheck = (int)Math.Floor(LastCheckTime.Subtract(DateTime.UtcNow).TotalSeconds);
                int remainingSeconds = SecondsRemainingAtLastCheck - secondsSinceLastCheck;
                return Math.Max(remainingSeconds,0);
            }
        }

        /// <summary>
        /// Get Remaining Days (rounded down)
        /// </summary>
        /// <returns></returns>
        public int GetRemainingDays()
        {
            return GetRemainingSeconds()/(24*3600);
        }

        public string GetPrivilegeStatus()
        {
            if(SecondsRemainingAtLastCheck==0 || SecondsRemainingAtLastCheck==int.MinValue || GetRemainingSeconds()<=0)
            {
                if(IsPrivileged)
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
                if(seconds> 3600 * 24)
                {
                    int days = seconds / (3600 * 24);
                    if(days==1)
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.day_left),days);
                    }
                    else
                    {
                        return string.Format(SeekerApplication.GetString(Resource.String.days_left), days);
                    }
                }
                else if(seconds > 3600)
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
                ((Task<int> t)=>{
                    if(t.IsFaulted)
                    {
                        if(feedback)
                        {
                            if(t.Exception.InnerException is TimeoutException)
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
                        if(t.Result>0)
                        {
                            IsPrivileged = true;
                        }
                        else if(t.Result==0)
                        {
                            IsPrivileged = false;
                        }
                        LastCheckTime = DateTime.UtcNow;
                        if (feedback)
                        {
                            SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.priv_success) +  ". " + SeekerApplication.GetString(Resource.String.status) + ": " + GetPrivilegeStatus(),ToastLength.Long);
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
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                        {

                            Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();

                        });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(() => { GetPrivilegesLogic(feedback); });

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
                if(PrivilegedUsers!=null)
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
                LastSetTime = new DateTime( SoulSeekState.SharedPreferences.GetLong(SoulSeekState.M_LastSetUpnpRuleTicks, 0) );
                LastSetLifeTime = SoulSeekState.SharedPreferences.GetInt(SoulSeekState.M_LifetimeSeconds, -1);
                LastSetPort = SoulSeekState.SharedPreferences.GetInt(SoulSeekState.M_PortMapped, -1);
                LastSetLocalIP = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_LastSetLocalIP, string.Empty);
            }
        }

        public static UPnpManager Instance
        {
            get
            {
                if(instance==null)
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
            System.Timers.Timer finishSearchTimer = new System.Timers.Timer(timeout*1000);
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

        public Tuple<ListeningIcon,string> GetIconAndMessage()
        {
            if(!SoulSeekState.ListenerUPnpEnabled)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.upnp_off));
            }
            else if(!SoulSeekState.ListenerEnabled)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.listener_off));
            }
            else if(RunningStatus == UpnpRunningStatus.NeverStarted)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.upnp_not_ran));
            }
            else if(RunningStatus == UpnpRunningStatus.CurrentlyRunning)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.PendingIcon, Context.GetString(Resource.String.upnp_currently_running));
            }
            else if(RunningStatus == UpnpRunningStatus.Finished)
            {
                if(DiagStatus == UpnpDiagStatus.NoUpnpDevicesFound)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.no_upnp_devices_found));
                }
                else if(DiagStatus == UpnpDiagStatus.WifiDisabled)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.upnp_wifi_only));
                }
                else if(DiagStatus == UpnpDiagStatus.NoWifiConnection)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.upnp_no_wifi_conn));
                }
                else if(DiagStatus == UpnpDiagStatus.UpnpDeviceFoundButFailedToMap)
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
            else if(RunningStatus == UpnpRunningStatus.AlreadyMapped)
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
            if(DevicesSuccessfullyMapped > 0)
            {
                DiagStatus = UpnpDiagStatus.Success;
            }
            else if(DevicesSuccessfullyMapped==0 && DevicesFound > 0)
            {
                DiagStatus = UpnpDiagStatus.UpnpDeviceFoundButFailedToMap;
            }
            else if(DevicesSuccessfullyMapped == 0 && DevicesFound == 0)
            {
                DiagStatus = UpnpDiagStatus.NoUpnpDevicesFound;
            }
            if(Feedback)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                if(DiagStatus == UpnpDiagStatus.NoUpnpDevicesFound)
                {
                    Toast.MakeText(Context, Context.GetString(Resource.String.no_upnp_devices_found), ToastLength.Short).Show();
                }
                else if(DiagStatus == UpnpDiagStatus.UpnpDeviceFoundButFailedToMap)
                {
                    Toast.MakeText(Context, Context.GetString(Resource.String.failed_to_set), ToastLength.Short).Show();
                }
                });
            }
            Feedback = false;
            MainActivity.LogDebug("finished " + DiagStatus.ToString());

            SearchFinished?.Invoke(null, new EventArgs());
            if(DiagStatus == UpnpDiagStatus.Success)
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
                if(LastSetLifeTime != -1 && LastSetTime.AddSeconds(LastSetLifeTime / 2.0) > DateTime.UtcNow && LastSetPort == SoulSeekState.ListenerPort && IsLocalIPsame())
                {
                    MainActivity.LogDebug("Renew Mapping Later... we already have a good one..");
                    RunningStatus = UpnpRunningStatus.AlreadyMapped;
                    SearchFinished?.Invoke(null,new EventArgs());
                    Feedback = false;
                    RenewMapping(); 
                }
                else
                {
                    MainActivity.LogDebug("search and set mapping...");
                    SearchAndSetMapping();
                }
            }
            catch(Exception e)
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
                if(LastSetLifeTime!=-1 && LastSetPort != -1 && LastSetTime!=DateTime.MinValue)
                {
                    if(RenewMappingTimer==null)
                    {
                        RenewMappingTimer = new System.Timers.Timer();
                        RenewMappingTimer.AutoReset = false;//since this function will get called again anyway.
                        RenewMappingTimer.Elapsed += RenewMappingTimer_Elapsed;
                    }
                    RenewMappingTimer.Interval = Math.Max(LastSetLifeTime * 1000 / 2, 3600 * 1000 * 2); //at least two hours (for now).  divided by 2!
                    RenewMappingTimer.Start();
                }
            }
            catch(Exception e)
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
            catch(Exception ex)
            {
                MainActivity.LogFirebase("IsLocalIPsame exception " + ex.Message + ex.StackTrace);
                return false;
            }
        }

        public void SearchAndSetMapping()
        {
            try
            {
                if(!SoulSeekState.ListenerEnabled || !SoulSeekState.ListenerUPnpEnabled)
                {
                    DiagStatus = UpnpDiagStatus.UpnpDisabled;
                    RunningStatus = UpnpRunningStatus.Finished;
                    SearchFinished?.Invoke(null, new EventArgs());
                    return;
                }
                if(Context==null)
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
                if(wm.ConnectionInfo.SupplicantState == SupplicantState.Disconnected || wm.ConnectionInfo.IpAddress == 0)
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
            catch(Exception e)
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
                if(DevicesFound>1)
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
                    catch(Exception ex)
                    {
                        //the task can throw (in which case the task.Wait throws)
                        
                        if(ex.InnerException is Mono.Nat.MappingException && ex.InnerException.Message != null && ex.InnerException.Message.Contains("Error 725: OnlyPermanentLeasesSupported")) //happened on my tablet... connected to my other 192.168.1.1 router
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
                            MainActivity.LogFirebase("CreatePortMapAsync " + ex.Message + ex.StackTrace);
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
                        if(timeOutGetMapping)
                        {
                            MainActivity.LogFirebase("GetSpecificMappingAsync timeout");
                            return;
                        }
                    }
                    catch(Exception ex)
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
                    if(LastSetLifeTime==0)
                    {
                        LastSetLifeTime = 4 * 3600;
                    }
                    else if(LastSetLifeTime < 2*3600)
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
            catch(Exception ex)
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


    [Application]
    public class SeekerApplication : Application
    {
        public static Context ApplicationContext = null; 
        public SeekerApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }
        
        public const bool AUTO_CONNECT_ON = true;

        public override void OnCreate()
        {
            base.OnCreate();
            ApplicationContext = this;
            #if !IzzySoft
            Firebase.FirebaseApp app = Firebase.FirebaseApp.InitializeApp(this);
            if(app==null)
            {
                MainActivity.crashlyticsEnabled = false;
            }
            #endif
            //MainActivity.LogFirebase("testing release");

            this.RegisterActivityLifecycleCallbacks(new ForegroundLifecycleTracker());
            var sharedPrefs = this.GetSharedPreferences("SoulSeekPrefs", 0);
            SoulSeekState.SharedPreferences = sharedPrefs;
            RestoreSoulSeekState(sharedPrefs);
            RestoreListeningState();
            UPnpManager.RestoreUpnpState();
            //SoulSeekState.SharedPreferences = sharedPrefs;

            if (SeekerKeepAliveService.CpuKeepAlive == null)
            {
                SeekerKeepAliveService.CpuKeepAlive = ((PowerManager)this.GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Partial,"Seeker Keep Alive Service Cpu");
            }

            if (SeekerKeepAliveService.WifiKeepAlive == null)
            {
                SeekerKeepAliveService.WifiKeepAlive = ((Android.Net.Wifi.WifiManager)this.GetSystemService(Context.WifiService)).CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Seeker Keep Alive Service Wifi");
            }

            if (SoulSeekState.SoulseekClient == null)
            {
                //need search response and enqueue download action...
                //SoulSeekState.SoulseekClient = new SoulseekClient(new SoulseekClientOptions(messageTimeout: 30000, enableListener: false, autoAcknowledgePrivateMessages: false, acceptPrivateRoomInvitations:SoulSeekState.AllowPrivateRoomInvitations)); //Enable Listener is False.  Default is True.
                SoulSeekState.SoulseekClient = new SoulseekClient(new SoulseekClientOptions(messageTimeout: 30000, enableListener: SoulSeekState.ListenerEnabled, autoAcknowledgePrivateMessages: false, acceptPrivateRoomInvitations:SoulSeekState.AllowPrivateRoomInvitations,listenPort:SoulSeekState.ListenerPort, userInfoResponseResolver: UserInfoResponseHandler)); //Enable Listener is False.  Default is True.
                SoulSeekState.SoulseekClient.UserDataReceived += SoulseekClient_UserDataReceived;
                SoulSeekState.SoulseekClient.UserStatusChanged += SoulseekClient_UserStatusChanged;
                //SoulSeekState.SoulseekClient.TransferProgressUpdated += Upload_TransferProgressUpdated;
                SoulSeekState.SoulseekClient.TransferStateChanged += Upload_TransferStateChanged;

                SoulSeekState.SoulseekClient.TransferProgressUpdated += SoulseekClient_TransferProgressUpdated;
                SoulSeekState.SoulseekClient.TransferStateChanged += SoulseekClient_TransferStateChanged; ;

                SoulSeekState.SoulseekClient.Connected += SoulseekClient_Connected;
                SoulSeekState.SoulseekClient.StateChanged += SoulseekClient_StateChanged;
                SoulSeekState.SoulseekClient.LoggedIn += SoulseekClient_LoggedIn;
                SoulSeekState.SoulseekClient.ServerInfoReceived += SoulseekClient_ServerInfoReceived;
                SoulSeekState.BrowseResponseReceived += BrowseFragment.SoulSeekState_BrowseResponseReceived;

                SoulSeekState.SoulseekClient.PrivilegedUserListReceived += SoulseekClient_PrivilegedUserListReceived;

                MessageController.Initialize();
                ChatroomController.Initialize();
                SoulSeekState.DownloadAdded -= MainActivity.SoulSeekState_DownloadAdded;
                SoulSeekState.DownloadAdded += MainActivity.SoulSeekState_DownloadAdded;

                SoulseekClient.ErrorLogHandler -= MainActivity.SoulseekClient_ErrorLogHandler;
                SoulseekClient.ErrorLogHandler += MainActivity.SoulseekClient_ErrorLogHandler;
                SoulseekClient.DebugLogHandler -= MainActivity.DebugLogHandler;
                SoulseekClient.DebugLogHandler += MainActivity.DebugLogHandler;
            }

            UPnpManager.Context = this;
            UPnpManager.Instance.SearchAndSetMappingIfRequired();
            strings_kbs = this.Resources.GetString(Resource.String.kilobytes_per_second);
            strings_kHz = this.Resources.GetString(Resource.String.kilohertz);
            //shouldnt we also connect??? TODO TODO


        }

        public class ProgressUpdatedUI : EventArgs
        {
            public ProgressUpdatedUI(TransferItem _ti, bool _wasFailed, bool _fullRefresh, double _percentComplete)
            {
                ti=_ti;
                wasFailed=_wasFailed;
                fullRefresh=_fullRefresh;
                percentComplete = _percentComplete;
            }
            public TransferItem ti;
            public bool wasFailed;
            public bool fullRefresh;
            public double percentComplete;
        }

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
            int indexOfItem = -1;
            TransferItem relevantItem = TransfersFragment.GetTransferItemByFileName(e.Transfer?.Filename, out indexOfItem);
            if (relevantItem != null)
            {
                relevantItem.State = e.Transfer.State;
            }
            if (e.Transfer.State.HasFlag(TransferStates.Errored) || e.Transfer.State.HasFlag(TransferStates.TimedOut))
            {
                if(relevantItem == null)
                {
                    return;
                }
                relevantItem.Failed = true;
                StateChangedAtIndex?.Invoke(null, indexOfItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Queued))
            {
                if (relevantItem == null)
                {
                    return;
                }
                relevantItem.Queued = true;
                if (relevantItem.QueueLength != 0) //this means that it probably came from a search response where we know the users queuelength  ***BUT THAT IS NEVER THE ACTUAL QUEUE LENGTH*** its always much shorter...
                {
                    //nothing to do, bc its already set..
                    MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, null);
                }
                else //this means that it came from a browse response where we may not know the users initial queue length... or if its unexpectedly queued.
                {
                    //GET QUEUE LENGTH AND UPDATE...
                    MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, null);
                }
                StateChangedAtIndex?.Invoke(null, indexOfItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Initializing))
            {
                if (relevantItem == null)
                {
                    return;
                }
                //clear queued flag...
                relevantItem.Queued = false;
                relevantItem.QueueLength = 0;
                StateChangedAtIndex?.Invoke(null, indexOfItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Completed))
            {
                if (relevantItem == null)
                {
                    return;
                }
                if (!e.Transfer.State.HasFlag(TransferStates.Cancelled))
                {
                    //clear queued flag...
                    relevantItem.Progress = 100;
                    StateChangedAtIndex?.Invoke(null, indexOfItem);
                }
            }
            else
            {
                StateChangedAtIndex?.Invoke(null, indexOfItem);
            }
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
            if (TransfersFragment.transferItems == null)
            {
                MainActivity.LogDebug("transferItems Null " + e.Transfer.Filename);
                return;
            }
            lock (TransfersFragment.transferItems)
            {
                foreach (TransferItem item in TransfersFragment.transferItems) //THIS is where those enumeration exceptions are all coming from...
                {
                    if (item.FullFilename.Equals(e.Transfer.Filename))
                    {
                        relevantItem = item;
                        break;
                    }
                }
            }


            if (relevantItem == null)
            {
                //this happens on Clear and Cancel All.
                MainActivity.LogDebug("Relevant Item Null " + e.Transfer.Filename);
                MainActivity.LogDebug("transferItems.Count " + TransfersFragment.transferItems.Count);
                return;
            }
            else
            {
                bool fullRefresh = false;
                double percentComplete = e.Transfer.PercentComplete;
                relevantItem.Progress = (int)percentComplete;
                relevantItem.RemainingTime = e.Transfer.RemainingTime;




                // int indexRemoved = -1;
                if (SoulSeekState.AutoClearComplete && System.Math.Abs(percentComplete - 100) < .001) //if 100% complete and autoclear
                {

                    Action action = new Action(() => {
                        int before = TransfersFragment.transferItems.Count;
                        lock (TransfersFragment.transferItems)
                        {
                            //used to occur on nonUI thread.  Im pretty sure this causes the recyclerview inconsistency crash..
                            //indexRemoved = transferItems.IndexOf(relevantItem);
                            TransfersFragment.transferItems.Remove(relevantItem); //TODO: shouldnt we do the corresponding Adapter.NotifyRemoveAt
                        }
                        int after = TransfersFragment.transferItems.Count;
                        MainActivity.LogDebug("transferItems.Remove(relevantItem): before: " + before + "after: " + after);
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

                ProgressUpdated?.Invoke(null,new ProgressUpdatedUI(relevantItem,wasFailed,fullRefresh, percentComplete));

            }
        }

        public static string GetVersionString()
        {
            try
            {
                PackageInfo pInfo = SoulSeekState.ActiveActivityRef.PackageManager.GetPackageInfo(SoulSeekState.ActiveActivityRef.PackageName,0);
                return pInfo.VersionName;
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("GetVersionString: " + e.Message);
                return string.Empty;
            }
        }


        //this is a cache for localized strings accessed in tight loops...
        private static string strings_kbs;
        public static string STRINGS_KBS
        {
            get
            {
                return strings_kbs;
            }
            private set
            {
                strings_kbs = value;
            }
        }

        private static string strings_kHz;
        public static string STRINGS_KHZ
        {
            get
            {
                return strings_kHz;
            }
            private set
            {
                strings_kHz = value;
            }
        }

        public static Task<UserInfo> UserInfoResponseHandler(string uname, IPEndPoint ipEndPoint)
        {
            if(IsUserInIgnoreList(uname))
            {
                return Task.FromResult(new UserInfo(string.Empty, 0, 0, false));
            }
            string bio = SoulSeekState.UserInfoBio ?? string.Empty;
            byte[] picture = GetUserInfoPicture();
            int uploadSlots = 1;
            int queueLength = 0;
            bool hasFreeSlots = true;
            if(!SoulSeekState.SharingOn) //in my experience even if someone is sharing nothing they say 1 upload slot and yes free slots.. but idk maybe 0 and no makes more sense??
            {
                uploadSlots = 0;
                queueLength = 0;
                hasFreeSlots = false;
            }

            return Task.FromResult(new UserInfo(bio, picture, uploadSlots, queueLength, hasFreeSlots));
        }

        private static byte[] GetUserInfoPicture()
        {
            if(SoulSeekState.UserInfoPictureName == null || SoulSeekState.UserInfoPictureName==string.Empty)
            {
                return null;
            }
            Java.IO.File userInfoPicDir = new Java.IO.File(ApplicationContext.FilesDir, EditUserInfoActivity.USER_INFO_PIC_DIR);
            if(!userInfoPicDir.Exists())
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
            if(e.WishlistInterval.HasValue)
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
            if(e.PreviousState.HasFlag(SoulseekClientStates.LoggedIn) && e.State.HasFlag(SoulseekClientStates.Disconnecting))
            {
                if (e.Exception is KickedFromServerException)
                {
                    MainActivity.LogDebug("Kicked Kicked Kicked");
                    if(SoulSeekState.ActiveActivityRef!=null)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(()=>{Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.kicked_due_to_other_client), ToastLength.Long).Show();});
                    }
                    return; //DO NOT RETRY!!! or will do an infinite loop!
                }



                //this is a "true" connected to disconnected
                ChatroomController.ClearAndCacheJoined();
                MainActivity.LogDebug("disconnected " + DateTime.UtcNow.ToString());
                if(SoulSeekState.logoutClicked)
                {
                    SoulSeekState.logoutClicked = false;
                }
                else if (AUTO_CONNECT_ON && SoulSeekState.currentlyLoggedIn)
                {
                    Thread reconnectRetrier = new Thread(ReconnectExponentialBackOffThreadTask);
                    reconnectRetrier.Start();
                }
            }
        }

        private void SoulseekClient_LoggedIn(object sender, EventArgs e)
        {
            ChatroomController.JoinAutoJoinRoomsAndPreviousJoined();
            //ChatroomController.ConnectionLapse.Add(new Tuple<bool, DateTime>(true, DateTime.UtcNow)); //just testing obv remove later...
            MainActivity.LogDebug("logged in " + DateTime.UtcNow.ToString());
            MainActivity.LogDebug("Listening State: " + SoulSeekState.SoulseekClient.GetListeningState());
            if (SoulSeekState.ListenerEnabled && !SoulSeekState.SoulseekClient.GetListeningState())
            {
                if(SoulSeekState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("SoulSeekState.ActiveActivityRef null SoulseekClient_LoggedIn");
                }
                else
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
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
            if(SoulSeekState.ActiveActivityRef==null)
            {
                MainActivity.LogDebug("cant show toast, active activity ref is null");
            }
            else
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread( () => { Toast.MakeText(SoulSeekState.ActiveActivityRef,msg, toastLength).Show(); } );
            }
        }

        public static string GetString(int resId)
        {
            return SoulSeekState.ActiveActivityRef.GetString(resId);
        }


        public static void ReconnectExponentialBackOffThreadTask()
        {
            int MAX_TRIES = 6;
            for(int i=0;i< MAX_TRIES; i++)
            {
                if(SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || !SoulSeekState.currentlyLoggedIn) 
                {   //^SoulSeekState.currentlyLoggedIn is if we are supposed to be logged in. so if false then we logged ourselves out on purpose so do not try to reconnect
                    return; //our work here is done
                }
                if(i!= MAX_TRIES-1)
                {
                    System.Threading.Thread.Sleep((int)(1000*Math.Pow((i+1),2)));
                }
                else
                {
                    //do 2 mins for the last try..
                    System.Threading.Thread.Sleep(1000*60*2);
                }
                try
                {
                    Task t = SoulSeekState.SoulseekClient.ConnectAsync(SoulSeekState.Username, SoulSeekState.Password);
                    t.Wait();
                    if(t.IsCompletedSuccessfully) 
                    {
                        MainActivity.LogDebug("RETRY " + i + "SUCCEEDED");
                        return; //our work here is done
                    }
                }
                catch(Exception)
                {

                }
                //if we got here we failed.. so try again shortly...
                MainActivity.LogDebug("RETRY " + i + "FAILED");
            }
        }

        public static void AddToIgnoreListFeedback(Context c, string username)
        {
            if (SeekerApplication.AddToIgnoreList(username))
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.added_to_ignore),username), ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.already_added_to_ignore), username), ToastLength.Short).Show();
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
            if(MainActivity.UserListContainsUser(username))
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
            return SoulSeekState.IgnoreUserList.Exists(userListItem => { return userListItem.Username == username; });
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
            if (e.Transfer.State.HasFlag(TransferStates.Succeeded))
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
                    Helpers.CreateNotificationChannel(SoulSeekState.MainActivityRef, MainActivity.UPLOADS_CHANNEL_ID, MainActivity.UPLOADS_CHANNEL_NAME);
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
                    nmc.Notify(notifInfo.NOTIF_ID_FOR_USER, n);
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
        private static void UserListAddIfContainsUser(string username, UserData userData, UserStatus userStatus)
        {
            lock (SoulSeekState.UserList)
            {
                bool found = false;
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
                            item.UserStatus = userStatus;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    //hopefully someone we later removed (since no RemoveUser command...)
                    //but also it could be users people added on a different client so maybe we want them???
                }
            }
        }


        public static void RestoreSoulSeekState(ISharedPreferences sharedPreferences) //the Bundle can be SLOWER than the SHARED PREFERENCES if SHARED PREFERENCES was saved in a different activity.  The best exapmle being DAYNIGHTMODE
        {   //day night mode sets the static, saves to shared preferences the new value, sets appcompat value, which recreates everything and calls restoreSoulSEekstate(bundle) where the bundle was older than shared prefs
            //because saveSoulSeekstate was not called in the meantime...
            if (sharedPreferences != null)
            {
                SoulSeekState.currentlyLoggedIn = sharedPreferences.GetBoolean(SoulSeekState.M_CurrentlyLoggedIn, false);
                SoulSeekState.Username = sharedPreferences.GetString(SoulSeekState.M_Username, "");
                SoulSeekState.Password = sharedPreferences.GetString(SoulSeekState.M_Password, "");
                SoulSeekState.SaveDataDirectoryUri = sharedPreferences.GetString(SoulSeekState.M_SaveDataDirectoryUri, "");
                SoulSeekState.NumberSearchResults = sharedPreferences.GetInt(SoulSeekState.M_NumberSearchResults, MainActivity.DEFAULT_SEARCH_RESULTS);
                SoulSeekState.DayNightMode = sharedPreferences.GetInt(SoulSeekState.M_DayNightMode, (int)AppCompatDelegate.ModeNightFollowSystem);
                SoulSeekState.AutoClearComplete = sharedPreferences.GetBoolean(SoulSeekState.M_AutoClearComplete, false);
                SoulSeekState.RememberSearchHistory = sharedPreferences.GetBoolean(SoulSeekState.M_RememberSearchHistory, true);
                SoulSeekState.FreeUploadSlotsOnly = sharedPreferences.GetBoolean(SoulSeekState.M_OnlyFreeUploadSlots, true);
                SoulSeekState.DisableDownloadToastNotification = sharedPreferences.GetBoolean(SoulSeekState.M_DisableToastNotifications, false);
                SoulSeekState.MemoryBackedDownload = sharedPreferences.GetBoolean(SoulSeekState.M_MemoryBackedDownload, false);
                SearchFragment.FilterSticky = sharedPreferences.GetBoolean(SoulSeekState.M_FilterSticky, false);
                SearchFragment.FilterStickyString = sharedPreferences.GetString(SoulSeekState.M_FilterStickyString, string.Empty);
                SearchFragment.SetSearchResultStyle(sharedPreferences.GetInt(SoulSeekState.M_SearchResultStyle, 1));
                SoulSeekState.UploadSpeed = sharedPreferences.GetInt(SoulSeekState.M_UploadSpeed, -1);
                SoulSeekState.UploadDataDirectoryUri = sharedPreferences.GetString(SoulSeekState.M_UploadDirectoryUri, "");
                SoulSeekState.SharingOn = sharedPreferences.GetBoolean(SoulSeekState.M_SharingOn, false);
                SoulSeekState.UserList = RestoreUserListFromString(sharedPreferences.GetString(SoulSeekState.M_UserList, string.Empty));
                SoulSeekState.IgnoreUserList = RestoreUserListFromString(sharedPreferences.GetString(SoulSeekState.M_IgnoreUserList, string.Empty));
                SoulSeekState.AllowPrivateRoomInvitations = sharedPreferences.GetBoolean(SoulSeekState.M_AllowPrivateRooomInvitations, false);
                SoulSeekState.StartServiceOnStartup = sharedPreferences.GetBoolean(SoulSeekState.M_ServiceOnStartup, true);

                SoulSeekState.UserInfoBio = sharedPreferences.GetString(SoulSeekState.M_UserInfoBio, string.Empty);
                SoulSeekState.UserInfoPictureName = sharedPreferences.GetString(SoulSeekState.M_UserInfoPicture, string.Empty);

                SoulSeekState.UserNotes = RestoreUserNotesFromString(sharedPreferences.GetString(SoulSeekState.M_UserNotes, string.Empty));

                SearchTabHelper.RestoreStateFromSharedPreferences();
                SettingsActivity.RestoreAdditionalDirectorySettingsFromSharedPreferences();
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

        public static System.Collections.Concurrent.ConcurrentDictionary<string,string> RestoreUserNotesFromString(string base64userNotes)
        {
            if (base64userNotes == string.Empty)
            {
                return new System.Collections.Concurrent.ConcurrentDictionary<string,string>();
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

        private void SoulseekClient_UserStatusChanged(object sender, UserStatusChangedEventArgs e)
        {
            if (e.Username == SoulSeekState.Username)
            {
                //not sure this will ever happen
            }
            else
            {
                if (SoulSeekState.UserList == null)
                {
                    MainActivity.LogFirebase("UserList is null on user status receive");
                }
                else
                {
                    UserListAddIfContainsUser(e.Username, null, new UserStatus(e.Status, e.IsPrivileged));
                }
            }

        }


    }


    public class SearchResponseComparer : IEqualityComparer<SearchResponse>
    {
        public bool Equals(SearchResponse s1, SearchResponse s2)
        {
            if(s1.Username == s2.Username)
            {
                if(s1.Files.Count == s2.Files.Count)
                {
                    if(s1.Files.First().Filename == s2.Files.First().Filename)
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
            return s1.Username.GetHashCode() + s1.Files.First().Filename.GetHashCode();
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
        private static System.Collections.Concurrent.ConcurrentDictionary<int, List<SearchResponse>> OldResultsToCompare = new System.Collections.Concurrent.ConcurrentDictionary<int, List<SearchResponse>>();

        public static void Initialize() //we need the wishlist interval before we can init
        {
            if(IsInitialized)
            {
                return;
            }
            if(searchIntervalMilliseconds == 0)
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
                searchIntervalMilliseconds = 2* 60 * 1000; //min of 2 mins...
            }
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
            var newResponses = SearchTabHelper.SearchTabCollection[id].SearchResponses.ToList();
            var differenceNewResults = newResponses.Except(OldResultsToCompare[id],new SearchResponseComparer()).ToList();
            int newUniqueResults = differenceNewResults.Count;
            if (newUniqueResults >= 1)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                    try
                    {
                        string description = string.Empty;
                        if(newUniqueResults>1)
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
                        notifIntent.AddFlags(ActivityFlags.SingleTop);
                        notifIntent.PutExtra(FromWishlistString, 1); //the tab to go to
                        notifIntent.PutExtra(FromWishlistStringID, id); //the tab to go to
                        PendingIntent pendingIntent =
                            PendingIntent.GetActivity(SoulSeekState.ActiveActivityRef, lastTerm.GetHashCode(), notifIntent, PendingIntentFlags.UpdateCurrent);
                        Notification n = Helpers.CreateNotification(SoulSeekState.ActiveActivityRef, pendingIntent, CHANNEL_ID,  SoulSeekState.ActiveActivityRef.GetString(Resource.String.wishlist) + ": " + lastTerm, description, false);
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
            SearchTabHelper.SaveStateToSharedPreferences();
        }

        private static void WishlistTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(SearchTabHelper.SearchTabCollection!=null)
            {
                var wishlistPairs = SearchTabHelper.SearchTabCollection.Where(pair=>pair.Value.SearchTarget==SearchTarget.Wishlist);
                if(wishlistPairs.Count()==0)
                {
                    return;
                }
                else
                {
                    MainActivity.LogInfoFirebase("wishlist search ran " + searchIntervalMilliseconds);
                    DateTime oldest = DateTime.MaxValue;
                    int oldestId = int.MaxValue;
                    foreach(var pair in wishlistPairs)
                    {
                        if(pair.Value.LastRanTime < oldest)
                        {
                            oldest = pair.Value.LastRanTime;
                            oldestId = pair.Key;
                        }
                    }
                    //oldestId is the one we want to autosearch

                    OldResultsToCompare[oldestId] = SearchTabHelper.SearchTabCollection[oldestId].SearchResponses.ToList();
                    
                    SearchFragment.SearchAPI((new CancellationTokenSource()).Token ,null, SearchTabHelper.SearchTabCollection[oldestId].LastSearchTerm, oldestId, true);

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
        public override IBinder OnBind(Intent intent)
        {
            return null; //does not allow binding. 
        }



        public static Notification CreateNotification(Context context, String contentText)
        {
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            notifIntent.PutExtra(FromTransferString, 2);
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, 0, notifIntent, 0);
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return Helpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.download_in_progress), contentText);
        }


        public static string PluralTransfersRemaining
        {
            get
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.transfers_remaining);
            }
        }

        public static string SingularTransfersRemaining
        {
            get
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.transfer_remaining);
            }
        }


        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            SoulSeekState.DownloadKeepAliveServiceRunning = true;

            Helpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
            Notification notification = null;
            int cnt = MainActivity.DL_COUNT;
            if (cnt == -1)
            {
                notification = CreateNotification(this, this.GetString(Resource.String.transfers_in_progress));
            }
            else
            {
                if(cnt == 1)
                {
                    notification = CreateNotification(this, string.Format(SingularTransfersRemaining, 1));
                }
                else
                {
                    notification = CreateNotification(this, string.Format(PluralTransfersRemaining, cnt));
                }
            }

            try
            {
            if(MainActivity.CpuKeepAlive!=null&&!MainActivity.CpuKeepAlive.IsHeld)
            {
                MainActivity.CpuKeepAlive.Acquire();
            }
            if (MainActivity.WifiKeepAlive != null && !MainActivity.WifiKeepAlive.IsHeld)
            {
                MainActivity.WifiKeepAlive.Acquire();
            }

            if(MainActivity.KeepAliveInactivityKillTimer != null)
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
            catch(System.Exception e)
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
            if (MainActivity.CpuKeepAlive != null)
            {
                MainActivity.CpuKeepAlive.Release();
            }
            if (MainActivity.WifiKeepAlive != null)
            {
                MainActivity.WifiKeepAlive.Release();
            }
            if(MainActivity.KeepAliveInactivityKillTimer != null)
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop();
            }
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

        public static Android.Net.Wifi.WifiManager.WifiLock WifiKeepAlive = null;
        public static PowerManager.WakeLock CpuKeepAlive = null;


        public override IBinder OnBind(Intent intent)
        {
            return null; //does not allow binding. 
        }



        public static Notification CreateNotification(Context context)
        {
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            notifIntent.AddFlags(ActivityFlags.SingleTop);
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, 0, notifIntent, 0);
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return Helpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.seeker_running), context.GetString(Resource.String.seeker_running_content));
        }


        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            MainActivity.LogInfoFirebase("keep alive service started...");
            SoulSeekState.IsStartUpServiceCurrentlyRunning = true;

            Helpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
            Notification notification = CreateNotification(this);

            try
            {
                if (CpuKeepAlive != null && !CpuKeepAlive.IsHeld)
                {
                    CpuKeepAlive.Acquire();
                    MainActivity.LogInfoFirebase("CpuKeepAlive acquire");
                }
                if (WifiKeepAlive != null && !WifiKeepAlive.IsHeld)
                {
                    WifiKeepAlive.Acquire();
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
            if (CpuKeepAlive != null && CpuKeepAlive.IsHeld)
            {
                CpuKeepAlive.Release();
                MainActivity.LogInfoFirebase("CpuKeepAlive release");
            }
            else if(CpuKeepAlive==null)
            {
                MainActivity.LogFirebase("CpuKeepAlive is null");
            }
            else if (!CpuKeepAlive.IsHeld)
            {
                MainActivity.LogFirebase("CpuKeepAlive not held");
            }
            if (WifiKeepAlive != null && WifiKeepAlive.IsHeld)
            {
                WifiKeepAlive.Release();
                MainActivity.LogInfoFirebase("WifiKeepAlive release");
            }
            else if(WifiKeepAlive == null)
            {
                MainActivity.LogFirebase("WifiKeepAlive is null");
            }
            else if(!WifiKeepAlive.IsHeld)
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














    //, WindowSoftInputMode = SoftInput.StateAlwaysHidden) didnt change anything..
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true/*, WindowSoftInputMode = SoftInput.AdjustNothing*/)]
    public class MainActivity : AppCompatActivity, AndriodApp1.MainActivity.DownloadCallback, ActivityCompat.IOnRequestPermissionsResultCallback, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        public static object SHARED_PREF_LOCK = new object();
        public const string logCatTag = "seeker";
        public static bool crashlyticsEnabled = true;
        public static void LogDebug(string msg)
        {
            #if ADB_LOGCAT
              log.Debug(logCatTag, msg);
            #endif
        }
        public static void LogFirebase(string msg)
        {
#if !IzzySoft
            if(crashlyticsEnabled)
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
#if !IzzySoft
            if(crashlyticsEnabled)
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
                KeyBoardVisibilityChanged?.Invoke(null,isShown);
                //onKeyboardVisibilityListener.onVisibilityChanged(isShown);
            }
        }

        public static EventHandler<bool> KeyBoardVisibilityChanged; 

        public void KeyboardChanged(object sender, bool isShown)
        {
            if(isShown)
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
        /// Searchable Filename, Uri.ToString(), length
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="directoryCount"></param>
        /// <returns></returns>
        public List<Tuple<string, string, long>> ParseSharedDirectory(DocumentFile dir, ref int directoryCount, out Dictionary<string,List<Tuple<string,string,long>>> auxilaryDuplicatesList)
        {
            List<Tuple<string, string, long>> pairs = new List<Tuple<string, string, long>>();
            auxilaryDuplicatesList = new Dictionary<string, List<Tuple<string, string, long>>>();
            traverseDocumentFile(dir,pairs, auxilaryDuplicatesList, ref directoryCount);
            return pairs;
        }


        private void traverseToGetDirectories(DocumentFile dir, List<Android.Net.Uri> dirUris)
        {
            if(dir.IsDirectory)
            {
                DocumentFile[] files = dir.ListFiles();
                for (int i = 0; i < files.Length; ++i)
                {
                    DocumentFile file = files[i];
                    if (file.IsDirectory)
                    {
                        dirUris.Add(file.Uri);
                        traverseToGetDirectories(file,dirUris);
                    }
                }
            }
        }

        private static Soulseek.Directory SlskDirFromDocumentFile(DocumentFile dirFile, string dirToStrip, bool diagFromDirectoryResolver)
        {
            string directoryPath = dirFile.Uri.Path; //on the emulator this is /tree/downloads/document/docwonlowds but the dirToStrip is uppercase Downloads
            directoryPath = directoryPath.Replace("/", @"\");
            try
            {
                directoryPath = directoryPath.Substring(directoryPath.ToLower().IndexOf(dirToStrip.ToLower()));
                directoryPath = directoryPath.Replace("/", @"\"); //probably strip out the root shared dir...
            }
            catch(Exception e)
            {
                //Non-fatal Exception: java.lang.Throwable: directoryPath: False\tree\msd:824\document\msd:825MusicStartIndex cannot be less than zero.
                //its possible for dirToStrip to be null
                //True\tree\0000-0000:Musica iTunes\document\0000-0000:Musica iTunesObject reference not set to an instance of an object 
                //Non-fatal Exception: java.lang.Throwable: directoryPath: True\tree\3061-6232:Musica\document\3061-6232:MusicaObject reference not set to an instance of an object  at AndriodApp1.MainActivity.SlskDirFromDocumentFile (AndroidX.DocumentFile.Provider.DocumentFile dirFile, System.String dirToStrip) [0x00024] in <778faaf2e13641b38ae2700aacc789af>:0 
                LogFirebase("directoryPath: " + (dirToStrip==null).ToString() + directoryPath + " from directory resolver: "+ diagFromDirectoryResolver+" toStrip: " + dirToStrip + e.Message + e.StackTrace);
            }
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
                    string fname = Helpers.GetFileNameFromFile(f.Uri.Path.Replace("/", @"\"));
                    string folderName = Helpers.GetFolderNameFromFile(f.Uri.Path.Replace("/", @"\"));
                    string searchableName = /*folderName + @"\" + */fname; //for the brose response should only be the filename!!! 
                    //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                    //bc we get a path that is:
                    //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\(2009.09.23) Sufjan Stevens - Live from Castaways\\09 Between Songs 4.mp3"
                    //not quite a full URI but it does add quite a bit..

                    //if (searchableName.Length > 7 && searchableName.Substring(0, 8).ToLower() == "primary:")
                    //{
                    //    searchableName = searchableName.Substring(8);
                    //}
                    var slskFile = new Soulseek.File(1, searchableName.Replace("/", @"\"), f.Length(), System.IO.Path.GetExtension(f.Uri.Path));
                    files.Add(slskFile);
                }
                catch(Exception e)
                {
                    LogDebug("Parse error with " + f.Uri.Path + e.Message + e.StackTrace);
                    LogFirebase("Parse error with " + f.Uri.Path + e.Message + e.StackTrace);
                }

            }
            var slskDir = new Soulseek.Directory(directoryPath, files);
            return slskDir;
        }

        /// <summary>
        /// Here you want a flattened list of directories.  Directories should have full paths.  Each dir has files only.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private BrowseResponse ParseSharedDirectoryForBrowseResponse(DocumentFile dir, ref List<Tuple<string,string>> friendlyDirNameToUriMapping)
        {
            List<Android.Net.Uri> dirUris = new List<Android.Net.Uri>();
            dirUris.Add(dir.Uri);
            traverseToGetDirectories(dir, dirUris);
            List<Soulseek.Directory> allDirs = new List<Soulseek.Directory>();
            foreach(Android.Net.Uri dirUri in dirUris)
            {
                DocumentFile dirFile = null;
                if(SoulSeekState.PreOpenDocumentTree())
                {
                    dirFile = DocumentFile.FromFile(new Java.IO.File(dirUri.Path));
                }
                else
                {
                    dirFile = DocumentFile.FromTreeUri(this,dirUri); //will return null or not exists for API below 21.
                }
                if(dir.Name==null)
                {
                    MainActivity.LogInfoFirebase("dirname is null " + dir.Uri?.ToString() ?? "dirUriIsNull");
                }
                var slskDir = SlskDirFromDocumentFile(dirFile, dir.Name, false);
                friendlyDirNameToUriMapping.Add(new Tuple<string, string>(slskDir.Name, dirFile.Uri.ToString()));
                allDirs.Add(slskDir);
            }
            return new BrowseResponse(allDirs,null);
        }

        public class CachedParseResults
        {
            public List<Tuple<string, string, long>> keys = null;
            public int direcotryCount = -1;
            public BrowseResponse browseResponse = null;
            public List<Tuple<string, string>> friendlyDirNameToUriMapping = null;
            public Dictionary<string, List<Tuple<string, string, long>>> dupAux = null;
        }

        public void ClearParsedCacheResults()
        {
            try
            {
                lock(SHARED_PREF_LOCK)
                { 
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_CACHE_stringUriPairs, string.Empty);
                editor.PutString(SoulSeekState.M_CACHE_browseResponse, string.Empty);
                editor.PutString(SoulSeekState.M_CACHE_friendlyDirNameToUriMapping, string.Empty);
                editor.PutString(SoulSeekState.M_CACHE_auxDupList, string.Empty);
                editor.Commit();
                }
            }
            catch(Exception e)
            {
                LogDebug("ClearParsedCacheResults " + e.Message + e.StackTrace);
                LogFirebase("ClearParsedCacheResults " + e.Message + e.StackTrace);
            }
        }

        public CachedParseResults GetCachedParseResults()
        {
            string s_stringUriPairs = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_stringUriPairs, string.Empty);
            string s_BrowseResponse = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_browseResponse, string.Empty);
            string s_FriendlyDirNameMapping = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_friendlyDirNameToUriMapping, string.Empty);       
            string s_AuxDupList = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_CACHE_auxDupList, string.Empty); //this one is optional!!
            if(s_stringUriPairs==string.Empty|| s_BrowseResponse==string.Empty|| s_FriendlyDirNameMapping==string.Empty)
            {
                return null;
            }
            else
            {
                //deserialize..
                try
                {
                    byte[] b_stringUriPairs = Convert.FromBase64String(s_stringUriPairs);
                    byte[] b_BrowseResponse = Convert.FromBase64String(s_BrowseResponse);
                    byte[] b_FriendlyDirNameMapping = Convert.FromBase64String(s_FriendlyDirNameMapping);
                    using(System.IO.MemoryStream m_stringUriPairs = new System.IO.MemoryStream(b_stringUriPairs))
                    using(System.IO.MemoryStream m_BrowseResponse = new System.IO.MemoryStream(b_BrowseResponse))
                    using(System.IO.MemoryStream m_FriendlyDirNameMapping = new System.IO.MemoryStream(b_FriendlyDirNameMapping))
                    {
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        CachedParseResults cachedParseResults = new CachedParseResults();
                        cachedParseResults.keys = binaryFormatter.Deserialize(m_stringUriPairs) as List<Tuple<string, string, long>>;
                        cachedParseResults.browseResponse = binaryFormatter.Deserialize(m_BrowseResponse) as BrowseResponse;
                        cachedParseResults.friendlyDirNameToUriMapping = binaryFormatter.Deserialize(m_FriendlyDirNameMapping) as List<Tuple<string, string>>;
                        cachedParseResults.direcotryCount = cachedParseResults.browseResponse.DirectoryCount;

                        if (s_AuxDupList != string.Empty)
                        {
                            byte[] b_AuxDupList = Convert.FromBase64String(s_AuxDupList);
                            using (System.IO.MemoryStream m_AuxDupList = new System.IO.MemoryStream(b_AuxDupList))
                            {
                                cachedParseResults.dupAux = binaryFormatter.Deserialize(m_AuxDupList) as Dictionary<string, List<Tuple<string, string, long>>>;
                            }
                        }

                        return cachedParseResults;
                    }

                }
                catch(Exception e)
                {
                    LogDebug("error deserializing" + e.Message + e.StackTrace);
                    LogFirebase("error deserializing" + e.Message + e.StackTrace);
                    return null;
                }
            }
        }

        /// <summary>
        /// Check Cache should be false if setting a new dir.. true if on startup.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="checkCache"></param>
        public void InitializeDatabase(DocumentFile dir,bool checkCache)
        {
            bool success = false;
            try
            {
                CachedParseResults cachedParseResults = null;
                if (checkCache)
                {
                    cachedParseResults = GetCachedParseResults();
                }

                if(cachedParseResults==null)
                {
                    LogDebug("No cached results");
                    int directoryCount = 0;
                    System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
                    s.Start();
                    Dictionary<string, List<Tuple<string, string, long>>> auxilaryDuplicatesList = null;
                    var stringUriPairs = ParseSharedDirectory(dir,ref directoryCount, out auxilaryDuplicatesList); //for 550 files takes 15 seconds (debug mode)
                    s.Stop();
                    LogDebug("ParseSharedDirectory: " + s.ElapsedMilliseconds);
                    s.Reset();
                    s.Start();
                    List<Tuple<string, string>> friendlyDirNameToUriMapping = new List<Tuple<string, string>>();
                    var browseResponse = ParseSharedDirectoryForBrowseResponse(dir, ref friendlyDirNameToUriMapping); //for 550 files takes 30 seconds (debug mode). serialized is 95kB.


                    //using (var writer = new System.IO.StringWriter())
                    //{
                    //    var serializer = new System.Xml.(browseResponse.GetType());
                    //    serializer.Serialize(writer, browseResponse);
                    //    string serializedString = writer.ToString();
                    //    LogDebug(serializedString.Length.ToString());
                    //}


                    s.Stop();
                    LogDebug("ParseSharedDirectoryForBrowseResponse: " + s.ElapsedMilliseconds);
                    s.Reset();

                    //put into cache.
                    using(System.IO.MemoryStream bResponsememoryStream = new System.IO.MemoryStream())
                    using(System.IO.MemoryStream stringUrimemoryStream = new System.IO.MemoryStream())
                    using(System.IO.MemoryStream friendlyDirNamememoryStream = new System.IO.MemoryStream())
                    using(System.IO.MemoryStream auxDupListStream = new System.IO.MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(bResponsememoryStream, browseResponse);
                        string bResponse = Convert.ToBase64String(bResponsememoryStream.ToArray());

                        formatter.Serialize(stringUrimemoryStream,stringUriPairs);
                        string bstringUriPairs = Convert.ToBase64String(stringUrimemoryStream.ToArray());

                        formatter.Serialize(friendlyDirNamememoryStream, friendlyDirNameToUriMapping);
                        string bfriendlyDirName = Convert.ToBase64String(friendlyDirNamememoryStream.ToArray());

                        formatter.Serialize(auxDupListStream, auxilaryDuplicatesList);
                        string bAuxDupListStreamName = Convert.ToBase64String(auxDupListStream.ToArray());
                        lock (SHARED_PREF_LOCK)
                        {
                            var editor = SoulSeekState.SharedPreferences.Edit();
                        editor.PutString(SoulSeekState.M_CACHE_stringUriPairs, bstringUriPairs);
                        editor.PutString(SoulSeekState.M_CACHE_browseResponse, bResponse);
                        editor.PutString(SoulSeekState.M_CACHE_friendlyDirNameToUriMapping, bfriendlyDirName);
                        editor.PutString(SoulSeekState.M_CACHE_auxDupList, bAuxDupListStreamName);
                        editor.Commit();
                        }
                    }

                    SlskHelp.SharedFileCache sharedFileCache = new SlskHelp.SharedFileCache(stringUriPairs, auxilaryDuplicatesList, directoryCount, browseResponse, friendlyDirNameToUriMapping);//.Select(_=>_.Item1).ToList());
                    sharedFileCache.Refreshed += SharedFileCache_Refreshed;
                    sharedFileCache.Fill();
                    SoulSeekState.SharedFileCache = sharedFileCache;
                }
                else
                {
                    LogDebug("Using cached results");
                    SlskHelp.SharedFileCache sharedFileCache = new SlskHelp.SharedFileCache(cachedParseResults.keys, cachedParseResults.dupAux, cachedParseResults.direcotryCount, cachedParseResults.browseResponse, cachedParseResults.friendlyDirNameToUriMapping);//.Select(_=>_.Item1).ToList());
                    sharedFileCache.Refreshed += SharedFileCache_Refreshed;
                    sharedFileCache.Fill();
                    SoulSeekState.SharedFileCache = sharedFileCache;
                }
                success = true;
                SoulSeekState.FailedShareParse = false;
                SoulSeekState.SharedFileCache.SuccessfullyInitialized = true;
            }
            catch(Exception e)
            {
                LogDebug("Error parsing files: " + e.Message + e.StackTrace);
                LogFirebase("Error parsing files: " + e.Message + e.StackTrace);
            }
            finally
            {
                if(!success)
                {
                    SoulSeekState.UploadDataDirectoryUri = null;
                    SoulSeekState.FailedShareParse = true;
                    //if success if false then SoulSeekState.SharedFileCache might be null still causing a crash!
                    if(SoulSeekState.SharedFileCache!=null)
                    {
                        SoulSeekState.SharedFileCache.SuccessfullyInitialized = false;
                    }
                }
            }
            //SoulSeekState.SoulseekClient.SearchResponseDelivered += SoulseekClient_SearchResponseDelivered;
            //SoulSeekState.SoulseekClient.SearchResponseDeliveryFailed += SoulseekClient_SearchResponseDeliveryFailed;
        }

        private void SoulseekClient_SearchResponseDeliveryFailed(object sender, SearchRequestResponseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void SoulseekClient_SearchResponseDelivered(object sender, SearchRequestResponseEventArgs e)
        {

        }

        private void SharedFileCache_Refreshed(object sender, (int Directories, int Files) e)
        {
            if(SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                SoulSeekState.SoulseekClient.SetSharedCountsAsync(e.Directories,e.Files);
            }
        }

        /// <summary>
        /// Inform server the number of files we are sharing or 0,0 if not sharing...
        /// </summary>
        public static void InformServerOfSharedFiles()
        {
            try
            {
                if (SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
                {
                    if(MeetsSharingConditions() && SoulSeekState.SharedFileCache != null && SoulSeekState.SharedFileCache.SuccessfullyInitialized)
                    {
                        MainActivity.LogDebug("Tell server we are sharing " + SoulSeekState.SharedFileCache.DirectoryCount + " dirs and " + SoulSeekState.SharedFileCache.FileCount + " files" );
                        SoulSeekState.SoulseekClient.SetSharedCountsAsync(SoulSeekState.SharedFileCache.DirectoryCount, SoulSeekState.SharedFileCache.FileCount);
                    }
                    else
                    {
                        MainActivity.LogDebug("Tell server we are sharing 0 dirs and 0 files");
                        SoulSeekState.SoulseekClient.SetSharedCountsAsync(0,0);
                    }
                }
            }
            catch(Exception e)
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
        public void traverseDocumentFile(DocumentFile dir, List<Tuple<string, string,long>> pairs, Dictionary<string,List<Tuple<string, string, long>>> auxilaryDuplicatesList, ref int directoryCount)
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
                        traverseDocumentFile(file, pairs, auxilaryDuplicatesList, ref directoryCount);
                    }
                    else
                    {
                        // do something here with the file
                        //LogDebug(file.Uri.ToString()); //encoded string representation //content://com.android.externalstorage.documents/tree/primary%3ASoulseek%20Complete/document/primary%3ASoulseek%20Complete%2F41-60%2F14-B-181%20Welcome%20To%20New%20York-Taylor%20Swift.mp3
                        //LogDebug(file.Uri.Path.ToString()); //gets decoded path // /tree/primary:Soulseek Complete/document/primary:Soulseek Complete/41-60/14-B-181 Welcome To New York-Taylor Swift.mp3
                        //LogDebug(Android.Net.Uri.Decode(file.Uri.ToString())); // content://com.android.externalstorage.documents/tree/primary:Soulseek Complete/document/primary:Soulseek Complete/41-60/14-B-181 Welcome To New York-Taylor Swift.mp3
                        //LogDebug(file.Uri.EncodedPath);  // /tree/primary%3ASoulseek%20Complete/document/primary%3ASoulseek%20Complete%2F41-60%2F14-B-181%20Welcome%20To%20New%20York-Taylor%20Swift.mp3 
                        //LogDebug(file.Uri.LastPathSegment); // primary:Soulseek Complete/41-60/14-B-181 Welcome To New York-Taylor Swift.mp3

                        string fullPath = file.Uri.Path.ToString().Replace('/','\\');

                        string searchableName = Helpers.GetFolderNameFromFile(fullPath) + @"\" + Helpers.GetFileNameFromFile(fullPath);
                        if(searchableName.Substring(0,8).ToLower()=="primary:")
                        {
                            searchableName = searchableName.Substring(8);
                        }
                        if(pairs.Exists((Tuple<string, string, long> tuple)=>{return tuple.Item1==searchableName; }))
                        {
                            //if there already exists a tuple with the same searchable name, put it into an auxilary list..
                            Tuple<string,string,long> matching = pairs.Find((Tuple<string, string, long> tuple) => { return tuple.Item1 == searchableName; });
                            pairs.Remove(matching);
                            pairs.Add(new Tuple<string,string,long>(matching.Item1,string.Empty,-1)); // this is how we know that we should look elsewhere for it...
                            if(auxilaryDuplicatesList.ContainsKey(searchableName))
                            {
                                auxilaryDuplicatesList[searchableName].Add(new Tuple<string,string,long>(searchableName, file.Uri.ToString(), file.Length()));
                            }
                            else
                            {
                                List<Tuple<string,string,long>> listOfDuplicates = new List<Tuple<string, string, long>>();
                                listOfDuplicates.Add(matching);
                                listOfDuplicates.Add(new Tuple<string, string, long>(searchableName, file.Uri.ToString(), file.Length()));
                                auxilaryDuplicatesList[searchableName] = listOfDuplicates;
                            }
                        }
                        else
                        {
                            pairs.Add(new Tuple<string,string,long>(searchableName, file.Uri.ToString(), file.Length()));
                        }
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


        public static string SETTINGS_INTENT = "com.example.seeker.SETTINGS";
        public static int SETTINGS_EXTERNAL = 0x430;
        public static int DEFAULT_SEARCH_RESULTS = 30;
        private int WRITE_EXTERNAL = 9999;
        private int NEW_WRITE_EXTERNAL = 0x428;
        private int MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL = 0x429;
        private Android.Support.V4.View.ViewPager pager = null;

        public static PowerManager.WakeLock CpuKeepAlive = null;
        public static Android.Net.Wifi.WifiManager.WifiLock WifiKeepAlive = null;
        public static System.Timers.Timer KeepAliveInactivityKillTimer = null;

        public static void KeepAliveInactivityKillTimerEllapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (CpuKeepAlive != null)
            {
                CpuKeepAlive.Release();
            }
            if(WifiKeepAlive != null)
            {
                WifiKeepAlive.Release();
            }
            KeepAliveInactivityKillTimer.Stop();
        }

        private ISharedPreferences sharedPreferences;
        private const string defaultMusicUri = "content://com.android.externalstorage.documents/tree/primary%3AMusic";
        protected override void OnCreate(Bundle savedInstanceState)
        {

            LogDebug("Main Activity On Create");
            


            try
            {
                if(CpuKeepAlive==null)
                {
                    CpuKeepAlive = ((PowerManager)this.GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Partial, "Seeker Download CPU_Keep_Alive");
                    CpuKeepAlive.SetReferenceCounted(false);
                }
                if(WifiKeepAlive==null)
                {
                    WifiKeepAlive = ((Android.Net.Wifi.WifiManager)this.GetSystemService(Context.WifiService)).CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Seeker Download Wifi_Keep_Alive");
                    WifiKeepAlive.SetReferenceCounted(false);
                }
            }
            catch(Exception e)
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

            //Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.my_toolbar);
            //SetSupportActionBar(myToolbar);
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);


            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);
            myToolbar.Title = "Home";
            myToolbar.InflateMenu(Resource.Menu.account_menu);
            SetSupportActionBar(myToolbar);
            myToolbar.InflateMenu(Resource.Menu.account_menu); //twice??




            System.Console.WriteLine("Testing.....");

            sharedPreferences = this.GetSharedPreferences("SoulSeekPrefs",0);

            if (TransfersFragment.transferItems == null)//bc our sharedPref string can be older than the transferItems
            {
                TransfersFragment.RestoreTransferItems(sharedPreferences);
            }

            //restoreSoulSeekState(savedInstanceState);

            LogDebug("Default Night Mode: " + AppCompatDelegate.DefaultNightMode); //-100 = night mode unspecified, default on my Pixel 2. also on api22 emulator it is -100.
                                                                                   //though setting it to -1 does not seem to recreate the activity or have any negative side effects..
            if(AppCompatDelegate.DefaultNightMode != SoulSeekState.DayNightMode)
            {
                AppCompatDelegate.DefaultNightMode = SoulSeekState.DayNightMode;
            }

            TabLayout tabs = (TabLayout)FindViewById(Resource.Id.tabs);
            
            pager = (Android.Support.V4.View.ViewPager)FindViewById(Resource.Id.pager);
            pager.PageSelected += Pager_PageSelected;
            TabsPagerAdapter adapter = new TabsPagerAdapter(SupportFragmentManager);
            
            tabs.TabSelected += Tabs_TabSelected;
            pager.Adapter = adapter;
            pager.AddOnPageChangeListener(new OnPageChangeLister1());
            //tabs.SetupWithViewPager(pager);

            if(Intent!=null)
            {
                if(Intent.GetIntExtra(DownloadForegroundService.FromTransferString, -1)==2)
                {
                    pager.SetCurrentItem(2,false);
                }
                else if(Intent.GetIntExtra(UserListActivity.IntentUserGoToBrowse, -1)==3)
                {
                    pager.SetCurrentItem(3,false);
                }
                else if(Intent.GetIntExtra(UserListActivity.IntentUserGoToSearch, -1)==1)
                {
                    //var navigator = SoulSeekState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                    //navigator.NavigationItemReselected += Navigator_NavigationItemReselected;
                    //navigator.NavigationItemSelected += Navigator_NavigationItemSelected;
                    //navigator.ViewAttachedToWindow += Navigator_ViewAttachedToWindow;
                    pager.SetCurrentItem(1,false);
                }
                else if(Intent.GetIntExtra(UserListActivity.IntentSearchRoom, -1)==1)
                {
                    pager.SetCurrentItem(1, false);
                }
                else if(Intent.GetIntExtra(WishlistController.FromWishlistString,-1)==1)
                {
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
                            if(SearchFragment.Instance?.IsResumed ?? false)
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
                else if (Intent.GetIntExtra(SettingsActivity.FromBrowseSelf, -1) == 3)
                {
                    MainActivity.LogInfoFirebase("from browse self");
                    pager.SetCurrentItem(3, false);
                }
            }


            SoulSeekState.MainActivityRef = this;
            SoulSeekState.ActiveActivityRef = this;

            //if we have all the conditions to share, then set sharing up.
            if (MeetsSharingConditions() && !SoulSeekState.IsParsing && (SoulSeekState.SharedFileCache == null || !SoulSeekState.SharedFileCache.SuccessfullyInitialized))
            {
                Action setUpSharedFileCache = new Action(() => {
                    LogDebug("We meet sharing conditions, lets set up the sharedFileCache for 1st time.");
                    try
                    {
                        DocumentFile docFile = null;
                        if(SoulSeekState.PreOpenDocumentTree())
                        {
                            docFile = DocumentFile.FromFile(new Java.IO.File(Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri).Path));
                        }
                        else
                        {
                            docFile = DocumentFile.FromTreeUri(this, Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri));
                        }
                        SoulSeekState.MainActivityRef.InitializeDatabase(docFile, true);
                    }
                    catch (Exception e)
                    {   
                        LogDebug("Error setting up sharedFileCache for 1st time." + e.Message + e.StackTrace);
                        SoulSeekState.UploadDataDirectoryUri = null;
                        SetUnsetSharingBasedOnConditions(false);
                        MainActivity.LogFirebase("MainActivity error parsing: " + e.Message + "  " + e.StackTrace);
                        this.RunOnUiThread(new Action(() =>
                        {
                            Toast.MakeText(this, this.GetString(Resource.String.error_sharing), ToastLength.Long).Show();
                        }));
                    }
                    if(SoulSeekState.SharedFileCache.SuccessfullyInitialized)
                    {
                        LogDebug("database full initialized.");
                        this.RunOnUiThread(new Action(() =>
                        {
                            Toast.MakeText(this, this.GetString(Resource.String.success_sharing), ToastLength.Short).Show();
                        }));
                        try
                        {
                            //setup soulseek client with handlers if all conditions met
                            SetUnsetSharingBasedOnConditions(false);
                        }
                        catch(Exception e)
                        {
                            MainActivity.LogFirebase("MainActivity error setting handlers: " + e.Message + "  " + e.StackTrace);
                        }
                    }
                });
                System.Threading.ThreadPool.QueueUserWorkItem((object o) => {setUpSharedFileCache(); });
                
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
            SoulseekClient.ClearDownloadAddedInternalHandler(this);
            SoulseekClient.DownloadAddedRemovedInternal += SoulseekClient_DownloadAddedRemovedInternal;


            SoulSeekState.SharedPreferences = sharedPreferences;
            SoulSeekState.MainActivityRef = this;
            SoulSeekState.ActiveActivityRef = this;

            UpdateForScreenSize();




            if (SoulSeekState.UseLegacyStorage())
            {
                if(ContextCompat.CheckSelfPermission(this,Manifest.Permission.WriteExternalStorage)==Android.Content.PM.Permission.Denied)
                {
                    ActivityCompat.RequestPermissions(this,new string[] {Manifest.Permission.WriteExternalStorage }, WRITE_EXTERNAL);
                }
                //file picker with legacy case
                if(SoulSeekState.SaveDataDirectoryUri!=null && SoulSeekState.SaveDataDirectoryUri.ToString() != "")
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
                        if (SoulSeekState.PreOpenDocumentTree())
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

                            //DocumentFile diagF = DocumentFile.FromTreeUri(this, chosenUri);
                            //var files = diagF.ListFiles();
                            //int cnt = files.Count();
                            //bool exists = DocumentFile.FromTreeUri(this, chosenUri).Exists();  
                            canWrite = DocumentFile.FromTreeUri(this, chosenUri).CanWrite();
                        }
                    }
                    catch (Exception e)
                    {
                        if (chosenUri != null)
                        {
                            LogFirebase("legacy DocumentFile.FromTreeUri failed with URI: " + chosenUri.ToString() + " " + e.Message);
                        }
                        else
                        {
                            LogFirebase("legacy DocumentFile.FromTreeUri failed with null URI");
                        }
                    }
                    if(canWrite)
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
                        MainActivity.LogFirebase("cannot write" + chosenUri?.ToString()??"null");
                    }
                }

                //now for incomplete
                if (SoulSeekState.ManualIncompleteDataDirectoryUri != null && SoulSeekState.ManualIncompleteDataDirectoryUri.ToString() != "")
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
                        if (SoulSeekState.PreOpenDocumentTree())
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

                            //DocumentFile diagF = DocumentFile.FromTreeUri(this, chosenUri);
                            //var files = diagF.ListFiles();
                            //int cnt = files.Count();
                            //bool exists = DocumentFile.FromTreeUri(this, chosenUri).Exists();  
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
                if(SoulSeekState.SaveDataDirectoryUri == null || SoulSeekState.SaveDataDirectoryUri.ToString() == "")
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
                    if(SoulSeekState.PreOpenDocumentTree()) //this will never get hit..
                    {
                        canWrite = DocumentFile.FromFile(new Java.IO.File(res.Path)).CanWrite();
                    }
                    else
                    {
                        canWrite = DocumentFile.FromTreeUri(this,res).CanWrite();
                    }
                }
                catch(Exception e)
                {
                    if(res!=null)
                    {
                        LogFirebase("DocumentFile.FromTreeUri failed with URI: " + res.ToString() + " " + e.Message);
                    }
                    else
                    {
                        LogFirebase("DocumentFile.FromTreeUri failed with null URI");
                    }
                }
                //if (DocumentFile.FromTreeUri(this, Uri.TryCreate("",UriKind.Absolute)))
                if(!canWrite)
                {

                    var b = new Android.App.AlertDialog.Builder(this);
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
                        catch(Exception ex)
                        {
                            if(ex.Message.Contains("No Activity found to handle Intent"))
                            {
                                Toast.MakeText(this, this.GetString(Resource.String.error_no_file_manager_dir), ToastLength.Long).Show();
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
                    SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, res);
                }

                //for incomplete case
                Android.Net.Uri incompleteRes = null; //var y = MediaStore.Audio.Media.ExternalContentUri.ToString();
                if (SoulSeekState.ManualIncompleteDataDirectoryUri != null && SoulSeekState.ManualIncompleteDataDirectoryUri.ToString() != "")
                {
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    incompleteRes = Android.Net.Uri.Parse(SoulSeekState.ManualIncompleteDataDirectoryUri);
                }
                bool canWriteIncomplete = false;
                try
                {
                    //a phone failed 4 times with //POCO X3 Pro
                    //Android 11(SDK 30)
                    //Caused by: java.lang.IllegalArgumentException: 
                    //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                    //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                    if (SoulSeekState.PreOpenDocumentTree()) //this will never get hit..
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
                if(canWriteIncomplete)
                {
                    SoulSeekState.RootIncompleteDocumentFile = DocumentFile.FromTreeUri(this, incompleteRes);
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
            //    DocumentFile f = root.FindFile("BeerNecessities" + "_dir_response");

            //    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(f.Uri);
            //    BrowseResponse br = null;

            //    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //    br = formatter.Deserialize(stream) as BrowseResponse;

            ////var br = SoulSeekState.SoulseekClient.BrowseAsync("mzawk");
            ////br.Wait();

            //    DownloadDialog.CreateTree(br,false, null, null, "BeerNecessities",out _);

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

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.Intent = intent;
            if (Intent.GetIntExtra(WishlistController.FromWishlistString, -1) == 1)
            {
                MainActivity.LogInfoFirebase("is null: " + (SearchFragment.Instance?.Activity == null || (SearchFragment.Instance?.IsResumed ?? false)).ToString());
                MainActivity.LogInfoFirebase("from wishlist clicked");
                int currentPage = pager.CurrentItem;
                int tabID = Intent.GetIntExtra(WishlistController.FromWishlistStringID, int.MaxValue);
                if (currentPage==1)
                {
                    if(tabID==int.MaxValue)
                    {
                        LogFirebase("tabID == int.MaxValue");
                    }
                    else if(!SearchTabHelper.SearchTabCollection.ContainsKey(tabID))
                    {
                        Toast.MakeText(this,this.GetString(Resource.String.wishlist_tab_error),ToastLength.Long).Show();
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
            else if(Intent.GetIntExtra(SettingsActivity.FromBrowseSelf, -1) == 3)
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

        private void Navigator_NavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Navigator_NavigationItemReselected(object sender, BottomNavigationView.NavigationItemReselectedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        public static void GetDownloadPlaceInQueue(string username, string fullFileName, Func<TransferItem, object> actionOnComplete = null)
        {

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.MainActivityRef, out t))
                {
                        t.ContinueWith(new Action<Task>((Task t) =>
                        {
                            if (t.IsFaulted)
                            {
                                SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                                {
                                    if (SoulSeekState.MainActivityRef != null)
                                    {
                                        Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show();
                                    }
                                    else
                                    {
                                        Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show();
                                    }
                        
                                });
                                return;
                            }
                            SoulSeekState.MainActivityRef.RunOnUiThread(() => { GetDownloadPlaceInQueueLogic(username, fullFileName, actionOnComplete); });
                        }));
                }
            }
            else
            {
                GetDownloadPlaceInQueueLogic(username, fullFileName, actionOnComplete);
            }
        }

        private static void GetDownloadPlaceInQueueLogic(string username, string fullFileName, Func<TransferItem,object> actionOnComplete = null)
        {

            Action<Task<int>> updateTask = new Action<Task<int>>(
                (Task<int> t) =>
                {
                    if (t.IsFaulted)
                    {
                        LogFirebase("GetDownloadPlaceInQueue" + t.Exception.ToString());
                    }
                    else
                    {
                        //update the transferItem array
                        TransferItem relevantItem = TransfersFragment.GetTransferItemByFileName(fullFileName, out int _);
                        if (relevantItem == null)
                        {
                            return;
                        }
                        else
                        {
                            if (t.Result > 0)
                            {
                                relevantItem.Queued = true;
                                relevantItem.QueueLength = t.Result;
                            }
                            else
                            {
                                relevantItem.Queued = false;
                                relevantItem.QueueLength = 0;
                            }
                        }
                        
                        if (actionOnComplete != null)
                        {
                            SoulSeekState.MainActivityRef?.RunOnUiThread(() => { actionOnComplete(relevantItem); });
                        }
                        else
                        {
                            TransferItemQueueUpdated?.Invoke(null, relevantItem);
                        }

                    }
                }
            );

            Task<int> getDownloadPlace = null;
            try
            {
                getDownloadPlace = SoulSeekState.SoulseekClient.GetDownloadPlaceInQueueAsync(username,fullFileName);
            }
            catch(TransferNotFoundException)
            {
                //it is not downloading... therefore retry the download...
                TransferItem item1 = TransfersFragment.GetTransferItemByFileName(fullFileName, out int _);
                //TransferItem item1 = transferItems[info.Position];  
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    item1.QueueLength = 0;
                    Android.Net.Uri incompleteUri = null;
                    Task task = DownloadDialog.DownloadFileAsync(item1.Username, item1.FullFilename, item1.Size, cancellationTokenSource);
                    task.ContinueWith(DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username, item1.FullFilename, item1.Size, task, cancellationTokenSource, item1.QueueLength,0))));
                }
                catch (DuplicateTransferException)
                {
                    //happens due to button mashing...
                    return;
                }
                catch (System.Exception error)
                {
                    Action a = new Action(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.error_) + error.Message, ToastLength.Long); });
                    if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                    {

                    }
                    else
                    {
                        MainActivity.LogFirebase(error.Message + " OnContextItemSelected");
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(a);
                    return; //otherwise null ref with task!
                }
                //TODO: THIS OCCURS TO SOON, ITS NOT gaurentted for the transfer to be in downloads yet...
                try
                {
                    getDownloadPlace = SoulSeekState.SoulseekClient.GetDownloadPlaceInQueueAsync(username, fullFileName);
                    getDownloadPlace.ContinueWith(updateTask);
                }
                catch(Exception e)
                {
                    LogFirebase("you likely called getdownloadplaceinqueueasync too soon..." + e.Message);
                }
                return;



            }
            catch(System.Exception e)
            {
                LogFirebase("GetDownloadPlaceInQueue" + e.Message);
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
                case Resource.Id.about_action:
                    var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
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


        public const string UPLOADS_CHANNEL_ID =  "upload channel ID";
        public const string UPLOADS_CHANNEL_NAME = "Upload Notifications";

        public static Notification CreateUploadNotification(Context context, String username, List<String> directories, int numFiles)
        {
            string fileS = numFiles == 1 ? SoulSeekState.ActiveActivityRef.GetString(Resource.String.file) : SoulSeekState.ActiveActivityRef.GetString(Resource.String.files);
            string titleText = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.upload_f_string),numFiles, fileS, username);
            string directoryString = string.Empty;
            if(directories.Count==1)
            {
                directoryString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.from_directory) + ": " + directories[0];
            }
            else
            {
                directoryString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.from_directories) + ": " + directories[0];
                for(int i=0;i<directories.Count;i++)
                {
                    if(i==0)
                    {
                        continue;
                    }
                    directoryString += ", " + directories[i];
                }
            }
            string contextText = directoryString;
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            notifIntent.PutExtra("From Upload", 2);
            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, 0, notifIntent, 0);
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

        /// <summary>
        /// This is for adding new users...
        /// </summary>
        /// <returns>true if user was already added</returns>
        public static bool UserListAddUser(UserData userData)
        {
            lock(SoulSeekState.UserList)
            {
                bool found = false;
                foreach(UserListItem item in SoulSeekState.UserList)
                {
                    if(item.Username == userData.Username)
                    {
                        found = true;
                        if(userData!=null)
                        {
                            item.UserData = userData;
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
                    if(SeekerApplication.IsUserInIgnoreList(userData.Username))
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
        /// This is for adding new users...
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
                if(itemToRemove==null)
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
            if(SeekerApplication.IsUserInIgnoreList(username))
            {
                return Task.FromResult(new BrowseResponse(Enumerable.Empty<Directory>()));
            }
            return Task.FromResult(SoulSeekState.SharedFileCache.BrowseResponse);
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
            Tuple<string, string> fullDirUri = SoulSeekState.SharedFileCache.FriendlyDirNameToUriMapping.Where((Tuple<string,string> t) => {return t.Item1.EndsWith(directory); }).FirstOrDefault();
            if(fullDirUri==null)
            {
                //could not find...
            }
            DocumentFile fullDir = null;
            if (SoulSeekState.PreOpenDocumentTree())
            {
                fullDir = DocumentFile.FromFile(new Java.IO.File(Android.Net.Uri.Parse(fullDirUri.Item2).Path));
            }
            else
            {
                fullDir = DocumentFile.FromTreeUri(SoulSeekState.MainActivityRef, Android.Net.Uri.Parse(fullDirUri.Item2));
            }
            //Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri).Path
            var slskDir = SlskDirFromDocumentFile(fullDir, Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri).Path, true);
            slskDir = new Directory(directory,slskDir.Files);
            return Task.FromResult(slskDir);
        }

        public static void TurnOnSharing()
        {
            SoulSeekState.SoulseekClient.Options.SetSharedHandlers(BrowseResponseResolver,SearchResponseResolver, DirectoryContentsResponseResolver, EnqueueDownloadAction);
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
        public static void SetUnsetSharingBasedOnConditions(bool informServerOfChangeIfThereIsAChange, bool force=false)
        {
            bool wasShared = SoulSeekState.SoulseekClient.Options.SearchResponseResolver == null; //when settings gets recreated can get nullref here.
            if (MeetsSharingConditions())
            {
                TurnOnSharing();
                if(!wasShared||force)
                {
                    InformServerOfSharedFiles();
                }
            }
            else
            {
                TurnOffSharing();
                if (wasShared)
                {
                    InformServerOfSharedFiles();
                }
            }
        }

        public static bool MeetsSharingConditions()
        {
            return (SoulSeekState.SharingOn && SoulSeekState.UploadDataDirectoryUri != null && SoulSeekState.UploadDataDirectoryUri != string.Empty);
        }

        public static Tuple<SharingIcons, string> GetSharingMessageAndIcon()
        {
            if(MeetsSharingConditions())
            {
                //try to parse this into a path: SoulSeekState.ShareDataDirectoryUri
                return new Tuple<SharingIcons,string>(SharingIcons.On,SoulSeekState.ActiveActivityRef.GetString(Resource.String.success_sharing));
            }
            else if(!SoulSeekState.SharingOn)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Off, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_off));
            }
            else if(SoulSeekState.IsParsing)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_currently_parsing));
            }
            else if (SoulSeekState.FailedShareParse)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_failure_parsing));
            }
            else if (SoulSeekState.UploadDataDirectoryUri == null || SoulSeekState.UploadDataDirectoryUri == string.Empty)
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_share_not_set));
            }
            else
            {
                return new Tuple<SharingIcons, string>(SharingIcons.Error, SoulSeekState.ActiveActivityRef.GetString(Resource.String.sharing_disabled_error));
            }
        }

        public void SetUpLoginContinueWith(Task t)
        {
            if(t==null)
            {
                return;
            }
            if(MeetsSharingConditions())
            {
                
                Action<Task> getAndSetLoggedInInfoAction = new Action<Task>((Task t) => { 
                    //we want to 
                    //UpdateStatus ??
                    //inform server if we are sharing..
                    //get our upload speed..
                    if(t.Status==TaskStatus.Faulted || t.IsFaulted || t.IsCanceled)
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


        public static int DL_COUNT = -1; // a hack see below

        //it works in the case of successfully finished, cancellation token used, etc.
        private void SoulseekClient_DownloadAddedRemovedInternal(object sender, SoulseekClient.DownloadAddedRemovedInternalEventArgs e)
        {
            //even with them all going onto same thread here you will still have (ex) a 16 count coming in after a 0 count sometimes.
            //SoulSeekState.MainActivityRef.RunOnUiThread(()=>
            //{
            MainActivity.LogDebug("SoulseekClient_DownloadAddedRemovedInternal with count:" + e.Count);
            MainActivity.LogDebug("the thread is: " + System.Threading.Thread.CurrentThread.ManagedThreadId);

            bool cancelAndClear = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - SoulSeekState.CancelAndClearAllWasPressedDebouncer)<750;
            MainActivity.LogDebug("SoulseekClient_DownloadAddedRemovedInternal cancel and clear:" + cancelAndClear);
            if (e.Count == 0|| cancelAndClear)
            {
                DL_COUNT = -1;
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                MainActivity.LogDebug("Stop Service");
                this.StopService(downloadServiceIntent);
                SoulSeekState.DownloadKeepAliveServiceRunning = false;
            }
            else if(!SoulSeekState.DownloadKeepAliveServiceRunning)
            {
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    bool isForeground = false;
                    try
                    {
                        if (this.Lifecycle?.CurrentState !=null)
                        {
                            isForeground = this.Lifecycle.CurrentState.IsAtLeast(Android.Arch.Lifecycle.Lifecycle.State.Resumed);
                        }
                    }
                    catch
                    {
                        LogFirebase("Exception thrown while checking lifecycle");
                    }

                    //LogDebug("IsForeground: " + isForeground + " current state: " + this.Lifecycle.CurrentState.ToString()); //REMOVE THIS!!!
                    if (isForeground)
                    {
                        this.StartService(downloadServiceIntent); //this will throw if the app is in background.
                    }
                    else
                    {
                        //only do this if we absolutely must
                        this.StartForegroundService(downloadServiceIntent);//added in 26
                    }
                }
                else
                {
                    this.StartService(downloadServiceIntent); //this will throw if the app is in background.
                }
                SoulSeekState.DownloadKeepAliveServiceRunning = true;
            }
            else if(SoulSeekState.DownloadKeepAliveServiceRunning && e.Count !=0)
            {
                DL_COUNT = e.Count;
                //for two downloads, this notification will go up before the service is started...

                //requires run on ui thread? NOPE
                string msg = string.Empty;
                if(e.Count==1)
                {
                    msg = string.Format(DownloadForegroundService.SingularTransfersRemaining,e.Count);
                }
                else
                {
                    msg = string.Format(DownloadForegroundService.PluralTransfersRemaining, e.Count);
                }
                var notif = DownloadForegroundService.CreateNotification(this, msg);
                NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Notify(DownloadForegroundService.NOTIF_ID, notif);
            }
            //});
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
                var pager = (Android.Support.V4.View.ViewPager)FindViewById(Resource.Id.pager);
                if (pager.CurrentItem == 3) //browse tab
                {
                    relevant = ((pager.Adapter as TabsPagerAdapter).GetItem(3) as BrowseFragment).BackButton();
                }
            }
            catch(Exception e)
            {
                //During Back Button: Attempt to invoke virtual method 'java.lang.Object android.content.Context.getSystemService(java.lang.String)' on a null object reference
                MainActivity.LogFirebase("During Back Button: " + e.Message);
            }
            if(!relevant)
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
            if(e?.Message!=null)
            {
                if(e.Message.Contains("Operation timed out"))
                {
                    //this happens to me all the time and it is literally fine
                    return;
                }
            }
            MainActivity.LogFirebase(e.Message);
        }

        public static bool ShowMessageAndCreateReconnectTask(Context c, out Task connectTask)
        {
            if(c==null)
            {
                c = SoulSeekState.MainActivityRef;
            }
            if(Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread()) //tested..
            {
                Toast tst = Toast.MakeText(c,c.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                tst.Show();
            }
            else
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(
                    () => {
                        Toast tst = Toast.MakeText(c, c.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                        tst.Show();
                    });
            }
            //if we are still not connected then creating the task will throw. 
            //also if the async part of the task fails we will get task.faulted.
            try
            {
                connectTask = SoulSeekState.SoulseekClient.ConnectAsync(SoulSeekState.Username, SoulSeekState.Password);
                return true;
            }
            catch
            {
                Toast tst2 = Toast.MakeText(c, c.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                tst2.Show();
            }
            connectTask = null;
            return false;
        }

        public static bool CurrentlyLoggedInButDisconnectedState()
        {
            return (SoulSeekState.currentlyLoggedIn && 
                (SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnected) || SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnecting)));
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

        public void RecreateFragment(Android.Support.V4.App.Fragment f)
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
            if(SeekerApplication.IsUserInIgnoreList(username))
            {
                return defaultResponse;
            }
            // some bots and perhaps users search for very short terms.  only respond to queries >= 3 characters.  sorry, U2 fans.
            if (query.Query.Length < 5)
            {
                return defaultResponse;
            }

            if(SoulSeekState.Username==null || SoulSeekState.Username == string.Empty || SoulSeekState.SharedFileCache==null)
            {
                return defaultResponse;
            }

            var results = SoulSeekState.SharedFileCache.Search(query);

            if (results.Any())
            {
                //Console.WriteLine($"[SENDING SEARCH RESULTS]: {results.Count()} records to {username} for query {query.SearchText}");
                int ourUploadSpeed = 1024 * 256;
                if(SoulSeekState.UploadSpeed>0)
                {
                    ourUploadSpeed = SoulSeekState.UploadSpeed;
                }
                return Task.FromResult(new SearchResponse(
                    SoulSeekState.Username,
                    token,
                    freeUploadSlots: 1,
                    uploadSpeed: ourUploadSpeed,
                    queueLength: 0,
                    fileList: results));
            }

            // if no results, either return null or an instance of SearchResponse with a fileList of length 0
            // in either case, no response will be sent to the requestor.
            return Task.FromResult<SearchResponse>(null);
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
            if(SeekerApplication.IsUserInIgnoreList(username))
            {
                return Task.CompletedTask;
            }
            
            //if a user tries to download a file from our browseResponse then their filename will be
            //  "Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\(2009.09.23) Sufjan Stevens - Live from Castaways\\(2009.09.23) Sufjan Stevens - Live from Castaways\\09 Between Songs 4.mp3" 
            //so check if it contains the uploadDataDirectoryUri
            string keyFilename = filename;
            string uploadDirfolderName = Helpers.GetFileNameFromFile(Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri).Path.Replace(@"/",@"\")); //this will actaully get the last folder name..
            if(filename.Contains(uploadDirfolderName))
            {
                string newFolderName = Helpers.GetFolderNameFromFile(filename);
                string newFileName = Helpers.GetFileNameFromFile(filename);
                keyFilename = newFolderName + @"\" + newFileName;
            }

            //the filename is basically "the key"
            _ = endpoint;
            string errorMsg = null;
            Tuple<string, string, long> ourFileInfo = SoulSeekState.SharedFileCache.GetFullInfoFromSearchableName(keyFilename, Android.Net.Uri.Parse(SoulSeekState.UploadDataDirectoryUri).Path.Replace(@"/", @"\"), out errorMsg);//SoulSeekState.SharedFileCache.FullInfo.Where((Tuple<string,string,long> fullInfoTuple) => {return fullInfoTuple.Item1 == keyFilename; }).FirstOrDefault(); //make this a method call GetFullInfo and check Aux dict
            if (ourFileInfo==null)
            {
                LogFirebase("ourFileInfo is null: " + ourFileInfo + " " + errorMsg);
                throw new DownloadEnqueueException($"File not found.");
            }

            DocumentFile ourFile = null;
            if (SoulSeekState.PreOpenDocumentTree())
            {
                ourFile = DocumentFile.FromFile(new Java.IO.File(Android.Net.Uri.Parse(ourFileInfo.Item2).Path));
            }
            else
            {
                ourFile = DocumentFile.FromTreeUri(SoulSeekState.MainActivityRef, Android.Net.Uri.Parse(ourFileInfo.Item2));
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

            // accept all download requests, and begin the upload immediately.
            // normally there would be an internal queue, and uploads would be handled separately.
            Task.Run(async () =>
            {
                using var stream = SoulSeekState.MainActivityRef.ContentResolver.OpenInputStream(ourFile.Uri); //outputstream.CanRead is false...
                //using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
                await SoulSeekState.SoulseekClient.UploadAsync(username, filename, ourFile.Length(), stream, options: null, cancellationToken: cts.Token); //THE FILENAME THAT YOU PASS INTO HERE MUST MATCH EXACTLY
                //ELSE THE CLIENT WILL REJECT IT.  //MUST MATCH EXACTLY THE ONE THAT WAS REQUESTED THAT IS..
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
            if (NEW_WRITE_EXTERNAL == requestCode)
            {
                if(resultCode == Result.Ok)
                {
                    var x = data.Data;
                    SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this,data.Data);
                    SoulSeekState.SaveDataDirectoryUri = data.Data.ToString();
                    this.ContentResolver.TakePersistableUriPermission(data.Data,ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                }
                else
                {
                    Action showDirectoryButton = new Action(() => {
                        ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_error));
                        AddLoggedInLayout(StaticHacks.LoginFragment.View);
                        if(!SoulSeekState.currentlyLoggedIn)
                        {
                            MainActivity.BackToLogInLayout(StaticHacks.LoginFragment.View, (StaticHacks.LoginFragment as LoginFragment).LogInClick);
                        }
                        if(StaticHacks.LoginFragment.View == null)//this can happen...
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
                    if(OnUIthread())
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
            else if(MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    var x = data.Data;
                    SoulSeekState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
                    SoulSeekState.SaveDataDirectoryUri = data.Data.ToString();
                    this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    //hide the button
                    Action hideButton = new Action(() => {
                        //ToastUI("Must select a directory to place downloads. This will be needed to place downloaded files.");
                        Button bttn = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.mustSelectDirectory);
                        bttn.Visibility = ViewStates.Gone;
                        //bttn.Click += MustSelectDirectoryClick;
                    });
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
                    Action reiterate = new Action(() => {
                        ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_error));
                        //Button bttn = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.mustSelectDirectory);
                        //bttn.Visibility = ViewStates.Visible;
                        //bttn.Click += MustSelectDirectoryClick;
                    });
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
            else if(SETTINGS_EXTERNAL == requestCode)
            {
                if(resultCode == Result.Ok)
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
            if (SoulSeekState.SaveDataDirectoryUri == null || SoulSeekState.SaveDataDirectoryUri.ToString() == "")
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
            }
            catch(Exception ex)
            {
                if (ex.Message.Contains("No Activity found to handle Intent"))
                {
                    Toast.MakeText(this, SoulSeekState.MainActivityRef.GetString(Resource.String.error_no_file_manager_dir), ToastLength.Long).Show();
                }
                else
                {
                    throw ex;
                }
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

        public static void SoulSeekState_DownloadAdded(object sender, DownloadAddedEventArgs e)
        {
            //once task completes, write to disk
            Action<Task> continuationActionSaveFile = DownloadContinuationActionUI(e);
            e.dlInfo.downloadTask.ContinueWith(continuationActionSaveFile);


        }

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
                Action action = null;
                if (task.IsCanceled)
                {
                    MainActivity.LogDebug((DateTimeOffset.Now.ToUnixTimeMilliseconds() - SoulSeekState.TaskWasCancelledToastDebouncer).ToString());
                    if((DateTimeOffset.Now.ToUnixTimeMilliseconds()-SoulSeekState.TaskWasCancelledToastDebouncer)>1000)
                    {
                        SoulSeekState.TaskWasCancelledToastDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        //action = () => { ToastUI("The task was cancelled."); };
                        //this.RunOnUiThread(action);
                    }

                    if(e.dlInfo.TransferItemReference.CancelAndRetryFlag) //if we pressed "Retry Download" and it was in progress so we first had to cancel...
                    {
                        e.dlInfo.TransferItemReference.CancelAndRetryFlag = false;
                        try
                        {
                            //retry download.
                            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                            Android.Net.Uri incompleteUri = null;
                            Task retryTask = DownloadDialog.DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, cancellationTokenSource);
                            retryTask.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, retryTask, cancellationTokenSource, e.dlInfo.QueueLength, 0, task.Exception))));
                        }
                        catch (System.Exception e)
                        {
                            MainActivity.LogFirebase("cancel and retry creation failed: " + e.Message + e.StackTrace);
                        }
                    }

                    return;
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    if (task.Exception.InnerException is System.TimeoutException)
                    {
                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.timeout_peer)); };
                    }
                    else if (task.Exception.InnerException is Soulseek.TransferException)
                    {
                        action = () => { ToastUI(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_establish_connection_to_peer),e.dlInfo.username)); };
                    }
                    else if (task.Exception.InnerException is Soulseek.UserOfflineException)
                    {
                        action = () => { ToastUI(task.Exception.InnerException.Message); };
                    }
                    else if (task.Exception.InnerException is Soulseek.TransferRejectedException)
                    {
                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.transfer_rejected)); };
                    }
                    else if (task.Exception.InnerException is Soulseek.SoulseekClientException &&
                            task.Exception.InnerException.Message != null &&
                            task.Exception.InnerException.Message.Contains("Failed to establish a direct or indirect message connection"))
                    {
                        LogDebug("Task Exception: " + task.Exception.InnerException.Message);
                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_establish_direct_or_indirect)); };
                    }
                    else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("read error: remote connection closed"))
                    {
                        MainActivity.LogFirebase("read error: remote connection closed");
                        LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.remote_conn_closed)); };
                    }
                    else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("network subsystem is down"))
                    {
                        MainActivity.LogFirebase("Network Subsystem is Down");
                        LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_down)); };
                    }
                    else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("reported as failed by"))
                    {
                        MainActivity.LogFirebase("Reported as failed by uploader");
                        LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.reported_as_failed)); };
                    }
                    else if (task.Exception.InnerException.Message != null && task.Exception.InnerException.Message.ToLower().Contains("failed to establish a direct or indirect message connection"))
                    {
                        MainActivity.LogFirebase("failed to establish a direct or indirect message connection");
                        LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                        action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_establish_direct_or_indirect)); };
                    }
                    else
                    {
                        //the server connection task.Exception.InnerException.Message.Contains("The server connection was closed unexpectedly") //this seems to be retry able
                        //or task.Exception.InnerException.InnerException.Message.Contains("The server connection was closed unexpectedly""
                        //or task.Exception.InnerException.Message.Contains("Transfer failed: Read error: Object reference not set to an instance of an object
                        if (task.Exception != null && task.Exception.InnerException != null)
                        {
                            //I get a lot of null refs from task.Exception.InnerException.Message
                                MainActivity.LogFirebase("Unhandled task exception: " + task.Exception.InnerException.Message + task.Exception.InnerException.StackTrace);
                                LogDebug("Unhandled task exception: " + task.Exception.InnerException.Message);
                            if(task.Exception.InnerException.InnerException != null)
                            {
                                //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                MainActivity.LogFirebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);

                                if(task.Exception.InnerException.InnerException.Message.Contains("ENOSPC (No space left on device)"))
                                {
                                    action = () => { ToastUI(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_no_space)); };
                                }

                                //this is to help with the collection was modified
                                if (task.Exception.InnerException.InnerException.InnerException != null)
                                {
                                    MainActivity.LogInfoFirebase("InnerInnerException: " + task.Exception.InnerException.InnerException.Message + task.Exception.InnerException.InnerException.StackTrace);
                                    var innerInner = task.Exception.InnerException.InnerException.InnerException;
                                    //1.983 - Non-fatal Exception: java.lang.Throwable: InnerInnerException: Transfer failed: Read error: Object reference not set to an instance of an object  at Soulseek.SoulseekClient.DownloadToStreamAsync (System.String username, System.String filename, System.IO.Stream outputStream, System.Nullable`1[T] size, System.Int64 startOffset, System.Int32 token, Soulseek.TransferOptions options, System.Threading.CancellationToken cancellationToken) [0x00cc2] in <bda1848b50e64cd7b441e1edf9da2d38>:0 
                                    MainActivity.LogFirebase("Innerx3_Exception: " + innerInner.Message + innerInner.StackTrace);
                                    //this is to help with the collection was modified
                                }

                            }
                        }
                        else if(task.Exception != null)
                        {
                            MainActivity.LogFirebase("Unhandled task exception (little info): " + task.Exception.Message);
                            LogDebug("Unhandled task exception (little info):" + task.Exception.Message);
                        }


                        if(e.dlInfo.RetryCount==0 && SoulSeekState.AutoRetryDownload)
                        {
                            try
                            {
                                //retry download.
                                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                                Android.Net.Uri incompleteUri = null;
                                Task retryTask = DownloadDialog.DownloadFileAsync(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, cancellationTokenSource);
                                retryTask.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(e.dlInfo.username, e.dlInfo.fullFilename, e.dlInfo.Size, retryTask, cancellationTokenSource, e.dlInfo.QueueLength, 1, task.Exception))));
                            }
                            catch(System.Exception e)
                            {
                                MainActivity.LogFirebase("retry creation failed: " + e.Message + e.StackTrace);
                            }
                            return;
                        }
                        else
                        {
                            LogDebug("The task did not complete due to unhandled exception");
                            if(action==null)
                            {
                                action = () => { ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.error_unspecified)); };
                            }
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(action);
                            return;
                        }
                    }

                    if(e.dlInfo.RetryCount==1 && e.dlInfo.PreviousFailureException!=null)
                    {
                        LogFirebase("auto retry failed: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString() + "new exception: " + task.Exception?.InnerException?.Message?.ToString());
                    }

                    //Action action2 = () => { ToastUI(task.Exception.ToString());};
                    //this.RunOnUiThread(action2);
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(action);
                    //System.Console.WriteLine(task.Exception.ToString());
                    return;
                }

                if (e.dlInfo.RetryCount == 1 && e.dlInfo.PreviousFailureException != null)
                {
                    LogFirebase("auto retry succeeded: prev exception: " + e.dlInfo.PreviousFailureException.InnerException?.Message?.ToString());
                }

                if (!SoulSeekState.DisableDownloadToastNotification)
                {
                    action = () => { ToastUI(Helpers.GetFileNameFromFile(e.dlInfo.fullFilename) + " finished downloading"); };
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(action);
                }
                string finalUri = string.Empty;
                if(task is Task<byte[]> tbyte)
                {
                    string path = SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, tbyte.Result, null, null, true, out finalUri);
                    SaveFileToMediaStore(path);
                }
                else if(task is Task<Tuple<string, string>> tString)
                {
                    //move file...
                    string path = SaveToFile(e.dlInfo.fullFilename, e.dlInfo.username, null, Android.Net.Uri.Parse(tString.Result.Item1), Android.Net.Uri.Parse(tString.Result.Item2), false, out finalUri);
                    SaveFileToMediaStore(path);
                }
                else
                {
                    LogFirebase("Very bad. Task is not the right type.....");
                }
                e.dlInfo.TransferItemReference.FinalUri = finalUri;
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

        public static void ToastUI(string msg)
        {
            Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, ToastLength.Long).Show();
        }

        public static void ToastUI_short(string msg)
        {
            Toast.MakeText(SoulSeekState.ActiveActivityRef, msg, ToastLength.Short).Show();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool OnUIthread()
        {
            if(Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.M) //23
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
        public static void AddLoggedInLayout(View rootView=null)
        {
            View bttn = StaticHacks.RootView?.FindViewById<Button>(Resource.Id.buttonLogout);
            View bttnTryTwo = rootView?.FindViewById<Button>(Resource.Id.buttonLogout);
            bool bttnIsAttached = false;
            bool bttnTwoIsAttached = false;
            if(bttn != null && bttn.IsAttachedToWindow)
            {
                bttnIsAttached = true;
            }
            if(bttnTryTwo != null && bttnTryTwo.IsAttachedToWindow)
            {
                bttnTwoIsAttached = true;
            }

            if (!bttnIsAttached && !bttnTwoIsAttached && !SoulSeekState.currentlyLoggedIn)
            {
                //THIS MEANS THAT WE STILL HAVE THE LOGINFRAGMENT NOT THE LOGGEDIN FRAGMENT
                RelativeLayout relLayout = SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, rootView as ViewGroup, false) as RelativeLayout;
                relLayout.LayoutParameters = new ViewGroup.LayoutParams(rootView.LayoutParameters);
                var action1 = new Action(() => {
                    (rootView as RelativeLayout).AddView(SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, rootView as ViewGroup, false));
                });
                if(OnUIthread())
                {
                    action1();
                }
                else
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(action1);
                }
            }
        }

        public static void UpdateUIForLoggedIn(View rootView=null, EventHandler BttnClick=null, View cWelcome=null, View cbttn=null, View cLoading=null, EventHandler SettingClick=null)
        {
            var action = new Action(() => {
                //this is the case where it already has the loggedin fragment loaded.
                Button bttn = null;
                TextView welcome = null;
                TextView loading = null;
                EditText editText = null;
                EditText editText2 = null;
                TextView textView = null;
                Button buttonLogin = null;
                Button settings = null;
                View noAccount = null;
                try
                {
                    if (StaticHacks.RootView != null && StaticHacks.RootView.IsAttachedToWindow)
                    {
                        bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loading = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.loadingView);
                        editText = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.editText);
                        editText2 = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.editText2);
                        textView = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.textView);
                        buttonLogin = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogin);
                        noAccount = StaticHacks.RootView.FindViewById(Resource.Id.noAccount);
                        settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                    else
                    {
                        bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loading = rootView.FindViewById<TextView>(Resource.Id.loadingView);
                        editText = rootView.FindViewById<EditText>(Resource.Id.editText);
                        editText2 = rootView.FindViewById<EditText>(Resource.Id.editText2);
                        textView = rootView.FindViewById<TextView>(Resource.Id.textView);
                        buttonLogin = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
                        noAccount = rootView.FindViewById(Resource.Id.noAccount);
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
                    Android.Support.V4.View.ViewCompat.SetTranslationZ(bttn, 90);
                    bttn.Click -= BttnClick;
                    bttn.Click += BttnClick;
                    loading.Visibility = ViewStates.Gone;
                    welcome.Text = "Welcome, " + SoulSeekState.Username;
                }
                else if (cWelcome != null)
                {
                    cWelcome.Visibility = ViewStates.Visible;
                    cbttn.Visibility = ViewStates.Visible;
                    Android.Support.V4.View.ViewCompat.SetTranslationZ(cbttn, 90);
                    cLoading.Visibility = ViewStates.Gone;
                }
                else
                {
                    StaticHacks.UpdateUI = true;//if we arent ready rn then do it when we are..
                }
                if (editText != null)
                {
                    editText.Visibility = ViewStates.Gone;
                    editText2.Visibility = ViewStates.Gone;
                    textView.Visibility = ViewStates.Gone;
                    noAccount.Visibility = ViewStates.Gone;
                    buttonLogin.Visibility = ViewStates.Gone;
                    Android.Support.V4.View.ViewCompat.SetTranslationZ(buttonLogin, 0);
                }

            });
            if(OnUIthread())
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
        public static void BackToLogInLayout(View rootView, EventHandler LogInClick)
        {
            var action = new Action(() => {
                //this is the case where it already has the loggedin fragment loaded.
                Button bttn = null;
                TextView welcome = null;
                TextView loading = null;
                EditText editText = null;
                EditText editText2 = null;
                TextView textView = null;
                Button buttonLogin = null;
                View noAccountHelp = null;
                Button settings = null;
                MainActivity.LogDebug("BackToLogInLayout");
                try
                {
                    if (StaticHacks.RootView != null && StaticHacks.RootView.IsAttachedToWindow)
                    {
                        bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loading = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.loadingView);

                        //this is the case we have a bad SAVED user pass....
                        try
                        {
                            editText = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.editText);
                            editText2 = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.editText2);
                            textView = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.textView);
                            buttonLogin = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogin);
                            noAccountHelp = StaticHacks.RootView.FindViewById(Resource.Id.noAccount);
                            if (editText == null)
                            {
                                RelativeLayout relLayout = SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.login, StaticHacks.RootView as ViewGroup, false) as RelativeLayout;
                                relLayout.LayoutParameters = new ViewGroup.LayoutParams(StaticHacks.RootView.LayoutParameters);
                                //var action1 = new Action(() => {
                                    (StaticHacks.RootView as RelativeLayout).AddView(SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.login, StaticHacks.RootView as ViewGroup, false));
                                //});
                            }
                            editText = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.editText);
                            editText2 = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.editText2);
                            textView = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.textView);
                            settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                            buttonLogin = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogin);
                            noAccountHelp = StaticHacks.RootView.FindViewById(Resource.Id.noAccount);
                            buttonLogin.Click -= LogInClick;
                            buttonLogin.Click += LogInClick;
                        }
                        catch
                        {

                        }

                    }
                    else
                    {
                        bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loading = rootView.FindViewById<TextView>(Resource.Id.loadingView);
                        editText = rootView.FindViewById<EditText>(Resource.Id.editText);
                        editText2 = rootView.FindViewById<EditText>(Resource.Id.editText2);
                        textView = rootView.FindViewById<TextView>(Resource.Id.textView);
                        buttonLogin = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
                        noAccountHelp = rootView.FindViewById(Resource.Id.noAccount);
                        settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                }
                catch
                {

                }
                if (editText != null)
                {
                    editText.Visibility = ViewStates.Visible;
                    editText2.Visibility = ViewStates.Visible;
                    textView.Visibility = ViewStates.Visible;
                    buttonLogin.Visibility = ViewStates.Visible;
                    noAccountHelp.Visibility = ViewStates.Visible;
                    Android.Support.V4.View.ViewCompat.SetTranslationZ(buttonLogin, 90);
                    
                    if(loading==null)
                    {
                        MainActivity.AddLoggedInLayout(rootView);
                        if(rootView!=null)
                        {
                            bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                            welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                            loading = rootView.FindViewById<TextView>(Resource.Id.loadingView);
                            settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                        }
                        if(loading==null && StaticHacks.RootView!=null)
                        {
                            bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                            welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                            loading = rootView.FindViewById<TextView>(Resource.Id.loadingView);
                            settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                        }
                    }
                    loading.Visibility = ViewStates.Gone; //can get nullref here!!! (at least before the .AddLoggedInLayout code..
                    welcome.Visibility = ViewStates.Gone;
                    settings.Visibility = ViewStates.Gone;
                    bttn.Visibility = ViewStates.Gone;
                    Android.Support.V4.View.ViewCompat.SetTranslationZ(bttn, 0);


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
            var action = new Action(() => {
            //this is the case where it already has the loggedin fragment loaded.
            Button bttn = null;
            TextView welcome = null;
            TextView loading = null;
            EditText editText = null;
            EditText editText2 = null;
            TextView textView = null;
            Button buttonLogin = null;
                View noAccount = null;
                Button settings = null;
            try
            {
                if (StaticHacks.RootView != null)
                {
                    bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                    welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                    loading = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.loadingView);
                }
                else
                {
                    bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                    settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                    welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                    loading = rootView.FindViewById<TextView>(Resource.Id.loadingView);
                    editText = rootView.FindViewById<EditText>(Resource.Id.editText);
                    editText2 = rootView.FindViewById<EditText>(Resource.Id.editText2);
                    textView = rootView.FindViewById<TextView>(Resource.Id.textView);
                    buttonLogin = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
                        noAccount = rootView.FindViewById(Resource.Id.noAccount);
                }
            }
            catch
            {

            }
            if (editText != null)
            {
                editText.Visibility = ViewStates.Gone;
                editText2.Visibility = ViewStates.Gone;
                textView.Visibility = ViewStates.Gone;
                buttonLogin.Visibility = ViewStates.Gone;
                    noAccount.Visibility = ViewStates.Gone;
                Android.Support.V4.View.ViewCompat.SetTranslationZ(buttonLogin, 0);
                loading.Visibility = ViewStates.Visible;
                welcome.Visibility = ViewStates.Gone;
                bttn.Visibility = ViewStates.Gone;
                    settings.Visibility = ViewStates.Gone;
                Android.Support.V4.View.ViewCompat.SetTranslationZ(bttn, 0);
                }

        });
            if(OnUIthread())
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
            atDirectory.CreateFile("nomedia/customnomedia",".nomedia");
        }

        public static object lock_toplevel_ifexist_create = new object();
        public static object lock_album_ifexist_create = new object();

        public static System.IO.Stream GetIncompleteStream(string username, string fullfilename, out Android.Net.Uri incompleteUri, out Android.Net.Uri parentUri, out long partialLength)
        {
            string name = Helpers.GetFileNameFromFile(fullfilename);
            string dir = Helpers.GetFolderNameFromFile(fullfilename);
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
                    lock(lock_toplevel_ifexist_create)
                    {
                        incompleteDir = new Java.IO.File(incompleteDirString);
                        if(!incompleteDir.Exists())
                        {
                            //make it and add nomedia...
                            incompleteDir.Mkdirs();
                            CreateNoMediaFileLegacy(incompleteDirString);
                        }
                    }

                    string fullDir = rootdir + @"/Soulseek Incomplete/" + Helpers.GenerateIncompleteFolderName(username, dir); //+ @"/" + name;
                    musicDir = new Java.IO.File(fullDir);
                    lock(lock_album_ifexist_create)
                    {
                        if(!musicDir.Exists())
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
                    throw e;
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
                //MainActivity.LogDebug("rootDocumentFileIsNull: " + rootDocumentFileIsNull);
                try
                {
                    if(useDownloadDir)
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
                    else if(useCustomDir)
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
                    else if(!rootdir.CanWrite())
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
                            if(slskDir1==null)
                            {
                                string diagMessage = CheckPermissions(rootdir.Uri);
                                LogFirebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                                LogInfoFirebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                            }
                            else if(!slskDir1.Exists())
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


                    string album_folder_name = Helpers.GenerateIncompleteFolderName(username, dir);
                    lock (lock_album_ifexist_create)
                    {
                        folderDir1 = slskDir1.FindFile(album_folder_name); //does the folder we want to save to exist
                        if (folderDir1 == null || !folderDir1.Exists())
                        {
                            folderDir1 = slskDir1.CreateDirectory(album_folder_name);
                            //folderDir1 = slskDir1.FindFile(album_folder_name); //if it does not exist you then have to get it again!!
                            if(folderDir1==null)
                            {
                                string rootUri = string.Empty;
                                if(SoulSeekState.RootDocumentFile!=null)
                                {
                                    rootUri = SoulSeekState.RootDocumentFile.Uri.ToString();
                                }
                                bool slskDirExistsWriteable = false;
                                if(slskDir1!=null)
                                {
                                    slskDirExistsWriteable = slskDir1.Exists() && slskDir1.CanWrite();
                                }
                                string diagMessage = CheckPermissions(slskDir1.Uri);
                                LogInfoFirebase("folderDir1 is null:" + album_folder_name + "root: " + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                                LogFirebase("folderDir1 is null:" + album_folder_name + "root: "  + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                            }
                            else if(!folderDir1.Exists())
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
                    stream = SoulSeekState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri,"wa");
                }
                else
                {
                    partialLength = 0;
                    DocumentFile mFile = Helpers.CreateMediaFile(folderDir1,name); //on samsung api 19 it renames song.mp3 to song.mp3.mp3. //TODO fix this! (tho below api 29 doesnt use this path anymore)
                    //String: name of new document, without any file extension appended; the underlying provider may choose to append the extension.. Whoops...
                    incompleteUri = mFile.Uri;
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
            if(SoulSeekState.ActiveActivityRef!=null)
            {
                var cursor = SoulSeekState.ActiveActivityRef.ContentResolver.Query(folder, new string[] {DocumentsContract.Document.ColumnFlags },null,null,null);
                int flags = 0;
                if (cursor.MoveToFirst())
                {
                    flags = cursor.GetInt(0);
                }
                cursor.Close();
                bool canWrite = (flags & (int)DocumentContractFlags.SupportsWrite) != 0;
                bool canDirCreate = (flags & (int)DocumentContractFlags.DirSupportsCreate) != 0;
                if(canWrite && canDirCreate)
                {
                    return "Can Write and DirSupportsCreate";
                }
                else if(canWrite)
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






        private static string SaveToFile(string fullfilename, string username, byte[] bytes, Android.Net.Uri uriOfIncomplete, Android.Net.Uri parentUriOfIncomplete, bool memoryMode, out string finalUri)
        {
            string name = Helpers.GetFileNameFromFile(fullfilename);
            string dir  = Helpers.GetFolderNameFromFile(fullfilename);
            string filePath = string.Empty;

            if(memoryMode && (bytes == null || bytes.Length==0))
            {
                LogFirebase("EMPTY or NULL BYTE ARRAY in mem mode");
            }

            if(!memoryMode && uriOfIncomplete == null)
            {
                LogFirebase("no URI in file mode");
            }
            finalUri = string.Empty;
            if(SoulSeekState.UseLegacyStorage() && 
                (SoulSeekState.RootDocumentFile == null && !SettingsActivity.UseIncompleteManualFolder())) //if the user didnt select a complete OR incomplete directory. i.e. pure java files.  
            {

                //this method works just fine if coming from a temp dir.  just not a open doc tree dir.

                string rootdir=string.Empty;
                //if (SoulSeekState.SaveDataDirectoryUri ==null || SoulSeekState.SaveDataDirectoryUri == string.Empty)
                //{
                    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //}
                //else
                //{
                //    rootdir = SoulSeekState.SaveDataDirectoryUri;
                //}
                if(!(new Java.IO.File(rootdir)).Exists())
                {
                    (new Java.IO.File(rootdir)).Mkdirs();
                }
                //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                string intermediateFolder = @"/";
                if (SoulSeekState.CreateCompleteAndIncompleteFolders)
                {
                    intermediateFolder = @"/Soulseek Complete/";
                }
                if(SoulSeekState.CreateUsernameSubfolders)
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
                    MoveFile(new FileInputStream(inFile),stream, inFile, inDir);
                }
            }
            else
            {
                bool useLegacyDocFileToJavaFileOverride = false;
                DocumentFile legacyRootDir = null;
                if(SoulSeekState.UseLegacyStorage() && SoulSeekState.RootDocumentFile == null && SettingsActivity.UseIncompleteManualFolder())
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
                        if(slskDir1 == null || !slskDir1.Exists())
                        {
                            slskDir1 = rootdir.CreateDirectory("Soulseek Complete");
                            LogDebug("Creating Soulseek Complete");
                            diagDidWeCreateSoulSeekDir = true;
                        }

                        if(slskDir1 == null)
                        {
                            diagSlskDirExistsAfterCreation = false;
                        }
                        else if(!slskDir1.Exists())
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
                        tempUsernameDir1 = slskDir1.FindFile(username); //does username folder exist
                        if (tempUsernameDir1 == null || !tempUsernameDir1.Exists())
                        {
                            tempUsernameDir1 = slskDir1.CreateDirectory(username);
                            LogDebug(string.Format("Creating {0} dir",username));
                            diagDidWeCreateUsernameDir = true;
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


                    folderDir1 = slskDir1.FindFile(dir); //does the folder we want to save to exist
                    if(folderDir1 == null || !folderDir1.Exists())
                    {
                        LogDebug("Creating " + dir);
                        folderDir1 = slskDir1.CreateDirectory(dir);
                    }
                    if(folderDir1== null || !folderDir1.Exists())
                    {
                        LogFirebase("folderDir is null or does not exists");
                    }
                    //folderDir1 = slskDir1.FindFile(dir); //if it does not exist you then have to get it again!!
                }
                catch(Exception e)
                {
                    MainActivity.LogFirebase("Filesystem Issue: " + e.Message + diagSlskDirExistsAfterCreation + diagRootDirExists + diagDidWeCreateSoulSeekDir + rootDocumentFileIsNull + SoulSeekState.CreateUsernameSubfolders);
                }

                if(rootdir == null && !SoulSeekState.UseLegacyStorage())
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(()=>{ ToastUI(SoulSeekState.MainActivityRef.GetString(Resource.String.seeker_cannot_access_files)); });
                }

                //BACKUP IF FOLDER DIR IS NULL
                if(folderDir1 == null)
                {
                    folderDir1 = rootdir; //use the root instead..
                }

                filePath = folderDir1.Uri + @"/" + name;

                //Java.IO.File musicFile = new Java.IO.File(filePath);
                //FileOutputStream stream = new FileOutputStream(mFile);
                if(memoryMode)
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
                    if(SoulSeekState.PreMoveDocument() ||
                        SettingsActivity.UseTempDirectory() || //i.e. if use temp dir which is file: // rather than content: //
                        (SoulSeekState.UseLegacyStorage() && SettingsActivity.UseIncompleteManualFolder() && SoulSeekState.RootDocumentFile == null) //i.e. if use complete dir is file: // rather than content: // but Incomplete is content: //
                        ) 
                    {
                        DocumentFile mFile = Helpers.CreateMediaFile(folderDir1, name);
                        uri = mFile.Uri;
                        finalUri = mFile.Uri.ToString();
                        System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                        MoveFile(SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete),stream,uriOfIncomplete, parentUriOfIncomplete);
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
                            //"/tree/primary:musictemp/document/primary:music2/J when two different uri trees the uri returned from move document is a mismash of the two... even tho it actually moves it correctly.
                            //folderDir1.FindFile(name).Uri.Path is right uri and IsFile returns true...
                            if(SettingsActivity.UseIncompleteManualFolder()) //fix due to above^  otherwise "Play File" silently fails
                            {
                                uri = folderDir1.FindFile(realName).Uri; //dont use name!!! in my case the name was .m4a but the actual file was .mp3!!
                            }
                        }
                        catch (Exception e)
                        {
                            MainActivity.LogFirebase("CRITICAL FILESYSTEM ERROR " + e.Message);
                            SeekerApplication.ShowToast("Error Saving File", ToastLength.Long);
                            MainActivity.LogDebug(e.Message + " " + uriOfIncomplete.Path); //Unknown Authority happens when source is file :/// storage/emulated/0/Android/data/com.companyname.andriodapp1/files/Soulseek%20Incomplete/
                        }
                        //throws "no static method with name='moveDocument' signature='(Landroid/content/ContentResolver;Landroid/net/Uri;Landroid/net/Uri;Landroid/net/Uri;)Landroid/net/Uri;' in class Landroid/provider/DocumentsContract;"
                    }
                    finalUri = uri.ToString();

                    //1220ms for 35mb so 10x slower
                    //DocumentFile mFile = folderDir1.CreateFile(@"audio/mp3", name);
                    //System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                    //MoveFile(SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);

                    if (uri == null)
                    {
                        LogFirebase("DocumentsContract MoveDocument FAILED, override incomplete: " + SoulSeekState.OverrideDefaultIncompleteLocations);
                    }
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

            if(SoulSeekState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory())
            {
                try
                {
                    if(!(new Java.IO.File(toDelete.Path)).Delete())
                    {
                        LogFirebase("Java.IO.File.Delete() failed to delete");
                    }
                }
                catch(Exception e)
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
            if (SoulSeekState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory())
            {
                parent = DocumentFile.FromFile(new Java.IO.File(parentToDelete.Path));
            }
            else
            {
                parent = DocumentFile.FromTreeUri(SoulSeekState.ActiveActivityRef, parentToDelete); //if from single uri then listing files will give unsupported operation exception...  //if temp (file: //)this will throw (which makes sense as it did not come from open tree uri)
            }
            LogDebug(parent.Name + "p name");
            LogDebug(parent.ListFiles().ToString());
            LogDebug(parent.ListFiles().Length.ToString());
            foreach(var f in parent.ListFiles())
            {
                LogDebug("child: " + f.Name);
            }

            if(parent.ListFiles().Length==1 && parent.ListFiles()[0].Name==".nomedia")
            {
                if (!parent.Delete())
                {
                    LogFirebase("parent.Delete() failed to delete parent");
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
            if(!toDelete.Delete())
            {
                LogFirebase("LEGACY df.Delete() failed to delete ()");
            }
            if (parent.ListFiles().Length == 1 && parent.ListFiles()[0].Name == ".nomedia")
            {
                if(!parent.ListFiles()[0].Delete())
                {
                    LogFirebase("LEGACY parent.Delete() failed to delete .nomedia child...");
                }
                if (!parent.Delete()) //this returns false... maybe delete .nomedia child??? YUP.  cannot delete non empty dir...
                {
                    LogFirebase("LEGACY parent.Delete() failed to delete parent");
                }
            }
            //LogDebug(toDelete.ParentFile.Name + ":" + toDelete.ParentFile.ListFiles().Length.ToString());
        }

        private static void SaveFileToMediaStore(string path)
        {
            //ContentValues contentValues = new ContentValues();
            //contentValues.Put(MediaStore.MediaColumns.DateAdded, DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
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
            editor.PutString(SoulSeekState.M_SaveDataDirectoryUri,SoulSeekState.SaveDataDirectoryUri);
            editor.PutInt(SoulSeekState.M_NumberSearchResults,SoulSeekState.NumberSearchResults);
            editor.PutInt(SoulSeekState.M_DayNightMode,SoulSeekState.DayNightMode);
            editor.PutBoolean(SoulSeekState.M_AutoClearComplete, SoulSeekState.AutoClearComplete);
            editor.PutBoolean(SoulSeekState.M_RememberSearchHistory, SoulSeekState.RememberSearchHistory);
            editor.PutBoolean(SoulSeekState.M_OnlyFreeUploadSlots, SoulSeekState.FreeUploadSlotsOnly);
            editor.PutBoolean(SoulSeekState.M_FilterSticky, SearchFragment.FilterSticky);
            editor.PutString(SoulSeekState.M_FilterStickyString, SearchTabHelper.FilterString);
            editor.PutBoolean(SoulSeekState.M_MemoryBackedDownload, SoulSeekState.MemoryBackedDownload);
            editor.PutInt(SoulSeekState.M_SearchResultStyle,(int)(SearchFragment.SearchResultStyle));
            editor.PutBoolean(SoulSeekState.M_DisableToastNotifications, SoulSeekState.DisableDownloadToastNotification);
            editor.PutInt(SoulSeekState.M_UploadSpeed, SoulSeekState.UploadSpeed);
            editor.PutString(SoulSeekState.M_UploadDirectoryUri, SoulSeekState.UploadDataDirectoryUri);
            editor.PutBoolean(SoulSeekState.M_SharingOn, SoulSeekState.SharingOn);
            editor.PutBoolean(SoulSeekState.M_AllowPrivateRooomInvitations, SoulSeekState.AllowPrivateRoomInvitations);

            if(SoulSeekState.UserList!=null)
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
            outState.PutBoolean(SoulSeekState.M_CurrentlyLoggedIn,SoulSeekState.currentlyLoggedIn);
            outState.PutString(SoulSeekState.M_Username, SoulSeekState.Username);
            outState.PutString(SoulSeekState.M_Password,SoulSeekState.Password);
            outState.PutString(SoulSeekState.M_SaveDataDirectoryUri, SoulSeekState.SaveDataDirectoryUri);
            outState.PutInt(SoulSeekState.M_NumberSearchResults, SoulSeekState.NumberSearchResults);
            outState.PutInt(SoulSeekState.M_DayNightMode, SoulSeekState.DayNightMode);
            outState.PutBoolean(SoulSeekState.M_AutoClearComplete, SoulSeekState.AutoClearComplete);
            outState.PutBoolean(SoulSeekState.M_RememberSearchHistory, SoulSeekState.RememberSearchHistory);
            outState.PutBoolean(SoulSeekState.M_MemoryBackedDownload, SoulSeekState.MemoryBackedDownload);
            outState.PutBoolean(SoulSeekState.M_FilterSticky, SearchFragment.FilterSticky);
            outState.PutBoolean(SoulSeekState.M_OnlyFreeUploadSlots, SoulSeekState.FreeUploadSlotsOnly);
            outState.PutBoolean(SoulSeekState.M_DisableToastNotifications, SoulSeekState.DisableDownloadToastNotification);
            outState.PutInt(SoulSeekState.M_SearchResultStyle, (int)(SearchFragment.SearchResultStyle));
            outState.PutString(SoulSeekState.M_FilterStickyString, SearchTabHelper.FilterString);
            outState.PutInt(SoulSeekState.M_UploadSpeed, SoulSeekState.UploadSpeed);
            outState.PutString(SoulSeekState.M_UploadDirectoryUri, SoulSeekState.UploadDataDirectoryUri);
            outState.PutBoolean(SoulSeekState.M_AllowPrivateRooomInvitations, SoulSeekState.AllowPrivateRoomInvitations);
            outState.PutBoolean(SoulSeekState.M_SharingOn, SoulSeekState.SharingOn);
            if(SoulSeekState.UserList != null)
            {
                outState.PutString(SoulSeekState.M_UserList, SeekerApplication.SaveUserListToString(SoulSeekState.UserList));
            }
            
        }

        private void Tabs_TabSelected(object sender, TabLayout.TabSelectedEventArgs e)
        {
            System.Console.WriteLine(e.Tab.Position);
            if(e.Tab.Position != 1) //i.e. if we are not the search tab
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
            //in addition each fragment is responsible for expanding their menu...
            if(e.Position==0)
            {
                this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                this.SupportActionBar.SetDisplayShowTitleEnabled(true);
                this.SupportActionBar.Title = this.GetString(Resource.String.home_tab);
                this.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.account_menu);
            }
            if(e.Position==1) //search
            {
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
                SearchFragment.ConfigureSupportCustomView(this.SupportActionBar.CustomView/*, initText*/);
                //this.SupportActionBar.CustomView.FindViewById<View>(Resource.Id.searchHere).FocusChange += MainActivity_FocusChange;
                this.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.account_menu);
                if(goToSearchTab != int.MaxValue)
                {
                    if (SearchFragment.Instance.Activity==null || !(SearchFragment.Instance.Activity.Lifecycle.CurrentState.IsAtLeast(Android.Arch.Lifecycle.Lifecycle.State.Started))) //this happens if we come from settings activity. Main Activity has NOT been started. SearchFragment has the .Actvity ref of an OLD activity.  so we are not ready yet. 
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
            else if(e.Position==2)
            {
                this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                this.SupportActionBar.SetDisplayShowTitleEnabled(true);
                this.SupportActionBar.Title = this.GetString(Resource.String.transfer_tab);
                this.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.browse_menu_empty);
            }
            else if(e.Position==3)
            {
                this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                this.SupportActionBar.SetDisplayShowTitleEnabled(true);
                this.SupportActionBar.Title = this.GetString(Resource.String.browse_tab);
                this.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.transfers_menu);
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
            if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {
                return;
            }
            else
            {
                FinishAndRemoveTask();
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
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
        public DownloadInfo(string usr, string file, long size,Task task, CancellationTokenSource token, int queueLength, int retryCount)
        {
            username = usr; fullFilename = file; downloadTask = task;Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount;
        }
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount,  Exception previousFailureException)
        {
            username = usr; fullFilename = file; downloadTask = task; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; PreviousFailureException = previousFailureException; 
        }
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount, Android.Net.Uri incompleteLocation)
        {
            username = usr; fullFilename = file; downloadTask = task; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; IncompleteLocation = incompleteLocation;
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
                return RequestedUserList.Where((userListItem) => {return userListItem.Username==uname; }).FirstOrDefault();
            }
        }

        private static bool ContainsUserInfo(string uname)
        {
            var uinfo = GetInfoForUser(uname);
            if(uinfo==null)
            {
                return false;
            }
            if(uinfo.UserInfo==null || uinfo.UserData==null)
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
            if(ContainsUserInfo(uname))
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
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) => {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() => {
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
                    if(userInfoTask.IsCompletedSuccessfully)
                    {
                        if(!AddIfRequestedUser(uname, null, null, userInfoTask.Result))
                        {
                            MainActivity.LogFirebase("requested user info logic yet could not find in list!!");
                            //HANDLE ERROR TODO
                        }
                        else
                        {
                            Action<View> action = new Action<View>((View v) => {
                                LaunchUserInfoView(uname);
                            });

                            SoulSeekState.ActiveActivityRef.RunOnUiThread( () => { 
                                //show snackbar (for active activity, active content view) so they can go to it... TODO
                                Snackbar sb = Snackbar.Make(SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.user_info_received),uname), Snackbar.LengthLong).SetAction(Resource.String.view, action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                                (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(Android.Graphics.Color.ParseColor("#BCC1F7"));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                                sb.Show();
                            });
                        }
                    }
                    else
                    {
                        Exception e = userInfoTask.Exception;

                        if(e.InnerException is SoulseekClientException && e.InnerException.Message.Contains("Failed to establish a direct or indirect message connection"))
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                                Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.user_info_failed_conn), uname),ToastLength.Long).Show();
                            });
                        }
                        else if(e.InnerException is UserOfflineException)
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                                Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.user_info_failed_offline), uname), ToastLength.Long).Show();
                            });
                        }
                        else if(e.InnerException is TimeoutException)
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
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
                        }
                        if (userStatus != null)
                        {
                            MainActivity.LogDebug("Requested server UserStatus received");
                            item.UserStatus = userStatus;
                        }
                        if(userInfo != null)
                        {
                            MainActivity.LogDebug("Requested peer UserInfo received");
                            item.UserInfo = userInfo;
                            if(userInfo.HasPicture)
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
                if(removeOldestPic)
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

    public static class SoulSeekState
    {
        static SoulSeekState()
        {
            downloadInfoList = new List<DownloadInfo>();
        }

        public static bool InDarkModeCache = false;
        public static bool currentlyLoggedIn = false;
        public static bool AutoClearComplete = false;
        public static bool FreeUploadSlotsOnly = true;
        public static bool DisableDownloadToastNotification = false;
        public static bool AutoRetryDownload = true;
        public static bool HideLockedResults = true;
        public static bool MemoryBackedDownload = false;
        public static int NumberSearchResults = MainActivity.DEFAULT_SEARCH_RESULTS;
        public static int DayNightMode = (int)(AppCompatDelegate.ModeNightFollowSystem);
        public static bool RememberSearchHistory = true;
        public static SoulseekClient SoulseekClient = null;
        public static String Username = null;
        public static bool logoutClicked = false;
        public static String Password = null;
        public static bool SharingOn = false;
        public static bool AllowPrivateRoomInvitations = false;
        public static bool StartServiceOnStartup = true;
        public static bool IsStartUpServiceCurrentlyRunning = false;
        public static String SaveDataDirectoryUri = null;
        public static String UploadDataDirectoryUri = null;
        public static String ManualIncompleteDataDirectoryUri = null;
        public static bool DownloadKeepAliveServiceRunning = false;
        public static SlskHelp.SharedFileCache SharedFileCache = null;
        public static int UploadSpeed = -1; //bytes
        public static bool FailedShareParse = false;
        public static bool IsParsing = false;
        public static List<UserListItem> IgnoreUserList = new List<UserListItem>();
        public static List<UserListItem> UserList = new List<UserListItem>();
        public static System.Collections.Concurrent.ConcurrentDictionary<string,string> UserNotes = null;

        public static string UserInfoBio = string.Empty;
        public static string UserInfoPictureName = string.Empty; //filename only. The picture will be in (internal storage) FilesDir/user_info_pic/filename.

        public static bool ListenerEnabled = true;
        public static volatile int ListenerPort = 33939;
        public static bool ListenerUPnpEnabled = true;

        public static bool CreateCompleteAndIncompleteFolders = true;
        public static bool CreateUsernameSubfolders = false;
        public static bool OverrideDefaultIncompleteLocations = false;

        public static EventHandler<EventArgs> DirectoryUpdatedEvent;

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

        public static void ClearDownloadAddedEventsFromTarget(object target)
        {
            if(DownloadAdded == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in DownloadAdded.GetInvocationList())
                {
                    if(d.Target==null) //i.e. static
                    {
                        continue;
                    }
                    if(d.Target.GetType() == target.GetType())
                    {
                        DownloadAdded -= (EventHandler<DownloadAddedEventArgs>)d;
                    }
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
            ClearSearchHistory?.Invoke(null,null);
        }



        public static event EventHandler<DownloadAddedEventArgs> DownloadAdded;
        public static event EventHandler<EventArgs> ClearSearchHistory;
        public static List<DownloadInfo> downloadInfoList;
        /// <summary>
        /// Context of last created activity
        /// </summary>
        public static volatile Activity ActiveActivityRef = null;
        public static ISharedPreferences SharedPreferences;
        public static volatile MainActivity MainActivityRef;



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

        public static void OnDownloadAdded(DownloadInfo dlInfo)
        {
            DownloadAdded(null,new DownloadAddedEventArgs(dlInfo));
        }

        public const string M_CurrentlyLoggedIn = "Momento_LoggedIn";
        public const string M_Username = "Momento_Username";
        public const string M_Password = "Momento_Password";
        public const string M_TransferList = "Momento_List";
        public const string M_Messages = "Momento_Messages";
        public const string M_SearchHistory = "Momento_SearchHistoryArray";
        public const string M_SaveDataDirectoryUri = "Momento_SaveDataDirectoryUri";
        public const string M_NumberSearchResults = "Momento_NumberSearchResults";
        public const string M_DayNightMode = "Momento_DayNightMode";
        public const string M_AutoClearComplete = "Momento_AutoClearComplete";
        public const string M_RememberSearchHistory = "Momento_RememberSearchHistory";
        public const string M_OnlyFreeUploadSlots = "Momento_FreeUploadSlots";
        public const string M_DisableToastNotifications = "Momento_DisableToastNotifications";
        public const string M_MemoryBackedDownload = "Momento_MemoryBackedDownload";
        public const string M_FilterSticky = "Momento_FilterSticky";
        public const string M_FilterStickyString = "Momento_FilterStickyString";
        public const string M_SearchResultStyle = "Momento_SearchResStyle";
        public const string M_UserList = "Cache_UserList";
        public const string M_IgnoreUserList = "Cache_IgnoreUserList";
        public const string M_JoinedRooms = "Cache_JoinedRooms";
        public const string M_AllowPrivateRooomInvitations = "Momento_AllowPrivateRoomInvitations";
        public const string M_ServiceOnStartup = "Momento_ServiceOnStartup";
        public const string M_chatroomsToNotify = "Momento_chatroomsToNotify";
        public const string M_SearchTabsState = "Momento_SearchTabsState";

        public const string M_UploadSpeed = "Momento_UploadSpeed";
        public const string M_UploadDirectoryUri = "Momento_UploadDirectoryUri";
        public const string M_SharingOn = "Momento_SharingOn";

        public const string M_PortMapped = "Momento_PortMapped";
        public const string M_LastSetUpnpRuleTicks = "Momento_LastSetUpnpRuleTicks";
        public const string M_LifetimeSeconds = "Momento_LifetimeSeconds";
        public const string M_LastSetLocalIP = "Momento_LastSetLocalIP";

        public const string M_CACHE_stringUriPairs = "Cache_stringUriPairs";
        public const string M_CACHE_browseResponse = "Cache_browseResponse";
        public const string M_CACHE_friendlyDirNameToUriMapping = "Cache_friendlyDirNameToUriMapping";
        public const string M_CACHE_auxDupList = "Cache_auxDupList";

        public const string M_ListenerEnabled = "Momento_ListenerEnabled";
        public const string M_ListenerPort = "Momento_ListenerPort";
        public const string M_ListenerUPnpEnabled = "Momento_ListenerUPnpEnabled";

        public const string M_UserInfoBio = "Momento_UserInfoBio";
        public const string M_UserInfoPicture = "Momento_UserInfoPicture";
        public const string M_UserNotes = "Momento_UserNotes";

        public const string M_ManualIncompleteDirectoryUri = "Momento_ManualIncompleteDirectoryUri";
        public const string M_UseManualIncompleteDirectoryUri = "Momento_UseManualIncompleteDirectoryUri";
        public const string M_CreateCompleteAndIncompleteFolders= "Momento_CreateCompleteIncomplete";
        public const string M_AdditionalUsernameSubdirectories = "Momento_M_AdditionalUsernameSubdirectories";

        public static event EventHandler<BrowseResponseEvent> BrowseResponseReceived;
        public static Android.Support.V4.Provider.DocumentFile RootDocumentFile = null;
        public static Android.Support.V4.Provider.DocumentFile RootIncompleteDocumentFile = null; //only gets set if can write the dir...
        public static void OnBrowseResponseReceived(BrowseResponse origBR, TreeNode<Directory> rootTree, string fromUsername, string startingLocation)
        {
            BrowseResponseReceived(null,new BrowseResponseEvent(origBR,rootTree, fromUsername, startingLocation));
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
        ///at android.support.v4.view.MotionEventCompatEclair.getX(MotionEventCompatEclair.java:32)
        ///at android.support.v4.view.MotionEventCompat$EclairMotionEventVersionImpl.getX(MotionEventCompat.java:86)
        ///at android.support.v4.view.MotionEventCompat.getX(MotionEventCompat.java:184)
        ///at android.support.v4.view.ViewPager.onInterceptTouchEvent(ViewPager.java:1339)
        /// </summary>
        /// <param name="context"></param>

        public ViewPagerFixed(Context context) :base(context)
        {

        }

        public ViewPagerFixed(Context context, Android.Util.IAttributeSet attrs) : base(context,attrs)
        {

        }

        public ViewPagerFixed(IntPtr intPtr,JniHandleOwnership handle) : base(intPtr,handle)
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
        public BrowseResponseEvent(BrowseResponse origBR,TreeNode<Directory> t, string u, string startingLocation)
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
        public static Android.Support.V4.App.Fragment LoginFragment = null;
        public static Android.Support.V4.App.Fragment TransfersFrag = null;
    }

    public static class Helpers
    {
        public static string GenerateIncompleteFolderName(string username, string albumFolderName)
        {
            string incompleteFolderName = username + "_" + albumFolderName;
            //Path.GetInvalidPathChars() doesnt seem like enough bc I still get failures on ''' and '&'
            foreach (char c in System.IO.Path.GetInvalidPathChars().Union(new []{'&','\''}))
            {
                incompleteFolderName = incompleteFolderName.Replace(c,'_');
            }
            return incompleteFolderName;
        }

        public static bool IsFileUri(string uriString)
        {
            if(uriString.StartsWith("file:"))
            {
                return true;
            }
            else if(uriString.StartsWith("content:"))
            {
                return false;
            }
            else
            {
                throw new Exception("IsFileUri failed: " + uriString);
            }
        }

        static Helpers()
        {
            KNOWN_TYPES = new List<string>() { ".mp3", ".flac", ".wav", ".aiff", ".wma", ".aac" }.AsReadOnly();
        }
        public static ReadOnlyCollection<string> KNOWN_TYPES;


        public static string GetNiceDateTime(DateTime dt)
        {
            System.Globalization.CultureInfo cultureInfo = null;
            try
            {
                cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("CANNOT GET CURRENT CULTURE: " + e.Message + e.StackTrace);
            }
            if (dt.Date == DateTime.Now.Date)
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.today) + " " + dt.ToString("h:mm:ss tt",cultureInfo); //cultureInfo can be null without issue..
            }
            else
            {
                return dt.ToString("MMM d h:mm:ss tt", cultureInfo);
            }
        }

        /// <summary>
        /// true if '/me ' message
        /// </summary>
        /// <returns>true if special message</returns>
        public static bool IsSpecialMessage(string msg)
        {
            if(string.IsNullOrEmpty(msg))
            {
                return false;
            }
            if(msg.StartsWith(@"/me "))
            {
                return true;
            }
            return false;
        }

        public static string ParseSpecialMessage(string msg)
        {
            if (!IsSpecialMessage(msg))
            {
                return msg;
            }
            else
            {
                //"/me goes to the store"
                //"goes to the store" + style
                return msg.Substring(4,msg.Length-4);
            }
        }

        public static void AddUserNoteMenuItem(IMenu menu, int i, int j, int k, string username)
        {
            string title = null;
            if(SoulSeekState.UserNotes.ContainsKey(username))
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.edit_note);
            }
            else
            {
                title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_note);
            }
            if(i!=-1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
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

        public static void SetMenuTitles(IMenu menu, string username)
        {
            var menuItem = menu.FindItem(Resource.Id.action_add_to_user_list);
            SetAddRemoveTitle(menuItem,username);
            menuItem = menu.FindItem(Resource.Id.action_add_user);
            SetAddRemoveTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.addUser);
            SetAddRemoveTitle(menuItem, username);
        }


        public static void AddAddRemoveUserMenuItem(IMenu menu, int i, int j, int k, string username, bool full_title = false)
        {
            string title = null;
            if (!MainActivity.UserListContainsUser(username))
            {
                if(full_title)
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

        public static void AddGivePrivilegesIfApplicable(IMenu menu, int indexToUse)
        {
            if(PrivilegesManager.Instance.GetRemainingDays()>=1)
            {
                if(indexToUse==-1)
                {
                    menu.Add(Resource.String.give_privileges);
                }
                else
                {
                    menu.Add(indexToUse, indexToUse, indexToUse, Resource.String.give_privileges);
                }
            }
        }

        /// <summary>
        /// returns true if found and handled.  a time saver for the more generic context menu items..
        /// </summary>
        /// <returns></returns>
        public static bool HandleCommonContextMenuActions(string contextMenuTitle, string usernameInQuestion, Context activity, View browseSnackView, Action uiUpdateActionNote=null, Action uiUpdateActionAdded_Removed = null, Action uiUpdateActionIgnored_Unignored=null)
        {
            if(activity==null)
            {
                activity = SoulSeekState.ActiveActivityRef;
            }
            if(contextMenuTitle == activity.GetString(Resource.String.ignore_user))
            {
                SeekerApplication.AddToIgnoreListFeedback(activity, usernameInQuestion);
                SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                return true;
            }
            else if(contextMenuTitle == activity.GetString(Resource.String.remove_from_ignored))
            {
                SeekerApplication.RemoveFromIgnoreListFeedback(activity, usernameInQuestion);
                SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                return true;
            }
            else if(contextMenuTitle == activity.GetString(Resource.String.msg_user))
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
                MainActivity.ToastUI_short(string.Format("Removing user: {0}", usernameInQuestion));
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
                Action<View> action = new Action<View>((v) => {
                    Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                    intent.AddFlags(ActivityFlags.SingleTop); //??
                    activity.StartActivity(intent);
                    //((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                });
                DownloadDialog.RequestFilesApi(usernameInQuestion, browseSnackView, action, null);
                return true;
            }
            else if(contextMenuTitle == activity.GetString(Resource.String.get_user_info))
            {
                RequestedUserInfoHelper.RequestUserInfoApi(usernameInQuestion);
                return true;
            }
            else if(contextMenuTitle == activity.GetString(Resource.String.give_privileges))
            {
                ShowGivePrilegesDialog(usernameInQuestion);
                return true;
            }
            else if(contextMenuTitle == activity.GetString(Resource.String.edit_note) ||
                    contextMenuTitle == activity.GetString(Resource.String.add_note))
            {
                ShowEditAddNoteDialog(usernameInQuestion, uiUpdateActionNote);
                return true;
            }
            return false;
        }

        public static void SetMessageTextView(TextView viewMessage, Message msg)
        {
            if (Helpers.IsSpecialMessage(msg.MessageText))
            {
                viewMessage.Text = Helpers.ParseSpecialMessage(msg.MessageText);
                viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Italic);
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
            if (dt.Date == DateTime.Now.Date)
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

        public static string GetFileNameFromFile(string filename)
        {
            int begin = filename.LastIndexOf("\\");
            string clipped = filename.Substring(begin + 1);
            return clipped;
        }


        //this is a helper for this issue:
        //var name1 = df.CreateFile("audio/m4a", "name1").Name;
        //var name2 = df.CreateFile("audio/x-m4a", "name2").Name;
        //  are both extensionless....
        public static DocumentFile CreateMediaFile(DocumentFile parent, string name)
        {
            if(Helpers.GetMimeTypeFromFilename(name) == M4A_MIME)
            {
                return parent.CreateFile(Helpers.GetMimeTypeFromFilename(name), name); //we use just name since it will not add the .m4a extension for us..
            }
            else if(Helpers.GetMimeTypeFromFilename(name) == APE_MIME)
            {
                return parent.CreateFile(Helpers.GetMimeTypeFromFilename(name), name); //we use just name since it will not add the .ape extension for us..
            }
            else if(Helpers.GetMimeTypeFromFilename(name)==null)
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
                switch(ext)
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
                if (e.Message.ToLower().Contains("no activity found to handle"))
                {
                    MainActivity.LogFirebase("viewUri: " + e.Message + httpUri.ToString());
                    SeekerApplication.ShowToast(string.Format("No application found to handle url \"{0}\".  Please install or enable web browser.", httpUri.ToString()),ToastLength.Long);
                }
            }
        }

        public static string GetFolderNameFromFile(string filename)
        {
            try
            {
                int end = filename.LastIndexOf("\\");
                string clipped = filename.Substring(0, end);
                int beginning = clipped.LastIndexOf("\\") + 1;
                return clipped.Substring(beginning);
            }
            catch
            {
                return "";
            }
        }

        public static string GetDominantFileType(SearchResponse resp)
        {
            //basically this works in two ways.  if the first file has a type of .mp3, .flac, .wav, .aiff, .wma, .aac then thats likely the type.
            //if not then we do a more expensive parsing, where we get the most common
            string ext = System.IO.Path.GetExtension(resp.Files.First().Filename);  //do not use Soulseek.File.Extension that will be "" most of the time...
            string dominantTypeToReturn = "";
            if (KNOWN_TYPES.Contains(ext))
            {
                dominantTypeToReturn = ext;
            }
            else
            {
                Dictionary<string,int> countTypes = new Dictionary<string, int>();
                ext = "";
                foreach(Soulseek.File f in resp.Files)
                {
                    ext = System.IO.Path.GetExtension(f.Filename);
                    if (countTypes.ContainsKey(ext))
                    {
                        countTypes[ext] = countTypes[ext] +1;
                    }
                    else
                    {
                        countTypes.Add(ext, 1);
                    }
                }
                string dominantType = "";
                int count = 0;
                foreach(var pair in countTypes)
                {
                    if(pair.Value>count)
                    {
                        dominantType = pair.Key;
                        count = pair.Value;
                    }
                }
                dominantTypeToReturn = dominantType;
            }
            //now get a representative file and get some extra info (if any)
            Soulseek.File representative = null;
            Soulseek.File representative2 = null;
            foreach (Soulseek.File f in resp.Files)
            {
                if(representative==null && dominantTypeToReturn == System.IO.Path.GetExtension(f.Filename))
                {
                    representative = f;
                    continue;
                }
                if(dominantTypeToReturn == System.IO.Path.GetExtension(f.Filename))
                {
                    representative2 = f;
                    break;
                }
            }
            if(representative==null)
            {
                //shouldnt happen
                return dominantTypeToReturn.TrimStart('.');
            }



            //vbr flags never work so just get two representative files and see if bitrate is same...


            bool isVbr = (representative.IsVariableBitRate == null) ? false : representative.IsVariableBitRate.Value;

            if (representative2 != null)
            {
                if(representative.BitRate!=null && representative2.BitRate != null)
                {
                    if(representative.BitRate != representative2.BitRate)
                    {
                        isVbr = true;
                    }
                }
            }

            int bitRate = -1;
            int bitDepth = -1;
            double sampleRate = double.NaN;
            foreach(var attr in representative.Attributes)
            {
                switch(attr.Type)
                {
                    case FileAttributeType.VariableBitRate:
                        if(attr.Value==1)
                        {
                            isVbr = true;
                        }
                        break;
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
            if(!isVbr && bitRate==-1 && bitDepth == -1 && double.IsNaN(sampleRate))
            {
                return dominantTypeToReturn.TrimStart('.'); //nothing to add
            }
            else if(isVbr)
            {
                return dominantTypeToReturn.TrimStart('.') + " (vbr)";
            }
            else if(bitDepth!=-1 && !double.IsNaN(sampleRate))
            {
                return dominantTypeToReturn.TrimStart('.') + " (" + bitDepth + ", " + sampleRate + SeekerApplication.STRINGS_KBS + ")";
            }
            else if(!double.IsNaN(sampleRate))
            {
                return dominantTypeToReturn.TrimStart('.') + " (" + sampleRate + SeekerApplication.STRINGS_KBS + ")";
            }
            else if(bitRate!=-1)
            {
                return dominantTypeToReturn.TrimStart('.') + " (" + bitRate + SeekerApplication.STRINGS_KBS + ")";
            }
            else
            {
                return dominantTypeToReturn.TrimStart('.');
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


        public static Notification CreateNotification(Context context, PendingIntent pendingIntent, string channelID, string titleText, string contentText, bool setOnlyAlertOnce=true)
        {
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            Notification notification = null;
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                notification =
                      new Notification.Builder(context, channelID)
                      .SetContentTitle(titleText)
                      .SetContentText(contentText)
                      .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                      .SetContentIntent(pendingIntent)
                      .SetOnlyAlertOnce(setOnlyAlertOnce) //maybe
                      .SetTicker(titleText).Build();
            }
            else
            {
                notification =
#pragma warning disable CS0618 // Type or member is obsolete
                  new Notification.Builder(context)
#pragma warning restore CS0618 // Type or member is obsolete
                  .SetContentTitle(titleText)
                  .SetContentText(contentText)
                  .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                  .SetContentIntent(pendingIntent)
                  .SetOnlyAlertOnce(setOnlyAlertOnce) //maybe
                  .SetTicker(titleText).Build();
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
            if(!int.TryParse(numDays,out numDaysInt))
            {
                MainActivity.ToastUI(Resource.String.error_days_entered_no_parse);
                return false;
            }
            if(numDaysInt<=0)
            {
                MainActivity.ToastUI(Resource.String.error_days_entered_not_positive);
                return false;
            }
            if(PrivilegesManager.Instance.GetRemainingDays() < numDaysInt)
            {
                MainActivity.ToastUI(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_insufficient_days),numDaysInt));
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
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return false; //if we get here we already did a toast message.
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                        {

                            Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();

                        });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(() => { GivePrivilegesLogic(username, numDaysInt); });
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
            SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.GetString(Resource.String.sending__),ToastLength.Short);
            SoulSeekState.SoulseekClient.GrantUserPrivilegesAsync(username,numDaysInt).ContinueWith(new Action<Task>
                ((Task t) => {
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
                       
                        SeekerApplication.ShowToast(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.give_priv_success), numDaysInt,username), ToastLength.Long);

                        //it could be a good idea to then GET privileges to see if it actually went through... but I think this is good enough...
                        //in the rare case that it fails they do get a message so they can figure it out
                    }
                }));
        }

        public static void ShowEditAddNoteDialog(string username, Action uiUpdateAction=null)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(SoulSeekState.ActiveActivityRef);
            builder.SetTitle(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.note_title), username));
            View viewInflated = LayoutInflater.From(SoulSeekState.ActiveActivityRef).Inflate(Resource.Layout.user_note_dialog, (ViewGroup)SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.editUserNote);

            string existingNote = null;
            SoulSeekState.UserNotes.TryGetValue(username, out existingNote);
            if(existingNote!=null)
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
                    if(!wasEmpty && isEmpty)
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
                    if(uiUpdateAction!=null)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(uiUpdateAction);
                    }

                }
                else if(isEmpty && wasEmpty)
                {
                    //nothing was there and nothing is there now
                    return;
                }
                else //something was there and is there now
                {
                    if(newText == existingNote)
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

            //System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            //{
            //    if (e.ActionId == Android.Views.InputMethods.ImeAction.Send || //in this case it is Send (blue checkmark)
            //        e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
            //        e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
            //        e.ActionId == Android.Views.InputMethods.ImeAction.Search)
            //    {
            //        MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
            //        //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
            //        //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
            //        //overriding this, the keyboard fails to go down by default for some reason.....
            //        try
            //        {
            //            Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
            //            imm.HideSoftInputFromWindow(SoulSeekState.ActiveActivityRef.Window.DecorView.WindowToken, 0);
            //        }
            //        catch (System.Exception ex)
            //        {
            //            MainActivity.LogFirebase(ex.Message + " error closing keyboard");
            //        }
            //        //Do the Browse Logic...
            //        eventHandler(sender, null);
            //    }
            //};

            //input.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.close, eventHandlerCancel);
            // Set up the buttons

            builder.Show();
        }

        private static void SaveUserNotes()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_UserNotes, SeekerApplication.SaveUserNotesToString(SoulSeekState.UserNotes));
                editor.Commit();
            }
        }


        public static void ShowGivePrilegesDialog(string username)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(SoulSeekState.ActiveActivityRef);
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
                GivePrilegesAPI(username,input.Text);
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

    [Serializable]
    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        public T Data;
        public bool IsFilteredOut = false;
        public TreeNode<T> Parent;
        public ICollection<TreeNode<T>> Children;
        public TreeNode(T data)
        {
            this.Data = data;
            this.Children = new LinkedList<TreeNode<T>>();
        }
        public TreeNode<T> AddChild(T child)
        {
            TreeNode<T> childNode = new TreeNode<T>(child) { Parent = this };
            this.Children.Add(childNode);
            return childNode;
        }

        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return null;
        }
    }

    public enum SharingIcons
    {
        Off = 0,
        Error = 1,
        On = 2,
    }



}
