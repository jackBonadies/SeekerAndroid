// <copyright file="GlobalDiagnostic.cs" company="JP Dillingham">
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
    ///     A statically acessible instance of <see cref="IDiagnosticFactory"/>.
    /// </summary>
    /// <remarks>
    ///     This should be used sparingly and only where it isn't feasible to pass a reference
    ///     to the source.
    /// </remarks>
    internal static class GlobalDiagnostic
    {
        private static IDiagnosticFactory Factory { get; set; }

        /// <summary>
        ///     Initializes the global factory with the specified <paramref name="factory"/>.
        /// </summary>
        /// <param name="factory">The factory to use.</param>
        public static void Init(IDiagnosticFactory factory) => Factory = factory;

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Trace"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        public static void Trace(string message) => Factory?.Trace(message);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Trace"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        /// <param name="exception">An optional Exception.</param>
        public static void Trace(string message, Exception exception) => Factory?.Trace(message, exception);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Debug"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        public static void Debug(string message) => Factory?.Debug(message);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Debug"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        /// <param name="exception">An optional Exception.</param>
        public static void Debug(string message, Exception exception) => Factory?.Debug(message, exception);

        /// <summary>
        ///     Creates an <see cref="DiagnosticLevel.Info"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        public static void Info(string message) => Factory?.Info(message);

        /// <summary>
        ///     Creates a <see cref="DiagnosticLevel.Warning"/> diagnostic message.
        /// </summary>
        /// <param name="message">The desired message.</param>
        /// <param name="exception">An optional Exception.</param>
        public static void Warning(string message, Exception exception = null) => Factory?.Warning(message, exception);
    }
}
