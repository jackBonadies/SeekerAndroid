// <copyright file="DistributedMessageHandler.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Handlers
{
    using System;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

    /// <summary>
    ///     Handles incoming messages from distributed connections.
    /// </summary>
    internal sealed class DistributedMessageHandler : IDistributedMessageHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedMessageHandler"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public DistributedMessageHandler(
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

        private string DeduplicationHash { get; set; }
        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Handles incoming messages from distributed children.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> from which the message originated.</param>
        /// <param name="args">The message event args.</param>
        public void HandleChildMessageRead(object sender, MessageEventArgs args)
        {
            HandleChildMessageRead(sender, args.Message);
        }

        /// <summary>
        ///     Handles incoming messages from distributed children.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> from which the message originated.</param>
        /// <param name="message">The message.</param>
        public async void HandleChildMessageRead(object sender, byte[] message)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Distributed>(message).ReadCode();

            if (code != MessageCode.Distributed.Ping)
            {
                Diagnostic.Debug($"Distributed child message received: {code} from {connection.Username} ({connection.IPEndPoint}) (id: {connection.Id})");
            }

            try
            {
                switch (code)
                {
                    case MessageCode.Distributed.ChildDepth:
                        break;

                    case MessageCode.Distributed.Ping:
                        var pingResponse = new DistributedPingResponse(SoulseekClient.GetNextToken());
                        await connection.WriteAsync(pingResponse).ConfigureAwait(false);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled distributed child message: {code} from {connection.Username} ({connection.IPEndPoint}); {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling distributed child message: {code} from {connection.Username} ({connection.IPEndPoint}); {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Handles outgoing messages to distributed children, post send.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> instance to which the message was sent.</param>
        /// <param name="args">The message event args.</param>
        public void HandleChildMessageWritten(object sender, MessageEventArgs args)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Distributed>(args.Message).ReadCode();

            if (code != MessageCode.Distributed.Ping)
            {
                Diagnostic.Debug($"Distributed child message sent: {code} to {connection.Username} ({connection.IPEndPoint}) (id: {connection.Id})");
            }
        }

        /// <summary>
        ///     Handles incoming messages.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="args">The message event args.</param>
        public void HandleMessageRead(object sender, MessageEventArgs args)
        {
            HandleMessageRead(sender, args.Message);
        }

        /// <summary>
        ///     Handles incoming messages.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="message">The message.</param>
        public async void HandleMessageRead(object sender, byte[] message)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Distributed>(message).ReadCode();

            if (code != MessageCode.Distributed.SearchRequest && code != MessageCode.Distributed.EmbeddedMessage && code != MessageCode.Distributed.Ping)
            {
                Diagnostic.Debug($"Distributed message received: {code} from {connection.Username} ({connection.IPEndPoint}) (id: {connection.Id})");
            }
            else if (SoulseekClient.Options.DeduplicateSearchRequests)
            {
                var current = Convert.ToBase64String(message);

                if (DeduplicationHash == current)
                {
                    return;
                }

                DeduplicationHash = current;
            }

            try
            {
                switch (code)
                {
                    // if we are connected to a branch root, we will receive EmbeddedMessage/93.
                    case MessageCode.Distributed.EmbeddedMessage:
                        var embeddedMessage = EmbeddedMessage.FromByteArray(message);

                        switch (embeddedMessage.DistributedCode)
                        {
                            // convert this message to a normal DistributedSearchRequest before forwarding.  this functionality is based
                            // on the observation that branch roots send embedded messages to children, while parents that are not a branch root
                            // send a plain SearchRequest/3.
                            case MessageCode.Distributed.SearchRequest:
                                var embeddedSearchRequest = DistributedSearchRequest.FromByteArray(embeddedMessage.DistributedMessage);

                                _ = SoulseekClient.DistributedConnectionManager.BroadcastMessageAsync(embeddedMessage.DistributedMessage).ConfigureAwait(false);

                                if (embeddedSearchRequest.Username == SoulseekClient.Username)
                                {
                                    break; // don't respond to our own searches
                                }

                                await SoulseekClient.SearchResponder.TryRespondAsync(embeddedSearchRequest.Username, embeddedSearchRequest.Token, embeddedSearchRequest.Query).ConfigureAwait(false);

                                break;
                            default:
                                Diagnostic.Debug($"Unhandled embedded message: {code} from {connection.Username} ({connection.IPEndPoint}); {message.Length} bytes");
                                break;
                        }

                        break;

                    // if we are connected to anyone other than a branch root, we will receive SearchRequest/3.
                    case MessageCode.Distributed.SearchRequest:
                        var searchRequest = DistributedSearchRequest.FromByteArray(message);

                        _ = SoulseekClient.DistributedConnectionManager.BroadcastMessageAsync(message).ConfigureAwait(false);

                        if (searchRequest.Username == SoulseekClient.Username)
                        {
                            break; // don't respond to our own searches
                        }

                        await SoulseekClient.SearchResponder.TryRespondAsync(searchRequest.Username, searchRequest.Token, searchRequest.Query).ConfigureAwait(false);

                        break;

                    case MessageCode.Distributed.Ping:
                        var pingResponse = DistributedPingResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Distributed.Ping, connection.Username), pingResponse);

                        break;

                    case MessageCode.Distributed.BranchLevel:
                        var branchLevel = DistributedBranchLevel.FromByteArray(message);

                        if ((connection.Username, connection.IPEndPoint) == SoulseekClient.DistributedConnectionManager.Parent)
                        {
                            SoulseekClient.DistributedConnectionManager.SetParentBranchLevel(branchLevel.Level);
                        }

                        break;

                    case MessageCode.Distributed.BranchRoot:
                        var branchRoot = DistributedBranchRoot.FromByteArray(message);

                        if ((connection.Username, connection.IPEndPoint) == SoulseekClient.DistributedConnectionManager.Parent)
                        {
                            SoulseekClient.DistributedConnectionManager.SetParentBranchRoot(branchRoot.Username);
                        }

                        break;

                    case MessageCode.Distributed.ChildDepth:
                        var childDepth = DistributedChildDepth.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.ChildDepthMessage, connection.Key), childDepth.Depth);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled distributed message: {code} from {connection.Username} ({connection.IPEndPoint}); {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling distributed message: {code} from {connection.Username} ({connection.IPEndPoint}); {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Handles outgoing messages, post send.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance to which the message was sent.</param>
        /// <param name="args">The message event args.</param>
        public void HandleMessageWritten(object sender, MessageEventArgs args)
        {
            var code = new MessageReader<MessageCode.Distributed>(args.Message).ReadCode();
            Diagnostic.Debug($"Distributed message sent: {code}");
        }

        /// <summary>
        ///     Handles embedded messages from the server.
        /// </summary>
        /// <param name="message">The message.</param>
        public async void HandleEmbeddedMessage(byte[] message)
        {
            var code = MessageCode.Distributed.Unknown;

            try
            {
                var embeddedMessage = EmbeddedMessage.FromByteArray(message);
                code = embeddedMessage.DistributedCode;
                var distributedMessage = embeddedMessage.DistributedMessage;

                switch (code)
                {
                    case MessageCode.Distributed.SearchRequest:
                        // receiving a SearchRequest/3 from the server as an embedded message indicates that we are
                        // operating as a branch root on the distributed network.
                        SoulseekClient.DistributedConnectionManager.PromoteToBranchRoot();

                        var searchRequest = DistributedSearchRequest.FromByteArray(distributedMessage);

                        _ = SoulseekClient.DistributedConnectionManager.BroadcastMessageAsync(message).ConfigureAwait(false);

                        await SoulseekClient.SearchResponder.TryRespondAsync(searchRequest.Username, searchRequest.Token, searchRequest.Query).ConfigureAwait(false);

                        break;
                    default:
                        Diagnostic.Debug($"Unhandled embedded message: {code}; {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling embedded message: {code}; {ex.Message}", ex);
            }
        }
    }
}