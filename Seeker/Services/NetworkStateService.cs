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

        public static bool IsNetworkPermitting()
        {
            return PreferencesState.AllowUploadsOnMetered || CurrentConnectionIsUnmetered;
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
                    bool oldState = CurrentConnectionIsUnmetered;
                    CurrentConnectionIsUnmetered = IsUnmetered(context, cm);
                    Logger.Debug("SetNetworkState is metered " + !CurrentConnectionIsUnmetered);
                    return oldState != CurrentConnectionIsUnmetered;
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Firebase("SetNetworkState" + e.Message + e.StackTrace);
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
    }
}
