// <copyright file="SoulseekClient.cs" company="JP Dillingham">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : ISoulseekClient
    {
        private const string DefaultAddress = "vps.slsknet.org";
        private const int DefaultPort = 2271;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        public SoulseekClient()
            : this(options: new SoulseekClientOptions())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="options">The client options.</param>
        public SoulseekClient(SoulseekClientOptions options)
            : this(options, serverConnection: null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="options">The client options.</param>
        /// <param name="serverConnection">The IMessageConnection instance to use.</param>
        /// <param name="connectionFactory">The IConnectionFactory instance to use.</param>
        /// <param name="peerConnectionManager">The IPeerConnectionManager instance to use.</param>
        /// <param name="distributedConnectionManager">The IDistributedConnectionManager instance to use.</param>
        /// <param name="serverMessageHandler">The IServerMessageHandler instance to use.</param>
        /// <param name="peerMessageHandler">The IPeerMessageHandler instance to use.</param>
        /// <param name="distributedMessageHandler">The IDistributedMessageHandler instance to use.</param>
        /// <param name="listener">The IListener instance to use.</param>
        /// <param name="listenerHandler">The IListenerHandler instance to use.</param>
        /// <param name="searchResponder">The ISearchResponder instance to use.</param>
        /// <param name="waiter">The IWaiter instance to use.</param>
        /// <param name="tokenFactory">The ITokenFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        /// <param name="ioAdapter">The IIOAdapter instance to use.</param>
        /// <param name="uploadTokenBucket">The ITokenBucket instance to use for uploads.</param>
        /// <param name="downloadTokenBucket">The ITokenBucket instance to use for downloads.</param>
#pragma warning disable S3427 // Method overloads with default parameter values should not overlap
        internal SoulseekClient(
            SoulseekClientOptions options = null,
            IMessageConnection serverConnection = null,
            IConnectionFactory connectionFactory = null,
            IPeerConnectionManager peerConnectionManager = null,
            IDistributedConnectionManager distributedConnectionManager = null,
            IServerMessageHandler serverMessageHandler = null,
            IPeerMessageHandler peerMessageHandler = null,
            IDistributedMessageHandler distributedMessageHandler = null,
            IListener listener = null,
            IListenerHandler listenerHandler = null,
            ISearchResponder searchResponder = null,
            IWaiter waiter = null,
            ITokenFactory tokenFactory = null,
            IDiagnosticFactory diagnosticFactory = null,
            IIOAdapter ioAdapter = null,
            ITokenBucket uploadTokenBucket = null,
            ITokenBucket downloadTokenBucket = null)
        {
#pragma warning restore S3427 // Method overloads with default parameter values should not overlap
            Options = options ?? new SoulseekClientOptions();

            RaiseEventsAsynchronously = Options.RaiseEventsAsynchronously;

            SearchSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentSearches, maxCount: Options.MaximumConcurrentSearches);

            GlobalDownloadSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentDownloads, maxCount: Options.MaximumConcurrentDownloads);
            GlobalUploadSemaphore = new SemaphoreSlim(initialCount: Options.MaximumConcurrentUploads, maxCount: Options.MaximumConcurrentUploads);

            UserEndPointSemaphoreCleanupTimer = new System.Timers.Timer(300000); // 5 minutes
            UserEndPointSemaphoreCleanupTimer.Elapsed += (sender, e) => _ = CleanupUserEndPointSemaphoresAsync();
            UserEndPointSemaphoreCleanupTimer.Start();

            UploadSemaphoreCleanupTimer = new System.Timers.Timer(900000); // 15 minutes
            UploadSemaphoreCleanupTimer.Elapsed += (sender, e) => _ = CleanupUploadSemaphoresAsync();
            UploadSemaphoreCleanupTimer.Start();

            UploadTokenBucket = uploadTokenBucket ?? new TokenBucket((Options.MaximumUploadSpeed * 1024L) / 10, 100);
            DownloadTokenBucket = downloadTokenBucket ?? new TokenBucket((Options.MaximumDownloadSpeed * 1024L) / 10, 100);

            ServerConnection = serverConnection;

            Waiter = waiter ?? new Waiter(Options.MessageTimeout);
            TokenFactory = tokenFactory ?? new TokenFactory(Options.StartingToken);
            IOAdapter = ioAdapter ?? new IOAdapter();

            Diagnostic = diagnosticFactory ?? new DiagnosticFactory(Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
            GlobalDiagnostic.Init(Diagnostic);

            ListenerHandler = listenerHandler ?? new ListenerHandler(this);
            ListenerHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            Listener = listener;

            SearchResponder = searchResponder ?? new SearchResponder(this);
            SearchResponder.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);
            SearchResponder.RequestReceived += (sender, e) => SearchRequestReceived?.Invoke(this, e);
            SearchResponder.ResponseDelivered += (sender, e) => SearchResponseDelivered?.Invoke(this, e);
            SearchResponder.ResponseDeliveryFailed += (sender, e) => SearchResponseDeliveryFailed?.Invoke(this, e);

            PeerMessageHandler = peerMessageHandler ?? new PeerMessageHandler(this);
            PeerMessageHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);
            PeerMessageHandler.DownloadFailed += (sender, e) =>
            {
                // this is also handled in PeerMessageHandler, but the logic in there throws a wait, and we're not guaranteed
                // to be waiting on that specific wait when we get this message. as a precaution to avoid downloads that
                // get stuck waiting for something to happen while the remote client considers the download dead, try to
                // fail any download that matches this filename and user (we shouldn't have >1 but stranger things could happen)
                try
                {
                    var downloads = DownloadDictionary.Values
                        .Where(d => d.Username == e.Username && d.Filename == e.Filename)
                        .ToList();

                    foreach (var download in downloads)
                    {
                        download.RemoteTaskCompletionSource.TrySetException(new TransferException("Download reported as failed by remote client"));
                        Diagnostic.Debug($"Download of {download.Filename} from {download.Username} reported as failed by remote client (token: {download.Token})");
                    }
                }
                catch (Exception ex)
                {
                    Diagnostic.Warning($"Failed to mark download(s) failed: {ex.Message}", ex);
                }
                finally
                {
                    DownloadFailed?.Invoke(this, e);
                }
            };

            PeerMessageHandler.DownloadDenied += (sender, e) =>
            {
                // this is handled in PeerMessageHandler, and we throw a TransferRequest wait in that logic, which is almost
                // certainly enough for this to work as expected in 100% of cases. this is a precaution to prevent 'stuck' transfers.
                try
                {
                    var downloads = DownloadDictionary.Values
                        .Where(d => d.Username == e.Username && d.Filename == e.Filename)
                        .ToList();

                    foreach (var download in downloads)
                    {
                        download.RemoteTaskCompletionSource.TrySetException(new TransferRejectedException(e.Message));
                        Diagnostic.Debug($"Download of {download.Filename} from {download.Username} rejected by remote client (token: {download.Token})");
                    }
                }
                catch (Exception ex)
                {
                    Diagnostic.Warning($"Failed to mark download(s) rejected: {ex.Message}", ex);
                }
                finally
                {
                    DownloadDenied?.Invoke(this, e);
                }
            };

            DistributedMessageHandler = distributedMessageHandler ?? new DistributedMessageHandler(this);
            DistributedMessageHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            PeerConnectionManager = peerConnectionManager ?? new PeerConnectionManager(this);
            PeerConnectionManager.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);

            DistributedConnectionManager = distributedConnectionManager ?? new DistributedConnectionManager(this);
            DistributedConnectionManager.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);
            DistributedConnectionManager.PromotedToBranchRoot += (sender, e) => PromotedToDistributedBranchRoot?.Invoke(this, e);
            DistributedConnectionManager.DemotedFromBranchRoot += (sender, e) => DemotedFromDistributedBranchRoot?.Invoke(this, e);
            DistributedConnectionManager.ParentAdopted += (sender, e) => DistributedParentAdopted?.Invoke(this, e);
            DistributedConnectionManager.ParentDisconnected += (sender, e) => DistributedParentDisconnected?.Invoke(this, e);
            DistributedConnectionManager.ChildAdded += (sender, e) => DistributedChildAdded?.Invoke(this, e);
            DistributedConnectionManager.ChildDisconnected += (sender, e) => DistributedChildDisconnected?.Invoke(this, e);
            DistributedConnectionManager.StateChanged += (sender, e) => DistributedNetworkStateChanged?.Invoke(this, e);

            ServerMessageHandler = serverMessageHandler ?? new ServerMessageHandler(this);
            ServerMessageHandler.UserCannotConnect += (sender, e) => UserCannotConnect?.Invoke(this, e);
            ServerMessageHandler.UserStatusChanged += (sender, e) => UserStatusChanged?.Invoke(this, e);
            ServerMessageHandler.UserStatisticsChanged += (sender, e) => UserStatisticsChanged?.Invoke(this, e);
            ServerMessageHandler.PrivateMessageReceived += (sender, e) => PrivateMessageReceived?.Invoke(this, e);
            ServerMessageHandler.PrivateRoomMembershipAdded += (sender, e) => PrivateRoomMembershipAdded?.Invoke(this, e);
            ServerMessageHandler.PrivateRoomMembershipRemoved += (sender, e) => PrivateRoomMembershipRemoved?.Invoke(this, e);
            ServerMessageHandler.PrivateRoomModeratedUserListReceived += (sender, e) => PrivateRoomModeratedUserListReceived?.Invoke(this, e);
            ServerMessageHandler.PrivateRoomModerationAdded += (sender, e) => PrivateRoomModerationAdded?.Invoke(this, e);
            ServerMessageHandler.PrivateRoomModerationRemoved += (sender, e) => PrivateRoomModerationRemoved?.Invoke(this, e);
            ServerMessageHandler.PrivateRoomUserListReceived += (sender, e) => PrivateRoomUserListReceived?.Invoke(this, e);
            ServerMessageHandler.PrivilegedUserListReceived += (sender, e) => PrivilegedUserListReceived?.Invoke(this, e);
            ServerMessageHandler.PrivilegeNotificationReceived += (sender, e) => PrivilegeNotificationReceived?.Invoke(this, e);
            ServerMessageHandler.RoomMessageReceived += (sender, e) => RoomMessageReceived?.Invoke(this, e);
            ServerMessageHandler.RoomTickerListReceived += (sender, e) => RoomTickerListReceived?.Invoke(this, e);
            ServerMessageHandler.RoomTickerAdded += (sender, e) => RoomTickerAdded?.Invoke(this, e);
            ServerMessageHandler.RoomTickerRemoved += (sender, e) => RoomTickerRemoved?.Invoke(this, e);
            ServerMessageHandler.PublicChatMessageReceived += (sender, e) => PublicChatMessageReceived?.Invoke(this, e);
            ServerMessageHandler.RoomJoined += (sender, e) => RoomJoined?.Invoke(this, e);
            ServerMessageHandler.RoomLeft += (sender, e) => RoomLeft?.Invoke(this, e);
            ServerMessageHandler.RoomListReceived += (sender, e) => RoomListReceived?.Invoke(this, e);
            ServerMessageHandler.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);
            ServerMessageHandler.GlobalMessageReceived += (sender, e) => GlobalMessageReceived?.Invoke(this, e);
            ServerMessageHandler.DistributedNetworkReset += (sender, e) => DistributedNetworkReset?.Invoke(this, e);
            ServerMessageHandler.ExcludedSearchPhrasesReceived += (sender, e) => ExcludedSearchPhrasesReceived?.Invoke(this, e);

            ServerMessageHandler.ServerInfoReceived += (sender, e) =>
            {
                ServerInfo = ServerInfo.With(
                    parentMinSpeed: e.ParentMinSpeed,
                    parentSpeedRatio: e.ParentSpeedRatio,
                    wishlistInterval: e.WishlistInterval,
                    isSupporter: e.IsSupporter);

                ServerInfoReceived?.Invoke(this, ServerInfo);
            };

            ServerMessageHandler.KickedFromServer += (sender, e) =>
            {
                Diagnostic.Info($"Kicked from server.");
                Disconnect("Kicked from server", new KickedFromServerException());
                KickedFromServer?.Invoke(this, e);
            };
        }

        /// <summary>
        ///     Occurs when a browse response receives data.
        /// </summary>
        public event EventHandler<BrowseProgressUpdatedEventArgs> BrowseProgressUpdated;

        /// <summary>
        ///     Occurs when the client connects.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        ///     Occurs when the client is demoted from a branch root on the distributed network.
        /// </summary>
        public event EventHandler DemotedFromDistributedBranchRoot;

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when the client disconnects.
        /// </summary>
        public event EventHandler<SoulseekClientDisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     Occurs when a child connection is added.
        /// </summary>
        public event EventHandler<DistributedChildEventArgs> DistributedChildAdded;

        /// <summary>
        ///     Occurs when a child connection is disconnected.
        /// </summary>
        public event EventHandler<DistributedChildEventArgs> DistributedChildDisconnected;

        /// <summary>
        ///     Occurs when the server requests a distributed network reset.
        /// </summary>
        public event EventHandler DistributedNetworkReset;

        /// <summary>
        ///     Occurs when the state of the distributed network changes.
        /// </summary>
        public event EventHandler<DistributedNetworkInfo> DistributedNetworkStateChanged;

        /// <summary>
        ///     Occurs when a new parent is adopted.
        /// </summary>
        public event EventHandler<DistributedParentEventArgs> DistributedParentAdopted;

        /// <summary>
        ///     Occurs when the parent is disconnected.
        /// </summary>
        public event EventHandler<DistributedParentEventArgs> DistributedParentDisconnected;

        /// <summary>
        ///     Occurs when a user reports that a download has been denied.
        /// </summary>
        public event EventHandler<DownloadDeniedEventArgs> DownloadDenied;

        /// <summary>
        ///     Occurs when a user reports that a download has failed.
        /// </summary>
        public event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        ///     Occurs when the server sends a list of excluded ("banned") search phrases.
        /// </summary>
        public event EventHandler<IReadOnlyCollection<string>> ExcludedSearchPhrasesReceived;

        /// <summary>
        ///     Occurs when a global message is received.
        /// </summary>
        public event EventHandler<string> GlobalMessageReceived;

        /// <summary>
        ///     Occurs when the client is forcefully disconnected from the server, probably because another client logged in with
        ///     the same credentials.
        /// </summary>
        public event EventHandler KickedFromServer;

        /// <summary>
        ///     Occurs when the client is logged in.
        /// </summary>
        public event EventHandler LoggedIn;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when the currently logged in user is granted membership to a private room.
        /// </summary>
        public event EventHandler<string> PrivateRoomMembershipAdded;

        /// <summary>
        ///     Occurs when the currently logged in user has membership to a private room revoked.
        /// </summary>
        public event EventHandler<string> PrivateRoomMembershipRemoved;

        /// <summary>
        ///     Occurs when a list of moderated users for a private room is received.
        /// </summary>
        public event EventHandler<RoomInfo> PrivateRoomModeratedUserListReceived;

        /// <summary>
        ///     Occurs when the currently logged in user is granted moderator status in a private room.
        /// </summary>
        public event EventHandler<string> PrivateRoomModerationAdded;

        /// <summary>
        ///     Occurs when the currently logged in user has moderator status removed in a private room.
        /// </summary>
        public event EventHandler<string> PrivateRoomModerationRemoved;

        /// <summary>
        ///     Occurs when a list of users for a private room is received.
        /// </summary>
        public event EventHandler<RoomInfo> PrivateRoomUserListReceived;

        /// <summary>
        ///     Occurs when the server sends a list of privileged users.
        /// </summary>
        public event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;

        /// <summary>
        ///     Occurs when the server sends a notification of new user privileges.
        /// </summary>
        public event EventHandler<PrivilegeNotificationReceivedEventArgs> PrivilegeNotificationReceived;

        /// <summary>
        ///     Occurs when the client has been promoted to a branch root on the distributed network.
        /// </summary>
        public event EventHandler PromotedToDistributedBranchRoot;

        /// <summary>
        ///     Occurs when a public chat message is received.
        /// </summary>
        public event EventHandler<PublicChatMessageReceivedEventArgs> PublicChatMessageReceived;

        /// <summary>
        ///     Occurs when a user joins a chat room.
        /// </summary>
        public event EventHandler<RoomJoinedEventArgs> RoomJoined;

        /// <summary>
        ///     Occurs when a user leaves a chat room.
        /// </summary>
        public event EventHandler<RoomLeftEventArgs> RoomLeft;

        /// <summary>
        ///     Occurs when the server sends a list of chat rooms.
        /// </summary>
        public event EventHandler<RoomList> RoomListReceived;

        /// <summary>
        ///     Occurs when a chat room message is received.
        /// </summary>
        public event EventHandler<RoomMessageReceivedEventArgs> RoomMessageReceived;

        /// <summary>
        ///     Occurs when a chat room ticker is added.
        /// </summary>
        public event EventHandler<RoomTickerAddedEventArgs> RoomTickerAdded;

        /// <summary>
        ///     Occurs when the server sends a list of tickers for a chat room.
        /// </summary>
        public event EventHandler<RoomTickerListReceivedEventArgs> RoomTickerListReceived;

        /// <summary>
        ///     Occurs when a chat room ticker is removed.
        /// </summary>
        public event EventHandler<RoomTickerRemovedEventArgs> RoomTickerRemoved;

        /// <summary>
        ///     Occurs when a search request is received.
        /// </summary>
        public event EventHandler<SearchRequestEventArgs> SearchRequestReceived;

        /// <summary>
        ///     Occurs when the response to a search request is delivered.
        /// </summary>
        public event EventHandler<SearchRequestResponseEventArgs> SearchResponseDelivered;

        /// <summary>
        ///     Occurs when the delivery of a response to a search request fails.
        /// </summary>
        public event EventHandler<SearchRequestResponseEventArgs> SearchResponseDeliveryFailed;

        /// <summary>
        ///     Occurs when a new search result is received.
        /// </summary>
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Occurs when a search changes state.
        /// </summary>
        public event EventHandler<SearchStateChangedEventArgs> SearchStateChanged;

        /// <summary>
        ///     Occurs when the server sends information upon login.
        /// </summary>
        public event EventHandler<ServerInfo> ServerInfoReceived;

        /// <summary>
        ///     Occurs when the client changes state.
        /// </summary>
        public event EventHandler<SoulseekClientStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Occurs when an active transfer sends or receives data.
        /// </summary>
        public event EventHandler<TransferProgressUpdatedEventArgs> TransferProgressUpdated;

        /// <summary>
        ///     Occurs when a transfer changes state.
        /// </summary>
        public event EventHandler<TransferStateChangedEventArgs> TransferStateChanged;

        /// <summary>
        ///     Occurs when a user fails to connect.
        /// </summary>
        public event EventHandler<UserCannotConnectEventArgs> UserCannotConnect;

        /// <summary>
        ///     Occurs when a user's statistics change.
        /// </summary>
        public event EventHandler<UserStatistics> UserStatisticsChanged;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        /// <remarks>Add a user to the server watch list with <see cref="WatchUserAsync(string, CancellationToken?)"/>.</remarks>
        public event EventHandler<UserStatus> UserStatusChanged;

        /// <summary>
        ///     Gets or sets a value indicating whether to raise events asynchronously.
        /// </summary>
        public static bool RaiseEventsAsynchronously { get; set; }

        /// <summary>
        ///     Gets the unresolved server address.
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        ///     Gets information about the distributed network.
        /// </summary>
        public DistributedNetworkInfo DistributedNetwork => DistributedNetworkInfo.FromDistributedConnectionManager(DistributedConnectionManager);

        /// <summary>
        ///     Gets a snapshot of current downloads.
        /// </summary>
        public IReadOnlyCollection<Transfer> Downloads => DownloadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        public IPAddress IPAddress => IPEndPoint?.Address;

        /// <summary>
        ///     Gets the resolved server endpoint.
        /// </summary>
        public IPEndPoint IPEndPoint { get; private set; }

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        public virtual SoulseekClientOptions Options { get; private set; }

        /// <summary>
        ///     Gets server port.
        /// </summary>
        public int? Port => IPEndPoint?.Port;

        /// <summary>
        ///     Gets information sent by the server upon login.
        /// </summary>
        public ServerInfo ServerInfo { get; private set; } = new ServerInfo(parentMinSpeed: null, parentSpeedRatio: null, wishlistInterval: null, isSupporter: null);

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public virtual SoulseekClientStates State { get; private set; } = SoulseekClientStates.Disconnected;

        /// <summary>
        ///     Gets a snapshot of current uploads.
        /// </summary>
        public IReadOnlyCollection<Transfer> Uploads => UploadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        public virtual string Username { get; private set; }

