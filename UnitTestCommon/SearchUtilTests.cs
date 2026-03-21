using Common.Search;
using NUnit.Framework;
using Soulseek;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    [TestFixture]
    public class SearchUtilTests
    {
        private const string Username = "testuser";
        private const int Token = 42;
        private const string DirName = @"@@user\Music\Album";

        private File CreateFile(string filename, long size = 1000, int code = 1, string extension = "mp3", IEnumerable<FileAttribute> attributes = null)
        {
            return new File(code, filename, size, extension, attributes);
        }

        private SearchResponse CreateSearchResponse(IEnumerable<File> files, IEnumerable<File> lockedFiles = null)
        {
            return new SearchResponse(Username, Token, true, 5000, 0, files, lockedFiles);
        }

        [Test]
        public void CreatesResponseWithFullFilenames()
        {
            var originalFile = CreateFile(DirName + @"\song.mp3");
            var originalResponse = CreateSearchResponse(new[] { originalFile });

            var dirFile = CreateFile("song.mp3");
            var directory = new Directory(DirName, new[] { dirFile });

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            Assert.AreEqual(1, result.Files.Count);
            Assert.AreEqual(DirName + @"\song.mp3", result.Files.First().Filename);
        }

        [Test]
        public void PreservesSearchResponseMetadata()
        {
            var originalResponse = CreateSearchResponse(new[] { CreateFile(DirName + @"\song.mp3") });
            var directory = new Directory(DirName, new[] { CreateFile("song.mp3") });

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            Assert.AreEqual(Username, result.Username);
            Assert.AreEqual(Token, result.Token);
            Assert.IsTrue(result.HasFreeUploadSlot);
            Assert.AreEqual(5000, result.UploadSpeed);
            Assert.AreEqual(0, result.QueueLength);
        }

        [Test]
        public void PreservesFileSizeAndExtension()
        {
            var originalResponse = CreateSearchResponse(new[] { CreateFile(DirName + @"\track.flac") });
            var dirFile = CreateFile("track.flac", size: 5000, extension: "flac");
            var directory = new Directory(DirName, new[] { dirFile });

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            var resultFile = result.Files.First();
            Assert.AreEqual(5000, resultFile.Size);
            Assert.AreEqual("flac", resultFile.Extension);
        }

        [Test]
        public void AugmentsAttributesFromOriginalFiles()
        {
            var attrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 320) };
            var originalFile = CreateFile(DirName + @"\song.mp3", attributes: attrs);
            var originalResponse = CreateSearchResponse(new[] { originalFile });

            var dirFile = CreateFile("song.mp3"); // no attributes
            var directory = new Directory(DirName, new[] { dirFile });

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            var resultFile = result.Files.First();
            Assert.AreEqual(1, resultFile.Attributes.Count);
            Assert.AreEqual(320, resultFile.Attributes.First().Value);
            Assert.AreEqual(FileAttributeType.BitRate, resultFile.Attributes.First().Type);
        }

        [Test]
        public void KeepsDirectoryFileAttributesWhenNoMatch()
        {
            var originalFile = CreateFile(DirName + @"\other.mp3");
            var originalResponse = CreateSearchResponse(new[] { originalFile });

            var dirAttrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.SampleRate, 44100) };
            var dirFile = CreateFile("song.mp3", attributes: dirAttrs);
            var directory = new Directory(DirName, new[] { dirFile });

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            var resultFile = result.Files.First();
            Assert.AreEqual(1, resultFile.Attributes.Count);
            Assert.AreEqual(44100, resultFile.Attributes.First().Value);
        }

        [Test]
        public void HandlesMultipleFiles()
        {
            var origFiles = new[]
            {
                CreateFile(DirName + @"\01 - first.mp3"),
                CreateFile(DirName + @"\02 - second.mp3"),
                CreateFile(DirName + @"\03 - third.mp3"),
            };
            var originalResponse = CreateSearchResponse(origFiles);

            var dirFiles = new[]
            {
                CreateFile("01 - first.mp3"),
                CreateFile("02 - second.mp3"),
                CreateFile("03 - third.mp3"),
                CreateFile("04 - fourth.mp3"),
            };
            var directory = new Directory(DirName, dirFiles);

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            Assert.AreEqual(4, result.Files.Count);
            Assert.AreEqual(DirName + @"\01 - first.mp3", result.Files.ElementAt(0).Filename);
            Assert.AreEqual(DirName + @"\02 - second.mp3", result.Files.ElementAt(1).Filename);
            Assert.AreEqual(DirName + @"\03 - third.mp3", result.Files.ElementAt(2).Filename);
            Assert.AreEqual(DirName + @"\04 - fourth.mp3", result.Files.ElementAt(3).Filename);
        }

        [Test]
        public void HideLockedTrue_OnlyMatchesAgainstUnlockedFiles()
        {
            var lockedAttrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 128) };
            var lockedFile = CreateFile(DirName + @"\song.mp3", attributes: lockedAttrs);
            var originalResponse = CreateSearchResponse(new File[0], lockedFiles: new[] { lockedFile });

            var dirFile = CreateFile("song.mp3");
            var directory = new Directory(DirName, new[] { dirFile });

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            // hideLocked=true means only unlocked files are searched for attribute matching
            // since the only match is locked, dirFile's own (null) attributes are used
            var resultFile = result.Files.First();
            Assert.AreEqual(0, resultFile.Attributes.Count);
        }

        [Test]
        public void HideLockedFalse_MatchesAgainstLockedFilesToo()
        {
            var lockedAttrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 128) };
            var lockedFile = CreateFile(DirName + @"\song.mp3", attributes: lockedAttrs);
            var originalResponse = CreateSearchResponse(new File[0], lockedFiles: new[] { lockedFile });

            var dirFile = CreateFile("song.mp3");
            var directory = new Directory(DirName, new[] { dirFile });

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: false);

            // hideLocked=false means locked files are included, so attributes should be augmented
            var resultFile = result.Files.First();
            Assert.AreEqual(1, resultFile.Attributes.Count);
            Assert.AreEqual(128, resultFile.Attributes.First().Value);
        }

        [Test]
        public void EmptyDirectory_ReturnsEmptyFileList()
        {
            var originalResponse = CreateSearchResponse(new[] { CreateFile(DirName + @"\song.mp3") });
            var directory = new Directory(DirName);

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            Assert.AreEqual(0, result.Files.Count);
        }

        [Test]
        public void DirectoryHasFilesNotInOriginal_StillCreatesFullPaths()
        {
            var originalResponse = CreateSearchResponse(new File[0]);

            var dirFiles = new[]
            {
                CreateFile("new_song.mp3"),
                CreateFile("another.flac", extension: "flac"),
            };
            var directory = new Directory(DirName, dirFiles);

            var result = SearchUtil.CreateSearchResponseFromDirectory(originalResponse, directory, hideLocked: true);

            Assert.AreEqual(2, result.Files.Count);
            Assert.AreEqual(DirName + @"\new_song.mp3", result.Files.ElementAt(0).Filename);
            Assert.AreEqual(DirName + @"\another.flac", result.Files.ElementAt(1).Filename);
        }
    }
}
