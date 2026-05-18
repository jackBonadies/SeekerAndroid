using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics.Drawable;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Seeker.Helpers;
using Seeker.Helpers.ActionSheet;
using Seeker.Services;

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


    public class UserRowViewHolder : RecyclerView.ViewHolder
    {
        public UserRowView userRowView;

        public UserRowViewHolder(View view) : base(view)
        {
            userRowView = (UserRowView)view;
            userRowView.ViewHolder = this;
        }

        public UserRowView getUnderlyingView()
        {
            return userRowView;
        }
    }

    public class UserListDiffCallback : DiffUtil.Callback
    {
        private List<UserListItem> oldList;
        private List<UserListItem> newList;

        public UserListDiffCallback(List<UserListItem> _oldList, List<UserListItem> _newList)
        {
            oldList = _oldList;
            newList = _newList;
        }

        public override int NewListSize => newList.Count;
        public override int OldListSize => oldList.Count;

        /// <summary>
        /// Doesnt seem to do anything.  still need to notify item changed, else it will be stale...
        /// </summary>
        /// <param name="oldItemPosition"></param>
        /// <param name="newItemPosition"></param>
        /// <returns></returns>
        public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
        {
            var oldItem = oldList[oldItemPosition];
            var newItem = newList[newItemPosition];
            return oldItem.Username.Equals(newItem.Username) && oldItem.GetStatusFromItem(out _) == newItem.GetStatusFromItem(out _);
        }

        public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
        {
            return oldList[oldItemPosition].Username.Equals(newList[newItemPosition].Username);
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
            (ViewHolder.BindingAdapter as RecyclerUserListAdapter).setPosition((sender as UserRowView).ViewHolder.BindingAdapterPosition);
            ShowActionSheet(sender as UserRowView);
            e.Handled = true;
        }

        public void UserRowView_Click(object sender, EventArgs e)
        {
            (ViewHolder.BindingAdapter as RecyclerUserListAdapter).setPosition((sender as UserRowView).ViewHolder.BindingAdapterPosition);
            ShowActionSheet(sender as UserRowView);
        }

        private void ShowActionSheet(UserRowView row)
        {
            string username = row.viewUsername.Text;
            var activity = this.UserListActivity ?? (SeekerState.ActiveActivityRef as Seeker.UserListActivity);
            if (activity == null)
            {
                return;
            }
            var snackView = activity.FindViewById<ViewGroup>(Resource.Id.userListMainLayoutId);
            bool isIgnored = row.BoundItem.Role == UserRole.Ignored;

            var config = new ActionSheetConfig();
            if (isIgnored)
            {
                config.Sections.Add(ActionSheetActions.BuildIgnoredUserActionsSection(
                    username,
                    activity,
                    snackView,
                    () =>
                    {
                        UserListService.Instance.RemoveFromIgnoreList(username);
                        activity?.NotifyItemRemovedExternal(username);
                    },
                    activity?.GetUpdateUserListItemActionExternal(username)));
            }
            else
            {
                Action refresh = activity?.GetUpdateUserListItemActionExternal(username);
                var options = new UserActionsOptions
                {
                    IncludeOnlineAlert = true,
                    IncludeGivePrivileges = true,
                    OnAddRemoved = refresh,
                    OnIgnoreChanged = refresh,
                    OnNoteChanged = refresh,
                    OnOnlineAlertChanged = refresh,
                    OverrideRemoveFromFriends = () =>
                    {
                        UserListService.Instance.RemoveUser(username);
                        activity?.NotifyItemRemovedExternal(username);
                    }
                };
                config.Sections.Add(ActionSheetActions.BuildUserActionsSection(username, activity, snackView, options));
            }

            UiHelpers.ShowActionSheetDialogSafe(activity.SupportFragmentManager, config);
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
            SeekerApplication.Toaster.ShowToast((sender as ImageView).TooltipText, ToastLength.Short);
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

                Soulseek.UserPresence status = item.GetStatusFromItem(out bool statusExists);

                if (item.Role == UserRole.Ignored)
                {
                    string ignoredString = SeekerState.ActiveActivityRef.GetString(Resource.String.ignored);
                    if (OperatingSystem.IsAndroidVersionAtLeast(26))
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
                            viewUserStatus.SetColorFilter(new Android.Graphics.Color(ContextCompat.GetColor(SeekerState.ActiveActivityRef, Resource.Color.away)));
                            string awayString = SeekerState.ActiveActivityRef.GetString(Resource.String.away);
                            if (OperatingSystem.IsAndroidVersionAtLeast(26))
                            {
                                viewUserStatus.TooltipText = awayString;
                            }
                            else
                            {
                                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(viewUserStatus, awayString);
                            }
                            break;
                        case Soulseek.UserPresence.Online:
                            viewUserStatus.SetColorFilter(new Android.Graphics.Color(ContextCompat.GetColor(SeekerState.ActiveActivityRef, Resource.Color.online))); //added in api 8 :) SetTint made it a weird dark color..
                            string onlineString = SeekerState.ActiveActivityRef.GetString(Resource.String.online);
                            if (OperatingSystem.IsAndroidVersionAtLeast(26))
                            {
                                viewUserStatus.TooltipText = onlineString;
                            }
                            else
                            {
                                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(viewUserStatus, onlineString);
                            }
                            break;
                        case Soulseek.UserPresence.Offline:
                            viewUserStatus.SetColorFilter(new Android.Graphics.Color(ContextCompat.GetColor(SeekerState.ActiveActivityRef, Resource.Color.offline)));
                            string offlineString = SeekerState.ActiveActivityRef.GetString(Resource.String.offline);
                            if (OperatingSystem.IsAndroidVersionAtLeast(26))
                            {
                                viewUserStatus.TooltipText = offlineString;
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
                    if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    {
                        viewUserStatus.TooltipText = doesNotExistString;
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
                    viewSpeed.Text = (item.UserData.AverageSpeed / 1024).ToString("N0") + " " + SimpleHelpers.STRINGS_KBS;
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