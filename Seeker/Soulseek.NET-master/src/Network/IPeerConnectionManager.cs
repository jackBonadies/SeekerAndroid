// <copyright file="IPeerConnectionManager.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Manages peer <see cref="IConnection"/> instances for the application.
    /// </summary>
    internal interface IPeerConnectionManager : IDisposable, IDiagnosticGenerator
    {
        /// <summary>
        ///     Gets current list of peer message connections.
        /// </summary>
        IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> MessageConnections { get; }

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        IReadOnlyDictionary<int, string> PendingSolicitations { get; }

        /// <summary>
        ///     Adds a new message connection from an incoming connection.
        /// </summary>
        /// <remarks>
        ///     This method will be invoked from <see cref="ListenerHandler"/> upon receipt of an incoming unsolicited message
        ///     only. Because this connection is fully established by the time it is passed to this method, it must supersede any
        ///     cached connection, as it will be the most recently established connection as tracked by the remote user.
        /// </remarks>
        /// <param name="username">The username of the user from which the connection originated.</param>
        /// <param name="incomingConnection">The the accepted connection.</param>
        /// <returns>The operation context.</returns>
        Task AddOrUpdateMessageConnectionAsync(string username, IConnection incomingConnection);

        /// <summary>
        ///     Awaits an incoming transfer connection from the specified <paramref name="username"/> for the specified
        ///     <paramref name="filename"/> and <paramref name="remoteToken"/>.
        /// </summary>
        /// <remarks>
        ///     After this method is invoked, a <see cref="TransferResponse"/> message with the <paramref name="remoteToken"/>
        ///     must be sent to the <paramref name="username"/> via a message connection to signal the remote peer to initate the connection.
        /// </remarks>
        /// <param name="username">The username of the user from which the connection is expected.</param>
        /// <param name="filename">The filename associated with the expected transfer.</param>
        /// <param name="remoteToken">The remote token associated with the expected transfer.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the established connection.</returns>
        Task<IConnection> AwaitTransferConnectionAsync(string username, string filename, int remoteToken, CancellationToken cancellationToken);

        /// <summary>
        ///     Gets an existing message connection to the specified <paramref name="username"/>, if one exists.
        /// </summary>
        /// <param name="username">The username of the user for which to retrieve the cached connection.</param>
        /// <returns>The operation context, including the cached connection, or null if one does not exist.</returns>
        Task<IMessageConnection> GetCachedMessageConnectionAsync(string username);

        /// <summary>
        ///     Returns an existing, or gets a new connection using the details in the specified
        ///     <paramref name="connectToPeerResponse"/> and pierces the remote peer's firewall.
        /// </summary>
        /// <remarks>
        ///     This method will be invoked from <see cref="Messaging.Handlers.ServerMessageHandler"/> upon receipt of an
        ///     unsolicited <see cref="ConnectToPeerResponse"/> of type 'P' only. This connection should only be initiated if
        ///     there is no existing connection; superceding should be avoided if possible.
        /// </remarks>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context, including the new or updated connection.</returns>
        Task<IMessageConnection> GetOrAddMessageConnectionAsync(ConnectToPeerResponse connectToPeerResponse);

        /// <summary>
        ///     Gets a new or existing message connection to the specified <paramref name="username"/>.
        /// </summary>
        /// <remarks>
        ///     If a connection doesn't exist, a new direct connection is attempted first, and, if unsuccessful, an indirect
        ///     connection is attempted.
        /// </remarks>
        /// <param name="username">The username of the user to which to connect.</param>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the new or existing connection.</returns>
        Task<IMessageConnection> GetOrAddMessageConnectionAsync(string username, IPEndPoint ipEndPoint, CancellationToken cancellationToken);

        /// <summary>
        ///     Gets a new or existing message connection to the specified <paramref name="username"/>.
        /// </summary>
        /// <remarks>
        ///     If a connection doesn't exist, a new direct connection is attempted first, and, if unsuccessful, an indirect
        ///     connection is attempted.
        /// </remarks>
        /// <param name="username">The username of the user to which to connect.</param>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="solicitationToken">The optional token for the indirect connection solicitation.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the new or existing connection.</returns>
        Task<IMessageConnection> GetOrAddMessageConnectionAsync(string username, IPEndPoint ipEndPoint, int solicitationToken, CancellationToken cancellationToken);

        /// <summary>
        ///     Adds a new transfer connection from an incoming connection.
        /// </summary>
        /// <param name="username">The username of the user from which the connection originated.</param>
        /// <param name="token">The token with which the firewall was pierced.</param>
        /// <param name="incomingConnection">The the accepted connection.</param>
        /// <returns>The operation context.</returns>
        Task<(IConnection Connection, int RemoteToken)> GetTransferConnectionAsync(string username, int token, IConnection incomingConnection);

        /// <summary>
        ///     Gets a new transfer connection using the details in the specified <paramref name="connectToPeerResponse"/>,
        ///     pierces the remote peer's firewall, and retrieves the remote token.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context, including the new connection and the associated remote token.</returns>
        Task<(IConnection Connection, int RemoteToken)> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse);

        /// <summary>
        ///     Gets a new transfer connection to the specified <paramref name="username"/> using the specified <paramref name="token"/>.
        /// </summary>
        /// <remarks>A direct connection is attempted first, and, if unsuccessful, an indirect connection is attempted.</remarks>
        /// <param name="username">The username of the user to which to connect.</param>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="token">The token with which to initialize the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the new connection.</returns>
        Task<IConnection> GetTransferConnectionAsync(string username, IPEndPoint ipEndPoint, int token, CancellationToken cancellationToken);

        /// <summary>
        ///     Removes and disposes all active and queued connections.
        /// </summary>
        void RemoveAndDisposeAll();
    }
}