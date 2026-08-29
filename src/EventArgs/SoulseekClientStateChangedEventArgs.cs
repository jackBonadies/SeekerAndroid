// <copyright file="SoulseekClientStateChangedEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised by a change in client state.
    /// </summary>
    public class SoulseekClientStateChangedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the client.</param>
        /// <param name="state">The current state of the client.</param>
        /// <param name="message">The message associated with the change in state, if applicable.</param>
        /// <param name="exception">The Exception associated with the change in state, if applicable.</param>
        public SoulseekClientStateChangedEventArgs(SoulseekClientStates previousState, SoulseekClientStates state, string message = null, Exception exception = null)
        {
            PreviousState = previousState;
            State = state;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the Exception associated with change in state, if applicable.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the message associated with the change in state, if applicable.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the previous client state.
        /// </summary>
        public SoulseekClientStates PreviousState { get; }

        /// <summary>
        ///     Gets the current client state.
        /// </summary>
        public SoulseekClientStates State { get; }
    }
}