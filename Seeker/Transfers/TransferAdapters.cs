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
using System.Collections.Generic;

using Common;
namespace Seeker
{
    public partial class TransfersFragment
    {
        public class TransferAdapterRecyclerIndividualItem : TransferAdapterRecyclerVersion
        {
            public TransferAdapterRecyclerIndividualItem(TransferItemManager source, TransferUIState uiState) : base(source, uiState)
            {
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                ITransferItemView view = TransferItemViewDetails.Create(parent, this.showSizes, this.showSpeed, this.showTimeRemaining);
                (view as View).Click += TransferAdapterRecyclerIndividualItem_Click;
                (view as View).LongClick += TransferAdapterRecyclerVersion_LongClick;
                return new TransferViewHolder(view as View);
            }

            private void TransferAdapterRecyclerIndividualItem_Click(object sender, EventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.BindingAdapterPosition);
                }
                else
                {
                    ShowItemContextMenu(sender as View);
                }
            }

        }

        public class TransferAdapterRecyclerFolderItem : TransferAdapterRecyclerVersion
        {
            public TransferAdapterRecyclerFolderItem(TransferItemManager source, TransferUIState uiState) : base(source, uiState)
            {
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                TransferItemViewFolder view = TransferItemViewFolder.Create(parent, this.showSizes, this.showSpeed, this.showTimeRemaining);
                view.Click += TransferAdapterRecyclerFolderItem_Click;
                view.LongClick += TransferAdapterRecyclerVersion_LongClick;
                view.FolderIndicator.Click += (sender, e) =>
                {
                    if (IsInBatchSelectMode)
                    {
                        ToggleItemBatchSelect(this, view.ViewHolder.BindingAdapterPosition);
                    }
                    else
                    {
                        ShowItemContextMenu(view);
                    }
                };
                return new TransferViewHolder(view);
            }

            private void TransferAdapterRecyclerFolderItem_Click(object sender, EventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.BindingAdapterPosition);
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
                    SeekerState.MainActivityRef.RefreshBackCallbackState();
                }
            }

        }

        // Shows the UI snapshot of the manager list (see TransferListSnapshot)
        public abstract class TransferAdapterRecyclerVersion : RecyclerView.Adapter
        {
            // underlying source list
            private readonly TransferItemManager source;
            private readonly TransferUIState uiState;
            // ui list (cloned from source list)
            private TransferListSnapshot snapshot;
            // item -> the view currently bound to it (for progress updates)
            private readonly Dictionary<ITransferItem, ITransferItemView> boundViews = new Dictionary<ITransferItem, ITransferItemView>();

            public override int ItemCount => snapshot.Count;
            protected ITransferItem selectedItem = null;
            public bool IsInBatchSelectMode;

            public TransfersFragment TransfersFragment;
#if DEBUG
            public void SelectedDebugInfo(ITransferItem iti)
            {

                int position = PositionOf(iti);
                Logger.Debug($"position: {position} ti name: {iti?.GetFolderName()}");

            }
#endif

            public IReadOnlyList<ITransferItem> Items => snapshot.Items;

            public ITransferItem GetItem(int position)
            {
                return snapshot.GetItem(position);
            }

            public int PositionOf(ITransferItem item)
            {
                return snapshot.IndexOf(item);
            }

            public void Rebuild()
            {
                snapshot = CreateSnapshot();
                NotifyDataSetChanged();
            }

            private TransferListSnapshot CreateSnapshot()
            {
                // sortKeys is a dict to detect when sort keys change (since unlike Alphabetical and DateAdded,
                //   a row's state and last action can change even if no new transfers are added / removed)
                Dictionary<ITransferItem, TransferBandKey> sortKeys = TransferSort.IsMutable(uiState.SortKey)
                    ? new Dictionary<ITransferItem, TransferBandKey>()
                    : null;
                return new TransferListSnapshot(source.GetUISnapshot(uiState, sortKeys), sortKeys, uiState.SortKey);
            }

            // the row's sort key changed since the list was sorted. false if row does not exist in this view.
            public bool NeedsResort(ITransferItem row)
            {
                return snapshot.HasSortKeyChanged(row);
            }

            // for a removal the caller just made in the manager. the rest of the manager's changes wait for the next Rebuild
            public bool RemoveRow(ITransferItem item)
            {
                int pos = PositionOf(item);
                if (pos < 0)
                {
                    return false;
                }
                snapshot.RemoveAt(pos);
                NotifyItemRemoved(pos);
                return true;
            }

            // repaints the item's row if a view is bound to it, if not bound, then the next bind reads the live fields
            //   thus it gets updated either way
            public bool RepaintRow(ITransferItem item)
            {
                ITransferItemView view = GetBoundView(item);
                if (view == null)
                {
                    return false;
                }
                view.setItem(item, this.IsInBatchSelectMode);
                return true;
            }

            public ITransferItemView GetBoundView(ITransferItem item)
            {
                if (item != null && boundViews.TryGetValue(item, out ITransferItemView view))
                {
                    return view;
                }
                return null;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                ITransferItemView view = (holder as TransferViewHolder).getTransferItemView();
                UnregisterBoundView(view);
                ITransferItem item = snapshot.GetItem(position);
                view.setItem(item, this.IsInBatchSelectMode);
                boundViews[item] = view;
            }

            public override void OnViewRecycled(Java.Lang.Object holder)
            {
                if (holder is TransferViewHolder transferHolder)
                {
                    UnregisterBoundView(transferHolder.getTransferItemView());
                }
                base.OnViewRecycled(holder);
            }

            private void UnregisterBoundView(ITransferItemView view)
            {
                ITransferItem previous = view.InnerTransferItem;
                // another holder may have been bound to that item since
                if (previous != null && boundViews.TryGetValue(previous, out ITransferItemView current) && current == view)
                {
                    boundViews.Remove(previous);
                }
            }

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

            protected void ShowItemContextMenu(View rowView)
            {
                ITransferItem item = (rowView as ITransferItemView).InnerTransferItem;
                setSelectedItem(item);
                TransfersFragment?.ShowTransferContextMenu(item);
            }

            protected void TransferAdapterRecyclerVersion_LongClick(object sender, View.LongClickEventArgs e)
            {
                int position = (sender as ITransferItemView).ViewHolder.BindingAdapterPosition;
                if (position == RecyclerView.NoPosition)
                {
                    return;
                }
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, position);
                }
                else
                {
                    TransfersFragment.EnterBatchSelect(position);
                }
            }

            protected readonly bool showSpeed = false;
            protected readonly bool showSizes = false;
            protected readonly bool showTimeRemaining = false;
            public TransferAdapterRecyclerVersion(TransferItemManager source, TransferUIState uiState)
            {
                this.source = source;
                this.uiState = uiState;
                snapshot = CreateSnapshot();
                showSpeed = PreferencesState.TransferViewShowSpeed;
                showSizes = PreferencesState.TransferViewShowSizes;
                showTimeRemaining = PreferencesState.TransferViewShowTimeRemaining;
            }

        }

        public class TransferViewHolder : RecyclerView.ViewHolder
        {
            private ITransferItemView transferItemView;


            public TransferViewHolder(View view) : base(view)
            {
                transferItemView = (ITransferItemView)view;
                transferItemView.ViewHolder = this;
            }

            public ITransferItemView getTransferItemView()
            {
                return transferItemView;
            }
        }
    }
}
