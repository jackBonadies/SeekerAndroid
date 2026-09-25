using NUnit.Framework;
using Seeker;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTestCommon
{
    [TestFixture]
    public class UploadQueueTests
    {
        [Test]
        public void NotQueued_ReturnsNull()
        {
            var queue = new UploadQueue();
            queue.Enqueue("userA", @"music\a1.mp3");

            Assert.IsNull(queue.EstimatePosition("userA", @"music\other.mp3"));
            Assert.IsNull(queue.EstimatePosition("userB", @"music\a1.mp3"));
        }

        [Test]
        public void SingleUser_IsOneBasedEnqueueOrder()
        {
            var queue = new UploadQueue();
            for (int i = 1; i <= 10; i++)
            {
                queue.Enqueue("userA", "a" + i);
            }
            Assert.AreEqual(1, queue.EstimatePosition("userA", "a1"));
            Assert.AreEqual(2, queue.EstimatePosition("userA", "a2"));
            Assert.AreEqual(3, queue.EstimatePosition("userA", "a3"));
            Assert.AreEqual(10, queue.EstimatePosition("userA", "a10"));
        }

        [Test]
        public void Started_ReturnsNullAndNoLongerCountsAhead()
        {
            var queue = new UploadQueue();
            var a1 = queue.Enqueue("userA", "a1");
            queue.Enqueue("userA", "a2");

            Wait(queue, a1);

            Assert.IsNull(queue.EstimatePosition("userA", "a1"));
            Assert.AreEqual(1, queue.EstimatePosition("userA", "a2"));
        }

        [Test]
        public void Removed_ReturnsNullAndLaterFilesMoveUp()
        {
            var queue = new UploadQueue();
            var entries = new UploadQueue.Entry[10];
            for (int i = 0; i < 10; i++)
            {
                entries[i] = queue.Enqueue("userA", "a" + (i + 1));
            }
            for (int i = 0; i < 9; i++)
            {
                queue.Remove(entries[i]);
            }
            Assert.IsNull(queue.EstimatePosition("userA", "a1"));
            Assert.AreEqual(1, queue.EstimatePosition("userA", "a10"));
        }

        [Test]
        public void OtherUsers_RoundRobinWithMax()
        {
            var queue = new UploadQueue();
            for (int i = 1; i <= 5; i++)
            {
                queue.Enqueue("userC", "c" + i);
            }
            queue.Enqueue("userA", "a1");
            queue.Enqueue("userA", "a2");
            queue.Enqueue("userA", "a3");
            queue.Enqueue("userB", "b1");

            Assert.AreEqual(1, queue.EstimatePosition("userB", "b1"));
            Assert.AreEqual(1, queue.EstimatePosition("userA", "a1"));
            Assert.AreEqual(1 + 2 + 1 + 2, queue.EstimatePosition("userA", "a3"));
            Assert.AreEqual(1 + 2 + 2 + 1, queue.EstimatePosition("userC", "c3"));
            Assert.AreEqual(1 + 4 + 3 + 1, queue.EstimatePosition("userC", "c5"));
        }

        private static UploadQueue MixedTierQueue()
        {
            var queue = new UploadQueue(u => u.StartsWith("priv"));
            for (int i = 1; i <= 5; i++)
            {
                queue.Enqueue("userC", "c" + i);
            }
            queue.Enqueue("privX", "x1");
            queue.Enqueue("privX", "x2");
            queue.Enqueue("privX", "x3");
            queue.Enqueue("privY", "y1");
            queue.Enqueue("userA", "a1");
            return queue;
        }

        [Test]
        public void Privileged_RoundRobinAmongPrivilegedOnly()
        {
            var queue = MixedTierQueue();

            Assert.AreEqual(1, queue.EstimatePosition("privX", "x1"));
            Assert.AreEqual(1, queue.EstimatePosition("privY", "y1"));
            Assert.AreEqual(1 + 2 + 1, queue.EstimatePosition("privX", "x3"));
        }

        [Test]
        public void NonPrivileged_BehindEveryQueuedPrivileged()
        {
            var queue = MixedTierQueue();

            Assert.AreEqual(1 + 4, queue.EstimatePosition("userA", "a1"));
            // privileged files requested after c3 still go first
            Assert.AreEqual(1 + 4 + 2 + 1, queue.EstimatePosition("userC", "c3"));
        }

        [Test]
        public void StartedPrivileged_NoLongerAhead()
        {
            var queue = new UploadQueue(u => u.StartsWith("priv"));
            var x1 = queue.Enqueue("privX", "x1");
            queue.Enqueue("privX", "x2");
            queue.Enqueue("userA", "a1");

            Wait(queue, x1);

            Assert.AreEqual(1 + 1, queue.EstimatePosition("userA", "a1"));
        }

        [Test]
        public void GrantedSlot_StaysStartedAfterRelease()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            queue.Enqueue("userA", "a2");
            Wait(queue, a1);

            queue.ReleaseSlot(a1);

            Assert.IsNull(queue.EstimatePosition("userA", "a1"));
            Assert.AreEqual(1, queue.EstimatePosition("userA", "a2"));
        }

        private static Task Wait(UploadQueue queue, UploadQueue.Entry entry, CancellationToken token = default)
        {
            return queue.AwaitSlotAsync(entry, token);
        }

        [Test]
        public void Slots_GrantedUpToLimit()
        {
            var queue = new UploadQueue(slotLimit: 2);
            var a1 = Wait(queue, queue.Enqueue("userA", "a1"));
            var b1 = Wait(queue, queue.Enqueue("userB", "b1"));
            var c1 = Wait(queue, queue.Enqueue("userC", "c1"));

            Assert.IsTrue(a1.IsCompletedSuccessfully);
            Assert.IsTrue(b1.IsCompletedSuccessfully);
            Assert.IsFalse(c1.IsCompleted);
        }

        [Test]
        public void ReleaseSameSlotTwiceIsHarmless()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            Wait(queue, a1);
            var b1 = Wait(queue, queue.Enqueue("userB", "b1"));
            var c1 = Wait(queue, queue.Enqueue("userC", "c1"));

            queue.ReleaseSlot(a1);
            Assert.IsTrue(b1.IsCompletedSuccessfully);

            queue.ReleaseSlot(a1);
            Assert.IsFalse(c1.IsCompleted);
        }

        [Test]
        public void NonPrivileged_InWaitOrder_IsRoundRobin()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            var a2 = queue.Enqueue("userA", "a2");
            var b1 = queue.Enqueue("userB", "b1");
            var c1 = queue.Enqueue("userC", "c1");
            Wait(queue, a1);
            var b1Wait = Wait(queue, b1);
            // the library starts a user's next file waiting just before releasing the current one
            var a2Wait = Wait(queue, a2);
            var c1Wait = Wait(queue, c1);

            queue.ReleaseSlot(a1);
            Assert.IsTrue(b1Wait.IsCompletedSuccessfully);
            Assert.IsFalse(a2Wait.IsCompleted);

            queue.ReleaseSlot(b1);
            Assert.IsTrue(a2Wait.IsCompletedSuccessfully);
            Assert.IsFalse(c1Wait.IsCompleted);

            queue.ReleaseSlot(a2);
            Assert.IsTrue(c1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void Privileged_GoFirst_RoundRobinAmongThemselves()
        {
            var queue = new UploadQueue(u => u.StartsWith("priv"), slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            var b1 = queue.Enqueue("userB", "b1");
            var x1 = queue.Enqueue("privX", "x1");
            var x2 = queue.Enqueue("privX", "x2");
            var y1 = queue.Enqueue("privY", "y1");
            Wait(queue, a1);
            var b1Wait = Wait(queue, b1);
            var x1Wait = Wait(queue, x1);
            var y1Wait = Wait(queue, y1);

            queue.ReleaseSlot(a1);
            Assert.IsTrue(x1Wait.IsCompletedSuccessfully);
            Assert.IsFalse(b1Wait.IsCompleted);

            var x2Wait = Wait(queue, x2);
            queue.ReleaseSlot(x1);
            Assert.IsTrue(y1Wait.IsCompletedSuccessfully);

            queue.ReleaseSlot(y1);
            Assert.IsTrue(x2Wait.IsCompletedSuccessfully);
            Assert.IsFalse(b1Wait.IsCompleted);

            queue.ReleaseSlot(x2);
            Assert.IsTrue(b1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void Cancel_WhileWaiting_GivesUpTheTurn()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            Wait(queue, a1);
            var cts = new CancellationTokenSource();
            var b1Wait = Wait(queue, queue.Enqueue("userB", "b1"), cts.Token);
            var c1Wait = Wait(queue, queue.Enqueue("userC", "c1"));

            cts.Cancel();
            Assert.IsTrue(b1Wait.IsCanceled);

            queue.ReleaseSlot(a1);
            Assert.IsTrue(c1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void AlreadyCancelled_WhileSlotsFull_IsCancelled()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            Wait(queue, a1);

            var b1Wait = Wait(queue, queue.Enqueue("userB", "b1"), new CancellationToken(true));
            Assert.IsTrue(b1Wait.IsCanceled);

            var c1Wait = Wait(queue, queue.Enqueue("userC", "c1"));
            queue.ReleaseSlot(a1);
            Assert.IsTrue(c1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void Cancel_AfterGrant_KeepsSlotUntilReleased()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var cts = new CancellationTokenSource();
            var a1 = queue.Enqueue("userA", "a1");
            var a1Wait = Wait(queue, a1, cts.Token);
            var b1Wait = Wait(queue, queue.Enqueue("userB", "b1"));

            cts.Cancel();
            Assert.IsTrue(a1Wait.IsCompletedSuccessfully);
            Assert.IsFalse(b1Wait.IsCompleted);

            queue.ReleaseSlot(a1);
            Assert.IsTrue(b1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void SlotLimit_Raised_GrantsWaiters()
        {
            var queue = new UploadQueue(slotLimit: 1);
            Wait(queue, queue.Enqueue("userA", "a1"));
            var b1Wait = Wait(queue, queue.Enqueue("userB", "b1"));
            var c1Wait = Wait(queue, queue.Enqueue("userC", "c1"));

            queue.SlotLimit = 3;
            Assert.IsTrue(b1Wait.IsCompletedSuccessfully);
            Assert.IsTrue(c1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void SlotLimit_Lowered_DoesNotRevoke()
        {
            var queue = new UploadQueue(slotLimit: 2);
            var a1 = queue.Enqueue("userA", "a1");
            var b1 = queue.Enqueue("userB", "b1");
            var a1Wait = Wait(queue, a1);
            var b1Wait = Wait(queue, b1);
            var c1Wait = Wait(queue, queue.Enqueue("userC", "c1"));

            queue.SlotLimit = 1;
            Assert.IsTrue(a1Wait.IsCompletedSuccessfully);
            Assert.IsTrue(b1Wait.IsCompletedSuccessfully);

            queue.ReleaseSlot(a1);
            Assert.IsFalse(c1Wait.IsCompleted);

            queue.ReleaseSlot(b1);
            Assert.IsTrue(c1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void Remove_ReleasesHeldSlot_AndCancelsWait()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            var b1 = queue.Enqueue("userB", "b1");
            Wait(queue, a1);
            var b1Wait = Wait(queue, b1);
            var c1Wait = Wait(queue, queue.Enqueue("userC", "c1"));

            queue.Remove(b1);
            Assert.IsTrue(b1Wait.IsCanceled);

            queue.Remove(a1);
            Assert.IsTrue(c1Wait.IsCompletedSuccessfully);
        }

        [Test]
        public void AwaitSlot_Twice_Throws()
        {
            var queue = new UploadQueue(slotLimit: 1);
            var a1 = queue.Enqueue("userA", "a1");
            Wait(queue, a1);

            Assert.Throws<System.InvalidOperationException>(() => Wait(queue, a1));
        }

        [Test]
        public void HasFreeSlot_FollowsUsedSlots()
        {
            var queue = new UploadQueue(slotLimit: 2);
            Assert.IsTrue(queue.HasFreeSlot);

            var a1 = queue.Enqueue("userA", "a1");
            Wait(queue, a1);
            Assert.IsTrue(queue.HasFreeSlot);

            Wait(queue, queue.Enqueue("userB", "b1"));
            Wait(queue, queue.Enqueue("userC", "c1"));
            Assert.IsFalse(queue.HasFreeSlot);
            Assert.AreEqual(2, queue.UsedSlots);

            // c1 takes the released slot
            queue.ReleaseSlot(a1);
            Assert.IsFalse(queue.HasFreeSlot);

            queue.SlotLimit = int.MaxValue;
            Assert.IsTrue(queue.HasFreeSlot);
        }

        [Test]
        public void QueuedCount_PrivilegedRequesterCountsOnlyPrivileged()
        {
            var queue = MixedTierQueue();

            Assert.AreEqual(10, queue.QueuedCount("userB"));
            Assert.AreEqual(4, queue.QueuedCount("privZ"));
        }

        [Test]
        public void QueuedCount_SkipsStarted()
        {
            var queue = new UploadQueue(u => u.StartsWith("priv"));
            var x1 = queue.Enqueue("privX", "x1");
            queue.Enqueue("privX", "x2");
            var a1 = queue.Enqueue("userA", "a1");
            queue.Enqueue("userA", "a2");

            Wait(queue, x1);
            Wait(queue, a1);

            Assert.AreEqual(2, queue.QueuedCount("userB"));
            Assert.AreEqual(1, queue.QueuedCount("privZ"));
        }
    }
}
