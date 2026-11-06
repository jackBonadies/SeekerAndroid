// <copyright file="PersonalSimilarUsersResponse.cs" company="JP Dillingham">
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
    ///     The response to a request for the users with interests similar to those of the currently logged in user.
    /// </summary>
    internal sealed class PersonalSimilarUsersResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PersonalSimilarUsersResponse"/> class.
        /// </summary>
        /// <param name="users">The list of similar users, and the similarity rating for each.</param>
        public PersonalSimilarUsersResponse(IEnumerable<(string Username, int Rating)> users)
        {
            Users = (users?.ToList() ?? new List<(string Username, int Rating)>()).AsReadOnly();
        }

        /// <summary>
        ///     Gets the list of similar users, and the similarity rating for each.
        /// </summary>
        public IReadOnlyCollection<(string Username, int Rating)> Users { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PersonalSimilarUsersResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static PersonalSimilarUsersResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetPersonalSimilarUsers)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PersonalSimilarUsersResponse)} (expected: {(int)MessageCode.Server.GetPersonalSimilarUsers}, received: {(int)code})");
            }

            var userCount = reader.ReadInteger();
            var users = new List<(string Username, int Rating)>();

            for (int i = 0; i < userCount; i++)
            {
                var username = reader.ReadString();
                var rating = reader.ReadInteger();

                users.Add((username, rating));
            }

            if (SoulseekClient.ReportUnreadMessageData && reader.HasMoreData)
            {
                Diagnostics.GlobalDiagnostic.Warning($"Message reader for {nameof(MessageCode.Server.GetPersonalSimilarUsers)} finalized with {reader.Remaining} unread bytes");
            }

            return new PersonalSimilarUsersResponse(users);
        }
    }
}