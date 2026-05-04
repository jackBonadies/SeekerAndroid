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

            recyclerView = root.FindViewById<RecyclerView>(Resource.Id.recyclerViewTickers);
            recyclerView.SetLayoutManager(new LinearLayoutManager(root.Context));

            var tickers = new List<RoomTicker>();
            if (ChatroomController.JoinedRoomTickers.TryGetValue(ourRoomName, out var stored))
            {
                tickers = stored.ToList();
                tickers.Reverse();
            }

            recyclerView.SetAdapter(new TickerRowAdapter(tickers));

            return root;
        }

        private class TickerRowAdapter : RecyclerView.Adapter
        {
            private readonly List<RoomTicker> tickers;

            public TickerRowAdapter(List<RoomTicker> tickers)
            {
                this.tickers = tickers;
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
                h.Message.SetText(BuildTickerSpan(ticker), TextView.BufferType.Spannable);
                h.Separator.Visibility = position == tickers.Count - 1 ? ViewStates.Gone : ViewStates.Visible;
            }

            private static SpannableStringBuilder BuildTickerSpan(RoomTicker ticker)
            {
                var builder = new SpannableStringBuilder();
                if (string.IsNullOrEmpty(ticker.Username))
                {
                    builder.Append(ticker.Message);
                    builder.SetSpan(new StyleSpan(TypefaceStyle.Italic), 0, builder.Length(), SpanTypes.InclusiveExclusive);
                }
                else
                {
                    builder.Append(ticker.Message);
                    var messageEnd = builder.Length();
                    builder.Append(" -" + ticker.Username);
                    builder.SetSpan(new StyleSpan(TypefaceStyle.Bold), messageEnd, builder.Length(), SpanTypes.ExclusiveExclusive);
                }
                return builder;
            }
        }

        private class TickerRowHolder : RecyclerView.ViewHolder
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
