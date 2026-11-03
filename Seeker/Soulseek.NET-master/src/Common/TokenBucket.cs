// <copyright file="TokenBucket.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Implements the 'token bucket' or 'leaky bucket' rate limiting algorithm.
    /// </summary>
    internal sealed class TokenBucket : ITokenBucket
    {
        private TaskCompletionSource<bool> waitForReset = new TaskCompletionSource<bool>();
        private long currentCount;
        private long currentCapacity;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TokenBucket"/> class.
        /// </summary>
        /// <param name="capacity">The bucket capacity.</param>
        /// <param name="interval">The interval at which tokens are replenished.</param>
        public TokenBucket(long capacity, int interval)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Bucket capacity must be greater than or equal to 1");
            }

            if (interval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than or equal to 1");
            }

            Capacity = capacity;

            currentCapacity = Capacity;
            currentCount = Capacity;

            Clock = new System.Timers.Timer(interval);
            Clock.Elapsed += (sender, e) =>
            {
                Interlocked.Exchange(ref currentCapacity, Capacity);
                Interlocked.Exchange(ref currentCount, currentCapacity);
                Reset();
            };

            Clock.Start();
        }

        /// <summary>
        ///     Gets the bucket capacity.
        /// </summary>
        public long Capacity { get; private set; }
        private System.Timers.Timer Clock { get; set; }
        private bool Disposed { get; set; }
        private SemaphoreSlim SyncRoot { get; } = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool> Disposal { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Asynchronously retrieves the specified token <paramref name="count"/> from the bucket.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If the requested <paramref name="count"/> exceeds the bucket <see cref="Capacity"/>, the request is lowered to
        ///         the capacity of the bucket.
        ///     </para>
        ///     <para>If the bucket has tokens available, but fewer than the requested amount, the available tokens are returned.</para>
        ///     <para>
        ///         If the bucket has no tokens available, execution waits for the bucket to be replenished before servicing the request.
        ///     </para>
        /// </remarks>
        /// <param name="count">The number of tokens to retrieve.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that completes when tokens have been provided.</returns>
        public Task<int> GetAsync(int count, CancellationToken cancellationToken = default)
        {
            if (count <= 0)
            {
                return Task.FromResult(0);
            }

            return GetInternalAsync(Math.Min(count, (int)Math.Min(int.MaxValue, currentCapacity)), cancellationToken);
        }

        /// <summary>
        ///     Returns the specified token <paramref name="count"/> to the bucket.
        /// </summary>
        /// <remarks>
        ///     <para>This method should only be called if tokens were retrieved from the bucket, but were not used.</para>
        ///     <para>
        ///         If the specified count exceeds the bucket capacity, the count is lowered to the capacity. Effectively this
        ///         allows the bucket to 'burst' up to 2x capacity to 'catch up' to the desired rate if tokens were wastefully
        ///         retrieved.
        ///     </para>
        ///     <para>If the specified count is negative, no change is made to the available count.</para>
        /// </remarks>
        /// <param name="count">The number of tokens to return.</param>
        public void Return(int count)
        {
            if (count <= 0)
            {
                return;
            }

            long current, updated;

            // lock-free read-then-act operation; the while loop is the backstop against the value changing between
            // when it was read and when we go to update it; if it changed CompareExchange returns false and we do it
            // again until we get a 'clean' update, which shouldn't take more than one iteration typically.
            do
            {
                var capacity = Interlocked.Read(ref currentCapacity);
                current = Interlocked.Read(ref currentCount);
                updated = Math.Min(current + Math.Min(count, capacity), capacity * 2);
            }
            while (Interlocked.CompareExchange(ref currentCount, value: updated, comparand: current) != current);
        }

        /// <summary>
        ///     Sets the bucket capacity to the supplied <paramref name="capacity"/>.
        /// </summary>
        /// <remarks>Change takes effect on the next reset.</remarks>
        /// <param name="capacity">The bucket capacity.</param>
        public void SetCapacity(long capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Bucket capacity must be greater than or equal to 1");
            }

            Capacity = capacity;
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Clock.Dispose();
                    Disposal.TrySetException(new ObjectDisposedException(nameof(TokenBucket)));
                }

                Disposed = true;
            }
        }

        private async Task<int> GetInternalAsync(int count, CancellationToken cancellationToken = default)
        {
            await SyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(TokenBucket));
                }

                // capture the present value of the wait so we have the right one if reset fires between when we check the
                // count and enter the body below to wait. if we get the new reset instead of the one that exist now,
                // we could wait another interval needlessly
                var currentWaitForReset = Volatile.Read(ref waitForReset);

                // if the bucket is empty, wait for a reset, then replenish it before continuing
                // this ensures tokens are distributed in the order in which callers obtain the semaphore,
                // which is as close to a FIFO as .NET synchronization primitives will allow
                if (Interlocked.Read(ref currentCount) == 0)
                {
                    // wait for the reset or for cancellation, whichever comes first
                    var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                    using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
                    {
                        var winner = await Task.WhenAny(currentWaitForReset.Task, cancelled.Task, Disposal.Task).ConfigureAwait(false);
                        await winner.ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                long current, availableCount;

                // lock-free read-then-act operation as seen above. this protects us against over- or under- granting
                // counts if another thread increments or decrements the count between the time we read it and the time
                // we go to update it with the deducted amount
                do
                {
                    // take the minimum of requested count or currentCount, deduct it from
                    // currentCount (potentially zeroing the bucket), and return it
                    current = Interlocked.Read(ref currentCount);
                    availableCount = Math.Min(current, count);
                }
                while (Interlocked.CompareExchange(ref currentCount, value: current - availableCount, comparand: current) != current);

                return (int)availableCount;
            }
            finally
            {
                SyncRoot.Release();
            }
        }

        private void Reset()
            => Interlocked.Exchange(ref waitForReset, new TaskCompletionSource<bool>()).TrySetResult(true);
    }
}