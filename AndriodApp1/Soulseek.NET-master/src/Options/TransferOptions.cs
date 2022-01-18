// <copyright file="TransferOptions.cs" company="JP Dillingham">
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
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Options for transfer operations.
    /// </summary>
    public class TransferOptions
    {
        private readonly Func<double, string, CancellationToken, Task> defaultGovernor =
            (a, b, token) => Task.CompletedTask;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferOptions"/> class.
        /// </summary>
        /// <param name="governor">The delegate used to govern transfer speed.</param>
        /// <param name="stateChanged">The Action to invoke when the transfer changes state.</param>
        /// <param name="progressUpdated">The Action to invoke when the transfer receives data.</param>
        /// <param name="maximumLingerTime">
        ///     The maximum linger time, in milliseconds, that a connection will attempt to cleanly close following a transfer.
        /// </param>
        /// <param name="disposeInputStreamOnCompletion">
        ///     A value indicating whether the input stream should be closed upon transfer completion.
        /// </param>
        /// <param name="disposeOutputStreamOnCompletion">
        ///     A value indicating whether the output stream should be closed upon transfer completion.
        /// </param>
        public TransferOptions(
            Func<double, string, CancellationToken, Task> governor = null,
            Action<TransferStateChangedEventArgs> stateChanged = null,
            Action<TransferProgressUpdatedEventArgs> progressUpdated = null,
            int maximumLingerTime = 3000,
            bool disposeInputStreamOnCompletion = false,
            bool disposeOutputStreamOnCompletion = false)
        {
            DisposeInputStreamOnCompletion = disposeInputStreamOnCompletion;
            DisposeOutputStreamOnCompletion = disposeOutputStreamOnCompletion;
            Governor = governor ?? defaultGovernor;
            StateChanged = stateChanged;
            ProgressUpdated = progressUpdated;
            MaximumLingerTime = maximumLingerTime;
        }

        /// <summary>
        ///     Gets a value indicating whether input streams should be closed upon transfer completion. (Default = false).
        /// </summary>
        public bool DisposeInputStreamOnCompletion { get; }

        /// <summary>
        ///     Gets a value indicating whether output streams should be closed upon transfer completion. (Default = false).
        /// </summary>
        public bool DisposeOutputStreamOnCompletion { get; }

        /// <summary>
        ///     Gets the delegate used to govern transfer speed. (Default = a delegate returning Task.CompletedTask).
        /// </summary>
        public Func<double, string, CancellationToken, Task> Governor { get; }

        /// <summary>
        ///     Gets the maximum linger time, in milliseconds, that a connection will attempt to cleanly close following a
        ///     transfer. (Default = 3000).
        /// </summary>
        public int MaximumLingerTime { get; }

        /// <summary>
        ///     Gets the Action to invoke when the transfer receives data. (Default = no action).
        /// </summary>
        public Action<TransferProgressUpdatedEventArgs> ProgressUpdated { get; }

        /// <summary>
        ///     Gets the Action to invoke when the transfer changes state. (Default = no action).
        /// </summary>
        public Action<TransferStateChangedEventArgs> StateChanged { get; }
    }
}