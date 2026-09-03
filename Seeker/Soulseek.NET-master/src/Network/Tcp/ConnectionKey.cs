// <copyright file="ConnectionKey.cs" company="JP Dillingham">
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

namespace Soulseek.Network.Tcp
{
    using System;
    using System.Net;

    /// <summary>
    ///     Uniquely identifies a <see cref="Connection"/> instance.
    /// </summary>
    internal sealed class ConnectionKey : IEquatable<ConnectionKey>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionKey"/> class.
        /// </summary>
        /// <param name="ipEndPoint">The IP endpoint of the connection.</param>
        public ConnectionKey(IPEndPoint ipEndPoint)
            : this(null, ipEndPoint)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionKey"/> class.
        /// </summary>
        /// <param name="username">The username associated with the connection.</param>
        /// <param name="ipEndPoint">The IP endpoint of the connection.</param>
        public ConnectionKey(string username, IPEndPoint ipEndPoint)
        {
            Username = username;
            IPEndPoint = ipEndPoint;
        }

        /// <summary>
        ///     Gets the IP endpoint of the connection.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Gets the username associated with the connection.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        ///     Compares the specified <paramref name="other"/> ConnectionKey to this instance.
        /// </summary>
        /// <param name="other">The ConnectionKey to which to compare.</param>
        /// <returns>A value indicating whether the specified ConnectionKey is equal to this instance.</returns>
        public bool Equals(ConnectionKey other)
        {
            return GetHashCode() == other?.GetHashCode();
        }

        /// <summary>
        ///     Compares the specified <paramref name="obj"/> to this instance.
        /// </summary>
        /// <param name="obj">The object to which to compare.</param>
        /// <returns>A value indicating whether the specified object is equal to this instance.</returns>
        public override bool Equals(object obj)
        {
            try
            {
                return Equals((ConnectionKey)obj);
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Returns the hash code of this instance.
        /// </summary>
        /// <returns>The hash code of this instance.</returns>
        public override int GetHashCode()
        {
            var str = $"{Username}:{IPEndPoint?.Address}:{IPEndPoint?.Port}";
#if NETSTANDARD2_0
            return str.GetHashCode();
#else
            return str.GetHashCode(StringComparison.CurrentCulture);
#endif
        }
    }
}