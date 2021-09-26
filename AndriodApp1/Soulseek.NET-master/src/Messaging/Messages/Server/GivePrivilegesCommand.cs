// <copyright file="GivePrivilegesCommand.cs" company="JP Dillingham">
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