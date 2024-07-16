using Android.Content;
using Android.Widget;
using Java.IO;
using SlskHelp;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Seeker.Helpers
{
    public class SearchTabHelper
    {
        public static void RemoveTabFromSharedPrefs(int wishlistSearchResultsToRemove, Context c, bool legacy = false)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, KeyConsts.M_wishlist_directory);
            if (!wishlist_dir.Exists())
            {
                wishlist_dir.Mkdir();
            }
            string name = System.Math.Abs(wishlistSearchResultsToRemove) + (legacy ? KeyConsts.M_wishlist_tab_legacy : KeyConsts.M_wishlist_tab);
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);
            if (!fileForOurInternalStorage.Delete())
            {
                MainActivity.LogDebug("HEADERS - Delete Search Results: FAILED TO DELETE");
                MainActivity.LogFirebase("HEADERS - Delete Search Results: FAILED TO DELETE");
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - Delete Search Results: " + sw.ElapsedMilliseconds);
        }


        public static void SaveAllSearchTabsToDisk(Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if (tabsToSave.Count == 0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                foreach (int tabIndex in tabsToSave)
                {
                    SaveSearchResultsToDisk(tabIndex, c);
                }
            }
            sw.Stop();
            MainActivity.LogDebug("HEADERS - Save ALL Search Results: " + sw.ElapsedMilliseconds);
        }

#if BinaryFormatterAvailable

        /// <summary>
        /// Restoring them when someone taps them is fast enough even for 1000 results...
        /// So this method probably isnt needed.
        /// </summary>
        /// <param name="c"></param>
        public static void MigrateAllSearchTabsFromDisk(Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if (tabsToSave.Count == 0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                foreach (int tabIndex in tabsToSave)
                {
                    List<SearchResponse> results = null;
                    try
                    {
                        results = RestoreSearchResultsFromDisk_Imp(tabIndex, c, true);
                    }
                    catch (Exception ex)
                    {
                        MainActivity.LogFirebase("Error Migrating Seach Tabs: " + ex.Message + ex.StackTrace);
                        RemoveTabFromSharedPrefs(tabIndex, c, true);
                    }

                    if (results != null)
                    {
                        RemoveTabFromSharedPrefs(tabIndex, c, true);
                        SaveSearchResultsToDisk_Imp(tabIndex, c, results);
                    }
                }
            }
            sw.Stop();
            MainActivity.LogDebug("HEADERS - Restore ALL Search Results: " + sw.ElapsedMilliseconds);
        }
