// <copyright file="AcknowledgePrivateMessageCommand.cs" company="JP Dillingham">
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
    ///     Acknowledges the reciept of a private message.
    /// </summary>
    internal sealed class AcknowledgePrivateMessageCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AcknowledgePrivateMessageCommand"/> class.
        /// </summary>
        /// <param name="id">The id of the private message to acknowledge.</param>
        public AcknowledgePrivateMessageCommand(int id)
        {
            Id = id;
        }

        /// <summary>
        ///     Gets the id of the private message to acknowledge.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.AcknowledgePrivateMessage)
                .WriteInteger(Id)
                .Build();
        }
    }
}