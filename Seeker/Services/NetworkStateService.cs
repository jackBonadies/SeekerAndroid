using Android.Content;
using Android.Net;
using Common;
using Seeker.Helpers;
using System;

namespace Seeker.Services
{
    public static class NetworkStateService
    {
        public static bool CurrentConnectionIsUnmetered = true;
        public static bool CurrentConnectionIsVpn = false;

        public static bool IsNetworkPermitting()
        {
            return IsMeteredPermitting() && IsVpnPermitting();
        }

        public static bool IsMeteredPermitting()
        {
            return PreferencesState.AllowUploadsOnMetered || CurrentConnectionIsUnmetered;
        }

        public static bool IsVpnPermitting()
        {
            return !PreferencesState.RequireVpnForSharing || CurrentConnectionIsVpn;
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

                if (cm == null) //null if class is not a supported system service.
                {
                    return false;
                }

                if (cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected)
                {
                    bool oldUnmetered = CurrentConnectionIsUnmetered;
                    bool oldVpn = CurrentConnectionIsVpn;
                    CurrentConnectionIsUnmetered = IsUnmetered(context, cm);
                    CurrentConnectionIsVpn = IsVpn(cm);
                    Logger.Debug("SetNetworkState is metered " + !CurrentConnectionIsUnmetered + " is vpn " + CurrentConnectionIsVpn);
                    return oldUnmetered != CurrentConnectionIsUnmetered || oldVpn != CurrentConnectionIsVpn;
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Firebase("SetNetworkState" + e.Message + e.StackTrace);
                return false;
            }
        }

        private static bool IsVpn(ConnectivityManager cm)
        {
            // HasTransport(Vpn) on the app's default network means this app's traffic actually routes through the VPN
            var capabilities = cm.GetNetworkCapabilities(cm.ActiveNetwork); // null mid-transition
            return capabilities != null && capabilities.HasTransport(TransportType.Vpn);
        }

        /// <summary>
        /// CONNECTIVITY_ACTION (ConnectionReceiver) does not fire when a VPN comes up over the
        /// same underlying network, so on API 24+ we also listen to default network callbacks.
        /// On API 23 ConnectionReceiver remains the only signal.
        /// </summary>
        public static void RegisterDefaultNetworkCallback(Context context)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                return;
            }
            try
            {
                ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
                cm?.RegisterDefaultNetworkCallback(new DefaultNetworkCallback(context.ApplicationContext));
            }
            catch (Exception e)
            {
                Logger.Firebase("RegisterDefaultNetworkCallback" + e.Message + e.StackTrace);
            }
        }

        private class DefaultNetworkCallback : ConnectivityManager.NetworkCallback
        {
            private readonly Context appContext;

            public DefaultNetworkCallback(Context context)
            {
                appContext = context;
            }

            public override void OnAvailable(Network network)
            {
                Recompute();
            }

            public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities)
            {
                Recompute();
            }

            public override void OnLost(Network network)
            {
                Recompute();
            }

            private void Recompute()
            {
                if (SetNetworkState(appContext))
                {
                    SharingService.SetUnsetSharingBasedOnConditions(true);
                    SharedFileService.SharingStatusChangedEvent?.Invoke(null, new EventArgs());
                }
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
    }
}
