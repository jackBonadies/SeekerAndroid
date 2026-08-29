// <copyright file="UploadAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class UploadAsyncTests
    {
        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_Stream_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, "filename", 1, (_) => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("username", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_File_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, "filename", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("username", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream throws ArgumentException given bad filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_Stream_Throws_ArgumentException_Given_Bad_Filename(string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", filename, 1, (_) => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("remoteFilename", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file throws ArgumentException given bad remote filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_File_Throws_ArgumentException_Given_Bad_Remote_Filename(string filename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", filename, Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("remoteFilename", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file throws ArgumentException given bad local filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_File_Throws_ArgumentException_Given_Bad_Local_Filename(string filename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "remote", filename));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("localFilename", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync file throws ArgumentException given missing local file")]
        public async Task UploadAsync_File_Throws_ArgumentException_Given_Missing_Local_File()
        {
            var ioMock = new Mock<IIOAdapter>();
            ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                .Returns(false);

            using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "remote", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<FileNotFoundException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream does not throw given zero size")]
        public async Task UploadAsync_Stream_Does_Not_Throw_Given_Zero_Size()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var zeroSizeArgument = 0;
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", zeroSizeArgument, (_) => Task.FromResult((Stream)stream)));

                // this only works because the check for connection state comes after the size guard clause
                // if that moves this test will break and/or not test the size guard
                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream throws ArgumentException bad size")]
        [InlineData(-1)]
        [InlineData(-12413)]
        public async Task UploadAsync_Stream_Throws_ArgumentException_Given_Bad_Size(long size)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", size, (_) => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("size", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream throws InvalidOperationException when not connected")]
        public async Task UploadAsync_Stream_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync file throws InvalidOperationException when not connected")]
        public async Task UploadAsync_File_Throws_InvalidOperationException_When_Not_Connected()
        {
            var ioMock = new Mock<IIOAdapter>();
            ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                .Returns(true);

            using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream throws ArgumentNullException given null stream factory")]
        public async Task UploadAsync_Stream_Throws_ArgumentNullException_Given_Null_Stream_Factory()
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, inputStreamFactory: null));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentNullException>(ex);
                Assert.Contains("stream factory is null", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream throws InvalidOperationException when not logged in")]
        public async Task UploadAsync_Stream_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync file throws InvalidOperationException when not logged in")]
        public async Task UploadAsync_File_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            var ioMock = new Mock<IIOAdapter>();
            ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                .Returns(true);

            using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync file throws IOException when file cant be read")]
        public async Task UploadAsync_File_Throws_IOException_When_File_Cant_Be_Read()
        {
            var ioMock = new Mock<IIOAdapter>();
            ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                .Returns(true);
            ioMock.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Throws(new IOException("foo"));

            using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<IOException>(ex);
                Assert.Contains("could not be opened for reading", ex.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.IsType<IOException>(ex.InnerException);
                Assert.Equal("foo", ex.InnerException.Message);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream throws DuplicateTokenException when token used")]
        public async Task UploadAsync_Stream_Throws_DuplicateTokenException_When_Token_Used()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(1, new TransferInternal(TransferDirection.Upload, "foo", "bar", 1));

                s.SetProperty("UploadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream), 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync file throws DuplicateTokenException when token used")]
        public async Task UploadAsync_File_Throws_DuplicateTokenException_When_Token_Used()
        {
            using (var testFile = new TestFile())
            {
                var ioMock = new Mock<IIOAdapter>();
                ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                    .Returns(true);
                ioMock.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(new FileStream(testFile.Path, FileMode.Open, FileAccess.Read));

                using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var queued = new ConcurrentDictionary<int, TransferInternal>();
                    queued.TryAdd(1, new TransferInternal(TransferDirection.Upload, "foo", "bar", 1));

                    s.SetProperty("UploadDictionary", queued);

                    var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", Guid.NewGuid().ToString(), 1));

                    Assert.NotNull(ex);
                    Assert.IsType<DuplicateTokenException>(ex);
                    Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream throws DuplicateTransferException when an existing Upload matches the username and filename"), AutoData]
        public async Task UploadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_The_Username_And_Filename(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                s.SetProperty("UploadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream throws DuplicateTransferException when an existing Upload matches a unique key"), AutoData]
        public async Task UploadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_A_Unique_Key(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);

                s.SetProperty("UniqueKeyDictionary", tracked);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream does not throw DuplicateTransferException when an existing Upload matches only the username"), AutoData]
        public async Task UploadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Username(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                s.SetProperty("UploadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename + "!", 1, (_) => Task.FromResult((Stream)stream), 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream does not throw DuplicateTransferException when an existing Upload matches only the filename"), AutoData]
        public async Task UploadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Filename(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                s.SetProperty("UploadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username + "!", filename, 1, (_) => Task.FromResult((Stream)stream), 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file throws DuplicateTransferException when an existing Upload matches the username and filename"), AutoData]
        public async Task UploadAsync_File_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_The_Username_And_Filename(string username, string filename, string localFilename)
        {
            using (var testFile = new TestFile())
            {
                var ioMock = new Mock<IIOAdapter>();
                ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                    .Returns(true);
                ioMock.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(new FileStream(testFile.Path, FileMode.Open, FileAccess.Read));

                using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var queued = new ConcurrentDictionary<int, TransferInternal>();
                    queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                    s.SetProperty("UploadDictionary", queued);

                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, localFilename, 1));

                    Assert.NotNull(ex);
                    Assert.IsType<DuplicateTransferException>(ex);
                    Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file throws DuplicateTransferException when an existing Upload matches a unique key"), AutoData]
        public async Task UploadAsync_File_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_A_Unique_Key(string username, string filename, string localFilename)
        {
            using (var testFile = new TestFile())
            {
                var ioMock = new Mock<IIOAdapter>();
                ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                    .Returns(true);
                ioMock.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(new FileStream(testFile.Path, FileMode.Open, FileAccess.Read));

                using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var tracked = new ConcurrentDictionary<string, bool>();
                    tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);

                    s.SetProperty("UniqueKeyDictionary", tracked);

                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, localFilename, 1));

                    Assert.NotNull(ex);
                    Assert.IsType<DuplicateTransferException>(ex);
                    Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
                }
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file does not throw DuplicateTransferException when an existing Upload matches only the username"), AutoData]
        public async Task UploadAsync_File_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Username(string username, string filename, string localFilename)
        {
            using (var testFile = new TestFile())
            {
                var ioMock = new Mock<IIOAdapter>();
                ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                    .Returns(true);
                ioMock.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(new FileStream(testFile.Path, FileMode.Open, FileAccess.Read));

                using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var queued = new ConcurrentDictionary<int, TransferInternal>();
                    queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                    s.SetProperty("UploadDictionary", queued);

                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename + "!", localFilename, 1));

                    Assert.NotNull(ex);
                    Assert.IsNotType<DuplicateTransferException>(ex);
                }
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file does not throw DuplicateTransferException when an existing Upload matches only the filename"), AutoData]
        public async Task UploadAsync_File_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Filename(string username, string filename, string localFilename)
        {
            using (var testFile = new TestFile())
            {
                var ioMock = new Mock<IIOAdapter>();
                ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                    .Returns(true);
                ioMock.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(new FileStream(testFile.Path, FileMode.Open, FileAccess.Read));

                using (var s = new SoulseekClient(ioAdapter: ioMock.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var queued = new ConcurrentDictionary<int, TransferInternal>();
                    queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                    s.SetProperty("UploadDictionary", queued);

                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username + "!", filename, localFilename, 1));

                    Assert.NotNull(ex);
                    Assert.IsNotType<DuplicateTransferException>(ex);
                }
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream uses given CancellationToken"), AutoData]
        public async Task UploadAsync_Stream_Uses_Given_CancellationToken(string username, string filename)
        {
            var cancellationToken = new CancellationToken(true);

            var conn = new Mock<IMessageConnection>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), cancellationToken: cancellationToken));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync file uses given CancellationToken"), AutoData]
        public async Task UploadAsync_File_Uses_Given_CancellationToken(string username, string filename, string localFilename)
        {
            var cancellationToken = new CancellationToken(true);

            var conn = new Mock<IMessageConnection>();

            using (var testFile = new TestFile())
            using (var fs1 = new FileStream(testFile.Path, FileMode.Open, FileAccess.Read))
            using (var fs2 = new FileStream(testFile.Path, FileMode.Open, FileAccess.Read))
            {
                var ioMock = new Mock<IIOAdapter>();
                ioMock.Setup(m => m.Exists(It.IsAny<string>()))
                    .Returns(true);
                ioMock.SetupSequence(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(fs1)
                    .Returns(fs2);
                ioMock.Setup(m => m.GetFileInfo(It.IsAny<string>()))
                    .Returns(new FileInfo(testFile.Path));

                using (var s = new SoulseekClient(serverConnection: conn.Object, ioAdapter: ioMock.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, localFilename, cancellationToken: cancellationToken));

                    Assert.NotNull(ex);
                    Assert.IsType<OperationCanceledException>(ex);
                }
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream throws TimeoutException on peer message connection timeout"), AutoData]
        public async Task UploadAsync_Stream_Throws_TimeoutException_On_Peer_Message_Connection_Timeout(IPEndPoint endpoint)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse("username", endpoint.Address, endpoint.Port)));

            var manager = new Mock<IPeerConnectionManager>();
            manager.Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<string>(), endpoint, It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: conn.Object, options: options, peerConnectionManager: manager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, (_) => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws UserOfflineException when user offline"), AutoData]
        public async Task UploadFromFileAsync_Throws_UserOfflineException_When_User_Offline(string username, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(new UserOfflineException()));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws TimeoutException on TransferResponse timeout"), AutoData]
        public async Task UploadFromFileAsync_Throws_TimeoutException_On_TransferResponse_Timeout(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            // simulate a timeout waiting for the remote client to ack the request to start
            waiter.Setup(m => m.Wait<TransferResponse>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws OperationCanceledException on TransferResponse cancellation"), AutoData]
        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_TransferResponse_Cancellation(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var waitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferResponse>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws OperationCanceledException on request write cancellation"), AutoData]
        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_Request_Write_Cancellation(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new OperationCanceledException()));

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws TimeoutException on transfer response timeout"), AutoData]
        public async Task UploadFromFileAsync_Throws_TimeoutException_On_Transfer_Response_Timeout(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferResponse>(new TimeoutException()));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<object>(new TimeoutException()));
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new TimeoutException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync completes following normal transfer connection disconnect"), AutoData]
        public async Task UploadFromFileAsync_Completes_Following_Normal_Transfer_Connection_Disconnect(string username, IPEndPoint endpoint, byte[] data, string filename, int token)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);
                var response = new TransferResponse(token, "foo".Length);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
                waiter.Setup(m => m.Complete(It.IsAny<WaitKey>()))
                    .Callback<WaitKey>((key) => tcs.TrySetResult(true));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var task = s.InvokeMethod<Task<Transfer>>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null);

                    transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                    var transfer = await task;

                    Assert.Equal(username, transfer.Username);
                    Assert.Equal(filename, transfer.Filename);
                    Assert.Equal(token, transfer.Token);
                    Assert.Equal(data.Length, transfer.Size);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws TimeoutException on unexpected transfer connection timeout"), AutoData]
        public async Task UploadFromFileAsync_Throws_TimeoutException_On_Unexpected_Transfer_Connection_Timeout(string username, IPEndPoint endpoint, string filename, int token, byte[] data)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var response = new TransferResponse(token, data.Length);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.Throw(It.IsAny<WaitKey>(), It.IsAny<TimeoutException>()))
                .Callback<WaitKey, Exception>((key, ex) => tcs.SetException(ex));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task); // hang indefinitely

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var timeoutEx = new TimeoutException("timed out");

            using (var testFile = new TestFile(data))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("timed out", timeoutEx));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
                Assert.Equal("timed out", ex.Message);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws OperationCanceledException on unexpected transfer connection cancellation"), AutoData]
        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_Unexpected_Transfer_Connection_Cancellation(string username, IPEndPoint endpoint, string filename, int token, byte[] data)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var response = new TransferResponse(token, data.Length);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.Throw(It.IsAny<WaitKey>(), It.IsAny<OperationCanceledException>()))
                .Callback<WaitKey, Exception>((key, ex) => tcs.SetException(ex));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task); // hang indefinitely

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var cancelEx = new OperationCanceledException("canceled");

            using (var testFile = new TestFile(data))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("canceled", cancelEx));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
                Assert.IsType<OperationCanceledException>(ex.InnerException);
                Assert.Equal("canceled", ex.InnerException.Message);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws wrapped Exception on unexpected transfer connection Exception"), AutoData]
        public async Task UploadFromFileAsync_Throws_Wrapped_Exception_On_Unexpected_Transfer_Connection_Exception(string username, IPEndPoint endpoint, string filename, int token, byte[] data)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var response = new TransferResponse(token, data.Length);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.Throw(It.IsAny<WaitKey>(), It.IsAny<Exception>()))
                .Callback<WaitKey, Exception>((key, ex) => tcs.SetException(ex));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task); // hang indefinitely

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var thrownEx = new Exception("some exception");

            using (var testFile = new TestFile(data))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("some exception", thrownEx));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.Contains("Failed to upload file", ex.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.Equal("Transfer failed: some exception", ex.InnerException.Message);
                Assert.Equal(thrownEx, ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync completes without Exception when transfer is allowed"), AutoData]
        public async Task UploadFromFileAsync_Completes_Without_Exception_When_Transfer_Is_Allowed(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                    Assert.Null(ex);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync completes without Exception when trailing read throws ConnectionReadException"), AutoData]
        public async Task UploadFromFileAsync_Completes_Without_Exception_When_Trailing_Read_Throws_ConnectionReadException(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.ReadAsync(It.Is<long>(l => l == 8), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
                transferConn.Setup(m => m.ReadAsync(It.Is<long>(l => l == 1), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromException<byte[]>(new ConnectionReadException()));

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                    Assert.Null(ex);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync completes without Exception after MaximumLingerTime when trailing read does not throw ConnectionReadException"), AutoData]
        public async Task UploadFromFileAsync_Completes_Without_Exception_After_MaximumLingerTime_When_Trailing_Read_Does_Not_Throw_ConnectionReadException(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            using (var testFile = new TestFile())
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.ReadAsync(It.Is<long>(l => l == 8), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
                transferConn.Setup(m => m.ReadAsync(It.Is<long>(l => l == 1), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(new byte[] { 0x0 }));

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                var txOptions = new TransferOptions(maximumLingerTime: 200);

                using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, txOptions, null));

                    Assert.Null(ex);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync produces warning diagnostic when disconnected due to MaximumLingerTime"), AutoData]
        public async Task UploadFromFileAsync_Produces_Warning_Diagnostic_When_Disconnected_Due_To_MaximumLingerTime(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.ReadAsync(It.Is<long>(l => l == 8), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
                transferConn.Setup(m => m.ReadAsync(It.Is<long>(l => l == 1), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(new byte[] { 0x0 }));

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                var diagnostic = new Mock<IDiagnosticFactory>();

                var txOptions = new TransferOptions(maximumLingerTime: 200);

                using (var s = new SoulseekClient(options: options, diagnosticFactory: diagnostic.Object, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, txOptions, null));

                    Assert.Null(ex);
                }

                diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("maximum linger time")), null), Times.Once);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync throws DuplicateTransferException when failing to insert UniqueKeyDictionary"), AutoData]
        public async Task UploadFromStreamAsync_Throws_DuplicateTransferException_If_Unique_Key_Add_Fails(string username, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var waiter = new Mock<IWaiter>();
            var conn = new Mock<IMessageConnection>();
            var connManager = new Mock<IPeerConnectionManager>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                tracked.TryAdd($"{TransferDirection.Upload}:{username}:{filename}", true);

                s.SetProperty("UniqueKeyDictionary", tracked);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync throws DuplicateTransferException when failing to insert UploadDictionary"), AutoData]
        public async Task UploadFromStreamAsync_Throws_DuplicateTransferException_If_UploadDictionary_Add_Fails(string username, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var waiter = new Mock<IWaiter>();
            var conn = new Mock<IMessageConnection>();
            var connManager = new Mock<IPeerConnectionManager>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                s.SetProperty("UniqueKeyDictionary", tracked);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(token, new TransferInternal(TransferDirection.Upload, "foo", "bar", token));

                s.SetProperty("UploadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);

                // release the unique key
                Assert.Empty(tracked);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync disposes stream given dispose option flag"), AutoData]
        public async Task UploadFromStreamAsync_Disposes_Stream_Given_Dispose_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: true);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);

                long p;

                var ex2 = Record.Exception(() =>
                {
                    p = stream.Position;
                });

                Assert.NotNull(ex2);
                Assert.IsType<ObjectDisposedException>(ex2);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync does not throw and produces a warning diagnostic if stream disposal fails"), AutoData]
        public async Task UploadFromStreamAsync_Does_Not_Throw_And_Produces_Warning_Diagnostic_If_Stream_Disposal_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            try
            {
                using (var stream = new UnDisposableStream())
                using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, diagnosticFactory: diagnostic.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var txoptions = new TransferOptions(disposeInputStreamOnCompletion: true);

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                    Assert.Null(ex);

                    diagnostic.Verify(m => m.Warning(It.Is<string>(str => str.ContainsInsensitive("failed to finalize input stream")), It.IsAny<Exception>()), Times.Once);
                }
            }
            catch (ObjectDisposedException)
            {
                // noop; this is expected because UnDisposableStream throws when dispose is called
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync does not dispose stream given false dispose option flag"), AutoData]
        public async Task UploadFromStreamAsync_Does_Not_Dispose_Stream_Given_False_Dispose_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);

                long p;

                var ex2 = Record.Exception(() =>
                {
                    p = stream.Position;
                });

                Assert.Null(ex2);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync does not throw if stream Position getter throws in finally block"), AutoData]
        public async Task UploadFromStreamAsync_Does_Not_Throw_If_Stream_Position_Getter_Throws(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            // reading the start offset
            transferConn.Setup(m => m.ReadAsync(8L, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[8]));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            // note: this stream is rigged so .Position throws the third time it's called, so we hit the finally block
            // this is shitty and fragile
            using (var stream = new UnPositionableStream())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, diagnosticFactory: diagnostic.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);

                diagnostic.Verify(m => m.Warning(It.Is<string>(str => str.ContainsInsensitive("determine final position")), It.IsAny<Exception>()), Times.Once);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync seeks stream to offset value"), AutoData]
        public async Task UploadFromStreamAsync_Seeks_Stream_To_Offset_Value(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            long size = new Random().Next(1000);
            long offset = size / 2;

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(offset)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);
                Assert.Equal(offset, stream.Position);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync skips write if offset equals size"), AutoData]
        public async Task UploadFromStreamAsync_Skips_Write_If_Offset_Equals_Size(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            long size = new Random().Next(100);
            long offset = size;

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(offset)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);
                Assert.Equal(offset, stream.Position);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync does not seek stream if SeekInputStreamAutomatically is false"), AutoData]
        public async Task UploadFromStreamAsync_Does_Not_Seek_Stream_If_SeekInputStreamAutomatically_Is_False(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            long size = new Random().Next(1000);
            long offset = size / 2;

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(offset)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(seekInputStreamAutomatically: false, disposeInputStreamOnCompletion: false, maximumLingerTime: 0);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);
                Assert.Equal(0, stream.Position);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync throws SoulseekClientException if seek is nonzero and input stream is not seekable"), AutoData]
        public async Task UploadFromStreamAsync_Throws_SoulseekClientException_If_Seek_Is_NonZero_And_Input_Stream_Is_Not_Seekable(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            long size = new Random().Next(1000);
            long offset = size / 2;

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(offset)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new UnSeekableWriteableStream()) // CanSeek = false
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<TransferException>(ex.InnerException);
            }

            transferConn.Verify(m => m.Disconnect(It.IsAny<string>(), It.Is<TransferException>(ex => ex.Message.ContainsInsensitive("input stream does not support seeking"))), Times.Once);
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync throws SoulseekClientException if seek is longer than file"), AutoData]
        public async Task UploadFromStreamAsync_Throws_SoulseekClientException_If_Seek_Is_Longer_Than_File(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            long size = new Random().Next(1000);
            long offset = size * 2;

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(offset)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<TransferException>(ex.InnerException);
            }

            transferConn.Verify(m => m.Disconnect(It.IsAny<string>(), It.Is<TransferException>(ex => ex.Message.ContainsInsensitive("exceeds file length"))), Times.Once);
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync writes correct length given nonzero offset value"), AutoData]
        public async Task UploadFromStreamAsync_Writes_Correct_Length_Given_Offset_Value(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            long size = new Random().Next(1000);
            long offset = size / 2;

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(offset)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);
                Assert.Equal(offset, stream.Position);
            }

            transferConn.Verify(m => m.WriteAsync(size - offset, It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync invokes reporter delegate passed in options"), AutoData]
        public async Task UploadFromStreamAsync_Invokes_Reporter_Delegate_Passed_In_Options(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    reporter(attempted, granted, actual);
                });

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                int att = 0;
                int gr = 0;
                int ack = 0;

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0, reporter: (tx, a, b, c) =>
                {
                    att = a;
                    gr = b;
                    ack = c;
                });

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Equal(attempted, att);
                Assert.Equal(granted, gr);
                Assert.Equal(actual, ack);

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync returns unused tokens to UploadTokenBucket"), AutoData]
        public async Task UploadFromStreamAsync_Returns_Unused_Tokens_To_UploadTokenBucket(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    reporter(attempted, granted, actual);
                });

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var bucket = new Mock<ITokenBucket>();

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, uploadTokenBucket: bucket.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                int att = 0;
                int gr = 0;
                int ack = 0;

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0, reporter: (tx, a, b, c) =>
                {
                    att = a;
                    gr = b;
                    ack = c;
                });

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Equal(attempted, att);
                Assert.Equal(granted, gr);
                Assert.Equal(actual, ack);

                Assert.Null(ex);

                bucket.Verify(m => m.Return(granted - actual), Times.Once);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync does not throw if reporter passed in options is null"), AutoData]
        public async Task UploadFromStreamAsync_Does_Not_Throw_If_Reporter_Passed_In_Options_Is_Null(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    reporter(attempted, granted, actual);
                });

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0, reporter: null);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync retrieves grant from Governor passed in options, then UploadTokenBucket"), AutoData]
        public async Task UploadFromStreamAsync_Retrieves_Grant_From_Governor_Passed_In_Options_Then_UploadTokenBucket(string username, IPEndPoint endpoint, string filename, int token)
        {
            // fix transfer size to 42.  this is less than the default buffer, so any request to the governor will be strictly 42.
            var size = 42;

            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>(async (length, inputStream, governor, reporter, cancellationToken) =>
                {
                    await governor(size, CancellationToken.None);
                });

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var bucket = new Mock<ITokenBucket>();

            using (var stream = new MemoryStream(new byte[size]))
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, uploadTokenBucket: bucket.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                // return a fixed 21 from the governor provided in options
                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false, maximumLingerTime: 0, governor: (tx, a, c) => Task.FromResult(21));

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);

                // the lesser of the buffer (default 16k) the transfer size (42) and the user-supplied governor (21) will
                // be used to take tokens from the bucket.
                bucket.Verify(m => m.GetAsync(Math.Min(size, 21), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws TransferRejectedException when acknowledgement is disallowed and message contains 'File not shared'"), AutoData]
        public async Task UploadFromFileAsync_Throws_TransferRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty); // reject
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferRejectedException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync invokes StateChanged delegate on state change"), AutoData]
        public async Task UploadFromFileAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(request));
                waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var fired = false;

                    await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(stateChanged: (e) => fired = true), null);

                    Assert.True(fired);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync raises UploadProgressUpdated event on data write"), AutoData]
        public async Task UploadFromFileAsync_Raises_UploadProgressUpdated_Event_On_Data_Read(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size, int progressSize)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Raises(m => m.DataWritten += null, this, new ConnectionDataEventArgs(progressSize, int.MaxValue));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testData = new TestFile(data))
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferProgressUpdatedEventArgs>();

                s.TransferProgressUpdated += (d, e) => events.Add(e);

                await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testData.Path, token, new TransferOptions(maximumLingerTime: 0), null);

                Assert.Equal(3, events.Count);
                Assert.Equal(TransferStates.InProgress, events[0].Transfer.State);

                // this one should have been raised by the DataWritten event
                Assert.Equal(TransferStates.InProgress, events[1].Transfer.State);
                Assert.Equal(progressSize, events[1].Transfer.BytesTransferred);

                Assert.Equal(TransferStates.InProgress, events[2].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync raises expected events on success"), AutoData]
        public async Task UploadFromFileAsync_Raises_Expected_Events_On_Success(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testData = new TestFile(data))
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                // we need to capture both types of events in the same place so that they are chronological
                var events = new List<TransferEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                s.TransferProgressUpdated += (sender, e) =>
                {
                    events.Add(e);
                };

                await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testData.Path, token, new TransferOptions(maximumLingerTime: 0), null);

                Assert.Equal(7, events.Count);

                // 1: None -> Queued | Locally
                Assert.Equal(TransferStates.None, ((TransferStateChangedEventArgs)events[0]).PreviousState);
                Assert.Equal(TransferStates.Queued | TransferStates.Locally, ((TransferStateChangedEventArgs)events[0]).Transfer.State);

                // 2: Queued | Locally -> Requested
                Assert.Equal(TransferStates.Queued | TransferStates.Locally, ((TransferStateChangedEventArgs)events[1]).PreviousState);
                Assert.Equal(TransferStates.Requested, ((TransferStateChangedEventArgs)events[1]).Transfer.State);

                // 3: Requested -> Initializing
                Assert.Equal(TransferStates.Requested, ((TransferStateChangedEventArgs)events[2]).PreviousState);
                Assert.Equal(TransferStates.Initializing, ((TransferStateChangedEventArgs)events[2]).Transfer.State);

                // 4: Initializing -> InProgress
                Assert.Equal(TransferStates.Initializing, ((TransferStateChangedEventArgs)events[3]).PreviousState);
                Assert.Equal(TransferStates.InProgress, ((TransferStateChangedEventArgs)events[3]).Transfer.State);

                // 5: Initial progress event is sent immediately after transitioning into InProgress
                var e4 = (TransferProgressUpdatedEventArgs)events[4];
                Assert.Equal(TransferStates.InProgress, e4.Transfer.State);
                Assert.Equal(0, e4.Transfer.BytesTransferred);

                // 6: Final progress event is sent immediately before transitioning into Completed | Succeeded
                // note that the size here is 0 because we can't inject an output stream; the final reported size
                // comes from the output stream position
                var e5 = (TransferProgressUpdatedEventArgs)events[5];
                Assert.Equal(TransferStates.InProgress, e5.Transfer.State);
                Assert.Equal(0, e5.Transfer.BytesTransferred);

                // 7: InProgress -> Completed | Succeeded
                Assert.Equal(TransferStates.InProgress, ((TransferStateChangedEventArgs)events[6]).PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, ((TransferStateChangedEventArgs)events[6]).Transfer.State);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync invokes ProgressUpdated delegate on data write"), AutoData]
        public async Task UploadFromFileAsync_Invokes_ProgressUpdated_Delegate_On_Data_Write(string username, IPEndPoint endpoint, string filename, byte[] data, int token)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, data.Length);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var request = new TransferRequest(TransferDirection.Upload, token, filename, data.Length);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);
                transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(BitConverter.GetBytes(0L));

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(request));
                waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                waiter.Setup(m => m.WaitIndefinitely<byte[]>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult<byte[]>(data));
                waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var testData = new TestFile(data))
                using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var fired = false;

                    await s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(progressUpdated: (e) => fired = true), null);

                    Assert.True(fired);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync raises Upload events on failure"), AutoData]
        public async Task UploadFromFileAsync_Raises_Upload_Events_On_Failure(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

            // fail the upload
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new MessageReadException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile(data))
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync raises Upload events on bad offset data"), AutoData]
        public async Task UploadFromFileAsync_Raises_Upload_Events_On_Bad_Offset_Data(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);
                transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(Array.Empty<byte>()));

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(request));
                waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var events = new List<TransferStateChangedEventArgs>();

                    s.TransferStateChanged += (sender, e) =>
                    {
                        events.Add(e);
                    };

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                    Assert.NotNull(ex);
                    Assert.IsType<SoulseekClientException>(ex);
                    Assert.IsType<MessageReadException>(ex.InnerException);

                    Assert.Equal(TransferStates.Initializing, events[events.Count - 1].PreviousState);
                    Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].Transfer.State);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync raises Upload events on timeout"), AutoData]
        public async Task UploadFromFileAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);
                transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

                // fail the upload
                transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromException(new TimeoutException()));

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(request));
                waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var events = new List<TransferStateChangedEventArgs>();

                    s.TransferStateChanged += (sender, e) =>
                    {
                        events.Add(e);
                    };

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null));

                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex);

                    Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                    Assert.Equal(TransferStates.Completed | TransferStates.TimedOut, events[events.Count - 1].Transfer.State);
                }
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync raises Upload events on cancellation"), AutoData]
        public async Task UploadFromFileAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, byte[] data, int token)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException("Wait cancelled."));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var testFile = new TestFile(data))
            using (var s = new SoulseekClient(null, waiter: waiter.Object, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);

                Assert.Equal(TransferStates.Completed | TransferStates.Cancelled, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync writes UploadDenied on cancellation"), AutoData]
        public async Task UploadFromFileAsync_Writes_UploadDenied_On_Cancellation(string username, string filename, int token, IPEndPoint endpoint)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var peerConn = new Mock<IMessageConnection>();
            peerConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            peerConn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException("Cancelled"));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(null, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }

            var expectedBytes = new UploadDenied(filename, "Cancelled").ToByteArray();
            peerConn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(expectedBytes)), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws SoulseekClientException and ConnectionException on transfer exception"), AutoData]
        public async Task UploadFromFileAsync_Throws_SoulseekClientException_And_ConnectionException_On_Transfer_Exception(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<byte[]>(new NullReferenceException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);
                Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws SoulseekClientException on failure to read offset data"), AutoData]
        public async Task UploadFromFileAsync_Throws_SoulseekClientException_On_Failure_To_Read_Offset_Data(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<byte[]>(new NullReferenceException()));
            transferConn.Setup(m => m.ReadAsync(1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new ConnectionReadException("Remote connection closed.", new ConnectionException("Remote connection closed."))));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);
                Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws SoulseekClientException on bad offset data"), AutoData]
        public async Task UploadFromFileAsync_Throws_SoulseekClientException_On_Bad_Offset_Data(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(Array.Empty<byte>()));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);
                Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws TimeoutException on transfer timeout"), AutoData]
        public async Task UploadFromFileAsync_Throws_TimeoutException_On_Transfer_Timeout(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new TimeoutException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task UploadFromFileAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }

            transferConn.Verify(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws TransferRejectedException on transfer rejection"), AutoData]
        public async Task UploadFromFileAsync_Throws_TransferRejectedException_On_Transfer_Rejection(string username, IPEndPoint endpoint, string filename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty); // reject
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferRejectedException>(ex);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws ConnectionException when transfer connection fails"), AutoData]
        public async Task UploadFromFileAsync_Throws_ConnectionException_When_Transfer_Connection_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new ConnectionException()));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync sets Exception property when transfer connection fails"), AutoData]
        public async Task UploadFromFileAsync_Sets_Exception_Property_When_Transfer_Connection_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new ConnectionException()));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                Exception caught = null;

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>(
                    "UploadFromFileAsync",
                    username,
                    filename,
                    testFile.Path,
                    token,
                    new TransferOptions(stateChanged: (args) =>
                    {
                        if (args.Transfer.State.HasFlag(TransferStates.Completed) && !args.Transfer.State.HasFlag(TransferStates.Succeeded))
                        {
                            caught = args.Transfer.Exception;
                        }
                    }),
                    null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);

                Assert.NotNull(caught);
                Assert.IsType<ConnectionException>(caught);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync updates remote user on failure"), AutoData]
        public async Task UploadFromFileAsync_Updates_Remote_User_On_Failure(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new NullReferenceException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.NotNull(ex);
            }

            var expectedBytes = new UploadFailed(filename).ToByteArray();
            conn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(expectedBytes)), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync swallows final read exception"), AutoData]
        public async Task UploadFromFileAsync_Swallows_Final_Read_Exception(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));
            transferConn.Setup(m => m.ReadAsync(1, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new ConnectionReadException("Remote connection closed", new ConnectionException("Remote connection closed"))));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: int.MaxValue), null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync throws TransferException if AcquireSlot throws"), AutoData]
        public async Task UploadFromStreamAsync_Throws_TransferException_If_AcquireSlot_Throws(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(slotAwaiter: (tx, ct) => throw new Exception("foo"));

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.NotNull(ex);

                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<TransferException>(ex.InnerException);
                Assert.IsType<Exception>(ex.InnerException.InnerException);
                Assert.Equal("foo", ex.InnerException.InnerException.Message);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync throws OperationCanceledException if AcquireSlot Task is cancelled"), AutoData]
        public async Task UploadFromStreamAsync_Throws_OperationCanceledException_If_AcquireSlot_Task_Is_Cancelled(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(slotAwaiter: (t, c) => Task.FromCanceled(new CancellationToken(canceled: true)));

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.NotNull(ex);

                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync does not throw if SlotReleased throws"), AutoData]
        public async Task UploadFromStreamAsync_Does_Not_Throw_If_SlotReleased_Throws(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, diagnosticFactory: diagnostic.Object, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(slotReleased: (t) => throw new Exception("foo"));

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, txoptions, null));

                Assert.Null(ex);
            }

            diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("encountered exception releasing upload slot")), It.IsAny<Exception>()));
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync releases unique key when succeeding"), AutoData]
        public async Task UploadFromStreamAsync_Releases_Unique_Key_When_Succeeding(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(8, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, diagnosticFactory: diagnostic.Object, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                s.SetProperty("UniqueKeyDictionary", tracked);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));

                Assert.Null(ex);

                Assert.Empty(tracked);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync releases unique key when failing"), AutoData]
        public async Task UploadFromStreamAsync_Releases_Unique_Key_When_Failing(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Throws(new Exception("oops"));

            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options: options, diagnosticFactory: diagnostic.Object, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                s.SetProperty("UniqueKeyDictionary", tracked);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, new Func<long, Task<Stream>>((_) => Task.FromResult((Stream)stream)), token, null, null));

                Assert.NotNull(ex);

                Assert.Empty(tracked);
            }
        }

        [Trait("Category", "UploadFromFileAsync")]
        [Theory(DisplayName = "UploadFromFileAsync throws when write throws"), AutoData]
        public async Task UploadFromFileAsync_Throws_When_Write_Throws(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            using (var testFile = new TestFile(data))
            {
                var options = new SoulseekClientOptions(messageTimeout: 5);

                var response = new TransferResponse(token, size);
                var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

                var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

                var transferConn = new Mock<IConnection>();
                transferConn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);
                transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken?>()))
                    .Returns(Task.FromResult(BitConverter.GetBytes(0L)));

                // fail the upload
                transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromException(new Exception("foo")));

                var waiter = new Mock<IWaiter>();
                waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
                waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(request));
                waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));
                waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

                var conn = new Mock<IMessageConnection>();
                conn.Setup(m => m.State)
                    .Returns(ConnectionState.Connected);

                var connManager = new Mock<IPeerConnectionManager>();
                connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(conn.Object));
                connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(transferConn.Object));

                using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var events = new List<TransferStateChangedEventArgs>();

                    s.TransferStateChanged += (sender, e) =>
                    {
                        events.Add(e);
                    };

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromFileAsync", username, filename, testFile.Path, token, new TransferOptions(maximumLingerTime: 0), null));

                    Assert.NotNull(ex);
                    Assert.IsType<SoulseekClientException>(ex);
                    Assert.IsType<Exception>(ex.InnerException);
                    Assert.Equal("foo", ex.InnerException.Message);
                }
            }
        }

        private class UnSeekableWriteableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanWrite => false;

            public override bool CanSeek => false;

            public override long Length => throw new NotImplementedException();

            public override long Position { get => 0; set => throw new NotImplementedException(); }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }

        private class UnPositionableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanWrite => false;

            public override bool CanSeek => false;

            public override long Length => 0;

            private int timesCalled = 0;

            public override long Position
            {
                get
                {
                    timesCalled++;

                    if (timesCalled == 3)
                    {
                        throw new NotImplementedException($"times called: {timesCalled}");
                    }

                    return 0;
                }
                set => throw new NotImplementedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return count;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return offset;
            }

            public override void SetLength(long value)
            {
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }
        }

        private class UnDisposableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanWrite => false;

            public override bool CanSeek => false;

            public override long Length => 0;

            public override long Position { get; set; }

            public override ValueTask DisposeAsync()
            {
                throw new ObjectDisposedException("failed disposal");
            }

            protected override void Dispose(bool disposing)
            {
                throw new ObjectDisposedException("failed disposal");
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return count;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return offset;
            }

            public override void SetLength(long value)
            {
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }
        }
    }
}
