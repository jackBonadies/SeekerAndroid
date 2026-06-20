using Common.Share;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitTestCommon
{
    /// <summary>
    /// Tests for the surgical (no disk re-walk) shared-folder removal in <see cref="SharedFileCache.WithFolderRemoved"/>.
    /// The headline guarantee is the equivalence test: removing root "Music" from a {Music, Pics} cache produces a
    /// cache that serializes byte-for-byte identically to one freshly built from only "Pics".
    /// </summary>
    public class SharedFileCacheRemovalTests
    {
        private static Tuple<long, string, Tuple<int, int, int, int>, bool, bool> Info(long size, bool locked = false, bool hidden = false)
            => Tuple.Create(size, "content://uri/" + size, Tuple.Create(-1, -1, -1, -1), locked, hidden);

        private static Soulseek.Directory Dir(string name, params (string fname, long size)[] files)
            => new Soulseek.Directory(name, files.Select(f => new Soulseek.File(1, f.fname, f.size, Path.GetExtension(f.fname))).ToList());

        private static SharedFileCache Build(
            List<KeyValuePair<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>> files,
            List<Soulseek.Directory> dirs,
            List<Soulseek.Directory> lockedDirs,
            List<Soulseek.Directory> hiddenDirs,
            List<Tuple<string, string>> mapping)
        {
            var dict = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
            foreach (var f in files)
            {
                dict[f.Key] = f.Value;
            }
            var helper = SharedFileCache.GenerateFileKeyToPresentableNameIndex(dict);
            var token = SharedFileCache.GenerateSearchTermTokenToFileKeysIndex(dict, helper);
            var browse = new BrowseResponse(dirs, lockedDirs);
            int nonHidden = dict.Count(p => !p.Value.Item5);
            return new SharedFileCache(dict, browse.DirectoryCount, browse, mapping, token, helper, hiddenDirs, nonHidden);
        }

        // {Music, Pics} fixture: Music has a plain subdir, a locked subdir, and a locked file;
        // Pics has a plain root and a hidden subdir + hidden file.
        private static SharedFileCache BuildMusicAndPics()
        {
            var files = new List<KeyValuePair<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>>
            {
                new("Music\\a.mp3", Info(10)),
                new("Music\\Album\\b.mp3", Info(20)),
                new("Music\\Locked\\c.mp3", Info(30, locked: true)),
                new("Pics\\x.jpg", Info(40)),
                new("Pics\\Hidden\\y.jpg", Info(50, hidden: true)),
            };
            var dirs = new List<Soulseek.Directory>
            {
                Dir("Music", ("a.mp3", 10)),
                Dir("Music\\Album", ("b.mp3", 20)),
                Dir("Pics", ("x.jpg", 40)),
            };
            var lockedDirs = new List<Soulseek.Directory> { Dir("Music\\Locked", ("c.mp3", 30)) };
            var hiddenDirs = new List<Soulseek.Directory> { Dir("Pics\\Hidden", ("y.jpg", 50)) };
            var mapping = new List<Tuple<string, string>>
            {
                Tuple.Create("Music", "uriMusic"),
                Tuple.Create("Music\\Album", "uriAlbum"),
                Tuple.Create("Music\\Locked", "uriLocked"),
                Tuple.Create("Pics", "uriPics"),
                Tuple.Create("Pics\\Hidden", "uriHidden"),
            };
            return Build(files, dirs, lockedDirs, hiddenDirs, mapping);
        }

        // The same survivors a fresh parse of only "Pics" would produce — same insertion order as the post-removal dict.
        private static SharedFileCache BuildPicsOnly()
        {
            var files = new List<KeyValuePair<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>>
            {
                new("Pics\\x.jpg", Info(40)),
                new("Pics\\Hidden\\y.jpg", Info(50, hidden: true)),
            };
            var dirs = new List<Soulseek.Directory> { Dir("Pics", ("x.jpg", 40)) };
            var lockedDirs = new List<Soulseek.Directory>();
            var hiddenDirs = new List<Soulseek.Directory> { Dir("Pics\\Hidden", ("y.jpg", 50)) };
            var mapping = new List<Tuple<string, string>>
            {
                Tuple.Create("Pics", "uriPics"),
                Tuple.Create("Pics\\Hidden", "uriHidden"),
            };
            return Build(files, dirs, lockedDirs, hiddenDirs, mapping);
        }

        [Test]
        public void RemoveRoot_FiltersFilesDirsMappingAndCounts()
        {
            var cache = BuildMusicAndPics();
            var result = cache.WithFolderRemoved("Music", out bool anythingRemoved);

            Assert.IsTrue(anythingRemoved);

            // No surviving file key belongs to "Music"; all "Pics" keys remain.
            CollectionAssert.AreEquivalent(
                new[] { "Pics\\x.jpg", "Pics\\Hidden\\y.jpg" },
                result.PresentableNameToFullFileInfo.Keys);

            var cpr = result.ToCachedParseResults();

            // Directories / locked / hidden: nothing named "Music" or under "Music\".
            CollectionAssert.AreEqual(new[] { "Pics" }, cpr.BrowseResponse.Directories.Select(d => d.Name));
            CollectionAssert.IsEmpty(cpr.BrowseResponse.LockedDirectories);
            CollectionAssert.AreEqual(new[] { "Pics\\Hidden" }, cpr.BrowseResponseHiddenPortion.Select(d => d.Name));

            // Mapping filtered likewise.
            CollectionAssert.AreEquivalent(
                new[] { "Pics", "Pics\\Hidden" },
                cpr.PresentableDirectoryNameToDirectoryUriMappings.Select(t => t.Item1));

            // DirectoryCount == Directories.Count; NonHidden excludes the hidden Pics file.
            Assert.AreEqual(1, result.DirectoryCount);
            Assert.AreEqual(1, result.GetNonHiddenFileCountForServer());

            // Original cache is untouched (new instance returned).
            Assert.AreEqual(5, cache.FileCount);
        }

        [Test]
        public void RemoveRoot_RegeneratesContiguousIndicesAndSearch()
        {
            var cache = BuildMusicAndPics();
            var result = cache.WithFolderRemoved("Music", out _);
            var cpr = result.ToCachedParseResults();

            // HelperIndex keys are contiguous 0..n-1 and its values equal the surviving file keys.
            CollectionAssert.AreEqual(new[] { 0, 1 }, cpr.FileKeyToPresentableName.Keys.OrderBy(k => k));
            CollectionAssert.AreEquivalent(
                result.PresentableNameToFullFileInfo.Keys,
                cpr.FileKeyToPresentableName.Values);

            // Every token-index code points at a valid HelperIndex key.
            foreach (var codes in cpr.SearchTermTokenToListOfFileKeys.Values)
            {
                foreach (int code in codes)
                {
                    Assert.IsTrue(cpr.FileKeyToPresentableName.ContainsKey(code), $"dangling token code {code}");
                }
            }

            // A term only present under the removed root no longer resolves; a surviving term still does.
            var goneByTerm = result.Search(new SearchQuery("a"), "someuser", out _);
            Assert.IsEmpty(goneByTerm);
            var kept = result.Search(new SearchQuery("x"), "someuser", out _).ToList();
            CollectionAssert.AreEqual(new[] { "Pics\\x.jpg" }, kept.Select(f => f.Filename));
        }

        [Test]
        public void RemoveRoot_EquivalentToFreshBuild()
        {
            var removed = BuildMusicAndPics().WithFolderRemoved("Music", out _);
            var fresh = BuildPicsOnly();
            AssertSerializedEqual(removed.ToCachedParseResults(), fresh.ToCachedParseResults());
        }

        [Test]
        public void RemoveNonexistentPrefix_NothingRemoved()
        {
            var cache = BuildMusicAndPics();
            var result = cache.WithFolderRemoved("DoesNotExist", out bool anythingRemoved);

            Assert.IsFalse(anythingRemoved);
            CollectionAssert.AreEquivalent(
                cache.PresentableNameToFullFileInfo.Keys,
                result.PresentableNameToFullFileInfo.Keys);
        }

        [Test]
        public void RemoveEmptyFolder_DirMatchedButNoFiles_StillReportsRemoved()
        {
            // An empty shared folder contributes a directory entry but no file-dict keys.
            var files = new List<KeyValuePair<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>>
            {
                new("Pics\\x.jpg", Info(40)),
            };
            var dirs = new List<Soulseek.Directory>
            {
                Dir("Empty"),
                Dir("Pics", ("x.jpg", 40)),
            };
            var mapping = new List<Tuple<string, string>>
            {
                Tuple.Create("Empty", "uriEmpty"),
                Tuple.Create("Pics", "uriPics"),
            };
            var cache = Build(files, dirs, new List<Soulseek.Directory>(), new List<Soulseek.Directory>(), mapping);

            var result = cache.WithFolderRemoved("Empty", out bool anythingRemoved);

            Assert.IsTrue(anythingRemoved, "a matched (empty) directory should count as removed even with no files");
            CollectionAssert.AreEqual(new[] { "Pics" }, result.ToCachedParseResults().BrowseResponse.Directories.Select(d => d.Name));
            Assert.AreEqual(1, result.FileCount);
        }

        [Test]
        public void RemovePrefixCollision_DoesNotRemoveSiblingWithSharedStringPrefix()
        {
            // "Mus" must not match "Music\.." — the '\' separator guards against substring-prefix collisions.
            var files = new List<KeyValuePair<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>>
            {
                new("Mus\\a.mp3", Info(10)),
                new("Music\\b.mp3", Info(20)),
            };
            var dirs = new List<Soulseek.Directory>
            {
                Dir("Mus", ("a.mp3", 10)),
                Dir("Music", ("b.mp3", 20)),
            };
            var mapping = new List<Tuple<string, string>>
            {
                Tuple.Create("Mus", "uriMus"),
                Tuple.Create("Music", "uriMusic"),
            };
            var cache = Build(files, dirs, new List<Soulseek.Directory>(), new List<Soulseek.Directory>(), mapping);

            var result = cache.WithFolderRemoved("Mus", out bool anythingRemoved);

            Assert.IsTrue(anythingRemoved);
            CollectionAssert.AreEqual(new[] { "Music\\b.mp3" }, result.PresentableNameToFullFileInfo.Keys);
            CollectionAssert.AreEqual(new[] { "Music" }, result.ToCachedParseResults().BrowseResponse.Directories.Select(d => d.Name));
        }

        private static void AssertSerializedEqual(CachedParseResults a, CachedParseResults b)
        {
            var dirA = Path.Combine(Path.GetTempPath(), "sfc_a_" + Guid.NewGuid().ToString("N"));
            var dirB = Path.Combine(Path.GetTempPath(), "sfc_b_" + Guid.NewGuid().ToString("N"));
            using (var pa = new TestCacheDataProvider(dirA, ownsDirectory: true))
            using (var pb = new TestCacheDataProvider(dirB, ownsDirectory: true))
            {
                Seeker.Serialization.CachedParseResultsSerializer.Store(pa, a);
                Seeker.Serialization.CachedParseResultsSerializer.Store(pb, b);

                var filesA = System.IO.Directory.GetFiles(dirA).Select(Path.GetFileName).OrderBy(x => x).ToList();
                var filesB = System.IO.Directory.GetFiles(dirB).Select(Path.GetFileName).OrderBy(x => x).ToList();
                CollectionAssert.AreEqual(filesA, filesB, "serialized file set differs");

                foreach (var f in filesA)
                {
                    var bytesA = System.IO.File.ReadAllBytes(Path.Combine(dirA, f));
                    var bytesB = System.IO.File.ReadAllBytes(Path.Combine(dirB, f));
                    CollectionAssert.AreEqual(bytesA, bytesB, $"serialized bytes differ for {f}");
                }
            }
        }
    }
}
