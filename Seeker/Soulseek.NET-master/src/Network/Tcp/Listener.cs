// <copyright file="Listener.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Network.Tcp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Listens for client connections for TCP network services.
    /// </summary>
    /// <remarks>
    ///     Excluded from code coverage due to the inability to test the accepted code block; You can't instantiate TcpClient with
    ///     an ip and port without it connecting immediately, so the test either must create a new connection to *something*, or a
    ///     bunch of hoops need to be jumped through to handle TcpClients coming from the listener not connected/without an
    ///     endpoint, both of which will and SHOULD throw exceptions and die.
    /// </remarks>
    internal sealed class Listener : IListener
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address to which to bind the listener.</param>
        /// <param name="port">The port of the listener.</param>
        /// <param name="connectionOptions">The optional options to use when creating <see cref="IConnection"/> instances.</param>
        /// <param name="tcpListener">The optional TcpClient instance to use.</param>
        public Listener(IPAddress ipAddress, int port, ConnectionOptions connectionOptions, ITcpListener tcpListener = null)
        {
            IPAddress = ipAddress;
            Port = port;
            ConnectionOptions = connectionOptions ?? new ConnectionOptions();
            TcpListener = tcpListener ?? new TcpListenerAdapter(new TcpListener(ipAddress, port));
        }

        /// <summary>
        ///     Occurs when a new connection is accepted.
        /// </summary>
        public event EventHandler<IConnection> Accepted;

        /// <summary>
        ///     Occurs when the listener encounters an exception while accepting a connection.
        /// </summary>
        public event EventHandler<Exception> Error;

        /// <summary>
        ///     Gets the options used when creating new <see cref="IConnection"/> instances.
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; }

        /// <summary>
        ///     Gets the port of the listener.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets a value indicating whether the listener is listening for connections.
        /// </summary>
        public bool Listening { get; private set; } = false;

        /// <summary>
        ///     Gets the port of the listener.
        /// </summary>
        public int Port { get; }

        private object SyncRoot { get; } = new object();
        private ITcpListener TcpListener { get; set; }
        private long MaxConsecutiveErrors { get; } = 20;
        private long ConsecutiveErrors { get; set; } // overflows after ~29 billion years

        /// <summary>
        ///     Starts the listener.
        /// </summary>
        /// <param name="backlog">The maximum number of pending connections the OS will queue for this listener.</param>
        public void Start(int backlog = (int)SocketOptionName.MaxConnections)
        {
            lock (SyncRoot)
            {
                if (Listening)
                {
                    return;
                }

                try
                {
                    TcpListener.Start(backlog);
                    Listening = true;
                    Task.Run(() => ListenContinuouslyAsync()).Forget();
                }
                catch (Exception)
                {
                    Listening = false;
                    TcpListener.Stop(); // unblocks AcceptTcpClientAsync()
                    throw;
                }
            }
        }

        /// <summary>
        ///     Stops the listener.
        /// </summary>
        public void Stop()
        {
            lock (SyncRoot)
            {
                if (!Listening)
                {
                    return;
                }

                Listening = false;
                TcpListener.Stop(); // unblocks AcceptTcpClientAsync()
            }
        }

        private async Task ListenContinuouslyAsync()
        {
            while (Listening)
            {
                try
                {
                    /*
                        throws if:

                        * the accept() call offloaded to the OS errors for whatever reason (exhaustion, other side hung up)
                        * the accept() call is being awaited and the Stop() method is invoked (the BCL cleans this up in the socket's finalizer)
                          in addition to purging any connections that may have been waiting to be accepted
                        * the Stop() method was invoked prior to this method's invocation (the underlying Socket has been disposed/nulled)
                    */
                    var client = await TcpListener.AcceptTcpClientAsync().ConfigureAwait(false);

                    Connection connection = default;

                    try
                    {
                        var endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                        connection = new Connection(endPoint, ConnectionOptions, new TcpClientAdapter(client));

                        if (connection.State != ConnectionState.Connected)
                        {
                            // the remote client disconnected between the OS accepting the socket and construction of
                            // the Connection instance completing; treat this the same as any other accept failure
                            throw new ConnectionException($"The remote client disconnected before the connection could be accepted");
                        }

                        ConsecutiveErrors = 0; // reset on success

                        _ = Task
                            .Run(() => Accepted?.Invoke(this, connection))
                            .ContinueWith(task => _ = task.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.RunContinuationsAsynchronously);
                    }
                    catch
                    {
                        client?.TryDispose();
                        connection?.TryDispose();

                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // if Listening has dropped, Stop() was called and we should exit. the exception isn't interesting here
                    // because Stop() will cause any waiting AcceptTcpClientAsync() call to throw when it's called
                    if (!Listening)
                    {
                        return;
                    }

                    ConsecutiveErrors++;

                    try
                    {
                        Error?.Invoke(this, ex);
                    }
                    catch
                    {
                        // noop
                    }
                    finally
                    {
                        /*
                            if AcceptTcpClientAsync() threw because of a non-transient issue, allowing the loop to come
                            back around and call it immediately will put this in a continuous, fast loop and peg the CPU.
                            once we have hit our MaxConsecutiveErrors, begin waiting a generous amount of time before looping again.
                            this spares the pegged CPU while also allowing the listener to recover automatically if/when whatever
                            condition that's causing the exceptions is resolved (outside of the application)
                        */
                        if (ConsecutiveErrors >= MaxConsecutiveErrors)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }
}
