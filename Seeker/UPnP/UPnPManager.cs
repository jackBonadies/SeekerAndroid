using Android.Content;
using Android.Net.Wifi;
using Android.Widget;
using System;
using System.Threading;
using Seeker.Helpers;

using Common;
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
            PreferencesManager.SaveUPnPState(LastSetTime.Ticks, LastSetLifeTime, LastSetPort, LastSetLocalIP);
        }

        public static void RestoreUpnpState()
        {
            PreferencesManager.RestoreUPnPState(out long ticks, out int lifetime, out int port, out string localIP);
            LastSetTime = new DateTime(ticks);
            LastSetLifeTime = lifetime;
            LastSetPort = port;
            LastSetLocalIP = localIP;
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
            if (!PreferencesState.ListenerUPnpEnabled)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.OffIcon, Context.GetString(Resource.String.upnp_off));
            }
            else if (!PreferencesState.ListenerEnabled)
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
                    Logger.Firebase("GetIconAndMessage We should not get here");
                    return new Tuple<ListeningIcon, string>(ListeningIcon.ErrorIcon, Context.GetString(Resource.String.error));
                }
            }
            else if (RunningStatus == UPnPRunningStatus.AlreadyMapped)
            {
                return new Tuple<ListeningIcon, string>(ListeningIcon.SuccessIcon, Context.GetString(Resource.String.upnp_last_success));
            }
            else
            {
                Logger.Firebase("GetIconAndMessage We should not get here 2");
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
                Logger.Firebase("FinishSearchTimer_Elapsed " + ex.Message + ex.StackTrace);
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
                if (DiagStatus == UPnPDiagStatus.NoUpnpDevicesFound)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.no_upnp_devices_found), ToastLength.Short);
                }
                else if (DiagStatus == UPnPDiagStatus.UpnpDeviceFoundButFailedToMap)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_set), ToastLength.Short);
                }
            }
            Feedback = false;
            Logger.Debug("finished " + DiagStatus.ToString());

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
                if (!PreferencesState.ListenerEnabled || !PreferencesState.ListenerUPnpEnabled)
                {
                    Logger.Debug("Upnp is off...");
                    SearchFinished?.Invoke(null, new EventArgs());
                    Feedback = false;
                    return;
                }
                if (LastSetLifeTime != -1 && LastSetTime.AddSeconds(LastSetLifeTime / 2.0) > DateTime.UtcNow && LastSetPort == PreferencesState.ListenerPort && IsLocalIPsame())
                {
                    Logger.Debug("Renew Mapping Later... we already have a good one..");
                    RunningStatus = UPnPRunningStatus.AlreadyMapped;
                    SearchFinished?.Invoke(null, new EventArgs());
                    Feedback = false;
                    RenewMapping();
                }
                else
                {
                    Logger.Debug("search and set mapping...");
                    SearchAndSetMapping();
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("SearchAndSetMappingIfRequired" + e.Message + e.StackTrace);
                Feedback = false;
            }
        }


        public static System.Timers.Timer RenewMappingTimer = null;
        public void RenewMapping() //if new port
        {
            Logger.Debug("renewing mapping");
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
                Logger.Firebase("RenewMapping" + e.Message + e.StackTrace);
            }
        }

        private void RenewMappingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.Debug("renew timer elapsed");
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
                Logger.Firebase("IsLocalIPsame exception " + ex.Message + ex.StackTrace);
                return false;
            }
        }

        public void SearchAndSetMapping()
        {
            try
            {
                if (!PreferencesState.ListenerEnabled || !PreferencesState.ListenerUPnpEnabled)
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
                Logger.Debug(LocalIP);

                DevicesFound = 0;
                DevicesSuccessfullyMapped = 0;
                RunningStatus = UPnPRunningStatus.CurrentlyRunning;
                SearchStarted?.Invoke(null, new EventArgs());
                if (Feedback)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.attempting_to_find_and_open), ToastLength.Short);
                }

                CancelSearchAfterTime();
                Mono.Nat.NatUtility.StartDiscovery(new Mono.Nat.NatProtocol[] { Mono.Nat.NatProtocol.Upnp });

            }
            catch (Exception e)
            {
                DiagStatus = UPnPDiagStatus.ErrorUnspecified;
                Logger.Firebase("SearchAndSetMapping: " + e.Message + e.StackTrace);
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
                Logger.Debug("Device Found");
                Interlocked.Increment(ref DevicesFound); //not sure if this will ever be greater than one....
                if (DevicesFound > 1)
                {
                    Logger.Firebase("more than 1 device found");
                }
                bool ipOurs = (e.Device as Mono.Nat.Upnp.UpnpNatDevice).LocalAddress.ToString() == Mono.Nat.NatUtility.LocalIpAddress; //I think this will always be true
                if (ipOurs)
                {
                    int oneWeek = 60 * 60 * 24 * 7; // == 604800.  on my home router I request 1 week, I get back 604800 in the mapping. but then on getting it again its 22 hours (which is probably the real time)
                    System.Threading.Tasks.Task<Mono.Nat.Mapping> t = e.Device.CreatePortMapAsync(new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp, PreferencesState.ListenerPort, PreferencesState.ListenerPort, oneWeek, "Android Seeker"));
                    try
                    {
                        bool timeOutCreateMapping = !(t.Wait(5000));
                        if (timeOutCreateMapping)
                        {
                            Logger.Firebase("CreatePortMapAsync timeout");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        //the task can throw (in which case the task.Wait throws)

                        if (ex.InnerException is Mono.Nat.MappingException && ex.InnerException.Message != null && ex.InnerException.Message.Contains("Error 725: OnlyPermanentLeasesSupported")) //happened on my tablet... connected to my other 192.168.1.1 router
                        {
                            System.Threading.Tasks.Task<Mono.Nat.Mapping> t0 = e.Device.CreatePortMapAsync(new Mono.Nat.Mapping(Mono.Nat.Protocol.Tcp, PreferencesState.ListenerPort, PreferencesState.ListenerPort, 0, "Android Seeker"));
                            try
                            {
                                bool timeOutCreateMapping0lease = !(t0.Wait(5000));
                                if (timeOutCreateMapping0lease)
                                {
                                    Logger.Firebase("CreatePortMapAsync timeout try with 0 lease");
                                    return;
                                }
                                t = t0; // use this good task instead. bc t.Result is gonna throw heh
                            }
                            catch (Exception ex0)
                            {
                                Logger.Firebase("CreatePortMapAsync try with 0 lease " + ex0.Message + ex0.StackTrace);
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

                            Logger.Debug("CreatePortMapAsync " + ex.Message + ex.StackTrace);
                            return;
                        }
                    }
                    Mono.Nat.Mapping mapping = t.Result;
                    int seconds = mapping.Lifetime;
                    int privatePort = mapping.PrivatePort;
                    int publicPort = mapping.PublicPort;

                    System.Threading.Tasks.Task<Mono.Nat.Mapping> t2 = e.Device.GetSpecificMappingAsync(Mono.Nat.Protocol.Tcp, PreferencesState.ListenerPort);
                    try
                    {
                        bool timeOutGetMapping = !(t2.Wait(5000));
                        if (timeOutGetMapping)
                        {
                            Logger.Firebase("GetSpecificMappingAsync timeout");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        //the task can throw (in which case the task.Wait throws)
                        Logger.Firebase("GetSpecificMappingAsync " + ex.Message + ex.StackTrace);
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
                        Logger.Firebase("less than 2 hours: " + LastSetLifeTime); //20 mins
                        LastSetLifeTime = 2 * 3600;
                    }

                    LastSetLocalIP = LocalIP;
                    LastSetPort = actualMapping.PublicPort;
                    SaveUpnpState();

                    Interlocked.Increment(ref DevicesSuccessfullyMapped);
                    Logger.Debug("successfully mapped");
                    DiagStatus = UPnPDiagStatus.Success;
                    DeviceSuccessfullyMapped?.Invoke(null, new EventArgs());
                }
                else
                {
                    Logger.Firebase("ip is not ours");
                }
            }
            catch (Exception ex)
            {
                Logger.Firebase("NatUtility_DeviceFound " + ex.Message + ex.StackTrace);
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