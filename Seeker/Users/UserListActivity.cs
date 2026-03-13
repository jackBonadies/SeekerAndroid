/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */

using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Messages;
using Seeker.Users;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Graphics.Drawable;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Common;
namespace Seeker
{
    [Activity(Label = "UserListActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false)]
    public class UserListActivity : ThemeableActivity
    {
        public static string PopUpMenuOwnerHack = string.Empty; //hack to get which listview item owns the popup menu (for on menu item click).

        public static string IntentSearchRoom = "SearchRoom";
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.user_list_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        private void NotifyItemChanged(string username)
        {
            int i = this.recyclerAdapter.GetPositionForUsername(username);
            if (i == -1)
            {
                return;
            }
            this.recyclerAdapter.NotifyItemChanged(i);
        }

        private Action GetUpdateUserListItemAction(string username)
        {
            Action a = new Action(() =>
            {
                NotifyItemChanged(username);
            });
            return a;
        }

        private void NotifyItemRemoved(string username)
        {
            int i = this.recyclerAdapter.GetPositionForUsername(username);
            if (i == -1)
            {
                return;
            }
            this.recyclerAdapter.RemoveFromDataSet(i);
            this.recyclerAdapter.NotifyItemRemoved(i);
        }


        public override bool OnContextItemSelected(IMenuItem item)
        {
            if (item.ItemId != Resource.Id.removeUser && item.ItemId != Resource.Id.removeUserFromIgnored)
            {
                if (UiHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), PopUpMenuOwnerHack, this, this.FindViewById<ViewGroup>(Resource.Id.userListMainLayoutId), GetUpdateUserListItemAction(PopUpMenuOwnerHack), null, null, GetUpdateUserListItemAction(PopUpMenuOwnerHack)))
                {
                    Logger.Debug("handled by commons");
                    return true;
                }
            }
            //TODO: handle common is below because actions like remove user also do an additional call.  it would be good to move that to an event.. OnResume to subscribe etc...
            switch (item.ItemId)
            {
                case Resource.Id.browseUsersFiles:
                    //do browse thing...
                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(MainActivity.GoToBrowseExtra, true);
                        this.StartActivity(intent);
                    });
                    View snackView = this.FindViewById<ViewGroup>(Resource.Id.userListMainLayoutId);
                    BrowseService.RequestFilesApi(PopUpMenuOwnerHack, snackView, action, null);
                    return true;
                case Resource.Id.searchUserFiles:
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = PopUpMenuOwnerHack;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(MainActivity.GoToSearchExtra, true);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.removeUser:
                    UserListService.Instance.RemoveUser(PopUpMenuOwnerHack);
                    this.NotifyItemRemoved(PopUpMenuOwnerHack);
                    return true;
                case Resource.Id.removeUserFromIgnored:
                    SeekerApplication.RemoveFromIgnoreList(PopUpMenuOwnerHack);
                    this.NotifyItemRemoved(PopUpMenuOwnerHack);
                    return true;
                case Resource.Id.messageUser:
                    Intent intentMsg = new Intent(SeekerState.ActiveActivityRef, typeof(MessagesActivity));
                    intentMsg.AddFlags(ActivityFlags.SingleTop);
                    intentMsg.PutExtra(MessageController.FromUserName, PopUpMenuOwnerHack); //so we can go to this user..
                    intentMsg.PutExtra(MessageController.ComingFromMessageTapped, true); //so we can go to this user..
                    this.StartActivity(intentMsg);
                    return true;
                case Resource.Id.getUserInfo:
                    RequestedUserInfoHelper.RequestUserInfoApi(PopUpMenuOwnerHack);
                    return true;
            }
            return true; //idk
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.add_user_action:
                    ShowEditTextDialogAddUserToList(false);
                    return true;
                case Resource.Id.add_user_to_ignored_action:
                    ShowEditTextDialogAddUserToList(true);
                    return true;
                case Resource.Id.sort_user_list_action:
                    ShowSortUserListDialog();
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressedDispatcher.OnBackPressed();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }
        private LinearLayoutManager recycleLayoutManager;
        public RecyclerUserListAdapter recyclerAdapter;
        private RecyclerView recyclerViewUserList;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SeekerState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.user_list_activity_layout);

            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.user_list_toolbar);
            myToolbar.InflateMenu(Resource.Menu.user_list_menu);
            myToolbar.Title = this.GetString(Resource.String.target_user_list);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            this.SupportActionBar.SetDisplayShowHomeEnabled(true);

            if (CommonState.UserList == null)
            {
                var sharedPref = this.GetSharedPreferences(Constants.SharedPrefFile, 0);
                CommonState.UserList = SerializationHelper.RestoreUserListFromString(sharedPref.GetString(KeyConsts.M_UserList, ""));
            }

            //this.SupportActionBar.SetBackgroundDrawable turn off overflow....

            recyclerViewUserList = this.FindViewById<RecyclerView>(Resource.Id.userList);
            recycleLayoutManager = new LinearLayoutManager(this);
            recyclerViewUserList.SetLayoutManager(recycleLayoutManager);

            RefreshUserList();
        }

        public void RefreshUserList()
        {
            if (CommonState.UserList != null)
            {
                lock (CommonState.UserList) //shouldnt we also lock IgnoreList?
                {
                    recyclerAdapter = new RecyclerUserListAdapter(this, ParseUserListForPresentation());
                    recyclerViewUserList.SetAdapter(recyclerAdapter);
                }
            }
        }


        private void OnUserStatusChanged(object sender, string username)
        {
            if (MainActivity.OnUIthread())
            {
                if (UserListSortOrder == SortOrder.OnlineStatus)
                {

                    var oldList = recyclerAdapter.localDataSet.ToList();
                    recyclerAdapter.localDataSet.Clear();
                    recyclerAdapter.localDataSet.AddRange(ParseUserListForPresentation());

                    DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new UserListDiffCallback(oldList, recyclerAdapter.localDataSet), true);
                    //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.Clear();
                    //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.AddRange(newList);
                    res.DispatchUpdatesTo(recyclerAdapter);

                    recyclerAdapter.NotifyItemChanged(recyclerAdapter.GetPositionForUsername(username));
                }
                else
                {
                    recyclerAdapter.NotifyItemChanged(recyclerAdapter.GetPositionForUsername(username));
                }
            }
            else
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() => { OnUserStatusChanged(null, username); });
            }
        }

        private static List<UserListItem> ParseUserListForPresentation()
        {
            String friendString = SeekerState.ActiveActivityRef.GetString(Resource.String.friends);
            String ignoredString = SeekerState.ActiveActivityRef.GetString(Resource.String.ignored);
            return UserListUtils.ParseUserListForPresentation(friendString, ignoredString);
        }

        protected override void OnPause()
        {
            SeekerApplication.UserStatusChangedUIEvent -= OnUserStatusChanged;
            base.OnPause();
        }

        protected override void OnResume()
        {
            RefreshUserList();
            SeekerApplication.UserStatusChangedUIEvent += OnUserStatusChanged;
            base.OnResume();
        }

        public override bool OnNavigateUp()
        {
            OnBackPressedDispatcher.OnBackPressed();
            return true;
            //return base.OnNavigateUp();
        }

        public static SortOrder UserListSortOrder
        {
            get => Common.PreferencesState.UserListSortOrder;
            set => Common.PreferencesState.UserListSortOrder = value;
        }
        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;
        public void ShowSortUserListDialog()
        {
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
            builder.SetTitle(this.Resources.GetString(Resource.String.SortUsersBy));

            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.change_sort_order_dialog, this.FindViewById(Android.Resource.Id.Content) as ViewGroup, false);

            AndroidX.AppCompat.Widget.AppCompatRadioButton onlineStatus = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.onlineStatus);
            AndroidX.AppCompat.Widget.AppCompatRadioButton alphaOrder = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.alphaOrder);
            AndroidX.AppCompat.Widget.AppCompatRadioButton dateAddedDesc = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.dateAddedDesc);
            AndroidX.AppCompat.Widget.AppCompatRadioButton dateAddedAsc = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.dateAddedAsc);

            RadioGroup radioGroupChangeUserSort = viewInflated.FindViewById<RadioGroup>(Resource.Id.radioGroupChangeUserSort);
            radioGroupChangeUserSort.CheckedChange += RadioGroupChangeUserSort_CheckedChange;


            switch (UserListSortOrder)
            {
                case SortOrder.DateAddedAsc:
                    dateAddedAsc.Checked = true;
                    break;
                case SortOrder.DateAddedDesc:
                    dateAddedDesc.Checked = true;
                    break;
                case SortOrder.Alphabetical:
                    alphaOrder.Checked = true;
                    break;
                case SortOrder.OnlineStatus:
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
                dialogInstance = null; //memory cleanup
            });

            builder.SetPositiveButton(Resource.String.okay, eventHandlerClose);
            dialogInstance = builder.Create();
            dialogInstance.Show();

        }

        private void RadioGroupChangeUserSort_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            SortOrder prev = UserListSortOrder;
            switch (e.CheckedId)
            {
                case Resource.Id.dateAddedAsc:
                    UserListSortOrder = SortOrder.DateAddedAsc;
                    break;
                case Resource.Id.dateAddedDesc:
                    UserListSortOrder = SortOrder.DateAddedDesc;
                    break;
                case Resource.Id.alphaOrder:
                    UserListSortOrder = SortOrder.Alphabetical;
                    break;
                case Resource.Id.onlineStatus:
                    UserListSortOrder = SortOrder.OnlineStatus;
                    break;
            }

            if (prev != UserListSortOrder)
            {
                PreferencesManager.SaveUserListSortOrder();
                this.RefreshUserList();
            }
        }

        public void ShowEditTextDialogAddUserToList(bool toIgnored)
        {
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
            // the reason the title is plural is because, using enter, you can add multiple users without closing the dialog.
            if (toIgnored)
            {
                string ignoreUser = this.GetString(Resource.String.ignore_user_title);
                builder.SetTitle(ignoreUser);
            }
            else
            {
                string addUser = this.GetString(Resource.String.add_user_title);
                builder.SetTitle(addUser);
            }

            var rootView = (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView;
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.autocomplete_user_dialog_content, rootView, false);
            AutoCompleteTextView input = (AutoCompleteTextView)viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserEditText);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(input, true);
            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                if (toIgnored)
                {
                    if (string.IsNullOrEmpty(input.Text))
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_type_a_username_to_add), ToastLength.Short);
                        return;
                    }

                    SeekerApplication.AddToIgnoreListFeedback(SeekerState.ActiveActivityRef, input.Text.ToString());
                    SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() => { RefreshUserList(); }));
                }
                else
                {
                    if (input.Text != String.Empty)
                    {
                        SeekerState.RecentUsersManager.AddUserToTop(input.Text, true);
                    }
                    UserListService.AddUserAPI(SeekerState.ActiveActivityRef, input.Text, new Action(() => { RefreshUserList(); }));
                }
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search)
                {
                    Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(Window.DecorView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Firebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Browse Logic...
                    eventHandler(sender, null);
                }
            };

            System.EventHandler<TextView.KeyEventArgs> keypressAction = (object sender, TextView.KeyEventArgs e) =>
            {
                if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
                {
                    Logger.Debug("keypress: " + e.Event.KeyCode.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(Window.DecorView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Firebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Browse Logic...
                    eventHandler(sender, null);
                }
                else
                {
                    e.Handled = false;
                }
            };

            input.KeyPress += keypressAction;

            input.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.add, eventHandler);
            builder.SetNegativeButton(Resource.String.close, eventHandlerCancel);
            // Set up the buttons

            var dialog = builder.Show();
            UiHelpers.DoNotEnablePositiveUntilText(dialog, input);
        }
    }
}