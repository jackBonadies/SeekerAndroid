using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTestCommon
{
    [TestFixture]
    public class DownloadUserSlotsTests
    {
        private DateTime now;
        private int limit;

        private DownloadUserSlotsLimiter Create()
        {
            return new DownloadUserSlotsLimiter(() => limit, TimeSpan.FromSeconds(20), () => now, useTimer: false);
        }

        [SetUp]
        public void SetUp()
        {
            now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
            limit = 1;
        }

        private void Advance(double seconds)
        {
            now = now.AddSeconds(seconds);
        }

        [Test]
        public void SecondUser_WaitsUntilFirstHasNothingLeft()
        {
            var slots = Create();
            Assert.IsTrue(slots.AcquireBatchAsync("a").IsCompleted);
            var a1 = slots.Track("a");
            a1.Update(TransferStates.Queued | TransferStates.Locally);

            var b = slots.AcquireBatchAsync("b");
            Assert.IsFalse(b.IsCompleted);

            a1.Update(TransferStates.Initializing);
            a1.Update(TransferStates.InProgress);
            slots.ReleaseBatch("a");
            Assert.IsFalse(b.IsCompleted);

            // nothing left for a, released at once, no grace
            a1.Update(TransferStates.Completed | TransferStates.Succeeded);
            Assert.IsTrue(b.IsCompleted);
        }

        [Test]
        public void QueuedPastGrace_GivesUpSlot()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var a1 = slots.Track("a");
            a1.Update(TransferStates.Queued | TransferStates.Locally);
            a1.Update(TransferStates.Requested);
            a1.Update(TransferStates.Queued | TransferStates.Remotely);
            slots.ReleaseBatch("a");

            var b = slots.AcquireBatchAsync("b");
            Advance(19);
            slots.AdmitWaiters();
            Assert.IsFalse(b.IsCompleted);

            Advance(2);
            slots.AdmitWaiters();
            Assert.IsTrue(b.IsCompleted);
        }

        [Test]
        public void QueuedThenOfferedWithinGrace_KeepsSlot()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var a1 = slots.Track("a");
            a1.Update(TransferStates.Queued | TransferStates.Remotely);
            slots.ReleaseBatch("a");

            var b = slots.AcquireBatchAsync("b");
            Advance(1);
            a1.Update(TransferStates.Initializing);
            Advance(60);
            slots.AdmitWaiters();
            Assert.IsFalse(b.IsCompleted);
        }

        [Test]
        public void GapBetweenFiles_KeepsSlot()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var a1 = slots.Track("a");
            var a2 = slots.Track("a");
            a1.Update(TransferStates.InProgress);
            a2.Update(TransferStates.Queued | TransferStates.Remotely);
            slots.ReleaseBatch("a");

            var b = slots.AcquireBatchAsync("b");
            a1.Update(TransferStates.Completed | TransferStates.Succeeded);
            Advance(1);
            slots.AdmitWaiters();
            Assert.IsFalse(b.IsCompleted);
            a2.Update(TransferStates.Initializing);
            Advance(60);
            slots.AdmitWaiters();
            Assert.IsFalse(b.IsCompleted);
        }

        [Test]
        public void DormantUserOffered_GoesOverLimit_AndBlocksNewUsers()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var a1 = slots.Track("a");
            a1.Update(TransferStates.Queued | TransferStates.Remotely);
            slots.ReleaseBatch("a");
            Advance(21);

            Assert.IsTrue(slots.AcquireBatchAsync("b").IsCompleted);
            var b1 = slots.Track("b");
            b1.Update(TransferStates.InProgress);

            // a's turn comes, accepted
            a1.Update(TransferStates.Initializing);
            Assert.AreEqual(2, slots.CountedUsersForTest());

            var c = slots.AcquireBatchAsync("c");
            Assert.IsFalse(c.IsCompleted);
            a1.Update(TransferStates.Completed | TransferStates.Succeeded);
            Assert.IsFalse(c.IsCompleted);
            b1.Update(TransferStates.Completed | TransferStates.Succeeded);
            slots.ReleaseBatch("b");
            Assert.IsTrue(c.IsCompleted);
        }

        [Test]
        public void UserHoldingSlot_IsAdmittedAheadOfWaiters()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var b = slots.AcquireBatchAsync("b");
            Assert.IsTrue(slots.AcquireBatchAsync("a").IsCompleted);
            Assert.IsFalse(b.IsCompleted);
            Assert.AreEqual(1, slots.CountedUsersForTest());
        }

        [Test]
        public void Waiters_AreAdmittedInOrder()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var b = slots.AcquireBatchAsync("b");
            var c = slots.AcquireBatchAsync("c");

            slots.ReleaseBatch("a");
            Assert.IsTrue(b.IsCompleted);
            Assert.IsFalse(c.IsCompleted);
            slots.ReleaseBatch("b");
            Assert.IsTrue(c.IsCompleted);
        }

        [Test]
        public void RaisingLimit_AdmitsOnPump()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var b = slots.AcquireBatchAsync("b");
            var c = slots.AcquireBatchAsync("c");
            limit = 2;
            slots.AdmitWaiters();
            Assert.IsTrue(b.IsCompleted);
            Assert.IsFalse(c.IsCompleted);
            limit = int.MaxValue;
            slots.AdmitWaiters();
            Assert.IsTrue(c.IsCompleted);
        }

        [Test]
        public void CancelledWait_IsWithdrawn()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var cts = new CancellationTokenSource();
            var b = slots.AcquireBatchAsync("b", cts.Token);
            var c = slots.AcquireBatchAsync("c");
            cts.Cancel();
            Assert.ThrowsAsync<TaskCanceledException>(async () => await b);
            Assert.AreEqual(1, slots.WaitingForTest());

            slots.ReleaseBatch("a");
            Assert.IsTrue(c.IsCompleted);
            Assert.AreEqual(1, slots.CountedUsersForTest());
        }

        [Test]
        public void AlreadyCancelled_DoesNotWait()
        {
            var slots = Create();
            var b = slots.AcquireBatchAsync("b", new CancellationToken(true));
            Assert.IsTrue(b.IsCanceled);
            Assert.AreEqual(0, slots.CountedUsersForTest());
        }

        [Test]
        public void Finish_IsIdempotent_AndLateUpdatesIgnored()
        {
            var slots = Create();
            slots.AcquireBatchAsync("a");
            var a1 = slots.Track("a");
            a1.Update(TransferStates.InProgress);
            slots.ReleaseBatch("a");
            a1.Finish();
            a1.Finish();
            a1.Update(TransferStates.InProgress);
            Assert.AreEqual(0, slots.CountedUsersForTest());
            Assert.IsTrue(slots.AcquireBatchAsync("b").IsCompleted);
        }

        [Test]
        public void NoLimit_NeverWaits()
        {
            limit = int.MaxValue;
            var slots = Create();
            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(slots.AcquireBatchAsync("u" + i).IsCompleted);
            }
        }

        [Test]
        public void InfiniteGrace_QueuedUserKeepsSlotUntilQueueDrains()
        {
            var slots = new DownloadUserSlotsLimiter(() => limit, DownloadUserSlotsLimiter.InfiniteGracePeriod, () => now, useTimer: false);
            slots.AcquireBatchAsync("a");
            var a1 = slots.Track("a");
            a1.Update(TransferStates.Queued | TransferStates.Remotely);
            slots.ReleaseBatch("a");

            var b = slots.AcquireBatchAsync("b");
            Advance(TimeSpan.FromDays(3650).TotalSeconds);
            slots.AdmitWaiters();
            Assert.IsFalse(b.IsCompleted);

            a1.Update(TransferStates.Initializing);
            a1.Update(TransferStates.Completed | TransferStates.Succeeded);
            Assert.IsTrue(b.IsCompleted);
        }

        [Test]
        public void InfiniteGrace_WithTimer_DoesNotThrow()
        {
            var slots = new DownloadUserSlotsLimiter(() => limit, DownloadUserSlotsLimiter.InfiniteGracePeriod, () => now);
            slots.AcquireBatchAsync("a");
            slots.Track("a").Update(TransferStates.Queued | TransferStates.Remotely);
            slots.ReleaseBatch("a");
            Assert.IsFalse(slots.AcquireBatchAsync("b").IsCompleted);
        }

        [Test]
        public void NegativeGrace_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DownloadUserSlotsLimiter(() => limit, TimeSpan.FromSeconds(-1)));
        }
    }
}
