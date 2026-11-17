using Android.Views;
using Common.Browse;
using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Helpers.AnchoredMenu;
using System.Collections.Generic;
using System.Linq;

using ActionMode = AndroidX.AppCompat.View.ActionMode;

namespace Seeker
{
    public partial class BrowseFragment
    {
        public static ActionMode BrowseActionMode = null;
        public static BrowseActionModeCallback BrowseActionModeCallbackInstance = null;

        public class BrowseActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            public BrowseAdapter Adapter;
            public BrowseFragment Frag;

            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.browse_menu_batch, menu);
                if (state.IsAnonymousSelf)
                {
                    menu.FindItem(Resource.Id.action_download_batch)?.SetVisible(false);
                }
                OverflowMenu.Attach(menu, Resource.Id.action_browse_batch_overflow, BuildOverflowMenu, dismissWithAnchor: true);
                UiHelpers.ApplyActionModeStatusBarInset(SeekerState.ActiveActivityRef);
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                return false;
            }

            private AnchoredMenuConfig BuildOverflowMenu()
            {
                if (BrowseActionMode == null)
                {
                    return null;
                }
                var ctx = Frag.Context ?? SeekerState.ActiveActivityRef;
                var config = new AnchoredMenuConfig();
                if (!state.IsAnonymousSelf)
                {
                    config.AddRow(Resource.Drawable.paused, ctx.GetString(Resource.String.QueueSelectedAsPaused), () => IfActive(() =>
                    {
                        Frag.DownloadBatchSelected(true);
                        BrowseActionMode?.Finish();
                    }));
                }
                config.AddRow(Resource.Drawable.information_outline, ctx.GetString(Resource.String.ShowSelectedInfo), () => IfActive(Frag.ShowBatchSelectedInfo));
                if (!state.IsAnonymousSelf)
                {
                    config.AddRow(Resource.Drawable.link_variant, ctx.GetString(Resource.String.CopySelectedURLs), () => IfActive(() =>
                    {
                        Frag.CopyBatchSelectedURLs();
                        BrowseActionMode?.Finish();
                    }));
                }
                config.AddDivider();
                config.AddRow(Resource.Drawable.select_all_24, ctx.GetString(Resource.String.SelectAll), () => IfActive(SelectAll));
                config.AddRow(Resource.Drawable.ink_selection_24, ctx.GetString(Resource.String.InvertSelection), () => IfActive(InvertSelection));
                return config;
            }

            // the mode can finish while the menu is open
            private static void IfActive(System.Action action)
            {
                if (BrowseActionMode != null)
                {
                    action();
                }
            }

            private void SelectAll()
            {
                BatchSelection.SelectAll(Adapter.SelectedPositions, BatchSelection.Positions(Adapter.ItemCount));
                Adapter.NotifyDataSetChanged();
                BatchSelection.ShowCount(BrowseActionMode, Adapter.SelectedPositions.Count);
            }

            private void InvertSelection()
            {
                BatchSelection.Invert(Adapter.SelectedPositions, BatchSelection.Positions(Adapter.ItemCount));
                Adapter.NotifyDataSetChanged();
                if (Adapter.SelectedPositions.Count == 0)
                {
                    BrowseActionMode?.Finish();
                }
                else
                {
                    BatchSelection.ShowCount(BrowseActionMode, Adapter.SelectedPositions.Count);
                }
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                switch (item.ItemId)
                {
                    case Resource.Id.action_download_batch:
                        Frag.DownloadBatchSelected(false);
                        BrowseActionMode?.Finish();
                        return true;
                }
                return true;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                SeekerState.ActiveActivityRef?.Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);

                BrowseActionMode = null;
                BrowseActionModeCallbackInstance = null;
                Adapter.SelectedPositions.Clear();
                Adapter.IsInBatchSelectMode = false;
                Adapter.NotifyDataSetChanged();
                SeekerState.MainActivityRef?.RefreshBackCallbackState();
            }
        }

        public void ToggleBatchSelect(int position)
        {
            var adapter = BrowseAdapterInstance;
            if (adapter == null) return;

            if (adapter.SelectedPositions.Contains(position))
            {
                adapter.SelectedPositions.Remove(position);
            }
            else
            {
                adapter.SelectedPositions.Add(position);
            }
            adapter.NotifyItemChanged(position);

            int cnt = adapter.SelectedPositions.Count;
            if (cnt == 0)
            {
                BrowseActionMode?.Finish();
            }
            else
            {
                BatchSelection.ShowCount(BrowseActionMode, cnt);
            }
        }

        private void DownloadBatchSelected(bool queuePaused)
        {
            var adapter = BrowseAdapterInstance;
            if (adapter == null) return;

            var sourceList = state.Filter.IsFiltered ? state.FilteredDataItems : state.DataItems;
            List<DataItem> selectedItems;
            lock (sourceList)
            {
                selectedItems = adapter.SelectedPositions.Where(i => i < sourceList.Count).Select(i => sourceList[i]).ToList();
            }

            if (selectedItems.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.nothing_to_download), Android.Widget.ToastLength.Long);
                return;
            }

            // files and folders together - one set of dialogs and one enqueue (i.e. one ordered request loop for the user)
            DownloadUserFilesEntry(queuePaused, false, selectedItems);
        }

        private void ShowBatchSelectedInfo()
        {
            var adapter = BrowseAdapterInstance;
            if (adapter == null) return;

            var sourceList = state.Filter.IsFiltered ? state.FilteredDataItems : state.DataItems;
            List<DataItem> selectedItems;
            lock (sourceList)
            {
                selectedItems = adapter.SelectedPositions.Where(i => i < sourceList.Count).Select(i => sourceList[i]).ToList();
            }

            if (selectedItems.Count == 0) return;

            FolderSummary aggregated = new FolderSummary();
            foreach (var item in selectedItems)
            {
                if (item.IsDirectory())
                {
                    var summary = BrowseUtils.GetFolderSummary(item);
                    aggregated.NumFiles += summary.NumFiles;
                    aggregated.NumSubFolders += summary.NumSubFolders;
                    aggregated.SizeBytes += summary.SizeBytes;
                    aggregated.LengthSeconds += summary.LengthSeconds;
                }
                else
                {
                    aggregated.NumFiles++;
                    if (item.File != null)
                    {
                        aggregated.SizeBytes += item.File.Size;
                        if (item.File.Length.HasValue)
                        {
                            aggregated.LengthSeconds += item.File.Length.Value;
                        }
                    }
                }
            }

            ShowFolderSummaryDialog(aggregated);
        }

        private void CopyBatchSelectedURLs()
        {
            var adapter = BrowseAdapterInstance;
            if (adapter == null) return;

            var sourceList = state.Filter.IsFiltered ? state.FilteredDataItems : state.DataItems;
            List<DataItem> selectedItems;
            lock (sourceList)
            {
                selectedItems = adapter.SelectedPositions.Where(i => i < sourceList.Count).Select(i => sourceList[i]).ToList();
            }

            if (selectedItems.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.nothing_selected), Android.Widget.ToastLength.Long);
                return;
            }

            string linkToCopy = string.Empty;
            foreach (var item in selectedItems)
            {
                if (linkToCopy != string.Empty)
                {
                    linkToCopy += " \n";
                }
                if (item.IsDirectory())
                {
                    linkToCopy += CommonHelpers.CreateSlskLink(true, item.Directory.Name, state.CurrentUsername);
                }
                else
                {
                    var ffi = BrowseUtils.ToFullFileInfo(item);
                    linkToCopy += CommonHelpers.CreateSlskLink(false, ffi.FullFileName, state.CurrentUsername);
                }
            }

            CommonHelpers.CopyTextToClipboard(SeekerState.ActiveActivityRef, linkToCopy);
            if (selectedItems.Count > 1)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.LinksCopied), Android.Widget.ToastLength.Short);
            }
            else
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.LinkCopied), Android.Widget.ToastLength.Short);
            }
        }
    }
}
