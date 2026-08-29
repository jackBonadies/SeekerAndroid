// <copyright file="PeerMessageHandlerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Handlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class PeerMessageHandlerTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws given null SoulseekClient")]
        public void Instantiation_Throws_Given_Null_SoulseekClient()
        {
            var ex = Record.Exception(() => new PeerMessageHandler(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("soulseekClient", ((ArgumentNullException)ex).ParamName);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on message"), AutoData]
        public void Creates_Diagnostic_On_Message(string username, IPEndPoint endpoint)
        {
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(msg));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentMinSpeed)
                .WriteInteger(1)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, new MessageEventArgs(message));

            Assert.Contains(messages, m => m.IndexOf("peer message received", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on diagnostic"), AutoData]
        public void Raises_DiagnosticGenerated_On_Diagnostic(string message)
        {
            using (var client = new SoulseekClient(options: null))
            {
                DiagnosticEventArgs args = default;

                PeerMessageHandler l = new PeerMessageHandler(client);
                l.DiagnosticGenerated += (sender, e) => args = e;

                var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");
                diagnostic.Info(message);

                Assert.Equal(message, args.Message);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Theory(DisplayName = "Raises DownloadDenied on UploadDenied"), AutoData]
        public void Raises_DownloadDenied_On_UploadDenied(string username, string filename, string message)
        {
            var (_, mocks) = GetFixture(username);

            using (var client = new SoulseekClient(options: null))
            {
                DownloadDeniedEventArgs args = default;

                PeerMessageHandler l = new PeerMessageHandler(client);
                l.DownloadDenied += (sender, e) => args = e;

                l.HandleMessageRead(mocks.PeerConnection.Object, new UploadDenied(filename, message).ToByteArray());

                Assert.NotNull(args);
                Assert.Equal(username, args.Username);
                Assert.Equal(filename, args.Filename);
                Assert.Equal(message, args.Message);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Theory(DisplayName = "Raises DownloadFailed on UploadFailed"), AutoData]
        public void Raises_DownloadFailed_On_UploadFailed(string username, string filename)
        {
            var (_, mocks) = GetFixture(username);

            using (var client = new SoulseekClient(options: null))
            {
                DownloadFailedEventArgs args = default;

                PeerMessageHandler l = new PeerMessageHandler(client);
                l.DownloadFailed += (sender, e) => args = e;

                l.HandleMessageRead(mocks.PeerConnection.Object, new UploadFailed(filename).ToByteArray());

                Assert.NotNull(args);
                Assert.Equal(username, args.Username);
                Assert.Equal(filename, args.Filename);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Does not throw raising DiagnosticGenerated if no handlers bound"), AutoData]
        public void Does_Not_Throw_Raising_DiagnosticGenerated_If_No_Handlers_Bound(string message)
        {
            using (var client = new SoulseekClient(options: null))
            {
                PeerMessageHandler l = new PeerMessageHandler(client);

                var diagnostic = l.GetProperty<IDiagnosticFactory>("Diagnostic");

                var ex = Record.Exception(() => diagnostic.Info(message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates diagnostic on PeerUploadFailed message"), AutoData]
        public void Creates_Diagnostic_On_PeerUploadFailed_Message(string username, IPEndPoint endpoint)
        {
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(msg));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString("foo")
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("peer message received", StringComparison.InvariantCultureIgnoreCase) > -1);
            Assert.Contains(messages, m => m.IndexOf("upload", StringComparison.InvariantCultureIgnoreCase) > -1 && m.IndexOf("failed", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws TransferRequest wait on PeerUploadFailed message"), AutoData]
        public void Throws_TransferRequest_Wait_On_PeerUploadFailed_Message(string username, IPEndPoint endpoint, string filename)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var dict = new ConcurrentDictionary<int, TransferInternal>();
            dict.TryAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

            mocks.Client.Setup(m => m.DownloadDictionary)
                .Returns(dict);

            mocks.PeerConnection.Setup(m => m.Username)
                .Returns(username);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString(filename)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.IsAny<TransferException>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates diagnostic on Exception"), AutoData]
        public void Creates_Diagnostic_On_Exception(string username, IPEndPoint endpoint)
        {
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .Build(); // malformed message

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("error handling peer message", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for TransferResponse"), AutoData]
        public void Completes_Wait_For_TransferResponse(string username, IPEndPoint endpoint, int token, int fileSize)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new TransferResponse(token, fileSize).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferResponse, username, token), It.Is<TransferResponse>(r => r.Token == token)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerInfoResponse"), AutoData]
        public void Completes_Wait_For_PeerInfoResponse(string username, IPEndPoint endpoint, string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(description)
                .WriteByte(1)
                .WriteInteger(picture.Length)
                .WriteBytes(picture)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte((byte)(hasFreeSlot ? 1 : 0))
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.InfoResponse, username), It.IsAny<UserInfo>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for folderContentsResponse"), AutoData]
        public void Completes_Wait_For_FolderContentsResponse(string username, IPEndPoint endpoint, int token, string dirname)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new FolderContentsResponse(token, dirname, new List<Directory>() { new Directory(dirname) }).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.FolderContentsResponse, username, token), It.IsAny<IEnumerable<Directory>>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerPlaceInQueueResponse"), AutoData]
        public void Completes_Wait_For_PeerPlaceInQueueResponse(string username, IPEndPoint endpoint, string filename, int placeInQueue)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString(filename)
                .WriteInteger(placeInQueue)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(
                m => m.Complete(
                    new WaitKey(MessageCode.Peer.PlaceInQueueResponse, username, filename),
                    It.Is<PlaceInQueueResponse>(r => r.Filename == filename && r.PlaceInQueue == placeInQueue)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerBrowseResponse"), AutoData]
        public void Completes_Wait_For_PeerBrowseResponse(string username, IPEndPoint endpoint, string directoryName)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(directoryName) // first directory name
                .WriteInteger(0) // first directory file count
                .Compress()
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.BrowseResponse, username), It.Is<BrowseResponse>(r => r.Directories.First().Name == directoryName)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws wait for PeerBrowseResponse given bad message"), AutoData]
        public void Throws_Wait_For_PeerBrowseResponse_Given_Bad_Message(string username, IPEndPoint endpoint)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.BrowseResponse, username), It.IsAny<MessageReadException>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores inactive search response"), AutoData]
        public void Ignores_Inactive_Search_Response(string username, IPEndPoint endpoint, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(mocks.PeerConnection.Object, msg));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws TransferRequest wait on PeerUploadDenied"), AutoData]
        public void Throws_TransferRequest_Wait_On_PeerUploadDenied(string username, IPEndPoint endpoint, string filename, string message)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadDenied)
                .WriteString(filename)
                .WriteString(message)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.IsAny<Exception>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Appends active search response"), AutoData]
        public void Appends_Active_Search_Response(string username, IPEndPoint endpoint, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var responses = new List<SearchResponse>();

            using (var search = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, token)
            {
                ResponseReceived = (r) => responses.Add(r),
            })
            {
                search.SetState(SearchStates.InProgress);
                mocks.Searches.TryAdd(token, search);

                handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

                Assert.Single(responses);
                Assert.Contains(responses, r => r.Username == username && r.Token == token);
            }
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Sends default UserInfoResponse if resolver throws")]
        public async Task Sends_Default_UserInfoResponse_If_Resolver_Throws()
        {
            var options = new SoulseekClientOptions(userInfoResolver: (u, i) => { throw new Exception(); });

            var defaultResponse = await new SoulseekClientOptions()
                .UserInfoResolver(null, null);

            var (handler, mocks) = GetFixture(options: options);

            var msg = new UserInfoRequest().ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(defaultResponse.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sends resolved UserInfoResponse"), AutoData]
        public void Sends_Resolved_UserInfoResponse(string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeUploadSlot)
        {
            var response = new UserInfo(description, uploadSlots, queueLength, hasFreeUploadSlot, picture);
            var options = new SoulseekClientOptions(userInfoResolver: (u, i) => Task.FromResult(response));

            var (handler, mocks) = GetFixture(options: options);

            var msg = new UserInfoRequest().ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed UserInfoResponse resolution"), AutoData]
        public void Creates_Diagnostic_On_Failed_UserInfoResponse_Resolution(string username, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(userInfoResolver: (u, i) => { throw new Exception(); });
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new UserInfoRequest().ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to resolve user info response", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sends resolved SearchResponse"), AutoData]
        public void Sends_Resolved_SearchResponse(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
        {
            var files = new List<File>()
            {
                new File(1, "1", 1, "1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, "2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
            var options = new SoulseekClientOptions(searchResponseResolver: (u, i, q) => Task.FromResult(response));

            var (handler, mocks) = GetFixture(options: options);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores PeerSearchRequest if search response resolver is null"), AutoData]
        public void Ignores_PeerSearchRequest_If_Search_Response_Resolver_Is_Null(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
        {
            var files = new List<File>()
            {
                new File(1, "1", 1, "1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, "2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
            var options = new SoulseekClientOptions(searchResponseResolver: null);

            var (handler, mocks) = GetFixture(options: options);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(mocks.PeerConnection.Object, msg));

            Assert.Null(ex);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(response.ToByteArray())), null), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores PeerSearchRequest if search response is empty"), AutoData]
        public void Ignores_PeerSearchRequest_If_Search_Response_Is_Empty(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
        {
            var files = new List<File>();

            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
            var options = new SoulseekClientOptions(searchResponseResolver: null);

            var (handler, mocks) = GetFixture(options: options);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(mocks.PeerConnection.Object, msg));

            Assert.Null(ex);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(response.ToByteArray())), null), Times.Never);
        }

        [Trait("Category", "SearchRequest")]
        [Theory(DisplayName = "Writes RawSearchResponse with expected length"), AutoData]
        public void Writes_RawSearchResponse_With_Expected_Length(string username, IPEndPoint endpoint, int token, string query)
        {
            var length = 1234L;
            var stream = new System.IO.MemoryStream(new byte[length]);
            var rawResponse = new RawSearchResponse(length, stream);

            var options = new SoulseekClientOptions(
                searchResponseResolver: (user, tok, searchQuery) => Task.FromResult<SearchResponse>(rawResponse));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(
                    It.Is<long>(l => l == length),
                    It.IsAny<System.IO.Stream>(),
                    It.IsAny<Func<int, CancellationToken, Task<int>>>(),
                    It.IsAny<Action<int, int, int>>(),
                    It.IsAny<CancellationToken?>()),
                Times.Once);
        }

        [Trait("Category", "SearchRequest")]
        [Theory(DisplayName = "Does not throw when disposing RawSearchResponse stream fails"), AutoData]
        public void Does_Not_Throw_When_Disposing_RawSearchResponse_Stream_Fails(string username, IPEndPoint endpoint, int token, string query)
        {
            var length = 1234L;
            var faultyStream = new FaultyDisposeStream();

            var rawResponse = new RawSearchResponse(length, faultyStream);

            var options = new SoulseekClientOptions(
                searchResponseResolver: (user, tok, searchQuery) => Task.FromResult<SearchResponse>(rawResponse));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(mocks.PeerConnection.Object, message));

            Assert.Null(ex);
        }

        private class FaultyDisposeStream : System.IO.MemoryStream
        {
            protected override void Dispose(bool disposing)
            {
                throw new Exception("Dispose failed");
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates diagnostic on failed search response resolution"), AutoData]
        public void Creates_Diagnostic_On_Failed_Search_Response_Resolution(string query, string username, int token, bool hasFreeUploadSlot, int uploadSpeed, int queueLength)
        {
            var files = new List<File>();

            var response = new SearchResponse(username, token, hasFreeUploadSlot, uploadSpeed, queueLength, files);
            var expectedEx = new Exception("error");
            var options = new SoulseekClientOptions(searchResponseResolver: (u, i, q) => Task.FromException<SearchResponse>(expectedEx));

            var (handler, mocks) = GetFixture(options: options);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(mocks.PeerConnection.Object, msg));

            Assert.Null(ex);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(response.ToByteArray())), null), Times.Never);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("error resolving search response")), expectedEx), Times.Once);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Sends resolved BrowseResponse")]
        public void Sends_Resolved_BrowseResponse()
        {
            var files = new List<File>()
            {
                new File(1, "1", 1, "1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, "2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            IEnumerable<Directory> dirs = new List<Directory>()
            {
                new Directory("1", files),
                new Directory("2", files),
            };

            var response = new BrowseResponse(dirs);
            var options = new SoulseekClientOptions(browseResponseResolver: (u, i) => Task.FromResult(response));

            var (handler, mocks) = GetFixture(options: options);

            var msg = new BrowseRequest().ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(o => o.Matches(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "BrowseRequest")]
        [Theory(DisplayName = "Writes RawBrowseResponse with expected length"), AutoData]
        public void Writes_RawBrowseResponse_With_Expected_Length(string username, IPEndPoint endpoint, int token, string query)
        {
            var length = 1234L;
            var stream = new System.IO.MemoryStream(new byte[length]);
            var rawResponse = new RawBrowseResponse(length, stream);

            var options = new SoulseekClientOptions(
                browseResponseResolver: (user, tok) => Task.FromResult<BrowseResponse>(rawResponse));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(
                    It.Is<long>(l => l == length),
                    It.IsAny<System.IO.Stream>(),
                    It.IsAny<Func<int, CancellationToken, Task<int>>>(),
                    It.IsAny<Action<int, int, int>>(),
                    It.IsAny<CancellationToken?>()),
                Times.Once);
        }

        [Trait("Category", "BrowseRequest")]
        [Theory(DisplayName = "Does not throw when disposing RawBrowseResponse stream fails"), AutoData]
        public void Does_Not_Throw_When_Disposing_RawBrowseResponse_Stream_Fails(string username, IPEndPoint endpoint, int token, string query)
        {
            var length = 1234L;
            var faultyStream = new FaultyDisposeStream();

            var rawResponse = new RawBrowseResponse(length, faultyStream);

            var options = new SoulseekClientOptions(
                browseResponseResolver: (user, tok) => Task.FromResult<BrowseResponse>(rawResponse));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseRequest)
                .WriteInteger(token)
                .WriteString(query)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessageRead(mocks.PeerConnection.Object, message));

            Assert.Null(ex);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed BrowseResponse resolution"), AutoData]
        public void Creates_Diagnostic_On_Failed_BrowseResponse_Resolution(string username, IPEndPoint endpoint)
        {
            var options = new SoulseekClientOptions(browseResponseResolver: (u, i) => { throw new Exception(); });
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new BrowseRequest().ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to resolve browse response", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sends resolved FolderContentsResponse"), AutoData]
        public void Sends_Resolved_FolderContentsResponse(int token, string dirname)
        {
            var files = new List<File>()
            {
                new File(1, "1", 1, "1", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, "2", new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            var dir = new Directory(dirname, files);

            var response = new FolderContentsResponse(token, dirname, new List<Directory>() { dir });
            var options = new SoulseekClientOptions(directoryContentsResolver: (u, i, t, d) => Task.FromResult(new List<Directory>() { dir }.AsEnumerable()));

            var (handler, mocks) = GetFixture(options: options);

            var msg = new FolderContentsRequest(token, dirname).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<IOutgoingMessage>(o => Encoding.UTF8.GetString(o.ToByteArray()) == Encoding.UTF8.GetString(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed FolderContentsResponse resolution"), AutoData]
        public void Creates_Diagnostic_On_Failed_FolderContentsResponse_Resolution(string username, IPEndPoint endpoint, int token, string dirname)
        {
            var options = new SoulseekClientOptions(directoryContentsResolver: (u, i, t, d) => { throw new Exception(); });
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new FolderContentsRequest(token, dirname).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to resolve directory contents response", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed QueueDownload invocation via QueueDownload"), AutoData]
        public void Creates_Diagnostic_On_Failed_QueueDownload_Invocation_Via_QueueDownload(string username, IPEndPoint endpoint, string filename)
        {
            var options = new SoulseekClientOptions(enqueueDownload: (u, f, i) => { throw new Exception(); });
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new QueueDownloadRequest(filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to invoke QueueDownload action", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Writes PlaceInQueueResponse on successful enqueue via QueueDownload"), AutoData]
        public void Writes_PlaceInQueueResponse_On_Successful_Enqueue_Via_QueueDownload(string username, IPEndPoint endpoint, string filename, int placeInQueue)
        {
            var options = new SoulseekClientOptions(
                enqueueDownload: (u, f, i) => Task.CompletedTask,
                placeInQueueResolver: (u, f, i) => Task.FromResult<int?>(placeInQueue));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new QueueDownloadRequest(filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection
                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not write PlaceInQueueResponse on successful enqueue via QueueDownload if placeInQueueResponse is null"), AutoData]
        public void Does_Not_Write_PlaceInQueueResponse_On_Successful_Enqueue_Via_QueueDownload_If_PlaceInQueueResponse_Is_Null(string username, IPEndPoint endpoint, string filename)
        {
            var options = new SoulseekClientOptions(
                enqueueDownload: (u, f, i) => Task.CompletedTask,
                placeInQueueResolver: (u, f, i) => Task.FromResult<int?>(null));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new QueueDownloadRequest(filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection
                .Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed QueueDownload invocation via TransferRequest"), AutoData]
        public void Creates_Diagnostic_On_Failed_QueueDownload_Invocation_Via_TransferRequest(string username, IPEndPoint endpoint, int token, string filename)
        {
            var options = new SoulseekClientOptions(enqueueDownload: (u, f, i) => { throw new Exception(); });
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, endpoint, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to invoke QueueDownload action", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Writes TransferResponse on successful QueueDownload invocation"), AutoData]
        public void Writes_TransferResponse_On_Successful_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename)
        {
            var options = new SoulseekClientOptions(enqueueDownload: (u, f, i) => Task.CompletedTask);
            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
            var expected = new TransferResponse(token, "Queued").ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(o => Encoding.UTF8.GetString(o.ToByteArray()) == Encoding.UTF8.GetString(expected)), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Writes PlaceInQueueResponse on successful QueueDownload invocation"), AutoData]
        public void Writes_PlaceInQueueResponse_On_Successful_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename, int placeInQueue)
        {
            var options = new SoulseekClientOptions(
                enqueueDownload: (u, f, i) => Task.CompletedTask,
                placeInQueueResolver: (u, f, i) => Task.FromResult<int?>(placeInQueue));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection
                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Writes PlaceInQueueResponse on PlaceInQueueRequest"), AutoData]
        public void Writes_PlaceInQueueResponse_On_PlaceInQueueRequest(string username, IPEndPoint endpoint, string filename, int placeInQueue)
        {
            var options = new SoulseekClientOptions(
                enqueueDownload: (u, f, i) => Task.CompletedTask,
                placeInQueueResolver: (u, f, i) => Task.FromResult<int?>(placeInQueue));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new PlaceInQueueRequest(filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection
                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not write PlaceInQueueResponse on PlaceInQueueRequest if response is null"), AutoData]
        public void Does_Not_Write_PlaceInQueueResponse_On_PlaceInQueueRequest_If_Response_Is_Null(string username, IPEndPoint endpoint, string filename)
        {
            var options = new SoulseekClientOptions(
                enqueueDownload: (u, f, i) => Task.CompletedTask,
                placeInQueueResolver: (u, f, i) => Task.FromResult<int?>(null));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new PlaceInQueueRequest(filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection
                .Verify(m => m.WriteAsync(It.IsAny<IOutgoingMessage>(), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not Write PlaceInQueueResponse on successful QueueDownload invocation if PlaceInQueueResponse is null"), AutoData]
        public void Does_Not_Write_PlaceInQueueResponse_On_Successful_QueueDownload_Invocation_If_PlaceInQueueResponse_Is_Null(string username, IPEndPoint endpoint, int token, string filename, int placeInQueue)
        {
            var options = new SoulseekClientOptions(
                enqueueDownload: (u, f, i) => Task.CompletedTask,
                placeInQueueResolver: (u, f, i) => Task.FromResult<int?>(null));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection
                .Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(new PlaceInQueueResponse(filename, placeInQueue).ToByteArray())), It.IsAny<CancellationToken?>()), Times.Never);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic when PlaceInQueueResponseResolver throws"), AutoData]
        public void Creates_Diagnostic_When_PlaceInQueueResponseResolver_Throws(string username, IPEndPoint endpoint, int token, string filename)
        {
            var ex = new NullReferenceException();

            var options = new SoulseekClientOptions(
                enqueueDownload: (u, f, i) => Task.CompletedTask,
                placeInQueueResolver: (u, f, i) => Task.FromException<int?>(ex));

            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("Failed to resolve place in Queue")), ex), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Writes TransferResponse and QueueFailedResponse on failed QueueDownload invocation"), AutoData]
        public void Writes_TransferResponse_And_QueueFailedResponse_On_Failed_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename)
        {
            var options = new SoulseekClientOptions(enqueueDownload: (u, f, i) => { throw new Exception(); });
            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
            var expectedTransferResponse = new TransferResponse(token, "Enqueue failed due to internal error").ToByteArray();
            var expectedQueueFailedResponse = new UploadDenied(filename, "Enqueue failed due to internal error").ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(o => Encoding.UTF8.GetString(o.ToByteArray()) == Encoding.UTF8.GetString(expectedTransferResponse)), null), Times.Once);
            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(o => Encoding.UTF8.GetString(o.ToByteArray()) == Encoding.UTF8.GetString(expectedQueueFailedResponse)), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Writes TransferResponse and QueueFailedResponse on rejected QueueDownload invocation"), AutoData]
        public void Writes_TransferResponse_And_QueueFailedResponse_On_Rejected_QueueDownload_Invocation(string username, IPEndPoint endpoint, int token, string filename, string rejectMessage)
        {
            var options = new SoulseekClientOptions(enqueueDownload: (u, f, i) => { throw new DownloadEnqueueException(rejectMessage); });
            var (handler, mocks) = GetFixture(username, endpoint, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
            var expectedTransferResponse = new TransferResponse(token, rejectMessage).ToByteArray();
            var expectedQueueFailedResponse = new UploadDenied(filename, rejectMessage).ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(o => Encoding.UTF8.GetString(o.ToByteArray()) == Encoding.UTF8.GetString(expectedTransferResponse)), null), Times.Once);
            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(o => Encoding.UTF8.GetString(o.ToByteArray()) == Encoding.UTF8.GetString(expectedQueueFailedResponse)), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes TransferRequest wait on upload request if transfer is tracked"), AutoData]
        public void Completes_TransferRequest_Wait_On_Upload_Request_If_Transfer_Is_Tracked(string username, IPEndPoint endpoint, int token, string filename)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var downloads = new ConcurrentDictionary<int, TransferInternal>();
            downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, username, filename, token));

            mocks.Client.Setup(m => m.DownloadDictionary)
                .Returns(downloads);

            var request = new TransferRequest(TransferDirection.Upload, token, filename);
            var message = request.ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.Is<TransferRequest>(t => t.Direction == request.Direction && t.Token == request.Token && t.Filename == request.Filename)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Does not complete TransferRequest wait on upload request if no downloads are tracked"), AutoData]
        public void Does_Not_Complete_TransferRequest_Wait_On_Upload_Request_If_No_Downloads_Are_Tracked(string username, IPEndPoint endpoint, int token, string filename)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var request = new TransferRequest(TransferDirection.Upload, token, filename);
            var message = request.ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.Is<TransferRequest>(t => t.Direction == request.Direction && t.Token == request.Token && t.Filename == request.Filename)), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes TransferRequest wait on upload request if transfer is tracked"), AutoData]
        public void Does_Not_Complete_TransferRequest_Wait_On_Upload_Request_If_Transfer_Is_Not_Tracked(string username, IPEndPoint endpoint, int token, string filename)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var downloads = new ConcurrentDictionary<int, TransferInternal>();
            downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, "not-username", filename, token));

            mocks.Client.Setup(m => m.DownloadDictionary)
                .Returns(downloads);

            var request = new TransferRequest(TransferDirection.Upload, token, filename);
            var message = request.ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.Is<TransferRequest>(t => t.Direction == request.Direction && t.Token == request.Token && t.Filename == request.Filename)), Times.Never);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Rejects TransferRequest upload request if transfer is not tracked"), AutoData]
        public void Rejects_TransferRequest_Upload_Request_If_Transfer_Is_Not_Tracked(string username, IPEndPoint endpoint, int token, string filename)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var request = new TransferRequest(TransferDirection.Upload, token, filename);
            var message = request.ToByteArray();

            handler.HandleMessageRead(mocks.PeerConnection.Object, message);

            var expected = new TransferResponse(token, "Cancelled").ToByteArray();

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<IOutgoingMessage>(msg => msg.ToByteArray().Matches(expected)), null), Times.Once);
        }

        [Trait("Category", "HandleMessageReceived")]
        [Theory(DisplayName = "Completes BrowseResponseConnection wait on browse response receipt"), AutoData]
        public void Completes_BrowseResponseConnection_Wait_On_Browse_Response_Receipt(string username, IPEndPoint endpoint)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var request = new BrowseResponse(Enumerable.Empty<Directory>());
            var message = request.ToByteArray();
            var args = new MessageReceivedEventArgs(message.Length, message.Skip(4).Take(4).ToArray());

            handler.HandleMessageReceived(mocks.PeerConnection.Object, args);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(Constants.WaitKey.BrowseResponseConnection, username), It.IsAny<(MessageReceivedEventArgs, IMessageConnection)>()), Times.Once);
        }

        [Trait("Category", "HandleMessageReceived")]
        [Theory(DisplayName = "Does nothing on unhandled MessageRecieved"), AutoData]
        public void Does_Nothing_On_Unhandled_MessageRecieved(string username, IPEndPoint endpoint)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var request = new BrowseRequest();
            var message = request.ToByteArray();
            var args = new MessageReceivedEventArgs(message.Length, message.Skip(4).Take(4).ToArray());

            var ex = Record.Exception(() => handler.HandleMessageReceived(mocks.PeerConnection.Object, args));

            Assert.Null(ex);
        }

        [Trait("Category", "HandleMessageWritten")]
        [Theory(DisplayName = "Creates diagnostic on MessageWritten"), AutoData]
        public void Creates_Diagnostic_On_MessageWritten(string username, IPEndPoint endpoint)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var request = new BrowseRequest();
            var message = request.ToByteArray();
            var args = new MessageEventArgs(message);

            handler.HandleMessageWritten(mocks.PeerConnection.Object, args);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Peer message sent: BrowseRequest"))));
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on exception handling BrowseResponse receipt"), AutoData]
        public void Creates_Diagnostic_On_Exception_Handling_BrowseResponse_Receipt(string username, IPEndPoint endpoint)
        {
            var (handler, mocks) = GetFixture(username, endpoint);

            var request = new BrowseResponse(Enumerable.Empty<Directory>());
            var message = request.ToByteArray();
            var args = new MessageReceivedEventArgs(message.Length, message.Skip(4).Take(4).ToArray());

            mocks.Waiter.Setup(m => m.Complete(new WaitKey(Constants.WaitKey.BrowseResponseConnection, username), It.IsAny<(MessageReceivedEventArgs, IMessageConnection)>()))
                .Throws(new Exception("foo"));

            handler.HandleMessageReceived(mocks.PeerConnection.Object, args);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("Error handling peer message")), It.IsAny<Exception>()), Times.Once);
        }

        private static (PeerMessageHandler Handler, Mocks Mocks) GetFixture(string username = null, IPEndPoint endpoint = null, SoulseekClientOptions options = null)
        {
            var mocks = new Mocks(options);

            endpoint = endpoint ?? new IPEndPoint(IPAddress.None, 0);

            mocks.ServerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.ServerConnection.Setup(m => m.IPEndPoint)
                .Returns(endpoint);

            mocks.PeerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.PeerConnection.Setup(m => m.IPEndPoint)
                .Returns(endpoint);

            var handler = new PeerMessageHandler(
                mocks.Client.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private class Mocks
        {
            public Mocks(SoulseekClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(clientOptions)
                {
                    CallBase = true,
                };

                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.DownloadDictionary).Returns(Downloads);
                Client.Setup(m => m.Searches).Returns(Searches);
                Client.Setup(m => m.ServerConnection).Returns(ServerConnection.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public ConcurrentDictionary<int, TransferInternal> Downloads { get; } = new ConcurrentDictionary<int, TransferInternal>();
            public ConcurrentDictionary<int, SearchInternal> Searches { get; } = new ConcurrentDictionary<int, SearchInternal>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IMessageConnection> PeerConnection { get; } = new Mock<IMessageConnection>();
        }
    }
}
