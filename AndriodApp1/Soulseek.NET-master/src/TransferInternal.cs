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
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     A single file transfer.
    /// </summary>
    internal sealed class TransferInternal
    {
        private readonly int progressUpdateLimit = 1000;
        private readonly double speedAlpha = 2f / 10;
        private double lastProgressBytes = 0;
        private long startOffset = 0;
        private DateTime? lastProgressTime = null;
        private bool speedInitialized = false;
        private TransferStates state = TransferStates.None;

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
        public DateTime? EndTime { get; private set; }

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
                lastProgressBytes = value;
                startOffset = value;
            }
        }

        /// <summary>
        ///     Gets the UTC time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.
        /// </summary>
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
                if (!state.HasFlag(TransferStates.InProgress) && value.HasFlag(TransferStates.InProgress))
                {
                    StartTime = DateTime.UtcNow;
                    EndTime = null;
                }
                else if (!state.HasFlag(TransferStates.Completed) && value.HasFlag(TransferStates.Completed))
                {
                    EndTime = DateTime.UtcNow;
                }

                state = value;
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
        public WaitKey WaitKey => new WaitKey(Constants.WaitKey.Transfer, Direction, Username, Filename, Token);

        /// <summary>
        ///     Updates the transfer progress.
        /// </summary>
        /// <param name="bytesTransferred">The total number of bytes transferred.</param>
        public void UpdateProgress(long bytesTransferred)
        {
            BytesTransferred = bytesTransferred;

            var ts = DateTime.UtcNow - (lastProgressTime ?? StartTime);

            if (ts.HasValue && ((!speedInitialized && bytesTransferred > 0) || ts.Value.TotalMilliseconds >= progressUpdateLimit || State.HasFlag(TransferStates.Completed)))
            {
                var currentSpeed = (bytesTransferred - lastProgressBytes) / (ts.Value.TotalMilliseconds / 1000d);
                AverageSpeed = !speedInitialized ? currentSpeed : ((currentSpeed - AverageSpeed) * speedAlpha) + AverageSpeed;
                speedInitialized = true;
                lastProgressTime = DateTime.UtcNow;
                lastProgressBytes = bytesTransferred;
            }
        }
    }
}