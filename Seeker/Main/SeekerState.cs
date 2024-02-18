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

        // Misc
        public static bool InDarkModeCache = false;
        public static bool currentlyLoggedIn = false;
        public static bool logoutClicked = false; // TODO hack


        // Settings
        public static bool AutoClearCompleteDownloads = false;
        public static bool AutoClearCompleteUploads = false;

        public static bool NotifyOnFolderCompleted = true;

        public static bool FreeUploadSlotsOnly = true;
        public static bool DisableDownloadToastNotification = true;
        public const bool AutoRetryDownload = true;

        public static bool HideLockedResultsInSearch = true;
        public static bool HideLockedResultsInBrowse = true;

        public static bool TransferViewShowSizes = false;
        public static bool TransferViewShowSpeed = false;

        public static bool MemoryBackedDownload = false;
        public static bool AutoRetryBackOnline = true; //this is for downloads that fail with the condition "User is Offline". this will also autodownload when we first log in as well.

        public static bool AutoRequeueDownloadsAtStartup = true;

        public static int NumberSearchResults = MainActivity.DEFAULT_SEARCH_RESULTS;
        public static int DayNightMode = (int)(AppCompatDelegate.ModeNightFollowSystem);
        public static ThemeHelper.NightThemeType NightModeVarient = ThemeHelper.NightThemeType.ClassicPurple;
        public static ThemeHelper.DayThemeType DayModeVarient = ThemeHelper.DayThemeType.ClassicPurple;
        public static bool RememberSearchHistory = true;
        public static SoulseekClient SoulseekClient = null;
        public static String Username = null;
        public static String Password = null;
        public static bool SharingOn = false;
        public static bool AllowPrivateRoomInvitations = false;
        public static bool StartServiceOnStartup = true;
        public static bool IsStartUpServiceCurrentlyRunning = false;

        public static bool AllowUploadsOnMetered = true;
        public static bool CurrentConnectionIsUnmetered = true;
        public static bool IsNetworkPermitting()
        {
            return AllowUploadsOnMetered || CurrentConnectionIsUnmetered;
        }

        public static SearchResultSorting DefaultSearchResultSortAlgorithm = SearchResultSorting.Available;

        public static String SaveDataDirectoryUri = null;
        public static bool SaveDataDirectoryUriIsFromTree = true;

        public static bool LegacyLanguageMigrated = false;
        public static string SystemLanguage;


        // Consts
        public static string Language = FieldLangAuto;
        public const string FieldLangAuto = "Auto";
        public const string FieldLangEn = "en";
        public const string FieldLangPtBr = "pt-rBR"; //language code -"r" region code
        public const string FieldLangFr = "fr";
        public const string FieldLangRu = "ru";
        public const string FieldLangEs = "es";
        public const string FieldLangUk = "uk"; //ukrainian
        public const string FieldLangCs = "cs"; //czech
        public const string FieldLangNl = "nl"; //dutch



        public static String ManualIncompleteDataDirectoryUri = null;
        public static bool ManualIncompleteDataDirectoryUriIsFromTree = true;

        public static bool SpeedLimitDownloadOn = false;
        public static bool SpeedLimitUploadOn = false;
        public static int SpeedLimitDownloadBytesSec = 4 * 1024 * 1024;//1048576;
        public static int SpeedLimitUploadBytesSec = 4 * 1024 * 1024;
        public static bool SpeedLimitDownloadIsPerTransfer = true;
        public static bool SpeedLimitUploadIsPerTransfer = true;

        public static bool ShowSmartFilters = true;
        public static SmartFilterState SmartFilterOptions;

        public static volatile bool DownloadKeepAliveServiceRunning = false;
        public static volatile bool UploadKeepAliveServiceRunning = false;

        public static TimeSpan OffsetFromUtcCached = TimeSpan.Zero;


        public static SlskHelp.SharedFileCache SharedFileCache = null;
        public static int UploadSpeed = -1; //bytes
        public static bool FailedShareParse = false;
        private static volatile bool isParsing = false;

        public static bool NumberOfSharedDirectoriesIsStale = true;
        public static bool AttemptedToSetUpSharing = false;

        public static bool OurCurrentStatusIsAway = false; //bool because it can only be online or away. we set this after we successfully change the status.
        //NOTE: 
        //If we end the connection abruptly (i.e. airplane mode, kill app, turn phone off) then our status will not be changed to offline. (at least after waiting for 20 mins, not sure when it would have)
        //  only if we close the tcp connection properly (FIN, ACK) (i.e. menu > Shut Down) does the server update our status properly to offline.
        //The server does not remember your old status.  So if you log in again after setting your status to away, then your status will be online.  You must set it to away again if desired.
        //There is some weirdness where we only get "GetStatus" (7) messages when we go from online to away.  Otherwise, we dont get anything.  So its not reliable for determining what our status is.
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
        public static bool ShowRecentUsers = true;

        public static string UserInfoBio = string.Empty;
        public static string UserInfoPictureName = string.Empty; //filename only. The picture will be in (internal storage) FilesDir/user_info_pic/filename.

        public static bool ListenerEnabled = true;
        public static volatile int ListenerPort = 33939;
        public static bool ListenerUPnpEnabled = true;

        public static bool CreateCompleteAndIncompleteFolders = true;
        public static bool CreateUsernameSubfolders = false;
        public static bool OverrideDefaultIncompleteLocations = false;

        public static bool PerformDeepMetadataSearch = true;

        public static bool AutoAwayOnInactivity = false;

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

        // TODOORG seperateclass models
        public struct SmartFilterState
        {
            public bool KeywordsEnabled;
            public int KeywordsOrder;
            public bool NumFilesEnabled;
            public int NumFilesOrder;
            public bool FileTypesEnabled;
            public int FileTypesOrder;
            public List<ChipType> GetEnabledOrder()
            {
                List<Tuple<ChipType, int>> tuples = new List<Tuple<ChipType, int>>();
                if (KeywordsEnabled)
                {
                    tuples.Add(new Tuple<ChipType, int>(ChipType.Keyword, KeywordsOrder));
                }
                if (NumFilesEnabled)
                {
                    tuples.Add(new Tuple<ChipType, int>(ChipType.FileCount, NumFilesOrder));
                }
                if (FileTypesEnabled)
                {
                    tuples.Add(new Tuple<ChipType, int>(ChipType.FileType, FileTypesOrder));
                }
                tuples.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
                return tuples.Select(t1 => t1.Item1).ToList();
            }

            public List<ConfigureChipItems> GetAdapterItems()
            {
                List<Tuple<string, int, bool>> tuples = new List<Tuple<string, int, bool>>();
                tuples.Add(new Tuple<string, int, bool>(GetNameFromEnum(ChipType.Keyword), KeywordsOrder, KeywordsEnabled));
                tuples.Add(new Tuple<string, int, bool>(GetNameFromEnum(ChipType.FileCount), NumFilesOrder, NumFilesEnabled));
                tuples.Add(new Tuple<string, int, bool>(GetNameFromEnum(ChipType.FileType), FileTypesOrder, FileTypesEnabled));
                tuples.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
                return tuples.Select(t1 => new ConfigureChipItems() { Name = t1.Item1, Enabled = t1.Item3 }).ToList();
            }

            public void FromAdapterItems(List<ConfigureChipItems> chipItems)
            {
                for (int i = 0; i < chipItems.Count; i++)
                {
                    ChipType ct = GetEnumFromName(chipItems[i].Name);
                    bool enabled = chipItems[i].Enabled;
                    switch (ct)
                    {
                        case ChipType.Keyword:
                            SeekerState.SmartFilterOptions.KeywordsEnabled = enabled;
                            SeekerState.SmartFilterOptions.KeywordsOrder = i;
                            break;
                        case ChipType.FileType:
                            SeekerState.SmartFilterOptions.FileTypesEnabled = enabled;
                            SeekerState.SmartFilterOptions.FileTypesOrder = i;
                            break;
                        case ChipType.FileCount:
                            SeekerState.SmartFilterOptions.NumFilesEnabled = enabled;
                            SeekerState.SmartFilterOptions.NumFilesOrder = i;
                            break;
                        default:
                            throw new Exception("unknown option");
                    }
                }
            }

            public const string DisplayNameKeyword = "Keywords";
            public const string DisplayNameType = "File Types";
            public const string DisplayNameCount = "# Files";

            public string GetNameFromEnum(ChipType chipType)
            {
                switch (chipType)
                {
                    case ChipType.Keyword:
                        return DisplayNameKeyword;
                    case ChipType.FileType:
                        return DisplayNameType;
                    case ChipType.FileCount:
                        return DisplayNameCount;
                    default:
                        throw new Exception("unknown enum");
                }
            }

            public ChipType GetEnumFromName(string name)
            {
                switch (name)
                {
                    case DisplayNameKeyword:
                        return ChipType.Keyword;
                    case DisplayNameType:
                        return ChipType.FileType;
                    case DisplayNameCount:
                        return ChipType.FileCount;
                    default:
                        throw new Exception("unknown enum");
                }
            }
        }


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
            return Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.R;
        }

        public static bool UseLegacyStorage()
        {
            return Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.Q;
        }

        public static bool PreOpenDocumentTree()
        {
            return Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.Lollipop;
        }

        public static bool PreMoveDocument()
        {
            return Android.OS.Build.VERSION.SdkInt < BuildVersionCodes.N;
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