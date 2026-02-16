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
        public static string IntentUserGoToBrowse = "GoToBrowse";
        public static string IntentUserGoToSearch = "GoToSearch";
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
                if (CommonHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), PopUpMenuOwnerHack, this, this.FindViewById<ViewGroup>(Resource.Id.userListMainLayoutId), GetUpdateUserListItemAction(PopUpMenuOwnerHack), null, null, GetUpdateUserListItemAction(PopUpMenuOwnerHack)))
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
                        intent.PutExtra(IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                    });
                    View snackView = this.FindViewById<ViewGroup>(Resource.Id.userListMainLayoutId);
                    DownloadDialog.RequestFilesApi(PopUpMenuOwnerHack, snackView, action, null);
                    return true;
                case Resource.Id.searchUserFiles:
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = PopUpMenuOwnerHack;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(IntentUserGoToSearch, 1);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.removeUser:
                    UserListService.RemoveUser(PopUpMenuOwnerHack);
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

            if (SeekerState.UserList == null)
            {
                var sharedPref = this.GetSharedPreferences(Constants.SharedPrefFile, 0);
                SeekerState.UserList = SerializationHelper.RestoreUserListFromString(sharedPref.GetString(KeyConsts.M_UserList, ""));
            }

            //this.SupportActionBar.SetBackgroundDrawable turn off overflow....

            recyclerViewUserList = this.FindViewById<RecyclerView>(Resource.Id.userList);
            recycleLayoutManager = new LinearLayoutManager(this);
            recyclerViewUserList.SetLayoutManager(recycleLayoutManager);

            RefreshUserList();
        }

        public enum SortOrder
        {
            DateAddedAsc = 0,
            DateAddedDesc = 1,
            Alphabetical = 2,
            OnlineStatus = 3
        }

        public class UserListAlphabeticalComparer : IComparer<UserListItem>
        {
            // Compares by UserCount then Name
            public int Compare(UserListItem x, UserListItem y)
            {
                if (x is UserListItem xData && y is UserListItem yData)
                {
                    return xData.Username.CompareTo(yData.Username);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// This will do it like QT does it, where ties will be broken by Alphabet.
        /// </summary>
        public class UserListOnlineStatusComparer : IComparer<UserListItem>
        {
            // Compares by UserCount then Name
            public int Compare(UserListItem x, UserListItem y)
            {

                int xStatus = x.DoesNotExist ? -1 : (int)UserRowView.GetStatusFromItem(x, out _);
                int yStatus = y.DoesNotExist ? -1 : (int)UserRowView.GetStatusFromItem(y, out _);

                if (xStatus == yStatus)
                {
                    //tie breaker is alphabet
                    return x.Username.CompareTo(y.Username);
                }
                else
                {
                    return yStatus - xStatus;
                }
            }
        }


        public static List<UserListItem> GetSortedUserList(List<UserListItem> userlistOrig, bool isIgnoreList)
        {
            //always copy so the original does not get messed up, since it stores info on date added.
            List<UserListItem> userlist = userlistOrig.ToList();
            if (!isIgnoreList)
            {
                switch (UserListSortOrder)
                {
                    case SortOrder.DateAddedAsc:
                        return userlist;
                    case SortOrder.DateAddedDesc:
                        userlist.Reverse();
                        return userlist;
                    case SortOrder.Alphabetical:
                        userlist.Sort(new UserListAlphabeticalComparer());
                        return userlist;
                    case SortOrder.OnlineStatus:
                        userlist.Sort(new UserListOnlineStatusComparer());
                        return userlist;
                    default:
                        return userlist;
                }
            }
            else
            {
                switch (UserListSortOrder)
                {
                    case SortOrder.DateAddedAsc:
                        return userlist;
                    case SortOrder.DateAddedDesc:
                        userlist.Reverse();
                        return userlist;
                    case SortOrder.Alphabetical:
                        userlist.Sort(new UserListAlphabeticalComparer());
                        return userlist;
                    case SortOrder.OnlineStatus:
                        //we do not keep any data on ignored users online status.
                        userlist.Sort(new UserListAlphabeticalComparer());
                        return userlist;
                    default:
                        return userlist;
                }
            }

        }

        private static List<UserListItem> ParseUserListForPresentation()
        {
            List<UserListItem> forAdapter = new List<UserListItem>();
            if (SeekerState.UserList.Count != 0)
            {
                forAdapter.Add(new UserListItem(SeekerState.ActiveActivityRef.GetString(Resource.String.friends), UserRole.Category));
                forAdapter.AddRange(GetSortedUserList(SeekerState.UserList, false));
            }
            if (SeekerState.IgnoreUserList.Count != 0)
            {
                forAdapter.Add(new UserListItem(SeekerState.ActiveActivityRef.GetString(Resource.String.ignored), UserRole.Category));
                forAdapter.AddRange(GetSortedUserList(SeekerState.IgnoreUserList, true));
            }
            return forAdapter;
        }

        public void RefreshUserList()
        {
            if (SeekerState.UserList != null)
            {
                lock (SeekerState.UserList) //shouldnt we also lock IgnoreList?
                {
                    recyclerAdapter = new RecyclerUserListAdapter(this, ParseUserListForPresentation());
                    recyclerViewUserList.SetAdapter(recyclerAdapter);
                }
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
                return oldList[oldItemPosition].Username.Equals(newList[newItemPosition].Username) && UserRowView.GetStatusFromItem(oldList[oldItemPosition], out _) == UserRowView.GetStatusFromItem(newList[newItemPosition], out _);
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                return oldList[oldItemPosition].Username.Equals(newList[newItemPosition].Username);
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
                    //int prevPosition = recyclerAdapter.GetPositionForUsername(username);
                    //recyclerAdapter.localDataSet.Clear();
                    //recyclerAdapter.localDataSet.AddRange(ParseUserListForPresentation());
                    //int newPosition = recyclerAdapter.GetPositionForUsername(username);
                    //if(prevPosition!=newPosition)
                    //{
                    //    recyclerAdapter.NotifyItemMoved(prevPosition, newPosition);
                    //}
                    //else
                    //{
                    //    recyclerAdapter.NotifyItemChanged(recyclerAdapter.GetPositionForUsername(username));
                    //}


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

        public static void AddUserLogic(Context c, string username, Action UIaction, bool massImportCase = false)
        {
            if (!massImportCase)
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.adding_user_), username), ToastLength.Short).Show();
            }

            Action<Task<Soulseek.UserData>> continueWithAction = (Task<Soulseek.UserData> t) =>
            {
                if (t == null || t.IsFaulted)
                {
                    //failed to add user
                    if (t.Exception != null && t.Exception.Message != null && t.Exception.Message.ToLower().Contains("the wait timed out"))
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                        {
                            Toast.MakeText(c, Resource.String.error_adding_user_timeout, ToastLength.Short).Show();
                        });
                    }
                    else if (t.Exception != null && t.Exception != null && t.Exception.InnerException is Soulseek.UserNotFoundException)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                        {
                            if (!massImportCase)
                            {
                                Toast.MakeText(c, Resource.String.error_adding_user_not_found, ToastLength.Short).Show();
                            }
                            else
                            {
                                Toast.MakeText(c, String.Format("Error adding {0}: user not found", username), ToastLength.Short).Show();
                            }
                        });
                    }
                }
                else
                {
                    UserListService.AddUser(t.Result);
                    if (!massImportCase)
                    {
                        if (SeekerState.SharedPreferences != null && SeekerState.UserList != null)
                        {
                            lock (SeekerState.SharedPrefLock)
                            {
                                var editor = SeekerState.SharedPreferences.Edit();
                                editor.PutString(KeyConsts.M_UserList, SerializationHelper.SaveUserListToString(SeekerState.UserList));
                                editor.Commit();
                            }
                        }
                    }
                    if (UIaction != null)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(UIaction);
                    }
                }
            };

            //Add User Logic...
            SeekerState.SoulseekClient.AddUserAsync(username).ContinueWith(continueWithAction);
        }

        public static void AddUserAPI(Context c, string username, Action UIaction, bool massImportCase = false)
        {

            if (username == string.Empty || username == null)
            {
                Toast.MakeText(c, Resource.String.must_type_a_username_to_add, ToastLength.Short).Show();
                return;
            }

            if (!PreferencesState.CurrentlyLoggedIn)
            {
                Toast.MakeText(c, Resource.String.must_be_logged_to_add_or_remove_user, ToastLength.Short).Show();
                return;
            }

            if (UserListService.ContainsUser(username))
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.already_added_user_), username), ToastLength.Short).Show();
                return;
            }

            Action<Task> actualActionToPerform = new Action<Task>((Task t) =>
            {
                if (t.IsFaulted)
                {
                    //only show once for the original fault.
                    Logger.Debug("task is faulted, prop? " + (t.Exception.InnerException is FaultPropagationException)); //t.Exception is always Aggregate Exception..
                    if (!(t.Exception.InnerException is FaultPropagationException))
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                    }
                    throw new FaultPropagationException();
                }
                SeekerState.ActiveActivityRef.RunOnUiThread(() => { AddUserLogic(c, username, UIaction, massImportCase); });
            });

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Logger.Debug("CurrentlyLoggedInButDisconnectedState");
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(c, false, out t))
                {
                    return;
                }
                SeekerApplication.OurCurrentLoginTask = t.ContinueWith(actualActionToPerform);
            }
            else if (MainActivity.IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(actualActionToPerform, "User will be added once login is complete."))
            {
                Logger.Debug("IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction");
                return;
            }
            else
            {
                AddUserLogic(c, username, UIaction, massImportCase);
            }
        }

        public static SortOrder UserListSortOrder
        {
            get => (SortOrder)Common.PreferencesState.UserListSortOrder;
            set => Common.PreferencesState.UserListSortOrder = (int)value;
        }
        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;
        public void ShowSortUserListDialog()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
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
                lock (SeekerState.SharedPrefLock)
                {
                    var editor = SeekerState.SharedPreferences.Edit();
                    editor.PutInt(KeyConsts.M_UserListSortOrder, (int)UserListSortOrder);
                    editor.Commit();
                }
                this.RefreshUserList();
            }
        }

        public void ShowEditTextDialogAddUserToList(bool toIgnored)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
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

            // I'm using fragment here so I'm using getView() to provide ViewGroup
            // but you can provide here any other instance of ViewGroup from your Fragment / Activity
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.add_user_to_userlist, (ViewGroup)this.FindViewById<ViewGroup>(Resource.Layout.user_list_activity_layout), false);
            // Set up the input
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
                        Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.must_type_a_username_to_add, ToastLength.Short).Show();
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
                    AddUserAPI(SeekerState.ActiveActivityRef, input.Text, new Action(() => { RefreshUserList(); }));
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
            CommonHelpers.DoNotEnablePositiveUntilText(dialog, input);
        }



        //public void IgnoredUserListItemMoreOptionsClick(object sender, EventArgs e) //this can get attached very many times...
        //{
        //    try
        //    {
        //        PopUpMenuOwnerHack = ((sender as View).Parent as ViewGroup).FindViewById<TextView>(Resource.Id.textViewUser).Text; //this is a hack.....
        //        PopupMenu popup = new PopupMenu(SeekerState.MainActivityRef, sender as View);
        //        popup.SetOnMenuItemClickListener(this);//  setOnMenuItemClickListener(MainActivity.this);
        //        popup.Inflate(Resource.Menu.selected_ignored_user_menu);
        //        popup.Show();
        //    }
        //    catch (System.Exception error)
        //    {
        //        //in response to a crash android.view.WindowManager.BadTokenException
        //        //This crash is usually caused by your app trying to display a dialog using a previously-finished Activity as a context.
        //        //in this case not showing it is probably best... as opposed to a crash...
        //        Logger.Firebase(error.Message + " IGNORE POPUP BAD ERROR");
        //    }
        //}

        //public void UserListItemMoreOptionsClick(object sender, EventArgs e) //this can get attached very many times...
        //{
        //    try
        //    {
        //        PopUpMenuOwnerHack = ((sender as View).Parent as ViewGroup).FindViewById<TextView>(Resource.Id.textViewUser).Text; //this is a hack.....
        //        PopupMenu popup = new PopupMenu(SeekerState.MainActivityRef, sender as View);
        //        popup.SetOnMenuItemClickListener(this);//  setOnMenuItemClickListener(MainActivity.this);
        //        popup.Inflate(Resource.Menu.selected_user_options);
        //        Helpers.AddUserNoteMenuItem(popup.Menu, -1, -1, -1, PopUpMenuOwnerHack);
        //        Helpers.AddGivePrivilegesIfApplicable(popup.Menu, -1);
        //        popup.Show();
        //    }
        //    catch (System.Exception error)
        //    {
        //        //in response to a crash android.view.WindowManager.BadTokenException
        //        //This crash is usually caused by your app trying to display a dialog using a previously-finished Activity as a context.
        //        //in this case not showing it is probably best... as opposed to a crash...
        //        Logger.Firebase(error.Message + " POPUP BAD ERROR");
        //    }
        //}
    }
}