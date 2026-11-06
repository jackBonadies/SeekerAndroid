// <copyright file="GetGlobalRecommendationsAsyncTests.cs" company="JP Dillingham">
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

    public class GetGlobalRecommendationsAsyncTests
    {
        [Trait("Category", "GetGlobalRecommendationsAsync")]
        [Theory(DisplayName = "GetGlobalRecommendationsAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetGlobalRecommendationsAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetGlobalRecommendationsAsync());

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetGlobalRecommendationsAsync")]
        [Fact(DisplayName = "GetGlobalRecommendationsAsync combines, splits, and sorts recommendations")]
        public async Task GetGlobalRecommendationsAsync_Combines_Splits_And_Sorts_Recommendations()
        {
            var recommendations = new List<(string Recommendation, int Count)>
            {
                ("low", 1),
                ("high", 10),
                ("negative in recommendations", -2),
                ("zero", 0),
            };

            var unrecommendations = new List<(string Unrecommendation, int Count)>
            {
                ("very negative", -5),
                ("positive in unrecommendations", 3),
                ("slightly negative", -1),
            };

            var result = new GlobalRecommendationsResponse(recommendations, unrecommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GlobalRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var (recommended, notRecommended) = await s.GetGlobalRecommendationsAsync();

                Assert.Equal(new[] { ("high", 10), ("positive in unrecommendations", 3), ("low", 1) }, recommended.Select(r => (r.Interest, r.Count)));
                Assert.Equal(new[] { ("very negative", -5), ("negative in recommendations", -2), ("slightly negative", -1), ("zero", 0) }, notRecommended.Select(r => (r.Interest, r.Count)));
            }
        }

        [Trait("Category", "GetGlobalRecommendationsAsync")]
        [Theory(DisplayName = "GetGlobalRecommendationsAsync uses given CancellationToken"), AutoData]
        public async Task GetGlobalRecommendationsAsync_Uses_Given_CancellationToken(List<(string Recommendation, int Count)> recommendations, List<(string Unrecommendation, int Count)> unrecommendations)
        {
            var cancellationToken = new CancellationToken();
            var result = new GlobalRecommendationsResponse(recommendations, unrecommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GlobalRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GetGlobalRecommendationsAsync(cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken));
        }

        [Trait("Category", "GetGlobalRecommendationsAsync")]
        [Theory(DisplayName = "GetGlobalRecommendationsAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GetGlobalRecommendationsAsync_Throws_SoulseekClientException_On_Throw(List<(string Recommendation, int Count)> recommendations, List<(string Unrecommendation, int Count)> unrecommendations)
        {
            var result = new GlobalRecommendationsResponse(recommendations, unrecommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GlobalRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetGlobalRecommendationsAsync());

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetGlobalRecommendationsAsync")]
        [Theory(DisplayName = "GetGlobalRecommendationsAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetGlobalRecommendationsAsync_Throws_TimeoutException_On_Timeout(List<(string Recommendation, int Count)> recommendations, List<(string Unrecommendation, int Count)> unrecommendations)
        {
            var result = new GlobalRecommendationsResponse(recommendations, unrecommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GlobalRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetGlobalRecommendationsAsync());

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetGlobalRecommendationsAsync")]
        [Theory(DisplayName = "GetGlobalRecommendationsAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetGlobalRecommendationsAsync_Throws_OperationCanceledException_On_Cancellation(List<(string Recommendation, int Count)> recommendations, List<(string Unrecommendation, int Count)> unrecommendations)
        {
            var result = new GlobalRecommendationsResponse(recommendations, unrecommendations);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<GlobalRecommendationsResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetGlobalRecommendationsAsync());

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
