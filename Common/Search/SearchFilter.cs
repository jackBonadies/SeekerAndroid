using Seeker.Extensions.SearchResponseExtensions;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public class FilterSpecialFlags
    {
        public bool ContainsSpecialFlags = false;
        public int MinFoldersInFile = 0;
        public int MinFileSizeMB = 0;
        public int MinBitRateKBS = 0;
        public bool IsVBR = false;
        public bool IsCBR = false;
        public void Clear()
        {
            ContainsSpecialFlags = false;
            MinFoldersInFile = 0;
            MinFileSizeMB = 0;
            MinBitRateKBS = 0;
            IsVBR = false;
            IsCBR = false;
        }
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

    public static class SearchFilter
    {
        public static ChipFilter ParseChips(List<ChipDataItem> chipDataItems)
        {
            ChipFilter chipFilter = new ChipFilter();
            var checkedChips = chipDataItems.Where(i => i.IsChecked).ToList();
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


        //TODO2026 - merge with browseutil
        public static void ParseFilterString(string filterString, List<string> wordsToAvoid, List<string> wordsToInclude, FilterSpecialFlags filterSpecialFlags)
        {
            List<string> filterStringSplit = filterString.Split(' ').ToList();
            wordsToAvoid.Clear();
            wordsToInclude.Clear();
            filterSpecialFlags.Clear();
            foreach (string word in filterStringSplit)
            {
                if (word.Contains("mbr:") || word.Contains("minbitrate:"))
                {
                    filterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        filterSpecialFlags.MinBitRateKBS = int.Parse(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfs:") || word.Contains("minfilesize:"))
                {
                    filterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        filterSpecialFlags.MinFileSizeMB = int.Parse(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfif:") || word.Contains("minfilesinfolder:"))
                {
                    filterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        filterSpecialFlags.MinFoldersInFile = int.Parse(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word == "isvbr")
                {
                    filterSpecialFlags.ContainsSpecialFlags = true;
                    filterSpecialFlags.IsVBR = true;
                }
                else if (word == "iscbr")
                {
                    filterSpecialFlags.ContainsSpecialFlags = true;
                    filterSpecialFlags.IsCBR = true;
                }
                else if (word.StartsWith('-'))
                {
                    if (word.Length > 1)//if just '-' dont remove everything. just skip it.
                    {
                        wordsToAvoid.Add(word.Substring(1)); //skip the '-'
                    }
                }
                else
                {
                    wordsToInclude.Add(word);
                }
            }
        }

        public static bool MatchesChipCriteria(SearchResponse s, ChipFilter chipFilter, bool hideLocked)
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


        public static bool MatchesCriteria(SearchResponse s, bool hideLocked, List<string> wordsToAvoid, List<string> wordsToInclude)
        {
            foreach (File f in s.GetFiles(hideLocked))
            {
                string dirString = Common.Helpers.GetFolderNameFromFile(f.Filename);
                string fileString = SimpleHelpers.GetFileNameFromFile(f.Filename);
                foreach (string avoid in wordsToAvoid)
                {
                    if (dirString.Contains(avoid, StringComparison.OrdinalIgnoreCase) || fileString.Contains(avoid, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                bool includesAll = true;
                foreach (string include in wordsToInclude)
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

        public static bool MatchesSpecialFlags(SearchResponse s, FilterSpecialFlags flags, bool hideLocked)
        {
            if (flags.MinFoldersInFile != 0)
            {
                if (flags.MinFoldersInFile > (hideLocked ? s.Files.Count : (s.Files.Count + s.LockedFiles.Count)))
                {
                    return false;
                }
            }
            if (flags.MinFileSizeMB != 0)
            {
                bool match = false;
                foreach (Soulseek.File f in s.GetFiles(hideLocked))
                {
                    int mb = (int)(f.Size) / (1024 * 1024);
                    if (mb > flags.MinFileSizeMB)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }
            }
            if (flags.MinBitRateKBS != 0)
            {
                bool match = false;
                foreach (Soulseek.File f in s.GetFiles(hideLocked))
                {
                    if (f.BitRate == null || !(f.BitRate.HasValue))
                    {
                        continue;
                    }
                    if ((int)(f.BitRate) > flags.MinBitRateKBS)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }
            }
            if (flags.IsCBR)
            {
                bool match = false;
                foreach (Soulseek.File f in s.GetFiles(hideLocked))
                {
                    if (f.IsVariableBitRate == false)//this is bool? can have no value...
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }
            }
            if (flags.IsVBR)
            {
                bool match = false;
                foreach (Soulseek.File f in s.GetFiles(hideLocked))
                {
                    if (f.IsVariableBitRate == true)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool MatchesAllCriteria(SearchResponse s, ChipFilter chipsFilter, FilterSpecialFlags filterSpecialFlags, List<string> wordsToAvoid, List<string> wordsToInclude, bool hideLocked)
        {
            if (!MatchesCriteria(s, hideLocked, wordsToAvoid, wordsToInclude))
            {
                return false;
            }
            if (!MatchesChipCriteria(s, chipsFilter, hideLocked))
            {
                return false;
            }
            //so it matches the word criteria.  now lets see if it matches the flags if any...
            if (!filterSpecialFlags.ContainsSpecialFlags)
            {
                return true;
            }
            return MatchesSpecialFlags(s, filterSpecialFlags, hideLocked);
        }
    }
}
