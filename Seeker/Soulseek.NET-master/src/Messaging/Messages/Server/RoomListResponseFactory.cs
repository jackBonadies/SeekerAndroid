// <copyright file="RoomListResponseFactory.cs" company="JP Dillingham">
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
    using System.Collections.Generic;

    /// <summary>
    ///     A list of available chat rooms.
    /// </summary>
    internal sealed class RoomListResponseFactory : IIncomingMessage
    {
        /// <summary>
        ///     Creates a new list of rooms from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static RoomList FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.RoomList)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(RoomListResponseFactory)} (expected: {(int)MessageCode.Server.RoomList}, received: {(int)code}");
            }

            var rooms = ReadRoomInfoList(reader);
            var ownedRooms = ReadRoomInfoList(reader);
            var privateRooms = ReadRoomInfoList(reader);
            var moderatedRoomNames = ReadRoomNameList(reader);

            return new RoomList(
                publicList: rooms,
                privateList: privateRooms,
                ownedList: ownedRooms,
                moderatedRoomNameList: moderatedRoomNames);
        }

        private static List<RoomInfo> ReadRoomInfoList(MessageReader<MessageCode.Server> reader)
        {
            var roomNames = ReadRoomNameList(reader);

            var userCountCount = reader.ReadInteger();
            var rooms = new List<RoomInfo>();

            for (int i = 0; i < userCountCount; i++)
            {
                var count = reader.ReadInteger();
                rooms.Add(new RoomInfo(roomNames[i], count));
            }

            return rooms;
        }

        private static List<string> ReadRoomNameList(MessageReader<MessageCode.Server> reader)
        {
            var roomCount = reader.ReadInteger();
            var roomNames = new List<string>();

            for (int i = 0; i < roomCount; i++)
            {
                roomNames.Add(reader.ReadString());
            }

            return roomNames;
        }
    }
}