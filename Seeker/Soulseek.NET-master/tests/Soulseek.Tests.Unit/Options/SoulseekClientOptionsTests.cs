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

namespace Soulseek.Tests.Unit.Options
{
    using System;
    using System.Collections.Generic;

    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
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
            bool enableUploadQueue,
            int maximumConcurrentSearches,
            int maximumConcurrentUploads,
            int maximumUploadSpeed,
            int maximumConcurrentDownloads,
            int maximumDownloadSpeed,
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

            var o = new SoulseekClientOptions(
                enableListener,
                listenAddress,
                listenPort,
                enableDistributedNetwork: enableDistributedNetwork,
                acceptDistributedChildren: acceptDistributedChildren,
                distributedChildLimit: distributedChildLimit,
                maximumConcurrentSearches: maximumConcurrentSearches,
                maximumConcurrentUploads: maximumConcurrentUploads,
                maximumUploadSpeed: maximumUploadSpeed,
                maximumConcurrentDownloads: maximumConcurrentDownloads,
                maximumDownloadSpeed: maximumDownloadSpeed,
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
            Assert.Equal(enableUploadQueue, o.EnableDistributedNetwork);
            Assert.Equal(maximumConcurrentSearches, o.MaximumConcurrentSearches);
            Assert.Equal(maximumConcurrentUploads, o.MaximumConcurrentUploads);
            Assert.Equal(maximumUploadSpeed, o.MaximumUploadSpeed);
            Assert.Equal(maximumConcurrentDownloads, o.MaximumConcurrentDownloads);
            Assert.Equal(maximumDownloadSpeed, o.MaximumDownloadSpeed);
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
            Assert.Equal(transferConnectionOptions.InactivityTimeout, o.TransferConnectionOptions.InactivityTimeout);

            Assert.Equal(userEndPointCache.Object, o.UserEndPointCache);
            Assert.Equal(searchResponseResolver, o.SearchResponseResolver);
            Assert.Equal(searchResponseCache.Object, o.SearchResponseCache);
            Assert.Equal(browseResponseResolver, o.BrowseResponseResolver);
            Assert.Equal(directoryContentsResponseResolver, o.DirectoryContentsResolver);
            Assert.Equal(userInfoResponseResolver, o.UserInfoResolver);
            Assert.Equal(enqueueDownloadAction, o.EnqueueDownload);
            Assert.Equal(placeInQueueResponseResolver, o.PlaceInQueueResolver);

            Assert.Equal(1, o.MaximumConcurrentUploadsPerUser);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with UploadSlotsPerUser set to 1")]
        public void Instantiates_With_UploadSlotsPerUser_1()
        {
            var o = new SoulseekClientOptions();

            Assert.Equal(1, o.MaximumConcurrentUploadsPerUser);
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

            var ex = await Record.ExceptionAsync(() => o.EnqueueDownload(string.Empty, ip, string.Empty));
            Assert.Null(ex);

            var placeInQueue = await o.PlaceInQueueResolver(string.Empty, ip, string.Empty);
            Assert.Null(placeInQueue);

            Assert.IsType<UserInfo>(await o.UserInfoResolver(string.Empty, ip));

            Assert.Null(o.SearchResponseResolver);
            Assert.Null(o.DirectoryContentsResolver);
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
        [Fact(DisplayName = "Throws if upload slots are zero")]
        public void Throws_If_Upload_Slots_Are_Zero()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(maximumConcurrentUploads: 0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("maximumConcurrentUploads", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if upload slots are negative")]
        public void Throws_If_Upload_Slots_Are_Negative()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(maximumConcurrentUploads: -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("maximumConcurrentUploads", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if download slots are zero")]
        public void Throws_If_Download_Slots_Are_Zero()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(maximumConcurrentDownloads: 0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("maximumConcurrentDownloads", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if download slots are negative")]
        public void Throws_If_Download_Slots_Are_Negative()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(maximumConcurrentDownloads: -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("maximumConcurrentDownloads", ((ArgumentOutOfRangeException)ex).ParamName);
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

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if MaxConcurrentSearches is negative")]
        public void Throws_If_MaxConcurrentSearches_Is_Negative()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(maximumConcurrentSearches: -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if MaxConcurrentSearches is zero")]
        public void Throws_If_MaxConcurrentSearches_Is_Zero()
        {
            SoulseekClientOptions x;
            var ex = Record.Exception(() => x = new SoulseekClientOptions(maximumConcurrentSearches: 0));

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
            int? maximumUploadSpeed,
            int? maximumDownloadSpeed,
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

            var patch = new SoulseekClientOptionsPatch(
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

            var original = new SoulseekClientOptions(
                maximumConcurrentUploads: 42,
                maximumConcurrentDownloads: 24,
                minimumDiagnosticLevel: DiagnosticLevel.None);

            var o = original.With(patch);

            // make sure the options that can't be patched did not change
            Assert.Equal(42, o.MaximumConcurrentUploads);
            Assert.Equal(24, o.MaximumConcurrentDownloads);
            Assert.Equal(DiagnosticLevel.None, o.MinimumDiagnosticLevel);

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

        [Trait("Category", "With")]
        [Theory(DisplayName = "Clones with expected properties"), AutoData]
        public void Clones_With_Expected_Properties(
            bool? enableListener,
            bool? enableDistributedNetwork,
            bool? acceptDistributedChildren,
            int? maximumUploadSpeed,
            int? maximumDownloadSpeed,
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

            var o = new SoulseekClientOptions().With(
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
