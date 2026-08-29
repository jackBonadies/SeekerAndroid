// <copyright file="SearchInternalTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Xunit;

    public class SearchInternalTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with expected data"), AutoData]
        public void Instantiates_With_Expected_Data(string searchText, int token, SearchOptions options)
        {
            var s = new SearchInternal(new SearchQuery(searchText), SearchScope.Network, token, options);

            Assert.Equal(searchText, s.Query.SearchText);
            Assert.Equal(SearchScope.Network.Type, s.Scope.Type);
            Assert.Equal(token, s.Token);
            Assert.Equal(options, s.Options);

            Assert.Equal(SearchStates.None, s.State);

            Assert.Equal(0, s.FileCount);
            Assert.Equal(0, s.LockedFileCount);
            Assert.Equal(0, s.ResponseCount);

            s.Dispose();
        }

        [Trait("Category", "Dispose")]
        [Fact(DisplayName = "Disposes without throwing")]
        public void Disposes_Without_Throwing()
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42);

            var ex = Record.Exception(() => s.Dispose());

            Assert.Null(ex);

            s.Dispose();
        }

        [Trait("Category", "Complete")]
        [Fact(DisplayName = "Complete sets state")]
        public void Complete_Sets_State()
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42);

            s.Complete(SearchStates.Cancelled);

            Assert.True(s.State.HasFlag(SearchStates.Completed));
            Assert.True(s.State.HasFlag(SearchStates.Cancelled));

            s.Dispose();
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Fact(DisplayName = "Response filter returns true when FilterResponses option is false")]
        public void Response_Filter_Returns_True_When_FilterResponses_Option_Is_False()
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: false));
            var response = new SearchResponse("u", 1, true, 1, 1, null);

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.True(filter);

            s.Dispose();
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Theory(DisplayName = "Response filter respects MinimumResponseFileCount option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void Response_Filter_Respects_MinimumResponseFileCount_Option(int actual, int option, bool expected)
        {
            var fixture = new Fixture();
            var file = fixture.Create<File>();

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: option));
            var response = new SearchResponse("u", 1, true, 1, 1, DuplicateFile(file, actual));

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);

            s.Dispose();
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Theory(DisplayName = "Response filter respects MinimumPeerUploadSpeed option")]
        [InlineData(0, 1, false)]
        [InlineData(1, 1, true)]
        [InlineData(1, 0, true)]
        public void Response_Filter_Respects_MinimumPeerUploadSpeed_Option(int actual, int option, bool expected)
        {
            var fixture = new Fixture();
            var file = fixture.Create<File>();

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: true, minimumPeerUploadSpeed: option));
            var response = new SearchResponse("u", 1, true, actual, 1, DuplicateFile(file, 1));

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);

            s.Dispose();
        }

        [Trait("Category", "ResponseMeetsOptionCriteria")]
        [Theory(DisplayName = "Response filter respects MaximumPeerQueueLength option")]
        [InlineData(0, 1, true)]
        [InlineData(1, 1, false)]
        [InlineData(1, 0, false)]
        public void Response_Filter_Respects_MaximumPeerQueueLength_Option(int actual, int option, bool expected)
        {
            var fixture = new Fixture();
            var file = fixture.Create<File>();

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: true, maximumPeerQueueLength: option));
            var response = new SearchResponse("u", 1, true, 1, actual, DuplicateFile(file, 1));

            var filter = s.InvokeMethod<bool>("ResponseMeetsOptionCriteria", response);

            Assert.Equal(expected, filter);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Fact(DisplayName = "TryAddResponse ignores response when search is not in progress")]
        public void TryAddResponse_Ignores_Response_When_Search_Is_Not_In_Progress()
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42);
            s.SetState(SearchStates.Completed);

            s.TryAddResponse(new SearchResponse("bar", 42, true, 1, 1, null));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Fact(DisplayName = "TryAddResponse ignores response when token does not match")]
        public void TryAddResponse_Ignores_Response_When_Token_Does_Not_Match()
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42);
            s.SetState(SearchStates.InProgress);

            s.TryAddResponse(new SearchResponse("bar", 24, true, 1, 1, null));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Fact(DisplayName = "TryAddResponse ignores response when response criteria not met")]
        public void TryAddResponse_Ignores_Response_When_Response_Criteria_Not_Met()
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
            s.SetState(SearchStates.InProgress);

            s.TryAddResponse(new SearchResponse("bar", 42, true, 1, 1, null));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Fact(DisplayName = "TryAddResponse ignores response when custom response filter returns false")]
        public void TryAddResponse_Ignores_Response_When_Custom_Response_Filter_Returns_False()
        {
            bool Filter(SearchResponse response) => false;

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: 0, responseFilter: Filter));
            s.SetState(SearchStates.InProgress);

            s.TryAddResponse(new SearchResponse("bar", 42, true, 1, 1, null));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Fact(DisplayName = "TryAddResponse ignores response when custom file filter removes all files")]
        public void TryAddResponse_Ignores_Response_When_Custom_File_Filter_Removes_All_Files()
        {
            bool Filter(File file) => false;

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1, fileFilter: Filter));
            s.SetState(SearchStates.InProgress);

            s.TryAddResponse(new SearchResponse("bar", 42, true, 1, 1, new List<File>() { new File(1, "a", 1, "b") }));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Fact(DisplayName = "TryAddResponse ignores response when custom file filter removes locked files")]
        public void TryAddResponse_Ignores_Response_When_Custom_File_Filter_Removes_Locked_Files()
        {
            bool Filter(File file) => false;

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 42, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1, fileFilter: Filter));
            s.SetState(SearchStates.InProgress);

            s.TryAddResponse(new SearchResponse("bar", 42, true, 1, 1, null, lockedFileList: new List<File>() { new File(1, "a", 1, "b") }));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Theory(DisplayName = "TryAddResponse adds response"), AutoData]
        public void TryAddResponse_Adds_Response(string username, int token, File file)
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
            s.SetState(SearchStates.InProgress);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteFile(file)
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            var responses = new List<SearchResponse>();
            s.ResponseReceived = (r) => responses.Add(r);

            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file }, new List<File>() { file }));

            Assert.Single(responses);

            var response = responses[0];
            var files = response.Files.ToList();

            Assert.Equal(1, response.FileCount);
            Assert.Equal(username, response.Username);
            Assert.Equal(file.Filename, files[0].Filename);
            Assert.Equal(file.Size, files[0].Size);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Theory(DisplayName = "TryAddResponse swallows exceptions"), AutoData]
        public void TryAddResponse_Swallows_Exceptions(string username, int token, File file)
        {
            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
            s.SetState(SearchStates.InProgress);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteFile(file)
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            var invoked = false;
            s.ResponseReceived += (r) => invoked = true;

            var ex = Record.Exception(() => s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file })));

            Assert.Null(ex);
            Assert.True(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Theory(DisplayName = "TryAddResponse ignores response when all files are filtered and response filtering is enabled"), AutoData]
        public void TryAddResponse_Ignores_Response_When_All_Files_Are_Filtered_And_Response_Filtering_Is_Enabled(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: true,
                    minimumResponseFileCount: 1,
                    fileFilter: (f) => false);

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
            s.SetState(SearchStates.InProgress);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, null));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Theory(DisplayName = "TryAddResponse ignores response ResponseFilter returns false"), AutoData]
        public void TryAddResponse_Ignores_Response_When_ResponseFilter_Returns_False(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: true,
                    minimumResponseFileCount: 1,
                    responseFilter: (r) => false);

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
            s.SetState(SearchStates.InProgress);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, null));

            var invoked = false;
            s.ResponseReceived = (r) => invoked = true;

            Assert.False(invoked);

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Theory(DisplayName = "TryAddResponse completes search and invokes completed event when file limit reached"), AutoData]
        public async Task TryAddResponse_Completes_Search_And_Invokes_Completed_Event_When_File_Limit_Reached(string username, int token, File file)
        {
            var options = new SearchOptions(
                    filterResponses: false,
                    minimumResponseFileCount: 1,
                    fileLimit: 1);

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
            s.SetState(SearchStates.InProgress);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteFile(file)
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file lists

            var task = s.WaitForCompletion(CancellationToken.None);

            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file }));

            await task;

            Assert.True(s.State.HasFlag(SearchStates.Completed));
            Assert.True(s.State.HasFlag(SearchStates.FileLimitReached));

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Theory(DisplayName = "TryAddResponse completes search and invokes completed event when response limit reached"), AutoData]
        public async Task TryAddResponse_Completes_Search_And_Invokes_Completed_Event_When_Response_Limit_Reached(string username, int token, byte code, string filename, int size, string extension)
        {
            var options = new SearchOptions(
                    filterResponses: false,
                    minimumResponseFileCount: 1,
                    responseLimit: 1,
                    fileLimit: 10000000);

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, options);
            s.SetState(SearchStates.InProgress);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteByte(code) // code
                .WriteString(filename) // filename
                .WriteLong(size) // size
                .WriteString(extension) // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file lists

            var task = s.WaitForCompletion(CancellationToken.None);

            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, null));

            await task;

            Assert.True(s.State.HasFlag(SearchStates.Completed));
            Assert.True(s.State.HasFlag(SearchStates.ResponseLimitReached));

            s.Dispose();
        }

        [Trait("Category", "TryAddResponse")]
        [Theory(DisplayName = "TryAddResponse invokes response received event"), AutoData]
        public void TryAddResponse_Invokes_Response_Received_Event_Handler(string username, int token, File file)
        {
            SearchResponse addResponse = null;

            var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token, new SearchOptions(filterResponses: true, minimumResponseFileCount: 1));
            s.SetState(SearchStates.InProgress);

            s.ResponseReceived += (response) => addResponse = response;

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token) // token
                .WriteInteger(1) // file count
                .WriteFile(file)
                .WriteByte(1) // free upload slots
                .WriteInteger(1) // upload speed
                .WriteLong(0) // queue length
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Build();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            reader.Seek(username.Length + 12); // seek to the start of the file list

            s.TryAddResponse(new SearchResponse(username, token, true, 1, 1, new List<File>() { file }));

            Assert.NotNull(addResponse);
            Assert.Equal(file.Filename, addResponse.Files.ToList()[0].Filename);

            s.Dispose();
        }

        [Trait("Category", "Cancel")]
        [Fact(DisplayName = "Cancel cancels")]
        public async Task Cancel_Cancels()
        {
            using (var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 1))
            {
                s.Cancel();

                var ex = await Record.ExceptionAsync(() => s.WaitForCompletion(CancellationToken.None));

                Assert.NotNull(ex);
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Trait("Category", "Timer")]
        [Fact(DisplayName = "Timer is disabled initially")]
        public void Timer_Disabled_Initially()
        {
            using (var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 1))
            {
                var timer = s.GetProperty<System.Timers.Timer>("SearchTimeoutTimer");

                Assert.False(timer.Enabled);
            }
        }

        [Trait("Category", "Timer")]
        [Fact(DisplayName = "Timer starts on transition to InProgress")]
        public void Timer_Starts_On_InProgress_Transition()
        {
            using (var s = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 1))
            {
                var timer = s.GetProperty<System.Timers.Timer>("SearchTimeoutTimer");

                Assert.False(timer.Enabled);

                s.SetState(SearchStates.Requested);

                Assert.False(timer.Enabled);

                s.SetState(SearchStates.Queued);

                Assert.False(timer.Enabled);

                s.SetState(SearchStates.InProgress);

                Assert.True(timer.Enabled);
            }
        }

        private static List<File> DuplicateFile(File file, int count)
        {
            var list = new List<File>();

            for (int i = 0; i < count; i++)
            {
                list.Add(file);
            }

            return list;
        }
    }
}