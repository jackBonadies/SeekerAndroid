// <copyright file="GivePrivilegesCommand.cs" company="JP Dillingham">
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
    ///     Grants privileges to a user.
    /// </summary>
    internal sealed class GivePrivilegesCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GivePrivilegesCommand"/> class.
        /// </summary>
        /// <param name="username">The username of the user to which to grant privileges.</param>
        /// <param name="days">The number of days of privileged status to grant.</param>
        public GivePrivilegesCommand(string username, int days)
        {
            Username = username;
            Days = days;
        }

        /// <summary>
        ///     Gets the number of days of privileged status to grant.
        /// </summary>
        public int Days { get; }

        /// <summary>
        ///     Gets the username of the user to which to grant privileges.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.GivePrivileges)
                .WriteString(Username)
                .WriteInteger(Days)
                .Build();
        }
    }
}