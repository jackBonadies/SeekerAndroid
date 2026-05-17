using Android.Animation;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Common;
using Common.Messages;
using Google.Android.Material.FloatingActionButton;
using Seeker.Helpers;
using Seeker.Helpers.AnchoredMenu;
using Seeker.Services;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Seeker.Chatroom
{
    public class ChatroomInnerFragment : AndroidX.Fragment.App.Fragment //,PopupMenu.IOnMenuItemClickListener
    {
        private RecyclerView recyclerViewInner;
        private LinearLayoutManager recycleLayoutManager;
        private ChatroomInnerRecyclerAdapter recyclerAdapter;
        private List<Message> messagesInternal = null;
        private List<StatusMessageUpdate> UI_statusMessagesInternal = null;

        //private string currentTickerText = string.Empty;
        private bool created = false;
        private View rootView = null;
        private View fabScrollToNewest = null;
        private EditText editTextEnterMessage = null;
        private ImageButton sendMessage = null;
        private TextView currentTickerView = null;
        private ObjectAnimator tickerLoadingPulseAnimator = null;

        private ViewFlipper joinEmptyStateFlipper = null;
        private TextView joinPendingTitle = null;
        private TextView joinFailedMessage = null;
        private TextView joinFailedSubtitle = null;
        private Button joinFailedRetry = null;

        private enum JoinEmptyState
        {
            None = -1,
            Pending = 0,
            Error = 1,
        }

        private JoinEmptyState currentJoinEmptyState = JoinEmptyState.None;

        private RecyclerView2 recyclerViewStatusesView;
        private View statusesContainer;
        private TextView statusesEmptyPlaceholder;
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
                return ChatroomController.ModeratedRoomData[OurRoomInfo.Name].Users.Contains(PreferencesState.Username);
            }
            return false;
        }

        public ChatroomInnerFragment() : base()
        {
            Logger.Debug("Chatroom Inner Fragment DEFAULT Constructor");
        }

        public ChatroomInnerFragment(Soulseek.RoomInfo roomInfo) : base()
        {
            Logger.Debug("Chatroom Inner Fragment ROOMINFO Constructor");

            OurRoomInfo = roomInfo;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            Activity?.AddMenuProvider(new InnerMenuProvider(this), ViewLifecycleOwner, AndroidX.Lifecycle.Lifecycle.State.Resumed);
        }

        public void HookUpEventHandlers(bool binding)
        {
            ChatroomController.MessageReceived -= OnMessageRecieved;
            ChatroomController.RoomMembershipRemoved -= OnRoomMembershipRemoved;
            ChatroomController.UserJoinedOrLeft -= OnUserJoinedOrLeft;
            ChatroomController.UserRoomStatusChanged -= OnUserRoomStatusChanged;
            ChatroomController.RoomTickerListReceived -= OnRoomTickerListReceived;
            ChatroomController.RoomTickerAdded -= OnRoomTickerAdded;
            ChatroomController.RoomJoinFailed -= OnRoomJoinFailed;
            ChatroomController.RoomDataReceived -= OnRoomDataReceivedForJoinState;
            if (binding)
            {
                ChatroomController.MessageReceived += OnMessageRecieved;
                ChatroomController.UserJoinedOrLeft += OnUserJoinedOrLeft;
                ChatroomController.UserRoomStatusChanged += OnUserRoomStatusChanged;
                ChatroomController.RoomTickerListReceived += OnRoomTickerListReceived;
                ChatroomController.RoomTickerAdded += OnRoomTickerAdded;
                ChatroomController.RoomMembershipRemoved += OnRoomMembershipRemoved;
                ChatroomController.RoomJoinFailed += OnRoomJoinFailed;
                ChatroomController.RoomDataReceived += OnRoomDataReceivedForJoinState;
            }
        }

        private void ApplyInitialJoinStateUi()
        {
            if (ChatroomController.HasRoomData(OurRoomInfo.Name))
            {
                SetJoinEmptyState(JoinEmptyState.None);
                return;
            }
            if (ChatroomController.RoomJoinStates.TryGetValue(OurRoomInfo.Name, out var status))
            {
                ApplyJoinStateUi(status.State, status.FailureMessage);
            }
            else
            {
                ApplyJoinStateUi(RoomJoinState.Pending, null);
            }
        }

        private void ApplyJoinStateUi(RoomJoinState state, string failureMessage)
        {
            if (joinEmptyStateFlipper == null)
            {
                return;
            }
            switch (state)
            {
                case RoomJoinState.Joined:
                    SetJoinEmptyState(JoinEmptyState.None);
                    UpdateSendEnabled();
                    this.Activity?.InvalidateOptionsMenu();
                    break;
                case RoomJoinState.Pending:
                    if (joinPendingTitle != null)
                    {
                        joinPendingTitle.Text = string.Format(this.Resources.GetString(Resource.String.room_join_pending), OurRoomInfo.Name);
                    }
                    SetJoinEmptyState(JoinEmptyState.Pending);
                    UpdateSendEnabled();
                    this.Activity?.InvalidateOptionsMenu();
                    break;
                case RoomJoinState.Forbidden:
                    if (joinFailedMessage != null)
                    {
                        joinFailedMessage.Text = this.Resources.GetString(Resource.String.room_join_failed);
                    }
                    if (joinFailedSubtitle != null)
                    {
                        joinFailedSubtitle.Text = this.Resources.GetString(Resource.String.room_join_forbidden);
                    }
                    SetJoinEmptyState(JoinEmptyState.Error);
                    UpdateSendEnabled();
                    break;
                case RoomJoinState.Failed:
                    if (joinFailedMessage != null)
                    {
                        joinFailedMessage.Text = this.Resources.GetString(Resource.String.room_join_failed);
                    }
                    if (joinFailedSubtitle != null)
                    {
                        joinFailedSubtitle.Text = failureMessage ?? string.Empty;
                    }
                    SetJoinEmptyState(JoinEmptyState.Error);
                    UpdateSendEnabled();
                    break;
            }
            SetActivityStatusesVisibility();
            SetTickerVisibility();
        }

        private void SetJoinEmptyState(JoinEmptyState state)
        {
            currentJoinEmptyState = state;
            if (joinEmptyStateFlipper == null)
            {
                return;
            }
            if (state == JoinEmptyState.None)
            {
                joinEmptyStateFlipper.Visibility = ViewStates.Gone;
                return;
            }
            if (joinEmptyStateFlipper.DisplayedChild != (int)state)
            {
                joinEmptyStateFlipper.DisplayedChild = (int)state;
            }
            joinEmptyStateFlipper.Visibility = ViewStates.Visible;
        }

        private void JoinFailedRetry_Click(object sender, EventArgs e)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                return;
            }
            ApplyJoinStateUi(RoomJoinState.Pending, null);
            ChatroomController.JoinRoomApi(OurRoomInfo.Name, true, true, feedback: true, false);
        }

        private void OnRoomJoinFailed(object sender, RoomJoinFailedEventArgs e)
        {
            if (OurRoomInfo == null || OurRoomInfo.Name != e.RoomName)
            {
                return;
            }
            this.Activity?.RunOnUiThread(() =>
            {
                ApplyJoinStateUi(
                    e.IsForbidden ? RoomJoinState.Forbidden : RoomJoinState.Failed,
                    e.Exception?.Message);
            });
        }

        private void OnRoomDataReceivedForJoinState(object sender, EventArgs e)
        {
            if (OurRoomInfo == null)
            {
                return;
            }
            if (!ChatroomController.RoomJoinStates.TryGetValue(OurRoomInfo.Name, out var status) || status.State != RoomJoinState.Joined)
            {
                return;
            }
            this.Activity?.RunOnUiThread(() =>
            {
                ApplyJoinStateUi(RoomJoinState.Joined, null);
            });
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
                            Logger.Firebase("OnMessageRecieved" + ex.Message);
                        }
                        recyclerAdapter.NotifyItemChanged(indexToUpdate);
                    }
                    else
                    {
                        //Message msg = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].Last(); //throws every time when testing with mass message test.
                        //the above method is O(n) and if anything gets enqueued in the mean time (which can happen since Enqueue() happens on background thread) it throws.
                        messagesInternal.Add(roomArgs.Message);
                        int lastVisibleItemPosition = recycleLayoutManager.FindLastVisibleItemPosition();
                        Logger.Debug("lastVisibleItemPosition : " + lastVisibleItemPosition);
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

        private void OnRoomMembershipRemoved(object sender, string room)
        {
            Logger.Debug("handler remove from " + room);

            if (OurRoomInfo != null && OurRoomInfo.Name == room)
            {
                this.Activity?.RunOnUiThread(new Action(() =>
                {
                    if (this.IsVisible)
                    {
                        Logger.Debug("pressed back from " + room);
                        this.Activity.OnBackPressedDispatcher.OnBackPressed();
                    }
                    ChatroomController.GetRoomListApi();
                }));
            }
        }

        private void AddStatusMessageUI(string user, StatusMessageUpdate statusMessage)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                //Logger.Debug("UI event handler for status view " + e.Joined);
                if (user == PreferencesState.Username && UI_statusMessagesInternal.Count > 0)
                {
                    //this is to correct an issue where:
                    //  (non UI thread) we join and are added to the room data
                    //  (UI thread) we get the room data and set up (with count of 1)
                    //  (UI thread) the event handler for us being added finally gets run

                    if (UI_statusMessagesInternal.Last().Equals(statusMessage))
                    {
                        Logger.Debug("UI event - throwing away the duplicate..");
                        return; //we already have this exact status message.
                    }
                }
                UI_statusMessagesInternal.Add(statusMessage);
                UpdateStatusesEmptyPlaceholder();
                int lastVisibleItemPosition = recycleLayoutManagerStatuses.FindLastVisibleItemPosition();
                Logger.Debug("lastVisibleItemPosition : " + lastVisibleItemPosition);
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

        private void OnUserJoinedOrLeft(object sender, UserJoinedOrLeftEventArgs e)
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
                //Logger.Debug("nonUI event handler for status view " + e.Joined);
                AddStatusMessageUI(e.User, e.StatusMessageUpdate.Value);
            }
        }

        private void OnUserRoomStatusChanged(object sender, UserRoomStatusChangedEventArgs e)
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
                //Logger.Debug("nonUI event handler for status view " + e.Joined);
                AddStatusMessageUI(e.User, e.StatusMessageUpdate);
            }
        }

        private void SetActivityStatusesVisibility()
        {
            if (statusesContainer == null)
            {
                return;
            }
            if (ChatroomActivity.ShowStatusesView && currentJoinEmptyState != JoinEmptyState.Error)
            {
                statusesContainer.Visibility = ViewStates.Visible;
            }
            else
            {
                statusesContainer.Visibility = ViewStates.Gone;
            }
        }

        private void UpdateStatusesEmptyPlaceholder()
        {
            if (recyclerViewStatusesView == null || statusesEmptyPlaceholder == null)
            {
                return;
            }
            bool isEmpty = UI_statusMessagesInternal == null || UI_statusMessagesInternal.Count == 0;
            recyclerViewStatusesView.Visibility = isEmpty ? ViewStates.Gone : ViewStates.Visible;
            statusesEmptyPlaceholder.Visibility = isEmpty ? ViewStates.Visible : ViewStates.Gone;
        }

        public void SetActivityStatusesView()
        {
            SetActivityStatusesVisibility();
            if (!ChatroomActivity.ShowStatusesView)
            {
                return;
            }

            if (ChatroomController.JoinedRoomStatusUpdateMessages.ContainsKey(OurRoomInfo.Name))
            {
                Logger.Debug("we have the room messages");
                UI_statusMessagesInternal = ChatroomController.JoinedRoomStatusUpdateMessages[OurRoomInfo.Name].ToList();
            }
            else
            {
                UI_statusMessagesInternal = new List<StatusMessageUpdate>();
            }

            //Logger.Debug("SetStatusView Count: " + UI_statusMessagesInternal.Count);

            recyclerUserStatusAdapter = new ChatroomStatusesRecyclerAdapter(UI_statusMessagesInternal); //this depends tightly on MessageController... since these are just strings..
            recyclerViewStatusesView.SetAdapter(recyclerUserStatusAdapter);

            UpdateStatusesEmptyPlaceholder();

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
            Logger.Debug("Chatroom Inner Fragment OnCreateView");

            //if (Username == null)
            //{
            //    Username = savedInstanceState.GetString("Inner_Username_ToMessage");
            //}

            rootView = inflater.Inflate(Resource.Layout.chatroom_inner_layout, container, false);
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(rootView, new BottomOnlyInsetsListener());
            currentTickerView = rootView.FindViewById<TextView>(Resource.Id.current_ticker);
            currentTickerView.Click += CurrentTickerView_Click;



            editTextEnterMessage = rootView.FindViewById<EditText>(Resource.Id.edit_gchat_message);
            sendMessage = rootView.FindViewById<ImageButton>(Resource.Id.button_gchat_send);

            Soulseek.RoomData roomData = null;
            if (ChatroomController.HasRoomData(OurRoomInfo.Name))
            {
                Logger.Debug("we have the room data");
                roomData = ChatroomController.GetRoomData(OurRoomInfo.Name);
            }
            else
            {
                Logger.Debug("joining room " + OurRoomInfo.Name);
                if (PreferencesState.CurrentlyLoggedIn)
                {
                    ChatroomController.JoinRoomApi(OurRoomInfo.Name, true, true, feedback: true, false);
                }
                else
                {
                    Logger.Debug("not logged in, on log in we will join");
                }
            }

            if (ChatroomController.JoinedRoomMessages.ContainsKey(OurRoomInfo.Name))
            {
                Logger.Debug("we have the room messages");
                messagesInternal = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].ToList();
            }
            else
            {
                messagesInternal = new List<Message>();
            }

            if (ChatroomController.JoinedRoomTickers.ContainsKey(OurRoomInfo.Name) && ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Count > 0)
            {
                Logger.Debug("we have the room tickers");
                var ticker = ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Last();
                SetTickerMessage(ticker);
            }
            else if (ChatroomController.JoinedRoomTickers.ContainsKey(OurRoomInfo.Name) && ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Count == 0)
            {
                Logger.Debug("no tickers yet");
                SetTickerMessage(new Soulseek.RoomTicker("", this.Resources.GetString(Resource.String.no_room_tickers)));
            }
            else
            {
                var s = new SpannableString(this.Resources.GetString(Resource.String.chatroom_loading_ticker));
                s.SetSpan(new StyleSpan(TypefaceStyle.Italic), 0, s.Length(), SpanTypes.ExclusiveExclusive);
                currentTickerView.TextFormatted = s;
                StartTickerLoadingPulse();
            }

            SetTickerVisibility();

            recyclerViewSmall = true;
            recyclerViewStatusesView = rootView.FindViewById<RecyclerView2>(Resource.Id.room_statuses_recycler_view);
            statusesContainer = rootView.FindViewById<View>(Resource.Id.room_statuses_container);
            statusesEmptyPlaceholder = rootView.FindViewById<TextView>(Resource.Id.room_statuses_empty);

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



            UpdateSendEnabled();
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

            recyclerViewInner.ScrollChange += RecyclerViewInner_ScrollChange;

            if (messagesInternal.Count != 0)
            {
                recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
            }
            Logger.Debug("currentlyInsideRoomName -- OnCreateView Inner -- " + ChatroomController.currentlyInsideRoomName);
            ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name;
            ChatroomController.UnreadRooms.TryRemove(OurRoomInfo.Name, out _);
            joinEmptyStateFlipper = rootView.FindViewById<ViewFlipper>(Resource.Id.joinEmptyStateFlipper);
            joinPendingTitle = rootView.FindViewById<TextView>(Resource.Id.joinPendingTitle);
            joinFailedMessage = rootView.FindViewById<TextView>(Resource.Id.joinFailedMessage);
            joinFailedSubtitle = rootView.FindViewById<TextView>(Resource.Id.joinFailedSubtitle);
            joinFailedRetry = rootView.FindViewById<Button>(Resource.Id.joinFailedRetry);
            joinFailedRetry.Click += JoinFailedRetry_Click;
            ApplyInitialJoinStateUi();

            HookUpEventHandlers(true); //this NEEDS to be strictly before SetStatusesView
            Logger.Debug("set up statuses view");
            SetActivityStatusesView();
            Logger.Debug("finish set up statuses view");

            created = true;

            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)ChatroomActivity.ChatroomActivityRef.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.chatroom_toolbar);
            myToolbar.InflateMenu(Resource.Menu.chatroom_inner_menu);
            myToolbar.Title = OurRoomInfo.Name;
            ChatroomActivity.ChatroomActivityRef.SetSupportActionBar(myToolbar);
            ChatroomActivity.ChatroomActivityRef.InvalidateOptionsMenu();
            return rootView;

        }

        public void SetTickerVisibility()
        {
            if (ChatroomActivity.ShowTickerView && currentJoinEmptyState != JoinEmptyState.Error)
            {
                currentTickerView.Visibility = ViewStates.Visible;
            }
            else
            {
                currentTickerView.Visibility = ViewStates.Gone;
            }
        }

        private void StartTickerLoadingPulse()
        {
            if (currentTickerView == null)
            {
                return;
            }
            if (tickerLoadingPulseAnimator != null && tickerLoadingPulseAnimator.IsRunning)
            {
                return;
            }
            tickerLoadingPulseAnimator = ObjectAnimator.OfFloat(currentTickerView, "alpha", 1f, 0.3f);
            tickerLoadingPulseAnimator.SetDuration(800);
            tickerLoadingPulseAnimator.RepeatMode = ValueAnimatorRepeatMode.Reverse;
            tickerLoadingPulseAnimator.RepeatCount = ValueAnimator.Infinite;
            tickerLoadingPulseAnimator.Start();
        }

        private void StopTickerLoadingPulse()
        {
            if (tickerLoadingPulseAnimator != null)
            {
                tickerLoadingPulseAnimator.Cancel();
                tickerLoadingPulseAnimator = null;
            }
            if (currentTickerView != null)
            {
                currentTickerView.Alpha = 1f;
            }
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
            //Logger.Debug("count " + recycleLayoutManager.ItemCount);
            //Logger.Debug("last vis " + recycleLayoutManager.FindLastVisibleItemPosition());
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
                    Logger.Debug("too small to expand" + this.recycleLayoutManagerStatuses.FindLastCompletelyVisibleItemPosition());
                    Logger.Debug("too small to expand" + this.recycleLayoutManagerStatuses.FindFirstCompletelyVisibleItemPosition());
                    Logger.Debug("too small to expand");
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

            statusesContainer.LayoutParameters.Height = pixels;//in px
            statusesContainer.ForceLayout(); //include children in the remeasure and relayout (not sure if necessary)
            statusesContainer.Invalidate(); //redraw
            statusesContainer.RequestLayout(); //relayout (for size changes)
        }

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
                StopTickerLoadingPulse();
                var builder = UiHelpers.BuildTickerSpan(t, currentTickerView.Context);
                currentTickerView.SetText(builder, TextView.BufferType.Spannable);
            }
            else
            {
                //this is if we arent there anymore...... but shouldnt we have unbound?? or if it simply comes in too fast....
                if (t == null)
                {
                    Logger.Debug("null ticker");
                }
                else
                {
                    Logger.Debug("null ticker view");
                }
            }
        }


        private void EditTextEnterMessage_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
            {
                e.Handled = true;
                //send the message and record our send message..
                SendChatroomMessageAPI(OurRoomInfo.Name, new Message(PreferencesState.Username, -1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

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
                SendChatroomMessageAPI(OurRoomInfo.Name, new Message(PreferencesState.Username, -1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

                editTextEnterMessage.Text = string.Empty;
            }
        }

        public override void OnAttach(Context activity)
        {
            Logger.Debug("OnAttach chatroom inner fragment !!");
            if (created) //attach can happen before we created our view...
            {
                Logger.Debug("iscreated= true OnAttach chatroom inner fragment !!");
                //try
                //{
                //    Logger.Debug("currentlyInsideRoomName -- OnAttach -- " + ChatroomController.currentlyInsideRoomName);
                //    ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name; //nullref
                //}
                //catch(Exception e)
                //{
                //    Logger.Debug("1" + e.Message);
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
                    Logger.Debug("2" + e.Message);
                }



                Logger.Debug("set setatus view");
                SetActivityStatusesView();
                Logger.Debug("set setatus view end");
                Logger.Debug("hook up event handlers ");
                HookUpEventHandlers(true);
                Logger.Debug("hook up event handlers end");

            }
            base.OnAttach(activity);
        }

        public override void OnPause()
        {
            Logger.Debug("currentlyInsideRoomName OnPause -- nulling");
            ChatroomController.currentlyInsideRoomName = string.Empty;
            StopTickerLoadingPulse();
            base.OnPause();
        }

        public override void OnResume()
        {
            Logger.Debug("currentlyInsideRoomName OnResume");
            if (OurRoomInfo != null)
            {
                ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name;
                ChatroomController.UnreadRooms.TryRemove(OurRoomInfo.Name, out _);
            }
            base.OnResume();
        }

        public override void OnDetach()
        {
            Logger.Debug("currentlyInsideRoomName OnDetach -- nulling");

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
                    Logger.Debug("task is faulted, prop? " + (t.Exception.InnerException is FaultPropagationException)); //t.Exception is always Aggregate Exception..
                    if (!(t.Exception.InnerException is FaultPropagationException))
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                    }
                    throw new FaultPropagationException();
                }
                SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    ChatroomController.SendChatroomMessageLogic(roomName, msg);
                }));
            });

            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short);
                return;
            }
            if (string.IsNullOrEmpty(msg.MessageText))
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.empty_message_error), ToastLength.Short);
                return;
            }
            SessionService.Instance.RunWithReconnect(actualActionToPerform, SeekerApplication.GetString(Resource.String.messageWillSendOnReConnect));

        }



        private void SendMessage_Click(object sender, EventArgs e)
        {
            //send the message and record our send message..
            SendChatroomMessageAPI(OurRoomInfo.Name, new Message(PreferencesState.Username, -1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

            editTextEnterMessage.Text = string.Empty;
        }

        private void EditTextEnterMessage_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            UpdateSendEnabled();
        }

        private void UpdateSendEnabled()
        {
            if (sendMessage == null || editTextEnterMessage == null)
            {
                return;
            }
            bool hasText = editTextEnterMessage.Text != null && editTextEnterMessage.Text.ToString() != string.Empty;
            bool joined = OurRoomInfo != null && ChatroomController.HasRoomData(OurRoomInfo.Name);
            bool enabled = hasText && joined;
            sendMessage.Enabled = enabled;
            sendMessage.Alpha = enabled ? 1.0f : 0.38f;
        }

        public void ShowSetTickerDialog(string roomToInvite)
        {
            void OkayAction(object sender, string textInput)
            {
                ChatroomController.SetTickerApi(roomToInvite, textInput, true);
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    UiHelpers._dialogInstance?.Dismiss(); // TODO why?
                }
            }

            UiHelpers.ShowSimpleDialog(
                this.Activity,
                Resource.Layout.edit_text_dialog_content,
                this.Resources.GetString(Resource.String.set_ticker),
                OkayAction,
                this.Resources.GetString(Resource.String.send),
                null,
                this.Resources.GetString(Resource.String.type_chatroom_ticker_message),
                this.Resources.GetString(Resource.String.cancel),
                this.Resources.GetString(Resource.String.must_type_ticker_text),
                true);
        }

        public void ShowAllTickersDialog(string roomName)
        {
            Logger.InfoFirebase("ShowAllTickersDialog" + (Activity?.IsFinishing) + (Activity?.IsDestroyed) + ParentFragmentManager.IsDestroyed);
            var tickerDialog = new AllTickersDialog(roomName);
            tickerDialog.Show(ParentFragmentManager, "ticker dialog");
        }

        public void ShowUserListDialog(Soulseek.RoomInfo roomInfo, bool isPrivate)
        {
            if (!ChatroomController.JoinedRoomData.ContainsKey(roomInfo.Name))
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.room_data_still_loading), ToastLength.Short);
                return;
            }
            var roomUserListDialog = new RoomUserListDialog(roomInfo.Name, isPrivate);
            roomUserListDialog.Show(ParentFragmentManager, "room user list dialog");
        }

        private AndroidX.AppCompat.App.AlertDialog inviteDialogInstance;

        public void ShowInviteUserDialog(string roomToInvite)
        {
            Logger.InfoFirebase("ShowInviteUserDialog" + (Activity?.IsFinishing) + (Activity?.IsDestroyed));
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(RequireContext());
            builder.SetTitle(Resources.GetString(Resource.String.inviteuser));

            View viewInflated = LayoutInflater.From(RequireContext()).Inflate(Resource.Layout.autocomplete_user_dialog_content, (ViewGroup)Activity.FindViewById(Android.Resource.Id.Content).RootView, false);

            AutoCompleteTextView input = (AutoCompleteTextView)viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserEditText);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(input);

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string userToAdd = input.Text;
                if (userToAdd == null || userToAdd == string.Empty)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_type_a_username_to_invite), ToastLength.Short);
                    (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    return;
                }
                SeekerState.RecentUsersManager.AddUserToTop(userToAdd, true);
                ChatroomController.AddRemoveUserToPrivateRoomAPI(roomToInvite, userToAdd, true, false);
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    inviteDialogInstance.Dismiss();
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
                    inviteDialogInstance.Dismiss();
                }
            });

            var editorAction = UiHelpers.MakeDialogEditorAction(Activity?.FindViewById(Android.Resource.Id.Content)?.RootView, eventHandler);

            var keypressAction = UiHelpers.MakeDialogKeyPressAction(Activity?.FindViewById(Android.Resource.Id.Content)?.RootView, eventHandler);

            input.KeyPress += keypressAction;
            input.EditorAction += editorAction;
            input.FocusChange += UiHelpers.OnFocusAdjustNothing;

            builder.SetPositiveButton(Resources.GetString(Resource.String.invite), eventHandler);
            builder.SetNegativeButton(Resources.GetString(Resource.String.cancel), eventHandlerCancel);

            inviteDialogInstance = builder.Create();
            try
            {
                inviteDialogInstance.Show();
                UiHelpers.DoNotEnablePositiveUntilText(inviteDialogInstance, input);
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("invite WindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = Activity?.IsFinishing == true;
                    Logger.Firebase("invite WindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("invite Exception null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = Activity?.IsFinishing == true;
                    Logger.Firebase("invite Exception are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
        }

        private class InnerMenuProvider : Java.Lang.Object, AndroidX.Core.View.IMenuProvider
        {
            private readonly ChatroomInnerFragment fragment;

            public InnerMenuProvider(ChatroomInnerFragment fragment)
            {
                this.fragment = fragment;
            }

            public void OnCreateMenu(IMenu menu, MenuInflater menuInflater)
            {
                menuInflater.Inflate(Resource.Menu.chatroom_inner_menu, menu);
                var overflowItem = menu.FindItem(Resource.Id.action_chatroom_overflow);
                var overflowView = overflowItem?.ActionView;
                if (overflowView != null)
                {
                    overflowView.Click -= OnOverflowClick;
                    overflowView.Click += OnOverflowClick;
                }
            }

            public void OnPrepareMenu(IMenu menu)
            {
            }

            public void OnMenuClosed(IMenu menu)
            {
            }

            public bool OnMenuItemSelected(IMenuItem item)
            {
                return false;
            }

            private void OnOverflowClick(object sender, EventArgs e)
            {
                var anchor = sender as View;
                if (anchor == null || OurRoomInfo == null)
                {
                    return;
                }
                var config = BuildConfig();
                if (config.Rows.Count == 0)
                {
                    return;
                }
                AnchoredMenuPopup.Show(anchor, config);
            }

            private AnchoredMenuConfig BuildConfig()
            {
                var ctx = fragment.RequireContext();
                var activity = fragment.Activity as ChatroomActivity;
                string roomName = OurRoomInfo.Name;

                bool isPrivate;
                bool isOwnedByUs;
                bool isOperator;
                if (ChatroomController.RoomList != null)
                {
                    isPrivate = fragment.IsPrivate();
                    isOwnedByUs = fragment.IsOwned();
                    isOperator = fragment.IsOperatedByUs();
                }
                else
                {
                    isPrivate = cachedPrivate;
                    isOwnedByUs = cachedOwned;
                    isOperator = cachedMod;
                }
                bool joined = ChatroomController.HasRoomData(roomName);

                var config = new AnchoredMenuConfig();

                if (joined)
                {
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.all_users_group_30dp,
                        Label = ctx.GetString(Resource.String.view_users),
                        OnClick = () => fragment.ShowUserListDialog(OurRoomInfo, ChatroomController.IsPrivate(roomName))
                    });
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.pinboard_all_tickers_30dp,
                        Label = ctx.GetString(Resource.String.view_all_tickers),
                        OnClick = () => fragment.ShowAllTickersDialog(roomName)
                    });
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.keep_set_ticker_30dp,
                        Label = ctx.GetString(Resource.String.setticker),
                        OnClick = () => fragment.ShowSetTickerDialog(roomName)
                    });
                }

                config.Rows.Add(new AnchoredMenuRow
                {
                    Kind = AnchoredMenuRowKind.Checkable,
                    IconResId = Resource.Drawable.autorenew_autojoin_30dp,
                    Label = ctx.GetString(Resource.String.auto_join),
                    GetChecked = () => fragment.IsAutoJoin(),
                    OnChecked = _ => ChatroomController.ToggleAutoJoin(roomName, true, activity)
                });
                config.Rows.Add(new AnchoredMenuRow
                {
                    Kind = AnchoredMenuRowKind.Checkable,
                    IconResId = Resource.Drawable.notifications_outline_30dp,
                    Label = ctx.GetString(Resource.String.notification),
                    GetChecked = () => fragment.IsNotifyOn(),
                    OnChecked = _ => ChatroomController.ToggleNotifyRoom(roomName, true, activity)
                });

                config.Rows.Add(new AnchoredMenuRow
                {
                    IconResId = Resource.Drawable.search_users_files,
                    Label = ctx.GetString(Resource.String.search_room),
                    OnClick = () =>
                    {
                        SearchTabHelper.SearchTarget = SearchTarget.Room;
                        SearchTabHelper.SearchTargetChosenRoom = roomName;
                        Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(MainActivity.IntentSearchRoomExtra, 1);
                        fragment.StartActivity(intent);
                    }
                });

                if (joined && (isOperator || isOwnedByUs))
                {
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.user_add,
                        Label = ctx.GetString(Resource.String.inviteuser),
                        OnClick = () => fragment.ShowInviteUserDialog(roomName)
                    });
                }

                if (joined && isOwnedByUs)
                {
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.logout_material,
                        Label = ctx.GetString(Resource.String.give_up_room),
                        Destructive = true,
                        OnClick = () => ChatroomController.DropMembershipOrOwnershipApi(roomName, true, true)
                    });
                }

                if (joined && isPrivate && !isOwnedByUs)
                {
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.logout_material,
                        Label = ctx.GetString(Resource.String.give_up_membership),
                        Destructive = true,
                        OnClick = () => ChatroomController.DropMembershipOrOwnershipApi(roomName, false, true)
                    });
                }

                if (joined)
                {
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.settings_outline_30dp,
                        Kind = AnchoredMenuRowKind.Submenu,
                        Label = ctx.GetString(Resource.String.Options),
                        SubMenuTitle = ctx.GetString(Resource.String.Options),
                        SubMenu = BuildStyleSubMenu(ctx),
                    });
                }

                return config;
            }

            private AnchoredMenuConfig BuildStyleSubMenu(Context ctx)
            {
                var sub = new AnchoredMenuConfig();
                sub.Rows.Add(new AnchoredMenuRow
                {
                    IconResId = Resource.Drawable.pinboard_all_tickers_30dp,
                    Label = ChatroomActivity.ShowTickerView
                        ? ctx.GetString(Resource.String.HideTickerView)
                        : ctx.GetString(Resource.String.ShowTickerView),
                    OnClick = () =>
                    {
                        ChatroomActivity.ShowTickerView = !ChatroomActivity.ShowTickerView;
                        fragment.SetTickerVisibility();
                        PreferencesManager.SaveShowTickerView();
                    }
                });
                sub.Rows.Add(new AnchoredMenuRow
                {
                    IconResId = Resource.Drawable.person_text_user_status_view_30dp,
                    Label = ChatroomActivity.ShowStatusesView
                        ? ctx.GetString(Resource.String.HideStatusView)
                        : ctx.GetString(Resource.String.ShowStatusView),
                    OnClick = () =>
                    {
                        ChatroomActivity.ShowStatusesView = !ChatroomActivity.ShowStatusesView;
                        fragment.SetActivityStatusesView();
                        PreferencesManager.SaveShowStatusesView();
                    }
                });
                return sub;
            }
        }
    }
}