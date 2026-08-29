// <copyright file="BrowseResponseTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using Xunit;

    public class BrowseResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Instantiates with no data")]
        public void Instantiates_With_No_Data()
        {
            var a = new BrowseResponse();

            Assert.Empty(a.Directories);
            Assert.Equal(0, a.DirectoryCount);
            Assert.Empty(a.LockedDirectories);
            Assert.Equal(0, a.LockedDirectoryCount);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Instantiates with the given directory list")]
        public void Instantiates_With_The_Given_Directory_List()
        {
            var dir = new Directory("foo");
            var list = new List<Directory>(new[] { dir });

            var a = new BrowseResponse(list);

            Assert.Equal(list.Count, a.DirectoryCount);
            Assert.Single(a.Directories);
            Assert.Equal(dir, a.Directories.ToList()[0]);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Instantiates with the given locked directory list")]
        public void Instantiates_With_The_Given_Locked_Directory_List()
        {
            var dir = new Directory("foo");
            var list = new List<Directory>(new[] { dir });

            var a = new BrowseResponse(lockedDirectoryList: list);

            Assert.Equal(list.Count, a.LockedDirectoryCount);
            Assert.Single(a.LockedDirectories);
            Assert.Equal(dir, a.LockedDirectories.ToList()[0]);
        }
    }
}
