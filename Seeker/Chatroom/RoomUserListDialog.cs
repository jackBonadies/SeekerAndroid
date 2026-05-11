using Seeker.Helpers;
using Seeker.Browse;
using Seeker.Messages;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Common;
using Google.Android.Material.BottomSheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Messages;

namespace Seeker.Chatroom
{
    public class RoomUserListDialog : BottomSheetDialogFragment
    {

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
                    UI_userDataList[previousPosition] = UI_userDataList[previousPosition].WithStatus(e.Status);
                    if (PreferencesState.SortChatroomUsersBy != SortOrderChatroomUsers.OnlineStatus)
                    {
                        //position wont change
                        roomUserListAdapter.NotifyItemChanged(previousPosition);
                    }
                    else
                    {
                        bool wasAtTop = recycleLayoutManager.FindFirstCompletelyVisibleItemPosition() == 0;
                        int positionOfTopItem = recycleLayoutManager.FindFirstVisibleItemPosition();

                        UI_userDataList.Sort(new ChatroomUserDataComparer(UserListService.Instance, PreferencesState.PutFriendsOnTop, PreferencesState.SortChatroomUsersBy)); //resort so the new item goes into place...
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
                            Logger.Debug("case where that person would otherwise be hidden, so we fix it by moving up seamlessly.");
                            recycleLayoutManager.ScrollToPosition(0);
                        }
                        else if (positionOfTopItem == previousPosition && positionOfTopItem != newPosition)
                        {
                            Logger.Debug("case where the recyclerview tries to disorientingly scroll to that person, so we fix it by not doing that..");
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
                            ChatroomUserData cud = ChatroomController.GetChatroomUserData(udata, Soulseek.UserRole.Normal);
                            UI_userDataList.Add(cud);
                            UI_userDataList.Sort(new ChatroomUserDataComparer(UserListService.Instance, PreferencesState.PutFriendsOnTop, PreferencesState.SortChatroomUsersBy)); //resort so the new item goes into place...
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
                            Logger.Debug("not there" + uname);
                            return;
                        }
                        UI_userDataList.RemoveAt(indexToRemove);
                        roomUserListAdapter.NotifyItemRemoved(indexToRemove);
                    }
                    UpdateMembersHeader();
                });

            }
            catch (Exception e)
            {
                Logger.Firebase("EXCEPTION UpdateData " + e.Message + e.StackTrace);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.room_users_dialog, container, false);
        }

        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;
        private static RoomUserListDialog RoomDialogInstance;
        public static void ShowSortRoomUserListDialog()
        {
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(SeekerState.ActiveActivityRef);
            builder.SetTitle(Resource.String.SortUsersBy);

            View viewInflated = LayoutInflater.From(SeekerState.ActiveActivityRef).Inflate(Resource.Layout.change_sort_room_user_list_dialog, SeekerState.ActiveActivityRef.FindViewById(Android.Resource.Id.Content) as ViewGroup, false);

            //AndroidX.AppCompat.Widget.AppCompatRadioButton onlineStatus = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.onlineStatus);
            AndroidX.AppCompat.Widget.AppCompatRadioButton alphaOrder = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.alphaOrder);
            AndroidX.AppCompat.Widget.AppCompatRadioButton onlineStatus = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.onlineStatus);

            RadioGroup radioGroupChangeUserSort = viewInflated.FindViewById<RadioGroup>(Resource.Id.radioGroupChangeUserSort);
            radioGroupChangeUserSort.CheckedChange += RadioGroupChangeUserSort_CheckedChange;

            CheckBox alwaysPlaceFriendsAtTopCheckBox = viewInflated.FindViewById<CheckBox>(Resource.Id.alwaysPlaceFriendsAtTop);
            alwaysPlaceFriendsAtTopCheckBox.Checked = PreferencesState.PutFriendsOnTop;
            alwaysPlaceFriendsAtTopCheckBox.CheckedChange += AlwaysPlaceFriendsAtTopCheckBox_CheckedChange;

            switch (PreferencesState.SortChatroomUsersBy)
            {
                case SortOrderChatroomUsers.Alphabetical:
                    alphaOrder.Checked = true;
                    break;
                case SortOrderChatroomUsers.OnlineStatus:
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
            bool putFriendsAtTop = PreferencesState.PutFriendsOnTop;
            PreferencesState.PutFriendsOnTop = e.IsChecked;
            if (putFriendsAtTop != PreferencesState.PutFriendsOnTop)
            {
                PreferencesManager.SavePutFriendsOnTop();
                RoomDialogInstance.RefreshUserListFull();
            }
        }

        private static void RadioGroupChangeUserSort_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            SortOrderChatroomUsers prev = PreferencesState.SortChatroomUsersBy;
            switch (e.CheckedId)
            {
                case Resource.Id.onlineStatus:
                    PreferencesState.SortChatroomUsersBy = SortOrderChatroomUsers.OnlineStatus;
                    break;
                case Resource.Id.alphaOrder:
                    PreferencesState.SortChatroomUsersBy = SortOrderChatroomUsers.Alphabetical;
                    break;
            }

            if (prev != PreferencesState.SortChatroomUsersBy)
            {
                PreferencesManager.SaveSortChatroomUsersBy();
                RoomDialogInstance.RefreshUserListFull();
            }
        }


        private List<Soulseek.UserData> UI_userDataList = null;

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        private View headerTitleRow;
        private View headerSearchRow;
        private TextView headerRoomName;
        private TextView headerMembersLine;
        private AndroidX.AppCompat.Widget.SearchView headerSearchView;
        private ImageButton searchClearButton;

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            var bottomSheet = ((BottomSheetDialog)Dialog).FindViewById<View>(Resource.Id.design_bottom_sheet);
            if (bottomSheet != null)
            {
                var behavior = BottomSheetBehavior.From(bottomSheet);
                behavior.State = BottomSheetBehavior.StateExpanded;
                behavior.SkipCollapsed = true;

                var lp = bottomSheet.LayoutParameters;
                lp.Height = ViewGroup.LayoutParams.MatchParent;
                bottomSheet.LayoutParameters = lp;
            }

            headerTitleRow = view.FindViewById<View>(Resource.Id.roomUsersHeaderTitleRow);
            headerSearchRow = view.FindViewById<View>(Resource.Id.roomUsersHeaderSearchRow);
            headerRoomName = view.FindViewById<TextView>(Resource.Id.roomUsersHeaderRoomName);
            headerMembersLine = view.FindViewById<TextView>(Resource.Id.roomUsersHeaderMembersLine);
            headerSearchView = view.FindViewById<AndroidX.AppCompat.Widget.SearchView>(Resource.Id.roomUsersHeaderSearchView);

            headerRoomName.Text = OurRoomName;

            headerSearchView.QueryHint = SeekerApplication.GetString(Resource.String.FilterUsers);
            headerSearchView.QueryTextSubmit += RoomUserListDialog_QueryTextSubmit;

            // Hide SearchView's built-in close button — we provide our own dual-state X.
            // SearchView's updateCloseButton() flips this Gone back to Visible whenever
            // text is non-empty, so we also clear its drawable so it renders as nothing
            // (and re-force Gone on every text change in the QueryTextChange handler below).
            var builtInClose = headerSearchView.FindViewById<ImageView>(Resource.Id.search_close_btn);
            if (builtInClose != null)
            {
                builtInClose.SetImageDrawable(null);
                builtInClose.Visibility = ViewStates.Gone;
            }

            searchClearButton = view.FindViewById<ImageButton>(Resource.Id.roomUsersHeaderSearchClose);
            searchClearButton.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(headerSearchView.Query))
                {
                    HideSearchRow();
                }
                else
                {
                    headerSearchView.SetQuery(string.Empty, true);
                }
            };

            // Wire after the clear button is captured so the handler can repaint it.
            headerSearchView.QueryTextChange += (s, e) =>
            {
                if (builtInClose != null)
                {
                    builtInClose.Visibility = ViewStates.Gone;
                }
                UpdateClearButtonState(e.NewText);
                RoomUserListDialog_QueryTextChange(s, e);
            };
            UpdateClearButtonState(string.Empty);

            var searchButton = view.FindViewById<ImageButton>(Resource.Id.roomUsersHeaderSearch);
            searchButton.Click += (s, e) => ShowSearchRow();

            var sortButton = view.FindViewById<ImageButton>(Resource.Id.roomUsersHeaderSort);
            sortButton.Click += (s, e) =>
            {
                RoomDialogInstance = this;
                ShowSortRoomUserListDialog();
            };

            recyclerViewUsers = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewUsers);
            recyclerViewUsers.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleLayoutManager = new LinearLayoutManager(Activity);
            this.RefreshUserListFull();
            recyclerViewUsers.SetLayoutManager(recycleLayoutManager);
        }

        private void ShowSearchRow()
        {
            headerTitleRow.Visibility = ViewStates.Invisible;
            headerSearchRow.Visibility = ViewStates.Visible;
            headerSearchView.Iconified = false;
            headerSearchView.RequestFocus();
        }

        private void HideSearchRow()
        {
            var imm = (InputMethodManager)Activity?.GetSystemService(Context.InputMethodService);
            imm?.HideSoftInputFromWindow(headerSearchView.WindowToken, 0);
            headerSearchView.SetQuery(string.Empty, false);
            headerSearchRow.Visibility = ViewStates.Gone;
            headerTitleRow.Visibility = ViewStates.Visible;
        }

        private void UpdateClearButtonState(string queryText)
        {
            if (searchClearButton == null)
            {
                return;
            }
            float density = searchClearButton.Resources.DisplayMetrics.Density;
            int basePad = (int)(10 * density);
            int innerPad = (int)(13 * density);
            if (string.IsNullOrEmpty(queryText))
            {
                var tv = new Android.Util.TypedValue();
                searchClearButton.Context.Theme.ResolveAttribute(Resource.Attribute.selectableItemBackgroundBorderless, tv, true);
                searchClearButton.SetBackgroundResource(tv.ResourceId);
                searchClearButton.SetPadding(basePad, basePad, basePad, basePad);
            }
            else
            {
                searchClearButton.SetBackgroundResource(Resource.Drawable.room_users_clear_circle);
                searchClearButton.SetPadding(innerPad, innerPad, innerPad, innerPad);
            }
        }

        private void UpdateMembersHeader()
        {
            if (headerMembersLine == null)
            {
                return;
            }
            string label = SeekerApplication.GetString(Resource.String.members);
            int count = ChatroomController.JoinedRoomData[OurRoomName].Users?.Count ?? 0;
            string full = label + "  " + count;
            var ss = new SpannableString(full);
            ss.SetSpan(new StyleSpan(TypefaceStyle.Bold), 0, label.Length, SpanTypes.ExclusiveExclusive);
            Color subdued = UiHelpers.GetColorFromAttribute(headerMembersLine.Context, Resource.Attribute.cellTextColorSubdued);
            ss.SetSpan(new ForegroundColorSpan(subdued), label.Length, full.Length, SpanTypes.ExclusiveExclusive);
            headerMembersLine.TextFormatted = ss;
        }

        public void RefreshUserListFull()
        {
            UI_userDataList = ChatroomController.GetWrappedUserData(OurRoomName, IsPrivate, this.FilterText);
            roomUserListAdapter = new RoomUserListRecyclerAdapter(UI_userDataList);
            recyclerViewUsers.SetAdapter(roomUserListAdapter);
            UpdateMembersHeader();
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
            int i = this.roomUserListAdapter.GetPositionForUserData(userData);
            if (i == -1)
            {
                return;
            }
            this.roomUserListAdapter.NotifyItemChanged(i);
        }

        public Action GetUpdateUserListRoomAction(Soulseek.UserData longClickedUserData)
        {
            Action a = new Action(() =>
            {
                NotifyItemChanged(longClickedUserData);
            });
            return a;
        }

        public Action GetUpdateUserListRoomActionAddedRemoved(Soulseek.UserData longClickedUserData)
        {
            Action a = null;
            if (PreferencesState.PutFriendsOnTop)
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
                    UI_userDataList.Sort(new ChatroomUserDataComparer(UserListService.Instance, PreferencesState.PutFriendsOnTop, PreferencesState.SortChatroomUsersBy)); //resort so the new item goes into place...
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
                        Logger.Debug("case where that person would otherwise be hidden, so we fix it by moving up seamlessly.");
                        recycleLayoutManager.ScrollToPosition(0);
                    }
                    else if (positionOfTopItem == previousPosition && positionOfTopItem != newPosition)
                    {
                        Logger.Debug("case where the recyclerview tries to disorientingly scroll to that person, so we fix it by not doing that..");
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

    }

}