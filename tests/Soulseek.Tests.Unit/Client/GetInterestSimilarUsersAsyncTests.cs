// <copyright file="GetInterestSimilarUsersAsyncTests.cs" company="JP Dillingham">
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

    public class GetInterestSimilarUsersAsyncTests
    {
        [Trait("Category", "GetInterestSimilarUsersAsync")]
        [Theory(DisplayName = "GetInterestSimilarUsersAsync throws ArgumentException given null or whitespace interest")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetInterestSimilarUsersAsync_Throws_ArgumentException_Given_Null_Or_Whitespace_Interest(string interest)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestSimilarUsersAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetInterestSimilarUsersAsync")]
        [Theory(DisplayName = "GetInterestSimilarUsersAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetInterestSimilarUsersAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetInterestSimilarUsersAsync("interest"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetInterestSimilarUsersAsync")]
        [Theory(DisplayName = "GetInterestSimilarUsersAsync returns expected info"), AutoData]
        public async Task GetInterestSimilarUsersAsync_Returns_Expected_Info(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var (returnedInterest, similarUsers) = await s.GetInterestSimilarUsersAsync(interest);

                Assert.Equal(interest, returnedInterest);
                Assert.Equal(usernames, similarUsers);
            }
        }

        [Trait("Category", "GetInterestSimilarUsersAsync")]
        [Theory(DisplayName = "GetInterestSimilarUsersAsync uses given CancellationToken"), AutoData]
        public async Task GetInterestSimilarUsersAsync_Uses_Given_CancellationToken(string interest, List<string> usernames)
        {
            var cancellationToken = new CancellationToken();
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GetInterestSimilarUsersAsync(interest, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken));
        }

        [Trait("Category", "GetInterestSimilarUsersAsync")]
        [Theory(DisplayName = "GetInterestSimilarUsersAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GetInterestSimilarUsersAsync_Throws_SoulseekClientException_On_Throw(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestSimilarUsersAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetInterestSimilarUsersAsync")]
        [Theory(DisplayName = "GetInterestSimilarUsersAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetInterestSimilarUsersAsync_Throws_TimeoutException_On_Timeout(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestSimilarUsersAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetInterestSimilarUsersAsync")]
        [Theory(DisplayName = "GetInterestSimilarUsersAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetInterestSimilarUsersAsync_Throws_OperationCanceledException_On_Cancellation(string interest, List<string> usernames)
        {
            var result = new InterestSimilarUsersResponse(interest, usernames);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestSimilarUsersResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestSimilarUsersAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
