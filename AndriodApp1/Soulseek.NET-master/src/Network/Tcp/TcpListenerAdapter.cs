// <copyright file="TcpListenerAdapter.cs" company="JP Dillingham">
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
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Listens for connections from TCP network clients.
    /// </summary>
    /// <remarks>
    ///     This is a pass-through implementation of <see cref="ITcpListener"/> over <see cref="TcpListener"/> intended to enable
    ///     dependency injection.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal sealed class TcpListenerAdapter : ITcpListener
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpListenerAdapter"/> class with an optional <paramref name="tcpListener"/>.
        /// </summary>
        /// <param name="tcpListener">The optional TcpListener to wrap.</param>
        public TcpListenerAdapter(TcpListener tcpListener = null)
        {
            TcpListener = tcpListener ?? new TcpListener(IPAddress.Parse("0.0.0.0"), 1);
        }

        private TcpListener TcpListener { get; set; }

        /// <summary>
        ///     Accepts a pending connection request as an asynchronous operation.
        /// </summary>
        /// <returns>The operation context, including the new connection client.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the listener has not been started with a call to Start().
        /// </exception>
        /// <exception cref="SocketException">Thrown when an error occurrs while accessing the socket.</exception>
        public Task<TcpClient> AcceptTcpClientAsync()
        {
            return TcpListener.AcceptTcpClientAsync();
        }

        /// <summary>
        ///     Determines if there are pending connection requests.
        /// </summary>
        /// <returns>A value indicating whether there are pending connection requests.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the listener has not been started with a call to Start().
        /// </exception>
        public bool Pending()
        {
            return TcpListener.Pending();
        }

        /// <summary>
        ///     Starts listening for incoming connection requests.
        /// </summary>
        /// <exception cref="SocketException">Thrown when an error occurrs while accessing the socket.</exception>
        public void Start()
        {
            TcpListener.Start();
        }

        /// <summary>
        ///     Closes the listener.
        /// </summary>
        /// <exception cref="SocketException">Thrown when an error occurrs while accessing the socket.</exception>
        public void Stop()
        {
            TcpListener.Stop();
        }
    }
}