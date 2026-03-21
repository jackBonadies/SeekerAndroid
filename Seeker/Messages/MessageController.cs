using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seeker.Helpers;
using Seeker.Services;

using Common;
using Common.Messages;
namespace Seeker.Messages
{
    public static class MessageController
    {
        public static object MessageListLockObject = new object(); //since the Messages List is not concurrent...
        public static bool IsInitialized = false;
        public static EventHandler<Message> MessageReceived;
        public static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>> RootMessages = null; //this is for when the user logs in as different people
        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>> Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
        public static string MessagesUsername = string.Empty;

        public static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, int>> RootLastReadMessageCounts = null;
        public static System.Collections.Concurrent.ConcurrentDictionary<string, int> LastReadMessageCounts = null;


        //static MessageController()
        //{
        //    lock (MessageListLockObject)
        //    {
        //        Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
        //        RestoreMessagesFromSharedPrefs
        //    }
        //}

        public static void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            lock (MessageListLockObject)
            {
                //SerializationHelper.MigratedMessages(SeekerState.SharedPreferences, KeyConsts.M_Messages_Legacy, KeyConsts.M_Messages);
                RestoreMessagesFromSharedPrefs(SeekerState.SharedPreferences);
            }
            SeekerState.SoulseekClient.PrivateMessageReceived += Client_PrivateMessageReceived;
            RestoreLastReadCountsFromSharedPrefs(SeekerState.SharedPreferences);
            bool isFirstTimeInit = !SeekerState.SharedPreferences.Contains(KeyConsts.M_LastReadMessageCounts);
            if (isFirstTimeInit && RootMessages != null)
            {
                foreach (var account in RootMessages)
                {
                    if (!RootLastReadMessageCounts.ContainsKey(account.Key))
                    {
                        RootLastReadMessageCounts[account.Key] = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
                    }
                    foreach (var conv in account.Value)
                    {
                        RootLastReadMessageCounts[account.Key][conv.Key] = conv.Value.Count;
                    }
                }
                // Re-point current user's dict
                if (!string.IsNullOrEmpty(PreferencesState.Username)
                    && RootLastReadMessageCounts.ContainsKey(PreferencesState.Username))
                {
                    LastReadMessageCounts = RootLastReadMessageCounts[PreferencesState.Username];
                }
            }
            // Persist so the key exists on next launch (even if empty), preventing re-seeding.
            if (isFirstTimeInit)
            {
                SaveLastReadCounts(SeekerState.SharedPreferences);
            }
            IsInitialized = true;
            PreferencesState.UsernameChanged += (u) => SwitchUser(u);
        }

