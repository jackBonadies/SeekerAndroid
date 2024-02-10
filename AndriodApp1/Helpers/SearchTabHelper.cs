﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace AndriodApp1.Helpers
{
    public class SearchTabHelper
    {
        public static void SaveStateToSharedPreferencesFullLegacy()
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
                Dictionary<int, SavedStateSearchTab> savedStates = new Dictionary<int, SavedStateSearchTab>();
                foreach (int tabIndex in tabsToSave)
                {
                    savedStates.Add(tabIndex, SavedStateSearchTab.GetSavedStateFromTab(SearchTabHelper.SearchTabCollection[tabIndex]));
                }
                using (System.IO.MemoryStream savedStateStream = new System.IO.MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(savedStateStream, savedStates);
                    stringToSave = Convert.ToBase64String(savedStateStream.ToArray());
                }
            }

            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_SearchTabsState_LEGACY, stringToSave);
                editor.Commit();
            }

            sw.Stop();
            MainActivity.LogDebug("OLD STYLE: " + sw.ElapsedMilliseconds);
        }

        public static void RemoveTabFromSharedPrefs(int wishlistSearchResultsToRemove, Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, "wishlist_dir");
            if (!wishlist_dir.Exists())
            {
                wishlist_dir.Mkdir();
            }
            string name = System.Math.Abs(wishlistSearchResultsToRemove) + "_wishlist_tab";
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);
            if(!fileForOurInternalStorage.Delete())
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

        /// <summary>
        /// Restoring them when someone taps them is fast enough even for 1000 results...
        /// So this method probably isnt needed.
        /// </summary>
        /// <param name="c"></param>
        public static void RestoreAllSearchTabsFromDisk(Context c)
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
                    RestoreSearchResultsFromDisk(tabIndex, c);
                }
            }
            sw.Stop();
            MainActivity.LogDebug("HEADERS - Restore ALL Search Results: " + sw.ElapsedMilliseconds);
        }


        public static void SaveSearchResultsToDisk(int wishlistSearchResultsToSave, Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, "wishlist_dir");
            if (!wishlist_dir.Exists())
            {
                wishlist_dir.Mkdir();
            }
            string name = System.Math.Abs(wishlistSearchResultsToSave) + "_wishlist_tab"; 
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);
            System.IO.Stream outputStream = c.ContentResolver.OpenOutputStream(Android.Support.V4.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri, "w");


            using (System.IO.MemoryStream searchRes = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(searchRes, SearchTabHelper.SearchTabCollection[wishlistSearchResultsToSave].SearchResponses);
                byte[] arr = searchRes.ToArray();
                outputStream.Write(arr,0,arr.Length);
                outputStream.Flush();
                outputStream.Close();
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - Save Search Results: " + sw.ElapsedMilliseconds + " count " + SearchTabHelper.SearchTabCollection[wishlistSearchResultsToSave].SearchResponses.Count);
        }


        public static void RestoreSearchResultsFromDisk(int wishlistSearchResultsToRestore, Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, "wishlist_dir");
            //if (!wishlist_dir.Exists())
            //{
            //    wishlist_dir.Mkdir();
            //}
            string name = System.Math.Abs(wishlistSearchResultsToRestore) + "_wishlist_tab";
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);

            //there are two cases.
            //  1) we imported the term.  In that case there are no results yet as it hasnt been ran.  Which is fine.  
            //  2) its a bug.
            if(!fileForOurInternalStorage.Exists())
            {
                if(SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastSearchResultsCount == 0 || SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastRanTime == DateTime.MinValue)
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
                return;
            }

            using(System.IO.Stream inputStream = c.ContentResolver.OpenInputStream(Android.Support.V4.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri))
            {
                MainActivity.LogDebug("HEADERS - get file: " + sw.ElapsedMilliseconds);

                using(System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    inputStream.CopyTo(ms);
                    ms.Position = 0;
                    MainActivity.LogDebug("HEADERS - read file: " + sw.ElapsedMilliseconds);
                    BinaryFormatter formatter = new BinaryFormatter();
                    //SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].SearchResponses = formatter.Deserialize(ms) as List<SearchResponse>;
                    SavedStateSearchTab tab = new SavedStateSearchTab();
                    tab.searchResponses = formatter.Deserialize(ms) as List<SearchResponse>;
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore] = SavedStateSearchTab.GetTabFromSavedState(tab, true, SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore]);

                    //SearchTabCollection[pair.Key] = SavedStateSearchTab.GetTabFromSavedState(pair.Value);
                }
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - Restore Search Results: " + sw.ElapsedMilliseconds + " count " + SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].SearchResponses.Count);
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
                using (System.IO.MemoryStream savedStateStream = new System.IO.MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(savedStateStream, savedStates);
                    stringToSave = Convert.ToBase64String(savedStateStream.ToArray());
                }
            }

            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_SearchTabsState_Headers, stringToSave);
                editor.Commit();
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - SaveHeadersToSharedPrefs: " + sw.ElapsedMilliseconds);
        }

        //load legacy, and then save new to shared prefs and disk
        public static void ConvertLegacyWishlistsIfApplicable(Context c)
        {
            string savedState = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_LEGACY, string.Empty);
            if (savedState == string.Empty)
            {
                //nothing to do...
                return;
            }
            else
            {
                MainActivity.LogDebug("Converting Wishlists to New Format...");
                RestoreStateFromSharedPreferencesLegacy();
                SoulSeekState.SharedPreferences.Edit().Remove(SoulSeekState.M_SearchTabsState_LEGACY).Commit();
                //string x = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_LEGACY, string.Empty); //works, string is empty.
                SaveHeadersToSharedPrefs();
                SaveAllSearchTabsToDisk(c);
            }
        }

        public static void RestoreHeadersFromSharedPreferences()
        {
            string savedState = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_Headers, string.Empty);
            if (savedState == string.Empty)
            {
                return;
            }
            else
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                MainActivity.LogDebug("HEADERS - base64 string length: " + sw.ElapsedMilliseconds);

                using (System.IO.MemoryStream memStream = new System.IO.MemoryStream(Convert.FromBase64String(savedState)))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    var savedStateDict = formatter.Deserialize(memStream) as Dictionary<int, SavedStateSearchTabHeader>;
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
                }
                sw.Stop();
                MainActivity.LogDebug("HEADERS - RestoreStateFromSharedPreferences: wishlist: " + sw.ElapsedMilliseconds);
            }
            //SoulSeekState.SharedPreferences.Edit().Remove
        }

        public static void RestoreStateFromSharedPreferencesLegacy()
        {
            string savedState = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_LEGACY, string.Empty);
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
                    BinaryFormatter formatter = new BinaryFormatter();
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
            SearchTabHelper.SaveSearchResultsToDisk(lastWishlistID, SoulSeekState.ActiveActivityRef);
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