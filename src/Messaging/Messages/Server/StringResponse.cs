// <copyright file="StringResponse.cs" company="JP Dillingham">
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

    /// <summary>
    ///     A simple string response.
    /// </summary>
    internal sealed class StringResponse : IIncomingMessage
    {
        /// <summary>
        ///     Creates a new instance of <see cref="StringResponse"/> with message code <typeparamref name="T"/> from the
        ///     specified <paramref name="bytes"/>.
        /// </summary>
        /// <typeparam name="T">The expected message code type.</typeparam>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static string FromByteArray<T>(byte[] bytes)
            where T : Enum
        {
            var reader = new MessageReader<T>(bytes);
            return reader.ReadString();
        }
    }
}