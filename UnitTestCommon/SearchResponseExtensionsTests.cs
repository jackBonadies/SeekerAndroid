using Common.Share;
using NUnit.Framework;
using Seeker;
using Seeker.Extensions.SearchResponseExtensions;
using Soulseek;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    [TestFixture]
    public class SearchResponseExtensionsTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SimpleHelpers.STRINGS_KBS = "kbs";
            SimpleHelpers.STRINGS_KHZ = "kHz";
        }

        private File CreateFile(string filename, long size = 1000, IEnumerable<FileAttribute> attributes = null)
        {
            return new File(1, filename, size, "mp3", attributes);
        }

        private SearchResponse CreateResponse(IEnumerable<File> files, IEnumerable<File> lockedFiles = null)
        {
            return new SearchResponse("testuser", 1, true, 5000, 0, files, lockedFiles);
        }

        [Test]
        public void GetFiles_HideLocked_ReturnsOnlyUnlockedFiles()
        {
            var file1 = CreateFile(@"\\user\Music\song1.mp3");
            var locked1 = CreateFile(@"\\user\Music\song2.mp3");
            var resp = CreateResponse(new[] { file1 }, new[] { locked1 });

            var result = resp.GetFiles(hideLocked: true).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(file1.Filename, result[0].Filename);
        }

        [Test]
        public void GetFiles_ShowLocked_ReturnsBothFilesAndLocked()
        {
            var file1 = CreateFile(@"\\user\Music\song1.mp3");
            var locked1 = CreateFile(@"\\user\Music\song2.mp3");
            var resp = CreateResponse(new[] { file1 }, new[] { locked1 });

            var result = resp.GetFiles(hideLocked: false).ToList();

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void IsLockedOnly_NoFilesButHasLocked_ReturnsTrue()
        {
            var locked1 = CreateFile(@"\\user\Music\song.mp3");
            var resp = CreateResponse(new List<File>(), new[] { locked1 });

            Assert.IsTrue(resp.IsLockedOnly());
        }

        [Test]
        public void IsLockedOnly_HasFiles_ReturnsFalse()
        {
            var file1 = CreateFile(@"\\user\Music\song.mp3");
            var resp = CreateResponse(new[] { file1 });

            Assert.IsFalse(resp.IsLockedOnly());
        }

        [Test]
        public void IsLockedOnly_NothingAtAll_ReturnsFalse()
        {
            var resp = CreateResponse(new List<File>(), new List<File>());

            Assert.IsFalse(resp.IsLockedOnly());
        }

        [Test]
        public void GetDominantFileType_KnownType_ReturnsExtWithoutDot()
        {
            var file1 = CreateFile(@"\\user\Music\song.mp3");
            var resp = CreateResponse(new[] { file1 });

            string result = resp.GetDominantFileTypeAndBitRate(hideLocked: true, out double bitRate);

            Assert.AreEqual("mp3", result);
        }

        [Test]
        public void GetDominantFileType_WorksForCompoundExtensions()
        {
            var file1 = CreateFile(@"\\user\Music\archive.tar.gz");
            var resp = CreateResponse(new[] { file1 });

            string result = resp.GetDominantFileTypeAndBitRate(hideLocked: true, out double bitRate);

            Assert.AreEqual("tar.gz", result);
        }

        [Test]
        public void GetDominantFileType_WithBitRate_IncludesBitRate()
        {
            var attrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 320) };
            var file1 = CreateFile(@"\\user\Music\song.mp3", attributes: attrs);
            var resp = CreateResponse(new[] { file1 });

            string result = resp.GetDominantFileTypeAndBitRate(hideLocked: true, out double bitRate);

            Assert.AreEqual("mp3 (320kbs)", result);
            Assert.AreEqual(320, bitRate);
        }

        [Test]
        public void GetDominantFileType_WithBitDepthAndSampleRate_IncludesBoth()
        {
            var attrs = new List<FileAttribute>
            {
                new FileAttribute(FileAttributeType.BitDepth, 24),
                new FileAttribute(FileAttributeType.SampleRate, 96000)
            };
            var file1 = new File(1, @"\\user\Music\song.flac", 5000, "flac", attrs);
            var resp = CreateResponse(new[] { file1 });

            string result = resp.GetDominantFileTypeAndBitRate(hideLocked: true, out double bitRate);

            Assert.AreEqual("flac (24, 96kHz)", result);
            Assert.AreEqual(24 * 96 * 2, bitRate);
        }

        [Test]
        public void GetDominantFileType_Vbr_IncludesVbr()
        {
            var attrs1 = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 320) };
            var attrs2 = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 192) };
            var file1 = CreateFile(@"\\user\Music\song1.mp3", attributes: attrs1);
            var file2 = CreateFile(@"\\user\Music\song2.mp3", attributes: attrs2);
            var resp = CreateResponse(new[] { file1, file2 });

            string result = resp.GetDominantFileTypeAndBitRate(hideLocked: true, out double bitRate);

            Assert.AreEqual("mp3 (vbr)", result);
        }

        [Test]
        public void GetDominantFileType_UnknownType_FindsMostCommon()
        {
            var ogg1 = new File(1, @"\\user\Music\a.ogg", 1000, "ogg");
            var ogg2 = new File(1, @"\\user\Music\b.ogg", 1000, "ogg");
            var txt1 = new File(1, @"\\user\Music\c.txt", 100, "txt");
            var resp = CreateResponse(new[] { ogg1, ogg2, txt1 });

            string result = resp.GetDominantFileTypeAndBitRate(hideLocked: true, out double bitRate);

            Assert.AreEqual("ogg", result);
        }

        [Test]
        public void GetElementAtAdapterPosition_HideLocked_ReturnsFromFiles()
        {
            var file1 = CreateFile(@"\\user\Music\song1.mp3");
            var file2 = CreateFile(@"\\user\Music\song2.mp3");
            var resp = CreateResponse(new[] { file1, file2 });

            var result = resp.GetElementAtAdapterPosition(hideLocked: true, position: 1);

            Assert.AreEqual(file2.Filename, result.Filename);
        }

        [Test]
        public void GetElementAtAdapterPosition_ShowLocked_ReturnsFromLockedWhenPastFiles()
        {
            var file1 = CreateFile(@"\\user\Music\song1.mp3");
            var locked1 = CreateFile(@"\\user\Music\locked1.mp3");
            var resp = CreateResponse(new[] { file1 }, new[] { locked1 });

            var result = resp.GetElementAtAdapterPosition(hideLocked: false, position: 1);

            Assert.AreEqual(locked1.Filename, result.Filename);
        }

        [Test]
        public void GetElementAtAdapterPosition_ShowLocked_ReturnsFromFilesWhenInRange()
        {
            var file1 = CreateFile(@"\\user\Music\song1.mp3");
            var locked1 = CreateFile(@"\\user\Music\locked1.mp3");
            var resp = CreateResponse(new[] { file1 }, new[] { locked1 });

            var result = resp.GetElementAtAdapterPosition(hideLocked: false, position: 0);

            Assert.AreEqual(file1.Filename, result.Filename);
        }
    }
}
