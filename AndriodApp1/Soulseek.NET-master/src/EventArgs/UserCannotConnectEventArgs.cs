// <copyright file="UserCannotConnectEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised when a user reports that they cannot connect.
    /// </summary>
    public class UserCannotConnectEventArgs : UserEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserCannotConnectEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="token">The unique connection token.</param>
        public UserCannotConnectEventArgs(int token, string username)
            : base(username)
        {
            Token = token;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserCannotConnectEventArgs"/> class.
        /// </summary>
        /// <param name="cannotConnect">The server message which generated the event.</param>
        internal UserCannotConnectEventArgs(CannotConnect cannotConnect)
            : this(cannotConnect.Token, cannotConnect.Username)
        {
        }

        /// <summary>
        ///     Gets the unique connection token.
        /// </summary>
        public int Token { get; }
    }
}