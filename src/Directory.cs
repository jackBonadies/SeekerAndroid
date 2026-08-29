// <copyright file="Directory.cs" company="JP Dillingham">
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
        public Directory(string name, IEnumerable<File> fileList = null)
        {
            Name = name;

            Files = (fileList?.ToList() ?? new List<File>()).AsReadOnly();
            FileCount = Files.Count;
        }

        /// <summary>
        ///     Gets the directory name.
        /// </summary>
        public string Name { get; }

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