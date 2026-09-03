// <copyright file="IDistributedMessageHandler.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Messaging.Handlers
{
    using Soulseek.Network;

    /// <summary>
    ///     Handles incoming messages from distributed connections.
    /// </summary>
    internal interface IDistributedMessageHandler : IMessageHandler
    {
        /// <summary>
        ///     Handles incoming messages from distributed children.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> from which the message originated.</param>
        /// <param name="args">The message event args.</param>
        void HandleChildMessageRead(object sender, MessageEventArgs args);

        /// <summary>
        ///     Handles incoming messages from distributed children.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> from which the message originated.</param>
        /// <param name="message">The message.</param>
        void HandleChildMessageRead(object sender, byte[] message);

        /// <summary>
        ///     Handles outgoing messages to distributed children, post send.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> instance to which the message was sent.</param>
        /// <param name="args">The message event args.</param>
        void HandleChildMessageWritten(object sender, MessageEventArgs args);

        /// <summary>
        ///     Handles embedded messages from the server.
        /// </summary>
        /// <param name="message">The message.</param>
        void HandleEmbeddedMessage(byte[] message);
    }
}