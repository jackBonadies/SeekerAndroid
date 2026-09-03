// <copyright file="SetSharedCountsCommand.cs" company="JP Dillingham">
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
    ///     Informs the server of the current number of shared directories and files.
    /// </summary>
    internal sealed class SetSharedCountsCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SetSharedCountsCommand"/> class.
        /// </summary>
        /// <param name="directoryCount">The number of shared directories.</param>
        /// <param name="fileCount">The number of shared files.</param>
        public SetSharedCountsCommand(int directoryCount, int fileCount)
        {
            DirectoryCount = directoryCount;
            FileCount = fileCount;
        }

        /// <summary>
        ///     Gets the number of shared directories.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the number of shared files.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.SharedFoldersAndFiles)
                .WriteInteger(DirectoryCount)
                .WriteInteger(FileCount)
                .Build();
        }
    }
}