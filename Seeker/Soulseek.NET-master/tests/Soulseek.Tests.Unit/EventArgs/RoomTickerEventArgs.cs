// <copyright file="RoomTickerEventArgs.cs" company="JP Dillingham">
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
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class RoomTickerEventArgs
    {
        [Trait("Category", "Instantiation")]
        [Trait("Class", "RoomTickerAddedEventArgs")]
        [Theory(DisplayName = "Instantiates with expected values"), AutoData]
        public void RoomTickerAddedEventArgs_Instantiates_With_Expected_Values(string roomName, RoomTicker ticker)
        {
            var x = new RoomTickerAddedEventArgs(roomName, ticker);

            Assert.Equal(roomName, x.RoomName);
            Assert.Equal(ticker, x.Ticker);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Class", "RoomTickerRemovedEventArgs")]
        [Theory(DisplayName = "Instantiates with expected values"), AutoData]
        public void RoomTickerRemovedEventArgs_Instantiates_With_Expected_Values(string roomName, string username)
        {
            var x = new RoomTickerRemovedEventArgs(roomName, username);

            Assert.Equal(roomName, x.RoomName);
            Assert.Equal(username, x.Username);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Class", "RoomTickerListReceivedEventArgs")]
        [Theory(DisplayName = "Instantiates with expected values"), AutoData]
        public void RoomTickerListReceivedEventArgs_Instantiates_With_Expected_Values(string roomName, IEnumerable<RoomTicker> tickers)
        {
            var x = new RoomTickerListReceivedEventArgs(roomName, tickers);

            Assert.Equal(roomName, x.RoomName);
            Assert.Equal(tickers.Count(), x.TickerCount);
            Assert.Equal(tickers, x.Tickers);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Class", "RoomTickerListReceivedEventArgs")]
        [Theory(DisplayName = "Instantiates with expected values given null tickers"), AutoData]
        public void RoomTickerListReceivedEventArgs_Instantiates_With_Expected_Values_Given_Null_Tickers(string roomName)
        {
            var x = new RoomTickerListReceivedEventArgs(roomName, null);

            Assert.Equal(roomName, x.RoomName);
            Assert.Equal(0, x.TickerCount);
            Assert.Empty(x.Tickers);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Class", "RoomTickerListReceivedEventArgs")]
        [Theory(DisplayName = "Instantiates with expected values"), AutoData]
        public void RoomTickerListReceivedEventArgs_Instantiates_With_FromNotification(string roomName, IEnumerable<RoomTicker> tickers)
        {
            var x = new RoomTickerListNotification(roomName, tickers.Count(), tickers);
            var y = new RoomTickerListReceivedEventArgs(x);

            Assert.Equal(roomName, y.RoomName);
            Assert.Equal(tickers.Count(), y.TickerCount);
            Assert.Equal(tickers, y.Tickers);
        }
    }
}
