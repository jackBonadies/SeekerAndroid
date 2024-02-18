// <copyright file="IMessageConnection.cs" company="JP Dillingham">
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
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Provides client connections to the Soulseek network.
    /// </summary>
    internal interface IMessageConnection : IConnection
    {
        /// <summary>
        ///     Occurs when message data is received.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is separate from the underlying <see cref="Connection.DataRead"/> because it is bounded to the
        ///         message payload. The base event will be raised when reading the message length and code, while this event will not.
        ///     </para>
        ///     <para>
        ///         This event is only useful for tracking the progress of large messages (larger than the receive buffer);
        ///         basically only the response to a browse request.
        ///     </para>
        /// </remarks>
        event EventHandler<MessageDataEventArgs> MessageDataRead;

        /// <summary>
        ///     Occurs when a new message is read in its entirety.
        /// </summary>
        event EventHandler<MessageEventArgs> MessageRead;

        /// <summary>
        ///     Occurs when a new message is received, but before it is read.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Occurs when a message is written in its entirety.
        /// </summary>
        event EventHandler<MessageEventArgs> MessageWritten;

        /// <summary>
        ///     Gets the length message codes received, in bytes.
        /// </summary>
        int CodeLength { get; }

        /// <summary>
        ///     Gets a value indicating whether this connection is connected to the server, as opposed to a peer.
        /// </summary>
        bool IsServerConnection { get; }

        /// <summary>
        ///     Gets a value indicating whether the internal continuous read loop is running.
        /// </summary>
        bool ReadingContinuously { get; }

        /// <summary>
        ///     Gets the username of the peer associated with the connection, if applicable.
        /// </summary>
        string Username { get; }

        /// <summary>
        ///     Begins the internal continuous read loop, if it has not yet started.
        /// </summary>
        /// <remarks>
        ///     This functionality should be used only when an incoming connection has already been established in an IConnection
        ///     instance and with a Connected ITcpClient, and when that IConnection is upgraded to an IMessageConnection, handing
        ///     off the ITcpClient instance without disconnecting it. Normally reading begins when the Connected event is fired,
        ///     but since the connection is already Connected the event will not be fired again. It is important to delay the
        ///     start of the read loop until after the calling code has had the chance to connect an event handler to the
        ///     MessageRead event, which is impossible if we simply start the loop immediately upon instantiation.
        /// </remarks>
        void StartReadingContinuously();

        /// <summary>
        ///     Asynchronously writes the specified <paramref name="message"/> to the connection.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="MessageException">
        ///     Thrown when an error is encountered while converting the message to a byte array.
        /// </exception>
        /// <exception cref="ConnectionWriteException">Thrown when an unexpected error occurs.</exception>
        Task WriteAsync(IOutgoingMessage message, CancellationToken? cancellationToken = null);
    }
}