// <copyright file="UserStatistics.cs" company="JP Dillingham">
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
    /// <summary>
    ///     User statistics.
    /// </summary>
    public class UserStatistics
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatistics"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="averageSpeed">The average upload speed of the user.</param>
        /// <param name="uploadCount">The number of uploads tracked by the server for this user.</param>
        /// <param name="fileCount">The number of files shared by the user.</param>
        /// <param name="directoryCount">The number of directories shared by the user.</param>
        public UserStatistics(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            Username = username;
            AverageSpeed = averageSpeed;
            UploadCount = uploadCount;
            FileCount = fileCount;
            DirectoryCount = directoryCount;
        }

        /// <summary>
        ///     Gets the average upload speed of the user.
        /// </summary>
        public int AverageSpeed { get; }

        /// <summary>
        ///     Gets the number of directories shared by the user.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the number of files shared by the user.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the number of uploads tracked by the server for this user.
        /// </summary>
        public long UploadCount { get; }

        /// <summary>
        ///     Gets the username of the user.
        /// </summary>
        public string Username { get; }
    }
}