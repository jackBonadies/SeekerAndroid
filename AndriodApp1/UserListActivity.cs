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

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Graphics.Drawable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidX.RecyclerView.Widget;

namespace AndriodApp1
{
    [Activity(Label = "UserListActivity", Theme = "@style/AppTheme.NoActionBar")]
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
            Action a = new Action(() => {
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
                if (Helpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), PopUpMenuOwnerHack, this, this.FindViewById<ViewGroup>(Resource.Id.userListMainLayoutId), GetUpdateUserListItemAction(PopUpMenuOwnerHack), null,null, GetUpdateUserListItemAction(PopUpMenuOwnerHack)))
                {
                    MainActivity.LogDebug("handled by commons");
                    return true;
                }
            }
            //TODO: handle common is below because actions like remove user also do an additional call.  it would be good to move that to an event.. OnResume to subscribe etc...
            switch (item.ItemId)
            {
                case Resource.Id.browseUsersFiles:
                    //do browse thing...
                    Action<View> action = new Action<View>((v) => {
                        Intent intent = new Intent(SoulSeekState.MainActivityRef, typeof(MainActivity));
                        intent.PutExtra(IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                        //((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    View snackView = this.FindViewById<ViewGroup>(Resource.Id.userListMainLayoutId);
                    DownloadDialog.RequestFilesApi(PopUpMenuOwnerHack, snackView, action, null);
                    return true;
                case Resource.Id.searchUserFiles:
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = PopUpMenuOwnerHack;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SoulSeekState.MainActivityRef, typeof(MainActivity));
                    intent.PutExtra(IntentUserGoToSearch, 1);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.removeUser:
                    MainActivity.UserListRemoveUser(PopUpMenuOwnerHack);
                    this.NotifyItemRemoved(PopUpMenuOwnerHack);
                    return true;
                case Resource.Id.removeUserFromIgnored:
                    SeekerApplication.RemoveFromIgnoreList(PopUpMenuOwnerHack);
                    this.NotifyItemRemoved(PopUpMenuOwnerHack);
                    return true;
                case Resource.Id.messageUser:
                    Intent intentMsg = new Intent(SoulSeekState.ActiveActivityRef, typeof(MessagesActivity));
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
            switch(item.ItemId)
            {
                case Resource.Id.add_user_action:
                    ShowEditTextDialogAddUserToList();
                    return true;
                case Resource.Id.sort_user_list_action:
                    ShowSortUserListDialog();
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressed();
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
            SoulSeekState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.user_list_activity_layout);

            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.user_list_toolbar);
            myToolbar.InflateMenu(Resource.Menu.user_list_menu);
            myToolbar.Title = this.GetString(Resource.String.target_user_list);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            this.SupportActionBar.SetDisplayShowHomeEnabled(true);

            if (SoulSeekState.UserList == null)
            {
                var sharedPref = this.GetSharedPreferences("SoulSeekPrefs", 0);
                SoulSeekState.UserList = SeekerApplication.RestoreUserListFromString(sharedPref.GetString(SoulSeekState.M_UserList,""));
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

                if(xStatus == yStatus)
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
            if(!isIgnoreList)
            {
                switch(UserListSortOrder)
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
            List <UserListItem> forAdapter = new List<UserListItem>();
            if (SoulSeekState.UserList.Count!=0)
            {
                forAdapter.Add(new UserListItem(SoulSeekState.ActiveActivityRef.GetString(Resource.String.friends),UserRole.Category));
                forAdapter.AddRange(GetSortedUserList(SoulSeekState.UserList, false));
            }
            if(SoulSeekState.IgnoreUserList.Count!=0)
            {
                forAdapter.Add(new UserListItem(SoulSeekState.ActiveActivityRef.GetString(Resource.String.ignored), UserRole.Category));
                forAdapter.AddRange(GetSortedUserList(SoulSeekState.IgnoreUserList, true));
            }
            return forAdapter;
        }

        public void RefreshUserList()
        {
            if (SoulSeekState.UserList != null)
            {
                lock (SoulSeekState.UserList)
                {
                    recyclerAdapter = new RecyclerUserListAdapter(this,ParseUserListForPresentation());
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
            if(MainActivity.OnUIthread())
            {
                if(UserListSortOrder == SortOrder.OnlineStatus)
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
                SoulSeekState.ActiveActivityRef.RunOnUiThread(()=>{ OnUserStatusChanged(null, username); });
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
            OnBackPressed();
            return true;
            //return base.OnNavigateUp();
        }

        public static void AddUserLogic(Context c, string username, Action UIaction, bool massImportCase = false)
        {
            if(!massImportCase)
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.adding_user_),username), ToastLength.Short).Show();
            }

            Action<Task<Soulseek.UserData>> continueWithAction = (Task<Soulseek.UserData> t) =>
            {
                if (t == null || t.IsFaulted)
                {
                    //failed to add user
                    if (t.Exception != null && t.Exception.Message != null && t.Exception.Message.ToLower().Contains("the wait timed out"))
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                            Toast.MakeText(c, Resource.String.error_adding_user_timeout, ToastLength.Short).Show();
                        });
                    }
                    else if (t.Exception != null && t.Exception != null && t.Exception.InnerException is Soulseek.UserNotFoundException)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                            if(!massImportCase)
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
                    MainActivity.UserListAddUser(t.Result);
                    if(!massImportCase)
                    {
                        if (SoulSeekState.SharedPreferences != null && SoulSeekState.UserList != null)
                        {
                            lock (MainActivity.SHARED_PREF_LOCK)
                            {
                             var editor = SoulSeekState.SharedPreferences.Edit();
                            editor.PutString(SoulSeekState.M_UserList, SeekerApplication.SaveUserListToString(SoulSeekState.UserList));
                            editor.Commit();
                            }
                        }
                    }
                    if (UIaction != null)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(UIaction);
                    }
                }
            };

            //Add User Logic...
            SoulSeekState.SoulseekClient.AddUserAsync(username).ContinueWith(continueWithAction);
        }

        public static void AddUserAPI(Context c, string username, Action UIaction, bool massImportCase = false)
        {
            
            if (username == string.Empty || username == null)
            {
                Toast.MakeText(c, Resource.String.must_type_a_username_to_add, ToastLength.Short).Show();
                return;
            }

            if(!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(c, Resource.String.must_be_logged_to_add_or_remove_user, ToastLength.Short).Show();
                return;
            }

            if(MainActivity.UserListContainsUser(username))
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.already_added_user_),username), ToastLength.Short).Show();
                return;
            }

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(c, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                        {

                            Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();

                        });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(()=>{AddUserLogic(c,username,UIaction, massImportCase); });

                }));
            }
            else
            {
                AddUserLogic(c, username, UIaction, massImportCase);
            }
        }

        public static SortOrder UserListSortOrder = SortOrder.DateAddedAsc;
        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;
        public void ShowSortUserListDialog()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle("Sort Users By");

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

            if(prev != UserListSortOrder)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutInt(SoulSeekState.M_UserListSortOrder, (int)UserListSortOrder);
                    editor.Commit();
                }
                this.RefreshUserList();
            }
        }

        public void ShowEditTextDialogAddUserToList()
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(Resource.String.add_user_title);
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
                if(input.Text!=String.Empty)
                {
                    SoulSeekState.RecentUsersManager.AddUserToTop(input.Text, true);
                }
                AddUserAPI(SoulSeekState.ActiveActivityRef, input.Text, new Action(() => { RefreshUserList(); }));
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
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(Window.DecorView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Browse Logic...
                    eventHandler(sender, null);
                }
            };

            System.EventHandler<TextView.KeyEventArgs> keypressAction = (object sender, TextView.KeyEventArgs e) =>
            {
                if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
                {
                    MainActivity.LogDebug("keypress: " + e.Event.KeyCode.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(Window.DecorView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
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

            builder.Show();
        }

        //public void IgnoredUserListItemMoreOptionsClick(object sender, EventArgs e) //this can get attached very many times...
        //{
        //    try
        //    {
        //        PopUpMenuOwnerHack = ((sender as View).Parent as ViewGroup).FindViewById<TextView>(Resource.Id.textViewUser).Text; //this is a hack.....
        //        PopupMenu popup = new PopupMenu(SoulSeekState.MainActivityRef, sender as View);
        //        popup.SetOnMenuItemClickListener(this);//  setOnMenuItemClickListener(MainActivity.this);
        //        popup.Inflate(Resource.Menu.selected_ignored_user_menu);
        //        popup.Show();
        //    }
        //    catch (System.Exception error)
        //    {
        //        //in response to a crash android.view.WindowManager.BadTokenException
        //        //This crash is usually caused by your app trying to display a dialog using a previously-finished Activity as a context.
        //        //in this case not showing it is probably best... as opposed to a crash...
        //        MainActivity.LogFirebase(error.Message + " IGNORE POPUP BAD ERROR");
        //    }
        //}

        //public void UserListItemMoreOptionsClick(object sender, EventArgs e) //this can get attached very many times...
        //{
        //    try
        //    {
        //        PopUpMenuOwnerHack = ((sender as View).Parent as ViewGroup).FindViewById<TextView>(Resource.Id.textViewUser).Text; //this is a hack.....
        //        PopupMenu popup = new PopupMenu(SoulSeekState.MainActivityRef, sender as View);
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
        //        MainActivity.LogFirebase(error.Message + " POPUP BAD ERROR");
        //    }
        //}
    }



    [System.Serializable]
    public enum UserRole
    {
        Friend = 0,
        Ignored = 1,
        Category = 2
    }

    /// <summary>
    /// This is the full user info...
    /// </summary>
    [System.Serializable] //else error even with binary serializer
    public class UserListItem
    {
        public string Username = string.Empty;
        public UserRole Role;
        public Soulseek.UserStatus UserStatus; //add user updates this..
        public Soulseek.UserData UserData; //add user updates this as well...
        public Soulseek.UserInfo UserInfo; //this is the "picture and everything" one that we have to explicitly request from the peer (not server)...
        public bool DoesNotExist; //we dont allow someone to add a user that does not exist, BUT if they add someone and their username expires or such, then this will happen...
        public UserListItem(string username, UserRole role)
        {
            Role = role;
            Username = username;
            UserStatus = null;
            UserData = null;
            UserInfo = null;
        }
        public UserListItem(string username)
        {
            Username = username;
            UserStatus = null;
            UserData = null;
            UserInfo = null;
        }
        public UserListItem()
        {
            UserStatus = null;
            UserData = null;
            UserInfo = null;
        }
    }

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
            MainActivity.LogDebug(username + " clicked");
            UserListItem userListItem = userRowView.BoundItem;
            bool isIgnored = userListItem.Role == UserRole.Ignored;

            if(isIgnored)
            {
                SoulSeekState.ActiveActivityRef.MenuInflater.Inflate(Resource.Menu.selected_ignored_user_menu, menu);
                Helpers.AddUserNoteMenuItem(menu, -1, -1, -1, userListItem.Username);
            }
            else
            {
                SoulSeekState.ActiveActivityRef.MenuInflater.Inflate(Resource.Menu.selected_user_options, menu);
                Helpers.AddUserNoteMenuItem(menu, -1, -1, -1, userListItem.Username);
                Helpers.AddUserOnlineAlertMenuItem(menu, -1, -1, -1, userListItem.Username);
                Helpers.AddGivePrivilegesIfApplicable(menu, -1);
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
            Toast.MakeText(SoulSeekState.ActiveActivityRef, (sender as ImageView).TooltipText, ToastLength.Short).Show();
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
                if(item.Role == UserRole.Ignored)
                {
                    viewStatsLayout.Visibility = ViewStates.Gone;
                }
                else
                {
                    viewStatsLayout.Visibility = ViewStates.Visible;
                }

                if(SoulSeekState.UserNotes.ContainsKey(item.Username))
                {
                    viewNoteLayout.Visibility = ViewStates.Visible;
                    string note = null;
                    SoulSeekState.UserNotes.TryGetValue(item.Username, out note);
                    viewNote.Text = SoulSeekState.ActiveActivityRef.GetString(Resource.String.note) + ": " + note;
                }
                else
                {
                    viewNoteLayout.Visibility = ViewStates.Gone;
                }

                if(SoulSeekState.UserOnlineAlerts.ContainsKey(item.Username))
                {
                    viewOnlineAlerts.Visibility = ViewStates.Visible;
                }
                else
                {
                    viewOnlineAlerts.Visibility = ViewStates.Invisible;
                }

                Soulseek.UserPresence status = GetStatusFromItem(item, out bool statusExists);

                if(item.Role == UserRole.Ignored)
                {
                    string ignoredString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.ignored);
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
                    switch(status)
                    {
                        case Soulseek.UserPresence.Away:
                            viewUserStatus.SetColorFilter(Resources.GetColor(Resource.Color.away));
                            string awayString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.away);
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
                            string onlineString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.online);
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
                            string offlineString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.offline);
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
                else if(item.DoesNotExist)
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

                if(userDataExists)
                {
                    viewNumFiles.Text = item.UserData.FileCount.ToString("N0") + " " + SoulSeekState.ActiveActivityRef.GetString(Resource.String.files);
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
                MainActivity.LogFirebase("user list activity set item: " + e.Message);
            }
            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }
    }
}