using Android.App;
using Android.Net;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seeker.Helpers
{
    public static class ServiceExtensions
    {
        public static void StartForegroundSafe(this Service service, int notificationId, Notification notification)
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.UpsideDownCake)
            {
                service.StartForeground(notificationId, notification, Android.Content.PM.ForegroundService.TypeSpecialUse);
            }
            else if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
            {
                service.StartForeground(notificationId, notification, Android.Content.PM.ForegroundService.TypeDataSync);
            }
            else
            {
                service.StartForeground(notificationId, notification);
            }
        }
    }
}
