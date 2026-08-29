// <copyright file="ServerInfoTests.cs" company="JP Dillingham">
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

    public class ServerInfoTests
    {
        [Trait("Category", "ServerInfo")]
        [Theory(DisplayName = "Instantiates with given values"), AutoData]
        public void ServerInfo_Initializes_With_Nulls(int parentMinSpeed, int parentSpeedRatio, int wishlistInterval, bool isSupporter)
        {
            var info = new ServerInfo(parentMinSpeed, parentSpeedRatio, wishlistInterval, isSupporter);

            Assert.Equal(parentMinSpeed, info.ParentMinSpeed);
            Assert.Equal(parentSpeedRatio, info.ParentSpeedRatio);
            Assert.Equal(wishlistInterval, info.WishlistInterval);
            Assert.Equal(isSupporter, info.IsSupporter);
        }

        [Trait("Category", "ServerInfo")]
        [Theory(DisplayName = "With overlays substitutions"), AutoData]
        public void ServerInfo_With_Overlays(int parentMinSpeed, int parentSpeedRatio, int wishlistInterval, bool isSupporter)
        {
            var info = new ServerInfo();

            Assert.Null(info.ParentMinSpeed);
            Assert.Null(info.ParentSpeedRatio);
            Assert.Null(info.WishlistInterval);
            Assert.Null(info.IsSupporter);

            info = info.With(parentMinSpeed: parentMinSpeed, parentSpeedRatio: parentSpeedRatio, wishlistInterval: wishlistInterval, isSupporter: isSupporter);

            Assert.Equal(parentMinSpeed, info.ParentMinSpeed);
            Assert.Equal(parentSpeedRatio, info.ParentSpeedRatio);
            Assert.Equal(wishlistInterval, info.WishlistInterval);
            Assert.Equal(isSupporter, info.IsSupporter);
        }

        [Trait("Category", "ServerInfo")]
        [Theory(DisplayName = "With does not overlay nulls"), AutoData]
        public void ServerInfo_With_Does_Not_Overlay_Nulls(int parentMinSpeed, int parentSpeedRatio, int wishlistInterval, bool isSupporter)
        {
            var info = new ServerInfo(parentMinSpeed, parentSpeedRatio, wishlistInterval, isSupporter);

            info = info.With(parentMinSpeed: null, parentSpeedRatio: null, wishlistInterval: null, isSupporter: null);

            Assert.Equal(parentMinSpeed, info.ParentMinSpeed);
            Assert.Equal(parentSpeedRatio, info.ParentSpeedRatio);
            Assert.Equal(wishlistInterval, info.WishlistInterval);
            Assert.Equal(isSupporter, info.IsSupporter);
        }
    }
}
