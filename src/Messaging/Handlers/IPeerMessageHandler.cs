// <copyright file="IPeerMessageHandler.cs" company="JP Dillingham">
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
    using Soulseek.Network;

    /// <summary>
    ///     Handles incoming messages from peer connections.
    /// </summary>
    internal interface IPeerMessageHandler : IMessageHandler
    {
        /// <summary>
        ///     Occurs when a user reports that a download has been denied.
        /// </summary>
        event EventHandler<DownloadDeniedEventArgs> DownloadDenied;

        /// <summary>
        ///     Occurs when a user reports that a download has failed.
        /// </summary>
        event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        ///     Handles the receipt of incoming messages, prior to the body having been read and parsed.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="args">The message receipt event args.</param>
        void HandleMessageReceived(object sender, MessageReceivedEventArgs args);
    }
}