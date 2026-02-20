using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Chatroom
{
    public class RoomUserListRecyclerAdapter : RecyclerView.Adapter, PopupMenu.IOnMenuItemClickListener
    {
        private List<Soulseek.UserData> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;

        public int GetPositionForUserData(Soulseek.UserData userData)
        {
            string uname = userData.Username;
            for (int i = 0; i < localDataSet.Count; i++)
            {
                if (uname == localDataSet[i].Username)
                {
                    return i;
                }
            }
            return -1;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as RoomUserItemViewHolder).userInnerView.setItem(localDataSet[position]);
            //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
        }

        public void setPosition(int position)
        {
            if (position == -1)
            {

            }
            this.position = position;
        }

        public Soulseek.UserData getDataAtPosition()
        {
            return this.localDataSet[this.position];
        }

        public int getPosition()
        {
            return this.position;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            RoomUserItemView view = RoomUserItemView.inflate(parent);
            view.setupChildren();
            // .inflate(R.layout.text_row_item, viewGroup, false);
            (view as View).Click += RoomUserListRecyclerAdapter_Click;
            (view as View).LongClick += RoomUserListRecyclerAdapter_LongClick;
            return new RoomUserItemViewHolder(view as View);


        }

        private void RoomUserListRecyclerAdapter_Click(object sender, EventArgs e)
        {
            RoomUserListDialog.longClickedUserData = (sender as RoomUserItemView).DataItem;

            (sender as View).ShowContextMenu();
        }

        private void RoomUserListRecyclerAdapter_LongClick(object sender, View.LongClickEventArgs e)
        {

            RoomUserListDialog.longClickedUserData = (sender as RoomUserItemView).DataItem;

            (sender as View).ShowContextMenu();
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            throw new NotImplementedException();
        }

        public RoomUserListRecyclerAdapter(List<Soulseek.UserData> ti)
        {
            localDataSet = ti;
        }

    }

    public class RoomUserItemViewHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
    {
        public RoomUserItemView userInnerView;


        public RoomUserItemViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            userInnerView = (RoomUserItemView)view;
            userInnerView.ViewHolder = this;
            (view as View).SetOnCreateContextMenuListener(this);
        }

        public RoomUserItemView getUnderlyingView()
        {
            return userInnerView;
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            RoomUserItemView roomUserItemView = v as RoomUserItemView;
            //private room specific options
            bool canRemoveModPriviledgesAndApplicable = false;
            bool canAddModPriviledgesAndApplicable = false;
            bool canRemoveUser = false;
            bool isPrivate = ChatroomController.IsPrivate(RoomUserListDialog.OurRoomName);
            if (isPrivate && roomUserItemView.DataItem is Soulseek.ChatroomUserData cData) //that means we are in a private room
            {
                if (ChatroomController.AreWeOwner(RoomUserListDialog.OurRoomName))
                {
                    if (cData.ChatroomUserRole == Soulseek.UserRole.Operator)
                    {
                        canRemoveModPriviledgesAndApplicable = true;
                    }
                    else
                    {
                        canAddModPriviledgesAndApplicable = true; //i.e. if the other user is non operator
                    }
                    canRemoveUser = true;
                }
                else if (ChatroomController.AreWeMod(RoomUserListDialog.OurRoomName))
                {
                    //we do not have any priviledges regarding fellow mods
                    if (cData.ChatroomUserRole == Soulseek.UserRole.Normal)
                    {
                        canRemoveUser = true;
                    }
                }
            }

            //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)menuInfo;


            if (canRemoveUser)
            {
                menu.Add(0, 0, 0, SeekerState.ActiveActivityRef.GetString(Resource.String.remove_user));
            }
            if (canRemoveModPriviledgesAndApplicable)
            {
                menu.Add(0, 1, 1, SeekerState.ActiveActivityRef.GetString(Resource.String.remove_mod_priv));
            }
            if (canAddModPriviledgesAndApplicable)
            {
                menu.Add(0, 2, 2, SeekerState.ActiveActivityRef.GetString(Resource.String.add_mod_priv));
            }


            //normal - add to user list, browse, etc...
            menu.Add(1, 3, 3, SeekerState.ActiveActivityRef.GetString(Resource.String.browse_user));
            menu.Add(1, 4, 4, SeekerState.ActiveActivityRef.GetString(Resource.String.search_user_files));
            CommonHelpers.AddAddRemoveUserMenuItem(menu, 1, 5, 5, roomUserItemView.DataItem.Username, true);
            CommonHelpers.AddIgnoreUnignoreUserMenuItem(menu, 1, 6, 6, roomUserItemView.DataItem.Username);
            menu.Add(1, 7, 7, SeekerState.ActiveActivityRef.GetString(Resource.String.msg_user));
            CommonHelpers.AddUserNoteMenuItem(menu, 1, 8, 8, roomUserItemView.DataItem.Username);

            var hackFixDialogContext = new FixForDialogFragmentContext();
            for (int i = 0; i < menu.Size(); i++)
            {
                menu.GetItem(i).SetOnMenuItemClickListener(hackFixDialogContext);
            }

        }

        public class FixForDialogFragmentContext : Java.Lang.Object, IMenuItemOnMenuItemClickListener
        {
            public bool OnMenuItemClick(Android.Views.IMenuItem item)
            {
                return RoomUserListDialog.forContextHelp.OnContextItemSelected(item);
            }
        }
    }

    public class RoomUserItemView : LinearLayout
    {
        public RoomUserItemViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewNumFiles;
        private TextView viewSpeed;
        private TextView viewOperatorStatus;
        private TextView viewFlag;
        private ImageView imageFriendIgnored;
        private ImageView imageNoted;
        private ImageView imageUserStatus;
        public Soulseek.UserData DataItem;

        public RoomUserItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.room_user_list_item, this, true);
            setupChildren();
        }
        public RoomUserItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.room_user_list_item, this, true);
            setupChildren();
        }
        public static RoomUserItemView inflate(ViewGroup parent)
        {
            RoomUserItemView itemView = (RoomUserItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.room_user_list_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.username);
            viewNumFiles = FindViewById<TextView>(Resource.Id.numFiles);
            viewSpeed = FindViewById<TextView>(Resource.Id.speed);
            viewFlag = FindViewById<TextView>(Resource.Id.flag);
            viewOperatorStatus = FindViewById<TextView>(Resource.Id.operatorStatus);
            imageFriendIgnored = FindViewById<ImageView>(Resource.Id.friend_ignored_image);
            imageNoted = FindViewById<ImageView>(Resource.Id.noted_image);
            imageUserStatus = FindViewById<ImageView>(Resource.Id.userStatus);
        }

        public void setItem(Soulseek.UserData userData)
        {
            DataItem = userData;
            viewFlag.Text = ChatroomActivity.LocaleToEmoji(userData.CountryCode.ToUpper());
            viewUsername.Text = userData.Username;
            viewNumFiles.Text = userData.FileCount.ToString("N0");
            viewSpeed.Text = (userData.AverageSpeed / 1024).ToString("N0") + " " + SimpleHelpers.STRINGS_KBS;
            if (userData is Soulseek.ChatroomUserData cData)
            {
                if (cData.ChatroomUserRole == Soulseek.UserRole.Normal)
                {
                    viewOperatorStatus.Visibility = ViewStates.Gone;
                }
                else if (cData.ChatroomUserRole == Soulseek.UserRole.Operator)
                {
                    viewOperatorStatus.Visibility = ViewStates.Visible;
                    viewOperatorStatus.Text = string.Format("({0})", SeekerState.ActiveActivityRef.GetString(Resource.String.mod).ToUpper());
                }
                else
                {
                    viewOperatorStatus.Visibility = ViewStates.Visible;
                    viewOperatorStatus.Text = string.Format("({0})", SeekerState.ActiveActivityRef.GetString(Resource.String.owner).ToUpper());
                }
            }
            else
            {
                viewOperatorStatus.Visibility = ViewStates.Gone;
            }
            if (SeekerState.UserNotes.ContainsKey(userData.Username))
            {
                imageNoted.Visibility = ViewStates.Visible;
            }
            else
            {
                imageNoted.Visibility = ViewStates.Invisible;
            }
            if (SeekerApplication.IsUserInIgnoreList(userData.Username))
            {
                imageFriendIgnored.SetImageResource(Resource.Drawable.account_cancel);
                imageFriendIgnored.Visibility = ViewStates.Visible;
            }
            else if (UserListService.Instance.ContainsUser(userData.Username))
            {
                imageFriendIgnored.SetImageResource(Resource.Drawable.account_star);
                imageFriendIgnored.Visibility = ViewStates.Visible;
            }
            else
            {
                imageFriendIgnored.Visibility = ViewStates.Invisible;
            }
            switch (userData.Status)
            {
                case Soulseek.UserPresence.Online:
                    imageUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.online));
                    break;
                case Soulseek.UserPresence.Away:
                    imageUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.away));
                    break;
                case Soulseek.UserPresence.Offline: //should NEVER happen
                    imageUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.offline));
                    break;
            }
        }
    }


}