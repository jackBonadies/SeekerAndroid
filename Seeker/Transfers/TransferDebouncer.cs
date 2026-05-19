using System;

namespace Seeker.Transfers
{
    /// <summary>
    /// Tracks whether a bulk transfer action (cancel-and-clear-all, abort-all) was just
    /// pressed. The cancellation events for each transfer fire on many threads after the
    /// button is pressed — the service code reads <see cref="IsActive"/> to recognize a
    /// recent bulk action and skip per-item handling.
    /// </summary>
    public sealed class TransferDebouncer
    {
        public static readonly TransferDebouncer CancelAndClearAll = new TransferDebouncer(TimeSpan.FromMilliseconds(750));
        public static readonly TransferDebouncer AbortAll = new TransferDebouncer(TimeSpan.FromMilliseconds(750));

        private readonly long windowMs;
        private long lastTriggerMs;

        private TransferDebouncer(TimeSpan window)
        {
            windowMs = (long)window.TotalMilliseconds;
            lastTriggerMs = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
        }

        public void Trigger()
        {
            lastTriggerMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public bool IsActive()
        {
            return (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastTriggerMs) < windowMs;
        }
    }
}
