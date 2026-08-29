// <copyright file="SoulseekClientDisconnectedEventArgs.cs" company="JP Dillingham">
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