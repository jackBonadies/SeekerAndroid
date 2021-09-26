// <copyright file="HaveNoParentsCommand.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Informs the server that we have no distributed parent.
    /// </summary>
    internal sealed class HaveNoParentsCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HaveNoParentsCommand"/> class.
        /// </summary>
        /// <param name="haveNoParents">A value indicating whether a distributed parent connection is needed.</param>
        public HaveNoParentsCommand(bool haveNoParents)
        {
            HaveNoParents = haveNoParents;
        }

        /// <summary>
        ///     Gets a value indicating whether a distributed parent connections is needed.
        /// </summary>
        public bool HaveNoParents { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.HaveNoParents)
                .WriteByte((byte)(HaveNoParents ? 1 : 0))
                .Build();
        }
    }
}