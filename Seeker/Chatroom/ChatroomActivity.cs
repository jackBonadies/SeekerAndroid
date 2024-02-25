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

using Seeker.Chatroom;
using Seeker.Helpers;
using Seeker.Messages;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Seeker
{

    [Activity(Label = "ChatroomActivity", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask, Exported = false)]
    public class ChatroomActivity : SlskLinkMenuActivity//, Android.Widget.PopupMenu.IOnMenuItemClickListener
    {
        public static ChatroomActivity ChatroomActivityRef = null;

        public static bool ShowStatusesView = true;
        public static bool ShowTickerView = false;
        public static bool ShowUserOnlineAwayStatusUpdates = true;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            MainActivity.LogDebug("chatroom activity on create");
            base.OnCreate(savedInstanceState);

            ChatroomActivityRef = this;
            SeekerState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.chatroom_main_layout);


            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.chatroom_toolbar);
            myToolbar.InflateMenu(Resource.Menu.chatroom_overview_list_menu);
            myToolbar.Title = this.Resources.GetString(Resource.String.chatrooms);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            //this.SupportActionBar.SetDisplayShowHomeEnabled(true);
            bool startWithUserFragment = false;

            if (savedInstanceState != null && savedInstanceState.GetBoolean("SaveStateAtChatroomInner"))
            {
                MainActivity.LogDebug("restoring chatroom inner fragment");
                if (ChatroomInnerFragment.OurRoomInfo == null)
                {
                    MainActivity.LogDebug("ourroominfo is null");
                    RestoreStartingRoomInfo(savedInstanceState);
                    ChatroomController.StartingState = ChatroomInnerFragment.OurRoomInfo != null ? ChatroomInnerFragment.OurRoomInfo.Name : null;
                    //this means we have since been killed
                }
                startWithUserFragment = true;
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(ChatroomInnerFragment.OurRoomInfo), "ChatroomInnerFragment").Commit();
                //savedInstanceState.Clear(); //else we will keep doing the first even if the second was done by intent..
            }
            else if (Intent != null) //if an intent started this activity
            {
                if (Intent.GetBooleanExtra(ChatroomController.ComingFromMessageTapped, false))
                {
                    string goToRoom = Intent.GetStringExtra(ChatroomController.FromRoomName);
                    if (goToRoom == string.Empty)
                    {
                        MainActivity.LogFirebase("empty goToUsersMessages");
                    }
                    else
                    {
                        startWithUserFragment = true;
                        Soulseek.RoomInfo roomInfo = ChatroomController.RoomListParsed.FirstOrDefault((roomInfo) => { return roomInfo.Name == goToRoom; }); //roomListParsed can be null, causing crash.
                        SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(roomInfo), "ChatroomInnerFragment").Commit();
                        //switch in that fragment...
                        //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame,new MessagesOverviewFragment()).Commit();
                    }
                }
            }

            if (!startWithUserFragment)
            {
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomOverviewFragment(), "OuterListChatroomFragment").Commit();
            }

            //this.SupportActionBar.SetBackgroundDrawable turn off overflow....

            //ListView userList = this.FindViewById<ListView>(Resource.Id.userList);

            //RefreshUserList();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.Intent = intent;
            if (intent.GetBooleanExtra(ChatroomController.ComingFromMessageTapped, false))
            {
                string goToRoom = intent.GetStringExtra(ChatroomController.FromRoomName);
                if (goToRoom == string.Empty)
                {
                    MainActivity.LogFirebase("empty goToRoom");
                }
                else
                {
                    Soulseek.RoomInfo roomInfo = ChatroomController.RoomListParsed.FirstOrDefault((roomInfo) => { return roomInfo.Name == goToRoom; });
                    SupportFragmentManager.BeginTransaction().Remove(new ChatroomInnerFragment()).Commit();
                    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(roomInfo), "ChatroomInnerFragment").Commit();
                    //switch in that fragment...
                    //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame,new MessagesOverviewFragment()).Commit();
                }
            }
        }

        private const string INNER_ROOM_NAME_CONST = "INNER_ROOM_NAME_CONST";
        private const string INNER_ROOM_COUNT_CONST = "INNER_ROOM_COUNT_CONST";
        private const string INNER_ROOM_PRIV_CONST = "INNER_ROOM_PRIV_CONST";
        private const string INNER_ROOM_OWNED_CONST = "INNER_ROOM_OWNED_CONST";
        private const string INNER_ROOM_MOD_CONST = "INNER_ROOM_MOD_CONST";

        /// <summary>
        /// This saves the starting room info in case we get "am state killed"
        /// Its all the info we need to rejoin the room and get the full data.
        /// </summary>
        /// <param name="outState"></param>
        private static void SaveStartingRoomInfo(Bundle outState, ChatroomInnerFragment f)
        {
            if (ChatroomInnerFragment.OurRoomInfo != null)
            {
                outState.PutString(INNER_ROOM_NAME_CONST, ChatroomInnerFragment.OurRoomInfo.Name);
                outState.PutInt(INNER_ROOM_COUNT_CONST, ChatroomInnerFragment.OurRoomInfo.UserCount);
            }
            if (f != null && ChatroomController.RoomList != null)
            {
                outState.PutBoolean(INNER_ROOM_PRIV_CONST, f.IsPrivate());
                outState.PutBoolean(INNER_ROOM_OWNED_CONST, f.IsOwned());
                outState.PutBoolean(INNER_ROOM_MOD_CONST, f.IsOperatedByUs());
            }
        }

        private static void RestoreStartingRoomInfo(Bundle inState)
        {
            string rName = inState.GetString(INNER_ROOM_NAME_CONST, string.Empty);
            int rCount = inState.GetInt(INNER_ROOM_COUNT_CONST, -1);
            if (rName == string.Empty)
            {
                MainActivity.LogFirebase("no restore info...");
                return;
            }
            MainActivity.LogFirebase("restoring info...");
            ChatroomInnerFragment.OurRoomInfo = new Soulseek.RoomInfo(rName, rCount);
            ChatroomInnerFragment.cachedMod = inState.GetBoolean(INNER_ROOM_MOD_CONST, false);
            ChatroomInnerFragment.cachedOwned = inState.GetBoolean(INNER_ROOM_OWNED_CONST, false);
            ChatroomInnerFragment.cachedPrivate = inState.GetBoolean(INNER_ROOM_PRIV_CONST, false);
        }

        protected override void OnSaveInstanceState(Bundle outState) //gets hit on rotate, home button press
        {
            var f = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment");
            if (f != null && f.IsVisible)
            {
                outState.PutBoolean("SaveStateAtChatroomInner", true);
                MainActivity.LogDebug("SaveStateAtChatroomInner OnSaveInstanceState");
                SaveStartingRoomInfo(outState, f as ChatroomInnerFragment);
                MainActivity.LogDebug("currentlyInsideRoomName -- OnSaveInstanceState -- " + ChatroomController.currentlyInsideRoomName);
                //ChatroomController.currentlyInsideRoomName = ChatroomInnerFragment.OurRoomInfo.Name; //this sets it after we are leaving....
            }
            else
            {
                outState.PutBoolean("SaveStateAtChatroomInner", false);
            }
            base.OnSaveInstanceState(outState);
        }


        public override void OnBackPressed()
        {
            //if f is non null and f is visible then that means you are backing out from the inner user fragment..
            var f = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment");
            if (f != null && f.IsVisible)
            {
                if (SupportFragmentManager.BackStackEntryCount == 0) //this is if we got to inner messages through a notification, in which case we are done..
                {
                    base.OnBackPressed();
                    return;
                }
                AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.chatroom_toolbar);
                myToolbar.InflateMenu(Resource.Menu.chatroom_overview_list_menu);
                myToolbar.Title = this.Resources.GetString(Resource.String.chatrooms);
                this.SetSupportActionBar(myToolbar);
                this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                this.SupportActionBar.SetHomeButtonEnabled(true);
                SupportFragmentManager.BeginTransaction().Remove(f).Commit();
                //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomOverviewFragment(), "OuterListChatroomFragment").Commit();
            }
            base.OnBackPressed();
        }


        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MainActivity.LogDebug("on create options menu");
            var fOuter = SupportFragmentManager.FindFragmentByTag("OuterListChatroomFragment");
            var fInner = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment");
            if (fOuter != null && fOuter.IsVisible)
            {
                MenuInflater.Inflate(Resource.Menu.chatroom_overview_list_menu, menu);
            }
            else if (fInner != null && fInner.IsVisible)
            {
                //if private different options...
                MenuInflater.Inflate(Resource.Menu.chatroom_inner_menu, menu);
            }
            else
            {
                MenuInflater.Inflate(Resource.Menu.chatroom_overview_list_menu, menu);
            }
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            base.OnPrepareOptionsMenu(menu);
            MainActivity.LogDebug("on prepare options menu");
            var fInner = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment");

            //fix : there is a bug where if you rotate the phone in the inner chatroom and then press back
            //that menu.FindItem(*) will all be null.
            //the reason for this, is that on back pressed, for a brief moment, both fOuter and fInner
            //are visible.  and according to the function above this (OnCreateOptionsMenu) it checks 
            //and inflates fOuter first.  Immediately after we come here.  So if fOuter is visible as well
            //then we are going back and should NOT prepare any items..

            bool transitionBothVisible = false;
            var fOuter = SupportFragmentManager.FindFragmentByTag("OuterListChatroomFragment");
            if (fOuter != null)
            {
                transitionBothVisible = fOuter.IsVisible;
            }

            //end fix

            if (fInner != null && fInner.IsVisible && !transitionBothVisible)
            {
                //if private different options...
                MainActivity.LogDebug("on prepare options menu INNER");
                bool isPrivate = false;
                bool isOwnedByUs = false;
                bool isOperator = false;
                if (ChatroomController.RoomList != null)
                {
                    isPrivate = (fInner as ChatroomInnerFragment).IsPrivate();
                    isOwnedByUs = (fInner as ChatroomInnerFragment).IsOwned();
                    isOperator = (fInner as ChatroomInnerFragment).IsOperatedByUs();
                }
                else
                {   //this is if we were killed and started back on the inner page.  we may not have the room list at that point.
                    isPrivate = ChatroomInnerFragment.cachedPrivate;
                    isOwnedByUs = ChatroomInnerFragment.cachedOwned;
                    isOperator = ChatroomInnerFragment.cachedMod;
                }
                MainActivity.LogDebug("isPrivate: " + isPrivate + "isOwnedByUs: " + isOwnedByUs + "isOperator: " + isOperator);
                menu.FindItem(Resource.Id.invite_user_action).SetVisible(isOperator || isOwnedByUs); //tho what about a public room owned by us ?? if such a thing exists???
                menu.FindItem(Resource.Id.give_up_room_action).SetVisible(isOwnedByUs);
                menu.FindItem(Resource.Id.give_up_membership_action).SetVisible(isPrivate && !isOwnedByUs);

                if ((fInner as ChatroomInnerFragment).IsAutoJoin())
                {
                    menu.FindItem(Resource.Id.toggle_autojoin_action).SetTitle(this.Resources.GetString(Resource.String.auto_join_on)); //brackets mean current.. thats how desktop does it..
                }
                else
                {
                    menu.FindItem(Resource.Id.toggle_autojoin_action).SetTitle(this.Resources.GetString(Resource.String.auto_join_off));
                }

                if ((fInner as ChatroomInnerFragment).IsNotifyOn())
                {
                    menu.FindItem(Resource.Id.toggle_notify_room_action).SetTitle(this.Resources.GetString(Resource.String.notification_on)); //brackets mean current.. thats how desktop does it..
                }
                else
                {
                    menu.FindItem(Resource.Id.toggle_notify_room_action).SetTitle(this.Resources.GetString(Resource.String.notification_off));
                }

                if (ChatroomActivity.ShowTickerView)
                {
                    menu.FindItem(Resource.Id.hide_show_ticker_action).SetTitle(Resource.String.HideTickerView);
                }
                else
                {
                    menu.FindItem(Resource.Id.hide_show_ticker_action).SetTitle(Resource.String.ShowTickerView);
                }

                if (ChatroomActivity.ShowStatusesView)
                {
                    menu.FindItem(Resource.Id.hide_show_user_status_action).SetTitle(Resource.String.HideStatusView);
                }
                else
                {
                    menu.FindItem(Resource.Id.hide_show_user_status_action).SetTitle(Resource.String.ShowStatusView);
                }
            }
            return true;
        }

