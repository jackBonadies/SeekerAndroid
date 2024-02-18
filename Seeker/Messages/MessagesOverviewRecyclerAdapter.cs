using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Snackbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Messages
{
    public class ItemTouchHelperMessageOverviewCallback : ItemTouchHelper.SimpleCallback
    {
        //public static string DELETED_USERNAME = string.Empty;
        //public static int DELETED_POSITION = -1;
        //public static List<Message> DELETED_DATA = null;
        private MessagesOverviewRecyclerAdapter adapter = null;
        private AndroidX.Fragment.App.Fragment containingFragment = null;
        public ItemTouchHelperMessageOverviewCallback(MessagesOverviewRecyclerAdapter _adapter, AndroidX.Fragment.App.Fragment outerFrag) : base(0, ItemTouchHelper.Left) //no dragging. left swiping.
        {
            containingFragment = outerFrag;
            adapter = _adapter;
            iconDrawable = ContextCompat.GetDrawable(SeekerState.ActiveActivityRef, Resource.Drawable.baseline_delete_outline_white_24);
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
            Action<View> undoSnackBarAction = new Action<View>((View v) =>
            {
                if (MessagesActivity.DELETED_USERNAME == string.Empty || MessagesActivity.DELETED_DATA == null || MessagesActivity.DELETED_POSITION == -1)
                {
                    //error
                    bool isNull = MessagesActivity.DELETED_DATA == null;
                    MainActivity.LogFirebase("failure on undo uname:" + MessagesActivity.DELETED_USERNAME + " " + isNull + " " + MessagesActivity.DELETED_POSITION);
                    Toast.MakeText(v.Context, Resource.String.failed_to_undo, ToastLength.Short).Show();
                    return;
                }
                MessageController.Messages[MessagesActivity.DELETED_USERNAME] = MessagesActivity.DELETED_DATA;
                MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
                if (!fromOptionMenu)
                {
                    adapter.RestoreAt(MessagesActivity.DELETED_POSITION, MessagesActivity.DELETED_USERNAME);
                }
                else
                {
                    (SeekerState.ActiveActivityRef as MessagesActivity).GetOverviewFragment().RefreshAdapter();
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
            MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);

            Snackbar sb = Snackbar.Make(containingFragment.View, string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.deleted_message_history_with),
                MessagesActivity.DELETED_USERNAME), Snackbar.LengthLong)
                .SetAction(Resource.String.undo, GetSnackBarAction(this.adapter, false))
                .SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
            (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
            sb.Show();
        }

        public override void OnChildDraw(Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
        {
            base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
            View itemView = viewHolder.ItemView;
            MainActivity.LogDebug("dX" + dX);
            if (dX > 0)
            {
                this.colorDrawable.SetBounds(itemView.Left, itemView.Top, itemView.Left + (int)dX, itemView.Bottom);
            }
            else if (dX < 0)
            {
                this.colorDrawable.SetBounds(itemView.Right + (int)dX, itemView.Top, itemView.Right, itemView.Bottom);
                double margin = (itemView.Bottom - itemView.Top) * .15; //BOTTOM IS GREATER THAN TOP
                int clipBounds = (int)((itemView.Bottom - itemView.Top) - 2 * margin);
                int level = Math.Min((int)(Math.Abs((dX + margin) / (clipBounds)) * 10000), 10000);
                MainActivity.LogDebug("level" + level);
                if (level < 0)
                {
                    level = 0;
                }
                clipDrawable.SetLevel(level);
                //int dXicon = -300;
                clipDrawable.SetBounds((int)(itemView.Right - clipBounds - margin), (int)(itemView.Top + margin), (int)(itemView.Right - margin), (int)(itemView.Bottom - margin));
            }
            else
            {
                this.colorDrawable.SetBounds(0, 0, 0, 0);
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


        private void MessageOverviewClick(object sender, EventArgs e)
        {
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

        public void NotifyNameChanged(string name)
        {
            int pos = localDataSet.IndexOf(name);
            if (pos != -1)
            {
                this.NotifyItemChanged(pos);
            }
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
        private TextView viewDateTimeAgo;
        private ImageView unreadImageView;

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
            viewDateTimeAgo = FindViewById<TextView>(Resource.Id.dateTimeAgo);
            unreadImageView = FindViewById<ImageView>(Resource.Id.unreadImageView);
        }

        public void setItem(string username)
        {
            viewUsername.Text = username;
            Message m = MessageController.Messages[username].Last();

            viewDateTimeAgo.Text = CommonHelpers.GetDateTimeSinceAbbrev(m.LocalDateTime);

            if (MessageController.UnreadUsernames.ContainsKey(username))
            {
                unreadImageView.Visibility = ViewStates.Visible;
                viewUsername.SetTypeface(viewUsername.Typeface, TypefaceStyle.Bold);
                viewDateTimeAgo.SetTypeface(viewDateTimeAgo.Typeface, TypefaceStyle.Bold);
                viewMessage.SetTypeface(viewMessage.Typeface, TypefaceStyle.Bold);
                viewUsername.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.normalTextColorNonTinted));
                viewDateTimeAgo.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.normalTextColorNonTinted));
                viewMessage.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.normalTextColorNonTinted));
            }
            else
            {
                unreadImageView.Visibility = ViewStates.Gone;
                viewUsername.SetTypeface(viewUsername.Typeface, TypefaceStyle.Normal);
                viewDateTimeAgo.SetTypeface(viewDateTimeAgo.Typeface, TypefaceStyle.Normal);
                viewMessage.SetTypeface(viewMessage.Typeface, TypefaceStyle.Normal);
                viewUsername.SetTextColor(SeekerState.ActiveActivityRef.Resources.GetColor(Resource.Color.defaultTextColor));
                viewDateTimeAgo.SetTextColor(SeekerState.ActiveActivityRef.Resources.GetColor(Resource.Color.defaultTextColor));
                viewMessage.SetTextColor(SeekerState.ActiveActivityRef.Resources.GetColor(Resource.Color.defaultTextColor));
            }

            string msgText = m.MessageText;
            if (m.FromMe)
            {
                msgText = "\u21AA" + msgText;
            }
            viewMessage.Text = msgText;
            //viewMessage.SetTextColor()
            //viewMessage.SetTextColor(GetColorFromAttribute(_mContext, Resource.Attribute.normalTextColor))
        }
    }

}