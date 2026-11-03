// <copyright file="PersonalRecommendationsResponseTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System.Collections.Generic;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class PersonalRecommendationsResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(List<(string Recommendation, int Count)> recommendations, List<(string Unrecommendation, int Count)> unrecommendations)
        {
            PersonalRecommendationsResponse response = null;

            var ex = Record.Exception(() => response = new PersonalRecommendationsResponse(recommendations, unrecommendations));

            Assert.Null(ex);

            Assert.Equal(recommendations, response.Recommendations);
            Assert.Equal(unrecommendations, response.Unrecommendations);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with empty lists given null lists")]
        public void Instantiates_With_Empty_Lists_Given_Null_Lists()
        {
            var response = new PersonalRecommendationsResponse(null, null);

            Assert.Empty(response.Recommendations);
            Assert.Empty(response.Unrecommendations);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => PersonalRecommendationsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalRecommendations)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => PersonalRecommendationsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(List<(string Recommendation, int Count)> recommendations, List<(string Unrecommendation, int Count)> unrecommendations)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalRecommendations)
                .WriteInteger(recommendations.Count);

            recommendations.ForEach(recommendation =>
            {
                builder
                    .WriteString(recommendation.Recommendation)
                    .WriteInteger(recommendation.Count);
            });

            builder.WriteInteger(unrecommendations.Count);

            unrecommendations.ForEach(unrecommendation =>
            {
                builder
                    .WriteString(unrecommendation.Unrecommendation)
                    .WriteInteger(unrecommendation.Count);
            });

            var response = PersonalRecommendationsResponse.FromByteArray(builder.Build());

            Assert.Equal(recommendations, response.Recommendations);
            Assert.Equal(unrecommendations, response.Unrecommendations);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data given negative counts"), AutoData]
        public void Parse_Returns_Expected_Data_Given_Negative_Counts(string unrecommendation, int count)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalRecommendations)
                .WriteInteger(0)
                .WriteInteger(1)
                .WriteString(unrecommendation)
                .WriteInteger(-count)
                .Build();

            var response = PersonalRecommendationsResponse.FromByteArray(msg);

            Assert.Single(response.Unrecommendations);
            Assert.Contains(response.Unrecommendations, u => u.Unrecommendation == unrecommendation && u.Count == -count);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns empty lists given no recommendations")]
        public void Parse_Returns_Empty_Lists_Given_No_Recommendations()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalRecommendations)
                .WriteInteger(0)
                .WriteInteger(0)
                .Build();

            var response = PersonalRecommendationsResponse.FromByteArray(msg);

            Assert.Empty(response.Recommendations);
            Assert.Empty(response.Unrecommendations);
        }
    }
}
