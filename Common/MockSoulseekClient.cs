#if DEBUG
namespace Seeker
{
    using Soulseek;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class MockSoulseekClient : ISoulseekClient
    {
        // --- Configurable delay ---
        public int SimulatedDelayMs { get; set; } = 200;

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
        public SoulseekClientStates State { get; set; } = SoulseekClientStates.None;
        public string Username { get; set; } = "mockUser";
        public string Address { get; set; } = "mock.server";
        public int? Port { get; set; } = 2242;
        public IPAddress? IPAddress { get; set; }
        public IPEndPoint? IPEndPoint { get; set; }
        public SoulseekClientOptions? Options { get; set; }
        public ServerInfo? ServerInfo { get; set; }
        public DistributedNetworkInfo? DistributedNetwork { get; set; }
        public IReadOnlyCollection<Transfer> Downloads => DownloadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();
        public IReadOnlyCollection<Transfer> Uploads => UploadDictionary.Values.Select(t => new Transfer(t)).ToList().AsReadOnly();

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
            if (username.Contains("slow"))
            {
                await Task.Delay(4000).ConfigureAwait(false);
            }
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);
            if (username.Contains("reject"))
            {
                ChangeState(SoulseekClientStates.Disconnected, "Failed");
                throw new LoginRejectedException($"The server rejected login attempt:");
            }
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
            if (username.Contains("slow"))
            {
                await Task.Delay(4000).ConfigureAwait(false);
            }
            ChangeState(SoulseekClientStates.Connecting, "Connecting");
            await Task.Delay(SimulatedDelayMs).ConfigureAwait(false);
            if (username.Contains("reject"))
            {
                ChangeState(SoulseekClientStates.Disconnected, "Failed");
                throw new LoginRejectedException($"The server rejected login attempt:");
            }
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
            int millisecondDelay = 100;
            if (username.Contains("ultraslow")) {
                millisecondDelay = 20000;
            } else if (username.Contains("slow")) {
                millisecondDelay = 5000;
            } else if (username.Contains("mid")) {
                millisecondDelay = 500;
            }
            await Task.Delay(millisecondDelay).ConfigureAwait(false);
            Soulseek.File MakeFile(string name, long size = 5_000_000) =>
                new Soulseek.File(1, name, size, name[(name.LastIndexOf('.') + 1)..],
                    new[] { new FileAttribute(FileAttributeType.BitRate, 320), new FileAttribute(FileAttributeType.Length, 240) });

