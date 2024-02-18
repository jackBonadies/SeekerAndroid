// <copyright file="PrivateRoomDropMembership.cs" company="JP Dillingham">
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
    ///     The command to drop membership in a private chat room.
    /// </summary>
    internal sealed class PrivateRoomDropMembership : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateRoomDropMembership"/> class.
        /// </summary>
        /// <param name="roomName">The room for which to drop membership.</param>
        public PrivateRoomDropMembership(string roomName)
        {
            RoomName = roomName;
        }

        /// <summary>
        ///     Gets the room for which to drop membership.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomDropMembership)
                .WriteString(RoomName)
                .Build();
        }
    }
}