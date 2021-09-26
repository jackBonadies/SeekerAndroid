// <copyright file="RoomLeftEventArgs.cs" company="JP Dillingham">
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
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Event arguments for events raised upon the departure of a user from a chat room.
    /// </summary>
    public class RoomLeftEventArgs : RoomEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomLeftEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the event took place.</param>
        /// <param name="username">The username of the user associated with the event.</param>
        public RoomLeftEventArgs(string roomName, string username)
            : base(roomName, username)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomLeftEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomLeftEventArgs(UserLeftRoomNotification notification)
            : this(notification.RoomName, notification.Username)
        {
        }
    }
}