            BrowseResponse browseResponse;
            if (username.Contains("large"))
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music", new[] { MakeFile("cover.jpg", 200_000) }),
                    new("@@mockuser\\Music\\ArtistA\\Album1", new[] { MakeFile("01 Track One.mp3"), MakeFile("02 Track Two.mp3"), MakeFile("03 Track Three.mp3") }),
                    new("@@mockuser\\Music\\ArtistA\\Album2", new[] { MakeFile("01 Intro.flac", 30_000_000), MakeFile("02 Main.flac", 45_000_000) }),
                    new("@@mockuser\\Music\\ArtistA\\Album2\\CD2", new[] { MakeFile("01 Bonus.flac", 28_000_000) }),
                    new("@@mockuser\\Music\\ArtistB\\Best Of", new[] { MakeFile("01 Hit.mp3"), MakeFile("02 Single.mp3"), MakeFile("03 Classic.mp3"), MakeFile("04 Deep Cut.mp3") }),
                    new("@@mockuser\\Music\\ArtistC\\Live", new[] { MakeFile("01 Opening.mp3"), MakeFile("02 Encore.mp3") }),
                    new("@@mockuser\\Music\\Various\\Compilation", new[] { MakeFile("01 Song.mp3"), MakeFile("02 Song.mp3"), MakeFile("03 Song.mp3") }),
                    new("@@mockuser\\Documents\\Misc", new[] { MakeFile("readme.txt", 1_000) }),
                };
                var lockedDirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music\\ArtistA\\Album3 (Private)", new[] { MakeFile("01 Unreleased.mp3"), MakeFile("02 Demo.mp3") }),
                    new("@@mockuser\\Music\\ArtistD\\Rare", new[] { MakeFile("01 Rarity.flac", 40_000_000) }),
                };
                browseResponse = new BrowseResponse(dirs, lockedDirs);
            }
            else if (username.Contains("medium"))
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music\\ArtistA\\Album1", new[] { MakeFile("01 Track One.mp3"), MakeFile("02 Track Two.mp3"), MakeFile("03 Track Three.mp3") }),
                    new("@@mockuser\\Music\\ArtistB\\Album1", new[] { MakeFile("01 First.mp3"), MakeFile("02 Second.mp3") }),
                    new("@@mockuser\\Music\\ArtistC\\Singles", new[] { MakeFile("01 Single.mp3") }),
                };
                browseResponse = new BrowseResponse(dirs);
            }
            else if (username.Contains("small"))
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music\\Album", new[] { MakeFile("01 Only Track.mp3"), MakeFile("02 Another.mp3") }),
                };
                browseResponse = new BrowseResponse(dirs);
            }
            else
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music"),
                    new("@@mockuser\\Documents"),
                    new("@@mockuser\\Pictures"),
                };
                browseResponse = new BrowseResponse(dirs);
            }

            return browseResponse;
        }

        public Task<IReadOnlyCollection<Soulseek.Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null, bool isLegacy = false)
        {
            if (GetDirectoryContentsAsyncHandler != null) return GetDirectoryContentsAsyncHandler(username, directoryName, token, cancellationToken, isLegacy);
            return Task.FromResult<IReadOnlyCollection<Soulseek.Directory>>(Array.Empty<Soulseek.Directory>());
        }

        private static readonly Random _random = new Random();
        private static readonly string[] _mockUsernames = { "musiclover42", "vinyl_rips", "flac_hoarder", "mp3collector", "audiophile99", "shareking", "basshead", "djmix", "recorddigger", "soundwave" };
        private static readonly string[] _mockArtists = { "bach", "beethoven", "mozart" };
        private static readonly string[] _mockAlbums = { "Greatest Hits", "Live Sessions", "Remastered Edition", "Deluxe", "The Collection", "Anthology", "Unplugged", "B-Sides" };
        private static readonly string[] _extensions = { "mp3", "flac", "ogg", "wav", "m4a" };

        private static (int count, int totalTimeMs, string search) ParseMockSearchParams(SearchQuery query)
        {
            int count = 30;
            int totalTimeMs = 1000;
            string search = string.Empty;
            foreach (var term in query.Terms)
            {
                if (term.StartsWith("n:", StringComparison.OrdinalIgnoreCase) && int.TryParse(term.Substring(2), out int n))
                    count = Math.Max(1, n);
                else if (term.StartsWith("t:", StringComparison.OrdinalIgnoreCase) && int.TryParse(term.Substring(2), out int t))
                    totalTimeMs = Math.Max(0, t);
                else  
                    search += query + " ";
                
            }
            return (count, totalTimeMs, search);
        }

        private static SearchResponse GenerateMockSearchResponse(int token, string term = "")
        {
            var username = _mockUsernames[_random.Next(_mockUsernames.Length)];
            var artist = _mockArtists[_random.Next(_mockArtists.Length)];
            var album = _mockAlbums[_random.Next(_mockAlbums.Length)];
            var ext = _extensions[_random.Next(_extensions.Length)];
            var uploadSpeed = _random.Next(50_000, 10_000_000);
            var queueLength = _random.Next(0, 50);
            var hasFreeSlot = _random.Next(2) == 0;
            var isLocked = _random.Next(5) == 0; // ~20% chance locked

            int trackCount = _random.Next(1, 6);
            var files = new List<Soulseek.File>();
            int bitRate = ext == "flac" ? 1411 : new[] { 128, 192, 256, 320 }[_random.Next(4)];
            for (int i = 0; i < trackCount; i++)
            {
                int trackNum = i + 1;
                long size = _random.Next(2_000_000, 60_000_000);
                int length = _random.Next(120, 480);
                string filename = $"@@{username}\\Music\\{artist}\\{term} - AlbumName {album}\\{trackNum:D2} Track {trackNum}.{ext}";
                files.Add(new Soulseek.File(1, filename, size, ext,
                    new[] { new FileAttribute(FileAttributeType.BitRate, bitRate), new FileAttribute(FileAttributeType.Length, length) }));
            }

            if (isLocked)
            {
                return new SearchResponse(username, token, hasFreeSlot, uploadSpeed, queueLength,
                    Array.Empty<Soulseek.File>(), files);
            }
            return new SearchResponse(username, token, hasFreeSlot, uploadSpeed, queueLength, files);
        }

        public async Task<(Soulseek.Search Search, IReadOnlyCollection<SearchResponse> Responses)> SearchAsync(SearchQuery query, SearchScope scope = null, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (SearchAsyncHandler != null) return await SearchAsyncHandler(query, scope, token, options, cancellationToken);
            var resolvedScope = scope ?? new SearchScope(SearchScopeType.Network);
            var resolvedToken = token ?? GetNextToken();

            var (count, totalTimeMs, search) = ParseMockSearchParams(query);
            int delayPerResponse = count > 0 ? totalTimeMs / count : 0;

            var searchRequested = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Requested, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.None, searchRequested));

            var searchInProgress = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, 0, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.Requested, searchInProgress));

            var allResponses = new List<SearchResponse>();
            for (int i = 0; i < count; i++)
            {
                if (cancellationToken?.IsCancellationRequested == true)
                    break;

                var response = GenerateMockSearchResponse(resolvedToken, search);
                allResponses.Add(response);

                var currentSearch = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, i + 1, 0, 0);
                options?.ResponseReceived?.Invoke((currentSearch, response));

                if (delayPerResponse > 0)
                    await Task.Delay(delayPerResponse).ConfigureAwait(false);
            }

            var searchCompleted = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Completed, count, 0, 0);
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.InProgress, searchCompleted));
            return (searchCompleted, allResponses.AsReadOnly());
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
            return await DownloadInternalAsync(username, remoteFilename, size ?? 1024, startOffset, token ?? GetNextToken(), options, cancellationToken ?? CancellationToken.None);
        }

        public async Task<Transfer> DownloadAsync(string username, string remoteFilename, Func<Task<System.IO.Stream>> outputStreamFactory, long? size = null, long startOffset = 0, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (DownloadToStreamAsyncHandler != null) return await DownloadToStreamAsyncHandler(username, remoteFilename, outputStreamFactory, size, startOffset, token, options, cancellationToken);
            return await DownloadInternalAsync(username, remoteFilename, size ?? 1024, startOffset, token ?? GetNextToken(), options, cancellationToken ?? CancellationToken.None);
        }

        public async Task<Transfer> UploadAsync(string username, string remoteFilename, string localFilename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (UploadFromFileAsyncHandler != null) return await UploadFromFileAsyncHandler(username, remoteFilename, localFilename, token, options, cancellationToken);
            return await UploadInternalAsync(username, remoteFilename, 1024, 0, token ?? GetNextToken(), options, cancellationToken ?? CancellationToken.None);
        }

        public async Task<Transfer> UploadAsync(string username, string remoteFilename, long size, Func<long, Task<System.IO.Stream>> inputStreamFactory, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (UploadFromStreamAsyncHandler != null) return await UploadFromStreamAsyncHandler(username, remoteFilename, size, inputStreamFactory, token, options, cancellationToken);
            return await UploadInternalAsync(username, remoteFilename, size, 0, token ?? GetNextToken(), options, cancellationToken ?? CancellationToken.None);
        }

        SemaphoreSlim GlobalDownloadSemaphore = new SemaphoreSlim(initialCount: 3, maxCount: 3);
        SemaphoreSlim GlobalUploadSemaphore = new SemaphoreSlim(initialCount: 3, maxCount: 3);
        ConcurrentDictionary<int, TransferInternal> DownloadDictionary = new ConcurrentDictionary<int, TransferInternal>();
        ConcurrentDictionary<int, TransferInternal> UploadDictionary = new ConcurrentDictionary<int, TransferInternal>();
        ConcurrentDictionary<string, bool> UniqueKeyDictionary = new ConcurrentDictionary<string, bool>();

        private async Task<Transfer> DownloadInternalAsync(string username, string filename, long size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            options ??= new TransferOptions();

            var download = new TransferInternal(TransferDirection.Download, username, filename, token, options)
            {
                StartOffset = startOffset,
                Size = size,
            };

            var uniqueKey = $"{TransferDirection.Download}:{username}:{filename}";

            if (!UniqueKeyDictionary.TryAdd(key: uniqueKey, value: true))
            {
                throw new DuplicateTransferException($"Duplicate download of {filename} from {username} aborted");
            }

            if (!DownloadDictionary.TryAdd(token, download))
            {
                UniqueKeyDictionary.TryRemove(uniqueKey, out _);
                throw new DuplicateTransferException($"Duplicate download of {filename} from {username} aborted");
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

            bool globalSemaphoreAcquired = false;

            try
            {
                UpdateState(TransferStates.Queued | TransferStates.Locally);
                await Task.Delay(SimulatedDelayMs, cancellationToken).ConfigureAwait(false);

                await GlobalDownloadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                globalSemaphoreAcquired = true;

                UpdateState(TransferStates.Requested);
                await Task.Delay(SimulatedDelayMs, cancellationToken).ConfigureAwait(false);

                UpdateState(TransferStates.Queued | TransferStates.Remotely);
                await Task.Delay(SimulatedDelayMs, cancellationToken).ConfigureAwait(false);

                UpdateState(TransferStates.Initializing);
                await Task.Delay(SimulatedDelayMs, cancellationToken).ConfigureAwait(false);

                UpdateState(TransferStates.InProgress);
                UpdateProgress(startOffset);

                int steps = 10;
                long chunkSize = (size - startOffset) / steps;
                for (int i = 1; i <= steps; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    if (_random.Next(100) == 0)
                    {
                        throw new Exception("Simulated Exception");
                    }
                    UpdateProgress(startOffset + chunkSize * i);
                }

                UpdateProgress(size);
                UpdateState(TransferStates.Completed | TransferStates.Succeeded);

                return new Transfer(download);
            }
            catch (OperationCanceledException)
            {
                UpdateState(TransferStates.Completed | TransferStates.Cancelled);
                throw;
            }
            catch (Exception)
            {
                UpdateState(TransferStates.Completed | TransferStates.Errored);
                throw;
            }
            finally
            {
                if (globalSemaphoreAcquired)
                {
                    GlobalDownloadSemaphore.Release();
                }

                DownloadDictionary.TryRemove(token, out _);
                UniqueKeyDictionary.TryRemove(uniqueKey, out _);
            }
        }

        private async Task<Transfer> UploadInternalAsync(string username, string filename, long size, long startOffset, int token, TransferOptions options, CancellationToken cancellationToken)
        {
            options ??= new TransferOptions();

            var upload = new TransferInternal(TransferDirection.Upload, username, filename, token, options)
            {
                StartOffset = startOffset,
                Size = size,
            };

            var uniqueKey = $"{TransferDirection.Upload}:{username}:{filename}";

            if (!UniqueKeyDictionary.TryAdd(key: uniqueKey, value: true))
            {
                throw new DuplicateTransferException($"Duplicate upload of {filename} to {username} aborted");
            }

            if (!UploadDictionary.TryAdd(token, upload))
            {
                UniqueKeyDictionary.TryRemove(uniqueKey, out _);
                throw new DuplicateTransferException($"Duplicate upload of {filename} to {username} aborted");
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

            bool globalSemaphoreAcquired = false;

            try
            {
                UpdateState(TransferStates.Queued | TransferStates.Locally);
                await Task.Delay(SimulatedDelayMs * 2, cancellationToken).ConfigureAwait(false);

                await GlobalUploadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                globalSemaphoreAcquired = true;

                UpdateState(TransferStates.Requested);
                await Task.Delay(SimulatedDelayMs * 2, cancellationToken).ConfigureAwait(false);

                UpdateState(TransferStates.Initializing);
                await Task.Delay(SimulatedDelayMs * 2, cancellationToken).ConfigureAwait(false);

                UpdateState(TransferStates.InProgress);
                UpdateProgress(startOffset);

                int steps = 10;
                long chunkSize = (size - startOffset) / steps;
                for (int i = 1; i <= steps; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    if (_random.Next(100) == 0)
                    {
                        throw new Exception("Simulated Exception");
                    }
                    UpdateProgress(startOffset + chunkSize * i);
                }

                UpdateProgress(size);
                UpdateState(TransferStates.Completed | TransferStates.Succeeded);

                return new Transfer(upload);
            }
            catch (OperationCanceledException)
            {
                UpdateState(TransferStates.Completed | TransferStates.Cancelled);
                throw;
            }
            catch (Exception)
            {
                UpdateState(TransferStates.Completed | TransferStates.Errored);
                throw;
            }
            finally
            {
                if (globalSemaphoreAcquired)
                {
                    GlobalUploadSemaphore.Release();
                }

                UploadDictionary.TryRemove(token, out _);
                UniqueKeyDictionary.TryRemove(uniqueKey, out _);
            }
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

        public bool IsTransferInDownloads(string username, string filename)
            => Downloads.Any(d => d.Username == username && d.Filename == filename);

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
