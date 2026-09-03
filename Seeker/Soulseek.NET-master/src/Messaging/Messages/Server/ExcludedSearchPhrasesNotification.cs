// <copyright file="ExcludedSearchPhrasesNotification.cs" company="JP Dillingham">
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
    using System.Collections.Generic;

    /// <summary>
    ///     A list of excluded ("banned") search phrases.
    /// </summary>
    internal sealed class ExcludedSearchPhrasesNotification : IIncomingMessage
    {
        /// <summary>
        ///     Creates a new instance of <see cref="ExcludedSearchPhrasesNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static IReadOnlyCollection<string> FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.ExcludedSearchPhrases)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(ExcludedSearchPhrasesNotification)} (expected: {(int)MessageCode.Server.ExcludedSearchPhrases}, received: {(int)code}");
            }

            var count = reader.ReadInteger();
            var list = new List<string>();

            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }

            return list.AsReadOnly();
        }
    }
}