using NUnit.Framework;
using Soulseek;
using Common;
using Common.Browse;
using Seeker;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    public class BrowseStateTest
    {
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

        // ===== HasResponse =====

        [Test]
        public void HasResponse_NoUsername_ReturnsFalse()
        {
            var state = new BrowseState();
            Assert.IsFalse(state.HasResponse());
        }

        [Test]
        public void HasResponse_EmptyUsername_ReturnsFalse()
        {
            var state = new BrowseState();
            state.CurrentUsername = "";
            Assert.IsFalse(state.HasResponse());
        }

        [Test]
        public void HasResponse_WithUsername_ReturnsTrue()
        {
            var state = new BrowseState();
            state.CurrentUsername = "testuser";
            Assert.IsTrue(state.HasResponse());
        }

        // ===== SetBrowseResponse =====

        [Test]
        public void SetBrowseResponse_Basic_PopulatesDataItems()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();

            var error = state.SetBrowseResponse("testuser", tree, null, null);

            Assert.AreEqual(BrowseStateError.None, error);
            Assert.AreEqual("testuser", state.CurrentUsername);
            Assert.AreEqual(2, state.DataItems.Count); // Music + Documents
        }

        [Test]
        public void SetBrowseResponse_EmptyStartingLocation_PopulatesFromRoot()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();

            var error = state.SetBrowseResponse("testuser", tree, null, "");

            Assert.AreEqual(BrowseStateError.None, error);
            Assert.AreEqual(2, state.DataItems.Count); // Music + Documents
        }

        [Test]
        public void SetBrowseResponse_ValidStartingLocation_PopulatesFromThere()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();

            var error = state.SetBrowseResponse("testuser", tree, null, @"root\Music");

            Assert.AreEqual(BrowseStateError.None, error);
            // Music has 1 child dir (SubFolder) + 2 files
            Assert.AreEqual(3, state.DataItems.Count);
        }

        [Test]
        public void SetBrowseResponse_InvalidStartingLocation_FallsBackToRoot()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();

            var error = state.SetBrowseResponse("testuser", tree, null, "nonexistent");

            Assert.AreEqual(BrowseStateError.CannotFindStartDirectory, error);
            Assert.AreEqual(2, state.DataItems.Count); // falls back to root
        }

        [Test]
        public void SetBrowseResponse_ClearsOldData()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();

            state.SetBrowseResponse("user1", tree, null, null);
            Assert.AreEqual(2, state.DataItems.Count);

            // Second call should clear old data
            var tree2 = new TreeNode<Directory>(new Directory("empty"), false);
            state.SetBrowseResponse("user2", tree2, null, null);
            Assert.AreEqual("user2", state.CurrentUsername);
            Assert.AreEqual(0, state.DataItems.Count);
        }

        [Test]
        public void SetBrowseResponse_ClearsFilter()
        {
            var state = new BrowseState();
            state.Filter.Set("test filter");
            Assert.IsTrue(state.Filter.IsFiltered);

            var tree = BuildTestTree();
            state.SetBrowseResponse("testuser", tree, null, null);

            Assert.IsFalse(state.Filter.IsFiltered);
        }

        // ===== ClearFilter =====

        [Test]
        public void ClearFilter_ResetsFilterState()
        {
            var state = new BrowseState();
            state.Filter.Set("test");
            state.CachedFilteredDataItems = new System.Tuple<string, List<DataItem>>("test", new List<DataItem>());

            state.ClearFilter();

            Assert.IsFalse(state.Filter.IsFiltered);
            Assert.IsNull(state.CachedFilteredDataItems);
            Assert.AreEqual(0, state.FilteredDataItems.Count);
        }

        // ===== UpdateFilteredResponses =====

        [Test]
        public void UpdateFilteredResponses_FiltersDataItems()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();
            state.SetBrowseResponse("testuser", tree, null, @"root\Music");
            // DataItems: SubFolder (dir), song1.mp3, song2.mp3

            state.Filter.Set("song1");
            state.UpdateFilteredResponses();

            Assert.IsTrue(state.FilteredDataItems.Count > 0);
            Assert.IsTrue(state.FilteredDataItems.Any(d => d.File != null && d.File.Filename == "song1.mp3"));
        }

        [Test]
        public void UpdateFilteredResponses_SetsCachedFilteredDataItems()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();
            state.SetBrowseResponse("testuser", tree, null, @"root\Music");

            state.Filter.Set("song");
            state.UpdateFilteredResponses();

            Assert.IsNotNull(state.CachedFilteredDataItems);
            Assert.AreEqual("song", state.CachedFilteredDataItems.Item1);
        }

        [Test]
        public void UpdateFilteredResponses_MoreRestrictiveSearch_UsesCachedResults()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();
            state.SetBrowseResponse("testuser", tree, null, @"root\Music");

            // First filter
            state.Filter.Set("song");
            state.UpdateFilteredResponses();
            int firstCount = state.FilteredDataItems.Count;

            // More restrictive filter (adds chars)
            state.Filter.Set("song1");
            state.UpdateFilteredResponses();

            // Should still work and possibly have fewer results
            Assert.IsTrue(state.FilteredDataItems.Count <= firstCount);
        }

        // ===== GoUpDirectory =====

        [Test]
        public void GoUpDirectory_EmptyDataItems_ReturnsFalse()
        {
            var state = new BrowseState();
            var calls = new List<string>();
            bool result = state.GoUpDirectory((f, d, b1, b2) => calls.Add("called"), 0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, calls.Count);
        }

        [Test]
        public void GoUpDirectory_FromFiles_GoesToParent()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();
            var musicNode = tree.Children.First();
            var subFolderNode = musicNode.Children.First();

            // Navigate to SubFolder's contents (files)
            state.SetBrowseResponse("testuser", tree, null, @"root\Music\SubFolder");
            Assert.IsTrue(state.DataItems.Any(d => d.File != null)); // should have files

            var adapterCalls = new List<(bool filtered, int count)>();
            bool result = state.GoUpDirectory((filtered, items, b1, b2) =>
            {
                adapterCalls.Add((filtered, items.Count));
            }, 0);

            Assert.IsTrue(result);
            // After going up from SubFolder files, should be at Music level
            Assert.IsTrue(state.DataItems.Count > 0);
        }

        [Test]
        public void GoUpDirectory_FromDirs_GoesToGrandparent()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();

            // Navigate to Music level (which shows SubFolder dir + files)
            state.SetBrowseResponse("testuser", tree, null, @"root\Music");
            // DataItems[0] is SubFolder directory

            var adapterCalls = 0;
            bool result = state.GoUpDirectory((filtered, items, b1, b2) =>
            {
                adapterCalls++;
            }, 0);

            Assert.IsTrue(result);
            // From Music's contents (SubFolder is a dir), going up via Parent.Parent
            // should land at root level
        }

        [Test]
        public void GoUpDirectory_AtRoot_ReturnsFalse()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();

            // At root level, DataItems are Music and Documents (dirs)
            state.SetBrowseResponse("testuser", tree, null, null);

            bool result = state.GoUpDirectory((f, d, b1, b2) => { }, 0);

            // Root dirs: Parent is root, Parent.Parent is null -> should return false
            Assert.IsFalse(result);
        }

        [Test]
        public void GoUpDirectory_CallsSetBrowseAdaptersCorrectly_NotFiltered()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();
            state.SetBrowseResponse("testuser", tree, null, @"root\Music\SubFolder");

            var adapterCalls = 0;
            bool result = state.GoUpDirectory((filtered, items, b1, b2) =>
            {
                adapterCalls++;
                Assert.IsFalse(filtered);
            }, 0);

            Assert.IsTrue(result);
            // When not filtered, setBrowseAdapters should be called exactly once
            // (once for the final state update)
            Assert.AreEqual(1, adapterCalls);
        }

        [Test]
        public void GoUpDirectory_CallsSetBrowseAdaptersCorrectly_Filtered()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();
            state.SetBrowseResponse("testuser", tree, null, @"root\Music\SubFolder");
            state.Filter.Set("song");
            state.UpdateFilteredResponses();

            var adapterCalls = 0;
            bool result = state.GoUpDirectory((filtered, items, b1, b2) =>
            {
                adapterCalls++;
            }, 0);

            Assert.IsTrue(result);
            // When filtered, setBrowseAdapters should be called exactly once
            Assert.AreEqual(1, adapterCalls);
        }

        [Test]
        public void GoUpDirectory_WithAdditionalLevels()
        {
            // Build a deeper tree:  root -> A -> B -> C (with a file)
            var rootDir = new Directory("");
            var root = new TreeNode<Directory>(rootDir, false);
            var dirA = new Directory(@"root\A");
            var nodeA = root.AddChild(dirA, false);
            var dirB = new Directory(@"root\A\B");
            var nodeB = nodeA.AddChild(dirB, false);
            var fileInB = new File(1, "test.mp3", 100, "mp3");
            var dirC = new Directory(@"root\A\B\C", new List<File> { fileInB });
            nodeB.AddChild(dirC, false);

            var state = new BrowseState();
            state.SetBrowseResponse("testuser", root, null, @"root\A\B");
            // DataItems: C (dir)
            // Going up from dir -> Parent.Parent = A, then additionalLevels=1 -> root

            bool result = state.GoUpDirectory((f, d, b1, b2) => { }, 1);
            Assert.IsTrue(result);
        }

        [Test]
        public void GoUpDirectory_ClearsCachedFilteredDataItems()
        {
            var state = new BrowseState();
            var tree = BuildTestTree();
            state.SetBrowseResponse("testuser", tree, null, @"root\Music\SubFolder");
            state.CachedFilteredDataItems = new System.Tuple<string, List<DataItem>>("cached", new List<DataItem>());

            state.GoUpDirectory((f, d, b1, b2) => { }, 0);

            Assert.IsNull(state.CachedFilteredDataItems);
        }
    }
}
