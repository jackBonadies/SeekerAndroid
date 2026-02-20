using NUnit.Framework;
using Seeker;
using Soulseek;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferItemManagerTests
    {
        private TransferItemManager manager;

        [SetUp]
        public void Setup()
        {
            manager = new TransferItemManager(false);
            TransferState.CancellationTokens.Clear();
            TransferState.UsersWhereDownloadFailedDueToOffline.Clear();
        }

        private TransferItem CreateTransferItem(string username, string fullFilename, string folderName = "folder1", long size = 1000)
        {
            return new TransferItem
            {
                Username = username,
                FullFilename = fullFilename,
                FolderName = folderName,
                Filename = fullFilename.Split('\\').Last(),
                Size = size,
            };
        }

        [Test]
        public void Add_AddsToAllTransferItems()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);

            Assert.AreEqual(1, manager.AllTransferItems.Count);
            Assert.AreEqual(ti, manager.AllTransferItems[0]);
        }

        [Test]
        public void Add_CreatesFolderItem()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);

            Assert.AreEqual(1, manager.AllFolderItems.Count);
            Assert.AreEqual("folder1", manager.AllFolderItems[0].FolderName);
            Assert.AreEqual("user1", manager.AllFolderItems[0].Username);
        }

        [Test]
        public void Add_SameFolderGroupsTogether()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            Assert.AreEqual(2, manager.AllTransferItems.Count);
            Assert.AreEqual(1, manager.AllFolderItems.Count);
            Assert.AreEqual(2, manager.AllFolderItems[0].TransferItems.Count);
        }

        [Test]
        public void Add_DifferentFoldersCreateSeparateFolderItems()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3", "folder1");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder2\\file.mp3", "folder2");
            manager.Add(ti1);
            manager.Add(ti2);

            Assert.AreEqual(2, manager.AllFolderItems.Count);
        }

        [Test]
        public void Add_DifferentUsersSameFolderCreateSeparateFolderItems()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            var ti2 = CreateTransferItem("user2", "\\dir\\folder1\\file.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            Assert.AreEqual(2, manager.AllFolderItems.Count);
        }

        [Test]
        public void Remove_RemovesFromAllTransferItems()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);
            manager.Remove(ti);

            Assert.AreEqual(0, manager.AllTransferItems.Count);
        }

        [Test]
        public void Remove_RemovesFolderWhenEmpty()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);
            manager.Remove(ti);

            Assert.AreEqual(0, manager.AllFolderItems.Count);
        }

        [Test]
        public void Remove_KeepsFolderWhenOtherItemsRemain()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);
            manager.Remove(ti1);

            Assert.AreEqual(1, manager.AllTransferItems.Count);
            Assert.AreEqual(1, manager.AllFolderItems.Count);
            Assert.AreEqual(1, manager.AllFolderItems[0].TransferItems.Count);
        }

        [Test]
        public void AddIfNotExistAndReturnTransfer_AddsNewItem()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            var result = manager.AddIfNotExistAndReturnTransfer(ti, out bool exists);

            Assert.IsFalse(exists);
            Assert.AreEqual(ti, result);
            Assert.AreEqual(1, manager.AllTransferItems.Count);
        }

        [Test]
        public void AddIfNotExistAndReturnTransfer_ReturnsExistingItem()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti2.Size = 9999;

            manager.AddIfNotExistAndReturnTransfer(ti1, out _);
            var result = manager.AddIfNotExistAndReturnTransfer(ti2, out bool exists);

            Assert.IsTrue(exists);
            Assert.AreEqual(ti1, result);
            Assert.AreEqual(1, manager.AllTransferItems.Count);
        }

        [Test]
        public void Exists_ReturnsTrueForExistingItem()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);

            Assert.IsTrue(manager.Exists("\\dir\\folder1\\file.mp3", "user1", 1000));
        }

        [Test]
        public void Exists_ReturnsFalseForNonexistentItem()
        {
            Assert.IsFalse(manager.Exists("\\dir\\folder1\\file.mp3", "user1", 1000));
        }

        [Test]
        public void ExistsAndInProcessing_ReturnsTrueWhenInProcessing()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.InProcessing = true;
            manager.Add(ti);

            Assert.IsTrue(manager.ExistsAndInProcessing("\\dir\\folder1\\file.mp3", "user1", 1000));
        }

        [Test]
        public void ExistsAndInProcessing_ReturnsFalseWhenNotInProcessing()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);

            Assert.IsFalse(manager.ExistsAndInProcessing("\\dir\\folder1\\file.mp3", "user1", 1000));
        }

        [Test]
        public void ClearAllComplete_RemovesCompletedItems()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            ti1.Progress = 100;
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            ti2.Progress = 50;
            manager.Add(ti1);
            manager.Add(ti2);

            manager.ClearAllComplete();

            Assert.AreEqual(1, manager.AllTransferItems.Count);
            Assert.AreEqual(ti2, manager.AllTransferItems[0]);
        }

        [Test]
        public void ClearAll_RemovesEverything()
        {
            manager.Add(CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3"));
            manager.Add(CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3"));

            manager.ClearAll();

            Assert.AreEqual(0, manager.AllTransferItems.Count);
            Assert.AreEqual(0, manager.AllFolderItems.Count);
        }

        [Test]
        public void IsEmpty_ReturnsTrueWhenEmpty()
        {
            Assert.IsTrue(manager.IsEmpty());
        }

        [Test]
        public void IsEmpty_ReturnsFalseWhenNotEmpty()
        {
            manager.Add(CreateTransferItem("user1", "\\dir\\folder1\\file.mp3"));
            Assert.IsFalse(manager.IsEmpty());
        }

        [Test]
        public void IsFolderNowComplete_ReturnsTrueWhenAllSucceeded()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            ti1.State = TransferStates.Completed | TransferStates.Succeeded;
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            ti2.State = TransferStates.Completed | TransferStates.Succeeded;
            manager.Add(ti1);
            manager.Add(ti2);

            Assert.IsTrue(manager.IsFolderNowComplete(ti1));
        }

        [Test]
        public void IsFolderNowComplete_ReturnsFalseWhenNotAllSucceeded()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            ti1.State = TransferStates.Completed | TransferStates.Succeeded;
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            ti2.State = TransferStates.Queued;
            manager.Add(ti1);
            manager.Add(ti2);

            Assert.IsFalse(manager.IsFolderNowComplete(ti1));
        }

        [Test]
        public void OnRelaunch_ResetsInProgressToCancelled()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.State = TransferStates.InProgress;
            manager.Add(ti);

            manager.OnRelaunch();

            Assert.AreEqual(TransferStates.Cancelled, ti.State);
            Assert.IsNull(ti.RemainingTime);
        }

        [Test]
        public void OnRelaunch_ResetsAbortedToCancelled()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.State = TransferStates.Completed | TransferStates.Aborted;
            manager.Add(ti);

            manager.OnRelaunch();

            Assert.AreEqual(TransferStates.Cancelled, ti.State);
        }

        [Test]
        public void OnRelaunch_PopulatesUsersWhereDownloadFailedDueToOffline()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.State = TransferStates.Completed | TransferStates.UserOffline;
            manager.Add(ti);

            manager.OnRelaunch();

            Assert.IsTrue(TransferState.UsersWhereDownloadFailedDueToOffline.ContainsKey("user1"));
        }

        [Test]
        public void CancelAll_CancelsAllTokens()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti1)] = cts1;
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti2)] = cts2;

            manager.CancelAll();

            Assert.IsTrue(cts1.IsCancellationRequested);
            Assert.IsTrue(cts2.IsCancellationRequested);
        }

        [Test]
        public void CancelAll_PrepareForClear_SetsCancelAndClearFlag()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.InProcessing = true;
            manager.Add(ti);
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti)] = new CancellationTokenSource();

            manager.CancelAll(prepareForClear: true);

            Assert.IsTrue(ti.CancelAndClearFlag);
        }

        [Test]
        public void CancelFolder_CancelsTokensForFolder()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            var ti3 = CreateTransferItem("user1", "\\dir\\folder2\\other.mp3", "folder2");
            manager.Add(ti1);
            manager.Add(ti2);
            manager.Add(ti3);

            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var cts3 = new CancellationTokenSource();
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti1)] = cts1;
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti2)] = cts2;
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti3)] = cts3;

            manager.CancelFolder(manager.AllFolderItems[0]);

            Assert.IsTrue(cts1.IsCancellationRequested);
            Assert.IsTrue(cts2.IsCancellationRequested);
            Assert.IsFalse(cts3.IsCancellationRequested);
        }

        // UI-coupled methods with TransferUIState

        [Test]
        public void GetUICurrentList_NotGroupedByFolder_ReturnsAllTransferItems()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);

            var uiState = new TransferUIState { GroupByFolder = false };
            var result = manager.GetUICurrentList(uiState);

            Assert.AreEqual(manager.AllTransferItems, result);
        }

        [Test]
        public void GetUICurrentList_GroupedByFolder_NoFolderSelected_ReturnsAllFolderItems()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);

            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = null };
            var result = manager.GetUICurrentList(uiState);

            Assert.AreEqual(manager.AllFolderItems, result);
        }

        [Test]
        public void GetUICurrentList_GroupedByFolder_FolderSelected_ReturnsFolderTransferItems()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            manager.Add(ti);

            var folder = manager.AllFolderItems[0];
            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = folder };
            var result = manager.GetUICurrentList(uiState);

            Assert.AreEqual(folder.TransferItems, result);
        }

        [Test]
        public void GetItemAtUserIndex_NotGrouped_ReturnsTransferItem()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var uiState = new TransferUIState { GroupByFolder = false };
            var result = manager.GetItemAtUserIndex(1, uiState);

            Assert.AreEqual(ti2, result);
        }

        [Test]
        public void GetItemAtUserIndex_GroupedNoFolder_ReturnsFolderItem()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3", "folder1");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder2\\file.mp3", "folder2");
            manager.Add(ti1);
            manager.Add(ti2);

            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = null };
            var result = manager.GetItemAtUserIndex(0, uiState);

            Assert.IsInstanceOf<FolderItem>(result);
            Assert.AreEqual("folder1", (result as FolderItem).FolderName);
        }

        [Test]
        public void GetItemAtUserIndex_GroupedWithFolder_ReturnsTransferItemFromFolder()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var folder = manager.AllFolderItems[0];
            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = folder };
            var result = manager.GetItemAtUserIndex(1, uiState);

            Assert.AreEqual(ti2, result);
        }

        [Test]
        public void GetUserIndexForTransferItem_NotGrouped_ReturnsIndex()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var uiState = new TransferUIState { GroupByFolder = false };
            Assert.AreEqual(1, manager.GetUserIndexForTransferItem(ti2, uiState));
        }

        [Test]
        public void GetUserIndexForTransferItem_GroupedNoFolder_ReturnsFolderIndex()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3", "folder1");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder2\\file.mp3", "folder2");
            manager.Add(ti1);
            manager.Add(ti2);

            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = null };
            Assert.AreEqual(1, manager.GetUserIndexForTransferItem(ti2, uiState));
        }

        [Test]
        public void GetUserIndexForTransferItem_GroupedWithFolder_ReturnsIndexInFolder()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var folder = manager.AllFolderItems[0];
            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = folder };
            Assert.AreEqual(1, manager.GetUserIndexForTransferItem(ti2, uiState));
        }

        [Test]
        public void RemoveAtUserIndex_NotGrouped_RemovesCorrectItem()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var uiState = new TransferUIState { GroupByFolder = false };
            var removed = manager.RemoveAtUserIndex(0, uiState);

            Assert.AreEqual(ti1, removed);
            Assert.AreEqual(1, manager.AllTransferItems.Count);
            Assert.AreEqual(ti2, manager.AllTransferItems[0]);
        }

        [Test]
        public void RemoveAtUserIndex_GroupedNoFolder_RemovesEntireFolder()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3", "folder1");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3", "folder1");
            var ti3 = CreateTransferItem("user1", "\\dir\\folder2\\file.mp3", "folder2");
            manager.Add(ti1);
            manager.Add(ti2);
            manager.Add(ti3);

            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = null };
            var removed = manager.RemoveAtUserIndex(0, uiState) as List<TransferItem>;

            Assert.AreEqual(2, removed.Count);
            Assert.AreEqual(1, manager.AllTransferItems.Count);
            Assert.AreEqual(1, manager.AllFolderItems.Count);
        }

        [Test]
        public void NeedsCleanUp_ReturnsTrueWhenIncompleteParentUriSet()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.IncompleteParentUri = "content://some/uri";

            Assert.IsTrue(TransferItemManager.NeedsCleanUp(ti));
        }

        [Test]
        public void NeedsCleanUp_ReturnsFalseWhenNull()
        {
            Assert.IsFalse(TransferItemManager.NeedsCleanUp(null));
        }

        [Test]
        public void NeedsCleanUp_ReturnsFalseWhenCancelAndClearFlagSet()
        {
            var ti = CreateTransferItem("user1", "\\dir\\folder1\\file.mp3");
            ti.IncompleteParentUri = "content://some/uri";
            ti.CancelAndClearFlag = true;

            Assert.IsFalse(TransferItemManager.NeedsCleanUp(ti));
        }

        [Test]
        public void ClearAllReturnCleanupItems_ReturnsItemsNeedingCleanup()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            ti1.IncompleteParentUri = "content://some/uri";
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var cleanupItems = manager.ClearAllReturnCleanupItems();

            Assert.AreEqual(1, cleanupItems.Count);
            Assert.AreEqual(ti1, cleanupItems[0]);
            Assert.AreEqual(0, manager.AllTransferItems.Count);
            Assert.AreEqual(0, manager.AllFolderItems.Count);
        }

        [Test]
        public void CancelSelectedItems_CancelsCorrectTokens()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            var ti3 = CreateTransferItem("user1", "\\dir\\folder1\\file3.mp3");
            manager.Add(ti1);
            manager.Add(ti2);
            manager.Add(ti3);

            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var cts3 = new CancellationTokenSource();
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti1)] = cts1;
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti2)] = cts2;
            TransferState.CancellationTokens[TransferState.ProduceCancellationTokenKey(ti3)] = cts3;

            var uiState = new TransferUIState
            {
                GroupByFolder = false,
                BatchSelectedItems = new List<int> { 0, 2 },
            };

            manager.CancelSelectedItems(uiState);

            Assert.IsTrue(cts1.IsCancellationRequested);
            Assert.IsFalse(cts2.IsCancellationRequested);
            Assert.IsTrue(cts3.IsCancellationRequested);
        }

        [Test]
        public void GetTransferItemsForUser_FiltersCorrectly()
        {
            manager.Add(CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3"));
            manager.Add(CreateTransferItem("user2", "\\dir\\folder1\\file2.mp3"));
            manager.Add(CreateTransferItem("user1", "\\dir\\folder1\\file3.mp3"));

            var items = manager.GetTransferItemsForUser("user1").ToList();

            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items.All(i => i.Username == "user1"));
        }

        [Test]
        public void GetListOfCondition_FiltersCorrectly()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            ti1.State = TransferStates.Queued;
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            ti2.State = TransferStates.InProgress;
            var ti3 = CreateTransferItem("user1", "\\dir\\folder1\\file3.mp3");
            ti3.State = TransferStates.Queued;
            manager.Add(ti1);
            manager.Add(ti2);
            manager.Add(ti3);

            var items = manager.GetListOfCondition(TransferStates.Queued);

            Assert.AreEqual(2, items.Count);
        }

        [Test]
        public void GetTransferItemWithIndexFromAll_FindsCorrectItem()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3");
            manager.Add(ti1);
            manager.Add(ti2);

            var result = manager.GetTransferItemWithIndexFromAll("\\dir\\folder1\\file2.mp3", "user1", out int index);

            Assert.AreEqual(ti2, result);
            Assert.AreEqual(1, index);
        }

        [Test]
        public void GetTransferItemWithIndexFromAll_ReturnsNullWhenNotFound()
        {
            var result = manager.GetTransferItemWithIndexFromAll("nonexistent", "user1", out int index);

            Assert.IsNull(result);
            Assert.AreEqual(-1, index);
        }

        [Test]
        public void ClearAllFromFolder_RemovesOnlyFolderItems()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3", "folder1");
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3", "folder1");
            var ti3 = CreateTransferItem("user1", "\\dir\\folder2\\file.mp3", "folder2");
            manager.Add(ti1);
            manager.Add(ti2);
            manager.Add(ti3);

            var folder1 = manager.AllFolderItems[0];
            manager.ClearAllFromFolder(folder1);

            Assert.AreEqual(1, manager.AllTransferItems.Count);
            Assert.AreEqual(1, manager.AllFolderItems.Count);
            Assert.AreEqual("folder2", manager.AllFolderItems[0].FolderName);
        }

        [Test]
        public void ClearAllCompleteFromFolder_RemovesOnlyCompletedInFolder()
        {
            var ti1 = CreateTransferItem("user1", "\\dir\\folder1\\file1.mp3", "folder1");
            ti1.Progress = 100;
            var ti2 = CreateTransferItem("user1", "\\dir\\folder1\\file2.mp3", "folder1");
            ti2.Progress = 50;
            manager.Add(ti1);
            manager.Add(ti2);

            var folder1 = manager.AllFolderItems[0];
            manager.ClearAllCompleteFromFolder(folder1);

            Assert.AreEqual(1, manager.AllTransferItems.Count);
            Assert.AreEqual(ti2, manager.AllTransferItems[0]);
            Assert.AreEqual(1, manager.AllFolderItems.Count); // folder still exists with remaining item
        }
    }
}
