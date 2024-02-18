// <copyright file="PlaceInQueueRequest.cs" company="JP Dillingham">
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
    ///     Requests the place of a file in a remote queue.
    /// </summary>
    internal sealed class PlaceInQueueRequest : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PlaceInQueueRequest"/> class.
        /// </summary>
        /// <param name="filename">The filename to check.</param>
        public PlaceInQueueRequest(string filename, bool isLegacy = false, bool isFolderNameLatin1Decoded = false)
        {
            Filename = filename;
            IsLegacy = isLegacy;
            IsFolderNameLatin1Decoded = isFolderNameLatin1Decoded;
        }

        /// <summary>
        ///     Gets the filename to check.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the filename to check.
        /// </summary>
        public bool IsLegacy { get; }

        /// <summary>
        ///     Gets the filename to check.
        /// </summary>
        public bool IsFolderNameLatin1Decoded { get; }


        /// <summary>
        ///     Creates a new instance of <see cref="PlaceInQueueRequest"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PlaceInQueueRequest FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.PlaceInQueueRequest)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(PlaceInQueueRequest)} response (expected: {(int)MessageCode.Peer.PlaceInQueueRequest}, received: {(int)code})");
            }

            var filename = reader.ReadString();

            return new PlaceInQueueRequest(filename);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueRequest)
                .WriteString(Filename, this.IsLegacy, this.IsFolderNameLatin1Decoded)
                .Build();
        }
    }
}