// <copyright file="GetRoomListAsyncTests.cs" company="JP Dillingham">
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
    using Soulseek.Network.Tcp;
    using Xunit;

    public class GetRoomListAsyncTests
    {
        [Trait("Category", "GetRoomListAsync")]
        [Fact(DisplayName = "GetRoomListAsync throws InvalidOperationException when not connected")]
        public async Task GetRoomListAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.GetRoomListAsync());

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetRoomListAsync")]
        [Fact(DisplayName = "GetRoomListAsync throws InvalidOperationException when not logged in")]
        public async Task GetRoomListAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.GetRoomListAsync());

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetRoomListAsync")]
        [Theory(DisplayName = "GetRoomListAsync returns expected response on success"), AutoData]
        public async Task GetRoomListAsync_Returns_Expected_Response_On_Success(RoomList rooms)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var key = new WaitKey(MessageCode.Server.RoomList);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<RoomList>(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(rooms));

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                RoomList response;

                response = await s.GetRoomListAsync();

                Assert.Equal(rooms, response);
            }
        }

        [Trait("Category", "GetRoomListAsync")]
        [Theory(DisplayName = "GetRoomListAsync uses given CancellationToken"), AutoData]
        public async Task GetRoomListAsync_Uses_Given_CancellationToken(RoomList rooms, CancellationToken cancellationToken)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var key = new WaitKey(MessageCode.Server.RoomList);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<RoomList>(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(rooms));

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                RoomList response;

                response = await s.GetRoomListAsync(cancellationToken);

                Assert.Equal(rooms, response);
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "GetRoomListAsync")]
        [Fact(DisplayName = "GetRoomListAsync throws SoulseekClientException when write throws")]
        public async Task GetRoomListAsync_Throws_SoulseekClientException_When_Write_Throws()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetRoomListAsync());

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetRoomListAsync")]
        [Fact(DisplayName = "GetRoomListAsync throws TimeoutException on timeout")]
        public async Task GetRoomListAsync_Throws_TimeoutException_On_Timeout()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetRoomListAsync());

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetRoomListAsync")]
        [Fact(DisplayName = "GetRoomListAsync throws OperationCanceledException on cancellation")]
        public async Task GetRoomListAsync_Throws_OperationCanceledException_On_Cancellation()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetRoomListAsync());

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
