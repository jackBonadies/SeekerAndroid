// <copyright file="SearchResponder.cs" company="JP Dillingham">
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
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

    /// <summary>
    ///     Responds to search requests.
    /// </summary>
    internal class SearchResponder : ISearchResponder
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponder"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public SearchResponder(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when a search request is received.
        /// </summary>
        public event EventHandler<SearchRequestEventArgs> RequestReceived;

        /// <summary>
        ///     Occurs when the response to a search request is delivered.
        /// </summary>
        public event EventHandler<SearchRequestResponseEventArgs> ResponseDelivered;

        /// <summary>
        ///     Occurs when the delivery of a response to a search request fails.
        /// </summary>
        public event EventHandler<SearchRequestResponseEventArgs> ResponseDeliveryFailed;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Discards the cached response matching the specified <paramref name="responseToken"/>, if one exists.
        /// </summary>
        /// <param name="responseToken">The token matching the cached response to discard.</param>
        /// <returns>A value indicating whether the cached response was discarded.</returns>
        public bool TryDiscard(int responseToken)
        {
            if (SoulseekClient.Options.SearchResponseCache != default)
            {
                try
                {
                    if (SoulseekClient.Options.SearchResponseCache.TryRemove(responseToken, out var response))
                    {
                        var (username, token, query, searchResponse) = response;

                        Diagnostic.Debug($"Discarded cached search response {responseToken} to {username} for query '{query}' with token {token}");
                        ResponseDeliveryFailed?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse));
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Diagnostic.Warning($"Error removing cached search response {responseToken}: {ex.Message}", ex);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        ///     Responds to the given search request, if a response could be resolved and matche(s) were found.
        /// </summary>
        /// <param name="username">The username of the requesting user.</param>
        /// <param name="token">The token for the search request.</param>
        /// <param name="query">The search query.</param>
        /// <returns>The operation context, including a value indicating whether a response was successfully sent.</returns>
        public async Task<bool> TryRespondAsync(string username, int token, string query)
        {
            RequestReceived?.Invoke(this, new SearchRequestEventArgs(username, token, query));

            if (SoulseekClient.Options.SearchResponseResolver == default)
            {
                return false;
            }

            SearchResponse searchResponse = null;

            try
            {
                searchResponse = await SoulseekClient.Options.SearchResponseResolver(username, token, SearchQuery.FromText(query)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error resolving search response for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
                return false;
            }

            if (searchResponse == null || searchResponse.FileCount + searchResponse.LockedFileCount <= 0)
            {
                return false;
            }

            try
            {
                Diagnostic.Debug($"Resolved {searchResponse.FileCount} files for query '{query}' with token {token} from {username}");

                var endpoint = await SoulseekClient.GetUserEndPointAsync(username).ConfigureAwait(false);
                var responseToken = SoulseekClient.GetNextToken();

                IMessageConnection peerConnection = default;

                try
                {
                    // attempt to connect and send the results immediately. either a direct connection succeeds, or a user
                    // responds to a solicited connection request prior to the configured connection timeout.
                    peerConnection = await SoulseekClient.PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, solicitationToken: responseToken, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // direct connection failed, and user did not respond to the solicited connection request before the timeout,
                    // but may respond later. cache the result along with the solicitation token that was sent so we can attempt a
                    // "second chance" delivery of results
                    if (SoulseekClient.Options.SearchResponseCache != default)
                    {
                        try
                        {
                            SoulseekClient.Options.SearchResponseCache.AddOrUpdate(responseToken, (username, token, query, searchResponse));
                            Diagnostic.Debug($"Failed to connect to {username} with solicitation token {responseToken} to deliver search results for query '{query}' with token {token}.  Cached response for potential delayed delivery.");
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Error caching undelivered search response {responseToken} for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
                        }
                    }

                    throw;
                }

                await peerConnection.WriteAsync(searchResponse.ToByteArray()).ConfigureAwait(false);

                Diagnostic.Debug($"Sent response containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
                ResponseDelivered?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse));

                return true;
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to send search response to {username} for query '{query}' with token {token}: {ex.Message}", ex);
            }

            return false;
        }

        /// <summary>
        ///     Sends the pending response matching the specified <paramref name="responseToken"/>, if one exists.
        /// </summary>
        /// <param name="responseToken">The token matching the pending response to send.</param>
        /// <returns>The operation context, including a value indicating whether a response was successfully sent.</returns>
        public async Task<bool> TryRespondAsync(int responseToken)
        {
            if (SoulseekClient.Options.SearchResponseCache != default)
            {
                var cached = false;
                (string Username, int Token, string Query, SearchResponse SearchResponse) record;

                try
                {
                    cached = SoulseekClient.Options.SearchResponseCache.TryRemove(responseToken, out record);
                }
                catch (Exception ex)
                {
                    Diagnostic.Warning($"Error retrieving cached search response {responseToken}: {ex.Message}", ex);
                    return false;
                }

                if (cached)
                {
                    var (username, token, query, searchResponse) = record;

                    try
                    {
                        var peerConnection = await SoulseekClient.PeerConnectionManager.GetCachedMessageConnectionAsync(username).ConfigureAwait(false);
                        await peerConnection.WriteAsync(searchResponse.ToByteArray()).ConfigureAwait(false);

                        Diagnostic.Debug($"Sent cached response {responseToken} containing {searchResponse.FileCount + searchResponse.LockedFileCount} files to {username} for query '{query}' with token {token}");
                        ResponseDelivered?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Diagnostic.Debug($"Failed to send cached search response {responseToken} to {username} for query '{query}' with token {token}: {ex.Message}", ex);
                        ResponseDeliveryFailed?.Invoke(this, new SearchRequestResponseEventArgs(username, token, query, searchResponse));
                    }
                }
            }

            return false;
        }
    }
}