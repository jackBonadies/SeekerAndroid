using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1.Chatroom
{
    public class AllTickersDialog : AndroidX.Fragment.App.DialogFragment
    {
        public static string OurRoomName = string.Empty;
        private ListView listViewTickers = null;
        private RoomTickerAdapter tickerAdapter = null;
        public AllTickersDialog(string ourRoomName)
        {
            OurRoomName = ourRoomName;
        }
        public AllTickersDialog()
        {

        }

        public override void OnResume()
        {
            base.OnResume();

            Window window = Dialog.Window;//  getDialog().getWindow();
            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;

            window.SetLayout((int)(width * 0.90), Android.Views.WindowManagerLayoutParams.WrapContent);//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.all_ticker_dialog, container); //container is parent
        }

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));

            this.SetStyle((int)DialogFragmentStyle.Normal, 0);
            this.Dialog.SetTitle(OurRoomName);

            listViewTickers = view.FindViewById<ListView>(Resource.Id.listViewTickers);


            UpdateListView();
        }

        private void UpdateListView()
        {
            var roomTickers = ChatroomController.JoinedRoomTickers[OurRoomName].ToList();
            roomTickers.Reverse();
            tickerAdapter = new RoomTickerAdapter(this.Activity, roomTickers);
            tickerAdapter.Owner = this;
            listViewTickers.Adapter = tickerAdapter;
        }
    }

}