// <copyright file="UserLeftRoomNotification.cs" company="JP Dillingham">
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
    ///     An incoming notification that a user has left a chat room.
    /// </summary>
    internal sealed class UserLeftRoomNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserLeftRoomNotification"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room from which the user left.</param>
        /// <param name="username">The username of the user that left.</param>
        public UserLeftRoomNotification(string roomName, string username)
        {
            RoomName = roomName;
            Username = username;
        }

        /// <summary>
        ///     Gets the name of the room from which the user left.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the username of the user that left.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserLeftRoomNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserLeftRoomNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.UserLeftRoom)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserLeftRoomNotification)} (expected: {(int)MessageCode.Server.UserLeftRoom}, received: {(int)code}");
            }

            var roomName = reader.ReadString();
            var username = reader.ReadString();

            return new UserLeftRoomNotification(roomName, username);
        }
    }
}