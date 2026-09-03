// <copyright file="LikedInterestAddCommand.cs" company="JP Dillingham">
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
    using Soulseek.Messaging;

    /// <summary>
    ///     Adds an interest to the user's list of liked interests.
    /// </summary>
    internal class LikedInterestAddCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LikedInterestAddCommand"/> class.
        /// </summary>
        /// <param name="interest">The liked interest to add.</param>
        public LikedInterestAddCommand(string interest)
        {
            Interest = interest;
        }

        /// <summary>
        ///     Gets the liked interest to add.
        /// </summary>
        public string Interest { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.LikedInterestAdd)
                .WriteString(Interest)
                .Build();
        }
    }
}
