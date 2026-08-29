// <copyright file="EmbeddedMessage.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System.Linq;

    /// <summary>
    ///     An embedded message, sent from the server and intended for forwarding to the distributed network.
    /// </summary>
    internal sealed class EmbeddedMessage : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EmbeddedMessage"/> class.
        /// </summary>
        /// <param name="distributedCode">The code of the embedded message.</param>
        /// <param name="distributedMessage">The embedded message.</param>
        public EmbeddedMessage(MessageCode.Distributed distributedCode, byte[] distributedMessage)
        {
            DistributedCode = distributedCode;
            DistributedMessage = distributedMessage;
        }

        /// <summary>
        ///     Gets the code of the embedded message.
        /// </summary>
        public MessageCode.Distributed DistributedCode { get; }

        /// <summary>
        ///     Gets the embedded message.
        /// </summary>
        public byte[] DistributedMessage { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="EmbeddedMessage"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static EmbeddedMessage FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.EmbeddedMessage)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(EmbeddedMessage)} (expected: {(int)MessageCode.Server.EmbeddedMessage}, received: {(int)code})");
            }

            var distributedCode = (MessageCode.Distributed)reader.ReadByte();

            var distributedMessage = new MessageBuilder()
                .WriteCode(distributedCode)
                .WriteBytes(bytes.Skip(9).ToArray())
                .Build();

            return new EmbeddedMessage(distributedCode, distributedMessage);
        }
    }
}
