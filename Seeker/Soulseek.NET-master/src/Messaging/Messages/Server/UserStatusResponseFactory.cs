// <copyright file="UserStatusResponseFactory.cs" company="JP Dillingham">
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
    ///     The response to a peer info request.
    /// </summary>
    internal static class UserStatusResponseFactory
    {
        /// <summary>
        ///     Creates a new instance of <see cref="UserStatusResponseFactory"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static UserStatus FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GetStatus)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserStatusResponseFactory)} (expected: {(int)MessageCode.Server.GetStatus}, received: {(int)code})");
            }

            var username = reader.ReadString();
            var presence = (UserPresence)reader.ReadInteger();
            var privileged = reader.ReadByte() > 0;

            return new UserStatus(username, presence, privileged);
        }
    }
}