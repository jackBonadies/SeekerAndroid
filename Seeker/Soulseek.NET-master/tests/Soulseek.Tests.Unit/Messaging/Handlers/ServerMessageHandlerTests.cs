// <copyright file="ServerMessageHandlerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Handlers
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

    public class ServerMessageHandlerTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws given null SoulseekClient")]
        public void Instantiation_Throws_Given_Null_SoulseekClient()
        {
            var ex = Record.Exception(() => new ServerMessageHandler(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("soulseekClient", ((ArgumentNullException)ex).ParamName);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates diagnostic on message")]
        public void Creates_Diagnostic_On_Message()
        {
            var (handler, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentMinSpeed)
                .WriteInteger(1)
                .Build();

            handler.HandleMessageRead(null, new MessageEventArgs(message));

            mocks.Diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates unhandled diagnostic on unhandled message")]
        public void Creates_Unhandled_Diagnostic_On_Unhandled_Message()
        {
            string msg = null;
            var (handler, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(m => msg = m);

            var message = new MessageBuilder().WriteCode(MessageCode.Server.AskPublicChat).Build();

            handler.HandleMessageRead(null, message);

            mocks.Diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Exactly(2));

            Assert.Contains("Unhandled", msg, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on diagnostic"), AutoData]
        public void Raises_DiagnosticGenerated_On_Diagnostic(string message)
        {
            using (var client = new SoulseekClient(minorVersion: 9999, options: null))
            {
                DiagnosticEventArgs args = default;

                ServerMessageHandler l = new ServerMessageHandler(client);
                l.DiagnosticGenerated += (sender, e) => args = e;

                var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");
                diagnostic.Info(message);

                Assert.Equal(message, args.Message);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Does not throw raising DiagnosticGenerated if no handlers bound"), AutoData]
        public void Does_Not_Throw_Raising_DiagnosticGenerated_If_No_Handlers_Bound(string message)
        {
            using (var client = new SoulseekClient(minorVersion: 9999, options: null))
            {
                ServerMessageHandler l = new ServerMessageHandler(client);

                var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");

                var ex = Record.Exception(() => diagnostic.Info(message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerGetPeerAddress"), AutoData]
        public void Handles_ServerGetPeerAddress(string username, IPAddress ip, int port)
        {
            UserAddressResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<UserAddressResponse>()))
                .Callback<WaitKey, UserAddressResponse>((key, response) => result = response);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString(username)
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .Build();

            handler.HandleMessageRead(null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(ip, result.IPAddress);
            Assert.Equal(port, result.Port);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateMessageReceived event on ServerPrivateMessage"), AutoData]
        public void Raises_PrivateMessageRecieved_Event_On_ServerPrivateMessage(int id, int timeOffset, string username, string message, bool replayed)
        {
            var options = new SoulseekClientOptions(autoAcknowledgePrivateMessages: false);
            var (handler, _) = GetFixture(options);

            var epoch = DateTime.UnixEpoch;
            var timestamp = epoch.AddSeconds(timeOffset);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(id)
                .WriteInteger(timeOffset)
                .WriteString(username)
                .WriteString(message)
                .WriteByte((byte)(replayed ? 0 : 1))
                .Build();

            PrivateMessageReceivedEventArgs response = null;
            handler.PrivateMessageReceived += (_, privateMessage) => response = privateMessage;

            handler.HandleMessageRead(null, msg);

            Assert.NotNull(response);
            Assert.Equal(id, response.Id);
            Assert.Equal(timestamp, response.Timestamp);
            Assert.Equal(username, response.Username);
            Assert.Equal(message, response.Message);
            Assert.Equal(replayed, response.Replayed);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises UserCannotConnect event on CannotConnect if username"), AutoData]
        public void Raises_UserCannotConnect_Event_On_CannotConnect_If_Username(int token, string username)
        {
            var (handler, _) = GetFixture();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(token)
                .WriteString(username)
                .Build();

            UserCannotConnectEventArgs response = null;
            handler.UserCannotConnect += (_, cannotConnect) => response = cannotConnect;

            handler.HandleMessageRead(null, msg);

            Assert.NotNull(response);
            Assert.Equal(token, response.Token);
            Assert.Equal(username, response.Username);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises UserCannotConnect event on CannotConnect if no username"), AutoData]
        public void Does_Not_Raise_UserCannotConnect_Event_On_CannotConnect_If_No_Username(int token)
        {
            var (handler, _) = GetFixture();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(token)
                .Build();

            UserCannotConnectEventArgs response = null;
            handler.UserCannotConnect += (_, cannotConnect) => response = cannotConnect;

            handler.HandleMessageRead(null, msg);

            Assert.Null(response);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Discards SearchResponse on CannotConnect"), AutoData]
        public void Discards_SearchResponse_On_CannotConnect(int token)
        {
            var (handler, mocks) = GetFixture();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(token)
                .Build();

            handler.HandleMessageRead(null, msg);

            mocks.SearchResponder.Verify(m => m.TryDiscard(token), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on CannotConnect if UserCannotConnect event is unbound"), AutoData]
        public void Does_Not_Throw_On_CannotConnect_If_UserCannotConnect_Event_Is_Unbound(int token, string username)
        {
            var (handler, _) = GetFixture();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(token)
                .WriteString(username)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, msg));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Acknowledges ServerPrivateMessage"), AutoData]
        internal void Acknowledges_ServerPrivateMessage(int id, int timeOffset, string username, string message, bool isAdmin)
        {
            var options = new SoulseekClientOptions(autoAcknowledgePrivateMessages: true);
            var (handler, mocks) = GetFixture(options);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(id)
                .WriteInteger(timeOffset)
                .WriteString(username)
                .WriteString(message)
                .WriteByte((byte)(isAdmin ? 1 : 0))
                .Build();

            handler.HandleMessageRead(null, msg);

            mocks.Client.Verify(m =>
                m.AcknowledgePrivateMessageAsync(id, It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Handles ParentMinSpeed")]
        internal void Handles_ParentMinSpeed()
        {
            int value = new Random().Next();
            ServerInfo result = null;

            var (handler, _) = GetFixture();

            handler.ServerInfoReceived += (_, arg) => result = arg;

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentMinSpeed)
                .WriteInteger(value)
                .Build();

            handler.HandleMessageRead(null, msg);

            Assert.Equal(value, result.ParentMinSpeed);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Handles ParentSpeedRatio")]
        internal void Handles_ParentSpeedRatio()
        {
            int value = new Random().Next();
            ServerInfo result = null;

            var (handler, _) = GetFixture();

            handler.ServerInfoReceived += (_, arg) => result = arg;

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentSpeedRatio)
                .WriteInteger(value)
                .Build();

            handler.HandleMessageRead(null, msg);

            Assert.Equal(value, result.ParentSpeedRatio);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Handles WishlistInterval")]
        internal void Handles_WishlistInterval()
        {
            int value = new Random().Next();
            ServerInfo result = null;

            var (handler, _) = GetFixture();

            handler.ServerInfoReceived += (_, arg) => result = arg;

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WishlistInterval)
                .WriteInteger(value)
                .Build();

            handler.HandleMessageRead(null, msg);

            Assert.Equal(value, result.WishlistInterval);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles IntegerResponse messages")]
        [InlineData(MessageCode.Server.CheckPrivileges)]
        internal void Handles_IntegerResponse_Messages(MessageCode.Server code)
        {
            int value = new Random().Next();
            int? result = null;

            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<int>()))
                .Callback<WaitKey, int>((key, response) => result = response);

            var msg = new MessageBuilder()
                .WriteCode(code)
                .WriteInteger(value)
                .Build();

            handler.HandleMessageRead(null, msg);

            Assert.Equal(value, result);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on ServerInfo messages when ServerInfoReceived is unbound")]
        [InlineData(MessageCode.Server.ParentMinSpeed)]
        [InlineData(MessageCode.Server.ParentSpeedRatio)]
        [InlineData(MessageCode.Server.WishlistInterval)]
        internal void Does_Not_Throw_On_ServerInfo_When_ServerInfoReceived_Is_Unbound(MessageCode.Server code)
        {
            var (handler, _) = GetFixture();

            var msg = new MessageBuilder()
                .WriteCode(code)
                .WriteInteger(42)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, msg));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerLogin"), AutoData]
        public void Handles_ServerLogin(bool success, string message, IPAddress ip, string hash, bool isSupporter)
        {
            LoginResponse result = null;

            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<LoginResponse>()))
                .Callback<WaitKey, LoginResponse>((key, response) => result = response);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteByte((byte)(success ? 1 : 0))
                .WriteString(message)
                .WriteBytes(ipBytes)
                .WriteString(hash)
                .WriteByte((byte)(isSupporter ? 1 : 0))
                .Build();

            handler.HandleMessageRead(null, msg);

            Assert.Equal(success, result.Succeeded);
            Assert.Equal(message, result.Message);
            Assert.Equal(ip, result.IPAddress);
            Assert.Equal(hash, result.Hash);
            Assert.Equal(isSupporter, result.IsSupporter);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerRoomList"), AutoData]
        public void Handles_ServerRoomList(List<(string Name, int UserCount)> rooms)
        {
            RoomList result = null;

            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<RoomList>()))
                .Callback<WaitKey, RoomList>((key, response) => result = response);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList);

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));

            handler.HandleMessageRead(null, builder.Build());

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.Public, r => r.Name == name);
            }

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.Private, r => r.Name == name);
            }

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.Owned, r => r.Name == name);
            }

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.ModeratedRoomNames, r => r == name);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises RoomListReceived"), AutoData]
        public void Raises_RoomListReceived(List<(string Name, int UserCount)> rooms)
        {
            RoomList result = null;

            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<RoomList>()))
                .Callback<WaitKey, RoomList>((key, response) => result = response);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList);

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteString(room.Name));

            handler.HandleMessageRead(null, builder.Build());

            handler.RoomListReceived += (sender, e) => result = e;

            handler.HandleMessageRead(null, builder.Build());

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.Public, r => r.Name == name);
            }

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.Private, r => r.Name == name);
            }

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.Owned, r => r.Name == name);
            }

            foreach (var (name, _) in rooms)
            {
                Assert.Contains(result.ModeratedRoomNames, r => r == name);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivilegedUserListReceived"), AutoData]
        public void Raises_PrivilegedUserListReceived(string[] names)
        {
            IReadOnlyCollection<string> eventResult = null;
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivilegedUsers)
                .WriteInteger(names.Length);

            foreach (var name in names)
            {
                builder.WriteString(name);
            }

            var msg = builder.Build();

            handler.PrivilegedUserListReceived += (sender, e) => eventResult = e;

            handler.HandleMessageRead(null, msg);

            foreach (var name in names)
            {
                Assert.Contains(eventResult, n => n == name);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PrivilegedUserListReceived when unbound"), AutoData]
        public void Does_Not_Throw_On_PrivilegedUserListReceived_When_Unbound(string[] names)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivilegedUsers)
                .WriteInteger(names.Length);

            foreach (var name in names)
            {
                builder.WriteString(name);
            }

            var msg = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, msg));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises ExcludedSearchPhrasesReceived"), AutoData]
        public void Raises_ExcludedSearchPhrasesReceived(string[] names)
        {
            IReadOnlyCollection<string> eventResult = null;
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.ExcludedSearchPhrases)
                .WriteInteger(names.Length);

            foreach (var name in names)
            {
                builder.WriteString(name);
            }

            var msg = builder.Build();

            handler.ExcludedSearchPhrasesReceived += (sender, e) => eventResult = e;

            handler.HandleMessageRead(null, msg);

            foreach (var name in names)
            {
                Assert.Contains(eventResult, n => n == name);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on ExcludedSearchPhrasesReceived when unbound"), AutoData]
        public void Does_Not_Throw_On_ExcludedSearchPhrasesReceived_When_Unbound(string[] names)
        {
            IReadOnlyCollection<string> result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<IReadOnlyCollection<string>>()))
                .Callback<WaitKey, IReadOnlyCollection<string>>((key, response) => result = response);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.ExcludedSearchPhrases)
                .WriteInteger(names.Length);

            foreach (var name in names)
            {
                builder.WriteString(name);
            }

            var msg = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, msg));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates connection on ConnectToPeerResponse 'P'"), AutoData]
        public void Creates_Connection_On_ConnectToPeerResponse_P(string username, int token, IPAddress ip, int port)
        {
            ConnectToPeerResponse response = null;
            var (handler, mocks) = GetFixture();

            mocks.PeerConnectionManager
                .Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<ConnectToPeerResponse>()))
                .Returns(Task.FromResult(new Mock<IMessageConnection>().Object))
                .Callback<ConnectToPeerResponse>((r) => response = r);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("P")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .WriteByte(0)
                .Build();

            handler.HandleMessageRead(null, msg);

            Assert.Equal(username, response.Username);
            Assert.Equal(ip, response.IPAddress);
            Assert.Equal(port, response.Port);

            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(It.IsAny<ConnectToPeerResponse>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores ConnectToPeerResponse 'F' on unexpected connection"), AutoData]
        public void Ignores_ConnectToPeerResponse_F_On_Unexpected_Connection(string username, int token, IPAddress ip, int port)
        {
            var (handler, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("F")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, msg));

            Assert.Null(ex);
            Assert.Empty(mocks.Downloads);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on ignored ConnectToPeerResponse 'F'"), AutoData]
        public void Raises_DiagnosticGenerated_On_Ignored_ConnectToPeerResponse_F(string username, int token, IPAddress ip, int port)
        {
            var mocks = new Mocks();
            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(minimumDiagnosticLevel: DiagnosticLevel.Debug));

            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("F")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .WriteByte(0)
                .Build();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);

            handler.HandleMessageRead(null, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Debug)
                .Where(d => d.Message.IndexOf("ignored", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on unknown ConnectToPeerResponse 'X'"), AutoData]
        public void Raises_DiagnosticGenerated_On_Ignored_ConnectToPeerResponse_X(string username, int token, IPAddress ip, int port)
        {
            var mocks = new Mocks();
            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(minimumDiagnosticLevel: DiagnosticLevel.Debug));

            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("X")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .WriteByte(0)
                .Build();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);

            handler.HandleMessageRead(null, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Debug)
                .Where(d => d.Message.IndexOf("unknown", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Attempts connection on expected ConnectToPeerResponse 'F'"), AutoData]
        public void Attempts_Connection_On_Expected_ConnectToPeerResponse_F(string filename, string username, int token, IPAddress ip, int port)
        {
            var active = new ConcurrentDictionary<int, TransferInternal>();
            active.TryAdd(token, new TransferInternal(TransferDirection.Download, username, filename, token));

            var mocks = new Mocks();
            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var transfer = new TransferInternal(TransferDirection.Download, username, filename, token)
            {
                RemoteToken = token,
            };

            mocks.Downloads.TryAdd(token, transfer);

            var key = new WaitKey(Constants.WaitKey.IndirectTransfer, username, filename, token);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("F")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .WriteByte(0)
                .Build();

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[] { 0, 0, 0, 0 }));

            mocks.PeerConnectionManager.Setup(m => m.GetTransferConnectionAsync(It.IsAny<ConnectToPeerResponse>()))
                .Returns(Task.FromResult((conn.Object, token)));

            handler.HandleMessageRead(null, msg);

            mocks.PeerConnectionManager.Verify(m => m.GetTransferConnectionAsync(It.IsAny<ConnectToPeerResponse>()), Times.Once);
            mocks.Waiter.Verify(m => m.Complete(key, conn.Object));
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores and disconnects connection on unexpected ConnectToPeerResponse 'F'"), AutoData]
        public void Ignores_Connection_On_Unexpected_ConnectToPeerResponse_F(string filename, string username, int token, IPAddress ip, int port)
        {
            var active = new ConcurrentDictionary<int, TransferInternal>();
            active.TryAdd(token, new TransferInternal(TransferDirection.Download, username, filename, token + 1));

            var mocks = new Mocks();
            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var transfer = new TransferInternal(TransferDirection.Download, username, filename, token + 1)
            {
                RemoteToken = token + 1,
            };

            mocks.Downloads.TryAdd(token + 1, transfer); // add a record for this user, but with the wrong token

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("F")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .WriteByte(0)
                .Build();

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[] { 0, 0, 0, 0 }));

            mocks.PeerConnectionManager.Setup(m => m.GetTransferConnectionAsync(It.IsAny<ConnectToPeerResponse>()))
                .Returns(Task.FromResult((conn.Object, token)));

            handler.HandleMessageRead(null, msg);

            mocks.PeerConnectionManager.Verify(m => m.GetTransferConnectionAsync(It.IsAny<ConnectToPeerResponse>()), Times.Once);
            mocks.Waiter.Verify(m => m.Complete(It.IsAny<WaitKey>(), conn.Object), Times.Never);

            conn.Verify(m => m.Disconnect("Unknown transfer", It.IsAny<Exception>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Adds child connection on ConnectToPeerResponse 'D'"), AutoData]
        public void Adds_Child_Connection_On_ConnectToPeerResponse_D(string username, int token, IPAddress ip, int port)
        {
            ConnectToPeerResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.DistributedConnectionManager
                .Setup(m => m.GetOrAddChildConnectionAsync(It.IsAny<ConnectToPeerResponse>()))
                .Callback<ConnectToPeerResponse>(r => result = r);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("D")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .WriteByte(0)
                .Build();

            handler.HandleMessageRead(null, msg);

            Assert.Equal(username, result.Username);
            Assert.Equal(token, result.Token);
            Assert.Equal(port, result.Port);
            Assert.Equal(ip, result.IPAddress);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Raises DiagnosticGenerated on Exception")]
        public void Raises_DiagnosticGenerated_On_Exception()
        {
            var mocks = new Mocks();

            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList)
                .Build();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);
            handler.HandleMessageRead(null, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Warning)
                .Where(d => d.Message.IndexOf("Error handling server message", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerAddUser"), AutoData]
        public void Handles_ServerAddUser(string username, bool exists, UserData userData)
        {
            WatchUserResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<WatchUserResponse>()))
                .Callback<WaitKey, WatchUserResponse>((key, response) => result = response);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)userData.Status)
                .WriteInteger(userData.AverageSpeed)
                .WriteLong(userData.UploadCount)
                .WriteInteger(userData.FileCount)
                .WriteInteger(userData.DirectoryCount)
                .WriteString(userData.CountryCode)
                .Build();

            handler.HandleMessageRead(null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(exists, result.Exists);
            Assert.Equal(userData.Status, result.UserData.Status);
            Assert.Equal(userData.AverageSpeed, result.UserData.AverageSpeed);
            Assert.Equal(userData.UploadCount, result.UserData.UploadCount);
            Assert.Equal(userData.FileCount, result.UserData.FileCount);
            Assert.Equal(userData.DirectoryCount, result.UserData.DirectoryCount);
            Assert.Equal(userData.CountryCode, result.UserData.CountryCode);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles Server.GetStatus"), AutoData]
        public void Handles_ServerGetStatus(string username, UserPresence status, bool privileged)
        {
            UserStatus result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<UserStatus>()))
                .Callback<WaitKey, UserStatus>((key, response) => result = response);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString(username)
                .WriteInteger((int)status)
                .WriteByte((byte)(privileged ? 1 : 0))
                .Build();

            handler.HandleMessageRead(null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(status, result.Presence);
            Assert.Equal(privileged, result.IsPrivileged);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles GetUserStats"), AutoData]
        public void Handles_GetUserStats(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            UserStatistics result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<UserStatistics>()))
                .Callback<WaitKey, UserStatistics>((key, response) => result = response);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString(username)
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .Build();

            handler.HandleMessageRead(null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(averageSpeed, result.AverageSpeed);
            Assert.Equal(uploadCount, result.UploadCount);
            Assert.Equal(fileCount, result.FileCount);
            Assert.Equal(directoryCount, result.DirectoryCount);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles NetInfo"), AutoData]
        public void Handles_NetInfo(List<(string Username, IPEndPoint IPEndPoint)> parents)
        {
            IEnumerable<(string Username, IPEndPoint IPEndPoint)> result = null;
            var (handler, mocks) = GetFixture();

            mocks.DistributedConnectionManager
                .Setup(m => m.AddParentConnectionAsync(It.IsAny<IEnumerable<(string Username, IPEndPoint IPEndPoint)>>()))
                .Callback<IEnumerable<(string Username, IPEndPoint IPEndPoint)>>(list => result = list);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(parents.Count);

            foreach (var parent in parents)
            {
                builder.WriteString(parent.Username);

                var ipBytes = parent.IPEndPoint.Address.GetAddressBytes();
                Array.Reverse(ipBytes);

                builder.WriteBytes(ipBytes);
                builder.WriteInteger(parent.IPEndPoint.Port);
            }

            var message = builder.Build();

            handler.HandleMessageRead(null, message);

            Assert.Equal(parents, result);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Produces diagnostic on NetInfo Exception"), AutoData]
        public void Produces_Diagnostic_On_NetInfo_Exception(List<(string Username, IPEndPoint IPEndPoint)> parents)
        {
            var (handler, mocks) = GetFixture();

            mocks.DistributedConnectionManager
                .Setup(m => m.AddParentConnectionAsync(It.IsAny<IEnumerable<(string Username, IPEndPoint IPEndPoint)>>()))
                .Throws(new Exception("foo"));

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(parents.Count);

            foreach (var parent in parents)
            {
                builder.WriteString(parent.Username);

                var ipBytes = parent.IPEndPoint.Address.GetAddressBytes();
                Array.Reverse(ipBytes);

                builder.WriteBytes(ipBytes);
                builder.WriteInteger(parent.IPEndPoint.Port);
            }

            var message = builder.Build();

            handler.HandleMessageRead(null, message);

            mocks.Diagnostic.Verify(m => m.Debug("Error handling NetInfo message: foo"));
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises UserStatusChanged on ServerGetStatus"), AutoData]
        public void Raises_UserStatusChanged_On_ServerGetStatus(string username, UserPresence status, bool privileged)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString(username)
                .WriteInteger((int)status)
                .WriteByte((byte)(privileged ? 1 : 0))
                .Build();

            UserStatus eventArgs = null;

            handler.UserStatusChanged += (sender, args) => eventArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(username, eventArgs.Username);
            Assert.Equal(status, eventArgs.Presence);
            Assert.Equal(privileged, eventArgs.IsPrivileged);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises UserStatsChanged on GetUserStats"), AutoData]
        public void Raises_UserStatsChanged_On_GetUserStats(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString(username)
                .WriteInteger(averageSpeed)
                .WriteLong(uploadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .Build();

            UserStatistics eventArgs = null;

            handler.UserStatisticsChanged += (sender, args) => eventArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(username, eventArgs.Username);
            Assert.Equal(averageSpeed, eventArgs.AverageSpeed);
            Assert.Equal(uploadCount, eventArgs.UploadCount);
            Assert.Equal(fileCount, eventArgs.FileCount);
            Assert.Equal(directoryCount, eventArgs.DirectoryCount);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles JoinRoom"), AutoData]
        public void Handles_JoinRoom(string roomName)
        {
            var (handler, mocks) = GetFixture();

            RoomData response = default;

            var key = new WaitKey(MessageCode.Server.JoinRoom, roomName);
            mocks.Waiter.Setup(m => m.Complete(It.Is<WaitKey>(k => k.Equals(key)), It.IsAny<RoomData>()))
                .Callback<WaitKey, RoomData>((k, r) => response = r);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.JoinRoom)
                .WriteString(roomName)
                .WriteInteger(0) // user count
                .WriteInteger(0) // status count
                .WriteInteger(0) // data count
                .WriteInteger(0) // slots free count
                .WriteInteger(0); // country count

            var message = builder.Build();

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, response.Name);
            Assert.Equal(0, response.UserCount);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles LeaveRoom"), AutoData]
        public void Handles_LeaveRoom(string roomName)
        {
            var (handler, mocks) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.LeaveRoom)
                .WriteString(roomName);

            var message = builder.Build();

            handler.HandleMessageRead(null, message);

            var key = new WaitKey(MessageCode.Server.LeaveRoom, roomName);
            mocks.Waiter.Verify(m => m.Complete(It.Is<WaitKey>(k => k.Equals(key))), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises RoomLeft on LeaveRoom"), AutoData]
        public void Raises_RoomLeft_On_LeaveRoom(string roomName, string username)
        {
            var (handler, mocks) = GetFixture();

            mocks.Client.Setup(m => m.Username).Returns(username);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.LeaveRoom)
                .WriteString(roomName);

            var message = builder.Build();
            RoomLeftEventArgs args = null;

            handler.RoomLeft += (sender, a) => args = a;
            handler.HandleMessageRead(null, message);

            Assert.NotNull(args);
            Assert.Equal(roomName, args.RoomName);
            Assert.Equal(username, args.Username);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles SayInChatRoom"), AutoData]
        public void Handles_SayInChatRoom(string roomName, string username, string msg)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.SayInChatRoom)
                .WriteString(roomName)
                .WriteString(username)
                .WriteString(msg);

            var message = builder.Build();

            RoomMessageReceivedEventArgs actual = default;
            handler.RoomMessageReceived += (sender, args) => actual = args;
            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, actual.RoomName);
            Assert.Equal(username, actual.Username);
            Assert.Equal(msg, actual.Message);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on SayInChatRoom when RoomMessageReceived is unbound"), AutoData]
        public void Does_Not_Throw_On_SayInChatRoom_When_RoomMessageReceived_Is_Unbound(string roomName, string username, string msg)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.SayInChatRoom)
                .WriteString(roomName)
                .WriteString(username)
                .WriteString(msg);

            var message = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles PublicChat"), AutoData]
        public void Handles_PublicChatMessage(string roomName, string username, string msg)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PublicChat)
                .WriteString(roomName)
                .WriteString(username)
                .WriteString(msg);

            var message = builder.Build();

            PublicChatMessageReceivedEventArgs actual = default;
            handler.PublicChatMessageReceived += (sender, args) => actual = args;
            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, actual.RoomName);
            Assert.Equal(username, actual.Username);
            Assert.Equal(msg, actual.Message);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PublicChat if PublicChatMessageReceived is not bound"), AutoData]
        public void Does_Not_Throw_On_PublicChat_If_PublicChatMessageReceived_Is_Not_Bound(string roomName, string username, string msg)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PublicChat)
                .WriteString(roomName)
                .WriteString(username)
                .WriteString(msg);

            var message = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles UserJoinedRoom"), AutoData]
        public void Handles_UserJoinedRoom(string roomName, string username, UserData data)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString(roomName)
                .WriteString(username)
                .WriteInteger((int)data.Status)
                .WriteInteger(data.AverageSpeed)
                .WriteLong(data.UploadCount)
                .WriteInteger(data.FileCount)
                .WriteInteger(data.DirectoryCount)
                .WriteInteger(data.SlotsFree.Value)
                .WriteString(data.CountryCode);

            var message = builder.Build();

            RoomJoinedEventArgs actual = default;
            handler.RoomJoined += (sender, args) => actual = args;
            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, actual.RoomName);
            Assert.Equal(username, actual.Username);
            Assert.Equal(data.Status, actual.UserData.Status);
            Assert.Equal(data.AverageSpeed, actual.UserData.AverageSpeed);
            Assert.Equal(data.UploadCount, actual.UserData.UploadCount);
            Assert.Equal(data.FileCount, actual.UserData.FileCount);
            Assert.Equal(data.DirectoryCount, actual.UserData.DirectoryCount);
            Assert.Equal(data.CountryCode, actual.UserData.CountryCode);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on UserJoinedRoom when RoomJoined is unbound"), AutoData]
        public void Does_Not_Throw_On_UserJoinedRoom_When_RoomJoined_Is_Unbound(string roomName, string username, UserData data)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString(roomName)
                .WriteString(username)
                .WriteInteger((int)data.Status)
                .WriteInteger(data.AverageSpeed)
                .WriteLong(data.UploadCount)
                .WriteInteger(data.FileCount)
                .WriteInteger(data.DirectoryCount)
                .WriteInteger(data.SlotsFree.Value)
                .WriteString(data.CountryCode);

            var message = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles UserLeftRoom"), AutoData]
        public void Handles_UserLeftRoom(string roomName, string username)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserLeftRoom)
                .WriteString(roomName)
                .WriteString(username);

            var message = builder.Build();

            RoomLeftEventArgs actual = default;
            handler.RoomLeft += (sender, args) => actual = args;
            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, actual.RoomName);
            Assert.Equal(username, actual.Username);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on UserLeftRoom when RoomLeft is unbound"), AutoData]
        public void Does_Not_Throw_On_UserLeftRoom_When_RoomLeft_Is_Unbound(string roomName, string username)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserLeftRoom)
                .WriteString(roomName)
                .WriteString(username);

            var message = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Raises KickedFromServer on KickedFromServer")]
        public void Raises_KickedFromServer_On_KickedFromServer()
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.KickedFromServer)
                .Build();

            EventArgs eventArgs = null;

            handler.KickedFromServer += (sender, args) => eventArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.NotNull(eventArgs);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Does not throw on KickedFromServer when KickedFromServer is unbound")]
        public void Does_Not_Throw_On_KickedFromServer_When_KickedFromServer_Is_Unbound()
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.KickedFromServer)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivilegeNotificationReceived on AddPrivilegedUser"), AutoData]
        public void Raises_PrivilegeNotificationReceived_On_AddPrivilegedUser(string username)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddPrivilegedUser)
                .WriteString(username)
                .Build();

            PrivilegeNotificationReceivedEventArgs eventArgs = null;

            handler.PrivilegeNotificationReceived += (sender, args) => eventArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.NotNull(eventArgs);
            Assert.Equal(username, eventArgs.Username);
            Assert.Null(eventArgs.Id);
            Assert.False(eventArgs.RequiresAcknowlegement);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on AddPrivilegedUser when PrivilegeNotificationReceived is unbound"), AutoData]
        public void Does_Not_Throw_On_AddPrivilegedUser_When_PrivilegeNotificationReceived_Is_Unbound(string username)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddPrivilegedUser)
                .WriteString(username)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivilegeNotificationReceived on NotifyPrivileges"), AutoData]
        public void Raises_PrivilegeNotificationReceived_On_NotifyPrivileges(string username, int id)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.NotifyPrivileges)
                .WriteInteger(id)
                .WriteString(username)
                .Build();

            PrivilegeNotificationReceivedEventArgs eventArgs = null;

            handler.PrivilegeNotificationReceived += (sender, args) => eventArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.NotNull(eventArgs);
            Assert.Equal(username, eventArgs.Username);
            Assert.Equal(id, eventArgs.Id);
            Assert.True(eventArgs.RequiresAcknowlegement);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Raises DistributedNetworkReset on DistributedReset")]
        public void Raises_DistributedNetworkReset_On_DistributedReset()
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.DistributedReset)
                .Build();

            bool fired = false;

            handler.DistributedNetworkReset += (sender, args) => fired = true;

            handler.HandleMessageRead(null, message);

            Assert.True(fired);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Raises DistributedNetworkReset on DistributedReset")]
        public void Raises_Resets_Distributed_Network_On_DistributedReset()
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.DistributedReset)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.DistributedConnectionManager.Verify(m => m.ResetStatus(), Times.Once);
            mocks.DistributedConnectionManager.Verify(m => m.RemoveAndDisposeAll(), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Acknowledges NotifyPrivileges when AutoAcknowledgePrivilegeNotifications is true"), AutoData]
        public void Acknowledges_NotifyPrivileges_When_AutoAcknowledgePrivilegeNotifications_Is_True(string username, int id)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.NotifyPrivileges)
                .WriteInteger(id)
                .WriteString(username)
                .Build();

            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(autoAcknowledgePrivilegeNotifications: true));

            handler.HandleMessageRead(null, message);

            mocks.Client.Verify(m => m.AcknowledgePrivilegeNotificationAsync(id, It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not acknowledge NotifyPrivileges when AutoAcknowledgePrivilegeNotifications is false"), AutoData]
        public void Does_Not_Acknowledge_NotifyPrivileges_When_AutoAcknowledgePrivilegeNotifications_Is_False(string username, int id)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.NotifyPrivileges)
                .WriteInteger(id)
                .WriteString(username)
                .Build();

            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(autoAcknowledgePrivilegeNotifications: false));

            handler.HandleMessageRead(null, message);

            mocks.Client.Verify(m => m.AcknowledgePrivilegeNotificationAsync(id, It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles UserPrivileges"), AutoData]
        public void Handles_UserPrivileges(string username, bool privileged)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserPrivileges)
                .WriteString(username)
                .WriteByte((byte)(privileged ? 1 : 0))
                .Build();

            mocks.Client.Setup(m => m.Options)
                .Returns(new SoulseekClientOptions(autoAcknowledgePrivilegeNotifications: false));

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete<bool>(new WaitKey(MessageCode.Server.UserPrivileges, username), privileged), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles NewPassword"), AutoData]
        public void Handles_NewPassword(string password)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.NewPassword)
                .WriteString(password)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete<string>(new WaitKey(MessageCode.Server.NewPassword), password), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles GlobalAdminMessage"), AutoData]
        public void Handles_GlobalAdminMessage(string msg)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GlobalAdminMessage)
                .WriteString(msg)
                .Build();

            string args = default;
            handler.GlobalMessageReceived += (sender, e) => args = e;
            handler.HandleMessageRead(null, message);

            Assert.NotNull(args);
            Assert.Equal(msg, args);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on GlobalAdminMessage when GlobalAdminMessage is unbound"), AutoData]
        public void Does_Not_Throw_On_GlobalAdminMessage_When_GlobalAdminMessage_Is_Unbound(string msg)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GlobalAdminMessage)
                .WriteString(msg)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Handles Ping")]
        public void Handles_Ping()
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.Ping)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.Ping)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles CannotJoinRoom"), AutoData]
        public void Handles_CannotJoinRoom(string roomName)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotJoinRoom)
                .WriteString(roomName)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Server.JoinRoom, roomName), It.IsAny<RoomJoinForbiddenException>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateRoomMembershipAdded on PrivateRoomAdded"), AutoData]
        public void Raises_PrivateRoomMembershipAdded_On_PrivateRoomAdded(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAdded)
                .WriteString(roomName)
                .Build();

            string room = default;

            handler.PrivateRoomMembershipAdded += (sender, args) => room = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, room);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PrivateRoomAdded when PrivateRoomMembershipAdded is unbound"), AutoData]
        public void Does_Not_Throw_On_PrivateRoomAdded_When_PrivateRoomMembershipAdded_Is_Unbound(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAdded)
                .WriteString(roomName)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateRoomModeratorAdded on PrivateRoomOperatorAdded"), AutoData]
        public void Raises_PrivateRoomModerationAdded_On_PrivateRoomOperatorAdded(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOperatorAdded)
                .WriteString(roomName)
                .Build();

            string room = default;

            handler.PrivateRoomModerationAdded += (sender, args) => room = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, room);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PrivateRoomOperatorAdded when PrivateRoomModerationAdded is unbound"), AutoData]
        public void Does_Not_Throw_On_PrivateRoomOperatorAdded_When_PrivateRoomModerationAdded_Is_Unbound(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOperatorAdded)
                .WriteString(roomName)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateRoomMembershipRemoved on PrivateRoomRemoved"), AutoData]
        public void Raises_PrivateRoomMembershipRemoved_On_PrivateRoomRemoved(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoved)
                .WriteString(roomName)
                .Build();

            string room = default;

            handler.PrivateRoomMembershipRemoved += (sender, args) => room = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, room);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PrivateRoomRemoved when PrivateRoomMembershipRemoved is unbound"), AutoData]
        public void Does_Not_Throw_On_PrivateRoomRemoved_When_PrivateRoomMembershipRemoved_Is_Unbound(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoved)
                .WriteString(roomName)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait on PrivateRoomRemoved"), AutoData]
        public void Completes_Wait_On_PrivateRoomRemoved(string roomName)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoved)
                .WriteString(roomName)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.PrivateRoomRemoved, roomName)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait on PrivateRoomToggle"), AutoData]
        public void Completes_Wait_On_PrivateRoomToggle(bool acceptInvitations)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomToggle)
                .WriteByte((byte)(acceptInvitations ? 1 : 0))
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.PrivateRoomToggle), acceptInvitations), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateRoomModerationRemoved on PrivateRoomOperatorRemoved"), AutoData]
        public void Raises_PrivateRoomModerationRemoved_On_PrivateRoomOperatorRemoved(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOperatorRemoved)
                .WriteString(roomName)
                .Build();

            string room = default;

            handler.PrivateRoomModerationRemoved += (sender, args) => room = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, room);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PrivateRoomOperatorRemoved when PrivateRoomModerationRemoved is unbound"), AutoData]
        public void Does_Not_Throw_On_PrivateRoomOperatorRemoved_When_PrivateRoomModerationRemoved_Is_Unbound(string roomName)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOperatorRemoved)
                .WriteString(roomName)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait on PrivateRoomOperatorRemoved"), AutoData]
        public void Completes_Wait_On_PrivateRoomOperatorRemoved(string roomName)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOperatorRemoved)
                .WriteString(roomName)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.PrivateRoomOperatorRemoved, roomName)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateRoomUserListReceived on PrivateRoomUsers"), AutoData]
        public void Raises_PrivateRoomUserListReceived_On_PrivateRoomUsers(string roomName, List<string> users)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomUsers)
                .WriteString(roomName)
                .WriteInteger(users.Count);

            users.ForEach(user => builder.WriteString(user));

            var message = builder.Build();

            RoomInfo room = default;

            handler.PrivateRoomUserListReceived += (sender, args) => room = args;

            handler.HandleMessageRead(null, message);

            Assert.NotNull(room);
            Assert.Equal(roomName, room.Name);
            Assert.Equal(users.Count, room.UserCount);

            foreach (var expected in users)
            {
                Assert.Contains(room.Users, actualUser => actualUser == expected);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PrivateRoomUsers when PrivateRoomUserListReceived is unbound"), AutoData]
        public void Does_Not_Throw_On_PrivateRoomUsers_When_PrivateRoomUserListReceived(string roomName, List<string> users)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomUsers)
                .WriteString(roomName)
                .WriteInteger(users.Count);

            users.ForEach(user => builder.WriteString(user));

            var message = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateRoomModeratorListReceived on PrivateRoomOwned"), AutoData]
        public void Raises_PrivateRoomModeratedUserListReceived_On_PrivateRoomOwned(string roomName, List<string> users)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOwned)
                .WriteString(roomName)
                .WriteInteger(users.Count);

            users.ForEach(user => builder.WriteString(user));

            var message = builder.Build();

            RoomInfo room = default;

            handler.PrivateRoomModeratedUserListReceived += (sender, args) => room = args;

            handler.HandleMessageRead(null, message);

            Assert.NotNull(room);
            Assert.Equal(roomName, room.Name);
            Assert.Equal(users.Count, room.UserCount);

            foreach (var expected in users)
            {
                Assert.Contains(room.Users, actualUser => actualUser == expected);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on PrivateRoomOwned when PrivateRoomModeratedUserListReceived is unbound"), AutoData]
        public void Does_Not_Throw_On_PrivateRoomOwned_When_PrivateRoomModeratedUserListRecieved_Is_Unbound(string roomName, List<string> users)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOwned)
                .WriteString(roomName)
                .WriteInteger(users.Count);

            users.ForEach(user => builder.WriteString(user));

            var message = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait on PrivateRoomAddUser"), AutoData]
        public void Completes_Wait_On_PrivateRoomAddUser(string roomName, string username)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddUser)
                .WriteString(roomName)
                .WriteString(username)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.PrivateRoomAddUser, roomName, username)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait on PrivateRoomRemoveUser"), AutoData]
        public void Completes_Wait_On_PrivateRoomRemoveUser(string roomName, string username)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveUser)
                .WriteString(roomName)
                .WriteString(username)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.PrivateRoomRemoveUser, roomName, username)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait on PrivateRoomAddOperator"), AutoData]
        public void Completes_Wait_On_PrivateRoomAddOperator(string roomName, string username)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddOperator)
                .WriteString(roomName)
                .WriteString(username)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.PrivateRoomAddOperator, roomName, username)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait on PrivateRoomRemoveOperator"), AutoData]
        public void Completes_Wait_On_PrivateRoomRemoveOperator(string roomName, string username)
        {
            var (handler, mocks) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveOperator)
                .WriteString(roomName)
                .WriteString(username)
                .Build();

            handler.HandleMessageRead(null, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Server.PrivateRoomRemoveOperator, roomName, username)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Forwards embedded messages")]
        public void Forwards_Embedded_Messages()
        {
            var (handler, mocks) = GetFixture();

            var distributedHandler = new Mock<IDistributedMessageHandler>();

            mocks.Client.Setup(m => m.DistributedMessageHandler)
                .Returns(distributedHandler.Object);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.EmbeddedMessage) // 93, same as distributed code
                .WriteByte(0x03)
                .WriteBytes(new byte[8])
                .WriteString("username")
                .WriteInteger(1)
                .WriteString("query")
                .Build();

            handler.HandleMessageRead(null, message);

            distributedHandler.Verify(m => m.HandleEmbeddedMessage(message), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Responds to SearchRequest"), AutoData]
        public void Responds_To_SearchRequest(string username, int token, string query)
        {
            var (handler, mocks) = GetFixture();

            var conn = new Mock<IMessageConnection>();

            var message = GetServerSearchRequest(username, token, query);

            handler.HandleMessageRead(conn.Object, message);

            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest if result is null"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_If_Result_Is_Null(string username, int token, string query)
        {
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult<SearchResponse>(null));
            var (handler, mocks) = GetFixture(options);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(new IPEndPoint(IPAddress.None, 0)));

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = GetServerSearchRequest(username, token, query);

            handler.HandleMessageRead(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()), Times.Never);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()), Times.Never);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), null), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest if result contains no files"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_If_Result_Contains_No_Files(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>());
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            var endpoint = new IPEndPoint(IPAddress.None, 0);

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));

            var peerConn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(peerConn.Object));

            var conn = new Mock<IMessageConnection>();

            var message = GetServerSearchRequest(username, token, query);

            handler.HandleMessageRead(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()), Times.Never);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()), Times.Never);

            // cheap hack here to compare the contents of the resulting byte arrays, since they are distinct arrays but contain the same bytes
            peerConn.Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), null), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest if it came from the local user"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_If_It_Came_From_The_Local_User(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>());
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            var conn = new Mock<IMessageConnection>();

            mocks.Client.Setup(m => m.Username)
                .Returns(username);

            // no active searches in dictionary, so this should NOT be treated as intentional
            var searches = new ConcurrentDictionary<int, SearchInternal>();
            mocks.Client.Setup(m => m.Searches).Returns(searches);

            var message = GetServerSearchRequest(username, token, query);
            var endpoint = new IPEndPoint(IPAddress.None, 0);

            handler.HandleMessageRead(conn.Object, message);

            mocks.Client.Verify(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()), Times.Never);
            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(username, endpoint, It.IsAny<CancellationToken>()), Times.Never);

            // verify that SearchResponder is NOT called since this is not an intentional self-search
            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Responds to SearchRequest if it came from the local user, and the search was intentionally sent"), AutoData]
        public void Responds_To_SearchRequest_If_It_Came_From_The_Local_User_And_It_Was_Intentionally_Sent(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>() { new File(1, "test.mp3", 123456, ".mp3") });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            var conn = new Mock<IMessageConnection>();

            mocks.Client.Setup(m => m.Username)
                .Returns(username);

            // create a search with User scope that includes the local username in subjects
            var searchQuery = new SearchQuery(query);
            var searchScope = SearchScope.User(username); // The local user is searching themselves
            var searchInternal = new SearchInternal(searchQuery, searchScope, token);

            // add the search to the active searches dictionary
            var searches = new ConcurrentDictionary<int, SearchInternal>();
            searches.TryAdd(token, searchInternal);

            mocks.Client.Setup(m => m.Searches).Returns(searches);

            var message = GetServerSearchRequest(username, token, query);

            handler.HandleMessageRead(conn.Object, message);

            // Verify that the search responder was called since this is an intentional self-search
            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest from local user if search scope is not User"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_From_Local_User_If_Search_Scope_Is_Not_User(string username, int token, string query)
        {
            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>() { new File(1, "test.mp3", 123456, ".mp3") });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            var conn = new Mock<IMessageConnection>();

            mocks.Client.Setup(m => m.Username)
                .Returns(username);

            // create a search with Network scope (not User scope), so it shouldn't be treated as intentional
            var searchQuery = new SearchQuery(query);
            var searchScope = SearchScope.Network;
            var searchInternal = new SearchInternal(searchQuery, searchScope, token);

            // add the search to the active searches dictionary
            var searches = new ConcurrentDictionary<int, SearchInternal>();
            searches.TryAdd(token, searchInternal);

            mocks.Client.Setup(m => m.Searches).Returns(searches);

            var message = GetServerSearchRequest(username, token, query);

            handler.HandleMessageRead(conn.Object, message);

            // verify that SearchResponder is NOT called since scope is not User
            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Doesn't respond to SearchRequest from local user if username not in search subjects"), AutoData]
        public void Doesnt_Respond_To_SearchRequest_From_Local_User_If_Username_Not_In_Search_Subjects(string username, string otherUsername, int token, string query)
        {
            var response = new SearchResponse("foo", token, false, 1, 1, new List<File>() { new File(1, "test.mp3", 123456, ".mp3") });
            var options = new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response));
            var (handler, mocks) = GetFixture(options);

            var conn = new Mock<IMessageConnection>();

            mocks.Client.Setup(m => m.Username)
                .Returns(username);

            // create a search with User scope but different username in subjects
            var searchQuery = new SearchQuery(query);
            var searchScope = SearchScope.User(otherUsername); // Different username, not the local user
            var searchInternal = new SearchInternal(searchQuery, searchScope, token);

            // add the search to the active searches dictionary
            var searches = new ConcurrentDictionary<int, SearchInternal>();
            searches.TryAdd(token, searchInternal);

            mocks.Client.Setup(m => m.Searches).Returns(searches);

            var message = GetServerSearchRequest(username, token, query);

            handler.HandleMessageRead(conn.Object, message);

            // verify that SearchResponder is NOT called since local username is not in subjects
            mocks.SearchResponder.Verify(m => m.TryRespondAsync(username, token, query), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw when handling SearchRequest if SearchResponseResolver is null"), AutoData]
        public void Does_Not_Throw_When_Handling_SearchRequest_If_SearchResponseResolver_Is_Null(string username, int token, string query)
        {
            var options = new SoulseekClientOptions(searchResponseResolver: null);
            var (handler, _) = GetFixture(options);

            var conn = new Mock<IMessageConnection>();

            var message = GetServerSearchRequest(username, token, query);

            var ex = Record.Exception(() => handler.HandleMessageRead(conn.Object, message));

            Assert.Null(ex);
        }

        [Trait("Category", "HandleMessageWritten")]
        [Fact(DisplayName = "Creates diagnostic on message written")]
        public void Creates_Diagnostic_On_Message_Written()
        {
            var (handler, mocks) = GetFixture();

            var message = GetServerSearchRequest("test", 0, "doesn't matter");

            handler.HandleMessageWritten(new Mock<IMessageConnection>().Object, new MessageEventArgs(message));

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Server message sent: FileSearch"))), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises RoomTickerListReceived on RoomTickers"), AutoData]
        public void Raises_RoomTickerListRecieved_On_RoomTickers(string roomName, List<RoomTicker> tickers)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickers)
                .WriteString(roomName)
                .WriteInteger(tickers.Count);

            tickers.ForEach(ticker =>
            {
                builder
                    .WriteString(ticker.Username)
                    .WriteString(ticker.Message);
            });

            var message = builder.Build();

            RoomTickerListReceivedEventArgs actualArgs = default;

            handler.RoomTickerListReceived += (sender, args) => actualArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, actualArgs.RoomName);
            Assert.Equal(tickers.Count, actualArgs.TickerCount);

            foreach (var ticker in tickers)
            {
                Assert.Contains(actualArgs.Tickers, t => ticker.Username == t.Username && ticker.Message == t.Message);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on RoomTickers when RoomTickerListReceived is unbound"), AutoData]
        public void Does_Not_Throw_On_RoomTickers_When_RoomTickerListReceived_Is_Unbound(string roomName, List<RoomTicker> tickers)
        {
            var (handler, _) = GetFixture();

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickers)
                .WriteString(roomName)
                .WriteInteger(tickers.Count);

            tickers.ForEach(ticker =>
            {
                builder
                    .WriteString(ticker.Username)
                    .WriteString(ticker.Message);
            });

            var message = builder.Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises RoomTickerAdded on RoomTickerAdd"), AutoData]
        public void Raises_RoomTickerAdded_On_RoomTickerAdd(string roomName, string username, string msg)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerAdd)
                .WriteString(roomName)
                .WriteString(username)
                .WriteString(msg)
                .Build();

            RoomTickerAddedEventArgs actualArgs = default;

            handler.RoomTickerAdded += (sender, args) => actualArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, actualArgs.RoomName);
            Assert.Equal(username, actualArgs.Ticker.Username);
            Assert.Equal(msg, actualArgs.Ticker.Message);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on RoomTickerAdd when RoomTickerAdded is unbound"), AutoData]
        public void Does_Not_Throw_On_RoomTickerAdd_When_RoomTickerAdded_Is_Unbound(string roomName, string username, string msg)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerAdd)
                .WriteString(roomName)
                .WriteString(username)
                .WriteString(msg)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises RoomTickerRemoved on RoomTickerRemove"), AutoData]
        public void Raises_RoomTickerRemoved_On_RoomTickerRemove(string roomName, string username)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerRemove)
                .WriteString(roomName)
                .WriteString(username)
                .Build();

            RoomTickerRemovedEventArgs actualArgs = default;

            handler.RoomTickerRemoved += (sender, args) => actualArgs = args;

            handler.HandleMessageRead(null, message);

            Assert.Equal(roomName, actualArgs.RoomName);
            Assert.Equal(username, actualArgs.Username);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not throw on RoomTickerRemove when RoomTickerRemoved is unbound"), AutoData]
        public void Does_Not_Throw_On_RoomTickerRemove_When_RoomTickerRemoved_Is_Unbound(string roomName, string username)
        {
            var (handler, _) = GetFixture();

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerRemove)
                .WriteString(roomName)
                .WriteString(username)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(null, message));

            Assert.Null(ex);
        }

        private static byte[] GetServerSearchRequest(string username, int token, string query)
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.FileSearch)
                .WriteString(username)
                .WriteInteger(token)
                .WriteString(query)
                .Build();
        }

        private static (ServerMessageHandler Handler, Mocks Mocks) GetFixture(SoulseekClientOptions clientOptions = null)
        {
            var mocks = new Mocks(clientOptions);

            var handler = new ServerMessageHandler(
                mocks.Client.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private class Mocks
        {
            public Mocks(SoulseekClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(9999, clientOptions)
                {
                    CallBase = true,
                };

                Client.Setup(m => m.ServerConnection).Returns(ServerConnection.Object);
                Client.Setup(m => m.PeerConnectionManager).Returns(PeerConnectionManager.Object);
                Client.Setup(m => m.DistributedConnectionManager).Returns(DistributedConnectionManager.Object);
                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.DownloadDictionary).Returns(Downloads);
                Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                Client.Setup(m => m.Options).Returns(clientOptions ?? new SoulseekClientOptions());
                Client.Setup(m => m.SearchResponder).Returns(SearchResponder.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
            public Mock<IDistributedConnectionManager> DistributedConnectionManager { get; } = new Mock<IDistributedConnectionManager>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public ConcurrentDictionary<int, TransferInternal> Downloads { get; } = new ConcurrentDictionary<int, TransferInternal>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<ISearchResponder> SearchResponder { get; } = new Mock<ISearchResponder>();
        }
    }
}
