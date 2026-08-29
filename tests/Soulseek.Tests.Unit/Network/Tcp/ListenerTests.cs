// <copyright file="ListenerTests.cs" company="JP Dillingham">
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
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ListenerTests
    {
        private static readonly Random RNG = new Random();

        private static int GetPort()
        {
            return 50000 + RNG.Next(1, 9999);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            Assert.Equal(IPAddress.Any, l.IPAddress);
            Assert.Equal(port, l.Port);
            Assert.Equal(options, l.ConnectionOptions);

            Assert.False(l.Listening);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Uses default ConnectionOptions if none supplied")]
        public void Uses_Default_ConnectionOptions_If_None_Supplied()
        {
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, connectionOptions: null, tcpListener.Object);

            Assert.NotNull(l.ConnectionOptions);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Uses supplied TcpListener")]
        public void Uses_Supplied_TcpListener()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var listener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener: listener.Object);

            var val = l.GetProperty<ITcpListener>("TcpListener");

            Assert.Equal(listener.Object, val);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Creates TcpListener if none supplied")]
        public void Creates_TcpListener_If_None_Supplied()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var l = new Listener(IPAddress.Any, port, options);

            var val = l.GetProperty<ITcpListener>("TcpListener");

            Assert.NotNull(val);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start starts listening")]
        public void Start_Starts_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            var first = l.Listening;

            l.Start();

            Assert.False(first);
            Assert.True(l.Listening);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start starts TcpListener")]
        public void Start_Starts_TcpListner()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            tcpListener.Verify(m => m.Start(It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start passes the given backlog to the TcpListener")]
        public void Start_Passes_The_Given_Backlog_To_The_TcpListener()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();
            var backlog = new Random().Next(128, int.MaxValue);

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start(backlog);

            tcpListener.Verify(m => m.Start(backlog), Times.Once);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start defaults Backlog to SocketOptionName.MaxConnections if not given")]
        public void Start_Defaults_Backlog_To_MaxConnections_If_Not_Given()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            tcpListener.Verify(m => m.Start((int)SocketOptionName.MaxConnections), Times.Once);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start does not start listener if already listening")]
        public void Start_Does_Not_Start_Listener_If_Already_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();
            l.Start();

            tcpListener.Verify(m => m.Start(It.IsAny<int>()), Times.Once);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start stops the listener if an exception is encountered")]
        public void Start_Stops_The_Listener_If_An_Exception_Is_Encountered()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.Start(It.IsAny<int>()))
                .Throws(new SocketException());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            var ex = Record.Exception(() => l.Start());

            Assert.NotNull(ex);
            Assert.False(l.Listening);

            // Stop() is invoked to unblock any pending AcceptTcpClientAsync() call, even though Start() never succeeded
            tcpListener.Verify(m => m.Start(It.IsAny<int>()), Times.Once);
            tcpListener.Verify(m => m.Stop(), Times.Once);
        }

        [Trait("Category", "Stop")]
        [Fact(DisplayName = "Stop stops listening")]
        public void Stop_Stops_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            var first = l.Listening;

            l.Stop();

            Assert.True(first);
            Assert.False(l.Listening);
        }

        [Trait("Category", "Stop")]
        [Fact(DisplayName = "Stop stops TcpListener")]
        public void Stop_Stops_TcpListener()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();
            l.Stop();

            tcpListener.Verify(m => m.Stop(), Times.Once);
        }

        [Trait("Category", "Stop")]
        [Fact(DisplayName = "Stop does not stop listener if not listening")]
        public void Stop_Does_Not_Stop_Listener_If_Not_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();
            var tcpListener = new Mock<ITcpListener>();

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            l.Stop();
            l.Stop();

            tcpListener.Verify(m => m.Stop(), Times.Once);
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop continues if AcceptTcpClientAsync throws")]
        public async Task Accept_Loop_Continues_If_AcceptTcpClientAsync_Throws()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(new SocketException());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            // if the exception thrown by AcceptTcpClientAsync() escaped the loop, it would only ever be called once
            tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.AtLeast(2));
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop continues if the accepted connection dispatch throws")]
        public async Task Accept_Loop_Continues_If_Accepted_Dispatch_Throws()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();

            // an unconnected TcpClient throws when its RemoteEndPoint is accessed; this happens inside the
            // fire-and-forget Task.Run() used to dispatch the Accepted event, not in the loop's try/catch
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ReturnsAsync(() => new TcpClient());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            // if the exception thrown while dispatching the accepted connection escaped and killed the loop,
            // AcceptTcpClientAsync() would only ever be called once
            tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.AtLeast(2));
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop continues if AcceptTcpClientAsync throws a non-socket exception")]
        public async Task Accept_Loop_Continues_If_AcceptTcpClientAsync_Throws_Non_Socket_Exception()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();

            // the catch block should be broad enough to handle any exception, not just SocketException
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(new InvalidOperationException("boom"));

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.AtLeast(2));
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Stop halts the accept loop")]
        public async Task Stop_Halts_The_Accept_Loop()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(new SocketException());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            l.Start();

            await Task.Delay(200);

            l.Stop();

            // give any in-flight iteration a chance to finish and the loop to observe Listening == false
            await Task.Delay(200);

            var countAfterStop = tcpListener.Invocations.Count;

            await Task.Delay(200);

            // no further calls should occur once the loop has actually exited
            Assert.Equal(countAfterStop, tcpListener.Invocations.Count);
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop raises Accepted with the accepted connection")]
        public async Task Accept_Loop_Raises_Accepted_With_Accepted_Connection()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            // a real, connected loopback pair is required here; an unconnected TcpClient throws
            // when its RemoteEndPoint is accessed, so a mock TcpClient won't do
            var serverListener = new TcpListener(IPAddress.Loopback, 0);
            serverListener.Start();
            var serverPort = ((IPEndPoint)serverListener.LocalEndpoint).Port;

            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync(IPAddress.Loopback, serverPort);
                var acceptedClient = await serverListener.AcceptTcpClientAsync();
                await connectTask;

                serverListener.Stop();

                var callCount = 0;

                var tcpListener = new Mock<ITcpListener>();
                tcpListener.Setup(m => m.AcceptTcpClientAsync())
                    .Returns(() =>
                    {
                        // hand back the real connection once, then fail fast on every subsequent call so the
                        // loop spins harmlessly (and quickly) until Stop() is called
                        if (Interlocked.Increment(ref callCount) == 1)
                        {
                            return Task.FromResult(acceptedClient);
                        }

                        return Task.FromException<TcpClient>(new ObjectDisposedException(nameof(TcpListener)));
                    });

                var tcs = new TaskCompletionSource<IConnection>();

                var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);
                l.Accepted += (sender, connection) => tcs.TrySetResult(connection);

                l.Start();

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));

                l.Stop();

                Assert.Same(tcs.Task, completed);

                var raised = await tcs.Task;

                Assert.NotNull(raised);
                Assert.Equal(((IPEndPoint)acceptedClient.Client.RemoteEndPoint).Address, raised.IPEndPoint.Address);
            }
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop exits when Listening transitions to false between iterations")]
        public async Task Accept_Loop_Exits_When_Listening_Transitions_To_False_Between_Iterations()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var (acceptedClient, connectingClient, _) = await CreateConnectedClientPairAsync();

            using (connectingClient)
            using (acceptedClient)
            {
                var tcpListener = new Mock<ITcpListener>();

                var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

                // the accept succeeds (no exception), so the loop should return to the top, observe that
                // Listening has gone false, and exit via the while condition rather than via the catch block
                tcpListener.Setup(m => m.AcceptTcpClientAsync())
                    .Callback(() => l.SetProperty("Listening", false))
                    .ReturnsAsync(acceptedClient);

                l.SetProperty("Listening", true);

                var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

                var completed = await Task.WhenAny(task, Task.Delay(2000));
                Assert.Same(task, completed);
                await task;

                tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.Once);
            }
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop does not raise Error when a pending accept throws after Listening has stopped")]
        public async Task Accept_Loop_Does_Not_Raise_Error_When_A_Pending_Accept_Throws_After_Listening_Has_Stopped()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var acceptTcs = new TaskCompletionSource<TcpClient>();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .Returns(acceptTcs.Task);

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);
            l.SetProperty("Listening", true);

            var errorRaised = false;
            l.Error += (sender, ex) => errorRaised = true;

            var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

            // simulate Stop() having been called while AcceptTcpClientAsync() was still pending; Stop()
            // itself would cause that pending call to throw, so replicate that directly via the tcs
            l.SetProperty("Listening", false);
            acceptTcs.TrySetException(new ObjectDisposedException(nameof(TcpListener)));

            var completed = await Task.WhenAny(task, Task.Delay(2000));
            Assert.Same(task, completed);
            await task; // should not throw; the exception is expected and swallowed

            Assert.False(errorRaised);
            Assert.Equal(0, l.GetProperty<long>("ConsecutiveErrors"));
            tcpListener.Verify(m => m.AcceptTcpClientAsync(), Times.Once);
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop raises Error for exceptions encountered while still listening")]
        public async Task Accept_Loop_Raises_Error_For_Exceptions_Encountered_While_Still_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var thrown = new InvalidOperationException("boom");

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(thrown);

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);
            l.SetProperty("Listening", true);

            Exception raised = null;
            l.Error += (sender, ex) =>
            {
                raised = ex;
                l.SetProperty("Listening", false); // stop the loop once the failure has been reported
            };

            var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

            var completed = await Task.WhenAny(task, Task.Delay(2000));
            Assert.Same(task, completed);
            await task;

            Assert.Same(thrown, raised);
            Assert.Equal(1, l.GetProperty<long>("ConsecutiveErrors"));
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop disposes the client and rethrows when the accepted client's endpoint can't be resolved")]
        public async Task Accept_Loop_Disposes_The_Client_And_Rethrows_When_The_Endpoint_Cannot_Be_Resolved()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var client = new DisposeTrackingTcpClient();

            // dispose the underlying socket directly so client.Client.RemoteEndPoint throws ObjectDisposedException
            // this happens before "connection" is ever assigned in ListenContinuouslyAsync, so connection stays unset
            client.Client.Dispose();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ReturnsAsync(client);

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);
            l.SetProperty("Listening", true);

            Exception raised = null;
            l.Error += (sender, ex) =>
            {
                raised = ex;
                l.SetProperty("Listening", false); // stop the loop after the first (and only) iteration
            };

            var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

            var completed = await Task.WhenAny(task, Task.Delay(2000));
            Assert.Same(task, completed);
            await task;

            Assert.True(client.Disposed);
            Assert.IsType<ObjectDisposedException>(raised);
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop disposes the client and connection and rethrows when the accepted client never finished connecting")]
        public async Task Accept_Loop_Disposes_The_Client_And_Connection_And_Rethrows_When_The_Accepted_Client_Never_Finished_Connecting()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            // a freshly-constructed, never-connected TcpClient: client.Client.RemoteEndPoint resolves to null
            // (rather than throwing) for a live-but-unconnected socket, so the endpoint cast succeeds and
            // Connection construction completes, but with TcpClient.Connected == false the whole way through
            var client = new DisposeTrackingTcpClient();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ReturnsAsync(client);

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);
            l.SetProperty("Listening", true);

            Exception raised = null;
            l.Error += (sender, ex) =>
            {
                raised = ex;
                l.SetProperty("Listening", false); // stop the loop after the first (and only) iteration
            };

            var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

            var completed = await Task.WhenAny(task, Task.Delay(2000));
            Assert.Same(task, completed);
            await task;

            Assert.True(client.Disposed);
            Assert.IsType<ConnectionException>(raised);
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop delays one second once consecutive errors reach the maximum")]
        public async Task Accept_Loop_Delays_One_Second_Once_Consecutive_Errors_Reach_The_Maximum()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

            var max = l.GetProperty<long>("MaxConsecutiveErrors");

            var callCount = 0L;
            var stopwatch = Stopwatch.StartNew();
            var elapsedAtMax = -1L;
            var elapsedAfterMax = -1L;

            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .Returns(() =>
                {
                    var count = Interlocked.Increment(ref callCount);

                    if (count == max)
                    {
                        elapsedAtMax = stopwatch.ElapsedMilliseconds;
                    }
                    else if (count == max + 1)
                    {
                        elapsedAfterMax = stopwatch.ElapsedMilliseconds;

                        // stop the loop; this call's exception will hit the early-return path instead of looping again
                        l.SetProperty("Listening", false);
                    }

                    return Task.FromException<TcpClient>(new SocketException());
                });

            l.SetProperty("Listening", true);

            var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

            var completed = await Task.WhenAny(task, Task.Delay(5000));
            Assert.Same(task, completed);
            await task;

            Assert.True(elapsedAtMax >= 0 && elapsedAtMax < 500, $"expected the first {max} failures to run back-to-back without delay, but they took {elapsedAtMax}ms");
            Assert.True(elapsedAfterMax - elapsedAtMax >= 900, $"expected a ~1 second delay after the {max}th consecutive failure, but only {elapsedAfterMax - elapsedAtMax}ms elapsed");
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop resets the consecutive error count after a successful accept")]
        public async Task Accept_Loop_Resets_The_Consecutive_Error_Count_After_A_Successful_Accept()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var (acceptedClient, connectingClient, _) = await CreateConnectedClientPairAsync();

            using (connectingClient)
            using (acceptedClient)
            {
                var callCount = 0;

                var tcpListener = new Mock<ITcpListener>();
                var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);

                tcpListener.Setup(m => m.AcceptTcpClientAsync())
                    .Returns(() =>
                    {
                        var count = Interlocked.Increment(ref callCount);

                        if (count <= 2)
                        {
                            return Task.FromException<TcpClient>(new SocketException());
                        }

                        // stop the loop once the successful accept has been fully processed
                        l.SetProperty("Listening", false);
                        return Task.FromResult(acceptedClient);
                    });

                l.SetProperty("Listening", true);

                var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

                var completed = await Task.WhenAny(task, Task.Delay(2000));
                Assert.Same(task, completed);
                await task;

                Assert.Equal(0, l.GetProperty<long>("ConsecutiveErrors"));
            }
        }

        [Trait("Category", "Accept Loop")]
        [Fact(DisplayName = "Accept loop does not throw when the Error handler throws")]
        public async Task Accept_Loop_Does_Not_Throw_When_The_Error_Handler_Throws()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var tcpListener = new Mock<ITcpListener>();
            tcpListener.Setup(m => m.AcceptTcpClientAsync())
                .ThrowsAsync(new SocketException());

            var l = new Listener(IPAddress.Any, port, options, tcpListener.Object);
            l.SetProperty("Listening", true);

            l.Error += (sender, err) =>
            {
                l.SetProperty("Listening", false); // stop the loop before the handler throws
                throw new Exception("handler boom");
            };

            var task = l.InvokeMethod<Task>("ListenContinuouslyAsync");

            var completed = await Task.WhenAny(task, Task.Delay(2000));
            Assert.Same(task, completed);

            var ex = await Record.ExceptionAsync(() => task);

            Assert.Null(ex);
        }

        private static async Task<(TcpClient AcceptedClient, TcpClient ConnectingClient, TcpListener ServerListener)> CreateConnectedClientPairAsync()
        {
            var serverListener = new TcpListener(IPAddress.Loopback, 0);
            serverListener.Start();
            var serverPort = ((IPEndPoint)serverListener.LocalEndpoint).Port;

            var connectingClient = new TcpClient();
            var connectTask = connectingClient.ConnectAsync(IPAddress.Loopback, serverPort);
            var acceptedClient = await serverListener.AcceptTcpClientAsync();
            await connectTask;

            serverListener.Stop();

            return (acceptedClient, connectingClient, serverListener);
        }

        private sealed class DisposeTrackingTcpClient : TcpClient
        {
            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
