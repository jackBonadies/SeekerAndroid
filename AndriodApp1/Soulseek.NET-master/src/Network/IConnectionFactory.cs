// <copyright file="IConnectionFactory.cs" company="JP Dillingham">
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

namespace Soulseek.Network
{
    using System;
    using System.Net;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Creates connections.
    /// </summary>
    internal interface IConnectionFactory
    {
        /// <summary>
        ///     Gets a distributed <see cref="IMessageConnection"/> with the specified parameters.
        /// </summary>
        /// <param name="username">The username of the peer associated with the connection, if applicable.</param>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        /// <returns>The created connection.</returns>
        IMessageConnection GetDistributedConnection(string username, IPEndPoint ipEndPoint, ConnectionOptions options = null, ITcpClient tcpClient = null);

        /// <summary>
        ///     Gets a <see cref="IMessageConnection"/> with the specified parameters.
        /// </summary>
        /// <param name="username">The username of the peer associated with the connection, if applicable.</param>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        /// <returns>The created connection.</returns>
        IMessageConnection GetMessageConnection(string username, IPEndPoint ipEndPoint, ConnectionOptions options = null, ITcpClient tcpClient = null);

        /// <summary>
        ///     Gets a <see cref="IMessageConnection"/> for use with a server connection and binds the specified event handlers
        ///     before returning.
        /// </summary>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="connectedEventHandler">The event handler for <see cref="IConnection.Connected"/>.</param>
        /// <param name="disconnectedEventHandler">The handler for <see cref="IConnection.Disconnected"/>.</param>
        /// <param name="messageReadEventHandler">The handler for <see cref="IMessageConnection.MessageRead"/>.</param>
        /// <param name="messageWrittenEventHandler">The handler for <see cref="IMessageConnection.MessageWritten"/>.</param>
        /// <param name="options">The options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        /// <returns>The created connection with event handlers bound.</returns>
        IMessageConnection GetServerConnection(
            IPEndPoint ipEndPoint,
            EventHandler connectedEventHandler,
            EventHandler<ConnectionDisconnectedEventArgs> disconnectedEventHandler,
            EventHandler<MessageEventArgs> messageReadEventHandler,
            EventHandler<MessageEventArgs> messageWrittenEventHandler,
            ConnectionOptions options = null,
            ITcpClient tcpClient = null);

        /// <summary>
        ///     Gets a <see cref="IConnection"/> for use with transfer connections with the specified parameters.
        /// </summary>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        /// <returns>The created connection.</returns>
        IConnection GetTransferConnection(IPEndPoint ipEndPoint, ConnectionOptions options = null, ITcpClient tcpClient = null);
    }
}