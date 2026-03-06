using Android.Views;
using Common.Browse;
using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Services;
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
                var activity = SeekerState.ActiveActivityRef;
                if (activity != null)
                {
                    var color = SearchItemViewExpandable.GetColorFromAttribute(activity, Resource.Attribute.colorPrimary);
                    activity.Window?.SetStatusBarColor(color);
                }
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                return false;
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                switch (item.ItemId)
                {
                    case Resource.Id.action_download_batch:
                        Frag.DownloadBatchSelected(false);
                        BrowseActionMode?.Finish();
                        return true;
                    case Resource.Id.action_queue_paused_batch:
                        Frag.DownloadBatchSelected(true);
                        BrowseActionMode?.Finish();
                        return true;
                    case Resource.Id.action_show_selected_info:
                        Frag.ShowBatchSelectedInfo();
                        return true;
                    case Resource.Id.action_copy_selected_url_batch:
                        Frag.CopyBatchSelectedURLs();
                        BrowseActionMode?.Finish();
                        return true;
                    case Resource.Id.select_all:
                        Adapter.SelectedPositions.Clear();
                        int cnt = Adapter.ItemCount;
                        for (int i = 0; i < cnt; i++)
                        {
                            Adapter.SelectedPositions.Add(i);
                        }
                        Adapter.NotifyDataSetChanged();
                        BrowseActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                        BrowseActionMode.Invalidate();
                        return true;
                    case Resource.Id.invert_selection:
                        List<int> oldOnes = Adapter.SelectedPositions.ToList();
                        Adapter.SelectedPositions.Clear();
                        List<int> all = new List<int>();
                        int cnt1 = Adapter.ItemCount;
                        for (int i = 0; i < cnt1; i++)
                        {
                            all.Add(i);
                        }
                        Adapter.SelectedPositions = all.Except(oldOnes).ToList();
                        Adapter.NotifyDataSetChanged();
                        if (Adapter.SelectedPositions.Count == 0)
                        {
                            BrowseActionMode?.Finish();
                        }
                        else
                        {
                            BrowseActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), Adapter.SelectedPositions.Count.ToString());
                            BrowseActionMode.Invalidate();
                        }
                        return true;
                }
                return true;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                SeekerState.ActiveActivityRef?.Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);

                int[] prevSelectedItems = Adapter.SelectedPositions.ToArray();
                BrowseActionMode = null;
                Adapter.SelectedPositions.Clear();
                Adapter.IsInBatchSelectMode = false;
                foreach (int i in prevSelectedItems)
                {
                    Adapter.NotifyItemChanged(i);
                }
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
                BrowseActionMode.Title = string.Format(SeekerApplication.GetString(Resource.String.Num_Selected), cnt.ToString());
                BrowseActionMode.Invalidate();
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

            // Separate files and folders
            var files = selectedItems.Where(di => !di.IsDirectory()).ToList();
            var folders = selectedItems.Where(di => di.IsDirectory()).ToList();

            if (folders.Count > 0)
            {
                // Download folders via the existing entry point
                foreach (var folder in folders)
                {
                    DownloadUserFilesEntry(queuePaused, false, folder);
                }
            }

            if (files.Count > 0)
            {
                var fileInfos = files.Select(item => BrowseUtils.ToFullFileInfo(item)).ToList();
                if (queuePaused)
                {
                    SessionService.Instance.RunWithReconnect(() => DownloadService.Instance.CreateDownloadAllTask(fileInfos.ToArray(), true, state.CurrentUsername).Start());
                }
                else
                {
                    SessionService.Instance.RunWithReconnect(() => DownloadService.Instance.CreateDownloadAllTask(fileInfos.ToArray(), false, state.CurrentUsername).Start());
                }
            }
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
