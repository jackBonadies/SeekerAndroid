using Seeker.Chatroom;
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

namespace Seeker
{
    public class SearchFragment : Fragment
    {
        public override void OnStart()
        {
            //this fixes the same bug as the MainActivity OnStart fixes.
            SearchFragment.Instance = this;
            MainActivity.LogDebug("SearchFragmentOnStart");
            base.OnStart();
        }
        public override void OnResume()
        {
            base.OnResume();
            MainActivity.LogDebug("Search Fragment On Resume");
            //you had a pending intent that could not get handled til now.
            if (MainActivity.goToSearchTab != int.MaxValue)
            {
                MainActivity.LogDebug("Search Fragment On Resume for wishlist");
                this.GoToTab(MainActivity.goToSearchTab, false, true);
                MainActivity.goToSearchTab = int.MaxValue;
            }
        }
        public View rootView = null;

        public static bool FilterSticky = false;
        public static string FilterStickyString = string.Empty; //if FilterSticky is on then always use this string..

        private Context context;
        public static List<string> searchHistory = new List<string>();


        public static SearchResultStyleEnum SearchResultStyle = SearchResultStyleEnum.Medium;

        public static IMenu ActionBarMenu = null;
        public static int LastSearchResponseCount = -1;

        private void ClearFilterStringAndCached(bool force = false)
        {
            if (!FilterSticky || force)
            {
                SearchTabHelper.FilterString = string.Empty;
                SearchTabHelper.FilteredResults = this.AreChipsFiltering();
                SearchTabHelper.WordsToAvoid.Clear();
                SearchTabHelper.WordsToInclude.Clear();
                SearchTabHelper.FilterSpecialFlags.Clear();
                EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
                if (filterText.Text != string.Empty)
                {
                    //else you trigger the event.
                    filterText.Text = string.Empty;
                    UpdateDrawableState(filterText, true);
                }
                FilterStickyString = string.Empty;
            }
        }

        public static void SetSearchResultStyle(int style)
        {
            //in case its out of range bc we add / rm enums in the future...
            foreach (int i in System.Enum.GetValues(typeof(SearchResultStyleEnum)))
            {
                if (i == style)
                {
                    SearchResultStyle = (SearchResultStyleEnum)(i);
                    break;
                }
            }
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
            //ActionBar actionBar = getActionBar();
            //// add the custom view to the action bar
            //actionBar.setCustomView(R.layout.actionbar_view);
            //SearchView searchView = (SearchView)(menu.FindItem(Resource.Id.action_search).ActionView);
            //menu.FindItem(Resource.Id.action_search).ExpandActionView();
            base.OnCreateOptionsMenu(menu, inflater);

            //IMenuItem searchItem = menu.FindItem(Resource.Id.action_search);


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
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                drawable = c.Resources.GetDrawable(idOfDrawable, c.Theme);
            }
            else
            {
                AndroidX.AppCompat.App.AppCompatDelegate.CompatVectorFromResourcesEnabled = true;
                //the above is needed else it fails **Java.Lang.RuntimeException:** 'File res/drawable/numeric_1_box_multiple_outline.xml from drawable resource ID #0x7f0700d3'
                drawable = c.Resources.GetDrawable(idOfDrawable);
            }
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
            if (FilterSticky)
            {
                filter.Text = FilterStickyString;
            }
            else
            {
                filter.Text = SearchTabHelper.FilterString;
            }
            UpdateDrawableState(filter, true);
        }

