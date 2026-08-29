// <copyright file="RoomSearchRequest.cs" company="JP Dillingham">
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
    ///     Requests a search from all members of a room.
    /// </summary>
    internal sealed class RoomSearchRequest : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomSearchRequest"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room to search.</param>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique token for the search.</param>
        public RoomSearchRequest(string roomName, string searchText, int token)
        {
            RoomName = roomName;
            SearchText = searchText;
            Token = token;
        }

        /// <summary>
        ///     Gets the name of the room to search.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the text for which to search.
        /// </summary>
        public string SearchText { get; }

        /// <summary>
        ///     Gets the unique token for the search.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomSearch)
                .WriteString(RoomName)
                .WriteInteger(Token)
                .WriteString(SearchText)
                .Build();
        }
    }
}