// <copyright file="SearchScopeType.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
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