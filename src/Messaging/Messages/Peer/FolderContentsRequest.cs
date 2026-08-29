// <copyright file="FolderContentsRequest.cs" company="JP Dillingham">
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
    /// <summary>
    ///     A request to retreive the contents of a directory from a remote user.
    /// </summary>
    internal sealed class FolderContentsRequest : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FolderContentsRequest"/> class.
        /// </summary>
        /// <param name="token">The unique token for the request.</param>
        /// <param name="directoryName">The directory to fetch.</param>
        public FolderContentsRequest(int token, string directoryName)
        {
            DirectoryName = directoryName;
            Token = token;
        }

        /// <summary>
        ///     Gets the directory to fetch.
        /// </summary>
        public string DirectoryName { get; }

        /// <summary>
        ///     Gets the unique token for the request.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="FolderContentsRequest"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static FolderContentsRequest FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.FolderContentsRequest)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(FolderContentsRequest)} (expected: {(int)MessageCode.Peer.FolderContentsRequest}, received: {(int)code})");
            }

            var token = reader.ReadInteger();
            var directoryName = reader.ReadString();

            return new FolderContentsRequest(token, directoryName);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsRequest)
                .WriteInteger(Token)
                .WriteString(DirectoryName)
                .Build();
        }
    }
}