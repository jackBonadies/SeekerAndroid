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
        [Theory(DisplayName = "UploadAsync throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("username", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

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
                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, "filename", 1, stream));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("username", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws ArgumentException given bad filename")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadAsync_Throws_ArgumentException_Given_Bad_Filename(string filename)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", filename, new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("filename", ex.Message, StringComparison.InvariantCultureIgnoreCase);
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
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", filename, 1, stream));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("filename", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws ArgumentException null or empty byte array")]
        [InlineData(null)]
        [InlineData(new byte[] { })]
        public async Task UploadAsync_Throws_ArgumentException_Given_Null_Or_Empty_Byte_Array(byte[] data)
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", data));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("data", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync stream throws ArgumentException bad length")]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-12413)]
        public async Task UploadAsync_Stream_Throws_ArgumentException_Given_Bad_Length(long length)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", length, stream));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
                Assert.Contains("length", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync throws InvalidOperationException when not connected")]
        public async Task UploadAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream throws InvalidOperationException when not connected")]
        public async Task UploadAsync_Stream_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, stream));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream throws ArgumentNullException given null stream")]
        public async Task UploadAsync_Stream_Throws_ArgumentNullException_Given_Null_Stream()
        {
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, null));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentNullException>(ex);
                Assert.Contains("stream is null", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync stream throws InvalidOperationException given unreadable stream")]
        public async Task UploadAsync_Stream_Throws_InvalidOperationException_Given_Unreadable_Stream()
        {
            using (var stream = new UnReadableWriteableStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, stream));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("not readable", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync throws InvalidOperationException when not logged in")]
        public async Task UploadAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
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

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, stream));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Fact(DisplayName = "UploadAsync throws DuplicateTokenException when token used")]
        public async Task UploadAsync_Throws_DuplicateTokenException_When_Token_Used()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(1, new TransferInternal(TransferDirection.Upload, "foo", "bar", 1));

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", new byte[] { 0x0 }, 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
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

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, stream, 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTokenException>(ex);
                Assert.Contains("token", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws DuplicateTransferException when an existing Upload matches the username and filename"), AutoData]
        public async Task UploadAsync_Throws_DuplicateTransferException_When_An_Existing_Upload_Matches_The_Username_And_Filename(string username, string filename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, new byte[] { 0x0 }, 1));

                Assert.NotNull(ex);
                Assert.IsType<DuplicateTransferException>(ex);
                Assert.Contains($"An active or queued upload of {filename} to {username} is already in progress", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync does not throw DuplicateTransferException when an existing Upload matches only the username"), AutoData]
        public async Task UploadAsync_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Username(string username, string filename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename + "!", new byte[] { 0x0 }, 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync does not throw DuplicateTransferException when an existing Upload matches only the filename"), AutoData]
        public async Task UploadAsync_Does_Not_Throw_DuplicateTransferException_When_An_Existing_Upload_Matches_Only_The_Filename(string username, string filename)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var queued = new ConcurrentDictionary<int, TransferInternal>();
                queued.TryAdd(0, new TransferInternal(TransferDirection.Upload, username, filename, 0));

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username + "!", filename, new byte[] { 0x0 }, 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
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

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, stream, 1));

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

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename + "!", 1, stream, 1));

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

                s.SetProperty("Uploads", queued);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username + "!", filename, 1, stream, 1));

                Assert.NotNull(ex);
                Assert.IsNotType<DuplicateTransferException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync uses given CancellationToken"), AutoData]
        public async Task UploadAsync_Uses_Given_CancellationToken(string username, string filename)
        {
            var cancellationToken = new CancellationToken(true);

            var conn = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: conn.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, new byte[] { 0x0 }, cancellationToken: cancellationToken));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
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

                var ex = await Record.ExceptionAsync(() => s.UploadAsync(username, filename, 1, stream, cancellationToken: cancellationToken));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadAsync")]
        [Theory(DisplayName = "UploadAsync throws TimeoutException on peer message connection timeout"), AutoData]
        public async Task UploadAsync_Throws_TimeoutException_On_Peer_Message_Connection_Timeout(IPEndPoint endpoint)
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

            using (var s = new SoulseekClient(waiter: waiter.Object, serverConnection: conn.Object, options: options, peerConnectionManager: manager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", new byte[] { 0x0 }));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
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

                var ex = await Record.ExceptionAsync(() => s.UploadAsync("username", "filename", 1, stream));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws UserOfflineException when user offline"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_UserOfflineException_When_User_Offline(string username, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(new UserOfflineException()));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws TimeoutException on TransferResponse timeout"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_TimeoutException_On_TransferResponse_Timeout(string username, IPEndPoint endpoint, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 1);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.Wait<TransferResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws OperationCanceledException on TransferResponse cancellation"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_OperationCanceledException_On_TransferResponse_Cancellation(string username, IPEndPoint endpoint, string filename, byte[] data, int token)
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws OperationCanceledException on request write cancellation"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_OperationCanceledException_On_Request_Write_Cancellation(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws TimeoutException on transfer response timeout"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_TimeoutException_On_Transfer_Response_Timeout(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync completes following normal transfer connection disconnect"), AutoData]
        public async Task UploadFromByteArrayAsync_Completes_Following_Normal_Transfer_Connection_Disconnect(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
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

                var task = s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(maximumLingerTime: 0), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws TimeoutException on unexpected transfer connection timeout"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_TimeoutException_On_Unexpected_Transfer_Connection_Timeout(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            waiter.Setup(m => m.Throw(It.IsAny<WaitKey>(), It.IsAny<TimeoutException>()))
                .Callback<WaitKey, Exception>((key, ex) => tcs.SetException(ex));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var timeoutEx = new TimeoutException("timed out");

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("timed out", timeoutEx));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
                Assert.Equal("timed out", ex.Message);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws OperationCanceledException on unexpected transfer connection cancellation"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_OperationCanceledException_On_Unexpected_Transfer_Connection_Cancellation(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            waiter.Setup(m => m.Throw(It.IsAny<WaitKey>(), It.IsAny<OperationCanceledException>()))
                .Callback<WaitKey, Exception>((key, ex) => tcs.SetException(ex));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var cancelEx = new OperationCanceledException("canceled");

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("canceled", cancelEx));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
                Assert.Equal("canceled", ex.Message);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws wrapped Exception on unexpected transfer connection Exception"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_Wrapped_Exception_On_Unexpected_Transfer_Connection_Exception(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);
            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            waiter.Setup(m => m.Throw(It.IsAny<WaitKey>(), It.IsAny<Exception>()))
                .Callback<WaitKey, Exception>((key, ex) => tcs.SetException(ex));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var transferConn = new Mock<IConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));
            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(transferConn.Object));

            var thrownEx = new Exception("some exception");

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null);

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

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync completes without Exception when transfer is allowed"), AutoData]
        public async Task UploadFromByteArrayAsync_Completes_Without_Exception_When_Transfer_Is_Allowed(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync completes without Exception when trailing read throws ConnectionReadException"), AutoData]
        public async Task UploadFromByteArrayAsync_Completes_Without_Exception_When_Trailing_Read_Throws_ConnectionReadException(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync completes without Exception after MaximumLingerTime when trailing read does not throw ConnectionReadException"), AutoData]
        public async Task UploadFromByteArrayAsync_Completes_Without_Exception_After_MaximumLingerTime_When_Trailing_Read_Does_Not_Throw_ConnectionReadException(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, txOptions, null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync produces warning diagnostic when disconnected due to MaximumLingerTime"), AutoData]
        public async Task UploadFromByteArrayAsync_Produces_Warning_Diagnostic_When_Disconnected_Due_To_MaximumLingerTime(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, txOptions, null));

                Assert.Null(ex);
            }

            diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("maximum linger time")), null), Times.Once);
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync disposes stream given dispose option flag"), AutoData]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "Discard")]
        public async Task UploadFromStreamAsync_Disposes_Stream_Given_Dispose_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: true);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, stream, token, txoptions, null));

                Assert.Null(ex);

                var ex2 = Record.Exception(() =>
                {
                    var p = stream.Position;
                });

                Assert.NotNull(ex2);
                Assert.IsType<ObjectDisposedException>(ex2);
            }
        }

        [Trait("Category", "UploadFromStreamAsync")]
        [Theory(DisplayName = "UploadFromStreamAsync does not dispose stream given false dispose option flag"), AutoData]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "Discard")]
        public async Task UploadFromStreamAsync_Does_Not_Dispose_Stream_Given_False_Dispose_Option_Flag(string username, IPEndPoint endpoint, string filename, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                var txoptions = new TransferOptions(disposeInputStreamOnCompletion: false);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, 1, stream, token, txoptions, null));

                Assert.Null(ex);

                var ex2 = Record.Exception(() =>
                {
                    var p = stream.Position;
                });

                Assert.Null(ex2);
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
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, stream, token, txoptions, null));

                Assert.Null(ex);
                Assert.Equal(offset, stream.Position);
            }
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
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new TransferException("foo", new NullReferenceException())));
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, stream, token, txoptions, null));

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
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromStreamAsync", username, filename, size, stream, token, txoptions, null));

                Assert.Null(ex);
                Assert.Equal(offset, stream.Position);
            }

            transferConn.Verify(m => m.WriteAsync(size - offset, It.IsAny<Stream>(), It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws TransferRejectedException when acknowledgement is disallowed and message contains 'File not shared'"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_TransferRejectedException_When_Acknowledgement_Is_Disallowed_And_File_Not_Shared(string username, IPEndPoint endpoint, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty); // reject
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferRejectedException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync invokes StateChanged delegate on state change"), AutoData]
        public async Task UploadFromByteArrayAsync_Invokes_StateChanged_Delegate_On_State_Change(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();

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

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var fired = false;

                await s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(stateChanged: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync raises UploadProgressUpdated event on data write"), AutoData]
        public async Task UploadFromByteArrayAsync_Raises_UploadProgressUpdated_Event_On_Data_Read(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
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
            transferConn.Setup(m => m.WriteAsync(It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Raises(m => m.DataWritten += null, this, new ConnectionDataEventArgs(1, 1));
            transferConn.Setup(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[size]));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
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

                var events = new List<TransferProgressUpdatedEventArgs>();

                s.TransferProgressUpdated += (d, e) => events.Add(e);

                await s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(maximumLingerTime: 0), null);

                Assert.Equal(3, events.Count);
                Assert.Equal(TransferStates.InProgress, events[0].Transfer.State);
                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, events[2].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync invokes ProgressUpdated delegate on data read"), AutoData]
        public async Task UploadFromByteArrayAsync_Invokes_ProgressUpdated_Delegate_On_Data_Read(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)))
                .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(1, 1));

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

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var fired = false;

                await s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(progressUpdated: (e) => fired = true), null);

                Assert.True(fired);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync raises Upload events on failure"), AutoData]
        public async Task UploadFromByteArrayAsync_Raises_Upload_Events_On_Failure(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
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

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new MessageReadException()));
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(maximumLingerTime: 0), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync raises Upload events on bad offset data"), AutoData]
        public async Task UploadFromByteArrayAsync_Raises_Upload_Events_On_Bad_Offset_Data(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
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
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new MessageReadException()));
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<MessageReadException>(ex.InnerException);

                Assert.Equal(TransferStates.Initializing, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.Errored, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync raises Upload events on timeout"), AutoData]
        public async Task UploadFromByteArrayAsync_Raises_Expected_Final_Event_On_Timeout(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
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

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new TimeoutException()));
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(maximumLingerTime: 0), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);

                Assert.Equal(TransferStates.InProgress, events[events.Count - 1].PreviousState);
                Assert.Equal(TransferStates.Completed | TransferStates.TimedOut, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync raises Upload events on cancellation"), AutoData]
        public async Task UploadFromByteArrayAsync_Raises_Expected_Final_Event_On_Cancellation(string username, string filename, byte[] data, int token)
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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);

                Assert.Equal(TransferStates.Completed | TransferStates.Cancelled, events[events.Count - 1].Transfer.State);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws SoulseekClientException and ConnectionException on transfer exception"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_SoulseekClientException_And_ConnectionException_On_Transfer_Exception(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new NullReferenceException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new ConnectionException("foo", new NullReferenceException())));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws SoulseekClientException on failure to read offset data"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_SoulseekClientException_On_Failure_To_Read_Offset_Data(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var innerException = new NullReferenceException();
            var outerException = new ConnectionException(string.Empty, innerException);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(outerException));
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.IsType<NullReferenceException>(ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws SoulseekClientException on bad offset data"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_SoulseekClientException_On_Bad_Offset_Data(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new ArgumentOutOfRangeException(nameof(size))));
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws TimeoutException on transfer timeout"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_TimeoutException_On_Transfer_Timeout(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
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

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new TimeoutException()));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
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

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new OperationCanceledException()));

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

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }

            transferConn.Verify(m => m.ReadAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws TransferRejectedException on transfer rejection"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_TransferRejectedException_On_Transfer_Rejection(string username, IPEndPoint endpoint, string filename, byte[] data, int token)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, string.Empty); // reject
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<TransferRejectedException>(ex);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync throws ConnectionException when transfer connection fails"), AutoData]
        public async Task UploadFromByteArrayAsync_Throws_ConnectionException_When_Transfer_Connection_Fails(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync updates remote user on failure"), AutoData]
        public void UploadFromByteArrayAsync_Updates_Remote_User_On_Failure(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Upload, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.ReadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<byte[]>(new NullReferenceException()));

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely<TransferRequest>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(request));
            waiter.Setup(m => m.Wait(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new ConnectionException("foo", new NullReferenceException())));

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

                var ex = Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(), null));

                Assert.NotNull(ex);
            }

            var expectedBytes = new UploadFailed(filename).ToByteArray();
            conn.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(expectedBytes)), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "UploadFromByteArrayAsync")]
        [Theory(DisplayName = "UploadFromByteArrayAsync swallows final read exception"), AutoData]
        public async Task UploadFromByteArrayAsync_Swallows_Final_Read_Exception(string username, IPEndPoint endpoint, string filename, byte[] data, int token, int size)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size);
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<TransferResponse>(It.Is<WaitKey>(w => w.Equals(responseWaitKey)), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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

            using (var s = new SoulseekClient(options: options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("UploadFromByteArrayAsync", username, filename, data, token, new TransferOptions(maximumLingerTime: int.MaxValue), null));

                Assert.Null(ex);
            }
        }

        private class UnReadableWriteableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanWrite => false;

            public override bool CanSeek => throw new NotImplementedException();

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
    }
}
