// <copyright file="IDiagnosticFactory.cs" company="JP Dillingham">
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

namespace Soulseek.Diagnostics
{
    using System;

    /// <summary>
    ///     Creates diagnostic messages.
    /// </summary>
    internal interface IDiagnosticFactory
    {
        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Trace"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        void Trace(string message);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Trace"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        /// <param name="exception">An optional Exception.</param>
        void Trace(string message, Exception exception);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Debug"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        void Debug(string message);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Debug"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        /// <param name="exception">An optional Exception.</param>
        void Debug(string message, Exception exception);

        /// <summary>
        ///     Creates an <see cref="DiagnosticLevel.Info"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        void Info(string message);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Warning"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        /// <param name="exception">An optional Exception.</param>
        void Warning(string message, Exception exception = null);
    }
}