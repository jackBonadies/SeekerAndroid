// <copyright file="TokenFactoryTests.cs" company="JP Dillingham">
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

    public class TokenFactoryTests
    {
        [Trait("Category", "Initialization")]
        [Theory(DisplayName = "Initializes with given start"), AutoData]
        public void Initializes_With_Given_Start(int start)
        {
            var t = new TokenFactory(start);

            var current = t.GetField<int>("current");

            Assert.Equal(start, current);
        }

        [Trait("Category", "Initialization")]
        [Theory(DisplayName = "First token is start"), AutoData]
        public void First_Token_Is_Start(int start)
        {
            var t = new TokenFactory(start);

            Assert.Equal(start, t.NextToken());
        }

        [Trait("Category", "GetToken")]
        [Theory(DisplayName = "Returns sequential tokens"), AutoData]
        public void Returns_Sequential_Tokens(int start)
        {
            var t = new TokenFactory(start);

            var t1 = t.NextToken();
            var t2 = t.NextToken();

            Assert.Equal(start, t1);
            Assert.Equal(start + 1, t2);
        }

        [Trait("Category", "GetToken")]
        [Fact(DisplayName = "Rolls over at int.MaxValue")]
        public void Rolls_Over_At_Int_MaxValue()
        {
            var t = new TokenFactory(int.MaxValue);

            var t1 = t.NextToken();
            var t2 = t.NextToken();

            Assert.Equal(int.MaxValue, t1);
            Assert.Equal(0, t2);
        }
    }
}
