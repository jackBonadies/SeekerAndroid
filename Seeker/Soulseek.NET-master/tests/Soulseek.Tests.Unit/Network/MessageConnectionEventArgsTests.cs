// <copyright file="MessageConnectionEventArgsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Network
{
    using AutoFixture.Xunit2;
    using Soulseek.Network;
    using Xunit;

    public class MessageConnectionEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "MessageDataEventArgs instantiates with the expected values"), AutoData]
        public void MessageDataEventArgs_Instantiates_With_The_Expected_Values(byte[] code, long current, long total)
        {
            var a = new MessageDataEventArgs(code, current, total);

            Assert.Equal(code, a.Code);
            Assert.Equal(current, a.CurrentLength);
            Assert.Equal(total, a.TotalLength);
            Assert.Equal((current / (double)total) * 100d, a.PercentComplete);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "MessageEventArgs instantiates with the expected values")]
        public void MessageEventArgs_Instantiates_With_The_Expected_Values()
        {
            var message = new byte[42];

            var a = new MessageEventArgs(message);

            Assert.Equal(message, a.Message);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "MessageReceivedEventArgs instantiates with the expected values"), AutoData]
        public void MessageReceivedEventArgs_Instantiates_With_The_Expected_Values(long length, byte[] code)
        {
            var a = new MessageReceivedEventArgs(length, code);

            Assert.Equal(code, a.Code);
            Assert.Equal(length, a.Length);
        }
    }
}