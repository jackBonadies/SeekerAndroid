// <copyright file="RoomListTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class RoomListTests
    {
        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageExcepton on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => RoomListResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => RoomListResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(List<RoomInfo> rooms)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList);

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));

            var response = RoomListResponseFactory.FromByteArray(builder.Build());

            foreach (var room in rooms)
            {
                Assert.Contains(response.Public, r => r.Name == room.Name && r.UserCount == room.UserCount);
            }

            foreach (var room in rooms)
            {
                Assert.Contains(response.Private, r => r.Name == room.Name && r.UserCount == room.UserCount);
            }

            foreach (var room in rooms)
            {
                Assert.Contains(response.Owned, r => r.Name == room.Name && r.UserCount == room.UserCount);
            }

            foreach (var room in rooms)
            {
                Assert.Contains(response.ModeratedRoomNames, r => r == room.Name);
            }
        }
    }
}
