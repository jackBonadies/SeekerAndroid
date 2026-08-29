// <copyright file="RoomTickerAddedNotification.cs" company="JP Dillingham">
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
    ///     A notification that a new ticker has been added to a chat room.
    /// </summary>
    internal sealed class RoomTickerAddedNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerAddedNotification"/> class.
        /// </summary>
        /// <param name="roomName">The name of the chat room to which the ticker was added.</param>
        /// <param name="ticker">The ticker.</param>
        public RoomTickerAddedNotification(
            string roomName,
            RoomTicker ticker)
        {
            RoomName = roomName;
            Ticker = ticker;
        }

        /// <summary>
        ///     Gets the name of the chat room to which the ticker was added.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the ticker.
        /// </summary>
        public RoomTicker Ticker { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="RoomTickerAddedNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static RoomTickerAddedNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.RoomTickerAdd)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(RoomTickerAddedNotification)} (expected: {(int)MessageCode.Server.RoomTickerAdd}, received: {(int)code}");
            }

            var roomName = reader.ReadString();
            var username = reader.ReadString();
            var message = reader.ReadString();

            return new RoomTickerAddedNotification(roomName, new RoomTicker(username, message));
        }
    }
}