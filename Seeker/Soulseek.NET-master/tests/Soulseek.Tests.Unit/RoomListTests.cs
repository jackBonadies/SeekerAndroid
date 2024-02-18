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

namespace Soulseek.Tests.Unit
{
    using System.Collections.Generic;
    using AutoFixture.Xunit2;
    using Xunit;

    public class RoomListTests
    {
        [Trait("Category", "RoomList")]
        [Theory(DisplayName = "RoomList instantiates properly"), AutoData]
        public void RoomList_Instantiates_Properly(
            List<RoomInfo> pub, List<RoomInfo> priv, List<RoomInfo> owned, List<string> moderated)
        {
            var list = new RoomList(pub, priv, owned, moderated);

            Assert.Equal(pub.Count, list.PublicCount);
            Assert.Equal(pub, list.Public);
            Assert.Equal(priv.Count, list.PrivateCount);
            Assert.Equal(priv, list.Private);
            Assert.Equal(owned.Count, list.OwnedCount);
            Assert.Equal(owned, list.Owned);
            Assert.Equal(moderated.Count, list.ModeratedRoomNameCount);
            Assert.Equal(moderated, list.ModeratedRoomNames);
        }

        [Trait("Category", "RoomList")]
        [Fact(DisplayName = "RoomList instantiates with empty lists if not given")]
        public void RoomList_Instantiates_With_Null_User_List_If_Not_Given()
        {
            var list = new RoomList(null, null, null, null);

            Assert.Empty(list.Public);
            Assert.Equal(0, list.PublicCount);
            Assert.Empty(list.Private);
            Assert.Equal(0, list.PrivateCount);
            Assert.Empty(list.Owned);
            Assert.Equal(0, list.OwnedCount);
            Assert.Empty(list.ModeratedRoomNames);
            Assert.Equal(0, list.ModeratedRoomNameCount);
        }
    }
}
