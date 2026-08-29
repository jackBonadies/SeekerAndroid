// <copyright file="PrivilegeNotification.cs" company="JP Dillingham">
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
    ///     An incoming notification of granted privileges.
    /// </summary>
    /// <remarks>
    ///     The Museek documentation states that this notification is sent in regards to _our_ privileges as opposed to other
    ///     users, however the inclusion of the username in the payload might imply this is incorrect. The Museek documentation
    ///     also has send/receive swapped; this was verified against the Nicotine source code.
    /// </remarks>
    internal sealed class PrivilegeNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivilegeNotification"/> class.
        /// </summary>
        /// <param name="id">The unique id of the notification.</param>
        /// <param name="username">The username of the user associated with the notification.</param>
        public PrivilegeNotification(int id, string username)
        {
            Id = id;
            Username = username;
        }

        /// <summary>
        ///     Gets the unique id of the notification.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     Gets the username of the user associated with the notification.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PrivilegeNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PrivilegeNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.NotifyPrivileges)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PrivilegeNotification)} (expected: {(int)MessageCode.Server.NotifyPrivileges}, received: {(int)code})");
            }

            var id = reader.ReadInteger();
            var username = reader.ReadString();

            return new PrivilegeNotification(id, username);
        }
    }
}