// <copyright file="DistributedConnectionManager.cs" company="JP Dillingham">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Manages distributed <see cref="IMessageConnection"/> instances for the application.
    /// </summary>
    internal sealed class DistributedConnectionManager : IDistributedConnectionManager
    {
        private static readonly int StatusAgeLimit = 300000; // 5 minutes
        private static readonly int StatusDebounceTime = 5000; // 5 seconds
        private static readonly int WatchdogTime = 900000; // 15 minutes
        private static readonly double LatencyAlpha = 0.005;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedConnectionManager"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="connectionFactory">The IConnectionFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public DistributedConnectionManager(
            SoulseekClient soulseekClient,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient;

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            StatusDebounceTimer = new SystemTimer()
            {
                Interval = StatusDebounceTime,
                Enabled = false,
                AutoReset = false,
            };

            StatusDebounceTimer.Elapsed += (sender, e) => UpdateStatusAsync().ConfigureAwait(false);

            WatchdogTimer = new SystemTimer()
            {
                Enabled = true,
                AutoReset = true,
                Interval = WatchdogTime,
            };

            WatchdogTimer.Elapsed += WatchdogTimer_Elapsed;
        }

        /// <summary>
        ///     Occurs when a child connection is added.
        /// </summary>
        public event EventHandler<DistributedChildEventArgs> ChildAdded;

        /// <summary>
        ///     Occurs when a child connection is disconnected.
        /// </summary>
        public event EventHandler<DistributedChildEventArgs> ChildDisconnected;

        /// <summary>
        ///     Occurs when the client is demoted from a branch root on the distributed network.
        /// </summary>
        public event EventHandler DemotedFromBranchRoot;

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when a new parent is adopted.
        /// </summary>
        public event EventHandler<DistributedParentEventArgs> ParentAdopted;

        /// <summary>
        ///     Occurs when the parent is disconnected.
        /// </summary>
        public event EventHandler<DistributedParentEventArgs> ParentDisconnected;

        /// <summary>
        ///     Occurs when the client has been promoted to a branch root on the distributed network.
        /// </summary>
        public event EventHandler PromotedToBranchRoot;

        /// <summary>
        ///     Occurs when the state of the distributed network changes.
        /// </summary>
        public event EventHandler<DistributedNetworkInfo> StateChanged;

        /// <summary>
        ///     Gets the average child broadcast latency.
        /// </summary>
        public double? AverageBroadcastLatency { get; private set; } = null;

        /// <summary>
        ///     Gets the current distributed branch level.
        /// </summary>
        public int BranchLevel => HasParent ? ParentBranchLevel + 1 : 0;

        /// <summary>
        ///     Gets the current distributed branch root.
        /// </summary>
        public string BranchRoot => (HasParent ? ParentBranchRoot : SoulseekClient.Username) ?? string.Empty;

        /// <summary>
        ///     Gets a value indicating whether child connections can be accepted.
        /// </summary>
        public bool CanAcceptChildren => Enabled && AcceptChildren && (HasParent || IsBranchRoot) && ChildDictionary.Count < ChildLimit;

        /// <summary>
        ///     Gets the number of allowed concurrent child connections.
        /// </summary>
        public int ChildLimit => SoulseekClient.Options.DistributedChildLimit;

        /// <summary>
        ///     Gets the current list of child connections.
        /// </summary>
        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children => ChildDictionary.Select(c => (c.Key, c.Value)).ToList().AsReadOnly();

        /// <summary>
        ///     Gets a value indicating whether a parent connection is established.
        /// </summary>
        public bool HasParent => ParentConnection?.State == ConnectionState.Connected;

        /// <summary>
        ///     Gets a value indicating whether the client is currently operating as a branch root.
        /// </summary>
        public bool IsBranchRoot { get; private set; } = false;

        /// <summary>
        ///     Gets the current parent connection.
        /// </summary>
        public (string Username, IPEndPoint IPEndPoint) Parent =>
            ParentConnection == null ? (string.Empty, null) : (ParentConnection.Username, ParentConnection.IPEndPoint);

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        public IReadOnlyDictionary<int, string> PendingSolicitations => new ReadOnlyDictionary<int, string>(PendingSolicitationDictionary);

        private bool AcceptChildren => SoulseekClient.Options.AcceptDistributedChildren;

        /// <remarks>
        ///     <para>Provides a thread-safe collection for managing connecting and connected children.</para>
        ///     <para>
        ///         The Lazy value allows us to use the Add and Update functions passed to the concurrent dictionary in a
        ///         thread-safe manner; the lazy values are swapped into the collection atomically, but the code wrapped in the
        ///         lazy value is executed when we await the value shortly after.
        ///     </para>
        ///     <para>
        ///         This collection should be used any time a child connection needs to be referenced, such as when broadcasting messages.
        ///     </para>
        /// </remarks>
        private ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>> ChildConnectionDictionary { get; set; } = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();

        /// <remarks>
        ///     <para>Provides a collection of chilren for which a connection was successfully negotiated.</para>
        ///     <para>
        ///         Unlike <see cref="ChildConnectionDictionary"/>, this collection does not include children for which a
        ///         connection is being established, making it a better representation of children that have successfully
        ///         connected for status reporting purposes.
        ///     </para>
        ///     <para>
        ///         This collection is redundant but was introduced to get around issues capturing an accurate count for status updates.
        ///     </para>
        /// </remarks>
        private ConcurrentDictionary<string, IPEndPoint> ChildDictionary { get; set; } = new ConcurrentDictionary<string, IPEndPoint>();

        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private bool Enabled => SoulseekClient.Options.EnableDistributedNetwork;
        private string LastStatus { get; set; }
        private DateTime LastStatusTimestamp { get; set; }
        private int ParentBranchLevel { get; set; } = 0;
        private string ParentBranchRoot { get; set; } = string.Empty;
        private List<(string Username, IPEndPoint IPEndPoint)> ParentCandidateList { get; set; } = new List<(string Username, IPEndPoint iPEndPoint)>();
        private IMessageConnection ParentConnection { get; set; }
        private SemaphoreSlim ParentSyncRoot { get; } = new SemaphoreSlim(1, 1);
        private ConcurrentDictionary<string, CancellationTokenSource> PendingInboundIndirectConnectionDictionary { get; set; } = new ConcurrentDictionary<string, CancellationTokenSource>();
        private ConcurrentDictionary<int, string> PendingSolicitationDictionary { get; } = new ConcurrentDictionary<int, string>();
        private SoulseekClient SoulseekClient { get; }
        private SystemTimer StatusDebounceTimer { get; set; }
        private SemaphoreSlim StatusSyncRoot { get; } = new SemaphoreSlim(1, 1);
        private SystemTimer WatchdogTimer { get; }

        /// <summary>
        ///     Adds a new child connection from an incoming connection.
        /// </summary>
        /// <remarks>
        ///     This method will be invoked from <see cref="ListenerHandler"/> upon receipt of an incoming unsolicited connection
        ///     only. Because this connection is fully established by the time it is passed to this method, it must supersede any
        ///     cached connection, as it will be the most recently established connection as tracked by the remote user.
        /// </remarks>
        /// <param name="username">The username from which the connection originated.</param>
        /// <param name="incomingConnection">The accepted connection.</param>
        /// <returns>The operation context.</returns>
        public async Task AddOrUpdateChildConnectionAsync(string username, IConnection incomingConnection)
        {
            var c = incomingConnection;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Inbound child connection to {username} ({c.IPEndPoint}) rejected: enabled {Enabled}; has parent: {HasParent}; is branch root: {IsBranchRoot}; children: {ChildDictionary.Count}/{ChildLimit}");
                c.Dispose();
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            try
            {
                await ChildConnectionDictionary.AddOrUpdate(
                    username,
                    new Lazy<Task<IMessageConnection>>(() => GetConnection()),
                    (key, cachedConnectionRecord) => new Lazy<Task<IMessageConnection>>(() => GetConnection(cachedConnectionRecord))).Value.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to establish an inbound direct child connection to {username} ({c.IPEndPoint}): {ex.Message}";
                Diagnostic.Debug($"{msg} (type: {c.Type}, id: {c.Id})");
                Diagnostic.Debug($"Purging child connection cache of failed connection to {username} ({c.IPEndPoint})");
                ChildConnectionDictionary.TryRemove(username, out _);
                throw new ConnectionException(msg, ex);
            }

            async Task<IMessageConnection> GetConnection(Lazy<Task<IMessageConnection>> cachedConnectionRecord = null)
            {
                Diagnostic.Debug($"Inbound child connection to {username} ({c.IPEndPoint}) accepted. (type: {c.Type}, id: {c.Id}");

                var superseded = false;

                var connection = ConnectionFactory.GetDistributedConnection(
                    username,
                    c.IPEndPoint,
                    SoulseekClient.Options.DistributedConnectionOptions,
                    c.HandoffTcpClient());

                Diagnostic.Debug($"Inbound child connection to {username} ({connection.IPEndPoint}) handed off. (old: {c.Id}, new: {connection.Id})");
                c.Dispose();

                connection.Type = ConnectionTypes.Inbound | ConnectionTypes.Direct;
                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleChildMessageRead;
                connection.MessageWritten += SoulseekClient.DistributedMessageHandler.HandleChildMessageWritten;
                connection.Disconnected += (sender, args) => ((IConnection)sender).Dispose();

                if (cachedConnectionRecord != null)
                {
                    if (PendingInboundIndirectConnectionDictionary.TryGetValue(username, out var pendingCts))
                    {
                        // cancel any connection pending due to a ConnectToPeer message; we don't want it to succeed because the
                        // remote client would supersede this connection with it.
                        Diagnostic.Debug($"Cancelling pending indirect child connection to {username}");
                        pendingCts.Cancel();
                    }

                    try
                    {
                        // because we cancelled any pending connection above, the Lazy<> function has completed executing and we
                        // know that awaiting .Value will return immediately, allowing us to tear down the existing connection.
                        var cachedConnection = await cachedConnectionRecord.Value.ConfigureAwait(false);
                        cachedConnection.Disconnected -= ChildConnection_Disconnected;
                        Diagnostic.Debug($"Superseding existing child connection to {username} ({cachedConnection.IPEndPoint}) (old: {c.Id}, new: {connection.Id}");
                        cachedConnection.Disconnect("Superseded.");
                        cachedConnection.Dispose();
                        superseded = true;
                    }
                    catch
                    {
                        // noop
                    }
                }

                try
                {
                    connection.StartReadingContinuously();

                    await connection.WriteAsync(GetBranchInformation()).ConfigureAwait(false);
                }
                catch
                {
                    connection.Dispose();
                    throw;
                }

                connection.Disconnected += ChildConnection_Disconnected;

                ChildDictionary.AddOrUpdate(username, connection.IPEndPoint, (k, v) => connection.IPEndPoint);

                Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
                Diagnostic.Info($"{(superseded ? "Updated" : "Added")} child connection to {connection.Username} ({connection.IPEndPoint})");

                if (!superseded)
                {
                    ChildAdded?.Invoke(this, new DistributedChildEventArgs(connection.Username, connection.IPEndPoint));
                    StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));
                }

                _ = UpdateStatusEventuallyAsync().ConfigureAwait(false);

                return connection;
            }
        }

        /// <summary>
        ///     Asynchronously connects to one of the specified <paramref name="parentCandidates"/>.
        /// </summary>
        /// <remarks>
        ///     This method is invoked upon receipt of a list of new parent candidates via a <see cref="NetInfoNotification"/>, or
        ///     when a previous parent is disconnected. In the event of a disconnection, a connection will be attempted using the
        ///     existing list of parent connections, if there is one.
        /// </remarks>
        /// <param name="parentCandidates">The list of parent connection candidates provided by the server.</param>
        /// <returns>The operation context.</returns>
        public async Task AddParentConnectionAsync(IEnumerable<(string Username, IPEndPoint IPEndPoint)> parentCandidates)
        {
            if (!Enabled)
            {
                Diagnostic.Debug($"Parent connection solicitation ignored; distributed network is not enabled.");
                return;
            }

            if (SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnected) || SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnecting))
            {
                return;
            }

            ParentCandidateList = parentCandidates.ToList();

            if (HasParent || ParentCandidateList.Count == 0)
            {
                var msg = HasParent ?
                    $"Parent connection solicitation ignored; already connected to parent {Parent.Username}" :
                    $"Parent candidate cache is empty; requesting a new list of candidates from the server";

                Diagnostic.Debug(msg);
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            if (!await ParentSyncRoot.WaitAsync(millisecondsTimeout: 0).ConfigureAwait(false))
            {
                Diagnostic.Debug($"Parent connection solicitation ignored; already in the process of establishing a connection.");
                return;
            }

            try
            {
                Diagnostic.Info($"Attempting to establish a new parent connection from {ParentCandidateList.Count} candidates");
                Diagnostic.Debug($"Parent candidates: {string.Join(", ", ParentCandidateList.Select(p => p.Username))}");

                using var cts = new CancellationTokenSource();
                var tasks = ParentCandidateList.Select(p => GetParentCandidateConnectionAsync(p.Username, p.IPEndPoint, cts.Token)).ToList();

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // noop
                }

                var successfulConnections = tasks
                    .Where(t => t.Status == TaskStatus.RanToCompletion)
                    .Select(async t => await t.ConfigureAwait(false))
                    .Select(t => t.Result)
                    .Where(t => t.Connection.State == ConnectionState.Connected) // successful connections that may have disconnected while we waited for others to settle
                    .OrderBy(c => c.BranchLevel)
                    .ToList();

                if (successfulConnections.Count > 0)
                {
                    Diagnostic.Debug($"Successfully established {successfulConnections.Count} connections.");

                    (ParentConnection, ParentBranchLevel, ParentBranchRoot) = successfulConnections.First();
                    Diagnostic.Debug($"Selected {ParentConnection.Username} as the best connection; branch root: {ParentBranchRoot}, branch level: {ParentBranchLevel}");

                    ParentConnection.Disconnected += ParentConnection_Disconnected;
                    ParentConnection.Disconnected -= ParentCandidateConnection_Disconnected;
                    ParentConnection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleMessageRead;
                    ParentConnection.MessageWritten += SoulseekClient.DistributedMessageHandler.HandleMessageWritten;

                    // there is a very small chance that a connection will disconnect between the time it was filtered above and before this code executes.
                    // we may or may not have bound the parent disconnect handler in time, meaning we may or may not have fired ParentDisconnected prior to
                    // firing ParentAdopted. this should be an extreme edge case and should self-correct, so this case is unhandled for the time being.
                    // if this becomes more common (ParentDisconnected firing before ParentAdopted, or ParentAdopted firing but status not updating because !HasParent),
                    // handle it here somewhere.
                    Diagnostic.Debug($"Parent connection to {ParentConnection.Username} ({ParentConnection.IPEndPoint}) established. (type: {ParentConnection.Type}, id: {ParentConnection.Id})");
                    Diagnostic.Info($"Adopted parent connection to {ParentConnection.Username} ({ParentConnection.IPEndPoint})");
                    DemoteFromBranchRoot();
                    ParentAdopted?.Invoke(this, new DistributedParentEventArgs(ParentConnection.Username, ParentConnection.IPEndPoint, ParentBranchLevel, ParentBranchRoot));
                    StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));

                    await UpdateStatusAsync().ConfigureAwait(false);
                    _ = BroadcastMessageAsync(GetBranchInformation()).ConfigureAwait(false);

                    successfulConnections.Remove((ParentConnection, ParentBranchLevel, ParentBranchRoot));
                    ParentCandidateList = successfulConnections.Select(c => (c.Connection.Username, c.Connection.IPEndPoint)).ToList();

                    Diagnostic.Debug($"Connected parent candidates not selected: {(ParentCandidateList.Count > 0 ? string.Join(", ", ParentCandidateList.Select(p => p.Username)) : "<none>")}");

                    foreach (var connection in successfulConnections.Select(c => c.Connection))
                    {
                        Diagnostic.Debug($"Disconnecting parent candidate connection to {connection.Username} ({connection.IPEndPoint})");
                        connection.Disconnect("Not selected.");
                        connection.Dispose();
                    }
                }
                else
                {
                    Diagnostic.Warning("Failed to connect to any of the available parent candidates");
                }
            }
            finally
            {
                await UpdateStatusAsync().ConfigureAwait(false);
                ParentSyncRoot.Release();
            }
        }

        /// <summary>
        ///     Asynchronously writes the specified bytes to each of the connected child connections.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context.</returns>
        public async Task BroadcastMessageAsync(byte[] bytes, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;

            static async Task Write(KeyValuePair<string, Lazy<Task<IMessageConnection>>> child, byte[] bytes, CancellationToken? cancellationToken)
            {
                IMessageConnection connection = default;

                try
                {
                    connection = await child.Value.Value.ConfigureAwait(false);

                    if (connection.State == ConnectionState.Connected)
                    {
                        await connection.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    connection?.Disconnect($"Broadcast failure: {ex.Message}");
                }
            }

            var sw = new Stopwatch();
            sw.Start();

            var tasks = new List<Task>();

            foreach (var child in ChildConnectionDictionary)
            {
                tasks.Add(Write(child, bytes, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            sw.Stop();

            if (!AverageBroadcastLatency.HasValue)
            {
                AverageBroadcastLatency = sw.ElapsedMilliseconds;
            }
            else
            {
                // EMA
                AverageBroadcastLatency = ((sw.ElapsedMilliseconds - AverageBroadcastLatency) * LatencyAlpha) + AverageBroadcastLatency;
            }
        }

        /// <summary>
        ///     Demotes the client from a branch root on the distributed network.
        /// </summary>
        public void DemoteFromBranchRoot()
        {
            if (IsBranchRoot)
            {
                IsBranchRoot = false;
                Diagnostic.Info($"Demoted from distributed branch root.");
                DemotedFromBranchRoot?.Invoke(this, EventArgs.Empty);
                StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));
            }
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IDistributedConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Adds a new child connection using the details in the specified <paramref name="connectToPeerResponse"/> and
        ///     pierces the remote peer's firewall.
        /// </summary>
        /// <remarks>
        ///     This method will be invoked from <see cref="Messaging.Handlers.ServerMessageHandler"/> upon receipt of an
        ///     unsolicited <see cref="ConnectToPeerResponse"/> of type 'D' only. This connection should only be initiated if
        ///     there is no existing connection; superseding should be avoided if possible.
        /// </remarks>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context.</returns>
        public async Task GetOrAddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            bool cached = true;
            var r = connectToPeerResponse;

            if (!CanAcceptChildren)
            {
                Diagnostic.Debug($"Inbound child connection to {r.Username} ({r.IPEndPoint}) rejected: enabled {Enabled}; has parent: {HasParent}; is branch root: {IsBranchRoot}; children: {ChildDictionary.Count}/{ChildLimit}");
                await UpdateStatusAsync().ConfigureAwait(false);
                return;
            }

            try
            {
                await ChildConnectionDictionary.GetOrAdd(
                    r.Username,
                    key => new Lazy<Task<IMessageConnection>>(() => GetConnection())).Value.ConfigureAwait(false);

                if (cached)
                {
                    Diagnostic.Debug($"Child connection from {r.Username} ({r.IPEndPoint}) for token {r.Token} ignored; connection already exists.");
                }
            }
            catch (Exception ex)
            {
                var msg = $"Failed to establish an inbound indirect child connection to {r.Username} ({r.IPEndPoint}): {ex.Message}";
                Diagnostic.Debug(msg);

                // only purge the connection if the thrown exception is something other than OperationCanceledException. if this
                // is thrown then a direct connection superseded this connection while it was being established, and
                // ChildConnectionDictionary contains the new, direct connection.
                if (!(ex is OperationCanceledException))
                {
                    Diagnostic.Debug($"Purging child connection cache of failed connection to {r.Username} ({r.IPEndPoint}).");

                    // remove the current record, which *should* be the one we added above.
                    ChildConnectionDictionary.TryRemove(r.Username, out var removed);

                    try
                    {
                        var connection = await removed.Value.ConfigureAwait(false);

                        // if the connection we removed is Direct, then a direct connection managed to come in after this attempt
                        // had timed out or failed, but before that connection was able to cancel the pending token this should be
                        // an extreme edge case, but log it as a warning so we can see how common it is.
                        if (connection.Type.HasFlag(ConnectionTypes.Direct))
                        {
                            Diagnostic.Warning($"Erroneously purged direct child connection to {r.Username} upon indirect failure");
                            ChildConnectionDictionary.TryAdd(r.Username, removed);
                        }
                    }
                    catch
                    {
                        // noop
                    }
                }

                throw new ConnectionException(msg, ex);
            }

            async Task<IMessageConnection> GetConnection()
            {
                cached = false;

                Diagnostic.Debug($"Attempting inbound indirect child connection to {r.Username} ({r.IPEndPoint}) for token {r.Token}");

                var connection = ConnectionFactory.GetDistributedConnection(
                    r.Username,
                    r.IPEndPoint,
                    SoulseekClient.Options.DistributedConnectionOptions);

                connection.Type = ConnectionTypes.Inbound | ConnectionTypes.Indirect;
                connection.MessageRead += SoulseekClient.DistributedMessageHandler.HandleChildMessageRead;
                connection.MessageWritten += SoulseekClient.DistributedMessageHandler.HandleChildMessageWritten;
                connection.Disconnected += (sender, args) => ((IConnection)sender).Dispose();

                using (var cts = new CancellationTokenSource())
                {
                    // add a record to the pending dictionary so we can tell whether the following code is waiting
                    PendingInboundIndirectConnectionDictionary.AddOrUpdate(r.Username, cts, (username, existingCts) => cts);

                    try
                    {
                        await connection.ConnectAsync(cts.Token).ConfigureAwait(false);

                        var request = new PierceFirewall(r.Token);
                        await connection.WriteAsync(request.ToByteArray(), cts.Token).ConfigureAwait(false);

                        await connection.WriteAsync(GetBranchInformation(), cts.Token).ConfigureAwait(false);
                    }
                    catch
                    {
                        connection.Dispose();
                        throw;
                    }
                    finally
                    {
                        // let everyone know this code is done executing and that .Value of the containing cache is safe to await
                        // with no delay.
                        PendingInboundIndirectConnectionDictionary.TryRemove(r.Username, out _);
                    }
                }

                connection.Disconnected += ChildConnection_Disconnected;

                ChildDictionary.AddOrUpdate(r.Username, connection.IPEndPoint, (k, v) => connection.IPEndPoint);

                Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
                Diagnostic.Info($"Added child connection to {connection.Username} ({connection.IPEndPoint})");
                ChildAdded?.Invoke(this, new DistributedChildEventArgs(connection.Username, connection.IPEndPoint));
                StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));

                _ = UpdateStatusEventuallyAsync().ConfigureAwait(false);

                return connection;
            }
        }

        /// <summary>
        ///     Promotes the client to a branch root on the distributed network.
        /// </summary>
        public void PromoteToBranchRoot()
        {
            if (!IsBranchRoot && !HasParent)
            {
                IsBranchRoot = true;
                Diagnostic.Info($"Promoted to distributed branch root.");
                PromotedToBranchRoot?.Invoke(this, EventArgs.Empty);
                StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));
            }
        }

        /// <summary>
        ///     Removes and disposes all active and queued connections.
        /// </summary>
        public async void RemoveAndDisposeAll()
        {
            PendingSolicitationDictionary.Clear();
            PendingInboundIndirectConnectionDictionary.Clear();
            ParentConnection?.Dispose();

            while (!ChildConnectionDictionary.IsEmpty)
            {
                if (ChildConnectionDictionary.TryRemove(ChildConnectionDictionary.Keys.First(), out var value))
                {
                    try
                    {
                        (await value.Value.ConfigureAwait(false))?.Dispose();
                    }
                    catch
                    {
                        // noop
                    }
                }
            }

            ChildDictionary.Clear();
        }

        /// <summary>
        ///     Resets stored state information about the distributed network.
        /// </summary>
        public void ResetStatus()
        {
            LastStatus = default;
            LastStatusTimestamp = default;
            DemoteFromBranchRoot();
        }

        /// <summary>
        ///     Sets the distributed <paramref name="branchLevel"/>.
        /// </summary>
        /// <param name="branchLevel">The distributed branch level.</param>
        public void SetParentBranchLevel(int branchLevel)
        {
            ParentBranchLevel = branchLevel;
            _ = UpdateStatusEventuallyAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Sets the distributed <paramref name="branchRoot"/>.
        /// </summary>
        /// <param name="branchRoot">The distributed branch root.</param>
        public void SetParentBranchRoot(string branchRoot)
        {
            ParentBranchRoot = branchRoot;
            _ = UpdateStatusEventuallyAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     Updates the server with the current status of the distributed network.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context.</returns>
        public async Task UpdateStatusAsync(CancellationToken? cancellationToken = null)
        {
            if (!SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || (!SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn)))
            {
                return;
            }

            await StatusSyncRoot.WaitAsync(cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

            try
            {
                var branchLevel = BranchLevel;
                var branchRoot = BranchRoot;
                var canAcceptChildren = CanAcceptChildren;
                var haveNoParents = Enabled && !HasParent;

                var status = new StringBuilder()
                    .Append($"Requesting parent: {haveNoParents}, ")
                    .Append($"Branch level: {branchLevel}, Branch root: {branchRoot}, ")
                    .Append($"Number of children: {ChildDictionary.Count}/{ChildLimit}, Accepting children: {canAcceptChildren}");

                if (!status.ToString().Equals(LastStatus, StringComparison.InvariantCultureIgnoreCase))
                {
                    Diagnostic.Debug($"Status changed; {status}");

                    try
                    {
                        var payload = new List<byte>();

                        payload.AddRange(new BranchLevelCommand(branchLevel).ToByteArray());
                        payload.AddRange(new BranchRootCommand(branchRoot).ToByteArray());
                        payload.AddRange(new AcceptChildrenCommand(canAcceptChildren).ToByteArray());
                        payload.AddRange(new HaveNoParentsCommand(haveNoParents).ToByteArray());

                        await SoulseekClient.ServerConnection.WriteAsync(payload.ToArray(), cancellationToken).ConfigureAwait(false);

                        StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));
                        Diagnostic.Info($"Updated distributed status; {status}");

                        LastStatus = status.ToString();
                        LastStatusTimestamp = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Failed to update distributed status: {ex.Message}";

                        if (SoulseekClient.State != SoulseekClientStates.Disconnected)
                        {
                            Diagnostic.Warning(msg, ex);
                        }
                        else
                        {
                            Diagnostic.Debug(msg, ex);
                        }
                    }
                }
                else
                {
                    Diagnostic.Debug($"Update skipped; status has not changed: {status}");
                }
            }
            finally
            {
                StatusSyncRoot.Release();
            }
        }

        private void ChildConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;
            ChildConnectionDictionary.TryRemove(connection.Username, out _);
            ChildDictionary.TryRemove(connection.Username, out _);

            Diagnostic.Debug($"Child connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");
            Diagnostic.Info($"Child connection to {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? "." : $": {e.Message}")}");
            ChildDisconnected?.Invoke(this, new DistributedChildEventArgs(connection.Username, connection.IPEndPoint));
            StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));

            connection.Dispose();

            _ = UpdateStatusEventuallyAsync().ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    WatchdogTimer.Dispose();
                    StatusDebounceTimer.Dispose();

                    StatusSyncRoot.Dispose();
                    ParentSyncRoot.Dispose();

                    RemoveAndDisposeAll();
                }

                Disposed = true;
            }
        }

        private byte[] GetBranchInformation()
        {
            var payload = new List<byte>();

            payload.AddRange(new DistributedBranchLevel(BranchLevel).ToByteArray());
            payload.AddRange(new DistributedBranchRoot(BranchRoot).ToByteArray());

            return payload.ToArray();
        }

        private async Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)> GetParentCandidateConnectionAsync(string username, IPEndPoint ipEndPoint, CancellationToken cancellationToken)
        {
            using var directCts = new CancellationTokenSource();
            using var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token);
            using var indirectCts = new CancellationTokenSource();
            using var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token);

            Diagnostic.Debug($"Attempting simultaneous direct and indirect parent candidate connections to {username} ({ipEndPoint})");

            var direct = GetParentCandidateConnectionDirectAsync(username, ipEndPoint, directLinkedCts.Token);
            var indirect = GetParentCandidateConnectionIndirectAsync(username, indirectLinkedCts.Token);

            var tasks = new[] { direct, indirect }.ToList();
            Task<IMessageConnection> task;

            do
            {
                task = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(task);
            }
            while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);

            if (task.Status != TaskStatus.RanToCompletion)
            {
                var msg = $"Failed to establish a direct or indirect parent candidate connection to {username} ({ipEndPoint})";
                Diagnostic.Debug(msg);
                throw new ConnectionException(msg);
            }

            var connection = await task.ConfigureAwait(false);
            var isDirect = task == direct;

            Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} parent candidate connection to {username} ({ipEndPoint}) established first, attempting to cancel {(isDirect ? "indirect" : "direct")} connection.");
            (isDirect ? indirectCts : directCts).Cancel();

            int branchLevel;
            string branchRoot;

            try
            {
                var initWait = WaitForParentCandidateConnectionInitializationAsync(connection, cancellationToken);

                if (isDirect)
                {
                    var request = new PeerInit(SoulseekClient.Username, Constants.ConnectionType.Distributed, SoulseekClient.GetNextToken());
                    await connection.WriteAsync(request.ToByteArray(), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    connection.StartReadingContinuously();
                }

                Diagnostic.Debug($"{(isDirect ? "Direct" : "Indirect")} parent candidate connection to {username} ({ipEndPoint}) initialized.  Waiting for branch information and first search request. (id: {connection.Id})");
                (branchLevel, branchRoot) = await initWait.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to negotiate parent candidate connection to {username} ({ipEndPoint}): {ex.Message}";
                Diagnostic.Debug($"{msg} (type: {connection.Type}, id: {connection.Id})");
                connection.Dispose();
                throw new ConnectionException(msg, ex);
            }

            Diagnostic.Debug($"Parent candidate connection to {username} ({ipEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
            return (connection, branchLevel, branchRoot);
        }

        private async Task<IMessageConnection> GetParentCandidateConnectionDirectAsync(string username, IPEndPoint ipEndPoint, CancellationToken cancellationToken)
        {
            Diagnostic.Debug($"Attempting direct parent candidate connection to {username} ({ipEndPoint})");

            var connection = ConnectionFactory.GetDistributedConnection(
                username,
                ipEndPoint,
                SoulseekClient.Options.DistributedConnectionOptions);

            connection.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct;
            connection.Disconnected += ParentCandidateConnection_Disconnected;

            try
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to establish a direct parent candidate connection to {username} ({ipEndPoint}): {ex.Message}");
                connection.Dispose();
                throw;
            }

            Diagnostic.Debug($"Direct parent candidate connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
            return connection;
        }

        private async Task<IMessageConnection> GetParentCandidateConnectionIndirectAsync(string username, CancellationToken cancellationToken)
        {
            var solicitationToken = SoulseekClient.GetNextToken();

            Diagnostic.Debug($"Soliciting indirect parent candidate connection to {username} with token {solicitationToken}");

            try
            {
                PendingSolicitationDictionary.TryAdd(solicitationToken, username);

                await SoulseekClient.ServerConnection
                    .WriteAsync(new ConnectToPeerRequest(solicitationToken, username, Constants.ConnectionType.Distributed), cancellationToken)
                    .ConfigureAwait(false);

                using var incomingConnection = await SoulseekClient.Waiter
                    .Wait<IConnection>(new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, username, solicitationToken), SoulseekClient.Options.DistributedConnectionOptions.ConnectTimeout, cancellationToken)
                    .ConfigureAwait(false);

                var connection = ConnectionFactory.GetDistributedConnection(
                    username,
                    incomingConnection.IPEndPoint,
                    SoulseekClient.Options.DistributedConnectionOptions,
                    incomingConnection.HandoffTcpClient());

                Diagnostic.Debug($"Indirect parent candidate connection to {username} ({incomingConnection.IPEndPoint}) handed off. (old: {incomingConnection.Id}, new: {connection.Id})");

                connection.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect;
                connection.Disconnected += ParentCandidateConnection_Disconnected;

                Diagnostic.Debug($"Indirect parent candidate connection to {username} ({connection.IPEndPoint}) established. (type: {connection.Type}, id: {connection.Id})");
                return connection;
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to establish an indirect parent candidate connection to {username} with token {solicitationToken}: {ex.Message}");
                throw;
            }
            finally
            {
                PendingSolicitationDictionary.TryRemove(solicitationToken, out var _);
            }
        }

        private void ParentCandidateConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;

            Diagnostic.Debug($"Parent candidate connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");

            connection.Dispose();
        }

        private async void ParentConnection_Disconnected(object sender, ConnectionDisconnectedEventArgs e)
        {
            var connection = (IMessageConnection)sender;

            Diagnostic.Debug($"Parent connection to {connection.Username} ({connection.IPEndPoint}) disconnected: {e.Message} (type: {connection.Type}, id: {connection.Id})");
            Diagnostic.Info($"Parent connection to {connection.Username} ({connection.IPEndPoint}) disconnected{(e.Message == null ? "." : $": {e.Message}")}.");
            ParentDisconnected?.Invoke(this, new DistributedParentEventArgs(connection.Username, connection.IPEndPoint, ParentBranchLevel, ParentBranchRoot));

            ParentConnection = null;
            ParentBranchLevel = 0;
            ParentBranchRoot = string.Empty;

            StateChanged?.Invoke(this, DistributedNetworkInfo.FromDistributedConnectionManager(this));

            connection.Dispose();

            try
            {
                await AddParentConnectionAsync(ParentCandidateList).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // noop
            }
        }

        private async Task UpdateStatusEventuallyAsync()
        {
            if (StatusDebounceTimer.Enabled && LastStatusTimestamp.AddMilliseconds(StatusAgeLimit) <= DateTime.UtcNow)
            {
                Diagnostic.Debug($"Distributed status age exceeds limit of {StatusAgeLimit}ms, forcing an update");
                await UpdateStatusAsync().ConfigureAwait(false);
            }

            StatusDebounceTimer.Reset();
        }

        private void WaitForParentCandidateConnection_MessageRead(object sender, MessageEventArgs e)
        {
            var conn = (IMessageConnection)sender;

            try
            {
                var code = new MessageReader<MessageCode.Distributed>(e.Message).ReadCode();

                switch (code)
                {
                    case MessageCode.Distributed.EmbeddedMessage:
                        var embeddedMessage = EmbeddedMessage.FromByteArray(e.Message);
                        if (embeddedMessage.DistributedCode == MessageCode.Distributed.SearchRequest)
                        {
                            SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Id));
                        }

                        break;

                    case MessageCode.Distributed.SearchRequest:
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Id));
                        break;

                    case MessageCode.Distributed.BranchLevel:
                        var branchLevel = DistributedBranchLevel.FromByteArray(e.Message);
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Id), branchLevel.Level);
                        break;

                    case MessageCode.Distributed.BranchRoot:
                        var branchRoot = DistributedBranchRoot.FromByteArray(e.Message);
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Id), branchRoot.Username);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to handle message from parent candidate: {ex.Message}", ex);
                conn.Disconnect(ex.Message);
                conn.Dispose();
            }
        }

        private async Task<(int BranchLevel, string BranchRoot)> WaitForParentCandidateConnectionInitializationAsync(IMessageConnection connection, CancellationToken cancellationToken)
        {
            connection.MessageRead += WaitForParentCandidateConnection_MessageRead;

            var branchLevelWait = SoulseekClient.Waiter.Wait<int>(new WaitKey(Constants.WaitKey.BranchLevelMessage, connection.Id), cancellationToken: cancellationToken);
            var branchRootWait = SoulseekClient.Waiter.Wait<string>(new WaitKey(Constants.WaitKey.BranchRootMessage, connection.Id), cancellationToken: cancellationToken);
            var searchWait = SoulseekClient.Waiter.Wait(new WaitKey(Constants.WaitKey.SearchRequestMessage, connection.Id), cancellationToken: cancellationToken);

            // wait for the branch level and first search request. branch roots will not send the root.
            var waits = new[] { branchLevelWait, searchWait }.ToList();
            var waitsTask = Task.WhenAll(waits);

            try
            {
                int branchLevel;
                string branchRoot;

                await waitsTask.ConfigureAwait(false);

                branchLevel = await branchLevelWait.ConfigureAwait(false);

                // if we didn't connect to a root, ensure we get the name of the root.
                if (branchLevel > 0)
                {
                    branchRoot = await branchRootWait.ConfigureAwait(false);
                }
                else
                {
                    Diagnostic.Debug($"Received branch level 0 from parent candidate {connection.Username}; this user is a branch root.");
                    branchRoot = connection.Username;
                }

                await searchWait.ConfigureAwait(false);

                return (branchLevel, branchRoot);
            }
            catch (Exception)
            {
                connection.Disconnect("One or more required messages was not received.");
                throw new ConnectionException($"Failed to retrieve branch info from parent candidate connection to {connection.Username} ({connection.IPEndPoint}); one or more required messages was not received. (id: {connection.Id})");
            }
            finally
            {
                connection.MessageRead -= WaitForParentCandidateConnection_MessageRead;
            }
        }

        private void WatchdogTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Enabled && !HasParent && !IsBranchRoot && SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) && SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                Diagnostic.Warning("No distributed parent connected.  Requesting a list of candidates.");
                UpdateStatusAsync().ConfigureAwait(false);
            }
        }
    }
}