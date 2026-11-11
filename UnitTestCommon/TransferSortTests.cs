using NUnit.Framework;
using Seeker;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferSortTests
    {
        private TransferItemManager manager;
        private CultureInfo previousCulture;

        [SetUp]
        public void Setup()
        {
            manager = new TransferItemManager(false);
            previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        private TransferItem Add(string username, string folder, string file)
        {
            var ti = new TransferItem
            {
                Username = username,
                FolderName = folder,
                Filename = file,
                FullFilename = @"\dir\" + folder + @"\" + file,
            };
            manager.Add(ti);
            return ti;
        }

        private static List<long> Seqs(IEnumerable<ITransferItem> items)
        {
            return items.Select(TransferSort.GetSeq).ToList();
        }

        private static void AssertAscending(IEnumerable<ITransferItem> items)
        {
            var seqs = Seqs(items);
            for (int i = 1; i < seqs.Count; i++)
            {
                Assert.Less(seqs[i - 1], seqs[i]);
            }
        }

        [Test]
        public void Add_AssignsIncreasingSeq_ToItemsAndFolders()
        {
            var a = Add("u1", "f1", "a.mp3");
            var b = Add("u1", "f2", "b.mp3");
            var c = Add("u1", "f1", "c.mp3");

            Assert.Less(a.Seq, b.Seq);
            Assert.Less(b.Seq, c.Seq);
            Assert.Less(manager.AllFolderItems[0].Seq, manager.AllFolderItems[1].Seq);
        }

        [Test]
        public void Seq_IsSharedAcrossManagers()
        {
            var uploads = new TransferItemManager(true);
            var a = Add("u1", "f1", "a.mp3");
            var up = new TransferItem { Username = "u2", FolderName = "f9", Filename = "x.mp3", FullFilename = @"\f9\x.mp3", isUpload = true };
            uploads.Add(up);
            var b = Add("u1", "f1", "b.mp3");

            Assert.Less(a.Seq, up.Seq);
            Assert.Less(up.Seq, b.Seq);
        }

        // the date added fast path copies storage order, so every list must stay in Seq order under concurrent adds
        [Test]
        public void ConcurrentAdds_KeepEveryListInSeqOrder()
        {
            Parallel.For(0, 2000, i =>
            {
                Add("u" + (i % 3), "folder" + (i % 7), "file" + i + ".mp3");
            });

            AssertAscending(manager.AllTransferItems);
            AssertAscending(manager.AllFolderItems);
            foreach (var folder in manager.AllFolderItems)
            {
                AssertAscending(folder.TransferItems);
            }
        }

        [Test]
        public void DateAdded_FastPathMatchesSort([Values(false, true)] bool groupByFolder, [Values(false, true)] bool descending)
        {
            Add("u2", "b", "2.mp3");
            Add("u1", "a", "1.mp3");
            Add("u2", "b", "3.mp3");
            Add("u3", "c", "4.mp3");
            var uiState = new TransferUIState { GroupByFolder = groupByFolder, SortKey = TransferSortKey.DateAdded, SortDescending = descending };

            var snapshot = manager.GetUISnapshot(uiState);

            var sorted = new List<ITransferItem>(snapshot);
            TransferSort.Sort(sorted, TransferSortKey.DateAdded, descending);
            CollectionAssert.AreEqual(sorted, snapshot);
            if (descending)
            {
                Assert.AreSame(groupByFolder ? (ITransferItem)manager.AllFolderItems.Last() : manager.AllTransferItems.Last(), snapshot[0]);
            }
        }

        [Test]
        public void DateAdded_InsideFolder_Descending()
        {
            var a = Add("u1", "f1", "a.mp3");
            var b = Add("u1", "f1", "b.mp3");
            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = manager.AllFolderItems[0], SortKey = TransferSortKey.DateAdded, SortDescending = true };

            CollectionAssert.AreEqual(new ITransferItem[] { b, a }, manager.GetUISnapshot(uiState));
        }

        [Test]
        public void FolderName_Folders_ByDisplayNameThenUser()
        {
            Add("zed", "Beta", "1.mp3");
            Add("amy", "alpha", "2.mp3");
            Add("amy", "Beta", "3.mp3");
            var uiState = new TransferUIState { GroupByFolder = true, SortKey = TransferSortKey.FolderName };

            var names = manager.GetUISnapshot(uiState).Cast<FolderItem>().Select(f => f.FolderName + "/" + f.Username).ToList();

            CollectionAssert.AreEqual(new[] { "alpha/amy", "Beta/amy", "Beta/zed" }, names);
        }

        // direction applies to the primary key only, ties stay ascending
        [Test]
        public void FolderName_Descending_ReversesFoldersOnly()
        {
            var a1 = Add("u1", "a", "1.mp3");
            var b2 = Add("u1", "b", "2.mp3");
            var a3 = Add("u1", "a", "3.mp3");
            var b1 = Add("u1", "b", "1.mp3");
            var uiState = new TransferUIState { GroupByFolder = false, SortKey = TransferSortKey.FolderName, SortDescending = true };

            CollectionAssert.AreEqual(new ITransferItem[] { b1, b2, a1, a3 }, manager.GetUISnapshot(uiState));
        }

        [Test]
        public void FolderName_InsideFolder_SortsByFileName()
        {
            var c = Add("u1", "f1", "c.mp3");
            var a = Add("u1", "f1", "A.mp3");
            var b = Add("u1", "f1", "b.mp3");
            var uiState = new TransferUIState { GroupByFolder = true, CurrentlySelectedFolder = manager.AllFolderItems[0], SortKey = TransferSortKey.FolderName };

            CollectionAssert.AreEqual(new ITransferItem[] { a, b, c }, manager.GetUISnapshot(uiState));
        }

        [Test]
        public void FolderName_NestedFolders_UseDisplayName()
        {
            // display name is reversed: "Album\Artist" -> "Artist\Album"
            Add("u1", @"Zulu\Artist", "1.mp3");
            Add("u1", @"Alpha\Band", "2.mp3");
            var uiState = new TransferUIState { GroupByFolder = true, SortKey = TransferSortKey.FolderName };

            var names = manager.GetUISnapshot(uiState).Cast<FolderItem>().Select(f => f.FolderName).ToList();

            CollectionAssert.AreEqual(new[] { @"Zulu\Artist", @"Alpha\Band" }, names);
        }

        [Test]
        public void User_TiesKeepDateAddedOrder([Values(false, true)] bool descending)
        {
            var b1 = Add("bob", "f1", "1.mp3");
            var a1 = Add("Alice", "f2", "1.mp3");
            var b2 = Add("bob", "f3", "2.mp3");
            var a2 = Add("Alice", "f4", "2.mp3");
            var uiState = new TransferUIState { GroupByFolder = false, SortKey = TransferSortKey.User, SortDescending = descending };

            var expected = descending ? new ITransferItem[] { b1, b2, a1, a2 } : new ITransferItem[] { a1, a2, b1, b2 };
            CollectionAssert.AreEqual(expected, manager.GetUISnapshot(uiState));
        }
    }
}
