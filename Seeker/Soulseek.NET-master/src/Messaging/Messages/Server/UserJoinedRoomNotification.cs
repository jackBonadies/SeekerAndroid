// <copyright file="UserJoinedRoomNotification.cs" company="JP Dillingham">
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
    ///     An incoming notification that a user has joined a chat room.
    /// </summary>
    internal sealed class UserJoinedRoomNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserJoinedRoomNotification"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room which the user joined.</param>
        /// <param name="username">The username of the user that joined.</param>
        /// <param name="userData">The user's data.</param>
        public UserJoinedRoomNotification(string roomName, string username, UserData userData)
        {
            RoomName = roomName;
            Username = username;
            UserData = userData;
        }

        /// <summary>
        ///     Gets the name of the room which the user joined.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the user's data.
        /// </summary>
        public UserData UserData { get; }

        /// <summary>
        ///     Gets the name of the user that joined.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserJoinedRoomNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserJoinedRoomNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.UserJoinedRoom)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserJoinedRoomNotification)} (expected: {(int)MessageCode.Server.UserJoinedRoom}, received: {(int)code}");
            }

            var roomName = reader.ReadString();
            var username = reader.ReadString();

            var status = (UserPresence)reader.ReadInteger();
            var averageSpeed = reader.ReadInteger();
            var downloadCount = reader.ReadLong();
            var fileCount = reader.ReadInteger();
            var directoryCount = reader.ReadInteger();
            var slotsFree = reader.ReadInteger();
            string countryCode = reader.ReadString();

            var userData = new UserData(username, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode, slotsFree);

            return new UserJoinedRoomNotification(roomName, username, userData);
        }
    }
}