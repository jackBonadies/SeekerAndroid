using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.Linq;

namespace UnitTestCommon
{
    [TestFixture]
    public class FolderItemTests
    {
        private TransferItem CreateTransferItem(string username, string fullFilename, string folderName = "folder1", long size = 1000, bool isUpload = false)
        {
            return new TransferItem
            {
                Username = username,
                FullFilename = fullFilename,
                FolderName = folderName,
                Filename = fullFilename.Split('\\').Last(),
                Size = size,
                isUpload = isUpload,
            };
        }

        // --- Constructor ---

        [Test]
        public void Constructor_SetsFieldsCorrectly()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual("jazz", folder.FolderName);
            Assert.AreEqual("alice", folder.Username);
            Assert.AreEqual(1, folder.TransferItems.Count);
            Assert.AreEqual(ti, folder.TransferItems[0]);
        }

        [Test]
        public void Constructor_NullFolderName_DerivesFolderNameFromFilename()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem(null, "alice", ti);

            Assert.AreEqual("jazz", folder.FolderName);
        }

        [Test]
        public void DefaultConstructor_CreatesEmptyTransferItems()
        {
            var folder = new FolderItem();

            Assert.IsNotNull(folder.TransferItems);
            Assert.AreEqual(0, folder.TransferItems.Count);
        }

        // --- Add / Remove / HasTransferItem / IsEmpty ---

        [Test]
        public void Add_AddsItem()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti1);

            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            folder.Add(ti2);

            Assert.AreEqual(2, folder.TransferItems.Count);
            Assert.IsTrue(folder.TransferItems.Contains(ti2));
        }

        [Test]
        public void Remove_RemovesItem()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            folder.Remove(ti1);

            Assert.AreEqual(1, folder.TransferItems.Count);
            Assert.IsFalse(folder.TransferItems.Contains(ti1));
        }

        [Test]
        public void HasTransferItem_ReturnsTrueForContainedItem()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.IsTrue(folder.HasTransferItem(ti));
        }

        [Test]
        public void HasTransferItem_ReturnsFalseForUnknownItem()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti1);

            Assert.IsFalse(folder.HasTransferItem(ti2));
        }

        [Test]
        public void IsEmpty_TrueAfterRemovingAll()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            folder.Remove(ti);

            Assert.IsTrue(folder.IsEmpty());
        }

        [Test]
        public void IsEmpty_FalseWhenItemsExist()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.IsFalse(folder.IsEmpty());
        }

        // --- IsUpload ---

        [Test]
        public void IsUpload_ReturnsTrueWhenFirstItemIsUpload()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz", isUpload: true);
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.IsTrue(folder.IsUpload());
        }

        [Test]
        public void IsUpload_ReturnsFalseWhenFirstItemIsDownload()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz", isUpload: false);
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.IsFalse(folder.IsUpload());
        }

        [Test]
        public void IsUpload_ReturnsFalseWhenEmpty()
        {
            var folder = new FolderItem();
            Assert.IsFalse(folder.IsUpload());
        }

        // --- GetDisplayFolderName ---

        [Test]
        public void GetDisplayFolderName_SingleLevel_ReturnsFolderName()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual("jazz", folder.GetDisplayFolderName());
        }

        [Test]
        public void GetDisplayFolderName_MultiLevel_ReturnsReversed()
        {
            var ti = CreateTransferItem("alice", "\\music\\artist\\album\\song.mp3", "artist\\album");
            var folder = new FolderItem("artist\\album", "alice", ti);

            Assert.AreEqual("album\\artist", folder.GetDisplayFolderName());
        }

        [Test]
        public void GetDisplayFolderName_ThreeLevels_ReturnsReversed()
        {
            var ti = CreateTransferItem("alice", "\\music\\a\\b\\c\\song.mp3", "a\\b\\c");
            var folder = new FolderItem("a\\b\\c", "alice", ti);

            Assert.AreEqual("c\\b\\a", folder.GetDisplayFolderName());
        }

        // --- GetDirectoryLevel ---

        [Test]
        public void GetDirectoryLevel_SingleFolder_Returns1()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual(1, folder.GetDirectoryLevel());
        }

        [Test]
        public void GetDirectoryLevel_NestedFolder_ReturnsCorrectLevel()
        {
            var ti = CreateTransferItem("alice", "\\music\\artist\\album\\song.mp3", "artist\\album");
            var folder = new FolderItem("artist\\album", "alice", ti);

            Assert.AreEqual(2, folder.GetDirectoryLevel());
        }

        [Test]
        public void GetDirectoryLevel_NullFolderName_Returns1()
        {
            var folder = new FolderItem { FolderName = null };

            Assert.AreEqual(1, folder.GetDirectoryLevel());
        }

        // --- GetFolderProgress ---

        [Test]
        public void GetFolderProgress_AllComplete_ReturnsFullBytes()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", size: 1000);
            ti1.Progress = 100;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 2000);
            ti2.Progress = 100;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var (totalBytes, bytesCompleted) = folder.GetFolderProgress();

            Assert.AreEqual(3000, totalBytes);
            Assert.AreEqual(3000, bytesCompleted);
        }

        [Test]
        public void GetFolderProgress_HalfComplete_ReturnsHalfBytes()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", size: 1000);
            ti1.Progress = 100;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 1000);
            ti2.Progress = 0;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var (totalBytes, bytesCompleted) = folder.GetFolderProgress();

            Assert.AreEqual(2000, totalBytes);
            Assert.AreEqual(1000, bytesCompleted);
        }

        [Test]
        public void GetFolderProgress_ZeroTotalBytes_ReturnsZero()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz", size: 0);
            ti.Progress = 0;
            var folder = new FolderItem("jazz", "alice", ti);

            var (totalBytes, bytesCompleted) = folder.GetFolderProgress();

            Assert.AreEqual(0, totalBytes);
            Assert.AreEqual(0, bytesCompleted);
        }

        [Test]
        public void GetFolderProgress_PartialProgress_CalculatesCorrectly()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", size: 1000);
            ti1.Progress = 50; // 500 bytes done
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 3000);
            ti2.Progress = 25; // 750 bytes done
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var (totalBytes, bytesCompleted) = folder.GetFolderProgress();

            Assert.AreEqual(4000, totalBytes);
            Assert.AreEqual(1250, bytesCompleted);
        }

        // --- GetQueueLength ---

        [Test]
        public void GetQueueLength_ReturnsLowestQueuedPosition()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued | TransferStates.Remotely;
            ti1.QueueLength = 50;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Queued | TransferStates.Remotely;
            ti2.QueueLength = 10;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            Assert.AreEqual(10, folder.GetQueueLength());
        }

        [Test]
        public void GetQueueLength_IgnoresNonQueuedItems()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued | TransferStates.Remotely;
            ti1.QueueLength = 50;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.InProgress;
            ti2.QueueLength = 5;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            Assert.AreEqual(50, folder.GetQueueLength());
        }

        [Test]
        public void GetQueueLength_IgnoresLocallyQueuedItems()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued | TransferStates.Locally;
            ti1.QueueLength = 5;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Queued | TransferStates.Remotely;
            ti2.QueueLength = 50;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            Assert.AreEqual(50, folder.GetQueueLength());
            Assert.AreEqual(ti2, folder.GetLowestQueuedTransferItem());
        }

        [Test]
        public void GetQueueLength_NoQueuedItems_ReturnsIntMaxValue()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            ti.State = TransferStates.InProgress;
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual(int.MaxValue, folder.GetQueueLength());
        }

        // --- GetLowestQueuedTransferItem ---

        [Test]
        public void GetLowestQueuedTransferItem_ReturnsItemWithLowestQueue()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued | TransferStates.Remotely;
            ti1.QueueLength = 50;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Queued | TransferStates.Remotely;
            ti2.QueueLength = 10;
            var ti3 = CreateTransferItem("alice", "\\music\\jazz\\song3.mp3", "jazz");
            ti3.State = TransferStates.Queued | TransferStates.Remotely;
            ti3.QueueLength = 30;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);
            folder.Add(ti3);

            var lowest = folder.GetLowestQueuedTransferItem();

            Assert.AreEqual(ti2, lowest);
        }

        [Test]
        public void GetLowestQueuedTransferItem_NoQueuedItems_ReturnsNull()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            ti.State = TransferStates.InProgress;
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.IsNull(folder.GetLowestQueuedTransferItem());
        }

        // --- GetState ---

        [Test]
        public void GetState_AnyInProgress_ReturnsInProgress()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.InProgress;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out bool isFailed, out bool anyOffline);

            Assert.AreEqual(TransferStates.InProgress, state);
            Assert.IsFalse(isFailed);
        }

        [Test]
        public void GetState_AllQueued_ReturnsQueued()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Queued;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out _, out _);

            Assert.IsTrue(state.HasFlag(TransferStates.Queued));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void GetState_QueuedRemotelyBeatsQueuedLocally_EitherOrder(bool remotelyFirst)
        {
            var local = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            local.State = TransferStates.Queued | TransferStates.Locally;
            var remote = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            remote.State = TransferStates.Queued | TransferStates.Remotely;
            var folder = new FolderItem("jazz", "alice", remotelyFirst ? remote : local);
            folder.Add(remotelyFirst ? local : remote);

            var state = folder.GetState(out _, out _);

            Assert.AreEqual(TransferStates.Queued | TransferStates.Remotely, state);
        }

        [Test]
        public void GetState_AllQueuedLocally_ReturnsQueuedLocally()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued | TransferStates.Locally;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Queued | TransferStates.Locally;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out _, out _);

            Assert.AreEqual(TransferStates.Queued | TransferStates.Locally, state);
        }

        [Test]
        public void GetState_AllSucceeded_ReturnsSucceeded()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Completed | TransferStates.Succeeded;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Completed | TransferStates.Succeeded;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out bool isFailed, out _);

            Assert.IsTrue(state.HasFlag(TransferStates.Succeeded));
            Assert.IsFalse(isFailed);
        }

        [Test]
        public void GetState_FailedItem_SetsIsFailedTrue()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Completed | TransferStates.Succeeded;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Completed | TransferStates.Errored;
            ti2.Failed = true;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            folder.GetState(out bool isFailed, out _);

            Assert.IsTrue(isFailed);
        }

        [Test]
        public void GetState_FailedOfflineItem_SetsAnyOfflineTrue()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            ti.State = TransferStates.Completed | TransferStates.UserOffline;
            ti.Failed = true;
            var folder = new FolderItem("jazz", "alice", ti);

            folder.GetState(out bool isFailed, out bool anyOffline);

            Assert.IsTrue(isFailed);
            Assert.IsTrue(anyOffline);
        }

        [Test]
        public void GetState_InProgressResetsIsFailed()
        {
            // If a failed item is followed by an InProgress item, isFailed should be false
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Completed | TransferStates.Errored;
            ti1.Failed = true;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.InProgress;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out bool isFailed, out _);

            Assert.AreEqual(TransferStates.InProgress, state);
            Assert.IsFalse(isFailed);
        }

        [Test]
        public void GetState_CancelledTakesPriorityOverSucceeded()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Completed | TransferStates.Succeeded;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Cancelled;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out _, out _);

            Assert.IsTrue(state.HasFlag(TransferStates.Cancelled));
        }

        [Test]
        public void GetState_QueuedTakesPriorityOverCancelled()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Cancelled;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Queued;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out _, out _);

            Assert.IsTrue(state.HasFlag(TransferStates.Queued));
        }

        [Test]
        public void GetState_ErroredTakesPriorityOverCancelled()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Cancelled;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Completed | TransferStates.Errored;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out _, out _);

            Assert.IsTrue(state.HasFlag(TransferStates.Errored));
        }

        [Test]
        public void GetState_InitializingTakesPriorityOverQueued()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Queued;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.Initializing;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            var state = folder.GetState(out _, out _);

            Assert.IsTrue(state.HasFlag(TransferStates.Initializing));
        }

        [Test]
        public void GetState_EmptyFolder_ReturnsNone()
        {
            var folder = new FolderItem();

            var state = folder.GetState(out bool isFailed, out bool anyOffline);

            Assert.AreEqual(TransferStates.None, state);
            Assert.IsFalse(isFailed);
            Assert.IsFalse(anyOffline);
        }

        // --- Priority chain: InProgress > Initializing/Requested/Aborted > Queued > Errored/Rejected/TimedOut > Cancelled > Succeeded ---

        [Test]
        public void GetState_FullPriorityChain_HighestWins()
        {
            // Succeeded < Cancelled < Errored < Queued < Initializing < InProgress
            var tiSucceeded = CreateTransferItem("alice", "\\music\\jazz\\s1.mp3", "jazz");
            tiSucceeded.State = TransferStates.Completed | TransferStates.Succeeded;
            var tiCancelled = CreateTransferItem("alice", "\\music\\jazz\\s2.mp3", "jazz");
            tiCancelled.State = TransferStates.Cancelled;
            var tiErrored = CreateTransferItem("alice", "\\music\\jazz\\s3.mp3", "jazz");
            tiErrored.State = TransferStates.Completed | TransferStates.Errored;
            var tiQueued = CreateTransferItem("alice", "\\music\\jazz\\s4.mp3", "jazz");
            tiQueued.State = TransferStates.Queued;

            var folder = new FolderItem("jazz", "alice", tiSucceeded);
            folder.Add(tiCancelled);
            folder.Add(tiErrored);
            folder.Add(tiQueued);

            var state = folder.GetState(out _, out _);
            Assert.IsTrue(state.HasFlag(TransferStates.Queued));
        }

        // --- ClearAllComplete ---

        [Test]
        public void ClearAllComplete_Download_RemovesSucceededItems()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.State = TransferStates.Completed | TransferStates.Succeeded;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.State = TransferStates.InProgress;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            folder.ClearAllComplete();

            Assert.AreEqual(1, folder.TransferItems.Count);
            Assert.AreEqual(ti2, folder.TransferItems[0]);
        }

        [Test]
        public void ClearAllComplete_Upload_AlsoRemovesCompletedStates()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", isUpload: true);
            ti1.State = TransferStates.Completed | TransferStates.Succeeded;
            ti1.Progress = 0; // Progress is 0, but state indicates completed
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", isUpload: true);
            ti2.State = TransferStates.InProgress;
            ti2.Progress = 50;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            folder.ClearAllComplete();

            Assert.AreEqual(1, folder.TransferItems.Count);
            Assert.AreEqual(ti2, folder.TransferItems[0]);
        }

        [Test]
        public void ClearAllComplete_NoCompletedItems_NoChange()
        {
            var ti1 = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            ti1.Progress = 50;
            var ti2 = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            ti2.Progress = 0;
            var folder = new FolderItem("jazz", "alice", ti1);
            folder.Add(ti2);

            folder.ClearAllComplete();

            Assert.AreEqual(2, folder.TransferItems.Count);
        }

        // --- ITransferItem interface ---

        [Test]
        public void GetFolderName_ReturnsFolderName()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual("jazz", folder.GetFolderName());
        }

        [Test]
        public void GetUsername_ReturnsUsername()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual("alice", folder.GetUsername());
        }

        [Test]
        public void GetRemainingTime_NoSpeedSample_ReturnsNull()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            ti.State = TransferStates.InProgress;
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.IsNull(folder.GetRemainingTime());
        }

        [Test]
        public void GetRemainingTime_QueuedFilesCountAtTheInProgressSpeed()
        {
            var active = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", size: 1000);
            active.State = TransferStates.InProgress;
            active.BytesTransferred = 400;
            active.AvgSpeed = 100;
            var queued = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 2000);
            queued.State = TransferStates.Queued;
            var done = CreateTransferItem("alice", "\\music\\jazz\\song3.mp3", "jazz", size: 5000);
            done.State = TransferStates.Succeeded;
            done.BytesTransferred = 5000;
            var folder = new FolderItem("jazz", "alice", active);
            folder.Add(queued);
            folder.Add(done);

            // (600 + 2000) / 100
            Assert.AreEqual(TimeSpan.FromSeconds(26), folder.GetRemainingTime());
        }

        [Test]
        public void GetRemainingTime_FailedAndPausedFilesAreLeftOut()
        {
            var active = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", size: 1000);
            active.State = TransferStates.InProgress;
            active.AvgSpeed = 50;
            var failed = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 9000);
            failed.State = TransferStates.Completed | TransferStates.Errored;
            var paused = CreateTransferItem("alice", "\\music\\jazz\\song3.mp3", "jazz", size: 9000);
            paused.State = TransferStates.Completed | TransferStates.Cancelled;
            var folder = new FolderItem("jazz", "alice", active);
            folder.Add(failed);
            folder.Add(paused);

            Assert.AreEqual(TimeSpan.FromSeconds(20), folder.GetRemainingTime());
        }

        [Test]
        public void GetRemainingTime_TwoInProgressFiles_SumsTheirSpeeds()
        {
            var a = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", size: 1000);
            a.State = TransferStates.InProgress;
            a.AvgSpeed = 100;
            var b = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 1000);
            b.State = TransferStates.InProgress;
            b.AvgSpeed = 300;
            var folder = new FolderItem("jazz", "alice", a);
            folder.Add(b);

            Assert.AreEqual(TimeSpan.FromSeconds(5), folder.GetRemainingTime());
        }

        [Test]
        public void GetRemainingTime_OvershotBytesClampToZero()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz", size: 1000);
            ti.State = TransferStates.InProgress;
            ti.BytesTransferred = 1200;
            ti.AvgSpeed = 10;
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual(TimeSpan.Zero, folder.GetRemainingTime());
        }

        private static readonly DateTime Now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);

        private TransferItem CreateSucceededItem(string fullFilename, double avgSpeed, TimeSpan sampledAgo, long size = 1000)
        {
            var ti = CreateTransferItem("alice", fullFilename, "jazz", size: size);
            ti.State = TransferStates.Completed | TransferStates.Succeeded;
            ti.BytesTransferred = size;
            ti.AvgSpeed = avgSpeed;
            ti.AvgSpeedSampledUtc = Now - sampledAgo;
            return ti;
        }

        [Test]
        public void GetRemainingTime_BetweenFiles_HoldsTheLastSampledSpeed()
        {
            var done = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(2));
            var next = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 1000);
            next.State = TransferStates.Queued | TransferStates.Remotely;
            var folder = new FolderItem("jazz", "alice", done);
            folder.Add(next);

            Assert.AreEqual(TimeSpan.FromSeconds(10), folder.GetRemainingTime(Now));
        }

        [Test]
        public void GetRemainingTime_NewFileWithoutSampleUsesTheHeldSpeed()
        {
            var done = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(3));
            var next = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 1000);
            next.State = TransferStates.InProgress;
            next.AvgSpeed = 0;
            var folder = new FolderItem("jazz", "alice", done);
            folder.Add(next);

            Assert.AreEqual(TimeSpan.FromSeconds(10), folder.GetRemainingTime(Now));
        }

        [Test]
        public void GetRemainingTime_LiveSpeedWinsOverHeld()
        {
            var done = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(1));
            var active = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 3000);
            active.State = TransferStates.InProgress;
            active.AvgSpeed = 300;
            var folder = new FolderItem("jazz", "alice", done);
            folder.Add(active);

            Assert.AreEqual(TimeSpan.FromSeconds(10), folder.GetRemainingTime(Now));
        }

        [Test]
        public void GetRemainingTime_HeldSpeedExpires()
        {
            var done = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(31));
            var next = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 1000);
            next.State = TransferStates.Queued | TransferStates.Remotely;
            var folder = new FolderItem("jazz", "alice", done);
            folder.Add(next);

            Assert.IsNull(folder.GetRemainingTime(Now));
        }

        [Test]
        public void GetRemainingTime_UnstampedSpeedIsNotHeld()
        {
            // AvgSpeed persists across restarts, the stamp does not.
            var done = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz", size: 1000);
            done.State = TransferStates.Completed | TransferStates.Succeeded;
            done.BytesTransferred = 1000;
            done.AvgSpeed = 100;
            var next = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 1000);
            next.State = TransferStates.Queued | TransferStates.Remotely;
            var folder = new FolderItem("jazz", "alice", done);
            folder.Add(next);

            Assert.IsNull(folder.GetRemainingTime(Now));
        }

        [Test]
        public void GetRemainingTime_NothingPending_ReturnsNullEvenWithRecentSample()
        {
            var a = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(1));
            var b = CreateSucceededItem("\\music\\jazz\\song2.mp3", 100, TimeSpan.FromSeconds(1));
            var folder = new FolderItem("jazz", "alice", a);
            folder.Add(b);

            Assert.IsNull(folder.GetRemainingTime(Now));

            b.State = TransferStates.Completed | TransferStates.Cancelled;
            b.BytesTransferred = 400;

            Assert.IsNull(folder.GetRemainingTime(Now));
        }

        [Test]
        public void GetAvgSpeed_NoSpeedSample_ReturnsZero()
        {
            var ti = CreateTransferItem("alice", "\\music\\jazz\\song.mp3", "jazz");
            ti.State = TransferStates.InProgress;
            var folder = new FolderItem("jazz", "alice", ti);

            Assert.AreEqual(0, folder.GetAvgSpeed());
        }

        [Test]
        public void GetAvgSpeed_TwoInProgressFiles_SumsTheirSpeeds()
        {
            var a = CreateTransferItem("alice", "\\music\\jazz\\song1.mp3", "jazz");
            a.State = TransferStates.InProgress;
            a.AvgSpeed = 100;
            var b = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            b.State = TransferStates.InProgress;
            b.AvgSpeed = 300;
            var folder = new FolderItem("jazz", "alice", a);
            folder.Add(b);

            Assert.AreEqual(400, folder.GetAvgSpeed());
        }

        [Test]
        public void GetAvgSpeed_BetweenFiles_HoldsTheLastSampledSpeed()
        {
            var done = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(2));
            var next = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz", size: 1000);
            next.State = TransferStates.Queued | TransferStates.Remotely;
            var folder = new FolderItem("jazz", "alice", done);
            folder.Add(next);

            Assert.AreEqual(100, folder.GetAvgSpeed(Now));
            // the estimate beside it is derived from the same speed
            Assert.AreEqual(TimeSpan.FromSeconds(10), folder.GetRemainingTime(Now));
        }

        [Test]
        public void GetAvgSpeed_HeldSpeedExpires()
        {
            var done = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(31));
            var next = CreateTransferItem("alice", "\\music\\jazz\\song2.mp3", "jazz");
            next.State = TransferStates.Queued | TransferStates.Remotely;
            var folder = new FolderItem("jazz", "alice", done);
            folder.Add(next);

            Assert.AreEqual(0, folder.GetAvgSpeed(Now));
        }

        [Test]
        public void GetAvgSpeed_NothingPending_ReturnsZeroEvenWithRecentSample()
        {
            var a = CreateSucceededItem("\\music\\jazz\\song1.mp3", 100, TimeSpan.FromSeconds(1));
            var b = CreateSucceededItem("\\music\\jazz\\song2.mp3", 100, TimeSpan.FromSeconds(1));
            var folder = new FolderItem("jazz", "alice", a);
            folder.Add(b);

            Assert.AreEqual(0, folder.GetAvgSpeed(Now));

            b.State = TransferStates.Completed | TransferStates.Cancelled;
            b.BytesTransferred = 400;

            Assert.AreEqual(0, folder.GetAvgSpeed(Now));
        }
    }
}
