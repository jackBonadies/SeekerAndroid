using Seeker.Chatroom;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seeker.Helpers;

using Common;
namespace Seeker.Messages
{
    public class MessagesInnerFragment : AndroidX.Fragment.App.Fragment
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
        public static bool currentlyResumed = false;

        public MessagesInnerFragment() : base()
        {

        }

        public MessagesInnerFragment(string username) : base()
        {
            Username = username;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Username == null)
            {
                Username = savedInstanceState.GetString("Inner_Username_ToMessage");
            }



            MessageController.MessageReceived += OnMessageReceived;
            rootView = inflater.Inflate(Resource.Layout.messages_inner_layout, container, false);


            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)MessagesActivity.MessagesActivityRef.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.messages_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_inner_list_menu);
            myToolbar.Title = Username;
            MessagesActivity.MessagesActivityRef.SetSupportActionBar(myToolbar);


            editTextEnterMessage = rootView.FindViewById<EditText>(Resource.Id.edit_gchat_message);
            sendMessage = rootView.FindViewById<Button>(Resource.Id.button_gchat_send);

            if (editTextEnterMessage.Text == null || editTextEnterMessage.Text.ToString() == string.Empty)
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
            if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
            {
                e.Handled = true;
                //send the message and record our send message..
                SendMessageAPI(new Message(Username, -1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

                editTextEnterMessage.Text = string.Empty;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void EditTextEnterMessage_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Send)
            {
                //send the message and record our send message..
                SendMessageAPI(new Message(Username, -1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

                editTextEnterMessage.Text = string.Empty;
            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case 0: //"Copy Text"
                    CommonHelpers.CopyTextToClipboard(this.Activity, ChatroomInnerFragment.MessagesLongClickData.MessageText);
                    break;
                default:
                    return base.OnContextItemSelected(item);
            }
            return true;
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("Inner_Username_ToMessage", Username);
            base.OnSaveInstanceState(outState);
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
                Toast.MakeText(contextToUse, Resource.String.must_type_text_to_send, ToastLength.Short).Show();
                if (fromDirectReplyAction)
                {
                    MessageController.ShowNotification(msg, true, true, "Failure - Message Text is Empty.");
                }
                return;
            }
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                Logger.Debug("not currently logged in");
                Toast.MakeText(contextToUse, Resource.String.must_be_logged_to_send_message, ToastLength.Short).Show();
                if (fromDirectReplyAction)
                {
                    MessageController.ShowNotification(msg, true, true, "Failure - Currently Logged Out.");
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
                        BroadcastFriendlyRunOnUiThread(() => { Toast.MakeText(contextToUse, Resource.String.failed_to_connect, ToastLength.Short).Show(); });
                    }
                    if (fromDirectReplyAction)
                    {
                        MessageController.ShowNotification(msg, true, true, "Failure - Cannot Log In.");
                    }
                    throw new FaultPropagationException();
                }
                BroadcastFriendlyRunOnUiThread(new Action(() =>
                {
                    SendMessageLogic(msg, fromDirectReplyAction, broadcastContext);
                }));
            });

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Logger.Debug("currently logged in but disconnected...");

                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(contextToUse, false, out t))
                {
                    return;
                }
                SeekerApplication.OurCurrentLoginTask = t.ContinueWith(actualActionToPerform);
            }
            else
            {
                if (MainActivity.IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(actualActionToPerform, "Message will send on connection re-establishment", contextToUse))
                {
                    Logger.Debug("on finish log in we will do it");
                    return;
                }
                else
                {
                    SendMessageLogic(msg, fromDirectReplyAction);
                }
            }

        }

        public static void SendMessageLogic(Message msg, bool fromDirectReplyAction, Android.Content.Context broadcastContext = null) //you can start out with a message...
        {
            Logger.Debug("SendMessageLogic");

            string usernameToMessage = msg.Username;
            if (MessageController.Messages.Keys.Contains(usernameToMessage))
            {
                MessageController.Messages[usernameToMessage].Add(msg);
            }
            else
            {
                MessageController.Messages[usernameToMessage] = new List<Message>(); //our first message to them..
                MessageController.Messages[usernameToMessage].Add(msg);
            }
            MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
            MessageController.RaiseMessageReceived(msg);
            Action<Task> continueWithAction = new Action<Task>((Task t) =>
            {
                if (t.IsFaulted)
                {
                    Logger.Debug("faulted " + t.Exception.ToString());
                    Logger.Debug("faulted " + t.Exception.InnerException.Message.ToString());
                    msg.SentMsgStatus = SentStatus.Failed;
                    Toast.MakeText(broadcastContext == null ? SeekerState.ActiveActivityRef : broadcastContext, Resource.String.failed_to_send_message, ToastLength.Long).Show(); //TODO

                    if (fromDirectReplyAction)
                    {
                        MessageController.ShowNotification(msg, true, true, "Failure - Cannot Send Message.", broadcastContext);
                    }
                }
                else
                {
                    Logger.Debug("did not fault");
                    msg.SentMsgStatus = SentStatus.Success;

                    if (fromDirectReplyAction)
                    {
                        MessageController.ShowNotification(msg, true, false, string.Empty, broadcastContext);
                    }
                }
                MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
                MessageController.RaiseMessageReceived(msg);
            });
            Logger.Debug("useranme to mesasge " + usernameToMessage);
            SeekerState.SoulseekClient.SendPrivateMessageAsync(usernameToMessage, msg.MessageText).ContinueWith(continueWithAction);
        }


        private void SendMessage_Click(object sender, EventArgs e)
        {
            //send the message and record our send message..
            SendMessageAPI(new Message(Username, -1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

            editTextEnterMessage.Text = string.Empty;
        }

        private void EditTextEnterMessage_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if (e.Text != null && e.Text.ToString() != string.Empty) //ICharSequence..
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
                if (messagesInternal.Count != 0)
                {
                    recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                }
                MessageController.MessageReceived -= OnMessageReceived;
                MessageController.MessageReceived += OnMessageReceived;
            }
            base.OnAttach(activity);
        }

        public override void OnResume()
        {
            Logger.Debug("inner frag resume");

            if (MessagesActivity.DirectReplyMessages.TryRemove(Username, out _))
            {
                Logger.Debug("remove the notification history");
                //remove the now possibly void notification
                NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                // notificationId is a unique int for each notification that you must define
                notificationManager.Cancel(Username.GetHashCode());
            }

            MessageController.UnsetAsUnreadAndSaveIfApplicable(Username);
            currentlyResumed = true;
            base.OnResume();
        }

        public override void OnPause()
        {
            Logger.Debug("inner frag pause");
            currentlyResumed = false;
            base.OnPause();
        }

        public override void OnDetach()
        {
            MessageController.MessageReceived -= OnMessageReceived;
            base.OnDetach();
        }

        public void OnMessageReceived(object sender, Message msg)
        {
            this.Activity.RunOnUiThread(new Action(() =>
            {
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

}