using NUnit.Framework;
using NUnit.Framework.Constraints;
using Soulseek;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Share;

namespace UnitTestCommon
{
    public class SearchUtilTest
    {
        [Test]
        public void FilterExcludedSearchPhrasesTest()
        {
            SearchUtil.ExcludedSearchPhrases = new List<string>() { "Homer Simpson", "Bart Simpson", "Lisa" };
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("homers diary"));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("homer_simpson diary"));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("Bart and Homer"));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile(@"homer folder//simpsons family//bart and homer file.txt"));
            Assert.IsTrue(SearchUtil.ShouldExcludeFile("homer Simpsons diary"));
            Assert.IsTrue(SearchUtil.ShouldExcludeFile("the homer Simpson full"));
            Assert.IsTrue(SearchUtil.ShouldExcludeFile(@"Full//LISAS diary"));

            SearchUtil.ExcludedSearchPhrases = new List<string>();
            Assert.IsFalse(SearchUtil.ShouldExcludeFile(""));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("test"));

            SearchUtil.ExcludedSearchPhrases = null;
            Assert.IsFalse(SearchUtil.ShouldExcludeFile(""));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("test"));
        }

        [Test]
        public void SearchReponseSplitShouldBeBasedOnEntireSubTree()
        {
            // do not split just based on direct parent (QT and nicotine use the full subtree)

            // this should be 4 results
            List<File> files = new List<File>()
            {
                new File(100, "@test\\Other Files\\Other\\FavoriteAlbum\\1.mp3", 1000, "mp3"),
                new File(100, "@test\\Other Files\\Other\\FavoriteAlbum\\2.mp3", 1000, "mp3"),
                new File(100, "@test\\Other Files\\Other\\FavoriteAlbum\\3.mp3", 1000, "mp3"),
                new File(100, "@test\\Other Files\\Other\\FavoriteAlbum\\4.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\1.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\2.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\3.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\4.mp3", 1000, "mp3"),
                new File(100, "@test\\Disco\\test.mp3", 1000, "mp3"),
                new File(100, "@test\\Literature\\test.epub", 1000, "epub"),
            };

            // this should be 3 results
            List<File> lockedFiles = new List<File>()
            {
                new File(100, "@test\\Locked Files\\Other\\FavoriteAlbum\\1.mp3", 1000, "mp3"),
                new File(100, "@test\\Locked Files\\Other\\FavoriteAlbum\\2.mp3", 1000, "mp3"),
                new File(100, "@test\\Locked Files\\Other\\FavoriteAlbum\\3.mp3", 1000, "mp3"),
                new File(100, "@test\\Locked Files\\Other\\FavoriteAlbum\\4.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\1.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\2.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\3.mp3", 1000, "mp3"),
                new File(100, "@test\\Music\\Other\\FavoriteAlbum\\4.mp3", 1000, "mp3"),
                new File(100, "@test\\Rock\\test.mp3", 1000, "mp3"),
            };
            var resp = new SearchResponse("username", 100, 100, 100, 100, files, lockedFiles);

            var result1 = Common.SearchResponseUtil.SplitMultiDirResponse(false, resp);
            Assert.AreEqual(7, result1.Item2.Count);

            var result2 = Common.SearchResponseUtil.SplitMultiDirResponse(true, resp);
            Assert.AreEqual(4, result2.Item2.Count);
        }
    }
}
