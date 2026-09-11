using Common.Search;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    [TestFixture]
    public class InsertOnlyDiffTests
    {
        private static List<object> Items(int n)
        {
            return Enumerable.Range(0, n).Select(_ => new object()).ToList();
        }

        private static List<object> Apply(List<object> oldList, List<(int Start, int Count)> runs, List<object> newList)
        {
            var result = oldList.ToList();
            foreach (var (start, count) in runs)
            {
                result.InsertRange(start, newList.GetRange(start, count));
            }
            return result;
        }

        [Test]
        public void BothEmpty_NoRuns()
        {
            Assert.IsTrue(InsertOnlyDiff.TryComputeInsertRuns(new List<object>(), new List<object>(), out var runs));
            Assert.IsEmpty(runs);
        }

        [Test]
        public void Identical_NoRuns()
        {
            var a = Items(5);
            Assert.IsTrue(InsertOnlyDiff.TryComputeInsertRuns(a, a.ToList(), out var runs));
            Assert.IsEmpty(runs);
        }

        [Test]
        public void EmptyOld_SingleRunOfEverything()
        {
            var b = Items(7);
            Assert.IsTrue(InsertOnlyDiff.TryComputeInsertRuns(new List<object>(), b, out var runs));
            Assert.AreEqual(new[] { (0, 7) }, runs.ToArray());
        }

        [Test]
        public void InsertsAtFrontMiddleEnd_AdjacentInsertsCoalesce()
        {
            var a = Items(4); // a0 a1 a2 a3
            var x = Items(5);
            var b = new List<object> { x[0], x[1], a[0], a[1], x[2], a[2], a[3], x[3], x[4] };

            Assert.IsTrue(InsertOnlyDiff.TryComputeInsertRuns(a, b, out var runs));
            Assert.AreEqual(new[] { (0, 2), (4, 1), (7, 2) }, runs.ToArray());
            Assert.AreEqual(b, Apply(a, runs, b));
        }

        [Test]
        public void Interleaved_OneRunPerInsert()
        {
            var a = Items(3);
            var x = Items(3);
            var b = new List<object> { a[0], x[0], a[1], x[1], a[2], x[2] };

            Assert.IsTrue(InsertOnlyDiff.TryComputeInsertRuns(a, b, out var runs));
            Assert.AreEqual(new[] { (1, 1), (3, 1), (5, 1) }, runs.ToArray());
            Assert.AreEqual(b, Apply(a, runs, b));
        }

        [Test]
        public void Removal_Fails()
        {
            var a = Items(4);
            var b = new List<object> { a[0], a[2], a[3], new object(), new object() };

            Assert.IsFalse(InsertOnlyDiff.TryComputeInsertRuns(a, b, out var runs));
            Assert.IsNull(runs);
        }

        [Test]
        public void Reorder_Fails()
        {
            var a = Items(3);
            var b = new List<object> { a[1], a[0], a[2] };

            Assert.IsFalse(InsertOnlyDiff.TryComputeInsertRuns(a, b, out var runs));
            Assert.IsNull(runs);
        }

        [Test]
        public void Shorter_Fails()
        {
            var a = Items(3);
            Assert.IsFalse(InsertOnlyDiff.TryComputeInsertRuns(a, a.Take(2).ToList(), out var runs));
            Assert.IsNull(runs);
        }

        [Test]
        public void MatchesByReferenceNotEquality()
        {
            var a = new List<string> { new string('x', 3) };
            var b = new List<string> { new string('x', 3), a[0] };

            Assert.IsTrue(InsertOnlyDiff.TryComputeInsertRuns(a, b, out var runs));
            Assert.AreEqual(new[] { (0, 1) }, runs.ToArray());
        }

        [Test]
        public void RandomizedInsertsRoundTrip()
        {
            var rng = new System.Random(1234);
            for (int iter = 0; iter < 200; iter++)
            {
                var a = Items(rng.Next(0, 30));
                var b = a.ToList();
                int inserts = rng.Next(0, 20);
                for (int k = 0; k < inserts; k++)
                {
                    b.Insert(rng.Next(0, b.Count + 1), new object());
                }

                Assert.IsTrue(InsertOnlyDiff.TryComputeInsertRuns(a, b, out var runs));
                Assert.AreEqual(inserts, runs.Sum(r => r.Count));
                Assert.AreEqual(b, Apply(a, runs, b));
            }
        }
    }
}
