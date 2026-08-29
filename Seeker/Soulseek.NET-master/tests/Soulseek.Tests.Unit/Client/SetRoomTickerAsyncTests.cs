// <copyright file="SetRoomTickerAsyncTests.cs" company="JP Dillingham">
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

    public class SetRoomTickerAsyncTests
    {
        [Trait("Category", "SetRoomTickerAsync")]
        [Theory(DisplayName = "SetRoomTickerAsync throws InvalidOperationException when not connected"), AutoData]
        public async Task SetRoomTickerAsync_Throws_InvalidOperationException_When_Not_Connected(string roomName, string message)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.SetRoomTickerAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "SetRoomTickerAsync")]
        [Theory(DisplayName = "SetRoomTickerAsync throws InvalidOperationException when not logged in"), AutoData]
        public async Task SetRoomTickerAsync_Throws_InvalidOperationException_When_Not_Logged_In(string roomName, string message)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.SetRoomTickerAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "SetRoomTickerAsync")]
        [Theory(DisplayName = "SetRoomTickerAsync throws ArgumentException given bad input")]
        [InlineData(null, "message")]
        [InlineData("  ", "message")]
        [InlineData("", "message")]
        [InlineData("username", null)]
        [InlineData("username", "")]
        public async Task SetRoomTickerAsync_Throws_ArgumentException_Given_Bad_Input(string roomName, string message)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.SetRoomTickerAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "SetRoomTickerAsync")]
        [Theory(DisplayName = "SetRoomTickerAsync does not throw when write does not throw"), AutoData]
        public async Task SetRoomTickerAsync_Does_Not_Throw_When_Write_Does_Not_Throw(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetRoomTickerAsync(roomName, message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "SetRoomTickerAsync")]
        [Theory(DisplayName = "SetRoomTickerAsync throws SoulseekClientException when write throws"), AutoData]
        public async Task SetRoomTickerAsync_Throws_Exception_When_Write_Throws(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetRoomTickerAsync(roomName, message));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "SetRoomTickerAsync")]
        [Theory(DisplayName = "SetRoomTickerAsync throws TimeoutException when write times out"), AutoData]
        public async Task SetRoomTickerAsync_Throws_TimeoutException_When_Write_Times_Out(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetRoomTickerAsync(roomName, message, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "SetRoomTickerAsync")]
        [Theory(DisplayName = "SetRoomTickerAsync throws OperationCanceledException when write is canceled"), AutoData]
        public async Task SetRoomTickerAsync_Throws_OperationCanceledException_When_Write_Is_Canceled(string roomName, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.SetRoomTickerAsync(roomName, message, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
