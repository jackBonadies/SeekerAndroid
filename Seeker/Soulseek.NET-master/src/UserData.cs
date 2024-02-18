﻿// <copyright file="UserData.cs" company="JP Dillingham">
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
    /// Like Userdata but has an additional flag for role.
    /// </summary>
    [System.Serializable]
    public class ChatroomUserData : UserData
    {


        public ChatroomUserData(string username, UserPresence status, int averageSpeed, long downloadCount, int fileCount, int directoryCount, string countryCode, int? slotsFree = null) : base(username, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode, slotsFree)
        {

        }

        /// <summary>
        ///     Gets or sets chatroom user role.
        /// </summary>
        public Soulseek.UserRole ChatroomUserRole { get; set;}

    }


    /// <summary>
    ///     User data.
    /// </summary>
    [System.Serializable]
    public class UserData
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserData"/> class.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="status">The status of the user.</param>
        /// <param name="averageSpeed">The average upload speed of the user.</param>
        /// <param name="downloadCount">The number of active user downloads.</param>
        /// <param name="fileCount">The number of files shared by the user.</param>
        /// <param name="directoryCount">The number of directories shared by the user.</param>
        /// <param name="countryCode">The user's country code.</param>
        /// <param name="slotsFree">The number of the user's free download slots, if provided.</param>
        public UserData(string username, UserPresence status, int averageSpeed, long downloadCount, int fileCount, int directoryCount, string countryCode, int? slotsFree = null)
        {
            Username = username;
            Status = status;
            AverageSpeed = averageSpeed;
            DownloadCount = downloadCount;
            FileCount = fileCount;
            DirectoryCount = directoryCount;
            SlotsFree = slotsFree;
            CountryCode = countryCode;
        }

        /// <summary>
        ///     Gets the average upload speed of the user.
        /// </summary>
        public int AverageSpeed { get; protected set;}

        /// <summary>
        ///     Gets the user's country code.
        /// </summary>
        public string CountryCode { get; protected set; }

        /// <summary>
        ///     Gets the number of directories shared by the user.
        /// </summary>
        public int DirectoryCount { get; protected set; }

        /// <summary>
        ///     Gets the number of active user downloads.
        /// </summary>
        public long DownloadCount { get; protected set; }

        /// <summary>
        ///     Gets the number of files shared by the user.
        /// </summary>
        public int FileCount { get; protected set; }

        /// <summary>
        ///     Gets the number of the user's free download slots, if provided.
        /// </summary>
        public int? SlotsFree { get; protected set; }

        /// <summary>
        ///     Gets the status of the user (0 = offline, 1 = away, 2 = online).
        /// </summary>
        public UserPresence Status { get; set; }

        /// <summary>
        ///     Gets the username of the user.
        /// </summary>
        public string Username { get; protected set; }
    }
}