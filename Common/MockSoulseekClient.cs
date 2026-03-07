#if DEBUG
namespace Seeker
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;

    public class MockSoulseekClient : ISoulseekClient
    {
        // --- Configurable delay ---
        public int SimulatedDelayMs { get; set; } = 50;

        // --- Configurable method handlers ---
        public Func<string, string, CancellationToken?, Task>? ConnectAsyncHandler { get; set; }
        public Func<string, int, string, string, CancellationToken?, Task>? ConnectWithAddressAsyncHandler { get; set; }
        public Action<string?, Exception?>? DisconnectHandler { get; set; }
        public Func<string, BrowseOptions, CancellationToken?, Task<BrowseResponse>>? BrowseAsyncHandler { get; set; }
        public Func<string, string, int?, CancellationToken?, bool, Task<IReadOnlyCollection<Soulseek.Directory>>>? GetDirectoryContentsAsyncHandler { get; set; }
        public Func<SearchQuery, SearchScope, int?, SearchOptions, CancellationToken?, Task<(Soulseek.Search Search, IReadOnlyCollection<SearchResponse> Responses)>>? SearchAsyncHandler { get; set; }
        public Func<SearchQuery, Action<SearchResponse>, SearchScope, int?, SearchOptions, CancellationToken?, Task<Soulseek.Search>>? SearchWithHandlerAsyncHandler { get; set; }
        public Func<string, string, string, long?, long, int?, TransferOptions, CancellationToken?, Task<Transfer>>? DownloadToFileAsyncHandler { get; set; }
        public Func<string, string, Func<Task<System.IO.Stream>>, long?, long, int?, TransferOptions, CancellationToken?, Task<Transfer>>? DownloadToStreamAsyncHandler { get; set; }
        public Func<string, string, string, int?, TransferOptions, CancellationToken?, Task<Transfer>>? UploadFromFileAsyncHandler { get; set; }
        public Func<string, string, long, Func<long, Task<System.IO.Stream>>, int?, TransferOptions, CancellationToken?, Task<Transfer>>? UploadFromStreamAsyncHandler { get; set; }
        public Func<string, bool, CancellationToken?, Task<RoomData>>? JoinRoomAsyncHandler { get; set; }
        public Func<string, CancellationToken?, Task>? LeaveRoomAsyncHandler { get; set; }
        public Func<string, string, CancellationToken?, Task>? SendRoomMessageAsyncHandler { get; set; }
        public Func<string, string, CancellationToken?, Task>? SendPrivateMessageAsyncHandler { get; set; }
        public Func<UserPresence, CancellationToken?, Task>? SetStatusAsyncHandler { get; set; }
        public Func<CancellationToken?, Task<long>>? PingServerAsyncHandler { get; set; }
        public Func<int, int, CancellationToken?, Task>? SetSharedCountsAsyncHandler { get; set; }
        public Func<SoulseekClientOptionsPatch, CancellationToken?, Task<bool>>? ReconfigureOptionsAsyncHandler { get; set; }
        public Func<CancellationToken?, Task<RoomList>>? GetRoomListAsyncHandler { get; set; }
        public Func<string, CancellationToken?, Task<UserData>>? WatchUserAsyncHandler { get; set; }
        public Func<string, CancellationToken?, Task>? UnwatchUserAsyncHandler { get; set; }
        public Func<int, CancellationToken?, Task>? AcknowledgePrivateMessageAsyncHandler { get; set; }
        public Func<string, CancellationToken?, Task>? ChangePasswordAsyncHandler { get; set; }
        public Func<string, int, CancellationToken?, Task>? GrantUserPrivilegesAsyncHandler { get; set; }
        public Func<CancellationToken?, Task<int>>? GetPrivilegesAsyncHandler { get; set; }
        public Func<string, CancellationToken?, Task<UserStatistics>>? GetUserStatisticsAsyncHandler { get; set; }
        public Func<string, CancellationToken?, Task<UserInfo>>? GetUserInfoAsyncHandler { get; set; }
        public Func<int, CancellationToken?, Task>? SendUploadSpeedAsyncHandler { get; set; }
        public Func<string, string, CancellationToken?, Task>? SetRoomTickerAsyncHandler { get; set; }

        // --- Mutable properties ---
        public SoulseekClientStates State { get; set; } = SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn;
        public string Username { get; set; } = "mockUser";
        public string Address { get; set; } = "mock.server";
        public int? Port { get; set; } = 2242;
        public IPAddress? IPAddress { get; set; }
        public IPEndPoint? IPEndPoint { get; set; }
        public SoulseekClientOptions? Options { get; set; }
        public ServerInfo? ServerInfo { get; set; }
        public DistributedNetworkInfo? DistributedNetwork { get; set; }
        public IReadOnlyCollection<Transfer> Downloads { get; set; } = Array.Empty<Transfer>();
        public IReadOnlyCollection<Transfer> Uploads { get; set; } = Array.Empty<Transfer>();

        // Explicit interface implementations for read-only properties
        string ISoulseekClient.Address => Address;
        string ISoulseekClient.Username => Username;
        SoulseekClientStates ISoulseekClient.State => State;
        int? ISoulseekClient.Port => Port;
        IPAddress ISoulseekClient.IPAddress => IPAddress!;
        IPEndPoint ISoulseekClient.IPEndPoint => IPEndPoint!;
        SoulseekClientOptions ISoulseekClient.Options => Options!;
        ServerInfo ISoulseekClient.ServerInfo => ServerInfo!;
        DistributedNetworkInfo ISoulseekClient.DistributedNetwork => DistributedNetwork!;
        IReadOnlyCollection<Transfer> ISoulseekClient.Downloads => Downloads;
        IReadOnlyCollection<Transfer> ISoulseekClient.Uploads => Uploads;

        // --- Events (all interface events) ---
        public event EventHandler<BrowseProgressUpdatedEventArgs>? BrowseProgressUpdated;
        public event EventHandler? Connected;
        public event EventHandler? DemotedFromDistributedBranchRoot;
        public event EventHandler<SoulseekClientDisconnectedEventArgs>? Disconnected;
        public event EventHandler<DistributedChildEventArgs>? DistributedChildAdded;
        public event EventHandler<DistributedChildEventArgs>? DistributedChildDisconnected;
        public event EventHandler? DistributedNetworkReset;
        public event EventHandler<DistributedNetworkInfo>? DistributedNetworkStateChanged;
        public event EventHandler<DistributedParentEventArgs>? DistributedParentAdopted;
        public event EventHandler<DistributedParentEventArgs>? DistributedParentDisconnected;
        public event EventHandler<DownloadDeniedEventArgs>? DownloadDenied;
        public event EventHandler<DownloadFailedEventArgs>? DownloadFailed;
        public event EventHandler<IReadOnlyCollection<string>>? ExcludedSearchPhrasesReceived;
        public event EventHandler<string>? GlobalMessageReceived;
        public event EventHandler? LoggedIn;
        public event EventHandler<PrivateMessageReceivedEventArgs>? PrivateMessageReceived;
        public event EventHandler<string>? PrivateRoomMembershipAdded;
        public event EventHandler<string>? PrivateRoomMembershipRemoved;
        public event EventHandler<RoomInfo>? PrivateRoomModeratedUserListReceived;
        public event EventHandler<string>? PrivateRoomModerationAdded;
        public event EventHandler<string>? PrivateRoomModerationRemoved;
        public event EventHandler<RoomInfo>? PrivateRoomUserListReceived;
        public event EventHandler<IReadOnlyCollection<string>>? PrivilegedUserListReceived;
        public event EventHandler<PrivilegeNotificationReceivedEventArgs>? PrivilegeNotificationReceived;
        public event EventHandler? PromotedToDistributedBranchRoot;
        public event EventHandler<PublicChatMessageReceivedEventArgs>? PublicChatMessageReceived;
        public event EventHandler<RoomJoinedEventArgs>? RoomJoined;
        public event EventHandler<RoomLeftEventArgs>? RoomLeft;
        public event EventHandler<RoomList>? RoomListReceived;
        public event EventHandler<RoomMessageReceivedEventArgs>? RoomMessageReceived;
        public event EventHandler<RoomTickerAddedEventArgs>? RoomTickerAdded;
        public event EventHandler<OperatorAddedRemovedEventArgs>? OperatorInPrivateRoomAddedRemoved;
        public event EventHandler<RoomTickerListReceivedEventArgs>? RoomTickerListReceived;
        public event EventHandler<RoomTickerRemovedEventArgs>? RoomTickerRemoved;
        public event EventHandler<SearchRequestEventArgs>? SearchRequestReceived;
        public event EventHandler<SearchRequestResponseEventArgs>? SearchResponseDelivered;
        public event EventHandler<SearchRequestResponseEventArgs>? SearchResponseDeliveryFailed;
        public event EventHandler<SearchResponseReceivedEventArgs>? SearchResponseReceived;
        public event EventHandler<SearchStateChangedEventArgs>? SearchStateChanged;
        public event EventHandler<ServerInfo>? ServerInfoReceived;
        public event EventHandler<SoulseekClientStateChangedEventArgs>? StateChanged;
        public event EventHandler<TransferProgressUpdatedEventArgs>? TransferProgressUpdated;
        public event EventHandler<TransferStateChangedEventArgs>? TransferStateChanged;
        public event EventHandler<UserCannotConnectEventArgs>? UserCannotConnect;
        public event EventHandler<UserStatistics>? UserStatisticsChanged;
        public event EventHandler<UserStatus>? UserStatusChanged;
        public event EventHandler<DiagnosticEventArgs>? DiagnosticGenerated;

        // --- Event raise helpers (for events Seeker subscribes to) ---
        public void RaiseConnected() => Connected?.Invoke(this, EventArgs.Empty);
        public void RaiseDisconnected(SoulseekClientDisconnectedEventArgs args) => Disconnected?.Invoke(this, args);
        public void RaiseStateChanged(SoulseekClientStateChangedEventArgs args) => StateChanged?.Invoke(this, args);
        public void RaiseLoggedIn() => LoggedIn?.Invoke(this, EventArgs.Empty);
        public void RaiseServerInfoReceived(ServerInfo args) => ServerInfoReceived?.Invoke(this, args);
        public void RaiseTransferStateChanged(TransferStateChangedEventArgs args) => TransferStateChanged?.Invoke(this, args);
        public void RaiseTransferProgressUpdated(TransferProgressUpdatedEventArgs args) => TransferProgressUpdated?.Invoke(this, args);
        public void RaiseSearchResponseReceived(SearchResponseReceivedEventArgs args) => SearchResponseReceived?.Invoke(this, args);
        public void RaisePrivateMessageReceived(PrivateMessageReceivedEventArgs args) => PrivateMessageReceived?.Invoke(this, args);
        public void RaiseRoomMessageReceived(RoomMessageReceivedEventArgs args) => RoomMessageReceived?.Invoke(this, args);
        public void RaiseRoomJoined(RoomJoinedEventArgs args) => RoomJoined?.Invoke(this, args);
        public void RaiseRoomLeft(RoomLeftEventArgs args) => RoomLeft?.Invoke(this, args);
        public void RaiseUserStatusChanged(UserStatus args) => UserStatusChanged?.Invoke(this, args);
        public void RaiseUserStatisticsChanged(UserStatistics args) => UserStatisticsChanged?.Invoke(this, args);
        public void RaisePrivilegedUserListReceived(IReadOnlyCollection<string> args) => PrivilegedUserListReceived?.Invoke(this, args);
        public void RaiseExcludedSearchPhrasesReceived(IReadOnlyCollection<string> args) => ExcludedSearchPhrasesReceived?.Invoke(this, args);
        public void RaiseDiagnosticGenerated(DiagnosticEventArgs args) => DiagnosticGenerated?.Invoke(this, args);
        public void RaisePrivateRoomMembershipAdded(string roomName) => PrivateRoomMembershipAdded?.Invoke(this, roomName);
        public void RaisePrivateRoomMembershipRemoved(string roomName) => PrivateRoomMembershipRemoved?.Invoke(this, roomName);
        public void RaisePrivateRoomUserListReceived(RoomInfo args) => PrivateRoomUserListReceived?.Invoke(this, args);
        public void RaisePrivateRoomModeratedUserListReceived(RoomInfo args) => PrivateRoomModeratedUserListReceived?.Invoke(this, args);
        public void RaisePrivateRoomModerationAdded(string roomName) => PrivateRoomModerationAdded?.Invoke(this, roomName);
        public void RaisePrivateRoomModerationRemoved(string roomName) => PrivateRoomModerationRemoved?.Invoke(this, roomName);
        public void RaiseRoomTickerAdded(RoomTickerAddedEventArgs args) => RoomTickerAdded?.Invoke(this, args);
        public void RaiseRoomTickerRemoved(RoomTickerRemovedEventArgs args) => RoomTickerRemoved?.Invoke(this, args);
        public void RaiseRoomTickerListReceived(RoomTickerListReceivedEventArgs args) => RoomTickerListReceived?.Invoke(this, args);
        public void RaiseOperatorInPrivateRoomAddedRemoved(OperatorAddedRemovedEventArgs args) => OperatorInPrivateRoomAddedRemoved?.Invoke(this, args);

        // --- Private helper mirroring SoulseekClient's ChangeState ---
        private void ChangeState(SoulseekClientStates newState, string message = null, Exception exception = null)
        {
            var prev = State;
            State = newState;
            StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(prev, newState, message, exception));
            if (newState == SoulseekClientStates.Connected)
                Connected?.Invoke(this, EventArgs.Empty);
            if (newState == (SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn))
                LoggedIn?.Invoke(this, EventArgs.Empty);
            if (newState == SoulseekClientStates.Disconnected)
                Disconnected?.Invoke(this, new SoulseekClientDisconnectedEventArgs(message, exception));
        }

        // --- Method implementations (used by Seeker) ---

        public async Task ConnectAsync(string username, string password, CancellationToken? cancellationToken = null)
        {
            if (ConnectAsyncHandler != null) { await ConnectAsyncHandler(username, password, cancellationToken); return; }
            ChangeState(SoulseekClientStates.Connecting, "Connecting");
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);
            ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggingIn, "Logging in");
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);
            Username = username;
            Address = Address ?? "mock.server";
            ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, "Logged in");
            ServerInfoReceived?.Invoke(this, new ServerInfo(parentMinSpeed: 1, parentSpeedRatio: 1, wishlistInterval: 120));
        }

        public async Task ConnectAsync(string address, int port, string username, string password, CancellationToken? cancellationToken = null)
        {
            if (ConnectWithAddressAsyncHandler != null) { await ConnectWithAddressAsyncHandler(address, port, username, password, cancellationToken); return; }
            Address = address;
            Port = port;
            ChangeState(SoulseekClientStates.Connecting, "Connecting");
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);
            ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggingIn, "Logging in");
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);
            Username = username;
            ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, "Logged in");
            ServerInfoReceived?.Invoke(this, new ServerInfo(parentMinSpeed: 1, parentSpeedRatio: 1, wishlistInterval: 120));
        }

        public void Disconnect(string? message = null, Exception? exception = null)
        {
            if (DisconnectHandler != null) { DisconnectHandler(message, exception); return; }
            ChangeState(SoulseekClientStates.Disconnecting, message);
            Username = null;
            ChangeState(SoulseekClientStates.Disconnected, message, exception);
        }

        public async Task<BrowseResponse> BrowseAsync(string username, BrowseOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (BrowseAsyncHandler != null) return await BrowseAsyncHandler(username, options, cancellationToken);
            await Task.Delay(SimulatedDelayMs * 2).ConfigureAwait(false);
            return new BrowseResponse(Array.Empty<Soulseek.Directory>());
        }

        public Task<IReadOnlyCollection<Soulseek.Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null, bool isLegacy = false)
        {
            if (GetDirectoryContentsAsyncHandler != null) return GetDirectoryContentsAsyncHandler(username, directoryName, token, cancellationToken, isLegacy);
            return Task.FromResult<IReadOnlyCollection<Soulseek.Directory>>(Array.Empty<Soulseek.Directory>());
        }

        public async Task<(Soulseek.Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (SearchAsyncHandler != null) return await SearchAsyncHandler(query, scope, token, options, cancellationToken);
            var resolvedScope = scope ?? new SearchScope(SearchScopeType.Network);
            var resolvedToken = token ?? GetNextToken();

            var searchRequested = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Requested, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.None, searchRequested));
            await Task.Delay(SimulatedDelayMs / 2).ConfigureAwait(false);

            var searchInProgress = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.Requested, searchInProgress));
            await Task.Delay(SimulatedDelayMs * 2).ConfigureAwait(false);

            var searchCompleted = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Completed, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.InProgress, searchCompleted));
            return (searchCompleted, Array.Empty<SearchResponse>());
        }

        public async Task<Soulseek.Search> SearchAsync(SearchQuery query, Action<SearchResponse> responseHandler, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (SearchWithHandlerAsyncHandler != null) return await SearchWithHandlerAsyncHandler(query, responseHandler, scope, token, options, cancellationToken);
            var resolvedScope = scope ?? new SearchScope(SearchScopeType.Network);
            var resolvedToken = token ?? GetNextToken();

            var searchRequested = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Requested, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.None, searchRequested));
            await Task.Delay(SimulatedDelayMs / 2).ConfigureAwait(false);

            var searchInProgress = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.Requested, searchInProgress));
            await Task.Delay(SimulatedDelayMs * 2).ConfigureAwait(false);

            var searchCompleted = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Completed, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.InProgress, searchCompleted));
            return searchCompleted;
        }

        public async Task<Transfer> DownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (DownloadToFileAsyncHandler != null) return await DownloadToFileAsyncHandler(username, remoteFilename, localFilename, size, startOffset, token, options, cancellationToken);
            return await SimulateTransferAsync(TransferDirection.Download, username, remoteFilename, size ?? 1024, token ?? GetNextToken());
        }

        public async Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<System.IO.Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (DownloadToStreamAsyncHandler != null) return await DownloadToStreamAsyncHandler(username, remoteFilename, outputStreamFactory, size, startOffset, token, options, cancellationToken);
            return await SimulateTransferAsync(TransferDirection.Download, username, remoteFilename, size ?? 1024, token ?? GetNextToken());
        }

        public async Task<Transfer> UploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (UploadFromFileAsyncHandler != null) return await UploadFromFileAsyncHandler(username, remoteFilename, localFilename, token, options, cancellationToken);
            return await SimulateTransferAsync(TransferDirection.Upload, username, remoteFilename, 1024, token ?? GetNextToken());
        }

        public async Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<System.IO.Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (UploadFromStreamAsyncHandler != null) return await UploadFromStreamAsyncHandler(username, remoteFilename, size, inputStreamFactory, token, options, cancellationToken);
            return await SimulateTransferAsync(TransferDirection.Upload, username, remoteFilename, size, token ?? GetNextToken());
        }

        private async Task<Transfer> SimulateTransferAsync(TransferDirection direction, string username, string filename, long size, int token)
        {
            var queued = new Transfer(direction, username, filename, token, TransferStates.Queued | TransferStates.Locally, size, 0);
            TransferStateChanged?.Invoke(this, new TransferStateChangedEventArgs(TransferStates.None, queued));
            await Task.Delay(SimulatedDelayMs / 2).ConfigureAwait(false);

            var requested = new Transfer(direction, username, filename, token, TransferStates.Requested, size, 0);
            TransferStateChanged?.Invoke(this, new TransferStateChangedEventArgs(TransferStates.Queued | TransferStates.Locally, requested));
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);

            var inProgress = new Transfer(direction, username, filename, token, TransferStates.InProgress, size, 0, startTime: DateTime.UtcNow);
            TransferStateChanged?.Invoke(this, new TransferStateChangedEventArgs(TransferStates.Requested, inProgress));
            await Task.Delay(SimulatedDelayMs * 2).ConfigureAwait(false);

            var completed = new Transfer(direction, username, filename, token, TransferStates.Completed | TransferStates.Succeeded, size, 0, bytesTransferred: size, endTime: DateTime.UtcNow);
            TransferStateChanged?.Invoke(this, new TransferStateChangedEventArgs(TransferStates.InProgress, completed));
            return completed;
        }

        public async Task<RoomData> JoinRoomAsync(string roomName, bool isPrivate = false, CancellationToken? cancellationToken = null)
        {
            if (JoinRoomAsyncHandler != null) return await JoinRoomAsyncHandler(roomName, isPrivate, cancellationToken);
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);
            return new RoomData(roomName, Array.Empty<UserData>(), isPrivate);
        }

        public async Task LeaveRoomAsync(string roomName, CancellationToken? cancellationToken = null)
        {
            if (LeaveRoomAsyncHandler != null) { await LeaveRoomAsyncHandler(roomName, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 2).ConfigureAwait(false);
        }

        public async Task SendRoomMessageAsync(string roomName, string message, CancellationToken? cancellationToken = null)
        {
            if (SendRoomMessageAsyncHandler != null) { await SendRoomMessageAsyncHandler(roomName, message, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public async Task SendPrivateMessageAsync(string username, string message, CancellationToken? cancellationToken = null)
        {
            if (SendPrivateMessageAsyncHandler != null) { await SendPrivateMessageAsyncHandler(username, message, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public async Task SetStatusAsync(UserPresence status, CancellationToken? cancellationToken = null)
        {
            if (SetStatusAsyncHandler != null) { await SetStatusAsyncHandler(status, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public async Task<long> PingServerAsync(CancellationToken? cancellationToken = null)
        {
            if (PingServerAsyncHandler != null) return await PingServerAsyncHandler(cancellationToken);
            await Task.Delay(SimulatedDelayMs / 2).ConfigureAwait(false);
            return 20L;
        }

        public async Task SetSharedCountsAsync(int directories, int files, CancellationToken? cancellationToken = null)
        {
            if (SetSharedCountsAsyncHandler != null) { await SetSharedCountsAsyncHandler(directories, files, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public Task<bool> ReconfigureOptionsAsync(SoulseekClientOptionsPatch patch, CancellationToken? cancellationToken = null)
        {
            if (ReconfigureOptionsAsyncHandler != null) return ReconfigureOptionsAsyncHandler(patch, cancellationToken);
            return Task.FromResult(false);
        }

        public Task<RoomList> GetRoomListAsync(CancellationToken? cancellationToken = null)
        {
            if (GetRoomListAsyncHandler != null) return GetRoomListAsyncHandler(cancellationToken);
            return Task.FromResult<RoomList>(null!);
        }

        public async Task<UserData> WatchUserAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (WatchUserAsyncHandler != null) return await WatchUserAsyncHandler(username, cancellationToken);
            await Task.Delay(SimulatedDelayMs / 2).ConfigureAwait(false);
            return new UserData(username, UserPresence.Online, 0, 0, 0, 0, string.Empty);
        }

        public async Task UnwatchUserAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (UnwatchUserAsyncHandler != null) { await UnwatchUserAsyncHandler(username, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public async Task AcknowledgePrivateMessageAsync(int privateMessageId, CancellationToken? cancellationToken = null)
        {
            if (AcknowledgePrivateMessageAsyncHandler != null) { await AcknowledgePrivateMessageAsyncHandler(privateMessageId, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public async Task ChangePasswordAsync(string password, CancellationToken? cancellationToken = null)
        {
            if (ChangePasswordAsyncHandler != null) { await ChangePasswordAsyncHandler(password, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public async Task GrantUserPrivilegesAsync(string username, int days, CancellationToken? cancellationToken = null)
        {
            if (GrantUserPrivilegesAsyncHandler != null) { await GrantUserPrivilegesAsyncHandler(username, days, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public Task<int> GetPrivilegesAsync(CancellationToken? cancellationToken = null)
        {
            if (GetPrivilegesAsyncHandler != null) return GetPrivilegesAsyncHandler(cancellationToken);
            return Task.FromResult(0);
        }

        public Task<UserStatistics> GetUserStatisticsAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (GetUserStatisticsAsyncHandler != null) return GetUserStatisticsAsyncHandler(username, cancellationToken);
            return Task.FromResult<UserStatistics>(null!);
        }

        public Task<UserInfo> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (GetUserInfoAsyncHandler != null) return GetUserInfoAsyncHandler(username, cancellationToken);
            return Task.FromResult<UserInfo>(null!);
        }

        public async Task SendUploadSpeedAsync(int speed, CancellationToken? cancellationToken = null)
        {
            if (SendUploadSpeedAsyncHandler != null) { await SendUploadSpeedAsyncHandler(speed, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        public async Task SetRoomTickerAsync(string roomName, string message, CancellationToken? cancellationToken = null)
        {
            if (SetRoomTickerAsyncHandler != null) { await SetRoomTickerAsyncHandler(roomName, message, cancellationToken); return; }
            await Task.Delay(SimulatedDelayMs / 5).ConfigureAwait(false);
        }

        // --- Methods used by Seeker (utility) ---

        private int _nextToken = 1;
        public int GetNextToken() => _nextToken++;

        public bool IsTransferInDownloads(string username, string filename) => false;

        private readonly List<Delegate> _searchResponseReceivedHandlers = new List<Delegate>();

        public void ClearSearchResponseReceivedFromTarget(object target)
        {
            // No-op for mock; real client removes handlers from a specific target
        }

        public int GetInvocationListOfSearchResponseReceived()
        {
            var handler = SearchResponseReceived;
            return handler?.GetInvocationList().Length ?? 0;
        }

        public bool GetListeningState() => true;

        // --- Methods NOT used by Seeker (throw NotImplementedException) ---

        public Task AcknowledgePrivilegeNotificationAsync(int privilegeNotificationId, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task AddPrivateRoomMemberAsync(string roomName, string username, CancellationToken? cancellationToken = null)
            => Task.CompletedTask;

        public Task AddPrivateRoomModeratorAsync(string roomName, string username, CancellationToken? cancellationToken = null)
            => Task.CompletedTask;

        public Task ConnectToUserAsync(string username, bool invalidateCache = false, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task DropPrivateRoomMembershipAsync(string roomName, CancellationToken? cancellationToken = null)
            => Task.CompletedTask;

        public Task DropPrivateRoomOwnershipAsync(string roomName, CancellationToken? cancellationToken = null)
            => Task.CompletedTask;

        public Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, string localFilename, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task<Task<Transfer>> EnqueueDownloadAsync(string username, string remoteFilename, Func<Task<System.IO.Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task<Task<Transfer>> EnqueueUploadAsync(string username, string remoteFilename, long size, Func<long, Task<System.IO.Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null, bool wasFileLatin1Decoded = false, bool wasFolderLatin1Decoded = false)
            => throw new NotImplementedException();

        public Task<IPEndPoint> GetUserEndPointAsync(string username, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task<bool> GetUserPrivilegedAsync(string username, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task<UserStatus> GetUserStatusAsync(string username, CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task RemovePrivateRoomMemberAsync(string roomName, string username, CancellationToken? cancellationToken = null)
            => Task.CompletedTask;

        public Task RemovePrivateRoomModeratorAsync(string roomName, string username, CancellationToken? cancellationToken = null)
            => Task.CompletedTask;

        public Task StartPublicChatAsync(CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        public Task StopPublicChatAsync(CancellationToken? cancellationToken = null)
            => throw new NotImplementedException();

        // --- IDisposable ---
        public void Dispose() { }
    }
}
#endif
