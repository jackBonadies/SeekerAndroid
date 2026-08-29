// <copyright file="MessageConnectionEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek.Network
{
    using System;

    /// <summary>
    ///     EventArgs for <see cref="MessageConnection"/> events.
    /// </summary>
#pragma warning disable S2094 // Classes should not be empty
    internal abstract class MessageConnectionEventArgs : EventArgs
#pragma warning restore S2094 // Classes should not be empty
    {
    }

    /// <summary>
    ///     EventArgs for <see cref="MessageConnection"/> events raised message data is received or sent.
    /// </summary>
    internal sealed class MessageDataEventArgs : MessageConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageDataEventArgs"/> class.
        /// </summary>
        /// <param name="code">The message code, as a byte array.</param>
        /// <param name="currentLength">The length of the event data.</param>
        /// <param name="totalLength">The total expected length of the data transfer.</param>
        public MessageDataEventArgs(byte[] code, long currentLength, long totalLength)
        {
            Code = code;
            CurrentLength = currentLength;
            TotalLength = totalLength;

            PercentComplete = (CurrentLength / (double)TotalLength) * 100d;
        }

        /// <summary>
        ///     Gets the message code, as a byte array.
        /// </summary>
        public byte[] Code { get; }

        /// <summary>
        ///     Gets the length of the event data.
        /// </summary>
        public long CurrentLength { get; }

        /// <summary>
        ///     Gets the progress of the data transfer as a percentage of current and total data length.
        /// </summary>
        public double PercentComplete { get; }

        /// <summary>
        ///     Gets the total expected length of the data transfer.
        /// </summary>
        public long TotalLength { get; }
    }

    /// <summary>
    ///     EventArgs for <see cref="MessageConnection"/> events raised when a message is read or written in its entirety.
    /// </summary>
    internal sealed class MessageEventArgs : MessageConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message associated with the event.</param>
        public MessageEventArgs(byte[] message)
        {
            Message = message;
        }

        /// <summary>
        ///     Gets the message associated with the event.
        /// </summary>
        public byte[] Message { get; }
    }

    /// <summary>
    ///     EventArgs for <see cref="MessageConnection"/> events raised when a message is received, but before it is read.
    /// </summary>
    internal sealed class MessageReceivedEventArgs : MessageConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="length">The message length.</param>
        /// <param name="code">The message code, as a byte array.</param>
        public MessageReceivedEventArgs(long length, byte[] code)
        {
            Length = length;
            Code = code;
        }

        /// <summary>
        ///     Gets the message code, as a byte array.
        /// </summary>
        public byte[] Code { get; }

        /// <summary>
        ///     Gets the message length.
        /// </summary>
        public long Length { get; }
    }
}