using Android.Content;
using Seeker.Services;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomNavigation;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Seeker.Helpers;

using Common;
namespace Seeker
{
    public partial class TransfersFragment : Fragment, PopupMenu.IOnMenuItemClickListener
    {
        private View rootView = null;

        /// <summary>
        /// We add these to this dict so we can (1) AddUser to get their statuses and (2) efficiently check them when
        /// we get user status changed events.
        /// This dict may contain a greater number of users than strictly necessary.  as it is just used to save time, 
        /// and having additional users here will not cause any issues.  (i.e. in case where other user was offline then the 
        /// user cleared that download, no harm as it will check and see there are no downloads to retry and just remove user)
        /// </summary>
        public static void AddToUserOffline(string username)
        {
            if (TransferState.UsersWhereDownloadFailedDueToOffline.ContainsKey(username))
            {
                return;
            }
            else
            {
                lock (TransferState.UsersWhereDownloadFailedDueToOffline)
                {
                    TransferState.UsersWhereDownloadFailedDueToOffline[username] = 0x0;
                }
                try
                {
                    SeekerState.SoulseekClient.WatchUserAsync(username);
                }
                catch (System.Exception)
                {
                    // noop
                    // if user is not logged in then next time they log in the user will be added...
                }
            }
        }

        public static TransferItemManager TransferItemManagerDL; //for downloads
        public static TransferItemManager TransferItemManagerUploads; //for uploads
        public static TransferItemManagerWrapper TransferItemManagerWrapped;


        private static TransfersViewState ViewState => TransfersViewState.Instance;

        //private ListView primaryListView = null;
        private TextView noTransfers = null;
        private Button setupUpSharing = null;
        private ISharedPreferences sharedPreferences = null;
        private static System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> ProgressUpdatedThrottler = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
        public const int THROTTLE_PROGRESS_UPDATED_RATE = 200;//in ms;

        public override void SetMenuVisibility(bool menuVisible)
        {
            //this is necessary if programmatically moving to a tab from another activity..
            if (menuVisible)
            {
                var navigator = SeekerState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if (navigator != null)
                {
                    navigator.Menu.GetItem(2).SetCheckable(true);
                    navigator.Menu.GetItem(2).SetChecked(true);
                }
            }
            base.SetMenuVisibility(menuVisible);
        }


        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.transfers_menu, menu);
            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            if (ViewState.InUploadsMode)
            {
                menu.FindItem(Resource.Id.action_clear_all_complete_and_aborted).SetVisible(true);
                menu.FindItem(Resource.Id.action_abort_all).SetVisible(true);
                if (ViewState.CurrentlySelectedUploadFolder == null)
                {
                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(true);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(true);


                    //todo: menu options.  clear all completed and aborted
                    //todo: menu options.  abort all - are you sure? dialog

                    menu.FindItem(Resource.Id.action_resume_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_pause_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(false);
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed).SetVisible(false);

                }
                else
                {


                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(false);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(false);

                    menu.FindItem(Resource.Id.action_resume_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_pause_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(false);
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed).SetVisible(false);
                }
            }
            else
            {
                menu.FindItem(Resource.Id.action_abort_all).SetVisible(false); //nullref here...
                menu.FindItem(Resource.Id.action_clear_all_complete_and_aborted).SetVisible(false);
                if (ViewState.CurrentlySelectedDLFolder == null)
                {
                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(true);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(true);

                    menu.FindItem(Resource.Id.action_resume_all).SetVisible(true);
                    menu.FindItem(Resource.Id.action_pause_all).SetVisible(true);
                    menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(true);
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(true);
                    menu.FindItem(Resource.Id.retry_all_failed).SetVisible(true);
                }
                else
                {

                    TransferViewHelper.GetStatusNumbers(ViewState.CurrentlySelectedDLFolder.TransferItems, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued);
                    //var state = ViewState.CurrentlySelectedDLFolder.GetState(out bool isFailed);

                    if (numPaused != 0)
                    {
                        menu.FindItem(Resource.Id.action_resume_all).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_resume_all).SetVisible(false);
                    }

                    if (numInProgress != 0 || numQueued != 0)
                    {
                        menu.FindItem(Resource.Id.action_pause_all).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_pause_all).SetVisible(false);
                    }

                    if (numSucceeded != 0)
                    {
                        menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(false);
                    }

