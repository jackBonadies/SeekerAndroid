using Seeker;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Common.Search
{
    public static class WishlistUtil
    {
        public static DateTime GetNextRunTime(DateTime lastWishlistRun, int intervalMilliSeconds, ConcurrentDictionary<int, SearchTab> wishlistTabs, SearchTab wishlistTab)
        {
            int tabsThatWillRunBefore = wishlistTabs.Count(it => it.Value != wishlistTab && lastWishlistRun.CompareTo(it.Value.LastRanTime) > 0);
            return lastWishlistRun.AddMilliseconds(intervalMilliSeconds * (tabsThatWillRunBefore + 1));
        }
    }
}
