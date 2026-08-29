// <copyright file="Transfer.cs" company="JP Dillingham">
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

    /// <summary>
    ///     A single file transfer.
    /// </summary>
    public class Transfer
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Transfer"/> class.
        /// </summary>
        /// <param name="direction">The transfer direction.</param>
        /// <param name="username">The username of the peer to or from which the file is to be transferred.</param>
        /// <param name="filename">The filename of the file to be transferred.</param>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="state">The state of the transfer.</param>
        /// <param name="size">The size of the file to be transferred, in bytes.</param>
        /// <param name="startOffset">The start offset of the transfer, in bytes.</param>
        /// <param name="bytesTransferred">The total number of bytes transferred.</param>
        /// <param name="averageSpeed">The current average download speed.</param>
        /// <param name="startTime">
        ///     The UTC time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.
        /// </param>
        /// <param name="endTime">
        ///     The UTC time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.
        /// </param>
        /// <param name="remoteToken">The remote unique token for the transfer.</param>
        /// <param name="ipEndPoint">The ip endpoint of the remote transfer connection, if one has been established.</param>
        /// <param name="exception">The Exception that caused the failure of the transfer, if applicable.</param>
        public Transfer(
            TransferDirection direction,
            string username,
            string filename,
            int token,
            TransferStates state,
            long size,
            long startOffset,
            long bytesTransferred = 0,
            double averageSpeed = 0,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int? remoteToken = null,
            IPEndPoint ipEndPoint = null,
            Exception exception = null)
        {
            Direction = direction;
            Username = username;
            Filename = filename;
            Token = token;
            State = state;
            Size = size;
            StartOffset = startOffset;
            BytesTransferred = bytesTransferred;
            AverageSpeed = averageSpeed;
            StartTime = startTime;
            EndTime = endTime;
            RemoteToken = remoteToken;
            IPEndPoint = ipEndPoint;
            Exception = exception;

            BytesRemaining = Size - BytesTransferred;
            ElapsedTime = StartTime == null ? null : (TimeSpan?)((EndTime ?? DateTime.UtcNow) - StartTime.Value);
            PercentComplete = Size == 0 ? 0 : (BytesTransferred / (double)Size) * 100;
            RemainingTime = AverageSpeed == 0 ? null : (TimeSpan?)TimeSpan.FromSeconds(BytesRemaining / AverageSpeed);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Transfer"/> class.
        /// </summary>
        /// <param name="transferInternal">The internal instance from which to copy data.</param>
        internal Transfer(TransferInternal transferInternal)
            : this(
                transferInternal.Direction,
                transferInternal.Username,
                transferInternal.Filename,
                transferInternal.Token,
                transferInternal.State,
                transferInternal.Size ?? 0,
                transferInternal.StartOffset,
                transferInternal.BytesTransferred,
                transferInternal.AverageSpeed,
                transferInternal.StartTime,
                transferInternal.EndTime,
                transferInternal.RemoteToken,
                transferInternal.IPEndPoint,
                transferInternal.Exception)
        {
        }

        /// <summary>
        ///     Gets the current average transfer speed.
        /// </summary>
        public double AverageSpeed { get; }

        /// <summary>
        ///     Gets the number of remaining bytes to be transferred.
        /// </summary>
        public long BytesRemaining { get; }

        /// <summary>
        ///     Gets the total number of bytes transferred.
        /// </summary>
        public long BytesTransferred { get; }

        /// <summary>
        ///     Gets the transfer direction.
        /// </summary>
        public TransferDirection Direction { get; }

        /// <summary>
        ///     Gets the current duration of the transfer, if it has been started.
        /// </summary>
        public TimeSpan? ElapsedTime { get; }

        /// <summary>
        ///     Gets the UTC time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.
        /// </summary>
        public DateTime? EndTime { get; }

        /// <summary>
        ///     Gets the <see cref="Exception"/> that caused the failure of the transfer, if applicable.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Gets the filename of the file to be transferred.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the ip endpoint of the remote transfer connection, if one has been established.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Gets the current progress in percent.
        /// </summary>
        public double PercentComplete { get; }

        /// <summary>
        ///     Gets the projected remaining duration of the transfer.
        /// </summary>
        public TimeSpan? RemainingTime { get; }

        /// <summary>
        ///     Gets the remote unique token for the transfer.
        /// </summary>
        public int? RemoteToken { get; }

        /// <summary>
        ///     Gets the size of the file to be transferred, in bytes.
        /// </summary>
        public long Size { get; }

        /// <summary>
        ///     Gets the starting offset of the transfer, in bytes.
        /// </summary>
        public long StartOffset { get; }

        /// <summary>
        ///     Gets the UTC time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.
        /// </summary>
        public DateTime? StartTime { get; }

        /// <summary>
        ///     Gets the state of the transfer.
        /// </summary>
        public TransferStates State { get; }

        /// <summary>
        ///     Gets the unique token for the transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer to or from which the file is to be transferred.
        /// </summary>
        public string Username { get; }
    }
}