// <copyright file="RoomMessageNotification.cs" company="JP Dillingham">
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
    ///     An incoming chat room message.
    /// </summary>
    internal sealed class RoomMessageNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageNotification"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        public RoomMessageNotification(string roomName, string username, string message)
        {
            RoomName = roomName;
            Username = username;
            Message = message;
        }

        /// <summary>
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the name of the room in which the message was sent.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the username of the user which sent the message.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PrivateMessageNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static RoomMessageNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.SayInChatRoom)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(RoomMessageNotification)} (expected: {(int)MessageCode.Server.SayInChatRoom}, received: {(int)code})");
            }

            var roomName = reader.ReadString();
            var username = reader.ReadString();
            var msg = reader.ReadString();

            return new RoomMessageNotification(roomName, username, msg);
        }
    }
}