// <copyright file="GetDownloadPlaceInQueueAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetDownloadPlaceInQueueAsyncTests
    {
        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetDownloadPlaceInQueueAsync_Throws_ArgumentException_On_Bad_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, "a"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws ArgumentException on bad filename")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetDownloadPlaceInQueueAsync_Throws_ArgumentException_On_Bad_Filename(string filename)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync("a", filename));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetPeerInfoAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync("a", "b"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws TransferNotFoundException when downloads from username not found"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Throws_TransferNotFoundException_When_Downloads_From_Username_Not_Found(string username, string filename)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var transfer = new TransferInternal(TransferDirection.Download, "different", filename, 1);
                var dict = new ConcurrentDictionary<int, TransferInternal>();

                dict.TryAdd(1, transfer);

                s.SetProperty("DownloadDictionary", dict);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));

                Assert.NotNull(ex);
                Assert.IsType<TransferNotFoundException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws TransferNotFoundException when download not found"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Throws_TransferNotFoundException_When_Download_Not_Found(string username, string filename)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var transfer = new TransferInternal(TransferDirection.Download, username, "different", 1);
                var dict = new ConcurrentDictionary<int, TransferInternal>();

                dict.TryAdd(1, transfer);

                s.SetProperty("DownloadDictionary", dict);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));

                Assert.NotNull(ex);
                Assert.IsType<TransferNotFoundException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync returns expected info"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Returns_Expected_Info(string username, string filename, int placeInQueue)
        {
            var result = new PlaceInQueueResponse(filename, placeInQueue);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PlaceInQueueResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", dict);

                var place = await s.GetDownloadPlaceInQueueAsync(username, filename);

                Assert.Equal(placeInQueue, place);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync uses given CancellationToken"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Uses_Given_CancellationToken(string username, string filename, int placeInQueue, CancellationToken cancellationToken)
        {
            var result = new PlaceInQueueResponse(filename, placeInQueue);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PlaceInQueueResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", dict);

                var place = await s.GetDownloadPlaceInQueueAsync(username, filename, cancellationToken: cancellationToken);

                Assert.Equal(placeInQueue, place);
            }

            waiter.Verify(m => m.Wait<PlaceInQueueResponse>(It.IsAny<WaitKey>(), null, cancellationToken), Times.Once);
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws UserOfflineException on user offline"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Throws_UserOfflineException_On_User_Offline(string username, string filename)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(new UserOfflineException()));

            var serverConn = new Mock<IMessageConnection>();
            var connManager = new Mock<IPeerConnectionManager>();

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", dict);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws SoulseekClientException on exception"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Throws_SoulseekClientException_On_Exception(string username, string filename)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PlaceInQueueResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new Exception());
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", dict);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Throws_TimeoutException_On_Timeout(string username, string filename)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PlaceInQueueResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", dict);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetDownloadPlaceInQueueAsync")]
        [Theory(DisplayName = "GetDownloadPlaceInQueueAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetDownloadPlaceInQueueAsync_Throws_OperationCanceledException_On_Cancellation(string username, string filename)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<PlaceInQueueResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.GetOrAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", dict);

                var ex = await Record.ExceptionAsync(() => s.GetDownloadPlaceInQueueAsync(username, filename));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
