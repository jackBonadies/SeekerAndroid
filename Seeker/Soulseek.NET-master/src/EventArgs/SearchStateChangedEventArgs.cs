// <copyright file="SearchStateChangedEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised by a change in search state.
    /// </summary>
    public class SearchStateChangedEventArgs : SearchEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the client.</param>
        /// <param name="search">The search instance with which to initialize data.</param>
        internal SearchStateChangedEventArgs(SearchStates previousState, Search search)
            : base(search)
        {
            PreviousState = previousState;
        }

        /// <summary>
        ///     Gets the previous search state.
        /// </summary>
        public SearchStates PreviousState { get; }
    }
}