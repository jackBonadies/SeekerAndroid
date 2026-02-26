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
using Android.Net;
using Seeker.Helpers;
using System;

namespace Seeker
{
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
                    if ((DateTime.UtcNow - DisconnectedTime).TotalSeconds < 2.0)
                    {
                        Logger.Debug("total seconds..." + (DateTime.UtcNow - DisconnectedTime).TotalSeconds);
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
                Logger.Debug("total seconds..." + (DateTime.UtcNow - NetworkHandOffTime).TotalSeconds);
                return (DateTime.UtcNow - NetworkHandOffTime).TotalSeconds < 30.0;
            }
        }
    }
}
