// <copyright file="FolderContentsResponseTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Compression;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class FolderContentsResponseTests
    {
        private Random Random { get; } = new Random();

        [Trait("Category", "Instantiation")]
        [Trait("Response", "FolderContentsResponse")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Given_Data(int token, Directory dir)
        {
            var dirList = new List<Directory>() { dir };
            var a = new FolderContentsResponse(token, directoryName: dir.Name, directories: dirList);

            Assert.Equal(token, a.Token);
            Assert.Equal(dir.Name, a.DirectoryName);
            Assert.Equal(dirList, a.Directories);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "FolderContentsResponse")]
        [Fact(DisplayName = "Parse throws MessageException on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .Build();

            var ex = Record.Exception(() => FolderContentsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "FolderContentsResponse")]
        [Fact(DisplayName = "Parse throws MessageCompressionException on uncompressed payload")]
        public void Parse_Throws_MessageCompressionException_On_Uncompressed_Payload()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2, 0x3 })
                .Build();

            var ex = Record.Exception(() => FolderContentsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageCompressionException>(ex);
            Assert.IsType<ZStreamException>(ex.InnerException);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "FolderContentsResponse")]
        [Theory(DisplayName = "Parse returns empty response given empty message"), AutoData]
        public void Parse_Returns_Empty_Response_Given_Empty_Message(int token, string dirname)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(token)
                .WriteString(dirname)
                .WriteInteger(1)
                .WriteString(dirname)
                .WriteInteger(0)
                .Compress()
                .Build();

            FolderContentsResponse r = default;

            var ex = Record.Exception(() => r = FolderContentsResponse.FromByteArray(msg));

            Assert.Null(ex);
            Assert.Equal(token, r.Token);
            Assert.Equal(dirname, r.Directories.First().Name);
            Assert.Equal(0, r.Directories.First().FileCount);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "FolderContentsResponse")]
        [Theory(DisplayName = "Parse throws MessageReadException on missing data"), AutoData]
        public void Parse_Throws_MessageReadException_On_Missing_Data(int token, string dirname)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(token)
                .WriteString(dirname)
                .WriteInteger(1)
                .WriteString(dirname)
                .Compress() // count is missing
                .Build();

            FolderContentsResponse r = default;
            var ex = Record.Exception(() => r = FolderContentsResponse.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "FolderContentsResponse")]
        [Theory(DisplayName = "Parse handles files with no attributes"), AutoData]
        public void Parse_Handles_Files_With_No_Attributes(int token, string dirname)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(token)
                .WriteString(dirname)
                .WriteInteger(1)
                .WriteString(dirname)
                .WriteInteger(1) // first directory file count
                .WriteByte(0x0) // file code
                .WriteString("foo") // name
                .WriteLong(12) // size
                .WriteString("bar") // extension
                .WriteInteger(0) // attribute count
                .Compress()
                .Build();

            FolderContentsResponse r = default;

            var ex = Record.Exception(() => r = FolderContentsResponse.FromByteArray(msg));

            Assert.Null(ex);

            Assert.Equal(dirname, r.Directories.First().Name);
            Assert.Equal(1, r.Directories.First().FileCount);
            Assert.Single(r.Directories.First().Files);

            var f = r.Directories.First().Files.ToList();

            Assert.Equal(0x0, f[0].Code);
            Assert.Equal("foo", f[0].Filename);
            Assert.Equal(12, f[0].Size);
            Assert.Equal("bar", f[0].Extension);
            Assert.Equal(0, f[0].AttributeCount);
            Assert.Empty(f[0].Attributes);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "FolderContentsResponse")]
        [Theory(DisplayName = "Parse handles a complete response"), AutoData]
        public void Parse_Handles_A_Complete_Response(int token)
        {
            var dir1 = GetRandomDirectory(2);
            var dir2 = GetRandomDirectory(1);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(token)
                .WriteString(dir1.Name) // name of first directory must go first
                .WriteInteger(2); // 2 directories

            BuildDirectory(builder, dir1);
            BuildDirectory(builder, dir2);

            var msg = builder
                .Compress()
                .Build();

            FolderContentsResponse r = default;

            var ex = Record.Exception(() => r = FolderContentsResponse.FromByteArray(msg));

            Assert.Null(ex);

            var d1 = r.Directories.First();

            Assert.Equal(dir1.Name, d1.Name);
            Assert.Equal(dir1.FileCount, d1.FileCount);

            var files1 = d1.Files.ToList();
            var msgFiles1 = d1.Files.ToList();

            for (int j = 0; j < d1.FileCount; j++)
            {
                Assert.Equal(files1[j].Code, msgFiles1[j].Code);
                Assert.Equal(files1[j].Filename, msgFiles1[j].Filename);
                Assert.Equal(files1[j].Size, msgFiles1[j].Size);
                Assert.Equal(files1[j].Extension, msgFiles1[j].Extension);
                Assert.Equal(files1[j].AttributeCount, msgFiles1[j].AttributeCount);

                var attr = files1[j].Attributes.ToList();
                var msgAttr = files1[j].Attributes.ToList();

                for (int k = 0; k < msgFiles1[j].AttributeCount; k++)
                {
                    Assert.Equal(attr[k].Type, msgAttr[k].Type);
                    Assert.Equal(attr[k].Value, msgAttr[k].Value);
                }
            }

            var d2 = r.Directories.Last();

            Assert.Equal(dir2.Name, d2.Name);
            Assert.Equal(dir2.FileCount, d2.FileCount);

            var files2 = d2.Files.ToList();
            var msgFiles2 = d2.Files.ToList();

            for (int j = 0; j < d2.FileCount; j++)
            {
                Assert.Equal(files2[j].Code, msgFiles2[j].Code);
                Assert.Equal(files2[j].Filename, msgFiles2[j].Filename);
                Assert.Equal(files2[j].Size, msgFiles2[j].Size);
                Assert.Equal(files2[j].Extension, msgFiles2[j].Extension);
                Assert.Equal(files2[j].AttributeCount, msgFiles2[j].AttributeCount);

                var attr = files2[j].Attributes.ToList();
                var msgAttr = files2[j].Attributes.ToList();

                for (int k = 0; k < msgFiles2[j].AttributeCount; k++)
                {
                    Assert.Equal(attr[k].Type, msgAttr[k].Type);
                    Assert.Equal(attr[k].Value, msgAttr[k].Value);
                }
            }
        }

        [Trait("Category", "ToByteArray")]
        [Theory(DisplayName = "ToByteArray returns the expected data"), AutoData]
        public void ToByteArray_Returns_Expected_Data(int token, string dirname1, string dirname2)
        {
            var list1 = new List<File>()
            {
                new File(1, "1", 1, ".1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, ".2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var list2 = new List<File>()
            {
                new File(3, "3", 3, ".3", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 3) }),
                new File(4, "4", 4, ".4", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 4) }),
            };

            var dir1 = new Directory(dirname1, list1); // "root" directory
            var dir2 = new Directory(dirname1 + "/" + dirname2, list2); // subdirectory

            var r = new FolderContentsResponse(token, dirname1, new List<Directory>() { dir1, dir2 });

            var bytes = r.ToByteArray();

            var m = new MessageReader<MessageCode.Peer>(bytes);
            m.Decompress();

            Assert.Equal(MessageCode.Peer.FolderContentsResponse, m.ReadCode());
            Assert.Equal(token, m.ReadInteger());

            Assert.Equal(dirname1, m.ReadString()); // "root" directory
            Assert.Equal(2, m.ReadInteger()); // count of directories
            Assert.Equal(dirname1, m.ReadString()); // name of first directory, which is the "root"
            Assert.Equal(dir1.FileCount, m.ReadInteger());

            // file 1
            Assert.Equal(1, m.ReadByte()); // code
            Assert.Equal("1", m.ReadString()); // name
            Assert.Equal(1, m.ReadLong()); // length
            Assert.Equal(".1", m.ReadString()); // ext
            Assert.Equal(1, m.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitDepth, (FileAttributeType)m.ReadInteger());
            Assert.Equal(1, m.ReadInteger());

            // file 2
            Assert.Equal(2, m.ReadByte()); // code
            Assert.Equal("2", m.ReadString()); // name
            Assert.Equal(2, m.ReadLong()); // length
            Assert.Equal(".2", m.ReadString()); // ext
            Assert.Equal(1, m.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitRate, (FileAttributeType)m.ReadInteger());
            Assert.Equal(2, m.ReadInteger());

            Assert.Equal(dirname1 + "/" + dirname2, m.ReadString()); // name of second directory, which is subdirectory
            Assert.Equal(dir2.FileCount, m.ReadInteger());

            // file 3
            Assert.Equal(3, m.ReadByte()); // code
            Assert.Equal("3", m.ReadString()); // name
            Assert.Equal(3, m.ReadLong()); // length
            Assert.Equal(".3", m.ReadString()); // ext
            Assert.Equal(1, m.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitDepth, (FileAttributeType)m.ReadInteger());
            Assert.Equal(3, m.ReadInteger());

            // file 4
            Assert.Equal(4, m.ReadByte()); // code
            Assert.Equal("4", m.ReadString()); // name
            Assert.Equal(4, m.ReadLong()); // length
            Assert.Equal(".4", m.ReadString()); // ext
            Assert.Equal(1, m.ReadInteger()); // attribute count

            Assert.Equal(FileAttributeType.BitRate, (FileAttributeType)m.ReadInteger());
            Assert.Equal(4, m.ReadInteger());
        }

        private static MessageBuilder BuildDirectory(MessageBuilder builder, Directory dir)
        {
            builder
                .WriteString(dir.Name)
                .WriteInteger(dir.FileCount);

            foreach (var file in dir.Files)
            {
                builder
                    .WriteByte((byte)file.Code)
                    .WriteString(file.Filename)
                    .WriteLong(file.Size)
                    .WriteString(file.Extension)
                    .WriteInteger(file.AttributeCount);

                foreach (var attribute in file.Attributes)
                {
                    builder
                        .WriteInteger((int)attribute.Type)
                        .WriteInteger(attribute.Value);
                }
            }

            return builder;
        }

        private FileAttribute GetRandomFileAttribute()
        {
            return new FileAttribute(
                type: (FileAttributeType)Random.Next(6),
                value: Random.Next());
        }

        private File GetRandomFile(int attributeCount)
        {
            var attributeList = new List<FileAttribute>();

            for (int i = 0; i < attributeCount; i++)
            {
                attributeList.Add(GetRandomFileAttribute());
            }

            return new File(
                code: Random.Next(2),
                filename: Guid.NewGuid().ToString(),
                size: Random.Next(),
                extension: Guid.NewGuid().ToString(),
                attributeList: attributeList);
        }

        private Directory GetRandomDirectory(int fileCount)
        {
            var fileList = new List<File>();

            for (int i = 0; i < fileCount; i++)
            {
                fileList.Add(GetRandomFile(Random.Next(5)));
            }

            return new Directory(
                name: Guid.NewGuid().ToString(),
                fileList: fileList);
        }
    }
}
