using Common.Search;
using NUnit.Framework;
using Seeker;
using Soulseek;
using System.Collections.Generic;

namespace UnitTestCommon
{
    [TestFixture]
    public class SearchResultMetadataTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // These are normally populated by SeekerApplication at startup; the formatter must
            // also work in a plain test process.
            SimpleHelpers.STRINGS_KBS = "kbps";
            SimpleHelpers.STRINGS_KHZ = "kHz";
        }

        private static File MakeFile(
            string path,
            long size = 1024,
            int? length = null,
            int? bitRate = null)
        {
            var attributes = new List<FileAttribute>();
            if (length.HasValue)
            {
                attributes.Add(new FileAttribute(FileAttributeType.Length, length.Value));
            }
            if (bitRate.HasValue)
            {
                attributes.Add(new FileAttribute(FileAttributeType.BitRate, bitRate.Value));
            }
            return new File(1, path, size, System.IO.Path.GetExtension(path).TrimStart('.'), attributes);
        }

        private static SearchResponse MakeResponse(
            IEnumerable<File> files,
            IEnumerable<File> lockedFiles = null,
            int uploadSpeed = 500 * 1024,
            bool hasFreeUploadSlot = true,
            int queueLength = 0,
            string username = "peer")
        {
            return new SearchResponse(
                username,
                token: 1,
                hasFreeUploadSlot: hasFreeUploadSlot,
                uploadSpeed: uploadSpeed,
                queueLength: queueLength,
                fileList: files,
                lockedFileList: lockedFiles);
        }

        [Test]
        public void Create_ExposesNameFormatSizeDurationPeerSpeedAndSlots()
        {
            var response = MakeResponse(new[]
            {
                MakeFile(@"@peer\Music\Album\song.mp3", size: 5 * 1024 * 1024, length: 245, bitRate: 320)
            });

            var meta = SearchResultMetadata.Create(response, hideLocked: true);

            Assert.AreEqual("song.mp3", meta.FileName);
            Assert.AreEqual("Album", meta.FolderName);
            Assert.AreEqual("peer", meta.Peer);
            Assert.AreEqual("mp3", meta.FormatName);
            Assert.AreEqual("320 kbps", meta.Bitrate);
            Assert.AreEqual("5 MB", meta.Size);
            Assert.AreEqual("4m 5s", meta.Duration);
            Assert.AreEqual("500 kb/s", meta.Speed);
            Assert.AreEqual("Free slot", meta.Slots);
            Assert.AreEqual(1, meta.FileCount);
            Assert.AreEqual("1 file", meta.FileCountText);
            Assert.IsFalse(meta.IsGroup);
            // A single file result keeps the file name as the primary identity and shows its
            // folder on the metadata line.
            Assert.AreEqual("song.mp3", meta.PrimaryText);
            Assert.AreEqual("Album", meta.SecondaryText);
            Assert.AreEqual("Album \u2022 5 MB \u2022 4m 5s \u2022 320 kbps", meta.DetailLine);
            // A single file row does not need a redundant "1 file" prefix.
            Assert.AreEqual(meta.DetailLine, meta.DetailLineWithFileCount);
        }

        [Test]
        public void Create_MultiFileGroupKeepsFolderPrimaryAndRepresentativeFileNameSecondary()
        {
            var response = MakeResponse(new[]
            {
                MakeFile(@"@peer\Music\Album\01 - one.mp3", size: 4 * 1024 * 1024, length: 200, bitRate: 320),
                MakeFile(@"@peer\Music\Album\02 - two.mp3", size: 9 * 1024 * 1024, length: 420, bitRate: 128),
            });

            var meta = SearchResultMetadata.Create(response, hideLocked: true);

            Assert.IsTrue(meta.IsGroup);
            Assert.AreEqual(2, meta.FileCount);
            Assert.AreEqual("2 files", meta.FileCountText);
            // The folder stays the visible group identity; the file name is the secondary line.
            Assert.AreEqual("Album", meta.PrimaryText);
            Assert.AreEqual("01 - one.mp3", meta.SecondaryText);
            // Metadata is the representative file's own facts, led by its name.
            Assert.AreEqual("3m 20s", meta.Duration);
            Assert.AreEqual("4 MB", meta.Size);
            Assert.AreEqual("320 kbps", meta.Bitrate);
            Assert.AreEqual("01 - one.mp3 \u2022 4 MB \u2022 3m 20s \u2022 320 kbps", meta.DetailLine);
            Assert.AreEqual("2 files \u2022 01 - one.mp3 \u2022 4 MB \u2022 3m 20s \u2022 320 kbps", meta.DetailLineWithFileCount);
            Assert.AreEqual("peer \u2022 01 - one.mp3 \u2022 mp3 \u2022 4 MB \u2022 3m 20s \u2022 320 kbps \u2022 500 kb/s \u2022 Free slot \u2022 2 files", meta.CompactLine);
        }

        [Test]
        public void Create_GroupDoesNotBorrowTheDominantBitrateForTheRepresentativeFile()
        {
            // The group dominant chip is derived from the mp3 file, but the representative (first)
            // file has no bitrate of its own. The row must not present the group bitrate next to
            // another file's size as if it described the group.
            var response = MakeResponse(new[]
            {
                MakeFile(@"@peer\Music\Album\cover.jpg", size: 100 * 1024),
                MakeFile(@"@peer\Music\Album\song.mp3", size: 5 * 1024 * 1024, length: 245, bitRate: 192),
            });

            var meta = SearchResultMetadata.Create(response, hideLocked: true);

            Assert.IsTrue(meta.IsGroup);
            Assert.AreEqual("Album", meta.PrimaryText);
            Assert.AreEqual("cover.jpg", meta.SecondaryText);
            Assert.AreEqual("100 KB", meta.Size);
            Assert.AreEqual(string.Empty, meta.Bitrate);
            Assert.AreEqual("cover.jpg \u2022 100 KB", meta.DetailLine);
            // The group dominant format/quality is preserved separately for the format chip.
            Assert.AreEqual("mp3", meta.FormatName);
            Assert.AreEqual("mp3 (192kbps)", meta.DominantFileType);
            Assert.IsFalse(meta.DetailLine.Contains("kbps"));
        }

        [Test]
        public void Create_SingularFileCountTextForSingleFileResult()
        {
            var response = MakeResponse(new[]
            {
                MakeFile(@"@peer\Music\Album\song.flac", size: 30 * 1024 * 1024)
            });

            var meta = SearchResultMetadata.Create(response, hideLocked: true);

            Assert.AreEqual("1 file", meta.FileCountText);
            Assert.IsFalse(meta.IsGroup);
        }

        [Test]
        public void Create_QueueIsShownWhenThereIsNoFreeUploadSlot()
        {
            var response = MakeResponse(
                new[] { MakeFile(@"@peer\Music\Album\song.flac", size: 10 * 1024 * 1024) },
                hasFreeUploadSlot: false,
                queueLength: 7);

            var meta = SearchResultMetadata.Create(response, hideLocked: true);

            Assert.AreEqual("Queue 7", meta.Slots);
        }

        [Test]
        public void Create_HideLockedExcludesLockedFilesFromCountAndDetail()
        {
            var response = MakeResponse(
                new[] { MakeFile(@"@peer\Music\Album\unlocked.mp3", size: 1024) },
                new[] { MakeFile(@"@peer\Music\Locked\locked.flac", size: 2048) });

            var hidden = SearchResultMetadata.Create(response, hideLocked: true);
            var shown = SearchResultMetadata.Create(response, hideLocked: false);

            Assert.AreEqual(1, hidden.FileCount);
            Assert.AreEqual(2, shown.FileCount);
            Assert.AreEqual("unlocked.mp3", hidden.FileName);
            Assert.AreEqual("unlocked.mp3", shown.FileName);
        }

        [Test]
        public void Create_LockedOnlyKeepsTheLockIndicatorAndMetadata()
        {
            var response = MakeResponse(
                files: new List<File>(),
                lockedFiles: new[] { MakeFile(@"@peer\Music\Locked\locked.flac", size: 3 * 1024 * 1024, length: 60) });

            var meta = SearchResultMetadata.Create(response, hideLocked: false);

            Assert.IsTrue(meta.IsLockedOnly);
            Assert.IsTrue(meta.FileName.EndsWith("locked.flac"));
            Assert.IsTrue(meta.FileName.StartsWith(SimpleHelpers.LOCK_EMOJI));
            Assert.AreEqual("flac", meta.FormatName);
            Assert.AreEqual("3 MB", meta.Size);
            Assert.AreEqual("1m 0s", meta.Duration);
        }

        [Test]
        public void Create_WithoutBitrateAttributeStillBuildsDetailLine()
        {
            var response = MakeResponse(new[]
            {
                MakeFile(@"@peer\Music\Album\song.mp3", size: 2 * 1024 * 1024)
            });

            var meta = SearchResultMetadata.Create(response, hideLocked: true);

            Assert.AreEqual(string.Empty, meta.Bitrate);
            Assert.AreEqual("Album \u2022 2 MB", meta.DetailLine);
        }

        [Test]
        public void Create_NullResponse_ReturnsEmptyMetadata()
        {
            var meta = SearchResultMetadata.Create(null, hideLocked: true);

            Assert.AreEqual(string.Empty, meta.FileName);
            Assert.AreEqual(string.Empty, meta.DetailLine);
            Assert.AreEqual(0, meta.FileCount);
        }
    }
}
