// <copyright file="IWaiter.cs" company="JP Dillingham">
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
    ///     Enables await-able server messages.
    /// </summary>
    internal interface IWaiter : IDisposable
    {
        /// <summary>
        ///     Gets the default timeout duration.
        /// </summary>
        int DefaultTimeout { get; }

        /// <summary>
        ///     Cancels the oldest wait matching the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique WaitKey for the wait.</param>
        void Cancel(WaitKey key);

        /// <summary>
        ///     Cancels all waits.
        /// </summary>
        void CancelAll();

        /// <summary>
        ///     Completes the oldest wait matching the specified <paramref name="key"/> with the specified <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="key">The unique WaitKey for the wait.</param>
        /// <param name="result">The wait result.</param>
        void Complete<T>(WaitKey key, T result);

        /// <summary>
        ///     Completes the oldest wait matching the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique WaitKey for the wait.</param>
        void Complete(WaitKey key);

        /// <summary>
        ///     Returns a value indicating whether the waiter has any waits for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique WaitKey for the wait.</param>
        /// <returns>A value indicating whether any waits exist for the key.</returns>
        bool HasWait(WaitKey key);

        /// <summary>
        ///     Throws the specified <paramref name="exception"/> on the oldest wait matching the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique WaitKey for the wait.</param>
        /// <param name="exception">The Exception to throw.</param>
        void Throw(WaitKey key, Exception exception);

        /// <summary>
        ///     Causes the oldest wait matching the specified <paramref name="key"/> to time out.
        /// </summary>
        /// <param name="key">The unique WaitKey for the wait.</param>
        void Timeout(WaitKey key);

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> and with the specified <paramref name="timeout"/>.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="timeout">The wait timeout.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        Task<T> Wait<T>(WaitKey key, int? timeout = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> and with the specified <paramref name="timeout"/>.
        /// </summary>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="timeout">The wait timeout.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        Task Wait(WaitKey key, int? timeout = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> which does not time out.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        Task<T> WaitIndefinitely<T>(WaitKey key, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> which does not time out.
        /// </summary>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        Task WaitIndefinitely(WaitKey key, CancellationToken? cancellationToken = null);
    }
}