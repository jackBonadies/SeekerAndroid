// <copyright file="DownloadAsyncTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class DownloadAsyncTests
    {
        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws ArgumentNullException null stream factory")]
        public async Task DownloadAsync_Stream_Throws_ArgumentNullException_Given_Null_Stream_Factory()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", outputStreamFactory: null));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentNullException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, "filename", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Stream_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, "filename", () => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad remote filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Remote_Filename(string filename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentException given bad local filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Throws_ArgumentException_Given_Bad_Local_Filename(string filename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "remote", filename));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream throws ArgumentException given bad filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadAsync_Stream_Throws_ArgumentException_Given_Bad_Filename(string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, () => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws ArgumentOutOfRangeException given negative size")]
        public async Task DownloadAsync_Stream_Throws_ArgumentOutOfRangeException_Given_Negative_Size()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "foo", () => Task.FromResult((Stream)stream), size: -1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("size", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws ArgumentOutOfRangeException given negative startOffset")]
        public async Task DownloadAsync_Stream_Throws_ArgumentOutOfRangeException_Given_Negative_StartOffset()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "foo", () => Task.FromResult((Stream)stream), startOffset: -1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("startOffset", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws ArgumentNullException given startOffset and no size")]
        public async Task DownloadAsync_Stream_Throws_ArgumentNullException_Given_StartOffset_And_No_Size()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "foo", () => Task.FromResult((Stream)stream), startOffset: 1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentNullException>(ex);
                Assert.Equal("size", ((ArgumentNullException)ex).ParamName);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentOutOfRangeException given negative size"), AutoData]
        public async Task DownloadAsync_Throws_ArgumentOutOfRangeException_Given_Negative_Size(string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "remote", localFilename, size: -1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("size", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentOutOfRangeException given negative startOffset"), AutoData]
        public async Task DownloadAsync_Throws_ArgumentOutOfRangeException_Given_Negative_StartOffset(string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "remote", localFilename, startOffset: -1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("startOffset", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws ArgumentNullException given startOffset and no size"), AutoData]
        public async Task DownloadAsync_Throws_ArgumentNullException_Given_StartOffset_And_No_Size(string localFilename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "remote", localFilename, startOffset: 1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentNullException>(ex);
                Assert.Equal("size", ((ArgumentNullException)ex).ParamName);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws InvalidOperationException when not connected"), AutoData]
        public async Task DownloadAsync_Throws_InvalidOperationException_When_Not_Connected(string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws InvalidOperationException when not connected")]
        public async Task DownloadAsync_Stream_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws InvalidOperationException when not logged in"), AutoData]
        public async Task DownloadAsync_Throws_InvalidOperationException_When_Not_Logged_In(string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws InvalidOperationException when not logged in")]
        public async Task DownloadAsync_Stream_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws DuplicateTokenException when token used by download"), AutoData]
        public async Task DownloadAsync_Throws_DuplicateTokenException_When_Token_Used_by_Download(string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(1, new TransferInternal(TransferDirection.Download, "foo", "bar", 1));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename, token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws DuplicateTokenException when token used by upload"), AutoData]
        public async Task DownloadAsync_Throws_DuplicateTokenException_When_Token_Used_By_Upload(string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(1, new TransferInternal(TransferDirection.Upload, "foo", "bar", 1));

                s.SetProperty("UploadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", localFilename, token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws DuplicateTokenException when token used by download")]
        public async Task DownloadAsync_Stream_Throws_DuplicateTokenException_When_Token_Used_By_Download()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(1, new TransferInternal(TransferDirection.Download, "foo", "bar", 1));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream), token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Fact(DisplayName = "DownloadAsync stream throws DuplicateTokenException when token used by upload")]
        public async Task DownloadAsync_Stream_Throws_DuplicateTokenException_When_Token_Used_By_Upload()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(1, new TransferInternal(TransferDirection.Upload, "foo", "bar", 1));

                s.SetProperty("UploadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", "filename", () => Task.FromResult((Stream)stream), token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws DuplicateTransferException when an existing download matches the username and filename"), AutoData]
        public async Task DownloadAsync_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_The_Username_And_Filename(string username, string filename, string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws DuplicateTransferException when an existing download matches a unique key"), AutoData]
        public async Task DownloadAsync_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_A_Unique_Key(string username, string filename, string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);

                s.SetProperty("UniqueKeyDictionary", tracked);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync does not throw DuplicateTransferException when an existing download matches only the username"), AutoData]
        public async Task DownloadAsync_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Username(string username, string filename, string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, username, "different", 0));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync does not throw DuplicateTransferException when an existing download matches only the filename"), AutoData]
        public async Task DownloadAsync_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Filename(string username, string filename, string localFilename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, "different", filename, 0));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, token: 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream throws DuplicateTransferException when an existing download matches the username and filename"), AutoData]
        public async Task DownloadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_The_Username_And_Filename(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream throws DuplicateTransferException when an existing download matches a unique key"), AutoData]
        public async Task DownloadAsync_Stream_Throws_DuplicateTransferException_When_An_Existing_Download_Matches_A_Unique_Key(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);

                s.SetProperty("UniqueKeyDictionary", tracked);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued download of {filename} from {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream does not throw DuplicateTransferException when an existing download matches only the username"), AutoData]
        public async Task DownloadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Username(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, username, "different", 0));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream does not throw DuplicateTransferException when an existing download matches only the filename"), AutoData]
        public async Task DownloadAsync_Stream_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Download_Matches_Only_The_Filename(string username, string filename)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Download, "different", filename, 0));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), token: 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream substitutes CancellationToken given null"), AutoData]
        public async Task DownloadAsync_Stream_Substitutes_CancellationToken_Given_Null(string username, string filename)
        {
            var conn = new Mock<IMessageConnection>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream)));
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), CancellationToken.None), Times.Once);
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream uses given CancellationToken"), AutoData]
        public async Task DownloadAsync_Stream_Uses_Given_CancellationToken(string username, string filename)
        {
            var cancellationToken = new CancellationToken();
            var conn = new Mock<IMessageConnection>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, () => Task.FromResult((Stream)stream), cancellationToken: cancellationToken));
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync substitutes CancellationToken given null"), AutoData]
        public async Task DownloadAsync_Substitutes_CancellationToken_Given_Null(string username, string filename, string localFilename)
        {
            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename));
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), CancellationToken.None), Times.Once);
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync uses given CancellationToken"), AutoData]
        public async Task DownloadAsync_Uses_Given_CancellationToken(string username, string filename, string localFilename)
        {
            var cancellationToken = new CancellationToken();
            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await Record.ExceptionAsync(() => s.DownloadAsync(username, filename, localFilename, cancellationToken: cancellationToken));
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.Once);
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws UserOfflineException on user offline"), AutoData]
        public async Task DownloadAsync_Throws_UserOfflineException_On_User_Offline(string filename, string localFilename)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(new UserOfflineException()));

            var manager = new Mock<IPeerConnectionManager>();

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: conn.Object, options: options, peerConnectionManager: manager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, localFilename));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync throws TimeoutException on peer message connection timeout"), AutoData]
        public async Task DownloadAsync_Throws_TimeoutException_On_Peer_Message_Connection_Timeout(IPEndPoint endpoint, string filename, string localFilename)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse("username", endpoint)));

            var manager = new Mock<IPeerConnectionManager>();
            manager.Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<string>(), endpoint, It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: conn.Object, options: options, peerConnectionManager: manager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, localFilename));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "DownloadAsync")]
        [Theory(DisplayName = "DownloadAsync stream throws TimeoutException on peer message connection timeout"), AutoData]
        public async Task DownloadAsync_Stream_Throws_TimeoutException_On_Peer_Message_Connection_Timeout(IPEndPoint endpoint, string filename)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse("username", endpoint)));

            var manager = new Mock<IPeerConnectionManager>();
            manager.Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<string>(), endpoint, It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: conn.Object, options: options, peerConnectionManager: manager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.DownloadAsync("username", filename, () => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferException when WriteAsync throws"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferException_When_WriteAsync_Throws(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), CancellationToken.None))
                .Throws(new ConnectionWriteException());
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var serverConn = new Mock<IMessageConnection>();
            serverConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: serverConn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferException on TransferResponse timeout"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferException_On_TransferResponse_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1, peerConnectionOptions: new ConnectionOptions(inactivityTimeout: 1));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));
            waiter.Setup(m => m.Wait<TransferResponse>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferException on TransferResponse cancellation"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferException_On_TransferResponse_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var waitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(waitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferResponse>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferException on TransferRequest cancellation"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferException_On_TransferRequest_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<TransferRequest>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferException on download cancellation"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferException_On_Download_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), CancellationToken.None))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            // here is the thing that is supposed to make this test work
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TimeoutException on transfer response timeout"), AutoData]
        public async Task DownloadToFileAsync_Throws_TimeoutException_On_Transfer_Response_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TimeoutException on read timeout"), AutoData]
        public async Task DownloadToFileAsync_Throws_TimeoutException_On_Read_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new TimeoutException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferRejectedException when acknowledgement is disallowed and message contains 'File not shared'"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "File not shared."); // not shared
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferRejectedException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync sets Exception property when transfer fails"), AutoData]
        public async Task DownloadToFileAsync_Sets_Exception_Property_When_transfer_Fails(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "File not shared."); // not shared
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                Exception caught = null;

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>(
                    "DownloadToFileAsync",
                    username,
                    filename,
                    localFilename,
                    0L,
                    0,
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
                Assert.IsType<TransferRejectedException>(ex);

                Assert.NotNull(caught);
                Assert.IsType<TransferRejectedException>(caught);
            }
        }

        /// <summary>
        ///     This test is intended to be the definitive source of truth regarding the sequence of Progress and State events
        ///     for a successful transfer.
        /// </summary>
        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync raises expected events on success"), AutoData]
        public async Task DownloadToFileAsync_Raises_Expected_Events_On_Success(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null);

                Assert.Equal(8, events.Count);

                // 1: None -> Queued | Locally
                Assert.Equal(TransferStates.None, ((TransferStateChangedEventArgs)events[0]).PreviousState);
                Assert.Equal(TransferStates.Queued | TransferStates.Locally, ((TransferStateChangedEventArgs)events[0]).Transfer.State);

                // 2: Queued | Locally -> Requested
                Assert.Equal(TransferStates.Queued | TransferStates.Locally, ((TransferStateChangedEventArgs)events[1]).PreviousState);
                Assert.Equal(TransferStates.Requested, ((TransferStateChangedEventArgs)events[1]).Transfer.State);

                // 3: Requested -> Queued | Remotely
                // note that somewhere along the way a change was made so that this state transition is captured even
                // if the remote client allows the transfer to begin immediately
                Assert.Equal(TransferStates.Requested, ((TransferStateChangedEventArgs)events[2]).PreviousState);
                Assert.Equal(TransferStates.Queued | TransferStates.Remotely, ((TransferStateChangedEventArgs)events[2]).Transfer.State);

                // 4: Queued | Remotely -> Initializing
                Assert.Equal(TransferStates.Queued | TransferStates.Remotely, ((TransferStateChangedEventArgs)events[3]).PreviousState);
                Assert.Equal(TransferStates.Initializing, ((TransferStateChangedEventArgs)events[3]).Transfer.State);

                // 5: Initializing -> InProgress
                Assert.Equal(TransferStates.Initializing, ((TransferStateChangedEventArgs)events[4]).PreviousState);
                Assert.Equal(TransferStates.InProgress, ((TransferStateChangedEventArgs)events[4]).Transfer.State);

                // 6: Initial progress event is sent immediately after transitioning into InProgress
                var e5 = (TransferProgressUpdatedEventArgs)events[5];
                Assert.Equal(TransferStates.InProgress, e5.Transfer.State);
                Assert.Equal(0, e5.Transfer.BytesTransferred);

                // 7: Final progress event is sent immediately before transitioning into Completed | Succeeded
                // note that the size here is 0 because we can't inject an output stream; the final reported size
                // comes from the output stream position
                var e6 = (TransferProgressUpdatedEventArgs)events[6];
                Assert.Equal(TransferStates.InProgress, e6.Transfer.State);
                Assert.Equal(0, e6.Transfer.BytesTransferred);

                // 8: InProgress -> Completed | Succeeded
                Assert.Equal(TransferStates.InProgress, ((TransferStateChangedEventArgs)events[7]).PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, ((TransferStateChangedEventArgs)events[7]).Transfer.State);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync uses size from TransferResponse given null size when skipping queue"), AutoData]
        public async Task DownloadToFileAsync_Uses_Size_From_TransferResponse_Given_Null_Size_When_Skipping_Queue(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, null, 0, token, new TransferOptions(), null);
            }

            transferConn.Verify(
                m => m.ReadAsync(
                    size,
                    It.IsAny<Stream>(),
                    It.IsAny<Func<int, CancellationToken, Task<int>>>(),
                    It.IsAny<Action<int, int, int>>(),
                    It.IsAny<CancellationToken?>()),
                Times.Once);
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferSizeMismatchException on size mismatch when skipping queue"), AutoData]
        public async Task DownloadToFileAsync_Throws_On_Size_Mismatch_When_Skipping_Queue(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, remoteSize); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferSizeMismatchException>(ex);
                Assert.Equal(size, ((TransferSizeMismatchException)ex).LocalSize);
                Assert.Equal(remoteSize, ((TransferSizeMismatchException)ex).RemoteSize);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync sets state to Aborted on size mismatch"), AutoData]
        public async Task DownloadToFileAsync_Sets_State_To_Aborted_On_Size_Mismatch(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, remoteSize); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                _ = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.Equal(TransferStates.Completed | TransferStates.Aborted, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync writes offset to connection"), AutoData]
        public async Task DownloadToFileAsync_Writes_Offset_To_Connection(string username, IPEndPoint endpoint, string filename, string localFilename, long offset, int token, int size)
        {
            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, offset, token, new TransferOptions(), null);
            }

            transferConn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => BitConverter.ToInt64(b, 0) == offset), It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "DownloadToStreamAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync does not throw if stream Position getter throws in finally block"), AutoData]
        public async Task DownloadToStreamAsync_Does_Not_Throw_If_Stream_Position_Getter_Throws_In_Finally_Block(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            // note: this stream is rigged so that .Position throws after the third call, so we hit the finally block
            using (var stream = new UnPositionableStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, diagnosticFactory: diagnostic.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeOutputStreamOnCompletion: true);
                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));

                Assert.Null(ex);

                diagnostic.Verify(m => m.Warning(It.Is<string>(str => str.ContainsInsensitive("determine final position")), It.IsAny<Exception>()), Times.Once);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync disposes output stream given option flag"), AutoData]
        public async Task DownloadToStreamAsync_Disposes_Output_Stream_Given_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeOutputStreamOnCompletion: true);
                await s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null);

                long p;

                var ex = Record.Exception(() =>
                {
                    p = stream.Position;
                });

                Assert.NotNull(ex);
                Assert.IsType<ObjectDisposedException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync does not throw and produces a warning diagnostic if stream disposal fails"), AutoData]
        public async Task DownloadToStreamAsync_Does_Not_Throw_And_Produces_Warning_Diagnostic_If_Stream_Disposal_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            try
            {
                using (var stream = new UnDisposableStream())
                using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, diagnosticFactory: diagnostic.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var txoptions = new TransferOptions(disposeOutputStreamOnCompletion: true);

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));

                    Assert.Null(ex);

                    diagnostic.Verify(m => m.Warning(It.Is<string>(str => str.ContainsInsensitive("failed to finalize output stream")), It.IsAny<Exception>()), Times.Once);
                }
            }
            catch (ObjectDisposedException)
            {
                // noop; this is expected because UnDisposableStream throws when dispose is called
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync does not throw and produces a warning diagnostic if stream flush fails"), AutoData]
        public async Task DownloadToStreamAsync_Does_Not_Throw_And_Produces_Warning_Diagnostic_If_Stream_Flush_Fails(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            try
            {
                using (var stream = new UnFlushableStream())
                using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, diagnosticFactory: diagnostic.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    var txoptions = new TransferOptions(disposeOutputStreamOnCompletion: true);

                    var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));

                    Assert.Null(ex);

                    diagnostic.Verify(m => m.Warning(It.Is<string>(str => str.ContainsInsensitive("failed to finalize output stream")), It.IsAny<Exception>()), Times.Once);
                }
            }
            catch (ObjectDisposedException)
            {
                // noop; this is expected because UnDisposableStream throws when dispose is called
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync completes following normal transfer connection disconnect"), AutoData]
        public async Task DownloadToStreamAsync_Completes_Following_Normal_Transfer_Connection_Disconnect(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var transfer = await task;

                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, transfer.State);
                Assert.Equal(username, transfer.Username);
                Assert.Equal(token, transfer.Token);
                Assert.Equal(filename, transfer.Filename);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync releases unique key on success"), AutoData]
        public async Task DownloadToStreamAsync_Releases_Unique_Key_On_Success(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, diagnosticFactory: diagnostic.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                s.SetProperty("UniqueKeyDictionary", tracked);

                var txoptions = new TransferOptions(disposeOutputStreamOnCompletion: true);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));

                Assert.Null(ex);

                Assert.Empty(tracked);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync releases unique key on failure"), AutoData]
        public async Task DownloadToStreamAsync_Releases_Unique_Key_On_Failure(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, diagnosticFactory: diagnostic.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                s.SetProperty("UniqueKeyDictionary", tracked);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(token, new TransferInternal(TransferDirection.Download, "foo", "bar", token));

                s.SetProperty("DownloadDictionary", queued);

                var txoptions = new TransferOptions(disposeOutputStreamOnCompletion: true);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);

                Assert.Empty(tracked);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync invokes Reporter delegate passed in options"), AutoData]
        public async Task DownloadToStreamAsync_Invokes_Reporter_Delegate_Passed_In_Options(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    reporter(attempted, granted, actual);
                });

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var att = 0;
                var gr = 0;
                var ack = 0;

                var opts = new TransferOptions(reporter: (tx, a, b, c) =>
                {
                    att = a;
                    gr = b;
                    ack = c;
                });

                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                await task;

                Assert.Equal(attempted, att);
                Assert.Equal(granted, gr);
                Assert.Equal(actual, ack);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync returns unused tokens to DownloadTokenBucket"), AutoData]
        public async Task DownloadToStreamAsync_Returns_Unused_Tokens_To_DownloadTokenBucket(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    reporter(attempted, granted, actual);
                });

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var bucket = new Mock<ITokenBucket>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, downloadTokenBucket: bucket.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var att = 0;
                var gr = 0;
                var ack = 0;

                var opts = new TransferOptions(reporter: (tx, a, b, c) =>
                {
                    att = a;
                    gr = b;
                    ack = c;
                });

                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                await task;

                Assert.Equal(attempted, att);
                Assert.Equal(granted, gr);
                Assert.Equal(actual, ack);

                bucket.Verify(m => m.Return(granted - actual), Times.Once);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync does not throw if Reporter delegate from options is null"), AutoData]
        public async Task DownloadToStreamAsync_Does_Not_Throw_If_Reporter_Delegate_From_Options_Is_Null(string username, IPEndPoint endpoint, string filename, int token, int size, int attempted, int granted, int actual)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    reporter(attempted, granted, actual);
                });

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var opts = new TransferOptions(reporter: null);

                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.Null(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync retrieves grant from Governor passed in options, then DownloadTokenBucket"), AutoData]
        public async Task DownloadToStreamAsync_Retrieves_Grant_From_Governor_Passed_In_Options_Then_DownloadTokenBucket(string username, IPEndPoint endpoint, string filename, int token)
        {
            // fix transfer size to 42.  this is less than the default buffer, so any request to the governor will be strictly 42.
            var size = 42;

            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken?>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>(async (length, inputStream, governor, reporter, cancellationToken) =>
                {
                    await governor(size, CancellationToken.None);
                });

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var bucket = new Mock<ITokenBucket>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, downloadTokenBucket: bucket.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                // return a fixed 21 from the governor provided in options
                var opts = new TransferOptions(governor: (tx, a, c) => Task.FromResult(21));

                var task = s.InvokeMethod<Task<Transfer>>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, opts, null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.Null(ex);

                // the lesser of the buffer (default 16k) the transfer size (42) and the user-supplied governor (21) will
                // be used to take tokens from the bucket.
                bucket.Verify(m => m.GetAsync(Math.Min(size, 21), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Trait("Category", "DownloadToStreamAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync throws DuplicateTransferException when failing to insert UniqueKeyDictionary"), AutoData]
        public async Task DownloadToStreamAsync_Throws_DuplicateTransferException_When_Failing_To_Insert_UniqueKeyDictionary(string username, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var waiter = new Mock<IWaiter>();
            var conn = new Mock<IMessageConnection>();
            var connManager = new Mock<IPeerConnectionManager>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                tracked.TryAdd($"{TransferDirection.Download}:{username}:{filename}", true);
                s.SetProperty("UniqueKeyDictionary", tracked);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, null, null));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "DownloadToStreamAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync throws DuplicateTransferException when failing to insert DownloadDictionary"), AutoData]
        public async Task DownloadToStreamAsync_Throws_DuplicateTransferException_When_Failing_To_Insert_DownloadDictionary(string username, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var waiter = new Mock<IWaiter>();
            var conn = new Mock<IMessageConnection>();
            var connManager = new Mock<IPeerConnectionManager>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var tracked = new ConcurrentDictionary<string, bool>();
                s.SetProperty("UniqueKeyDictionary", tracked);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(token, new TransferInternal(TransferDirection.Download, "foo", "bar", token));

                s.SetProperty("DownloadDictionary", queued);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, null, null));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);

                // ensure the unique key is released
                Assert.Empty(tracked);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync throws TimeoutException on unexpected transfer connection timeout"), AutoData]
        public async Task DownloadToStreamAsync_Throws_TimeoutException_On_Unexpected_Transfer_Connection_Timeout(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            // help us hang the read task indefinitely
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // capture the cancellation token passed to read so we can ensure it is cancelled
            CancellationToken? capturedToken = default;

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    capturedToken = cancellationToken.GetValueOrDefault();
                })
                .Returns(tcs.Task); // this will hang the read indefinitely until it is cancelled when disconnected.  if this test hangs, this is why

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("timed out", new TimeoutException("timed out")));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
                Assert.Equal("timed out", ex.Message);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync throws OperationCanceledException on unexpected transfer connection cancellation"), AutoData]
        public async Task DownloadToStreamAsync_Throws_OperationCanceledException_On_Unexpected_Transfer_Connection_Cancellation(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            // help us hang the read task indefinitely
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // capture the cancellation token passed to read so we can ensure it is cancelled
            CancellationToken? capturedToken = default;

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    capturedToken = cancellationToken.GetValueOrDefault();
                })
                .Returns(tcs.Task); // this will hang the read indefinitely until it is cancelled when disconnected.  if this test hangs, this is why

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);

                // make the transfer appear to disconnect
                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("cancelled", new OperationCanceledException("cancelled")));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
                Assert.Equal("Operation cancelled", ex.Message);

                // make sure the read is cancelled (this would hang if not, but still)
                Assert.True(capturedToken.Value.IsCancellationRequested);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync throws wrapped Exception on unexpected transfer connection Exception"), AutoData]
        public async Task DownloadToStreamAsync_Throws_Wrapped_Exception_On_Unexpected_Transfer_Connection_Exception(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            // help us hang the read task indefinitely
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // capture the cancellation token passed to read so we can ensure it is cancelled
            CancellationToken? capturedToken = default;

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    capturedToken = cancellationToken.GetValueOrDefault();
                })
                .Returns(tcs.Task); // this will hang the read indefinitely until it is cancelled when disconnected.  if this test hangs, this is why

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);

                var thrownEx = new Exception("some exception");

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("some exception", thrownEx));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.Contains("Failed to download file", ex.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.Equal("Transfer failed: some exception", ex.InnerException.Message);
                Assert.Equal(thrownEx, ex.InnerException.InnerException);

                // make sure the read is cancelled (this would hang if not, but still)
                Assert.True(capturedToken.Value.IsCancellationRequested);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync throws wrapped Exception on remote client DownloadFailed message"), AutoData]
        public async Task DownloadToStreamAsync_Throws_Wrapped_Exception_On_Remote_Client_DownloadFailed_Message(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            // help us hang the read task indefinitely
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // capture the cancellation token passed to read so we can ensure it is cancelled
            CancellationToken? capturedToken = default;

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    capturedToken = cancellationToken.GetValueOrDefault();
                })
                .Returns(tcs.Task); // this will hang the read indefinitely until it is cancelled when disconnected.  if this test hangs, this is why

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var peerMessageHandler = new Mock<IPeerMessageHandler>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, peerMessageHandler: peerMessageHandler.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);

                // Give the download task time to start and reach the point where it's waiting for data
                await Task.Delay(100);

                // Raise the DownloadFailed event
                peerMessageHandler.Raise(m => m.DownloadFailed += null, peerMessageHandler.Object, new DownloadFailedEventArgs(username, filename));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.Contains("Failed to download file", ex.Message, StringComparison.InvariantCultureIgnoreCase);
                Assert.IsType<TransferException>(ex.InnerException);
                Assert.Equal("Download reported as failed by remote client", ex.InnerException.Message);

                // make sure the read is cancelled (this would hang if not, but still)
                Assert.True(capturedToken.Value.IsCancellationRequested);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync throws wrapped Exception on remote client DownloadDenied message"), AutoData]
        public async Task DownloadToStreamAsync_Throws_Wrapped_Exception_On_Remote_Client_DownloadDenied_Message(string username, IPEndPoint endpoint, string filename, int token, int size, string denialMessage)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            // help us hang the read task indefinitely
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // capture the cancellation token passed to read so we can ensure it is cancelled
            CancellationToken? capturedToken = default;

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Callback<long, Stream, Func<int, CancellationToken, Task<int>>, Action<int, int, int>, CancellationToken?>((length, inputStream, governor, reporter, cancellationToken) =>
                {
                    capturedToken = cancellationToken.GetValueOrDefault();
                })
                .Returns(tcs.Task); // this will hang the read indefinitely until it is cancelled when disconnected.  if this test hangs, this is why

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var peerMessageHandler = new Mock<IPeerMessageHandler>();

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object, peerMessageHandler: peerMessageHandler.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, new TransferOptions(), null);

                // Give the download task time to start and reach the point where it's waiting for data
                await Task.Delay(100);

                // Raise the DownloadFailed event
                peerMessageHandler.Raise(m => m.DownloadDenied += null, peerMessageHandler.Object, new DownloadDeniedEventArgs(username, filename, denialMessage));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<TransferRejectedException>(ex);
                Assert.Equal(denialMessage, ex.Message);

                // make sure the read is cancelled (this would hang if not, but still)
                Assert.True(capturedToken.Value.IsCancellationRequested);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToStreamAsync does not dispose output stream given no option flag"), AutoData]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "Discard")]
        public async Task DownloadToStreamAsync_Does_Not_Dispose_Output_Stream_Given_No_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var data = new byte[] { 0x0, 0x1, 0x2, 0x3 };

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var txoptions = new TransferOptions(disposeOutputStreamOnCompletion: false);
                await s.InvokeMethod<Task>("DownloadToStreamAsync", username, filename, new Func<Task<Stream>>(() => Task.FromResult((Stream)stream)), (long?)size, 0, token, txoptions, null);

                var ex = Record.Exception(() =>
                {
                    var p = stream.Position;
                });

                Assert.Null(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync uses size from TransferResponse given null size when queued"), AutoData]
        public async Task DownloadToFileAsync_Uses_Size_From_TransferResponse_When_Queued(string username, IPEndPoint endpoint, string remoteFilename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, remoteFilename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, remoteFilename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, remoteFilename, localFilename, null, 0, token, new TransferOptions(), null);
            }

            transferConn.Verify(
                m => m.ReadAsync(
                    size,
                    It.IsAny<Stream>(),
                    It.IsAny<Func<int, CancellationToken, Task<int>>>(),
                    It.IsAny<Action<int, int, int>>(),
                    It.IsAny<CancellationToken?>()),
                Times.Once);
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferSizeMismatchException on mismatch when queued"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferSizeMismatchException_On_Mismatch_When_Queued(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, remoteSize);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferSizeMismatchException>(ex);
                Assert.Equal(size, ((TransferSizeMismatchException)ex).LocalSize);
                Assert.Equal(remoteSize, ((TransferSizeMismatchException)ex).RemoteSize);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync sets transfer state to Aborted on mismatch when queued"), AutoData]
        public async Task DownloadToFileAsync_Sets_Transfer_State_To_On_Mismatch_When_Queued(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int remoteSize)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, remoteSize);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                _ = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.Equal(TransferStates.Completed | TransferStates.Aborted, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync uses given size when queued"), AutoData]
        public async Task DownloadToFileAsync_Uses_Given_Size_When_Queued(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var queueResponse = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(queueResponse));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, null, 0, token, new TransferOptions(), null);
            }

            transferConn.Verify(
                m => m.ReadAsync(
                    size,
                    It.IsAny<Stream>(),
                    It.IsAny<Func<int, CancellationToken, Task<int>>>(),
                    It.IsAny<Action<int, int, int>>(),
                    It.IsAny<CancellationToken?>()),
                Times.Once);
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync initiates a transfer if remote client does not initiate after a disallowed response"), AutoData]
        public async Task DownloadToFileAsync_Initiates_A_Transfer_If_Remote_Client_Does_Not_Initiate(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new ConnectionException("failed")));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var fired = false;

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(stateChanged: (e) => fired = true), null);

                Assert.True(fired);
            }

            connManager.Verify(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync invokes StateChanged delegate on state change"), AutoData]
        public async Task DownloadToFileAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(stateChanged: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync raises DownloadProgressUpdated event on data read"), AutoData]
        public async Task DownloadToFileAsync_Raises_DownloadProgressUpdated_Event_On_Data_Read(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size, int progressSize)
        {
            var options = new SoulseekClientOptions(messageTimeout: 500);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[size]))
                .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(progressSize, int.MaxValue));

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var events = new List<TransferProgressUpdatedEventArgs>();

                s.TransferProgressUpdated += (d, e) => events.Add(e);

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null);

                Assert.Equal(3, events.Count);
                Assert.Equal(TransferStates.InProgress, events[0].Transfer.State);

                // this one should have been raised by the DataRead event
                Assert.Equal(TransferStates.InProgress, events[1].Transfer.State);
                Assert.Equal(progressSize, events[1].Transfer.BytesTransferred);

                Assert.Equal(TransferStates.InProgress, events[2].Transfer.State);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync invokes ProgressUpdated delegate on data read"), AutoData]
        public async Task DownloadToFileAsync_Invokes_ProgressUpdated_Delegate_On_Data_Read(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(progressUpdated: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync opens stream with FileMode.Create if startOffset is 0"), AutoData]
        public async Task DownloadToFileAsync_Opens_Stream_With_FileMode_Create_If_StartOffset_Is_0(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)))
                .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(1, 1));

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            {
                var io = new Mock<IIOAdapter>();
                io.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(new FileStream(testFile.Path, FileMode.OpenOrCreate));

                using (var s = new SoulseekClient(options, ioAdapter: io.Object, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null);
                }

                io.Verify(m => m.GetFileStream(localFilename, FileMode.Create, FileAccess.Write, FileShare.None), Times.Once);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync opens stream with FileMode.Append if startOffset is greater than 0"), AutoData]
        public async Task DownloadToFileAsync_Opens_Stream_With_FileMode_Append_If_StartOffset_Is_Greater_Than_0(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)))
                .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(1, 1));

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            using (var testFile = new TestFile())
            {
                var io = new Mock<IIOAdapter>();
                io.Setup(m => m.GetFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                    .Returns(new FileStream(testFile.Path, FileMode.OpenOrCreate));

                using (var s = new SoulseekClient(options, ioAdapter: io.Object, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                    await s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 1, token, new TransferOptions(), null);
                }

                io.Verify(m => m.GetFileStream(localFilename, FileMode.Append, FileAccess.Write, FileShare.None), Times.Once);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync raises Download events on failure"), AutoData]
        public async Task DownloadToFileAsync_Raises_Download_Events_On_Failure(string username, IPEndPoint endpoint, string remoteFilename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, remoteFilename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // this fails the download
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, remoteFilename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync raises Download events on timeout"), AutoData]
        public async Task DownloadToFileAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // this fails the download
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.TimedOut, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync raises Download events on cancellation"), AutoData]
        public async Task DownloadToFileAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, string localFilename, int token)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException("Wait cancelled."));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(null, waiter: waiter.Object, serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<TransferStateChangedEventArgs>();

                s.TransferStateChanged += (sender, e) =>
                {
                    events.Add(e);
                };

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);

                Assert.Equal(TransferStates.Completed | TransferStates.Cancelled, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferException and ConnectionException on transfer exception"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferException_And_ConnectionException_On_Transfer_Exception(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new ConnectionReadException("connection ReadAsync wraps everything in ConnectionReadException", new NullReferenceException())));

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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsAssignableFrom<ConnectionException>(ex.InnerException);
                Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TimeoutException on transfer timeout"), AutoData]
        public async Task DownloadToFileAsync_Throws_TimeoutException_On_Transfer_Timeout(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task DownloadToFileAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<int, CancellationToken, Task<int>>>(), It.IsAny<Action<int, int, int>>(), It.IsAny<CancellationToken>()))
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
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws TransferRejectedException on transfer rejection"), AutoData]
        public async Task DownloadToFileAsync_Throws_TransferRejectedException_On_Transfer_Rejection(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Throws(new TransferRejectedException("foo"));

            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, 0L, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferRejectedException>(ex);
            }
        }

        [Trait("Category", "DownloadToFileAsync")]
        [Theory(DisplayName = "DownloadToFileAsync throws ConnectionException when transfer connection fails"), AutoData]
        public async Task DownloadToFileAsync_Throws_ConnectionException_When_Transfer_Connection_Fails(string username, IPEndPoint endpoint, string filename, string localFilename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, "Queued");
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var expected = new Exception("foo");

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.AwaitTransferConnectionAsync(username, filename, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(expected));

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task<Transfer>>("DownloadToFileAsync", username, filename, localFilename, (long?)size, 0, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal(expected, ex.InnerException);
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

        private class UnFlushableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanWrite => false;

            public override bool CanSeek => false;

            public override long Length => 0;

            public override long Position { get; set; }

            public override void Flush()
            {
                throw new IOException("failed to flush");
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
