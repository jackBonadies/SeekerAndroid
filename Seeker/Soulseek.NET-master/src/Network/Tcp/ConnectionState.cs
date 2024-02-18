﻿// <copyright file="ConnectionState.cs" company="JP Dillingham">
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

namespace Soulseek.Network.Tcp
{
    /// <summary>
    ///     Connection state.
    /// </summary>
    internal enum ConnectionState
    {
        /// <summary>
        ///     Pending.
        /// </summary>
        Pending = 0,

        /// <summary>
        ///     Connecting.
        /// </summary>
        Connecting = 1,

        /// <summary>
        ///     Connected.
        /// </summary>
        Connected = 2,

        /// <summary>
        ///     Disconnecting.
        /// </summary>
        Disconnecting = 3,

        /// <summary>
        ///     Disconnected.
        /// </summary>
        Disconnected = 4,
    }
}