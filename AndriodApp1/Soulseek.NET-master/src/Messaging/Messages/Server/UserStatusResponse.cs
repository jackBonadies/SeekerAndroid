// <copyright file="UserStatusResponse.cs" company="JP Dillingham">
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
    ///     The response to a peer info request.
    /// </summary>
    internal sealed class UserStatusResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the peer.</param>
        /// <param name="status">The status of the peer.</param>
        /// <param name="isPrivileged">A value indicating whether the peer is privileged.</param>
        public UserStatusResponse(string username, UserPresence status, bool isPrivileged)
        {
            Username = username;
            Status = status;
            IsPrivileged = isPrivileged;
        }

        /// <summary>
        ///     Gets a value indicating whether the peer is privileged.
        /// </summary>
        public bool IsPrivileged { get; }

        /// <summary>
        ///     Gets the status of the peer.
        /// </summary>
        public UserPresence Status { get; }

        /// <summary>
        ///     Gets the username of the peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserStatusResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserStatusResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetStatus)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserStatusResponse)} (expected: {(int)MessageCode.Server.GetStatus}, received: {(int)code})");
            }

            var username = reader.ReadString();
            var status = (UserPresence)reader.ReadInteger();
            var privileged = reader.ReadByte() > 0;

            return new UserStatusResponse(username, status, privileged);
        }
    }
}