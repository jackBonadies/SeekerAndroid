// <copyright file="PrivateRoomRemoveUserTests.cs" company="JP Dillingham">
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

    public class PrivateRoomRemoveUserTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string roomName, string username)
        {
            PrivateRoomRemoveUser response = null;

            var ex = Record.Exception(() => response = new PrivateRoomRemoveUser(roomName, username));

            Assert.Null(ex);

            Assert.Equal(roomName, response.RoomName);
            Assert.Equal(username, response.Username);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => PrivateRoomRemoveUser.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveUser)
                .Build();

            var ex = Record.Exception(() => PrivateRoomRemoveUser.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data on success"), AutoData]
        public void Parse_Returns_Expected_Data_On_Success(string roomName, string username)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveUser)
                .WriteString(roomName)
                .WriteString(username)
                .Build();

            var response = PrivateRoomRemoveUser.FromByteArray(msg);

            Assert.Equal(roomName, response.RoomName);
            Assert.Equal(username, response.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "Constructs the correct message"), AutoData]
        public void Constructs_The_Correct_Message(string roomName, string username)
        {
            var a = new PrivateRoomRemoveUser(roomName, username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.PrivateRoomRemoveUser, code);
            Assert.Equal(roomName, reader.ReadString());
            Assert.Equal(username, reader.ReadString());
        }
    }
}
