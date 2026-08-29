// <copyright file="File.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A file within search and browse results.
    /// </summary>
    public class File
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="File"/> class.
        /// </summary>
        /// <param name="code">The file code.</param>
        /// <param name="filename">The file name.</param>
        /// <param name="size">The file size in bytes.</param>
        /// <param name="extension">The file extension.</param>
        /// <param name="attributeList">The optional list of <see cref="FileAttribute"/> s.</param>
        public File(int code, string filename, long size, string extension, IEnumerable<FileAttribute> attributeList = null)
        {
            Code = code;
            Filename = filename;
            Size = size;
            Extension = extension;

            Attributes = (attributeList?.ToList() ?? new List<FileAttribute>()).AsReadOnly();
            AttributeCount = Attributes.Count;

            foreach (var attribute in Attributes)
            {
                switch (attribute.Type)
                {
                    case FileAttributeType.BitDepth:
                        BitDepth = attribute.Value;
                        break;
                    case FileAttributeType.BitRate:
                        BitRate = attribute.Value;
                        break;
                    case FileAttributeType.VariableBitRate:
                        IsVariableBitRate = attribute.Value != 0;
                        break;
                    case FileAttributeType.Length:
                        Length = attribute.Value;
                        break;
                    case FileAttributeType.SampleRate:
                        SampleRate = attribute.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        ///     Gets the number of file <see cref="FileAttribute"/> s.
        /// </summary>
        public int AttributeCount { get; }

        /// <summary>
        ///     Gets the file attributes.
        /// </summary>
        public IReadOnlyCollection<FileAttribute> Attributes { get; }

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.BitDepth"/> attribute.
        /// </summary>
        public int? BitDepth { get; }

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.BitRate"/> attribute.
        /// </summary>
        public int? BitRate { get; }

        /// <summary>
        ///     Gets the file code.
        /// </summary>
        public int Code { get; }

        /// <summary>
        ///     Gets the file extension.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        ///     Gets the file name.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="FileAttributeType.VariableBitRate"/> attribute value indicates a
        ///     file with a variable bit rate.
        /// </summary>
        public bool? IsVariableBitRate { get; }

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.Length"/> attribute.
        /// </summary>
        public int? Length { get; }

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.SampleRate"/> attribute.
        /// </summary>
        public int? SampleRate { get; }

        /// <summary>
        ///     Gets the file size in bytes.
        /// </summary>
        public long Size { get; }
    }
}