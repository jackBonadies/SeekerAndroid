using Common;
using Soulseek;
using System;
using System.Collections.Generic;

namespace Seeker
{
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

namespace Common
{
    public class TreeNode<T>
    {
        public T Data;
        public bool IsFilteredOut = false;
        public bool IsLocked = false;
        public TreeNode<T>? Parent;
        public ICollection<TreeNode<T>> Children;

        public TreeNode(T data, bool isLocked)
        {
            this.Data = data;
            this.Children = new LinkedList<TreeNode<T>>();
            this.IsLocked = isLocked;
        }


        public TreeNode<T> AddChild(T child, bool isChildLocked)
        {
            TreeNode<T> childNode = new TreeNode<T>(child, isChildLocked) { Parent = this };
            this.Children.Add(childNode);
            return childNode;
        }
    }
}
