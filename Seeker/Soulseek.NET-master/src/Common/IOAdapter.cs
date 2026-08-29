// <copyright file="IOAdapter.cs" company="JP Dillingham">
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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    ///     A testable adapter around System.IO.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class IOAdapter : IIOAdapter
    {
        /// <summary>
        ///     Returns true if the given path exists, false otherwse.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>A value indicating whether the given path exists.</returns>
        public bool Exists(string path) => System.IO.File.Exists(path);

        /// <summary>
        ///     Creates a new FileStream from the given <paramref name="path"/> using the specified <paramref name="mode"/> and <paramref name="access"/>.
        /// </summary>
        /// <param name="path">The path to open.</param>
        /// <param name="mode">The file mode.</param>
        /// <param name="access">The file access level.</param>
        /// <param name="share">The file sharing access.</param>
        /// <returns>The created FileStream.</returns>
        public FileStream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share)
            => new FileStream(path, mode, access, share);

        /// <summary>
        ///     Returns a new FileInfo object from the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path for which to retrieve info.</param>
        /// <returns>The created FileInfo.</returns>
        public FileInfo GetFileInfo(string path)
            => new FileInfo(path);
    }
}
