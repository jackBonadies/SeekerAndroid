// <copyright file="IListener.cs" company="JP Dillingham">
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
    using System.Net;

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
        void Start();

        /// <summary>
        ///     Stops the listener.
        /// </summary>
        void Stop();
    }
}
