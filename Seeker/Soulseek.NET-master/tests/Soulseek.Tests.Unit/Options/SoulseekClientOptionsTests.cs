// <copyright file="SoulseekClientOptionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Soulseek.Diagnostics;
    using Xunit;

    public class SoulseekClientOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiation(
            bool enableListener,
            bool enableDistributedNetwork,
            bool acceptDistributedChildren,
            int distributedChildLimit,
            bool deduplicateSearchRequests,
            int messageTimeout,
            bool autoAcknowledgePrivateMessages,
            bool autoAcknowledgePrivilegeNotifications,
            bool acceptPrivateRoomInvitations,
            DiagnosticLevel minimumDiagnosticLevel,
            int startingToken)
        {
            var serverConnectionOptions = new ConnectionOptions();
            var peerConnectionOptions = new ConnectionOptions();
            var transferConnectionOptions = new ConnectionOptions();
            var incomingConnectionOptions = new ConnectionOptions();
            var distributedConnectionOptions = new ConnectionOptions();

            var rnd = new Random();
            var listenPort = rnd.Next(1024, 65535);

            var o = new SoulseekClientOptions(
                enableListener,
                listenPort,
                userEndPointCache: null,
                enableDistributedNetwork: enableDistributedNetwork,
                acceptDistributedChildren: acceptDistributedChildren,
                distributedChildLimit: distributedChildLimit,
                deduplicateSearchRequests: deduplicateSearchRequests,
                messageTimeout: messageTimeout,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                autoAcknowledgePrivilegeNotifications: autoAcknowledgePrivilegeNotifications,
                acceptPrivateRoomInvitations: acceptPrivateRoomInvitations,
                minimumDiagnosticLevel: minimumDiagnosticLevel,
                startingToken: startingToken,
                serverConnectionOptions: serverConnectionOptions,
                peerConnectionOptions: peerConnectionOptions,
                transferConnectionOptions: transferConnectionOptions,
                incomingConnectionOptions: incomingConnectionOptions,
                distributedConnectionOptions: distributedConnectionOptions);

            Assert.Equal(enableListener, o.EnableListener);
            Assert.Equal(listenPort, o.ListenPort);
            Assert.Null(o.UserEndPointCache);
            Assert.Equal(enableDistributedNetwork, o.EnableDistributedNetwork);
            Assert.Equal(acceptDistributedChildren, o.AcceptDistributedChildren);
            Assert.Equal(distributedChildLimit, o.DistributedChildLimit);
            Assert.Equal(deduplicateSearchRequests, o.DeduplicateSearchRequests);
            Assert.Equal(messageTimeout, o.MessageTimeout);
            Assert.Equal(autoAcknowledgePrivateMessages, o.AutoAcknowledgePrivateMessages);
            Assert.Equal(autoAcknowledgePrivilegeNotifications, o.AutoAcknowledgePrivilegeNotifications);
            Assert.Equal(acceptPrivateRoomInvitations, o.AcceptPrivateRoomInvitations);
            Assert.Equal(minimumDiagnosticLevel, o.MinimumDiagnosticLevel);
            Assert.Equal(startingToken, o.StartingToken);
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
            Assert.Equal(-1, o.TransferConnectionOptions.InactivityTimeout);

            Assert.Null(o.UserEndPointCache);
            Assert.Null(o.SearchResponseCache);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with default options if null"), AutoData]
        public void Instantiation_Defaults_Options_If_Null(
            int messageTimeout,
            bool autoAcknowledgePrivateMessages,
            DiagnosticLevel minimumDiagnosticLevel,
            int startingToken)
        {
            var o = new SoulseekClientOptions(
                messageTimeout: messageTimeout,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                minimumDiagnosticLevel: minimumDiagnosticLevel,
                startingToken: startingToken);

            Assert.NotNull(o.ServerConnectionOptions);
            Assert.NotNull(o.PeerConnectionOptions);
            Assert.NotNull(o.TransferConnectionOptions);
            Assert.NotNull(o.DistributedConnectionOptions);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with default delegates"), AutoData]
        public async Task Instantiation_Default_Delegates(
            int messageTimeout,
            bool autoAcknowledgePrivateMessages,
            DiagnosticLevel minimumDiagnosticLevel,
            int startingToken)
        {
            var o = new SoulseekClientOptions(
                messageTimeout: messageTimeout,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                minimumDiagnosticLevel: minimumDiagnosticLevel,
                startingToken: startingToken);

            var ip = new IPEndPoint(IPAddress.None, 1);

            Assert.Equal(Enumerable.Empty<Directory>(), (await o.BrowseResponseResolver(string.Empty, ip)).Directories);
            Assert.Equal(Enumerable.Empty<Directory>(), (await o.BrowseResponseResolver(string.Empty, ip)).LockedDirectories);

            var ex = await Record.ExceptionAsync(() => o.EnqueueDownloadAction(string.Empty, ip, string.Empty));
            Assert.Null(ex);

            var placeInQueue = await o.PlaceInQueueResponseResolver(string.Empty, ip, string.Empty);
            Assert.Null(placeInQueue);

            Assert.IsType<UserInfo>(await o.UserInfoResponseResolver(string.Empty, ip));

            Assert.Null(o.SearchResponseResolver);
            Assert.Null(o.DirectoryContentsResponseResolver);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given cache implementations")]
        public void Instantiates_With_Given_Cache_Implementations()
        {
            var user = new UserEndPointCache();
            var search = new SearchResponseCache();

            var o = new SoulseekClientOptions(userEndPointCache: user, searchResponseCache: search);

            Assert.Equal(user, o.UserEndPointCache);
            Assert.Equal(search, o.SearchResponseCache);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if distributed child limit is less than zero")]
        public void Throws_If_Distributed_Child_Limit_Is_Less_Than_Zero()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(distributedChildLimit: -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if listen port is too high")]
        public void Throws_If_Listen_Port_Is_Too_High()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(listenPort: 999999999));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if listen port is too low")]
        public void Throws_If_Listen_Port_Is_Too_Low()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(listenPort: 1023));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "With")]
        [Fact(DisplayName = "Throws if patch is null")]
        public void Throws_If_Patch_Is_Null()
        {
            var ex = Record.Exception(() => new SoulseekClientOptions().With(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "With")]
        [Theory(DisplayName = "Clones with expected properties given a patch"), AutoData]
        public void Clones_With_Expected_Properties_Given_A_Patch(
            bool? enableListener,
            bool? enableDistributedNetwork,
            bool? acceptDistributedChildren,
            int? distributedChildLimit,
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

            var rnd = new Random();
            var listenPort = rnd.Next(1024, 65535);

            var patch = new SoulseekClientOptionsPatch(
                enableListener,
                listenPort,
                enableDistributedNetwork: enableDistributedNetwork,
                acceptDistributedChildren: acceptDistributedChildren,
                distributedChildLimit: distributedChildLimit,
                deduplicateSearchRequests: deduplicateSearchRequests,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                autoAcknowledgePrivilegeNotifications: autoAcknowledgePrivilegeNotifications,
                acceptPrivateRoomInvitations: acceptPrivateRoomInvitations,
                serverConnectionOptions: serverConnectionOptions,
                peerConnectionOptions: peerConnectionOptions,
                transferConnectionOptions: transferConnectionOptions,
                incomingConnectionOptions: incomingConnectionOptions,
                distributedConnectionOptions: distributedConnectionOptions);

            var o = new SoulseekClientOptions().With(patch);

            Assert.Equal(enableListener, o.EnableListener);
            Assert.Equal(listenPort, o.ListenPort);
            Assert.Equal(enableDistributedNetwork, o.EnableDistributedNetwork);
            Assert.Equal(acceptDistributedChildren, o.AcceptDistributedChildren);
            Assert.Equal(distributedChildLimit, o.DistributedChildLimit);
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
            Assert.Equal(-1, o.TransferConnectionOptions.InactivityTimeout);
        }

        [Trait("Category", "With")]
        [Theory(DisplayName = "Clones with expected properties"), AutoData]
        public void Clones_With_Expected_Properties(
            bool? enableListener,
            bool? enableDistributedNetwork,
            bool? acceptDistributedChildren,
            int? distributedChildLimit,
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

            var rnd = new Random();
            var listenPort = rnd.Next(1024, 65535);

            var o = new SoulseekClientOptions().With(
                enableListener,
                listenPort,
                enableDistributedNetwork: enableDistributedNetwork,
                acceptDistributedChildren: acceptDistributedChildren,
                distributedChildLimit: distributedChildLimit,
                deduplicateSearchRequests: deduplicateSearchRequests,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                autoAcknowledgePrivilegeNotifications: autoAcknowledgePrivilegeNotifications,
                acceptPrivateRoomInvitations: acceptPrivateRoomInvitations,
                serverConnectionOptions: serverConnectionOptions,
                peerConnectionOptions: peerConnectionOptions,
                transferConnectionOptions: transferConnectionOptions,
                incomingConnectionOptions: incomingConnectionOptions,
                distributedConnectionOptions: distributedConnectionOptions);

            Assert.Equal(enableListener, o.EnableListener);
            Assert.Equal(listenPort, o.ListenPort);
            Assert.Equal(enableDistributedNetwork, o.EnableDistributedNetwork);
            Assert.Equal(acceptDistributedChildren, o.AcceptDistributedChildren);
            Assert.Equal(distributedChildLimit, o.DistributedChildLimit);
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
            Assert.Equal(-1, o.TransferConnectionOptions.InactivityTimeout);
        }

        [Trait("Category", "With")]
        [Fact(DisplayName = "Does not throw given all nulls")]
        public void Does_Not_Throw_Given_All_Nulls()
        {
            var ex = Record.Exception(() => new SoulseekClientOptions().With());

            Assert.Null(ex);
        }

        private class UserEndPointCache : IUserEndPointCache
        {
            public void AddOrUpdate(string username, IPEndPoint endPoint)
            {
                throw new NotImplementedException();
            }

            public bool TryGet(string username, out IPEndPoint endPoint)
            {
                throw new NotImplementedException();
            }
        }

        private class SearchResponseCache : ISearchResponseCache
        {
            public void AddOrUpdate(int responseToken, (string Username, int Token, string Query, SearchResponse SearchResponse) response)
            {
                throw new NotImplementedException();
            }

            public bool TryGet(int responseToken, out (string Username, int Token, string Query, SearchResponse SearchResponse) response)
            {
                throw new NotImplementedException();
            }

            public bool TryRemove(int responseToken, out (string Username, int Token, string Query, SearchResponse SearchResponse) response)
            {
                throw new NotImplementedException();
            }
        }
    }
}
