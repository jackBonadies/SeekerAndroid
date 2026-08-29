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
        internal static File ReadFile(this MessageReader<MessageCode.Peer> reader, bool fileIsFullfilename = false, bool isDirectoryDecodedViaLatin1 = false)
        {
            var code = reader.ReadByte();
            var filename = reader.ReadStringAndNoteEncoding(out bool isLatin1);
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

            #if DEBUG

            if(isLatin1)
            {
                
            }

            #endif

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
                attributeList,
                isLatin1Decoded: isLatin1,
                isDirectoryLatin1Decoded: (fileIsFullfilename && isLatin1) || isDirectoryDecodedViaLatin1);
        }

        /// <summary>
        ///     Reads a list of <paramref name="count"/> files from the <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The reader from which to read the list of files.</param>
        /// <param name="count">The number of files to read.</param>
        /// <param name="filenameIsFullfilename">The filename we are reading includes the directory.</param>
        /// <returns>The list of files.</returns>
        internal static IReadOnlyCollection<File> ReadFiles(this MessageReader<MessageCode.Peer> reader, int count, bool filenameIsFullfilename = false)
        {
            var files = new List<File>();

            for (int i = 0; i < count; i++)
            {
                files.Add(reader.ReadFile(filenameIsFullfilename));
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
            var directoryName = reader.ReadStringAndNoteEncoding(out bool isDirectoryDecodedViaLatin1);

            #if DEBUG

            if(isDirectoryDecodedViaLatin1)
            {

            }

            #endif

            var fileCount = reader.ReadInteger();

            var fileList = new List<File>();

            for (int j = 0; j < fileCount; j++)
            {
                fileList.Add(reader.ReadFile(false, isDirectoryDecodedViaLatin1));
            }

            return new Directory(
                name: directoryName,
                fileList: fileList,
                decodedViaLatin1: isDirectoryDecodedViaLatin1);
        }
    }
}
