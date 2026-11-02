using Common.Transfers;
using NUnit.Framework;
using Seeker;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferRateLimiterTests
    {
        private class FakeClock
        {
            public long Now;
            public readonly List<double> Waits = new List<double>();

            public Task Delay(TimeSpan wait, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                Waits.Add(wait.TotalMilliseconds);
                Now += (long)Math.Ceiling(wait.TotalMilliseconds);
                return Task.CompletedTask;
            }
        }

        private static TransferRateLimiter Create(FakeClock clock, Func<int> rate) =>
            new TransferRateLimiter(rate, () => clock.Now, 1000, clock.Delay);

        private static long Transfer(TransferRateLimiter limiter, FakeClock clock, long simulatedMs, int chunk = 16384, long readMs = 0)
        {
            long bytes = 0;
            long end = clock.Now + simulatedMs;
            while (clock.Now < end)
            {
                int grant = limiter.GetAsync(chunk, CancellationToken.None).GetAwaiter().GetResult();
                bytes += Math.Min(chunk, grant);
                clock.Now += readMs;
            }
            return bytes;
        }

        [TestCase(4 * 1024)]
        [TestCase(100 * 1024)]
        [TestCase(256 * 1024)]
        [TestCase(1024 * 1024)]
        [TestCase(4 * 1024 * 1024)]
        public void HoldsTheConfiguredRate(int rate)
        {
            var clock = new FakeClock();
            var limiter = Create(clock, () => rate);
            long bytes = Transfer(limiter, clock, 60_000);
            double actual = bytes / 60.0;
            // one bucket (100 ms) of burst at the start, plus rounding of each wait to whole ms
            Assert.That(actual, Is.EqualTo(rate).Within(2).Percent, $"got {actual:F0} B/s");
        }

        [Test]
        public void SlowerNetworkIsNotThrottledFurther()
        {
            var clock = new FakeClock();
            var limiter = Create(clock, () => 1024 * 1024);
            // 16 KB every 100 ms = 160 KB/s, well under the 1 MB/s limit
            long bytes = Transfer(limiter, clock, 10_000, readMs: 100);
            Assert.That(bytes / 10.0, Is.EqualTo(163840).Within(2).Percent);
            Assert.That(clock.Waits, Is.Empty);
        }

        [Test]
        public void NoWaitIsLongerThanOneBucket()
        {
            var clock = new FakeClock();
            var limiter = Create(clock, () => 1024);
            Transfer(limiter, clock, 30_000);
            Assert.That(clock.Waits, Is.Not.Empty);
            Assert.That(clock.Waits, Has.All.LessThanOrEqualTo(100.0 + 1));
        }

        [Test]
        public void UnlimitedGrantsEverythingWithoutWaiting()
        {
            var clock = new FakeClock();
            var limiter = Create(clock, () => 0);
            Assert.AreEqual(int.MaxValue, limiter.GetAsync(16384, CancellationToken.None).Result);
            Assert.That(clock.Waits, Is.Empty);
        }

        [Test]
        public void RateChangesApplyToTheNextGrant()
        {
            var clock = new FakeClock();
            int rate = 1024 * 1024;
            var limiter = Create(clock, () => rate);
            Transfer(limiter, clock, 5_000);

            rate = 100 * 1024;
            long bytes = Transfer(limiter, clock, 10_000);
            Assert.That(bytes / 10.0, Is.EqualTo(rate).Within(2).Percent);

            rate = 0;
            Assert.AreEqual(int.MaxValue, limiter.GetAsync(16384, CancellationToken.None).Result);
            // time passes while unlimited, which refills the bucket
            clock.Now += 100;

            // limited again starts with a full bucket, not the debt from before
            rate = 100 * 1024;
            clock.Waits.Clear();
            int grant = limiter.GetAsync(16384, CancellationToken.None).Result;
            Assert.AreEqual(10240, grant);
            Assert.That(clock.Waits, Is.Empty);
        }

        [Test]
        public void UnusedBytesAreReturned()
        {
            var clock = new FakeClock();
            var limiter = Create(clock, () => 100 * 1024);
            int first = limiter.GetAsync(16384, CancellationToken.None).Result;
            Assert.AreEqual(10240, first);
            // the socket only had 240 bytes, so 10000 go back and the next grant needs no wait
            limiter.Return(first, 240);
            int second = limiter.GetAsync(10000, CancellationToken.None).Result;
            Assert.AreEqual(10000, second);
            Assert.That(clock.Waits, Is.Empty);
        }

        [Test]
        public void CancellationPropagates()
        {
            var clock = new FakeClock();
            var limiter = Create(clock, () => 1024);
            limiter.GetAsync(16384, CancellationToken.None).Wait();
            Assert.ThrowsAsync<OperationCanceledException>(() => limiter.GetAsync(16384, new CancellationToken(true)));
        }

        [Test]
        public async Task RealClockHoldsTheRate()
        {
            var limiter = new TransferRateLimiter(() => 512 * 1024);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long bytes = 0;
            while (bytes < 512 * 1024 * 2)
            {
                bytes += Math.Min(16384, await limiter.GetAsync(16384, CancellationToken.None));
            }
            // 1 MB at 512 KB/s, minus the initial 100 ms bucket
            Assert.That(sw.Elapsed.TotalSeconds, Is.InRange(1.8, 2.4));
        }

        [TestCase(false, false, 262144, int.MaxValue)]
        [TestCase(true, true, 262144, int.MaxValue)]
        [TestCase(true, false, 262144, 256)]
        [TestCase(true, false, 100, 1)]
        public void TotalLimitMapsToLibraryKiB(bool enabled, bool perTransfer, int bytesPerSec, int expected)
        {
            Assert.AreEqual(expected, SpeedLimitHelper.ToTotalLimitKiB(enabled, perTransfer, bytesPerSec));
        }
    }
}
