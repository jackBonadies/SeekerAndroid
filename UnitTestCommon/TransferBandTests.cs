using NUnit.Framework;
using Seeker;
using Soulseek;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using StringAssert = NUnit.Framework.Legacy.StringAssert;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferBandTests
    {
        private const TransferStates Done = TransferStates.Completed | TransferStates.Succeeded;
        private const TransferStates Paused = TransferStates.Completed | TransferStates.Cancelled;
        private const TransferStates Errored = TransferStates.Completed | TransferStates.Errored;
        private const TransferStates Pending = TransferStates.Queued | TransferStates.Locally;
        private const TransferStates Downloading = TransferStates.InProgress;

        private TransferItemManager manager;

        [SetUp]
        public void Setup()
        {
            manager = new TransferItemManager(false);
        }

        // stamp: the user action that added it, 0 for a row saved before the stamp existed
        private TransferItem Add(string folder, string file, TransferStates state, long stamp)
        {
            var ti = new TransferItem
            {
                Username = "u1",
                FolderName = folder,
                Filename = file,
                FullFilename = @"\dir\" + folder + @"\" + file,
                State = state,
                LastActionStamp = stamp,
            };
            manager.Add(ti);
            return ti;
        }

        private TransferItem Add(string folder, string file, TransferStates state)
        {
            return Add(folder, file, state, TransferBands.NextStamp());
        }

        private FolderItem Folder(string name)
        {
            return manager.AllFolderItems.First(f => f.FolderName == name);
        }

        private List<ITransferItem> Sorted(TransferSortKey key, bool groupByFolder, bool descending = false, FolderItem inFolder = null)
        {
            return manager.GetUISnapshot(new TransferUIState
            {
                GroupByFolder = groupByFolder,
                CurrentlySelectedFolder = inFolder,
                SortKey = key,
                SortDescending = descending,
            });
        }

        private List<string> SortedFolderNames(TransferSortKey key, bool descending = false)
        {
            return Sorted(key, groupByFolder: true, descending).Cast<FolderItem>().Select(f => f.FolderName).ToList();
        }

        // --- band of a state, one case per status chip branch ---

        [TestCase(TransferStates.None, TransferBand.Paused)]
        [TestCase(TransferStates.Completed | TransferStates.Succeeded, TransferBand.Completed)]
        [TestCase(TransferStates.InProgress, TransferBand.Active)]
        [TestCase(TransferStates.Initializing, TransferBand.Active)]
        [TestCase(TransferStates.Requested, TransferBand.Active)]
        [TestCase(TransferStates.Queued | TransferStates.Locally, TransferBand.Active)]
        [TestCase(TransferStates.Queued | TransferStates.Remotely, TransferBand.Active)]
        [TestCase(TransferStates.Cancelled, TransferBand.Paused)]
        [TestCase(TransferStates.Completed | TransferStates.Cancelled, TransferBand.Paused)]
        [TestCase(TransferStates.Completed | TransferStates.Rejected, TransferBand.Failed)]
        [TestCase(TransferStates.Completed | TransferStates.TimedOut, TransferBand.Failed)]
        [TestCase(TransferStates.Completed | TransferStates.Errored | TransferStates.UserOffline, TransferBand.Failed)]
        [TestCase(TransferStates.Completed | TransferStates.Errored | TransferStates.CannotConnect, TransferBand.Failed)]
        [TestCase(TransferStates.Errored | TransferStates.UserOffline | TransferStates.FallenFromQueue, TransferBand.Failed)]
        [TestCase(TransferStates.Completed | TransferStates.Aborted, TransferBand.Active)]
        [TestCase(TransferStates.Completed | TransferStates.Errored, TransferBand.Failed)]
        [TestCase(TransferStates.Errored, TransferBand.Failed)]
        public void Of_FollowsTheStatusChip(TransferStates state, TransferBand expected)
        {
            Assert.AreEqual(expected, TransferBands.Of(state));
        }

        [Test]
        public void NextStamp_IsStrictlyIncreasing()
        {
            long previous = TransferBands.NextStamp();
            for (int i = 0; i < 10000; i++)
            {
                long next = TransferBands.NextStamp();
                Assert.Greater(next, previous);
                previous = next;
            }
        }

        [Test]
        public void Observe_KeepsNewStampsAboveARestoredOne()
        {
            long future = TransferBands.NextStamp() + System.TimeSpan.FromHours(1).Ticks;

            Add("f1", "1.mp3", Done, future);

            Assert.Greater(TransferBands.NextStamp(), future);
        }

        // --- only user actions stamp ---

        [Test]
        public void StateChanges_NeverStamp()
        {
            var ti = Add("f1", "1.mp3", Pending, 42);

            ti.State = TransferStates.Requested;
            ti.State = Downloading;
            ti.State = Errored;
            ti.State = Pending;
            ti.State = Done;

            Assert.AreEqual(42, ti.LastActionStamp);
        }

        [Test]
        public void MarkUserPause_OnlyStampsActiveRows()
        {
            var active = Add("f1", "1.mp3", Downloading, 1);
            var done = Add("f1", "2.mp3", Done, 1);
            var failed = Add("f1", "3.mp3", Errored, 1);
            var paused = Add("f1", "4.mp3", Paused, 1);

            foreach (var ti in new[] { active, done, failed, paused })
            {
                ti.MarkUserPause(99);
            }

            Assert.AreEqual(99, active.LastActionStamp);
            Assert.AreEqual(1, done.LastActionStamp);
            Assert.AreEqual(1, failed.LastActionStamp);
            Assert.AreEqual(1, paused.LastActionStamp);
        }

        [Test]
        public void CancelAll_GivesTheActiveRowsOneStamp()
        {
            var a = Add("f1", "1.mp3", Downloading, 1);
            var b = Add("f2", "1.mp3", Pending, 1);
            var done = Add("f2", "2.mp3", Done, 1);

            manager.CancelAll();

            Assert.Greater(a.LastActionStamp, 1);
            Assert.AreEqual(a.LastActionStamp, b.LastActionStamp);
            Assert.AreEqual(1, done.LastActionStamp);
        }

        [Test]
        public void CancelSelectedItems_SharesOneStampAcrossFoldersAndFiles()
        {
            var inFolder = Add("f1", "1.mp3", Downloading, 1);
            var single = Add("f2", "1.mp3", Pending, 1);
            var uiState = new TransferUIState { BatchSelectedItems = new HashSet<ITransferItem> { Folder("f1"), single } };

            manager.CancelSelectedItems(uiState);

            Assert.Greater(inFolder.LastActionStamp, 1);
            Assert.AreEqual(inFolder.LastActionStamp, single.LastActionStamp);
        }

        [Test]
        public void Relaunch_PausesActiveRowsWithoutStamping()
        {
            var active = Add("f1", "1.mp3", Downloading, 5);

            manager.OnRelaunch();

            Assert.AreEqual(TransferStates.Cancelled, active.State);
            Assert.AreEqual(5, active.LastActionStamp);
        }

        [Test]
        public void Relaunch_StampsLegacyRowsByFolderInDateAddedOrder()
        {
            var older1 = Add("older", "1.mp3", Done, 0);
            var newer1 = Add("newer", "1.mp3", Done, 0);
            var older2 = Add("older", "2.mp3", Done, 0);
            var stamped = Add("newer", "2.mp3", Done);

            manager.OnRelaunch();

            Assert.AreEqual(older1.LastActionStamp, older2.LastActionStamp);
            Assert.Less(older1.LastActionStamp, newer1.LastActionStamp);
            Assert.Less(newer1.LastActionStamp, stamped.LastActionStamp);
            CollectionAssert.AreEqual(new ITransferItem[] { newer1, older1, older2 },
                Sorted(TransferSortKey.LastAction, groupByFolder: false, descending: true).Where(t => t != stamped));
        }

        // --- persistence ---

        private static string Serialize(List<TransferItem> items)
        {
            using (var writer = new StringWriter())
            {
                new XmlSerializer(typeof(List<TransferItem>)).Serialize(writer, items);
                return writer.ToString();
            }
        }

        private static List<TransferItem> Deserialize(string xml)
        {
            using (var reader = new StringReader(xml))
            {
                return (List<TransferItem>)new XmlSerializer(typeof(List<TransferItem>)).Deserialize(reader);
            }
        }

        [Test]
        public void Xml_KeepsTheStateAndTheStamp()
        {
            var ti = new TransferItem { Username = "u1", FullFilename = @"\a\b.mp3", State = Done, LastActionStamp = 1234 };

            string xml = Serialize(new List<TransferItem> { ti });
            var restored = Deserialize(xml).Single();

            StringAssert.Contains("<State>Completed Succeeded</State>", xml);
            Assert.AreEqual(Done, restored.State);
            Assert.AreEqual(1234, restored.LastActionStamp);
        }

        // a file written before the stamp existed
        [Test]
        public void Xml_ReadsTheOldLayout()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>"
                + "<ArrayOfTransferItem xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                + "<TransferItem><Filename>b.mp3</Filename><Username>u1</Username><FolderName>a</FolderName>"
                + "<FullFilename>\\a\\b.mp3</FullFilename><Progress>100</Progress><BytesTransferred>5</BytesTransferred>"
                + "<Failed>true</Failed><State>Completed Errored UserOffline</State><Size>5</Size><isUpload>false</isUpload>"
                + "</TransferItem></ArrayOfTransferItem>";

            var restored = Deserialize(xml).Single();

            Assert.AreEqual(TransferStates.Completed | TransferStates.Errored | TransferStates.UserOffline, restored.State);
            Assert.AreEqual(5, restored.Size);
            Assert.AreEqual(0, restored.LastActionStamp);
        }

        // --- status sort ---

        [Test]
        public void State_SortsByBand([Values(false, true)] bool descending)
        {
            var done = Add("f1", "1.mp3", Done);
            var paused = Add("f1", "2.mp3", Paused);
            var failed = Add("f1", "3.mp3", Errored);
            var active = Add("f1", "4.mp3", Downloading);

            var expected = descending
                ? new ITransferItem[] { done, paused, failed, active }
                : new ITransferItem[] { active, failed, paused, done };
            CollectionAssert.AreEqual(expected, Sorted(TransferSortKey.Status, groupByFolder: false, descending));
            CollectionAssert.AreEqual(expected, Sorted(TransferSortKey.Status, groupByFolder: true, descending, Folder("f1")));
        }

        // within a band: the last action, newest first, whichever way the bands run
        [Test]
        public void State_NewestActionFirstInABand([Values(false, true)] bool descending)
        {
            var first = Add("f1", "1.mp3", Downloading);
            var second = Add("f1", "2.mp3", Downloading);
            var third = Add("f1", "3.mp3", Downloading);

            // finishing is not an action, so the order they finish in does not matter
            first.State = Done;
            third.State = Done;
            second.State = Done;

            CollectionAssert.AreEqual(new ITransferItem[] { third, second, first }, Sorted(TransferSortKey.Status, groupByFolder: false, descending));
        }

        [Test]
        public void State_ABatchKeepsTrackOrder()
        {
            long added = TransferBands.NextStamp();
            var a = Add("f1", "1.mp3", Paused, added);
            var b = Add("f1", "2.mp3", Paused, added);
            var c = Add("f1", "3.mp3", Paused, added);

            CollectionAssert.AreEqual(new ITransferItem[] { a, b, c }, Sorted(TransferSortKey.Status, groupByFolder: false));
        }

        [Test]
        public void State_RowsWithoutAStampFallBackToDateAdded()
        {
            var a = Add("f1", "1.mp3", Done, 0);
            var b = Add("f1", "2.mp3", Done, 0);
            var stamped = Add("f1", "3.mp3", Done);

            CollectionAssert.AreEqual(new ITransferItem[] { stamped, a, b }, Sorted(TransferSortKey.Status, groupByFolder: false));
        }

        [Test]
        public void State_Folders([Values(false, true)] bool descending)
        {
            Add("done-old", "1.mp3", Done);
            Add("active", "1.mp3", Downloading);
            Add("active", "2.mp3", Done);
            Add("paused", "1.mp3", Paused);
            Add("failed", "1.mp3", Errored);
            Add("failed", "2.mp3", Done);
            Add("done-new", "1.mp3", Done);

            var expected = descending
                ? new[] { "done-new", "done-old", "paused", "failed", "active" }
                : new[] { "active", "failed", "paused", "done-new", "done-old" };
            CollectionAssert.AreEqual(expected, SortedFolderNames(TransferSortKey.Status, descending));
        }

        // --- folders ---

        [Test]
        public void Folder_BandIsItsBestFile_AndAgreesWithGetState()
        {
            var mixes = new[]
            {
                new[] { Done, Done },
                new[] { Done, Paused },
                new[] { Done, Paused, Errored },
                new[] { Done, Paused, Errored, TransferStates.Queued | TransferStates.Remotely },
                new[] { Errored, Downloading },
                new[] { Paused, Pending },
                new[] { Done, TransferStates.Completed | TransferStates.TimedOut },
                new[] { Paused, TransferStates.Completed | TransferStates.Aborted },
            };
            for (int i = 0; i < mixes.Length; i++)
            {
                string name = "mix" + i;
                for (int j = 0; j < mixes[i].Length; j++)
                {
                    Add(name, j + ".mp3", mixes[i][j]);
                }
                var folder = Folder(name);

                TransferBand band = folder.GetBand(out _);

                Assert.AreEqual(mixes[i].Select(TransferBands.Of).Min(), band, name);
                Assert.AreEqual(TransferBands.Of(folder.GetState(out _, out _)), band, name);
            }
        }

        [Test]
        public void Folder_StampIsTheNewestActionOnAnyFile()
        {
            long added = TransferBands.NextStamp();
            var a = Add("f1", "1.mp3", Done, added);
            Add("f1", "2.mp3", Errored, added);
            var folder = Folder("f1");

            a.State = Pending;
            folder.GetBand(out long afterAutoRetry);
            long retried = TransferBands.NextStamp();
            a.MarkUserAction(retried);
            folder.GetBand(out long afterUserRetry);

            Assert.AreEqual(added, afterAutoRetry);
            Assert.AreEqual(retried, afterUserRetry);
        }

        // --- last action sort ---

        [Test]
        public void LastAction_NewestFirstWhenDescending([Values(false, true)] bool descending)
        {
            var old = Add("f1", "1.mp3", Done);
            var newer = Add("f2", "1.mp3", Paused);
            var resumed = Add("f3", "1.mp3", Downloading);
            old.MarkUserAction(TransferBands.NextStamp());

            var expected = descending
                ? new ITransferItem[] { old, resumed, newer }
                : new ITransferItem[] { newer, resumed, old };
            CollectionAssert.AreEqual(expected, Sorted(TransferSortKey.LastAction, groupByFolder: false, descending));
        }

        [Test]
        public void LastAction_ABatchKeepsTrackOrderBothWays([Values(false, true)] bool descending)
        {
            long added = TransferBands.NextStamp();
            var a = Add("f1", "1.mp3", Pending, added);
            var b = Add("f1", "2.mp3", Pending, added);
            var c = Add("f1", "3.mp3", Pending, added);

            CollectionAssert.AreEqual(new ITransferItem[] { a, b, c }, Sorted(TransferSortKey.LastAction, groupByFolder: false, descending));
            CollectionAssert.AreEqual(new ITransferItem[] { a, b, c }, Sorted(TransferSortKey.LastAction, groupByFolder: true, descending, Folder("f1")));
        }

        [Test]
        public void LastAction_IgnoresState()
        {
            Add("older", "1.mp3", Downloading);
            Add("newer", "1.mp3", Done);

            CollectionAssert.AreEqual(new[] { "newer", "older" }, SortedFolderNames(TransferSortKey.LastAction, descending: true));
        }

        // --- knowing when to re-sort ---

        private TransferListSnapshot Snapshot(TransferSortKey key, bool groupByFolder)
        {
            var keys = new Dictionary<ITransferItem, TransferBandKey>();
            var uiState = new TransferUIState { GroupByFolder = groupByFolder, SortKey = key };
            return new TransferListSnapshot(manager.GetUISnapshot(uiState, keys), keys, key);
        }

        [Test]
        public void Snapshot_State_FileRowChangesWithItsBandOrAnAction()
        {
            var a = Add("f1", "1.mp3", Pending);
            var snapshot = Snapshot(TransferSortKey.Status, groupByFolder: false);

            a.State = Downloading;
            Assert.IsFalse(snapshot.HasSortKeyChanged(a));

            a.State = Done;
            Assert.IsTrue(snapshot.HasSortKeyChanged(a));

            a.State = Pending;
            Assert.IsFalse(snapshot.HasSortKeyChanged(a));
            a.MarkUserAction(TransferBands.NextStamp());
            Assert.IsTrue(snapshot.HasSortKeyChanged(a));
        }

        [Test]
        public void Snapshot_State_FolderRowChangesOnlyWithTheFolder()
        {
            var a = Add("f1", "1.mp3", Downloading);
            var b = Add("f1", "2.mp3", TransferStates.Queued | TransferStates.Remotely);
            var snapshot = Snapshot(TransferSortKey.Status, groupByFolder: true);

            a.State = Done;
            Assert.IsFalse(snapshot.HasSortKeyChanged(a.Folder));
            Assert.IsFalse(snapshot.HasSortKeyChanged(a));

            b.State = Done;
            Assert.IsTrue(snapshot.HasSortKeyChanged(a.Folder));
        }

        [Test]
        public void Snapshot_LastAction_ChangesOnlyWithAnAction()
        {
            var a = Add("f1", "1.mp3", Pending);
            var snapshot = Snapshot(TransferSortKey.LastAction, groupByFolder: true);

            a.State = Done;
            Assert.IsFalse(snapshot.HasSortKeyChanged(a.Folder));

            a.MarkUserAction(TransferBands.NextStamp());
            Assert.IsTrue(snapshot.HasSortKeyChanged(a.Folder));
        }

        [Test]
        public void Snapshot_OtherSorts_NeverReportAChange()
        {
            var a = Add("f1", "1.mp3", Pending);
            var snapshot = new TransferListSnapshot(Sorted(TransferSortKey.DateAdded, groupByFolder: false));

            a.State = Done;
            a.MarkUserAction(TransferBands.NextStamp());

            Assert.IsFalse(snapshot.HasSortKeyChanged(a));
        }
    }
}
