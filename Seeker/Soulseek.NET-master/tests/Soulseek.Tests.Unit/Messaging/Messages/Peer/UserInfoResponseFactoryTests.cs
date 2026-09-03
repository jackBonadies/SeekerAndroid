// <copyright file="UserInfoResponseFactoryTests.cs" company="JP Dillingham">
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
    using Xunit;

    public class UserInfoResponseFactoryTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            UserInfo response = null;

            var ex = Record.Exception(() => response = new UserInfo(description, uploadSlots, queueLength, hasFreeSlot, picture));

            Assert.Null(ex);

            Assert.Equal(description, response.Description);
            Assert.True(response.HasPicture);
            Assert.Equal(picture, response.Picture);
            Assert.Equal(uploadSlots, response.UploadSlots);
            Assert.Equal(queueLength, response.QueueLength);
            Assert.Equal(hasFreeSlot, response.HasFreeUploadSlot);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => UserInfoResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString("foo")
                .Build();

            var ex = Record.Exception(() => UserInfoResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data with picture"), AutoData]
        public void Parse_Returns_Expected_Data_With_Picture(string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(description)
                .WriteByte(1)
                .WriteInteger(picture.Length)
                .WriteBytes(picture)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte((byte)(hasFreeSlot ? 1 : 0))
                .Build();

            var response = UserInfoResponseFactory.FromByteArray(msg);

            Assert.Equal(description, response.Description);
            Assert.True(response.HasPicture);
            Assert.Equal(picture, response.Picture);
            Assert.Equal(uploadSlots, response.UploadSlots);
            Assert.Equal(queueLength, response.QueueLength);
            Assert.Equal(hasFreeSlot, response.HasFreeUploadSlot);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data without picture"), AutoData]
        public void Parse_Returns_Expected_Data_Without_Picture(string description, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(description)
                .WriteByte(0)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte((byte)(hasFreeSlot ? 1 : 0))
                .Build();

            var response = UserInfoResponseFactory.FromByteArray(msg);

            Assert.Equal(description, response.Description);
            Assert.False(response.HasPicture);
            Assert.Equal(uploadSlots, response.UploadSlots);
            Assert.Equal(queueLength, response.QueueLength);
            Assert.Equal(hasFreeSlot, response.HasFreeUploadSlot);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns expected data with picture"), AutoData]
        public void ToByteArray_Returns_Expected_Data_With_Picture(string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var r = new UserInfo(description, uploadSlots, queueLength, hasFreeSlot, picture).ToByteArray();

            var m = new MessageReader<MessageCode.Peer>(r);
            var code = m.ReadCode();

            Assert.Equal(MessageCode.Peer.InfoResponse, code);
            Assert.Equal(description, m.ReadString());
            Assert.True(m.ReadByte() == 1);
            Assert.Equal(picture.Length, m.ReadInteger());
            Assert.Equal(picture, m.ReadBytes(picture.Length));
            Assert.Equal(uploadSlots, m.ReadInteger());
            Assert.Equal(queueLength, m.ReadInteger());
            Assert.Equal(hasFreeSlot, m.ReadByte() == 1);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns expected data with no picture"), AutoData]
        public void ToByteArray_Returns_Expected_Data_With_No_Picture(string description, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var r = new UserInfo(description, uploadSlots, queueLength, hasFreeSlot).ToByteArray();

            var m = new MessageReader<MessageCode.Peer>(r);
            var code = m.ReadCode();

            Assert.Equal(MessageCode.Peer.InfoResponse, code);
            Assert.Equal(description, m.ReadString());
            Assert.False(m.ReadByte() == 1); // no picture
            Assert.Equal(uploadSlots, m.ReadInteger());
            Assert.Equal(queueLength, m.ReadInteger());
            Assert.Equal(hasFreeSlot, m.ReadByte() == 1);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns expected data with no free slot"), AutoData]
        public void ToByteArray_Returns_Expected_Data_With_No_Free_Slot(string description, int uploadSlots, int queueLength)
        {
            var r = new UserInfo(description, uploadSlots, queueLength, false).ToByteArray();

            var m = new MessageReader<MessageCode.Peer>(r);
            var code = m.ReadCode();

            Assert.Equal(MessageCode.Peer.InfoResponse, code);
            Assert.Equal(description, m.ReadString());
            Assert.False(m.ReadByte() == 1); // no picture
            Assert.Equal(uploadSlots, m.ReadInteger());
            Assert.Equal(queueLength, m.ReadInteger());
            Assert.False(m.ReadByte() == 1);
        }
    }
}
