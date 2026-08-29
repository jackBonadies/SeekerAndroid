// <copyright file="RoomTickerListReceivedEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Event arguments for events raised upon the receipt of a list of chat room tickers.
    /// </summary>
    public class RoomTickerListReceivedEventArgs : RoomTickerEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room to which the list applies.</param>
        /// <param name="tickers">The list of tickers.</param>
        public RoomTickerListReceivedEventArgs(string roomName, IEnumerable<RoomTicker> tickers)
            : base(roomName)
        {
            Tickers = (tickers?.ToList() ?? new List<RoomTicker>()).AsReadOnly();
            TickerCount = Tickers.Count;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomTickerListReceivedEventArgs(RoomTickerListNotification notification)
            : this(notification.RoomName, notification.Tickers)
        {
        }

        /// <summary>
        ///     Gets the number of tickers.
        /// </summary>
        public int TickerCount { get; }

        /// <summary>
        ///     Gets the list of tickers.
        /// </summary>
        public IReadOnlyCollection<RoomTicker> Tickers { get; }
    }
}
