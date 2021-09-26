// <copyright file="PrivateRoomAddOperator.cs" company="JP Dillingham">
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
    ///     The command and response to add a moderator to a private chat room.
    /// </summary>
    internal sealed class PrivateRoomAddOperator : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateRoomAddOperator"/> class.
        /// </summary>
        /// <param name="roomName">The room to which to add the user.</param>
        /// <param name="username">The username of the user to add.</param>
        public PrivateRoomAddOperator(string roomName, string username)
        {
            RoomName = roomName;
            Username = username;
        }

        /// <summary>
        ///     Gets the room to which to add the user.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the username of the user to add.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PrivateRoomAddOperator"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PrivateRoomAddOperator FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.PrivateRoomAddOperator)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(MessageCode.Server.PrivateRoomAddOperator)} (expected: {(int)MessageCode.Server.PrivateRoomAddOperator}, received: {(int)code})");
            }

            var roomName = reader.ReadString();
            var username = reader.ReadString();

            return new PrivateRoomAddOperator(roomName, username);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddOperator)
                .WriteString(RoomName)
                .WriteString(Username)
                .Build();
        }
    }
}