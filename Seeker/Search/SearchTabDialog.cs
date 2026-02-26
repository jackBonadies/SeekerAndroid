using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Seeker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    public class SearchTabDialog : AndroidX.Fragment.App.DialogFragment, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private RecyclerView recyclerViewSearches = null;
        private RecyclerView recyclerViewWishlists = null;

        private LinearLayoutManager recycleSearchesLayoutManager = null;
        private LinearLayoutManager recycleWishlistsLayoutManager = null;

        public SearchTabItemRecyclerAdapter recycleSearchesAdapter = null;
        public SearchTabItemRecyclerAdapter recycleWishesAdapter = null;

        private Button newSearch = null;
        private Button newWishlist = null;

        private TextView wishlistTitle = null;

        public static SearchTabDialog Instance = null;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.search_tab_layout, container); //error inflating MaterialButton
        }

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            Instance = this;
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            //Dialog.SetTitle("File Info"); //is this needed in any way??
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));
            this.SetStyle((int)Android.App.DialogFragmentStyle.NoTitle, 0);
            //this.Dialog.SetTitle("Search Tab");
            recyclerViewSearches = view.FindViewById<RecyclerView>(Resource.Id.searchesRecyclerView);
            recyclerViewSearches.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recyclerViewWishlists = view.FindViewById<RecyclerView>(Resource.Id.wishlistsRecyclerView);
            recyclerViewWishlists.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleSearchesLayoutManager = new LinearLayoutManager(this.Activity);
            recyclerViewSearches.SetLayoutManager(recycleSearchesLayoutManager);
            recycleWishlistsLayoutManager = new LinearLayoutManager(this.Activity);
            recyclerViewWishlists.SetLayoutManager(recycleWishlistsLayoutManager);
            recycleSearchesAdapter = new SearchTabItemRecyclerAdapter(GetSearchTabIds());
            var wishTabIds = GetWishesTabIds();
            recycleWishesAdapter = new SearchTabItemRecyclerAdapter(wishTabIds);
            recycleWishesAdapter.ForWishlist = true;

            wishlistTitle = view.FindViewById<TextView>(Resource.Id.wishlistTitle);
            if (wishTabIds.Count == 0)
            {
                wishlistTitle.SetText(Resource.String.wishlist_empty_bold);
            }
            else
            {
                wishlistTitle.SetText(Resource.String.wishlist_bold);
            }
            recyclerViewSearches.SetAdapter(recycleSearchesAdapter);
            recyclerViewWishlists.SetAdapter(recycleWishesAdapter);
            newSearch = view.FindViewById<Button>(Resource.Id.createNewSearch);
            newSearch.Click += NewSearch_Click;
            //newSearch.CompoundDrawablePadding = 6;
            Android.Graphics.Drawables.Drawable drawable = null;
            if (OperatingSystem.IsAndroidVersionAtLeast(21))
            {
                drawable = this.Context.Resources.GetDrawable(Resource.Drawable.ic_add_black_24dp, this.Context.Theme);
            }
            else
            {
                drawable = this.Context.Resources.GetDrawable(Resource.Drawable.ic_add_black_24dp);
            }
            newSearch.SetCompoundDrawablesWithIntrinsicBounds(drawable, null, null, null);

        }

        //private void NewWishlist_Click(object sender, EventArgs e)
        //{
        //    SearchTabHelper.AddWishlistSearchTab();
        //}

        private void NewSearch_Click(object sender, EventArgs e)
        {
            int tabID = SearchTabHelper.AddSearchTab();
            SearchFragment.Instance.GoToTab(tabID, false);
            SearchTabDialog.Instance.Dismiss();
            SearchFragment.Instance.SetCustomViewTabNumberImageViewState();
        }

        public override void OnResume()
        {
            base.OnResume();

            Logger.Debug("OnResume ran");
            //this.View.ViewTreeObserver.AddOnGlobalLayoutListener(this);
            //Window window = Dialog.Window;//  getDialog().getWindow();

            //int currentWindowHeight = window.DecorView.Height;
            //int currentWindowWidth = window.DecorView.Width;

            //int xxx = this.View.RootView.Width;
            //int xxxxx = this.View.Width;

            Dialog?.SetSizeProportional(.9, -1);

            Logger.Debug("OnResume End");
        }

        private List<int> GetSearchTabIds()
        {
            var listOFIds = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget != SearchTarget.Wishlist).Select((pair1) => pair1.Key).ToList();
            listOFIds.Sort();
            return listOFIds;
        }

        public static List<int> GetWishesTabIds()
        {
            var listOFIds = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget == SearchTarget.Wishlist).Select((pair1) => pair1.Key).ToList();
            listOFIds.Sort();
            listOFIds.Reverse();
            return listOFIds;
        }

        public void OnGlobalLayout()
        {
        }
    }

}