// <copyright file="UploadFailed.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Messaging.Messages
{
    /// <summary>
    ///     A notification that an upload has failed.
    /// </summary>
    internal sealed class UploadFailed : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UploadFailed"/> class.
        /// </summary>
        /// <param name="filename">The filename which failed to be uploaded.</param>
        public UploadFailed(string filename)
        {
            Filename = filename;
        }

        /// <summary>
        ///     Gets the filename which failed to be uploaded.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UploadFailed"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static UploadFailed FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.UploadFailed)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UploadFailed)} (expected: {(int)MessageCode.Peer.UploadFailed}, received: {(int)code})");
            }

            var filename = reader.ReadString();

            return new UploadFailed(filename);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString(Filename)
                .Build();
        }
    }
}