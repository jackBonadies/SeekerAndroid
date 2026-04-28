using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    public class SimpleHelpersTest
    {
        [SetUp]
        public void Setup()
        {
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
            Assert.That(result, Is.EqualTo("0 MB"));
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
            string result = SimpleHelpers.GetFileNameFromFile(@"music\artist\song.mp3");
            Assert.That(result, Is.EqualTo("song.mp3"));
        }

        [Test]
        public void GetFileNameFromFile_NoBackslash()
        {
            // LastIndexOf returns -1, Substring(0) returns the whole string
            string result = SimpleHelpers.GetFileNameFromFile("song.mp3");
            Assert.That(result, Is.EqualTo("song.mp3"));
        }

        [Test]
        public void GetFileNameFromFile_TrailingBackslash_ReturnsEmpty()
        {
            string result = SimpleHelpers.GetFileNameFromFile(@"music\artist\");
            Assert.That(result, Is.EqualTo(""));
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

        // --- GetRecentTimeNiceFormated ---

        [Test]
        public void GetRecentTimeNiceFormated_UnderOneMinute_ReturnsJustNow()
        {
            string result = SimpleHelpers.GetRecentTimeNiceFormated(DateTime.Now, TimeSpan.FromSeconds(30), "just now", "min ago", "hr ago", "yesterday", "days ago");
            Assert.That(result, Is.EqualTo("just now"));
        }

        [Test]
        public void GetRecentTimeNiceFormated_45Minutes_ReturnsMinAgo()
        {
            string result = SimpleHelpers.GetRecentTimeNiceFormated(DateTime.Now, TimeSpan.FromMinutes(45), "just now", "min ago", "hr ago", "yesterday", "days ago");
            Assert.That(result, Is.EqualTo("45 min ago"));
        }

        [Test]
        public void GetRecentTimeNiceFormated_3Hours_ReturnsHrAgo()
        {
            string result = SimpleHelpers.GetRecentTimeNiceFormated(DateTime.Now, TimeSpan.FromHours(3), "just now", "min ago", "hr ago", "yesterday", "days ago");
            Assert.That(result, Is.EqualTo("3 hr ago"));
        }

        [Test]
        public void GetRecentTimeNiceFormated_36Hours_ReturnsYesterday()
        {
            string result = SimpleHelpers.GetRecentTimeNiceFormated(DateTime.Now, TimeSpan.FromHours(36), "just now", "min ago", "hr ago", "yesterday", "days ago");
            Assert.That(result, Is.EqualTo("yesterday"));
        }

        [Test]
        public void GetRecentTimeNiceFormated_5Days_ReturnsDaysAgo()
        {
            string result = SimpleHelpers.GetRecentTimeNiceFormated(DateTime.Now, TimeSpan.FromDays(5), "just now", "min ago", "hr ago", "yesterday", "days ago");
            Assert.That(result, Is.EqualTo("5 days ago"));
        }

        [Test]
        public void GetRecentTimeNiceFormated_OverOneMonth_ReturnsFormattedDate()
        {
            var timeRan = new DateTime(2025, 4, 14);
            string result = SimpleHelpers.GetRecentTimeNiceFormated(timeRan, TimeSpan.FromDays(35), "just now", "min ago", "hr ago", "yesterday", "days ago");
            Assert.That(result, Is.EqualTo("Apr 14"));
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
