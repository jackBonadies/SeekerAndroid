// <copyright file="GetInterestRecommendationsAsyncTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetInterestRecommendationsAsyncTests
    {
        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync throws ArgumentException given null or whitespace interest")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetInterestRecommendationsAsync_Throws_ArgumentException_Given_Null_Or_Whitespace_Interest(string interest)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestRecommendationsAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetInterestRecommendationsAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetInterestRecommendationsAsync("interest"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync returns expected info"), AutoData]
        public async Task GetInterestRecommendationsAsync_Returns_Expected_Info(string interest, List<(string Recommendation, int Count)> recommendations)
        {
            var result = new InterestRecommendationsResponse(interest, recommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var (returnedInterest, recommended, notRecommended) = await s.GetInterestRecommendationsAsync(interest);

                Assert.Equal(interest, returnedInterest);
                Assert.Equal(recommendations.OrderByDescending(r => r.Count), recommended.Select(r => (r.Interest, r.Count)));
                Assert.Empty(notRecommended);
            }
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync splits and sorts recommendations"), AutoData]
        public async Task GetInterestRecommendationsAsync_Splits_And_Sorts_Recommendations(string interest)
        {
            var recommendations = new List<(string Recommendation, int Count)>
            {
                ("low", 1),
                ("zero", 0),
                ("high", 5),
                ("slightly negative", -3),
                ("very negative", -7),
            };

            var result = new InterestRecommendationsResponse(interest, recommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var (_, recommended, notRecommended) = await s.GetInterestRecommendationsAsync(interest);

                Assert.Equal(new[] { ("high", 5), ("low", 1) }, recommended.Select(r => (r.Interest, r.Count)));
                Assert.Equal(new[] { ("very negative", -7), ("slightly negative", -3), ("zero", 0) }, notRecommended.Select(r => (r.Interest, r.Count)));
            }
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync returns interest from response"), AutoData]
        public async Task GetInterestRecommendationsAsync_Returns_Interest_From_Response(string interest, string responseInterest, List<(string Recommendation, int Count)> recommendations)
        {
            var result = new InterestRecommendationsResponse(responseInterest, recommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var (returnedInterest, _, _) = await s.GetInterestRecommendationsAsync(interest);

                Assert.Equal(responseInterest, returnedInterest);
            }
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync uses given CancellationToken"), AutoData]
        public async Task GetInterestRecommendationsAsync_Uses_Given_CancellationToken(string interest, List<(string Recommendation, int Count)> recommendations)
        {
            var cancellationToken = new CancellationToken();
            var result = new InterestRecommendationsResponse(interest, recommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GetInterestRecommendationsAsync(interest, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken));
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GetInterestRecommendationsAsync_Throws_SoulseekClientException_On_Throw(string interest, List<(string Recommendation, int Count)> recommendations)
        {
            var result = new InterestRecommendationsResponse(interest, recommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestRecommendationsAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetInterestRecommendationsAsync_Throws_TimeoutException_On_Timeout(string interest, List<(string Recommendation, int Count)> recommendations)
        {
            var result = new InterestRecommendationsResponse(interest, recommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestRecommendationsAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetInterestRecommendationsAsync")]
        [Theory(DisplayName = "GetInterestRecommendationsAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetInterestRecommendationsAsync_Throws_OperationCanceledException_On_Cancellation(string interest, List<(string Recommendation, int Count)> recommendations)
        {
            var result = new InterestRecommendationsResponse(interest, recommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<InterestRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetInterestRecommendationsAsync(interest));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
