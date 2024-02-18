using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Chip;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{

    public static class ChipsHelper
    {
        /// <summary>
        /// If hide hidden is true then for counts only consider unlocked. else consider everything.
        /// </summary>
        /// <param name="responses"></param>
        /// <param name="searchTerm"></param>
        /// <param name="smartFilterOptions"></param>
        /// <returns></returns>
        public static List<ChipDataItem> GetChipDataItemsFromSearchResults(List<Soulseek.SearchResponse> responses, string searchTerm, SeekerState.SmartFilterState smartFilterOptions)
        {
            Dictionary<ChipType, IEnumerable<ChipDataItem>> finalData = new Dictionary<ChipType, IEnumerable<ChipDataItem>>();
            bool hideHidden = SeekerState.HideLockedResultsInSearch;

            //this is relevant to both
            if (smartFilterOptions.FileTypesEnabled || smartFilterOptions.NumFilesEnabled)
            {

                Dictionary<string, int> fileTypeCounts = new Dictionary<string, int>();
                Dictionary<int, int> fileCountCounts = new Dictionary<int, int>();

                //inital pass
                int count = responses.Count;
                for (int i = 0; i < count; i++)
                {
                    var searchResponse = responses[i]; //the search is done at this point, so search responses will not be changed.

                    //create file type, file num, and keyword buckets.
                    //get counts to show in order
                    //there are parent child relationships between 'fileType' and 'fileType (vbr/kbps/samples/depth)'
                    string ftype = searchResponse.GetDominantFileType(hideHidden, out _);
                    if (string.IsNullOrEmpty(ftype))
                    {
                        continue;
                    }
                    if (fileTypeCounts.ContainsKey(ftype))
                    {
                        fileTypeCounts[ftype]++;
                    }
                    else
                    {
                        fileTypeCounts[ftype] = 1;
                    }
                    //int baseIndex = ftype.IndexOf(" (");
                    //if (baseIndex != -1)
                    //{
                    //    fileTypeBases.Add(ftype.Substring(0,baseIndex));
                    //}

                    int fcount = hideHidden ? searchResponse.FileCount : searchResponse.FileCount + searchResponse.LockedFileCount;
                    if (fileCountCounts.ContainsKey(fcount))
                    {
                        fileCountCounts[fcount]++;
                    }
                    else
                    {
                        fileCountCounts[fcount] = 1;
                    }

                    //TODO: keywords
#if DEBUG
                    Console.WriteLine(CommonHelpers.GetFolderNameFromFile(searchResponse.GetElementAtAdapterPosition(false, 0).Filename));
#endif
                }

                if (smartFilterOptions.NumFilesEnabled)
                {
                    //second pass
                    //create file count buckets
                    List<string> chipDescriptions = new List<string>();
                    if (fileCountCounts.Count > 4)
                    {
                        //do groups.
                        //the each group consists of >= 1/4 of the results
                        int groupSize = count / 4;
                        var sortedList = fileCountCounts.ToList();
                        //key is the folder count, value is the number of times that folder count appeared.
                        sortedList.Sort((x, y) => x.Key.CompareTo(y.Key)); //low to high
                        int start = int.MinValue;
                        int partialTotal = 0;
                        int numGroups = 0;

                        for (int ii = 0; ii < sortedList.Count; ii++)
                        {
                            if (numGroups == 3)
                            {
                                //put the rest in the last bucket
                                if (ii == sortedList.Count - 1)
                                {
                                    //we are on the last one
                                    chipDescriptions.Add($"{sortedList[ii].Key} files");
                                }
                                else
                                {
                                    chipDescriptions.Add($"{sortedList[ii].Key} to {sortedList[sortedList.Count - 1].Key} files");
                                }
                                break;
                            }
                            if (((sortedList[ii].Value + partialTotal) >= groupSize) || (sortedList.Count - 1 == ii)) //or if its the last one..
                            {
                                //thats all for this group
                                if (start == int.MinValue)
                                {
                                    //that means we start and end here
                                    numGroups++;
                                    if (sortedList[ii].Key == 1)
                                    {
                                        chipDescriptions.Add($"{sortedList[ii].Key} file");
                                    }
                                    else
                                    {
                                        chipDescriptions.Add($"{sortedList[ii].Key} files");
                                    }
                                }
                                else
                                {
                                    //that means we start and end here
                                    numGroups++;
                                    chipDescriptions.Add($"{start} to {sortedList[ii].Key} files");
                                }
                                partialTotal = 0;
                                start = int.MinValue;
                            }
                            else
                            {
                                if (start == int.MinValue)
                                {
                                    start = sortedList[ii].Key;
                                }
                                partialTotal += sortedList[ii].Value;
                            }
                        }
                    }
                    else
                    {
                        //todo if only one group should we still do it?? maybe it is informative.. and might happen rather rarely..
                        //if you do make sure you test as count-1 is a thing below... to get the LastInGroup..
                        var sortedList = fileCountCounts.ToList();
                        //key is the folder count, value is the number of times that folder count appeared.
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

                    finalData[ChipType.FileCount] = chipDescriptions.Select(str => new ChipDataItem(ChipType.FileCount, false, str));
                }

                if (smartFilterOptions.FileTypesEnabled)
                {
                    List<string> fileTypeBases = new List<string>();
                    //get bases
                    foreach (string fileType in fileTypeCounts.Keys)
                    {
                        int fIndexBase = fileType.IndexOf(" (");
                        if (fIndexBase != -1)
                        {
                            string fbase = fileType.Substring(0, fIndexBase);
                            if (!fileTypeBases.Contains(fbase))
                            {
                                fileTypeBases.Add(fbase);
                            }
                        }
                    }

                    //fileTypeBases i.e. mp3, flac
                    //if bases have more than 1 add "base - all"
                    int bases = 0;
                    foreach (string fileTypeBase in fileTypeBases)
                    {
                        int count1 = 0;
                        int results = 0;
                        foreach (var fileType in fileTypeCounts)
                        {
                            if (fileType.Key.Contains(fileTypeBase + " ")) //not just base, but base + " "
                            {
                                count1++;
                                results += fileType.Value;
                            }
                        }
                        if (count1 > 1)
                        {
                            //add a " - all".
                            //remove the (base) if it is there.
                            fileTypeCounts.Remove(fileTypeBase);
                            fileTypeCounts.Add(fileTypeBase + " - all", results);
                            bases++;
                        }
                    }

                    //now sort.  the sort is a bit special as its mostly by number of results, but with variants coming after all (if there are any)
                    var sortedListPass1 = fileTypeCounts.ToList();
                    sortedListPass1.Sort((x, y) => y.Value.CompareTo(x.Value));
                    var sortedListPass1str = sortedListPass1.Select((pair) => pair.Key).ToList();
                    int startIndex = 0;
                    while (bases > 0)
                    {
                        for (int iii = startIndex; iii < sortedListPass1str.Count; iii++)
                        {
                            string allStr = sortedListPass1str[iii];
                            if (allStr.Contains(" - all"))
                            {
                                startIndex = iii + 1;
                                string basetype = allStr.Replace(" - all", "");
                                var stringsToMove = sortedListPass1str.FindAll(((ftype) => ((ftype.Contains(basetype + " ") || ftype == basetype) && ftype != allStr)));
                                foreach (string stringToMove in stringsToMove)
                                {
                                    sortedListPass1str.Remove(stringToMove);
                                    sortedListPass1str.Insert(startIndex, stringToMove);
                                    startIndex += 1;
                                }
                                bases--;
                                break;
                            }
                        }
                    }

#if DEBUG
                    foreach (string ftype in sortedListPass1str)
                    {
                        MainActivity.LogDebug(ftype + " : " + fileTypeCounts[ftype]);
                    }
#endif



                    var chipsListFileTypes = sortedListPass1str.Select(str => new ChipDataItem(ChipType.FileType, false, str)).ToList();


                    //further grouping of file types..
                    if (sortedListPass1str.Count > 14)
                    {
                        //a lot of times we have wayyy too many mp3 varients.
                        //if more than 5 varients or if 2+ varients are less than 7.5% then group them up.
                        List<Tuple<string, int, int>> varientsToGroupUp = new List<Tuple<string, int, int>>();
                        string currentBase = null;
                        int currentMax = -1;
                        int counter = 0;
                        bool cutoffConditionReached = false;
                        int varientsPastCutoff = 0;
                        foreach (string ftype in sortedListPass1str)
                        {

                            if (currentBase != null && (ftype.Contains(currentBase + " ") || ftype == currentBase))
                            {
                                counter++;
                                if (counter > 5 || (double)(fileTypeCounts[ftype]) / currentMax < .075)
                                {
                                    varientsPastCutoff++;
                                }
                            }
                            else
                            {
                                //we finished this grouping if applicable...
                                if (currentBase != null && varientsPastCutoff >= 2)
                                {
                                    varientsToGroupUp.Add(new Tuple<string, int, int>(currentBase, varientsPastCutoff, counter));
                                }
                                currentBase = null;
                                varientsPastCutoff = 0;
                                counter = 0;
                            }

                            if (ftype.Contains(" - all"))
                            {
                                currentBase = ftype.Replace(" - all", "");
                                currentMax = fileTypeCounts[ftype];
                                counter++;
                            }
                        }

                        //get the chips here...
                        foreach (var tup in varientsToGroupUp)
                        {
                            int start_all = sortedListPass1str.IndexOf(tup.Item1 + " - all");
                            int start = start_all + tup.Item3 - tup.Item2;
                            var rangeToCondense = sortedListPass1str.GetRange(start, tup.Item2);
                            //put range to condense in the tag...
                            sortedListPass1str.RemoveRange(start, tup.Item2);
                            chipsListFileTypes.RemoveRange(start, tup.Item2);
                            sortedListPass1str.Insert(start, tup.Item1 + " (other)");
                            chipsListFileTypes.Insert(start, new ChipDataItem(ChipType.FileType, false, tup.Item1 + " (other)", rangeToCondense.ToList()));
                        }

                        //if still more then 14 chop off those at the end...
                        //just dont split a group i.e. -all (vbr) split after it.
                        int pointToSplit = 13;
                        if (sortedListPass1str.Count() > 15) //so 16 or more
                        {
                            while (pointToSplit < sortedListPass1str.Count())
                            {
                                string fType = sortedListPass1str[pointToSplit];
                                string ftypebase = fType;
                                if (fType.Contains(" ("))
                                {
                                    ftypebase = fType.Substring(0, fType.IndexOf(" ("));

                                }
                                if (!(sortedListPass1str[pointToSplit - 1].Contains(ftypebase)))
                                {
                                    //then it is not part of a group, we are done and can split here...
                                    break;
                                }
                                else
                                {
                                    pointToSplit++;
                                }
                            }
                            if (sortedListPass1str.Count() - pointToSplit > 2) //i.e. if there is actually stuff to group up.
                            {
                                int cnt = sortedListPass1str.Count() - pointToSplit;
                                var endToCondense = sortedListPass1str.GetRange(pointToSplit, sortedListPass1str.Count() - pointToSplit);
                                //put range to condense in the tag...
                                sortedListPass1str.RemoveRange(pointToSplit, cnt);
                                chipsListFileTypes.RemoveRange(pointToSplit, cnt);
                                sortedListPass1str.Insert(pointToSplit, "other");
                                chipsListFileTypes.Insert(pointToSplit, new ChipDataItem(ChipType.FileType, false, "other", endToCondense.ToList()));
                            }
                        }
                    }
                    finalData[ChipType.FileType] = chipsListFileTypes;
                }
            }

            //keywords
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
            //end keywords

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


            //var dataItems = chipDescriptions.Select(str => new ChipDataItem(ChipType.FileCount, false, str));
            //var dataItemsList = dataItems.ToList();
            //if(dataItemsList.Count() > 0)
            //{
            //    dataItemsList[dataItemsList.Count() - 1].LastInGroup = true;
            //}
            //dataItemsList.AddRange(chipsListFileTypes);
            //if(chipKeywords.Count() > 0)
            //{
            //    dataItemsList[dataItemsList.Count() - 1].LastInGroup = true;
            //}
            //dataItemsList.AddRange(chipKeywords);
            return finalItems;
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
                    string fline = CommonHelpers.GetFolderNameFromFile(responses[i].GetElementAtAdapterPosition(false, 0).Filename);

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
                    string fline = CommonHelpers.GetParentFolderNameFromFile(responses[i].GetElementAtAdapterPosition(false, 0).Filename);

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
                MainActivity.LogFirebase("keywords failed " + ex.Message + ex.StackTrace);
                return new List<Tuple<string, HashSet<string>>>();
            }
        }


        public class KeywordHelper
        {

            public static string GetInvarientKey(string key)
            {
                string invarientKey = key.ToLower();
                invarientKey = invarientKey.Replace("and", "&");
                invarientKey = invarientKey.Replace(",", "");  //todo more efficient replace...
                invarientKey = invarientKey.Replace("'", "");
                invarientKey = invarientKey.Replace("-", "");
                invarientKey = invarientKey.Replace("_", "");

                //sufjan.stevens 

                //group these chars
                switch (invarientKey)
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
                return invarientKey;
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

            //            public static bool IsCommonParentFolder(string t)
            //            {
            //#if USE_PARENT
            //            switch (t)
            //            {
            //                case "music":
            //                case "complete":
            //                case "@flac":
            //                case "audio":
            //                case "mp#":
            //                case "m?sica":
            //                case "album":
            //                case "flac albums":
            //                case "albums":
            //                    return true;
            //                default:
            //                    return false;
            //            }
            //#else
            //                return false;
            //#endif

            //            }

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


            public Dictionary<string, int> invarientKeyCounts = new Dictionary<string, int>();
            public Dictionary<string, int> realCounts = new Dictionary<string, int>();
            public Dictionary<string, HashSet<string>> invarientToReal = new Dictionary<string, HashSet<string>>();

            public void VoteIfExists(string term)
            {
                string invariantTerm = GetInvarientKey(term);
                if (invarientKeyCounts.ContainsKey(invariantTerm))
                {
                    invarientKeyCounts[invariantTerm]++;
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

                if (invarientToReal.ContainsKey(invariantTerm))
                {
                    invarientToReal[invariantTerm].Add(term);
                }
                else
                {
                    invarientToReal[invariantTerm] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    invarientToReal[invariantTerm].Add(term);
                }
            }

            public void AddKey(string term)
            {
                string invariantTerm = GetInvarientKey(term);
                if (invarientKeyCounts.ContainsKey(invariantTerm))
                {
                    invarientKeyCounts[invariantTerm]++;
                }
                else
                {
                    invarientKeyCounts[invariantTerm] = 1;
                }

                if (realCounts.ContainsKey(term))
                {
                    realCounts[term]++;
                }
                else
                {
                    realCounts[term] = 1;
                }

                if (invarientToReal.ContainsKey(invariantTerm))
                {
                    invarientToReal[invariantTerm].Add(term);
                }
                else
                {
                    invarientToReal[invariantTerm] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    invarientToReal[invariantTerm].Add(term);
                }
            }

            public List<Tuple<string, HashSet<string>>> GetTopCandidates(string searchTerm, int topN)
            {
                //weigh the years, and throw out the just file types.
                string searchTermInvarient = GetInvarientKey(searchTerm);
                foreach (string key in invarientKeyCounts.Keys.ToList())
                {
                    if (IsSingleFileAttributeType(key)) //todo use collection...
                    {
                        invarientKeyCounts.Remove(key);
                    }
                    else if (searchTermInvarient.Contains(key))
                    {
                        invarientKeyCounts.Remove(key);
                    }
                    else if (IsYear(key))
                    {
                        invarientKeyCounts[key] /= 4;
                    }
                    else if (IsCommonAttribute(key))
                    {

                        invarientKeyCounts[key] = (int)(invarientKeyCounts[key] * .6);
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


                var l = invarientKeyCounts.ToList();
                l.Sort((x, y) => y.Value.CompareTo(x.Value));
                List<Tuple<string, HashSet<string>>> keyTerms = new List<Tuple<string, HashSet<string>>>();
                if (topN > l.Count)
                {
                    topN = l.Count;
                }
                for (int i = 0; i < topN; i++)
                {
                    var hs = invarientToReal[l[i].Key];
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

    public class ChipsItemRecyclerAdapter : RecyclerView.Adapter
    {
        private List<ChipDataItem> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            ChipItemView view = ChipItemView.inflate(parent);
            view.setupChildren();
            view.Chip.CheckedChange += Chip_CheckedChange;

            return new ChipItemViewHolder(view as View);


        }

        /// <summary>
        /// multiple for a type should be OR'd together. none means all.
        /// </summary>
        /// <returns></returns>
        public List<ChipDataItem> GetCheckedItemsForType(ChipType type)
        {
            return localDataSet.Where(item => item.ChipType == type && item.IsChecked && item.IsEnabled).ToList();
        }

        private void Chip_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            //results need to update.
            int pos = ((sender as View).Parent.Parent as ChipItemView).ViewHolder.AdapterPosition;
            bool prevValue = localDataSet[pos].IsChecked;
            localDataSet[pos].IsChecked = e.IsChecked;
            if (prevValue != e.IsChecked)
            {
                if (localDataSet[pos].ChipType == ChipType.FileType)
                {
                    if (localDataSet[pos].DisplayText.Contains(" - all"))
                    {
                        string baseType = localDataSet[pos].DisplayText.Replace(" - all", "");
                        for (int i = 0; i < localDataSet.Count; i++)
                        {
                            if (localDataSet[i].DisplayText.Contains(baseType) && localDataSet[i].DisplayText != localDataSet[pos].DisplayText)
                            {
                                localDataSet[i].IsEnabled = !e.IsChecked;
                                this.NotifyItemChanged(i); //needed to turn off animations for this. else doesn't look too good.
                            }
                        }
                    }
                }
                //if changed, then alert to filter.
            }
            //if (e.IsChecked)
            //{
            //    CheckedItems.Add(pos);
            //}
            //else
            //{
            //    CheckedItems.Remove(pos);
            //}
            var searchTab = SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab];
            searchTab.ChipsFilter = SearchFragment.ParseChips(searchTab);
            SearchFragment.Instance.RefreshOnChipChanged();
        }


        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as ChipItemViewHolder).chipItemView.setItem(localDataSet[position]);
        }


        //private void SearchTabLayout_Click(object sender, EventArgs e)
        //{
        //    position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
        //    int tabToGoTo = localDataSet[position];
        //    SearchFragment.Instance.GoToTab(tabToGoTo, false);
        //    SearchTabDialog.Instance.Dismiss();
        //}

        public ChipsItemRecyclerAdapter(List<ChipDataItem> ti)
        {
            if (ti == null)
            {
                localDataSet = new List<ChipDataItem>();
            }
            else
            {
                localDataSet = ti;
            }
        }

    }


    public class ChipItemViewHolder : RecyclerView.ViewHolder
    {
        public ChipItemView chipItemView;


        public ChipItemViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            chipItemView = (ChipItemView)view;
            chipItemView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }
    }

    public enum ChipType
    {
        FileType = 0,
        FileCount = 1,
        Keyword = 2
    }

    public class ChipDataItem
    {
        public readonly string DisplayText;
        public readonly List<string> Children; //this is for "other". this is what the chip actually represents..
        public readonly ChipType ChipType;
        public bool LastInGroup; //last in group AND there is more after it
        public bool IsChecked = false;
        public bool IsEnabled = true; //(-all case)
        public ChipDataItem(ChipType chipType, bool lastInGroup, string displayText)
        {
            this.ChipType = chipType;
            this.LastInGroup = lastInGroup;
            this.DisplayText = displayText;
            this.Children = null;
        }
        public ChipDataItem(ChipType chipType, bool lastInGroup, string displayText, List<string> children)
        {
            this.ChipType = chipType;
            this.LastInGroup = lastInGroup;
            this.DisplayText = displayText;
            this.Children = children;
        }
        public bool HasTag()
        {
            return this.Children != null;
        }
    }


    public class ChipItemView : LinearLayout
    {
        public Chip Chip;
        public View ChipSeparator;
        public View ChipLayout;
        public ChipItemViewHolder ViewHolder;

        public ChipItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chip_item_view, this, true);
            setupChildren();
        }
        public ChipItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chip_item_view, this, true);
            setupChildren();
        }

        public static ChipItemView inflate(ViewGroup parent)
        {
            var c = new ContextThemeWrapper(parent.Context, Resource.Style.MaterialThemeForChip);
            ChipItemView itemView = (ChipItemView)LayoutInflater.From(c).Inflate(Resource.Layout.chip_item_view_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            Chip = FindViewById<Chip>(Resource.Id.chip1);
            ChipSeparator = FindViewById<View>(Resource.Id.chipSeparator);
            ChipLayout = FindViewById<View>(Resource.Id.chipLayout);
        }

        public void setItem(ChipDataItem item)
        {
            Chip.Text = item.DisplayText;
            Chip.Checked = item.IsChecked;

            Chip.Enabled = item.IsEnabled;
            Chip.Clickable = item.IsEnabled;

            if (item.LastInGroup)
            {
                //we already have the right padding due to the separator so set it to 0
                ChipLayout.SetPadding(ChipLayout.PaddingLeft, ChipLayout.PaddingTop, 0, ChipLayout.PaddingBottom);
                ChipSeparator.Visibility = ViewStates.Visible;
            }
            else
            {
                ChipLayout.SetPadding(ChipLayout.PaddingLeft, ChipLayout.PaddingTop, 4, ChipLayout.PaddingBottom);
                ChipSeparator.Visibility = ViewStates.Gone;
            }


        }
    }
}