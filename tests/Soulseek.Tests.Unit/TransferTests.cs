// <copyright file="TransferTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Net;
    using AutoFixture.Xunit2;
    using Xunit;

    public class TransferTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with expected data"), AutoData]
        public void Instantiates_With_Expected_Data(
            TransferDirection direction,
            string username,
            string filename,
            int token,
            TransferStates state,
            long size,
            long startOffset,
            long bytesTransferred,
            double averageSpeed,
            DateTime? startTime,
            DateTime? endTime,
            int? remoteToken,
            IPEndPoint endpoint,
            Exception exception)
        {
            var t = new Transfer(
                direction,
                username,
                filename,
                token,
                state,
                size,
                startOffset,
                bytesTransferred,
                averageSpeed,
                startTime,
                endTime,
                remoteToken,
                endpoint,
                exception);

            Assert.Equal(direction, t.Direction);
            Assert.Equal(username, t.Username);
            Assert.Equal(filename, t.Filename);
            Assert.Equal(token, t.Token);
            Assert.Equal(state, t.State);
            Assert.Equal(size, t.Size);
            Assert.Equal(startOffset, t.StartOffset);
            Assert.Equal(bytesTransferred, t.BytesTransferred);
            Assert.Equal(averageSpeed, t.AverageSpeed);
            Assert.Equal(startTime, t.StartTime);
            Assert.Equal(endTime, t.EndTime);
            Assert.Equal(remoteToken, t.RemoteToken);
            Assert.Equal(endpoint, t.IPEndPoint);
            Assert.Equal(exception, t.Exception);

            Assert.Equal(t.Size - t.BytesTransferred, t.BytesRemaining);
            Assert.Equal(t.EndTime - t.StartTime, t.ElapsedTime);
            Assert.Equal((t.BytesTransferred / (double)t.Size) * 100, t.PercentComplete);
            Assert.NotNull(t.RemainingTime);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with expected data given TransferInternal"), AutoData]
        internal void Instantiates_With_Expected_Data_Given_TransferInternal(string username, string filename, int token)
        {
            var ex = new Exception("foo");

            var i = new TransferInternal(TransferDirection.Download, username, filename, token);
            i.Exception = ex;

            var t = new Transfer(i);

            Assert.Equal(i.Direction, t.Direction);
            Assert.Equal(i.Username, t.Username);
            Assert.Equal(i.Filename, t.Filename);
            Assert.Equal(i.Token, t.Token);
            Assert.Equal(i.State, t.State);
            Assert.Equal(0, t.Size);
            Assert.Equal(i.StartOffset, t.StartOffset);
            Assert.Equal(i.BytesTransferred, t.BytesTransferred);
            Assert.Equal(i.AverageSpeed, t.AverageSpeed);
            Assert.Equal(i.StartTime, t.StartTime);
            Assert.Equal(i.EndTime, t.EndTime);
            Assert.Equal(i.RemoteToken, t.RemoteToken);
            Assert.Equal(i.IPEndPoint, t.IPEndPoint);
            Assert.Equal(ex, t.Exception);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "ElapsedTime returns null if StartTime is null")]
        internal void ElapsedTime_Returns_Null_If_StartTime_Is_Null()
        {
            var i = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);
            var d = new Transfer(i);

            Assert.Null(d.ElapsedTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "ElapsedTime is not null if StartTime is not null")]
        internal void ElapsedTime_Is_Not_Null_If_StartTime_Is_Not_Null()
        {
            var i = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            var s = new DateTime(2019, 4, 25, 0, 0, 0, DateTimeKind.Utc);

            i.SetProperty("StartTime", s);

            var d = new Transfer(i);

            Assert.NotNull(d.ElapsedTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "ElapsedTime returns elapsed time between StartTime and EndTime")]
        internal void ElapsedTime_Returns_Elapsed_Time_Between_StartTime_And_EndTime()
        {
            var i = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            var s = new DateTime(2019, 4, 25, 0, 0, 0, DateTimeKind.Utc);
            var e = new DateTime(2019, 4, 26, 0, 0, 0, DateTimeKind.Utc);

            i.SetProperty("StartTime", s);
            i.SetProperty("EndTime", e);

            var d = new Transfer(i);

            Assert.Equal(e - s, d.ElapsedTime);
        }

        [Trait("Category", "PercentComplete")]
        [Fact(DisplayName = "PercentComplete returns 0 if Size is 0")]
        internal void PercentComplete_Returns_0_If_Size_Is_0()
        {
            var i = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0)
            {
                Size = 0,
            };

            var d = new Transfer(i);

            Assert.Equal(0, d.PercentComplete);
        }

        [Trait("Category", "RemainingTime")]
        [Fact(DisplayName = "RemainingTime returns null if AverageSpeed is null")]
        internal void RemainingTime_Returns_Null_If_AverageSpeed_Is_Null()
        {
            var i = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);
            var d = new Transfer(i);

            Assert.Null(d.RemainingTime);
        }
    }
}
