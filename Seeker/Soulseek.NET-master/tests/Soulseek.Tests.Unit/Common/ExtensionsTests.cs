// <copyright file="ExtensionsTests.cs" company="JP Dillingham">
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
    using System.Timers;
    using Moq;
    using Xunit;

    public class ExtensionsTests
    {
        [Trait("Category", "Extension")]
        [Fact(DisplayName = "DequeueAndDisposeAll dequeues and disposes all")]
        public void DequeueAndDisposeAll_Dequeues_And_Disposes_All()
        {
            using (var t1 = new Timer())
            using (var t2 = new Timer())
            {
                var queue = new ConcurrentQueue<Timer>();
                queue.Enqueue(t1);
                queue.Enqueue(t2);

                queue.DequeueAndDisposeAll();

                var ex1 = Record.Exception(() => t1.Start());
                var ex2 = Record.Exception(() => t2.Start());

                Assert.Empty(queue);

                Assert.NotNull(ex1);
                Assert.IsType<ObjectDisposedException>(ex1);

                Assert.NotNull(ex2);
                Assert.IsType<ObjectDisposedException>(ex2);
            }
        }

        [Trait("Category", "Extension")]
        [Fact(DisplayName = "Timer reset does not throw given a disposed timer")]
        public void Timer_Reset_Does_Not_Throw_On_Disposed_Timer()
        {
            var timer = new Timer();
            timer.Dispose();

            var ex = Record.Exception(() => timer.Reset());

            Assert.Null(ex);
        }

        [Trait("Category", "Extension")]
        [Fact(DisplayName = "TryDispose does not throw if Dispose throws")]
        public void TryDispose_Does_Not_Throw_If_Dispose_Throws()
        {
            var obj = new Mock<IDisposable>();
            obj.Setup(m => m.Dispose()).Throws(new Exception());

            var ex1 = Record.Exception(() => obj.Object.Dispose());

            Assert.NotNull(ex1);

            var ex2 = Record.Exception(() => obj.Object.TryDispose());

            Assert.Null(ex2);
        }

        [Trait("Category", "Extension")]
        [Fact(DisplayName = "TryDispose returns false if Dispose throws")]
        public void TryDispose_Returns_False_If_Dispose_Throws()
        {
            var obj = new Mock<IDisposable>();
            obj.Setup(m => m.Dispose()).Throws(new Exception());

            Assert.False(obj.Object.TryDispose());
        }

        [Trait("Category", "Extension")]
        [Fact(DisplayName = "TryDispose returns true if Dispose does not throw")]
        public void TryDispose_Returns_True_If_Dispose_Does_Not_Throw()
        {
            var obj = new Mock<IDisposable>();

            Assert.True(obj.Object.TryDispose());
        }
    }
}
