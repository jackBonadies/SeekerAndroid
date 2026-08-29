// <copyright file="EnqueueDownloadAsyncTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class EnqueueDownloadAsyncTests
    {
        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync throws immediately given bad input")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task EnqueueDownloadAsync_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, "filename", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync stream throws immediately given bad input")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task EnqueueDownloadAsync_Stream_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient())
            {
                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, "filename", () => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync returns download task after enqueue"), AutoData]
        public async Task EnqueueDownloadAsync_Returns_Download_Task_After_Enqueue(string username, string filename, string localFilename, long size, int token, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            // make download wait for our task completion source
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            // complete our TCS when the disconnected event handler fires
            waiter.Setup(m => m.Complete(It.IsAny<WaitKey>()))
                .Callback(() => tcs.TrySetResult(data));

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

                var downloadTask = await s.EnqueueDownloadAsync(username, filename, localFilename, (long?)size, 0, token);

                Assert.NotNull(downloadTask);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var transfer = await downloadTask;

                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, transfer.State);
                Assert.Equal(username, transfer.Username);
                Assert.Equal(token, transfer.Token);
                Assert.Equal(filename, transfer.Filename);
            }
        }

        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync stream returns download task after enqueue"), AutoData]
        public async Task EnqueueDownloadAsync_Stream_Returns_Download_Task_After_Enqueue(string username, string filename, long size, int token, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            // make download wait for our task completion source
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            // complete our TCS when the disconnected event handler fires
            waiter.Setup(m => m.Complete(It.IsAny<WaitKey>()))
                .Callback(() => tcs.TrySetResult(data));

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

                var downloadTask = await s.EnqueueDownloadAsync(username, filename, () => Task.FromResult((Stream)stream), (long?)size, 0, token);

                Assert.NotNull(downloadTask);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var transfer = await downloadTask;

                Assert.Equal(TransferStates.Completed | TransferStates.Succeeded, transfer.State);
                Assert.Equal(username, transfer.Username);
                Assert.Equal(token, transfer.Token);
                Assert.Equal(filename, transfer.Filename);
            }
        }

        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync stream throws download exception on error"), AutoData]
        public async Task EnqueueDownloadAsync_Stream_Throws_Download_Exception_On_Error(string username, string filename, long size, int token, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            // make download wait for our task completion source
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            // complete our TCS when the disconnected event handler fires
            waiter.Setup(m => m.Complete(It.IsAny<WaitKey>()))
                .Callback(() => tcs.TrySetResult(data));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            var expectedEx = new Exception("some problem");

            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(expectedEx));

            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var downloadTask = await s.EnqueueDownloadAsync(username, filename, () => Task.FromResult((Stream)stream), (long?)size, 0, token);

                Assert.NotNull(downloadTask);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var ex = await Record.ExceptionAsync(() => downloadTask);

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }
        }

        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync throws download exception on error"), AutoData]
        public async Task EnqueueDownloadAsync_Throws_Download_Exception_On_Error(string username, string filename, string localFilename, long size, int token, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            // make download wait for our task completion source
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            // complete our TCS when the disconnected event handler fires
            waiter.Setup(m => m.Complete(It.IsAny<WaitKey>()))
                .Callback(() => tcs.TrySetResult(data));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            var expectedEx = new Exception("some problem");

            connManager.Setup(m => m.GetTransferConnectionAsync(username, endpoint, token, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(expectedEx));

            using (var s = new SoulseekClient(options, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var downloadTask = await s.EnqueueDownloadAsync(username, filename, localFilename, (long?)size, 0, token);

                Assert.NotNull(downloadTask);

                transferConn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("done"));

                var ex = await Record.ExceptionAsync(() => downloadTask);

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }
        }

        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync stream throws download exception on error before queue"), AutoData]
        public async Task EnqueueDownloadAsync_Stream_Throws_Download_Exception_On_Error_Before_Queue(string username, string filename, long size, int token, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            var expectedEx = new Exception("some problem");

            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(expectedEx));

            // make download wait for our task completion source
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            // complete our TCS when the disconnected event handler fires
            waiter.Setup(m => m.Complete(It.IsAny<WaitKey>()))
                .Callback(() => tcs.TrySetResult(data));

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

                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, filename, () => Task.FromResult((Stream)stream), 0L, 0, token));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<UserEndPointException>(ex.InnerException);
                Assert.Equal(expectedEx, ex.InnerException.InnerException);
            }
        }

        [Trait("Category", "EnqueueDownloadAsync")]
        [Theory(DisplayName = "EnqueueDownloadAsync throws download exception on error before queue"), AutoData]
        public async Task EnqueueDownloadAsync_Throws_Download_Exception_On_Error_Before_Queue(string username, string filename, string localFilename, long size, int token, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(messageTimeout: 5);

            var response = new TransferResponse(token, size); // allowed, will start download immediately
            var responseWaitKey = new WaitKey(MessageCode.Peer.TransferResponse, username, token);

            var request = new TransferRequest(TransferDirection.Download, token, filename, size);

            var transferConn = new Mock<IConnection>();
            transferConn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            var expectedEx = new Exception("some problem");

            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(expectedEx));

            // make download wait for our task completion source
            waiter.Setup(m => m.WaitIndefinitely(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

            // complete our TCS when the disconnected event handler fires
            waiter.Setup(m => m.Complete(It.IsAny<WaitKey>()))
                .Callback(() => tcs.TrySetResult(data));

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

                var ex = await Record.ExceptionAsync(() => s.EnqueueDownloadAsync(username, filename, localFilename, 0L, 0, token));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<UserEndPointException>(ex.InnerException);
                Assert.Equal(expectedEx, ex.InnerException.InnerException);
            }
        }
    }
}
