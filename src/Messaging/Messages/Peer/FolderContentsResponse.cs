// <copyright file="FolderContentsResponse.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     The response to a peer folder contents request.
    /// </summary>
    internal sealed class FolderContentsResponse : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FolderContentsResponse"/> class.
        /// </summary>
        /// <param name="token">The unique token for the request.</param>
        /// <param name="directoryName">The name of the requested (root) directory.</param>
        /// <param name="directories">The directory contents.</param>
        public FolderContentsResponse(int token, string directoryName, IEnumerable<Directory> directories)
        {
            Token = token;

            DirectoryName = directoryName;

            Directories = directories.ToList().AsReadOnly();
            DirectoryCount = Directories.Count;
        }

        /// <summary>
        ///     Gets the directory contents.
        /// </summary>
        public IReadOnlyCollection<Directory> Directories { get; }

        /// <summary>
        ///     Gets the number of directories.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the name of the requested (root) directory.
        /// </summary>
        public string DirectoryName { get; }

        /// <summary>
        ///     Gets the token for the response.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="FolderContentsResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static FolderContentsResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.FolderContentsResponse)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(FolderContentsResponse)} (expected: {(int)MessageCode.Peer.FolderContentsResponse}, received: {(int)code}");
            }

            reader.Decompress();

            var token = reader.ReadInteger();
            var rootDirectory = reader.ReadString(); // directory name, should always match that of the first directory
            var directoryCount = reader.ReadInteger(); // directory count, should always be 1
            var directoryList = new List<Directory>();

            for (int i = 0; i < directoryCount; i++)
            {
                directoryList.Add(reader.ReadDirectory());
            }

            return new FolderContentsResponse(token, rootDirectory, directoryList);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(Token)
                .WriteString(DirectoryName)
                .WriteInteger(DirectoryCount);

            foreach (var directory in Directories)
            {
                builder.WriteDirectory(directory);
            }

            builder.Compress();
            return builder.Build();
        }
    }
}