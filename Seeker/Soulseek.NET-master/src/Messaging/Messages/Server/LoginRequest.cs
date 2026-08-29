// <copyright file="LoginRequest.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Logs in to the server.
    /// </summary>
    internal sealed class LoginRequest : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LoginRequest"/> class.
        /// </summary>
        /// <param name="minorVersion">The minor version of the client.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public LoginRequest(int minorVersion, string username, string password)
        {
            MinorVersion = minorVersion;

            Username = username;
            Password = password;

            Hash = $"{Username}{Password}".ToMD5Hash();
        }

        /// <summary>
        ///     Gets the MD5 hash of the username and password.
        /// </summary>
        public string Hash { get; }

        /// <summary>
        ///     Gets the minor client version.
        /// </summary>
        public int MinorVersion { get; }

        /// <summary>
        ///     Gets the password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        ///     Gets the username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets the client version.
        /// </summary>
        public int Version { get; } = Constants.MajorVersion;

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteString(Username)
                .WriteString(Password)
                .WriteInteger(Version)
                .WriteString(Hash)
                .WriteInteger(MinorVersion)
                .Build();
        }
    }
}