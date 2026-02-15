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

using Seeker.Helpers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Android.Runtime;
using Android.Text;
using Google.Android.Material.Snackbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Seeker.Messages;
using AndroidX.Activity;

namespace Seeker
{

    [Activity(Label = "MessagesActivity", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask, Exported = false)]
    public class MessagesActivity : SlskLinkMenuActivity//, Android.Widget.PopupMenu.IOnMenuItemClickListener
    {
        public static MessagesActivity MessagesActivityRef = null;

        /// <summary>
        /// basically this keeps track of the direct reply messages stack.
        /// if a user replies to a message from notification or gets a new one when the notificaiton is up, then it gets added to.
        /// but once the user clears the notification OR goes to the activity to respond to the message then it gets cleared.
        /// </summary>
        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<MessageController.MessageNotifExtended>> DirectReplyMessages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<MessageController.MessageNotifExtended>>();

        public void ChangeToInnerFragment(string username)
        {
            if (IsFinishing || IsDestroyed)
            {
                return;
            }
            else
            {
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesInnerFragment(username), "InnerUserFragment").AddToBackStack("InnerUserFragmentBackStack").Commit();
            }
        }

        bool savedStateAtInner = false;
        protected override void OnSaveInstanceState(Bundle outState)
        {
            var f = SupportFragmentManager.FindFragmentByTag("InnerUserFragment");
            if (f != null && f.IsVisible)
            {
                outState.PutBoolean("SaveStateAtInner", true);
            }
            else
            {
                outState.PutBoolean("SaveStateAtInner", false);
            }
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            var fOuter = SupportFragmentManager.FindFragmentByTag("OuterUserFragment");
            var fInner = SupportFragmentManager.FindFragmentByTag("InnerUserFragment");
            if (fOuter != null && fOuter.IsVisible)
            {
                MenuInflater.Inflate(Resource.Menu.messages_overview_list_menu, menu);
            }
            else if (fInner != null && fInner.IsVisible)
            {
                MenuInflater.Inflate(Resource.Menu.messages_inner_list_menu, menu);

            }
            else
            {
                MenuInflater.Inflate(Resource.Menu.messages_overview_list_menu, menu);
            }
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            CommonHelpers.SetMenuTitles(menu, MessagesInnerFragment.Username);
            CommonHelpers.SetIgnoreAddExclusive(menu, MessagesInnerFragment.Username);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (CommonHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), MessagesInnerFragment.Username, this, this.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId)))
            {
                return true;
            }
            switch (item.ItemId)
            {
                case Resource.Id.message_user_action:
                    ShowEditTextMessageUserDialog();
                    return true;
                case Resource.Id.action_add_to_user_list:
                    UserListActivity.AddUserAPI(this, MessagesInnerFragment.Username, new Action(() => { Toast.MakeText(this, Resource.String.success_added_user, ToastLength.Short).Show(); }));
                    return true;
                case Resource.Id.action_search_files:
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = MessagesInnerFragment.Username;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToSearch, 1);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.action_browse_files:
                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                    });
                    View snackView = this.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId);
                    DownloadDialog.RequestFilesApi(MessagesInnerFragment.Username, snackView, action, null);
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressedDispatcher.OnBackPressed();
                    return true;
                case Resource.Id.action_delete_messages:
                    DELETED_USERNAME = MessagesInnerFragment.Username;
                    DELETED_POSITION = int.MaxValue;
                    MessageController.Messages.Remove(MessagesActivity.DELETED_USERNAME, out DELETED_DATA);
                    MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
                    this.SwitchToOuter(SupportFragmentManager.FindFragmentByTag("InnerUserFragment"), true);
                    return true;
                case Resource.Id.action_delete_all_messages:
                    if (MessageController.Messages.Count == 0) //nullref
                    {
                        Toast.MakeText(this, this.GetString(Resource.String.deleted_all_no_messages), ToastLength.Long).Show();
                        return true;
                    }
                    DELETED_DICTIONARY = MessageController.Messages.ToDictionary(entry => entry.Key, entry => entry.Value);
                    MessageController.Messages.Clear();
                    MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
                    this.GetOverviewFragment().RefreshAdapter();
                    Snackbar sb = Snackbar.Make(this.GetOverviewFragment().View, SeekerState.ActiveActivityRef.GetString(Resource.String.deleted_all_messages), Snackbar.LengthLong).SetAction("Undo", GetUndoDeleteAllSnackBarAction()).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                    (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                    sb.Show();
                    return true;

            }
            return base.OnOptionsItemSelected(item);
        }

        //note: when the undo snackbar is up and you click into an inner then the snackbar is still there, I tested it and clicking undo works properly in this case :)


        public Action<View> GetUndoDeleteAllSnackBarAction()
        {
            Action<View> undoSnackBarAction = new Action<View>((View v) =>
            {
                if (MessagesActivity.DELETED_DICTIONARY == null)
                {
                    //error
                    bool isNull = MessagesActivity.DELETED_DICTIONARY == null;
                    Logger.Firebase("failure on undo delete all. dict was null");
                    Toast.MakeText(v.Context, Resource.String.failed_to_undo, ToastLength.Short).Show();
                    return;
                }

                foreach (var entry in MessagesActivity.DELETED_DICTIONARY)
                {
                    MessageController.Messages[entry.Key] = entry.Value;
                }

                MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
                (SeekerState.ActiveActivityRef as MessagesActivity).GetOverviewFragment().RefreshAdapter();
                MessagesActivity.DELETED_DICTIONARY = null;
            });
            return undoSnackBarAction;
        }

        public MessagesOverviewFragment GetOverviewFragment()
        {
            return (SupportFragmentManager.FindFragmentByTag("OuterUserFragment") as MessagesOverviewFragment);
        }

        private static AndroidX.AppCompat.App.AlertDialog messageUserDialog = null;

        public void ShowEditTextMessageUserDialog()
        {
            if (MainActivity.IsNotLoggedIn())
            {
                Toast.MakeText(this, Resource.String.must_be_logged_to_send_message, ToastLength.Short).Show();
                return;
            }

            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(SeekerState.ActiveActivityRef.GetString(Resource.String.msg_user) + ":");
            // I'm using fragment here so I'm using getView() to provide ViewGroup
            // but you can provide here any other instance of ViewGroup from your Fragment / Activity
            var rootView = (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView;
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.message_chosen_user, rootView, false);
            // Set up the input
            AutoCompleteTextView input = (AutoCompleteTextView)viewInflated.FindViewById<EditText>(Resource.Id.chosenUserEditText);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(input);
            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string userToMessage = input.Text;
                if (userToMessage == null || userToMessage == string.Empty)
                {
                    Toast.MakeText(this, Resource.String.must_type_a_username_to_message, ToastLength.Short).Show();
                    if ((sender as AndroidX.AppCompat.App.AlertDialog) == null)
                    {
                        messageUserDialog.Dismiss();
                    }
                    else
                    {
                        (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    }
                    return;
                }

                SeekerState.RecentUsersManager.AddUserToTop(userToMessage, true);

                //Do Logic of going to Username View
                this.ChangeToInnerFragment(userToMessage);


                //DownloadDialog.RequestFilesApi(userToMessage, this.View, goSnackBarAction, null);

                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    messageUserDialog.Dismiss();
                }
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    messageUserDialog.Dismiss();
                }
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search) //ImeNull if being called due to the enter key being pressed. (MSDN) but ImeNull gets called all the time....
                {
                    Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)this.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(this.FindViewById(Android.Resource.Id.Content).RootView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Firebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Message User logic
                    eventHandler(sender, null);
                }
            };


            System.EventHandler<TextView.KeyEventArgs> keypressAction = (object sender, TextView.KeyEventArgs e) =>
            {
                if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
                {
                    Logger.Debug("keypress: " + e.Event.KeyCode.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(this.FindViewById(Android.Resource.Id.Content).RootView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Firebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Browse Logic...
                    eventHandler(sender, null);
                }
                else
                {
                    e.Handled = false;
                }
            };

            input.KeyPress += keypressAction;
            input.EditorAction += editorAction;
            input.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.cancel, eventHandlerCancel);
            // Set up the buttons

            messageUserDialog = builder.Create();
            messageUserDialog.Show();
            CommonHelpers.DoNotEnablePositiveUntilText(messageUserDialog, input);
        }

        private void Input_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SeekerState.ActiveActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                Logger.Firebase("MainActivity_FocusChange" + err.Message);
            }
        }


        private void onBackPressedAction(OnBackPressedCallback callback)
        {
            //if f is non null and f is visible then that means you are backing out from the inner user fragment..
            var f = SupportFragmentManager.FindFragmentByTag("InnerUserFragment");
            if (f != null && f.IsVisible)
            {
                if (SupportFragmentManager.BackStackEntryCount == 0) //this is if we got to inner messages through a notification, in which case we are done..
                {
                    bool root = IsTaskRoot;
                    Logger.Debug("IS TASK ROOT: " + root); //returns false if there is in fact a task behind it (such as the main activity task).
                    if (IsTaskRoot) //it is TRUE if we swiped seeker from task list and then later followed a notification..
                    {
                        Intent intent = new Intent(this, typeof(MainActivity));
                        intent.AddFlags(ActivityFlags.ClearTop);
                        this.StartActivity(intent);
                        this.Finish(); //without this, pressing back just launches the main activity (messages will still be behind it)
                        //and so you can go back infinitely, it will show messages behind it, then it will launch main again, then messages behind it, etc.
                        return;
                    }
                    else
                    {
                        callback.Enabled = false;
                        OnBackPressedDispatcher.OnBackPressed();
                        callback.Enabled = true;
                        return;
                    }
                }
                AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.messages_toolbar);
                myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
                myToolbar.Title = SeekerState.ActiveActivityRef.GetString(Resource.String.messages);
                this.SetSupportActionBar(myToolbar);
                this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                this.SupportActionBar.SetHomeButtonEnabled(true);
                SupportFragmentManager.BeginTransaction().Remove(f).Commit();

                //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesOverviewFragment(), "OuterUserFragment").Commit();
            }
            callback.Enabled = false;
            OnBackPressedDispatcher.OnBackPressed();
            callback.Enabled = true;
        }


        //Delete Undo Helpers
        public static string DELETED_USERNAME = string.Empty;
        public static int DELETED_POSITION = -1;
        public static List<Message> DELETED_DATA = null;
        public static volatile bool FromDeleteMessage = false;
        //for delete all
        public static Dictionary<string, List<Message>> DELETED_DICTIONARY = null;

        /// <summary>
        /// This method will switch you from inner to outer.  If you came to inner from a notification, outer will be added.
        /// note: whenever we go back we recreate the fragment so we dont need to mess around with the adapter (in the case of delete), it will be recreated.
        /// </summary>
        /// <param name="innerFragment"></param>
        public void SwitchToOuter(AndroidX.Fragment.App.Fragment innerFragment, bool forDeleteMessage)
        {
            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.messages_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
            myToolbar.Title = SeekerState.ActiveActivityRef.GetString(Resource.String.messages);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            var outerExists = SupportFragmentManager.FindFragmentByTag("OuterUserFragment");
            FromDeleteMessage = forDeleteMessage;
            if (outerExists != null)
            {
                SupportFragmentManager.PopBackStack();
            }
            else
            {
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesOverviewFragment(), "OuterUserFragment").Commit();
            }
            //this.SupportActionBar.InvalidateOptionsMenu(); occurs to soon... outer fragment is not yet visible...
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.Intent = intent;
            if (intent.GetBooleanExtra(MessageController.ComingFromMessageTapped, false))
            {
                string goToUsersMessages = intent.GetStringExtra(MessageController.FromUserName);
                if (goToUsersMessages == string.Empty)
                {
                    Logger.Firebase("empty goToUsersMessages");
                }
                else
                {
                    SupportFragmentManager.BeginTransaction().Remove(new MessagesInnerFragment()).Commit();
                    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesInnerFragment(goToUsersMessages), "InnerUserFragment").Commit();
                    //switch in that fragment...
                    //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame,new MessagesOverviewFragment()).Commit();
                }
            }
        }

        protected override void OnResume()
        {
            if (SeekerState.Username != MessageController.MessagesUsername && MessageController.RootMessages != null)
            {
                MessageController.MessagesUsername = SeekerState.Username;
                MessageController.Messages = MessageController.RootMessages[SeekerState.Username]; //username can be null here... perhaps restarting the app without internet or such...
            }
            base.OnResume();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var backPressedCallback = new GenericOnBackPressedCallback(true, onBackPressedAction);
            OnBackPressedDispatcher.AddCallback(backPressedCallback);

            bool reborn = false;
            if (savedInstanceState == null)
            {
                Logger.Debug("Messages Activity On Create NEW");
            }
            else
            {
                reborn = true;
                Logger.Debug("Messages Activity On Create REBORN");
            }

            MessagesActivityRef = this;
            SeekerState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.messages_main_layout);


            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.messages_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
            myToolbar.Title = SeekerState.ActiveActivityRef.GetString(Resource.String.messages);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            //this.SupportActionBar.SetDisplayShowHomeEnabled(true);

            if (MessageController.RootMessages == null)
            {
                var sharedPref = this.GetSharedPreferences(Constants.SharedPrefFile, 0);
                MessageController.RestoreMessagesFromSharedPrefs(sharedPref);
                if (SeekerState.Username != null && SeekerState.Username != string.Empty)
                {
                    MessageController.MessagesUsername = SeekerState.Username;
                    if (!MessageController.RootMessages.ContainsKey(SeekerState.Username))
                    {
                        MessageController.RootMessages[SeekerState.Username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                    }
                    else
                    {
                        MessageController.Messages = MessageController.RootMessages[SeekerState.Username];
                    }
                }
            }
            else if (SeekerState.Username != MessageController.MessagesUsername)
            {
                MessageController.MessagesUsername = SeekerState.Username;
                if (SeekerState.Username == null || SeekerState.Username == string.Empty)
                {
                    MessageController.Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                }
                else
                {
                    if (MessageController.RootMessages.ContainsKey(SeekerState.Username))
                    {
                        MessageController.Messages = MessageController.RootMessages[SeekerState.Username];
                    }
                    else
                    {
                        MessageController.RootMessages[SeekerState.Username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                        MessageController.Messages = MessageController.RootMessages[SeekerState.Username];
                    }
                }
            }
            bool startWithUserFragment = false;

            if (savedInstanceState != null && savedInstanceState.GetBoolean("SaveStateAtInner"))
            {
                startWithUserFragment = true;
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesInnerFragment(MessagesInnerFragment.Username), "InnerUserFragment").Commit();
                //savedInstanceState.Clear(); //else we will keep doing the first even if the second was done by intent..
            }
            else if (Intent != null) //if an intent started this activity
            {
                if (Intent.GetBooleanExtra(MessageController.ComingFromMessageTapped, false))
                {
                    Logger.Debug("coming from message tapped");
                    string goToUsersMessages = Intent.GetStringExtra(MessageController.FromUserName);
                    if (goToUsersMessages == string.Empty)
                    {
                        Logger.Firebase("empty goToUsersMessages");
                    }
                    else
                    {
                        startWithUserFragment = true;
                        SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesInnerFragment(goToUsersMessages), "InnerUserFragment").Commit();
                        //switch in that fragment...
                        //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame,new MessagesOverviewFragment()).Commit();
                    }
                }
            }

            if (!startWithUserFragment)
            {
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesOverviewFragment(), "OuterUserFragment").Commit();
            }

            //this.SupportActionBar.SetBackgroundDrawable turn off overflow....

            //ListView userList = this.FindViewById<ListView>(Resource.Id.userList);

            //RefreshUserList();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public enum SentStatus
    {
        None = 0,
        Pending = 1,
        Failed = 2,
        Success = 3,
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public enum SpecialMessageCode
    {
        None = 0,
        Reconnect = 1,
        Disconnect = 2,
    }

    [BroadcastReceiver(Exported = false, Label = "OurMessagesBroadcastReceiver")]
    public class MessagesBroadcastReceiver : BroadcastReceiver
    {
        /// <summary>
        /// Just in case we press it while on the message overview page
        /// </summary>
        public static EventHandler<string> MarkAsReadFromNotification;

        public override void OnReceive(Context context, Intent intent)
        {
            //bool directReply = intent.GetBooleanExtra("direct_reply_extra",false);

            //bool markAsRead = intent.GetBooleanExtra("mark_as_read_extra", false);
            string uname = intent.GetStringExtra("seeker_username");

            //bool delete = intent.GetBooleanExtra("clear_notif_extra", false);

            bool delete = intent.Action == "seeker_clear_notification";
            bool markAsRead = intent.Action == "seeker_mark_as_read";
            bool directReply = intent.Action == "seeker_direct_reply";

            Logger.Debug(intent.Action == null ? "MessagesBroadcastReceiver null" : ("MessagesBroadcastReceiver " + intent.Action));

            if (delete)
            {
                MessagesActivity.DirectReplyMessages.TryRemove(uname, out _);
                return;
            }



            if (markAsRead)
            {
                MessageController.UnreadUsernames.TryRemove(uname, out _);

                NotificationManagerCompat notificationManager = NotificationManagerCompat.From(context);
                // notificationId is a unique int for each notification that you must define
                notificationManager.Cancel(uname.GetHashCode());

                MarkAsReadFromNotification?.Invoke(null, uname);

                return;
            }

            Bundle remoteInputBundle = AndroidX.Core.App.RemoteInput.GetResultsFromIntent(intent);
            if (directReply)
            {
                MessageController.UnreadUsernames.TryRemove(uname, out _);
                if (remoteInputBundle != null)
                {
                    string replyText = remoteInputBundle.GetString("key_text_result");
                    //Message msg = new Message(SeekerState.Username, -1, false, Helpers.GetDateTimeNowSafe(), DateTime.UtcNow, replyText, false);
                    Logger.Debug("direct reply " + replyText + " " + uname);
                    MessagesInnerFragment.SendMessageAPI(new Message(uname, -1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, replyText, true, SentStatus.Pending), true, context);


                }
            }
        }
    }


}