// <copyright file="PeerSearchRequest.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    /// <summary>
    ///     Requests a search from a peer.
    /// </summary>
    internal sealed class PeerSearchRequest : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerSearchRequest"/> class.
        /// </summary>
        /// <param name="token">The unique token for the request.</param>
        /// <param name="query">The search query.</param>
        public PeerSearchRequest(int token, string query)
        {
            Token = token;
            Query = query;
        }

        /// <summary>
        ///     Gets the search query.
        /// </summary>
        public string Query { get; }

        /// <summary>
        ///     Gets the unique token for the search.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="ServerSearchRequest"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static PeerSearchRequest FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.SearchRequest)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PeerSearchRequest)} (expected: {(int)MessageCode.Peer.SearchRequest}, received: {(int)code})");
            }

            var token = reader.ReadInteger();
            var query = reader.ReadString();

            return new PeerSearchRequest(token, query);
        }
    }
}