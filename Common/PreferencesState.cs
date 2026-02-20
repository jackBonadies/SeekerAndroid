using Common.Messages;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public static class PreferencesState
    {
        // Account
        public static bool CurrentlyLoggedIn = false;
        public static string Username = null;
        public static string Password = null;

        // Data directories
        public static string SaveDataDirectoryUri = null;
        public static bool SaveDataDirectoryUriIsFromTree = true;
        public static string ManualIncompleteDataDirectoryUri = null;
        public static bool ManualIncompleteDataDirectoryUriIsFromTree = true;

        // Search
        public static int NumberSearchResults = 250; // Constants.DefaultSearchResults
        public static bool RememberSearchHistory = true;
        public static bool ShowRecentUsers = true;
        public static bool FreeUploadSlotsOnly = true;
        public static bool HideLockedResultsInSearch = true;
        public static bool HideLockedResultsInBrowse = true;
        public static Seeker.SearchResultSorting DefaultSearchResultSortAlgorithm = Seeker.SearchResultSorting.Available;
        public static bool FilterSticky = false;
        public static string FilterStickyString = string.Empty;
        public static int SearchResultStyle = 1; // Medium

        // UI / Theme
        public static int DayNightMode = -1; // AppCompatDelegate.ModeNightFollowSystem
        public static NightThemeType NightModeVarient = NightThemeType.ClassicPurple;
        public static DayThemeType DayModeVarient = DayThemeType.ClassicPurple;
        public static bool LegacyLanguageMigrated = false;
        public static string Language = FieldLangAuto;

        // Language constants
        public const string FieldLangAuto = "Auto";
        public const string FieldLangEn = "en";
        public const string FieldLangPtBr = "pt-rBR";
        public const string FieldLangFr = "fr";
        public const string FieldLangRu = "ru";
        public const string FieldLangEs = "es";
        public const string FieldLangUk = "uk";
        public const string FieldLangCs = "cs";
        public const string FieldLangIt = "it";
        public const string FieldLangNl = "nl";

        // Transfers
        public static bool AutoClearCompleteDownloads = false;
        public static bool AutoClearCompleteUploads = false;
        public static bool NotifyOnFolderCompleted = true;
        public static bool DisableDownloadToastNotification = true;
        public static bool MemoryBackedDownload = false;
        public static bool TransferViewShowSizes = false;
        public static bool TransferViewShowSpeed = false;
        public static bool AutoRetryBackOnline = true;
        public static bool NoSubfolderForSingle = false;
        public static bool CreateCompleteAndIncompleteFolders = true;
        public static bool CreateUsernameSubfolders = false;
        public static bool OverrideDefaultIncompleteLocations = false;

        // Speed limits
        public static bool SpeedLimitDownloadOn = false;
        public static bool SpeedLimitUploadOn = false;
        public static int SpeedLimitDownloadBytesSec = 4 * 1024 * 1024;
        public static int SpeedLimitUploadBytesSec = 4 * 1024 * 1024;
        public static bool SpeedLimitDownloadIsPerTransfer = true;
        public static bool SpeedLimitUploadIsPerTransfer = true;

        // Sharing
        public static bool SharingOn = false;
        public static int UploadSpeed = -1;
        public static bool AllowUploadsOnMetered = true;

        // Social
        public static bool AllowPrivateRoomInvitations = false;
        public static bool StartServiceOnStartup = true;
        public static bool AutoAwayOnInactivity = false;
        public static string UserInfoBio = string.Empty;
        public static string UserInfoPictureName = string.Empty;

        // Chatroom
        public static bool ShowStatusesView = true;
        public static bool ShowTickerView = false;
        public static SortOrderChatroomUsers SortChatroomUsersBy = SortOrderChatroomUsers.Alphabetical; // Alphabetical
        public static bool PutFriendsOnTop = false;

        // User list
        public static int UserListSortOrder = 0; // DateAddedAsc

        // Smart filters
        public static bool ShowSmartFilters = true;
        public static SmartFilterState SmartFilterOptions;

        // Listener
        public static bool ListenerEnabled = true;
        public static int ListenerPort = 33939;
        public static bool ListenerUPnpEnabled = true;

        // Diagnostics
        public static bool LogDiagnostics = false;

        // Simultaneous downloads
        public static bool LimitSimultaneousDownloads = false;
        public static int MaxSimultaneousLimit = 1;

        // Deep metadata
        public static bool PerformDeepMetadataSearch = true;

        /// <summary>
        /// Smart filter configuration state. Pure data - no Android dependencies.
        /// </summary>
        public struct SmartFilterState
        {
            public bool KeywordsEnabled;
            public int KeywordsOrder;
            public bool NumFilesEnabled;
            public int NumFilesOrder;
            public bool FileTypesEnabled;
            public int FileTypesOrder;

            public List<Seeker.ChipType> GetEnabledOrder()
            {
                List<System.Tuple<Seeker.ChipType, int>> tuples = new List<System.Tuple<Seeker.ChipType, int>>();
                if (KeywordsEnabled)
                {
                    tuples.Add(new System.Tuple<Seeker.ChipType, int>(Seeker.ChipType.Keyword, KeywordsOrder));
                }
                if (NumFilesEnabled)
                {
                    tuples.Add(new System.Tuple<Seeker.ChipType, int>(Seeker.ChipType.FileCount, NumFilesOrder));
                }
                if (FileTypesEnabled)
                {
                    tuples.Add(new System.Tuple<Seeker.ChipType, int>(Seeker.ChipType.FileType, FileTypesOrder));
                }
                tuples.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
                return tuples.Select(t1 => t1.Item1).ToList();
            }

            public List<Seeker.ConfigureChipItems> GetAdapterItems()
            {
                List<System.Tuple<string, int, bool>> tuples = new List<System.Tuple<string, int, bool>>();
                tuples.Add(new System.Tuple<string, int, bool>(GetNameFromEnum(Seeker.ChipType.Keyword), KeywordsOrder, KeywordsEnabled));
                tuples.Add(new System.Tuple<string, int, bool>(GetNameFromEnum(Seeker.ChipType.FileCount), NumFilesOrder, NumFilesEnabled));
                tuples.Add(new System.Tuple<string, int, bool>(GetNameFromEnum(Seeker.ChipType.FileType), FileTypesOrder, FileTypesEnabled));
                tuples.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
                return tuples.Select(t1 => new Seeker.ConfigureChipItems() { Name = t1.Item1, Enabled = t1.Item3 }).ToList();
            }

            public void FromAdapterItems(List<Seeker.ConfigureChipItems> chipItems)
            {
                for (int i = 0; i < chipItems.Count; i++)
                {
                    Seeker.ChipType ct = GetEnumFromName(chipItems[i].Name);
                    bool enabled = chipItems[i].Enabled;
                    switch (ct)
                    {
                        case Seeker.ChipType.Keyword:
                            KeywordsEnabled = enabled;
                            KeywordsOrder = i;
                            break;
                        case Seeker.ChipType.FileType:
                            FileTypesEnabled = enabled;
                            FileTypesOrder = i;
                            break;
                        case Seeker.ChipType.FileCount:
                            NumFilesEnabled = enabled;
                            NumFilesOrder = i;
                            break;
                        default:
                            throw new System.Exception("unknown option");
                    }
                }
                SmartFilterOptions = this;
            }

            public const string DisplayNameKeyword = "Keywords";
            public const string DisplayNameType = "File Types";
            public const string DisplayNameCount = "# Files";

            public string GetNameFromEnum(Seeker.ChipType chipType)
            {
                switch (chipType)
                {
                    case Seeker.ChipType.Keyword:
                        return DisplayNameKeyword;
                    case Seeker.ChipType.FileType:
                        return DisplayNameType;
                    case Seeker.ChipType.FileCount:
                        return DisplayNameCount;
                    default:
                        throw new System.Exception("unknown enum");
                }
            }

            public Seeker.ChipType GetEnumFromName(string name)
            {
                switch (name)
                {
                    case DisplayNameKeyword:
                        return Seeker.ChipType.Keyword;
                    case DisplayNameType:
                        return Seeker.ChipType.FileType;
                    case DisplayNameCount:
                        return Seeker.ChipType.FileCount;
                    default:
                        throw new System.Exception("unknown enum");
                }
            }
        }
    }
}
