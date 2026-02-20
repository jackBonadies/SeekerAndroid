// <copyright file="UserInfoResponseFactory.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using Soulseek.Messaging;

    /// <summary>
    ///     The response to a user info request.
    /// </summary>
    internal static class UserInfoResponseFactory
    {
        /// <summary>
        ///     Creates a new instance of <see cref="UserInfo"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static UserInfo FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.InfoResponse)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UserInfo)} (expected: {(int)MessageCode.Peer.InfoResponse}, received: {(int)code})");
            }

            var description = reader.ReadString();
            var hasPicture = reader.ReadByte() > 0;
            byte[] picture = null;

            if (hasPicture)
            {
                var pictureLen = reader.ReadInteger();
                picture = reader.ReadBytes(pictureLen);
            }

            var uploadSlots = reader.ReadInteger();
            var queueLength = reader.ReadInteger();
            var hasFreeUploadSlot = reader.ReadByte() > 0;

            return new UserInfo(description, uploadSlots, queueLength, hasFreeUploadSlot, picture);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <param name="userInfo">The instance from which to construct the byte array.</param>
        /// <returns>The constructed byte array.</returns>
        public static byte[] ToByteArray(this UserInfo userInfo)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(userInfo.Description)
                .WriteByte((byte)(userInfo.HasPicture ? 1 : 0));

            if (userInfo.HasPicture)
            {
                builder
                    .WriteInteger(userInfo.Picture.Length)
                    .WriteBytes(userInfo.Picture);
            }

            builder
                .WriteInteger(userInfo.UploadSlots)
                .WriteInteger(userInfo.QueueLength)
                .WriteByte((byte)(userInfo.HasFreeUploadSlot ? 1 : 0));

            return builder.Build();
        }
    }
}