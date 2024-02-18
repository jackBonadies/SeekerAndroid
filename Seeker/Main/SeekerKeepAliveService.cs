using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Lifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    //TODOORG seperate class
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
                PendingIntent.GetActivity(context, 0, notifIntent, CommonHelpers.AppendMutabilityIfApplicable((PendingIntentFlags)0, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return CommonHelpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.seeker_running), context.GetString(Resource.String.seeker_running_content), true, true, true);
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
            SeekerState.IsStartUpServiceCurrentlyRunning = true;

            CommonHelpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
            Notification notification = CreateNotification(this);


            //System.Threading.Thread.Sleep(4000); // bug exposer - does help reproduce on samsung galaxy

            //if (foreground)
            //{
            try
            {
                StartForeground(NOTIF_ID, notification); // this can crash if started in background... (and firebase does say they started in background)
            }
            catch (Exception e)
            {
                // this exception is in fact catchable.. though "startForegroundService() did not then call Service.startForeground()" is supposed to cause issues
                //   in my case it did not.
                SeekerState.IsStartUpServiceCurrentlyRunning = false;
                bool foreground = (SeekerState.ActiveActivityRef as AppCompatActivity).Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Resumed);
                MainActivity.LogFirebase($"StartForeground issue: is foreground: {foreground} {e.Message} {e.StackTrace}");
#if DEBUG
                SeekerApplication.ShowToast($"StartForeground failed - is foreground: {foreground}", ToastLength.Long);
#endif
            }

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
            //}
            //runs indefinitely until stop.

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            SeekerState.IsStartUpServiceCurrentlyRunning = false;
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



}