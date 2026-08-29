// <copyright file="ListenerHandler.cs" company="JP Dillingham">
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
    using System.Linq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Handles incoming connections established by the <see cref="IListener"/>.
    /// </summary>
    internal sealed class ListenerHandler : IListenerHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ListenerHandler"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public ListenerHandler(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Handle <see cref="IListener.Accepted"/> events.
        /// </summary>
        /// <param name="sender">The originating <see cref="IListener"/> instance.</param>
        /// <param name="connection">The accepted connection.</param>
        public async void HandleConnection(object sender, IConnection connection)
        {
            Diagnostic.Debug($"Accepted incoming connection from {connection.IPEndPoint.Address} on {SoulseekClient.Listener.IPAddress}:{SoulseekClient.Listener.Port} (id: {connection.Id})");

            try
            {
                var lengthBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);

                var bodyBytes = await connection.ReadAsync(length).ConfigureAwait(false);
                byte[] message = lengthBytes.Concat(bodyBytes).ToArray();

                if (PeerInit.TryFromByteArray(message, out var peerInit))
                {
                    // this connection is the result of an unsolicited connection from the remote peer, either to request info or
                    // browse, or to send a file.
                    Diagnostic.Debug($"PeerInit for connection type {peerInit.ConnectionType} received from {peerInit.Username} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");

                    if (peerInit.ConnectionType == Constants.ConnectionType.Peer)
                    {
                        await SoulseekClient.PeerConnectionManager.AddOrUpdateMessageConnectionAsync(
                            peerInit.Username,
                            connection).ConfigureAwait(false);
                    }
                    else if (peerInit.ConnectionType == Constants.ConnectionType.Transfer)
                    {
                        // slightly misleading name; this hands the incoming connection off instead of establishing new
                        var (transferConnection, remoteToken) = await SoulseekClient.PeerConnectionManager.GetTransferConnectionAsync(
                            peerInit.Username,
                            peerInit.Token,
                            connection).ConfigureAwait(false);

                        var waitKey = new WaitKey(Constants.WaitKey.DirectTransfer, peerInit.Username, remoteToken);

                        // check to see if we are expecting this token, and if so complete the wait and start the upload
                        if (SoulseekClient.Waiter.HasWait(waitKey))
                        {
                            SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.DirectTransfer, peerInit.Username, remoteToken), transferConnection);
                        }
                        else
                        {
                            // either a random client connected and tried to download something without being told it could,
                            // or a client tried to initiate a transfer as a last-ditch effort to "save" an upload
                            Diagnostic.Debug($"Unexpected transfer connection for token {peerInit.Token} from {peerInit.Username} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");
                            transferConnection.Disconnect("Transfer connection rejected: unknown token");
                        }
                    }
                    else if (peerInit.ConnectionType == Constants.ConnectionType.Distributed)
                    {
                        await SoulseekClient.DistributedConnectionManager.AddOrUpdateChildConnectionAsync(
                            peerInit.Username,
                            connection).ConfigureAwait(false);
                    }
                }
                else if (PierceFirewall.TryFromByteArray(message, out var pierceFirewall))
                {
                    // this connection is the result of a ConnectToPeer request sent to the user, and the incoming message will
                    // contain the token that was provided in the request. Ensure this token is among those expected, and use it
                    // to determine the username of the remote user.
                    if (SoulseekClient.PeerConnectionManager.PendingSolicitations.TryGetValue(pierceFirewall.Token, out var peerUsername))
                    {
                        Diagnostic.Debug($"Peer PierceFirewall with token {pierceFirewall.Token} received from {peerUsername} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedPeerConnection, peerUsername, pierceFirewall.Token), connection);
                    }
                    else if (SoulseekClient.DistributedConnectionManager.PendingSolicitations.TryGetValue(pierceFirewall.Token, out var distributedUsername))
                    {
                        Diagnostic.Debug($"Distributed PierceFirewall with token {pierceFirewall.Token} received from {distributedUsername} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, distributedUsername, pierceFirewall.Token), connection);
                    }
                    else if (SoulseekClient.Options.SearchResponseCache != null && SoulseekClient.Options.SearchResponseCache.TryGet(pierceFirewall.Token, out var cachedSearchResponse))
                    {
                        // users may connect to retrieve search results long after we've given up waiting for them.  if this is the case, accept the connection,
                        // cache it with the manager for potential reuse, then try to send the pending response.
                        var (username, _, _, _) = cachedSearchResponse;

                        Diagnostic.Debug($"PierceFirewall matching pending search response received from {username} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");
                        await SoulseekClient.PeerConnectionManager.AddOrUpdateMessageConnectionAsync(username, connection).ConfigureAwait(false);
                        await SoulseekClient.SearchResponder.TryRespondAsync(pierceFirewall.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new ConnectionException($"Unknown PierceFirewall attempt with token {pierceFirewall.Token} from {connection.IPEndPoint.Address}:{connection.IPEndPoint.Port} (id: {connection.Id})");
                    }
                }
                else
                {
                    throw new ConnectionException($"Unrecognized initialization message: {BitConverter.ToString(message)} ({message.Length} bytes, id: {connection.Id})");
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to initialize direct connection from {connection.IPEndPoint.Address}:{connection.IPEndPoint.Port}: {ex.Message}");
                connection.Disconnect(exception: ex);
                connection.Dispose();
            }
        }
    }
}