                    if (numFailed != 0)
                    {
                        menu.FindItem(Resource.Id.retry_all_failed).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.retry_all_failed).SetVisible(false);
                    }

                    //i.e. if there are any not in the succeeded state that clear all complete would clear
                    if (numFailed != 0 || numInProgress != 0 || numPaused != 0)
                    {
                        menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(false);
                    }

                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(false);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(false);
                }
            }

            if (PreferencesState.TransferViewShowSizes)
            {
                menu.FindItem(Resource.Id.action_show_size).SetTitle(Resource.String.HideSize);
            }
            else
            {
                menu.FindItem(Resource.Id.action_show_size).SetTitle(Resource.String.ShowSize);
            }

            if (PreferencesState.TransferViewShowSpeed)
            {
                menu.FindItem(Resource.Id.action_show_speed).SetTitle(Resource.String.HideSpeed);
            }
            else
            {
                menu.FindItem(Resource.Id.action_show_speed).SetTitle(Resource.String.ShowSpeed);
            }

            base.OnPrepareOptionsMenu(menu);
        }

        public void RefreshForModeSwitch()
        {
            SetRecyclerAdapter();
            SeekerState.MainActivityRef.SetTransferSupportActionBarState();
            SetNoTransfersMessage();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    // TODO2026: obsolete on android 33?
                    SeekerState.MainActivityRef.OnBackPressed();
                    return true;
                case Resource.Id.action_clear_all_complete: //clear all complete
                    Logger.InfoFirebase("Clear All Complete Pressed");
                    if (ViewState.CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.ClearAllComplete();
                    }
                    else
                    {
                        TransferItemManagerDL.ClearAllCompleteFromFolder(ViewState.CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_toggle_group_by: //toggle group by. group / ungroup by folder.
                    ViewState.GroupByFolder = !ViewState.GroupByFolder;
                    SetRecyclerAdapter();
                    return true;
                case Resource.Id.action_show_size:
                    PreferencesState.TransferViewShowSizes = !PreferencesState.TransferViewShowSizes;
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_show_speed:
                    PreferencesState.TransferViewShowSpeed = !PreferencesState.TransferViewShowSpeed;
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_clear_all_complete_and_aborted:
                    Logger.InfoFirebase("Clear All Complete Pressed");
                    if (ViewState.CurrentlySelectedUploadFolder == null)
                    {
                        TransferItemManagerUploads.ClearAllComplete();
                    }
                    else
                    {
                        TransferItemManagerUploads.ClearAllCompleteFromFolder(ViewState.CurrentlySelectedUploadFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_toggle_download_upload:
                    ViewState.InUploadsMode = !ViewState.InUploadsMode;
                    RefreshForModeSwitch();
                    return true;
                case Resource.Id.action_cancel_and_clear_all: //cancel and clear all
                    Logger.InfoFirebase("action_cancel_and_clear_all Pressed");
                    SeekerState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (ViewState.CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.CancelAll(true);
                        var cleanupItems = TransferItemManagerDL.ClearAllReturnCleanupItems();
                        if (cleanupItems.Any()) TransferItemManagerWrapper.CleanupEntry(cleanupItems);
                    }
                    else
                    {
                        TransferItemManagerDL.CancelFolder(ViewState.CurrentlySelectedDLFolder, true);
                        var cleanupItems = TransferItemManagerDL.ClearAllFromFolderReturnCleanupItems(ViewState.CurrentlySelectedDLFolder);
                        if (cleanupItems.Any()) TransferItemManagerWrapper.CleanupEntry(cleanupItems);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_abort_all: //abort all
                    Logger.InfoFirebase("action_abort_all_pressed");
                    SeekerState.AbortAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (ViewState.CurrentlySelectedUploadFolder == null)
                    {
                        TransferItemManagerUploads.CancelAll();
                    }
                    else
                    {
                        TransferItemManagerUploads.CancelFolder(ViewState.CurrentlySelectedUploadFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_pause_all:
                    Logger.InfoFirebase("pause all Pressed");
                    SeekerState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (ViewState.CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.CancelAll();
                    }
                    else
                    {
                        TransferItemManagerDL.CancelFolder(ViewState.CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_resume_all:
                    Logger.InfoFirebase("resume all Pressed");
                    if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                    {
                        Task t;
                        if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                        {
                            return base.OnContextItemSelected(item);
                        }
                        t.ContinueWith(new Action<Task>((Task t) =>
                        {
                            if (t.IsFaulted)
                            {
                                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                                {
                                    if (Context != null)
                                    {
                                        Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                    }
                                    else
                                    {
                                        Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                    }

                                });
                                return;
                            }
                            if (ViewState.CurrentlySelectedDLFolder == null)
                            {
                                SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(false, true, null, false); });
                            }
                            else
                            {
                                DownloadRetryAllConditionLogic(false, false, ViewState.CurrentlySelectedDLFolder, false);
                            }
                        }));
                    }
                    else
                    {
                        if (ViewState.CurrentlySelectedDLFolder == null)
                        {
                            DownloadRetryAllConditionLogic(false, true, null, false);
                        }
                        else
                        {
                            DownloadRetryAllConditionLogic(false, false, ViewState.CurrentlySelectedDLFolder, false);
                        }
                    }
                    return true;
                case Resource.Id.retry_all_failed:
                    RetryAllConditionEntry(true, false);
                    return true;
                case Resource.Id.batch_select:
                    TransfersActionModeCallback = new ActionModeCallback() { Adapter = recyclerTransferAdapter, Frag = this };
                    ForceOutIfZeroSelected = false;
                    //AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)SeekerState.MainActivityRef.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
                    //TransfersActionMode = myToolbar.StartActionMode(TransfersActionModeCallback);
                    TransfersActionMode = SeekerState.MainActivityRef.StartActionMode(TransfersActionModeCallback);
                    recyclerTransferAdapter.IsInBatchSelectMode = true;
                    TransfersActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), 0);
                    TransfersActionMode.Invalidate();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void RetryAllConditionEntry(bool failed, bool batchSelectedOnly)
        {
            //get the batch selected transfer items ahead of time, because the next thing we do is clear the batch selected indices
            //AND also the user can add or clear transfers in the case where we continue on logging in for this, so the positions will be wrong..
            var listTi = batchSelectedOnly ? GetBatchSelectedItemsForRetryCondition(failed) : null;
            Logger.InfoFirebase("retry all failed Pressed batch? " + batchSelectedOnly);
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                        {
                            if (Context != null)
                            {
                                Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                            }
                            else
                            {
                                Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                            }

                        });
                        return;
                    }
                    if (ViewState.CurrentlySelectedDLFolder == null)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(failed, true, null, batchSelectedOnly, listTi); });
                    }
                    else
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(failed, false, ViewState.CurrentlySelectedDLFolder, batchSelectedOnly, listTi); });
                    }
                }));
            }
            else
            {
                if (ViewState.CurrentlySelectedDLFolder == null)
                {
                    DownloadRetryAllConditionLogic(failed, true, null, batchSelectedOnly, listTi);
                }
                else
                {
                    DownloadRetryAllConditionLogic(failed, false, ViewState.CurrentlySelectedDLFolder, batchSelectedOnly, listTi);
                }
            }
        }


        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        public TransferAdapterRecyclerVersion recyclerTransferAdapter;

        public override void OnDestroy()
        {
            try
            {
                TransfersActionMode?.Finish();
            }
            catch (System.Exception)
            {

            }
            base.OnDestroy();
        }

        public static void RestoreUploadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferListv2 = string.Empty;//sharedPreferences.GetString(KeyConsts.M_Upload_TransferList_v2, string.Empty); //TODO !!! replace !!!
            if (transferListv2 == string.Empty)
            {
                RestoreUploadTransferItemsLegacy(sharedPreferences);
            }
            else
            {
                //restore the simple way via deserializing...
                TransferItemManagerUploads = new TransferItemManager(true);
                using (var stream = new System.IO.StringReader(transferListv2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerUploads.GetType());
                    TransferItemManagerUploads = serializer.Deserialize(stream) as TransferItemManager;
                    //BackwardsCompatFunction(transferItemsLegacy);
                    TransferItemManagerUploads.OnRelaunch();
                }
            }
        }

        public static void RestoreUploadTransferItemsLegacy(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(KeyConsts.M_TransferListUpload, string.Empty);
            if (transferList == string.Empty)
            {
                TransferItemManagerUploads = new TransferItemManager(true);
            }
            else
            {
                var transferItemsLegacy = new List<TransferItem>();
                using (var stream = new System.IO.StringReader(transferList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(transferItemsLegacy.GetType());
                    transferItemsLegacy = serializer.Deserialize(stream) as List<TransferItem>;
                    //BackwardsCompatFunction(transferItemsLegacy);
                    OnRelaunch(transferItemsLegacy);
                }

                TransferItemManagerUploads = new TransferItemManager(true);
                //populate the new data structure.
                foreach (var ti in transferItemsLegacy)
                {
                    TransferItemManagerUploads.Add(ti);
                }

            }
        }



        public static void RestoreDownloadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferListv2 = string.Empty;//sharedPreferences.GetString(KeyConsts.M_TransferList_v2, string.Empty);
            if (transferListv2 == string.Empty)
            {
                RestoreDownloadTransferItemsLegacy(sharedPreferences);
            }
            else
            {
                //restore the simple way via deserializing...
                TransferItemManagerDL = new TransferItemManager();
                using (var stream = new System.IO.StringReader(transferListv2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerDL.GetType());
                    TransferItemManagerDL = serializer.Deserialize(stream) as TransferItemManager;
                    //BackwardsCompatFunction(transferItemsLegacy);
                    TransferItemManagerDL.OnRelaunch();
                }
            }


        }

        public static void RestoreDownloadTransferItemsLegacy(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(KeyConsts.M_TransferList, string.Empty);
            if (transferList == string.Empty)
            {
                TransferItemManagerDL = new TransferItemManager();
            }
            else
            {
                var transferItemsLegacy = new List<TransferItem>();
                using (var stream = new System.IO.StringReader(transferList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(transferItemsLegacy.GetType());
                    transferItemsLegacy = serializer.Deserialize(stream) as List<TransferItem>;
                    BackwardsCompatFunction(transferItemsLegacy);
                    OnRelaunch(transferItemsLegacy);
                }

                TransferItemManagerDL = new TransferItemManager();
                //populate the new data structure.
                foreach (var ti in transferItemsLegacy)
                {
                    TransferItemManagerDL.Add(ti);
                }

            }
        }

        public static void OnRelaunch(List<TransferItem> transfers)
        {   //transfers that were previously InProgress before we shut down should now be considered paused (cancelled). or aborted (cancelled upload)
            foreach (var ti in transfers)
            {
                if (ti.State.HasFlag(TransferStates.InProgress))
                {
                    ti.State = TransferStates.Cancelled;
                    ti.RemainingTime = null;
                }

                if (ti.State.HasFlag(TransferStates.Aborted)) //i.e. re-requesting.
                {
                    ti.State = TransferStates.Cancelled;
                    ti.RemainingTime = null;
                }

                if (ti.State.HasFlag(TransferStates.UserOffline))
                {
                    TransferState.UsersWhereDownloadFailedDueToOffline[ti.Username] = 0x0;
                }
            }
        }

        public static void BackwardsCompatFunction(List<TransferItem> transfers)
        {
            //this should eventually be removed.  its just for the old transfers which have state == None
            foreach (var ti in transfers)
            {
                if (ti.State == TransferStates.None)
                {
                    if (ti.Failed)
                    {
                        ti.State = TransferStates.Errored;
                    }
                    else if (ti.Progress == 100)
                    {
                        ti.State = TransferStates.Succeeded;
                    }
                }
            }
        }

        private void SetNoTransfersMessage()
        {
            if (ViewState.InUploadsMode)
            {
                if (!(TransferItemManagerUploads.IsEmpty()))
                {
                    noTransfers.Visibility = ViewStates.Gone;
                    setupUpSharing.Visibility = ViewStates.Gone;
                }
                else
                {
                    noTransfers.Visibility = ViewStates.Visible;
                    if (SharedFileService.MeetsSharingConditions())
                    {
                        noTransfers.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads_yet);
                        setupUpSharing.Visibility = ViewStates.Gone;
                    }
                    else if(PreferencesState.SharingOn && UploadDirectoryManager.UploadDirectories.Count != 0 && UploadDirectoryManager.AreAllFailed())
                    {
                        noTransfers.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads_yet_failed_to_set_up_shared_files);
                        setupUpSharing.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        noTransfers.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads_yet_not_sharing);
                        setupUpSharing.Visibility = ViewStates.Visible;
                    }
                }
            }
            else
            {
                setupUpSharing.Visibility = ViewStates.Gone;
                if (!(TransferItemManagerDL.IsEmpty()))
                {
                    noTransfers.Visibility = ViewStates.Gone;
                }
                else
                {
                    noTransfers.Visibility = ViewStates.Visible;
                    noTransfers.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_transfers_yet);
                }
            }
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            StaticHacks.TransfersFrag = this;
            HasOptionsMenu = true;
            Logger.Debug("TransfersFragment OnCreateView");
            if (!OperatingSystem.IsAndroidVersionAtLeast(21))
            {
                AndroidX.AppCompat.App.AppCompatDelegate.CompatVectorFromResourcesEnabled = true;
                this.rootView = inflater.Inflate(Resource.Layout.transfers, container, false);
            }
            else
            {
                this.rootView = inflater.Inflate(Resource.Layout.transfers, container, false);
            }
            //this.primaryListView = rootView.FindViewById<ListView>(Resource.Id.listView1);
            recyclerViewTransferItems = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerView1);
            this.noTransfers = rootView.FindViewById<TextView>(Resource.Id.noTransfersView);
            this.setupUpSharing = rootView.FindViewById<Button>(Resource.Id.setUpSharing);
            this.setupUpSharing.Click += SetupUpSharing_Click;

            //View transferOptions = rootView.FindViewById<View>(Resource.Id.transferOptions);
            //transferOptions.Click += TransferOptions_Click;
            this.RegisterForContextMenu(recyclerViewTransferItems); //doesnt work for recycle views
            sharedPreferences = SeekerState.SharedPreferences;
            //if (TransferItemManagerDL == null)//bc our sharedPref string can be older than the transferItems
            //{
            //    RestoreDownloadTransferItems(sharedPreferences);
            //    RestoreUploadTransferItems(sharedPreferences);
            //    TransferItemManagerWrapped = new TransferItemManagerWrapper(TransfersFragment.TransferItemManagerUploads, TransfersFragment.TransferItemManagerDL);
            //}

            SetNoTransfersMessage();


            //TransferAdapter customAdapter = new TransferAdapter(Context, transferItems);
            //primaryListView.Adapter = (customAdapter);

            recycleLayoutManager = new CustomLinearLayoutManager(Activity);
            SetRecyclerAdapter();

            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);

            Logger.InfoFirebase("AutoClear: " + PreferencesState.AutoClearCompleteDownloads);
            Logger.InfoFirebase("AutoRetry: " + SeekerState.AutoRetryDownload);

            return rootView;
        }

        private void SetupUpSharing_Click(object sender, EventArgs e)
        {
            Intent intent2 = new Intent(SeekerState.MainActivityRef, typeof(SettingsActivity));
            intent2.PutExtra(SettingsActivity.SCROLL_TO_SHARING_SECTION_STRING, SettingsActivity.SCROLL_TO_SHARING_SECTION);
            SeekerState.MainActivityRef.StartActivityForResult(intent2, 140);
        }

        public void SaveScrollPositionOnMovingIntoFolder()
        {
            ViewState.ScrollPositionBeforeMovingIntoFolder = ((LinearLayoutManager)recycleLayoutManager).FindFirstVisibleItemPosition();
            View v = recyclerViewTransferItems.GetChildAt(0);
            if (v == null)
            {
                ViewState.ScrollOffsetBeforeMovingIntoFolder = 0;
            }
            else
            {
                ViewState.ScrollOffsetBeforeMovingIntoFolder = v.Top - recyclerViewTransferItems.Top;
            }
        }

        public void RestoreScrollPosition()
        {
            ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(ViewState.ScrollPositionBeforeMovingIntoFolder, ViewState.ScrollOffsetBeforeMovingIntoFolder); //if you dont do with offset, it scrolls it until the visible item is simply in view (so it will be at bottom, almost a whole screen off)
        }

        // TODO2026
        public static ActionModeCallback TransfersActionModeCallback = null;
        public static ActionMode TransfersActionMode = null;

        public void SetRecyclerAdapter(bool restoreState = false)
        {
            lock (TransferItemManagerWrapped.GetUICurrentList())
            {
                int prevScrollPos = 0;
                int scrollOffset = 0;
                if (restoreState)
                {
                    prevScrollPos = ((LinearLayoutManager)recycleLayoutManager).FindFirstVisibleItemPosition();
                    View v = recyclerViewTransferItems.GetChildAt(0);
                    if (v == null)
                    {
                        scrollOffset = 0;
                    }
                    else
                    {
                        scrollOffset = v.Top - recyclerViewTransferItems.Top;
                    }
                }


                if (ViewState.GroupByFolder && !ViewState.CurrentlyInFolder())
                {
                    recyclerTransferAdapter = new TransferAdapterRecyclerFolderItem(TransferItemManagerWrapped.GetUICurrentList() as List<FolderItem>);
                }
                else
                {
                    recyclerTransferAdapter = new TransferAdapterRecyclerIndividualItem(TransferItemManagerWrapped.GetUICurrentList() as List<TransferItem>);
                }
                recyclerTransferAdapter.TransfersFragment = this;
                recyclerTransferAdapter.IsInBatchSelectMode = (TransfersActionMode != null);
                recyclerViewTransferItems.SetAdapter(recyclerTransferAdapter);

                if (restoreState)
                {
                    ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(prevScrollPos, scrollOffset);
                }
            }
        }

        /// <summary>
        /// This is a UI event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TranferQueueStateChanged(object sender, TransferItem e)
        {
            SeekerState.MainActivityRef.RunOnUiThread(new Action(() =>
            {
                UpdateQueueState(e.FullFilename);
            }));
        }

        private void TransferOptions_Click(object sender, EventArgs e)
        {
            try
            {
                PopupMenu popup = new PopupMenu(SeekerState.MainActivityRef, sender as View);
                popup.SetOnMenuItemClickListener(this);//  setOnMenuItemClickListener(MainActivity.this);
                popup.Inflate(Resource.Menu.transfers_options);
                popup.Show();
            }
            catch (System.Exception error)
            {
                //in response to a crash android.view.WindowManager.BadTokenException
                //This crash is usually caused by your app trying to display a dialog using a previously-finished Activity as a context.
                //in this case not showing it is probably best... as opposed to a crash...
                Logger.Firebase(error.Message + " POPUP BAD ERROR");
            }
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            return false;
        }


        public override void OnResume()
        {
            StaticHacks.TransfersFrag = this;
            if (MainActivity.fromNotificationMoveToUploads)
            {
                MainActivity.fromNotificationMoveToUploads = false;
                this.MoveToUploadForNotif();
            }
            SetNoTransfersMessage(); // in case coming back from settings
            base.OnResume();
        }

        public void MoveToUploadForNotif()
        {
            ViewState.InUploadsMode = true;
            ViewState.CurrentlySelectedDLFolder = null;
            ViewState.CurrentlySelectedUploadFolder = null;
            this.RefreshForModeSwitch();
            SeekerState.MainActivityRef.InvalidateOptionsMenu();
        }

        public override void OnPause()
        {
            base.OnPause();
            Logger.Debug("TransferFragment OnPause");  //this occurs when we move to the Account Tab or if we press the home button (i.e. to later kill the process)
                                                                //so this is a good place to do it.
            SaveTransferItems(sharedPreferences);
        }

        public static object TransferStateSaveLock = new object();

        public static void SaveTransferItems(ISharedPreferences sharedPreferences, bool force = true, int maxSecondsUpdate = 0)
        {
            Logger.Debug("---- saving transfer items enter ----");
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            sw.Start();
#endif

            if (force || (SeekerApplication.TransfersDownloadsCompleteStale && DateTime.UtcNow.Subtract(SeekerApplication.TransfersLastSavedTime).TotalSeconds > maxSecondsUpdate)) //stale and we havent updated too recently..
            {
                Logger.Debug("---- saving transfer items actual save ----");
                if (TransferItemManagerDL?.AllTransferItems == null)
                {
                    return;
                }
                string listOfDownloadItems = string.Empty;
                string listOfUploadItems = string.Empty;
                using (var writer = new System.IO.StringWriter())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerDL.AllTransferItems.GetType());
                    serializer.Serialize(writer, TransferItemManagerDL.AllTransferItems);
                    listOfDownloadItems = writer.ToString();
                }
                using (var writer = new System.IO.StringWriter())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerUploads.AllTransferItems.GetType());
                    serializer.Serialize(writer, TransferItemManagerUploads.AllTransferItems);
                    listOfUploadItems = writer.ToString();
                }
                PreferencesManager.SaveTransferItems(listOfDownloadItems, listOfUploadItems);

                SeekerApplication.TransfersDownloadsCompleteStale = false;
                SeekerApplication.TransfersLastSavedTime = DateTime.UtcNow;
            }

