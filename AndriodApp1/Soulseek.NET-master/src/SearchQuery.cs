// <copyright file="SearchQuery.cs" company="JP Dillingham">
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
    using System.Text;

    /// <summary>
    ///     A search query.
    /// </summary>
    public class SearchQuery
    {
        private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchQuery"/> class.
        /// </summary>
        /// <param name="terms">The list of search terms.</param>
        /// <param name="exclusions">The list of excluded terms.</param>
        public SearchQuery(IEnumerable<string> terms, IEnumerable<string> exclusions = null)
        {
            Terms = (terms?.ToList() ?? new List<string>()).AsReadOnly();
            Exclusions = (exclusions?.ToList() ?? new List<string>()).AsReadOnly();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchQuery"/> class.
        /// </summary>
        /// <param name="query">The query text.</param>
        /// <param name="exclusions">The list of excluded terms.</param>
        public SearchQuery(string query, IEnumerable<string> exclusions)
            : this(query?.Split(' '), exclusions)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchQuery"/> class.
        /// </summary>
        /// <param name="searchText">The full search text of the query.</param>
        public SearchQuery(string searchText)
        {
            IEnumerable<string> tokens = searchText?.Split(' ') ?? Enumerable.Empty<string>();

            var excludedTokens = tokens.Where(t => t.StartsWith("-", IgnoreCase) && t.Length > 1);
            Exclusions = excludedTokens.Select(t => t.TrimStart('-')).Distinct().ToList().AsReadOnly();

            Terms = tokens.Where(token => !excludedTokens.Contains(token)).ToList().AsReadOnly();
        }

        /// <summary>
        ///     Gets the list of excluded terms.
        /// </summary>
        public IReadOnlyCollection<string> Exclusions { get; }

        /// <summary>
        ///     Gets the query text, concatenated from <see cref="Terms"/>.
        /// </summary>
        public string Query => string.Join(" ", Terms);

        /// <summary>
        ///     Gets the full search text, including both <see cref="Terms"/> and <see cref="Exclusions"/>.
        /// </summary>
        public string SearchText => ToString();

        /// <summary>
        ///     Gets the list of search terms.
        /// </summary>
        public IReadOnlyCollection<string> Terms { get; }

        /// <summary>
        ///     Returns a new instance of <see cref="SearchQuery"/> from the specified search text.
        /// </summary>
        /// <param name="searchText">The text from which to create the query.</param>
        /// <returns>The new SearchQuery instance.</returns>
        public static SearchQuery FromText(string searchText) => new SearchQuery(searchText);

        /// <summary>
        ///     Returns the full search text.
        /// </summary>
        /// <returns>The full search text.</returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(Query);
            builder.Append(Exclusions.Count > 0 ? " " + string.Join(" ", Exclusions.Select(e => $"-{e}")) : string.Empty);

            return builder.ToString();
        }
    }
}