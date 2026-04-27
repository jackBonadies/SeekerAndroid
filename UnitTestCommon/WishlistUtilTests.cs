using NUnit.Framework;
using Seeker;
using Common.Search;
using System;
using System.Collections.Concurrent;

namespace UnitTestCommon
{
    public class WishlistUtilTests
    {
        private static ConcurrentDictionary<int, SearchTab> Build(SearchTab self, params (int id, DateTime lastRan)[] others)
        {
            var dict = new ConcurrentDictionary<int, SearchTab>();
            dict[1] = self;
            foreach (var (id, lastRan) in others)
            {
                dict[id] = new SearchTab { LastRanTime = lastRan, SearchTarget = SearchTarget.Wishlist };
            }
            return dict;
        }

        [Test]
        public void NoOtherTabs_NextRunIsOneIntervalAfterLastRun()
        {
            DateTime lastRun = new DateTime(2026, 1, 1, 12, 0, 0);
            SearchTab self = new SearchTab { LastRanTime = lastRun };
            var tabs = Build(self);

            DateTime next = WishlistUtil.GetNextRunTime(lastRun, 60_000, tabs, self);

            Assert.AreEqual(lastRun.AddSeconds(60), next);
        }

        [Test]
        public void OlderTabs_QueueAheadAndShiftNextRun()
        {
            DateTime lastRun = new DateTime(2026, 1, 1, 12, 0, 0);
            SearchTab self = new SearchTab { LastRanTime = lastRun };
            var tabs = Build(self,
                (2, lastRun.AddSeconds(-30)),
                (3, lastRun.AddSeconds(-90)));

            DateTime next = WishlistUtil.GetNextRunTime(lastRun, 60_000, tabs, self);

            // 2 tabs ahead + self => 3 intervals after lastRun
            Assert.AreEqual(lastRun.AddSeconds(180), next);
        }

        [Test]
        public void NewerTabs_AreNotCounted()
        {
            DateTime lastRun = new DateTime(2026, 1, 1, 12, 0, 0);
            SearchTab self = new SearchTab { LastRanTime = lastRun };
            var tabs = Build(self,
                (2, lastRun.AddSeconds(30)),
                (3, lastRun.AddSeconds(120)));

            DateTime next = WishlistUtil.GetNextRunTime(lastRun, 60_000, tabs, self);

            Assert.AreEqual(lastRun.AddSeconds(60), next);
        }

        [Test]
        public void MixedOlderAndNewerTabs_OnlyOlderShiftNextRun()
        {
            DateTime lastRun = new DateTime(2026, 1, 1, 12, 0, 0);
            SearchTab self = new SearchTab { LastRanTime = lastRun };
            var tabs = Build(self,
                (2, lastRun.AddSeconds(-10)),
                (3, lastRun.AddSeconds(-200)),
                (4, lastRun.AddSeconds(45)),
                (5, lastRun.AddSeconds(300)));

            DateTime next = WishlistUtil.GetNextRunTime(lastRun, 60_000, tabs, self);

            // 2 older tabs ahead + self => 3 intervals
            Assert.AreEqual(lastRun.AddSeconds(180), next);
        }

        [Test]
        public void SelfIsExcludedEvenWhenLastRanTimeMatchesAnotherTab()
        {
            DateTime lastRun = new DateTime(2026, 1, 1, 12, 0, 0);
            SearchTab self = new SearchTab { LastRanTime = lastRun };
            // Another tab with the exact same LastRanTime — must not count as "older"
            // (strict greater-than) and self must be filtered by reference, not by time.
            var tabs = Build(self, (2, lastRun));

            DateTime next = WishlistUtil.GetNextRunTime(lastRun, 60_000, tabs, self);

            Assert.AreEqual(lastRun.AddSeconds(60), next);
        }

        [Test]
        public void IntervalIsApplied()
        {
            DateTime lastRun = new DateTime(2026, 1, 1, 12, 0, 0);
            SearchTab self = new SearchTab { LastRanTime = lastRun };
            var tabs = Build(self, (2, lastRun.AddSeconds(-1)));

            DateTime next = WishlistUtil.GetNextRunTime(lastRun, 300_000, tabs, self);

            Assert.AreEqual(lastRun.AddSeconds(600), next);
        }
    }
}
