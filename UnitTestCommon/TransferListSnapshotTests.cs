using NUnit.Framework;
using Seeker;
using System.Collections.Generic;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferListSnapshotTests
    {
        private static TransferItem Item(string name)
        {
            return new TransferItem { Username = "user1", FullFilename = @"\dir\folder1\" + name, FolderName = "folder1", Filename = name };
        }

        [Test]
        public void IndexOf_FindsByIdentity()
        {
            var a = Item("a.mp3");
            var b = Item("b.mp3");
            var lookalike = Item("b.mp3");
            var snapshot = new TransferListSnapshot(new List<ITransferItem> { a, b });

            Assert.AreEqual(0, snapshot.IndexOf(a));
            Assert.AreEqual(1, snapshot.IndexOf(b));
            Assert.AreEqual(-1, snapshot.IndexOf(lookalike));
            Assert.AreEqual(-1, snapshot.IndexOf(null));
        }

        [Test]
        public void GetItem_OutOfRangeIsNull()
        {
            var a = Item("a.mp3");
            var snapshot = new TransferListSnapshot(new List<ITransferItem> { a });

            Assert.AreSame(a, snapshot.GetItem(0));
            Assert.IsNull(snapshot.GetItem(1));
            Assert.IsNull(snapshot.GetItem(-1));
        }

        [Test]
        public void RemoveAt_ShiftsLaterPositions()
        {
            var a = Item("a.mp3");
            var b = Item("b.mp3");
            var c = Item("c.mp3");
            var snapshot = new TransferListSnapshot(new List<ITransferItem> { a, b, c });
            Assert.AreEqual(2, snapshot.IndexOf(c));

            snapshot.RemoveAt(snapshot.IndexOf(a));

            Assert.AreEqual(2, snapshot.Count);
            Assert.AreEqual(-1, snapshot.IndexOf(a));
            Assert.AreEqual(0, snapshot.IndexOf(b));
            Assert.AreEqual(1, snapshot.IndexOf(c));
        }
    }
}
