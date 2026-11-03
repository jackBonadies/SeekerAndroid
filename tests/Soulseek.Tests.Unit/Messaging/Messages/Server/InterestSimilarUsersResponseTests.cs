// <copyright file="InterestSimilarUsersResponseTests.cs" company="JP Dillingham">
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

    public class InterestSimilarUsersResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string interest, List<string> usernames)
        {
            InterestSimilarUsersResponse response = null;

            var ex = Record.Exception(() => response = new InterestSimilarUsersResponse(interest, usernames));

            Assert.Null(ex);

            Assert.Equal(interest, response.Interest);
            Assert.Equal(usernames, response.Usernames);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with empty list given null list"), AutoData]
        public void Instantiates_With_Empty_List_Given_Null_List(string interest)
        {
            var response = new InterestSimilarUsersResponse(interest, null);

            Assert.Empty(response.Usernames);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => InterestSimilarUsersResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestSimilarUsers)
                .WriteString("foo")
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => InterestSimilarUsersResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string interest, List<string> usernames)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestSimilarUsers)
                .WriteString(interest)
                .WriteInteger(usernames.Count);

            usernames.ForEach(username => builder.WriteString(username));

            var response = InterestSimilarUsersResponse.FromByteArray(builder.Build());

            Assert.Equal(interest, response.Interest);
            Assert.Equal(usernames, response.Usernames);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns empty list given no users"), AutoData]
        public void Parse_Returns_Empty_List_Given_No_Users(string interest)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestSimilarUsers)
                .WriteString(interest)
                .WriteInteger(0)
                .Build();

            var response = InterestSimilarUsersResponse.FromByteArray(msg);

            Assert.Equal(interest, response.Interest);
            Assert.Empty(response.Usernames);
        }
    }
}
