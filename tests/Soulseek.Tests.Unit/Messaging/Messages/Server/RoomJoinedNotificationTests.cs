// <copyright file="RoomJoinedNotificationTests.cs" company="JP Dillingham">
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

    public class RoomJoinedNotificationTests
    {
        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.JoinRoom)
                .Build();

            var ex = Record.Exception(() => UserJoinedRoomNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => UserJoinedRoomNotification.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when CountryCode"), AutoData]
        public void Parse_Returns_Expected_Data_When_CountryCode(string roomName, string username, UserData data)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString(roomName)
                .WriteString(username)
                .WriteInteger((int)data.Status)
                .WriteInteger(data.AverageSpeed)
                .WriteLong(data.UploadCount)
                .WriteInteger(data.FileCount)
                .WriteInteger(data.DirectoryCount)
                .WriteInteger(data.SlotsFree.Value)
                .WriteString(data.CountryCode);

            var response = UserJoinedRoomNotification.FromByteArray(builder.Build());

            Assert.Equal(roomName, response.RoomName);
            Assert.Equal(username, response.Username);
            Assert.Equal(data.Status, response.UserData.Status);
            Assert.Equal(data.AverageSpeed, response.UserData.AverageSpeed);
            Assert.Equal(data.UploadCount, response.UserData.UploadCount);
            Assert.Equal(data.FileCount, response.UserData.FileCount);
            Assert.Equal(data.DirectoryCount, response.UserData.DirectoryCount);
            Assert.Equal(data.SlotsFree, response.UserData.SlotsFree);
            Assert.Equal(data.CountryCode, response.UserData.CountryCode);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when no CountryCode"), AutoData]
        public void Parse_Returns_Expected_Data_When_No_CountryCode(string roomName, string username, UserData data)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString(roomName)
                .WriteString(username)
                .WriteInteger((int)data.Status)
                .WriteInteger(data.AverageSpeed)
                .WriteLong(data.UploadCount)
                .WriteInteger(data.FileCount)
                .WriteInteger(data.DirectoryCount)
                .WriteInteger(data.SlotsFree.Value)
                .WriteString(string.Empty);

            var response = UserJoinedRoomNotification.FromByteArray(builder.Build());

            Assert.Equal(roomName, response.RoomName);
            Assert.Equal(username, response.Username);
            Assert.Equal(data.Status, response.UserData.Status);
            Assert.Equal(data.AverageSpeed, response.UserData.AverageSpeed);
            Assert.Equal(data.UploadCount, response.UserData.UploadCount);
            Assert.Equal(data.FileCount, response.UserData.FileCount);
            Assert.Equal(data.DirectoryCount, response.UserData.DirectoryCount);
            Assert.Equal(data.SlotsFree, response.UserData.SlotsFree);
            Assert.Empty(response.UserData.CountryCode);
        }
    }
}
