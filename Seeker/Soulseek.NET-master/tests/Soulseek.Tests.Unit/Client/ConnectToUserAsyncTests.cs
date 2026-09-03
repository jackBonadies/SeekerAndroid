// <copyright file="ConnectToUserAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class ConnectToUserAsyncTests
    {
        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "ConnectToUserAsync Throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task Throws_ArgumentException_On_Bad_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectToUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "ConnectToUserAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task ConnectToUserAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.ConnectToUserAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Gets a connection"), AutoData]
        public async Task Gets_A_Connection(string username)
        {
            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            using (client)
            {
                await client.ConnectToUserAsync(username);
            }

            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Invalidates cache if invalidateCache is true"), AutoData]
        public async Task Invalidates_Cache_If_InvalidateCache_Is_True(string username)
        {
            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            using (client)
            {
                await client.ConnectToUserAsync(username, invalidateCache: true);
            }

            mocks.PeerConnectionManager.Verify(m => m.TryInvalidateMessageConnectionCache(username), Times.Once);
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Does not invalidate cache if invalidateCache is false"), AutoData]
        public async Task Does_Not_Invalidate_Cache_If_InvlidateCache_Is_False(string username)
        {
            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            using (client)
            {
                await client.ConnectToUserAsync(username, invalidateCache: false);
            }

            mocks.PeerConnectionManager.Verify(m => m.TryInvalidateMessageConnectionCache(username), Times.Never);
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Creates a diagnostic message if the cache was invalidated"), AutoData]
        public async Task Creates_A_Diagnostic_Message_If_The_Cache_Was_Invalidated(string username)
        {
            var (client, mocks) = GetFixture();

            mocks.PeerConnectionManager.Setup(m => m.TryInvalidateMessageConnectionCache(username)).Returns(true);

            mocks.Waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("127.0.0.1"), 1)));

            using (client)
            {
                await client.ConnectToUserAsync(username, invalidateCache: true);
            }

            mocks.Diagnostic.Verify(m => m.Debug($"Invalidated message connection cache for {username}"));
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Fact(DisplayName = "Throws UserOfflineException when user is offline")]
        public async Task Throws_UserOfflineException_When_User_Is_Offline()
        {
            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new UserOfflineException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Fact(DisplayName = "Throws TimeoutException when connection times out")]
        public async Task Throws_TimeoutException_When_Connection_Times_Out()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Fact(DisplayName = "Throws OperationCanceledException when canceled")]
        public async Task Throws_OperationCanceledException_When_Canceled()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Fact(DisplayName = "Throws SoulseekClientException on exception")]
        public async Task Throws_SoulseekClientException_On_Exception()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("foo"));

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectToUserAsync("u"));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<UserEndPointException>(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException.InnerException);
                Assert.Equal("foo", ex.InnerException.InnerException.Message);
            }
        }

        [Trait("Category", "ConnectToUserAsync")]
        [Theory(DisplayName = "Uses given CancellationToken"), AutoData]
        public async Task Uses_Given_CancellationToken(string user)
        {
            var cancellationToken = new CancellationToken();

            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(user, IPAddress.Parse("127.0.0.1"), 1)));

            using (client)
            {
                await client.ConnectToUserAsync(user, cancellationToken: cancellationToken);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.AtLeastOnce);
        }

        private static (SoulseekClient client, Mocks Mocks) GetFixture(SoulseekClientOptions options = null)
        {
            var mocks = new Mocks();
            var client = new SoulseekClient(
                minorVersion: 9999,
                serverConnection: mocks.ServerConnection.Object,
                peerConnectionManager: mocks.PeerConnectionManager.Object,
                waiter: mocks.Waiter.Object,
                diagnosticFactory: mocks.Diagnostic.Object,
                options: options ?? new SoulseekClientOptions());

            client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
            return (client, mocks);
        }

        private class Mocks
        {
            public Mocks()
            {
                PeerConnectionManager.Setup(m => m.TryInvalidateMessageConnectionCache(It.IsAny<string>()))
                    .Returns(false);
                PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<string>(), It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new Mock<IMessageConnection>().Object));

                ServerConnection.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            }

            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
        }
    }
}
