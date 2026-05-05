using Seeker.Chatroom;
using Seeker.Browse;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Snackbar;
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
        private bool created = false;
        private View rootView = null;
        private EditText editTextEnterMessage = null;
        private ImageButton sendMessage = null;
        public string Username = null;

        public MessagesInnerFragment() : base()
        {

        }

        public MessagesInnerFragment(string username) : base()
        {
            Username = username;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            Activity?.AddMenuProvider(new InnerMenuProvider(this), ViewLifecycleOwner, AndroidX.Lifecycle.Lifecycle.State.Resumed);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Username == null)
            {
                Username = savedInstanceState.GetString("Inner_Username_ToMessage");
            }

            MessageController.MessageReceived += OnMessageReceived;
            rootView = inflater.Inflate(Resource.Layout.messages_inner_layout, container, false);
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(rootView, new BottomOnlyInsetsListener());

            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)MessagesActivity.MessagesActivityRef.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.messages_toolbar);
            myToolbar.Title = Username;
            MessagesActivity.MessagesActivityRef.SetSupportActionBar(myToolbar);


            editTextEnterMessage = rootView.FindViewById<EditText>(Resource.Id.edit_gchat_message);
            sendMessage = rootView.FindViewById<ImageButton>(Resource.Id.button_gchat_send);

            if (editTextEnterMessage.Text == null || editTextEnterMessage.Text.ToString() == string.Empty)
            {
                sendMessage.Enabled = false;
                sendMessage.Alpha = 0.38f;
            }
            else
            {
                sendMessage.Enabled = true;
                sendMessage.Alpha = 1.0f;
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
            recyclerViewInner.SetLayoutManager(recycleLayoutManager);
            RebuildMessagesAdapter();
            created = true;
            return rootView;
        }

        private void RebuildMessagesAdapter()
        {
            var messages = MessageController.Messages.GetValueOrDefault(Username, new List<Message>()).ToList();
            recyclerAdapter = new MessagesInnerRecyclerAdapter(messages); //this depends tightly on MessageController... since these are just strings..
            recyclerViewInner.SetAdapter(recyclerAdapter);
            if (messages.Count != 0)
            {
                recyclerViewInner.ScrollToPosition(messages.Count - 1);
            }
        }

        private void EditTextEnterMessage_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
            {
                e.Handled = true;
                //send the message and record our send message..
                MessageController.SendMessageAPI(new Message(Username, -1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

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
                MessageController.SendMessageAPI(new Message(Username, -1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

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

        private class InnerMenuProvider : Java.Lang.Object, AndroidX.Core.View.IMenuProvider
        {
            private readonly MessagesInnerFragment fragment;

            public InnerMenuProvider(MessagesInnerFragment fragment)
            {
                this.fragment = fragment;
            }

            public void OnCreateMenu(IMenu menu, MenuInflater menuInflater)
            {
                menuInflater.Inflate(Resource.Menu.messages_inner_list_menu, menu);
            }

            public void OnPrepareMenu(IMenu menu)
            {
                UiHelpers.SetMenuTitles(menu, fragment.Username);
                UiHelpers.SetIgnoreAddExclusive(menu, fragment.Username);
            }

            public void OnMenuClosed(IMenu menu)
            {
            }

            public bool OnMenuItemSelected(IMenuItem item)
            {
                var activity = fragment.Activity;
                if (activity == null)
                {
                    return false;
                }
                var username = fragment.Username;
                if (UiHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), username, activity, activity.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId)))
                {
                    return true;
                }
                switch (item.ItemId)
                {
                    case Resource.Id.action_add_to_user_list:
                        UserListService.AddUserAPI(activity, username, new Action(() => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.success_added_user), ToastLength.Short); }));
                        return true;
                    case Resource.Id.action_search_files:
                        SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                        SearchTabHelper.SearchTargetChosenUser = username;
                        Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(MainActivity.GoToSearchExtra, true);
                        fragment.StartActivity(intent);
                        return true;
                    case Resource.Id.action_browse_files:
                        BrowseService.RequestFilesApi(username, null);
                        return true;
                    case Resource.Id.action_delete_messages:
                        var (deletedMessages, deletedReadCount) = MessageController.DeleteMessageFromUserWithUndo(username);
                        Snackbar sb1 = Snackbar.Make(SeekerState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content),
                                string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.deleted_message_history_with),
                                username),
                                Snackbar.LengthLong)
                            .SetAction(Resource.String.undo, (View v) => ItemTouchHelperMessageOverviewCallback.UndoSingleUserMessagesDeleteAction(null, (username, deletedMessages, deletedReadCount), -1, true));
                        sb1.Show();
                        (activity as MessagesActivity)?.SwitchToOuter(fragment, true);
                        return true;
                }
                return false;
            }
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("Inner_Username_ToMessage", Username);
            base.OnSaveInstanceState(outState);
        }



        private void SendMessage_Click(object sender, EventArgs e)
        {
            //send the message and record our send message..
            MessageController.SendMessageAPI(new Message(Username, -1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

            editTextEnterMessage.Text = string.Empty;
        }

        private void EditTextEnterMessage_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if (e.Text != null && e.Text.ToString() != string.Empty) //ICharSequence..
            {
                sendMessage.Enabled = true;
                sendMessage.Alpha = 1.0f;
            }
            else
            {
                sendMessage.Enabled = false;
                sendMessage.Alpha = 0.38f;
            }
        }

        public override void OnAttach(Context activity)
        {
            if (created) //attach can happen before we created our view...
            {
                RebuildMessagesAdapter();
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
            base.OnResume();
        }

        public override void OnPause()
        {
            Logger.Debug("inner frag pause");
            base.OnPause();
        }

        public override void OnDetach()
        {
            MessageController.MessageReceived -= OnMessageReceived;
            base.OnDetach();
        }

        public void OnMessageReceived(object sender, Message msg)
        {
            if (msg.Username != Username)
            {
                return;
            }

            this.Activity?.RunOnUiThread(new Action(() =>
            {
                RebuildMessagesAdapter();
                if (IsResumed)
                {
                    MessageController.MarkAsRead(Username);
                }
            }));
        }


    }

}