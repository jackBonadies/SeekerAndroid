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
using Javax.Security.Auth;
using AndroidX.Activity;
using Common;
using Common.Messages;

namespace Seeker
{

    [Activity(Label = "ChatroomActivity", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask, Exported = false)]
    public class ChatroomActivity : SlskLinkMenuActivity//, Android.Widget.PopupMenu.IOnMenuItemClickListener
    {
        public static ChatroomActivity ChatroomActivityRef = null;

        public static bool ShowStatusesView
        {
            get => Common.PreferencesState.ShowStatusesView;
            set => Common.PreferencesState.ShowStatusesView = value;
        }
        public static bool ShowTickerView
        {
            get => Common.PreferencesState.ShowTickerView;
            set => Common.PreferencesState.ShowTickerView = value;
        }
        public static bool ShowUserOnlineAwayStatusUpdates = true;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Logger.Debug("chatroom activity on create");
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

            var backPressedCallback = new GenericOnBackPressedCallback(true, onBackPressedAction);
            OnBackPressedDispatcher.AddCallback(backPressedCallback);

            if (savedInstanceState != null && savedInstanceState.GetBoolean("SaveStateAtChatroomInner"))
            {
                Logger.Debug("restoring chatroom inner fragment");
                if (ChatroomInnerFragment.OurRoomInfo == null)
                {
                    Logger.Debug("ourroominfo is null");
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
                        Logger.Firebase("empty goToUsersMessages");
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


        private void onBackPressedAction(OnBackPressedCallback callback)
        {
            //if f is non null and f is visible then that means you are backing out from the inner user fragment..
            var f = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment");
            if (f != null && f.IsVisible)
            {
                if (SupportFragmentManager.BackStackEntryCount == 0) //this is if we got to inner messages through a notification, in which case we are done..
                {
                    callback.Enabled = false;
                    OnBackPressedDispatcher.OnBackPressed();
                    callback.Enabled = true;
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
            callback.Enabled = false;
            OnBackPressedDispatcher.OnBackPressed();
            callback.Enabled = true;
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
                    Logger.Firebase("empty goToRoom");
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
                Logger.Firebase("no restore info...");
                return;
            }
            Logger.Firebase("restoring info...");
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
                Logger.Debug("SaveStateAtChatroomInner OnSaveInstanceState");
                SaveStartingRoomInfo(outState, f as ChatroomInnerFragment);
                Logger.Debug("currentlyInsideRoomName -- OnSaveInstanceState -- " + ChatroomController.currentlyInsideRoomName);
                //ChatroomController.currentlyInsideRoomName = ChatroomInnerFragment.OurRoomInfo.Name; //this sets it after we are leaving....
            }
            else
            {
                outState.PutBoolean("SaveStateAtChatroomInner", false);
            }
            base.OnSaveInstanceState(outState);
        }




        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            Logger.Debug("on create options menu");
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
            Logger.Debug("on prepare options menu");
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
                Logger.Debug("on prepare options menu INNER");
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
                Logger.Debug("isPrivate: " + isPrivate + "isOwnedByUs: " + isOwnedByUs + "isOperator: " + isOperator);
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
                    Message m = new Message("test", 1, false, SimpleHelpers.GetDateTimeNowSafe(), DateTime.UtcNow, "test" + i, false);
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
                    this.OnBackPressedDispatcher.OnBackPressed();
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
                    PreferencesManager.SaveShowTickerView();
                    return true;
                case Resource.Id.hide_show_user_status_action:
                    ChatroomActivity.ShowStatusesView = !ChatroomActivity.ShowStatusesView;
                    var f1 = SupportFragmentManager.FindFragmentByTag("ChatroomInnerFragment") as ChatroomInnerFragment;
                    f1.SetStatusesView();
                    PreferencesManager.SaveShowStatusesView();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void ShowAllTickersDialog(string roomName)
        {
            Logger.InfoFirebase("ShowAllTickersDialog" + this.IsFinishing + this.IsDestroyed + SupportFragmentManager.IsDestroyed);
            var tickerDialog = new AllTickersDialog(roomName);
            tickerDialog.Show(SupportFragmentManager, "ticker dialog");
        }

        public void ShowUserListDialog(Soulseek.RoomInfo roomInfo, bool isPrivate)
        {
            if (!ChatroomController.JoinedRoomData.ContainsKey(roomInfo.Name))
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.room_data_still_loading), ToastLength.Short);
                return;
            }
            var roomUserListDialog = new RoomUserListDialog(roomInfo.Name, isPrivate);
            roomUserListDialog.Show(SupportFragmentManager, "room user list dialog");
        }


        public void ShowInviteUserDialog(string roomToInvite)
        {
            Logger.InfoFirebase("ShowInviteUserDialog" + this.IsFinishing + this.IsDestroyed);
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
            builder.SetTitle(this.Resources.GetString(Resource.String.inviteuser));

            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.autocomplete_user_dialog_content, (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView, false);

            AutoCompleteTextView input = (AutoCompleteTextView)viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserEditText);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(input);

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string userToAdd = input.Text;
                if (userToAdd == null || userToAdd == string.Empty)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_type_a_username_to_invite), ToastLength.Short);
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
                    Logger.Debug("IME ACTION: " + e.ActionId.ToString());
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
                        imm.HideSoftInputFromWindow(this.FindViewById(Android.Resource.Id.Content).RootView.WindowToken, 0);
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
            input.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(this.Resources.GetString(Resource.String.invite), eventHandler);
            builder.SetNegativeButton(this.Resources.GetString(Resource.String.cancel), eventHandlerCancel);
            // Set up the buttons

            dialogInstance = builder.Create();
            try
            {
                dialogInstance.Show();
                UiHelpers.DoNotEnablePositiveUntilText(dialogInstance, input);
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("invite WindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    Logger.Firebase("invite WindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("invite Exception null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    Logger.Firebase("invite Exception are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
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
            Logger.InfoFirebase("ShowSetTickerDialog" + this.IsFinishing + this.IsDestroyed);

            void OkayAction(object sender, string textInput)
            {
                ChatroomController.SetTickerApi(roomToInvite, textInput, true);
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    UiHelpers._dialogInstance?.Dismiss(); // TODO why?
                }
            }

            UiHelpers.ShowSimpleDialog(
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
            Logger.InfoFirebase("ShowEditCreateChatroomDialog" + this.IsFinishing + this.IsDestroyed);
            //AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(); //failed to bind....
            Context c = this;
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(c); //failed to bind....
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
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_type_chatroom_name), ToastLength.Short);
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
                UiHelpers.DoNotEnablePositiveUntilText(dialogInstance, chatNameInput);
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("createroomWindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    Logger.Firebase("createroomWindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("createroomException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    Logger.Firebase("createroomException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
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
                Logger.Firebase("createroomMainActivity_FocusChange" + err.Message);
            }
        }




    }


}
