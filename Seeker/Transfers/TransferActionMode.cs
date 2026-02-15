using Android.Views;
using Seeker.Helpers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

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
                        Logger.InfoFirebase("action_cancel_and_clear_batch Pressed");
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
    }
}
