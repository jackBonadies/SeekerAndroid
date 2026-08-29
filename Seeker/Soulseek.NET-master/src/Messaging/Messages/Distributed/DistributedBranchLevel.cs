// <copyright file="DistributedBranchLevel.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Informs distributed children of the current branch level.
    /// </summary>
    internal sealed class DistributedBranchLevel : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedBranchLevel"/> class.
        /// </summary>
        /// <param name="level">The current branch level.</param>
        public DistributedBranchLevel(int level)
        {
            Level = level;
        }

        /// <summary>
        ///     Gets the current branch level.
        /// </summary>
        public int Level { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="DistributedBranchLevel"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static DistributedBranchLevel FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Distributed>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Distributed.BranchLevel)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(DistributedBranchLevel)} (expected: {(int)MessageCode.Distributed.BranchLevel}, received: {(int)code})");
            }

            var level = reader.ReadInteger();

            return new DistributedBranchLevel(level);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(Level)
                .Build();
        }
    }
}