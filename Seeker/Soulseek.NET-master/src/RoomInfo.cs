﻿// <copyright file="RoomInfo.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Information about a chat room.
    /// </summary>
    public class RoomInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomInfo"/> class.
        /// </summary>
        /// <param name="name">The room name.</param>
        /// <param name="userCount">The number of users in the room.</param>
        public RoomInfo(string name, int userCount)
        {
            Name = name;
            Users = new List<string>().AsReadOnly();
            UserCount = userCount;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomInfo"/> class.
        /// </summary>
        /// <param name="name">The room name.</param>
        /// <param name="userList">The users in the room, if available.</param>
        public RoomInfo(string name, IEnumerable<string> userList)
        {
            Name = name;
            Users = (userList?.ToList() ?? new List<string>()).AsReadOnly();
            UserCount = Users.Count;
        }

        /// <summary>
        ///     Gets the room name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the number of users in the room.
        /// </summary>
        public int UserCount { get; }

        /// <summary>
        ///     Gets the users in the room, if available.
        /// </summary>
        public IReadOnlyCollection<string> Users { get; }
    }

    /// <summary>
    /// Dummy RoomInfoCategory for Adapter
    /// </summary>
    public class RoomInfoCategory : RoomInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomInfoCategory"/> class.
        /// </summary>
        /// <param name="name">The category name.</param>
        /// <param name="userCount">The number of users in the room.</param>
        public RoomInfoCategory(string name) : base(name, null)
        {
        }
    }
}