// <copyright file="IUserEndPointCache.cs" company="JP Dillingham">
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
    using System.Net;

    /// <summary>
    ///     A cache for user endpoints.
    /// </summary>
    public interface IUserEndPointCache
    {
        /// <summary>
        ///     Attempts to fetch a cached <see cref="IPEndPoint"/> for the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username for which the endpoint is to be fetched.</param>
        /// <param name="endPoint">The cached endpoint, or null if not cached.</param>
        /// <returns>A value indicating whether an endpoint for the specified <paramref name="username"/> is cached.</returns>
        bool TryGet(string username, out IPEndPoint endPoint);

        /// <summary>
        ///     Adds or updates the cached <see cref="IPEndPoint"/> for the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username for which the endpoint is to be added or updated.</param>
        /// <param name="endPoint">The endpoint to cache.</param>
        void AddOrUpdate(string username, IPEndPoint endPoint);
    }
}
