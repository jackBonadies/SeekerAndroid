// <copyright file="ITokenBucket.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Implements the 'token bucket' or 'leaky bucket' rate limiting algorithm.
    /// </summary>
    internal interface ITokenBucket : IDisposable
    {
        /// <summary>
        ///     Gets the bucket capacity.
        /// </summary>
        public long Capacity { get; }

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
        Task<int> GetAsync(int count, CancellationToken cancellationToken = default);

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
        void Return(int count);

        /// <summary>
        ///     Sets the bucket capacity to the supplied <paramref name="capacity"/>.
        /// </summary>
        /// <remarks>Change takes effect on the next reset.</remarks>
        /// <param name="capacity">The bucket capacity.</param>
        void SetCapacity(long capacity);
    }
}