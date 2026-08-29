// <copyright file="DistributedNetworkInfo.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Soulseek.Network;

    /// <summary>
    ///     Information about the distributed network.
    /// </summary>
    public class DistributedNetworkInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedNetworkInfo"/> class.
        /// </summary>
        /// <param name="averageBroadcastLatency">The average child broadcast latency.</param>
        /// <param name="branchLevel">The current distributed branch level.</param>
        /// <param name="branchRoot">The current distributed branch root.</param>
        /// <param name="isBranchRoot">A value indicating whether the client is currently operating as a branch root.</param>
        /// <param name="childLimit">The number of allowed concurrent child connections.</param>
        /// <param name="canAcceptChildren">A value indicating whether child connections can be accepted.</param>
        /// <param name="children">The current list of child connections.</param>
        /// <param name="parent">The current parent connection.</param>
        /// <param name="hasParent">A value indicating whether a parent connection is established.</param>
        public DistributedNetworkInfo(
            double? averageBroadcastLatency,
            int branchLevel,
            string branchRoot,
            bool isBranchRoot,
            int childLimit,
            bool canAcceptChildren,
            IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> children,
            (string Username, IPEndPoint IPEndPoint) parent,
            bool hasParent)
        {
            AverageBroadcastLatency = averageBroadcastLatency;
            BranchLevel = branchLevel;
            BranchRoot = branchRoot;
            CanAcceptChildren = canAcceptChildren;
            ChildLimit = childLimit;
            Children = children?.ToList().AsReadOnly();
            HasParent = hasParent;
            IsBranchRoot = isBranchRoot;
            Parent = parent;
        }

        /// <summary>
        ///     Gets the average child broadcast latency.
        /// </summary>
        public double? AverageBroadcastLatency { get; }

        /// <summary>
        ///     Gets the current distributed branch level.
        /// </summary>
        public int BranchLevel { get; }

        /// <summary>
        ///     Gets the current distributed branch root.
        /// </summary>
        public string BranchRoot { get; }

        /// <summary>
        ///     Gets a value indicating whether child connections can be accepted.
        /// </summary>
        public bool CanAcceptChildren { get; }

        /// <summary>
        ///     Gets the number of allowed concurrent child connections.
        /// </summary>
        public int ChildLimit { get; }

        /// <summary>
        ///     Gets the current list of child connections.
        /// </summary>
        public IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)> Children { get; }

        /// <summary>
        ///     Gets a value indicating whether a parent connection is established.
        /// </summary>
        public bool HasParent { get; }

        /// <summary>
        ///     Gets a value indicating whether the client is currently operating as a branch root.
        /// </summary>
        public bool IsBranchRoot { get; }

        /// <summary>
        ///     Gets the current parent connection.
        /// </summary>
        public (string Username, IPEndPoint IPEndPoint) Parent { get; }

        /// <summary>
        ///     Derives <see cref="DistributedNetworkInfo"/> from the internal <see cref="IDistributedConnectionManager"/>.
        /// </summary>
        /// <param name="manager">The manager instance from which to derive info.</param>
        /// <returns>The derived info.</returns>
        internal static DistributedNetworkInfo FromDistributedConnectionManager(IDistributedConnectionManager manager)
            => new DistributedNetworkInfo(
                averageBroadcastLatency: manager.AverageBroadcastLatency,
                branchLevel: manager.BranchLevel,
                branchRoot: manager.BranchRoot,
                isBranchRoot: manager.IsBranchRoot,
                childLimit: manager.ChildLimit,
                canAcceptChildren: manager.CanAcceptChildren,
                children: manager.Children,
                parent: manager.Parent,
                hasParent: manager.HasParent);
    }
}