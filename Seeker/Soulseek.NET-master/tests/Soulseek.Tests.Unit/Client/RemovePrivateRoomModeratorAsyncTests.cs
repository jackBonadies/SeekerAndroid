﻿// <copyright file="RemovePrivateRoomModeratorAsyncTests.cs" company="JP Dillingham">
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

    public class RemovePrivateRoomModeratorAsyncTests
    {
        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Theory(DisplayName = "RemovePrivateRoomModeratorAsync throws ArgumentException given bad roomName")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("\t")]

        public async Task RemovePrivateRoomModeratorAsync_Throws_ArgumentException_Given_Bad_RoomName(string roomName)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync(roomName, "user"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Equal("roomName", ((ArgumentException)ex).ParamName);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Theory(DisplayName = "RemovePrivateRoomModeratorAsync throws ArgumentException given bad roomName")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("\t")]

        public async Task RemovePrivateRoomModeratorAsync_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync("room", username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Equal("username", ((ArgumentException)ex).ParamName);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Fact(DisplayName = "RemovePrivateRoomModeratorAsync throws InvalidOperationException when not connected")]
        public async Task RemovePrivateRoomModeratorAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync("room", "user"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Fact(DisplayName = "RemovePrivateRoomModeratorAsync throws InvalidOperationException when not logged in")]
        public async Task RemovePrivateRoomModeratorAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync("room", "user"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Theory(DisplayName = "RemovePrivateRoomModeratorAsync completes when wait completes"), AutoData]
        public async Task RemovePrivateRoomModeratorAsync_Completes_When_Wait_Completes(string roomName, string username)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync(roomName, username));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Theory(DisplayName = "RemovePrivateRoomModeratorAsync throws when wait throws"), AutoData]
        public async Task RemovePrivateRoomModeratorAsync_Throws_When_Wait_Throws(string roomName, string username)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var expectedEx = new TimeoutException();

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(expectedEx));

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync(roomName, username));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Theory(DisplayName = "RemovePrivateRoomModeratorAsync throws SoulseekClientException when write throws"), AutoData]
        public async Task RemovePrivateRoomModeratorAsync_Throws_SoulseekClientException_When_Write_Throws(string roomName, string username)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync(roomName, username, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Theory(DisplayName = "RemovePrivateRoomModeratorAsync throws TimeoutException when write times out"), AutoData]
        public async Task RemovePrivateRoomModeratorAsync_Throws_TimeoutException_When_Write_Times_Out(string roomName, string username)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync(roomName, username, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "RemovePrivateRoomModeratorAsync")]
        [Theory(DisplayName = "RemovePrivateRoomModeratorAsync throws OperationCanceledException when write is canceled"), AutoData]
        public async Task RemovePrivateRoomModeratorAsync_Throws_OperationCanceledException_When_Write_Is_Canceled(string roomName, string username)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.RemovePrivateRoomModeratorAsync(roomName, username, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
