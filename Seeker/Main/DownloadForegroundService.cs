using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Lifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    //Services are natural singletons. There will be 0 or 1 instance of your service at any given time.
    [Service(Name = "com.companyname.andriodapp1.DownloadService", ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
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
                PendingIntent.GetActivity(context, NonZeroRequestCode, notifIntent, CommonHelpers.AppendMutabilityIfApplicable((PendingIntentFlags)0, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return CommonHelpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.download_in_progress), contentText, true, true);
        }


        public static string PluralDownloadsRemaining
        {
            get
            {
                return SeekerState.ActiveActivityRef.GetString(Resource.String.downloads_remaining);
            }
        }

        public static string SingularDownloadRemaining
        {
            get
            {
                return SeekerState.ActiveActivityRef.GetString(Resource.String.download_remaining);
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
            SeekerState.DownloadKeepAliveServiceRunning = true;

            CommonHelpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
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

            try
            {
                // can throw ForegroundServiceStartNotAllowedException
                StartForeground(NOTIF_ID, notification);
            }
            catch(System.Exception e)
            {
                // its okay to just not promote this service to foreground.
                // you will still get notifications, it will just be a lower priority process
                // also, next time this gets hit (i.e. if a user starts another set of downloads)
                // the service can then maybe successfully get promoted.
                MainActivity.LogFirebaseError($"Download service failed promoting to foreground. background: {ForegroundLifecycleTracker.IsBackground()}", e);

            }
            //runs indefinitely until stop.

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            SeekerState.DownloadKeepAliveServiceRunning = false;
            SeekerApplication.ReleaseTransferLocksIfServicesComplete();
            //save once complete
            TransfersFragment.SaveTransferItems(SeekerState.SharedPreferences, false, 0);
            base.OnDestroy();
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }

}