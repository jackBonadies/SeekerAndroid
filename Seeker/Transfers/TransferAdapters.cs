using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Seeker.Helpers;
using Seeker.Search;
using Soulseek;
using System;

using Common;
namespace Seeker
{
    public partial class TransfersFragment
    {
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
                    if (ViewState.InUploadsMode)
                    {
                        ViewState.CurrentlySelectedUploadFolder = f;
                    }
                    else
                    {
                        ViewState.CurrentlySelectedDLFolder = f;
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
                Logger.Debug($"position: {position} ti name: {iti.GetDisplayName()}");

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
                    Logger.InfoFirebase("selected item was set as null");
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
                showSpeed = PreferencesState.TransferViewShowSpeed;
                showSizes = PreferencesState.TransferViewShowSizes;
            }

        }

        public enum TransferContextMenuItem
        {
            RetryResumeDownload = 0,
            ClearFromList = 1,
            CancelAndClear = 2,
            RefreshQueuePosition = 3,
            PlayFile = 4,
            UserOptions = 5,
            BrowseUser = 6,
            BrowseAtLocation = 7,
            SearchUserFiles = 8,
            AddRemoveUser = 9,
            MessageUser = 10,
            GetUserInfo = 11,
            UserNote = 12,
            GivePrivileges = 13,
            ResumeFolder = 100,
            PauseFolderOrAbortUploads = 101,
            RetryFailedFiles = 102,
            AbortUpload = 103,
            IgnoreUnshareUser = 104,
            BatchSelect = 105,
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
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.RetryResumeDownload, 0, Resource.String.resume_dl);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.RetryResumeDownload, 0, Resource.String.retry_dl);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && folderItemState.HasFlag(TransferStates.Cancelled)  /*&& fi.GetFolderProgress() > 0*/)
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.ResumeFolder, 0, Resource.String.ResumeFolder);
                        }
                        else if (tvh != null && fi != null && (!folderItemState.HasFlag(TransferStates.Completed) && !folderItemState.HasFlag(TransferStates.Succeeded) && !folderItemState.HasFlag(TransferStates.Errored) && !folderItemState.HasFlag(TransferStates.TimedOut) && !folderItemState.HasFlag(TransferStates.Rejected)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.PauseFolderOrAbortUploads, 0, Resource.String.PauseFolder);
                        }
                    }
                }
                else
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && !(SimpleHelpers.IsUploadCompleteOrAborted(ti.State)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.AbortUpload, 0, Resource.String.AbortUpload);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && !(SimpleHelpers.IsUploadCompleteOrAborted(folderItemState))) ;
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.PauseFolderOrAbortUploads, 0, Resource.String.AbortUploads);
                        }
                    }
                }
                if (!isUpload)
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (ti.State.HasFlag(TransferStates.Succeeded)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.ClearFromList, 1, Resource.String.clear_from_list);
                            //if completed then we dont need to show the cancel option...
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.CancelAndClear, 2, Resource.String.cancel_and_clear);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && (folderItemState.HasFlag(TransferStates.Succeeded)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.ClearFromList, 1, Resource.String.clear_from_list);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.CancelAndClear, 2, Resource.String.cancel_and_clear);
                        }
                    }
                }
                else
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (SimpleHelpers.IsUploadCompleteOrAborted(ti.State)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.ClearFromList, 1, Resource.String.clear_from_list);
                            //if completed then we dont need to show the cancel option...
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.CancelAndClear, 2, Resource.String.AbortandClearUpload);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && (SimpleHelpers.IsUploadCompleteOrAborted(folderItemState)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.ClearFromList, 1, Resource.String.clear_from_list);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.CancelAndClear, 2, Resource.String.AbortandClearUploads);
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
                                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.RefreshQueuePosition, 3, Resource.String.refresh_queue_pos);
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
                                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.RefreshQueuePosition, 3, Resource.String.refresh_queue_pos);
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
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.PlayFile, 4, Resource.String.play_file);
                        }
                    }
                    else
                    {
                        if (folderItemState.HasFlag(TransferStates.TimedOut) || folderItemState.HasFlag(TransferStates.Rejected) || folderItemState.HasFlag(TransferStates.Errored) || anyFailed)
                        {
                            //no op
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.RetryFailedFiles, 4, Resource.String.RetryFailedFiles);
                        }
                    }
                }
                var subMenu = menu.AddSubMenu(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.UserOptions, 5, Resource.String.UserOptions);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.BrowseUser, 6, Resource.String.browse_user);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.BrowseAtLocation, 7, Resource.String.browse_at_location);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.SearchUserFiles, 8, Resource.String.search_user_files);
                CommonHelpers.AddAddRemoveUserMenuItem(subMenu, UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.AddRemoveUser, 9, tvh.InnerTransferItem.GetUsername(), false);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.MessageUser, 10, Resource.String.msg_user);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.GetUserInfo, 11, Resource.String.get_user_info);
                CommonHelpers.AddUserNoteMenuItem(subMenu, UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.UserNote, 12, tvh.InnerTransferItem.GetUsername());
                CommonHelpers.AddGivePrivilegesIfApplicable(subMenu, (int)TransferContextMenuItem.GivePrivileges);

                if (isUpload)
                {
                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.IgnoreUnshareUser, 6, Resource.String.IgnoreUnshareUser);
                }
                //finally batch selection mode
                menu.Add(UNIQUE_TRANSFER_GROUP_ID, (int)TransferContextMenuItem.BatchSelect, 16, Resource.String.BatchSelect);

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
    }
}
