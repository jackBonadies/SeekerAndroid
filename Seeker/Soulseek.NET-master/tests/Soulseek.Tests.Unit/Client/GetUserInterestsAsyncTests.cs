// <copyright file="GetUserInterestsAsyncTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetUserInterestsAsyncTests
    {
        [Trait("Category", "GetUserInterestsAsync")]
        [Theory(DisplayName = "GetUserInterestsAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetUserInterestsAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserInterestsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetUserInterestsAsync")]
        [Theory(DisplayName = "GetUserInterestsAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetUserInterestsAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetUserInterestsAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetUserInterestsAsync")]
        [Theory(DisplayName = "GetUserInterestsAsync returns expected info"), AutoData]
        public async Task GetUserInterestsAsync_Returns_Expected_Info(string username, List<string> likes, List<string> hates)
        {
            var result = new UserInterestsResponse(username, likes, hates);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInterestsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var interests = await s.GetUserInterestsAsync(username);

                Assert.Equal(username, interests.Username);
                Assert.Equal(likes, interests.Likes);
                Assert.Equal(hates, interests.Hates);
            }
        }

        [Trait("Category", "GetUserInterestsAsync")]
        [Theory(DisplayName = "GetUserInterestsAsync uses given CancellationToken"), AutoData]
        public async Task GetUserInterestsAsync_Uses_Given_CancellationToken(string username, List<string> likes, List<string> hates)
        {
            var cancellationToken = new CancellationToken();
            var result = new UserInterestsResponse(username, likes, hates);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInterestsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GetUserInterestsAsync(username, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken));
        }

        [Trait("Category", "GetUserInterestsAsync")]
        [Theory(DisplayName = "GetUserInterestsAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GetUserInterestsAsync_Throws_SoulseekClientException_On_Throw(string username, List<string> likes, List<string> hates)
        {
            var result = new UserInterestsResponse(username, likes, hates);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInterestsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserInterestsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetUserInterestsAsync")]
        [Theory(DisplayName = "GetUserInterestsAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetUserInterestsAsync_Throws_TimeoutException_On_Timeout(string username, List<string> likes, List<string> hates)
        {
            var result = new UserInterestsResponse(username, likes, hates);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInterestsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserInterestsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetUserInterestsAsync")]
        [Theory(DisplayName = "GetUserInterestsAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetUserInterestsAsync_Throws_OperationCanceledException_On_Cancellation(string username, List<string> likes, List<string> hates)
        {
            var result = new UserInterestsResponse(username, likes, hates);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserInterestsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserInterestsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
