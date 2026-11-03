// <copyright file="UserInterestsTests.cs" company="JP Dillingham">
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

    public class UserInterestsTests
    {
        [Trait("Category", "UserInterests")]
        [Theory(DisplayName = "UserInterests instantiates with the given data"), AutoData]
        public void UserInterests_Instantiates_With_The_Given_Data(string username, List<string> likes, List<string> hates)
        {
            var interests = new UserInterests(username, likes, hates);

            Assert.Equal(username, interests.Username);
            Assert.Equal(likes, interests.Likes);
            Assert.Equal(hates, interests.Hates);
        }

        [Trait("Category", "UserInterests")]
        [Theory(DisplayName = "UserInterests instantiates with empty lists given null lists"), AutoData]
        public void UserInterests_Instantiates_With_Empty_Lists_Given_Null_Lists(string username)
        {
            var interests = new UserInterests(username, null, null);

            Assert.Equal(username, interests.Username);
            Assert.Empty(interests.Likes);
            Assert.Empty(interests.Hates);
        }
    }
}
