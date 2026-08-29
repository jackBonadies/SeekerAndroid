// <copyright file="PierceFirewallTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using System.Collections.Generic;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class PierceFirewallTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(int token)
        {
            var r = new PierceFirewall(token);

            Assert.Equal(token, r.Token);
        }

        [Trait("Category", "TryParse")]
        [Fact(DisplayName = "TryParse returns false on code mismatch")]
        public void TryParse_Returns_False_On_Code_Mismatch()
        {
            var msg = new List<byte>();

            msg.AddRange(BitConverter.GetBytes(0)); // overall length, ignored for this test.
            msg.Add((byte)MessageCode.Initialization.PeerInit);

            var r = PierceFirewall.TryFromByteArray(msg.ToArray(), out var result);

            Assert.False(r);
            Assert.Null(result);
        }

        [Trait("Category", "TryParse")]
        [Fact(DisplayName = "TryParse returns false on missing data")]
        public void TryParse_Returns_False_On_Missing_Data()
        {
            var msg = new List<byte>();

            msg.AddRange(BitConverter.GetBytes(0)); // overall length, ignored for this test.
            msg.Add((byte)MessageCode.Initialization.PierceFirewall);

            // omit token
            var r = PierceFirewall.TryFromByteArray(msg.ToArray(), out var result);

            Assert.False(r);
            Assert.Null(result);
        }

        [Trait("Category", "TryParse")]
        [Theory(DisplayName = "TryParse returns expected data"), AutoData]
        public void TryParse_Returns_Expected_Data(int token)
        {
            var msg = new List<byte>();

            msg.AddRange(BitConverter.GetBytes(0)); // overall length, ignored for this test.
            msg.Add((byte)MessageCode.Initialization.PierceFirewall);

            msg.AddRange(BitConverter.GetBytes(token));

            // omit token
            var r = PierceFirewall.TryFromByteArray(msg.ToArray(), out var result);

            Assert.True(r);
            Assert.NotNull(result);

            Assert.Equal(token, result.Token);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PierceFirewallRequest")]
        [Fact(DisplayName = "PierceFirewallRequest instantiates properly")]
        public void PierceFirewallRequest_Instantiates_Properly()
        {
            var token = new Random().Next();
            var a = new PierceFirewall(token);

            Assert.Equal(token, a.Token);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PierceFirewallRequest")]
        [Fact(DisplayName = "PierceFirewallRequest constructs the correct Message")]
        public void PierceFirewallRequest_Constructs_The_Correct_Message()
        {
            var token = new Random().Next();
            var a = new PierceFirewall(token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Initialization>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Initialization.PierceFirewall, code);
            Assert.Equal(4 + 1 + 4, msg.Length);

            Assert.Equal(token, reader.ReadInteger());
        }
    }
}
