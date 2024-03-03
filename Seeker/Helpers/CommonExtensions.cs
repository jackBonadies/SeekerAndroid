using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.Lifecycle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Helpers
{
    public static class CommonExtensions
    {
        public static bool? IsResumed(this FragmentActivity activity)
        {
            return activity?.Lifecycle?.CurrentState?.IsAtLeast(AndroidX.Lifecycle.Lifecycle.State.Resumed);
        }

        /// <summary>
        /// Set width and height of dialog as fraction of window size
        /// i.e. if .9 then 90% of width
        /// </summary>
        /// <param name="widthProportion"></param>
        /// <param name="heightProportion"></param>
        public static void SetSizeProportional(this Dialog dialog, double widthProportion = -1, double heightProportion = -1)
        {
            var window = dialog?.Window;
            if(window == null)
            {
                return;
            }

            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = (int)(size.X * widthProportion);
            int height = (int)(size.Y * heightProportion);


            if(widthProportion == -1)
            {
                width = Android.Views.WindowManagerLayoutParams.WrapContent;
            }

            if (heightProportion == -1)
            {
                height = Android.Views.WindowManagerLayoutParams.WrapContent;
            }

            window.SetLayout(width, height);
            window.SetGravity(GravityFlags.Center);
        }
    }
}