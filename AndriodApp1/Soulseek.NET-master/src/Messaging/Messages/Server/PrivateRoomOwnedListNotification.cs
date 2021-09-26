// <copyright file="PrivateRoomOwnedListNotification.cs" company="JP Dillingham">
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
    ///     An incoming list of users in a private chat room over which the currently logged in user has moderation rights.
    /// </summary>
    internal sealed class PrivateRoomOwnedListNotification : IIncomingMessage
    {
        /// <summary>
        ///     Creates a new instance of <see cref="PrivateRoomOwnedListNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static RoomInfo FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.PrivateRoomOwned)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PrivateRoomOwnedListNotification)} (expected: {(int)MessageCode.Server.PrivateRoomOwned}, received: {(int)code}");
            }

            var roomName = reader.ReadString();
            var userCount = reader.ReadInteger();

            var userList = new List<string>();

            for (int i = 0; i < userCount; i++)
            {
                userList.Add(reader.ReadString());
            }

            return new RoomInfo(roomName, userList);
        }
    }
}