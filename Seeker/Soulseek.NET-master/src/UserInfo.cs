// <copyright file="UserInfo.cs" company="JP Dillingham">
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
    ///     The response to a user info request.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfo"/> class.
        /// </summary>
        /// <param name="description">The peer's description.</param>
        /// <param name="picture">If configured, the picture data.</param>
        /// <param name="uploadSlots">The number of configured upload slots.</param>
        /// <param name="queueLength">The current queue length.</param>
        /// <param name="hasFreeUploadSlot">A value indicating whether an upload slot is free.</param>
        public UserInfo(string description, int uploadSlots, int queueLength, bool hasFreeUploadSlot, byte[] picture = null)
        {
            Description = description;
            HasPicture = picture != null;
            Picture = picture;
            UploadSlots = uploadSlots;
            QueueLength = queueLength;
            HasFreeUploadSlot = hasFreeUploadSlot;
        }

        /// <summary>
        ///     Gets the user's description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     Gets a value indicating whether an upload slot is free.
        /// </summary>
        public bool HasFreeUploadSlot { get; }

        /// <summary>
        ///     Gets a value indicating whether a picture has been configured.
        /// </summary>
        public bool HasPicture { get; }

        /// <summary>
        ///     Gets the picture data, if configured.
        /// </summary>
        public byte[] Picture { get; }

        /// <summary>
        ///     Gets the current queue length.
        /// </summary>
        public int QueueLength { get; }

        /// <summary>
        ///     Gets the number of configured upload slots.
        /// </summary>
        public int UploadSlots { get; }
    }
}