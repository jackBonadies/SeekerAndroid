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
        private readonly Func<Transfer, int, CancellationToken, Task<int>> defaultGovernor =
            (tx, s, token) => Task.FromResult(int.MaxValue);

        private readonly Func<Transfer, CancellationToken, Task> defaultSlotAwaiter =
            (tx, token) => Task.CompletedTask;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferOptions"/> class.
        /// </summary>
        /// <param name="governor">
        ///     The delegate, accepting the number of requested bytes and returning the number of granted bytes, used to govern
        ///     transfer speed.
        /// </param>
        /// <param name="stateChanged">The delegate to invoke when the transfer changes state.</param>
        /// <param name="progressUpdated">The delegate to invoke when the transfer receives data.</param>
        /// <param name="slotAwaiter">The delegate used to await a slot to start the transfer (uploads only).</param>
        /// <param name="slotReleased">The delegate used to signal release of the slot (uploads only).</param>
        /// <param name="reporter">
        ///     The delegate, accepting the number of bytes attempted, granted, and transferred for each chunk, used to report
        ///     transfer statistics.
        /// </param>
        /// <param name="maximumLingerTime">
        ///     The maximum linger time, in milliseconds, that a connection will attempt to cleanly close following a transfer.
        /// </param>
        /// <param name="seekInputStreamAutomatically">
        ///     A value indicating whether the input stream should be automatically seeked to the desired start offset, if one is specified.
        /// </param>
        /// <param name="disposeInputStreamOnCompletion">
        ///     A value indicating whether the input stream should be closed upon transfer completion.
        /// </param>
        /// <param name="disposeOutputStreamOnCompletion">
        ///     A value indicating whether the output stream should be closed upon transfer completion.
        /// </param>
        public TransferOptions(
            Func<Transfer, int, CancellationToken, Task<int>> governor = null,
            Action<(TransferStates PreviousState, Transfer Transfer)> stateChanged = null,
            Action<(long PreviousBytesTransferred, Transfer Transfer)> progressUpdated = null,
            Func<Transfer, CancellationToken, Task> slotAwaiter = null,
            Action<Transfer> slotReleased = null,
            Action<Transfer, int, int, int> reporter = null,
            int maximumLingerTime = 3000,
            bool seekInputStreamAutomatically = true,
            bool disposeInputStreamOnCompletion = true,
            bool disposeOutputStreamOnCompletion = true)
        {
            SeekInputStreamAutomatically = seekInputStreamAutomatically;
            DisposeInputStreamOnCompletion = disposeInputStreamOnCompletion;
            DisposeOutputStreamOnCompletion = disposeOutputStreamOnCompletion;
            Governor = governor ?? defaultGovernor;
            SlotAwaiter = slotAwaiter ?? defaultSlotAwaiter;
            SlotReleased = slotReleased;
            Reporter = reporter;

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
        ///     Gets the delegate, accepting the number of requested bytes and returning the number of granted bytes, used to
        ///     govern transfer speed. (Default = a delegate returning int.MaxValue).
        /// </summary>
        public Func<Transfer, int, CancellationToken, Task<int>> Governor { get; }

        /// <summary>
        ///     Gets the maximum linger time, in milliseconds, that a connection will attempt to cleanly close following a
        ///     transfer. (Default = 3000).
        /// </summary>
        public int MaximumLingerTime { get; }

        /// <summary>
        ///     Gets the delegate to invoke when the transfer receives data. (Default = no action).
        /// </summary>
        public Action<(long PreviousBytesTransferred, Transfer Transfer)> ProgressUpdated { get; }

        /// <summary>
        ///     Gets the delegate, accepting the number of bytes attempted, granted, and transferred for each chunk, used to
        ///     report transfer statistics. (Default = no action).
        /// </summary>
        public Action<Transfer, int, int, int> Reporter { get; }

        /// <summary>
        ///     Gets a value indicating whether the input stream should be automatically seeked to the desired start offset, if
        ///     one is specified.
        /// </summary>
        public bool SeekInputStreamAutomatically { get; }

        /// <summary>
        ///     Gets the delegate used to await a slot to start the transfer (uploads only). (Default = a delegate returning Task.CompletedTask).
        /// </summary>
        public Func<Transfer, CancellationToken, Task> SlotAwaiter { get; }

        /// <summary>
        ///     Gets the delegate used to signal release of the slot (uploads only). (Default = no action).
        /// </summary>
        public Action<Transfer> SlotReleased { get; }

        /// <summary>
        ///     Gets the delegate to invoke when the transfer changes state. (Default = no action).
        /// </summary>
        public Action<(TransferStates PreviousState, Transfer Transfer)> StateChanged { get; }

        /// <summary>
        ///     Returns a clone of this instance with <see cref="StateChanged"/> wrapped in a new delegate that first invokes <paramref name="stateChanged"/>.
        /// </summary>
        /// <param name="stateChanged">A new delegate to execute prior to the existing delegate.</param>
        /// <returns>A clone of this instance with the combined StateChanged delegates.</returns>
        public TransferOptions WithAdditionalStateChanged(Action<(TransferStates PreviousState, Transfer Transfer)> stateChanged)
        {
            return new TransferOptions(
                governor: Governor,
                stateChanged: (args) =>
                {
                    stateChanged?.Invoke(args);
                    StateChanged?.Invoke(args);
                },
                progressUpdated: ProgressUpdated,
                slotAwaiter: SlotAwaiter,
                slotReleased: SlotReleased,
                reporter: Reporter,
                maximumLingerTime: MaximumLingerTime,
                seekInputStreamAutomatically: SeekInputStreamAutomatically,
                disposeInputStreamOnCompletion: DisposeInputStreamOnCompletion,
                disposeOutputStreamOnCompletion: DisposeOutputStreamOnCompletion);
        }

        /// <summary>
        ///     Returns a clone of this instance with the specified disposal options.
        /// </summary>
        /// <param name="disposeInputStreamOnCompletion">
        ///     A value indicating whether the input stream should be closed upon transfer completion.
        /// </param>
        /// <param name="disposeOutputStreamOnCompletion">
        ///     A value indicating whether the output stream should be closed upon transfer completion.
        /// </param>
        /// <returns>A clone of this instance with the specified disposal options.</returns>
        public TransferOptions WithDisposalOptions(
            bool? disposeInputStreamOnCompletion = null,
            bool? disposeOutputStreamOnCompletion = null)
        {
            return new TransferOptions(
                governor: Governor,
                stateChanged: StateChanged,
                progressUpdated: ProgressUpdated,
                slotAwaiter: SlotAwaiter,
                slotReleased: SlotReleased,
                reporter: Reporter,
                maximumLingerTime: MaximumLingerTime,
                seekInputStreamAutomatically: SeekInputStreamAutomatically,
                disposeInputStreamOnCompletion: disposeInputStreamOnCompletion ?? DisposeInputStreamOnCompletion,
                disposeOutputStreamOnCompletion: disposeOutputStreamOnCompletion ?? DisposeOutputStreamOnCompletion);
        }
    }
}