// <copyright file="JoinLeaveRoomAsyncTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class JoinLeaveRoomAsyncTests
    {
        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws InvalidOperationException when not connected"), AutoData]
        public async Task JoinRoomAsync_Throws_InvalidOperationException_When_Not_Connected(string roomName)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws InvalidOperationException when not logged in"), AutoData]
        public async Task JoinRoomAsync_Throws_InvalidOperationException_When_Not_Logged_In(string roomName)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws ArgumentException given bad input")]
        [InlineData(null)]
        [InlineData("  ")]
        [InlineData("")]
        public async Task JoinRoomAsync_Throws_ArgumentException_Given_Bad_Input(string roomName)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync returns expected response on success"), AutoData]
        public async Task JoinRoomAsync_Returns_Expected_Response_On_Success(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var expectedResponse = new RoomData(roomName, Enumerable.Empty<UserData>(), false, null, null);

            var key = new WaitKey(MessageCode.Server.JoinRoom, roomName);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<RoomData>(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(expectedResponse));

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                RoomData response;

                response = await s.JoinRoomAsync(roomName);

                Assert.Equal(expectedResponse, response);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync uses given CancellationToken"), AutoData]
        public async Task JoinRoomAsync_Uses_Given_CancellationToken(string roomName, bool isPrivate)
        {
            var cancellationToken = new CancellationToken();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var expectedResponse = new RoomData(roomName, Enumerable.Empty<UserData>(), false, null, null);

            var key = new WaitKey(MessageCode.Server.JoinRoom, roomName);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<RoomData>(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(expectedResponse));

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                RoomData response;

                response = await s.JoinRoomAsync(roomName, isPrivate, cancellationToken);

                Assert.Equal(expectedResponse, response);
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws SoulseekClientException when write throws"), AutoData]
        public async Task JoinRoomAsync_Throws_SoulseekClientException_When_Write_Throws(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws TimeoutException on timeout"), AutoData]
        public async Task JoinRoomAsync_Throws_TimeoutException_On_Timeout(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws NoResponseException on wait timeout"), AutoData]
        public async Task JoinRoomAsync_Throws_NoResponseException_On_Wait_Timeout(string roomName)
        {
            var conn = new Mock<IMessageConnection>();

            var key = new WaitKey(MessageCode.Server.JoinRoom, roomName);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<RoomData>(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<RoomData>(new TimeoutException()));

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<NoResponseException>(ex);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task JoinRoomAsync_Throws_OperationCanceledException_On_Cancellation(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "JoinRoomAsync")]
        [Theory(DisplayName = "JoinRoomAsync throws RoomJoinForbiddenException when server rejects"), AutoData]
        public async Task JoinRoomAsync_Throws_RoomJoinForbiddenException_When_Server_Rejects(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var key = new WaitKey(MessageCode.Server.JoinRoom, roomName);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<RoomData>(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Throws(new RoomJoinForbiddenException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.JoinRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<RoomJoinForbiddenException>(ex);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync throws InvalidOperationException when not connected"), AutoData]
        public async Task LeaveRoomAsync_Throws_InvalidOperationException_When_Not_Connected(string roomName)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync throws InvalidOperationException when not logged in"), AutoData]
        public async Task LeaveRoomAsync_Throws_InvalidOperationException_When_Not_Logged_In(string roomName)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync throws ArgumentException given bad input")]
        [InlineData(null)]
        [InlineData("  ")]
        [InlineData("")]
        public async Task LeaveRoomAsync_Throws_ArgumentException_Given_Bad_Input(string roomName)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync returns expected response on success"), AutoData]
        public async Task LeaveRoomAsync_Returns_Expected_Response_On_Success(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var key = new WaitKey(MessageCode.Server.LeaveRoom, roomName);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync throws NoResponseException on wait timeout"), AutoData]
        public async Task LeaveRoomAsync_Throws_NoResponseException_On_Wait_Timeout(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var key = new WaitKey(MessageCode.Server.LeaveRoom, roomName);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(new TimeoutException()));

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<NoResponseException>(ex);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync uses given CancellationToken"), AutoData]
        public async Task LeaveRoomAsync_Uses_Given_CancellationToken(string roomName)
        {
            var cancellationToken = new CancellationToken();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var key = new WaitKey(MessageCode.Server.LeaveRoom, roomName);
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName, cancellationToken));

                Assert.Null(ex);
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync throws SoulseekClientException when write throws"), AutoData]
        public async Task LeaveRoomAsync_Throws_SoulseekClientException_When_Write_Throws(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync throws TimeoutException on timeout"), AutoData]
        public async Task LeaveRoomAsync_Throws_TimeoutException_On_Timeout(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "LeaveRoomAsync")]
        [Theory(DisplayName = "LeaveRoomAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task LeaveRoomAsync_Throws_OperationCanceledException_On_Cancellation(string roomName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.LeaveRoomAsync(roomName));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
