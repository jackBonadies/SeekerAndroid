// <copyright file="ListenerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Network.Tcp
{
    using System;
    using System.Net;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ListenerTests
    {
        private static readonly Random RNG = new Random();

        private static int GetPort()
        {
            return 50000 + RNG.Next(1, 9999);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var l = new Listener(IPAddress.Any, port, options);

            Assert.Equal(IPAddress.Any, l.IPAddress);
            Assert.Equal(port, l.Port);
            Assert.Equal(options, l.ConnectionOptions);

            Assert.False(l.Listening);
        }

        [Trait("Category", "Start")]
        [Fact(DisplayName = "Start starts listening")]
        public void Start_Starts_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var l = new Listener(IPAddress.Any, port, options);

            var first = l.Listening;

            l.Start();

            Assert.False(first);
            Assert.True(l.Listening);
        }

        [Trait("Category", "Stop")]
        [Fact(DisplayName = "Stop stops listening")]
        public void Stop_Stops_Listening()
        {
            var options = new ConnectionOptions();
            var port = GetPort();

            var l = new Listener(IPAddress.Any, port, options);

            l.Start();

            var first = l.Listening;

            l.Stop();

            Assert.True(first);
            Assert.False(l.Listening);
        }
    }
}
