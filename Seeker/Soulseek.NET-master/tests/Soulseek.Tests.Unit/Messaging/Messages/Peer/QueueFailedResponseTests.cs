// <copyright file="QueueFailedResponseTests.cs" company="JP Dillingham">
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

    public class QueueFailedResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with the given data")]
        public void Instantiates_With_The_Given_Data()
        {
            var file = Guid.NewGuid().ToString();
            var reason = Guid.NewGuid().ToString();

            QueueFailedResponse response = null;

            var ex = Record.Exception(() => response = new QueueFailedResponse(file, reason));

            Assert.Null(ex);

            Assert.Equal(file, response.Filename);
            Assert.Equal(reason, response.Message);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => QueueFailedResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.QueueFailed)
                .Build();

            var ex = Record.Exception(() => QueueFailedResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data")]
        public void Parse_Returns_Expected_Data()
        {
            var file = Guid.NewGuid().ToString();
            var reason = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.QueueFailed)
                .WriteString(file)
                .WriteString(reason)
                .Build();

            var response = QueueFailedResponse.FromByteArray(msg);

            Assert.Equal(file, response.Filename);
            Assert.Equal(reason, response.Message);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns expected data"), AutoData]
        public void ToByteArray_Returns_Expected_Data(string filename, string message)
        {
            var m = new QueueFailedResponse(filename, message).ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(m);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.QueueFailed, code);
            Assert.Equal(4 + 4 + 4 + filename.Length + 4 + message.Length, m.Length);
            Assert.Equal(filename, reader.ReadString());
            Assert.Equal(message, reader.ReadString());
        }
    }
}
