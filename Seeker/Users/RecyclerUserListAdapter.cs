using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Graphics.Drawable;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seeker.Helpers;

namespace Seeker.Users
{
    public class RecyclerUserListAdapter : RecyclerView.Adapter
    {
        public List<UserListItem> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public static int VIEW_FRIEND = 0;
        public static int VIEW_IGNORED = 1;
        public static int VIEW_CATEGORY = 2;
        UserListActivity UserListActivity = null;
        public RecyclerUserListAdapter(UserListActivity activity, List<UserListItem> ti)
        {
            this.UserListActivity = activity;
            localDataSet = ti;
        }

        public void RemoveFromDataSet(int position)
        {
            localDataSet.RemoveAt(position);
        }

        public int GetPositionForUsername(string uname)
        {
            for (int i = 0; i < localDataSet.Count; i++)
            {
                if (uname == localDataSet[i].Username && localDataSet[i].Role != UserRole.Category)
                {
                    return i;
                }
            }
            return -1;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (localDataSet[position].Role == UserRole.Friend)
            {
                (holder as UserRowViewHolder).userRowView.setItem(localDataSet[position]);
            }
            else if (localDataSet[position].Role == UserRole.Ignored)
            {
                (holder as UserRowViewHolder).userRowView.setItem(localDataSet[position]);
            }
            else //category
            {
                (holder as ChatroomOverviewCategoryHolder).chatroomOverviewView.setItem(localDataSet[position]);
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
            return (int)(localDataSet[position].Role);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {
            if (viewType == (int)UserRole.Friend)
            {
                UserRowView view = UserRowView.inflate(parent);
                view.setupChildren();
                //view.viewMoreOptions.Click += this.UserListActivity.UserListItemMoreOptionsClick;
                view.viewUserStatus.Click += view.ViewUserStatus_Click;
                view.viewUserStatus.LongClick += view.ViewUserStatus_LongClick;
                view.UserListActivity = this.UserListActivity;
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).Click += view.UserRowView_Click;
                (view as View).LongClick += view.UserRowView_LongClick;
                return new UserRowViewHolder(view as View);
            }
            else if (viewType == (int)UserRole.Ignored)
            {
                UserRowView view = UserRowView.inflate(parent);
                view.setupChildren();
                //view.viewMoreOptions.Click += this.UserListActivity.IgnoredUserListItemMoreOptionsClick;
                view.viewUserStatus.Click += view.ViewUserStatus_Click;
                view.viewUserStatus.LongClick += view.ViewUserStatus_LongClick;
                view.UserListActivity = this.UserListActivity;
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).LongClick += ChatroomReceivedAdapter_LongClick;
                (view as View).Click += view.UserRowView_Click;
                (view as View).LongClick += view.UserRowView_LongClick;
                return new UserRowViewHolder(view as View);
            }
            else// if(viewType == CATEGORY)
            {
                ChatroomOverviewCategoryView view = ChatroomOverviewCategoryView.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += MessageOverviewClick;
                return new ChatroomOverviewCategoryHolder(view as View);
            }

        }
    }


    public class UserRowViewHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
    {
        public UserRowView userRowView;



        public UserRowViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            userRowView = (UserRowView)view;
            userRowView.ViewHolder = this;
            userRowView.SetOnCreateContextMenuListener(this);
        }

