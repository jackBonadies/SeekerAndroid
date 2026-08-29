// <copyright file="GetUserStatisticsAsyncTests.cs" company="JP Dillingham">
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
    using Xunit;

    public class GetUserStatisticsAsyncTests
    {
        [Trait("Category", "GetUserStatisticsAsync")]
        [Theory(DisplayName = "GetUserStatisticsAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetUserStatisticsAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatisticsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetUserStatisticsAsync")]
        [Theory(DisplayName = "GetUserStatisticsAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetUserStatisticsAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatisticsAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetUserStatisticsAsync")]
        [Theory(DisplayName = "GetUserStatisticsAsync returns expected info"), AutoData]
        public async Task GetUserStatisticsAsync_Returns_Expected_Info(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var result = new UserStatistics(username, averageSpeed, uploadCount, fileCount, directoryCount);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatistics>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var stats = await s.GetUserStatisticsAsync(username);

                Assert.Equal(username, stats.Username);
                Assert.Equal(averageSpeed, stats.AverageSpeed);
                Assert.Equal(uploadCount, stats.UploadCount);
                Assert.Equal(fileCount, stats.FileCount);
                Assert.Equal(directoryCount, stats.DirectoryCount);
            }
        }

        [Trait("Category", "GetUserStatisticsAsync")]
        [Theory(DisplayName = "GetUserStatisticsAsync uses given CancellationToken"), AutoData]
        public async Task GetUserStatisticsAsync_Uses_Given_CancellationToken(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var cancellationToken = new CancellationToken();
            var result = new UserStatistics(username, averageSpeed, uploadCount, fileCount, directoryCount);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatistics>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.GetUserStatisticsAsync(username, cancellationToken);
            }

            serverConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken));
        }

        [Trait("Category", "GetUserStatisticsAsync")]
        [Theory(DisplayName = "GetUserStatisticsAsync throws SoulseekClientException on throw"), AutoData]
        public async Task GetUserStatisticsAsync_Throws_SoulseekClientExceptionn_On_Throw(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var result = new UserStatistics(username, averageSpeed, uploadCount, fileCount, directoryCount);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatistics>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionException("foo"));

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatisticsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "GetUserStatisticsAsync")]
        [Theory(DisplayName = "GetUserStatisticsAsync throws TimeoutException on timeout"), AutoData]
        public async Task GetUserStatisticsAsync_Throws_TimeoutException_On_Timeout(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var result = new UserStatistics(username, averageSpeed, uploadCount, fileCount, directoryCount);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatistics>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatisticsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetUserStatisticsAsync")]
        [Theory(DisplayName = "GetUserStatisticsAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task GetUserStatisticsAsync_Throws_OperationCanceledException_On_Cancellation(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var result = new UserStatistics(username, averageSpeed, uploadCount, fileCount, directoryCount);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserStatistics>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(result));

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: serverConn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserStatisticsAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }
    }
}
