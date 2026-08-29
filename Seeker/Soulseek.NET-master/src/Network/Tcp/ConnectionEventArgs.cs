// <copyright file="ConnectionEventArgs.cs" company="JP Dillingham">
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

namespace Soulseek.Network.Tcp
{
    using System;

    /// <summary>
    ///     EventArgs for <see cref="Connection"/> events.
    /// </summary>
#pragma warning disable S2094 // Classes should not be empty
    internal abstract class ConnectionEventArgs : EventArgs
#pragma warning restore S2094 // Classes should not be empty
    {
    }

    /// <summary>
    ///     EventArgs for <see cref="Connection"/> events raised by the exchange of data with a remote host.
    /// </summary>
    internal sealed class ConnectionDataEventArgs : ConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionDataEventArgs"/> class.
        /// </summary>
        /// <param name="currentLength">The length of the event data.</param>
        /// <param name="totalLength">The total expected length of the data transfer.</param>
        public ConnectionDataEventArgs(long currentLength, long totalLength)
        {
            CurrentLength = currentLength;
            TotalLength = totalLength;

            PercentComplete = (CurrentLength / (double)TotalLength) * 100d;
        }

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
    ///     EventArgs for <see cref="Connection"/> events raised when the connection is disconnected.
    /// </summary>
    internal sealed class ConnectionDisconnectedEventArgs : ConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionDisconnectedEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message describing the reason for the disconnect.</param>
        /// <param name="exception">The optional Exception associated with the disconnect.</param>
        public ConnectionDisconnectedEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the optional Exception associated with the disconnect.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the message describing the reason for the disconnect.
        /// </summary>
        public string Message { get; }
    }

    /// <summary>
    ///     EventArgs for <see cref="Connection"/> events raised by a change of connection state.
    /// </summary>
    internal sealed class ConnectionStateChangedEventArgs : ConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The state from which the connection changed.</param>
        /// <param name="currentState">The state to which the connection changed.</param>
        /// <param name="message">The optional message describing the nature of the change.</param>
        /// <param name="exception">The optional Exception associated with the change.</param>
        public ConnectionStateChangedEventArgs(ConnectionState previousState, ConnectionState currentState, string message = null, Exception exception = null)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the state to which the connection changed.
        /// </summary>
        public ConnectionState CurrentState { get; }

        /// <summary>
        ///     Gets the optional Exception associated with the change.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the optional message describing the nature of the change.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the state from which the connection changed.
        /// </summary>
        public ConnectionState PreviousState { get; }
    }
}