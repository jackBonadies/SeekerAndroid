// <copyright file="BrowseAsyncTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
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

    public class BrowseAsyncTests
    {
        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws ArgumentException given bad username")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task BrowseAsync_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.BrowseAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Fact(DisplayName = "BrowseAsync throws InvalidOperationException when not connected")]
        public async Task BrowseAsync_Throws_InvalidOperationException_When_Not_Connected()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.BrowseAsync("foo"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("Connected", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Fact(DisplayName = "BrowseAsync throws InvalidOperationException when not logged in")]
        public async Task BrowseAsync_Throws_InvalidOperationException_When_Not_Logged_In()
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = await Record.ExceptionAsync(() => s.BrowseAsync("foo"));

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
                Assert.Contains("logged in", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync returns expected response on success"), AutoData]
        public async Task BrowseAsync_Returns_Expected_Response_On_Success(string username, IPEndPoint endpoint, string localUsername, List<Directory> directories)
        {
            var response = new BrowseResponse(directories);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((new MessageReceivedEventArgs(1, new byte[] { 0x0 }), conn.Object)));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var result = await s.BrowseAsync(username);

                Assert.Equal(response, result);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync uses given CancellationToken"), AutoData]
        public async Task BrowseAsync_Uses_Given_CancellationToken(string username, IPEndPoint endpoint, string localUsername, List<Directory> directories, CancellationToken cancellationToken)
        {
            var response = new BrowseResponse(directories);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((new MessageReceivedEventArgs(1, new byte[] { 0x0 }), conn.Object)));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await s.BrowseAsync(username, cancellationToken: cancellationToken);
            }

            conn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), cancellationToken), Times.AtLeastOnce);
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync raises BrowseProgressUpdated event at least twice"), AutoData]
        public async Task BrowseAsync_Raises_BrowseProgressUpdated_Event_At_Least_Twice(string username, IPEndPoint endpoint, string localUsername, List<Directory> directories, int length)
        {
            var response = new BrowseResponse(directories);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((new MessageReceivedEventArgs(length, new byte[] { 0x0 }), conn.Object)));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<BrowseProgressUpdatedEventArgs>();
                s.BrowseProgressUpdated += (sender, args) => events.Add(args);

                await s.BrowseAsync(username);

                Assert.NotEmpty(events);
                Assert.Equal(2, events.Count);
                Assert.Equal(0, events[0].PercentComplete);
                Assert.Equal(100, events[1].PercentComplete);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync invokes ProgressUpdated Action at least twice"), AutoData]
        public async Task BrowseAsync_Invokes_ProgressUpdated_Action_At_Least_Twice(string username, IPEndPoint endpoint, string localUsername, List<Directory> directories, int length)
        {
            var response = new BrowseResponse(directories);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((new MessageReceivedEventArgs(length, new byte[] { 0x0 }), conn.Object)));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var events = new List<(string Username, long BytesTransferred, long BytesRemaining, double PercentComplete, long Size)>();

                await s.BrowseAsync(username, new BrowseOptions(progressUpdated: (args) => events.Add(args)));

                Assert.NotEmpty(events);
                Assert.Equal(2, events.Count);
                Assert.Equal(0, events[0].PercentComplete);
                Assert.Equal(100, events[1].PercentComplete);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws OperationCanceledException on cancellation"), AutoData]
        public async Task BrowseAsync_Throws_OperationCanceledException_On_Cancellation(string username, IPEndPoint endpoint, string localUsername)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<BrowseResponse>(new OperationCanceledException()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((new MessageReceivedEventArgs(1, new byte[] { 0x0 }), conn.Object)));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.BrowseAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws TimeoutException on timeout"), AutoData]
        public async Task BrowseAsync_Throws_TimeoutException_On_Timeout(string username, IPEndPoint endpoint, string localUsername)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()));
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<(MessageReceivedEventArgs, IMessageConnection)>(new TimeoutException()));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.BrowseAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws SoulseekClientException on write exception"), AutoData]
        public async Task BrowseAsync_Throws_SoulseekClientException_On_Write_Exception(string username, IPEndPoint endpoint, string localUsername)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => new MessageReader<MessageCode.Peer>(msg.ToByteArray()).ReadCode() == MessageCode.Peer.BrowseRequest), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.BrowseAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionWriteException>(ex.InnerException);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws wait on Exception"), AutoData]
        public async Task BrowseAsync_Throws_Wait_On_Exception(string username, IPEndPoint endpoint, string localUsername)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => new MessageReader<MessageCode.Peer>(msg.ToByteArray()).ReadCode() == MessageCode.Peer.BrowseRequest), It.IsAny<CancellationToken>()))
                .Throws(new ConnectionWriteException());

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.BrowseAsync(username));

                Assert.NotNull(ex);
            }

            waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.BrowseResponse, username), It.IsAny<Exception>()));
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws UserOfflineException on user not found"), AutoData]
        public async Task BrowseAsync_Throws_UserOfflineException_On_User_Not_Found(string username, IPEndPoint endpoint, string localUsername)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<UserAddressResponse>(new UserOfflineException()));

            var conn = new Mock<IMessageConnection>();

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.BrowseAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<UserOfflineException>(ex);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws SoulseekClientException on disconnect"), AutoData]
        public async Task BrowseAsync_Throws_SoulseekClientException_On_Disconnect(string username, IPEndPoint endpoint, string localUsername)
        {
            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<BrowseResponse>(new ConnectionException("disconnected unexpectedly")));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Raises(m => m.Disconnected += null, conn.Object, new ConnectionDisconnectedEventArgs(string.Empty));

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((new MessageReceivedEventArgs(1, new byte[] { 0x0 }), conn.Object)));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.BrowseAsync(username));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
                Assert.Contains("disconnected unexpectedly", ex.InnerException.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "BrowseAsync")]
        [Theory(DisplayName = "BrowseAsync throws SoulseekClientException on unexpected disconnect"), AutoData]
        public async Task BrowseAsync_Throws_SoulseekClientException_On_Unexpected_Disconnect(string username, IPEndPoint endpoint, string localUsername)
        {
            var tcs = new TaskCompletionSource<BrowseResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            var waiter = new Mock<IWaiter>();
            waiter.Setup(m => m.WaitIndefinitely<BrowseResponse>(It.IsAny<WaitKey>(), It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            waiter.Setup(m => m.Wait<UserAddressResponse>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UserAddressResponse(username, endpoint.Address, endpoint.Port)));

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connManager = new Mock<IPeerConnectionManager>();
            connManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            waiter.Setup(m => m.Wait<(MessageReceivedEventArgs, IMessageConnection)>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((new MessageReceivedEventArgs(1, new byte[] { 0x0 }), conn.Object)));
            waiter.Setup(m => m.Throw(It.IsAny<WaitKey>(), It.IsAny<Exception>()))
                .Callback<WaitKey, Exception>((key, ex) => tcs.SetException(ex));

            using (var s = new SoulseekClient(minorVersion: 9999, waiter: waiter.Object, serverConnection: conn.Object, peerConnectionManager: connManager.Object))
            {
                s.SetProperty("Username", localUsername);
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var task = s.BrowseAsync(username);

                conn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("foo"));

                var ex = await Record.ExceptionAsync(() => task);

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.IsType<ConnectionException>(ex.InnerException);
            }
        }
    }
}
