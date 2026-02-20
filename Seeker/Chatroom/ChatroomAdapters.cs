using Seeker.Chatroom;
using Seeker.Helpers;
using Seeker.Messages;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using Common.Messages;

namespace Seeker
{
    public class UserStatusView : LinearLayout
    {
        public RecyclerView.ViewHolder ViewHolder { get; set; }
        private TextView viewUserStatus;

        public UserStatusView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.user_status_update_item, this, true);
            setupChildren();
        }
        public UserStatusView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.user_status_update_item, this, true);
            setupChildren();
        }

        public static UserStatusView inflate(ViewGroup parent)
        {
            UserStatusView itemView = (UserStatusView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.user_status_update_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUserStatus = FindViewById<TextView>(Resource.Id.userStatusMessage);
        }

        private void SetMessageText(TextView userStatus, StatusMessageUpdate data)
        {
            string statusMessage = null;
            DateTime dateTimeLocal = data.DateTimeUtc.Add(SeekerState.OffsetFromUtcCached);
            string timePrefix = $"[{CommonHelpers.GetNiceDateTimeGroupChat(dateTimeLocal)}]";
            switch (data.StatusType)
            {
                case StatusMessageType.Joined:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserJoined);
                    break;
                case StatusMessageType.Left:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserLeft);
                    break;
                case StatusMessageType.WentAway:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserWentAway);
                    break;
                case StatusMessageType.CameBack:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserCameBack);
                    break;
            }
            userStatus.Text = string.Format(statusMessage, timePrefix, data.Username);
        }

        public void setItem(StatusMessageUpdate userStatusMessage)
        {
            SetMessageText(viewUserStatus, userStatusMessage);
        }
    }


    public class UserStatusHolder : RecyclerView.ViewHolder
    {
        public UserStatusView userStatusInnerView;

        public UserStatusHolder(View view) : base(view)
        {
            userStatusInnerView = (UserStatusView)view;
            userStatusInnerView.ViewHolder = this;
        }

        public UserStatusView getUnderlyingView()
        {
            return userStatusInnerView;
        }
    }


    public class ChatroomStatusesRecyclerAdapter : RecyclerView.Adapter
    {
        private List<StatusMessageUpdate> localDataSet;
        public override int ItemCount => localDataSet.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as UserStatusHolder).getUnderlyingView().setItem(localDataSet[position]);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            UserStatusView view = UserStatusView.inflate(parent);
            view.setupChildren();
            return new UserStatusHolder(view as View);
        }

        public ChatroomStatusesRecyclerAdapter(List<StatusMessageUpdate> ti)
        {
            localDataSet = ti;
        }
    }


    public class ChatroomInnerRecyclerAdapter : RecyclerView.Adapter
    {
        private List<Message> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public static int VIEW_SENT = 1;
        public static int VIEW_RECEIVER = 2;
        public static int VIEW_STATUS = 3;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (localDataSet[position].FromMe)
            {
                (holder as MessageInnerViewSentHolder).messageInnerView.setItem(localDataSet[position]);
            }
            else if (localDataSet[position].SpecialCode != 0)
            {
                (holder as MessageConnectionStatusHolder).messageInnerView.setItem(localDataSet[position]);
            }
            else
            {
                (holder as GroupMessageInnerViewReceivedHolder).messageInnerView.setItem(localDataSet[position]);
            }
        }

        public void setPosition(int position)
        {
            this.position = position;
        }

        public int getPosition()
        {
            return this.position;
        }

        public override int GetItemViewType(int position)
        {
            if (localDataSet[position].FromMe)
            {
                return VIEW_SENT;
            }
            else if (localDataSet[position].SpecialCode != 0)
            {
                return VIEW_STATUS;
            }
            else
            {
                return VIEW_RECEIVER;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == VIEW_SENT)
            {
                MessageInnerViewSent view = MessageInnerViewSent.inflate(parent);
                view.setupChildren();
                (view as View).LongClick += ChatroomReceivedAdapter_LongClick;
                return new MessageInnerViewSentHolder(view as View);
            }
            else if (viewType == VIEW_RECEIVER)
            {
                GroupMessageInnerViewReceived view = GroupMessageInnerViewReceived.inflate(parent);
                view.setupChildren();
                (view as View).LongClick += ChatroomReceivedAdapter_LongClick;
                return new GroupMessageInnerViewReceivedHolder(view as View);
            }
            else
            {
                MessageConnectionStatus view = MessageConnectionStatus.inflate(parent);
                view.setupChildren();
                return new MessageConnectionStatusHolder(view as View);
            }
        }

        private void ChatroomReceivedAdapter_LongClick(object sender, View.LongClickEventArgs e)
        {
            if (sender is GroupMessageInnerViewReceived recv)
            {
                ChatroomInnerFragment.MessagesLongClickData = recv.DataItem;
            }
            else if (sender is MessageInnerViewSent sent)
            {
                ChatroomInnerFragment.MessagesLongClickData = sent.DataItem;
            }

            (sender as View).ShowContextMenu();
        }

        public ChatroomInnerRecyclerAdapter(List<Message> ti)
        {
            localDataSet = ti;
        }
    }


    public class GroupMessageInnerViewReceivedHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
    {
        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            Logger.Debug("ShowSlskLinkContextMenu " + SimpleHelpers.ShowSlskLinkContextMenu);

            if (menu.FindItem(SlskLinkMenuActivity.FromSlskLinkBrowseAtLocation) != null)
            {
                return;
            }
            else if (SimpleHelpers.ShowSlskLinkContextMenu)
            {
                SimpleHelpers.ShowSlskLinkContextMenu = false;
                return;
            }

            if (v is GroupMessageInnerViewReceived)
            {
                ChatroomInnerFragment.MessagesLongClickData = (v as GroupMessageInnerViewReceived).DataItem;
            }
            else
            {
                Logger.Firebase("sender for GroupMessageInnerViewReceivedHolder.GroupMessageInnerViewReceived is " + v.GetType().Name);
            }

            menu.Add(0, 0, 0, SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.copy_text));
            menu.Add(1, 1, 1, SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.ignore_user));
            CommonHelpers.AddAddRemoveUserMenuItem(menu, 2, 2, 2, ChatroomInnerFragment.MessagesLongClickData.Username);
            var subMenu = menu.AddSubMenu(3, 3, 3, SeekerState.ActiveActivityRef.GetString(Resource.String.more_options));
            subMenu.Add(4, 4, 4, Resource.String.search_user_files);
            subMenu.Add(5, 5, 5, Resource.String.browse_user);
            subMenu.Add(6, 6, 6, Resource.String.get_user_info);
            subMenu.Add(7, 7, 7, Resource.String.msg_user);
            CommonHelpers.AddUserNoteMenuItem(subMenu, 8, 8, 8, ChatroomInnerFragment.MessagesLongClickData.Username);
        }

        public GroupMessageInnerViewReceived messageInnerView;

        public GroupMessageInnerViewReceivedHolder(View view) : base(view)
        {
            messageInnerView = (GroupMessageInnerViewReceived)view;
            messageInnerView.ViewHolder = this;
            view.SetOnCreateContextMenuListener(this);
        }

        public GroupMessageInnerViewReceived getUnderlyingView()
        {
            return messageInnerView;
        }
    }

    public class GroupMessageInnerViewReceived : ConstraintLayout
    {
        public GroupMessageInnerViewReceivedHolder ViewHolder { get; set; }
        private TextView viewTimeStamp;
        private TextView viewMessage;
        private TextView viewUsername;
        public Message DataItem;

        public GroupMessageInnerViewReceived(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.group_messages_inner_item_toMe, this, true);
            setupChildren();
        }
        public GroupMessageInnerViewReceived(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.group_messages_inner_item_toMe, this, true);
            setupChildren();
        }
        public static GroupMessageInnerViewReceived inflate(ViewGroup parent)
        {
            GroupMessageInnerViewReceived itemView = (GroupMessageInnerViewReceived)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.group_messages_inner_item_toMe_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewTimeStamp = FindViewById<TextView>(Resource.Id.text_gchat_timestamp_other);
            viewMessage = FindViewById<TextView>(Resource.Id.text_gchat_message_other);
            viewUsername = FindViewById<TextView>(Resource.Id.text_gchat_user_other);
        }

        public void setItem(Message msg)
        {
            DataItem = msg;
            viewTimeStamp.Text = CommonHelpers.GetNiceDateTimeGroupChat(msg.LocalDateTime);
            CommonHelpers.SetMessageTextView(viewMessage, msg);
            if (msg.SameAsLastUser)
            {
                viewUsername.Visibility = ViewStates.Gone;
                viewUsername.Text = string.Empty;
            }
            else
            {
                viewUsername.Visibility = ViewStates.Visible;
                viewUsername.Text = msg.Username;
            }
        }
    }

    public class RecyclerView2 : RecyclerView
    {
        public RecyclerView2(Context context) : base(context)
        {
        }

        public RecyclerView2(Context context, IAttributeSet attrSet) : base(context, attrSet)
        {
        }

        public RecyclerView2(Context context, IAttributeSet attrSet, int defStyle) : base(context, attrSet, defStyle)
        {
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            if (oldh > h)
            {
                Logger.Debug("size changed to smaller...");
                this.GetLayoutManager().ScrollToPosition(this.GetLayoutManager().ItemCount - 1);
            }
        }
    }



    public class ChatroomOverviewRecyclerAdapter : RecyclerView.Adapter
    {
        private List<Soulseek.RoomInfo> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public static int VIEW_CATEGORY_HEADER = 1;
        public static int VIEW_ROOM = 2;
        public static int VIEW_JOINED_ROOM = 3;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (localDataSet[position] is RoomInfoCategory)
            {
                (holder as ChatroomOverviewCategoryHolder).chatroomOverviewView.setItem(localDataSet[position]);
            }
            else
            {
                if (holder is ChatroomOverviewHolder chatOverview)
                {
                    chatOverview.chatroomOverviewView.setItem(localDataSet[position]);
                }
                else if (holder is ChatroomOverviewJoinedViewHolder chatJoinedViewHolder)
                {
                    chatJoinedViewHolder.chatroomOverviewView.setItem(localDataSet[position]);
                }
            }
        }

        public void notifyRoomStatusChanged(string roomName)
        {
            for (int i = 0; i < localDataSet.Count; i++)
            {
                if (localDataSet[i].Name == roomName)
                {
                    this.NotifyItemChanged(i);
                    Logger.Debug("NotifyItemChanged notifyRoomStatusChanged");
                    break;
                }
            }
        }

        public void notifyRoomStatusesChanged(List<string> rooms)
        {
            foreach (string roomName in rooms)
            {
                notifyRoomStatusChanged(roomName);
            }
        }

        public void setPosition(int position)
        {
            this.position = position;
        }

        public int getPosition()
        {
            return this.position;
        }

        private void ChatroomOverviewClick(object sender, EventArgs e)
        {
            setPosition((sender as IChatroomOverviewBase).ViewHolder.AdapterPosition);
            ChatroomActivity.ChatroomActivityRef.ChangeToInnerFragment(localDataSet[position]);
        }

        public override int GetItemViewType(int position)
        {
            if (localDataSet[position] is RoomInfoCategory)
            {
                return VIEW_CATEGORY_HEADER;
            }
            else if (ChatroomController.JoinedRoomNames.Contains(localDataSet[position].Name))
            {
                return VIEW_JOINED_ROOM;
            }
            else
            {
                return VIEW_ROOM;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == VIEW_CATEGORY_HEADER)
            {
                ChatroomOverviewCategoryView view = ChatroomOverviewCategoryView.inflate(parent);
                view.setupChildren();
                return new ChatroomOverviewCategoryHolder(view as View);
            }
            else if (viewType == VIEW_JOINED_ROOM)
            {
                ChatroomOverviewJoinedView view = ChatroomOverviewJoinedView.inflate(parent);
                view.setupChildren();
                (view as View).Click += ChatroomOverviewClick;
                view.FindViewById<ImageView>(Resource.Id.leaveRoom).Click += ChatroomOverviewRecyclerAdapter_Click;
                return new ChatroomOverviewJoinedViewHolder(view as View);
            }
            else
            {
                ChatroomOverviewView view = ChatroomOverviewView.inflate(parent);
                view.setupChildren();
                (view as View).Click += ChatroomOverviewClick;
                return new ChatroomOverviewHolder(view as View);
            }
        }

        private void ChatroomOverviewRecyclerAdapter_Click(object sender, EventArgs e)
        {
            setPosition(((sender as View).Parent.Parent as IChatroomOverviewBase).ViewHolder.AdapterPosition);
            string roomName = localDataSet[position].Name;
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                ChatroomController.RemoveRoomFromJoinedAndOthers(roomName);
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.leaving_room), roomName), ToastLength.Short).Show();
                });
                ChatroomController.UpdateJoinedRooms();
            }
            else
            {
                ChatroomController.JoinRoomApi(roomName, false, true, true, false);
            }
        }

        public ChatroomOverviewRecyclerAdapter(List<Soulseek.RoomInfo> ti)
        {
            localDataSet = ti;
        }
    }

    public class MessageConnectionStatusHolder : RecyclerView.ViewHolder
    {
        public MessageConnectionStatus messageInnerView;

        public MessageConnectionStatusHolder(View view) : base(view)
        {
            messageInnerView = (MessageConnectionStatus)view;
            messageInnerView.ViewHolder = this;
        }

        public MessageConnectionStatus getUnderlyingView()
        {
            return messageInnerView;
        }
    }

    public class MessageConnectionStatus : LinearLayout
    {
        public MessageConnectionStatusHolder ViewHolder { get; set; }
        private TextView viewStatus;

        public MessageConnectionStatus(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_connect_disconnect_item, this, true);
            setupChildren();
        }
        public MessageConnectionStatus(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_connect_disconnect_item, this, true);
            setupChildren();
        }

        public static MessageConnectionStatus inflate(ViewGroup parent)
        {
            MessageConnectionStatus itemView = (MessageConnectionStatus)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_connect_disconnect_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewStatus = FindViewById<TextView>(Resource.Id.statusMsg);
        }

        public void setItem(Message msg)
        {
            viewStatus.Text = msg.MessageText;
        }
    }


    public class ChatroomOverviewHolder : RecyclerView.ViewHolder
    {
        public ChatroomOverviewView chatroomOverviewView;

        public ChatroomOverviewHolder(View view) : base(view)
        {
            chatroomOverviewView = (ChatroomOverviewView)view;
            chatroomOverviewView.ViewHolder = this;
        }

        public ChatroomOverviewView getUnderlyingView()
        {
            return chatroomOverviewView;
        }
    }


    public class ChatroomOverviewCategoryHolder : RecyclerView.ViewHolder
    {
        public ChatroomOverviewCategoryView chatroomOverviewView;

        public ChatroomOverviewCategoryHolder(View view) : base(view)
        {
            chatroomOverviewView = (ChatroomOverviewCategoryView)view;
            chatroomOverviewView.ViewHolder = this;
        }

        public ChatroomOverviewCategoryView getUnderlyingView()
        {
            return chatroomOverviewView;
        }
    }

    public interface IChatroomOverviewBase
    {
        public void setItem(Soulseek.RoomInfo roomInfo);
        public RecyclerView.ViewHolder ViewHolder { get; set; }
    }

    public class ChatroomOverviewView : LinearLayout, IChatroomOverviewBase
    {
        public RecyclerView.ViewHolder ViewHolder { get; set; }
        private TextView viewRoomName;
        private TextView viewUsersInRoom;

        public ChatroomOverviewView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_item, this, true);
            setupChildren();
        }
        public ChatroomOverviewView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_item, this, true);
            setupChildren();
        }

        public static ChatroomOverviewView inflate(ViewGroup parent)
        {
            ChatroomOverviewView itemView = (ChatroomOverviewView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_overview_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewRoomName = FindViewById<TextView>(Resource.Id.roomName);
            viewUsersInRoom = FindViewById<TextView>(Resource.Id.usersInRoom);
        }

        public void setItem(Soulseek.RoomInfo roomInfo)
        {
            viewRoomName.Text = roomInfo.Name;
            viewUsersInRoom.Text = roomInfo.UserCount.ToString();
        }
    }

    public class ChatroomOverviewJoinedViewHolder : RecyclerView.ViewHolder
    {
        public ChatroomOverviewJoinedView chatroomOverviewView;

        public ChatroomOverviewJoinedViewHolder(View view) : base(view)
        {
            chatroomOverviewView = (ChatroomOverviewJoinedView)view;
            chatroomOverviewView.ViewHolder = this;
        }

        public ChatroomOverviewJoinedView getUnderlyingView()
        {
            return chatroomOverviewView;
        }
    }

    public class ChatroomOverviewJoinedView : LinearLayout, IChatroomOverviewBase
    {
        public RecyclerView.ViewHolder ViewHolder { get; set; }
        private TextView viewRoomName;
        private TextView viewUsersInRoom;
        private ImageView unreadImageView;

        public ChatroomOverviewJoinedView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_joined_item, this, true);
            setupChildren();
        }
        public ChatroomOverviewJoinedView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_joined_item, this, true);
            setupChildren();
        }

        public static ChatroomOverviewJoinedView inflate(ViewGroup parent)
        {
            ChatroomOverviewJoinedView itemView = (ChatroomOverviewJoinedView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_overview_joined_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewRoomName = FindViewById<TextView>(Resource.Id.roomName);
            viewUsersInRoom = FindViewById<TextView>(Resource.Id.usersInRoom);
            unreadImageView = FindViewById<ImageView>(Resource.Id.unreadImageView);
        }

        public void setItem(Soulseek.RoomInfo roomInfo)
        {
            viewRoomName.Text = roomInfo.Name;
            viewUsersInRoom.Text = roomInfo.UserCount.ToString();

            if (ChatroomController.UnreadRooms.ContainsKey(roomInfo.Name))
            {
                unreadImageView.Visibility = ViewStates.Visible;

                viewRoomName.SetTypeface(null, TypefaceStyle.Bold);
                viewUsersInRoom.SetTypeface(null, TypefaceStyle.Bold);
                viewRoomName.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.normalTextColorNonTinted));
                viewUsersInRoom.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.normalTextColorNonTinted));
            }
            else
            {
                unreadImageView.Visibility = ViewStates.Gone;

                viewRoomName.SetTypeface(null, TypefaceStyle.Normal);
                viewUsersInRoom.SetTypeface(null, TypefaceStyle.Normal);
                viewRoomName.SetTextColor(SeekerState.ActiveActivityRef.Resources.GetColor(Resource.Color.defaultTextColor));
                viewUsersInRoom.SetTextColor(SeekerState.ActiveActivityRef.Resources.GetColor(Resource.Color.defaultTextColor));
            }

            if (ChatroomController.CurrentlyJoinedRoomNames.ContainsKey(roomInfo.Name))
            {
                unreadImageView.Alpha = 1.0f;
                viewRoomName.Alpha = 1.0f;
                viewUsersInRoom.Alpha = 1.0f;
            }
            else
            {
                unreadImageView.Alpha = 0.4f;
                viewRoomName.Alpha = 0.4f;
                viewUsersInRoom.Alpha = 0.4f;
            }
        }
    }


    public class ChatroomOverviewCategoryView : LinearLayout
    {
        public ChatroomOverviewCategoryHolder ViewHolder { get; set; }
        private TextView viewCategory;

        public ChatroomOverviewCategoryView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_category_item, this, true);
            setupChildren();
        }
        public ChatroomOverviewCategoryView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_category_item, this, true);
            setupChildren();
        }

        public static ChatroomOverviewCategoryView inflate(ViewGroup parent)
        {
            ChatroomOverviewCategoryView itemView = (ChatroomOverviewCategoryView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_overview_category_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewCategory = FindViewById<TextView>(Resource.Id.categoryHeader);
        }

        public void setItem(UserListItem usertype)
        {
            viewCategory.Text = usertype.Username;
        }

        public void setItem(Soulseek.RoomInfo roomInfo)
        {
            viewCategory.Text = roomInfo.Name;
        }
    }
}
