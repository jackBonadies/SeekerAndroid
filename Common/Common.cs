using Soulseek;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class Algorithms
    {
        private static string GetLongestBeginningSubstring(string a, string b)
        {
            int maxLen = Math.Min(a.Length, b.Length);
            int maxIndexInCommon = 0;
            for (int i = 0; i < maxLen; i++)
            {
                if (a[i] == b[i])
                {
                    maxIndexInCommon++;
                }
                else
                {
                    break;
                }
            }
            return a.Substring(0, maxIndexInCommon);//this can be empty..
        }

        public static string GetLongestCommonParent(string a, string b)
        {
            if (!a.Contains('\\') || !b.Contains('\\'))
            {
                return string.Empty;
            }
            string allOtherThanCurrentDirA = a.Substring(0, a.LastIndexOf('\\') + 1);
            string allOtherThanCurrentDirB = b.Substring(0, b.LastIndexOf('\\') + 1);
            int maxLen = Math.Min(allOtherThanCurrentDirA.Length, allOtherThanCurrentDirB.Length);
            int maxIndexInCommon = 0;
            for (int i = 0; i < maxLen; i++)
            {
                if (a[i] == b[i])
                {
                    maxIndexInCommon++;
                }
                else
                {
                    break;
                }
            }
            string potential = a.Substring(0, maxIndexInCommon);
            int lastIndex = potential.LastIndexOf('\\');
            if (lastIndex == -1)
            {
                return string.Empty;
            }
            else
            {
                return potential.Substring(0, lastIndex);
            }
        }

        private static TreeNode<Directory> AddChildNode(TreeNode<Directory> curNode, Tuple<Directory, bool> dInfo, bool filter, List<string> wordsToAvoid, List<string> wordsToInclude)
        {
            if (!filter)
            {
                return curNode.AddChild(dInfo.Item1, dInfo.Item2); //add child and now curNode points to the next guy
            }
            else
            {
                var nextNode = curNode.AddChild(FilterDirectory(dInfo.Item1, wordsToAvoid, wordsToInclude), dInfo.Item2);
                nextNode.IsFilteredOut = true;
                return nextNode;
            }
        }

        public static TreeNode<Directory> CreateTreeCore(BrowseResponse b, bool filter, List<string> wordsToAvoid, List<string> wordsToInclude, string username, bool hideLocked)
        {

            TreeNode<Directory> rootNode = null;

            String prevDirName = string.Empty;
            TreeNode<Directory> curNode = null;

            TreeNode<Directory> prevNodeDebug = null;

            Tuple<Directory, bool>[] dirInfoArray = null; //true if locked.
            if (hideLocked)
            {
                dirInfoArray = b.Directories.Select(d => new Tuple<Directory, bool>(d, false)).ToArray();
            }
            else
            {
                dirInfoArray = b.Directories.Select(d => new Tuple<Directory, bool>(d, false)).Concat(b.LockedDirectories.Select(d => new Tuple<Directory, bool>(d, true))).ToArray();
            }
            //TODO I think moving this out of a lambda would make it faster, but need to do unit tests first!
            Array.Sort(dirInfoArray, (x, y) =>
            {
                string nx = x.Item1.Name;
                string ny = y.Item1.Name;
                int len = Math.Min(nx.Length, ny.Length);

                for (int i = 0; i < len; i++)
                {
                    char cx = nx[i], cy = ny[i];

                    if (cx == '\\' || cy == '\\')
                    {
                        if (cx != cy)
                        {
                            return cx == '\\' ? -1 : 1;
                        }
                        continue;
                    }

                    int comp = char.ToLowerInvariant(cx).CompareTo(char.ToLowerInvariant(cy));
                    if (comp != 0)
                    {
                        // if they are different case invariant (this prevents say B getting placed before lowercase a)
                        return comp;
                    } 
                    else
                    {
                        int comp1 = cx.CompareTo(cy); 
                        if (comp1 != 0)
                        {
                            // if they are different case sensitive (some users will have Music and music. these are distinct folders)
                            return comp1;
                        }
                    }
                }

                return nx.Length - ny.Length;
            });

            if (dirInfoArray[0].Item1.Name == "\\")
            {
                dirInfoArray = dirInfoArray.Skip(1).ToArray();
            }
            else if (dirInfoArray[0].Item1.Name.EndsWith("\\"))
            {
                dirInfoArray[0] = new Tuple<Directory, bool>(new Directory(dirInfoArray[0].Item1.Name.Substring(0, dirInfoArray[0].Item1.Name.Length - 1), dirInfoArray[0].Item1.Files), dirInfoArray[0].Item2);
            }

            bool emptyRoot = false;
            if (Helpers.IsChildDirString(dirInfoArray[dirInfoArray.Length - 1].Item1.Name, dirInfoArray[0].Item1.Name, true) || dirInfoArray[dirInfoArray.Length - 1].Item1.Name.Equals(dirInfoArray[0].Item1.Name))
            {
                //normal single tree case..
            }
            else
            {
                string newRootDirName = GetLongestCommonParent(dirInfoArray[dirInfoArray.Length - 1].Item1.Name, dirInfoArray[0].Item1.Name);
                if (newRootDirName == string.Empty)
                {
                    newRootDirName = "";
                    emptyRoot = true;
                }
                if (newRootDirName.LastIndexOf("\\") != -1)
                {
                    newRootDirName = newRootDirName.Substring(0, newRootDirName.LastIndexOf("\\"));
                }
                Directory rootDirectory = new Directory(newRootDirName);

                //kickstart things
                rootNode = new TreeNode<Directory>(rootDirectory, false); //the children will set themselves as locked
                prevDirName = newRootDirName;
                curNode = rootNode;
            }



            foreach (Tuple<Directory, bool> dInfo in dirInfoArray)
            {
                if (prevDirName == string.Empty && !emptyRoot) //this means that you did not set anything. sometimes the root literally IS empty..
                {
                    rootNode = new TreeNode<Directory>(dInfo.Item1, dInfo.Item2);
                    curNode = rootNode;
                    prevDirName = dInfo.Item1.Name;
                }
                else if (Helpers.IsChildDirString(dInfo.Item1.Name, prevDirName, curNode?.Parent == null)) //if the next directory contains the previous in its path then it is a child. //this is not true... it will set music as the child of mu //TODO !!!!!
                {
                    curNode = AddChildNode(curNode, dInfo, filter, wordsToAvoid, wordsToInclude);
                    prevDirName = dInfo.Item1.Name;
                }
                else
                {
                    prevNodeDebug = new TreeNode<Directory>(curNode.Data, dInfo.Item2);
                    curNode = curNode.Parent; //This is not good if the first node is not the root...
                    while (!Helpers.IsChildDirString(dInfo.Item1.Name, curNode.Data.Name, curNode?.Parent == null))
                    {
                        if (curNode.Parent == null)
                        {
                            break; //this might be hiding an error
                        }
                        curNode = curNode.Parent; // may have to go up more than one
                    }
                    curNode = AddChildNode(curNode, dInfo, filter, wordsToAvoid, wordsToInclude);
                    prevDirName = dInfo.Item1.Name;
                }
            }

            if (filter)
            {
                //unhide any ones with valid Files (by default they are all hidden).
                IterateTreeAndUnsetFilteredForValid(rootNode);
            }

            return rootNode;
        }

        private static void IterateTreeAndUnsetFilteredForValid(TreeNode<Directory> root)
        {
            if (root.Data.FileCount != 0)
            {
                //set self and all parents as unhidden
                SetSelfAndAllParentsAsUnFilteredOut(root);
            }
            foreach (TreeNode<Directory> child in root.Children)
            {
                IterateTreeAndUnsetFilteredForValid(child);
            }
        }

        private static void SetSelfAndAllParentsAsUnFilteredOut(TreeNode<Directory> node)
        {
            node.IsFilteredOut = false;
            if (node.Parent == null)
            {
                return;//we reached the top, mission accomplished.
            }
            if (node.Parent.IsFilteredOut)
            {
                SetSelfAndAllParentsAsUnFilteredOut(node.Parent);
            }
        }

        public static Directory FilterDirectory(Directory d, List<string> wordsToAvoid, List<string> wordsToInclude)
        {
            if (d.FileCount == 0)
            {
                return d;
            }
            List<File> files = new List<File>();
            string fullyQualDirName = d.Name;
            foreach (File f in d.Files)
            {
                string fullName = fullyQualDirName + f.Filename;

                bool badTerm = false;
                if (wordsToAvoid != null)
                {
                    foreach (string avoid in wordsToAvoid)
                    {
                        if (fullName.Contains(avoid, StringComparison.OrdinalIgnoreCase))
                        {
                            //return false;
                            badTerm = true;
                        }
                    }
                }
                if (badTerm)
                {
                    continue; //i.e. its not going to be included..
                }
                bool includesAll = true;
                if (wordsToInclude != null)
                {
                    foreach (string include in wordsToInclude)
                    {
                        if (!fullName.Contains(include, StringComparison.OrdinalIgnoreCase))
                        {
                            includesAll = false;
                            break;
                        }
                    }
                }
                if (includesAll)
                {
                    files.Add(f);
                }
            }
            return new Directory(d.Name, files, d.DecodedViaLatin1);
        }
    }

    public class Helpers
    {
        /// <summary>
        /// Replaces d.Name.Contains(prevDirName) which fails for Mu, Music
        /// </summary>
        /// <param name="possibleChild"></param>
        /// <param name="possibleParent"></param>
        /// <returns></returns>
        public static bool IsChildDirString(string possibleChild, string possibleParent, bool rootCase)
        {
            if (rootCase)
            {
                if (possibleChild.LastIndexOf("\\") == -1 && possibleParent.LastIndexOf("\\") == -1)
                {
                    if (possibleParent.IndexOf(':') == (possibleParent.Length - 1)) //i.e. primary:
                    {
                        return possibleChild.Contains(possibleParent);
                    }
                    else if (possibleChild.Equals(possibleParent))
                    {
                        return true; //else the primary:music case fails.
                    }
                }
            }
            int pathSep = possibleChild.LastIndexOf("\\");
            if (pathSep == -1)
            {
                return false;
            }
            else
            {
                //fails in possibleChild="Music (1)\\test" possibleParent="Music" case
                //return possibleChild.Substring(0, pathSep).Contains(possibleParent);

                return possibleChild.Substring(0, pathSep + 1).StartsWith(possibleParent + "\\") || possibleChild.Substring(0, pathSep) == possibleParent || possibleParent == String.Empty;
            }
        }

        public static string GetFullPathFromFile(string fullFilename)
        {
            var lastIndex = fullFilename.LastIndexOf('\\');
            return fullFilename.Substring(0, lastIndex);
        }

        public static string GetFolderNameFromFile(string filename, int levels = 1)
        {
            try
            {
                int folderCount = 0;
                int index = -1; //-1 is important.  i.e. in the case of Folder\test.mp3, it can be Folder.
                int firstIndex = int.MaxValue;
                for (int i = filename.Length - 1; i >= 0; i--)
                {
                    if (filename[i] == '\\')
                    {
                        folderCount++;
                        if (firstIndex == int.MaxValue)
                        {
                            //strip off the file name
                            firstIndex = i;
                        }
                        if (folderCount == (levels + 1))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                return filename.Substring(index + 1, firstIndex - index - 1);
            }
            catch
            {
                return "";
            }
        }

        public static string GetParentFolderNameFromFile(string filename)
        {
            try
            {
                string parent = filename.Substring(0, filename.LastIndexOf('\\'));
                parent = parent.Substring(0, parent.LastIndexOf('\\'));
                parent = parent.Substring(parent.LastIndexOf('\\') + 1);
                return parent;
            }
            catch
            {
                return "";
            }
        }
    }

    public class TreeNode<T> : IEnumerable<TreeNode<T>>
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

        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
