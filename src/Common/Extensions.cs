// <copyright file="Extensions.cs" company="JP Dillingham">
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
        /// <param name="task">The task to continue.</param>
        public static void Forget(this Task task)
        {
            task.ContinueWith(t => { }, TaskContinuationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        ///     Continue a task and report an Exception if one is raised.
        /// </summary>
        /// <typeparam name="T">The type of Exception to throw.</typeparam>
        /// <param name="task">The task to continue.</param>
        public static void ForgetButThrowWhenFaulted<T>(this Task task)
            where T : Exception
        {
            task.ContinueWith(t => { throw (T)Activator.CreateInstance(typeof(T), t.Exception.Message, t.Exception); }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.RunContinuationsAsynchronously);
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
            using MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sBuilder.ToString();
        }
    }
}