#if DEBUG
            sw.Stop();
            Logger.Debug("saving time: " + sw.ElapsedMilliseconds);
#endif


        }

        private List<TransferItem> GetBatchSelectedItemsForRetryCondition(bool selectFailed)
        {
            bool folderItems = false;
            if (ViewState.GroupByFolder && !ViewState.CurrentlyInFolder())
            {
                folderItems = true;
            }
            var uiState = ViewState.CreateDLUIState();
            List<TransferItem> tis = new List<TransferItem>();
            foreach (int pos in ViewState.BatchSelectedItems)
            {
                if (folderItems)
                {
                    var fi = TransferItemManagerDL.GetItemAtUserIndex(pos, uiState) as FolderItem;
                    foreach (TransferItem ti in fi.TransferItems)
                    {
                        if (selectFailed && ti.Failed)
                        {
                            tis.Add(ti);
                        }
                        else if (!selectFailed && (ti.State.HasFlag(TransferStates.Cancelled) || ti.State.HasFlag(TransferStates.Queued)))
                        {
                            tis.Add(ti);
                        }
                    }
                }
                else
                {
                    var ti = TransferItemManagerDL.GetItemAtUserIndex(pos, uiState) as TransferItem;
                    if (selectFailed && ti.Failed)
                    {
                        tis.Add(ti);
                    }
                    else if (!selectFailed && (ti.State.HasFlag(TransferStates.Cancelled) || ti.State.HasFlag(TransferStates.Queued)))
                    {
                        tis.Add(ti);
                    }
                }
            }
            return tis;
        }


        public static void DownloadRetryAllConditionLogic(bool selectFailed, bool all, FolderItem specifiedFolderOnly, bool batchSelectedOnly, List<TransferItem> batchSelectedTis = null) //if true DownloadRetryAllFailed if false Resume All Paused. if not all then specified folder
        {
            IEnumerable<TransferItem> transferItemConditionList = new List<TransferItem>();
            if (batchSelectedOnly)
            {
                if (batchSelectedTis == null)
                {
                    throw new System.Exception("No batch selected transfer items provided");
                }
                transferItemConditionList = batchSelectedTis;
            }
            else if (all)
            {
                if (selectFailed)
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfFailed().Select(tup => tup.Item1);
                }
                else
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfPaused().Select(tup => tup.Item1);
                }
            }
            else
            {
                if (selectFailed)
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfFailedFromFolder(specifiedFolderOnly).Select(tup => tup.Item1);
                }
                else
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfPausedFromFolder(specifiedFolderOnly).Select(tup => tup.Item1);
                }
            }
            bool exceptionShown = false;
            foreach (TransferItem item in transferItemConditionList)
            {
                //TransferItem item1 = transferItems[info.Position];  
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    TransferState.SetupCancellationToken(item, cancellationTokenSource, out _);
                    var dlInfo = new DownloadInfo(item.Username, item.FullFilename, item.Size, null, cancellationTokenSource, item.QueueLength, 0, item.GetDirectoryLevel()) { TransferItemReference = item };
                    Task task = TransfersUtil.DownloadFileAsync(item.Username, item.FullFilename, item.GetSizeForDL(), cancellationTokenSource, out _, dlInfo, isFileDecodedLegacy: item.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: item.ShouldEncodeFolderLatin1());
                    task.ContinueWith(DownloadService.DownloadContinuationActionUI(new DownloadAddedEventArgs(dlInfo)));
                }
                catch (DuplicateTransferException)
                {
                    //happens due to button mashing...
                    return;
                }
                catch (System.Exception error)
                {
                    Action a = new Action(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.error_) + error.Message, ToastLength.Long).Show(); });
                    if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                    {

                    }
                    else
                    {
                        Logger.Firebase(error.Message + " OnContextItemSelected");
                    }
                    if (!exceptionShown)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(a);
                        exceptionShown = true;
                    }
                    return; //otherwise null ref with task!
                }
                //save to disk, update ui.
                //task.ContinueWith(SeekerState.MainActivityRef.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username,item1.FullFilename,item1.Size,task, cancellationTokenSource))));
                item.Progress = 0; //no longer red... some good user feedback
                item.Failed = false;

            }

            var refreshOnlySelected = new Action(() =>
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(
                    () =>
                    {

                        //previously this was not always on the UI thread (i.e. when called due to
                        // a user we previously downloaded from going back online) when this happened
                        // not only did the recycleview break weirdly, but the whole main activity
                        // (the login screen, search screen, action bar, browse screen).

                        var uiState = ViewState.CreateDLUIState();
                        HashSet<int> indicesToUpdate = new HashSet<int>();
                        foreach (TransferItem ti in transferItemConditionList)
                        {
                            int pos = TransferItemManagerDL.GetUserIndexForTransferItem(ti, uiState);
                            if (pos == -1)
                            {
                                Logger.Debug("pos == -1!!");
                                continue;
                            }

                            if (indicesToUpdate.Contains(pos))
                            {
                                //this is for if we are in a folder.  since previously we would update a folder of 10 items, 10 times which looked quite glitchy...
                                Logger.Debug($"skipping same pos {pos}");
                            }
                            else
                            {
                                indicesToUpdate.Add(pos);
                            }
                        }
                        if (ViewState.InUploadsMode)
                        {
                            return;
                        }
                        foreach (int i in indicesToUpdate)
                        {
                            Logger.Debug($"updating {i}");
                            if (StaticHacks.TransfersFrag != null)
                            {
                                StaticHacks.TransfersFrag.recyclerTransferAdapter?.NotifyItemChanged(i);
                            }
                        }



                    });
            });
            lock (TransferItemManagerDL.GetUICurrentList(ViewState.CreateDLUIState())) //TODO: test
            { //also can update this to do a partial refresh...
                if (StaticHacks.TransfersFrag != null)
                {
                    StaticHacks.TransfersFrag.refreshListView(refreshOnlySelected);
                }
            }
        }


        private void DownloadRetryLogic(ITransferItem transferItem)
        {
            //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
            //its possible that in between creating this contextmenu and getting here that the transfer items changed especially if maybe a re-login was done...
            //either position changed, or someone just straight up cleared them in the meantime...
            //maybe we can add the filename in the menuinfo to match it with, rather than the index..

            //NEVER USE GetChildAt!!!  IT WILL RETURN NULL IF YOU HAVE MORE THAN ONE PAGE OF DATA
            //FindViewByPosition works PERFECTLY.  IT RETURNS THE VIEW CORRESPONDING TO THE TRANSFER LIST..

            //ITransferItemView targetView = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(position) as ITransferItemView;

            //TransferItem item1 = null;
            //Logger.Debug("targetView is null? " + (targetView == null).ToString());
            //if (targetView == null)
            //{
            //    SeekerApplication.ShowToast(SeekerState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
            //    return;
            //}
            string chosenFname = (transferItem as TransferItem).FullFilename; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            string chosenUname = (transferItem as TransferItem).Username; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            Logger.Debug("chosenFname? " + chosenFname);
            TransferItem item1 = TransferItemManagerDL.GetTransferItemWithIndexFromAll(chosenFname, chosenUname, out int _);
            int indexToRefresh = TransferItemManagerDL.GetUserIndexForTransferItem(item1, ViewState.CreateDLUIState());
            Logger.Debug("item1 is null?" + (item1 == null).ToString());//tested
            if (item1 == null || indexToRefresh == -1)
            {
                SeekerApplication.ShowToast(SeekerState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
                return;
            }

            //int tokenNum = int.MinValue;
            if (SeekerState.SoulseekClient.IsTransferInDownloads(item1.Username, item1.FullFilename/*, out tokenNum*/))
            {
                Logger.Debug("transfer is in Downloads !!! " + item1.FullFilename);
                item1.CancelAndRetryFlag = true;
                ClearTransferForRetry(item1, indexToRefresh);
                if (item1.CancellationTokenSource != null)
                {
                    if (!item1.CancellationTokenSource.IsCancellationRequested)
                    {
                        item1.CancellationTokenSource.Cancel();
                    }
                }
                else
                {
                    Logger.Firebase("CTS is null. this should not happen. we should always set it before downloading.");
                }
                return; //the dl continuation method will take care of it....
            }

            //TransferItem item1 = transferItems[info.Position];  
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {

                TransferState.SetupCancellationToken(item1, cancellationTokenSource, out _);
                var dlInfo = new DownloadInfo(item1.Username, item1.FullFilename, item1.Size, null, cancellationTokenSource, item1.QueueLength, item1.Failed ? 1 : 0, item1.GetDirectoryLevel()) { TransferItemReference = item1 };
                Task task = TransfersUtil.DownloadFileAsync(item1.Username, item1.FullFilename, item1.GetSizeForDL(), cancellationTokenSource, out _, dlInfo, isFileDecodedLegacy: item1.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: item1.ShouldEncodeFolderLatin1());
                task.ContinueWith(DownloadService.DownloadContinuationActionUI(new DownloadAddedEventArgs(dlInfo))); //if paused do retry counter 0.
            }
            catch (DuplicateTransferException)
            {
                //happens due to button mashing...
                return;
            }
            catch (System.Exception error)
            {
                Action a = new Action(() => { Toast.MakeText(SeekerState.MainActivityRef, SeekerState.MainActivityRef.GetString(Resource.String.error_) + error.Message, ToastLength.Long); });
                if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                {

                }
                else
                {
                    Logger.Firebase(error.Message + " OnContextItemSelected");
                }
                SeekerState.MainActivityRef.RunOnUiThread(a);
                return; //otherwise null ref with task!
            }
            ClearTransferForRetry(item1, indexToRefresh);
        }

        private void ClearTransferForRetry(TransferItem item1, int position)
        {
            item1.Progress = 0; //no longer red... some good user feedback
            item1.QueueLength = int.MaxValue; //let the State Changed update this for us...
            item1.Failed = false;
            var refreshOnlySelected = new Action(() =>
            {

                Logger.Debug("notifyItemChanged " + position);

                recyclerTransferAdapter.NotifyItemChanged(position);


            });
            lock (TransferItemManagerDL.GetUICurrentList(ViewState.CreateDLUIState()))
            { //also can update this to do a partial refresh...
                refreshListView(refreshOnlySelected);
            }
        }

        private bool NotLoggedInShowMessageGaurd(string msg)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                Toast.MakeText(SeekerState.ActiveActivityRef, "Must be logged in to " + msg, ToastLength.Short).Show();
                return true;
            }
            return false;
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            if (item.GroupId == UNIQUE_TRANSFER_GROUP_ID)
            {
                if (recyclerTransferAdapter == null)
                {
                    Logger.InfoFirebase("recyclerTransferAdapter is null");
                }

                ITransferItem ti = recyclerTransferAdapter.getSelectedItem();

                if (ti == null)
                {
                    Logger.InfoFirebase("ti is null");
                }

                int position = TransferItemManagerWrapped.GetUserIndexForITransferItem(ti);


                if (position == -1)
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                    return base.OnContextItemSelected(item);
                }

                if (CommonHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), ti.GetUsername(), SeekerState.ActiveActivityRef, this.View))
                {
                    Logger.Debug("handled by commons");
                    return base.OnContextItemSelected(item);
                }

                switch (item.ItemId)
                {
                    case 0: //single transfer only
                        //retry download (resume download)
                        if (NotLoggedInShowMessageGaurd("start transfer"))
                        {
                            return true;
                        }

                        if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                        {
                            Task t;
                            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                            {
                                return base.OnContextItemSelected(item);
                            }
                            t.ContinueWith(new Action<Task>((Task t) =>
                            {
                                if (t.IsFaulted)
                                {
                                    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                                    {
                                        if (Context != null)
                                        {
                                            Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }
                                        else
                                        {
                                            Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }

                                    });
                                    return;
                                }
                                SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryLogic(ti); });
                            }));
                        }
                        else
                        {
                            DownloadRetryLogic(ti);
                        }
                        //Toast.MakeText(Applicatio,"Retrying...",ToastLength.Short).Show();
                        break;
                    case 1:
                        //clear complete?
                        //info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
                        Logger.InfoFirebase("Clear Complete item pressed");
                        lock (TransferItemManagerWrapped.GetUICurrentList()) //TODO: test
                        {
                            try
                            {
                                if (ViewState.InUploadsMode)
                                {
                                    TransferItemManagerWrapped.RemoveAtUserIndex(position);
                                }
                                else
                                {
                                    TransferItemManagerWrapped.RemoveAndCleanUpAtUserIndex(position); //UI
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                //Logger.Firebase("case1: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                                Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                                return base.OnContextItemSelected(item);
                            }
                            recyclerTransferAdapter.NotifyItemRemoved(position);  //UI
                            //refreshListView();
                        }
                        break;
                    case 2: //cancel and clear (downloads) OR abort and clear (uploads)
                        //info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
                        Logger.InfoFirebase("Cancel and Clear item pressed");
                        ITransferItem tItem = null;
                        try
                        {
                            tItem = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //Logger.Firebase("case2: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        if (tItem is TransferItem tti)
                        {
                            bool wasInProgress = tti.State.HasFlag(TransferStates.InProgress);
                            TransferState.CancellationTokens.TryGetValue(TransferState.ProduceCancellationTokenKey(tti), out CancellationTokenSource uptoken);
                            uptoken?.Cancel();
                            //TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(tItem)]?.Cancel(); throws if does not exist.
                            TransferState.CancellationTokens.Remove(TransferState.ProduceCancellationTokenKey(tti), out _);
                            lock (TransferItemManagerWrapped.GetUICurrentList())
                            {
                                if (ViewState.InUploadsMode)
                                {
                                    TransferItemManagerWrapped.RemoveAtUserIndex(position);
                                }
                                else
                                {
                                    TransferItemManagerWrapped.RemoveAndCleanUpAtUserIndex(position); //this means basically, wait for the stream to be closed. no race conditions..
                                }
                                recyclerTransferAdapter.NotifyItemRemoved(position);
                            }
                        }
                        else if (tItem is FolderItem fi)
                        {
                            TransferItemManagerWrapped.CancelFolder(fi, true);
                            TransferItemManagerWrapped.ClearAllFromFolderAndClean(fi);
                            lock (TransferItemManagerWrapped.GetUICurrentList())
                            {
                                //TransferItemManagerDL.RemoveAtUserIndex(position); we already removed
                                recyclerTransferAdapter.NotifyItemRemoved(position);
                            }
                        }
                        else
                        {
                            Logger.InfoFirebase("Cancel and Clear item pressed - bad item");
                        }
                        break;
                    case 3:
                        if (NotLoggedInShowMessageGaurd("get queue position"))
                        {
                            return true;
                        }
                        tItem = null;
                        try
                        {
                            tItem = TransferItemManagerDL.GetItemAtUserIndex(position, ViewState.CreateDLUIState());
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //Logger.Firebase("case3: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }

                        //the folder implementation will re-request all queued files
                        //recently I thought of just doing the lowest, but then if the lowest is ready, it will download leaving the other transfers behind.


                        if (tItem is TransferItem)
                        {
                            GetQueuePosition(tItem as TransferItem);
                        }
                        else if (tItem is FolderItem folderItem)
                        {
                            lock (folderItem.TransferItems)
                            {
                                foreach (TransferItem transferItem in folderItem.TransferItems.Where(ti => ti.State == TransferStates.Queued))
                                {
                                    GetQueuePosition(transferItem);
                                }
                            }
                        }
                        break;
                    case 4:
                        tItem = null;
                        try
                        {
                            tItem = TransferItemManagerDL.GetItemAtUserIndex(position, ViewState.CreateDLUIState()) as TransferItem;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //Logger.Firebase("case4: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        try
                        {
                            //tested on API25 and API30
                            //AndroidX.Core.Content.FileProvider
                            Android.Net.Uri uriToUse = null;
                            if (SeekerState.UseLegacyStorage() && SimpleHelpers.IsFileUri((tItem as TransferItem).FinalUri)) //i.e. if it is a FILE URI.
                            {
                                uriToUse = AndroidX.Core.Content.FileProvider.GetUriForFile(this.Context, this.Context.ApplicationContext.PackageName + ".provider", new Java.IO.File(Android.Net.Uri.Parse((tItem as TransferItem).FinalUri).Path));
                            }
                            else
                            {
                                uriToUse = Android.Net.Uri.Parse((tItem as TransferItem).FinalUri);
                            }
                            Intent playFileIntent = new Intent(Intent.ActionView);
                            //playFileIntent.SetDataAndType(uriToUse,"audio/*");  
                            playFileIntent.SetDataAndType(uriToUse, CommonHelpers.GetMimeTypeFromFilename((tItem as TransferItem).FullFilename));   //works
                            playFileIntent.AddFlags(ActivityFlags.GrantReadUriPermission | /*ActivityFlags.NewTask |*/ ActivityFlags.GrantWriteUriPermission); //works.  newtask makes it go to foobar and immediately jump back
                            //Intent chooser = Intent.CreateChooser(playFileIntent, "Play song with");
                            this.StartActivity(playFileIntent); //also the chooser isnt needed.  if you show without the chooser, it will show you the options and you can check Only Once, Always.
                        }
                        catch (System.Exception e)
                        {
                            Logger.Firebase(e.Message + e.StackTrace);
                            Toast.MakeText(this.Context, Resource.String.failed_to_play, ToastLength.Short).Show(); //normally bc no player is installed.
                        }
                        break;
                    case 7: //browse at location (browse at folder)
                        if (NotLoggedInShowMessageGaurd("browse folder"))
                        {
                            return true;
                        }
                        if (ti is TransferItem ttti)
                        {
                            string startingDir = SimpleHelpers.GetDirectoryRequestFolderName(ttti.FullFilename);
                            Action<View> action = new Action<View>((v) =>
                            {
                                ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                            });

                            DownloadDialog.RequestFilesApi(ttti.Username, this.View, action, startingDir);
                        }
                        else if (ti is FolderItem fi)
                        {
                            if (fi.IsEmpty())
                            {
                                //since if auto clear is on, and the menu is already up, the final item in this folder can clear before we end up selecting something.
                                Toast.MakeText(SeekerState.ActiveActivityRef, "Folder is empty.", ToastLength.Short).Show();
                                return true;
                            }
                            string startingDir = SimpleHelpers.GetDirectoryRequestFolderName(fi.TransferItems[0].FullFilename);
                            for (int i = 0; i < fi.GetDirectoryLevel() - 1; i++)
                            {
                                startingDir = SimpleHelpers.GetDirectoryRequestFolderName(startingDir); //keep going up..
                            }


                            Action<View> action = new Action<View>((v) =>
                            {
                                ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                            });

                            DownloadDialog.RequestFilesApi(fi.Username, this.View, action, startingDir);
                        }
                        break;
                    case 100: //resume folder
                        if (NotLoggedInShowMessageGaurd("resume folder"))
                        {
                            return true;
                        }
                        Logger.InfoFirebase("resume folder Pressed");
                        if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                        {
                            Task t;
                            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                            {
                                return base.OnContextItemSelected(item);
                            }
                            t.ContinueWith(new Action<Task>((Task t) =>
                            {
                                if (t.IsFaulted)
                                {
                                    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                                    {
                                        if (Context != null)
                                        {
                                            Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }
                                        else
                                        {
                                            Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }

                                    });
                                    return;
                                }
                                SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(false, false, ti as FolderItem, false); });
                            }));
                        }
                        else
                        {
                            DownloadRetryAllConditionLogic(false, false, ti as FolderItem, false);
                        }
                        break;
                    case 101: //pause folder or abort uploads (uploads)
                        TransferItemManagerWrapped.CancelFolder(ti as FolderItem);
                        int index = TransferItemManagerWrapped.GetIndexForFolderItem(ti as FolderItem);
                        recyclerTransferAdapter.NotifyItemChanged(index);
                        break;
                    case 102: //retry failed downloads from folder
                        if (NotLoggedInShowMessageGaurd("retry folder"))
                        {
                            return true;
                        }
                        Logger.InfoFirebase("retry folder Pressed");
                        if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                        {
                            Task t;
                            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                            {
                                return base.OnContextItemSelected(item);
                            }
                            t.ContinueWith(new Action<Task>((Task t) =>
                            {
                                if (t.IsFaulted)
                                {
                                    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                                    {
                                        if (Context != null)
                                        {
                                            Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }
                                        else
                                        {
                                            Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }

                                    });
                                    return;
                                }
                                SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(true, false, ti as FolderItem, false); });
                            }));
                        }
                        else
                        {
                            DownloadRetryAllConditionLogic(true, false, ti as FolderItem, false);
                        }
                        break;
                    case 103: //abort upload
                        Logger.InfoFirebase("Abort Upload item pressed");
                        tItem = null;
                        try
                        {
                            tItem = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //Logger.Firebase("case2: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        TransferItem uploadToCancel = tItem as TransferItem;

                        TransferState.CancellationTokens.TryGetValue(TransferState.ProduceCancellationTokenKey(uploadToCancel), out CancellationTokenSource token);
                        token?.Cancel();
                        //TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(tItem)]?.Cancel(); throws if does not exist.
                        TransferState.CancellationTokens.Remove(TransferState.ProduceCancellationTokenKey(uploadToCancel), out _);
                        lock (TransferItemManagerWrapped.GetUICurrentList())
                        {
                            recyclerTransferAdapter.NotifyItemChanged(position);
                        }
                        break;
                    case 104: //ignore (unshare) user
                        Logger.InfoFirebase("Unshare User item pressed");
                        IEnumerable<TransferItem> tItems = TransferItemManagerWrapped.GetTransferItemsForUser(ti.GetUsername());
                        foreach (var tiToCancel in tItems)
                        {
                            TransferState.CancellationTokens.TryGetValue(TransferState.ProduceCancellationTokenKey(tiToCancel), out CancellationTokenSource token1);
                            token1?.Cancel();
                            TransferState.CancellationTokens.Remove(TransferState.ProduceCancellationTokenKey(tiToCancel), out _);
                            lock (TransferItemManagerWrapped.GetUICurrentList())
                            {
                                int posOfCancelled = TransferItemManagerWrapped.GetUserIndexForTransferItem(tiToCancel);
                                if (posOfCancelled != -1)
                                {
                                    recyclerTransferAdapter.NotifyItemChanged(posOfCancelled);
                                }
                            }
                        }
                        SeekerApplication.AddToIgnoreListFeedback(SeekerState.ActiveActivityRef, ti.GetUsername());
                        break;
                    case 105: //batch selection mode
                        TransfersActionModeCallback = new ActionModeCallback() { Adapter = recyclerTransferAdapter, Frag = this };
                        ForceOutIfZeroSelected = true;
                        //AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)SeekerState.MainActivityRef.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
                        //TransfersActionMode = myToolbar.StartActionMode(TransfersActionModeCallback);
                        TransfersActionMode = SeekerState.MainActivityRef.StartActionMode(TransfersActionModeCallback);
                        recyclerTransferAdapter.IsInBatchSelectMode = true;
                        ToggleItemBatchSelect(recyclerTransferAdapter, position);
                        break;
                }
            }
            return base.OnContextItemSelected(item);
        }

        public static bool ForceOutIfZeroSelected = true;
        public static void ToggleItemBatchSelect(TransferAdapterRecyclerVersion recyclerTransferAdapter, int pos)
        {
            if (ViewState.BatchSelectedItems.Contains(pos))
            {
                ViewState.BatchSelectedItems.Remove(pos);
            }
            else
            {
                ViewState.BatchSelectedItems.Add(pos);
            }
            recyclerTransferAdapter.NotifyItemChanged(pos);
            int cnt = ViewState.BatchSelectedItems.Count;
            if (cnt == 0 && ForceOutIfZeroSelected)
            {
                TransfersActionMode.Finish();
            }
            else
            {
                TransfersActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                TransfersActionMode.Invalidate();
            }
        }

        public static void UpdateBatchSelectedItemsIfApplicable(TransferItem ti)
        {
            if (TransfersActionMode == null)
            {
                return;
            }
            int userPostionBeingRemoved = TransferItemManagerWrapped.GetUserIndexForTransferItem(ti);
            if (userPostionBeingRemoved == -1)
            {
                //it is not currently on our screen, perhaps it is in uploads (and we are in downloads) or we are inside a folder (and it is outside)
                Logger.Debug("batch on, different screen item removed");
                return;
            }
            Logger.Debug("batch on, updating: " + userPostionBeingRemoved);
            //adjust numbers
            int cnt = ViewState.BatchSelectedItems.Count;
            for (int i = cnt - 1; i >= 0; i--)
            {
                int position = ViewState.BatchSelectedItems[i];
                if (position < userPostionBeingRemoved)
                {
                    continue;
                }
                else if (position == userPostionBeingRemoved)
                {
                    ViewState.BatchSelectedItems.RemoveAt(i);
                }
                else
                {
                    ViewState.BatchSelectedItems[i] = position - 1;
                }
            }
            //if there was only 1 and its the one that just finished then take us out of batchSelectedItems
            if (ViewState.BatchSelectedItems.Count == 0)
            {
                TransfersActionMode.Finish();
            }
            else if (ViewState.BatchSelectedItems.Count != cnt) //if we have 1 less now.
            {
                TransfersActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), ViewState.BatchSelectedItems.Count.ToString());
                TransfersActionMode.Invalidate();
            }
        }



        public void GetQueuePosition(TransferItem ttItem)
        {
            int queueLenOld = ttItem.QueueLength;

            Func<TransferItem, object> actionOnComplete = new Func<TransferItem, object>((TransferItem t) =>
            {
                try
                {
                    if (queueLenOld == t.QueueLength) //always true bc its a reference...
                    {
                        if (queueLenOld == int.MaxValue)
                        {
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Position in queue is unknown.", ToastLength.Short).Show();
                        }
                        else
                        {
                            Toast.MakeText(SeekerState.ActiveActivityRef, string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.position_is_still_), t.QueueLength), ToastLength.Short).Show();
                        }
                    }
                    else
                    {
                        int indexOfItem = TransferItemManagerDL.GetUserIndexForTransferItem(t, ViewState.CreateDLUIState());
                        if (indexOfItem == -1 && ViewState.InUploadsMode)
                        {
                            return null;
                        }
                        recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
                    }
                }
                catch (System.Exception e)
                {
                    Logger.Firebase("actionOnComplete" + e.Message + e.StackTrace);
                }

                return null;
            });

            DownloadService.GetDownloadPlaceInQueue(ttItem.Username, ttItem.FullFilename, true, false, ttItem, actionOnComplete);
        }

        public void UpdateQueueState(string fullFilename) //Add this to the event handlers so that when downloads are added they have their queue position.
        {
            try
            {
                if (ViewState.InUploadsMode)
                {
                    return;
                }
                int indexOfItem = TransferItemManagerDL.GetUserIndexForTransferItem(fullFilename, ViewState.CreateDLUIState());
                Logger.Debug("NotifyItemChanged + UpdateQueueState" + indexOfItem);
                Logger.Debug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: " + fullFilename);
                if (recyclerTransferAdapter.ItemCount == indexOfItem)
                {

                }
                Logger.Debug("UI thread: " + Looper.MainLooper.IsCurrentThread);
                recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
            }
            catch (System.Exception)
            {

            }
        }

        private void refreshListViewSafe()
        {
            try
            {
                refreshListView();
            }
            catch (System.Exception)
            {

            }
        }

        private void ClearProgressBarColor(ProgressBar pb)
        {
#pragma warning disable 0618
            if (OperatingSystem.IsAndroidVersionAtLeast(21))
            {
                pb.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
            }
            else
            {
                pb.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
            }
#pragma warning restore 0618
        }


        private void refreshItemProgress(int indexToRefresh, int progress, TransferItem relevantItem, bool wasFailed, double avgSpeedBytes)
        {
            //View v = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(indexToRefresh);


            //METHOD 1 of updating... (causes flicker)
            //recyclerViewTransferItems.GetAdapter().NotifyItemChanged(indexToRefresh); //this index is the index in the transferItem list...

            //METHOD 2 of updating (no flicker)

            //so this guys adapter is good and up to date.  but anything regarding View is bogus. Including the Views TextViews and InnerTransferItems. but its Adapter is good and visually everything looks fine...

            ITransferItemView v = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(indexToRefresh) as ITransferItemView; //its doing the wrong one!!! also its a bogus view, not shown anywhere on screen...
            if (v != null) //it scrolled out of view which is find bc it will get updated when it gets rebound....
            {
                if (v is TransferItemViewFolder)
                {

                    int prog = (v.InnerTransferItem as FolderItem).GetFolderProgress(out long totalBytes, out long completedBytes);
                    v.progressBar.Progress = prog;
                    if (v.GetShowProgressSize())
                    {
                        (v.GetProgressSizeTextView() as ProgressSizeTextView).Progress = prog;
                        TransferViewHelper.SetSizeText(v.GetProgressSizeTextView(), prog, totalBytes);
                    }

                    TimeSpan? timeRemaining = null;
                    long bytesRemaining = totalBytes - completedBytes;
                    if (avgSpeedBytes != 0)
                    {
                        timeRemaining = TimeSpan.FromSeconds(bytesRemaining / avgSpeedBytes);
                    }
                    (v.InnerTransferItem as FolderItem).RemainingFolderTime = timeRemaining;
                    //TODO chain avg speeds so that its per folder rather than per transfer.

                    if (relevantItem.State.HasFlag(TransferStates.InProgress))
                    {
                        if (v.GetShowSpeed())
                        {
                            v.GetAdditionalStatusInfoView().Text = SimpleHelpers.GetTransferSpeedString(avgSpeedBytes) + "  •  " + TransferViewHelper.GetTimeRemainingString(timeRemaining);
                        }
                        else
                        {
                            v.GetAdditionalStatusInfoView().Text = TransferViewHelper.GetTimeRemainingString(timeRemaining);
                        }

                    }
                    else if (relevantItem.State.HasFlag(TransferStates.Queued) && !(relevantItem.IsUpload()))
                    {
                        int queueLen = v.InnerTransferItem.GetQueueLength();
                        if (queueLen == int.MaxValue) //unknown
                        {
                            v.GetAdditionalStatusInfoView().Text = "";
                        }
                        else
                        {
                            v.GetAdditionalStatusInfoView().Text = string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.position_), queueLen.ToString());
                        }
                    }
                    else
                    {
                        v.GetAdditionalStatusInfoView().Text = "";
                    }


                    //TransferViewHelper.SetAdditionalStatusText(v.GetAdditionalStatusInfoView(), v.InnerTransferItem, relevantItem.State);
                    if (wasFailed)
                    {
                        ClearProgressBarColor(v.progressBar);
                    }
                }
                else
                {
                    v.progressBar.Progress = progress;
                    if (v.GetShowProgressSize())
                    {
                        TransferViewHelper.SetSizeText(v.GetProgressSizeTextView(), relevantItem.Progress, relevantItem.Size);
                    }
                    TransferViewHelper.SetAdditionalStatusText(v.GetAdditionalStatusInfoView(), relevantItem, relevantItem.State, v.GetShowSpeed());
                    if (wasFailed)
                    {
                        ClearProgressBarColor(v.progressBar);
                    }
                }
            }
        }
        private void refreshListViewSpecificItem(int indexOfItem)
        {
            //creating the TransferAdapter can cause a Collection was Modified error due to transferItems.
            //maybe a better way to do this is .ToList().... rather than locking...
            //TransferAdapter customAdapter = null;
            if (Context == null)
            {
                if (SeekerState.MainActivityRef == null)
                {
                    Logger.Firebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
                    return;
                }
                //customAdapter = new TransferAdapter(SeekerState.MainActivityRef, transferItems);
            }
            else
            {
                //customAdapter = new TransferAdapter(Context, transferItems);
            }
            if (this.noTransfers == null)
            {
                Logger.Firebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            SetNoTransfersMessage();
            Logger.Debug("NotifyItemChanged" + indexOfItem);
            Logger.Debug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: ");
            Logger.Debug("UI thread: " + Looper.MainLooper.IsCurrentThread);
            if (recyclerTransferAdapter.ItemCount == indexOfItem)
            {

            }
            recyclerTransferAdapter.NotifyItemChanged(indexOfItem);

        }

        public void refreshListView(Action specificRefreshAction = null)
        {
            //creating the TransferAdapter can cause a Collection was Modified error due to transferItems.
            //maybe a better way to do this is .ToList().... rather than locking...
            //TransferAdapter customAdapter = null;
            if (Context == null)
            {
                if (SeekerState.MainActivityRef == null)
                {
                    Logger.Firebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
                    return;
                }
            }
            else
            {
            }
            if (this.noTransfers == null)
            {
                Logger.Firebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            SetNoTransfersMessage();
            if (specificRefreshAction == null)
            {
                recyclerTransferAdapter.NotifyDataSetChanged();
            }
            else
            {
                specificRefreshAction();
            }
        }

        //public string const IndividualItemType = 1;
        //public string const FolderItemType = 2;


        private void TransferProgressUpdated(object sender, SeekerApplication.ProgressUpdatedUIEventArgs e)
        {
            bool needsRefresh = (e.ti.IsUpload() && ViewState.InUploadsMode) || (!(e.ti.IsUpload()) && !(ViewState.InUploadsMode));
            if (!needsRefresh)
            {
                return;
            }
            if (e.percentComplete != 0)
            {
                if (e.fullRefresh)
                {

                    Action action = refreshListViewSafe; //notify data set changed...
                                                         //if (indexRemoved!=-1)
                                                         //{

                    //    var refreshOnlySelected = new Action(() => {

                    //        Logger.Debug("notifyItemRemoved " + indexRemoved + "count: " + recyclerTransferAdapter.ItemCount);
                    //        if(indexRemoved == recyclerTransferAdapter.ItemCount)
                    //        {

                    //        }
                    //        recyclerTransferAdapter?.NotifyItemRemoved(indexRemoved);


                    //    });

                    //    var refresh1 = new Action(()  => refreshListView(refreshOnlySelected) );

                    //    action = refreshOnlySelected;
                    //}
                    //else
                    //{
                    //    action = refreshListViewSafe;
                    //}
                    Activity?.RunOnUiThread(action); //in case of rotation it is the ACTIVITY which will be null!!!!
                }
                else
                {
                    try
                    {
                        bool isNew = !ProgressUpdatedThrottler.ContainsKey(e.ti.FullFilename + e.ti.Username);

                        DateTime now = DateTime.UtcNow;
                        DateTime lastUpdated = ProgressUpdatedThrottler.GetOrAdd(e.ti.FullFilename + e.ti.Username, now); //this returns now if the key is not in the dictionary!
                        if (now.Subtract(lastUpdated).TotalMilliseconds > THROTTLE_PROGRESS_UPDATED_RATE || isNew)
                        {
                            ProgressUpdatedThrottler[e.ti.FullFilename + e.ti.Username] = now;
                        }
                        else if (e.wasFailed)
                        {
                            //still update..
                        }
                        else
                        {
                            //there was a bug where there were multiple instances of tabspageradapter and one would always get their event handler before the other
                            //basically updating a recyclerview that wasnt even visible, while the other was never getting to update due to the throttler.
                            //this is fixed by attaching and dettaching the event handlers on start / stop.
                            return;
                        }


                        //partial refresh just update progress..
                        //TransferItemManagerDL.GetTransferItemWithIndexFromAll(e.ti.FullFilename, out index);

                        //Logger.Debug("Index is "+index+" TransferProgressUpdated"); //tested!

                        //int indexToUpdate = transferItems.IndexOf(relevantItem);

                        Activity?.RunOnUiThread(() =>
                        {
                            int index = -1;
                            index = TransferItemManagerWrapped.GetUserIndexForTransferItem(e.ti);
                            if (index == -1)
                            {
                                Logger.Debug("Index is -1 TransferProgressUpdated");
                                return;
                            }
                            Logger.Debug("UI THREAD TRANSFER PROGRESS UPDATED"); //this happens every 20ms.  so less often then tranfer progress updated.  usually 6 of those can happen before 2 of these.
                            refreshItemProgress(index, e.ti.Progress, e.ti, e.wasFailed, e.avgspeedBytes);

                        });



                    }
                    catch (System.Exception error)
                    {
                        Logger.Firebase(error.Message + " partial update");
                    }
                }
            }
        }

        private void TransferStateChangedItem(object sender, TransferItem ti)
        {
            Action action = new Action(() =>
            {

                int index = TransferItemManagerWrapped.GetUserIndexForTransferItem(ti); //todo null ti
                if (index == -1)
                {
                    return; //this is likely an upload when we are on downloads page or vice versa.
                }
                refreshListViewSpecificItem(index);

            });
            SeekerState.MainActivityRef.RunOnUiThread(action);
        }

        private void TransferStateChanged(object sender, int index)
        {
            Action action = new Action(() =>
            {

                refreshListViewSpecificItem(index);

            });
            SeekerState.MainActivityRef.RunOnUiThread(action);
        }


        public override void OnStart()
        {
            SeekerApplication.StateChangedAtIndex += TransferStateChanged;
            SeekerApplication.StateChangedForItem += TransferStateChangedItem;
            SeekerApplication.ProgressUpdated += TransferProgressUpdated;
            MainActivity.TransferAddedUINotify += MainActivity_TransferAddedUINotify; ; //todo this should eventually be for downloads too.
            DownloadService.TransferItemQueueUpdated += TranferQueueStateChanged;

            if (recyclerTransferAdapter != null)
            {
                recyclerTransferAdapter.NotifyDataSetChanged();
            }

            base.OnStart();
        }

        private void MainActivity_TransferAddedUINotify(object sender, TransferItem e)
        {
            if (MainActivity.OnUIthread())
            {
                if (e.IsUpload() && ViewState.InUploadsMode)
                {
                    lock (TransferItemManagerWrapped.GetUICurrentList())
                    { //todo can update this to do a partial refresh... just the index..
                        if (ViewState.GroupByFolder && !ViewState.CurrentlyInFolder())
                        {
                            //folderview - so we may insert or update
                            //int index = TransferItemManagerWrapped.GetUserIndexForTransferItem(e);
                            refreshListView(); //just to be safe...

                        }
                        else
                        {
                            //int index = TransferItemManagerWrapped.GetUserIndexForTransferItem(e);
                            //refreshListView(()=>{recyclerTransferAdapter.NotifyItemInserted(index); });
                            refreshListView(); //just to be safe...
                        }

                    }
                }
            }
            else
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() => { MainActivity_TransferAddedUINotify(null, e); });
            }
        }

        public override void OnStop()
        {
            SeekerApplication.StateChangedAtIndex -= TransferStateChanged;
            SeekerApplication.ProgressUpdated -= TransferProgressUpdated;
            SeekerApplication.StateChangedForItem -= TransferStateChangedItem;
            DownloadService.TransferItemQueueUpdated -= TranferQueueStateChanged;
            MainActivity.TransferAddedUINotify -= MainActivity_TransferAddedUINotify;
            base.OnStop();
        }

        //NOTE: there can be several TransfersFragment at a time.
        //this can be done by having many MainActivitys in the back stack
        //i.e. go to UserList > press browse files > go to UserList > press browse files --> 3 TransferFragments, 2 of which are stopped but not Destroyed.
        public override void OnCreate(Bundle savedInstanceState)
        {

            MainActivity.ClearDownloadAddedEventsFromTarget(this);
            MainActivity.DownloadAddedUINotify += SeekerState_DownloadAddedUINotify;
            //todo I dont think this should be here.  I think the only reason its not causing a problem is because the user cannot add a download from the transfer page.
            //if they could then the download might not show because this is OnCreate!! so it will only update the last one you created.  
            //so you can create a second one, back out of it, and the first one will not get recreated and so it will not have an event. 


            base.OnCreate(savedInstanceState);
        }


        private void SeekerState_DownloadAddedUINotify(object sender, DownloadAddedEventArgs e)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                //occurs on nonUI thread...
                //if there is any deadlock due to this, then do Thread.Start().
                lock (TransferItemManagerDL.GetUICurrentList(ViewState.CreateDLUIState()))
                { //todo can update this to do a partial refresh... just the index..
                    refreshListView();
                }
            });
        }

    }

}