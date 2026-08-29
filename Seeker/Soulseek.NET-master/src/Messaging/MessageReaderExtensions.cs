// <copyright file="MessageReaderExtensions.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Extensions for <see cref="MessageReader{T}"/>.
    /// </summary>
    /// <remarks>
    ///     This keeps domain logic out of the reader.
    /// </remarks>
    internal static class MessageReaderExtensions
    {
        /// <summary>
        ///     Reads a file from the <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The reader from which to read the file.</param>
        /// <returns>The file.</returns>
        internal static File ReadFile(this MessageReader<MessageCode.Peer> reader)
        {
            var code = reader.ReadByte();
            var filename = reader.ReadString();
            var size = reader.ReadLong();
            var extension = reader.ReadString();

            // check for an overflow, most likely sent from Soulseek NS due to a file size
            // exceeding 2gb.
            if (size < 0)
            {
                var sizeBytes = BitConverter.GetBytes(size);

                if (sizeBytes.Skip(4).All(b => b == 0xFF))
                {
                    size = BitConverter.ToUInt32(sizeBytes.Take(4).ToArray(), 0);
                }
            }

            var attributeCount = reader.ReadInteger();
            var attributeList = new List<FileAttribute>();

            for (int i = 0; i < attributeCount; i++)
            {
                var attribute = new FileAttribute(
                    type: (FileAttributeType)reader.ReadInteger(),
                    value: reader.ReadInteger());

                attributeList.Add(attribute);
            }

            return new File(
                code,
                filename,
                size,
                extension,
                attributeList);
        }

        /// <summary>
        ///     Reads a list of <paramref name="count"/> files from the <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The reader from which to read the list of files.</param>
        /// <param name="count">The number of files to read.</param>
        /// <returns>The list of files.</returns>
        internal static IReadOnlyCollection<File> ReadFiles(this MessageReader<MessageCode.Peer> reader, int count)
        {
            var files = new List<File>();

            for (int i = 0; i < count; i++)
            {
                files.Add(reader.ReadFile());
            }

            return files.AsReadOnly();
        }

        /// <summary>
        ///     Reads a directory from the <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The reader from which to read the directory.</param>
        /// <returns>The directory.</returns>
        internal static Directory ReadDirectory(this MessageReader<MessageCode.Peer> reader)
        {
            var directoryName = reader.ReadString();
            var fileCount = reader.ReadInteger();

            var fileList = new List<File>();

            for (int j = 0; j < fileCount; j++)
            {
                fileList.Add(reader.ReadFile());
            }

            return new Directory(
                name: directoryName,
                fileList: fileList);
        }
    }
}
