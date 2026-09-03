// <copyright file="ParentsIPCommand.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System;
    using System.Net;

    /// <summary>
    ///     Informs the server of the IP address of the current distributed parent.
    /// </summary>
    internal sealed class ParentsIPCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ParentsIPCommand"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address of the current distributed parent, or null if none is connected.</param>
        public ParentsIPCommand(IPAddress ipAddress = null)
        {
            IPAddress = ipAddress;
        }

        /// <summary>
        ///     Gets the IP address of the current distributed parent.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            byte[] ipBytes = Array.Empty<byte>();

            if (IPAddress != default)
            {
                ipBytes = IPAddress.GetAddressBytes();
                Array.Reverse(ipBytes);
            }

            return new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentsIP)
                .WriteBytes(ipBytes)
                .Build();
        }
    }
}