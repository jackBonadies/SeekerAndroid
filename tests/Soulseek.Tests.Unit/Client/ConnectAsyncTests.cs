// <copyright file="ConnectAsyncTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ConnectAsyncTests
    {
        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Throws ArgumentException on bad credentials")]
        [InlineData(null, "a")]
        [InlineData("", "a")]
        [InlineData("a", null)]
        [InlineData("a", "")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public async Task Throws_ArgumentException_On_Bad_Credentials(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Address throws AddressException on bad address"), AutoData]
        public async Task Address_Throws_ArgumentException_On_Bad_Address(string address)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, 1, "u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<AddressException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Throws ListenException on bad listen port")]
        public async Task Address_Throws_ListenException_On_Bad_Listen_Port()
        {
            var port = Mocks.Port;

            using (var s = new SoulseekClient(new SoulseekClientOptions(enableListener: true, listenPort: port)))
            {
                Listener listener = null;

                try
                {
                    listener = new Listener(IPAddress.Any, port, new ConnectionOptions());
                    listener.Start();

                    var ex = await Record.ExceptionAsync(() => s.ConnectAsync("u", "p"));

                    Assert.NotNull(ex);
                    Assert.IsType<ListenException>(ex);
                }
                finally
                {
                    listener.Stop();
                }
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Address throws ArgumentOutOfRangeException on bad port")]
        [InlineData(-1)]
        [InlineData(65536)]
        public async Task Address_Throws_ArgumentException_On_Bad_Port(int port)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync("127.0.0.01", port, "u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Address throws ArgumentException on bad input")]
        [InlineData("127.0.0.1", 1, null, "a")]
        [InlineData("127.0.0.1", 1, "", "a")]
        [InlineData("127.0.0.1", 1, "a", null)]
        [InlineData("127.0.0.1", 1, "a", "")]
        [InlineData("127.0.0.1", 1, "", "")]
        [InlineData("127.0.0.1", 1, null, null)]
        [InlineData(null, 1, "user", "pass")]
        [InlineData("", 1, "user", "pass")]
        [InlineData(" ", 1, "user", "pass")]
        public async Task Address_Throws_ArgumentException_On_Bad_Input(string address, int port, string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(address, port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Throws InvalidOperationException if connected"), AutoData]
        public async Task Throws_InvalidOperationException_When_Already_Connected(string username, string password)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Address throws InvalidOperationException if connected"), AutoData]
        public async Task Address_Throws_InvalidOperationException_If_Connected(IPEndPoint endpoint, string username, string password)
        {
            var (client, _) = GetFixture();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Throws InvalidOperationException if connecting"), AutoData]
        public async Task Throws_InvalidOperationException_If_Connecting(string username, string password)
        {
            var (client, _) = GetFixture();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connecting);

                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Address throws InvalidOperationException if connecting"), AutoData]
        public async Task Address_Throws_InvalidOperationException_If_Connecting(IPEndPoint endpoint, string username, string password)
        {
            var (client, _) = GetFixture();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connecting);

                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Throws InvalidOperationException if logging in"), AutoData]
        public async Task Throws_InvalidOperationException_If_Logging_In(string username, string password)
        {
            var (client, _) = GetFixture();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.LoggingIn);

                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Address throws InvalidOperationException if logging in"), AutoData]
        public async Task Address_Throws_InvalidOperationException_If_Logging_In(IPEndPoint endpoint, string username, string password)
        {
            var (client, _) = GetFixture();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.LoggingIn);

                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Throws when ServerConnection throws")]
        public async Task Throws_When_ServerConnection_Throws()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>())).Throws(new ConnectionException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectAsync("u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Throws TimeoutException when connection times out")]
        public async Task Throws_TimeoutException_When_Connection_Times_Out()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>())).Throws(new TimeoutException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectAsync("u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Throws OperationCanceledException when canceled")]
        public async Task Throws_OperationCanceledException_When_Canceled()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectAsync("u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Sets state to Disconnected on failure")]
        public async Task Sets_State_To_Disconnected_On_Failure()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>())).Throws(new ConnectionException());

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectAsync("u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);

                Assert.Equal(SoulseekClientStates.Disconnected, client.State);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connects, logs in, and sets listen port"), AutoData]
        public async Task Connects_And_Logs_In(string username, string password)
        {
            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectAsync(username, password);
            }

            mocks.ServerConnection.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()));

            var expectedBytes = new LoginRequest(username, password).ToByteArray()
                .Concat(new SetListenPortCommand(client.Options.ListenPort).ToByteArray())
                .ToArray();

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(msg => msg.Matches(expectedBytes)), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Sets state to Connected | LoggedIn on success")]
        public async Task Sets_State_To_Connected_LoggedIn_On_Success()
        {
            var (client, _) = GetFixture();

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectAsync("u", "p"));

                Assert.Null(ex);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, client.State);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Raises correct StateChanged sequence on success")]
        public async Task Raises_Correct_StateChanged_Sequence_On_Success()
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection
                .Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask)
                .Callback(() => client.InvokeMethod("ChangeState", SoulseekClientStates.Connected, "Connected", null));

            using (client)
            {
                var events = new List<SoulseekClientStateChangedEventArgs>();

                client.StateChanged += (e, args) => events.Add(args);

                var ex = await Record.ExceptionAsync(() => client.ConnectAsync("u", "p"));

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, client.State);

                Assert.Equal(SoulseekClientStates.Connecting, events[0].State);
                Assert.Equal(SoulseekClientStates.Connected, events[1].State);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggingIn, events[2].State);
                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, events[3].State);
            }
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "ServerConnection_Connected raises StateChanged event")]
        public void ServerConnection_Connected_Raises_StateChanged_Event()
        {
            SoulseekClientStateChangedEventArgs args = null;

            using (var s = new SoulseekClient())
            {
                s.StateChanged += (sender, e) => args = e;

                s.InvokeMethod("ServerConnection_Connected", null, EventArgs.Empty);

                Assert.NotNull(args);
                Assert.Equal(SoulseekClientStates.Connected, args.State);
            }
        }

        [Trait("Category", "ConnectInternal")]
        [Theory(DisplayName = "Exits gracefully if already connected and logged in"), AutoData]
        public async Task Exits_Gracefully_If_Already_Connected_And_Logged_In(IPEndPoint endpoint, string username, string password)
        {
            var fired = false;

            using (var s = new SoulseekClient())
            {
                s.StateChanged += (sender, e) => fired = true;
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("ConnectInternalAsync", endpoint.Address.ToString(), endpoint, username, password, null);

                await task;

                Assert.False(fired);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Uses given CancellationToken"), AutoData]
        public async Task Uses_Given_CancellationToken(string user, string password)
        {
            var cancellationToken = new CancellationToken();

            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectAsync(user, password, cancellationToken);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, client.State);
                Assert.Equal(user, client.Username);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.AtLeastOnce);
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Address uses given CancellationToken"), AutoData]
        public async Task Address_uses_Given_CancellationToken(IPEndPoint endpoint, string user, string password)
        {
            var cancellationToken = new CancellationToken();

            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectAsync(endpoint.Address.ToString(), endpoint.Port, user, password, cancellationToken);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, client.State);
                Assert.Equal(user, client.Username);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.AtLeastOnce);
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Starts listener on success"), AutoData]
        public async Task Starts_Listener_On_Success(string user, string password)
        {
            var port = Mocks.Port;
            var (client, _) = GetFixture(new SoulseekClientOptions(listenPort: port));

            using (client)
            {
                await client.ConnectAsync(user, password);

                Assert.NotNull(client.Listener);
                Assert.Equal(port, client.Listener.Port);
                Assert.True(client.Listener.Listening);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Sets listen port on success"), AutoData]
        public async Task Sets_Listen_Port_On_Success(string user, string password)
        {
            var port = Mocks.Port;
            var (client, mocks) = GetFixture(new SoulseekClientOptions(listenPort: port));

            using (client)
            {
                await client.ConnectAsync(user, password);

                Assert.Equal(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, client.State);
                Assert.Equal(user, client.Username);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<SetListenPortCommand>(), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Configures distributed network with parent info on success"), AutoData]
        public async Task LoginAsync_Configures_Distributed_Network_With_Parent_Info_On_Success(string user, string password)
        {
            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectAsync(user, password);
            }

            mocks.DistributedConnectionManager.Verify(m => m.UpdateStatusAsync(It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Sets PrivateRoomToggle on success"), AutoData]
        public async Task Sets_PrivateRoomToggle_On_Success(string user, string password)
        {
            var (client, mocks) = GetFixture();

            using (client)
            {
                await client.ConnectAsync(user, password);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<PrivateRoomToggle>(), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Raises ServerInfoReceived on login"), AutoData]
        public async Task Raises_ServerInfoReceived_On_Login(string user, string password, bool isSupporter)
        {
            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty, isSupporter: isSupporter)));

            using (client)
            {
                ServerInfo args = null;

                client.ServerInfoReceived += (sender, e) => args = e;

                await client.ConnectAsync(user, password);

                Assert.NotNull(args);

                // IsSupporter is the only property we can expect to be set at this stage, the rest are
                // delivered/set out of band now (originally they were required during login)
                Assert.Equal(isSupporter, args.IsSupporter);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Sets ServerInfo on login"), AutoData]
        public async Task Sets_ServerInfo_On_Login(string user, string password, bool isSupporter)
        {
            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(true, string.Empty, isSupporter: isSupporter)));

            using (client)
            {
                await client.ConnectAsync(user, password);

                Assert.Equal(isSupporter, client.ServerInfo.IsSupporter);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Disconnects and throws LoginRejectedException on login rejection"), AutoData]
        public async Task Disconnects_And_Throws_LoginException_On_Login_Rejection(string user, string password)
        {
            var (client, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new LoginResponse(false, string.Empty)));

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(user, password));

                Assert.NotNull(ex);
                Assert.IsType<LoginRejectedException>(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, client.State);
                Assert.Null(client.Username);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Throws SoulseekClientException on message write exception"), AutoData]
        public async Task LoginAsync_Throws_SoulseekClientException_On_Message_Write_Exception(string user, string password)
        {
            var (client, mocks) = GetFixture();

            mocks.ServerConnection.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Exception>(new ConnectionWriteException()));

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ConnectAsync(user, password));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        private static (SoulseekClient client, Mocks Mocks) GetFixture(SoulseekClientOptions clientOptions = null)
        {
            var mocks = new Mocks();
            var client = new SoulseekClient(
                distributedConnectionManager: mocks.DistributedConnectionManager.Object,
                connectionFactory: mocks.ConnectionFactory.Object,
                waiter: mocks.Waiter.Object,
                options: clientOptions ?? new SoulseekClientOptions(enableListener: false));

            return (client, mocks);
        }

        private class Mocks
        {
            public Mocks()
            {
                ConnectionFactory = new Mock<IConnectionFactory>();
                ConnectionFactory.Setup(m => m.GetServerConnection(
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                    It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<ConnectionOptions>(),
                    It.IsAny<ITcpClient>()))
                    .Returns(ServerConnection.Object);

                DistributedConnectionManager = new Mock<IDistributedConnectionManager>();
                DistributedConnectionManager.Setup(m => m.BranchLevel).Returns(0);
                DistributedConnectionManager.Setup(m => m.BranchRoot).Returns(string.Empty);

                Waiter.Setup(m => m.Wait<LoginResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new LoginResponse(true, string.Empty)));
            }

            private static readonly Random Rng = new Random();
            public static int Port => Rng.Next(1024, IPEndPoint.MaxPort);

            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public Mock<IConnectionFactory> ConnectionFactory { get; }
            public Mock<IDistributedConnectionManager> DistributedConnectionManager { get; }
        }
    }
}
