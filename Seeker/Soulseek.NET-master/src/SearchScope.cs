// <copyright file="SearchScope.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Search scope definition.
    /// </summary>
    public class SearchScope
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchScope"/> class.
        /// </summary>
        /// <param name="type">The scope type.</param>
        /// <param name="subjects">The scope subjects, if applicable.</param>
        public SearchScope(SearchScopeType type, params string[] subjects)
        {
            Type = type;

            subjects ??= Array.Empty<string>();

            if ((Type == SearchScopeType.Network || Type == SearchScopeType.Wishlist) && subjects.Length > 0)
            {
                throw new ArgumentException($"The {Type} search scope can not be used with subjects", nameof(subjects));
            }

            if (Type == SearchScopeType.Room && (subjects.Length != 1 || string.IsNullOrEmpty(subjects[0])))
            {
                throw new ArgumentException($"The Room search scope requires a single, non null and non empty subject", nameof(subjects));
            }

            if (Type == SearchScopeType.User)
            {
                if (subjects.Length == 0)
                {
                    throw new ArgumentException($"The User search scope requires at least one subject", nameof(subjects));
                }

                if (subjects.Any(s => string.IsNullOrEmpty(s)))
                {
                    throw new ArgumentException($"One or more of the supplied User scope subjects is null or empty", nameof(subjects));
                }
            }

            Subjects = subjects;
        }

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.Network"/> scope.
        /// </summary>
        public static SearchScope Network => new SearchScope(SearchScopeType.Network);

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.Wishlist"/> scope.
        /// </summary>
        public static SearchScope Wishlist => new SearchScope(SearchScopeType.Wishlist);

        /// <summary>
        ///     Gets the scope subjects, if applicable.
        /// </summary>
        /// <remarks>Ignored for <see cref="SearchScopeType.Network"/> and <see cref="SearchScopeType.Wishlist"/>.</remarks>
        public IEnumerable<string> Subjects { get; }

        /// <summary>
        ///     Gets the scope type.
        /// </summary>
        public SearchScopeType Type { get; }

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.Room"/> scope with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The room to search.</param>
        /// <returns>A Room scope with the specified <paramref name="roomName"/>.</returns>
        public static SearchScope Room(string roomName) => new SearchScope(SearchScopeType.Room, roomName);

        /// <summary>
        ///     Gets a <see cref="SearchScopeType.User"/> scope with the specified <paramref name="usernames"/>.
        /// </summary>
        /// <param name="usernames">The username(s) of the user(s) to search.</param>
        /// <returns>A User scope with the specified <paramref name="usernames"/>.</returns>
        public static SearchScope User(params string[] usernames) => new SearchScope(SearchScopeType.User, usernames);
    }
}