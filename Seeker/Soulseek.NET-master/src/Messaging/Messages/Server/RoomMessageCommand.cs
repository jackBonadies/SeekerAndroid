// <copyright file="RoomMessageCommand.cs" company="JP Dillingham">
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
    ///     Sends a message to a chat room.
    /// </summary>
    internal sealed class RoomMessageCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomMessageCommand"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        public RoomMessageCommand(string roomName, string message)
        {
            RoomName = roomName;
            Message = message;
        }

        /// <summary>
        ///     Gets the message to send.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the name of the room to which the message is to be sent.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.SayInChatRoom)
                .WriteString(RoomName)
                .WriteString(Message)
                .Build();
        }
    }
}