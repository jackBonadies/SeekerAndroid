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
        public const string DefaultMusicUri = "content://com.android.externalstorage.documents/tree/primary%3AMusic";

        // Misc (non-persisted)
        public static bool InDarkModeCache = false;
        public static volatile LoginFragment LoginFragmentRef = null;


        public static bool AutoRequeueDownloadsAtStartup = true;

        public static ISoulseekClient SoulseekClient = null;

        public static bool IsStartUpServiceCurrentlyRunning = false;

        public static string SystemLanguage;

        public static volatile bool DownloadKeepAliveServiceRunning = false;
        public static volatile bool UploadKeepAliveServiceRunning = false;

        public static TimeSpan OffsetFromUtcCached = TimeSpan.Zero;

        public static bool OurCurrentStatusIsAway = false;
        public enum PendingStatusChange
        {
            NothingPending = 0,
            AwayPending = 1,
            OnlinePending = 2,
        }
        public static PendingStatusChange PendingStatusChangeToAwayOnline = PendingStatusChange.NothingPending;

        public static EventHandler<EventArgs> DirectoryUpdatedEvent;


        /// <summary>
        /// This is for when the cancelAndClear button was last pressed.  It is because of the massive amount of cancellation
        /// events all occuring on different threads that all go to affect the service.
        /// </summary>
        public static long CancelAndClearAllWasPressedDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
        public static long AbortAllWasPressedDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();


        public static void ClearSearchHistoryEventsFromTarget(object target)
        {
            if (ClearSearchHistory == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in ClearSearchHistory.GetInvocationList())
                {
                    if (d.Target.GetType() == target.GetType())
                    {
                        ClearSearchHistory -= (EventHandler<EventArgs>)d;
                    }
                }
            }
        }

        public static void ClearSearchHistoryInvoke()
        {
            ClearSearchHistory?.Invoke(null, EventArgs.Empty);
        }



        public static event EventHandler<EventArgs> ClearSearchHistory;
        /// <summary>
        /// Context of last created activity
        /// </summary>
        public static volatile FragmentActivity ActiveActivityRef = null;
        public static ISharedPreferences SharedPreferences;
        public static volatile MainActivity MainActivityRef;

        // TODO hack?
        public static ManualResetEvent ManualResetEvent = new ManualResetEvent(false); //previously this was on the loginfragment but
                                                                                       //it would get recreated every time so there were lost instances with threads waiting forever....

        public static event EventHandler<BrowseResponseEvent> BrowseResponseReceived;
        public static AndroidX.DocumentFile.Provider.DocumentFile RootDocumentFile = null;
        public static AndroidX.DocumentFile.Provider.DocumentFile RootIncompleteDocumentFile = null; //only gets set if can write the dir...
        public static void OnBrowseResponseReceived(BrowseResponse origBR, TreeNode<Directory> rootTree, string fromUsername, string startingLocation)
        {
            BrowseResponseReceived(null, new BrowseResponseEvent(origBR, rootTree, fromUsername, startingLocation));
        }
        public static void ClearOnBrowseResponseReceivedEventsFromTarget(object target)
        {
            if (BrowseResponseReceived == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in BrowseResponseReceived.GetInvocationList())
                {
                    if (d.Target.GetType() == target.GetType())
                    {
                        BrowseResponseReceived -= (EventHandler<BrowseResponseEvent>)d;
                    }
                }
            }
        }

        public static DocumentFile OpenRootFile(Context context, Android.Net.Uri chosenUri)
        {
            return DocumentFile.FromTreeUri(context, chosenUri);
        }
    }

}
