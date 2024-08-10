using Android.Content;
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

namespace Seeker
{
    public class TransfersFragment : Fragment, PopupMenu.IOnMenuItemClickListener
    {
        private View rootView = null;

        /// <summary>
        /// We add these to this dict so we can (1) AddUser to get their statuses and (2) efficiently check them when
        /// we get user status changed events.
        /// This dict may contain a greater number of users than strictly necessary.  as it is just used to save time, 
        /// and having additional users here will not cause any issues.  (i.e. in case where other user was offline then the 
        /// user cleared that download, no harm as it will check and see there are no downloads to retry and just remove user)
        /// </summary>
        public static Dictionary<string, byte> UsersWhereDownloadFailedDueToOffline = new Dictionary<string, byte>();
        public static void AddToUserOffline(string username)
        {
            if (UsersWhereDownloadFailedDueToOffline.ContainsKey(username))
            {
                return;
            }
            else
            {
                lock (TransfersFragment.UsersWhereDownloadFailedDueToOffline)
                {
                    UsersWhereDownloadFailedDueToOffline[username] = 0x0;
                }
                try
                {
                    SeekerState.SoulseekClient.AddUserAsync(username);
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

        public static bool GroupByFolder = false;

        public static int ScrollPositionBeforeMovingIntoFolder = int.MinValue;
        public static int ScrollOffsetBeforeMovingIntoFolder = int.MinValue;
        public static FolderItem CurrentlySelectedDLFolder = null;
        public static FolderItem CurrentlySelectedUploadFolder = null;
        public static bool CurrentlyInFolder()
        {
            if (CurrentlySelectedDLFolder == null && CurrentlySelectedUploadFolder == null)
            {
                return false;
            }
            return true;
        }
        public static FolderItem GetCurrentlySelectedFolder()
        {
            if (InUploadsMode)
            {
                return CurrentlySelectedUploadFolder;
            }
            else
            {
                return CurrentlySelectedDLFolder;
            }
        }
        public static volatile bool InUploadsMode = false;

        //private ListView primaryListView = null;
        private TextView noTransfers = null;
        private Button setupUpSharing = null;
        private ISharedPreferences sharedPreferences = null;
        private static System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> ProgressUpdatedThrottler = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
        public static int THROTTLE_PROGRESS_UPDATED_RATE = 200;//in ms;

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
            if (InUploadsMode)
            {
                menu.FindItem(Resource.Id.action_clear_all_complete_and_aborted).SetVisible(true);
                menu.FindItem(Resource.Id.action_abort_all).SetVisible(true);
                if (CurrentlySelectedUploadFolder == null)
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
                if (CurrentlySelectedDLFolder == null)
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

                    TransferViewHelper.GetStatusNumbers(CurrentlySelectedDLFolder.TransferItems, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued);
                    //var state = CurrentlySelectedDLFolder.GetState(out bool isFailed);

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

            if (SeekerState.TransferViewShowSizes)
            {
                menu.FindItem(Resource.Id.action_show_size).SetTitle(Resource.String.HideSize);
            }
            else
            {
                menu.FindItem(Resource.Id.action_show_size).SetTitle(Resource.String.ShowSize);
            }

            if (SeekerState.TransferViewShowSpeed)
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
                    SeekerState.MainActivityRef.OnBackPressed();
                    return true;
                case Resource.Id.action_clear_all_complete: //clear all complete
                    MainActivity.LogInfoFirebase("Clear All Complete Pressed");
                    if (CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.ClearAllComplete();
                    }
                    else
                    {
                        TransferItemManagerDL.ClearAllCompleteFromFolder(CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_toggle_group_by: //toggle group by. group / ungroup by folder.
                    GroupByFolder = !GroupByFolder;
                    SetRecyclerAdapter();
                    return true;
                case Resource.Id.action_show_size:
                    SeekerState.TransferViewShowSizes = !SeekerState.TransferViewShowSizes;
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_show_speed:
                    SeekerState.TransferViewShowSpeed = !SeekerState.TransferViewShowSpeed;
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_clear_all_complete_and_aborted:
                    MainActivity.LogInfoFirebase("Clear All Complete Pressed");
                    if (CurrentlySelectedUploadFolder == null)
                    {
                        TransferItemManagerUploads.ClearAllComplete();
                    }
                    else
                    {
                        TransferItemManagerUploads.ClearAllCompleteFromFolder(CurrentlySelectedUploadFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_toggle_download_upload:
                    InUploadsMode = !InUploadsMode;
                    RefreshForModeSwitch();
                    return true;
                case Resource.Id.action_cancel_and_clear_all: //cancel and clear all
                    MainActivity.LogInfoFirebase("action_cancel_and_clear_all Pressed");
                    SeekerState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.CancelAll(true);
                        TransferItemManagerDL.ClearAllAndClean();
                    }
                    else
                    {
                        TransferItemManagerDL.CancelFolder(CurrentlySelectedDLFolder, true);
                        TransferItemManagerDL.ClearAllFromFolderAndClean(CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_abort_all: //abort all
                    MainActivity.LogInfoFirebase("action_abort_all_pressed");
                    SeekerState.AbortAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (CurrentlySelectedUploadFolder == null)
                    {
                        TransferItemManagerUploads.CancelAll();
                    }
                    else
                    {
                        TransferItemManagerUploads.CancelFolder(CurrentlySelectedUploadFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_pause_all:
                    MainActivity.LogInfoFirebase("pause all Pressed");
                    SeekerState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.CancelAll();
                    }
                    else
                    {
                        TransferItemManagerDL.CancelFolder(CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_resume_all:
                    MainActivity.LogInfoFirebase("resume all Pressed");
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
                            if (CurrentlySelectedDLFolder == null)
                            {
                                SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(false, true, null, false); });
                            }
                            else
                            {
                                DownloadRetryAllConditionLogic(false, false, CurrentlySelectedDLFolder, false);
                            }
                        }));
                    }
                    else
                    {
                        if (CurrentlySelectedDLFolder == null)
                        {
                            DownloadRetryAllConditionLogic(false, true, null, false);
                        }
                        else
                        {
                            DownloadRetryAllConditionLogic(false, false, CurrentlySelectedDLFolder, false);
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
            MainActivity.LogInfoFirebase("retry all failed Pressed batch? " + batchSelectedOnly);
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
                    if (CurrentlySelectedDLFolder == null)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(failed, true, null, batchSelectedOnly, listTi); });
                    }
                    else
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(failed, false, CurrentlySelectedDLFolder, batchSelectedOnly, listTi); });
                    }
                }));
            }
            else
            {
                if (CurrentlySelectedDLFolder == null)
                {
                    DownloadRetryAllConditionLogic(failed, true, null, batchSelectedOnly, listTi);
                }
                else
                {
                    DownloadRetryAllConditionLogic(failed, false, CurrentlySelectedDLFolder, batchSelectedOnly, listTi);
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
                    TransfersFragment.UsersWhereDownloadFailedDueToOffline[ti.Username] = 0x0;
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
            if (TransfersFragment.InUploadsMode)
            {
                if (!(TransferItemManagerUploads.IsEmpty()))
                {
                    noTransfers.Visibility = ViewStates.Gone;
                    setupUpSharing.Visibility = ViewStates.Gone;
                }
                else
                {
                    noTransfers.Visibility = ViewStates.Visible;
                    if (MainActivity.MeetsSharingConditions())
                    {
                        noTransfers.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads_yet);
                        setupUpSharing.Visibility = ViewStates.Gone;
                    }
                    else if(SeekerState.SharingOn && UploadDirectoryManager.UploadDirectories.Count != 0 && UploadDirectoryManager.AreAllFailed())
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
            MainActivity.LogDebug("TransfersFragment OnCreateView");
            if (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
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
            //if (savedInstanceState != null)
            //{
            //    // Restore saved layout manager type.
            //    mCurrentLayoutManagerType = (LayoutManagerType)savedInstanceState
            //            .getSerializable(KEY_LAYOUT_MANAGER);
            //}

            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);

            //// If a layout manager has already been set, get current scroll position.
            //if (mRecyclerView.getLayoutManager() != null)
            //{
            //    scrollPosition = ((LinearLayoutManager)mRecyclerView.getLayoutManager())
            //            .findFirstCompletelyVisibleItemPosition();
            //}
            //recyclerViewTransferItems.ScrollToPosition()

            //// If a layout manager has already been set, get current scroll position.
            //if (mRecyclerView.getLayoutManager() != null)
            //{
            //    scrollPosition = ((LinearLayoutManager)mRecyclerView.getLayoutManager())
            //            .findFirstCompletelyVisibleItemPosition();
            //}



            MainActivity.LogInfoFirebase("AutoClear: " + SeekerState.AutoClearCompleteDownloads);
            MainActivity.LogInfoFirebase("AutoRetry: " + SeekerState.AutoRetryDownload);

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
            ScrollPositionBeforeMovingIntoFolder = ((LinearLayoutManager)recycleLayoutManager).FindFirstVisibleItemPosition();
            View v = recyclerViewTransferItems.GetChildAt(0);
            if (v == null)
            {
                ScrollOffsetBeforeMovingIntoFolder = 0;
            }
            else
            {
                ScrollOffsetBeforeMovingIntoFolder = v.Top - recyclerViewTransferItems.Top;
            }
        }

        public void RestoreScrollPosition()
        {
            ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(ScrollPositionBeforeMovingIntoFolder, ScrollOffsetBeforeMovingIntoFolder); //if you dont do with offset, it scrolls it until the visible item is simply in view (so it will be at bottom, almost a whole screen off)
        }

        public static ActionModeCallback TransfersActionModeCallback = null;
        //public static Android.Support.V7.View.ActionMode TransfersActionMode = null;
        public static ActionMode TransfersActionMode = null;
        public static List<int> BatchSelectedItems = new List<int>();

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


                if (GroupByFolder && !CurrentlyInFolder())
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
                MainActivity.LogFirebase(error.Message + " POPUP BAD ERROR");
            }
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            return false;
            //switch (item.ItemId)
            //{
            //    case Resource.Id.clearAllComplete:
            //        //TransferItem[] tempArry = new TransferItem[transferItems.Count]();
            //        //transferItems.CopyTo(tempArry);
            //        lock (transferItems)
            //        {
            //            //Occurs on UI thread.
            //            transferItems.RemoveAll((TransferItem i) => { return i.Progress > 99; });
            //            //for (int i=0; i< tempArry.Count;i++)
            //            //{
            //            //    if(tempArry[i].Progress>99)
            //            //    {
            //            //        transferItems.Remove(tempArry[i]);
            //            //    }
            //            //}
            //            refreshListView();
            //        }
            //        return true;
            //    case Resource.Id.cancelAndClearAll:
            //        lock (transferItems)
            //        {
            //            SeekerState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            //            for (int i = 0; i < transferItems.Count; i++)
            //            {
            //                //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
            //                CancellationTokens.TryGetValue(ProduceCancellationTokenKey(transferItems[i]), out CancellationTokenSource token);
            //                token?.Cancel();
            //                //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
            //            }
            //            CancellationTokens.Clear();
            //            transferItems.Clear();
            //            refreshListView();
            //        }
            //        return true;
            //    //case Resource.Id.openDownloadsDir:
            //    //    Intent chooser = new Intent(Intent.ActionView); //not get content
            //    //    //actionview - no apps can perform this action.
            //    //    //chooser.AddCategory(Intent.CategoryOpenable);

            //    //    if (SeekerState.UseLegacyStorage())
            //    //    {
            //    //        if (SeekerState.SaveDataDirectoryUri == null || SeekerState.SaveDataDirectoryUri == string.Empty)
            //    //        {
            //    //            string rootDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
            //    //            chooser.SetDataAndType(Android.Net.Uri.Parse(rootDir), "*/*");
            //    //        }
            //    //        else
            //    //        {
            //    //            chooser.SetDataAndType(Android.Net.Uri.Parse(SeekerState.SaveDataDirectoryUri), "*/*");
            //    //        }
            //    //    }
            //    //    else
            //    //    {
            //    //        if (SeekerState.SaveDataDirectoryUri==null || SeekerState.SaveDataDirectoryUri==string.Empty)
            //    //        {
            //    //            Toast tst = Toast.MakeText(Context, "Download Directory is not set.  Please set it to enable downloading.", ToastLength.Short);
            //    //            tst.Show();
            //    //            return true;
            //    //        }

            //    //        chooser.SetData(Android.Net.Uri.Parse(SeekerState.SaveDataDirectoryUri));
            //    //    }

            //    //    try
            //    //    {
            //    //        StartActivity(Intent.CreateChooser(chooser,"Download Directory"));
            //    //    }
            //    //    catch
            //    //    {
            //    //        Toast tst = Toast.MakeText(Context, "No compatible File Browser app installed", ToastLength.Short);
            //    //        tst.Show();
            //    //    }
            //    //    return true;
            //    default:
            //        return false;

            //}
        }


        //public override void OnDestroyView()
        //{
        //    try
        //    {
        //        SeekerState.SoulseekClient.TransferProgressUpdated -= SoulseekClient_TransferProgressUpdated;
        //        SeekerState.SoulseekClient.TransferStateChanged -= SoulseekClient_TransferStateChanged;
        //        MainActivity.TransferItemQueueUpdated -= TranferQueueStateChanged;
        //    }
        //    catch (System.Exception)
        //    {

        //    }
        //    base.OnDestroyView();
        //}

        //public override void OnDestroy()
        //{
        //    //SeekerState.DownloadAdded -= SeekerState_DownloadAdded;
        //    //SeekerState.SoulseekClient.TransferProgressUpdated -= SoulseekClient_TransferProgressUpdated;
        //    //SeekerState.SoulseekClient.TransferStateChanged -= SoulseekClient_TransferStateChanged;
        //    base.OnDestroy();
        //}

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
            InUploadsMode = true;
            CurrentlySelectedDLFolder = null;
            CurrentlySelectedUploadFolder = null;
            this.RefreshForModeSwitch();
            SeekerState.MainActivityRef.InvalidateOptionsMenu();
        }

        public override void OnPause()
        {
            base.OnPause();
            MainActivity.LogDebug("TransferFragment OnPause");  //this occurs when we move to the Account Tab or if we press the home button (i.e. to later kill the process)
                                                                //so this is a good place to do it.
            SaveTransferItems(sharedPreferences);
        }

        public static object TransferStateSaveLock = new object();

        public static void SaveTransferItems(ISharedPreferences sharedPreferences, bool force = true, int maxSecondsUpdate = 0)
        {
            MainActivity.LogDebug("---- saving transfer items enter ----");
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            sw.Start();
#endif

            if (force || (SeekerApplication.TransfersDownloadsCompleteStale && DateTime.UtcNow.Subtract(SeekerApplication.TransfersLastSavedTime).TotalSeconds > maxSecondsUpdate)) //stale and we havent updated too recently..
            {
                MainActivity.LogDebug("---- saving transfer items actual save ----");
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
                lock (MainActivity.SHARED_PREF_LOCK)
                    lock (TransferStateSaveLock)
                    {
                        var editor = sharedPreferences.Edit();
                        editor.PutString(KeyConsts.M_TransferList, listOfDownloadItems);
                        editor.PutString(KeyConsts.M_TransferListUpload, listOfUploadItems);
                        editor.Commit();
                    }

                SeekerApplication.TransfersDownloadsCompleteStale = false;
                SeekerApplication.TransfersLastSavedTime = DateTime.UtcNow;
            }

#if DEBUG
            sw.Stop();
            MainActivity.LogDebug("saving time: " + sw.ElapsedMilliseconds);
#endif


        }

        private List<TransferItem> GetBatchSelectedItemsForRetryCondition(bool selectFailed)
        {
            bool folderItems = false;
            if (GroupByFolder && !CurrentlyInFolder())
            {
                folderItems = true;
            }
            List<TransferItem> tis = new List<TransferItem>();
            foreach (int pos in BatchSelectedItems)
            {
                if (folderItems)
                {
                    var fi = TransferItemManagerDL.GetItemAtUserIndex(pos) as FolderItem;
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
                    var ti = TransferItemManagerDL.GetItemAtUserIndex(pos) as TransferItem;
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
                    Android.Net.Uri incompleteUri = null;
                    SetupCancellationToken(item, cancellationTokenSource, out _);
                    Task task = TransfersUtil.DownloadFileAsync(item.Username, item.FullFilename, item.GetSizeForDL(), cancellationTokenSource, out _, isFileDecodedLegacy: item.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: item.ShouldEncodeFolderLatin1());
                    task.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item.Username, item.FullFilename, item.Size, task, cancellationTokenSource, item.QueueLength, 0, item.GetDirectoryLevel()) { TransferItemReference = item })));
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
                        MainActivity.LogFirebase(error.Message + " OnContextItemSelected");
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

                        HashSet<int> indicesToUpdate = new HashSet<int>();
                        foreach (TransferItem ti in transferItemConditionList)
                        {
                            int pos = TransferItemManagerDL.GetUserIndexForTransferItem(ti);
                            if (pos == -1)
                            {
                                MainActivity.LogDebug("pos == -1!!");
                                continue;
                            }

                            if (indicesToUpdate.Contains(pos))
                            {
                                //this is for if we are in a folder.  since previously we would update a folder of 10 items, 10 times which looked quite glitchy...
                                MainActivity.LogDebug($"skipping same pos {pos}");
                            }
                            else
                            {
                                indicesToUpdate.Add(pos);
                            }
                        }
                        if (InUploadsMode)
                        {
                            return;
                        }
                        foreach (int i in indicesToUpdate)
                        {
                            MainActivity.LogDebug($"updating {i}");
                            if (StaticHacks.TransfersFrag != null)
                            {
                                StaticHacks.TransfersFrag.recyclerTransferAdapter?.NotifyItemChanged(i);
                            }
                        }



                    });
            });
            lock (TransferItemManagerDL.GetUICurrentList()) //TODO: test
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
            //MainActivity.LogDebug("targetView is null? " + (targetView == null).ToString());
            //if (targetView == null)
            //{
            //    SeekerApplication.ShowToast(SeekerState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
            //    return;
            //}
            string chosenFname = (transferItem as TransferItem).FullFilename; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            string chosenUname = (transferItem as TransferItem).Username; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            MainActivity.LogDebug("chosenFname? " + chosenFname);
            TransferItem item1 = TransferItemManagerDL.GetTransferItemWithIndexFromAll(chosenFname, chosenUname, out int _);
            int indexToRefresh = TransferItemManagerDL.GetUserIndexForTransferItem(item1);
            MainActivity.LogDebug("item1 is null?" + (item1 == null).ToString());//tested
            if (item1 == null || indexToRefresh == -1)
            {
                SeekerApplication.ShowToast(SeekerState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
                return;
            }

            //int tokenNum = int.MinValue;
            if (SeekerState.SoulseekClient.IsTransferInDownloads(item1.Username, item1.FullFilename/*, out tokenNum*/))
            {
                MainActivity.LogDebug("transfer is in Downloads !!! " + item1.FullFilename);
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
                    MainActivity.LogFirebase("CTS is null. this should not happen. we should always set it before downloading.");
                }
                return; //the dl continuation method will take care of it....
            }

            //TransferItem item1 = transferItems[info.Position];  
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {

                Android.Net.Uri incompleteUri = null;
                SetupCancellationToken(item1, cancellationTokenSource, out _);
                Task task = TransfersUtil.DownloadFileAsync(item1.Username, item1.FullFilename, item1.GetSizeForDL(), cancellationTokenSource, out _, isFileDecodedLegacy: item1.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: item1.ShouldEncodeFolderLatin1());
                //SeekerState.SoulseekClient.DownloadAsync(
                //username: item1.Username,
                //filename: item1.FullFilename,
                //size: item1.Size,
                //cancellationToken: cancellationTokenSource.Token);
                task.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username, item1.FullFilename, item1.Size, task, cancellationTokenSource, item1.QueueLength, item1.Failed ? 1 : 0, item1.GetDirectoryLevel()) { TransferItemReference = item1 }))); //if paused do retry counter 0.
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
                    MainActivity.LogFirebase(error.Message + " OnContextItemSelected");
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

                MainActivity.LogDebug("notifyItemChanged " + position);

                recyclerTransferAdapter.NotifyItemChanged(position);


            });
            lock (TransferItemManagerDL.GetUICurrentList())
            { //also can update this to do a partial refresh...
                refreshListView(refreshOnlySelected);
            }
        }

        private bool NotLoggedInShowMessageGaurd(string msg)
        {
            if (!SeekerState.currentlyLoggedIn)
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
                    MainActivity.LogInfoFirebase("recyclerTransferAdapter is null");
                }

                ITransferItem ti = recyclerTransferAdapter.getSelectedItem();

                if (ti == null)
                {
                    MainActivity.LogInfoFirebase("ti is null");
                }

                int position = TransferItemManagerWrapped.GetUserIndexForITransferItem(ti);


                if (position == -1)
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                    return base.OnContextItemSelected(item);
                }

                if (CommonHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), ti.GetUsername(), SeekerState.ActiveActivityRef, this.View))
                {
                    MainActivity.LogDebug("handled by commons");
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
                        MainActivity.LogInfoFirebase("Clear Complete item pressed");
                        lock (TransferItemManagerWrapped.GetUICurrentList()) //TODO: test
                        {
                            try
                            {
                                if (InUploadsMode)
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
                                //MainActivity.LogFirebase("case1: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                                Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                                return base.OnContextItemSelected(item);
                            }
                            recyclerTransferAdapter.NotifyItemRemoved(position);  //UI
                            //refreshListView();
                        }
                        break;
                    case 2: //cancel and clear (downloads) OR abort and clear (uploads)
                        //info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
                        MainActivity.LogInfoFirebase("Cancel and Clear item pressed");
                        ITransferItem tItem = null;
                        try
                        {
                            tItem = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case2: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        if (tItem is TransferItem tti)
                        {
                            bool wasInProgress = tti.State.HasFlag(TransferStates.InProgress);
                            CancellationTokens.TryGetValue(ProduceCancellationTokenKey(tti), out CancellationTokenSource uptoken);
                            uptoken?.Cancel();
                            //CancellationTokens[ProduceCancellationTokenKey(tItem)]?.Cancel(); throws if does not exist.
                            CancellationTokens.Remove(ProduceCancellationTokenKey(tti), out _);
                            lock (TransferItemManagerWrapped.GetUICurrentList())
                            {
                                if (InUploadsMode)
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
                            MainActivity.LogInfoFirebase("Cancel and Clear item pressed - bad item");
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
                            tItem = TransferItemManagerDL.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case3: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
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
                            tItem = TransferItemManagerDL.GetItemAtUserIndex(position) as TransferItem;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case4: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        try
                        {
                            //tested on API25 and API30
                            //AndroidX.Core.Content.FileProvider
                            Android.Net.Uri uriToUse = null;
                            if (SeekerState.UseLegacyStorage() && CommonHelpers.IsFileUri((tItem as TransferItem).FinalUri)) //i.e. if it is a FILE URI.
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
                            MainActivity.LogFirebase(e.Message + e.StackTrace);
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
                            string startingDir = CommonHelpers.GetDirectoryRequestFolderName(ttti.FullFilename);
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
                            string startingDir = CommonHelpers.GetDirectoryRequestFolderName(fi.TransferItems[0].FullFilename);
                            for (int i = 0; i < fi.GetDirectoryLevel() - 1; i++)
                            {
                                startingDir = CommonHelpers.GetDirectoryRequestFolderName(startingDir); //keep going up..
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
                        MainActivity.LogInfoFirebase("resume folder Pressed");
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
                        MainActivity.LogInfoFirebase("retry folder Pressed");
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
                        MainActivity.LogInfoFirebase("Abort Upload item pressed");
                        tItem = null;
                        try
                        {
                            tItem = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case2: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SeekerState.ActiveActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        TransferItem uploadToCancel = tItem as TransferItem;

                        CancellationTokens.TryGetValue(ProduceCancellationTokenKey(uploadToCancel), out CancellationTokenSource token);
                        token?.Cancel();
                        //CancellationTokens[ProduceCancellationTokenKey(tItem)]?.Cancel(); throws if does not exist.
                        CancellationTokens.Remove(ProduceCancellationTokenKey(uploadToCancel), out _);
                        lock (TransferItemManagerWrapped.GetUICurrentList())
                        {
                            recyclerTransferAdapter.NotifyItemChanged(position);
                        }
                        break;
                    case 104: //ignore (unshare) user
                        MainActivity.LogInfoFirebase("Unshare User item pressed");
                        IEnumerable<TransferItem> tItems = TransferItemManagerWrapped.GetTransferItemsForUser(ti.GetUsername());
                        foreach (var tiToCancel in tItems)
                        {
                            CancellationTokens.TryGetValue(ProduceCancellationTokenKey(tiToCancel), out CancellationTokenSource token1);
                            token1?.Cancel();
                            CancellationTokens.Remove(ProduceCancellationTokenKey(tiToCancel), out _);
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
            if (BatchSelectedItems.Contains(pos))
            {
                BatchSelectedItems.Remove(pos);
            }
            else
            {
                BatchSelectedItems.Add(pos);
            }
            recyclerTransferAdapter.NotifyItemChanged(pos);
            int cnt = BatchSelectedItems.Count;
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
                MainActivity.LogDebug("batch on, different screen item removed");
                return;
            }
            MainActivity.LogDebug("batch on, updating: " + userPostionBeingRemoved);
            //adjust numbers
            int cnt = BatchSelectedItems.Count;
            for (int i = cnt - 1; i >= 0; i--)
            {
                int position = BatchSelectedItems[i];
                if (position < userPostionBeingRemoved)
                {
                    continue;
                }
                else if (position == userPostionBeingRemoved)
                {
                    BatchSelectedItems.RemoveAt(i);
                }
                else
                {
                    BatchSelectedItems[i] = position - 1;
                }
            }
            //if there was only 1 and its the one that just finished then take us out of batchSelectedItems
            if (BatchSelectedItems.Count == 0)
            {
                TransfersActionMode.Finish();
            }
            else if (BatchSelectedItems.Count != cnt) //if we have 1 less now.
            {
                TransfersActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), BatchSelectedItems.Count.ToString());
                TransfersActionMode.Invalidate();
            }
        }

        public class ActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            public TransferAdapterRecyclerVersion Adapter;
            public TransfersFragment Frag;
            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.transfers_menu_batch, menu);
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                if (BatchSelectedItems.Count == 0)
                {
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all_batch).SetVisible(false);
                }
                else
                {
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all_batch).SetVisible(true);
                }


                if (TransfersFragment.InUploadsMode)
                {
                    //the only thing you can do is clear and abort the selected
                    menu.FindItem(Resource.Id.resume_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.pause_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed_batch).SetVisible(false);
                    return false;
                }
                else
                {
                    menu.FindItem(Resource.Id.resume_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.pause_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed_batch).SetVisible(false);

                    TransferStates transferStates = TransferStates.None;
                    bool failed = false;
                    List<TransferItem> transfersSelected = new List<TransferItem>();
                    foreach (int position in BatchSelectedItems)
                    {
                        var ti = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        if (ti is TransferItem singleTi)
                        {
                            transfersSelected.Add(singleTi);
                        }
                        else if (ti is FolderItem folderTi)
                        {
                            transfersSelected.AddRange(folderTi.TransferItems);
                        }
                    }
                    TransferViewHelper.GetStatusNumbers(transfersSelected, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued);

                    if (numPaused != 0)
                    {
                        menu.FindItem(Resource.Id.resume_selected_batch).SetVisible(true);
                    }
                    if (numInProgress != 0 || numQueued != 0)
                    {
                        menu.FindItem(Resource.Id.pause_selected_batch).SetVisible(true);
                    }
                    if (numFailed != 0)
                    {
                        menu.FindItem(Resource.Id.retry_all_failed_batch).SetVisible(true);
                    }

                    //clear all complete??

                }
                return false;
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                switch (item.ItemId)
                {
                    //this is the only option that uploads gets
                    case Resource.Id.action_cancel_and_clear_all_batch:
                        MainActivity.LogInfoFirebase("action_cancel_and_clear_batch Pressed");
                        SeekerState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        TransferItemManagerWrapped.CancelSelectedItems(true);
                        TransferItemManagerWrapped.ClearSelectedItemsAndClean();
                        var selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach (int pos in selected)
                        {
                            Adapter.NotifyItemRemoved(pos);
                        }
                        //since all selected stuff is going away. its what Gmail action mode does.
                        TransfersActionMode.Finish(); //TransfersActionMode can be null!
                        break;
                    case Resource.Id.pause_selected_batch:
                        TransferItemManagerWrapped.CancelSelectedItems(false);
                        selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach (int pos in selected)
                        {
                            Adapter.NotifyItemChanged(pos);
                        }
                        //since all selected stuff is going away. its what Gmail action mode does.
                        TransfersActionMode.Finish();
                        break;
                    case Resource.Id.resume_selected_batch:
                        Frag.RetryAllConditionEntry(false, true);
                        selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach (int pos in selected)
                        {
                            Adapter.NotifyItemChanged(pos);
                        }
                        TransfersActionMode.Finish();
                        break;
                    case Resource.Id.retry_all_failed_batch:
                        Frag.RetryAllConditionEntry(true, true);
                        selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach (int pos in selected)
                        {
                            Adapter.NotifyItemChanged(pos);
                        }
                        TransfersActionMode.Finish();
                        break;
                    case Resource.Id.select_all:
                        BatchSelectedItems.Clear();
                        int cnt = TransfersActionModeCallback.Adapter.ItemCount;
                        for (int i = 0; i < cnt; i++)
                        {
                            BatchSelectedItems.Add(i);
                        }

                        TransfersActionModeCallback.Adapter.NotifyDataSetChanged();

                        TransfersActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                        TransfersActionMode.Invalidate();
                        return true;
                    case Resource.Id.invert_selection:
                        ForceOutIfZeroSelected = false;
                        List<int> oldOnes = BatchSelectedItems.ToList();
                        BatchSelectedItems.Clear();
                        List<int> all = new List<int>();
                        int cnt1 = TransfersActionModeCallback.Adapter.ItemCount;
                        for (int i = 0; i < cnt1; i++)
                        {
                            all.Add(i);
                        }
                        BatchSelectedItems = all.Except(oldOnes).ToList();

                        TransfersActionModeCallback.Adapter.NotifyDataSetChanged();

                        TransfersActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), BatchSelectedItems.Count.ToString());
                        TransfersActionMode.Invalidate();
                        return true;
                }
                return true;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {

                int[] prevSelectedItems = new int[BatchSelectedItems.Count];
                BatchSelectedItems.CopyTo(prevSelectedItems);
                TransfersActionMode = null;
                BatchSelectedItems.Clear();
                this.Adapter.IsInBatchSelectMode = false;
                foreach (int i in prevSelectedItems)
                {
                    this.Adapter.NotifyItemChanged(i);
                }

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
                        int indexOfItem = TransferItemManagerDL.GetUserIndexForTransferItem(t);
                        if (indexOfItem == -1 && InUploadsMode)
                        {
                            return null;
                        }
                        recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
                    }
                }
                catch (System.Exception e)
                {
                    MainActivity.LogFirebase("actionOnComplete" + e.Message + e.StackTrace);
                }

                return null;
            });

            MainActivity.GetDownloadPlaceInQueue(ttItem.Username, ttItem.FullFilename, true, false, ttItem, actionOnComplete);
        }

        public void UpdateQueueState(string fullFilename) //Add this to the event handlers so that when downloads are added they have their queue position.
        {
            try
            {
                if (InUploadsMode)
                {
                    return;
                }
                int indexOfItem = TransferItemManagerDL.GetUserIndexForTransferItem(fullFilename);
                MainActivity.LogDebug("NotifyItemChanged + UpdateQueueState" + indexOfItem);
                MainActivity.LogDebug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: " + fullFilename);
                if (recyclerTransferAdapter.ItemCount == indexOfItem)
                {

                }
                MainActivity.LogDebug("UI thread: " + Looper.MainLooper.IsCurrentThread);
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
            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
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
                            v.GetAdditionalStatusInfoView().Text = CommonHelpers.GetTransferSpeedString(avgSpeedBytes) + "  •  " + TransferViewHelper.GetTimeRemainingString(timeRemaining);
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
                    MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
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
                MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            SetNoTransfersMessage();
            MainActivity.LogDebug("NotifyItemChanged" + indexOfItem);
            MainActivity.LogDebug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: ");
            MainActivity.LogDebug("UI thread: " + Looper.MainLooper.IsCurrentThread);
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
                    MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
                    return;
                }
            }
            else
            {
            }
            if (this.noTransfers == null)
            {
                MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            SetNoTransfersMessage();
            if (specificRefreshAction == null)
            {
                //primaryListView.Adapter = (customAdapter); try with just notifyDataSetChanged...

                //int oldCount = recyclerTransferAdapter.ItemCount;
                //int newCount = transferItems.Count;
                //MainActivity.LogDebug("!!! recyclerTransferAdapter.NotifyDataSetChanged() old:: " + oldCount + " new: " + newCount);
                ////old 73, new 73...
                //recyclerTransferAdapter.NotifyItemRangeRemoved(0,oldCount);
                //recyclerTransferAdapter.NotifyItemRangeInserted(0,newCount);

                recyclerTransferAdapter.NotifyDataSetChanged();
                //(primaryListView.Adapter as TransferAdapter).NotifyDataSetChanged();

            }
            else
            {
                specificRefreshAction();
            }
        }

        //public string const IndividualItemType = 1;
        //public string const FolderItemType = 2;

        public class TransferAdapterRecyclerIndividualItem : TransferAdapterRecyclerVersion
        {
            public TransferAdapterRecyclerIndividualItem(System.Collections.IList ti) : base(ti)
            {
                localDataSet = ti;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                (holder as TransferViewHolder).getTransferItemView().setItem(localDataSet[position] as TransferItem, this.IsInBatchSelectMode);
                //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                bool TYPE_TO_USE = true;
                ITransferItemView view = TransferItemViewDetails.inflate(parent, this.showSizes, this.showSpeed);

                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).Click += TransferAdapterRecyclerIndividualItem_Click;
                (view as View).LongClick += TransferAdapterRecyclerVersion_LongClick;
                return new TransferViewHolder(view as View);
            }

            private void TransferAdapterRecyclerIndividualItem_Click(object sender, EventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
            }

            protected void TransferAdapterRecyclerVersion_LongClick(object sender, View.LongClickEventArgs e)
            {
                if (!IsInBatchSelectMode)
                {
                    setSelectedItem((sender as ITransferItemView).InnerTransferItem);
                    (sender as View).ShowContextMenu();
                }
                else
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
            }

        }





        public class TransferAdapterRecyclerFolderItem : TransferAdapterRecyclerVersion
        {
            public TransferAdapterRecyclerFolderItem(System.Collections.IList ti) : base(ti)
            {
                localDataSet = ti;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                (holder as TransferViewHolder).getTransferItemView().setItem(localDataSet[position] as FolderItem, this.IsInBatchSelectMode);
                //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                ITransferItemView view = TransferItemViewFolder.inflate(parent, this.showSizes, this.showSpeed);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).Click += TransferAdapterRecyclerFolderItem_Click;
                (view as View).LongClick += TransferAdapterRecyclerVersion_LongClick;
                return new TransferViewHolder(view as View);
            }

            private void TransferAdapterRecyclerFolderItem_Click(object sender, EventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
                else
                {
                    FolderItem f = (sender as ITransferItemView).InnerTransferItem as FolderItem;
                    setSelectedItem(f);
                    if (InUploadsMode)
                    {
                        CurrentlySelectedUploadFolder = f;
                    }
                    else
                    {
                        CurrentlySelectedDLFolder = f;
                    }

                    TransfersFragment.SaveScrollPositionOnMovingIntoFolder();
                    TransfersFragment.SetRecyclerAdapter();
                    SeekerState.MainActivityRef.SetTransferSupportActionBarState();
                    SeekerState.MainActivityRef.InvalidateOptionsMenu();
                }
            }

            protected void TransferAdapterRecyclerVersion_LongClick(object sender, View.LongClickEventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
                else
                {
                    setSelectedItem((sender as ITransferItemView).InnerTransferItem);
                    (sender as View).ShowContextMenu();
                }
            }

        }

        public class ProgressSizeTextView : TextView
        {
            public int Progress = 0;
            private readonly bool isInNightMode = false;
            public ProgressSizeTextView(Context context, IAttributeSet attrs) : base(context, attrs)
            {
                isInNightMode = DownloadDialog.InNightMode(context);
            }
            protected override void OnDraw(Canvas canvas)
            {
                if (isInNightMode)
                {
                    canvas.Save();
                    this.SetTextColor(Color.White);
                    base.OnDraw(canvas);
                    canvas.Restore();
                }
                else
                {
                    Rect rect = new Rect();
                    this.GetDrawingRect(rect);
                    rect.Right = (int)(rect.Left + (Progress * .01) * (rect.Right - rect.Left));
                    canvas.Save();
                    canvas.ClipRect(rect, Region.Op.Difference);
                    this.SetTextColor(Color.Black);
                    base.OnDraw(canvas);
                    canvas.Restore();

                    canvas.Save();
                    canvas.ClipRect(rect, Region.Op.Intersect); // lets draw inside center rect only
                    this.SetTextColor(Color.White);
                    base.OnDraw(canvas);
                    canvas.Restore();
                }
            }
        }


        public abstract class TransferAdapterRecyclerVersion : RecyclerView.Adapter //<TransferAdapterRecyclerVersion.TransferViewHolder>
        {
            protected System.Collections.IList localDataSet;
            public override int ItemCount => localDataSet.Count;
            protected ITransferItem selectedItem = null;
            public bool IsInBatchSelectMode;

            public TransfersFragment TransfersFragment;
#if DEBUG
            public void SelectedDebugInfo(ITransferItem iti)
            {

                int position = TransferItemManagerWrapped.GetUserIndexForITransferItem(iti);
                MainActivity.LogDebug($"position: {position} ti name: {iti.GetDisplayName()}");

            }
#endif

            public void setSelectedItem(ITransferItem item)
            {
                this.selectedItem = item;
#if DEBUG
                SelectedDebugInfo(item);
#endif
                if (this.selectedItem == null)
                {
                    MainActivity.LogInfoFirebase("selected item was set as null");
                }
            }

            public ITransferItem getSelectedItem()
            {
                return this.selectedItem;
            }

            //public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            //{
            //    (holder as TransferViewHolder).getTransferItemView().setItem(localDataSet[position] as TransferItem);
            //    //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            //}




            protected readonly bool showSpeed = false;
            protected readonly bool showSizes = false;
            public TransferAdapterRecyclerVersion(System.Collections.IList tranfersList)
            {
                localDataSet = tranfersList;
                showSpeed = SeekerState.TransferViewShowSpeed;
                showSizes = SeekerState.TransferViewShowSizes;
            }

        }

        public const int UNIQUE_TRANSFER_GROUP_ID = 303;
        public class TransferViewHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
        {
            private ITransferItemView transferItemView;


            public TransferViewHolder(View view) : base(view)
            {
                //super(view);
                // Define click listener for the ViewHolder's View

                transferItemView = (ITransferItemView)view;
                transferItemView.ViewHolder = this;
                (transferItemView as View).SetOnCreateContextMenuListener(this);
            }

            public ITransferItemView getTransferItemView()
            {
                return transferItemView;
            }

            public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
            {
                //base.OnCreateContextMenu(menu, v, menuInfo);
                ITransferItemView tvh = v as ITransferItemView;
                TransferItem ti = null;
                FolderItem fi = null;
                TransferStates folderItemState = TransferStates.None;
                bool isTransferItem = false;
                bool anyFailed = false;
                //bool anyOffline = false;
                bool isUpload = false;
                if (tvh?.InnerTransferItem is TransferItem tvhi)
                {
                    isTransferItem = true;
                    ti = tvhi;
                    isUpload = ti.IsUpload();
                }
                else if (tvh?.InnerTransferItem is FolderItem tvhf)
                {
                    fi = tvhf;
                    folderItemState = fi.GetState(out anyFailed, out _);
                    isUpload = fi.IsUpload();
                }
                //else
                //{
                //shouldnt happen....
                AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)menuInfo;
                int pos1 = info?.Position ?? -1;
                //}


                //if somehow we got here without setting the transfer item. then set it now...  you have menuInfo.Position, AND tvh.InnerTransferItem. and recyclerTransfer.GetSelectedItem() to check for null.

                if (!isUpload)
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && ti.State.HasFlag(TransferStates.Cancelled) /*&& ti.Progress > 0*/) //progress > 0 doesnt work if someone queues an item as paused...
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 0, 0, Resource.String.resume_dl);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 0, 0, Resource.String.retry_dl);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && folderItemState.HasFlag(TransferStates.Cancelled)  /*&& fi.GetFolderProgress() > 0*/)
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 100, 0, Resource.String.ResumeFolder);
                        }
                        else if (tvh != null && fi != null && (!folderItemState.HasFlag(TransferStates.Completed) && !folderItemState.HasFlag(TransferStates.Succeeded) && !folderItemState.HasFlag(TransferStates.Errored) && !folderItemState.HasFlag(TransferStates.TimedOut) && !folderItemState.HasFlag(TransferStates.Rejected)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 101, 0, Resource.String.PauseFolder);
                        }
                    }
                }
                else
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && !(CommonHelpers.IsUploadCompleteOrAborted(ti.State)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 103, 0, Resource.String.AbortUpload);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && !(CommonHelpers.IsUploadCompleteOrAborted(folderItemState))) ;
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 101, 0, Resource.String.AbortUploads);
                        }
                    }
                }
                if (!isUpload)
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (ti.State.HasFlag(TransferStates.Succeeded)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                            //if completed then we dont need to show the cancel option...
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, Resource.String.cancel_and_clear);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && (folderItemState.HasFlag(TransferStates.Succeeded)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, Resource.String.cancel_and_clear);
                        }
                    }
                }
                else
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (CommonHelpers.IsUploadCompleteOrAborted(ti.State)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                            //if completed then we dont need to show the cancel option...
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, Resource.String.AbortandClearUpload);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && (CommonHelpers.IsUploadCompleteOrAborted(folderItemState)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, Resource.String.AbortandClearUploads);
                        }
                    }
                }

                if (!isUpload)
                {
                    if (isTransferItem)
                    {

                        if (tvh != null && ti != null)
                        {
                            if (ti.QueueLength > 0)
                            {
                                //the queue length of a succeeded download can be 183......
                                //bc queue length AND free upload slots!!
                                if (ti.State.HasFlag(TransferStates.Succeeded) ||
                                    ti.State.HasFlag(TransferStates.Completed))
                                {
                                    //no op
                                }
                                else
                                {
                                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, 3, 3, Resource.String.refresh_queue_pos);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null)
                        {
                            if (fi.GetQueueLength() > 0)
                            {
                                //the queue length of a succeeded download can be 183......
                                //bc queue length AND free upload slots!!
                                if (folderItemState.HasFlag(TransferStates.Succeeded) ||
                                    folderItemState.HasFlag(TransferStates.Completed))
                                {
                                    //no op
                                }
                                else
                                {
                                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, 3, 3, Resource.String.refresh_queue_pos);
                                }
                            }
                        }
                    }
                }

                if (!isUpload)
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (ti.State.HasFlag(TransferStates.Succeeded)) && ti.FinalUri != string.Empty)
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 4, 4, Resource.String.play_file);
                        }
                    }
                    else
                    {
                        if (folderItemState.HasFlag(TransferStates.TimedOut) || folderItemState.HasFlag(TransferStates.Rejected) || folderItemState.HasFlag(TransferStates.Errored) || anyFailed)
                        {
                            //no op
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 102, 4, Resource.String.RetryFailedFiles);
                        }
                    }
                }
                var subMenu = menu.AddSubMenu(UNIQUE_TRANSFER_GROUP_ID, 5, 5, Resource.String.UserOptions);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 6, 6, Resource.String.browse_user);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 7, 7, Resource.String.browse_at_location);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 8, 8, Resource.String.search_user_files);
                CommonHelpers.AddAddRemoveUserMenuItem(subMenu, UNIQUE_TRANSFER_GROUP_ID, 9, 9, tvh.InnerTransferItem.GetUsername(), false);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 10, 10, Resource.String.msg_user);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 11, 11, Resource.String.get_user_info);
                CommonHelpers.AddUserNoteMenuItem(subMenu, UNIQUE_TRANSFER_GROUP_ID, 12, 12, tvh.InnerTransferItem.GetUsername());
                CommonHelpers.AddGivePrivilegesIfApplicable(subMenu, 13);

                if (isUpload)
                {
                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, 104, 6, Resource.String.IgnoreUnshareUser);
                }
                //finally batch selection mode
                menu.Add(UNIQUE_TRANSFER_GROUP_ID, 105, 16, Resource.String.BatchSelect);

                //if (!isUpload)
                //{
                //    if (isTransferItem)
                //    {
                //        if(ti.State.HasFlag(TransferStates.UserOffline))
                //        {

                //        }
                //    }
                //    else
                //    {
                //        if(anyOffline)
                //        {
                //            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 106, 17, "Do Not Auto-Retry When User Goes Back Online");
                //            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 106, 17, "Auto-Retry When User Goes Back Online");
                //        }
                //    }
                //}
            }

        }

        private void TransferProgressUpdated(object sender, SeekerApplication.ProgressUpdatedUIEventArgs e)
        {
            bool needsRefresh = (e.ti.IsUpload() && TransfersFragment.InUploadsMode) || (!(e.ti.IsUpload()) && !(TransfersFragment.InUploadsMode));
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

                    //        MainActivity.LogDebug("notifyItemRemoved " + indexRemoved + "count: " + recyclerTransferAdapter.ItemCount);
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

                        //MainActivity.LogDebug("Index is "+index+" TransferProgressUpdated"); //tested!

                        //int indexToUpdate = transferItems.IndexOf(relevantItem);

                        Activity?.RunOnUiThread(() =>
                        {
                            int index = -1;
                            index = TransferItemManagerWrapped.GetUserIndexForTransferItem(e.ti);
                            if (index == -1)
                            {
                                MainActivity.LogDebug("Index is -1 TransferProgressUpdated");
                                return;
                            }
                            MainActivity.LogDebug("UI THREAD TRANSFER PROGRESS UPDATED"); //this happens every 20ms.  so less often then tranfer progress updated.  usually 6 of those can happen before 2 of these.
                            refreshItemProgress(index, e.ti.Progress, e.ti, e.wasFailed, e.avgspeedBytes);

                        });



                    }
                    catch (System.Exception error)
                    {
                        MainActivity.LogFirebase(error.Message + " partial update");
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
            MainActivity.TransferItemQueueUpdated += TranferQueueStateChanged;

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
                if (e.IsUpload() && InUploadsMode)
                {
                    lock (TransferItemManagerWrapped.GetUICurrentList())
                    { //todo can update this to do a partial refresh... just the index..
                        if (GroupByFolder && !CurrentlyInFolder())
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
            MainActivity.TransferItemQueueUpdated -= TranferQueueStateChanged;
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
                lock (TransferItemManagerDL.GetUICurrentList())
                { //todo can update this to do a partial refresh... just the index..
                    refreshListView();
                }
            });
        }

        public static void SetupCancellationToken(TransferItem transferItem, CancellationTokenSource cts, out CancellationTokenSource oldToken)
        {
            transferItem.CancellationTokenSource = cts;
            if (!CancellationTokens.TryAdd(ProduceCancellationTokenKey(transferItem), cts)) //returns false if it already exists in dict
            {
                //likely old already exists so just replace the old one
                oldToken = CancellationTokens[ProduceCancellationTokenKey(transferItem)];
                CancellationTokens[ProduceCancellationTokenKey(transferItem)] = cts;
            }
            else
            {
                oldToken = null;
            }
        }

        public static string ProduceCancellationTokenKey(TransferItem i)
        {
            return ProduceCancellationTokenKey(i.FullFilename, i.Size, i.Username);
        }

        public static string ProduceCancellationTokenKey(string fullFilename, long size, string username)
        {
            return fullFilename + size.ToString() + username;
        }


        public static System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> CancellationTokens = new System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource>();




    }

}