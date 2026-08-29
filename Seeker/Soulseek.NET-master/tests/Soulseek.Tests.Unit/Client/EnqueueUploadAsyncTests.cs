// <copyright file="EnqueueUploadAsyncTests.cs" company="JP Dillingham">
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
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class EnqueueUploadAsyncTests
    {
        [Trait("Category", "EnqueueUploadAsync")]
        [Theory(DisplayName = "EnqueueUploadAsync file throws immediately given bad input")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task EnqueueUploadAsync_File_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, "filename", Guid.NewGuid().ToString()));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "EnqueueUploadAsync")]
        [Theory(DisplayName = "EnqueueUploadAsync stream throws immediately given bad input")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task EnqueueUploadAsync_Stream_Throws_ArgumentException_Given_Bad_Username(string username)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, "filename", 1, (_) => Task.FromResult((Stream)stream)));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Trait("Category", "EnqueueUploadAsync")]
        [Theory(DisplayName = "EnqueueUploadAsync file returns after upload enters Queued state"), AutoData]
        public async Task EnqueueUploadAsync_File_Returns_After_Upload_Enters_Queued_State(string username, string filename, int token)
        {
            using (var testFile = new TestFile())
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, filename, testFile.Path, token, new TransferOptions(), null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "EnqueueUploadAsync")]
        [Theory(DisplayName = "EnqueueUploadAsync stream returns after upload enters Queued state"), AutoData]
        public async Task EnqueueUploadAsync_Stream_Returns_After_Upload_Enters_Queued_State(string username, string filename, int token)
        {
            using (var stream = new MemoryStream())
            using (var s = new SoulseekClient(minorVersion: 9999))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = await Record.ExceptionAsync(() => s.EnqueueUploadAsync(username, filename, 1, (_) => Task.FromResult((Stream)stream), token, new TransferOptions(), null));

                Assert.Null(ex);
            }
        }
    }
}
