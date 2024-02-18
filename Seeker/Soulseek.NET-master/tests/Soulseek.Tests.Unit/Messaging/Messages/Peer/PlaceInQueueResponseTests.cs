// <copyright file="PlaceInQueueResponseTests.cs" company="JP Dillingham">
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

    public class PlaceInQueueResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string filename, int placeInQueue)
        {
            var a = new PlaceInQueueResponse(filename, placeInQueue);

            Assert.Equal(filename, a.Filename);
            Assert.Equal(placeInQueue, a.PlaceInQueue);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => PlaceInQueueResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .Build();

            var ex = Record.Exception(() => PlaceInQueueResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string filename, int placeInQueue)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString(filename)
                .WriteInteger(placeInQueue)
                .Build();

            var response = PlaceInQueueResponse.FromByteArray(msg);

            Assert.Equal(filename, response.Filename);
            Assert.Equal(placeInQueue, response.PlaceInQueue);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray constructs the correct message"), AutoData]
        public void ToByteArray_Constructs_The_Correct_Message(string filename, int placeInQueue)
        {
            var res = new PlaceInQueueResponse(filename, placeInQueue).ToByteArray();
            var reader = new MessageReader<MessageCode.Peer>(res);

            Assert.Equal(MessageCode.Peer.PlaceInQueueResponse, reader.ReadCode());
            Assert.Equal(filename, reader.ReadString());
            Assert.Equal(placeInQueue, reader.ReadInteger());
        }
    }
}
