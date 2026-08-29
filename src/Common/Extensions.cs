// <copyright file="Extensions.cs" company="JP Dillingham">
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
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    ///     Extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        ///     Dequeues and disposes of all instances within the specified <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The contained type of the queue.</typeparam>
        /// <param name="concurrentQueue">The queue from which to dequeue and dispose.</param>
        public static void DequeueAndDisposeAll<T>(this ConcurrentQueue<T> concurrentQueue)
            where T : IDisposable
        {
            while (!concurrentQueue.IsEmpty)
            {
                if (concurrentQueue.TryDequeue(out var value))
                {
                    value.Dispose();
                }
            }
        }

        /// <summary>
        ///     Continue a task and swallow any Exceptions.
        /// </summary>
        /// <remarks>
        ///     The continuation touches the Task <see cref="Task.Exception"/> (if one exists)
        ///     to avoid an <see cref="TaskScheduler.UnobservedTaskException"/>.
        /// </remarks>
        /// <param name="task">The task to continue.</param>
        /// <param name="options">Optional continuation options.</param>
        public static void Forget(this Task task, TaskContinuationOptions? options = null)
        {
            task.ContinueWith(
                continuationAction: t => _ = t.Exception, // this is a no-op, but it marks the Exception as having been observed so it won't throw
                continuationOptions: TaskContinuationOptions.OnlyOnFaulted | (options ?? TaskContinuationOptions.RunContinuationsAsynchronously));
        }

        /// <summary>
        ///     Removes and disposes of all instances within the specified <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="concurrentDictionary">The dictionary from which to remove.</param>
        public static void RemoveAndDisposeAll<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> concurrentDictionary)
                    where TValue : IDisposable
        {
            while (!concurrentDictionary.IsEmpty)
            {
                if (concurrentDictionary.TryRemove(concurrentDictionary.Keys.First(), out var value))
                {
                    value.Dispose();
                }
            }
        }

        /// <summary>
        ///     Reset a timer.
        /// </summary>
        /// <param name="timer">The timer to reset.</param>
        public static void Reset(this Timer timer)
        {
            try
            {
                timer.Stop();
                timer.Start();
            }
            catch (ObjectDisposedException)
            {
                // noop
            }
        }

        /// <summary>
        ///     Returns the MD5 hash of a string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <returns>The MD5 hash of the input string.</returns>
        public static string ToMD5Hash(this string str)
        {
#pragma warning disable S4790 // Weak hashing algorithms should not be used

            using MD5 md5Hash = MD5.Create();
#pragma warning restore S4790 // Weak hashing algorithms should not be used

            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sBuilder.ToString();
        }

        /// <summary>
        ///     Safely disposes an <see cref="IDisposable"/> instance.
        /// </summary>
        /// <param name="obj">The IDisposable instance to dispose.</param>
        /// <returns>A value indicating whether the disposal succeeded.</returns>
        public static bool TryDispose(this IDisposable obj)
        {
            try
            {
                obj.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}