// <copyright file="WaitKeyTests.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class WaitKeyTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with parts")]
        public void Instantiates_With_Parts()
        {
            WaitKey k = null;
            var ex = Record.Exception(() => k = new WaitKey(1, 2));

            Assert.Null(ex);
            Assert.NotNull(k);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with null parts")]
        public void Instantiates_With_Null_Parts()
        {
            WaitKey k = null;
            var ex = Record.Exception(() => k = new WaitKey());

            Assert.Null(ex);
            Assert.NotNull(k);
        }

        [Trait("Category", "Hash Code")]
        [Fact(DisplayName = "GetHashCode() matches given identical key")]
        public void GetHashCode_Matches_Given_Identical_Key()
        {
            var k1 = new WaitKey(1, "test", 5m);
            var k2 = new WaitKey(1, "test", 5m);

            Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
        }

        [Trait("Category", "Hash Code")]
        [Fact(DisplayName = "GetHashCode() differs given different key")]
        public void GetHashCode_Differs_Given_Different_Key()
        {
            var k1 = new WaitKey(1, "test", 5m);
            var k2 = new WaitKey(2, "foo", 2.5m);

            Assert.NotEqual(k1.GetHashCode(), k2.GetHashCode());
        }

        [Trait("Category", "Hash Code")]
        [Fact(DisplayName = "GetHashCode() returns 0 for null parts")]
        public void GetHashCode_Returns_0_For_Null_Parts()
        {
            var k = new WaitKey();

            Assert.Equal(0, k.GetHashCode());
        }

        [Trait("Category", "Token Parts")]
        [Fact(DisplayName = "TokenParts returns parts")]
        public void TokenParts_Returns_Parts()
        {
            WaitKey k = new WaitKey(1, 2);

            Assert.Equal(2, k.TokenParts.Length);
            Assert.Contains(1, k.TokenParts);
            Assert.Contains(2, k.TokenParts);
        }

        [Trait("Category", "Token Parts")]
        [Fact(DisplayName = "TokenParts returns empty given null parts")]
        public void TokenParts_Returns_Empty_Given_Null_Parts()
        {
            WaitKey k = new WaitKey();

            Assert.NotNull(k.TokenParts);
            Assert.Empty(k.TokenParts);
        }

        [Trait("Category", "Token")]
        [Fact(DisplayName = "Token is blank given null parts")]
        public void Token_Is_Blank_Given_Null_Parts()
        {
            WaitKey k = new WaitKey();

            Assert.Equal(string.Empty, k.Token);
        }

        [Trait("Category", "Token")]
        [Fact(DisplayName = "Token contains all parts")]
        public void Token_Contains_All_Parts()
        {
            var parts = new List<object>();

            for (int i = 0; i < 10; i++)
            {
                parts.Add(Guid.NewGuid().ToString());
            }

            WaitKey k = new WaitKey(parts.ToArray());

            foreach (var part in parts)
            {
                Assert.Contains(part.ToString(), k.Token, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "ToString")]
        [Fact(DisplayName = "ToString returns Token")]
        public void ToString_Returns_Token()
        {
            var k = new WaitKey(1, "foo", 2, "bar");

            Assert.Equal(k.Token, k.ToString());
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns true when equal")]
        public void Equals_Returns_True_When_Equal()
        {
            var k1 = new WaitKey("foo", 2);
            var k2 = new WaitKey("foo", 2);

            Assert.True(k1.Equals(k2));
            Assert.True(k2.Equals(k1));
        }

        [Trait("Category", "==")]
        [Fact(DisplayName = "== returns true when equal")]
        public void DoubleEqualOperator_Returns_True_When_Equal()
        {
            var k1 = new WaitKey("foo", 2);
            var k2 = new WaitKey("foo", 2);

            Assert.True(k1 == k2);
            Assert.True(k2 == k1);
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns false when not equal")]
        public void Equals_Returns_False_When_Not_Equal()
        {
            var k1 = new WaitKey("foo", 2);
            var k2 = new WaitKey("bar", 3);

            Assert.False(k1.Equals(k2));
            Assert.False(k2.Equals(k1));
        }

        [Trait("Category", "!=")]
        [Fact(DisplayName = "!= returns false when not equal")]
        public void NotEqualOperator_Returns_False_When_Not_Equal()
        {
            var k1 = new WaitKey("foo", 2);
            var k2 = new WaitKey("bar", 3);

            Assert.True(k1 != k2);
            Assert.True(k2 != k1);
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals returns false when different type")]
        public void Equals_Returns_False_When_Different_Type()
        {
            var k1 = new WaitKey("foo", 2);
            var k2 = "bar";

            Assert.False(k1.Equals(k2));
            Assert.False(k2.Equals(k1));
        }

        [Trait("Category", "Equals")]
        [Fact(DisplayName = "Equals handles boxed instances")]
        public void Equals_Handles_Boxed_Instances()
        {
            var k1 = new WaitKey("foo", 2);
            var k2 = new WaitKey("foo", 2);

            Assert.True(k1.Equals((object)k2));
            Assert.True(k2.Equals((object)k1));
        }
    }
}
