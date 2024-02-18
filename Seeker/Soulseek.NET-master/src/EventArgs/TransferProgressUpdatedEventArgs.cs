// <copyright file="TransferProgressUpdatedEventArgs.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Event arguments for events raised by an update to transfer progress.
    /// </summary>
    public class TransferProgressUpdatedEventArgs : TransferEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferProgressUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="previousBytesTransferred">The previous total number of bytes transferred.</param>
        /// <param name="transfer">The transfer which raised the event.</param>
        internal TransferProgressUpdatedEventArgs(long previousBytesTransferred, Transfer transfer)
            : base(transfer)
        {
            PreviousBytesTransferred = previousBytesTransferred;
        }

        /// <summary>
        ///     Gets the total number of bytes transferred prior to the event.
        /// </summary>
        public long PreviousBytesTransferred { get; }
    }
}