using Android.Graphics;
using Android.Views;
using Android.Widget;
using Common;
using Common.Search;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Seeker.Services;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

using ActionMode = AndroidX.AppCompat.View.ActionMode;

namespace Seeker
{
    public partial class SearchFragment
    {
        private ActionMode searchActionMode;
        private readonly SearchSelection searchSelection = new SearchSelection();

        /// <summary>True while the result multi-selection action mode is active.</summary>
        public bool IsSelectionMode => searchActionMode != null;

        public bool IsResultSelected(SearchResponse response) => searchSelection.IsSelected(response);

        /// <summary>Long-press entry point: starts the action mode and selects the pressed result.</summary>
        public void EnterSelectionMode(int position)
        {
            var response = GetDisplayedResponseAt(position);
            if (response == null)
            {
                return;
            }

            if (searchActionMode == null)
            {
                var callback = new SearchActionModeCallback { Frag = this };
                searchActionMode = SeekerState.MainActivityRef?.StartSupportActionMode(callback);
                if (searchActionMode == null)
                {
                    return;
                }
            }

            searchSelection.Select(response);
            UpdateSelectionActionModeTitle();
            recyclerSearchAdapter?.NotifyDataSetChanged();
        }

        /// <summary>A row tap while selecting toggles that row; it never opens the single-result dialog.</summary>
        public void ToggleSelectionAt(int position)
        {
            if (!IsSelectionMode)
            {
                return;
            }
            var response = GetDisplayedResponseAt(position);
            if (response == null)
            {
                return;
            }

            searchSelection.Toggle(response);
            recyclerSearchAdapter?.NotifyItemChanged(position);
            UpdateSelectionActionModeTitle();
        }

        /// <summary>
        /// Clears the stable identities and dismisses the action mode. Called on tab change and
        /// when a search/tab is replaced, never by ordinary streaming result updates.
        /// </summary>
        public void ClearSelectionAndFinishActionMode()
        {
            if (searchActionMode != null)
            {
                searchActionMode.Finish();
            }
            else
            {
                searchSelection.Clear();
                recyclerSearchAdapter?.NotifyDataSetChanged();
            }
        }

        /// <summary>
        /// Drops identities no longer present in the current filtered view. Sorting does not call
        /// this (it keeps every row); filtering does, so a hidden row cannot stay silently selected.
        /// </summary>
        private void PruneSelectionToVisible()
        {
            if (searchSelection.IsEmpty)
            {
                return;
            }
            searchSelection.PruneTo(GetDisplayedResponses());
            if (searchSelection.IsEmpty)
            {
                searchActionMode?.Finish();
                return;
            }
            UpdateSelectionActionModeTitle();
        }

        private void SelectAllVisibleResults()
        {
            searchSelection.SelectAll(GetDisplayedResponses());
            recyclerSearchAdapter?.NotifyDataSetChanged();
            UpdateSelectionActionModeTitle();
        }

        private void UpdateSelectionActionModeTitle()
        {
            if (searchActionMode == null)
            {
                return;
            }
            int count = searchSelection.Count;
            if (count == 0)
            {
                searchActionMode.Finish();
                return;
            }
            searchActionMode.Title = string.Format(
                SeekerApplication.GetString(Resource.String.Num_Selected), count);
            searchActionMode.Invalidate();
        }

        /// <summary>Current visible rows: the filtered list when any filter is active.</summary>
        private List<SearchResponse> GetDisplayedResponses()
        {
            bool filtering = SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive();
            return filtering
                ? SearchTabHelper.UI_SearchResponses
                : SearchTabHelper.SearchResponses;
        }

        private SearchResponse GetDisplayedResponseAt(int position)
        {
            var responses = GetDisplayedResponses();
            if (responses == null || position < 0 || position >= responses.Count)
            {
                return null;
            }
            return responses[position];
        }

        /// <summary>
        /// Batch enqueue through the existing DownloadService/session flow. Selections are stable
        /// result identities, files are read from each result's own username, and the locked-file
        /// preference is honoured. The same GetFiles/GetFullFileInfos helpers the per-result dialog
        /// uses are reused, so no second file picker or enqueue implementation exists.
        /// </summary>
        private void DownloadSelectedResults(bool queuePaused)
        {
            var selected = searchSelection.Resolve(GetDisplayedResponses());
            if (selected.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(
                    SeekerApplication.GetString(Resource.String.nothing_selected_extra), ToastLength.Long);
                return;
            }

            bool hideLocked = PreferencesState.HideLockedResultsInSearch;
            var payloads = new List<(string Username, Soulseek.File[] Files)>();
            foreach (var group in selected.GroupBy(response => response.Username))
            {
                var files = new List<Soulseek.File>();
                foreach (var response in group)
                {
                    files.AddRange(response.GetFiles(hideLocked));
                }
                if (files.Count != 0)
                {
                    payloads.Add((group.Key, files.ToArray()));
                }
            }

            if (payloads.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(
                    SeekerApplication.GetString(Resource.String.nothing_to_download), ToastLength.Long);
                return;
            }

            void Enqueue()
            {
                foreach (var payload in payloads)
                {
                    DownloadService.Instance.EnqueueFilesFireAndForget(
                        Common.Browse.BrowseUtils.GetFullFileInfos(payload.Files),
                        queuePaused,
                        payload.Username);
                }
            }

            SessionService.Instance.RunWithReconnect((Action)Enqueue, silent: false);
        }

        public class SearchActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            public SearchFragment Frag;

            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.search_batch_menu, menu);
                UiHelpers.ApplyActionModeStatusBarInset(SeekerState.ActiveActivityRef);
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
                        Frag.DownloadSelectedResults(false);
                        mode.Finish();
                        return true;
                    case Resource.Id.action_queue_paused_batch:
                        Frag.DownloadSelectedResults(true);
                        mode.Finish();
                        return true;
                    case Resource.Id.select_all:
                        Frag.SelectAllVisibleResults();
                        return true;
                }
                return false;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {
                SeekerState.ActiveActivityRef?.Window?.SetStatusBarColor(Color.Transparent);
                Frag.searchActionMode = null;
                Frag.searchSelection.Clear();
                Frag.recyclerSearchAdapter?.NotifyDataSetChanged();
                SeekerState.MainActivityRef?.RefreshBackCallbackState();
            }
        }
    }
}
