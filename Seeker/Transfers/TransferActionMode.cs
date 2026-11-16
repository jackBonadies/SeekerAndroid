using Android.Views;
using Seeker.Helpers;
using Seeker.Helpers.AnchoredMenu;
using Seeker.Transfers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

using ActionMode = AndroidX.AppCompat.View.ActionMode;

namespace Seeker
{
    public partial class TransfersFragment
    {
        public class ActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            public TransfersFragment Frag;
            // always the fragment's current adapter
            private TransferAdapterRecyclerVersion Adapter => Frag.recyclerTransferAdapter;
            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.transfers_menu_batch, menu);
                OverflowMenu.Attach(menu, Resource.Id.action_transfers_batch_overflow, () => BuildOverflowMenu(mode), dismissWithAnchor: true);
                UiHelpers.ApplyActionModeStatusBarInset(SeekerState.ActiveActivityRef);
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                menu.FindItem(Resource.Id.action_cancel_and_clear_all_batch).SetVisible(ViewState.BatchSelectedItems.Count != 0);
                return false;
            }

            private AnchoredMenuConfig BuildOverflowMenu(ActionMode mode)
            {
                if (TransfersActionMode == null)
                {
                    return null;
                }
                var ctx = Frag.Context ?? SeekerState.ActiveActivityRef;
                var config = new AnchoredMenuConfig();
                // uploads: the only thing you can do is clear and abort the selected (the bar's trash icon)
                if (!ViewState.InUploadsMode)
                {
                    List<TransferItem> transfersSelected = new List<TransferItem>();
                    foreach (ITransferItem iti in ViewState.BatchSelectedItems)
                    {
                        if (iti is TransferItem singleTi)
                        {
                            transfersSelected.Add(singleTi);
                        }
                        else if (iti is FolderItem folderTi)
                        {
                            transfersSelected.AddRange(folderTi.TransferItems);
                        }
                    }
                    TransferViewHelper.GetStatusNumbers(transfersSelected, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued);

                    if (numPaused != 0)
                    {
                        AddRow(config, Resource.Drawable.play_arrow_24, ctx.GetString(Resource.String.ResumeSelected), () => IfActive(() =>
                        {
                            Frag.RetryAllConditionEntry(false, true);
                            NotifyChangedAndClear(Adapter);
                            mode.Finish();
                        }));
                    }
                    if (numInProgress != 0 || numQueued != 0)
                    {
                        AddRow(config, Resource.Drawable.paused, ctx.GetString(Resource.String.PauseSelected), () => IfActive(() =>
                        {
                            TransferItems.TransferItemManagerWrapped.CancelSelectedItems(false);
                            NotifyChangedAndClear(Adapter);
                            Frag.ScheduleRebuildIfMutableSort();
                            //since all selected stuff is going away. its what Gmail action mode does.
                            mode.Finish();
                        }));
                    }
                    if (numFailed != 0)
                    {
                        AddRow(config, Resource.Drawable.baseline_refresh_white_24, ctx.GetString(Resource.String.RetryFailed), () => IfActive(() =>
                        {
                            Frag.RetryAllConditionEntry(true, true);
                            NotifyChangedAndClear(Adapter);
                            mode.Finish();
                        }));
                    }
                    config.AddDivider();
                }
                AddRow(config, Resource.Drawable.select_all_24, ctx.GetString(Resource.String.SelectAll), () => IfActive(() => SelectAll(mode)));
                AddRow(config, Resource.Drawable.ink_selection_24, ctx.GetString(Resource.String.InvertSelection), () => IfActive(() => InvertSelection(mode)));
                return config;
            }

            // the mode can finish while the menu is open (a transfer completing clears the last selection)
            private static void IfActive(Action action)
            {
                if (TransfersActionMode != null)
                {
                    action();
                }
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                // Clicks are posted to the main looper, so one can arrive after the action
                // mode was already finished (double-tap, a transfer completing and clearing
                // the last selection, a page swipe). Ignore it rather than act on a dead mode.
                if (TransfersActionMode == null)
                {
                    return true;
                }
                switch (item.ItemId)
                {
                    //this is the only option that uploads gets
                    case Resource.Id.action_cancel_and_clear_all_batch:
                        Logger.InfoFirebase("action_cancel_and_clear_batch Pressed");
                        TransferDebouncer.CancelAndClearAll.Trigger();
                        var removed = ViewState.BatchSelectedItems.ToList();
                        TransferItems.TransferItemManagerWrapped.CancelSelectedItems(true);
                        TransferItems.TransferItemManagerWrapped.ClearSelectedItemsAndClean();
                        ViewState.BatchSelectedItems.Clear();
                        foreach (ITransferItem iti in removed)
                        {
                            Frag.RemoveRow(iti);
                        }
                        //since all selected stuff is going away. its what Gmail action mode does.
                        mode.Finish();
                        break;
                }
                return true;
            }

            private void SelectAll(ActionMode mode)
            {
                ViewState.BatchSelectedItems.Clear();
                foreach (ITransferItem iti in Adapter.Items)
                {
                    ViewState.BatchSelectedItems.Add(iti);
                }
                int cnt = ViewState.BatchSelectedItems.Count;

                Adapter.NotifyDataSetChanged();

                mode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                mode.Invalidate();
            }

            private void InvertSelection(ActionMode mode)
            {
                ForceOutIfZeroSelected = false;
                var inverted = new HashSet<ITransferItem>();
                foreach (ITransferItem iti in Adapter.Items)
                {
                    if (!ViewState.BatchSelectedItems.Contains(iti))
                    {
                        inverted.Add(iti);
                    }
                }
                ViewState.BatchSelectedItems.Clear();
                foreach (var iti in inverted)
                {
                    ViewState.BatchSelectedItems.Add(iti);
                }

                Adapter.NotifyDataSetChanged();

                mode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), ViewState.BatchSelectedItems.Count.ToString());
                mode.Invalidate();
            }

            private static void AddRow(AnchoredMenuConfig config, int iconResId, string label, Action onClick)
            {
                config.Rows.Add(new AnchoredMenuRow
                {
                    IconResId = iconResId,
                    Label = label,
                    OnClick = onClick,
                });
            }

            // for actions (pause/resume/retry) that mutate items in place: clear the selection and repaint those rows
            private static void NotifyChangedAndClear(TransferAdapterRecyclerVersion adapter)
            {
                var selected = ViewState.BatchSelectedItems.ToList();
                ViewState.BatchSelectedItems.Clear();
                foreach (ITransferItem iti in selected)
                {
                    adapter.RepaintRow(iti);
                }
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                SeekerState.ActiveActivityRef?.Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);

                TransfersActionMode = null;
                ViewState.BatchSelectedItems.Clear();
                this.Adapter.IsInBatchSelectMode = false;
                this.Adapter.NotifyDataSetChanged();
            }

        }
    }
}
