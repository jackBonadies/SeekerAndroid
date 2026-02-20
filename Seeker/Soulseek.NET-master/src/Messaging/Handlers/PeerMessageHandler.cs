// <copyright file="PeerMessageHandler.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

    /// <summary>
    ///     Handles incoming messages from peer connections.
    /// </summary>
    internal sealed class PeerMessageHandler : IPeerMessageHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerMessageHandler"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public PeerMessageHandler(
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

        /// <summary>
        ///     Occurs when a user reports that a download has been denied.
        /// </summary>
        public event EventHandler<DownloadDeniedEventArgs> DownloadDenied;

        /// <summary>
        ///     Occurs when a user reports that a download has failed.
        /// </summary>
        public event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

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
            var code = new MessageReader<MessageCode.Peer>(message).ReadCode();

            Diagnostic.Debug($"Peer message received: {code} from {connection.Username} ({connection.IPEndPoint}) (id: {connection.Id})");

            try
            {
                switch (code)
                {
                    case MessageCode.Peer.SearchResponse:
                        var searchResponse = SearchResponseFactory.FromByteArray(message);

                        if (SoulseekClient.Searches.TryGetValue(searchResponse.Token, out var search))
                        {
                            search.TryAddResponse(searchResponse);
                        }

                        break;

                    case MessageCode.Peer.BrowseResponse:
                        var browseWaitKey = new WaitKey(MessageCode.Peer.BrowseResponse, connection.Username);

                        try
                        {
                            SoulseekClient.Waiter.Complete(browseWaitKey, BrowseResponseFactory.FromByteArray(message));
                        }
                        catch (Exception ex)
                        {
                            SoulseekClient.Waiter.Throw(browseWaitKey, new MessageReadException("The peer returned an invalid browse response", ex));
                            throw;
                        }

                        break;

                    case MessageCode.Peer.InfoRequest:
                        UserInfo outgoingInfo;

                        try
                        {
                            outgoingInfo = await SoulseekClient.Options
                                .UserInfoResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            outgoingInfo = await new SoulseekClientOptions()
                                .UserInfoResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);

                            Diagnostic.Warning($"Failed to resolve user info response: {ex.Message}", ex);
                        }

                        await connection.WriteAsync(outgoingInfo.ToByteArray()).ConfigureAwait(false);
                        Diagnostic.Info($"User info sent to {connection.Username}");

                        break;

                    case MessageCode.Peer.SearchRequest:
                        var searchRequest = PeerSearchRequest.FromByteArray(message);

                        if (SoulseekClient.Options.SearchResponseResolver == default)
                        {
                            break;
                        }

                        try
                        {
                            var peerSearchResponse = await SoulseekClient.Options.SearchResponseResolver(connection.Username, searchRequest.Token, SearchQuery.FromText(searchRequest.Query)).ConfigureAwait(false);

                            if (peerSearchResponse is RawSearchResponse rawSearchResponse)
                            {
                                await connection.WriteAsync(rawSearchResponse.Length, rawSearchResponse.Stream).ConfigureAwait(false);

                                try
                                {
                                    rawSearchResponse.Stream.Dispose();
                                }
                                catch
                                {
                                    // noop
                                }
                            }
                            else if (peerSearchResponse != null && peerSearchResponse.FileCount + peerSearchResponse.LockedFileCount > 0)
                            {
                                await connection.WriteAsync(peerSearchResponse.ToByteArray()).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Error resolving search response for query '{searchRequest.Query}' requested by {connection.Username} with token {searchRequest.Token}: {ex.Message}", ex);
                        }

                        break;

                    case MessageCode.Peer.BrowseRequest:
                        BrowseResponse browseResponse;

                        try
                        {
                            browseResponse = await SoulseekClient.Options.BrowseResponseResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            browseResponse = await new SoulseekClientOptions()
                                .BrowseResponseResolver(connection.Username, connection.IPEndPoint).ConfigureAwait(false);

                            Diagnostic.Warning($"Failed to resolve browse response: {ex.Message}", ex);
                        }

                        if (browseResponse is RawBrowseResponse rawBrowseResponse)
                        {
                            await connection.WriteAsync(rawBrowseResponse.Length, rawBrowseResponse.Stream).ConfigureAwait(false);

                            try
                            {
                                rawBrowseResponse.Stream.Dispose();
                            }
                            catch
                            {
                                // noop
                            }
                        }
                        else
                        {
                            await connection.WriteAsync(browseResponse.ToByteArray()).ConfigureAwait(false);
                        }

                        Diagnostic.Info($"Share contents sent to {connection.Username}");

                        break;

                    case MessageCode.Peer.FolderContentsRequest:
                        var folderContentsRequest = FolderContentsRequest.FromByteArray(message);
                        IEnumerable<Directory> outgoingFolderContents = null;

                        try
                        {
                            outgoingFolderContents = await SoulseekClient.Options.DirectoryContentsResolver(
                                connection.Username,
                                connection.IPEndPoint,
                                folderContentsRequest.Token,
                                folderContentsRequest.DirectoryName).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to resolve directory contents response: {ex.Message}", ex);
                        }

                        if (outgoingFolderContents != null)
                        {
                            var folderContentsResponseMessage = new FolderContentsResponse(folderContentsRequest.Token, folderContentsRequest.DirectoryName, outgoingFolderContents);

                            await connection.WriteAsync(folderContentsResponseMessage).ConfigureAwait(false);
                            Diagnostic.Info($"Folder contents for {folderContentsRequest.DirectoryName} sent to {connection.Username}");
                        }

                        break;

                    case MessageCode.Peer.FolderContentsResponse:
                        var folderContentsResponse = FolderContentsResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.FolderContentsResponse, connection.Username, folderContentsResponse.Token), folderContentsResponse.Directories);
                        break;

                    case MessageCode.Peer.InfoResponse:
                        var incomingInfo = UserInfoResponseFactory.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.InfoResponse, connection.Username), incomingInfo);
                        break;

                    case MessageCode.Peer.TransferResponse:
                        var transferResponse = TransferResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.TransferResponse, connection.Username, transferResponse.Token), transferResponse);
                        break;

                    case MessageCode.Peer.QueueDownload:
                        var queueDownloadRequest = QueueDownloadRequest.FromByteArray(message);

                        var (queueRejected, queueRejectionMessage) =
                            await TryEnqueueDownloadAsync(connection.Username, connection.IPEndPoint, queueDownloadRequest.Filename).ConfigureAwait(false);

                        if (queueRejected)
                        {
                            await connection.WriteAsync(new UploadDenied(queueDownloadRequest.Filename, queueRejectionMessage)).ConfigureAwait(false);
                        }
                        else
                        {
                            await TrySendPlaceInQueueAsync(connection, queueDownloadRequest.Filename).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Peer.TransferRequest:
                        var transferRequest = TransferRequest.FromByteArray(message);

                        if (transferRequest.Direction == TransferDirection.Upload)
                        {
                            if (!SoulseekClient.DownloadDictionary.IsEmpty && SoulseekClient.DownloadDictionary.Values.Any(d => d.Username == connection.Username && d.Filename == transferRequest.Filename))
                            {
                                SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, transferRequest.Filename), transferRequest);
                            }
                            else
                            {
                                // reject the transfer with an empty reason.  it was probably cancelled, but we can't be sure.
                                Diagnostic.Debug($"Rejecting unknown upload from {connection.Username} for {transferRequest.Filename} with token {transferRequest.Token}");
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, "Cancelled")).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            var (transferRejected, transferRejectionMessage) = await TryEnqueueDownloadAsync(connection.Username, connection.IPEndPoint, transferRequest.Filename).ConfigureAwait(false);

                            if (transferRejected)
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, transferRejectionMessage)).ConfigureAwait(false);
                                await connection.WriteAsync(new UploadDenied(transferRequest.Filename, transferRejectionMessage)).ConfigureAwait(false);
                            }
                            else
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, "Queued")).ConfigureAwait(false);
                                await TrySendPlaceInQueueAsync(connection, transferRequest.Filename).ConfigureAwait(false);
                            }
                        }

                        break;

                    case MessageCode.Peer.UploadDenied:
                        var uploadDeniedResponse = UploadDenied.FromByteArray(message);

                        Diagnostic.Debug($"Download of {uploadDeniedResponse.Filename} from {connection.Username} was denied: {uploadDeniedResponse.Message}");
                        SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, uploadDeniedResponse.Filename), new TransferRejectedException(uploadDeniedResponse.Message));

                        DownloadDenied?.Invoke(this, new DownloadDeniedEventArgs(connection.Username, uploadDeniedResponse.Filename, uploadDeniedResponse.Message));
                        break;

                    case MessageCode.Peer.PlaceInQueueResponse:
                        var placeInQueueResponse = PlaceInQueueResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.PlaceInQueueResponse, connection.Username, placeInQueueResponse.Filename), placeInQueueResponse);
                        break;

                    case MessageCode.Peer.PlaceInQueueRequest:
                        var placeInQueueRequest = PlaceInQueueRequest.FromByteArray(message);
                        await TrySendPlaceInQueueAsync(connection, placeInQueueRequest.Filename).ConfigureAwait(false);

                        break;

                    case MessageCode.Peer.UploadFailed:
                        var uploadFailedResponse = UploadFailed.FromByteArray(message);

                        var msg = $"Download of {uploadFailedResponse.Filename} reported as failed by {connection.Username}";
                        Diagnostic.Debug(msg);

                        SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, uploadFailedResponse.Filename), new TransferException(msg));

                        DownloadFailed?.Invoke(this, new DownloadFailedEventArgs(connection.Username, uploadFailedResponse.Filename));
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled peer message: {code} from {connection.Username} ({connection.IPEndPoint}); {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling peer message: {code} from {connection.Username} ({connection.IPEndPoint}); {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Handles the receipt of incoming messages, prior to the body having been read and parsed.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="args">The message receipt event args.</param>
        public void HandleMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            var connection = (IMessageConnection)sender;
            var code = (MessageCode.Peer)BitConverter.ToInt32(args.Code, 0);

            try
            {
                switch (code)
                {
                    case MessageCode.Peer.BrowseResponse:
                        var key = new WaitKey(Constants.WaitKey.BrowseResponseConnection, connection.Username);
                        SoulseekClient.Waiter.Complete(key, (EventArgs: args, Connection: connection));
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling peer message: {code} from {connection.Username} ({connection.IPEndPoint}); {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Handles outgoing messages, post send.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance to which the message was sent.</param>
        /// <param name="args">The message event args.</param>
        public void HandleMessageWritten(object sender, MessageEventArgs args)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Peer>(args.Message).ReadCode();
            Diagnostic.Debug($"Peer message sent: {code} ({connection.IPEndPoint}) (id: {connection.Id})");
        }

        private async Task<(bool Rejected, string RejectionMessage)> TryEnqueueDownloadAsync(string username, IPEndPoint ipEndPoint, string filename)
        {
            bool rejected = false;
            string rejectionMessage = string.Empty;

            try
            {
                await SoulseekClient.Options
                    .EnqueueDownload(username, ipEndPoint, filename).ConfigureAwait(false);
            }
            catch (DownloadEnqueueException ex)
            {
                // pass the exception message through to the remote user only if EnqueueDownloadException is thrown
                rejected = true;
                rejectionMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Failed to invoke QueueDownload action: {ex.Message}", ex);

                // if any other exception is thrown, return a generic message. do this to avoid exposing potentially sensitive
                // information that may be contained in the Exception message (filesystem details, etc.)
                rejected = true;
                rejectionMessage = "Enqueue failed due to internal error";
            }

            return (rejected, rejectionMessage);
        }

        private async Task TrySendPlaceInQueueAsync(IMessageConnection connection, string filename)
        {
            int? placeInQueue = null;

            try
            {
                placeInQueue = await SoulseekClient.Options.PlaceInQueueResolver(connection.Username, connection.IPEndPoint, filename).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Failed to resolve place in queue for file {filename} from {connection.Username}: {ex.Message}", ex);
                return;
            }

            if (placeInQueue.HasValue)
            {
                await connection.WriteAsync(new PlaceInQueueResponse(filename, placeInQueue.Value)).ConfigureAwait(false);
            }
        }
    }
}