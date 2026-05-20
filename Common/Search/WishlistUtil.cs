using Seeker;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Common.Search
{
    public static class WishlistUtil
    {
        public static DateTime GetNextRunTime(DateTime lastWishlistTriggerTime, int intervalMilliSeconds, ConcurrentDictionary<int, SearchTab> searchTabs, SearchTab wishlistTab)
        {
            int tabsThatWillRunBefore = searchTabs.Count(it => it.Value.SearchTarget == SearchTarget.Wishlist && !IsFull(it.Value) && it.Value != wishlistTab && wishlistTab.LastRanTime.CompareTo(it.Value.LastRanTime) > 0);
            return lastWishlistTriggerTime.AddMilliseconds(intervalMilliSeconds * (tabsThatWillRunBefore + 1));
        }

        public const int MaxResultsPerWishlist = 2500;

        public static bool IsFull(SearchTab tab)
        {
            return tab != null && tab.LastSearchResultsCount >= MaxResultsPerWishlist;
        }
    }
}
