// <copyright file="UserInterestsResponseTests.cs" company="JP Dillingham">
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

    public class UserInterestsResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string username, List<string> likes, List<string> hates)
        {
            UserInterestsResponse response = null;

            var ex = Record.Exception(() => response = new UserInterestsResponse(username, likes, hates));

            Assert.Null(ex);

            Assert.Equal(username, response.Username);
            Assert.Equal(likes, response.Likes);
            Assert.Equal(hates, response.Hates);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with empty lists given null lists"), AutoData]
        public void Instantiates_With_Empty_Lists_Given_Null_Lists(string username)
        {
            var response = new UserInterestsResponse(username, null, null);

            Assert.Empty(response.Likes);
            Assert.Empty(response.Hates);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => UserInterestsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserInterests)
                .WriteString("foo")
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => UserInterestsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string username, List<string> likes, List<string> hates)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserInterests)
                .WriteString(username)
                .WriteInteger(likes.Count);

            likes.ForEach(like => builder.WriteString(like));

            builder.WriteInteger(hates.Count);

            hates.ForEach(hate => builder.WriteString(hate));

            var response = UserInterestsResponse.FromByteArray(builder.Build());

            Assert.Equal(username, response.Username);
            Assert.Equal(likes, response.Likes);
            Assert.Equal(hates, response.Hates);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns empty lists given no interests"), AutoData]
        public void Parse_Returns_Empty_Lists_Given_No_Interests(string username)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserInterests)
                .WriteString(username)
                .WriteInteger(0)
                .WriteInteger(0);

            var response = UserInterestsResponse.FromByteArray(builder.Build());

            Assert.Equal(username, response.Username);
            Assert.Empty(response.Likes);
            Assert.Empty(response.Hates);
        }
    }
}
