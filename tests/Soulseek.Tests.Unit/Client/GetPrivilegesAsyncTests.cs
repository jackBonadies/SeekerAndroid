// <copyright file="GetPrivilegesAsyncTests.cs" company="JP Dillingham">
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

    public class GetPrivilegesAsyncTests
    {
        [Trait("Category", "GetPrivilegesAsync")]
        [Theory(DisplayName = "GetPrivilegesAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetPrivilegesAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetPrivilegesAsync());

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetPrivilegesAsync")]
        [Fact(DisplayName = "GetPrivilegesAsync throws OperationCanceledException when canceled")]
        public async Task GetPrivilegesAsync_Throws_OperationCanceledException_When_Canceled()
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetPrivilegesAsync());

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "GetPrivilegesAsync")]
        [Fact(DisplayName = "GetPrivilegesAsync throws TimeoutException when timed out")]
        public async Task GetPrivilegesAsync_Throws_TimeoutException_When_Timed_Out()
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetPrivilegesAsync());

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetPrivilegesAsync")]
        [Fact(DisplayName = "GetPrivilegesAsync throws SoulseekClientException on error other than cancel or timeout")]
        public async Task GetPrivilegesAsync_Throws_SoulseekClientException_On_Error_Other_Than_Cancel_Or_Timeout()
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetPrivilegesAsync());

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
            }
        }

        [Trait("Category", "GetPrivilegesAsync")]
        [Theory(DisplayName = "GetPrivilegesAsync returns expected values"), AutoData]
        public async Task GetPrivilegesAsync_Returns_Expected_Values(int days)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(days));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(minorVersion: 9999, serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var result = await s.GetPrivilegesAsync();

                Assert.Equal(days, result);
            }
        }
    }
}
