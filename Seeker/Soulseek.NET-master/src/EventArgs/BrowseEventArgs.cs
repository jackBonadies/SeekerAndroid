// <copyright file="BrowseEventArgs.cs" company="JP Dillingham">
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
    ///     Generic event arguments for browse events.
    /// </summary>
    public class BrowseEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username associated with the event.</param>
        public BrowseEventArgs(string username)
        {
            Username = username;
        }

        /// <summary>
        ///     Gets the username associated with the event.
        /// </summary>
        public string Username { get; }
    }
}