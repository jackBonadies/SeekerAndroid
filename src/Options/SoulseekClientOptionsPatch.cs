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
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     A patch for SoulseekClientOptions.
    /// </summary>
    public class SoulseekClientOptionsPatch
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientOptionsPatch"/> class.
        /// </summary>
        /// <param name="enableListener">A value indicating whether to listen for incoming connections.</param>
        /// <param name="listenIPAddress">The IP address on which to listen for incoming connections.</param>
        /// <param name="listenPort">The port on which to listen for incoming connections.</param>
        /// <param name="enableDistributedNetwork">A value indicating whether to establish distributed network connections.</param>
        /// <param name="acceptDistributedChildren">A value indicating whether to accept distributed child connections.</param>
        /// <param name="distributedChildLimit">The number of allowed distributed children.</param>
        /// <param name="maximumUploadSpeed">The total maximum allowable upload speed, in kibibytes per second.</param>
        /// <param name="maximumDownloadSpeed">The total maximum allowable download speed, in kibibytes per second.</param>
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
        /// <param name="userEndPointCache">The user endpoint cache to use when resolving user endpoints.</param>
        /// <param name="searchResponseResolver">
        ///     The delegate used to resolve the <see cref="SearchResponse"/> for an incoming <see cref="SearchRequest"/>.
        /// </param>
        /// <param name="searchResponseCache">
        ///     The search response cache to use when a response is not able to be delivered immediately.
        /// </param>
        /// <param name="browseResponseResolver">
        ///     The delegate used to resolve the <see cref="BrowseResponse"/> for an incoming <see cref="BrowseRequest"/>.
        /// </param>
        /// <param name="directoryContentsResolver">
        ///     The delegate used to resolve the list of <see cref="Directory"/> for an incoming <see cref="FolderContentsRequest"/>.
        /// </param>
        /// <param name="userInfoResolver">The delegate used to resolve the <see cref="UserInfo"/> for an incoming <see cref="UserInfoRequest"/>.</param>
        /// <param name="enqueueDownload">The delegate invoked upon an receipt of an incoming <see cref="QueueDownloadRequest"/>.</param>
        /// <param name="placeInQueueResolver">
        ///     The delegate used to resolve the <see cref="int"/> response for an incoming request.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value supplied for <paramref name="listenPort"/> is not between 1024 and 65535.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value supplied for <paramref name="distributedChildLimit"/> is less than zero.
        /// </exception>
        public SoulseekClientOptionsPatch(
            bool? enableListener = null,
            IPAddress listenIPAddress = null,
            int? listenPort = null,
            bool? enableDistributedNetwork = null,
            bool? acceptDistributedChildren = null,
            int? distributedChildLimit = null,
            int? maximumUploadSpeed = null,
            int? maximumDownloadSpeed = null,
            bool? deduplicateSearchRequests = null,
            bool? autoAcknowledgePrivateMessages = null,
            bool? autoAcknowledgePrivilegeNotifications = null,
            bool? acceptPrivateRoomInvitations = null,
            ConnectionOptions serverConnectionOptions = null,
            ConnectionOptions peerConnectionOptions = null,
            ConnectionOptions transferConnectionOptions = null,
            ConnectionOptions incomingConnectionOptions = null,
            ConnectionOptions distributedConnectionOptions = null,
            IUserEndPointCache userEndPointCache = null,
            Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null,
            ISearchResponseCache searchResponseCache = null,
            Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null,
            Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> directoryContentsResolver = null,
            Func<string, IPEndPoint, Task<UserInfo>> userInfoResolver = null,
            Func<string, IPEndPoint, string, Task> enqueueDownload = null,
            Func<string, IPEndPoint, string, Task<int?>> placeInQueueResolver = null)
        {
            EnableListener = enableListener;
            ListenIPAddress = listenIPAddress;

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

            MaximumUploadSpeed = maximumUploadSpeed;
            MaximumDownloadSpeed = maximumDownloadSpeed;

            DeduplicateSearchRequests = deduplicateSearchRequests;

            AutoAcknowledgePrivateMessages = autoAcknowledgePrivateMessages;
            AutoAcknowledgePrivilegeNotifications = autoAcknowledgePrivilegeNotifications;
            AcceptPrivateRoomInvitations = acceptPrivateRoomInvitations;

            ServerConnectionOptions = serverConnectionOptions?.WithoutInactivityTimeout();
            PeerConnectionOptions = peerConnectionOptions;
            TransferConnectionOptions = transferConnectionOptions;
            IncomingConnectionOptions = incomingConnectionOptions;
            DistributedConnectionOptions = distributedConnectionOptions;

            UserEndPointCache = userEndPointCache;

            SearchResponseResolver = searchResponseResolver;
            SearchResponseCache = searchResponseCache;

            BrowseResponseResolver = browseResponseResolver;
            DirectoryContentsResolver = directoryContentsResolver;

            UserInfoResolver = userInfoResolver;
            EnqueueDownload = enqueueDownload;
            PlaceInQueueResolver = placeInQueueResolver;
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
        ///     Gets the delegate used to resolve the response for an incoming browse request.
        /// </summary>
        public Func<string, IPEndPoint, Task<BrowseResponse>> BrowseResponseResolver { get; }

        /// <summary>
        ///     Gets a value indicating whether duplicated distributed search requests should be discarded.
        /// </summary>
        public bool? DeduplicateSearchRequests { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the response for an incoming directory contents request.
        /// </summary>
        public Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>> DirectoryContentsResolver { get; }

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
        ///     Gets a value indicating whether to listen for incoming connections.
        /// </summary>
        public bool? EnableListener { get; }

        /// <summary>
        ///     Gets the delegate invoked upon an receipt of an incoming <see cref="QueueDownloadRequest"/>.
        /// </summary>
        /// <remarks>
        ///     This delegate must throw an Exception to indicate a rejected download. If the thrown Exception is of type
        ///     <see cref="DownloadEnqueueException"/> the message will be sent to the client, otherwise a default message will be sent.
        /// </remarks>
        public Func<string, IPEndPoint, string, Task> EnqueueDownload { get; }

        /// <summary>
        ///     Gets the options for incoming connections.
        /// </summary>
        public ConnectionOptions IncomingConnectionOptions { get; }

        /// <summary>
        ///     Gets the IP address on which to listen for incoming connections. (Default = IPAddress.Any/"0.0.0.0").
        /// </summary>
        public IPAddress ListenIPAddress { get; }

        /// <summary>
        ///     Gets the port on which to listen for incoming connections.
        /// </summary>
        public int? ListenPort { get; }

        /// <summary>
        ///     Gets the total maximum allowable download speed, in kibibytes per second.
        /// </summary>
        public int? MaximumDownloadSpeed { get; }

        /// <summary>
        ///     Gets the total maximum allowable upload speed, in kibibytes per second.
        /// </summary>
        public int? MaximumUploadSpeed { get; }

        /// <summary>
        ///     Gets the options for peer message connections.
        /// </summary>
        public ConnectionOptions PeerConnectionOptions { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="PlaceInQueueResponse"/> for an incoming request.
        /// </summary>
        public Func<string, IPEndPoint, string, Task<int?>> PlaceInQueueResolver { get; }

        /// <summary>
        ///     Gets the search response cache to use when a response is not able to be delivered immediately.
        /// </summary>
        public ISearchResponseCache SearchResponseCache { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="SearchResponse"/> for an incoming request. (Default = do not respond).
        /// </summary>
        public Func<string, int, SearchQuery, Task<SearchResponse>> SearchResponseResolver { get; }

        /// <summary>
        ///     Gets the options for the server message connection.
        /// </summary>
        public ConnectionOptions ServerConnectionOptions { get; }

        /// <summary>
        ///     Gets the options for peer transfer connections.
        /// </summary>
        public ConnectionOptions TransferConnectionOptions { get; }

        /// <summary>
        ///     Gets the user endpoint cache to use when resolving user endpoints.
        /// </summary>
        public IUserEndPointCache UserEndPointCache { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="UserInfo"/> for an incoming request.
        /// </summary>
        public Func<string, IPEndPoint, Task<UserInfo>> UserInfoResolver { get; }
    }
}
