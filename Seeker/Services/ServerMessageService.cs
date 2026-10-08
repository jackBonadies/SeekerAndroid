using Android.App;
using Android.Content;
using Android.Views;
using AndroidX.Core.App;
using Common;
using Google.Android.Material.Snackbar;
using Seeker.Helpers;
using System;

namespace Seeker.Services
{
    // Admin broadcasts from the server (GlobalAdminMessage, e.g. "going offline for maintenance").
    // The latest one is kept in prefs and shown as a banner on the account tab until dismissed.
    public static class ServerMessageService
    {
        public const string CHANNEL_ID = "Server Announcements ID";
        public const string CHANNEL_NAME = "Server Announcements";
        private const int NotificationId = 0x5e7e;
        private const int SnackbarDurationMs = 8000;

        private static readonly TimeSpan RepeatWindow = TimeSpan.FromHours(24);

        private static readonly object syncRoot = new object();

        public static string Text { get; private set; }
        public static DateTime ReceivedUtc { get; private set; }
        public static bool Dismissed { get; private set; }

        public static bool HasActiveMessage => !Dismissed && !string.IsNullOrEmpty(Text);

        // UI thread
        public static event EventHandler Changed;

        public static void Initialize(ISharedPreferences prefs)
        {
            lock (syncRoot)
            {
                Text = prefs.GetString(KeyConsts.M_ServerMessageText, null);
                ReceivedUtc = new DateTime(prefs.GetLong(KeyConsts.M_ServerMessageReceivedTicks, 0), DateTimeKind.Utc);
                Dismissed = prefs.GetBoolean(KeyConsts.M_ServerMessageDismissed, false);
            }
        }

        public static void OnGlobalMessageReceived(object sender, string message)
        {
            try
            {
                Logger.Debug("server announcement received: " + message);
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }
                message = message.Trim();

                lock (syncRoot)
                {
                    DateTime now = DateTime.UtcNow;
                    // if message is same and we already showed it recently, dont show it again.
                    // dont think this happens in practice - I think the server announcement is a 
                    // one-time broadcast
                    if (message == Text && now - ReceivedUtc < RepeatWindow)
                    {
                        Logger.Debug("discarding repeat server announcement");
                        return;
                    }
                    Text = message;
                    ReceivedUtc = now;
                    Dismissed = false;
                    Save();
                }

                ShowNotification(message);
                SeekerApplication.RunOnUIThread(() =>
                {
                    RaiseChanged();
                    ShowSnackbar(message);
                });
            }
            catch (Exception e)
            {
                Logger.FirebaseError("OnGlobalMessageReceived failed", e);
            }
        }

        public static void Dismiss()
        {
            lock (syncRoot)
            {
                Dismissed = true;
                Save();
            }
            try
            {
                NotificationManagerCompat.From(SeekerApplication.ApplicationContext).Cancel(NotificationId);
            }
            catch (Exception e)
            {
                Logger.Debug("cancel server message notification failed: " + e.Message);
            }
            RaiseChanged();
        }

        private static void Save()
        {
            var editor = SeekerState.SharedPreferences.Edit();
            editor.PutString(KeyConsts.M_ServerMessageText, Text);
            editor.PutLong(KeyConsts.M_ServerMessageReceivedTicks, ReceivedUtc.Ticks);
            editor.PutBoolean(KeyConsts.M_ServerMessageDismissed, Dismissed);
            editor.Apply();
        }

        private static void RaiseChanged()
        {
            try
            {
                Changed?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Logger.FirebaseError("ServerMessageService.Changed handler failed", e);
            }
        }

        private static Intent CreateGoToAccountIntent(Context context)
        {
            Intent intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ReorderToFront);
            intent.PutExtra(MainActivity.GoToAccountExtra, true);
            return intent;
        }

        private static void ShowSnackbar(string message)
        {
            if (ForegroundLifecycleTracker.IsBackground() || SeekerState.ActiveActivityRef == null)
            {
                return;
            }
            try
            {
                var activity = SeekerState.ActiveActivityRef;
                string text = string.Format(SeekerApplication.GetString(Resource.String.server_message_snackbar), message);
                Snackbar.Make(UiHelpers.GetViewForSnackbar(), text, SnackbarDurationMs)
                    .SetAction(SeekerApplication.GetString(Resource.String.view), (View v) =>
                    {
                        activity.StartActivity(CreateGoToAccountIntent(activity));
                    })
                    .Show();
            }
            catch (Exception e)
            {
                Logger.FirebaseError("server message snackbar failed", e);
            }
        }

        private static void ShowNotification(string message)
        {
            try
            {
                Context context = SeekerApplication.ApplicationContext;
                CommonHelpers.CreateNotificationChannel(context, CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High); //only high will "peek"
                PendingIntent pendingIntent = PendingIntent.GetActivity(context, NotificationId, CreateGoToAccountIntent(context),
                    CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                string title = SeekerApplication.GetString(Resource.String.server_message_title);
                Notification notification = new NotificationCompat.Builder(context, CHANNEL_ID)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
                    .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(true)
                    .SetTicker(title)
                    .Build();
                NotificationManagerCompat.From(context).Notify(NotificationId, notification);
            }
            catch (Exception e)
            {
                Logger.FirebaseError("server message notification failed", e);
            }
        }
    }
}
