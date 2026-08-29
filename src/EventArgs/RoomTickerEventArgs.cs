// <copyright file="RoomTickerEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events related to chat room tickers.
    /// </summary>
    public abstract class RoomTickerEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the chat room associated with the event.</param>
        protected RoomTickerEventArgs(string roomName)
        {
            RoomName = roomName;
        }

        /// <summary>
        ///     Gets the name of the chat room associated with the event.
        /// </summary>
        public string RoomName { get; }
    }
}
