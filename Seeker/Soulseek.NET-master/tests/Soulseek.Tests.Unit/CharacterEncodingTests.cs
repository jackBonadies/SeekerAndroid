// <copyright file="CharacterEncodingTests.cs" company="JP Dillingham">
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
    using Xunit;

    public class CharacterEncodingTests
    {
        [Trait("Category", "CharacterEncoding")]
        [Fact(DisplayName = "Returns UTF-8 from UTF8 prop")]
        public void Returns_UTF_8_From_UTF_Prop()
        {
            Assert.Equal("UTF-8", CharacterEncoding.UTF8);
        }

        [Trait("Category", "CharacterEncoding")]
        [Fact(DisplayName = "Returns ISO-8859-1 from ISO88591 prop")]
        public void Returns_ISO_8859_1_From_ISO88591_Prop()
        {
            Assert.Equal("ISO-8859-1", CharacterEncoding.ISO88591);
        }

        [Trait("Category", "CharacterEncoding")]
        [Fact(DisplayName = "Throws given anything other than UTF-8 or ISO-8859-1")]
        public void Throws_Given_Anything_Other_Than_UTF_8_Or_ISO_8859_1()
        {
            var ex = Record.Exception(() => new CharacterEncoding("foo"));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Trait("Category", "CharacterEncoding")]
        [Fact(DisplayName = "Throws given null")]
        public void Throws_Given_Null()
        {
            var ex = Record.Exception(() => new CharacterEncoding(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }
    }
}
