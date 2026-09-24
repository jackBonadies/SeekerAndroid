using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTestCommon
{
    public class SimpleHelpersTest
    {
        [SetUp]
        public void Setup()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            SimpleHelpers.STRINGS_KBS = " kbs";
            SimpleHelpers.STRINGS_KHZ = " kHz";
        }

        // --- AvoidLineBreaks ---

        [Test]
        public void AvoidLineBreaks_ReplacesSpacesWithNonBreaking()
        {
            string result = SimpleHelpers.AvoidLineBreaks("hello world");
            Assert.That(result, Does.Not.Contain(" ")); // no normal spaces
            Assert.That(result, Does.Contain("\u00A0"));
        }

        [Test]
        public void AvoidLineBreaks_InsertsWordJoinerAfterBackslash()
        {
            string result = SimpleHelpers.AvoidLineBreaks("path\\file");
            Assert.That(result, Does.Contain("\\\u2060"));
        }

        [Test]
        public void AvoidLineBreaks_EmptyString()
        {
            Assert.That(SimpleHelpers.AvoidLineBreaks(""), Is.EqualTo(""));
        }

        // --- GetHumanReadableTime ---

        [Test]
        public void GetHumanReadableTime_ZeroSeconds_ReturnsEmpty()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(0), Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetHumanReadableTime_SecondsOnly()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(45), Is.EqualTo("45s"));
        }

        [Test]
        public void GetHumanReadableTime_MinutesAndSeconds()
        {
            // 3 minutes 5 seconds = 185 seconds
            Assert.That(SimpleHelpers.GetHumanReadableTime(185), Is.EqualTo("3m5s"));
        }

        [Test]
        public void GetHumanReadableTime_HoursMinutesSeconds()
        {
            // 1 hour 2 minutes 3 seconds = 3723
            Assert.That(SimpleHelpers.GetHumanReadableTime(3723), Is.EqualTo("1h2m3s"));
        }

        [Test]
        public void GetHumanReadableTime_ExactlyOneMinute()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(60), Is.EqualTo("1m0s"));
        }

        [Test]
        public void GetHumanReadableTime_ExactlyOneHour()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(3600), Is.EqualTo("1h0m0s"));
        }

        [Test]
        public void GetHumanReadableTime_WithSpace_SecondsOnly()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(45, true), Is.EqualTo("45s"));
        }

        [Test]
        public void GetHumanReadableTime_WithSpace_MinutesAndSeconds()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(185, true), Is.EqualTo("3m 5s"));
        }

        [Test]
        public void GetHumanReadableTime_WithSpace_HoursMinutesSeconds()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(3723, true), Is.EqualTo("1h 2m 3s"));
        }

        [Test]
        public void GetHumanReadableTime_WithSpace_ZeroStillEmpty()
        {
            Assert.That(SimpleHelpers.GetHumanReadableTime(0, true), Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetHumanReadableTime_LargeValue()
        {
            // 100 hours = 360000 seconds
            Assert.That(SimpleHelpers.GetHumanReadableTime(360000), Is.EqualTo("100h0m0s"));
        }

        [TestCase(0, "0s")]
        [TestCase(45, "45s")]
        [TestCase(59.9, "59s")]
        [TestCase(60, "1m 0s")]
        [TestCase(123, "2m 3s")]
        [TestCase(3600, "1h 0m")]
        [TestCase(3723, "1h 2m")]
        [TestCase(86400 + 4 * 3600 + 59 * 60 + 59, "1d 4h")]
        [TestCase(-5, "0s")]
        public void FormatTimeRemaining_FormatsByLargestUnit(double seconds, string expected)
        {
            Assert.AreEqual(expected, SimpleHelpers.FormatTimeRemaining(TimeSpan.FromSeconds(seconds)));
        }

        [Test]
        public void DescribeException_UnwrapsAggregateAndWalksInnerChain()
        {
            var inner = new TimeoutException("The wait timed out after 30000 milliseconds");
            var outer = new SoulseekClientException("Failed to download file x from user y: The wait timed out", inner);
            var faulted = Task.FromException(outer);

            Assert.AreEqual(
                "SoulseekClientException: Failed to download file x from user y: The wait timed out <- TimeoutException: The wait timed out after 30000 milliseconds",
                SimpleHelpers.DescribeException(faulted.Exception));
            Assert.AreEqual("TimeoutException: The wait timed out after 30000 milliseconds", SimpleHelpers.DescribeException(inner));
            Assert.AreEqual("null", SimpleHelpers.DescribeException(null));
        }

        // --- GetHumanReadableSize ---

        [Test]
        public void GetHumanReadableSize_SmallSize_ReturnsMb()
        {
            long bytes = 5 * 1024 * 1024; // 5 MB
            string result = SimpleHelpers.GetHumanReadableSize(bytes);
            Assert.That(result, Is.EqualTo("5 MB"));
        }

        [Test]
        public void GetHumanReadableSize_LargeSize_ReturnsGb()
        {
            long bytes = 2L * 1024 * 1024 * 1024; // 2 GB
            string result = SimpleHelpers.GetHumanReadableSize(bytes);
            Assert.That(result, Is.EqualTo("2 GB"));
        }

        [Test]
        public void GetHumanReadableSize_JustOverGbThreshold()
        {
            long bytes = 1024L * 1024 * 1024 + 1;
            string result = SimpleHelpers.GetHumanReadableSize(bytes);
            Assert.That(result, Does.Contain("GB"));
        }

        [Test]
        public void GetHumanReadableSize_ExactlyAtGbThreshold_ReturnsMb()
        {
            // exactly 1 GB is NOT > 1 GB, so should return mb
            long bytes = 1024L * 1024 * 1024;
            string result = SimpleHelpers.GetHumanReadableSize(bytes);
            Assert.That(result, Does.Contain("MB"));
        }

        [Test]
        public void GetHumanReadableSize_ZeroBytes()
        {
            string result = SimpleHelpers.GetHumanReadableSize(0);
            Assert.That(result, Is.EqualTo("0 B"));
        }

        // --- GetHumanReadableProgressSize ---

        // always show ~3 significant digits
        [TestCase(5270000L, 9050000L, "5.03 / 8.63 MB")]
        [TestCase(12900000L, 47185920L, "12.3 / 45.0 MB")]
        [TestCase(365953024L, 367001600L, "349 / 350 MB")]
        [TestCase(429496730L, 1717986918L, "0.40 / 1.60 GB")]
        [TestCase(46080L, 307200L, "45 / 300 KB")]
        [TestCase(100L, 900L, "100 / 900 B")]
        public void GetHumanReadableProgressSize_Tiers(long current, long total, string expected)
        {
            Assert.That(SimpleHelpers.GetHumanReadableProgressSize(current, total), Is.EqualTo(expected));
        }

        [Test]
        public void GetHumanReadableProgressSize_ZeroProgress_KeepsTotalWidth()
        {
            Assert.That(SimpleHelpers.GetHumanReadableProgressSize(0, 9050000L), Is.EqualTo("0.00 / 8.63 MB"));
        }

        [Test]
        public void GetHumanReadableProgressSize_Complete()
        {
            long bytes = 5L * 1024 * 1024;
            Assert.That(SimpleHelpers.GetHumanReadableProgressSize(bytes, bytes), Is.EqualTo("5.00 / 5.00 MB"));
        }

        [Test]
        public void GetHumanReadableProgressSize_ExactlyAtGbThreshold_ReturnsMb()
        {
            long bytes = 1024L * 1024 * 1024;
            Assert.That(SimpleHelpers.GetHumanReadableProgressSize(bytes / 2, bytes), Is.EqualTo("512 / 1024 MB"));
        }

        [Test]
        public void GetHumanReadableProgressSize_ZeroTotal_DoesNotThrow()
        {
            Assert.That(SimpleHelpers.GetHumanReadableProgressSize(0, 0), Is.EqualTo("0 / 0 B"));
        }

        // --- GetTransferSpeedString ---

        [Test]
        public void GetTransferSpeedString_AboveMb()
        {
            string result = SimpleHelpers.GetTransferSpeedString(2 * 1048576.0);
            Assert.That(result, Is.EqualTo("2.0 MB/s"));
        }

        [Test]
        public void GetTransferSpeedString_BelowMb()
        {
            string result = SimpleHelpers.GetTransferSpeedString(512 * 1024.0);
            Assert.That(result, Is.EqualTo("512.0 KB/s"));
        }

        [Test]
        public void GetTransferSpeedString_ExactlyMb_ReturnsKbs()
        {
            // exactly 1MB is NOT > 1MB
            string result = SimpleHelpers.GetTransferSpeedString(1048576.0);
            Assert.That(result, Does.Contain("KB/s"));
        }

        // --- IsFileUri ---

        [Test]
        public void IsFileUri_FileScheme_ReturnsTrue()
        {
            Assert.That(SimpleHelpers.IsFileUri("file:///storage/test"), Is.True);
        }

        [Test]
        public void IsFileUri_ContentScheme_ReturnsFalse()
        {
            Assert.That(SimpleHelpers.IsFileUri("content://com.android/test"), Is.False);
        }

        [Test]
        public void IsFileUri_UnknownScheme_Throws()
        {
            Assert.Throws<Exception>(() => SimpleHelpers.IsFileUri("https://example.com"));
        }

        // --- IsSpecialMessage ---

        [Test]
        public void IsSpecialMessage_Null_ReturnsFalse()
        {
            Assert.That(SimpleHelpers.IsSpecialMessage(null, out _), Is.False);
        }

        [Test]
        public void IsSpecialMessage_Empty_ReturnsFalse()
        {
            Assert.That(SimpleHelpers.IsSpecialMessage("", out _), Is.False);
        }

        [Test]
        public void IsSpecialMessage_SlashMe()
        {
            Assert.That(SimpleHelpers.IsSpecialMessage("/me waves", out var type), Is.True);
            Assert.That(type, Is.EqualTo(SimpleHelpers.SpecialMessageType.SlashMe));
        }

        [Test]
        public void IsSpecialMessage_SlashMeWithoutSpace_NotSpecial()
        {
            // "/me" without trailing space should not match
            Assert.That(SimpleHelpers.IsSpecialMessage("/me", out _), Is.False);
        }

        [Test]
        public void IsSpecialMessage_MagnetLink()
        {
            Assert.That(SimpleHelpers.IsSpecialMessage("check this magnet:?xt=urn:btih:abc123", out var type), Is.True);
            Assert.That(type, Is.EqualTo(SimpleHelpers.SpecialMessageType.MagnetLink));
        }

        [Test]
        public void IsSpecialMessage_SlskLink()
        {
            Assert.That(SimpleHelpers.IsSpecialMessage("download slsk://user/file", out var type), Is.True);
            Assert.That(type, Is.EqualTo(SimpleHelpers.SpecialMessageType.SlskLink));
        }

        [Test]
        public void IsSpecialMessage_NormalMessage_ReturnsFalse()
        {
            Assert.That(SimpleHelpers.IsSpecialMessage("hello world", out var type), Is.False);
            Assert.That(type, Is.EqualTo(SimpleHelpers.SpecialMessageType.None));
        }

        // --- ParseSpecialMessage ---

        [Test]
        public void ParseSpecialMessage_SlashMe_StripsPrefix()
        {
            string result = SimpleHelpers.ParseSpecialMessage("/me goes to the store");
            Assert.That(result, Is.EqualTo("goes to the store"));
        }

        [Test]
        public void ParseSpecialMessage_MagnetLink_ReturnsUnchanged()
        {
            string msg = "get this magnet:?xt=urn:btih:abc123";
            Assert.That(SimpleHelpers.ParseSpecialMessage(msg), Is.EqualTo(msg));
        }

        [Test]
        public void ParseSpecialMessage_NormalMessage_ReturnsUnchanged()
        {
            string msg = "hello world";
            Assert.That(SimpleHelpers.ParseSpecialMessage(msg), Is.EqualTo(msg));
        }

        // --- GetDirectoryRequestFolderName ---

        [Test]
        public void GetDirectoryRequestFolderName_NormalPath()
        {
            string result = SimpleHelpers.GetDirectoryRequestFolderName(@"music\artist\album\song.mp3");
            Assert.That(result, Is.EqualTo(@"music\artist\album"));
        }

        [Test]
        public void GetDirectoryRequestFolderName_NoBackslash_ReturnsEmpty()
        {
            // no backslash means LastIndexOf returns -1, Substring(0, -1) throws
            // the catch block returns ""
            string result = SimpleHelpers.GetDirectoryRequestFolderName("song.mp3");
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void GetDirectoryRequestFolderName_TrailingBackslash()
        {
            string result = SimpleHelpers.GetDirectoryRequestFolderName(@"music\artist\");
            Assert.That(result, Is.EqualTo(@"music\artist"));
        }

        // --- GetFileNameFromFile ---

        [Test]
        public void GetFileNameFromFile_NormalPath()
        {
            string result = SimpleHelpers.GetFileNameFromFile(@"music\artist\song.mp3").ToString();
            Assert.That(result, Is.EqualTo("song.mp3"));
        }

        [Test]
        public void GetFileNameFromFile_NoBackslash()
        {
            // LastIndexOf returns -1, Substring(0) returns the whole string
            string result = SimpleHelpers.GetFileNameFromFile("song.mp3").ToString();
            Assert.That(result, Is.EqualTo("song.mp3"));
        }

        [Test]
        public void GetFileNameFromFile_TrailingBackslash_ReturnsEmpty()
        {
            string result = SimpleHelpers.GetFileNameFromFile(@"music\artist\").ToString();
            Assert.That(result, Is.EqualTo(""));
        }

        [TestCase(1, @"level1")]
        [TestCase(2, @"level2\level1")]
        [TestCase(3, @"level3\level2\level1")]
        [TestCase(4, @"level4\level3\level2\level1")]
        public void GetFolderNameFromFileLevels_ThaiCulture_DoesNotThrow(int levels, string expected)
        {
            RunInCulture("th-TH", () =>
            {
                string result = SimpleHelpers.GetFolderNameFromFile(@"level4\level3\level2\level1\song.mp3", levels).ToString();
                Assert.That(result, Is.EqualTo(expected));
            });
        }

        [Test]
        public void GetParentFolderName_ThaiCulture_DoesNotThrow()
        {
            RunInCulture("th-TH", () =>
            {
                string result = SimpleHelpers.GetParentFolderNameFromFile(@"level4\level3\level2\level1\song.mp3").ToString();
                Assert.That(result, Is.EqualTo("level2"));
                result = SimpleHelpers.GetParentFolderNameFromFile(@"level2\level1\song.mp3").ToString();
                Assert.That(result, Is.EqualTo("level2"));
            });
        }

        [TestCase(@"level2\level1\song.mp3", 5, @"level2\level1")]
        [TestCase(@"level2\level1\song.mp3", 0, "")]
        [TestCase("song.mp3", 1, "")]
        [TestCase("", 1, "")]
        [TestCase(@"\song.mp3", 1, "")]
        public void GetFolderNameFromFile_EdgeCases(string path, int levels, string expected)
        {
            Assert.That(SimpleHelpers.GetFolderNameFromFile(path, levels).ToString(), Is.EqualTo(expected));
        }

        [TestCase(@"level3\level2\level1\song.mp3", "level2")]
        [TestCase(@"level2\level1\song.mp3", "level2")]
        [TestCase(@"\level2\level1\song.mp3", "level2")]
        [TestCase(@"level1\song.mp3", "")]
        [TestCase("song.mp3", "")]
        [TestCase("", "")]
        public void GetParentFolderNameFromFile_EdgeCases(string path, string expected)
        {
            Assert.That(SimpleHelpers.GetParentFolderNameFromFile(path).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void GetFileNameFromFile_Null_ReturnsEmpty()
        {
            Assert.That(SimpleHelpers.GetFileNameFromFile((string)null).ToString(), Is.EqualTo(""));
        }

        [Test]
        public void GetDirectoryRequestFolderName_ThaiCulture_ReturnsFolder()
        {
            RunInCulture("th-TH", () =>
            {
                string result = SimpleHelpers.GetDirectoryRequestFolderName(@"music\artist\album\song.mp3");
                Assert.That(result, Is.EqualTo(@"music\artist\album"));
            });
        }

        [Test]
        public void GetAllButLast_ThaiCulture_ReturnsParent()
        {
            RunInCulture("th-TH", () =>
            {
                string result = SimpleHelpers.GetAllButLast(@"raw:\storage\emulated\0\Download\Soulseek Complete");
                Assert.That(result, Is.EqualTo(@"raw:\storage\emulated\0\Download"));
            });
        }

        [Test]
        public void GetFullPathFromFile_ThaiCulture_ReturnsFolder()
        {
            RunInCulture("th-TH", () =>
            {
                string result = SimpleHelpers.GetAllButLast(@"music\artist\album\song.mp3");
                Assert.That(result, Is.EqualTo(@"music\artist\album"));
            });
        }

        private static void RunInCulture(string cultureName, Action action)
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(cultureName);
                action();
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        // --- GetAllButLast ---

        [Test]
        public void GetAllButLast_NormalPath()
        {
            string result = SimpleHelpers.GetAllButLast(@"raw:\storage\emulated\0\Download\Soulseek Complete");
            Assert.That(result, Is.EqualTo(@"raw:\storage\emulated\0\Download"));
        }

        [Test]
        public void GetAllButLast_NoBackslash_Throws()
        {
            // LastIndexOf returns -1, Substring(0, -1) throws
            // Unlike GetDirectoryRequestFolderName, this has no try/catch
            Assert.Throws<ArgumentOutOfRangeException>(() => SimpleHelpers.GetAllButLast("nobackslash"));
        }

        // --- IsUploadCompleteOrAborted ---

        [Test]
        public void IsUploadCompleteOrAborted_Succeeded_ReturnsTrue()
        {
            Assert.That(SimpleHelpers.IsUploadCompleteOrAborted(TransferStates.Succeeded), Is.True);
        }

        [Test]
        public void IsUploadCompleteOrAborted_Cancelled_ReturnsTrue()
        {
            Assert.That(SimpleHelpers.IsUploadCompleteOrAborted(TransferStates.Cancelled), Is.True);
        }

        [Test]
        public void IsUploadCompleteOrAborted_InProgress_ReturnsFalse()
        {
            Assert.That(SimpleHelpers.IsUploadCompleteOrAborted(TransferStates.InProgress), Is.False);
        }

        [Test]
        public void IsUploadCompleteOrAborted_None_ReturnsFalse()
        {
            Assert.That(SimpleHelpers.IsUploadCompleteOrAborted(TransferStates.None), Is.False);
        }

        [Test]
        public void IsUploadCompleteOrAborted_CompletedFlag_ReturnsTrue()
        {
            Assert.That(SimpleHelpers.IsUploadCompleteOrAborted(TransferStates.Completed), Is.True);
        }

        // --- GetHumanReadableAttributesForSingleItem ---

        [Test]
        public void GetHumanReadableAttributes_NoAttributes_ReturnsEmpty()
        {
            var file = new File(1, "test.mp3", 1000, "mp3", new List<FileAttribute>());
            string result = SimpleHelpers.GetHumanReadableAttributesForSingleItem(file);
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetHumanReadableAttributes_BitRateOnly()
        {
            var attrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitRate, 320) };
            var file = new File(1, "test.mp3", 1000, "mp3", attrs);
            string result = SimpleHelpers.GetHumanReadableAttributesForSingleItem(file);
            Assert.That(result, Is.EqualTo("320 kbs"));
        }

        [Test]
        public void GetHumanReadableAttributes_SampleRateOnly()
        {
            var attrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.SampleRate, 44100) };
            var file = new File(1, "test.flac", 1000, "flac", attrs);
            string result = SimpleHelpers.GetHumanReadableAttributesForSingleItem(file);
            Assert.That(result, Is.EqualTo("44.1 kHz"));
        }

        [Test]
        public void GetHumanReadableAttributes_BitDepthAndSampleRate()
        {
            var attrs = new List<FileAttribute>
            {
                new FileAttribute(FileAttributeType.BitDepth, 24),
                new FileAttribute(FileAttributeType.SampleRate, 96000)
            };
            var file = new File(1, "test.flac", 1000, "flac", attrs);
            string result = SimpleHelpers.GetHumanReadableAttributesForSingleItem(file);
            Assert.That(result, Is.EqualTo("24, 96 kHz"));
        }

        [Test]
        public void GetHumanReadableAttributes_BitDepthOnly_ReturnsEmpty()
        {
            // bitDepth without sampleRate falls through to else -> empty
            var attrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.BitDepth, 24) };
            var file = new File(1, "test.flac", 1000, "flac", attrs);
            string result = SimpleHelpers.GetHumanReadableAttributesForSingleItem(file);
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        // --- GetSizeLengthAttrString ---

        [Test]
        public void GetSizeLengthAttrString_NoLengthNoAttrs_ReturnsSizeOnly()
        {
            var file = new File(1, "test.mp3", 5 * 1024 * 1024, "mp3", new List<FileAttribute>());
            string result = SimpleHelpers.GetSizeLengthAttrString(file);
            Assert.That(result, Does.Contain("MB"));
            Assert.That(result, Does.Not.Contain("•"));
        }

        [Test]
        public void GetSizeLengthAttrString_WithLength_IncludesTime()
        {
            var attrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.Length, 185) };
            var file = new File(1, "test.mp3", 5 * 1024 * 1024, "mp3", attrs);
            string result = SimpleHelpers.GetSizeLengthAttrString(file);
            Assert.That(result, Does.Contain("•"));
            Assert.That(result, Does.Contain("3m 5s"));
        }

        [TestCase(5_624_222L, "5.36 MB")]
        [TestCase(5L * 1024 * 1024, "5 MB")]
        [TestCase(512_345_900L, "489 MB")]
        [TestCase(1_234_567_890L, "1.15 GB")]
        [TestCase(20_234_567_890L, "18.8 GB")]
        public void GetSizeAttribute(long size, string expected)
        {
            var attrs = new List<FileAttribute> { new FileAttribute(FileAttributeType.Length, 185) };
            var file = new File(1, "test.mp3", size, "mp3", attrs);
            string result = SimpleHelpers.GetSizeLengthAttrString(file);
            Assert.That(result, Does.Contain(expected));
        }

        [Test]
        public void GetSizeLengthAttrString_WithLengthAndAttrs_IncludesBoth()
        {
            var attrs = new List<FileAttribute>
            {
                new FileAttribute(FileAttributeType.Length, 185),
                new FileAttribute(FileAttributeType.BitRate, 320),
            };
            var file = new File(1, "test.mp3", 5 * 1024 * 1024, "mp3", attrs);
            string result = SimpleHelpers.GetSizeLengthAttrString(file);
            // should have size • time • bitrate
            var parts = result.Split('•');
            Assert.That(parts.Length, Is.EqualTo(3));
        }

        // --- SortSlskDirFiles ---

        [Test]
        public void SortSlskDirFiles_SortsByFilename()
        {
            var files = new List<File>
            {
                new File(1, "c.mp3", 100, "mp3"),
                new File(2, "a.mp3", 100, "mp3"),
                new File(3, "b.mp3", 100, "mp3"),
            };
            SimpleHelpers.SortSlskDirFiles(files);
            Assert.That(files[0].Filename, Is.EqualTo("a.mp3"));
            Assert.That(files[1].Filename, Is.EqualTo("b.mp3"));
            Assert.That(files[2].Filename, Is.EqualTo("c.mp3"));
        }

        // --- GetRecentTimeBucket ---

        [TestCase(0, RecentTimeUnit.JustNow, 0)]
        [TestCase(59, RecentTimeUnit.JustNow, 0)]
        [TestCase(60, RecentTimeUnit.Minutes, 1)]
        [TestCase(45 * 60, RecentTimeUnit.Minutes, 45)]
        [TestCase(60 * 60 - 1, RecentTimeUnit.Minutes, 59)]
        [TestCase(60 * 60, RecentTimeUnit.Hours, 1)]
        [TestCase(3 * 3600, RecentTimeUnit.Hours, 3)]
        [TestCase(24 * 3600 - 1, RecentTimeUnit.Hours, 23)]
        [TestCase(24 * 3600, RecentTimeUnit.Days, 1)]
        [TestCase(36 * 3600, RecentTimeUnit.Days, 1)]
        [TestCase(48 * 3600 - 1, RecentTimeUnit.Days, 1)]
        [TestCase(48 * 3600, RecentTimeUnit.Days, 2)]
        [TestCase(5 * 86400, RecentTimeUnit.Days, 5)]
        [TestCase(30 * 86400 - 1, RecentTimeUnit.Days, 29)]
        [TestCase(30 * 86400, RecentTimeUnit.AbsoluteDate, 0)]
        [TestCase(35 * 86400, RecentTimeUnit.AbsoluteDate, 0)]
        public void GetRecentTimeBucket_PicksUnitAndWholeCount(int totalSeconds, RecentTimeUnit expectedUnit, int expectedCount)
        {
            var (unit, count) = SimpleHelpers.GetRecentTimeBucket(TimeSpan.FromSeconds(totalSeconds));
            Assert.That(unit, Is.EqualTo(expectedUnit));
            Assert.That(count, Is.EqualTo(expectedCount));
        }

        // --- ToLocalTimeSafe ---

        [Test]
        public void ToLocalTimeSafe_ConvertsUtcToLocal()
        {
            var utc = new DateTime(2025, 4, 14, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(SimpleHelpers.ToLocalTimeSafe(utc), Is.EqualTo(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local)));
        }

        [Test]
        public void ToLocalTimeSafe_UnspecifiedKindIsTreatedAsUtc()
        {
            var utc = new DateTime(2025, 4, 14, 12, 0, 0, DateTimeKind.Utc);
            var unspecified = new DateTime(utc.Ticks);
            Assert.That(SimpleHelpers.ToLocalTimeSafe(unspecified), Is.EqualTo(SimpleHelpers.ToLocalTimeSafe(utc)));
        }

        // --- KNOWN_TYPES ---

        [Test]
        public void KnownTypes_ContainsExpectedExtensions()
        {
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Contain(".mp3"));
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Contain(".flac"));
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Contain(".wav"));
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Contain(".aiff"));
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Contain(".wma"));
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Contain(".aac"));
        }

        [Test]
        public void KnownTypes_DoesNotContainUnexpected()
        {
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Not.Contain(".ogg"));
            Assert.That(SimpleHelpers.KNOWN_TYPES, Does.Not.Contain(".opus"));
        }

        // --- GetFolderNameForSearchResult ---

        [Test]
        public void GetFolderNameForSearchResult_WithFiles_ReturnsFolderName()
        {
            var files = new List<File> { new File(1, @"user\Music\Album\song.mp3", 1000, "mp3") };
            var response = new SearchResponse("testuser", 1, true, 0, 0, files);
            string result = SimpleHelpers.GetFolderNameForSearchResult(response);
            Assert.That(result, Does.Contain("Album"));
        }

        [Test]
        public void GetFolderNameForSearchResult_OnlyLockedFiles_PrependsLockEmoji()
        {
            var lockedFiles = new List<File> { new File(1, @"user\Music\Album\song.mp3", 1000, "mp3") };
            var response = new SearchResponse("testuser", 1, true, 0, 0, new List<File>(), lockedFiles);
            string result = SimpleHelpers.GetFolderNameForSearchResult(response);
            Assert.That(result, Does.StartWith(SimpleHelpers.LOCK_EMOJI));
        }

        [Test]
        public void GetFolderNameForSearchResult_NoFiles_ReturnsLockedPlaceholder()
        {
            var response = new SearchResponse("testuser", 1, true, 0, 0, new List<File>());
            string result = SimpleHelpers.GetFolderNameForSearchResult(response);
            Assert.That(result, Is.EqualTo("\\Locked\\"));
        }

        // --- MagnetLinkRegex / SlskLinkRegex ---

        [Test]
        public void MagnetLinkRegex_MatchesValidLink()
        {
            var match = SimpleHelpers.MagnetLinkRegex.Match("get this magnet:?xt=urn:btih:abc123def456 now");
            Assert.That(match.Success, Is.True);
            Assert.That(match.Value, Is.EqualTo("magnet:?xt=urn:btih:abc123def456"));
        }

        [Test]
        public void SlskLinkRegex_MatchesValidLink()
        {
            var match = SimpleHelpers.SlskLinkRegex.Match("try slsk://username/file.mp3 ok");
            Assert.That(match.Success, Is.True);
            Assert.That(match.Value, Is.EqualTo("slsk://username/file.mp3"));
        }

        // --- Edge cases for GetHumanReadableTime format string correctness ---

        [Test]
        public void GetHumanReadableTime_WithoutSpace_NoSpacesBetweenComponents()
        {
            // 1h 1m 1s = 3661
            string result = SimpleHelpers.GetHumanReadableTime(3661);
            Assert.That(result, Is.EqualTo("1h1m1s"));
            // verify no spaces at all
            Assert.That(result, Does.Not.Contain(" "));
        }

        [Test]
        public void GetHumanReadableTime_WithSpace_SpaceBeforeEachUnit()
        {
            string result = SimpleHelpers.GetHumanReadableTime(3661, true);
            Assert.That(result, Is.EqualTo("1h 1m 1s"));
        }

        [Test]
        public void GetHumanReadableTime_SecondsOnly_WithoutSpace_FormatArgUnused()
        {
            string withoutSpace = SimpleHelpers.GetHumanReadableTime(45, false);
            string withSpace = SimpleHelpers.GetHumanReadableTime(45, true);
            Assert.That(withoutSpace, Is.EqualTo("45s"));
            Assert.That(withSpace, Is.EqualTo("45s"));
        }
    }
}
