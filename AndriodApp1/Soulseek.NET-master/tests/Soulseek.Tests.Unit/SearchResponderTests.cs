// <copyright file="SearchResponderTests.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class SearchResponderTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            SearchResponder r = null;

            var ex = Record.Exception(() => (r, _) = GetFixture());

            Assert.Null(ex);
            Assert.NotNull(r);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if SoulseekClient is null")]
        public void Throws_If_SoulseekClient_Is_Null()
        {
            var ex = Record.Exception(() => new SearchResponder(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("soulseekClient", ((ArgumentNullException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Ensures Diagnostic given null")]
        public void Ensures_Diagnostic_Given_Null()
        {
            var (_, mocks) = GetFixture();

            SearchResponder r = default;
            var ex = Record.Exception(() => r = new SearchResponder(mocks.Client.Object));

            Assert.Null(ex);
            Assert.NotNull(r.GetProperty<IDiagnosticFactory>("Diagnostic"));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Uses given Diagnostic")]
        public void Uses_Given_Diagnostic()
        {
            var (_, mocks) = GetFixture();

            var diagnostic = new Mock<IDiagnosticFactory>().Object;

            SearchResponder r = default;
            var ex = Record.Exception(() => r = new SearchResponder(mocks.Client.Object, diagnostic));

            Assert.Null(ex);
            Assert.Equal(diagnostic, r.GetProperty<IDiagnosticFactory>("Diagnostic"));
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard removes token from cache"), AutoData]
        public void TryDiscard_Removes_Token_From_Cache(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var removed = responder.TryDiscard(responseToken);

            Assert.True(removed);

            cache.Verify(m => m.TryRemove(responseToken, out record), Times.Once);
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard raises ResponseDeliveryFailed when discarding"), AutoData]
        public void TryDiscard_Raises_ResponseDeliveryFailed_When_Discarding(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            SearchRequestResponseEventArgs args = null;
            responder.ResponseDeliveryFailed += (sender, e) => args = e;

            var removed = responder.TryDiscard(responseToken);

            Assert.True(removed);
            Assert.NotNull(args);
            Assert.Equal(username, args.Username);
            Assert.Equal(token, args.Token);
            Assert.Equal(query, args.Query);
            Assert.Equal(searchResponse, args.SearchResponse);
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard does not throw raising unbound ResponseDeliveryFailed when discarding"), AutoData]
        public void TryDiscard_Does_Not_Throw_Raising_Unbound_ResponseDeliveryFailed_When_Discarding(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var ex = Record.Exception(() => responder.TryDiscard(responseToken));

            Assert.Null(ex);
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard produces debug when discarding"), AutoData]
        public void TryDiscard_Produces_Debug_When_Discarding(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var removed = responder.TryDiscard(responseToken);

            Assert.True(removed);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Discarded cached search response {responseToken}"))), Times.Once);
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard returns false if not cached"), AutoData]
        public void TryDiscard_Returns_False_If_Not_Cached(int responseToken)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = default;

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(false);

            var removed = responder.TryDiscard(responseToken);

            Assert.False(removed);

            cache.Verify(m => m.TryRemove(responseToken, out record), Times.Once);
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard returns false if cache throws"), AutoData]
        public void TryDiscard_Returns_False_If_Cache_Throws(int responseToken)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = default;

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Throws(new Exception());

            var removed = responder.TryDiscard(responseToken);

            Assert.False(removed);

            cache.Verify(m => m.TryRemove(responseToken, out record), Times.Once);
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard returns false if cache throws"), AutoData]
        public void TryDiscard_Returns_Produces_Warning_If_Cache_Throws(int responseToken)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = default;

            var expectedEx = new Exception();

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Throws(expectedEx);

            var removed = responder.TryDiscard(responseToken);

            Assert.False(removed);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive($"Error removing cached search response {responseToken}")), expectedEx), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync returns false if ResponseResolver is null"), AutoData]
        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Is_Null(string username, int token, string query)
        {
            var (responder, _) = GetFixture(new SoulseekClientOptions(searchResponseResolver: null));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync returns false if ResponseResolver throws"), AutoData]
        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Throws(string username, int token, string query)
        {
            var (responder, _) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => throw new Exception()));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync generates warning if ResponseResolver throws"), AutoData]
        public async Task TryRespondAsync_Generates_Warning_If_ResponseResolver_Throws(string username, int token, string query)
        {
            var ex = new Exception();

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => throw ex));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("Error resolving search response")), ex), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync returns false if ResponseResolver returns null"), AutoData]
        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Returns_Null(string username, int token, string query)
        {
            var (responder, _) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult<SearchResponse>(null)));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync returns false if ResponseResolver returns zero files"), AutoData]
        public async Task TryRespondAsync_Returns_False_If_ResponseResolver_Returns_Zero_Files(string username, int token, string query)
        {
            var response = new SearchResponse(username, token, 0, 0, 0, new List<File>());
            var (responder, _) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(response)));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync raises RequestReceived"), AutoData]
        public async Task TryRespondAsync_Raises_RequestReceived(string username, int token, string query)
        {
            var (responder, _) = GetFixture();

            SearchRequestEventArgs args = null;
            responder.RequestReceived += (sender, e) => args = e;

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);
            Assert.NotNull(args);
            Assert.Equal(username, args.Username);
            Assert.Equal(token, args.Token);
            Assert.Equal(query, args.Query);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync sends response and returns true"), AutoData]
        public async Task TryRespondAsync_Sends_Response_And_Returns_True(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var conn = new Mock<IMessageConnection>();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.True(responded);

            conn.Verify(m => m.WriteAsync(It.Is<byte[]>(b => b.Matches(searchResponse.ToByteArray())), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync raises ResponseDelivered when sending response"), AutoData]
        public async Task TryRespondAsync_Raises_ResponseDelivered_When_Sending_Response(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var conn = new Mock<IMessageConnection>();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            SearchRequestResponseEventArgs args = null;
            responder.ResponseDelivered += (sender, e) => args = e;

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.True(responded);

            Assert.NotNull(args);
            Assert.Equal(username, args.Username);
            Assert.Equal(token, args.Token);
            Assert.Equal(query, args.Query);
            Assert.Equal(searchResponse, args.SearchResponse);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync generates debug when resolving response"), AutoData]
        public async Task TryRespondAsync_Generates_Debug_When_Resolving_Response(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var conn = new Mock<IMessageConnection>();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.True(responded);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Resolved"))), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync generates debug when sending response"), AutoData]
        public async Task TryRespondAsync_Generates_Debug_When_Sending_Response(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var conn = new Mock<IMessageConnection>();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(conn.Object));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.True(responded);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Sent response containing"))), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync returns false on failure"), AutoData]
        public async Task TryRespondAsync_Returns_False_On_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var ex = new ConnectionException();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IMessageConnection>(ex));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync caches response on connect failure"), AutoData]
        public async Task TryRespondAsync_Caches_Response_On_Connect_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var cache = new Mock<ISearchResponseCache>();

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(
                searchResponseCache: cache.Object,
                searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var ex = new ConnectionException();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IMessageConnection>(ex));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);

            var value = (username, token, query, searchResponse);
            cache.Verify(m => m.AddOrUpdate(responseToken, value), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync generates warning on cache add failure"), AutoData]
        public async Task TryRespondAsync_Generates_Warning_On_Cache_Add_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var value = (username, token, query, searchResponse);
            var cacheEx = new Exception();

            var cache = new Mock<ISearchResponseCache>();
            cache.Setup(m => m.AddOrUpdate(responseToken, value))
                .Throws(cacheEx);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(
                searchResponseCache: cache.Object,
                searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var ex = new ConnectionException();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IMessageConnection>(ex));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive("Error caching undelivered search response")), cacheEx), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync generates debug on failure"), AutoData]
        public async Task TryRespondAsync_Generates_Debug_On_Failure(string username, int token, string query, SearchResponse searchResponse, IPEndPoint endpoint, int responseToken)
        {
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseResolver: (u, t, q) => Task.FromResult(searchResponse)));

            mocks.Client.Setup(m => m.GetUserEndPointAsync(username, It.IsAny<CancellationToken?>()))
                .Returns(Task.FromResult(endpoint));
            mocks.Client.Setup(m => m.GetNextToken())
                .Returns(responseToken);

            var ex = new ConnectionException();

            mocks.PeerConnectionManager.Setup(m => m.GetOrAddMessageConnectionAsync(username, endpoint, responseToken, It.IsAny<CancellationToken>()))
                .Returns(Task.FromException<IMessageConnection>(ex));

            var responded = await responder.TryRespondAsync(username, token, query);

            Assert.False(responded);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive("Failed to send search response")), ex), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token returns false if cache is null"), AutoData]
        public async Task TryRespondAsync_Token_Returns_False_If_Cache_Is_Null(int responseToken)
        {
            var (responder, _) = GetFixture();

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token returns false if not cached"), AutoData]
        public async Task TryRespondAsync_Token_Returns_False_If_Not_Cached(int responseToken)
        {
            (string Username, int Token, string Query, SearchResponse SearchResponse) record = default;

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(false);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token returns false if cache throws"), AutoData]
        public async Task TryRespondAsync_Token_Returns_False_If_Cache_Throws(int responseToken)
        {
            (string Username, int Token, string Query, SearchResponse SearchResponse) record = default;

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Throws(new Exception());

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token produces warning if cache throws"), AutoData]
        public async Task TryRespondAsync_Token_Produces_Warning_If_Cache_Throws(int responseToken)
        {
            (string Username, int Token, string Query, SearchResponse SearchResponse) record = default;

            var expectedEx = new Exception();

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Throws(expectedEx);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.False(responded);

            mocks.Diagnostic.Verify(m => m.Warning(It.Is<string>(s => s.ContainsInsensitive($"Error retrieving cached search response {responseToken}")), expectedEx), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token returns true if delivered"), AutoData]
        public async Task TryRespondAsync_Token_Returns_True_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var conn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Returns(Task.FromResult(conn.Object));

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.True(responded);

            conn.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken?>()), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token produces debug if delivered"), AutoData]
        public async Task TryRespondAsync_Token_Produces_Debug_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var conn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Returns(Task.FromResult(conn.Object));

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.True(responded);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Sent cached response {responseToken}"))), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token raises ResponseDelivered if delivered"), AutoData]
        public async Task TryRespondAsync_Token_Raises_ResponseDelivered_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var conn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Returns(Task.FromResult(conn.Object));

            SearchRequestResponseEventArgs args = null;
            responder.ResponseDelivered += (sender, e) => args = e;

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.True(responded);
            Assert.NotNull(args);
            Assert.Equal(username, args.Username);
            Assert.Equal(token, args.Token);
            Assert.Equal(query, args.Query);
            Assert.Equal(searchResponse, args.SearchResponse);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token does not throw raising unbound ResponseDelivered if delivered"), AutoData]
        public async Task TryRespondAsync_Token_Does_Not_Throw_Raising_Unbound_ResponseDelivered_If_Delivered(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var conn = new Mock<IMessageConnection>();
            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Returns(Task.FromResult(conn.Object));

            var ex = await Record.ExceptionAsync(() => responder.TryRespondAsync(responseToken));

            Assert.Null(ex);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token returns false if delivery fails"), AutoData]
        public async Task TryRespondAsync_Token_Returns_False_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Throws(new Exception());

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.False(responded);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token produces debug if delivery fails"), AutoData]
        public async Task TryRespondAsync_Token_Produces_Debug_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            var ex = new Exception();

            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Throws(ex);

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.False(responded);

            mocks.Diagnostic.Verify(m => m.Debug(It.Is<string>(s => s.ContainsInsensitive($"Failed to send cached search response {responseToken}")), ex), Times.Once);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token raises ResponseDeliveryFailed if delivery fails"), AutoData]
        public async Task TryRespondAsync_Token_Raises_ResponseDeliveryFailed_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Throws(new Exception());

            SearchRequestResponseEventArgs args = null;
            responder.ResponseDeliveryFailed += (sender, e) => args = e;

            var responded = await responder.TryRespondAsync(responseToken);

            Assert.False(responded);
            Assert.NotNull(args);
            Assert.Equal(username, args.Username);
            Assert.Equal(token, args.Token);
            Assert.Equal(query, args.Query);
            Assert.Equal(searchResponse, args.SearchResponse);
        }

        [Trait("Category", "TryRespondAsync")]
        [Theory(DisplayName = "TryRespondAsync token does not throw raising unbound ResponseDeliveryFailed if delivery fails"), AutoData]
        public async Task TryRespondAsync_Token_Does_Not_Throw_Raising_Unbound_ResponseDeliveryFailed_If_Delivery_Fails(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var record = (username, token, query, searchResponse);

            var cache = GetCacheMock();
            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            mocks.PeerConnectionManager.Setup(m => m.GetCachedMessageConnectionAsync(username))
                .Throws(new Exception());

            var ex = await Record.ExceptionAsync(() => responder.TryRespondAsync(responseToken));

            Assert.Null(ex);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Diagnostic raises DiagnosticGenerated")]
        public void Diagnostic_Raises_DiagnosticGenerated()
        {
            var (_, mocks) = GetFixture();
            var responder = new SearchResponder(mocks.Client.Object);

            bool fired = false;
            responder.DiagnosticGenerated += (sender, e) => fired = true;

            var diag = responder.GetProperty<IDiagnosticFactory>("Diagnostic");
            diag.Info("test");

            Assert.True(fired);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Diagnostic does not throw raising unbound DiagnosticGenerated")]
        public void Diagnostic_Does_Not_Throw_Raising_Unbound_DiagnosticGenerated()
        {
            var (_, mocks) = GetFixture();
            var responder = new SearchResponder(mocks.Client.Object);

            var diag = responder.GetProperty<IDiagnosticFactory>("Diagnostic");

            var ex = Record.Exception(() => diag.Info("test"));

            Assert.Null(ex);
        }

        private (SearchResponder SearchResponder, Mocks Mocks) GetFixture(SoulseekClientOptions options = null)
        {
            var mocks = new Mocks(options);

            var responder = new SearchResponder(
                mocks.Client.Object,
                mocks.Diagnostic.Object);

            return (responder, mocks);
        }

        private Mock<ISearchResponseCache> GetCacheMock() => new Mock<ISearchResponseCache>();

        private class Mocks
        {
            public Mocks(SoulseekClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(clientOptions)
                {
                    CallBase = true,
                };

                Client.Setup(m => m.PeerConnectionManager)
                    .Returns(PeerConnectionManager.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
        }
    }
}
