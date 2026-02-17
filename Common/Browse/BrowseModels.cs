using Common;
using Soulseek;
using System.Collections.Generic;

namespace Seeker
{
    public class BrowseFilter
    {
        public string FilterString { get; private set; }
        public List<string> WordsToAvoid { get; private set; } = new List<string>();
        public List<string> WordsToInclude { get; private set; } = new List<string>();
        public bool IsFiltered { 
            get
            {
                return !string.IsNullOrEmpty(FilterString);
            }
        }

        public void Set(string filterString)
        {
            FilterString = filterString;
            WordsToAvoid.Clear();
            WordsToInclude.Clear();
            //FilterSpecialFlags.Clear(); //TODO why?
            var filterStringSplit = FilterString.Split(' ');
            foreach (string word in filterStringSplit)
            {
                //if (word.Contains("mbr:") || word.Contains("minbitrate:"))
                //{
                //    FilterSpecialFlags.ContainsSpecialFlags = true;
                //    try
                //    {
                //        FilterSpecialFlags.MinBitRateKBS = Integer.ParseInt(word.Split(':')[1]);
                //    }
                //    catch (System.Exception)
                //    {

                //    }
                //}
                //else if (word.Contains("mfs:") || word.Contains("minfilesize:"))
                //{
                //    FilterSpecialFlags.ContainsSpecialFlags = true;
                //    try
                //    {
                //        FilterSpecialFlags.MinFileSizeMB = (Integer.ParseInt(word.Split(':')[1]));
                //    }
                //    catch (System.Exception)
                //    {

                //    }
                //}
                //else if (word.Contains("mfif:") || word.Contains("minfilesinfolder:"))
                //{
                //    FilterSpecialFlags.ContainsSpecialFlags = true;
                //    try
                //    {
                //        FilterSpecialFlags.MinFoldersInFile = Integer.ParseInt(word.Split(':')[1]);
                //    }
                //    catch (System.Exception)
                //    {

                //    }
                //}
                //else if (word == "isvbr")
                //{
                //    FilterSpecialFlags.ContainsSpecialFlags = true;
                //    FilterSpecialFlags.IsVBR = true;
                //}
                //else if (word == "iscbr")
                //{
                //    FilterSpecialFlags.ContainsSpecialFlags = true;
                //    FilterSpecialFlags.IsCBR = true;
                //}
                if (word.StartsWith('-'))
                {
                    if (word.Length > 1)//if just '-' dont remove everything. just skip it.
                    {
                        WordsToAvoid.Add(word.Substring(1)); //skip the '-'
                    }
                }
                else
                {
                    WordsToInclude.Add(word);
                }
            }
        }

        public void Reset()
        {
            FilterString = null;
            WordsToAvoid.Clear();
            WordsToInclude.Clear();
        }
    }
    public class FileLockedUnlockedWrapper
    {
        public Soulseek.File File;
        public bool IsLocked;
        public FileLockedUnlockedWrapper(Soulseek.File _file, bool _isLocked)
        {
            File = _file;
            IsLocked = _isLocked;
        }
    }

    public class FolderSummary
    {
        public int LengthSeconds = 0;
        public long SizeBytes = 0;
        public int NumFiles = 0;
        public int NumSubFolders = 0;
        public void AddFile(Soulseek.File file)
        {
            if (file.Length.HasValue)
            {
                LengthSeconds += file.Length.Value;
            }
            SizeBytes += file.Size;
            NumFiles++;
        }
    }

    public class PathItem
    {
        public string DisplayName;
        public bool IsLastNode;
        public PathItem(string displayName, bool isLastNode)
        {
            DisplayName = displayName;
            IsLastNode = isLastNode;
        }
    }

    public class FullFileInfo
    {
        public long Size = 0;
        public string FullFileName = string.Empty;
        public int Depth = 1;
        public bool wasFilenameLatin1Decoded = false;
        public bool wasFolderLatin1Decoded = false;
    }

    public class DataItem
    {
        private string Name = "";
        public Directory Directory;
        public Soulseek.File File;
        public TreeNode<Directory> Node;
        public DataItem(Directory d, TreeNode<Directory> n)
        {
            Name = d.Name; //this is the full name (other than file).. i.e. primary:Music\\Soulseek Complete
            Directory = d;
            Node = n;
        }
        public DataItem(Soulseek.File f, TreeNode<Directory> n)
        {
            Name = f.Filename;
            File = f;
            Node = n;
        }
        public bool IsDirectory()
        {
            return Directory != null;
        }

        public string GetDisplayName()
        {
            if (IsDirectory())
            {
                if (this.Node.IsLocked)
                {
                    return SimpleHelpers.LOCK_EMOJI + SimpleHelpers.GetFileNameFromFile(Name);
                }
                else
                {
                    return SimpleHelpers.GetFileNameFromFile(Name);
                }
            }
            else
            {
                if (this.Node.IsLocked)
                {
                    return SimpleHelpers.LOCK_EMOJI + Name;
                }
                else
                {
                    return Name;
                }
            }
        }
    }
}
