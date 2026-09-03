// <copyright file="ChangePasswordAsyncTests.cs" company="JP Dillingham">
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

    public class ChangePasswordAsyncTests
    {
        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync throws ArgumentException on bad password")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task ChangePasswordAsync_Throws_ArgumentException_On_Null_Username(string password)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task ChangePasswordAsync_Throws_InvalidOperationException_If_Not_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync succeeds on matching confirmation"), AutoData]
        public async Task ChangePasswordAsync_Succeeds_On_Matching_Confirmation(string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<string>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(password));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync uses given CancellationToken"), AutoData]
        public async Task ChangePasswordAsync_Uses_Given_CancellationToken(string password, CancellationToken cancellationToken)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<string>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(password));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.ChangePasswordAsync(password, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync throws on mismatching confirmation"), AutoData]
        public async Task ChangePasswordAsync_Throws_On_Mismatching_Confirmation(string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<string>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(password + "!"));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.True(ex.Message.ContainsInsensitive("doesn't match the specified password"));
            }
        }

        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync throws SoulseekClientException on throw"), AutoData]
        public async Task ChangePasswordAsync_Throws_SoulseekClientException_On_Throw(string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<string>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(password));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new ConnectionException()));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync throws TimeoutException on timeout"), AutoData]
        public async Task ChangePasswordAsync_Throws_TimeoutException_On_Timeout(string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<string>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(password));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "ChangePasswordAsync")]
        [Theory(DisplayName = "ChangePasswordAsync throws OperationCanceledException on cancel"), AutoData]
        public async Task ChangePasswordAsync_Throws_OperationCanceledException_On_Cancel(string password)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<string>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(password));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.ChangePasswordAsync(password));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
