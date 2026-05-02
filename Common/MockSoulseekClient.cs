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
        public MockSoulseekClient()
        {
            StartBackgroundTimers();
        }

        public int SimulatedDelayMs { get; set; } = 200;

        public int BrowseUploadIntervalSec { get; set; } = 180;
        public int PrivateMessageIntervalSec { get; set; } = 60;
        public int ExcludedPhrasesIntervalSec { get; set; } = 60;
        public int UserStatusIntervalSec { get; set; } = 10;

        private CancellationTokenSource? _backgroundTimersCts;

        private static readonly string[] _mockMessages =
        {
            "hello test",
            "test - how are you doing?",
            "what is in your collection? anything of note?",
            "how is everything going?",
            "test",
        };

        private static readonly string[] _mockExcludedPhrases =
        {
            "testArtistA", "testArtistB"
        };

        public void StartBackgroundTimers()
        {
            StopBackgroundTimers();
            _backgroundTimersCts = new CancellationTokenSource();
            var ct = _backgroundTimersCts.Token;

            if (PrivateMessageIntervalSec > 0)
            {
                _ = RunPrivateMessageLoop(ct);
            }
            if (ExcludedPhrasesIntervalSec > 0)
            {
                _ = RunExcludedPhrasesLoop(ct);
            }
            if (UserStatusIntervalSec > 0)
            {
                _ = RunUserStatusLoop(ct);
            }
            if (BrowseUploadIntervalSec > 0)
            {
                _ = RunBrowseUploadLoop(ct);
            }
        }

        public void StopBackgroundTimers()
        {
            _backgroundTimersCts?.Cancel();
            _backgroundTimersCts?.Dispose();
            _backgroundTimersCts = null;
        }

        private async Task RunPrivateMessageLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(PrivateMessageIntervalSec * 1000, ct);
                    var username = _mockUsernames[_random.Next(_mockUsernames.Length)];
                    var message = _mockMessages[_random.Next(_mockMessages.Length)];
                    var args = new PrivateMessageReceivedEventArgs(
                        _random.Next(1, 100000),
                        DateTime.UtcNow,
                        username,
                        message,
                        false);
                    RaisePrivateMessageReceived(args);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunBrowseUploadLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(10_000, ct);
                var username = _mockUsernames[_random.Next(_mockUsernames.Length)];
                var response = Options?.BrowseResponseResolver(username, IPEndPoint)?.Result;
                if (response != null)
                {
                    var nonEmptyDirectories = response.Directories.Where(dir => dir.FileCount != 0);
                    var index = _random.Next(0, nonEmptyDirectories.Count());
                    var directoryToDownload = nonEmptyDirectories.ElementAt(index);
                    foreach (var file in directoryToDownload.Files)
                    {
                        Options?.EnqueueDownload(username, IPEndPoint, directoryToDownload.Name + @"\" + file.Filename);
                    }
                }
                await Task.Delay(BrowseUploadIntervalSec * 1000, ct);
            }
        }

        private async Task RunExcludedPhrasesLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(ExcludedPhrasesIntervalSec * 1000, ct);
                    int count = _random.Next(1, _mockExcludedPhrases.Length + 1);
                    var phrases = _mockExcludedPhrases
                        .OrderBy(_ => _random.Next())
                        .Take(count)
                        .ToList()
                        .AsReadOnly();
                    RaiseExcludedSearchPhrasesReceived(phrases);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunUserStatusLoop(CancellationToken ct)
        {
            var presenceValues = (UserPresence[])Enum.GetValues(typeof(UserPresence));
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(UserStatusIntervalSec * 1000, ct);
                    var combined = new List<string>();
                    foreach (var item in Common.CommonState.UserList)
                    {
                        combined.Add(item.Username);
                    }
                    foreach (var item in Common.CommonState.IgnoreUserList)
                    {
                        combined.Add(item.Username);
                    }
                    if (combined.Count == 0)
                    {
                        continue;
                    }
                    var targetUser = combined[_random.Next(combined.Count)];
                    var newPresence = presenceValues[_random.Next(presenceValues.Length)];
                    var status = new UserStatus(targetUser, newPresence, false);
                    RaiseUserStatusChanged(status);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

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
        public void RaiseRoomListReceived(RoomList args) => RoomListReceived?.Invoke(this, args);
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
            if (username == "large2")
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music", new[] { MakeFile("cover.jpg", 200_000) }),
                    new("@@mockuser\\Music\\ArtistA", new[] {MakeFile("looseArtist1Track.mp3", 200_000)}),
                    new("@@mockuser\\Music\\ArtistA\\Album1", new[] { MakeFile("01 Track One.mp3"), MakeFile("02 Track Two.mp3"), MakeFile("03 Track Three failed.mp3") }),
                    new("@@mockuser\\Music\\ArtistA\\Album2", new[] { MakeFile("01 Intro.flac", 30_000_000), MakeFile("02 Main.flac", 45_000_000) }),
                    new("@@mockuser\\Music\\ArtistA\\Album2\\CD2", new[] { MakeFile("01 Bonus.flac", 28_000_000) }),
                    new("@@mockuser\\Music\\ArtistB"),
                    new("@@mockuser\\Music\\ArtistB\\Best Of", new[] { MakeFile("01 Hit.mp3"), MakeFile("02 Single.mp3"), MakeFile("03 Classic.mp3"), MakeFile("04 Deep Cut.mp3") }),
                    new("@@mockuser\\Music\\ArtistC"),
                    new("@@mockuser\\Music\\ArtistC\\Live", new[] { MakeFile("01 Opening.mp3"), MakeFile("02 Encore.mp3") }),
                    new("@@mockuser\\Music\\Various"),
                    new("@@mockuser\\Music\\Various\\Compilation", new[] { MakeFile("01 Song.mp3"), MakeFile("02 Song.mp3"), MakeFile("03 Song.mp3") }),
                    new("@@mockuser\\Documents"),
                    new("@@mockuser\\Documents\\Misc"),
                    new("@@mockuser\\Documents\\Misc\\Test"),
                    new("@@mockuser\\Documents\\Misc\\Test\\Test2"),
                    new("@@mockuser\\Documents\\Misc\\Test\\Test2\\HelloWorld", new[] { MakeFile("readme.txt", 1_000) }),
                };
                var lockedDirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music\\ArtistA\\Album3 (Private)", new[] { MakeFile("01 Unreleased.mp3"), MakeFile("02 Demo failed.mp3") }),
                    new("@@mockuser\\Music\\ArtistD\\Rare", new[] { MakeFile("01 Rarity.flac", 40_000_000) }),
                };
                browseResponse = new BrowseResponse(dirs, lockedDirs);

            }
            else if (username.Contains("large"))
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music", new[] { MakeFile("cover.jpg", 200_000) }),
                    new("@@mockuser\\Music\\ArtistA\\Album1", new[] { MakeFile("01 Track One.mp3"), MakeFile("02 Track Two.mp3"), MakeFile("03 Track Three failed.mp3") }),
                    new("@@mockuser\\Music\\ArtistA\\Album2", new[] { MakeFile("01 Intro.flac", 30_000_000), MakeFile("02 Main.flac", 45_000_000) }),
                    new("@@mockuser\\Music\\ArtistA\\Album2\\CD2", new[] { MakeFile("01 Bonus.flac", 28_000_000) }),
                    new("@@mockuser\\Music\\ArtistB\\Best Of", new[] { MakeFile("01 Hit.mp3"), MakeFile("02 Single.mp3"), MakeFile("03 Classic.mp3"), MakeFile("04 Deep Cut.mp3") }),
                    new("@@mockuser\\Music\\ArtistC\\Live", new[] { MakeFile("01 Opening.mp3"), MakeFile("02 Encore.mp3") }),
                    new("@@mockuser\\Music\\Various\\Compilation", new[] { MakeFile("01 Song.mp3"), MakeFile("02 Song.mp3"), MakeFile("03 Song.mp3") }),
                    new("@@mockuser\\Documents\\Misc", new[] { MakeFile("readme.txt", 1_000) }),
                };
                var lockedDirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music\\ArtistA\\Album3 (Private)", new[] { MakeFile("01 Unreleased.mp3"), MakeFile("02 Demo failed.mp3") }),
                    new("@@mockuser\\Music\\ArtistD\\Rare", new[] { MakeFile("01 Rarity.flac", 40_000_000) }),
                };
                browseResponse = new BrowseResponse(dirs, lockedDirs);
            }
            else if (username.Contains("medium"))
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music\\ArtistA\\Album1", new[] { MakeFile("01 Track One.mp3"), MakeFile("02 Track Two.mp3"), MakeFile("03 Track Three.mp3") }),
                    new("@@mockuser\\Music\\ArtistB\\Album1", new[] { MakeFile("01 First.mp3"), MakeFile("02 Second failed.mp3") }),
                    new("@@mockuser\\Music\\ArtistC\\Singles", new[] { MakeFile("01 Single.mp3") }),
                };
                browseResponse = new BrowseResponse(dirs);
            }
            else if (username.Contains("small"))
            {
                var dirs = new List<Soulseek.Directory>
                {
                    new("@@mockuser\\Music\\Album", new[] { MakeFile("01 Only Track.mp3"), MakeFile("02 Another failed.mp3") }),
                };
                browseResponse = new BrowseResponse(dirs);
            }
            else if (username == "beethoven_fan")
            {
                browseResponse = GenerateBeethovenFanBrowseResponse();
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

        public async Task<IReadOnlyCollection<Soulseek.Directory>> GetDirectoryContentsAsync(string username, string directoryName, int? token = null, CancellationToken? cancellationToken = null, bool isLegacy = false)
        {
            if (GetDirectoryContentsAsyncHandler != null)
            {
                return await GetDirectoryContentsAsyncHandler(username, directoryName, token, cancellationToken, isLegacy);
            }

            await Task.Delay(_random.Next(800, 3000), cancellationToken ?? CancellationToken.None);

            var directories = new List<Soulseek.Directory>();
            int dirCount = _random.Next(100) < 80 ? 1 : 2;

            for (int d = 0; d < dirCount; d++)
            {
                string dirName = d == 0 ? directoryName : directoryName + "\\2CD" + (d + 1);
                int fileCount = _random.Next(1, 36);
                var ext = _extensions[_random.Next(_extensions.Length)];
                int bitRate = ext == "flac" ? 1411 : new[] { 128, 192, 256, 320 }[_random.Next(4)];
                var files = new List<Soulseek.File>();

                for (int i = 0; i < fileCount; i++)
                {
                    int trackNum = i + 1;
                    long size = _random.Next(2_000_000, 60_000_000);
                    int length = _random.Next(120, 480);
                    string filename = $"{trackNum:D2} Track {trackNum}.{ext}";
                    var fileAttributes = new[] { new FileAttribute(FileAttributeType.BitRate, bitRate), new FileAttribute(FileAttributeType.Length, length) };
                    files.Add(new Soulseek.File(1, filename, size, ext, fileAttributes));
                }

                directories.Add(new Soulseek.Directory(dirName, files));
            }

            return directories.AsReadOnly();
        }

        private static readonly Random _random = new Random();
        private static readonly string[] _mockUsernames = { "musiclover42", "vinyl_rips", "flac_hoarder", "mp3collector", "audiophile99", "shareking", "basshead", "djmix", "recorddigger", "soundwave" };

        private static readonly string[] _mockAdjectives =
        {
            "happy", "lazy", "brave", "silly", "swift", "quiet", "loud",
            "mighty", "tiny", "gentle", "fierce", "wise", "lucky", "cool", "wild"
        };

        private static readonly string[] _mockNouns =
        {
            "tiger", "falcon", "river", "mountain", "forest", "panda", "dragon",
            "wolf", "eagle", "otter", "dolphin", "fox", "lion", "bear", "owl"
        };

        private static readonly string[] _mockRoomBaseNames =
        {
            "records", "indie", "test", "flac", "mp3", "programming",
            "math", "music", "books", "jazz", "electronic", "rock",
            "ambient", "metal", "classical", "hiphop"
        };

        private static string GenerateMockUsername() =>
            _mockAdjectives[_random.Next(_mockAdjectives.Length)]
            + _mockNouns[_random.Next(_mockNouns.Length)]
            + _random.Next(1, 1000);

        private static IEnumerable<RoomInfo> GenerateMockRooms(int count, int stableCount, string suffix)
        {
            for (int i = 0; i < count; i++)
            {
                string baseName = i < stableCount
                    ? _mockRoomBaseNames[i % _mockRoomBaseNames.Length]
                    : _mockRoomBaseNames[_random.Next(_mockRoomBaseNames.Length)]
                      + "_" + _random.Next(1000, 10000);
                yield return new RoomInfo(baseName + suffix, _random.Next(1, 1301));
            }
        }

        private static readonly string[] _mockArtists = { "bach", "beethoven", "mozart" };
        private static readonly string[] _mockAlbums = { "Greatest Hits", "Live Sessions", "Remastered Edition", "Deluxe", "The Collection", "Anthology", "Unplugged", "B-Sides" };
        private static readonly string[] _extensions = { "mp3", "flac", "ogg", "wav", "m4a" };
        private static readonly string[] _lossy = { "mp3", "ogg", "m4a" };
        private static readonly string[] _lossless = { "flac", "wav" };

        private static (int count, int totalTimeMs, string search) ParseMockSearchParams(SearchQuery query)
        {
            int count = 30;
            int totalTimeMs = 1000;
            string search = string.Empty;
            foreach (var term in query.Terms)
            {
                if (term.StartsWith("n:", StringComparison.OrdinalIgnoreCase) && int.TryParse(term.Substring(2), out int n))
                    count = Math.Max(0, n);
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
            int minUploadSpeed = (int)(100_000 * Math.Pow(10, _random.Next(2)));
            var uploadSpeed = _random.Next(minUploadSpeed, minUploadSpeed * 10);
            var queueLength = _random.Next(0, 50);
            var hasFreeSlot = _random.Next(2) == 0;
            var isLocked = _random.Next(5) == 0; // ~20% chance locked

            int trackCount = _random.Next(1, 30);
            var files = new List<Soulseek.File>();
            int bitRate = ext == "flac" ? 1411 : new[] { 128, 192, 256, 320 }[_random.Next(4)];
            for (int i = 0; i < trackCount; i++)
            {
                int trackNum = i + 1;
                long size = _random.Next(2_000_000, 60_000_000);
                int length = _random.Next(120, 480);
                string filename = $"@@{username}\\Music\\{artist}\\{term} - AlbumName {album}\\{trackNum:D2} Track {trackNum}.{ext}";
                var fileAttributes = new[] { new FileAttribute(FileAttributeType.BitRate, bitRate), new FileAttribute(FileAttributeType.Length, length) };
                if (ext == "flac" && _random.Next(2) == 0)
                {
                    fileAttributes = fileAttributes.Concat(new[] { new FileAttribute(FileAttributeType.SampleRate, 44100), new FileAttribute(FileAttributeType.BitDepth, 16) }).ToArray();
                }
                else if (_lossy.Contains(ext))
                {
                    switch(_random.Next(3))
                    {
                        case 0:
                            fileAttributes = fileAttributes.Concat(new[] { new FileAttribute(FileAttributeType.VariableBitRate, 0) }).ToArray();
                            break;
                        case 1:
                            fileAttributes = fileAttributes.Concat(new[] { new FileAttribute(FileAttributeType.VariableBitRate, 1) }).ToArray();
                            break;
                        default:
                            break;
                    }
                }
                files.Add(new Soulseek.File(1, filename, size, ext, fileAttributes));
            }

            if (isLocked)
            {
                return new SearchResponse(username, token, hasFreeSlot, uploadSpeed, queueLength,
                    Array.Empty<Soulseek.File>(), files);
            }
            return new SearchResponse(username, token, hasFreeSlot, uploadSpeed, queueLength, files);
        }

        private static Soulseek.File MakeSearchFile(string username, string folder, string filename, string ext, long sizeBytes, int lengthSeconds, int bitRate, bool isVbr = false, int sampleRate = 0, int bitDepth = 0)
        {
            string fullPath = $"@@{username}\\Music\\{folder}\\{filename}";
            var attrs = new List<FileAttribute>
            {
                new FileAttribute(FileAttributeType.Length, lengthSeconds),
            };
            if (bitRate != 0)
            {
                attrs.Add(new FileAttribute(FileAttributeType.BitRate, bitRate));
            }
            if (isVbr)
            {
                attrs.Add(new FileAttribute(FileAttributeType.VariableBitRate, 1));
            }
            if (sampleRate > 0)
            {
                attrs.Add(new FileAttribute(FileAttributeType.SampleRate, sampleRate));
            }
            if (bitDepth > 0)
            {
                attrs.Add(new FileAttribute(FileAttributeType.BitDepth, bitDepth));
            }
            return new Soulseek.File(1, fullPath, sizeBytes, ext, attrs);
        }

        private static List<SearchResponse> GenerateBeethovenOvertureResponses(int token)
        {
            var responses = new List<SearchResponse>();

            Soulseek.File Flac(string user, string folder, string name, long size, int length) =>
                MakeSearchFile(user, folder, name, "flac", size, length, 1411, false, 44100, 16);

            Soulseek.File Mp3(string user, string folder, string name, long size, int length, int bitRate) =>
                MakeSearchFile(user, folder, name, "mp3", size, length, bitRate);

            Soulseek.File Mp3Vbr(string user, string folder, string name, long size, int length, int bitRate) =>
                MakeSearchFile(user, folder, name, "mp3", size, length, bitRate, true);

            Soulseek.File Other(string user, string folder, string name, string ext, long size) =>
                MakeSearchFile(user, folder, name, ext, size, 0, 0);

            responses.Add(new SearchResponse("vinyl_rips", token, true, 8_132_000, 0, new[]
            {
                Other("vinyl_rips", "Beethoven, Ludwig van", "Beethoven Complete Works (1770-1827).tar.gz", "tar.gz", 4_200_000_000),
                Other("vinyl_rips", "Beethoven, Ludwig van", "README.txt", "txt", 2048),
            }));

            {
                string u = "mp3collector";
                string f = "Furtwangler Conducts Beethoven";
                responses.Add(new SearchResponse(u, token, true, 7_488_000, 3, new[]
                {
                    Mp3Vbr(u, f, "01 - Overture 'Leonore' No.3, Op.72b.mp3", 11_200_000, 420, 245),
                    Mp3Vbr(u, f, "02 - Overture 'Coriolan', Op.62.mp3", 8_100_000, 311, 210),
                    Mp3Vbr(u, f, "03 - Overture 'Egmont', Op.84.mp3", 9_500_000, 356, 235),
                    Mp3Vbr(u, f, "04 - Symphony No.5 in C minor, Op.67 - 1. Allegro con brio.mp3", 12_800_000, 482, 260),
                    Mp3Vbr(u, f, "05 - Symphony No.5 in C minor, Op.67 - 2. Andante con moto.mp3", 14_200_000, 548, 255),
                    Mp3Vbr(u, f, "06 - Symphony No.5 in C minor, Op.67 - 3. Allegro.mp3", 8_900_000, 338, 250),
                    Mp3Vbr(u, f, "07 - Symphony No.5 in C minor, Op.67 - 4. Allegro.mp3", 16_100_000, 610, 265),
                    Mp3Vbr(u, f, "08 - Symphony No.7 in A major, Op.92 - 2. Allegretto.mp3", 11_600_000, 440, 248),
                }));
            }

            {
                string u = "flac_hoarder";
                string f = "Karajan - Beethoven Symphony No. 9 and Overture 'Coriolan'";
                responses.Add(new SearchResponse(u, token, true, 4_736_000, 2, new[]
                {
                    Flac(u, f, "01 - Overture 'Coriolan', Op.62.flac", 44_650_000, 551),
                    Flac(u, f, "02 - Symphony No.9 in D minor, Op.125 - 'Choral' - 1. Allegro ma non troppo, un poco maestoso.flac", 83_900_000, 1005),
                    Flac(u, f, "03 - Symphony No.9 in D minor, Op.125 - 'Choral' - 2. Molto vivace.flac", 55_370_000, 660),
                    Flac(u, f, "04 - Symphony No.9 in D minor, Op.125 - 'Choral' - 3. Adagio molto e cantabile.flac", 76_560_000, 1005),
                    Flac(u, f, "05 - Symphony No.9 in D minor, Op.125 - 'Choral' - 4. Presto.flac", 32_140_000, 382),
                    Flac(u, f, "06 - Symphony No.9 in D minor, Op.125 - 'Choral' - 4. Presto - 'O Freunde, nicht diese Tone!' - Allegro assai.flac", 97_780_000, 1175),
                    Other(u, f, "Info.txt", "txt", 512),
                    Other(u, f, "Ludwig van Beethoven - Symphonie No. 9 and Overture 'Coriolan', Op.62.cue", "cue", 1024),
                    Other(u, f, "Ludwig van Beethoven - Symphonie No. 9 and Overture 'Coriolan', Op.62.log", "log", 3072),
                    Other(u, f, "Ludwig van Beethoven - Symphonie No. 9 and Overture 'Coriolan', Op.62.m3u", "m3u", 512),
                    Other(u, f, "folder.jpg", "jpg", 45_000),
                }));
            }

            {
                string u = "basshead";
                string f = "Trans-Siberian Orchestra - Beethoven's Last Night (2000)";
                responses.Add(new SearchResponse(u, token, false, 3_792_000, 12, new[]
                {
                    Mp3(u, f, "01 - An Angel's Share.mp3", 5_400_000, 225, 192),
                    Mp3(u, f, "02 - Overture.mp3", 3_600_000, 150, 192),
                    Mp3(u, f, "03 - Who Is This Child.mp3", 4_800_000, 200, 192),
                    Mp3(u, f, "04 - Fate.mp3", 6_200_000, 258, 192),
                    Mp3(u, f, "05 - What Is Eternal.mp3", 3_900_000, 163, 192),
                    Mp3(u, f, "06 - Mephistopheles.mp3", 7_400_000, 308, 192),
                    Mp3(u, f, "07 - The Dreams of Candlelight.mp3", 5_100_000, 213, 192),
                    Mp3(u, f, "08 - Beethoven.mp3", 6_800_000, 283, 192),
                    Mp3(u, f, "09 - A Last Illusion.mp3", 5_500_000, 229, 192),
                    Mp3(u, f, "10 - Beethoven's Last Night.mp3", 9_200_000, 383, 192),
                }));
            }

            {
                string u = "audiophile99";
                string f = "2009 - Classical Best Of - 884385716677 - 418645";
                responses.Add(new SearchResponse(u, token, true, 3_784_000, 0, new[]
                {
                    Flac(u, f, "01 - Beethoven - Overture 'Egmont', Op.84.flac", 38_200_000, 458),
                    Flac(u, f, "02 - Mozart - Overture to 'The Marriage of Figaro'.flac", 22_100_000, 264),
                    Flac(u, f, "03 - Beethoven - Overture 'Coriolan', Op.62.flac", 34_900_000, 418),
                    Flac(u, f, "04 - Dvorak - Carnival Overture, Op.92.flac", 42_500_000, 508),
                    Flac(u, f, "05 - Beethoven - Overture 'Leonore' No.3, Op.72b.flac", 56_100_000, 672),
                    Flac(u, f, "06 - Brahms - Academic Festival Overture, Op.80.flac", 44_800_000, 536),
                    Flac(u, f, "07 - Tchaikovsky - 1812 Overture, Op.49.flac", 61_400_000, 735),
                    Flac(u, f, "08 - Beethoven - Overture 'The Creatures of Prometheus', Op.43.flac", 28_600_000, 342),
                }));
            }

            {
                string u = "shareking";

                string f6 = "Beethoven Complete Edition - CD 011 - Overtures";
                responses.Add(new SearchResponse(u, token, true, 3_198_000, 5, new[]
                {
                    Flac(u, f6, "01 - Overture 'Leonore' No.1, Op.138.flac", 42_300_000, 507),
                    Flac(u, f6, "02 - Overture 'Leonore' No.2, Op.72a.flac", 57_800_000, 693),
                    Flac(u, f6, "03 - Overture 'Leonore' No.3, Op.72b.flac", 58_200_000, 698),
                    Flac(u, f6, "04 - Overture 'Fidelio', Op.72c.flac", 30_400_000, 364),
                    Flac(u, f6, "05 - Overture 'Coriolan', Op.62.flac", 36_700_000, 440),
                    Flac(u, f6, "06 - Overture 'Egmont', Op.84.flac", 38_100_000, 457),
                    Flac(u, f6, "07 - Overture 'King Stephen', Op.117.flac", 30_200_000, 362),
                    Flac(u, f6, "08 - Overture 'The Ruins of Athens', Op.113.flac", 25_800_000, 309),
                    Flac(u, f6, "09 - Overture 'The Creatures of Prometheus', Op.43.flac", 27_100_000, 325),
                    Flac(u, f6, "10 - Overture 'Namensfeier', Op.115.flac", 28_400_000, 340),
                    Flac(u, f6, "11 - Overture 'The Consecration of the House', Op.124.flac", 46_900_000, 562),
                }));

                string f7 = "Beethoven Complete Edition - CD 012 - Orchestral Works, Organ Works";
                responses.Add(new SearchResponse(u, token, true, 3_198_000, 5, new[]
                {
                    Flac(u, f7, "01 - Wellington's Victory, Op.91 - Part 1.flac", 38_700_000, 464),
                    Flac(u, f7, "02 - Wellington's Victory, Op.91 - Part 2.flac", 32_100_000, 385),
                    Flac(u, f7, "03 - March in D major, WoO 24.flac", 14_200_000, 170),
                    Flac(u, f7, "04 - Polonaise in D major, WoO 21.flac", 16_800_000, 201),
                    Flac(u, f7, "05 - Ecossaise in D major, WoO 22.flac", 5_300_000, 63),
                    Flac(u, f7, "06 - Overture and Incidental Music to 'Egmont' - Overture.flac", 39_400_000, 472),
                }));

                string f8 = "Beethoven Complete Edition - CD 061 - Leonore Part I (Original Version of Fidelio, 1805)";
                responses.Add(new SearchResponse(u, token, true, 3_198_000, 5, new[]
                {
                    Flac(u, f8, "01 - Leonore - Overture.flac", 52_600_000, 631),
                    Flac(u, f8, "02 - Leonore - Act 1, No.1 Duet 'Jetzt, Schatzchen, jetzt sind wir allein'.flac", 34_200_000, 410),
                    Flac(u, f8, "03 - Leonore - Act 1, No.2 Aria 'O war ich schon mit dir vereint'.flac", 24_800_000, 297),
                    Flac(u, f8, "04 - Leonore - Act 1, No.3 Quartet 'Ein Mann ist bald genommen'.flac", 41_300_000, 495),
                    Flac(u, f8, "05 - Leonore - Act 1, No.4 Aria 'Hat man nicht auch Gold beineben'.flac", 22_100_000, 265),
                }));

                string f9 = "Beethoven Complete Edition - CD 067 - Die Ruinen von Athen, Konig Stephan";
                responses.Add(new SearchResponse(u, token, true, 3_198_000, 5, new[]
                {
                    Flac(u, f9, "01 - Die Ruinen von Athen, Op.113 - Overture.flac", 24_500_000, 294),
                    Flac(u, f9, "02 - Die Ruinen von Athen, Op.113 - Chorus 'Tochter des machtigen Zeus'.flac", 18_200_000, 218),
                    Flac(u, f9, "03 - Die Ruinen von Athen, Op.113 - Duet 'Ohne Verschulden'.flac", 21_600_000, 259),
                    Flac(u, f9, "04 - Die Ruinen von Athen, Op.113 - Turkish March.flac", 15_300_000, 183),
                    Flac(u, f9, "05 - Konig Stephan, Op.117 - Overture.flac", 30_400_000, 364),
                    Flac(u, f9, "06 - Konig Stephan, Op.117 - Chorus 'Ruhend von seinen Thaten'.flac", 19_700_000, 236),
                }));

                string f10 = "Beethoven Complete Edition - CD 087 - Symphony No. 3, Leonore Overtures - Otto Klemperer";
                responses.Add(new SearchResponse(u, token, true, 3_198_000, 5, new[]
                {
                    Flac(u, f10, "01 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 1. Allegro con brio.flac", 72_400_000, 868),
                    Flac(u, f10, "02 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 2. Marcia funebre.flac", 65_200_000, 782),
                    Flac(u, f10, "03 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 3. Scherzo.flac", 28_900_000, 346),
                    Flac(u, f10, "04 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 4. Finale.flac", 48_200_000, 578),
                    Flac(u, f10, "05 - Overture 'Leonore' No.1, Op.138.flac", 41_800_000, 501),
                    Flac(u, f10, "06 - Overture 'Leonore' No.2, Op.72a.flac", 56_400_000, 676),
                }));
            }

            {
                string u = "recorddigger";

                string f11 = "Beethoven - Symphonies and Overtures (Gardiner) - Disc 1";
                responses.Add(new SearchResponse(u, token, false, 2_909_000, 8, new[]
                {
                    Flac(u, f11, "01 - Overture 'Coriolan', Op.62.flac", 35_200_000, 422),
                    Flac(u, f11, "02 - Symphony No.5 in C minor, Op.67 - 1. Allegro con brio.flac", 55_400_000, 664),
                    Flac(u, f11, "03 - Symphony No.5 in C minor, Op.67 - 2. Andante con moto.flac", 68_100_000, 817),
                    Flac(u, f11, "04 - Symphony No.5 in C minor, Op.67 - 3. Allegro.flac", 39_500_000, 474),
                    Flac(u, f11, "05 - Symphony No.5 in C minor, Op.67 - 4. Allegro.flac", 58_700_000, 704),
                }));

                string f12 = "Beethoven - Symphonies and Overtures (Gardiner) - Disc 2";
                responses.Add(new SearchResponse(u, token, false, 2_909_000, 8, new[]
                {
                    Flac(u, f12, "01 - Overture 'Egmont', Op.84.flac", 37_800_000, 453),
                    Flac(u, f12, "02 - Symphony No.7 in A major, Op.92 - 1. Poco sostenuto - Vivace.flac", 61_200_000, 734),
                    Flac(u, f12, "03 - Symphony No.7 in A major, Op.92 - 2. Allegretto.flac", 56_800_000, 681),
                    Flac(u, f12, "04 - Symphony No.7 in A major, Op.92 - 3. Presto.flac", 34_200_000, 410),
                    Flac(u, f12, "05 - Symphony No.7 in A major, Op.92 - 4. Allegro con brio.flac", 48_600_000, 583),
                }));
            }

            {
                string u = "musiclover42";
                string f = "Beethoven Overtures - Herbert von Karajan (1965)";
                responses.Add(new SearchResponse(u, token, true, 5_200_000, 1, new[]
                {
                    Mp3(u, f, "01 - Overture 'Leonore' No.3, Op.72b.mp3", 14_800_000, 370, 320),
                    Mp3(u, f, "02 - Overture 'Fidelio', Op.72c.mp3", 9_600_000, 240, 320),
                    Mp3(u, f, "03 - Overture 'Coriolan', Op.62.mp3", 11_200_000, 280, 320),
                    Mp3(u, f, "04 - Overture 'Egmont', Op.84.mp3", 12_000_000, 300, 320),
                    Mp3(u, f, "05 - Overture 'King Stephen', Op.117.mp3", 9_200_000, 230, 320),
                    Mp3(u, f, "06 - Overture 'The Ruins of Athens', Op.113.mp3", 8_400_000, 210, 320),
                    Mp3(u, f, "07 - Overture 'The Consecration of the House', Op.124.mp3", 15_600_000, 390, 320),
                }));
            }

            {
                string u = "djmix";
                string f = "Bernstein Conducts Beethoven Overtures (1978)";
                responses.Add(new SearchResponse(u, token, true, 4_100_000, 0, new[]
                {
                    Flac(u, f, "01 - Overture 'Leonore' No.3, Op.72b.flac", 58_900_000, 707),
                    Flac(u, f, "02 - Overture 'Egmont', Op.84.flac", 39_200_000, 470),
                    Flac(u, f, "03 - Overture 'Coriolan', Op.62.flac", 36_400_000, 437),
                    Flac(u, f, "04 - Overture 'Fidelio', Op.72c.flac", 31_200_000, 374),
                    Flac(u, f, "05 - Overture 'The Creatures of Prometheus', Op.43.flac", 28_400_000, 340),
                    Flac(u, f, "06 - Overture 'King Stephen', Op.117.flac", 30_800_000, 369),
                }));
            }

            {
                string u = "soundwave";
                string f = "Beethoven Overtures - Szell, Cleveland Orchestra (1967)";
                responses.Add(new SearchResponse(u, token, true, 3_400_000, 4, new[]
                {
                    Mp3(u, f, "01 - Overture 'Leonore' No.2, Op.72a.mp3", 13_400_000, 418, 256),
                    Mp3(u, f, "02 - Overture 'Leonore' No.3, Op.72b.mp3", 12_200_000, 381, 256),
                    Mp3(u, f, "03 - Overture 'Egmont', Op.84.mp3", 9_600_000, 300, 256),
                    Mp3(u, f, "04 - Overture 'Coriolan', Op.62.mp3", 8_800_000, 275, 256),
                    Mp3(u, f, "05 - Overture 'Fidelio', Op.72c.mp3", 7_600_000, 238, 256),
                }));
            }

            return responses;
        }

        private static BrowseResponse GenerateBeethovenFanBrowseResponse()
        {
            const string root = "@@beethoven_fan\\Classical\\Beethoven Complete Works";

            Soulseek.File F(string name, long size, int lengthSec)
            {
                string ext = name[(name.LastIndexOf('.') + 1)..];
                var attrs = new List<FileAttribute>
                {
                    new FileAttribute(FileAttributeType.BitRate, 1411),
                    new FileAttribute(FileAttributeType.Length, lengthSec),
                    new FileAttribute(FileAttributeType.SampleRate, 44100),
                    new FileAttribute(FileAttributeType.BitDepth, 16),
                };
                return new Soulseek.File(1, name, size, ext, attrs);
            }

            var dirs = new List<Soulseek.Directory>
            {
                new(root),

                new($"{root}\\CD 001 - Symphonies Nos.1&3", new[]
                {
                    F("01 - Symphony No.1 in C major, Op.21 - 1. Adagio molto - Allegro con brio.flac", 42_300_000, 507),
                    F("02 - Symphony No.1 in C major, Op.21 - 2. Andante cantabile con moto.flac", 36_100_000, 433),
                    F("03 - Symphony No.1 in C major, Op.21 - 3. Menuetto. Allegro molto e vivace.flac", 18_400_000, 220),
                    F("04 - Symphony No.1 in C major, Op.21 - 4. Adagio - Allegro molto e vivace.flac", 30_200_000, 362),
                    F("05 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 1. Allegro con brio.flac", 72_400_000, 868),
                    F("06 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 2. Marcia funebre. Adagio assai.flac", 65_200_000, 782),
                    F("07 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 3. Scherzo. Allegro vivace.flac", 28_900_000, 346),
                    F("08 - Symphony No.3 in E-flat major, Op.55 'Eroica' - 4. Finale. Allegro molto.flac", 48_200_000, 578),
                }),

                new($"{root}\\CD 002 - Symphonies Nos.2&7", new[]
                {
                    F("01 - Symphony No.2 in D major, Op.36 - 1. Adagio molto - Allegro con brio.flac", 53_800_000, 645),
                    F("02 - Symphony No.2 in D major, Op.36 - 2. Larghetto.flac", 48_600_000, 583),
                    F("03 - Symphony No.2 in D major, Op.36 - 3. Scherzo. Allegro.flac", 18_200_000, 218),
                    F("04 - Symphony No.2 in D major, Op.36 - 4. Allegro molto.flac", 31_400_000, 376),
                    F("05 - Symphony No.7 in A major, Op.92 - 1. Poco sostenuto - Vivace.flac", 61_200_000, 734),
                    F("06 - Symphony No.7 in A major, Op.92 - 2. Allegretto.flac", 42_800_000, 513),
                    F("07 - Symphony No.7 in A major, Op.92 - 3. Presto - Assai meno presto.flac", 34_200_000, 410),
                    F("08 - Symphony No.7 in A major, Op.92 - 4. Allegro con brio.flac", 38_600_000, 463),
                }),

                new($"{root}\\CD 003 - Symphonies Nos.6&8", new[]
                {
                    F("01 - Symphony No.6 in F major, Op.68 'Pastoral' - 1. Erwachen heiterer Empfindungen.flac", 52_400_000, 628),
                    F("02 - Symphony No.6 in F major, Op.68 'Pastoral' - 2. Szene am Bach.flac", 58_100_000, 697),
                    F("03 - Symphony No.6 in F major, Op.68 'Pastoral' - 3. Lustiges Zusammensein der Landleute.flac", 24_600_000, 295),
                    F("04 - Symphony No.6 in F major, Op.68 'Pastoral' - 4. Gewitter, Sturm.flac", 18_300_000, 219),
                    F("05 - Symphony No.6 in F major, Op.68 'Pastoral' - 5. Hirtengesang.flac", 44_200_000, 530),
                    F("06 - Symphony No.8 in F major, Op.93 - 1. Allegro vivace e con brio.flac", 40_100_000, 481),
                    F("07 - Symphony No.8 in F major, Op.93 - 2. Allegretto scherzando.flac", 18_700_000, 224),
                    F("08 - Symphony No.8 in F major, Op.93 - 3. Tempo di menuetto.flac", 22_400_000, 268),
                    F("09 - Symphony No.8 in F major, Op.93 - 4. Allegro vivace.flac", 34_600_000, 415),
                }),

                new($"{root}\\CD 004 - Symphonies Nos.4&5", new[]
                {
                    F("01 - Symphony No.4 in B-flat major, Op.60 - 1. Adagio - Allegro vivace.flac", 49_800_000, 597),
                    F("02 - Symphony No.4 in B-flat major, Op.60 - 2. Adagio.flac", 46_200_000, 554),
                    F("03 - Symphony No.4 in B-flat major, Op.60 - 3. Menuetto. Allegro vivace.flac", 26_800_000, 321),
                    F("04 - Symphony No.4 in B-flat major, Op.60 - 4. Allegro ma non troppo.flac", 32_100_000, 385),
                    F("05 - Symphony No.5 in C minor, Op.67 - 1. Allegro con brio.flac", 35_400_000, 424),
                    F("06 - Symphony No.5 in C minor, Op.67 - 2. Andante con moto.flac", 46_800_000, 561),
                    F("07 - Symphony No.5 in C minor, Op.67 - 3. Allegro.flac", 25_600_000, 307),
                    F("08 - Symphony No.5 in C minor, Op.67 - 4. Allegro.flac", 48_400_000, 580),
                }),

                new($"{root}\\CD 005 - Symphony No.9", new[]
                {
                    F("01 - Symphony No.9 in D minor, Op.125 - 'Choral' - 1. Allegro ma non troppo.flac", 72_800_000, 873),
                    F("02 - Symphony No.9 in D minor, Op.125 - 'Choral' - 2. Molto vivace.flac", 55_370_000, 664),
                    F("03 - Symphony No.9 in D minor, Op.125 - 'Choral' - 3. Adagio molto e cantabile.flac", 76_560_000, 918),
                    F("04 - Symphony No.9 in D minor, Op.125 - 'Choral' - 4. Presto - 'O Freunde'.flac", 102_400_000, 1228),
                }),

                new($"{root}\\CD 006 - Piano Concertos Nos.1&3", new[]
                {
                    F("01 - Piano Concerto No.1 in C major, Op.15 - 1. Allegro con brio.flac", 68_200_000, 818),
                    F("02 - Piano Concerto No.1 in C major, Op.15 - 2. Largo.flac", 48_400_000, 580),
                    F("03 - Piano Concerto No.1 in C major, Op.15 - 3. Rondo. Allegro.flac", 41_200_000, 494),
                    F("04 - Piano Concerto No.3 in C minor, Op.37 - 1. Allegro con brio.flac", 72_600_000, 871),
                    F("05 - Piano Concerto No.3 in C minor, Op.37 - 2. Largo.flac", 44_800_000, 537),
                    F("06 - Piano Concerto No.3 in C minor, Op.37 - 3. Rondo. Allegro.flac", 46_200_000, 554),
                }),

                new($"{root}\\CD 007 - Piano Concertos No.2&Op.61", new[]
                {
                    F("01 - Piano Concerto No.2 in B-flat major, Op.19 - 1. Allegro con brio.flac", 58_400_000, 700),
                    F("02 - Piano Concerto No.2 in B-flat major, Op.19 - 2. Adagio.flac", 38_600_000, 463),
                    F("03 - Piano Concerto No.2 in B-flat major, Op.19 - 3. Rondo. Molto allegro.flac", 30_200_000, 362),
                    F("04 - Violin Concerto (Piano Version), Op.61a - 1. Allegro ma non troppo.flac", 64_800_000, 777),
                    F("05 - Violin Concerto (Piano Version), Op.61a - 2. Larghetto.flac", 42_200_000, 506),
                    F("06 - Violin Concerto (Piano Version), Op.61a - 3. Rondo. Allegro.flac", 48_600_000, 583),
                }),

                new($"{root}\\CD 008 - Piano Concertos Nos.4&5", new[]
                {
                    F("01 - Piano Concerto No.4 in G major, Op.58 - 1. Allegro moderato.flac", 68_400_000, 820),
                    F("02 - Piano Concerto No.4 in G major, Op.58 - 2. Andante con moto.flac", 24_600_000, 295),
                    F("03 - Piano Concerto No.4 in G major, Op.58 - 3. Rondo. Vivace.flac", 44_200_000, 530),
                    F("04 - Piano Concerto No.5 in E-flat major, Op.73 'Emperor' - 1. Allegro.flac", 82_400_000, 988),
                    F("05 - Piano Concerto No.5 in E-flat major, Op.73 'Emperor' - 2. Adagio un poco mosso.flac", 38_800_000, 465),
                    F("06 - Piano Concerto No.5 in E-flat major, Op.73 'Emperor' - 3. Rondo. Allegro.flac", 50_200_000, 602),
                }),

                new($"{root}\\CD 009 - Violin Concerto", new[]
                {
                    F("01 - Violin Concerto in D major, Op.61 - 1. Allegro ma non troppo.flac", 78_200_000, 938),
                    F("02 - Violin Concerto in D major, Op.61 - 2. Larghetto.flac", 44_600_000, 535),
                    F("03 - Violin Concerto in D major, Op.61 - 3. Rondo. Allegro.flac", 46_800_000, 561),
                    F("04 - Romanze No.1 in G major, Op.40.flac", 32_400_000, 388),
                    F("05 - Romanze No.2 in F major, Op.50.flac", 38_200_000, 458),
                }),

                new($"{root}\\CD 010 - Triple Concerto", new[]
                {
                    F("01 - Triple Concerto in C major, Op.56 - 1. Allegro.flac", 72_800_000, 873),
                    F("02 - Triple Concerto in C major, Op.56 - 2. Largo.flac", 28_400_000, 340),
                    F("03 - Triple Concerto in C major, Op.56 - 3. Rondo alla Polacca.flac", 58_600_000, 703),
                    F("04 - Choral Fantasy in C minor, Op.80.flac", 82_400_000, 988),
                }),

                new($"{root}\\CD 011 - Overtures", new[]
                {
                    F("01 - Overture 'Leonore' No.1, Op.138.flac", 42_300_000, 507),
                    F("02 - Overture 'Leonore' No.2, Op.72a.flac", 57_800_000, 693),
                    F("03 - Overture 'Leonore' No.3, Op.72b.flac", 58_200_000, 698),
                    F("04 - Overture 'Fidelio', Op.72c.flac", 30_400_000, 364),
                    F("05 - Overture 'Coriolan', Op.62.flac", 36_700_000, 440),
                    F("06 - Overture 'Egmont', Op.84.flac", 38_100_000, 457),
                    F("07 - Overture 'King Stephen', Op.117.flac", 30_200_000, 362),
                    F("08 - Overture 'The Ruins of Athens', Op.113.flac", 25_800_000, 309),
                    F("09 - Overture 'The Creatures of Prometheus', Op.43.flac", 27_100_000, 325),
                    F("10 - Overture 'Namensfeier', Op.115.flac", 28_400_000, 340),
                    F("11 - Overture 'The Consecration of the House', Op.124.flac", 46_900_000, 562),
                }),

                new($"{root}\\CD 012 - Orchestral Works, Organ Works", new[]
                {
                    F("01 - Wellington's Victory, Op.91 - Part 1. Battle.flac", 38_700_000, 464),
                    F("02 - Wellington's Victory, Op.91 - Part 2. Victory Symphony.flac", 32_100_000, 385),
                    F("03 - March in D major, WoO 24.flac", 14_200_000, 170),
                    F("04 - Polonaise in D major, WoO 21.flac", 16_800_000, 201),
                    F("05 - Ecossaise in D major, WoO 22.flac", 5_300_000, 63),
                    F("06 - Gratulations-Menuett, WoO 3.flac", 12_400_000, 148),
                    F("07 - Fugue in D major for Organ, WoO 31.flac", 8_600_000, 103),
                }),

                new($"{root}\\CD 013 - Dances I", new[]
                {
                    F("01 - 12 Minuets, WoO 7 - No.1 in D major.flac", 8_400_000, 100),
                    F("02 - 12 Minuets, WoO 7 - No.2 in B-flat major.flac", 7_200_000, 86),
                    F("03 - 12 Minuets, WoO 7 - No.3 in G major.flac", 8_800_000, 105),
                    F("04 - 12 Minuets, WoO 7 - No.4 in E-flat major.flac", 7_600_000, 91),
                    F("05 - 12 Minuets, WoO 7 - No.5 in C major.flac", 9_200_000, 110),
                    F("06 - 12 Minuets, WoO 7 - No.6 in A major.flac", 8_100_000, 97),
                    F("07 - 12 German Dances, WoO 8 - No.1 in C major.flac", 6_800_000, 81),
                    F("08 - 12 German Dances, WoO 8 - No.2 in A major.flac", 7_400_000, 88),
                    F("09 - 12 German Dances, WoO 8 - No.3 in F major.flac", 6_200_000, 74),
                    F("10 - 12 German Dances, WoO 8 - No.4 in B-flat major.flac", 7_800_000, 93),
                    F("11 - 12 German Dances, WoO 8 - No.5 in E-flat major.flac", 6_600_000, 79),
                    F("12 - 12 German Dances, WoO 8 - No.6 in G major.flac", 7_100_000, 85),
                }),

                new($"{root}\\CD 014 - Dances II", new[]
                {
                    F("01 - 12 Contredanses, WoO 14 - No.1.flac", 5_800_000, 69),
                    F("02 - 12 Contredanses, WoO 14 - No.2.flac", 6_400_000, 76),
                    F("03 - 12 Contredanses, WoO 14 - No.3.flac", 5_200_000, 62),
                    F("04 - 12 Contredanses, WoO 14 - No.4.flac", 6_100_000, 73),
                    F("05 - 12 Contredanses, WoO 14 - No.5.flac", 5_600_000, 67),
                    F("06 - 12 Contredanses, WoO 14 - No.6.flac", 7_200_000, 86),
                    F("07 - 6 Landlerische Tanze, WoO 15 - No.1.flac", 4_800_000, 57),
                    F("08 - 6 Landlerische Tanze, WoO 15 - No.2.flac", 5_100_000, 61),
                    F("09 - 6 Landlerische Tanze, WoO 15 - No.3.flac", 4_600_000, 55),
                    F("10 - 11 Modlinger Tanze, WoO 17 - No.1.flac", 5_400_000, 64),
                    F("11 - 11 Modlinger Tanze, WoO 17 - No.2.flac", 4_900_000, 58),
                }),

                new($"{root}\\CD 015 - Music for Wind Ensemble I", new[]
                {
                    F("01 - Octet in E-flat major, Op.103 - 1. Allegro.flac", 34_600_000, 415),
                    F("02 - Octet in E-flat major, Op.103 - 2. Andante.flac", 28_200_000, 338),
                    F("03 - Octet in E-flat major, Op.103 - 3. Menuetto.flac", 18_400_000, 220),
                    F("04 - Octet in E-flat major, Op.103 - 4. Finale. Presto.flac", 22_800_000, 273),
                    F("05 - Rondino in E-flat major, WoO 25.flac", 16_200_000, 194),
                    F("06 - Sextet in E-flat major, Op.71 - 1. Adagio - Allegro.flac", 32_400_000, 388),
                    F("07 - Sextet in E-flat major, Op.71 - 2. Adagio.flac", 26_800_000, 321),
                    F("08 - Sextet in E-flat major, Op.71 - 3. Menuetto. Quasi allegretto.flac", 18_600_000, 223),
                    F("09 - Sextet in E-flat major, Op.71 - 4. Rondo. Allegro.flac", 24_200_000, 290),
                }),

                new($"{root}\\CD 016 - Music for Wind Ensemble II", new[]
                {
                    F("01 - March in B-flat major, WoO 29 No.1.flac", 12_400_000, 148),
                    F("02 - March in F major, WoO 19.flac", 14_800_000, 177),
                    F("03 - March in C major, WoO 20.flac", 11_200_000, 134),
                    F("04 - Ecossaise in D major, WoO 22.flac", 5_800_000, 69),
                    F("05 - Three Equali for Four Trombones, WoO 30 - No.1 Andante.flac", 8_400_000, 100),
                    F("06 - Three Equali for Four Trombones, WoO 30 - No.2 Poco adagio.flac", 6_200_000, 74),
                    F("07 - Three Equali for Four Trombones, WoO 30 - No.3 Poco sostenuto.flac", 7_800_000, 93),
                }),

                new($"{root}\\CD 017 - Chamber Music for Flute I", new[]
                {
                    F("01 - Serenade in D major, Op.25 - 1. Entrata. Allegro.flac", 18_400_000, 220),
                    F("02 - Serenade in D major, Op.25 - 2. Tempo ordinario d'un menuetto.flac", 22_600_000, 271),
                    F("03 - Serenade in D major, Op.25 - 3. Allegro molto.flac", 14_200_000, 170),
                    F("04 - Serenade in D major, Op.25 - 4. Andante con variazioni.flac", 38_400_000, 460),
                    F("05 - Serenade in D major, Op.25 - 5. Allegro scherzando e vivace.flac", 12_800_000, 153),
                    F("06 - Serenade in D major, Op.25 - 6. Adagio - Allegro vivace e disinvolto.flac", 28_600_000, 343),
                }),

                new($"{root}\\CD 018 - Chamber Music for Flute II", new[]
                {
                    F("01 - Trio in G major, WoO 37 - 1. Allegro.flac", 32_400_000, 388),
                    F("02 - Trio in G major, WoO 37 - 2. Adagio.flac", 26_200_000, 314),
                    F("03 - Trio in G major, WoO 37 - 3. Thema andante con variazioni.flac", 38_800_000, 465),
                    F("04 - Duo No.1 in C major for Clarinet and Bassoon, WoO 27 - 1. Allegro commodo.flac", 14_200_000, 170),
                    F("05 - Duo No.1 in C major for Clarinet and Bassoon, WoO 27 - 2. Larghetto sostenuto.flac", 18_600_000, 223),
                    F("06 - Duo No.1 in C major for Clarinet and Bassoon, WoO 27 - 3. Rondo. Allegretto.flac", 16_400_000, 196),
                }),

                new($"{root}\\CD 019 - Septet Op.20 & Sextet Op.81b", new[]
                {
                    F("01 - Septet in E-flat major, Op.20 - 1. Adagio - Allegro con brio.flac", 48_200_000, 578),
                    F("02 - Septet in E-flat major, Op.20 - 2. Adagio cantabile.flac", 42_600_000, 511),
                    F("03 - Septet in E-flat major, Op.20 - 3. Tempo di menuetto.flac", 22_400_000, 268),
                    F("04 - Septet in E-flat major, Op.20 - 4. Tema con variazioni. Andante.flac", 38_800_000, 465),
                    F("05 - Septet in E-flat major, Op.20 - 5. Scherzo. Allegro molto e vivace.flac", 16_200_000, 194),
                    F("06 - Septet in E-flat major, Op.20 - 6. Andante con moto alla marcia - Presto.flac", 44_600_000, 535),
                    F("07 - Sextet in E-flat major, Op.81b - 1. Allegro con brio.flac", 36_400_000, 436),
                    F("08 - Sextet in E-flat major, Op.81b - 2. Adagio.flac", 28_800_000, 345),
                    F("09 - Sextet in E-flat major, Op.81b - 3. Rondo. Allegro.flac", 32_200_000, 386),
                }),
            };

            dirs.Insert(0, new Soulseek.Directory("@@beethoven_fan\\Classical"));

            return new BrowseResponse(dirs);
        }

        private static SearchResponse MakeResponseFileTypeBitRate(int resolvedToken, string search, string cachedType, double cachedBitRate = 128.0)
        {
            var resp = GenerateMockSearchResponse(resolvedToken, search);
            resp.cachedDominantFileType = cachedType;
            resp.cachedCalcBitRate = cachedBitRate;
            return resp;
        }

        private static List<SearchResponse> MakeChipResponses(int resolvedToken, string search, params (string type, int count)[] buckets)
        {
            var list = new List<SearchResponse>();
            foreach (var (t, c) in buckets)
            {
                for (int i = 0; i < c; i++)
                {
                    list.Add(MakeResponseFileTypeBitRate(resolvedToken, search, t));
                }
            }
            return list;
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

            string joinedTerms = string.Join(" ", query.Terms).ToLowerInvariant();

            if (Seeker.Debug.SearchCaptureStore.IsConfigured &&
                Seeker.Debug.SearchCaptureStore.TryLoad(joinedTerms, out var capturedResponses, out _))
            {
                int delayPerCapture = capturedResponses.Count > 0 ? totalTimeMs / capturedResponses.Count : 0;
                var replayList = new List<SearchResponse>();
                for (int i = 0; i < capturedResponses.Count; i++)
                {
                    if (cancellationToken?.IsCancellationRequested == true)
                    {
                        break;
                    }
                    replayList.Add(capturedResponses[i]);
                    var curSearch = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, i + 1, 0, 0);
                    options?.ResponseReceived?.Invoke((curSearch, capturedResponses[i]));
                    if (delayPerCapture > 0)
                    {
                        await Task.Delay(delayPerCapture).ConfigureAwait(false);
                    }
                }
                var replayDone = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Completed, replayList.Count, 0, 0);
                SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(SearchStates.InProgress, replayDone));
                return (replayDone, replayList.AsReadOnly());
            }

            bool isBeethovenOverture = joinedTerms.Contains("beethoven") && joinedTerms.Contains("overture");
            bool isChipTestOther = joinedTerms.Contains("chiptest") && joinedTerms.Contains("other");
            bool isSlowSearch = joinedTerms.Contains("slowsearch");
            bool is0Results = joinedTerms.Contains("0results");
            bool is1Results = joinedTerms.Contains("1results");
            bool isCurated = isBeethovenOverture || isChipTestOther;

            var allResponses = new List<SearchResponse>();
            if (isCurated)
            {
                List<SearchResponse> curatedResponses = new();
                if (isBeethovenOverture)
                {
                    curatedResponses = GenerateBeethovenOvertureResponses(resolvedToken);
                } 
                else if (isChipTestOther)
                {
                    curatedResponses = MakeChipResponses(resolvedToken, search,
                        ("mp3", 10),
                        ("mp3 (vbr)", 10),
                        ("mp3 (320kbs)", 10),
                        ("mp3 (256kbs)", 10),
                        ("mp3 (196kbs)", 1),
                        ("mp3 (128kbs)", 1),
                        ("mp3 (120kbs)", 1),
                        ("mp3 (96kbs)", 1),
                        ("flac", 10),
                        ("flac (vbr)", 10),
                        ("flac (16, 44.1 kHz)", 20),
                        ("flac (24, 44.1 kHz)", 10),
                        ("flac (16, 48 kHz)", 10),
                        ("flac (test)", 1),
                        ("flac (test2)", 1),
                        ("m4a", 10),
                        ("aac", 10),
                        ("alac", 10),
                        ("test3", 1),
                        ("test4", 1),
                        ("test5", 1),
                        ("test6", 1),
                        ("test7", 1),
                        ("test8", 1),
                        ("test9", 1),
                        ("test10", 1),
                        ("tar.gz", 10),
                        ("epub", 7),
                        ("txt", 7),
                        ("", 1),
                        ("flac (test3)", 1)
                    );
                }
                int delayPerCurated = curatedResponses.Count > 0 ? totalTimeMs / curatedResponses.Count : 0;
                for (int i = 0; i < curatedResponses.Count; i++)
                {
                    if (cancellationToken?.IsCancellationRequested == true)
                    {
                        break;
                    }
                    allResponses.Add(curatedResponses[i]);
                    var currentSearch = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, i + 1, 0, 0);
                    options?.ResponseReceived?.Invoke((currentSearch, curatedResponses[i]));
                    if (delayPerCurated > 0)
                    {
                        await Task.Delay(delayPerCurated).ConfigureAwait(false);
                    }
                }
            }
            else if (is0Results)
            {
                await Task.Delay(10000).ConfigureAwait(false);
            }
            else if (is1Results)
            {
                await Task.Delay(8000).ConfigureAwait(false);
                var response = GenerateMockSearchResponse(resolvedToken, search);
                allResponses.Add(response);
                var currentSearch = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, 1, 0, 0);
                options?.ResponseReceived?.Invoke((currentSearch, response));
                await Task.Delay(2000).ConfigureAwait(false);
            }
            else
            {
                if (isSlowSearch)
                {
                    await Task.Delay(3000).ConfigureAwait(false);
                }
                for (int i = 0; i < count; i++)
                {
                    if (cancellationToken?.IsCancellationRequested == true)
                    {
                        break;
                    }
                    var response = GenerateMockSearchResponse(resolvedToken, search);
                    allResponses.Add(response);
                    var currentSearch = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.InProgress, i + 1, 0, 0);
                    options?.ResponseReceived?.Invoke((currentSearch, response));
                    if (delayPerResponse > 0)
                    {
                        await Task.Delay(delayPerResponse).ConfigureAwait(false);
                    }
                }
            }

            var searchCompleted = new Soulseek.Search(query, resolvedScope, resolvedToken, SearchStates.Completed, allResponses.Count, 0, 0);
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
                if (filename.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    UpdateState(TransferStates.Queued | TransferStates.Locally);
                    UpdateState(TransferStates.Completed | TransferStates.Errored);
                    throw new TransferRejectedException("Transfer rejected: filename contains 'failed'");
                }

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
                if (filename.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    UpdateState(TransferStates.Queued | TransferStates.Locally);
                    UpdateState(TransferStates.Completed | TransferStates.Errored);
                    throw new TransferRejectedException("Transfer rejected: filename contains 'failed'");
                }

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
                    // sporadic failure
                    //if (_random.Next(100) == 0)
                    //{
                    //    throw new Exception("Simulated Exception");
                    //}
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
            Options = (Options ?? new SoulseekClientOptions()).With(
                searchResponseResolver: patch.SearchResponseResolver,
                browseResponseResolver: patch.BrowseResponseResolver,
                enqueueDownload: patch.EnqueueDownload,
                directoryContentsResolver: patch.DirectoryContentsResolver);
            return Task.FromResult(true);
        }

        public async Task<RoomList> GetRoomListAsync(CancellationToken? cancellationToken = null)
        {
            if (GetRoomListAsyncHandler != null) return await GetRoomListAsyncHandler(cancellationToken);

            await Task.Delay(20_000).ConfigureAwait(false);

            var roomList = new RoomList(
                publicList:            GenerateMockRooms(20, stableCount: 8, suffix: "_public"),
                privateList:           GenerateMockRooms(10, stableCount: 4, suffix: "_private"),
                ownedList:             GenerateMockRooms(10, stableCount: 4, suffix: "_owned"),
                moderatedRoomNameList: GenerateMockRooms(10, stableCount: 4, suffix: "_moderated").Select(r => r.Name));

            RaiseRoomListReceived(roomList);
            return roomList;
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

        public async Task<UserStatistics> GetUserStatisticsAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (GetUserStatisticsAsyncHandler != null) return await GetUserStatisticsAsyncHandler(username, cancellationToken);
            // this will either be very slow or very fast to test it coming in before or after UserInfo
            bool fast = _random.Next(0,2) == 0;
            int wait = fast ? 100 : 10_000;
            await Task.Delay(wait);
            var userStats = new UserStatistics(username, _random.Next(100_000, 10_000_000), _random.Next(0, 100), _random.Next(0, 100_000), _random.Next(0, 10_000));
            this.UserStatisticsChanged?.Invoke(this, userStats);
            return await Task.FromResult<UserStatistics>(userStats);
        }

        public async Task<UserInfo> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (GetUserInfoAsyncHandler != null) return await GetUserInfoAsyncHandler(username, cancellationToken);
            await Task.Delay(100);
            bool includePic = (_random.Next(0,2) == 0 || username.Contains("pic") && !username.Contains("nopic"));
            byte[]? pictureBytes = null;
            if (includePic)
            {
                var stream = typeof(MockSoulseekClient).Assembly.GetManifestResourceStream("Common.DebugAssets.githublogo.png");
                if (stream != null)
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        stream.CopyTo(ms);
                        pictureBytes = ms.ToArray();
                    }
                    stream.Dispose();
                }
            }
            var res = await Task.FromResult(new UserInfo("Mock user description for " + username, _random.Next(0, 100), _random.Next(0, 1), includePic, pictureBytes));
            return res;
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

        public async Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null, bool wasFileLatin1Decoded = false, bool wasFolderLatin1Decoded = false)
        {
            await Task.Delay(_random.Next(0, 5000));
            return _random.Next(1, 125);
        }

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
        public void Dispose()
        {
            StopBackgroundTimers();
        }
    }
}
#endif
