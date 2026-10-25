namespace Seeker
{
    /// <summary>
    /// Single source of truth for the app shell's pager pages and the bottom
    /// navigation order.
    /// </summary>
    /// <remarks>
    /// The legacy page indices are a compatibility contract: notifications,
    /// deep links and saved state all target 1 = Search, 2 = Transfers,
    /// 3 = Browse and 0 = Account/login. Messages is appended as page 4 so no
    /// existing index moves. Account stays a real pager page (so login/global
    /// actions keep their state) but is deliberately not a bottom-nav item.
    /// </remarks>
    public static class MainNavigation
    {
        public const int AccountPage = 0;
        public const int SearchPage = 1;
        public const int TransfersPage = 2;
        public const int BrowsePage = 3;
        public const int MessagesPage = 4;
        public const int PageCount = 5;

        /// <summary>Where a fresh activity (no saved page, no deep link) lands.</summary>
        public const int DefaultLandingPage = SearchPage;

        /// <summary>The bottom-navigation order: Search | Transfers | Browse | Messages.</summary>
        public static readonly int[] BottomMenuPages =
        {
            SearchPage,
            TransfersPage,
            BrowsePage,
            MessagesPage,
        };

        public const string CurrentPageStateKey = "seeker.main.currentPage";
        public const string LastPrimaryPageStateKey = "seeker.main.lastPrimaryPage";

        /// <summary>Primary destinations are the four bottom-navigation pages.</summary>
        public static bool IsPrimaryDestination(int page)
        {
            switch (page)
            {
                case SearchPage:
                case TransfersPage:
                case BrowsePage:
                case MessagesPage:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Restores a saved pager page. Unknown values (older/newer builds,
        /// corrupted state) fall back to the default landing page instead of
        /// leaving ViewPager2 on an invalid index.
        /// </summary>
        public static int NormalizeRestoredPage(int page)
        {
            return page >= AccountPage && page < PageCount ? page : DefaultLandingPage;
        }

        /// <summary>Maps a bottom-navigation menu id to its pager page, or -1.</summary>
        public static int PageForMenuItem(int itemId)
        {
            switch (itemId)
            {
                case Resource.Id.navigation_search:
                    return SearchPage;
                case Resource.Id.navigation_transfers:
                    return TransfersPage;
                case Resource.Id.navigation_browse:
                    return BrowsePage;
                case Resource.Id.navigation_messages:
                    return MessagesPage;
                default:
                    return -1;
            }
        }

        /// <summary>Maps a pager page to its bottom-navigation menu id, or 0.</summary>
        public static int MenuItemForPage(int page)
        {
            switch (page)
            {
                case SearchPage:
                    return Resource.Id.navigation_search;
                case TransfersPage:
                    return Resource.Id.navigation_transfers;
                case BrowsePage:
                    return Resource.Id.navigation_browse;
                case MessagesPage:
                    return Resource.Id.navigation_messages;
                default:
                    return 0;
            }
        }
    }
}
