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

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Snackbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace AndriodApp1
{
    
    [Activity(Label = "MessagesActivity", Theme = "@style/AppTheme.NoActionBar",LaunchMode =Android.Content.PM.LaunchMode.SingleTask)]
    public class MessagesActivity : Android.Support.V7.App.AppCompatActivity//, Android.Widget.PopupMenu.IOnMenuItemClickListener
    {
        public static MessagesActivity MessagesActivityRef = null;

        public void ChangeToInnerFragment(string username)
        {
            if(IsFinishing || IsDestroyed)
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
            if(f != null && f.IsVisible)
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
            if(fOuter!=null && fOuter.IsVisible)
            {
                MenuInflater.Inflate(Resource.Menu.messages_overview_list_menu, menu);
            }
            else if(fInner!= null && fInner.IsVisible)
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
            Helpers.SetMenuTitles(menu, MessagesInnerFragment.Username);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(Helpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), MessagesInnerFragment.Username,this, this.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId)))
            {
                return true;
            }
            switch (item.ItemId)
            {
                case Resource.Id.message_user_action:
                    ShowEditTextMessageUserDialog();
                    return true;
                case Resource.Id.action_add_to_user_list:
                    UserListActivity.AddUserAPI(this, MessagesInnerFragment.Username,new Action(()=>{Toast.MakeText(this,Resource.String.success_added_user, ToastLength.Short).Show(); }));
                    return true;
                case Resource.Id.action_search_files:
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = MessagesInnerFragment.Username;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SoulSeekState.MainActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToSearch, 1);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.action_browse_files:
                    Action<View> action = new Action<View>((v) => {
                        Intent intent = new Intent(SoulSeekState.MainActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                        //((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    View snackView = this.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId);
                    DownloadDialog.RequestFilesApi(MessagesInnerFragment.Username, snackView, action, null);
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    return true;
                case Resource.Id.action_delete_messages:
                    DELETED_USERNAME = MessagesInnerFragment.Username;
                    DELETED_POSITION = int.MaxValue;
                    MessageController.Messages.Remove(MessagesActivity.DELETED_USERNAME, out DELETED_DATA);
                    MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
                    this.SwitchToOuter(SupportFragmentManager.FindFragmentByTag("InnerUserFragment"),true);
                    return true;
                case Resource.Id.action_delete_all_messages:
                    if(MessageController.Messages.Count==0)
                    {
                        Toast.MakeText(this,this.GetString(Resource.String.deleted_all_no_messages),ToastLength.Long).Show();
                        return true;
                    }
                    DELETED_DICTIONARY = MessageController.Messages.ToDictionary(entry=>entry.Key, entry=>entry.Value);
                    MessageController.Messages.Clear();
                    MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
                    this.GetOverviewFragment().RefreshAdapter();
                    Snackbar sb = Snackbar.Make(this.GetOverviewFragment().View, SoulSeekState.ActiveActivityRef.GetString(Resource.String.deleted_all_messages), Snackbar.LengthLong).SetAction("Undo", GetUndoDeleteAllSnackBarAction()).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                    (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(Android.Graphics.Color.ParseColor("#BCC1F7"));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                    sb.Show();
                    return true;

            }
            return base.OnOptionsItemSelected(item);
        }

        //note: when the undo snackbar is up and you click into an inner then the snackbar is still there, I tested it and clicking undo works properly in this case :)


        public Action<View> GetUndoDeleteAllSnackBarAction()
        {
            Action<View> undoSnackBarAction = new Action<View>((View v) => {
                if (MessagesActivity.DELETED_DICTIONARY == null)
                {
                    //error
                    bool isNull = MessagesActivity.DELETED_DICTIONARY == null;
                    MainActivity.LogFirebase("failure on undo delete all. dict was null");
                    Toast.MakeText(v.Context, Resource.String.failed_to_undo, ToastLength.Short).Show();
                    return;
                }

                foreach(var entry in MessagesActivity.DELETED_DICTIONARY)
                {
                    MessageController.Messages[entry.Key] = entry.Value;
                }

                MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
                (SoulSeekState.ActiveActivityRef as MessagesActivity).GetOverviewFragment().RefreshAdapter();
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
            if(SoulSeekState.Username==null || SoulSeekState.Username == string.Empty)
            {
                Toast.MakeText(this, Resource.String.must_be_logged_to_send_message, ToastLength.Short).Show();
                return;
            }

            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(SoulSeekState.ActiveActivityRef.GetString(Resource.String.msg_user) + ":");
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
                    if((sender as AndroidX.AppCompat.App.AlertDialog)==null)
                    {
                        messageUserDialog.Dismiss();
                    }
                    else
                    {
                        (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    }
                    return;
                }

                SoulSeekState.RecentUsersManager.AddUserToTop(userToMessage, true);

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
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
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
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Message User logic
                    eventHandler(sender, null);
                }
            };


            System.EventHandler<TextView.KeyEventArgs> keypressAction = (object sender, TextView.KeyEventArgs e) =>
            {
                if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
                {
                    MainActivity.LogDebug("keypress: " + e.Event.KeyCode.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(this.FindViewById(Android.Resource.Id.Content).RootView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
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
        }

        private void Input_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SoulSeekState.ActiveActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("MainActivity_FocusChange" + err.Message);
            }
        }


        public override void OnBackPressed()
        {
            //if f is non null and f is visible then that means you are backing out from the inner user fragment..
            var f = SupportFragmentManager.FindFragmentByTag("InnerUserFragment");
            if(f!=null && f.IsVisible)
            {
                if(SupportFragmentManager.BackStackEntryCount==0) //this is if we got to inner messages through a notification, in which case we are done..
                {
                    bool root = IsTaskRoot;
                    MainActivity.LogDebug("IS TASK ROOT: " + root); //returns false if there is in fact a task behind it (such as the main activity task).
                    if(IsTaskRoot) //it is TRUE if we swiped seeker from task list and then later followed a notification..
                    {
                        Intent intent = new Intent(this, typeof(MainActivity));
                        intent.AddFlags(ActivityFlags.ClearTop);   
                        this.StartActivity(intent);
                        return;
                    }
                    else
                    {
                        base.OnBackPressed();
                        return;
                    }
                }
                Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.messages_toolbar);
                myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
                myToolbar.Title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.messages);
                this.SetSupportActionBar(myToolbar);
                this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                this.SupportActionBar.SetHomeButtonEnabled(true);
                SupportFragmentManager.BeginTransaction().Remove(f).Commit();

                //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new MessagesOverviewFragment(), "OuterUserFragment").Commit();
            }
            base.OnBackPressed();
        }


        //Delete Undo Helpers
        public static string DELETED_USERNAME = string.Empty;
        public static int DELETED_POSITION = -1;
        public static List<Message> DELETED_DATA = null;
        public static volatile bool FromDeleteMessage = false;
        //for delete all
        public static Dictionary<string,List<Message>> DELETED_DICTIONARY = null;

        /// <summary>
        /// This method will switch you from inner to outer.  If you came to inner from a notification, outer will be added.
        /// note: whenever we go back we recreate the fragment so we dont need to mess around with the adapter (in the case of delete), it will be recreated.
        /// </summary>
        /// <param name="innerFragment"></param>
        public void SwitchToOuter(Android.Support.V4.App.Fragment innerFragment, bool forDeleteMessage)
        {
            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.messages_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
            myToolbar.Title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.messages);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            var outerExists = SupportFragmentManager.FindFragmentByTag("OuterUserFragment");
            FromDeleteMessage = forDeleteMessage;
            if(outerExists!=null)
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
                    MainActivity.LogFirebase("empty goToUsersMessages");
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
            if (SoulSeekState.Username != MessageController.MessagesUsername && MessageController.RootMessages!=null)
            {
                MessageController.MessagesUsername = SoulSeekState.Username;
                MessageController.Messages = MessageController.RootMessages[SoulSeekState.Username]; //username can be null here... perhaps restarting the app without internet or such...
            }
            base.OnResume();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SeekerApplication.SetActivityTheme(this);
            base.OnCreate(savedInstanceState);

            MessagesActivityRef = this;
            SoulSeekState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.messages_main_layout);


            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.messages_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
            myToolbar.Title = SoulSeekState.ActiveActivityRef.GetString(Resource.String.messages);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            //this.SupportActionBar.SetDisplayShowHomeEnabled(true);

            if (MessageController.RootMessages == null)
            {
                var sharedPref = this.GetSharedPreferences("SoulSeekPrefs", 0);
                MessageController.RestoreMessagesFromSharedPrefs(sharedPref);
                if(SoulSeekState.Username != null && SoulSeekState.Username != string.Empty)
                {
                    MessageController.MessagesUsername = SoulSeekState.Username;
                    if(!MessageController.RootMessages.ContainsKey(SoulSeekState.Username))
                    {
                        MessageController.RootMessages[SoulSeekState.Username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                    }
                    else
                    {
                        MessageController.Messages = MessageController.RootMessages[SoulSeekState.Username];
                    }
                }
            }
            else if(SoulSeekState.Username != MessageController.MessagesUsername)
            {
                MessageController.MessagesUsername = SoulSeekState.Username;
                if(SoulSeekState.Username==null || SoulSeekState.Username==string.Empty)
                {
                    MessageController.Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                }
                else
                {
                    if(MessageController.RootMessages.ContainsKey(SoulSeekState.Username))
                    {
                        MessageController.Messages = MessageController.RootMessages[SoulSeekState.Username];
                    }
                    else
                    {
                        MessageController.RootMessages[SoulSeekState.Username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                        MessageController.Messages = MessageController.RootMessages[SoulSeekState.Username];
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
            else if (Intent!=null) //if an intent started this activity
            {
                if(Intent.GetBooleanExtra(MessageController.ComingFromMessageTapped, false))
                {
                    string goToUsersMessages = Intent.GetStringExtra(MessageController.FromUserName);
                    if(goToUsersMessages==string.Empty)
                    {
                        MainActivity.LogFirebase("empty goToUsersMessages");
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
        Success  =3,
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

    [System.Serializable] //else error even with binary serializer
    public class Message
    {
        public string Username;
        public int Id;
        public bool Replayed;
        public DateTime LocalDateTime;
        public DateTime UtcDateTime;
        public string MessageText;
        public bool FromMe = false;
        public SentStatus SentMsgStatus = SentStatus.None;
        public SpecialMessageCode SpecialCode = SpecialMessageCode.None;
        public bool SameAsLastUser = false;
        public Message()
        {
            //i think this is necessary for serialization...
        }

        public Message(string username, int id, bool replayed, DateTime localDateTime, DateTime utcDateTime, string messageText, bool fromMe)
        {
            Username = username;
            Id = id;
            Replayed = replayed;
            LocalDateTime = localDateTime;
            UtcDateTime = utcDateTime;
            MessageText = messageText;
            FromMe = fromMe;
        }

        public Message(string username, int id, bool replayed, DateTime localDateTime, DateTime utcDateTime, string messageText, bool fromMe, SentStatus sentStatus)
        {
            Username = username;
            Id = id;
            Replayed = replayed;
            LocalDateTime = localDateTime;
            UtcDateTime = utcDateTime;
            MessageText = messageText;
            FromMe = fromMe;
            SentMsgStatus = sentStatus;
        }

        public Message(DateTime localDateTime, DateTime utcDateTime, SpecialMessageCode connectOrDisconnect)
        {
            Username = string.Empty;
            Id = -2;
            Replayed = false;
            LocalDateTime = localDateTime;
            UtcDateTime = utcDateTime;
            SetConnectDisconnectText(localDateTime, connectOrDisconnect);
            SentMsgStatus = 0;
            SpecialCode = connectOrDisconnect;
        }

        private void SetConnectDisconnectText(DateTime localDateTime, SpecialMessageCode connectOrDisconnect)
        {
            if(connectOrDisconnect == SpecialMessageCode.Disconnect)
            {
                MessageText = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.chatroom_disconnected_at), Helpers.GetNiceDateTime(localDateTime));
            }
            else
            {
                MessageText = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.chatroom_reconnected_at), Helpers.GetNiceDateTime(localDateTime));
            }
        }
    }

    public static class MessageController
    {
        public static object MessageListLockObject = new object(); //since the Messages List is not concurrent...
        public static bool IsInitialized = false;
        public static EventHandler<Message> MessageReceived;
        public static System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>> RootMessages = null; //this is for when the user logs in as different people
        public static System.Collections.Concurrent.ConcurrentDictionary<string,List<Message>> Messages = null;//new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
        public static string MessagesUsername = string.Empty;
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
            SoulSeekState.SoulseekClient.PrivateMessageReceived += Client_PrivateMessageReceived;
            lock(MessageListLockObject)
            {
                RestoreMessagesFromSharedPrefs(SoulSeekState.SharedPreferences);
            }
            IsInitialized = true;
        }

        private static void Client_PrivateMessageReceived(object sender, Soulseek.PrivateMessageReceivedEventArgs e)
        {
            try
            {
                if (SeekerApplication.IsUserInIgnoreList(e.Username))
                {
                    MainActivity.LogDebug("IGNORED PM received: " + e.Username);
                    return;
                }

                //file
                Message msg = new Message(e.Username, e.Id, e.Replayed, e.Timestamp.ToLocalTime(), e.Timestamp, e.Message, false);
            lock (MessageListLockObject)
            {
                if(SoulSeekState.Username == null || SoulSeekState.Username == string.Empty)
                {
                    MainActivity.LogFirebase("we received a message while our username is still null");
                }
                else if(!RootMessages.ContainsKey(SoulSeekState.Username))
                {
                    RootMessages[SoulSeekState.Username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
                    MessagesUsername = SoulSeekState.Username;
                    Messages = RootMessages[SoulSeekState.Username];
                }
                else if(RootMessages.ContainsKey(SoulSeekState.Username))
                {
                    Messages = RootMessages[SoulSeekState.Username];
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

            //save to prefs
            SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);

            try
            {
                //raise event
                MessageReceived?.Invoke(sender, msg); //if this throws it does not crash anything. it will fail silently which is quite bad bc then we never ACK the message.
            }
            catch(Exception error)
            {
                MainActivity.LogFirebase("MessageReceived raise event failed: " + error.Message);
            }

            try
            {
                SoulSeekState.SoulseekClient.AcknowledgePrivateMessageAsync(msg.Id).ContinueWith((Action<Task>)LogIfFaulted);
            }
            catch(Exception err)
            {
                MainActivity.LogFirebase("AcknowledgePrivateMessageAsync: " + err.Message);
            }
            }
            catch(Exception exc)
            {
                MainActivity.LogFirebase("msg received:" + exc.Message + exc.StackTrace);
            }
        }

        public static void RaiseMessageReceived(Message msg) //normally this is if it is a message from us...
        {
            MessageReceived?.Invoke(null, msg);
        }

        public static void LogIfFaulted(Task t)
        {
            if(t.IsFaulted)
            {
                MainActivity.LogFirebase("AcknowledgePrivateMessageAsync faulted: " + t.Exception.Message + t.Exception.StackTrace);
            }
        }

        public static string CHANNEL_ID = "Private Messages ID";
        public static string CHANNEL_NAME = "Private Messages";
        public static string FromUserName = "FromThisUser";
        public static string ComingFromMessageTapped = "FromAMessage";

        public static void ShowNotification(Message msg)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                try
                {
                    Helpers.CreateNotificationChannel(SoulSeekState.ActiveActivityRef, CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MessagesActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop);
                    notifIntent.PutExtra(FromUserName, msg.Username); //so we can go to this user..
                    notifIntent.PutExtra(ComingFromMessageTapped, true); //so we can go to this user..
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SoulSeekState.ActiveActivityRef, msg.Username.GetHashCode(), notifIntent, PendingIntentFlags.UpdateCurrent);
                    Notification n = Helpers.CreateNotification(SoulSeekState.ActiveActivityRef, pendingIntent, CHANNEL_ID, $"Message from {msg.Username}", msg.MessageText, false); //TODO
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SoulSeekState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify(msg.Username.GetHashCode(), n);
                }
                catch(System.Exception e)
                {
                    MainActivity.LogFirebase("ShowNotification failed: " + e.Message + e.StackTrace);
                }
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
            using (System.IO.MemoryStream messagesStream = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                lock(MessageListLockObject)
                {
                    formatter.Serialize(messagesStream, RootMessages);
                }
                messagesString = Convert.ToBase64String(messagesStream.ToArray());
            }
            if(messagesString != null && messagesString != string.Empty)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = sharedPrefs.Edit();
                    editor.PutString(SoulSeekState.M_Messages, messagesString);
                    bool success = editor.Commit();
                }
            }
        }

        public static void RestoreMessagesFromSharedPrefs(ISharedPreferences sharedPrefs)
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            string messages = sharedPrefs.GetString(SoulSeekState.M_Messages, string.Empty);
            if (messages == string.Empty)
            {
                RootMessages = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>>();
                Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
            }
            else
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(messages)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    RootMessages = binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>>;
                    if(SoulSeekState.Username!=null && SoulSeekState.Username != string.Empty && RootMessages.ContainsKey(SoulSeekState.Username))
                    {
                        Messages = RootMessages[SoulSeekState.Username];
                        MessagesUsername = SoulSeekState.Username;
                    }
                }
            }
        }
    }



    public class MessagesInnerFragment : Android.Support.V4.App.Fragment
    {
        private RecyclerView recyclerViewInner;
        private LinearLayoutManager recycleLayoutManager;
        private MessagesInnerRecyclerAdapter recyclerAdapter;
        private List<Message> messagesInternal = null;
        private bool created = false;
        private View rootView = null;
        private EditText editTextEnterMessage = null;
        private Button sendMessage = null;
        public static string Username = null;

        public MessagesInnerFragment() : base()
        {

        }

        public MessagesInnerFragment(string username) : base()
        {
            Username = username;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if(Username==null)
            {
                Username = savedInstanceState.GetString("Inner_Username_ToMessage");
            }

            

            MessageController.MessageReceived += OnMessageReceived;
            rootView = inflater.Inflate(Resource.Layout.messages_inner_layout, container, false);

            
            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)MessagesActivity.MessagesActivityRef.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.messages_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_inner_list_menu);
            myToolbar.Title = Username;
            MessagesActivity.MessagesActivityRef.SetSupportActionBar(myToolbar);


            editTextEnterMessage = rootView.FindViewById<EditText>(Resource.Id.edit_gchat_message);
            sendMessage = rootView.FindViewById<Button>(Resource.Id.button_gchat_send);

            if(editTextEnterMessage.Text == null || editTextEnterMessage.Text.ToString()==string.Empty)
            {
                sendMessage.Enabled = false;
            }
            else
            {
                sendMessage.Enabled = true;
            }
            editTextEnterMessage.TextChanged += EditTextEnterMessage_TextChanged;
            editTextEnterMessage.EditorAction += EditTextEnterMessage_EditorAction;
            editTextEnterMessage.KeyPress += EditTextEnterMessage_KeyPress;
            sendMessage.Click += SendMessage_Click;

            //TextView noMessagesView = rootView.FindViewById<TextView>(Resource.Id.noMessagesView);
            recyclerViewInner = rootView.FindViewById<RecyclerView>(Resource.Id.recycler_messages);
            //recyclerViewInner.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleLayoutManager = new LinearLayoutManager(Activity);
            recycleLayoutManager.StackFromEnd = true;
            recycleLayoutManager.ReverseLayout = false;
            if (MessageController.Messages.Keys.Contains(Username))
            {
                messagesInternal = MessageController.Messages[Username].ToList();
            }
            else
            {
                messagesInternal = new List<Message>();
            }
            recyclerAdapter = new MessagesInnerRecyclerAdapter(messagesInternal); //this depends tightly on MessageController... since these are just strings..
            recyclerViewInner.SetAdapter(recyclerAdapter);
            recyclerViewInner.SetLayoutManager(recycleLayoutManager);
            if (messagesInternal.Count != 0)
            {
                recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
            }
            created = true;
            return rootView;
        }

        private void EditTextEnterMessage_KeyPress(object sender, View.KeyEventArgs e)
        {
            if(e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
            {
                e.Handled = true;
                //send the message and record our send message..
                SendMessageAPI(new Message(Username, -1, false, DateTime.Now, DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

                editTextEnterMessage.Text = string.Empty;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void EditTextEnterMessage_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if(e.ActionId == Android.Views.InputMethods.ImeAction.Send)
            {
                //send the message and record our send message..
                SendMessageAPI(new Message(Username, -1, false, DateTime.Now, DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

                editTextEnterMessage.Text = string.Empty;
            }
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("Inner_Username_ToMessage",Username);
            base.OnSaveInstanceState(outState);
        }

        public void SendMessageAPI(Message msg)
        {
            if(msg.MessageText==string.Empty)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_type_text_to_send, ToastLength.Short).Show();
                return;
            }
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_be_logged_to_send_message, ToastLength.Short).Show();
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) => {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() => {
                        SendMessageLogic(msg);
                    }));
                }));
            }
            else
            {
                SendMessageLogic(msg);
            }

        }

        public void SendMessageLogic(Message msg) //you can start out with a message...
        {
            if(MessageController.Messages.Keys.Contains(Username))
            {
                MessageController.Messages[Username].Add(msg);
            }
            else
            {
                MessageController.Messages[Username] = new List<Message>(); //our first message to them..
                MessageController.Messages[Username].Add(msg);
            }
            MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
            MessageController.RaiseMessageReceived(msg);
            Action<Task> continueWithAction = new Action<Task>((Task t)=>
            {
                if(t.IsFaulted)
                {
                    msg.SentMsgStatus = SentStatus.Failed;
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_send_message, ToastLength.Long).Show();
                }
                else
                {
                    msg.SentMsgStatus = SentStatus.Success;
                }
                MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
                MessageController.RaiseMessageReceived(msg);
            });
            SoulSeekState.SoulseekClient.SendPrivateMessageAsync(Username, msg.MessageText).ContinueWith(continueWithAction);
        }


        private void SendMessage_Click(object sender, EventArgs e)
        {
            //send the message and record our send message..
            SendMessageAPI(new Message(Username,-1,false,DateTime.Now, DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

            editTextEnterMessage.Text = string.Empty;
        }

        private void EditTextEnterMessage_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if(e.Text!=null && e.Text.ToString() != string.Empty) //ICharSequence..
            {
                sendMessage.Enabled = true;
            }
            else
            {
                sendMessage.Enabled = false;
            }
        }

        public override void OnAttach(Context activity)
        {
            if (created) //attach can happen before we created our view...
            {
                messagesInternal = MessageController.Messages[Username].ToList();
                recyclerAdapter = new MessagesInnerRecyclerAdapter(messagesInternal); //this depends tightly on MessageController... since these are just strings..
                recyclerViewInner.SetAdapter(recyclerAdapter);
                if(messagesInternal.Count!=0)
                {
                    recyclerViewInner.ScrollToPosition(messagesInternal.Count-1);
                }
                MessageController.MessageReceived -= OnMessageReceived;
                MessageController.MessageReceived += OnMessageReceived;
            }
            base.OnAttach(activity);
        }

        public override void OnDetach()
        {
            MessageController.MessageReceived -= OnMessageReceived;
            base.OnDetach();
        }

        public void OnMessageReceived(object sender, Message msg)
        {
            this.Activity.RunOnUiThread(new Action(() => {
                    messagesInternal = MessageController.Messages[Username];
                    recyclerAdapter = new MessagesInnerRecyclerAdapter(messagesInternal); //this depends tightly on MessageController... since these are just strings..
                    recyclerViewInner.SetAdapter(recyclerAdapter);
                    recyclerAdapter.NotifyDataSetChanged();
                    if (messagesInternal.Count != 0)
                    {
                        recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                    }
            }));
        }


    }


    
    public class MessagesOverviewFragment : Android.Support.V4.App.Fragment
    {
        private RecyclerView recyclerViewOverview;
        private LinearLayoutManager recycleLayoutManager;
        public MessagesOverviewRecyclerAdapter recyclerAdapter;
        private List<string> internalList = null;
        private TextView noMessagesView = null;
        private bool created = false;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MessageController.MessageReceived += OnMessageReceived;
            View rootView = inflater.Inflate(Resource.Layout.messages_overview, container, false);
            noMessagesView = rootView.FindViewById<TextView>(Resource.Id.noMessagesView);
            if (MessageController.Messages==null || MessageController.Messages.Keys.Count==0)
            {
                noMessagesView.Visibility = ViewStates.Visible;
            }
            else
            {
                noMessagesView.Visibility = ViewStates.Gone;
            }
            recyclerViewOverview = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewOverview);
            recyclerViewOverview.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));

            recycleLayoutManager = new LinearLayoutManager(Activity);
            if(MessageController.Messages==null)
            {
                internalList = new List<string>();
            }
            else
            {
                internalList = GetOverviewList();//MessageController.Messages.Keys.ToList();
            }
            recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
            recyclerViewOverview.SetAdapter(recyclerAdapter);
            recyclerViewOverview.SetLayoutManager(recycleLayoutManager);

            ItemTouchHelperMessageOverviewCallback itemTouchHelperMessageOverviewCallback = new ItemTouchHelperMessageOverviewCallback(recyclerAdapter, this);
            ItemTouchHelper itemTouchHelper = new ItemTouchHelper(itemTouchHelperMessageOverviewCallback);
            itemTouchHelper.AttachToRecyclerView(recyclerViewOverview);

            created = true;
            return rootView;
        }

        

        public override void OnResume()
        {
            base.OnResume();
            SoulSeekState.ActiveActivityRef.InvalidateOptionsMenu();
            if(MessagesActivity.FromDeleteMessage)
            {
                MessagesActivity.FromDeleteMessage = false;
                Snackbar sb = Snackbar.Make(SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.deleted_message_history_with), MessagesActivity.DELETED_USERNAME), Snackbar.LengthLong).SetAction("Undo", ItemTouchHelperMessageOverviewCallback.GetSnackBarAction(recyclerAdapter, true)).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(Android.Graphics.Color.ParseColor("#BCC1F7"));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                sb.Show();
            }
        }

        public class MessageOverviewComparer : IComparer<KeyValuePair<string, List<Message>>>
        {
            // Compares by UserCount then Name
            public int Compare(KeyValuePair<string,List<Message>> x, KeyValuePair<string, List<Message>> y)
            {
                if(x.Value.Count == 0 && y.Value.Count == 0)
                {
                    return 0;
                }
                else if(x.Value.Count == 0)
                { 
                    return 1;
                }
                else if(y.Value.Count==0)
                {
                    return -1;
                }
                else
                {
                    return y.Value.Last().LocalDateTime.CompareTo(x.Value.Last().LocalDateTime);
                }
            }
        }

        public static List<string> GetOverviewList()
        {
            var listToSort = MessageController.Messages.ToList();
            listToSort.Sort(new MessageOverviewComparer());
            return listToSort.Select((pair)=>pair.Key).ToList();
        }

        public void OnMessageReceived(object sender, Message msg)
        {
            var activity = this.Activity != null ? this.Activity : MessagesActivity.MessagesActivityRef;
            activity.RunOnUiThread(new Action(() => {
                //if(internalList!=null && internalList.Contains(msg.Username))
                //{
                //    //update this one...
                //    recyclerAdapter.NotifyDataSetChanged();//NotifyItemChanged(internalList.IndexOf(msg.Username));
                //}
                //else
                //{
                    this.RefreshAdapter();
                //}
            }));
        }

        public void RefreshAdapter()
        {
            internalList = GetOverviewList();
            if (internalList.Count != 0)
            {
                noMessagesView.Visibility = ViewStates.Gone;
            }
            recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
            recyclerViewOverview.SetAdapter(recyclerAdapter);
            recyclerAdapter.NotifyDataSetChanged();
        }

        public override void OnAttach(Context activity)
        {
            if(created) //attach can happen before we created our view...
            {
                internalList = GetOverviewList();
                if (internalList.Count!=0)
                {
                    noMessagesView.Visibility = ViewStates.Gone;
                }
                recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
                recyclerViewOverview.SetAdapter(recyclerAdapter);
                MessageController.MessageReceived -= OnMessageReceived;
                MessageController.MessageReceived += OnMessageReceived;
            }
            base.OnAttach(activity);
        }

        public override void OnDetach()
        {
            MessageController.MessageReceived -= OnMessageReceived;
            base.OnDetach();
        }
    }


    public class MessagesInnerRecyclerAdapter : RecyclerView.Adapter
    {
        private List<Message> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public static int VIEW_SENT = 1;
        public static int VIEW_RECEIVER = 2;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (localDataSet[position].FromMe)
            {
                (holder as MessageInnerViewSentHolder).messageInnerView.setItem(localDataSet[position]);
            }
            else
            {
                (holder as MessageInnerViewReceivedHolder).messageInnerView.setItem(localDataSet[position]);
            }
            //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
        }

        public void setPosition(int position)
        {
            this.position = position;
        }

        public int getPosition()
        {
            return this.position;
        }

        //public override void OnViewRecycled(Java.Lang.Object holder)
        //{
        //    base.OnViewRecycled(holder);
        //}

        //private void MessageOverviewClick(object sender, EventArgs e)
        //{
        //    //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
        //    //pop.Inflate(Resource.Menu.download_diag_options);
        //    //pop.Show();
        //    setPosition((sender as MessageOverviewView).ViewHolder.AdapterPosition);
        //    MessagesActivity.ChangeFragment((sender as MessageOverviewView).View);
        //}
        public override int GetItemViewType(int position)
        {
            if(localDataSet[position].FromMe)
            {
                return VIEW_SENT;
            }
            else
            {
                return VIEW_RECEIVER;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {
            if(viewType==VIEW_SENT)
            {
                MessageInnerViewSent view = MessageInnerViewSent.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += MessageOverviewClick;
                return new MessageInnerViewSentHolder(view as View);
            }
            else
            {
                MessageInnerViewReceived view = MessageInnerViewReceived.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += MessageOverviewClick;
                return new MessageInnerViewReceivedHolder(view as View);
            }

        }

        public MessagesInnerRecyclerAdapter(List<Message> ti)
        {
            localDataSet = ti;
        }

    }




    public class MessageInnerViewSentHolder : RecyclerView.ViewHolder
    {
        public MessageInnerViewSent messageInnerView;


        public MessageInnerViewSentHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            messageInnerView = (MessageInnerViewSent)view;
            messageInnerView.ViewHolder = this;
            //(MessageOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public MessageInnerViewSent getUnderlyingView()
        {
            return messageInnerView;
        }
    }

    public class MessageInnerViewReceivedHolder : RecyclerView.ViewHolder
    {
        public MessageInnerViewReceived messageInnerView;


        public MessageInnerViewReceivedHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            messageInnerView = (MessageInnerViewReceived)view;
            messageInnerView.ViewHolder = this;
            //(MessageOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public MessageInnerViewReceived getUnderlyingView()
        {
            return messageInnerView;
        }
    }




    public class MessageInnerViewSent : LinearLayout
    {
        public MessageInnerViewSentHolder ViewHolder { get; set; }
        private TextView viewTimeStamp;
        private TextView viewMessage;
        private AndroidX.CardView.Widget.CardView cardView;

        public MessageInnerViewSent(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.messages_inner_item_fromMe, this, true);
            setupChildren();
        }
        public MessageInnerViewSent(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.messages_inner_item_fromMe, this, true);
            setupChildren();
        }

        public static MessageInnerViewSent inflate(ViewGroup parent)
        {
            MessageInnerViewSent itemView = (MessageInnerViewSent)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.messages_inner_item_fromMe_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewTimeStamp = FindViewById<TextView>(Resource.Id.text_gchat_timestamp_me);
            viewMessage = FindViewById<TextView>(Resource.Id.text_gchat_message_me);
            cardView = FindViewById<AndroidX.CardView.Widget.CardView>(Resource.Id.card_gchat_message_me);
        }

        public static Color GetColorFromInteger(int color)
        {
            return Color.Rgb(Color.GetRedComponent(color), Color.GetGreenComponent(color), Color.GetBlueComponent(color));
        }

        public static Color GetColorFromMsgStatus(SentStatus status)
        {
            int resourceIntColor = -1;
            switch(status)
            {
                case SentStatus.Pending:
                case SentStatus.Success:
                    resourceIntColor = Resource.Color.mainPurple;
                    break;
                case SentStatus.Failed:
                    resourceIntColor = Resource.Color.hardErrorRed;
                    break;
                case SentStatus.None:
                    throw new Exception("Sent status should not be none");
            }

            if ((int)Android.OS.Build.VERSION.SdkInt >= 23)
            {
                return GetColorFromInteger(ContextCompat.GetColor(SoulSeekState.ActiveActivityRef, resourceIntColor));
            }
            else
            {
                return SoulSeekState.ActiveActivityRef.Resources.GetColor(resourceIntColor);
            }
        }

        public void setItem(Message msg)
        {
            cardView.CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf( GetColorFromMsgStatus(msg.SentMsgStatus) );
            if(msg.SentMsgStatus == SentStatus.Pending)
            {
                viewTimeStamp.Text = SoulSeekState.ActiveActivityRef.GetString(Resource.String.pending_);
            }
            else if(msg.SentMsgStatus == SentStatus.Failed)
            {
                viewTimeStamp.Text = SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed);
            }
            else
            {
                viewTimeStamp.Text = Helpers.GetNiceDateTime( msg.LocalDateTime );
            }
            Helpers.SetMessageTextView(viewMessage, msg);
        }
    }

    public class MessageInnerViewReceived : ConstraintLayout
    {
        public MessageInnerViewReceivedHolder ViewHolder { get; set; }
        private TextView viewTimeStamp;
        private TextView viewMessage;

        public MessageInnerViewReceived(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.messages_inner_item_toMe, this, true);
            setupChildren();
        }
        public MessageInnerViewReceived(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.messages_inner_item_toMe, this, true);
            setupChildren();
        }
        public static MessageInnerViewReceived inflate(ViewGroup parent)
        {
            MessageInnerViewReceived itemView = (MessageInnerViewReceived)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.messages_inner_item_toMe_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewTimeStamp = FindViewById<TextView>(Resource.Id.text_gchat_timestamp_other);
            viewMessage = FindViewById<TextView>(Resource.Id.text_gchat_message_other);
        }

        public void setItem(Message msg)
        {
            viewTimeStamp.Text = Helpers.GetNiceDateTime( msg.LocalDateTime );
            Helpers.SetMessageTextView(viewMessage, msg);
        }
    }



    public class ItemTouchHelperMessageOverviewCallback : ItemTouchHelper.SimpleCallback
    {
        //public static string DELETED_USERNAME = string.Empty;
        //public static int DELETED_POSITION = -1;
        //public static List<Message> DELETED_DATA = null;
        private MessagesOverviewRecyclerAdapter adapter = null;
        private Android.Support.V4.App.Fragment containingFragment = null;
        public ItemTouchHelperMessageOverviewCallback(MessagesOverviewRecyclerAdapter _adapter, Android.Support.V4.App.Fragment outerFrag) : base(0,ItemTouchHelper.Left) //no dragging. left swiping.
        {
            containingFragment = outerFrag;
            adapter = _adapter;
            iconDrawable = ContextCompat.GetDrawable(SoulSeekState.ActiveActivityRef, Resource.Drawable.baseline_delete_outline_white_24);
            clipDrawable = new ClipDrawable(iconDrawable, GravityFlags.Right, ClipDrawableOrientation.Horizontal);
        }
        private Android.Graphics.Drawables.ColorDrawable colorDrawable = new Android.Graphics.Drawables.ColorDrawable(Color.ParseColor("#ed4a51"));
        private Android.Graphics.Drawables.Drawable iconDrawable = null;
        private Android.Graphics.Drawables.ClipDrawable clipDrawable = null;
       
        public override bool OnMove(RecyclerView p0, RecyclerView.ViewHolder p1, RecyclerView.ViewHolder p2)
        {
            return false;
        }

        public static Action<View> GetSnackBarAction(MessagesOverviewRecyclerAdapter adapter, bool fromOptionMenu = false)
        {
            Action<View> undoSnackBarAction = new Action<View>((View v) => {
                if (MessagesActivity.DELETED_USERNAME == string.Empty || MessagesActivity.DELETED_DATA == null || MessagesActivity.DELETED_POSITION == -1)
                {
                    //error
                    bool isNull = MessagesActivity.DELETED_DATA == null;
                    MainActivity.LogFirebase("failure on undo uname:" + MessagesActivity.DELETED_USERNAME + " " + isNull + " " + MessagesActivity.DELETED_POSITION);
                    Toast.MakeText(v.Context, Resource.String.failed_to_undo, ToastLength.Short).Show();
                    return;
                }
                MessageController.Messages[MessagesActivity.DELETED_USERNAME] = MessagesActivity.DELETED_DATA;
                MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
                if (!fromOptionMenu)
                {
                    adapter.RestoreAt(MessagesActivity.DELETED_POSITION, MessagesActivity.DELETED_USERNAME);
                }
                else
                {
                    (SoulSeekState.ActiveActivityRef as MessagesActivity).GetOverviewFragment().RefreshAdapter();
                }
                MessagesActivity.DELETED_USERNAME = string.Empty; MessagesActivity.DELETED_DATA = null; MessagesActivity.DELETED_POSITION = -1;
            });
            return undoSnackBarAction;
        }

        public override void OnSwiped(RecyclerView.ViewHolder p0, int p1)
        {
            //delete and save messages
            //show snackbar
            MessagesActivity.DELETED_POSITION = p0.AbsoluteAdapterPosition;
            MessagesActivity.DELETED_USERNAME = adapter.At(MessagesActivity.DELETED_POSITION);
            adapter.RemoveAt(MessagesActivity.DELETED_POSITION); //removes from adapter data and notifies.
            MessageController.Messages.Remove(MessagesActivity.DELETED_USERNAME, out MessagesActivity.DELETED_DATA);
            MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);

            Snackbar sb = Snackbar.Make(containingFragment.View, string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.deleted_message_history_with), MessagesActivity.DELETED_USERNAME), Snackbar.LengthLong).SetAction("Undo", GetSnackBarAction(this.adapter, false)).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
            (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(Android.Graphics.Color.ParseColor("#BCC1F7"));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
            sb.Show();
        }

        public override void OnChildDraw(Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
        {
            base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
            View itemView = viewHolder.ItemView;
            MainActivity.LogDebug("dX" + dX);
            if (dX>0)
            {
                this.colorDrawable.SetBounds(itemView.Left, itemView.Top, itemView.Left + (int)dX, itemView.Bottom);
            }
            else if(dX<0)
            {
                this.colorDrawable.SetBounds(itemView.Right +(int) dX, itemView.Top, itemView.Right, itemView.Bottom);
                double margin = (itemView.Bottom - itemView.Top)*.15; //BOTTOM IS GREATER THAN TOP
                int clipBounds = (int)((itemView.Bottom - itemView.Top)-2*margin);
                int level = Math.Min((int)(Math.Abs((dX + margin) / (clipBounds)) * 10000), 10000);
                MainActivity.LogDebug("level"+ level);
                if(level<0)
                {
                    level=0;
                }
                clipDrawable.SetLevel(level);
                //int dXicon = -300;
                clipDrawable.SetBounds((int)(itemView.Right - clipBounds - margin), (int)(itemView.Top + margin), (int)(itemView.Right - margin), (int)(itemView.Bottom-margin));
            }
            else
            {
                this.colorDrawable.SetBounds(0,0,0,0);
                //this.iconDrawable.SetBounds(0,0,0,0);
            }
            this.colorDrawable.Draw(c);
            clipDrawable.Draw(c);
        }
    }


    public class MessagesOverviewRecyclerAdapter : RecyclerView.Adapter
    {
        private List<string> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as MessageOverviewHolder).messageOverviewView.setItem(localDataSet[position]);
            //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
        }

        public void setPosition(int position)
        {
            this.position = position;
        }

        public int getPosition()
        {
            return this.position;
        }

        //public override void OnViewRecycled(Java.Lang.Object holder)
        //{
        //    base.OnViewRecycled(holder);
        //}

        private void MessageOverviewClick(object sender, EventArgs e)
        {
            //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
            //pop.Inflate(Resource.Menu.download_diag_options);
            //pop.Show();
            setPosition((sender as MessageOverviewView).ViewHolder.AdapterPosition);
            MessagesActivity.MessagesActivityRef.ChangeToInnerFragment(localDataSet[position]);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            MessageOverviewView view = MessageOverviewView.inflate(parent);
            view.setupChildren();
            // .inflate(R.layout.text_row_item, viewGroup, false);
            (view as View).Click += MessageOverviewClick;
            return new MessageOverviewHolder(view as View);

        }

        public string At(int pos)
        {
            return localDataSet[pos];
        }

        public void RemoveAt(int pos)
        {
            localDataSet.RemoveAt(pos);
            this.NotifyItemRemoved(pos);
        }

        public void RestoreAt(int pos, string uname)
        {
            localDataSet.Insert(pos, uname);
            this.NotifyItemInserted(pos);
        }

        public MessagesOverviewRecyclerAdapter(List<string> ti)
        {
            localDataSet = ti;
        }

    }

    public class MessageOverviewHolder : RecyclerView.ViewHolder
    {
        public MessageOverviewView messageOverviewView;


        public MessageOverviewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            messageOverviewView = (MessageOverviewView)view;
            messageOverviewView.ViewHolder = this;
            //(MessageOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public MessageOverviewView getUnderlyingView()
        {
            return messageOverviewView;
        }
    }

    public class MessageOverviewView : LinearLayout
    {
        public MessageOverviewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewMessage;

        public MessageOverviewView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.message_overview_item, this, true);
            setupChildren();
        }
        public MessageOverviewView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.message_overview_item, this, true);
            setupChildren();
        }

        public static MessageOverviewView inflate(ViewGroup parent)
        {
            MessageOverviewView itemView = (MessageOverviewView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.message_overview_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.username);
            viewMessage = FindViewById<TextView>(Resource.Id.message);
        }

        public void setItem(string username)
        {
            viewUsername.Text = username;
            Message m = MessageController.Messages[username].Last();
            string msgText = m.MessageText;
            if (m.FromMe)
            {
                msgText = "\u21AA" + msgText;
            }
            viewMessage.Text = msgText;
        }
    }
}