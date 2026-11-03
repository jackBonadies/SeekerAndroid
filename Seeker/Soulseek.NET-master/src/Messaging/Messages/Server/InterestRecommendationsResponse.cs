// <copyright file="InterestRecommendationsResponse.cs" company="JP Dillingham">
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
    ///     The response to a request for the recommendations related to an interest.
    /// </summary>
    internal sealed class InterestRecommendationsResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InterestRecommendationsResponse"/> class.
        /// </summary>
        /// <param name="interest">The interest to which the recommendations apply.</param>
        /// <param name="recommendations">The list of recommendations, and the number of recommendations for each.</param>
        public InterestRecommendationsResponse(string interest, IEnumerable<(string Recommendation, int Count)> recommendations)
        {
            Interest = interest;

            Recommendations = (recommendations?.ToList() ?? new List<(string Recommendation, int Count)>()).AsReadOnly();
        }

        /// <summary>
        ///     Gets the interest to which the recommendations apply.
        /// </summary>
        public string Interest { get; }

        /// <summary>
        ///     Gets the list of recommendations, and the number of recommendations for each.
        /// </summary>
        public IReadOnlyCollection<(string Recommendation, int Count)> Recommendations { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="InterestRecommendationsResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static InterestRecommendationsResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetInterestRecommendations)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(InterestRecommendationsResponse)} (expected: {(int)MessageCode.Server.GetInterestRecommendations}, received: {(int)code})");
            }

            var interest = reader.ReadString();

            var recommendationCount = reader.ReadInteger();
            var recommendations = new List<(string Recommendation, int Count)>();

            for (int i = 0; i < recommendationCount; i++)
            {
                var recommendation = reader.ReadString();
                var count = reader.ReadInteger();

                recommendations.Add((recommendation, count));
            }

            if (SoulseekClient.ReportUnreadMessageData && reader.HasMoreData)
            {
                Diagnostics.GlobalDiagnostic.Warning($"Message reader for {nameof(MessageCode.Server.GetInterestRecommendations)} finalized with {reader.Remaining} unread bytes");
            }

            return new InterestRecommendationsResponse(interest, recommendations);
        }
    }
}