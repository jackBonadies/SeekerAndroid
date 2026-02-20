// <copyright file="UserAddressResponse.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System;
    using System.Net;

    /// <summary>
    ///     The response to a request for a peer's address.
    /// </summary>
    internal sealed class UserAddressResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserAddressResponse"/> class.
        /// </summary>
        /// <param name="username">The requested peer username.</param>
        /// <param name="ipAddress">The IP address of the peer.</param>
        /// <param name="port">The port on which the peer is listening.</param>
        public UserAddressResponse(string username, IPAddress ipAddress, int port)
            : this(username, new IPEndPoint(ipAddress, port))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserAddressResponse"/> class.
        /// </summary>
        /// <param name="username">The requested peer username.</param>
        /// <param name="endpoint">The IP endpoint of the peer.</param>
        public UserAddressResponse(string username, IPEndPoint endpoint)
        {
            Username = username;
            IPEndPoint = endpoint;

            IPAddress = IPEndPoint.Address;
            Port = IPEndPoint.Port;
        }

        /// <summary>
        ///     Gets the IP address of the peer.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the IP endpoint of the peer.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Gets the port on which the peer is listening.
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Gets the requested peer username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserAddressResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserAddressResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetPeerAddress)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserAddressResponse)} (expected: {(int)MessageCode.Server.GetPeerAddress}, received: {(int)code})");
            }

            var username = reader.ReadString();

            var ipBytes = reader.ReadBytes(4);
            Array.Reverse(ipBytes);
            var ipAddress = new IPAddress(ipBytes);

            var port = reader.ReadInteger();

            return new UserAddressResponse(username, ipAddress, port);
        }
    }
}