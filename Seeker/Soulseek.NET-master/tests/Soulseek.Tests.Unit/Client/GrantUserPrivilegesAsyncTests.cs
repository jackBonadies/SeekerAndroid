// <copyright file="GrantUserPrivilegesAsyncTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GrantUserPrivilegesAsyncTests
    {
        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GrantUserPrivilegesAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GrantUserPrivilegesAsync(username, 1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Equal("username", ((ArgumentException)ex).ParamName);
            }
        }

        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync throws ArgumentException on days <= 0")]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task GrantUserPrivilegesAsync_Throws_ArgumentException_On_Days_LEQ_0(int days)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GrantUserPrivilegesAsync("foo", days));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Equal("days", ((ArgumentException)ex).ParamName);
            }
        }

        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GrantUserPrivilegesAsync_Throws_InvalidOperationException_If_Not_Connected_And_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GrantUserPrivilegesAsync("a", 1));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync throws TimeoutException on timeout"), AutoData]
        public async Task GrantUserPrivilegesAsync_Throws_TimeoutException_On_Timeout(string username, int days)
        {
            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GrantUserPrivilegesAsync(username, days));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync throws OperationCanceledException on cancel"), AutoData]
        public async Task GrantUserPrivilegesAsync_Throws_OperationCanceledException_On_Cancel(string username, int days)
        {
            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GrantUserPrivilegesAsync(username, days));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GrantUserPrivilegesAsync_Throws_SoulseekClientException_On_Throw(string username, int days)
        {
            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GrantUserPrivilegesAsync(username, days));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
            }
        }

        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync does not throw on wait completion"), AutoData]
        public async Task GrantUserPrivilegesAsync_Does_Not_Throw_On_Wait_Completion(string username, int days)
        {
            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait(It.Is<WaitKey>(k => k == new WaitKey(MessageCode.Server.GivePrivileges)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: serverConn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GrantUserPrivilegesAsync(username, days));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "GrantUserPrivilegesAsync")]
        [Theory(DisplayName = "GrantUserPrivilegesAsync uses given CancellationToken"), AutoData]
        public async Task GrantUserPrivilegesAsync_Uses_Given_CancellationToken(string username, int days)
        {
            var cancellationToken = new CancellationToken();

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait(It.Is<WaitKey>(k => k == new WaitKey(MessageCode.Server.GivePrivileges)), null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: serverConn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GrantUserPrivilegesAsync(username, days, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }
    }
}
