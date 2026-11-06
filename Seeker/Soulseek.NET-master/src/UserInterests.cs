// <copyright file="UserInterests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A user's interests.
    /// </summary>
    public class UserInterests
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInterests"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="likes">The list of things the user likes.</param>
        /// <param name="hates">The list of things the user hates.</param>
        public UserInterests(string username, IEnumerable<string> likes, IEnumerable<string> hates)
        {
            Username = username;

            Likes = (likes?.ToList() ?? new List<string>()).AsReadOnly();
            Hates = (hates?.ToList() ?? new List<string>()).AsReadOnly();
        }

        /// <summary>
        ///     Gets the list of things the user hates.
        /// </summary>
        public IReadOnlyCollection<string> Hates { get; }

        /// <summary>
        ///     Gets the list of things the user likes.
        /// </summary>
        public IReadOnlyCollection<string> Likes { get; }

        /// <summary>
        ///     Gets the username of the user.
        /// </summary>
        public string Username { get; }
    }
}
