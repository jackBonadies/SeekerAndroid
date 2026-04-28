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
        public FormatFilterType FormatFilter = FormatFilterType.Any;

        public static readonly HashSet<string> LosslessExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flac", "wav", "aiff", "alac", "ape", "wv", "dsf", "dff"
        };

        public static readonly HashSet<string> LossyExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mp3", "aac", "ogg", "opus", "wma", "m4a"
        };

        public void Clear()
        {
            ContainsSpecialFlags = false;
            MinFoldersInFile = 0;
            MinFileSizeMB = 0;
            MinBitRateKBS = 0;
            IsVBR = false;
            IsCBR = false;
            FormatFilter = FormatFilterType.Any;
        }
    }

    public abstract class ChipDataItem
    {
        public abstract string GetFullDisplayText();
        public abstract ChipType ChipType { get; }
        public bool LastInGroup;
        public bool IsChecked = false;
        public bool IsEnabled = true;
    }

    public class FileTypeChipDataItem : ChipDataItem
    {
        public override ChipType ChipType => ChipType.FileType;
        public readonly string BaseFileType;
        public readonly List<string>? Children;
        public bool IsAllCase = false;

        public bool IsOtherCase => Children != null;

        public FileTypeChipDataItem(string baseFileType)
        {
            this.BaseFileType = baseFileType;
        }

        public FileTypeChipDataItem(string baseFileType, bool isAllCase)
        {
            this.BaseFileType = baseFileType;
            this.IsAllCase = isAllCase;
        }

        public FileTypeChipDataItem(string baseFileType, List<string> children)
        {
            this.BaseFileType = baseFileType;
            this.Children = children;
        }

        public override string GetFullDisplayText()
        {
            if (IsAllCase)
            {
                return BaseFileType + ChipsHelper.ALL_SUFFIX;
            }
            return BaseFileType;
        }
    }

    public class FileCountChipDataItem : ChipDataItem
    {
        public override ChipType ChipType => ChipType.FileCount;
        public readonly int FileCountStart;
        public readonly int FileCountEnd;

        public FileCountChipDataItem(int fileCountStart, int fileCountEnd)
        {
            this.FileCountStart = fileCountStart;
            this.FileCountEnd = fileCountEnd;
        }

        public override string GetFullDisplayText()
        {
            if (FileCountStart == FileCountEnd)
            {
                if (FileCountStart == 1)
                {
                    return "1 file";
                }
                return $"{FileCountStart} files";
            }
            return $"{FileCountStart}-{FileCountEnd} files";
        }
    }

    public class KeywordChipDataItem : ChipDataItem
    {
        public override ChipType ChipType => ChipType.Keyword;
        public readonly string Keyword;
        public readonly HashSet<string>? InvariantVariants;

        public KeywordChipDataItem(string keyword)
        {
            this.Keyword = keyword;
        }

        public KeywordChipDataItem(string keyword, HashSet<string> invariantVariants)
        {
            this.Keyword = keyword;
            this.InvariantVariants = invariantVariants;
        }

        public override string GetFullDisplayText()
        {
            return Keyword;
        }
    }

    public class ChipFilter
    {
        //this comes from "mp3 - all" and will match any (== "mp3") or (contains "mp3 ") results
        //the items in these filters are always OR'd
        public ChipFilter()
        {
            AllVariantsFileType = new List<string>();
            SpecificFileType = new List<string>();
            NumFiles = new List<int>();
            FileRanges = new List<Tuple<int, int>>();
            Keywords = new List<string>();
            KeywordInvariant = new List<HashSet<string>>();

        }
        public List<string> AllVariantsFileType;
        public List<string> SpecificFileType;
        public List<int> NumFiles;
        public List<Tuple<int, int>> FileRanges;

        //these are the keywords.  keywords invariant will contain say "Paul and Jake", "Paul & Jake". they are OR'd inner.  both collections outer are AND'd.
        public List<string> Keywords;
        public List<HashSet<string>> KeywordInvariant;

        public bool IsEmpty()
        {
            return (AllVariantsFileType.Count == 0 && SpecificFileType.Count == 0 && NumFiles.Count == 0 && FileRanges.Count == 0 && Keywords.Count == 0 && KeywordInvariant.Count == 0);
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
                switch (chip)
                {
                    case FileCountChipDataItem fc:
                        if (fc.FileCountStart == fc.FileCountEnd)
                        {
                            chipFilter.NumFiles.Add(fc.FileCountStart);
                        }
                        else
                        {
                            chipFilter.FileRanges.Add(new Tuple<int, int>(fc.FileCountStart, fc.FileCountEnd));
                        }
                        break;

                    case FileTypeChipDataItem ft:
                        if (ft.Children != null)
                        {
                            foreach (var subChipString in ft.Children)
                            {
                                if (subChipString.EndsWith(ChipsHelper.ALL_SUFFIX))
                                {
                                    chipFilter.AllVariantsFileType.Add(subChipString.Replace(ChipsHelper.ALL_SUFFIX, ""));
                                }
                                else
                                {
                                    chipFilter.SpecificFileType.Add(subChipString);
                                }
                            }
                        }
                        else if (ft.IsAllCase)
                        {
                            chipFilter.AllVariantsFileType.Add(ft.BaseFileType);
                        }
                        else
                        {
                            chipFilter.SpecificFileType.Add(ft.BaseFileType);
                        }
                        break;

                    case KeywordChipDataItem kw:
                        if (kw.InvariantVariants == null)
                        {
                            chipFilter.Keywords.Add(kw.Keyword);
                        }
                        else
                        {
                            chipFilter.KeywordInvariant.Add(kw.InvariantVariants);
                        }
                        break;
                }
            }
            return chipFilter;
        }


        public static void ParseFilterString(string filterString, List<string> wordsToAvoid, List<string> wordsToInclude)
        {
            ParseFilterString(filterString, wordsToAvoid, wordsToInclude, null);
        }

        public static void ParseFilterString(string filterString, List<string> wordsToAvoid, List<string> wordsToInclude, FilterSpecialFlags filterSpecialFlags)
        {
            List<string> filterStringSplit = filterString.Split(' ').ToList();
            foreach (string word in filterStringSplit)
            {
                if (filterSpecialFlags != null && (word.Contains("mbr:") || word.Contains("minbitrate:")))
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
                else if (filterSpecialFlags != null && (word.Contains("mfs:") || word.Contains("minfilesize:")))
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
                else if (filterSpecialFlags != null && (word.Contains("mfif:") || word.Contains("minfilesinfolder:")))
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
                else if (filterSpecialFlags != null && word == "isvbr")
                {
                    filterSpecialFlags.ContainsSpecialFlags = true;
                    filterSpecialFlags.IsVBR = true;
                }
                else if (filterSpecialFlags != null && word == "iscbr")
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

                match = chipFilter.AllVariantsFileType.Count == 0 && chipFilter.SpecificFileType.Count == 0;
                foreach (string variant in chipFilter.AllVariantsFileType)
                {
                    if (s.GetDominantFileTypeAndBitRate(hideLocked, out _) == variant || s.GetDominantFileTypeAndBitRate(hideLocked, out _).Contains(variant + " "))
                    {
                        match = true;
                    }
                }
                foreach (string specific in chipFilter.SpecificFileType)
                {
                    if (s.GetDominantFileTypeAndBitRate(hideLocked, out _) == specific)
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
                foreach (HashSet<string> keywordsInvar in chipFilter.KeywordInvariant)
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
            if (flags.FormatFilter != FormatFilterType.Any)
            {
                string dominantType = s.GetDominantFileTypeAndBitRate(hideLocked, out _);
                string ext = dominantType.Contains(' ') ? dominantType.Substring(0, dominantType.IndexOf(' ')) : dominantType;
                if (flags.FormatFilter == FormatFilterType.Lossless && !FilterSpecialFlags.LosslessExtensions.Contains(ext))
                {
                    return false;
                }
                if (flags.FormatFilter == FormatFilterType.Lossy && !FilterSpecialFlags.LossyExtensions.Contains(ext))
                {
                    return false;
                }
            }
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
                        break;
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
                    if ((int)(f.BitRate) >= flags.MinBitRateKBS)
                    {
                        match = true;
                        break;
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
                        break;
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
                        break;
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
