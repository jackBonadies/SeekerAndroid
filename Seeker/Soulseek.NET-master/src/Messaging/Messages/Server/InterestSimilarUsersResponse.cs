// <copyright file="InterestSimilarUsersResponse.cs" company="JP Dillingham">
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
    ///     The response to a request for the users with an interest in common.
    /// </summary>
    internal sealed class InterestSimilarUsersResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InterestSimilarUsersResponse"/> class.
        /// </summary>
        /// <param name="interest">The interest to which the users apply.</param>
        /// <param name="usernames">The list of the usernames of the users sharing the interest.</param>
        public InterestSimilarUsersResponse(string interest, IEnumerable<string> usernames)
        {
            Interest = interest;
            Usernames = (usernames?.ToList() ?? new List<string>()).AsReadOnly();
        }

        /// <summary>
        ///     Gets the interest to which the users apply.
        /// </summary>
        public string Interest { get; }

        /// <summary>
        ///     Gets the list of the usernames of the users sharing the interest.
        /// </summary>
        public IReadOnlyCollection<string> Usernames { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="InterestSimilarUsersResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static InterestSimilarUsersResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetInterestSimilarUsers)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(InterestSimilarUsersResponse)} (expected: {(int)MessageCode.Server.GetInterestSimilarUsers}, received: {(int)code})");
            }

            var interest = reader.ReadString();

            var userCount = reader.ReadInteger();
            var usernames = new List<string>();

            for (int i = 0; i < userCount; i++)
            {
                usernames.Add(reader.ReadString());
            }

            if (SoulseekClient.ReportUnreadMessageData && reader.HasMoreData)
            {
                Diagnostics.GlobalDiagnostic.Warning($"Message reader for {nameof(MessageCode.Server.GetInterestSimilarUsers)} finalized with {reader.Remaining} unread bytes");
            }

            return new InterestSimilarUsersResponse(interest, usernames);
        }
    }
}