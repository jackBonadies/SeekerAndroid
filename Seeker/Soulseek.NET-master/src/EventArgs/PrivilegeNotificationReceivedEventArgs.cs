// <copyright file="PrivilegeNotificationReceivedEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised upon notification of new privileges.
    /// </summary>
    public class PrivilegeNotificationReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivilegeNotificationReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username of the new privileged user.</param>
        /// <param name="id">The unique id of the notification, if applicable.</param>
        public PrivilegeNotificationReceivedEventArgs(string username, int? id = null)
        {
            Username = username;
            Id = id;

            RequiresAcknowlegement = Id.HasValue;
        }

        /// <summary>
        ///     Gets the username of the new privileged user.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets the unique id of the notification, if applicable.
        /// </summary>
        public int? Id { get; }

        /// <summary>
        ///     Gets a value indicating whether the notification must be acknowleged.
        /// </summary>
        public bool RequiresAcknowlegement { get; }
    }
}