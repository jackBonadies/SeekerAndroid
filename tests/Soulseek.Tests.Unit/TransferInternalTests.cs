// <copyright file="TransferInternalTests.cs" company="JP Dillingham">
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
    using System.Threading;

    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class TransferInternalTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        internal void Instantiates_With_The_Given_Data(string username, string filename, int token)
        {
            var d = new TransferInternal(TransferDirection.Download, username, filename, token);

            Assert.Equal(username, d.Username);
            Assert.Equal(filename, d.Filename);
            Assert.Equal(token, d.Token);
        }

        [Trait("Category", "Properties")]
        [Theory(DisplayName = "Properties default to expected values"), AutoData]
        internal void Properties_Default_To_Expected_Values(string username, string filename, int token, TransferOptions options)
        {
            var d = new TransferInternal(TransferDirection.Download, username, filename, token, options);

            Assert.Null(d.Connection);
            Assert.Null(d.IPEndPoint);
            Assert.Null(d.RemoteToken);
            Assert.Null(d.Size);
            Assert.Equal(TransferStates.None, d.State);
            Assert.Equal(0, d.AverageSpeed);
            Assert.Equal(0, d.BytesTransferred);
            Assert.Equal(0, d.BytesRemaining);
            Assert.Null(d.ElapsedTime);
            Assert.Null(d.RemainingTime);
            Assert.Null(d.StartTime);
            Assert.Null(d.EndTime);
            Assert.Equal(0, d.PercentComplete);
            Assert.Null(d.RemoteToken);
            Assert.Equal(options, d.Options);
            Assert.Equal(0, d.StartOffset);
            Assert.Null(d.Exception);
            Assert.NotNull(d.RemoteTaskCompletionSource);
        }

        [Trait("Category", "Properties")]
        [Theory(DisplayName = "IPAddress and Port props return Connection props"), AutoData]
        internal void IPAddress_And_Port_Props_Return_Connection_Props(string username, string filename, int token, IPEndPoint endpoint)
        {
            var d = new TransferInternal(TransferDirection.Download, username, filename, token);

            var c = new Mock<IConnection>();
            c.Setup(m => m.IPEndPoint)
                .Returns(endpoint);

            d.Connection = c.Object;

            Assert.Equal(endpoint, d.IPEndPoint);
        }

        [Trait("Category", "Wait Key")]
        [Theory(DisplayName = "Wait key is expected value"), AutoData]
        internal void Wait_Key_Is_Expected_Value(string username, string filename, int token, TransferDirection direction)
        {
            var d = new TransferInternal(direction, username, filename, token);

            Assert.Equal(new WaitKey(Constants.WaitKey.Transfer, direction, username, filename, token), d.WaitKey);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "State transitions set time props")]
        internal void State_Transitions_Set_Time_Props()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            var s1 = d.StartTime;
            var e1 = d.EndTime;
            var et1 = d.ElapsedTime;
            var rt1 = d.RemainingTime;

            d.State = TransferStates.InProgress;

            var s2 = d.StartTime;
            var e2 = d.EndTime;
            var et2 = d.ElapsedTime;
            var rt2 = d.RemainingTime;

            d.State = TransferStates.Completed;

            Assert.Null(s1);
            Assert.Null(e1);
            Assert.Null(et1);
            Assert.Null(rt1);

            Assert.NotNull(s2);
            Assert.Null(e2);
            Assert.NotNull(et2);
            Assert.Null(rt2);

            Assert.NotNull(d.StartTime);
            Assert.NotNull(d.EndTime);
            Assert.NotNull(d.ElapsedTime);
            Assert.Null(d.RemainingTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "State transition into InProgress sets StartTime")]
        internal void State_Transition_Into_InProgress_Sets_StartTime()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Null(d.StartTime);

            d.State = TransferStates.InProgress;

            Assert.NotNull(d.StartTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "Subsequent state transition into InProgress does not change StartTime")]
        internal void Subsequent_State_Transition_Into_InProgress_Does_Not_Change_StartTime()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            d.State = TransferStates.InProgress;
            var first = d.StartTime;

#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests
            Thread.Sleep(1);
#pragma warning restore S2925 // "Thread.Sleep" should not be used in tests

            d.State = TransferStates.InProgress;

            Assert.Equal(first, d.StartTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "State transition into Completed sets EndTime")]
        internal void State_Transition_Into_Completed_Sets_EndTime()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);
            d.State = TransferStates.InProgress;

            Assert.Null(d.EndTime);

            d.State = TransferStates.Completed | TransferStates.Succeeded;

            Assert.NotNull(d.EndTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "Subsequent state transition into Completed does not change EndTime")]
        internal void Subsequent_State_Transition_Into_Completed_Does_Not_Change_EndTime()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            d.State = TransferStates.Completed;
            var first = d.EndTime;

#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests
            Thread.Sleep(1);
