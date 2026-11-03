// <copyright file="RecommendationTests.cs" company="JP Dillingham">
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

    public class RecommendationTests
    {
        [Trait("Category", "Recommendation")]
        [Theory(DisplayName = "Recommendation instantiates with the given data"), AutoData]
        public void Recommendation_Instantiates_With_The_Given_Data(string interest, int count)
        {
            var recommendation = new Recommendation(interest, count);

            Assert.Equal(interest, recommendation.Interest);
            Assert.Equal(count, recommendation.Count);
        }
    }
}
