// <copyright file="DownloadFailedEventArgs.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Event arguments for events raised when a user reports that an upload has failed.
    /// </summary>
    public class DownloadFailedEventArgs : UserEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadFailedEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username associated with the event.</param>
        /// <param name="filename">The filename associated with the event.</param>
        public DownloadFailedEventArgs(string username, string filename)
            : base(username)
        {
            Filename = filename;
        }

        /// <summary>
        ///     Gets the filename associated with the event.
        /// </summary>
        public string Filename { get; }
    }
}