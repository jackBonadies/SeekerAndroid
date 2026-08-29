// <copyright file="LoginResponseTests.cs" company="JP Dillingham">
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
    using System.Net;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class LoginResponseTests
    {
        private static string RandomGuid { get; } = Guid.NewGuid().ToString();
        private Random Random { get; } = new Random();

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with the given data")]
        public void Instantiates_With_The_Given_Data()
        {
            var success = Random.Next() % 2 == 1;
            var msg = RandomGuid;
            var ip = new IPAddress(Random.Next(1024));

            LoginResponse response = null;

            var ex = Record.Exception(() => response = new LoginResponse(success, msg, ip));

            Assert.Null(ex);

            Assert.Equal(success, response.Succeeded);
            Assert.Equal(msg, response.Message);
            Assert.Equal(ip, response.IPAddress);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => LoginResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .Build();

            var ex = Record.Exception(() => LoginResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data on failure")]
        public void Parse_Returns_Expected_Data_On_Failure()
        {
            var str = RandomGuid;

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteByte(0)
                .WriteString(str)
                .Build();

            var response = LoginResponse.FromByteArray(msg);

            Assert.False(response.Succeeded);
            Assert.Equal(str, response.Message);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse returns expected data on success")]
        public void Parse_Returns_Expected_Data_On_Success()
        {
            var ip = new IPAddress(Random.Next(1024));
            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteByte(1)
                .WriteString(string.Empty)
                .WriteBytes(ipBytes)
                .WriteString("foo")
                .WriteByte(1)
                .Build();

            var response = LoginResponse.FromByteArray(msg);

            Assert.True(response.Succeeded);
            Assert.Equal(string.Empty, response.Message);
            Assert.Equal(ip, response.IPAddress);
            Assert.Equal("foo", response.Hash);
            Assert.True(response.IsSupporter);
        }
    }
}
