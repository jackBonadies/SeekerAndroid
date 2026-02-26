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
        /// <remarks>
        /// examples: 
        /// bool yes = IsCurrentSearchMoreRestrictive("hello how are you -ex","hello how are yo -excludeTerms");
        /// bool yes = IsCurrentSearchMoreRestrictive("hello how are you -excludeTerms", "hello how are yo -excludeTerms");
        /// bool no = IsCurrentSearchMoreRestrictive("hello how are you -excludeTerms", "hello how are yo -excludeTerms -a");
        /// bool no = IsCurrentSearchMoreRestrictive("hello how are you -excludeTerms -b -c", "hello how are yo -excludeTerms -a");
        /// bool yes = IsCurrentSearchMoreRestrictive("hello how are you -excludeTerms -a -b -c", "hello how are yo -excludeTerms -a");
        /// bool yes = IsCurrentSearchMoreRestrictive("hello how are you -excludeTerms -", "hello how are yo -excludeTerms");
        /// bool yes = IsCurrentSearchMoreRestrictive("hello how are you -excludeTerms -a", "hello how are yo -excludeTerms -");
        /// bool no = IsCurrentSearchMoreRestrictive("hello how are you -excludeTerms -aa", "hello how are yo -excludeTerms -a");
        /// bool no = IsCurrentSearchMoreRestrictive("hell", "hello");
        /// bool yes = IsCurrentSearchMoreRestrictive("hello", "hell");
        /// </remarks>
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
    }
}
