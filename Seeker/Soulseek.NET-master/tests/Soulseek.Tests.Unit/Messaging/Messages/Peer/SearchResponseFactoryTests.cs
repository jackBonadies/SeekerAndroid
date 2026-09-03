// <copyright file="SearchResponseFactoryTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Compression;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class SearchResponseFactoryTests
    {
        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageException on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .Build();

            var ex = Record.Exception(() => SearchResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageCompressionException on uncompressed payload")]
        public void Parse_Throws_MessageCompressionException_On_Uncompressed_Payload()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2, 0x3 })
                .Build();

            var ex = Record.Exception(() => SearchResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageCompressionException>(ex);
            Assert.IsType<ZStreamException>(ex.InnerException);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("foo")
                .Compress()
                .Build();

            var ex = Record.Exception(() => SearchResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Fact(DisplayName = "Parse throws MessageReadException on file count mismatch")]
        public void Parse_Throws_MessageReadException_On_File_Count_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("foo")
                .WriteInteger(0)
                .WriteInteger(20) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(0x20) // code
                .WriteString("filename2") // filename
                .WriteLong(30) // size
                .WriteString("ext2") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(40) // attribute[0].value
                .WriteByte(0)
                .WriteInteger(0)
                .WriteLong(0)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var ex = Record.Exception(() => SearchResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse returns expected data"), AutoData]
        public void Parse_Returns_Expected_Data(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte((byte)(hasFreeUploadSlot ? 1 : 0))
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponseFactory.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(1, r.FileCount);
            Assert.Equal(hasFreeUploadSlot, r.HasFreeUploadSlot);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);

            Assert.Single(r.Files);

            var file = r.Files.ToList()[0];

            Assert.Equal(0x2, file.Code);
            Assert.Equal("filename", file.Filename);
            Assert.Equal(3, file.Size);
            Assert.Equal("ext", file.Extension);
            Assert.Equal(1, file.AttributeCount);
            Assert.Single(file.Attributes);
            Assert.Equal(FileAttributeType.BitDepth, file.Attributes.ToList()[0].Type);
            Assert.Equal(4, file.Attributes.ToList()[0].Value);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles legacy responses with 4 byte queue length"), AutoData]
        public void Parse_Handles_Legacy_Responses_With_4_Byte_Queue_Length(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte((byte)(hasFreeUploadSlot ? 1 : 0))
                .WriteInteger(uploadSpeed)
                .WriteInteger(queueLength)
                .Compress()
                .Build();

            var r = SearchResponseFactory.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(1, r.FileCount);
            Assert.Equal(hasFreeUploadSlot, r.HasFreeUploadSlot);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);

            Assert.Single(r.Files);

            var file = r.Files.ToList()[0];

            Assert.Equal(0x2, file.Code);
            Assert.Equal("filename", file.Filename);
            Assert.Equal(3, file.Size);
            Assert.Equal("ext", file.Extension);
            Assert.Equal(1, file.AttributeCount);
            Assert.Single(file.Attributes);
            Assert.Equal(FileAttributeType.BitDepth, file.Attributes.ToList()[0].Type);
            Assert.Equal(4, file.Attributes.ToList()[0].Value);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles empty responses"), AutoData]
        public void Parse_Handles_Empty_Responses(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(0) // file count
                .WriteByte((byte)(hasFreeUploadSlot ? 1 : 0))
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponseFactory.FromByteArray(msg);

            Assert.Equal(username, r.Username);
            Assert.Equal(token, r.Token);
            Assert.Equal(0, r.FileCount);
            Assert.Equal(hasFreeUploadSlot, r.HasFreeUploadSlot);
            Assert.Equal(uploadSpeed, r.UploadSpeed);
            Assert.Equal(queueLength, r.QueueLength);

            Assert.Empty(r.Files);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles multiple files"), AutoData]
        public void Parse_Handles_Multiple_Files(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(2) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(0x20) // code
                .WriteString("filename2") // filename
                .WriteLong(30) // size
                .WriteString("ext2") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(40) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponseFactory.FromByteArray(msg);

            Assert.Equal(2, r.Files.Count);

            var file = r.Files.ToList();

            Assert.Equal(0x2, file[0].Code);
            Assert.Equal("filename", file[0].Filename);
            Assert.Equal(3, file[0].Size);
            Assert.Equal("ext", file[0].Extension);
            Assert.Equal(1, file[0].AttributeCount);
            Assert.Single(file[0].Attributes);
            Assert.Equal(FileAttributeType.BitDepth, file[0].Attributes.ToList()[0].Type);
            Assert.Equal(4, file[0].Attributes.ToList()[0].Value);

            Assert.Equal(0x20, file[1].Code);
            Assert.Equal("filename2", file[1].Filename);
            Assert.Equal(30, file[1].Size);
            Assert.Equal("ext2", file[1].Extension);
            Assert.Equal(1, file[1].AttributeCount);
            Assert.Single(file[1].Attributes);
            Assert.Equal(FileAttributeType.BitRate, file[1].Attributes.ToList()[0].Type);
            Assert.Equal(40, file[1].Attributes.ToList()[0].Value);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles locked files"), AutoData]
        public void Parse_Handles_Locked_Files(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(2) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(0x20) // code
                .WriteString("filename2") // filename
                .WriteLong(30) // size
                .WriteString("ext2") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(40) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteInteger(1) // locked file count
                .WriteByte(0x30) // code
                .WriteString("filename3") // filename
                .WriteLong(40) // size
                .WriteString("ext3") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(50) // attribute[0].value
                .Compress()
                .Build();

            var r = SearchResponseFactory.FromByteArray(msg);

            Assert.Equal(2, r.Files.Count);

            var file = r.Files.ToList();

            Assert.Equal(0x2, file[0].Code);
            Assert.Equal("filename", file[0].Filename);
            Assert.Equal(3, file[0].Size);
            Assert.Equal("ext", file[0].Extension);
            Assert.Equal(1, file[0].AttributeCount);
            Assert.Single(file[0].Attributes);
            Assert.Equal(FileAttributeType.BitDepth, file[0].Attributes.ToList()[0].Type);
            Assert.Equal(4, file[0].Attributes.ToList()[0].Value);

            Assert.Equal(0x20, file[1].Code);
            Assert.Equal("filename2", file[1].Filename);
            Assert.Equal(30, file[1].Size);
            Assert.Equal("ext2", file[1].Extension);
            Assert.Equal(1, file[1].AttributeCount);
            Assert.Single(file[1].Attributes);
            Assert.Equal(FileAttributeType.BitRate, file[1].Attributes.ToList()[0].Type);
            Assert.Equal(40, file[1].Attributes.ToList()[0].Value);

            var locked = r.LockedFiles.ToList();

            Assert.Equal(0x30, locked[0].Code);
            Assert.Equal("filename3", locked[0].Filename);
            Assert.Equal(40, locked[0].Size);
            Assert.Equal("ext3", locked[0].Extension);
            Assert.Equal(1, locked[0].AttributeCount);
            Assert.Single(locked[0].Attributes);
            Assert.Equal(FileAttributeType.BitRate, locked[0].Attributes.ToList()[0].Type);
            Assert.Equal(50, locked[0].Attributes.ToList()[0].Value);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles empty attributes"), AutoData]
        public void Parse_Handles_Empty_Attributes(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(0) // attribute count
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponseFactory.FromByteArray(msg);

            Assert.Single(r.Files);
            Assert.Empty(r.Files.ToList()[0].Attributes);
        }

        [Trait("Category", "Parse")]
        [Theory(DisplayName = "Parse handles multiple attributes"), AutoData]
        public void Parse_Handles_Multiple_Attributes(string username, int token, byte freeUploadSlots, int uploadSpeed, long queueLength)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(2) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteInteger((int)FileAttributeType.BitRate) // attribute[0].type
                .WriteInteger(5) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var r = SearchResponseFactory.FromByteArray(msg);

            Assert.Single(r.Files);

            var file = r.Files.ToList()[0];

            Assert.Equal(FileAttributeType.BitDepth, file.Attributes.ToList()[0].Type);
            Assert.Equal(4, file.Attributes.ToList()[0].Value);

            Assert.Equal(FileAttributeType.BitRate, file.Attributes.ToList()[1].Type);
            Assert.Equal(5, file.Attributes.ToList()[1].Value);
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns expected data"), AutoData]
        public void ToByteArray_Returns_Expected_Data(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
        {
            var list = new List<File>()
            {
                new File(1, "1", 1, ".1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, ".2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var s = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, list);
            var m = s.ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(m);
            reader.Decompress();
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.SearchResponse, code);

            Assert.Equal(username, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(2, reader.ReadInteger());

            // file 1
            Assert.Equal(1, reader.ReadByte()); // code
            Assert.Equal("1", reader.ReadString()); // name
            Assert.Equal(1, reader.ReadLong()); // length
            Assert.Equal(".1", reader.ReadString()); // ext
            Assert.Equal(1, reader.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitDepth, (FileAttributeType)reader.ReadInteger());
            Assert.Equal(1, reader.ReadInteger());

            // file 2
            Assert.Equal(2, reader.ReadByte()); // code
            Assert.Equal("2", reader.ReadString()); // name
            Assert.Equal(2, reader.ReadLong()); // length
            Assert.Equal(".2", reader.ReadString()); // ext
            Assert.Equal(1, reader.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitRate, (FileAttributeType)reader.ReadInteger());
            Assert.Equal(2, reader.ReadInteger());
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns expected data when HasFreeUploadSlot = false"), AutoData]
        public void ToByteArray_Returns_Expected_Data_When_HasFreeUploadSlot_False(string username, int token, int uploadSpeed, int queueLength)
        {
            var list = new List<File>();

            var s = new SearchResponse(username, token, hasFreeUploadSlot: false, uploadSpeed, queueLength, list);
            var m = s.ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(m);
            reader.Decompress();
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.SearchResponse, code);

            Assert.Equal(username, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(0, reader.ReadInteger());

            Assert.Equal(0, reader.ReadByte());
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns expected data when HasFreeUploadSlot = true"), AutoData]
        public void ToByteArray_Returns_Expected_Data_When_HasFreeUploadSlot_True(string username, int token, int uploadSpeed, int queueLength)
        {
            var list = new List<File>();

            var s = new SearchResponse(username, token, hasFreeUploadSlot: true, uploadSpeed, queueLength, list);
            var m = s.ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(m);
            reader.Decompress();
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.SearchResponse, code);

            Assert.Equal(username, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(0, reader.ReadInteger());

            Assert.Equal(1, reader.ReadByte());
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray handles locked files"), AutoData]
        public void ToByteArray_Handles_Locked_Files(string username, int token, bool freeUploadSlots, int uploadSpeed, int queueLength)
        {
            var list = new List<File>()
            {
                new File(1, "1", 1, ".1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
            };

            var locked = new List<File>()
            {
                new File(2, "2", 2, ".2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var s = new SearchResponse(username, token, hasFreeUploadSlot: freeUploadSlots, uploadSpeed, queueLength, list, locked);
            var m = s.ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(m);
            reader.Decompress();
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.SearchResponse, code);

            Assert.Equal(username, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(1, reader.ReadInteger());

            // file 1
            Assert.Equal(1, reader.ReadByte()); // code
            Assert.Equal("1", reader.ReadString()); // name
            Assert.Equal(1, reader.ReadLong()); // length
            Assert.Equal(".1", reader.ReadString()); // ext
            Assert.Equal(1, reader.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitDepth, (FileAttributeType)reader.ReadInteger());
            Assert.Equal(1, reader.ReadInteger());

            Assert.Equal(freeUploadSlots, reader.ReadByte() > 0); // code
            Assert.Equal(uploadSpeed, reader.ReadInteger()); // upload speed
            Assert.Equal(queueLength, reader.ReadLong()); // queue length

            // locked file count
            Assert.Equal(1, reader.ReadInteger());

            // file 2
            Assert.Equal(2, reader.ReadByte()); // code
            Assert.Equal("2", reader.ReadString()); // name
            Assert.Equal(2, reader.ReadLong()); // length
            Assert.Equal(".2", reader.ReadString()); // ext
            Assert.Equal(1, reader.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitRate, (FileAttributeType)reader.ReadInteger());
            Assert.Equal(2, reader.ReadInteger());
        }
    }
}