#pragma warning restore S2925 // "Thread.Sleep" should not be used in tests

            d.State = TransferStates.Completed;

            Assert.Equal(first, d.EndTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "State transition into Completed sets StartTime if transfer never went InProgress")]
        internal void State_Transition_Into_Completed_Sets_StartTime_If_Transfer_Never_Went_InProgress()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Null(d.StartTime);

            d.State = TransferStates.Completed | TransferStates.Succeeded;

            Assert.NotNull(d.EndTime);
            Assert.NotNull(d.StartTime);
            Assert.Equal(d.StartTime, d.EndTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "State transition into Completed updates progress")]
        internal void State_Transition_Into_Completed_Updates_Progress()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0)
            {
                Size = 9000,
            };

            // set BytesTransferred with a 'backdoor' to avoid calling UpdateProgress
            d.SetProperty(nameof(d.BytesTransferred), 4500);

            // hack in a start time to make the average math produce something greater than 0
            d.SetProperty(nameof(d.StartTime), DateTime.UtcNow.AddSeconds(-1));

            // ensure average speed has not been set yet
            Assert.Equal(0, d.AverageSpeed);

            d.State = TransferStates.Completed | TransferStates.Succeeded;

            Assert.True(d.AverageSpeed > 0);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "ElapsedTime returns null if StartTime is null")]
        internal void ElapsedTime_Returns_Null_If_StartTime_Is_Null()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Null(d.ElapsedTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "ElapsedTime is not null if StartTime is not null")]
        internal void ElapsedTime_Is_Not_Null_If_StartTime_Is_Not_Null()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            var s = new DateTime(2019, 4, 25, 0, 0, 0, DateTimeKind.Utc);

            d.SetProperty("StartTime", s);

            Assert.NotNull(d.ElapsedTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "ElapsedTime returns elapsed time between StartTime and EndTime")]
        internal void ElapsedTime_Returns_Elapsed_Time_Between_StartTime_And_EndTime()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            var s = new DateTime(2019, 4, 25, 0, 0, 0, DateTimeKind.Utc);
            var e = new DateTime(2019, 4, 26, 0, 0, 0, DateTimeKind.Utc);

            d.SetProperty("StartTime", s);
            d.SetProperty("EndTime", e);

            Assert.Equal(e - s, d.ElapsedTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "RemainingTime works")]
        internal void RemainingTime_Works()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Null(d.RemainingTime);

            d.SetProperty("AverageSpeed", 1);
            d.SetProperty("Size", 2L);
            d.SetProperty("BytesTransferred", 1);

            Assert.Equal(TimeSpan.FromSeconds(1), d.RemainingTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "PercentComplete works")]
        internal void PercentComplete_Works()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.PercentComplete);

            d.SetProperty("Size", 100L);
            d.SetProperty("BytesTransferred", 50);

            Assert.Equal(50, d.PercentComplete);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "Exception works")]
        internal void Exception_Works()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Null(d.Exception);

            var ex = new Exception("foo");
            d.SetProperty("Exception", ex);

            Assert.Equal(ex, d.Exception);
        }

        [Trait("Category", "StartOffset")]
        [Theory(DisplayName = "StartOffset setter sets BytesTransferred and other vars"), AutoData]
        internal void StartOffset_Setter_Sets_BytesTransferred_And_Other_Vars(long startOffset)
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.StartOffset);
            Assert.Equal(0, d.BytesTransferred);

            d.StartOffset = startOffset;

            Assert.Equal(startOffset, d.BytesTransferred);
            Assert.Equal(startOffset, d.GetField<long>("startOffset"));
            Assert.Equal(startOffset, d.GetField<double>("lastProgressBytes"));
        }

        [Trait("Category", "UpdateProgress")]
        [Fact(DisplayName = "UpdateProgress sets AverageSpeed initial value")]
        internal void UpdateProgress_Sets_AverageSpeed_To_Initial_Value()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.AverageSpeed);

            d.SetField("progressUpdateLimit", 0);
            d.SetProperty("State", TransferStates.InProgress);
            d.InvokeMethod("UpdateProgress", 100000);

            Assert.NotEqual(0, d.AverageSpeed);
        }

        [Trait("Category", "UpdateProgress")]
        [Fact(DisplayName = "UpdateProgress updates AverageSpeed on subsequent call")]
        internal void UpdateProgress_Updates_AverageSpeed_On_Subsequent_Call()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.AverageSpeed);

            d.SetField("progressUpdateLimit", 0);
            d.SetProperty("State", TransferStates.InProgress);
            d.InvokeMethod("UpdateProgress", 100000);

            var v1 = d.AverageSpeed;
            Assert.NotEqual(0, v1);

            d.InvokeMethod("UpdateProgress", 10);

            Assert.NotEqual(v1, d.AverageSpeed);
        }

        [Trait("Category", "UpdateProgress")]
        [Fact(DisplayName = "UpdateProgress ignores AverageSpeed if StartTime and lastProgressTime is null")]
        internal void UpdateProgress_Ignores_AverageSpeed_If_StarTime_And_lastProgressTime_Is_Null()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.AverageSpeed);

            d.InvokeMethod("UpdateProgress", 100000);

            Assert.Equal(0, d.AverageSpeed);
        }

        [Trait("Category", "UpdateProgress")]
        [Theory(DisplayName = "UpdateProgress calculates final speed when BytesTransferred equals Size without Completed state"), AutoData]
        internal void UpdateProgress_Calculates_Final_Speed_When_BytesTransferred_Equals_Size_Without_Completed_State(long size)
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0)
            {
                Size = size,
            };

            d.State = TransferStates.InProgress;

            // force StartTime back 1 second so the math works
            d.SetProperty(nameof(d.StartTime), DateTime.UtcNow.AddSeconds(-1));

            Assert.Equal(0, d.AverageSpeed);

            d.InvokeMethod("UpdateProgress", 10000L);
            d.UpdateProgress(10000L);

            Assert.True(d.AverageSpeed > 0);
        }

        [Trait("Category", "UpdateProgress")]
        [Fact(DisplayName = "UpdateProgress does not throw if size is null")]
        internal void UpdateProgress_Does_Not_Throw_If_Size_Is_Null()
        {
            var d = new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0);

            d.State = TransferStates.InProgress;

            // force StartTime back 1 second so the math works
            d.SetProperty(nameof(d.StartTime), DateTime.UtcNow.AddSeconds(-1));

            Assert.Equal(0, d.AverageSpeed);

            d.InvokeMethod("UpdateProgress", 10000L);
            d.UpdateProgress(10000L);

            Assert.True(d.AverageSpeed > 0);
        }
    }
}
