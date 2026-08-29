// <copyright file="SoulseekClientDisconnectedEventArgs.cs" company="JP Dillingham">
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

    /// <summary>
    ///     Event arguments for events raised by client disconnect.
    /// </summary>
    public class SoulseekClientDisconnectedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientDisconnectedEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message describing the reason for the disconnect.</param>
        /// <param name="exception">The Exception associated with the disconnect, if applicable.</param>
        public SoulseekClientDisconnectedEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the Exception associated with change in state, if applicable.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the message describing the reason for the disconnect.
        /// </summary>
        public string Message { get; }
    }
}