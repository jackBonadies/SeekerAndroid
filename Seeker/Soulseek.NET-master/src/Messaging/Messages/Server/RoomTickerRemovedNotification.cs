// <copyright file="RoomTickerRemovedNotification.cs" company="JP Dillingham">
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
    ///     A notification that a ticker has been removed from a chat room.
    /// </summary>
    internal sealed class RoomTickerRemovedNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerRemovedNotification"/> class.
        /// </summary>
        /// <param name="roomName">The name of the chat room from which the ticker was removed.</param>
        /// <param name="username">The name of the user to which the ticker belonged.</param>
        public RoomTickerRemovedNotification(
            string roomName,
            string username)
        {
            RoomName = roomName;
            Username = username;
        }

        /// <summary>
        ///     Gets the name of the chat room from which the ticker was removed.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the name of the user to which the ticker belonged.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="RoomTickerRemovedNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static RoomTickerRemovedNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.RoomTickerRemove)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(RoomTickerRemovedNotification)} (expected: {(int)MessageCode.Server.RoomTickerRemove}, received: {(int)code}");
            }

            var roomName = reader.ReadString();
            var username = reader.ReadString();

            return new RoomTickerRemovedNotification(roomName, username);
        }
    }
}