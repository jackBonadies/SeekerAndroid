// <copyright file="RoomTickerListNotification.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     An incoming list of tickers for a chat room.
    /// </summary>
    internal sealed class RoomTickerListNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerListNotification"/> class.
        /// </summary>
        /// <param name="roomName">The name of the chat room to which the list applies.</param>
        /// <param name="tickerCount">The number of tickers.</param>
        /// <param name="tickers">The list of tickers.</param>
        public RoomTickerListNotification(
            string roomName,
            int tickerCount,
            IEnumerable<RoomTicker> tickers)
        {
            RoomName = roomName;
            TickerCount = tickerCount;
            Tickers = tickers.ToList().AsReadOnly();
        }

        /// <summary>
        ///     Gets the name of the room to which the list applies.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the number of tickers.
        /// </summary>
        public int TickerCount { get; }

        /// <summary>
        ///     Gets the list of tickers.
        /// </summary>
        public IReadOnlyCollection<RoomTicker> Tickers { get; }

        /// <summary>
        ///     Creates a new list of privileged users from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static RoomTickerListNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.RoomTickers)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(RoomTickerListNotification)} (expected: {(int)MessageCode.Server.RoomTickers}, received: {(int)code}");
            }

            var roomName = reader.ReadString();
            var tickerCount = reader.ReadInteger();
            var tickers = new List<RoomTicker>();

            for (int i = 0; i < tickerCount; i++)
            {
                var username = reader.ReadString();
                var message = reader.ReadString();

                tickers.Add(new RoomTicker(username, message));
            }

            return new RoomTickerListNotification(roomName, tickerCount, tickers);
        }
    }
}