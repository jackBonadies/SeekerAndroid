// <copyright file="RoomJoinedEventArgs.cs" company="JP Dillingham">
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
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Event arguments for events raised upon the join of a user to a chat room.
    /// </summary>
    public class RoomJoinedEventArgs : RoomEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomJoinedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the event took place.</param>
        /// <param name="username">The username of the user associated with the event.</param>
        /// <param name="userData">The user's data.</param>
        public RoomJoinedEventArgs(string roomName, string username, UserData userData)
            : base(roomName, username)
        {
            UserData = userData;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomJoinedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomJoinedEventArgs(UserJoinedRoomNotification notification)
            : this(notification.RoomName, notification.Username, notification.UserData)
        {
        }

        /// <summary>
        ///     Gets the user's data.
        /// </summary>
        public UserData UserData { get; }
    }
}
