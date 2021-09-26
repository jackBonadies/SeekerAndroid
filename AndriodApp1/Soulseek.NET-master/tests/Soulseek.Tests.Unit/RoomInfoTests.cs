// <copyright file="RoomInfoTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System.Collections.Generic;
    using AutoFixture.Xunit2;
    using Xunit;

    public class RoomInfoTests
    {
        [Trait("Category", "RoomInfo")]
        [Theory(DisplayName = "RoomInfo instantiates properly"), AutoData]
        public void RoomInfo_Instantiates_Properly(string roomName, List<string> users)
        {
            var info = new RoomInfo(roomName, users);

            Assert.Equal(roomName, info.Name);
            Assert.Equal(users.Count, info.UserCount);
            Assert.Equal(users, info.Users);
        }

        [Trait("Category", "RoomInfo")]
        [Theory(DisplayName = "RoomInfo instantiates properly with count only"), AutoData]
        public void RoomInfo_Instantiates_Properly_With_Count_Only(string roomName, int count)
        {
            var info = new RoomInfo(roomName, count);

            Assert.Equal(roomName, info.Name);
            Assert.Equal(count, info.UserCount);
            Assert.Empty(info.Users);
        }

        [Trait("Category", "RoomInfo")]
        [Theory(DisplayName = "RoomInfo instantiates with null user list if none is given"), AutoData]
        public void RoomInfo_Instantiates_With_Null_User_List_If_Not_Given(string roomName)
        {
            var info = new RoomInfo(roomName, userList: null);

            Assert.Equal(roomName, info.Name);
            Assert.Equal(0, info.UserCount);
            Assert.Empty(info.Users);
        }
    }
}
