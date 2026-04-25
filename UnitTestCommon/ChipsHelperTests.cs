using Common;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
using NUnit.Framework;
using Seeker;
using Seeker.Debug;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTestCommon
{
    [TestFixture]
    public class ChipsHelperTests
    {
        private bool _origHideLocked;
        private PreferencesState.SmartFilterState _origSmartFilter;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SimpleHelpers.STRINGS_KBS = "kbs";
            SimpleHelpers.STRINGS_KHZ = "kHz";
        }

        [SetUp]
        public void SetUp()
        {
            _origHideLocked = PreferencesState.HideLockedResultsInSearch;
            _origSmartFilter = PreferencesState.SmartFilterOptions;
            PreferencesState.HideLockedResultsInSearch = true;
        }

        [TearDown]
        public void TearDown()
        {
            PreferencesState.HideLockedResultsInSearch = _origHideLocked;
            PreferencesState.SmartFilterOptions = _origSmartFilter;
        }

        private static SearchResponse MakeResponse(string cachedType, double cachedBitRate = 128.0)
        {
            var dummyFile = new File(1, @"\\u\a.mp3", 1000, "mp3");
            var resp = new SearchResponse("testuser", 1, true, 5000, 0, new[] { dummyFile });
            resp.cachedDominantFileType = cachedType;
            resp.cachedCalcBitRate = cachedBitRate;
            return resp;
        }

        private static List<SearchResponse> MakeResponses(params (string type, int count)[] buckets)
        {
            var list = new List<SearchResponse>();
            foreach (var (t, c) in buckets)
            {
                for (int i = 0; i < c; i++)
                {
                    list.Add(MakeResponse(t));
                }
            }
            return list;
        }

        private static SearchResponse MakeResponseWithFiles(int fileCount)
        {
            var files = new File[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                files[i] = new File(1, $@"\\u\a{i}.mp3", 1000, "mp3");
            }
            return new SearchResponse("testuser", 1, true, 5000, 0, files);
        }

        private static List<SearchResponse> MakeResponsesWithFileCounts(params (int fileCount, int times)[] buckets)
        {
            var list = new List<SearchResponse>();
            foreach (var (fc, times) in buckets)
            {
                for (int i = 0; i < times; i++)
                {
                    list.Add(MakeResponseWithFiles(fc));
                }
            }
            return list;
        }

        private static PreferencesState.SmartFilterState FileTypesOnly()
        {
            return new PreferencesState.SmartFilterState
            {
                FileTypesEnabled = true,
                FileTypesOrder = 0,
                NumFilesEnabled = false,
                KeywordsEnabled = false,
            };
        }

        private static PreferencesState.SmartFilterState FileCountsOnly()
        {
            return new PreferencesState.SmartFilterState
            {
                FileTypesEnabled = false,
                NumFilesEnabled = true,
                KeywordsEnabled = false,
            };
        }

        private static PreferencesState.SmartFilterState KeywordsOnly()
        {
            return new PreferencesState.SmartFilterState
            {
                FileTypesEnabled = false,
                NumFilesEnabled = false,
                KeywordsEnabled = true,
            };
        }

        private static PreferencesState.SmartFilterState All()
        {
            return new PreferencesState.SmartFilterState
            {
                FileTypesEnabled = true,
                NumFilesEnabled = true,
                KeywordsEnabled = true,
            };
        }

        private static List<ChipDataItem> RunFileTypes(List<SearchResponse> responses )
        {
            return ChipsHelper.GetChipDataItemsFromSearchResults(responses, "search", FileTypesOnly());
        }

        private static List<ChipDataItem> RunFileCounts(List<SearchResponse> responses )
        {
            return ChipsHelper.GetChipDataItemsFromSearchResults(responses, "search", FileCountsOnly());
        }

        private static List<ChipDataItem> RunKeywords(List<SearchResponse> responses, string search)
        {
            return ChipsHelper.GetChipDataItemsFromSearchResults(responses, search, KeywordsOnly());
        }

        private static List<ChipDataItem> RunAll(List<SearchResponse> responses, string search)
        {
            return ChipsHelper.GetChipDataItemsFromSearchResults(responses, search, All());
        }

        private static List<string> Texts(List<ChipDataItem> chips)
        {
            return chips.Select(c => c.GetFullDisplayText()).ToList();
        }

        // --- Scenario tests --------------------------------------------------

        [Test]
        public void FileType_FlacVariantsAndFlacxx_GroupsFlacUnderAll()
        {
            var responses = MakeResponses(
                ("flac", 3),
                ("flac (vbr)", 5),
                ("flac (16, 44.1kHz)", 2),
                ("flac (192, 24kHz)", 1),
                ("flacxx", 4));

            var result = RunFileTypes(responses);
            var texts = Texts(result);

            // plain "flac" is removed when "flac - all" is introduced
            Assert.IsFalse(texts.Contains("flac"), "plain 'flac' should be removed once 'flac - all' exists");
            Assert.IsTrue(texts.Contains("flac - all"));
            Assert.IsTrue(texts.Contains("flacxx"), "'flacxx' must not be grouped under 'flac'");

            // "flac - all" must come before its variants and they must be contiguous
            int allIdx = texts.IndexOf("flac - all");
            Assert.AreEqual(0, allIdx, "'flac - all' is the largest group and should sort first");
            Assert.AreEqual("flac (vbr)", texts[1]);
            Assert.AreEqual("flac (16, 44.1kHz)", texts[2]);
            Assert.AreEqual("flac (192, 24kHz)", texts[3]);
            Assert.AreEqual("flacxx", texts[4]);
            Assert.AreEqual(5, result.Count);

            Assert.IsTrue(result.All(c => c is FileTypeChipDataItem));
        }

        [Test]
        public void FileType_RealisticMix_ProducesExpectedOrder()
        {
            for (int i = 0; i < 2; i++)
            {
                var responses = MakeResponses(
                    ("mp3 (320kbs)", 40),
                    ("mp3 (vbr)", 20),
                    ("mp3 (192kbs)", 10),
                    ("flac (16, 44.1kHz)", 15),
                    ("flac (24, 96kHz)", 3),
                    ("m4a", 6),
                    ("ogg", 2),
                    ("wav", 1));

                if (i == 1)
                {
                    responses.Reverse();
                }

                var result = RunFileTypes(responses);
                var texts = Texts(result);


                // 8 distinct + 2 "- all" chips = 10 total, no (other) bucketing (<=14).
                Assert.AreEqual(10, result.Count);

                Assert.AreEqual("mp3 - all", texts[0]);
                Assert.AreEqual("mp3 (320kbs)", texts[1]);
                Assert.AreEqual("mp3 (vbr)", texts[2]);
                Assert.AreEqual("mp3 (192kbs)", texts[3]);
                Assert.AreEqual("flac - all", texts[4]);
                Assert.AreEqual("flac (16, 44.1kHz)", texts[5]);
                Assert.AreEqual("flac (24, 96kHz)", texts[6]);
                Assert.AreEqual("m4a", texts[7]);
                Assert.AreEqual("ogg", texts[8]);
                Assert.AreEqual("wav", texts[9]);

                Assert.IsFalse(result.Any(c => ((FileTypeChipDataItem)c).IsOtherCase), "no (other) bucket expected at this size");
            }
        }

        [Test]
        public void FileType_ThirtyOneOffTypes_TriggersTrailingOtherBucket()
        {
            var buckets = new (string, int)[30];
            for (int i = 0; i < 30; i++)
            {
                buckets[i] = ($"t{i:D2}", 1);
            }
            var responses = MakeResponses(buckets);

            var result = RunFileTypes(responses);

            // 30 > 14 triggers grouping; with no bases, only the trailing tail-chop fires.
            Assert.AreEqual(14, result.Count);

            var last = (FileTypeChipDataItem)result[13];
            Assert.AreEqual("other", last.BaseFileType);
            Assert.IsTrue(last.IsOtherCase);
            Assert.AreEqual(17, last.Children.Count); // 30 - 13

            // Union of the 13 top-level chip labels + the "other" children equals the 30 inputs.
            var allInputs = Enumerable.Range(0, 30).Select(i => $"t{i:D2}").ToHashSet();
            var observed = new HashSet<string>(result.Take(13).Select(c => ((FileTypeChipDataItem)c).BaseFileType));
            observed.UnionWith(last.Children);
            Assert.IsTrue(allInputs.SetEquals(observed));
        }


        [Test]
        public void FileType_ManyTypes_TriggersTrailingOtherVariantBucket()
        {
            var buckets = new (string, int)[30];
            for (int i = 0; i < 30; i++)
            {
                buckets[i] = ($"t{i:D2}", 1);
            }
            var mp3Buckets = new (string, int)[] { 
                ("mp3", 10),
                ("mp3 (vbr)", 10),
                ("mp3 (320)", 10),
                ("mp3 (256)", 10),
                ("mp3 (196)", 1),
                ("mp3 (128)", 1),
                ("mp3 (120)", 1),
                ("mp3 (96)", 1),
            };
            
            var responses = MakeResponses(buckets.Concat(mp3Buckets).ToArray());

            var result = RunFileTypes(responses);
            var texts = Texts(result);

            Assert.AreEqual(14, result.Count);

            // mp3 group occupies first 5 slots: "- all", 3 top variants, "(other)" with 4 rare variants
            Assert.AreEqual("mp3 - all", texts[0]);
            Assert.AreEqual("mp3 (other)", texts[4]);
            var mp3Other = (FileTypeChipDataItem)result[4];
            Assert.IsTrue(mp3Other.IsOtherCase);
            Assert.AreEqual(4, mp3Other.Children.Count);

            // trailing "other" bucket with the overflow t## types
            var last = (FileTypeChipDataItem)result[13];
            Assert.AreEqual("other", last.BaseFileType);
            Assert.IsTrue(last.IsOtherCase);
            Assert.AreEqual(22, last.Children.Count);

            // All 30 t## inputs accounted for across visible chips + both "other" buckets
            var allTInputs = Enumerable.Range(0, 30).Select(i => $"t{i:D2}").ToHashSet();
            var observedT = new HashSet<string>();
            foreach (var chip in result.Cast<FileTypeChipDataItem>())
            {
                if (chip.BaseFileType.StartsWith("t"))
                {
                    observedT.Add(chip.BaseFileType);
                }
                if (chip.IsOtherCase)
                {
                    foreach (var child in chip.Children)
                    {
                        if (child.StartsWith("t"))
                        {
                            observedT.Add(child);
                        }
                    }
                }
            }
            Assert.IsTrue(allTInputs.SetEquals(observedT));
        }


        [Test]
        public void FileType_OverwhelminglyDominant_NoAllChipCreatedForSingleVariant()
        {
            var responses = MakeResponses(
                ("mp3 (320kbs)", 1000),
                ("flac", 2),
                ("ogg", 1));

            var result = RunFileTypes(responses);
            var texts = Texts(result);

            Assert.AreEqual(3, result.Count);
            // Only one "mp3 (...)" variant, so no "mp3 - all" chip is created.
            Assert.IsFalse(texts.Any(t => t.EndsWith(" - all")));
            Assert.AreEqual("mp3 (320kbs)", texts[0]);
            Assert.AreEqual("flac", texts[1]);
            Assert.AreEqual("ogg", texts[2]);
        }

        // --- Tricky / bug-hunting cases --------------------------------------

        [Test]
        public void FileType_EmptyResponses_ReturnsEmptyList()
        {
            var result = RunFileTypes(new List<SearchResponse>());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FileType_SingleResponseSingleType_SingleChip()
        {
            var result = RunFileTypes(MakeResponses(("mp3", 1)));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("mp3", ((FileTypeChipDataItem)result[0]).BaseFileType);
            Assert.IsFalse(((FileTypeChipDataItem)result[0]).IsOtherCase);
        }

        [Test]
        public void FileType_ExactlyFifteenTypes_NoTrailingOtherBucket()
        {
            // 14 one-offs + 1 with count 2 = 15 distinct types.
            // Count > 14 enters grouping branch; Count > 15 (tail chop) is FALSE so nothing is chopped.
            var buckets = new List<(string, int)>();
            for (int i = 0; i < 14; i++)
            {
                buckets.Add(($"t{i:D2}", 1));
            }
            buckets.Add(("topper", 2));
            var responses = MakeResponses(buckets.ToArray());

            var result = RunFileTypes(responses);

            Assert.AreEqual(15, result.Count);
            Assert.IsFalse(result.Any(c => ((FileTypeChipDataItem)c).BaseFileType == "other"));
            Assert.AreEqual("topper", ((FileTypeChipDataItem)result[0]).BaseFileType); // highest count sorts first
        }

        [Test]
        public void FileType_BaseWithManyVariantsPlusSingletons_DoesNotThrow()
        {
            // 1 base with 8 variants (triggers mp3 (other) sub-grouping) plus 10 singletons.
            // Goal: verify the range arithmetic at ChipsHelper.cs:180-187 and 193-222
            // doesn't blow up when multiple consolidation passes run back-to-back.
            var buckets = new List<(string, int)>
            {
                ("mp3 (320kbs)", 100),
                ("mp3 (vbr)", 40),
                ("mp3 (256kbs)", 30),
                ("mp3 (192kbs)", 20),
                ("mp3 (160kbs)", 15),
                ("mp3 (128kbs)", 2),
                ("mp3 (96kbs)", 2),
                ("mp3 (64kbs)", 1),
            };
            for (int i = 0; i < 10; i++)
            {
                buckets.Add(($"ext{i:D2}", 1));
            }
            var responses = MakeResponses(buckets.ToArray());

            List<ChipDataItem> result = null;
            Assert.DoesNotThrow(() => result = RunFileTypes(responses));
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Any(c => c.GetFullDisplayText() == "mp3 - all"),
                "'mp3 - all' chip must exist with 8 mp3 variants");
        }

        [Test]
        public void FileType_UnmatchedAndEdgeParenthesisStrings_DoesNotThrow()
        {
            // Pathological type strings: unmatched paren, and a type that is exactly "x (".
            // IndexOf(" (") returns a valid index, so base extraction at line 75 produces
            // "weird" and "x" respectively - harmless, but worth guarding against.
            var responses = MakeResponses(
                ("weird (thing", 3),
                ("x (", 2),
                ("mp3", 5));

            List<ChipDataItem> result = null;
            Assert.DoesNotThrow(() => result = RunFileTypes(responses));
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(Texts(result).Contains("mp3"));
        }

        [Test]
        public void FileType_LeadingSpaceParenYieldsEmptyBase_DoesNotThrow()
        {
            // " (oops)" - IndexOf(" (") == 0 so base is "". The empty base goes into
            // fileTypeBases and is compared with Contains("" + " ") == Contains(" "),
            // which matches any type containing a space. The method must still not crash.
            var responses = MakeResponses(
                (" (oops)", 3),
                ("mp3 (vbr)", 5),
                ("mp3 (320kbs)", 10),
                ("flac", 2));

            List<ChipDataItem> result = null;
            Assert.DoesNotThrow(() => result = RunFileTypes(responses));
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void FileType_NullCachedTypeStillHandled_DoesNotThrow()
        {
            // If cachedDominantFileType is null, the parser falls through and reads the
            // dummy file's extension (.mp3). Make sure ChipsHelper tolerates this mixed
            // scenario where some responses have cache set and others don't.
            var withCache = MakeResponse("flac (vbr)");
            var dummyFile = new File(1, @"\\u\a.mp3", 1000, "mp3");
            var withoutCache = new SearchResponse("u", 1, true, 1000, 0, new[] { dummyFile });

            List<ChipDataItem> result = null;
            Assert.DoesNotThrow(() =>
                result = ChipsHelper.GetChipDataItemsFromSearchResults(
                    new List<SearchResponse> { withCache, withoutCache },
                    "search",
                    FileTypesOnly()));
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }
        
        // --- File Count Tests ---------------------------------------------------

        private static string Serialize(List<string> chips)
        {
            return "[\n" + string.Join(",\n", chips.Select(c => "  " + c)) + "\n]";
        }

        [Test]
        public void FileCount_FewDistinctValues_AllWhales()
        {
            var responses = MakeResponsesWithFileCounts((1, 110), (3, 200), (10, 80));
            Assert.AreEqual(@"[
  1 file,
  3 files,
  10 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_WhaleWithMinnows_UserExample()
        {
            var responses = MakeResponsesWithFileCounts(
                (1, 1000), (2, 4), (3, 5), (6, 7), (9, 100));
            Assert.AreEqual(@"[
  1 file,
  2 to 9 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_TwoWhales_MinnowsBetween()
        {
            var responses = MakeResponsesWithFileCounts(
                (1, 300), (2, 5), (3, 8), (7, 2), (10, 250), (11, 3), (20, 200));
            Assert.AreEqual(@"[
  1 file,
  2 to 7 files,
  10 files,
  11 files,
  20 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_SingleDominantWhale_AllOthersMinnows()
        {
            var responses = MakeResponsesWithFileCounts(
                (1, 900), (2, 3), (5, 2), (8, 1), (10, 4));
            Assert.AreEqual(@"[
  1 file,
  2 to 10 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_MinnowPool_HardSplitAt18Percent()
        {
            var responses = MakeResponsesWithFileCounts(
                (1, 50), (2, 50), (3, 50), (4, 50), (5, 1),
                (10, 50), (11, 50), (12, 50), (13, 50),
                (20, 50), (21, 50));
            Assert.AreEqual(@"[
  1 to 2 files,
  3 to 5 files,
  10 to 11 files,
  12 to 13 files,
  20 to 21 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_SoftZone_SplitsAtNaturalGap()
        {
            var responses = MakeResponsesWithFileCounts(
                (10, 40), (11, 40), (12, 40), (20, 40), (21, 40), (22, 40));
            Assert.AreEqual(@"[
  10 files,
  11 files,
  12 files,
  20 files,
  21 files,
  22 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_SoftZone_SplitsAtNaturalGap2()
        {
            var responses = MakeResponsesWithFileCounts(
                (1, 1), (2, 1), (4, 1), (5, 100), (6, 1), (7, 1));
            Assert.AreEqual(@"[
  1 to 4 files,
  5 files,
  6 to 7 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_AllUniform_PoolSplitsReasonably()
        {
            var buckets = new (int, int)[100];
            for (int i = 0; i < 100; i++)
            {
                buckets[i] = (i + 1, 1);
            }
            var responses = MakeResponsesWithFileCounts(buckets);
            Assert.AreEqual(@"[
  1 to 24 files,
  25 to 48 files,
  49 to 72 files,
  73 to 96 files,
  97 to 100 files
]", Serialize(Texts(RunFileCounts(responses))));
        }

        [Test]
        public void FileCount_EndToEnd_ViaSearchResponses()
        {
            var responses = MakeResponsesWithFileCounts(
                (1, 199), (10, 200), (11, 200), (119, 100), (120, 101));
            Assert.AreEqual(@"[
  1 file,
  10 files,
  11 files,
  119 files,
  120 files
]", Serialize(Texts(RunFileCounts(responses))));
        }


        private static IEnumerable<TestCaseData> SearchResponseFiles()
        {
            string dir = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "SearchResponse");
            foreach (var file in System.IO.Directory.GetFiles(dir))
            {
                yield return new TestCaseData(file).SetName("Keywords_RealData_" + System.IO.Path.GetFileNameWithoutExtension(file));
            }
        }

        // --- keyword test ---
        [Test, TestCaseSource(nameof(SearchResponseFiles))]
        public void Keywords_RealData(string fileName)
        {
            var searchResponseFolder = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "SearchResponse");
            var answerDirectory = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "SearchResponseKeywordAnswers");
            CaptureTestHelper.Configure(searchResponseFolder);
            string fullFilename = System.IO.Path.Combine(searchResponseFolder, fileName);
            var capture = CaptureTestHelper.TryLoad(fullFilename);
            List<SearchResponse> responses = new List<SearchResponse>();
            for (int i = 0; i < capture.Responses.Count; i++)
            {
                var result = Common.SearchResponseUtil.SplitMultiDirResponse(PreferencesState.HideLockedResultsInSearch, capture.Responses.ElementAt(i));
                if (result.Item1)
                {
                    foreach (var r in result.Item2)
                    {
                        responses.Add(r);
                    }
                }
                else
                {
                    responses.Add(capture.Responses[i]);
                }
            }
            var keywordChips = RunKeywords(responses, capture.Query);
            var keywords = string.Join(';',keywordChips.Select(it => (it as KeywordChipDataItem).Keyword));

            var encryptedAnswer = System.IO.File.ReadAllBytes(System.IO.Path.Combine(answerDirectory, System.IO.Path.GetFileNameWithoutExtension(fileName) + ".answer"));
            var answer = Encoding.UTF8.GetString(new EncryptedFileHelper().Decrypt(encryptedAnswer));//Encoding.UTF8.GetBytes(keywords));
            //var enryptedAnswer = new EncryptedFileHelper().Encrypt(Encoding.UTF8.GetBytes(keywords));
            //System.IO.File.WriteAllBytes(System.IO.Path.Combine(@"C:\seeker_newest\UnitTestCommon\TestData\SearchResponseKeywordAnswers", System.IO.Path.GetFileNameWithoutExtension(fileName) + ".answer"), enryptedAnswer);
            Assert.AreEqual(answer, keywords);
            Console.WriteLine(capture.Query);
            Console.WriteLine(keywords);
        }

        private static SearchResponse MakeResponseWithFolder(string folderName)
        {
            var file = new File(1, folderName + @"\track.mp3", 1000, "mp3");
            return new SearchResponse("testuser", 1, true, 5000, 0, new[] { file });
        }

        private static SearchResponse MakeLockedOnlyResponseWithFolder(string folderName)
        {
            var file = new File(1, folderName + @"\track.mp3", 1000, "mp3");
            return new SearchResponse("testuser", 1, true, 5000, 0, Array.Empty<File>(), new[] { file });
        }

        private static List<string> KeywordTexts(List<ChipDataItem> chips)
        {
            return chips.Select(c => ((KeywordChipDataItem)c).Keyword).ToList();
        }

        [Test]
        public void Keywords_Bug_CaseMismatch_DisplayNameUsesFirstInsertedCasingNotMostCommon()
        {
            var responses = new List<SearchResponse>();
            for (int i = 0; i < 3; i++)
            {
                responses.Add(MakeResponseWithFolder("The Helloworlds"));
            }
            for (int i = 0; i < 10; i++)
            {
                responses.Add(MakeResponseWithFolder("the helloworlds"));
            }

            var chips = RunKeywords(responses, "album");
            var keywords = KeywordTexts(chips);

            Assert.Greater(keywords.Count, 0, "expected at least one keyword chip");
            Assert.AreEqual("the helloworlds", keywords[0],
                "display for the top keyword should reflect the most common casing (10 lowercase vs 3 title-case)");
        }

        [Test]
        public void Keywords_Bug_YearIntegerDivision_WipesLowCountYears()
        {
            var responses = new List<SearchResponse>();
            for (int i = 0; i < 3; i++)
            {
                responses.Add(MakeResponseWithFolder("Album (1994)"));
            }
            for (int i = 0; i < 7; i++)
            {
                responses.Add(MakeResponseWithFolder("Album (1999)"));
            }
            responses.Add(MakeResponseWithFolder("Album (Remastered)"));

            var chips = RunKeywords(responses, "search");
            var keywords = KeywordTexts(chips);

            int year2Idx = keywords.IndexOf("1999");
            int yearIdx = keywords.IndexOf("1994");
            int remasterIdx = keywords.IndexOf("Remastered");

            Assert.Greater(yearIdx, -1, "'1994' should appear in the keyword chips");
            Assert.Greater(remasterIdx, -1, "'Remastered' should appear in the keyword chips");
            Assert.Less(year2Idx, remasterIdx,
                "'1999' (count 7/4) should rank higher than 'Remastered' (count 1)");
        }

        [Test]
        public void Keywords_Bug_IgnoresHideLockedPreference_MinesLockedOnlyResponses()
        {
            PreferencesState.HideLockedResultsInSearch = true;

            var responses = new List<SearchResponse>();
            for (int i = 0; i < 5; i++)
            {
                responses.Add(MakeResponseWithFolder("NormalZone"));
            }
            for (int i = 0; i < 10; i++)
            {
                responses.Add(MakeLockedOnlyResponseWithFolder("LockedOnlyZone"));
            }

            var chips = RunKeywords(responses, "search");
            var keywords = KeywordTexts(chips);

            Assert.IsFalse(keywords.Contains("LockedOnlyZone"),
                "locked-only responses must not contribute keywords when HideLockedResultsInSearch=true");
        }
    }

    [TestFixture]
    public class KeywordHelper_IsSingleFileAttributeTypeTests
    {
        [TestCase("mp3")]
        [TestCase("flac")]
        [TestCase("wav")]
        [TestCase("wma")]
        [TestCase("aac")]
        [TestCase("mp4")]
        [TestCase("aiff")]
        [TestCase("ogg")]
        [TestCase("opus")]
        [TestCase("320")]
        [TestCase("192k")]
        [TestCase("mp3 320")]
        [TestCase("mp3 192")]
        [TestCase("mp3 v0")]
        [TestCase("mp3 128")]
        [TestCase("320 kbps")]
        [TestCase("320kbps")]
        [TestCase("m4a 128")]
        [TestCase("v0")]
        [TestCase("mp3 320kbps")]
        [TestCase("@192")]
        [TestCase("@320")]
        [TestCase("flac 24bit")]
        [TestCase("mp3 320 44")]
        public void ExistingAttributes_AreClassifiedTrue(string term)
        {
            Assert.IsTrue(Seeker.ChipsHelper.KeywordHelper.IsSingleFileAttributeType(term),
                $"\"{term}\" should be classified as a file attribute");
        }

        [TestCase("mp3 160")]
        [TestCase("mp3 224")]
        [TestCase("opus 128")]
        [TestCase("m4a 256")]
        [TestCase("mp3 320 cbr")]
        [TestCase("flac 16/44")]
        [TestCase("flac 16-44.1")]
        [TestCase("mp3 cbr")]
        [TestCase("mp3 vbr")]
        [TestCase("aac 256k")]
        public void LongTailAttributes_AreClassifiedTrue(string term)
        {
            Assert.IsTrue(Seeker.ChipsHelper.KeywordHelper.IsSingleFileAttributeType(term),
                $"\"{term}\" should be classified as a file attribute");
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("cant hold these mp3")]
        [TestCase("mp3 1995")]
        [TestCase("mp3 live")]
        [TestCase("greatest hits")]
        [TestCase("remastered")]
        [TestCase("mp3 flac")]
        [TestCase("mp3 123")]
        [TestCase("mp3 999")]
        [TestCase("mp3 5")]
        [TestCase("flac 100")]
        [TestCase("123")]
        [TestCase("999")]
        [TestCase("42")]
        public void NonAttributes_AreClassifiedFalse(string term)
        {
            Assert.IsFalse(Seeker.ChipsHelper.KeywordHelper.IsSingleFileAttributeType(term),
                $"\"{term}\" should not be classified as a file attribute");
        }
    }
}