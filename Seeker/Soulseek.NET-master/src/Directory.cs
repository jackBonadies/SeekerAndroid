// <copyright file="Directory.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     Copyright (c) 2021-2026 Jack Bonadies
//     Modified: added a Latin-1 decoding flag
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A file directory within a peer's shared files.
    /// </summary>
    public class Directory
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Directory"/> class.
        /// </summary>
        /// <param name="name">The directory name.</param>
        /// <param name="fileList">The optional list of <see cref="File"/> s.</param>
        /// <param name="decodedViaLatin1">Whether the directory name was decoded as ISO-8859-1 rather than UTF-8.</param>
        public Directory(string name, IEnumerable<File> fileList = null, bool decodedViaLatin1 = false)
        {
            Name = name;

            Files = (fileList?.ToList() ?? new List<File>()).AsReadOnly();
            FileCount = Files.Count;
            DecodedViaLatin1 = decodedViaLatin1;
        }

        /// <summary>
        ///     Gets the directory name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets a value indicating whether the directory name was decoded as ISO-8859-1 rather than UTF-8.
        /// </summary>
        public bool DecodedViaLatin1 { get; }

        /// <summary>
        ///     Gets the number of files within the directory.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the collection of files contained within the directory.
        /// </summary>
        public IReadOnlyCollection<File> Files { get; }
    }
}