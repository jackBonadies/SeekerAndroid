// <copyright file="TransferSizeMismatchException.cs" company="JP Dillingham">
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
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents errors that occur when the remote size of a file does not match the size specified locally.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class TransferSizeMismatchException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferSizeMismatchException"/> class.
        /// </summary>
        /// <param name="localSize">The size requested locally.</param>
        /// <param name="remoteSize">The size reported by the remote peer.</param>
        public TransferSizeMismatchException(long localSize, long remoteSize)
            : base()
        {
            LocalSize = localSize;
            RemoteSize = remoteSize;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferSizeMismatchException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="localSize">The size requested locally.</param>
        /// <param name="remoteSize">The size reported by the remote peer.</param>
        public TransferSizeMismatchException(string message, long localSize, long remoteSize)
            : base(message)
        {
            LocalSize = localSize;
            RemoteSize = remoteSize;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferSizeMismatchException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="localSize">The size requested locally.</param>
        /// <param name="remoteSize">The size reported by the remote peer.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public TransferSizeMismatchException(string message, long localSize, long remoteSize, Exception innerException)
            : base(message, innerException)
        {
            LocalSize = localSize;
            RemoteSize = remoteSize;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferSizeMismatchException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected TransferSizeMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        ///     Gets the size reported by the remote peer.
        /// </summary>
        public long RemoteSize { get; }

        /// <summary>
        ///     Gets the size requested locally.
        /// </summary>
        public long LocalSize { get; }
    }
}