        private void SetTransitionDrawableState()
        {
            if (SearchTabHelper.CurrentlySearching)
            {
                MainActivity.LogDebug("CURRENT SEARCHING SET TRANSITION DRAWABLE");
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
                        Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.search_tab_error, ToastLength.Long).Show();
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
                    //bool isVisible = GetTransitionDrawable().IsVisible;
                    //GetTransitionDrawable().InvalidateSelf();
                    //(this.Activity as AndroidX.AppCompat.App.AppCompatActivity).FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).RefreshDrawableState();
                    //(this.Activity as AndroidX.AppCompat.App.AppCompatActivity).FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).PostInvalidateOnAnimation();
                    //(this.Activity as AndroidX.AppCompat.App.AppCompatActivity).FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).PostInvalidate();
                    //Handler handler = new Handler(Looper.MainLooper);
                    //handler.PostDelayed(new Action(()=> {
                    //    (this.Activity as AndroidX.AppCompat.App.AppCompatActivity).FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).PostInvalidate();
                    //    GetTransitionDrawable().InvalidateSelf();
                    //    ActionBarMenu?.FindItem(Resource.Id.action_search).SetVisible(false);
                    //    ActionBarMenu?.FindItem(Resource.Id.action_search).SetVisible(true);
                    //}), 100);

                    SetFilterState();



                    if (SearchTabHelper.SearchTabCollection[fromTab].FilteredResults)
                    {
                        if (SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount != SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count)
                        {
                            MainActivity.LogDebug("filtering...");
                            UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);  //WE JUST NEED TO FILTER THE NEW RESPONSES!!
                        }
                        SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
                        //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses); //this throws, its not ready..
                        //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        //lv.Adapter = (customAdapter);

                        recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                        recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);

                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);

                    }
                    else
                    {
                        //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.SearchTabCollection[fromTab].SearchResponses);
                        //MainActivity.LogDebug("new tab refresh " + tabToGoTo + " count " + SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count);
                        //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        //lv.Adapter = (customAdapter);
                        SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.ToList();
                        recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                        recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);

                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                    if (!fromIntent)
                    {
                        GetTransitionDrawable().InvalidateSelf();
                    }
                    this.SetCustomViewTabNumberImageViewState();
                    if (this.Activity == null)
                    {
                        GetSearchFragmentMoreDiag();
                    }
                    this.Activity.InvalidateOptionsMenu(); //this wil be the new nullref if fragment isnt ready...

                    if (!fromIntent)
                    {
                        SetTransitionDrawableState();
                    }
                });
                if (SeekerState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("mainActivityRef is null GoToTab");
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
                        Toast.MakeText(this.Context, Resource.String.wishlist_tab_target, ToastLength.Long).Show();
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
                        MainActivity.LogDebug("transitionDrawable: REVERSE transition");
                        (item.Icon as Android.Graphics.Drawables.TransitionDrawable).ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                        SearchTabHelper.CancellationTokenSource.Cancel();
                        SearchTabHelper.CurrentlySearching = false;
                        return true;
                    }
                    else
                    {
                        (item.Icon as Android.Graphics.Drawables.TransitionDrawable).StartTransition(SearchToCloseDuration);
                        PerformBackUpRefresh();
                        MainActivity.LogDebug("START TRANSITION");
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
                case Resource.Id.action_add_to_wishlist:
                    AddSearchToWishlist();
                    return true;
                    //case Resource.Id.action_view_search_tabs:
                    //    ShowSearchTabsDialog();
                    //    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void AddSearchToWishlist()
        {
            //here we "fork" the current search, adding it to the wishlist
            if (SearchTabHelper.LastSearchTerm == string.Empty || SearchTabHelper.LastSearchTerm == null)
            {
                Toast.MakeText(this.Context, Resource.String.perform_search_first, ToastLength.Long).Show();
                return;
            }
            SearchTabHelper.AddWishlistSearchTabFromCurrent();
            Toast.MakeText(this.Context, string.Format(this.Context.GetString(Resource.String.added_to_wishlist), SearchTabHelper.LastSearchTerm), ToastLength.Long).Show();
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

        public static SearchFragment GetSearchFragmentMoreDiag()
        {
            if (SeekerState.ActiveActivityRef is MainActivity)
            {
                MainActivity.LogInfoFirebase("current activity is Main");
            }
            else
            {
                MainActivity.LogInfoFirebase("current activity is NOT Main");
            }
            foreach (Fragment frag in SeekerState.MainActivityRef.SupportFragmentManager.Fragments)
            {
                if (frag is SearchFragment sfrag)
                {
                    MainActivity.LogInfoFirebase("yes search fragment,  isAdded: " + sfrag.IsAdded);
                    return sfrag;
                }
            }
            MainActivity.LogInfoFirebase("no search fragment.");
            return null;
        }

        public void ShowSearchTabsDialog()
        {
            SearchTabDialog searchTabDialog = new SearchTabDialog();
            //bool isAdded = (((SeekerState.MainActivityRef.FindViewById(Resource.Id.pager) as AndroidX.ViewPager.Widget.ViewPager).Adapter as TabsPagerAdapter).GetItem(1) as SearchFragment).IsAdded; //this is EXTREMELY stale
            if (!this.IsAdded || this.Activity == null) //then child fragment manager will likely be null
            {
                MainActivity.LogInfoFirebase("ShowSearchTabsDialog, fragment no longer attached...");

                //foreach(Fragment frag in SeekerState.MainActivityRef.SupportFragmentManager.Fragments)
                //{
                //    if(frag is SearchFragment sfrag)
                //    {
                //        bool isAdded = sfrag.IsAdded;
                //    }
                //}

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
                    cancel.SetColorFilter(SearchItemViewExpandable.GetColorFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.mainTextColor), PorterDuff.Mode.SrcAtop);
                }
                actv.SetCompoundDrawables(null, null, cancel, null);
            }
        }


        public static void ConfigureSupportCustomView(View customView/*, Context contextJustInCase*/) //todo: seems to be an error. which seems entirely possible. where ActiveActivityRef does not get set yet.
        {
            MainActivity.LogDebug("ConfigureSupportCustomView");
            AutoCompleteTextView actv = customView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
            try
            {
                actv.Text = SearchingText; //this works with string.Empty and emojis so I dont think its here...
                UpdateDrawableState(actv);
                actv.Touch += Actv_Touch;
                //ContextCompat.GetDrawable(SeekerState.MainActivityRef,Resource.Drawable.ic_cancel_black_24dp);
            }
            catch (System.ArgumentException e)
            {
                MainActivity.LogFirebase("ArugmentException Value does not fall within range: " + SearchingText + " " + e.Message);
            }
            catch (System.Exception e)
            {
                MainActivity.LogFirebase("catchException Value does not fall within range: " + SearchingText + " " + e.Message);
            }
            catch
            {
                MainActivity.LogFirebase("catchunspecException Value does not fall within range: " + SearchingText);
            }
            ImageView iv = customView.FindViewById<ImageView>(Resource.Id.search_tabs);
            iv.Click += Iv_Click;
            actv.EditorAction -= Search_EditorActionHELPER;
            actv.EditorAction += Search_EditorActionHELPER;
            string searchHistoryXML = SeekerState.SharedPreferences.GetString(KeyConsts.M_SearchHistory, string.Empty);
            if (searchHistory == null || searchHistory.Count == 0) // i think we just have to deserialize once??
            {
                if (searchHistoryXML == string.Empty)
                {
                    searchHistory = new List<string>();
                }
                else
                {
                    using (var stream = new System.IO.StringReader(searchHistoryXML))
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(searchHistory.GetType()); //this happens too often not allowing new things to be properly stored..
                        searchHistory = serializer.Deserialize(stream) as List<string>;
                    }
                    //noTransfers.Visibility = ViewStates.Gone;
                }

            }

            SetSearchHintTarget(SearchTabHelper.SearchTarget, actv);

            Context contextToUse = SeekerState.ActiveActivityRef;
            //if (SeekerState.ActiveActivityRef==null)
            //{
            //    MainActivity.LogFirebase("Active ActivityRef is null!!!");
            //    //contextToUse = contextJustInCase;
            //}
            //else
            //{
            //    contextToUse = SeekerState.ActiveActivityRef;
            //}

            actv.Adapter = new ArrayAdapter<string>(contextToUse, Resource.Layout.autoSuggestionRow, searchHistory);
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
                    //e.Handled = true;
                    editText.Text = string.Empty;
                    UpdateDrawableState(editText);
                    //editText.RequestFocus();
                }
            }
        }

        private static void Actv_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            throw new NotImplementedException();
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
                MainActivity.LogFirebase("MainActivity_FocusChange" + err.Message);
            }
        }

        public void SearchResultStyleChanged()
        {
            //notify changed isnt enough if the xml is different... it is enough in the case of expandAll to collapseAll tho..

            //(rootView.FindViewById<ListView>(Resource.Id.listView1).Adapter as SearchAdapter).NotifyDataSetChanged();
            RecyclerView rv = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewSearches); //TODO //TODO //TODO

            if (SearchTabHelper.FilteredResults)
            {
                SearchAdapterRecyclerVersion customAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                rv.SetAdapter(customAdapter);
            }
            else
            {
                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses.ToList();
                SearchAdapterRecyclerVersion customAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                rv.SetAdapter(customAdapter);
            }

            //(rootView.FindViewById<ListView>(Resource.Id.listView1).Adapter as SearchAdapter)
        }

        public class BSDF_Menu : BottomSheetDialogFragment
        {
            //public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
            //{
            //    inflater.Inflate(Resource.Menu.transfers_menu, menu);
            //    base.OnCreateOptionsMenu(menu, inflater);
            //}

            //public override int Theme => Resource.Style.BottomSheetDialogTheme;

            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {

                //return base.OnCreateView(inflater, container, savedInstanceState);
                View rootView = inflater.Inflate(Resource.Layout.search_results_expandablexml, container);
                RadioGroup resultStyleRadioGroup = rootView.FindViewById<RadioGroup>(Resource.Id.radioGroup);



                switch (SearchFragment.SearchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandedAll:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonExpanded);
                        break;
                    case SearchResultStyleEnum.CollapsedAll:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonCollapsed);
                        break;
                    case SearchResultStyleEnum.Medium:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMedium);
                        break;
                    case SearchResultStyleEnum.Minimal:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMinimal);
                        break;
                }
                resultStyleRadioGroup.CheckedChange += ResultStyleRadioGroup_CheckedChange;
                return rootView;
            }

            private void ResultStyleRadioGroup_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
            {
                //RadioButton checkedRadioButton = (RadioButton)(sender as View).FindViewById(e.CheckedId);
                var prev = SearchFragment.SearchResultStyle;
                switch (e.CheckedId)
                {
                    case Resource.Id.radioButtonExpanded:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.ExpandedAll;
                        break;
                    case Resource.Id.radioButtonCollapsed:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.CollapsedAll;
                        break;
                    case Resource.Id.radioButtonMedium:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.Medium;
                        break;
                    case Resource.Id.radioButtonMinimal:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.Minimal;
                        break;
                }
                if (prev != SearchFragment.SearchResultStyle)
                {
                    SearchFragment.Instance.SearchResultStyleChanged();
                }
                this.Dismiss();
            }

            //public override int Theme => Resource.Style.MyCustomTheme; //for rounded corners...
        }


        private static void Iv_Click(object sender, EventArgs e)
        {
            //SearchFragment.Instance = GetSearchFragment(); //tested this and it works!
            if (!SearchFragment.Instance.IsAdded)
            {
                SearchFragment f = GetSearchFragment(); //is there an attached fragment?? i.e. is our instance just a stale one..
                if (f == null)
                {
                    MainActivity.LogInfoFirebase("search fragment not on activities fragment manager");
                }
                else if (!f.IsAdded)
                {
                    MainActivity.LogInfoFirebase("search fragment from activities fragment manager is not added");
                }
                else
                {
                    MainActivity.LogInfoFirebase("search fragment from activities fragment manager is good, though not setting it");
                    //SearchFragment.Instance = f; 
                }
                MainActivity.LogFirebase("SearchFragment.Instance.IsAdded == false, currently searching: " + SearchTabHelper.CurrentlySearching);
            }
            //try
            //{
            SearchFragment.Instance.ShowSearchTabsDialog();
            //}
            //catch(Java.Lang.Exception ex)
            //{
            //    string currentMainState = SeekerState.MainActivityRef.Lifecycle.CurrentState.ToString();
            //    string currentFragState = SearchFragment.Instance.Lifecycle.CurrentState.ToString();
            //    string diagMessage = string.Format("Last Stopped: {0} Last Started: {1} currentMainState: {2} currentFragState: {3}", ForegroundLifecycleTracker.DiagLastStopped,ForegroundLifecycleTracker.DiagLastStarted, currentMainState, currentFragState);
            //    System.Exception diagException = new System.Exception(diagMessage);
            //    throw diagException;
            //}
            //catch(System.Exception ex)
            //{
            //    string currentMainState = SeekerState.MainActivityRef.Lifecycle.CurrentState.ToString();
            //    string currentFragState = SearchFragment.Instance.Lifecycle.CurrentState.ToString();
            //    string diagMessage = string.Format("Last Stopped: {0} Last Started: {1} currentMainState: {2} currentFragState: {3}", ForegroundLifecycleTracker.DiagLastStopped, ForegroundLifecycleTracker.DiagLastStarted, currentMainState, currentFragState);
            //    System.Exception diagException = new System.Exception(diagMessage, ex);
            //    throw diagException;
            //}
        }

        public static void ShowChangeResultStyleBottomDialog()
        {
            BSDF_Menu bsdf = new BSDF_Menu();
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
            //SeekerState.MainActivityRef.SupportActionBar.SetDisplayShowCustomEnabled(true);
            //SeekerState.MainActivityRef.SupportActionBar.SetCustomView(Resource.Layout.custom_menu_layout);//FindViewById< AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar).(Resource.Layout.custom_menu_layout);
            MainActivity.LogDebug("SearchFragmentOnCreateView");
            MainActivity.LogDebug("SearchFragmentOnCreateView - SearchResponses.Count=" + SearchTabHelper.SearchResponses.Count);
            this.rootView = inflater.Inflate(Resource.Layout.searches, container, false);
            UpdateForScreenSize();

            //Button search = rootView.FindViewById<Button>(Resource.Id.button2);
            //search.Click -= Search_Click;
            //search.Click += Search_Click;

            context = this.Context;

            //note: changing from AutoCompleteTextView to EditText fixes both the hardware keyboard issue, and the backspace issue.

            //this.listView = rootView.FindViewById<ListView>(Resource.Id.listView1);

            recyclerViewChips = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewChips);
            //if(SeekerState.ShowSmartFilters)
            //{
            recyclerViewChips.Visibility = ViewStates.Visible;
            //}
            //else
            //{
            //    recyclerViewChips.Visibility = ViewStates.Gone;
            //}

            var manager = new LinearLayoutManager(this.Context, LinearLayoutManager.Horizontal, false);
            recyclerViewChips.SetItemAnimator(null);
            recyclerViewChips.SetLayoutManager(manager);
            recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
            recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);

            RelativeLayout rel = rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
            bsb.Hideable = true;
            bsb.PeekHeight = 320;
            bsb.State = BottomSheetBehavior.StateHidden;

            CheckBox filterSticky = rootView.FindViewById<CheckBox>(Resource.Id.stickyFilterCheckbox);
            filterSticky.Checked = FilterSticky;
            filterSticky.CheckedChange += FilterSticky_CheckedChange;

            //bsb.SetBottomSheetCallback(new MyCallback());
            View b = rootView.FindViewById<View>(Resource.Id.bsbutton);
            (b as FloatingActionButton).SetImageResource(Resource.Drawable.ic_filter_list_white_24dp);
            View v = rootView.FindViewById<View>(Resource.Id.focusableLayout);
            v.Focusable = true;
            //SetFocusable(int) was added in API26. bool was there since API1
            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
            {
                v.SetFocusable(ViewFocusability.Focusable);
            }
            else
            {
                //v.SetFocusable(true); no bool method in xamarin...
            }

            v.FocusableInTouchMode = true;
            //b.Focusable = true;
            //b.SetFocusable(ViewFocusability.Focusable);
            //b.FocusableInTouchMode = true;
            b.Click += B_Click;

            //Button clearFilter = rootView.FindViewById<Button>(Resource.Id.clearFilter);
            //clearFilter.Click += ClearFilter_Click;

            string searchHistoryXML = SeekerState.SharedPreferences.GetString(KeyConsts.M_SearchHistory, string.Empty);
            if (searchHistory == null || searchHistory.Count == 0) // i think we just have to deserialize once??
            {
                if (searchHistoryXML == string.Empty)
                {
                    searchHistory = new List<string>();
                }
                else
                {
                    using (var stream = new System.IO.StringReader(searchHistoryXML))
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(searchHistory.GetType()); //this happens too often not allowing new things to be properly stored..
                        searchHistory = serializer.Deserialize(stream) as List<string>;
                    }
                    //noTransfers.Visibility = ViewStates.Gone;
                }
            }

            //actv.Adapter = new ArrayAdapter<string>(context, Resource.Layout.autoSuggestionRow, searchHistory);
            //actv.KeyPress -= Actv_KeyPress;
            //actv.KeyPress += Actv_KeyPress;

            //List<SearchResponse> rowItems = new List<SearchResponse>();
            //if (SearchTabHelper.FilteredResults)
            //{
            //    SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.FilteredResponses);
            //    listView.Adapter = (customAdapter);
            //}
            //else
            //{
            //    SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.SearchResponses);
            //    listView.Adapter = (customAdapter);
            //}


            recyclerViewTransferItems = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewSearches);
            recycleLayoutManager = new LinearLayoutManager(Activity);
            recyclerViewTransferItems.SetItemAnimator(null); //todo
            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);
            if (SearchTabHelper.FilteredResults)
            {
                recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
                //CustomAdapter customAdapter = new CustomAdapter(Context, FilteredResponses);
                //lv.Adapter = (customAdapter);
            }
            else
            {
                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses.ToList();
                recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
                //CustomAdapter customAdapter = new CustomAdapter(Context, SearchResponses);
                //lv.Adapter = (customAdapter);
            }


            //listView.ItemClick -= Lv_ItemClick;
            //listView.ItemClick += Lv_ItemClick;
            //listView.Clickable = true;
            //listView.Focusable = true;
            SeekerState.ClearSearchHistoryEventsFromTarget(this);
            SeekerState.ClearSearchHistory += SeekerState_ClearSearchHistory;
            SeekerState.SoulseekClient.ClearSearchResponseReceivedFromTarget(this);
            //SeekerState.SoulseekClient.SearchResponseReceived -= SoulseekClient_SearchResponseReceived;
            int x = SeekerState.SoulseekClient.GetInvocationListOfSearchResponseReceived();
            MainActivity.LogDebug("NUMBER OF DELEGATES AFTER WE REMOVED OURSELF: (before doing the deep clear this would increase every rotation orientation)" + x);
            //SeekerState.SoulseekClient.SearchResponseReceived += SoulseekClient_SearchResponseReceived;
            MainActivity.LogDebug("SearchFragmentOnCreateViewEnd - SearchResponses.Count=" + SearchTabHelper.SearchResponses.Count);

            EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
            filterText.TextChanged += FilterText_TextChanged;
            filterText.FocusChange += FilterText_FocusChange;
            filterText.EditorAction += FilterText_EditorAction;
            filterText.Touch += FilterText_Touch;
            if (FilterSticky)
            {
                filterText.Text = FilterStickyString;
            }
            UpdateDrawableState(filterText, true);

            Button showHideSmartFilters = rootView.FindViewById<Button>(Resource.Id.toggleSmartFilters);
            showHideSmartFilters.Text = SeekerState.ShowSmartFilters ? this.GetString(Resource.String.HideSmartFilters) : this.GetString(Resource.String.ShowSmartFilters);
            showHideSmartFilters.Click += ShowHideSmartFilters_Click;

            return rootView;
        }

        private void ShowHideSmartFilters_Click(object sender, EventArgs e)
        {
            SeekerState.ShowSmartFilters = !SeekerState.ShowSmartFilters;
            Button showHideSmartFilters = rootView.FindViewById<Button>(Resource.Id.toggleSmartFilters);
            showHideSmartFilters.Text = SeekerState.ShowSmartFilters ? this.GetString(Resource.String.HideSmartFilters) : this.GetString(Resource.String.ShowSmartFilters);
            if (SeekerState.ShowSmartFilters)
            {
                if (SearchTabHelper.CurrentlySearching)
                {
                    return; //it will update on complete search
                }
                if ((SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].SearchResponses?.Count ?? 0) != 0)
                {
                    List<ChipDataItem> chipDataItems = ChipsHelper.GetChipDataItemsFromSearchResults(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].SearchResponses, SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].LastSearchTerm, SeekerState.SmartFilterOptions);
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
                    //e.Handled = true;
                    editText.Text = string.Empty;
                    UpdateDrawableState(editText, true);

                    ClearFilterStringAndCached(true);
                    //editText.RequestFocus();
                }
            }
        }

        /// <summary>
        /// Are chips filtering out results..
        /// </summary>
        /// <returns></returns>
        private bool AreChipsFiltering()
        {
            if (!SeekerState.ShowSmartFilters || (SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems?.Count ?? 0) == 0)
            {
                return false;
            }
            else
            {
                return SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems.Any(i => i.IsChecked);
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
                MainActivity.LogFirebase("MainActivity_FocusChange" + err.Message);
            }
        }

        //private void ClearFilter_Click(object sender, EventArgs e)
        //{
        //    CheckBox filterSticky = rootView.FindViewById<CheckBox>(Resource.Id.stickyFilterCheckbox);
        //    filterSticky.Checked = false;
        //    ClearFilterStringAndCached(true);
        //}

        private void B_Click(object sender, EventArgs e)
        {
            RelativeLayout rel = rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
            if (bsb.State != BottomSheetBehavior.StateExpanded && bsb.State != BottomSheetBehavior.StateCollapsed)
            {
                bsb.State = BottomSheetBehavior.StateExpanded;
            }
            else
            {
                //if the keyboard is up and the edittext is in focus then maybe just put the keyboard down
                //else put the bottom sheet down.  
                //so make it two tiered.
                //or maybe just unset the focus...
                EditText test = rootView.FindViewById<EditText>(Resource.Id.filterText);
                //Android.Views.InputMethods.InputMethodManager IMM = context.GetSystemService(Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;
                //Rect outRect = new Rect();
                //this.rootView.GetWindowVisibleDisplayFrame(outRect);
                //MainActivity.LogDebug("Window Visible Display Frame " + outRect.Height());
                //MainActivity.LogDebug("Actual Height " + this.rootView.Height);
                //Type immType = IMM.GetType();

                //MainActivity.LogDebug("Y Position " + rel.GetY());
                //int[] location = new int[2];
                //rel.GetLocationOnScreen(location);
                //MainActivity.LogDebug("X Pos: " + location[0] + "  Y Pos: " + location[1]);
                //var method = immType.GetProperty("InputMethodWindowVisibleHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                //foreach (var prop in immType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                //{
                //    MainActivity.LogDebug(string.Format("Property Name: {0}", prop.Name));
                //}
                //foreach(var meth in immType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                //{
                //    MainActivity.LogDebug(string.Format("Property Name: {0}", meth.Name));
                //}

                MainActivity.LogDebug(this.Resources.Configuration.HardKeyboardHidden.ToString()); //on pixel2 it is YES. on emulator with HW Keyboard = true it is NO

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
                    bsb.State = BottomSheetBehavior.StateHidden;

                }
                //test.ClearFocus(); //doesnt do anything. //maybe focus the search text.

                bsb.State = BottomSheetBehavior.StateHidden;
            }
        }

        private void FilterSticky_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            FilterSticky = e.IsChecked;
            if (FilterSticky)
            {
                FilterStickyString = SearchTabHelper.FilterString;
            }
        }

        private static void Search_EditorActionHELPER(object sender, TextView.EditorActionEventArgs e)
        {
            //bool x = SeekerState.MainActivityRef.IsDestroyed;

            //SearchFragment searchFragment = ((SeekerState.MainActivityRef.FindViewById(Resource.Id.pager) as AndroidX.ViewPager.Widget.ViewPager).Adapter as TabsPagerAdapter).GetItem(1) as SearchFragment;
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
                        //MainActivity.LogFirebase("editTextSearch is NULL only on cached activity");//these are both real cases that occur
                        editSearchText = searchHere.Text;
                    }
                    else
                    {
                        //MainActivity.LogFirebase("editTextSearch is NULL from both cached and MainActivity"); //these are both real cases that occur
                        editSearchText = SearchingText;
                    }
                }
                else
                {
                    editSearchText = editTextSearch.Text;
                }
                MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                //overriding this, the keyboard fails to go down by default for some reason.....
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                }
                catch (System.Exception ex)
                {
                    MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                }
                var transitionDrawable = GetTransitionDrawable();
                if (SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
                {
                    MainActivity.LogDebug("transitionDrawable: reverse transition");
                    transitionDrawable.ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                    SearchTabHelper.CancellationTokenSource.Cancel();
                    SearchTabHelper.CurrentlySearching = false;
                }
                else
                {
                    MainActivity.LogDebug("transitionDrawable: start transition");
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
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
            chooseUserInput.Visibility = ViewStates.Gone;
            SetSearchHintTarget(SearchTarget.AllUsers);
        }

        private void ChosenUser_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
            chooseUserInput.Visibility = ViewStates.Visible;
            SetSearchHintTarget(SearchTarget.ChosenUser);
        }

        private void UserList_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.UserList;
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
            chooseUserInput.Visibility = ViewStates.Gone;
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
                    MainActivity.LogInfoFirebase("GetTransitionDrawable activity is null");
                    SearchFragment f = GetSearchFragment();
                    if (f == null)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable no search fragment attached to activity");
                    }
                    else if (!f.IsAdded)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable attached but not added");
                    }
                    else if (f.Activity == null)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable f.Activity activity is null");
                    }
                    else
                    {
                        MainActivity.LogInfoFirebase("we should be using the fragment manager one...");
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



        private void SetRoomSpinnerAndEditTextInitial(Spinner s, EditText custom)
        {
            if (SearchTabHelper.SearchTargetChosenRoom == string.Empty)
            {
                s.SetSelection(0);
            }
            else
            {
                bool found = false;
                for (int i = 0; i < s.Adapter.Count; i++)
                {
                    if ((string)(s.GetItemAtPosition(i)) == SearchTabHelper.SearchTargetChosenRoom)
                    {
                        found = true;
                        s.SetSelection(i);
                        custom.Text = string.Empty;
                        break;
                    }
                }
                if (!found)
                {
                    s.SetSelection(s.Adapter.Count - 1);
                    custom.Text = SearchTabHelper.SearchTargetChosenRoom;
                }
            }
        }

        public void ShowChangeSortOrderDialog()
        {
            Context toUse = this.Activity != null ? this.Activity : SeekerState.MainActivityRef;
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(toUse, Resource.Style.MyAlertDialogTheme); //used to be our cached main activity ref...
            builder.SetTitle(Resource.String.sort_results_by_);
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
                    var old = SeekerState.DefaultSearchResultSortAlgorithm;
                    SeekerState.DefaultSearchResultSortAlgorithm = SearchTabHelper.SortHelperSorting; //whatever one we just changed it to.
                    if (old != SeekerState.DefaultSearchResultSortAlgorithm)
                    {
                        lock (MainActivity.SHARED_PREF_LOCK)
                        {
                            var editor = SeekerState.SharedPreferences.Edit();
                            editor.PutInt(KeyConsts.M_DefaultSearchResultSortAlgorithm, (int)SeekerState.DefaultSearchResultSortAlgorithm);
                            editor.Commit();
                        }
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

                    if (SearchTabHelper.FilteredResults)
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



                }
            }
        }





        private AutoCompleteTextView chooseUserInput = null;
        private EditText customRoomName = null;
        private Spinner roomListSpinner = null;
        private LinearLayout targetRoomLayout = null;
        public void ShowChangeTargetDialog()
        {
            Context toUse = this.Activity != null ? this.Activity : SeekerState.MainActivityRef;
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(toUse, Resource.Style.MyAlertDialogTheme); //used to be our cached main activity ref...
            builder.SetTitle(Resource.String.search_target_);
            View viewInflated = LayoutInflater.From(toUse).Inflate(Resource.Layout.changeusertarget, this.rootView as ViewGroup, false);
            chooseUserInput = viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserInput);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(chooseUserInput);
            customRoomName = viewInflated.FindViewById<EditText>(Resource.Id.customRoomName);
            targetRoomLayout = viewInflated.FindViewById<LinearLayout>(Resource.Id.targetRoomLayout);
            roomListSpinner = viewInflated.FindViewById<Spinner>(Resource.Id.roomListSpinner);

            AndroidX.AppCompat.Widget.AppCompatRadioButton allUsers = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.allUsers);
            AndroidX.AppCompat.Widget.AppCompatRadioButton chosenUser = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.chosenUser);
            AndroidX.AppCompat.Widget.AppCompatRadioButton userList = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetUserList);
            AndroidX.AppCompat.Widget.AppCompatRadioButton room = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetRoom);
            List<string> possibleRooms = new List<string>();
            if (ChatroomController.JoinedRoomNames != null && ChatroomController.JoinedRoomNames.Count != 0)
            {
                possibleRooms = ChatroomController.JoinedRoomNames.ToList();
            }
            possibleRooms.Add(SeekerState.ActiveActivityRef.GetString(Resource.String.custom_));
            roomListSpinner.Adapter = new ArrayAdapter<string>(SeekerState.ActiveActivityRef, Resource.Layout.support_simple_spinner_dropdown_item, possibleRooms.ToArray());
            SetRoomSpinnerAndEditTextInitial(roomListSpinner, customRoomName);
            chooseUserInput.Text = SearchTabHelper.SearchTargetChosenUser;
            switch (SearchTabHelper.SearchTarget)
            {
                case SearchTarget.AllUsers:
                    allUsers.Checked = true;
                    chooseUserInput.Visibility = ViewStates.Gone;
                    targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
                    break;
                case SearchTarget.UserList:
                    userList.Checked = true;
                    targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
                    chooseUserInput.Visibility = ViewStates.Gone;
                    break;
                case SearchTarget.ChosenUser:
                    chosenUser.Checked = true;
                    targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
                    chooseUserInput.Visibility = ViewStates.Visible;
                    chooseUserInput.Text = SearchTabHelper.SearchTargetChosenUser;
                    break;
                case SearchTarget.Room:
                    room.Checked = true;
                    chooseUserInput.Visibility = ViewStates.Gone;
                    targetRoomLayout.Visibility = ViewStates.Visible;
                    if (roomListSpinner.SelectedItem.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.custom_))
                    {
                        customRoomName.Visibility = ViewStates.Visible;
                        customRoomName.Text = SearchTabHelper.SearchTargetChosenRoom;
                    }
                    break;
            }

            allUsers.Click += AllUsers_Click;
            room.Click += Room_Click;
            chosenUser.Click += ChosenUser_Click;
            first = true;
            roomListSpinner.ItemSelected += RoomListSpinner_ItemSelected;
            userList.Click += UserList_Click;
            chooseUserInput.TextChanged += ChooseUserInput_TextChanged;
            customRoomName.TextChanged += CustomRoomName_TextChanged;


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
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                    }
                    eventHandlerClose(sender, null);
                }
            };

            chooseUserInput.EditorAction += editorAction;
            customRoomName.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.okay, eventHandlerClose);
            dialogInstance = builder.Create();
            dialogInstance.Show();
        }
        private bool first = true;
        private void RoomListSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (roomListSpinner.Adapter.Count - 1 == e.Position)
            {
                customRoomName.Visibility = ViewStates.Visible;
                if (first)
                {
                    first = false;
                }
                else
                {
                    customRoomName.Text = string.Empty; //if you go off this and back then it should clear
                    SearchTabHelper.SearchTargetChosenRoom = string.Empty;
                }
            }
            else
            {
                SearchTabHelper.SearchTargetChosenRoom = roomListSpinner.GetItemAtPosition(e.Position).ToString();
                customRoomName.Visibility = ViewStates.Gone;
            }
        }

        private void CustomRoomName_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            SearchTabHelper.SearchTargetChosenRoom = e.Text.ToString();
        }

        private string GetRoomListSpinnerSelection()
        {
            if (roomListSpinner.SelectedItem.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.custom_))
            {
                return SearchTabHelper.SearchTargetChosenRoom;
            }
            else
            {
                return roomListSpinner.SelectedItem.ToString();
            }
        }

        private void Room_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTargetChosenRoom = GetRoomListSpinnerSelection();
            SearchTabHelper.SearchTarget = SearchTarget.Room;
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Visible;
            chooseUserInput.Visibility = ViewStates.Gone;
            if (roomListSpinner.SelectedItem.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.custom_))
            {
                customRoomName.Visibility = ViewStates.Visible;
            }
            else
            {
                customRoomName.Visibility = ViewStates.Gone;
            }
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
                MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
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
                    MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                }
            }
        }

        //public class MyCallback : BottomSheetBehavior.BottomSheetCallback
        //{
        //    public override void OnSlide(View bottomSheet, float slideOffset)
        //    {
        //        //
        //    }

        //    public override void OnStateChanged(View bottomSheet, int newState)  //the main problem is the slow animation...
        //    {
        //        if(newState==BottomSheetBehavior.StateHidden)
        //        {
        //            try
        //            {

        //                //SeekerState.MainActivityRef.DispatchKeyEvent(new KeyEvent(new KeyEventActions(),Keycode.Enter));
        //                Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
        //                imm.HideSoftInputFromWindow(bottomSheet.WindowToken, 0);
        //            }
        //            catch
        //            {
        //                //not worth throwing over
        //            }
        //        }
        //    }
        //}

        public class ChipFilter
        {
            //this comes from "mp3 - all" and will match any (== "mp3") or (contains "mp3 ") results
            //the items in these filters are always OR'd
            public ChipFilter()
            {
                AllVarientsFileType = new List<string>();
                SpecificFileType = new List<string>();
                NumFiles = new List<int>();
                FileRanges = new List<Tuple<int, int>>();
                Keywords = new List<string>();
                KeywordInvarient = new List<List<string>>();

            }
            public List<string> AllVarientsFileType;
            public List<string> SpecificFileType;
            public List<int> NumFiles;
            public List<Tuple<int, int>> FileRanges;

            //these are the keywords.  keywords invarient will contain say "Paul and Jake", "Paul & Jake". they are OR'd inner.  both collections outer are AND'd.
            public List<string> Keywords;
            public List<List<string>> KeywordInvarient;

            public bool IsEmpty()
            {
                return (AllVarientsFileType.Count == 0 && SpecificFileType.Count == 0 && NumFiles.Count == 0 && FileRanges.Count == 0 && Keywords.Count == 0 && KeywordInvarient.Count == 0);
            }
        }

        public static ChipFilter ParseChips(SearchTab searchTab)
        {
            ChipFilter chipFilter = new ChipFilter();
            var checkedChips = searchTab.ChipDataItems.Where(i => i.IsChecked).ToList();
            foreach (var chip in checkedChips)
            {
                if (chip.ChipType == ChipType.FileCount)
                {
                    if (chip.DisplayText.EndsWith(" file"))
                    {
                        chipFilter.NumFiles.Add(1);
                    }
                    else if (chip.DisplayText.Contains(" to "))
                    {
                        int endmin = chip.DisplayText.IndexOf(" to ");
                        int min = int.Parse(chip.DisplayText.Substring(0, endmin));
                        int max = int.Parse(chip.DisplayText.Substring(endmin + 4, chip.DisplayText.IndexOf(" files") - (endmin + 4)));
                        chipFilter.FileRanges.Add(new Tuple<int, int>(min, max));
                    }
                    else if (chip.DisplayText.EndsWith(" files"))
                    {
                        chipFilter.NumFiles.Add(int.Parse(chip.DisplayText.Replace(" files", "")));
                    }
                }
                else if (chip.ChipType == ChipType.FileType)
                {
                    if (chip.HasTag())
                    {
                        foreach (var subChipString in chip.Children)
                        {
                            //its okay if this contains "mp3 (other)" say because if it does then by definition it will also contain
                            //mp3 - all bc we dont split groups.
                            if (subChipString.EndsWith(" - all"))
                            {
                                chipFilter.AllVarientsFileType.Add(subChipString.Replace(" - all", ""));
                            }
                            else
                            {
                                chipFilter.SpecificFileType.Add(subChipString);
                            }
                        }
                    }
                    else if (chip.DisplayText.EndsWith(" - all"))
                    {
                        chipFilter.AllVarientsFileType.Add(chip.DisplayText.Replace(" - all", ""));
                    }
                    else
                    {
                        chipFilter.SpecificFileType.Add(chip.DisplayText);
                    }
                }
                else if (chip.ChipType == ChipType.Keyword)
                {
                    if (chip.Children == null)
                    {
                        chipFilter.Keywords.Add(chip.DisplayText);
                    }
                    else
                    {
                        chipFilter.KeywordInvarient.Add(chip.Children);
                    }
                }
            }
            return chipFilter;
        }


        public static void ParseFilterString(SearchTab searchTab)
        {
            List<string> filterStringSplit = searchTab.FilterString.Split(' ').ToList();
            searchTab.WordsToAvoid.Clear();
            searchTab.WordsToInclude.Clear();
            searchTab.FilterSpecialFlags.Clear();
            foreach (string word in filterStringSplit)
            {
                if (word.Contains("mbr:") || word.Contains("minbitrate:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinBitRateKBS = Integer.ParseInt(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfs:") || word.Contains("minfilesize:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinFileSizeMB = (Integer.ParseInt(word.Split(':')[1]));
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfif:") || word.Contains("minfilesinfolder:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinFoldersInFile = Integer.ParseInt(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word == "isvbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsVBR = true;
                }
                else if (word == "iscbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsCBR = true;
                }
                else if (word.StartsWith('-'))
                {
                    if (word.Length > 1)//if just '-' dont remove everything. just skip it.
                    {
                        searchTab.WordsToAvoid.Add(word.Substring(1)); //skip the '-'
                    }
                }
                else
                {
                    searchTab.WordsToInclude.Add(word);
                }
            }
        }

        private bool MatchesChipCriteria(SearchResponse s, ChipFilter chipFilter, bool hideLocked)
        {
            if (chipFilter == null || chipFilter.IsEmpty())
            {
                return true;
            }
            else
            {
                bool match = chipFilter.NumFiles.Count == 0 && chipFilter.FileRanges.Count == 0;
                int fcount = hideLocked ? s.FileCount : s.FileCount + s.LockedFileCount;
                foreach (int num in chipFilter.NumFiles)
                {
                    if (fcount == num)
                    {
                        match = true;
                    }
                }
                foreach (Tuple<int, int> range in chipFilter.FileRanges)
                {
                    if (fcount >= range.Item1 && fcount <= range.Item2)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }

                match = chipFilter.AllVarientsFileType.Count == 0 && chipFilter.SpecificFileType.Count == 0;
                foreach (string varient in chipFilter.AllVarientsFileType)
                {
                    if (s.GetDominantFileType(hideLocked, out _) == varient || s.GetDominantFileType(hideLocked, out _).Contains(varient + " "))
                    {
                        match = true;
                    }
                }
                foreach (string specific in chipFilter.SpecificFileType)
                {
                    if (s.GetDominantFileType(hideLocked, out _) == specific)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }

                string fullFname = s.Files.FirstOrDefault()?.Filename ?? s.LockedFiles.FirstOrDefault().Filename;
                foreach (string keyword in chipFilter.Keywords)
                {
                    if (!Common.Helpers.GetFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase) &&
                        !Common.Helpers.GetParentFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return false;
                    }
                }
                foreach (List<string> keywordsInvar in chipFilter.KeywordInvarient)
                {
                    //do any match?
                    bool anyMatch = false;
                    foreach (string keyword in keywordsInvar)
                    {
                        if (Common.Helpers.GetFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase) ||
                            Common.Helpers.GetParentFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                        {
                            anyMatch = true;
                            break;
                        }
                    }
                    if (!anyMatch)
                    {
                        return false;
                    }
                }
                if (!match)
                {
                    return false;
                }

                return true;
            }
        }


        private bool MatchesCriteria(SearchResponse s, bool hideLocked)
        {
            foreach (File f in s.GetFiles(hideLocked))
            {
                string dirString = Common.Helpers.GetFolderNameFromFile(f.Filename);
                string fileString = CommonHelpers.GetFileNameFromFile(f.Filename);
                foreach (string avoid in SearchTabHelper.WordsToAvoid)
                {
                    if (dirString.Contains(avoid, StringComparison.OrdinalIgnoreCase) || fileString.Contains(avoid, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                bool includesAll = true;
                foreach (string include in SearchTabHelper.WordsToInclude)
                {
                    if (!dirString.Contains(include, StringComparison.OrdinalIgnoreCase) && !fileString.Contains(include, StringComparison.OrdinalIgnoreCase))
                    {
                        includesAll = false;
                        break;
                    }
                }
                if (includesAll)
                {
                    return true;
                }
            }
            return false;
        }

        //should be called whenever either the filter changes, new search results come in, the search gets cleared, etc.
        //includes chips
        private void UpdateFilteredResponses(SearchTab searchTab)
        {
            //The Rules:
            //if separated by space, then it must contain both them, in any order
            //if - in front then it must not contain this word
            //there are also several keywords

            MainActivity.LogDebug("Words To Avoid: " + searchTab.WordsToAvoid.ToString());
            MainActivity.LogDebug("Words To Include: " + searchTab.WordsToInclude.ToString());
            MainActivity.LogDebug("Whether to Filer: " + searchTab.FilteredResults);
            MainActivity.LogDebug("FilterString: " + searchTab.FilterString);
            bool hideLocked = SeekerState.HideLockedResultsInSearch;
            searchTab.UI_SearchResponses.Clear();
            searchTab.UI_SearchResponses.AddRange(searchTab.SearchResponses.FindAll(new Predicate<SearchResponse>(
            (SearchResponse s) =>
            {
                if (!MatchesCriteria(s, hideLocked))
                {
                    return false;
                }
                else if (!MatchesChipCriteria(s, searchTab.ChipsFilter, hideLocked))
                {
                    return false;
                }
                else
                {   //so it matches the word criteria.  now lets see if it matches the flags if any...
                    if (!searchTab.FilterSpecialFlags.ContainsSpecialFlags)
                    {
                        return true;
                    }
                    else
                    {
                        //we need to make sure this also matches our special flags
                        if (searchTab.FilterSpecialFlags.MinFoldersInFile != 0)
                        {
                            if (searchTab.FilterSpecialFlags.MinFoldersInFile > (hideLocked ? s.Files.Count : (s.Files.Count + s.LockedFiles.Count)))
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.MinFileSizeMB != 0)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                int mb = (int)(f.Size) / (1024 * 1024);
                                if (mb > searchTab.FilterSpecialFlags.MinFileSizeMB)
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.MinBitRateKBS != 0)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                if (f.BitRate == null || !(f.BitRate.HasValue))
                                {
                                    continue;
                                }
                                if ((int)(f.BitRate) > searchTab.FilterSpecialFlags.MinBitRateKBS)
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.IsCBR)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                if (f.IsVariableBitRate == false)//this is bool? can have no value...
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.IsVBR)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                if (f.IsVariableBitRate == true)
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            })));
        }

        private void FilterText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            MainActivity.LogDebug("Text Changed: " + e.Text);
            string oldFilterString = SearchTabHelper.FilteredResults ? SearchTabHelper.FilterString : string.Empty;
            if ((e.Text != null && e.Text.ToString() != string.Empty && SearchTabHelper.SearchResponses != null) || this.AreChipsFiltering())
            {

#if DEBUG
                var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
                SearchTabHelper.FilteredResults = true;
                SearchTabHelper.FilterString = e.Text.ToString();
                if (FilterSticky)
                {
                    FilterStickyString = SearchTabHelper.FilterString;
                }
                ParseFilterString(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);

                var oldList = SearchTabHelper.UI_SearchResponses.ToList();
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);



#if DEBUG

                int oldCount = oldList.Count;
                int newCount = SearchTabHelper.UI_SearchResponses.Count();
                MainActivity.LogDebug($"update filtered only - old {oldCount} new {newCount} time {sw.ElapsedMilliseconds} ms");

#endif

                //DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(oldList, SearchTabHelper.UI_SearchResponses), true);

                //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.FilteredResponses);
                //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                //lv.Adapter = (customAdapter);

                //res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);
#if DEBUG
                sw.Stop();
#endif
                //DIFFUTIL is extremely extremely slow going from large to small number i.e. 1000 to 250 results.  
                //it takes a full 700ms.  Whereas NotifyDataSetChanged and setting the adapter take 0-7ms. with notifydatasetcahnged being a bit faster.

                recyclerSearchAdapter.NotifyDataSetChanged(); //does have the nice effect that if nothing changes, you dont just back to top. (unlike old method)
#if DEBUG
                MainActivity.LogDebug($"old {oldCount} new {newCount} time {sw.ElapsedMilliseconds} ms");

#endif

            }
            else
            {
                SearchTabHelper.FilteredResults = false;
                SearchTabHelper.FilterString = string.Empty;
                if (FilterSticky)
                {
                    FilterStickyString = SearchTabHelper.FilterString;
                }
                ParseFilterString(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);



                // DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(SearchTabHelper.UI_SearchResponses, SearchTabHelper.SearchResponses), true);


                SearchTabHelper.UI_SearchResponses.Clear();
                SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);
                //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.Clear();
                //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.AddRange(newList);

                //res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);




                recyclerSearchAdapter.NotifyDataSetChanged(); //does have the nice effect that if nothing changes, you dont just back to top.
                //recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
            }

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
            if (this.AreChipsFiltering() || !string.IsNullOrEmpty(SearchTabHelper.FilterString))
            {
                SearchTabHelper.FilteredResults = true;
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);

                //recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                //recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);

                recyclerSearchAdapter.NotifyDataSetChanged();
                bool refSame = System.Object.ReferenceEquals(recyclerSearchAdapter.localDataSet, SearchTabHelper.UI_SearchResponses);
                bool refSame2 = System.Object.ReferenceEquals(recyclerSearchAdapter.localDataSet, SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].UI_SearchResponses);
                //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.FilteredResponses);
                //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                //lv.Adapter = (customAdapter);
            }
            else
            {
                SearchTabHelper.FilteredResults = false;
                SearchTabHelper.UI_SearchResponses.Clear();// = SearchTabHelper.SearchResponses.ToList();
                SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);// = SearchTabHelper.SearchResponses.ToList();
                //recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                //recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
                recyclerSearchAdapter.NotifyDataSetChanged();
            }
        }

        //private void Actv_Click(object sender, EventArgs e)
        //{
        //    Android.Views.InputMethods.InputMethodManager im = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
        //    im.ShowSoftInput(sender as View, 0);
        //    (sender as View).RequestFocus();
        //}

        private void SeekerState_ClearSearchHistory(object sender, EventArgs e)
        {
            searchHistory = new List<string>();
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_SearchHistory, string.Empty);
                editor.Commit();
            }
            if (SeekerState.MainActivityRef?.SupportActionBar?.CustomView != null)
            {
                AutoCompleteTextView actv = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                actv.Adapter = new ArrayAdapter<string>(context, Resource.Layout.autoSuggestionRow, searchHistory);
            }
        }

        private void UpdateForScreenSize()
        {
            if (!SeekerState.IsLowDpi()) return;
            try
            {
                //this.rootView.FindViewById<TextView>(Resource.Id.searchesQueue).SetTextSize(ComplexUnitType.Dip,8);
                //this.rootView.FindViewById<TextView>(Resource.Id.searchesKbs).SetTextSize(ComplexUnitType.Sp, 8);
            }
            catch
            {
                //not worth throwing over
            }
        }

        public override void OnPause()
        {
            MainActivity.LogDebug("SearchFragmentOnPause");
            base.OnPause();

            string listOfSearchItems = string.Empty;
            using (var writer = new System.IO.StringWriter())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(searchHistory.GetType());
                serializer.Serialize(writer, searchHistory);
                listOfSearchItems = writer.ToString();
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_SearchHistory, listOfSearchItems);
                if (FilterSticky)
                {
                    editor.PutBoolean(KeyConsts.M_FilterSticky, FilterSticky);
                    editor.PutString(KeyConsts.M_FilterStickyString, SearchTabHelper.FilterString);
                }
                editor.PutInt(KeyConsts.M_SearchResultStyle, (int)SearchResultStyle);
                editor.Commit();
            }
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
                MainActivity.LogDebug("transitionDrawable: RESET transition");
                transitionDrawable.ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                SearchTabHelper.CancellationTokenSource.Cancel();
                SearchTabHelper.CurrentlySearching = false;
            }
            else
            {
                transitionDrawable.StartTransition(SearchToCloseDuration);
                PerformBackUpRefresh();
                MainActivity.LogDebug("START TRANSITION");
                SearchTabHelper.CurrentlySearching = true;
            }
            SearchTabHelper.CancellationTokenSource = new CancellationTokenSource();
            EditText editTextSearch = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            SearchAPI(SearchTabHelper.CancellationTokenSource.Token, transitionDrawable, editTextSearch.Text, SearchTabHelper.CurrentTab);
            if (sender != null)
            {
                (sender as AutoCompleteTextView).DismissDropDown();
            }
            MainActivity.LogDebug("Enter Pressed..");
        }

        private void Actv_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down)
            {
                MainActivity.LogDebug("ENTER PRESSED " + e.KeyCode.ToString());
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
                    //MainActivity.LogDebug(e.KeyCode.ToString()); //happens on HW keyboard... event does NOT get called on SW keyboard. :)
                    //MainActivity.LogDebug((sender as AutoCompleteTextView).IsFocused.ToString());
                    (sender as AutoCompleteTextView).OnKeyDown(e.KeyCode, e.Event);
                }
            }
        }

        private static void SoulseekClient_SearchResponseReceived(object sender, SearchResponseReceivedEventArgs e, int fromTab, bool fromWishlist)
        {
            //MainActivity.LogDebug("SoulseekClient_SearchResponseReceived");
            //MainActivity.LogDebug(e.Response.Username + " queuelength: " + e.Response.QueueLength + " free upload slots" + e.Response.FreeUploadSlots);
            //Console.WriteLine("Response Received");
            //CustomAdapter customAdapter = new CustomAdapter(Context, searchResponses);
            //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
            //lv.Adapter = (customAdapter);
            if (e.Response.FileCount == 0 && SeekerState.HideLockedResultsInSearch || !SeekerState.HideLockedResultsInSearch && e.Response.FileCount == 0 && e.Response.LockedFileCount == 0)
            {
                MainActivity.LogDebug("Skipping Locked or 0/0");
                return;
            }
            //MainActivity.LogDebug("SEARCH RESPONSE RECEIVED");
            refreshListView(e.Response, fromTab, fromWishlist);
            //SeekerState.MainActivityRef.RunOnUiThread(action);

        }

        private static void clearListView(bool fromWishlist)
        {
            if (fromWishlist)
            {
                return; //we combine results...
            }


            MainActivity.LogDebug("clearListView SearchResponses.Clear()");
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
                SearchFragment.Instance.recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                SearchFragment.Instance.recyclerViewTransferItems.SetAdapter(SearchFragment.Instance.recyclerSearchAdapter);

                SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
                SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);


            }
        }


        public override void OnDetach() //happens whenever the fragment gets recreated.  (i.e. on rotating device).
        {
            MainActivity.LogDebug("search frag detach");
            base.OnDetach();
        }
        private AutoCompleteTextView searchEditText = null;
        public override void OnAttach(Android.App.Activity activity)
        {
            MainActivity.LogDebug("search frag attach");
            base.OnAttach(activity);
        }

        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        private SearchAdapterRecyclerVersion recyclerSearchAdapter;

        public class SearchAdapterRecyclerVersion : RecyclerView.Adapter
        {
            public List<int> oppositePositions = new List<int>();



            public List<SearchResponse> localDataSet;
            public override int ItemCount => localDataSet.Count;
            private int position = -1;

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {


                (holder as SearchViewHolder).getSearchItemView().setItem(localDataSet[position], position);
                //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            }

            public void setPosition(int position)
            {
                this.position = position;
            }

            public int getPosition()
            {
                return this.position;
            }

            //public override void OnViewRecycled(Java.Lang.Object holder)
            //{
            //    base.OnViewRecycled(holder);
            //}

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                ISearchItemViewBase view = null;
                switch (this.searchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandedAll:
                    case SearchResultStyleEnum.CollapsedAll:
                        view = SearchItemViewExpandable.inflate(parent);
                        (view as SearchItemViewExpandable).AdapterRef = this;
                        (view as View).FindViewById<ImageView>(Resource.Id.expandableClick).Click += CustomAdapter_Click;
                        (view as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1;
                        break;
                    case SearchResultStyleEnum.Medium:
                        view = SearchItemViewMedium.inflate(parent);
                        break;
                    case SearchResultStyleEnum.Minimal:
                        view = SearchItemViewMinimal.inflate(parent);
                        break;
                }
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //view.LongClick += TransferAdapterRecyclerVersion_LongClick;
                (view as View).Click += View_Click;
                return new SearchViewHolder(view as View);

            }

            private void View_Click(object sender, EventArgs e)
            {
                GetSearchFragment().showEditDialog((sender as ISearchItemViewBase).ViewHolder.AdapterPosition);
            }

            private SearchResultStyleEnum searchResultStyle;

            public SearchAdapterRecyclerVersion(List<SearchResponse> ti)
            {
                oldList = null; // no longer valid...
                localDataSet = ti;
                searchResultStyle = SearchFragment.SearchResultStyle;
                oppositePositions = new List<int>();
            }

            private void CustomAdapter_Click1(object sender, EventArgs e)
            {
                //MainActivity.LogInfoFirebase("CustomAdapter_Click1");
                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View);
                SearchFragment.Instance.showEditDialog(position);
            }


            private void CustomAdapter_Click(object sender, EventArgs e)
            {
                //throw new NotImplementedException();


                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View);

                //int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
                var v = ((sender as View).Parent.Parent as View).FindViewById<View>(Resource.Id.detailsExpandable);
                var img = ((sender as View).Parent.Parent as View).FindViewById<ImageView>(Resource.Id.expandableClick);
                if (v.Visibility == ViewStates.Gone)
                {
                    img.Animate().RotationBy((float)(180.0)).SetDuration(350).Start();
                    v.Visibility = ViewStates.Visible;
                    SearchItemViewExpandable.PopulateFilesListView(v as LinearLayout, this.localDataSet[position]);
                    if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
                    {
                        oppositePositions.Add(position);
                        oppositePositions.Sort();
                    }
                    else
                    {
                        oppositePositions.Remove(position);
                    }
                }
                else
                {
                    img.Animate().RotationBy((float)(-180.0)).SetDuration(350).Start();
                    v.Visibility = ViewStates.Gone;
                    if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
                    {
                        oppositePositions.Remove(position);
                    }
                    else
                    {
                        oppositePositions.Add(position);
                        oppositePositions.Sort();
                    }
                }
            }
        }

        public class SearchViewHolder : RecyclerView.ViewHolder
        {
            private ISearchItemViewBase searchItemView;

            public SearchViewHolder(View view) : base(view)
            {
                //super(view);
                // Define click listener for the ViewHolder's View

                searchItemView = (ISearchItemViewBase)view;
                searchItemView.ViewHolder = this;
                //searchItemView.SetOnCreateContextMenuListener(this);
            }

            public ISearchItemViewBase getSearchItemView()
            {
                return searchItemView;
            }
        }

        public class SearchDiffCallback : DiffUtil.Callback
        {
            private List<SearchResponse> oldList;
            private List<SearchResponse> newList;

            public SearchDiffCallback(List<SearchResponse> _oldList, List<SearchResponse> _newList)
            {
                oldList = _oldList;
                newList = _newList;
            }

            public override int NewListSize => newList.Count;

            public override int OldListSize => oldList.Count;

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                return oldList[oldItemPosition].Equals(newList[newItemPosition]); //my override
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                return oldList[oldItemPosition] == newList[newItemPosition];
            }
        }



        //public override void OnStop()
        //{
        //    searchEditText.KeyPress -= Actv_KeyPress;
        //    searchEditText.EditorAction -= Search_EditorAction;

        //    base.OnStop();
        //}

        //public override void OnResume()
        //{

        //    base.OnResume();
        //    searchEditText = (this.Activity as AndroidX.AppCompat.App.AppCompatActivity).SupportActionBar.CustomView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);

        //    searchEditText.KeyPress -= Actv_KeyPress;
        //    searchEditText.KeyPress += Actv_KeyPress;
        //    searchEditText.EditorAction -= Search_EditorAction;
        //    searchEditText.EditorAction += Search_EditorAction;
        //}

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
        private static List<SearchResponse> newList = new List<SearchResponse>();

        /// <summary>
        /// To add a search response to the list view
        /// </summary>
        /// <param name="resp"></param>
        private static void refreshListView(SearchResponse resp, int fromTab, bool fromWishlist)
        {
            //sort before adding.

            //if search response has multiple directories, we want to split it up just like 
            //how desktop client does.
            //if(SearchTabHelper.CurrentTab == fromTab) //we want to sort 
            //{


            lock (SearchTabHelper.SearchTabCollection[fromTab].SortHelperLockObject) //lock object for the sort helper in question. this used to be the current tab one. I think thats wrong.
            {
                Tuple<bool, List<SearchResponse>> splitResponses = new Tuple<bool, List<SearchResponse>>(false, null);
                try
                {
                    splitResponses = Common.SearchResponseUtil.SplitMultiDirResponse(SeekerState.HideLockedResultsInSearch, resp);
                }
                catch (System.Exception e)
                {
                    MainActivity.LogFirebase(e.Message + " splitmultidirresponse");
                }

                try
                {

                    if (splitResponses.Item1)
                    { //we have multiple to add
                        foreach (SearchResponse splitResponse in splitResponses.Item2)
                        {
                            if (fromWishlist && WishlistController.OldResultsToCompare[fromTab].Contains(splitResponse))
                            {
                                continue;
                            }
                            SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Add(splitResponse, null);
                        }
                    }
                    else
                    {
                        if (fromWishlist && WishlistController.OldResultsToCompare[fromTab].Contains(resp))
                        {
                        }
                        else
                        {
                            SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Add(resp, null); //before I added an .Equals method I would get Duplicate Key Exceptions...
                        }
                    }
                }
                catch (System.Exception e)
                {
                    MainActivity.LogDebug(e.Message);
                }

                SearchTabHelper.SearchTabCollection[fromTab].SearchResponses = SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Keys.ToList();
                SearchTabHelper.SearchTabCollection[fromTab].LastSearchResultsCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
            }

            //if (fromTab == SearchTabHelper.CurrentTab)
            //{
            //    newList = SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Keys.ToList();
            //}
            //only do fromWishlist if SearchFragment.Instance is not null...

            if ((!fromWishlist || SearchFragment.Instance != null) && fromTab == SearchTabHelper.CurrentTab)
            {
                Action a = new Action(() =>
                {
#if DEBUG
                    Seeker.SearchFragment.StopWatch.Stop();
                    //MainActivity.LogDebug("time between start and stop " + AndriodApp1.SearchFragment.StopWatch.ElapsedMilliseconds);
                    Seeker.SearchFragment.StopWatch.Reset();
                    Seeker.SearchFragment.StopWatch.Start();
#endif
                    //SearchResponses.Add(resp);
                    //MainActivity.LogDebug("UI - SEARCH RESPONSE RECEIVED");
                    if (fromTab != SearchTabHelper.CurrentTab)
                    {
                        return;
                    }
                    //int total = newList.Count;
                    int total = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
                    //MainActivity.LogDebug("START _ ui thread response received - search collection: " + total);
                    if (SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount == total)
                    {
                        //MainActivity.LogDebug("already did it..: " + total);
                        //we already updated for this one.
                        //the UI marshelled calls are delayed.  as a result there will be many all coming in with the final search response count of say 751.  
                        return;
                    }

                    //MainActivity.LogDebug("refreshListView SearchResponses.Count = " + SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count);

                    if (SearchTabHelper.SearchTabCollection[fromTab].FilteredResults)
                    {
                        //SearchTabHelper.SearchTabCollection[fromTab].SearchResponses = newList;
                        oldList = GetOldList(SearchTabHelper.SearchTabCollection[fromTab].FilterString);
                        if (oldList == null)
                        {
                            SearchFragment.Instance.UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);  //WE JUST NEED TO FILTER THE NEW RESPONSES!!
                                                                                                                            //todo: diffutil.. was filtered -> now filtered...
                            SearchFragment.Instance.recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                            SearchFragment.Instance.recyclerViewTransferItems.SetAdapter(SearchFragment.Instance.recyclerSearchAdapter);
                        }
                        else
                        {
                            //todo: place back
                            var recyclerViewState = SearchFragment.Instance.recycleLayoutManager.OnSaveInstanceState();//  recyclerView.getLayoutManager().onSaveInstanceState();


                            SearchFragment.Instance.UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);
                            MainActivity.LogDebug("refreshListView  oldList: " + oldList.Count + " newList " + newList.Count);
                            DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(oldList, SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses), true);
                            //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.Clear();
                            //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.AddRange(newList);
                            res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);


                            SearchFragment.Instance.recycleLayoutManager.OnRestoreInstanceState(recyclerViewState);
                        }
                        SetOldList(SearchTabHelper.SearchTabCollection[fromTab].FilterString, SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses.ToList());
                        //SearchAdapter customAdapter = new SearchAdapter(SearchFragment.Instance.context, SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses);
                        //SearchFragment.Instance.listView.Adapter = (customAdapter);
                    }
                    else
                    {
                        oldList = GetOldList(null);
                        List<SearchResponse> newListx = null;
                        if (oldList == null)
                        {
                            SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.ToList();
                            newListx = SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses;
                            SearchFragment.Instance.recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                            SearchFragment.Instance.recyclerViewTransferItems.SetAdapter(SearchFragment.Instance.recyclerSearchAdapter);
                        }
                        else
                        {
                            //the SaveInstanceState and RestoreInstanceState are needed, else autoscroll... even when animations are off...
                            //https://stackoverflow.com/questions/43458146/diffutil-in-recycleview-making-it-autoscroll-if-a-new-item-is-added
                            var recyclerViewState = SearchFragment.Instance.recycleLayoutManager.OnSaveInstanceState();//  recyclerView.getLayoutManager().onSaveInstanceState();

                            newListx = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.ToList();
#if DEBUG
                            if (oldList.Count == 0)
                            {
                                MainActivity.LogDebug("refreshListView  oldList: " + oldList.Count + " newList " + newListx.Count);
                            }
#endif
                            DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(oldList, newListx), true); //race condition where gototab sets oldList to empty and so in DiffUtil we get an index out of range.... or maybe a wishlist happening at thte same time does it??????
                            //SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Clear();
                            //SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.AddRange(newList);
                            SearchFragment.Instance.recyclerSearchAdapter.localDataSet.Clear();
                            SearchFragment.Instance.recyclerSearchAdapter.localDataSet.AddRange(newListx);
                            res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);

                            SearchFragment.Instance.recycleLayoutManager.OnRestoreInstanceState(recyclerViewState);
                        }

                        //when I was adding an empty list here updates only took 1 millisecond (though updating was choppy and weird)... whereas with an actual diff it takes 10 - 50ms but looks a lot nicer.
                        SetOldList(null, newListx);
                        // 

                        //SearchAdapter customAdapter = new SearchAdapter(SearchFragment.Instance.context, SearchTabHelper.SearchTabCollection[fromTab].SearchResponses);
                        //SearchFragment.Instance.listView.Adapter = (customAdapter);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = total;
                    Seeker.SearchFragment.StopWatch.Stop();
                    //MainActivity.LogDebug("time it takes to set adapter for " + total + " results: " + AndriodApp1.SearchFragment.StopWatch.ElapsedMilliseconds);
