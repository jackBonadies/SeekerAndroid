// <copyright file="UploadDenied.cs" company="JP Dillingham">
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
    ///     A notification that an upload has been denied.
    /// </summary>
    internal sealed class UploadDenied : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UploadDenied"/> class.
        /// </summary>
        /// <param name="filename">The filename for which the upload was denied.</param>
        /// <param name="message">The reason for the denial.</param>
        public UploadDenied(string filename, string message)
        {
            Filename = filename;
            Message = message;
        }

        /// <summary>
        ///     Gets the filename for which the upload was denied.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the reason for the denial.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="UploadDenied"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static UploadDenied FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.UploadDenied)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(UploadDenied)} (expected: {(int)MessageCode.Peer.UploadDenied}, received: {(int)code})");
            }

            var filename = reader.ReadString();
            var msg = reader.ReadString();

            return new UploadDenied(filename, msg);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadDenied)
                .WriteString(Filename)
                .WriteString(Message)
                .Build();
        }
    }
}