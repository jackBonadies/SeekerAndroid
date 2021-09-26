// <copyright file="DistributedBranchRoot.cs" company="JP Dillingham">
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
    ///     Informs distributed children of the current branch root.
    /// </summary>
    internal sealed class DistributedBranchRoot : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedBranchRoot"/> class.
        /// </summary>
        /// <param name="username">The username of the current branch root.</param>
        public DistributedBranchRoot(string username)
        {
            Username = username;
        }

        /// <summary>
        ///     Gets the username of the current branch root.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="DistributedBranchRoot"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static DistributedBranchRoot FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Distributed>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Distributed.BranchRoot)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(DistributedBranchRoot)} (expected: {(int)MessageCode.Distributed.BranchRoot}, received: {(int)code})");
            }

            var username = reader.ReadString();

            return new DistributedBranchRoot(username);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchRoot)
                .WriteString(Username)
                .Build();
        }
    }
}