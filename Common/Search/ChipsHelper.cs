using Common;
using Seeker.Extensions.SearchResponseExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    public static class ChipsHelper
    {
        public const string ALL_SUFFIX = " - all";
        private const int TOTAL_FILENUM_BUCKETS = 4;

        /// <summary>
        /// If hide hidden is true then for counts only consider unlocked. else consider everything.
        /// </summary>
        /// <param name="responses"></param>
        /// <param name="searchTerm"></param>
        /// <param name="smartFilterOptions"></param>
        /// <returns></returns>
        public static List<ChipDataItem> GetChipDataItemsFromSearchResults(List<Soulseek.SearchResponse> responses, string searchTerm, PreferencesState.SmartFilterState smartFilterOptions)
        {
            Dictionary<ChipType, IEnumerable<ChipDataItem>> finalData = new Dictionary<ChipType, IEnumerable<ChipDataItem>>();
            bool hideHidden = PreferencesState.HideLockedResultsInSearch;

            if (smartFilterOptions.FileTypesEnabled || smartFilterOptions.NumFilesEnabled)
            {
                Dictionary<string, int> fullFileTypeCounts;
                Dictionary<int, int> fileCountCounts;
                int totalSearchResultCount;
                getFileStatistics(responses, hideHidden, out fullFileTypeCounts, out fileCountCounts, out totalSearchResultCount);

                if (smartFilterOptions.NumFilesEnabled)
                {
                    List<string> chipDescriptions = generateFileCountChips(fileCountCounts, totalSearchResultCount);
                    finalData[ChipType.FileCount] = chipDescriptions.Select(str => new ChipDataItem(ChipType.FileCount, false, str));
                }

                if (smartFilterOptions.FileTypesEnabled)
                {
                    calculateAndAddBaseStatsFromVariants(fullFileTypeCounts);

                    List<string> sortedGroupedFileTypes = sortAndGroupFileTypes(fullFileTypeCounts);

                    var chipsListFileTypes = sortedGroupedFileTypes.Select(
                        str => str.EndsWith(ALL_SUFFIX) ? new ChipDataItem(ChipType.FileType, false, str.Substring(0, str.Length - ALL_SUFFIX.Length), true) : new ChipDataItem(ChipType.FileType, false, str)).ToList();

                    condenseFileTypeChips(fullFileTypeCounts, chipsListFileTypes);
                    finalData[ChipType.FileType] = chipsListFileTypes;
                }
            }

            if (smartFilterOptions.KeywordsEnabled)
            {
                List<ChipDataItem> chipKeywords = new List<ChipDataItem>();
                var keywords = GetKeywords(responses, searchTerm);
                foreach (var keyword in keywords)
                {
                    if (keyword.Item2 != null)
                    {
                        chipKeywords.Add(new ChipDataItem(ChipType.Keyword, false, keyword.Item1, keyword.Item2.ToList()));
                    }
                    else
                    {
                        chipKeywords.Add(new ChipDataItem(ChipType.Keyword, false, keyword.Item1));
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
                        if (fullFileTypeCounts.ContainsKey(fullFileType))
                        {
                            fullFileTypeCounts[fullFileType]++;
                        }
                        else
                        {
                            fullFileTypeCounts[fullFileType] = 1;
                        }
                    }

                    int fcount = hideHidden ? searchResponse.FileCount : searchResponse.FileCount + searchResponse.LockedFileCount;
                    if (fileCountCounts.ContainsKey(fcount))
                    {
                        fileCountCounts[fcount]++;
                    }
                    else
                    {
                        fileCountCounts[fcount] = 1;
                    }
                }
            }
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
                var variantsToGroupUp = new List<(ChipDataItem baseChip, int variantsPastCutoff, int totalCount)>();
                ChipDataItem? currentBaseChip = null;
                int currentMax = -1;
                int counter = 0;
                int variantsPastCutoff = 0;
                foreach (ChipDataItem chipItem in chipsListFileTypes)
                {
                    if (currentBaseChip != null && (chipItem.BaseDisplayText.Contains(currentBaseChip.BaseDisplayText + " ") || chipItem.BaseDisplayText == currentBaseChip.BaseDisplayText))
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
                    chipsListFileTypes.Insert(start, new ChipDataItem(ChipType.FileType, false, tup.baseChip.BaseDisplayText + " (other)", rangeToCondense.Select(it=>it.GetFullDisplayText()).ToList()));
                }

                //if still more then 14 chop off those at the end...
                //just dont split a group i.e. -all (vbr) split after it.
                int pointToSplit = 13;
                if (chipsListFileTypes.Count() > 15) //so 16 or more
                {
                    while (pointToSplit < chipsListFileTypes.Count())
                    {
                        string fType = chipsListFileTypes[pointToSplit].BaseDisplayText;
                        string ftypebase = fType;
                        if (fType.Contains(" ("))
                        {
                            ftypebase = fType.Substring(0, fType.IndexOf(" ("));

                        }
                        if (!(chipsListFileTypes[pointToSplit - 1].BaseDisplayText.Contains(ftypebase)))
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
                        chipsListFileTypes.Insert(pointToSplit, new ChipDataItem(ChipType.FileType, false, "other", endToCondense.Select(it => it.GetFullDisplayText()).ToList()));
                    }
                }
            }
        }

        private static List<string> generateFileCountChips(Dictionary<int, int> fileCountCounts, int totalSearchResultCount)
        {
            //second pass
            //create file count buckets
            List<string> chipDescriptions = new List<string>();
            if (fileCountCounts.Count > TOTAL_FILENUM_BUCKETS)
            {
                // we have too many buckets, we need to group them
                // each group consists of >= 1/4 of the results
                int groupSize = totalSearchResultCount / TOTAL_FILENUM_BUCKETS;
                var sortedList = fileCountCounts.ToList();
                //key is the folder count, value is the number of times that folder count appeared.
                sortedList.Sort((x, y) => x.Key.CompareTo(y.Key)); //low to high
                int start = int.MinValue;
                int partialTotal = 0;
                int numGroups = 0;

                for (int j = 0; j < sortedList.Count; j++)
                {
                    if (numGroups == TOTAL_FILENUM_BUCKETS - 1)
                    {
                        //put the rest in the last bucket
                        if (j == sortedList.Count - 1)
                        {
                            //we are on the last one
                            chipDescriptions.Add($"{sortedList[j].Key} files");
                        }
                        else
                        {
                            chipDescriptions.Add($"{sortedList[j].Key} to {sortedList[sortedList.Count - 1].Key} files");
                        }
                        break;
                    }
                    if (((sortedList[j].Value + partialTotal) >= groupSize) || (sortedList.Count - 1 == j)) //or if its the last one..
                    {
                        //thats all for this group
                        if (start == int.MinValue)
                        {
                            //that means we start and end here
                            numGroups++;
                            if (sortedList[j].Key == 1)
                            {
                                chipDescriptions.Add($"{sortedList[j].Key} file");
                            }
                            else
                            {
                                chipDescriptions.Add($"{sortedList[j].Key} files");
                            }
                        }
                        else
                        {
                            //that means we start and end here
                            numGroups++;
                            chipDescriptions.Add($"{start} to {sortedList[j].Key} files");
                        }
                        partialTotal = 0;
                        start = int.MinValue;
                    }
                    else
                    {
                        if (start == int.MinValue)
                        {
                            start = sortedList[j].Key;
                        }
                        partialTotal += sortedList[j].Value;
                    }
                }
            }
            else
            {
                // no need to group, we only have at most 4 distinct buckets
                var sortedList = fileCountCounts.ToList();
                sortedList.Sort((x, y) => x.Key.CompareTo(y.Key));
                foreach (var pair in sortedList)
                {
                    if (pair.Key == 1)
                    {
                        chipDescriptions.Add($"{pair.Key} file");
                    }
                    else
                    {
                        chipDescriptions.Add($"{pair.Key} files");
                    }
                }
            }

            return chipDescriptions;
        }

        public static List<Tuple<string, HashSet<string>>> GetKeywords(List<Soulseek.SearchResponse> responses, string searchTerm)
        {
            try
            {
                //var sw = System.Diagnostics.Stopwatch.StartNew();
                KeywordHelper keywordHelper = new KeywordHelper();
                Dictionary<string, int> counts = new Dictionary<string, int>();
                int totalCount = responses.Count();
                for (int i = 0; i < totalCount; i++)
                {
                    string fline = Common.Helpers.GetFolderNameFromFile(responses[i].GetElementAtAdapterPosition(false, 0).Filename);

                    //fline = fline.Replace(" - ", " ");
                    //fline = fline.Replace(", ", " ");
                    if (fline.StartsWith(" - "))
                    {
                        fline = fline.Substring(3);
                    }
                    fline = fline.Trim();

                    //fline = fline.ToLower(); //this should be moved so its only for the keys..

                    //test if something is in parenthesis and treat it specially bc its likely attributes???

                    foreach (string term in fline.Split(new string[] { "- ", " -", "{", "}", "[", "]", "(", ")", " _ " }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        bool inParen = false;
                        bool startsWithYear = false;
                        int dateLen = 0;
                        string trimmedTerm = term.Trim();
                        //Trippie_Redd-Trip_At_Knight-WEB-2021-ESG this is its own thing.. so just continue after processing...
                        if (KeywordHelper.IsNoSpacesFormat(trimmedTerm))
                        {
                            foreach (string t in trimmedTerm.Replace('_', ' ').Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                keywordHelper.AddKey(t.Trim());
                            }
                            continue;
                        }

                        if (KeywordHelper.IsInParenthesis(trimmedTerm, fline))
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

                                keywordHelper.AddKey(year);
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
                                    keywordHelper.AddKey(restTerm.Trim());
                                }
                            }
                            else
                            {
                                keywordHelper.AddKey(rest);
                            }
                        }
                        else
                        {
                            keywordHelper.AddKey(trimmedTerm);
                        }
                    }
                    //generate all ngrams
                }




                //#if PARENT_VOTE

                //var alllines = File.ReadLines(@"H:\Seeker_Testing\search_results\the_avalanches.txt");
                //var sw = System.Diagnostics.Stopwatch.StartNew();
                //KeywordHelper keywordHelper = new KeywordHelper();
                //Dictionary<string, int> counts = new Dictionary<string, int>();
                for (int i = 0; i < totalCount; i++)
                {
                    //string nline = line.Replace("animal collective","",StringComparison.InvariantCultureIgnoreCase);
                    string fline = Common.Helpers.GetParentFolderNameFromFile(responses[i].GetElementAtAdapterPosition(false, 0).Filename);

                    //fline = fline.Replace(" - ", " ");
                    //fline = fline.Replace(", ", " ");
                    if (fline.StartsWith(" - "))
                    {
                        fline = fline.Substring(3);
                    }
                    fline = fline.Trim();

                    //fline = fline.ToLower(); //this should be moved so its only for the keys..

                    //test if something is in parenthesis and treat it specially bc its likely attributes???

                    foreach (string term in fline.Split(new string[] { "- ", " -", "{", "}", "[", "]", "(", ")", " _ " }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string termTrimmed = term.Trim();
                        bool inParen = false;
                        bool startsWithYear = false;
                        int dateLen = 0;

                        if (KeywordHelper.IsCommonParentFolder2(KeywordHelper.GetInvarientKey(termTrimmed)))
                        {
                            continue;
                        }

                        //Trippie_Redd-Trip_At_Knight-WEB-2021-ESG this is its own thing.. so just continue after processing...
                        if (KeywordHelper.IsNoSpacesFormat(termTrimmed))
                        {
                            foreach (string t in termTrimmed.Replace('_', ' ').Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                keywordHelper.VoteIfExists(t.Trim());
                            }
                            continue;
                        }

                        if (KeywordHelper.IsInParenthesis(termTrimmed, fline))
                        {
                            //normally if in parenthesis those are attributes so split them by ','
                            inParen = true;
                        }
                        if (KeywordHelper.StartsWithYearOrDate(termTrimmed, out dateLen))
                        {
                            startsWithYear = true;
                        }

                        if (inParen || startsWithYear)
                        {
                            if (startsWithYear)
                            {
                                string year = termTrimmed.Substring(0, dateLen);

                                keywordHelper.VoteIfExists(year);
                            }
                            string rest = termTrimmed;//.Substring(5).Trim();
                            if (startsWithYear)
                            {
                                rest = rest.Substring(dateLen).Trim();
                            }

                            if (inParen)
                            {
                                foreach (string restTerm in rest.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    keywordHelper.VoteIfExists(restTerm.Trim());
                                }
                            }
                            else
                            {
                                keywordHelper.VoteIfExists(rest);
                            }
                        }
                        else
                        {
                            keywordHelper.VoteIfExists(termTrimmed);
                        }
                    }
                    //generate all ngrams
                }

                //#endif
                return keywordHelper.GetTopCandidates(searchTerm, 11);
            }
            catch (Exception ex)
            {
                // TODO2026 add back
                //Logger.Firebase("keywords failed " + ex.Message + ex.StackTrace);
                return new List<Tuple<string, HashSet<string>>>();
            }
        }


        public class KeywordHelper
        {

            public static string GetInvarientKey(string key)
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

            public static bool IsYear(string potentialYear)
            {
                if (int.TryParse(potentialYear, out int potInt))
                {
                    if (potInt >= 1900 && potInt <= 2034)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static bool IsCommonParentFolder2(string t)
            {
                //#if PARENT_VOTE
                if (t.Length == 1)
                {
                    return true;
                }

                switch (t)
                {
                    case "music":
                    case "complete":
                    case "@flac":
                    case "audio":
                    case "mp#":
                    case "m?sica":
                    case "album":
                    case "flac albums":
                    case "albums":
                        return true;
                    default:
                        return false;
                }
                //#else
                //                return false;
                //#endif

            }

            public static bool IsSingleFileAttributeType(string t)
            {
                switch (t)
                {
                    case "mp3":
                    case "flac":
                    case "wav":
                    case "wma":
                    case "aac":
                    case "mp4":
                    case "aiff":
                    case "ogg":
                    case "opus":
                    case "320":
                    case "16-44.1":
                    case "192k":
                    case "mp3 320":
                    case "mp3 192":
                    case "mp3 v0":
                    case "mp3 128":
                    case "320 kbps":
                    case "320kbps":
                    case "m4a 128":
                    case "v0":
                    case "mp3 320kbps":
                    case "!!!":
                    case "-":
                    case "@192":
                    case "@320":
                    case "flac 24bit":
                    case "mp3 320 44":
                        return true;
                    default:
                        return false;
                }
            }

            public static bool IsInParenthesis(string term, string line)
            {
                int i = line.IndexOf(term);
                if (i > 1)
                {
                    int t = term.Length;
                    if ((line[i - 1] == '(' || line[i - 1] == '{' || line[i - 1] == '[') &&
                        (i + t < line.Length) && ((line[i + t] == ')' || line[i + t] == '}' || line[i + t] == ']')))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
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
                if (IsYear(line.Substring(0, 4)))
                {
                    if (IsSpecialChar(line[4]))
                    {
                        //can we also get month and day if available?
                        if (line.Length >= 10)
                        {
                            if (IsSpecialChar(line[7]) && int.TryParse(line.Substring(5, 2), out _) && int.TryParse(line.Substring(8, 2), out _))
                            {
                                dateLen = 10;
                                return true;
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


            public Dictionary<string, int> invariantKeyCounts = new Dictionary<string, int>();
            public Dictionary<string, int> realCounts = new Dictionary<string, int>();
            public Dictionary<string, HashSet<string>> invariantToReal = new Dictionary<string, HashSet<string>>();

            public void VoteIfExists(string term)
            {
                string invariantTerm = GetInvarientKey(term);
                if (invariantKeyCounts.ContainsKey(invariantTerm))
                {
                    invariantKeyCounts[invariantTerm]++;
                }
                else
                {
                    return;
                }

                if (realCounts.ContainsKey(term))
                {
                    realCounts[term]++;
                }
                else
                {
                    realCounts[term] = 1;
                }

                if (invariantToReal.ContainsKey(invariantTerm))
                {
                    invariantToReal[invariantTerm].Add(term);
                }
                else
                {
                    invariantToReal[invariantTerm] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    invariantToReal[invariantTerm].Add(term);
                }
            }

            public void AddKey(string term)
            {
                string invariantTerm = GetInvarientKey(term);
                if (invariantKeyCounts.ContainsKey(invariantTerm))
                {
                    invariantKeyCounts[invariantTerm]++;
                }
                else
                {
                    invariantKeyCounts[invariantTerm] = 1;
                }

                if (realCounts.ContainsKey(term))
                {
                    realCounts[term]++;
                }
                else
                {
                    realCounts[term] = 1;
                }

                if (invariantToReal.ContainsKey(invariantTerm))
                {
                    invariantToReal[invariantTerm].Add(term);
                }
                else
                {
                    invariantToReal[invariantTerm] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    invariantToReal[invariantTerm].Add(term);
                }
            }

            public List<Tuple<string, HashSet<string>>> GetTopCandidates(string searchTerm, int topN)
            {
                //weigh the years, and throw out the just file types.
                string searchTermInvarient = GetInvarientKey(searchTerm);
                foreach (string key in invariantKeyCounts.Keys.ToList())
                {
                    if (IsSingleFileAttributeType(key)) //todo use collection...
                    {
                        invariantKeyCounts.Remove(key);
                    }
                    else if (searchTermInvarient.Contains(key))
                    {
                        invariantKeyCounts.Remove(key);
                    }
                    else if (IsYear(key))
                    {
                        invariantKeyCounts[key] /= 4;
                    }
                    else if (IsCommonAttribute(key))
                    {

                        invariantKeyCounts[key] = (int)(invariantKeyCounts[key] * .6);
                    }
                    else
                    {
                        //#if USE_PARENT
                        //                        if(IsCommonParentFolder(key))
                        //                        {
                        //                            invarientKeyCounts.Remove(key);
                        //                        }
                        //#endif
                    }

                }
                //
                //l.Sort((x, y) => y.Value.CompareTo(x.Value));


                var l = invariantKeyCounts.ToList();
                l.Sort((x, y) => y.Value.CompareTo(x.Value));
                List<Tuple<string, HashSet<string>>> keyTerms = new List<Tuple<string, HashSet<string>>>();
                if (topN > l.Count)
                {
                    topN = l.Count;
                }
                for (int i = 0; i < topN; i++)
                {
                    var hs = invariantToReal[l[i].Key];
                    if (hs.Count > 1)
                    {
                        int max = -1;
                        string displayName = string.Empty;
                        for (int iiii = 0; iiii < hs.Count; iiii++)
                        {
                            string name = hs.ElementAt(iiii);
                            if (realCounts[name] > max)
                            {
                                max = realCounts[name];
                                displayName = name;
                            }
                        }
                        keyTerms.Add(new Tuple<string, HashSet<string>>(displayName, hs));
                    }
                    else
                    {
                        keyTerms.Add(new Tuple<string, HashSet<string>>(hs.ElementAt(0), null));
                    }

                }
                return keyTerms;
            }
        }
    }
}
