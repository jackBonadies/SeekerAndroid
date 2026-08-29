// <copyright file="ISearchResponder.cs" company="JP Dillingham">
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
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;

    /// <summary>
    ///     Responds to search requests.
    /// </summary>
    internal interface ISearchResponder : IDiagnosticGenerator
    {
        /// <summary>
        ///     Occurs when a search request is received.
        /// </summary>
        event EventHandler<SearchRequestEventArgs> RequestReceived;

        /// <summary>
        ///     Occurs when the response to a search request is delivered.
        /// </summary>
        event EventHandler<SearchRequestResponseEventArgs> ResponseDelivered;

        /// <summary>
        ///     Occurs when the delivery of a response to a search request fails.
        /// </summary>
        event EventHandler<SearchRequestResponseEventArgs> ResponseDeliveryFailed;

        /// <summary>
        ///     Discards the cached response matching the specified <paramref name="responseToken"/>, if one exists.
        /// </summary>
        /// <param name="responseToken">The token matching the cached response to discard.</param>
        /// <returns>A value indicating whether the cached response was discarded.</returns>
        bool TryDiscard(int responseToken);

        /// <summary>
        ///     Responds to the given search request, if a response could be resolved and matche(s) were found.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The token for the search request.</param>
        /// <param name="query">The search query.</param>
        /// <returns>The operation context, including a value indicating whether a response was successfully sent.</returns>
        Task<bool> TryRespondAsync(string username, int token, string query);

        /// <summary>
        ///     Sends the cached response matching the specified <paramref name="responseToken"/>, if one exists.
        /// </summary>
        /// <param name="responseToken">The token matching the cached response to send.</param>
        /// <returns>The operation context, including a value indicating whether a response was successfully sent.</returns>
        Task<bool> TryRespondAsync(int responseToken);
    }
}