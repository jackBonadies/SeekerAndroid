// <copyright file="MessageCode.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging
{
    /// <summary>
    ///     Message codes.
    /// </summary>
    internal static class MessageCode
    {
        /// <summary>
        ///     Distributed message codes.
        /// </summary>
        public enum Distributed : byte
        {
            /// <summary>
            ///     0
            /// </summary>
            Ping = 0,

            /// <summary>
            ///     3
            /// </summary>
            SearchRequest = 3,

            /// <summary>
            ///     4
            /// </summary>
            BranchLevel = 4,

            /// <summary>
            ///     5
            /// </summary>
            BranchRoot = 5,

            /// <summary>
            ///     6
            /// </summary>
            Unknown = 6,

            /// <summary>
            ///     7
            /// </summary>
            ChildDepth = 7,

            /// <summary>
            ///     Server code 93
            /// </summary>
            EmbeddedMessage = 93,
        }

        /// <summary>
        ///     Connection initialization codes.
        /// </summary>
        public enum Initialization : byte
        {
            /// <summary>
            ///     Pierce firewall; sent by peers responding to a solicited connection request.
            /// </summary>
            PierceFirewall = 0,

            /// <summary>
            ///     Peer init; sent by peers creating a direct connection.
            /// </summary>
            PeerInit = 1,
        }

        /// <summary>
        ///     Peer message codes.
        /// </summary>
        /// <remarks>
        ///     Codes from 0-5500 were sent to Soulseek Qt 2020.3.12 and all but the ones
        ///     documented here were reported as 'unknown'.
        /// </remarks>
        public enum Peer
        {
            /// <summary>
            ///     1 (Deprecated)
            /// </summary>
            PrivateMessage = 1,

            /// <summary>
            ///     4
            /// </summary>
            BrowseRequest = 4,

            /// <summary>
            ///     5
            /// </summary>
            BrowseResponse = 5,

            /// <summary>
            ///     8 (Deprecated)
            /// </summary>
            SearchRequest = 8,

            /// <summary>
            ///     9
            /// </summary>
            SearchResponse = 9,

            /// <summary>
            ///     10 (Deprecated)
            /// </summary>
            PrivateRoomInvitation = 10,

            /// <summary>
            ///     14 (Deprecated)
            /// </summary>
            CancelledQueuedTransfer = 14,

            /// <summary>
            ///     15
            /// </summary>
            InfoRequest = 15,

            /// <summary>
            ///     16
            /// </summary>
            InfoResponse = 16,

            /// <summary>
            ///     33 (Deprecated)
            /// </summary>
            SendConnectToken = 33,

            /// <summary>
            ///     34 (Deprecated)
            /// </summary>
            MoveDownloadToTop = 34,

            /// <summary>
            ///     36
            /// </summary>
            FolderContentsRequest = 36,

            /// <summary>
            ///     37
            /// </summary>
            FolderContentsResponse = 37,

            /// <summary>
            ///     40
            /// </summary>
            TransferRequest = 40,

            /// <summary>
            ///     41
            /// </summary>
            TransferResponse = 41,

            /// <summary>
            ///     42
            /// </summary>
            UploadPlacehold = 42,

            /// <summary>
            ///     43
            /// </summary>
            QueueDownload = 43,

            /// <summary>
            ///     44
            /// </summary>
            PlaceInQueueResponse = 44,

            /// <summary>
            ///     46
            /// </summary>
            UploadFailed = 46,

            /// <summary>
            ///     47 (Deprecated)
            /// </summary>
            ExactFileSearchRequest = 47,

            /// <summary>
            ///     48 (Deprecated)
            /// </summary>
            QueuedDownloads = 48,

            /// <summary>
            ///     49 (Deprecated)
            /// </summary>
            IndirectFileSearchRequest = 49,

            /// <summary>
            ///     50
            /// </summary>
            UploadDenied = 50,

            /// <summary>
            ///     51
            /// </summary>
            PlaceInQueueRequest = 51,

            /// <summary>
            ///     52 (Deprecated)
            /// </summary>
            UploadQueueNotification = 52,
        }

        /// <summary>
        ///     Server message codes.
        /// </summary>
#pragma warning disable CA1724 // The type name Server conflicts in whole or in part with the namespace 'Microsoft.SqlServer.Server'
        public enum Server
#pragma warning restore CA1724 // The type name Server conflicts in whole or in part with the namespace 'Microsoft.SqlServer.Server'
        {
            /// <summary>
            ///     0/Unknown
            /// </summary>
            Unknown = 0,

            /// <summary>
            ///     1
            /// </summary>
            Login = 1,

            /// <summary>
            ///     2
            /// </summary>
            SetListenPort = 2,

            /// <summary>
            ///     3
            /// </summary>
            GetPeerAddress = 3,

            /// <summary>
            ///     5
            /// </summary>
            WatchUser = 5,

            /// <summary>
            ///     6
            /// </summary>
            UnwatchUser = 6,

            /// <summary>
            ///     7
            /// </summary>
            GetStatus = 7,

            /// <summary>
            ///     13
            /// </summary>
            SayInChatRoom = 13,

            /// <summary>
            ///     14
            /// </summary>
            JoinRoom = 14,

            /// <summary>
            ///     15
            /// </summary>
            LeaveRoom = 15,

            /// <summary>
            ///     16
            /// </summary>
            UserJoinedRoom = 16,

            /// <summary>
            ///     17
            /// </summary>
            UserLeftRoom = 17,

            /// <summary>
            ///     18
            /// </summary>
            ConnectToPeer = 18,

            /// <summary>
            ///     22
            /// </summary>
            PrivateMessage = 22,

            /// <summary>
            ///     23
            /// </summary>
            AcknowledgePrivateMessage = 23,

            /// <summary>
            ///     26
            /// </summary>
            FileSearch = 26,

            /// <summary>
            ///     28
            /// </summary>
            SetOnlineStatus = 28,

            /// <summary>
            ///     32
            /// </summary>
            Ping = 32,

            /// <summary>
            ///     34
            /// </summary>
            SendSpeed = 34,

            /// <summary>
            ///     35
            /// </summary>
            SharedFoldersAndFiles = 35,

            /// <summary>
            ///     36
            /// </summary>
            GetUserStats = 36,

            /// <summary>
            ///     40
            /// </summary>
            QueuedDownloads = 40,

            /// <summary>
            ///     41
            /// </summary>
            KickedFromServer = 41,

            /// <summary>
            ///     42
            /// </summary>
            UserSearch = 42,

            /// <summary>
            ///     51
            /// </summary>
            InterestAdd = 51,

            /// <summary>
            ///     52
            /// </summary>
            InterestRemove = 52,

            /// <summary>
            ///     54
            /// </summary>
            GetRecommendations = 54,

            /// <summary>
            ///     56
            /// </summary>
            GetGlobalRecommendations = 56,

            /// <summary>
            ///     57
            /// </summary>
            GetUserInterests = 57,

            /// <summary>
            ///     64
            /// </summary>
            RoomList = 64,

            /// <summary>
            ///     65
            /// </summary>
            ExactFileSearch = 65,

            /// <summary>
            ///     66
            /// </summary>
            GlobalAdminMessage = 66,

            /// <summary>
            ///     69
            /// </summary>
            PrivilegedUsers = 69,

            /// <summary>
            ///     71
            /// </summary>
            HaveNoParents = 71,

            /// <summary>
            ///     73
            /// </summary>
            ParentsIP = 73,

            /// <summary>
            ///     83
            /// </summary>
            ParentMinSpeed = 83,

            /// <summary>
            ///     84
            /// </summary>
            ParentSpeedRatio = 84,

            /// <summary>
            ///     86
            /// </summary>
            ParentInactivityTimeout = 86,

            /// <summary>
            ///     87
            /// </summary>
            SearchInactivityTimeout = 87,

            /// <summary>
            ///     88
            /// </summary>
            MinimumParentsInCache = 88,

            /// <summary>
            ///     90
            /// </summary>
            DistributedAliveInterval = 90,

            /// <summary>
            ///     91
            /// </summary>
            AddPrivilegedUser = 91,

            /// <summary>
            ///     92
            /// </summary>
            CheckPrivileges = 92,

            /// <summary>
            ///     93
            /// </summary>
            EmbeddedMessage = 93,

            /// <summary>
            ///     100
            /// </summary>
            AcceptChildren = 100,

            /// <summary>
            ///     102
            /// </summary>
            NetInfo = 102,

            /// <summary>
            ///     103
            /// </summary>
            WishlistSearch = 103,

            /// <summary>
            ///     104
            /// </summary>
            WishlistInterval = 104,

            /// <summary>
            ///     110
            /// </summary>
            GetSimilarUsers = 110,

            /// <summary>
            ///     111
            /// </summary>
            GetItemRecommendations = 111,

            /// <summary>
            ///     112
            /// </summary>
            GetItemSimilarUsers = 112,

            /// <summary>
            ///     113
            /// </summary>
            RoomTickers = 113,

            /// <summary>
            ///     114
            /// </summary>
            RoomTickerAdd = 114,

            /// <summary>
            ///     115
            /// </summary>
            RoomTickerRemove = 115,

            /// <summary>
            ///     116
            /// </summary>
            SetRoomTicker = 116,

            /// <summary>
            ///     117
            /// </summary>
            HatedInterestAdd = 117,

            /// <summary>
            ///     118
            /// </summary>
            HatedInterestRemove = 118,

            /// <summary>
            ///     120
            /// </summary>
            RoomSearch = 120,

            /// <summary>
            ///     121
            /// </summary>
            SendUploadSpeed = 121,

            /// <summary>
            ///     122
            /// </summary>
            UserPrivileges = 122,

            /// <summary>
            ///     123
            /// </summary>
            GivePrivileges = 123,

            /// <summary>
            ///     124
            /// </summary>
            NotifyPrivileges = 124,

            /// <summary>
            ///     125
            /// </summary>
            AcknowledgeNotifyPrivileges = 125,

            /// <summary>
            ///     126
            /// </summary>
            BranchLevel = 126,

            /// <summary>
            ///     127
            /// </summary>
            BranchRoot = 127,

            /// <summary>
            ///     129
            /// </summary>
            ChildDepth = 129,

            /// <summary>
            ///     130
            /// </summary>
            DistributedReset = 130,

            /// <summary>
            ///     133
            /// </summary>
            PrivateRoomUsers = 133,

            /// <summary>
            ///     134
            /// </summary>
            PrivateRoomAddUser = 134,

            /// <summary>
            ///     135
            /// </summary>
            PrivateRoomRemoveUser = 135,

            /// <summary>
            ///     136
            /// </summary>
            PrivateRoomDropMembership = 136,

            /// <summary>
            ///     137
            /// </summary>
            PrivateRoomDropOwnership = 137,

            /// <summary>
            ///     138
            /// </summary>
            PrivateRoomUnknown = 138,

            /// <summary>
            ///     139
            /// </summary>
            PrivateRoomAdded = 139,

            /// <summary>
            ///     140
            /// </summary>
            PrivateRoomRemoved = 140,

            /// <summary>
            ///     141
            /// </summary>
            PrivateRoomToggle = 141,

            /// <summary>
            ///     142
            /// </summary>
            NewPassword = 142,

            /// <summary>
            ///     143
            /// </summary>
            PrivateRoomAddOperator = 143,

            /// <summary>
            ///     144
            /// </summary>
            PrivateRoomRemoveOperator = 144,

            /// <summary>
            ///     145
            /// </summary>
            PrivateRoomOperatorAdded = 145,

            /// <summary>
            ///     146
            /// </summary>
            PrivateRoomOperatorRemoved = 146,

            /// <summary>
            ///     148
            /// </summary>
            PrivateRoomOwned = 148,

            /// <summary>
            ///     149
            /// </summary>
            MessageUsers = 149,

            /// <summary>
            ///     150
            /// </summary>
            AskPublicChat = 150,

            /// <summary>
            ///     151
            /// </summary>
            StopPublicChat = 151,

            /// <summary>
            ///     152
            /// </summary>
            PublicChat = 152,

            /// <summary>
            ///     153
            /// </summary>
            RelatedSearch = 153,

            /// <summary>
            ///     160
            /// </summary>
            ExcludedSearchPhrases = 160,

            /// <summary>
            ///     1001
            /// </summary>
            CannotConnect = 1001,

            /// <summary>
            ///     1002
            /// </summary>
            CannotCreateRoom = 1002,

            /// <summary>
            ///     1003
            /// </summary>
            CannotJoinRoom = 1003,
        }
    }
}