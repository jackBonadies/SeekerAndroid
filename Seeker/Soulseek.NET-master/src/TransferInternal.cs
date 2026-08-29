// <copyright file="TransferInternal.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     A single file transfer.
    /// </summary>
    internal sealed class TransferInternal
    {
        private readonly int progressUpdateLimit = 1000;
        private readonly double speedAlpha = 2f / 10;
        private double lastProgressBytes = 0;
        private DateTime? lastProgressTime = null;
        private bool speedInitialized = false;
        private TransferStates state = TransferStates.None;
        private long startOffset = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferInternal"/> class.
        /// </summary>
        /// <param name="direction">The transfer direction.</param>
        /// <param name="username">The username of the peer to or from which the file is to be transferred.</param>
        /// <param name="filename">The filename of the file to be transferred.</param>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="options">The options for the transfer.</param>
        public TransferInternal(TransferDirection direction, string username, string filename, int token, TransferOptions options = null)
        {
            Direction = direction;
            Username = username;
            Filename = filename;
            Token = token;

            Options = options ?? new TransferOptions();

            WaitKey = new WaitKey(Constants.WaitKey.Transfer, Direction, Username, Filename, Token);
        }

        /// <summary>
        ///     Gets the current average download speed.
        /// </summary>
        public double AverageSpeed { get; private set; }

        /// <summary>
        ///     Gets the number of remaining bytes to be transferred.
        /// </summary>
        public long BytesRemaining => (Size ?? 0) - BytesTransferred;

        /// <summary>
        ///     Gets the total number of bytes transferred.
        /// </summary>
        public long BytesTransferred { get; private set; }

        /// <summary>
        ///     Gets or sets the connection used for the transfer.
        /// </summary>
        /// <remarks>Ensure that the reference instance is disposed when the transfer is complete.</remarks>
        public IConnection Connection { get; set; }

        /// <summary>
        ///     Gets the transfer direction.
        /// </summary>
        public TransferDirection Direction { get; }

        /// <summary>
        ///     Gets the current duration of the transfer, if it has been started.
        /// </summary>
        public TimeSpan? ElapsedTime => StartTime == null ? null : (TimeSpan?)((EndTime ?? DateTime.UtcNow) - StartTime.Value);

        /// <summary>
        ///     Gets the UTC time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.
        /// </summary>
        /// <remarks>
        ///     Should ONLY be set when the State transitions into Completed.
        /// </remarks>
        public DateTime? EndTime { get; private set; } = null;

        /// <summary>
        ///     Gets or sets the <see cref="Exception"/> that caused the failure of the transfer, if applicable.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        ///     Gets the filename of the file to be transferred.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the ip endpoint of the remote transfer connection, if one has been established.
        /// </summary>
        public IPEndPoint IPEndPoint => Connection?.IPEndPoint;

        /// <summary>
        ///     Gets the options for the transfer.
        /// </summary>
        public TransferOptions Options { get; }

        /// <summary>
        ///     Gets the current progress in percent.
        /// </summary>
        public double PercentComplete => Size.HasValue ? (BytesTransferred / (double)Size) * 100 : 0;

        /// <summary>
        ///     Gets the projected remaining duration of the transfer.
        /// </summary>
        public TimeSpan? RemainingTime => AverageSpeed == 0 ? null : (TimeSpan?)TimeSpan.FromSeconds(BytesRemaining / AverageSpeed);

        /// <summary>
        ///     Gets or sets the remote unique token for the transfer.
        /// </summary>
        public int? RemoteToken { get; set; }

        /// <summary>
        ///     Gets or sets the size of the file to be transferred, in bytes.
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        ///     Gets or sets the start offset of the transfer, in bytes.
        /// </summary>
        public long StartOffset
        {
            get
            {
                return startOffset;
            }
            set
            {
                startOffset = value;

                // fast-forward the transfer up to StartOffset so percent completion
                // and transfer speed computation works properly
                BytesTransferred = value;
                lastProgressBytes = value;
            }
        }

        /// <summary>
        ///     Gets the UTC time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.
        /// </summary>
        /// <remarks>
        ///     Should ONLY be set when the State transitions into InProgress, or if it transitions into Completed
        ///     without having first transitioned into InProgress.
        /// </remarks>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        ///     Gets or sets the state of the transfer.
        /// </summary>
        public TransferStates State
        {
            get
            {
                return state;
            }

            set
            {
                var time = DateTime.UtcNow;

                if (value.HasFlag(TransferStates.InProgress) && !StartTime.HasValue)
                {
                    StartTime = time;
                }
                else if (value.HasFlag(TransferStates.Completed) && !EndTime.HasValue)
                {
                    EndTime = time;

                    // in case the transfer never transitioned into InProgress, set StartTime too
                    StartTime ??= time;
                }

                state = value;

                // ensure the average is calculated properly when the transfer ends, regardless of whether
                // an outside caller is calling UpdateProgress (as they should?)
                if (state.HasFlag(TransferStates.Completed))
                {
                    UpdateProgress(BytesTransferred);
                }
            }
        }

        /// <summary>
        ///     Gets the unique token for the transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer to or from which the file is to be transferred.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets the wait key for the transfer.
        /// </summary>
        public WaitKey WaitKey { get; }

        /// <summary>
        ///     Gets the task completion source used to end the transfer if/when the remote client reports that it has failed or been rejected.
        /// </summary>
        public TaskCompletionSource<bool> RemoteTaskCompletionSource { get; } = new TaskCompletionSource<bool>();

        /// <summary>
        ///     Updates the transfer progress.
        /// </summary>
        /// <param name="bytesTransferred">The total number of bytes transferred.</param>
        public void UpdateProgress(long bytesTransferred)
        {
            BytesTransferred = bytesTransferred;

            // that's odd! the transfer hasn't transitioned into InProgress yet. just ignore it..
            if (!StartTime.HasValue)
            {
                return;
            }

            // if the state is Completed, we're guaranteed to have both StartTime and EndTime
            // it's possible that StartTime = EndTime, and if that's the case, substitute 1ms for the duration
            if (State.HasFlag(TransferStates.Completed))
            {
                var duration = Math.Max(1, (EndTime.Value - StartTime.Value).TotalMilliseconds) / 1000d;
                var totalSpeed = (BytesTransferred - StartOffset) / duration;
                AverageSpeed = totalSpeed;

                return;
            }

            // if we've transferred all of the data but not yet transitioned into Completed, we won't have an EndTime
            // yet, so we'll have to use the current time; the transition to Completed will happen soon!
            if (Size.HasValue && BytesTransferred >= Size.Value)
            {
                var duration = Math.Max(1, (DateTime.UtcNow - StartTime.Value).TotalMilliseconds) / 1000d;
                var totalSpeed = (BytesTransferred - StartOffset) / duration;
                AverageSpeed = totalSpeed;

                return;
            }

            /*
                for all other updates, we want to use a moving average, and for that to work well we need to throttle
                it a bit and only update once per second so the math works

                we keep track of the last update, and if it hasn't been at least `progressUpdateLimit` ms since the last
                update, we'll do nothing.

                this means that AverageSpeed will be zero until at least `progressUpdateLimit` ms after the start of the
                transfer! and this is why we have special cases above for when the transfer is complete; otherwise very
                fast transfers will have an average speed of 0.
            */
            var ts = DateTime.UtcNow - (lastProgressTime ?? StartTime.Value);

            if (ts.TotalMilliseconds >= progressUpdateLimit)
            {
                var currentSpeed = (BytesTransferred - lastProgressBytes) / (ts.TotalMilliseconds / 1000d);
                AverageSpeed = !speedInitialized ? currentSpeed : ((currentSpeed - AverageSpeed) * speedAlpha) + AverageSpeed;
                speedInitialized = true;
                lastProgressTime = DateTime.UtcNow;
                lastProgressBytes = BytesTransferred;
            }
        }
    }
}