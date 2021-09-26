// <copyright file="CannotJoinRoom.cs" company="JP Dillingham">
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
    ///     A message indicating an unsuccessful attempt to join a chat room.
    /// </summary>
    internal sealed class CannotJoinRoom : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CannotJoinRoom"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room which could not be joined.</param>
        public CannotJoinRoom(string roomName)
        {
            RoomName = roomName;
        }

        /// <summary>
        ///     Gets the name of the room which could not be joined.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="CannotJoinRoom"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static CannotJoinRoom FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.CannotJoinRoom)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(CannotJoinRoom)} (expected: {(int)MessageCode.Server.CannotJoinRoom}, received: {(int)code})");
            }

            var roomName = reader.ReadString();

            return new CannotJoinRoom(roomName);
        }
    }
}