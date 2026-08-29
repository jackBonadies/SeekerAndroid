// <copyright file="WaiterTests.cs" company="JP Dillingham">
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
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging;
    using Xunit;
    using static Soulseek.Waiter;

    public class WaiterTests
    {
        [Trait("Category", "Wait Completion")]
        [Fact(DisplayName = "Complete dequeues wait")]
        public async Task Complete_Dequeues_Wait()
        {
            using (var waiter = new Waiter())
            {
                var result = Guid.NewGuid();
                var task = waiter.Wait<Guid>(new WaitKey(MessageCode.Server.Login));
                waiter.Complete(new WaitKey(MessageCode.Server.Login), result);

                var waitResult = await task;

                var key = new WaitKey(MessageCode.Server.Login);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");

                Assert.Equal(result, waitResult);

                Assert.False(waits.TryGetValue(key, out _));
            }
        }

        [Trait("Category", "Wait Cancellation")]
        [Fact(DisplayName = "Cancel dequeues wait and cancels task")]
        public async Task Cancel_Dequeues_Wait_And_Cancels_Task()
        {
            using (var waiter = new Waiter())
            {
                var key = new WaitKey(MessageCode.Server.Login);
                var task = waiter.Wait(key);

                waiter.Cancel(key);

                var ex = await Record.ExceptionAsync(() => task);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");

                Assert.NotNull(ex);
                Assert.IsType<TaskCanceledException>(ex);

                Assert.False(waits.TryGetValue(key, out _));
            }
        }

        [Trait("Category", "Wait Timeout")]
        [Fact(DisplayName = "Timeout dequeues wait and throws task with TimeoutException")]
        public async Task Timeout_Dequeues_Wait_And_Throws_Task_With_TimeoutException()
        {
            using (var waiter = new Waiter())
            {
                var key = new WaitKey(MessageCode.Server.Login);
                var task = waiter.Wait(key);

                waiter.Timeout(key);

                var ex = await Record.ExceptionAsync(() => task);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");

                Assert.NotNull(ex);
                Assert.IsType<TimeoutException>(ex);

                Assert.False(waits.TryGetValue(key, out _));
            }
        }

        [Trait("Category", "HasWait")]
        [Fact(DisplayName = "HasWait returns true when waitkey exists")]
        public void HasWait_Returns_True_When_WaitKey_Exists()
        {
            using (var waiter = new Waiter())
            {
                var waitKey = new WaitKey("foo");

                waiter.Wait(waitKey);

                Assert.True(waiter.HasWait(waitKey));
            }
        }

        [Trait("Category", "HasWait")]
        [Fact(DisplayName = "HasWait returns false when no waitkey exists")]
        public void HasWait_Returns_False_When_No_WaitKey_Exists()
        {
            using (var waiter = new Waiter())
            {
                var waitKey = new WaitKey("foo");

                Assert.False(waiter.HasWait(waitKey));
            }
        }

        [Trait("Category", "Wait Completion")]
        [Fact(DisplayName = "Complete for missing wait does not throw")]
        public void Complete_For_Missing_Wait_Does_Not_Throw()
        {
            using (var waiter = new Waiter())
            {
                var ex = Record.Exception(() => waiter.Complete<object>(new WaitKey(MessageCode.Server.AddPrivilegedUser), null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Wait Completion")]
        [Fact(DisplayName = "Complete does not remove queue if additional waits are enqueued")]
        public void Complete_Does_Not_Remove_Queue_If_Additional_Waits_Are_Enqueued()
        {
            int CountWaits(Waiter waiter, WaitKey key)
            {
                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                return queue?.Count ?? 0;
            }

            using (var waiter = new Waiter())
            {
                var key = new WaitKey("foo");

                waiter.Wait(key);
                waiter.Wait(key);

                Assert.Equal(2, CountWaits(waiter, key));

                waiter.Complete(key);

                Assert.Equal(1, CountWaits(waiter, key));
            }
        }

        [Trait("Category", "Wait Completion")]
        [Fact(DisplayName = "Non generic Complete for missing wait does not throw")]
        public void Non_Generic_Complete_For_Missing_Wait_Does_Not_Throw()
        {
            using (var waiter = new Waiter())
            {
                var ex = Record.Exception(() => waiter.Complete(new WaitKey(MessageCode.Server.AddPrivilegedUser)));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Wait Timeout")]
        [Fact(DisplayName = "Expiration ignores non-timed out waits")]
        public void Expiration_Ignores_Non_Timed_Out_Waits()
        {
            var key = new WaitKey(MessageCode.Server.Login);

            using (var waiter = new Waiter(0))
            {
                Task<object> task = waiter.Wait<object>(key);
                waiter.Wait<object>(key, 30000);

                object result = null;

                var ex = Record.Exception(() => result = task.Result);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                try
                {
                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);

                    Assert.NotEmpty(waits);
                    Assert.Single(waits);

                    Assert.NotNull(queue);
                    Assert.Single(queue);
                }
                finally
                {
                    wait.Dispose();
                }
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiate with DefaultTimeout")]
        public void Instantiate_With_DefaultTimeout()
        {
            var timeout = new Random().Next();

            Waiter t = null;
            var ex = Record.Exception(() => t = new Waiter(timeout));

            Assert.Null(ex);
            Assert.NotNull(t);
            Assert.Equal(timeout, t.DefaultTimeout);

            t.Dispose();
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiate with empty constructor")]
        public void Instantiate_With_Empty_Constructor()
        {
            Waiter t = null;
            var ex = Record.Exception(() => t = new Waiter());

            var defaultConst = t.GetField<int>("DefaultTimeoutValue");

            Assert.Null(ex);
            Assert.NotNull(t);
            Assert.Equal(defaultConst, t.DefaultTimeout);

            t.Dispose();
        }

        [Trait("Category", "Wait Creation")]
        [Fact(DisplayName = "Wait for subsequent MessageCode enqueues Wait")]
        public void Wait_For_Subsequent_MessageCode_Enqueues_Wait()
        {
            using (var waiter = new Waiter())
            {
                var task1 = waiter.Wait<object>(new WaitKey(MessageCode.Server.Login));
                var task2 = waiter.Wait<object>(new WaitKey(MessageCode.Server.Login));

                var key = new WaitKey(MessageCode.Server.Login);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);

                Assert.IsType<Task<object>>(task1);
                Assert.NotNull(task1);
                Assert.Equal(TaskStatus.WaitingForActivation, task1.Status);

                Assert.IsType<Task<object>>(task2);
                Assert.NotNull(task2);
                Assert.Equal(TaskStatus.WaitingForActivation, task2.Status);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Equal(2, queue.Count);
            }
        }

        [Trait("Category", "Wait Creation")]
        [Theory(DisplayName = "Wait invocation creates valid Wait")]
        [InlineData(MessageCode.Server.Login, null, null)]
        [InlineData(MessageCode.Server.Login, "token", null)]
        [InlineData(MessageCode.Server.Login, null, 10000)]
        [InlineData(MessageCode.Server.Login, "token", 10000)]
        internal void Wait_Invocation_Creates_Valid_Wait(MessageCode.Server code, string token, int? timeout)
        {
            var key = new WaitKey(code, token);

            using (var waiter = new Waiter())
            {
                Task<object> task = waiter.Wait<object>(key, timeout);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                try
                {
                    Assert.IsType<Task<object>>(task);
                    Assert.NotNull(task);
                    Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                    Assert.NotEmpty(waits);
                    Assert.Single(waits);

                    Assert.NotNull(queue);
                    Assert.Single(queue);

                    if (timeout != null)
                    {
                        Assert.Equal(timeout, wait.Timeout);
                    }
                }
                finally
                {
                    wait.Dispose();
                }
            }
        }

        [Trait("Category", "Wait Creation")]
        [Theory(DisplayName = "Non generic Wait invocation creates valid Wait")]
        [InlineData(MessageCode.Server.Login, null, null)]
        [InlineData(MessageCode.Server.Login, "token", null)]
        [InlineData(MessageCode.Server.Login, null, 10000)]
        [InlineData(MessageCode.Server.Login, "token", 10000)]
        internal void Non_Generic_Wait_Invocation_Creates_Valid_Wait(MessageCode.Server code, string token, int? timeout)
        {
            var key = new WaitKey(code, token);

            using (var waiter = new Waiter())
            {
                Task task = waiter.Wait(key, timeout);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                try
                {
                    Assert.IsType<Task<object>>(task);
                    Assert.NotNull(task);
                    Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                    Assert.NotEmpty(waits);
                    Assert.Single(waits);

                    Assert.NotNull(queue);
                    Assert.Single(queue);

                    if (timeout != null)
                    {
                        Assert.Equal(timeout, wait.Timeout);
                    }
                }
                finally
                {
                    wait.Dispose();
                }
            }
        }

        [Trait("Category", "Wait Timeout")]
        [Fact(DisplayName = "Wait throws and is dequeued when timing out")]
        public void Wait_Throws_And_Is_Dequeued_When_Timing_out()
        {
            var key = new WaitKey(MessageCode.Server.Login);

            using (var waiter = new Waiter(0))
            {
                Task<object> task = waiter.Wait<object>(key);
                object result = null;

                // stick another wait in the same queue to prevent the disposal logic from removing
                // the dictionary record before we can inspect it
                waiter.Wait<object>(key, timeout: 99999);

                var ex = Record.Exception(() => result = task.Result);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                try
                {
                    Assert.NotNull(ex);
                    Assert.IsType<TimeoutException>(ex.InnerException);

                    Assert.NotEmpty(waits);
                    Assert.Single(waits);

                    Assert.NotNull(queue);
                    Assert.Single(queue);
                }
                finally
                {
                    wait.Dispose();
                }
            }
        }

        [Trait("Category", "Wait Cancellation")]
        [Fact(DisplayName = "Wait throws and is dequeued when cancelled")]
        public void Wait_Throws_And_Is_Dequeued_When_Cancelled()
        {
            using (var tcs = new CancellationTokenSource())
            {
                tcs.CancelAfter(100);

                var key = new WaitKey(MessageCode.Server.Login);

                using (var waiter = new Waiter(0))
                {
                    Task<object> task = waiter.Wait<object>(key, 9999, tcs.Token);
                    object result = null;

                    // stick another wait in the same queue to prevent the disposal logic from removing
                    // the dictionary record before we can inspect it
                    waiter.Wait<object>(key, 99999, CancellationToken.None);

                    var ex = Record.Exception(() => result = task.Result);

                    var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                    waits.TryGetValue(key, out var queue);
                    queue.TryPeek(out var wait);

                    try
                    {
                        Assert.NotNull(ex);
                        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);

                        Assert.NotEmpty(waits);
                        Assert.Single(waits);

                        Assert.NotNull(queue);
                        Assert.Single(queue); // should contain only the dummy wait
                    }
                    finally
                    {
                        wait.Dispose();
                    }
                }
            }
        }

        [Trait("Category", "Wait Cleanup")]
        [Fact(DisplayName = "Wait dictionary and queue are collected after last wait is dequeued")]
        public void Wait_Dictionary_And_Queue_Are_Collected_After_Last_Wait_Is_Dequeued()
        {
            using (var waiter = new Waiter())
            {
                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                var locks = waiter.GetProperty<ConcurrentDictionary<WaitKey, ReaderWriterLockSlim>>("Locks");

                int CountWaits(WaitKey k)
                {
                    waits.TryGetValue(k, out var queue);
                    return queue?.Count ?? 0;
                }

                var key = new WaitKey("foo");

                waiter.Wait(key);
                waiter.Wait(key);

                Assert.Equal(2, CountWaits(key));
                Assert.True(waits.TryGetValue(key, out _));
                Assert.True(locks.TryGetValue(key, out _));

                waiter.Complete(key);

                Assert.Equal(1, CountWaits(key));
                Assert.True(waits.TryGetValue(key, out _));
                Assert.True(locks.TryGetValue(key, out _));

                waiter.Complete(key);

                Assert.Equal(0, CountWaits(key));
                Assert.False(waits.TryGetValue(key, out _));
                Assert.False(locks.TryGetValue(key, out _));
            }
        }

        [Trait("Category", "Wait Cancellation")]
        [Fact(DisplayName = "All waits are cancelled when CancelAll is invoked")]
        public void All_Waits_Are_Cancelled_When_CancelAll_Is_Invoked()
        {
            using (var waiter = new Waiter(9999))
            {
                var loginKey = new WaitKey(MessageCode.Server.Login, "1");
                var loginKey2 = new WaitKey(MessageCode.Server.Login, "2");
                var leaveKey = new WaitKey(MessageCode.Server.LeaveRoom);

                var loginTask = waiter.Wait<object>(loginKey);
                var loginTask2 = waiter.Wait<object>(loginKey2);
                var leaveTask = waiter.Wait<object>(leaveKey);

                waiter.CancelAll();

                Assert.True(loginTask.IsCanceled);
                Assert.True(loginTask2.IsCanceled);
                Assert.True(leaveTask.IsCanceled);
            }
        }

        [Trait("Category", "Wait Throw")]
        [Fact(DisplayName = "Wait throws and is dequeued when thrown")]
        public void Wait_Throws_And_Is_Dequeued_When_Thrown()
        {
            var key = new WaitKey(MessageCode.Server.Login);

            using (var waiter = new Waiter(0))
            {
                Task<object> task = waiter.Wait<object>(key, 999999);
                object result = null;

                waiter.Throw(key, new InvalidOperationException("error"));

                var ex = Record.Exception(() => result = task.Result);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex.InnerException);
                Assert.Equal("error", ex.InnerException.Message);

                Assert.False(waits.TryGetValue(key, out _));
            }
        }

        [Trait("Category", "Wait Creation")]
        [Fact(DisplayName = "WaitIndefinitely invocation creates Wait with max timeout")]
        public void WaitIndefinitely_Invocation_Creates_Wait_With_Max_Timeout()
        {
            var key = new WaitKey(MessageCode.Server.Login);

            using (var waiter = new Waiter())
            {
                Task<object> task = waiter.WaitIndefinitely<object>(key);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                try
                {
                    Assert.IsType<Task<object>>(task);
                    Assert.NotNull(task);
                    Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                    Assert.NotEmpty(waits);
                    Assert.Single(waits);

                    Assert.NotNull(queue);
                    Assert.Single(queue);
                    Assert.Equal(int.MaxValue, wait.Timeout);
                }
                finally
                {
                    wait.Dispose();
                }
            }
        }

        [Trait("Category", "Wait Creation")]
        [Fact(DisplayName = "Non generic WaitIndefinitely invocation creates Wait with max timeout")]
        public void Non_Generic_WaitIndefinitely_Invocation_Creates_Wait_With_Max_Timeout()
        {
            var key = new WaitKey(MessageCode.Server.Login);

            using (var waiter = new Waiter())
            {
                Task task = waiter.WaitIndefinitely(key);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);

                queue.TryPeek(out var wait);

                try
                {
                    Assert.IsType<Task<object>>(task);
                    Assert.NotNull(task);
                    Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                    Assert.NotEmpty(waits);
                    Assert.Single(waits);

                    Assert.NotNull(queue);
                    Assert.Single(queue);
                    Assert.Equal(int.MaxValue, wait.Timeout);
                }
                finally
                {
                    wait.Dispose();
                }
            }
        }

        [Trait("Category", "PendingWait")]
        [Fact(DisplayName = "PendingWait Dispose() does not throw if not Registered")]
        public void PendingWait_Dispose_Does_Not_Throw_If_Not_Registered()
        {
            var p = new PendingWait(new TaskCompletionSource(), 99999, cancelAction: () => { }, timeoutAction: () => { }, CancellationToken.None);

            var ex = Record.Exception(() => p.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "PendingWait")]
        [Fact(DisplayName = "PendingWait Dispose() does not throw if Registered")]
        public void PendingWait_Dispose_Does_Not_Throw_If_Registered()
        {
            var p = new PendingWait(new TaskCompletionSource(), 99999, cancelAction: () => { }, timeoutAction: () => { }, CancellationToken.None);
            p.Register();

            var ex = Record.Exception(() => p.Dispose());

            Assert.Null(ex);
        }

        [Trait("Category", "Exception")]
        [Fact(DisplayName = "Thorws SoulseekClientException given type mismatch")]
        public void Throws_SoulseekClientException_Given_Type_Mismatch()
        {
            using (var waiter = new Waiter())
            {
                var key = new WaitKey(MessageCode.Server.Login);

                // wait for a Guid
                _ = waiter.Wait<Guid>(key);

                // complete with an int
                var ex = Record.Exception(() => waiter.Complete<int>(key, 42));

                Assert.NotNull(ex);
                Assert.IsType<SoulseekClientException>(ex);
                Assert.Contains("mismatch", ex.Message);
            }
        }
    }
}