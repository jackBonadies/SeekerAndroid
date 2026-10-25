using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Snackbar;
using Seeker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Messages
{
    public partial class MessagesOverviewFragment : AndroidX.Fragment.App.Fragment
    {
        private RecyclerView recyclerViewOverview;
        private LinearLayoutManager recycleLayoutManager;
        public MessagesOverviewRecyclerAdapter recyclerAdapter;
        private List<string> internalList = null;
        private View noMessagesView = null;
        private bool created = false;
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            Activity?.AddMenuProvider(new OverviewMenuProvider(this), ViewLifecycleOwner, AndroidX.Lifecycle.Lifecycle.State.Resumed);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MessageController.MessageReceived += OnMessageReceived;
            View rootView = inflater.Inflate(Resource.Layout.messages_overview, container, false);
            noMessagesView = rootView.FindViewById<View>(Resource.Id.noMessagesView);
            if (MessageController.Messages == null || MessageController.Messages.Keys.Count == 0)
            {
                noMessagesView.Visibility = ViewStates.Visible;
            }
            else
            {
                noMessagesView.Visibility = ViewStates.Gone;
            }
            recyclerViewOverview = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewOverview);
            recyclerViewOverview.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(recyclerViewOverview, new Seeker.Helpers.BottomOnlyInsetsListener());

            recycleLayoutManager = new LinearLayoutManager(Activity);
            if (MessageController.Messages == null)
            {
                internalList = new List<string>();
            }
            else
            {
                internalList = GetSortedMessagesList();//MessageController.Messages.Keys.ToList();
            }
            recyclerAdapter = CreateAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
            recyclerViewOverview.SetAdapter(recyclerAdapter);
            recyclerViewOverview.SetLayoutManager(recycleLayoutManager);

            ItemTouchHelperMessageOverviewCallback itemTouchHelperMessageOverviewCallback = new ItemTouchHelperMessageOverviewCallback(recyclerAdapter, this);
            ItemTouchHelper itemTouchHelper = new ItemTouchHelper(itemTouchHelperMessageOverviewCallback);
            itemTouchHelper.AttachToRecyclerView(recyclerViewOverview);

            var fabNewMessage = rootView.FindViewById<Google.Android.Material.FloatingActionButton.FloatingActionButton>(Resource.Id.fabNewMessage);
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(fabNewMessage, new Seeker.Helpers.BottomMarginInsetsListener(fabNewMessage));
            fabNewMessage.Click += (s, e) => (Activity as IMessagesOverviewHost)?.ShowNewMessageDialog();

            created = true;
            return rootView;
        }



        public override void OnResume()
        {
            base.OnResume();
            MessagesBroadcastReceiver.MarkAsReadFromNotification += UpdateMarkAsReadFromNotif;
            SeekerApplication.UserStatusChangedUIEvent += OnUserStatusChanged;

            // The shell keeps this fragment alive while a thread is opened in MessagesActivity.
            // Returning from that thread must re-read the read markers (and any deletions done
            // there) instead of showing the stale snapshots from before the activity pause.
            if (created && recyclerAdapter != null)
            {
                RefreshAdapter();
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            MessagesBroadcastReceiver.MarkAsReadFromNotification -= UpdateMarkAsReadFromNotif;
            SeekerApplication.UserStatusChangedUIEvent -= OnUserStatusChanged;
        }

        private void UpdateMarkAsReadFromNotif(object o, string uname)
        {
            if (uname == null)
            {
                recyclerAdapter.NotifyDataSetChanged();
                return;
            }
            recyclerAdapter.NotifyNameChanged(uname);
        }

        private void OnUserStatusChanged(object sender, string username)
        {
            if (SeekerApplication.OnUIThread())
            {
                recyclerAdapter.NotifyNameChanged(username);
            }
            else
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                    recyclerAdapter.NotifyNameChanged(username));
            }
        }

        public class MessageOverviewComparer : IComparer<KeyValuePair<string, List<Message>>>
        {
            public int Compare(KeyValuePair<string, List<Message>> x, KeyValuePair<string, List<Message>> y)
            {
                if (x.Value.Count == 0 && y.Value.Count == 0)
                {
                    return 0;
                }
                else if (x.Value.Count == 0)
                {
                    return 1;
                }
                else if (y.Value.Count == 0)
                {
                    return -1;
                }
                else
                {
                    return y.Value.Last().UtcDateTime.CompareTo(x.Value.Last().UtcDateTime);
                }
            }
        }

        public static List<string> GetSortedMessagesList()
        {
            var listToSort = MessageController.Messages.ToList();
            listToSort.Sort(new MessageOverviewComparer());
            return listToSort.Select((pair) => pair.Key).ToList();
        }

        private MessagesOverviewRecyclerAdapter CreateAdapter(List<string> list)
        {
            return new MessagesOverviewRecyclerAdapter(list, OnOverviewItemClick, OnItemLongClick);
        }

        /// <summary>
        /// Single click. Batch mode toggles selection; otherwise the host opens the
        /// conversation (thread inside MessagesActivity, MessagesActivity intent from
        /// the shell), so no navigation logic is duplicated here.
        /// </summary>
        public void OnOverviewItemClick(int position)
        {
            var adapter = recyclerAdapter;
            if (adapter == null || position < 0 || position >= adapter.localDataSet.Count)
            {
                return;
            }
            if (adapter.IsInBatchSelectMode)
            {
                ToggleBatchSelect(position);
                return;
            }
            (Activity as IMessagesOverviewHost)?.OpenMessagesConversation(adapter.localDataSet[position]);
        }

        /// <summary>Anchors snackbars/action menus correctly in both host activities.</summary>
        public Android.Views.View SnackbarAnchor => this.View ?? Activity?.FindViewById(Android.Resource.Id.Content);

        public ViewGroup ContextMenuAnchor =>
            this.View as ViewGroup ?? Activity?.FindViewById<ViewGroup>(Android.Resource.Id.Content);

        public void OnMessageReceived(object sender, Message msg)
        {
            var activity = this.Activity ?? (Android.App.Activity)MessagesActivity.MessagesActivityRef ?? SeekerState.MainActivityRef;
            activity?.RunOnUiThread(new Action(() =>
            {
                this.RefreshAdapter();
            }));
        }

        /// <summary>Undo for "delete all" without depending on the hosting activity type.</summary>
        public void UndoDeleteAllMessages(Dictionary<string, List<Message>> deletedMessageDictionary, Dictionary<string, int> deletedMessageCountDictionary)
        {
            if (deletedMessageDictionary == null)
            {
                Logger.Firebase("failure on undo delete all. dict was null");
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_undo), ToastLength.Short);
                return;
            }

            foreach (var entry in deletedMessageDictionary)
            {
                MessageController.Messages[entry.Key] = entry.Value;
            }

            foreach (var entry in deletedMessageCountDictionary)
            {
                MessageController.LastReadMessageCounts[entry.Key] = entry.Value;
            }

            MessageController.SaveMessagesToSharedPrefs(SeekerState.SharedPreferences);
            MessageController.SaveLastReadCounts(SeekerState.SharedPreferences);
            RefreshAdapter();
        }

        public void RefreshAdapter()
        {
            MessagesOverviewActionMode?.Finish();
            var newList = GetSortedMessagesList();
            if (newList.Count != 0)
            {
                noMessagesView.Visibility = ViewStates.Gone;
            }
            else
            {
                noMessagesView.Visibility = ViewStates.Visible;
            }
            var diff = DiffUtil.CalculateDiff(new MessageOverviewDiffCallback(internalList, newList), true);
            internalList = newList;
            recyclerAdapter.localDataSet = newList;
            diff.DispatchUpdatesTo(recyclerAdapter);
        }

        public override void OnAttach(Context activity)
        {
            if (created) //attach can happen before we created our view...
            {
                MessagesOverviewActionMode?.Finish();
                var newList = GetSortedMessagesList();
                if (newList.Count != 0)
                {
                    noMessagesView.Visibility = ViewStates.Gone;
                }
                if (recyclerAdapter == null)
                {
                    recyclerAdapter = CreateAdapter(internalList);
                }
                var diff = DiffUtil.CalculateDiff(new MessageOverviewDiffCallback(internalList, newList), true);
                internalList = newList;
                recyclerAdapter.localDataSet = newList;
                diff.DispatchUpdatesTo(recyclerAdapter);
                MessageController.MessageReceived -= OnMessageReceived;
                MessageController.MessageReceived += OnMessageReceived;
            }
            base.OnAttach(activity);
        }

        public override void OnDetach()
        {
            MessageController.MessageReceived -= OnMessageReceived;
            base.OnDetach();
        }

        private sealed class OverviewMenuProvider : Java.Lang.Object, AndroidX.Core.View.IMenuProvider
        {
            private readonly MessagesOverviewFragment fragment;

            public OverviewMenuProvider(MessagesOverviewFragment fragment)
            {
                this.fragment = fragment;
            }

            public void OnCreateMenu(IMenu menu, MenuInflater menuInflater)
            {
                menuInflater.Inflate(Resource.Menu.messages_overview_list_menu, menu);
            }

            public void OnPrepareMenu(IMenu menu)
            {
                menu.FindItem(Resource.Id.action_delete_all_messages)?.SetVisible(!MessageController.Messages.IsEmpty);
                menu.FindItem(Resource.Id.mark_all_as_read_action)?.SetVisible(MessageController.GetTotalUnreadCount() > 0);
            }

#pragma warning disable S1186 // Required override - omitting causes java.lang.AbstractMethodError at runtime
            public void OnMenuClosed(IMenu menu)
            {
            }
#pragma warning restore S1186

            public bool OnMenuItemSelected(IMenuItem item)
            {
                switch (item.ItemId)
                {
                    case Resource.Id.mark_all_as_read_action:
                        MessageController.MarkAllAsRead();
                        fragment.recyclerAdapter.NotifyDataSetChanged();
                        MessagesBroadcastReceiver.MarkAsReadFromNotification?.Invoke(null, null);
                        return true;
                    case Resource.Id.action_delete_all_messages:
                        if (MessageController.Messages.IsEmpty)
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.deleted_all_no_messages), ToastLength.Long);
                            return true;
                        }
                        var (deletedAllMessages, deletedAllLastReadMessageCounts) = MessageController.DeleteAllMessagesWithUndo();
                        fragment.RefreshAdapter();
                        Snackbar sb = Snackbar.Make(fragment.SnackbarAnchor, SeekerApplication.GetString(Resource.String.deleted_all_messages), Snackbar.LengthLong)
                            .SetAction(Resource.String.undo, (View v) => fragment.UndoDeleteAllMessages(deletedAllMessages, deletedAllLastReadMessageCounts));
                        sb.Show();
                        return true;
                }
                return false;
            }
        }
    }
}