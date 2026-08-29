// <copyright file="TransferOptionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Options
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class TransferOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_Given_Data(
            bool seekInput,
            bool disposeInput,
            bool disposeOutput,
            Func<Transfer, int, CancellationToken, Task<int>> governor,
            Action<(TransferStates PreviousState, Transfer Transfer)> stateChanged,
            int maximumLingerTime,
            Action<(long PreviousBytesTransferred, Transfer Transfer)> progressUpdated,
            Func<Transfer, CancellationToken, Task> acquireSlot,
            Action<Transfer, int, int, int> reporter,
            Action<Transfer> slotReleased)
        {
            var o = new TransferOptions(
                governor,
                stateChanged,
                progressUpdated,
                acquireSlot,
                slotReleased,
                reporter,
                maximumLingerTime,
                seekInput,
                disposeInput,
                disposeOutput);

            Assert.Equal(seekInput, o.SeekInputStreamAutomatically);
            Assert.Equal(disposeInput, o.DisposeInputStreamOnCompletion);
            Assert.Equal(disposeOutput, o.DisposeOutputStreamOnCompletion);
            Assert.Equal(governor, o.Governor);
            Assert.Equal(stateChanged, o.StateChanged);
            Assert.Equal(progressUpdated, o.ProgressUpdated);
            Assert.Equal(maximumLingerTime, o.MaximumLingerTime);
            Assert.Equal(acquireSlot, o.SlotAwaiter);
            Assert.Equal(slotReleased, o.SlotReleased);
            Assert.Equal(reporter, o.Reporter);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with defaults")]
        public async Task Instantiates_With_Defaults()
        {
            var o = new TransferOptions();

            Assert.True(o.DisposeInputStreamOnCompletion);
            Assert.True(o.DisposeOutputStreamOnCompletion);

            var ex = await Record.ExceptionAsync(() => o.Governor(null, 1, CancellationToken.None));
            Assert.Null(ex);

            var ex2 = await Record.ExceptionAsync(() => o.SlotAwaiter(null, CancellationToken.None));
            Assert.Null(ex2);

            Assert.Null(o.StateChanged);
            Assert.Null(o.ProgressUpdated);
            Assert.Null(o.SlotReleased);
        }

        [Trait("Category", "WithAdditionalStateChanged")]
        [Theory(DisplayName = "WithAdditionalStateChanged returns copy other than StateChanged"), AutoData]
        public void WithAdditionalStateChanged_Returns_Copy_Other_Than_StateChanged(
            bool seekInput,
            bool disposeInput,
            bool disposeOutput,
            Func<Transfer, int, CancellationToken, Task<int>> governor,
            Action<(TransferStates PreviousState, Transfer Transfer)> stateChanged,
            int maximumLingerTime,
            Action<(long PreviousBytesTransferred, Transfer Transfer)> progressUpdated,
            Func<Transfer, CancellationToken, Task> acquireSlot,
            Action<Transfer> slotReleased)
        {
            var n = new TransferOptions(
                governor: governor,
                stateChanged: stateChanged,
                progressUpdated: progressUpdated,
                slotAwaiter: acquireSlot,
                slotReleased: slotReleased,
                maximumLingerTime: maximumLingerTime,
                seekInputStreamAutomatically: seekInput,
                disposeInputStreamOnCompletion: disposeInput,
                disposeOutputStreamOnCompletion: disposeOutput);

            var o = n.WithAdditionalStateChanged(null);

            Assert.Equal(seekInput, o.SeekInputStreamAutomatically);
            Assert.Equal(disposeInput, o.DisposeInputStreamOnCompletion);
            Assert.Equal(disposeOutput, o.DisposeOutputStreamOnCompletion);
            Assert.Equal(governor, o.Governor);
            Assert.Equal(progressUpdated, o.ProgressUpdated);
            Assert.Equal(maximumLingerTime, o.MaximumLingerTime);
            Assert.Equal(acquireSlot, o.SlotAwaiter);
            Assert.Equal(slotReleased, o.SlotReleased);

            Assert.NotEqual(stateChanged, o.StateChanged);
        }

        [Trait("Category", "WithAdditionalStateChanged")]
        [Fact(DisplayName = "WithAdditionalStateChanged returns copy that executes both StateChanged")]
        public void WithAdditionalStateChanged_Returns_Copy_That_Executes_Both_StateChanged()
        {
            var one = false;
            var two = false;

            var n = new TransferOptions(stateChanged: (_) => { one = true; });

            var o = n.WithAdditionalStateChanged((_) => { two = true; });

            o.StateChanged(default);

            Assert.True(one);
            Assert.True(two);
        }

        [Trait("Category", "WithAdditionalStateChanged")]
        [Fact(DisplayName = "WithAdditionalStateChanged returns copy that does not throw if both StateChanged are null")]
        public void WithAdditionalStateChanged_Returns_Copy_That_Does_Not_Throw_If_Both_StateChanged_Are_Null()
        {
            var n = new TransferOptions(stateChanged: null);

            var o = n.WithAdditionalStateChanged(null);

            var ex = Record.Exception(() => o.StateChanged(default));

            Assert.Null(ex);
        }

        [Trait("Category", "WithDisposalOptions")]
        [Theory(DisplayName = "WithDisposalOptions returns unchanged copy if both options are null"), AutoData]
        public void WithAdditionalStateChanged_Returns_Unchanged_Copy_If_Both_Options_Are_Null(
            bool seekInput,
            bool disposeInput,
            bool disposeOutput,
            Func<Transfer, int, CancellationToken, Task<int>> governor,
            Action<(TransferStates PreviousState, Transfer Transfer)> stateChanged,
            int maximumLingerTime,
            Action<(long PreviousBytesTransferred, Transfer Transfer)> progressUpdated,
            Func<Transfer, CancellationToken, Task> acquireSlot,
            Action<Transfer> slotReleased)
        {
            var n = new TransferOptions(
                governor: governor,
                stateChanged: stateChanged,
                progressUpdated: progressUpdated,
                slotAwaiter: acquireSlot,
                slotReleased: slotReleased,
                maximumLingerTime: maximumLingerTime,
                seekInputStreamAutomatically: seekInput,
                disposeInputStreamOnCompletion: disposeInput,
                disposeOutputStreamOnCompletion: disposeOutput);

            var o = n.WithDisposalOptions();

            Assert.Equal(governor, o.Governor);
            Assert.Equal(stateChanged, o.StateChanged);
            Assert.Equal(progressUpdated, o.ProgressUpdated);
            Assert.Equal(acquireSlot, o.SlotAwaiter);
            Assert.Equal(slotReleased, o.SlotReleased);
            Assert.Equal(maximumLingerTime, o.MaximumLingerTime);
            Assert.Equal(seekInput, o.SeekInputStreamAutomatically);
            Assert.Equal(disposeInput, o.DisposeInputStreamOnCompletion);
            Assert.Equal(disposeOutput, o.DisposeOutputStreamOnCompletion);
        }

        [Trait("Category", "WithDisposalOptions")]
        [Theory(DisplayName = "WithDisposalOptions returns changed copy if both options are specified"), AutoData]
        public void WithAdditionalStateChanged_Returns_Changed_Copy_If_Both_Options_Are_Specified(
            bool seekInput,
            bool disposeInput,
            bool disposeOutput,
            Func<Transfer, int, CancellationToken, Task<int>> governor,
            Action<(TransferStates PreviousState, Transfer Transfer)> stateChanged,
            int maximumLingerTime,
            Action<(long PreviousBytesTransferred, Transfer Transfer)> progressUpdated,
            Func<Transfer, CancellationToken, Task> acquireSlot,
            Action<Transfer> slotReleased)
        {
            var n = new TransferOptions(
                governor: governor,
                stateChanged: stateChanged,
                progressUpdated: progressUpdated,
                slotAwaiter: acquireSlot,
                slotReleased: slotReleased,
                maximumLingerTime: maximumLingerTime,
                seekInputStreamAutomatically: seekInput,
                disposeInputStreamOnCompletion: !disposeInput,
                disposeOutputStreamOnCompletion: !disposeOutput);

            var o = n.WithDisposalOptions(
                disposeInputStreamOnCompletion: disposeInput,
                disposeOutputStreamOnCompletion: disposeOutput);

            Assert.Equal(governor, o.Governor);
            Assert.Equal(stateChanged, o.StateChanged);
            Assert.Equal(progressUpdated, o.ProgressUpdated);
            Assert.Equal(acquireSlot, o.SlotAwaiter);
            Assert.Equal(slotReleased, o.SlotReleased);
            Assert.Equal(maximumLingerTime, o.MaximumLingerTime);
            Assert.Equal(seekInput, o.SeekInputStreamAutomatically);
            Assert.Equal(disposeInput, o.DisposeInputStreamOnCompletion);
            Assert.Equal(disposeOutput, o.DisposeOutputStreamOnCompletion);
        }
    }
}
