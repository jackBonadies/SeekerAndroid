using Android.Content;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Seeker.Browse;
using Seeker.Helpers;
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
                var activity = SeekerState.ActiveActivityRef;
                if (activity != null)
                {
                    var color = UiHelpers.GetColorFromAttribute(activity, Resource.Attribute.colorPrimary);
                    activity.Window?.SetStatusBarColor(color);
                }
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                int selectedCount = Adapter.SelectedPositions.Count;
                bool singleSelected = selectedCount == 1;
                bool anySelected = selectedCount > 0;

                // Single-select only items
                menu.FindItem(Resource.Id.action_browse_files)?.SetVisible(singleSelected);
                menu.FindItem(Resource.Id.action_search_files)?.SetVisible(singleSelected);
                menu.FindItem(Resource.Id.action_get_user_info)?.SetVisible(singleSelected);
                menu.FindItem(Resource.Id.action_delete_messages)?.SetVisible(singleSelected);

                // Single + multi items
                menu.FindItem(Resource.Id.action_add_to_user_list)?.SetVisible(anySelected);
                menu.FindItem(Resource.Id.action_ignore)?.SetVisible(anySelected);

                if (singleSelected)
                {
                    string username = Adapter.localDataSet[Adapter.SelectedPositions[0]];
                    UiHelpers.SetMenuTitles(menu, username);
                    UiHelpers.SetIgnoreAddExclusive(menu, username);
                }
                else if (anySelected)
                {
                    menu.FindItem(Resource.Id.action_add_to_user_list)?.SetTitle(Resource.String.add_to_user_list);
                    menu.FindItem(Resource.Id.action_ignore)?.SetTitle(Resource.String.ignore_user);
                }

                return true;
            }

            private List<string> GetSelectedUsernames()
            {
                return Adapter.SelectedPositions
                    .Where(i => i < Adapter.localDataSet.Count)
                    .Select(i => Adapter.localDataSet[i])
                    .ToList();
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                switch (item.ItemId)
                {
                    case Resource.Id.action_browse_files:
                    {
                        string username = GetSelectedUsernames().FirstOrDefault();
                        if (username != null)
                        {
                            Action<View> action = new Action<View>((v) =>
                            {
                                Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                                intent.PutExtra(MainActivity.GoToBrowseExtra, true);
                                SeekerState.ActiveActivityRef.StartActivity(intent);
                            });
                            View snackView = SeekerState.ActiveActivityRef.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId);
                            BrowseService.RequestFilesApi(username, snackView, action, null);
                        }
                        MessagesOverviewActionMode?.Finish();
                        return true;
                    }
                    case Resource.Id.action_search_files:
                    {
                        string username = GetSelectedUsernames().FirstOrDefault();
                        if (username != null)
                        {
                            SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                            SearchTabHelper.SearchTargetChosenUser = username;
                            Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                            intent.PutExtra(MainActivity.GoToSearchExtra, true);
                            SeekerState.ActiveActivityRef.StartActivity(intent);
                        }
                        MessagesOverviewActionMode?.Finish();
                        return true;
                    }
                    case Resource.Id.action_get_user_info:
                    {
                        string username = GetSelectedUsernames().FirstOrDefault();
                        if (username != null)
                        {
                            RequestedUserInfoHelper.RequestUserInfoApi(username);
                        }
                        MessagesOverviewActionMode?.Finish();
                        return true;
                    }
                    case Resource.Id.action_delete_messages:
                        Frag.DeleteBatchSelected();
                        return true;
                    case Resource.Id.action_add_to_user_list:
                    {
                        var selectedUsernames = GetSelectedUsernames();
                        if (selectedUsernames.Count == 1)
                        {
                            UiHelpers.HandleCommonContextMenuActions(
                                item.TitleFormatted.ToString(), selectedUsernames[0],
                                SeekerState.ActiveActivityRef,
                                SeekerState.ActiveActivityRef.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId));
                        }
                        else
                        {
                            foreach (string username in selectedUsernames)
                            {
                                if (!UserListService.Instance.ContainsUser(username))
                                {
                                    UserListService.AddUserAPI(SeekerState.ActiveActivityRef, username, new Action(() => { }));
                                }
                            }
                        }
                        MessagesOverviewActionMode?.Finish();
                        return true;
                    }
                    case Resource.Id.action_ignore:
                    {
                        var selectedUsernames = GetSelectedUsernames();
                        if (selectedUsernames.Count == 1)
                        {
                            UiHelpers.HandleCommonContextMenuActions(
                                item.TitleFormatted.ToString(), selectedUsernames[0],
                                SeekerState.ActiveActivityRef,
                                SeekerState.ActiveActivityRef.FindViewById<ViewGroup>(Resource.Id.messagesMainLayoutId));
                        }
                        else
                        {
                            foreach (string username in selectedUsernames)
                            {
                                if (!SeekerApplication.IsUserInIgnoreList(username))
                                {
                                    SeekerApplication.AddToIgnoreListFeedback(SeekerState.ActiveActivityRef, username);
                                }
                            }
                        }
                        MessagesOverviewActionMode?.Finish();
                        return true;
                    }
                    case Resource.Id.action_delete_selected_batch:
                        Frag.DeleteBatchSelected();
                        return true;
                    case Resource.Id.select_all:
                        Adapter.SelectedPositions.Clear();
                        int cnt = Adapter.ItemCount;
                        for (int i = 0; i < cnt; i++)
                        {
                            Adapter.SelectedPositions.Add(i);
                        }
                        Adapter.NotifyDataSetChanged();
                        MessagesOverviewActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                        MessagesOverviewActionMode.Invalidate();
                        return true;
                    case Resource.Id.invert_selection:
                        List<int> oldOnes = Adapter.SelectedPositions.ToList();
                        Adapter.SelectedPositions.Clear();
                        List<int> all = new List<int>();
                        int cnt1 = Adapter.ItemCount;
                        for (int i = 0; i < cnt1; i++)
                        {
                            all.Add(i);
                        }
                        Adapter.SelectedPositions = all.Except(oldOnes).ToList();
                        Adapter.NotifyDataSetChanged();
                        if (Adapter.SelectedPositions.Count == 0)
                        {
                            MessagesOverviewActionMode?.Finish();
                        }
                        else
                        {
                            MessagesOverviewActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), Adapter.SelectedPositions.Count.ToString());
                            MessagesOverviewActionMode.Invalidate();
                        }
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
            if (cnt == 0)
            {
                MessagesOverviewActionMode?.Finish();
            }
            else
            {
                MessagesOverviewActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                MessagesOverviewActionMode.Invalidate();
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

            MessagesOverviewActionModeCallbackInstance = new MessagesOverviewActionModeCallback() { Adapter = adapter, Frag = this };
            MessagesOverviewActionMode = (SeekerState.ActiveActivityRef as MessagesActivity).StartSupportActionMode(MessagesOverviewActionModeCallbackInstance);
            adapter.IsInBatchSelectMode = true;
            ToggleBatchSelect(position);
            adapter.NotifyDataSetChanged();
        }
    }
}
