// <copyright file="GetDirectoryContentsAsyncTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetDirectoryContentsAsyncTests
    {
        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetDirectoryContentsAsync_Throws_ArgumentException_On_Bad_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetDirectoryContentsAsync(username, "foo"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws ArgumentException on bad directory")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetDirectoryContentsAsync_Throws_ArgumentException_On_Bad_Directory(string directory)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetDirectoryContentsAsync("foo", directory));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetDirectoryContentsAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetDirectoryContentsAsync("a", "b"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_TimeoutException_On_Timeout(string username, string directory)
        {
            var result = new Directory(directory);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<Directory>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new TimeoutException()));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                IEnumerable<Directory> dirs = null;
                var ex = await Record.ExceptionAsync(async () => dirs = await s.GetDirectoryContentsAsync(username, directory));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_OperationCanceledException_On_Cancellation(string username, string directory)
        {
            var result = new Directory(directory);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<Directory>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new OperationCanceledException()));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                IEnumerable<Directory> dirs = null;
                var ex = await Record.ExceptionAsync(async () => dirs = await s.GetDirectoryContentsAsync(username, directory));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws UserOfflineException on user offline"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_UserOfflineException_On_User_Offline(string username, string directory)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(new UserOfflineException()));

            var serverConn = new Mock<IMessageConnection>();
            var connManager = new Mock<IPeerConnectionManager>();

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                IEnumerable<Directory> dirs = null;
                var ex = await Record.ExceptionAsync(async () => dirs = await s.GetDirectoryContentsAsync(username, directory));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GetDirectoryContentsAsync_Throws_SoulseekClientException_On_Throw(string username, string directory)
        {
            var result = new Directory(directory);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<Directory>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new NullReferenceException()));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                IEnumerable<Directory> dirs = null;
                var ex = await Record.ExceptionAsync(async () => dirs = await s.GetDirectoryContentsAsync(username, directory));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<NullReferenceException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync returns expected Directory"), AutoData]
        public async Task GetDirectoryContentsAsync_Returns_Expected_Directory(string username, string directory)
        {
            IReadOnlyCollection<Directory> result = new List<Directory>() { new Directory(directory) }.AsReadOnly();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<IReadOnlyCollection<Directory>>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dir = await s.GetDirectoryContentsAsync(username, directory);

                Assert.Equal(result, dir);
            }
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync uses given token"), AutoData]
        public async Task GetDirectoryContentsAsync_Uses_Given_Token(string username, string directory, int token)
        {
            IReadOnlyCollection<Directory> result = new List<Directory>() { new Directory(directory) }.AsReadOnly();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<IReadOnlyCollection<Directory>>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dir = await s.GetDirectoryContentsAsync(username, directory, token);

                Assert.Equal(result, dir);
            }

            conn.Verify(
                m =>
                m.WriteAsync(
                    It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new FolderContentsRequest(token, directory).ToByteArray())),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Trait("Category", "GetDirectoryContentsAsync")]
        [Theory(DisplayName = "GetDirectoryContentsAsync uses given CancellationToken"), AutoData]
        public async Task GetDirectoryContentsAsync_Uses_Given_CancellationToken(string username, string directory, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Directory> result = new List<Directory>() { new Directory(directory) }.AsReadOnly();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<IReadOnlyCollection<Directory>>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var conn = new Mock<IMessageConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var dir = await s.GetDirectoryContentsAsync(username, directory, cancellationToken: cancellationToken);

                Assert.Equal(result, dir);
            }

            serverConn.Verify(
                m =>
                m.WriteAsync(
                    It.IsAny<IOutgoingMessage>(),
                    cancellationToken),
                Times.Once);
        }
    }
}
