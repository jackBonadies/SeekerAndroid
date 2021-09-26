// <copyright file="UserStatusChangedEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised by user state changed events.
    /// </summary>
    public class UserStatusChangedEventArgs : UserEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusChangedEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="status">The status of the user.</param>
        /// <param name="isPrivileged">A value indicating whether the user is privileged.</param>
        public UserStatusChangedEventArgs(string username, UserPresence status, bool isPrivileged = false)
            : base(username)
        {
            Status = status;
            IsPrivileged = isPrivileged;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusChangedEventArgs"/> class.
        /// </summary>
        /// <param name="userStatusResponse">The status response which generated the event.</param>
        internal UserStatusChangedEventArgs(UserStatusResponse userStatusResponse)
            : this(userStatusResponse.Username, userStatusResponse.Status, userStatusResponse.IsPrivileged)
        {
        }

        /// <summary>
        ///     Gets a value indicating whether the user is privileged.
        /// </summary>
        public bool IsPrivileged { get; }

        /// <summary>
        ///     Gets the status of the user.
        /// </summary>
        public UserPresence Status { get; }
    }
}