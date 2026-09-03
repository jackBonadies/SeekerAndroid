// <copyright file="WatchUserAsyncTests.cs" company="JP Dillingham">
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
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class WatchUserAsyncTests
    {
        [Trait("Category", "WatchUserAsync")]
        [Theory(DisplayName = "WatchUserAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task WatchUserAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.WatchUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "WatchUserAsync")]
        [Theory(DisplayName = "WatchUserAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task WatchUserAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.WatchUserAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "WatchUserAsync")]
        [Theory(DisplayName = "WatchUserAsync returns expected info"), AutoData]
        public async Task WatchUserAsync_Returns_Expected_Info(string username, UserData userData)
        {
            var result = new WatchUserResponse(username, true, userData);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<WatchUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var add = await s.WatchUserAsync(username);

                Assert.Equal(result.UserData, add);
            }
        }

        [Trait("Category", "WatchUserAsync")]
        [Theory(DisplayName = "WatchUserAsync uses given CancellationToken"), AutoData]
        public async Task WatchUserAsync_Uses_Given_CancellationToken(string username, UserData userData, CancellationToken cancellationToken)
        {
            var result = new WatchUserResponse(username, true, userData);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<WatchUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.WatchUserAsync(username, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "WatchUserAsync")]
        [Theory(DisplayName = "WatchUserAsync throws UserNotFoundException when exists is false"), AutoData]
        public async Task WatchUserAsync_Throws_UserNotFoundException_When_Exists_Is_False(string username, UserData userData)
        {
            var result = new WatchUserResponse(username, false, userData);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<WatchUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.WatchUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserNotFoundException>(ex);
            }
        }

        [Trait("Category", "WatchUserAsyncAsync")]
        [Theory(DisplayName = "WatchUserAsyncAsync throws SoulseekClientException on throw"), AutoData]
        public async Task WatchUserAsyncAsync_Throws_SoulseekClientException_On_Throw(string username, bool exists, UserData userData)
        {
            var result = new WatchUserResponse(username, exists, userData);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<WatchUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.WatchUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "WatchUserAsyncAsync")]
        [Theory(DisplayName = "WatchUserAsync throws TimeoutException on timeout"), AutoData]
        public async Task WatchUserAsyncAsync_Throws_TimeoutException_On_Timeout(string username, bool exists, UserData userData)
        {
            var result = new WatchUserResponse(username, exists, userData);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<WatchUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.WatchUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "WatchUserAsync")]
        [Theory(DisplayName = "WatchUserAsync throws OperationCanceledException on cancel"), AutoData]
        public async Task WatchUserAsync_Throws_OperationCanceledException_On_Cancel(string username, bool exists, UserData userData)
        {
            var result = new WatchUserResponse(username, exists, userData);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<WatchUserResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.WatchUserAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