#if DEBUG
                    Seeker.SearchFragment.StopWatch.Reset();
                    Seeker.SearchFragment.StopWatch.Start();
#endif

                    //                    oldList = newList.ToList();

                    //MainActivity.LogDebug("END _ ui thread response received - search collection: " + total);
                });

                SeekerState.MainActivityRef?.RunOnUiThread(a);

            }

        }

        public static System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();

        private void Lv_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            showEditDialog(e.Position);
            //throw new NotImplementedException();
        }

        //private long lastClickTime = 0;
        public static bool dlDialogShown = false;

        public void showEditDialog(int pos)
        {
            try
            {
                //if (SystemClock.ElapsedRealtime() - lastClickTime < 500)
                //{
                //    return;
                //}
                //lastClickTime = SystemClock.ElapsedRealtime();
                if (dlDialogShown)
                {
                    dlDialogShown = false; //just in the worst case we dont want to prevent too badly.
                    return;
                }
                dlDialogShown = true;
                SearchResponse dlDiagResp = null;
                if (SearchTabHelper.FilteredResults)
                {
                    dlDiagResp = SearchTabHelper.UI_SearchResponses.ElementAt<SearchResponse>(pos);
                }
                else
                {
                    dlDiagResp = SearchTabHelper.SearchResponses.ElementAt<SearchResponse>(pos);
                }
                DownloadDialog downloadDialog = DownloadDialog.CreateNewInstance(pos, dlDiagResp);
                downloadDialog.Show(FragmentManager, "tag_download_test");
                // When creating a DialogFragment from within a Fragment, you must use the Fragment's CHILD FragmentManager to ensure that the state is properly restored after configuration changes. ????
            }
            catch (System.Exception e)
            {
                System.String msg = string.Empty;
                if (SearchTabHelper.FilteredResults)
                {
                    msg = "Filtered.Count " + SearchTabHelper.UI_SearchResponses.Count.ToString() + " position selected = " + pos.ToString();
                }
                else
                {
                    msg = "SearchResponses.Count = " + SearchTabHelper.SearchResponses.Count.ToString() + " position selected = " + pos.ToString();
                }

                MainActivity.LogFirebase(msg + " showEditDialog" + e.Message);
                Action a = new Action(() => { Toast.MakeText(SeekerState.ActiveActivityRef, "Error, please try again: " + msg, ToastLength.Long).Show(); });
                SeekerState.ActiveActivityRef.RunOnUiThread(a);
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
                    MainActivity.LogDebug("perform backup refresh");
                }

            }), 310);
        }

        public const int SearchToCloseDuration = 300;

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
            //MainActivity.ShowAlert(new System.Exception("test"),this.Context);
            //EditText editTextSearch = null;
            try
            {
                //all click event handlers occur on UI thread.
                clearListView(fromWishlist);
                //editTextSearch = SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            }
            catch (System.Exception e)
            {
                //if(SeekerState.MainActivityRef==null)
                //{
                //    MainActivity.LogFirebase("Search Logic: MainActivityRef is null");
                //}
                //else if(SeekerState.MainActivityRef.SupportActionBar==null)
                //{
                //    MainActivity.LogFirebase("Search Logic: Support Action Bar");
                //}
                //else if(SeekerState.MainActivityRef.SupportActionBar.CustomView == null)
                //{
                //    MainActivity.LogFirebase("Search Logic: SupportActionBar.CustomView");
                //}
                //else if(SeekerState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere)==null)
                //{
                //    MainActivity.LogFirebase("Search Logic: searchHere");
                //}
                //throw e;
            }
            //in my testing:
            // if someone has 0 free upload slots I could never download from them (even if queue was 68, 250) the progress bar just never moves, though no error, nothing.. ****
            // if someone has 1 free upload slot and a queue size of 100, 143, 28, 5, etc. it worked just fine.
            int searchTimeout = SearchTabHelper.SearchTarget == SearchTarget.AllUsers ? 5000 : 12000;

            Action<SearchResponseReceivedEventArgs> searchResponseReceived = new Action<SearchResponseReceivedEventArgs>((SearchResponseReceivedEventArgs e) =>
            {
                SoulseekClient_SearchResponseReceived(null, e, fromTab, fromWishlist);
            });

            SearchOptions searchOptions = new SearchOptions(responseLimit: SeekerState.NumberSearchResults, searchTimeout: searchTimeout, maximumPeerQueueLength: int.MaxValue, minimumPeerFreeUploadSlots: SeekerState.FreeUploadSlotsOnly ? 1 : 0, responseReceived: searchResponseReceived);
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
                if (SeekerState.UserList == null || SeekerState.UserList.Count == 0)
                {
                    SeekerState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SeekerState.MainActivityRef, Resource.String.user_list_empty, ToastLength.Short).Show();
                    }
                    ));
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, SeekerState.UserList.Select(item => item.Username).ToArray());
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.ChosenUser)
            {
                if (SearchTabHelper.SearchTargetChosenUser == string.Empty)
                {
                    SeekerState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SeekerState.MainActivityRef, Resource.String.no_user, ToastLength.Short).Show();
                    }));
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, new string[] { SearchTabHelper.SearchTargetChosenUser });
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.Room)
            {
                if (SearchTabHelper.SearchTargetChosenRoom == string.Empty)
                {
                    SeekerState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SeekerState.MainActivityRef, Resource.String.no_room, ToastLength.Short).Show();
                    }));
                    return;
                }
                scope = new SearchScope(SearchScopeType.Room, new string[] { SearchTabHelper.SearchTargetChosenRoom });
            }
            try
            {
                Task<IReadOnlyCollection<SearchResponse>> t = null;
                if (fromTab == SearchTabHelper.CurrentTab)
                {
                    //there was a bug where wishlist search would clear this in the middle of diffutil calculating causing out of index crash.
                    oldList?.Clear();
                }
                t = SeekerState.SoulseekClient.SearchAsync(SearchQuery.FromText(searchString), options: searchOptions, scope: scope, cancellationToken: cancellationToken);
                //t = TestClient.SearchAsync(searchString, searchResponseReceived, cancellationToken);
                //drawable.StartTransition() - since if we get here, the search is launched and the continue with will always happen...

                t.ContinueWith(new Action<Task<IReadOnlyCollection<SearchResponse>>>((Task<IReadOnlyCollection<SearchResponse>> t) =>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;

                    if (!t.IsCompletedSuccessfully && t.Exception != null)
                    {
                        MainActivity.LogDebug("search exception: " + t.Exception.Message);
                    }

                    if (t.IsCanceled)
                    {
                        //then the user pressed the button so we dont need to change it back...
                        //GetSearchFragment().GetTransitionDrawable().ResetTransition(); //this does it immediately.
                    }
                    else
                    {

                        SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                        {
                            try
                            {
                                if (fromTab == SearchTabHelper.CurrentTab && !fromWishlist)
                                {
                                    MainActivity.LogDebug("transitionDrawable: ReverseTransition transition");
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
                    if ((!t.IsCanceled) && t.Result.Count == 0 && !fromWishlist) //if t is cancelled, t.Result throws..
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                        {
                            Toast.MakeText(SeekerState.MainActivityRef, Resource.String.no_search_results, ToastLength.Short).Show();
                        }));
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
                        if (SeekerState.ShowSmartFilters)
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
                            List<ChipDataItem> chipDataItems = ChipsHelper.GetChipDataItemsFromSearchResults(SearchTabHelper.SearchTabCollection[fromTab].SearchResponses, SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm, SeekerState.SmartFilterOptions);
                            SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = chipDataItems;
                            SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                            {
                                SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                                SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                            }));
                        }

                    }

                }));



                if (SearchTabHelper.FilteredResults && FilterSticky && !fromWishlist)
                {
                    //remind the user that the filter is ON.
                    t.ContinueWith(new Action<Task>(
                        (Task t) =>
                        {
                            SeekerState.MainActivityRef.RunOnUiThread(new Action(() =>
                            {

                                RelativeLayout rel = SearchFragment.Instance.rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
                                BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
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

                    Toast.MakeText(SeekerState.ActiveActivityRef, errorMsg, ToastLength.Short).Show();
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
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
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    Toast.MakeText(SeekerState.ActiveActivityRef, errorMsg, ToastLength.Short).Show();
                    if (!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
                    {
                        transitionDrawable.ResetTransition();
                    }
                }));
                return;
            }
            //catch(InvalidOperationException)
            //{
            //    //this means that we lost connection to the client.  lets re-login and try search again..
            //    //idealy we do this on a separate thread but for testing..

            //    //SeekerState.SoulseekClient.SearchAsync(SearchQuery.FromText(editTextSearch.Text), options: searchOptions);
            //}
            catch (System.Exception ue)
            {

                SeekerState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    MainActivity.LogDebug("transitionDrawable: RESET transition");

                    Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.search_error_unspecified, ToastLength.Short).Show();
                    MainActivity.LogFirebase("tabpageradapter searchclick: " + ue.Message);

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
                if (SeekerState.RememberSearchHistory)
                {
                    if (!searchHistory.Contains(searchString))
                    {
                        searchHistory.Add(searchString);
                    }
                }
                var actv = SeekerState.MainActivityRef.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere); // lot of nullrefs with actv before this change....
                if (actv == null)
                {
                    actv = (SearchFragment.Instance.Activity as AndroidX.AppCompat.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                    if (actv == null)
                    {
                        MainActivity.LogFirebase("actv stull null, cannot refresh adapter");
                        return;
                    }
                }
                actv.Adapter = new ArrayAdapter<string>(SearchFragment.Instance.context, Resource.Layout.autoSuggestionRow, searchHistory); //refresh adapter
            }
        }

        public static void SearchAPI(CancellationToken cancellationToken, Android.Graphics.Drawables.TransitionDrawable transitionDrawable, string searchString, int fromTab, bool fromWishlist = false)
        {
            SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm = searchString;
            SearchTabHelper.SearchTabCollection[fromTab].LastRanTime = CommonHelpers.GetDateTimeNowSafe();
            if (!fromWishlist)
            {
                //try to clearFocus on the search if you can (gets rid of blinking cursor)
                ClearFocusSearchEditText();
                MainActivity.LogDebug("Search_Click");
            }
            //#if !DEBUG
            if (!SeekerState.currentlyLoggedIn)
            {
                if (!fromWishlist)
                {
                    Toast tst = Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.must_be_logged_to_search, ToastLength.Long);
                    tst.Show();
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    transitionDrawable.ResetTransition();

                }

                SearchTabHelper.CurrentlySearching = false;
                return;
            }
            else if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //re-connect if from wishlist as well. just do it quietly.
                //if (fromWishlist)
                //{
                //    return;
                //}
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, fromWishlist, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        if (!fromWishlist)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                            });
                        }
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { SearchLogic(cancellationToken, transitionDrawable, searchString, fromTab, fromWishlist); });

                }));
            }
            else
            {
                //#endif
                SearchLogic(cancellationToken, transitionDrawable, searchString, fromTab, fromWishlist);
                //#if !DEBUG
            }
            //#endif
        }
    }

}