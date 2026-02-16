using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using Common;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Seeker
{
    public static class SeekerState
    {
        static SeekerState()
        {
            downloadInfoList = new List<DownloadInfo>();
        }

        // Misc (non-persisted)
        public static bool InDarkModeCache = false;
        public static bool logoutClicked = false; // TODO hack

        public const bool AutoRetryDownload = true;
        public static bool AutoRequeueDownloadsAtStartup = true;

        public static SoulseekClient SoulseekClient = null;

        public static bool IsStartUpServiceCurrentlyRunning = false;

        public static bool CurrentConnectionIsUnmetered = true;
        public static bool IsNetworkPermitting()
        {
            return PreferencesState.AllowUploadsOnMetered || CurrentConnectionIsUnmetered;
        }

        public static string SystemLanguage;

        public static volatile bool DownloadKeepAliveServiceRunning = false;
        public static volatile bool UploadKeepAliveServiceRunning = false;

        public static TimeSpan OffsetFromUtcCached = TimeSpan.Zero;

        public static SlskHelp.SharedFileCache SharedFileCache = null;
        public static bool FailedShareParse = false;
        private static volatile bool isParsing = false;

        public static bool NumberOfSharedDirectoriesIsStale = true;
        public static bool AttemptedToSetUpSharing = false;

        public static bool OurCurrentStatusIsAway = false;
        public enum PendingStatusChange
        {
            NothingPending = 0,
            AwayPending = 1,
            OnlinePending = 2,
        }
        public static PendingStatusChange PendingStatusChangeToAwayOnline = PendingStatusChange.NothingPending;

        public static List<UserListItem> IgnoreUserList = new List<UserListItem>();
        public static List<UserListItem> UserList = new List<UserListItem>();
        public static RecentUserManager RecentUsersManager = null;
        public static System.Collections.Concurrent.ConcurrentDictionary<string, string> UserNotes = null;
        /// <summary>
        /// There is no concurrent hashset so concurrent dictionary is used. the value is pointless so its only 1 byte.
        /// </summary>
        public static System.Collections.Concurrent.ConcurrentDictionary<string, byte> UserOnlineAlerts = null;

        public static EventHandler<EventArgs> DirectoryUpdatedEvent;
        public static EventHandler<EventArgs> SharingStatusChangedEvent;

        /// <summary>
        /// This is only for showing toasts.  The logic is as follows.  If we showed a cancelled toast
        /// notification <1000ms ago then dont keep showing them. if >1s ago then its okay to show.
        /// They all come in super fast
        /// </summary>
        public static long TaskWasCancelledToastDebouncer = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();

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
            ClearSearchHistory?.Invoke(null, null);
        }



        //public static event EventHandler<DownloadAddedEventArgs> DownloadAdded;
        /// <summary>
        /// Occurs after we set up the DownloadAdded transfer item.
        /// </summary>

        public static event EventHandler<EventArgs> ClearSearchHistory;
        public static List<DownloadInfo> downloadInfoList;
        /// <summary>
        /// Context of last created activity
        /// </summary>
        public static volatile FragmentActivity ActiveActivityRef = null;
        public static ISharedPreferences SharedPreferences;
        public static volatile MainActivity MainActivityRef;

        public static bool IsParsing
        {
            get
            {
                return isParsing;
            }
            set
            {
                isParsing = value;
                NumberParsed = 0; //reset
            }
        }


        public static int NumberParsed = 0;

        // TODO utils
        public static bool RequiresEitherOpenDocumentTreeOrManageAllFiles()
        {
            //29 does has the requestExternalStorage workaround.
            return OperatingSystem.IsAndroidVersionAtLeast(30);
        }

        public static bool UseLegacyStorage()
        {
            return !OperatingSystem.IsAndroidVersionAtLeast(29);
        }

        public static bool PreOpenDocumentTree()
        {
            return !OperatingSystem.IsAndroidVersionAtLeast(21);
        }

        public static bool PreMoveDocument()
        {
            return !OperatingSystem.IsAndroidVersionAtLeast(24);
        }

        public static bool IsLowDpi()
        {
            return Android.Content.Res.Resources.System.DisplayMetrics.WidthPixels < 768;
        }
        // TODO utils



        // TODO hack?
        public static ManualResetEvent ManualResetEvent = new ManualResetEvent(false); //previously this was on the loginfragment but
                                                                                       //it would get recreated every time so there were lost instances with threads waiting forever....

        //public static void OnDownloadAdded(DownloadInfo dlInfo)
        //{
        //    DownloadAdded(null,new DownloadAddedEventArgs(dlInfo));
        //}

        public static AndroidX.DocumentFile.Provider.DocumentFile DiagnosticTextFile = null;
        public static System.IO.StreamWriter DiagnosticStreamWriter = null;

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
    }

}
