// <copyright file="OutgoingTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using System.Net;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class OutgoingTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Request", "PrivateMessageCommand")]
        [Theory(DisplayName = "PrivateMessageCommand instantiates properly"), AutoData]
        public void PrivateMessageCommand_Instantiates_Properly(string message, string username)
        {
            var msg = new PrivateMessageCommand(username, message);

            Assert.Equal(message, msg.Message);
            Assert.Equal(username, msg.Username);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PrivateMessageCommand")]
        [Theory(DisplayName = "PrivateMessageCommand constructs the correct message"), AutoData]
        public void PrivateMessageCommand_Constructs_The_Correct_Message(string message, string username)
        {
            var msg = new PrivateMessageCommand(username, message).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.PrivateMessage, code);

            Assert.Equal(4 + 4 + 4 + username.Length + 4 + message.Length, msg.Length);
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(message, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "ParentsIPCommand")]
        [Theory(DisplayName = "ParentsIPCommand instantiates properly"), AutoData]
        public void ParentsIPCommand_Instantiates_Properly(IPAddress ipAddress)
        {
            var msg = new ParentsIPCommand(ipAddress);

            Assert.Equal(ipAddress, msg.IPAddress);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "ParentsIPCommand")]
        [Theory(DisplayName = "ParentsIPCommand constructs the correct message"), AutoData]
        public void ParentsIPCommand_Constructs_The_Correct_Message(IPAddress ipAddress)
        {
            var msg = new ParentsIPCommand(ipAddress).ToByteArray();
            var expected = ipAddress.GetAddressBytes();
            Array.Reverse(expected);

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.ParentsIP, code);

            Assert.Equal(4 + 4 + 4, msg.Length);
            Assert.Equal(expected, reader.ReadBytes(4));
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "ChildDepthCommand")]
        [Theory(DisplayName = "ChildDepthCommand instantiates properly"), AutoData]
        public void ChildDepthCommand_Instantiates_Properly(int depth)
        {
            var msg = new ChildDepthCommand(depth);

            Assert.Equal(depth, msg.Depth);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "ChildDepthCommand")]
        [Theory(DisplayName = "ChildDepthCommand constructs the correct message"), AutoData]
        public void ChildDepthCommand_Constructs_The_Correct_Message(int depth)
        {
            var msg = new ChildDepthCommand(depth).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.ChildDepth, code);

            Assert.Equal(4 + 4 + 4, msg.Length);
            Assert.Equal(depth, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "AcknowledgePrivateMessage")]
        [Fact(DisplayName = "AcknowledgePrivateMessage instantiates properly")]
        public void AcknowledgePrivateMessage_Instantiates_Properly()
        {
            var num = new Random().Next();
            var a = new AcknowledgePrivateMessageCommand(num);

            Assert.Equal(num, a.Id);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "AcknowledgePrivateMessage")]
        [Fact(DisplayName = "AcknowledgePrivateMessage constructs the correct Message")]
        public void AcknowledgePrivateMessage_Constructs_The_Correct_Message()
        {
            var num = new Random().Next();
            var msg = new AcknowledgePrivateMessageCommand(num).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.AcknowledgePrivateMessage, code);

            // length + code + token
            Assert.Equal(4 + 4 + 4, msg.Length);
            Assert.Equal(num, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "UserAddressRequest")]
        [Fact(DisplayName = "UserAddressRequest instantiates properly")]
        public void UserAddressRequest_Instantiates_Properly()
        {
            var name = Guid.NewGuid().ToString();
            var a = new UserAddressRequest(name);

            Assert.Equal(name, a.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserAddressRequest")]
        [Fact(DisplayName = "UserAddressRequest constructs the correct Message")]
        public void UserAddressRequest_Constructs_The_Correct_Message()
        {
            var name = Guid.NewGuid().ToString();
            var msg = new UserAddressRequest(name).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GetPeerAddress, code);

            // length + code + name length + name string
            Assert.Equal(4 + 4 + 4 + name.Length, msg.Length);
            Assert.Equal(name, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "LoginRequest")]
        [Fact(DisplayName = "LoginRequest instantiates properly")]
        public void LoginRequest_Instantiates_Properly()
        {
            var name = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();
            var a = new LoginRequest(minorVersion: 9999, name, password);

            Assert.Equal(name, a.Username);
            Assert.Equal(password, a.Password);
            Assert.NotEmpty(a.Hash);
            Assert.NotEqual(0, a.Version);
            Assert.NotEqual(0, a.MinorVersion);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "LoginRequest")]
        [Fact(DisplayName = "LoginRequest constructs the correct Message")]
        public void LoginRequest_Constructs_The_Correct_Message()
        {
            var name = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();
            var a = new LoginRequest(minorVersion: 9999, name, password);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.Login, code);
            Assert.Equal(name.Length + password.Length + a.Hash.Length + 28, msg.Length);
            Assert.Equal(name, reader.ReadString());
            Assert.Equal(password, reader.ReadString());
            Assert.Equal(a.Version, reader.ReadInteger());
            Assert.Equal(a.Hash, reader.ReadString());
            Assert.Equal(a.MinorVersion, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PeerBrowseRequest")]
        [Fact(DisplayName = "PeerBrowseRequest instantiates properly")]
        public void PeerBrowseRequest_Instantiates_Properly()
        {
            BrowseRequest a = null;

            var ex = Record.Exception(() => a = new BrowseRequest());

            Assert.Null(ex);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PeerBrowseRequest")]
        [Fact(DisplayName = "PeerBrowseRequest constructs the correct Message")]
        public void PeerBrowseRequest_Constructs_The_Correct_Message()
        {
            var msg = new BrowseRequest().ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.BrowseRequest, code);
            Assert.Equal(8, msg.Length);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SearchRequest")]
        [Theory(DisplayName = "SearchRequest instantiates properly"), AutoData]
        public void SearchRequest_Instantiates_Properly(string text, int token)
        {
            var a = new SearchRequest(text, token);

            Assert.Equal(text, a.SearchText);
            Assert.Equal(token, a.Token);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SearchRequest")]
        [Theory(DisplayName = "SearchRequest constructs the correct Message"), AutoData]
        public void SearchRequest_Constructs_The_Correct_Message(string text, int token)
        {
            var a = new SearchRequest(text, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.FileSearch, code);
            Assert.Equal(4 + 4 + 4 + 4 + text.Length, msg.Length);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(text, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "WishlistSearchRequest")]
        [Theory(DisplayName = "WishlistSearchRequest instantiates properly"), AutoData]
        public void WishlistSearchRequest_Instantiates_Properly(string text, int token)
        {
            var a = new WishlistSearchRequest(text, token);

            Assert.Equal(text, a.SearchText);
            Assert.Equal(token, a.Token);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "WishlistSearchRequest")]
        [Theory(DisplayName = "WishlistSearchRequest constructs the correct Message"), AutoData]
        public void WishlistSearchRequest_Constructs_The_Correct_Message(string text, int token)
        {
            var a = new WishlistSearchRequest(text, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.WishlistSearch, code);
            Assert.Equal(4 + 4 + 4 + 4 + text.Length, msg.Length);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(text, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PeerInfoRequest")]
        [Fact(DisplayName = "PeerInfoRequest constructs the correct Message")]
        public void PeerInfoRequest_Constructs_The_Correct_Message()
        {
            var a = new UserInfoRequest();
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.InfoRequest, code);
            Assert.Equal(8, msg.Length);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "AddUserRequest")]
        [Theory(DisplayName = "AddUserRequest instantiates properly"), AutoData]
        public void AddUserRequest_Instantiates_Properly(string username)
        {
            var a = new WatchUserRequest(username);

            Assert.Equal(username, a.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "WatchUserRequest")]
        [Theory(DisplayName = "WatchUserRequest constructs the correct message"), AutoData]
        public void WatchUserRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new WatchUserRequest(username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.WatchUser, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "UserStatusRequest")]
        [Theory(DisplayName = "UserStatusRequest instantiates properly"), AutoData]
        public void UserStatusRequest_Instantiates_Properly(string username)
        {
            var a = new UserStatusRequest(username);

            Assert.Equal(username, a.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserStatusRequest")]
        [Theory(DisplayName = "UserStatusRequest constructs the correct message"), AutoData]
        public void UserStatusRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new UserStatusRequest(username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GetStatus, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "UserStatsRequest")]
        [Theory(DisplayName = "UserStatsRequest instantiates properly"), AutoData]
        public void UserStatsRequest_Instantiates_Properly(string username)
        {
            var a = new UserStatisticsRequest(username);

            Assert.Equal(username, a.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserStatsRequest")]
        [Theory(DisplayName = "UserStatsRequest constructs the correct message"), AutoData]
        public void UserStatsRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new UserStatisticsRequest(username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GetUserStats, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetListenPort")]
        [Fact(DisplayName = "SetListenPort instantiates properly")]
        public void SetListenPort_Instantiates_Properly()
        {
            var port = new Random().Next(1024, 50000);
            var a = new SetListenPortCommand(port);

            Assert.Equal(port, a.Port);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetListenPort")]
        [Fact(DisplayName = "SetListenPort throws if port is less than 1024")]
        public void SetListenPort_Throws_If_Port_Is_Less_Than_1024()
        {
            var ex = Record.Exception(() => new SetListenPortCommand(0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetListenPort")]
        [Fact(DisplayName = "SetListenPort throws if port is less than 1024")]
        public void SetListenPort_Throws_If_Port_Is_More_Than_Max()
        {
            var ex = Record.Exception(() => new SetListenPortCommand(int.MaxValue));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SetListenPort")]
        [Fact(DisplayName = "SetListenPort constructs the correct message")]
        public void SetListenPort_Constructs_The_Correct_Message()
        {
            var port = new Random().Next(1024, 50000);
            var a = new SetListenPortCommand(port);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SetListenPort, code);
            Assert.Equal(port, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "ConnectToPeerRequest")]
        [Theory(DisplayName = "ConnectToPeerRequest instantiates properly"), AutoData]
        public void ConnectToPeerRequest_Instantiates_Properly(int token, string username, string type)
        {
            var a = new ConnectToPeerRequest(token, username, type);

            Assert.Equal(token, a.Token);
            Assert.Equal(username, a.Username);
            Assert.Equal(type, a.Type);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "ConnectToPeerRequest")]
        [Theory(DisplayName = "ConnectToPeerRequest constructs the correct message"), AutoData]
        public void ConnectToPeerRequest_Constructs_The_Correct_Message(int token, string username, string type)
        {
            var a = new ConnectToPeerRequest(token, username, type);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.ConnectToPeer, code);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(type, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetSharedCounts")]
        [Theory(DisplayName = "SetSharedCounts instantiates properly"), AutoData]
        public void SetSharedCounts_Instantiates_Properly(int dirs, int files)
        {
            var a = new SetSharedCountsCommand(dirs, files);

            Assert.Equal(dirs, a.DirectoryCount);
            Assert.Equal(files, a.FileCount);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SetSharedCounts")]
        [Theory(DisplayName = "SetSharedCounts constructs the correct message"), AutoData]
        public void SetSharedCounts_Constructs_The_Correct_Message(int dirs, int files)
        {
            var a = new SetSharedCountsCommand(dirs, files);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SharedFoldersAndFiles, code);
            Assert.Equal(dirs, reader.ReadInteger());
            Assert.Equal(files, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetOnlineStatus")]
        [Theory(DisplayName = "SetOnlineStatus instantiates properly"), AutoData]
        public void SetOnlineStatus_Instantiates_Properly(UserPresence status)
        {
            var a = new SetOnlineStatusCommand(status);

            Assert.Equal(status, a.Status);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SetOnlineStatus")]
        [Theory(DisplayName = "SetOnlineStatus constructs the correct message"), AutoData]
        public void SetOnlineStatus_Constructs_The_Correct_Message(UserPresence status)
        {
            var a = new SetOnlineStatusCommand(status);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SetOnlineStatus, code);
            Assert.Equal((int)status, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SendUploadSpeed")]
        [Theory(DisplayName = "SendUploadSpeed instantiates properly"), AutoData]
        public void SendUploadSpeed_Instantiates_Properly(int speed)
        {
            var a = new SendUploadSpeedCommand(speed);

            Assert.Equal(speed, a.Speed);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SendUploadSpeed")]
        [Theory(DisplayName = "SendUploadSpeed constructs the correct message"), AutoData]
        public void SendUploadSpeed_Constructs_The_Correct_Message(int speed)
        {
            var a = new SendUploadSpeedCommand(speed);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SendUploadSpeed, code);
            Assert.Equal(speed, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "RoomMessageCommand")]
        [Theory(DisplayName = "RoomMessageCommand instantiates properly"), AutoData]
        public void RoomMessageCommand_Instantiates_Properly(string room, string msg)
        {
            var a = new RoomMessageCommand(room, msg);

            Assert.Equal(room, a.RoomName);
            Assert.Equal(msg, a.Message);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "RoomMessageCommand")]
        [Theory(DisplayName = "RoomMessageCommand constructs the correct message"), AutoData]
        public void RoomMessageCommand_Constructs_The_Correct_Message(string room, string m)
        {
            var a = new RoomMessageCommand(room, m);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SayInChatRoom, code);
            Assert.Equal(room, reader.ReadString());
            Assert.Equal(m, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "JoinRoomRequest")]
        [Theory(DisplayName = "JoinRoomRequest instantiates properly"), AutoData]
        public void JoinRoomRequest_Instantiates_Properly(string room, bool isPrivate)
        {
            var a = new JoinRoomRequest(room, isPrivate);

            Assert.Equal(room, a.RoomName);
            Assert.Equal(isPrivate, a.IsPrivate);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "JoinRoomRequest")]
        [Theory(DisplayName = "JoinRoomRequest constructs the correct message when not private"), AutoData]
        public void JoinRoomRequest_Constructs_The_Correct_Message_When_Not_Private(string room)
        {
            var a = new JoinRoomRequest(room);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.JoinRoom, code);
            Assert.Equal(room, reader.ReadString());
            Assert.Equal(0, reader.ReadInteger());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "JoinRoomRequest")]
        [Theory(DisplayName = "JoinRoomRequest constructs the correct message when private"), AutoData]
        public void JoinRoomRequest_Constructs_The_Correct_Message_When_Private(string room)
        {
            var a = new JoinRoomRequest(room, true);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.JoinRoom, code);
            Assert.Equal(room, reader.ReadString());
            Assert.Equal(1, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "LeaveRoomRequest")]
        [Theory(DisplayName = "LeaveRoomRequest instantiates properly"), AutoData]
        public void LeaveRoomRequest_Instantiates_Properly(string room)
        {
            var a = new LeaveRoomRequest(room);

            Assert.Equal(room, a.RoomName);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "LeaveRoomRequest")]
        [Theory(DisplayName = "LeaveRoomRequest constructs the correct message"), AutoData]
        public void LeaveRoomRequest_Constructs_The_Correct_Message(string room)
        {
            var a = new LeaveRoomRequest(room);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.LeaveRoom, code);
            Assert.Equal(room, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "RoomListRequest")]
        [Fact(DisplayName = "RoomListRequest constructs the correct message")]
        public void RoomListRequest_Constructs_The_Correct_Message()
        {
            var a = new RoomListRequest();
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.RoomList, code);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserSearchRequest")]
        [Theory(DisplayName = "UserSearchRequest constructs the correct message"), AutoData]
        public void UserSearchRequest_Constructs_The_Correct_Message(string username, string searchText, int token)
        {
            var a = new UserSearchRequest(username, searchText, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.UserSearch, code);
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(searchText, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "RoomSearchRequest")]
        [Theory(DisplayName = "RoomSearchRequest constructs the correct message"), AutoData]
        public void RoomSearchRequest_Constructs_The_Correct_Message(string roomName, string searchText, int token)
        {
            var a = new RoomSearchRequest(roomName, searchText, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.RoomSearch, code);
            Assert.Equal(roomName, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(searchText, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "AcknowledgePrivilegeNotificationCommand")]
        [Theory(DisplayName = "AcknowledgePrivilegeNotificationCommand constructs the correct message"), AutoData]
        public void AcknowledgePrivilegeNotificationCommand_Constructs_The_Correct_Message(int token)
        {
            var a = new AcknowledgePrivilegeNotificationCommand(token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.AcknowledgeNotifyPrivileges, code);
            Assert.Equal(token, reader.ReadInteger());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "CheckPrivilegesRequest")]
        [Fact(DisplayName = "CheckPrivilegesRequest constructs the correct message")]
        public void CheckPrivilegesRequest_Constructs_The_Correct_Message()
        {
            var a = new CheckPrivilegesRequest();
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.CheckPrivileges, code);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "GivePrivilegesCommand")]
        [Theory(DisplayName = "GivePrivilegesCommand constructs the correct message"), AutoData]
        public void GivePrivilegesCommand_Constructs_The_Correct_Message(string username, int days)
        {
            var a = new GivePrivilegesCommand(username, days);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GivePrivileges, code);
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(days, reader.ReadInteger());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserPrivilegesRequest")]
        [Theory(DisplayName = "UserPrivilegesRequest constructs the correct message"), AutoData]
        public void UserPrivilegesRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new UserPrivilegesRequest(username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.UserPrivileges, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PrivateRoomDropMembershipCommand")]
        [Theory(DisplayName = "PrivateRoomDropMembershipCommand constructs the correct message"), AutoData]
        public void PrivateRoomDropMembershipCommand_Constructs_The_Correct_Message(string roomName)
        {
            var a = new PrivateRoomDropMembershipCommand(roomName);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.PrivateRoomDropMembership, code);
            Assert.Equal(roomName, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PrivateRoomDropOwnershipCommand")]
        [Theory(DisplayName = "PrivateRoomDropOwnershipCommand constructs the correct message"), AutoData]
        public void PrivateRoomDropOwnershipCommand_Constructs_The_Correct_Message(string roomName)
        {
            var a = new PrivateRoomDropOwnershipCommand(roomName);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.PrivateRoomDropOwnership, code);
            Assert.Equal(roomName, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "StartPublicChatCommand")]
        [Fact(DisplayName = "StartPublicChatCommand constructs the correct message")]
        public void StartPublicChatCommand_Constructs_The_Correct_Message()
        {
            var a = new StartPublicChatCommand();
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.AskPublicChat, code);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "StopPublicChatCommand")]
        [Fact(DisplayName = "StopPublicChatCommand constructs the correct message")]
        public void StopPublicChatCommand_Constructs_The_Correct_Message()
        {
            var a = new StopPublicChatCommand();
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.StopPublicChat, code);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SetRoomTickerCommand")]
        [Theory(DisplayName = "SetRoomTickerCommand constructs the correct message"), AutoData]
        public void SetRoomTickerCommand_Constructs_The_Correct_Message(string roomName, string message)
        {
            var a = new SetRoomTickerCommand(roomName, message);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SetRoomTicker, code);
            Assert.Equal(roomName, reader.ReadString());
            Assert.Equal(message, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UnwatchUserCommand")]
        [Theory(DisplayName = "UnwatchUserCommand constructs the correct message"), AutoData]
        public void UnwatchUserCommand_Constructs_The_Correct_Message(string username)
        {
            var a = new UnwatchUserCommand(username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.UnwatchUser, code);
            Assert.Equal(username, reader.ReadString());
            Assert.False(reader.HasMoreData);
        }
    }
}
