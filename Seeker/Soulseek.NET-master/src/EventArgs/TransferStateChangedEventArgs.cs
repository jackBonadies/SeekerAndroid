﻿// <copyright file="TransferStateChangedEventArgs.cs" company="JP Dillingham">
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
    ///     Event arguments for events raised by a change in transfer state.
    /// </summary>
    public class TransferStateChangedEventArgs : TransferEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the transfer.</param>
        /// <param name="transfer">The transfer which raised the event.</param>
        internal TransferStateChangedEventArgs(TransferStates previousState, Transfer transfer)
            : base(transfer)
        {
            PreviousState = previousState;
        }

        /// <summary>
        ///     Gets the previous state of the transfer.
        /// </summary>
        public TransferStates PreviousState { get; }

        /// <summary>
        ///     Location of the incomplete stream.
        /// </summary>
        public string IncompleteParentUri { get; set; }
    }
}