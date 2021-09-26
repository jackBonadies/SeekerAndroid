// <copyright file="DistributedSearchRequestTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class DistributedSearchRequestTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string username, int token, string query)
        {
            var r = new DistributedSearchRequest(username, token, query);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(query, r.Query);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray Constructs the correct Message"), AutoData]
        public void ToByteArray_Constructs_The_Correct_Message(string username, int token, string query)
        {
            var msg = new DistributedSearchRequest(username, token, query).ToByteArray();

            var reader = new MessageReader<MessageCode.Distributed>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Distributed.SearchRequest, code);

            // includes unknown 4 bytes
            Assert.Equal(4 + 1 + 4 + username.Length + 4 + 4 + 4 + query.Length, msg.Length);

            Assert.Equal(0, reader.ReadInteger());
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(query, reader.ReadString());
        }

        [Trait("Category", "FromByteArray")]
        [Theory(DisplayName = "FromByteArray returns the expected data"), AutoData]
        public void FromByteArray_Returns_Expected_Data(string username, int token, string query)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.SearchRequest)
                .WriteInteger(0) // unknown
                .WriteString(username)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var response = DistributedSearchRequest.FromByteArray(msg);

            Assert.Equal(username, response.Username);
            Assert.Equal(token, response.Token);
            Assert.Equal(query, response.Query);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray throws MessageException on code mismatch")]
        public void FromByteArray_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => DistributedSearchRequest.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }
    }
}
