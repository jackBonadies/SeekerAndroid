using Android.Content;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Helpers.AnchoredMenu;
using Seeker.Helpers.ActionSheet;
using Seeker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

using ActionMode = AndroidX.AppCompat.View.ActionMode;

namespace Seeker.Messages
{
    public partial class MessagesOverviewFragment
    {
        public static ActionMode MessagesOverviewActionMode = null;
        public static MessagesOverviewActionModeCallback MessagesOverviewActionModeCallbackInstance = null;

        public class MessagesOverviewActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            public MessagesOverviewRecyclerAdapter Adapter;
            public MessagesOverviewFragment Frag;

            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.messages_overview_batch_menu, menu);
                OverflowMenu.Attach(menu, Resource.Id.action_messages_batch_overflow, BuildOverflowMenu, dismissWithAnchor: true);
                UiHelpers.ApplyActionModeStatusBarInset(SeekerState.ActiveActivityRef);
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                bool anySelected = Adapter.SelectedPositions.Count > 0;
                menu.FindItem(Resource.Id.action_mark_as_read)?.SetVisible(anySelected && GetSelectedUsernames().Any(u => MessageController.GetUnreadCount(u) > 0));
                return true;
            }

            private List<string> GetSelectedUsernames()
            {
                return Adapter.SelectedPositions
                    .Where(i => i < Adapter.localDataSet.Count)
                    .Select(i => Adapter.localDataSet[i])
                    .ToList();
            }

            private AnchoredMenuConfig BuildOverflowMenu()
            {
                if (MessagesOverviewActionMode == null)
                {
                    return null;
                }
                var activity = SeekerState.ActiveActivityRef;
                var selectedUsernames = GetSelectedUsernames();
                var config = new AnchoredMenuConfig();
                if (selectedUsernames.Count == 1)
                {
                    // same rows as the chat's own menu
                    var snackView = activity.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId);
                    var userSection = ActionSheetActions.BuildUserActionsSection(selectedUsernames[0], activity, snackView, new UserActionsOptions { ExcludeMessage = true });
                    config.AddRows(userSection);
                    foreach (var row in config.Rows)
                    {
                        var inner = row.OnClick;
                        row.OnClick = () => ThenFinish(inner);
                    }
                }
                else if (selectedUsernames.Count > 1)
                {
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.user_add,
                        Label = activity.GetString(Resource.String.add_to_user_list),
                        OnClick = () => ThenFinish(() =>
                        {
                            foreach (string username in GetSelectedUsernames())
                            {
                                if (!UserListService.Instance.ContainsUser(username))
                                {
                                    UserListService.AddUserAPI(SeekerState.ActiveActivityRef, username, new Action(() => { }));
                                }
                            }
                        })
                    });
                    config.Rows.Add(new AnchoredMenuRow
                    {
                        IconResId = Resource.Drawable.account_cancel_outline,
                        Label = activity.GetString(Resource.String.ignore_users),
                        Destructive = true,
                        OnClick = () => ThenFinish(() =>
                        {
                            foreach (string username in GetSelectedUsernames())
                            {
                                if (!UserListService.Instance.IsUserInIgnoreList(username))
                                {
                                    UserListService.AddToIgnoreListFeedback(SeekerState.ActiveActivityRef, username);
                                }
                            }
                        })
                    });
                }
                config.AddDivider();
                config.AddRow(Resource.Drawable.select_all_24, activity.GetString(Resource.String.SelectAll), () => IfActive(SelectAll));
                config.AddRow(Resource.Drawable.ink_selection_24, activity.GetString(Resource.String.InvertSelection), () => IfActive(InvertSelection));
                return config;
            }

            // the mode can finish while the menu is open
            private static void IfActive(Action action)
            {
                if (MessagesOverviewActionMode != null)
                {
                    action();
                }
            }

            private static void ThenFinish(Action action)
            {
                IfActive(() =>
                {
                    action?.Invoke();
                    MessagesOverviewActionMode?.Finish();
                });
            }

            private void SelectAll()
            {
                BatchSelection.SelectAll(Adapter.SelectedPositions, BatchSelection.Positions(Adapter.ItemCount));
                Adapter.NotifyDataSetChanged();
                BatchSelection.ShowCount(MessagesOverviewActionMode, Adapter.SelectedPositions.Count);
            }

            private void InvertSelection()
            {
                BatchSelection.Invert(Adapter.SelectedPositions, BatchSelection.Positions(Adapter.ItemCount));
                Adapter.NotifyDataSetChanged();
                if (Adapter.SelectedPositions.Count == 0)
                {
                    MessagesOverviewActionMode?.Finish();
                }
                else
                {
                    BatchSelection.ShowCount(MessagesOverviewActionMode, Adapter.SelectedPositions.Count);
                }
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                switch (item.ItemId)
                {
                    case Resource.Id.action_delete_selected_batch:
                        Frag.DeleteBatchSelected();
                        return true;
                    case Resource.Id.action_mark_as_read:
                        MessageController.MarkAsRead(GetSelectedUsernames());
                        Adapter.NotifyDataSetChanged();
                        MessagesBroadcastReceiver.MarkAsReadFromNotification?.Invoke(null, null);
                        MessagesOverviewActionMode?.Finish();
                        return true;
                }
                return true;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                SeekerState.ActiveActivityRef?.Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);

                MessagesOverviewActionMode = null;
                Adapter.SelectedPositions.Clear();
                Adapter.IsInBatchSelectMode = false;
                Adapter.NotifyDataSetChanged();
            }
        }

        public void ToggleBatchSelect(int position)
        {
            var adapter = recyclerAdapter;
            if (adapter == null)
            {
                return;
            }

            if (adapter.SelectedPositions.Contains(position))
            {
                adapter.SelectedPositions.Remove(position);
            }
            else
            {
                adapter.SelectedPositions.Add(position);
            }
            adapter.NotifyItemChanged(position);

            int cnt = adapter.SelectedPositions.Count;
            var actionMode = MessagesOverviewActionMode;
            if (cnt == 0)
            {
                actionMode?.Finish();
            }
            else if (actionMode != null)
            {
                BatchSelection.ShowCount(actionMode, cnt);
            }
        }

        public void DeleteBatchSelected()
        {
            var adapter = recyclerAdapter;
            if (adapter == null)
            {
                return;
            }

            var selectedPositions = adapter.SelectedPositions.Where(i => i < adapter.localDataSet.Count).OrderByDescending(i => i).ToList();
            if (selectedPositions.Count == 0)
            {
                return;
            }

            var deletedDataList = new List<(string username, List<Message> messages, int readCount)>();
            foreach (int pos in selectedPositions)
            {
                string username = adapter.localDataSet[pos];
                var (deletedMessages, readCount) = MessageController.DeleteMessageFromUserWithUndo(username);
                deletedDataList.Add((username, deletedMessages, readCount));
            }

            MessagesOverviewActionMode?.Finish();
            RefreshAdapter();

            Snackbar sb = Snackbar.Make(this.View,
                string.Format(SeekerApplication.GetString(Resource.String.deleted_selected_message_histories), deletedDataList.Count.ToString()),
                Snackbar.LengthLong)
                .SetAction(Resource.String.undo, (Android.Views.View view) =>
                {
                    foreach (var data in deletedDataList)
                    {
                        MessageController.UndoDeleteMessagesFromUser(data);
                    }
                    RefreshAdapter();
                });
            sb.Show();
        }

        public void OnItemLongClick(int position)
        {
            if (MessagesOverviewActionMode != null)
            {
                ToggleBatchSelect(position);
                return;
            }
            var adapter = recyclerAdapter;
            if (adapter == null)
            {
                return;
            }

            var act = this.Activity as MessagesActivity ?? MessagesActivity.MessagesActivityRef;
            if (act == null)
            {
                Logger.Firebase("On Long Click Messages Activity is Null");
                return;
            }
            MessagesOverviewActionModeCallbackInstance = new MessagesOverviewActionModeCallback() { Adapter = adapter, Frag = this };
            MessagesOverviewActionMode = act.StartSupportActionMode(MessagesOverviewActionModeCallbackInstance);
            adapter.IsInBatchSelectMode = true;
            ToggleBatchSelect(position);
            adapter.NotifyDataSetChanged();
        }
    }
}
