// <copyright file="SendRoomMessageAsyncTests.cs" company="JP Dillingham">
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
    using Soulseek.Network.Tcp;
    using Xunit;

    public class SendRoomMessageAsyncTests
    {
        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync throws InvalidOperationException when not connected"), AutoData]
        public async Task SendRoomMessageAsync_Throws_InvalidOperationException_When_Not_Connected(string roomName, string message)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.SendRoomMessageAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync throws InvalidOperationException when not logged in"), AutoData]
        public async Task SendRoomMessageAsync_Throws_InvalidOperationException_When_Not_Logged_In(string roomName, string message)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.SendRoomMessageAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync throws ArgumentException given bad input")]
        [InlineData(null, "message")]
        [InlineData("  ", "message")]
        [InlineData("", "message")]
        [InlineData("username", null)]
        [InlineData("username", "")]
        public async Task SendRoomMessageAsync_Throws_ArgumentException_Given_Bad_Input(string roomName, string message)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.SendRoomMessageAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync does not throw when write does not throw"), AutoData]
        public async Task SendRoomMessageAsync_Does_Not_Throw_When_Write_Does_Not_Throw(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SendRoomMessageAsync(roomName, message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync uses given CancellationToken"), AutoData]
        public async Task SendRoomMessageAsync_Uses_Given_CancellationToken(string roomName, string message)
        {
            var cancellationToken = new CancellationToken(true);

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.SendRoomMessageAsync(roomName, message, cancellationToken);
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync throws SoulseekClientException when write throws"), AutoData]
        public async Task SendRoomMessageAsync_Throws_SoulseekClientException_When_Write_Throws(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SendRoomMessageAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync throws TimeoutException on timeout"), AutoData]
        public async Task SendRoomMessageAsync_Throws_TimeoutException_On_Timeout(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SendRoomMessageAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "SendRoomMessageAsync")]
        [Theory(DisplayName = "SendRoomMessageAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task SendRoomMessageAsync_Throws_OperationCanceledException_On_Cancellation(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SendRoomMessageAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
