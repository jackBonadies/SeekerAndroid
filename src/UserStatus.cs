// <copyright file="UserStatus.cs" company="JP Dillingham">
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
    ///     User status.
    /// </summary>
    public class UserStatus
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatus"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="presence">The user's network presence.</param>
        /// <param name="isPrivileged">A value indicating whether the user is privileged.</param>
        public UserStatus(string username, UserPresence presence, bool isPrivileged)
        {
            Username = username;
            Presence = presence;
            IsPrivileged = isPrivileged;
        }

        /// <summary>
        ///     Gets a value indicating whether the user is privileged.
        /// </summary>
        public bool IsPrivileged { get; }

        /// <summary>
        ///     Gets the user's network presence.
        /// </summary>
        public UserPresence Presence { get; }

        /// <summary>
        ///     Gets the username of the user.
        /// </summary>
        public string Username { get; }
    }
}