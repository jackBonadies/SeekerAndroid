// <copyright file="WatchUserResponseTests.cs" company="JP Dillingham">
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

    public class WatchUserResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(string username, bool exists, UserData userData)
        {
            var r = new WatchUserResponse(username, exists, userData);

            Assert.Equal(username, r.Username);
            Assert.Equal(exists, r.Exists);
            Assert.Equal(userData, r.UserData);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => WatchUserResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .Build();

            var ex = Record.Exception(() => WatchUserResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when user exists"), AutoData]
        public void Parse_Returns_Expected_Data_When_User_Exists(string username, UserData userData)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)userData.Status)
                .WriteInteger(userData.AverageSpeed)
                .WriteLong(userData.UploadCount)
                .WriteInteger(userData.FileCount)
                .WriteInteger(userData.DirectoryCount)
                .WriteString(userData.CountryCode)
                .Build();

            var r = WatchUserResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.True(r.Exists);
            Assert.Equal(userData.Status, r.UserData.Status);
            Assert.Equal(userData.AverageSpeed, r.UserData.AverageSpeed);
            Assert.Equal(userData.UploadCount, r.UserData.UploadCount);
            Assert.Equal(userData.FileCount, r.UserData.FileCount);
            Assert.Equal(userData.DirectoryCount, r.UserData.DirectoryCount);
            Assert.Equal(userData.CountryCode, r.UserData.CountryCode);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data when user does not exist"), AutoData]
        public void Parse_Returns_Expected_Data_When_User_Does_Not_Exist(string username)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString(username)
                .WriteByte(0) // exists = false
                .Build();

            var r = WatchUserResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.False(r.Exists);
            Assert.Null(r.UserData);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse does not throw if CountryCode is blank"), AutoData]
        public void Parse_Does_Not_Throw_If_CountryCode_Is_Blank(string username, UserData userData)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)userData.Status)
                .WriteInteger(userData.AverageSpeed)
                .WriteLong(userData.UploadCount)
                .WriteInteger(userData.FileCount)
                .WriteInteger(userData.DirectoryCount)
                .WriteString(string.Empty)
                .Build();

            var r = WatchUserResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.True(r.Exists);
            Assert.Equal(userData.Status, r.UserData.Status);
            Assert.Equal(userData.AverageSpeed, r.UserData.AverageSpeed);
            Assert.Equal(userData.UploadCount, r.UserData.UploadCount);
            Assert.Equal(userData.FileCount, r.UserData.FileCount);
            Assert.Equal(userData.DirectoryCount, r.UserData.DirectoryCount);
            Assert.Empty(r.UserData.CountryCode);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse does not throw if CountryCode is missing"), AutoData]
        public void Parse_Does_Not_Throw_If_CountryCode_Is_Missing(string username, UserData userData)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)userData.Status)
                .WriteInteger(userData.AverageSpeed)
                .WriteLong(userData.UploadCount)
                .WriteInteger(userData.FileCount)
                .WriteInteger(userData.DirectoryCount)
                .Build();

            var r = WatchUserResponse.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.True(r.Exists);
            Assert.Equal(userData.Status, r.UserData.Status);
            Assert.Equal(userData.AverageSpeed, r.UserData.AverageSpeed);
            Assert.Equal(userData.UploadCount, r.UserData.UploadCount);
            Assert.Equal(userData.FileCount, r.UserData.FileCount);
            Assert.Equal(userData.DirectoryCount, r.UserData.DirectoryCount);
            Assert.Null(r.UserData.CountryCode);
        }
    }
}
