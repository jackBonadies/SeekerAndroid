// <copyright file="Constants.cs" company="JP Dillingham">
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

namespace Soulseek
{
    /// <summary>
    ///     Application constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        ///     Connection methods.
        /// </summary>
        internal static class ConnectionMethod
        {
            /// <summary>
            ///     Direct.
            /// </summary>
            public const string Direct = "Direct";

            /// <summary>
            ///     Indirect.
            /// </summary>
            public const string Indirect = "Indirect";
        }

        /// <summary>
        ///     Connection types.
        /// </summary>
        internal static class ConnectionType
        {
            /// <summary>
            ///     Distributed (D).
            /// </summary>
            public const string Distributed = "D";

            /// <summary>
            ///     Peer (P).
            /// </summary>
            public const string Peer = "P";

            /// <summary>
            ///     Transfer (F).
            /// </summary>
            public const string Transfer = "F";
        }

        /// <summary>
        ///     Wait keys.
        /// </summary>
        internal static class WaitKey
        {
            /// <summary>
            ///     BranchLevelMessage.
            /// </summary>
            public const string BranchLevelMessage = "BranchLevelMessage";

            /// <summary>
            ///     BranchRootMessage.
            /// </summary>
            public const string BranchRootMessage = "BranchRootMessage";

            /// <summary>
            ///     BrowseResponseConnection.
            /// </summary>
            public const string BrowseResponseConnection = "BrowseResponseConnection";

            /// <summary>
            ///     ChildDepthMessage.
            /// </summary>
            public const string ChildDepthMessage = "ChildDepthMessage";

            /// <summary>
            ///     DirectTransfer.
            /// </summary>
            public const string DirectTransfer = "DirectTransfer";

            /// <summary>
            ///     IndirectTransfer.
            /// </summary>
            public const string IndirectTransfer = "IndirectTransfer";

            /// <summary>
            ///     SearchRequestMessage.
            /// </summary>
            public const string SearchRequestMessage = "SearchRequestMessage";

            /// <summary>
            ///     SolicitedDistributedConnection.
            /// </summary>
            public const string SolicitedDistributedConnection = "SolicitedDistributedConnection";

            /// <summary>
            ///     SolicitedPeerConnection.
            /// </summary>
            public const string SolicitedPeerConnection = "SolicitedPeerConnection";

            /// <summary>
            ///     Transfer.
            /// </summary>
            public const string Transfer = "Transfer";
        }
    }
}