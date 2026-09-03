// <copyright file="SearchScopeTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SearchScopeTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates Network Default")]
        public void Instantiates_Network_Default()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Network));

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.Network, s.Type);
            Assert.Empty(s.Subjects);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws on Network when subjects is not empty"), AutoData]
        public void Throws_On_Network_When_Subjects_Is_Not_Empty(string[] subjects)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Network, subjects));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("subjects"));
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws on Wishlist when subjects is not empty"), AutoData]
        public void Throws_On_Wishlist_When_Subjects_Is_Not_Empty(string[] subjects)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Wishlist, subjects));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("subjects"));
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates Room"), AutoData]
        public void Instantiates_Room(string room)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, room));

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.Room, s.Type);
            Assert.Single(s.Subjects);
            Assert.Equal(room, s.Subjects.First());
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on Room when subjects is empty")]
        public void Throws_On_Room_When_Subjects_Is_Empty()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on Room when subjects is one null string")]
        public void Throws_On_Room_When_Subjects_Is_One_Null_String()
        {
            SearchScope s = null;

#pragma warning disable S3878 // Arrays should not be created for params parameters

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, new string[] { null }));
#pragma warning restore S3878 // Arrays should not be created for params parameters

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on Room when subjects is one empty string")]
        public void Throws_On_Room_When_Subjects_Is_One_Empty_String()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, string.Empty));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on Room when subjects is more than one")]
        public void Throws_On_Room_When_Subjects_Is_More_Than_One()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.Room, "one", "two"));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires a single, non null and non empty"));
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates User"), AutoData]
        public void Instantiates_User(string user)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.User, user));

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.User, s.Type);
            Assert.Single(s.Subjects);
            Assert.Equal(user, s.Subjects.First());
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on User when subjects is empty")]
        public void Throws_On_User_When_Subjects_Is_Empty()
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.User, Array.Empty<string>()));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("requires at least one subject"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on User when subjects contains a null")]
        public void Throws_On_User_When_Subjects_Contains_A_Null()
        {
            SearchScope s = null;

#pragma warning disable S3878 // Arrays should not be created for params parameters

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.User, new string[] { "one", null }));
#pragma warning restore S3878 // Arrays should not be created for params parameters

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("One or more of the supplied User scope subjects is null or empty"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws on User when subjects contains an empty string")]
        public void Throws_On_User_When_Subjects_Contains_An_Empty_String()
        {
            SearchScope s = null;

#pragma warning disable S3878 // Arrays should not be created for params parameters

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.User, new string[] { "one", string.Empty }));
#pragma warning restore S3878 // Arrays should not be created for params parameters

            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
            Assert.True(ex.Message.ContainsInsensitive("One or more of the supplied User scope subjects is null or empty"));
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates User with multiples"), AutoData]
        public void Instantiates_User_With_Multiples(string[] users)
        {
            SearchScope s = null;

            var ex = Record.Exception(() => s = new SearchScope(SearchScopeType.User, users));

            Assert.Null(ex);

            Assert.Equal(SearchScopeType.User, s.Type);
            Assert.Equal(users.Length, s.Subjects.Count());
            Assert.Equal(users, s.Subjects);
        }

        [Trait("Category", "Factories")]
        [Fact(DisplayName = "Network returns Network scope")]
        public void Network_Returns_Network_Scope()
        {
            var s = SearchScope.Network;

            Assert.Equal(SearchScopeType.Network, s.Type);
            Assert.Empty(s.Subjects);
        }

        [Trait("Category", "Factories")]
        [Fact(DisplayName = "Wishlist returns Wishlist scope")]
        public void Wishlist_Returns_Wishlist_Scope()
        {
            var s = SearchScope.Wishlist;

            Assert.Equal(SearchScopeType.Wishlist, s.Type);
            Assert.Empty(s.Subjects);
        }

        [Trait("Category", "Factories")]
        [Theory(DisplayName = "Room() returns Room scope"), AutoData]
        public void Room_Returns_Room_Scope(string room)
        {
            var s = SearchScope.Room(room);

            Assert.Equal(SearchScopeType.Room, s.Type);
            Assert.Single(s.Subjects);
            Assert.Equal(room, s.Subjects.First());
        }

        [Trait("Category", "Factories")]
        [Theory(DisplayName = "User() returns User scope"), AutoData]
        public void User_Returns_User_Scope(string[] users)
        {
            var s = SearchScope.User(users);

            Assert.Equal(SearchScopeType.User, s.Type);
            Assert.Equal(users.Length, s.Subjects.Count());
            Assert.Equal(users, s.Subjects);
        }
    }
}
