using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Transfers
{
    // Token bucket for a single transfer, refilled from elapsed time.
    // One instance per DownloadAsync / UploadAsync call, no global shared state.
    public sealed class TransferRateLimiter
    {
        private const double BucketSeconds = 0.1;

        private readonly Func<int> getBytesPerSecond;
        private readonly Func<long> getTimestamp;
        private readonly long timestampFrequency;
        private readonly Func<TimeSpan, CancellationToken, Task> delay;

        private double tokens;
        private long lastTimestamp;
        private int lastRate;

        public TransferRateLimiter(Func<int> getBytesPerSecond)
            : this(getBytesPerSecond, Stopwatch.GetTimestamp, Stopwatch.Frequency, Task.Delay)
        {
        }

        public TransferRateLimiter(Func<int> getBytesPerSecond, Func<long> getTimestamp, long timestampFrequency, Func<TimeSpan, CancellationToken, Task> delay)
        {
            this.getBytesPerSecond = getBytesPerSecond;
            this.getTimestamp = getTimestamp;
            this.timestampFrequency = timestampFrequency;
            this.delay = delay;
        }

        public async Task<int> GetAsync(int requestedBytes, CancellationToken cancellationToken)
        {
            int rate = getBytesPerSecond();
            if (rate <= 0)
            {
                return int.MaxValue;
            }

            // the bucket holds BucketSeconds worth of bytes
            double amount = Math.Max(1.0, rate * BucketSeconds);
            long now = getTimestamp();
            if (lastRate == 0)
            {
                // if first time take the full amount
                tokens = amount;
            }
            else
            {
                // here we always min, we dont allow "catch up" rather the speed limit is more like a ceiling
                //  there will never be bursts above the ceiling
                // this is for if we are calling this say 20x a second, we will take .05 * rate, so half of "amount"
                tokens = Math.Min(amount, tokens + ((now - lastTimestamp) * (double)rate / timestampFrequency));
            }
            lastTimestamp = now;
            lastRate = rate;

            // take the grant now and wait off any debt, so the wait is at most one bucket
            int grant = (int)Math.Max(1, Math.Min(requestedBytes, amount));
            tokens -= grant;
            double waitSeconds = tokens < 0 ? -tokens / rate : 0;

            if (waitSeconds > 0)
            {
                await delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken).ConfigureAwait(false);
            }
            return grant;
        }

        // bytes granted but not read (the socket returned less) go back in the bucket.
        // no clamp: the next GetAsync clamps to the current capacity before granting
        public void Return(int grantedBytes, int actualBytes)
        {
            int unused = grantedBytes - actualBytes;
            if (unused > 0)
            {
                tokens += unused;
            }
        }

        // TransferOptions governor / reporter shapes
        public Task<int> GovernAsync(Soulseek.Transfer transfer, int requestedBytes, CancellationToken cancellationToken) => GetAsync(requestedBytes, cancellationToken);

        public void Report(Soulseek.Transfer transfer, int attemptedBytes, int grantedBytes, int actualBytes) => Return(grantedBytes, actualBytes);
    }
}
