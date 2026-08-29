// <copyright file="ISoulseekClient.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public interface ISoulseekClient : IDisposable, IDiagnosticGenerator
    {
        /// <summary>
        ///     Occurs when a browse response receives data.
        /// </summary>
        event EventHandler<BrowseProgressUpdatedEventArgs> BrowseProgressUpdated;

        /// <summary>
        ///     Occurs when the client connects.
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        ///     Occurs when the client is demoted from a branch root on the distributed network.
        /// </summary>
        event EventHandler DemotedFromDistributedBranchRoot;

        /// <summary>
        ///     Occurs when the client disconnects.
        /// </summary>
        event EventHandler<SoulseekClientDisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     Occurs when a child connection is added.
        /// </summary>
        event EventHandler<DistributedChildEventArgs> DistributedChildAdded;

        /// <summary>
        ///     Occurs when a child connection is disconnected.
        /// </summary>
        event EventHandler<DistributedChildEventArgs> DistributedChildDisconnected;

        /// <summary>
        ///     Occurs when the server requests a distributed network reset.
        /// </summary>
        event EventHandler DistributedNetworkReset;

        /// <summary>
        ///     Occurs when the state of the distributed network changes.
        /// </summary>
        event EventHandler<DistributedNetworkInfo> DistributedNetworkStateChanged;

        /// <summary>
        ///     Occurs when a new parent is adopted.
        /// </summary>
        event EventHandler<DistributedParentEventArgs> DistributedParentAdopted;

        /// <summary>
        ///     Occurs when the parent is disconnected.
        /// </summary>
        event EventHandler<DistributedParentEventArgs> DistributedParentDisconnected;

        /// <summary>
        ///     Occurs when a user reports that a download has been denied.
        /// </summary>
        event EventHandler<DownloadDeniedEventArgs> DownloadDenied;

        /// <summary>
        ///     Occurs when a user reports that a download has failed.
        /// </summary>
        event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        ///     Occurs when the server sends a list of excluded ("banned") search phrases.
        /// </summary>
        event EventHandler<IReadOnlyCollection<string>> ExcludedSearchPhrasesReceived;

        /// <summary>
        ///     Occurs when a global message is received.
        /// </summary>
        event EventHandler<string> GlobalMessageReceived;

        /// <summary>
        ///     Occurs when the client is logged in.
        /// </summary>
        event EventHandler LoggedIn;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when the currently logged in user is granted membership to a private room.
        /// </summary>
        event EventHandler<string> PrivateRoomMembershipAdded;

        /// <summary>
        ///     Occurs when the currently logged in user has membership to a private room revoked.
        /// </summary>
        event EventHandler<string> PrivateRoomMembershipRemoved;

        /// <summary>
        ///     Occurs when a list of moderated users for a private room is received.
        /// </summary>
        event EventHandler<RoomInfo> PrivateRoomModeratedUserListReceived;

        /// <summary>
        ///     Occurs when the currently logged in user is granted moderator status in a private room.
        /// </summary>
        event EventHandler<string> PrivateRoomModerationAdded;

        /// <summary>
        ///     Occurs when the currently logged in user has moderator status removed in a private room.
        /// </summary>
        event EventHandler<string> PrivateRoomModerationRemoved;

        /// <summary>
        ///     Occurs when a list of users for a private room is received.
        /// </summary>
        event EventHandler<RoomInfo> PrivateRoomUserListReceived;

        /// <summary>
        ///     Occurs when the server sends a list of privileged users.
        /// </summary>
        event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;

        /// <summary>
        ///     Occurs when the server sends a notification of new user privileges.
        /// </summary>
        event EventHandler<PrivilegeNotificationReceivedEventArgs> PrivilegeNotificationReceived;

        /// <summary>
        ///     Occurs when the client has been promoted to a branch root on the distributed network.
        /// </summary>
        event EventHandler PromotedToDistributedBranchRoot;

        /// <summary>
        ///     Occurs when a public chat message is received.
        /// </summary>
        event EventHandler<PublicChatMessageReceivedEventArgs> PublicChatMessageReceived;

        /// <summary>
        ///     Occurs when a user joins a chat room.
        /// </summary>
        event EventHandler<RoomJoinedEventArgs> RoomJoined;

        /// <summary>
        ///     Occurs when a user leaves a chat room.
        /// </summary>
        event EventHandler<RoomLeftEventArgs> RoomLeft;

        /// <summary>
        ///     Occurs when the server sends a list of chat rooms.
        /// </summary>
        event EventHandler<RoomList> RoomListReceived;

        /// <summary>
        ///     Occurs when a chat room message is received.
        /// </summary>
        event EventHandler<RoomMessageReceivedEventArgs> RoomMessageReceived;

        /// <summary>
        ///     Occurs when a chat room ticker is added.
        /// </summary>
        event EventHandler<RoomTickerAddedEventArgs> RoomTickerAdded;

        /// <summary>
        ///     Occurs when the server sends a list of tickers for a chat room.
        /// </summary>
        event EventHandler<RoomTickerListReceivedEventArgs> RoomTickerListReceived;

        /// <summary>
        ///     Occurs when a chat room ticker is removed.
        /// </summary>
        event EventHandler<RoomTickerRemovedEventArgs> RoomTickerRemoved;

        /// <summary>
        ///     Occurs when a search request is received.
        /// </summary>
        event EventHandler<SearchRequestEventArgs> SearchRequestReceived;

        /// <summary>
        ///     Occurs when the response to a search request is delivered.
        /// </summary>
        event EventHandler<SearchRequestResponseEventArgs> SearchResponseDelivered;

        /// <summary>
        ///     Occurs when the delivery of a response to a search request fails.
        /// </summary>
        event EventHandler<SearchRequestResponseEventArgs> SearchResponseDeliveryFailed;

        /// <summary>
        ///     Occurs when a new search response is received.
        /// </summary>
        event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Occurs when a search changes state.
        /// </summary>
        event EventHandler<SearchStateChangedEventArgs> SearchStateChanged;

        /// <summary>
        ///     Occurs when the server sends information upon login.
        /// </summary>
        event EventHandler<ServerInfo> ServerInfoReceived;

        /// <summary>
        ///     Occurs when the client changes state.
        /// </summary>
        event EventHandler<SoulseekClientStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Occurs when an active transfer sends or receives data.
        /// </summary>
        event EventHandler<TransferProgressUpdatedEventArgs> TransferProgressUpdated;

        /// <summary>
        ///     Occurs when a transfer changes state.
        /// </summary>
        event EventHandler<TransferStateChangedEventArgs> TransferStateChanged;

        /// <summary>
        ///     Occurs when a user fails to connect.
        /// </summary>
        event EventHandler<UserCannotConnectEventArgs> UserCannotConnect;

        /// <summary>
        ///     Occurs when a user's statistics change.
        /// </summary>
        public event EventHandler<UserStatistics> UserStatisticsChanged;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        /// <remarks>Add a user to the server watch list with <see cref="WatchUserAsync(string, CancellationToken?)"/>.</remarks>
        event EventHandler<UserStatus> UserStatusChanged;

        /// <summary>
        ///     Gets the unresolved server address.
        /// </summary>
        string Address { get; }

        /// <summary>
        ///     Gets information about the distributed network.
        /// </summary>
        DistributedNetworkInfo DistributedNetwork { get; }

        /// <summary>
        ///     Gets a snapshot of current downloads.
        /// </summary>
        IReadOnlyCollection<Transfer> Downloads { get; }

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the resolved server endpoint.
        /// </summary>
        IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        SoulseekClientOptions Options { get; }

        /// <summary>
        ///     Gets the server port.
        /// </summary>
        int? Port { get; }

        /// <summary>
        ///     Gets information sent by the server upon login.
        /// </summary>
        ServerInfo ServerInfo { get; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        SoulseekClientStates State { get; }

        /// <summary>
        ///     Gets a snapshot of current uploads.
        /// </summary>
        IReadOnlyCollection<Transfer> Uploads { get; }

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        string Username { get; }

        /// <summary>
        ///     Asynchronously sends a private message acknowledgement for the specified <paramref name="privateMessageId"/>.
        /// </summary>
        /// <param name="privateMessageId">The unique id of the private message to acknowledge.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="privateMessageId"/> is less than zero.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task AcknowledgePrivateMessageAsync(int privateMessageId, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously sends a privilege notification acknowledgement for the specified <paramref name="privilegeNotificationId"/>.
        /// </summary>
        /// <param name="privilegeNotificationId">The unique id of the privilege notification to acknowledge.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="privilegeNotificationId"/> is less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task AcknowledgePrivilegeNotificationAsync(int privilegeNotificationId, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="username"/> to the list of members in the specified private <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room to which to add the user.</param>
        /// <param name="username">The username of the user to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task AddPrivateRoomMemberAsync(string roomName, string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="username"/> to the list of moderators in the specified private <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room to which to add the user.</param>
        /// <param name="username">The username of the user to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task AddPrivateRoomModeratorAsync(string roomName, string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <remarks>
        ///     By default, this operation will not time out locally, but rather will wait until the remote connection is broken.
        ///     If a local timeout is desired, specify an appropriate <see cref="CancellationToken"/>.
        /// </remarks>
        /// <param name="username">The user to browse.</param>
        /// <param name="options">The operation <see cref="BrowseOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the fetched list of files.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<BrowseResponse> BrowseAsync(string username, BrowseOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously changes the password for the currently logged in user.
        /// </summary>
        /// <param name="password">The new password.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="password"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task ChangePasswordAsync(string password, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously connects the client to the default server and logs in using the specified
        ///     <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when a connection is already in the process of being established.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
        /// <exception cref="ListenException">Thrown when binding a listener to the specified address and/or port fails.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="LoginRejectedException">Thrown when the login is rejected by the remote server.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task ConnectAsync(string username, string password, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously connects the client to the specified server <paramref name="address"/> and <paramref name="port"/>
        ///     and logs in using the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="address"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="port"/> is not within the valid port range 0-65535.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when a connection is already in the process of being established.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
        /// <exception cref="AddressException">Thrown when the provided address can't be resolved.</exception>
        /// <exception cref="ListenException">Thrown when binding a listener to the specified address and/or port fails.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="LoginRejectedException">Thrown when the login is rejected by the remote server.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task ConnectAsync(string address, int port, string username, string password, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously establishes and caches a connection to the specified <paramref name="username"/>. If a connection
        ///     is already cached, it is returned unless the <paramref name="invalidateCache"/> option is specified.
        /// </summary>
        /// <param name="username">The user to which to connect.</param>
        /// <param name="invalidateCache">
        ///     A value indicating whether to establish a new connection, regardless of whether one is cached.
        /// </param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task ConnectToUserAsync(string username, bool invalidateCache = false, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Disconnects the client from the server.
        /// </summary>
        /// <param name="message">An optional message describing the reason the client is being disconnected.</param>
        /// <param name="exception">An optional Exception causing the disconnect.</param>
        void Disconnect(string message = null, Exception exception = null);

        /// <summary>
        ///     <para>
        ///         Asynchronously downloads the specified <paramref name="remoteFilename"/> from the specified
        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
        ///         <paramref name="cancellationToken"/> to the specified <paramref name="localFilename"/>.
        ///     </para>
        ///     <para>
        ///         If the destination file exists and <paramref name="startOffset"/> is greater than zero, the existing file is
        ///         appended. Otherwise, it is overwritten.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     If <paramref name="size"/> is omitted, the size provided by the remote client is used. Transfers initiated without
        ///     specifying a size are limited to 4gb or less due to a shortcoming of the SoulseekQt client.
        /// </remarks>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="remoteFilename">The file to download, as reported by the remote user.</param>
        /// <param name="localFilename">The fully qualified filename of the destination file.</param>
        /// <param name="size">The size of the file, in bytes.</param>
        /// <param name="startOffset">The offset at which to start the download, in bytes.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///     The Task representing the asynchronous operation, including the transfer context and a byte array containing the
        ///     file contents.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/>, <paramref name="remoteFilename"/>, or
        ///     <paramref name="localFilename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="startOffset"/> is greater than zero but <paramref name="size"/> is not specified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the specified <paramref name="size"/> or <paramref name="startOffset"/> is less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="remoteFilename"/> from the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="TransferSizeMismatchException">
        ///     Thrown when the remote size of the transfer is different from the specified size.
        /// </exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Transfer> DownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously downloads the specified <paramref name="remoteFilename"/> from the specified
        ///     <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
        ///     <paramref name="cancellationToken"/> to the <see cref="Stream"/> created by the specified <paramref name="outputStreamFactory"/>.
        /// </summary>
        /// <remarks>
        ///     If <paramref name="size"/> is omitted, the size provided by the remote client is used. Transfers initiated without
        ///     specifying a size are limited to 4gb or less due to a shortcoming of the SoulseekQt client.
        /// </remarks>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="remoteFilename">The file to download, as reported by the remote user.</param>
        /// <param name="outputStreamFactory">A delegate used to create the stream to which to write the file contents.</param>
        /// <param name="size">The size of the file, in bytes.</param>
        /// <param name="startOffset">The offset at which to start the download, in bytes.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the transfer context.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="remoteFilename"/> is null, empty, or consists only
        ///     of whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="startOffset"/> is greater than zero but <paramref name="size"/> is not specified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the specified <paramref name="size"/> or <paramref name="startOffset"/> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the specified <paramref name="outputStreamFactory"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="remoteFilename"/> from the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="TransferSizeMismatchException">
        ///     Thrown when the remote size of the transfer is different from the specified size.
        /// </exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously removes the currently logged in user from the list of members in the specified private <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room for which the membership is to be dropped.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task DropPrivateRoomMembershipAsync(string roomName, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously removes the currently logged in user from the ownership of the specified private <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room for which the ownership is to be dropped.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task DropPrivateRoomOwnershipAsync(string roomName, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     <para>
        ///         Asynchronously enqueues a download for the specified <paramref name="remoteFilename"/> from the specified
        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
        ///         <paramref name="cancellationToken"/>. to the specified <paramref name="localFilename"/>.
        ///     </para>
        ///     <para>
        ///         If the destination file exists and <paramref name="startOffset"/> is greater than zero, the existing file is
        ///         appended. Otherwise, it is overwritten.
        ///     </para>
        ///     <para>
        ///         Functionally the same as
        ///         <see cref="DownloadAsync(string, string, string, long?, long, int?, TransferOptions, CancellationToken?)"/>,
        ///         but returns the download Task as soon as the download has been remotely enqueued.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If <paramref name="size"/> is omitted, the size provided by the remote client is used. Transfers initiated
        ///         without specifying a size are limited to 4gb or less due to a shortcoming of the SoulseekQt client.
        ///     </para>
        ///     <para>
        ///         The operation will be blocked if <see cref="SoulseekClientOptions.MaximumConcurrentDownloads"/> is exceeded,
        ///         and will not continue until the number of active downloads has decreased below the limit.
        ///     </para>
        /// </remarks>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="remoteFilename">The file to download, as reported by the remote user.</param>
        /// <param name="localFilename">The fully qualified filename of the destination file.</param>
        /// <param name="size">The size of the file, in bytes.</param>
        /// <param name="startOffset">The offset at which to start the download, in bytes.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///     The Task representing the asynchronous operation, including the transfer context and a byte array containing the
        ///     file contents.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/>, <paramref name="remoteFilename"/>, or
        ///     <paramref name="localFilename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the specified <paramref name="size"/> or <paramref name="startOffset"/> is less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="remoteFilename"/> from the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="TransferSizeMismatchException">
        ///     Thrown when the remote size of the transfer is different from the specified size.
        /// </exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     <para>
        ///         Asynchronously enqueues a download for the specified <paramref name="remoteFilename"/> from the specified
        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified
        ///         <paramref name="cancellationToken"/> to the <see cref="Stream"/> created by the specified <paramref name="outputStreamFactory"/>.
        ///     </para>
        ///     <para>
        ///         Functionally the same as
        ///         <see cref="DownloadAsync(string, string, Func{Task{Stream}}, long?, long, int?, TransferOptions, CancellationToken?)"/>,
        ///         but returns the download Task as soon as the download has been remotely enqueued.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If <paramref name="size"/> is omitted, the size provided by the remote client is used. Transfers initiated
        ///         without specifying a size are limited to 4gb or less due to a shortcoming of the SoulseekQt client.
        ///     </para>
        ///     <para>
        ///         The operation will be blocked if <see cref="SoulseekClientOptions.MaximumConcurrentDownloads"/> is exceeded,
        ///         and will not continue until the number of active downloads has decreased below the limit.
        ///     </para>
        /// </remarks>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="remoteFilename">The file to download, as reported by the remote user.</param>
        /// <param name="outputStreamFactory">A delegate used to create the stream to which to write the file contents.</param>
        /// <param name="size">The size of the file, in bytes.</param>
        /// <param name="startOffset">The offset at which to start the download, in bytes.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous download operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="remoteFilename"/> is null, empty, or consists only
        ///     of whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the specified <paramref name="size"/> or <paramref name="startOffset"/> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the specified <paramref name="outputStreamFactory"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="remoteFilename"/> from the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="TransferSizeMismatchException">
        ///     Thrown when the remote size of the transfer is different from the specified size.
        /// </exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     <para>
        ///         Asynchronously enqueues an upload for the specified <paramref name="remoteFilename"/> from the specified
        ///         <paramref name="localFilename"/> to the the specified <paramref name="username"/> using the specified unique
        ///         <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        ///     </para>
        ///     <para>
        ///         Functionally the same as
        ///         <see cref="UploadAsync(string, string, string, int?, TransferOptions, CancellationToken?)"/>, but returns the
        ///         upload Task as soon as the upload has been locally enqueued.
        ///     </para>
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
        /// <param name="localFilename">The fully qualified filename of the file to upload.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous upload operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/>, <paramref name="remoteFilename"/>, or
        ///     <paramref name="localFilename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     Thrown when the specified <paramref name="localFilename"/> can not be found.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="remoteFilename"/> to the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     <para>
        ///         Asynchronously enqueues an upload for the specified <paramref name="remoteFilename"/> from the
        ///         <see cref="Stream"/> created by the specified <paramref name="inputStreamFactory"/> to the the specified
        ///         <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        ///     </para>
        ///     <para>
        ///         Functionally the same as
        ///         <see cref="UploadAsync(string, string, long, Func{long, Task{Stream}}, int?, TransferOptions, CancellationToken?)"/>,
        ///         but returns the upload Task as soon as the upload has been locally enqueued.
        ///     </para>
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
        /// <param name="size">The size of the file to upload, in bytes.</param>
        /// <param name="inputStreamFactory">A delegate used to create the stream from which to retrieve the file contents.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous upload operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="remoteFilename"/> is null, empty, or consists only
        ///     of whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="size"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the specified <paramref name="inputStreamFactory"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="remoteFilename"/> to the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the contents of the specified <paramref name="directoryName"/> from the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the directory contents.</param>
        /// <param name="directoryName">The name of the directory to fetch.</param>
        /// <param name="token">The unique token for the operation.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the directory contents.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="directoryName"/> is null, empty, or consists only
        ///     of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<IReadOnlyCollection<Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the current place of the specified <paramref name="filename"/> in the queue of the
        ///     specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user whose queue to check.</param>
        /// <param name="filename">The file to check.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the current place of the file in the queue.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TransferNotFoundException">Thrown when a corresponding download is not active.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Gets the next token for use in client operations.
        /// </summary>
        /// <remarks>
        ///     <para>Tokens are returned sequentially and the token value rolls over to 0 when it has reached <see cref="int.MaxValue"/>.</para>
        ///     <para>This operation is thread safe.</para>
        /// </remarks>
        /// <returns>The next token.</returns>
        /// <threadsafety instance="true"/>
        int GetNextToken();

        /// <summary>
        ///     Asynchronously fetches the number of remaining days of privileges of the currently logged in user.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<int> GetPrivilegesAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the list of chat rooms on the server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the list of server rooms.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<RoomList> GetRoomListAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the IP endpoint of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the connection information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the connection information.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="UserEndPointException">Thrown when an exception is encountered during the operation.</exception>
        Task<IPEndPoint> GetUserEndPointAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches information about the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the information response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<UserInfo> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the status of the privileges of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user for which to fetch privileges.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<bool> GetUserPrivilegedAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches statistics for the specified <paramref name="username"/>.
        /// </summary>
        /// <remarks>
        ///     Statistics are returned for any given username, regardless of online status, even if no user with that name exists
        ///     or has ever existed. All values are zero in the case of an unknown user, and presumably the last reported values
        ///     are returned when a user exists but is offline.
        /// </remarks>
        /// <param name="username">The username of the user for which to fetch statistics.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<UserStatistics> GetUserStatisticsAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the status of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user for which to fetch the status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<UserStatus> GetUserStatusAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously grants the specified <paramref name="username"/> the specified number of days
        ///     <paramref name="days"/> of privileged status.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         There is no immediate or direct response for this operation, and because error conditions may only be inferred
        ///         by monitoring private messages as described below, this library defers this inferrence to implementing code.
        ///         This method returns after the command is dispatched and does not monitor for any type of response.
        ///     </para>
        ///     <para>
        ///         If the operation succeeds, there may or may not eventually be a <see cref="PrivilegedUserNotification"/> event
        ///         for the specified user.
        ///     </para>
        ///     <para>
        ///         If the operation fails, the server will send a private message from the username "server", with the IsAdmin
        ///         flag set, and with one of the messages:
        ///         <list type="bullet">
        ///             <item>"User {specified username} does not exist."</item>
        ///             <item>"Youcurrently do not have any privileges to give." (note the spacing in Youcurrently)</item>
        ///             <item>
        ///                 "You don't have enough privilege credit for this operation. Either give away less privilege or donate
        ///                 in the Web tab to receive more credit."
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <param name="username">The user to which to grant privileges.</param>
        /// <param name="days">The number of days of privileged status to grant.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the specified <paramref name="days"/> are less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task GrantUserPrivilegesAsync(string username, int days, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously joins the chat room with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <remarks>When successful, a corresponding <see cref="RoomJoined"/> event will be raised.</remarks>
        /// <param name="roomName">The name of the chat room to join.</param>
        /// <param name="isPrivate">A value indicating whether the room is private.</param>
        /// <param name="cancellationToken">The token to minotor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="RoomJoinForbiddenException">Thrown when the server rejects the request.</exception>
        /// <exception cref="NoResponseException">Thrown when the server does not respond to the request.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<RoomData> JoinRoomAsync(string roomName, bool isPrivate = false, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously leaves the chat room with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <remarks>When successful, a corresponding <see cref="RoomLeft"/> event will be raised.</remarks>
        /// <param name="roomName">The name of the chat room to leave.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="NoResponseException">Thrown when the server does not respond to the request.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task LeaveRoomAsync(string roomName, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously pings the server to check connectivity.
        /// </summary>
        /// <remarks>The server doesn't seem to be responding; this may have been deprecated.</remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the response time in miliseconds.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<long> PingServerAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously applies the specified <paramref name="patch"/> to the client options.
        /// </summary>
        /// <remarks>
        ///     <para>Options that can be changed without reinstantiation of the client are limited to those included in <see cref="SoulseekClientOptionsPatch"/>.</para>
        ///     <para>
        ///         If the client is connected when this method completes, a re-connect of the client may be required, depending
        ///         on the options that were changed. The return value of this method indicates when a re-connect is necessary.
        ///         The following options require a re-connect under these circumstances:
        ///         <list type="bullet">
        ///             <item>ServerConnectionOptions</item>
        ///             <item>DistributedConnectionOptions</item>
        ///             <item>EnableDistributedNetwork (transition from enabled to disabled only)</item>
        ///         </list>
        ///     </para>
        ///     <para>
        ///         Enabling or disabling the listener or changing the listen address and/or port takes effect immediately.
        ///         Remaining options will be updated immediately, but any objects instantiated will not be updated (for example,
        ///         established connections will retain the options with which they were instantiated).
        ///     </para>
        /// </remarks>
        /// <param name="patch">A patch containing the updated options.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///     The Task representing the asynchronous operation, including a value indicating whether a server reconnect is
        ///     required for the new options to fully take effect.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="patch"/> is null.</exception>
        /// <exception cref="ListenException">Thrown when binding a listener to the specified address and/or port fails.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<bool> ReconfigureOptionsAsync(SoulseekClientOptionsPatch patch, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously removes the specified <paramref name="username"/> from the list of members in the specified private <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room from which to remove the user.</param>
        /// <param name="username">The username of the user to remove.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task RemovePrivateRoomMemberAsync(string roomName, string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously removes the specified <paramref name="username"/> from the list of moderators in the specified
        ///     private <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room from which to remove the user.</param>
        /// <param name="username">The username of the user to remove.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task RemovePrivateRoomModeratorAsync(string roomName, string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="query"/> using the specified unique
        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="scope">the search scope.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the search context and results.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the search text of the specified <paramref name="query"/> is null, empty, or consists of only whitespace..
        /// </exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="query"/> using the specified unique
        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="responseHandler">The delegate to invoke for each response.</param>
        /// <param name="scope">the search scope.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the search context.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the search text of the specified <paramref name="query"/> is null, empty, or consists of only whitespace..
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the specified <paramref name="responseHandler"/> delegate is null.
        /// </exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        Task<Search> SearchAsync(SearchQuery query, Action<SearchResponse> responseHandler, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously sends the specified private <paramref name="message"/> to the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="message"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task SendPrivateMessageAsync(string username, string message, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously sends the specified chat room <paramref name="message"/> to the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The name of the room to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="message"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task SendRoomMessageAsync(string roomName, string message, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously informs the server of the most recently completed upload transfer <paramref name="speed"/>.
        /// </summary>
        /// <param name="speed">The speed of the most recently completed upload transfer, in bytes per second.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value of <paramref name="speed"/> is less than or equal to zero.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task SendUploadSpeedAsync(int speed, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously sets a chat room ticker containing the specified <paramref name="message"/> in the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The name of the room in which the ticker is to be set.</param>
        /// <param name="message">The ticker message.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="message"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task SetRoomTickerAsync(string roomName, string message, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously informs the server of the number of shared <paramref name="directories"/> and <paramref name="files"/>.
        /// </summary>
        /// <param name="directories">The number of shared directories.</param>
        /// <param name="files">The number of shared files.</param>
        /// <param name="cancellationToken">The token to monitor for cancelation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value of <paramref name="directories"/> or <paramref name="files"/> is less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task SetSharedCountsAsync(int directories, int files, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously informs the server of the current online <paramref name="status"/> of the client.
        /// </summary>
        /// <param name="status">The current status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task SetStatusAsync(UserPresence status, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously starts receiving public chat messages.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task StartPublicChatAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously stops receiving public chat messages.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task StopPublicChatAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously removes the specified <paramref name="username"/> from the server watch list for the current session.
        /// </summary>
        /// <remarks>
        ///     Once a user is removed the server will no longer send status updates for that user, ending
        ///     <see cref="UserStatusChanged"/> events for that user.
        /// </remarks>
        /// <param name="username">The username of the user to unwatch.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task UnwatchUserAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously uploads the specified <paramref name="remoteFilename"/> from the specified
        ///     <paramref name="localFilename"/> to the the specified <paramref name="username"/> using the specified unique
        ///     <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
        /// <param name="localFilename">The fully qualified filename of the file to upload.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the transfer context.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/>, <paramref name="remoteFilename"/>, or
        ///     <paramref name="localFilename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     Thrown when the specified <paramref name="localFilename"/> can not be found.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="remoteFilename"/> to the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Transfer> UploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously uploads the specified <paramref name="remoteFilename"/> from the <see cref="Stream"/> created by
        ///     the specified <paramref name="inputStreamFactory"/> to the the specified <paramref name="username"/> using the
        ///     specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="remoteFilename">The filename of the file to upload, as requested by the remote user.</param>
        /// <param name="size">The size of the file to upload, in bytes.</param>
        /// <param name="inputStreamFactory">A delegate used to create the stream from which to retrieve the file contents.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the transfer context.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="remoteFilename"/> is null, empty, or consists only
        ///     of whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="size"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the specified <paramref name="inputStreamFactory"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="remoteFilename"/> to the specified
        ///     <paramref name="username"/> is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserOfflineException">Thrown when the specified user is offline.</exception>
        /// <exception cref="TransferRejectedException">Thrown when the transfer is rejected.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="username"/> to the server watch list for the current session.
        /// </summary>
        /// <remarks>
        ///     Once a user is added the server will begin sending status updates for that user, which will generate
        ///     <see cref="UserStatusChanged"/> events.
        /// </remarks>
        /// <param name="username">The username of the user to watch.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserNotFoundException">Thrown when the specified user is not registered.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        Task<UserData> WatchUserAsync(string username, CancellationToken? cancellationToken = null);
    }
}