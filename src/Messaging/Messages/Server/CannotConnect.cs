// <copyright file="CannotConnect.cs" company="JP Dillingham">
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
    ///     A message indicating an unsuccessful attempt to connect by a peer.
    /// </summary>
    internal sealed class CannotConnect : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CannotConnect"/> class.
        /// </summary>
        /// <param name="token">The unique connection token.</param>
        /// <param name="username">The username of the peer.</param>
        public CannotConnect(int token, string username = null)
        {
            Token = token;
            Username = username;
        }

        /// <summary>
        ///     Gets the unique connection token.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="CannotConnect"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static CannotConnect FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.CannotConnect)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(CannotConnect)} (expected: {(int)MessageCode.Server.CannotConnect}, received: {(int)code})");
            }

            var token = reader.ReadInteger();

            string username = null;

            if (reader.HasMoreData)
            {
                username = reader.ReadString();
            }

            return new CannotConnect(token, username);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            var builder = new MessageBuilder();

            builder
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(Token);

            if (!string.IsNullOrEmpty(Username))
            {
                builder.WriteString(Username);
            }

            return builder.Build();
        }
    }
}