// <copyright file="RoomDataTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Xunit;

    public class RoomDataTests
    {
        [Trait("Category", "RoomData")]
        [Theory(DisplayName = "RoomData uses empty list if one is omitted"), AutoData]
        public void RoomData_Users_Uses_Empty_List_If_One_Is_Omitted(string roomName)
        {
            var data = new RoomData(roomName, null, false, null, null);

            Assert.NotNull(data.Users);
        }

        [Trait("Category", "RoomData")]
        [Theory(DisplayName = "RoomData uses null list if one is omitted"), AutoData]
        public void RoomData_Operators_Uses_Null_List_If_One_Is_Omitted(string roomName)
        {
            var data = new RoomData(roomName, null, false, null, null);

            Assert.Null(data.Operators);
            Assert.Null(data.OperatorCount);
        }
    }
}
