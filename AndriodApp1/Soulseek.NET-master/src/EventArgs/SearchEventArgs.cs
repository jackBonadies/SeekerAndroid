// <copyright file="SearchEventArgs.cs" company="JP Dillingham">
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
    ///     Generic event arguments for search events.
    /// </summary>
    public abstract class SearchEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchEventArgs"/> class.
        /// </summary>
        /// <param name="search">The search which raised the event.</param>
        protected SearchEventArgs(Search search)
        {
            Search = search;
        }

        /// <summary>
        ///     Gets the instance which raised the event.
        /// </summary>
        public Search Search { get; }
    }
}