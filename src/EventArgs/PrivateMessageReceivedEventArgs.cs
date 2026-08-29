// <copyright file="PrivateMessageReceivedEventArgs.cs" company="JP Dillingham">
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
    using System;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Event arguments for events raised upon receipt of a private message.
    /// </summary>
    public class PrivateMessageReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="id">The unique id of the message.</param>
        /// <param name="timestamp">The UTC timestamp at which the message was sent.</param>
        /// <param name="username">The username of the user which sent the message.</param>
        /// <param name="message">The message content.</param>
        /// <param name="replayed">A value indicating whether the message was replayed from a previous time.</param>
        public PrivateMessageReceivedEventArgs(int id, DateTime timestamp, string username, string message, bool replayed)
        {
            Id = id;
            Timestamp = timestamp;
            Username = username;
            Message = message;
            Replayed = replayed;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateMessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal PrivateMessageReceivedEventArgs(PrivateMessageNotification notification)
            : this(notification.Id, notification.Timestamp, notification.Username, notification.Message, notification.Replayed)
        {
        }

        /// <summary>
        ///     Gets the unique id of the message.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     Gets the message content.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets a value indicating whether the message was replayed from a previous time.
        /// </summary>
        public bool Replayed { get; }

        /// <summary>
        ///     Gets the UTC timestamp at which the message was sent.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        ///     Gets the username of the user which sent the message.
        /// </summary>
        public string Username { get; }
    }
}