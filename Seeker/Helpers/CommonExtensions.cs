using Android.App;
using Android.Content;
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
    }
}