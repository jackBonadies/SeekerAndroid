// <copyright file="ServerInfo.cs" company="JP Dillingham">
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
        /// <param name="wishlistInterval">The interval for wishlist searches, in miliseconds.</param>
        public ServerInfo(int? parentMinSpeed, int? parentSpeedRatio, int? wishlistInterval)
        {
            ParentMinSpeed = parentMinSpeed;
            ParentSpeedRatio = parentSpeedRatio;
            WishlistInterval = wishlistInterval;
        }

        /// <summary>
        ///     Gets the ParentMinSpeed value.
        /// </summary>
        public int? ParentMinSpeed { get; }

        /// <summary>
        ///     Gets the ParentSpeedRatio value.
        /// </summary>
        public int? ParentSpeedRatio { get; }

        /// <summary>
        ///     Gets the interval for wishlist searches, in miliseconds.
        /// </summary>
        public int? WishlistInterval { get; }
    }
}