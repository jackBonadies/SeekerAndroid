using NUnit.Framework;
using Seeker;

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

            queue.MarkStarted(a1);

            Assert.IsNull(queue.EstimatePosition("userA", "a1"));
            Assert.AreEqual(1, queue.EstimatePosition("userA", "a2"));
        }

        [Test]
        public void Removed_ReturnsNullAndLaterFilesMoveUp()
        {
            var queue = new UploadQueue();
            for (int i = 1; i <= 10; i++)
            {
                queue.Enqueue("userA", "a" + i);
            }
            for (int i = 1; i <= 9; i++)
            {
                queue.Enqueue("userA", "a" + i);
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
    }
}
