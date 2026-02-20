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
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    ///     A response to a file search.
    /// </summary>
    [System.Serializable]
    public class SearchResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="freeUploadSlots">The number of free upload slots for the peer.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="fileList">The file list.</param>
        /// <param name="lockedFileList">The optional locked file list.</param>
        public SearchResponse(string username, int token, int freeUploadSlots, int uploadSpeed, long queueLength, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
        {
            Username = username;
            Token = token;
            FreeUploadSlots = freeUploadSlots;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;

            Files = (fileList?.ToList() ?? new List<File>()).AsReadOnly();
            FileCount = Files.Count;

            LockedFiles = (lockedFileList?.ToList() ?? new List<File>()).AsReadOnly();
            LockedFileCount = LockedFiles.Count;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            if (this.Username  != ((SearchResponse)(obj)).Username)
            {
                return false;
            }
            if(this.Files.Count != 0 && ((SearchResponse)(obj)).Files.Count != 0)
            {
                return this.Files.First().Filename == ((SearchResponse)(obj)).Files.First().Filename;
            }
            else
            {
                return true;
            }
        }

        public string cachedDominantFileType = null;
        public double cachedCalcBitRate = double.NaN;


        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="searchResponse">An existing instance from which to copy properties.</param>
        /// <param name="fileList">The file list with which to replace the existing file list.</param>
        /// <param name="lockedFileList">The optional locked file list with which to replace the existing locked file list.</param>
        internal SearchResponse(SearchResponse searchResponse, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
            : this(searchResponse.Username, searchResponse.Token, searchResponse.FreeUploadSlots, searchResponse.UploadSpeed, searchResponse.QueueLength, fileList, lockedFileList)
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
        ///     Gets the number of free upload slots for the peer.
        /// </summary>
        public int FreeUploadSlots { get; }

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
        public long QueueLength { get; }

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
    }
}