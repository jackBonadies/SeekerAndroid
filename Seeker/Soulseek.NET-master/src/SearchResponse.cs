// <copyright file="SearchResponse.cs" company="JP Dillingham">
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
    ///     A response to a file search.
    /// </summary>
    public class SearchResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="hasFreeUploadSlot">A value indicating whether the peer has a free upload slot.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="fileList">The file list.</param>
        /// <param name="lockedFileList">The optional locked file list.</param>
        public SearchResponse(string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
        {
            Username = username;
            Token = token;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;

            HasFreeUploadSlot = hasFreeUploadSlot;

            Files = (fileList?.ToList() ?? new List<File>()).AsReadOnly();
            FileCount = Files.Count;

            LockedFiles = (lockedFileList?.ToList() ?? new List<File>()).AsReadOnly();
            LockedFileCount = LockedFiles.Count;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="searchResponse">An existing instance from which to copy properties.</param>
        /// <param name="fileList">The file list with which to replace the existing file list.</param>
        /// <param name="lockedFileList">The optional locked file list with which to replace the existing locked file list.</param>
        internal SearchResponse(SearchResponse searchResponse, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
            : this(searchResponse.Username, searchResponse.Token, hasFreeUploadSlot: searchResponse.HasFreeUploadSlot, searchResponse.UploadSpeed, searchResponse.QueueLength, fileList, lockedFileList)
        {
        }

        /// <summary>
        ///     Gets the number of files contained within the result, as counted by the original response from the peer and prior
        ///     to filtering. For the filtered count, check the length of <see cref="Files"/>.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the list of files.
        /// </summary>
        public IReadOnlyCollection<File> Files { get; }

        /// <summary>
        ///     Gets a value indicating whether the peer has a free upload slot.
        /// </summary>
        public bool HasFreeUploadSlot { get; }

        /// <summary>
        ///     Gets the number of files contained within the result, as counted by the original response from the peer and prior
        ///     to filtering. For the filtered count, check the length of <see cref="LockedFiles"/>.
        /// </summary>
        public int LockedFileCount { get; }

        /// <summary>
        ///     Gets the list of locked files.
        /// </summary>
        public IReadOnlyCollection<File> LockedFiles { get; }

        /// <summary>
        ///     Gets the length of the peer's upload queue.
        /// </summary>
        public int QueueLength { get; }

        /// <summary>
        ///     Gets the unique search token.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the upload speed of the peer.
        /// </summary>
        public int UploadSpeed { get; }

        /// <summary>
        ///     Gets the username of the responding peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Serializes the response to the raw byte array sent over the network.
        /// </summary>
        /// <returns>The serialized response.</returns>
        public byte[] ToByteArray()
        {
            return SearchResponseFactory.ToByteArray(this);
        }
    }
}