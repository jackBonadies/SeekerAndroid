// <copyright file="SearchRequestResponseEventArgs.cs" company="JP Dillingham">
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