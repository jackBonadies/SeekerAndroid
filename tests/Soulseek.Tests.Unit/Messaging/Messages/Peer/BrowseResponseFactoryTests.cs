// <copyright file="BrowseResponseFactoryTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;
    using Soulseek.Messaging.Compression;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class BrowseResponseFactoryTests
    {
        private Random Random { get; } = new Random();

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse throws MessageException on code mismatch")]
        public void Parse_Throws_MessageException_On_Code_Mismatch()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .Build();

            var ex = Record.Exception(() => BrowseResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageException>(ex);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse throws MessageCompressionException on uncompressed payload")]
        public void Parse_Throws_MessageCompressionException_On_Uncompressed_Payload()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteBytes(new byte[] { 0x0, 0x1, 0x2, 0x3 })
                .Build();

            var ex = Record.Exception(() => BrowseResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageCompressionException>(ex);
            Assert.IsType<ZStreamException>(ex.InnerException);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse returns empty response given empty message")]
        public void Parse_Returns_Empty_Response_Given_Empty_Message()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(0)
                .Compress()
                .Build();

            BrowseResponse r = default;

            var ex = Record.Exception(() => r = BrowseResponseFactory.FromByteArray(msg));

            Assert.Null(ex);
            Assert.Equal(0, r.DirectoryCount);
            Assert.Empty(r.Directories);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse handles empty directory")]
        public void Parse_Handles_Empty_Directory()
        {
            var name = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(name) // first directory name
                .WriteInteger(0) // first directory file count
                .Compress()
                .Build();

            BrowseResponse r = default;

            var ex = Record.Exception(() => r = BrowseResponseFactory.FromByteArray(msg));

            Assert.Null(ex);
            Assert.Equal(1, r.DirectoryCount);
            Assert.Single(r.Directories);

            var d = r.Directories.ToList();

            Assert.Equal(name, d[0].Name);
            Assert.Equal(0, d[0].FileCount);
            Assert.Empty(d[0].Files);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse throws MessageReadException on missing data")]
        public void Parse_Throws_MessageReadException_On_Missing_Data()
        {
            var name = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(name) // first directory name
                .Compress() // count is missing
                .Build();

            BrowseResponse r = default;
            var ex = Record.Exception(() => r = BrowseResponseFactory.FromByteArray(msg));

            Assert.NotNull(ex);
            Assert.IsType<MessageReadException>(ex);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse handles files with no attributes")]
        public void Parse_Handles_Files_With_No_Attributes()
        {
            var name = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(name) // first directory name
                .WriteInteger(1) // first directory file count
                .WriteByte(0x0) // file code
                .WriteString("foo") // name
                .WriteLong(12) // size
                .WriteString("bar") // extension
                .WriteInteger(0) // attribute count
                .Compress()
                .Build();

            BrowseResponse r = default;

            var ex = Record.Exception(() => r = BrowseResponseFactory.FromByteArray(msg));

            Assert.Null(ex);
            Assert.Equal(1, r.DirectoryCount);
            Assert.Single(r.Directories);

            var d = r.Directories.ToList();

            Assert.Equal(name, d[0].Name);
            Assert.Equal(1, d[0].FileCount);
            Assert.Single(d[0].Files);

            var f = d[0].Files.ToList();

            Assert.Equal(0x0, f[0].Code);
            Assert.Equal("foo", f[0].Filename);
            Assert.Equal(12, f[0].Size);
            Assert.Equal("bar", f[0].Extension);
            Assert.Equal(0, f[0].AttributeCount);
            Assert.Empty(f[0].Attributes);
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse handles a response with only locked files")]
        public void Parse_Handles_Response_With_Only_Locked_Files()
        {
            var dirs = new List<Directory>();

            for (int i = 0; i < 5; i++)
            {
                dirs.Add(GetRandomDirectory(i));
            }

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(0) // 0 unlocked files
                .WriteInteger(0) // unknown
                .WriteInteger(dirs.Count);

            foreach (var dir in dirs)
            {
                BuildDirectory(builder, dir);
            }

            var msg = builder
                .Compress()
                .Build();

            BrowseResponse r = default;

            var ex = Record.Exception(() => r = BrowseResponseFactory.FromByteArray(msg));

            Assert.Null(ex);

            Assert.Equal(0, r.DirectoryCount);

            Assert.Equal(dirs.Count, r.LockedDirectoryCount);
            Assert.Equal(dirs.Count, r.LockedDirectories.Count);

            var msgDirs = r.LockedDirectories.ToList();

            for (int i = 0; i < msgDirs.Count; i++)
            {
                Assert.Equal(dirs[i].Name, msgDirs[i].Name);
                Assert.Equal(dirs[i].FileCount, msgDirs[i].FileCount);

                var files = dirs[i].Files.ToList();
                var msgFiles = msgDirs[i].Files.ToList();

                for (int j = 0; j < msgDirs[i].FileCount; j++)
                {
                    Assert.Equal(files[j].Code, msgFiles[j].Code);
                    Assert.Equal(files[j].Filename, msgFiles[j].Filename);
                    Assert.Equal(files[j].Size, msgFiles[j].Size);
                    Assert.Equal(files[j].Extension, msgFiles[j].Extension);
                    Assert.Equal(files[j].AttributeCount, msgFiles[j].AttributeCount);

                    var attr = files[j].Attributes.ToList();
                    var msgAttr = files[j].Attributes.ToList();

                    for (int k = 0; k < msgFiles[j].AttributeCount; k++)
                    {
                        Assert.Equal(attr[k].Type, msgAttr[k].Type);
                        Assert.Equal(attr[k].Value, msgAttr[k].Value);
                    }
                }
            }
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Fact(DisplayName = "Parse handles a complete response")]
        public void Parse_Handles_A_Complete_Response()
        {
            var dirs = new List<Directory>();

            for (int i = 0; i < 5; i++)
            {
                dirs.Add(GetRandomDirectory(i));
            }

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(dirs.Count);

            foreach (var dir in dirs)
            {
                BuildDirectory(builder, dir);
            }

            builder
                .WriteInteger(0) // unknown
                .WriteInteger(dirs.Count);

            foreach (var dir in dirs)
            {
                BuildDirectory(builder, dir);
            }

            var msg = builder
                .Compress()
                .Build();

            BrowseResponse r = default;

            var ex = Record.Exception(() => r = BrowseResponseFactory.FromByteArray(msg));

            Assert.Null(ex);
            Assert.Equal(dirs.Count, r.DirectoryCount);
            Assert.Equal(dirs.Count, r.Directories.Count);
            Assert.Equal(dirs.Count, r.LockedDirectoryCount);
            Assert.Equal(dirs.Count, r.LockedDirectories.Count);

            var msgDirs = r.Directories.ToList();

            for (int i = 0; i < msgDirs.Count; i++)
            {
                Assert.Equal(dirs[i].Name, msgDirs[i].Name);
                Assert.Equal(dirs[i].FileCount, msgDirs[i].FileCount);

                var files = dirs[i].Files.ToList();
                var msgFiles = msgDirs[i].Files.ToList();

                for (int j = 0; j < msgDirs[i].FileCount; j++)
                {
                    Assert.Equal(files[j].Code, msgFiles[j].Code);
                    Assert.Equal(files[j].Filename, msgFiles[j].Filename);
                    Assert.Equal(files[j].Size, msgFiles[j].Size);
                    Assert.Equal(files[j].Extension, msgFiles[j].Extension);
                    Assert.Equal(files[j].AttributeCount, msgFiles[j].AttributeCount);

                    var attr = files[j].Attributes.ToList();
                    var msgAttr = files[j].Attributes.ToList();

                    for (int k = 0; k < msgFiles[j].AttributeCount; k++)
                    {
                        Assert.Equal(attr[k].Type, msgAttr[k].Type);
                        Assert.Equal(attr[k].Value, msgAttr[k].Value);
                    }
                }
            }

            msgDirs = r.LockedDirectories.ToList();

            for (int i = 0; i < msgDirs.Count; i++)
            {
                Assert.Equal(dirs[i].Name, msgDirs[i].Name);
                Assert.Equal(dirs[i].FileCount, msgDirs[i].FileCount);

                var files = dirs[i].Files.ToList();
                var msgFiles = msgDirs[i].Files.ToList();

                for (int j = 0; j < msgDirs[i].FileCount; j++)
                {
                    Assert.Equal(files[j].Code, msgFiles[j].Code);
                    Assert.Equal(files[j].Filename, msgFiles[j].Filename);
                    Assert.Equal(files[j].Size, msgFiles[j].Size);
                    Assert.Equal(files[j].Extension, msgFiles[j].Extension);
                    Assert.Equal(files[j].AttributeCount, msgFiles[j].AttributeCount);

                    var attr = files[j].Attributes.ToList();
                    var msgAttr = files[j].Attributes.ToList();

                    for (int k = 0; k < msgFiles[j].AttributeCount; k++)
                    {
                        Assert.Equal(attr[k].Type, msgAttr[k].Type);
                        Assert.Equal(attr[k].Value, msgAttr[k].Value);
                    }
                }
            }
        }

        [Trait("Category", "Parse")]
        [Trait("Response", "BrowseResponse")]
        [Theory(DisplayName = "Parse handles an overflowed file size from Soulseek NS")]
        [InlineData(-1, 4294967295)] // https://onlinetoolz.net/unsigned-signed
        [InlineData(42, 42)]
        [InlineData(-1180018327, 3114948969)]
        public void Parse_Handles_An_Overflowed_File_Size_From_Soulseek_NS(long given, long expected)
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString("dir") // first directory name
                .WriteInteger(1) // first directory file count
                .WriteByte(0x0) // file code
                .WriteString("foo") // name
                .WriteLong(given) // size
                .WriteString("bar") // extension
                .WriteInteger(0) // attribute count
                .Compress()
                .Build();

            BrowseResponse r = default;

            var ex = Record.Exception(() => r = BrowseResponseFactory.FromByteArray(msg));

            Assert.Null(ex);

            var d = r.Directories.ToList();
            var f = d[0].Files.ToList();

            Assert.Equal(expected, f[0].Size);
        }

        [Trait("Category", "ToByteArray")]
        [Fact(DisplayName = "ToByteArray returns the expected data")]
        public void ToByteArray_Returns_Expected_Data()
        {
            var list = new List<File>()
            {
                new File(1, "1", 1, ".1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, ".2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var dirs = new List<Directory>()
            {
                new Directory("dir1", list),
                new Directory("dir2", list),
            };

            var r = new BrowseResponse(dirs);

            var bytes = r.ToByteArray();

            var m = new MessageReader<MessageCode.Peer>(bytes);
            m.Decompress();

            Assert.Equal(MessageCode.Peer.BrowseResponse, m.ReadCode());
            Assert.Equal(2, m.ReadInteger());

            // dir 1
            Assert.Equal("dir1", m.ReadString());
            Assert.Equal(2, m.ReadInteger());

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
        }

        [Trait("Category", "ToByteArray")]
        [Fact(DisplayName = "ToByteArray returns the expected data when only locked files given")]
        public void ToByteArray_Returns_Expected_Data_When_Only_Locked_Files_Given()
        {
            var list = new List<File>()
            {
                new File(1, "1", 1, ".1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, ".2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var dirs = new List<Directory>()
            {
                new Directory("dir1", list),
            };

            var r = new BrowseResponse(lockedDirectoryList: dirs);

            var bytes = r.ToByteArray();

            var m = new MessageReader<MessageCode.Peer>(bytes);
            m.Decompress();

            Assert.Equal(MessageCode.Peer.BrowseResponse, m.ReadCode());
            Assert.Equal(0, m.ReadInteger());
            Assert.Equal(0, m.ReadInteger()); // unknown
            Assert.Equal(1, m.ReadInteger()); // locked directory count

            // dir 1
            Assert.Equal("dir1", m.ReadString());
            Assert.Equal(2, m.ReadInteger());

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
