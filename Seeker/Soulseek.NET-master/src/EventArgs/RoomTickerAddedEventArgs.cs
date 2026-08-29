// <copyright file="RoomTickerAddedEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek
{
    /// <summary>
    ///     Event arguments for events raised when a new ticker is added to a chat room.
    /// </summary>
    public class RoomTickerAddedEventArgs : RoomTickerEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerAddedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the chat room to which the ticker was added.</param>
        /// <param name="ticker">The ticker.</param>
        public RoomTickerAddedEventArgs(string roomName, RoomTicker ticker)
            : base(roomName)
        {
            Ticker = ticker;
        }

        /// <summary>
        ///     Gets the ticker.
        /// </summary>
        public RoomTicker Ticker { get; }
    }
}
