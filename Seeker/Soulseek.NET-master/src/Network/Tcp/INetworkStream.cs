// <copyright file="INetworkStream.cs" company="JP Dillingham">
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

namespace Soulseek.Network.Tcp
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides the underlying stream of data for network access.
    /// </summary>
    internal interface INetworkStream : IDisposable
    {
        /// <summary>
        ///     Gets or sets the read timeout for the stream.
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        ///     Gets or sets the write timeout for the stream.
        /// </summary>
        int WriteTimeout { get; set; }

        /// <summary>
        ///     Closes the <see cref="NetworkStream"/>.
        /// </summary>
        void Close();

        /// <summary>
        ///     Asynchronously reads data from the <see cref="NetworkStream"/>.
        /// </summary>
        /// <param name="buffer">An array of type <see cref="byte"/> into which the read data will be written.</param>
        /// <param name="offset">The location in <paramref name="buffer"/> from which to start reading data.</param>
        /// <param name="size">The number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="offset"/> is less than zero or greater than the length of
        ///     <paramref name="buffer"/>, or <paramref name="size"/> is less than zero or greater than the length of
        ///     <paramref name="buffer"/> minus the value of the <paramref name="offset"/> parameter.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the stream is write only.</exception>
        /// <exception cref="IOException">Thrown when an error occurs while reading from the network.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the <see cref="NetworkStream"/> is closed.</exception>
        Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken);

#if NETSTANDARD2_1_OR_GREATER
        /// <summary>
        ///     Asynchronously reads data from the <see cref="NetworkStream"/>.
        /// </summary>
        /// <param name="buffer">An array of type <see cref="byte"/> into which the read data will be written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="buffer"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the stream is write only.</exception>
        /// <exception cref="IOException">Thrown when an error occurs while reading from the network.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the <see cref="NetworkStream"/> is closed.</exception>
        ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
#endif

        /// <summary>
        ///     Asynchronously writes data to the <see cref="NetworkStream"/>.
        /// </summary>
        /// <param name="buffer">An array of type <see cref="byte"/> that contains the data to write to the <see cref="NetworkStream"/>.</param>
        /// <param name="offset">The location in <paramref name="buffer"/> from which to start writing data.</param>
        /// <param name="size">The number of bytes to write to the <see cref="NetworkStream"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="offset"/> is less than zero or greater than the length of
        ///     <paramref name="buffer"/>, or <paramref name="size"/> is less than zero or greater than the length of
        ///     <paramref name="buffer"/> minus the value of the <paramref name="offset"/> parameter.
        /// </exception>
        /// <exception cref="IOException">Thrown when an error occurs while writing to the network.</exception>
        /// <exception cref="ObjectDisposedException">
        ///     Thrown when the <see cref="NetworkStream"/> is closed or when there was a failure reading from the network.
        /// </exception>
        Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken);

#if NETSTANDARD2_1_OR_GREATER
        /// <summary>
        ///     Asynchronously writes data to the <see cref="NetworkStream"/>.
        /// </summary>
        /// <param name="buffer">A ReadOnlyMemory span of type <see cref="byte"/> that contains the data to write to the <see cref="NetworkStream"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="buffer"/> is null.</exception>
        /// <exception cref="IOException">Thrown when an error occurs while writing to the network.</exception>
        /// <exception cref="ObjectDisposedException">
        ///     Thrown when the <see cref="NetworkStream"/> is closed or when there was a failure reading from the network.
        /// </exception>
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
#endif
    }
}