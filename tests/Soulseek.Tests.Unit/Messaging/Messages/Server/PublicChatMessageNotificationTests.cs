// <copyright file="PublicChatMessageNotificationTests.cs" company="JP Dillingham">
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

    public class PublicChatMessageNotificationTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string roomName, string username, string message)
        {
            PublicChatMessageNotification response = null;

            var ex = Record.Exception(() => response = new PublicChatMessageNotification(roomName, username, message));

            Assert.Null(ex);

            Assert.Equal(roomName, response.RoomName);
            Assert.Equal(username, response.Username);
            Assert.Equal(message, response.Message);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => PublicChatMessageNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PublicChat)
                .Build();

            var ex = Record.Exception(() => PublicChatMessageNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string roomName, string username, string message)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PublicChat)
                .WriteString(roomName)
                .WriteString(username)
                .WriteString(message)
                .Build();

            var response = PublicChatMessageNotification.FromByteArray(msg);

            Assert.Equal(roomName, response.RoomName);
            Assert.Equal(username, response.Username);
            Assert.Equal(message, response.Message);
        }
    }
}
