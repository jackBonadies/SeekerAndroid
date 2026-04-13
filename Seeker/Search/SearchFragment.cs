using Seeker.Chatroom;
using Seeker.Services;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Seeker.Search;
using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Button;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.FloatingActionButton;
using Java.Lang;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Common;
namespace Seeker
{
    public partial class SearchFragment : Fragment
    {
        public View rootView = null;
        private View noSearchesView = null;
        private Context context;
        public static bool ExpandAllResults { get => PreferencesState.ExpandAllResults; set => PreferencesState.ExpandAllResults = value; }
        public static IMenu ActionBarMenu = null;
        public static int LastSearchResponseCount = -1;

        public override void OnStart()
        {
            SearchFragment.Instance = this;
            Logger.Debug("SearchFragmentOnStart");
            base.OnStart();
        }

        public override void OnResume()
        {
            base.OnResume();
            Logger.Debug("Search Fragment On Resume");
            //you had a pending intent that could not get handled til now.
            if (MainActivity.goToSearchTab != int.MaxValue)
            {
                Logger.Debug("Search Fragment On Resume for wishlist");
                this.GoToTab(MainActivity.goToSearchTab, false, true);
                MainActivity.goToSearchTab = int.MaxValue;
            }
        }

        private void ClearFilterStringAndCached(bool force = false)
        {
            if (!PreferencesState.FilterSticky || force)
            {
                // Clear()
                SearchTabHelper.TextFilter.Reset();
                EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
                if (filterText.Text != string.Empty)
                {
                    //else you trigger the event.
                    filterText.Text = string.Empty;
                    UpdateDrawableState(filterText, true);
                }
                PreferencesState.FilterStickyString = string.Empty;

                PreferencesState.FilterFormat = FormatFilterType.Any;
                PreferencesState.FilterMinBitrateKbs = 0;
                PreferencesManager.SaveFilterControlsState();
                ResetFilterControlsUI();
            }
        }

        private void ResetFilterControlsUI()
        {
            var formatToggle = rootView?.FindViewById<MaterialButtonToggleGroup>(Resource.Id.formatToggleGroup);
            formatToggle?.Check(Resource.Id.formatAny);

            var bitrateToggle = rootView?.FindViewById<MaterialButtonToggleGroup>(Resource.Id.bitrateToggleGroup);
            bitrateToggle?.Check(Resource.Id.bitrateAny);
        }



        public override void SetMenuVisibility(bool menuVisible)
        {
            //this is necessary if programmatically moving to a tab from another activity..
            if (menuVisible)
            {
                var navigator = SeekerState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if (navigator != null)
                {
                    navigator.Menu.GetItem(1).SetCheckable(true);
                    navigator.Menu.GetItem(1).SetChecked(true);
                }
            }
            base.SetMenuVisibility(menuVisible);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.search_menu, menu); //test432
            (menu.FindItem(Resource.Id.action_search).Icon as Android.Graphics.Drawables.TransitionDrawable).CrossFadeEnabled = true;
            if (SearchTabHelper.SearchTarget == SearchTarget.Wishlist)
            {
                menu.FindItem(Resource.Id.action_add_to_wishlist).SetVisible(false);
            }
            else
            {
                menu.FindItem(Resource.Id.action_add_to_wishlist).SetVisible(true);
            }

            ActionBarMenu = menu;

            if (SearchTabHelper.CurrentlySearching)
            {
                GetTransitionDrawable().StartTransition(0);
            }
            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            var toggleItem = menu.FindItem(Resource.Id.action_toggle_expand_all);
            if (toggleItem != null)
            {
                bool isExpandable = PreferencesState.SearchResultStyle == SearchResultStyleEnum.ExpandableLegacy ||
                    PreferencesState.SearchResultStyle == SearchResultStyleEnum.ExpandableModern;
                toggleItem.SetVisible(isExpandable);
                if (isExpandable)
                {
                    toggleItem.SetTitle(ExpandAllResults
                        ? Resource.String.collapse_all
                        : Resource.String.expand_all);
                }
            }
            base.OnPrepareOptionsMenu(menu);
        }

        public static void SetCustomViewTabNumberInner(ImageView imgView, Context c)
        {
            int numTabs = int.MinValue;
            if (SearchTabHelper.SearchTarget == SearchTarget.Wishlist)
            {
                numTabs = -1;
            }
            else
            {
                numTabs = SearchTabHelper.SearchTabCollection.Keys.Count;
            }
            int idOfDrawable = int.MinValue;
            if (numTabs > 10)
            {
                numTabs = 10;
            }
            switch (numTabs)
            {
                case 1:
                    idOfDrawable = Resource.Drawable.numeric_1_box_multiple_outline;
                    break;
                case 2:
                    idOfDrawable = Resource.Drawable.numeric_2_box_multiple_outline;
                    break;
                case 3:
                    idOfDrawable = Resource.Drawable.numeric_3_box_multiple_outline;
                    break;
                case 4:
                    idOfDrawable = Resource.Drawable.numeric_4_box_multiple_outline;
                    break;
                case 5:
                    idOfDrawable = Resource.Drawable.numeric_5_box_multiple_outline;
                    break;
                case 6:
                    idOfDrawable = Resource.Drawable.numeric_6_box_multiple_outline;
                    break;
                case 7:
                    idOfDrawable = Resource.Drawable.numeric_7_box_multiple_outline;
                    break;
                case 8:
                    idOfDrawable = Resource.Drawable.numeric_8_box_multiple_outline;
                    break;
                case 9:
                    idOfDrawable = Resource.Drawable.numeric_9_box_multiple_outline;
                    break;
                case 10:
                    idOfDrawable = Resource.Drawable.numeric_9_plus_box_multiple_outline;
                    break;
                case -1: //wishlist
                    idOfDrawable = Resource.Drawable.wishlist_icon;
                    break;
            }

            Android.Graphics.Drawables.Drawable drawable = null;
            drawable = c.Resources.GetDrawable(idOfDrawable, c.Theme);
            imgView.SetImageDrawable(drawable);

        }

        public void SetCustomViewTabNumberImageViewState()
        {
            ImageView imgView = null;
            Context c = null;
            if (this.Activity is AndroidX.AppCompat.App.AppCompatActivity appCompat)
            {
                c = this.Activity;
                imgView = appCompat.SupportActionBar?.CustomView?.FindViewById<ImageView>(Resource.Id.search_tabs);
            }
            else
            {
                c = SeekerState.MainActivityRef;
                imgView = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<ImageView>(Resource.Id.search_tabs);
            }

            SetCustomViewTabNumberInner(imgView, c);
        }

