// <copyright file="PrivateRoomToggle.cs" company="JP Dillingham">
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
    ///     The command and response to a request to toggle receipt of private room invitations.
    /// </summary>
    internal sealed class PrivateRoomToggle : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivateRoomToggle"/> class.
        /// </summary>
        /// <param name="acceptInvitations">A value indicating whether to accept private room invitations.</param>
        public PrivateRoomToggle(bool acceptInvitations)
        {
            AcceptInvitations = acceptInvitations;
        }

        /// <summary>
        ///     Gets a value indicating whether to accept private room invitations.
        /// </summary>
        public bool AcceptInvitations { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PrivateRoomToggle"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PrivateRoomToggle FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.PrivateRoomToggle)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(MessageCode.Server.PrivateRoomToggle)} (expected: {(int)MessageCode.Server.PrivateRoomToggle}, received: {(int)code})");
            }

            var acceptInvitations = reader.ReadByte() > 0;

            return new PrivateRoomToggle(acceptInvitations);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomToggle)
                .WriteByte((byte)(AcceptInvitations ? 1 : 0))
                .Build();
        }
    }
}