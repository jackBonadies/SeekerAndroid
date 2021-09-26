// <copyright file="DistributedChildDepth.cs" company="JP Dillingham">
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
    ///     Informs distributed parents of a child's child depth.
    /// </summary>
    internal sealed class DistributedChildDepth : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedChildDepth"/> class.
        /// </summary>
        /// <param name="depth">The current depth of the child.</param>
        public DistributedChildDepth(int depth)
        {
            Depth = depth;
        }

        /// <summary>
        ///     Gets the current depth of the child.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="DistributedChildDepth"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static DistributedChildDepth FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Distributed>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Distributed.ChildDepth)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(DistributedChildDepth)} (expected: {(int)MessageCode.Distributed.ChildDepth}, received: {(int)code})");
            }

            var depth = reader.ReadInteger();

            return new DistributedChildDepth(depth);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(Depth)
                .Build();
        }
    }
}