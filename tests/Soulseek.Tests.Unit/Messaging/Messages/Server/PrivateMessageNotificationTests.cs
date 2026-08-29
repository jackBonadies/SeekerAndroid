// <copyright file="PrivateMessageNotificationTests.cs" company="JP Dillingham">
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
    using System;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class PrivateMessageNotificationTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(int id, DateTime timestamp, string username, string message, bool replayed)
        {
            PrivateMessageNotification response = null;

            var ex = Record.Exception(() => response = new PrivateMessageNotification(id, timestamp, username, message, replayed));

            Assert.Null(ex);

            Assert.Equal(id, response.Id);
            Assert.Equal(timestamp, response.Timestamp);
            Assert.Equal(username, response.Username);
            Assert.Equal(message, response.Message);
            Assert.Equal(replayed, response.Replayed);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => PrivateMessageNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .Build();

            var ex = Record.Exception(() => PrivateMessageNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(int id, int timeOffset, string username, string message)
        {
            var epoch = DateTime.UnixEpoch;
            var timestamp = epoch.AddSeconds(timeOffset);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(id)
                .WriteInteger(timeOffset)
                .WriteString(username)
                .WriteString(message)
                .WriteByte((byte)0)
                .Build();

            var response = PrivateMessageNotification.FromByteArray(msg);

            Assert.Equal(id, response.Id);
            Assert.Equal(timestamp, response.Timestamp);
            Assert.Equal(username, response.Username);
            Assert.Equal(message, response.Message);
        }
    }
}
