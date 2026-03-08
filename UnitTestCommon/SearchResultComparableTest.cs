using NUnit.Framework;
using Soulseek;
using Common;
using Seeker;
using Seeker.Search;
using System.Collections.Generic;

namespace UnitTestCommon
{
    public class SearchResultComparableTest
    {
        private static SearchResponse MakeResponse(
            string username,
            int uploadSpeed,
            int queueLength,
            bool hasFreeUploadSlot,
            List<File> files = null,
            List<File> lockedFiles = null)
        {
            return new SearchResponse(
                username,
                token: 1,
                hasFreeUploadSlot: hasFreeUploadSlot,
                uploadSpeed: uploadSpeed,
                queueLength: queueLength,
                fileList: files ?? new List<File>
                {
                    new File(100, $@"@{username}\Music\song.mp3", 1000, "mp3")
                },
                lockedFileList: lockedFiles ?? new List<File>());
        }

        // ── Available sorting ──

        [Test]
        public void Available_UnlockedFilesBeatLockedOnly()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Available);
            var hasUnlocked = MakeResponse("alice", 100, 0, true);
            var lockedOnly = MakeResponse("bob", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File>
                {
                    new File(100, @"@bob\Music\song.mp3", 1000, "mp3")
                });

            Assert.Less(comparer.Compare(hasUnlocked, lockedOnly), 0);
            Assert.Greater(comparer.Compare(lockedOnly, hasUnlocked), 0);
        }

        [Test]
        public void Available_FreeSlotBeatsBusy()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Available);
            var free = MakeResponse("alice", 100, 0, hasFreeUploadSlot: true);
            var busy = MakeResponse("bob", 100, 0, hasFreeUploadSlot: false);

            Assert.Less(comparer.Compare(free, busy), 0);
            Assert.Greater(comparer.Compare(busy, free), 0);
        }

        [Test]
        public void Available_ShorterQueueBeatLonger()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Available);
            var shortQueue = MakeResponse("alice", 100, queueLength: 2, hasFreeUploadSlot: true);
            var longQueue = MakeResponse("bob", 100, queueLength: 10, hasFreeUploadSlot: true);

            Assert.Less(comparer.Compare(shortQueue, longQueue), 0);
            Assert.Greater(comparer.Compare(longQueue, shortQueue), 0);
        }

        [Test]
        public void Available_HigherSpeedBeatsLower()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Available);
            var fast = MakeResponse("alice", uploadSpeed: 5000, queueLength: 0, hasFreeUploadSlot: true);
            var slow = MakeResponse("bob", uploadSpeed: 100, queueLength: 0, hasFreeUploadSlot: true);

            Assert.Less(comparer.Compare(fast, slow), 0);
            Assert.Greater(comparer.Compare(slow, fast), 0);
        }

        [Test]
        public void Available_PrecedenceOrder_FileCountBeforeFreeSlot()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Available);
            // locked-only but free slot should still lose to unlocked + busy
            var unlockedBusy = MakeResponse("alice", 100, 0, hasFreeUploadSlot: false);
            var lockedFree = MakeResponse("bob", 100, 0, hasFreeUploadSlot: true,
                files: new List<File>(),
                lockedFiles: new List<File>
                {
                    new File(100, @"@bob\Music\song.mp3", 1000, "mp3")
                });

            Assert.Less(comparer.Compare(unlockedBusy, lockedFree), 0);
        }

        [Test]
        public void Available_EqualStats_FallsBackToFilename()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Available);
            var a = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\AAA\song.mp3", 1000, "mp3") });
            var b = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\ZZZ\song.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(a, b), 0);
            Assert.Greater(comparer.Compare(b, a), 0);
        }

        [Test]
        public void Available_IdenticalResponses_ReturnsZero()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Available);
            var files = new List<File> { new File(100, @"@user\Music\song.mp3", 1000, "mp3") };
            var x = MakeResponse("user", 100, 0, true, files: files);
            var y = MakeResponse("user", 100, 0, true, files: files);

            Assert.AreEqual(0, comparer.Compare(x, y));
        }

        // ── Fastest sorting ──

        [Test]
        public void Fastest_OnlySpeedMatters()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Fastest);
            var fast = MakeResponse("alice", uploadSpeed: 9000, queueLength: 100, hasFreeUploadSlot: false);
            var slow = MakeResponse("bob", uploadSpeed: 1, queueLength: 0, hasFreeUploadSlot: true);

            Assert.Less(comparer.Compare(fast, slow), 0);
        }

        [Test]
        public void Fastest_LockedFilesStillRanked()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Fastest);
            var lockedFast = MakeResponse("alice", uploadSpeed: 9000, queueLength: 0, hasFreeUploadSlot: true,
                files: new List<File>(),
                lockedFiles: new List<File>
                {
                    new File(100, @"@alice\Music\song.mp3", 1000, "mp3")
                });
            var unlockedSlow = MakeResponse("bob", uploadSpeed: 1, queueLength: 0, hasFreeUploadSlot: true);

            Assert.Less(comparer.Compare(lockedFast, unlockedSlow), 0);
        }

        [Test]
        public void Fastest_EqualSpeed_FallsBackToFilename()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Fastest);
            var a = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\AAA\song.mp3", 1000, "mp3") });
            var b = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\ZZZ\song.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(a, b), 0);
        }

        // ── FolderAlphabetical sorting ──

        [Test]
        public void FolderAlphabetical_SortsByFolderName()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FolderAlphabetical);
            var a = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\Alpha\song.mp3", 1000, "mp3") });
            var b = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\Zebra\song.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(a, b), 0);
            Assert.Greater(comparer.Compare(b, a), 0);
        }

        [Test]
        public void FolderAlphabetical_SameFolderTiebreaksByUsername()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FolderAlphabetical);
            var a = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\SameFolder\song.mp3", 1000, "mp3") });
            var b = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\SameFolder\song.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(a, b), 0);
            Assert.Greater(comparer.Compare(b, a), 0);
        }

        [Test]
        public void FolderAlphabetical_UsesLockedFilesWhenNoUnlocked()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FolderAlphabetical);
            var a = MakeResponse("alice", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@alice\Alpha\song.mp3", 1000, "mp3") });
            var b = MakeResponse("bob", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@bob\Zebra\song.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(a, b), 0);
        }

        // ── Wishlist comparer ──

        [Test]
        public void Wishlist_SameUserSameFiles_ReturnsZero()
        {
            var comparer = new SearchResultComparableWishlist(SearchResultSorting.Available);
            var files = new List<File> { new File(100, @"@user\Music\song.mp3", 1000, "mp3") };
            var x = new SearchResponse("user", 1, true, 100, 0, files);
            var y = new SearchResponse("user", 1, true, 50, 0, files);

            Assert.AreEqual(0, comparer.Compare(x, y));
        }

        [Test]
        public void Wishlist_SameUserDifferentFiles_FallsBackToBase()
        {
            var comparer = new SearchResultComparableWishlist(SearchResultSorting.Available);
            var filesA = new List<File> { new File(100, @"@user\Music\songA.mp3", 1000, "mp3") };
            var filesB = new List<File> { new File(100, @"@user\Music\songB.mp3", 1000, "mp3") };
            var fast = new SearchResponse("user", 1, true, 5000, 0, filesA);
            var slow = new SearchResponse("user", 1, true, 100, 0, filesB);

            // different files so should fall through to base compare (speed)
            Assert.Less(comparer.Compare(fast, slow), 0);
        }

        [Test]
        public void Wishlist_DifferentUsers_FallsBackToBase()
        {
            var comparer = new SearchResultComparableWishlist(SearchResultSorting.Available);
            var fast = MakeResponse("alice", uploadSpeed: 5000, queueLength: 0, hasFreeUploadSlot: true);
            var slow = MakeResponse("bob", uploadSpeed: 100, queueLength: 0, hasFreeUploadSlot: true);

            Assert.Less(comparer.Compare(fast, slow), 0);
        }

        [Test]
        public void Wishlist_SameUserSameLockedFiles_ReturnsZero()
        {
            var comparer = new SearchResultComparableWishlist(SearchResultSorting.Available);
            var locked = new List<File> { new File(100, @"@user\Music\song.mp3", 1000, "mp3") };
            var x = new SearchResponse("user", 1, true, 100, 0, new List<File>(), locked);
            var y = new SearchResponse("user", 1, true, 50, 0, new List<File>(), locked);

            Assert.AreEqual(0, comparer.Compare(x, y));
        }

        [Test]
        public void TwoResponsesWithDifferentUsernamesShouldNotThrowArgumentExceptionForDuplicates()
        {
            var comparer = new SearchResultComparableWishlist(SearchResultSorting.Available);
            var sortedDict = new SortedDictionary<SearchResponse, object>(comparer);
            var files = new List<File> { new File(100, @"@user\Music\songA.mp3", 1000, "mp3") };
            var userA = new SearchResponse("userA", 1, true, 5000, 0, files);
            var userB = new SearchResponse("userB", 1, true, 5000, 0, files);
            sortedDict.Add(userA, null);
            sortedDict.Add(userB, null);
            Assert.AreEqual(2, sortedDict.Count);
            comparer = new SearchResultComparableWishlist(SearchResultSorting.FolderAlphabetical);
            sortedDict = new SortedDictionary<SearchResponse, object>(comparer);
            sortedDict.Add(userA, null);
            sortedDict.Add(userB, null);
            Assert.AreEqual(2, sortedDict.Count);
            comparer = new SearchResultComparableWishlist(SearchResultSorting.Fastest);
            sortedDict = new SortedDictionary<SearchResponse, object>(comparer);
            sortedDict.Add(userA, null);
            sortedDict.Add(userB, null);
            Assert.AreEqual(2, sortedDict.Count);
            comparer = new SearchResultComparableWishlist(SearchResultSorting.BitRate);
            sortedDict = new SortedDictionary<SearchResponse, object>(comparer);
            sortedDict.Add(userA, null);
            sortedDict.Add(userB, null);
            Assert.AreEqual(2, sortedDict.Count);
        }
    }
}
