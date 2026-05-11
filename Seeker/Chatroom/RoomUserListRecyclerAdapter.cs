using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Common.Messages;
using Seeker.Helpers.ActionSheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Chatroom
{
    public class RoomUserListRecyclerAdapter : RecyclerView.Adapter
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
            ShowUserActionSheet((sender as RoomUserItemView).DataItem);
        }

        private void RoomUserListRecyclerAdapter_LongClick(object sender, View.LongClickEventArgs e)
        {
            ShowUserActionSheet((sender as RoomUserItemView).DataItem);
            e.Handled = true;
        }

        private void ShowUserActionSheet(Soulseek.UserData userdata)
        {
            var dialog = RoomUserListDialog.forContextHelp;
            var activity = SeekerState.ActiveActivityRef;
            var snackView = dialog?.View?.FindViewById<ViewGroup>(Resource.Id.userListRoom);

            var options = new UserActionsOptions
            {
                OnAddRemoved = dialog?.GetUpdateUserListRoomActionAddedRemoved(userdata),
                OnIgnoreChanged = dialog?.GetUpdateUserListRoomAction(userdata),
                OnNoteChanged = dialog?.GetUpdateUserListRoomAction(userdata),
                RoomAdmin = BuildRoomAdminContext(userdata, dialog)
            };

            var config = new ActionSheetConfig();
            config.Sections.Add(ActionSheetActions.BuildUserActionsSection(userdata.Username, activity, snackView, options));

            ActionSheetDialog.PendingConfig = config;
            new ActionSheetDialog().Show(activity.SupportFragmentManager, "actionSheet");
        }

        private static RoomAdminContext BuildRoomAdminContext(Soulseek.UserData userdata, RoomUserListDialog dialog)
        {
            string roomName = RoomUserListDialog.OurRoomName;
            bool isPrivate = ChatroomController.IsPrivate(roomName);
            if (!isPrivate || !(userdata is ChatroomUserData cData))
            {
                return null;
            }
            bool canRemoveUser = false;
            bool canAddMod = false;
            bool canRemoveMod = false;
            if (ChatroomController.AreWeOwner(roomName))
            {
                canRemoveUser = true;
                if (cData.ChatroomUserRole == Soulseek.UserRole.Operator)
                {
                    canRemoveMod = true;
                }
                else
                {
                    canAddMod = true;
                }
            }
            else if (ChatroomController.AreWeMod(roomName))
            {
                if (cData.ChatroomUserRole == Soulseek.UserRole.Normal)
                {
                    canRemoveUser = true;
                }
            }
            if (!canRemoveUser && !canAddMod && !canRemoveMod)
            {
                return null;
            }
            return new RoomAdminContext
            {
                RoomName = roomName,
                CanRemoveUser = canRemoveUser,
                CanAddMod = canAddMod,
                CanRemoveMod = canRemoveMod,
                OnAdminChanged = dialog?.GetUpdateUserListRoomAction(userdata)
            };
        }

        public RoomUserListRecyclerAdapter(List<Soulseek.UserData> ti)
        {
            localDataSet = ti;
        }

    }

    public class RoomUserItemViewHolder : RecyclerView.ViewHolder
    {
        public RoomUserItemView userInnerView;

        public RoomUserItemViewHolder(View view) : base(view)
        {
            userInnerView = (RoomUserItemView)view;
            userInnerView.ViewHolder = this;
        }

        public RoomUserItemView getUnderlyingView()
        {
            return userInnerView;
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
            if (userData is ChatroomUserData cData)
            {
                if (cData.ChatroomUserRole == Soulseek.UserRole.Normal)
                {
                    viewOperatorStatus.Visibility = ViewStates.Gone;
                }
                else if (cData.ChatroomUserRole == Soulseek.UserRole.Operator)
                {
                    viewOperatorStatus.Visibility = ViewStates.Visible;
                    viewOperatorStatus.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.mod).ToUpper();
                }
                else
                {
                    viewOperatorStatus.Visibility = ViewStates.Visible;
                    viewOperatorStatus.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.owner).ToUpper();
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
                    imageUserStatus.SetColorFilter(new Android.Graphics.Color(ContextCompat.GetColor(SeekerState.ActiveActivityRef, Resource.Color.online)));
                    break;
                case Soulseek.UserPresence.Away:
                    imageUserStatus.SetColorFilter(new Android.Graphics.Color(ContextCompat.GetColor(SeekerState.ActiveActivityRef, Resource.Color.away)));
                    break;
                case Soulseek.UserPresence.Offline: //should NEVER happen
                    imageUserStatus.SetColorFilter(new Android.Graphics.Color(ContextCompat.GetColor(SeekerState.ActiveActivityRef, Resource.Color.offline)));
                    break;
            }
        }
    }


}