#pragma warning disable SA1600 // Elements should be documented
        internal virtual IDistributedConnectionManager DistributedConnectionManager { get; }
        internal virtual IDistributedMessageHandler DistributedMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, TransferInternal> DownloadDictionary { get; set; } = new ConcurrentDictionary<int, TransferInternal>();
        internal virtual IListener Listener { get; private set; }
        internal virtual IListenerHandler ListenerHandler { get; }
        internal virtual IPeerConnectionManager PeerConnectionManager { get; }
        internal virtual IPeerMessageHandler PeerMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, SearchInternal> Searches { get; set; } = new ConcurrentDictionary<int, SearchInternal>();
        internal virtual ISearchResponder SearchResponder { get; }
        internal virtual IMessageConnection ServerConnection { get; private set; }
        internal virtual IServerMessageHandler ServerMessageHandler { get; }
        internal virtual ConcurrentDictionary<int, TransferInternal> UploadDictionary { get; set; } = new ConcurrentDictionary<int, TransferInternal>();
        internal virtual ConcurrentDictionary<string, bool> UniqueKeyDictionary { get; set; } = new ConcurrentDictionary<string, bool>();
        internal virtual IWaiter Waiter { get; }
#pragma warning restore SA1600 // Elements should be documented

        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; } = false;
        private ITokenBucket DownloadTokenBucket { get; }
        private SemaphoreSlim SearchSemaphore { get; }
        private SemaphoreSlim GlobalDownloadSemaphore { get; }
        private SemaphoreSlim GlobalUploadSemaphore { get; }
        private IIOAdapter IOAdapter { get; set; } = new IOAdapter();
        private SemaphoreSlim StateSyncRoot { get; } = new SemaphoreSlim(1, 1);
        private ITokenFactory TokenFactory { get; }
        private System.Timers.Timer UploadSemaphoreCleanupTimer { get; }
        private ConcurrentDictionary<string, SemaphoreSlim> UploadSemaphores { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();
        private SemaphoreSlim UploadSemaphoreSyncRoot { get; } = new SemaphoreSlim(1, 1);
        private ITokenBucket UploadTokenBucket { get; }
        private System.Timers.Timer UserEndPointSemaphoreCleanupTimer { get; }
        private ConcurrentDictionary<string, SemaphoreSlim> UserEndPointSemaphores { get; } = new ConcurrentDictionary<string, SemaphoreSlim>();
        private SemaphoreSlim UserEndPointSemaphoreSyncRoot { get; } = new SemaphoreSlim(1, 1);

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
        public virtual Task AcknowledgePrivateMessageAsync(int privateMessageId, CancellationToken? cancellationToken = null)
        {
            if (privateMessageId < 0)
            {
                throw new ArgumentException("The private message ID must be greater than zero", nameof(privateMessageId));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to acknowledge private messages (currently: {State})");
            }

            return AcknowledgePrivateMessageInternalAsync(privateMessageId, cancellationToken ?? CancellationToken.None);
        }

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
        public virtual Task AcknowledgePrivilegeNotificationAsync(int privilegeNotificationId, CancellationToken? cancellationToken = null)
        {
            if (privilegeNotificationId < 0)
            {
                throw new ArgumentException("The privilege notification ID must be greater than zero", nameof(privilegeNotificationId));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to acknowledge privilege notifications (currently: {State})");
            }

            return AcknowledgePrivilegeNotificationInternalAsync(privilegeNotificationId, cancellationToken ?? CancellationToken.None);
        }

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
        public Task AddPrivateRoomMemberAsync(string roomName, string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to add members to private rooms (currently: {State})");
            }

            return AddPrivateRoomMemberInternalAsync(roomName, username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task AddPrivateRoomModeratorAsync(string roomName, string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to add moderators to private rooms (currently: {State})");
            }

            return AddPrivateRoomModeratorInternalAsync(roomName, username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<BrowseResponse> BrowseAsync(string username, BrowseOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to browse (currently: {State})");
            }

            options ??= new BrowseOptions();

            return BrowseInternalAsync(username, options, cancellationToken ?? CancellationToken.None);
        }

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
        public Task ChangePasswordAsync(string password, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("The password must not be a null or empty string, or one consisting only of whitespace", nameof(password));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in change a password (currently: {State})");
            }

            return ChangePasswordInternalAsync(password, cancellationToken ?? CancellationToken.None);
        }

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
        public Task ConnectAsync(string username, string password, CancellationToken? cancellationToken = null)
        {
            return ConnectAsync(DefaultAddress, DefaultPort, username, password, cancellationToken ?? CancellationToken.None);
        }

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
        public Task ConnectAsync(string address, int port, string username, string password, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Address must not be a null or empty string, or one consisting only of whitespace", nameof(address));
            }

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"The port must be within the range {IPEndPoint.MinPort}-{IPEndPoint.MaxPort} (specified: {port})");
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Username may not be null or an empty string", nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password may not be null or an empty string", nameof(password));
            }

            if (State.HasFlag(SoulseekClientStates.Connecting) || State.HasFlag(SoulseekClientStates.LoggingIn))
            {
                throw new InvalidOperationException($"A connection is already in the process of being established");
            }

            if (State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The client is already connected");
            }

            if (!IPAddress.TryParse(address, out IPAddress ipAddress))
            {
                try
                {
                    ipAddress = Dns.GetHostEntry(address).AddressList[0];
                }
                catch (SocketException ex)
                {
                    throw new AddressException($"Failed to resolve address '{address}': {ex.Message}", ex);
                }
            }

            if (Options.EnableListener)
            {
                Listener listener = null;

                // probe to see if we can listen on the configured port and address. if this throws, something is either already
                // listening on this port, or the user has specified a bad interface IP
                try
                {
                    listener = new Listener(Options.ListenIPAddress, Options.ListenPort, Options.IncomingConnectionOptions);
                    listener.Start();
                }
                catch (Exception)
                {
                    throw new ListenException($"Failed to start listening on {Options.ListenIPAddress}:{Options.ListenPort}; the IP and/or port may be in use or are otherwise unavailable");
                }
                finally
                {
                    listener?.Stop();
                }
            }

            return ConnectInternalAsync(address, new IPEndPoint(ipAddress, port), username, password, cancellationToken ?? CancellationToken.None);
        }

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
        public Task ConnectToUserAsync(string username, bool invalidateCache = false, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username may not be null or an empty string", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to connect to other users (currently: {State})");
            }

            return ConnectToUserInternalAsync(username, invalidateCache, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Disconnects the client from the server.
        /// </summary>
        /// <param name="message">An optional message describing the reason the client is being disconnected.</param>
        /// <param name="exception">An optional Exception causing the disconnect.</param>
        public void Disconnect(string message = null, Exception exception = null)
        {
            if (State != SoulseekClientStates.Disconnected && State != SoulseekClientStates.Disconnecting)
            {
                ChangeState(SoulseekClientStates.Disconnecting, message, exception);

                message ??= exception?.Message ?? "Client disconnected";

                Listener?.Stop();

                if (ServerConnection != default)
                {
                    ServerConnection.Disconnected -= ServerConnection_Disconnected;
                }

                ServerConnection?.Disconnect(message, exception);

                DistributedConnectionManager.RemoveAndDisposeAll();
                DistributedConnectionManager.ResetStatus();

                Searches.Values.ToList().ForEach(search =>
                {
                    search.Cancel();
                });

                Searches.RemoveAndDisposeAll();

                Username = null;

                ChangeState(SoulseekClientStates.Disconnected, message, exception);
            }
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
        public Task<Transfer> DownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(remoteFilename))
            {
                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
            }

            if (string.IsNullOrWhiteSpace(localFilename))
            {
                throw new ArgumentException("The local filename must not be a null or empty string, or one consisting only of whitespace", nameof(localFilename));
            }

            if (size.HasValue && size.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "The size, if supplied, must be greater than or equal to zero");
            }

            if (startOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startOffset), "The start offset must be greater than or equal to zero");
            }

            if (startOffset > 0 && !size.HasValue)
            {
                throw new ArgumentNullException(nameof(size), "The size must be specified if the start offset is not zero");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to download files (currently: {State})");
            }

            token ??= GetNextToken();

            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (DownloadDictionary.Values.Any(d => d.Username == username && d.Filename == remoteFilename))
            {
                throw new DuplicateTransferException($"An active or queued download of {remoteFilename} from {username} is already in progress");
            }

            if (UniqueKeyDictionary.ContainsKey($"{TransferDirection.Download}:{username}:{remoteFilename}"))
            {
                throw new DuplicateTransferException($"An active or queued download of {remoteFilename} from {username} is already in progress");
            }

            options ??= new TransferOptions();

            return DownloadToFileAsync(username, remoteFilename, localFilename, size, startOffset, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(remoteFilename))
            {
                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
            }

            if (size.HasValue && size.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "The size, if supplied, must be greater than or equal to zero");
            }

            if (startOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startOffset), "The start offset must be greater than or equal to zero");
            }

            if (startOffset > 0 && !size.HasValue)
            {
                throw new ArgumentNullException(nameof(size), "The size must be specified if the start offset is not zero");
            }

            if (outputStreamFactory == null)
            {
                throw new ArgumentNullException(nameof(outputStreamFactory), "The specified output stream factory is null");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to download files (currently: {State})");
            }

            token ??= GetNextToken();

            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (DownloadDictionary.Values.Any(d => d.Username == username && d.Filename == remoteFilename))
            {
                throw new DuplicateTransferException($"An active or queued download of {remoteFilename} from {username} is already in progress");
            }

            if (UniqueKeyDictionary.ContainsKey($"{TransferDirection.Download}:{username}:{remoteFilename}"))
            {
                throw new DuplicateTransferException($"An active or queued download of {remoteFilename} from {username} is already in progress");
            }

            options ??= new TransferOptions();

            return DownloadToStreamAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

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
        public Task DropPrivateRoomMembershipAsync(string roomName, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to drop private room membership (currently: {State})");
            }

            return DropPrivateRoomMembershipInternalAsync(roomName, cancellationToken ?? CancellationToken.None);
        }

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
        public Task DropPrivateRoomOwnershipAsync(string roomName, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to drop private room ownership (currently: {State})");
            }

            return DropPrivateRoomOwnershipInternalAsync(roomName, cancellationToken ?? CancellationToken.None);
        }

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
        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>();

            options ??= new TransferOptions();
            options = options.WithAdditionalStateChanged(args =>
            {
                var state = args.Transfer.State;

                if (state == (TransferStates.Queued | TransferStates.Remotely))
                {
                    enqueuedTaskCompletionSource.TrySetResult(true);
                }
                else if (state.HasFlag(TransferStates.Completed) && !state.HasFlag(TransferStates.Succeeded))
                {
                    enqueuedTaskCompletionSource.TrySetResult(false);
                }
            });

            // this may throw immediately, if there are issues with the input
            var downloadTask = DownloadAsync(username, remoteFilename, localFilename, size, startOffset, token, options, cancellationToken);

            var success = await enqueuedTaskCompletionSource.Task.ConfigureAwait(false);

            if (!success)
            {
                await downloadTask.ConfigureAwait(false);
            }

            return downloadTask;
        }

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
        public async Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>();

            options ??= new TransferOptions();
            options = options.WithAdditionalStateChanged(args =>
            {
                var state = args.Transfer.State;

                if (state == (TransferStates.Queued | TransferStates.Remotely))
                {
                    enqueuedTaskCompletionSource.TrySetResult(true);
                }
                else if (state.HasFlag(TransferStates.Completed) && !state.HasFlag(TransferStates.Succeeded))
                {
                    enqueuedTaskCompletionSource.TrySetResult(false);
                }
            });

            // this may throw immediately, if there are issues with the input
            var downloadTask = DownloadAsync(username, remoteFilename, outputStreamFactory, size, startOffset, token, options, cancellationToken);

            var success = await enqueuedTaskCompletionSource.Task.ConfigureAwait(false);

            if (!success)
            {
                await downloadTask.ConfigureAwait(false);
            }

            return downloadTask;
        }

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
        public async Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>();

            options ??= new TransferOptions();
            options = options.WithAdditionalStateChanged(args =>
            {
                if (args.Transfer.State == (TransferStates.Queued | TransferStates.Locally))
                {
                    enqueuedTaskCompletionSource.TrySetResult(true);
                }
            });

            // this may throw immediately, if there are issues with the input
            var uploadTask = UploadAsync(username, remoteFilename, localFilename, token, options, cancellationToken);

            await enqueuedTaskCompletionSource.Task.ConfigureAwait(false);
            return uploadTask;
        }

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
        public async Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            var enqueuedTaskCompletionSource = new TaskCompletionSource<bool>();

            options ??= new TransferOptions();
            options = options.WithAdditionalStateChanged(args =>
            {
                if (args.Transfer.State == (TransferStates.Queued | TransferStates.Locally))
                {
                    enqueuedTaskCompletionSource.TrySetResult(true);
                }
            });

            // this may throw immediately, if there are issues with the input
            var uploadTask = UploadAsync(username, remoteFilename, size, inputStreamFactory, token, options, cancellationToken);

            await enqueuedTaskCompletionSource.Task.ConfigureAwait(false);
            return uploadTask;
        }

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
        public Task<IReadOnlyCollection<Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(directoryName))
            {
                throw new ArgumentException("The directory name must not be a null or empty string, or one consisting only of whitespace", nameof(directoryName));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch directory contents (currently: {State})");
            }

            token ??= GetNextToken();

            return GetDirectoryContentsInternalAsync(username, directoryName, token.Value, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("The filename must not be a null or empty string, or one consisting only of whitespace", nameof(filename));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be Connected and LoggedIn to check download queue position (currently: {State})");
            }

            if (!DownloadDictionary.Any(d => d.Value.Username == username && d.Value.Filename == filename))
            {
                throw new TransferNotFoundException($"A download of {filename} from user {username} is not active");
            }

            return GetDownloadPlaceInQueueInternalAsync(username, filename, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Gets the next token for use in client operations.
        /// </summary>
        /// <remarks>
        ///     <para>Tokens are returned sequentially and the token value rolls over to 0 when it has reached <see cref="int.MaxValue"/>.</para>
        ///     <para>This operation is thread safe.</para>
        /// </remarks>
        /// <returns>The next token.</returns>
        /// <threadsafety instance="true"/>
        public virtual int GetNextToken() => TokenFactory.NextToken();

        /// <summary>
        ///     Asynchronously fetches the number of remaining days of privileges of the currently logged in user.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        public async Task<int> GetPrivilegesAsync(CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be Connected and LoggedIn to check privileges (currently: {State})");
            }

            try
            {
                var waitKey = new WaitKey(MessageCode.Server.CheckPrivileges);
                var wait = Waiter.Wait<int>(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new CheckPrivilegesRequest(), cancellationToken).ConfigureAwait(false);

                var result = await wait.ConfigureAwait(false);
                return result;
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to get privileges: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously fetches the list of chat rooms on the server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the list of server rooms.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        public async Task<RoomList> GetRoomListAsync(CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch the list of chat rooms (currently: {State})");
            }

            try
            {
                var roomListWait = Waiter.Wait<RoomList>(new WaitKey(MessageCode.Server.RoomList), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new RoomListRequest(), cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

                var response = await roomListWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to fetch the list of chat rooms from the server: {ex.Message}", ex);
            }
        }

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
        public virtual Task<IPEndPoint> GetUserEndPointAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user endpoint (currently: {State})");
            }

            return GetUserEndPointInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<UserInfo> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user information (currently: {State})");
            }

            return GetUserInfoInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<bool> GetUserPrivilegedAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to check user privileges (currently: {State})");
            }

            return GetUserPrivilegedInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<UserStatistics> GetUserStatisticsAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user statistics (currently: {State})");
            }

            return GetUserStatisticsInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<UserStatus> GetUserStatusAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user status (currently: {State})");
            }

            return GetUserStatusInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task GrantUserPrivilegesAsync(string username, int days, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (days <= 0)
            {
                throw new ArgumentException("The number of days granted must be greater than zero", nameof(days));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to grant user privileges (currently: {State})");
            }

            return GrantUserPrivilegesInternalAsync(username, days, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<RoomData> JoinRoomAsync(string roomName, bool isPrivate = false, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to join a chat room (currently: {State})");
            }

            return JoinRoomInternalAsync(roomName, isPrivate, cancellationToken ?? CancellationToken.None);
        }

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
        public Task LeaveRoomAsync(string roomName, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to leave a chat room (currently: {State})");
            }

            return LeaveRoomInternalAsync(roomName, cancellationToken ?? CancellationToken.None);
        }

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
        public async Task<long> PingServerAsync(CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to send a ping (currently: {State})");
            }

            try
            {
                var wait = Waiter.Wait(new WaitKey(MessageCode.Server.Ping), null, cancellationToken);
                var ping = new ServerPing();

                var sw = new Stopwatch();
                sw.Start();

                await ServerConnection.WriteAsync(ping, cancellationToken).ConfigureAwait(false);

                await wait.ConfigureAwait(false);

                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to ping the server: {ex.Message}", ex);
            }
        }

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
        public Task<bool> ReconfigureOptionsAsync(SoulseekClientOptionsPatch patch, CancellationToken? cancellationToken = null)
        {
            if (patch == null)
            {
                throw new ArgumentNullException(nameof(patch), "The patch must not be null");
            }

            // if the listen address or port is changing, probe to see if we can listen on the new address/port if not, reject the
            // reconfigure attempt
            if ((patch.ListenIPAddress != null && !patch.ListenIPAddress.Equals(Options.ListenIPAddress)) || (patch.ListenPort.HasValue && patch.ListenPort != Options.ListenPort))
            {
                Listener listener = null;
                var newAddress = patch.ListenIPAddress ?? Options.ListenIPAddress;
                var newPort = patch.ListenPort ?? Options.ListenPort;

                try
                {
                    listener = new Listener(newAddress, newPort, Options.IncomingConnectionOptions);
                    listener.Start();
                }
                catch (Exception)
                {
                    throw new ListenException($"Failed to start listening on {newAddress}:{newPort}; the IP and/or port may be in use or are otherwise unavailable");
                }
                finally
                {
                    listener?.Stop();
                }
            }

            return ReconfigureOptionsInternalAsync(patch, cancellationToken ?? CancellationToken.None);
        }

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
        public Task RemovePrivateRoomMemberAsync(string roomName, string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to remove users from private rooms (currently: {State})");
            }

            return RemovePrivateRoomMemberInternalAsync(roomName, username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task RemovePrivateRoomModeratorAsync(string roomName, string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting of only whitespace", nameof(roomName));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to remove moderators from private rooms (currently: {State})");
            }

            return RemovePrivateRoomModeratorInternalAsync(roomName, username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (string.IsNullOrWhiteSpace(query.SearchText))
            {
                throw new ArgumentException("Search text must not be a null or empty string, or one consisting only of whitespace", nameof(query));
            }

            if (query.Terms.Count == 0)
            {
                throw new ArgumentException("Search query must contain at least one non-exclusion term", nameof(query));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to perform a search (currently: {State})");
            }

            token ??= TokenFactory.NextToken();

            if (Searches.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"An active search with token {token.Value} is already in progress");
            }

            scope ??= new SearchScope(SearchScopeType.Network);
            options ??= new SearchOptions();

            if (options.RemoveSingleCharacterSearchTerms)
            {
                query = new SearchQuery(query.Terms.Where(term => term.Length > 1), query.Exclusions);
            }

            if (query.Terms.Count == 0)
            {
                throw new ArgumentException("Search query must contain at least one non-exclusion term with length greater than 1", nameof(query));
            }

            return SearchToCollectionAsync(query, scope, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<Search> SearchAsync(SearchQuery query, Action<SearchResponse> responseHandler, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (string.IsNullOrWhiteSpace(query.SearchText))
            {
                throw new ArgumentException("Search text must not be a null or empty string, or one consisting only of whitespace", nameof(query));
            }

            if (query.Terms.Count == 0)
            {
                throw new ArgumentException("Search query must contain at least one non-exclusion term", nameof(query));
            }

            if (responseHandler == default)
            {
                throw new ArgumentNullException(nameof(responseHandler), "The specified Response delegate is null");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to perform a search (currently: {State})");
            }

            token ??= TokenFactory.NextToken();

            if (Searches.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"An active search with token {token.Value} is already in progress");
            }

            scope ??= new SearchScope(SearchScopeType.Network);
            options ??= new SearchOptions();

            if (options.RemoveSingleCharacterSearchTerms)
            {
                query = new SearchQuery(query.Terms.Where(term => term.Length > 1), query.Exclusions);
            }

            if (query.Terms.Count == 0)
            {
                throw new ArgumentException("Search query must contain at least one non-exclusion term with length greater than 1", nameof(query));
            }

            return SearchToCallbackAsync(query, responseHandler, scope, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

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
        public Task SendPrivateMessageAsync(string username, string message, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("The message must not be a null or empty string", nameof(message));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to send a private message (currently: {State})");
            }

            return SendPrivateMessageInternalAsync(username, message, cancellationToken ?? CancellationToken.None);
        }

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
        public Task SendRoomMessageAsync(string roomName, string message, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting only of whitespace", nameof(roomName));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("The message must not be a null or empty string", nameof(message));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to send a chat room message (currently: {State})");
            }

            return SendRoomMessageInternalAsync(roomName, message, cancellationToken ?? CancellationToken.None);
        }

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
        public Task SendUploadSpeedAsync(int speed, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set upload speed (currently: {State})");
            }

            if (speed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), $"The upload speed must be greater than zero");
            }

            try
            {
                return ServerConnection.WriteAsync(new SendUploadSpeedCommand(speed), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to set upload speed: {ex.Message}", ex);
            }
        }

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
        public Task SetRoomTickerAsync(string roomName, string message, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                throw new ArgumentException("The room name must not be a null or empty string, or one consisting only of whitespace", nameof(roomName));
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("The message must not be a null or empty string", nameof(message));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set chat room tickers (currently: {State})");
            }

            try
            {
                return ServerConnection.WriteAsync(new SetRoomTickerCommand(roomName, message), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to set chat room ticker in room {roomName}: {ex.Message}", ex);
            }
        }

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
        public Task SetSharedCountsAsync(int directories, int files, CancellationToken? cancellationToken = null)
        {
            if (directories < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(directories), directories, "The directory count must be equal to or greater than zero");
            }

            if (files < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(files), files, "The file count must be equal to or greater than zero");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set shared counts (currently: {State})");
            }

            try
            {
                return ServerConnection.WriteAsync(new SetSharedCountsCommand(directories, files), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to set shared counts to {directories} directories and {files} files: {ex.Message}", ex);
            }
        }

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
        public Task SetStatusAsync(UserPresence status, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to set online status (currently: {State})");
            }

            try
            {
                return ServerConnection.WriteAsync(new SetOnlineStatusCommand(status), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to set user status to {status}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously starts receiving public chat messages.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        public Task StartPublicChatAsync(CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to start public chat (currently: {State})");
            }

            try
            {
                return ServerConnection.WriteAsync(new StartPublicChatCommand(), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to start public chat: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Asynchronously stops receiving public chat messages.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SoulseekClientException">Thrown when an exception is encountered during the operation.</exception>
        public Task StopPublicChatAsync(CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to stop public chat (currently: {State})");
            }

            try
            {
                return ServerConnection.WriteAsync(new StopPublicChatCommand(), cancellationToken ?? CancellationToken.None);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to stop public chat: {ex.Message}", ex);
            }
        }

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
        public Task UnwatchUserAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to add users (currently: {State})");
            }

            return UnwatchUserInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<Transfer> UploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(remoteFilename))
            {
                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
            }

            if (string.IsNullOrWhiteSpace(localFilename))
            {
                throw new ArgumentException("The local filename must not be a null or empty string, or one consisting only of whitespace", nameof(localFilename));
            }

            if (!IOAdapter.Exists(localFilename))
            {
                throw new FileNotFoundException("The local file does not exist", localFilename);
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to upload files (currently: {State})");
            }

            try
            {
                using var stream = IOAdapter.GetFileStream(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException ex)
            {
                throw new IOException($"The local file {localFilename} could not be opened for reading: {ex.Message}", ex);
            }

            token ??= GetNextToken();

            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (UploadDictionary.Values.Any(d => d.Username == username && d.Filename == remoteFilename))
            {
                throw new DuplicateTransferException($"An active or queued upload of {remoteFilename} to {username} is already in progress");
            }

            if (UniqueKeyDictionary.ContainsKey($"{TransferDirection.Upload}:{username}:{remoteFilename}"))
            {
                throw new DuplicateTransferException($"An active or queued upload of {remoteFilename} to {username} is already in progress");
            }

            options ??= new TransferOptions();

            return UploadFromFileAsync(username, remoteFilename, localFilename, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(remoteFilename))
            {
                throw new ArgumentException("The remote filename must not be a null or empty string, or one consisting only of whitespace", nameof(remoteFilename));
            }

            if (size < 0)
            {
                throw new ArgumentException("The requested size must be greater than or equal to zero", nameof(size));
            }

            if (inputStreamFactory == null)
            {
                throw new ArgumentNullException(nameof(inputStreamFactory), "The specified input stream factory is null");
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to upload files (currently: {State})");
            }

            token ??= GetNextToken();

            if (UploadDictionary.ContainsKey(token.Value) || DownloadDictionary.ContainsKey(token.Value))
            {
                throw new DuplicateTokenException($"The specified or generated token {token} is already in progress");
            }

            if (UploadDictionary.Values.Any(d => d.Username == username && d.Filename == remoteFilename))
            {
                throw new DuplicateTransferException($"An active or queued upload of {remoteFilename} to {username} is already in progress");
            }

            if (UniqueKeyDictionary.ContainsKey($"{TransferDirection.Upload}:{username}:{remoteFilename}"))
            {
                throw new DuplicateTransferException($"An active or queued upload of {remoteFilename} to {username} is already in progress");
            }

            options ??= new TransferOptions();

            return UploadFromStreamAsync(username, remoteFilename, size, inputStreamFactory, token.Value, options, cancellationToken ?? CancellationToken.None);
        }

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
        public Task<UserData> WatchUserAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting of only whitespace", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to add users (currently: {State})");
            }

            return WatchUserInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        /// <param name="disposing">A value indicating whether disposal is in progress.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Disconnect("Client is being disposed", new ObjectDisposedException(GetType().Name));
                    Listener?.Stop();

                    PeerConnectionManager.Dispose();
                    DistributedConnectionManager.Dispose();

                    Waiter.Dispose();

                    UploadTokenBucket.Dispose();
                    DownloadTokenBucket.Dispose();

                    StateSyncRoot.Dispose();
                    UploadSemaphoreSyncRoot.Dispose();
                    UserEndPointSemaphoreSyncRoot.Dispose();

                    SearchSemaphore.Dispose();
                    GlobalUploadSemaphore.Dispose();
                    GlobalDownloadSemaphore.Dispose();

                    ServerConnection?.Dispose();

                    UserEndPointSemaphoreCleanupTimer.Dispose();
                    UploadSemaphoreCleanupTimer.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task AcknowledgePrivateMessageInternalAsync(int privateMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new AcknowledgePrivateMessageCommand(privateMessageId), cancellationToken).ConfigureAwait(false);
                Diagnostic.Debug($"Acknowledged private message ID {privateMessageId}");
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to acknowledge private message with ID {privateMessageId}: {ex.Message}", ex);
            }
        }

        private async Task AcknowledgePrivilegeNotificationInternalAsync(int privilegeNotificationId, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new AcknowledgePrivilegeNotificationCommand(privilegeNotificationId), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to acknowledge privilege notification with ID {privilegeNotificationId}: {ex.Message}", ex);
            }
        }

        private async Task AddPrivateRoomMemberInternalAsync(string roomName, string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.PrivateRoomAddUser, roomName, username);
                var wait = Waiter.Wait(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new PrivateRoomAddUser(roomName, username), cancellationToken).ConfigureAwait(false);

                await wait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to add user {username} as member of private room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task AddPrivateRoomModeratorInternalAsync(string roomName, string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.PrivateRoomAddOperator, roomName, username);
                var wait = Waiter.Wait(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new PrivateRoomAddOperator(roomName, username), cancellationToken).ConfigureAwait(false);

                await wait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to add user {username} as moderator of private room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task<BrowseResponse> BrowseInternalAsync(string username, BrowseOptions options, CancellationToken cancellationToken)
        {
            var browseWaitKey = new WaitKey(MessageCode.Peer.BrowseResponse, username);
            var completionEventFired = false;

            void UpdateProgress(object sender, MessageDataEventArgs args)
            {
                if (Math.Abs(args.PercentComplete - 100) == 0)
                {
                    completionEventFired = true;
                }

                var e = new BrowseProgressUpdatedEventArgs(username, args.CurrentLength, args.TotalLength);
                options.ProgressUpdated?.Invoke((e.Username, e.BytesTransferred, e.BytesRemaining, e.PercentComplete, e.Size));
                BrowseProgressUpdated?.Invoke(this, e);
            }

            try
            {
                MessageReceivedEventArgs responseReceivedEventArgs;
                IMessageConnection responseConnection;
                long? responseLength;

                // prepare an indefinite wait for the operation. this is completed by either successful completion of the message
                // transfer, or by the receiving connection being disconnected.
                var browseWait = Waiter.WaitIndefinitely<BrowseResponse>(browseWaitKey, cancellationToken);

                // prepare a wait for the receipt of the response message with the timeout value specified in options. this allows
                // the operation to wait for the remote client to compose the response message. this wait is completed when the
                // browse response message is received, but before it is read entirely.
                var responseConnectionKey = new WaitKey(Constants.WaitKey.BrowseResponseConnection, username);
                var responseConnectionWait = Waiter.Wait<(MessageReceivedEventArgs, IMessageConnection)>(responseConnectionKey, options.ResponseTimeout, cancellationToken);

                try
                {
                    // fetch the user's address and a connection and write the browse request to the remote user
                    var endpoint = await GetUserEndPointAsync(username, cancellationToken).ConfigureAwait(false);
                    var connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken).ConfigureAwait(false);
                    await connection.WriteAsync(new BrowseRequest(), cancellationToken).ConfigureAwait(false);

                    // wait for the receipt of the response message. this may come back on a connection different from the one
                    // which made the request.
                    (responseReceivedEventArgs, responseConnection) = await responseConnectionWait.ConfigureAwait(false);
                    responseLength = responseReceivedEventArgs.Length - 4;

                    responseConnection.Disconnected += (sender, args) =>
                        Waiter.Throw(browseWaitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {args.Message}", args.Exception));

                    responseConnection.MessageDataRead += UpdateProgress;
                }
                catch (Exception ex)
                {
                    // if anything in the try block above threw, throw the wait for the browse. because it is indefinite, it needs
                    // to be removed before this code exits. once the response connection is returned and the disconnected event
                    // bound the risk is mitigated.
                    Waiter.Throw(browseWaitKey, ex);
                    throw;
                }

                // fake a progress update since we'll always miss the first packet (this is what fires the received event, so
                // we've already read the first 4k or whatever the read buffer size is)
                UpdateProgress(responseConnection, new MessageDataEventArgs(responseReceivedEventArgs.Code, 0, responseLength.Value));

                var response = await browseWait.ConfigureAwait(false);

                responseConnection.MessageDataRead -= UpdateProgress;

                // if the response was under 4k, we won't receive a DataRead event informing us of 100% completion. if this is the
                // case, fake it
                if (!completionEventFired)
                {
                    UpdateProgress(responseConnection, new MessageDataEventArgs(responseReceivedEventArgs.Code, responseLength.Value, responseLength.Value));
                }

                return response;
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to browse user {username}: {ex.Message}", ex);
            }
        }

        private async Task ChangePasswordInternalAsync(string password, CancellationToken cancellationToken)
        {
            string response;

            try
            {
                var waitKey = new WaitKey(MessageCode.Server.NewPassword);
                var wait = Waiter.Wait<string>(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new NewPassword(password), cancellationToken).ConfigureAwait(false);

                response = await wait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to change password: {ex.Message}", ex);
            }

            if (!response.Equals(password, StringComparison.CurrentCulture))
            {
                throw new SoulseekClientException("Probably failed to change password; the response from the server doesn't match the specified password");
            }
        }

        private void ChangeState(SoulseekClientStates state, string message, Exception exception = null)
        {
            var previousState = State;
            State = state;

            Diagnostic.Debug($"Client state changed from {previousState} to {state}{(message == null ? string.Empty : $"; message: {message}")}");
            StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(previousState, State, message, exception));

            if (State == SoulseekClientStates.Connected)
            {
                Connected?.Invoke(this, EventArgs.Empty);
            }
            else if (State == (SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn))
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
            else if (State == SoulseekClientStates.Disconnected)
            {
                Disconnected?.Invoke(this, new SoulseekClientDisconnectedEventArgs(message, exception));
            }
        }

        private async Task CleanupUploadSemaphoresAsync()
        {
            if (await UploadSemaphoreSyncRoot.WaitAsync(0).ConfigureAwait(false))
            {
                try
                {
                    foreach (var kvp in UploadSemaphores)
                    {
                        if (await kvp.Value.WaitAsync(0).ConfigureAwait(false))
                        {
                            UploadSemaphores.TryRemove(kvp.Key, out var removed);
                            removed.Dispose();
                            Diagnostic.Debug($"Cleaned up upload semaphore for {kvp.Key}");
                        }
                    }
                }
                finally
                {
                    UploadSemaphoreSyncRoot.Release();
                }
            }
        }

        private async Task CleanupUserEndPointSemaphoresAsync()
        {
            if (await UserEndPointSemaphoreSyncRoot.WaitAsync(0).ConfigureAwait(false))
            {
                try
                {
                    foreach (var kvp in UserEndPointSemaphores)
                    {
                        if (await kvp.Value.WaitAsync(0).ConfigureAwait(false))
                        {
                            UserEndPointSemaphores.TryRemove(kvp.Key, out var removed);
                            removed.Dispose();
                            Diagnostic.Debug($"Cleaned up user endpoint semaphore for {kvp.Key}");
                        }
                    }
                }
                finally
                {
                    UserEndPointSemaphoreSyncRoot.Release();
                }
            }
        }

        private async Task ConnectInternalAsync(string address, IPEndPoint ipEndPoint, string username, string password, CancellationToken cancellationToken)
        {
            try
            {
                await StateSyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    // if another thread somehow managed to get queued behind the semaphore while a previous thread was
                    // connecting, drop it and don't establish a new connection. it shouldn't be possible for this method to exit
                    // in states other than Disconnected or Connected | LoggedIn, and if the previous attempt resulted in a
                    // Disconnected state, we want to proceed.
                    if (State.HasFlag(SoulseekClientStates.Connected) && State.HasFlag(SoulseekClientStates.LoggedIn))
                    {
                        return;
                    }

                    ChangeState(SoulseekClientStates.Connecting, $"Connecting");

                    if (Options.EnableListener)
                    {
                        Listener = new Listener(Options.ListenIPAddress, Options.ListenPort, connectionOptions: Options.IncomingConnectionOptions);
                        Listener.Accepted += ListenerHandler.HandleConnection;
                        Listener.Start();
                    }

                    ServerConnection = ConnectionFactory.GetServerConnection(
                        ipEndPoint,
                        ServerConnection_Connected,
                        ServerConnection_Disconnected,
                        ServerConnection_MessageRead,
                        ServerConnection_MessageWritten,
                        Options.ServerConnectionOptions);

                    await ServerConnection.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    Address = address;
                    IPEndPoint = ipEndPoint;

                    ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggingIn, $"Logging in");

                    using var loginFailureCts = new CancellationTokenSource();
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, loginFailureCts.Token);

                    var loginWait = Waiter.Wait<LoginResponse>(new WaitKey(MessageCode.Server.Login), cancellationToken: cancellationToken);

                    // concatenate the login request with the set listen port command to prevent a race condition where remote
                    // users are notified of the login but the listen port is not yet set, resulting in the server reporting a
                    // port of 0 if the notified users attempt to connect (e.g. to re-request uploads). this is still possible,
                    // but much less likely. the server will not accept a listen port command prior to login.
                    var loginBytes = new LoginRequest(username, password).ToByteArray()
                        .Concat(new SetListenPortCommand(Options.ListenPort).ToByteArray())
                        .ToArray();

                    await ServerConnection.WriteAsync(loginBytes, cancellationToken).ConfigureAwait(false);

                    var response = await loginWait.ConfigureAwait(false);

                    if (response.Succeeded)
                    {
                        ServerInfo = ServerInfo.With(isSupporter: response.IsSupporter);
                        ServerInfoReceived?.Invoke(this, ServerInfo);

                        Username = username;

                        ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, "Logged in");

                        await SendConfigurationMessagesAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        loginFailureCts.Cancel();
                        throw new LoginRejectedException($"The server rejected login attempt: {response.Message}");
                    }
                }
                catch (Exception ex) when (!(ex is LoginRejectedException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
                {
                    throw new SoulseekClientException($"Failed to connect: {ex.Message}", ex);
                }
                finally
                {
                    StateSyncRoot.Release();
                }
            }
            catch (Exception ex)
            {
                Disconnect(ex.Message, exception: ex);
                throw;
            }
        }

        private async Task ConnectToUserInternalAsync(string username, bool invalidateCache, CancellationToken cancellationToken)
        {
            try
            {
                var endpoint = await GetUserEndPointAsync(username, cancellationToken).ConfigureAwait(false);

                if (invalidateCache && PeerConnectionManager.TryInvalidateMessageConnectionCache(username))
                {
                    Diagnostic.Debug($"Invalidated message connection cache for {username}");
                }

                await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to connect to user {username}: {ex.Message}", ex);
            }
        }

        private async Task<Transfer> DownloadToFileAsync(string username, string remoteFilename, string localFilename, long? size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            options = options.WithDisposalOptions(disposeOutputStreamOnCompletion: true);

            var fileMode = FileMode.Create;

            if (startOffset > 0)
            {
                fileMode = FileMode.Append;
            }

            return await DownloadToStreamAsync(username, remoteFilename, () => Task.FromResult((Stream)IOAdapter.GetFileStream(localFilename, fileMode, FileAccess.Write, FileShare.None)), size, startOffset, token, options, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Transfer> DownloadToStreamAsync(string username, string remoteFilename, Func<Task<Stream>> outputStreamFactory, long? size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            options ??= new TransferOptions();

            var download = new TransferInternal(TransferDirection.Download, username, remoteFilename, token, options)
            {
                StartOffset = startOffset,
                Size = size,
            };

            // we can't allow more than one concurrent transfer for the same file from the same user. we're already checking for this
            // in the public-scoped methods, by checking the contents of the Download/UploadDictionary, but that's not thread safe;
            // a caller can spam calls and get downloads through concurrently. this check is the last line of defense; if we make
            // it past here this unique combination is "locked" until the transfer is complete (as long as we remove it in the finally block!)
            var uniqueKey = $"{TransferDirection.Download}:{username}:{remoteFilename}";

            if (!UniqueKeyDictionary.TryAdd(key: uniqueKey, value: true))
            {
                throw new DuplicateTransferException($"Duplicate download of {remoteFilename} from {username} aborted");
            }

            // we also can't allow the same token to be used across different transfers. we're checking for this in the public-scoped
            // methods as well, but again, concurrent calls can sneak past.
            if (!DownloadDictionary.TryAdd(download.Token, download))
            {
                // we would have obtained exclusive access over this unique combination in the code above, so we need to release it
                UniqueKeyDictionary.TryRemove(uniqueKey, out _);

                throw new DuplicateTransferException($"Duplicate download of {remoteFilename} from {username} aborted");
            }

            var lastState = TransferStates.None;

            void UpdateState(TransferStates state)
            {
                download.State = state;
                var e = new TransferStateChangedEventArgs(previousState: lastState, transfer: new Transfer(download));
                lastState = state;
                options.StateChanged?.Invoke((e.PreviousState, e.Transfer));
                TransferStateChanged?.Invoke(this, e);
            }

            void UpdateProgress(long bytesDownloaded)
            {
                var lastBytes = download.BytesTransferred;
                download.UpdateProgress(bytesDownloaded);
                var e = new TransferProgressUpdatedEventArgs(lastBytes, new Transfer(download));
                options.ProgressUpdated?.Invoke((e.PreviousBytesTransferred, e.Transfer));
                TransferProgressUpdated?.Invoke(this, e);
            }

            var transferStartRequestedWaitKey = new WaitKey(MessageCode.Peer.TransferRequest, download.Username, download.Filename);
            bool globalSemaphoreAcquired = false;

            Stream outputStream = null;

            try
            {
                UpdateState(TransferStates.Queued | TransferStates.Locally);

                // acquire the global download semaphore to ensure we aren't trying to process more than the total allotted
                // concurrent downloads globally. if we hit this limit, downloads will stack up behind it and will be processed in
                // a first-in-first-out manner.
                await GlobalDownloadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                globalSemaphoreAcquired = true;
                Diagnostic.Debug($"Global download semaphore for file {Path.GetFileName(download.Filename)} to {username} acquired");

                var endpoint = await GetUserEndPointAsync(username, cancellationToken).ConfigureAwait(false);
                var peerConnection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken).ConfigureAwait(false);
                Diagnostic.Debug($"Fetched peer connection for download of {Path.GetFileName(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");

                // prepare two waits; one for the transfer response to confirm that our request is acknowledged and another for
                // the eventual transfer request sent when the peer is ready to send the file. the response message should be
                // returned immediately, while the request will be sent only when we've reached the front of the remote queue.
                var transferRequestAcknowledged = Waiter.Wait<TransferResponse>(
                    new WaitKey(MessageCode.Peer.TransferResponse, download.Username, download.Token), Options.PeerConnectionOptions.InactivityTimeout, cancellationToken);
                var transferStartRequested = Waiter.WaitIndefinitely<TransferRequest>(transferStartRequestedWaitKey, cancellationToken);

                // request the file
                await peerConnection.WriteAsync(new TransferRequest(TransferDirection.Download, token, remoteFilename), cancellationToken).ConfigureAwait(false);
                Diagnostic.Debug($"Wrote transfer request for download of {Path.GetFileName(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");

                UpdateState(TransferStates.Requested);

                var transferRequestAcknowledgement = await transferRequestAcknowledged.ConfigureAwait(false);
                Diagnostic.Debug($"Received transfer request ACK for download of {Path.GetFileName(download.Filename)} from {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");

                if (transferRequestAcknowledgement.IsAllowed)
                {
                    // the size of the remote file may have changed since it was sent in a search or browse response
                    if (download.Size.HasValue && download.Size.Value != transferRequestAcknowledgement.FileSize)
                    {
                        throw new TransferSizeMismatchException($"Transfer aborted: the remote size of {transferRequestAcknowledgement.FileSize} does not match expected size {download.Size}", download.Size.Value, transferRequestAcknowledgement.FileSize);
                    }

                    // the peer is ready to initiate the transfer immediately; we are bypassing their queue. fake a transition to
                    // queued for conststency
                    UpdateState(TransferStates.Queued | TransferStates.Remotely);

                    // if size wasn't supplied, use the size provided by the remote client. for files over 4gb, the value provided
                    // by the remote client will erroneously be reported as zero and the transfer will fail.
                    download.Size ??= transferRequestAcknowledgement.FileSize;

                    UpdateState(TransferStates.Initializing);

                    // connect to the peer to retrieve the file; for these types of transfers, we must initiate the transfer connection.
                    download.Connection = await PeerConnectionManager
                        .GetTransferConnectionAsync(username, endpoint, transferRequestAcknowledgement.Token, cancellationToken)
                        .ConfigureAwait(false);
                    Diagnostic.Debug($"Fetched transfer connection for download of {Path.GetFileName(download.Filename)} from {username} (id: {download.Connection.Id}, state: {download.Connection.State})");
                }
                else if (!string.Equals(transferRequestAcknowledgement.Message.TrimEnd('.'), "Queued", StringComparison.OrdinalIgnoreCase))
                {
                    throw new TransferRejectedException($"Transfer rejected: {transferRequestAcknowledgement.Message}");
                }
                else
                {
                    // the download is remotely queued, so put it in the local queue.
                    UpdateState(TransferStates.Queued | TransferStates.Remotely);

                    // wait for the peer to respond that they are ready to start the transfer
                    var transferStartRequest = await transferStartRequested.ConfigureAwait(false);

                    // the size of the remote file may have changed since it was sent in a search or browse response
                    if (download.Size.HasValue && download.Size.Value != transferStartRequest.FileSize)
                    {
                        throw new TransferSizeMismatchException($"Transfer aborted: the remote size of {transferStartRequest.FileSize} does not match expected size {download.Size}", download.Size.Value, transferStartRequest.FileSize);
                    }

                    // if size wasn't supplied, use the size provided by the remote client. for files over 4gb, the value provided
                    // by the remote client will erroneously be reported as zero and the transfer will fail.
                    download.Size ??= transferStartRequest.FileSize;
                    download.RemoteToken = transferStartRequest.Token;

                    UpdateState(TransferStates.Initializing);

                    // respond to the peer that we are ready to accept the file but first, get a fresh connection (or maybe it's
                    // cached in the manager) to the peer in case it disconnected and was purged while we were waiting.
                    peerConnection = await PeerConnectionManager
                        .GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken)
                        .ConfigureAwait(false);
                    Diagnostic.Debug($"Fetched peer connection for download of {Path.GetFileName(download.Filename)} from {username} (id: {peerConnection.Id}, state: {peerConnection.State})");

                    // prepare a wait for the eventual transfer connection
                    var connectionTask = PeerConnectionManager
                        .AwaitTransferConnectionAsync(download.Username, download.Filename, download.RemoteToken.Value, cancellationToken);

                    // initiate the connection
                    await peerConnection.WriteAsync(new TransferResponse(download.RemoteToken.Value, download.Size ?? 0), cancellationToken).ConfigureAwait(false);

                    try
                    {
                        download.Connection = await connectionTask.ConfigureAwait(false);
                        Diagnostic.Debug($"Fetched transfer connection for download of {Path.GetFileName(download.Filename)} from {username} (id: {download.Connection.Id}, state: {download.Connection.State})");
                    }
                    catch (ConnectionException)
                    {
                        // if the remote user doesn't initiate a transfer connection, try to initiate one from this end. the
                        // remote client in this scenario is most likely Nicotine+.
                        Diagnostic.Warning($"Attempting to initiate a second-chance transfer connection to {username} for download of {download.Filename}");

                        download.Connection = await PeerConnectionManager
                            .GetTransferConnectionAsync(username, endpoint, download.RemoteToken.Value, cancellationToken)
                            .ConfigureAwait(false);

                        Diagnostic.Warning($"Successfully established a second-chance transfer connection to {username} for download of {download.Filename}");
                    }
                }

                // create a task completion source that represents the disconnect of the transfer connection. this is one of two tasks that will 'race'
                // to determine the outcome of the download.
                var disconnectedTaskCancellationSource = new TaskCompletionSource<Exception>(cancellationToken);

                // once we have a 'winner' of the task race, we want to stop the loser as quickly as possible.
                // we'll do that with a cancellation token that we bind to the one that was passed into the method.
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var linkedCancellationToken = linkedCancellationTokenSource.Token;

                download.Connection.DataRead += (sender, e) => UpdateProgress(download.StartOffset + e.CurrentLength);
                download.Connection.Disconnected += (sender, e) =>
                {
                    if (e.Exception is OperationCanceledException || e.Exception is TimeoutException)
                    {
                        disconnectedTaskCancellationSource.SetException(e.Exception);
                        return;
                    }

                    disconnectedTaskCancellationSource.SetException(new ConnectionException($"Transfer failed: {e.Message}", e.Exception));
                };

                outputStream = await outputStreamFactory().ConfigureAwait(false);

                Diagnostic.Debug($"Seeking download of {Path.GetFileName(download.Filename)} from {username} to starting offset of {startOffset} bytes");
                var startOffsetBytes = BitConverter.GetBytes(startOffset);
                await download.Connection.WriteAsync(startOffsetBytes, linkedCancellationToken).ConfigureAwait(false);

                UpdateState(TransferStates.InProgress);
                UpdateProgress(download.StartOffset);

                var tokenBucket = DownloadTokenBucket;

                var readTask = download.Connection.ReadAsync(
                    length: download.Size.Value - startOffset,
                    outputStream: outputStream,
                    governor: async (requestedBytes, cancelToken) =>
                    {
                        var bytesGrantedByCaller = await options.Governor(new Transfer(download), requestedBytes, cancelToken).ConfigureAwait(false);
                        return await tokenBucket.GetAsync(Math.Min(requestedBytes, bytesGrantedByCaller), cancelToken).ConfigureAwait(false);
                    },
                    reporter: (attemptedBytes, grantedBytes, actualBytes) =>
                    {
                        options.Reporter?.Invoke(new Transfer(download), attemptedBytes, grantedBytes, actualBytes);
                        tokenBucket.Return(grantedBytes - actualBytes);
                    },
                    cancellationToken: linkedCancellationToken);

                var firstTask = await Task.WhenAny(
                    readTask, // we successfully read all of the data
                    disconnectedTaskCancellationSource.Task, // the connection is disconnected
                    download.RemoteTaskCompletionSource.Task).ConfigureAwait(false);

                // cancel the losing task
                linkedCancellationTokenSource.Cancel();

                if (firstTask == download.RemoteTaskCompletionSource.Task)
                {
                    // the remote client sent either UploadFailed (almost certain) or UploadDenied (not sure if possible);
                    // and we set either a TransferException (failed) or TransferRejectedException (denied) on this TCS
                    // in the event handlers above. await to force the exception to bubble up
                    await download.RemoteTaskCompletionSource.Task.ConfigureAwait(false);
                }
                else if (firstTask == disconnectedTaskCancellationSource.Task)
                {
                    // the logic in the Disconnected handler above was executed, and the transfer connection is dead
                    // await to force the exception to bubble up
                    await disconnectedTaskCancellationSource.Task.ConfigureAwait(false);
                }

                await readTask.ConfigureAwait(false);

                // update the state 'manually' so the final UpdateProgress() captures the Transfer in the terminal state
                UpdateProgress(download.StartOffset + (outputStream?.Position ?? 0));
                UpdateState(TransferStates.Completed | TransferStates.Succeeded);

                Diagnostic.Info($"Download of {Path.GetFileName(download.Filename)} from {username} complete ({startOffset + outputStream.Position} of {download.Size} bytes).");

                download.Connection.Disconnect("Transfer complete");

                return new Transfer(download);
            }
            catch (TransferRejectedException ex)
            {
                download.Exception = ex;
                UpdateState(TransferStates.Completed | TransferStates.Rejected);

                throw;
            }
            catch (TransferSizeMismatchException ex)
            {
                download.Exception = ex;
                UpdateState(TransferStates.Completed | TransferStates.Aborted);

                throw;
            }
            catch (OperationCanceledException ex)
            {
                download.Connection?.Disconnect("Transfer cancelled", ex);

                download.Exception = ex;
                UpdateProgress(download.StartOffset + (outputStream?.Position ?? 0));
                UpdateState(TransferStates.Completed | TransferStates.Cancelled);

                Diagnostic.Debug(ex.ToString());

                // cancelled async operations can throw TaskCanceledException, which is a subclass of OperationCanceledException,
                // but we want to be deterministic, so wrap and re-throw them.
                throw new OperationCanceledException("Operation cancelled", ex, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                download.Connection?.Disconnect("Transfer timed out", ex);

                download.Exception = ex;
                UpdateProgress(download.StartOffset + (outputStream?.Position ?? 0));
                UpdateState(TransferStates.Completed | TransferStates.TimedOut);

                Diagnostic.Debug(ex.ToString());

                throw;
            }
            catch (Exception ex)
            {
                download.Connection?.Disconnect("Transfer error", ex);

                download.Exception = ex;
                UpdateProgress(download.StartOffset + (outputStream?.Position ?? 0));
                UpdateState(TransferStates.Completed | TransferStates.Errored);

                Diagnostic.Debug(ex.ToString());

                if (ex is UserOfflineException)
                {
                    throw;
                }

                throw new SoulseekClientException($"Failed to download file {remoteFilename} from user {username}: {ex.Message}", ex);
            }
            finally
            {
                /*
                    do our best to clean up, in descending order of importance. this stuff is all 'nice to have' but shouldn't
                    leave the client in an inoperable state if it fails; more like we may leak resource handles over time if
                    we consistently fail to do this
                */
                try
                {
                    // clean up the waits in case the code threw before they were awaited.
                    try
                    {
                        Waiter.Cancel(transferStartRequestedWaitKey);
                    }
                    catch (Exception ex)
                    {
                        Diagnostic.Warning($"Failed to cancel wait for key {transferStartRequestedWaitKey}: {ex.Message}");
                    }

                    try
                    {
                        download.Connection?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Diagnostic.Warning($"Failed to dispose transfer connection for file {remoteFilename} from user {username}: {ex.Message}");
                    }

                    long finalStreamPosition = 0;

                    // attempt to get the actual final position of the stream for accurate record keeping. if something goes wrong,
                    // which can happen depending on the stream type (e.g. FileStream.Position can throw if the file is closed),
                    // set it to zero and let the consumer figure it out
                    try
                    {
                        finalStreamPosition = outputStream?.Position ?? 0;
                    }
                    catch (Exception ex)
                    {
                        Diagnostic.Warning($"Failed to determine final position of output stream for file {Path.GetFileName(download.Filename)} from {username}: {ex.Message}", ex);
                    }

                    if (options.DisposeOutputStreamOnCompletion && outputStream != null)
                    {
                        try
                        {
                            try
                            {
                                await outputStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                            }
                            finally
                            {
#if NETSTANDARD2_0
                                outputStream.Dispose();
#else
                                await outputStream.DisposeAsync().ConfigureAwait(false);
#endif
                            }
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to finalize output stream for file {Path.GetFileName(download.Filename)} from {username}: {ex.Message}", ex);
                        }
                    }
                }
                finally
                {
                    /*
                        make sure we do all of the absolutely-must-do cleanup; if any of this fails we will leave the
                        client in an inoperable state over time
                    */
                    if (globalSemaphoreAcquired)
                    {
                        try
                        {
                            GlobalDownloadSemaphore.Release(releaseCount: 1);
                            Diagnostic.Debug($"Global download semaphore for file {Path.GetFileName(download.Filename)} from {username} released");
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to release global download semaphore for file {Path.GetFileName(download.Filename)} to {username}: {ex.Message}");
                        }
                    }

                    DownloadDictionary.TryRemove(download.Token, out _);
                    UniqueKeyDictionary.TryRemove(uniqueKey, out _);
                }
            }
        }

        private async Task DropPrivateRoomMembershipInternalAsync(string roomName, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.PrivateRoomRemoved, roomName);
                var wait = Waiter.Wait(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new PrivateRoomDropMembershipCommand(roomName), cancellationToken).ConfigureAwait(false);

                await wait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to drop membership of private room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task DropPrivateRoomOwnershipInternalAsync(string roomName, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.PrivateRoomRemoved, roomName);
                var wait = Waiter.Wait(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new PrivateRoomDropOwnershipCommand(roomName), cancellationToken).ConfigureAwait(false);

                await wait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to drop ownership of private room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task<IReadOnlyCollection<Directory>> GetDirectoryContentsInternalAsync(string username, string directoryName, int token, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.FolderContentsResponse, username, token);
                var contentsWait = Waiter.Wait<IReadOnlyCollection<Directory>>(waitKey, cancellationToken: cancellationToken);

                var endpoint = await GetUserEndPointAsync(username, cancellationToken).ConfigureAwait(false);

                var connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken).ConfigureAwait(false);
                await connection.WriteAsync(new FolderContentsRequest(token, directoryName), cancellationToken).ConfigureAwait(false);

                var response = await contentsWait.ConfigureAwait(false);

                return response.ToList().AsReadOnly();
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to retrieve directory contents for {directoryName} from {username}: {ex.Message}", ex);
            }
        }

        private async Task<int> GetDownloadPlaceInQueueInternalAsync(string username, string filename, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.PlaceInQueueResponse, username, filename);
                var responseWait = Waiter.Wait<PlaceInQueueResponse>(waitKey, null, cancellationToken);

                var endpoint = await GetUserEndPointAsync(username, cancellationToken).ConfigureAwait(false);
                var connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken).ConfigureAwait(false);
                await connection.WriteAsync(new PlaceInQueueRequest(filename), cancellationToken).ConfigureAwait(false);

                var response = await responseWait.ConfigureAwait(false);

                return response.PlaceInQueue;
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to fetch place in queue for download of {filename} from {username}: {ex.Message}", ex);
            }
        }

        private async Task<IPEndPoint> GetUserEndPointInternalAsync(string username, CancellationToken cancellationToken)
        {
            var cache = Options.UserEndPointCache;

            if (cache != default)
            {
                static void TryCacheOperation(Action action)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        throw new UserEndPointCacheException($"Exception retrieving or updating user endpoint cache: {ex.Message}", ex);
                    }
                }

                bool cached = false;
                IPEndPoint endPoint = default;

                TryCacheOperation(() => cached = cache.TryGet(username, out endPoint));

                if (cached)
                {
                    Diagnostic.Debug($"EndPoint cache HIT for {username}: {endPoint}");
                    return endPoint;
                }

                SemaphoreSlim semaphore;
                Task semaphoreWaitTask;

                await UserEndPointSemaphoreSyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    semaphore = UserEndPointSemaphores.GetOrAdd(username, new SemaphoreSlim(1, 1));
                    semaphoreWaitTask = semaphore.WaitAsync(cancellationToken);
                }
                finally
                {
                    UserEndPointSemaphoreSyncRoot.Release();
                }

                await semaphoreWaitTask.ConfigureAwait(false);

                try
                {
                    TryCacheOperation(() => cached = cache.TryGet(username, out endPoint));

                    if (cached)
                    {
                        Diagnostic.Debug($"EndPoint cache HIT for {username}: {endPoint}");
                        return endPoint;
                    }

                    endPoint = await GetEndPoint().ConfigureAwait(false);

                    TryCacheOperation(() => cache.AddOrUpdate(username, endPoint));

                    Diagnostic.Debug($"EndPoint cache MISS for {username}: {endPoint}");

                    return endPoint;
                }
                finally
                {
                    semaphore.Release();
                }
            }

            return await GetEndPoint().ConfigureAwait(false);

            async Task<IPEndPoint> GetEndPoint()
            {
                try
                {
                    var waitKey = new WaitKey(MessageCode.Server.GetPeerAddress, username);
                    var addressWait = Waiter.Wait<UserAddressResponse>(waitKey, cancellationToken: cancellationToken);

                    await ServerConnection.WriteAsync(new UserAddressRequest(username), cancellationToken).ConfigureAwait(false);

                    var response = await addressWait.ConfigureAwait(false);

                    if (response.IPAddress.Equals(IPAddress.Any))
                    {
                        throw new UserOfflineException($"User {username} appears to be offline");
                    }

                    return new IPEndPoint(response.IPAddress, response.Port);
                }
                catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
                {
                    throw new UserEndPointException($"Failed to retrieve endpoint for user {username}: {ex.Message}", ex);
                }
            }
        }

        private async Task<UserInfo> GetUserInfoInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Peer.InfoResponse, username);
                var infoWait = Waiter.Wait<UserInfo>(waitKey, cancellationToken: cancellationToken);

                var endpoint = await GetUserEndPointAsync(username, cancellationToken).ConfigureAwait(false);

                var connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken).ConfigureAwait(false);
                await connection.WriteAsync(new UserInfoRequest(), cancellationToken).ConfigureAwait(false);

                var response = await infoWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to retrieve information for user {username}: {ex.Message}", ex);
            }
        }

        private async Task<bool> GetUserPrivilegedInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.UserPrivileges, username);
                var wait = Waiter.Wait<bool>(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new UserPrivilegesRequest(username), cancellationToken).ConfigureAwait(false);

                var result = await wait.ConfigureAwait(false);
                return result;
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to get privileges for {username}: {ex.Message}", ex);
            }
        }

        private async Task<UserStatistics> GetUserStatisticsInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var getStatisticsWait = Waiter.Wait<UserStatistics>(new WaitKey(MessageCode.Server.GetUserStats, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new UserStatisticsRequest(username), cancellationToken).ConfigureAwait(false);

                return await getStatisticsWait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to retrieve statistics for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task<UserStatus> GetUserStatusInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var getStatusWait = Waiter.Wait<UserStatus>(new WaitKey(MessageCode.Server.GetStatus, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new UserStatusRequest(username), cancellationToken).ConfigureAwait(false);

                return await getStatusWait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is UserOfflineException) && !(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to retrieve status for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task GrantUserPrivilegesInternalAsync(string username, int days, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new GivePrivilegesCommand(username, days), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to grant {days} days of privileges to {username}: {ex.Message}", ex);
            }
        }

        private async Task<RoomData> JoinRoomInternalAsync(string roomName, bool isPrivate, CancellationToken cancellationToken)
        {
            try
            {
                // the server may send a CannotJoinRoom message, which will cause the wait to throw RoomJoinForbiddenException if
                // the room is already joined, the server won't respond at all, which will eventually cause a TimeoutException
                var joinRoomWait = Waiter.Wait<RoomData>(new WaitKey(MessageCode.Server.JoinRoom, roomName), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new JoinRoomRequest(roomName, isPrivate), cancellationToken).ConfigureAwait(false);

                try
                {
                    var response = await joinRoomWait.ConfigureAwait(false);
                    return response;
                }
                catch (TimeoutException)
                {
                    throw new NoResponseException($"The server didn't respond to the request to join chat room {roomName}. This probably indicates that the room is already joined.");
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException) && !(ex is RoomJoinForbiddenException) && !(ex is NoResponseException))
            {
                throw new SoulseekClientException($"Failed to join chat room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task LeaveRoomInternalAsync(string roomName, CancellationToken cancellationToken)
        {
            try
            {
                var leaveRoomWait = Waiter.Wait(new WaitKey(MessageCode.Server.LeaveRoom, roomName), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new LeaveRoomRequest(roomName), cancellationToken).ConfigureAwait(false);

                try
                {
                    await leaveRoomWait.ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    throw new NoResponseException($"The server didn't respond to the request to leave chat room {roomName}.  This probably indicates that the room is not joined.");
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException) && !(ex is NoResponseException))
            {
                throw new SoulseekClientException($"Failed to leave chat room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task<bool> ReconfigureOptionsInternalAsync(SoulseekClientOptionsPatch patch, CancellationToken cancellationToken)
        {
            bool IsConnected() => State.HasFlag(SoulseekClientStates.Connected) && State.HasFlag(SoulseekClientStates.LoggedIn);

            await StateSyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // capture the state. at this point the client is either Connected | LoggedIn, or not. if not, a reconnect will
                // not be required and we won't send configuration messages. it isn't possible to transition into Connected |
                // LoggedIn because of the SyncRoot, but we can transition *from* Connected | LoggedIn to Disconnected or
                // Disconnecting. if this happens, we will return false, indicating that a reconnect is not necessary. this is
                // safe because the client can't be reconnected while this code holds the SyncRoot semaphore.
                bool connected = IsConnected();
                bool reconnectRequired = false;

                var enableDistributedNetworkChanged = patch.EnableDistributedNetwork.HasValue && patch.EnableDistributedNetwork.Value != Options.EnableDistributedNetwork;
                var acceptDistributedChildrenChanged = patch.AcceptDistributedChildren.HasValue && patch.AcceptDistributedChildren.Value != Options.AcceptDistributedChildren;
                var distributedConnectionOptionsChanged = patch.DistributedConnectionOptions != null && patch.DistributedConnectionOptions != Options.DistributedConnectionOptions;

                var distribledNetworkWasDisabled = enableDistributedNetworkChanged && !patch.EnableDistributedNetwork.Value;
                var distributedChildrenWereDisabled = acceptDistributedChildrenChanged && !patch.AcceptDistributedChildren.Value;

                if (connected && (distribledNetworkWasDisabled || distributedChildrenWereDisabled || distributedConnectionOptionsChanged))
                {
                    // reconnect to avoid state issues that might be caused by disabling this on the fly. if we are disabling the
                    // big concerns are in half-open parent or child connections; we can stop the server from sending
                    // NetInfo/demote us from branch root by sending HaveNoParents=false. if changing from disabled to enabled,
                    // there's no restart required.
                    reconnectRequired = true;
                }

                var serverConnectionOptionsChanged = patch.ServerConnectionOptions != null && patch.ServerConnectionOptions != Options.ServerConnectionOptions;

                if (connected && serverConnectionOptionsChanged)
                {
                    // required because we need to re-instantiate ServerConnection in order to pass it the new options
                    reconnectRequired = true;
                }

                var enableListenerChanged = patch.EnableListener.HasValue && patch.EnableListener.Value != Options.EnableListener;
                var listenAddressChanged = patch.ListenIPAddress != null && !patch.ListenIPAddress.Equals(Options.ListenIPAddress);
                var listenPortChanged = patch.ListenPort.HasValue && patch.ListenPort.Value != Options.ListenPort;
                var incomingConnectionOptionsChanged = patch.IncomingConnectionOptions != null && patch.IncomingConnectionOptions != Options.IncomingConnectionOptions;

                if (enableListenerChanged || listenAddressChanged || listenPortChanged || incomingConnectionOptionsChanged)
                {
                    var wasListening = Listener?.Listening ?? false;

                    Listener?.Stop();
                    Listener = null;

                    Options = Options.With(
                        enableListener: patch.EnableListener,
                        listenIPAddress: patch.ListenIPAddress,
                        listenPort: patch.ListenPort,
                        incomingConnectionOptions: patch.IncomingConnectionOptions);

                    if (wasListening && Options.EnableListener)
                    {
                        Listener = new Listener(Options.ListenIPAddress, Options.ListenPort, Options.IncomingConnectionOptions);
                        Listener.Accepted += ListenerHandler.HandleConnection;
                        Listener.Start();
                    }
                }

                var maximumUploadSpeedChanged = patch.MaximumUploadSpeed.HasValue && patch.MaximumUploadSpeed.Value != Options.MaximumUploadSpeed;
                var maximumDownloadSpeedChanged = patch.MaximumDownloadSpeed.HasValue && patch.MaximumDownloadSpeed.Value != Options.MaximumDownloadSpeed;

                Options = Options.With(
                    enableDistributedNetwork: patch.EnableDistributedNetwork,
                    acceptDistributedChildren: patch.AcceptDistributedChildren,
                    distributedChildLimit: patch.DistributedChildLimit,
                    maximumUploadSpeed: patch.MaximumUploadSpeed,
                    maximumDownloadSpeed: patch.MaximumDownloadSpeed,
                    acceptPrivateRoomInvitations: patch.AcceptPrivateRoomInvitations,
                    deduplicateSearchRequests: patch.DeduplicateSearchRequests,
                    autoAcknowledgePrivateMessages: patch.AutoAcknowledgePrivateMessages,
                    autoAcknowledgePrivilegeNotifications: patch.AutoAcknowledgePrivilegeNotifications,
                    serverConnectionOptions: patch.ServerConnectionOptions,
                    peerConnectionOptions: patch.PeerConnectionOptions,
                    transferConnectionOptions: patch.TransferConnectionOptions,
                    incomingConnectionOptions: patch.IncomingConnectionOptions,
                    distributedConnectionOptions: patch.DistributedConnectionOptions,
                    userEndPointCache: patch.UserEndPointCache,
                    searchResponseResolver: patch.SearchResponseResolver,
                    searchResponseCache: patch.SearchResponseCache,
                    browseResponseResolver: patch.BrowseResponseResolver,
                    directoryContentsResolver: patch.DirectoryContentsResolver,
                    userInfoResolver: patch.UserInfoResolver,
                    enqueueDownload: patch.EnqueueDownload,
                    placeInQueueResolver: patch.PlaceInQueueResolver);

                if (maximumUploadSpeedChanged)
                {
                    UploadTokenBucket.SetCapacity((Options.MaximumUploadSpeed * 1024L) / 10);
                }

                if (maximumDownloadSpeedChanged)
                {
                    DownloadTokenBucket.SetCapacity((Options.MaximumDownloadSpeed * 1024L) / 10);
                }

                Diagnostic.Info("Options reconfigured successfully");

                if (IsConnected())
                {
                    Diagnostic.Debug($"Updating server with latest configuration");
                    await SendConfigurationMessagesAsync(cancellationToken).ConfigureAwait(false);

                    if (reconnectRequired)
                    {
                        Diagnostic.Warning("Server reconnect required for options to fully take effect");
                    }

                    return reconnectRequired;
                }

                return false;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to reconfigure options: {ex.Message}.  Any successful reconfiguration has not been rolled back; retry with the same patch until successful or consider this as a fatal Exception", ex);
            }
            finally
            {
                StateSyncRoot.Release();
            }
        }

        private async Task RemovePrivateRoomMemberInternalAsync(string roomName, string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.PrivateRoomRemoveUser, roomName, username);
                var wait = Waiter.Wait(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new PrivateRoomRemoveUser(roomName, username), cancellationToken).ConfigureAwait(false);

                await wait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to remove user {username} as member of private room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task RemovePrivateRoomModeratorInternalAsync(string roomName, string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.Server.PrivateRoomRemoveOperator, roomName, username);
                var wait = Waiter.Wait(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteAsync(new PrivateRoomRemoveOperator(roomName, username), cancellationToken).ConfigureAwait(false);

                await wait.ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to remove user {username} as moderator of private room {roomName}: {ex.Message}", ex);
            }
        }

        private async Task<Search> SearchToCallbackAsync(SearchQuery query, Action<SearchResponse> responseHandler, SearchScope scope, int token, SearchOptions options, CancellationToken cancellationToken)
        {
            var search = new SearchInternal(query, scope, token, options);
            var lastState = SearchStates.None;

            void UpdateState(SearchStates state)
            {
                search.SetState(state);
                var e = new SearchStateChangedEventArgs(previousState: lastState, search: new Search(search));
                lastState = state;
                options.StateChanged?.Invoke((e.PreviousState, e.Search));
                SearchStateChanged?.Invoke(this, e);
            }

            try
            {
                Searches.TryAdd(search.Token, search);
                UpdateState(SearchStates.Requested);

                Diagnostic.Debug($"Attempting to acquire search semaphore for search '{query.SearchText}' ({SearchSemaphore.CurrentCount} available)");
                UpdateState(SearchStates.Queued);

                // obtain a semaphore, or wait until one becomes available. this is done as a protective measure
                // against automation that may not think to do this, resulting in the server being bombarded by requests
                await SearchSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                Diagnostic.Debug($"Acquired search semaphore for search '{query.SearchText}'");

                try
                {
                    var message = scope.Type switch
                    {
                        SearchScopeType.Room => new RoomSearchRequest(scope.Subjects.First(), search.Query.SearchText, search.Token).ToByteArray(),
                        SearchScopeType.User => scope.Subjects.SelectMany(u => new UserSearchRequest(u, search.Query.SearchText, search.Token).ToByteArray()).ToArray(),
                        SearchScopeType.Wishlist => new WishlistSearchRequest(search.Query.SearchText, search.Token).ToByteArray(),
                        _ => new SearchRequest(search.Query.SearchText, search.Token).ToByteArray()
                    };

                    search.ResponseReceived = (response) =>
                    {
                        responseHandler(response);

                        var e = new SearchResponseReceivedEventArgs(response, new Search(search));
                        options.ResponseReceived?.Invoke((e.Search, e.Response));
                        SearchResponseReceived?.Invoke(this, e);
                    };

                    await ServerConnection.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                    UpdateState(SearchStates.InProgress);

                    await search.WaitForCompletion(cancellationToken).ConfigureAwait(false);
                    UpdateState(SearchStates.Completed | search.State);

                    Diagnostic.Debug($"Search for '{query.SearchText}' completed: {search.State}");

                    return new Search(search);
                }
                finally
                {
                    SearchSemaphore.Release(releaseCount: 1);
                    Diagnostic.Debug($"Released search semaphore for search '{query.SearchText}' ({SearchSemaphore.CurrentCount} available)");
                }
            }
            catch (OperationCanceledException)
            {
                search.Complete(SearchStates.Cancelled);
                UpdateState(SearchStates.Completed | SearchStates.Cancelled);

                throw;
            }
            catch (TimeoutException)
            {
                // note that a timeout in this context is a timeout writing the search request to the server.
                // if a search 'times out' waiting for results, it completes successfully with the TimedOut state
                // and does not throw
                search.Complete(SearchStates.Errored);
                UpdateState(SearchStates.Completed | SearchStates.Errored);

                throw;
            }
            catch (Exception ex)
            {
                search.Complete(SearchStates.Errored);
                UpdateState(SearchStates.Completed | SearchStates.Errored);

                throw new SoulseekClientException($"Failed to search for {query.SearchText} ({token}): {ex.Message}", ex);
            }
            finally
            {
                Searches.TryRemove(search.Token, out _);
                search.Dispose();
            }
        }

        private async Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchToCollectionAsync(SearchQuery query, SearchScope scope, int token, SearchOptions options, CancellationToken cancellationToken)
        {
            var responseBag = new ConcurrentBag<SearchResponse>();

            void ResponseReceived(SearchResponse response)
            {
                responseBag.Add(response);
            }

            var search = await SearchToCallbackAsync(query, ResponseReceived, scope, token, options, cancellationToken).ConfigureAwait(false);
            return (search, responseBag.ToList().AsReadOnly());
        }

        private async Task SendConfigurationMessagesAsync(CancellationToken cancellationToken)
        {
            // the client sends an undocumented message in the format 02/listen port/01/obfuscated port. we don't support
            // obfuscation, so we send only the listen port. it probably wouldn't hurt to send an 00 afterwards.
            await ServerConnection.WriteAsync(new SetListenPortCommand(Options.ListenPort), cancellationToken).ConfigureAwait(false);

            await ServerConnection.WriteAsync(new PrivateRoomToggle(Options.AcceptPrivateRoomInvitations), cancellationToken).ConfigureAwait(false);

            await DistributedConnectionManager.UpdateStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task SendPrivateMessageInternalAsync(string username, string message, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new PrivateMessageCommand(username, message), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to send private message to user {username}: {ex.Message}", ex);
            }
        }

        private async Task SendRoomMessageInternalAsync(string roomName, string message, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new RoomMessageCommand(roomName, message), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is TimeoutException))
            {
                throw new SoulseekClientException($"Failed to send message to room {roomName}: {ex.Message}", ex);
            }
        }

        private void ServerConnection_Connected(object sender, EventArgs e)
        {
            ChangeState(SoulseekClientStates.Connected, $"Connected to {IPEndPoint}");
        }

        private void ServerConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            Disconnect(e.Message, e.Exception);
        }

        private void ServerConnection_MessageRead(object sender, MessageEventArgs e)
        {
            ServerMessageHandler.HandleMessageRead(sender, e);
        }

        private void ServerConnection_MessageWritten(object sender, MessageEventArgs e)
        {
            ServerMessageHandler.HandleMessageWritten(sender, e);
        }

        private async Task UnwatchUserInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteAsync(new UnwatchUserCommand(username), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to unwatch user {username}: {ex.Message}", ex);
            }
        }

        private async Task<Transfer> UploadFromFileAsync(string username, string remoteFilename, string localFilename, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            options = options.WithDisposalOptions(disposeInputStreamOnCompletion: true);
            var ioAdapter = IOAdapter;

            var length = ioAdapter.GetFileInfo(localFilename).Length;

            return await UploadFromStreamAsync(username, remoteFilename, length, (_) => Task.FromResult((Stream)ioAdapter.GetFileStream(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read)), token, options, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Transfer> UploadFromStreamAsync(string username, string remoteFilename, long size, Func<long, Task<Stream>> inputStreamFactory, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            options ??= new TransferOptions();

            var upload = new TransferInternal(TransferDirection.Upload, username, remoteFilename, token, options)
            {
                Size = size,
            };

            // we can't allow more than one concurrent transfer for the same file from the same user. we're already checking for this
            // in the public-scoped methods, by checking the contents of the Download/UploadDictionary, but that's not thread safe;
            // a caller can spam calls and get transfers through concurrently. this check is the last line of defense; if we make
            // it past here this unique combination is "locked" until the transfer is complete (as long as we remove it in the finally block!)
            var uniqueKey = $"{TransferDirection.Upload}:{username}:{remoteFilename}";

            if (!UniqueKeyDictionary.TryAdd(key: uniqueKey, value: true))
            {
                throw new DuplicateTransferException($"Duplicate upload of {remoteFilename} to {username} aborted");
            }

            // we also can't allow the same token to be used across different transfers. we're checking for this in the public-scoped
            // methods as well, but again, concurrent calls can sneak past.
            if (!UploadDictionary.TryAdd(upload.Token, upload))
            {
                // we would have obtained exclusive access over this unique combination in the code above, so we need to release it
                UniqueKeyDictionary.TryRemove(uniqueKey, out _);

                throw new DuplicateTransferException($"Duplicate upload of {remoteFilename} to {username} aborted");
            }

            var lastState = TransferStates.None;

            void UpdateState(TransferStates state)
            {
                upload.State = state;
                var e = new TransferStateChangedEventArgs(previousState: lastState, transfer: new Transfer(upload));
                lastState = state;
                options.StateChanged?.Invoke((e.PreviousState, e.Transfer));
                TransferStateChanged?.Invoke(this, e);
            }

            void UpdateProgress(long bytesUploaded)
            {
                var lastBytes = upload.BytesTransferred;
                upload.UpdateProgress(bytesUploaded);
                var e = new TransferProgressUpdatedEventArgs(lastBytes, new Transfer(upload));
                options.ProgressUpdated?.Invoke((e.PreviousBytesTransferred, e.Transfer));
                TransferProgressUpdated?.Invoke(this, e);
            }

            IPEndPoint endpoint = null;
            bool semaphoreAcquired = false;
            bool uploadSlotAcquired = false;
            bool globalSemaphoreAcquired = false;

            Stream inputStream = null;

            SemaphoreSlim semaphore = null;
            Task semaphoreWaitTask;

            try
            {
                await UploadSemaphoreSyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    // fetch (or create) an upload semaphore for this user. Soulseek NS can't handle concurrent downloads from the
                    // same source, so we need to enforce this regardless of what downstream implementations do.
                    semaphore = UploadSemaphores.GetOrAdd(username, new SemaphoreSlim(initialCount: Options.MaximumConcurrentUploadsPerUser, maxCount: Options.MaximumConcurrentUploadsPerUser));
                    semaphoreWaitTask = semaphore.WaitAsync(cancellationToken);
                }
                finally
                {
                    UploadSemaphoreSyncRoot.Release();
                }

                UpdateState(TransferStates.Queued | TransferStates.Locally);

                // permissive stage 1: acquire the per-user semaphore to ensure we aren't trying to process more than the allotted
                // concurrent uploads to this user, and ensure that we aren't trying to acquire a slot for an upload until the
                // requesting user is ready to receive it
                await semaphoreWaitTask.ConfigureAwait(false);
                semaphoreAcquired = true;
                Diagnostic.Debug($"Upload semaphore for file {Path.GetFileName(upload.Filename)} to {username} acquired");

                // permissive stage 2: acquire an upload slot from the calling code
                try
                {
                    await options.SlotAwaiter(new Transfer(upload), cancellationToken).ConfigureAwait(false);
                    uploadSlotAcquired = true;
                    Diagnostic.Debug($"Upload slot for file {Path.GetFileName(upload.Filename)} to {username} acquired");
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    throw new TransferException($"Failed to acquire an upload slot for file {Path.GetFileName(upload.Filename)} to {username}: {ex.Message}", ex);
                }

                // permissive stage 3: acquire the global upload semaphore to ensure we aren't trying to process more than the
                // total allotted concurrent uploads globally. if we hit this limit, uploads will stack up behind it and will be
                // processed in a round-robin-like fashion due to the limit on per-user concurrency. calling code can avoid this
                // by providing an implementation of AcquireSlot() that won't exceed the maximum concurrent upload limit
                await GlobalUploadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                globalSemaphoreAcquired = true;
                Diagnostic.Debug($"Global upload semaphore for file {Path.GetFileName(upload.Filename)} to {username} acquired");

                // all permissives have been given fetch the user endpoint and request that the transfer begins
                endpoint = await GetUserEndPointAsync(username, cancellationToken).ConfigureAwait(false);
                var messageConnection = await PeerConnectionManager
                    .GetOrAddMessageConnectionAsync(username, endpoint, cancellationToken)
                    .ConfigureAwait(false);
                Diagnostic.Debug($"Fetched peer connection for upload of {Path.GetFileName(upload.Filename)} to {username} (id: {messageConnection.Id}, state: {messageConnection.State})");

                // prepare a wait for the transfer response
                var transferRequestAcknowledged = Waiter.Wait<TransferResponse>(
                    new WaitKey(MessageCode.Peer.TransferResponse, upload.Username, upload.Token), Options.PeerConnectionOptions.InactivityTimeout, cancellationToken);

                // request to start the upload
                var transferRequest = new TransferRequest(TransferDirection.Upload, upload.Token, upload.Filename, size);

                await messageConnection.WriteAsync(transferRequest, cancellationToken).ConfigureAwait(false);
                Diagnostic.Debug($"Wrote transfer request for upload of {Path.GetFileName(upload.Filename)} to {username} (id: {messageConnection.Id}, state: {messageConnection.State})");

                UpdateState(TransferStates.Requested);

                var transferRequestAcknowledgement = await transferRequestAcknowledged.ConfigureAwait(false);
                Diagnostic.Debug($"Received transfer request ACK for upload of {Path.GetFileName(upload.Filename)} to {username}: allowed: {transferRequestAcknowledgement.IsAllowed}, message: {transferRequestAcknowledgement.Message} (token: {token})");

                if (!transferRequestAcknowledgement.IsAllowed)
                {
                    throw new TransferRejectedException($"Transfer rejected: {transferRequestAcknowledgement.Message}");
                }

                UpdateState(TransferStates.Initializing);

                upload.Connection = await PeerConnectionManager
                    .GetTransferConnectionAsync(upload.Username, endpoint, upload.Token, cancellationToken)
                    .ConfigureAwait(false);
                Diagnostic.Debug($"Fetched transfer connection for upload of {Path.GetFileName(upload.Filename)} to {username} (id: {upload.Connection.Id}, state: {upload.Connection.State})");

                // create a task completion source that represents the disconnect of the transfer connection. this is one of two tasks that will 'race'
                // to determine the outcome of the upload.
                var disconnectedTaskCancellationSource = new TaskCompletionSource<Exception>(cancellationToken);

                // once we have a 'winner' of the task race, we want to stop the loser as quickly as possible.
                // we'll do that with a cancellation token that we bind to the one that was passed into the method.
                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var linkedCancellationToken = linkedCancellationTokenSource.Token;

                upload.Connection.DataWritten += (sender, e) => UpdateProgress(upload.StartOffset + e.CurrentLength);
                upload.Connection.Disconnected += (sender, e) =>
                {
                    if (e.Exception is OperationCanceledException || e.Exception is TimeoutException)
                    {
                        disconnectedTaskCancellationSource.SetException(e.Exception);
                        return;
                    }

                    disconnectedTaskCancellationSource.SetException(new ConnectionException($"Transfer failed: {e.Message}", e.Exception));
                };

                try
                {
                    var startOffsetBytes = await upload.Connection.ReadAsync(8, cancellationToken).ConfigureAwait(false);
                    upload.StartOffset = BitConverter.ToInt64(startOffsetBytes, 0);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not TimeoutException)
                {
                    Diagnostic.Debug($"Failed to read start offset for upload of {Path.GetFileName(upload.Filename)} to {username}: {ex.Message}");
                    throw new MessageReadException($"Failed to read transfer start offset: {ex.Message}", ex);
                }

                if (upload.StartOffset > upload.Size)
                {
                    throw new TransferException($"Requested start offset of {upload.StartOffset} bytes exceeds file length of {upload.Size} bytes");
                }

                Diagnostic.Debug($"Resolving input stream for upload of {Path.GetFileName(upload.Filename)} to {username}");
                inputStream = await inputStreamFactory(upload.StartOffset).ConfigureAwait(false);

                if (upload.StartOffset > 0 && options.SeekInputStreamAutomatically)
                {
                    if (!inputStream.CanSeek)
                    {
                        throw new TransferException($"Requested non-zero start offset but input stream does not support seeking");
                    }

                    Diagnostic.Debug($"Seeking upload of {Path.GetFileName(upload.Filename)} to {username} to starting offset of {upload.StartOffset} bytes");
                    inputStream.Seek(upload.StartOffset, SeekOrigin.Begin);
                }

                UpdateState(TransferStates.InProgress);
                UpdateProgress(upload.StartOffset);

                Task writeTask;

                // don't try to write to the connection if the peer is re-requesting a file that's already complete
                if (upload.Size.Value - upload.StartOffset > 0)
                {
                    var tokenBucket = UploadTokenBucket;

                    writeTask = upload.Connection.WriteAsync(
                        length: upload.Size.Value - upload.StartOffset,
                        inputStream: inputStream,
                        governor: async (requestedBytes, cancelToken) =>
                        {
                            var bytesGrantedByCaller = await options.Governor(new Transfer(upload), requestedBytes, cancelToken).ConfigureAwait(false);
                            return await tokenBucket.GetAsync(Math.Min(requestedBytes, bytesGrantedByCaller), cancellationToken).ConfigureAwait(false);
                        },
                        reporter: (attemptedBytes, grantedBytes, actualBytes) =>
                        {
                            options.Reporter?.Invoke(new Transfer(upload), attemptedBytes, grantedBytes, actualBytes);
                            tokenBucket.Return(grantedBytes - actualBytes);
                        },
                        cancellationToken: cancellationToken);
                }
                else
                {
                    writeTask = Task.CompletedTask;
                }

                var firstTask = await Task.WhenAny(
                    writeTask,
                    disconnectedTaskCancellationSource.Task).ConfigureAwait(false);

                // cancel the losing task
                linkedCancellationTokenSource.Cancel();

                if (firstTask == disconnectedTaskCancellationSource.Task)
                {
                    // the logic in the Disconnected handler above was executed, and the transfer connection is dead
                    // await to force the exception to bubble up
                    await disconnectedTaskCancellationSource.Task.ConfigureAwait(false);
                }

                await writeTask.ConfigureAwait(false);

                // figure out how and when to disconnect the connection. ideally the receiving end disconnects; this way we
                // know they've gotten all of the data. we can encourage this by attempting to read data, which works well for
                // Soulseek NS and Qt, but takes some time with Nicotine+. if the receiving end won't disconnect, wait the
                // configured MaximumLingerTime and disconnect on our end. the receiver may not have gotten all the data if it
                // comes to this, so linger time shouldn't be less than a couple of seconds.
                try
                {
                    var lingerStartTime = DateTime.UtcNow;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (lingerStartTime.AddMilliseconds(options.MaximumLingerTime) <= DateTime.UtcNow)
                        {
                            upload.Connection.Disconnect("Transfer complete, maximum linger time exceeded");
                            Diagnostic.Warning($"Transfer connection for upload of {Path.GetFileName(upload.Filename)} to {username} forcibly closed after exceeding maximum linger time of {options.MaximumLingerTime}ms.");
                            break;
                        }

                        await upload.Connection.ReadAsync(1, cancellationToken).ConfigureAwait(false);
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (ConnectionReadException)
                {
                    // swallow this specific exception; we're expecting it when the connection closes.
                }

                UpdateProgress(inputStream?.Position ?? 0);
                UpdateState(TransferStates.Completed | TransferStates.Succeeded);

                Diagnostic.Info($"Upload of {Path.GetFileName(upload.Filename)} to {username} complete ({inputStream.Position} of {upload.Size} bytes).");

                return new Transfer(upload);
            }
            catch (TransferRejectedException ex)
            {
                upload.Exception = ex;
                UpdateState(TransferStates.Completed | TransferStates.Rejected);

                throw;
            }
            catch (OperationCanceledException ex)
            {
                upload.Connection?.Disconnect("Transfer cancelled", ex);

                upload.Exception = ex;
                UpdateProgress(inputStream?.Position ?? 0);
                UpdateState(TransferStates.Completed | TransferStates.Cancelled);

                Diagnostic.Debug(ex.ToString());

                // cancelled async operations can throw TaskCanceledException, which is a subclass of OperationCanceledException,
                // but we want to be deterministic, so wrap and re-throw them.
                throw new OperationCanceledException("Operation cancelled", ex, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                upload.Connection?.Disconnect("Transfer timed out", ex);

                upload.Exception = ex;
                UpdateProgress(inputStream?.Position ?? 0);
                UpdateState(TransferStates.Completed | TransferStates.TimedOut);

                Diagnostic.Debug(ex.ToString());

                throw;
            }
            catch (Exception ex)
            {
                upload.Connection?.Disconnect("Transfer error", ex);

                upload.Exception = ex;
                UpdateProgress(inputStream?.Position ?? 0);
                UpdateState(TransferStates.Completed | TransferStates.Errored);

                Diagnostic.Debug(ex.ToString());

                if (ex is UserOfflineException)
                {
                    throw;
                }

                throw new SoulseekClientException($"Failed to upload file {remoteFilename} to user {username}: {ex.Message}", ex);
            }
            finally
            {
                /*
                    do our best to clean up, in descending order of importance. this stuff is all 'nice to have' but shouldn't
                    leave the client in an inoperable state if it fails; more like we may leak resource handles over time if
                    we consistently fail to do this
                */
                try
                {
                    try
                    {
                        upload.Connection?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Diagnostic.Warning($"Failed to dispose transfer connection for file {remoteFilename} to user {username}: {ex.Message}");
                    }

                    long finalStreamPosition = 0;

                    // attempt to get the actual final position of the stream for accurate record keeping. if something goes wrong,
                    // which can happen depending on the stream type (e.g. FileStream.Position can throw if the file is closed),
                    // set it to zero and let the consumer figure it out
                    try
                    {
                        finalStreamPosition = inputStream?.Position ?? 0;
                    }
                    catch (Exception ex)
                    {
                        Diagnostic.Warning($"Failed to determine final position of input stream for file {Path.GetFileName(upload.Filename)} to {username}: {ex.Message}", ex);
                    }

                    if (options.DisposeInputStreamOnCompletion && inputStream != null)
                    {
                        try
                        {
#if NETSTANDARD2_0
                            inputStream.Dispose();
#else
                            await inputStream.DisposeAsync().ConfigureAwait(false);
#endif
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to finalize input stream for file {Path.GetFileName(upload.Filename)} to {username}: {ex.Message}", ex);
                        }
                    }

                    if (!upload.State.HasFlag(TransferStates.Succeeded))
                    {
                        // if the upload failed, try to send a message to the user informing them.
                        try
                        {
                            // fetch the endpoint again, in case it failed or was never fetched because the semaphore wasn't obtained.
                            // this allows us to send UploadDenied for cancelled queued files
                            endpoint = await GetUserEndPointAsync(username).ConfigureAwait(false);
                            var messageConnection = await PeerConnectionManager
                                .GetOrAddMessageConnectionAsync(username, endpoint, CancellationToken.None)
                                .ConfigureAwait(false);

                            // send UploadDenied if we cancelled the transfer. this should prevent the remote client from re-enqueuing
                            if (upload.State.HasFlag(TransferStates.Cancelled))
                            {
                                await messageConnection.WriteAsync(new UploadDenied(remoteFilename, "Cancelled")).ConfigureAwait(false);
                            }
                            else
                            {
                                await messageConnection.WriteAsync(new UploadFailed(remoteFilename)).ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            // swallow any exceptions here. the user may be offline, we might fail to connect, we might fail to send
                            // the message. we don't *need* this to succeed, and there's a good chance that it won't if the user lost
                            // connectivity, causing the upload to fail in the first place
                        }
                    }
                }
                finally
                {
                    /*
                        make sure we do all of the absolutely-must-do cleanup; if any of this fails we will leave the
                        client in an inoperable state over time
                    */
                    if (semaphoreAcquired)
                    {
                        try
                        {
                            Diagnostic.Debug($"Upload semaphore for file {Path.GetFileName(upload.Filename)} to {username} released");
                            semaphore.Release(releaseCount: 1);
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to release upload semaphore for user {username}: {ex.Message}");
                        }
                    }

                    if (uploadSlotAcquired)
                    {
                        try
                        {
                            // give the next thread time to acquire the semaphore. this is extremely sub-optimal, but if there's a waiting
                            // upload we want the code within AcquireSlot() to be aware of it before we release the slot. 10ms should be
                            // plenty of time, as this release and the subsequent thread acquiring it should happen within nanoseconds.
                            await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);

                            Diagnostic.Debug($"Upload slot for file {Path.GetFileName(upload.Filename)} to {username} released");

                            options.SlotReleased?.Invoke(new Transfer(upload));
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Encountered Exception releasing upload slot for file {Path.GetFileName(upload.Filename)} to {username}: {ex.Message}", ex);
                        }
                    }

                    if (globalSemaphoreAcquired)
                    {
                        try
                        {
                            GlobalUploadSemaphore.Release(releaseCount: 1);
                            Diagnostic.Debug($"Global upload semaphore for file {Path.GetFileName(upload.Filename)} to {username} released");
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to release global upload semaphore for file {Path.GetFileName(upload.Filename)} to {username}: {ex.Message}");
                        }
                    }

                    UploadDictionary.TryRemove(upload.Token, out _);
                    UniqueKeyDictionary.TryRemove(uniqueKey, out _);
                }
            }
        }

        private async Task<UserData> WatchUserInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var addUserWait = Waiter.Wait<WatchUserResponse>(new WaitKey(MessageCode.Server.WatchUser, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteAsync(new WatchUserRequest(username), cancellationToken).ConfigureAwait(false);

                var response = await addUserWait.ConfigureAwait(false);

                if (!response.Exists)
                {
                    throw new UserNotFoundException($"User {username} does not exist");
                }

                return response.UserData;
            }
            catch (Exception ex) when (!(ex is UserNotFoundException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
            {
                throw new SoulseekClientException($"Failed to watch user {username}: {ex.Message}", ex);
            }
        }
    }
}