#endif

        public static void SaveSearchResultsToDisk_Imp(int wishlistSearchResultsToSave, Context c, List<SearchResponse> searchResultsToSave)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, KeyConsts.M_wishlist_directory);
            if (!wishlist_dir.Exists())
            {
                wishlist_dir.Mkdir();
            }
            string name = System.Math.Abs(wishlistSearchResultsToSave) + KeyConsts.M_wishlist_tab;

            var arr = SerializationHelper.SaveSearchResponsesToByteArray(searchResultsToSave);
            CommonHelpers.SaveToDisk(c, arr, wishlist_dir, name);
        }


        public static void SaveSearchResultsToDisk(int wishlistSearchResultsToSave, Context c)
        {
            var searchResultsToSave = SearchTabHelper.SearchTabCollection[wishlistSearchResultsToSave].SearchResponses;
            SaveSearchResultsToDisk_Imp(wishlistSearchResultsToSave, c, searchResultsToSave);
        }

        public static List<SearchResponse> RestoreSearchResultsFromDisk_Imp(int wishlistSearchResultsToRestore, Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, KeyConsts.M_wishlist_directory);
            //if (!wishlist_dir.Exists())
            //{
            //    wishlist_dir.Mkdir();
            //}
            string name = System.Math.Abs(wishlistSearchResultsToRestore) + KeyConsts.M_wishlist_tab;
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);

            if (!fileForOurInternalStorage.Exists())
            {
                return null;
            }

            using (System.IO.Stream inputStream = c.ContentResolver.OpenInputStream(AndroidX.DocumentFile.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri))
            {
                MainActivity.LogDebug("HEADERS - get file: " + sw.ElapsedMilliseconds);

                MainActivity.LogDebug("HEADERS - read file: " + sw.ElapsedMilliseconds);

                var restoredSearchResponses = SerializationHelper.RestoreSearchResponsesFromStream(inputStream);
                return restoredSearchResponses;
            }
        }

        public static void RestoreSearchResultsFromDisk(int wishlistSearchResultsToRestore, Context c)
        {
            List<SearchResponse> restoredSearchResults = null;
            try
            {
                restoredSearchResults = RestoreSearchResultsFromDisk_Imp(wishlistSearchResultsToRestore, c);
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("FAILED to restore search results from disk " + e.Message + e.StackTrace);
            }


            //there are two cases.
            //  1) we imported the term.  In that case there are no results yet as it hasnt been ran.  Which is fine.  
            //  2) its a bug.
            if (restoredSearchResults == null)
            {
                if (SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastSearchResultsCount == 0 || SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastRanTime == DateTime.MinValue)
                {
                    //nothing to do.  this is the good case..
                }
                else
                {
                    //log error... but still safely fix the state. otherwise the user wont even be able to load the app without crash...
                    MainActivity.LogFirebase("search tab does not exist on disk but it should... ");
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastRanTime = DateTime.MinValue;
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastSearchResponseCount = 0;
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastSearchResultsCount = 0;
                    try
                    {
                        //may not be on UI thread if from wishlist timer elapsed...
                        Toast.MakeText(c, "Failed to restore wishlist search results from disk", ToastLength.Long).Show();
                    }
                    catch
                    {

                    }
                }
                //safely fix the state. even in case of error...
                SavedStateSearchTab tab = new SavedStateSearchTab();
                tab.searchResponses = new List<SearchResponse>();
                SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore] = SavedStateSearchTab.GetTabFromSavedState(tab, true, SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore]);
            }
            else
            {
                SavedStateSearchTab tab = new SavedStateSearchTab();
                tab.searchResponses = restoredSearchResults;
                SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore] = SavedStateSearchTab.GetTabFromSavedState(tab, true, SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore]);
            }
        }


        public static void SaveHeadersToSharedPrefs()
        {

            var sw = System.Diagnostics.Stopwatch.StartNew();

            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if (tabsToSave.Count == 0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                Dictionary<int, SavedStateSearchTabHeader> savedStates = new Dictionary<int, SavedStateSearchTabHeader>();
                foreach (int tabIndex in tabsToSave)
                {
                    savedStates.Add(tabIndex, SavedStateSearchTabHeader.GetSavedStateHeaderFromTab(SearchTabHelper.SearchTabCollection[tabIndex]));
                }

                stringToSave = SerializationHelper.SerializeToString(savedStates);
            }

            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_SearchTabsState_Headers, stringToSave);
                editor.Commit();
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - SaveHeadersToSharedPrefs: " + sw.ElapsedMilliseconds);
        }

#if BinaryFormatterAvailable

        //load legacy, and then save new to shared prefs and disk
        public static void ConvertLegacyWishlistsIfApplicable(Context c)
        {
            string savedState = SeekerState.SharedPreferences.GetString(KeyConsts.M_SearchTabsState_LEGACY, string.Empty);
            if (savedState == string.Empty)
            {
                //nothing to do...
                return;
            }
            else
            {
                MainActivity.LogDebug("Converting Wishlists to New Format...");
                RestoreStateFromSharedPreferencesLegacy();
                SeekerState.SharedPreferences.Edit().Remove(KeyConsts.M_SearchTabsState_LEGACY).Commit();
                //string x = SeekerState.SharedPreferences.GetString(KeyConsts.M_SearchTabsState_LEGACY, string.Empty); //works, string is empty.
                SaveHeadersToSharedPrefs();
                SaveAllSearchTabsToDisk(c);
            }
        }

#endif

        public static void RestoreHeadersFromSharedPreferences()
        {
            string savedState = SeekerState.SharedPreferences.GetString(KeyConsts.M_SearchTabsState_Headers, string.Empty);
            if (savedState == string.Empty)
            {
                return;
            }
            else
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                MainActivity.LogDebug("HEADERS - base64 string length: " + sw.ElapsedMilliseconds);

                Dictionary<int, SavedStateSearchTabHeader> savedStateDict = SerializationHelper.RestoreSavedStateHeaderDictFromString(savedState);

                int lowestID = int.MaxValue;
                foreach (var pair in savedStateDict)
                {
                    if (pair.Key < lowestID)
                    {
                        lowestID = pair.Key;
                    }
                    SearchTabCollection[pair.Key] = SavedStateSearchTabHeader.GetTabFromSavedState(pair.Value, null);
                }
                if (lowestID != int.MaxValue)
                {
                    lastWishlistID = lowestID;
                }

                sw.Stop();
                MainActivity.LogDebug("HEADERS - RestoreStateFromSharedPreferences: wishlist: " + sw.ElapsedMilliseconds);
            }
            //SeekerState.SharedPreferences.Edit().Remove
        }

