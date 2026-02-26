// <copyright file="OperatorAddedRemovedEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for operator added or removed to our room
    /// </summary>
    public class OperatorAddedRemovedEventArgs : System.EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OperatorAddedRemovedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the chat room.</param>
        /// <param name="username">The name of the user.</param>
        /// <param name="added">Whether the operator was added (true) or removed (false).</param>
        public OperatorAddedRemovedEventArgs(string roomName, string username, bool added)
        {
            RoomName = roomName;
            Username = username;
            Added = added;
        }

        /// <summary>
        ///     Gets the name of the user
        /// </summary>
        public string Username { get; }
        /// <summary>
        ///     Gets the room name
        /// </summary>
        public string RoomName { get; }
        /// <summary>
        ///     Gets if Added (else removed)
        /// </summary>
        public bool Added { get; }
    }
}
