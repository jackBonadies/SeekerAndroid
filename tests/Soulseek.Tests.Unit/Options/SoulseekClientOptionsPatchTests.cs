// <copyright file="SoulseekClientOptionsPatchTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Options
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Xunit;

    public class SoulseekClientOptionsPatchTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiation(
            bool? enableListener,
            bool? enableDistributedNetwork,
            bool? acceptDistributedChildren,
            int? distributedChildLimit,
            int? maximumUploadSpeed,
            int? maximumDownloadSpeed,
            bool? deduplicateSearchRequests,
            bool? autoAcknowledgePrivateMessages,
            bool? autoAcknowledgePrivilegeNotifications,
            bool? acceptPrivateRoomInvitations)
        {
            var serverConnectionOptions = new ConnectionOptions();
            var peerConnectionOptions = new ConnectionOptions();
            var transferConnectionOptions = new ConnectionOptions();
            var incomingConnectionOptions = new ConnectionOptions();
            var distributedConnectionOptions = new ConnectionOptions();

            var userEndPointCache = new Mock<IUserEndPointCache>();
            var searchResponseCache = new Mock<ISearchResponseCache>();

            var searchResponseResolver = new Func<string, int, SearchQuery, Task<SearchResponse>>((s, i, q) => Task.FromResult<SearchResponse>(null));
            var browseResponseResolver = new Func<string, IPEndPoint, Task<BrowseResponse>>((s, i) => Task.FromResult<BrowseResponse>(null));
            var directoryContentsResponseResolver = new Func<string, IPEndPoint, int, string, Task<IEnumerable<Directory>>>((s, i, ii, ss) => Task.FromResult<IEnumerable<Directory>>(null));
            var userInfoResponseResolver = new Func<string, IPEndPoint, Task<UserInfo>>((s, i) => Task.FromResult<UserInfo>(null));
            var enqueueDownloadAction = new Func<string, IPEndPoint, string, Task>((s, i, ss) => Task.CompletedTask);
            var placeInQueueResponseResolver = new Func<string, IPEndPoint, string, Task<int?>>((s, i, ss) => Task.FromResult<int?>(0));

            var rnd = new Random();
            var listenAddress = IPAddress.Parse(string.Join(".", rnd.Next(0, 254).ToString(), rnd.Next(0, 254).ToString(), rnd.Next(0, 254).ToString(), rnd.Next(0, 254).ToString()));
            var listenPort = rnd.Next(1024, 65535);

            var o = new SoulseekClientOptionsPatch(
                enableListener,
                listenAddress,
                listenPort,
                enableDistributedNetwork: enableDistributedNetwork,
                acceptDistributedChildren: acceptDistributedChildren,
                distributedChildLimit: distributedChildLimit,
                maximumUploadSpeed: maximumUploadSpeed,
                maximumDownloadSpeed: maximumDownloadSpeed,
                deduplicateSearchRequests: deduplicateSearchRequests,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                autoAcknowledgePrivilegeNotifications: autoAcknowledgePrivilegeNotifications,
                acceptPrivateRoomInvitations: acceptPrivateRoomInvitations,
                serverConnectionOptions: serverConnectionOptions,
                peerConnectionOptions: peerConnectionOptions,
                transferConnectionOptions: transferConnectionOptions,
                incomingConnectionOptions: incomingConnectionOptions,
                distributedConnectionOptions: distributedConnectionOptions,
                userEndPointCache: userEndPointCache.Object,
                searchResponseResolver: searchResponseResolver,
                searchResponseCache: searchResponseCache.Object,
                browseResponseResolver: browseResponseResolver,
                directoryContentsResolver: directoryContentsResponseResolver,
                userInfoResolver: userInfoResponseResolver,
                enqueueDownload: enqueueDownloadAction,
                placeInQueueResolver: placeInQueueResponseResolver);

            Assert.Equal(enableListener, o.EnableListener);
            Assert.Equal(listenAddress, o.ListenIPAddress);
            Assert.Equal(listenPort, o.ListenPort);
            Assert.Equal(enableDistributedNetwork, o.EnableDistributedNetwork);
            Assert.Equal(acceptDistributedChildren, o.AcceptDistributedChildren);
            Assert.Equal(distributedChildLimit, o.DistributedChildLimit);
            Assert.Equal(maximumUploadSpeed, o.MaximumUploadSpeed);
            Assert.Equal(maximumDownloadSpeed, o.MaximumDownloadSpeed);
            Assert.Equal(deduplicateSearchRequests, o.DeduplicateSearchRequests);
            Assert.Equal(autoAcknowledgePrivateMessages, o.AutoAcknowledgePrivateMessages);
            Assert.Equal(autoAcknowledgePrivilegeNotifications, o.AutoAcknowledgePrivilegeNotifications);
            Assert.Equal(acceptPrivateRoomInvitations, o.AcceptPrivateRoomInvitations);
            Assert.Equal(peerConnectionOptions, o.PeerConnectionOptions);
            Assert.Equal(incomingConnectionOptions, o.IncomingConnectionOptions);
            Assert.Equal(distributedConnectionOptions, o.DistributedConnectionOptions);

            Assert.Equal(serverConnectionOptions.ReadBufferSize, o.ServerConnectionOptions.ReadBufferSize);
            Assert.Equal(serverConnectionOptions.WriteBufferSize, o.ServerConnectionOptions.WriteBufferSize);
            Assert.Equal(serverConnectionOptions.ConnectTimeout, o.ServerConnectionOptions.ConnectTimeout);
            Assert.Equal(-1, o.ServerConnectionOptions.InactivityTimeout);

            Assert.Equal(transferConnectionOptions.ReadBufferSize, o.TransferConnectionOptions.ReadBufferSize);
            Assert.Equal(transferConnectionOptions.WriteBufferSize, o.TransferConnectionOptions.WriteBufferSize);
            Assert.Equal(transferConnectionOptions.ConnectTimeout, o.TransferConnectionOptions.ConnectTimeout);
            Assert.Equal(transferConnectionOptions.InactivityTimeout, o.TransferConnectionOptions.InactivityTimeout);

            Assert.Equal(userEndPointCache.Object, o.UserEndPointCache);
            Assert.Equal(searchResponseResolver, o.SearchResponseResolver);
            Assert.Equal(searchResponseCache.Object, o.SearchResponseCache);
            Assert.Equal(browseResponseResolver, o.BrowseResponseResolver);
            Assert.Equal(directoryContentsResponseResolver, o.DirectoryContentsResolver);
            Assert.Equal(userInfoResponseResolver, o.UserInfoResolver);
            Assert.Equal(enqueueDownloadAction, o.EnqueueDownload);
            Assert.Equal(placeInQueueResponseResolver, o.PlaceInQueueResolver);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given data")]
        public void Removes_Timeout_On_Server_Optons()
        {
            var serverConnectionOptions = new ConnectionOptions();
            var transferConnectionOptions = new ConnectionOptions();

            var o = new SoulseekClientOptionsPatch(
                serverConnectionOptions: serverConnectionOptions,
                transferConnectionOptions: transferConnectionOptions);

            Assert.Equal(-1, o.ServerConnectionOptions.InactivityTimeout);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given data")]
        public void Does_Not_Remove_Timeout_On_Transfer_Options()
        {
            var serverConnectionOptions = new ConnectionOptions();
            var transferConnectionOptions = new ConnectionOptions();

            var o = new SoulseekClientOptionsPatch(
                serverConnectionOptions: serverConnectionOptions,
                transferConnectionOptions: transferConnectionOptions);

            Assert.Equal(transferConnectionOptions.InactivityTimeout, o.TransferConnectionOptions.InactivityTimeout);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Does not throw if server and transfer options not given")]
        public void Does_Not_Throw_If_Server_And_Transfer_Options_Not_Given()
        {
            var ex = Record.Exception(() => new SoulseekClientOptionsPatch());

            Assert.Null(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if distributed child limit is less than zero")]
        public void Throws_If_Distributed_Child_Limit_Is_Less_Than_Zero()
        {
            SoulseekClientOptionsPatch x;
            var ex = Record.Exception(() => x = new SoulseekClientOptionsPatch(distributedChildLimit: -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if listen port is too high")]
        public void Throws_If_Listen_Port_Is_Too_High()
        {
            SoulseekClientOptionsPatch x;
            var ex = Record.Exception(() => x = new SoulseekClientOptionsPatch(listenPort: 999999999));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if listen port is too low")]
        public void Throws_If_Listen_Port_Is_Too_Low()
        {
            SoulseekClientOptionsPatch x;
            var ex = Record.Exception(() => x = new SoulseekClientOptionsPatch(listenPort: 1023));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }
    }
}
