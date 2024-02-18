// <copyright file="DistributedChildEventArgs.cs" company="JP Dillingham">
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
    using System.Net;

    /// <summary>
    ///     Event arguments for the event raised when a distributed child connection changes.
    /// </summary>
    public class DistributedChildEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedChildEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username associated with the connection.</param>
        /// <param name="ipEndPoint">The IP endpoint of the connection.</param>
        public DistributedChildEventArgs(string username, IPEndPoint ipEndPoint)
        {
            Username = username;
            IPEndPoint = ipEndPoint;
        }

        /// <summary>
        ///     Gets the IP endpoint of the connection.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Gets the username associated with the connection.
        /// </summary>
        public string Username { get; }
    }
}