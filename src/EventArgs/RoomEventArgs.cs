// <copyright file="RoomEventArgs.cs" company="JP Dillingham">
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
    ///     Generic event arguments for chat room events.
    /// </summary>
    public abstract class RoomEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the event took place.</param>
        /// <param name="username">The username of the user associated with the event.</param>
        protected RoomEventArgs(string roomName, string username)
        {
            RoomName = roomName;
            Username = username;
        }

        /// <summary>
        ///     Gets the name of the room in which the event took place.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the username of the user associated with the event.
        /// </summary>
        public string Username { get; }
    }
}
