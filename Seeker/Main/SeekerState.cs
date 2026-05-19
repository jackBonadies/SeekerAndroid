using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.DocumentFile.Provider;
using AndroidX.Fragment.App;
using Common;
using Common.Share;
using Soulseek;
using System;
using System.Threading;

namespace Seeker
{
    public static class SeekerState
    {
        // Misc (non-persisted)
        public static bool InDarkModeCache = false;
        public static volatile LoginFragment LoginFragmentRef = null;


        public static ISoulseekClient SoulseekClient = null;

        public static string SystemLanguage;

        public static TimeSpan OffsetFromUtcCached = TimeSpan.Zero;

        public static bool OurCurrentStatusIsAway = false;
        public enum PendingStatusChange
        {
            NothingPending = 0,
            AwayPending = 1,
            OnlinePending = 2,
        }
        public static PendingStatusChange PendingStatusChangeToAwayOnline = PendingStatusChange.NothingPending;



        /// <summary>
        /// This is for when the cancelAndClear button was last pressed.  It is because of the massive amount of cancellation
        /// events all occuring on different threads that all go to affect the service.
        /// </summary>
        public static long CancelAndClearAllWasPressedDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
        public static long AbortAllWasPressedDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();


        /// <summary>
        /// Context of last created activity
        /// </summary>
        public static volatile FragmentActivity ActiveActivityRef = null;
        public static ISharedPreferences SharedPreferences;
        public static volatile MainActivity MainActivityRef;

        // TODO hack?
        public static ManualResetEvent ManualResetEvent = new ManualResetEvent(false); //previously this was on the loginfragment but
                                                                                       //it would get recreated every time so there were lost instances with threads waiting forever....

    }

}
