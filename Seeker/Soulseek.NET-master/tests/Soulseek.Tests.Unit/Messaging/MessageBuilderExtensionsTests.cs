// <copyright file="MessageBuilderExtensionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging
{
    using System;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Xunit;

    public class MessageBuilderExtensionsTests
    {
        [Trait("Category", "WriteFile")]
        [Fact(DisplayName = "WriteFile throws given null file")]
        public void WriteFile_Throws_Given_Null_File()
        {
            var builder = new MessageBuilder();

            var ex = Record.Exception(() => builder.WriteFile(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("file", ((ArgumentNullException)ex).ParamName);
        }

        [Trait("Category", "WriteFile")]
        [Theory(DisplayName = "WriteFile writes expected data"), AutoData]
        public void WriteFile_Writes_Expected_Data(File file)
        {
            var builder = new MessageBuilder();
            builder.WriteCode(MessageCode.Peer.BrowseResponse);

            var ex = Record.Exception(() => builder.WriteFile(file));

            Assert.Null(ex);

            var msg = new MessageReader<MessageCode.Peer>(builder.Build());
            msg.ReadCode();

            Assert.Equal(file.Code, msg.ReadByte());
            Assert.Equal(file.Filename, msg.ReadString());
            Assert.Equal(file.Size, msg.ReadLong());
            Assert.Equal(file.Extension, msg.ReadString());
            Assert.Equal(file.AttributeCount, msg.ReadInteger());
        }

        [Trait("Category", "WriteDirectory")]
        [Fact(DisplayName = "WriteDirectory throws given null file")]
        public void WriteDirectory_Throws_Given_Null_File()
        {
            var builder = new MessageBuilder();

            var ex = Record.Exception(() => builder.WriteDirectory(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("directory", ((ArgumentNullException)ex).ParamName);
        }

        [Trait("Category", "WriteDirectory")]
        [Theory(DisplayName = "WriteDirectory writes expected data"), AutoData]
        public void WriteDirectory_Writes_Expected_Data(Directory directory)
        {
            var builder = new MessageBuilder();
            builder.WriteCode(MessageCode.Peer.BrowseResponse);

            var ex = Record.Exception(() => builder.WriteDirectory(directory));

            Assert.Null(ex);

            var msg = new MessageReader<MessageCode.Peer>(builder.Build());
            msg.ReadCode();

            Assert.Equal(directory.Name, msg.ReadString());
            Assert.Equal(directory.FileCount, msg.ReadInteger());
        }
    }
}
