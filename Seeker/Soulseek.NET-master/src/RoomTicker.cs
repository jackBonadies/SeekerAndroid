// <copyright file="RoomTicker.cs" company="JP Dillingham">
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
    ///     A chat room ticker.
    /// </summary>
    public class RoomTicker
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTicker"/> class.
        /// </summary>
        /// <param name="username">The username of the user to which the ticker belongs.</param>
        /// <param name="message">The ticker message.</param>
        public RoomTicker(string username, string message)
        {
            Username = username;
            Message = message;
        }

        /// <summary>
        ///     Gets username of the user to which the ticker belongs.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets the ticker message.
        /// </summary>
        public string Message { get; }
    }
}