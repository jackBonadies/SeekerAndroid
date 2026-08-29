// <copyright file="ISearchResponseCache.cs" company="JP Dillingham">
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
    ///     A cache for undelivered search responses.
    /// </summary>
    public interface ISearchResponseCache
    {
        /// <summary>
        ///     Adds or updates the cached <see cref="SearchResponse"/> and context for the specified <paramref name="responseToken"/>.
        /// </summary>
        /// <param name="responseToken">The token for which the response is to be added or updated.</param>
        /// <param name="response">The response and context to cache.</param>
        void AddOrUpdate(int responseToken, (string Username, int Token, string Query, SearchResponse SearchResponse) response);

        /// <summary>
        ///     Attempts to fetch a cached <see cref="SearchResponse"/> and context for the specified <paramref name="responseToken"/>.
        /// </summary>
        /// <param name="responseToken">The token for the cached response.</param>
        /// <param name="response">The cached response and context, if present.</param>
        /// <returns>A value indicating whether a response for the specified <paramref name="responseToken"/> is cached.</returns>
        bool TryGet(int responseToken, out (string Username, int Token, string Query, SearchResponse SearchResponse) response);

        /// <summary>
        ///     Attempts to remove a cached <see cref="SearchResponse"/> and context for the specified <paramref name="responseToken"/>.
        /// </summary>
        /// <param name="responseToken">The token for the cached response to remove.</param>
        /// <param name="response">The cached response and context, if removed.</param>
        /// <returns>A value indicating whether a response for the specified <paramref name="responseToken"/> was removed.</returns>
        bool TryRemove(int responseToken, out (string Username, int Token, string Query, SearchResponse SearchResponse) response);
    }
}