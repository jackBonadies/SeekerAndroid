using Seeker.Helpers;
using Seeker.Messages;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Chatroom
{
    public class RoomUserListDialog : AndroidX.Fragment.App.DialogFragment //, PopupMenu.IOnMenuItemClickListener doesnt work for dialogfragment
    {

        public static Soulseek.UserData longClickedUserData = null;


        public static string OurRoomName = string.Empty;
        public static bool IsPrivate = false;
        private RecyclerView recyclerViewUsers = null;
        private RoomUserListRecyclerAdapter roomUserListAdapter = null;
        private LinearLayoutManager recycleLayoutManager = null;

        public static RoomUserListDialog forContextHelp = null;

        public RoomUserListDialog(string ourRoomName, bool isPrivate)
        {
            OurRoomName = ourRoomName;
            IsPrivate = isPrivate;
            forContextHelp = this;
        }
        public RoomUserListDialog()
        {
            forContextHelp = this;
        }


        public override void OnResume()
        {
            base.OnResume();
            ChatroomController.RoomModeratorsChanged += OnRoomModeratorsChanged;
            ChatroomController.UserJoinedOrLeft += OnUserJoinedOrLeft;
            ChatroomController.UserRoomStatusChanged += OnUserRoomStatusChanged;
            Window window = Dialog.Window;//  getDialog().getWindow();
            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;

            window.SetLayout((int)(width * 0.90), Android.Views.WindowManagerLayoutParams.WrapContent);//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
        }
        public override void OnPause()
        {
            ChatroomController.RoomModeratorsChanged -= OnRoomModeratorsChanged;
            ChatroomController.UserJoinedOrLeft -= OnUserJoinedOrLeft;
            ChatroomController.UserRoomStatusChanged -= OnUserRoomStatusChanged;
            base.OnPause();
        }

        public void OnUserRoomStatusChanged(object sender, UserRoomStatusChangedEventArgs e)
        {
            if (e.RoomName == OurRoomName)
            {

                this.Activity.RunOnUiThread(() =>
                {

                    int previousPosition = -1;
                    for (int i = 0; i < UI_userDataList.Count; i++)
                    {
                        if (UI_userDataList[i].Username == e.User)
                        {
                            previousPosition = i;
                            break;
                        }
                    }
                    if (previousPosition == -1)
                    {
                        return;
                    }
                    UI_userDataList[previousPosition].Status = e.Status;
                    if (ChatroomController.SortChatroomUsersBy != ChatroomController.SortOrderChatroomUsers.OnlineStatus)
                    {
                        //position wont change
                        roomUserListAdapter.NotifyItemChanged(previousPosition);
                    }
                    else
                    {
                        bool wasAtTop = recycleLayoutManager.FindFirstCompletelyVisibleItemPosition() == 0;
                        int positionOfTopItem = recycleLayoutManager.FindFirstVisibleItemPosition();

                        UI_userDataList.Sort(new ChatroomController.ChatroomUserDataComparer(ChatroomController.PutFriendsOnTop, ChatroomController.SortChatroomUsersBy)); //resort so the new item goes into place...
                        int newPosition = -1;
                        for (int i = 0; i < UI_userDataList.Count; i++)
                        {
                            if (UI_userDataList[i].Username == e.User)
                            {
                                newPosition = i;
                                break;
                            }
                        }

                        IParcelable p = null;
                        if (positionOfTopItem == previousPosition && positionOfTopItem != newPosition)
                        {
                            p = recycleLayoutManager.OnSaveInstanceState();
                        }

                        roomUserListAdapter.NotifyItemMoved(previousPosition, newPosition);
                        roomUserListAdapter.NotifyItemChanged(newPosition); //this is always necessary..

                        if (wasAtTop)
                        {
                            MainActivity.LogDebug("case where that person would otherwise be hidden, so we fix it by moving up seamlessly.");
                            recycleLayoutManager.ScrollToPosition(0);
                        }
                        else if (positionOfTopItem == previousPosition && positionOfTopItem != newPosition)
                        {
                            MainActivity.LogDebug("case where the recyclerview tries to disorientingly scroll to that person, so we fix it by not doing that..");
                            recycleLayoutManager.OnRestoreInstanceState(p);
                        }


                    }

                });
            }
        }

        public void OnUserJoinedOrLeft(object sender, UserJoinedOrLeftEventArgs e)
        {
            if (e.RoomName == OurRoomName)
            {
                UpdateDataIncremental(e.Joined, e.User, e.UserData);
            }
        }

        public void OnRoomModeratorsChanged(object sender, UserJoinedOrLeftEventArgs e)
        {
            //TODO diffutil stuff... well we can do the diffutil since its easy to see who got added / removed...
            if (e.RoomName == OurRoomName)
            {
                UpdateDataIncremental(e.Joined, e.User, e.UserData);
            }
        }

        private void UpdateDataIncremental(bool joined, string uname, Soulseek.UserData udata)
        {
            try
            {

                this.Activity.RunOnUiThread(() =>
                {

                    if (joined)
                    {
                        if (uname.Contains(FilterText))
                        {
                            Soulseek.ChatroomUserData cud = ChatroomController.GetChatroomUserData(udata, Soulseek.UserRole.Normal);
                            UI_userDataList.Add(cud);
                            UI_userDataList.Sort(new ChatroomController.ChatroomUserDataComparer(ChatroomController.PutFriendsOnTop, ChatroomController.SortChatroomUsersBy)); //resort so the new item goes into place...
                            int itemInsertedAt = UI_userDataList.IndexOf(cud);
                            roomUserListAdapter.NotifyItemInserted(itemInsertedAt);
                        }
                    }
                    else
                    {
                        int indexToRemove = -1;
                        for (int i = 0; i < UI_userDataList.Count; i++)
                        {
                            if (UI_userDataList[i].Username == uname)
                            {
                                indexToRemove = i;
                                break;
                            }
                        }
                        if (indexToRemove == -1)
                        {
                            MainActivity.LogDebug("not there" + uname);
                            return;
                        }
                        UI_userDataList.RemoveAt(indexToRemove);
                        roomUserListAdapter.NotifyItemRemoved(indexToRemove);
                    }
                });

            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("EXCEPTION UpdateData " + e.Message + e.StackTrace);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.room_users_dialog, container); //container is parent
        }

        public class ToolbarMenuItemClickListener : Java.Lang.Object, AndroidX.AppCompat.Widget.Toolbar.IOnMenuItemClickListener
        {
            public RoomUserListDialog RoomDialog;
            public bool OnMenuItemClick(IMenuItem item)
            {
                switch (item.ItemId)
                {
                    case Resource.Id.sort_room_user_list_action:
                        RoomDialogInstance = RoomDialog;
                        ShowSortRoomUserListDialog();
                        return true;
                }
                return true;
            }
        }


        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;
        private static RoomUserListDialog RoomDialogInstance;
        public static void ShowSortRoomUserListDialog()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(SeekerState.ActiveActivityRef, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(Resource.String.SortUsersBy);

            View viewInflated = LayoutInflater.From(SeekerState.ActiveActivityRef).Inflate(Resource.Layout.change_sort_room_user_list_dialog, SeekerState.ActiveActivityRef.FindViewById(Android.Resource.Id.Content) as ViewGroup, false);

            //AndroidX.AppCompat.Widget.AppCompatRadioButton onlineStatus = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.onlineStatus);
            AndroidX.AppCompat.Widget.AppCompatRadioButton alphaOrder = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.alphaOrder);
            AndroidX.AppCompat.Widget.AppCompatRadioButton onlineStatus = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.onlineStatus);

            RadioGroup radioGroupChangeUserSort = viewInflated.FindViewById<RadioGroup>(Resource.Id.radioGroupChangeUserSort);
            radioGroupChangeUserSort.CheckedChange += RadioGroupChangeUserSort_CheckedChange;

            CheckBox alwaysPlaceFriendsAtTopCheckBox = viewInflated.FindViewById<CheckBox>(Resource.Id.alwaysPlaceFriendsAtTop);
            alwaysPlaceFriendsAtTopCheckBox.Checked = ChatroomController.PutFriendsOnTop;
            alwaysPlaceFriendsAtTopCheckBox.CheckedChange += AlwaysPlaceFriendsAtTopCheckBox_CheckedChange;

            switch (ChatroomController.SortChatroomUsersBy)
            {
                case ChatroomController.SortOrderChatroomUsers.Alphabetical:
                    alphaOrder.Checked = true;
                    break;
                case ChatroomController.SortOrderChatroomUsers.OnlineStatus:
                    onlineStatus.Checked = true;
                    break;
            }

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandlerClose = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    dialogInstance.Dismiss();
                }
                RoomDialogInstance = null;
                dialogInstance = null; //memory cleanup
            });

            builder.SetPositiveButton(Resource.String.okay, eventHandlerClose);
            dialogInstance = builder.Create();
            dialogInstance.Show();

        }

        private static void AlwaysPlaceFriendsAtTopCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            bool putFriendsAtTop = ChatroomController.PutFriendsOnTop;
            ChatroomController.PutFriendsOnTop = e.IsChecked;
            if (putFriendsAtTop != ChatroomController.PutFriendsOnTop)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SeekerState.SharedPreferences.Edit();
                    editor.PutBoolean(KeyConsts.M_RoomUserListShowFriendsAtTop, ChatroomController.PutFriendsOnTop);
                    editor.Commit();
                }
                RoomDialogInstance.RefreshUserListFull();
            }
        }

        private static void RadioGroupChangeUserSort_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            ChatroomController.SortOrderChatroomUsers prev = ChatroomController.SortChatroomUsersBy;
            switch (e.CheckedId)
            {
                case Resource.Id.onlineStatus:
                    ChatroomController.SortChatroomUsersBy = ChatroomController.SortOrderChatroomUsers.OnlineStatus;
                    break;
                case Resource.Id.alphaOrder:
                    ChatroomController.SortChatroomUsersBy = ChatroomController.SortOrderChatroomUsers.Alphabetical;
                    break;
            }

            if (prev != ChatroomController.SortChatroomUsersBy)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SeekerState.SharedPreferences.Edit();
                    editor.PutInt(KeyConsts.M_RoomUserListSortOrder, (int)ChatroomController.SortChatroomUsersBy);
                    editor.Commit();
                }
                RoomDialogInstance.RefreshUserListFull();
            }
        }


        private List<Soulseek.UserData> UI_userDataList = null;

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));

            this.SetStyle((int)DialogFragmentStyle.Normal, 0);
            this.Dialog.SetTitle(OurRoomName);


            var toolbar = (AndroidX.AppCompat.Widget.Toolbar)view.FindViewById(Resource.Id.roomUsersDialogToolbar);
            var tmicl = new ToolbarMenuItemClickListener();
            tmicl.RoomDialog = this;
            toolbar.SetOnMenuItemClickListener(tmicl);
            toolbar.InflateMenu(Resource.Menu.room_user_dialog_menu);
            toolbar.Title = OurRoomName;
            var searchRoomMenuItem = toolbar.Menu.FindItem(Resource.Id.search_room_action);
            var searchView = searchRoomMenuItem.ActionView as AndroidX.AppCompat.Widget.SearchView;
            searchView.QueryHint = SeekerApplication.GetString(Resource.String.FilterUsers);
            searchView.QueryTextChange += RoomUserListDialog_QueryTextChange;
            searchView.QueryTextSubmit += RoomUserListDialog_QueryTextSubmit;


            recyclerViewUsers = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewUsers);
            recyclerViewUsers.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleLayoutManager = new LinearLayoutManager(Activity);
            this.RefreshUserListFull();
            //userViewDataList = ChatroomController.GetWrappedUserData(OurRoomName, IsPrivate, out this.cachedNewestTimeJoined);
            //roomUserListAdapter = new RoomUserListRecyclerAdapter(userViewDataList);
            //recyclerViewUsers.SetAdapter(roomUserListAdapter);
            recyclerViewUsers.SetLayoutManager(recycleLayoutManager);
            this.RegisterForContextMenu(recyclerViewUsers);
        }

        public void RefreshUserListFull()
        {
            UI_userDataList = ChatroomController.GetWrappedUserData(OurRoomName, IsPrivate, this.FilterText);
            roomUserListAdapter = new RoomUserListRecyclerAdapter(UI_userDataList);
            recyclerViewUsers.SetAdapter(roomUserListAdapter);
        }

        private void RoomUserListDialog_QueryTextSubmit(object sender, AndroidX.AppCompat.Widget.SearchView.QueryTextSubmitEventArgs e)
        {
            //nothing to do. we do it as text change.
        }

        private void RoomUserListDialog_QueryTextChange(object sender, AndroidX.AppCompat.Widget.SearchView.QueryTextChangeEventArgs e)
        {
            string oldText = FilterText;
            FilterText = e.NewText;
            if (FilterText.Contains(oldText))
            {
                //more restrictive so just filter out based on our current..
                var filtered = UI_userDataList.Where(x => x.Username.Contains(FilterText, StringComparison.InvariantCultureIgnoreCase)).ToList();
                UI_userDataList.Clear();
                UI_userDataList.AddRange(filtered);
                this.roomUserListAdapter.NotifyDataSetChanged();
            }
            else
            {
                //less restrictive so get from main data source.
                UI_userDataList.Clear();
                UI_userDataList.AddRange(ChatroomController.GetWrappedUserData(OurRoomName, IsPrivate, this.FilterText));
                this.roomUserListAdapter.NotifyDataSetChanged();
            }
        }

        private string FilterText = string.Empty;

        private void FilterUIUsersView()
        {
            UI_userDataList.Clear();

        }

        //public bool OnMenuItemClick(IMenuItem item)
        //{
        //    return false;
        //    //throw new NotImplementedException();
        //}

        private void NotifyItemChanged(Soulseek.UserData userData)
        {
            int i = this.roomUserListAdapter.GetPositionForUserData(longClickedUserData);
            if (i == -1)
            {
                return;
            }
            this.roomUserListAdapter.NotifyItemChanged(i);
        }

        private Action GetUpdateUserListRoomAction(Soulseek.UserData longClickedUserData)
        {
            Action a = new Action(() =>
            {
                NotifyItemChanged(longClickedUserData);
            });
            return a;
        }

        private Action GetUpdateUserListRoomActionAddedRemoved(Soulseek.UserData longClickedUserData)
        {
            Action a = null;
            if (ChatroomController.PutFriendsOnTop)
            {
                a = new Action(() =>
                {
                    bool wasAtTop = recycleLayoutManager.FindFirstCompletelyVisibleItemPosition() == 0;
                    int positionOfTopItem = recycleLayoutManager.FindFirstVisibleItemPosition();

                    int previousPosition = -1;
                    for (int i = 0; i < UI_userDataList.Count; i++)
                    {
                        if (UI_userDataList[i].Username == longClickedUserData.Username)
                        {
                            previousPosition = i;
                            break;
                        }
                    }
                    if (previousPosition == -1)
                    {
                        return;
                    }
                    UI_userDataList.Sort(new ChatroomController.ChatroomUserDataComparer(ChatroomController.PutFriendsOnTop, ChatroomController.SortChatroomUsersBy)); //resort so the new item goes into place...
                    int newPosition = -1;
                    for (int i = 0; i < UI_userDataList.Count; i++)
                    {
                        if (UI_userDataList[i].Username == longClickedUserData.Username)
                        {
                            newPosition = i;
                            break;
                        }
                    }

                    IParcelable p = null;
                    if (positionOfTopItem == previousPosition && positionOfTopItem != newPosition)
                    {
                        p = recycleLayoutManager.OnSaveInstanceState();
                    }

                    roomUserListAdapter.NotifyItemMoved(previousPosition, newPosition);
                    roomUserListAdapter.NotifyItemChanged(newPosition); //this is always necessary..

                    if (wasAtTop)
                    {
                        MainActivity.LogDebug("case where that person would otherwise be hidden, so we fix it by moving up seamlessly.");
                        recycleLayoutManager.ScrollToPosition(0);
                    }
                    else if (positionOfTopItem == previousPosition && positionOfTopItem != newPosition)
                    {
                        MainActivity.LogDebug("case where the recyclerview tries to disorientingly scroll to that person, so we fix it by not doing that..");
                        recycleLayoutManager.OnRestoreInstanceState(p);
                    }
                });
            }
            else
            {
                a = new Action(() =>
                {
                    NotifyItemChanged(longClickedUserData);
                });
            }
            return a;
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            var userdata = longClickedUserData;
            if (item.ItemId != 0) //this is "Remove User" as in Remove User from Room!
            {
                if (CommonHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), userdata.Username, SeekerState.ActiveActivityRef, this.View.FindViewById<ViewGroup>(Resource.Id.userListRoom), GetUpdateUserListRoomAction(userdata), GetUpdateUserListRoomActionAddedRemoved(userdata), GetUpdateUserListRoomAction(userdata)))
                {
                    MainActivity.LogDebug("Handled by commons");
                    return base.OnContextItemSelected(item);
                }
            }
            switch (item.ItemId)
            {
                case 0: //"Remove User"
                    ChatroomController.AddRemoveUserToPrivateRoomAPI(OurRoomName, userdata.Username, true, false, true);
                    //                    SeekerState.ActiveActivityRef.RunOnUiThread(GetUpdateUserListRoomAction(userdata));
                    return true;
                case 1: //"Remove Moderator Privilege"
                    ChatroomController.AddRemoveUserToPrivateRoomAPI(OurRoomName, userdata.Username, true, true, true);
                    SeekerState.ActiveActivityRef.RunOnUiThread(GetUpdateUserListRoomAction(userdata));
                    return true;
                case 2:
                    ChatroomController.AddRemoveUserToPrivateRoomAPI(OurRoomName, userdata.Username, true, true, false);
                    SeekerState.ActiveActivityRef.RunOnUiThread(GetUpdateUserListRoomAction(userdata));
                    return true;
                case 3: //browse user
                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                    });
                    View snackView = this.View.FindViewById<ViewGroup>(Resource.Id.userListRoom);
                    DownloadDialog.RequestFilesApi(userdata.Username, snackView, action, null);
                    return true;
                case 4: //search users files
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = userdata.Username;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToSearch, 1);
                    this.StartActivity(intent);
                    return true;
                case 7: //message user
                    Intent intentMsg = new Intent(SeekerState.ActiveActivityRef, typeof(MessagesActivity));
                    intentMsg.AddFlags(ActivityFlags.SingleTop);
                    intentMsg.PutExtra(MessageController.FromUserName, userdata.Username); //so we can go to this user..
                    intentMsg.PutExtra(MessageController.ComingFromMessageTapped, true); //so we can go to this user..
                    this.StartActivity(intentMsg);
                    return true;
                default:
                    break;
            }

            return base.OnContextItemSelected(item);
        }

    }

}