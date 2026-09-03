// <copyright file="IListener.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    ///     Listens for client connections for TCP network services.
    /// </summary>
    internal interface IListener
    {
        /// <summary>
        ///     Occurs when a new connection is accepted.
        /// </summary>
        event EventHandler<IConnection> Accepted;

        /// <summary>
        ///     Occurs when the listener encounters an exception while accepting a connection.
        /// </summary>
        event EventHandler<Exception> Error;

        /// <summary>
        ///     Gets the options used when creating new <see cref="IConnection"/> instances.
        /// </summary>
        ConnectionOptions ConnectionOptions { get; }

        /// <summary>
        ///     Gets the IP address to which the listener is bound.
        /// </summary>
        IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets a value indicating whether the listener is listening for connections.
        /// </summary>
        bool Listening { get; }

        /// <summary>
        ///     Gets the port of the listener.
        /// </summary>
        int Port { get; }

        /// <summary>
        ///     Starts the listener.
        /// </summary>
        /// <param name="backlog">The maximum number of pending connections the OS will queue for this listener.</param>
        void Start(int backlog = (int)SocketOptionName.MaxConnections);

        /// <summary>
        ///     Stops the listener.
        /// </summary>
        void Stop();
    }
}
