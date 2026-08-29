// <copyright file="BrowseResponseFactory.cs" company="JP Dillingham">
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
    using System.Collections.Generic;

    /// <summary>
    ///     Factory for browse response messages. This class helps keep message abstractions from leaking into the public API via
    ///     <see cref="BrowseResponse"/>, which is a public class.
    /// </summary>
    internal static class BrowseResponseFactory
    {
        /// <summary>
        ///     Creates a new instance of <see cref="BrowseResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static BrowseResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.BrowseResponse)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(BrowseResponse)} (expected: {(int)MessageCode.Peer.BrowseResponse}, received: {(int)code}");
            }

            reader.Decompress();

            var directoryCount = reader.ReadInteger();
            var directoryList = new List<Directory>();
            var lockedDirectoryList = new List<Directory>();

            for (int i = 0; i < directoryCount; i++)
            {
                directoryList.Add(reader.ReadDirectory());
            }

            if (reader.HasMoreData)
            {
                _ = reader.ReadInteger();

                if (reader.HasMoreData)
                {
                    var lockedDirectoryCount = reader.ReadInteger();

                    for (int i = 0; i < lockedDirectoryCount; i++)
                    {
                        lockedDirectoryList.Add(reader.ReadDirectory());
                    }
                }
            }

            return new BrowseResponse(directoryList, lockedDirectoryList);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <param name="browseResponse">The instance from which to construct the byte array.</param>
        /// <returns>The constructed byte array.</returns>
        public static byte[] ToByteArray(this BrowseResponse browseResponse)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(browseResponse.DirectoryCount);

            foreach (var directory in browseResponse.Directories)
            {
                builder.WriteDirectory(directory);
            }

            builder.WriteInteger(0);
            builder.WriteInteger(browseResponse.LockedDirectoryCount);

            foreach (var directory in browseResponse.LockedDirectories)
            {
                builder.WriteDirectory(directory);
            }

            builder.Compress();
            return builder.Build();
        }
    }
}