        public UserRowView getUnderlyingView()
        {
            return userRowView;
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {

            //base.OnCreateContextMenu(menu, v, menuInfo);
            UserRowView userRowView = v as UserRowView;
            string username = userRowView.viewUsername.Text;
            UserListActivity.PopUpMenuOwnerHack = username;
            Logger.Debug(username + " clicked");
            UserListItem userListItem = userRowView.BoundItem;
            bool isIgnored = userListItem.Role == UserRole.Ignored;

            if (isIgnored)
            {
                SeekerState.ActiveActivityRef.MenuInflater.Inflate(Resource.Menu.selected_ignored_user_menu, menu);
                CommonHelpers.AddUserNoteMenuItem(menu, -1, -1, -1, userListItem.Username);
            }
            else
            {
                SeekerState.ActiveActivityRef.MenuInflater.Inflate(Resource.Menu.selected_user_options, menu);
                CommonHelpers.AddUserNoteMenuItem(menu, -1, -1, -1, userListItem.Username);
                CommonHelpers.AddUserOnlineAlertMenuItem(menu, -1, -1, -1, userListItem.Username);
                CommonHelpers.AddGivePrivilegesIfApplicable(menu, -1);
            }
        }
    }


    public class UserRowView : RelativeLayout
    {
        public UserListItem BoundItem;

        public UserRowViewHolder ViewHolder;
        public UserListActivity UserListActivity = null;
        public TextView viewUsername;
        public ImageView viewUserStatus;
        public ImageView viewMoreOptions;
        public ImageView viewOnlineAlerts;
        public TextView viewNumFiles;
        public TextView viewSpeed;
        public TextView viewNote;

        public ViewGroup viewNoteLayout;
        public ViewGroup viewStatsLayout;

