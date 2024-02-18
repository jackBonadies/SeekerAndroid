// <copyright file="ProxyOptions.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    ///     Proxy configuration options.
    /// </summary>
    public class ProxyOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProxyOptions"/> class.
        /// </summary>
        /// <param name="address">The address of the proxy server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="username">The username for the proxy, if applicable.</param>
        /// <param name="password">The password for the proxy, if applicable.</param>
        public ProxyOptions(string address, int port, string username = null, string password = null)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Address must not be a null or empty string, or one consisting only of whitespace", nameof(address));
            }

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port), $"The port must be within the range {IPEndPoint.MinPort}-{IPEndPoint.MaxPort} (specified: {port})");
            }

            if (username == default != (password == default))
            {
                throw new ArgumentException("Username and password must both be specified");
            }

            if (username != default)
            {
                if (username.Length < 1 || username.Length > 255)
                {
                    throw new ArgumentOutOfRangeException(nameof(username), "The username must be between 1 and 255 characters");
                }

                if (password.Length < 1 || password.Length > 255)
                {
                    throw new ArgumentOutOfRangeException(nameof(password), "The password must be between 1 and 255 characters");
                }
            }

            if (!IPAddress.TryParse(address, out IPAddress ipAddress))
            {
                try
                {
                    ipAddress = Dns.GetHostEntry(address).AddressList[0];
                }
                catch (SocketException ex)
                {
                    throw new AddressException($"Failed to resolve address '{Address}': {ex.Message}", ex);
                }
            }

            Address = address;
            IPAddress = ipAddress;
            Port = port;
            IPEndPoint = new IPEndPoint(IPAddress, Port);
            Username = username;
            Password = password;
        }

        /// <summary>
        ///     Gets the address of the proxy server to which to connect.
        /// </summary>
        public string Address { get; }

        /// <summary>
        ///     Gets the resolved proxy server address.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the resolved proxy server endpoint.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Gets the password for the proxy, if applicable.
        /// </summary>
        public string Password { get; }

        /// <summary>
        ///     Gets the port to which to connect.
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Gets the username for the proxy, if applicable.
        /// </summary>
        public string Username { get; }
    }
}