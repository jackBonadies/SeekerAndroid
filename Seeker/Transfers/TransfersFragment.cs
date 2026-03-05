using Android.Content;
using Seeker.Browse;
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
        private static TransfersViewState ViewState => TransfersViewState.Instance;

        private View rootView = null;
        private TextView noTransfers = null;
        private Button setupUpSharing = null;
        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        public TransferAdapterRecyclerVersion recyclerTransferAdapter;

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
                        TransferItems.TransferItemManagerDL.ClearAllComplete();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerDL.ClearAllCompleteFromFolder(ViewState.CurrentlySelectedDLFolder);
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
                        TransferItems.TransferItemManagerUploads.ClearAllComplete();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerUploads.ClearAllCompleteFromFolder(ViewState.CurrentlySelectedUploadFolder);
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
                        TransferItems.TransferItemManagerDL.CancelAll(true);
                        var cleanupItems = TransferItems.TransferItemManagerDL.ClearAllReturnCleanupItems();
                        if (cleanupItems.Any()) TransferItems.TransferItemManagerWrapped.CleanupEntry(cleanupItems);
                    }
                    else
                    {
                        TransferItems.TransferItemManagerDL.CancelFolder(ViewState.CurrentlySelectedDLFolder, true);
                        var cleanupItems = TransferItems.TransferItemManagerDL.ClearAllFromFolderReturnCleanupItems(ViewState.CurrentlySelectedDLFolder);
                        if (cleanupItems.Any()) TransferItems.TransferItemManagerWrapped.CleanupEntry(cleanupItems);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_abort_all: //abort all
                    Logger.InfoFirebase("action_abort_all_pressed");
                    SeekerState.AbortAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (ViewState.CurrentlySelectedUploadFolder == null)
                    {
                        TransferItems.TransferItemManagerUploads.CancelAll();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerUploads.CancelFolder(ViewState.CurrentlySelectedUploadFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_pause_all:
                    Logger.InfoFirebase("pause all Pressed");
                    SeekerState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (ViewState.CurrentlySelectedDLFolder == null)
                    {
                        TransferItems.TransferItemManagerDL.CancelAll();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerDL.CancelFolder(ViewState.CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_resume_all:
                    Logger.InfoFirebase("resume all Pressed");
                    SessionService.Instance.RunWithReconnect(() =>
                    {
                        if (ViewState.CurrentlySelectedDLFolder == null)
                        {
                            DownloadService.Instance.DownloadRetryAllConditionLogic(false, true, null, false);
                        }
                        else
                        {
                            DownloadService.Instance.DownloadRetryAllConditionLogic(false, false, ViewState.CurrentlySelectedDLFolder, false);
                        }
                    });
                    return true;
                case Resource.Id.retry_all_failed:
                    RetryAllConditionEntry(true, false);
                    return true;
                case Resource.Id.batch_select:
                    TransfersActionModeCallback = new ActionModeCallback() { Adapter = recyclerTransferAdapter, Frag = this };
                    ForceOutIfZeroSelected = false;
                    TransfersActionMode = SeekerState.MainActivityRef.StartSupportActionMode(TransfersActionModeCallback);
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
            SessionService.Instance.RunWithReconnect(() =>
            {
                if (ViewState.CurrentlySelectedDLFolder == null)
                {
                    DownloadService.Instance.DownloadRetryAllConditionLogic(failed, true, null, batchSelectedOnly, listTi);
                }
                else
                {
                    DownloadService.Instance.DownloadRetryAllConditionLogic(failed, false, ViewState.CurrentlySelectedDLFolder, batchSelectedOnly, listTi);
                }
            });
        }

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

        private void SetNoTransfersMessage()
        {
            if (ViewState.InUploadsMode)
            {
                if (!(TransferItems.TransferItemManagerUploads.IsEmpty()))
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
                if (!(TransferItems.TransferItemManagerDL.IsEmpty()))
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
            recyclerViewTransferItems = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerView1);
            this.noTransfers = rootView.FindViewById<TextView>(Resource.Id.noTransfersView);
            this.setupUpSharing = rootView.FindViewById<Button>(Resource.Id.setUpSharing);
            this.setupUpSharing.Click += SetupUpSharing_Click;

            this.RegisterForContextMenu(recyclerViewTransferItems); //doesnt work for recycle views

            SetNoTransfersMessage();

            recycleLayoutManager = new CustomLinearLayoutManager(Activity);
            SetRecyclerAdapter();

            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);

            Logger.InfoFirebase("AutoClear: " + PreferencesState.AutoClearCompleteDownloads);
            Logger.InfoFirebase("AutoRetry: " + PreferencesState.AutoRetryDownload);

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
        public static AndroidX.AppCompat.View.ActionMode TransfersActionMode = null;

        public void SetRecyclerAdapter(bool restoreState = false)
        {
            lock (TransferItems.TransferItemManagerWrapped.GetUICurrentList())
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
                    recyclerTransferAdapter = new TransferAdapterRecyclerFolderItem(TransferItems.TransferItemManagerWrapped.GetUICurrentList() as List<FolderItem>);
                }
                else
                {
                    recyclerTransferAdapter = new TransferAdapterRecyclerIndividualItem(TransferItems.TransferItemManagerWrapped.GetUICurrentList() as List<TransferItem>);
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
        private void TransferQueueStateChanged(object sender, TransferItem e)
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
            DownloadService.Instance.TransferItemChanged += OnTransferItemChanged;
            DownloadService.Instance.TransferListRefreshRequested += OnTransferListRefreshRequested;
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
            ViewState.SwitchToUploadsMode();
            this.RefreshForModeSwitch();
            SeekerState.MainActivityRef.InvalidateOptionsMenu();
        }

        public override void OnPause()
        {
            DownloadService.Instance.TransferItemChanged -= OnTransferItemChanged;
            DownloadService.Instance.TransferListRefreshRequested -= OnTransferListRefreshRequested;
            base.OnPause();
            Logger.Debug("TransferFragment OnPause");
        }

        private void OnTransferItemChanged(object sender, int position)
        {
            recyclerTransferAdapter?.NotifyItemChanged(position);
        }

        private void OnTransferListRefreshRequested(object sender, Action specificRefreshAction)
        {
            refreshListView(specificRefreshAction);
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
                    var fi = TransferItems.TransferItemManagerDL.GetItemAtUserIndex(pos, uiState) as FolderItem;
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
                    var ti = TransferItems.TransferItemManagerDL.GetItemAtUserIndex(pos, uiState) as TransferItem;
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




        private void DownloadRetryLogic(ITransferItem transferItem)
        {
            string chosenFname = (transferItem as TransferItem).FullFilename; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            string chosenUname = (transferItem as TransferItem).Username; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            Logger.Debug("chosenFname? " + chosenFname);
            TransferItem item1 = TransferItems.TransferItemManagerDL.GetTransferItemWithIndexFromAll(chosenFname, chosenUname, out int _);
            int indexToRefresh = TransferItems.TransferItemManagerDL.GetUserIndexForTransferItem(item1, ViewState.CreateDLUIState());
            Logger.Debug("item1 is null?" + (item1 == null).ToString());//tested
            if (item1 == null || indexToRefresh == -1)
            {
                SeekerApplication.Toaster.ShowToast(SeekerState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
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

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {

                TransferState.SetupCancellationToken(item1, cancellationTokenSource, out _);
                var dlInfo = new DownloadInfo(item1.Username, item1.FullFilename, item1.Size, null, cancellationTokenSource, item1.QueueLength, item1.Failed ? 1 : 0, item1.GetDirectoryLevel()) { TransferItemReference = item1 };
                Task task = DownloadService.Instance.DownloadFileAsync(item1.Username, item1.FullFilename, item1.GetSizeForDL(), cancellationTokenSource, out _, dlInfo, isFileDecodedLegacy: item1.ShouldEncodeFileLatin1(), isFolderDecodedLegacy: item1.ShouldEncodeFolderLatin1());
                task.ContinueWith(DownloadService.Instance.DownloadContinuationActionUI(new DownloadAddedEventArgs(dlInfo))); //if paused do retry counter 0.
            }
            catch (DuplicateTransferException)
            {
                //happens due to button mashing...
                return;
            }
            catch (System.Exception error)
            {
                Action a = new Action(() => { SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_) + error.Message, ToastLength.Long); });
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
            item1.TransferItemExtra &= ~TransferItemExtras.DirNotSet;
            var refreshOnlySelected = new Action(() =>
            {

                Logger.Debug("notifyItemChanged " + position);

                recyclerTransferAdapter.NotifyItemChanged(position);


            });
            lock (TransferItems.TransferItemManagerDL.GetUICurrentList(ViewState.CreateDLUIState()))
            { //also can update this to do a partial refresh...
                refreshListView(refreshOnlySelected);
            }
        }

        private bool NotLoggedInShowMessageGaurd(string msg)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast("Must be logged in to " + msg, ToastLength.Short);
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

                int position = TransferItems.TransferItemManagerWrapped.GetUserIndexForITransferItem(ti);


                if (position == -1)
                {
                    SeekerApplication.Toaster.ShowToast("Selected transfer does not exist anymore.. try again.", ToastLength.Short);
                    return base.OnContextItemSelected(item);
                }

                if (UiHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), ti.GetUsername(), SeekerState.ActiveActivityRef, this.View))
                {
                    Logger.Debug("handled by commons");
                    return base.OnContextItemSelected(item);
                }

                switch ((TransferContextMenuItem)item.ItemId)
                {
                    case TransferContextMenuItem.RetryResumeDownload: //single transfer only
                        //retry download (resume download)
                        if (NotLoggedInShowMessageGaurd("start transfer"))
                        {
                            return true;
                        }

                        SessionService.Instance.RunWithReconnect(() => DownloadRetryLogic(ti));
                        //Toast.MakeText(Applicatio,"Retrying...",ToastLength.Short).Show();
                        break;
                    case TransferContextMenuItem.ClearFromList:
                        Logger.InfoFirebase("Clear Complete item pressed");
                        lock (TransferItems.TransferItemManagerWrapped.GetUICurrentList()) //TODO: test
                        {
                            try
                            {
                                if (ViewState.InUploadsMode)
                                {
                                    TransferItems.TransferItemManagerWrapped.RemoveAtUserIndex(position);
                                }
                                else
                                {
                                    TransferItems.TransferItemManagerWrapped.RemoveAndCleanUpAtUserIndex(position); //UI
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                    SeekerApplication.Toaster.ShowToast("Selected transfer does not exist anymore.. try again.", ToastLength.Short);
                                return base.OnContextItemSelected(item);
                            }
                            recyclerTransferAdapter.NotifyItemRemoved(position);
                        }
                        break;
                    case TransferContextMenuItem.CancelAndClear: //cancel and clear (downloads) OR abort and clear (uploads)
                        Logger.InfoFirebase("Cancel and Clear item pressed");
                        ITransferItem tItem = null;
                        try
                        {
                            tItem = TransferItems.TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            SeekerApplication.Toaster.ShowToast("Selected transfer does not exist anymore.. try again.", ToastLength.Short);
                            return base.OnContextItemSelected(item);
                        }
                        if (tItem is TransferItem tti)
                        {
                            TransferState.CancelAndRemoveToken(tti);
                            lock (TransferItems.TransferItemManagerWrapped.GetUICurrentList())
                            {
                                if (ViewState.InUploadsMode)
                                {
                                    TransferItems.TransferItemManagerWrapped.RemoveAtUserIndex(position);
                                }
                                else
                                {
                                    TransferItems.TransferItemManagerWrapped.RemoveAndCleanUpAtUserIndex(position); //this means basically, wait for the stream to be closed. no race conditions..
                                }
                                recyclerTransferAdapter.NotifyItemRemoved(position);
                            }
                        }
                        else if (tItem is FolderItem fi)
                        {
                            TransferItems.TransferItemManagerWrapped.CancelFolder(fi, true);
                            TransferItems.TransferItemManagerWrapped.ClearAllFromFolderAndClean(fi);
                            lock (TransferItems.TransferItemManagerWrapped.GetUICurrentList())
                            {
                                recyclerTransferAdapter.NotifyItemRemoved(position);
                            }
                        }
                        else
                        {
                            Logger.InfoFirebase("Cancel and Clear item pressed - bad item");
                        }
                        break;
                    case TransferContextMenuItem.RefreshQueuePosition:
                        if (NotLoggedInShowMessageGaurd("get queue position"))
                        {
                            return true;
                        }
                        tItem = null;
                        try
                        {
                            tItem = TransferItems.TransferItemManagerDL.GetItemAtUserIndex(position, ViewState.CreateDLUIState());
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            SeekerApplication.Toaster.ShowToast("Selected transfer does not exist anymore.. try again.", ToastLength.Short);
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
                    case TransferContextMenuItem.PlayFile:
                        tItem = null;
                        try
                        {
                            tItem = TransferItems.TransferItemManagerDL.GetItemAtUserIndex(position, ViewState.CreateDLUIState()) as TransferItem;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            SeekerApplication.Toaster.ShowToast("Selected transfer does not exist anymore.. try again.", ToastLength.Short);
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
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_play), ToastLength.Short); //normally bc no player is installed.
                        }
                        break;
                    case TransferContextMenuItem.BrowseAtLocation: //browse at location (browse at folder)
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

                            BrowseService.RequestFilesApi(ttti.Username, this.View, action, startingDir);
                        }
                        else if (ti is FolderItem fi)
                        {
                            if (fi.IsEmpty())
                            {
                                //since if auto clear is on, and the menu is already up, the final item in this folder can clear before we end up selecting something.
                                SeekerApplication.Toaster.ShowToast("Folder is empty.", ToastLength.Short);
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

                            BrowseService.RequestFilesApi(fi.Username, this.View, action, startingDir);
                        }
                        break;
                    case TransferContextMenuItem.ResumeFolder: //resume folder
                        if (NotLoggedInShowMessageGaurd("resume folder"))
                        {
                            return true;
                        }
                        Logger.InfoFirebase("resume folder Pressed");
                        SessionService.Instance.RunWithReconnect(() => DownloadService.Instance.DownloadRetryAllConditionLogic(false, false, ti as FolderItem, false));
                        break;
                    case TransferContextMenuItem.PauseFolderOrAbortUploads: //pause folder or abort uploads (uploads)
                        TransferItems.TransferItemManagerWrapped.CancelFolder(ti as FolderItem);
                        int index = TransferItems.TransferItemManagerWrapped.GetIndexForFolderItem(ti as FolderItem);
                        recyclerTransferAdapter.NotifyItemChanged(index);
                        break;
                    case TransferContextMenuItem.RetryFailedFiles: //retry failed downloads from folder
                        if (NotLoggedInShowMessageGaurd("retry folder"))
                        {
                            return true;
                        }
                        Logger.InfoFirebase("retry folder Pressed");
                        SessionService.Instance.RunWithReconnect(() => DownloadService.Instance.DownloadRetryAllConditionLogic(true, false, ti as FolderItem, false));
                        break;
                    case TransferContextMenuItem.AbortUpload: //abort upload
                        Logger.InfoFirebase("Abort Upload item pressed");
                        tItem = null;
                        try
                        {
                            tItem = TransferItems.TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            SeekerApplication.Toaster.ShowToast("Selected transfer does not exist anymore.. try again.", ToastLength.Short);
                            return base.OnContextItemSelected(item);
                        }
                        TransferItem uploadToCancel = tItem as TransferItem;
                        TransferState.CancelAndRemoveToken(uploadToCancel);
                        lock (TransferItems.TransferItemManagerWrapped.GetUICurrentList())
                        {
                            recyclerTransferAdapter.NotifyItemChanged(position);
                        }
                        break;
                    case TransferContextMenuItem.IgnoreUnshareUser: //ignore (unshare) user
                        Logger.InfoFirebase("Unshare User item pressed");
                        IEnumerable<TransferItem> tItems = TransferItems.TransferItemManagerWrapped.GetTransferItemsForUser(ti.GetUsername());
                        foreach (var tiToCancel in tItems)
                        {
                            TransferState.CancelAndRemoveToken(tiToCancel);
                            lock (TransferItems.TransferItemManagerWrapped.GetUICurrentList())
                            {
                                int posOfCancelled = TransferItems.TransferItemManagerWrapped.GetUserIndexForTransferItem(tiToCancel);
                                if (posOfCancelled != -1)
                                {
                                    recyclerTransferAdapter.NotifyItemChanged(posOfCancelled);
                                }
                            }
                        }
                        SeekerApplication.AddToIgnoreListFeedback(SeekerState.ActiveActivityRef, ti.GetUsername());
                        break;
                    case TransferContextMenuItem.BatchSelect: //batch selection mode
                        TransfersActionModeCallback = new ActionModeCallback() { Adapter = recyclerTransferAdapter, Frag = this };
                        ForceOutIfZeroSelected = true;
                        TransfersActionMode = SeekerState.MainActivityRef.StartSupportActionMode(TransfersActionModeCallback);
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
            int userPositionBeingRemoved = TransferItems.TransferItemManagerWrapped.GetUserIndexForTransferItem(ti);
            if (userPositionBeingRemoved == -1)
            {
                //it is not currently on our screen, perhaps it is in uploads (and we are in downloads) or we are inside a folder (and it is outside)
                Logger.Debug("batch on, different screen item removed");
                return;
            }
            Logger.Debug("batch on, updating: " + userPositionBeingRemoved);
            //adjust numbers
            int cnt = ViewState.BatchSelectedItems.Count;
            for (int i = cnt - 1; i >= 0; i--)
            {
                int position = ViewState.BatchSelectedItems[i];
                if (position < userPositionBeingRemoved)
                {
                    continue;
                }
                else if (position == userPositionBeingRemoved)
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
                    if (queueLenOld == t.QueueLength) //queueLenOld is a value type snapshot, so this checks if the queue position changed
                    {
                        if (queueLenOld == int.MaxValue)
                        {
                            SeekerApplication.Toaster.ShowToast("Position in queue is unknown.", ToastLength.Short);
                        }
                        else
                        {
                            SeekerApplication.Toaster.ShowToast(string.Format(SeekerApplication.GetString(Resource.String.position_is_still_), t.QueueLength), ToastLength.Short);
                        }
                    }
                    else
                    {
                        int indexOfItem = TransferItems.TransferItemManagerDL.GetUserIndexForTransferItem(t, ViewState.CreateDLUIState());
                        if (indexOfItem == -1)
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

            DownloadService.Instance.GetDownloadPlaceInQueue(ttItem.Username, ttItem.FullFilename, true, false, ttItem, actionOnComplete);
        }

        public void UpdateQueueState(string fullFilename) //Add this to the event handlers so that when downloads are added they have their queue position.
        {
            try
            {
                if (ViewState.InUploadsMode)
                {
                    return;
                }
                int indexOfItem = TransferItems.TransferItemManagerDL.GetUserIndexForTransferItem(fullFilename, ViewState.CreateDLUIState());
                if (indexOfItem < 0 || indexOfItem >= recyclerTransferAdapter.ItemCount)
                {
                    return;
                }
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
            ITransferItemView v = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(indexToRefresh) as ITransferItemView;
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
            if (Context == null)
            {
                if (SeekerState.MainActivityRef == null)
                {
                    Logger.Firebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
                    return;
                }
            }
            if (this.noTransfers == null)
            {
                Logger.Firebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            SetNoTransfersMessage();
            if (indexOfItem < 0 || indexOfItem >= recyclerTransferAdapter.ItemCount)
            {
                return;
            }
            recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
        }

        public void refreshListView(Action specificRefreshAction = null)
        {
            if (Context == null)
            {
                if (SeekerState.MainActivityRef == null)
                {
                    Logger.Firebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
                    return;
                }
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

        private void TransferProgressUpdated(object sender, SeekerApplication.ProgressUpdatedUIEventArgs e)
        {
            if (e.ti.IsUpload() != ViewState.InUploadsMode)
            {
                return;
            }
            if (e.percentComplete == 0)
            {
                return;
            }
            if (e.fullRefresh)
            {
                Activity?.RunOnUiThread(refreshListViewSafe); //in case of rotation it is the ACTIVITY which will be null!!!!
                return;
            }
            try
            {
                DateTime now = DateTime.UtcNow;
                string throttleKey = e.ti.GetThrottleKey();
                DateTime lastUpdated = ProgressUpdatedThrottler.GetOrAdd(throttleKey, now);
                bool isNew = lastUpdated == now;
                bool shouldUpdate = isNew
                    || e.wasFailed
                    || now.Subtract(lastUpdated).TotalMilliseconds > THROTTLE_PROGRESS_UPDATED_RATE;

                if (!shouldUpdate)
                {
                    return;
                }

                ProgressUpdatedThrottler[throttleKey] = now;

                Activity?.RunOnUiThread(() =>
                {
                    int index = TransferItems.TransferItemManagerWrapped.GetUserIndexForTransferItem(e.ti);
                    if (index == -1)
                    {
                        Logger.Debug("Index is -1 TransferProgressUpdated");
                        return;
                    }
                    refreshItemProgress(index, e.ti.Progress, e.ti, e.wasFailed, e.avgspeedBytes);
                });
            }
            catch (System.Exception error)
            {
                Logger.Firebase(error.Message + " partial update");
            }
        }

        private void TransferStateChangedItem(object sender, TransferItem ti)
        {
            Action action = new Action(() =>
            {

                int index = TransferItems.TransferItemManagerWrapped.GetUserIndexForTransferItem(ti); //todo null ti
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
            UploadService.TransferAddedUINotify += MainActivity_TransferAddedUINotify; //todo this should eventually be for downloads too.
            DownloadService.Instance.TransferItemQueueUpdated += TransferQueueStateChanged;

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
                    lock (TransferItems.TransferItemManagerWrapped.GetUICurrentList())
                    { //todo can update this to do a partial refresh... just the index..
                        if (ViewState.GroupByFolder && !ViewState.CurrentlyInFolder())
                        {
                            //folderview - so we may insert or update
                            //int index = TransferItems.TransferItemManagerWrapped.GetUserIndexForTransferItem(e);
                            refreshListView(); //just to be safe...

                        }
                        else
                        {
                            //int index = TransferItems.TransferItemManagerWrapped.GetUserIndexForTransferItem(e);
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
            DownloadService.Instance.TransferItemQueueUpdated -= TransferQueueStateChanged;
            UploadService.TransferAddedUINotify -= MainActivity_TransferAddedUINotify;
            base.OnStop();
        }

        //NOTE: there can be several TransfersFragment at a time.
        //this can be done by having many MainActivitys in the back stack
        //i.e. go to UserList > press browse files > go to UserList > press browse files --> 3 TransferFragments, 2 of which are stopped but not Destroyed.
        public override void OnCreate(Bundle savedInstanceState)
        {

            DownloadService.Instance.ClearDownloadAddedEventsFromTarget(this);
            DownloadService.Instance.DownloadAddedUINotify += SeekerState_DownloadAddedUINotify;
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
                lock (TransferItems.TransferItemManagerDL.GetUICurrentList(ViewState.CreateDLUIState()))
                { //todo can update this to do a partial refresh... just the index..
                    refreshListView();
                }
            });
        }

    }

}