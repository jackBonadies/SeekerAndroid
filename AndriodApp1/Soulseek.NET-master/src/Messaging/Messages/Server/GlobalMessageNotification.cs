// <copyright file="GlobalMessageNotification.cs" company="JP Dillingham">
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
    ///     An incoming global message.
    /// </summary>
    internal sealed class GlobalMessageNotification : IIncomingMessage
    {
        /// <summary>
        ///     Creates a new instance of <see cref="GlobalMessageNotification"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static string FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.GlobalAdminMessage)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(GlobalMessageNotification)} (expected: {(int)MessageCode.Server.GlobalAdminMessage}, received: {(int)code})");
            }

            var msg = reader.ReadString();

            return msg;
        }
    }
}