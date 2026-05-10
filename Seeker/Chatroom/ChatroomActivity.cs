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

    [Activity(Label = "ChatroomActivity", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTop, Exported = false)]
    public class ChatroomActivity : SlskLinkMenuActivity//, Android.Widget.PopupMenu.IOnMenuItemClickListener
    {
        public static ChatroomActivity ChatroomActivityRef { private set; get; } = null;

        private GenericOnBackPressedCallback backPressedCallback;

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

        protected override void OnResume()
        {
            // this needs to be set here. otherwise we can create a new chatroom activity, go back to previous
            // and the ref will point to the now finished activity.
            ChatroomActivityRef = this;
            base.OnResume();
        }

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

            backPressedCallback = new GenericOnBackPressedCallback(false, onBackPressedAction);
            OnBackPressedDispatcher.AddCallback(backPressedCallback);

            if (savedInstanceState != null && savedInstanceState.GetBoolean(SAVE_STATE_AT_INNER_KEY))
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
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(ChatroomInnerFragment.OurRoomInfo), INNER_FRAGMENT_TAG).Commit();
                backPressedCallback.Enabled = true;
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
                        SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(roomInfo), INNER_FRAGMENT_TAG).Commit();
                        backPressedCallback.Enabled = true;
                        //switch in that fragment...
                        //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame,new MessagesOverviewFragment()).Commit();
                    }
                }
            }

            if (!startWithUserFragment)
            {
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomOverviewFragment(), OVERVIEW_FRAGMENT_TAG).Commit();
            }

            //this.SupportActionBar.SetBackgroundDrawable turn off overflow....

            //ListView userList = this.FindViewById<ListView>(Resource.Id.userList);

            //RefreshUserList();
        }


        private void onBackPressedAction(OnBackPressedCallback callback)
        {
            //if f is non null and f is visible then that means you are backing out from the inner user fragment..
            var f = SupportFragmentManager.FindFragmentByTag(INNER_FRAGMENT_TAG);
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
                callback.Enabled = false;
                OnBackPressedDispatcher.OnBackPressed();
                // we are now on outer — leave the callback disabled so predictive back works on the overview list
                return;
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
                    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(roomInfo), INNER_FRAGMENT_TAG).Commit();
                    backPressedCallback.Enabled = true;
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

        private const string INNER_FRAGMENT_TAG = "ChatroomInnerFragment";
        private const string OVERVIEW_FRAGMENT_TAG = "OuterListChatroomFragment";
        private const string INNER_FRAGMENT_BACKSTACK = "ChatroomInnerFragmentBackStack";
        private const string SAVE_STATE_AT_INNER_KEY = "SaveStateAtChatroomInner";

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
            var f = SupportFragmentManager.FindFragmentByTag(INNER_FRAGMENT_TAG);
            if (f != null && f.IsVisible)
            {
                outState.PutBoolean(SAVE_STATE_AT_INNER_KEY, true);
                Logger.Debug("SaveStateAtChatroomInner OnSaveInstanceState");
                SaveStartingRoomInfo(outState, f as ChatroomInnerFragment);
                Logger.Debug("currentlyInsideRoomName -- OnSaveInstanceState -- " + ChatroomController.currentlyInsideRoomName);
                //ChatroomController.currentlyInsideRoomName = ChatroomInnerFragment.OurRoomInfo.Name; //this sets it after we are leaving....
            }
            else
            {
                outState.PutBoolean(SAVE_STATE_AT_INNER_KEY, false);
            }
            base.OnSaveInstanceState(outState);
        }




        public override bool OnSupportNavigateUp()
        {
            OnBackPressedDispatcher.OnBackPressed();
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


        private void Input_KeyPress(object sender, View.KeyEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static System.String LocaleToEmoji(string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                int unicode = 0x1F310;
                return new System.String(Java.Lang.Character.ToChars(unicode));
            }
            int firstLetter = Java.Lang.Character.CodePointAt(locale, 0) - 0x41 + 0x1F1E6;
            int secondLetter = Java.Lang.Character.CodePointAt(locale, 1) - 0x41 + 0x1F1E6;
            return new System.String(Java.Lang.Character.ToChars(firstLetter)) + new System.String(Java.Lang.Character.ToChars(secondLetter));
        }

        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;


        public void ChangeToInnerFragment(Soulseek.RoomInfo roomInfo)
        {
            if (IsFinishing || IsDestroyed)
            {
                return;
            }
            else
            {
                //when you first click a room before you have joined, all the info you have is roomname and count. userlist is empty.
                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.content_frame, new ChatroomInnerFragment(roomInfo), INNER_FRAGMENT_TAG).AddToBackStack(INNER_FRAGMENT_BACKSTACK).Commit();
                backPressedCallback.Enabled = true;
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
            chatNameInput.FocusChange += UiHelpers.OnFocusAdjustNothing;

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

    }


}
