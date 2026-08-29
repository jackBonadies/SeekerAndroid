// <copyright file="RawBrowseResponseTests.cs" company="JP Dillingham">
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
    using System.IO;
    using AutoFixture.Xunit2;
    using Xunit;

    public class RawBrowseResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Given_Data(long length)
        {
            using (var stream = new MemoryStream())
            {
                var r = new RawBrowseResponse(length, stream);

                Assert.Equal(length, r.Length);
                Assert.Equal(stream, r.Stream);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws if the given length is less than or equal to zero")]
        [InlineData(0)]
        [InlineData(-1)]
        public void Throws_If_The_Given_Length_Is_LTE_Zero(long length)
        {
            using (var stream = new MemoryStream())
            {
                var ex = Record.Exception(() => new RawBrowseResponse(length, stream));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws if the given stream is null"), AutoData]
        public void Throws_If_The_Given_Stream_Is_Null(long length)
        {
            var ex = Record.Exception(() => new RawBrowseResponse(length, null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }
    }
}
