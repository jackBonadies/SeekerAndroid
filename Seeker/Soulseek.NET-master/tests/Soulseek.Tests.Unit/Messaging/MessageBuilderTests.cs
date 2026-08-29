// <copyright file="MessageBuilderTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Xunit;

    public class MessageBuilderTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates without exception")]
        public void Instantiates_Without_Exception()
        {
            MessageBuilder builder = null;
            var ex = Record.Exception(() => builder = new MessageBuilder());

            Assert.Null(ex);
        }

        [Trait("Category", "Code")]
        [Fact(DisplayName = "Peer code sets code bytes")]
        public void Peer_Code_Sets_Code_Bytes()
        {
            var builder = new MessageBuilder();

            builder.WriteCode(MessageCode.Peer.BrowseRequest);

            var code = builder.GetProperty<List<byte>>("CodeBytes");

            Assert.Equal(BitConverter.GetBytes((int)MessageCode.Peer.BrowseRequest), code);
        }

        [Trait("Category", "Code")]
        [Fact(DisplayName = "Server code sets code bytes")]
        public void Server_Code_Sets_Code_Bytes()
        {
            var builder = new MessageBuilder();

            builder.WriteCode(MessageCode.Server.AcceptChildren);

            var code = builder.GetProperty<List<byte>>("CodeBytes");

            Assert.Equal(BitConverter.GetBytes((int)MessageCode.Server.AcceptChildren), code);
        }

        [Trait("Category", "Code")]
        [Theory(DisplayName = "Distributed Code sets code bytes"), AutoData]
        internal void Distributed_Code_Sets_Code_Bytes(MessageCode.Distributed code)
        {
            var builder = new MessageBuilder();

            builder.WriteCode(code);

            var c = builder.GetProperty<List<byte>>("CodeBytes").ToArray();

            Assert.Equal((byte)code, c[0]);
        }

        [Trait("Category", "Code")]
        [Theory(DisplayName = "Initialization Code sets code bytes"), AutoData]
        internal void Initialization_Code_Sets_Code_Bytes(MessageCode.Initialization code)
        {
            var builder = new MessageBuilder();

            builder.WriteCode(code);

            var c = builder.GetProperty<List<byte>>("CodeBytes").ToArray();

            Assert.Equal((byte)code, c[0]);
        }

        [Trait("Category", "Code")]
        [Fact(DisplayName = "Code resets code bytes")]
        public void Code_Resets_Code_Bytes()
        {
            var builder = new MessageBuilder();

            builder.WriteCode(MessageCode.Peer.BrowseRequest);

            var code1 = builder.GetProperty<List<byte>>("CodeBytes");

            builder.WriteCode(MessageCode.Peer.BrowseResponse);

            var code2 = builder.GetProperty<List<byte>>("CodeBytes");

            Assert.Equal(BitConverter.GetBytes((int)MessageCode.Peer.BrowseRequest).ToList(), code1);
            Assert.Equal(BitConverter.GetBytes((int)MessageCode.Peer.BrowseResponse).ToList(), code2);
        }

        [Trait("Category", "Build")]
        [Fact(DisplayName = "Build throws when code not set")]
        public void Build_Throws_When_Code_Not_Set()
        {
            var builder = new MessageBuilder();

            var ex = Record.Exception(() => builder.Build());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Build")]
        [Fact(DisplayName = "Build returns empty message when empty")]
        public void Build_Returns_Empty_Message_When_Empty()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.BrowseRequest, code);
            Assert.False(reader.HasMoreData);
        }

        [Trait("Category", "Compress")]
        [Theory(DisplayName = "Compress produces valid data"), AutoData]
        public void Compress_Produces_Valid_Data(string txt, int num, string txt2)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoRequest)
                .WriteString(txt)
                .WriteInteger(num)
                .WriteString(txt2)
                .Compress()
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);

            reader.Decompress();

            Assert.Equal(txt, reader.ReadString());
            Assert.Equal(num, reader.ReadInteger());
            Assert.Equal(txt2, reader.ReadString());
        }

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress throws when payload is empty")]
        public void Compress_Throws_When_Payload_Is_Empty()
        {
            var ex = Record.Exception(() => new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoRequest)
                .Compress());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress throws when already compressed")]
        public void Compress_Throws_When_Already_Compressed()
        {
            var ex = Record.Exception(() => new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoRequest)
                .WriteString("foo")
                .Compress()
                .Compress());

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("already", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "Compress")]
        [Fact(DisplayName = "Compress throws MessageCompressionException on compression exception")]
        public void Compress_Throws_MessageCompressionException_On_Zlib_Exception()
        {
            var builder = new MessageBuilder();
            var ex = Record.Exception(() => builder.InvokeMethod("Compress", BindingFlags.NonPublic | BindingFlags.Instance, null, null));

            Assert.NotNull(ex);
            Assert.NotNull(ex.InnerException);
            Assert.NotNull(ex.InnerException.InnerException);
            Assert.IsType<MessageCompressionException>(ex.InnerException.InnerException);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes throws InvalidOperationException when payload has been compressed")]
        public void WriteBytes_Throws_When_Payload_Has_Been_Compressed()
        {
            var builder = new MessageBuilder();
            builder.WriteCode(MessageCode.Peer.BrowseRequest);
            builder.WriteString("foo");
            builder.Compress();

            var ex = Record.Exception(() => builder.WriteBytes(new byte[] { 0x0 }));

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes throws ArgumentNullException given null byte array")]
        public void WriteBytes_Throws_Given_Null_Byte_Array()
        {
            var builder = new MessageBuilder();

            var ex = Record.Exception(() => builder.WriteBytes(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes writes given bytes")]
        public void WriteBytes_Writes_Given_Bytes()
        {
            var bytes = new byte[] { 0x0, 0x1, 0x2 };

            var builder = new MessageBuilder();
            builder.WriteBytes(bytes);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Equal(3, payload.Count);
            Assert.Equal(bytes.ToList(), payload);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteBytes appends given bytes")]
        public void WriteBytes_Appends_Given_Bytes()
        {
            var bytes = new byte[] { 0x0, 0x1, 0x2 };

            var builder = new MessageBuilder();
            builder.WriteBytes(bytes);

            var payload1 = builder.GetProperty<List<byte>>("PayloadBytes").ToList();

            builder.WriteBytes(bytes);

            var payload2 = builder.GetProperty<List<byte>>("PayloadBytes").ToList();
            var bytes2 = bytes.ToList();
            bytes2.AddRange(bytes);

            Assert.Equal(3, payload1.Count);
            Assert.Equal(bytes.ToList(), payload1);
            Assert.Equal(6, payload2.Count);
            Assert.Equal(bytes2, payload2);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteByte writes given byte")]
        public void WriteByte_Writes_Given_Byte()
        {
            var data = (byte)0x0;

            var builder = new MessageBuilder();
            builder.WriteByte(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Single(payload);
            Assert.Equal(new[] { data }.ToList(), payload);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteInteger writes given int")]
        public void WriteInteger_Writes_Given_Int()
        {
            var data = new Random().Next();

            var builder = new MessageBuilder();
            builder.WriteInteger(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Equal(4, payload.Count);
            Assert.Equal(BitConverter.GetBytes(data).ToList(), payload);
        }

        [Trait("Category", "Write")]
        [Fact(DisplayName = "WriteLong writes given long")]
        public void WriteLong_Writes_Given_Long()
        {
            var data = (long)new Random().Next();

            var builder = new MessageBuilder();
            builder.WriteLong(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            Assert.Equal(8, payload.Count);
            Assert.Equal(BitConverter.GetBytes(data).ToList(), payload);
        }

        [Trait("Category", "WriteBytes")]
        [Fact(DisplayName = "WriteString writes given string prepended with length")]
        public void WriteString_Writes_Given_String_Prepended_With_Length()
        {
            var data = Guid.NewGuid().ToString();

            var builder = new MessageBuilder();
            builder.WriteString(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            var expectedBytes = new List<byte>();
            expectedBytes.AddRange(BitConverter.GetBytes(data.Length));
            expectedBytes.AddRange(Encoding.UTF8.GetBytes(data));

            Assert.Equal(expectedBytes.Count, payload.Count);
            Assert.Equal(expectedBytes, payload);
        }

        [Trait("Category", "WriteBytes")]
        [Fact(DisplayName = "WriteString prefers ISO-8859-1")]
        public void WriteString_Prefers_ISO()
        {
            var data = "foo";

            var builder = new MessageBuilder();
            builder.WriteString(data);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            var expectedBytes = new List<byte>();
            expectedBytes.AddRange(BitConverter.GetBytes(data.Length));
            expectedBytes.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(data));

            Assert.Equal(expectedBytes.Count, payload.Count);
            Assert.Equal(expectedBytes, payload);
        }

        [Trait("Category", "WriteBytes")]
        [InlineData("UTF-8")]
        [InlineData("ISO-8859-1")]
        [Theory(DisplayName = "WriteString obeys specified encoding")]
        public void WriteString_Obeys_Specified_Encoding(string encoding)
        {
            var data = "a";

            var builder = new MessageBuilder();
            builder.WriteString(data, new CharacterEncoding(encoding));

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            var expectedBytes = new List<byte>();
            var encodedBytes = Encoding.GetEncoding(encoding).GetBytes(data);
            expectedBytes.AddRange(BitConverter.GetBytes(encodedBytes.Length));
            expectedBytes.AddRange(encodedBytes);

            Assert.Equal(expectedBytes.Count, payload.Count);
            Assert.Equal(expectedBytes, payload);
        }

        [Trait("Category", "WriteBytes")]
        [Fact(DisplayName = "WriteString writes UTF8 if ISO-8859-1 is not supported")]
        public void WriteString_Writes_UTF8_If_ISO_Is_Not_Supported()
        {
            var data = "බ";

            var builder = new MessageBuilder();
            builder.WriteString(data, CharacterEncoding.ISO88591);

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");

            var expectedBytes = Encoding.UTF8.GetBytes(data);

            Assert.Equal(4 + expectedBytes.Length, payload.Count);
            Assert.Equal(expectedBytes, payload.Skip(4));
        }

        [Trait("Category", "WriteBytes")]
        [Fact(DisplayName = "WriteString writes correct length for strings containing UTF16")]
        public void WriteString_Writes_Correct_Length_For_Strings_Containing_UTF16()
        {
            var builder = new MessageBuilder();
            builder.WriteString("ǔ");

            var payload = builder.GetProperty<List<byte>>("PayloadBytes");
            var expectedBytes = Encoding.UTF8.GetBytes("ǔ");

            Assert.Equal(6, payload.Count);
            Assert.Equal(expectedBytes, payload.Skip(4).Take(2));
        }
    }
}
