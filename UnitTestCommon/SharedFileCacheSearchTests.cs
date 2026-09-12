using Common.Share;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    /// <summary>
    /// Tests for term matching
    /// </summary>
    public class SharedFileCacheSearchTests
    {
        private static Tuple<long, string, Tuple<int, int, int, int>, bool, bool> Info(long size)
            => Tuple.Create(size, "content://uri/" + size, Tuple.Create(-1, -1, -1, -1), false, false);

        private static SharedFileCache BuildCache(params string[] presentableKeys)
        {
            var dict = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
            long size = 10;
            foreach (var key in presentableKeys)
            {
                dict[key] = Info(size);
                size += 10;
            }
            var helper = SharedFileCache.GenerateFileKeyToPresentableNameIndex(dict);
            var token = SharedFileCache.GenerateSearchTermTokenToFileKeysIndex(dict, helper);
            var browse = new BrowseResponse(new List<Soulseek.Directory>(), new List<Soulseek.Directory>());
            var cache = new SharedFileCache(dict, browse.DirectoryCount, browse,
                new List<Tuple<string, string>>(), token, helper, new List<Soulseek.Directory>(), dict.Count);
            cache.SuccessfullyInitialized = true;
            return cache;
        }

        [Test]
        public void Search_ExcludeTerm_FiltersOutMatchingFiles()
        {
            var cache = BuildCache("Music\\love song.mp3", "Music\\love ballad.mp3");

            // sanity: a bare term matches both files whose name contains it.
            var both = cache.Search(new SearchQuery("love"), "someuser", out _).Select(f => f.Filename).ToList();
            CollectionAssert.AreEquivalent(new[] { "Music\\love song.mp3", "Music\\love ballad.mp3" }, both);

            // the exclusion must drop the file whose name contains the excluded token.
            var filtered = cache.Search(new SearchQuery("love -ballad"), "someuser", out _).Select(f => f.Filename).ToList();
            CollectionAssert.AreEqual(new[] { "Music\\love song.mp3" }, filtered);
        }

        // The Soulseek server does not normalize query text, and the indexed tokens are lowercased
        // with punctuation stripped so an incoming term must go through the exact same
        // normalization or a search that differs only in case / punctuation silently returns nothing.

        [TestCase("love")]
        [TestCase("Love")]
        [TestCase("LOVE SONG")]
        [TestCase("lOvE sOnG")]
        public void Search_IsCaseInsensitive(string query)
        {
            var cache = BuildCache("Music\\Love Song.mp3");

            var hits = cache.Search(new SearchQuery(query), "someuser", out _).Select(f => f.Filename).ToList();
            CollectionAssert.AreEqual(new[] { "Music\\Love Song.mp3" }, hits);
        }

        [Test]
        public void Search_ExcludeTerm_IsCaseInsensitive()
        {
            var cache = BuildCache("Music\\love song.mp3", "Music\\love ballad.mp3");

            var filtered = cache.Search(new SearchQuery("love -BALLAD"), "someuser", out _).Select(f => f.Filename).ToList();
            CollectionAssert.AreEqual(new[] { "Music\\love song.mp3" }, filtered);
        }

        [TestCase("Live")]
        [TestCase("(Live)")]
        [TestCase("(LIVE)")]
        [TestCase("show, live")]
        public void Search_QueryPunctuation_IsStrippedLikeTheIndex(string query)
        {
            var cache = BuildCache("Music\\Show (Live).mp3");

            var hits = cache.Search(new SearchQuery(query), "someuser", out _).Select(f => f.Filename).ToList();
            CollectionAssert.AreEqual(new[] { "Music\\Show (Live).mp3" }, hits);
        }

        [Test]
        public void Search_TurkishCulture_CaseFoldsInvariantly()
        {
            // culture-sensitive ToLower maps 'I' to dotless 'ı' under tr-TR; both sides must fold the same way
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
                var cache = BuildCache("Music\\Idol.mp3");

                var hits = cache.Search(new SearchQuery("IDOL"), "someuser", out _).Select(f => f.Filename).ToList();
                CollectionAssert.AreEqual(new[] { "Music\\Idol.mp3" }, hits);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void Search_ExcludeTerm_CanRemoveAllMatches()
        {
            var cache = BuildCache("Music\\love song.mp3", "Music\\love ballad.mp3");

            // "song" matches only the first file, but excluding the term they share ("love") drops it.
            var none = cache.Search(new SearchQuery("song -love"), "someuser", out _).ToList();
            CollectionAssert.IsEmpty(none);
        }
    }
}
