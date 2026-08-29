// <copyright file="NewPassword.cs" company="JP Dillingham">
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
    ///     The command and response to a password change.
    /// </summary>
    internal sealed class NewPassword : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NewPassword"/> class.
        /// </summary>
        /// <param name="password">The new password.</param>
        public NewPassword(string password)
        {
            Password = password;
        }

        /// <summary>
        ///     Gets the new password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="NewPassword"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static NewPassword FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.NewPassword)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(MessageCode.Server.NewPassword)} (expected: {(int)MessageCode.Server.NewPassword}, received: {(int)code})");
            }

            var password = reader.ReadString();

            return new NewPassword(password);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Server.NewPassword)
                .WriteString(Password)
                .Build();
        }
    }
}