using NUnit.Framework;
using Soulseek;
using Common;
using Common.Browse;
using Seeker;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    public class BrowseUtilsTest
    {
        // Helper: build a simple tree
        //   root ("")
        //   ├── Music (dir, files: song1.mp3 100b, song2.mp3 200b)
        //   │   └── SubFolder (dir, files: song3.mp3 300b)
        //   └── Documents (dir, files: readme.txt 50b)
        private static TreeNode<Directory> BuildTestTree()
        {
            var rootDir = new Directory("");
            var root = new TreeNode<Directory>(rootDir, false);

            var musicFiles = new List<File>
            {
                new File(1, "song1.mp3", 100, "mp3"),
                new File(2, "song2.mp3", 200, "mp3"),
            };
            var musicDir = new Directory(@"root\Music", musicFiles);
            var musicNode = root.AddChild(musicDir, false);

            var subFiles = new List<File>
            {
                new File(3, "song3.mp3", 300, "mp3"),
            };
            var subDir = new Directory(@"root\Music\SubFolder", subFiles);
            musicNode.AddChild(subDir, false);

            var docFiles = new List<File>
            {
                new File(4, "readme.txt", 50, "txt"),
            };
            var docDir = new Directory(@"root\Documents", docFiles);
            root.AddChild(docDir, false);

            return root;
        }

        // ===== GetDataItemsForNode =====

        [Test]
        public void GetDataItemsForNode_ReturnsChildDirsAndFiles()
        {
            var root = BuildTestTree();
            var items = BrowseUtils.GetDataItemsForNode(root);

            // root has 2 child dirs (Music, Documents), no direct files
            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items[0].IsDirectory());
            Assert.IsTrue(items[1].IsDirectory());
        }

        [Test]
        public void GetDataItemsForNode_LeafDirReturnsFiles()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First(); // Music
            var items = BrowseUtils.GetDataItemsForNode(musicNode);

            // Music has 1 child dir (SubFolder) + 2 files (song1, song2)
            Assert.AreEqual(3, items.Count);
            Assert.IsTrue(items[0].IsDirectory()); // SubFolder
            Assert.IsFalse(items[1].IsDirectory()); // song1.mp3
            Assert.IsFalse(items[2].IsDirectory()); // song2.mp3
        }

        [Test]
        public void GetDataItemsForNode_EmptyDir()
        {
            var emptyDir = new Directory("empty");
            var emptyNode = new TreeNode<Directory>(emptyDir, false);
            var items = BrowseUtils.GetDataItemsForNode(emptyNode);
            Assert.AreEqual(0, items.Count);
        }

        // ===== GetNodeByName =====

        [Test]
        public void GetNodeByName_FindsRoot()
        {
            var root = BuildTestTree();
            var found = BrowseUtils.GetNodeByName(root, "");
            Assert.IsNotNull(found);
            Assert.AreSame(root, found);
        }

        [Test]
        public void GetNodeByName_FindsNestedNode()
        {
            var root = BuildTestTree();
            var found = BrowseUtils.GetNodeByName(root, @"root\Music\SubFolder");
            Assert.IsNotNull(found);
            Assert.AreEqual(@"root\Music\SubFolder", found.Data.Name);
        }

        [Test]
        public void GetNodeByName_ReturnsNullIfNotFound()
        {
            var root = BuildTestTree();
            var found = BrowseUtils.GetNodeByName(root, "nonexistent");
            Assert.IsNull(found);
        }

        // ===== BuildDownloadFileInfos =====

        [Test]
        public void BuildDownloadFileInfos_FilesOnly_NoSubDirs()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            var subFolderNode = musicNode.Children.First();
            // SubFolder has only files
            var items = BrowseUtils.GetDataItemsForNode(subFolderNode);

            var (topLevel, recursive, containsSubDirs) = BrowseUtils.BuildDownloadFileInfos(items);

            Assert.IsFalse(containsSubDirs);
            Assert.AreEqual(1, topLevel.Count);
            Assert.AreEqual(0, recursive.Count); // no subdirs means empty recursive list
        }

        [Test]
        public void BuildDownloadFileInfos_MixedDirsAndFiles()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            // Music has SubFolder (dir) + song1 + song2 (files)
            var items = BrowseUtils.GetDataItemsForNode(musicNode);

            var (topLevel, recursive, containsSubDirs) = BrowseUtils.BuildDownloadFileInfos(items);

            Assert.IsTrue(containsSubDirs);
            Assert.AreEqual(2, topLevel.Count); // song1, song2
            Assert.IsTrue(recursive.Count >= 3); // song1, song2, song3 (recursive)
        }

        [Test]
        public void BuildDownloadFileInfos_EmptyList()
        {
            var (topLevel, recursive, containsSubDirs) = BrowseUtils.BuildDownloadFileInfos(new List<DataItem>());
            Assert.IsFalse(containsSubDirs);
            Assert.AreEqual(0, topLevel.Count);
            Assert.AreEqual(0, recursive.Count);
        }

        // ===== ToFullFileInfo =====

        [Test]
        public void ToFullFileInfo_CorrectFields()
        {
            var dir = new Directory(@"root\Music", new List<File>());
            var node = new TreeNode<Directory>(dir, false);
            var file = new File(1, "song.mp3", 999, "mp3");
            var dataItem = new DataItem(file, node);

            var result = BrowseUtils.ToFullFileInfo(dataItem);

            Assert.AreEqual(@"root\Music\song.mp3", result.FullFileName);
            Assert.AreEqual(999, result.Size);
        }

        // ===== IsCurrentSearchMoreRestrictive =====
        // Based on the documented examples in the source

        [Test]
        public void IsMoreRestrictive_AddingIncludeWord_IsMoreRestrictive()
        {
            Assert.IsTrue(BrowseUtils.IsCurrentSearchMoreRestrictive("hello how are you", "hello how are yo"));
        }

        [Test]
        public void IsMoreRestrictive_RemovingIncludeChars_IsNotMoreRestrictive()
        {
            Assert.IsFalse(BrowseUtils.IsCurrentSearchMoreRestrictive("hell", "hello"));
        }

        [Test]
        public void IsMoreRestrictive_AddingExcludeTerm_IsMoreRestrictive()
        {
            Assert.IsTrue(BrowseUtils.IsCurrentSearchMoreRestrictive("hello -excludeTerms -a", "hello -excludeTerms"));
        }

        [Test]
        public void IsMoreRestrictive_FewerExcludeTerms_IsNotMoreRestrictive()
        {
            Assert.IsFalse(BrowseUtils.IsCurrentSearchMoreRestrictive("hello -excludeTerms", "hello -excludeTerms -a"));
        }

        [Test]
        public void IsMoreRestrictive_DifferentExcludeTerms_IsNotMoreRestrictive()
        {
            Assert.IsFalse(BrowseUtils.IsCurrentSearchMoreRestrictive("hello -excludeTerms -b -c", "hello -excludeTerms -a"));
        }

        [Test]
        public void IsMoreRestrictive_SupersetExcludeTerms_IsMoreRestrictive()
        {
            Assert.IsTrue(BrowseUtils.IsCurrentSearchMoreRestrictive("hello -excludeTerms -a -b -c", "hello -excludeTerms -a"));
        }

        [Test]
        public void IsMoreRestrictive_DashOnly_IsMoreRestrictive()
        {
            // "-" alone has length 1, so it's not treated as an exclude word
            Assert.IsTrue(BrowseUtils.IsCurrentSearchMoreRestrictive("hello -excludeTerms -", "hello -excludeTerms"));
        }

        [Test]
        public void IsMoreRestrictive_ExcludeTermNotContaining_IsNotMoreRestrictive()
        {
            // -aa does not "contain" -a as a prefix match, it's a different term
            Assert.IsFalse(BrowseUtils.IsCurrentSearchMoreRestrictive("hello -aa", "hello -a"));
        }

        // ===== FilterBrowseList =====

        [Test]
        public void FilterBrowseList_IncludeFilter()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            var items = BrowseUtils.GetDataItemsForNode(musicNode);
            // items: SubFolder (dir), song1.mp3, song2.mp3

            var filter = new TextFilter();
            filter.Set("song1");

            var filtered = BrowseUtils.FilterBrowseList(items, filter);
            // Should match song1.mp3 file
            Assert.IsTrue(filtered.Any(d => d.File != null && d.File.Filename == "song1.mp3"));
            Assert.IsFalse(filtered.Any(d => d.File != null && d.File.Filename == "song2.mp3"));
        }

        [Test]
        public void FilterBrowseList_ExcludeFilter()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            var items = BrowseUtils.GetDataItemsForNode(musicNode);

            var filter = new TextFilter();
            filter.Set("-song1");

            var filtered = BrowseUtils.FilterBrowseList(items, filter);
            Assert.IsFalse(filtered.Any(d => d.File != null && d.File.Filename == "song1.mp3"));
        }

        [Test]
        public void FilterBrowseList_EmptyFilter_ReturnsAll()
        {
            var root = BuildTestTree();
            var items = BrowseUtils.GetDataItemsForNode(root);

            var filter = new TextFilter();
            // filter not set, so WordsToInclude and WordsToAvoid are empty

            var filtered = BrowseUtils.FilterBrowseList(items, filter);
            Assert.AreEqual(items.Count, filtered.Count);
        }

        [Test]
        public void FilterBrowseList_DirectoryMatchesViaChildren()
        {
            var root = BuildTestTree();
            // root has Music and Documents as children
            var items = BrowseUtils.GetDataItemsForNode(root);

            var filter = new TextFilter();
            filter.Set("song3");

            // Music dir should match because its child SubFolder contains song3.mp3
            var filtered = BrowseUtils.FilterBrowseList(items, filter);
            Assert.IsTrue(filtered.Any(d => d.IsDirectory() && d.Directory.Name.Contains("Music")));
        }

        // ===== GetRecursiveFullFileInfo =====

        [Test]
        public void GetRecursiveFullFileInfo_FlattensTree()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            var items = BrowseUtils.GetDataItemsForNode(musicNode);
            // SubFolder (dir with song3), song1, song2

            var recursive = BrowseUtils.GetRecursiveFullFileInfo(items);

            // Should get song1, song2, song3
            Assert.AreEqual(3, recursive.Count);
        }

        [Test]
        public void GetRecursiveFullFileInfo_SingleFile()
        {
            var dir = new Directory(@"root\Music", new List<File>());
            var node = new TreeNode<Directory>(dir, false);
            var file = new File(1, "test.mp3", 100, "mp3");
            var dataItem = new DataItem(file, node);

            var recursive = BrowseUtils.GetRecursiveFullFileInfo(dataItem);
            Assert.AreEqual(1, recursive.Count);
            Assert.AreEqual(@"root\Music\test.mp3", recursive[0].FullFileName);
        }

        // ===== GetFolderSummary =====

        [Test]
        public void GetFolderSummary_SingleDir_CountsAllRecursiveFiles()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            var musicDataItem = new DataItem(musicNode.Data, musicNode);

            var summary = BrowseUtils.GetFolderSummary(musicDataItem);

            // Music has 2 direct files + SubFolder with 1 file = 3 total
            Assert.AreEqual(3, summary.NumFiles);
            Assert.AreEqual(1, summary.NumSubFolders);
            Assert.AreEqual(600, summary.SizeBytes); // 100 + 200 + 300
        }

        [Test]
        public void GetFolderSummary_ListOverload()
        {
            var root = BuildTestTree();
            var items = BrowseUtils.GetDataItemsForNode(root);

            var summary = BrowseUtils.GetFolderSummary(items);

            // Music (2 files + SubFolder with 1 file) + Documents (1 file) = 4 total files
            Assert.AreEqual(4, summary.NumFiles);
            Assert.AreEqual(3, summary.NumSubFolders);
            Assert.AreEqual(650, summary.SizeBytes); // 100+200+300+50
        }

        // ===== GetPathItems =====

        [Test]
        public void GetPathItems_FromDirItems_ReturnsPath()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            var items = BrowseUtils.GetDataItemsForNode(musicNode);
            // items[0] is SubFolder dir, so path should go from root -> Music

            var pathItems = BrowseUtils.GetPathItems(items);

            Assert.IsTrue(pathItems.Count >= 1);
            // First item should be root
            Assert.AreEqual("root", pathItems[0].DisplayName);
            // Last item should be marked as last node
            Assert.IsTrue(pathItems[pathItems.Count - 1].IsLastNode);
        }

        [Test]
        public void GetPathItems_FromFileItems_ReturnsPath()
        {
            var root = BuildTestTree();
            var musicNode = root.Children.First();
            var subFolderNode = musicNode.Children.First();
            var items = BrowseUtils.GetDataItemsForNode(subFolderNode);
            // items are files inside SubFolder

            var pathItems = BrowseUtils.GetPathItems(items);

            Assert.IsTrue(pathItems.Count >= 1);
        }

        [Test]
        public void GetPathItems_EmptyList_ReturnsEmpty()
        {
            var pathItems = BrowseUtils.GetPathItems(new List<DataItem>());
            Assert.AreEqual(0, pathItems.Count);
        }

        // ===== GetFullFileInfos =====

        [Test]
        public void GetFullFileInfos_ConvertsFiles()
        {
            var files = new List<File>
            {
                new File(1, "song1.mp3", 100, "mp3"),
                new File(2, "song2.mp3", 200, "mp3"),
            };

            var result = BrowseUtils.GetFullFileInfos(files);

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("song1.mp3", result[0].FullFileName);
            Assert.AreEqual(100, result[0].Size);
            Assert.AreEqual(1, result[0].Depth);
        }
    }
}
