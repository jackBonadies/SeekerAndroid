// <copyright file="UserStatusResponseFactory.cs" company="JP Dillingham">
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
    ///     The response to a peer info request.
    /// </summary>
    internal static class UserStatusResponseFactory
    {
        /// <summary>
        ///     Creates a new instance of <see cref="UserStatusResponseFactory"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserStatus FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetStatus)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserStatusResponseFactory)} (expected: {(int)MessageCode.Server.GetStatus}, received: {(int)code})");
            }

            var username = reader.ReadString();
            var presence = (UserPresence)reader.ReadInteger();
            var privileged = reader.ReadByte() > 0;

            return new UserStatus(username, presence, privileged);
        }
    }
}