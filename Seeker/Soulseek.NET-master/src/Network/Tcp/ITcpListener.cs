// <copyright file="ITcpListener.cs" company="JP Dillingham">
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

namespace Soulseek.Network.Tcp
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Listens for connections from TCP network clients.
    /// </summary>
    internal interface ITcpListener
    {
        /// <summary>
        ///     Accepts a pending connection request as an asynchronous operation.
        /// </summary>
        /// <returns>The operation context, including the new connection client.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the listener has not been started with a call to Start().
        /// </exception>
        /// <exception cref="SocketException">Thrown when an error occurrs while accessing the socket.</exception>
        Task<TcpClient> AcceptTcpClientAsync();

        /// <summary>
        ///     Determines if there are pending connection requests.
        /// </summary>
        /// <returns>A value indicating whether there are pending connection requests.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the listener has not been started with a call to Start().
        /// </exception>
        bool Pending();

        /// <summary>
        ///     Starts listening for incoming connection requests.
        /// </summary>
        /// <param name="backlog">The maximum number of pending connections the OS will queue for this listener.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a negative backlog value is provided.</exception>
        /// <exception cref="SocketException">Thrown when an error occurrs while accessing the socket.</exception>
        void Start(int backlog = (int)SocketOptionName.MaxConnections);

        /// <summary>
        ///     Closes the listener.
        /// </summary>
        /// <exception cref="SocketException">Thrown when an error occurrs while accessing the socket.</exception>
        void Stop();
    }
}