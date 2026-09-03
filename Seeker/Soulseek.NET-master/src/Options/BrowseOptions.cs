// <copyright file="BrowseOptions.cs" company="JP Dillingham">
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

    /// <summary>
    ///     Options for the browse operation.
    /// </summary>
    public class BrowseOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseOptions"/> class.
        /// </summary>
        /// <param name="responseTimeout">The timeout for the response, in milliseconds.</param>
        /// <param name="progressUpdated">The Action to invoke when the browse response receives data.</param>
        public BrowseOptions(
            int responseTimeout = 60000,
            Action<(string Username, long BytesTransferred, long BytesRemaining, double PercentComplete, long Size)> progressUpdated = null)
        {
            ResponseTimeout = responseTimeout;
            ProgressUpdated = progressUpdated;
        }

        /// <summary>
        ///     Gets the Action to invoke when the browse response receives data.
        /// </summary>
        public Action<(string Username, long BytesTransferred, long BytesRemaining, double PercentComplete, long Size)> ProgressUpdated { get; }

        /// <summary>
        ///     Gets the timeout for the response, in milliseconds. (Default = 60000).
        /// </summary>
        public int ResponseTimeout { get; }
    }
}