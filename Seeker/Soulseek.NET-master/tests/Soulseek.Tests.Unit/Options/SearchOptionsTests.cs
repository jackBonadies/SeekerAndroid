// <copyright file="SearchOptionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Options
{
    using System;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Defaults(
            int searchTimeout,
            int responseLimit,
            bool filterResponses,
            int minimumResponseFileCount,
            int maximumPeerQueueLength,
            int minimumPeerUploadSpeed,
            Func<SearchResponse, bool> responseFilter,
            int fileLimit,
            bool removeSingleCharacterSearchTerms,
            Func<File, bool> fileFilter,
            Action<(SearchStates PreviousState, Search Search)> stateChanged,
            Action<(Search Search, SearchResponse Response)> responseReceived)
        {
            var o = new SearchOptions(
                searchTimeout,
                responseLimit,
                filterResponses,
                minimumResponseFileCount,
                maximumPeerQueueLength,
                minimumPeerUploadSpeed,
                fileLimit,
                removeSingleCharacterSearchTerms,
                responseFilter,
                fileFilter,
                stateChanged,
                responseReceived);

            Assert.Equal(searchTimeout, o.SearchTimeout);
            Assert.Equal(responseLimit, o.ResponseLimit);
            Assert.Equal(filterResponses, o.FilterResponses);
            Assert.Equal(minimumResponseFileCount, o.MinimumResponseFileCount);
            Assert.Equal(maximumPeerQueueLength, o.MaximumPeerQueueLength);
            Assert.Equal(minimumPeerUploadSpeed, o.MinimumPeerUploadSpeed);
            Assert.Equal(responseFilter, o.ResponseFilter);
            Assert.Equal(fileLimit, o.FileLimit);
            Assert.Equal(removeSingleCharacterSearchTerms, o.RemoveSingleCharacterSearchTerms);
            Assert.Equal(fileFilter, o.FileFilter);
            Assert.Equal(stateChanged, o.StateChanged);
            Assert.Equal(responseReceived, o.ResponseReceived);
        }
    }
}
