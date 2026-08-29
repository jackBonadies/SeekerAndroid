// <copyright file="SetSharedCountsCommand.cs" company="JP Dillingham">
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