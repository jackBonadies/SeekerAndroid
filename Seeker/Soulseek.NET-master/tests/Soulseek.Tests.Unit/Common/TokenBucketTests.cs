// <copyright file="TokenBucketTests.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class TokenBucketTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given 0 count")]
        public void Throws_ArgumentOutOfRangeException_Given_0_Count()
        {
            var ex = Record.Exception(() => new TokenBucket(0, 1000));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given negative count")]
        public void Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            var ex = Record.Exception(() => new TokenBucket(-1, 1000));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given 0 interval")]
        public void Throws_ArgumentOutOfRangeException_Given_0_Interval()
        {
            var ex = Record.Exception(() => new TokenBucket(1000, 0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("interval", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given negative interval")]
        public void Throws_ArgumentOutOfRangeException_Given_Negative_Interval()
        {
            var ex = Record.Exception(() => new TokenBucket(1000, -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("interval", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Sets properties"), AutoData]
        public void Sets_Properties(int count, int interval)
        {
            using (var t = new TokenBucket(count, interval))
            {
                Assert.Equal(count, t.Capacity);
                Assert.Equal(interval, t.GetProperty<System.Timers.Timer>("Clock").Interval);
                Assert.Equal(count, t.GetField<long>("currentCount"));
                Assert.Equal(count, t.GetField<long>("currentCapacity"));
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given 0 count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_0_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCapacity(0));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given negative count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCapacity(-1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCapacity")]
        [Theory(DisplayName = "SetCapacity sets capacity"), AutoData]
        public void SetCapacity_Sets_Capacity(int count)
        {
            using (var t = new TokenBucket(10, 1000))
            {
                t.SetCapacity(count);

                Assert.Equal(count, t.Capacity);
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity does not change the active capacity until the next reset")]
        public void SetCapacity_Does_Not_Change_The_Active_Capacity_Until_The_Next_Reset()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.SetCapacity(10);

                Assert.Equal(100, t.GetField<long>("currentCapacity"));
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity does not change the available count until the next reset if the new capacity is lower")]
        public void SetCapacity_Does_Not_Change_The_Available_Count_Until_The_Next_Reset_If_The_New_Capacity_Is_Lower()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.SetCapacity(10);

                Assert.Equal(100, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity does not add tokens until the next reset if the new capacity is higher")]
        public void SetCapacity_Does_Not_Add_Tokens_Until_The_Next_Reset_If_The_New_Capacity_Is_Higher()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.SetCapacity(1000);

                Assert.Equal(100, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity fills the bucket to the new capacity on the next reset")]
        public async Task SetCapacity_Fills_The_Bucket_To_The_New_Capacity_On_The_Next_Reset()
        {
            using (var t = new TokenBucket(100, 50))
            {
                t.SetCapacity(1000);

                await Task.Delay(500);

                Assert.Equal(1000, t.GetField<long>("currentCapacity"));
                Assert.Equal(1000, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity lowers the bucket to the new capacity on the next reset")]
        public async Task SetCapacity_Lowers_The_Bucket_To_The_New_Capacity_On_The_Next_Reset()
        {
            using (var t = new TokenBucket(100, 50))
            {
                t.SetCapacity(10);

                await Task.Delay(500);

                Assert.Equal(10, t.GetField<long>("currentCapacity"));
                Assert.Equal(10, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync decrements count by requested count")]
        public async Task GetAsync_Decrements_Count_By_Requested_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns capacity if request exceeds capacity")]
        public async Task GetAsync_Returns_Capacity_If_Request_Exceeds_Capacity()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                int tokens = 0;
                var ex = await Record.ExceptionAsync(async () => tokens = await t.GetAsync(11));

                Assert.Null(ex);
                Assert.Equal(10, tokens);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns int.MaxValue if capacity exceeds int.MaxValue")]
        public async Task GetAsync_Returns_IntMaxValue_If_Capacity_Exceeds_IntMaxValue()
        {
            using (var t = new TokenBucket(int.MaxValue * 2L, 100000))
            {
                var granted = await t.GetAsync(int.MaxValue);

                Assert.Equal(int.MaxValue, granted);
                Assert.Equal((int.MaxValue * 2L) - int.MaxValue, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync uses the active capacity until the next reset")]
        public async Task GetAsync_Uses_The_Active_Capacity_Until_The_Next_Reset()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.SetCapacity(10);

                var granted = await t.GetAsync(100);

                Assert.Equal(100, granted);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns available tokens if request exceeds available count")]
        public async Task GetAsync_Returns_Available_Tokens_If_Request_Exceeds_Available_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.GetAsync(6);
                var count = await t.GetAsync(6);

                Assert.Equal(4, count);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync waits for reset if bucket is depleted")]
        public async Task GetAsync_Waits_For_Reset_If_Bucket_Is_Depleted()
        {
            using (var t = new TokenBucket(1, 10))
            {
                await t.GetAsync(1);
                await t.GetAsync(1);
                await t.GetAsync(1);

                Assert.True(true);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync grants tokens after waiting for reset")]
        public async Task GetAsync_Grants_Tokens_After_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(5, 250))
            {
                await t.GetAsync(5);

                var task = t.GetAsync(5);

                Assert.False(task.IsCompleted);

                var completed = await Task.WhenAny(task, Task.Delay(2000));

                Assert.Same(task, completed);
                Assert.Equal(5, await task);
            }
        }

        [Trait("Category", "GetAsync")]
        [Theory(DisplayName = "GetAsync with a zero or negative count grants zero")]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(int.MinValue)]
        public async Task GetAsync_With_A_Zero_Or_Negative_Count_Grants_Zero(int count)
        {
            using (var t = new TokenBucket(100, 100000))
            {
                var granted = await t.GetAsync(count);

                Assert.Equal(0, granted);
            }
        }

        [Trait("Category", "GetAsync")]
        [Theory(DisplayName = "GetAsync with a zero or negative count does not change the available count")]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task GetAsync_With_A_Zero_Or_Negative_Count_Does_Not_Change_The_Available_Count(int count)
        {
            using (var t = new TokenBucket(100, 100000))
            {
                await t.GetAsync(count);

                Assert.Equal(100, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Theory(DisplayName = "GetAsync with a zero or negative count returns immediately if the bucket is empty")]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task GetAsync_With_A_Zero_Or_Negative_Count_Returns_Immediately_If_The_Bucket_Is_Empty(int count)
        {
            using (var t = new TokenBucket(1, 100000))
            {
                await t.GetAsync(1);

                var task = t.GetAsync(count);

                Assert.True(task.IsCompleted);
                Assert.Equal(0, await task);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns no more than capacity if the bucket is overfilled")]
        public async Task GetAsync_Returns_No_More_Than_Capacity_If_The_Bucket_Is_Overfilled()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.Return(100);

                var granted = await t.GetAsync(150);

                Assert.Equal(100, granted);
                Assert.Equal(100, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync throws OperationCanceledException given a cancelled token")]
        public async Task GetAsync_Throws_OperationCanceledException_Given_A_Cancelled_Token()
        {
            using (var t = new TokenBucket(100, 100000))
            using (var cts = new CancellationTokenSource())
            {
                await cts.CancelAsync();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t.GetAsync(1, cts.Token));
                Assert.Equal(100, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync throws OperationCanceledException if cancelled while waiting for reset")]
        public async Task GetAsync_Throws_OperationCanceledException_If_Cancelled_While_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(1, 100000))
            using (var cts = new CancellationTokenSource())
            {
                await t.GetAsync(1);

                var task = t.GetAsync(1, cts.Token);

                await cts.CancelAsync();

                var completed = await Task.WhenAny(task, Task.Delay(1000));

                Assert.Same(task, completed);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync does not take tokens if cancelled while waiting for reset")]
        public async Task GetAsync_Does_Not_Take_Tokens_If_Cancelled_While_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(1, 100000))
            using (var cts = new CancellationTokenSource())
            {
                await t.GetAsync(1);

                var task = t.GetAsync(1, cts.Token);

                await cts.CancelAsync();

                Assert.Same(task, await Task.WhenAny(task, Task.Delay(1000)));
                await Record.ExceptionAsync(() => task);

                // the next caller can still get the tokens that were returned, so the cancelled request released the semaphore
                t.Return(1);

                var next = t.GetAsync(1);
                var completed = await Task.WhenAny(next, Task.Delay(1000));

                Assert.Same(next, completed);
                Assert.Equal(1, await next);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync does not change the bucket if cancelled while waiting for reset")]
        public async Task GetAsync_Does_Not_Change_The_Bucket_If_Cancelled_While_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(1, 100000))
            using (var cts = new CancellationTokenSource())
            {
                await t.GetAsync(1);

                var reset = t.GetField<TaskCompletionSource<bool>>("waitForReset");

                var cancelled = t.GetAsync(1, cts.Token);
                var other = t.GetAsync(1);

                await cts.CancelAsync();
                await Record.ExceptionAsync(() => cancelled);

                // the other caller is still waiting for the timer to replenish the bucket
                var completed = await Task.WhenAny(other, Task.Delay(500));

                Assert.NotSame(other, completed);
                Assert.Equal(0, t.GetField<long>("currentCount"));
                Assert.Equal(1, t.Capacity);
                Assert.Same(reset, t.GetField<TaskCompletionSource<bool>>("waitForReset"));
                Assert.False(reset.Task.IsCompleted);
            }
        }

        [Trait("Category", "Return")]
        [Theory(DisplayName = "Return does not change count given zero or negative")]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(int.MinValue)]
        public async Task Return_Does_Not_Change_Count_Given_Zero_Or_Negative(int count)
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetField<long>("currentCount"));

                t.Return(count);

                Assert.Equal(5, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return adds capacity given value larger than capacity")]
        public async Task Return_Adds_Capacity_Given_Value_Larger_Than_Capacity()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetField<long>("currentCount"));

                t.Return(50);

                Assert.Equal(15, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return adds given value")]
        public async Task Return_Adds_Given_Value()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetField<long>("currentCount"));

                t.Return(5);

                Assert.Equal(10, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return can overfill the bucket by up to 2x capacity")]
        public void Return_Can_Overfill_The_Bucket_By_Up_To_2x_Capacity()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                // returned tokens are added on top of a full bucket, allowing a burst
                t.Return(50);

                Assert.Equal(150, t.GetField<long>("currentCount"));

                // but the bucket never holds more than 2x capacity, no matter how many tokens are returned
                t.Return(100);
                t.Return(100);

                Assert.Equal(200, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return uses the active capacity until the next reset")]
        public void Return_Uses_The_Active_Capacity_Until_The_Next_Reset()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.SetCapacity(10);

                // with the new capacity (10) the count would be capped at 20; the active capacity (100) caps it at 200
                t.Return(100);

                Assert.Equal(200, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return allows a burst of no more than 2x capacity within a single interval")]
        public async Task Return_Allows_A_Burst_Of_No_More_Than_2x_Capacity_Within_A_Single_Interval()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.Return(100);
                t.Return(100);

                var first = await t.GetAsync(100);
                var second = await t.GetAsync(100);

                // the burst is spent, so a third request must wait for the next interval
                var third = t.GetAsync(100);
                var completed = await Task.WhenAny(third, Task.Delay(250));

                Assert.Equal(100, first);
                Assert.Equal(100, second);
                Assert.NotSame(third, completed);
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Reset discards an unused burst")]
        public async Task Reset_Discards_An_Unused_Burst()
        {
            using (var t = new TokenBucket(100, 50))
            {
                t.Return(100);

                await Task.Delay(500);

                Assert.Equal(100, t.GetField<long>("currentCount"));
            }
        }

        [Trait("Category", "Concurrency")]
        [Fact(DisplayName = "Concurrent Return and GetAsync keep the count between 0 and 2x capacity")]
        public async Task Concurrent_Return_And_GetAsync_Keep_The_Count_Between_0_And_2x_Capacity()
        {
            const int capacity = 1000000;
            const int maxRequest = 100;

            using (var t = new TokenBucket(capacity, 100000))
            {
                var outOfRange = 0;

                // each worker returns and then takes a small amount, so the bucket never empties and no request waits for a
                // reset. returns race each other and in-flight withdrawals, which exercises the compare-and-swap retries.
                var workers = Enumerable.Range(0, Environment.ProcessorCount * 2).Select(_ => Task.Run(async () =>
                {
                    for (int i = 0; i < 20000; i++)
                    {
                        var request = 1 + (i % maxRequest);

                        t.Return(request);
                        var granted = await t.GetAsync(request);

                        if (granted < 0 || granted > request)
                        {
                            Interlocked.Increment(ref outOfRange);
                        }
                    }
                })).ToArray();

                var all = Task.WhenAll(workers);
                var completed = await Task.WhenAny(all, Task.Delay(60000));

                Assert.Same(all, completed);
                Assert.Equal(0, outOfRange);
                Assert.InRange(t.GetField<long>("currentCount"), 0, capacity * 2);
            }
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Dispose does not throw if called more than once")]
        public void Dispose_Does_Not_Throw_If_Called_More_Than_Once()
        {
            var t = new TokenBucket(10, 100000);

            t.Dispose();
            var ex = Record.Exception(() => t.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Dispose without disposing marks the bucket disposed without disposing the clock")]
        public void Dispose_Without_Disposing_Marks_The_Bucket_Disposed_Without_Disposing_The_Clock()
        {
            var t = new TokenBucket(10, 100000);
            var clock = t.GetProperty<System.Timers.Timer>("Clock");

            try
            {
                t.InvokeMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, false);

                Assert.True(t.GetProperty<bool>("Disposed"));
                Assert.True(clock.Enabled);
                Assert.False(t.GetProperty<TaskCompletionSource<bool>>("Disposal").Task.IsCompleted);
            }
            finally
            {
                clock.Dispose();
            }
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "GetAsync throws ObjectDisposedException if the bucket is disposed")]
        public async Task GetAsync_Throws_ObjectDisposedException_If_The_Bucket_Is_Disposed()
        {
            var t = new TokenBucket(10, 100000);

            t.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => t.GetAsync(1));
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Dispose throws ObjectDisposedException to a request waiting for reset")]
        public async Task Dispose_Throws_ObjectDisposedException_To_A_Request_Waiting_For_Reset()
        {
            var t = new TokenBucket(1, 100000);

            await t.GetAsync(1);

            var task = t.GetAsync(1);

            t.Dispose();

            var completed = await Task.WhenAny(task, Task.Delay(1000));

            Assert.Same(task, completed);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => task);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Dispose throws ObjectDisposedException to requests queued behind a request waiting for reset")]
        public async Task Dispose_Throws_ObjectDisposedException_To_Requests_Queued_Behind_A_Request_Waiting_For_Reset()
        {
            var t = new TokenBucket(1, 100000);

            await t.GetAsync(1);

            var waiting = t.GetAsync(1);
            var queued = new[] { t.GetAsync(1), t.GetAsync(1) };

            t.Dispose();

            var all = Task.WhenAll(queued.Prepend(waiting).Select(task => Record.ExceptionAsync(() => task)));
            var completed = await Task.WhenAny(all, Task.Delay(1000));

            Assert.Same(all, completed);
            Assert.All(await all, ex => Assert.IsType<ObjectDisposedException>(ex));
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Reset does not throw if the bucket is disposed")]
        public void Reset_Does_Not_Throw_If_The_Bucket_Is_Disposed()
        {
            var t = new TokenBucket(1, 100000);

            t.Dispose();

            // the timer can still fire once after it is disposed
            var ex = Record.Exception(() => t.InvokeMethod("Reset"));

            Assert.Null(ex);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Dispose faults the disposal task with ObjectDisposedException")]
        public async Task Dispose_Faults_The_Disposal_Task_With_ObjectDisposedException()
        {
            var t = new TokenBucket(1, 100000);
            var disposal = t.GetProperty<TaskCompletionSource<bool>>("Disposal");

            Assert.False(disposal.Task.IsCompleted);

            t.Dispose();

            Assert.True(disposal.Task.IsFaulted);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => disposal.Task);
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Dispose throws ObjectDisposedException to a request waiting for reset if the timer ticks after disposal")]
        public async Task Dispose_Throws_ObjectDisposedException_To_A_Request_Waiting_For_Reset_If_The_Timer_Ticks_After_Disposal()
        {
            var t = new TokenBucket(1, 100000);

            await t.GetAsync(1);

            t.Dispose();

            // a tick that was already in flight swaps in a new reset signal after disposal; nothing will ever complete it
            t.InvokeMethod("Reset");

            // simulate a request that passed the Disposed check before Dispose() ran, then captured the new reset signal
            t.SetProperty("Disposed", false);

            var task = t.GetAsync(1);
            var completed = await Task.WhenAny(task, Task.Delay(1000));

            Assert.Same(task, completed);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => task);
        }
    }
}
