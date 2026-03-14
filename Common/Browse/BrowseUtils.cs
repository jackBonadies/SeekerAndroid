using Seeker;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Browse
{
    public class BrowseUtils
    {
        public static FolderSummary GetFolderSummary(DataItem di)
        {
            FolderSummary folderSummary = new FolderSummary();
            SumFiles(folderSummary, di);
            return folderSummary;
        }

        public static FolderSummary GetFolderSummary(List<DataItem> di)
        {
            FolderSummary folderSummary = new FolderSummary();
            foreach (var childNode in di)
            {
                SumFiles(folderSummary, childNode);
                if (childNode.IsDirectory())
                {
                    folderSummary.NumSubFolders++;
                }
            }
            return folderSummary;
        }

        private static void SumFiles(FolderSummary fileSummary, DataItem d)
        {
            if (d.File != null)
            {
                fileSummary.AddFile(d.File);
                return;
            }
            else
            {
                foreach (Soulseek.File slskFile in d.Directory.Files) //files in dir
                {
                    fileSummary.AddFile(slskFile);
                }
                foreach (var childNode in d.Node.Children) //dirs in dir
                {
                    fileSummary.NumSubFolders++;
                    SumFiles(fileSummary, new DataItem(childNode.Data, childNode));
                }
                return;
            }
        }

        /// <summary>
        /// Flattens the tree
        /// </summary>
        /// <param name="di"></param>
        /// <returns></returns>
        public static List<FullFileInfo> GetRecursiveFullFileInfo(DataItem di)
        {
            List<FullFileInfo> listOfFiles = new List<FullFileInfo>();
            AddFiles(listOfFiles, di);
            return listOfFiles;
        }

        public static List<FullFileInfo> GetRecursiveFullFileInfo(List<DataItem> di)
        {
            List<FullFileInfo> listOfFiles = new List<FullFileInfo>();
            foreach (var childNode in di)
            {
                AddFiles(listOfFiles, childNode);
            }
            return listOfFiles;
        }

        private static void AddFiles(List<FullFileInfo> fullFileList, DataItem d)
        {
            if (d.File != null)
            {
                FullFileInfo f = new FullFileInfo();
                f.FullFileName = d.Node.Data.Name + @"\" + d.File.Filename;
                f.Size = d.File.Size;
                f.wasFilenameLatin1Decoded = d.File.IsLatin1Decoded;
                f.wasFolderLatin1Decoded = d.Node.Data.DecodedViaLatin1;
                fullFileList.Add(f);
                return;
            }
            else
            {
                foreach (Soulseek.File slskFile in d.Directory.Files) //files in dir
                {
                    FullFileInfo f = new FullFileInfo();
                    f.FullFileName = d.Node.Data.Name + @"\" + slskFile.Filename;
                    f.Size = slskFile.Size;
                    f.wasFilenameLatin1Decoded = slskFile.IsLatin1Decoded;
                    f.wasFolderLatin1Decoded = d.Node.Data.DecodedViaLatin1;
                    fullFileList.Add(f);
                }
                foreach (var childNode in d.Node.Children) //dirs in dir
                {
                    AddFiles(fullFileList, new DataItem(childNode.Data, childNode));
                }
                return;
            }
        }

        /// <summary>
        /// basically if the current filter is more restrictive, then we dont have to filter everything again, we can just filter the existing filtered data more.
        /// </summary>
        /// <param name="currentSearch"></param>
        /// <param name="previousSearch"></param>
        /// <returns></returns>
        public static bool IsCurrentSearchMoreRestrictive(string currentSearch, string previousSearch)
        {
            var currentWords = currentSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var previousWords = previousSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentExcludeWords = currentWords.Where(s => s.StartsWith("-") && s.Length != 1);
            var previousExcludeWords = previousWords.Where(s => s.StartsWith("-") && s.Length != 1);
            var currentIncludeWords = currentWords.Where(s => !s.StartsWith("-"));
            var previousIncludeWords = previousWords.Where(s => !s.StartsWith("-"));
            string currentIncludeString = string.Join(' ', currentIncludeWords).Trim();
            string previousIncludeString = string.Join(' ', previousIncludeWords).Trim();

            if (currentExcludeWords.Count() < previousExcludeWords.Count())
            {
                //current is not necessarily more restrictive as it excludes less.
                return false;
            }

            //and for each previous exlusion, the current exlusions are just as bad.
            for (int i = 0; i < previousExcludeWords.Count(); i++)
            {
                if (!previousExcludeWords.ElementAt(i).Contains(currentExcludeWords.ElementAt(i)))
                {
                    return false;
                }
            }

            return currentIncludeString.Contains(previousIncludeString);
        }

        public static FullFileInfo ToFullFileInfo(DataItem d)
        {
            return new FullFileInfo
            {
                FullFileName = d.Node.Data.Name + @"\" + d.File.Filename,
                Size = d.File.Size,
                wasFilenameLatin1Decoded = d.File.IsLatin1Decoded,
                wasFolderLatin1Decoded = d.Node.Data.DecodedViaLatin1
            };
        }

        public static FullFileInfo[] GetFullFileInfos(IEnumerable<Soulseek.File> files)
        {
            return files.Select(it=>new FullFileInfo() { Size = it.Size, FullFileName = it.Filename, Depth = 1, wasFilenameLatin1Decoded = it.IsLatin1Decoded, wasFolderLatin1Decoded = it.IsDirectoryLatin1Decoded }).ToArray();
        }
        private static bool MatchesCriteriaFull(DataItem di, TextFilter filter)
        {
            string fullyQualifiedName = string.Empty;
            if (di.File != null)
            {
                //we are looking at files here...
                fullyQualifiedName = di.Node.Data.Name + di.File.Filename;
            }
            else
            {
                fullyQualifiedName = di.Node.Data.Name;
                //maybe here we should also do children...
            }


            foreach (string avoid in filter.WordsToAvoid)
            {
                if (fullyQualifiedName.Contains(avoid, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                    //badTerm = true;
                }
            }
            bool includesAll = true;
            foreach (string include in filter.WordsToInclude)
            {
                if (!fullyQualifiedName.Contains(include, StringComparison.OrdinalIgnoreCase))
                {
                    includesAll = false;
                }
            }
            if (includesAll)
            {
                return true;
            }
            else
            {
                //search children for a match.. if there are children.. else we are done..
                if (di.Node.Children.Count == 0 && (di.Directory == null || di.Directory.Files.Count == 0))
                {
                    return false;
                }
                else if (di.File != null)
                {
                    //then we are at the end
                    return false;
                }
                else
                {
                    if (di.Node.Children.Count != 0)
                    {
                        foreach (TreeNode<Directory> child in di.Node.Children)
                        {
                            if (MatchesCriteriaFull(new DataItem(child.Data, child), filter))
                            {
                                return true;
                            }
                        }
                    }
                    if (di.File == null && di.Directory != null && di.Directory.Files.Count != 0)
                    {
                        foreach (File f in di.Directory.Files)
                        {
                            if (MatchesCriteriaFull(new DataItem(f, di.Node), filter))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
        }

        public static List<DataItem> FilterBrowseList(List<DataItem> unfiltered, TextFilter filter)
        {
            List<DataItem> filtered = new List<DataItem>();
            foreach (DataItem di in unfiltered)
            {
                if (MatchesCriteriaFull(di, filter)) //change back to shallow...
                {
                    filtered.Add(di);
                }
            }
            return filtered;
        }
        public static List<PathItem> GetPathItems(List<DataItem> nonFilteredDataItemsForListView)
        {
            if (nonFilteredDataItemsForListView.Count == 0)
            {
                return new List<PathItem>();
            }
            List<PathItem> pathItemsList = new List<PathItem>();
            if (nonFilteredDataItemsForListView[0].IsDirectory())
            {
                GetPathItemsInternal(pathItemsList, nonFilteredDataItemsForListView[0].Node.Parent, true);
            }
            else
            {
                GetPathItemsInternal(pathItemsList, nonFilteredDataItemsForListView[0].Node, true);
            }
            pathItemsList.Reverse();
            FixNullRootDisplayName(pathItemsList);
            return pathItemsList;
        }

        private static void FixNullRootDisplayName(List<PathItem> pathItemsList)
        {
            if (pathItemsList.Count > 0 && pathItemsList[0].DisplayName == string.Empty)
            {
                pathItemsList[0].DisplayName = "root";
            }
        }

        private static void GetPathItemsInternal(List<PathItem> pathItems, TreeNode<Directory> treeNode, bool lastChild)
        {
            string displayName = SimpleHelpers.GetFileNameFromFile(treeNode.Data.Name);
            pathItems.Add(new PathItem(displayName, lastChild));
            if (treeNode.Parent == null)
            {
                return;
            }
            else
            {
                GetPathItemsInternal(pathItems, treeNode.Parent, false);
            }
        }
        public static void SetDepthTags(DataItem d, List<FullFileInfo> recursiveFileInfo)
        {
            int lowestLevel = GetLevel(d.Node.Data.Name);
            foreach (FullFileInfo fullFileInfo in recursiveFileInfo)
            {
                int level = GetLevel(fullFileInfo.FullFileName);
                int depth = level - lowestLevel + 1;
#if DEBUG
                if (depth == 0)
                {
                    throw new Exception("depth is 0");
                }
#endif
                fullFileInfo.Depth = depth;
            }

        }

        private static int GetLevel(string fileName)
        {
            int count = 0;
            foreach (char c in fileName)
            {
                if (c == '\\')
                {
                    count++;
                }
            }
            return count;
        }

        public record struct BrowseStats(double NumFolders, double NumFiles);

        public static BrowseStats GetBrowseStats(BrowseResponse browseResponse)
        {
            if (PreferencesState.HideLockedResultsInBrowse)
            {
                return new BrowseStats(browseResponse.DirectoryCount, browseResponse.Directories.Sum(it => it.FileCount));
            } 
            else
            {
                return new BrowseStats(browseResponse.DirectoryCount + browseResponse.LockedDirectoryCount, 
                    browseResponse.Directories.Sum(it => it.FileCount) + browseResponse.LockedDirectories.Sum(it => it.FileCount));
            }
        }

        public static List<DataItem> GetDataItemsForNode(TreeNode<Directory> node)
        {
            var items = new List<DataItem>();
            foreach (TreeNode<Directory> child in node.Children)
            {
                items.Add(new DataItem(child.Data, child));
            }
            if (node.Data != null && node.Data.FileCount != 0)
            {
                foreach (Soulseek.File f in node.Data.OrderedFiles)
                {
                    items.Add(new DataItem(f, node));
                }
            }
            return items;
        }

        public static (List<FullFileInfo> topLevel, List<FullFileInfo> recursive, bool containsSubDirs) BuildDownloadFileInfos(List<DataItem> sourceList)
        {
            bool containsSubDirs = false;
            var topLevel = new List<FullFileInfo>();
            foreach (DataItem d in sourceList)
            {
                if (d.IsDirectory())
                {
                    containsSubDirs = true;
                }
                else
                {
                    topLevel.Add(ToFullFileInfo(d));
                }
            }
            var recursive = containsSubDirs ? GetRecursiveFullFileInfo(sourceList) : new List<FullFileInfo>();
            return (topLevel, recursive, containsSubDirs);
        }

        public static TreeNode<Directory> GetNodeByName(TreeNode<Directory> rootTree, string nameToFindDirName)
        {
            if (rootTree.Data.Name == nameToFindDirName)
            {
                return rootTree;
            }
            else
            {
                foreach (TreeNode<Directory> d in rootTree.Children)
                {
                    var node = GetNodeByName(d, nameToFindDirName);
                    if (node != null)
                    {
                        return node;
                    }
                }
            }
            return null;
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

        public static TreeNode<Directory> CreateTreeFromFlatList(BrowseResponse b, bool filter, List<string> wordsToAvoid, List<string> wordsToInclude, string username, bool hideLocked)
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
}
