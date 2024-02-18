// <copyright file="BrowseEventArgsTests.cs" company="JP Dillingham">
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

    public class BrowseEventArgsTests
    {
        [Trait("Category", "BrowseEventArgs Instantiation")]
        [Theory(DisplayName = "BrowseEventArgs Instantiates with the given data"), AutoData]
        internal void BrowseEventArgs_Instantiates_With_The_Given_Data(string username)
        {
            var e = new BrowseEventArgs(username);

            Assert.Equal(username, e.Username);
        }

        [Trait("Category", "BrowseProgressUpdatedEventArgs Instantiation")]
        [Theory(DisplayName = "BrowseProgressUpdatedEventArgs Instantiates with the given data"), AutoData]
        internal void BrowseProgressUpdatedEventArgs_Instantiates_With_The_Given_Data(string username, long bytes, long size)
        {
            var e = new BrowseProgressUpdatedEventArgs(username, bytes, size);

            Assert.Equal(username, e.Username);
            Assert.Equal(bytes, e.BytesTransferred);
            Assert.Equal(size, e.Size);
            Assert.Equal(size - bytes, e.BytesRemaining);
            Assert.Equal((bytes / (double)size) * 100, e.PercentComplete);
        }
    }
}
