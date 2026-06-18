using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;
using Soulseek;
using System.Collections.Generic;
using System.Linq;

namespace Seeker.Chatroom
{
    public class AllTickersDialog : BottomSheetDialogFragment
    {
        private const string ArgRoomName = "roomName";

        private string ourRoomName = string.Empty;
        private RecyclerView recyclerView;
        private ViewFlipper stateFlipper;
        private TickerRowAdapter adapter;

        private enum TickerDisplayState
        {
            Tickers = 0,
            Empty = 1,
            Loading = 2,
        }

        public AllTickersDialog(string ourRoomName)
        {
            this.ourRoomName = ourRoomName;
            var args = new Bundle();
            args.PutString(ArgRoomName, ourRoomName);
            Arguments = args;
        }

        public AllTickersDialog()
        {
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Arguments != null)
            {
                ourRoomName = Arguments.GetString(ArgRoomName, ourRoomName);
            }

            var root = inflater.Inflate(Resource.Layout.all_ticker_dialog, container, false);

            stateFlipper = root.FindViewById<ViewFlipper>(Resource.Id.tickerStateFlipper);
            recyclerView = root.FindViewById<RecyclerView>(Resource.Id.recyclerViewTickers);
            recyclerView.SetLayoutManager(new LinearLayoutManager(root.Context));

            adapter = new TickerRowAdapter(new List<RoomTicker>());
            recyclerView.SetAdapter(adapter);

            return root;
        }

        public override void OnResume()
        {
            base.OnResume();
            ChatroomController.RoomTickerListReceived += OnRoomTickerListReceived;
            ApplyState();
        }

        public override void OnPause()
        {
            base.OnPause();
            ChatroomController.RoomTickerListReceived -= OnRoomTickerListReceived;
        }

        private void OnRoomTickerListReceived(object sender, Soulseek.RoomTickerListReceivedEventArgs e)
        {
            if (e.RoomName != ourRoomName)
            {
                return;
            }
            Activity?.RunOnUiThread(ApplyState);
        }

        private void ApplyState()
        {
            if (stateFlipper == null)
            {
                return;
            }
            if (ChatroomController.JoinedRoomTickers.TryGetValue(ourRoomName, out var stored))
            {
                if (stored.Count > 0)
                {
                    var list = stored.ToList();
                    list.Reverse();
                    adapter.SetData(list);
                    SetState(TickerDisplayState.Tickers);
                }
                else
                {
                    SetState(TickerDisplayState.Empty);
                }
            }
            else
            {
                SetState(TickerDisplayState.Loading);
            }
        }

        private void SetState(TickerDisplayState state)
        {
            if (stateFlipper.DisplayedChild != (int)state)
            {
                stateFlipper.DisplayedChild = (int)state;
            }
        }

        private sealed class TickerRowAdapter : RecyclerView.Adapter
        {
            private readonly List<RoomTicker> tickers;

            public TickerRowAdapter(List<RoomTicker> tickers)
            {
                this.tickers = tickers;
            }

            public void SetData(List<RoomTicker> newData)
            {
                tickers.Clear();
                tickers.AddRange(newData);
                NotifyDataSetChanged();
            }

            public override int ItemCount => tickers.Count;

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ticker_row, parent, false);
                return new TickerRowHolder(view);
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var h = (TickerRowHolder)holder;
                var ticker = tickers[position];
                h.Message.SetText(UiHelpers.BuildTickerSpan(ticker, h.Message.Context), TextView.BufferType.Spannable);
                h.Separator.Visibility = position == tickers.Count - 1 ? ViewStates.Gone : ViewStates.Visible;
            }

        }

        private sealed class TickerRowHolder : RecyclerView.ViewHolder
        {
            public TextView Message;
            public View Separator;

            public TickerRowHolder(View view) : base(view)
            {
                Message = view.FindViewById<TextView>(Resource.Id.tickerRowMessage);
                Separator = view.FindViewById<View>(Resource.Id.tickerRowSeparator);
            }
        }
    }
}
