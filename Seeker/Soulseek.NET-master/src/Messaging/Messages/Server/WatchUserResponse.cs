// <copyright file="WatchUserResponse.cs" company="JP Dillingham">
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
    ///     The response to an add user request.
    /// </summary>
    internal sealed class WatchUserResponse : IIncomingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WatchUserResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the added peer.</param>
        /// <param name="exists">A value indicating whether the username exists on the network.</param>
        /// <param name="userData">If <see cref="Exists"/>, the user's data.</param>
        public WatchUserResponse(string username, bool exists, UserData userData = null)
        {
            Username = username;
            Exists = exists;
            UserData = userData;
        }

        /// <summary>
        ///     Gets a value indicating whether the username exists on the network.
        /// </summary>
        public bool Exists { get; }

        /// <summary>
        ///     Gets the user's data, if the user <see cref="Exists"/>.
        /// </summary>
        public UserData UserData { get; }

        /// <summary>
        ///     Gets the username of the added peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="WatchUserResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static WatchUserResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.WatchUser)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(WatchUserResponse)} (expected: {(int)MessageCode.Server.WatchUser}, received: {(int)code})");
            }

            var username = reader.ReadString();
            var b = reader.ReadByte();
            var exists = b > 0;

            UserData user = null;

            if (exists)
            {
                var status = (UserPresence)reader.ReadInteger();
                var averageSpeed = reader.ReadInteger();
                var downloadCount = reader.ReadLong();
                var fileCount = reader.ReadInteger();
                var directoryCount = reader.ReadInteger();
                string countryCode = null;
                if (reader.HasMoreData)
                {
                    countryCode = reader.ReadString();
                }

                user = new UserData(username, status, averageSpeed, downloadCount, fileCount, directoryCount, countryCode);
            }

            return new WatchUserResponse(username, exists, user);
        }
    }
}