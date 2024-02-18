// <copyright file="PeerConnectionManagerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class PeerConnectionManagerTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            PeerConnectionManager c = null;

            var ex = Record.Exception(() => (c, _) = GetFixture());

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(0, c.MessageConnections.Count);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if no SoulseekClient given")]
        public void Throws_If_SoulseekClient_Given()
        {
            var ex = Record.Exception(() => _ = new PeerConnectionManager(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("soulseekClient", ((ArgumentNullException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Ensures Diagnostic given null")]
        public void Ensures_Diagnostic_Given_Null()
        {
            using (var client = new SoulseekClient(options: null))
            {
                PeerConnectionManager c = default;

                var ex = Record.Exception(() => c = new PeerConnectionManager(client));

                Assert.Null(ex);
                Assert.NotNull(c.GetProperty<IDiagnosticFactory>("Diagnostic"));
            }
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var (manager, mocks) = GetFixture();

            using (manager)
            using (var c = new PeerConnectionManager(mocks.Client.Object))
            {
                var ex = Record.Exception(() => c.Dispose());

                Assert.Null(ex);
            }
        }

        [Trait("Category", "RemoveAndDisposeAll")]
        [Fact(DisplayName = "RemoveAndDisposeAll removes and disposes all")]
        public void RemoveAndDisposeAll_Removes_And_Disposes_All()
        {
            var (manager, _) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            using (manager)
            {
                var peer = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
                peer.GetOrAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

                manager.SetProperty("MessageConnectionDictionary", peer);

                var solicitations = new ConcurrentDictionary<int, string>();
                solicitations.TryAdd(1, "bar");

                manager.SetProperty("PendingSolicitationDictionary", solicitations);

                manager.RemoveAndDisposeAll();

                Assert.Empty(manager.PendingSolicitations);
                Assert.Empty(manager.MessageConnections);
            }
        }

        [Trait("Category", "RemoveAndDisposeAll")]
        [Fact(DisplayName = "RemoveAndDisposeAll does not throw on null values")]
        public void RemoveAndDisposeAll_Does_Not_Throw_On_Null_Values()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                var peer = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
                peer.GetOrAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult<IMessageConnection>(null)));

                manager.SetProperty("MessageConnectionDictionary", peer);

                var solicitations = new ConcurrentDictionary<int, string>();
                solicitations.TryAdd(1, "bar");

                manager.SetProperty("PendingSolicitationDictionary", solicitations);

                manager.RemoveAndDisposeAll();

                Assert.Empty(manager.PendingSolicitations);
                Assert.Empty(manager.MessageConnections);
            }
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync reads token and returns connection"), AutoData]
        internal async Task AddTransferConnectionAsync_Reads_Token_And_Returns_Connection(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var (connection, remoteToken) = await manager.GetTransferConnectionAsync(username, token, incomingConn.Object);

                Assert.Equal(conn.Object, connection);
                Assert.Equal(token, remoteToken);
            }
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync disposes connection on exception"), AutoData]
        internal async Task AddTransferConnectionAsync_Disposes_Connection_On_Exception(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync produces diagnostic on disconnect"), AutoData]
        internal async Task AddTransferConnectionAsync_Produces_Diagnostic_On_Disconnect(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Callback<long, CancellationToken?>((i, t) => conn.Raise(mock => mock.Disconnected += null, null, new ConnectionDisconnectedEventArgs("foo")))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("disconnected", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync sets connection type to inbound direct"), AutoData]
        internal async Task AddTransferConnectionAsync_Sets_Connection_Type_To_Inbound_Direct(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.GetTransferConnectionAsync(username, token, incomingConn.Object);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Inbound | ConnectionTypes.Direct);
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync produces expected diagnostic on failure"), AutoData]
        internal async Task AddTransferConnectionAsync_Produces_Expected_Diagnostic_On_Failure(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromException<byte[]>(new Exception("foo")));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));

                Assert.NotNull(ex);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish an inbound transfer connection"))));
        }

        [Trait("Category", "AddTransferConnectionAsync")]
        [Theory(DisplayName = "AddTransferConnectionAsync throws expected exception on failure"), AutoData]
        internal async Task AddTransferConnectionAsync_Throws_Expected_Exception_On_Failure(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromException<byte[]>(new Exception("foo")));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, token, incomingConn.Object));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.True(ex.Message.ContainsInsensitive("Failed to establish an inbound transfer connection"));
                Assert.IsType<Exception>(ex.InnerException);
                Assert.True(ex.InnerException.Message.ContainsInsensitive("foo"));
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync starts reading"), AutoData]
        internal async Task AddMessageConnectionAsync_Starts_Reading(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object);
            }

            conn.Verify(m => m.StartReadingContinuously());
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync disposes connection and throws if start reading throws"), AutoData]
        internal async Task AddMessageConnectionAsync_Disposes_Connection_And_Throws_If_Start_Reading_Throws(string username, IPEndPoint endpoint, int token)
        {
            var thrown = new Exception();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            conn.Setup(m => m.StartReadingContinuously())
                .Throws(thrown);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                var caught = await Record.ExceptionAsync(() => manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object));

                Assert.NotNull(caught);
                Assert.IsType<ConnectionException>(caught);
                Assert.Equal(thrown, caught.InnerException);
            }

            conn.Verify(m => m.StartReadingContinuously(), Times.Once);
            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync adds connection"), AutoData]
        internal async Task AddMessageConnectionAsync_Adds_Connection(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object);

                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPEndPoint == endpoint);
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync replaces duplicate connection and does not dispose old"), AutoData]
        internal async Task AddMessageConnectionAsync_Replaces_Duplicate_Connection_And_Does_Not_Dispose_Old(string username, IPEndPoint endpoint, int token)
        {
            var conn1 = GetMessageConnectionMock(username, endpoint);
            conn1.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn1.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var conn2 = GetMessageConnectionMock(username, endpoint);
            conn2.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn2.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object);

                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPEndPoint == endpoint);

                // swap in the second connection
                mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                    .Returns(conn2.Object);

                // call this again to force the first connection out and second in its place
                await manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object);

                // make sure we still have just the one
                Assert.Single(manager.MessageConnections);
                Assert.Contains(manager.MessageConnections, c => c.Username == username && c.IPEndPoint == endpoint);

                // verify that the first connection was disposed
                conn1.Verify(m => m.Dispose(), Times.Never);
                conn1.Verify(m => m.Disconnect(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync does not throw if fetch of cached throws"), AutoData]
        internal async Task AddMessageConnectionAsync_Does_Not_Throw_If_Fetch_Of_Cached_Throws(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            var incomingConn = GetConnectionMock(endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // set the dictionary up with an entry that will throw when fetched
            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromException<IMessageConnection>(new Exception())));

            using (manager)
            {
                manager.SetProperty("MessageConnectionDictionary", dict);

                var ex = await Record.ExceptionAsync(() => manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync cancels pending indirect connection"), AutoData]
        internal async Task AddMessageConnectionAsync_Cancels_Pending_Indirect_Connection(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            var incomingConn = GetConnectionMock(endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(GetMessageConnectionMock(username, endpoint).Object)));

            using (manager)
            using (var cts = new CancellationTokenSource())
            {
                manager.SetProperty("MessageConnectionDictionary", dict);

                var pendingDict = new ConcurrentDictionary<string, CancellationTokenSource>();
                pendingDict.TryAdd(username, cts);

                manager.SetProperty("PendingInboundIndirectConnectionDictionary", pendingDict);

                await manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object);

                Assert.True(cts.IsCancellationRequested);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Cancelling pending inbound indirect"))));
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync throws expected exception on failure"), AutoData]
        internal async Task AddMessageConnectionAsync_Throws_Expected_Exception_Failure(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);
            incomingConn.Setup(m => m.HandoffTcpClient())
                .Throws(new Exception("foo"));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.True(ex.Message.ContainsInsensitive("Failed to establish an inbound message connection"));
                Assert.IsType<Exception>(ex.InnerException);
                Assert.Equal("foo", ex.InnerException.Message);
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync purges cache on failure"), AutoData]
        internal async Task AddMessageConnectionAsync_Purges_Cache_On_Failure(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);
            incomingConn.Setup(m => m.HandoffTcpClient())
                .Throws(new Exception("foo"));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object));

                Assert.NotNull(ex);

                Assert.Empty(manager.MessageConnections);
            }
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync produces expected diagnostic on failure"), AutoData]
        internal async Task AddMessageConnectionAsync_Produces_Expected_Diagnostic_On_Failure(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);
            incomingConn.Setup(m => m.HandoffTcpClient())
                .Throws(new Exception("foo"));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object));

                Assert.NotNull(ex);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish an inbound message connection"))));
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Purging message connection cache of failed connection"))));
        }

        [Trait("Category", "AddMessageConnectionAsync")]
        [Theory(DisplayName = "AddMessageConnectionAsync sets connection type to inbound direct"), AutoData]
        internal async Task AddMessageConnectionAsync_Sets_Connection_Type_To_Inbound_Direct(string username, IPEndPoint endpoint, int token)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var incomingConn = GetConnectionMock(endpoint);

            using (manager)
            {
                await manager.AddOrUpdateMessageConnectionAsync(username, incomingConn.Object);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Inbound | ConnectionTypes.Direct);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync CTPR connects and pierces firewall"), AutoData]
        internal async Task GetTransferConnectionAsync_CTPR_Connects_And_Pierces_Firewall(string username, IPEndPoint endpoint, int token, bool isPrivileged)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, isPrivileged);
            var expectedBytes = new PierceFirewall(token).ToByteArray();
            byte[] actualBytes = Array.Empty<byte>();

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask)
                .Callback<byte[], CancellationToken?>((b, c) => actualBytes = b);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            (IConnection Connection, int RemoteToken) newConn;

            using (manager)
            {
                newConn = await manager.GetTransferConnectionAsync(ctpr);
            }

            Assert.Equal(endpoint.Address, newConn.Connection.IPEndPoint.Address);
            Assert.Equal(endpoint.Port, newConn.Connection.IPEndPoint.Port);
            Assert.Equal(token, newConn.RemoteToken);

            Assert.Equal(expectedBytes, actualBytes);

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken?>()), Times.Once);
            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync CTPR disposes connection if connect fails"), AutoData]
        internal async Task GetTransferConnectionAsync_CTPR_Disposes_Connection_If_Connect_Fails(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedException, ex.InnerException);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync adds diagnostic on disconnect"), AutoData]
        internal async Task GetTransferConnectionAsync_Adds_Diagnostic_On_Disconnect(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Callback(() => conn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("disconnect")))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedException, ex.InnerException);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Transfer connection") && s.ContainsInsensitive("disconnected"))));
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync CTPR sets type to inbound indirect"), AutoData]
        public async Task GetTransferConnectionAsync_CTPR_Sets_Type_To_Inbound_Indirect(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ReadAsync(4, null))
                .Returns(Task.FromResult(BitConverter.GetBytes(token)));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                await manager.GetTransferConnectionAsync(ctpr);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Inbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync CTPR produces expected diagnostic on failure"), AutoData]
        public async Task GetTransferConnectionAsync_CTPR_Produces_Expected_Diagnostic_On_Failure(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, "F", endpoint, token, false);
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(ctpr));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish an inbound indirect transfer connection"))), Times.Once);
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync disposes connection if connect fails"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Disposes_Connection_If_Connect_Fails(IPEndPoint endpoint, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync returns connection if connect succeeds"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Returns_Connection_If_Connect_Succeeds(IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync adds diagnostic on disconnect"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Adds_Diagnostic_On_Disconnect(IPEndPoint endpoint, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Callback(() => conn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("disconnected")))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Transfer connection") && s.ContainsInsensitive("disconnected"))));
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync sets connection type to Outbound Direct"), AutoData]
        internal async Task GetTransferConnectionOutboundDirectAsync_Sets_Connection_Type_To_Outbound_Direct(IPEndPoint endpoint, int token)
        {
            ConnectionTypes type = ConnectionTypes.None;

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.SetupSet(m => m.Type = It.IsAny<ConnectionTypes>())
                .Callback<ConnectionTypes>(o => type = o);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None))
            {
                Assert.Equal(ConnectionTypes.Outbound | ConnectionTypes.Direct, type);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct);
        }

        [Trait("Category", "GetTransferOutboundDirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundDirectAsync produces expected diagnostics on failure"), AutoData]
        public async Task GetTransferConnectionOutboundDirectAsyncnc_Produces_Expected_Diagnostic_On_Failure(IPEndPoint endpoint, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundDirectAsync", endpoint, token, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish a direct transfer connection"))), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync sends ConnectToPeerRequest"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Sends_ConnectToPeerRequest(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(b => true), CancellationToken.None));
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync throws if wait throws"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Throws_If_Wait_Throws(IPEndPoint endpoint, string username, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync hands off ITcpConnection"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Hands_Off_ITcpConnection(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            conn.Verify(m => m.HandoffTcpClient(), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync sets connection type to Outbound Indirect"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Sets_Connection_Type_To_Outbound_Indirect(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync adds and removes from PendingSolicitationDictionary"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Adds_And_Removes_From_PendingSolicitationDictionary(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                List<KeyValuePair<int, string>> pending = new List<KeyValuePair<int, string>>();

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                    .Callback<WaitKey, int?, CancellationToken?>((w, i, c) => pending = manager.GetProperty<ConcurrentDictionary<int, string>>("PendingSolicitationDictionary").ToList())
                    .Returns(Task.FromResult(conn.Object));

                using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
                {
                    Assert.Equal(conn.Object, newConn);

                    Assert.Single(pending);
                    Assert.Equal(username, pending[0].Value);
                    Assert.Empty(manager.PendingSolicitations);
                }
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync produces expected diagnostic on failure"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Produces_Expected_Diagnostic_On_Failure(IPEndPoint endpoint, string username, int token)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish an indirect transfer connection"))), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetTransferConnectionOutboundIndirectAsync adds diagnostic on disconnect"), AutoData]
        internal async Task GetTransferConnectionOutboundIndirectAsync_Adds_Diagnostic_On_Disconnect(IPEndPoint endpoint, string username, int token)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("established"))))
                .Callback(() => conn.Raise(m => m.Disconnected += null, new ConnectionDisconnectedEventArgs("disconnected")));

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.IsAny<IPEndPoint>(), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IConnection>>("GetTransferConnectionOutboundIndirectAsync", username, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, newConn);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("disconnected"))), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync returns direct connection when direct connects first"), AutoData]
        internal async Task GetTransferConnectionAsync_Returns_Direct_Connection_When_Direct_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync returns indirect connection when indirect connects first"), AutoData]
        internal async Task GetTransferConnectionAsync_Returns_Indirect_Connection_When_Indirect_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(indirect.Object));

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                Assert.Equal(indirect.Object, newConn);
                Assert.Equal(ConnectionTypes.Indirect, newConn.Type);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync throws ConnectionException when direct and indirect connections fail"), AutoData]
        internal async Task GetTransferConnectionAsync_Throws_ConnectionException_When_Direct_And_Indirect_Connections_Fail(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync generates expected diagnostics on successful connection"), AutoData]
        internal async Task GetTransferConnectionAsync_Generates_Expected_Diagnostics(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Attempting simultaneous direct and indirect transfer connections"))));
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"established first, attempting to cancel"))));
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("transfer connection to") && s.ContainsInsensitive("established."))));
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync produces expected diagnostics on connection failure"), AutoData]
        public async Task GetTransferConnectionAsync_Produces_Expected_Diagnostic_On_Failure(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish a direct or indirect transfer connection"))), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync produces expected diagnostics on negotiation failure"), AutoData]
        public async Task GetTransferConnectionAsync_Produces_Expected_Diagnostic_On_Negotiation_Failure(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask); // succeeds
            direct.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(new ConnectionException());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to negotiate transfer connection"))), Times.Once);
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync sends PeerInit on direct connection established"), AutoData]
        internal async Task GetTransferConnectionAsync_Sends_PeerInit_On_Direct_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Transfer, token).ToByteArray();

            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);

                direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(peerInit)), It.IsAny<CancellationToken?>()), Times.Once);
            }
        }

        [Trait("Category", "GetTransferConnectionAsync")]
        [Theory(DisplayName = "GetTransferConnectionAsync writes token on connection established"), AutoData]
        internal async Task GetTransferConnectionAsync_Writes_Token_On_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetConnectionMock(dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetConnectionMock(iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == directPort), It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetTransferConnection(It.Is<IPEndPoint>(e => e.Port == indirectPort), It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetTransferConnectionAsync(username, dendpoint, token, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);

                direct.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(BitConverter.GetBytes(token))), It.IsAny<CancellationToken?>()), Times.Once);
            }
        }

        [Trait("Category", "MessageConnectionProvisional_Disconnected")]
        [Theory(DisplayName = "MessageConnectionProvisional_Disconnected disposes connection"), AutoData]
        internal void MessageConnectionProvisional_Disconnected_Disposes_Connection(string message)
        {
            var conn = new Mock<IMessageConnection>();
            var (manager, mocks) = GetFixture();

            using (manager)
            {
                manager.InvokeMethod("MessageConnectionProvisional_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected removes and disposes connection"), AutoData]
        internal void MessageConnection_Disconnected_Removes_And_Disposes_Connection(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, mocks) = GetFixture();

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Empty(dict);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected generates diagnostic on removal"), AutoData]
        internal void MessageConnection_Disconnected_Generates_Diagnostic_On_Removal(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, mocks) = GetFixture();

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Contains(diagnostics, m => m.ContainsInsensitive($"Removed message connection record for {username}"));
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected does not throw if connection isn't tracked"), AutoData]
        internal void MessageConnection_Disconnected_Does_Not_Throw_If_Connection_Isnt_Tracked(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();

            var (manager, mocks) = GetFixture();

            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Empty(dict);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "MessageConnection_Disconnected")]
        [Theory(DisplayName = "MessageConnection_Disconnected does not generate removed diagnostic if connection isn't tracked"), AutoData]
        internal void MessageConnection_Disconnected_Does_Generate_Diagnostic_If_Connection_Isnt_Tracked(string username, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Key)
                .Returns(new ConnectionKey(username, new IPEndPoint(IPAddress.None, 0)));
            conn.Setup(m => m.Username)
                .Returns(username);

            var (manager, mocks) = GetFixture();

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            manager.SetProperty("MessageConnectionDictionary", dict);

            using (manager)
            {
                manager.InvokeMethod("MessageConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("removed"))), Times.Never);
            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync connects and returns connection if connect succeeds"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Connects_And_Returns_Connection_If_Connect_Succeeds(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, endpoint, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actualConn);
            }

            conn.Verify(m => m.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync disposes connection if connect fails"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Disposes_Connection_If_Connect_Fails(string username, IPEndPoint endpoint)
        {
            var expectedEx = new Exception("foo");

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, endpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedEx, ex);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync sets type to Outbound Direct"), AutoData]
        internal async Task GetMessageConnectionOutboundDirectAsync_Sets_Type_To_Outbound_Direct(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);
            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, endpoint, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actualConn);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct);
        }

        [Trait("Category", "GetMessageConnectionOutboundDirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundDirectAsync produces expected diagnostics on failure"), AutoData]
        public async Task GetMessageConnectionOutboundDirectAsync_Produces_Expected_Diagnostics_On_Failure(string username, IPEndPoint endpoint)
        {
            var expectedEx = new Exception("foo");

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundDirectAsync", username, endpoint, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish a direct message connection"))), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync sends ConnectToPeerRequest"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Sends_ConnectToPeerRequest(IPEndPoint endpoint, string username, int solicitationToken)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, solicitationToken, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, newConn);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(b => true), CancellationToken.None));
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync throws if wait throws"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Throws_If_Wait_Throws(IPEndPoint endpoint, string username, int solicitationToken)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, solicitationToken, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expectedException, ex);
            }
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync hands off ITcpConnection"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Hands_Off_ITcpConnection(IPEndPoint endpoint, string username, int solicitationToken)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, solicitationToken, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, newConn);
            }

            conn.Verify(m => m.HandoffTcpClient(), Times.Once);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync sets connection context to Indirect"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Sets_Connection_Context_To_Indirect(IPEndPoint endpoint, string username, int solicitationToken)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, solicitationToken, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, newConn);
            }

            msgConn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync adds and removes from PendingSolicitationDictionary"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Adds_And_Removes_From_PendingSolicitationDictionary(IPEndPoint endpoint, string username, int solicitationToken)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            using (manager)
            {
                List<KeyValuePair<int, string>> pending = new List<KeyValuePair<int, string>>();

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                    .Callback<WaitKey, int?, CancellationToken?>((w, i, c) => pending = manager.GetProperty<ConcurrentDictionary<int, string>>("PendingSolicitationDictionary").ToList())
                    .Returns(Task.FromResult(conn.Object));

                using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, solicitationToken, CancellationToken.None))
                {
                    Assert.Equal(msgConn.Object, newConn);

                    Assert.Single(pending);
                    Assert.Equal(username, pending[0].Value);
                    Assert.Empty(manager.PendingSolicitations);
                }
            }

            msgConn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync does not call StartReadingContinuously"), AutoData]
        internal async Task GetMessageConnectionOutboundIndirectAsync_Does_Not_Call_StartReadingContinuously(IPEndPoint endpoint, string username, int solicitationToken)
        {
            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            using (manager)
            {
                List<KeyValuePair<int, string>> pending = new List<KeyValuePair<int, string>>();

                mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                    .Callback<WaitKey, int?, CancellationToken?>((w, i, c) => pending = manager.GetProperty<ConcurrentDictionary<int, string>>("PendingSolicitationDictionary").ToList())
                    .Returns(Task.FromResult(conn.Object));

                using (var newConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, solicitationToken, CancellationToken.None))
                {
                    Assert.Equal(msgConn.Object, newConn);
                }
            }

            msgConn.Verify(m => m.StartReadingContinuously(), Times.Never);
        }

        [Trait("Category", "GetMessageConnectionOutboundIndirectAsync")]
        [Theory(DisplayName = "GetMessageConnectionOutboundIndirectAsync produces expected diagnostic on failure"), AutoData]
        public async Task GetMessageConnectionOutboundIndirectAsync_Produces_Expected_Diagnostic_On_Failure(IPEndPoint endpoint, string username, int solicitationToken)
        {
            var expectedException = new Exception("foo");

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedException);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetMessageConnectionOutboundIndirectAsync", username, solicitationToken, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish an indirect message connection"))), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns existing connection if exists"), AutoData]
        internal async Task GetOrAddMessageConnectionAsyncCTPR_Returns_Existing_Connection_If_Exists(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, _) = GetFixture();

            using (manager)
            using (var sem = new SemaphoreSlim(1, 1))
            {
                manager.SetProperty("MessageConnectionDictionary", dict);

                using (var existingConn = await manager.GetOrAddMessageConnectionAsync(ctpr))
                {
                    Assert.Equal(conn.Object, existingConn);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync updates PendingInboundDirectConnectionDictionary if key exists"), AutoData]
        internal async Task GetOrAddMessageConnectionAsyncCTPR_Updates_PendingInboundDirectConnectionDictionary_If_Key_Exists(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            var ct = new CancellationTokenSource(99999);
            var dict = new ConcurrentDictionary<string, CancellationTokenSource>();
            dict.GetOrAdd(username, ct);

            using (manager)
            {
                manager.SetProperty("PendingInboundIndirectConnectionDictionary", dict);

                using (var newConn = await manager.GetOrAddMessageConnectionAsync(ctpr))
                {
                    Assert.Equal(conn.Object, newConn);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync connects and returns new if not existing"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Connects_And_Returns_New_If_Not_Existing(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                using (var newConn = await manager.GetOrAddMessageConnectionAsync(ctpr))
                {
                    Assert.Equal(conn.Object, newConn);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync disposes connection and throws on connect failure"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Disposes_Connection_And_Throws_On_Connect_Failure(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception();
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }

            conn.Verify(m => m.Dispose());
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync disposes connection and throws on write failure"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Disposes_Connection_And_Throws_On_Write_Failure(string username, IPEndPoint endpoint, int token)
        {
            var expectedEx = new Exception();
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }

            conn.Verify(m => m.Dispose());
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync pierces firewall with correct token"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Pierces_Firewall_With_Correct_Token(string username, IPEndPoint endpoint, int token)
        {
            var expectedMessage = new PierceFirewall(token).ToByteArray();
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                (await manager.GetOrAddMessageConnectionAsync(ctpr)).Dispose();
            }

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(expectedMessage)), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR generates expected diagnostic on successful connection"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Generates_Expected_Diagnostic_On_Successful_Connection(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var newConn = await manager.GetOrAddMessageConnectionAsync(ctpr);

                Assert.NotNull(newConn);

                newConn.Dispose();
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Attempting inbound indirect message connection"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Message connection to {username}") && s.ContainsInsensitive("established"))), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR purges cache on failure"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Purges_Cache_On_Failure(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(new Exception("foo")));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(ctpr));

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");

                Assert.Empty(dict);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Purging message connection cache"))), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR produces warning and replaces if wrong connection is purged"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Produces_Warning_And_Replaces_If_Wrong_Connection_Is_Purged(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var (manager, mocks) = GetFixture();

            var directConn = new Mock<IMessageConnection>();
            directConn.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Callback(() =>
                {
                    var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");
                    var record = new Lazy<Task<IMessageConnection>>(() => Task.FromResult(directConn.Object));
                    dict.AddOrUpdate(username, record, (k, v) => record);
                })
                .Returns(Task.FromException(new Exception("foo")));

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(ctpr));

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");

                Assert.NotEmpty(dict);

                dict.TryGetValue(username, out var remainingRecord);

                var remainingConn = await remainingRecord.Value;

                Assert.Equal(directConn.Object, remainingConn);
            }

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("Erroneously purged direct message connection")), It.IsAny<Exception>()), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR produces expected diagnostics on failure"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Produces_Expected_Diagnostics_On_Failure(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(new Exception("foo")));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(ctpr));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Attempting inbound indirect message connection"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish an inbound indirect message connection"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Purging message connection cache"))), Times.Once);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR throws expected exception on failure"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Throws_Expected_Exception_On_Failure(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);
            var expectedEx = new Exception("foo");

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(expectedEx));

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR caches connection if uncached"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Caches_Connection_If_Uncached(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            using (var c = await manager.GetOrAddMessageConnectionAsync(ctpr).ConfigureAwait(false))
            {
                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");

                Assert.NotEmpty(dict);
                Assert.True(dict.ContainsKey(username));

                Assert.True(dict.TryGetValue(username, out var record));

                using (var cached = await record.Value.ConfigureAwait(false))
                {
                    Assert.Equal(conn.Object, cached);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR returns cached connection if cached"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Returns_Cached_Connection_If_Cached(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            using (manager)
            {
                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");
                dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

                var c = await manager.GetOrAddMessageConnectionAsync(ctpr).ConfigureAwait(false);

                Assert.Equal(conn.Object, c);

                c.Dispose();
            }

            mocks.ConnectionFactory.Verify(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null), Times.Never);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync CTPR sets connection type to inbound indirect"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_CTPR_Sets_Connection_Type_To_Inbound_Indirect(string username, IPEndPoint endpoint, int token)
        {
            var ctpr = new ConnectToPeerResponse(username, Constants.ConnectionType.Peer, endpoint, token, false);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var (manager, mocks) = GetFixture();

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(conn.Object);

            using (manager)
            {
                (await manager.GetOrAddMessageConnectionAsync(ctpr)).Dispose();
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Inbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns existing connection if exists"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Returns_Existing_Connection_If_Exists(string username, IPEndPoint endpoint)
        {
            var conn = GetMessageConnectionMock(username, endpoint);

            var dict = new ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>();
            dict.GetOrAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var (manager, _) = GetFixture();

            using (manager)
            using (var sem = new SemaphoreSlim(1, 1))
            {
                manager.SetProperty("MessageConnectionDictionary", dict);

                using (var existingConn = await manager.GetOrAddMessageConnectionAsync(username, endpoint, CancellationToken.None))
                {
                    Assert.Equal(conn.Object, existingConn);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns direct connection when direct connects first"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Returns_Direct_Connection_When_Direct_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IMessageConnection>(It.Is<WaitKey>(k => k.TokenParts.Contains(Constants.WaitKey.SolicitedPeerConnection)), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns indirect connection when indirect connects first"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Returns_Indirect_Connection_When_Indirect_Connects_First(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var incomingIndirect = GetConnectionMock(iendpoint);
            incomingIndirect.Setup(m => m.IPEndPoint)
                .Returns(iendpoint);
            incomingIndirect.Setup(m => m.HandoffTcpClient())
                .Returns(new Mock<ITcpClient>().Object);

            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(incomingIndirect.Object));

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Equal(indirect.Object, newConn);
                Assert.Equal(ConnectionTypes.Indirect, newConn.Type);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync throws ConnectionException when direct and indirect connections fail"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Throws_ConnectionException_When_Direct_And_Indirect_Connections_Fail(string localUsername, string username, IPAddress ipAddress, int directPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync generates expected diagnostics on successful connection"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Generates_Expected_Diagnostics(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IMessageConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            List<string> diagnostics = new List<string>();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(s => diagnostics.Add(s));

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Contains(diagnostics, s => s.ContainsInsensitive("Attempting simultaneous direct and indirect message connections"));
                Assert.Contains(diagnostics, s => s.ContainsInsensitive($"established first, attempting to cancel"));
                Assert.Contains(
                    diagnostics,
                    s => s.ContainsInsensitive("message connection to") && s.ContainsInsensitive("established."));
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync caches connection if uncached"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Caches_Connection_If_Uncached(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");

                Assert.NotEmpty(dict);
                Assert.True(dict.TryGetValue(username, out var record));

                using (var cached = await record.Value)
                {
                    Assert.Equal(direct.Object, cached);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync returns cached connection if cached"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Returns_Cached_Connection_If_Cached(string username, IPAddress ipAddress, int directPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var (manager, mocks) = GetFixture();

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");
            dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(Task.FromResult(direct.Object)));

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.True(dict.TryGetValue(username, out var record));

                using (var cached = await record.Value)
                {
                    Assert.Equal(direct.Object, cached);
                }
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync starts reading continuously if indirect"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Starts_Reading_Continuously_If_Indirect(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(new ConnectionException()));

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult((IConnection)indirect.Object));

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                indirect.Verify(m => m.StartReadingContinuously(), Times.Once);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync produces expected diagnostic on negotiation failure"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Produces_Expected_Diagnostics_On_Negotiation_Failure(string username, IPAddress ipAddress, int directPort, string localUsername)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            direct.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(new ConnectionException());

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to negotiate message connection"))));
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Purging message connection cache of failed connection"))));
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync throws expected Exception on negotiation failure"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Throws_Expected_Exception_On_Negotiation_Failure(string username, IPAddress ipAddress, int directPort, string localUsername)
        {
            var exception = new Exception("foo");

            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);
            direct.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(exception);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(exception, ex.InnerException);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync produces expected diagnostic on failure"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Produces_Expected_Diagnostics_On_Failure(string username, IPAddress ipAddress, int directPort, string localUsername)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish a direct or indirect message connection"))));
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Purging message connection cache of failed connection"))));
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync throws expected Exception on failure"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Throws_Expected_Exception_On_Failure(string username, IPAddress ipAddress, int directPort, string localUsername)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Contains("Failed to establish a direct or indirect message connection", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync purges cache on failure"), AutoData]
        public async Task GetOrAddMessageConnectionAsync_Purges_Cache_On_Failure(string username, IPAddress ipAddress, int directPort, string localUsername)
        {
            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);
            direct.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None));

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");

                Assert.NotNull(ex);
                Assert.Empty(dict);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Purging message connection cache of failed connection"))));
        }

        [Trait("Category", "GetOrAddMessageConnectionAsync")]
        [Theory(DisplayName = "GetOrAddMessageConnectionAsync sends PeerInit on direct connection established"), AutoData]
        internal async Task GetOrAddMessageConnectionAsync_Sends_PeerInit_On_Direct_Connection_Established(string localUsername, string username, IPAddress ipAddress, int directPort, int indirectPort, int token)
        {
            var peerInit = new PeerInit(localUsername, Constants.ConnectionType.Peer, token).ToByteArray();

            var dendpoint = new IPEndPoint(ipAddress, directPort);
            var direct = GetMessageConnectionMock(username, dendpoint);
            direct.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var iendpoint = new IPEndPoint(ipAddress, indirectPort);
            var indirect = GetMessageConnectionMock(username, iendpoint);
            indirect.Setup(m => m.Type)
                .Returns(ConnectionTypes.Indirect);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(localUsername);
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(token);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, dendpoint, It.IsAny<ConnectionOptions>(), null))
                .Returns(direct.Object);
            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, iendpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(indirect.Object);

            mocks.Waiter.Setup(m => m.Wait<IMessageConnection>(It.IsAny<WaitKey>(), null, It.IsAny<CancellationToken?>()))
                .Throws(new Exception());

            using (manager)
            using (var newConn = await manager.GetOrAddMessageConnectionAsync(username, dendpoint, CancellationToken.None))
            {
                Assert.Equal(direct.Object, newConn);
                Assert.Equal(ConnectionTypes.Direct, newConn.Type);

                direct.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(peerInit)), It.IsAny<CancellationToken?>()), Times.Once);
            }
        }

        [Trait("Category", "GetCachedMessageConnectionAsync")]
        [Theory(DisplayName = "GetCachedMessageConnectionAsync returns cached connection if cached"), AutoData]
        public async Task GetCachedMessageConnectionAsync_Returns_Cached_Connection_If_Cached(string username, IPEndPoint endpoint)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");
            dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(Task.FromResult(conn.Object)));

            using (manager)
            using (var cachedConn = await manager.GetCachedMessageConnectionAsync(username))
            {
                Assert.Equal(conn.Object, cachedConn);
            }
        }

        [Trait("Category", "GetCachedMessageConnectionAsync")]
        [Theory(DisplayName = "GetCachedMessageConnectionAsync returns null if not cached"), AutoData]
        public async Task GetCachedMessageConnectionAsync_Returns_Null_If_Not_Cached(string username)
        {
            var (manager, _) = GetFixture();

            using (manager)
            using (var cachedConn = await manager.GetCachedMessageConnectionAsync(username))
            {
                Assert.Null(cachedConn);
            }
        }

        [Trait("Category", "GetCachedMessageConnectionAsync")]
        [Theory(DisplayName = "GetCachedMessageConnectionAsync produces diagnostic if cached"), AutoData]
        public async Task GetCachedMessageConnectionAsync_Produces_Diagnostic_If_Cached(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");
            dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(Task.FromResult(conn.Object)));

            using (manager)
            using (var cachedConn = await manager.GetCachedMessageConnectionAsync(username))
            {
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Retrieved cached message connection"))), Times.Once);
            }
        }

        [Trait("Category", "GetCachedMessageConnectionAsync")]
        [Theory(DisplayName = "GetCachedMessageConnectionAsync returns null if retrieval throws"), AutoData]
        public async Task GetCachedMessageConnectionAsync_Returns_Null_If_Retrieval_Throws(string username)
        {
            var (manager, _) = GetFixture();

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");
            dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(Task.FromException<IMessageConnection>(new ConnectionException())));

            using (manager)
            using (var cachedConn = await manager.GetCachedMessageConnectionAsync(username))
            {
                Assert.Null(cachedConn);
            }
        }

        [Trait("Category", "GetCachedMessageConnectionAsync")]
        [Theory(DisplayName = "GetCachedMessageConnectionAsync produces diagnostic if retrieval throws"), AutoData]
        public async Task GetCachedMessageConnectionAsync_Produces_Diagnostic_If_Retrieval_Throws(string username)
        {
            var (manager, mocks) = GetFixture();

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("MessageConnectionDictionary");
            dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(Task.FromException<IMessageConnection>(new ConnectionException())));

            using (manager)
            using (var cachedConn = await manager.GetCachedMessageConnectionAsync(username))
            {
                mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to retrieve cached message connection"))), Times.Once);
            }
        }

        [Trait("Category", "AwaitTransferConnectionAsync")]
        [Theory(DisplayName = "AwaitTransferConnectionAsync returns indirect when indirect connects"), AutoData]
        internal async Task AwaitTransferConnectionAsync_Returns_Indirect_When_Indirect_Connects(string username, string filename, int token, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetConnectionMock(endpoint);

            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(indirectKey, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            mocks.Waiter.Setup(m => m.Wait<IConnection>(directKey, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            using (manager)
            using (var actual = await manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actual);
            }
        }

        [Trait("Category", "AwaitTransferConnectionAsync")]
        [Theory(DisplayName = "AwaitTransferConnectionAsync returns direct when direct connects"), AutoData]
        internal async Task AwaitTransferConnectionAsync_Returns_Direct_When_Direct_Connects(string username, string filename, int token, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetConnectionMock(endpoint);

            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(indirectKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            mocks.Waiter.Setup(m => m.Wait<IConnection>(directKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var actual = await manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actual);
            }
        }

        [Trait("Category", "AwaitTransferConnectionAsync")]
        [Theory(DisplayName = "AwaitTransferConnectionAsync throws ConnectionException when both fail"), AutoData]
        internal async Task AwaitTransferConnectionAsync_Throws_ConnectionException_When_Both_Fail(string username, string filename, int token)
        {
            var (manager, mocks) = GetFixture();

            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(indirectKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            mocks.Waiter.Setup(m => m.Wait<IConnection>(directKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Contains("Failed to establish a direct or indirect transfer connection", ex.Message, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        [Trait("Category", "AwaitTransferConnectionAsync")]
        [Theory(DisplayName = "AwaitTransferConnectionAsync produces expected diagnostics on connection"), AutoData]
        internal async Task AwaitTransferConnectionAsync_Produces_Expected_Diagnostics_On_connection(string username, string filename, int token, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetConnectionMock(endpoint);

            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(indirectKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            mocks.Waiter.Setup(m => m.Wait<IConnection>(directKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var actual = await manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actual);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Waiting for a direct or indirect transfer connection"))));
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("established first, attempting to cancel"))));
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("established."))));
        }

        [Trait("Category", "AwaitTransferConnectionAsync")]
        [Theory(DisplayName = "AwaitTransferConnectionAsync produces expected diagnostics on failure"), AutoData]
        internal async Task AwaitTransferConnectionAsync_Produces_Expected_Diagnostics_On_Failure(string username, string filename, int token)
        {
            var (manager, mocks) = GetFixture();

            var indirectKey = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);
            var directKey = new WaitKey(Constants.WaitKey.DirectTransfer, username, token);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(indirectKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            mocks.Waiter.Setup(m => m.Wait<IConnection>(directKey, It.IsAny<int>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.AwaitTransferConnectionAsync(username, filename, token, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish a direct or indirect transfer connection"))));
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Diagnostic raises DiagnosticGenerated")]
        internal void Diagnostic_Raises_DiagnosticGenerated()
        {
            using (var client = new SoulseekClient())
            using (var manager = new PeerConnectionManager(client))
            {
                bool fired = false;
                manager.DiagnosticGenerated += (o, e) => fired = true;

                var diag = manager.GetProperty<IDiagnosticFactory>("Diagnostic");
                diag.Info("test");

                Assert.True(fired);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Diagnostic does not throw if DiagnosticGenerated not subscribed")]
        internal void Diagnostic_Does_Not_Throw_If_DiagnosticGenerated_Not_Subscribed()
        {
            using (var client = new SoulseekClient())
            using (var manager = new PeerConnectionManager(client))
            {
                var diag = manager.GetProperty<IDiagnosticFactory>("Diagnostic");
                var ex = Record.Exception(() => diag.Info("test"));

                Assert.Null(ex);
            }
        }

        private (PeerConnectionManager Manager, Mocks Mocks) GetFixture(string username = null, IPEndPoint endpoint = null, SoulseekClientOptions options = null)
        {
            var mocks = new Mocks(options);

            mocks.ServerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.ServerConnection.Setup(m => m.IPEndPoint)
                .Returns(endpoint ?? new IPEndPoint(IPAddress.None, 0));

            var handler = new PeerConnectionManager(
                mocks.Client.Object,
                mocks.ConnectionFactory.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private Mock<IMessageConnection> GetMessageConnectionMock(string username, IPEndPoint endpoint)
        {
            var mock = new Mock<IMessageConnection>();
            mock.Setup(m => m.Username).Returns(username);
            mock.Setup(m => m.IPEndPoint).Returns(endpoint);

            return mock;
        }

        private Mock<IConnection> GetConnectionMock(IPEndPoint endpoint)
        {
            var mock = new Mock<IConnection>();
            mock.Setup(m => m.IPEndPoint)
                .Returns(endpoint);

            return mock;
        }

        private class Mocks
        {
            public Mocks(SoulseekClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(clientOptions)
                {
                    CallBase = true,
                };

                Client.Setup(m => m.ServerConnection).Returns(ServerConnection.Object);
                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.Listener).Returns(Listener.Object);
                Client.Setup(m => m.PeerMessageHandler).Returns(PeerMessageHandler.Object);
                Client.Setup(m => m.DistributedMessageHandler).Returns(DistributedMessageHandler.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public Mock<IListener> Listener { get; } = new Mock<IListener>();
            public Mock<IPeerMessageHandler> PeerMessageHandler { get; } = new Mock<IPeerMessageHandler>();
            public Mock<IDistributedMessageHandler> DistributedMessageHandler { get; } = new Mock<IDistributedMessageHandler>();
            public Mock<IConnectionFactory> ConnectionFactory { get; } = new Mock<IConnectionFactory>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<ITcpClient> TcpClient { get; } = new Mock<ITcpClient>();
        }
    }
}
