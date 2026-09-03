// <copyright file="ServerInfo.cs" company="JP Dillingham">
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
    ///     Session information from the server.
    /// </summary>
    /// <remarks>
    ///     <para>Values are null until the client is connected and logged in.</para>
    ///     <para>
    ///         The <see cref="ParentMinSpeed"/> and <see cref="ParentSpeedRatio"/> values serve an unknown purpose, but are most
    ///         likely related to the distributed network somehow.
    ///     </para>
    /// </remarks>
    public class ServerInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ServerInfo"/> class.
        /// </summary>
        /// <param name="parentMinSpeed">The ParentMinSpeed value.</param>
        /// <param name="parentSpeedRatio">The ParentSpeedRatio value.</param>
        /// <param name="wishlistInterval">The interval for wishlist searches, in seconds.</param>
        /// <param name="isSupporter">
        ///     A value indicating whether the logged in user has ever purchased privileges, regardless of whether the user has active
        ///     privileges at the present moment.
        /// </param>
        public ServerInfo(
            int? parentMinSpeed = null,
            int? parentSpeedRatio = null,
            int? wishlistInterval = null,
            bool? isSupporter = null)
        {
            ParentMinSpeed = parentMinSpeed;
            ParentSpeedRatio = parentSpeedRatio;
            WishlistInterval = wishlistInterval;
            IsSupporter = isSupporter;
        }

        /// <summary>
        ///     Gets a value indicating whether the logged in user has ever purchased privileges, regardless of whether the user has active
        ///     privileges at the present moment.
        /// </summary>
        public bool? IsSupporter { get; }

        /// <summary>
        ///     Gets the ParentMinSpeed value.
        /// </summary>
        public int? ParentMinSpeed { get; }

        /// <summary>
        ///     Gets the ParentSpeedRatio value.
        /// </summary>
        public int? ParentSpeedRatio { get; }

        /// <summary>
        ///     Gets the interval for wishlist searches, in seconds.
        /// </summary>
        public int? WishlistInterval { get; }

        /// <summary>
        ///     Creates a clone of this instance with the specified substitutions.
        /// </summary>
        /// <param name="parentMinSpeed">The ParentMinSpeed value.</param>
        /// <param name="parentSpeedRatio">The ParentSpeedRatio value.</param>
        /// <param name="wishlistInterval">The interval for wishlist searches, in seconds.</param>
        /// <param name="isSupporter">
        ///     A value indicating whether the logged in user has ever purchased privileges, regardless of whether the user has active
        ///     privileges at the present moment.
        /// </param>
        /// <returns>The cloned instance.</returns>
        internal ServerInfo With(
            int? parentMinSpeed = null,
            int? parentSpeedRatio = null,
            int? wishlistInterval = null,
            bool? isSupporter = null)
        {
            return new ServerInfo(
                parentMinSpeed: parentMinSpeed ?? ParentMinSpeed,
                parentSpeedRatio: parentSpeedRatio ?? ParentSpeedRatio,
                wishlistInterval: wishlistInterval ?? WishlistInterval,
                isSupporter: isSupporter ?? IsSupporter);
        }
    }
}