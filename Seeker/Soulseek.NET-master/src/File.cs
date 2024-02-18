﻿// <copyright file="File.cs" company="JP Dillingham">
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
    [System.Serializable]
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
        public File(int code, string filename, long size, string extension, IEnumerable<FileAttribute> attributeList = null, bool isLatin1Decoded = false, bool isDirectoryLatin1Decoded = false)
        {
            Code = code;
            Filename = filename;
            Size = size;
            Extension = extension;

            Attributes = (attributeList?.ToList() ?? new List<FileAttribute>()).AsReadOnly();
            AttributeCount = Attributes.Count;
            IsLatin1Decoded = isLatin1Decoded;
            IsDirectoryLatin1Decoded = isDirectoryLatin1Decoded;

        }

        //public File DeepCopy()
        //{
        //    File f = new File(this.Code,this.Filename,this.Size,this.Extension,this.Attributes);
        //    return f;
        //}

        //for serializer
        private File()
        {

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
        public int? BitDepth => GetAttributeValue(FileAttributeType.BitDepth);

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.BitRate"/> attribute.
        /// </summary>
        public int? BitRate => GetAttributeValue(FileAttributeType.BitRate);


        /// <summary>
        ///     Gets the file code.
        /// </summary>
        [field: System.NonSerialized]
        public bool IsLatin1Decoded { get; }

        /// <summary>
        ///     Gets the file code. If True it is.  If False, either no or do not know (consult Directory).
        /// </summary>
        [field: System.NonSerialized]
        public bool IsDirectoryLatin1Decoded { get; }

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
        ///     This will be decoded as the fullname including directory if from search response.
        ///     This will be decoded as just the filename if from browse response.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets a value indicating whether the <see cref="FileAttributeType.VariableBitRate"/> attribute value indicates a
        ///     file with a variable bit rate.
        /// </summary>
        public bool? IsVariableBitRate
        {
            get
            {
                var val = GetAttributeValue(FileAttributeType.VariableBitRate);
                return val == null ? (bool?)null : val != 0;
            }
        }

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.Length"/> attribute.
        /// </summary>
        public int? Length => GetAttributeValue(FileAttributeType.Length);

        /// <summary>
        ///     Gets the value of the <see cref="FileAttributeType.SampleRate"/> attribute.
        /// </summary>
        public int? SampleRate => GetAttributeValue(FileAttributeType.SampleRate);

        /// <summary>
        ///     Gets the file size in bytes.
        /// </summary>
        public long Size { get; }

        /// <summary>
        ///     Returns the value of the specified attribute <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The attribute to return.</param>
        /// <returns>The value of the specified attribute.</returns>
        public int? GetAttributeValue(FileAttributeType type)
        {
            return Attributes.FirstOrDefault(a => a.Type == type)?.Value;
        }
    }
}