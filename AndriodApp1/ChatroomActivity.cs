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
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace AndriodApp1
{

    [Activity(Label = "ChatroomActivity", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]
    public class ChatroomActivity : Android.Support.V7.App.AppCompatActivity//, Android.Widget.PopupMenu.IOnMenuItemClickListener
    {
        public static ChatroomActivity ChatroomActivityRef = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            MainActivity.LogDebug("chatroom activity on create");
            base.OnCreate(savedInstanceState);

            ChatroomActivityRef = this;
            SoulSeekState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.chatroom_main_layout);


            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.chatroom_toolbar);
            myToolbar.InflateMenu(Resource.Menu.chatroom_overview_list_menu);
            myToolbar.Title = this.Resources.GetString(Resource.String.chatrooms);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);
            //this.SupportActionBar.SetDisplayShowHomeEnabled(true);

            //if (MessageController.RootMessages == null)
            //{
            //    var sharedPref = this.GetSharedPreferences("SoulSeekPrefs", 0);
            //    MessageController.RestoreMessagesFromSharedPrefs(sharedPref);
            //    if (SoulSeekState.Username != null && SoulSeekState.Username != string.Empty)
            //    {
            //        MessageController.MessagesUsername = SoulSeekState.Username;
            //        if (!MessageController.RootMessages.ContainsKey(SoulSeekState.Username))
            //        {
            //            MessageController.RootMessages[SoulSeekState.Username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
            //        }
            //        else
            //        {
            //            MessageController.Messages = MessageController.RootMessages[SoulSeekState.Username];
            //        }
            //    }
            //}
            //else if (SoulSeekState.Username != MessageController.MessagesUsername)
            //{
            //    MessageController.MessagesUsername = SoulSeekState.Username;
            //    if (SoulSeekState.Username == null || SoulSeekState.Username == string.Empty)
            //    {
            //        MessageController.Messages = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
            //    }
            //    else
            //    {
            //        if (MessageController.RootMessages.ContainsKey(SoulSeekState.Username))
            //        {
            //            MessageController.Messages = MessageController.RootMessages[SoulSeekState.Username];
            //        }
            //        else
            //        {
            //            MessageController.RootMessages[SoulSeekState.Username] = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Message>>();
            //            MessageController.Messages = MessageController.RootMessages[SoulSeekState.Username];
            //        }
            //    }
            //}
            bool startWithUserFragment = false;
            
            if (savedInstanceState != null && savedInstanceState.GetBoolean("SaveStateAtChatroomInner"))
            {
                MainActivity.LogDebug("restoring chatroom inner fragment");
                if(ChatroomInnerFragment.OurRoomInfo==null)
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
                        Soulseek.RoomInfo roomInfo = ChatroomController.RoomListParsed.FirstOrDefault((roomInfo)=>{return roomInfo.Name == goToRoom; });
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
                    Soulseek.RoomInfo roomInfo = ChatroomController.RoomListParsed.FirstOrDefault((roomInfo) =>{return roomInfo.Name == goToRoom; });
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
            if(ChatroomInnerFragment.OurRoomInfo!=null)
            {
                outState.PutString(INNER_ROOM_NAME_CONST, ChatroomInnerFragment.OurRoomInfo.Name);
                outState.PutInt(INNER_ROOM_COUNT_CONST, ChatroomInnerFragment.OurRoomInfo.UserCount);
            }
            if(f!=null&&ChatroomController.RoomList!=null)
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
            if(rName==string.Empty)
            {
                MainActivity.LogFirebase("no restore info...");
                return;
            }
            MainActivity.LogFirebase("restoring info...");
            ChatroomInnerFragment.OurRoomInfo = new Soulseek.RoomInfo(rName,rCount);
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
                ChatroomController.currentlyInsideRoomName = ChatroomInnerFragment.OurRoomInfo.Name;
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
                Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.chatroom_toolbar);
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
            if(fOuter!=null)
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
                if (ChatroomController.RoomList!=null)
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

            }
            return true;
        }


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
                    ChatroomController.ToggleNotifyRoom(ChatroomInnerFragment.OurRoomInfo.Name, true, this);
                    return true;
                case Resource.Id.view_user_list_action:
                    ShowUserListDialog(ChatroomInnerFragment.OurRoomInfo, ChatroomController.IsPrivate(ChatroomInnerFragment.OurRoomInfo));
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
                    Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentSearchRoom, 1);
                    this.StartActivity(intent);
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void ShowAllTickersDialog(string roomName)
        {
            MainActivity.LogInfoFirebase("ShowAllTickersDialog" + this.IsFinishing + this.IsDestroyed + SupportFragmentManager.IsDestroyed);
            var tickerDialog = new AllTickersDialog(roomName);
            tickerDialog.Show(SupportFragmentManager,"ticker dialog");
        }

        public void ShowUserListDialog(Soulseek.RoomInfo roomInfo, bool isPrivate)
        {
            if(!ChatroomController.JoinedRoomData.ContainsKey(roomInfo.Name))
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
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this); //failed to bind....
            builder.SetTitle(this.Resources.GetString(Resource.String.invite_user));
            // I'm using fragment here so I'm using getView() to provide ViewGroup
            // but you can provide here any other instance of ViewGroup from your Fragment / Activity
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.invite_user_dialog_content, (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView, false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.inviteUserTextEdit);

            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string userToAdd = input.Text;
                if (userToAdd == null || userToAdd == string.Empty)
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, this.Resources.GetString(Resource.String.must_type_a_username_to_invite), ToastLength.Short).Show();
                    (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    return;
                }
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
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
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

            input.EditorAction += editorAction;
            input.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(this.Resources.GetString(Resource.String.invite), eventHandler);
            builder.SetNegativeButton(this.Resources.GetString(Resource.String.cancel), eventHandlerCancel);
            // Set up the buttons

            dialogInstance = builder.Create();
            try
            {
                dialogInstance.Show();
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SoulSeekState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("invite WindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SoulSeekState.MainActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("invite WindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SoulSeekState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("invite Exception null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SoulSeekState.MainActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("invite Exception are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }

        }


        public static System.String LocaleToEmoji(string locale)
        {
            if(locale==string.Empty)
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
            //AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(); //failed to bind....
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this); //failed to bind....
            builder.SetTitle(this.Resources.GetString(Resource.String.set_ticker));
            // I'm using fragment here so I'm using getView() to provide ViewGroup
            // but you can provide here any other instance of ViewGroup from your Fragment / Activity
            View viewInflated = LayoutInflater.From(this).Inflate(Resource.Layout.set_ticker_dialog_content, (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView, false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.setTickerEditText);

            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string tickerText = input.Text;
                if (tickerText == null || tickerText == string.Empty)
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, this.Resources.GetString(Resource.String.must_type_ticker_text), ToastLength.Short);
                    (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    return;
                }
                ChatroomController.SetTickerApi(roomToInvite, tickerText, true);
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
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
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

            input.EditorAction += editorAction;
            input.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(this.Resources.GetString(Resource.String.send), eventHandler);
            builder.SetNegativeButton(this.Resources.GetString(Resource.String.cancel), eventHandlerCancel);
            // Set up the buttons

            dialogInstance = builder.Create();
            try
            {
                dialogInstance.Show();
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SoulSeekState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("ticker WindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SoulSeekState.MainActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("ticker WindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SoulSeekState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("tickerException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SoulSeekState.MainActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("tickerException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }

        }



        public void ChangeToInnerFragment(Soulseek.RoomInfo roomInfo)
        {
            if(IsFinishing || IsDestroyed)
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
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(c); //failed to bind....
            builder.SetTitle(this.Resources.GetString(Resource.String.create_chatroom_));
            // I'm using fragment here so I'm using getView() to provide ViewGroup
            // but you can provide here any other instance of ViewGroup from your Fragment / Activity
            View viewInflated = LayoutInflater.From(c).Inflate(Resource.Layout.create_chatroom_dialog, (ViewGroup)this.FindViewById(Android.Resource.Id.Content).RootView, false);
            // Set up the input
            EditText chatNameInput = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.createChatroomName);
            CheckBox chatPrivateCheckBox = (CheckBox)viewInflated.FindViewById<CheckBox>(Resource.Id.createChatroomPrivate);

            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string chatname = chatNameInput.Text;
                bool isPrivate = chatPrivateCheckBox.Checked;
                if (chatname == null || chatname == string.Empty)
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, this.Resources.GetString(Resource.String.must_type_chatroom_name), ToastLength.Short).Show();
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

            //System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            //{
            //    if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
            //        e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
            //        e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
            //        e.ActionId == Android.Views.InputMethods.ImeAction.Search) //ImeNull if being called due to the enter key being pressed. (MSDN) but ImeNull gets called all the time....
            //    {
            //        MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
            //        //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
            //        //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
            //        //overriding this, the keyboard fails to go down by default for some reason.....
            //        try
            //        {
            //            Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
            //            imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
            //        }
            //        catch (System.Exception ex)
            //        {
            //        }
            //        //Do the Browse Logic...
            //        eventHandler(sender, null);
            //    }
            //};

            //input.EditorAction += editorAction;
            chatNameInput.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(this.Resources.GetString(Resource.String.create_chatroom), eventHandler);
            builder.SetNegativeButton(this.Resources.GetString(Resource.String.cancel), eventHandlerCancel);
            // Set up the buttons

            dialogInstance = builder.Create();
            try
            {
                dialogInstance.Show();
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SoulSeekState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("createroomWindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SoulSeekState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("createroomWindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SoulSeekState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("createroomException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SoulSeekState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.IsFinishing;
                    MainActivity.LogFirebase("createroomException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }

        }

        private void Input_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SoulSeekState.MainActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("createroomMainActivity_FocusChange" + err.Message);
            }
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
            else if(localDataSet[position].SpecialCode!=0)
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

        //public override void OnViewRecycled(Java.Lang.Object holder)
        //{
        //    base.OnViewRecycled(holder);
        //}

        //private void MessageOverviewClick(object sender, EventArgs e)
        //{
        //    //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
        //    //pop.Inflate(Resource.Menu.download_diag_options);
        //    //pop.Show();
        //    setPosition((sender as MessageOverviewView).ViewHolder.AdapterPosition);
        //    MessagesActivity.ChangeFragment((sender as MessageOverviewView).View);
        //}
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
                //(view as View).Click += MessageOverviewClick;
                return new MessageInnerViewSentHolder(view as View);
            }
            else if(viewType == VIEW_RECEIVER)
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
            //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
            //pop.Inflate(Resource.Menu.download_diag_options);
            //pop.Show();
            //setPosition((sender as RoomUserItemView).ViewHolder.AdapterPosition);

            ChatroomInnerFragment.MessagesLongClickData = (sender as GroupMessageInnerViewReceived).DataItem;

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
            menu.Add(0, 0, 0, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.copy_text));
            menu.Add(1, 1, 1, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.ignore_user));
            menu.Add(2, 2, 2, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.add_user));
        }

        public GroupMessageInnerViewReceived messageInnerView;


        public GroupMessageInnerViewReceivedHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            messageInnerView = (GroupMessageInnerViewReceived)view;
            messageInnerView.ViewHolder = this;
            view.SetOnCreateContextMenuListener(this);
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
            viewTimeStamp.Text = Helpers.GetNiceDateTimeGroupChat(msg.LocalDateTime);
            viewMessage.Text = msg.MessageText;
            if(msg.SameAsLastUser)
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





    public class ChatroomInnerFragment : Android.Support.V4.App.Fragment //,PopupMenu.IOnMenuItemClickListener
    {
        private RecyclerView recyclerViewInner;
        private LinearLayoutManager recycleLayoutManager;
        private ChatroomInnerRecyclerAdapter recyclerAdapter;
        private List<Message> messagesInternal = null;
        //private string currentTickerText = string.Empty;
        private bool created = false;
        private View rootView = null;
        private EditText editTextEnterMessage = null;
        private Button sendMessage = null;
        private TextView currentTickerView = null;
        public static Soulseek.RoomInfo OurRoomInfo = null;

        //these are for if we get killed by system on the chatroom inner fragment and we do not yet have the room list.
        public static bool cachedPrivate = false;
        public static bool cachedOwned = false;
        public static bool cachedMod = false;

        public bool IsPrivate()
        {
            return ChatroomController.IsPrivate(OurRoomInfo);
        }
        public bool IsOwned()
        {
            return ChatroomController.IsOwnedByUs(OurRoomInfo);
        }
        public bool IsAutoJoin()
        {
            return ChatroomController.IsAutoJoinOn(OurRoomInfo);
        }
        public bool IsNotifyOn()
        {
            return ChatroomController.IsNotifyOn(OurRoomInfo);
        }
        public bool IsOperatedByUs()
        {
            if(ChatroomController.ModeratedRoomData.ContainsKey(OurRoomInfo.Name))
            {
                return ChatroomController.ModeratedRoomData[OurRoomInfo.Name].Users.Contains(SoulSeekState.Username);
            }
            return false;
        }

        public ChatroomInnerFragment() : base()
        {
            MainActivity.LogDebug("Chatroom Inner Fragment DEFAULT Constructor");
        }

        public ChatroomInnerFragment(Soulseek.RoomInfo roomInfo) : base()
        {
            MainActivity.LogDebug("Chatroom Inner Fragment ROOMINFO Constructor");

            OurRoomInfo = roomInfo;




        }

        public void HookUpEventHandlers(bool binding)
        {
            ChatroomController.MessageReceived -= OnMessageRecieved;
            ChatroomController.RoomMembershipRemoved -= OnRoomMembershipRemoved;
            ChatroomController.UserJoinedOrLeft -= OnUserJoinedOrLeft;
            ChatroomController.RoomTickerListReceived -= OnRoomTickerListReceived;
            ChatroomController.RoomTickerAdded -= OnRoomTickerAdded;
            if (binding)
            {
                ChatroomController.MessageReceived += OnMessageRecieved;
                ChatroomController.UserJoinedOrLeft += OnUserJoinedOrLeft;
                ChatroomController.RoomTickerListReceived += OnRoomTickerListReceived;
                ChatroomController.RoomTickerAdded += OnRoomTickerAdded;
                ChatroomController.RoomMembershipRemoved += OnRoomMembershipRemoved;

            }
        }

        public void OnRoomTickerAdded(object sender, Soulseek.RoomTickerAddedEventArgs e)
        {
            if (OurRoomInfo != null && OurRoomInfo.Name == e.RoomName)
            {
                this.Activity?.RunOnUiThread(new Action(() => {
                    this.SetTickerMessage(e.Ticker);
                }));
            }
        }


        public void OnRoomTickerListReceived(object sender, Soulseek.RoomTickerListReceivedEventArgs e)
        {
            //this is the first room ticker event you get...
            //nothing to do UNLESS we are not showing any tickers currently.. also make sure its our room..
            if (OurRoomInfo != null && OurRoomInfo.Name == e.RoomName)
            {
                this.Activity?.RunOnUiThread(new Action(() => {
                    if(e.TickerCount==0)
                    {
                        this.SetTickerMessage(new Soulseek.RoomTicker("",this.Resources.GetString(Resource.String.no_room_tickers)));
                    }
                    else
                    {
                        this.SetTickerMessage(e.Tickers.Last());
                    }
                }));
            }
        }
        public static Message MessagesLongClickData = null;
        public override bool OnContextItemSelected(IMenuItem item)
        {
            //MainActivity.LogDebug(MessagesLongClickData.MessageText + MessagesLongClickData.Username);
            string username = MessagesLongClickData.Username;
            switch (item.ItemId)
            {
                case 0: //"Copy Text"
                    Helpers.CopyTextToClipboard(this.Activity, MessagesLongClickData.MessageText);
                    break;
                case 1: //"Ignore User"
                    SeekerApplication.AddToIgnoreListFeedback(this.Activity, username);
                    break;
                case 2://"Add User"
                    UserListActivity.AddUserAPI(SoulSeekState.MainActivityRef, username, null);
                    break;

            }
            return base.OnContextItemSelected(item);
        }

        public void OnMessageRecieved(object sender, MessageReceivedArgs roomArgs)
        {
            if(OurRoomInfo!=null && OurRoomInfo.Name == roomArgs.RoomName)
            {
                this.Activity?.RunOnUiThread(new Action(() => {

                    if(roomArgs.FromUsConfirmation) //special case, the message is already there we just need to update it
                    {
                        recyclerAdapter.NotifyItemChanged(messagesInternal.Count - 1);
                    }
                    else
                    {
                        Message msg = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].Last();
                        messagesInternal.Add(msg);
                        int lastVisibleItemPosition = recycleLayoutManager.FindLastVisibleItemPosition();
                        MainActivity.LogDebug("lastVisibleItemPosition : " + lastVisibleItemPosition);
                        recyclerAdapter.NotifyItemInserted(messagesInternal.Count - 1);

                        if (lastVisibleItemPosition >= messagesInternal.Count - 2) //since its based on the old list index so -1 -1
                        {
                            if (messagesInternal.Count != 0)
                            {
                                recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                            }
                        }
                    }

                    //above is the new "refresh incremental" method

                    //this is the old "refresh everything" method
                    //messagesInternal = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].ToList();
                    //recyclerAdapter = new ChatroomInnerRecyclerAdapter(messagesInternal);
                    //recyclerViewInner.SetAdapter(recyclerAdapter);
                    //recyclerAdapter.NotifyDataSetChanged(); 
                    //if (messagesInternal.Count != 0) TEMP
                    //{
                    //    recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                    //}
                }));
            }
        }

        public void OnRoomMembershipRemoved(object sender, string room)
        {
            MainActivity.LogDebug("handler remove from " + room);



            if (OurRoomInfo != null && OurRoomInfo.Name == room)
            {
                this.Activity?.RunOnUiThread(new Action(() => {
                    if(this.IsVisible)
                    {
                        MainActivity.LogDebug("pressed back from " + room);
                        this.Activity.OnBackPressed();
                    }
                    ChatroomController.GetRoomListApi();
                }));
            }
        }

        public void OnUserJoinedOrLeft(object sender, UserJoinedOrLeftEventArgs e)
        {
            //nothing to do UNLESS you are planning on showing something live.
            //maybe if you have a number counter, then its useful..
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MainActivity.LogDebug("Chatroom Inner Fragment OnCreateView");

            //if (Username == null)
            //{
            //    Username = savedInstanceState.GetString("Inner_Username_ToMessage");
            //}

            rootView = inflater.Inflate(Resource.Layout.chatroom_inner_layout, container, false);
            currentTickerView = rootView.FindViewById<TextView>(Resource.Id.current_ticker);
            currentTickerView.Click += CurrentTickerView_Click;
            editTextEnterMessage = rootView.FindViewById<EditText>(Resource.Id.edit_gchat_message);
            sendMessage = rootView.FindViewById<Button>(Resource.Id.button_gchat_send);

            Soulseek.RoomData roomData = null;
            if (ChatroomController.HasRoomData(OurRoomInfo.Name))
            {
                MainActivity.LogDebug("we have the room data");
                roomData = ChatroomController.GetRoomData(OurRoomInfo.Name);
            }
            else
            {
                MainActivity.LogDebug("joining room " + OurRoomInfo.Name);
                if(SoulSeekState.currentlyLoggedIn)
                {
                    ChatroomController.JoinRoomApi(OurRoomInfo.Name, true, true, false, false);
                }
                else
                {
                    MainActivity.LogDebug("not logged in, on log in we will join");
                }
            }

            if (ChatroomController.JoinedRoomMessages.ContainsKey(OurRoomInfo.Name))
            {
                MainActivity.LogDebug("we have the room messages");
                messagesInternal = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].ToList();
            }
            else
            {
                messagesInternal = new List<Message>();
            }

            if (ChatroomController.JoinedRoomTickers.ContainsKey(OurRoomInfo.Name) && ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Count>0)
            {
                MainActivity.LogDebug("we have the room tickers");
                var ticker = ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Last();
                SetTickerMessage(ticker);
            }
            else if(ChatroomController.JoinedRoomTickers.ContainsKey(OurRoomInfo.Name) && ChatroomController.JoinedRoomTickers[OurRoomInfo.Name].Count == 0)
            {
                MainActivity.LogDebug("no tickers yet");
                SetTickerMessage(new Soulseek.RoomTicker("", this.Resources.GetString(Resource.String.no_room_tickers)));
            }
            else
            {
                currentTickerView.Text = this.Resources.GetString(Resource.String.loading_current_ticker);
            }



            if (editTextEnterMessage.Text == null || editTextEnterMessage.Text.ToString() == string.Empty)
            {
                sendMessage.Enabled = false;
            }
            else
            {
                sendMessage.Enabled = true;
            }
            editTextEnterMessage.TextChanged += EditTextEnterMessage_TextChanged;
            editTextEnterMessage.EditorAction += EditTextEnterMessage_EditorAction;
            sendMessage.Click += SendMessage_Click;

            //TextView noMessagesView = rootView.FindViewById<TextView>(Resource.Id.noMessagesView);
            recyclerViewInner = rootView.FindViewById<RecyclerView>(Resource.Id.recycler_messages);
            //recyclerViewInner.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleLayoutManager = new LinearLayoutManager(Activity);
            recycleLayoutManager.StackFromEnd = true;
            recycleLayoutManager.ReverseLayout = false;
            recyclerAdapter = new ChatroomInnerRecyclerAdapter(messagesInternal); //this depends tightly on MessageController... since these are just strings..
            recyclerViewInner.SetAdapter(recyclerAdapter);
            recyclerViewInner.SetLayoutManager(recycleLayoutManager);
            this.RegisterForContextMenu(recyclerViewInner);
            if (messagesInternal.Count != 0)
            {
                recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
            }

            ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name;
            HookUpEventHandlers(true);
            created=true;

            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)ChatroomActivity.ChatroomActivityRef.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.chatroom_toolbar);
            myToolbar.InflateMenu(Resource.Menu.chatroom_inner_menu);
            myToolbar.Title = OurRoomInfo.Name;
            ChatroomActivity.ChatroomActivityRef.SetSupportActionBar(myToolbar);
            ChatroomActivity.ChatroomActivityRef.InvalidateOptionsMenu();
            return rootView;

        }

        private void CurrentTickerView_Click(object sender, EventArgs e)
        {
            TextView tickerView = (sender as TextView);
            if (tickerView.MaxLines == 2)
            {
                tickerView.SetMaxLines(int.MaxValue);
            }
            else
            {
                tickerView.SetMaxLines(2);
            }
        }

        //public static Android.Text.ISpanned FormatTickerHTML(Soulseek.RoomTicker t)
        //{
        //    return AndroidX.Core.Text.HtmlCompat.FromHtml(@"<font color=#cc0029> </font>", AndroidX.Core.Text.HtmlCompat.FromHtmlModeLegacy);
        //}

        private void SetTickerMessage(Soulseek.RoomTicker t)
        {
            if(t!=null && currentTickerView!=null)
            {
                if(t.Username==string.Empty)
                {
                    //for the no tickers msg
                    currentTickerView.Text = t.Message;
                }
                else
                {
                    currentTickerView.Text = t.Message + " --" + t.Username;
                }
            }
            else
            {
                //this is if we arent there anymore...... but shouldnt we have unbound?? or if it simply comes in too fast....
                if(t==null)
                {
                    MainActivity.LogDebug("null ticker");
                }
                else
                {
                    MainActivity.LogDebug("null ticker view");
                }
            }
        }

        private void EditTextEnterMessage_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Send)
            {
                //send the message and record our send message..
                SendChatroomMessageAPI(OurRoomInfo.Name,new Message(SoulSeekState.Username, -1, false, DateTime.Now, DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

                editTextEnterMessage.Text = string.Empty;
            }
        }

        public override void OnAttach(Context activity)
        {
            
            if (created) //attach can happen before we created our view...
            {
                try
                {
                ChatroomController.currentlyInsideRoomName = OurRoomInfo.Name; //nullref
                }
                catch(Exception e)
                {
                    MainActivity.LogDebug("1" + e.Message);
                }
                try
                {
                messagesInternal = ChatroomController.JoinedRoomMessages[OurRoomInfo.Name].ToList();
                recyclerAdapter = new ChatroomInnerRecyclerAdapter(messagesInternal); //this depends tightly on MessageController... since these are just strings..
                recyclerViewInner.SetAdapter(recyclerAdapter);
                if (messagesInternal.Count != 0)
                {
                    recyclerViewInner.ScrollToPosition(messagesInternal.Count - 1);
                }
                }
                catch(Exception e)
                {
                    MainActivity.LogDebug("2" + e.Message);
                }
                HookUpEventHandlers(true);
            }
            base.OnAttach(activity);
        }

        public override void OnDetach()
        {
            ChatroomController.currentlyInsideRoomName = string.Empty;
            HookUpEventHandlers(false);
            base.OnDetach();
        }



        public void SendChatroomMessageAPI(string roomName, Message msg)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, this.Resources.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show(); });
                return;
            }
            if(msg.MessageText==string.Empty)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, this.Resources.GetString(Resource.String.empty_message_error), ToastLength.Short).Show();
                });
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) => {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, this.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() => {
                        ChatroomController.SendChatroomMessageLogic(roomName, msg);
                    }));
                }));
            }
            else
            {
                ChatroomController.SendChatroomMessageLogic(roomName, msg);
            }

        }

        private void SendMessage_Click(object sender, EventArgs e)
        {
            //send the message and record our send message..
            SendChatroomMessageAPI(OurRoomInfo.Name, new Message(SoulSeekState.Username, -1, false, DateTime.Now, DateTime.UtcNow, editTextEnterMessage.Text, true, SentStatus.Pending));

            editTextEnterMessage.Text = string.Empty;
        }

        private void EditTextEnterMessage_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if (e.Text != null && e.Text.ToString() != string.Empty) //ICharSequence..
            {
                sendMessage.Enabled = true;
            }
            else
            {
                sendMessage.Enabled = false;
            }
        }


    }








    public class ChatroomOverviewFragment : Android.Support.V4.App.Fragment
    {
        private RecyclerView recyclerViewOverview;
        private LinearLayoutManager recycleLayoutManager;
        private ChatroomOverviewRecyclerAdapter recyclerAdapter;
        private SearchView filterChatroomView;
        private Soulseek.RoomList internalList = null;
        private List<Soulseek.RoomInfo> internalListParsed = null;
        private List<Soulseek.RoomInfo> internalListParsedFiltered = null;
        private static string FilterString = string.Empty;
        private TextView chatroomsListLoadingView = null;
        private bool created = false;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MainActivity.LogDebug("create chatroom overview view");
            ChatroomController.RoomListReceived += OnChatListReceived;
            View rootView = inflater.Inflate(Resource.Layout.chatroom_overview, container, false);
            chatroomsListLoadingView = rootView.FindViewById<TextView>(Resource.Id.chatroomListLoading);
            filterChatroomView = rootView.FindViewById<SearchView>(Resource.Id.filterChatroom);
            filterChatroomView.QueryTextChange += FilterChatroomView_QueryTextChange;
            if (ChatroomController.RoomList == null)
            {
                chatroomsListLoadingView.Visibility = ViewStates.Visible;
            }
            else
            {
                chatroomsListLoadingView.Visibility = ViewStates.Gone;
            }
            recyclerViewOverview = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewOverview);
            recyclerViewOverview.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleLayoutManager = new LinearLayoutManager(Activity);
            if (ChatroomController.RoomList == null)
            {
                internalList = null;
                internalListParsed = new List<Soulseek.RoomInfo>();
                ChatroomController.GetRoomListApi();
            }
            else
            {
                internalList = ChatroomController.RoomList;
                internalListParsed = ChatroomController.GetParsedList(ChatroomController.RoomList);
            }
            recyclerAdapter = new ChatroomOverviewRecyclerAdapter(FilterRoomList(internalListParsed)); //this depends tightly on MessageController... since these are just strings..
            recyclerViewOverview.SetAdapter(recyclerAdapter);
            recyclerViewOverview.SetLayoutManager(recycleLayoutManager);
            recyclerAdapter.NotifyDataSetChanged();
            created = true;
            return rootView;
        }

        private void FilterChatroomView_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            FilterString = e.NewText;
            this.UpdateChatroomList();
        }

        private static List<Soulseek.RoomInfo> FilterRoomList(List<Soulseek.RoomInfo> original)
        {
            if(FilterString!=string.Empty)
            {
                return original.Where((roomInfo) => {return roomInfo.Name.Contains(FilterString); }).ToList();
            }
            else
            {
                return original;
            }
        }

        public void OnChatListReceived(object sender, EventArgs eventArgs)
        {
            internalList = ChatroomController.RoomList;
            internalListParsed = ChatroomController.RoomListParsed; //here it is already parsed.
            
            this.UpdateChatroomList();
        }

        private void UpdateChatroomList()
        {
            MainActivity.LogDebug("update chatroom list");
            var filteredRoomList = FilterRoomList(internalListParsed);
            var activity = this.Activity != null ? this.Activity : ChatroomActivity.ChatroomActivityRef;
            activity.RunOnUiThread(new Action(() => {
                recyclerAdapter = new ChatroomOverviewRecyclerAdapter(filteredRoomList); //this depends tightly on MessageController... since these are just strings..
                chatroomsListLoadingView.Visibility = ViewStates.Gone;
                recyclerViewOverview.SetAdapter(recyclerAdapter);
                recyclerAdapter.NotifyDataSetChanged();
            }
            ));
        }



        //public void OnMessageReceived(object sender, Message msg)
        //{
        //    var activity = this.Activity != null ? this.Activity : MessagesActivity.MessagesActivityRef;
        //    activity.RunOnUiThread(new Action(() => {
        //        if (internalList != null && internalList.Contains(msg.Username))
        //        {
        //            //update this one...
        //            recyclerAdapter.NotifyItemChanged(internalList.IndexOf(msg.Username));
        //        }
        //        else
        //        {
        //            internalList = MessageController.Messages.Keys.ToList();
        //            if (internalList.Count != 0)
        //            {
        //                noMessagesView.Visibility = ViewStates.Gone;
        //            }
        //            recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
        //            recyclerViewOverview.SetAdapter(recyclerAdapter);
        //            recyclerAdapter.NotifyDataSetChanged();
        //        }
        //    }));
        //}

        public override void OnAttach(Context activity)
        {
            if (created) //attach can happen before we created our view...
            {
                internalList = ChatroomController.RoomList;
                internalListParsed = ChatroomController.GetParsedList(ChatroomController.RoomList);
                recyclerAdapter = new ChatroomOverviewRecyclerAdapter(FilterRoomList(internalListParsed)); //this depends tightly on MessageController... since these are just strings..
                recyclerViewOverview.SetAdapter(recyclerAdapter);
                recyclerAdapter.NotifyDataSetChanged();
                MainActivity.LogDebug("on chatroom attach");
                ChatroomController.RoomListReceived -= OnChatListReceived;
                ChatroomController.RoomListReceived += OnChatListReceived;
            }
            base.OnAttach(activity);
        }

        //public override void OnDetach()
        //{
        //    MessageController.MessageReceived -= OnMessageReceived;
        //    base.OnDetach();
        //}
    }

    public class UserJoinedOrLeftEventArgs
    {
        public bool Joined;
        public string User;
        public string RoomName;
        public UserJoinedOrLeftEventArgs(string roomName, bool joined, string user)
        {
            Joined = joined;
            User = user;
            RoomName = roomName;
        }
    }

    public class MessageReceivedArgs
    {
        public MessageReceivedArgs(string roomName)
        {
            RoomName = roomName;
        }
        public MessageReceivedArgs(string roomName, bool fromUsPending, bool fromUsCon)
        {
            RoomName= roomName;
            FromUsPending = fromUsPending;
            FromUsConfirmation= fromUsCon;
        }
        public string RoomName;
        public bool FromUsPending;
        public bool FromUsConfirmation;
    }

    public class ChatroomController
    {
        public static Soulseek.RoomList RoomList = null;
        public static List<Soulseek.RoomInfo> RoomListParsed = null;
        public static List<Tuple<bool,DateTime>> ConnectionLapse = new List<Tuple<bool, DateTime>>(); //true = connected
        public static EventHandler<EventArgs> RoomListReceived;
        /// <summary>
        /// Invoked whenever moderators are added or removed
        /// </summary>
        public static EventHandler<UserJoinedOrLeftEventArgs> RoomModeratorsChanged; //could be updated to give the user that left or joined....
        public static EventHandler<EventArgs> RoomDataReceived;


        public static EventHandler<MessageReceivedArgs> MessageReceived;
        public static EventHandler<UserJoinedOrLeftEventArgs> UserJoinedOrLeft;
        public static EventHandler<Soulseek.RoomTickerListReceivedEventArgs> RoomTickerListReceived;
        public static EventHandler<Soulseek.RoomTickerAddedEventArgs> RoomTickerAdded;
        public static EventHandler<Soulseek.RoomTickerRemovedEventArgs> RoomTickerRemoved;
        public static EventHandler<string> RoomMembershipRemoved;

        public static bool IsInitialized;
        public static int MAX_MESSAGES_PER_ROOM = 100;
        
        public static List<string> JoinedRoomNames = null; //!!these are ones we are currently joined!! so autojoins after we actually join them and joined but that are not set to autojoin...
        public static List<string> AutoJoinRoomNames = null; //we automatically join these at startup.  if all goes well then JoinedRoomNames should contain all of these...

        public static List<string> NotifyRoomNames = null; //!!these are ones we are currently joined!! so autojoins after we actually join them and joined but that are not set to autojoin...
        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RootNotifyRoomNames = null; //we automatically join these at startup.  if all goes well then JoinedRoomNames should contain all of these...


        //this is for all users that one may log in as...
        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<string>> RootAutoJoinRoomNames = null;
        public static string CurrentUsername = null;

        public static System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomData> JoinedRoomData = new System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomData>();

        public static System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomInfo> ModeratedRoomData = new System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomInfo>();

        public static System.Collections.Concurrent.ConcurrentDictionary<string, Queue<Message>> JoinedRoomMessages = new System.Collections.Concurrent.ConcurrentDictionary<string, Queue<Message>>();
        public static System.Collections.Concurrent.ConcurrentDictionary<string, string> JoinedRoomMessagesLastUserHelper = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        public static System.Collections.Concurrent.ConcurrentDictionary<string, List<Soulseek.RoomTicker>> JoinedRoomTickers = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Soulseek.RoomTicker>>();

        public static bool SortByPopular = true;

        private static bool FirstConnect = true;
        private static void SetConnectionLapsedMessage(bool reconnect)
        {
            if(reconnect && FirstConnect)
            {
                FirstConnect = false;
                return;
            }

            if(PrevJoined==null|| PrevJoined.Count==0)
            {
                //nothing we need to do...
            }
            else
            {
                SpecialMessageCode code = reconnect ? SpecialMessageCode.Reconnect : SpecialMessageCode.Disconnect;
                foreach (string room in PrevJoined)
                {
                    ChatroomController.AddMessage(room, new Message(DateTime.Now, DateTime.UtcNow, code));
                    ChatroomController.MessageReceived?.Invoke(null, new MessageReceivedArgs( room ));
                }
            }
        }


        public static bool IsPrivate(Soulseek.RoomInfo roomInfo)
        {
            if(RoomList.Private.Any(privRoom => { return privRoom.Name == roomInfo.Name; }))
            {
                return true;
            }
            else if(RoomList.Owned.Any(privRoom => { return privRoom.Name == roomInfo.Name; }))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsOwnedByUs(Soulseek.RoomInfo roomInfo)
        {
            return RoomList.Owned.Any(ownedRoom => { return ownedRoom.Name == roomInfo.Name; }); //use AreWeOwner instead maybe...
        }

        public static bool IsAutoJoinOn(Soulseek.RoomInfo autoJoinOn)
        {
            return AutoJoinRoomNames.Any(autoJoin=>{return autoJoinOn.Name == autoJoin; });
        }

        public static bool IsNotifyOn(Soulseek.RoomInfo notifyOn)
        {
            return NotifyRoomNames.Any(notifyOnName => { return notifyOn.Name == notifyOnName; });
        }

        public static bool AreWeMod(string roomname)
        {
            return ChatroomController.JoinedRoomData[roomname].Operators.Contains(SoulSeekState.Username);
        }

        public static bool AreWeOwner(string roomname)
        {
            return ChatroomController.JoinedRoomData[roomname].Owner == SoulSeekState.Username;
        }

        public static List<Soulseek.UserData> GetWrappedUserData(string roomName, bool isPrivate)
        {
            if(isPrivate)
            {
                List<Soulseek.UserData> chatroomUserData = new List<Soulseek.UserData>();
                var userlist = ChatroomController.JoinedRoomData[roomName].Users.ToList();
                var opList = ChatroomController.JoinedRoomData[roomName].Operators.ToList();
                var owner = ChatroomController.JoinedRoomData[roomName].Owner;
                foreach (Soulseek.UserData user in userlist)
                {
                    if(user.Username==owner)
                    {
                        chatroomUserData.Add(GetChatroomUserData(user,Soulseek.UserRole.Owner));
                    }
                    else if(opList.Contains(user.Username))
                    {
                        chatroomUserData.Add(GetChatroomUserData(user, Soulseek.UserRole.Operator));
                    }
                    else
                    {
                        chatroomUserData.Add(GetChatroomUserData(user, Soulseek.UserRole.Normal));
                    }
                }
                chatroomUserData.Sort(new ChatroomUserDataComparer());
                return chatroomUserData;

            }
            else
            {
                return ChatroomController.JoinedRoomData[roomName].Users.ToList();
            }
        }

        public static Soulseek.ChatroomUserData GetChatroomUserData(Soulseek.UserData ud, Soulseek.UserRole role)
        {
            var wrappedUser = new Soulseek.ChatroomUserData(ud.Username,ud.Status, ud.AverageSpeed, ud.DownloadCount, ud.FileCount, ud.DirectoryCount, ud.CountryCode, ud.SlotsFree);
            wrappedUser.ChatroomUserRole = role;
            return wrappedUser;
        }


        public static void SendChatroomMessageLogic(string roomName, Message msg) //you can start out with a message...
        {

            ChatroomController.AddMessage(roomName,msg);

            //MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
            ChatroomController.MessageReceived?.Invoke(null, new MessageReceivedArgs(roomName, true, false));
            Action<Task> continueWithAction = new Action<Task>((Task t) =>
            {
                if (t.IsFaulted)
                {
                    msg.SentMsgStatus = SentStatus.Failed;
                    SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_send_message), ToastLength.Long);
                }
                else
                {
                    msg.SentMsgStatus = SentStatus.Success;
                }
                //MessageController.SaveMessagesToSharedPrefs(SoulSeekState.SharedPreferences);
                ChatroomController.MessageReceived?.Invoke(null, new MessageReceivedArgs(roomName, false, true));
            });
            SoulSeekState.SoulseekClient.SendRoomMessageAsync(roomName, msg.MessageText).ContinueWith(continueWithAction);
        }



        public static bool HasRoomData(string name)
        {
            return JoinedRoomData.ContainsKey(name);
        }

        public static Soulseek.RoomData GetRoomData(string name)
        {
            if(JoinedRoomData.ContainsKey(name))
            {
                return JoinedRoomData[name];
            }
            else
            {
                return null;
            }
        }

        public static List<Soulseek.RoomInfo> GetParsedList(Soulseek.RoomList roomList)
        {
            List<Soulseek.RoomInfo> ownedList = roomList.Owned.ToList();
            List<Soulseek.RoomInfo> publicList = roomList.Public.ToList();
            List<Soulseek.RoomInfo> privateList = roomList.Private.ToList();

            List<Soulseek.RoomInfo> allRooms = new List<Soulseek.RoomInfo>();

            if (JoinedRoomNames.Count!=0)
            {
                allRooms.Add(new Soulseek.RoomInfoCategory(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.joined)));
                //find the rooms and add them...
                foreach(string roomName in JoinedRoomNames)
                {
                    Soulseek.RoomInfo foundRoom = ownedList.FirstOrDefault((room)=>{return room.Name==roomName; });
                    if(foundRoom!=null)
                    {
                        allRooms.Add(foundRoom);
                        continue;
                    }
                    foundRoom = publicList.FirstOrDefault((room)=>{return room.Name==roomName; });
                    if (foundRoom != null)
                    {
                        allRooms.Add(foundRoom);
                        continue;
                    }
                    foundRoom = privateList.FirstOrDefault((room)=>{return room.Name==roomName; });
                    if (foundRoom != null)
                    {
                        allRooms.Add(foundRoom);
                        continue;
                    }
                }
            }

            if(roomList.OwnedCount!=0)
            {
                List<Soulseek.RoomInfo> filteredOwned = ownedList.Where((roomInfo) => { return !JoinedRoomNames.Contains(roomInfo.Name); }).ToList();
                if(filteredOwned.Count>0)
                {
                    allRooms.Add(new Soulseek.RoomInfoCategory(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.owned)));
                    filteredOwned.Sort(new RoomCountComparer());
                    allRooms.AddRange(filteredOwned);
                }
            }
            
            if(roomList.PrivateCount!=0)
            {
                List<Soulseek.RoomInfo> filtered = privateList.Where((roomInfo) => { return !JoinedRoomNames.Contains(roomInfo.Name); }).ToList();
                if(filtered.Count>0)
                {
                    allRooms.Add(new Soulseek.RoomInfoCategory(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.private_room)));
                    filtered.Sort(new RoomCountComparer());
                    allRooms.AddRange(filtered);
                }
            }

            if (roomList.PublicCount != 0)
            {
                allRooms.Add(new Soulseek.RoomInfoCategory(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.public_room)));
                List<Soulseek.RoomInfo> noSpam = publicList.Where((roomInfo) => { return !SpamList.Contains(roomInfo.Name) && !JoinedRoomNames.Contains(roomInfo.Name);}).ToList();
                noSpam.Sort(new RoomCountComparer());
                allRooms.AddRange(noSpam);
            }

            return allRooms;
        }

        public static List<string> SpamList = null;

        static ChatroomController()
        {
            //we filter out unfortunate room names
            SpamList = new List<string>();
            SpamList.Add("! ! ! NO JEWS");
            SpamList.Add("! ! ! NO FAGGOTS");
            SpamList.Add("! ! ! NO NIGGERS");
            SpamList.Add("! ! ! NO NIGGERS ! ! !");
            SpamList.Add("! ! ! NO WOMEN");
            SpamList.Add("! ! ! NO QUEERS");
            SpamList.Add("dev");
        }

        public class RoomCountComparer : IComparer<Soulseek.RoomInfo>
        {
            // Compares by UserCount then Name
            public int Compare(Soulseek.RoomInfo x, Soulseek.RoomInfo y)
            {
                if (x.UserCount.CompareTo(y.UserCount) != 0)
                {
                    return y.UserCount.CompareTo(x.UserCount); //high to low
                }
                else if (x.Name.CompareTo(y.Name) != 0)
                {
                    return x.Name.CompareTo(y.Name);
                }
                else
                {
                    return 0;
                }
            }
        }

        public class ChatroomUserDataComparer : IComparer<Soulseek.UserData>
        {
            // Compares by UserCount then Name
            public int Compare(Soulseek.UserData x, Soulseek.UserData y)
            {
                if(x is Soulseek.ChatroomUserData xData && y is Soulseek.ChatroomUserData yData)
                {
                    return (int)yData.ChatroomUserRole - (int)xData.ChatroomUserRole;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static void ToggleAutoJoin(string roomName, bool feedback, Context c)
        {
            if(AutoJoinRoomNames.Contains(roomName))
            {
                if(feedback)
                {
                    Toast.MakeText(c, string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.startup_room_off),roomName),ToastLength.Short).Show();
                }
                AutoJoinRoomNames.Remove(roomName);
            }
            else
            {
                if(feedback)
                {
                    Toast.MakeText(c, string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.startup_room_on), roomName), ToastLength.Short).Show();
                }
                AutoJoinRoomNames.Add(roomName);
            }
            SaveAutoJoinRoomsToSharedPrefs();
        }

        public static void ToggleNotifyRoom(string roomName, bool feedback, Context c)
        {
            if (NotifyRoomNames.Contains(roomName))
            {
                if (feedback)
                {
                    Toast.MakeText(c, string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.notif_room_off), roomName), ToastLength.Short).Show();
                }
                NotifyRoomNames.Remove(roomName);
            }
            else
            {
                if (feedback)
                {
                    Toast.MakeText(c, string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.notif_room_on), roomName), ToastLength.Short).Show();
                }
                NotifyRoomNames.Add(roomName);
            }
            SaveNotifyRoomsToSharedPrefs();
        }

        public static void SaveAutoJoinRoomsToSharedPrefs()
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            if (RootAutoJoinRoomNames == null || AutoJoinRoomNames==null)
            {
                return;
            }
            RootAutoJoinRoomNames[SoulSeekState.Username] = AutoJoinRoomNames;
            string joinedRoomsString = string.Empty;
            using (System.IO.MemoryStream autoJoinStream = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(autoJoinStream, RootAutoJoinRoomNames);
                joinedRoomsString = Convert.ToBase64String(autoJoinStream.ToArray());
            }
            if (joinedRoomsString != null && joinedRoomsString != string.Empty)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutString(SoulSeekState.M_JoinedRooms, joinedRoomsString);
                    bool success = editor.Commit();
                }
            }
        }

        public static void RestoreAutoJoinRoomsFromSharedPrefs(ISharedPreferences sharedPreferences)
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            string joinedRooms = sharedPreferences.GetString(SoulSeekState.M_JoinedRooms, string.Empty);
            if (joinedRooms == string.Empty)
            {
                RootAutoJoinRoomNames = new System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>();
                AutoJoinRoomNames = new List<string>();
            }
            else
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(joinedRooms)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    RootAutoJoinRoomNames = binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>;
                    if (SoulSeekState.Username != null && SoulSeekState.Username != string.Empty && RootAutoJoinRoomNames.ContainsKey(SoulSeekState.Username))
                    {
                        AutoJoinRoomNames = RootAutoJoinRoomNames[SoulSeekState.Username];
                        CurrentUsername = SoulSeekState.Username;
                    }
                    else
                    {
                        AutoJoinRoomNames = new List<string>();
                        CurrentUsername = SoulSeekState.Username;
                    }
                }
            }
        }





        public static void SaveNotifyRoomsToSharedPrefs()
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            if (RootNotifyRoomNames == null || NotifyRoomNames == null)
            {
                return;
            }
            RootNotifyRoomNames[SoulSeekState.Username] = NotifyRoomNames;
            string notifyRoomsString = string.Empty;
            using (System.IO.MemoryStream notifyStream = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(notifyStream, RootNotifyRoomNames);
                notifyRoomsString = Convert.ToBase64String(notifyStream.ToArray());
            }
            if (notifyRoomsString != null && notifyRoomsString != string.Empty)
            {
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutString(SoulSeekState.M_chatroomsToNotify, notifyRoomsString);
                    bool success = editor.Commit();
                }
            }
        }

        public static void RestoreNotifyRoomsToSharedPrefs(ISharedPreferences sharedPreferences)
        {
            //For some reason, the generic Dictionary in .net 2.0 is not XML serializable.
            string notifyRooms = sharedPreferences.GetString(SoulSeekState.M_chatroomsToNotify, string.Empty);
            if (notifyRooms == string.Empty)
            {
                RootNotifyRoomNames = new System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>();
                NotifyRoomNames = new List<string>();
            }
            else
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream(Convert.FromBase64String(notifyRooms)))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    RootNotifyRoomNames = binaryFormatter.Deserialize(mem) as System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>;
                    if (SoulSeekState.Username != null && SoulSeekState.Username != string.Empty && RootNotifyRoomNames.ContainsKey(SoulSeekState.Username))
                    {
                        NotifyRoomNames = RootNotifyRoomNames[SoulSeekState.Username];
                        CurrentUsername = SoulSeekState.Username;
                    }
                    else
                    {
                        NotifyRoomNames = new List<string>();
                        CurrentUsername = SoulSeekState.Username;
                    }
                }
            }
        }











        public static void Initialize()
        {
            RestoreAutoJoinRoomsFromSharedPrefs(SoulSeekState.SharedPreferences);
            RestoreNotifyRoomsToSharedPrefs(SoulSeekState.SharedPreferences);
            //if auto join rooms list...
            SoulSeekState.SoulseekClient.PrivateRoomMembershipAdded += SoulseekClient_PrivateRoomMembershipAdded;
            SoulSeekState.SoulseekClient.PrivateRoomMembershipRemoved += SoulseekClient_PrivateRoomMembershipRemoved;
            SoulSeekState.SoulseekClient.PrivateRoomModeratedUserListReceived += SoulseekClient_PrivateRoomModeratedUserListReceived;
            SoulSeekState.SoulseekClient.PrivateRoomModerationAdded += SoulseekClient_PrivateRoomModerationAdded;
            SoulSeekState.SoulseekClient.PrivateRoomModerationRemoved += SoulseekClient_PrivateRoomModerationRemoved;
            SoulSeekState.SoulseekClient.PrivateRoomUserListReceived += SoulseekClient_PrivateRoomUserListReceived;
           // SoulSeekState.SoulseekClient.
            SoulSeekState.SoulseekClient.RoomJoined += SoulseekClient_RoomJoined;
            SoulSeekState.SoulseekClient.RoomLeft += SoulseekClient_RoomLeft;
            //SoulSeekState.SoulseekClient.RoomListReceived
            SoulSeekState.SoulseekClient.RoomMessageReceived += SoulseekClient_RoomMessageReceived;
            SoulSeekState.SoulseekClient.RoomTickerAdded += SoulseekClient_RoomTickerAdded;
            SoulSeekState.SoulseekClient.OperatorInPrivateRoomAddedRemoved += SoulseekClient_OperatorInPrivateRoomAddedRemoved;
            SoulSeekState.SoulseekClient.RoomTickerRemoved += SoulseekClient_RoomTickerRemoved;
            SoulSeekState.SoulseekClient.RoomTickerListReceived += SoulseekClient_RoomTickerListReceived;

            JoinedRoomTickers = new System.Collections.Concurrent.ConcurrentDictionary<string, List<Soulseek.RoomTicker>>();
            JoinedRoomNames = new List<string>(); 
            JoinedRoomData = new System.Collections.Concurrent.ConcurrentDictionary<string, Soulseek.RoomData>();

            IsInitialized = true;
        }

        private static void SoulseekClient_OperatorInPrivateRoomAddedRemoved(object sender, Soulseek.OperatorAddedRemovedEventArgs e)
        {
            MainActivity.LogDebug("SoulseekClient_OperatorInPrivateRoomAddedRemoved " + e.RoomName + " " +e.Username + " " + e.Added);

            if (JoinedRoomData.ContainsKey(e.RoomName))
            {
                var oldRoomData = JoinedRoomData[e.RoomName];
                IEnumerable<string> newOperatorList = null;
                if (e.Added)
                {
                     newOperatorList = oldRoomData.Operators.Append(e.Username); 
                }
                else
                {
                     newOperatorList = oldRoomData.Operators.Where((string username) => { return username != e.Username; });
                }
                JoinedRoomData[e.RoomName] = new Soulseek.RoomData(oldRoomData.Name, oldRoomData.Users, oldRoomData.IsPrivate, oldRoomData.Owner, newOperatorList);
            }
            else
            {
                //bad
            }
            RoomModeratorsChanged?.Invoke(null, new UserJoinedOrLeftEventArgs(e.RoomName, e.Added, e.Username));


        }

        private static void SoulseekClient_PrivateRoomUserListReceived(object sender, Soulseek.RoomInfo e)
        {
            MainActivity.LogDebug("SoulseekClient_PrivateRoomModerationRemoved " + e.UserCount); //this is the same as the normal user list received event as far as I can tell...
        }

        private static void SoulseekClient_PrivateRoomModerationRemoved(object sender, string e)
        {
            MainActivity.LogDebug("SoulseekClient_PrivateRoomModerationRemoved " + e); //this only happens on change... not useful I dont think...
        }

        private static void SoulseekClient_PrivateRoomModerationAdded(object sender, string e)
        {
            MainActivity.LogDebug("SoulseekClient_PrivateRoomModerationAdded " + e); //this only happens on change... not useful I dont think...
        }

        private static void SoulseekClient_PrivateRoomModeratedUserListReceived(object sender, Soulseek.RoomInfo e)
        {
            MainActivity.LogDebug("SoulseekClient_PrivateRoomModeratedUserListReceived " + e.UserCount);
            ModeratedRoomData[e.Name] = e; //this is WHO ARE THE OPERATORS. and it will show everyone who is an OPERATOR but not an OWNER. So if your name is here you are an operator.. also this gets called every change.
            //update the room data
            if(JoinedRoomData.ContainsKey(e.Name))
            {
                var oldRoomData = JoinedRoomData[e.Name];
                JoinedRoomData[e.Name] = new Soulseek.RoomData(oldRoomData.Name, oldRoomData.Users, oldRoomData.IsPrivate, oldRoomData.Owner, e.Users);
            }
            RoomModeratorsChanged?.Invoke(null, new UserJoinedOrLeftEventArgs(e.Name, false, null));
        }

        private static void SoulseekClient_PrivateRoomMembershipRemoved(object sender, string e)
        {
            MainActivity.LogDebug("SoulseekClient_PrivateRoomMembershipRemoved " + e);
            //if we remove ourselves or someone else removes us, then we need to back out of the room (and also refresh the list for good feedback).

            //removing should go here, that way we will not autojoin a room we are no longer part of.
            //if we get kicked.
            if (JoinedRoomNames.Contains(e))
            {
                JoinedRoomNames.Remove(e);
                JoinedRoomData.Remove(e, out _); //that way when we go to inner, we wont think we have already joined...
                if (AutoJoinRoomNames != null && AutoJoinRoomNames.Contains(e))
                {
                    AutoJoinRoomNames.Remove(e);
                    SaveAutoJoinRoomsToSharedPrefs();
                }
            }

            RoomMembershipRemoved?.Invoke(null,e);
        }

        private static void SoulseekClient_PrivateRoomMembershipAdded(object sender, string e)
        {
            MainActivity.LogDebug("SoulseekClient_PrivateRoomMembershipAdded "+ e);
        }

        //public static void UpdateSameUserFlagIfApplicable(string roomName, Message msg)
        //{
        //    if (JoinedRoomMessages.ContainsKey(roomName))
        //    {
        //        JoinedRoomMessages[roomName].
        //    }

        public static void AddMessage(string roomName, Message msg)
        {
            if(NotifyRoomNames.Contains(roomName) && msg.SpecialCode==SpecialMessageCode.None) //i.e. do not show the disconnect or reconnect messages..
            {
                ShowNotification(msg, roomName);
            }
            FlagLastUsernameViaHelper(roomName, msg);
            if (JoinedRoomMessages.ContainsKey(roomName))
            {
                //check last name structure
                //if last name is this then set msg.SpecialSameUserFlag = true;
                JoinedRoomMessages[roomName].Enqueue(msg);
                if (JoinedRoomMessages[roomName].Count > 100)
                {
                    JoinedRoomMessages[roomName].Dequeue();
                }
            }
            else
            {
                JoinedRoomMessages[roomName] = new Queue<Message>();
                JoinedRoomMessages[roomName].Enqueue(msg);
                if (JoinedRoomMessages[roomName].Count > 100)
                {
                    JoinedRoomMessages[roomName].Dequeue();
                }
            }
        }

        private static void FlagLastUsernameViaHelper(string roomName, Message msg)
        {
            string lastUser = JoinedRoomMessagesLastUserHelper.GetValueOrDefault(roomName);
            if(lastUser==msg.Username)
            {
                msg.SameAsLastUser = true;
            }
            else
            {
                JoinedRoomMessagesLastUserHelper[roomName] = msg.Username;
            }
        }

        private static void SoulseekClient_RoomMessageReceived(object sender, Soulseek.RoomMessageReceivedEventArgs e)
        {
            if(SeekerApplication.IsUserInIgnoreList(e.Username))
            {
                MainActivity.LogDebug("IGNORED room msg received: r:" + e.RoomName + " u: " + e.Username);
                return;
            }

                MainActivity.LogDebug("room msg received: r:" + e.RoomName + " u: " + e.Username);

            Message msg = new Message(e.Username, -1, false, DateTime.Now, DateTime.UtcNow, e.Message, false);
            if(e.Username == SoulSeekState.Username)
            {
                //we already logged it..
                return;
            }
            AddMessage(e.RoomName, msg);
            MessageReceived?.Invoke(null,new MessageReceivedArgs(e.RoomName));
        }

        private static void SoulseekClient_RoomLeft(object sender, Soulseek.RoomLeftEventArgs e)
        {
            if (JoinedRoomData.ContainsKey(e.RoomName))
            {
                var oldRoomData = JoinedRoomData[e.RoomName];
                var newUserList = oldRoomData.Users.Where((Soulseek.UserData userData)=>{ return userData.Username != e.Username; });
                JoinedRoomData[e.RoomName] = new Soulseek.RoomData(oldRoomData.Name, newUserList, oldRoomData.IsPrivate, oldRoomData.Owner, oldRoomData.Operators);
            }
            else
            {
                //bad
            }
            UserJoinedOrLeft?.Invoke(null, new UserJoinedOrLeftEventArgs(e.RoomName,false,e.Username));
        }

        private static void SoulseekClient_RoomJoined(object sender, Soulseek.RoomJoinedEventArgs e)
        {
            MainActivity.LogDebug("User Joined" + e.Username);
            if (JoinedRoomData.ContainsKey(e.RoomName))
            {
                var oldRoomData = JoinedRoomData[e.RoomName];
                JoinedRoomData[e.RoomName] = new Soulseek.RoomData(oldRoomData.Name, oldRoomData.Users.Append(e.UserData),oldRoomData.IsPrivate, oldRoomData.Owner, oldRoomData.Operators);
            }
            else if(e.Username == SoulSeekState.Username)
            {
                //this is when we first join..
            }
            else
            {
                //bad
            }
            UserJoinedOrLeft?.Invoke(null, new UserJoinedOrLeftEventArgs(e.RoomName, true, e.Username));
        }

        private static void SoulseekClient_RoomTickerAdded(object sender, Soulseek.RoomTickerAddedEventArgs e)
        {
            MainActivity.LogDebug("SoulseekClient_RoomTickerAdded");
            if (JoinedRoomTickers.ContainsKey(e.RoomName))
            {
                JoinedRoomTickers[e.RoomName].Add(e.Ticker);
            }
            else
            {
                //I dont know if this gets hit or not...
            }
            RoomTickerAdded?.Invoke(null,e);
        }

        private static void SoulseekClient_RoomTickerRemoved(object sender, Soulseek.RoomTickerRemovedEventArgs e)
        {
            MainActivity.LogDebug("RoomTickerRemovedEventArgs");
            //idk what to do here
            RoomTickerRemoved?.Invoke(null, e);
        }

        private static void SoulseekClient_RoomTickerListReceived(object sender, Soulseek.RoomTickerListReceivedEventArgs e)
        {
            MainActivity.LogDebug("SoulseekClient_RoomTickerListReceived");
            JoinedRoomTickers[e.RoomName] = e.Tickers.ToList();
            RoomTickerListReceived?.Invoke(null, e);
        }
        public static string StartingState = null; //this is if we get killed in the inner fragment.
        public static List<string> PrevJoined = null;
        public static void ClearAndCacheJoined()
        {
            if(JoinedRoomNames==null || JoinedRoomNames.Count == 0)
            {
                return;
            }
            PrevJoined = new List<string>();
            foreach (string joined in JoinedRoomNames)
            {
                PrevJoined.Add(joined);
            }
            JoinedRoomNames.Clear();
            SetConnectionLapsedMessage(false);
        }

        public static bool AttemptedToJoinAutoJoins = false;
        public static void JoinAutoJoinRoomsAndPreviousJoined()
        {
            GetRoomListApi();
            ChatroomController.SetConnectionLapsedMessage(true);
            if (AutoJoinRoomNames != null && AutoJoinRoomNames.Count > 0)
            {
                foreach (string roomName in AutoJoinRoomNames)
                {
                    JoinRoomApi(roomName, true, false, false, true);
                }
            }
            if (PrevJoined != null && PrevJoined.Count > 0) //if connect and reconnect
            {
                foreach (string roomName in PrevJoined)
                {
                    JoinRoomApi(roomName, true, false, false, false);
                }
                PrevJoined.Clear();
            }

            //if we got killed.
            if(StartingState!=null && StartingState!=string.Empty)
            {
                MainActivity.LogDebug("starting state is not null " + StartingState);
                JoinRoomApi(StartingState, true, false, false, false);
                StartingState=null;
            }

            AttemptedToJoinAutoJoins = true;
        }

        public static string CHANNEL_ID = "Chatroom Messages ID";
        public static string CHANNEL_NAME = "Chatroom Messages";
        public static string FromRoomName = "FromThisRoom";
        public static string ComingFromMessageTapped = "FromAMessage";
        public static string currentlyInsideRoomName = string.Empty;

        public static void ShowNotification(Message msg, string roomName)
        {
            if(msg.Username == SoulSeekState.Username)
            {
                return;
            }
            MainActivity.LogDebug("currently in room: " + currentlyInsideRoomName);
            if (roomName == currentlyInsideRoomName)
            {
                return;
            }
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                try
                {
                    Helpers.CreateNotificationChannel(SoulSeekState.ActiveActivityRef, CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SoulSeekState.ActiveActivityRef, typeof(ChatroomActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop);
                    notifIntent.PutExtra(FromRoomName, roomName); //so we can go to this user..
                    notifIntent.PutExtra(ComingFromMessageTapped, true); //so we can go to this user..
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SoulSeekState.ActiveActivityRef, msg.Username.GetHashCode(), notifIntent, PendingIntentFlags.UpdateCurrent);
                    Notification n = Helpers.CreateNotification(SoulSeekState.ActiveActivityRef, pendingIntent, CHANNEL_ID, string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.new_room_message_received), roomName), msg.Username + ": " + msg.MessageText, false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SoulSeekState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify(roomName.GetHashCode(), n);
                }
                catch (System.Exception e)
                {
                    MainActivity.LogFirebase("ShowNotification failed: " + e.Message + e.StackTrace);
                }
            });
        }

        public static void GetRoomListApi(bool feedback = false)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                if(feedback)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.must_be_logged_to_get_room_list), ToastLength.Short).Show();
                    });
                }
                return;
            }
            if(feedback)
            {
                if(SoulSeekState.ActiveActivityRef!=null)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.requesting_room_list), ToastLength.Short).Show();
                    });
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.MainActivityRef.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => { GetRoomListLogic(feedback); }));
                }));
            }
            else
            {
                GetRoomListLogic(feedback);
            }
        }

        public static void GetRoomListLogic(bool feedback)
        {
            Task<Soulseek.RoomList> task = null;
            try
            {
                task = SoulSeekState.SoulseekClient.GetRoomListAsync();
            }
            catch (Exception e)
            {
                return;
            }
            task.ContinueWith((Task<Soulseek.RoomList> task) =>
            {
                if (task.IsFaulted)
                {

                }
                else
                {
                    RoomList = task.Result;
                    RoomListParsed = GetParsedList(RoomList);
                    if(feedback)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.room_list_received), ToastLength.Short).Show();
                        });
                    }
                    RoomListReceived?.Invoke(null, new EventArgs());
                }
            });
        }


        public static void CreateRoomApi(string roomName, bool isPrivate, bool feedback)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.must_be_logged_to_create_room), ToastLength.Short).Show();
                return;
            }
            if (feedback)
            {
                if (SoulSeekState.ActiveActivityRef != null)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.privateRoomCreation), ToastLength.Short).Show();
                    });
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => { CreateRoomLogic(roomName, isPrivate, feedback); }));
                }));
            }
            else
            {
                CreateRoomLogic(roomName, isPrivate, feedback);
            }
        }

        public static void AddRemoveUserToPrivateRoomAPI(string roomName, string userToAdd, bool feedback, bool asMod, bool removeInstead=false)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.must_be_logged_to_add_or_remove_user), ToastLength.Short).Show();
                return;
            }
            if (feedback)
            {
                if (SoulSeekState.ActiveActivityRef != null)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {


                        string msg = string.Empty;
                        if(asMod && removeInstead)
                        {
                            msg = SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.removing_mod);
                        }
                        else if(asMod && !removeInstead)
                        {
                            msg = SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.adding_mod);
                        }
                        else if(!asMod && !removeInstead)
                        {
                            msg = SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.inviting_user_to);
                        }
                        else if(!asMod && removeInstead)
                        {
                            msg = SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.removing_user_from);
                        }
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(msg,roomName), ToastLength.Short).Show();

                    });
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => { AddUserToPrivateRoomLogic(roomName, userToAdd, feedback, asMod, removeInstead); }));
                }));
            }
            else
            {
                AddUserToPrivateRoomLogic(roomName, userToAdd, feedback, asMod, removeInstead);
            }
        }


        public static void AddUserToPrivateRoomLogic(string roomName, string userToAdd, bool feedback, bool asMod, bool removeInstead)
        {
            Task task = null;
            string failureMsg = string.Empty;
            string successMsg = string.Empty;
            try
            {
                if(asMod && !removeInstead)
                {
                    successMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.success_added_mod);
                    failureMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_added_mod);
                    task = SoulSeekState.SoulseekClient.AddPrivateRoomModeratorAsync(roomName, userToAdd);
                }
                else if(!asMod && !removeInstead)
                {
                    successMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.success_invite_user);
                    failureMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_invite_user);
                    task = SoulSeekState.SoulseekClient.AddPrivateRoomMemberAsync(roomName, userToAdd);
                }
                else if(asMod && removeInstead)
                {
                    successMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.success_remove_mod);
                    failureMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_remove_mod);
                    task = SoulSeekState.SoulseekClient.RemovePrivateRoomModeratorAsync(roomName, userToAdd);
                }
                else if (!asMod && removeInstead)
                {
                    successMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.success_removed_user);
                    failureMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_removed_user);
                    task = SoulSeekState.SoulseekClient.RemovePrivateRoomMemberAsync(roomName, userToAdd);
                }
            }
            catch (Exception e)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, failureMsg, ToastLength.Short).Show();
                });
                return;
            }
            task.ContinueWith((Task task) =>
            {
                if (task.IsFaulted)
                {
                    //TODO

                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, failureMsg, ToastLength.Short).Show();
                    });

                }
                else
                {
                    //add to joined list and save joined list...

                    if (feedback)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, successMsg, ToastLength.Short).Show();
                        });
                    }

                }
            });
        }




        public static void CreateRoomLogic(string roomName, bool isPrivate, bool feedback)
        {
            Task<Soulseek.RoomData> task = null;
            try
            {
                task = SoulSeekState.SoulseekClient.JoinRoomAsync(roomName, isPrivate); //this will create it if it does not exist..
            }
            catch (Exception e)
            {
                return;
            }
            task.ContinueWith((Task<Soulseek.RoomData> task) =>
            {
                if (task.IsFaulted)
                {

                }
                else
                {
                    //add to joined list and save joined list...

                    if (feedback)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                            Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.create_and_refresh), ToastLength.Short).Show();
                        });
                    }
                    if (!JoinedRoomNames.Contains(roomName))
                    {
                        JoinedRoomNames.Add(roomName);
                        //TODO: SAVE
                    }
                    JoinedRoomData[roomName] = task.Result;
                    GetRoomListApi();
                    
                }
            });
        }


        public static void DropMembershipOrOwnershipApi(string roomName, bool ownership, bool feedback)
        {
            string ownershipString = SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.ownership);
            string membershipString = SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.membership);
            if (!SoulSeekState.currentlyLoggedIn)
            {
                string membership = ownership ? ownershipString : membershipString;
                Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.must_be_logged_to_drop_private), ToastLength.Short).Show();
                return;
            }
            if (feedback)
            {
                if (SoulSeekState.ActiveActivityRef != null)
                {
                    string membership = ownership ? ownershipString : membershipString;
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.dropping_MEMBERSHIP_of_ROOMNAME), ToastLength.Short).Show();
                    });
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => { DropMembershipOrOwnershipLogic(roomName, ownership, feedback); }));
                }));
            }
            else
            {
                DropMembershipOrOwnershipLogic(roomName, ownership, feedback);
            }
        }

        public static void DropMembershipOrOwnershipLogic(string roomName, bool ownership, bool feedback)
        {
            Task task = null;
            try
            {
                if(ownership)
                {
                    task = SoulSeekState.SoulseekClient.DropPrivateRoomOwnershipAsync(roomName); //this will create it if it does not exist..
                }
                else
                {
                    task = SoulSeekState.SoulseekClient.DropPrivateRoomMembershipAsync(roomName); //this will create it if it does not exist..
                }
            }
            catch (Exception e)
            {
                if (feedback)
                {
                    string ownershipString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.ownership);
                    string membershipString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.membership);
                    string membership = ownership ? ownershipString : membershipString;
                    SeekerApplication.ShowToast(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_remove), membership), ToastLength.Short);
                    MainActivity.LogFirebase("DropMembershipOrOwnershipLogic " + membership + e.Message + e.StackTrace);
                }
                return;
            }
            task.ContinueWith((Task task) =>
            {
                string ownershipString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.ownership);
                string membershipString = SoulSeekState.ActiveActivityRef.GetString(Resource.String.membership);
                string membership = ownership ? ownershipString : membershipString;
                if (task.IsFaulted)
                {
                    if (feedback)
                    {
                        SeekerApplication.ShowToast(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_remove), membership), ToastLength.Short);
                    }
                    MainActivity.LogFirebase("DropMembershipOrOwnershipLogic " + task.Exception);
                }
                else
                {
                    //I dont think there is anything we need to do... I think that our event will tell us about our new ticker...
                    if (feedback)
                    {
                        SeekerApplication.ShowToast(string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.successfully_removed), membership), ToastLength.Short);
                    }

                }
            });
        }

        public static void SetTickerApi(string roomName, string tickerMessage, bool feedback)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.must_be_logged_to_set_ticker), ToastLength.Short).Show();
                return;
            }
            if (feedback)
            {
                if (SoulSeekState.ActiveActivityRef != null)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.setting_ticker), ToastLength.Short).Show();
                    });
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => { SetTickerLogic(roomName, tickerMessage, feedback); }));
                }));
            }
            else
            {
                SetTickerLogic(roomName, tickerMessage, feedback);
            }
        }

        public static void SetTickerLogic(string roomName, string tickerMessage, bool feedback)
        {
            Task task = null;
            try
            {
                task = SoulSeekState.SoulseekClient.SetRoomTickerAsync(roomName, tickerMessage); //this will create it if it does not exist..
            }
            catch (Exception e)
            {
                if(feedback)
                {
                    SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_set_ticker), ToastLength.Short);
                }
                return;
            }
            task.ContinueWith((Task task) =>
            {
                if (task.IsFaulted)
                {
                    if (feedback)
                    {
                        SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.failed_to_set_ticker), ToastLength.Short);
                    }
                }
                else
                {
                    //I dont think there is anything we need to do... I think that our event will tell us about our new ticker...
                    if (feedback)
                    {
                        SeekerApplication.ShowToast(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.successfully_set_ticker), ToastLength.Short);
                    }
                }
            });
        }



        public static void JoinRoomApi(string roomName, bool joining, bool refreshViewAfter, bool feedback, bool fromAutoJoin)
        {
            MainActivity.LogDebug("JOINING ROOM" + roomName);
            if (!SoulSeekState.currentlyLoggedIn)
            {   //since this happens on startup its no good to have this logic...
                MainActivity.LogDebug("CANT JOIN NOT LOGGED IN:" + roomName);
                return;
            }
            if (feedback && !joining)
            {
                if (SoulSeekState.ActiveActivityRef != null)
                {
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, string.Format(SoulSeekState.ActiveActivityRef.Resources.GetString(Resource.String.leaving_room), roomName), ToastLength.Short).Show();
                    });
                }
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => { JoinRoomLogic(roomName, joining, refreshViewAfter, feedback, fromAutoJoin); }));
                }));
            }
            else
            {
                JoinRoomLogic(roomName, joining, refreshViewAfter, feedback, fromAutoJoin);
            }
        }

        public static void JoinRoomLogic(string roomName, bool joining, bool refreshViewAfter, bool feedback, bool fromAutoJoin)
        {
            Task task = null;
            try
            {
                if(joining)
                {
                    task = SoulSeekState.SoulseekClient.JoinRoomAsync(roomName); //this will create it if it does not exist..
                }
                else
                {
                    task = SoulSeekState.SoulseekClient.LeaveRoomAsync(roomName); //this will create it if it does not exist..
                }

            }
            catch (Exception e)
            {
                return;
            }
            task.ContinueWith((Task task) =>
            {
                if (task.IsFaulted)
                {
                    MainActivity.LogDebug(task.Exception.GetType().Name);
                    MainActivity.LogDebug(task.Exception.Message);
                    if(fromAutoJoin)
                    {
                        if(task.Exception != null && task.Exception.InnerException != null && task.Exception.InnerException.InnerException != null)
                        {
                            if(task.Exception.InnerException.InnerException is Soulseek.RoomJoinForbiddenException)
                            {
                                MainActivity.LogDebug("forbidden room exception!! remove it from autojoin.." + joining);
                                MainActivity.LogFirebase("forbidden room exception!! remove it from autojoin.." + joining + "room name" + roomName); //these should only be private rooms else we are doing something wrong...
                                if (AutoJoinRoomNames != null && AutoJoinRoomNames.Contains(roomName))
                                {
                                    AutoJoinRoomNames.Remove(roomName);
                                    SaveAutoJoinRoomsToSharedPrefs();
                                }
                            }
                        }
                        else
                        {
                            MainActivity.LogDebug("failed to join autojoin... join?" + joining);
                        }
                    }
                    MainActivity.LogDebug("join / leave task failed... join?" + joining);
                }
                else
                {
                    if(task is Task<Soulseek.RoomData> taskRoomData)
                    {
                        //add to joined list and save joined list...
                        if(!JoinedRoomNames.Contains(roomName))
                        {
                            JoinedRoomNames.Add(roomName);
                            //TODO: SAVE
                        }
                        JoinedRoomData[roomName] = taskRoomData.Result;
                        RoomDataReceived?.Invoke(null,new EventArgs());
                    }
                    else
                    {
                        if(joining)
                        {
                            MainActivity.LogDebug("WRONG TASK TYPE");
                        }
                        else
                        {
                            //add to joined list and save joined list...
                            if (JoinedRoomNames.Contains(roomName))
                            {
                                JoinedRoomNames.Remove(roomName);
                                JoinedRoomData.Remove(roomName,out _); //that way when we go to inner, we wont think we have already joined...
                                if(AutoJoinRoomNames!=null && AutoJoinRoomNames.Contains(roomName))
                                {
                                    AutoJoinRoomNames.Remove(roomName);
                                    SaveAutoJoinRoomsToSharedPrefs();
                                }
                                //TODO: SAVE
                            }
                        }
                    }
                    if(refreshViewAfter)
                    {
                        ChatroomController.GetRoomListApi(false);
                    }
                }
            });
        }



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
                if(holder is ChatroomOverviewHolder chatOverview)
                {
                    chatOverview.chatroomOverviewView.setItem(localDataSet[position]);
                }
                else if(holder is ChatroomOverviewJoinedViewHolder chatJoinedViewHolder)
                {
                    chatJoinedViewHolder.chatroomOverviewView.setItem(localDataSet[position]);
                }
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
            else if(ChatroomController.JoinedRoomNames.Contains(localDataSet[position].Name))
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
            if(viewType==VIEW_CATEGORY_HEADER)
            {
                ChatroomOverviewCategoryView view = ChatroomOverviewCategoryView.inflate(parent);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //(view as View).Click += ChatroomOverviewClick;
                return new ChatroomOverviewCategoryHolder(view as View);
            }
            else if(viewType == VIEW_JOINED_ROOM)
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
            ChatroomController.JoinRoomApi(localDataSet[position].Name, false, true, true, false);
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
        public RecyclerView.ViewHolder ViewHolder { get;set;}
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


    public class RoomUserListDialog : Android.Support.V4.App.DialogFragment //, PopupMenu.IOnMenuItemClickListener doesnt work for dialogfragment
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
            base.OnPause();
        }

        public void OnUserJoinedOrLeft(object sender, UserJoinedOrLeftEventArgs e)
        {
            if (e.RoomName==OurRoomName)
            {
                UpdateData();
            }
        }

        public void OnRoomModeratorsChanged(object sender, UserJoinedOrLeftEventArgs eventArgs)
        {
            //TODO diffutil stuff... well we can do the diffutil since its easy to see who got added / removed...
            if(eventArgs.RoomName == OurRoomName)
            {
                UpdateData();
            }
        }

        private void UpdateData()
        {
            try
            {
                this.Activity.RunOnUiThread( () => {
                    roomUserListAdapter = new RoomUserListRecyclerAdapter(ChatroomController.GetWrappedUserData(OurRoomName, IsPrivate));
                    recyclerViewUsers.SetAdapter(roomUserListAdapter);
                    roomUserListAdapter.NotifyDataSetChanged();
                });
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("EXCEPTION UpdateData " + e.Message + e.StackTrace);
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.room_users_dialog, container); //container is parent
        }

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            //Dialog.SetTitle("File Info"); //is this needed in any way??

            this.SetStyle((int)DialogFragmentStyle.Normal, 0);
            this.Dialog.SetTitle(OurRoomName);

            recyclerViewUsers = view.FindViewById<RecyclerView>(Resource.Id.recyclerViewUsers);
            recyclerViewUsers.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleLayoutManager = new LinearLayoutManager(Activity);
            roomUserListAdapter = new RoomUserListRecyclerAdapter(ChatroomController.GetWrappedUserData(OurRoomName,IsPrivate));
            recyclerViewUsers.SetAdapter(roomUserListAdapter);
            recyclerViewUsers.SetLayoutManager(recycleLayoutManager);
            this.RegisterForContextMenu(recyclerViewUsers);
            UpdateListView();
        }

        private void UpdateListView()
        {
            //var users = ChatroomController.JoinedRoomData[OurRoomName].Users;
            //roomUserListAdapter = new RoomUserListRecyclerAdapter(this.Activity, users);
            ////roomUserListAdapter.Owner = this;
            //listViewTickers.Adapter = tickerAdapter;
        }

        //public bool OnMenuItemClick(IMenuItem item)
        //{
        //    return false;
        //    //throw new NotImplementedException();
        //}

        public override bool OnContextItemSelected(IMenuItem item)
        {
            var userdata = longClickedUserData;
            switch (item.ItemId)
            {
                case 0: //"Remove User"
                    ChatroomController.AddRemoveUserToPrivateRoomAPI(OurRoomName, userdata.Username,true,false,true);
                    return true;
                case 1: //"Remove Moderator Privilege"
                    ChatroomController.AddRemoveUserToPrivateRoomAPI(OurRoomName, userdata.Username, true, true, true);
                    return true;
                case 2:
                    ChatroomController.AddRemoveUserToPrivateRoomAPI(OurRoomName, userdata.Username, true, true, false);
                    return true;
                case 3: //browse user
                    Action<View> action = new Action<View>((v) => {
                        Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                        //((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    View snackView = this.View.FindViewById<ViewGroup>(Resource.Id.userListRoom);
                    DownloadDialog.RequestFilesApi(userdata.Username, snackView, action, null);
                    return true;
                case 4: //search users files
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = userdata.Username;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToSearch, 1);
                    this.StartActivity(intent);
                    return true;
                case 5: //add to user list
                    UserListActivity.AddUserAPI(SoulSeekState.ActiveActivityRef, userdata.Username, null);
                    return true;
                case 6: //add to ignored
                    SeekerApplication.AddToIgnoreListFeedback(SoulSeekState.ActiveActivityRef, userdata.Username);
                    return true;
                case 7: //message user
                    Intent intentMsg = new Intent(SoulSeekState.ActiveActivityRef, typeof(MessagesActivity));
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


    public class RoomUserListRecyclerAdapter : RecyclerView.Adapter, PopupMenu.IOnMenuItemClickListener
    {
        private List<Soulseek.UserData> localDataSet;
        public override int ItemCount => localDataSet.Count;
        private int position = -1;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as RoomUserItemViewHolder).userInnerView.setItem(localDataSet[position]);
            //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
        }

        public void setPosition(int position)
        {
            if(position==-1)
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

        //public override void OnViewRecycled(Java.Lang.Object holder)
        //{
        //    base.OnViewRecycled(holder);
        //}

        //private void MessageOverviewClick(object sender, EventArgs e)
        //{
        //    //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
        //    //pop.Inflate(Resource.Menu.download_diag_options);
        //    //pop.Show();
        //    setPosition((sender as MessageOverviewView).ViewHolder.AdapterPosition);
        //    MessagesActivity.ChangeFragment((sender as MessageOverviewView).View);
        //}


        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            RoomUserItemView view = RoomUserItemView.inflate(parent);
            view.setupChildren();
            // .inflate(R.layout.text_row_item, viewGroup, false);
            (view as View).LongClick += RoomUserListRecyclerAdapter_LongClick;
            return new RoomUserItemViewHolder(view as View);


        }


        private void RoomUserListRecyclerAdapter_LongClick(object sender, View.LongClickEventArgs e)
        {
            //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
            //pop.Inflate(Resource.Menu.download_diag_options);
            //pop.Show();
            //setPosition((sender as RoomUserItemView).ViewHolder.AdapterPosition);

            RoomUserListDialog.longClickedUserData = (sender as RoomUserItemView).DataItem;

            (sender as View).ShowContextMenu();
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            throw new NotImplementedException();
        }

        public RoomUserListRecyclerAdapter(List<Soulseek.UserData> ti)
        {
            localDataSet = ti;
        }

    }

    public class RoomUserItemViewHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
    {
        public RoomUserItemView userInnerView;


        public RoomUserItemViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            userInnerView = (RoomUserItemView)view;
            userInnerView.ViewHolder = this;
            (view as View).SetOnCreateContextMenuListener(this);
        }

        public RoomUserItemView getUnderlyingView()
        {
            return userInnerView;
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            RoomUserItemView roomUserItemView = v as RoomUserItemView;
            //private room specific options
            bool canRemoveModPriviledgesAndApplicable = false;
            bool canAddModPriviledgesAndApplicable = false;
            bool canRemoveUser = false;
            if(roomUserItemView.DataItem is Soulseek.ChatroomUserData cData) //that means we are in a private room
            {
                if(ChatroomController.AreWeOwner(RoomUserListDialog.OurRoomName))
                {
                    if(cData.ChatroomUserRole == Soulseek.UserRole.Operator)
                    {
                        canRemoveModPriviledgesAndApplicable = true;
                    }
                    else
                    {
                        canAddModPriviledgesAndApplicable = true; //i.e. if the other user is non operator
                    }
                    canRemoveUser = true;
                }
                else if(ChatroomController.AreWeMod(RoomUserListDialog.OurRoomName))
                {
                    //we do not have any priviledges regarding fellow mods
                    if (cData.ChatroomUserRole == Soulseek.UserRole.Normal)
                    {
                        canRemoveUser = true;
                    }
                }
            }

            AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)menuInfo;


            if(canRemoveUser)
            {
                menu.Add(0, 0, 0, SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_user));
            }
            if(canRemoveModPriviledgesAndApplicable)
            {
                menu.Add(0, 1, 1, SoulSeekState.ActiveActivityRef.GetString(Resource.String.remove_mod_priv));
            }
            if(canAddModPriviledgesAndApplicable)
            {
                menu.Add(0, 2, 2, SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_mod_priv));
            }
            

            //normal - add to user list, browse, etc...
            menu.Add(1, 3, 3, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user));
            menu.Add(1, 4, 4, SoulSeekState.ActiveActivityRef.GetString(Resource.String.search_user_files));
            menu.Add(1, 5, 5, SoulSeekState.ActiveActivityRef.GetString(Resource.String.add_to_user_list));
            menu.Add(1, 6, 6, SoulSeekState.ActiveActivityRef.GetString(Resource.String.ignore_user));
            menu.Add(1, 7, 7, SoulSeekState.ActiveActivityRef.GetString(Resource.String.msg_user));

            var hackFixDialogContext = new FixForDialogFragmentContext();
            for (int i = 0; i < menu.Size(); i++)
            {
                menu.GetItem(i).SetOnMenuItemClickListener(hackFixDialogContext);
            }
            

        }

        public class FixForDialogFragmentContext : Java.Lang.Object, IMenuItemOnMenuItemClickListener
        {
            public bool OnMenuItemClick(Android.Views.IMenuItem item)
            {
                return RoomUserListDialog.forContextHelp.OnContextItemSelected(item);
            }
        }
    }

    public class RoomUserItemView : LinearLayout
    {
        public RoomUserItemViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewNumFiles;
        private TextView viewSpeed;
        private TextView viewOperatorStatus;
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
            viewOperatorStatus = FindViewById<TextView>(Resource.Id.operatorStatus);
        }

        public void setItem(Soulseek.UserData userData)
        {
            DataItem = userData;
            viewUsername.Text = ChatroomActivity.LocaleToEmoji(userData.CountryCode.ToUpper()) + " " + userData.Username;
            viewNumFiles.Text = userData.FileCount.ToString("N0");
            viewSpeed.Text = (userData.AverageSpeed / 1024).ToString("N0") + " " + SeekerApplication.STRINGS_KBS;
            if(userData is Soulseek.ChatroomUserData cData)
            {
                if(cData.ChatroomUserRole == Soulseek.UserRole.Normal)
                {
                    viewOperatorStatus.Visibility = ViewStates.Gone;
                }
                else if(cData.ChatroomUserRole == Soulseek.UserRole.Operator)
                {
                    viewOperatorStatus.Visibility = ViewStates.Visible;
                    viewOperatorStatus.Text = string.Format("({0})",SoulSeekState.ActiveActivityRef.GetString(Resource.String.mod).ToUpper());
                }
                else
                {
                    viewOperatorStatus.Visibility = ViewStates.Visible;
                    viewOperatorStatus.Text = string.Format("({0})", SoulSeekState.ActiveActivityRef.GetString(Resource.String.owner).ToUpper());
                }
            }
            else
            {
                viewOperatorStatus.Visibility = ViewStates.Gone;
            }
            
        }
    }










    public class AllTickersDialog : Android.Support.V4.App.DialogFragment
    {
        public static string OurRoomName = string.Empty;
        private ListView listViewTickers = null;
        private TickerAdapter tickerAdapter = null;
        public AllTickersDialog(string ourRoomName)
        {
            OurRoomName = ourRoomName;
        }
        public AllTickersDialog()
        {

        }

        public override void OnResume()
        {
            base.OnResume();

            Window window = Dialog.Window;//  getDialog().getWindow();
            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;

            window.SetLayout((int)(width * 0.90), Android.Views.WindowManagerLayoutParams.WrapContent);//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.all_ticker_dialog, container); //container is parent
        }

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            //Dialog.SetTitle("File Info"); //is this needed in any way??

            this.SetStyle((int)DialogFragmentStyle.Normal, 0);
            this.Dialog.SetTitle(OurRoomName);

            listViewTickers = view.FindViewById<ListView>(Resource.Id.listViewTickers);
 

            UpdateListView();
        }

        private void UpdateListView()
        {
            var roomTickers = ChatroomController.JoinedRoomTickers[OurRoomName].ToList();
            roomTickers.Reverse();
            tickerAdapter = new TickerAdapter(this.Activity, roomTickers);
            tickerAdapter.Owner = this;
            listViewTickers.Adapter = tickerAdapter;
        }
    }

    public class TickerAdapter : ArrayAdapter<Soulseek.RoomTicker>
    {
        public Android.Support.V4.App.DialogFragment Owner = null;
        public TickerAdapter(Context c, List<Soulseek.RoomTicker> items) : base(c, 0, items)
        {
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TickerItemView itemView = (TickerItemView)convertView;
            if (null == itemView)
            {
                itemView = TickerItemView.inflate(parent);
            }
            itemView.setItem(GetItem(position));
            return itemView;
            //return base.GetView(position, convertView, parent);
        }
    }

    public class TickerItemView : LinearLayout
    {
        private TextView tickerTextView;
        public TickerItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.ticker_item, this, true);
            setupChildren();
        }
        public TickerItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.ticker_item, this, true);
            setupChildren();
        }

        public static TickerItemView inflate(ViewGroup parent)
        {
            TickerItemView itemView = (TickerItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ticker_item_dummy, parent, false);
            return itemView;
        }

        private void setupChildren()
        {
            tickerTextView = FindViewById<TextView>(Resource.Id.textView1);
        }

        public void setItem(Soulseek.RoomTicker t)
        {
            tickerTextView.Text = t.Message + " --" + t.Username;
        }
    }


    }