#if DEBUG
        public void MassMessagesTestMethod()
        {
            Task.Run(() =>
            {
                var r = new Random();
                System.Threading.Thread.Sleep(100);
                for (int i = 0; i < 1000; i++)
                {
                    System.Threading.Thread.Sleep(r.Next(0, 100));
                    Message m = new Message("test", 1, false, CommonHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, "test" + i, false);
                    ChatroomController.AddMessage(ChatroomInnerFragment.OurRoomInfo.Name, m); //background thread
                    ChatroomController.MessageReceived?.Invoke(null, new MessageReceivedArgs(ChatroomInnerFragment.OurRoomInfo.Name, m));
                }
                //produces an immediate collection was modified on the .Last() call.  could not have been an easier test ;)
            });
        }
#endif


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.refresh_room_list_action:
                    ChatroomController.GetRoomListApi(true);
                    return true;
                case Resource.Id.create_room_action:
                    ShowEditCreateChatroomDialog();
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    return true;
                case Resource.Id.toggle_autojoin_action:
                    ChatroomController.ToggleAutoJoin(ChatroomInnerFragment.OurRoomInfo.Name, true, this);
                    return true;
                case Resource.Id.toggle_notify_room_action:
                    //#if DEBUG
                    //this.MassMessagesTestMethod();
                    //#endif
                    ChatroomController.ToggleNotifyRoom(ChatroomInnerFragment.OurRoomInfo.Name, true, this);
                    return true;
                case Resource.Id.view_user_list_action:
                    ShowUserListDialog(ChatroomInnerFragment.OurRoomInfo, ChatroomController.IsPrivate(ChatroomInnerFragment.OurRoomInfo.Name));
                    return true;
                case Resource.Id.show_all_tickers_action:
                    ShowAllTickersDialog(ChatroomInnerFragment.OurRoomInfo.Name);
                    return true;
                case Resource.Id.set_ticker_action:
                    ShowSetTickerDialog(ChatroomInnerFragment.OurRoomInfo.Name);
                    return true;
                case Resource.Id.invite_user_action:
                    ShowInviteUserDialog(ChatroomInnerFragment.OurRoomInfo.Name);
                    return true;
                case Resource.Id.give_up_room_action:
                    ChatroomController.DropMembershipOrOwnershipApi(ChatroomInnerFragment.OurRoomInfo.Name, true, true);
                    return true;
                case Resource.Id.give_up_membership_action:
                    ChatroomController.DropMembershipOrOwnershipApi(ChatroomInnerFragment.OurRoomInfo.Name, false, true);
                    return true;
                case Resource.Id.search_room_action:
                    SearchTabHelper.SearchTarget = SearchTarget.Room;
                    SearchTabHelper.SearchTargetChosenRoom = ChatroomInnerFragment.OurRoomInfo.Name;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentSearchRoom, 1);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.hide_show_ticker_action:
                    ChatroomActivity.ShowTickerView = !ChatroomActivity.ShowTickerView;
                    var f = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment") as ChatroomInnerFragment;
                    if (ChatroomActivity.ShowTickerView)
                    {
                        f.currentTickerView.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        f.currentTickerView.Visibility = ViewStates.Gone;
                    }
                    lock (MainActivity.SHARED_PREF_LOCK)
                    {
                        var editor = SeekerState.SharedPreferences.Edit();
                        editor.PutBoolean(KeyConsts.M_ShowTickerView, ChatroomActivity.ShowTickerView);
                        editor.Commit();
                    }
                    return true;
                case Resource.Id.hide_show_user_status_action:
                    ChatroomActivity.ShowStatusesView = !ChatroomActivity.ShowStatusesView;
                    var f1 = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment") as ChatroomInnerFragment;
                    f1.SetStatusesView();
                    lock (MainActivity.SHARED_PREF_LOCK)
                    {
                        var editor = SeekerState.SharedPreferences.Edit();
                        editor.PutBoolean(KeyConsts.M_ShowStatusesView, ChatroomActivity.ShowStatusesView);
                        editor.Commit();
                    }
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void ShowAllTickersDialog(string roomName)
        {
            MainActivity.LogInfoFirebase("ShowAllTickersDialog" + this.IsFinishing + this.IsDestroyed + SupportFragmentManager.IsDestroyed);
            var tickerDialog = new AllTickersDialog(roomName);
            tickerDialog.Show(SupportFragmentManager, "ticker dialog");
        }

        public void ShowUserListDialog(Soulseek.RoomInfo roomInfo, bool isPrivate)
        {
            if (!ChatroomController.JoinedRoomData.ContainsKey(roomInfo.Name))
            {
                Toast.MakeText(this, this.Resources.GetString(Resource.String.room_data_still_loading), ToastLength.Short).Show();
                return;
            }
            var roomUserListDialog = new RoomUserListDialog(roomInfo.Name, isPrivate);
            roomUserListDialog.Show(SupportFragmentManager, "room user list dialog");
        }


        public void ShowInviteUserDialog(string roomToInvite)
        {
            MainActivity.LogInfoFirebase("ShowInviteUserDialog" + this.IsFinishing + this.IsDestroyed);
            //AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(); //failed to bind....
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme); //failed to bind....
            builder.SetTitle(this.Resources.GetString(Resource.String.invite_user));

            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.invite_user_dialog_content, (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView, false);

            AutoCompleteTextView input = (AutoCompleteTextView)viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.inviteUserTextEdit);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(input);

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string userToAdd = input.Text;
                if (userToAdd == null || userToAdd == string.Empty)
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, this.Resources.GetString(Resource.String.must_type_a_username_to_invite), ToastLength.Short).Show();
                    (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    return;
                }
                SeekerState.RecentUsersManager.AddUserToTop(userToAdd, true);
                ChatroomController.AddRemoveUserToPrivateRoomAPI(roomToInvite, userToAdd, true, false);
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    ChatroomActivity.dialogInstance.Dismiss();
                }
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    ChatroomActivity.dialogInstance.Dismiss();
                }
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Send ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search) //ImeNull if being called due to the enter key being pressed. (MSDN) but ImeNull gets called all the time....
                {
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(this.FindViewById(Android.Resource.Id.Content).RootView.WindowToken, 0);
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
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(this.FindViewById(Android.Resource.Id.Content).RootView.WindowToken, 0);
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
            input.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(this.Resources.GetString(Resource.String.invite), eventHandler);
            builder.SetNegativeButton(this.Resources.GetString(Resource.String.cancel), eventHandlerCancel);
            // Set up the buttons

            dialogInstance = builder.Create();
            try
            {
                dialogInstance.Show();
                CommonHelpers.DoNotEnablePositiveUntilText(dialogInstance, input);
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("invite WindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("invite WindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("invite Exception null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("invite Exception are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }

        }

        private void Input_KeyPress(object sender, View.KeyEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static System.String LocaleToEmoji(string locale)
        {
            if (locale == string.Empty)
            {
                int unicode = 0x1F310;
                return new System.String(Java.Lang.Character.ToChars(unicode));
            }
            int firstLetter = Java.Lang.Character.CodePointAt(locale, 0) - 0x41 + 0x1F1E6;
            int secondLetter = Java.Lang.Character.CodePointAt(locale, 1) - 0x41 + 0x1F1E6;
            return new System.String(Java.Lang.Character.ToChars(firstLetter)) + new System.String(Java.Lang.Character.ToChars(secondLetter));
        }




        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;

        public void ShowSetTickerDialog(string roomToInvite)
        {
            MainActivity.LogInfoFirebase("ShowSetTickerDialog" + this.IsFinishing + this.IsDestroyed);

            void OkayAction(object sender, string textInput)
            {
                ChatroomController.SetTickerApi(roomToInvite, textInput, true);
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    CommonHelpers._dialogInstance?.Dismiss(); // TODO why?
                }
            }

            CommonHelpers.ShowSimpleDialog(
                this,
                Resource.Layout.edit_text_dialog_content,
                this.Resources.GetString(Resource.String.set_ticker),
                OkayAction,
                this.Resources.GetString(Resource.String.send),
                null,
                this.Resources.GetString(Resource.String.type_chatroom_ticker_message),
                this.Resources.GetString(Resource.String.cancel),
                this.Resources.GetString(Resource.String.must_type_ticker_text),
                true);
        }



        public void ChangeToInnerFragment(Soulseek.RoomInfo roomInfo)
        {
            if (IsFinishing || IsDestroyed)
            {
                return;
            }
            else
            {
                //when you first click a room before you have joined, all the info you have is roomname and count. userlist is empty.
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(roomInfo), "ChatroomInnerFragment").AddToBackStack("ChatroomInnerFragmentBackStack").Commit();
            }
        }

        public void ShowEditCreateChatroomDialog()
        {
            MainActivity.LogInfoFirebase("ShowEditCreateChatroomDialog" + this.IsFinishing + this.IsDestroyed);
            //AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(); //failed to bind....
            Context c = this;
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(c, Resource.Style.MyAlertDialogTheme); //failed to bind....
            builder.SetTitle(this.Resources.GetString(Resource.String.create_chatroom_));

            View viewInflated = LayoutInflater.From(c).Inflate(Resource.Layout.create_chatroom_dialog, (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView, false);

            EditText chatNameInput = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.createChatroomName);
            CheckBox chatPrivateCheckBox = (CheckBox)viewInflated.FindViewById<CheckBox>(Resource.Id.createChatroomPrivate);

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string chatname = chatNameInput.Text;
                bool isPrivate = chatPrivateCheckBox.Checked;
                if (chatname == null || chatname == string.Empty)
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, this.Resources.GetString(Resource.String.must_type_chatroom_name), ToastLength.Short).Show();
                    (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    return;
                }

                ChatroomController.CreateRoomApi(chatname, isPrivate, true);

                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    dialogInstance.Dismiss();
                }
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    dialogInstance.Dismiss();
                }
            });

            //input.EditorAction += editorAction;
            chatNameInput.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(this.Resources.GetString(Resource.String.create_chatroom), eventHandler);
            builder.SetNegativeButton(this.Resources.GetString(Resource.String.cancel), eventHandlerCancel);
            // Set up the buttons

            dialogInstance = builder.Create();
            try
            {
                dialogInstance.Show();
                CommonHelpers.DoNotEnablePositiveUntilText(dialogInstance, chatNameInput);
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("createroomWindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("createroomWindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("createroomException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("createroomException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }

        }

        private void Input_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SeekerState.ActiveActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("createroomMainActivity_FocusChange" + err.Message);
            }
        }




    }


    public class UserStatusView : LinearLayout
    {
        public RecyclerView.ViewHolder ViewHolder { get; set; }
        private TextView viewUserStatus;

        public UserStatusView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.user_status_update_item, this, true);
            setupChildren();
        }
        public UserStatusView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.user_status_update_item, this, true);
            setupChildren();
        }

        public static UserStatusView inflate(ViewGroup parent)
        {
            UserStatusView itemView = (UserStatusView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.user_status_update_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUserStatus = FindViewById<TextView>(Resource.Id.userStatusMessage);
        }

        private void SetMessageText(TextView userStatus, ChatroomController.StatusMessageUpdate data)
        {
            string statusMessage = null;
            DateTime dateTimeLocal = data.DateTimeUtc.Add(SeekerState.OffsetFromUtcCached);
            string timePrefix = $"[{CommonHelpers.GetNiceDateTimeGroupChat(dateTimeLocal)}]";
            switch (data.StatusType)
            {
                case ChatroomController.StatusMessageType.Joined:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserJoined);
                    break;
                case ChatroomController.StatusMessageType.Left:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserLeft);
                    break;
                case ChatroomController.StatusMessageType.WentAway:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserWentAway);
                    break;
                case ChatroomController.StatusMessageType.CameBack:
                    statusMessage = "{0} {1} " + SeekerApplication.GetString(Resource.String.theUserCameBack);
                    break;
            }
            userStatus.Text = string.Format(statusMessage, timePrefix, data.Username);
        }

        public void setItem(ChatroomController.StatusMessageUpdate userStatusMessage)
        {
            SetMessageText(viewUserStatus, userStatusMessage);
            //string msgText = m.ChatroomText;
            //if (m.FromMe)
            //{
            //    msgText = "\u21AA" + msgText;
            //}
            //viewUsersInRoom.Text = msgText;
        }
    }


    public class UserStatusHolder : RecyclerView.ViewHolder
    {
        public UserStatusView userStatusInnerView;


        public UserStatusHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            userStatusInnerView = (UserStatusView)view;
            userStatusInnerView.ViewHolder = this;
            //(MessageOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public UserStatusView getUnderlyingView()
        {
            return userStatusInnerView;
        }
    }


    public class ChatroomStatusesRecyclerAdapter : RecyclerView.Adapter
    {
        private List<ChatroomController.StatusMessageUpdate> localDataSet;
        public override int ItemCount => localDataSet.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as UserStatusHolder).getUnderlyingView().setItem(localDataSet[position]);
            //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            UserStatusView view = UserStatusView.inflate(parent);
            view.setupChildren();
            // .inflate(R.layout.text_row_item, viewGroup, false);
            //(view as View).Click += MessageOverviewClick;
            return new UserStatusHolder(view as View);

        }

        public ChatroomStatusesRecyclerAdapter(List<ChatroomController.StatusMessageUpdate> ti)
        {
            localDataSet = ti;
        }
    }


    public class ChatroomInnerRecyclerAdapter : RecyclerView.Adapter
    {
        private List<Message> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public static int VIEW_SENT = 1;
        public static int VIEW_RECEIVER = 2;
        public static int VIEW_STATUS = 3;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (localDataSet[position].FromMe)
            {
                (holder as MessageInnerViewSentHolder).messageInnerView.setItem(localDataSet[position]);
            }
            else if (localDataSet[position].SpecialCode != 0)
            {
                (holder as MessageConnectionStatusHolder).messageInnerView.setItem(localDataSet[position]);
            }
            else
            {
                (holder as GroupMessageInnerViewReceivedHolder).messageInnerView.setItem(localDataSet[position]);
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
            if (localDataSet[position].FromMe)
            {
                return VIEW_SENT;
            }
            else if (localDataSet[position].SpecialCode != 0)
            {
                return VIEW_STATUS;
            }
            else
            {
                return VIEW_RECEIVER;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {
            if (viewType == VIEW_SENT)
            {
                MessageInnerViewSent view = MessageInnerViewSent.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).LongClick += ChatroomReceivedAdapter_LongClick;
                return new MessageInnerViewSentHolder(view as View);
            }
            else if (viewType == VIEW_RECEIVER)
            {
                GroupMessageInnerViewReceived view = GroupMessageInnerViewReceived.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).LongClick += ChatroomReceivedAdapter_LongClick;
                return new GroupMessageInnerViewReceivedHolder(view as View);
            }
            else// if(viewType == VIEW_STATUS)
            {
                MessageConnectionStatus view = MessageConnectionStatus.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += MessageOverviewClick;
                return new MessageConnectionStatusHolder(view as View);
            }

        }

        private void ChatroomReceivedAdapter_LongClick(object sender, View.LongClickEventArgs e)
        {
            if (sender is GroupMessageInnerViewReceived recv)
            {
                ChatroomInnerFragment.MessagesLongClickData = recv.DataItem;
            }
            else if (sender is MessageInnerViewSent sent)
            {
                ChatroomInnerFragment.MessagesLongClickData = sent.DataItem;
            }


            (sender as View).ShowContextMenu();
        }

        public ChatroomInnerRecyclerAdapter(List<Message> ti)
        {
            localDataSet = ti;
        }

    }


    public class GroupMessageInnerViewReceivedHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
    {
        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            MainActivity.LogDebug("ShowSlskLinkContextMenu " + CommonHelpers.ShowSlskLinkContextMenu);

            //if this is the slsk link menu then we are done, dont add anything extra. if failed to parse slsk link, then there will be no browse at location.
            //in that case we still dont want to show anything.
            if (menu.FindItem(SlskLinkMenuActivity.FromSlskLinkBrowseAtLocation) != null)
            {
                return;
            }
            else if (CommonHelpers.ShowSlskLinkContextMenu)
            {
                //closing wont turn this off since its invalid parse, so turn it off here...
                CommonHelpers.ShowSlskLinkContextMenu = false;
                return;
            }

            //its possible to get here without the AdapterLongClick depending on what part you hold down on the message.  I am not sure why...
            if (v is GroupMessageInnerViewReceived)
            {
                ChatroomInnerFragment.MessagesLongClickData = (v as GroupMessageInnerViewReceived).DataItem;
            }
            else
            {
                MainActivity.LogFirebase("sender for GroupMessageInnerViewReceivedHolder.GroupMessageInnerViewReceived is " + v.GetType().Name);
            }

            menu.Add(0, 0, 0, SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.copy_text));
            menu.Add(1, 1, 1, SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.ignore_user));
            CommonHelpers.AddAddRemoveUserMenuItem(menu, 2, 2, 2, ChatroomInnerFragment.MessagesLongClickData.Username);
            var subMenu = menu.AddSubMenu(3, 3, 3, SeekerState.ActiveActivityRef.GetString(Resource.String.more_options));
            subMenu.Add(4, 4, 4, Resource.String.search_user_files);
            subMenu.Add(5, 5, 5, Resource.String.browse_user);
            subMenu.Add(6, 6, 6, Resource.String.get_user_info);
            subMenu.Add(7, 7, 7, Resource.String.msg_user);
            //subMenu.Add(8,8,8,Resource.String.give_privileges);
            CommonHelpers.AddUserNoteMenuItem(subMenu, 8, 8, 8, ChatroomInnerFragment.MessagesLongClickData.Username);
        }

        public GroupMessageInnerViewReceived messageInnerView;


        public GroupMessageInnerViewReceivedHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            messageInnerView = (GroupMessageInnerViewReceived)view;
            messageInnerView.ViewHolder = this;
            view.SetOnCreateContextMenuListener(this); //otherwise no listener
        }

        public GroupMessageInnerViewReceived getUnderlyingView()
        {
            return messageInnerView;
        }


    }

    public class GroupMessageInnerViewReceived : ConstraintLayout
    {
        public GroupMessageInnerViewReceivedHolder ViewHolder { get; set; }
        private TextView viewTimeStamp;
        private TextView viewMessage;
        private TextView viewUsername;
        public Message DataItem;

        public GroupMessageInnerViewReceived(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.group_messages_inner_item_toMe, this, true);
            setupChildren();
        }
        public GroupMessageInnerViewReceived(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.group_messages_inner_item_toMe, this, true);
            setupChildren();
        }
        public static GroupMessageInnerViewReceived inflate(ViewGroup parent)
        {
            GroupMessageInnerViewReceived itemView = (GroupMessageInnerViewReceived)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.group_messages_inner_item_toMe_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewTimeStamp = FindViewById<TextView>(Resource.Id.text_gchat_timestamp_other);
            viewMessage = FindViewById<TextView>(Resource.Id.text_gchat_message_other);
            viewUsername = FindViewById<TextView>(Resource.Id.text_gchat_user_other);
        }

        public void setItem(Message msg)
        {
            DataItem = msg;
            viewTimeStamp.Text = CommonHelpers.GetNiceDateTimeGroupChat(msg.LocalDateTime);
            CommonHelpers.SetMessageTextView(viewMessage, msg);
            if (msg.SameAsLastUser)
            {
                viewUsername.Visibility = ViewStates.Gone;
                viewUsername.Text = string.Empty;
            }
            else
            {
                viewUsername.Visibility = ViewStates.Visible;
                viewUsername.Text = msg.Username;
            }

        }
    }

    public class RecyclerView2 : RecyclerView
    {
        public RecyclerView2(Context context) : base(context)
        {
        }

        public RecyclerView2(Context context, IAttributeSet attrSet) : base(context, attrSet)
        {

        }

        public RecyclerView2(Context context, IAttributeSet attrSet, int defStyle) : base(context, attrSet, defStyle)
        {

        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            if (oldh > h)
            {
                MainActivity.LogDebug("size changed to smaller...");
                this.GetLayoutManager().ScrollToPosition(this.GetLayoutManager().ItemCount - 1);
            }
        }
    }

    public class UserRoomStatusChangedEventArgs
    {
        public string User;
        public string RoomName;
        public ChatroomController.StatusMessageUpdate StatusMessageUpdate;
        public Soulseek.UserPresence Status;
        public UserRoomStatusChangedEventArgs(string roomName, string user, Soulseek.UserPresence status, ChatroomController.StatusMessageUpdate statusMessageUpdate)
        {
            User = user;
            RoomName = roomName;
            StatusMessageUpdate = statusMessageUpdate;
            Status = status;
        }
    }

    public class UserJoinedOrLeftEventArgs
    {
        public bool Joined;
        public string User;
        public string RoomName;
        public ChatroomController.StatusMessageUpdate? StatusMessageUpdate;
        public Soulseek.UserData UserData;
        public UserJoinedOrLeftEventArgs(string roomName, bool joined, string user, ChatroomController.StatusMessageUpdate? statusMessageUpdate = null, Soulseek.UserData uData = null, bool isOperator = false)
        {
            Joined = joined;
            User = user;
            RoomName = roomName;
            StatusMessageUpdate = statusMessageUpdate;
            UserData = uData;
        }
    }

    public class MessageReceivedArgs
    {
        public MessageReceivedArgs(string roomName, Message m)
        {
            RoomName = roomName;
            Message = m;
        }
        public MessageReceivedArgs(string roomName, bool fromUsPending, bool fromUsCon, Message m)
        {
            RoomName = roomName;
            FromUsPending = fromUsPending;
            FromUsConfirmation = fromUsCon;
            Message = m;
        }
        public string RoomName;
        public bool FromUsPending;
        public bool FromUsConfirmation;
        public Message Message;
    }


    public class ChatroomOverviewRecyclerAdapter : RecyclerView.Adapter
    {
        private List<Soulseek.RoomInfo> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public static int VIEW_CATEGORY_HEADER = 1;
        public static int VIEW_ROOM = 2;
        public static int VIEW_JOINED_ROOM = 3;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (localDataSet[position] is Soulseek.RoomInfoCategory)
            {
                (holder as ChatroomOverviewCategoryHolder).chatroomOverviewView.setItem(localDataSet[position]);
            }
            else
            {
                if (holder is ChatroomOverviewHolder chatOverview)
                {
                    chatOverview.chatroomOverviewView.setItem(localDataSet[position]);
                }
                else if (holder is ChatroomOverviewJoinedViewHolder chatJoinedViewHolder)
                {
                    chatJoinedViewHolder.chatroomOverviewView.setItem(localDataSet[position]);
                }
            }
            //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
        }

        public void notifyRoomStatusChanged(string roomName)
        {
            for (int i = 0; i < localDataSet.Count; i++)
            {
                if (localDataSet[i].Name == roomName)
                {
                    this.NotifyItemChanged(i);
                    MainActivity.LogDebug("NotifyItemChanged notifyRoomStatusChanged");
                    break;
                }
            }
        }

        public void notifyRoomStatusesChanged(List<string> rooms)
        {
            foreach (string roomName in rooms)
            {
                notifyRoomStatusChanged(roomName);
            }
        }

        public void setPosition(int position)
        {
            this.position = position;
        }

        public int getPosition()
        {
            return this.position;
        }

        //public override void OnViewRecycled(Java.Lang.Object holder)
        //{
        //    base.OnViewRecycled(holder);
        //}

        private void ChatroomOverviewClick(object sender, EventArgs e)
        {
            setPosition((sender as IChatroomOverviewBase).ViewHolder.AdapterPosition);
            ChatroomActivity.ChatroomActivityRef.ChangeToInnerFragment(localDataSet[position]);
        }

        public override int GetItemViewType(int position)
        {
            if (localDataSet[position] is Soulseek.RoomInfoCategory)
            {
                return VIEW_CATEGORY_HEADER;
            }
            else if (ChatroomController.JoinedRoomNames.Contains(localDataSet[position].Name))
            {
                return VIEW_JOINED_ROOM;
            }
            else
            {
                return VIEW_ROOM;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == VIEW_CATEGORY_HEADER)
            {
                ChatroomOverviewCategoryView view = ChatroomOverviewCategoryView.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += ChatroomOverviewClick;
                return new ChatroomOverviewCategoryHolder(view as View);
            }
            else if (viewType == VIEW_JOINED_ROOM)
            {
                ChatroomOverviewJoinedView view = ChatroomOverviewJoinedView.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).Click += ChatroomOverviewClick;
                view.FindViewById<ImageView>(Resource.Id.leaveRoom).Click += ChatroomOverviewRecyclerAdapter_Click;
                return new ChatroomOverviewJoinedViewHolder(view as View);
            }
            else
            {
                ChatroomOverviewView view = ChatroomOverviewView.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).Click += ChatroomOverviewClick;
                return new ChatroomOverviewHolder(view as View);
            }


        }

        private void ChatroomOverviewRecyclerAdapter_Click(object sender, EventArgs e)
        {
            setPosition(((sender as View).Parent.Parent as IChatroomOverviewBase).ViewHolder.AdapterPosition);
            string roomName = localDataSet[position].Name;
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //allow user to leave even if offline.
                //just remove it from list so that they do not rejoin when logging back in.
                ChatroomController.RemoveRoomFromJoinedAndOthers(roomName);
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.leaving_room), roomName), ToastLength.Short).Show();
                });
                ChatroomController.UpdateJoinedRooms();
            }
            else
            {
                ChatroomController.JoinRoomApi(roomName, false, true, true, false);
            }
        }

        public ChatroomOverviewRecyclerAdapter(List<Soulseek.RoomInfo> ti)
        {
            localDataSet = ti;
        }

    }

    public class MessageConnectionStatusHolder : RecyclerView.ViewHolder
    {
        public MessageConnectionStatus messageInnerView;


        public MessageConnectionStatusHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            messageInnerView = (MessageConnectionStatus)view;
            messageInnerView.ViewHolder = this;
            //(MessageOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public MessageConnectionStatus getUnderlyingView()
        {
            return messageInnerView;
        }
    }

    public class MessageConnectionStatus : LinearLayout
    {
        public MessageConnectionStatusHolder ViewHolder { get; set; }
        private TextView viewStatus;

        public MessageConnectionStatus(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_connect_disconnect_item, this, true);
            setupChildren();
        }
        public MessageConnectionStatus(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_connect_disconnect_item, this, true);
            setupChildren();
        }

        public static MessageConnectionStatus inflate(ViewGroup parent)
        {
            MessageConnectionStatus itemView = (MessageConnectionStatus)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_connect_disconnect_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewStatus = FindViewById<TextView>(Resource.Id.statusMsg);
        }

        public void setItem(Message msg)
        {
            viewStatus.Text = msg.MessageText;
        }
    }


    public class ChatroomOverviewHolder : RecyclerView.ViewHolder
    {
        public ChatroomOverviewView chatroomOverviewView;


        public ChatroomOverviewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            chatroomOverviewView = (ChatroomOverviewView)view;
            chatroomOverviewView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public ChatroomOverviewView getUnderlyingView()
        {
            return chatroomOverviewView;
        }
    }


    public class ChatroomOverviewCategoryHolder : RecyclerView.ViewHolder
    {
        public ChatroomOverviewCategoryView chatroomOverviewView;


        public ChatroomOverviewCategoryHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            chatroomOverviewView = (ChatroomOverviewCategoryView)view;
            chatroomOverviewView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public ChatroomOverviewCategoryView getUnderlyingView()
        {
            return chatroomOverviewView;
        }
    }

    public interface IChatroomOverviewBase
    {
        public void setItem(Soulseek.RoomInfo roomInfo);
        public RecyclerView.ViewHolder ViewHolder { get; set; }
    }

    public class ChatroomOverviewView : LinearLayout, IChatroomOverviewBase
    {
        public RecyclerView.ViewHolder ViewHolder { get; set; }
        private TextView viewRoomName;
        private TextView viewUsersInRoom;

        public ChatroomOverviewView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_item, this, true);
            setupChildren();
        }
        public ChatroomOverviewView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_item, this, true);
            setupChildren();
        }

        public static ChatroomOverviewView inflate(ViewGroup parent)
        {
            ChatroomOverviewView itemView = (ChatroomOverviewView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_overview_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewRoomName = FindViewById<TextView>(Resource.Id.roomName);
            viewUsersInRoom = FindViewById<TextView>(Resource.Id.usersInRoom);
        }

        public void setItem(Soulseek.RoomInfo roomInfo)
        {
            viewRoomName.Text = roomInfo.Name;
            viewUsersInRoom.Text = roomInfo.UserCount.ToString();
            //string msgText = m.ChatroomText;
            //if (m.FromMe)
            //{
            //    msgText = "\u21AA" + msgText;
            //}
            //viewUsersInRoom.Text = msgText;
        }
    }

    public class ChatroomOverviewJoinedViewHolder : RecyclerView.ViewHolder
    {
        public ChatroomOverviewJoinedView chatroomOverviewView;


        public ChatroomOverviewJoinedViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            chatroomOverviewView = (ChatroomOverviewJoinedView)view;
            chatroomOverviewView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public ChatroomOverviewJoinedView getUnderlyingView()
        {
            return chatroomOverviewView;
        }
    }

    public class ChatroomOverviewJoinedView : LinearLayout, IChatroomOverviewBase
    {
        public RecyclerView.ViewHolder ViewHolder { get; set; }
        private TextView viewRoomName;
        private TextView viewUsersInRoom;
        private ImageView unreadImageView;

        public ChatroomOverviewJoinedView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_joined_item, this, true);
            setupChildren();
        }
        public ChatroomOverviewJoinedView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_joined_item, this, true);
            setupChildren();
        }

        public static ChatroomOverviewJoinedView inflate(ViewGroup parent)
        {
            ChatroomOverviewJoinedView itemView = (ChatroomOverviewJoinedView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_overview_joined_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewRoomName = FindViewById<TextView>(Resource.Id.roomName);
            viewUsersInRoom = FindViewById<TextView>(Resource.Id.usersInRoom);
            unreadImageView = FindViewById<ImageView>(Resource.Id.unreadImageView);
        }

        public void setItem(Soulseek.RoomInfo roomInfo)
        {
            viewRoomName.Text = roomInfo.Name;
            viewUsersInRoom.Text = roomInfo.UserCount.ToString();


            if (ChatroomController.UnreadRooms.ContainsKey(roomInfo.Name))
            {
                unreadImageView.Visibility = ViewStates.Visible;

                viewRoomName.SetTypeface(null, TypefaceStyle.Bold);
                viewUsersInRoom.SetTypeface(null, TypefaceStyle.Bold);
                viewRoomName.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.normalTextColorNonTinted));
                viewUsersInRoom.SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.normalTextColorNonTinted));
            }
            else
            {
                unreadImageView.Visibility = ViewStates.Gone;

                // previously we did "viewRoomName.Typeface" instead of "null"
                // this had side effects due to reusing views. the bold would stay!
                viewRoomName.SetTypeface(null, TypefaceStyle.Normal);
                viewUsersInRoom.SetTypeface(null, TypefaceStyle.Normal);
                viewRoomName.SetTextColor(SeekerState.ActiveActivityRef.Resources.GetColor(Resource.Color.defaultTextColor));
                viewUsersInRoom.SetTextColor(SeekerState.ActiveActivityRef.Resources.GetColor(Resource.Color.defaultTextColor));
            }

            if (ChatroomController.CurrentlyJoinedRoomNames.ContainsKey(roomInfo.Name))
            {
                unreadImageView.Alpha = 1.0f;
                viewRoomName.Alpha = 1.0f;
                viewUsersInRoom.Alpha = 1.0f;
            }
            else
            {
                unreadImageView.Alpha = 0.4f;
                viewRoomName.Alpha = 0.4f;
                viewUsersInRoom.Alpha = 0.4f;
            }

            //string msgText = m.ChatroomText;
            //if (m.FromMe)
            //{
            //    msgText = "\u21AA" + msgText;
            //}
            //viewUsersInRoom.Text = msgText;
        }
    }


    public class ChatroomOverviewCategoryView : LinearLayout
    {
        public ChatroomOverviewCategoryHolder ViewHolder { get; set; }
        private TextView viewCategory;

        public ChatroomOverviewCategoryView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_category_item, this, true);
            setupChildren();
        }
        public ChatroomOverviewCategoryView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chatroom_overview_category_item, this, true);
            setupChildren();
        }

        public static ChatroomOverviewCategoryView inflate(ViewGroup parent)
        {
            ChatroomOverviewCategoryView itemView = (ChatroomOverviewCategoryView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chatroom_overview_category_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewCategory = FindViewById<TextView>(Resource.Id.categoryHeader);
        }

        public void setItem(UserListItem usertype)
        {
            viewCategory.Text = usertype.Username;
            //string msgText = m.ChatroomText;
            //if (m.FromMe)
            //{
            //    msgText = "\u21AA" + msgText;
            //}
            //viewUsersInRoom.Text = msgText;
        }

        public void setItem(Soulseek.RoomInfo roomInfo)
        {
            viewCategory.Text = roomInfo.Name;
            //string msgText = m.ChatroomText;
            //if (m.FromMe)
            //{
            //    msgText = "\u21AA" + msgText;
            //}
            //viewUsersInRoom.Text = msgText;
        }
    }




}
