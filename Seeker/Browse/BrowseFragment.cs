/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */

using Seeker.Browse;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Common;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.FloatingActionButton;
using Seeker.Services;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using Seeker.Helpers;
using Common.Browse;

namespace Seeker
{
    public class BrowseFragment : Fragment
    {
        //for filtering - we always just get the filtered copy from the main copy on the fly.
        //the main copy will move up, down, etc.  so no need for the filtered copy to keep track of any of that
        //just do what we normally do and then generate the filtered copy as the very last step

        private const int BROWSE_TAB_INDEX = 3;
        private const int BOTTOM_SHEET_PEEK_HEIGHT = 320;

        private static BrowseState state = new BrowseState();
        private DataItem DataItemSelectedForLongClick = null;
        private BrowseAdapter BrowseAdapterInstance => recyclerViewDirectories?.GetAdapter() as BrowseAdapter;

        public View rootView;
        private RecyclerView recyclerViewDirectories;
        private LinearLayoutManager browseLayoutManager;
        private RecyclerView treePathRecyclerView;
        private LinearLayoutManager treePathLayoutManager;
        private TreePathRecyclerAdapter treePathRecyclerAdapter;

        private static Stack<Tuple<int, int>> ScrollPositionRestore = new Stack<Tuple<int, int>>();
        private static Tuple<int, int> ScrollPositionRestoreRotate = null;


        private bool isPaused = true;
        private View noBrowseView = null;
        private View separator = null;

        private System.Timers.Timer DebounceTimer = null;
        private System.Diagnostics.Stopwatch DiagStopWatch = new System.Diagnostics.Stopwatch();
        private long lastTime = -1;

        public BrowseFragment() : base()
        {
            if (DebounceTimer == null)
            {
                DebounceTimer = new System.Timers.Timer(250);
                DebounceTimer.AutoReset = false;
            }
            DiagStopWatch.Start();
        }

