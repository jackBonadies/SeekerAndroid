// <copyright file="RoomList.cs" company="JP Dillingham">
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
    public class RoomList
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomList"/> class.
        /// </summary>
        /// <param name="publicList">The list of public rooms.</param>
        /// <param name="privateList">The list of private rooms.</param>
        /// <param name="ownedList">The list of rooms owned by the currently logged in user.</param>
        /// <param name="moderatedRoomNameList">The list of room names in which the currently logged in user has moderator status.</param>
        public RoomList(
            IEnumerable<RoomInfo> publicList,
            IEnumerable<RoomInfo> privateList,
            IEnumerable<RoomInfo> ownedList,
            IEnumerable<string> moderatedRoomNameList)
        {
            Public = (publicList?.ToList() ?? new List<RoomInfo>()).AsReadOnly();
            PublicCount = Public.Count;

            Private = (privateList?.ToList() ?? new List<RoomInfo>()).AsReadOnly();
            PrivateCount = Private.Count;

            Owned = (ownedList?.ToList() ?? new List<RoomInfo>()).AsReadOnly();
            OwnedCount = Owned.Count;

            ModeratedRoomNames = (moderatedRoomNameList?.ToList() ?? new List<string>()).AsReadOnly();
            ModeratedRoomNameCount = ModeratedRoomNames.Count;
        }

        /// <summary>
        ///     Gets the number of public rooms.
        /// </summary>
        public int PublicCount { get; }

        /// <summary>
        ///     Gets the number of private rooms.
        /// </summary>
        public int PrivateCount { get; }

        /// <summary>
        ///     Gets the number of rooms owned by the currently logged in user.
        /// </summary>
        public int OwnedCount { get; }

        /// <summary>
        ///     Gets the number of room names in which the currently logged in user has moderator status.
        /// </summary>
        public int ModeratedRoomNameCount { get; }

        /// <summary>
        ///     Gets the list of public rooms.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Public { get; }

        /// <summary>
        ///     Gets the list of private rooms.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Private { get; }

        /// <summary>
        ///     Gets the list of rooms owned by the currently logged in user.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Owned { get; }

        /// <summary>
        ///     Gets the list of room names in which the currently logged in user has moderator status.
        /// </summary>
        public IReadOnlyCollection<string> ModeratedRoomNames { get; }
    }
}