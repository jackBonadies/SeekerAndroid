// <copyright file="JoinRoomResponse.cs" company="JP Dillingham">
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
    using System.Collections.Generic;

    /// <summary>
    ///     The response to request to join a chat room.
    /// </summary>
    internal sealed class JoinRoomResponse : IIncomingMessage
    {
        /// <summary>
        ///     Creates a new instance of <see cref="RoomData"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        internal static RoomData FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.JoinRoom)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(JoinRoomResponse)} (expected: {(int)MessageCode.Server.JoinRoom}, received: {(int)code}");
            }

            var roomName = reader.ReadString();

            var userCount = reader.ReadInteger();
            var userNames = new List<string>();

            for (int i = 0; i < userCount; i++)
            {
                userNames.Add(reader.ReadString());
            }

            var statusCount = reader.ReadInteger();
            var statuses = new List<UserPresence>();

            for (int i = 0; i < statusCount; i++)
            {
                statuses.Add((UserPresence)reader.ReadInteger());
            }

            var dataCount = reader.ReadInteger();
            var datums = new List<(int AverageSpeed, long DownloadCount, int FileCount, int DirectoryCount)>();

            for (int i = 0; i < dataCount; i++)
            {
                var averageSpeed = reader.ReadInteger();
                var downloadCount = reader.ReadLong();
                var fileCount = reader.ReadInteger();
                var directoryCount = reader.ReadInteger();

                datums.Add((averageSpeed, downloadCount, fileCount, directoryCount));
            }

            var slotsFreeCount = reader.ReadInteger();
            var slots = new List<int>();

            for (int i = 0; i < slotsFreeCount; i++)
            {
                slots.Add(reader.ReadInteger());
            }

            var countryCount = reader.ReadInteger();
            var countries = new List<string>();

            for (int i = 0; i < countryCount; i++)
            {
                countries.Add(reader.ReadString());
            }

            var users = new List<UserData>();

            for (int i = 0; i < userCount; i++)
            {
                var name = userNames[i];
                var status = statuses[i];
                var (averageSpeed, downloadCount, fileCount, directoryCount) = datums[i];
                var slot = slots[i];
                var country = countries[i];

                users.Add(new UserData(name, status, averageSpeed, downloadCount, fileCount, directoryCount, country, slot));
            }

            string owner = null;
            int? operatorCount = null;
            List<string> operatorList = null;

            if (reader.HasMoreData)
            {
                owner = reader.ReadString();
                operatorCount = reader.ReadInteger();
                operatorList = new List<string>();

                for (int i = 0; i < operatorCount; i++)
                {
                    operatorList.Add(reader.ReadString());
                }
            }

            return new RoomData(roomName, users, owner != null, owner, operatorList);
        }
    }
}