        /// <summary>
        /// For use going down directories
        /// </summary>
        private void SaveScrollPosition()
        {
            try
            {
                int index = browseLayoutManager.FindFirstVisibleItemPosition();
                View v = browseLayoutManager.FindViewByPosition(index);
                int top = (v == null) ? 0 : (v.Top - recyclerViewDirectories.PaddingTop);
                ScrollPositionRestore.Push(new Tuple<int, int>(index, top));
            }
            catch (Exception e)
            {
                Logger.Firebase(e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// For use rotating screen, leaving and coming back, etc...
        /// </summary>
        private void SaveScrollPositionRotate()
        {
            try
            {
                int index = browseLayoutManager.FindFirstVisibleItemPosition();
                View v = browseLayoutManager.FindViewByPosition(index);
                int top = (v == null) ? 0 : (v.Top - recyclerViewDirectories.PaddingTop);
                ScrollPositionRestoreRotate = new Tuple<int, int>(index, top);
            }
            catch (Exception e)
            {
                Logger.Firebase(e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// For use going up directories
        /// </summary>
        private void RestoreScrollPosition()
        {
            try
            {
                Tuple<int, int> pos = ScrollPositionRestore.Pop();
                browseLayoutManager.ScrollToPositionWithOffset(pos.Item1, pos.Item2);
            }
            catch (Exception e)
            {
                Logger.Firebase(e.Message + e.StackTrace);
            }

        }

        private void DebounceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            state.UpdateFilteredResponses(); // this is the expensive function...
            SeekerState.MainActivityRef.RunOnUiThread(() =>
            {
                lock (state.FilteredDataItems)
                {
                    BrowseAdapter customAdapter = new BrowseAdapter(state.FilteredDataItems, this); //enumeration exception (that is, before I added the lock)
                    RecyclerView rv = rootView?.FindViewById<RecyclerView>(Resource.Id.listViewDirectories);
                    if (rv != null)
                    {
                        rv.SetAdapter(customAdapter);
                    }
                }
            });
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            if (state.HasResponse())
            {
                inflater.Inflate(Resource.Menu.browse_menu_full, menu);
            }
            else
            {
                inflater.Inflate(Resource.Menu.browse_menu_empty, menu);
            }
            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            int numSelected = BrowseAdapterInstance?.SelectedPositions?.Count ?? 0;

            UiHelpers.SetMenuTitles(menu, state.CurrentUsername);

            if (menu.FindItem(Resource.Id.action_up_directory) != null) //lets just make sure we are using the full menu.  o.w. the menu is empty so these guys dont exist.
            {
                if (numSelected == 0)
                {
                    menu.FindItem(Resource.Id.action_download_selected_files).SetVisible(false);
                    menu.FindItem(Resource.Id.action_queue_selected_paused).SetVisible(false);
                    menu.FindItem(Resource.Id.action_copy_selected_url).SetVisible(false);
                }
                else if (numSelected > 0)
                {
                    menu.FindItem(Resource.Id.action_download_selected_files).SetVisible(true);
                    menu.FindItem(Resource.Id.action_queue_selected_paused).SetVisible(true);
                    menu.FindItem(Resource.Id.action_copy_selected_url).SetVisible(true);
                    if (numSelected > 1)
                    {
                        menu.FindItem(Resource.Id.action_copy_selected_url).SetTitle(Resource.String.CopySelectedURLs);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_copy_selected_url).SetTitle(Resource.String.CopySelectedURL);
                    }
                }
            }


            base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId != Resource.Id.action_browse_user) //special handling (this browse user means browse user dialog).
            {
                if (UiHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), state.CurrentUsername, SeekerState.ActiveActivityRef, null))
                {
                    return true;
                }
            }
            switch (item.ItemId)
            {
                case Resource.Id.action_queue_folder_paused:
                    DownloadUserFilesEntry(true, true);
                    return true;
                case Resource.Id.action_browse_user:
                    ShowEditTextBrowseUserDialog();
                    return true;
                case Resource.Id.action_up_directory:
                    GoUpDirectory();
                    return true;
                case Resource.Id.action_download_files:
                    DownloadUserFilesEntry(false, true);
                    return true;
                case Resource.Id.action_download_selected_files:
                    DownloadSelectedFiles(false);
                    BrowseAdapterInstance.SelectedPositions.Clear();
                    ClearAllSelectedPositions();
                    return true;
                case Resource.Id.action_show_folder_info:
                    var folderSummary = BrowseUtils.GetFolderSummary(state.DataItems);
                    ShowFolderSummaryDialog(folderSummary);
                    return true;
                case Resource.Id.action_queue_selected_paused:
                    DownloadSelectedFiles(true);
                    BrowseAdapterInstance.SelectedPositions.Clear();
                    ClearAllSelectedPositions();
                    return true;
                case Resource.Id.action_copy_folder_url:
                    string fullDirName = state.DataItems[0].Node.Data.Name;
                    string slskLink = CommonHelpers.CreateSlskLink(true, fullDirName, state.CurrentUsername);
                    CommonHelpers.CopyTextToClipboard(SeekerState.ActiveActivityRef, slskLink);
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.LinkCopied), ToastLength.Short);
                    return true;
                case Resource.Id.action_copy_selected_url:
                    CopySelectedURLs();
                    BrowseAdapterInstance.SelectedPositions.Clear();
                    ClearAllSelectedPositions();
                    return true;
                case Resource.Id.action_add_user:
                    UserListService.AddUserAPI(SeekerState.MainActivityRef, state.CurrentUsername, null);
                    return true;
                case Resource.Id.action_get_user_info:
                    RequestedUserInfoHelper.RequestUserInfoApi(state.CurrentUsername);
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public override void SetMenuVisibility(bool menuVisible)
        {
            //this is necessary if programmatically moving to a tab from another activity..
            if (menuVisible)
            {
                var navigator = SeekerState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if (navigator != null)
                {
                    navigator.Menu.GetItem(BROWSE_TAB_INDEX).SetCheckable(true);
                    navigator.Menu.GetItem(BROWSE_TAB_INDEX).SetChecked(true);
                }
            }
            base.SetMenuVisibility(menuVisible);
        }

        public override void OnDestroyView()
        {
            DebounceTimer.Elapsed -= DebounceTimer_Elapsed; //the timer is static...
            base.OnDestroyView();
        }

        public static BrowseFragment Instance = null;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Instance = this;
            this.HasOptionsMenu = true;
            SeekerState.InDarkModeCache = DownloadDialog.InNightMode(this.Context);
            Logger.Debug("BrowseFragmentOnCreateView");
            this.rootView = inflater.Inflate(Resource.Layout.browse, container, false);
            recyclerViewDirectories = this.rootView.FindViewById<RecyclerView>(Resource.Id.listViewDirectories);
            browseLayoutManager = new LinearLayoutManager(this.Context);
            recyclerViewDirectories.SetLayoutManager(browseLayoutManager);
            recyclerViewDirectories.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            this.RegisterForContextMenu(recyclerViewDirectories);
            DebounceTimer.Elapsed += DebounceTimer_Elapsed;

            treePathRecyclerView = this.rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewHorizontalPath);
            treePathLayoutManager = new LinearLayoutManager(this.Context, LinearLayoutManager.Horizontal, false);
            treePathRecyclerView.SetLayoutManager(treePathLayoutManager);


            //savedInstanceState can be null if first time.
            int[]? selectedPos = savedInstanceState?.GetIntArray("selectedPositions");

            if (state.Filter.IsFiltered)
            {
                lock (state.FilteredDataItems)
                { //on ui thread.
                    currentUsernameUI = state.CurrentUsername;
                    recyclerViewDirectories.SetAdapter(new BrowseAdapter(state.FilteredDataItems, this, selectedPos));
                }
            }
            else
            {
                lock (state.DataItems)
                { //on ui thread.
                    currentUsernameUI = state.CurrentUsername;
                    recyclerViewDirectories.SetAdapter(new BrowseAdapter(state.DataItems, this, selectedPos));
                }
            }

            if (state.DataItems.Count != 0)
            {
                state.PathItems = BrowseUtils.GetPathItems(state.DataItems);
            }

            treePathRecyclerAdapter = new TreePathRecyclerAdapter(state.PathItems, this);
            treePathRecyclerView.SetAdapter(treePathRecyclerAdapter);

            this.noBrowseView = this.rootView.FindViewById<TextView>(Resource.Id.noBrowseView);
            this.separator = this.rootView.FindViewById<View>(Resource.Id.recyclerViewHorizontalPathSep);
            this.separator.Visibility = ViewStates.Gone;
            if (state.HasResponse())
            {
                noBrowseView.Visibility = ViewStates.Gone;
                separator.Visibility = ViewStates.Visible;
            }

