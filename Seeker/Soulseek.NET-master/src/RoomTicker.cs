// <copyright file="RoomTicker.cs" company="JP Dillingham">
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
    ///     A chat room ticker.
    /// </summary>
    public class RoomTicker
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTicker"/> class.
        /// </summary>
        /// <param name="username">The username of the user to which the ticker belongs.</param>
        /// <param name="message">The ticker message.</param>
        public RoomTicker(string username, string message)
        {
            Username = username;
            Message = message;
        }

        /// <summary>
        ///     Gets username of the user to which the ticker belongs.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets the ticker message.
        /// </summary>
        public string Message { get; }
    }
}