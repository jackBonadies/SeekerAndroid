// <copyright file="PlaceInQueueResponse.cs" company="JP Dillingham">
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
    ///     The response received when an attempt to queue a file for downloading has failed.
    /// </summary>
    internal sealed class PlaceInQueueResponse : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PlaceInQueueResponse"/> class.
        /// </summary>
        /// <param name="filename">The filename which was checked.</param>
        /// <param name="placeInQueue">The current place in the peer's queue.</param>
        public PlaceInQueueResponse(string filename, int placeInQueue)
        {
            Filename = filename;
            PlaceInQueue = placeInQueue;
        }

        /// <summary>
        ///     Gets the filename which failed to be queued.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the current place in the peer's queue.
        /// </summary>
        public int PlaceInQueue { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PlaceInQueueResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PlaceInQueueResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.PlaceInQueueResponse)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PlaceInQueueResponse)} (expected: {(int)MessageCode.Peer.PlaceInQueueResponse}, received: {(int)code})");
            }

            var filename = reader.ReadString();
            var placeInQueue = reader.ReadInteger();

            return new PlaceInQueueResponse(filename, placeInQueue);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString(Filename)
                .WriteInteger(PlaceInQueue)
                .Build();
        }
    }
}