/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */
using Seeker.Services;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Seeker.Search;
using Android;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.DocumentFile.Provider;
using AndroidX.Fragment.App;
using AndroidX.Lifecycle;
using AndroidX.ViewPager.Widget;
using Common;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Snackbar;
using Google.Android.Material.Tabs;
using Java.IO;
using SlskHelp;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using static Android.Provider.DocumentsContract;
using log = Android.Util.Log;
using Seeker.Serialization;
using AndroidX.Activity;
using Seeker.Transfers;

//using System.IO;
//readme:
//dotnet add package Soulseek --version 1.0.0-rc3.1
//xamarin
//Had to rewrite this one from .csproj
//<Import Project="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild
//\Xamarin\Android\Xamarin.Android.CSharp.targets" />
namespace Seeker
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, Exported = true/*, WindowSoftInputMode = SoftInput.AdjustNothing*/)]
    public partial class MainActivity :
        ThemeableActivity, 
        ActivityCompat.IOnRequestPermissionsResultCallback, 
        BottomNavigationView.IOnNavigationItemSelectedListener
    {



        public static EventHandler<bool> KeyBoardVisibilityChanged;


        public void KeyboardChanged(object sender, bool isShown)
        {
            if (isShown)
            {
                SeekerState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Animate().Alpha(0f).SetDuration(250).SetListener(new BottomNavigationViewAnimationListener());
                //it will be left at 0% opacity! even when unhiding it!

                //SeekerState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Gone;
            }
            else
            {
                //SeekerState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Animate().Alpha(100).SetDuration(250).SetListener(new BottomNavigationViewAnimationListener());
                SeekerState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Visible;
                SeekerState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Animate().Alpha(1f).SetDuration(300).SetListener(null);
                //SeekerState.MainActivityRef.FindViewById<BottomNavigationView>(Resource.Id.navigation).Visibility = ViewStates.Visible;
            }
        }


        public static event EventHandler<TransferItem> TransferAddedUINotify;
        public static void RaiseTransferAddedUINotify(TransferItem item) => TransferAddedUINotify?.Invoke(null, item);

        public static event EventHandler<DownloadAddedEventArgs> DownloadAddedUINotify;

        public static void InvokeDownloadAddedUINotify(DownloadAddedEventArgs e)
        {
            DownloadAddedUINotify?.Invoke(null, e);
        }

        public static void ClearDownloadAddedEventsFromTarget(object target)
        {
            if (DownloadAddedUINotify == null)
            {
                return;
            }
            else
            {
                foreach (Delegate d in DownloadAddedUINotify.GetInvocationList())
                {
                    if (d.Target == null) //i.e. static
                    {
                        continue;
                    }
                    if (d.Target.GetType() == target.GetType())
                    {
                        DownloadAddedUINotify -= (EventHandler<DownloadAddedEventArgs>)d;
                    }
                }
            }
        }

        /// <summary>
        /// Presentable Filename, Uri.ToString(), length
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="directoryCount"></param>
        /// <returns></returns>
        private void traverseToGetDirectories(DocumentFile dir, List<Android.Net.Uri> dirUris)
        {
            if (dir.IsDirectory)
            {
                DocumentFile[] files = dir.ListFiles(); //doesnt need to be sorted
                for (int i = 0; i < files.Length; ++i)
                {
                    DocumentFile file = files[i];
                    if (file.IsDirectory)
                    {
                        dirUris.Add(file.Uri);
                        traverseToGetDirectories(file, dirUris);
                    }
                }
            }
        }

        private void SoulseekClient_SearchResponseDeliveryFailed(object sender, SearchRequestResponseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void SoulseekClient_SearchResponseDelivered(object sender, SearchRequestResponseEventArgs e)
        {

        }


        /// <summary>
        /// returns a list of searchable names (final dir + filename) for other's searches, and then the URI so android can actually get that file.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public void traverseDocumentFile(DocumentFile dir, List<Tuple<string, string, long, string>> pairs, Dictionary<string, List<Tuple<string, string, long>>> auxilaryDuplicatesList, bool isRootCase, string volName, ref int directoryCount)
        {
            if (dir.Exists())
            {
                DocumentFile[] files = dir.ListFiles();
                for (int i = 0; i < files.Length; ++i)
                {
                    DocumentFile file = files[i];
                    if (file.IsDirectory)
                    {
                        directoryCount++;
                        traverseDocumentFile(file, pairs, auxilaryDuplicatesList, false, volName, ref directoryCount);
                    }
                    else
                    {
                        // do something here with the file
                        //Logger.Debug(file.Uri.ToString()); //encoded string representation //content://com.android.externalstorage.documents/tree/primary%3ASoulseek%20Complete/document/primary%3ASoulseek%20Complete%2F41-60%2F14-B-181%20x.mp3
                        //Logger.Debug(file.Uri.Path.ToString()); //gets decoded path // /tree/primary:Soulseek Complete/document/primary:Soulseek Complete/41-60/14-B-181x.mp3
                        //Logger.Debug(Android.Net.Uri.Decode(file.Uri.ToString())); // content://com.android.externalstorage.documents/tree/primary:Soulseek Complete/document/primary:Soulseek Complete/41-60/14-B-181x.mp3
                        //Logger.Debug(file.Uri.EncodedPath);  // /tree/primary%3ASoulseek%20Complete/document/primary%3ASoulseek%20Complete%2F41-60%2F14-B-181%20x.mp3 
                        //Logger.Debug(file.Uri.LastPathSegment); // primary:Soulseek Complete/41-60/14-B-181 Welcome To New York-Taylor Swift.mp3

                        string fullPath = file.Uri.Path.ToString().Replace('/', '\\');
                        string presentableName = file.Uri.LastPathSegment.Replace('/', '\\');

                        string searchableName = Common.Helpers.GetFolderNameFromFile(fullPath) + @"\" + CommonHelpers.GetFileNameFromFile(fullPath);
                        if (isRootCase && (volName != null))
                        {
                            if (searchableName.Substring(0, volName.Length) == volName)
                            {
                                if (searchableName.Length != volName.Length) //i.e. if its just "primary:"
                                {
                                    searchableName = searchableName.Substring(volName.Length);
                                }
                            }
                        }
                        pairs.Add(new Tuple<string, string, long, string>(searchableName, file.Uri.ToString(), file.Length(), presentableName));
                    }
                }
            }
        }

        public void traverseFile(Java.IO.File dir)
        {
            if (dir.Exists())
            {
                Java.IO.File[] files = dir.ListFiles();
                for (int i = 0; i < files.Length; ++i)
                {
                    Java.IO.File file = files[i];
                    if (file.IsDirectory)
                    {
                        traverseFile(file);
                    }
                    else
                    {
                        // do something here with the file
                        Logger.Debug(file.Path);
                        Logger.Debug(file.AbsolutePath);
                        Logger.Debug(file.CanonicalPath);
                    }
                }
            }
        }


        public const string SETTINGS_INTENT = "com.example.seeker.SETTINGS";
        public const int SETTINGS_EXTERNAL = 0x430;
        public const int DEFAULT_SEARCH_RESULTS = 250;
        private const int WRITE_EXTERNAL = 9999;
        private const int NEW_WRITE_EXTERNAL = 0x428;
        private const int MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL = 0x429;
        private const int NEW_WRITE_EXTERNAL_VIA_LEGACY = 0x42A;
        private const int MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY = 0x42B;
        private const int NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen = 0x42C;
        private const int MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen = 0x42D;
        private const int POST_NOTIFICATION_PERMISSION = 0x42E;
        private AndroidX.ViewPager.Widget.ViewPager pager = null;

        public static PowerManager.WakeLock CpuKeepAlive_Transfer = null;
        public static Android.Net.Wifi.WifiManager.WifiLock WifiKeepAlive_Transfer = null;
        public static System.Timers.Timer KeepAliveInactivityKillTimer = null;

        public static void KeepAliveInactivityKillTimerEllapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (CpuKeepAlive_Transfer != null)
            {
                CpuKeepAlive_Transfer.Release();
            }
            if (WifiKeepAlive_Transfer != null)
            {
                WifiKeepAlive_Transfer.Release();
            }
            KeepAliveInactivityKillTimer.Stop();
        }


        private ISharedPreferences sharedPreferences;
        private const string defaultMusicUri = "content://com.android.externalstorage.documents/tree/primary%3AMusic";
        protected override void OnCreate(Bundle savedInstanceState)
        {

            //basically if the Intent created the MainActivity, then we want to handle it (i.e. if from "Search Here")
            //however, if we say rotate the device or leave and come back to it (and the activity got destroyed in the mean time) then
            //it will re-handle the activity each time.  We can check if it is truly "new" by looking at the savedInstanceState.
            bool reborn = false;
            if (savedInstanceState == null)
            {
                Logger.Debug("Main Activity On Create NEW");
            }
            else
            {
                reborn = true;
                Logger.Debug("Main Activity On Create REBORN");
            }


            try
            {
                if (CpuKeepAlive_Transfer == null)
                {
                    CpuKeepAlive_Transfer = ((PowerManager)this.GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Partial, "Seeker Download CPU_Keep_Alive");
                    CpuKeepAlive_Transfer.SetReferenceCounted(false);
                }
                if (WifiKeepAlive_Transfer == null)
                {
                    WifiKeepAlive_Transfer = ((Android.Net.Wifi.WifiManager)this.GetSystemService(Context.WifiService)).CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Seeker Download Wifi_Keep_Alive");
                    WifiKeepAlive_Transfer.SetReferenceCounted(false);
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("error init keepalives: " + e.Message + e.StackTrace);
            }
            if (KeepAliveInactivityKillTimer == null)
            {
                KeepAliveInactivityKillTimer = new System.Timers.Timer(60 * 1000 * 10); //kill after 10 mins of no activity..
                                                                                        //remember that this is a fallback. for when foreground service is still running but nothing is happening otherwise.
                KeepAliveInactivityKillTimer.Elapsed += KeepAliveInactivityKillTimerEllapsed;
                KeepAliveInactivityKillTimer.AutoReset = false;
            }

            //FirebaseCrash.Report();
            //Logger.Firebase("This happened......"));

            //this.Window.SetSoftInputMode();

            base.OnCreate(savedInstanceState);
            //System.Threading.Thread.CurrentThread.Name = "Main Activity Thread";
            Xamarin.Essentials.Platform.Init(this, savedInstanceState); //this is what you are supposed to do.
            SetContentView(Resource.Layout.activity_main);

            //SerializationTests.TestInflateAll(this);

            //AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById(Resource.Id.my_toolbar);
            //SetSupportActionBar(myToolbar);
            BottomNavigationView navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);


            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById(Resource.Id.toolbar);
            myToolbar.Title = this.GetString(Resource.String.home_tab);
            myToolbar.InflateMenu(Resource.Menu.account_menu);
            SetSupportActionBar(myToolbar);
            myToolbar.InflateMenu(Resource.Menu.account_menu); //twice??


            var backPressedCallback = new GenericOnBackPressedCallback(true, onBackPressedAction);
            OnBackPressedDispatcher.AddCallback(backPressedCallback);

            System.Console.WriteLine("Testing.....");

            sharedPreferences = this.GetSharedPreferences(Constants.SharedPrefFile, 0);

            //if (uiModeManager.NightMode == UiModeManager.ModeNightYes)
            //{
            //    // System is in Night mode
            //}
            //else if (uiModeManager.NightMode == Android.App.UiNightMode.Yes)
            //{
            //    // System is in Day mode
            //}


            //this.RequestPostNotificationPermissionsIfApplicable();

            //restoreSeekerState(savedInstanceState);

            TabLayout tabs = (TabLayout)FindViewById(Resource.Id.tabs);

            pager = (AndroidX.ViewPager.Widget.ViewPager)FindViewById(Resource.Id.pager);
            pager.PageSelected += Pager_PageSelected;
            TabsPagerAdapter adapter = new TabsPagerAdapter(SupportFragmentManager);

            tabs.TabSelected += Tabs_TabSelected;
            pager.Adapter = adapter;
            pager.AddOnPageChangeListener(new OnPageChangeLister1());
            //tabs.SetupWithViewPager(pager);
            //this is a relatively safe way that prevents rotates from redoing the intent.
            bool alreadyHandled = Intent.GetBooleanExtra("ALREADY_HANDLED", false);
            Intent = Intent.PutExtra("ALREADY_HANDLED", true);
            //Intent = i;
            if (Intent != null)
            {
                //if(Intent.Flags == (ActivityFlags.LaunchedFromHistory | ActivityFlags.NewTask))
                //{
                //    //FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY | FLAG_ACTIVITY_NEW_TASK
                //    //-back button then resumed from history
                //    //FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY
                //    //-home button then resumed from history
                //    //FLAG_ACTIVITY_NEW_TASK
                //    //-clicking app icon or intent filter
                //    Logger.Debug("new task | launched from history");
                //}


                if (Intent.GetIntExtra(DownloadForegroundService.FromTransferString, -1) == 2)
                {
                    pager.SetCurrentItem(2, false);
                }
                else if (Intent.GetIntExtra(SeekerApplication.FromFolderAlert, -1) == 2)
                {
                    pager.SetCurrentItem(2, false);
                }
                else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToBrowse, -1) == 3)
                {
                    pager.SetCurrentItem(3, false);
                }
                else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToSearch, -1) == 1)
                {
                    //var navigator = SeekerState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                    //navigator.NavigationItemReselected += Navigator_NavigationItemReselected;
                    //navigator.NavigationItemSelected += Navigator_NavigationItemSelected;
                    //navigator.ViewAttachedToWindow += Navigator_ViewAttachedToWindow;
                    pager.SetCurrentItem(1, false);
                }
                else if (Intent.GetIntExtra(UserListActivity.IntentSearchRoom, -1) == 1)
                {
                    pager.SetCurrentItem(1, false);
                }
                else if (Intent.GetIntExtra(WishlistController.FromWishlistString, -1) == 1 && !reborn) //if its not reborn then the OnNewIntent will handle it...
                {
                    SeekerState.MainActivityRef = this; //set these early. they are needed
                    SeekerState.ActiveActivityRef = this;

                    Logger.InfoFirebase("is resumed: " + (SearchFragment.Instance?.IsResumed ?? false).ToString());
                    Logger.InfoFirebase("from wishlist clicked");
                    int currentPage = pager.CurrentItem;
                    int tabID = Intent.GetIntExtra(WishlistController.FromWishlistStringID, int.MaxValue);
                    if (currentPage == 1) //this is the case even if process previously got am state killed.
                    {
                        Logger.InfoFirebase("from wishlist clicked - current page");
                        if (tabID == int.MaxValue)
                        {
                            Logger.Firebase("tabID == int.MaxValue");
                        }
                        else if (!SearchTabHelper.SearchTabCollection.ContainsKey(tabID))
                        {
                            Logger.Firebase("doesnt contain key");
                            Toast.MakeText(this, this.GetString(Resource.String.wishlist_tab_error), ToastLength.Long).Show();
                        }
                        else
                        {
                            if (SearchFragment.Instance?.IsResumed ?? false) //!??! this logic is backwards...
                            {
                                Logger.Debug("we are on the search page but we need to wait for OnResume search frag");
                                goToSearchTab = tabID; //we read this we resume
                            }
                            else
                            {
                                SearchFragment.Instance.GoToTab(tabID, false, true);
                            }
                        }
                    }
                    else
                    {
                        Logger.InfoFirebase("from wishlist clicked - different page");
                        //when we move to the page, lets move to our tab, if its not the current one..
                        goToSearchTab = tabID; //we read this when we move tab...
                        pager.SetCurrentItem(1, false);
                    }
                }
                else if (((Intent.GetIntExtra(UploadForegroundService.FromTransferUploadString, -1) == 2) || (Intent.GetIntExtra(UPLOADS_NOTIF_EXTRA, -1) == 2)) && !alreadyHandled) //else every rotation will change Downloads to Uploads.
                {
                    HandleFromNotificationUploadIntent();
                }
                else if (Intent.GetIntExtra(SettingsActivity.FromBrowseSelf, -1) == 3)
                {
                    Logger.InfoFirebase("from browse self");
                    pager.SetCurrentItem(3, false);
                }
                else if (SearchSendIntentHelper.IsFromActionSend(Intent) && !reborn) //this will always create a new instance, so if its reborn then its an old intent that we already followed.
                {
                    SeekerState.MainActivityRef = this;
                    SeekerState.ActiveActivityRef = this;
                    Logger.Debug("MainActivity action send intent");
                    //give us a new fresh tab if the current one has a search in it...
                    if (!string.IsNullOrEmpty(SearchTabHelper.LastSearchTerm))
                    {
                        Logger.Debug("lets go to a new fresh tab");
                        int newTabToGoTo = SearchTabHelper.AddSearchTab();

                        Logger.Debug("search fragment null? " + (SearchFragment.Instance == null).ToString());

                        if (SearchFragment.Instance?.IsResumed ?? false)
                        {
                            //if resumed is true
                            SearchFragment.Instance.GoToTab(newTabToGoTo, false, true);
                        }
                        else
                        {
                            Logger.Debug("we are on the search page but we need to wait for OnResume search frag");
                            goToSearchTab = newTabToGoTo; //we read this we resume
                        }
                    }

                    //go to search tab
                    Logger.Debug("prev search term: " + SearchDialog.SearchTerm);
                    SearchDialog.SearchTerm = Intent.GetStringExtra(Intent.ExtraText);
                    SearchDialog.IsFollowingLink = false;
                    pager.SetCurrentItem(1, false);
                    if (SearchSendIntentHelper.TryParseIntent(Intent, out string searchTermFound))
                    {
                        //we are done parsing the intent
                        SearchDialog.SearchTerm = searchTermFound;
                    }
                    else if (SearchSendIntentHelper.FollowLinkTaskIfApplicable(Intent))
                    {
                        SearchDialog.IsFollowingLink = true;
                    }
                    //close previous instance
                    if (SearchDialog.Instance != null)
                    {
                        Logger.Debug("previous instance exists");
                        //SearchDialog.Instance.Dismiss(); //throws exception, cannot perform this action after onSaveInstanceState
                    }
                    var searchDialog = new SearchDialog(SearchDialog.SearchTerm, SearchDialog.IsFollowingLink);
                    searchDialog.Show(SupportFragmentManager, "Search Dialog");
                }
            }


            SeekerState.MainActivityRef = this;
            SeekerState.ActiveActivityRef = this;


            //UploadDirectoryManager.UploadDirectories = new List<UploadDirectoryInfo>();
            //UploadDirectoryManager.UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic/document/864A-C3E8%3AMusic", true, false, false, "Music (1)"));
            //UploadDirectoryManager.UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic%2F%5B2000%5D%20Spirit%20x/document/864A-C3E8%3AMusic%2F%5B2000%5D%20Spirix", true, true, false, null));
            //UploadDirectoryManager.//UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/primary%3AMusic/document/primary%3AMusic", true, false, false, null));
            //UploadDirectoryManager.UploadDirectories.Add(new UploadDirectoryInfo(@"content://com.android.externalstorage.documents/tree/864A-C3E8%3AMusic/document/864A-C3E8%3AMusic", true, false, false, "Music (1)"));


            //if we have all the conditions to share, then set sharing up.
            if (SharedFileService.MeetsSharingConditions() && !SeekerState.IsParsing && !SharedFileService.IsSharingSetUpSuccessfully())
            {
                Seeker.Services.SharingService.SetUpSharing();
            }
            else if (SeekerState.NumberOfSharedDirectoriesIsStale)
            {
                SharedFileService.InformServerOfSharedFiles();
                SeekerState.AttemptedToSetUpSharing = true;
            }


            //this.DeleteSharedPreferences("SoulSeekPrefs");

            //Mono.Nat.NatUtility.DeviceFound += NatUtility_DeviceFound; //error bad message parsePAL_UPNP_SOAP_E_INVALID_ARGS. was able to look at all the current wifi port mappings however :)
            //Mono.Nat.NatUtility.StartDiscovery();


            //InternetGatewayDevice[] IGDs = InternetGatewayDevice.getDevices(60000);
            //if (IGDs != null)
            //{
            //    for (int i = 0; i < IGDs.Length; i++)
            //    {

            //    }
            //}
            //Android.Net.Wifi.WifiManager wm = this.GetSystemService(WifiService);
            //wm.
            //Mono.Nat.NatUtility.UnknownDeviceFound += NatUtility_DeviceFound;



            SeekerState.SharedPreferences = sharedPreferences;
            SeekerState.MainActivityRef = this;
            SeekerState.ActiveActivityRef = this;

            UpdateForScreenSize();

            //#if DEBUG //todo remove

            //WishlistController.SearchIntervalMilliseconds = 1000*30;
            //WishlistController.Initialize();

            //#endif


            if (SeekerState.UseLegacyStorage())
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Denied)
                {
                    ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage }, WRITE_EXTERNAL);
                }
                //file picker with legacy case
                if (!string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri))
                {
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    Android.Net.Uri chosenUri = Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);
                    bool canWrite = false;
                    try
                    {
                        //a phone failed 4 times with //POCO X3 Pro
                        //Android 11(SDK 30)
                        //Caused by: java.lang.IllegalArgumentException: 
                        //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                        //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                        if (SeekerState.PreOpenDocumentTree() || !PreferencesState.SaveDataDirectoryUriIsFromTree)
                        {
                            canWrite = DocumentFile.FromFile(new Java.IO.File(chosenUri.Path)).CanWrite();
                        }
                        else
                        {
                            //on changing the code and restarting for api 22 
                            //persistenduripermissions is empty
                            //and exists is false, cannot list files

                            //var list1 = this.ContentResolver.PersistedUriPermissions;
                            //foreach(var item1 in list1)
                            //{
                            //    string content1 = item1.Uri.Path;
                            //}


                            canWrite = DocumentFile.FromTreeUri(this, chosenUri).CanWrite();
                        }
                    }
                    catch (Exception e)
                    {
                        if (chosenUri != null)
                        {
                            //legacy DocumentFile.FromTreeUri failed with URI: /tree/2A6B-256B:Seeker/Soulseek Complete Invalid URI: /tree/2A6B-256B:Seeker/Soulseek Complete
                            //legacy DocumentFile.FromTreeUri failed with URI: /tree/raw:/storage/emulated/0/Download/Soulseek Downloads Invalid URI: /tree/raw:/storage/emulated/0/Download/Soulseek Downloads
                            Logger.Firebase("legacy DocumentFile.FromTreeUri failed with URI: " + chosenUri.ToString() + " " + e.Message + " scheme " + chosenUri.Scheme);
                        }
                        else
                        {
                            Logger.Firebase("legacy DocumentFile.FromTreeUri failed with null URI");
                        }
                    }
                    if (canWrite)
                    {
                        if (SeekerState.PreOpenDocumentTree())
                        {
                            SeekerState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(chosenUri.Path));
                        }
                        else
                        {
                            SeekerState.RootDocumentFile = DocumentFile.FromTreeUri(this, chosenUri);
                        }
                    }
                    else
                    {
                        Logger.Firebase("cannot write" + chosenUri?.ToString() ?? "null");
                    }
                }

                //now for incomplete
                if (!string.IsNullOrEmpty(PreferencesState.ManualIncompleteDataDirectoryUri))
                {
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    Android.Net.Uri chosenIncompleteUri = Android.Net.Uri.Parse(PreferencesState.ManualIncompleteDataDirectoryUri);
                    bool canWrite = false;
                    try
                    {
                        //a phone failed 4 times with //POCO X3 Pro
                        //Android 11(SDK 30)
                        //Caused by: java.lang.IllegalArgumentException: 
                        //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                        //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                        if (SeekerState.PreOpenDocumentTree() || !PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree)
                        {
                            canWrite = DocumentFile.FromFile(new Java.IO.File(chosenIncompleteUri.Path)).CanWrite();
                        }
                        else
                        {
                            //on changing the code and restarting for api 22 
                            //persistenduripermissions is empty
                            //and exists is false, cannot list files

                            //var list1 = this.ContentResolver.PersistedUriPermissions;
                            //foreach(var item1 in list1)
                            //{
                            //    string content1 = item1.Uri.Path;
                            //}


                            canWrite = DocumentFile.FromTreeUri(this, chosenIncompleteUri).CanWrite();
                        }
                    }
                    catch (Exception e)
                    {
                        if (chosenIncompleteUri != null)
                        {
                            Logger.Firebase("legacy Incomplete DocumentFile.FromTreeUri failed with URI: " + chosenIncompleteUri.ToString() + " " + e.Message);
                        }
                        else
                        {
                            Logger.Firebase("legacy Incomplete DocumentFile.FromTreeUri failed with null URI");
                        }
                    }
                    if (canWrite)
                    {
                        if (SeekerState.PreOpenDocumentTree())
                        {
                            SeekerState.RootIncompleteDocumentFile = DocumentFile.FromFile(new Java.IO.File(chosenIncompleteUri.Path));
                        }
                        else
                        {
                            SeekerState.RootIncompleteDocumentFile = DocumentFile.FromTreeUri(this, chosenIncompleteUri);
                        }
                    }
                    else
                    {
                        Logger.Firebase("cannot write incomplete" + chosenIncompleteUri?.ToString() ?? "null");
                    }
                }


            }
            else
            {

                Android.Net.Uri res = null; //var y = MediaStore.Audio.Media.ExternalContentUri.ToString();
                if (string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri))
                {
                    //try
                    //{
                    //    //storage/emulated/0/music
                    //    Java.IO.File f = new Java.IO.File(@"/storage/emulated/0/Music");///Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic);
                    //    //res = f.ToURI();
                    //    res = Android.Net.Uri.FromFile(f);//Parse(f.ToURI().ToString());
                    //}
                    //catch
                    //{
                    //    res = Android.Net.Uri.Parse(defaultMusicUri);//TryCreate("content://com.android.externalstorage.documents/tree/primary%3AMusic", UriKind.Absolute,out res);
                    //}
                    res = Android.Net.Uri.Parse(defaultMusicUri);
                }
                else
                {
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    res = Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);
                }

                bool canWrite = false;
                try
                {
                    //a phone failed 4 times with //POCO X3 Pro
                    //Android 11(SDK 30)
                    //Caused by: java.lang.IllegalArgumentException: 
                    //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                    //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                    if (SeekerState.PreOpenDocumentTree() || !PreferencesState.SaveDataDirectoryUriIsFromTree) //this will never get hit..
                    {
                        canWrite = DocumentFile.FromFile(new Java.IO.File(res.Path)).CanWrite();
                    }
                    else
                    {
                        canWrite = DocumentFile.FromTreeUri(this, res).CanWrite();
                    }

                    // if canwrite is false then if we try to create a file we get null.
                    //if (PreferencesState.SaveDataDirectoryUriIsFromTree)
                    //{
                    //    SeekerState.RootDocumentFile = DocumentFile.FromTreeUri(this, res);

                    //}
                    //else
                    //{
                    //    SeekerState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(res.Path));
                    //}

                    //var file1 = SeekerState.RootDocumentFile.CreateFile("text/plain", "testing_1234.txt");


                }
                catch (Exception e)
                {
                    if (res != null)
                    {
                        Logger.Firebase("DocumentFile.FromTreeUri failed with URI: " + res.ToString() + " " + e.Message);
                    }
                    else
                    {
                        Logger.Firebase("DocumentFile.FromTreeUri failed with null URI");
                    }
                }
                //if (DocumentFile.FromTreeUri(this, Uri.TryCreate("",UriKind.Absolute)))
                if (!canWrite)
                {

                    var b = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
                    b.SetTitle(this.GetString(Resource.String.seeker_needs_dl_dir));
                    b.SetMessage(this.GetString(Resource.String.seeker_needs_dl_dir_content));
                    ManualResetEvent mre = new ManualResetEvent(false);
                    EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                    {
                        var storageManager = Android.OS.Storage.StorageManager.FromContext(this);
                        var intent = storageManager.PrimaryStorageVolume.CreateOpenDocumentTreeIntent();
                        intent.PutExtra(DocumentsContract.ExtraInitialUri, res);
                        intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPrefixUriPermission);
                        try
                        {
                            this.StartActivityForResult(intent, NEW_WRITE_EXTERNAL);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains(CommonHelpers.NoDocumentOpenTreeToHandle))
                            {
                                FallbackFileSelectionEntry(false);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    });
                    b.SetPositiveButton(Resource.String.okay, eventHandler);
                    b.SetCancelable(false);
                    b.Show();


                    //this.SendBroadcast(storageManager.PrimaryStorageVolume.CreateOpenDocumentTreeIntent());
                }
                else
                {
                    if (PreferencesState.SaveDataDirectoryUriIsFromTree)
                    {
                        SeekerState.RootDocumentFile = DocumentFile.FromTreeUri(this, res);

                    }
                    else
                    {
                        SeekerState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(res.Path));
                    }
                }

                bool manualSet = false;
                //for incomplete case
                Android.Net.Uri incompleteRes = null; //var y = MediaStore.Audio.Media.ExternalContentUri.ToString();
                if (!string.IsNullOrEmpty(PreferencesState.ManualIncompleteDataDirectoryUri))
                {
                    manualSet = true;
                    // an example of a random bad url that passes parsing but fails FromTreeUri: "file:/media/storage/sdcard1/data/example.externalstorage/files/"
                    incompleteRes = Android.Net.Uri.Parse(PreferencesState.ManualIncompleteDataDirectoryUri);
                }
                else
                {
                    manualSet = false;
                }

                if (manualSet)
                {
                    bool canWriteIncomplete = false;
                    try
                    {
                        //a phone failed 4 times with //POCO X3 Pro
                        //Android 11(SDK 30)
                        //Caused by: java.lang.IllegalArgumentException: 
                        //at android.provider.DocumentsContract.getTreeDocumentId(DocumentsContract.java:1278)
                        //at androidx.documentfile.provider.DocumentFile.fromTreeUri(DocumentFile.java:136)
                        if (SeekerState.PreOpenDocumentTree() || !PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree)
                        {
                            canWriteIncomplete = DocumentFile.FromFile(new Java.IO.File(incompleteRes.Path)).CanWrite();
                        }
                        else
                        {
                            canWriteIncomplete = DocumentFile.FromTreeUri(this, incompleteRes).CanWrite();
                        }
                    }
                    catch (Exception e)
                    {
                        if (incompleteRes != null)
                        {
                            Logger.Firebase("DocumentFile.FromTreeUri failed with incomplete URI: " + incompleteRes.ToString() + " " + e.Message);
                        }
                        else
                        {
                            Logger.Firebase("DocumentFile.FromTreeUri failed with incomplete null URI");
                        }
                    }
                    if (canWriteIncomplete)
                    {
                        if (SeekerState.PreOpenDocumentTree() || !PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree)
                        {
                            SeekerState.RootIncompleteDocumentFile = DocumentFile.FromFile(new Java.IO.File(incompleteRes.Path));
                        }
                        else
                        {
                            SeekerState.RootIncompleteDocumentFile = DocumentFile.FromTreeUri(this, incompleteRes);
                        }
                    }
                }
            }
        }

        public void FallbackFileSelection(int requestCode)
        {
            //Create FolderOpenDialog
            SimpleFileDialog fileDialog = new SimpleFileDialog(SeekerState.ActiveActivityRef, SimpleFileDialog.FileSelectionMode.FolderChoose);
            fileDialog.GetFileOrDirectoryAsync(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath).ContinueWith(
                (Task<string> t) =>
                {
                    if (t.Result == null || t.Result == string.Empty)
                    {
                        this.OnActivityResult(requestCode, Result.Canceled, new Intent());
                        return;
                    }
                    else
                    {
                        var intent = new Intent();
                        DocumentFile f = DocumentFile.FromFile(new Java.IO.File(t.Result));
                        intent.SetData(f.Uri);
                        this.OnActivityResult(requestCode, Result.Ok, intent);
                    }
                });
        }

        public static Action<Task> GetPostNotifPermissionTask()
        {
            return new Action<Task>((task) =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    RequestPostNotificationPermissionsIfApplicable();
                }
            });

        }

        private static bool postNotficationAlreadyRequestedInSession = false;
        /// <summary>
        /// As far as where to place this, doing it on launch is no good (as they will already
        ///   see yet another though more important permission in the background behind them).
        /// Doing this on login (i.e. first session login) seems decent.
        /// </summary>
        private static void RequestPostNotificationPermissionsIfApplicable()
        {
            if (postNotficationAlreadyRequestedInSession)
            {
                return;
            }
            postNotficationAlreadyRequestedInSession = true;

            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return;
            }

            try
            {
                if (ContextCompat.CheckSelfPermission(SeekerState.ActiveActivityRef, Manifest.Permission.PostNotifications) == Android.Content.PM.Permission.Denied)
                {
                    bool alreadyShown = SeekerState.SharedPreferences.GetBoolean(KeyConsts.M_PostNotificationRequestAlreadyShown, false);
                    if (alreadyShown)
                    {
                        return;
                    }

                    if (OnUIthread())
                    {
                        RequestNotifPermissionsLogic();
                    }
                    else
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(RequestNotifPermissionsLogic);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("RequestPostNotificationPermissionsIfApplicable error: " + e.Message + e.StackTrace);
            }
        }

        // recommended way, if user only denies once then next time (ShouldShowRequestPermissionRationale lets us know this)
        //   then show a blurb on what the permissions are used for and ask a second (and last) time.
        private static void RequestNotifPermissionsLogic()
        {
            try
            {
                void setAlreadyShown()
                {
                    PreferencesManager.SavePostNotificationShown();
                }

                ActivityCompat.RequestPermissions(SeekerState.ActiveActivityRef, new string[] { Manifest.Permission.PostNotifications }, POST_NOTIFICATION_PERMISSION);
                setAlreadyShown();

                // better to not bother the user.. they know what notifications are and they know how to turn them on in settings after the fact.
                // also the dialog asking someone "okay" to then show the android "yes"/"no" dialog feels weird (even if recommended way).
                //if (ActivityCompat.ShouldShowRequestPermissionRationale(SeekerState.ActiveActivityRef, Manifest.Permission.PostNotifications))
                //{
                //    var b = new AndroidX.AppCompat.App.AlertDialog.Builder(SeekerState.ActiveActivityRef, Resource.Style.MyAlertDialogTheme);
                //    b.SetTitle("Allow Notifications?");
                //    b.SetMessage("Seeker provides push notifications to keep you updated on file uploads/downloads and incoming user messages.");
                //    ManualResetEvent mre = new ManualResetEvent(false);

                //    // make sure we never prompt the user for these permissions again

                //    EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                //    {
                //        ActivityCompat.RequestPermissions(SeekerState.ActiveActivityRef, new string[] { Manifest.Permission.PostNotifications }, POST_NOTIFICATION_PERMISSION);
                //        setAlreadyShown();
                //    });
                //    b.SetPositiveButton(Resource.String.okay, eventHandler);

                //    EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs noArgs) =>
                //    {
                //        setAlreadyShown();
                //    });
                //    b.SetNegativeButton("No thanks", eventHandlerCancel);

                //    b.SetCancelable(false);
                //    b.Show();
                //}
            }
            catch (Exception e)
            {
                Logger.Firebase("RequestPostNotificationPermissionsIfApplicable error: " + e.Message + e.StackTrace);
            }
        }

        protected override void OnStart()
        {
            //this fixes a bug as follows:
            //previously we only set MainActivityRef on Create.
            //therefore if one launches MainActivity via a new intent (i.e. go to user list, then search users files) it will be set with the new search user activity.
            //then if you press back twice you will see the original activity but the MainActivityRef will still be set to the now destroyed activity since it was last to call onCreate.
            //so then the FragmentManager will be null among other things...
            SeekerState.MainActivityRef = this;
            base.OnStart();
        }
        public static bool fromNotificationMoveToUploads = false;
        protected override void OnNewIntent(Intent intent)
        {
            Logger.Debug("OnNewIntent");
            base.OnNewIntent(intent);
            Intent = intent.PutExtra("ALREADY_HANDLED", true);
            if (Intent.GetIntExtra(WishlistController.FromWishlistString, -1) == 1)
            {
                Logger.InfoFirebase("is null: " + (SearchFragment.Instance?.Activity == null || (SearchFragment.Instance?.IsResumed ?? false)).ToString());
                Logger.InfoFirebase("from wishlist clicked");
                int currentPage = pager.CurrentItem;
                int tabID = Intent.GetIntExtra(WishlistController.FromWishlistStringID, int.MaxValue);
                if (currentPage == 1)
                {
                    if (tabID == int.MaxValue)
                    {
                        Logger.Firebase("tabID == int.MaxValue");
                    }
                    else if (!SearchTabHelper.SearchTabCollection.ContainsKey(tabID))
                    {
                        Toast.MakeText(this, this.GetString(Resource.String.wishlist_tab_error), ToastLength.Long).Show();
                    }
                    else
                    {
                        if (SearchFragment.Instance?.Activity == null || (SearchFragment.Instance?.IsResumed ?? false))
                        {
                            Logger.Debug("we are on the search page but we need to wait for OnResume search frag");
                            goToSearchTab = tabID; //we read this we resume
                        }
                        else
                        {
                            SearchFragment.Instance.GoToTab(tabID, false, true);
                        }
                    }
                }
                else
                {
                    //when we move to the page, lets move to our tab, if its not the current one..
                    goToSearchTab = tabID; //we read this when we move tab...
                    pager.SetCurrentItem(1, false);
                }
            }
            else if (((Intent.GetIntExtra(UploadForegroundService.FromTransferUploadString, -1) == 2) || (Intent.GetIntExtra(UPLOADS_NOTIF_EXTRA, -1) == 2))) //else every rotation will change Downloads to Uploads.
            {
                HandleFromNotificationUploadIntent();
            }
            else if (Intent.GetIntExtra(SettingsActivity.FromBrowseSelf, -1) == 3)
            {
                Logger.InfoFirebase("from browse self");
                pager.SetCurrentItem(3, false);
            }
            else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToBrowse, -1) == 3)
            {
                pager.SetCurrentItem(3, false);
            }
            else if (Intent.GetIntExtra(UserListActivity.IntentUserGoToSearch, -1) == 1)
            {
                //var navigator = SeekerState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                //navigator.NavigationItemReselected += Navigator_NavigationItemReselected;
                //navigator.NavigationItemSelected += Navigator_NavigationItemSelected;
                //navigator.ViewAttachedToWindow += Navigator_ViewAttachedToWindow;
                pager.SetCurrentItem(1, false);
            }
            else if (Intent.GetIntExtra(DownloadForegroundService.FromTransferString, -1) == 2)
            {
                pager.SetCurrentItem(2, false);
            }
            else if (Intent.GetIntExtra(SeekerApplication.FromFolderAlert, -1) == 2)
            {
                pager.SetCurrentItem(2, false);
            }
        }

        private void HandleFromNotificationUploadIntent()
        {
            //either we change to uploads mode now (if resumed), or we wait for on resume to do it.

            Logger.InfoFirebase("from uploads clicked");
            int currentPage = pager.CurrentItem;
            if (currentPage == 2)
            {
                if (StaticHacks.TransfersFrag?.Activity == null || (StaticHacks.TransfersFrag?.IsResumed ?? false))
                {
                    Logger.InfoFirebase("we need to wait for on resume");
                    fromNotificationMoveToUploads = true; //we read this in onresume
                }
                else
                {
                    //we can change to uploads mode now
                    Logger.Debug("go to upload now");
                    StaticHacks.TransfersFrag.MoveToUploadForNotif();
                }
            }
            else
            {
                fromNotificationMoveToUploads = true; //we read this in onresume
                pager.SetCurrentItem(2, false);
            }
        }

        /// <summary>
        /// This is responsible for filing the PMs into the data structure...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoulseekClient_PrivateMessageReceived(object sender, PrivateMessageReceivedEventArgs e)
        {
            AddMessage(e);
            //string msg = e.Message;
            //throw new NotImplementedException();
        }

        private void AddMessage(PrivateMessageReceivedEventArgs messageEvent)
        {

        }

        private void Navigator_ViewAttachedToWindow(object sender, View.ViewAttachedToWindowEventArgs e)
        {
            // throw new NotImplementedException();
        }
        private void OnCloseClick(object sender, DialogClickEventArgs e)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.user_list_action:
                    Intent intent = new Intent(SeekerState.MainActivityRef, typeof(UserListActivity));
                    SeekerState.MainActivityRef.StartActivityForResult(intent, 141);
                    return true;
                case Resource.Id.messages_action:
                    Intent intentMessages = new Intent(SeekerState.MainActivityRef, typeof(MessagesActivity));
                    SeekerState.MainActivityRef.StartActivityForResult(intentMessages, 142);
                    return true;
                case Resource.Id.chatroom_action:
                    Intent intentChatroom = new Intent(SeekerState.MainActivityRef, typeof(ChatroomActivity));
                    SeekerState.MainActivityRef.StartActivityForResult(intentChatroom, 143);
                    return true;
                case Resource.Id.settings_action:
                    Intent intent2 = new Intent(SeekerState.MainActivityRef, typeof(SettingsActivity));
                    SeekerState.MainActivityRef.StartActivityForResult(intent2, 140);
                    return true;
                case Resource.Id.shutdown_action:
                    Intent intent3 = new Intent(this, typeof(CloseActivity));
                    //Clear all activities and start new task
                    //ClearTask - causes any existing task that would be associated with the activity 
                    // to be cleared before the activity is started. can only be used in conjunction with NewTask.
                    // basically it clears all activities in the current task.
                    intent3.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    this.StartActivity(intent3);
                    if (OperatingSystem.IsAndroidVersionAtLeast(21))
                    {
                        this.FinishAndRemoveTask();
                    }
                    else
                    {
                        this.FinishAffinity();
                    }
                    return true;
                case Resource.Id.about_action:
                    var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
                    //var diag = builder.SetMessage(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.about_body).TrimStart(' '), SeekerApplication.GetVersionString())).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
                    var diag = builder.SetMessage(Resource.String.about_body).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
                    diag.Show();
                    var origString = string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.about_body), SeekerApplication.GetVersionString()); //this is a literal CDATA string.
                    if (OperatingSystem.IsAndroidVersionAtLeast(24))
                    {
                        ((TextView)diag.FindViewById(Android.Resource.Id.Message)).TextFormatted = Android.Text.Html.FromHtml(origString, Android.Text.FromHtmlOptions.ModeLegacy); //this can be slow so do NOT do it in loops...
                    }
                    else
                    {
                        ((TextView)diag.FindViewById(Android.Resource.Id.Message)).TextFormatted = Android.Text.Html.FromHtml(origString); //this can be slow so do NOT do it in loops...
                    }
                    ((TextView)diag.FindViewById(Android.Resource.Id.Message)).MovementMethod = (Android.Text.Method.LinkMovementMethod.Instance);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public static void ShowSimpleAlertDialog(Context c, int messageResourceString, int actionResourceString)
        {

            void OnCloseClick(object sender, DialogClickEventArgs e)
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            }

            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(c, Resource.Style.MyAlertDialogTheme);
            //var diag = builder.SetMessage(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.about_body).TrimStart(' '), SeekerApplication.GetVersionString())).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            var diag = builder.SetMessage(messageResourceString).SetPositiveButton(actionResourceString, OnCloseClick).Create();
            diag.Show();
        }


        public const string UPLOADS_CHANNEL_ID = "upload channel ID";
        public const string UPLOADS_CHANNEL_NAME = "Upload Notifications";
        public const string UPLOADS_NOTIF_EXTRA = "From Upload";





        ///// <summary>
        ///// this is for global uploading event handling only.  the tabpageadapter is the one for downloading... and for upload tranferpage specific events
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void Upload_TransferProgressUpdated(object sender, TransferProgressUpdatedEventArgs e)
        //{
        //    if (e.Transfer.Direction == TransferDirection.Download)
        //    {
        //        return;
        //    }
        //    //the transfer has average speed already calcuated for us... it also has remainingtime for us too :)
        //}





        /// <summary>
        ///     Creates and returns an <see cref="IEnumerable{T}"/> of <see cref="Soulseek.Directory"/> in response to a remote request.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="endpoint">The IP endpoint of the requesting user.</param>
        /// <returns>A Task resolving an IEnumerable of Soulseek.Directory.</returns>

        /// <summary>
        /// Do this on any changes (like in Settings) but also on Login.
        /// </summary>
        /// <param name="informServerOfChangeIfThereIsAChange"></param>
        /// <param name="force">force if we are chaning the upload directory...</param>

        public void SetUpLoginContinueWith(Task t)
        {
            if (t == null)
            {
                return;
            }
            if (SharedFileService.MeetsSharingConditions())
            {

                Action<Task> getAndSetLoggedInInfoAction = new Action<Task>((Task t) =>
                {
                    //we want to 
                    //UpdateStatus ??
                    //inform server if we are sharing..
                    //get our upload speed..
                    if (t.Status == TaskStatus.Faulted || t.IsFaulted || t.IsCanceled)
                    {
                        return;
                    }
                    SharedFileService.InformServerOfSharedFiles(); //dont need to get the result of this one.
                    SeekerState.SoulseekClient.GetUserDataAsync(PreferencesState.Username); //the result of this one if from an event handler..
                    //.ContinueWith(
                    //    (Task<UserData> userDataTask) =>
                    //    {
                    //        if(userDataTask.IsFaulted)
                    //        {
                    //            Logger.Firebase("userDataTask is faulted " + userDataTask.Exception);
                    //            return;
                    //        }
                    //        else
                    //        {
                    //            //userDataTask.Result.AverageSpeed;
                    //        }

                    //    });

                });
                t.ContinueWith(getAndSetLoggedInInfoAction);
            }
        }

        public bool OnBrowseTab()
        {
            try
            {
                var pager = (AndroidX.ViewPager.Widget.ViewPager)FindViewById(Resource.Id.pager);
                return pager.CurrentItem == 3;
            }
            catch
            {
                Logger.Firebase("OnBrowseTab failed");
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        private void onBackPressedAction(OnBackPressedCallback callback)
        {
            bool relevant = false;
            try
            {
                //TabLayout tabs = (TabLayout)FindViewById(Resource.Id.tabs); returns -1
                var pager = (AndroidX.ViewPager.Widget.ViewPager)FindViewById(Resource.Id.pager);
                if (pager.CurrentItem == 3) //browse tab
                {
                    relevant = BrowseFragment.Instance.BackButton();
                }
                else if (pager.CurrentItem == 2) //transfer tab
                {
                    if (TransfersFragment.GetCurrentlySelectedFolder() != null)
                    {
                        if (TransfersFragment.InUploadsMode)
                        {
                            TransfersFragment.CurrentlySelectedUploadFolder = null;
                        }
                        else
                        {
                            TransfersFragment.CurrentlySelectedDLFolder = null;
                        }
                        SetTransferSupportActionBarState();
                        this.InvalidateOptionsMenu();
                        //((pager.Adapter as TabsPagerAdapter).GetItem(2) as TransfersFragment).SetRecyclerAdapter();  //if you go to transfers rotate phone and then OnBackPressed gets hit,. the fragment that getitem returns will be very old.
                        //((pager.Adapter as TabsPagerAdapter).GetItem(2) as TransfersFragment).RestoreScrollPosition();
                        StaticHacks.TransfersFrag.SetRecyclerAdapter();
                        StaticHacks.TransfersFrag.RestoreScrollPosition();
                        relevant = true;
                    }
                }
            }
            catch (Exception e)
            {
                //During Back Button: Attempt to invoke virtual method 'java.lang.Object android.content.Context.getSystemService(java.lang.String)' on a null object reference
                Logger.Firebase("During Back Button: " + e.Message);
            }
            if (!relevant)
            {
                callback.Enabled = false;
                OnBackPressedDispatcher.OnBackPressed();
                callback.Enabled = true;
            }
        }

        public static void DebugLogHandler(object sender, SoulseekClient.ErrorLogEventArgs e)
        {
            Logger.Debug(e.Message);
        }

        public static void SoulseekClient_ErrorLogHandler(object sender, SoulseekClient.ErrorLogEventArgs e)
        {
            if (e?.Message != null)
            {
                if (e.Message.Contains("Operation timed out"))
                {
                    //this happens to me all the time and it is literally fine
                    return;
                }
            }
            Logger.Firebase(e.Message);
        }

        public static bool IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(Action<Task> action, string msg = null, Context contextToUseForMessage = null)
        {
            lock (SeekerApplication.OurCurrentLoginTaskSyncObject)
            {
                //old: SeekerState.SoulseekClient.State.HasFlag(Soulseek.SoulseekClientStates.Connecting) || SeekerState.SoulseekClient.State.HasFlag(Soulseek.SoulseekClientStates.LoggingIn). this is not good enough since you can still pass this if you are connected but not yet Logging In!
                if (!SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || !SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
                {
                    //Logger.Debug("IsLoggingInTaskCurrentlyBeingPerformed: TRUE");
                    SeekerApplication.OurCurrentLoginTask = SeekerApplication.OurCurrentLoginTask.ContinueWith(action, System.Threading.CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                    if (msg != null)
                    {
                        if (contextToUseForMessage == null)
                        {
                            Toast.MakeText(SeekerState.ActiveActivityRef, msg, ToastLength.Short).Show();
                        }
                        else
                        {
                            Toast.MakeText(contextToUseForMessage, msg, ToastLength.Short).Show();
                        }
                    }
                    return true;
                }
                else
                {
                    //Logger.Debug("IsLoggingInTaskCurrentlyBeingPerformed: FALSE");
                    return false;
                }
            }
        }

        public static bool ShowMessageAndCreateReconnectTask(Context c, bool silent, out Task connectTask)
        {
            if (c == null)
            {
                c = SeekerState.MainActivityRef;
            }
            if (Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread()) //tested..
            {
                if (!silent)
                {
                    Toast tst = Toast.MakeText(c, c.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                    tst.Show();
                }
            }
            else
            {
                if (!silent)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(
                    () =>
                    {
                        Toast tst = Toast.MakeText(c, c.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                        tst.Show();
                    });
                }
            }
            //if we are still not connected then creating the task will throw. 
            //also if the async part of the task fails we will get task.faulted.
            try
            {
                connectTask = SeekerApplication.ConnectAndPerformPostConnectTasks(PreferencesState.Username, PreferencesState.Password);
                return true;
            }
            catch
            {
                if (!silent)
                {
                    Toast tst2 = Toast.MakeText(c, c.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                    tst2.Show();
                }
            }
            connectTask = null;
            return false;
        }

        public static bool CurrentlyLoggedInButDisconnectedState()
        {
            return (PreferencesState.CurrentlyLoggedIn &&
                (SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnected) || SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnecting)));
        }

        public static void SetStatusApi(bool away)
        {
            if (IsNotLoggedIn())
            {
                return;
            }
            if (!SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || !SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                //dont log in just for this.
                //but if we later connect while still in the background, it may be best to set a flag.
                //do it when we log in... since we could not set it now...
                SeekerState.PendingStatusChangeToAwayOnline = away ? SeekerState.PendingStatusChange.AwayPending : SeekerState.PendingStatusChange.OnlinePending;
                return;
            }
            try
            {
                SeekerState.SoulseekClient.SetStatusAsync(away ? UserPresence.Away : UserPresence.Online).ContinueWith((Task t) =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        SeekerState.PendingStatusChangeToAwayOnline = SeekerState.PendingStatusChange.NothingPending;
                        SeekerState.OurCurrentStatusIsAway = away;
                        string statusString = away ? "away" : "online"; //not user facing
                        Logger.Debug($"We successfully changed our status to {statusString}");
                    }
                    else
                    {
                        Logger.Debug("SetStatusApi FAILED " + t.Exception?.Message);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Debug("SetStatusApi FAILED " + e.Message + e.StackTrace);
            }
        }

        private void UpdateForScreenSize()
        {
            if (!SeekerState.IsLowDpi()) return;
            try
            {
                TabLayout tabs = (TabLayout)FindViewById(Resource.Id.tabs);
                LinearLayout vg = (LinearLayout)tabs.GetChildAt(0);
                int tabsCount = vg.ChildCount;
                for (int j = 0; j < tabsCount; j++)
                {
                    ViewGroup vgTab = (ViewGroup)vg.GetChildAt(j);
                    int tabChildsCount = vgTab.ChildCount;
                    for (int i = 0; i < tabChildsCount; i++)
                    {
                        View tabViewChild = vgTab.GetChildAt(i);
                        if (tabViewChild is TextView)
                        {
                            ((TextView)tabViewChild).SetAllCaps(false);
                        }
                    }
                }
            }
            catch
            {
                //not worth throwing over..
            }
        }

        public void RecreateFragment(AndroidX.Fragment.App.Fragment f)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                SupportFragmentManager.BeginTransaction().Detach(f).CommitNowAllowingStateLoss();//hisbeginTransaction().detach(fragment).commitNow()
                SupportFragmentManager.BeginTransaction().Attach(f).CommitNowAllowingStateLoss();
            }
            else
            {
                //SupportFragmentManager
                SupportFragmentManager.BeginTransaction().Detach(f).Attach(f).CommitNow();
                //supportFragmentManager.beginTransaction().detach(fragment).attach(fragment).commitNow()
            }
        }



        public static string GetLastPathSegment(string uri)
        {
            return Android.Net.Uri.Parse(uri).LastPathSegment;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (NEW_WRITE_EXTERNAL == requestCode || NEW_WRITE_EXTERNAL_VIA_LEGACY == requestCode || NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
            {
                Action showDirectoryButton = new Action(() =>
                {
                    ToastUI(SeekerState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_error));
                    AddLoggedInLayout(StaticHacks.LoginFragment.View); //todo: nullref
                    if (!PreferencesState.CurrentlyLoggedIn)
                    {
                        MainActivity.BackToLogInLayout(StaticHacks.LoginFragment.View, (StaticHacks.LoginFragment as LoginFragment).LogInClick);
                    }
                    if (StaticHacks.LoginFragment.View == null)//this can happen...
                    {   //.View is a method so it can return null.  I tested it on MainActivity.OnPause and it was in fact null.
                        ToastUI(SeekerState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_choose_settings));
                        Logger.Firebase("StaticHacks.LoginFragment.View is null");
                        return;
                    }
                    Button bttn = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.mustSelectDirectory);
                    Button bttnLogout = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.buttonLogout);
                    if (bttn != null)
                    {
                        bttn.Visibility = ViewStates.Visible;
                        bttn.Click += MustSelectDirectoryClick;
                    }
                });

                if (NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
                {
                    //the resultCode will always be Cancelled for this since you have to back out of it.
                    //so instead we check Android.OS.Environment.IsExternalStorageManager
                    if (SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
                    {
                        //phase 2 - actually pick a file.
                        FallbackFileSelection(NEW_WRITE_EXTERNAL_VIA_LEGACY);
                        return;
                    }
                    else
                    {
                        if (OnUIthread())
                        {
                            showDirectoryButton();
                        }
                        else
                        {
                            RunOnUiThread(showDirectoryButton);
                        }
                        return;
                    }
                }


                if (resultCode == Result.Ok)
                {
                    if (NEW_WRITE_EXTERNAL == requestCode)
                    {
                        var x = data.Data;
                        SeekerState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
                        PreferencesState.SaveDataDirectoryUri = data.Data.ToString();
                        PreferencesState.SaveDataDirectoryUriIsFromTree = true;
                        this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    }
                    else if (NEW_WRITE_EXTERNAL_VIA_LEGACY == requestCode)
                    {
                        SeekerState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(data.Data.Path));
                        PreferencesState.SaveDataDirectoryUri = data.Data.ToString();
                        PreferencesState.SaveDataDirectoryUriIsFromTree = false;
                    }
                }
                else
                {

                    if (OnUIthread())
                    {
                        showDirectoryButton();
                    }
                    else
                    {
                        RunOnUiThread(showDirectoryButton);
                    }

                    //throw new Exception("Seeker requires access to a directory where it can store files.");
                }
            }
            else if (MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL == requestCode ||
                MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY == requestCode ||
                MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
            {

                Action reiterate = new Action(() =>
                {
                    ToastUI(SeekerState.MainActivityRef.GetString(Resource.String.seeker_needs_dl_dir_error));
                });

                Action hideButton = new Action(() =>
                {
                    Button bttn = StaticHacks.LoginFragment.View.FindViewById<Button>(Resource.Id.mustSelectDirectory);
                    bttn.Visibility = ViewStates.Gone;
                });

                if (MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
                {
                    //the resultCode will always be Cancelled for this since you have to back out of it.
                    //so instead we check Android.OS.Environment.IsExternalStorageManager
                    if (SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
                    {
                        //phase 2 - actually pick a file.
                        FallbackFileSelection(MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY);
                        return;
                    }
                    else
                    {
                        if (OnUIthread())
                        {
                            reiterate();
                        }
                        else
                        {
                            RunOnUiThread(reiterate);
                        }
                        return;
                    }
                }


                if (resultCode == Result.Ok)
                {
                    if (MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY == requestCode)
                    {
                        SeekerState.RootDocumentFile = DocumentFile.FromFile(new Java.IO.File(data.Data.Path));
                        PreferencesState.SaveDataDirectoryUri = data.Data.ToString();
                        PreferencesState.SaveDataDirectoryUriIsFromTree = false;
                    }
                    else if (MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL == requestCode)
                    {
                        SeekerState.RootDocumentFile = DocumentFile.FromTreeUri(this, data.Data);
                        PreferencesState.SaveDataDirectoryUri = data.Data.ToString();
                        PreferencesState.SaveDataDirectoryUriIsFromTree = true;
                        this.ContentResolver.TakePersistableUriPermission(data.Data, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                    }

                    //hide the button

                    if (OnUIthread())
                    {
                        hideButton();
                    }
                    else
                    {
                        RunOnUiThread(hideButton);
                    }
                }
                else
                {
                    if (OnUIthread())
                    {
                        reiterate();
                    }
                    else
                    {
                        RunOnUiThread(reiterate);
                    }

                    //throw new Exception("Seeker requires access to a directory where it can store files.");
                }
            }
            else if (SETTINGS_EXTERNAL == requestCode)
            {
                if (resultCode == Result.Ok)
                {
                    //get settings and set our things.
                }
                else if (resultCode == Result.Canceled)
                {
                    //do nothing...
                }
            }
            //else
            //{
            //    base.OnActivityResult(requestCode, resultCode, data);
            //}
        }

        private void MustSelectDirectoryClick(object sender, EventArgs e)
        {
            var storageManager = Android.OS.Storage.StorageManager.FromContext(this);
            var intent = storageManager.PrimaryStorageVolume.CreateOpenDocumentTreeIntent();
            intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPrefixUriPermission);
            Android.Net.Uri res = null; //var y = MediaStore.Audio.Media.ExternalContentUri.ToString();
            if (string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri))
            {
                //try
                //{
                //    //storage/emulated/0/music
                //    Java.IO.File f = new Java.IO.File(@"/storage/emulated/0/Music");///Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic);
                //    //res = f.ToURI();
                //    res = Android.Net.Uri.FromFile(f);//Parse(f.ToURI().ToString());
                //}
                //catch
                //{
                //    res = Android.Net.Uri.Parse(defaultMusicUri);//TryCreate("content://com.android.externalstorage.documents/tree/primary%3AMusic", UriKind.Absolute,out res);
                //}
                res = Android.Net.Uri.Parse(defaultMusicUri);
            }
            else
            {
                res = Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);
            }
            intent.PutExtra(DocumentsContract.ExtraInitialUri, res);
            try
            {
                this.StartActivityForResult(intent, MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL);
                //if(1==1)
                //{
                //    throw new Exception(Helpers.NoDocumentOpenTreeToHandle);
                //}

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(CommonHelpers.NoDocumentOpenTreeToHandle))
                {
                    FallbackFileSelectionEntry(true);
                }
                else
                {
                    throw ex;
                }
            }
        }

        private void FallbackFileSelectionEntry(bool mustSelectDirectoryButton)
        {
            bool hasManageAllFilesManisfestPermission = false;

#if IzzySoft
            hasManageAllFilesManisfestPermission = true;
#endif

            if (SeekerState.RequiresEitherOpenDocumentTreeOrManageAllFiles() && hasManageAllFilesManisfestPermission && !Android.OS.Environment.IsExternalStorageManager) //this is "step 1"
            {
                Intent allFilesPermission = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                Android.Net.Uri packageUri = Android.Net.Uri.FromParts("package", this.PackageName, null);
                allFilesPermission.SetData(packageUri);
                this.StartActivityForResult(allFilesPermission, mustSelectDirectoryButton ? MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen : NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen);
            }
            else if (SettingsActivity.DoWeHaveProperPermissionsForInternalFilePicker())
            {
                FallbackFileSelection(mustSelectDirectoryButton ? MUST_SELECT_A_DIRECTORY_WRITE_EXTERNAL_VIA_LEGACY : NEW_WRITE_EXTERNAL_VIA_LEGACY);
            }
            else
            {


                if (SeekerState.RequiresEitherOpenDocumentTreeOrManageAllFiles() && !hasManageAllFilesManisfestPermission)
                {
                    ShowSimpleAlertDialog(this, Resource.String.error_no_file_manager_dir_manage_storage, Resource.String.okay);
                }
                else
                {
                    Toast.MakeText(this, SeekerState.ActiveActivityRef.GetString(Resource.String.error_no_file_manager_dir), ToastLength.Long).Show();
                }


                //Note:
                //If your app targets Android 12 (API level 31) or higher, its toast is limited to two lines of text and shows the application icon next to the text.
                //Be aware that the line length of this text varies by screen size, so it's good to make the text as short as possible.
                //on Pixel 5 emulator this limit is around 78 characters.
                //^It must BOTH target Android 12 AND be running on Android 12^
            }
        }

        private void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            e.Handled = false; //make sure we still crash.. we just want to clean up..
            try
            {
                //save transfers state !!!
                TransfersFragment.SaveTransferItems(sharedPreferences);
            }
            catch
            {

            }
            try
            {
                //stop dl service..
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                Logger.Debug("Stop Service");
                this.StopService(downloadServiceIntent);
            }
            catch
            {

            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="e"></param>
        ///// <param name="c"></param>
        //public static void ShowAlert(Exception e, Context c)
        //{
        //    var b = new Android.App.AlertDialog.Builder(c);
        //    b.SetTitle("An Unhandled Exception Occured"); 
        //    b.SetMessage(e.Message + 
        //        System.Environment.NewLine + 
        //        System.Environment.NewLine + 
        //        e.StackTrace);
        //    b.Show();
        //}

        //public void LogWriter(string logMessage)
        //{
        //    LogWrite(logMessage);
        //}
        //public void LogWrite(string logMessage)
        //{
        //    var m_exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        //    try
        //    {
        //        using (System.IO.StreamWriter w = System.IO.File.AppendText(m_exePath + "\\" + "log.txt"))
        //        {
        //            w.WriteLine("Testing testing testing");
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        //public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        //{
        //    if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
        //    {
        //        return;
        //    }
        //    else
        //    {
        //        FinishAndRemoveTask();
        //    }
        //}

        //public static void SeekerState_DownloadAdded(object sender, DownloadAddedEventArgs e)
        //{
        //    Logger.Debug("SeekerState_DownloadAdded");
        //    TransferItem transferItem = new TransferItem();
        //    transferItem.Filename = Helpers.GetFileNameFromFile(e.dlInfo.fullFilename);
        //    transferItem.FolderName = Helpers.GetFolderNameFromFile(e.dlInfo.fullFilename);
        //    transferItem.Username = e.dlInfo.username;
        //    transferItem.FullFilename = e.dlInfo.fullFilename;
        //    transferItem.Size = e.dlInfo.Size;
        //    transferItem.QueueLength = e.dlInfo.QueueLength;
        //    e.dlInfo.TransferItemReference = transferItem;

        //    TransfersFragment.SetupCancellationToken(transferItem, e.dlInfo.CancellationTokenSource, out _);
        //    //transferItem.CancellationTokenSource = e.dlInfo.CancellationTokenSource;
        //    //if (!CancellationTokens.TryAdd(ProduceCancellationTokenKey(transferItem), e.dlInfo.CancellationTokenSource))
        //    //{
        //    //    //likely old already exists so just replace the old one
        //    //    CancellationTokens[ProduceCancellationTokenKey(transferItem)] = e.dlInfo.CancellationTokenSource;
        //    //}

        //    //once task completes, write to disk
        //    Action<Task> continuationActionSaveFile = DownloadContinuationActionUI(e);
        //    e.dlInfo.downloadTask.ContinueWith(continuationActionSaveFile);

        //    TransfersFragment.TransferItemManagerDL.Add(transferItem);
        //    MainActivity.DownloadAddedUINotify?.Invoke(null, e);
        //}


        public static void ToastUI(int msgCode)
        {
            Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(msgCode), ToastLength.Long).Show();
        }

        public static void ToastUI_short(int msgCode)
        {
            Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(msgCode), ToastLength.Short).Show();
        }

        private static bool HasNonASCIIChars(string str)
        {
            return (System.Text.Encoding.UTF8.GetByteCount(str) != str.Length);
        }


        /// <summary>
        /// This is to solve the problem of, are all the toasts part of the same session?  
        /// For example if you download a locked folder of 20 files, you will get immediately 20 toasts
        /// So our logic is, if you just did a message, wait a full second before showing anything more.
        /// </summary>
        /// <param name="msgToToast"></param>
        /// <param name="caseOrCode"></param>
        /// <param name="usernameIfApplicable"></param>
        /// <param name="seconds">might be useful to increase this if something has a lot of variance even if requested at the same time, like a timeout.</param>
        public static void ToastUIWithDebouncer(string msgToToast, string caseOrCode, string usernameIfApplicable = "", int seconds = 1)
        {
            long curTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            //if it does not exist then updatedTime will be curTime.  If it does exist but is older than a second then updated time will also be curTime.  In those two cases, show the toast.
            Logger.Debug("curtime " + curTime);
            bool stale = false;
            long updatedTime = ToastUIDebouncer.AddOrUpdate(caseOrCode + usernameIfApplicable, curTime, (key, oldValue) =>
            {

                Logger.Debug("key exists: " + (curTime - oldValue).ToString());

                stale = (curTime - oldValue) < (seconds * 1000);
                if (stale)
                {
                    Logger.Debug("stale");
                }

                return stale ? oldValue : curTime;
            });
            Logger.Debug("updatedTime " + updatedTime);
            if (!stale)
            {
                ToastUI(msgToToast);
            }
        }
        private static System.Collections.Concurrent.ConcurrentDictionary<string, long> ToastUIDebouncer = new System.Collections.Concurrent.ConcurrentDictionary<string, long>();

        public static void ToastUI(string msg)
        {
            if (OnUIthread())
            {
                Toast.MakeText(SeekerState.ActiveActivityRef, msg, ToastLength.Long).Show();
            }
            else
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, msg, ToastLength.Long).Show();
                });
            }
        }

        public static void ToastUI_short(string msg)
        {
            if (OnUIthread())
            {
                Toast.MakeText(SeekerState.ActiveActivityRef, msg, ToastLength.Short).Show();
            }
            else
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                    {
                        Toast.MakeText(SeekerState.ActiveActivityRef, msg, ToastLength.Short).Show();
                    });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool OnUIthread()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                return Looper.MainLooper.IsCurrentThread;
            }
            else
            {
                return Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootView"></param>
        /// <param name="force">the log in layout is full of hacks. that being said force 
        ///   makes it so that if we are currently logged in to still add the logged in fragment
        ///   if not there, which makes sense. </param>
        public static void AddLoggedInLayout(View rootView = null, bool force = false)
        {
            View bttn = StaticHacks.RootView?.FindViewById<Button>(Resource.Id.buttonLogout);
            View bttnTryTwo = rootView?.FindViewById<Button>(Resource.Id.buttonLogout);
            bool bttnIsAttached = false;
            bool bttnTwoIsAttached = false;
            if (bttn != null && bttn.IsAttachedToWindow)
            {
                bttnIsAttached = true;
            }
            if (bttnTryTwo != null && bttnTryTwo.IsAttachedToWindow)
            {
                bttnTwoIsAttached = true;
            }

            if (!bttnIsAttached && !bttnTwoIsAttached && (!PreferencesState.CurrentlyLoggedIn || force))
            {
                //THIS MEANS THAT WE STILL HAVE THE LOGINFRAGMENT NOT THE LOGGEDIN FRAGMENT
                //ViewGroup relLayout = SeekerState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, rootView as ViewGroup, false) as ViewGroup;
                //relLayout.LayoutParameters = new ViewGroup.LayoutParams(rootView.LayoutParameters);
                var action1 = new Action(() =>
                {
                    (rootView as ViewGroup).AddView(SeekerState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, rootView as ViewGroup, false));
                });
                if (OnUIthread())
                {
                    action1();
                }
                else
                {
                    SeekerState.MainActivityRef.RunOnUiThread(action1);
                }
            }
        }

        public static void UpdateUIForLoggedIn(View rootView = null, EventHandler BttnClick = null, View cWelcome = null, View cbttn = null, ViewGroup cLoading = null, EventHandler SettingClick = null)
        {
            var action = new Action(() =>
            {
                //this is the case where it already has the loggedin fragment loaded.
                Button bttn = null;
                TextView welcome = null;
                ViewGroup loggingInLayout = null;
                ViewGroup logInLayout = null;

                Button settings = null;
                try
                {
                    if (StaticHacks.RootView != null && StaticHacks.RootView.IsAttachedToWindow)
                    {
                        bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);

                        logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);

                        settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                    else
                    {
                        bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);

                        logInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);

                        settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                }
                catch
                {

                }
                if (welcome != null)
                {
                    //meanwhile: rootView.FindViewById<TextView>(Resource.Id.userNameView).  so I dont think that the welcome here is the right one.. I dont think it exists.
                    //try checking properties such as isAttachedToWindow, getWindowVisiblity etx...
                    welcome.Visibility = ViewStates.Visible;

                    bool isShown = welcome.IsShown;
                    bool isAttachedToWindow = welcome.IsAttachedToWindow;
                    bool isActivated = welcome.Activated;
                    ViewStates viewState = welcome.WindowVisibility;


                    //welcome = rootView.FindViewById(Resource.Id.userNameView) as Android.Widget.TextView;
                    //if(welcome!=null)
                    //{
                    //isShown = welcome.IsShown;
                    //isAttachedToWindow = welcome.IsAttachedToWindow;
                    //isActivated = welcome.Activated;
                    //viewState = welcome.WindowVisibility;
                    //}


                    bttn.Visibility = ViewStates.Visible;
                    settings.Visibility = ViewStates.Visible;


                    settings.Click -= SettingClick;
                    settings.Click += SettingClick;
                   AndroidX.Core.View.ViewCompat.SetTranslationZ(bttn, 90);
                    bttn.Click -= BttnClick;
                    bttn.Click += BttnClick;
                    loggingInLayout.Visibility = ViewStates.Gone;
                    welcome.Text = String.Format(SeekerApplication.GetString(Resource.String.welcome), PreferencesState.Username);
                }
                else if (cWelcome != null)
                {
                    cWelcome.Visibility = ViewStates.Visible;
                    cbttn.Visibility = ViewStates.Visible;
                   AndroidX.Core.View.ViewCompat.SetTranslationZ(cbttn, 90);
                    cLoading.Visibility = ViewStates.Gone;
                }
                else
                {
                    StaticHacks.UpdateUI = true;//if we arent ready rn then do it when we are..
                }
                if (logInLayout != null)
                {
                    logInLayout.Visibility = ViewStates.Gone;
                   AndroidX.Core.View.ViewCompat.SetTranslationZ(logInLayout.FindViewById<Button>(Resource.Id.buttonLogin), 0);
                }

            });
            if (OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }

        public static bool IsNotLoggedIn()
        {
            return (!PreferencesState.CurrentlyLoggedIn) || PreferencesState.Username == null || PreferencesState.Password == null || PreferencesState.Username == string.Empty;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootView"></param>
        public static void BackToLogInLayout(View rootView, EventHandler LogInClick, bool clearUserPass = true)
        {
            var action = new Action(() =>
            {
                //this is the case where it already has the loggedin fragment loaded.
                Button bttn = null;
                TextView welcome = null;
                TextView loading = null;
                //EditText editText = null;
                //EditText editText2 = null;
                //TextView textView = null;
                ViewGroup loggingInLayout = null;
                ViewGroup logInLayout = null;
                Button buttonLogin = null;
                //View noAccountHelp = null;
                Button settings = null;
                Logger.Debug("BackToLogInLayout");
                try
                {
                    if (StaticHacks.RootView != null && StaticHacks.RootView.IsAttachedToWindow)
                    {
                        Logger.Debug("StaticHacks.RootView != null");
                        bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);

                        //this is the case we have a bad SAVED user pass....
                        try
                        {
                            logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                            //editText2 = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.etPassword);
                            //textView = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.textView);
                            buttonLogin = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogin);
                            //noAccountHelp = StaticHacks.RootView.FindViewById(Resource.Id.noAccount);
                            if (logInLayout == null)
                            {
                                ViewGroup relLayout = SeekerState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.login, StaticHacks.RootView as ViewGroup, false) as ViewGroup;
                                relLayout.LayoutParameters = new ViewGroup.LayoutParams(StaticHacks.RootView.LayoutParameters);
                                //var action1 = new Action(() => {
                                (StaticHacks.RootView as ViewGroup).AddView(SeekerState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.login, StaticHacks.RootView as ViewGroup, false));
                                //});
                            }
                            //editText = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.etUsername);
                            //editText2 = StaticHacks.RootView.FindViewById<EditText>(Resource.Id.etPassword);
                            //textView = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.textView);
                            settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                            buttonLogin = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogin);
                            //noAccountHelp = StaticHacks.RootView.FindViewById(Resource.Id.noAccount);
                            logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                            buttonLogin.Click -= LogInClick;
                            (StaticHacks.LoginFragment as Seeker.LoginFragment).rootView = StaticHacks.RootView;
                            (StaticHacks.LoginFragment as Seeker.LoginFragment).SetUpLogInLayout();
                            //buttonLogin.Click += LogInClick;
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug("BackToLogInLayout" + ex.Message);
                        }

                    }
                    else
                    {
                        Logger.Debug("StaticHacks.RootView == null");
                        bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                        logInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                        buttonLogin = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
                        settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                    }
                }
                catch
                {

                }
                Logger.Debug("logInLayout is here? " + (logInLayout != null).ToString());
                if (logInLayout != null)
                {
                    logInLayout.Visibility = ViewStates.Visible;
                    if (!clearUserPass && !string.IsNullOrEmpty(PreferencesState.Username))
                    {
                        logInLayout.FindViewById<EditText>(Resource.Id.etUsername).Text = PreferencesState.Username;
                        logInLayout.FindViewById<EditText>(Resource.Id.etPassword).Text = PreferencesState.Password;
                    }
                   AndroidX.Core.View.ViewCompat.SetTranslationZ(buttonLogin, 90);

                    if (loading == null)
                    {
                        MainActivity.AddLoggedInLayout(rootView);
                        if (rootView != null)
                        {
                            bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                            welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                            loggingInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                            settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                        }
                        if (rootView == null && loading == null && StaticHacks.RootView != null)
                        {
                            bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                            welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                            loggingInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                            settings = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                        }
                    }
                    loggingInLayout.Visibility = ViewStates.Gone; //can get nullref here!!! (at least before the .AddLoggedInLayout code..
                    welcome.Visibility = ViewStates.Gone;
                    settings.Visibility = ViewStates.Gone;
                    bttn.Visibility = ViewStates.Gone;
                   AndroidX.Core.View.ViewCompat.SetTranslationZ(bttn, 0);


                }

            });
            if (OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootView"></param>
        public static void UpdateUIForLoggingInLoading(View rootView = null)
        {
            Logger.Debug("UpdateUIForLoggingInLoading");
            var action = new Action(() =>
            {
                //this is the case where it already has the loggedin fragment loaded.
                Button logoutButton = null;
                TextView welcome = null;
                ViewGroup loggingInView = null;
                ViewGroup logInLayout = null;
                Button settingsButton = null;
                try
                {
                    if (StaticHacks.RootView != null && rootView == null)
                    {
                        logoutButton = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        settingsButton = StaticHacks.RootView.FindViewById<Button>(Resource.Id.settingsButton);
                        welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInView = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                        logInLayout = StaticHacks.RootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);

                    }
                    else
                    {
                        logoutButton = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                        settingsButton = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                        welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                        loggingInView = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                        logInLayout = rootView.FindViewById<ViewGroup>(Resource.Id.logInLayout);
                    }
                }
                catch
                {

                }
                if (logInLayout != null)
                {
                    logInLayout.Visibility = ViewStates.Gone; //todo change back.. //basically when we AddChild we add it UNDER the logInLayout.. so making it gone makes everything gone... we need a root layout for it...
                   AndroidX.Core.View.ViewCompat.SetTranslationZ(logInLayout.FindViewById<Button>(Resource.Id.buttonLogin), 0);
                    loggingInView.Visibility = ViewStates.Visible;
                    welcome.Visibility = ViewStates.Gone; //WE GET NULLREF HERE. FORCE connection already established exception and maybe see what is going on here...
                    logoutButton.Visibility = ViewStates.Gone;
                    settingsButton.Visibility = ViewStates.Gone;
                   AndroidX.Core.View.ViewCompat.SetTranslationZ(logoutButton, 0);
                }

            });
            if (OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }

        protected override void OnPause()
        {
            //Logger.Debug(".view is null " + (StaticHacks.LoginFragment.View==null).ToString()); it is null
            base.OnPause();

            TransfersFragment.SaveTransferItems(sharedPreferences);
            lock (SeekerState.SharedPrefLock)
            {
                var editor = sharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_CurrentlyLoggedIn, PreferencesState.CurrentlyLoggedIn);
                editor.PutString(KeyConsts.M_Username, PreferencesState.Username);
                editor.PutString(KeyConsts.M_Password, PreferencesState.Password);
                editor.PutString(KeyConsts.M_SaveDataDirectoryUri, PreferencesState.SaveDataDirectoryUri);
                editor.PutBoolean(KeyConsts.M_SaveDataDirectoryUriIsFromTree, PreferencesState.SaveDataDirectoryUriIsFromTree);
                editor.PutInt(KeyConsts.M_NumberSearchResults, PreferencesState.NumberSearchResults);
                editor.PutInt(KeyConsts.M_DayNightMode, PreferencesState.DayNightMode);
                editor.PutBoolean(KeyConsts.M_AutoClearComplete, PreferencesState.AutoClearCompleteDownloads);
                editor.PutBoolean(KeyConsts.M_AutoClearCompleteUploads, PreferencesState.AutoClearCompleteUploads);
                editor.PutBoolean(KeyConsts.M_RememberSearchHistory, PreferencesState.RememberSearchHistory);
                editor.PutBoolean(KeyConsts.M_RememberUserHistory, PreferencesState.ShowRecentUsers);
                editor.PutBoolean(KeyConsts.M_TransfersShowSizes, PreferencesState.TransferViewShowSizes);
                editor.PutBoolean(KeyConsts.M_TransfersShowSpeed, PreferencesState.TransferViewShowSpeed);
                editor.PutBoolean(KeyConsts.M_OnlyFreeUploadSlots, PreferencesState.FreeUploadSlotsOnly);
                editor.PutBoolean(KeyConsts.M_HideLockedSearch, PreferencesState.HideLockedResultsInSearch);
                editor.PutBoolean(KeyConsts.M_HideLockedBrowse, PreferencesState.HideLockedResultsInBrowse);
                editor.PutBoolean(KeyConsts.M_FilterSticky, SearchFragment.FilterSticky);
                editor.PutString(KeyConsts.M_FilterStickyString, SearchTabHelper.FilterString);
                editor.PutBoolean(KeyConsts.M_MemoryBackedDownload, PreferencesState.MemoryBackedDownload);
                editor.PutInt(KeyConsts.M_SearchResultStyle, (int)(SearchFragment.SearchResultStyle));
                editor.PutBoolean(KeyConsts.M_DisableToastNotifications, PreferencesState.DisableDownloadToastNotification);
                editor.PutInt(KeyConsts.M_UploadSpeed, PreferencesState.UploadSpeed);
                //editor.PutString(KeyConsts.M_UploadDirectoryUri, SeekerState.UploadDataDirectoryUri);
                editor.PutBoolean(KeyConsts.M_SharingOn, PreferencesState.SharingOn);
                editor.PutBoolean(KeyConsts.M_AllowPrivateRooomInvitations, PreferencesState.AllowPrivateRoomInvitations);

                if (SeekerState.UserList != null)
                {
                    editor.PutString(KeyConsts.M_UserList, SerializationHelper.SaveUserListToString(SeekerState.UserList));
                }

                //editor.Apply();
                editor.Commit();
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean(KeyConsts.M_CurrentlyLoggedIn, PreferencesState.CurrentlyLoggedIn);
            outState.PutString(KeyConsts.M_Username, PreferencesState.Username);
            outState.PutString(KeyConsts.M_Password, PreferencesState.Password);
            outState.PutBoolean(KeyConsts.M_SaveDataDirectoryUriIsFromTree, PreferencesState.SaveDataDirectoryUriIsFromTree);
            outState.PutString(KeyConsts.M_SaveDataDirectoryUri, PreferencesState.SaveDataDirectoryUri);
            outState.PutInt(KeyConsts.M_NumberSearchResults, PreferencesState.NumberSearchResults);
            outState.PutInt(KeyConsts.M_DayNightMode, PreferencesState.DayNightMode);
            outState.PutBoolean(KeyConsts.M_AutoClearComplete, PreferencesState.AutoClearCompleteDownloads);
            outState.PutBoolean(KeyConsts.M_AutoClearCompleteUploads, PreferencesState.AutoClearCompleteUploads);
            outState.PutBoolean(KeyConsts.M_RememberSearchHistory, PreferencesState.RememberSearchHistory);
            outState.PutBoolean(KeyConsts.M_RememberUserHistory, PreferencesState.ShowRecentUsers);
            outState.PutBoolean(KeyConsts.M_MemoryBackedDownload, PreferencesState.MemoryBackedDownload);
            outState.PutBoolean(KeyConsts.M_FilterSticky, SearchFragment.FilterSticky);
            outState.PutBoolean(KeyConsts.M_OnlyFreeUploadSlots, PreferencesState.FreeUploadSlotsOnly);
            outState.PutBoolean(KeyConsts.M_HideLockedSearch, PreferencesState.HideLockedResultsInSearch);
            outState.PutBoolean(KeyConsts.M_HideLockedBrowse, PreferencesState.HideLockedResultsInBrowse);
            outState.PutBoolean(KeyConsts.M_DisableToastNotifications, PreferencesState.DisableDownloadToastNotification);
            outState.PutInt(KeyConsts.M_SearchResultStyle, (int)(SearchFragment.SearchResultStyle));
            outState.PutString(KeyConsts.M_FilterStickyString, SearchTabHelper.FilterString);
            outState.PutInt(KeyConsts.M_UploadSpeed, PreferencesState.UploadSpeed);
            //outState.PutString(KeyConsts.M_UploadDirectoryUri, SeekerState.UploadDataDirectoryUri);
            outState.PutBoolean(KeyConsts.M_AllowPrivateRooomInvitations, PreferencesState.AllowPrivateRoomInvitations);
            outState.PutBoolean(KeyConsts.M_SharingOn, PreferencesState.SharingOn);
            if (SeekerState.UserList != null)
            {
                outState.PutString(KeyConsts.M_UserList, SerializationHelper.SaveUserListToString(SeekerState.UserList));
            }

        }

        private void Tabs_TabSelected(object sender, TabLayout.TabSelectedEventArgs e)
        {
            System.Console.WriteLine(e.Tab.Position);
            if (e.Tab.Position != 1) //i.e. if we are not the search tab
            {
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)this.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow((sender as View).WindowToken, 0);
                }
                catch
                {
                    //not worth throwing over
                }
            }
        }
        public static int goToSearchTab = int.MaxValue;
        private void Pager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            //if we are changing modes and the transfers action mode is not null (i.e. is active)
            //then we need to get out of it.
            if (TransfersFragment.TransfersActionMode != null)
            {
                TransfersFragment.TransfersActionMode.Finish();
            }
            //in addition each fragment is responsible for expanding their menu...
            switch (e.Position)
            {
                case 0:
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);

                    this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                    this.SupportActionBar.SetDisplayShowTitleEnabled(true);
                    this.SupportActionBar.Title = this.GetString(Resource.String.home_tab);
                    this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.account_menu);
                    break;
                case 1:
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);

                    //string initText = string.Empty;
                    //if (this.SupportActionBar?.CustomView != null) //it is null on device rotation...
                    //{
                    //    AutoCompleteTextView v = this.SupportActionBar.CustomView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                    //    if(v!=null)
                    //    {
                    //        initText = v.Text;
                    //    }
                    //}
                    //the SupportActionBar.CustomView will still be here if we leave tabs and come back.. so just set the text on it again.

                    this.SupportActionBar.SetDisplayShowCustomEnabled(true);
                    this.SupportActionBar.SetDisplayShowTitleEnabled(false);
                    this.SupportActionBar.SetCustomView(Resource.Layout.custom_menu_layout);
                    SearchFragment.ConfigureSupportCustomView(this.SupportActionBar.CustomView/*, this*/);
                    this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.account_menu);
                    if (goToSearchTab != int.MaxValue)
                    {
                        if (SearchFragment.Instance?.Activity == null || !(SearchFragment.Instance.Activity.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started))) //this happens if we come from settings activity. Main Activity has NOT been started. SearchFragment has the .Actvity ref of an OLD activity.  so we are not ready yet. 
                        {
                            //let onresume go to the search tab..
                            Logger.Debug("Delay Go To Wishlist Search Fragment for OnResume");
                        }
                        else
                        {
                            //can we do this now??? or should we pass this down to the search fragment for when it gets created...  maybe we should put this in a like "OnResume"
                            Logger.Debug("Do Go To Wishlist in page selected");
                            SearchFragment.Instance.GoToTab(goToSearchTab, false, true);
                            goToSearchTab = int.MaxValue;
                        }
                    }
                    break;
                case 2:
                    this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                    this.SupportActionBar.SetDisplayShowTitleEnabled(true);

                    SetTransferSupportActionBarState();

                    this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.browse_menu_empty);  //todo remove?
                    break;
                case 3:
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);

                    this.SupportActionBar.SetDisplayShowCustomEnabled(false);
                    this.SupportActionBar.SetDisplayShowTitleEnabled(true);
                    if (string.IsNullOrEmpty(BrowseFragment.CurrentUsername))
                    {
                        this.SupportActionBar.Title = this.GetString(Resource.String.browse_tab);
                    }
                    else
                    {
                        this.SupportActionBar.Title = this.GetString(Resource.String.browse_tab) + ": " + BrowseFragment.CurrentUsername;
                    }
                    this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.transfers_menu);
                    break;
            }
        }

        public void SetTransferSupportActionBarState()
        {
            if (TransfersFragment.InUploadsMode)
            {
                if (TransfersFragment.CurrentlySelectedUploadFolder == null)
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.Uploads);
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);
                }
                else
                {
                    this.SupportActionBar.Title = TransfersFragment.CurrentlySelectedUploadFolder.FolderName;
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    this.SupportActionBar.SetHomeButtonEnabled(true);
                }
            }
            else
            {
                if (TransfersFragment.CurrentlySelectedDLFolder == null)
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.Downloads);
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);
                }
                else
                {
                    this.SupportActionBar.Title = TransfersFragment.CurrentlySelectedDLFolder.FolderName;
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    this.SupportActionBar.SetHomeButtonEnabled(true);
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            //MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            switch (requestCode)
            {
                case POST_NOTIFICATION_PERMISSION:
                    break;
                default:
                    if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    {
                        return;
                    }
                    else
                    {
                        FinishAndRemoveTask(); //TODO - why?? this was added in initial commit. kills process if permission not granted?
                    }
                    break;
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_home:
                    pager.CurrentItem = 0;
                    break;
                case Resource.Id.navigation_search:
                    pager.CurrentItem = 1;
                    break;
                case Resource.Id.navigation_transfers:
                    pager.CurrentItem = 2;
                    break;
                case Resource.Id.navigation_browse:
                    pager.CurrentItem = 3;
                    break;
            }
            return true;
        }

    }

}
