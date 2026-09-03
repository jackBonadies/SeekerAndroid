// <copyright file="ProxyOptionsTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class ProxyOptionsTests
    {
        private static readonly Random Rng = new Random();
        public static int Port => Rng.Next(1024, IPEndPoint.MaxPort);

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates properly"), AutoData]
        public void Instantiates_Properly(string username, string password)
        {
            var address = "127.0.0.1";
            var port = Port;

            ProxyOptions o = null;

            var ex = Record.Exception(() => o = new ProxyOptions(address, port, username, password));

            Assert.Null(ex);
            Assert.NotNull(o);

            Assert.Equal(address, o.Address);
            Assert.Equal(port, o.Port);
            Assert.Equal(username, o.Username);
            Assert.Equal(password, o.Password);

            Assert.Equal(IPAddress.Parse(address), o.IPAddress);
            Assert.Equal(new IPEndPoint(IPAddress.Parse(address), port), o.IPEndPoint);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws AddressException on bad address"), AutoData]
        public void Throws_ArgumentException_On_Bad_Address(string address)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                ProxyOptions o = null;

                var ex = Record.Exception(() => o = new ProxyOptions(address, 1, "u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<AddressException>(ex);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Does not throw on resolveable address")]
        public void Does_Not_Throw_On_Resolveable_Address()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                ProxyOptions o = null;

                var ex = Record.Exception(() => o = new ProxyOptions("localhost", 1, "u", "p"));

                Assert.Null(ex);

                Assert.True(IPAddress.IsLoopback(o.IPAddress));
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws ArgumentOutOfRangeException on bad port")]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Throws_ArgumentException_On_Bad_Port(int port)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                ProxyOptions o = null;

                var ex = Record.Exception(() => o = new ProxyOptions("127.0.0.01", port, "u", "p"));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws ArgumentException on bad input")]
        [InlineData("127.0.0.1", 1, null, "a")]
        [InlineData("127.0.0.1", 1, "a", null)]
        [InlineData(null, 1, "user", "pass")]
        [InlineData("", 1, "user", "pass")]
        [InlineData(" ", 1, "user", "pass")]
        public void Throws_ArgumentException_On_Bad_Input(string address, int port, string username, string password)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                ProxyOptions o = null;

                var ex = Record.Exception(() => o = new ProxyOptions(address, port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Does not throw if username and password are null")]
        public void Does_Not_Throw_If_Username_And_Password_Are_Null()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                ProxyOptions o = null;

                var ex = Record.Exception(() => o = new ProxyOptions("127.0.0.1", 1, username: null, password: null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Throws ArgumentOutOfRangeException on bad input")]
        [InlineData("127.0.0.1", 1, "", "")]
        [InlineData("127.0.0.1", 1, "", "a")]
        [InlineData("127.0.0.1", 1, "a", "")]
        [InlineData(
            "127.0.0.1",
            1,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "127.0.0.1",
            1,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "a")]
        [InlineData(
            "127.0.0.1",
            1,
            "a",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public void Throws_ArgumentOutOfRangeException_On_Bad_Input(string address, int port, string username, string password)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                ProxyOptions o = null;

                var ex = Record.Exception(() => o = new ProxyOptions(address, port, username, password));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
            }
        }
    }
}
