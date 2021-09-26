// <copyright file="SoulseekClientOptionsPatch.cs" company="JP Dillingham">
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
    using System.Net;

    /// <summary>
    ///     A patch for SoulseekClientOptions.
    /// </summary>
    public class SoulseekClientOptionsPatch
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientOptionsPatch"/> class.
        /// </summary>
        /// <param name="enableListener">A value indicating whether to listen for incoming connections.</param>
        /// <param name="listenPort">The port on which to listen for incoming connections.</param>
        /// <param name="enableDistributedNetwork">A value indicating whether to establish distributed network connections.</param>
        /// <param name="acceptDistributedChildren">A value indicating whether to accept distributed child connections.</param>
        /// <param name="distributedChildLimit">The number of allowed distributed children.</param>
        /// <param name="deduplicateSearchRequests">
        ///     A value indicating whether duplicated distributed search requests should be discarded.
        /// </param>
        /// <param name="autoAcknowledgePrivateMessages">
        ///     A value indicating whether to automatically send a private message acknowledgement upon receipt.
        /// </param>
        /// <param name="autoAcknowledgePrivilegeNotifications">
        ///     A value indicating whether to automatically send a privilege notification acknowledgement upon receipt.
        /// </param>
        /// <param name="acceptPrivateRoomInvitations">A value indicating whether to accept private room invitations.</param>
        /// <param name="serverConnectionOptions">The options for the server message connection.</param>
        /// <param name="peerConnectionOptions">The options for peer message connections.</param>
        /// <param name="transferConnectionOptions">The options for peer transfer connections.</param>
        /// <param name="incomingConnectionOptions">The options for incoming connections.</param>
        /// <param name="distributedConnectionOptions">The options for distributed message connections.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value supplied for <paramref name="listenPort"/> is not between 1024 and 65535.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value supplied for <paramref name="distributedChildLimit"/> is less than zero.
        /// </exception>
        public SoulseekClientOptionsPatch(
            bool? enableListener = null,
            int? listenPort = null,
            bool? enableDistributedNetwork = null,
            bool? acceptDistributedChildren = null,
            int? distributedChildLimit = null,
            bool? deduplicateSearchRequests = null,
            bool? autoAcknowledgePrivateMessages = null,
            bool? autoAcknowledgePrivilegeNotifications = null,
            bool? acceptPrivateRoomInvitations = null,
            ConnectionOptions serverConnectionOptions = null,
            ConnectionOptions peerConnectionOptions = null,
            ConnectionOptions transferConnectionOptions = null,
            ConnectionOptions incomingConnectionOptions = null,
            ConnectionOptions distributedConnectionOptions = null)
        {
            EnableListener = enableListener;
            ListenPort = listenPort;

            if (ListenPort < 1024 || ListenPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(listenPort), "Must be between 1024 and 65535");
            }

            EnableDistributedNetwork = enableDistributedNetwork;
            AcceptDistributedChildren = acceptDistributedChildren;
            DistributedChildLimit = distributedChildLimit;

            if (DistributedChildLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distributedChildLimit), "Must be greater than or equal to zero");
            }

            DeduplicateSearchRequests = deduplicateSearchRequests;

            AutoAcknowledgePrivateMessages = autoAcknowledgePrivateMessages;
            AutoAcknowledgePrivilegeNotifications = autoAcknowledgePrivilegeNotifications;
            AcceptPrivateRoomInvitations = acceptPrivateRoomInvitations;

            ServerConnectionOptions = serverConnectionOptions?.WithoutInactivityTimeout();
            PeerConnectionOptions = peerConnectionOptions;
            TransferConnectionOptions = transferConnectionOptions?.WithoutInactivityTimeout();
            IncomingConnectionOptions = incomingConnectionOptions;
            DistributedConnectionOptions = distributedConnectionOptions;
        }

        /// <summary>
        ///     Gets a value indicating whether to accept distributed child connections.
        /// </summary>
        public bool? AcceptDistributedChildren { get; }

        /// <summary>
        ///     Gets a value indicating whether to accept private room invitations.
        /// </summary>
        public bool? AcceptPrivateRoomInvitations { get; }

        /// <summary>
        ///     Gets a value indicating whether to automatically send a private message acknowledgement upon receipt.
        /// </summary>
        public bool? AutoAcknowledgePrivateMessages { get; }

        /// <summary>
        ///     Gets a value indicating whether to automatically send a privilege notification acknowledgement upon receipt.
        /// </summary>
        public bool? AutoAcknowledgePrivilegeNotifications { get; }

        /// <summary>
        ///     Gets a value indicating whether duplicated distributed search requests should be discarded.
        /// </summary>
        public bool? DeduplicateSearchRequests { get; }

        /// <summary>
        ///     Gets the number of allowed distributed children.
        /// </summary>
        public int? DistributedChildLimit { get; }

        /// <summary>
        ///     Gets the options for distributed message connections.
        /// </summary>
        public ConnectionOptions DistributedConnectionOptions { get; }

        /// <summary>
        ///     Gets a value indicating whether to establish distributed network connections.
        /// </summary>
        public bool? EnableDistributedNetwork { get; }

        /// <summary>
        ///     Gets a value indicating whether to listen for incoming connections. (Default = true).
        /// </summary>
        public bool? EnableListener { get; }

        /// <summary>
        ///     Gets the options for incoming connections.
        /// </summary>
        public ConnectionOptions IncomingConnectionOptions { get; }

        /// <summary>
        ///     Gets the port on which to listen for incoming connections.
        /// </summary>
        public int? ListenPort { get; }

        /// <summary>
        ///     Gets the options for peer message connections.
        /// </summary>
        public ConnectionOptions PeerConnectionOptions { get; }

        /// <summary>
        ///     Gets the options for the server message connection.
        /// </summary>
        public ConnectionOptions ServerConnectionOptions { get; }

        /// <summary>
        ///     Gets the options for peer transfer connections.
        /// </summary>
        public ConnectionOptions TransferConnectionOptions { get; }
    }
}