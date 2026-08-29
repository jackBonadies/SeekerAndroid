// <copyright file="SearchStates.cs" company="JP Dillingham">
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
    ///     Search state.
    /// </summary>
    [Flags]
    public enum SearchStates
    {
        /// <summary>
        ///     None.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Requested.
        /// </summary>
        Requested = 1,

        /// <summary>
        ///     In progress.
        /// </summary>
        InProgress = 2,

        /// <summary>
        ///     Completed.
        /// </summary>
        Completed = 4,

        /// <summary>
        ///     Completed due to cancellation.
        /// </summary>
        Cancelled = 8,

        /// <summary>
        ///     Completed due to the timeout value specified in <see cref="SearchOptions"/> having been reached.
        /// </summary>
        /// <remarks>The timeout duration is from the time of the last response.</remarks>
        TimedOut = 16,

        /// <summary>
        ///     Completed due to the response limit specified in <see cref="SearchOptions"/> having been reached.
        /// </summary>
        ResponseLimitReached = 32,

        /// <summary>
        ///     Completed due to the file limit specified in <see cref="SearchOptions"/> having been reached.
        /// </summary>
        FileLimitReached = 64,

        /// <summary>
        ///     Completed due to error.
        /// </summary>
        Errored = 128,

        /// <summary>
        ///     Queued.
        /// </summary>
        Queued = 256,
    }
}