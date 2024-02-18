// <copyright file="SoulseekClientOptionsPatchTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using AutoFixture.Xunit2;
    using Xunit;

    public class SoulseekClientOptionsPatchTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiation(
            bool? enableListener,
            bool? enableDistributedNetwork,
            bool? acceptDistributedChildren,
            int? distributedChildLimit,
            bool? deduplicateSearchRequests,
            bool? autoAcknowledgePrivateMessages,
            bool? autoAcknowledgePrivilegeNotifications,
            bool? acceptPrivateRoomInvitations)
        {
            var serverConnectionOptions = new ConnectionOptions();
            var peerConnectionOptions = new ConnectionOptions();
            var transferConnectionOptions = new ConnectionOptions();
            var incomingConnectionOptions = new ConnectionOptions();
            var distributedConnectionOptions = new ConnectionOptions();

            var rnd = new Random();
            var listenPort = rnd.Next(1024, 65535);

            var o = new SoulseekClientOptionsPatch(
                enableListener,
                listenPort,
                enableDistributedNetwork: enableDistributedNetwork,
                acceptDistributedChildren: acceptDistributedChildren,
                distributedChildLimit: distributedChildLimit,
                deduplicateSearchRequests: deduplicateSearchRequests,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages,
                autoAcknowledgePrivilegeNotifications: autoAcknowledgePrivilegeNotifications,
                acceptPrivateRoomInvitations: acceptPrivateRoomInvitations,
                serverConnectionOptions: serverConnectionOptions,
                peerConnectionOptions: peerConnectionOptions,
                transferConnectionOptions: transferConnectionOptions,
                incomingConnectionOptions: incomingConnectionOptions,
                distributedConnectionOptions: distributedConnectionOptions);

            Assert.Equal(enableListener, o.EnableListener);
            Assert.Equal(listenPort, o.ListenPort);
            Assert.Equal(enableDistributedNetwork, o.EnableDistributedNetwork);
            Assert.Equal(acceptDistributedChildren, o.AcceptDistributedChildren);
            Assert.Equal(distributedChildLimit, o.DistributedChildLimit);
            Assert.Equal(deduplicateSearchRequests, o.DeduplicateSearchRequests);
            Assert.Equal(autoAcknowledgePrivateMessages, o.AutoAcknowledgePrivateMessages);
            Assert.Equal(autoAcknowledgePrivilegeNotifications, o.AutoAcknowledgePrivilegeNotifications);
            Assert.Equal(acceptPrivateRoomInvitations, o.AcceptPrivateRoomInvitations);
            Assert.Equal(peerConnectionOptions, o.PeerConnectionOptions);
            Assert.Equal(incomingConnectionOptions, o.IncomingConnectionOptions);
            Assert.Equal(distributedConnectionOptions, o.DistributedConnectionOptions);

            Assert.Equal(serverConnectionOptions.ReadBufferSize, o.ServerConnectionOptions.ReadBufferSize);
            Assert.Equal(serverConnectionOptions.WriteBufferSize, o.ServerConnectionOptions.WriteBufferSize);
            Assert.Equal(serverConnectionOptions.ConnectTimeout, o.ServerConnectionOptions.ConnectTimeout);
            Assert.Equal(-1, o.ServerConnectionOptions.InactivityTimeout);

            Assert.Equal(transferConnectionOptions.ReadBufferSize, o.TransferConnectionOptions.ReadBufferSize);
            Assert.Equal(transferConnectionOptions.WriteBufferSize, o.TransferConnectionOptions.WriteBufferSize);
            Assert.Equal(transferConnectionOptions.ConnectTimeout, o.TransferConnectionOptions.ConnectTimeout);
            Assert.Equal(-1, o.TransferConnectionOptions.InactivityTimeout);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with given data")]
        public void Removes_Timeout_On_Server_And_Transfer_Options()
        {
            var serverConnectionOptions = new ConnectionOptions();
            var transferConnectionOptions = new ConnectionOptions();

            var o = new SoulseekClientOptionsPatch(
                serverConnectionOptions: serverConnectionOptions,
                transferConnectionOptions: transferConnectionOptions);

            Assert.Equal(-1, o.ServerConnectionOptions.InactivityTimeout);
            Assert.Equal(-1, o.TransferConnectionOptions.InactivityTimeout);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Does not throw if server and transfer options not given")]
        public void Does_Not_Throw_If_Server_And_Transfer_Options_Not_Given()
        {
            var ex = Record.Exception(() => new SoulseekClientOptionsPatch());

            Assert.Null(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if distributed child limit is less than zero")]
        public void Throws_If_Distributed_Child_Limit_Is_Less_Than_Zero()
        {
            SoulseekClientOptionsPatch x;
            var ex = Record.Exception(() => x = new SoulseekClientOptionsPatch(distributedChildLimit: -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if listen port is too high")]
        public void Throws_If_Listen_Port_Is_Too_High()
        {
            SoulseekClientOptionsPatch x;
            var ex = Record.Exception(() => x = new SoulseekClientOptionsPatch(listenPort: 999999999));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if listen port is too low")]
        public void Throws_If_Listen_Port_Is_Too_Low()
        {
            SoulseekClientOptionsPatch x;
            var ex = Record.Exception(() => x = new SoulseekClientOptionsPatch(listenPort: 1023));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }
    }
}
