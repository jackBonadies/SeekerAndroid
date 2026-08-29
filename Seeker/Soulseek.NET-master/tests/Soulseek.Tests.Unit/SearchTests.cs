// <copyright file="SearchTests.cs" company="JP Dillingham">
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

    public class SearchTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with expected data"), AutoData]
        public void Instantiates_With_Expected_Data(string searchText, int token, SearchStates state, int responseCount, int fileCount, int lockedFileCount)
        {
            var s = new Search(new SearchQuery(searchText), SearchScope.Network, token, state, responseCount, fileCount, lockedFileCount);

            Assert.Equal(searchText, s.Query.SearchText);
            Assert.Equal(SearchScope.Network.Type, s.Scope.Type);
            Assert.Equal(token, s.Token);
            Assert.Equal(state, s.State);
            Assert.Equal(responseCount, s.ResponseCount);
            Assert.Equal(fileCount, s.FileCount);
            Assert.Equal(lockedFileCount, s.LockedFileCount);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with expected data given SearchInternal"), AutoData]
        internal void Instantiates_With_Expected_Data_Given_SearchInternal(string searchText, int token)
        {
            var i = new SearchInternal(SearchQuery.FromText(searchText), SearchScope.Network, token);
            i.SetState(SearchStates.Completed);
            i.TryAddResponse(new SearchResponse("foo", 42, false, 420, 24, new List<File>()
            {
                new File(1, "foo.bar", 2323, "bar", null),
            }));

            var s = new Search(i);

            Assert.Equal(i.Query.SearchText, s.Query.SearchText);
            Assert.Equal(i.Token, s.Token);
            Assert.Equal(i.State, s.State);
            Assert.Equal(i.ResponseCount, s.ResponseCount);
            Assert.Equal(i.FileCount, s.FileCount);
            Assert.Equal(i.LockedFileCount, s.LockedFileCount);
        }
    }
}
