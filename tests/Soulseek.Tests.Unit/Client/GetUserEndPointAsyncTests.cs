// <copyright file="GetUserEndPointAsyncTests.cs" company="JP Dillingham">
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
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class GetUserEndPointAsyncTests
    {
        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync throws ArgumentException on bad username")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("")]
        public async Task GetUserEndPointAsync_Throws_ArgumentException_On_Null_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserEndPointAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync throws InvalidOperationException if not connected and logged in")]
        [InlineData(SoulseekClientStates.None)]
        [InlineData(SoulseekClientStates.Disconnected)]
        [InlineData(SoulseekClientStates.Connected)]
        [InlineData(SoulseekClientStates.LoggedIn)]
        public async Task GetUserEndPointAsync_Throws_InvalidOperationException_If_Logged_In(SoulseekClientStates state)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", state);

                var ex = await Record.ExceptionAsync(() => s.GetUserEndPointAsync("a"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync throws OperationCanceledException when canceled"), AutoData]
        public async Task GetUserEndPointAsync_Throws_OperationCanceledException_When_Canceled(string username)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            using (var s = new SoulseekClient(waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserEndPointAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync throws TimeoutException when timed out"), AutoData]
        public async Task GetUserEndPointAsync_Throws_TimeoutException_When_Timed_Out(string username)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserEndPointAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync throws UserEndPointException on error other than cancel or timeout"), AutoData]
        public async Task GetUserEndPointAsync_Throws_UserEndPointException_On_Error_Other_Than_Cancel_Or_Timeout(string username)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            using (var s = new SoulseekClient(waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserEndPointAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserEndPointException>(ex);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync throws UserOfflineException when peer is offline"), AutoData]
        public async Task GetUserEndPointAsync_Throws_UserOfflineException_When_Peer_Is_Offline(string username)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, IPAddress.Parse("0.0.0.0"), 0)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserEndPointAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync returns expected values"), AutoData]
        public async Task GetUserEndPointAsync_Returns_Expected_Values(string username, IPAddress ip, int port)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, ip, port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var addr = await s.GetUserEndPointAsync(username);

                Assert.Equal(ip, addr.Address);
                Assert.Equal(port, addr.Port);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync returns cached endpoint if cached"), AutoData]
        public async Task GetUserEndPointAsync_Returns_Cached_Endpoint_If_Cached(string username, IPEndPoint endpoint)
        {
            var cache = new Mock<IUserEndPointCache>();
            cache.Setup(m => m.TryGet(username, out endpoint))
                .Returns(true);

            using (var s = new SoulseekClient(new SoulseekClientOptions(userEndPointCache: cache.Object)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var addr = await s.GetUserEndPointAsync(username);

                Assert.Equal(endpoint.Address, addr.Address);
                Assert.Equal(endpoint.Port, addr.Port);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync returns cached endpoint if cached by previous thread"), AutoData]
        public async Task GetUserEndPointAsync_Returns_Cached_Endpoint_If_Cached_By_Previous_Thread(string username, IPEndPoint endpoint)
        {
            var cache = new Mock<IUserEndPointCache>();
            cache.Setup(m => m.TryGet(username, out endpoint))
                .Returns(false);

            using (var sem = new SemaphoreSlim(1, 1))
            {
                using (var s = new SoulseekClient(new SoulseekClientOptions(userEndPointCache: cache.Object)))
                {
                    var semaphores = s.GetProperty<ConcurrentDictionary<string, SemaphoreSlim>>("UserEndPointSemaphores");
                    semaphores.TryAdd(username, sem);

                    // wait the sempahore to make the test code wait
                    var wait = sem.WaitAsync();

                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    // invoke, but dont await.  this will wait the semaphore
                    var addrWait = s.GetUserEndPointAsync(username);

                    // simulate a cache update while we were waiting
                    cache.Setup(m => m.TryGet(username, out endpoint))
                        .Returns(true);

                    // release the semaphore to cause the code above to enter the critical section
                    sem.Release();
                    await wait;

                    // cache hit and return
                    var addr = await addrWait;

                    Assert.Equal(endpoint.Address, addr.Address);
                    Assert.Equal(endpoint.Port, addr.Port);
                }
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync returns expected values on cache miss"), AutoData]
        public async Task GetUserEndPointAsync_Returns_Expected_Values_On_Cache_Miss(string username, IPEndPoint endpoint)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var cache = new Mock<IUserEndPointCache>();
            cache.Setup(m => m.TryGet(username, out endpoint))
                .Returns(false);

            using (var s = new SoulseekClient(serverConnection: conn.Object, waiter: waiter.Object, options: new SoulseekClientOptions(userEndPointCache: cache.Object)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var addr = await s.GetUserEndPointAsync(username);

                Assert.Equal(endpoint.Address, addr.Address);
                Assert.Equal(endpoint.Port, addr.Port);
            }
        }

        [Trait("Category", "GetUserEndPointAsync")]
        [Theory(DisplayName = "GetUserEndPointAsync throws UserEndPointCacheException when cache operation throws"), AutoData]
        public async Task GetUserEndPointAsync_Throws_UserEndPointCacheException_When_Cache_Operation_Throws(string username)
        {
            var exception = new Exception();
            var endpoint = new IPEndPoint(0, 0);

            var cache = new Mock<IUserEndPointCache>();
            cache.Setup(m => m.TryGet(username, out endpoint))
                .Throws(exception);

            using (var s = new SoulseekClient(new SoulseekClientOptions(userEndPointCache: cache.Object)))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.GetUserEndPointAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserEndPointCacheException>(ex);
                Assert.Equal(exception, ex.InnerException);
            }
        }
    }
}
