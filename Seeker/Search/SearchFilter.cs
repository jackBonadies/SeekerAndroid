using Java.Lang;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public partial class SearchFragment
    {
        public class ChipFilter
        {
            //this comes from "mp3 - all" and will match any (== "mp3") or (contains "mp3 ") results
            //the items in these filters are always OR'd
            public ChipFilter()
            {
                AllVarientsFileType = new List<string>();
                SpecificFileType = new List<string>();
                NumFiles = new List<int>();
                FileRanges = new List<Tuple<int, int>>();
                Keywords = new List<string>();
                KeywordInvarient = new List<List<string>>();

            }
            public List<string> AllVarientsFileType;
            public List<string> SpecificFileType;
            public List<int> NumFiles;
            public List<Tuple<int, int>> FileRanges;

            //these are the keywords.  keywords invarient will contain say "Paul and Jake", "Paul & Jake". they are OR'd inner.  both collections outer are AND'd.
            public List<string> Keywords;
            public List<List<string>> KeywordInvarient;

            public bool IsEmpty()
            {
                return (AllVarientsFileType.Count == 0 && SpecificFileType.Count == 0 && NumFiles.Count == 0 && FileRanges.Count == 0 && Keywords.Count == 0 && KeywordInvarient.Count == 0);
            }
        }

        public static ChipFilter ParseChips(SearchTab searchTab)
        {
            ChipFilter chipFilter = new ChipFilter();
            var checkedChips = searchTab.ChipDataItems.Where(i => i.IsChecked).ToList();
            foreach (var chip in checkedChips)
            {
                if (chip.ChipType == ChipType.FileCount)
                {
                    if (chip.DisplayText.EndsWith(" file"))
                    {
                        chipFilter.NumFiles.Add(1);
                    }
                    else if (chip.DisplayText.Contains(" to "))
                    {
                        int endmin = chip.DisplayText.IndexOf(" to ");
                        int min = int.Parse(chip.DisplayText.Substring(0, endmin));
                        int max = int.Parse(chip.DisplayText.Substring(endmin + 4, chip.DisplayText.IndexOf(" files") - (endmin + 4)));
                        chipFilter.FileRanges.Add(new Tuple<int, int>(min, max));
                    }
                    else if (chip.DisplayText.EndsWith(" files"))
                    {
                        chipFilter.NumFiles.Add(int.Parse(chip.DisplayText.Replace(" files", "")));
                    }
                }
                else if (chip.ChipType == ChipType.FileType)
                {
                    if (chip.HasTag())
                    {
                        foreach (var subChipString in chip.Children)
                        {
                            //its okay if this contains "mp3 (other)" say because if it does then by definition it will also contain
                            //mp3 - all bc we dont split groups.
                            if (subChipString.EndsWith(" - all"))
                            {
                                chipFilter.AllVarientsFileType.Add(subChipString.Replace(" - all", ""));
                            }
                            else
                            {
                                chipFilter.SpecificFileType.Add(subChipString);
                            }
                        }
                    }
                    else if (chip.DisplayText.EndsWith(" - all"))
                    {
                        chipFilter.AllVarientsFileType.Add(chip.DisplayText.Replace(" - all", ""));
                    }
                    else
                    {
                        chipFilter.SpecificFileType.Add(chip.DisplayText);
                    }
                }
                else if (chip.ChipType == ChipType.Keyword)
                {
                    if (chip.Children == null)
                    {
                        chipFilter.Keywords.Add(chip.DisplayText);
                    }
                    else
                    {
                        chipFilter.KeywordInvarient.Add(chip.Children);
                    }
                }
            }
            return chipFilter;
        }


        public static void ParseFilterString(SearchTab searchTab)
        {
            List<string> filterStringSplit = searchTab.FilterString.Split(' ').ToList();
            searchTab.WordsToAvoid.Clear();
            searchTab.WordsToInclude.Clear();
            searchTab.FilterSpecialFlags.Clear();
            foreach (string word in filterStringSplit)
            {
                if (word.Contains("mbr:") || word.Contains("minbitrate:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinBitRateKBS = Integer.ParseInt(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfs:") || word.Contains("minfilesize:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinFileSizeMB = (Integer.ParseInt(word.Split(':')[1]));
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfif:") || word.Contains("minfilesinfolder:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinFoldersInFile = Integer.ParseInt(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word == "isvbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsVBR = true;
                }
                else if (word == "iscbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsCBR = true;
                }
                else if (word.StartsWith('-'))
                {
                    if (word.Length > 1)//if just '-' dont remove everything. just skip it.
                    {
                        searchTab.WordsToAvoid.Add(word.Substring(1)); //skip the '-'
                    }
                }
                else
                {
                    searchTab.WordsToInclude.Add(word);
                }
            }
        }

        private bool MatchesChipCriteria(SearchResponse s, ChipFilter chipFilter, bool hideLocked)
        {
            if (chipFilter == null || chipFilter.IsEmpty())
            {
                return true;
            }
            else
            {
                bool match = chipFilter.NumFiles.Count == 0 && chipFilter.FileRanges.Count == 0;
                int fcount = hideLocked ? s.FileCount : s.FileCount + s.LockedFileCount;
                foreach (int num in chipFilter.NumFiles)
                {
                    if (fcount == num)
                    {
                        match = true;
                    }
                }
                foreach (Tuple<int, int> range in chipFilter.FileRanges)
                {
                    if (fcount >= range.Item1 && fcount <= range.Item2)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }

                match = chipFilter.AllVarientsFileType.Count == 0 && chipFilter.SpecificFileType.Count == 0;
                foreach (string varient in chipFilter.AllVarientsFileType)
                {
                    if (s.GetDominantFileType(hideLocked, out _) == varient || s.GetDominantFileType(hideLocked, out _).Contains(varient + " "))
                    {
                        match = true;
                    }
                }
                foreach (string specific in chipFilter.SpecificFileType)
                {
                    if (s.GetDominantFileType(hideLocked, out _) == specific)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }

                string fullFname = s.Files.FirstOrDefault()?.Filename ?? s.LockedFiles.FirstOrDefault().Filename;
                foreach (string keyword in chipFilter.Keywords)
                {
                    if (!Common.Helpers.GetFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase) &&
                        !Common.Helpers.GetParentFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return false;
                    }
                }
                foreach (List<string> keywordsInvar in chipFilter.KeywordInvarient)
                {
                    //do any match?
                    bool anyMatch = false;
                    foreach (string keyword in keywordsInvar)
                    {
                        if (Common.Helpers.GetFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase) ||
                            Common.Helpers.GetParentFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                        {
                            anyMatch = true;
                            break;
                        }
                    }
                    if (!anyMatch)
                    {
                        return false;
                    }
                }
                if (!match)
                {
                    return false;
                }

                return true;
            }
        }


        private bool MatchesCriteria(SearchResponse s, bool hideLocked)
        {
            foreach (File f in s.GetFiles(hideLocked))
            {
                string dirString = Common.Helpers.GetFolderNameFromFile(f.Filename);
                string fileString = CommonHelpers.GetFileNameFromFile(f.Filename);
                foreach (string avoid in SearchTabHelper.WordsToAvoid)
                {
                    if (dirString.Contains(avoid, StringComparison.OrdinalIgnoreCase) || fileString.Contains(avoid, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                bool includesAll = true;
                foreach (string include in SearchTabHelper.WordsToInclude)
                {
                    if (!dirString.Contains(include, StringComparison.OrdinalIgnoreCase) && !fileString.Contains(include, StringComparison.OrdinalIgnoreCase))
                    {
                        includesAll = false;
                        break;
                    }
                }
                if (includesAll)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
