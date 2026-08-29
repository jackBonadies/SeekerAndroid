// <copyright file="SearchScopeType.cs" company="JP Dillingham">
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
    ///     Search scope type.
    /// </summary>
    public enum SearchScopeType
    {
        /// <summary>
        ///     Network.
        /// </summary>
        Network = 0,

        /// <summary>
        ///     User.
        /// </summary>
        User = 1,

        /// <summary>
        ///     Room.
        /// </summary>
        Room = 2,

        /// <summary>
        ///     Wishlist.
        /// </summary>
        Wishlist = 3,
    }
}