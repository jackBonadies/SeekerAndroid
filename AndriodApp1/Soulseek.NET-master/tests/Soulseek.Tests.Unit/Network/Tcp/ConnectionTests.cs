// <copyright file="ConnectionTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Network.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Network.Tcp;
    using Xunit;
    using Xunit.Abstractions;

    public class ConnectionTests
    {
        private readonly Action<string> output;

        public ConnectionTests(ITestOutputHelper outputHelper)
        {
            output = (s) => outputHelper.WriteLine(s);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates properly"), AutoData]
        public void Instantiates_Properly(IPEndPoint endpoint)
        {
            Connection c = null;

            var ex = Record.Exception(() => c = new Connection(endpoint));

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(endpoint.Address, c.IPEndPoint.Address);
            Assert.Equal(endpoint.Port, c.IPEndPoint.Port);
            Assert.Equal(new ConnectionKey(endpoint), c.Key);
            Assert.Equal(ConnectionState.Pending, c.State);
            Assert.NotEqual(Guid.Empty, c.Id);
            Assert.Equal(ConnectionTypes.None, c.Type);
            Assert.NotEqual(new TimeSpan(), c.InactiveTime);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Type property sets"), AutoData]
        public void Type_Property_Sets(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                c.Type = ConnectionTypes.Direct | ConnectionTypes.Outbound;

                Assert.Equal(ConnectionTypes.Direct | ConnectionTypes.Outbound, c.Type);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given options"), AutoData]
        public void Instantiates_With_Given_Options(IPEndPoint endpoint)
        {
            var options = new ConnectionOptions(1, 1, 1);

            using (var c = new Connection(endpoint, options))
            {
                Assert.Equal(options, c.Options);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given TcpClient"), AutoData]
        public void Instantiates_With_Given_TcpClient(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ct = c.GetProperty<ITcpClient>("TcpClient");

                    Assert.Equal(t.Object, ct);
                }
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with null InactivityTimer if timeout is <= 0 and TcpClient is connected"), AutoData]
        public void Instantiates_With_Null_InactivityTimer_If_Timeout_Is_LEQ_0_And_TcpClient_Is_Connected(IPEndPoint endpoint)
        {
            var options = new ConnectionOptions(1, 1, 1, inactivityTimeout: -1);

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object, options: options))
                {
                    Assert.Null(c.GetProperty<System.Timers.Timer>("InactivityTimer"));
                }
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with started InactivityTimer if timeout is > 0 and TcpClient is connected"), AutoData]
        public void Instantiates_With_Started_InactivityTimer_If_Timeout_Is_GT_0_And_TcpClient_Is_Connected(IPEndPoint endpoint)
        {
            var options = new ConnectionOptions(1, 1, 1, inactivityTimeout: 1);

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object, options: options))
                {
                    var timer = c.GetProperty<System.Timers.Timer>("InactivityTimer");

                    Assert.NotNull(timer);
                    Assert.True(timer.Enabled);
                }
            }
        }

        [Trait("Category", "Dispose")]
        [Theory(DisplayName = "Disposes without throwing"), AutoData]
        public void Disposes_Without_Throwing(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                var ex = Record.Exception(() => c.Dispose());

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnects on inactivity"), AutoData]
        public async Task Disconnects_On_Inactivity(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);

                using (var c = new Connection(endpoint, tcpClient: t.Object, options: new ConnectionOptions(inactivityTimeout: 1)))
                {
                    await c.ConnectAsync();

                    var ex = await Record.ExceptionAsync(() => c.WaitForDisconnect());

                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex);
                    Assert.Equal(ConnectionState.Disconnected, c.State);
                }
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnects when disconnected without throwing"), AutoData]
        public void Disconnects_When_Not_Connected_Without_Throwing(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                c.SetProperty("State", ConnectionState.Disconnected);

                var ex = Record.Exception(() => c.Disconnect());

                Assert.Null(ex);
                Assert.Equal(ConnectionState.Disconnected, c.State);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnects when not disconnected"), AutoData]
        public void Disconnects_When_Not_Disconnected_Without_Throwing(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                c.SetProperty("State", ConnectionState.Connected);

                var ex = Record.Exception(() => c.Disconnect());

                Assert.Null(ex);
                Assert.Equal(ConnectionState.Disconnected, c.State);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnect raises StateChanged event"), AutoData]
        public void Disconnect_Raises_StateChanged_Event(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                c.SetProperty("State", ConnectionState.Connected);

                var eventArgs = new List<ConnectionStateChangedEventArgs>();

                c.StateChanged += (sender, e) => eventArgs.Add(e);

                c.Disconnect("foo");

                Assert.Equal(ConnectionState.Disconnected, c.State);

                // the event will fire twice, once on transition to Disconnecting, and again on transition to Disconnected.
                Assert.Equal(2, eventArgs.Count);
                Assert.Equal(ConnectionState.Disconnecting, eventArgs[0].CurrentState);
                Assert.Equal(ConnectionState.Disconnected, eventArgs[1].CurrentState);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnect raises Disconnected event"), AutoData]
        public void Disconnect_Raises_Disconnected_Event(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                c.SetProperty("State", ConnectionState.Connected);

                var eventArgs = new List<string>();

                c.Disconnected += (sender, e) => eventArgs.Add(e.Message);

                c.Disconnect("foo");

                Assert.Equal(ConnectionState.Disconnected, c.State);

                Assert.Single(eventArgs);
                Assert.Equal("foo", eventArgs[0]);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws when not pending or disconnected"), AutoData]
        public async Task Connect_Throws_When_Not_Pending_Or_Disconnected(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                c.SetProperty("State", ConnectionState.Connected);

                var ex = await Record.ExceptionAsync(() => c.ConnectAsync());

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect connects when not connected or transitioning"), AutoData]
        public async Task Connect_Connects_When_Not_Connected_Or_Transitioning(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ConnectAsync());

                    Assert.Null(ex);
                    Assert.Equal(ConnectionState.Connected, c.State);

                    t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws when timed out"), AutoData]
        public async Task Connect_Throws_When_Timed_Out(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()))
                    .Returns(Task.Run(() => Thread.Sleep(10000)));

                var o = new ConnectionOptions(connectTimeout: 0);
                using (var c = new Connection(endpoint, options: o, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ConnectAsync());

                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws OperationCanceledException when token is cancelled"), AutoData]
        public async Task Connect_Throws_OperationCanceledException_When_Token_Is_Cancelled(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()))
                    .Returns(Task.Run(() => Thread.Sleep(10000)));

                var o = new ConnectionOptions(connectTimeout: 10000);

                using (var c = new Connection(endpoint, options: o, tcpClient: t.Object))
                {
                    Exception ex = null;

                    using (var cts = new CancellationTokenSource())
                    {
                        cts.Cancel();
                        ex = await Record.ExceptionAsync(() => c.ConnectAsync(cts.Token));
                    }

                    Assert.NotNull(ex);
                    Assert.IsType<OperationCanceledException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect throws when TcpClient throws"), AutoData]
        public async Task Connect_Throws_When_TcpClient_Throws(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()))
                    .Returns(Task.Run(() => { throw new SocketException(); }));

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ConnectAsync());

                    Assert.NotNull(ex);
                    Assert.IsType<ConnectionException>(ex);
                    Assert.IsType<SocketException>(ex.InnerException);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect connects through proxy if configured"), AutoData]
        public async Task Connect_Connects_Through_Proxy_If_Configured(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);

                var proxy = new ProxyOptions("127.0.0.1", 1, "username", "password");
                var options = new ConnectionOptions(proxyOptions: proxy);

                using (var c = new Connection(endpoint, options: options, tcpClient: t.Object))
                {
                    var eventArgs = new List<EventArgs>();

                    c.Connected += (sender, e) => eventArgs.Add(e);

                    await c.ConnectAsync();

                    Assert.Equal(ConnectionState.Connected, c.State);
                    Assert.Single(eventArgs);

                    t.Verify(
                        m => m.ConnectThroughProxyAsync(
                            proxy.IPEndPoint.Address,
                            proxy.IPEndPoint.Port,
                            It.IsAny<IPAddress>(),
                            It.IsAny<int>(),
                            proxy.Username,
                            proxy.Password,
                            It.IsAny<CancellationToken?>()),
                        Times.Once);
                }
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect raises Connected event"), AutoData]
        public async Task Connect_Raises_Connected_Event(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var eventArgs = new List<EventArgs>();

                    c.Connected += (sender, e) => eventArgs.Add(e);

                    await c.ConnectAsync();

                    Assert.Equal(ConnectionState.Connected, c.State);
                    Assert.Single(eventArgs);

                    t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect raises StateChanged event"), AutoData]
        public async Task Connect_Raises_StateChanged_Event(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var eventArgs = new List<ConnectionStateChangedEventArgs>();

                    c.StateChanged += (sender, e) => eventArgs.Add(e);

                    await c.ConnectAsync();

                    Assert.Equal(ConnectionState.Connected, c.State);

                    // the event will fire twice, once on transition to Connecting, and again on transition to Connected.
                    Assert.Equal(2, eventArgs.Count);
                    Assert.Equal(ConnectionState.Connecting, eventArgs[0].CurrentState);
                    Assert.Equal(ConnectionState.Connected, eventArgs[1].CurrentState);

                    t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
                }
            }
        }

        [Trait("Category", "WaitForDisconnect")]
        [Theory(DisplayName = "WaitForDisconnect waits for disconnect"), AutoData]
        public async Task WaitForDisconnect_Waits_For_Disconnect(IPEndPoint endpoint, string message)
        {
            using (var c = new Connection(endpoint))
            {
                c.SetProperty("State", ConnectionState.Connected);
                c.Disconnect(message);

                var actualMessage = await c.WaitForDisconnect();

                Assert.Equal(message, actualMessage);
            }
        }

        [Trait("Category", "WaitForDisconnect")]
        [Theory(DisplayName = "WaitForDisconnect throws OperationCanceledException when cancelled"), AutoData]
        public async Task WaitForDisconnect_Throws_OperationCanceledException_When_Canceled(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                c.SetProperty("State", ConnectionState.Connected);

                var ct = new CancellationToken(canceled: true);

                var ex = await Record.ExceptionAsync(() => c.WaitForDisconnect(ct));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "Watchdog")]
        [Theory(DisplayName = "Watchdog disconnects when TcpClient disconnects"), AutoData]
        public async Task Watchdog_Disconnects_When_TcpClient_Disconnects(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(false);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var disconnectRaisedByWatchdog = false;
                    c.Disconnected += (sender, e) => disconnectRaisedByWatchdog = true;

                    var timer = c.GetProperty<System.Timers.Timer>("WatchdogTimer");
                    timer.Interval = 1;
                    timer.Reset();

                    await c.ConnectAsync();

                    Assert.Equal(ConnectionState.Connected, c.State);

                    var start = DateTime.UtcNow;

                    while (!disconnectRaisedByWatchdog)
                    {
                        if ((DateTime.UtcNow - start).TotalMilliseconds > 10000)
                        {
                            throw new Exception("Watchdog didn't disconnect in 10000ms");
                        }
                    }

                    Assert.True(disconnectRaisedByWatchdog);

                    t.Verify(m => m.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write throws given null bytes"), AutoData]
        public async Task Write_Throws_Given_Null_Bytes(IPEndPoint endpoint)
        {
            using (var c = new Connection(endpoint))
            {
                var ex = await Record.ExceptionAsync(() => c.WriteAsync(null));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write throws given zero bytes"), AutoData]
        public async Task Write_Throws_Given_Zero_Bytes(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(Array.Empty<byte>()));

                    Assert.NotNull(ex);
                    Assert.IsType<ArgumentException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write from stream throws given negative length")]
        [InlineData(-1)]
        [InlineData(-121412)]
        public async Task Write_From_Stream_Throws_Given_Negative_Length(long length)
        {
            var endpoint = new IPEndPoint(IPAddress.None, 0);

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(length, stream, (ct) => Task.CompletedTask));

                    Assert.NotNull(ex);
                    Assert.IsType<ArgumentException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write from stream throws given null stream"), AutoData]
        public async Task Write_From_Stream_Throws_Given_Null_Stream(IPEndPoint endpoint)
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(1, null, (ct) => Task.CompletedTask));

                    Assert.NotNull(ex);
                    Assert.IsType<ArgumentNullException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write from stream throws given unreadable stream"), AutoData]
        public async Task Write_From_Stream_Throws_Given_Unreadable_Stream(IPEndPoint endpoint)
        {
            using (var stream = new UnReadableWriteableStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                var t = new Mock<ITcpClient>();
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(1, stream, (ct) => Task.CompletedTask));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write throws if TcpClient is not connected"), AutoData]
        public async Task Write_Throws_If_TcpClient_Is_Not_Connected(IPEndPoint endpoint)
        {
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(false);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(new byte[] { 0x0, 0x1 }));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write from stream throws if TcpClient is not connected"), AutoData]
        public async Task Write_From_Stream_Throws_If_TcpClient_Is_Not_Connected(IPEndPoint endpoint, int length, Func<CancellationToken, Task> governor)
        {
            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(false);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(length, stream, governor));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write throws if connection is not connected"), AutoData]
        public async Task Write_Throws_If_Connection_Is_Not_Connected(IPEndPoint endpoint)
        {
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    c.SetProperty("State", ConnectionState.Disconnected);

                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(new byte[] { 0x0, 0x1 }));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write from stream throws if connection is not connected"), AutoData]
        public async Task Write_From_Stream_Throws_If_Connection_Is_Not_Connected(IPEndPoint endpoint, int length, Func<CancellationToken, Task> governor)
        {
            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    c.SetProperty("State", ConnectionState.Disconnected);

                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(length, stream, governor));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write throws if Stream throws"), AutoData]
        public async Task Write_Throws_If_Stream_Throws(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new SocketException());

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(new byte[] { 0x0, 0x1 }));

                    Assert.NotNull(ex);
                    Assert.IsType<ConnectionWriteException>(ex);
                    Assert.IsType<SocketException>(ex.InnerException);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write throws if Stream times out"), AutoData]
        public async Task Write_Throws_If_Stream_Times_Out(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(new byte[] { 0x0, 0x1 }));

                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write throws if Stream is canceled"), AutoData]
        public async Task Write_Throws_If_Stream_Is_Canceled(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(new byte[] { 0x0, 0x1 }));

                    Assert.NotNull(ex);
                    Assert.IsType<OperationCanceledException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write does not throw given good input and if Stream does not throw"), AutoData]
        public async Task Write_Does_Not_Throw_Given_Good_Input_And_If_Stream_Does_Not_Throw(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(new byte[] { 0x0, 0x1 }));

                    Assert.Null(ex);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write passes CancellationToken"), AutoData]
        public async Task Write_Passes_CancellationToken(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                var cancellationToken = CancellationToken.None;

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await c.WriteAsync(new byte[] { 0x0, 0x1 }, cancellationToken);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), cancellationToken), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write stream passes CancellationToken"), AutoData]
        public async Task Write_Stream_Passes_CancellationToken(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(1));

            var t = new Mock<ITcpClient>();

            var data = new byte[] { 0x0, 0x1 };

            using (var stream = new MemoryStream(data))
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                var cancellationToken = CancellationToken.None;

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await c.WriteAsync(1, stream, (ct) => Task.CompletedTask, cancellationToken);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), cancellationToken), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write stream does not throw null governor"), AutoData]
        public async Task Write_Stream_Handles_Null_Governor(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(1));

            var t = new Mock<ITcpClient>();

            var data = new byte[] { 0x0, 0x1 };

            using (var stream = new MemoryStream(data))
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(1, stream, governor: null));

                    Assert.Null(ex);
                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write resets LastActivityTime"), AutoData]
        public async Task Write_Resets_LastActivityTime(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await Task.Delay(100);
                    var time = c.InactiveTime;

                    await c.WriteAsync(new byte[] { 0x0, 0x1 });

                    var time2 = c.InactiveTime;

                    Assert.True(time2 < time);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write limits writes to send buffer size"), AutoData]
        public async Task Write_Stream_Limits_Writes_To_Send_Buffer_Size(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var t = new Mock<ITcpClient>();

            var data = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };

            using (var stream = new MemoryStream(data))
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object, options: new ConnectionOptions(writeBufferSize: 5)))
                {
                    await c.WriteAsync(10, stream, (ct) => Task.CompletedTask);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), 5, It.IsAny<CancellationToken>()), Times.Exactly(2));
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write from stream does not throw given good input and if Stream does not throw"), AutoData]
        public async Task Write_From_Stream_Does_Not_Throw_Given_Good_Input_And_If_Stream_Does_Not_Throw(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            var t = new Mock<ITcpClient>();

            var data = new byte[] { 0x0, 0x1 };

            using (var stream = new MemoryStream(data))
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.WriteAsync(data.Length, stream, (ct) => Task.CompletedTask));

                    Assert.Null(ex);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Write")]
        [Theory(DisplayName = "Write raises DataWritten event"), AutoData]
        public async Task Write_Raises_DataWritten_Event(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var eventArgs = new List<ConnectionDataEventArgs>();

                    c.DataWritten += (sender, e) => eventArgs.Add(e);

                    await c.WriteAsync(new byte[] { 0x0 });

                    Assert.Single(eventArgs);
                    Assert.Equal(1, eventArgs[0].CurrentLength);
                    Assert.Equal(1, eventArgs[0].TotalLength);

                    s.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read throws if TcpClient is not connected"), AutoData]
        public async Task Read_Throws_If_TcpClient_Is_Not_Connected(IPEndPoint endpoint)
        {
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(false);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream throws if TcpClient is not connected"), AutoData]
        public async Task Read_To_Stream_Throws_If_TcpClient_Is_Not_Connected(IPEndPoint endpoint, Func<CancellationToken, Task> governor)
        {
            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(false);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1, stream, governor));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read throws if connection is not connected"), AutoData]
        public async Task Read_Throws_If_Connection_Is_Not_Connected(IPEndPoint endpoint)
        {
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    t.Setup(m => m.Client).Returns(socket);
                    c.SetProperty("State", ConnectionState.Disconnected);

                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream throws if connection is not connected"), AutoData]
        public async Task Read_To_Stream_Throws_If_Connection_Is_Not_Connected(IPEndPoint endpoint, Func<CancellationToken, Task> governor)
        {
            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    t.Setup(m => m.Client).Returns(socket);
                    c.SetProperty("State", ConnectionState.Disconnected);

                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1, stream, governor));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read does not throw if length is long and larger than int"), AutoData]
        public async Task Read_Does_Not_Throw_If_Length_Is_Long_And_Larger_Than_Int(IPEndPoint endpoint, long length)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((int)length));

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(length));

                    Assert.Null(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream does not throw if length is long and larger than int"), AutoData]
        public async Task Read_To_Stream_Does_Not_Throw_If_Length_Is_Long_And_Larger_Than_Int(IPEndPoint endpoint, long length, Func<CancellationToken, Task> governor)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult((int)length));

            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(length, stream, governor));

                    Assert.Null(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read does not throw given good input and if Stream does not throw"), AutoData]
        public async Task Read_Does_Not_Throw_Given_Good_Input_And_If_Stream_Does_Not_Throw(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.Null(ex);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream does not throw given good input and if Stream does not throw"), AutoData]
        public async Task Read_To_Stream_Does_Not_Throw_Given_Good_Input_And_If_Stream_Does_Not_Throw(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1, stream, (ct) => Task.CompletedTask));

                    Assert.Null(ex);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read passes given CancellationToken"), AutoData]
        public async Task Read_Passes_Given_CancellationToken(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            var cancellationToken = CancellationToken.None;

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await c.ReadAsync(1, cancellationToken);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), cancellationToken), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read loops over Stream.ReadAsync on partial read"), AutoData]
        public async Task Read_Loops_Over_Stream_ReadAsync_On_Partial_Read(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await c.ReadAsync(3);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream passes CancellationToken"), AutoData]
        public async Task Read_To_Stream_Passes_CancellationToken(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            var cancellationToken = CancellationToken.None;

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await c.ReadAsync(1, stream, (ct) => Task.CompletedTask, cancellationToken);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), cancellationToken), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream does not throw given null governor"), AutoData]
        public async Task Read_To_Stream_Does_Not_Throw_Given_Governor(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1, outputStream: stream, governor: null));

                    Assert.Null(ex);
                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream loops over Stream.ReadAsync on partial read"), AutoData]
        public async Task Read_To_Stream_Loops_Over_Stream_ReadAsync_On_Partial_Read(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await c.ReadAsync(3, stream, (ct) => Task.CompletedTask);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read limits reads to buffer size"), AutoData]
        public async Task Read_To_Stream_Limits_Reads_To_Buffer_Size(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 5));

            var t = new Mock<ITcpClient>();

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object, options: new ConnectionOptions(readBufferSize: 5)))
                {
                    await c.ReadAsync(10, stream, (ct) => Task.CompletedTask);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), 5, It.IsAny<CancellationToken>()), Times.Exactly(2));
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read throws if Stream throws"), AutoData]
        public async Task Read_Throws_If_Stream_Throws(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new SocketException());

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.NotNull(ex);
                    Assert.IsType<ConnectionReadException>(ex);
                    Assert.IsType<SocketException>(ex.InnerException);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read throws if Stream times out"), AutoData]
        public async Task Read_Throws_If_Stream_Times_Out(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read throws if Stream is cancelled"), AutoData]
        public async Task Read_Throws_If_Stream_Is_Cancelled(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.NotNull(ex);
                    Assert.IsType<OperationCanceledException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read does not throw given zero length"), AutoData]
        public async Task Read_Does_Not_Throw_Given_Zero_Length(IPEndPoint endpoint)
        {
            var t = new Mock<ITcpClient>();
            t.Setup(m => m.Connected).Returns(true);

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(0));

                    Assert.Null(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read returns empty byte array given zero length"), AutoData]
        public async Task Read_Returns_Empty_Byte_Array_Given_Zero_Length(IPEndPoint endpoint)
        {
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var bytes = await c.ReadAsync(0);

                    Assert.Empty(bytes);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read throws given zero or negative length")]
        [InlineData(-12151353)]
        [InlineData(-1)]
        public async Task Read_Throws_Given_Zero_Or_Negative_Length(int length)
        {
            var t = new Mock<ITcpClient>();
            var endpoint = new IPEndPoint(IPAddress.None, 0);

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(length));

                    Assert.NotNull(ex);
                    Assert.IsType<ArgumentException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream throws given zero or negative length")]
        [InlineData(-12151353)]
        [InlineData(-1)]
        public async Task Read_To_Stream_Throws_Given_Zero_Or_Negative_Length(int length)
        {
            var t = new Mock<ITcpClient>();
            var endpoint = new IPEndPoint(IPAddress.None, 0);

            using (var stream = new MemoryStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(length, stream, (ct) => Task.CompletedTask));

                    Assert.NotNull(ex);
                    Assert.IsType<ArgumentException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream throws given null stream"), AutoData]
        public async Task Read_To_Stream_Throws_Given_Null_Stream(IPEndPoint endpoint, int length)
        {
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(length, null, (ct) => Task.CompletedTask));

                    Assert.NotNull(ex);
                    Assert.IsType<ArgumentNullException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read to stream throws given unwriteable stream"), AutoData]
        public async Task Read_To_Stream_Throws_Given_Unwriteable_Stream(IPEndPoint endpoint, int length)
        {
            var t = new Mock<ITcpClient>();

            using (var stream = new UnReadableWriteableStream())
            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(length, stream, (ct) => Task.CompletedTask));

                    Assert.NotNull(ex);
                    Assert.IsType<InvalidOperationException>(ex);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read disconnects if Stream returns 0"), AutoData]
        public async Task Read_Disconnects_If_Stream_Returns_0(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 0));

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.NotNull(ex);
                    Assert.IsType<ConnectionReadException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read raises DataRead event"), AutoData]
        public async Task Read_Raises_DataRead_Event(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var eventArgs = new List<ConnectionDataEventArgs>();

                    c.DataRead += (sender, e) => eventArgs.Add(e);

                    await c.ReadAsync(3);

                    Assert.Equal(3, eventArgs.Count);
                    Assert.Equal(1, eventArgs[0].CurrentLength);
                    Assert.Equal(3, eventArgs[0].TotalLength);
                    Assert.Equal(2, eventArgs[1].CurrentLength);
                    Assert.Equal(3, eventArgs[1].TotalLength);
                    Assert.Equal(3, eventArgs[2].CurrentLength);
                    Assert.Equal(3, eventArgs[2].TotalLength);

                    s.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read resets LastActivityTime"), AutoData]
        public async Task Read_Resets_LastActivityTime(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    await Task.Delay(100);
                    var time = c.InactiveTime;

                    await c.ReadAsync(3);

                    var time2 = c.InactiveTime;

                    Assert.True(time2 < time);
                }
            }
        }

        [Trait("Category", "Read")]
        [Theory(DisplayName = "Read times out on inactivity"), AutoData]
        public async Task Read_Times_Out_On_Inactivity(IPEndPoint endpoint)
        {
            var s = new Mock<INetworkStream>();
            s.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.Run(() => 1));

            var t = new Mock<ITcpClient>();

            using (var sock = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(sock);
                t.Setup(m => m.Connected).Returns(true);
                t.Setup(m => m.GetStream()).Returns(s.Object);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    c.GetProperty<System.Timers.Timer>("InactivityTimer").Interval = 100;

                    var ex = await Record.ExceptionAsync(() => c.ReadAsync(1));

                    Assert.NotNull(ex);
                    output(ex.Message);
                    Assert.IsType<ConnectionReadException>(ex);

                    Assert.Equal(ConnectionState.Disconnected, c.State);
                }
            }
        }

        [Trait("Category", "HandoffTcpClient")]
        [Theory(DisplayName = "HandoffTcpClient hands off"), AutoData]
        public void HandoffTcpClient_Hands_Off(IPEndPoint endpoint)
        {
            var t = new Mock<ITcpClient>();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.IP))
            {
                t.Setup(m => m.Client).Returns(socket);

                using (var c = new Connection(endpoint, tcpClient: t.Object))
                {
                    var first = c.GetProperty<ITcpClient>("TcpClient");

                    var tcpClient = c.HandoffTcpClient();

                    var second = c.GetProperty<ITcpClient>("TcpClient");

                    Assert.Equal(t.Object, tcpClient);
                    Assert.NotNull(first);
                    Assert.Null(second);
                }
            }
        }

        private class UnReadableWriteableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanWrite => false;

            public override bool CanSeek => throw new NotImplementedException();

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }
    }
}
