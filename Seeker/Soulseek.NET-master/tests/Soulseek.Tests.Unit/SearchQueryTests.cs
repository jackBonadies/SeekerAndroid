// <copyright file="SearchQueryTests.cs" company="JP Dillingham">
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

    public class SearchQueryTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given values"), AutoData]
        public void Instantiates_With_Given_Values(string query, IEnumerable<string> exclusions)
        {
            var s = new SearchQuery(query, exclusions);

            Assert.Equal(query, s.Query);
            Assert.Equal(exclusions, s.Exclusions);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with null searchText")]
        public void Instantiates_With_Null_SearchText()
        {
            var s = new SearchQuery(null);

            Assert.Empty(s.Terms);
            Assert.Empty(s.Exclusions);
            Assert.Equal(string.Empty, s.Query);
            Assert.Equal(string.Empty, s.SearchText);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with null query and exclusions")]
        public void Instantiates_With_Null_Query_And_Exclusions()
        {
            var s = new SearchQuery(query: null, exclusions: null);

            Assert.Empty(s.Terms);
            Assert.Empty(s.Exclusions);
            Assert.Equal(string.Empty, s.Query);
            Assert.Equal(string.Empty, s.SearchText);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with null terms and exclusions")]
        public void Instantiates_With_Null_Terms_And_Exclusions()
        {
            var s = new SearchQuery(terms: null, exclusions: null);

            Assert.Empty(s.Terms);
            Assert.Empty(s.Exclusions);
            Assert.Equal(string.Empty, s.Query);
            Assert.Equal(string.Empty, s.SearchText);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Splits terms and exclusions")]
        public void Splits_Terms_And_Exclusions()
        {
            var s = new SearchQuery("foo bar -baz -qux");

            Assert.Equal("foo", s.Terms.ToList()[0]);
            Assert.Equal("bar", s.Terms.ToList()[1]);
            Assert.Equal("baz", s.Exclusions.ToList()[0]);
            Assert.Equal("qux", s.Exclusions.ToList()[1]);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Constructs expected search text")]
        [InlineData("foo", new[] { "bar", "baz" }, "foo -bar -baz")]
        [InlineData("foo", new[] { "bar" }, "foo -bar")]
        [InlineData("foo", null, "foo")]
        public void Constructs_Expected_Search_Text(string query, string[] exclusions, string expected)
        {
            var s = new SearchQuery(query, exclusions);

            Assert.Equal(expected, s.SearchText);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses query-only search text")]
        public void Parses_Query_Only_Search_Text()
        {
            var s = new SearchQuery("foo");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo", s.SearchText);
            Assert.Empty(s.Exclusions);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses single character tokens and punctuation from search text")]
        public void Parses_Single_Character_Tokens_And_Punctuation_From_Search_Text()
        {
            var s = new SearchQuery("a ! b @ c # d % e ^ f & g * h ( i ) j - k _ l + m = n -big_old_exclusion ~ o ` p [ q { r ] s } t | u \\ v ; w : x ' y \" z , a < b . c > d / e ?");

            Assert.Equal("a ! b @ c # d % e ^ f & g * h ( i ) j - k _ l + m = n ~ o ` p [ q { r ] s } t | u \\ v ; w : x ' y \" z , a < b . c > d / e ?", s.Query);
            Assert.Equal("a ! b @ c # d % e ^ f & g * h ( i ) j - k _ l + m = n ~ o ` p [ q { r ] s } t | u \\ v ; w : x ' y \" z , a < b . c > d / e ? -big_old_exclusion", s.SearchText);
            Assert.Single(s.Exclusions);
            Assert.Equal("big_old_exclusion", s.Exclusions.ToList()[0]);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses exclusions")]
        public void Parses_Exclusions()
        {
            var s = new SearchQuery("foo -bar -baz");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo -bar -baz", s.SearchText);
            Assert.Equal(2, s.Exclusions.Count);
            Assert.Equal("bar", s.Exclusions.ToList()[0]);
            Assert.Equal("baz", s.Exclusions.ToList()[1]);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses exclusions out of order")]
        public void Parses_Exclusions_Out_Of_Order()
        {
            var s = new SearchQuery("-bar foo -baz");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo -bar -baz", s.SearchText);
            Assert.Equal(2, s.Exclusions.Count);
            Assert.Equal("bar", s.Exclusions.ToList()[0]);
            Assert.Equal("baz", s.Exclusions.ToList()[1]);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Parses releated exclusions singly")]
        public void Parses_Repeated_Exclusions_Singly()
        {
            var s = new SearchQuery("-bar foo -baz -baz -bar");

            Assert.Equal("foo", s.Query);
            Assert.Equal("foo -bar -baz", s.SearchText);
            Assert.Equal(2, s.Exclusions.Count);
            Assert.Equal("bar", s.Exclusions.ToList()[0]);
            Assert.Equal("baz", s.Exclusions.ToList()[1]);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Preserves duplicate terms")]
        public void Preserves_Duplicate_Terms()
        {
            var s = new SearchQuery("foo bar foo foo");

            Assert.Equal("foo bar foo foo", s.Query);
            Assert.Equal("foo bar foo foo", s.SearchText);
            Assert.Empty(s.Exclusions);
        }

        [Trait("Category", "FromText")]
        [Theory(DisplayName = "FromText returns new instance from given text"), AutoData]
        public void FromText_Returns_New_Instance_From_Given_Text(string searchText)
        {
            var s = SearchQuery.FromText(searchText);

            Assert.Equal(searchText, s.SearchText);
        }
    }
}
