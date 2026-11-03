// <copyright file="PersonalRecommendationsResponse.cs" company="JP Dillingham">
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
    ///     The response to a request for recommendations based on the currently logged in user's interests.
    /// </summary>
    internal sealed class PersonalRecommendationsResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PersonalRecommendationsResponse"/> class.
        /// </summary>
        /// <param name="recommendations">The list of recommendations, and the number of recommendations for each.</param>
        /// <param name="unrecommendations">The list of unrecommendations, and the number of unrecommendations for each.</param>
        public PersonalRecommendationsResponse(
            IEnumerable<(string Recommendation, int Count)> recommendations,
            IEnumerable<(string Unrecommendation, int Count)> unrecommendations)
        {
            Recommendations = (recommendations?.ToList() ?? new List<(string Recommendation, int Count)>()).AsReadOnly();
            Unrecommendations = (unrecommendations?.ToList() ?? new List<(string Unrecommendation, int Count)>()).AsReadOnly();
        }

        /// <summary>
        ///     Gets the list of recommendations, and the number of recommendations for each.
        /// </summary>
        public IReadOnlyCollection<(string Recommendation, int Count)> Recommendations { get; }

        /// <summary>
        ///     Gets the list of unrecommendations, and the number of unrecommendations for each.
        /// </summary>
        public IReadOnlyCollection<(string Unrecommendation, int Count)> Unrecommendations { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PersonalRecommendationsResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static PersonalRecommendationsResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetPersonalRecommendations)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PersonalRecommendationsResponse)} (expected: {(int)MessageCode.Server.GetPersonalRecommendations}, received: {(int)code})");
            }

            var recommendationCount = reader.ReadInteger();
            var recommendations = new List<(string Recommendation, int Count)>();

            for (int i = 0; i < recommendationCount; i++)
            {
                var recommendation = reader.ReadString();
                var count = reader.ReadInteger();

                recommendations.Add((recommendation, count));
            }

            var unrecommendationCount = reader.ReadInteger();
            var unrecommendations = new List<(string Unrecommendation, int Count)>();

            for (int i = 0; i < unrecommendationCount; i++)
            {
                var unrecommendation = reader.ReadString();
                var count = reader.ReadInteger();

                unrecommendations.Add((unrecommendation, count));
            }

            if (SoulseekClient.ReportUnreadMessageData && reader.HasMoreData)
            {
                Diagnostics.GlobalDiagnostic.Warning($"Message reader for {nameof(MessageCode.Server.GetPersonalRecommendations)} finalized with {reader.Remaining} unread bytes");
            }

            return new PersonalRecommendationsResponse(recommendations, unrecommendations);
        }
    }
}