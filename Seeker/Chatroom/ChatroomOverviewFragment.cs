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
using Seeker.Helpers;

namespace Seeker.Chatroom
{
    public class ChatroomOverviewFragment : AndroidX.Fragment.App.Fragment
    {
        private RecyclerView recyclerViewOverview;
        private LinearLayoutManager recycleLayoutManager;
        private ChatroomOverviewRecyclerAdapter recyclerAdapter;
        private SearchView filterChatroomView;
        private static string FilterString = string.Empty;
        private View chatroomsListLoadingView = null;
        private bool created = false;

        private static List<Soulseek.RoomInfo> CurrentParsedList =>
            ChatroomController.RoomListParsed ?? new List<Soulseek.RoomInfo>();
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Logger.Debug("create chatroom overview view");
            ChatroomController.RoomListReceived += OnChatListReceived;
            View rootView = inflater.Inflate(Resource.Layout.chatroom_overview, container, false);
            chatroomsListLoadingView = rootView.FindViewById<View>(Resource.Id.chatroomListLoading);
            filterChatroomView = rootView.FindViewById<SearchView>(Resource.Id.filterChatroom);
            filterChatroomView.QueryTextChange += FilterChatroomView_QueryTextChange;
            recyclerViewOverview = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewOverview);
            recyclerViewOverview.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(recyclerViewOverview, new BottomOnlyInsetsListener());
            recycleLayoutManager = new LinearLayoutManager(Activity);
            if (ChatroomController.RoomList == null)
            {
                chatroomsListLoadingView.Visibility = ViewStates.Visible;
                ChatroomController.GetRoomListApi();
            }
            else
            {
                chatroomsListLoadingView.Visibility = ViewStates.Gone;
                ChatroomController.RefreshParsedList();
            }
            recyclerAdapter = new ChatroomOverviewRecyclerAdapter(FilterRoomList(CurrentParsedList)); //this depends tightly on MessageController... since these are just strings..
            recyclerViewOverview.SetAdapter(recyclerAdapter);
            recyclerViewOverview.SetLayoutManager(recycleLayoutManager);

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
            Logger.Debug("OnCurrentConnectedCleared");
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
            Logger.Debug("OnCurrentConnectedChanged");
            SeekerState.ActiveActivityRef?.RunOnUiThread(() => { this.recyclerAdapter?.notifyRoomStatusChanged(room); });
        }

        public void OnJoinedRoomsHaveUpdated(object sender, EventArgs e)
        {
            Logger.Debug("OnJoinedRoomsHaveUpdated");
            ChatroomController.RefreshParsedList(); //reparse this for our newly joined rooms.
            this.UpdateChatroomList();
        }

        public override void OnResume()
        {
            Logger.Debug("overview on resume");
            Logger.Debug("hook up chat overview event handlers ");
            HookUpOverviewEventHandlers(true);
            recyclerAdapter?.NotifyDataSetChanged();
            base.OnResume();
        }

        public override void OnPause()
        {
            Logger.Debug("overview on pause");
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
            this.UpdateChatroomList();
        }

        private void UpdateChatroomList()
        {
            Logger.Debug("update chatroom list");
            var filteredRoomList = FilterRoomList(CurrentParsedList);
            var activity = this.Activity != null ? this.Activity : ChatroomActivity.ChatroomActivityRef;
            activity?.RunOnUiThread(new Action(() =>
            {
                chatroomsListLoadingView.Visibility = ViewStates.Gone;
                recyclerAdapter?.SetItems(filteredRoomList);
            }
            ));
        }



        public override void OnAttach(Context activity)
        {
            if (created) //attach can happen before we created our view...
            {
                ChatroomController.RefreshParsedList();
                UpdateChatroomList();
                Logger.Debug("on chatroom overview attach");
                ChatroomController.RoomListReceived -= OnChatListReceived;
                ChatroomController.RoomListReceived += OnChatListReceived;
            }
            base.OnAttach(activity);
        }
    }

}