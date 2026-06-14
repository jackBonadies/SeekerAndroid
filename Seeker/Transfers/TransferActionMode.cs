using Android.Views;
using Seeker.Helpers;
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
            public TransferAdapterRecyclerVersion Adapter;
            public TransfersFragment Frag;
            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.transfers_menu_batch, menu);
                var activity = SeekerState.ActiveActivityRef;
                if (activity != null)
                {
                    var color = UiHelpers.GetColorFromAttribute(activity, Resource.Attribute.colorPrimary);
                    activity.Window?.SetStatusBarColor(color);
                }
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                if (ViewState.BatchSelectedItems.Count == 0)
                {
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all_batch).SetVisible(false);
                }
                else
                {
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all_batch).SetVisible(true);
                }


                if (ViewState.InUploadsMode)
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
                        // Capture positions BEFORE removal, descending so NotifyItemRemoved
                        // calls don't reference indices that have already shifted.
                        var removedPositions = ViewState.BatchSelectedItems
                            .Select(it => TransferItems.TransferItemManagerWrapped.GetUserIndexForITransferItem(it))
                            .Where(p => p >= 0)
                            .OrderByDescending(p => p)
                            .ToList();
                        TransferItems.TransferItemManagerWrapped.CancelSelectedItems(true);
                        TransferItems.TransferItemManagerWrapped.ClearSelectedItemsAndClean();
                        ViewState.BatchSelectedItems.Clear();
                        foreach (int pos in removedPositions)
                        {
                            Adapter.NotifyItemRemoved(pos);
                        }
                        //since all selected stuff is going away. its what Gmail action mode does.
                        mode.Finish();
                        break;
                    case Resource.Id.pause_selected_batch:
                        TransferItems.TransferItemManagerWrapped.CancelSelectedItems(false);
                        NotifyChangedAndClear(Adapter);
                        //since all selected stuff is going away. its what Gmail action mode does.
                        mode.Finish();
                        break;
                    case Resource.Id.resume_selected_batch:
                        Frag.RetryAllConditionEntry(false, true);
                        NotifyChangedAndClear(Adapter);
                        mode.Finish();
                        break;
                    case Resource.Id.retry_all_failed_batch:
                        Frag.RetryAllConditionEntry(true, true);
                        NotifyChangedAndClear(Adapter);
                        mode.Finish();
                        break;
                    case Resource.Id.select_all:
                        ViewState.BatchSelectedItems.Clear();
                        int cnt = Adapter.ItemCount;
                        for (int i = 0; i < cnt; i++)
                        {
                            var iti = TransferItems.TransferItemManagerWrapped.GetItemAtUserIndex(i);
                            if (iti != null)
                            {
                                ViewState.BatchSelectedItems.Add(iti);
                            }
                        }

                        Adapter.NotifyDataSetChanged();

                        mode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                        mode.Invalidate();
                        return true;
                    case Resource.Id.invert_selection:
                        ForceOutIfZeroSelected = false;
                        int cnt1 = Adapter.ItemCount;
                        var inverted = new HashSet<ITransferItem>();
                        for (int i = 0; i < cnt1; i++)
                        {
                            var iti = TransferItems.TransferItemManagerWrapped.GetItemAtUserIndex(i);
                            if (iti != null && !ViewState.BatchSelectedItems.Contains(iti))
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
                        return true;
                }
                return true;
            }

            /// <summary>
            /// For actions (pause/resume/retry) that mutate items in place: derive each
            /// selected item's current position, clear the selection, and notify those rows.
            /// </summary>
            private static void NotifyChangedAndClear(TransferAdapterRecyclerVersion adapter)
            {
                var positions = ViewState.BatchSelectedItems
                    .Select(it => TransferItems.TransferItemManagerWrapped.GetUserIndexForITransferItem(it))
                    .Where(p => p >= 0)
                    .ToList();
                ViewState.BatchSelectedItems.Clear();
                foreach (int pos in positions)
                {
                    adapter.NotifyItemChanged(pos);
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
