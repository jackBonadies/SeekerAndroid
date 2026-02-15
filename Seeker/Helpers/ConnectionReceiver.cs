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
using Android.Net;
using Android.Widget;
using Seeker.Helpers;
using Seeker.Services;
using System;

namespace Seeker
{
    public class ConnectionReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            NetworkInfo netInfo = intent?.GetParcelableExtra("networkInfo") as NetworkInfo;
            bool isConnected = NetworkHandoffDetector.ProcessEvent(netInfo);

            Logger.Debug("ConnectionReceiver.OnReceive");

            string action = intent?.Action;
            if (action != null && action == ConnectivityManager.ConnectivityAction)
            {
                bool changed = SeekerApplication.SetNetworkState(context);
                if (changed)
                {
                    Logger.Debug("metered state changed.. lets set up our handlers and inform server..");
                    SharingService.SetUnsetSharingBasedOnConditions(true);
                    SeekerState.SharingStatusChangedEvent?.Invoke(null, new EventArgs());
                }

#if DEBUG
                ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);

                if (cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected)
                {
                    Logger.Debug("info: " + cm.ActiveNetworkInfo.GetDetailedState().ToString());
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
                        Logger.Debug("info: " + cm.ActiveNetworkInfo.GetDetailedState().ToString());
                        SeekerApplication.ShowToast("Is Disconnected", ToastLength.Long);
                    }
                    else
                    {
                        Logger.Debug("info: Is Disconnected(null)");
                        SeekerApplication.ShowToast("Is Disconnected (null)", ToastLength.Long);
                    }
                }
#endif
            }
        }

        public static bool DoWeHaveInternet()
        {
            ConnectivityManager cm = (ConnectivityManager)(SeekerState.ActiveActivityRef.GetSystemService(Context.ConnectivityService));
            return cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected;
        }
    }
}
