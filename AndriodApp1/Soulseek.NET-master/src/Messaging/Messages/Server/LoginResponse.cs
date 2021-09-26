// <copyright file="LoginResponse.cs" company="JP Dillingham">
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
    ///     The response to a login request.
    /// </summary>
    internal sealed class LoginResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LoginResponse"/> class.
        /// </summary>
        /// <param name="succeeded">A value indicating whether the login was successful.</param>
        /// <param name="message">The reason for a login failure.</param>
        /// <param name="ipAddress">The client IP address, if the login was successful.</param>
        public LoginResponse(bool succeeded, string message, IPAddress ipAddress = null)
        {
            Succeeded = succeeded;
            Message = message;
            IPAddress = ipAddress;
        }

        /// <summary>
        ///     Gets the client IP address, if the login was successful.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the reason for a login failure.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets a value indicating whether the login was successful.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="LoginResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static LoginResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.Login)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(LoginResponse)} (expected: {(int)MessageCode.Server.Login}, received: {(int)code}");
            }

            var succeeded = reader.ReadByte() == 1;
            var msg = reader.ReadString();

            var ipAddress = default(IPAddress);

            if (succeeded)
            {
                var ipBytes = reader.ReadBytes(4);
                Array.Reverse(ipBytes);
                ipAddress = new IPAddress(ipBytes);
            }

            return new LoginResponse(succeeded, msg, ipAddress);
        }
    }
}