        public static void SwitchUser(string username)
        {
            lock (MessageListLockObject)
            {
                if (username == MessagesUsername)
                {
                    return;
                }

                MessagesUsername = username;

                if (RootMessages != null)
                {
                    if (string.IsNullOrEmpty(username))
                    {
                        Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                    }
                    else
                    {
                        if (!RootMessages.ContainsKey(username))
                        {
                            RootMessages[username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                        }
                        Messages = RootMessages[username];
                    }
                }

                if (RootLastReadMessageCounts != null)
                {
                    if (string.IsNullOrEmpty(username))
                    {
                        LastReadMessageCounts = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
                    }
                    else
                    {
                        if (!RootLastReadMessageCounts.ContainsKey(username))
                        {
                            RootLastReadMessageCounts[username] = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
                        }
                        LastReadMessageCounts = RootLastReadMessageCounts[username];
                    }
                }
            }
        }

        public static (List<Message>, int) DeleteMessageFromUserWithUndo(string usernameToDelete)
        {
            Messages.Remove(usernameToDelete, out var deleteMessages);
            LastReadMessageCounts.Remove(usernameToDelete, out int readCount);
            SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
            SaveLastReadCounts(SeekerState.SharedPreferences);
            return (deleteMessages, readCount);
        }

        public static (Dictionary<string, List<Message>> deletedMessageDictionary, Dictionary<string, int> deletedMessageCountDictionary) DeleteAllMessagesWithUndo()
        {
            var deletedAllMessages = MessageController.Messages.ToDictionary(entry => entry.Key, entry => entry.Value);
            var deletedAllLastReadMessageCounts = MessageController.LastReadMessageCounts.ToDictionary(entry => entry.Key, entry => entry.Value);
            Messages.Clear();
            LastReadMessageCounts.Clear();
            SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
            SaveLastReadCounts(SeekerState.SharedPreferences);
            return (deletedAllMessages, deletedAllLastReadMessageCounts);
        }


        private static void Client_PrivateMessageReceived(object sender, Soulseek.PrivateMessageReceivedEventArgs e)
        {
            try
            {
                if (SeekerApplication.IsUserInIgnoreList(e.Username))
                {
                    Logger.Debug("IGNORED PM received: " + e.Username);
                    return;
                }

                //file
                Message msg = new Message(e.Username, e.Id, e.Replayed, e.Timestamp.ToLocalTime(), e.Timestamp, e.Message, false);
                lock (MessageListLockObject)
                {
                    if (string.IsNullOrEmpty(PreferencesState.Username))
                    {
                        Logger.Firebase("we received a message while our username is still null");
                    }

                    if (Messages.ContainsKey(e.Username))
                    {
                        Messages[e.Username].Add(msg);
                    }
                    else
                    {
                        Messages[e.Username] = new List<Message>();
                        Messages[e.Username].Add(msg);
                    }
                }
                //do notification
                //on UI thread..
                ShowNotification(msg);

                SetAsUnreadAndSaveIfApplicable(e.Username);

                //save to prefs
                SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);

                try
                {
                    //raise event
                    MessageReceived?.Invoke(sender, msg); //if this throws it does not crash anything. it will fail silently which is quite bad bc then we never ACK the message.
                }
                catch (Exception error)
                {
                    Logger.Firebase("MessageReceived raise event failed: " + error.Message);
                }

                try
                {
                    SeekerState.SoulseekClient.AcknowledgePrivateMessageAsync(msg.Id).ContinueWith((Action<Task>)LogIfFaulted);
                }
                catch (Exception err)
                {
                    Logger.Firebase("AcknowledgePrivateMessageAsync: " + err.Message);
                }
            }
            catch (Exception exc)
            {
                Logger.Firebase("msg received:" + exc.Message + exc.StackTrace);
            }
        }

        public static void RaiseMessageReceived(Message msg) //normally this is if it is a message from us...
        {
            MessageReceived?.Invoke(null, msg);
        }

        public static void LogIfFaulted(Task t)
        {
            if (t.IsFaulted)
            {
                Logger.Firebase("AcknowledgePrivateMessageAsync faulted: " + t.Exception.Message + t.Exception.StackTrace);
            }
        }


