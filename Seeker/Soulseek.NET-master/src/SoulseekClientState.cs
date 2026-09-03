// <copyright file="SoulseekClientState.cs" company="JP Dillingham">
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
    using System;

    /// <summary>
    ///     Soulseek client state.
    /// </summary>
    [Flags]
    public enum SoulseekClientStates
    {
        /// <summary>
        ///     None.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Disconnected.
        /// </summary>
        Disconnected = 1,

        /// <summary>
        ///     Connected.
        /// </summary>
        Connected = 2,

        /// <summary>
        ///     Logged in.
        /// </summary>
        LoggedIn = 4,

        /// <summary>
        ///     Connecting.
        /// </summary>
        Connecting = 8,

        /// <summary>
        ///     Logging in.
        /// </summary>
        LoggingIn = 16,

        /// <summary>
        ///     Disconnecting.
        /// </summary>
        Disconnecting = 32,
    }
}