        public EditText GetCustomViewSearchHere()
        {
            if (this.Activity is AndroidX.AppCompat.App.AppCompatActivity appCompat)
            {
                var editText = appCompat.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere);
                if (editText == null)
                {

                }
                return editText;
            }
            else
            {
                var editText = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
                if (editText == null)
                {

                }
                return editText;
            }
        }

        private void SetFilterState()
        {
            EditText filter = rootView.FindViewById<EditText>(Resource.Id.filterText);
            if (PreferencesState.FilterSticky)
            {
                filter.Text = PreferencesState.FilterStickyString;
            }
            else
            {
                filter.Text = SearchTabHelper.TextFilter.FilterString;
            }
            UpdateDrawableState(filter, true);
        }

        private void SetTransitionDrawableState()
        {
            if (SearchTabHelper.CurrentlySearching)
            {
                Logger.Debug("CURRENT SEARCHING SET TRANSITION DRAWABLE");
                GetTransitionDrawable().StartTransition(0);
            }
            else
            {
                GetTransitionDrawable().ResetTransition();
            }
            //forces refresh.
            ActionBarMenu.FindItem(Resource.Id.action_search).SetVisible(false);
            ActionBarMenu.FindItem(Resource.Id.action_search).SetVisible(true);
        }

        public void GoToTab(int tabToGoTo, bool force, bool fromIntent = false)
        {
            if (force || tabToGoTo != SearchTabHelper.CurrentTab)
            {
                int lastTab = SearchTabHelper.CurrentTab;
                SearchTabHelper.CurrentTab = tabToGoTo;

                //update for current tab
                //set icon state
                //set search text
                //set results
                //set filter if not sticky
                int fromTab = SearchTabHelper.CurrentTab;

                Action a = new Action(() =>
                {

                    if (!SearchTabHelper.SearchTabCollection.ContainsKey(tabToGoTo))
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.search_tab_error), ToastLength.Long);
                        SearchTabHelper.CurrentTab = lastTab;
                        fromTab = lastTab;
                        return;
                    }

                    if (tabToGoTo < 0)
                    {
                        if (!SearchTabHelper.SearchTabCollection[fromTab].IsLoaded())
                        {
                            SearchTabHelper.RestoreSearchResultsFromDisk(tabToGoTo, SeekerState.ActiveActivityRef);
                        }
                    }

                    GetCustomViewSearchHere().Text = SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm;
                    SetSearchHintTarget(SearchTabHelper.SearchTabCollection[fromTab].SearchTarget);
                    if (!fromIntent)
                    {
                        SetTransitionDrawableState();
                    }//timing issue where menu options invalidate etc. may not be done yet...
                    SetFilterState();

                    if (SearchTabHelper.SearchTabCollection[fromTab].TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive())
                    {
                        if (SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount != SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count)
                        {
                            Logger.Debug("filtering...");
                            UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);  //WE JUST NEED TO FILTER THE NEW RESPONSES!!
                        }
                        SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                        recyclerViewTransferItems.SetAdapter(CreateSearchConcatAdapter(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses));

                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);

                    }
                    else
                    {
                        SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.ToList();
                        recyclerViewTransferItems.SetAdapter(CreateSearchConcatAdapter(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses));

                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                    if (!fromIntent)
                    {
                        GetTransitionDrawable().InvalidateSelf();
                    }
                    this.SetCustomViewTabNumberImageViewState();
                    this.Activity?.InvalidateOptionsMenu(); //this wil be the new nullref if fragment isnt ready...

                    if (!fromIntent)
                    {
                        SetTransitionDrawableState();
                    }
                    UpdateNoSearchesView();
                });
                if (SeekerState.MainActivityRef == null)
                {
                    Logger.Firebase("mainActivityRef is null GoToTab");
                }
                SeekerState.MainActivityRef?.RunOnUiThread(a);
            }
        }




        public override bool OnOptionsItemSelected(IMenuItem item)
        {

            switch (item.ItemId)
            {
                case Resource.Id.action_search_target:
                    if (SearchTabHelper.SearchTarget == SearchTarget.Wishlist)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.wishlist_tab_target), ToastLength.Long);
                        return true;
                    }
                    ShowChangeTargetDialog();
                    return true;
                case Resource.Id.action_sort_results_by:
                    ShowChangeSortOrderDialog();
                    return true;
                case Resource.Id.action_search:
                    if (SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
                    {
                        Logger.Debug("transitionDrawable: REVERSE transition");
                        (item.Icon as Android.Graphics.Drawables.TransitionDrawable).ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                        SearchTabHelper.CancellationTokenSource.Cancel();
                        SearchTabHelper.CurrentlySearching = false;
                        return true;
                    }
                    else
                    {
                        (item.Icon as Android.Graphics.Drawables.TransitionDrawable).StartTransition(SearchToCloseDuration);
                        PerformBackUpRefresh();
                        Logger.Debug("START TRANSITION");
                        SearchTabHelper.CurrentlySearching = true;
                        SearchTabHelper.CancellationTokenSource = new CancellationTokenSource();
                        EditText editText = SeekerState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere);
                        string searchText = string.Empty;
                        if (editText == null)
                        {
                            searchText = SearchingText;
                        }
                        else
                        {
                            searchText = editText.Text;
                        }
                        SearchAPI(SearchTabHelper.CancellationTokenSource.Token, (item.Icon as Android.Graphics.Drawables.TransitionDrawable), searchText, SearchTabHelper.CurrentTab);
                        return true;
                    }
                case Resource.Id.action_change_result_style:
                    ShowChangeResultStyleBottomDialog();
                    return true;
                case Resource.Id.action_toggle_expand_all:
                    ExpandAllResults = !ExpandAllResults;
                    this.Activity?.InvalidateOptionsMenu();
                    SearchResultStyleChanged();
                    return true;
                case Resource.Id.action_add_to_wishlist:
                    AddSearchToWishlist();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void AddSearchToWishlist()
        {
            //here we "fork" the current search, adding it to the wishlist
            if (SearchTabHelper.LastSearchTerm == string.Empty || SearchTabHelper.LastSearchTerm == null)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.perform_search_first), ToastLength.Long);
                return;
            }
            SearchTabHelper.AddWishlistSearchTabFromCurrent();
            SeekerApplication.Toaster.ShowToast(string.Format(SeekerApplication.GetString(Resource.String.added_to_wishlist), SearchTabHelper.LastSearchTerm), ToastLength.Long);
            this.SetCustomViewTabNumberImageViewState();
        }

        public static SearchFragment GetSearchFragment()
        {
            foreach (Fragment frag in SeekerState.MainActivityRef.SupportFragmentManager.Fragments)
            {
                if (frag is SearchFragment sfrag)
                {
                    return sfrag;
                }
            }
            return null;
        }

        public void ShowSearchTabsDialog()
        {
            SearchTabDialog searchTabDialog = new SearchTabDialog();
            if (!this.IsAdded || this.Activity == null) //then child fragment manager will likely be null
            {
                Logger.InfoFirebase("ShowSearchTabsDialog, fragment no longer attached...");

                searchTabDialog.Show(SeekerState.MainActivityRef.SupportFragmentManager, "search tab dialog");
                //I tested this many times (outside of this clause).  Works very well.x
                //But I dont know if not attached fragment will just cause other issues later on... yes it will as for example adding a new search tab, there are methods that rely on this.Activity and this.rootView etc.
                return;
            }
            searchTabDialog.Show(this.ChildFragmentManager, "search tab dialog");
        }

        public static void UpdateDrawableState(EditText actv, bool purple = false)
        {
            if (actv.Text == string.Empty || actv.Text == null)
            {
                actv.SetCompoundDrawables(null, null, null, null);
            }
            else
            {
                var cancel = ContextCompat.GetDrawable(SeekerState.MainActivityRef, Resource.Drawable.ic_cancel_black_24dp);
                cancel.SetBounds(0, 0, cancel.IntrinsicWidth, cancel.IntrinsicHeight);
                if (purple)
                {
                    //https://developer.android.com/reference/android/graphics/PorterDuff.Mode
                    cancel.SetColorFilter(UiHelpers.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor), PorterDuff.Mode.SrcAtop);
                }
                actv.SetCompoundDrawables(null, null, cancel, null);
            }
        }


        public static void ConfigureSupportCustomView(View customView/*, Context contextJustInCase*/) //todo: seems to be an error. which seems entirely possible. where ActiveActivityRef does not get set yet.
        {
            Logger.Debug("ConfigureSupportCustomView");
            AutoCompleteTextView actv = customView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
            try
            {
                actv.Text = SearchingText; //this works with string.Empty and emojis so I dont think its here...
                UpdateDrawableState(actv);
                actv.Touch += Actv_Touch;
            }
            catch (System.ArgumentException e)
            {
                Logger.Firebase("ArugmentException Value does not fall within range: " + SearchingText + " " + e.Message);
            }
            catch (System.Exception e)
            {
                Logger.Firebase("catchException Value does not fall within range: " + SearchingText + " " + e.Message);
            }
            catch
            {
                Logger.Firebase("catchunspecException Value does not fall within range: " + SearchingText);
            }
            ImageView iv = customView.FindViewById<ImageView>(Resource.Id.search_tabs);
            iv.Click += Iv_Click;
            actv.EditorAction -= Search_EditorActionHELPER;
            actv.EditorAction += Search_EditorActionHELPER;
            SetSearchHintTarget(SearchTabHelper.SearchTarget, actv);

            Context contextToUse = SeekerState.ActiveActivityRef;

            actv.Adapter = new ArrayAdapter<string>(contextToUse, Resource.Layout.search_dropdown_item, PreferencesState.SearchHistory);
            actv.KeyPress -= Actv_KeyPressHELPER;
            actv.KeyPress += Actv_KeyPressHELPER;
            actv.FocusChange += MainActivity_FocusChange;
            actv.TextChanged += Actv_TextChanged;

            SetCustomViewTabNumberInner(iv, contextToUse);
        }

        private static void Actv_Touch(object sender, View.TouchEventArgs e)
        {
            EditText editText = sender as EditText;
            e.Handled = false;
            if (e.Event.GetX() >= (editText.Width - editText.TotalPaddingRight))
            {
                if (e.Event.Action == MotionEventActions.Up)
                {
                    editText.Text = string.Empty;
                    UpdateDrawableState(editText);
                }
            }
        }

        private static string SearchingText = string.Empty;

        private static void Actv_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            //if it went from non empty to empty, or vice versa
            if (SearchingText == string.Empty && e.Text.ToString() != string.Empty)
            {
                UpdateDrawableState(sender as EditText);
            }
            else if (SearchingText != string.Empty && e.Text.ToString() == string.Empty)
            {
                UpdateDrawableState(sender as EditText);
            }
            SearchingText = e.Text.ToString();
        }

        public static void MainActivity_FocusChange(object sender, View.FocusChangeEventArgs e)
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

        public void UpdateNoSearchesView()
        {
            if (noSearchesView == null)
            {
                return;
            }
            bool neverSearched = string.IsNullOrEmpty(SearchTabHelper.LastSearchTerm);
            noSearchesView.Visibility = neverSearched ? ViewStates.Visible : ViewStates.Gone;
        }

        public void SearchResultStyleChanged()
        {
            this.Activity?.InvalidateOptionsMenu();
            RecyclerView rv = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewSearches); //TODO //TODO //TODO

            if (SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive())
            {
                rv.SetAdapter(CreateSearchConcatAdapter(SearchTabHelper.UI_SearchResponses));
            }
            else
            {
                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses.ToList();
                rv.SetAdapter(CreateSearchConcatAdapter(SearchTabHelper.UI_SearchResponses));
            }
        }



        private static void Iv_Click(object sender, EventArgs e)
        {
            if (!SearchFragment.Instance.IsAdded)
            {
                SearchFragment f = GetSearchFragment(); //is there an attached fragment?? i.e. is our instance just a stale one..
                if (f == null)
                {
                    Logger.InfoFirebase("search fragment not on activities fragment manager");
                }
                else if (!f.IsAdded)
                {
                    Logger.InfoFirebase("search fragment from activities fragment manager is not added");
                }
                else
                {
                    Logger.InfoFirebase("search fragment from activities fragment manager is good, though not setting it");
                    //SearchFragment.Instance = f; 
                }
                Logger.Firebase("SearchFragment.Instance.IsAdded == false, currently searching: " + SearchTabHelper.CurrentlySearching);
            }
            SearchFragment.Instance.ShowSearchTabsDialog();
        }

        public static void ShowChangeResultStyleBottomDialog()
        {
            BottomSheetDialogFragmentMenu bsdf = new BottomSheetDialogFragmentMenu();
            bsdf.HasOptionsMenu = true;
            bsdf.ShowNow(SeekerState.MainActivityRef.SupportFragmentManager, "options");
        }

        public static volatile SearchFragment Instance = null;
        public RecyclerView recyclerViewChips;
        public ChipsItemRecyclerAdapter recyclerChipsAdapter;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Instance = this;
            HasOptionsMenu = true;
            Logger.Debug("SearchFragmentOnCreateView");
            Logger.Debug("SearchFragmentOnCreateView - SearchResponses.Count=" + SearchTabHelper.SearchResponses.Count);
            this.rootView = inflater.Inflate(Resource.Layout.searches, container, false);

            context = this.Context;

            recyclerViewChips = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewChips);
            recyclerViewChips.Visibility = ViewStates.Visible;

            var manager = new LinearLayoutManager(this.Context, LinearLayoutManager.Horizontal, false);
            recyclerViewChips.SetItemAnimator(null);
            recyclerViewChips.SetLayoutManager(manager);
            recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
            recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);

            View bottomSheetView = rootView.FindViewById<View>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(bottomSheetView);
            bsb.Hideable = true;
            bsb.PeekHeight = 320;
            bsb.State = BottomSheetBehavior.StateHidden;

            CheckBox filterSticky = rootView.FindViewById<CheckBox>(Resource.Id.stickyFilterCheckbox);
            filterSticky.Checked = PreferencesState.FilterSticky;
            filterSticky.CheckedChange += FilterSticky_CheckedChange;

            //bsb.SetBottomSheetCallback(new MyCallback());
            View b = rootView.FindViewById<View>(Resource.Id.bsbutton);
            (b as FloatingActionButton).SetImageResource(Resource.Drawable.ic_filter_list_white_24dp);
            View v = rootView.FindViewById<View>(Resource.Id.focusableLayout);
            v.Focusable = true;
            //SetFocusable(int) was added in API26. bool was there since API1
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                v.SetFocusable(ViewFocusability.Focusable);
            }
            else
            {
                //v.SetFocusable(true); no bool method in xamarin...
            }

            v.FocusableInTouchMode = true;
            b.Click += B_Click;

            recyclerViewTransferItems = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewSearches);
            recycleLayoutManager = new LinearLayoutManager(Activity);
            recyclerViewTransferItems.SetItemAnimator(null); //todo
            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);
            if (SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive())
            {
                recyclerViewTransferItems.SetAdapter(CreateSearchConcatAdapter(SearchTabHelper.UI_SearchResponses));
            }
            else
            {
                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses.ToList();
                recyclerViewTransferItems.SetAdapter(CreateSearchConcatAdapter(SearchTabHelper.UI_SearchResponses));
            }

            noSearchesView = rootView.FindViewById<View>(Resource.Id.noSearchesView);
            UpdateNoSearchesView();

            SeekerState.ClearSearchHistoryEventsFromTarget(this);
            SeekerState.ClearSearchHistory += SeekerState_ClearSearchHistory;
            SeekerState.SoulseekClient.ClearSearchResponseReceivedFromTarget(this);
            int x = SeekerState.SoulseekClient.GetInvocationListOfSearchResponseReceived();
            Logger.Debug("NUMBER OF DELEGATES AFTER WE REMOVED OURSELF: (before doing the deep clear this would increase every rotation orientation)" + x);
            Logger.Debug("SearchFragmentOnCreateViewEnd - SearchResponses.Count=" + SearchTabHelper.SearchResponses.Count);

            EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
            filterText.TextChanged += FilterText_TextChanged;
            filterText.FocusChange += FilterText_FocusChange;
            filterText.EditorAction += FilterText_EditorAction;
            filterText.Touch += FilterText_Touch;
            if (PreferencesState.FilterSticky)
            {
                filterText.Text = PreferencesState.FilterStickyString;
            }
            UpdateDrawableState(filterText, true);

            Button showHideSmartFilters = rootView.FindViewById<Button>(Resource.Id.toggleSmartFilters);
            showHideSmartFilters.Text = PreferencesState.ShowSmartFilters ? this.GetString(Resource.String.HideSmartFilters) : this.GetString(Resource.String.ShowSmartFilters);
            showHideSmartFilters.Click += ShowHideSmartFilters_Click;

            SetupFilterControls();

            return rootView;
        }

        private void SetupFilterControls()
        {
            var bgTint = GetSegmentedButtonBgTint();
            var textTint = GetSegmentedButtonTextTint();

            var formatToggle = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.formatToggleGroup);
            ApplyToggleGroupTint(formatToggle, bgTint, textTint);
            formatToggle.Check(GetFormatButtonId(PreferencesState.FilterFormat));
            formatToggle.ButtonChecked += FormatToggle_ButtonChecked;

            var bitrateToggle = rootView.FindViewById<MaterialButtonToggleGroup>(Resource.Id.bitrateToggleGroup);
            ApplyToggleGroupTint(bitrateToggle, bgTint, textTint);
            for (int i = 0; i < bitrateToggle.ChildCount; i++)
            {
                var btn = (MaterialButton)bitrateToggle.GetChildAt(i);
                btn.SetPadding(0, btn.PaddingTop, 0, btn.PaddingBottom);
            }
            bitrateToggle.Check(GetBitrateButtonId(PreferencesState.FilterMinBitrateKbs));
            bitrateToggle.ButtonChecked += BitrateToggle_ButtonChecked;
        }

        private Android.Content.Res.ColorStateList GetSegmentedButtonBgTint()
        {
            var typedValue = new Android.Util.TypedValue();
            this.Context.Theme.ResolveAttribute(Resource.Attribute.mainPurple, typedValue, true);
            int accentColor = typedValue.Data;

            this.Context.Theme.ResolveAttribute(Resource.Attribute.dialog_background, typedValue, true);
            int uncheckedColor = typedValue.Data;

            var states = new int[][]
            {
                new int[] { Android.Resource.Attribute.StateChecked },
                new int[] { -Android.Resource.Attribute.StateChecked }
            };
            var colors = new int[] { accentColor, uncheckedColor };
            return new Android.Content.Res.ColorStateList(states, colors);
        }

        private Android.Content.Res.ColorStateList GetSegmentedButtonTextTint()
        {
            var typedValue = new Android.Util.TypedValue();
            this.Context.Theme.ResolveAttribute(Resource.Attribute.normalTextColor, typedValue, true);
            int uncheckedColor = typedValue.Data;

            var states = new int[][]
            {
                new int[] { Android.Resource.Attribute.StateChecked },
                new int[] { -Android.Resource.Attribute.StateChecked }
            };
            var colors = new int[] { Android.Graphics.Color.White, uncheckedColor };
            return new Android.Content.Res.ColorStateList(states, colors);
        }

        private static void ApplyToggleGroupTint(MaterialButtonToggleGroup group, Android.Content.Res.ColorStateList bgTint, Android.Content.Res.ColorStateList textTint)
        {
            for (int i = 0; i < group.ChildCount; i++)
            {
                var btn = (MaterialButton)group.GetChildAt(i);
                btn.BackgroundTintList = bgTint;
                btn.SetTextColor(textTint);
            }
        }

        private void FormatToggle_ButtonChecked(object sender, MaterialButtonToggleGroup.ButtonCheckedEventArgs e)
        {
            if (!e.P2)
            {
                return;
            }
            switch (e.P1)
            {
                case Resource.Id.formatLossless:
                    PreferencesState.FilterFormat = FormatFilterType.Lossless;
                    break;
                case Resource.Id.formatLossy:
                    PreferencesState.FilterFormat = FormatFilterType.Lossy;
                    break;
                default:
                    PreferencesState.FilterFormat = FormatFilterType.Any;
                    break;
            }
            PreferencesManager.SaveFilterControlsState();
            ApplyFilterControls();
        }

        private void BitrateToggle_ButtonChecked(object sender, MaterialButtonToggleGroup.ButtonCheckedEventArgs e)
        {
            if (!e.P2)
            {
                return;
            }
            switch (e.P1)
            {
                case Resource.Id.bitrate320:
                    PreferencesState.FilterMinBitrateKbs = 320;
                    break;
                case Resource.Id.bitrate256:
                    PreferencesState.FilterMinBitrateKbs = 256;
                    break;
                case Resource.Id.bitrate192:
                    PreferencesState.FilterMinBitrateKbs = 192;
                    break;
                case Resource.Id.bitrate128:
                    PreferencesState.FilterMinBitrateKbs = 128;
                    break;
                default:
                    PreferencesState.FilterMinBitrateKbs = 0;
                    break;
            }
            PreferencesManager.SaveFilterControlsState();
            ApplyFilterControls();
        }

        private void ApplyFilterControls()
        {
            if (AreFilterControlsActive() || AreChipsFiltering() || SearchTabHelper.TextFilter.IsFiltered)
            {
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);
                recyclerSearchAdapter.NotifyDataSetChanged();
            }
            else
            {
                SearchTabHelper.UI_SearchResponses.Clear();
                SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);
                recyclerSearchAdapter.NotifyDataSetChanged();
            }
            NotifySearchHeaderChanged();
        }

        private static int GetFormatButtonId(FormatFilterType format)
        {
            switch (format)
            {
                case FormatFilterType.Lossless: return Resource.Id.formatLossless;
                case FormatFilterType.Lossy: return Resource.Id.formatLossy;
                default: return Resource.Id.formatAny;
            }
        }

        private static int GetBitrateButtonId(int bitrateKbs)
        {
            switch (bitrateKbs)
            {
                case 320: return Resource.Id.bitrate320;
                case 256: return Resource.Id.bitrate256;
                case 192: return Resource.Id.bitrate192;
                case 128: return Resource.Id.bitrate128;
                default: return Resource.Id.bitrateAny;
            }
        }

        private void ShowHideSmartFilters_Click(object sender, EventArgs e)
        {
            PreferencesState.ShowSmartFilters = !PreferencesState.ShowSmartFilters;
            Button showHideSmartFilters = rootView.FindViewById<Button>(Resource.Id.toggleSmartFilters);
            showHideSmartFilters.Text = PreferencesState.ShowSmartFilters ? this.GetString(Resource.String.HideSmartFilters) : this.GetString(Resource.String.ShowSmartFilters);
            if (PreferencesState.ShowSmartFilters)
            {
                if (SearchTabHelper.CurrentlySearching)
                {
                    return; //it will update on complete search
                }
                if ((SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].SearchResponses?.Count ?? 0) != 0)
                {
                    List<ChipDataItem> chipDataItems = ChipsHelper.GetChipDataItemsFromSearchResults(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].SearchResponses, SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].LastSearchTerm, PreferencesState.SmartFilterOptions);
                    SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = chipDataItems;
                    SeekerState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                    }));
                }
            }
            else
            {
                SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = null;
                SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipsFilter = null; //in case there was previously a filter
                SearchFragment.Instance.RefreshOnChipChanged();
                SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(null);
                SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
            }
        }

        private void FilterText_Touch(object sender, View.TouchEventArgs e)
        {
            EditText editText = sender as EditText;
            e.Handled = false;
            if (e.Event.GetX() >= (editText.Width - editText.TotalPaddingRight))
            {
                if (e.Event.Action == MotionEventActions.Up)
                {
                    editText.Text = string.Empty;
                    UpdateDrawableState(editText, true);

                    ClearFilterStringAndCached(true);
                }
            }
        }

        /// <summary>
        /// Are chips filtering out results..
        /// </summary>
        /// <returns></returns>
        private static bool AreChipsFiltering()
        {
            if (!PreferencesState.ShowSmartFilters || (SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems?.Count ?? 0) == 0)
            {
                return false;
            }
            else
            {
                return SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems.Any(i => i.IsChecked);
            }
        }

        private static bool AreFilterControlsActive()
        {
            return PreferencesState.FilterFormat != FormatFilterType.Any
                || PreferencesState.FilterMinBitrateKbs > 0;
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

        private void B_Click(object sender, EventArgs e)
        {
            View bottomSheetView = rootView.FindViewById<View>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(bottomSheetView);
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
                        rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
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
                    rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                }
                bsb.State = BottomSheetBehavior.StateHidden;
            }
        }

        private void FilterSticky_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            PreferencesState.FilterSticky = e.IsChecked;
            if (PreferencesState.FilterSticky)
            {
                PreferencesState.FilterStickyString = SearchTabHelper.TextFilter.FilterString;
            }
        }

        private static void Search_EditorActionHELPER(object sender, TextView.EditorActionEventArgs e)
        {
            SearchFragment.Instance.Search_EditorAction(sender, e);
        }

        private void Search_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Search)
            {
                string editSearchText = null;
                EditText editTextSearch = SeekerState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere); //get asap to avoid nullref...
                if (editTextSearch == null)
                {
                    EditText searchHere = (this.Activity as AndroidX.AppCompat.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere);
                    if (searchHere != null)
                    {
                        editSearchText = searchHere.Text;
                    }
                    else
                    {
                        editSearchText = SearchingText;
                    }
                }
                else
                {
                    editSearchText = editTextSearch.Text;
                }
                Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                }
                catch (System.Exception ex)
                {
                    Logger.Firebase(ex.Message + " error closing keyboard");
                }
                var transitionDrawable = GetTransitionDrawable();
                if (SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
                {
                    Logger.Debug("transitionDrawable: reverse transition");
                    transitionDrawable.ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                    SearchTabHelper.CancellationTokenSource.Cancel();
                    SearchTabHelper.CurrentlySearching = false;
                }
                else
                {
                    Logger.Debug("transitionDrawable: start transition");
                    transitionDrawable.StartTransition(SearchToCloseDuration);
                    PerformBackUpRefresh();
                    SearchTabHelper.CurrentlySearching = true;
                }
                SearchTabHelper.CancellationTokenSource = new CancellationTokenSource();
                SearchAPI(SearchTabHelper.CancellationTokenSource.Token, transitionDrawable, editSearchText, SearchTabHelper.CurrentTab);
                (sender as AutoCompleteTextView).DismissDropDown();
            }
        }

        private void ChooseUserInput_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            SearchTabHelper.SearchTargetChosenUser = e.Text.ToString();
        }

        private void AllUsers_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.AllUsers;
            targetRoomInputLayout.Visibility = ViewStates.Gone;
            chooseUserInputLayout.Visibility = ViewStates.Gone;
            SetSearchHintTarget(SearchTarget.AllUsers);
        }

        private void ChosenUser_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
            targetRoomInputLayout.Visibility = ViewStates.Gone;
            chooseUserInputLayout.Visibility = ViewStates.Visible;
            SetSearchHintTarget(SearchTarget.ChosenUser);
        }

        private void UserList_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.UserList;
            targetRoomInputLayout.Visibility = ViewStates.Gone;
            chooseUserInputLayout.Visibility = ViewStates.Gone;
            SetSearchHintTarget(SearchTarget.UserList);
        }

        public static void SetSearchHintTarget(SearchTarget target, AutoCompleteTextView actv = null)
        {
            if (actv == null)
            {
                actv = SeekerState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
            }
            if (actv != null)
            {
                switch (target)
                {
                    case SearchTarget.AllUsers:
                        actv.Hint = SeekerApplication.ApplicationContext.GetString(Resource.String.search_here);
                        break;
                    case SearchTarget.UserList:
                        actv.Hint = SeekerApplication.ApplicationContext.GetString(Resource.String.saerch_user_list);
                        break;
                    case SearchTarget.Room:
                        actv.Hint = string.Format(SeekerApplication.ApplicationContext.GetString(Resource.String.search_room_), SearchTabHelper.SearchTargetChosenRoom);
                        break;
                    case SearchTarget.ChosenUser:
                        actv.Hint = string.Format(SeekerApplication.ApplicationContext.GetString(Resource.String.search_user_), SearchTabHelper.SearchTargetChosenUser);
                        break;
                    case SearchTarget.Wishlist:
                        actv.Hint = SeekerApplication.ApplicationContext.GetString(Resource.String.wishlist_search);
                        break;
                }
            }
        }

        public Android.Graphics.Drawables.TransitionDrawable GetTransitionDrawable()
        {
            Android.Graphics.Drawables.TransitionDrawable icon = ActionBarMenu?.FindItem(Resource.Id.action_search)?.Icon as Android.Graphics.Drawables.TransitionDrawable;
            //tested this and it works well
            if (icon == null)
            {
                if (this.Activity == null)
                {
                    Logger.InfoFirebase("GetTransitionDrawable activity is null");
                    SearchFragment f = GetSearchFragment();
                    if (f == null)
                    {
                        Logger.InfoFirebase("GetTransitionDrawable no search fragment attached to activity");
                    }
                    else if (!f.IsAdded)
                    {
                        Logger.InfoFirebase("GetTransitionDrawable attached but not added");
                    }
                    else if (f.Activity == null)
                    {
                        Logger.InfoFirebase("GetTransitionDrawable f.Activity activity is null");
                    }
                    else
                    {
                        Logger.InfoFirebase("we should be using the fragment manager one...");
                    }
                }
                //when coming from an intent its actually (toolbar.Menu.FindItem(Resource.Id.action_search)) that is null.  so the menu is there, just no action_search menu item.
                AndroidX.AppCompat.Widget.Toolbar toolbar = (this.Activity as AndroidX.AppCompat.App.AppCompatActivity).FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
                return toolbar.Menu.FindItem(Resource.Id.action_search).Icon as Android.Graphics.Drawables.TransitionDrawable; //nullref
            }
            else
            {
                return icon;
            }
            //return ActionBarMenu.FindItem(Resource.Id.action_search).Icon as Android.Graphics.Drawables.TransitionDrawable; // we got nullref here...

        }

        public static void ClearFocusSearchEditText()
        {
            SeekerState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<View>(Resource.Id.searchHere)?.ClearFocus();
        }




        public void ShowChangeSortOrderDialog()
        {
            Context toUse = this.Activity != null ? this.Activity : SeekerState.MainActivityRef;
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(toUse);
            builder.SetTitle(Resource.String.sort_results_by);
            View viewInflated = LayoutInflater.From(toUse).Inflate(Resource.Layout.changeresultsortorder, this.rootView as ViewGroup, false); //TODO replace rootView with ActiveActivity.GetContent()

            AndroidX.AppCompat.Widget.AppCompatRadioButton sortAvailability = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.availability);
            AndroidX.AppCompat.Widget.AppCompatRadioButton sortSpeed = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.speed);
            AndroidX.AppCompat.Widget.AppCompatRadioButton sortFolderNameAlpha = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.folderNameAlpha);
            AndroidX.AppCompat.Widget.AppCompatRadioButton sortBitrate = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.bitRate);
            CheckBox checkBoxSetAsDefault = viewInflated.FindViewById<CheckBox>(Resource.Id.setAsDefault);
            switch (SearchTabHelper.SortHelperSorting)
            {
                case SearchResultSorting.Available:
                    sortAvailability.Checked = true;
                    break;
                case SearchResultSorting.Fastest:
                    sortSpeed.Checked = true;
                    break;
                case SearchResultSorting.FolderAlphabetical:
                    sortFolderNameAlpha.Checked = true;
                    break;
                case SearchResultSorting.BitRate:
                    sortBitrate.Checked = true;
                    break;
            }

            sortAvailability.Click += SortAvailabilityClick;
            sortSpeed.Click += SortSpeedClick;
            sortFolderNameAlpha.Click += SortFoldernameAlphaClick;
            sortBitrate.Click += SortBitrate_Click;

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> positiveButtonEventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                //if cancelled via back button we dont go here

                if (checkBoxSetAsDefault.Checked)
                {
                    var old = PreferencesState.DefaultSearchResultSortAlgorithm;
                    PreferencesState.DefaultSearchResultSortAlgorithm = SearchTabHelper.SortHelperSorting; //whatever one we just changed it to.
                    if (old != PreferencesState.DefaultSearchResultSortAlgorithm)
                    {
                        PreferencesManager.SaveDefaultSearchResultSortAlgorithm();
                    }
                }
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    dialogInstance.Dismiss();
                }
            });

            builder.SetPositiveButton(Resource.String.okay, positiveButtonEventHandler);
            dialogInstance = builder.Create();
            dialogInstance.Show();
        }

        private void SortBitrate_Click(object sender, EventArgs e)
        {
            UpdateSortAvailability(SearchResultSorting.BitRate);
        }

        private void SortAvailabilityClick(object sender, EventArgs e)
        {
            UpdateSortAvailability(SearchResultSorting.Available);
        }

        private void SortSpeedClick(object sender, EventArgs e)
        {
            UpdateSortAvailability(SearchResultSorting.Fastest);
        }

        private void SortFoldernameAlphaClick(object sender, EventArgs e)
        {
            UpdateSortAvailability(SearchResultSorting.FolderAlphabetical);
        }

        private void UpdateSortAvailability(SearchResultSorting searchResultSorting)
        {
            if (SearchTabHelper.SortHelperSorting != searchResultSorting)
            {
                lock (SearchTabHelper.SortHelperLockObject) //this is also always going to be on the UI thread. so we have that guaranteeing safety. 
                {
                    SearchTabHelper.SortHelperSorting = searchResultSorting;
                    SearchTabHelper.SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(SearchTabHelper.SortHelperSorting));

                    //put all the search responses into the new sort helper
                    if (SearchTabHelper.SearchResponses != null)
                    {
                        foreach (var searchResponse in SearchTabHelper.SearchResponses)
                        {
                            if (!SearchTabHelper.SortHelper.ContainsKey(searchResponse))
                            {
                                SearchTabHelper.SortHelper.Add(searchResponse, null);
                            }
                            else
                            {

                            }
                        }
                    }

                    //now that they are sorted, replace them.
                    SearchTabHelper.SearchResponses = SearchTabHelper.SortHelper.Keys.ToList();

                    if (SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive())
                    {
                        UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);
                        recyclerSearchAdapter.NotifyDataSetChanged();
                    }
                    else
                    {
                        SearchTabHelper.UI_SearchResponses.Clear();
                        SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);
                        recyclerSearchAdapter.NotifyDataSetChanged();
                    }
                    NotifySearchHeaderChanged();



                }
            }
        }

        private AutoCompleteTextView chooseUserInput = null;
        private View chooseUserInputLayout = null;
        private AutoCompleteTextView targetRoomInput = null;
        private View targetRoomInputLayout = null;
        public void ShowChangeTargetDialog()
        {
            Context toUse = this.Activity != null ? this.Activity : SeekerState.MainActivityRef;
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(toUse);
            builder.SetTitle(Resource.String.search_target_);
            View viewInflated = LayoutInflater.From(toUse).Inflate(Resource.Layout.changeusertarget, this.rootView as ViewGroup, false);
            chooseUserInput = viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserInput);
            chooseUserInputLayout = viewInflated.FindViewById<View>(Resource.Id.chosenUserInputLayout);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(chooseUserInput);
            targetRoomInput = viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.targetRoomInput);
            targetRoomInputLayout = viewInflated.FindViewById<View>(Resource.Id.targetRoomInputLayout);
            List<string> joinedRooms = ChatroomController.JoinedRoomNames?.ToList() ?? new List<string>();
            targetRoomInput.Adapter = new ArrayAdapter<string>(SeekerState.ActiveActivityRef, Android.Resource.Layout.SimpleDropDownItem1Line, joinedRooms);
            targetRoomInput.Text = SearchTabHelper.SearchTargetChosenRoom;

            AndroidX.AppCompat.Widget.AppCompatRadioButton allUsers = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.allUsers);
            AndroidX.AppCompat.Widget.AppCompatRadioButton chosenUser = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.chosenUser);
            AndroidX.AppCompat.Widget.AppCompatRadioButton userList = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetUserList);
            AndroidX.AppCompat.Widget.AppCompatRadioButton room = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetRoom);
            chooseUserInput.Text = SearchTabHelper.SearchTargetChosenUser;
            switch (SearchTabHelper.SearchTarget)
            {
                case SearchTarget.AllUsers:
                    allUsers.Checked = true;
                    chooseUserInputLayout.Visibility = ViewStates.Gone;
                    targetRoomInputLayout.Visibility = ViewStates.Gone;
                    break;
                case SearchTarget.UserList:
                    userList.Checked = true;
                    targetRoomInputLayout.Visibility = ViewStates.Gone;
                    chooseUserInputLayout.Visibility = ViewStates.Gone;
                    break;
                case SearchTarget.ChosenUser:
                    chosenUser.Checked = true;
                    targetRoomInputLayout.Visibility = ViewStates.Gone;
                    chooseUserInputLayout.Visibility = ViewStates.Visible;
                    chooseUserInput.Text = SearchTabHelper.SearchTargetChosenUser;
                    break;
                case SearchTarget.Room:
                    room.Checked = true;
                    chooseUserInputLayout.Visibility = ViewStates.Gone;
                    targetRoomInputLayout.Visibility = ViewStates.Visible;
                    break;
            }

            allUsers.Click += AllUsers_Click;
            room.Click += Room_Click;
            chosenUser.Click += ChosenUser_Click;
            userList.Click += UserList_Click;
            chooseUserInput.TextChanged += ChooseUserInput_TextChanged;
            targetRoomInput.TextChanged += TargetRoomInput_TextChanged;


            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandlerClose = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                SetSearchHintTarget(SearchTabHelper.SearchTarget, (this.Activity as AndroidX.AppCompat.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere)); //in case of hitting choose user, you still have to update the name (since that gets input after clicking radio button)...
                if (SearchTabHelper.SearchTarget == SearchTarget.ChosenUser && !string.IsNullOrEmpty(SearchTabHelper.SearchTargetChosenUser))
                {
                    SeekerState.RecentUsersManager.AddUserToTop(SearchTabHelper.SearchTargetChosenUser, true);
                }
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    dialogInstance.Dismiss();
                }
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search) //i get a lot of imenull..
                {
                    Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Firebase(ex.Message + " error closing keyboard");
                    }
                    eventHandlerClose(sender, null);
                }
            };

            chooseUserInput.EditorAction += editorAction;
            targetRoomInput.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.okay, eventHandlerClose);
            dialogInstance = builder.Create();
            dialogInstance.Show();
        }
        private void TargetRoomInput_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            SearchTabHelper.SearchTargetChosenRoom = e.Text.ToString();
        }

        private void Room_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.Room;
            targetRoomInputLayout.Visibility = ViewStates.Visible;
            chooseUserInputLayout.Visibility = ViewStates.Gone;
            SetSearchHintTarget(SearchTarget.Room);
        }

        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;

        private void FilterText_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Search)
            {
                Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
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

        //should be called whenever either the filter changes, new search results come in, the search gets cleared, etc.
        //includes chips
        private void UpdateFilteredResponses(SearchTab searchTab)
        {
            //The Rules:
            //if separated by space, then it must contain both them, in any order
            //if - in front then it must not contain this word
            //there are also several keywords

            Logger.Debug("Words To Avoid: " + searchTab.TextFilter.WordsToAvoid.ToString());
            Logger.Debug("Words To Include: " + searchTab.TextFilter.WordsToInclude.ToString());
            Logger.Debug("Whether to Filter: " + searchTab.TextFilter.IsFiltered);
            Logger.Debug("FilterString: " + searchTab.TextFilter.FilterString);
            bool hideLocked = PreferencesState.HideLockedResultsInSearch;
            var mergedFlags = MergeFilterFlags(searchTab.TextFilter.FilterSpecialFlags);
            searchTab.UI_SearchResponses.Clear();
            searchTab.UI_SearchResponses.AddRange(searchTab.SearchResponses.FindAll(
                s => SearchFilter.MatchesAllCriteria(s, searchTab.ChipsFilter, mergedFlags, searchTab.TextFilter.WordsToAvoid, searchTab.TextFilter.WordsToInclude, hideLocked)));
        }

        private static FilterSpecialFlags MergeFilterFlags(FilterSpecialFlags textFlags)
        {
            var merged = new FilterSpecialFlags();
            if (textFlags != null)
            {
                merged.MinBitRateKBS = textFlags.MinBitRateKBS;
                merged.MinFileSizeMB = textFlags.MinFileSizeMB;
                merged.MinFoldersInFile = textFlags.MinFoldersInFile;
                merged.IsVBR = textFlags.IsVBR;
                merged.IsCBR = textFlags.IsCBR;
                merged.FormatFilter = textFlags.FormatFilter;
                merged.ContainsSpecialFlags = textFlags.ContainsSpecialFlags;
            }
            if (PreferencesState.FilterFormat != FormatFilterType.Any)
            {
                merged.FormatFilter = PreferencesState.FilterFormat;
                merged.ContainsSpecialFlags = true;
            }
            if (PreferencesState.FilterMinBitrateKbs > 0)
            {
                merged.MinBitRateKBS = System.Math.Max(merged.MinBitRateKBS, PreferencesState.FilterMinBitrateKbs);
                merged.ContainsSpecialFlags = true;
            }
            return merged;
        }

        private void FilterText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            Logger.Debug("Text Changed: " + e.Text);
            string oldFilterString = SearchTabHelper.TextFilter.IsFiltered ? SearchTabHelper.TextFilter.FilterString : string.Empty;
            if ((e.Text != null && e.Text.ToString() != string.Empty && SearchTabHelper.SearchResponses != null) || AreChipsFiltering() || AreFilterControlsActive())
            {

#if DEBUG
                var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
                SearchTabHelper.TextFilter.Set(e.Text.ToString());
                if (PreferencesState.FilterSticky)
                {
                    PreferencesState.FilterStickyString = SearchTabHelper.TextFilter.FilterString;
                }

                var oldList = SearchTabHelper.UI_SearchResponses.ToList();
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);
#if DEBUG

                int oldCount = oldList.Count;
                int newCount = SearchTabHelper.UI_SearchResponses.Count();
                Logger.Debug($"update filtered only - old {oldCount} new {newCount} time {sw.ElapsedMilliseconds} ms");
#endif
#if DEBUG
                sw.Stop();
#endif
                recyclerSearchAdapter.NotifyDataSetChanged(); //does have the nice effect that if nothing changes, you dont just back to top. (unlike old method)
#if DEBUG
                Logger.Debug($"old {oldCount} new {newCount} time {sw.ElapsedMilliseconds} ms");
#endif

            }
            else
            {
                SearchTabHelper.TextFilter.Reset();
                if (PreferencesState.FilterSticky)
                {
                    PreferencesState.FilterStickyString = string.Empty;
                }

                SearchTabHelper.UI_SearchResponses.Clear();
                SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);

                recyclerSearchAdapter.NotifyDataSetChanged(); //does have the nice effect that if nothing changes, you dont just back to top.
            }
            NotifySearchHeaderChanged();

            if (oldFilterString == string.Empty && e.Text.ToString() != string.Empty)
            {
                UpdateDrawableState(sender as EditText, true);
            }
            else if (oldFilterString != string.Empty && e.Text.ToString() == string.Empty)
            {
                UpdateDrawableState(sender as EditText, true);
            }
        }

        /// <summary>
        /// !!!!!!!!!!!!!!!!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RefreshOnChipChanged()
        {
            if (AreChipsFiltering() || SearchTabHelper.TextFilter.IsFiltered || AreFilterControlsActive())
            {
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);
                recyclerSearchAdapter.NotifyDataSetChanged();
            }
            else
            {
                SearchTabHelper.UI_SearchResponses.Clear();
                SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);
                recyclerSearchAdapter.NotifyDataSetChanged();
            }
            NotifySearchHeaderChanged();
        }

        private void SeekerState_ClearSearchHistory(object sender, EventArgs e)
        {
            PreferencesState.SearchHistory = new List<string>();
            PreferencesManager.ClearSearchHistory();
            if (SeekerState.MainActivityRef?.SupportActionBar?.CustomView != null)
            {
                AutoCompleteTextView actv = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                actv.Adapter = new ArrayAdapter<string>(context, Resource.Layout.search_dropdown_item, PreferencesState.SearchHistory);
            }
        }

        public override void OnPause()
        {
            Logger.Debug("SearchFragmentOnPause");
            base.OnPause();
            PreferencesManager.SaveSearchFragmentFilterState(PreferencesState.FilterSticky, SearchTabHelper.TextFilter.FilterString, (int)PreferencesState.SearchResultStyle, ExpandAllResults);
        }

        private static void Actv_KeyPressHELPER(object sender, View.KeyEventArgs e)
        {
            SearchFragment.Instance.Actv_KeyPress(sender, e);
        }

        public static void PerformSearchLogicFromSearchDialog(string searchTerm)
        {
            EditText editTextSearch = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            editTextSearch.Text = searchTerm;
            SearchFragment.Instance.PeformSearchLogic(null);
        }

        private void PeformSearchLogic(object sender)
        {
            var transitionDrawable = GetTransitionDrawable();
            if (SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
            {
                Logger.Debug("transitionDrawable: RESET transition");
                transitionDrawable.ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                SearchTabHelper.CancellationTokenSource.Cancel();
                SearchTabHelper.CurrentlySearching = false;
            }
            else
            {
                transitionDrawable.StartTransition(SearchToCloseDuration);
                PerformBackUpRefresh();
                Logger.Debug("START TRANSITION");
                SearchTabHelper.CurrentlySearching = true;
            }
            SearchTabHelper.CancellationTokenSource = new CancellationTokenSource();
            EditText editTextSearch = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            SearchAPI(SearchTabHelper.CancellationTokenSource.Token, transitionDrawable, editTextSearch.Text, SearchTabHelper.CurrentTab);
            if (sender != null)
            {
                (sender as AutoCompleteTextView).DismissDropDown();
            }
            Logger.Debug("Enter Pressed..");
        }

        private void Actv_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down)
            {
                Logger.Debug("ENTER PRESSED " + e.KeyCode.ToString());
                PeformSearchLogic(sender);
            }
            else if (e.KeyCode == Keycode.Del && e.Event.Action == KeyEventActions.Down)
            {
                (sender as AutoCompleteTextView).OnKeyDown(e.KeyCode, e.Event);
                return;
            }
            else if ((e.Event.Action == KeyEventActions.Down || e.Event.Action == KeyEventActions.Up) && (e.KeyCode == Android.Views.Keycode.Back || e.KeyCode == Android.Views.Keycode.VolumeUp || e.KeyCode == Android.Views.Keycode.VolumeDown))
            {
                //for some reason e.Handled is always true coming in.  also only on down volume press does anything.
                e.Handled = false;
            }
            else //this will only occur on unhandled keys which on the softkeyboard probably has to be in the above two categories....
            {
                if (e.Event.Action == KeyEventActions.Down)
                {
                    //Logger.Debug(e.KeyCode.ToString()); //happens on HW keyboard... event does NOT get called on SW keyboard. :)
                    //Logger.Debug((sender as AutoCompleteTextView).IsFocused.ToString());
                    (sender as AutoCompleteTextView).OnKeyDown(e.KeyCode, e.Event);
                }
            }
        }

        private static void AddIncomingSearchResponse(SearchResponse response, int fromTab, bool fromWishlist)
        {
            if ((response.FileCount == 0 && PreferencesState.HideLockedResultsInSearch) || (!PreferencesState.HideLockedResultsInSearch && response.FileCount == 0 && response.LockedFileCount == 0))
            {
                Logger.Debug("Skipping Locked or 0/0");
                return;
            }
            AddIncomingSearchResponseImp(response, fromTab, fromWishlist);

        }

        private static void clearListView(bool fromWishlist)
        {
            if (fromWishlist)
            {
                return; //we combine results...
            }


            Logger.Debug("clearListView SearchResponses.Clear()");
            SearchTabHelper.SortHelper.Clear();
            SearchTabHelper.SearchResponses.Clear();
            SearchTabHelper.LastSearchResponseCount = -1;
            SearchTabHelper.UI_SearchResponses.Clear();
            SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = null;
            SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipsFilter = null;
            if (!fromWishlist)
            {
                SearchFragment.Instance.ClearFilterStringAndCached();

                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses?.ToList();
                SearchFragment.Instance.recyclerViewTransferItems.SetAdapter(SearchFragment.Instance.CreateSearchConcatAdapter(SearchTabHelper.UI_SearchResponses));

                SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
                SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);

                SearchFragment.Instance.UpdateNoSearchesView();
            }
        }


        public override void OnDetach() //happens whenever the fragment gets recreated.  (i.e. on rotating device).
        {
            Logger.Debug("search frag detach");
            base.OnDetach();
        }
        public override void OnAttach(Context context)
        {
            Logger.Debug("search frag attach");
            base.OnAttach(context);
        }

        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        private SearchAdapterRecyclerVersion recyclerSearchAdapter;
        private SearchResultsHeaderAdapter recyclerSearchHeaderAdapter;

        private ConcatAdapter CreateSearchConcatAdapter(List<SearchResponse> responses)
        {
            recyclerSearchAdapter = new SearchAdapterRecyclerVersion(responses);
            recyclerSearchHeaderAdapter = new SearchResultsHeaderAdapter();
            return new ConcatAdapter(recyclerSearchHeaderAdapter, recyclerSearchAdapter);
        }

        private void NotifySearchHeaderChanged()
        {
            recyclerSearchHeaderAdapter?.NotifyItemChanged(0);
        }


        private static List<SearchResponse> GetOldList(string filter)
        {
            if (filter == oldListCondition)
            {
                return oldList;
            }
            return null;
        }

        private static void SetOldList(string filter, List<SearchResponse> searchResponses)
        {
            oldListCondition = !string.IsNullOrEmpty(filter) ? filter : null;
            oldList = searchResponses;
        }

        private static List<SearchResponse> oldList = new List<SearchResponse>();
        private static string oldListCondition = string.Empty;

        /// <summary>
        /// Applies new search results to the RecyclerView, creating a new adapter if needed
        /// or using DiffUtil for incremental updates.
        /// </summary>
        private static void ApplySearchResults(List<SearchResponse> newResults, string cacheKey)
        {
            var prevList = GetOldList(cacheKey);
            if (prevList == null)
            {
                Instance.recyclerViewTransferItems.SetAdapter(Instance.CreateSearchConcatAdapter(newResults));
            }
            else
            {
                // SaveInstanceState/RestoreInstanceState prevents autoscroll even when animations are off
                var state = Instance.recycleLayoutManager.OnSaveInstanceState();
#if DEBUG
                if (prevList.Count == 0)
                {
                    Logger.Debug("refreshListView  oldList: " + prevList.Count + " newList " + newResults.Count);
                }
#endif
                var diff = DiffUtil.CalculateDiff(new SearchDiffCallback(prevList, newResults), true);
                Instance.recyclerSearchAdapter.localDataSet = newResults;
                diff.DispatchUpdatesTo(Instance.recyclerSearchAdapter);
                Instance.recycleLayoutManager.OnRestoreInstanceState(state);
            }
            Instance.NotifySearchHeaderChanged();
            SetOldList(cacheKey, newResults.ToList());
            Instance.UpdateNoSearchesView();
        }

        /// <summary>
        /// To add a search response to the list view
        /// </summary>
        private static void AddIncomingSearchResponseImp(SearchResponse resp, int fromTab, bool fromWishlist)
        {
            var tab = SearchTabHelper.SearchTabCollection[fromTab];
            lock (tab.SortHelperLockObject)
            {
                Tuple<bool, List<SearchResponse>> splitResponses = new Tuple<bool, List<SearchResponse>>(false, null);
                try
                {
                    splitResponses = Common.SearchResponseUtil.SplitMultiDirResponse(PreferencesState.HideLockedResultsInSearch, resp);
                }
                catch (System.Exception e)
                {
                    Logger.Firebase(e.Message + " splitmultidirresponse");
                }

                try
                {
                    if (splitResponses.Item1)
                    {
                        foreach (SearchResponse splitResponse in splitResponses.Item2)
                        {
                            if (fromWishlist && WishlistController.OldResultsToCompare[fromTab].Contains(splitResponse))
                            {
                                continue;
                            }
                            tab.SortHelper.Add(splitResponse, null);
                        }
                    }
                    else
                    {
                        if (!fromWishlist || !WishlistController.OldResultsToCompare[fromTab].Contains(resp))
                        {
                            tab.SortHelper.Add(resp, null);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Logger.Debug(e.Message);
                }

                tab.SearchResponses = tab.SortHelper.Keys.ToList();
                tab.LastSearchResultsCount = tab.SearchResponses.Count;
            }

            if ((!fromWishlist || SearchFragment.Instance != null) && fromTab == SearchTabHelper.CurrentTab)
            {
                Action a = new Action(() =>
                {
#if DEBUG
                    Seeker.SearchFragment.StopWatch.Stop();
                    Seeker.SearchFragment.StopWatch.Reset();
                    Seeker.SearchFragment.StopWatch.Start();
#endif
                    if (fromTab != SearchTabHelper.CurrentTab)
                    {
                        return;
                    }
                    int total = tab.SearchResponses.Count;
                    if (tab.LastSearchResponseCount == total)
                    {
                        return;
                    }

                    if (tab.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive())
                    {
                        SearchFragment.Instance.UpdateFilteredResponses(tab);
                        ApplySearchResults(tab.UI_SearchResponses, tab.TextFilter.FilterString);
                    }
                    else
                    {
                        tab.UI_SearchResponses = tab.SearchResponses.ToList();
                        ApplySearchResults(tab.UI_SearchResponses, null);
                    }
                    tab.LastSearchResponseCount = total;
#if DEBUG
                    Seeker.SearchFragment.StopWatch.Stop();
                    Seeker.SearchFragment.StopWatch.Reset();
                    Seeker.SearchFragment.StopWatch.Start();
#endif
                });

                SeekerState.MainActivityRef?.RunOnUiThread(a);

            }

        }

        public static System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();

        private void Lv_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            showEditDialog(e.Position);
        }

        public static bool dlDialogShown = false;

        public void showEditDialog(int pos)
        {
            try
            {
                if (dlDialogShown)
                {
                    dlDialogShown = false; //just in the worst case we dont want to prevent too badly.
                    return;
                }
                dlDialogShown = true;
                SearchResponse dlDiagResp = null;
                if (SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive())
                {
                    dlDiagResp = SearchTabHelper.UI_SearchResponses.ElementAt<SearchResponse>(pos);
                }
                else
                {
                    dlDiagResp = SearchTabHelper.SearchResponses.ElementAt<SearchResponse>(pos);
                }
                DownloadDialog downloadDialog = DownloadDialog.CreateNewInstance(pos, dlDiagResp);
                downloadDialog.Show(FragmentManager, DownloadDialog.DOWNLOAD_DIALOG_FRAGMENT);
                // When creating a DialogFragment from within a Fragment, you must use the Fragment's CHILD FragmentManager to ensure that the state is properly restored after configuration changes. ????
            }
            catch (System.Exception e)
            {
                System.String msg = string.Empty;
                if (SearchTabHelper.TextFilter.IsFiltered || AreChipsFiltering() || AreFilterControlsActive())
                {
                    msg = "Filtered.Count " + SearchTabHelper.UI_SearchResponses.Count.ToString() + " position selected = " + pos.ToString();
                }
                else
                {
                    msg = "SearchResponses.Count = " + SearchTabHelper.SearchResponses.Count.ToString() + " position selected = " + pos.ToString();
                }

                Logger.Firebase(msg + " showEditDialog" + e.Message);
                SeekerApplication.Toaster.ShowToast("Error, please try again: " + msg, ToastLength.Long);
            }
        }

        private void PerformBackUpRefresh()
        {
            Handler h = new Handler(Looper.MainLooper);
            h.PostDelayed(new Action(() =>
            {
                var menuItem = ActionBarMenu?.FindItem(Resource.Id.action_search);
                if (menuItem != null)
                {
                    menuItem.SetVisible(false);
                    menuItem.SetVisible(true);
                    Logger.Debug("perform backup refresh");
                }

            }), 310);
        }

        public const int SearchToCloseDuration = 300;

        private static bool searchResponseFilter(SearchResponse s)
        {
            if (PreferencesState.FreeUploadSlotsOnly && !s.HasFreeUploadSlot)
            {
                return false;
            }
            return true;
        }

        private static void SearchLogic(CancellationToken cancellationToken, Android.Graphics.Drawables.TransitionDrawable transitionDrawable, string searchString, int fromTab, bool fromWishlist)
        {
            try
            {
                if (!fromWishlist)
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SearchFragment.Instance.context.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(SearchFragment.Instance.rootView.WindowToken, 0);
                }
            }
            catch
            {
                //not worth throwing over
            }
            try
            {
                //all click event handlers occur on UI thread.
                clearListView(fromWishlist);
                //editTextSearch = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            }
            catch (System.Exception e)
            {
            }
            //in my testing:
            // if someone has 0 free upload slots I could never download from them (even if queue was 68, 250) the progress bar just never moves, though no error, nothing.. ****
            // if someone has 1 free upload slot and a queue size of 100, 143, 28, 5, etc. it worked just fine.
            int searchTimeout = SearchTabHelper.SearchTarget == SearchTarget.AllUsers ? 5000 : 12000;

            Action<(Soulseek.Search, SearchResponse)> searchResponseReceived = new Action<(Soulseek.Search, SearchResponse)>(tuple =>
            {
                AddIncomingSearchResponse(tuple.Item2, fromTab, fromWishlist);
            });

            SearchOptions searchOptions = new SearchOptions(responseLimit: PreferencesState.NumberSearchResults, 
                searchTimeout: searchTimeout, 
                maximumPeerQueueLength: int.MaxValue, 
                responseReceived: searchResponseReceived, 
                responseFilter: (SearchResponse s) => searchResponseFilter(s), 
                filterResponses: true);
            SearchScope scope = null;
            if (fromWishlist)
            {
                scope = new SearchScope(SearchScopeType.Wishlist); //this is the same as passing no option for search scope 
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.AllUsers || SearchTabHelper.SearchTarget == SearchTarget.Wishlist) //this is like a manual wishlist search...
            {
                scope = new SearchScope(SearchScopeType.Network); //this is the same as passing no option for search scope
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.UserList)
            {
                if (CommonState.UserList == null || CommonState.UserList.Count == 0)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.user_list_empty), ToastLength.Short);
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, CommonState.UserList.Select(item => item.Username).ToArray());
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.ChosenUser)
            {
                if (SearchTabHelper.SearchTargetChosenUser == string.Empty)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.no_user), ToastLength.Short);
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, new string[] { SearchTabHelper.SearchTargetChosenUser });
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.Room)
            {
                if (SearchTabHelper.SearchTargetChosenRoom == string.Empty)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.no_room), ToastLength.Short);
                    return;
                }
                scope = new SearchScope(SearchScopeType.Room, new string[] { SearchTabHelper.SearchTargetChosenRoom });
            }
            try
            {
                Task<(Soulseek.Search, IReadOnlyCollection<SearchResponse>)> t = null;
                if (fromTab == SearchTabHelper.CurrentTab)
                {
                    //there was a bug where wishlist search would clear this in the middle of diffutil calculating causing out of index crash.
                    oldList?.Clear();
                }
                t = SeekerState.SoulseekClient.SearchAsync(SearchQuery.FromText(searchString), options: searchOptions, scope: scope, cancellationToken: cancellationToken);
                //t = TestClient.SearchAsync(searchString, searchResponseReceived, cancellationToken);
                //drawable.StartTransition() - since if we get here, the search is launched and the continue with will always happen...

                t.ContinueWith(new Action<Task<(Soulseek.Search, IReadOnlyCollection<SearchResponse>)>>(t =>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;

                    if (!t.IsCompletedSuccessfully && t.Exception != null)
                    {
                        Logger.Debug("search exception: " + t.Exception.Message);
                    }

                    if (t.IsCanceled)
                    {
                    }
                    else
                    {

                        SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                        {
                            try
                            {
                                if (fromTab == SearchTabHelper.CurrentTab && !fromWishlist)
                                {
                                    Logger.Debug("transitionDrawable: ReverseTransition transition");
                                    //this can be stale, not part of anything anymore....
                                    //no real way to test that.  IsVisible returns true...
                                    try
                                    {
                                        GetSearchFragment().GetTransitionDrawable().ReverseTransition(SearchToCloseDuration);
                                    }
                                    catch
                                    {

                                    }
                                    SearchFragment.Instance.PerformBackUpRefresh();


                                }
                            }
                            catch (System.ObjectDisposedException e)
                            {
                                //since its disposed when you go back to the screen it will be the correct search icon again..
                                //noop
                            }
                        }));

                    }
                    if ((!t.IsCanceled) && t.Result.Item2.Count == 0 && !fromWishlist) //if t is cancelled, t.Result throws..
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.no_search_results), ToastLength.Short);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResultsCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                    if (fromWishlist)
                    {
                        WishlistController.SearchCompleted(fromTab);
                    }
                    else if (SearchTabHelper.SearchTabCollection[fromTab].SearchTarget == SearchTarget.Wishlist)
                    {
                        //this is if the search was not automatic (i.e. wishlist timer elapsed) but was performed in the wishlist tab..
                        //therefore save the new results...
                        SearchTabHelper.SaveHeadersToSharedPrefs();
                        SearchTabHelper.SaveSearchResultsToDisk(fromTab, SeekerState.ActiveActivityRef);
                    }

                    if (fromTab == SearchTabHelper.CurrentTab)
                    {
                        if (PreferencesState.ShowSmartFilters)
                        {
#if DEBUG
                            try
                            {
                                var df = SeekerState.RootDocumentFile.CreateFile("text/plain", SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm.Replace(' ', '_'));
                                var outputStream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(df.Uri);
                                foreach (var sr in SearchTabHelper.SearchTabCollection[fromTab].SearchResponses)
                                {
                                    byte[] bytesText = System.Text.Encoding.ASCII.GetBytes(sr.Files.First().Filename + System.Environment.NewLine);
                                    outputStream.Write(bytesText, 0, bytesText.Length);
                                }
                                outputStream.Close();
                            }
                            catch
                            {

                            }

#endif
                            List<ChipDataItem> chipDataItems = ChipsHelper.GetChipDataItemsFromSearchResults(SearchTabHelper.SearchTabCollection[fromTab].SearchResponses, SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm, PreferencesState.SmartFilterOptions);
                            SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = chipDataItems;
                            SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                            {
                                SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                                SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                            }));
                        }

                    }

                }));



                if (SearchTabHelper.TextFilter.IsFiltered && PreferencesState.FilterSticky && !fromWishlist)
                {
                    //remind the user that the filter is ON.
                    t.ContinueWith(new Action<Task>(
                        (Task t) =>
                        {
                            SeekerState.MainActivityRef.RunOnUiThread(new Action(() =>
                            {

                                View bottomSheetView = SearchFragment.Instance.rootView.FindViewById<View>(Resource.Id.bottomSheet);
                                BottomSheetBehavior bsb = BottomSheetBehavior.From(bottomSheetView);
                                if (bsb.State == BottomSheetBehavior.StateHidden)
                                {

                                    View BSButton = SearchFragment.Instance.rootView.FindViewById<View>(Resource.Id.bsbutton);
                                    ObjectAnimator objectAnimator = new ObjectAnimator();
                                    ObjectAnimator anim1 = ObjectAnimator.OfFloat(BSButton, "scaleX", 2.0f);
                                    anim1.SetInterpolator(new Android.Views.Animations.LinearInterpolator());
                                    anim1.SetDuration(200);
                                    ObjectAnimator anim2 = ObjectAnimator.OfFloat(BSButton, "scaleY", 2.0f);
                                    anim2.SetInterpolator(new Android.Views.Animations.LinearInterpolator());
                                    anim2.SetDuration(200);

                                    ObjectAnimator anim3 = ObjectAnimator.OfFloat(BSButton, "scaleX", 1.0f);
                                    anim3.SetInterpolator(new Android.Views.Animations.BounceInterpolator());
                                    anim3.SetDuration(1000);
                                    ObjectAnimator anim4 = ObjectAnimator.OfFloat(BSButton, "scaleY", 1.0f);
                                    anim4.SetInterpolator(new Android.Views.Animations.BounceInterpolator());
                                    anim4.SetDuration(1000);

                                    AnimatorSet set1 = new AnimatorSet();
                                    set1.PlayTogether(anim1, anim2);
                                    //set1.Start();

                                    AnimatorSet set2 = new AnimatorSet();
                                    set2.PlayTogether(anim3, anim4);
                                    //set2.Start();

                                    AnimatorSet setTotal = new AnimatorSet();
                                    setTotal.PlaySequentially(set1, set2);
                                    setTotal.Start();

                                }

                            }));
                        }
                        ));
                }
            }
            catch (ArgumentNullException ane)
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    string errorMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.no_search_text);
                    if (fromWishlist)
                    {
                        errorMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.no_wish_text);
                    }

                    SeekerApplication.Toaster.ShowToast(errorMsg, ToastLength.Short);
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    Logger.Debug("transitionDrawable: RESET transition");
                    if (!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
                    {
                        transitionDrawable.ResetTransition();
                    }
                }
                ));
                //MainActivity.ShowAlert(ane, this.Context);
                return;
            }
            catch (ArgumentException ae)
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    string errorMsg = SeekerState.MainActivityRef.GetString(Resource.String.no_search_text);
                    if (fromWishlist)
                    {
                        errorMsg = SeekerState.ActiveActivityRef.GetString(Resource.String.no_wish_text);
                    }
                    Logger.Debug("transitionDrawable: RESET transition");
                    SeekerApplication.Toaster.ShowToast(errorMsg, ToastLength.Short);
                    if (!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
                    {
                        transitionDrawable.ResetTransition();
                    }
                }));
                return;
            }
            catch (System.Exception ue)
            {

                SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    Logger.Debug("transitionDrawable: RESET transition");

                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.search_error_unspecified), ToastLength.Short);
                    Logger.Firebase("tabpageradapter searchclick: " + ue.Message);

                    if (!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
                    {
                        transitionDrawable.ResetTransition();
                    }
                }));
                return;
            }
            if (!fromWishlist)
            {
                //add a new item to our search history
                if (PreferencesState.RememberSearchHistory)
                {
                    if (!PreferencesState.SearchHistory.Contains(searchString))
                    {
                        PreferencesState.SearchHistory.Add(searchString);
                        PreferencesManager.SaveSearchHistory();
                    }
                }
                var actv = SeekerState.MainActivityRef.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere); // lot of nullrefs with actv before this change....
                if (actv == null)
                {
                    actv = (SearchFragment.Instance.Activity as AndroidX.AppCompat.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                    if (actv == null)
                    {
                        Logger.Firebase("actv stull null, cannot refresh adapter");
                        return;
                    }
                }
                actv.Adapter = new ArrayAdapter<string>(SearchFragment.Instance.context, Resource.Layout.search_dropdown_item, PreferencesState.SearchHistory); //refresh adapter
            }
        }

        public static void SearchAPI(CancellationToken cancellationToken, Android.Graphics.Drawables.TransitionDrawable transitionDrawable, string searchString, int fromTab, bool fromWishlist = false)
        {
            SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm = searchString;
            SearchTabHelper.SearchTabCollection[fromTab].LastRanTime = SimpleHelpers.GetDateTimeNowSafe();
            if (!fromWishlist)
            {
                //try to clearFocus on the search if you can (gets rid of blinking cursor)
                ClearFocusSearchEditText();
                Logger.Debug("Search_Click");
            }
            //#if !DEBUG
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                if (!fromWishlist)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_search), ToastLength.Long);
                    Logger.Debug("transitionDrawable: RESET transition");
                    transitionDrawable.ResetTransition();

                }

                SearchTabHelper.CurrentlySearching = false;
                return;
            }
            else
            {
                SessionService.Instance.RunWithReconnect(() => SearchLogic(cancellationToken, transitionDrawable, searchString, fromTab, fromWishlist), silent: fromWishlist);
            }
        }
    }

}