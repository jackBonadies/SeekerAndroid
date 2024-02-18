// <copyright file="ServerPingTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class ServerPingTests
    {
        [Trait("Category", "ToByteArray")]
        [Fact(DisplayName = "ToByteArray Constructs the correct Message")]
        public void ToByteArray_Constructs_The_Correct_Message()
        {
            var msg = new ServerPing().ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.Ping, code);
            Assert.Equal(8, msg.Length);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray returns the expected data")]
        public void FromByteArray_Returns_Expected_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Ping)
                .Build();

            var ex = Record.Exception(() => ServerPing.FromByteArray(msg));

            Assert.Null(ex);
        }

        [Trait("Category", "FromByteArray")]
        [Fact(DisplayName = "FromByteArray throws MessageException on code mismatch")]
        public void FromByteArray_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.BranchLevel)
                .WriteInteger(1)
                .Build();

            var ex = Record.Exception(() => ServerPing.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }
    }
}
