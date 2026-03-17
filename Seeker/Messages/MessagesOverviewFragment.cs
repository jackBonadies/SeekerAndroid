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
            SeekerState.ActiveActivityRef.InvalidateOptionsMenu();
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
            // Compares by UserCount then Name
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
                //if(internalList!=null && internalList.Contains(msg.Username))
                //{
                //    //update this one...
                //    recyclerAdapter.NotifyDataSetChanged();//NotifyItemChanged(internalList.IndexOf(msg.Username));
                //}
                //else
                //{
                this.RefreshAdapter();
                //}
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
    }
}