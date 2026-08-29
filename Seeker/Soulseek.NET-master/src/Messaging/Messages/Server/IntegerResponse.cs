// <copyright file="IntegerResponse.cs" company="JP Dillingham">
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

    /// <summary>
    ///     A simple integer response.
    /// </summary>
    internal sealed class IntegerResponse : IIncomingMessage
    {
        /// <summary>
        ///     Creates a new instance of <see cref="IntegerResponse"/> with message code <typeparamref name="T"/> from the
        ///     specified <paramref name="bytes"/>.
        /// </summary>
        /// <typeparam name="T">The expected message code type.</typeparam>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static int FromByteArray<T>(byte[] bytes)
            where T : Enum
        {
            var reader = new MessageReader<T>(bytes);
            return reader.ReadInteger();
        }
    }
}