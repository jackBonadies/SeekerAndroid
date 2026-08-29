// <copyright file="UserPrivilegeResponse.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    /// <summary>
    ///     The response to a request for a user's privileges.
    /// </summary>
    internal sealed class UserPrivilegeResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserPrivilegeResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the peer.</param>
        /// <param name="isPrivileged">A value indicating whether the peer is privileged.</param>
        public UserPrivilegeResponse(string username, bool isPrivileged)
        {
            Username = username;
            IsPrivileged = isPrivileged;
        }

        /// <summary>
        ///     Gets a value indicating whether the peer is privileged.
        /// </summary>
        public bool IsPrivileged { get; }

        /// <summary>
        ///     Gets the username of the peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserPrivilegeResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserPrivilegeResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.UserPrivileges)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserPrivilegeResponse)} (expected: {(int)MessageCode.Server.UserPrivileges}, received: {(int)code})");
            }

            var username = reader.ReadString();
            var privileged = reader.ReadByte() > 0;

            return new UserPrivilegeResponse(username, privileged);
        }
    }
}