        //private TextView viewQueue;
        public UserRowView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.user_row, this, true);
            setupChildren();
        }
        public UserRowView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.user_row, this, true);
            setupChildren();
        }

        public void UserRowView_LongClick(object sender, View.LongClickEventArgs e)
        {
            (ViewHolder.BindingAdapter as RecyclerUserListAdapter).setPosition((sender as UserRowView).ViewHolder.AdapterPosition);
            (sender as View).ShowContextMenu();
        }

        public void UserRowView_Click(object sender, EventArgs e)
        {
            (ViewHolder.BindingAdapter as RecyclerUserListAdapter).setPosition((sender as UserRowView).ViewHolder.AdapterPosition);
            (sender as View).ShowContextMenu();
        }

        public static UserRowView inflate(ViewGroup parent)
        {
            UserRowView itemView = (UserRowView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.user_row_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewUserStatus = FindViewById<ImageView>(Resource.Id.userStatus);
            viewNumFiles = FindViewById<TextView>(Resource.Id.numFiles);
            viewSpeed = FindViewById<TextView>(Resource.Id.speed);
            viewNote = FindViewById<TextView>(Resource.Id.textViewNote);
            viewOnlineAlerts = FindViewById<ImageView>(Resource.Id.online_alerts_image);

            viewNoteLayout = FindViewById<ViewGroup>(Resource.Id.noteLayout);
            viewStatsLayout = FindViewById<ViewGroup>(Resource.Id.statsLayout);
            //viewMoreOptions = FindViewById<ImageView>(Resource.Id.options);
        }


        public void ViewUserStatus_Click(object sender, EventArgs e)
        {
            (sender as ImageView).PerformLongClick();

        }

        public void ViewUserStatus_LongClick(object sender, View.LongClickEventArgs e)
        {
            Toast.MakeText(SeekerState.ActiveActivityRef, (sender as ImageView).TooltipText, ToastLength.Short).Show();
        }

        //both item.UserStatus and item.UserData have status
        public static Soulseek.UserPresence GetStatusFromItem(UserListItem uli, out bool statusExists)
        {
            statusExists = false;
            Soulseek.UserPresence status = Soulseek.UserPresence.Away;
            if (uli.UserStatus != null)
            {
                statusExists = true;
                status = uli.UserStatus.Presence;
            }
            else if (uli.UserData != null)
            {
                statusExists = true;
                status = uli.UserData.Status;
            }
            return status;
        }

        public void setItem(UserListItem item)
        {
            BoundItem = item;
            viewUsername.Text = item.Username;
            try
            {
                if (item.Role == UserRole.Ignored)
                {
                    viewStatsLayout.Visibility = ViewStates.Gone;
                }
                else
                {
                    viewStatsLayout.Visibility = ViewStates.Visible;
                }

                if (SeekerState.UserNotes.ContainsKey(item.Username))
                {
                    viewNoteLayout.Visibility = ViewStates.Visible;
                    string note = null;
                    SeekerState.UserNotes.TryGetValue(item.Username, out note);
                    viewNote.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.note) + ": " + note;
                }
                else
                {
                    viewNoteLayout.Visibility = ViewStates.Gone;
                }

                if (SeekerState.UserOnlineAlerts.ContainsKey(item.Username))
                {
                    viewOnlineAlerts.Visibility = ViewStates.Visible;
                }
                else
                {
                    viewOnlineAlerts.Visibility = ViewStates.Invisible;
                }

                Soulseek.UserPresence status = GetStatusFromItem(item, out bool statusExists);

                if (item.Role == UserRole.Ignored)
                {
                    string ignoredString = SeekerState.ActiveActivityRef.GetString(Resource.String.ignored);
                    if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
                    {
                        viewUserStatus.TooltipText = ignoredString; //api26+ otherwise crash...
                    }
                    else
                    {
                        AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(viewUserStatus, ignoredString);
                    }
                }
                else if (statusExists && !item.DoesNotExist)
                {
                    var drawable = DrawableCompat.Wrap(viewUserStatus.Drawable);
                    var mutableDrawable = drawable.Mutate();
                    switch (status)
                    {
                        case Soulseek.UserPresence.Away:
                            viewUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.away));
                            string awayString = SeekerState.ActiveActivityRef.GetString(Resource.String.away);
                            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
                            {
                                viewUserStatus.TooltipText = awayString; //api26+ otherwise crash...
                            }
                            else
                            {
                                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(viewUserStatus, awayString);
                            }
                            break;
                        case Soulseek.UserPresence.Online:
                            viewUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.online)); //added in api 8 :) SetTint made it a weird dark color..
                            string onlineString = SeekerState.ActiveActivityRef.GetString(Resource.String.online);
                            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
                            {
                                viewUserStatus.TooltipText = onlineString; //api26+ otherwise crash...
                            }
                            else
                            {
                                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(viewUserStatus, onlineString);
                            }
                            break;
                        case Soulseek.UserPresence.Offline:
                            viewUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.offline));
                            string offlineString = SeekerState.ActiveActivityRef.GetString(Resource.String.offline);
                            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
                            {
                                viewUserStatus.TooltipText = offlineString; //api26+ otherwise crash...
                            }
                            else
                            {
                                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(viewUserStatus, offlineString);
                            }
                            break;
                    }
                }
                else if (item.DoesNotExist)
                {
                    viewUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.offline));
                    string doesNotExistString = "Does not Exist";
                    if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
                    {
                        viewUserStatus.TooltipText = doesNotExistString; //api26+ otherwise crash...
                    }
                    else
                    {
                        AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(viewUserStatus, doesNotExistString);
                    }
                }


                //int fCount=-1;
                //int speed = -1;

                bool userDataExists = false;
                if (item.UserData != null)
                {
                    userDataExists = true;
                    //fCount = item.UserData.FileCount;
                    //speed = item.UserData.AverageSpeed;
                }

                if (userDataExists)
                {
                    viewNumFiles.Text = item.UserData.FileCount.ToString("N0") + " " + SeekerState.ActiveActivityRef.GetString(Resource.String.files);
                    viewSpeed.Text = (item.UserData.AverageSpeed / 1024).ToString("N0") + " " + SlskHelp.CommonHelpers.STRINGS_KBS;
                }
                else
                {
                    viewNumFiles.Text = "-";
                    viewSpeed.Text = "-";
                }

                //viewFoldername.Text = Helpers.GetFolderNameFromFile(GetFileName(item));
                //viewSpeed.Text = (item.UploadSpeed / 1024).ToString(); //kb/s
            }
            catch (Exception e)
            {
                Logger.Firebase("user list activity set item: " + e.Message);
            }
            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }
    }
}