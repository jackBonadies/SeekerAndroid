using Android.Content;
using Android.Widget;
using Android.Views;
using AndroidX.Core.View;
using Common;
using Google.Android.Material.BottomNavigation;
using Seeker.Messages;
using System;

namespace Seeker
{
    public partial class MainActivity
    {
        private int lastPrimaryPage = MainNavigation.DefaultLandingPage;

        protected override void OnResume()
        {
            base.OnResume();
            MessageController.MessageReceived += OnMessageReceivedUpdateMessagesBadge;
            MessagesBroadcastReceiver.MarkAsReadFromNotification += OnMarkAsReadUpdateMessagesBadge;
            UpdateMessagesBadge();
        }

        protected override void OnPause()
        {
            MessageController.MessageReceived -= OnMessageReceivedUpdateMessagesBadge;
            MessagesBroadcastReceiver.MarkAsReadFromNotification -= OnMarkAsReadUpdateMessagesBadge;
            base.OnPause();
        }

        private void OnMessageReceivedUpdateMessagesBadge(object sender, Message message)
        {
            RunOnUiThread(UpdateMessagesBadge);
        }

        // IMessagesOverviewHost: the shell reuses the exact MessagesOverviewFragment,
        // adapter and action mode; opening a thread is still the MessagesActivity intent.
        public void ShowNewMessageDialog()
        {
            MessagesComposeDialog.Show(this, OpenMessagesConversation);
        }

        public void OpenMessagesConversation(string username)
        {
            Intent intent = new Intent(this, typeof(MessagesActivity));
            intent.PutExtra(MessageController.FromUserName, username);
            intent.PutExtra(MessageController.ComingFromMessageTapped, true);
            StartActivityForResult(intent, 142);
        }

        private void OnMarkAsReadUpdateMessagesBadge(object sender, string username)
        {
            RunOnUiThread(UpdateMessagesBadge);
        }

        /// <summary>
        /// Unread bubble on the Messages bottom-nav item. Kept deliberately small:
        /// MessageController is already the source of truth, so the shell only mirrors
        /// GetTotalUnreadCount() and never marks anything read by itself.
        /// </summary>
        private void UpdateMessagesBadge()
        {
            var navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            if (navigation == null)
            {
                return;
            }
            int unread = MessageController.GetTotalUnreadCount();
            if (unread <= 0)
            {
                navigation.RemoveBadge(Resource.Id.navigation_messages);
                return;
            }
            var badge = navigation.GetOrCreateBadge(Resource.Id.navigation_messages);
            if (badge == null)
            {
                return;
            }
            badge.MaxCharacterCount = 3;
            badge.Number = unread;
        }

        /// <summary>
        /// Secondary navigation lives in the app's left drawer.  Keeping it out of a bottom
        /// sheet makes the hamburger behave like standard Android navigation.
        /// </summary>
        private void ShowSecondaryNavigationSheet()
        {
            navigationDrawer?.OpenDrawer(GravityCompat.Start);
        }

        private void InitializeNavigationDrawer()
        {
            BindDrawerItem(Resource.Id.drawerAccount, GoToAccountPage);
            BindDrawerItem(Resource.Id.drawerUsers, () => StartActivityForResult(new Intent(this, typeof(UserListActivity)), 141));
            BindDrawerItem(Resource.Id.drawerWishlist, GoToWishlist);
            BindDrawerItem(Resource.Id.drawerChatrooms, () => StartActivityForResult(new Intent(this, typeof(ChatroomActivity)), 143));
            BindDrawerItem(Resource.Id.drawerSettings, () => StartActivityForResult(new Intent(this, typeof(SettingsActivity)), 140));
            BindDrawerItem(Resource.Id.drawerAbout, () => Seeker.Settings.Rows.AboutBottomSheet.Show(this));
            BindDrawerItem(Resource.Id.drawerShutdown, () =>
            {
                var intent = new Intent(this, typeof(CloseActivity));
                intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                StartActivity(intent);
                FinishAndRemoveTask();
            });
        }

        private void BindDrawerItem(int viewId, Action action)
        {
            FindViewById<View>(viewId)?.Click += (sender, args) =>
            {
                navigationDrawer?.CloseDrawer(GravityCompat.Start);
                action();
            };
        }

        private void GoToAccountPage()
        {
            pager?.SetCurrentItem(MainNavigation.AccountPage, false);
        }

        private void GoToWishlist()
        {
            pager?.SetCurrentItem(MainNavigation.SearchPage, false);
            pager?.Post(() => SearchFragment.Instance?.ShowSearchTabsDialog());
        }
    }
}
