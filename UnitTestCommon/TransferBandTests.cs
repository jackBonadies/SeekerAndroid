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

        private TransferItemManager manager;

        [SetUp]
        public void Setup()
        {
            manager = new TransferItemManager(false);
        }

        private TransferItem Add(string folder, string file, TransferStates state, long stamp = 0)
        {
            var ti = new TransferItem
            {
                Username = "u1",
                FolderName = folder,
                Filename = file,
                FullFilename = @"\dir\" + folder + @"\" + file,
            };
            ti.HintBandStamp(stamp);
            ti.State = state;
            manager.Add(ti);
            return ti;
        }

        private static void Move(TransferItem ti, TransferStates state, long stamp = 0)
        {
            ti.HintBandStamp(stamp);
            ti.State = state;
        }

        private FolderItem Folder(string name)
        {
            return manager.AllFolderItems.First(f => f.FolderName == name);
        }

        private List<ITransferItem> Sorted(bool groupByFolder, bool descending = false, FolderItem inFolder = null)
        {
            return manager.GetUISnapshot(new TransferUIState
            {
                GroupByFolder = groupByFolder,
                CurrentlySelectedFolder = inFolder,
                SortKey = TransferSortKey.State,
                SortDescending = descending,
            });
        }

        // --- band of a state, one case per status chip branch ---

        [TestCase(TransferStates.None, TransferBand.Unset)]
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

            TransferBands.Observe(future);

            Assert.Greater(TransferBands.NextStamp(), future);
        }

        // --- stamping ---

        [Test]
        public void State_StampsOnlyWhenTheBandChanges()
        {
            var ti = new TransferItem();
            ti.State = Pending;
            long entered = ti.BandStamp;
            Assert.AreNotEqual(0, entered);

            ti.State = TransferStates.Requested;
            ti.State = TransferStates.Queued | TransferStates.Remotely;
            ti.State = TransferStates.Initializing;
            ti.State = TransferStates.InProgress;
            Assert.AreEqual(entered, ti.BandStamp);

            ti.State = Done;
            Assert.Greater(ti.BandStamp, entered);
        }

        [Test]
        public void State_AddingAFlagInsideTheBandDoesNotStamp()
        {
            var ti = new TransferItem();
            ti.State = Errored;
            long entered = ti.BandStamp;

            ti.State |= TransferStates.UserOffline;

            Assert.AreEqual(entered, ti.BandStamp);
        }

        [Test]
        public void State_FirstWriteStampsEvenWhenQueuedPaused()
        {
            var ti = new TransferItem();

            ti.State = TransferStates.Cancelled;

            Assert.AreNotEqual(0, ti.BandStamp);
        }

        [Test]
        public void Hint_IsUsedForTheNextBandChangeOnly()
        {
            var ti = new TransferItem();
            ti.State = Pending;

            ti.HintBandStamp(42);
            ti.State = TransferStates.Requested;
            Assert.AreNotEqual(42, ti.BandStamp);
            ti.State = Paused;
            Assert.AreEqual(42, ti.BandStamp);

            ti.State = Pending;
            Assert.AreNotEqual(42, ti.BandStamp);
        }

        [Test]
        public void Hint_OnlyIfActive_SkipsRowsACancelWillNotMove()
        {
            var ti = new TransferItem();
            ti.State = Done;

            ti.HintBandStamp(42, onlyIfActive: true);
            ti.State = Pending;

            Assert.AreNotEqual(42, ti.BandStamp);
        }

        [Test]
        public void PersistedState_NeverStamps()
        {
            var ti = new TransferItem();
            ti.State = TransferStates.InProgress;
            long entered = ti.BandStamp;

            ti.PersistedState = TransferStates.Cancelled;

            Assert.AreEqual(TransferStates.Cancelled, ti.State);
            Assert.AreEqual(entered, ti.BandStamp);
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
        public void Xml_KeepsTheStateElementAndTheStamp()
        {
            var ti = new TransferItem { Username = "u1", FullFilename = @"\a\b.mp3" };
            ti.State = Done;

            string xml = Serialize(new List<TransferItem> { ti });
            var restored = Deserialize(xml).Single();

            StringAssert.Contains("<State>Completed Succeeded</State>", xml);
            StringAssert.DoesNotContain("PersistedState", xml);
            StringAssert.DoesNotContain("BandStampHint", xml);
            Assert.AreEqual(Done, restored.State);
            Assert.AreEqual(ti.BandStamp, restored.BandStamp);
        }

        // the element order of a file written before the stamp existed (State was a field between Failed and Size)
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
            Assert.AreEqual(0, restored.BandStamp);
        }

        [Test]
        public void Relaunch_PausesActiveRowsWithoutRestamping()
        {
            var active = Add("f1", "1.mp3", TransferStates.InProgress);
            var done = Add("f1", "2.mp3", Done);
            long activeStamp = active.BandStamp;
            long doneStamp = done.BandStamp;

            manager.OnRelaunch();

            Assert.AreEqual(TransferStates.Cancelled, active.State);
            Assert.AreEqual(activeStamp, active.BandStamp);
            Assert.AreEqual(doneStamp, done.BandStamp);
        }

        // --- sorting files ---

        [Test]
        public void Files_SortByBand([Values(false, true)] bool descending)
        {
            var done = Add("f1", "1.mp3", Done);
            var paused = Add("f1", "2.mp3", Paused);
            var failed = Add("f1", "3.mp3", Errored);
            var active = Add("f1", "4.mp3", TransferStates.InProgress);
            var queued = Add("f1", "5.mp3", TransferStates.Queued | TransferStates.Remotely, active.BandStamp);

            var expected = descending
                ? new ITransferItem[] { done, paused, failed, active, queued }
                : new ITransferItem[] { active, queued, failed, paused, done };
            CollectionAssert.AreEqual(expected, Sorted(groupByFolder: false, descending));
            CollectionAssert.AreEqual(expected, Sorted(groupByFolder: true, descending, Folder("f1")));
        }

        [Test]
        public void Files_NewestInABandIsFirst_BothDirections([Values(false, true)] bool descending)
        {
            var first = Add("f1", "1.mp3", TransferStates.InProgress);
            var second = Add("f1", "2.mp3", TransferStates.InProgress);
            var third = Add("f1", "3.mp3", TransferStates.InProgress);

            // the one added first finishes last
            Move(second, Done);
            Move(third, Done);
            Move(first, Done);

            CollectionAssert.AreEqual(new ITransferItem[] { first, third, second }, Sorted(groupByFolder: false, descending));
        }

        [Test]
        public void Files_ABatchSharingAStampKeepsTrackOrder()
        {
            long added = TransferBands.NextStamp();
            var a = Add("f1", "1.mp3", Pending, added);
            var b = Add("f1", "2.mp3", Pending, added);
            var c = Add("f1", "3.mp3", Pending, added);
            long paused = TransferBands.NextStamp();
            Move(c, Paused, paused);
            Move(a, Paused, paused);
            Move(b, Paused, paused);

            CollectionAssert.AreEqual(new ITransferItem[] { a, b, c }, Sorted(groupByFolder: false));
        }

        [Test]
        public void Files_WithoutAStampFallBackToDateAdded()
        {
            var a = Add("f1", "1.mp3", TransferStates.None);
            var b = Add("f1", "2.mp3", TransferStates.None);
            a.PersistedState = Done;
            b.PersistedState = Done;
            var stamped = Add("f1", "3.mp3", Done);

            CollectionAssert.AreEqual(new ITransferItem[] { stamped, a, b }, Sorted(groupByFolder: false));
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
                new[] { Errored, TransferStates.InProgress },
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
        public void Folder_StaysPutWhileItsFilesFinishFailAndRetry()
        {
            long added = TransferBands.NextStamp();
            var a = Add("f1", "1.mp3", Pending, added);
            var b = Add("f1", "2.mp3", Pending, added);
            var c = Add("f1", "3.mp3", Pending, added);
            var folder = Folder("f1");
            Assert.AreEqual(TransferBand.Active, folder.GetBand(out long entered));
            Assert.AreEqual(added, entered);

            Move(a, Done);
            Move(b, Errored);
            Assert.AreEqual(TransferBand.Active, folder.GetBand(out long afterFailure));
            Move(b, Pending);
            Assert.AreEqual(TransferBand.Active, folder.GetBand(out long afterRetry));

            Assert.AreEqual(added, afterFailure);
            Assert.AreEqual(added, afterRetry);
            Assert.Greater(b.BandStamp, added);
            Assert.AreNotEqual(0, c.BandStamp);
        }

        [Test]
        public void Folder_TakesTheStampOfWhatMovedItOutOfActive()
        {
            var a = Add("f1", "1.mp3", TransferStates.InProgress);
            var b = Add("f1", "2.mp3", TransferStates.InProgress);
            var folder = Folder("f1");
            folder.GetBand(out _);

            Move(a, Done);
            Move(b, Done);

            Assert.AreEqual(TransferBand.Completed, folder.GetBand(out long stamp));
            Assert.AreEqual(b.BandStamp, stamp);
        }

        [Test]
        public void Folder_PausedAsABatchTakesTheSharedStamp()
        {
            var a = Add("f1", "1.mp3", TransferStates.InProgress);
            var b = Add("f1", "2.mp3", TransferStates.Queued | TransferStates.Remotely);
            var done = Add("f1", "3.mp3", Done);
            var folder = Folder("f1");
            folder.GetBand(out _);
            long pauseAll = TransferBands.NextStamp();

            foreach (var ti in new[] { a, b, done })
            {
                ti.HintBandStamp(pauseAll, onlyIfActive: true);
            }
            Move(b, Paused);
            Move(a, Paused);

            Assert.AreEqual(TransferBand.Paused, folder.GetBand(out long stamp));
            Assert.AreEqual(pauseAll, stamp);
            Assert.AreEqual(0, done.BandStampHint);
        }

        [Test]
        public void Folder_ResumingIntoActiveTakesTheResumeStamp()
        {
            var a = Add("f1", "1.mp3", Paused);
            var b = Add("f1", "2.mp3", Done);
            var folder = Folder("f1");
            folder.GetBand(out _);
            long resume = TransferBands.NextStamp();

            Move(a, Pending, resume);

            Assert.AreEqual(TransferBand.Active, folder.GetBand(out long stamp));
            Assert.AreEqual(resume, stamp);
            Assert.Less(b.BandStamp, resume);
        }

        [Test]
        public void Folders_SortByBandThenNewestFirst([Values(false, true)] bool descending)
        {
            Add("done-old", "1.mp3", Done);
            Add("active", "1.mp3", TransferStates.InProgress);
            Add("active", "2.mp3", Done);
            Add("paused", "1.mp3", Paused);
            Add("failed", "1.mp3", Errored);
            Add("failed", "2.mp3", Done);
            Add("done-new", "1.mp3", Done);

            var names = Sorted(groupByFolder: true, descending).Cast<FolderItem>().Select(f => f.FolderName).ToList();

            var expected = descending
                ? new[] { "done-new", "done-old", "paused", "failed", "active" }
                : new[] { "active", "failed", "paused", "done-new", "done-old" };
            CollectionAssert.AreEqual(expected, names);
        }

        // --- knowing when to re-sort ---

        private TransferListSnapshot Snapshot(bool groupByFolder)
        {
            var keys = new Dictionary<ITransferItem, TransferBandKey>();
            var uiState = new TransferUIState { GroupByFolder = groupByFolder, SortKey = TransferSortKey.State };
            return new TransferListSnapshot(manager.GetUISnapshot(uiState, keys), keys);
        }

        [Test]
        public void Snapshot_FileRow_ChangedOnlyByABandChange()
        {
            var a = Add("f1", "1.mp3", Pending);
            var snapshot = Snapshot(groupByFolder: false);

            Move(a, TransferStates.InProgress);
            Assert.IsFalse(snapshot.HasBandChanged(a));

            Move(a, Done);
            Assert.IsTrue(snapshot.HasBandChanged(a));
            Assert.IsFalse(snapshot.HasBandChanged(a.Folder));
        }

        [Test]
        public void Snapshot_FolderRow_ChangedOnlyWhenTheFolderChangesBand()
        {
            var a = Add("f1", "1.mp3", TransferStates.InProgress);
            var b = Add("f1", "2.mp3", TransferStates.Queued | TransferStates.Remotely);
            var snapshot = Snapshot(groupByFolder: true);

            Move(a, Done);
            Assert.IsFalse(snapshot.HasBandChanged(a.Folder));
            Assert.IsFalse(snapshot.HasBandChanged(a));

            Move(b, Done);
            Assert.IsTrue(snapshot.HasBandChanged(a.Folder));
        }

        [Test]
        public void Snapshot_OtherSorts_NeverReportAChange()
        {
            var a = Add("f1", "1.mp3", Pending);
            var uiState = new TransferUIState { GroupByFolder = false, SortKey = TransferSortKey.DateAdded };
            var snapshot = new TransferListSnapshot(manager.GetUISnapshot(uiState));

            Move(a, Done);

            Assert.IsFalse(snapshot.HasBandChanged(a));
        }
    }
}
