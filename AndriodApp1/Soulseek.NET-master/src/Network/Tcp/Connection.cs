// <copyright file="Connection.cs" company="JP Dillingham">
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

namespace Soulseek.Network.Tcp
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Provides client connections for TCP network services.
    /// </summary>
    internal class Connection : IConnection
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        public Connection(IPEndPoint ipEndPoint, ConnectionOptions options = null, ITcpClient tcpClient = null)
        {
            Id = Guid.NewGuid();

            IPEndPoint = ipEndPoint;
            Options = options ?? new ConnectionOptions();

            TcpClient = tcpClient ?? new TcpClientAdapter(new TcpClient(IPEndPoint.AddressFamily)); //otherwise IPv6 will fail

            if (Options.InactivityTimeout > 0)
            {
                InactivityTimer = new SystemTimer()
                {
                    Enabled = false,
                    AutoReset = false,
                    Interval = Options.InactivityTimeout,
                };

                InactivityTimer.Elapsed += (sender, e) =>
                {
                    var ex = new TimeoutException($"Inactivity timeout of {Options.InactivityTimeout} milliseconds was reached");
                    Disconnect(ex.Message, ex);
                };
            }

            //this is for server connections only.
            if(Options.TcpKeepAlive)
            {
                try
                {


                    //[DllImport("libc", SetLastError = true)]
                    //public static extern unsafe int setsockopt(int socket, int opt1, int opt2, void* name, uint size);
                    //[DllImport("libc", SetLastError = true)]
                    //public static extern unsafe int getsockopt(int socket, int level, int optname, void* optval, uint* optlen);
                    //int on = 1;
                    //uint on3 = 4;
                    //int za = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    //int x = setsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 1, 9, &on, sizeof(int));
                    //za = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    //on = 3;
                    //int y = setsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 6, 5, &on, sizeof(int));
                    //za = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    //int z = setsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 6, 6, &on, sizeof(int));
                    //za = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    //int q = setsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 6, 4, &on, sizeof(int));
                    //on = 20;
                    //q = getsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 6, 4, &on, &on3);

                    //getsockopt returns values set by setsockopt which is good.
                    //if the values are set by IOControl they are not returned which seems bad.
                    //in both cases things seemed to work a lot better, but wireshark did not report keep alive packets.. :/
                    //to test - 
                    //turn off wifi
                    //kill foreground service
                    //adb shell dumpsys battery unplug
                    //adb shell dumpsys deviceidle force-idle
                    //try sending message to self, or check privileges etc.




                    //its night and day with this code.  it really does seem to fix it.
                    //with it you get a timeout around 10 seconds after doze mode.
                    //another solution is to register the enter idle mode broadcast. can also check (SoulSeekState.ActiveActivityRef.GetSystemService(PowerService) as PowerManager).IsDeviceIdleMode
                    int size = 4;
                    byte[] keepAlive = new byte[size * 3];
                    
                    //// Turn keepalive on
                    System.Buffer.BlockCopy(System.BitConverter.GetBytes(1U), 0, keepAlive, 0, size);
                    
                    //// Amount of time without activity before sending a keepalive
                    System.Buffer.BlockCopy(System.BitConverter.GetBytes(3000U), 0, keepAlive, size, size);
                    //(TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, 1);
                        //// Keepalive interval to 5 seconds
                    System.Buffer.BlockCopy(System.BitConverter.GetBytes(2000U), 0, keepAlive, size * 2, size);
                    //(TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.SendTimeout = 1000;
                    (TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.IOControl(System.Net.Sockets.IOControlCode.KeepAliveValues, keepAlive, null);


                    //on Redmi 3s - with values 1 (on), 30 (idle), and 10 (interval) it sends one after 1s inactivity, 1s interval.
                    //rooted, tcpdump -i any 'tcp port 2271' -w test.pcap
                    //on Redmi 3s - with values 1 (on), 5000 (idle), and 5000 (interval) it sends one after 5s inactivity, 5s interval.
                    //so likely its in ms with the lower bound being 1s
                    //on rooted samsung galaxy tab 3, lineageOS, same thing :)

                    //int qt = getsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 6, 4, &on, &on3);
                    //qt = getsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 6, 5, &on, &on3);
                    //qt = getsockopt((int)((TcpClient as Soulseek.Network.Tcp.TcpClientAdapter).Client.Handle), 6, 6, &on, &on3);
                }
                catch (Exception)
                {
                    //if we cant set the keep alive, just continue on.
                }
            }

            WatchdogTimer = new SystemTimer()
            {
                Enabled = false,
                AutoReset = true,
                Interval = 250,
            };

            WatchdogTimer.Elapsed += (sender, e) =>
            {
                if (TcpClient == null || !TcpClient.Connected)
                {
                    Disconnect("The server connection was closed unexpectedly");
                }
            };

            if (TcpClient.Connected)
            {
                State = ConnectionState.Connected;
                InactivityTimer?.Start();
                WatchdogTimer.Start();
                Stream = TcpClient.GetStream();
            }
        }

        /// <summary>
        ///     Occurs when the connection is connected.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        ///     Occurs when data is ready from the connection.
        /// </summary>
        public event EventHandler<ConnectionDataEventArgs> DataRead;

        /// <summary>
        ///     Occurs when data has been written to the connection.
        /// </summary>
        public event EventHandler<ConnectionDataEventArgs> DataWritten;

        /// <summary>
        ///     Occurs when the connection is disconnected.
        /// </summary>
        public event EventHandler<ConnectionDisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     Occurs when the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Gets the connection id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     Gets the amount of time since the last activity.
        /// </summary>
        public TimeSpan InactiveTime => DateTime.UtcNow - LastActivityTime;

        /// <summary>
        ///     Gets or sets the remote IP endpoint of the connection.
        /// </summary>
        public IPEndPoint IPEndPoint { get; protected set; }

        /// <summary>
        ///     Gets the unique identifier of the connection.
        /// </summary>
        public virtual ConnectionKey Key => new ConnectionKey(IPEndPoint);

        /// <summary>
        ///     Gets or sets the options for the connection.
        /// </summary>
        public ConnectionOptions Options { get; protected set; }

        /// <summary>
        ///     Gets or sets the current connection state.
        /// </summary>
        public ConnectionState State { get; protected set; }

        /// <summary>
        ///     Gets or sets the connection type.
        /// </summary>
        public ConnectionTypes Type { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the object is disposed.
        /// </summary>
        protected bool Disposed { get; set; } = false;

        /// <summary>
        ///     Gets or sets the timer used to monitor for transfer inactivity.
        /// </summary>
        protected SystemTimer InactivityTimer { get; set; }

        /// <summary>
        ///     Gets or sets the network stream for the connection.
        /// </summary>
        protected INetworkStream Stream { get; set; }

        /// <summary>
        ///     Gets or sets the TcpClient used by the connection.
        /// </summary>
        protected ITcpClient TcpClient { get; set; }

        /// <summary>
        ///     Gets or sets the timer used to monitor the status of the TcpClient.
        /// </summary>
        protected SystemTimer WatchdogTimer { get; set; }

        /// <summary>
        ///     Gets or sets the time at which the last activity took place.
        /// </summary>
        protected DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

        private TaskCompletionSource<string> DisconnectTaskCompletionSource { get; } = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        ///     Asynchronously connects the client to the configured <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection is already connected, or is transitioning between states.
        /// </exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when the time attempting to connect exceeds the configured <see cref="ConnectionOptions.ConnectTimeout"/> value.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when <paramref name="cancellationToken"/> cancellation is requested.
        /// </exception>
        /// <exception cref="ConnectionException">Thrown when an unexpected error occurs.</exception>
        public async Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            if (State != ConnectionState.Pending && State != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException($"Invalid attempt to connect a connected or transitioning connection (current state: {State})");
            }

            cancellationToken ??= CancellationToken.None;

            // create a new TCS to serve as the trigger which will throw when the CTS times out a TCS is basically a 'fake' task
            // that ends when the result is set programmatically. create another for cancellation via the externally provided token.
            var timeoutTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                ChangeState(ConnectionState.Connecting, $"Connecting to {IPEndPoint}");

                // create a new CTS with our desired timeout. when the timeout expires, the cancellation will fire
                using (var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(Options.ConnectTimeout)))
                {
                    Task connectTask;

                    if (Options.ProxyOptions != default)
                    {
                        var proxy = Options.ProxyOptions;

                        connectTask = TcpClient.ConnectThroughProxyAsync(
                            proxy.IPEndPoint.Address,
                            proxy.IPEndPoint.Port,
                            IPEndPoint.Address,
                            IPEndPoint.Port,
                            proxy.Username,
                            proxy.Password,
                            cancellationToken);
                    }
                    else
                    {
                        connectTask = TcpClient.ConnectAsync(IPEndPoint.Address, IPEndPoint.Port);
                    }

                    // register the TCS with the CTS. when the cancellation fires (due to timeout), it will set the value of the
                    // TCS via the registered delegate, ending the 'fake' task, then bind the externally supplied CT with the same
                    // TCS. either the timeout or the external token can now cancel the operation.
#if NETSTANDARD2_0
                    using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
                    using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
#else
                    await using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
                    await using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
#endif
                    {
                        var completedTask = await Task.WhenAny(connectTask, timeoutTaskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);

                        if (completedTask == timeoutTaskCompletionSource.Task)
                        {
                            throw new TimeoutException($"Operation timed out after {Options.ConnectTimeout} milliseconds");
                        }
                        else if (completedTask == cancellationTaskCompletionSource.Task)
                        {
                            throw new OperationCanceledException("Operation cancelled", cancellationToken.Value);
                        }

                        if (connectTask.Exception?.InnerException != null)
                        {
                            throw connectTask.Exception.InnerException;
                        }
                    }
                }

                InactivityTimer?.Start();
                WatchdogTimer.Start();
                Stream = TcpClient.GetStream();

                ChangeState(ConnectionState.Connected, $"Connected to {IPEndPoint}");
            }
            catch (Exception ex)
            {
                Disconnect($"Connection Error: {ex.Message}", ex);
                //Do event so that firebase can log...
                //SoulseekClient.InvokeErrorLogHandler(ex.Message + ex.StackTrace + IPEndPoint.AddressFamily); can be operation cancelled, no route to host, connection timed out, network is unreachable, network subsystem is down, too many open files
                if (ex is TimeoutException || ex is OperationCanceledException)
                {
                    throw;
                }

                throw new ConnectionException($"Failed to connect to {IPEndPoint}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Disconnects the client.
        /// </summary>
        /// <param name="message">The optional message or reason for the disconnect.</param>
        /// <param name="exception">The optional Exception associated with the disconnect.</param>
        public void Disconnect(string message = null, Exception exception = null)
        {
            if (State != ConnectionState.Disconnected && State != ConnectionState.Disconnecting)
            {
                message ??= exception?.Message;

                ChangeState(ConnectionState.Disconnecting, message);

                InactivityTimer?.Stop();
                WatchdogTimer.Stop();
                Stream?.Close();
                TcpClient?.Close();

                ChangeState(ConnectionState.Disconnected, message, exception);
            }
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnection"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Decouples and returns the underlying TCP connection for this connection, allowing the TCP connection to survive
        ///     beyond the lifespan of this instance.
        /// </summary>
        /// <returns>The underlying TCP connection for this connection.</returns>
        public ITcpClient HandoffTcpClient()
        {
            var tcpClient = TcpClient;

            TcpClient = null;
            Stream = null;

            return tcpClient;
        }

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionReadException"/> is thrown.</remarks>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation, including the read bytes.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="length"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionReadException">Thrown when an unexpected error occurs.</exception>
        public Task<byte[]> ReadAsync(long length, CancellationToken? cancellationToken = null)
        {
            if (length < 0)
            {
                throw new ArgumentException("The requested length must be greater than or equal to zero", nameof(length));
            }

            if (!TcpClient.Connected)
            {
                throw new InvalidOperationException("The underlying Tcp connection is closed");
            }

            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            return ReadInternalAsync(length, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionReadException"/> is thrown.</remarks>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="outputStream">The stream to which the read data is to be written.</param>
        /// <param name="governor">The delegate used to govern transfer speed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation, including the read bytes.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="length"/> is less than 1.</exception>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="outputStream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the specified <paramref name="outputStream"/> is not writeable.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionReadException">Thrown when an unexpected error occurs.</exception>
        public Task ReadAsync(long length, Stream outputStream, Func<CancellationToken, Task> governor, CancellationToken? cancellationToken = null)
        {
            if (length < 0)
            {
                throw new ArgumentException("The requested length must be greater than or equal to zero", nameof(length));
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream), "The specified output stream is null");
            }

            if (!outputStream.CanWrite)
            {
                throw new InvalidOperationException("The specified output stream is not writeable");
            }

            if (!TcpClient.Connected)
            {
                throw new InvalidOperationException("The underlying Tcp connection is closed");
            }

            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            return ReadInternalAsync(length, outputStream, governor ?? ((t) => Task.CompletedTask), cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Waits for the connection to disconnect, returning the message or throwing the Exception which caused the disconnect.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The message describing the reason for the disconnect.</returns>
        /// <exception cref="Exception">Thrown when the connection is disconnected as the result of an Exception.</exception>
        public Task<string> WaitForDisconnect(CancellationToken? cancellationToken = null)
        {
            cancellationToken?.Register(() =>
                Disconnect(exception: new OperationCanceledException("Operation cancelled")));

            return DisconnectTaskCompletionSource.Task;
        }

        /// <summary>
        ///     Asynchronously writes the specified bytes to the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionWriteException"/> is thrown.</remarks>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="bytes"/> array is null or empty.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionWriteException">Thrown when an unexpected error occurs.</exception>
        public Task WriteAsync(byte[] bytes, CancellationToken? cancellationToken = null)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException("Invalid attempt to send empty data", nameof(bytes));
            }

            if (!TcpClient.Connected)
            {
                throw new InvalidOperationException("The underlying Tcp connection is closed");
            }

            //bool selectRead = TcpClient.Client.Poll(1000000, SelectMode.SelectRead);
            //bool selectWrite = TcpClient.Client.Poll(1000000, SelectMode.SelectWrite);
            //bool selectError = TcpClient.Client.Poll(1000000, SelectMode.SelectError);

            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            return WriteInternalAsync(bytes, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously writes the specified bytes to the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionWriteException"/> is thrown.</remarks>
        /// <param name="length">The number of bytes to write.</param>
        /// <param name="inputStream">The stream from which the written data is to be read.</param>
        /// <param name="governor">The delegate used to govern transfer speed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="length"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="inputStream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the specified <paramref name="inputStream"/> is not readable.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionWriteException">Thrown when an unexpected error occurs.</exception>
        public Task WriteAsync(long length, Stream inputStream, Func<CancellationToken, Task> governor, CancellationToken? cancellationToken = null)
        {
            if (length <= 0)
            {
                throw new ArgumentException("The requested length must be greater than or equal to zero", nameof(length));
            }

            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream), "The specified output stream is null");
            }

            if (!inputStream.CanRead)
            {
                throw new InvalidOperationException("The specified input stream is not readable");
            }

            if (!TcpClient.Connected)
            {
                throw new InvalidOperationException("The underlying Tcp connection is closed");
            }

            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            return WriteInternalAsync(length, inputStream, governor ?? ((t) => Task.CompletedTask), cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Changes the state of the connection to the specified <paramref name="state"/> and raises events with the
        ///     optionally specified <paramref name="message"/>.
        /// </summary>
        /// <param name="state">The state to which to change.</param>
        /// <param name="message">The optional message describing the nature of the change.</param>
        /// <param name="exception">The optional Exception associated with the change.</param>
        protected void ChangeState(ConnectionState state, string message, Exception exception = null)
        {
            var eventArgs = new ConnectionStateChangedEventArgs(previousState: State, currentState: state, message: message, exception: exception);

            State = state;

            Interlocked.CompareExchange(ref StateChanged, null, null)?
                .Invoke(this, eventArgs);

            if (State == ConnectionState.Connected)
            {
                Interlocked.CompareExchange(ref Connected, null, null)?
                    .Invoke(this, EventArgs.Empty);
            }
            else if (State == ConnectionState.Disconnected)
            {
                Interlocked.CompareExchange(ref Disconnected, null, null)?
                    .Invoke(this, new ConnectionDisconnectedEventArgs(message, exception));

                if (exception != null)
                {
                    DisconnectTaskCompletionSource.SetException(exception);
                }
                else
                {
                    DisconnectTaskCompletionSource.SetResult(message);
                }
            }
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="Connection"/>.
        /// </summary>
        /// <param name="disposing">A value indicating whether the object is in the process of disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Disconnect("Connection is being disposed", new ObjectDisposedException(GetType().Name));
                    InactivityTimer?.Dispose();
                    WatchdogTimer.Dispose();
                    Stream?.Dispose();
                    TcpClient?.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<byte[]> ReadInternalAsync(long length, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            using var stream = new MemoryStream();
#else
            await using var stream = new MemoryStream();
#endif

            await ReadInternalAsync(length, stream, (c) => Task.CompletedTask, cancellationToken).ConfigureAwait(false); //cannot access a disposed object TODO:ERROR
            return stream.ToArray();
        }

        private async Task ReadInternalAsync(long length, Stream outputStream, Func<CancellationToken, Task> governor, CancellationToken cancellationToken)
        {
            ResetInactivityTime();

            var buffer = new byte[Options.ReadBufferSize];
            long totalBytesRead = 0;

            try
            {
                while (totalBytesRead < length)
                {
                    await governor(cancellationToken).ConfigureAwait(false);

                    var bytesRemaining = length - totalBytesRead;
                    var bytesToRead = bytesRemaining >= buffer.Length ? buffer.Length : (int)bytesRemaining; // cast to int is safe because of the check against buffer length.
#if DEBUG
                    if (IPEndPoint.Address.ToString() == "2607:7700:0:b::d04c:aa3b")
                    {
                        Console.WriteLine("server pre read bytes low level: " + bytesToRead);
                        Console.WriteLine(this.Id);
                        Console.WriteLine(TcpClient.Client.RemoteEndPoint.ToString() + TcpClient.Client.LocalEndPoint.ToString());
                    }
#endif
                    var bytesRead = await Stream.ReadAsync(buffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);
#if DEBUG
                    if (IPEndPoint.Address.ToString() == "2607:7700:0:b::d04c:aa3b")
                    {
                        Console.WriteLine("server post read bytes low level");
                    }
#endif
                    if (bytesRead == 0)
                    {
#if DEBUG
                        if (IPEndPoint.Address.ToString() == "2607:7700:0:b::d04c:aa3b")
                        {
                            Console.WriteLine("server remote connection closed");
                        }
#endif
                        throw new ConnectionException("Remote connection closed");
                    }

                    totalBytesRead += bytesRead;

#if NETSTANDARD2_0
                    await outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
#else
                    await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
#endif

                    Interlocked.CompareExchange(ref DataRead, null, null)?
                        .Invoke(this, new ConnectionDataEventArgs(totalBytesRead, length));

                    ResetInactivityTime();
                }

                await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
#if DEBUG
                if (IPEndPoint.Address.ToString() == "2607:7700:0:b::d04c:aa3b")
                {
                    Console.WriteLine("server remote disconnect");
                }
#endif
                Disconnect($"Read error: {ex.Message}", ex);

                if (ex is TimeoutException || ex is OperationCanceledException)
                {
                    throw;
                }

                throw new ConnectionReadException($"Failed to read {length} bytes from {IPEndPoint}: {ex.Message}", ex);
            }
        }

        private void ResetInactivityTime()
        {
            InactivityTimer?.Reset();
            LastActivityTime = DateTime.UtcNow;
        }

        private async Task WriteInternalAsync(byte[] bytes, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            using var stream = new MemoryStream(bytes);
#else
            await using var stream = new MemoryStream(bytes);
#endif

            await WriteInternalAsync(bytes.Length, stream, (c) => Task.CompletedTask, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteInternalAsync(long length, Stream inputStream, Func<CancellationToken, Task> governor, CancellationToken cancellationToken)
        {
            ResetInactivityTime();

            var inputBuffer = new byte[Options.WriteBufferSize];
            var totalBytesWritten = 0;

            try
            {
                while (totalBytesWritten < length)
                {
                    await governor(cancellationToken).ConfigureAwait(false);

                    var bytesRemaining = length - totalBytesWritten;

                    var bytesToRead = bytesRemaining >= inputBuffer.Length ? inputBuffer.Length : (int)bytesRemaining;
#if NETSTANDARD2_0
                    var bytesRead = await inputStream.ReadAsync(inputBuffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);
#else
                    var bytesRead = await inputStream.ReadAsync(inputBuffer.AsMemory(0, bytesToRead), cancellationToken).ConfigureAwait(false);
#endif

                    await Stream.WriteAsync(inputBuffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                    totalBytesWritten += bytesRead;
#if DEBUG
                    if (IPEndPoint.Address.ToString() == "2607:7700:0:b::d04c:aa3b")
                    {
                        Console.WriteLine("server write bytes: ");
                        Console.WriteLine(this.Id);
                        Console.WriteLine(TcpClient.Client.RemoteEndPoint.ToString() + TcpClient.Client.LocalEndPoint.ToString());
                    }
#endif

                    Interlocked.CompareExchange(ref DataWritten, null, null)?
                        .Invoke(this, new ConnectionDataEventArgs(totalBytesWritten, length));

                    ResetInactivityTime();
                }
            }
            catch (Exception ex)
            {
                Disconnect($"Write error: {ex.Message}", ex);

                if (ex is TimeoutException || ex is OperationCanceledException)
                {
                    throw;
                }

                throw new ConnectionWriteException($"Failed to write {length} bytes to {IPEndPoint}: {ex.Message}", ex);
            }
        }
    }
}