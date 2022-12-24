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
            //TreeNode<Directory> rootNode = null;
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
            //StringComparer alphabetComparer = StringComparer.Create(new System.Globalization.CultureInfo("en-US"), true); //else 'a' is 26 behind 'A'
            Array.Sort(dirInfoArray, (x, y) =>
            {
                int len1 = x.Item1.Name.Count();
                int len2 = y.Item1.Name.Count();
                int len = Math.Min(len1, len2);
                for (int i = 0; i < len; i++)
                {
                    char cx = x.Item1.Name[i];
                    char cy = y.Item1.Name[i];
                    if (cx == '\\' || cy == '\\')
                    {
                        if (cx == '\\' && cy != '\\')
                        {
                            return -1;
                        }
                        if (cx != '\\' && cy == '\\')
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        //int comp = System.String.Compare(x.Name, i, y.Name, i , 1);
                        int comp = char.ToLowerInvariant(cx).CompareTo(char.ToLowerInvariant(cy));
                        if (comp != 0)
                        {
                            return comp;
                        }
                    }
                }
                return len1 - len2;
            }
            ); //sometimes i dont quite think they are sorted.
            //sorting alphabetically seems weird, but since it puts shorter strings in front of longer ones, it does actually sort the highest parent 1st, etc.

            //sorting fails as it does not consider \\ higher than other chars, etc.
            //Music
            //music 3
            //Music\test
            //fixed with custom comparer



            //normally peoples files look like 
            //@@datd\complete
            //@@datd\complete\1990
            //@@datd\complete\1990\test
            //but sometimes they do not have a common parent! they are not a tree but many different trees (which soulseek allows)
            //in that case we need to make a common root, as the directory everyone has (even if its the fake "@@adfadf" directory NOT TRUE)
            //I think a quick hack would be.. is the first directory name contained in the last directory name

            //User case (This is SoulseekQT Im guessing)
            //@@bvenl\0
            //@@bvenl\1
            //@@bvenl\2
            //@@bvenl\2\complete
            //@@bvenl\2\complete\1990
            //@@bvenl\2\complete\1990\test


            //User
            //@@pulvh\FLAC Library
            //@@pulvh\Old School

            //User (This is Nicotine multi-root Im guessing)
            //__INTERNAL_ERROR__P:\\My Videos\\++Music SD++\\ArtistName"
            //__INTERNAL_ERROR__P:\\My Videos\\++Music SD++\\ArtistName"
            //__INTERNAL_ERROR__P:\\My Videos\\++Music SD++\\ArtistName"
            //FLAC"
            //FLAC\..."...
            //FLAC\\++Various Artists++\\Artist"
            //NOTE THERE IS NO FAKE @@lskjdf
            //sometimes the root is the empty string

            //User - the first is literally just '\\' not an actual directory name... (old PowerPC Mac version??)
            //\\
            //\\Volumes
            //...
            //"\\Volumes\\Music\\**Artist**"
            //or 
            //adfzdg\\  (Note this should be adfzdg)...
            //adfzdg\\Music
            //I think this would be a special case where we simply remove the first dir.
            if (dirInfoArray[0].Item1.Name == "\\")
            {
                dirInfoArray = dirInfoArray.Skip(1).ToArray();
            }
            else if (dirInfoArray[0].Item1.Name.EndsWith("\\"))
            {
                dirInfoArray[0] = new Tuple<Directory, bool>(new Directory(dirInfoArray[0].Item1.Name.Substring(0, dirInfoArray[0].Item1.Name.Length - 1), dirInfoArray[0].Item1.Files), dirInfoArray[0].Item2);
            }



            bool emptyRoot = false;
            //if(dirArray[dirArray.Length-1].Name.Contains(dirArray[0].Name))
            if (Helpers.IsChildDirString(dirInfoArray[dirInfoArray.Length - 1].Item1.Name, dirInfoArray[0].Item1.Name, true) || dirInfoArray[dirInfoArray.Length - 1].Item1.Name.Equals(dirInfoArray[0].Item1.Name))
            {
                //normal single tree case..
            }
            else
            {
                //we need to set the first root..
                //GetLongestCommonParent(dirArray[dirArray.Length - 1].Name, dirArray[0].Name);
                string newRootDirName = GetLongestCommonParent(dirInfoArray[dirInfoArray.Length - 1].Item1.Name, dirInfoArray[0].Item1.Name);
                if (newRootDirName == string.Empty)
                {
                    //MainActivity.LogFirebase("Root is the empty string: " + username); //this is fine
                    newRootDirName = "";
                    emptyRoot = true;
                }
                //if(newRootDirName.EndsWith("\\"))
                //{
                //    newRootDirName = newRootDirName.Substring(0, newRootDirName.Length-1);
                //    //else our new folder root will be "@@sdfklj\\" rather than "@@sdfklj" causing problems..
                //}
                //the rootname can be "@@sdfklj\\! " if the directories are "@@sdfklj\\! mp3", "@@sdfklj\\! flac"
                if (newRootDirName.LastIndexOf("\\") != -1)
                {
                    newRootDirName = newRootDirName.Substring(0, newRootDirName.LastIndexOf("\\"));
                    //else our new folder root will be "@@sdfklj\\" rather than "@@sdfklj" causing problems..
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
                    //uncomment these lines for the Nicotine way of handling QT trees
                    //if(curNode.Data.Name == "" && curNode?.Parent == null && dInfo.Item1.Name.Contains('\\')) //QT double directory case. 
                    //{   //sometimes the dir is just '//' not sure how that happens since even if you put files into root, it is still proper.
                    //    Directory rootSubDirectory = new Directory(dInfo.Item1.Name.Substring(0, dInfo.Item1.Name.IndexOf('\\')));
                    //    curNode = AddChildNode(curNode, new Tuple<Directory,bool>(rootSubDirectory, dInfo.Item2), filter, wordsToAvoid, wordsToInclude);
                    //}
                    curNode = AddChildNode(curNode, dInfo, filter, wordsToAvoid, wordsToInclude);
                    prevDirName = dInfo.Item1.Name;
                }
                else
                { //go up one OR more than one
                    prevNodeDebug = new TreeNode<Directory>(curNode.Data, dInfo.Item2);
                    curNode = curNode.Parent; //This is not good if the first node is not the root...


                    //if(dInfo==null || dInfo.Item1 == null || curNode == null || curNode.Data == null)
                    //{

                    //}

                    while (!Helpers.IsChildDirString(dInfo.Item1.Name, curNode.Data.Name, curNode?.Parent == null))
                    {
                        if (curNode.Parent == null)
                        {
                            break; //this might be hiding an error
                        }
                        curNode = curNode.Parent; // may have to go up more than one
                    }
                    //uncomment these lines for the Nicotine way of handling QT trees
                    //if (curNode.Data.Name == "" && curNode?.Parent == null && dInfo.Item1.Name.Contains('\\')) //QT double directory case.
                    //{
                    //    Directory rootSubDirectory = new Directory(dInfo.Item1.Name.Substring(0, dInfo.Item1.Name.IndexOf('\\')));
                    //    curNode = AddChildNode(curNode, new Tuple<Directory, bool>(rootSubDirectory, dInfo.Item2), filter, wordsToAvoid, wordsToInclude);
                    //}
                    curNode = AddChildNode(curNode, dInfo, filter, wordsToAvoid, wordsToInclude);
                    prevDirName = dInfo.Item1.Name;
                }
            }

            if (filter)
            {
                //unhide any ones with valid Files (by default they are all hidden).
                IterateTreeAndUnsetFilteredForValid(rootNode);
            }

            //logging code for unit tests / diagnostic..
            //var root2 = DocumentFile.FromTreeUri(SoulSeekState.MainActivityRef, Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri));
            //DocumentFile exists2 = root.FindFile(username + "_parsed_answer");
            //if (exists2 == null || !exists2.Exists())
            //{
            //    DocumentFile f = root2.CreateFile(@"custom\binary", username + "_parsed_answer");

            //    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(f.Uri);
            //    //Java.IO.File musicFile = new Java.IO.File(filePath);
            //    //FileOutputStream stream = new FileOutputStream(mFile);
            //    using (System.IO.MemoryStream userListStream = new System.IO.MemoryStream())
            //    {
            //        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //        formatter.Serialize(userListStream, rootNode);

            //        //write to binary..

            //        stream.Write(userListStream.ToArray());
            //        stream.Close();
            //    }
            //}
            //end logging code for unit tests / diagnostic..

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


                //{
                //    return true;
                //}
                //else
                //{
                //    //special case. since primary: is a parent of primary:Music
                //    if(possibleParent.LastIndexOf("\\")==-1 && possibleParent.IndexOf(':')==(possibleParent.Length-1))
                //    {
                //        return true;
                //    }
                //    return false;
                //}
            }
        }
    }

    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        public T Data;
        public bool IsFilteredOut = false;
        public bool IsLocked = false;
        public TreeNode<T> Parent;
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
            return null;
        }
    }
}
