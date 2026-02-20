// <copyright file="Search.cs" company="JP Dillingham">
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
    ///     A single file search.
    /// </summary>
    public class Search
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="scope">The scope of the search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="state">The state of the search.</param>
        /// <param name="responseCount">The current number of responses received.</param>
        /// <param name="fileCount">The total number of files contained within received responses.</param>
        /// <param name="lockedFileCount">The total number of locked files contained within received responses.</param>
        public Search(SearchQuery query, SearchScope scope, int token, SearchStates state, int responseCount, int fileCount, int lockedFileCount)
        {
            Query = query;
            Scope = scope;
            Token = token;
            State = state;
            ResponseCount = responseCount;
            FileCount = fileCount;
            LockedFileCount = lockedFileCount;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Search"/> class.
        /// </summary>
        /// <param name="searchInternal">The internal instance from which to copy data.</param>
        internal Search(SearchInternal searchInternal)
            : this(
                searchInternal.Query,
                searchInternal.Scope,
                searchInternal.Token,
                searchInternal.State,
                searchInternal.ResponseCount,
                searchInternal.FileCount,
                searchInternal.LockedFileCount)
        {
        }

        /// <summary>
        ///     Gets the total number of files contained within received responses.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the total number of locked files contained within received responses.
        /// </summary>
        public int LockedFileCount { get; }

        /// <summary>
        ///     Gets the search query.
        /// </summary>
        public SearchQuery Query { get; }

        /// <summary>
        ///     Gets the current number of responses received.
        /// </summary>
        public int ResponseCount { get; }

        /// <summary>
        ///     Gets the scope of the saerch.
        /// </summary>
        public SearchScope Scope { get; }

        /// <summary>
        ///     Gets the state of the search.
        /// </summary>
        public SearchStates State { get; }

        /// <summary>
        ///     Gets the unique identifier for the search.
        /// </summary>
        public int Token { get; }
    }
}