using Android.Content;
using Android.Widget;
using Common;
using Google.Android.Material.BottomNavigation;
using Seeker.Helpers.ActionSheet;
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
        /// Secondary navigation. Account/login, Users, Wishlist, Chatrooms, Settings,
        /// About and Shutdown all live here now that Account is not a bottom-nav tab.
        /// Built on the existing ActionSheet primitive.
        /// </summary>
        private void ShowSecondaryNavigationSheet()
        {
            var config = new ActionSheetConfig();

            var account = new ActionSheetSection { HeaderText = GetString(Resource.String.section_caps_account) };
            account.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.folder_account_outline,
                Label = GetString(Resource.String.nav_account),
                OnClick = GoToAccountPage,
            });
            account.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.baseline_group_white_24,
                Label = GetString(Resource.String.target_user_list),
                OnClick = () =>
                {
                    Intent intent = new Intent(this, typeof(UserListActivity));
                    StartActivityForResult(intent, 141);
                },
            });
            account.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.schedule_clock_wishlist,
                Label = GetString(Resource.String.wishlist),
                OnClick = GoToWishlist,
            });
            config.Sections.Add(account);

            var communication = new ActionSheetSection { HeaderText = GetString(Resource.String.section_caps_communication) };
            communication.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.outline_forum_white_24,
                Label = GetString(Resource.String.chatrooms),
                OnClick = () =>
                {
                    Intent intent = new Intent(this, typeof(ChatroomActivity));
                    StartActivityForResult(intent, 143);
                },
            });
            config.Sections.Add(communication);

            var setup = new ActionSheetSection { HeaderText = GetString(Resource.String.section_caps_setup) };
            setup.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.baseline_settings_white_24,
                Label = GetString(Resource.String.settings),
                OnClick = () =>
                {
                    Intent intent = new Intent(this, typeof(SettingsActivity));
                    StartActivityForResult(intent, 140);
                },
            });
            setup.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.information_outline,
                Label = GetString(Resource.String.about_title),
                OnClick = () => Seeker.Settings.Rows.AboutBottomSheet.Show(this),
            });
            setup.Rows.Add(new ActionSheetRow
            {
                Destructive = true,
                IconResId = Resource.Drawable.power_settings_new_28dp,
                Label = GetString(Resource.String.shutdown),
                OnClick = () =>
                {
                    Intent intent = new Intent(this, typeof(CloseActivity));
                    intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    StartActivity(intent);
                    FinishAndRemoveTask();
                },
            });
            config.Sections.Add(setup);

            UiHelpers.ShowActionSheetDialogSafe(SupportFragmentManager, config);
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
