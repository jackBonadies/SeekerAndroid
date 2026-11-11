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
        private View noTransfers = null;
        private ImageView noTransfersIcon = null;
        private TextView noTransfersHeader = null;
        private TextView noTransfersSubtitle = null;
        private Button setupUpSharing = null;
        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        public TransferAdapterRecyclerVersion recyclerTransferAdapter;

        public const int THROTTLE_PROGRESS_UPDATED_RATE = 200;//in ms;

        // coalesces list rebuilds requested from transfer threads (e.g. one per file of a folder being added)
        private const int REBUILD_COALESCE_MS = 100;
        private static readonly Handler rebuildHandler = new Handler(Looper.MainLooper);
        private Action scheduledRebuild;
        private int rebuildPending;



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

            if (PreferencesState.TransferViewShowTimeRemaining)
            {
                menu.FindItem(Resource.Id.action_show_time_remaining).SetTitle(Resource.String.HideTimeRemaining);
            }
            else
            {
                menu.FindItem(Resource.Id.action_show_time_remaining).SetTitle(Resource.String.ShowTimeRemaining);
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
                    SeekerState.MainActivityRef.OnBackPressedDispatcher.OnBackPressed();
                    return true;
                case Resource.Id.action_clear_all_complete: //clear all complete
                    Logger.InfoFirebase("Clear All Complete Pressed");
                    // TODO: move to helper (either TranferItemManager or Wrapper)
                    if (ViewState.CurrentlySelectedDLFolder == null)
                    {
                        TransferItems.TransferItemManagerDL.ClearAllComplete();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerDL.ClearAllCompleteFromFolder(ViewState.CurrentlySelectedDLFolder);
                    }
                    RebuildList();
                    return true;
                case Resource.Id.action_toggle_group_by: //toggle group by. group / ungroup by folder.
                    ViewState.GroupByFolder = !ViewState.GroupByFolder;
                    PreferencesManager.SaveTransferViewGroupByFolder();
                    SetRecyclerAdapter();
                    return true;
                case Resource.Id.action_show_size:
                    PreferencesState.TransferViewShowSizes = !PreferencesState.TransferViewShowSizes;
                    PreferencesManager.SaveTransferViewShowSizes();
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_show_speed:
                    PreferencesState.TransferViewShowSpeed = !PreferencesState.TransferViewShowSpeed;
                    PreferencesManager.SaveTransferViewShowSpeed();
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_show_time_remaining:
                    PreferencesState.TransferViewShowTimeRemaining = !PreferencesState.TransferViewShowTimeRemaining;
                    PreferencesManager.SaveTransferViewShowTimeRemaining();
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_clear_all_complete_and_aborted:
                    Logger.InfoFirebase("Clear All Complete Pressed");
                    // TODO: move to helper (either TranferItemManager or Wrapper)
                    if (ViewState.CurrentlySelectedUploadFolder == null)
                    {
                        TransferItems.TransferItemManagerUploads.ClearAllComplete();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerUploads.ClearAllCompleteFromFolder(ViewState.CurrentlySelectedUploadFolder);
                    }
                    RebuildList();
                    return true;
                case Resource.Id.action_toggle_download_upload:
                    ViewState.InUploadsMode = !ViewState.InUploadsMode;
                    PreferencesManager.SaveTransferViewInUploadsMode();
                    RefreshForModeSwitch();
                    return true;
                case Resource.Id.action_cancel_and_clear_all: //cancel and clear all
                    Logger.InfoFirebase("action_cancel_and_clear_all Pressed");
                    TransferDebouncer.CancelAndClearAll.Trigger();
                    // TODO: move to helper (either TranferItemManager or Wrapper)
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
                    RebuildList();
                    return true;
                case Resource.Id.action_abort_all: //abort all
                    Logger.InfoFirebase("action_abort_all_pressed");
                    TransferDebouncer.AbortAll.Trigger();
                    // TODO: move to helper (either TranferItemManager or Wrapper)
                    if (ViewState.CurrentlySelectedUploadFolder == null)
                    {
                        TransferItems.TransferItemManagerUploads.CancelAll();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerUploads.CancelFolder(ViewState.CurrentlySelectedUploadFolder);
                    }
                    RebuildList();
                    return true;
                case Resource.Id.action_pause_all:
                    Logger.InfoFirebase("pause all Pressed");
                    TransferDebouncer.CancelAndClearAll.Trigger();
                    // TODO: move to helper (either TranferItemManager or Wrapper)
                    if (ViewState.CurrentlySelectedDLFolder == null)
                    {
                        TransferItems.TransferItemManagerDL.CancelAll();
                    }
                    else
                    {
                        TransferItems.TransferItemManagerDL.CancelFolder(ViewState.CurrentlySelectedDLFolder);
                    }
                    RebuildList();
                    return true;
                case Resource.Id.action_resume_all:
                    Logger.InfoFirebase("resume all Pressed");
                    // TODO: move to helper (either TranferItemManager or Wrapper)
                    SessionService.Instance.RunWithReconnect(() =>
                    {
                        if (ViewState.CurrentlySelectedDLFolder == null)
                            DownloadService.Instance.ResumeAllPaused();
                        else
                            DownloadService.Instance.ResumePausedFromFolder(ViewState.CurrentlySelectedDLFolder);
                    });
                    return true;
                case Resource.Id.retry_all_failed:
                    RetryAllConditionEntry(true, false);
                    return true;
                case Resource.Id.batch_select:
                    TransfersActionModeCallback = new ActionModeCallback() { Frag = this };
                    ForceOutIfZeroSelected = false;
                    TransfersActionMode = SeekerState.MainActivityRef.StartSupportActionMode(TransfersActionModeCallback);
                    recyclerTransferAdapter.IsInBatchSelectMode = true;
                    recyclerTransferAdapter.NotifyDataSetChanged();
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
            var listTi = batchSelectedOnly ? TransferItems.TransferItemManagerDL.GetBatchSelectedForRetryCondition(ViewState.CreateDLUIState(), failed) : null;
            Logger.InfoFirebase("retry all failed Pressed batch? " + batchSelectedOnly);
            SessionService.Instance.RunWithReconnect(() =>
            {
                if (batchSelectedOnly)
                {
                    DownloadService.Instance.DownloadRetryAll(listTi);
                }
                else if (ViewState.CurrentlySelectedDLFolder == null)
                {
                    if (failed) DownloadService.Instance.RetryAllFailed();
                    else        DownloadService.Instance.ResumeAllPaused();
                }
                else
                {
                    var folder = ViewState.CurrentlySelectedDLFolder;
                    if (failed) DownloadService.Instance.RetryFailedFromFolder(folder);
                    else        DownloadService.Instance.ResumePausedFromFolder(folder);
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
                    noTransfersIcon.SetImageResource(Resource.Drawable.upload_48dp);
                    noTransfersHeader.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads);
                    if (SharedFileService.MeetsSharingConditions())
                    {
                        noTransfersSubtitle.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads_subtitle);
                        setupUpSharing.Visibility = ViewStates.Gone;
                    }
                    else if (PreferencesState.SharingOn && UploadDirectoryManager.UploadDirectories.Count != 0 && UploadDirectoryManager.AreAllFailed())
                    {
                        noTransfersSubtitle.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads_not_sharing_subtitle);
                        setupUpSharing.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        noTransfersSubtitle.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_uploads_not_sharing_subtitle);
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
                    noTransfersIcon.SetImageResource(Resource.Drawable.download_48);
                    noTransfersHeader.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_downloads);
                    noTransfersSubtitle.Text = SeekerState.ActiveActivityRef.GetString(Resource.String.no_downloads_subtitle);
                }
            }
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            StaticHacks.TransfersFrag = this;
            HasOptionsMenu = true;
            Logger.Debug("TransfersFragment OnCreateView");
            scheduledRebuild ??= RunScheduledRebuild;
            this.rootView = inflater.Inflate(Resource.Layout.transfers, container, false);
            recyclerViewTransferItems = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerView1);
            this.noTransfers = rootView.FindViewById<View>(Resource.Id.noTransfersView);
            this.noTransfersIcon = rootView.FindViewById<ImageView>(Resource.Id.noTransfersIcon);
            this.noTransfersHeader = rootView.FindViewById<TextView>(Resource.Id.noTransfersHeader);
            this.noTransfersSubtitle = rootView.FindViewById<TextView>(Resource.Id.noTransfersSubtitle);
            this.setupUpSharing = rootView.FindViewById<Button>(Resource.Id.setUpSharing);
            this.setupUpSharing.Click += SetupUpSharing_Click;

            this.RegisterForContextMenu(recyclerViewTransferItems); //doesnt work for recycle views

            SetNoTransfersMessage();

            recycleLayoutManager = new CustomLinearLayoutManager(Activity);
            SetRecyclerAdapter();

            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);
            recyclerViewTransferItems.AddItemDecoration(new AndroidX.RecyclerView.Widget.DividerItemDecoration(Activity, AndroidX.RecyclerView.Widget.DividerItemDecoration.Vertical));

            // default cross fade animation looks glitchy (i.e. when updating status of item). this doesnt affect item add/remove animations.
            if (recyclerViewTransferItems.GetItemAnimator() is SimpleItemAnimator itemAnimator)
            {
                itemAnimator.SupportsChangeAnimations = false;
            }

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

        public override void OnDestroyView()
        {
            // a scheduled rebuild that lands after this is a no-op
            rootView = null;
            base.OnDestroyView();
        }

        // the first visible row and its offset from the top of the list
        private ITransferItem GetScrollAnchor(out int position, out int offset)
        {
            var lm = (LinearLayoutManager)recycleLayoutManager;
            position = lm.FindFirstVisibleItemPosition();
            offset = 0;
            if (position == RecyclerView.NoPosition)
            {
                return null;
            }
            View v = lm.FindViewByPosition(position);
            if (v != null)
            {
                offset = lm.GetDecoratedTop(v) - lm.PaddingTop;
            }
            return recyclerTransferAdapter?.GetItem(position);
        }

        public void SaveScrollPositionOnMovingIntoFolder()
        {
            ViewState.ScrollAnchorBeforeMovingIntoFolder = GetScrollAnchor(out int position, out int offset);
            ViewState.ScrollPositionBeforeMovingIntoFolder = position;
            ViewState.ScrollOffsetBeforeMovingIntoFolder = offset;
        }

        public void RestoreScrollPosition()
        {
            // by identity: folders may have been added or removed while we were inside one
            int position = recyclerTransferAdapter.PositionOf(ViewState.ScrollAnchorBeforeMovingIntoFolder);
            if (position < 0)
            {
                position = Math.Min(ViewState.ScrollPositionBeforeMovingIntoFolder, recyclerTransferAdapter.ItemCount - 1);
            }
            ViewState.ScrollAnchorBeforeMovingIntoFolder = null;
            if (position < 0)
            {
                return;
            }
            ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(position, ViewState.ScrollOffsetBeforeMovingIntoFolder);
        }

        // TODO2026
        public static ActionModeCallback TransfersActionModeCallback = null;
        public static AndroidX.AppCompat.View.ActionMode TransfersActionMode = null;

        public void SetRecyclerAdapter(bool restoreState = false)
        {
            int prevScrollPos = 0;
            int scrollOffset = 0;
            if (restoreState)
            {
                GetScrollAnchor(out prevScrollPos, out scrollOffset);
            }

            TransferItemManager source = TransferItems.TransferItemManagerWrapped.GetManagerForCurrentMode();
            TransferUIState uiState = ViewState.CreateUIState();
            if (ViewState.GroupByFolder && !ViewState.CurrentlyInFolder())
            {
                recyclerTransferAdapter = new TransferAdapterRecyclerFolderItem(source, uiState);
            }
            else
            {
                recyclerTransferAdapter = new TransferAdapterRecyclerIndividualItem(source, uiState);
            }
            recyclerTransferAdapter.TransfersFragment = this;
            recyclerTransferAdapter.IsInBatchSelectMode = (TransfersActionMode != null);
            recyclerViewTransferItems.SetAdapter(recyclerTransferAdapter);

            if (restoreState && prevScrollPos >= 0)
            {
                ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(prevScrollPos, scrollOffset);
            }
        }

        // Re-reads the manager into the adapter (i.e. recreating the snapshot)
        public void RebuildList()
        {
            if (rootView == null || recyclerTransferAdapter == null)
            {
                return;
            }
            SetNoTransfersMessage();
            ITransferItem anchor = GetScrollAnchor(out int anchorPos, out int anchorOffset);
            // if at the very top stay there (so we can see rows added above us)
            if (anchorPos == 0 && anchorOffset >= 0)
            {
                anchor = null;
            }
            recyclerTransferAdapter.Rebuild();
            if (anchor != null)
            {
                int newPos = recyclerTransferAdapter.PositionOf(anchor);
                if (newPos >= 0 && newPos != anchorPos)
                {
                    ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(newPos, anchorOffset);
                }
            }
        }

        // any thread
        public void ScheduleRebuild()
        {
            Action rebuild = scheduledRebuild;
            if (rebuild == null || Interlocked.CompareExchange(ref rebuildPending, 1, 0) != 0)
            {
                return;
            }
            rebuildHandler.PostDelayed(rebuild, REBUILD_COALESCE_MS);
        }

        private void RunScheduledRebuild()
        {
            Interlocked.Exchange(ref rebuildPending, 0);
            RebuildList();
        }

        // a row whose item changed in place. in folder view a file's row is its folder
        private void RepaintTransferRow(TransferItem ti)
        {
            var adapter = recyclerTransferAdapter;
            if (rootView == null || adapter == null || ti == null)
            {
                return;
            }
            if (!adapter.RepaintRow(ti))
            {
                adapter.RepaintRow(ti.Folder);
            }
        }

        // same as RepaintTransferRow, but a folder row is repainted once no matter how many of its files changed
        private void RepaintTransferRows(IEnumerable<TransferItem> items)
        {
            var adapter = recyclerTransferAdapter;
            if (rootView == null || adapter == null)
            {
                return;
            }
            var foldersDone = new HashSet<FolderItem>();
            foreach (TransferItem ti in items)
            {
                if (ti == null || adapter.RepaintRow(ti))
                {
                    continue;
                }
                if (ti.Folder != null && foldersDone.Add(ti.Folder))
                {
                    adapter.RepaintRow(ti.Folder);
                }
            }
        }

        // for an item the caller just removed from its manager
        private void RemoveRow(ITransferItem item)
        {
            recyclerTransferAdapter.RemoveRow(item);
            SetNoTransfersMessage();
        }

        /// <summary>
        /// This is a UI event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TransferQueueStateChanged(object sender, TransferItem e)
        {
            SeekerApplication.RunOnUIThread(() => RepaintTransferRow(e));
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
            ViewState.SwitchToUploadsMode();
            PreferencesManager.SaveTransferViewInUploadsMode();
            this.RefreshForModeSwitch();
            SeekerState.MainActivityRef.InvalidateOptionsMenu();
        }

        public override void OnPause()
        {
            base.OnPause();
            Logger.Debug("TransferFragment OnPause");
        }

        private void OnTransferItemsChanged(object sender, IReadOnlyList<TransferItem> items)
        {
            RepaintTransferRows(items);
        }

        private void OnTransferListRefreshRequested(object sender, EventArgs e)
        {
            ScheduleRebuild();
        }


        private void DownloadRetryLogic(TransferItem transferItem)
        {
            string chosenFname = transferItem.FullFilename; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            string chosenUname = transferItem.Username; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            Logger.Debug("chosenFname? " + chosenFname);
            TransferItem item1 = TransferItems.TransferItemManagerDL.GetTransferItemWithIndexFromAll(chosenFname, chosenUname, out int _);
            Logger.Debug("item1 is null?" + (item1 == null).ToString());//tested
            if (item1 == null)
            {
                SeekerApplication.Toaster.ShowToast(SeekerState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
                return;
            }

            try
            {
                DownloadService.Instance.RetryDownloadItem(item1);
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
            item1.ClearStateForRetry();
            SeekerApplication.RunOnUIThread(() => RepaintTransferRow(item1));
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

                if (recyclerTransferAdapter.PositionOf(ti) == -1)
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

                        SessionService.Instance.RunWithReconnect(() => DownloadRetryLogic(ti as TransferItem));
                        //Toast.MakeText(Applicatio,"Retrying...",ToastLength.Short).Show();
                        break;
                    case TransferContextMenuItem.ClearFromList:
                        Logger.InfoFirebase("Clear Complete item pressed");
                        if (ViewState.InUploadsMode)
                        {
                            // Uploads have no incomplete file to clean up.
                            if (ti is TransferItem tiUpload)
                            {
                                TransferItems.TransferItemManagerWrapped.Remove(tiUpload);
                            }
                            else if (ti is FolderItem fiUpload)
                            {
                                TransferItems.TransferItemManagerWrapped.ClearAllFromFolder(fiUpload);
                            }
                        }
                        else
                        {
                            if (ti is TransferItem tiDownload)
                            {
                                TransferItems.TransferItemManagerWrapped.RemoveAndCleanUp(tiDownload);
                            }
                            else if (ti is FolderItem fiDownload)
                            {
                                TransferItems.TransferItemManagerWrapped.RemoveAndCleanUp(fiDownload);
                            }
                        }
                        RemoveRow(ti);
                        break;
                    case TransferContextMenuItem.CancelAndClear: //cancel and clear (downloads) OR abort and clear (uploads)
                        Logger.InfoFirebase("Cancel and Clear item pressed");
                        if (ti is TransferItem tti)
                        {
                            TransferState.CancelAndRemoveToken(tti);
                            // TODO MOVE
                            if (ViewState.InUploadsMode)
                            {
                                TransferItems.TransferItemManagerWrapped.Remove(tti);
                            }
                            else
                            {
                                TransferItems.TransferItemManagerWrapped.RemoveAndCleanUp(tti); //this means basically, wait for the stream to be closed. no race conditions..
                            }
                            RemoveRow(ti);
                        }
                        else if (ti is FolderItem fi)
                        {
                            TransferItems.TransferItemManagerWrapped.CancelFolder(fi, true);
                            TransferItems.TransferItemManagerWrapped.ClearAllFromFolderAndClean(fi);
                            RemoveRow(ti);
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

                        //the folder implementation will re-request all queued files
                        //recently I thought of just doing the lowest, but then if the lowest is ready, it will download leaving the other transfers behind.

                        // TODO MOVE
                        if (ti is TransferItem tiQueue)
                        {
                            GetQueuePosition(tiQueue);
                        }
                        else if (ti is FolderItem folderItem)
                        {
                            lock (folderItem.TransferItems)
                            {
                                foreach (TransferItem transferItem in folderItem.TransferItems.Where(ti => ti.State.HasFlag(TransferStates.Queued) && ti.State.HasFlag(TransferStates.Remotely)))
                                {
                                    GetQueuePosition(transferItem);
                                }
                            }
                        }
                        break;
                    case TransferContextMenuItem.PlayFile:
                        TransferItem tiPlay = ti as TransferItem;
                        try
                        {
                            //tested on API25 and API30
                            //AndroidX.Core.Content.FileProvider
                            Android.Net.Uri uriToUse = null;
                            if (PlatformInfo.UseLegacyStorage() && SimpleHelpers.IsFileUri(tiPlay.FinalUri)) //i.e. if it is a FILE URI.
                            {
                                uriToUse = AndroidX.Core.Content.FileProvider.GetUriForFile(this.Context, this.Context.ApplicationContext.PackageName + ".provider", new Java.IO.File(Android.Net.Uri.Parse(tiPlay.FinalUri).Path));
                            }
                            else
                            {
                                uriToUse = Android.Net.Uri.Parse(tiPlay.FinalUri);
                            }
                            Intent playFileIntent = new Intent(Intent.ActionView);
                            //playFileIntent.SetDataAndType(uriToUse,"audio/*");
                            playFileIntent.SetDataAndType(uriToUse, MediaMimeTypes.GetMimeTypeFromFilename(tiPlay.FullFilename));   //works
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
                            BrowseService.RequestFilesApi(ttti.Username, startingDir);
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
                            BrowseService.RequestFilesApi(fi.Username, startingDir);
                        }
                        break;
                    case TransferContextMenuItem.ResumeFolder: //resume folder
                        if (NotLoggedInShowMessageGaurd("resume folder"))
                        {
                            return true;
                        }
                        Logger.InfoFirebase("resume folder Pressed");
                        SessionService.Instance.RunWithReconnect(() => DownloadService.Instance.ResumePausedFromFolder(ti as FolderItem));
                        break;
                    case TransferContextMenuItem.PauseFolderOrAbortUploads: //pause folder or abort uploads (uploads)
                        TransferItems.TransferItemManagerWrapped.CancelFolder(ti as FolderItem);
                        recyclerTransferAdapter.RepaintRow(ti);
                        break;
                    case TransferContextMenuItem.RetryFailedFiles: //retry failed downloads from folder
                        if (NotLoggedInShowMessageGaurd("retry folder"))
                        {
                            return true;
                        }
                        Logger.InfoFirebase("retry folder Pressed");
                        SessionService.Instance.RunWithReconnect(() => DownloadService.Instance.RetryFailedFromFolder(ti as FolderItem));
                        break;
                    case TransferContextMenuItem.AbortUpload: //abort upload
                        Logger.InfoFirebase("Abort Upload item pressed");
                        TransferItem uploadToCancel = ti as TransferItem;
                        TransferState.CancelAndRemoveToken(uploadToCancel);
                        recyclerTransferAdapter.RepaintRow(ti);
                        break;
                    case TransferContextMenuItem.IgnoreUnshareUser: //ignore (unshare) user
                        Logger.InfoFirebase("Unshare User item pressed");
                        IEnumerable<TransferItem> tItems = TransferItems.TransferItemManagerWrapped.GetTransferItemsForUser(ti.GetUsername());
                        foreach (var tiToCancel in tItems)
                        {
                            TransferState.CancelAndRemoveToken(tiToCancel);
                        }
                        RepaintTransferRows(tItems);
                        UserListService.AddToIgnoreListFeedback(SeekerState.ActiveActivityRef, ti.GetUsername());
                        break;
                }
            }
            return base.OnContextItemSelected(item);
        }

        public void EnterBatchSelect(int position)
        {
            if (TransfersActionMode != null)
            {
                return;
            }
            TransfersActionModeCallback = new ActionModeCallback() { Frag = this };
            ForceOutIfZeroSelected = true;
            TransfersActionMode = SeekerState.MainActivityRef.StartSupportActionMode(TransfersActionModeCallback);
            recyclerTransferAdapter.IsInBatchSelectMode = true;
            recyclerTransferAdapter.NotifyDataSetChanged();
            ToggleItemBatchSelect(recyclerTransferAdapter, position);
        }

        public static bool ForceOutIfZeroSelected = true;
        public static void ToggleItemBatchSelect(TransferAdapterRecyclerVersion recyclerTransferAdapter, int pos)
        {
            // Row clicks are posted to the main looper, so one can arrive after the action
            // mode was already finished and nulled out. Ignore it.
            if (TransfersActionMode == null)
            {
                return;
            }
            var item = recyclerTransferAdapter.GetItem(pos);
            if (item == null)
            {
                return;
            }
            if (!ViewState.BatchSelectedItems.Add(item))
            {
                // already present → toggle off
                ViewState.BatchSelectedItems.Remove(item);
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
            int prevCount = ViewState.BatchSelectedItems.Count;

            // Direct case (flat or in-folder view): ti itself was selected.
            ViewState.BatchSelectedItems.Remove(ti);

            // Folder-grouped view: ti's parent folder may have been selected and may now be
            // empty/removed. Drop any FolderItem that no longer exists in the manager.
            ViewState.BatchSelectedItems.RemoveWhere(item =>
                item is FolderItem fi && !TransferItems.TransferItemManagerWrapped.ContainsFolder(fi));

            if (ViewState.BatchSelectedItems.Count == prevCount)
            {
                //it is not currently on our screen, perhaps it is in uploads (and we are in downloads) or we are inside a folder (and it is outside)
                Logger.Debug("batch on, different screen item removed");
                return;
            }
            Logger.Debug("batch on, updating selection");
            if (ViewState.BatchSelectedItems.Count == 0)
            {
                TransfersActionMode.Finish();
            }
            else
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
                        SeekerApplication.RunOnUIThread(() => RepaintTransferRow(t));
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

        private void PaintProgress(TransferItem relevantItem, bool wasFailed)
        {
            var adapter = recyclerTransferAdapter;
            if (rootView == null || adapter == null)
            {
                return;
            }
            // the file's own row, else (folder view) its folder's row. not bound = off screen, the next bind reads the live fields
            ITransferItemView v = adapter.GetBoundView(relevantItem) ?? adapter.GetBoundView(relevantItem.Folder);
            if (v == null)
            {
                return;
            }
            if (v is TransferItemViewFolder folderView)
            {
                var fi = v.InnerTransferItem as FolderItem;
                var folderState = fi.GetState(out _, out _);
                TransferViewHelper.SetAdditionalStatusText(v.GetStatusDot(), v.GetAdditionalStatusInfoView(), v.GetSizeTextView(), v.GetSpeedTextView(), fi, folderState, v.GetShowProgressSize(), v.GetShowSpeed(), isFolder: true);
                TransferViewHelper.SetTimeRemainingText(v.GetTimeRemainingSeparatorView(), v.GetTimeRemainingTextView(), v.GetSpeedTextView(), fi, v.GetShowTimeRemaining());
                TransferViewHelper.UpdateSegmentedProgressBar(folderView.segmentedProgressBar, fi);
            }
            else
            {
                v.progressBar.Progress = relevantItem.GetProgressForPresentation();
                TransferViewHelper.SetAdditionalStatusText(v.GetStatusDot(), v.GetAdditionalStatusInfoView(), v.GetSizeTextView(), v.GetSpeedTextView(), relevantItem, relevantItem.State, v.GetShowProgressSize(), v.GetShowSpeed());
                TransferViewHelper.SetTimeRemainingText(v.GetTimeRemainingSeparatorView(), v.GetTimeRemainingTextView(), v.GetSpeedTextView(), relevantItem, v.GetShowTimeRemaining());
                TransferViewHelper.SetProgressBarTint(v.progressBar, relevantItem.State, wasFailed, relevantItem.IsUpload());
            }
        }

        // on a transfer thread
        private void TransferProgressUpdated(object sender, ProgressUpdatedUIEventArgs e)
        {
            if (e.TransferItem.IsUpload() != ViewState.InUploadsMode)
            {
                return;
            }
            if (e.PercentComplete == 0)
            {
                return;
            }
            try
            {
                TransferItem ti = e.TransferItem;
                // throttle per painted row: in folder view every file of a folder paints the one folder row
                FolderItem folderRow = (ViewState.GroupByFolder && !ViewState.CurrentlyInFolder()) ? ti.Folder : null;
                long now = System.Environment.TickCount64;
                long last = folderRow != null ? folderRow.LastProgressPostMs : ti.LastProgressPostMs;
                if (!e.WasFailed && now - last < THROTTLE_PROGRESS_UPDATED_RATE)
                {
                    return;
                }
                if (folderRow != null)
                {
                    folderRow.LastProgressPostMs = now;
                }
                else
                {
                    ti.LastProgressPostMs = now;
                }

                bool wasFailed = e.WasFailed;
                SeekerApplication.RunOnUIThread(() => PaintProgress(ti, wasFailed));
            }
            catch (System.Exception error)
            {
                Logger.Firebase(error.Message + " partial update");
            }
        }

        private void TransferStateChangedItem(object sender, TransferItem ti)
        {
            SeekerApplication.RunOnUIThread(() => RepaintTransferRow(ti));
        }


        public override void OnStart()
        {
            Seeker.Transfers.TransferEventRouter.StateChangedForItem += TransferStateChangedItem;
            Seeker.Transfers.TransferEventRouter.ProgressUpdated += TransferProgressUpdated;
            Seeker.Transfers.TransferEventRouter.ItemRemoved += OnRouterItemRemoved;
            UploadService.TransferAddedUINotify += MainActivity_TransferAddedUINotify; //todo this should eventually be for downloads too.
            DownloadService.Instance.TransferItemQueueUpdated += TransferQueueStateChanged;
            DownloadService.Instance.TransferItemsChanged += OnTransferItemsChanged;
            DownloadService.Instance.TransferListRefreshRequested += OnTransferListRefreshRequested;
            DownloadService.Instance.DownloadAddedUINotify += SeekerState_DownloadAddedUINotify;

            if (recyclerTransferAdapter != null)
            {
                RebuildList();
            }

            base.OnStart();
        }

        private void MainActivity_TransferAddedUINotify(object sender, TransferItem e)
        {
            if (e.IsUpload() == ViewState.InUploadsMode)
            {
                ScheduleRebuild();
            }
        }

        public override void OnStop()
        {
            Seeker.Transfers.TransferEventRouter.ProgressUpdated -= TransferProgressUpdated;
            Seeker.Transfers.TransferEventRouter.StateChangedForItem -= TransferStateChangedItem;
            Seeker.Transfers.TransferEventRouter.ItemRemoved -= OnRouterItemRemoved;
            DownloadService.Instance.TransferItemQueueUpdated -= TransferQueueStateChanged;
            UploadService.TransferAddedUINotify -= MainActivity_TransferAddedUINotify;
            DownloadService.Instance.TransferItemsChanged -= OnTransferItemsChanged;
            DownloadService.Instance.TransferListRefreshRequested -= OnTransferListRefreshRequested;
            DownloadService.Instance.DownloadAddedUINotify -= SeekerState_DownloadAddedUINotify;
            base.OnStop();
        }

        // UI thread, right after the removal
        private void OnRouterItemRemoved(object sender, TransferItem ti)
        {
            var adapter = recyclerTransferAdapter;
            if (rootView == null || adapter == null)
            {
                return;
            }
            if (!adapter.RemoveRow(ti) && ti.Folder != null)
            {
                // folder view: the folder row lost a file, or went away with its last one
                if (ti.Folder.IsEmpty())
                {
                    adapter.RemoveRow(ti.Folder);
                }
                else
                {
                    adapter.RepaintRow(ti.Folder);
                }
            }
            SetNoTransfersMessage();
        }

        private void SeekerState_DownloadAddedUINotify(object sender, DownloadAddedEventArgs e)
        {
            if (!ViewState.InUploadsMode)
            {
                ScheduleRebuild();
            }
        }

    }
}