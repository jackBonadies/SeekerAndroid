// <copyright file="SearchResponseFactory.cs" company="JP Dillingham">
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
    using System.Linq;

    /// <summary>
    ///     Factory for search response messages. This class helps keep message abstractions from leaking into the public API via
    ///     <see cref="SearchResponse"/>, which is a public class.
    /// </summary>
    internal static class SearchResponseFactory
    {
        /// <summary>
        ///     Creates a new instance of <see cref="SearchResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static SearchResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.SearchResponse)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(SearchResponse)} (expected: {(int)MessageCode.Peer.SearchResponse}, received: {(int)code}");
            }

            reader.Decompress();

            var username = reader.ReadString();
            var token = reader.ReadInteger();
            var fileCount = reader.ReadInteger();

            var fileList = reader.ReadFiles(fileCount);

            var freeUploadSlots = reader.ReadByte();
            var uploadSpeed = reader.ReadInteger();
            var queueLength = reader.ReadInteger();

            if (reader.HasMoreData)
            {
                // most clients send an unknown integer between queue length and the locked file count
                _ = reader.ReadInteger();
            }

            IEnumerable<File> lockedFileList = Enumerable.Empty<File>();

            if (reader.HasMoreData)
            {
                var count = reader.ReadInteger();
                lockedFileList = reader.ReadFiles(count);
            }

            return new SearchResponse(username, token, hasFreeUploadSlot: freeUploadSlots > 0, uploadSpeed, queueLength, fileList, lockedFileList);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <param name="searchResponse">The instance from which to construct the byte array.</param>
        /// <returns>The constructed byte array.</returns>
        public static byte[] ToByteArray(this SearchResponse searchResponse)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(searchResponse.Username)
                .WriteInteger(searchResponse.Token)
                .WriteInteger(searchResponse.FileCount);

            foreach (var file in searchResponse.Files)
            {
                builder.WriteFile(file);
            }

            builder
                .WriteByte((byte)(searchResponse.HasFreeUploadSlot ? 1 : 0))
                .WriteInteger(searchResponse.UploadSpeed)
                .WriteInteger(searchResponse.QueueLength)
                .WriteInteger(0); // unknown value included for compatibility

            builder.WriteInteger(searchResponse.LockedFileCount);

            foreach (var file in searchResponse.LockedFiles)
            {
                builder.WriteFile(file);
            }

            builder.Compress();
            return builder.Build();
        }
    }
}