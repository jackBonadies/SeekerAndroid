using Common;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Seeker
{
    public static class ChipsHelper
    {
        public const string ALL_SUFFIX = " - all";
        private const int MAX_FILENUM_BUCKETS = 5;
        private const int MAX_KEYWORD_CHIPS = 11;

        /// <summary>
        /// If hide hidden is true then for counts only consider unlocked. else consider everything.
        /// </summary>
        /// <param name="responses"></param>
        /// <param name="searchTerm"></param>
        /// <param name="smartFilterOptions"></param>
        /// <returns></returns>
        public static List<ChipDataItem> GetChipDataItemsFromSearchResults(List<Soulseek.SearchResponse> responses, string searchTerm, PreferencesState.SmartFilterState smartFilterOptions, bool hideHidden)
        {
            Dictionary<ChipType, IEnumerable<ChipDataItem>> finalData = new Dictionary<ChipType, IEnumerable<ChipDataItem>>();

            if (smartFilterOptions.FileTypesEnabled || smartFilterOptions.NumFilesEnabled)
            {
                Dictionary<string, int> fullFileTypeCounts;
                Dictionary<int, int> fileCountCounts;
                int totalSearchResultCount;
                getFileStatistics(responses, hideHidden, out fullFileTypeCounts, out fileCountCounts, out totalSearchResultCount);

                if (smartFilterOptions.NumFilesEnabled)
                {
                    finalData[ChipType.FileCount] = generateFileCountChips(fileCountCounts, totalSearchResultCount);
                }

                if (smartFilterOptions.FileTypesEnabled)
                {
                    calculateAndAddBaseStatsFromVariants(fullFileTypeCounts);

                    List<string> sortedGroupedFileTypes = sortAndGroupFileTypes(fullFileTypeCounts);

                    var chipsListFileTypes = sortedGroupedFileTypes.Select(
                        (Func<string, ChipDataItem>)(str => str.EndsWith(ALL_SUFFIX) ? new FileTypeChipDataItem(str.Substring(0, str.Length - ALL_SUFFIX.Length), true) : new FileTypeChipDataItem(str))).ToList();

                    condenseFileTypeChips(fullFileTypeCounts, chipsListFileTypes);
                    finalData[ChipType.FileType] = chipsListFileTypes;
                }
            }

            if (smartFilterOptions.KeywordsEnabled)
            {
                var chipKeywords = new List<KeywordChipDataItem>();
                var keywords = getKeywords(responses, searchTerm, hideHidden);
                foreach (var keyword in keywords)
                {
                    if (keyword.invariantsCollection != null)
                    {
                        chipKeywords.Add(new KeywordChipDataItem(keyword.displayKeyword, keyword.invariantsCollection));
                    }
                    else
                    {
                        chipKeywords.Add(new KeywordChipDataItem(keyword.displayKeyword));
                    }
                }
                finalData[ChipType.Keyword] = chipKeywords;
            }

            List<ChipDataItem> finalItems = new List<ChipDataItem>();
            var enabledOrder = smartFilterOptions.GetEnabledOrder();
            for (int i = 0; i < enabledOrder.Count; i++)
            {
                finalItems.AddRange(finalData[enabledOrder[i]]);
                if (i < enabledOrder.Count - 1 && finalData[enabledOrder[i]].Count() > 0)
                {
                    finalItems[finalItems.Count - 1].LastInGroup = true;
                }
            }

            return finalItems;

            static void calculateAndAddBaseStatsFromVariants(Dictionary<string, int> fullFileTypeCounts)
            {
                // do any bases such as "flac" have multiple file types under them (i.e. flac (vbr), flac (16, 44.1))
                var baseStats = new Dictionary<string, (int count, int total)>();
                foreach (var kvp in fullFileTypeCounts)
                {
                    string baseName = kvp.Key;
                    int index = baseName.IndexOf(" (");
                    if (index != -1)
                    {
                        baseName = baseName.Substring(0, index);
                    }

                    if (baseStats.TryGetValue(baseName, out var stats))
                    {
                        baseStats[baseName] = (stats.count + 1, stats.total + kvp.Value);
                    }
                    else
                    {
                        baseStats[baseName] = (1, kvp.Value);
                    }
                }

                // consolidate multiple variant bases into a "- all"
                foreach (var kvp in baseStats)
                {
                    if (kvp.Value.count > 1)
                    {
                        // remove "flac" if it exists
                        fullFileTypeCounts.Remove(kvp.Key);
                        // replace with flac - all
                        fullFileTypeCounts[kvp.Key + ALL_SUFFIX] = kvp.Value.total;
                    }
                }
            }

            static void getFileStatistics(List<Soulseek.SearchResponse> responses, bool hideHidden, out Dictionary<string, int> fullFileTypeCounts, out Dictionary<int, int> fileCountCounts, out int totalSearchResultCount)
            {
                fullFileTypeCounts = new Dictionary<string, int>();
                fileCountCounts = new Dictionary<int, int>();
                totalSearchResultCount = responses.Count;
                for (int i = 0; i < totalSearchResultCount; i++)
                {
                    //the search is done at this point, so search responses will not be changed.
                    var searchResponse = responses[i];

                    // i.e. mp3 (vbr)
                    string fullFileType = searchResponse.GetDominantFileTypeAndBitRate(hideHidden, out _);
                    if (!string.IsNullOrEmpty(fullFileType))
                    {
                        if (fullFileTypeCounts.TryGetValue(fullFileType, out int count))
                        {
                            fullFileTypeCounts[fullFileType] = count + 1;
                        } 
                        else
                        {
                            fullFileTypeCounts[fullFileType] = 1;
                        }
                    }

                    int fcount = hideHidden ? searchResponse.FileCount : searchResponse.FileCount + searchResponse.LockedFileCount;
                    if (fileCountCounts.TryGetValue(fcount, out int fcountCurrent))
                    {
                        fileCountCounts[fcount] = fcountCurrent + 1;
                    }
                    else
                    {
                        fileCountCounts[fcount] = 1;
                    }
                }
            }
        }

        private static List<FileCountChipDataItem> generateFileCountChips(Dictionary<int, int> fileCountCounts, int totalSearchResultCount)
        {
            var sorted = fileCountCounts.ToList();
            sorted.Sort((x, y) => x.Key.CompareTo(y.Key));

            var groups = SeparateWhalesAndPoolMinnows(sorted, totalSearchResultCount);

            var chips = new List<FileCountChipDataItem>();
            foreach (var group in groups)
            {
                chips.Add(new FileCountChipDataItem(group.MinValue, group.MaxValue));
            }

            return chips;
        }


        private static List<string> sortAndGroupFileTypes(Dictionary<string, int> fullFileTypeCounts)
        {
            // now sort and group variants so they are together
            var sortedListPass1 = fullFileTypeCounts.ToList();
            sortedListPass1.Sort((x, y) => y.Value.CompareTo(x.Value));
            var sortedListPass1str = sortedListPass1.Select((pair) => pair.Key).ToList();
            for (int i = 0; i < sortedListPass1str.Count; i++)
            {
                string allStr = sortedListPass1str[i];
                if (allStr.EndsWith(ALL_SUFFIX))
                {
                    var startVariantIndex = i + 1;
                    string basetype = allStr.Substring(0, allStr.Length - ALL_SUFFIX.Length);
                    var stringsToMove = sortedListPass1str.FindAll(((ftype) => ((ftype.StartsWith(basetype + " ") || ftype == basetype) && ftype != allStr)));
                    foreach (string stringToMove in stringsToMove)
                    {
                        sortedListPass1str.Remove(stringToMove);
                        sortedListPass1str.Insert(startVariantIndex, stringToMove);
                        startVariantIndex += 1;
                    }
                    i += stringsToMove.Count;
                }
            }

            return sortedListPass1str;
        }

        private static void condenseFileTypeChips(Dictionary<string, int> fullFileTypeCounts, List<ChipDataItem> chipsListFileTypes)
        {
            // further grouping of file types..
            if (chipsListFileTypes.Count > 14)
            {
                //a lot of times we have wayyy too many mp3 varients.
                //if more than 5 variants or if 2+ variants are less than 7.5% then group them up.
                var variantsToGroupUp = new List<(FileTypeChipDataItem baseChip, int variantsPastCutoff, int totalCount)>();
                FileTypeChipDataItem? currentBaseChip = null;
                int currentMax = -1;
                int counter = 0;
                int variantsPastCutoff = 0;
                foreach (FileTypeChipDataItem chipItem in chipsListFileTypes)
                {
                    if (currentBaseChip != null && (chipItem.BaseFileType.Contains(currentBaseChip.BaseFileType + " ") || chipItem.BaseFileType == currentBaseChip.BaseFileType))
                    {
                        counter++;
                        if (counter > 5 || (double)(fullFileTypeCounts[chipItem.GetFullDisplayText()]) / currentMax < .075)
                        {
                            variantsPastCutoff++;
                        }
                    }
                    else
                    {
                        if (currentBaseChip != null && variantsPastCutoff >= 2)
                        {
                            variantsToGroupUp.Add(new(currentBaseChip, variantsPastCutoff, counter));
                        }
                        currentBaseChip = chipItem;
                        variantsPastCutoff = 0;
                        counter = 0;
                    }

                    if (chipItem.IsAllCase)
                    {
                        currentBaseChip = chipItem;
                        currentMax = fullFileTypeCounts[chipItem.GetFullDisplayText()];
                        counter++;
                    }
                }

                //get the chips here...
                foreach (var tup in variantsToGroupUp)
                {
                    int start_all = chipsListFileTypes.IndexOf(tup.baseChip);
                    int start = start_all + tup.totalCount - tup.variantsPastCutoff;
                    var rangeToCondense = chipsListFileTypes.GetRange(start, tup.variantsPastCutoff);
                    //put range to condense in the tag...
                    chipsListFileTypes.RemoveRange(start, tup.variantsPastCutoff);
                    chipsListFileTypes.Insert(start, new FileTypeChipDataItem(tup.baseChip.BaseFileType + " (other)", rangeToCondense.Select(it=>it.GetFullDisplayText()).ToList()));
                }

                //if still more then 14 chop off those at the end...
                //just dont split a group i.e. -all (vbr) split after it.
                int pointToSplit = 13;
                if (chipsListFileTypes.Count() > 15) //so 16 or more
                {
                    while (pointToSplit < chipsListFileTypes.Count())
                    {
                        string fType = ((FileTypeChipDataItem)chipsListFileTypes[pointToSplit]).BaseFileType;
                        string ftypebase = fType;
                        if (fType.Contains(" ("))
                        {
                            ftypebase = fType.Substring(0, fType.IndexOf(" ("));

                        }
                        if (!(((FileTypeChipDataItem)chipsListFileTypes[pointToSplit - 1]).BaseFileType.Contains(ftypebase)))
                        {
                            //then it is not part of a group, we are done and can split here...
                            break;
                        }
                        else
                        {
                            pointToSplit++;
                        }
                    }
                    if (chipsListFileTypes.Count() - pointToSplit > 2) //i.e. if there is actually stuff to group up.
                    {
                        int cnt = chipsListFileTypes.Count() - pointToSplit;
                        var endToCondense = chipsListFileTypes.GetRange(pointToSplit, chipsListFileTypes.Count() - pointToSplit);
                        //put range to condense in the tag...
                        chipsListFileTypes.RemoveRange(pointToSplit, cnt);
                        chipsListFileTypes.Insert(pointToSplit, new FileTypeChipDataItem("other", endToCondense.Select(it => it.GetFullDisplayText()).ToList()));
                    }
                }
            }
        }

        internal struct FileCountGroup
        {
            public int MinValue;
            public int MaxValue;
            public int TotalFrequency;
            public List<(int Value, int Frequency)> Entries;

            public FileCountGroup(int value, int frequency)
            {
                MinValue = value;
                MaxValue = value;
                TotalFrequency = frequency;
                Entries = new List<(int, int)> { (value, frequency) };
            }
        }

        internal static FileCountGroup MergeGroups(FileCountGroup a, FileCountGroup b)
        {
            var merged = new FileCountGroup();
            merged.MinValue = Math.Min(a.MinValue, b.MinValue);
            merged.MaxValue = Math.Max(a.MaxValue, b.MaxValue);
            merged.TotalFrequency = a.TotalFrequency + b.TotalFrequency;
            merged.Entries = new List<(int, int)>(a.Entries);
            merged.Entries.AddRange(b.Entries);
            return merged;
        }

        internal static List<FileCountGroup> SeparateWhalesAndPoolMinnows(List<KeyValuePair<int, int>> sorted, int totalCount)
        {
            double whaleThreshold = totalCount * 0.25; // outright whales
            double hardSplit = totalCount * 0.25; // if we get over 25%, then split ahead
            double softSplit = totalCount * 0.18; // if we get over 18%, then consider splitting ahead based on variance

            var groups = new List<FileCountGroup>();
            FileCountGroup? currentPool = null;

            for (int i = 0; i < sorted.Count; i++)
            {
                var kvp = sorted[i];

                if (kvp.Value >= whaleThreshold)
                {
                    if (currentPool != null)
                    {
                        groups.Add(currentPool.Value);
                        currentPool = null;
                    }
                    groups.Add(new FileCountGroup(kvp.Key, kvp.Value));
                }
                else
                {
                    if (currentPool == null)
                    {
                        currentPool = new FileCountGroup(kvp.Key, kvp.Value);
                    }
                    else
                    {
                        var pool = currentPool.Value;
                        int newFreq = pool.TotalFrequency + kvp.Value;

                        bool shouldSplit = false;

                        if (newFreq >= hardSplit)
                        {
                            shouldSplit = true;
                        }
                        else if (pool.TotalFrequency >= softSplit && pool.Entries.Count >= 2)
                        {
                            // in the soft zone: split at natural gaps
                            double avgGap = (double)(pool.MaxValue - pool.MinValue) / (pool.Entries.Count - 1);
                            int gapToNext = kvp.Key - pool.MaxValue;
                            if (gapToNext > avgGap)
                            {
                                shouldSplit = true;
                            }
                        }

                        if (shouldSplit)
                        {
                            groups.Add(pool);
                            currentPool = new FileCountGroup(kvp.Key, kvp.Value);
                        }
                        else
                        {
                            currentPool = MergeGroups(pool, new FileCountGroup(kvp.Key, kvp.Value));
                        }
                    }
                }
            }

            if (currentPool != null)
            {
                groups.Add(currentPool.Value);
            }

            return groups;
        }


        public static List<(string displayKeyword, HashSet<string>? invariantsCollection)> getKeywords(List<Soulseek.SearchResponse> responses, string searchTerm, bool hideHidden)
        {
            try
            {
                // here we are looking for keywords - for example - 
                //   The Album Name (WEB) (2CD) - we want "The Album Name" "Web" "2CD"
                //   The Album Name - The Artist Name 2020 - we want "The Album Name""The Artist Name" "2020" 
                //   2020 - The Album Name - we want "The Album Name" "2020" 
                //   The Album Name (live) (album) (single) (compilation) (video) - we want "The Album Name" "live" "album" "single" "compilation" "video"
                //   The Album Name (Label, 2020) - we want "The Album Name" "label" "2020"
                //   The Album Name 2xLP (1996-10) [24B-96kHz] [OPUS 192] - we want "The Album Name" "2xLP" "1996-10" "24B-96kHz" "OPUS 192"
                //   NAME_CD2_Label_Year_Flac - we want "Name" "CD2" "Label" "Year" "Flac"
                //   Album Name (1994.01.01) - we want "Album Name" "1994.01.01"

                KeywordHelper keywordHelper = new KeywordHelper();
                int totalCount = responses.Count;
                for (int i = 0; i < totalCount; i++)
                {
                    if (hideHidden && responses[i].IsLockedOnly())
                    {
                        continue;
                    }
                    string folderName = Common.Helpers.GetFolderNameFromFile(responses[i].GetElementAtAdapterPosition(hideHidden, 0).Filename);
                    AddKeywordsFromFolderName(keywordHelper.AddKey, folderName, false);
                }

                for (int i = 0; i < totalCount; i++)
                {
                    if (hideHidden && responses[i].IsLockedOnly())
                    {
                        continue;
                    }
                    string parentFolderName = Common.Helpers.GetParentFolderNameFromFile(responses[i].GetElementAtAdapterPosition(hideHidden, 0).Filename);
                    AddKeywordsFromFolderName(keywordHelper.VoteIfExists, parentFolderName, true);
                }

                return keywordHelper.GetTopCandidates(searchTerm, MAX_KEYWORD_CHIPS);
            }
            catch (Exception ex)
            {
                Logger.FirebaseError("keywords failed", ex);
                return new();
            }
        }

        public static void AddKeywordsFromFolderName(Action<string> addKey, string folderName, bool ignoreCommonParentNames)
        {
            //fline = fline.Replace(" - ", " ");
            //fline = fline.Replace(", ", " ");
            if (folderName.StartsWith(" - "))
            {
                folderName = folderName.Substring(3);
            }
            folderName = folderName.Trim();

            //fline = fline.ToLower(); //this should be moved so its only for the keys..

            //test if something is in parenthesis and treat it specially bc its likely attributes???

            foreach (string term in folderName.Split(new string[] { "- ", " -", "{", "}", "[", "]", "(", ")", " _ " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (IgnoredTerms.Contains(term))
                {
                    continue;
                }
                bool inParen = false;
                bool startsWithYear = false;
                int dateLen = 0;
                string trimmedTerm = term.Trim();

                if (ignoreCommonParentNames && KeywordHelper.ShouldIgnoreParentFolderTerm(KeywordHelper.GetInvariantKey(trimmedTerm)))
                {
                    continue;
                }

                //Artist_Name-Album_Name-WEB-2021-ESG this is its own thing.. so just continue after processing...
                if (KeywordHelper.IsNoSpacesFormat(trimmedTerm))
                {
                    foreach (string t in trimmedTerm.Replace('_', ' ').Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        addKey(t.Trim());
                    }
                    continue;
                }

                if (KeywordHelper.IsInParenthesis(trimmedTerm, folderName))
                {
                    //normally if in parenthesis those are attributes so split them by ','
                    inParen = true;
                }
                if (KeywordHelper.StartsWithYearOrDate(trimmedTerm, out dateLen))
                {
                    startsWithYear = true;
                }

                if (inParen || startsWithYear)
                {
                    if (startsWithYear)
                    {
                        string year = trimmedTerm.Substring(0, dateLen);
                        addKey(year);
                    }
                    string rest = trimmedTerm;//.Substring(5).Trim();
                    if (startsWithYear)
                    {
                        rest = rest.Substring(dateLen).Trim();
                        //if after stripping date it now starts with - 
                        if (rest.StartsWith(", ") || rest.StartsWith(". "))
                        {
                            rest = rest.Substring(2);
                        }
                    }

                    if (inParen)
                    {
                        foreach (string restTerm in rest.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            addKey(restTerm.Trim());
                        }
                    }
                    else
                    {
                        addKey(rest);
                    }
                }
                else
                {
                    addKey(trimmedTerm);
                }
            }
        }

        private static readonly ImmutableHashSet<string> IgnoredTerms = ImmutableHashSet.Create<string>(
            "@eaDir", // NAS storage devices, no meaning
            "!!!"
            );

        public class KeywordHelper
        {

            public static string GetInvariantKey(string key)
            {
                string invariantKey = key.ToLower();
                invariantKey = invariantKey.Replace("and", "&");
                invariantKey = invariantKey.Replace(",", "");  //todo more efficient replace...
                invariantKey = invariantKey.Replace("'", "");
                invariantKey = invariantKey.Replace("-", "");
                invariantKey = invariantKey.Replace("_", "");

                //group these chars
                switch (invariantKey)
                {
                    case "cd1":
                        return "disc 1";
                    case "cd 1":
                        return "disc 1";
                    case "disc1":
                        return "disc 1";
                    case "cd2":
                        return "disc 2";
                    case "cd 2":
                        return "disc 2";
                    case "disc2":
                        return "disc 2";
                    case "va":
                        return "V.A.";
                    case "various artists":
                        return "V.A.";
                    case "v.a.":
                        return "V.A.";
                }
                return invariantKey;
            }

            public static bool IsCommonAttribute(string key)
            {

                switch (key)
                {
                    case "disc 1":
                    case "disc 2":
                    case "2cd":
                    case "cd":
                        return true;
                    default:
                        return false;
                }
            }

            public static bool IsYear(ReadOnlySpan<char> potentialYear)
            {
                if (int.TryParse(potentialYear, out int potInt))
                {
                    if (potInt >= 1900 && potInt <= 2044)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static bool ShouldIgnoreParentFolderTerm(string term)
            {
                if (term.Length == 1)
                {
                    return true;
                }

                switch (term)
                {
                    case "music":
                    case "complete":
                    case "@flac":
                    case "audio":
                    case "mp#":
                    case "mp3":
                    case "m?sica":
                    case "album":
                    case "flac albums":
                    case "albums":
                        return true;
                    default:
                        return false;
                }
            }

            private static readonly ImmutableHashSet<string> KnownAudioFormats = ImmutableHashSet.Create(
                StringComparer.OrdinalIgnoreCase,
                "mp3", "flac", "wav", "aiff", "wma", "aac", "ogg", "opus", "m4a", "mp4");

            // bare-integer tokens that should classify as a bitrate/sample-rate modifier.
            // avoids pruning real keywords that happen to be numbers (e.g. catalog numbers, years already handled elsewhere).
            private static readonly ImmutableHashSet<int> KnownBareBitrateNumbers = ImmutableHashSet.Create(
                64, 96, 112, 128, 160, 192, 224, 256, 320, // MP3/AAC bitrates
                44, 48);                                   // common sample rates (44.1/48kHz abbreviated)

            public static bool IsSingleFileAttributeType(string term)
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return false;
                }

                int knownFormatCount = 0;
                int modifierCount = 0;

                ReadOnlySpan<char> remaining = term.AsSpan().Trim();
                while (!remaining.IsEmpty)
                {
                    int spaceIdx = -1;
                    for (int i = 0; i < remaining.Length; i++)
                    {
                        if (remaining[i] == ' ' || remaining[i] == '\t')
                        {
                            spaceIdx = i;
                            break;
                        }
                    }

                    ReadOnlySpan<char> token;
                    if (spaceIdx == -1)
                    {
                        token = remaining;
                        remaining = ReadOnlySpan<char>.Empty;
                    }
                    else
                    {
                        token = remaining.Slice(0, spaceIdx);
                        remaining = remaining.Slice(spaceIdx + 1).TrimStart();
                    }

                    if (token.IsEmpty)
                    {
                        continue;
                    }

                    if (KnownAudioFormats.Contains(token.ToString()))
                    {
                        knownFormatCount++;
                    }
                    else if (IsBitrateOrFormatModifier(token))
                    {
                        modifierCount++;
                    }
                    else
                    {
                        // any "other" token disqualifies the term
                        return false;
                    }
                }

                // reject "mp3 flac" (multiple formats) — that's a parent-folder keyword, not a single attribute
                if (knownFormatCount > 1)
                {
                    return false;
                }
                return (knownFormatCount + modifierCount) > 0;
            }

            // Matches tokens like: 128, 320, 44.1, 1644.1, 16-44.1, 16/44.1, 192k, 320kbps, 44khz, 24bit, v0, v1, cbr, vbr, @320
            private static bool IsBitrateOrFormatModifier(ReadOnlySpan<char> token)
            {
                if (token.IsEmpty)
                {
                    return false;
                }

                // @<digits> — @192, @320
                if (token[0] == '@')
                {
                    if (token.Length < 2)
                    {
                        return false;
                    }
                    for (int i = 1; i < token.Length; i++)
                    {
                        if (!char.IsDigit(token[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                // v0/v1/v2/... — MP3 VBR preset
                if (token.Length == 2 && (token[0] == 'v' || token[0] == 'V') && char.IsDigit(token[1]))
                {
                    return true;
                }

                // cbr / vbr
                if (token.Length == 3)
                {
                    if (SpanEqualsIgnoreCase(token, "cbr") || SpanEqualsIgnoreCase(token, "vbr"))
                    {
                        return true;
                    }
                }

                // bare unit tokens — allow "320 kbps" to classify both tokens as modifiers
                if (SpanEqualsIgnoreCase(token, "kbps")
                    || SpanEqualsIgnoreCase(token, "khz")
                    || SpanEqualsIgnoreCase(token, "bit")
                    || SpanEqualsIgnoreCase(token, "bits"))
                {
                    return true;
                }

                // <digits>[.<digits>][(-|/)<digits>[.<digits>]][unit suffix]
                int idx = 0;
                int leadingDigitEnd;
                if (!TryConsumeDigitRun(token, ref idx))
                {
                    return false;
                }
                leadingDigitEnd = idx;
                bool hasDecimal = TryConsumeDotDigits(token, ref idx);
                bool hasPair = false;
                if (idx < token.Length && (token[idx] == '-' || token[idx] == '/'))
                {
                    idx++;
                    if (!TryConsumeDigitRun(token, ref idx))
                    {
                        return false;
                    }
                    TryConsumeDotDigits(token, ref idx);
                    hasPair = true;
                }

                if (idx == token.Length)
                {
                    // bare number, no suffix — digit pairs ("16-44.1") are unambiguous audio markers;
                    // pure integers must match the known-bitrates set so "mp3 123" is not pruned.
                    if (hasPair)
                    {
                        return true;
                    }
                    if (hasDecimal)
                    {
                        return false;
                    }
                    if (!int.TryParse(token.Slice(0, leadingDigitEnd), out int value))
                    {
                        return false;
                    }
                    return KnownBareBitrateNumbers.Contains(value);
                }

                ReadOnlySpan<char> suffix = token.Slice(idx);
                return SpanEqualsIgnoreCase(suffix, "k")
                    || SpanEqualsIgnoreCase(suffix, "kb")
                    || SpanEqualsIgnoreCase(suffix, "kbps")
                    || SpanEqualsIgnoreCase(suffix, "khz")
                    || SpanEqualsIgnoreCase(suffix, "hz")
                    || SpanEqualsIgnoreCase(suffix, "bit")
                    || SpanEqualsIgnoreCase(suffix, "bits");
            }

            private static bool TryConsumeDigitRun(ReadOnlySpan<char> token, ref int idx)
            {
                if (idx >= token.Length || !char.IsDigit(token[idx]))
                {
                    return false;
                }
                while (idx < token.Length && char.IsDigit(token[idx]))
                {
                    idx++;
                }
                return true;
            }

            private static bool TryConsumeDotDigits(ReadOnlySpan<char> token, ref int idx)
            {
                if (idx >= token.Length || token[idx] != '.')
                {
                    return false;
                }
                int savedIdx = idx;
                idx++;
                if (idx >= token.Length || !char.IsDigit(token[idx]))
                {
                    idx = savedIdx;
                    return false;
                }
                while (idx < token.Length && char.IsDigit(token[idx]))
                {
                    idx++;
                }
                return true;
            }

            private static bool SpanEqualsIgnoreCase(ReadOnlySpan<char> span, string literal)
            {
                if (span.Length != literal.Length)
                {
                    return false;
                }
                for (int i = 0; i < span.Length; i++)
                {
                    if (char.ToLowerInvariant(span[i]) != literal[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            public static bool IsInParenthesis(string term, string line)
            {
                return line.Contains("(" + term + ")") ||
                       line.Contains("[" + term + "]") ||
                       line.Contains("{" + term + "}");
            }

            public static bool IsNoSpacesFormat(string term)
            {
                if (!term.Contains(' ') && term.Contains('_') && term.Contains('-'))
                {
                    return true;
                }
                return false;
            }

            public static bool StartsWithYearOrDate(string line, out int dateLen)
            {
                dateLen = 0;
                if (line.Length < 5)
                {
                    return false;
                }
                if (IsYear(line.AsSpan(0, 4)))
                {
                    if (IsSpecialChar(line[4]))
                    {
                        //can we also get month and day if available?
                        if (line.Length >= 10)
                        {
                            if (IsSpecialChar(line[7]) && int.TryParse(line.AsSpan(5, 2), out int month) && int.TryParse(line.AsSpan(8, 2), out int day))
                            {
                                if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                                {
                                    dateLen = 10;
                                    return true;
                                }
                                return false;
                            }
                        }
                        dateLen = 4;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }

            public static bool IsSpecialChar(char c)
            {
                return c == ' ' || c == '-' || c == '.' || c == ',';
            }


            private class KeywordStats
            {
                public int InvariantCount;
                public Dictionary<string, int> RealCounts = new Dictionary<string, int>();
            }

            private readonly Dictionary<string, KeywordStats> stats = new Dictionary<string, KeywordStats>();

            public void VoteIfExists(string term)
            {
                if (!stats.ContainsKey(GetInvariantKey(term)))
                {
                    return;
                }
                AddKey(term);
            }

            public void AddKey(string term)
            {
                string invariantTerm = GetInvariantKey(term);
                if (!stats.TryGetValue(invariantTerm, out var s))
                {
                    s = new KeywordStats();
                    stats[invariantTerm] = s;
                }
                s.InvariantCount++;
                s.RealCounts.TryGetValue(term, out int realCount);
                s.RealCounts[term] = realCount + 1;
            }

            public List<(string displayKeyword, HashSet<string>?)> GetTopCandidates(string searchTerm, int topN)
            {
                // drop keys that shouldn't be candidates at all; weighting is applied at sort time.
                string searchTermInvariant = GetInvariantKey(searchTerm);
                foreach (string key in stats.Keys.ToList())
                {
                    if (IsSingleFileAttributeType(key)
                        || searchTermInvariant.Contains(key) // consequence of: if someone selects "20" we filter for "20" not " 20 "
                        || IgnoredTerms.Contains(key))
                    {
                        stats.Remove(key);
                    }
                }
                var sorted = stats.ToList();
                sorted.Sort((x, y) => Score(y).CompareTo(Score(x)));
                List<(string displayKeyword, HashSet<string>? invariantsCollection)> keyTerms = new();
                if (topN > sorted.Count)
                {
                    topN = sorted.Count;
                }
                for (int i = 0; i < topN; i++)
                {
                    var realCounts = sorted[i].Value.RealCounts;
                    string displayName = string.Empty;
                    int max = -1;
                    foreach (var kvp in realCounts)
                    {
                        if (kvp.Value > max)
                        {
                            max = kvp.Value;
                            displayName = kvp.Key;
                        }
                    }
                    var variants = new HashSet<string>(realCounts.Keys, StringComparer.OrdinalIgnoreCase);
                    if (variants.Count > 1)
                    {
                        keyTerms.Add((displayName, variants));
                    }
                    else
                    {
                        keyTerms.Add((displayName, null));
                    }
                }
                return keyTerms;

                static double Score(KeyValuePair<string, KeywordStats> kvp)
                {
                    double s = kvp.Value.InvariantCount;
                    if (IsYear(kvp.Key))
                    {
                        s *= 0.25;
                    }
                    else if (IsCommonAttribute(kvp.Key))
                    {
                        s *= 0.6;
                    }
                    return s;
                }
            }
        }
    }
}