#if BinaryFormatterAvailable
        public static void RestoreStateFromSharedPreferencesLegacy()
        {
            string savedState = SeekerState.SharedPreferences.GetString(KeyConsts.M_SearchTabsState_LEGACY, string.Empty);
            if (savedState == string.Empty)
            {
                return;
            }
            else
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                MainActivity.LogDebug("base64 string length: " + sw.ElapsedMilliseconds);

                using (System.IO.MemoryStream memStream = new System.IO.MemoryStream(Convert.FromBase64String(savedState)))
                {
                    BinaryFormatter formatter = SerializationHelper.GetLegacyBinaryFormatter();
                    var savedStateDict = formatter.Deserialize(memStream) as Dictionary<int, SavedStateSearchTab>;
                    int lowestID = int.MaxValue;
                    foreach (var pair in savedStateDict)
                    {
                        if (pair.Key < lowestID)
                        {
                            lowestID = pair.Key;
                        }
                        SearchTabCollection[pair.Key] = SavedStateSearchTab.GetTabFromSavedState(pair.Value);
                    }
                    if (lowestID != int.MaxValue)
                    {
                        lastWishlistID = lowestID;
                    }
                }
                sw.Stop();
                MainActivity.LogDebug("RestoreStateFromSharedPreferences: wishlist: " + sw.ElapsedMilliseconds);
            }
        }
