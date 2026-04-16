using Common;
using NUnit.Framework;
using Seeker;
using Soulseek;
using System.Collections.Generic;
using System.Linq;

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

        private static List<ChipDataItem> Run(List<SearchResponse> responses)
        {
            return ChipsHelper.GetChipDataItemsFromSearchResults(responses, "search", FileTypesOnly());
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

            var result = Run(responses);
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

            Assert.IsTrue(result.All(c => c.ChipType == ChipType.FileType));
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

                var result = Run(responses);
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

                Assert.IsFalse(result.Any(c => c.HasTag()), "no (other) bucket expected at this size");
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

            var result = Run(responses);

            // 30 > 14 triggers grouping; with no bases, only the trailing tail-chop fires.
            Assert.AreEqual(14, result.Count);

            var last = result[13];
            Assert.AreEqual("other", last.BaseDisplayText);
            Assert.IsTrue(last.HasTag());
            Assert.AreEqual(17, last.Children.Count); // 30 - 13

            // Union of the 13 top-level chip labels + the "other" children equals the 30 inputs.
            var allInputs = Enumerable.Range(0, 30).Select(i => $"t{i:D2}").ToHashSet();
            var observed = new HashSet<string>(result.Take(13).Select(c => c.BaseDisplayText));
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

            var result = Run(responses);
            var texts = Texts(result);

            Assert.AreEqual(14, result.Count);

            // mp3 group occupies first 5 slots: "- all", 3 top variants, "(other)" with 4 rare variants
            Assert.AreEqual("mp3 - all", texts[0]);
            Assert.AreEqual("mp3 (other)", texts[4]);
            Assert.IsTrue(result[4].HasTag());
            Assert.AreEqual(4, result[4].Children.Count);

            // trailing "other" bucket with the overflow t## types
            var last = result[13];
            Assert.AreEqual("other", last.BaseDisplayText);
            Assert.IsTrue(last.HasTag());
            Assert.AreEqual(22, last.Children.Count);

            // All 30 t## inputs accounted for across visible chips + both "other" buckets
            var allTInputs = Enumerable.Range(0, 30).Select(i => $"t{i:D2}").ToHashSet();
            var observedT = new HashSet<string>();
            foreach (var chip in result)
            {
                if (chip.BaseDisplayText.StartsWith("t"))
                {
                    observedT.Add(chip.BaseDisplayText);
                }
                if (chip.HasTag())
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

            var result = Run(responses);
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
            var result = Run(new List<SearchResponse>());
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FileType_SingleResponseSingleType_SingleChip()
        {
            var result = Run(MakeResponses(("mp3", 1)));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("mp3", result[0].BaseDisplayText);
            Assert.IsFalse(result[0].HasTag());
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

            var result = Run(responses);

            Assert.AreEqual(15, result.Count);
            Assert.IsFalse(result.Any(c => c.BaseDisplayText == "other"));
            Assert.AreEqual("topper", result[0].BaseDisplayText); // highest count sorts first
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
            Assert.DoesNotThrow(() => result = Run(responses));
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
            Assert.DoesNotThrow(() => result = Run(responses));
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
            Assert.DoesNotThrow(() => result = Run(responses));
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
    }
}