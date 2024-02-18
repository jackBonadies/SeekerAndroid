﻿// <copyright file="ReconfigureOptionsAsyncTests.cs" company="JP Dillingham">
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
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ReconfigureOptionsAsyncTests
    {
        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Throws ArgumentNullException given null patch")]
        public async Task Throws_ArgumentNullException_Given_Null_Patch()
        {
            var (client, _) = GetFixture();

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(null));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentNullException>(ex);
                Assert.Equal("patch", ((ArgumentNullException)ex).ParamName);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Throws ListenPortException given listen port which can not be bound")]
        public async Task Throws_ListenPortException_Given_Listen_Port_Which_Can_Not_Be_Bound()
        {
            var (client, mocks) = GetFixture();

            var port = Mocks.Port;
            var patch = new SoulseekClientOptionsPatch(listenPort: port);

            Listener listener = null;

            try
            {
                // listen on the port to bind it
                listener = new Listener(port, new ConnectionOptions());
                listener.Start();

                using (client)
                {
                    var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                    Assert.NotNull(ex);
                    Assert.IsType<ListenPortException>(ex);
                    Assert.True(ex.Message.ContainsInsensitive($"failed to start listening on port {port}"));
                }
            }
            finally
            {
                listener?.Stop();
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Does not throw given empty patch")]
        public async Task Does_Not_Throw_Given_Empty_Patch()
        {
            var (client, _) = GetFixture();

            var patch = new SoulseekClientOptionsPatch();

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Returns true if client connected and EnableDistributedNetwork changed to false")]
        public async Task Returns_True_If_Client_Connected_And_EnableDistributedNetwork_Changed_To_False()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(enableDistributedNetwork: true));

            var patch = new SoulseekClientOptionsPatch(enableDistributedNetwork: false);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var reconnectRequired = await client.ReconfigureOptionsAsync(patch);

                Assert.True(reconnectRequired);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Returns false if client connected and EnableDistributedNetwork changed to true")]
        public async Task Returns_False_If_Client_Connected_And_EnableDistributedNetwork_Changed_To_True()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(enableDistributedNetwork: false));

            var patch = new SoulseekClientOptionsPatch(enableDistributedNetwork: true);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var reconnectRequired = await client.ReconfigureOptionsAsync(patch);

                Assert.False(reconnectRequired);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Configures distributed network")]
        public async Task Configures_Distributed_Network_()
        {
            var (client, mocks) = GetFixture(new SoulseekClientOptions(enableDistributedNetwork: false));

            var patch = new SoulseekClientOptionsPatch(enableDistributedNetwork: true);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await client.ReconfigureOptionsAsync(patch);
            }

            mocks.DistributedConnectionManager.Verify(m => m.UpdateStatusAsync(It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Returns true if client connected and DistributedConnectionOptions changed")]
        public async Task Returns_True_If_Client_Connected_And_DistributedConnectionOptions_Changed()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(distributedConnectionOptions: new ConnectionOptions()));

            var patch = new SoulseekClientOptionsPatch(distributedConnectionOptions: new ConnectionOptions());

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var reconnectRequired = await client.ReconfigureOptionsAsync(patch);

                Assert.True(reconnectRequired);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Returns true if client connected and ServerConnectionOptions changed")]
        public async Task Returns_True_If_Client_Connected_And_ServerConnectionOptions_Changed()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(serverConnectionOptions: new ConnectionOptions()));

            var patch = new SoulseekClientOptionsPatch(serverConnectionOptions: new ConnectionOptions());

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var reconnectRequired = await client.ReconfigureOptionsAsync(patch);

                Assert.True(reconnectRequired);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Nulls listener if EnableListener changed from true to false")]
        public async Task Nulls_Listener_If_EnableListener_Changed()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(enableListener: true));

            var patch = new SoulseekClientOptionsPatch(enableListener: false);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await client.ReconfigureOptionsAsync(patch);

                Assert.Null(client.Listener);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Reconfigures listener if EnableListener changed from false to true")]
        public async Task Reconfigures_Listener_If_EnableListener_Changed()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(enableListener: false));

            var patch = new SoulseekClientOptionsPatch(enableListener: true);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                var ogListener = client.Listener;

                await client.ReconfigureOptionsAsync(patch);

                Assert.NotEqual(ogListener, client.Listener);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Sends SetListenPortCommand if EnableListener changed from false to true")]
        public async Task Sends_SetListenPortCommand_If_EnableListener_Changed_From_False_To_true()
        {
            var (client, mocks) = GetFixture(new SoulseekClientOptions(enableListener: false));

            var patch = new SoulseekClientOptionsPatch(enableListener: true);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await client.ReconfigureOptionsAsync(patch);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<SetListenPortCommand>(), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Does not throw if Listener is null")]
        public async Task Does_Not_Throw_If_Listener_Is_Null()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(enableListener: true));

            var patch = new SoulseekClientOptionsPatch(enableListener: false);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                client.SetProperty("Listener", null);

                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Reconfigures listener if ListenPort changed")]
        public async Task Reconfigures_Listener_If_ListenPort_Changed()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(listenPort: Mocks.Port));

            var patch = new SoulseekClientOptionsPatch(listenPort: Mocks.Port);

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await client.ReconfigureOptionsAsync(patch);

                Assert.Equal(patch.ListenPort, client.Listener.Port);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Reconfigures listener if IncomingConnectionOptions changed")]
        public async Task Reconfigures_Listener_If_IncomingConnectionOptions_Changed()
        {
            var (client, _) = GetFixture(new SoulseekClientOptions(incomingConnectionOptions: new ConnectionOptions()));

            var patch = new SoulseekClientOptionsPatch(incomingConnectionOptions: new ConnectionOptions());

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await client.ReconfigureOptionsAsync(patch);

                Assert.Equal(patch.IncomingConnectionOptions, client.Listener.ConnectionOptions);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Sends PrivateRoomToggle")]
        public async Task Sends_PrivateRoomToggle()
        {
            var (client, mocks) = GetFixture(new SoulseekClientOptions());

            var patch = new SoulseekClientOptionsPatch();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                await client.ReconfigureOptionsAsync(patch);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<PrivateRoomToggle>(), It.IsAny<CancellationToken?>()));
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Does not send PrivateRoomToggle if not connected")]
        public async Task Does_Not_Send_PrivateRoomToggle_If_Not_Connected()
        {
            var (client, mocks) = GetFixture(new SoulseekClientOptions());

            var patch = new SoulseekClientOptionsPatch();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Disconnected);

                await client.ReconfigureOptionsAsync(patch);
            }

            mocks.ServerConnection.Verify(m => m.WriteAsync(It.IsAny<PrivateRoomToggle>(), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Updates options")]
        public async Task Updates_Options()
        {
            var (client, mocks) = GetFixture(new SoulseekClientOptions(
                enableListener: false,
                listenPort: Mocks.Port,
                enableDistributedNetwork: false,
                acceptDistributedChildren: false,
                distributedChildLimit: 5,
                deduplicateSearchRequests: true,
                autoAcknowledgePrivateMessages: false,
                autoAcknowledgePrivilegeNotifications: false,
                acceptPrivateRoomInvitations: false,
                serverConnectionOptions: new ConnectionOptions(readBufferSize: 10),
                peerConnectionOptions: new ConnectionOptions(),
                transferConnectionOptions: new ConnectionOptions(readBufferSize: 20),
                incomingConnectionOptions: new ConnectionOptions(),
                distributedConnectionOptions: new ConnectionOptions()));

            var patch = new SoulseekClientOptionsPatch(
                enableListener: true,
                listenPort: Mocks.Port,
                enableDistributedNetwork: true,
                acceptDistributedChildren: true,
                distributedChildLimit: 10,
                deduplicateSearchRequests: false,
                autoAcknowledgePrivateMessages: true,
                autoAcknowledgePrivilegeNotifications: true,
                acceptPrivateRoomInvitations: true,
                serverConnectionOptions: new ConnectionOptions(readBufferSize: 100),
                peerConnectionOptions: new ConnectionOptions(),
                transferConnectionOptions: new ConnectionOptions(readBufferSize: 200),
                incomingConnectionOptions: new ConnectionOptions(),
                distributedConnectionOptions: new ConnectionOptions());

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Disconnected);

                await client.ReconfigureOptionsAsync(patch);

                Assert.Equal(patch.EnableListener, client.Options.EnableListener);
                Assert.Equal(patch.ListenPort, client.Options.ListenPort);
                Assert.Equal(patch.EnableDistributedNetwork, client.Options.EnableDistributedNetwork);
                Assert.Equal(patch.AcceptDistributedChildren, client.Options.AcceptDistributedChildren);
                Assert.Equal(patch.DistributedChildLimit, client.Options.DistributedChildLimit);
                Assert.Equal(patch.DeduplicateSearchRequests, client.Options.DeduplicateSearchRequests);
                Assert.Equal(patch.AutoAcknowledgePrivateMessages, client.Options.AutoAcknowledgePrivateMessages);
                Assert.Equal(patch.AutoAcknowledgePrivilegeNotifications, client.Options.AutoAcknowledgePrivilegeNotifications);
                Assert.Equal(patch.AcceptPrivateRoomInvitations, client.Options.AcceptPrivateRoomInvitations);
                Assert.Equal(patch.PeerConnectionOptions, client.Options.PeerConnectionOptions);
                Assert.Equal(patch.IncomingConnectionOptions, client.Options.IncomingConnectionOptions);
                Assert.Equal(patch.DistributedConnectionOptions, client.Options.DistributedConnectionOptions);

                Assert.Equal(patch.ServerConnectionOptions.ReadBufferSize, client.Options.ServerConnectionOptions.ReadBufferSize);
                Assert.Equal(patch.TransferConnectionOptions.ReadBufferSize, client.Options.TransferConnectionOptions.ReadBufferSize);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Throws OperationCanceledException when cancelled")]
        public async Task Throws_OperationCanceledException_When_Cancelled()
        {
            var (client, mocks) = GetFixture(new SoulseekClientOptions());
            mocks.ServerConnection
                .Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Throws(new OperationCanceledException());

            var patch = new SoulseekClientOptionsPatch();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Throws TimeoutException when timed out")]
        public async Task Throws_TimeoutException_When_Timed_Out()
        {
            var (client, mocks) = GetFixture(new SoulseekClientOptions());
            mocks.ServerConnection
                .Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Throws(new TimeoutException());

            var patch = new SoulseekClientOptionsPatch();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Throws SoulseekClientException on any other error")]
        public async Task Throws_SoulseekClientException_On_Any_Other_Error()
        {
            var expectedEx = new Exception("foo");

            var (client, mocks) = GetFixture(new SoulseekClientOptions());
            mocks.ServerConnection
                .Setup(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()))
                .Throws(expectedEx);

            var patch = new SoulseekClientOptionsPatch();

            using (client)
            {
                client.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.Equal(expectedEx, ex.InnerException);
            }
        }

        private (SoulseekClient client, Mocks Mocks) GetFixture(SoulseekClientOptions clientOptions = null)
        {
            var mocks = new Mocks();
            var client = new SoulseekClient(
                distributedConnectionManager: mocks.DistributedConnectionManager.Object,
                connectionFactory: mocks.ConnectionFactory.Object,
                serverConnection: mocks.ServerConnection.Object,
                listener: mocks.Listener.Object,
                options: clientOptions ?? new SoulseekClientOptions(enableListener: false));

            return (client, mocks);
        }

        private class Mocks
        {
            public Mocks()
            {
                ConnectionFactory = new Mock<IConnectionFactory>();
                ConnectionFactory.Setup(m => m.GetServerConnection(
                    It.IsAny<IPEndPoint>(),
                    It.IsAny<EventHandler>(),
                    It.IsAny<EventHandler<ConnectionDisconnectedEventArgs>>(),
                    It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<EventHandler<MessageEventArgs>>(),
                    It.IsAny<ConnectionOptions>(),
                    It.IsAny<ITcpClient>()))
                    .Returns(ServerConnection.Object);

                DistributedConnectionManager = new Mock<IDistributedConnectionManager>();
                DistributedConnectionManager.Setup(m => m.BranchLevel).Returns(0);
                DistributedConnectionManager.Setup(m => m.BranchRoot).Returns(string.Empty);
            }

            private static readonly Random Rng = new Random();
            public static int Port => Rng.Next(1024, IPEndPoint.MaxPort);

            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IConnectionFactory> ConnectionFactory { get; }
            public Mock<IListener> Listener { get; } = new Mock<IListener>();
            public Mock<IDistributedConnectionManager> DistributedConnectionManager { get; }
        }
    }
}
