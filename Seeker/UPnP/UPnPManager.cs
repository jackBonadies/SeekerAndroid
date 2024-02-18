using Android.Content;
using Android.Net.Wifi;
using Android.Widget;
using System;
using System.Threading;

namespace Seeker.UPnP
{
    // TODO Org UPNP folder
    public class UPnpManager
    {
        public static Context Context = null;
        private static UPnpManager instance = null;

        public volatile int DevicesFound = -1;
        public volatile int DevicesSuccessfullyMapped = -1;
        public volatile UPnPDiagStatus DiagStatus = UPnPDiagStatus.None;
        public volatile UPnPRunningStatus RunningStatus = UPnPRunningStatus.NeverStarted;
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
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutLong(KeyConsts.M_LastSetUpnpRuleTicks, LastSetTime.Ticks);
                editor.PutInt(KeyConsts.M_LifetimeSeconds, LastSetLifeTime);
                editor.PutInt(KeyConsts.M_PortMapped, LastSetPort);
                editor.PutString(KeyConsts.M_LastSetLocalIP, LastSetLocalIP);
                editor.Commit();
            }
        }

        public static void RestoreUpnpState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                LastSetTime = new DateTime(SeekerState.SharedPreferences.GetLong(KeyConsts.M_LastSetUpnpRuleTicks, 0));
                LastSetLifeTime = SeekerState.SharedPreferences.GetInt(KeyConsts.M_LifetimeSeconds, -1);
                LastSetPort = SeekerState.SharedPreferences.GetInt(KeyConsts.M_PortMapped, -1);
                LastSetLocalIP = SeekerState.SharedPreferences.GetString(KeyConsts.M_LastSetLocalIP, string.Empty);
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
            if (!SeekerState.ListenerUPnpEnabled)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.upnp_off));
            }
            else if (!SeekerState.ListenerEnabled)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.listener_off));
            }
            else if (RunningStatus == UPnPRunningStatus.NeverStarted)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.upnp_not_ran));
            }
            else if (RunningStatus == UPnPRunningStatus.CurrentlyRunning)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.PendingIcon, Context.GetString(Resource.String.upnp_currently_running));
            }
            else if (RunningStatus == UPnPRunningStatus.Finished)
            {
                if (DiagStatus == UPnPDiagStatus.NoUpnpDevicesFound)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.no_upnp_devices_found));
                }
                else if (DiagStatus == UPnPDiagStatus.WifiDisabled)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.upnp_wifi_only));
                }
                else if (DiagStatus == UPnPDiagStatus.NoWifiConnection)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.upnp_no_wifi_conn));
                }
                else if (DiagStatus == UPnPDiagStatus.UpnpDeviceFoundButFailedToMap)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.failed_to_set));
                }
                else if (DiagStatus == UPnPDiagStatus.ErrorUnspecified)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.error));
                }
                else if (DiagStatus == UPnPDiagStatus.Success)
                {
                    return new Tuple<ListeningIcon, string>(ListeningIcon.SuccessIcon, Context.GetString(Resource.String.upnp_success));
                }
                else
                {
                    MainActivity.LogFirebase("GetIconAndMessage We should not get here");
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.error));
                }
            }
            else if (RunningStatus == UPnPRunningStatus.AlreadyMapped)
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
            RunningStatus = UPnPRunningStatus.Finished;
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
                DiagStatus = UPnPDiagStatus.Success;
            }
            else if (DevicesSuccessfullyMapped == 0 && DevicesFound > 0)
            {
                DiagStatus = UPnPDiagStatus.UpnpDeviceFoundButFailedToMap;
            }
            else if (DevicesSuccessfullyMapped == 0 && DevicesFound == 0)
            {
                DiagStatus = UPnPDiagStatus.NoUpnpDevicesFound;
            }
            if (Feedback)
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    if (DiagStatus == UPnPDiagStatus.NoUpnpDevicesFound)
                    {
                        Toast.MakeText(Context, Context.GetString(Resource.String.no_upnp_devices_found), ToastLength.Short).Show();
                    }
                    else if (DiagStatus == UPnPDiagStatus.UpnpDeviceFoundButFailedToMap)
                    {
                        Toast.MakeText(Context, Context.GetString(Resource.String.failed_to_set), ToastLength.Short).Show();
                    }
                });
            }
            Feedback = false;
            MainActivity.LogDebug("finished " + DiagStatus.ToString());

            SearchFinished?.Invoke(null, new EventArgs());
            if (DiagStatus == UPnPDiagStatus.Success)
            {
                //set up timer to run again...
                RenewMapping();
            }
        }

        public void SearchAndSetMappingIfRequired()
        {
            try
            {
                if (!SeekerState.ListenerEnabled || !SeekerState.ListenerUPnpEnabled)
                {
                    MainActivity.LogDebug("Upnp is off...");
                    SearchFinished?.Invoke(null, new EventArgs());
                    Feedback = false;
                    return;
                }
                if (LastSetLifeTime != -1 && LastSetTime.AddSeconds(LastSetLifeTime / 2.0) > DateTime.UtcNow && LastSetPort == SeekerState.ListenerPort && IsLocalIPsame())
                {
                    MainActivity.LogDebug("Renew Mapping Later... we already have a good one..");
                    RunningStatus = UPnPRunningStatus.AlreadyMapped;
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
                if (!SeekerState.ListenerEnabled || !SeekerState.ListenerUPnpEnabled)
                {
                    DiagStatus = UPnPDiagStatus.UpnpDisabled;
                    RunningStatus = UPnPRunningStatus.Finished;
                    SearchFinished?.Invoke(null, new EventArgs());
                    return;
                }
                if (Context == null)
                {
                    DiagStatus = UPnPDiagStatus.ErrorUnspecified;
                    RunningStatus = UPnPRunningStatus.Finished;
                    throw new Exception("SearchAndSetMapping Context is null");
                }
                WifiManager wm = (WifiManager)Context.GetSystemService(Context.WifiService);
                if (wm.WifiState == Android.Net.WifiState.Disabled) //if just mobile is on and wifi is off.
                {
                    //wifi is disabled.
                    DiagStatus = UPnPDiagStatus.WifiDisabled;
                    RunningStatus = UPnPRunningStatus.Finished;
                    SearchFinished?.Invoke(null, new EventArgs());
                    return;
                }
                if (wm.ConnectionInfo.SupplicantState == SupplicantState.Disconnected || wm.ConnectionInfo.IpAddress == 0)
                {
                    //wifi is disabled.
                    DiagStatus = UPnPDiagStatus.NoWifiConnection;
                    RunningStatus = UPnPRunningStatus.Finished;
                    SearchFinished?.Invoke(null, new EventArgs());
                    return;
                }
                LocalIP = Mono.Nat.NatUtility.LocalIpAddress = Android.Text.Format.Formatter.FormatIpAddress(wm.ConnectionInfo.IpAddress);
                //string gatewayAddress = Android.Text.Format.Formatter.FormatIpAddress(wm.DhcpInfo.Gateway);
                MainActivity.LogDebug(LocalIP);

                DevicesFound = 0;
                DevicesSuccessfullyMapped = 0;
                RunningStatus = UPnPRunningStatus.CurrentlyRunning;
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
                DiagStatus = UPnPDiagStatus.ErrorUnspecified;
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
                    System.Threading.Tasks.Task<Mono.Nat.Mapping> t = e.Device.CreatePortMapAsync(new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp, SeekerState.ListenerPort, SeekerState.ListenerPort, oneWeek, "Android Seeker"));
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
                            System.Threading.Tasks.Task<Mono.Nat.Mapping> t0 = e.Device.CreatePortMapAsync(new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp, SeekerState.ListenerPort, SeekerState.ListenerPort, 0, "Android Seeker"));
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

                    System.Threading.Tasks.Task<Mono.Nat.Mapping> t2 = e.Device.GetSpecificMappingAsync(Mono.Nat.Protocol.Tcp, SeekerState.ListenerPort);
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
                    DiagStatus = UPnPDiagStatus.Success;
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
}