// <copyright file="CannotConnectTests.cs" company="JP Dillingham">
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

    public class CannotConnectTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates correctly given token and username"), AutoData]
        public void Instantiates_Correctly_Given_Token_And_Username(int token, string username)
        {
            var msg = new CannotConnect(token, username);

            Assert.Equal(token, msg.Token);
            Assert.Equal(username, msg.Username);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates correctly given token and username"), AutoData]
        public void Instantiates_Correctly_Given_Token_Only(int token)
        {
            var msg = new CannotConnect(token);

            Assert.Equal(token, msg.Token);
            Assert.Null(msg.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray Constructs the correct Message given token and username"), AutoData]
        public void ToByteArray_Constructs_The_Correct_Message_Given_Token_And_Username(int token, string username)
        {
            var msg = new CannotConnect(token, username).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.CannotConnect, code);
            Assert.Equal(8 + 4 + 4 + username.Length, msg.Length);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray Constructs the correct Message given token only"), AutoData]
        public void ToByteArray_Constructs_The_Correct_Message_Given_Token_Only(int token)
        {
            var msg = new CannotConnect(token).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.CannotConnect, code);
            Assert.Equal(8 + 4, msg.Length);
            Assert.Equal(token, reader.ReadInteger());
            Assert.False(reader.HasMoreData);
        }

        [Trait("Category", "FromByteArray")]
        [Theory(DisplayName = "FromByteArray returns the expected data"), AutoData]
        public void FromByteArray_Returns_Expected_Data(int token, string username)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(token)
                .WriteString(username)
                .Build();

            var m = CannotConnect.FromByteArray(msg);

            Assert.Equal(token, m.Token);
            Assert.Equal(username, m.Username);
        }

        [Trait("Category", "FromByteArray")]
        [Theory(DisplayName = "FromByteArray returns the expected data given message with only token"), AutoData]
        public void FromByteArray_Returns_Expected_Data_Given_Message_With_Only_Token(int token)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(token)
                .Build();

            var m = CannotConnect.FromByteArray(msg);

            Assert.Equal(token, m.Token);
            Assert.Null(m.Username);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray throws MessageException on code mismatch")]
        public void FromByteArray_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.BranchLevel)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => CannotConnect.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }
    }
}
