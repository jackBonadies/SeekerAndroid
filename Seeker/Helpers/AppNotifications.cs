using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Seeker.Helpers;
using System;

namespace Seeker
{
    public static class AppNotifications
    {
        public const string CHANNEL_ID_USER_ONLINE = "User Online Alerts ID";
        public const string CHANNEL_NAME_USER_ONLINE = "User Online Alerts";

        public const string CHANNEL_ID_FOLDER_ALERT = "Folder Finished Downloading Alerts ID";
        public const string CHANNEL_NAME_FOLDER_ALERT = "Folder Finished Downloading Alerts";

        public const string CHANNEL_ID_CHATROOM = "Chatroom Messages ID";
        public const string CHANNEL_NAME_CHATROOM = "Chatroom Messages";

        public const string CHANNEL_ID_PRIVATE_MESSAGE = "Private Messages ID";
        public const string CHANNEL_NAME_PRIVATE_MESSAGE = "Private Messages";

        public const string CHANNEL_ID_WISHLIST = "Wishlist Controller ID";
        public const string CHANNEL_NAME_WISHLIST = "Wishlists";

        public const string CHANNEL_ID_UPLOAD_COMPLETED = "upload channel ID";
        public const string CHANNEL_NAME_UPLOAD_COMPLETED = "Upload Notifications";

        public const string CHANNEL_ID_UPLOAD_FOREGROUND = "my channel id - upload";
        public const string CHANNEL_NAME_UPLOAD_FOREGROUND = "Foreground Upload Service";

        public const string CHANNEL_ID_DOWNLOAD_FOREGROUND = "my channel id";
        public const string CHANNEL_NAME_DOWNLOAD_FOREGROUND = "Foreground Download Service";

        public const string CHANNEL_ID_KEEP_ALIVE = "seeker keep alive id";
        public const string CHANNEL_NAME_KEEP_ALIVE = "Seeker Keep Alive Service";

        public static void ShowNotificationForUserOnlineAlert(string username)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, CHANNEL_ID_USER_ONLINE, CHANNEL_NAME_USER_ONLINE, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SeekerState.ActiveActivityRef, typeof(UserListActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop);
                    notifIntent.PutExtra(SeekerApplication.FromUserOnlineAlert, true);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SeekerState.ActiveActivityRef, username.GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = CommonHelpers.CreateNotification(SeekerState.ActiveActivityRef, pendingIntent, CHANNEL_ID_USER_ONLINE, "User Online", string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.user_X_is_now_online), username), false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    notificationManager.Notify(username.GetHashCode(), n);
                }
                catch (Exception e)
                {
                    Logger.Firebase("ShowNotificationForUserOnlineAlert failed: " + e.Message + e.StackTrace);
                }
            });
        }

        public static void ShowNotificationForCompletedFolder(string foldername, string username)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, CHANNEL_ID_FOLDER_ALERT, CHANNEL_NAME_FOLDER_ALERT, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ReorderToFront); //otherwise if another activity is in front then this intent will do nothing...
                    notifIntent.PutExtra(MainActivity.FolderAlertExtra, true);
                    notifIntent.PutExtra(MainActivity.FolderAlertUsernameExtra, username);
                    notifIntent.PutExtra(MainActivity.FolderAlertFoldernameExtra, foldername);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SeekerState.ActiveActivityRef, (foldername + username).GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = CommonHelpers.CreateNotification(SeekerState.ActiveActivityRef, pendingIntent, CHANNEL_ID_FOLDER_ALERT, SeekerApplication.GetString(Resource.String.FolderFinishedDownloading), string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.folder_X_from_user_Y_finished), foldername, username), false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    notificationManager.Notify((foldername + username).GetHashCode(), n);
                }
                catch (Exception e)
                {
                    Logger.Firebase("ShowNotificationForCompletedFolder failed: " + e.Message + e.StackTrace);
                }
            });
        }
    }
}
