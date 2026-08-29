// <copyright file="SearchRequestResponseEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised upon the disposition of a search request response.
    /// </summary>
    public class SearchRequestResponseEventArgs : SearchRequestEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchRequestResponseEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The unique token for the request.</param>
        /// <param name="query">The search query.</param>
        /// <param name="searchResponse">The search response.</param>
        public SearchRequestResponseEventArgs(string username, int token, string query, SearchResponse searchResponse)
            : base(username, token, query)
        {
            SearchResponse = searchResponse;
        }

        /// <summary>
        ///     Gets the search response.
        /// </summary>
        public SearchResponse SearchResponse { get; }
    }
}