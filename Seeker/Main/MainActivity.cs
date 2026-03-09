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
using ActivityFlags = Android.Content.ActivityFlags;

//using System.IO;
//readme:
//dotnet add package Soulseek --version 1.0.0-rc3.1
//xamarin
//Had to rewrite this one from .csproj
//<Import Project="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild
//\Xamarin\Android\Xamarin.Android.CSharp.targets" />
namespace Seeker
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask, MainLauncher = true, Exported = true/*, WindowSoftInputMode = SoftInput.AdjustNothing*/)]
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



        private ISharedPreferences sharedPreferences;
        protected override void OnCreate(Bundle savedInstanceState)
        {
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
            base.OnCreate(savedInstanceState);
            //System.Threading.Thread.CurrentThread.Name = "Main Activity Thread";
            Xamarin.Essentials.Platform.Init(this, savedInstanceState); //this is what you are supposed to do.
            SetContentView(Resource.Layout.activity_main);

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
                SeekerState.MainActivityRef = this; //set these early. they are needed
                SeekerState.ActiveActivityRef = this;


                if (Intent.GetIntExtra(DownloadForegroundService.FromTransferString, -1) == 2)
                {
                    pager.SetCurrentItem(2, false);
                }
                else if (Intent.GetBooleanExtra(FolderAlertExtra, false))
                {
                    pager.SetCurrentItem(2, false);
                }
                else if (Intent.GetBooleanExtra(GoToBrowseExtra, false))
                {
                    pager.SetCurrentItem(3, false);
                }
                else if (Intent.GetBooleanExtra(GoToSearchExtra, false))
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
                    HandleWishlistIntent();
                }
                else if ((Intent.GetBooleanExtra(GoToUploadsForegroundExtra, false) || Intent.GetBooleanExtra(GoToUploadsExtra, false)) && !alreadyHandled) //else every rotation will change Downloads to Uploads.
                {
                    HandleFromNotificationUploadIntent();
                }
                else if (Intent.GetBooleanExtra(GoToBrowseSelfExtra, false))
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

            //TODO2026 - need to think about this
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

            SeekerState.SharedPreferences = sharedPreferences;
            SeekerState.MainActivityRef = this;
            SeekerState.ActiveActivityRef = this;

            UpdateForScreenSize();

            // Document files are initialized once per process in SeekerApplication.OnCreate.
            // Here we only handle the things that require an Activity context.
            if (SeekerState.UseLegacyStorage())
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Denied)
                {
                    ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage }, WRITE_EXTERNAL);
                }
            }
            else if (SeekerState.RootDocumentFile == null)
            {
                // SeekerApplication.InitializeDocumentFiles could not write the download directory —
                // permission was revoked. Ask the user to re-select one.
                Android.Net.Uri res = string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri)
                    ? Android.Net.Uri.Parse(SeekerState.DefaultMusicUri)
                    : Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);
                var b = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
                b.SetTitle(this.GetString(Resource.String.seeker_needs_dl_dir));
                b.SetMessage(this.GetString(Resource.String.seeker_needs_dl_dir_content));
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
                        if (ex.Message.Contains(SimpleHelpers.NoDocumentOpenTreeToHandle))
                        {
                            FallbackFileSelectionEntry(false);
                        }
                        else
                        {
                            throw;
                        }
                    }
                });
                b.SetPositiveButton(Resource.String.okay, eventHandler);
                b.SetCancelable(false);
                b.Show();
            }
        }


        private void HandleWishlistIntent()
        {
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
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.wishlist_tab_error), ToastLength.Long);
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
                Logger.InfoFirebase("from wishlist clicked - different page");
                //when we move to the page, lets move to our tab, if its not the current one..
                goToSearchTab = tabID; //we read this when we move tab...
                pager.SetCurrentItem(1, false);
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
                HandleWishlistIntent();
            }
            else if ((Intent.GetBooleanExtra(GoToUploadsForegroundExtra, false) || Intent.GetBooleanExtra(GoToUploadsExtra, false))) //else every rotation will change Downloads to Uploads.
            {
                HandleFromNotificationUploadIntent();
            }
            else if (Intent.GetBooleanExtra(GoToBrowseSelfExtra, false))
            {
                Logger.InfoFirebase("from browse self");
                pager.SetCurrentItem(3, false);
            }
            else if (Intent.GetBooleanExtra(GoToBrowseExtra, false))
            {
                pager.SetCurrentItem(3, false);
            }
            else if (Intent.GetBooleanExtra(GoToSearchExtra, false))
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
            else if (Intent.GetBooleanExtra(FolderAlertExtra, false))
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
                    var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
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

        public const string UPLOADS_CHANNEL_ID = "upload channel ID";
        public const string UPLOADS_CHANNEL_NAME = "Upload Notifications";

        // Intent extra keys — all intent extras targeting MainActivity are defined here.
        public const string GoToBrowseExtra = "GoToBrowse";
        public const string GoToSearchExtra = "GoToSearch";
        public const string GoToUploadsExtra = "GoToUploads";
        public const string GoToUploadsForegroundExtra = "GoToUploadsForeground";
        public const string GoToBrowseSelfExtra = "GoToBrowseSelf";
        public const string FolderAlertExtra = "FolderAlert";
        public const string FolderAlertUsernameExtra = "FolderAlertUsername";
        public const string FolderAlertFoldernameExtra = "FolderAlertFoldername";

        /// <summary>
        ///     Creates and returns an <see cref="IEnumerable{T}"/> of <see cref="Soulseek.Directory"/> in response to a remote request.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="endpoint">The IP endpoint of the requesting user.</param>
        /// <returns>A Task resolving an IEnumerable of Soulseek.Directory.</returns>


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
                    if (TransfersViewState.Instance.GetCurrentlySelectedFolder() != null)
                    {
                        if (TransfersViewState.Instance.InUploadsMode)
                        {
                            TransfersViewState.Instance.CurrentlySelectedUploadFolder = null;
                        }
                        else
                        {
                            TransfersViewState.Instance.CurrentlySelectedDLFolder = null;
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

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (NEW_WRITE_EXTERNAL == requestCode || NEW_WRITE_EXTERNAL_VIA_LEGACY == requestCode || NEW_WRITE_EXTERNAL_VIA_LEGACY_Settings_Screen == requestCode)
            {
                Action showDirectoryButton = new Action(() =>
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.seeker_needs_dl_dir_error), ToastLength.Long);
                    var loginFrag = SeekerState.LoginFragmentRef;
                    if (loginFrag == null || loginFrag.View == null)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.seeker_needs_dl_dir_choose_settings), ToastLength.Long);
                        Logger.Firebase("LoginFragmentRef or its View is null");
                        return;
                    }
                    if (!PreferencesState.CurrentlyLoggedIn)
                    {
                        loginFrag.ShowLoginForm(prefill: true);
                    }
                    loginFrag.ShowMustSelectDirectoryButton(MustSelectDirectoryClick);
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
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.seeker_needs_dl_dir_error), ToastLength.Long);
                });

                Action hideButton = new Action(() =>
                {
                    SeekerState.LoginFragmentRef?.HideMustSelectDirectoryButton();
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
                //    res = Android.Net.Uri.Parse(SeekerState.DefaultMusicUri);//TryCreate("content://com.android.externalstorage.documents/tree/primary%3AMusic", UriKind.Absolute,out res);
                //}
                res = Android.Net.Uri.Parse(SeekerState.DefaultMusicUri);
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
                if (ex.Message.Contains(SimpleHelpers.NoDocumentOpenTreeToHandle))
                {
                    FallbackFileSelectionEntry(true);
                }
                else
                {
                    throw;
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
                    UiHelpers.ShowSimpleAlertDialog(this, Resource.String.error_no_file_manager_dir_manage_storage, Resource.String.okay);
                }
                else
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_no_file_manager_dir), ToastLength.Long);
                }


                //Note:
                //If your app targets Android 12 (API level 31) or higher, its toast is limited to two lines of text and shows the application icon next to the text.
                //Be aware that the line length of this text varies by screen size, so it's good to make the text as short as possible.
                //on Pixel 5 emulator this limit is around 78 characters.
                //^It must BOTH target Android 12 AND be running on Android 12^
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



        protected override void OnPause()
        {
            base.OnPause();

            string userListSerialized = CommonState.UserList != null
                ? SerializationHelper.SaveUserListToString(CommonState.UserList)
                : null;
            PreferencesManager.SaveOnPauseState(userListSerialized);
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
            outState.PutBoolean(KeyConsts.M_FilterSticky, PreferencesState.FilterSticky);
            outState.PutBoolean(KeyConsts.M_OnlyFreeUploadSlots, PreferencesState.FreeUploadSlotsOnly);
            outState.PutBoolean(KeyConsts.M_HideLockedSearch, PreferencesState.HideLockedResultsInSearch);
            outState.PutBoolean(KeyConsts.M_HideLockedBrowse, PreferencesState.HideLockedResultsInBrowse);
            outState.PutBoolean(KeyConsts.M_DisableToastNotifications, PreferencesState.DisableDownloadToastNotification);
            outState.PutInt(KeyConsts.M_SearchResultStyle, (int)(SearchFragment.SearchResultStyle));
            outState.PutString(KeyConsts.M_FilterStickyString, SearchTabHelper.TextFilter.FilterString);
            outState.PutInt(KeyConsts.M_UploadSpeed, PreferencesState.UploadSpeed);
            //outState.PutString(KeyConsts.M_UploadDirectoryUri, SeekerState.UploadDataDirectoryUri);
            outState.PutBoolean(KeyConsts.M_AllowPrivateRooomInvitations, PreferencesState.AllowPrivateRoomInvitations);
            outState.PutBoolean(KeyConsts.M_SharingOn, PreferencesState.SharingOn);
            if (CommonState.UserList != null)
            {
                outState.PutString(KeyConsts.M_UserList, SerializationHelper.SaveUserListToString(CommonState.UserList));
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
            if (BrowseFragment.BrowseActionMode != null)
            {
                BrowseFragment.BrowseActionMode.Finish();
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
                    this.SupportActionBar.SubtitleFormatted = null;
                    this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.account_menu);
                    break;
                case 1:
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);

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
                    BrowseFragment.SetActionBarTitle();
                    this.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).InflateMenu(Resource.Menu.transfers_menu);
                    break;
            }
        }

        public void SetTransferSupportActionBarState()
        {
            if (TransfersViewState.Instance.InUploadsMode)
            {
                if (TransfersViewState.Instance.CurrentlySelectedUploadFolder == null)
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.Uploads);
                    this.SupportActionBar.SubtitleFormatted = null;
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);
                }
                else
                {
                    this.SupportActionBar.Title = TransfersViewState.Instance.CurrentlySelectedUploadFolder.FolderName;
                    this.SupportActionBar.SubtitleFormatted = null;
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    this.SupportActionBar.SetHomeButtonEnabled(true);
                }
            }
            else
            {
                if (TransfersViewState.Instance.CurrentlySelectedDLFolder == null)
                {
                    this.SupportActionBar.Title = this.GetString(Resource.String.Downloads);
                    this.SupportActionBar.SubtitleFormatted = null;
                    this.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    this.SupportActionBar.SetHomeButtonEnabled(false);
                }
                else
                {
                    this.SupportActionBar.Title = TransfersViewState.Instance.CurrentlySelectedDLFolder.FolderName;
                    this.SupportActionBar.SubtitleFormatted = null;
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

        #if DEBUG
        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Keycode.VolumeUp)
            {
                SeekerState.SoulseekClient.Disconnect("test");
            }
            else if (e.KeyCode == Keycode.VolumeDown)
            {
                SeekerState.SoulseekClient.ConnectAsync("slowtest", "slowpass");
            }
            return base.DispatchKeyEvent(e);
        }
        #endif
    }
}
