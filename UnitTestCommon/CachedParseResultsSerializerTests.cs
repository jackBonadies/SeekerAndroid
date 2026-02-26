using NUnit.Framework;
using Seeker;
using Seeker.Serialization;
using Soulseek;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VerifyNUnit;

namespace UnitTestCommon
{
    internal class TestCacheDataProvider : ICacheDataProvider, IDisposable
    {
        private readonly string _dir;
        private readonly bool _ownsDirectory;
        private int _fileCount = -1;

        public TestCacheDataProvider(string dir, bool ownsDirectory = false)
        {
            _dir = dir;
            _ownsDirectory = ownsDirectory;
        }

        public bool CacheExists() => System.IO.Directory.Exists(_dir);

        public void EnsureCacheExists() => System.IO.Directory.CreateDirectory(_dir);

        public Stream OpenRead(string filename)
        {
            var path = Path.Combine(_dir, filename);
            return System.IO.File.Exists(path) ? System.IO.File.OpenRead(path) : null;
        }

        public void Write(string filename, byte[] data)
            => System.IO.File.WriteAllBytes(Path.Combine(_dir, filename), data);

        public int GetCachedFileCount() => _fileCount;

        public void SaveCachedFileCount(int count) => _fileCount = count;

        public void Dispose()
        {
            if (_ownsDirectory && System.IO.Directory.Exists(_dir))
            {
                System.IO.Directory.Delete(_dir, true);
            }
        }
    }

    public class CachedParseResultsSerializerTests
    {
        [Test]
        public async Task RoundTrip_AllFieldsPreserved()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "cpr_test_" + Guid.NewGuid().ToString("N"));

            using (var provider = new TestCacheDataProvider(tempDir, ownsDirectory: true))
            {
                var files1 = new List<Soulseek.File>
                {
                    new Soulseek.File(1, "song.mp3", 5000000L, "mp3",
                        new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 320) },
                        false, false)
                };
                var files2 = new List<Soulseek.File>
                {
                    new Soulseek.File(2, "track.flac", 20000000L, "flac", null, false, false)
                };

                var dirs = new List<Soulseek.Directory>
                {
                    new Soulseek.Directory("Music/Album1", files1),
                    new Soulseek.Directory("Music/Album2", files2)
                };

                var hiddenDirs = new List<Soulseek.Directory>
                {
                    new Soulseek.Directory("Private/Stuff", new List<Soulseek.File>())
                };

                var browseResponse = new BrowseResponse(dirs);
                var keys = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>
                {
                    ["key1"] = Tuple.Create(100L, "val1", Tuple.Create(1, 2, 3, 4), true, false),
                    ["key2"] = Tuple.Create(200L, "val2", Tuple.Create(5, 6, 7, 8), false, true)
                };
                var tokenIndex = new Dictionary<string, List<int>>
                {
                    ["token1"] = new List<int> { 1, 2, 3 },
                    ["token2"] = new List<int> { 4, 5 }
                };
                var helperIndex = new Dictionary<int, string>
                {
                    [1] = "helper1",
                    [2] = "helper2"
                };
                var friendlyDirMapping = new List<Tuple<string, string>>
                {
                    Tuple.Create("friendly1", "uri1"),
                    Tuple.Create("friendly2", "uri2")
                };

                var original = new CachedParseResults(
                    keys, browseResponse.DirectoryCount, browseResponse,
                    hiddenDirs, friendlyDirMapping, tokenIndex, helperIndex, 42);

                CachedParseResultsSerializer.Store(provider, original);
                var restored = CachedParseResultsSerializer.Restore(provider);

                Assert.IsNotNull(restored);
                await Verifier.Verify(restored);
            }
        }

        [Test]
        public void Restore_MissingCacheDirectory_ReturnsNull()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), "cpr_nonexistent_" + Guid.NewGuid().ToString("N"));

            using (var provider = new TestCacheDataProvider(nonExistentDir))
            {
                var result = CachedParseResultsSerializer.Restore(provider);
                Assert.IsNull(result);
            }
        }

        [Test]
        public async Task BackwardCompat_RestoreFromTestDataSet1()
        {
            var testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Set1");

            // Set1 has the .mpk files but no SharedPreferences, so use -1 for file count
            using (var provider = new TestCacheDataProvider(testDataDir))
            {
                var result = CachedParseResultsSerializer.Restore(provider);

                Assert.IsNotNull(result);
                await Verifier.Verify(result);
            }
        }
    }
}
