// <copyright file="UserInterestsResponse.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Messaging.Messages
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     The response to a request for a user's interests.
    /// </summary>
    internal sealed class UserInterestsResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInterestsResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="likes">The list of things the user likes.</param>
        /// <param name="hates">The list of things the user hates.</param>
        public UserInterestsResponse(string username, IEnumerable<string> likes, IEnumerable<string> hates)
        {
            Username = username;

            Likes = (likes?.ToList() ?? new List<string>()).AsReadOnly();
            Hates = (hates?.ToList() ?? new List<string>()).AsReadOnly();
        }

        /// <summary>
        ///     Gets the list of things the user hates.
        /// </summary>
        public IReadOnlyCollection<string> Hates { get; }

        /// <summary>
        ///     Gets the list of things the user likes.
        /// </summary>
        public IReadOnlyCollection<string> Likes { get; }

        /// <summary>
        ///     Gets the username of the user.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UserInterestsResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserInterestsResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetUserInterests)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserInterestsResponse)} (expected: {(int)MessageCode.Server.GetUserInterests}, received: {(int)code})");
            }

            var username = reader.ReadString();

            var likeCount = reader.ReadInteger();
            var likes = new List<string>();

            for (int i = 0; i < likeCount; i++)
            {
                likes.Add(reader.ReadString());
            }

            var hateCount = reader.ReadInteger();
            var hates = new List<string>();

            for (int i = 0; i < hateCount; i++)
            {
                hates.Add(reader.ReadString());
            }

            if (SoulseekClient.ReportUnreadMessageData && reader.HasMoreData)
            {
                Diagnostics.GlobalDiagnostic.Warning($"Message reader for {nameof(MessageCode.Server.GetUserInterests)} finalized with {reader.Remaining} unread bytes");
            }

            return new UserInterestsResponse(username, likes, hates);
        }
    }
}