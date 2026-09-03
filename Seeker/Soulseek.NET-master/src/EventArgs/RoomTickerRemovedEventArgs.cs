// <copyright file="RoomTickerRemovedEventArgs.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Event arguments for events raised when a ticker is removed from a chat room.
    /// </summary>
    public class RoomTickerRemovedEventArgs : RoomTickerEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerRemovedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the chat room from which the ticker was removed.</param>
        /// <param name="username">The name of the user to which the ticker belonged.</param>
        public RoomTickerRemovedEventArgs(string roomName, string username)
            : base(roomName)
        {
            Username = username;
        }

        /// <summary>
        ///     Gets the name of the user to which the ticker belonged.
        /// </summary>
        public string Username { get; }
    }
}
