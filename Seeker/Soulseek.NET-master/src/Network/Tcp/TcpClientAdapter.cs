// <copyright file="TcpClientAdapter.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides client connections for TCP network services.
    /// </summary>
    /// <remarks>
    ///     This is a pass-through implementation of <see cref="ITcpClient"/> over <see cref="TcpClient"/> intended to enable
    ///     dependency injection.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal sealed class TcpClientAdapter : ITcpClient
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpClientAdapter"/> class with an optional <paramref name="tcpClient"/>.
        /// </summary>
        /// <param name="tcpClient">The optional TcpClient to wrap.</param>
        public TcpClientAdapter(TcpClient tcpClient = null)
        {
            TcpClient = tcpClient ?? new TcpClient();
        }

        /// <summary>
        ///     Gets the underlying <see cref="Socket"/>.
        /// </summary>
        public Socket Client => TcpClient.Client;

        /// <summary>
        ///     Gets a value indicating whether the client is connected.
        /// </summary>
        public bool Connected => TcpClient.Connected;

        /// <summary>
        ///     Gets the client remote endpoint.
        /// </summary>
        public IPEndPoint RemoteEndPoint => (IPEndPoint)TcpClient.Client.RemoteEndPoint;

        private bool Disposed { get; set; }
        private TcpClient TcpClient { get; set; }

        /// <summary>
        ///     Closes the client connection.
        /// </summary>
        public void Close()
        {
            TcpClient.Close();
            Dispose(false);
        }

        /// <summary>
        ///     Connects the client to a remote TCP host using the specified IP address and port number as an asynchronous operation.
        /// </summary>
        /// <param name="address">The IP address to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the address parameter is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the port parameter is not between <see cref="IPEndPoint.MinPort"/> and <see cref="IPEndPoint.MaxPort"/>.
        /// </exception>
        /// <exception cref="SocketException">Thrown when an error occurs while accessing the socket.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the TCP client has been disposed.</exception>
        public Task ConnectAsync(IPAddress address, int port)
        {
            return TcpClient.ConnectAsync(address, port);
        }

        /// <summary>
        ///     Connects to the specified <paramref name="destinationAddress"/> and <paramref name="destinationPort"/> via the
        ///     specified <paramref name="proxyAddress"/> and <paramref name="proxyPort"/>.
        /// </summary>
        /// <param name="proxyAddress">The address of the proxy server to which to connect.</param>
        /// <param name="proxyPort">The port of the proxy server to which to connect.</param>
        /// <param name="destinationAddress">The destination address to which to connect.</param>
        /// <param name="destinationPort">The desintation port to which to connect.</param>
        /// <param name="username">The optional username for the proxy.</param>
        /// <param name="password">The optional password for the proxy.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///     The Task representing the asynchronous operation, including the address and port reported by the proxy server
        ///     following connection.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the proxy or destination address is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the proxy or destination port is not within the valid port range 0-65535.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when a username is supplied without a password, or vice versa.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the username or password is longer than 255 characters.</exception>
        /// <exception cref="ProxyException">Thrown when an unexpected error occurs.</exception>
        public Task<(string ProxyAddress, int ProxyPort)> ConnectThroughProxyAsync(
            IPAddress proxyAddress,
            int proxyPort,
            IPAddress destinationAddress,
            int destinationPort,
            string username = null,
            string password = null,
            CancellationToken? cancellationToken = null)
        {
            if (proxyAddress == default)
            {
                throw new ArgumentNullException(nameof(proxyAddress));
            }

            if (proxyPort < IPEndPoint.MinPort || proxyPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(proxyPort), proxyPort, $"Proxy port must be within {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}, inclusive");
            }

            if (destinationAddress == default)
            {
                throw new ArgumentNullException(nameof(destinationAddress));
            }

            if (destinationPort < IPEndPoint.MinPort || destinationPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationPort), destinationPort, $"Destination port must be within {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}, inclusive");
            }

            if (username == default != (password == default))
            {
                throw new ArgumentException("Username and password must both be supplied");
            }

            if (username != default && username.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(username), "The username length must be less than or equal to 255 characters");
            }

            if (password != default && password.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(password), "The password length must be less than or equal to 255 characters");
            }

            return ConnectThroughProxyInternalAsync(proxyAddress, proxyPort, destinationAddress, destinationPort, cancellationToken ?? CancellationToken.None, username, password);
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="TcpClientAdapter"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Returns the <see cref="NetworkStream"/> used to send and receive data.
        /// </summary>
        /// <returns>The NetworkStream used to send and receive data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the TCP client is not connected to a remote host.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the TCP client has been disposed.</exception>
        public INetworkStream GetStream()
        {
            return new NetworkStreamAdapter(TcpClient.GetStream());
        }

        private async Task<(string ProxyAddress, int ProxyPort)> ConnectThroughProxyInternalAsync(
            IPAddress proxyAddress,
            int proxyPort,
            IPAddress destinationAddress,
            int destinationPort,
            CancellationToken cancellationToken,
            string username = null,
            string password = null)
        {
            const byte SOCKS_5 = 0x05;

            const byte AUTH_ANONYMOUS = 0x00;
            const byte AUTH_USERNAME = 0x02;
            const byte AUTH_VERSION = 0x1;

            const byte CONNECT = 0x01;

            const byte IPV4 = 0x01;
            const byte DOMAIN = 0x03;
            const byte IPV6 = 0x04;

            const byte EMPTY = 0x00;
            const byte ERROR = 0xFF;

            var usingCredentials = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
            var buffer = new byte[1024];

            async Task<byte[]> ReadAsync(INetworkStream stream, int length, CancellationToken cancellationToken)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);
                return buffer.AsSpan().Slice(0, bytesRead).ToArray();
            }

            static Task WriteAsync(INetworkStream stream, byte[] data, CancellationToken cancellationToken)
                => stream.WriteAsync(data, 0, data.Length, cancellationToken);

            try
            {
                await ConnectAsync(proxyAddress, proxyPort).ConfigureAwait(false);
                var stream = GetStream();

                byte[] auth;

                if (usingCredentials)
                {
                    auth = new byte[] { SOCKS_5, 0x02, AUTH_ANONYMOUS, AUTH_USERNAME };
                }
                else
                {
                    auth = new byte[] { SOCKS_5, 0x01, AUTH_ANONYMOUS };
                }

                await WriteAsync(stream, auth, cancellationToken).ConfigureAwait(false);

                var authResponse = await ReadAsync(stream, 2, cancellationToken).ConfigureAwait(false);

                if (authResponse[0] != SOCKS_5)
                {
                    throw new ProxyException($"Invalid SOCKS version (expected: {SOCKS_5}, received: {authResponse[0]})");
                }

                switch (authResponse[1])
                {
                    case AUTH_ANONYMOUS:
                        break;

                    case AUTH_USERNAME:
                        if (!usingCredentials)
                        {
                            throw new ProxyException("Server requests authorization but none was provided");
                        }

                        var creds = new List<byte>
                        {
                            AUTH_VERSION,
                            (byte)username.Length,
                        };

                        creds.AddRange(Encoding.ASCII.GetBytes(username));

                        creds.Add((byte)password.Length);
                        creds.AddRange(Encoding.ASCII.GetBytes(password));

                        await WriteAsync(stream, creds.ToArray(), cancellationToken).ConfigureAwait(false);

                        var credsResponse = await ReadAsync(stream, 2, cancellationToken).ConfigureAwait(false);

                        if (credsResponse.Length != 2)
                        {
                            throw new ProxyException("Abnormal authentication response from server");
                        }

                        if (credsResponse[0] != AUTH_VERSION)
                        {
                            throw new ProxyException($"Invalid authentication subnegotiation version (expected: {AUTH_VERSION}, received: {credsResponse[0]})");
                        }

                        if (credsResponse[1] != EMPTY)
                        {
                            throw new ProxyException($"Authentication failed: error code {credsResponse[1]}");
                        }

                        break;

                    case ERROR:
                        throw new ProxyException($"Server does not support the specified authentication method(s)");
                    default:
                        throw new ProxyException($"Unknown auth METHOD response from server: {authResponse[1]}");
                }

                var connection = new List<byte>() { SOCKS_5, CONNECT, EMPTY, IPV4 };

                connection.AddRange(destinationAddress.GetAddressBytes());
                connection.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)destinationPort)));

                await WriteAsync(stream, connection.ToArray(), cancellationToken).ConfigureAwait(false);

                var connectionResponse = await ReadAsync(stream, 4, CancellationToken.None).ConfigureAwait(false);

                if (connectionResponse[0] != SOCKS_5)
                {
                    throw new ProxyException($"Invalid SOCKS version (expected: {SOCKS_5}, received: {authResponse[0]})");
                }

                if (connectionResponse[1] != EMPTY)
                {
                    string msg = connectionResponse[1] switch
                    {
                        0x01 => "General SOCKS server failure",
                        0x02 => "Connection not allowed by ruleset",
                        0x03 => "Network unreachable",
                        0x04 => "Host unreachable",
                        0x05 => "Connection refused",
                        0x06 => "TTL expired",
                        0x07 => "Command not supported",
                        0x08 => "Address type not supported",
                        _ => $"Unknown SOCKS error {connectionResponse[1]}",
                    };

                    throw new ProxyException($"SOCKS connection failed: {msg}");
                }

                string boundAddress;
                ushort boundPort;

                try
                {
                    switch (connectionResponse[3])
                    {
                        case IPV4:
                            var boundIPBytes = await ReadAsync(stream, 4, CancellationToken.None).ConfigureAwait(false);
                            boundAddress = new IPAddress(BitConverter.ToUInt32(boundIPBytes, 0)).ToString();
                            break;

                        case DOMAIN:
                            var lengthBytes = await ReadAsync(stream, 1, CancellationToken.None).ConfigureAwait(false);

                            if (lengthBytes[0] == ERROR)
                            {
                                throw new ProxyException("Invalid domain name");
                            }

                            var boundDomainBytes = await ReadAsync(stream, lengthBytes[0], CancellationToken.None).ConfigureAwait(false);
                            boundAddress = Encoding.ASCII.GetString(boundDomainBytes);
                            break;

                        case IPV6:
                            var boundIPv6Bytes = await ReadAsync(stream, 16, CancellationToken.None).ConfigureAwait(false);
                            boundAddress = new IPAddress(boundIPv6Bytes).ToString();
                            break;

                        default:
                            throw new ProxyException($"Unknown SOCKS Address type (expected: one of {IPV4}, {DOMAIN}, {IPV6}, received: {connectionResponse[3]})");
                    }
                }
                catch (Exception ex)
                {
                    throw new ProxyException($"Invalid address response from server: {ex.Message}");
                }

                var boundPortBytes = await ReadAsync(stream, 2, CancellationToken.None).ConfigureAwait(false);
                boundPort = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(boundPortBytes, 0));

                return (boundAddress, boundPort);
            }
            catch (Exception ex) when (!(ex is ProxyException))
            {
                throw new ProxyException($"Failed to connect to proxy: {ex.Message}", ex);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    TcpClient.Dispose();
                }

                Disposed = true;
            }
        }
    }
}