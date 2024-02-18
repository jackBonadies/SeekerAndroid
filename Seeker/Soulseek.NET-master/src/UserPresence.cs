// <copyright file="UserPresence.cs" company="JP Dillingham">
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
    ///     User status.
    /// </summary>
    public enum UserPresence
    {
        /// <summary>
        ///     Offline.
        /// </summary>
        Offline = 0,

        /// <summary>
        ///     Away.
        /// </summary>
        Away = 1,

        /// <summary>
        ///     Online.
        /// </summary>
        Online = 2,
    }

    /// <summary>
    ///     User status.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        ///     Normal.
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     Mod.
        /// </summary>
        Operator = 1,

        /// <summary>
        ///     Owner.
        /// </summary>
        Owner = 2,
    }
}