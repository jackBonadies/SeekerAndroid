// <copyright file="SendUploadSpeedCommand.cs" company="JP Dillingham">
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
    ///     Informs the server of the most recent upload transfer speed.
    /// </summary>
    internal sealed class SendUploadSpeedCommand : IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SendUploadSpeedCommand"/> class.
        /// </summary>
        /// <param name="speed">The most recent upload transfer speed, in bytes per second.</param>
        public SendUploadSpeedCommand(int speed)
        {
            Speed = speed;
        }

        /// <summary>
        ///     Gets the most recent upload transfer speed, in bytes per second.
        /// </summary>
        public int Speed { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.SendUploadSpeed)
                .WriteInteger(Speed)
                .Build();
        }
    }
}