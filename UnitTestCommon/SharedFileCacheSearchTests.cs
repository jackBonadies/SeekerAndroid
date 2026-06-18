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
