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

namespace AndriodApp1.Messages
{
    public class MessagesOverviewFragment : AndroidX.Fragment.App.Fragment
    {
        private RecyclerView recyclerViewOverview;
        private LinearLayoutManager recycleLayoutManager;
        public MessagesOverviewRecyclerAdapter recyclerAdapter;
        private List<string> internalList = null;
        private TextView noMessagesView = null;
        private bool created = false;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MessageController.MessageReceived += OnMessageReceived;
            View rootView = inflater.Inflate(Resource.Layout.messages_overview, container, false);
            noMessagesView = rootView.FindViewById<TextView>(Resource.Id.noMessagesView);
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
                internalList = GetOverviewList();//MessageController.Messages.Keys.ToList();
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
            SoulSeekState.ActiveActivityRef.InvalidateOptionsMenu();
            if (MessagesActivity.FromDeleteMessage)
            {
                MessagesActivity.FromDeleteMessage = false;
                Snackbar sb = Snackbar.Make(SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content),
                        string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.deleted_message_history_with),
                        MessagesActivity.DELETED_USERNAME),
                        Snackbar.LengthLong)
                    .SetAction(Resource.String.undo, ItemTouchHelperMessageOverviewCallback.GetSnackBarAction(recyclerAdapter, true))
                    .SetActionTextColor(Resource.Color.lightPurpleNotTransparent);

                (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView)
                    .SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainTextColor));
                sb.Show();
            }
            MessagesBroadcastReceiver.MarkAsReadFromNotification += UpdateMarkAsReadFromNotif;
        }

        public override void OnPause()
        {
            base.OnPause();
            MessagesBroadcastReceiver.MarkAsReadFromNotification -= UpdateMarkAsReadFromNotif;
        }

        private void UpdateMarkAsReadFromNotif(object o, string uname)
        {
            recyclerAdapter.NotifyNameChanged(uname);
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

        public static List<string> GetOverviewList()
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
            internalList = GetOverviewList();
            if (internalList.Count != 0)
            {
                noMessagesView.Visibility = ViewStates.Gone;
            }
            recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
            recyclerViewOverview.SetAdapter(recyclerAdapter);
            recyclerAdapter.NotifyDataSetChanged();
        }

        public override void OnAttach(Context activity)
        {
            if (created) //attach can happen before we created our view...
            {
                internalList = GetOverviewList();
                if (internalList.Count != 0)
                {
                    noMessagesView.Visibility = ViewStates.Gone;
                }
                recyclerAdapter = new MessagesOverviewRecyclerAdapter(internalList); //this depends tightly on MessageController... since these are just strings..
                recyclerViewOverview.SetAdapter(recyclerAdapter);
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