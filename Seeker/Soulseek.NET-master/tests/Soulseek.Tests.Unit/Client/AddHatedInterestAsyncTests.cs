// <copyright file="AddHatedInterestAsyncTests.cs" company="JP Dillingham">
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

    public class AddHatedInterestAsyncTests
    {
        [Trait("Category", "AddHatedInterestAsync")]
        [Theory(DisplayName = "AddHatedInterestAsync throws ArgumentException given bad interest")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("	")]
        public async Task AddHatedInterestAsync_Throws_ArgumentException_Given_Bad_Interest(string interest)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Equal("interest", ((ArgumentException)ex).ParamName);
            }
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Fact(DisplayName = "AddHatedInterestAsync throws InvalidOperationException when not connected")]
        public async Task AddHatedInterestAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync("interest"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Fact(DisplayName = "AddHatedInterestAsync throws InvalidOperationException when not logged in")]
        public async Task AddHatedInterestAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync("interest"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Theory(DisplayName = "AddHatedInterestAsync does not throw when write does not throw"), AutoData]
        public async Task AddHatedInterestAsync_Does_Not_Throw_When_Write_Does_Not_Throw(string interest)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync(interest));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Theory(DisplayName = "AddHatedInterestAsync sends expected interest"), AutoData]
        public async Task AddHatedInterestAsync_Sends_Expected_Interest(string interest)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync(interest));

                Assert.Null(ex);
            }

            conn.Verify(m => m.WriteAsync(It.Is<HatedInterestAddCommand>(c => c.Interest == interest), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Theory(DisplayName = "AddHatedInterestAsync uses given CancellationToken"), AutoData]
        public async Task AddHatedInterestAsync_Uses_Given_CancellationToken(string interest, CancellationToken cancellationToken)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.AddHatedInterestAsync(interest, cancellationToken);
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Theory(DisplayName = "AddHatedInterestAsync throws SoulseekClientException when write throws"), AutoData]
        public async Task AddHatedInterestAsync_Throws_SoulseekClientException_When_Write_Throws(string interest)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync(interest, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Theory(DisplayName = "AddHatedInterestAsync throws TimeoutException when write times out"), AutoData]
        public async Task AddHatedInterestAsync_Throws_TimeoutException_When_Write_Times_Out(string interest)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync(interest, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "AddHatedInterestAsync")]
        [Theory(DisplayName = "AddHatedInterestAsync throws OperationCanceledException when write is canceled"), AutoData]
        public async Task AddHatedInterestAsync_Throws_OperationCanceledException_When_Write_Is_Canceled(string interest)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.AddHatedInterestAsync(interest, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
