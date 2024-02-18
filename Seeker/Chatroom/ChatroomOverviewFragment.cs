using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Chatroom
{
    public class ChatroomOverviewFragment : AndroidX.Fragment.App.Fragment
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

            HookUpOverviewEventHandlers(true);

            created = true;
            return rootView;
        }

        private void HookUpOverviewEventHandlers(bool binding)
        {
            ChatroomController.RoomNowHasUnreadMessages -= OnRoomNowHasUnreadMessages;
            ChatroomController.CurrentlyJoinedRoomHasUpdated -= OnCurrentConnectedChanged;
            ChatroomController.CurrentlyJoinedRoomsCleared -= OnCurrentConnectedCleared;
            ChatroomController.JoinedRoomsHaveUpdated -= OnJoinedRoomsHaveUpdated;
            if (binding)
            {
                ChatroomController.RoomNowHasUnreadMessages += OnRoomNowHasUnreadMessages;
                ChatroomController.CurrentlyJoinedRoomHasUpdated += OnCurrentConnectedChanged;
                ChatroomController.CurrentlyJoinedRoomsCleared += OnCurrentConnectedCleared;
                ChatroomController.JoinedRoomsHaveUpdated += OnJoinedRoomsHaveUpdated;
            }
        }

        /// <summary>
        /// This is due to log out.
        /// In which case grey out the joined rooms.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="rooms"></param>
        public void OnCurrentConnectedCleared(object sender, List<string> rooms)
        {
            MainActivity.LogDebug("OnCurrentConnectedCleared");
            SeekerState.ActiveActivityRef?.RunOnUiThread(() => { this.recyclerAdapter?.notifyRoomStatusesChanged(rooms); });
        }

        public void OnRoomNowHasUnreadMessages(object sender, string room)
        {
            SeekerState.ActiveActivityRef?.RunOnUiThread(() => { this.recyclerAdapter?.notifyRoomStatusChanged(room); });
        }

        /// <summary>
        /// This is when we re-connect and successfully send server join message.
        /// We ungrey if we were previously greyed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="room"></param>
        public void OnCurrentConnectedChanged(object sender, string room)
        {
            MainActivity.LogDebug("OnCurrentConnectedChanged");
            SeekerState.ActiveActivityRef?.RunOnUiThread(() => { this.recyclerAdapter?.notifyRoomStatusChanged(room); });
        }

        public void OnJoinedRoomsHaveUpdated(object sender, EventArgs e)
        {
            MainActivity.LogDebug("OnJoinedRoomsHaveUpdated");
            ChatroomController.RoomListParsed = ChatroomController.GetParsedList(ChatroomController.RoomList); //reparse this for our newly joined rooms.
            internalList = ChatroomController.RoomList;
            internalListParsed = ChatroomController.RoomListParsed;
            this.UpdateChatroomList();
        }

        public override void OnResume()
        {
            MainActivity.LogDebug("overview on resume");
            MainActivity.LogDebug("hook up chat overview event handlers ");
            HookUpOverviewEventHandlers(true);
            recyclerAdapter?.NotifyDataSetChanged();
            base.OnResume();
        }

        public override void OnPause()
        {
            MainActivity.LogDebug("overview on pause");
            HookUpOverviewEventHandlers(false);
            base.OnPause();
        }

        private void FilterChatroomView_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            FilterString = e.NewText;
            this.UpdateChatroomList();
        }

        private static List<Soulseek.RoomInfo> FilterRoomList(List<Soulseek.RoomInfo> original)
        {
            if (FilterString != string.Empty)
            {
                return original.Where((roomInfo) => { return roomInfo.Name.Contains(FilterString, StringComparison.InvariantCultureIgnoreCase); }).ToList();
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
            activity?.RunOnUiThread(new Action(() =>
            {
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
                MainActivity.LogDebug("on chatroom overview attach");
                ChatroomController.RoomListReceived -= OnChatListReceived;
                ChatroomController.RoomListReceived += OnChatListReceived;
            }
            base.OnAttach(activity);
        }

        //public override void OnDetach()
        //{
        //    MainActivity.LogDebug("chat overview OnDetach -- nulling");
        //    base.OnDetach();
        //}
    }

}