#endif



        static SearchTabHelper()
        {
            SearchTabCollection[CurrentTab] = new SearchTab();
        }
        //CURRENT TAB == what is current being shown in UI. Therefore, everyone should use it OTHER THAN the search logic which should only use it IF it matches the current tab.
        public static volatile int CurrentTab = 0;
        public static TabType CurrentTabType = TabType.Search;
        public static string FilterStickyString = string.Empty;

        public static System.Collections.Concurrent.ConcurrentDictionary<int, SearchTab> SearchTabCollection = new System.Collections.Concurrent.ConcurrentDictionary<int, SearchTab>();
        private static int lastSearchID = 0;
        private static int lastWishlistID = 0;

        //all of these getters and setters work on current tab.

        public static int AddSearchTab() //returns ID of new search term added.
        {
            lastSearchID++;
            SearchTabCollection[lastSearchID] = new SearchTab();
            return lastSearchID;
        }

        //public static int AddWishlistSearchTab() //returns ID of new search term added.
        //{
        //    lastWishlistID--;
        //    SearchTabCollection[lastWishlistID] = new SearchTab();
        //    SearchTabCollection[lastWishlistID].SearchTarget = SearchTarget.Wishlist;
        //    return lastWishlistID;
        //}

        public static void AddWishlistSearchTabFromCurrent()
        {
            lastWishlistID--;
            SearchTabCollection[lastWishlistID] = SearchTabCollection[CurrentTab].Clone(true);
            SearchTabCollection[lastWishlistID].SearchTarget = SearchTarget.Wishlist;
            SearchTabCollection[lastWishlistID].CurrentlySearching = false;

            //*********************
            SearchTabHelper.SaveSearchResultsToDisk(lastWishlistID, SeekerState.ActiveActivityRef);
            SearchTabHelper.SaveHeadersToSharedPrefs();
        }

        /// <summary>
        /// This is done in the case of import
        /// TODO: testing - this is the only way to create a wishlist tab without the main activity created so beware!! and test!!
        ///    prove that this is always initialized (so we dont save and wipe away all previous wishes...)
        /// </summary>
        public static void AddWishlistSearchTabFromString(string wish)
        {
            lastWishlistID--;
            SearchTabCollection[lastWishlistID] = new SearchTab();
            SearchTabCollection[lastWishlistID].LastSearchTerm = wish;
            SearchTabCollection[lastWishlistID].SearchTarget = SearchTarget.Wishlist;
            SearchTabCollection[lastWishlistID].CurrentlySearching = false;

            //*********************

        }

        public static int LastSearchResultsCount
        {
            get
            {
                return SearchTabCollection[CurrentTab].LastSearchResultsCount;
            }
            set
            {
                SearchTabCollection[CurrentTab].LastSearchResultsCount = value;
            }
        }

        public static string LastSearchTerm
        {
            get
            {
                return SearchTabCollection[CurrentTab].LastSearchTerm;
            }
            set
            {
                SearchTabCollection[CurrentTab].LastSearchTerm = value;
            }
        }

        public static SearchResultSorting SortHelperSorting
        {
            get
            {
                return SearchTabCollection[CurrentTab].SortHelperSorting;
            }
            set
            {
                SearchTabCollection[CurrentTab].SortHelperSorting = value;
            }
        }

        public static int LastSearchResponseCount //static so when the fragment gets remade we can use it
        {
            get
            {
                return SearchTabCollection[CurrentTab].LastSearchResponseCount;
            }
            set
            {
                SearchTabCollection[CurrentTab].LastSearchResponseCount = value;
            }
        }

        public static List<SearchResponse> SearchResponses //static so when the fragment gets remade we can use it
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchResponses;

            }
            set
            {
                SearchTabCollection[CurrentTab].SearchResponses = value;
            }
        }

        public static SortedDictionary<SearchResponse, object> SortHelper
        {
            get
            {
                return SearchTabCollection[CurrentTab].SortHelper;
            }
            set
            {
                SearchTabCollection[CurrentTab].SortHelper = value;
            }
        }

        /// <summary>
        /// Locking on the SortHelper is not enough, since it gets replaced if user changes the sort algorithm
        /// </summary>
        public static object SortHelperLockObject
        {
            get
            {
                return SearchTabCollection[CurrentTab].SortHelperLockObject;
            }
        }

        public static bool FilteredResults
        {
            get
            {
                return SearchTabCollection[CurrentTab].FilteredResults;
            }
            set
            {
                SearchTabCollection[CurrentTab].FilteredResults = value;
            }
        }
        //public static bool FilterSticky
        //{
        //    get
        //    {
        //        return SearchTabCollection[CurrentTab].FilterSticky;
        //    }
        //    set
        //    {
        //        SearchTabCollection[CurrentTab].FilterSticky = value;
        //    }
        //}

        public static CancellationTokenSource CancellationTokenSource
        {
            get
            {
                return SearchTabCollection[CurrentTab].CancellationTokenSource;
            }
            set
            {
                SearchTabCollection[CurrentTab].CancellationTokenSource = value;
            }
        }

        public static string FilterString
        {
            get
            {
                return SearchTabCollection[CurrentTab].FilterString;
            }
            set
            {
                SearchTabCollection[CurrentTab].FilterString = value;
            }
        }
        public static List<string> WordsToAvoid
        {
            get
            {
                return SearchTabCollection[CurrentTab].WordsToAvoid;
            }
            set
            {
                SearchTabCollection[CurrentTab].WordsToAvoid = value;
            }
        }
        public static List<string> WordsToInclude
        {
            get
            {
                return SearchTabCollection[CurrentTab].WordsToInclude;
            }
            set
            {
                SearchTabCollection[CurrentTab].WordsToInclude = value;
            }
        }


        public static FilterSpecialFlags FilterSpecialFlags
        {
            get
            {
                return SearchTabCollection[CurrentTab].FilterSpecialFlags;
            }
            set
            {
                SearchTabCollection[CurrentTab].FilterSpecialFlags = value;
            }
        }

        public static List<SearchResponse> UI_SearchResponses
        {
            get
            {
                return SearchTabCollection[CurrentTab].UI_SearchResponses;
            }
            set
            {
                SearchTabCollection[CurrentTab].UI_SearchResponses = value;
            }
        }
        public static SearchTarget SearchTarget
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchTarget;
            }
            set
            {
                SearchTabCollection[CurrentTab].SearchTarget = value;
            }
        }
        public static bool CurrentlySearching
        {
            get
            {
                return SearchTabCollection[CurrentTab].CurrentlySearching;
            }
            set
            {
                SearchTabCollection[CurrentTab].CurrentlySearching = value;
            }
        }


        public static string SearchTargetChosenUser
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchTargetChosenUser;
            }
            set
            {
                SearchTabCollection[CurrentTab].SearchTargetChosenUser = value;
            }
        }

        public static string SearchTargetChosenRoom
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchTargetChosenRoom;
            }
            set
            {
                SearchTabCollection[CurrentTab].SearchTargetChosenRoom = value;
            }
        }


    }

}