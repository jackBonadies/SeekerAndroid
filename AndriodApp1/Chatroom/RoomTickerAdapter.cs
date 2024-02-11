using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1.Chatroom
{
    public class RoomTickerAdapter : ArrayAdapter<Soulseek.RoomTicker>
    {
        public AndroidX.Fragment.App.DialogFragment Owner = null;
        public RoomTickerAdapter(Context c, List<Soulseek.RoomTicker> items) : base(c, 0, items)
        {
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TickerItemView itemView = (TickerItemView)convertView;
            if (null == itemView)
            {
                itemView = TickerItemView.inflate(parent);
            }
            itemView.setItem(GetItem(position));
            return itemView;
            //return base.GetView(position, convertView, parent);
        }
    }

    public class TickerItemView : LinearLayout
    {
        private TextView tickerTextView;
        public TickerItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.ticker_item, this, true);
            setupChildren();
        }
        public TickerItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.ticker_item, this, true);
            setupChildren();
        }

        public static TickerItemView inflate(ViewGroup parent)
        {
            TickerItemView itemView = (TickerItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.ticker_item_dummy, parent, false);
            return itemView;
        }

        private void setupChildren()
        {
            tickerTextView = FindViewById<TextView>(Resource.Id.textView1);
        }

        public void setItem(Soulseek.RoomTicker t)
        {
            tickerTextView.Text = t.Message + " --" + t.Username;
        }
    }

}