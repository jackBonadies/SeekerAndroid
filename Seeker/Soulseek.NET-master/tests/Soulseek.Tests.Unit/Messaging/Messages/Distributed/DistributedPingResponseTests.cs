// <copyright file="DistributedPingResponseTests.cs" company="JP Dillingham">
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

    public class DistributedPingResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(int token)
        {
            var r = new DistributedPingResponse(token);

            Assert.Equal(token, r.Token);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray Constructs the correct Message"), AutoData]
        public void ToByteArray_Constructs_The_Correct_Message(int token)
        {
            var msg = new DistributedPingResponse(token).ToByteArray();

            var reader = new MessageReader<MessageCode.Distributed>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Distributed.Ping, code);
            Assert.Equal(4 + 1 + 4, msg.Length);

            Assert.Equal(token, reader.ReadInteger());
        }

        [Trait("Category", "FromByteArray")]
        [Theory(DisplayName = "FromByteArray returns the expected data"), AutoData]
        public void FromByteArray_Returns_Expected_Data(int token)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .WriteInteger(token)
                .Build();

            var response = DistributedPingResponse.FromByteArray(msg);

            Assert.Equal(token, response.Token);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray does not throw if message is missing token")]
        public void FromByteArray_Does_Not_Throw_If_Message_Is_Missing_Token()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .Build();

            var response = DistributedPingResponse.FromByteArray(msg);

            Assert.Equal(0, response.Token);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray throws MessageException on code mismatch")]
        public void FromByteArray_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => DistributedPingResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }
    }
}
