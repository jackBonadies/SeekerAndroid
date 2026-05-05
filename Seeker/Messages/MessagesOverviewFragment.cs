using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Snackbar;
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
            recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
            recyclerViewOverview.SetAdapter(recyclerAdapter);
            recyclerViewOverview.SetLayoutManager(recycleLayoutManager);

            ItemTouchHelperMessageOverviewCallback itemTouchHelperMessageOverviewCallback = new ItemTouchHelperMessageOverviewCallback(recyclerAdapter, this);
            ItemTouchHelper itemTouchHelper = new ItemTouchHelper(itemTouchHelperMessageOverviewCallback);
            itemTouchHelper.AttachToRecyclerView(recyclerViewOverview);

            created = true;
            return rootView;
        }



        public override void OnResume()
        {
            base.OnResume();
            MessagesBroadcastReceiver.MarkAsReadFromNotification += UpdateMarkAsReadFromNotif;
            SeekerApplication.UserStatusChangedUIEvent += OnUserStatusChanged;
        }

        public override void OnPause()
        {
            base.OnPause();
            MessagesBroadcastReceiver.MarkAsReadFromNotification -= UpdateMarkAsReadFromNotif;
            SeekerApplication.UserStatusChangedUIEvent -= OnUserStatusChanged;
        }

        private void UpdateMarkAsReadFromNotif(object o, string uname)
        {
            recyclerAdapter.NotifyNameChanged(uname);
        }

        private void OnUserStatusChanged(object sender, string username)
        {
            if (MainActivity.OnUIthread())
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
                    return y.Value.Last().LocalDateTime.CompareTo(x.Value.Last().LocalDateTime);
                }
            }
        }

        public static List<string> GetSortedMessagesList()
        {
            var listToSort = MessageController.Messages.ToList();
            listToSort.Sort(new MessageOverviewComparer());
            return listToSort.Select((pair) => pair.Key).ToList();
        }

        public void OnMessageReceived(object sender, Message msg)
        {
            var activity = this.Activity != null ? this.Activity : MessagesActivity.MessagesActivityRef;
            activity.RunOnUiThread(new Action(() =>
            {
                this.RefreshAdapter();
            }));
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
                    recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList);
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

        private class OverviewMenuProvider : Java.Lang.Object, AndroidX.Core.View.IMenuProvider
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
            }

            public void OnMenuClosed(IMenu menu)
            {
            }

            public bool OnMenuItemSelected(IMenuItem item)
            {
                var activity = fragment.Activity as MessagesActivity;
                if (activity == null)
                {
                    return false;
                }
                switch (item.ItemId)
                {
                    case Resource.Id.message_user_action:
                        activity.ShowEditTextMessageUserDialog();
                        return true;
                    case Resource.Id.action_delete_all_messages:
                        if (MessageController.Messages.Count == 0)
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.deleted_all_no_messages), ToastLength.Long);
                            return true;
                        }
                        var (deletedAllMessages, deletedAllLastReadMessageCounts) = MessageController.DeleteAllMessagesWithUndo();
                        fragment.RefreshAdapter();
                        Snackbar sb = Snackbar.Make(fragment.View, SeekerState.ActiveActivityRef.GetString(Resource.String.deleted_all_messages), Snackbar.LengthLong)
                            .SetAction("Undo", (View v) => activity.GetUndoDeleteAllSnackBarAction(deletedAllMessages, deletedAllLastReadMessageCounts));
                        sb.Show();
                        return true;
                }
                return false;
            }
        }
    }
}