// <copyright file="Settings.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Integration
{
    using System;

    /// <summary>
    ///     Configuration for integration tests.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        ///     Gets the username to use when logging in.
        /// </summary>
        public static string Username => Environment.GetEnvironmentVariable("SLSK_INTEGRATION_USERNAME");

        /// <summary>
        ///     Gets the password to use when logging in.
        /// </summary>
        public static string Password => Environment.GetEnvironmentVariable("SLSK_INTEGRATION_PASSWORD");
    }
}
