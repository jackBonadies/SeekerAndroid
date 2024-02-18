using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndriodApp1.Chatroom
{
    public class ChatroomInnerFragment : AndroidX.Fragment.App.Fragment //,PopupMenu.IOnMenuItemClickListener
    {
        private RecyclerView recyclerViewInner;
        private LinearLayoutManager recycleLayoutManager;
        private ChatroomInnerRecyclerAdapter recyclerAdapter;
        private List<Message> messagesInternal = null;
        private List<ChatroomController.StatusMessageUpdate> UI_statusMessagesInternal = null;

        //private string currentTickerText = string.Empty;
        private bool created = false;
        private View rootView = null;
        private View fabScrollToNewest = null;
        private EditText editTextEnterMessage = null;
        private Button sendMessage = null;
        public TextView currentTickerView = null;

        private RecyclerView2 recyclerViewStatusesView;
        private LinearLayoutManager recycleLayoutManagerStatuses;
        private ChatroomStatusesRecyclerAdapter recyclerUserStatusAdapter;

        public static Soulseek.RoomInfo OurRoomInfo = null;

        //these are for if we get killed by system on the chatroom inner fragment and we do not yet have the room list.
        public static bool cachedPrivate = false;
        public static bool cachedOwned = false;
        public static bool cachedMod = false;

        public bool IsPrivate()
        {
            return ChatroomController.IsPrivate(OurRoomInfo.Name);
        }
        public bool IsOwned()
        {
            return ChatroomController.IsOwnedByUs(OurRoomInfo);
        }
        public bool IsAutoJoin()
        {
            return ChatroomController.IsAutoJoinOn(OurRoomInfo);
        }
        public bool IsNotifyOn()
        {
            return ChatroomController.IsNotifyOn(OurRoomInfo);
        }
        public bool IsOperatedByUs()
        {
            if (ChatroomController.ModeratedRoomData.ContainsKey(OurRoomInfo.Name))
            {
                return ChatroomController.ModeratedRoomData[OurRoomInfo.Name].Users.Contains(SeekerState.Username);
            }
            return false;
        }

        public ChatroomInnerFragment() : base()
        {
            MainActivity.LogDebug("Chatroom Inner Fragment DEFAULT Constructor");
        }

        public ChatroomInnerFragment(Soulseek.RoomInfo roomInfo) : base()
        {
            MainActivity.LogDebug("Chatroom Inner Fragment ROOMINFO Constructor");

            OurRoomInfo = roomInfo;




        }

        public void HookUpEventHandlers(bool binding)
        {
            ChatroomController.MessageReceived -= OnMessageRecieved;
            ChatroomController.RoomMembershipRemoved -= OnRoomMembershipRemoved;
            ChatroomController.UserJoinedOrLeft -= OnUserJoinedOrLeft;
            ChatroomController.UserRoomStatusChanged -= OnUserRoomStatusChanged;
            ChatroomController.RoomTickerListReceived -= OnRoomTickerListReceived;
            ChatroomController.RoomTickerAdded -= OnRoomTickerAdded;
            if (binding)
            {
                ChatroomController.MessageReceived += OnMessageRecieved;
                ChatroomController.UserJoinedOrLeft += OnUserJoinedOrLeft;
                ChatroomController.UserRoomStatusChanged += OnUserRoomStatusChanged;
                ChatroomController.RoomTickerListReceived += OnRoomTickerListReceived;
                ChatroomController.RoomTickerAdded += OnRoomTickerAdded;
                ChatroomController.RoomMembershipRemoved += OnRoomMembershipRemoved;

            }
        }

        public void OnRoomTickerAdded(object sender, Soulseek.RoomTickerAddedEventArgs e)
        {
            if (OurRoomInfo != null && OurRoomInfo.Name == e.RoomName)
            {
                this.Activity?.RunOnUiThread(new Action(() =>
                {
                    this.SetTickerMessage(e.Ticker);
                }));
            }
        }


        public void OnRoomTickerListReceived(object sender, Soulseek.RoomTickerListReceivedEventArgs e)
        {
            //this is the first room ticker event you get...
            //nothing to do UNLESS we are not showing any tickers currently.. also make sure its our room..
            if (OurRoomInfo != null && OurRoomInfo.Name == e.RoomName)
            {
                this.Activity?.RunOnUiThread(new Action(() =>
                {
                    if (e.TickerCount == 0)
                    {
                        this.SetTickerMessage(new Soulseek.RoomTicker("", this.Resources.GetString(Resource.String.no_room_tickers)));
                    }
                    else
                    {
                        this.SetTickerMessage(e.Tickers.Last());
                    }
                }));
            }
        }

        //TODO: Why is this sometimes a popup anchored at (x,y) and otherwise a full screen context menu??
        //It goes through LongPress sometimes but other times it just shows the context menu on its own...
        public static Message MessagesLongClickData = null;
        public override bool OnContextItemSelected(IMenuItem item)
        {
            //if(Helpers.ShowSlskLinkContextMenu)
            //{
            //    return base.OnContextItemSelected(item);
            //}
            //MainActivity.LogDebug(MessagesLongClickData.MessageText + MessagesLongClickData.Username);
            string username = MessagesLongClickData.Username;
            if (CommonHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), username, SeekerState.ActiveActivityRef, this.View))
            {
                MainActivity.LogDebug("Handled by commons");
                return base.OnContextItemSelected(item);
            }
            switch (item.ItemId)
            {
                case 0: //"Copy Text"
                    CommonHelpers.CopyTextToClipboard(this.Activity, MessagesLongClickData.MessageText);
                    break;
                case 1: //"Ignore User"
                    SeekerApplication.AddToIgnoreListFeedback(this.Activity, username);
                    break;
                case 2://"Add User"
                    UserListActivity.AddUserAPI(SeekerState.ActiveActivityRef, username, null);
                    break;

            }
            return base.OnContextItemSelected(item);
        }

        public void OnMessageRecieved(object sender, MessageReceivedArgs roomArgs)
        {
            if (OurRoomInfo != null && OurRoomInfo.Name == roomArgs.RoomName)
            {
                this.Activity?.RunOnUiThread(new Action(() =>
                {

                    if (roomArgs.FromUsConfirmation) //special case, the message is already there we just need to update it
                    {
                        //the old way (of just doing count - 1) only worked when no messages got there in the mean time.
                        //to test just put a small delay in the send method and receive msg in meantime, and old method will fail...
                        int indexToUpdate = messagesInternal.Count - 1;
                        try
                        {
                            for (int i = messagesInternal.Count - 1; i >= 0; i--)
                            {
                                if (System.Object.ReferenceEquals(messagesInternal[i], roomArgs.Message))
                                {
                                    indexToUpdate = i;
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MainActivity.LogFirebase("OnMessageRecieved" + ex.Message);
                        }
                        recyclerAdapter.NotifyItemChanged(indexToUpdate);
                    }
                    else
                    {
                        //Message msg = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].Last(); //throws every time when testing with mass message test.
                        //the above method is O(n) and if anything gets enqueued in the mean time (which can happen since Enqueue() happens on background thread) it throws.
                        messagesInternal.Add(roomArgs.Message);
                        int lastVisibleItemPosition = recycleLayoutManager.FindLastVisibleItemPosition();
                        MainActivity.LogDebug("lastVisibleItemPosition : " + lastVisibleItemPosition);
                        recyclerAdapter.NotifyItemInserted(messagesInternal.Count - 1);

                        if (lastVisibleItemPosition >= messagesInternal.Count - 2) //since its based on the old list index so -1 -1
                        {
                            if (messagesInternal.Count != 0)
                            {
                                recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                            }
                        }
                        //we are now too far away.
                        if (recycleLayoutManager.ItemCount - lastVisibleItemPosition > scroll_pos_too_far)
                        {
                            fabScrollToNewest.Visibility = ViewStates.Visible;
                        }
                    }

                    //above is the new "refresh incremental" method

                    //this is the old "refresh everything" method
                    //messagesInternal = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].ToList();
                    //recyclerAdapter = new ChatroomInnerRecyclerAdapter(messagesInternal);
                    //recyclerViewInner.SetAdapter(recyclerAdapter);
                    //recyclerAdapter.NotifyDataSetChanged(); 
                    //if (messagesInternal.Count != 0) TEMP
                    //{
                    //    recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                    //}
                }));
            }
        }

        public void OnRoomMembershipRemoved(object sender, string room)
        {
            MainActivity.LogDebug("handler remove from " + room);



            if (OurRoomInfo != null && OurRoomInfo.Name == room)
            {
                this.Activity?.RunOnUiThread(new Action(() =>
                {
                    if (this.IsVisible)
                    {
                        MainActivity.LogDebug("pressed back from " + room);
                        this.Activity.OnBackPressed();
                    }
                    ChatroomController.GetRoomListApi();
                }));
            }
        }

        private void AddStatusMessageUI(string user, ChatroomController.StatusMessageUpdate statusMessage)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                //MainActivity.LogDebug("UI event handler for status view " + e.Joined);
                if (user == SeekerState.Username && UI_statusMessagesInternal.Count > 0)
                {
                    //this is to correct an issue where:
                    //  (non UI thread) we join and are added to the room data
                    //  (UI thread) we get the room data and set up (with count of 1)
                    //  (UI thread) the event handler for us being added finally gets run

                    if (UI_statusMessagesInternal.Last().Equals(statusMessage))
                    {
                        MainActivity.LogDebug("UI event - throwing away the duplicate..");
                        return; //we already have this exact status message.
                    }
                }
                UI_statusMessagesInternal.Add(statusMessage);
                int lastVisibleItemPosition = recycleLayoutManagerStatuses.FindLastVisibleItemPosition();
                MainActivity.LogDebug("lastVisibleItemPosition : " + lastVisibleItemPosition);
                recyclerUserStatusAdapter.NotifyItemInserted(UI_statusMessagesInternal.Count - 1);

                if (lastVisibleItemPosition >= UI_statusMessagesInternal.Count - 2) //since its based on the old list index so -1 -1
                {
                    if (UI_statusMessagesInternal.Count != 0)
                    {
                        recyclerViewStatusesView.ScrollToPosition(UI_statusMessagesInternal.Count - 1);
                    }
                }
            });
        }

        public void OnUserJoinedOrLeft(object sender, UserJoinedOrLeftEventArgs e)
        {
            //nothing to do UNLESS you are planning on showing something live.
            //maybe if you have a number counter, then its useful..
            if (!ChatroomActivity.ShowStatusesView)
            {
                return;
            }
            else
            {
                if (OurRoomInfo == null || OurRoomInfo.Name != e.RoomName)
                {
                    //not our room..
                    return;
                }
                //MainActivity.LogDebug("nonUI event handler for status view " + e.Joined);
                AddStatusMessageUI(e.User, e.StatusMessageUpdate.Value);
            }
        }

        public void OnUserRoomStatusChanged(object sender, UserRoomStatusChangedEventArgs e)
        {
            //nothing to do UNLESS you are planning on showing something live.
            //maybe if you have a number counter, then its useful..
            if (!ChatroomActivity.ShowStatusesView || !ChatroomActivity.ShowUserOnlineAwayStatusUpdates)
            {
                return;
            }
            else
            {
                if (OurRoomInfo == null || OurRoomInfo.Name != e.RoomName)
                {
                    //not our room..
                    return;
                }
                //MainActivity.LogDebug("nonUI event handler for status view " + e.Joined);
                AddStatusMessageUI(e.User, e.StatusMessageUpdate);
            }
        }

        public void SetStatusesView()
        {
            //#if DEBUG //exposes bug that is now fixed.
            //System.Threading.Thread.Sleep(1000);
            //#endif
            if (ChatroomActivity.ShowStatusesView)
            {
                recyclerViewStatusesView.Visibility = ViewStates.Visible;
            }
            else
            {
                recyclerViewStatusesView.Visibility = ViewStates.Gone;
                return;
            }

            if (ChatroomController.JoinedRoomStatusUpdateMessages.ContainsKey(OurRoomInfo.Name))
            {
                MainActivity.LogDebug("we have the room messages");
                UI_statusMessagesInternal = ChatroomController.JoinedRoomStatusUpdateMessages[OurRoomInfo.Name].ToList();
            }
            else
            {
                UI_statusMessagesInternal = new List<ChatroomController.StatusMessageUpdate>();
            }

            //MainActivity.LogDebug("SetStatusView Count: " + UI_statusMessagesInternal.Count);

            recyclerUserStatusAdapter = new ChatroomStatusesRecyclerAdapter(UI_statusMessagesInternal); //this depends tightly on MessageController... since these are just strings..
            recyclerViewStatusesView.SetAdapter(recyclerUserStatusAdapter);

            if (UI_statusMessagesInternal.Count != 0)
            {
                recyclerViewStatusesView.ScrollToPosition(UI_statusMessagesInternal.Count - 1);
            }
        }

        /// <summary>
        /// Since recyclerview does not have a click event itself.
        /// </summary>
        public class CustomClickEvent : Java.Lang.Object, Android.Views.View.IOnTouchListener
        {
            public RecyclerView RecyclerView;
            private float mStartX;
            private float mStartY;
            bool View.IOnTouchListener.OnTouch(View recyclerView, MotionEvent event1)
            {
                bool isConsumed = false;
                switch (event1.Action)
                {
                    case MotionEventActions.Down:
                        mStartX = event1.GetX();
                        mStartY = event1.GetY();
                        break;
                    case MotionEventActions.Up:
                        float endX = event1.GetX();
                        float endY = event1.GetY();
                        if (detectClick(mStartX, mStartY, endX, endY))
                        {
                            RecyclerView.PerformClick();
                        }
                        break;
                }
                return isConsumed;
            }

            private static bool detectClick(float startX, float startY, float endX, float endY)
            {
                return Math.Abs(startX - endX) < 3.0 && Math.Abs(startY - endY) < 3.0;
            }

            //void RecyclerView.IOnItemTouchListener.OnRequestDisallowInterceptTouchEvent(bool disallow)
            //{

            //}

            //void RecyclerView.IOnItemTouchListener.OnTouchEvent(RecyclerView recyclerView, MotionEvent @event)
            //{

            //}
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MainActivity.LogDebug("Chatroom Inner Fragment OnCreateView");

            //if (Username == null)
            //{
            //    Username = savedInstanceState.GetString("Inner_Username_ToMessage");
            //}

            rootView = inflater.Inflate(Resource.Layout.chatroom_inner_layout, container, false);
            currentTickerView = rootView.FindViewById<TextView>(Resource.Id.current_ticker);
            currentTickerView.Click += CurrentTickerView_Click;



            editTextEnterMessage = rootView.FindViewById<EditText>(Resource.Id.edit_gchat_message);
            sendMessage = rootView.FindViewById<Button>(Resource.Id.button_gchat_send);

            Soulseek.RoomData roomData = null;
            if (ChatroomController.HasRoomData(OurRoomInfo.Name))
            {
                MainActivity.LogDebug("we have the room data");
                roomData = ChatroomController.GetRoomData(OurRoomInfo.Name);
            }
            else
            {
                MainActivity.LogDebug("joining room " + OurRoomInfo.Name);
                if (SeekerState.currentlyLoggedIn)
                {
                    ChatroomController.JoinRoomApi(OurRoomInfo.Name, true, true, false, false);
                }
                else
                {
                    MainActivity.LogDebug("not logged in, on log in we will join");
                }
            }

            if (ChatroomController.JoinedRoomMessages.ContainsKey(OurRoomInfo.Name))
            {
                MainActivity.LogDebug("we have the room messages");
                messagesInternal = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].ToList();
            }
            else
            {
                messagesInternal = new List<Message>();
            }

            if (ChatroomController.JoinedRoomTickers.ContainsKey(OurRoomInfo.Name) && ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Count > 0)
            {
                MainActivity.LogDebug("we have the room tickers");
                var ticker = ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Last();
                SetTickerMessage(ticker);
            }
            else if (ChatroomController.JoinedRoomTickers.ContainsKey(OurRoomInfo.Name) && ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Count == 0)
            {
                MainActivity.LogDebug("no tickers yet");
                SetTickerMessage(new Soulseek.RoomTicker("", this.Resources.GetString(Resource.String.no_room_tickers)));
            }
            else
            {
                currentTickerView.Text = this.Resources.GetString(Resource.String.loading_current_ticker);
            }

            if (ChatroomActivity.ShowTickerView)
            {
                currentTickerView.Visibility = ViewStates.Visible;
            }
            else
            {
                currentTickerView.Visibility = ViewStates.Gone;
            }

            recyclerViewSmall = true;
            recyclerViewStatusesView = rootView.FindViewById<RecyclerView2>(Resource.Id.room_statuses_recycler_view);

            CustomClickEvent cce = new CustomClickEvent();
            cce.RecyclerView = recyclerViewStatusesView;
            recyclerViewStatusesView.SetOnTouchListener(cce);

            recyclerViewStatusesView.Click += RecyclerViewStatusesView_Click;
            recycleLayoutManagerStatuses = new LinearLayoutManager(Activity);
            recycleLayoutManagerStatuses.StackFromEnd = false;
            recycleLayoutManagerStatuses.ReverseLayout = false;
            recyclerViewStatusesView.SetLayoutManager(recycleLayoutManagerStatuses);
            fabScrollToNewest = rootView.FindViewById<View>(Resource.Id.bsbutton);
            (fabScrollToNewest as FloatingActionButton).SetImageResource(Resource.Drawable.arrow_down);
            fabScrollToNewest.Clickable = true;
            fabScrollToNewest.Click += ScrollToBottomClick;



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
            recyclerAdapter = new ChatroomInnerRecyclerAdapter(messagesInternal); //this depends tightly on MessageController... since these are just strings..
            recyclerViewInner.SetAdapter(recyclerAdapter);
            recyclerViewInner.SetLayoutManager(recycleLayoutManager);

            //does not work on sub23
            if ((int)Android.OS.Build.VERSION.SdkInt >= 23)
            {
                recyclerViewInner.ScrollChange += RecyclerViewInner_ScrollChange;
            }

            this.RegisterForContextMenu(recyclerViewInner);
            if (messagesInternal.Count != 0)
            {
                recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
            }
            MainActivity.LogDebug("currentlyInsideRoomName -- OnCreateView Inner -- " + ChatroomController.currentlyInsideRoomName);
            ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name;
            ChatroomController.UnreadRooms.TryRemove(OurRoomInfo.Name, out _);
            HookUpEventHandlers(true); //this NEEDS to be strictly before SetStatusesView
            MainActivity.LogDebug("set up statuses view");
            SetStatusesView();
            MainActivity.LogDebug("finish set up statuses view");

            created = true;

            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)ChatroomActivity.ChatroomActivityRef.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.chatroom_toolbar);
            myToolbar.InflateMenu(Resource.Menu.chatroom_inner_menu);
            myToolbar.Title = OurRoomInfo.Name;
            ChatroomActivity.ChatroomActivityRef.SetSupportActionBar(myToolbar);
            ChatroomActivity.ChatroomActivityRef.InvalidateOptionsMenu();
            return rootView;

        }

        private const int scroll_pos_too_far = 16;

        private void RecyclerViewInner_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            //the messages start at 0
            //and so what matters is how far the last message is from the count.
            //so if last message is 19 and count is 21 then you are one behind..
            if (recycleLayoutManager.FindLastVisibleItemPosition() == (recycleLayoutManager.ItemCount - 1) || recycleLayoutManager.ItemCount == 0) //you can see the latest message.
            {
                fabScrollToNewest.Visibility = ViewStates.Gone;
            }
            else if (recycleLayoutManager.ItemCount - recycleLayoutManager.FindLastVisibleItemPosition() > scroll_pos_too_far)
            {
                fabScrollToNewest.Visibility = ViewStates.Visible;
            }
            //MainActivity.LogDebug("count " + recycleLayoutManager.ItemCount);
            //MainActivity.LogDebug("last vis " + recycleLayoutManager.FindLastVisibleItemPosition());
        }

        private void ScrollToBottomClick(object sender, EventArgs e)
        {
            this.recyclerViewInner.ScrollToPosition(this.recyclerAdapter.ItemCount - 1);
        }

        private bool recyclerViewSmall = true;
        private void RecyclerViewStatusesView_Click(object sender, EventArgs e)
        {
            if (recyclerViewSmall)
            {
                //dont expand if there is nothing to show..
                if (this.recycleLayoutManagerStatuses.FindFirstCompletelyVisibleItemPosition() == 0 && this.recycleLayoutManagerStatuses.FindLastCompletelyVisibleItemPosition() >= this.recycleLayoutManagerStatuses.ItemCount - 1)
                {
                    MainActivity.LogDebug("too small to expand" + this.recycleLayoutManagerStatuses.FindLastCompletelyVisibleItemPosition());
                    MainActivity.LogDebug("too small to expand" + this.recycleLayoutManagerStatuses.FindFirstCompletelyVisibleItemPosition());
                    MainActivity.LogDebug("too small to expand");
                    return;
                }
            }

            int dps = 200;
            recyclerViewSmall = !recyclerViewSmall;
            if (recyclerViewSmall)
            {
                dps = 82;
            }
            float scale = this.Context.Resources.DisplayMetrics.Density;
            int pixels = (int)(dps * scale + 0.5f);

            recyclerViewStatusesView.LayoutParameters.Height = pixels;//in px
            recyclerViewStatusesView.ForceLayout(); //include children in the remeasure and relayout (not sure if necessary)
            recyclerViewStatusesView.Invalidate(); //redraw
            recyclerViewStatusesView.RequestLayout(); //relayout (for size changes)
        }

        //happens too late...
        //private void RecyclerViewStatusesView_LayoutChange(object sender, View.LayoutChangeEventArgs e)
        //{
        //    MainActivity.LogDebug("RecyclerViewStatusesView_LayoutChange" + e.OldTop + "   " + e.Top);
        //    MainActivity.LogDebug("RecyclerViewStatusesView_LayoutChange" + e.OldBottom + "   " + e.Bottom);
        //    MainActivity.LogDebug("RecyclerViewStatusesView_LayoutChange" + this.recycleLayoutManagerStatuses.FindLastCompletelyVisibleItemPosition());
        //    MainActivity.LogDebug("RecyclerViewStatusesView_LayoutChange" + this.recycleLayoutManagerStatuses.FindFirstCompletelyVisibleItemPosition());
        //    if(e.OldBottom > e.Bottom)
        //    {
        //        this.recycleLayoutManagerStatuses.ScrollToPosition(this.recycleLayoutManagerStatuses.ItemCount - 1);
        //    }
        //}

        private void CurrentTickerView_Click(object sender, EventArgs e)
        {
            TextView tickerView = (sender as TextView);
            if (tickerView.MaxLines == 2)
            {
                tickerView.SetMaxLines(int.MaxValue);
            }
            else
            {
                tickerView.SetMaxLines(2);
            }
        }

        //public static Android.Text.ISpanned FormatTickerHTML(Soulseek.RoomTicker t)
        //{
        //    return AndroidX.Core.Text.HtmlCompat.FromHtml(@"<font color=#cc0029> </font>", AndroidX.Core.Text.HtmlCompat.FromHtmlModeLegacy);
        //}

        private void SetTickerMessage(Soulseek.RoomTicker t)
        {
            if (t != null && currentTickerView != null)
            {
                if (t.Username == string.Empty)
                {
                    //for the no tickers msg
                    currentTickerView.Text = t.Message;
                }
                else
                {
                    currentTickerView.Text = t.Message + " --" + t.Username;
                }
            }
            else
            {
                //this is if we arent there anymore...... but shouldnt we have unbound?? or if it simply comes in too fast....
                if (t == null)
                {
                    MainActivity.LogDebug("null ticker");
                }
                else
                {
                    MainActivity.LogDebug("null ticker view");
                }
            }
        }


        private void EditTextEnterMessage_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
            {
                e.Handled = true;
                //send the message and record our send message..
                SendChatroomMessageAPI(OurRoomInfo.Name, new Message(SeekerState.Username, -1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

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
                SendChatroomMessageAPI(OurRoomInfo.Name, new Message(SeekerState.Username, -1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

                editTextEnterMessage.Text = string.Empty;
            }
        }

        public override void OnAttach(Context activity)
        {
            MainActivity.LogDebug("OnAttach chatroom inner fragment !!");
            if (created) //attach can happen before we created our view...
            {
                MainActivity.LogDebug("iscreated= true OnAttach chatroom inner fragment !!");
                //try
                //{
                //    MainActivity.LogDebug("currentlyInsideRoomName -- OnAttach -- " + ChatroomController.currentlyInsideRoomName);
                //    ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name; //nullref
                //}
                //catch(Exception e)
                //{
                //    MainActivity.LogDebug("1" + e.Message);
                //}
                try
                {
                    messagesInternal = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].ToList();
                    recyclerAdapter = new ChatroomInnerRecyclerAdapter(messagesInternal); //this depends tightly on MessageController... since these are just strings..
                    recyclerViewInner.SetAdapter(recyclerAdapter);
                    if (messagesInternal.Count != 0)
                    {
                        recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                    }
                }
                catch (Exception e)
                {
                    MainActivity.LogDebug("2" + e.Message);
                }



                MainActivity.LogDebug("set setatus view");
                SetStatusesView();
                MainActivity.LogDebug("set setatus view end");
                MainActivity.LogDebug("hook up event handlers ");
                HookUpEventHandlers(true);
                MainActivity.LogDebug("hook up event handlers end");

            }
            base.OnAttach(activity);
        }

        public override void OnPause()
        {
            MainActivity.LogDebug("currentlyInsideRoomName OnPause -- nulling");
            ChatroomController.currentlyInsideRoomName = string.Empty;
            base.OnPause();
        }

        public override void OnResume()
        {
            MainActivity.LogDebug("currentlyInsideRoomName OnResume");
            if (OurRoomInfo != null)
            {
                ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name;
                ChatroomController.UnreadRooms.TryRemove(OurRoomInfo.Name, out _);
            }
            base.OnResume();
        }

        public override void OnDetach()
        {
            MainActivity.LogDebug("currentlyInsideRoomName OnDetach -- nulling");

            HookUpEventHandlers(false);
            base.OnDetach();
        }


        public void SendChatroomMessageAPI(string roomName, Message msg)
        {

            Action<Task> actualActionToPerform = new Action<Task>((Task t) =>
            {
                if (t.IsFaulted)
                {
                    //only show once for the original fault.
                    MainActivity.LogDebug("task is faulted, prop? " + (t.Exception.InnerException is FaultPropagationException)); //t.Exception is always Aggregate Exception..
                    if (!(t.Exception.InnerException is FaultPropagationException))
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, this.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                    }
                    throw new FaultPropagationException();
                }
                SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    ChatroomController.SendChatroomMessageLogic(roomName, msg);
                }));
            });

            if (!SeekerState.currentlyLoggedIn)
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, this.Resources.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show();
                });
                return;
            }
            if (string.IsNullOrEmpty(msg.MessageText))
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, this.Resources.GetString(Resource.String.empty_message_error), ToastLength.Short).Show();
                });
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                MainActivity.LogDebug("CurrentlyLoggedInButDisconnectedState: TRUE");
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return;
                }
                SeekerApplication.OurCurrentLoginTask = t.ContinueWith(actualActionToPerform);
            }
            else
            {
                if (MainActivity.IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(actualActionToPerform, SeekerApplication.GetString(Resource.String.messageWillSendOnReConnect)))
                {
                    return;
                }
                else
                {
                    ChatroomController.SendChatroomMessageLogic(roomName, msg);
                }
            }

        }



        private void SendMessage_Click(object sender, EventArgs e)
        {
            //send the message and record our send message..
            SendChatroomMessageAPI(OurRoomInfo.Name, new Message(SeekerState.Username, -1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

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
    }
}