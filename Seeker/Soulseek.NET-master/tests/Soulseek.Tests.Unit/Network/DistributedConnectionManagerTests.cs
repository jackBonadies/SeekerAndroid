// <copyright file="DistributedConnectionManagerTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class DistributedConnectionManagerTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            DistributedConnectionManager c = null;

            var ex = Record.Exception(() => (c, _) = GetFixture());

            Assert.Null(ex);
            Assert.NotNull(c);

            Assert.Equal(0, c.BranchLevel);
            Assert.Equal(string.Empty, c.BranchRoot);
            Assert.False(c.CanAcceptChildren);
            Assert.Empty(c.Children);
            Assert.Equal(new SoulseekClientOptions().DistributedChildLimit, c.ChildLimit);
            Assert.False(c.HasParent);
            Assert.Equal((string.Empty, default(IPEndPoint)), c.Parent);
            Assert.Empty(c.PendingSolicitations);
            Assert.Null(c.AverageBroadcastLatency);
        }

        [Trait("Category", "BranchRoot")]
        [Fact(DisplayName = "BranchRoot returns empty string if no username is set and no parent")]
        public void BranchRoot_Returns_Empty_String_If_No_Username_And_No_Parent()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                Assert.Equal(string.Empty, manager.BranchRoot);
            }
        }

        [Trait("Category", "BranchRoot")]
        [Theory(DisplayName = "BranchRoot returns username if set and no parent"), AutoData]
        public void BranchRoot_Returns_Username_If_Set_And_No_Parent(string username)
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username).Returns(username);

            using (manager)
            {
                Assert.Equal(username, manager.BranchRoot);
            }
        }

        [Trait("Category", "BranchRoot")]
        [Theory(DisplayName = "BranchRoot returns parent branch root if has parent"), AutoData]
        public void BranchRoot_Returns_Parent_Branch_Root_If_Has_Parent(string username, string parentBranchRoot)
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username).Returns(username);

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State).Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);
                manager.SetProperty("ParentBranchRoot", parentBranchRoot);

                Assert.Equal(parentBranchRoot, manager.BranchRoot);
            }
        }

        [Trait("Category", "BranchLevel")]
        [Fact(DisplayName = "BranchLevel returns 0 if no parent")]
        public void BranchRoot_Returns_Zero_If_No_Parent()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                Assert.Equal(0, manager.BranchLevel);
            }
        }

        [Trait("Category", "BranchLevel")]
        [Theory(DisplayName = "BranchLevel returns parent branch level plus 1 if has parent"), AutoData]
        public void BranchRoot_Returns_Parent_Branch_Level_Plus_1_If_Has_Parent(int parentBranchLevel)
        {
            var (manager, _) = GetFixture();

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State).Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);
                manager.SetProperty("ParentBranchLevel", parentBranchLevel);

                Assert.Equal(parentBranchLevel + 1, manager.BranchLevel);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "CanAcceptChildren is false if AcceptDistributedChildren is false")]
        public void CanAcceptChildren_Is_False_If_AcceptDistributedChildren_Is_False()
        {
            using (var s = new SoulseekClient(new SoulseekClientOptions(
                acceptDistributedChildren: false,
                distributedChildLimit: 10)))
            {
                using (var c = new DistributedConnectionManager(s))
                {
                    Assert.False(c.CanAcceptChildren);
                }
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "CanAcceptChildren is false if EnableDistributedNetwork is false")]
        public void CanAcceptChildren_Is_False_If_EnableDistributedNEtwork_Is_False()
        {
            using (var s = new SoulseekClient(new SoulseekClientOptions(
                enableDistributedNetwork: false,
                acceptDistributedChildren: true,
                distributedChildLimit: 10)))
            {
                using (var c = new DistributedConnectionManager(s))
                {
                    Assert.False(c.CanAcceptChildren);
                }
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "CanAcceptChildren is true if AcceptDistributedChildren is true")]
        public void CanAcceptChildren_Is_True_If_AcceptDistributedChildren_Is_True()
        {
            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(new SoulseekClientOptions(
                acceptDistributedChildren: true,
                distributedChildLimit: 10)))
            {
                using (var c = new DistributedConnectionManager(s))
                {
                    c.SetProperty("ParentConnection", parent.Object);

                    Assert.True(c.CanAcceptChildren);
                }
            }
        }

        [Trait("Category", "HasParent")]
        [Fact(DisplayName = "HasParent returns false if parent is null")]
        public void HasParent_Returns_False_If_Parent_Is_Null()
        {
            using (var s = new SoulseekClient(new SoulseekClientOptions(
                acceptDistributedChildren: false,
                distributedChildLimit: 10)))
            {
                using (var c = new DistributedConnectionManager(s))
                {
                    Assert.False(c.HasParent);
                }
            }
        }

        [Trait("Category", "HasParent")]
        [Fact(DisplayName = "HasParent returns false parent is not connected")]
        public void HasParent_Returns_False_If_Parent_Is_Not_Connected()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Disconnected);

            using (var s = new SoulseekClient(new SoulseekClientOptions(
                acceptDistributedChildren: false,
                distributedChildLimit: 10)))
            {
                using (var c = new DistributedConnectionManager(s))
                {
                    c.SetProperty("ParentConnection", conn.Object);

                    Assert.False(c.HasParent);
                }
            }
        }

        [Trait("Category", "HasParent")]
        [Fact(DisplayName = "HasParent returns returns true if parent is connected")]
        public void HasParent_Returns_True_If_Parent_Is_Connected()
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (var s = new SoulseekClient(new SoulseekClientOptions(
                acceptDistributedChildren: false,
                distributedChildLimit: 10)))
            {
                using (var c = new DistributedConnectionManager(s))
                {
                    c.SetProperty("ParentConnection", conn.Object);

                    Assert.True(c.HasParent);
                }
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on diagnostic"), AutoData]
        public void Raises_DiagnosticGenerated_On_Diagnostic(string message)
        {
            using (var client = new SoulseekClient(options: null))
            {
                DiagnosticEventArgs args = default;

                using (var l = new DistributedConnectionManager(client))
                {
                    l.DiagnosticGenerated += (sender, e) => args = e;

                    var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");
                    diagnostic.Info(message);

                    Assert.Equal(message, args.Message);
                }
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Does not throw raising DiagnosticGenerated if no handlers bound"), AutoData]
        public void Does_Not_Throw_Raising_DiagnosticGenerated_If_No_Handlers_Bound(string message)
        {
            using (var client = new SoulseekClient(options: null))
            using (var l = new DistributedConnectionManager(client))
            {
                var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");

                var ex = Record.Exception(() => diagnostic.Info(message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var (manager, mocks) = GetFixture();

            using (manager)
            using (var c = new DistributedConnectionManager(mocks.Client.Object))
            {
                var ex = Record.Exception(() => c.Dispose());

                Assert.Null(ex);
            }
        }

        [Trait("Category", "PromoteToBranchRoot")]
        [Fact(DisplayName = "PromoteToBranchRoot promotes to branch root")]
        public void PromoteToBranchRoot_Promotes_To_Branch_Root()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                Assert.False(manager.IsBranchRoot);

                manager.PromoteToBranchRoot();

                Assert.True(manager.IsBranchRoot);
            }
        }

        [Trait("Category", "PromoteToBranchRoot")]
        [Fact(DisplayName = "PromoteToBranchRoot raises PromotedToBranchRoot and StateChanged")]
        public void PromoteToBranchRoot_Raises_PromotedToBranchRoot_And_StateChanged()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                bool fired = false;
                bool stateChangedFired = false;

                manager.PromotedToBranchRoot += (sender, args) => fired = true;
                manager.StateChanged += (sender, rags) => stateChangedFired = true;

                manager.PromoteToBranchRoot();

                Assert.True(fired);
                Assert.True(stateChangedFired);
            }
        }

        [Trait("Category", "PromoteToBranchRoot")]
        [Fact(DisplayName = "PromoteToBranchRoot does not raise PromotedToBranchRoot if already root")]
        public void PromoteToBranchRoot_Does_Not_Raise_PromotedToBranchRoot_If_Already_Root()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                manager.PromoteToBranchRoot();

                bool fired = false;

                manager.PromotedToBranchRoot += (sender, args) => fired = true;

                manager.PromoteToBranchRoot();

                Assert.False(fired);
            }
        }

        [Trait("Category", "PromoteToBranchRoot")]
        [Fact(DisplayName = "PromoteToBranchRoot does not promote if HasParent")]
        public void PromoteToBranchRoot_Does_Not_Promote_If_HasParent()
        {
            var (manager, _) = GetFixture();

            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.State).Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object);

                manager.PromoteToBranchRoot();

                Assert.False(manager.IsBranchRoot);
            }
        }

        [Trait("Category", "DemoteFromBranchRoot")]
        [Fact(DisplayName = "DemoteFromBranchRoot promotes to branch root")]
        public void DemoteFromBranchRoot_Demotes_From_Branch_Root()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                manager.PromoteToBranchRoot();
                Assert.True(manager.IsBranchRoot);

                manager.DemoteFromBranchRoot();

                Assert.False(manager.IsBranchRoot);
            }
        }

        [Trait("Category", "DemoteFromBranchRoot")]
        [Fact(DisplayName = "DemoteFromBranchRoot raises DemotedFromBranchRoot")]
        public void DemoteFromBranchRoot_Raises_DemotedFromBranchRoot()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                manager.PromoteToBranchRoot();
                Assert.True(manager.IsBranchRoot);

                var fired = false;

                manager.DemotedFromBranchRoot += (sender, args) => fired = true;

                manager.DemoteFromBranchRoot();

                Assert.True(fired);
            }
        }

        [Trait("Category", "DemoteFromBranchRoot")]
        [Fact(DisplayName = "DemoteFromBranchRoot raises StateChanged")]
        public void DemoteFromBranchRoot_Raises_StateChanged()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                manager.PromoteToBranchRoot();
                Assert.True(manager.IsBranchRoot);

                var fired = false;

                manager.StateChanged += (sender, args) => fired = true;

                manager.DemoteFromBranchRoot();

                Assert.True(fired);
            }
        }

        [Trait("Category", "SetParentBranchLevel")]
        [Theory(DisplayName = "SetParentBranchLevel sets branch level"), AutoData]
        public void SetBranchLevel_Sets_Branch_Level(int branchLevel)
        {
            var (manager, _) = GetFixture();

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                manager.SetParentBranchLevel(branchLevel);

                Assert.Equal(branchLevel + 1, manager.BranchLevel);
            }
        }

        [Trait("Category", "SetBranchLevel")]
        [Theory(DisplayName = "SetBranchLevel resets StatusDebounceTimer"), AutoData]
        public void SetBranchLevel_Resets_StatusDebounceTimer(int branchLevel)
        {
            var (manager, _) = GetFixture();

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var timer = manager.GetProperty<System.Timers.Timer>("StatusDebounceTimer");

                Assert.False(timer.Enabled);

                manager.SetParentBranchLevel(branchLevel);

                Assert.Equal(branchLevel + 1, manager.BranchLevel);
                Assert.True(timer.Enabled);
            }
        }

        [Trait("Category", "SetBranchRoot")]
        [Theory(DisplayName = "SetBranchRoot sets branch root"), AutoData]
        public void SetBranchRoot_Sets_Branch_Root(string branchRoot)
        {
            var (manager, _) = GetFixture();

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                manager.SetParentBranchRoot(branchRoot);

                Assert.Equal(branchRoot, manager.BranchRoot);
            }
        }

        [Trait("Category", "SetBranchRoot")]
        [Theory(DisplayName = "SetBranchRoot resets StatusDebounceTimer"), AutoData]
        public void SetBranchRoot_Resets_StatusDebounceTimer(string branchRoot)
        {
            var (manager, _) = GetFixture();

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var timer = manager.GetProperty<System.Timers.Timer>("StatusDebounceTimer");

                Assert.False(timer.Enabled);

                manager.SetParentBranchRoot(branchRoot);

                Assert.Equal(branchRoot, manager.BranchRoot);
                Assert.True(timer.Enabled);
            }
        }

        [Trait("Category", "BroadcastMessageAsync")]
        [Theory(DisplayName = "BroadcastMessageAsync broadcasts message"), AutoData]
        public async Task BroadcastMessageAsync_Broadcasts_Message(byte[] bytes)
        {
            var (manager, _) = GetFixture();

            var c1 = new Mock<IMessageConnection>();
            c1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var c2 = new Mock<IMessageConnection>();
            c2.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("c1", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c1.Object)));
            dict.TryAdd("c2", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c2.Object)));

            using (manager)
            {
                await manager.BroadcastMessageAsync(bytes, CancellationToken.None);
            }

            c1.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(bytes)), CancellationToken.None));
            c2.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(bytes)), CancellationToken.None));
        }

        [Trait("Category", "BroadcastMessageAsync")]
        [Theory(DisplayName = "BroadcastMessageAsync sets AverageBroadcastLatency"), AutoData]
        public async Task BroadcastMessageAsync_Sets_AverageBroadcastLatency(byte[] bytes)
        {
            var (manager, _) = GetFixture();

            var c1 = new Mock<IMessageConnection>();
            c1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("c1", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c1.Object)));

            using (manager)
            {
                Assert.Null(manager.AverageBroadcastLatency);

                await manager.BroadcastMessageAsync(bytes, CancellationToken.None);

                Assert.NotNull(manager.AverageBroadcastLatency);
            }
        }

        [Trait("Category", "BroadcastMessageAsync")]
        [Theory(DisplayName = "BroadcastMessageAsync updates AverageBroadcastLatency"), AutoData]
        public async Task BroadcastMessageAsync_Updates_AverageBroadcastLatency(byte[] bytes)
        {
            var (manager, _) = GetFixture();

            var c1 = new Mock<IMessageConnection>();
            c1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("c1", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c1.Object)));

            using (manager)
            {
                manager.SetProperty(nameof(manager.AverageBroadcastLatency), double.MaxValue);

                await manager.BroadcastMessageAsync(bytes, CancellationToken.None);

                Assert.True(manager.AverageBroadcastLatency < double.MaxValue);
            }
        }

        [Trait("Category", "BroadcastMessageAsync")]
        [Theory(DisplayName = "BroadcastMessageAsync does not broadcast message to unconnected connections"), AutoData]
        public async Task BroadcastMessageAsync_Does_Not_Broadcast_Message_To_Unconnected_Connections(byte[] bytes)
        {
            var (manager, _) = GetFixture();

            var c1 = new Mock<IMessageConnection>();
            c1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var c2 = new Mock<IMessageConnection>();
            c2.Setup(m => m.State)
                .Returns(ConnectionState.Disconnected);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("c1", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c1.Object)));
            dict.TryAdd("c2", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c2.Object)));

            using (manager)
            {
                await manager.BroadcastMessageAsync(bytes, CancellationToken.None);
            }

            c1.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(bytes)), CancellationToken.None), Times.Once);
            c2.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(bytes)), CancellationToken.None), Times.Never);
        }

        [Trait("Category", "BroadcastMessageAsync")]
        [Theory(DisplayName = "BroadcastMessageAsync disconnects connection if write throws"), AutoData]
        public async Task BroadcastMessageAsync_Disconnects_Connection_If_Write_Throws(byte[] bytes)
        {
            var (manager, _) = GetFixture();

            var c1 = new Mock<IMessageConnection>();
            c1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);
            c1.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(new Exception("foo"));

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("c1", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c1.Object)));

            using (manager)
            {
                await manager.BroadcastMessageAsync(bytes, CancellationToken.None);
            }

            c1.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(bytes)), CancellationToken.None), Times.Once);
            c1.Verify(m => m.Disconnect(It.Is<string>(s => s == "Broadcast failure: foo"), It.IsAny<Exception>()), Times.Once);
        }

        [Trait("Category", "BroadcastMessageAsync")]
        [Theory(DisplayName = "BroadcastMessageAsync does not throw if connection is null"), AutoData]
        public async Task BroadcastMessageAsync_Does_Not_Throw_If_Connection_Is_Null(byte[] bytes)
        {
            var (manager, _) = GetFixture();

            var c1 = new Mock<IMessageConnection>();

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("c1", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(c1.Object)));
            dict.TryAdd("c2", new Lazy<Task<IMessageConnection>>(() => Task.FromResult<IMessageConnection>(null)));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.BroadcastMessageAsync(bytes));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ParentConnection_Disconnected")]
        [Theory(DisplayName = "ParentConnection_Disconnected cleans up"), AutoData]
        public void ParentConnection_Disconnected_Cleans_Up(string username, IPEndPoint endpoint, string message)
        {
            var c = GetMessageConnectionMock(username, endpoint);

            var (manager, _) = GetFixture();

            using (manager)
            {
                manager.SetProperty("ParentConnection", new Mock<IMessageConnection>().Object);
                manager.SetProperty("ParentBranchLevel", 1);
                manager.SetProperty("ParentBranchRoot", "foo");

                manager.InvokeMethod("ParentConnection_Disconnected", c.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Null(manager.GetProperty<IMessageConnection>("ParentConnection"));
                Assert.Equal(0, manager.BranchLevel);
                Assert.Equal(string.Empty, manager.BranchRoot);
            }
        }

        [Trait("Category", "ParentConnection_Disconnected")]
        [Theory(DisplayName = "ParentConnection_Disconnected raises ParentDisconnected and StateChanged"), AutoData]
        public void ParentConnection_Raises_ParentDisconnected_And_StateChanged(string username, IPEndPoint endpoint, string message)
        {
            var c = GetMessageConnectionMock(username, endpoint);

            var (manager, _) = GetFixture();

            using (manager)
            {
                manager.SetProperty("ParentConnection", new Mock<IMessageConnection>().Object);
                manager.SetProperty("ParentBranchLevel", 1);
                manager.SetProperty("ParentBranchRoot", "foo");

                DistributedParentEventArgs actualArgs = default;
                bool stateChangedFired = false;

                manager.ParentDisconnected += (sender, args) => actualArgs = args;
                manager.StateChanged += (sender, args) => stateChangedFired = true;

                manager.InvokeMethod("ParentConnection_Disconnected", c.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Equal(username, actualArgs.Username);
                Assert.Equal(endpoint, actualArgs.IPEndPoint);
                Assert.Equal(1, actualArgs.BranchLevel);
                Assert.Equal("foo", actualArgs.BranchRoot);

                Assert.True(stateChangedFired);
            }
        }

        [Trait("Category", "ParentConnection_Disconnected")]
        [Theory(DisplayName = "ParentConnection_Disconnected produces expected diagnostics"), AutoData]
        public void ParentConnection_Disconnected_Produces_Expected_Diagnostics(string username, IPEndPoint endpoint, string message)
        {
            var c = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            using (manager)
            {
                manager.SetProperty("ParentConnection", new Mock<IMessageConnection>().Object);
                manager.SetProperty("ParentBranchLevel", 1);
                manager.SetProperty("ParentBranchRoot", "foo");

                manager.InvokeMethod("ParentConnection_Disconnected", c.Object, new ConnectionDisconnectedEventArgs(message));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("parent connection") && s.ContainsInsensitive("disconnected") && s.ContainsInsensitive("type:"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Info(It.Is<string>(s => s.ContainsInsensitive("parent connection") && s.ContainsInsensitive("disconnected"))), Times.Once);
        }

        [Trait("Category", "ParentConnection_Disconnected")]
        [Theory(DisplayName = "ParentConnection_Disconnected produces expected diagnostic when message is empty"), AutoData]
        public void ParentConnection_Disconnected_Produces_Expected_Diagnostic_When_Message_Is_Empty(string username, IPEndPoint endpoint)
        {
            var c = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            using (manager)
            {
                manager.SetProperty("ParentConnection", new Mock<IMessageConnection>().Object);
                manager.SetProperty("ParentBranchLevel", 1);
                manager.SetProperty("ParentBranchRoot", "foo");

                manager.InvokeMethod("ParentConnection_Disconnected", c.Object, new ConnectionDisconnectedEventArgs(null));
            }

            mocks.Diagnostic.Verify(m => m.Info(It.Is<string>(s => s.ContainsInsensitive("parent connection") && s.ContainsInsensitive("disconnected."))), Times.Once);
        }

        [Trait("Category", "ParentConnection_Disconnected")]
        [Theory(DisplayName = "ParentConnection_Disconnected does not throw if AddParentConnectionAsync throws"), AutoData]
        public void ParentConnection_Disconnected_Does_Not_Throw_If_AddParentConnectionAsync_Throws(string username, IPEndPoint endpoint, string message)
        {
            var c = GetMessageConnectionMock(username, endpoint);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            using (manager)
            {
                manager.SetProperty("ParentCandidateList", null); // force a null ref

                var ex = Record.Exception(() => manager.InvokeMethod("ParentConnection_Disconnected", c.Object, new ConnectionDisconnectedEventArgs(message)));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR rejects if over child limit"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Rejects_If_Over_Child_Limit(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions(distributedChildLimit: 0));

            using (manager)
            {
                await manager.GetOrAddChildConnectionAsync(ctpr);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("rejected", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR updates status on rejection"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Updates_Status_On_Rejection(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions(distributedChildLimit: 0));

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            using (manager)
            {
                await manager.GetOrAddChildConnectionAsync(ctpr);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR adds child on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Adds_Child_On_Successful_Connection(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await manager.GetOrAddChildConnectionAsync(ctpr);

                var child = manager.Children.FirstOrDefault();

                Assert.Single(manager.Children);
                Assert.NotEqual(default((string, IPEndPoint)), child);
                Assert.Equal(ctpr.Username, child.Username);
                Assert.Equal(ctpr.IPAddress, child.IPEndPoint.Address);
                Assert.Equal(ctpr.Port, child.IPEndPoint.Port);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR raises ChildAdded on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Raises_ChildAdded_On_Successful_Connection(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                DistributedChildEventArgs actualArgs = default;

                manager.ChildAdded += (sender, args) => actualArgs = args;

                await manager.GetOrAddChildConnectionAsync(ctpr);

                Assert.NotNull(actualArgs);
                Assert.Equal(ctpr.Username, actualArgs.Username);
                Assert.Equal(ctpr.IPEndPoint, actualArgs.IPEndPoint);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR raises StateChanged on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Raises_StateChanged_On_Successful_Connection(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                bool stateChangedFired = false;

                manager.StateChanged += (_, args) => stateChangedFired = true;

                await manager.GetOrAddChildConnectionAsync(ctpr);

                Assert.True(stateChangedFired);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR resets StatusDebounceTimer on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Resets_StatusDebounceTimer_On_Successful_Connection(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);
                var timer = manager.GetProperty<System.Timers.Timer>("StatusDebounceTimer");

                Assert.False(timer.Enabled);

                await manager.GetOrAddChildConnectionAsync(ctpr);

                var child = manager.Children.FirstOrDefault();

                Assert.Single(manager.Children);
                Assert.NotEqual(default((string, IPEndPoint)), child);
                Assert.Equal(ctpr.Username, child.Username);
                Assert.Equal(ctpr.IPAddress, child.IPEndPoint.Address);
                Assert.Equal(ctpr.Port, child.IPEndPoint.Port);

                Assert.True(timer.Enabled);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR discards CTPR on cached connection"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Discards_CTPR_On_Cached_Connection(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
                dict.TryAdd(ctpr.Username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

                await manager.GetOrAddChildConnectionAsync(ctpr);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("ignored; connection already exists."))));
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR disposes connection on throw"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Disposes_Connection_On_Throw(ConnectToPeerResponse ctpr)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var ex = await Record.ExceptionAsync(() => manager.GetOrAddChildConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR purges cache for user throw"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Purges_Cache_On_Throw(ConnectToPeerResponse ctpr)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await Record.ExceptionAsync(() => manager.GetOrAddChildConnectionAsync(ctpr));

                Assert.Empty(manager.Children);
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR produces warning and replaces if wrong connection is purged"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Produces_Warning_And_Replaces_If_Wrong_Connection_Is_Purged(ConnectToPeerResponse ctpr)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var directConn = new Mock<IMessageConnection>();
            directConn.Setup(m => m.Type)
                .Returns(ConnectionTypes.Direct);

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Callback(() =>
                {
                    var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
                    var record = new Lazy<Task<IMessageConnection>>(() => Task.FromResult(directConn.Object));
                    dict.AddOrUpdate(ctpr.Username, record, (k, v) => record);
                })
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await Record.ExceptionAsync(() => manager.GetOrAddChildConnectionAsync(ctpr));

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");

                Assert.NotEmpty(dict);

                dict.TryGetValue(ctpr.Username, out var remainingRecord);

                var remainingConn = await remainingRecord.Value;

                Assert.Equal(directConn.Object, remainingConn);
            }

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("Erroneously purged direct child connection")), It.IsAny<Exception>()), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR generates expected diagnostics on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Generates_Expected_Diagnostics_On_Successful_Connection(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await manager.GetOrAddChildConnectionAsync(ctpr);
            }

            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Attempting inbound indirect child connection to {ctpr.Username}"))), Times.Once);
            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"child connection to {ctpr.Username}") && s.ContainsInsensitive("established"))), Times.Once);
            mocks.Diagnostic
                .Verify(m => m.Info(It.Is<string>(s => s.ContainsInsensitive($"Added child connection to {ctpr.Username}"))), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR sends expected branch information on connection when connected to parent"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Sends_Expected_Branch_Information_On_Connection_When_Connected_To_Parent(ConnectToPeerResponse ctpr, int level, string root)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object); // fake any connection
                manager.SetParentBranchLevel(level);
                manager.SetParentBranchRoot(root);
                await manager.GetOrAddChildConnectionAsync(ctpr);
            }

            var expected = new List<byte>();
            expected.AddRange(new DistributedBranchLevel(manager.BranchLevel).ToByteArray());
            expected.AddRange(new DistributedBranchRoot(manager.BranchRoot).ToByteArray());

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(expected.ToArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR generates expected diagnostic on error"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Generates_Expected_Diagnostic_On_Error(ConnectToPeerResponse ctpr)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await Record.ExceptionAsync(() => manager.GetOrAddChildConnectionAsync(ctpr));
            }

            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Failed to establish an inbound indirect child connection"))), Times.Once);
            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Purging child connection cache of failed connection"))), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR throws expected exception on failure"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Throws_Expected_Exception_On_Failure(ConnectToPeerResponse ctpr)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var ex = await Record.ExceptionAsync(() => manager.GetOrAddChildConnectionAsync(ctpr));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR adds entry to pending dictionary"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Adds_Entry_To_Pending_Dictionary(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var tcs = new TaskCompletionSource();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(tcs.Task);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var task = manager.GetOrAddChildConnectionAsync(ctpr);

                var dict = manager.GetProperty<ConcurrentDictionary<string, CancellationTokenSource>>("PendingInboundIndirectConnectionDictionary");
                var exists = dict.ContainsKey(ctpr.Username);

                tcs.SetResult();

                await task;

                Assert.True(exists);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR removes entry from pending dictionary on success"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Removes_Entry_From_Pending_Dictionary_On_Success(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await manager.GetOrAddChildConnectionAsync(ctpr);

                var dict = manager.GetProperty<ConcurrentDictionary<string, CancellationTokenSource>>("PendingInboundIndirectConnectionDictionary");
                Assert.False(dict.ContainsKey(ctpr.Username));
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync CTPR removes entry from pending dictionary on failure"), AutoData]
        internal async Task AddChildConnectionAsync_Ctpr_Removes_Entry_From_Pending_Dictionary_On_Failure(ConnectToPeerResponse ctpr)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(ctpr.Username, ctpr.IPEndPoint);

            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(new Exception()));

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(ctpr.Username, ctpr.IPEndPoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var ex = await Record.ExceptionAsync(() => manager.GetOrAddChildConnectionAsync(ctpr));

                Assert.NotNull(ex);

                var dict = manager.GetProperty<ConcurrentDictionary<string, CancellationTokenSource>>("PendingInboundIndirectConnectionDictionary");
                Assert.False(dict.ContainsKey(ctpr.Username));
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync rejects if over child limit"), AutoData]
        internal async Task AddChildConnectionAsync_Rejects_If_Over_Child_Limit(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions(distributedChildLimit: 0));

            using (manager)
            {
                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("rejected", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync disposes TcpClient on rejection"), AutoData]
        internal async Task AddChildConnectionAsync_Disposes_TcpClient_On_Rejection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions(distributedChildLimit: 0));
            var conn = GetMessageConnectionMock(username, endpoint);

            using (manager)
            {
                await manager.AddOrUpdateChildConnectionAsync(username, conn.Object);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.Contains("rejected", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync disposes connection on throw"), AutoData]
        internal async Task AddChildConnectionAsync_Disposes_Connection_On_Throw(string username, IPEndPoint endpoint)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var ex = await Record.ExceptionAsync(() => manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object));

                Assert.NotNull(ex);

                conn.Verify(m => m.Dispose(), Times.Once);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync throws expected Exception on throw"), AutoData]
        internal async Task AddChildConnectionAsync_Throws_Expected_Exception_On_Throw(string username, IPEndPoint endpoint)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var ex = await Record.ExceptionAsync(() => manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync updates status on rejection"), AutoData]
        internal async Task AddChildConnectionAsync_Updates_Status_On_Rejection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions(distributedChildLimit: 0));

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            mocks.TcpClient.Setup(m => m.RemoteEndPoint)
                .Returns(endpoint);

            using (manager)
            {
                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync adds child on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Adds_Child_On_Successful_Connection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);

                var child = manager.Children.FirstOrDefault();

                Assert.Single(manager.Children);
                Assert.NotEqual(default((string, IPEndPoint)), child);
                Assert.Equal(username, child.Username);
                Assert.Equal(endpoint.Address, child.IPEndPoint.Address);
                Assert.Equal(endpoint.Port, child.IPEndPoint.Port);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync resets StatusDebounceTimer on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Resets_StatusDebounceTimer_On_Successful_Connection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);
                var timer = manager.GetProperty<System.Timers.Timer>("StatusDebounceTimer");

                Assert.False(timer.Enabled);

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);

                var child = manager.Children.FirstOrDefault();

                Assert.Single(manager.Children);
                Assert.NotEqual(default((string, IPEndPoint)), child);
                Assert.Equal(username, child.Username);
                Assert.Equal(endpoint.Address, child.IPEndPoint.Address);
                Assert.Equal(endpoint.Port, child.IPEndPoint.Port);

                Assert.True(timer.Enabled);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync supersedes existing connection on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Supersedes_Existing_Connection_On_Successful_Connection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var existingConn = GetMessageConnectionMock(username, endpoint);

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
                dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(existingConn.Object)));

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);

                var child = manager.Children.FirstOrDefault();

                Assert.Single(manager.Children);
                Assert.NotEqual(default((string, IPEndPoint)), child);
                Assert.Equal(username, child.Username);
                Assert.Equal(endpoint.Address, child.IPEndPoint.Address);
                Assert.Equal(endpoint.Port, child.IPEndPoint.Port);
            }

            existingConn.Verify(m => m.Disconnect("Superseded.", It.IsAny<Exception>()));
            existingConn.Verify(m => m.Dispose());
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync produces expected diagnostic when superseding connection"), AutoData]
        internal async Task AddChildConnectionAsync_Produces_Expected_Diagnostic_When_Superceding_Connection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var existingConn = GetMessageConnectionMock(username, endpoint);

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
                dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(existingConn.Object)));

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);

                var child = manager.Children.FirstOrDefault();

                Assert.Single(manager.Children);
                Assert.NotEqual(default((string, IPEndPoint)), child);
                Assert.Equal(username, child.Username);
                Assert.Equal(endpoint.Address, child.IPEndPoint.Address);
                Assert.Equal(endpoint.Port, child.IPEndPoint.Port);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Superseding existing child connection"))));
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync does not throw if superseded connection throws"), AutoData]
        internal async Task AddChildConnectionAsync_Does_Not_Throw_If_Superseded_Connection_Throws(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var existingConn = GetMessageConnectionMock(username, endpoint);
            existingConn.Setup(m => m.Disconnect(It.IsAny<string>(), null))
                .Throws(new Exception());

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
                dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(existingConn.Object)));

                var ex = await Record.ExceptionAsync(() =>
                    manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object));

                Assert.Null(ex);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Superseding existing child connection"))));
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync cancels pending connection if one exists"), AutoData]
        internal async Task AddChildConnectionAsync_Cancels_Pending_Connection_If_One_Exists(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var existingConn = GetMessageConnectionMock(username, endpoint);

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var cts = new CancellationTokenSource();
            var pendingDict = new ConcurrentDictionary<string, CancellationTokenSource>();
            pendingDict.AddOrUpdate(username, cts, (k, v) => cts);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
                dict.TryAdd(username, new Lazy<Task<IMessageConnection>>(() => Task.FromResult(existingConn.Object)));

                manager.SetProperty("PendingInboundIndirectConnectionDictionary", pendingDict);

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);

                Assert.True(cts.IsCancellationRequested);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Cancelling pending indirect child connection to {username}"))));
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync purges cache for user on throw"), AutoData]
        internal async Task AddChildConnectionAsync_Purges_Cache_For_User_On_Throw(string username, IPEndPoint endpoint)
        {
            var expectedEx = new Exception("foo");

            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object));

                Assert.Empty(manager.Children);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync sends expected branch info when connected to parent"), AutoData]
        internal async Task AddChildConnectionAsync_Sends_Expected_Branch_Info_When_Connected_To_Parent(string username, IPEndPoint endpoint, int level, string root)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object); // fake any connection
                manager.SetParentBranchLevel(level);
                manager.SetParentBranchRoot(root);
                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);
            }

            var expected = new List<byte>();
            expected.AddRange(new DistributedBranchLevel(manager.BranchLevel).ToByteArray());
            expected.AddRange(new DistributedBranchRoot(manager.BranchRoot).ToByteArray());

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(expected.ToArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync invokes StartReadingContinuously on successful connection"), AutoData]
        internal async Task AddChildConnectionAsync_Invokes_StartReadingContinuously_On_Successful_Connection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);
            }

            conn.Verify(m => m.StartReadingContinuously(), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync generates expected diagnostics on success"), AutoData]
        internal async Task AddChildConnectionAsync_Generates_Expected_Diagnostics_On_Success(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);
            }

            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"child connection to {username}") && s.ContainsInsensitive("accepted"))), Times.Once);
            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"child connection to {username}") && s.ContainsInsensitive("handed off"))), Times.Once);
            mocks.Diagnostic
                .Verify(m => m.Info(It.Is<string>(s => s.ContainsInsensitive($"Added child connection to {username}"))), Times.Once);
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync raises ChildAdded on success when not superseding"), AutoData]
        internal async Task AddChildConnectionAsync_Raises_ChildAdded_On_Success_When_Not_Superseding(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                DistributedChildEventArgs actualArgs = default;
                manager.ChildAdded += (sender, args) => actualArgs = args;

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);

                Assert.Equal(username, actualArgs.Username);
                Assert.Equal(endpoint, actualArgs.IPEndPoint);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync raises StateChanged on success when not superseding"), AutoData]
        internal async Task AddChildConnectionAsync_Raises_StateChanged_On_Success_When_Not_Superseding(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<int>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                bool stateChangedFired = false;

                manager.StateChanged += (sender, args) => stateChangedFired = true;

                await manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object);

                Assert.True(stateChangedFired);
            }
        }

        [Trait("Category", "AddChildConnectionAsync")]
        [Theory(DisplayName = "AddChildConnectionAsync generates expected diagnostics on throw"), AutoData]
        internal async Task AddChildConnectionAsync_Generates_Expected_Diagnostics_On_Throw(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Throws(new Exception("foo"));

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            mocks.ConnectionFactory.Setup(m => m.GetMessageConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);

                await Record.ExceptionAsync(() => manager.AddOrUpdateChildConnectionAsync(username, GetMessageConnectionMock(username, endpoint).Object));
            }

            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Failed to establish an inbound direct child connection"))), Times.Once);
            mocks.Diagnostic
                .Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Purging child connection cache of failed connection"))), Times.Once);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync skips update client isn't connected")]
        internal async Task UpdateStatusAsync_Skips_Update_If_Client_Not_Connected()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Disconnected);

            using (manager)
            {
                await manager.UpdateStatusAsync();
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync skips update client isn't logged in")]
        internal async Task UpdateStatusAsync_Skips_Update_If_Client_Not_Logged_In()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected);

            using (manager)
            {
                await manager.UpdateStatusAsync();
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync skips update if no change and parent connected")]
        internal async Task UpdateStatusAsync_Skips_Update_If_No_Change_And_Parent_Connected()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var conn = GetMessageConnectionMock("foo", null);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                // bit of a hack here, but this is the expected status on an uninitialized instance
                manager.SetProperty("LastStatus", "Requesting parent: False, Branch level: 1, Branch root: , Number of children: 0/25, Accepting children: True");
                manager.SetProperty("ParentConnection", conn.Object);
                await manager.UpdateStatusAsync();
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync writes expected payload to server")]
        internal async Task UpdateStatusAsync_Writes_Expected_Payload_To_Server()
        {
            var expectedPayload = Convert.FromBase64String("CAAAAH4AAAABAAAACAAAAH8AAAAAAAAABQAAAGQAAAABBQAAAEcAAAAA");

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var conn = GetMessageConnectionMock("foo", null);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object);
                await manager.UpdateStatusAsync();

                Assert.True(manager.GetProperty<bool>("Enabled"));
                Assert.True(manager.CanAcceptChildren);
                Assert.True(manager.HasParent);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(expectedPayload)), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync writes HaveNoParents = false if disabled")]
        internal async Task UpdateStatusAsync_Writes_HaveNoParents_False_If_Disabled()
        {
            var expectedPayload = Convert.FromBase64String("CAAAAH4AAAAAAAAACAAAAH8AAAAAAAAABQAAAGQAAAAABQAAAEcAAAAA");

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(enableDistributedNetwork: false));

            var conn = GetMessageConnectionMock("foo", null);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                await manager.UpdateStatusAsync(CancellationToken.None);

                Assert.False(manager.GetProperty<bool>("Enabled"));
                Assert.False(manager.CanAcceptChildren);
                Assert.False(manager.HasParent);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(expectedPayload)), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync produces status diagnostic on success")]
        internal async Task UpdateStatusAsync_Produces_Status_Diagnostic_On_Success()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var conn = GetMessageConnectionMock("foo", null);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object);
                await manager.UpdateStatusAsync();
            }

            mocks.Diagnostic.Verify(m => m.Info(It.Is<string>(s => s.ContainsInsensitive("Updated distributed status"))), Times.Once);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync raises StateChanged on success")]
        internal async Task UpdateStatusAsync_Raises_StateChanged_On_Success()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var conn = GetMessageConnectionMock("foo", null);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object);

                bool stateChangedFired = false;

                manager.StateChanged += (sender, args) => stateChangedFired = true;

                await manager.UpdateStatusAsync();

                Assert.True(stateChangedFired);
            }
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync produces diagnostic warning on failure when connected")]
        internal async Task UpdateStatusAsync_Produces_Diagnostic_Warning_On_Failure_When_Connected()
        {
            var expectedEx = new Exception(string.Empty);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            mocks.ServerConnection.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(expectedEx));

            var conn = GetMessageConnectionMock("foo", null);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object);
                await manager.UpdateStatusAsync();
            }

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("Failed to update distributed status")), It.Is<Exception>(e => e == expectedEx)), Times.Once);
        }

        [Trait("Category", "UpdateStatusAsync")]
        [Fact(DisplayName = "UpdateStatusAsync produces diagnostic debug on failure when disconnected")]
        internal async Task UpdateStatusAsync_Produces_Diagnostic_Debug_On_Failure_When_Disconnected()
        {
            var expectedEx = new Exception(string.Empty);

            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            mocks.ServerConnection.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(expectedEx))
                .Callback(() => mocks.Client.Setup(m => m.State).Returns(SoulseekClientStates.Disconnected));

            var conn = GetMessageConnectionMock("foo", null);
            conn.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", conn.Object);
                await manager.UpdateStatusAsync();
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to update distributed status")), It.Is<Exception>(e => e == expectedEx)), Times.Once);
        }

        [Trait("Category", "ResetStatus")]
        [Theory(DisplayName = "ResetStatus resets status and demotes from branch root"), AutoData]
        internal void ResetStatus_Resets_Status_And_Demotes_From_Branch_Root(string lastStatus, DateTime lastStatusTimestamp)
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                manager.SetProperty("LastStatus", lastStatus);
                manager.SetProperty("LastStatusTimestamp", lastStatusTimestamp);
                manager.SetProperty("IsBranchRoot", true);

                manager.ResetStatus();

                Assert.Equal(default, manager.GetProperty<string>("LastStatus"));
                Assert.Equal(default, manager.GetProperty<DateTime>("LastStatusTimestamp"));
                Assert.False(manager.IsBranchRoot);
            }
        }

        [Trait("Category", "ChildConnection_Disconnected")]
        [Theory(DisplayName = "ChildConnection_Disconnected removes child"), AutoData]
        internal void ChildConnection_Disconnected_Removes_Child(string message)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var dict2 = manager.GetProperty<ConcurrentDictionary<string, IPEndPoint>>("ChildDictionary");
            dict2.TryAdd("foo", conn.Object.IPEndPoint);

            using (manager)
            {
                manager.SetProperty("ChildConnectionDictionary", dict);
                manager.SetProperty("ChildDictionary", dict2);

                manager.InvokeMethod("ChildConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Empty(dict);
                Assert.Empty(dict2);
            }
        }

        [Trait("Category", "ChildConnection_Disconnected")]
        [Theory(DisplayName = "ChildConnection_Disconnected raises ChildDisconnected"), AutoData]
        internal void ChildConnection_Disconnected_Raises_ChildDisconnected(string message)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var dict2 = manager.GetProperty<ConcurrentDictionary<string, IPEndPoint>>("ChildDictionary");
            dict2.TryAdd("foo", conn.Object.IPEndPoint);

            using (manager)
            {
                DistributedChildEventArgs actualArgs = default;

                manager.ChildDisconnected += (sender, args) => actualArgs = args;

                manager.InvokeMethod("ChildConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Equal("foo", actualArgs.Username);
                Assert.Equal(conn.Object.IPEndPoint, actualArgs.IPEndPoint);
            }
        }

        [Trait("Category", "ChildConnection_Disconnected")]
        [Theory(DisplayName = "ChildConnection_Disconnected raises StateChanged"), AutoData]
        internal void ChildConnection_Disconnected_Raises_StateChanged(string message)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            var dict2 = manager.GetProperty<ConcurrentDictionary<string, IPEndPoint>>("ChildDictionary");
            dict2.TryAdd("foo", conn.Object.IPEndPoint);

            using (manager)
            {
                bool stateChangedFired = false;

                manager.StateChanged += (sender, args) => stateChangedFired = true;

                manager.InvokeMethod("ChildConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.True(stateChangedFired);
            }
        }

        [Trait("Category", "ChildConnection_Disconnected")]
        [Theory(DisplayName = "ChildConnection_Disconnected resets StatusDebounceTimer"), AutoData]
        internal void ChildConnection_Disconnected_Resets_StatusDebounceTimer(string message)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            using (manager)
            {
                manager.SetProperty("ChildConnectionDictionary", dict);
                var timer = manager.GetProperty<System.Timers.Timer>("StatusDebounceTimer");

                Assert.False(timer.Enabled);

                manager.InvokeMethod("ChildConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));

                Assert.Empty(dict);

                Assert.True(timer.Enabled);
            }
        }

        [Trait("Category", "ChildConnection_Disconnected")]
        [Theory(DisplayName = "ChildConnection_Disconnected disposes connection"), AutoData]
        internal void ChildConnection_Disconnected_Disposes_Connection(string message)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            using (manager)
            {
                manager.SetProperty("ChildConnectionDictionary", dict);

                manager.InvokeMethod("ChildConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "ChildConnection_Disconnected")]
        [Theory(DisplayName = "ChildConnection_Disconnected produces expected diagnostic"), AutoData]
        internal void ChildConnection_Disconnected_Produces_Expected_Diagnostic(string message)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            var dict = manager.GetProperty<ConcurrentDictionary<string, Lazy<Task<IMessageConnection>>>>("ChildConnectionDictionary");
            dict.TryAdd("foo", new Lazy<Task<IMessageConnection>>(() => Task.FromResult(conn.Object)));

            using (manager)
            {
                manager.SetProperty("ChildConnectionDictionary", dict);

                manager.InvokeMethod("ChildConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Child connection to foo") && s.ContainsInsensitive("disconnected"))), Times.Once);
        }

        [Trait("Category", "ParentCandidateConnection_Disconnected")]
        [Theory(DisplayName = "ParentCandidateConnection_Disconnected disposes connection"), AutoData]
        internal void ParentCandidateConnection_Disconnected_Disposes_Connection(string message)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            using (manager)
            {
                manager.InvokeMethod("ParentCandidateConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "ParentCandidateConnection_Disconnected")]
        [Theory(DisplayName = "ParentCandidateConnection_Disconnected produces expected diagnostic"), AutoData]
        internal void ParentCandidateConnection_Disconnected_Produces_Expected_Diagnostic(string message)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock("foo", null);

            using (manager)
            {
                manager.InvokeMethod("ParentCandidateConnection_Disconnected", conn.Object, new ConnectionDisconnectedEventArgs(message));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Parent candidate") && s.ContainsInsensitive("disconnected"))), Times.Once);
        }

        [Trait("Category", "GetParentCandidateConnectionIndirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionIndirectAsync removes solicitation on throw"), AutoData]
        internal async Task GetParentCandidateConnectionIndirectAsync_Removes_Solicitation_On_Throw(string username)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionIndirectAsync", username, CancellationToken.None));

                Assert.Empty(manager.PendingSolicitations);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionIndirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionIndirectAsync throws expected exception on failure"), AutoData]
        internal async Task GetParentCandidateConnectionIndirectAsync_Throws_Expected_Exception_On_Failure(string username)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            var expected = new Exception("foo");

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(expected));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionIndirectAsync", username, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.Equal(expected, ex);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionIndirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionIndirectAsync returns expected connection"), AutoData]
        internal async Task GetParentCandidateConnectionIndirectAsync_Returns_Expected_Connection(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionIndirectAsync", username, CancellationToken.None))
            {
                Assert.Equal(msgConn.Object, actualConn);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionIndirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionIndirectAsync sets type to Outbound Indirect"), AutoData]
        internal async Task GetParentCandidateConnectionIndirectAsync_Sets_Type_To_Outbound_Indirect(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionIndirectAsync", username, CancellationToken.None))
            {
                msgConn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionIndirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionIndirectAsync produces expected diagnostic on success"), AutoData]
        internal async Task GetParentCandidateConnectionIndirectAsync_Produces_Expected_Diagnostic(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            var conn = GetConnectionMock(endpoint);
            conn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            var msgConn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(msgConn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(conn.Object));

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionIndirectAsync", username, CancellationToken.None))
            {
                mocks.Diagnostic.Verify(m =>
                    m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Indirect parent candidate connection to {username}") && s.ContainsInsensitive($"handed off"))));
                mocks.Diagnostic.Verify(m =>
                    m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Indirect parent candidate connection to {username}") && s.ContainsInsensitive($"established"))));
            }
        }

        [Trait("Category", "GetParentCandidateConnectionIndirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionIndirectAsync produces expected diagnostic on failure"), AutoData]
        internal async Task GetParentCandidateConnectionIndirectAsync_Produces_Expected_Diagnostic_On_Failure(string username)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionIndirectAsync", username, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish an indirect parent candidate connection"))));
        }

        [Trait("Category", "GetParentCandidateConnectionDirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionDirectAsync returns expected connection"), AutoData]
        internal async Task GetParentCandidateConnectionDirectAsync_Returns_Expected_Connection(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionDirectAsync", username, endpoint, CancellationToken.None))
            {
                Assert.Equal(conn.Object, actualConn);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionDirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionDirectAsync sets type to Outbound Direct"), AutoData]
        internal async Task GetParentCandidateConnectionDirectAsync_Sets_Type_To_Outbound_Direct(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (await manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionDirectAsync", username, endpoint, CancellationToken.None))
            {
                conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionDirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionDirectAsync disposes connection on throw"), AutoData]
        internal async Task GetParentCandidateConnectionDirectAsync_Disposes_Connection_On_Throw(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(new Exception()));

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionDirectAsync", username, endpoint, CancellationToken.None));
            }

            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetParentCandidateConnectionDirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionDirectAsync throws expected exception on failure"), AutoData]
        internal async Task GetParentCandidateConnectionDirectAsync_Throws_Expected_Exception_On_Failure(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            var expected = new Exception("foo");

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(expected));

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionDirectAsync", username, endpoint, CancellationToken.None));

                Assert.Equal(expected, ex);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionDirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionDirectAsync produces expected diagnostic on success"), AutoData]
        internal async Task GetParentCandidateConnectionDirectAsync_Produces_Expected_Diagnostic_On_Success(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            using (var actualConn = await manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionDirectAsync", username, endpoint, CancellationToken.None))
            {
                mocks.Diagnostic.Verify(m =>
                    m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Direct parent candidate connection to {username}") && s.ContainsInsensitive($"established"))));
            }
        }

        [Trait("Category", "GetParentCandidateConnectionDirectAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionDirectAsync produces expected diagnostic on failure"), AutoData]
        internal async Task GetParentCandidateConnectionDirectAsync_Produces_Expected_Diagnostic_On_Failure(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            var expected = new Exception("foo");

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<Task>(expected));

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<IMessageConnection>>("GetParentCandidateConnectionDirectAsync", username, endpoint, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m =>
                m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Failed to establish a direct parent candidate connection to {username}"))));
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync returns direct when direct connects first"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Returns_Direct_When_Direct_Connects_First(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var actual = await manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None);

                Assert.Equal(conn.Object, actual.Connection);
                Assert.Equal(branchLevel, actual.BranchLevel);
                Assert.Equal(branchRoot, actual.BranchRoot);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Direct);
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync sends PeerInit when direct connects first"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Sends_PeerInit_When_Direct_Connects_First(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id, int token)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(token);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var actual = await manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None);

                Assert.Equal(conn.Object, actual.Connection);
                Assert.Equal(branchLevel, actual.BranchLevel);
                Assert.Equal(branchRoot, actual.BranchRoot);
            }

            var peerInit = new PeerInit(localUser, Constants.ConnectionType.Distributed, token).ToByteArray();

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(peerInit)), It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync returns indirect when indirect connects first"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Returns_Indirect_When_Inirect_Connects_First(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var initConn = GetConnectionMock(endpoint);
            initConn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            // direct fetch fails
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Throws(new Exception());

            // indirect succeeds
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), mocks.TcpClient.Object))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(initConn.Object));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var actual = await manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None);

                Assert.Equal(conn.Object, actual.Connection);
            }

            conn.VerifySet(m => m.Type = ConnectionTypes.Outbound | ConnectionTypes.Indirect);
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync invokes StartReadingContinuously when indirect connects first"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Invokes_StartReadingContinuously_When_Inirect_Connects_First(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var initConn = GetConnectionMock(endpoint);
            initConn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            // direct fetch fails
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Throws(new Exception());

            // indirect succeeds
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), mocks.TcpClient.Object))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(initConn.Object));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var actual = await manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None);

                Assert.Equal(conn.Object, actual.Connection);
            }

            conn.Verify(m => m.StartReadingContinuously(), Times.Once);
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync throws when neither direct nor indirect connects"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Throws_When_Neither_Direct_Nor_Indirect_Connects(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var initConn = GetConnectionMock(endpoint);
            initConn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            // direct fetch fails
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Throws(new Exception());

            // indirect fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.True(ex.Message.ContainsInsensitive($"Failed to establish a direct or indirect parent candidate connection to {username}"));
            }
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync produces expected diagnostic when neither direct nor indirect connects"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Produces_Expected_Diagnostic_When_Neither_Direct_Nor_Indirect_Connects(string localUser, string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var initConn = GetConnectionMock(endpoint);
            initConn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            // direct fetch fails
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Throws(new Exception());

            // indirect fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to establish a direct or indirect parent candidate connection"))));
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync returns expected branch info"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Returns_Expected_Branch_Info(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var actual = await manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None);

                Assert.Equal(branchLevel, actual.BranchLevel);
                Assert.Equal(branchRoot, actual.BranchRoot);
            }
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync throws when branch level not received"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Throws_When_Branch_Level_Not_Received(string localUser, string username, IPEndPoint endpoint, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<int>(new Exception()));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.True(ex.Message.ContainsInsensitive($"Failed to negotiate parent candidate connection to {username}"));
            }
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync throws when branch root not received"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Throws_When_Branch_Root_Not_Received(string localUser, string username, IPEndPoint endpoint, int branchLevel, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel + 1));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<string>(new Exception()));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.True(ex.Message.ContainsInsensitive($"Failed to negotiate parent candidate connection to {username}"));
            }
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync throws when initial search request not received"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Throws_When_Initial_Search_Request_Not_Received(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException(new Exception()));

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.True(ex.Message.ContainsInsensitive($"Failed to negotiate parent candidate connection to {username}"));
            }
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync disconnects and disposes connection when init fails"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Disconnects_And_Disposes_Connection_When_Init_Fails(string localUser, string username, IPEndPoint endpoint, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<int>(new Exception()));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<ConnectionException>(ex);
                Assert.True(ex.Message.ContainsInsensitive($"Failed to negotiate parent candidate connection to {username}"));
            }

            conn.Verify(m => m.Disconnect("One or more required messages was not received.", It.IsAny<Exception>()), Times.Once);
            conn.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync produces expected diagnostics when init fails"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Produces_Expected_Diagnostics_When_Init_Fails(string localUser, string username, IPEndPoint endpoint, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn.Object);

            // indirect wait fails
            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<IConnection>(new Exception()));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromException<int>(new Exception()));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                await Record.ExceptionAsync(() => manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None));
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to negotiate parent candidate connection"))));
        }

        [Trait("Category", "GetParentCandidateConnectionAsync")]
        [Theory(DisplayName = "GetParentCandidateConnectionAsync produces expected diagnostic on success"), AutoData]
        internal async Task GetParentCandidateConnectionAsync_Produces_Expected_Diagnostic_On_Success(string localUser, string username, IPEndPoint endpoint, int branchLevel, string branchRoot, Guid id)
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);

            var initConn = GetConnectionMock(endpoint);
            initConn.Setup(m => m.HandoffTcpClient())
                .Returns(mocks.TcpClient.Object);

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id)
                .Returns(id);

            // direct fetch fails
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), null))
                .Throws(new Exception());

            // indirect succeeds
            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username, endpoint, It.IsAny<ConnectionOptions>(), mocks.TcpClient.Object))
                .Returns(conn.Object);

            mocks.Waiter.Setup(m => m.Wait<IConnection>(It.IsAny<WaitKey>(), It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(initConn.Object));

            var branchLevelWaitKey = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn.Object.Id);
            var branchRootWaitKey = new WaitKey(Constants.WaitKey.BranchRootMessage, conn.Object.Id);
            var searchWaitKey = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchLevel));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(branchRoot));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                await manager.InvokeMethod<Task<(IMessageConnection Connection, int BranchLevel, string BranchRoot)>>("GetParentCandidateConnectionAsync", username, endpoint, CancellationToken.None);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Attempting simultaneous direct and indirect parent candidate connections"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Soliciting indirect"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Indirect") && s.ContainsInsensitive($"handed off"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Indirect") && s.ContainsInsensitive($"initialized.  Waiting for branch information and first search request"))), Times.Once);
            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Indirect") && s.ContainsInsensitive($"established."))), Times.Once);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Fact(DisplayName = "AddParentConnectionAsync returns if HasParent")]
        internal async Task AddParentConnectionAsync_Returns_If_HasParent()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                ("foo", new IPEndPoint(IPAddress.None, 1)),
            };

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);
                await manager.AddParentConnectionAsync(candidates);
            }

            mocks.Diagnostic.Verify(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Fact(DisplayName = "AddParentConnectionAsync returns if ParentCandidates is empty")]
        internal async Task AddParentConnectionAsync_Returns_If_ParentCandidates_Is_Empty()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>();

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);
            }

            mocks.Diagnostic.Verify(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Fact(DisplayName = "AddParentConnectionAsync returns if distributed network is disabled")]
        internal async Task AddParentConnectionAsync_Returns_If_Distributed_Network_Is_Disabled()
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions(enableDistributedNetwork: false));

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>();

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Parent connection solicitation ignored; distributed network is not enabled."))), Times.Once);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Fact(DisplayName = "AddParentConnectionAsync returns if already processing")]
        internal async Task AddParentConnectionAsync_Returns_If_Already_Processing()
        {
            var (manager, mocks) = GetFixture(options: new SoulseekClientOptions());

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                ("foo", new IPEndPoint(IPAddress.None, 1)),
                ("bar", new IPEndPoint(IPAddress.None, 2)),
            };

            using (manager)
            {
                var semaphore = manager.GetProperty<SemaphoreSlim>("ParentSyncRoot");
                await semaphore.WaitAsync();

                await manager.AddParentConnectionAsync(candidates);
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Parent connection solicitation ignored; already in the process of establishing a connection."))), Times.Once);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Fact(DisplayName = "AddParentConnectionAsync produces warning diagnostic, does not throw, and updates status if no candidates connect")]
        internal async Task AddParentConnectionAsync_Produces_Warning_Diagnostic_Does_Not_Throw_And_Updates_Status_If_No_Candidates_Connect()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                ("foo", new IPEndPoint(IPAddress.None, 1)),
                ("bar", new IPEndPoint(IPAddress.None, 2)),
            };

            using (manager)
            {
                var ex = await Record.ExceptionAsync(() => manager.AddParentConnectionAsync(candidates));

                Assert.Null(ex);
            }

            mocks.Diagnostic.Verify(m => m.Warning("Failed to connect to any of the available parent candidates", It.IsAny<Exception>()), Times.Once);
            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Theory(DisplayName = "AddParentConnectionAsync sets Parent to successful connection"), AutoData]
        internal async Task AddParentConnectionAsync_Sets_Parent_To_Successful_Connection(string localUser, string username1, IPEndPoint endpoint1, string username2, IPEndPoint endpoint2, Guid id1, Guid id2)
        {
            var (manager, mocks) = GetFixture();

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                (username1, endpoint1),
                (username2, endpoint2),
            };

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            // mocks for connection #1
            var conn1 = GetMessageConnectionMock(username1, endpoint1);
            conn1.Setup(m => m.Id)
                .Returns(id1);
            conn1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username1, endpoint1, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            // mocks for connection #2
            var conn2 = GetMessageConnectionMock(username2, endpoint1);
            conn2.Setup(m => m.Id)
                .Returns(id2);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username2, endpoint2, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn2.Object);

            // message mocks, to allow either connection to be established fully
            var branchLevelWaitKey1 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn1.Object.Id);
            var branchRootWaitKey1 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn1.Object.Id);
            var searchWaitKey1 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn1.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var branchLevelWaitKey2 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn2.Object.Id);
            var branchRootWaitKey2 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn2.Object.Id);
            var searchWaitKey2 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn2.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.Delay(5000)); // ensure conn1 completes first

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);

                Assert.Equal(conn1.Object.Username, manager.Parent.Username);
                Assert.Equal(conn1.Object.IPEndPoint.Address, manager.Parent.IPEndPoint.Address);
                Assert.Equal(conn1.Object.IPEndPoint.Port, manager.Parent.IPEndPoint.Port);
            }
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Theory(DisplayName = "AddParentConnectionAsync disposes unselected candidates"), AutoData]
        internal async Task AddParentConnectionAsync_Disposes_Unselected_Candidates(string localUser, string username1, IPEndPoint endpoint1, string username2, IPEndPoint endpoint2, Guid id1, Guid id2)
        {
            var (manager, mocks) = GetFixture();

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                (username1, endpoint1),
                (username2, endpoint2),
            };

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            // mocks for connection #1
            var conn1 = GetMessageConnectionMock(username1, endpoint1);
            conn1.Setup(m => m.Id)
                .Returns(id1);
            conn1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username1, endpoint1, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            // mocks for connection #2
            var conn2 = GetMessageConnectionMock(username2, endpoint2);
            conn2.Setup(m => m.Id)
                .Returns(id2);
            conn2.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username2, endpoint2, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn2.Object);

            // message mocks, to allow either connection to be established fully
            var branchLevelWaitKey1 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn1.Object.Id);
            var branchRootWaitKey1 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn1.Object.Id);
            var searchWaitKey1 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn1.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var branchLevelWaitKey2 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn2.Object.Id);
            var branchRootWaitKey2 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn2.Object.Id);
            var searchWaitKey2 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn2.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.Delay(5000)); // ensure conn1 completes first

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);
            }

            conn2.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Theory(DisplayName = "AddParentConnectionAsync retains unselected candidates in ParentCandidateList"), AutoData]
        internal async Task AddParentConnectionAsync_Retains_Unselected_Candidates_In_ParentCandidateList(string localUser, string username1, IPEndPoint endpoint1, string username2, IPEndPoint endpoint2, Guid id1, Guid id2)
        {
            var (manager, mocks) = GetFixture();

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                (username1, endpoint1),
                (username2, endpoint2),
            };

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            // mocks for connection #1
            var conn1 = GetMessageConnectionMock(username1, endpoint1);
            conn1.Setup(m => m.Id)
                .Returns(id1);
            conn1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username1, endpoint1, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            // mocks for connection #2
            var conn2 = GetMessageConnectionMock(username2, endpoint2);
            conn2.Setup(m => m.Id)
                .Returns(id2);
            conn2.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username2, endpoint2, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn2.Object);

            // message mocks, to allow either connection to be established fully
            var branchLevelWaitKey1 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn1.Object.Id);
            var branchRootWaitKey1 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn1.Object.Id);
            var searchWaitKey1 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn1.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var branchLevelWaitKey2 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn2.Object.Id);
            var branchRootWaitKey2 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn2.Object.Id);
            var searchWaitKey2 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn2.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.Delay(5000)); // ensure conn1 completes first

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);

                var goodCandidates = manager.GetProperty<List<(string Username, IPEndPoint IPEndPoint)>>("ParentCandidateList");

                Assert.Single(goodCandidates);
                Assert.Equal(candidates[1], goodCandidates[0]);
            }

            conn2.Verify(m => m.Dispose(), Times.Once);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Theory(DisplayName = "AddParentConnectionAsync retains only connected unselected candidates in ParentCandidateList"), AutoData]
        internal async Task AddParentConnectionAsync_Retains_Only_Connected_Unselected_Candidates_In_ParentCandidateList(string localUser, string username1, IPEndPoint endpoint1, string username2, IPEndPoint endpoint2, Guid id1, Guid id2)
        {
            var (manager, mocks) = GetFixture();

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                (username1, endpoint1),
                (username2, endpoint2),
            };

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            // mocks for connection #1
            var conn1 = GetMessageConnectionMock(username1, endpoint1);
            conn1.Setup(m => m.Id)
                .Returns(id1);
            conn1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username1, endpoint1, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            // mocks for connection #2
            var conn2 = GetMessageConnectionMock(username2, endpoint2);
            conn2.Setup(m => m.Id)
                .Returns(id2);
            conn2.Setup(m => m.State)
                .Returns(ConnectionState.Disconnected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username2, endpoint2, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn2.Object);

            // message mocks, to allow either connection to be established fully
            var branchLevelWaitKey1 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn1.Object.Id);
            var branchRootWaitKey1 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn1.Object.Id);
            var searchWaitKey1 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn1.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var branchLevelWaitKey2 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn2.Object.Id);
            var branchRootWaitKey2 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn2.Object.Id);
            var searchWaitKey2 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn2.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.Delay(5000)); // ensure conn1 completes first

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);

                var goodCandidates = manager.GetProperty<List<(string Username, IPEndPoint IPEndPoint)>>("ParentCandidateList");

                Assert.Empty(goodCandidates);
            }
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Theory(DisplayName = "AddParentConnectionAsync does not throw if only one parent candidate connects"), AutoData]
        internal async Task AddParentConnectionAsync_Does_Not_Throw_If_ParentCandidateList_Is_Empty(string localUser, string username1, IPEndPoint endpoint1, Guid id1)
        {
            var (manager, mocks) = GetFixture();

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                (username1, endpoint1),
            };

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            // mocks for connection #1
            var conn1 = GetMessageConnectionMock(username1, endpoint1);
            conn1.Setup(m => m.Id)
                .Returns(id1);
            conn1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username1, endpoint1, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            // message mocks, to allow either connection to be established fully
            var branchLevelWaitKey1 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn1.Object.Id);
            var branchRootWaitKey1 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn1.Object.Id);
            var searchWaitKey1 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn1.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);

                var goodCandidates = manager.GetProperty<List<(string Username, IPEndPoint IPEndPoint)>>("ParentCandidateList");

                Assert.Empty(goodCandidates);
            }
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Theory(DisplayName = "AddParentConnectionAsync produces expected diagnostics on connect"), AutoData]
        internal async Task AddParentConnectionAsync_Produces_Expected_Diagnostic_On_Connect(string localUser, string username1, IPEndPoint endpoint1, string username2, IPEndPoint endpoint2, Guid id1, Guid id2)
        {
            var (manager, mocks) = GetFixture();

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                (username1, endpoint1),
                (username2, endpoint2),
            };

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            // mocks for connection #1
            var conn1 = GetMessageConnectionMock(username1, endpoint1);
            conn1.Setup(m => m.Id)
                .Returns(id1);
            conn1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username1, endpoint1, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            // mocks for connection #2
            var conn2 = GetMessageConnectionMock(username2, endpoint2);
            conn2.Setup(m => m.Id)
                .Returns(id2);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username2, endpoint2, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn2.Object);

            // message mocks, to allow either connection to be established fully
            var branchLevelWaitKey1 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn1.Object.Id);
            var branchRootWaitKey1 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn1.Object.Id);
            var searchWaitKey1 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn1.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var branchLevelWaitKey2 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn2.Object.Id);
            var branchRootWaitKey2 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn2.Object.Id);
            var searchWaitKey2 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn2.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(0));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.Delay(5000)); // ensure conn1 completes first

            using (manager)
            {
                await manager.AddParentConnectionAsync(candidates);
            }

            mocks.Diagnostic.Verify(m => m.Info(It.Is<string>(s => s == $"Attempting to establish a new parent connection from {candidates.Count} candidates")), Times.Once);
            mocks.Diagnostic.Verify(m => m.Info(It.Is<string>(s => s == $"Adopted parent connection to {conn1.Object.Username} ({conn1.Object.IPEndPoint})")), Times.Once);
        }

        [Trait("Category", "AddParentConnectionAsync")]
        [Theory(DisplayName = "AddParentConnectionAsync raises ParentAdopted and StateChanged on connect"), AutoData]
        internal async Task AddParentConnectionAsync_Raises_ParentAdopted_and_StateChanged_On_Connect(string localUser, string username1, IPEndPoint endpoint1, string username2, IPEndPoint endpoint2, Guid id1, Guid id2)
        {
            var (manager, mocks) = GetFixture();

            var candidates = new List<(string Username, IPEndPoint IPEndPoint)>
            {
                (username1, endpoint1),
                (username2, endpoint2),
            };

            mocks.Client.Setup(m => m.Username)
                .Returns(localUser);
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            // mocks for connection #1
            var conn1 = GetMessageConnectionMock(username1, endpoint1);
            conn1.Setup(m => m.Id)
                .Returns(id1);
            conn1.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username1, endpoint1, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn1.Object);

            // mocks for connection #2
            var conn2 = GetMessageConnectionMock(username2, endpoint2);
            conn2.Setup(m => m.Id)
                .Returns(id2);

            mocks.ConnectionFactory.Setup(m => m.GetDistributedConnection(username2, endpoint2, It.IsAny<ConnectionOptions>(), It.IsAny<ITcpClient>()))
                .Returns(conn2.Object);

            // message mocks, to allow either connection to be established fully
            var branchLevelWaitKey1 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn1.Object.Id);
            var branchRootWaitKey1 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn1.Object.Id);
            var searchWaitKey1 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn1.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(1));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo1"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey1, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.CompletedTask);

            var branchLevelWaitKey2 = new WaitKey(Constants.WaitKey.BranchLevelMessage, conn2.Object.Id);
            var branchRootWaitKey2 = new WaitKey(Constants.WaitKey.BranchRootMessage, conn2.Object.Id);
            var searchWaitKey2 = new WaitKey(Constants.WaitKey.SearchRequestMessage, conn2.Object.Id);
            mocks.Waiter.Setup(m => m.Wait<int>(branchLevelWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(2));
            mocks.Waiter.Setup(m => m.Wait<string>(branchRootWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult("foo2"));
            mocks.Waiter.Setup(m => m.Wait(searchWaitKey2, It.IsAny<int?>(), It.IsAny<CancellationToken?>()))
                .Returns(Task.Delay(5000)); // ensure conn1 completes first

            using (manager)
            {
                DistributedParentEventArgs actualArgs = default;
                bool stateChangedFired = false;

                manager.ParentAdopted += (sender, args) => actualArgs = args;
                manager.StateChanged += (sender, args) => stateChangedFired = true;

                await manager.AddParentConnectionAsync(candidates);

                Assert.Equal(username1, actualArgs.Username);
                Assert.Equal(endpoint1, actualArgs.IPEndPoint);
                Assert.Equal(1, actualArgs.BranchLevel);
                Assert.Equal("foo1", actualArgs.BranchRoot);

                Assert.True(stateChangedFired);
            }
        }

        [Trait("Category", "WaitForParentCandidateConnection_MessageRead")]
        [Theory(DisplayName = "WaitForParentCandidateConnection_MessageRead completes search wait on search request"), AutoData]
        internal void WaitForParentCandidateConnection_MessageRead_Completes_Search_Wait_On_Search_Request(string username, IPEndPoint endpoint, Guid id, int token, string query)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id).Returns(id);

            var key = new WaitKey(Constants.WaitKey.SearchRequestMessage, id);
            var args = new MessageEventArgs(new DistributedSearchRequest(username, token, query).ToByteArray());

            using (manager)
            {
                manager.InvokeMethod("WaitForParentCandidateConnection_MessageRead", conn.Object, args);
            }

            mocks.Waiter.Verify(m => m.Complete(key));
        }

        [Trait("Category", "WaitForParentCandidateConnection_MessageRead")]
        [Theory(DisplayName = "WaitForParentCandidateConnection_MessageRead completes search wait on server search request"), AutoData]
        internal void WaitForParentCandidateConnection_MessageRead_Completes_Search_Wait_On_Server_Search_Request(string username, int token, string query, IPEndPoint endpoint, Guid id)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id).Returns(id);

            var key = new WaitKey(Constants.WaitKey.SearchRequestMessage, id);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.EmbeddedMessage)
                .WriteByte(0x03)
                .WriteInteger(49)
                .WriteString(username)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var args = new MessageEventArgs(msg);

            using (manager)
            {
                manager.InvokeMethod("WaitForParentCandidateConnection_MessageRead", conn.Object, args);
            }

            mocks.Waiter.Verify(m => m.Complete(key));
        }

        [Trait("Category", "WaitForParentCandidateConnection_MessageRead")]
        [Theory(DisplayName = "WaitForParentCandidateConnection_MessageRead completes branchlevel wait on branchlevel"), AutoData]
        internal void ParentCandidateConnection_MessageRead_Completes_BranchLevel_Wait_On_BranchLevel(string username, IPEndPoint endpoint, Guid id, int level)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id).Returns(id);

            var key = new WaitKey(Constants.WaitKey.BranchLevelMessage, id);
            var args = new MessageEventArgs(new DistributedBranchLevel(level).ToByteArray());

            using (manager)
            {
                manager.InvokeMethod("WaitForParentCandidateConnection_MessageRead", conn.Object, args);
            }

            mocks.Waiter.Verify(m => m.Complete(key, level));
        }

        [Trait("Category", "WaitForParentCandidateConnection_MessageRead")]
        [Theory(DisplayName = "WaitForParentCandidateConnection_MessageRead completes branchroot wait on branchroot"), AutoData]
        internal void WaitForParentCandidateConnection_MessageRead_Completes_BranchRoot_Wait_On_BranchRoot(string username, IPEndPoint endpoint, Guid id, string root)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);
            conn.Setup(m => m.Id).Returns(id);

            var key = new WaitKey(Constants.WaitKey.BranchRootMessage, id);
            var args = new MessageEventArgs(new DistributedBranchRoot(root).ToByteArray());

            using (manager)
            {
                manager.InvokeMethod<string>("WaitForParentCandidateConnection_MessageRead", conn.Object, args);
            }

            mocks.Waiter.Verify(m => m.Complete(key, root));
        }

        [Trait("Category", "WaitForParentCandidateConnection_MessageRead")]
        [Theory(DisplayName = "WaitForParentCandidateConnection_MessageRead ignores all other messages"), AutoData]
        internal void WaitForParentCandidateConnection_MessageRead_Ignores_All_Other_Messages(string username, IPEndPoint endpoint)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            var args = new MessageEventArgs(new ServerPing().ToByteArray());

            using (manager)
            {
                var ex = Record.Exception(() => manager.InvokeMethod("WaitForParentCandidateConnection_MessageRead", conn.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "WaitForParentCandidateConnection_MessageRead")]
        [Theory(DisplayName = "WaitForParentCandidateConnection_MessageRead disconnects and disposes on exception"), AutoData]
        internal void WaitForParentCandidateConnection_MessageRead_Disconnects_And_Disposes_On_Exception(string username, IPEndPoint endpoint)
        {
            var (manager, _) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            var args = new MessageEventArgs(new byte[4]);

            using (manager)
            {
                var ex = Record.Exception(() => manager.InvokeMethod("WaitForParentCandidateConnection_MessageRead", conn.Object, args));

                Assert.Null(ex); // should swallow
            }

            conn.Verify(m => m.Disconnect(It.IsAny<string>(), It.IsAny<Exception>()));
            conn.Verify(m => m.Dispose());
        }

        [Trait("Category", "WaitForParentCandidateConnection_MessageRead")]
        [Theory(DisplayName = "WaitForParentCandidateConnection_MessageRead produces expected diagnostic on exception"), AutoData]
        internal void WaitForParentCandidateConnection_MessageRead_Produces_Expected_Diagnostic_On_Exception(string username, IPEndPoint endpoint)
        {
            var (manager, mocks) = GetFixture();

            var conn = GetMessageConnectionMock(username, endpoint);

            var args = new MessageEventArgs(new byte[4]);

            using (manager)
            {
                var ex = Record.Exception(() => manager.InvokeMethod("WaitForParentCandidateConnection_MessageRead", conn.Object, args));

                Assert.Null(ex); // should swallow
            }

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to handle message from parent candidate")), It.IsAny<Exception>()));
        }

        [Trait("Category", "Watchdog")]
        [Fact(DisplayName = "Watchdog produces warning when no parent connected")]
        internal void Watchdog_Produces_Warning_When_No_Parent_Connected()
        {
            var (manager, mocks) = GetFixture();
            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            using (manager)
            {
                manager.InvokeMethod("WatchdogTimer_Elapsed", null, null);
            }

            mocks.Diagnostic.Verify(m => m.Warning("No distributed parent connected.  Requesting a list of candidates.", null), Times.Once);
        }

        [Trait("Category", "Watchdog")]
        [Fact(DisplayName = "Watchdog does not produce a warning when server is not connected and logged in")]
        internal void Watchdog_Does_Not_Produce_A_Warning_When_Server_Is_Not_Connected_And_Logged_In()
        {
            var (manager, mocks) = GetFixture();

            using (manager)
            {
                manager.InvokeMethod("WatchdogTimer_Elapsed", null, null);
            }

            mocks.Diagnostic.Verify(m => m.Warning("No distributed parent connected.  Requesting a list of candidates.", null), Times.Never);
        }

        [Trait("Category", "GetBranchInformation")]
        [Theory(DisplayName = "GetBranchInformation returns expected info when root is established"), AutoData]
        internal void GetBranchInformation_Returns_Expected_Info_When_Root_Is_Established(string root, int level)
        {
            var (manager, _) = GetFixture();

            var parent = new Mock<IMessageConnection>();
            parent.Setup(m => m.State)
                .Returns(ConnectionState.Connected);

            var expected = new List<byte>();

            expected.AddRange(new DistributedBranchLevel(level + 1).ToByteArray());
            expected.AddRange(new DistributedBranchRoot(root).ToByteArray());

            using (manager)
            {
                manager.SetProperty("ParentConnection", parent.Object);
                manager.SetProperty("ParentBranchRoot", root);
                manager.SetProperty("ParentBranchLevel", level);

                var info = manager.InvokeMethod<byte[]>("GetBranchInformation");

                Assert.True(info.Matches(expected.ToArray()));
            }
        }

        [Trait("Category", "GetBranchInformation")]
        [Theory(DisplayName = "GetBranchInformation returns expected info when no root is established"), AutoData]
        internal void GetBranchInformation_Returns_Expected_Info_When_No_Root_Is_Established(string username)
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username)
                .Returns(username);

            var expected = new List<byte>();

            expected.AddRange(new DistributedBranchLevel(0).ToByteArray());
            expected.AddRange(new DistributedBranchRoot(username).ToByteArray());

            using (manager)
            {
                manager.SetProperty("ParentConnection", null);

                var info = manager.InvokeMethod<byte[]>("GetBranchInformation");

                Assert.True(info.Matches(expected.ToArray()));
            }
        }

        [Trait("Category", "ChildConnection_Disconnected")]
        [Fact(DisplayName = "ChildConnection_Disconnected produces expected diagnostic given null message")]
        internal void ChildConnection_Disconnected_Does_Not_Throw_Given_Null_Message()
        {
            var (manager, mocks) = GetFixture();

            var child = new Mock<IMessageConnection>();
            child.Setup(m => m.Username)
                .Returns("username");

            var args = new ConnectionDisconnectedEventArgs(null);

            using (manager)
            {
                manager.InvokeMethod("ChildConnection_Disconnected", child.Object, args);
            }

            mocks.Diagnostic.Verify(m => m.Info(It.Is<string>(s => s.ContainsInsensitive("disconnected."))), Times.Once);
        }

        [Trait("Category", "UpdateStatusEventually")]
        [Fact(DisplayName = "UpdateStatusEventually resets StatusDebounceTimer")]
        internal async Task UpdateStatusEventually_Resets_StatusDebounceTimer()
        {
            var (manager, _) = GetFixture();

            using (manager)
            {
                var timer = manager.GetProperty<System.Timers.Timer>("StatusDebounceTimer");

                Assert.False(timer.Enabled);

                await manager.InvokeMethod<Task>("UpdateStatusEventuallyAsync");

                Assert.True(timer.Enabled);
            }
        }

        [Trait("Category", "UpdateStatusEventually")]
        [Fact(DisplayName = "UpdateStatusEventually updates immediately if stale")]
        internal async Task UpdateStatusEventually_Updates_Immediately_If_Stale()
        {
            var (manager, mocks) = GetFixture();

            mocks.Client.Setup(m => m.State)
                .Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

            using (manager)
            {
                await manager.InvokeMethod<Task>("UpdateStatusEventuallyAsync");

                manager.SetProperty("LastStatusTimestamp", DateTime.UtcNow.AddDays(-365));

                await manager.InvokeMethod<Task>("UpdateStatusEventuallyAsync");
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Once);
        }

        private static (DistributedConnectionManager Manager, Mocks Mocks) GetFixture(string username = null, IPEndPoint endpoint = null, SoulseekClientOptions options = null)
        {
            var mocks = new Mocks(options);

            mocks.ServerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.ServerConnection.Setup(m => m.IPEndPoint)
                .Returns(endpoint ?? new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0));

            var handler = new DistributedConnectionManager(
                mocks.Client.Object,
                mocks.ConnectionFactory.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private static Mock<IMessageConnection> GetMessageConnectionMock(string username, IPEndPoint endpoint)
        {
            var mock = new Mock<IMessageConnection>();
            mock.Setup(m => m.Username).Returns(username);
            mock.Setup(m => m.IPEndPoint).Returns(endpoint ?? new IPEndPoint(IPAddress.None, 0));

            return mock;
        }

        private static Mock<IConnection> GetConnectionMock(IPEndPoint endpoint)
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
                Client.Setup(m => m.DistributedMessageHandler).Returns(DistributedMessageHandler.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public Mock<IListener> Listener { get; } = new Mock<IListener>();
            public Mock<IDistributedMessageHandler> DistributedMessageHandler { get; } = new Mock<IDistributedMessageHandler>();
            public Mock<IConnectionFactory> ConnectionFactory { get; } = new Mock<IConnectionFactory>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<ITcpClient> TcpClient { get; } = new Mock<ITcpClient>();
        }
    }
}
