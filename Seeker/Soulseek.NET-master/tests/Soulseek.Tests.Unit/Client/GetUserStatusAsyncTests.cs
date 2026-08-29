// <copyright file="GetUserStatusAsyncTests.cs" company="JP Dillingham">
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

    public class GetUserStatusAsyncTests
    {
        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetUserStatusAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetUserStatusAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatusAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync returns expected info"), AutoData]
        public async Task GetUserStatusAsync_Returns_Expected_Info(string username, UserPresence presence, bool privileged)
        {
            var result = new UserStatus(username, presence, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatus>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var status = await s.GetUserStatusAsync(username);

                Assert.Equal(result.Presence, status.Presence);
                Assert.Equal(result.IsPrivileged, status.IsPrivileged);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync uses given CancellationToken"), AutoData]
        public async Task GetUserStatusAsync_Uses_Given_CancellationToken(string username, UserPresence presence, bool privileged)
        {
            var cancellationToken = new CancellationToken();
            var result = new UserStatus(username, presence, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatus>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GetUserStatusAsync(username, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken));
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws UserOfflineException on user offline"), AutoData]
        public async Task GetUserStatusAsync_Throws_UserOfflineException_On_User_Offline(string username)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatus>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserStatus>(new UserOfflineException()));

            var serverConn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GetUserStatusAsync_Throws_SoulseekClientExceptionn_On_Throw(string username, UserPresence status, bool privileged)
        {
            var result = new UserStatus(username, status, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatus>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetUserStatusAsync_Throws_TimeoutException_On_Timeout(string username, UserPresence status, bool privileged)
        {
            var result = new UserStatus(username, status, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatus>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetUserStatusAsync")]
        [Theory(DisplayName = "GetUserStatusAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetUserStatusAsync_Throws_OperationCanceledException_On_Cancellation(string username, UserPresence status, bool privileged)
        {
            var result = new UserStatus(username, status, privileged);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatus>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatusAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
