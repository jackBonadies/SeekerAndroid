// <copyright file="IServerMessageHandler.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Handles incoming messages from the server connection.
    /// </summary>
    internal interface IServerMessageHandler : IMessageHandler
    {
        /// <summary>
        ///     Occurs when the server requests a distributed network reset.
        /// </summary>
        event EventHandler DistributedNetworkReset;

        /// <summary>
        ///     Occurs when the server sends a list of excluded ("banned") search phrases.
        /// </summary>
        event EventHandler<IReadOnlyCollection<string>> ExcludedSearchPhrasesReceived;

        /// <summary>
        ///     Occurs when a global message is received.
        /// </summary>
        event EventHandler<string> GlobalMessageReceived;

        /// <summary>
        ///     Occurs when the client is forcefully disconnected from the server, probably because another client logged in with
        ///     the same credentials.
        /// </summary>
        event EventHandler KickedFromServer;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when the currently logged in user is granted membership to a private room.
        /// </summary>
        event EventHandler<string> PrivateRoomMembershipAdded;

        /// <summary>
        ///     Occurs when the currently logged in user has membership to a private room revoked.
        /// </summary>
        event EventHandler<string> PrivateRoomMembershipRemoved;

        /// <summary>
        ///     Occurs when a list of moderated users for a private room is received.
        /// </summary>
        event EventHandler<RoomInfo> PrivateRoomModeratedUserListReceived;

        /// <summary>
        ///     Occurs when the currently logged in user is granted moderator status in a private room.
        /// </summary>
        event EventHandler<string> PrivateRoomModerationAdded;

        /// <summary>
        ///     Occurs when the currently logged in user has moderator status removed in a private room.
        /// </summary>
        event EventHandler<string> PrivateRoomModerationRemoved;

        /// <summary>
        ///     Occurs when a list of users for a private room is received.
        /// </summary>
        event EventHandler<RoomInfo> PrivateRoomUserListReceived;

        /// <summary>
        ///     Occurs when the server sends a list of privileged users.
        /// </summary>
        event EventHandler<IReadOnlyCollection<string>> PrivilegedUserListReceived;

        /// <summary>
        ///     Occurs when the server sends a notification of new user privileges.
        /// </summary>
        event EventHandler<PrivilegeNotificationReceivedEventArgs> PrivilegeNotificationReceived;

        /// <summary>
        ///     Occurs when a public chat message is received.
        /// </summary>
        event EventHandler<PublicChatMessageReceivedEventArgs> PublicChatMessageReceived;

        /// <summary>
        ///     Occurs when a user joins a chat room.
        /// </summary>
        event EventHandler<RoomJoinedEventArgs> RoomJoined;

        /// <summary>
        ///     Occurs when a user leaves a chat room.
        /// </summary>
        event EventHandler<RoomLeftEventArgs> RoomLeft;

        /// <summary>
        ///     Occurs when the server sends a list of chat rooms.
        /// </summary>
        event EventHandler<RoomList> RoomListReceived;

        /// <summary>
        ///     Occurs when a chat room message is received.
        /// </summary>
        event EventHandler<RoomMessageReceivedEventArgs> RoomMessageReceived;

        /// <summary>
        ///     Occurs when a chat room ticker is added.
        /// </summary>
        event EventHandler<RoomTickerAddedEventArgs> RoomTickerAdded;

        /// <summary>
        ///     Occurs when the server sends a list of tickers for a chat room.
        /// </summary>
        event EventHandler<RoomTickerListReceivedEventArgs> RoomTickerListReceived;

        /// <summary>
        ///     Occurs when a chat room ticker is removed.
        /// </summary>
        event EventHandler<RoomTickerRemovedEventArgs> RoomTickerRemoved;

        /// <summary>
        ///     Occurs when the server sends operational information.
        /// </summary>
        event EventHandler<ServerInfo> ServerInfoReceived;

        /// <summary>
        ///     Occurs when a user fails to connect.
        /// </summary>
        event EventHandler<UserCannotConnectEventArgs> UserCannotConnect;

        /// <summary>
        ///     Occurs when a user's statistics change.
        /// </summary>
        event EventHandler<UserStatistics> UserStatisticsChanged;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        event EventHandler<UserStatus> UserStatusChanged;
    }
}