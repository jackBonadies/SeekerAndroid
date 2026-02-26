using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seeker.Helpers;

namespace Seeker
{
    //Services are natural singletons. There will be 0 or 1 instance of your service at any given time.
    [Service(Name = "com.companyname.andriodapp1.UploadService", ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync | Android.Content.PM.ForegroundService.TypeSpecialUse)]
    [MetaData(Constants.AndroidForegroundSpecialUseMetadata, Value = Constants.AndroidForegroundSpecialUseDescription)]
    public class UploadForegroundService : Service
    {
        public const int NOTIF_ID = 1112;
        public const int NonZeroRequestCode = 7671;
        public const string CHANNEL_ID = "my channel id - upload";
        public const string CHANNEL_NAME = "Foreground Upload Service";
        public const string FromTransferUploadString = "FromTransfer_UPLOAD"; //todo update for onclick...
        public override IBinder OnBind(Intent intent)
        {
            return null; //does not allow binding. 
        }



        public static Notification CreateNotification(Context context, String contentText)
        {
            Intent notifIntent = new Intent(context, typeof(MainActivity));
            //notifIntent.
            notifIntent.AddFlags(ActivityFlags.SingleTop);
            notifIntent.PutExtra(FromTransferUploadString, 2);

            PendingIntent pendingIntent =
                PendingIntent.GetActivity(context, NonZeroRequestCode, notifIntent, CommonHelpers.AppendMutabilityIfApplicable((PendingIntentFlags)0, true));
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.
            return CommonHelpers.CreateNotification(context, pendingIntent, CHANNEL_ID, context.GetString(Resource.String.uploads_in_progress), contentText, true, true);
        }


        public static string PluralUploadsRemaining
        {
            get
            {
                return SeekerState.ActiveActivityRef.GetString(Resource.String.uploads_remaining);
            }
        }

        public static string SingularUploadRemaining
        {
            get
            {
                return SeekerState.ActiveActivityRef.GetString(Resource.String.upload_remaining);
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

            SeekerState.UploadKeepAliveServiceRunning = true;

            CommonHelpers.CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);//in android 8.1 and later must create a notif channel else get Bad Notification for startForeground error.
            Notification notification = null;
            int cnt = SeekerApplication.UPLOAD_COUNT;
            if (cnt == -1)
            {
                notification = CreateNotification(this, this.GetString(Resource.String.transfers_in_progress));
            }
            else
            {
                if (cnt == 1)
                {
                    notification = CreateNotification(this, string.Format(SingularUploadRemaining, 1));
                }
                else
                {
                    notification = CreateNotification(this, string.Format(PluralUploadsRemaining, cnt));
                }
            }

            try
            {
                SeekerApplication.AcquireTransferLocksAndResetTimer();
            }
            catch (System.Exception e)
            {
                Logger.Firebase("timer issue: " + e.Message + e.StackTrace);
            }
            this.StartForegroundSafe(NOTIF_ID, notification);
            //runs indefinitely until stop.

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            SeekerState.UploadKeepAliveServiceRunning = false;
            SeekerApplication.ReleaseTransferLocksIfServicesComplete();

            base.OnDestroy();
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }
    }


}