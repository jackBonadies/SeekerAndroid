// <copyright file="SearchResponseTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchResponseTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Given_Data(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength, File file)
        {
            var list = new List<File>()
            {
                file,
            };

            var locked = new List<File>()
            {
                file,
                file,
            };

            var r = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, list, locked);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(hasFreeUploadSlot, r.HasFreeUploadSlot);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);

            Assert.Equal(1, r.FileCount);
            Assert.Equal(list, r.Files);
            Assert.Single(r.Files);
            Assert.Equal(file, r.Files.First());

            Assert.Equal(locked, r.LockedFiles);
            Assert.Equal(2, r.LockedFileCount);
            Assert.Equal(file, r.LockedFiles.First());
            Assert.Equal(file, r.LockedFiles.Last());
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given response and list, replacing filecount with list length"), AutoData]
        public void Instantiates_With_Given_Response_And_List(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
        {
            var r1 = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, null);

            var r2 = new SearchResponse(r1, new List<File>() { new File(1, "foo", 2, "ext") });

            Assert.Empty(r1.Files);
            Assert.Single(r2.Files);

            Assert.Equal(r1.Username, r2.Username);
            Assert.Equal(r1.Token, r2.Token);
            Assert.Equal(1, r2.FileCount);
            Assert.Equal(r1.UploadSpeed, r2.UploadSpeed);
            Assert.Equal(r1.QueueLength, r2.QueueLength);

            Assert.Equal(hasFreeUploadSlot, r2.HasFreeUploadSlot);
        }
    }
}
