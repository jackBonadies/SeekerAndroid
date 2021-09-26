// <copyright file="NetInfoNotification.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>
    ///     An incoming list of available distributed parent candidates.
    /// </summary>
    internal sealed class NetInfoNotification : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NetInfoNotification"/> class.
        /// </summary>
        /// <param name="parentCount">The number of parent candidates.</param>
        /// <param name="parents">The list of parent candidates.</param>
        public NetInfoNotification(int parentCount, IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            ParentCount = parentCount;
            Parents = parents.ToList().AsReadOnly();
        }

        /// <summary>
        ///     Gets the number of parent candidates.
        /// </summary>
        public int ParentCount { get; }

        /// <summary>
        ///     Gets the list of parent candidates.
        /// </summary>
        public IReadOnlyCollection<(string Username, IPAddress IPAddress, int Port)> Parents { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="NetInfoNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static NetInfoNotification FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.NetInfo)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(NetInfoNotification)} (expected: {(int)MessageCode.Server.NetInfo}, received: {(int)code})");
            }

            var parentCount = reader.ReadInteger();
            var parents = new List<(string Username, IPAddress IPAddress, int Port)>();

            for (int i = 0; i < parentCount; i++)
            {
                var username = reader.ReadString();

                var ipBytes = reader.ReadBytes(4);
                Array.Reverse(ipBytes);
                var ipAddress = new IPAddress(ipBytes);

                var port = reader.ReadInteger();

                parents.Add((username, ipAddress, port));
            }

            return new NetInfoNotification(parentCount, parents.AsReadOnly());
        }
    }
}