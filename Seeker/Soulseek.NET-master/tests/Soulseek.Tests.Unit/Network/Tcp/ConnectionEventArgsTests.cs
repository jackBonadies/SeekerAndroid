// <copyright file="ConnectionEventArgsTests.cs" company="JP Dillingham">
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
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ConnectionEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ConnectionDataEventArgs instantiates properly")]
        public void ConnectionDataEventArgs_Instantiates_Properly()
        {
            var data = new byte[] { 0x0, 0x1, 0x3 };

            ConnectionDataEventArgs d = null;

            var ex = Record.Exception(() => d = new ConnectionDataEventArgs(data.Length, 20));

            Assert.Null(ex);
            Assert.NotNull(d);

            Assert.Equal(3, d.CurrentLength);
            Assert.Equal(20, d.TotalLength);
            Assert.Equal(15d, d.PercentComplete);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ConnectionStateChangedEventArgs instantiates properly")]
        public void ConnectionStateChangedEventArgs_Instantiates_Properly()
        {
            ConnectionStateChangedEventArgs s = null;

            var e = new Exception("bar");

            var ex = Record.Exception(() => s = new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected, "foo", e));

            Assert.Null(ex);
            Assert.NotNull(s);

            Assert.Equal(ConnectionState.Connected, s.PreviousState);
            Assert.Equal(ConnectionState.Disconnected, s.CurrentState);
            Assert.Equal("foo", s.Message);
            Assert.Equal(e, s.Exception);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ConnectionStateChangedEventArgs message is null if omitted")]
        public void ConnectionStateChangedEventArgs_Message_Is_Null_If_Omitted()
        {
            ConnectionStateChangedEventArgs s = null;

            var ex = Record.Exception(() => s = new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected));

            Assert.Null(ex);
            Assert.NotNull(s);

            Assert.Equal(ConnectionState.Connected, s.PreviousState);
            Assert.Equal(ConnectionState.Disconnected, s.CurrentState);
            Assert.Null(s.Message);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ConnectionDisconnectedEventArgs instantiates properly")]
        public void ConnectionDisconnectedEventArgs_Instantiates_Properly()
        {
            ConnectionDisconnectedEventArgs s = null;

            var e = new Exception("bar");

            var ex = Record.Exception(() => s = new ConnectionDisconnectedEventArgs("foo", e));

            Assert.Null(ex);
            Assert.NotNull(s);

            Assert.Equal("foo", s.Message);
            Assert.Equal(e, s.Exception);
        }
    }
}
