// <copyright file="RoomMessageReceivedEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised upon receipt of a chat room message.
    /// </summary>
    public class RoomMessageReceivedEventArgs : RoomEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room in which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        public RoomMessageReceivedEventArgs(string roomName, string username, string message)
            : base(roomName, username)
        {
            Message = message;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomMessageReceivedEventArgs(RoomMessageNotification notification)
            : this(notification.RoomName, notification.Username, notification.Message)
        {
        }

        /// <summary>
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }
    }
}
