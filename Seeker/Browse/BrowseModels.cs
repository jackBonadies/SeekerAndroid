using Common;
using Seeker.Helpers;
using Soulseek;

namespace Seeker
{
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
                    return new System.String(Java.Lang.Character.ToChars(0x1F512)) + CommonHelpers.GetFileNameFromFile(Name);
                }
                else
                {
                    return CommonHelpers.GetFileNameFromFile(Name);
                }
            }
            else
            {
                if (this.Node.IsLocked)
                {
                    return new System.String(Java.Lang.Character.ToChars(0x1F512)) + Name;
                }
                else
                {
                    return Name;
                }
            }
        }
    }
}
