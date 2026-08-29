// <copyright file="RawSearchResponse.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.IO;

    /// <summary>
    ///     A raw response to a file search, presented as a stream of binary data.
    /// </summary>
    /// <remarks>
    ///     This is a hack to simulate a discriminated union.
    /// </remarks>
    public class RawSearchResponse : SearchResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RawSearchResponse"/> class.
        /// </summary>
        /// <remarks>
        ///     The input stream will be disposed after the response is written.
        /// </remarks>
        /// <param name="length">The length of the response, in bytes.</param>
        /// <param name="stream">The raw input stream.</param>
        public RawSearchResponse(long length, Stream stream)
            : base(string.Empty, 0, false, 0, 0, null)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "The response length must be greater than zero");
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "The specified input stream is null");
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("The specified input stream can not be read", nameof(stream));
            }

            Length = length;
            Stream = stream;
        }

        /// <summary>
        ///     Gets the length of the response, in bytes.
        /// </summary>
        public long Length { get; }

        /// <summary>
        ///     Gets the raw input stream providing the response.
        /// </summary>
        public Stream Stream { get; }
    }
}