            View v = rootView.FindViewById<View>(Resource.Id.relativeLayout1);
            v.Focusable = true;
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                v.SetFocusable(ViewFocusability.Focusable);
            }

            v.FocusableInTouchMode = true;

            EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
            filterText.TextChanged += FilterText_TextChanged;
            filterText.FocusChange += FilterText_FocusChange;
            filterText.EditorAction += FilterText_EditorAction;
            filterText.Touch += FilterText_Touch;
            SearchFragment.UpdateDrawableState(filterText, true);

            RelativeLayout rel = rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
            BottomSheetBehavior bottomSheetBehavior = BottomSheetBehavior.From(rel);
            bottomSheetBehavior.Hideable = true;
            bottomSheetBehavior.PeekHeight = BOTTOM_SHEET_PEEK_HEIGHT;
            bottomSheetBehavior.State = BottomSheetBehavior.StateHidden;
            View floatingActionButton = rootView.FindViewById<View>(Resource.Id.bsbutton);
            (floatingActionButton as FloatingActionButton).SetImageResource(Resource.Drawable.ic_filter_list_white_24dp);
            floatingActionButton.Click += FloatingActionButtonClick;

            return this.rootView;
        }

        private void FilterText_Touch(object sender, View.TouchEventArgs e)
        {
            EditText editText = sender as EditText;
            e.Handled = false;
            if (e.Event.GetX() >= (editText.Width - editText.TotalPaddingRight))
            {
                if (e.Event.Action == MotionEventActions.Up)
                {
                    //e.Handled = true;
                    editText.Text = string.Empty;
                    SearchFragment.UpdateDrawableState(editText, true);
                }
            }
        }

        private void FilterText_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SeekerState.MainActivityRef.Window.SetSoftInputMode(SoftInput.AdjustResize);
            }
            catch (System.Exception err)
            {
                Logger.Firebase("MainActivity_FocusChange" + err.Message);
            }
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
        }

        private void FloatingActionButtonClick(object sender, EventArgs e)
        {
            RelativeLayout rel = rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
            if (bsb.State != BottomSheetBehavior.StateExpanded && bsb.State != BottomSheetBehavior.StateCollapsed)
            {
                bsb.State = BottomSheetBehavior.StateExpanded;
            }
            else
            {
                EditText test = rootView.FindViewById<EditText>(Resource.Id.filterText);
                Logger.Debug(this.Resources.Configuration.HardKeyboardHidden.ToString()); //on pixel2 it is YES. on emulator with HW Keyboard = true it is NO

                if (test.IsFocused && (this.Resources.Configuration.HardKeyboardHidden == Android.Content.Res.HardKeyboardHidden.Yes)) //it can still be focused without the keyboard up...
                {
                    try
                    {

                        //SeekerState.MainActivityRef.DispatchKeyEvent(new KeyEvent(new KeyEventActions(),Keycode.Enter));
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)Context.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                        test.ClearFocus();
                        rootView.FindViewById<View>(Resource.Id.relativeLayout1).RequestFocus();
                    }
                    catch
                    {
                        //not worth throwing over
                    }
                    return;
                }
                else if (test.IsFocused && (this.Resources.Configuration.HardKeyboardHidden == Android.Content.Res.HardKeyboardHidden.No))
                {

                    //we still want to change focus as otherwise one can still type into it...
                    test.ClearFocus();
                    rootView.FindViewById<View>(Resource.Id.relativeLayout1).RequestFocus();
                    bsb.State = BottomSheetBehavior.StateHidden;

                }
                bsb.State = BottomSheetBehavior.StateHidden;
            }
        }


        private void FilterText_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Search)
            {
                Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                if (rootView == null || SeekerState.MainActivityRef == null)
                {
                    return;
                }
                rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                rootView.FindViewById<View>(Resource.Id.relativeLayout1).RequestFocus();
                //overriding this, the keyboard fails to go down by default for some reason.....
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                }
                catch (System.Exception ex)
                {
                    Logger.Firebase(ex.Message + " error closing keyboard");
                }
            }
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutIntArray("selectedPositions", BrowseAdapterInstance?.SelectedPositions?.ToArray());
            base.OnSaveInstanceState(outState);
        }



        public override void OnViewStateRestored(Bundle savedInstanceState)
        {
            base.OnViewStateRestored(savedInstanceState);
            //the system by default will set the value of filterText to whatever it was last... but we keep track of it using the static so do that instead....
            //cant do this OnViewCreated since filterText.Text will always be empty...
            DebounceTimer.Stop();
            EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
            filterText.Text = state.Filter.FilterString;  //this will often be empty (which is good) if we got a new response... otherwise (on screen rotate, it will be the same as it otherwise was).
            SearchFragment.UpdateDrawableState(filterText, true);
        }

        // this is on the fragment in case we go back to the old mainActivity on the backstack
        private string currentUsernameUI;
        public static EventHandler<EventArgs> BrowseResponseReceivedUI;

        public static string GetTabTitle(Android.Content.Context context)
        {
            if (string.IsNullOrEmpty(state.CurrentUsername))
            {
                return context.GetString(Resource.String.browse_tab);
            }
            return context.GetString(Resource.String.browse_tab) + ": " + state.CurrentUsername;
        }

        public void BrowseResponseReceivedUI_Handler(object sender, EventArgs args)
        {
            //if the fragment was never created then this.Context will be null
            SeekerState.MainActivityRef.RunOnUiThread(() =>
            {
                lock (state.DataItems)
                {
                    if (BrowseFragment.Instance?.Context != null && BrowseFragment.Instance?.rootView != null)
                    {
                        BrowseFragment.Instance.RefreshOnRecieved();
                    }
                }
                var pager = (AndroidX.ViewPager.Widget.ViewPager)SeekerState.MainActivityRef?.FindViewById(Resource.Id.pager);
                if (pager != null && pager.CurrentItem == BROWSE_TAB_INDEX)
                {
                    SeekerState.MainActivityRef.SupportActionBar.Title = GetTabTitle(SeekerState.MainActivityRef);
                    SeekerState.MainActivityRef.InvalidateOptionsMenu();
                }

            });
        }

        public override void OnResume()
        {
            base.OnResume();
            if (SeekerState.MainActivityRef?.SupportActionBar?.Title != null && !string.IsNullOrEmpty(state.CurrentUsername)
                && !SeekerState.MainActivityRef.SupportActionBar.Title.EndsWith(": " + state.CurrentUsername)
                && SeekerState.MainActivityRef.OnBrowseTab())
            {
                SeekerState.MainActivityRef.SupportActionBar.Title = GetTabTitle(SeekerState.MainActivityRef);
            }
            BrowseResponseReceivedUI += BrowseResponseReceivedUI_Handler;
            if (currentUsernameUI != state.CurrentUsername)
            {
                currentUsernameUI = state.CurrentUsername;
                BrowseResponseReceivedUI_Handler(null, new EventArgs());
            }
            Instance = this;
            if (recyclerViewDirectories != null && browseLayoutManager != null && ScrollPositionRestoreRotate != null)
            {
                //restore scroll
                browseLayoutManager.ScrollToPositionWithOffset(ScrollPositionRestoreRotate.Item1, ScrollPositionRestoreRotate.Item2);
            }
            isPaused = false;
        }

        public override void OnPause()
        {
            BrowseResponseReceivedUI -= BrowseResponseReceivedUI_Handler;
            SaveScrollPositionRotate();
            base.OnPause();
            isPaused = true;
        }




        private void FilterText_TextChanged(object sender, TextChangedEventArgs e)
        {
            string oldFilterString = state.Filter.IsFiltered ? state.Filter.FilterString : string.Empty;
            Logger.Debug("time between typing: " + (DiagStopWatch.ElapsedMilliseconds - lastTime).ToString());
            lastTime = DiagStopWatch.ElapsedMilliseconds;
            if (e.Text != null && e.Text.ToString() != string.Empty && isPaused)
            {
                return;//this is the case where going from search fragment to browse fragment this event gets fired
                //with an old e.text value and so its impossible to autoclear the value.
            }
            Logger.Debug("Text Changed: " + e.Text);
            if (e.Text != null && e.Text.ToString() != string.Empty)
            {
                state.Filter.Set(e.Text.ToString());

                DebounceTimer.Stop(); //average time bewteen typing is around 150-250 ms (if you know what you are going to type etc).  backspacing (i.e. holding it down) is about 50 ms.
                DebounceTimer.Start();
            }
            else
            {
                DebounceTimer.Stop();
                state.Filter.Reset();
                lock (state.DataItems) //collection was modified exception here...
                {
                    BrowseAdapter customAdapter = new BrowseAdapter(state.DataItems, this);
                    RecyclerView rv = rootView.FindViewById<RecyclerView>(Resource.Id.listViewDirectories);
                    rv.SetAdapter(customAdapter);
                }
            }

            if (oldFilterString == string.Empty && e.Text.ToString() != string.Empty)
            {
                SearchFragment.UpdateDrawableState(sender as EditText, true);
            }
            else if (oldFilterString != string.Empty && e.Text.ToString() == string.Empty)
            {
                SearchFragment.UpdateDrawableState(sender as EditText, true);
            }
        }

        private List<FullFileInfo> GetSelectedFileInfos()
        {
            var result = new List<FullFileInfo>();
            var selectedPositions = BrowseAdapterInstance.SelectedPositions;
            var sourceList = state.Filter.IsFiltered ? state.FilteredDataItems : state.DataItems;
            lock (sourceList)
            {
                result = selectedPositions.Where(i => i < sourceList.Count).Select(i => sourceList[i]).Select(item => BrowseUtils.ToFullFileInfo(item)).ToList();
            }
            return result;
        }

        private void CopySelectedURLs()
        {
            if ((!state.Filter.IsFiltered && state.DataItems.Count == 0) || (state.Filter.IsFiltered && state.FilteredDataItems.Count == 0))
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.NothingToCopy), ToastLength.Long);
            }
            else if (BrowseAdapterInstance.SelectedPositions.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.nothing_selected), ToastLength.Long);
            }
            else
            {
                List<FullFileInfo> slskFile = GetSelectedFileInfos();

                string linkToCopy = string.Empty;
                foreach (FullFileInfo ffi in slskFile)
                {
                    //there is something before us
                    if (linkToCopy != string.Empty)
                    {
                        linkToCopy = linkToCopy + " \n";
                    }
                    linkToCopy = linkToCopy + CommonHelpers.CreateSlskLink(false, ffi.FullFileName, state.CurrentUsername);
                }
                CommonHelpers.CopyTextToClipboard(SeekerState.ActiveActivityRef, linkToCopy);
                if (BrowseAdapterInstance.SelectedPositions.Count > 1)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.LinksCopied), ToastLength.Short);
                }
                else
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.LinkCopied), ToastLength.Short);
                }
            }


        }

        private void DownloadSelectedFiles(bool queuePaused)
        {
            if ((!state.Filter.IsFiltered && state.DataItems.Count == 0) || (state.Filter.IsFiltered && state.FilteredDataItems.Count == 0))
            {
                SeekerApplication.Toaster.ShowToast(this.Resources.GetString(Resource.String.nothing_to_download), ToastLength.Long);
            }
            else if (BrowseAdapterInstance.SelectedPositions.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(this.Resources.GetString(Resource.String.nothing_selected), ToastLength.Long);
            }
            else
            {
                List<FullFileInfo> slskFile = GetSelectedFileInfos();
                SessionService.Instance.RunWithReconnect(() => DownloadService.Instance.CreateDownloadAllTask(slskFile.ToArray(), queuePaused, state.CurrentUsername).Start());
            }
        }

        private void DownloadUserFilesEntryStage3(bool downloadSubfolders, List<FullFileInfo> recursiveFullFileInfo, List<FullFileInfo> topLevelFullFileInfoOnly, bool queuePaused)
        {
            var filesToDownload = downloadSubfolders ? recursiveFullFileInfo : topLevelFullFileInfoOnly;
            if (filesToDownload.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(this.Resources.GetString(Resource.String.nothing_to_download), ToastLength.Long);
                return;
            }
            Browse.BrowseService.DownloadListOfFiles(filesToDownload, queuePaused, state.CurrentUsername);
        }

        private void DownloadUserFilesEntryStage2(List<DataItem> dataItemsForDownload, List<DataItem> filteredDataItemsForDownload, bool justFilteredItems, bool queuePaused)
        {
            var sourceList = justFilteredItems ? filteredDataItemsForDownload : dataItemsForDownload;
            if (sourceList.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(this.Resources.GetString(Resource.String.nothing_to_download), ToastLength.Long);
                return;
            }

            var (topLevelFullFileInfoOnly, recursiveFullFileInfo, containsSubDirs) = BrowseUtils.BuildDownloadFileInfos(sourceList);
            int toplevelItems = topLevelFullFileInfoOnly.Count;
            int totalItems = recursiveFullFileInfo.Count;

            if (containsSubDirs)
            {
                var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(SeekerState.ActiveActivityRef);
                builder.SetTitle(Resource.String.ThisFolderContainsSubfolders);

                string topLevelStr = string.Format(SeekerApplication.GetString(
                    toplevelItems == 1 ? Resource.String.item_total_singular : Resource.String.item_total_plural), toplevelItems);
                string recursiveStr = string.Format(SeekerApplication.GetString(
                    totalItems == 1 ? Resource.String.item_total_singular : Resource.String.item_total_plural), totalItems);

                if (queuePaused)
                {
                    builder.SetMessage(string.Format(SeekerApplication.GetString(Resource.String.subfolders_warning_queue_paused), recursiveStr, topLevelStr));
                }
                else
                {
                    builder.SetMessage(string.Format(SeekerApplication.GetString(Resource.String.subfolders_warning), recursiveStr, topLevelStr));
                }
                EventHandler<DialogClickEventArgs> eventHandlerCurrentFolder = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                {
                    DownloadUserFilesEntryStage3(false, recursiveFullFileInfo, topLevelFullFileInfoOnly, queuePaused);
                });
                EventHandler<DialogClickEventArgs> eventHandlerRecursiveFolders = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                {
                    BrowseUtils.SetDepthTags(sourceList.First(), recursiveFullFileInfo);
                    DownloadUserFilesEntryStage3(true, recursiveFullFileInfo, topLevelFullFileInfoOnly, queuePaused);
                });
                builder.SetPositiveButton(Resource.String.all, eventHandlerRecursiveFolders);
                builder.SetNegativeButton(Resource.String.current_folder_only, eventHandlerCurrentFolder);
                builder.Show();
            }
            else
            {
                DownloadUserFilesEntryStage3(false, recursiveFullFileInfo, topLevelFullFileInfoOnly, queuePaused);
            }
        }

        /// <param name="queuePaused"></param>
        /// <param name="downloadShownInListView">True if to select everything currently shown in the listview.  False if the user is selecting a single folder.</param>
        private void DownloadUserFilesEntry(bool queuePaused, bool downloadShownInListView, DataItem itemSelected = null)
        {
            List<DataItem> dataItemsForDownload;
            List<DataItem> filteredDataItemsForDownload;

            if (downloadShownInListView)
            {
                lock (state.DataItems)
                {
                    dataItemsForDownload = state.DataItems.ToList();
                }
                lock (state.FilteredDataItems)
                {
                    filteredDataItemsForDownload = state.FilteredDataItems.ToList();
                }
            }
            else
            {
                if (itemSelected == null)
                {
                    UiHelpers.ShowReportErrorDialog(SeekerState.ActiveActivityRef, "Browse User File Selection Issue");
                    return;
                }
                dataItemsForDownload = BrowseUtils.GetDataItemsForNode(itemSelected.Node);
                filteredDataItemsForDownload = BrowseUtils.FilterBrowseList(dataItemsForDownload, state.Filter);
            }

            if (dataItemsForDownload.Count == 0)
            {
                SeekerApplication.Toaster.ShowToast(this.Resources.GetString(Resource.String.nothing_to_download), ToastLength.Long);
                return;
            }
            if (state.Filter.IsFiltered && (dataItemsForDownload.Count != filteredDataItemsForDownload.Count))
            {
                var b = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(SeekerState.ActiveActivityRef);
                b.SetTitle(Resource.String.filter_is_on);
                b.SetMessage(Resource.String.filter_is_on_body);
                EventHandler<DialogClickEventArgs> eventHandlerAll = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                {
                    DownloadUserFilesEntryStage2(dataItemsForDownload, filteredDataItemsForDownload, false, queuePaused);
                });
                EventHandler<DialogClickEventArgs> eventHandlerFiltered = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
                {
                    DownloadUserFilesEntryStage2(dataItemsForDownload, filteredDataItemsForDownload, true, queuePaused);
                });
                b.SetPositiveButton(Resource.String.just_filtered, eventHandlerFiltered);
                b.SetNegativeButton(Resource.String.all, eventHandlerAll);
                b.Show();
            }
            else
            {
                DownloadUserFilesEntryStage2(dataItemsForDownload, filteredDataItemsForDownload, false, queuePaused);
            }
        }


        private void UpDirectory(object sender, System.EventArgs e)
        {
            GoUpDirectory();
        }

        private DataItem GetItemSelected(int position, bool filteredResults)
        {
            DataItem itemSelected = null;
            if (filteredResults)
            {
                try
                {
                    itemSelected = state.FilteredDataItems[position];
                }
                catch (ArgumentOutOfRangeException) //this did happen to me.... when filtering...
                {
                    string logMsg = $"ListViewDirectories_ItemClick position: {position} state.FilteredDataItems.Count: {state.FilteredDataItems.Count}";
                    Logger.Firebase(logMsg);
                    return null;
                }
            }
            else
            {
                try
                {
                    itemSelected = state.DataItems[position]; //out of bounds here...
                }
                catch (ArgumentOutOfRangeException)
                {
                    string logMsg = $"ListViewDirectories_ItemClick position: {position} state.FilteredDataItems.Count: {state.FilteredDataItems.Count} isFilter {filteredResults}";
                    Logger.Firebase(logMsg);
                    return null;
                }
            }
            return itemSelected;
        }
        public void OnItemLongClick(int position, View view)
        {
            DataItemSelectedForLongClick = GetItemSelected(position, state.Filter.IsFiltered);
            if (DataItemSelectedForLongClick == null)
            {
                return;
            }
            if (DataItemSelectedForLongClick.IsDirectory())
            {
                view.ShowContextMenu();
            }
            else
            {
                //no special long click event if file.
                this.OnItemClick(position);
            }
        }

        private void PopulateDataItemsToItemSelected(List<DataItem> dirs, DataItem itemSelected)
        {
            dirs.Clear();
            dirs.AddRange(BrowseUtils.GetDataItemsForNode(itemSelected.Node));
        }

        public void OnItemClick(int position)
        {
            state.CachedFilteredDataItems = null;
            bool filteredResults = state.Filter.IsFiltered;
            DataItem itemSelected = GetItemSelected(position, filteredResults);
            if (itemSelected == null)
            {
                return;
            }

            bool isFile = false;
            lock (state.DataItems)
            {
                if (itemSelected.IsDirectory())
                {
                    if (itemSelected.Node.Children.Count == 0 && (itemSelected.Directory == null || itemSelected.Directory.FileCount == 0))
                    {
                        //dont let them do this... if this happens then there is no way to get back up...
                        SeekerApplication.Toaster.ShowToast(this.Resources.GetString(Resource.String.directory_is_empty), ToastLength.Short);
                        return;
                    }
                    SaveScrollPosition();

                    PopulateDataItemsToItemSelected(state.DataItems, itemSelected);

                    if (!filteredResults)
                    {
                        SetBrowseAdapters(filteredResults, state.DataItems, false, false);
                    }
                }
                else
                {
                    isFile = true;

                    var adapter = BrowseAdapterInstance;
                    bool alreadySelected = adapter.SelectedPositions.Contains(position);
                    if (!alreadySelected)
                    {
                        adapter.SelectedPositions.Add(position);
                    }
                    else
                    {
                        adapter.SelectedPositions.Remove(position);
                    }
                    adapter.NotifyItemChanged(position);
                }

            }
            lock (state.DataItems)
            {
                lock (state.FilteredDataItems)
                {
                    if (!isFile && filteredResults)
                    {
                        SetBrowseAdapters(filteredResults, state.DataItems, false, false);
                    }
                }
            }
        }

        private void ClearAllSelectedPositions()
        {
            //nullref crash was here.. not worth crashing over...
            if (recyclerViewDirectories == null)
            {
                return;
            }
            var adapter = BrowseAdapterInstance;
            if (adapter != null)
            {
                adapter.NotifyDataSetChanged();
            }
        }

        public bool BackButton()
        {
            return GoUpDirectory();
        }

        public bool GoUpDirectory(int additionalLevels = 0)
        {
            bool res = state.GoUpDirectory(SetBrowseAdapters, additionalLevels);
            if (res)
            {
                RestoreScrollPosition();
            }
            return res;
        }

        /// <summary>
        /// Sets both the main and the Path Items adapters.  necessary when first loading or when going up or down directories (i.e. if path changes).  not necessary if just changing the filter.
        /// </summary>
        /// <param name="toFilter"></param>
        /// <param name="nonFilteredItems"></param>
        public void SetBrowseAdapters(bool toFilter, List<DataItem> nonFilteredItems, bool fullRefreshOfPathItems, bool goingUp = false)
        {
            if (toFilter)
            {
                state.FilteredDataItems = BrowseUtils.FilterBrowseList(state.DataItems, state.Filter);
                recyclerViewDirectories.SetAdapter(new BrowseAdapter(state.FilteredDataItems, this));
            }
            else
            {
                recyclerViewDirectories.SetAdapter(new BrowseAdapter(state.DataItems, this));
            }

            var items = BrowseUtils.GetPathItems(state.DataItems);
            state.PathItems.Clear();
            state.PathItems.AddRange(items);
            if (fullRefreshOfPathItems)
            {
                treePathRecyclerAdapter.NotifyDataSetChanged();
            }
            else if (goingUp)
            {
                treePathRecyclerAdapter.NotifyDataSetChanged();
            }
            else
            {
                treePathRecyclerAdapter.NotifyDataSetChanged();
                treePathRecyclerView.ScrollToPosition(state.PathItems.Count - 1);
            }
        }



        //https://stackoverflow.com/questions/5297842/how-to-handle-oncontextitemselected-in-a-multi-fragment-activity
        //onContextItemSelected() is called for all currently existing fragments starting with the first added one.
        public const int UNIQUE_BROWSE_GROUP_ID = 304;
        private const int CONTEXT_DOWNLOAD_FOLDER = 0;
        private const int CONTEXT_QUEUE_FOLDER_PAUSED = 1;
        private const int CONTEXT_SHOW_FOLDER_INFO = 2;
        private const int CONTEXT_COPY_URL = 3;
        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            menu.Add(UNIQUE_BROWSE_GROUP_ID, CONTEXT_DOWNLOAD_FOLDER, CONTEXT_DOWNLOAD_FOLDER, Resource.String.download_folder);
            menu.Add(UNIQUE_BROWSE_GROUP_ID, CONTEXT_QUEUE_FOLDER_PAUSED, CONTEXT_QUEUE_FOLDER_PAUSED, Resource.String.QueueFolderAsPaused);
            menu.Add(UNIQUE_BROWSE_GROUP_ID, CONTEXT_SHOW_FOLDER_INFO, CONTEXT_SHOW_FOLDER_INFO, Resource.String.ShowFolderInfo);
            menu.Add(UNIQUE_BROWSE_GROUP_ID, CONTEXT_COPY_URL, CONTEXT_COPY_URL, Resource.String.CopyURL);
            base.OnCreateContextMenu(menu, v, menuInfo);
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            if (item.GroupId == UNIQUE_BROWSE_GROUP_ID)
            {
                switch (item.ItemId)
                {
                    case CONTEXT_DOWNLOAD_FOLDER:
                        DownloadUserFilesEntry(false, false, DataItemSelectedForLongClick);
                        return true;
                    case CONTEXT_QUEUE_FOLDER_PAUSED:
                        DownloadUserFilesEntry(true, false, DataItemSelectedForLongClick);
                        return true;
                    case CONTEXT_SHOW_FOLDER_INFO:
                        var folderSummary = BrowseUtils.GetFolderSummary(DataItemSelectedForLongClick);
                        ShowFolderSummaryDialog(folderSummary);
                        return true;
                    case CONTEXT_COPY_URL:
                        string slskLink = CommonHelpers.CreateSlskLink(true, DataItemSelectedForLongClick.Directory.Name, state.CurrentUsername);
                        CommonHelpers.CopyTextToClipboard(SeekerState.ActiveActivityRef, slskLink);
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.LinkCopied), ToastLength.Short);
                        return true;
                }
            }
            return base.OnContextItemSelected(item);
        }

        public void ShowFolderSummaryDialog(FolderSummary folderSummary)
        {
            string lengthTimePt2 = (folderSummary.LengthSeconds == 0) ? ": -" : string.Format(": {0}", SimpleHelpers.GetHumanReadableTime(folderSummary.LengthSeconds));
            string lengthTime = SeekerApplication.GetString(Resource.String.Length) + lengthTimePt2;

            string sizeString = SeekerApplication.GetString(Resource.String.size_column) + string.Format(" {0}", SimpleHelpers.GetHumanReadableSize(folderSummary.SizeBytes));

            string numFilesString = SeekerApplication.GetString(Resource.String.NumFiles) + string.Format(": {0}", folderSummary.NumFiles);
            string numSubFoldersString = SeekerApplication.GetString(Resource.String.NumSubfolders) + string.Format(": {0}", folderSummary.NumSubFolders);

            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this.Context);

            void OnCloseClick(object sender, DialogClickEventArgs e)
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            }

            var diag = builder.SetMessage(numFilesString +
                System.Environment.NewLine +
                System.Environment.NewLine +
                numSubFoldersString +
                System.Environment.NewLine +
                System.Environment.NewLine +
                sizeString +
                System.Environment.NewLine +
                System.Environment.NewLine +
                lengthTime).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            diag.Show();
            diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor));
        }

        private static void ClearFilterUIString(bool force = false)
        {
            if (BrowseFragment.Instance != null && BrowseFragment.Instance.rootView != null) //if you havent been there it will be null.
            {
                SeekerState.MainActivityRef.RunOnUiThread(() =>
                {
                    EditText filterText = BrowseFragment.Instance.rootView.FindViewById<EditText>(Resource.Id.filterText);
                    filterText.Text = string.Empty;
                    SearchFragment.UpdateDrawableState(filterText, true);
                });
            }
        }

        public static void SeekerState_BrowseResponseReceived(object sender, BrowseResponseEvent e)
        {
            ClearFilterUIString();
            ScrollPositionRestore?.Clear();
            ScrollPositionRestoreRotate = null;
            var errorCode = state.SetBrowseResponse(e.Username, e.BrowseResponseTree, e.StartingLocation);
            if (errorCode == BrowseStateError.CannotFindStartDirectory)
            {
                Logger.Firebase("SeekerState_BrowseResponseReceived: startingPoint is null " + e.StartingLocation);
                SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.error_browse_at_location), ToastLength.Long);
            }
            BrowseResponseReceivedUI?.Invoke(null, new EventArgs());
        }

        public void RefreshOnRecieved()
        {
            if (noBrowseView != null)
            {
                noBrowseView.Visibility = ViewStates.Gone;
                separator.Visibility = ViewStates.Visible;
            }
            recyclerViewDirectories = rootView.FindViewById<RecyclerView>(Resource.Id.listViewDirectories);
            if (browseLayoutManager == null)
            {
                browseLayoutManager = new LinearLayoutManager(this.Context);
                recyclerViewDirectories.SetLayoutManager(browseLayoutManager);
                recyclerViewDirectories.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            }

            Logger.InfoFirebase("RefreshOnRecieved " + state.CurrentUsername);
            currentUsernameUI = state.CurrentUsername;
            SetBrowseAdapters(false, state.DataItems, true);
        }

        private void DismissKeyboard()
        {
            try
            {
                Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
            }
            catch (System.Exception ex)
            {
                Logger.Firebase(ex.Message + " error closing keyboard");
            }
        }

        private static AndroidX.AppCompat.App.AlertDialog browseUserDialog = null;

        public void ShowEditTextBrowseUserDialog()
        {
            FragmentActivity c = this.Activity != null ? this.Activity : SeekerState.MainActivityRef;
            Logger.InfoFirebase("ShowEditTextBrowseUserDialog" + c.IsDestroyed + c.IsFinishing);
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(c);
            builder.SetTitle(c.Resources.GetString(Resource.String.browse_user));

            View viewInflated = LayoutInflater.From(c).Inflate(Resource.Layout.autocomplete_user_dialog_content, (ViewGroup)this.View, false);

            AutoCompleteTextView input = (AutoCompleteTextView)viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserEditText);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(input);

            builder.SetView(viewInflated);

            Action<View> goSnackBarAction = new Action<View>((View v) =>
            {
                ((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(BROWSE_TAB_INDEX, true);
            });

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //Do the Browse Logic...
                string usernameToBrowse = input.Text;
                if (string.IsNullOrWhiteSpace(usernameToBrowse))
                {
                    SeekerApplication.Toaster.ShowToast(SeekerState.MainActivityRef.Resources.GetString(Resource.String.must_type_a_username_to_browse), ToastLength.Short);
                    if (sender is AndroidX.AppCompat.App.AlertDialog aDiag1) //actv
                    {
                        aDiag1.Dismiss();
                    }
                    else
                    {
                        BrowseFragment.browseUserDialog.Dismiss();
                    }
                    return;
                }
                SeekerState.RecentUsersManager.AddUserToTop(usernameToBrowse, true);
                BrowseService.RequestFilesApi(usernameToBrowse, this.View, goSnackBarAction, null);
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    BrowseFragment.browseUserDialog.Dismiss();
                }
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    BrowseFragment.browseUserDialog.Dismiss();
                }
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search)
                {
                    Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                    DismissKeyboard();
                    eventHandler(sender, null);
                }
            };

            System.EventHandler<TextView.KeyEventArgs> keypressAction = (object sender, TextView.KeyEventArgs e) =>
            {
                if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
                {
                    Logger.Debug("keypress: " + e.Event.KeyCode.ToString());
                    DismissKeyboard();
                    eventHandler(sender, null);
                }
                else
                {
                    e.Handled = false;
                }
            };

            input.KeyPress += keypressAction;
            input.EditorAction += editorAction;
            input.FocusChange += Input_FocusChange;

            builder.SetPositiveButton(this.Resources.GetString(Resource.String.okay), eventHandler);
            builder.SetNegativeButton(this.Resources.GetString(Resource.String.cancel), eventHandlerCancel);
            // Set up the buttons

            BrowseFragment.browseUserDialog = builder.Create();
            try
            {
                BrowseFragment.browseUserDialog.Show();
                UiHelpers.DoNotEnablePositiveUntilText(BrowseFragment.browseUserDialog, input);

            }
            catch (WindowManagerBadTokenException e)
            {
                if (SeekerState.MainActivityRef == null || this.Activity == null)
                {
                    Logger.Firebase("WindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.MainActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.Activity.IsFinishing;
                    Logger.Firebase("WindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SeekerState.MainActivityRef == null || this.Activity == null)
                {
                    Logger.Firebase("Exception null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.MainActivityRef.IsFinishing;
                    bool isOurActivityFinishing = this.Activity.IsFinishing;
                    Logger.Firebase("Exception are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }

        }

        private void Input_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SeekerState.MainActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                Logger.Firebase("MainActivity_FocusChange" + err.Message);
            }
        }
    }
}