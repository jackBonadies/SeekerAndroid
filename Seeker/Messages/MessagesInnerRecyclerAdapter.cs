using Seeker.Chatroom;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seeker.Helpers;

namespace Seeker.Messages
{
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

        public override int GetItemViewType(int position)
        {
            if (localDataSet[position].FromMe)
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
            if (viewType == VIEW_SENT)
            {
                MessageInnerViewSent view = MessageInnerViewSent.inflate(parent);
                view.setupChildren();
                view.LongClick += View_LongClick;
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += MessageOverviewClick;
                return new MessageInnerViewSentHolder(view as View);
            }
            else
            {
                MessageInnerViewReceived view = MessageInnerViewReceived.inflate(parent);
                view.setupChildren();
                view.LongClick += View_LongClick;
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += MessageOverviewClick;
                return new MessageInnerViewReceivedHolder(view as View);
            }

        }

        private void View_LongClick(object sender, View.LongClickEventArgs e)
        {
            View anchor = sender as View;
            Message msg = null;
            GravityFlags gravity = GravityFlags.Start;
            if (sender is MessageInnerViewSent msgSent)
            {
                msg = msgSent.DataItem;
                gravity = GravityFlags.End;
            }
            else if (sender is MessageInnerViewReceived msgRecv)
            {
                msg = msgRecv.DataItem;
                gravity = GravityFlags.Start;
            }
            if (anchor == null || msg == null)
            {
                return;
            }

            UiHelpers.ShowCopyMessageTextPopup(anchor, msg, gravity);
            e.Handled = true;
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
            messageInnerView = (MessageInnerViewSent)view;
            messageInnerView.ViewHolder = this;
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
            messageInnerView = (MessageInnerViewReceived)view;
            messageInnerView.ViewHolder = this;
        }

        public MessageInnerViewReceived getUnderlyingView()
        {
            return messageInnerView;
        }
    }

    public class MessageInnerViewSent : LinearLayout
    {
        public MessageInnerViewSentHolder ViewHolder { get; set; }
        public bool IsGroupChat { get; set; }
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
            switch (status)
            {
                case SentStatus.Pending:
                case SentStatus.Success:
                    return UiHelpers.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainPurple);
                case SentStatus.Failed:
                    resourceIntColor = Resource.Color.hardErrorRed;
                    return GetColorFromInteger(ContextCompat.GetColor(SeekerState.ActiveActivityRef, resourceIntColor));
                case SentStatus.None:
                    throw new Exception("Sent status should not be none");
            }
            return Color.Red; //unreachable
        }

        public void setItem(Message msg)
        {
            DataItem = msg;
            cardView.CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(GetColorFromMsgStatus(msg.SentMsgStatus));
            if (msg.SentMsgStatus == SentStatus.Pending)
            {
                viewTimeStamp.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.pending_);
            }
            else if (msg.SentMsgStatus == SentStatus.Failed)
            {
                viewTimeStamp.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.failed);
            }
            else
            {
                viewTimeStamp.Text = IsGroupChat
                    ? CommonHelpers.GetNiceDateTimeGroupChat(msg.LocalDateTime)
                    : CommonHelpers.GetNiceDateTime(msg.LocalDateTime);
            }
            UiHelpers.SetMessageTextView(viewMessage, msg);
        }

        public Message DataItem;
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
            DataItem = msg;
            viewTimeStamp.Text = CommonHelpers.GetNiceDateTime(msg.LocalDateTime);
            UiHelpers.SetMessageTextView(viewMessage, msg);
        }
        public Message DataItem;
    }


}