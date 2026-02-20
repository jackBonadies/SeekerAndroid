// <copyright file="IDistributedConnectionManager.cs" company="JP Dillingham">
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
    ///     Manages distributed <see cref="IMessageConnection"/> instances for the application.
    /// </summary>
    internal interface IDistributedConnectionManager : IDisposable, IDiagnosticGenerator
    {
        /// <summary>
        ///     Occurs when a child connection is added.
        /// </summary>
        event EventHandler<DistributedChildEventArgs> ChildAdded;

        /// <summary>
        ///     Occurs when a child connection is disconnected.
        /// </summary>
        event EventHandler<DistributedChildEventArgs> ChildDisconnected;

        /// <summary>
        ///     Occurs when the client is demoted from a branch root on the distributed network.
        /// </summary>
        event EventHandler DemotedFromBranchRoot;

        /// <summary>
        ///     Occurs when a new parent is adopted.
        /// </summary>
        event EventHandler<DistributedParentEventArgs> ParentAdopted;

        /// <summary>
        ///     Occurs when the parent is disconnected.
        /// </summary>
        event EventHandler<DistributedParentEventArgs> ParentDisconnected;

        /// <summary>
        ///     Occurs when the client has been promoted to a branch root on the distributed network.
        /// </summary>
        event EventHandler PromotedToBranchRoot;

        /// <summary>
        ///     Occurs when the state of the distributed network changes.
        /// </summary>
        event EventHandler<DistributedNetworkInfo> StateChanged;

        /// <summary>
        ///     Gets the average child broadcast latency.
        /// </summary>
        double? AverageBroadcastLatency { get; }

        /// <summary>
        ///     Gets the current distributed branch level.
        /// </summary>
        int BranchLevel { get; }

        /// <summary>
        ///     Gets the current distributed branch root.
        /// </summary>
        string BranchRoot { get; }

        /// <summary>
        ///     Gets a value indicating whether child connections can be accepted.
        /// </summary>
        bool CanAcceptChildren { get; }

        /// <summary>
        ///     Gets the number of allowed concurrent child connections.
        /// </summary>
        int ChildLimit { get; }

        /// <summary>
        ///     Gets the current list of child connections.
        /// </summary>
        IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children { get; }

        /// <summary>
        ///     Gets a value indicating whether a parent connection is established.
        /// </summary>
        bool HasParent { get; }

        /// <summary>
        ///     Gets a value indicating whether the client is currently operating as a branch root.
        /// </summary>
        bool IsBranchRoot { get; }

        /// <summary>
        ///     Gets the current parent connection.
        /// </summary>
        (string Username, IPEndPoint IPEndPoint) Parent { get; }

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        IReadOnlyDictionary<int, string> PendingSolicitations { get; }

        /// <summary>
        ///     Adds a new child connection from an incoming connection.
        /// </summary>
        /// <param name="username">The username from which the connection originated.</param>
        /// <param name="incomingConnection">The accepted connection.</param>
        /// <returns>The operation context.</returns>
        Task AddOrUpdateChildConnectionAsync(string username, IConnection incomingConnection);

        /// <summary>
        ///     Asynchronously connects to one of the specified <paramref name="parentCandidates"/>.
        /// </summary>
        /// <param name="parentCandidates">The list of parent connection candidates provided by the server.</param>
        /// <returns>The operation context.</returns>
        Task AddParentConnectionAsync(IEnumerable<(string Username, IPEndPoint IPEndPoint)> parentCandidates);

        /// <summary>
        ///     Asynchronously writes the specified bytes to each of the connected child connections.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context.</returns>
        Task BroadcastMessageAsync(byte[] bytes, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Demotes the client from a branch root on the distributed network.
        /// </summary>
        void DemoteFromBranchRoot();

        /// <summary>
        ///     Adds a new child connection using the details in the specified <paramref name="connectToPeerResponse"/> and
        ///     pierces the remote peer's firewall.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context.</returns>
        Task GetOrAddChildConnectionAsync(ConnectToPeerResponse connectToPeerResponse);

        /// <summary>
        ///     Promotes the client to a branch root on the distributed network.
        /// </summary>
        void PromoteToBranchRoot();

        /// <summary>
        ///     Removes and disposes the parent and all child connections.
        /// </summary>
        void RemoveAndDisposeAll();

        /// <summary>
        ///     Resets stored state information about the distributed network.
        /// </summary>
        void ResetStatus();

        /// <summary>
        ///     Sets the distributed <paramref name="branchLevel"/>.
        /// </summary>
        /// <param name="branchLevel">The distributed branch level.</param>
        void SetParentBranchLevel(int branchLevel);

        /// <summary>
        ///     Sets the distributed <paramref name="branchRoot"/>.
        /// </summary>
        /// <param name="branchRoot">The distributed branch root.</param>
        void SetParentBranchRoot(string branchRoot);

        /// <summary>
        ///     Updates the server with the current status of the distributed network.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context.</returns>
        Task UpdateStatusAsync(CancellationToken? cancellationToken = null);
    }
}