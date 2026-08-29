// <copyright file="CleanupSemaphoresAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Client
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Xunit;

    public class CleanupSemaphoresAsyncTests
    {
        [Trait("Category", "CleanupSemaphores")]
        [Fact(DisplayName = "Upload exits if semaphore is held")]
        public async Task Upload_Exits_If_Semaphore_Is_Held()
        {
            using (var s = new SoulseekClient())
            {
                var sem = s.GetProperty<SemaphoreSlim>("UploadSemaphoreSyncRoot");
                await sem.WaitAsync();

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("CleanupUploadSemaphoresAsync"));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "CleanupSemaphores")]
        [Theory(DisplayName = "Upload removes record if semaphore is not held"), AutoData]
        public async Task Upload_Removes_Record_If_Semaphore_Is_Not_Held(string username)
        {
            using (var s = new SoulseekClient())
            {
                var dict = s.GetProperty<ConcurrentDictionary<string, SemaphoreSlim>>("UploadSemaphores");

                var sem = new SemaphoreSlim(1, 1);
                dict.TryAdd(username, sem);

                await s.InvokeMethod<Task>("CleanupUploadSemaphoresAsync");

                Assert.Empty(dict);
            }
        }

        [Trait("Category", "CleanupSemaphores")]
        [Theory(DisplayName = "Upload produces diagnostic when removing record"), AutoData]
        public async Task Upload_Produces_Diagnostic_When_Removing_Record(string username)
        {
            var diag = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(diagnosticFactory: diag.Object))
            {
                var dict = s.GetProperty<ConcurrentDictionary<string, SemaphoreSlim>>("UploadSemaphores");

                var sem = new SemaphoreSlim(1, 1);
                dict.TryAdd(username, sem);

                await s.InvokeMethod<Task>("CleanupUploadSemaphoresAsync");
            }

            diag.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"cleaned up upload semaphore for {username}"))), Times.Once);
        }

        [Trait("Category", "CleanupSemaphores")]
        [Theory(DisplayName = "Upload does not remove record if semaphore is held"), AutoData]
        public async Task Upload_Does_Not_Remove_Record_If_Semaphore_Is_Held(string username)
        {
            using (var s = new SoulseekClient())
            {
                var dict = s.GetProperty<ConcurrentDictionary<string, SemaphoreSlim>>("UploadSemaphores");

                var sem = new SemaphoreSlim(1, 1);
                await sem.WaitAsync();
                dict.TryAdd(username, sem);

                await s.InvokeMethod<Task>("CleanupUploadSemaphoresAsync");

                Assert.NotEmpty(dict);
                Assert.Equal(username, dict.Keys.First());
                Assert.Equal(sem, dict.Values.First());
            }
        }

        [Trait("Category", "CleanupSemaphores")]
        [Fact(DisplayName = "UserEndPoint exits if semaphore is held")]
        public async Task UserEndPoint_Exits_If_Semaphore_Is_Held()
        {
            using (var s = new SoulseekClient())
            {
                var sem = s.GetProperty<SemaphoreSlim>("UserEndPointSemaphoreSyncRoot");
                await sem.WaitAsync();

                var ex = await Record.ExceptionAsync(() => s.InvokeMethod<Task>("CleanupUserEndPointSemaphoresAsync"));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "CleanupSemaphores")]
        [Theory(DisplayName = "UserEndPoint removes record if semaphore is not held"), AutoData]
        public async Task UserEndPoint_Removes_Record_If_Semaphore_Is_Not_Held(string username)
        {
            using (var s = new SoulseekClient())
            {
                var dict = s.GetProperty<ConcurrentDictionary<string, SemaphoreSlim>>("UserEndPointSemaphores");

                var sem = new SemaphoreSlim(1, 1);
                dict.TryAdd(username, sem);

                await s.InvokeMethod<Task>("CleanupUserEndPointSemaphoresAsync");

                Assert.Empty(dict);
            }
        }

        [Trait("Category", "CleanupSemaphores")]
        [Theory(DisplayName = "UserEndPoint produces diagnostic when removing record"), AutoData]
        public async Task UserEndPoint_Produces_Diagnostic_When_Removing_Record(string username)
        {
            var diag = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(diagnosticFactory: diag.Object))
            {
                var dict = s.GetProperty<ConcurrentDictionary<string, SemaphoreSlim>>("UserEndPointSemaphores");

                var sem = new SemaphoreSlim(1, 1);
                dict.TryAdd(username, sem);

                await s.InvokeMethod<Task>("CleanupUserEndPointSemaphoresAsync");
            }

            diag.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"cleaned up user endpoint semaphore for {username}"))), Times.Once);
        }

        [Trait("Category", "CleanupSemaphores")]
        [Theory(DisplayName = "UserEndPoint does not remove record if semaphore is held"), AutoData]
        public async Task UserEndPoint_Does_Not_Remove_Record_If_Semaphore_Is_Held(string username)
        {
            using (var s = new SoulseekClient())
            {
                var dict = s.GetProperty<ConcurrentDictionary<string, SemaphoreSlim>>("UserEndPointSemaphores");

                var sem = new SemaphoreSlim(1, 1);
                await sem.WaitAsync();
                dict.TryAdd(username, sem);

                await s.InvokeMethod<Task>("CleanupUserEndPointSemaphoresAsync");

                Assert.NotEmpty(dict);
                Assert.Equal(username, dict.Keys.First());
                Assert.Equal(sem, dict.Values.First());
            }
        }
    }
}
