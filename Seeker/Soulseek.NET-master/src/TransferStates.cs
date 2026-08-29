// <copyright file="TransferStates.cs" company="JP Dillingham">
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
    ///     Transfer state.
    /// </summary>
    /// <remarks>
    ///     The Completed state will be accompanied by one other flag consisting of <see cref="Succeeded"/>,
    ///     <see cref="Cancelled"/>, <see cref="TimedOut"/> or <see cref="Errored"/>.
    /// </remarks>
    [Flags]
    public enum TransferStates
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
        ///     Queued (remotely for downloads, locally for uploads).
        /// </summary>
        Queued = 2,

        /// <summary>
        ///     Initializing.
        /// </summary>
        Initializing = 4,

        /// <summary>
        ///     In progress.
        /// </summary>
        InProgress = 8,

        /// <summary>
        ///     Completed; check remaining state flags for disposition.
        /// </summary>
        Completed = 16,

        /// <summary>
        ///     Completed due to a successful transfer.
        /// </summary>
        Succeeded = 32,

        /// <summary>
        ///     Completed due to cancellation.
        /// </summary>
        Cancelled = 64,

        /// <summary>
        ///     Completed due to timeout.
        /// </summary>
        TimedOut = 128,

        /// <summary>
        ///     Completed due to transfer error.
        /// </summary>
        Errored = 256,

        /// <summary>
        ///     Completed due to rejection by peer.
        /// </summary>
        Rejected = 512,

        /// <summary>
        ///     Completed due to unexpected circumstances.
        /// </summary>
        Aborted = 1024,

        /// <summary>
        ///     Queued locally, due to user-defined constraints.
        /// </summary>
        Locally = 2048,

        /// <summary>
        ///     Queued remotely.
        /// </summary>
        Remotely = 4096,
    }
}