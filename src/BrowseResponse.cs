// <copyright file="BrowseResponse.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     A response to a peer browse request.
    /// </summary>
    public class BrowseResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseResponse"/> class.
        /// </summary>
        /// <param name="directoryList">The optional directory list.</param>
        /// <param name="lockedDirectoryList">The optional locked directory list.</param>
        public BrowseResponse(IEnumerable<Directory> directoryList = null, IEnumerable<Directory> lockedDirectoryList = null)
        {
            Directories = (directoryList?.ToList() ?? new List<Directory>()).AsReadOnly();
            DirectoryCount = Directories.Count;

            LockedDirectories = (lockedDirectoryList?.ToList() ?? new List<Directory>()).AsReadOnly();
            LockedDirectoryCount = LockedDirectories.Count;
        }

        /// <summary>
        ///     Gets the list of directories.
        /// </summary>
        public IReadOnlyCollection<Directory> Directories { get; }

        /// <summary>
        ///     Gets the number of directories.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the list of locked directories.
        /// </summary>
        public IReadOnlyCollection<Directory> LockedDirectories { get; }

        /// <summary>
        ///     Gets the number of locked directories.
        /// </summary>
        public int LockedDirectoryCount { get; }

        /// <summary>
        ///     Serializes the response to the raw byte array sent over the network.
        /// </summary>
        /// <returns>The serialized response.</returns>
        public byte[] ToByteArray()
        {
            return BrowseResponseFactory.ToByteArray(this);
        }
    }
}