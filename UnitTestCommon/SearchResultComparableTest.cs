using NUnit.Framework;
using Soulseek;
using Common;
using Seeker;
using Seeker.Search;
using System.Collections.Generic;
using System.Linq;

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

        // ── Filename sorting ──

        [Test]
        public void Filename_ComparesFileNameNotEntirePath()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Filename);
            var a = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\ZZZ\AAA.mp3", 1000, "mp3") });
            var b = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\AAA\ZZZ.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(a, b), 0);
            Assert.Greater(comparer.Compare(b, a), 0);
        }

        [Test]
        public void Filename_IsCaseInsensitiveWithDeterministicOrdinalTieBreak()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Filename);
            var upper = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\Music\Alpha.mp3", 1000, "mp3") });
            var lower = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\Music\alpha.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(upper, lower), 0);
            Assert.Greater(comparer.Compare(lower, upper), 0);
            Assert.Less(comparer.Compare(upper, lower), 0, "comparison is deterministic");
        }

        [Test]
        public void Filename_FallsBackToLockedFile()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Filename);
            var a = MakeResponse("alice", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@alice\Music\AAA.mp3", 1000, "mp3") });
            var b = MakeResponse("bob", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@bob\Music\ZZZ.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(a, b), 0);
        }

        [Test]
        public void Filename_UnlockedRepresentativeWinsOverLockedEvenWhenLockedNameSortsFirst()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Filename);
            var unlocked = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\Music\ZZZ.mp3", 1000, "mp3") });
            var lockedOnly = MakeResponse("bob", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@bob\Music\AAA.mp3", 1000, "mp3") });

            // Different names decide first: AA comes before ZZ, even though one result is locked-only.
            Assert.Less(comparer.Compare(lockedOnly, unlocked), 0);
        }

        [Test]
        public void Filename_EmptyResponseSortsLast()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Filename);
            var empty = MakeResponse("empty", 0, 0, true,
                files: new List<File>(), lockedFiles: new List<File>());
            var nonEmpty = MakeResponse("alice", 100, 0, true);

            Assert.Less(comparer.Compare(nonEmpty, empty), 0);
            Assert.Greater(comparer.Compare(empty, nonEmpty), 0);
        }

        [Test]
        public void Filename_MixedLockedUnlockedSameNameStillDeterministic()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Filename);
            var unlocked = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\Music\song.mp3", 1000, "mp3") });
            var lockedOnly = MakeResponse("bob", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@bob\Music\song.mp3", 1000, "mp3") });

            // Same file name -> availability tie-break, which prefers unlocked.
            Assert.Less(comparer.Compare(unlocked, lockedOnly), 0);
            Assert.Greater(comparer.Compare(lockedOnly, unlocked), 0);
        }

        // ── File size sorting ──

        [Test]
        public void FileSize_LargerRepresentativeFileComesFirst()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FileSize);
            var large = MakeResponse("alice", 100, 0, true,
                files: new List<File> { new File(100, @"@alice\Music\large.flac", 9000, "flac") });
            var small = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\Music\small.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(large, small), 0);
            Assert.Greater(comparer.Compare(small, large), 0);
        }

        [Test]
        public void FileSize_UsesRepresentativeFirstFileNotAnotherFile()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FileSize);
            var firstSmall = MakeResponse("alice", 100, 0, true,
                files: new List<File>
                {
                    new File(100, @"@alice\Music\first.mp3", 1000, "mp3"),
                    new File(100, @"@alice\Music\second.flac", 50000, "flac"),
                });
            var medium = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\Music\medium.mp3", 2000, "mp3") });

            // The first (representative) file is 1 KB, so the 2 KB result sorts first.
            Assert.Less(comparer.Compare(medium, firstSmall), 0);
            Assert.Greater(comparer.Compare(firstSmall, medium), 0);
        }

        [Test]
        public void FileSize_FallsBackToLockedFile()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FileSize);
            var largeLocked = MakeResponse("alice", 100, 0, true,
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@alice\Music\large.flac", 9000, "flac") });
            var smallUnlocked = MakeResponse("bob", 100, 0, true,
                files: new List<File> { new File(100, @"@bob\Music\small.mp3", 1000, "mp3") });

            Assert.Less(comparer.Compare(largeLocked, smallUnlocked), 0);
        }

        [Test]
        public void FileSize_EmptyResponseSortsLast()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FileSize);
            var empty = MakeResponse("empty", 0, 0, true,
                files: new List<File>(), lockedFiles: new List<File>());
            var nonEmpty = MakeResponse("alice", 100, 0, true);

            Assert.Less(comparer.Compare(nonEmpty, empty), 0);
            Assert.Greater(comparer.Compare(empty, nonEmpty), 0);
        }

        [Test]
        public void Filename_10000Results_SortsByFileName()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Filename);
            var results = new List<SearchResponse>(10000);
            for (int i = 0; i < 10000; i++)
            {
                results.Add(MakeResponse("user" + i, 100, 0, true,
                    files: new List<File>
                    {
                        new File(100, $@"@user{i}\Music\track{9999 - i:D5}.mp3", 1000, "mp3")
                    }));
            }

            results.Sort(comparer);

            Assert.AreEqual(10000, results.Count);
            for (int i = 1; i < results.Count; i++)
            {
                string previous = SimpleHelpers.GetFileNameFromFile(results[i - 1].Files.First().Filename).ToString();
                string current = SimpleHelpers.GetFileNameFromFile(results[i].Files.First().Filename).ToString();
                Assert.LessOrEqual(string.Compare(previous, current, System.StringComparison.OrdinalIgnoreCase), 0);
            }
        }

        [Test]
        public void FileSize_10000Results_SortsDescendingByRepresentativeSize()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.FileSize);
            var results = new List<SearchResponse>(10000);
            for (int i = 0; i < 10000; i++)
            {
                results.Add(MakeResponse("user" + i, 100, 0, true,
                    files: new List<File>
                    {
                        new File(100, $@"@user{i}\Music\track.mp3", i + 1, "mp3")
                    }));
            }

            results.Sort(comparer);

            Assert.AreEqual(10000, results.Count);
            Assert.AreEqual(10000, results[0].Files.First().Size);
            Assert.AreEqual(1, results[results.Count - 1].Files.First().Size);
            for (int i = 1; i < results.Count; i++)
            {
                Assert.GreaterOrEqual(
                    results[i - 1].Files.First().Size,
                    results[i].Files.First().Size);
            }
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
        public void Fastest_10000Results_SortsDescendingBySpeed()
        {
            var comparer = new SearchResultComparable(SearchResultSorting.Fastest);
            var results = new List<SearchResponse>(10000);
            for (int i = 0; i < 10000; i++)
            {
                results.Add(MakeResponse("user" + i, uploadSpeed: i, queueLength: 0, hasFreeUploadSlot: true));
            }

            results.Sort(comparer);

            Assert.AreEqual(10000, results.Count);
            Assert.AreEqual(9999, results[0].UploadSpeed);
            Assert.AreEqual(0, results[results.Count - 1].UploadSpeed);
            for (int i = 1; i < results.Count; i++)
            {
                Assert.Greater(results[i - 1].UploadSpeed, results[i].UploadSpeed);
            }
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