        private static Color GetYouTextColor(bool useNightColors, Context contextToUse)
        {
            //for api 31+ use secondary color
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                if (useNightColors)
                {
                    return contextToUse.Resources.GetColor(Android.Resource.Color.SystemAccent2200, SeekerState.ActiveActivityRef.Theme);
                }
                else
                {
                    return contextToUse.Resources.GetColor(Android.Resource.Color.SystemAccent2600, SeekerState.ActiveActivityRef.Theme);
                }
            }
            else
            {
                if (useNightColors)
                {
                    return Color.White;
                }
                else
                {
                    return Color.Black;
                }
            }
        }

        private static Color GetNiceAndroidBlueNotifColor(bool useNightColors, Context contextToUse)
        {
            var newTheme = contextToUse.Resources.NewTheme();
            newTheme.ApplyStyle(ThemeHelper.GetThemeInChosenDayNightMode(useNightColors, contextToUse), true);
            return UiHelpers.GetColorFromAttribute(contextToUse, Resource.Attribute.android_default_notification_blue_color, newTheme);
        }

        private static Color GetOtherTextColor(bool useNightColors, Context contextToUse)
        {
            //for api 31+ use primary color
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                if (useNightColors)
                {
                    return contextToUse.Resources.GetColor(Android.Resource.Color.SystemAccent1200, SeekerState.ActiveActivityRef.Theme);
                }
                else
                {
                    return contextToUse.Resources.GetColor(Android.Resource.Color.SystemAccent1600, SeekerState.ActiveActivityRef.Theme);
                }
            }
            else
            {
                //todo
                var newTheme = contextToUse.Resources.NewTheme();
                newTheme.ApplyStyle(ThemeHelper.GetThemeInChosenDayNightMode(useNightColors, contextToUse), true);
                return UiHelpers.GetColorFromAttribute(contextToUse, Resource.Attribute.android_default_notification_complementary_color, newTheme);
            }
        }

        private static Color GetActionTextColor(bool useNightColors, Context contextToUse)
        {
            //for api 31+ use primary color
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                return GetOtherTextColor(useNightColors, contextToUse);
            }
            else
            {
                //todo
                if (useNightColors)
                {
                    return Color.White;
                }
                else
                {
                    return Color.Black;
                }
            }
        }

        public static Android.Text.SpannableStringBuilder GetSpannableForCollapsed(MessageNotifExtended messageNotifExtended, bool useNightColors, Context contextToUse)
        {
            Android.Text.SpannableStringBuilder ssb = new Android.Text.SpannableStringBuilder();

            if (messageNotifExtended.IsSpecialMessage)
            {
                string title = String.Format(SeekerApplication.GetString(Resource.String.MessagesWithUser), messageNotifExtended.Username);
                var titleSpan = new Android.Text.SpannableString(title + " \n");
                titleSpan.SetSpan(new Android.Text.Style.ForegroundColorSpan(GetYouTextColor(useNightColors, contextToUse)), 0, title.Length, Android.Text.SpanTypes.InclusiveInclusive);
                titleSpan.SetSpan(new Android.Text.Style.StyleSpan(TypefaceStyle.Bold), 0, title.Length, Android.Text.SpanTypes.InclusiveInclusive);

                ssb.Append(titleSpan);

                var spannableStringError = new Android.Text.SpannableString(messageNotifExtended.MessageText);
                spannableStringError.SetSpan(new Android.Text.Style.ForegroundColorSpan(Color.Red), 0, messageNotifExtended.MessageText.Length, Android.Text.SpanTypes.InclusiveInclusive);
                ssb.Append(spannableStringError);
                return ssb;
            }

            string uname = messageNotifExtended.IsOurMessage ? SeekerApplication.GetString(Resource.String.You) : messageNotifExtended.Username;
            var spannableString = new Android.Text.SpannableString(uname + " ");

            Android.Text.Style.ForegroundColorSpan fcs = null;
            if (messageNotifExtended.IsOurMessage)
            {
                fcs = new Android.Text.Style.ForegroundColorSpan(GetYouTextColor(useNightColors, contextToUse));
            }
            else
            {
                fcs = new Android.Text.Style.ForegroundColorSpan(GetOtherTextColor(useNightColors, contextToUse));
            }
            spannableString.SetSpan(fcs, 0, uname.Length, Android.Text.SpanTypes.InclusiveInclusive);

            var bld = new Android.Text.Style.StyleSpan(TypefaceStyle.Bold);
            spannableString.SetSpan(bld, 0, uname.Length, Android.Text.SpanTypes.InclusiveInclusive);


            ssb.Append(spannableString);
            //var textColorSubdued = new Android.Text.Style.ForegroundColorSpan(Color.White);//UiHelpers.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cellTextColorSubdued));
            string msgToShow = "\n" + messageNotifExtended.MessageText;
            var spannableString2 = new Android.Text.SpannableString(msgToShow);
            //spannableString2.SetSpan(textColorSubdued, 0, msgToShow.Length, SpanTypes.InclusiveInclusive);
            ssb.Append(spannableString2);
            return ssb;
        }


        public static Android.Text.SpannableStringBuilder GetSpannableForExpanded(List<MessageNotifExtended> messageNotifExtended, bool useNightColors, Context contextToUse)
        {
            var lastFive = messageNotifExtended.TakeLast(5); //not nearly enough room to display 8
            string lastUsername = null;
            Android.Text.SpannableStringBuilder ssb = new Android.Text.SpannableStringBuilder();

            bool showErrors = true;
            if (!lastFive.Last().IsSpecialMessage)
            {
                showErrors = false;
            }

            for (int i = 0; i < lastFive.Count(); i++)
            {
                var msg = lastFive.ElementAt(i);

                if (msg.IsSpecialMessage)
                {
                    if (!showErrors)
                    {
                        continue;
                    }
                    else
                    {
                        var spannableString = new Android.Text.SpannableString(msg.MessageText + ((i != lastFive.Count() - 1) ? " \n" : string.Empty));
                        spannableString.SetSpan(new Android.Text.Style.ForegroundColorSpan(Color.Red), 0, msg.MessageText.Length, Android.Text.SpanTypes.InclusiveInclusive);
                        //spannableString.SetSpan(new Android.Text.Style.StyleSpan(TypefaceStyle.Bold), 0, msg.MessageText.Length, Android.Text.SpanTypes.InclusiveInclusive);
                        ssb.Append(spannableString);
                        continue;
                    }
                }
                string uname = msg.IsOurMessage ? SeekerApplication.GetString(Resource.String.You) : msg.Username;
                if (lastUsername != uname)
                {
                    //add header

                    var spannableString = new Android.Text.SpannableString(uname + " \n"); //space after to prevent android bug

                    Android.Text.Style.ForegroundColorSpan fcs = null;
                    if (msg.IsOurMessage)
                    {
                        fcs = new Android.Text.Style.ForegroundColorSpan(GetYouTextColor(useNightColors, contextToUse)); //normal color text...
                    }
                    else
                    {
                        fcs = new Android.Text.Style.ForegroundColorSpan(GetOtherTextColor(useNightColors, contextToUse));
                    }
                    spannableString.SetSpan(fcs, 0, uname.Length, Android.Text.SpanTypes.InclusiveInclusive);

                    var bld = new Android.Text.Style.StyleSpan(TypefaceStyle.Bold);
                    spannableString.SetSpan(bld, 0, uname.Length, Android.Text.SpanTypes.InclusiveInclusive);

                    ssb.Append(spannableString);

                }
                //now append text
                Android.Text.SpannableString spannableString2 = null;
                if (i != lastFive.Count() - 1)
                {
                    spannableString2 = new Android.Text.SpannableString(msg.MessageText + "\n");
                }
                else
                {
                    spannableString2 = new Android.Text.SpannableString(msg.MessageText);
                }
                //var textColorSubdued = new Android.Text.Style.ForegroundColorSpan(Color.White);//UiHelpers.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.cellTextColorSubdued));
                //spannableString2.SetSpan(textColorSubdued, 0, msg.MessageText.Length, SpanTypes.InclusiveInclusive);
                ssb.Append(spannableString2);

                lastUsername = uname;
            }

            return ssb;
        }

        /// <summary>
        /// Will get if the system (i.e. not the app) is in night mode.
        /// Because for notification colors only the system matters!!
        /// </summary>
        /// <returns></returns>
        public static bool GetIfSystemIsInNightMode(Context contextToUse)
        {
            if (PreferencesState.DayNightMode == (int)(AndroidX.AppCompat.App.AppCompatDelegate.ModeNightFollowSystem))
            {
                //if we follow the system then we can just return whether our app is in night mode.
                return DownloadDialog.InNightMode(contextToUse);
            }
            else
            {
                //if we do not follow the system we have to use the UI Mode Service
                UiModeManager uiModeManager = (UiModeManager)contextToUse.GetSystemService(Context.UiModeService);//getSystemService(Context.UI_MODE_SERVICE);
                int mode = (int)(uiModeManager.NightMode);
                if (mode == (int)UiNightMode.Yes)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public const string CHANNEL_ID = "Private Messages ID";
        public const string CHANNEL_NAME = "Private Messages";
        public const string FromUserName = "FromThisUser";
        public const string ComingFromMessageTapped = "FromAMessage";

        public static void ShowNotificationLogic(Message msg, bool fromOurResponse = false, bool directReplyFailure = false, string directReplayFailureReason = "", Context broadcastContext = null)
        {
            try
            {
                Context contextToUse = broadcastContext == null ? SeekerState.ActiveActivityRef : broadcastContext;
                if (contextToUse == null)
                {
                    contextToUse = SeekerApplication.ApplicationContext;
                }
                CommonHelpers.CreateNotificationChannel(contextToUse, CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High); //only high will "peek"


                Intent notifIntent = new Intent(contextToUse, typeof(MessagesActivity));
                notifIntent.AddFlags(ActivityFlags.SingleTop);
                notifIntent.PutExtra(FromUserName, msg.Username); //so we can go to this user..
                notifIntent.PutExtra(ComingFromMessageTapped, true); //so we can go to this user..
                PendingIntent pendingIntent =
                    PendingIntent.GetActivity(contextToUse, msg.Username.GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                NotificationManagerCompat notificationManager = NotificationManagerCompat.From(contextToUse);

                //no direct reply in <26 and so the actions are rather pointless..
                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                {

                    bool systemIsInNightMode = GetIfSystemIsInNightMode(contextToUse);


                    AndroidX.Core.App.RemoteInput remoteInput = new AndroidX.Core.App.RemoteInput.Builder("key_text_result").SetLabel(SeekerApplication.GetString(Resource.String.sendmessage_)).Build();
                    Intent replayIntent = new Intent(contextToUse, typeof(MessagesBroadcastReceiver)); //TODO TODO we need a broadcast receiver...
                    replayIntent.PutExtra("direct_reply_extra", true);
                    replayIntent.SetAction("seeker_direct_reply");
                    replayIntent.PutExtra("seeker_username", msg.Username);
                    PendingIntent replyPendingIntent = PendingIntent.GetBroadcast(contextToUse, msg.Username.GetHashCode(), replayIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, false)); //mutable, the end user needs to be able to mutate with direct replay action..
                    NotificationCompat.Action replyAction = new NotificationCompat.Action.Builder(Resource.Drawable.baseline_chat_bubble_white_24, "Reply", replyPendingIntent).SetAllowGeneratedReplies(false).AddRemoteInput(remoteInput).Build(); //TODO icon


                    //NotificationCompat.MessagingStyle messagingStyle = new NotificationCompat.MessagingStyle("me").SetConversationTitle("hi hello there").SetGroupConversation(true);

                    var mne = new MessageNotifExtended() { Username = msg.Username, IsOurMessage = fromOurResponse, IsSpecialMessage = directReplyFailure, MessageText = directReplyFailure ? directReplayFailureReason : msg.MessageText };

                    //if(!directReplyFailure)
                    //{
                    if (MessagesActivity.DirectReplyMessages.ContainsKey(msg.Username))
                    {
                        MessagesActivity.DirectReplyMessages[msg.Username].Add(mne);
                    }
                    else
                    {
                        MessagesActivity.DirectReplyMessages[msg.Username] = new List<MessageNotifExtended>();
                        MessagesActivity.DirectReplyMessages[msg.Username].Add(mne);
                    }
                    //}


                    //foreach (NotificationCompat.MessagingStyle.Message message in MessagesActivity.DirectReplyMessages[msg.Username])
                    //{
                    //    messagingStyle.AddMessage(message);
                    //}


                    RemoteViews notificationLayout = new RemoteViews(contextToUse.PackageName, Resource.Layout.simple_custom_notification);
                    RemoteViews notificationLayoutExpanded = new RemoteViews(contextToUse.PackageName, Resource.Layout.simple_custom_notification);

                    notificationLayout.SetTextViewText(Resource.Id.textView1, GetSpannableForCollapsed(MessagesActivity.DirectReplyMessages[msg.Username].Last(), systemIsInNightMode, contextToUse));
                    notificationLayoutExpanded.SetTextViewText(Resource.Id.textView1, GetSpannableForExpanded(MessagesActivity.DirectReplyMessages[msg.Username], systemIsInNightMode, contextToUse));


                    Intent clearNotifIntent = new Intent(contextToUse, typeof(MessagesBroadcastReceiver)); //TODO TODO we need a broadcast receiver...
                    clearNotifIntent.PutExtra("clear_notif_extra", true);
                    clearNotifIntent.SetAction("seeker_clear_notification");
                    clearNotifIntent.PutExtra("seeker_username", msg.Username);
                    PendingIntent clearNotifPendingIntent = PendingIntent.GetBroadcast(contextToUse, msg.Username.GetHashCode(), clearNotifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));



                    Intent markAsReadIntent = new Intent(contextToUse, typeof(MessagesBroadcastReceiver)); //TODO TODO we need a broadcast receiver...
                    markAsReadIntent.PutExtra("mark_as_read_extra", true);
                    markAsReadIntent.SetAction("seeker_mark_as_read");


                    markAsReadIntent.PutExtra("seeker_username", msg.Username);
                    PendingIntent markAsReadPendingIntent = PendingIntent.GetBroadcast(contextToUse, msg.Username.GetHashCode(), markAsReadIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true)); //else the new extras will not arrive...

                    string markAsRead = "Mark As Read";
                    //android messages app does "mark as read" even after you respond so I think it is fine..
                    //if (fromOurResponse)
                    //{
                    //    markAsRead = "Dismiss";
                    //}

                    //setColor ?? todo
                    NotificationCompat.Builder builder = new NotificationCompat.Builder(contextToUse, CHANNEL_ID)
                        .AddAction(Resource.Drawable.baseline_chat_bubble_white_24, markAsRead, markAsReadPendingIntent)
                        .AddAction(replyAction)
                        .SetStyle(new NotificationCompat.DecoratedCustomViewStyle())
                        .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                        //.SetCategory(NotificationCompat.CategoryMessage)
                        .SetContentIntent(pendingIntent)
                        .SetCustomContentView(notificationLayout)
                        .SetCustomBigContentView(notificationLayoutExpanded)
                        .SetAutoCancel(true) //so when we tap it will go away. does not apply to actions though.
                        .SetOnlyAlertOnce(fromOurResponse) //it will make noise on new messages...
                        .SetDeleteIntent(clearNotifPendingIntent);

                    //if android 12+ let the system pick the color.  it will make it Android.Resource.Color.SystemAccent1100 if dark Android.Resource.Color.SystemAccent1600 otherwise.
                    if (!OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        builder.SetColor(GetNiceAndroidBlueNotifColor(systemIsInNightMode, contextToUse));
                    }

                    var notification = builder.Build();

                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify(msg.Username.GetHashCode(), notification);

                }
                else
                {
                    Notification n = CommonHelpers.CreateNotification(contextToUse, pendingIntent, CHANNEL_ID, $"Message from {msg.Username}", msg.MessageText, false); //TODO
                    notificationManager.Notify(msg.Username.GetHashCode(), n);
                }
            }
            catch (System.Exception e)
            {
                Logger.Firebase("ShowNotification failed: " + e.Message + e.StackTrace);
            }

        }

        public static void ShowNotification(Message msg, bool fromOurResponse = false, bool directReplyFailure = false, string directReplayFailureMessage = "", Context broadcastContext = null)
        {
            BroadcastFriendlyRunOnUiThread(() =>
            {
                ShowNotificationLogic(msg, fromOurResponse, directReplyFailure, directReplayFailureMessage, broadcastContext);
            });
        }

        public static void SaveMessagesToSharedPrefs(ISharedPreferences sharedPrefs)
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            if (RootMessages == null)
            {
                return;
            }
            string messagesString = string.Empty;
            lock (MessageListLockObject)
            {
                messagesString = SerializationHelper.SaveMessagesToString(RootMessages);

            }
            if (messagesString != null && messagesString != string.Empty)
            {
                PreferencesManager.SaveMessages(messagesString);
            }
        }

        public static void RestoreMessagesFromSharedPrefs(ISharedPreferences sharedPrefs)
        {
            string messages = sharedPrefs.GetString(KeyConsts.M_Messages, string.Empty);
            if (messages == string.Empty)
            {
                RootMessages = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>>();
                Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                if (!string.IsNullOrEmpty(PreferencesState.Username))
                {
                    RootMessages[PreferencesState.Username] = Messages;
                }
            }
            else
            {
                RootMessages = SerializationHelper.RestoreMessagesFromString(messages);
                MessagesUsername = PreferencesState.Username;
                Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                if (!string.IsNullOrEmpty(PreferencesState.Username) && RootMessages.ContainsKey(PreferencesState.Username))
                {
                    Messages = RootMessages[PreferencesState.Username];
                } 
                else if(!string.IsNullOrEmpty(PreferencesState.Username))
                {
                    RootMessages[MessagesUsername] = Messages;
                }
            }
        }

        public static void RestoreLastReadCountsFromSharedPrefs(ISharedPreferences sharedPrefs)
        {
            string lastReadCounts = sharedPrefs.GetString(KeyConsts.M_LastReadMessageCounts, string.Empty);
            RootLastReadMessageCounts = SerializationHelper.RestoreLastReadCountsFromString(lastReadCounts);

            if (!string.IsNullOrEmpty(PreferencesState.Username))
            {
                if (RootLastReadMessageCounts.ContainsKey(PreferencesState.Username))
                {
                    LastReadMessageCounts = RootLastReadMessageCounts[PreferencesState.Username];
                }
                else
                {
                    LastReadMessageCounts = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
                    RootLastReadMessageCounts[PreferencesState.Username] = LastReadMessageCounts;
                }
            }
            else
            {
                LastReadMessageCounts = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
            }
        }

        public static void SaveLastReadCounts(ISharedPreferences sharedPrefs)
        {
            if (RootLastReadMessageCounts == null)
            {
                return;
            }
            if (RootLastReadMessageCounts.IsEmpty)
            {
                PreferencesManager.SaveLastReadMessageCounts(String.Empty);
            }
            else
            {
                var messagesString = SerializationHelper.SaveLastReadCountsToString(RootLastReadMessageCounts);
                if (!string.IsNullOrEmpty(messagesString))
                {
                    PreferencesManager.SaveLastReadMessageCounts(messagesString);
                }
            }
        }

        public static int GetUnreadCount(string username)
        {
            var messages = Messages;
            var lastReadCounts = LastReadMessageCounts;
            if (messages == null || !messages.TryGetValue(username, out var msgList))
            {
                return 0;
            }
            int currentCount = msgList.Count;
            int lastRead = lastReadCounts != null && lastReadCounts.TryGetValue(username, out int val) ? val : 0;
            return Math.Max(currentCount - lastRead, 0);
        }

        public static int GetTotalUnreadCount()
        {
            var messages = Messages;
            var lastReadCounts = LastReadMessageCounts;
            if (messages == null || lastReadCounts == null)
            {
                return 0;
            }
            int total = 0;
            foreach (var kvp in messages)
            {
                int currentCount = kvp.Value.Count;
                int lastRead = lastReadCounts.TryGetValue(kvp.Key, out int val) ? val : 0;
                total += Math.Max(currentCount - lastRead, 0);
            }
            return total;
        }

        public static void SetAsUnreadAndSaveIfApplicable(string username)
        {
            if (MessagesInnerFragment.currentlyResumed && MessagesInnerFragment.Username == username)
            {
                //if we are already at this user then update last-read count to current.
                MarkAsRead(username);
                return;
            }
            // No action needed — the gap between lastRead and current count IS the unread indicator.
        }

        public static void UnsetAsUnreadAndSaveIfApplicable(string username)
        {
            MarkAsRead(username);
        }

        public static void MarkAsRead(string username)
        {
            if (Messages != null && Messages.TryGetValue(username, out var msgList))
            {
                LastReadMessageCounts[username] = msgList.Count;
            }
            SaveLastReadCounts(SeekerState.SharedPreferences);
        }

        public static void BroadcastFriendlyRunOnUiThread(Action action)
        {
            if (SeekerState.ActiveActivityRef != null)
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(action);
            }
            else
            {
                new Handler(Looper.MainLooper).Post(action);
            }
        }

        public static void SendMessageAPI(Message msg, bool fromDirectReplyAction = false, Android.Content.Context broadcastContext = null)
        {
            //if the seeker process is hard killed (i.e. go to Running Services > kill) and the notification is still up,
            //then soulseekclient will be good, but the activeActivityRef will be null. so use the broadcastContext.

            Android.Content.Context contextToUse = broadcastContext == null ? SeekerState.ActiveActivityRef : broadcastContext;

            Logger.Debug("is soulseekclient null: " + (SeekerState.SoulseekClient == null).ToString());
            Logger.Debug("is ActiveActivityRef null: " + (SeekerState.ActiveActivityRef == null).ToString());


            if (string.IsNullOrEmpty(msg.MessageText))
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_type_text_to_send), ToastLength.Short);
                if (fromDirectReplyAction)
                {
                    ShowNotification(msg, true, true, "Failure - Message Text is Empty.");
                }
                return;
            }
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                Logger.Debug("not currently logged in");
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_send_message), ToastLength.Short);
                if (fromDirectReplyAction)
                {
                    ShowNotification(msg, true, true, "Failure - Currently Logged Out.");
                }
                return;
            }

            Action<Task> actualActionToPerform = new Action<Task>((Task t) =>
            {

                Logger.Debug("our continue with action is occuring!...");
                if (t.IsFaulted)
                {
                    if (!(t.Exception.InnerException is FaultPropagationException))
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                    }
                    if (fromDirectReplyAction)
                    {
                        ShowNotification(msg, true, true, "Failure - Cannot Log In.");
                    }
                    throw new FaultPropagationException();
                }
                BroadcastFriendlyRunOnUiThread(new Action(() =>
                {
                    SendMessageLogic(msg, fromDirectReplyAction, broadcastContext);
                }));
            });

            SessionService.Instance.RunWithReconnect(actualActionToPerform, "Message will send on connection re-establishment", contextToUse);

        }

        public static void SendMessageLogic(Message msg, bool fromDirectReplyAction, Android.Content.Context broadcastContext = null) //you can start out with a message...
        {
            Logger.Debug("SendMessageLogic");

            string usernameToMessage = msg.Username;
            if (Messages.Keys.Contains(usernameToMessage))
            {
                Messages[usernameToMessage].Add(msg);
            }
            else
            {
                Messages[usernameToMessage] = new List<Message>(); //our first message to them..
                Messages[usernameToMessage].Add(msg);
            }
            MarkAsRead(usernameToMessage);
            SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
            RaiseMessageReceived(msg);
            Action<Task> continueWithAction = new Action<Task>((Task t) =>
            {
                if (t.IsFaulted)
                {
                    Logger.Debug("faulted " + t.Exception.ToString());
                    Logger.Debug("faulted " + t.Exception.InnerException.Message.ToString());
                    msg.SentMsgStatus = SentStatus.Failed;
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_send_message), ToastLength.Long); //TODO

                    if (fromDirectReplyAction)
                    {
                        ShowNotification(msg, true, true, "Failure - Cannot Send Message.", broadcastContext);
                    }
                }
                else
                {
                    Logger.Debug("did not fault");
                    msg.SentMsgStatus = SentStatus.Success;

                    if (fromDirectReplyAction)
                    {
                        ShowNotification(msg, true, false, string.Empty, broadcastContext);
                    }
                }
                SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
                RaiseMessageReceived(msg);
            });
            Logger.Debug("useranme to mesasge " + usernameToMessage);
            SeekerState.SoulseekClient.SendPrivateMessageAsync(usernameToMessage, msg.MessageText).ContinueWith(continueWithAction);
        }

        public static void UndoDeleteMessagesFromUser((string username, List<Message> messages, int readCount) deletedData)
        {
            Messages[deletedData.username] = deletedData.messages;
            LastReadMessageCounts[deletedData.username] = deletedData.readCount;
            SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
            SaveLastReadCounts(SeekerState.SharedPreferences);
        }
    }
}