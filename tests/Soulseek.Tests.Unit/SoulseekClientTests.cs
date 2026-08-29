// <copyright file="SoulseekClientTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class SoulseekClientTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with with given options")]
        public void Instantiates_With_Given_Options()
        {
            var options = new SoulseekClientOptions();

            using (var s = new SoulseekClient(options))
            {
                Assert.Equal(options, s.Options);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates without exception")]
        public void Instantiates_Without_Exception()
        {
            SoulseekClient s = null;

            var ex = Record.Exception(() => s = new SoulseekClient());

            Assert.Null(ex);
            Assert.NotNull(s);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "State is Disconnected initially")]
        public void State_Is_Disconnected_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Username is null initially")]
        public void Username_Is_Null_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.Null(s.Username);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "IPEndPoint is null initially")]
        public void IPEndPoint_Is_Null_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.Null(s.IPEndPoint);
                Assert.Null(s.IPAddress);
                Assert.Null(s.Port);
            }
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "ServerInfo is not null, but contains nulls initially")]
        public void ServerInfo_Is_Not_Null_But_Contains_Nulls_Initially()
        {
            using (var s = new SoulseekClient())
            {
                Assert.NotNull(s.ServerInfo);
                Assert.Null(s.ServerInfo.ParentMinSpeed);
                Assert.Null(s.ServerInfo.ParentSpeedRatio);
                Assert.Null(s.ServerInfo.WishlistInterval);
            }
        }

        [Trait("Category", "Port")]
        [Theory(DisplayName = "Port returns IPEndPoint port if not null"), AutoData]
        public void Port_Returns_IPEndPoint_Port_If_Not_Null(IPAddress ip, int port)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("IPEndPoint", new IPEndPoint(ip, port));

                Assert.Equal(port, s.Port);
            }
        }

        [Trait("Category", "IPAddress")]
        [Theory(DisplayName = "IPAddress returns IPEndPoint address if not null"), AutoData]
        public void IPAddress_Returns_IPEndPoint_Address_If_Not_Null(IPAddress ip, int port)
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("IPEndPoint", new IPEndPoint(ip, port));

                Assert.Equal(ip, s.IPAddress);
            }
        }

        [Trait("Category", "Uploads")]
        [Theory(DisplayName = "Uploads returns UploadDictionary snapshot"), AutoData]
        internal void Uploads_Returns_UploadDictionary_Snapshot(string one, string two)
        {
            using (var s = new SoulseekClient())
            {
                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.TryAdd(1, new TransferInternal(TransferDirection.Upload, one, one, 1));
                dict.TryAdd(2, new TransferInternal(TransferDirection.Upload, two, two, 2));

                s.SetProperty("UploadDictionary", dict);

                Assert.Equal(2, s.Uploads.Count);

                var list = s.Uploads.ToList();
                Assert.Equal(one, list[0].Filename);
                Assert.Equal(one, list[0].Username);
                Assert.Equal(1, list[0].Token);

                Assert.Equal(two, list[1].Filename);
                Assert.Equal(two, list[1].Username);
                Assert.Equal(2, list[1].Token);
            }
        }

        [Trait("Category", "Downloads")]
        [Theory(DisplayName = "Downloads returns DownloadsDictionary snapshot"), AutoData]
        internal void Downloads_Returns_DownloadsDictionary_Snapshot(string one, string two)
        {
            using (var s = new SoulseekClient())
            {
                var dict = new ConcurrentDictionary<int, TransferInternal>();
                dict.TryAdd(1, new TransferInternal(TransferDirection.Download, one, one, 1));
                dict.TryAdd(2, new TransferInternal(TransferDirection.Download, two, two, 2));

                s.SetProperty("DownloadDictionary", dict);

                Assert.Equal(2, s.Downloads.Count);

                var list = s.Downloads.ToList();
                Assert.Equal(one, list[0].Filename);
                Assert.Equal(one, list[0].Username);
                Assert.Equal(1, list[0].Token);

                Assert.Equal(two, list[1].Filename);
                Assert.Equal(two, list[1].Username);
                Assert.Equal(2, list[1].Token);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect handler disconnects")]
        public void Disconnect_Handler_Disconnects()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                s.InvokeMethod("ServerConnection_Disconnected", null, new ConnectionDisconnectedEventArgs(string.Empty));

                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect sets state to Disconnected")]
        public void Disconnect_Disconnects()
        {
            using (var s = new SoulseekClient())
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect raises StateChanged event")]
        public void Disconnect_Raises_StateChanged_Event()
        {
            var events = new List<SoulseekClientStateChangedEventArgs>();

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => events.Add(e);

                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);

                Assert.Equal(SoulseekClientStates.Disconnecting, events[0].State);
                Assert.Equal(SoulseekClientStates.Disconnected, events[1].State);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect does not raise StateChanged event if already disconnected")]
        public void Disconnect_Does_Not_Raise_StateChanged_Event_If_Already_Disconnected()
        {
            var fired = false;

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => fired = true;

                s.SetProperty("State", SoulseekClientStates.Disconnected);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.False(fired);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect does not raise StateChanged event if disconnecting")]
        public void Disconnect_Does_Not_Raise_StateChanged_Event_If_Disconnecting()
        {
            var fired = false;

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => fired = true;

                s.SetProperty("State", SoulseekClientStates.Disconnecting);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnecting, s.State);

                Assert.False(fired);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect uses default message if none is given")]
        public void Disconnect_Uses_Default_Message_If_None_Is_Given()
        {
            string message = default;

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => message = e.Message;

                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.Equal("Client disconnected", message);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnect uses given message"), AutoData]
        public void Disconnect_Uses_Given_Message(string msg)
        {
            string message = default;

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => message = e.Message;

                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = Record.Exception(() => s.Disconnect(msg));

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.Equal(msg, message);
            }
        }

        [Trait("Category", "Disconnect")]
        [Theory(DisplayName = "Disconnect uses Exception message if no message is supplied"), AutoData]
        public void Disconnect_Uses_Exception_Message_If_No_Message_Is_Supplied(string msg)
        {
            string message = default;
            var exception = new Exception(msg);

            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.StateChanged += (sender, e) => message = e.Message;

                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = Record.Exception(() => s.InvokeMethod("Disconnect", null, exception));

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                Assert.Equal(msg, message);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect clears searches")]
        public void Disconnect_Clears_Searches()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                using (var search1 = new SearchInternal(new SearchQuery(string.Empty), SearchScope.Network, 0, new SearchOptions()))
                using (var search2 = new SearchInternal(new SearchQuery(string.Empty), SearchScope.Network, 1, new SearchOptions()))
                {
                    var searches = new ConcurrentDictionary<int, SearchInternal>();
                    searches.TryAdd(0, search1);
                    searches.TryAdd(1, search2);

                    s.SetProperty("Searches", searches);

                    var ex = Record.Exception(() => s.Disconnect());

                    Assert.Null(ex);
                    Assert.Equal(SoulseekClientStates.Disconnected, s.State);
                    Assert.Empty(searches);
                }
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect does not clear downloads")]
        public void Disconnect_Clears_Downloads()
        {
            var c = new Mock<IMessageConnection>();

            using (var s = new SoulseekClient(serverConnection: c.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(0, new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 0));
                downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, string.Empty, string.Empty, 1));

                s.SetProperty("DownloadDictionary", downloads);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);
                Assert.NotEmpty(downloads);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect does not clear peer queue")]
        public void Disconnect_Clears_Peer_Queue()
        {
            var c = new Mock<IMessageConnection>();

            var p = new Mock<IPeerConnectionManager>();

            using (var s = new SoulseekClient(serverConnection: c.Object, peerConnectionManager: p.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected);

                var ex = Record.Exception(() => s.Disconnect());

                Assert.Null(ex);
                Assert.Equal(SoulseekClientStates.Disconnected, s.State);

                p.Verify(m => m.RemoveAndDisposeAll(), Times.Never);
            }
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect cancels searches")]
        public async Task Disconnect_Cancels_Searches()
        {
            var c = new Mock<IMessageConnection>();

            var p = new Mock<IPeerConnectionManager>();

            using (var search = new SearchInternal(new SearchQuery("foo"), SearchScope.Network, 1))
            {
                var searches = new ConcurrentDictionary<int, SearchInternal>();
                searches.TryAdd(1, search);

                using (var s = new SoulseekClient(serverConnection: c.Object, peerConnectionManager: p.Object))
                {
                    s.SetProperty("State", SoulseekClientStates.Connected);
                    s.SetProperty("Searches", searches);

                    var searchWait = search.WaitForCompletion(CancellationToken.None);

                    s.Disconnect();

                    var ex = await Record.ExceptionAsync(() => searchWait);

                    Assert.NotNull(ex);
                    Assert.IsType<OperationCanceledException>(ex);
                }
            }
        }

        [Trait("Category", "Dispose/Finalize")]
        [Fact(DisplayName = "Disposes without exception")]
        public void Disposes_Without_Exception()
        {
            using (var s = new SoulseekClient())
            {
                var ex = Record.Exception(() => s.Dispose());

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Dispose/Finalize")]
        [Fact(DisplayName = "Finalizes without exception")]
        public void Finalizes_Without_Exception()
        {
            using (var s = new SoulseekClient())
            {
                var ex = Record.Exception(() => s.InvokeMethod("Finalize"));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState does not throw if StateChange is unsubscribed")]
        public void ChangeState_Does_Not_Throw_If_StateChange_Is_Unsubscribed()
        {
            using (var s = new SoulseekClient())
            {
                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Connected, string.Empty, null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ChangeState")]
        [Theory(DisplayName = "ChangeState produces diagnostic"), AutoData]
        public void ChangeState_Produces_Diagnostic(string message, Exception exception)
        {
            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(diagnosticFactory: diagnostic.Object))
            {
                s.InvokeMethod("ChangeState", SoulseekClientStates.Disconnected, message, exception);
            }

            diagnostic.Verify(m => m.Debug(It.Is<string>(s => s == $"Client state changed from Disconnected to Disconnected; message: {message}")), Times.Once);
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState produces diagnostic and omits message if none is provided")]
        public void ChangeState_Produces_Diagnostic_And_Omits_Message_If_None_Is_Provided()
        {
            var diagnostic = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(diagnosticFactory: diagnostic.Object))
            {
                s.InvokeMethod("ChangeState", SoulseekClientStates.Disconnected, null, null);
            }

            diagnostic.Verify(m => m.Debug(It.Is<string>(s => s == $"Client state changed from Disconnected to Disconnected")), Times.Once);
        }

        [Trait("Category", "ChangeState")]
        [Theory(DisplayName = "ChangeState fires Disconnected event when transitioning to Disconnected"), AutoData]
        public void ChangeState_Fires_Disconnected_Event_When_Transitioning_To_Disconnected(string message, Exception exception)
        {
            using (var s = new SoulseekClient())
            {
                SoulseekClientDisconnectedEventArgs args = null;
                s.Disconnected += (sender, e) => args = e;

                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Disconnected, message, exception));

                Assert.Null(ex);
                Assert.NotNull(args);
                Assert.Equal(message, args.Message);
                Assert.Equal(exception, args.Exception);
            }
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState fires Connected event when transitioning to Connected")]
        public void ChangeState_Fires_Connected_Event_When_Transitioning_To_Connected()
        {
            using (var s = new SoulseekClient())
            {
                bool fired = false;
                s.Connected += (sender, e) => fired = true;

                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Connected, string.Empty, null));

                Assert.Null(ex);
                Assert.True(fired);
            }
        }

        [Trait("Category", "ChangeState")]
        [Fact(DisplayName = "ChangeState fires LoggedIn event when transitioning to LoggedIn")]
        public void ChangeState_Fires_LoggedIn_Event_When_Transitioning_To_LoggedIn()
        {
            using (var s = new SoulseekClient())
            {
                bool fired = false;
                s.LoggedIn += (sender, e) => fired = true;

                var ex = Record.Exception(() => s.InvokeMethod("ChangeState", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn, string.Empty, null));

                Assert.Null(ex);
                Assert.True(fired);
            }
        }

        [Trait("Category", "GetNextToken")]
        [Theory(DisplayName = "GetNextToken invokes TokenFactory"), AutoData]
        public void GetNextToken_Invokes_TokenFactory(int token)
        {
            var f = new Mock<ITokenFactory>();
            f.Setup(m => m.NextToken())
                .Returns(token);

            using (var s = new SoulseekClient(tokenFactory: f.Object))
            {
                var t = s.GetNextToken();

                Assert.Equal(token, t);

                f.Verify(m => m.NextToken(), Times.Once);
            }
        }

        [Trait("Category", "KickedFromServer")]
        [Fact(DisplayName = "Raises KickedFromServer when kicked from server")]
        public void Raises_KickedFromServer_When_Kicked_From_Server()
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                bool fired = false;
                s.KickedFromServer += (sender, args) => fired = true;

                handlerMock.Raise(m => m.KickedFromServer += null, EventArgs.Empty);

                Assert.True(fired);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Fact(DisplayName = "Raises DownloadFailed when user sends UploadFailed")]
        public void Raises_DownloadFailed_When_User_Sends_UploadFailed()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                DownloadFailedEventArgs args = null;
                s.DownloadFailed += (sender, e) => args = e;

                var expected = new DownloadFailedEventArgs("user", "file");
                handlerMock.Raise(m => m.DownloadFailed += null, expected);

                Assert.NotNull(args);
                Assert.Equal(expected, args);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Fact(DisplayName = "Does not throw if DownloadFailed is unbound when user sends UploadFailed")]
        public void Does_Not_Throw_If_DownloadFailed_Is_Unbound_When_User_Sends_UploadFailed()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var ex = Record.Exception(() => handlerMock.Raise(m => m.DownloadFailed += null, new DownloadFailedEventArgs("user", "file")));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Fact(DisplayName = "Raises DownloadDenied when user sends UploadDenied")]
        public void Raises_DownloadDenied_When_User_Sends_UploadDenied()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                DownloadDeniedEventArgs args = null;
                s.DownloadDenied += (sender, e) => args = e;

                var expected = new DownloadDeniedEventArgs("user", "file", "message");
                handlerMock.Raise(m => m.DownloadDenied += null, expected);

                Assert.NotNull(args);
                Assert.Equal(expected, args);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Fact(DisplayName = "Does not throw if DownloadDownload is unbound when user sends Uploaddenied")]
        public void Does_Not_Throw_If_DownloadDenied_Is_Unbound_When_User_Sends_Uploaddenied()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var ex = Record.Exception(() => handlerMock.Raise(m => m.DownloadDenied += null, new DownloadDeniedEventArgs("user", "file", "msg")));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Fact(DisplayName = "Does not throw if no matching download in DownloadDictionary when user sends UploadDenied")]
        public void Does_Not_Throw_If_No_Matching_Download_When_User_Sends_UploadDenied()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, "otheruser", "otherfile", 1));

                s.SetProperty("DownloadDictionary", downloads);

                var ex = Record.Exception(() => handlerMock.Raise(m => m.DownloadDenied += null, new DownloadDeniedEventArgs("user", "file", "msg")));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Fact(DisplayName = "Sets exception on RemoteTaskCompletionSource when one matching download exists")]
        public void Sets_Exception_On_RemoteTaskCompletionSource_When_One_Matching_Download_Exists_For_Denied()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var download = new TransferInternal(TransferDirection.Download, "user", "file", 1);
                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(1, download);

                s.SetProperty("DownloadDictionary", downloads);

                handlerMock.Raise(m => m.DownloadDenied += null, new DownloadDeniedEventArgs("user", "file", "denied"));

                Assert.True(download.RemoteTaskCompletionSource.Task.IsFaulted);
                Assert.IsType<TransferRejectedException>(download.RemoteTaskCompletionSource.Task.Exception.InnerException);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Fact(DisplayName = "Sets exception on both RemoteTaskCompletionSources when two matching downloads exist")]
        public void Sets_Exception_On_Both_RemoteTaskCompletionSources_When_Two_Matching_Downloads_Exist_For_Denied()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var download1 = new TransferInternal(TransferDirection.Download, "user", "file", 1);
                var download2 = new TransferInternal(TransferDirection.Download, "user", "file", 2);
                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(1, download1);
                downloads.TryAdd(2, download2);

                s.SetProperty("DownloadDictionary", downloads);

                handlerMock.Raise(m => m.DownloadDenied += null, new DownloadDeniedEventArgs("user", "file", "denied"));

                Assert.True(download1.RemoteTaskCompletionSource.Task.IsFaulted);
                Assert.IsType<TransferRejectedException>(download1.RemoteTaskCompletionSource.Task.Exception.InnerException);

                Assert.True(download2.RemoteTaskCompletionSource.Task.IsFaulted);
                Assert.IsType<TransferRejectedException>(download2.RemoteTaskCompletionSource.Task.Exception.InnerException);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Fact(DisplayName = "Invokes DownloadDenied event handler when exception is thrown in handler logic")]
        public void Invokes_DownloadDenied_Event_Handler_When_Exception_Is_Thrown_In_Handler_Logic()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();
            DownloadDeniedEventArgs capturedArgs = null;

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                // Set a null DownloadDictionary to cause an exception
                s.SetProperty("DownloadDictionary", null);

                s.DownloadDenied += (sender, e) => capturedArgs = e;

                var expectedArgs = new DownloadDeniedEventArgs("user", "file", "msg");
                var ex = Record.Exception(() => handlerMock.Raise(m => m.DownloadDenied += null, expectedArgs));

                Assert.Null(ex);
                Assert.NotNull(capturedArgs);
                Assert.Equal(expectedArgs, capturedArgs);
            }
        }

        [Trait("Category", "DownloadDenied")]
        [Fact(DisplayName = "Generates diagnostic warning containing 'rejected' when exception is thrown in handler logic")]
        public void Generates_Diagnostic_Warning_Containing_Rejected_When_Exception_Is_Thrown_In_Handler_Logic()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();
            var diagnosticMock = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object, diagnosticFactory: diagnosticMock.Object))
            {
                // Set a null DownloadDictionary to cause an exception
                s.SetProperty("DownloadDictionary", null);

                var expectedArgs = new DownloadDeniedEventArgs("user", "file", "msg");
                handlerMock.Raise(m => m.DownloadDenied += null, expectedArgs);

                diagnosticMock.Verify(m => m.Warning(It.Is<string>(msg => msg.Contains("rejected")), It.IsAny<Exception>()), Times.Once);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Fact(DisplayName = "Does not throw if no matching download in DownloadDictionary when user sends UploadFailed")]
        public void Does_Not_Throw_If_No_Matching_Download_When_User_Sends_UploadFailed()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(1, new TransferInternal(TransferDirection.Download, "otheruser", "otherfile", 1));

                s.SetProperty("DownloadDictionary", downloads);

                var ex = Record.Exception(() => handlerMock.Raise(m => m.DownloadFailed += null, new DownloadFailedEventArgs("user", "file")));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Fact(DisplayName = "Sets exception on RemoteTaskCompletionSource when one matching download exists")]
        public void Sets_Exception_On_RemoteTaskCompletionSource_When_One_Matching_Download_Exists()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var download = new TransferInternal(TransferDirection.Download, "user", "file", 1);
                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(1, download);

                s.SetProperty("DownloadDictionary", downloads);

                handlerMock.Raise(m => m.DownloadFailed += null, new DownloadFailedEventArgs("user", "file"));

                Assert.True(download.RemoteTaskCompletionSource.Task.IsFaulted);
                Assert.IsType<TransferException>(download.RemoteTaskCompletionSource.Task.Exception.InnerException);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Fact(DisplayName = "Sets exception on both RemoteTaskCompletionSources when two matching downloads exist")]
        public void Sets_Exception_On_Both_RemoteTaskCompletionSources_When_Two_Matching_Downloads_Exist()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                var download1 = new TransferInternal(TransferDirection.Download, "user", "file", 1);
                var download2 = new TransferInternal(TransferDirection.Download, "user", "file", 2);
                var downloads = new ConcurrentDictionary<int, TransferInternal>();
                downloads.TryAdd(1, download1);
                downloads.TryAdd(2, download2);

                s.SetProperty("DownloadDictionary", downloads);

                handlerMock.Raise(m => m.DownloadFailed += null, new DownloadFailedEventArgs("user", "file"));

                Assert.True(download1.RemoteTaskCompletionSource.Task.IsFaulted);
                Assert.IsType<TransferException>(download1.RemoteTaskCompletionSource.Task.Exception.InnerException);

                Assert.True(download2.RemoteTaskCompletionSource.Task.IsFaulted);
                Assert.IsType<TransferException>(download2.RemoteTaskCompletionSource.Task.Exception.InnerException);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Fact(DisplayName = "Invokes DownloadFailed event handler when exception is thrown in handler logic")]
        public void Invokes_DownloadFailed_Event_Handler_When_Exception_Is_Thrown_In_Handler_Logic()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();
            DownloadFailedEventArgs capturedArgs = null;

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object))
            {
                // Set a null DownloadDictionary to cause an exception
                s.SetProperty("DownloadDictionary", null);

                s.DownloadFailed += (sender, e) => capturedArgs = e;

                var expectedArgs = new DownloadFailedEventArgs("user", "file");
                var ex = Record.Exception(() => handlerMock.Raise(m => m.DownloadFailed += null, expectedArgs));

                Assert.Null(ex);
                Assert.NotNull(capturedArgs);
                Assert.Equal(expectedArgs, capturedArgs);
            }
        }

        [Trait("Category", "DownloadFailed")]
        [Fact(DisplayName = "Generates diagnostic warning containing 'failed' when exception is thrown in handler logic")]
        public void Generates_Diagnostic_Warning_Containing_Failed_When_Exception_Is_Thrown_In_Handler_Logic()
        {
            var handlerMock = new Mock<IPeerMessageHandler>();
            var diagnosticMock = new Mock<IDiagnosticFactory>();

            using (var s = new SoulseekClient(peerMessageHandler: handlerMock.Object, diagnosticFactory: diagnosticMock.Object))
            {
                // Set a null DownloadDictionary to cause an exception
                s.SetProperty("DownloadDictionary", null);

                var expectedArgs = new DownloadFailedEventArgs("user", "file");
                handlerMock.Raise(m => m.DownloadFailed += null, expectedArgs);

                diagnosticMock.Verify(m => m.Warning(It.Is<string>(msg => msg.Contains("failed")), It.IsAny<Exception>()), Times.Once);
            }
        }

        [Trait("Category", "KickedFromServer")]
        [Fact(DisplayName = "Disconnects when kicked from server")]
        public void Disconnects_When_Kicked_From_Server()
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                s.SetProperty("State", SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                SoulseekClientDisconnectedEventArgs e = null;
                s.Disconnected += (sender, args) => e = args;

                handlerMock.Raise(m => m.KickedFromServer += null, EventArgs.Empty);

                Assert.True(e.Exception is KickedFromServerException);
            }
        }

        [Trait("Category", "GlobalMessageRecieved")]
        [Theory(DisplayName = "Raises GlobalMessageRecieved on receipt"), AutoData]
        public void Raises_GlobalMessageReceived_On_Receipt(string msg)
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                string args = default;
                s.GlobalMessageReceived += (sender, e) => args = e;

                handlerMock.Raise(m => m.GlobalMessageReceived += null, this, msg);

                Assert.NotNull(args);
                Assert.Equal(msg, args);
            }
        }

        [Trait("Category", "GlobalMessageRecieved")]
        [Theory(DisplayName = "Does not throw when GlobalMessageRecieved and no handler bound"), AutoData]
        public void Does_Not_Throw_When_GlobalMessageReceived_And_No_Handler_Bound(string msg)
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                var ex = Record.Exception(() => handlerMock.Raise(m => m.GlobalMessageReceived += null, this, msg));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerInfoReceived")]
        [Theory(DisplayName = "Raises ServerInfoReceived on receipt"), AutoData]
        public void Raises_ServerInfoReceived_On_Receipt(ServerInfo info)
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                ServerInfo args = default;
                s.ServerInfoReceived += (sender, e) => args = e;

                handlerMock.Raise(m => m.ServerInfoReceived += null, this, info);

                Assert.NotNull(args);
                Assert.Equal(info.ParentMinSpeed, args.ParentMinSpeed);
                Assert.Equal(info.ParentSpeedRatio, args.ParentSpeedRatio);
                Assert.Equal(info.WishlistInterval, args.WishlistInterval);
                Assert.Equal(info.IsSupporter, args.IsSupporter);
            }
        }

        [Trait("Category", "ServerInfoReceived")]
        [Fact(DisplayName = "Does not throw when ServerInfoReceived and no handler bound")]
        public void Does_Not_Throw_When_ServerInfoReceived_And_No_Handler_Bound()
        {
            var handlerMock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                var ex = Record.Exception(() => handlerMock.Raise(m => m.ServerInfoReceived += null, this, new ServerInfo()));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "MessageRead")]
        [Fact(DisplayName = "MessageRead invokes HandleMessageRead")]
        public void MessageRead_Invokes_HandleMessageRead()
        {
            var handlerMock = new Mock<IServerMessageHandler>();
            var args = new MessageEventArgs(new byte[4]);

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                s.InvokeMethod("ServerConnection_MessageRead", this, args);
            }

            handlerMock.Verify(m => m.HandleMessageRead(It.IsAny<object>(), args), Times.Once);
        }

        [Trait("Category", "MessageWritten")]
        [Fact(DisplayName = "MessageWritten invokes HandleMessageWritten")]
        public void MessageWritten_Invokes_HandleMessageWritten()
        {
            var handlerMock = new Mock<IServerMessageHandler>();
            var args = new MessageEventArgs(new byte[4]);

            using (var s = new SoulseekClient(serverMessageHandler: handlerMock.Object))
            {
                s.InvokeMethod("ServerConnection_MessageWritten", this, args);
            }

            handlerMock.Verify(m => m.HandleMessageWritten(It.IsAny<object>(), args), Times.Once);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when ListenerHandler raises")]
        public void Raises_DiagnosticGenerated_When_ListenerHandler_Raises()
        {
            var mock = new Mock<IListenerHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(listenerHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when ListenerHandler raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_ListenerHandler_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IListenerHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(listenerHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when PeerMessageHandler raises")]
        public void Raises_DiagnosticGenerated_When_PeerMessageHandler_Raises()
        {
            var mock = new Mock<IPeerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(peerMessageHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when PeerMessageHandler raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_PeerMessageHandler_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IPeerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(peerMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when DistributedMessageHandler raises")]
        public void Raises_DiagnosticGenerated_When_DistributedMessageHandler_Raises()
        {
            var mock = new Mock<IDistributedMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(distributedMessageHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when DistributedMessageHandler raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_DistributedMessageHandler_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IDistributedMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(distributedMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when PeerConnectionManager raises")]
        public void Raises_DiagnosticGenerated_When_PeerConnectionManager_Raises()
        {
            var mock = new Mock<IPeerConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(peerConnectionManager: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when PeerConnectionManager raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_PeerConnectionManager_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IPeerConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(peerConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Raises DiagnosticGenerated when DistributedConnectionManager raises")]
        public void Raises_DiagnosticGenerated_When_DistributedConnectionManager_Raises()
        {
            var mock = new Mock<IDistributedConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            object raiser = null;
            DiagnosticEventArgs raisedArgs = null;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) =>
                {
                    raiser = sender;
                    raisedArgs = args;
                };

                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);
            }

            Assert.NotNull(raiser);
            Assert.Equal(mock.Object, raiser);

            Assert.NotNull(raisedArgs);
            Assert.Equal(expectedArgs, raisedArgs);
        }

        [Trait("Category", "Event")]
        [Fact(DisplayName = "Does not throw when DistributedConnectionManager raises if diagnostic handler not bound")]
        public void Does_Not_Throw_When_DistributedConnectionManager_Raises_If_Diagnostic_Handler_Not_Bound()
        {
            var mock = new Mock<IDistributedConnectionManager>();
            var expectedArgs = new DiagnosticEventArgs(DiagnosticLevel.Info, "foo");

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserStatusChanged fires when handler raises"), AutoData]
        public void UserStatusChanged_Fires_When_Handler_Raises(string username, UserPresence presense, bool privileged)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserStatus(username, presense, privileged);
            UserStatus actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.UserStatusChanged += (sender, args) => actualArgs = args;
                mock.Raise(m => m.UserStatusChanged += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserStatusChanged does not throw if event not bound"), AutoData]
        public void UserStatusChanged_Does_Not_Throw_If_Event_Not_Bound(string username, UserPresence presense, bool privileged)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserStatus(username, presense, privileged);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.UserStatusChanged += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserStatisticsChanged fires when handler raises"), AutoData]
        public void UserStatisticsChangedFires_When_Handler_Raises(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserStatistics(username, averageSpeed, uploadCount, fileCount, directoryCount);
            UserStatistics actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.UserStatisticsChanged += (sender, args) => actualArgs = args;
                mock.Raise(m => m.UserStatisticsChanged += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserStatisticsChanged does not throw if event not bound"), AutoData]
        public void UserStatisticsChanged_Does_Not_Throw_If_Event_Not_Bound(string username, int averageSpeed, long uploadCount, int fileCount, int directoryCount)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserStatistics(username, averageSpeed, uploadCount, fileCount, directoryCount);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.UserStatisticsChanged += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "DistributedNetworkReset fires when handler raises")]
        public void DistributedNetworkReset_Fires_When_Handler_Raises()
        {
            var mock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                bool fired = false;

                s.DistributedNetworkReset += (sender, args) => fired = true;
                mock.Raise(m => m.DistributedNetworkReset += null, mock.Object, EventArgs.Empty);

                Assert.True(fired);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "DistributedNetworkReset does not throw if event not bound")]
        public void DistributedNetworkReset_Does_Not_Throw_If_Event_Not_Bound()
        {
            var mock = new Mock<IServerMessageHandler>();

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DistributedNetworkReset += null, mock.Object, EventArgs.Empty));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "DistributedNetworkStatusChanged fires when handler raises")]
        public void DistributedNetworkStatusChanged_Fires_When_Handler_Raises()
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                bool fired = false;

                s.DistributedNetworkStateChanged += (sender, args) => fired = true;
                mock.Raise(m => m.StateChanged += null, mock.Object, new DistributedNetworkInfo(0, 1, "root", true, 1, true, null, default, true));

                Assert.True(fired);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "DistributedNetworkStateChanged does not throw if event not bound")]
        public void DistributedNetworkStateChanged_Does_Not_Throw_If_Event_Not_Bound()
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.StateChanged += null, mock.Object, new DistributedNetworkInfo(0, 1, "root", true, 1, true, null, default, true)));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateMessageReceived fires when handler raises"), AutoData]
        public void PrivateMessageReceived_Fires_When_Handler_Raises(int id, DateTime timestamp, string username, string message, bool isAdmin)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivateMessageReceivedEventArgs(id, timestamp, username, message, isAdmin);
            PrivateMessageReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateMessageReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateMessageReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateMessageReceived does not throw if event not bound"), AutoData]
        public void PrivateMessageReceived_Does_Not_Throw_If_Event_Not_Bound(int id, DateTime timestamp, string username, string message, bool isAdmin)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivateMessageReceivedEventArgs(id, timestamp, username, message, isAdmin);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateMessageReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegedUserListReceived fires when handler raises"), AutoData]
        public void PrivilegedUserListReceived_Fires_When_Handler_Raises(string[] usernames)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = usernames.ToList().AsReadOnly();
            IReadOnlyCollection<string> actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivilegedUserListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivilegedUserListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "ExcludedSearchPhrasesReceived fires when handler raises"), AutoData]
        public void ExcludedSearchPhrasesReceived_Fires_When_Handler_Raises(string[] usernames)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = usernames.ToList().AsReadOnly();
            IReadOnlyCollection<string> actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.ExcludedSearchPhrasesReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.ExcludedSearchPhrasesReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegedUserListReceived does not throw if event not bound"), AutoData]
        public void PrivilegedUserListReceived_Does_Not_Throw_If_Event_Not_Bound(string[] usernames)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = usernames.ToList().AsReadOnly();

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivilegedUserListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "ExcludedSearchPhrasesReceived does not throw if event not bound"), AutoData]
        public void ExcludedSearchPhrasesReceived_Does_Not_Throw_If_Event_Not_Bound(string[] usernames)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = usernames.ToList().AsReadOnly();

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.ExcludedSearchPhrasesReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegeNotificationReceived fires when handler raises"), AutoData]
        public void PrivilegeNotificationReceived_Fires_When_Handler_Raises(string username, int id)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivilegeNotificationReceivedEventArgs(username, id);
            PrivilegeNotificationReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivilegeNotificationReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivilegeNotificationReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivilegeNotificationReceived does not throw if event not bound"), AutoData]
        public void PrivilegeNotificationReceived_Does_Not_Throw_If_Event_Not_Bound(string username, int id)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PrivilegeNotificationReceivedEventArgs(username, id);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivilegeNotificationReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserCannotConnect fires when handler raises"), AutoData]
        public void UserCannotConnect_Fires_When_Handler_Raises(int token, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserCannotConnectEventArgs(token, username);
            UserCannotConnectEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.UserCannotConnect += (sender, args) => actualArgs = args;
                mock.Raise(m => m.UserCannotConnect += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "UserCannotConnect does not throw if event not bound"), AutoData]
        public void UserCannotConnect_Does_Not_Throw_If_Event_Not_Bound(int token, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new UserCannotConnectEventArgs(token, username);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.UserCannotConnect += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomMessageReceived fires when handler raises"), AutoData]
        public void RoomMessageReceived_Fires_When_Handler_Raises(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomMessageReceivedEventArgs(roomName, username, message);
            RoomMessageReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomMessageReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomMessageReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomMessageReceived does not throw if event not bound"), AutoData]
        public void RoomMessageReceived_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomMessageReceivedEventArgs(roomName, username, message);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomMessageReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerAdded fires when handler raises"), AutoData]
        public void RoomTickerAdded_Fires_When_Handler_Raises(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerAddedEventArgs(roomName, new RoomTicker(username, message));
            RoomTickerAddedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomTickerAdded += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomTickerAdded += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerAdded does not throw if event not bound"), AutoData]
        public void RoomTickerAdded_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerAddedEventArgs(roomName, new RoomTicker(username, message));

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomTickerAdded += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerRemoved fires when handler raises"), AutoData]
        public void RoomTickerRemoved_Fires_When_Handler_Raises(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerRemovedEventArgs(roomName, username);
            RoomTickerRemovedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomTickerRemoved += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomTickerRemoved += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerRemoved does not throw if event not bound"), AutoData]
        public void RoomTickerRemoved_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerRemovedEventArgs(roomName, username);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomTickerRemoved += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerListReceived fires when handler raises"), AutoData]
        public void RoomTickerListReceived_Fires_When_Handler_Raises(string roomName, List<RoomTicker> tickers)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerListReceivedEventArgs(roomName, tickers);
            RoomTickerListReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomTickerListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomTickerListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomTickerListReceived does not throw if event not bound"), AutoData]
        public void RoomTickerListReceived_Does_Not_Throw_If_Event_Not_Bound(string roomName, List<RoomTicker> tickers)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomTickerListReceivedEventArgs(roomName, tickers);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomTickerListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PublicChatMessageReceived fires when handler raises"), AutoData]
        public void PublicChatMessageReceived_Fires_When_Handler_Raises(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PublicChatMessageReceivedEventArgs(roomName, username, message);
            PublicChatMessageReceivedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PublicChatMessageReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PublicChatMessageReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PublicChatMessageReceived does not throw if event not bound"), AutoData]
        public void PublicChatMessageReceived_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new PublicChatMessageReceivedEventArgs(roomName, username, message);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PublicChatMessageReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomJoined fires when handler raises"), AutoData]
        public void RoomJoined_Fires_When_Handler_Raises(string roomName, string username, UserData userData)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomJoinedEventArgs(roomName, username, userData);
            RoomJoinedEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomJoined += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomJoined += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomJoined does not throw if event not bound"), AutoData]
        public void RoomJoined_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username, UserData userData)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomJoinedEventArgs(roomName, username, userData);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomJoined += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomLeft fires when handler raises"), AutoData]
        public void RoomLeft_Fires_When_Handler_Raises(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomLeftEventArgs(roomName, username);
            RoomLeftEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomLeft += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomLeft += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomLeft does not throw if event not bound"), AutoData]
        public void RoomLeft_Does_Not_Throw_If_Event_Not_Bound(string roomName, string username)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new RoomLeftEventArgs(roomName, username);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomLeft += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomListReceived fires when handler raises"), AutoData]
        public void RoomListReceived_Fires_When_Handler_Raises(RoomList rooms)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = rooms;
            RoomList actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.RoomListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.RoomListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "RoomListReceived does not throw if event not bound"), AutoData]
        public void RoomListReceived_Does_Not_Throw_If_Event_Not_Bound(RoomList rooms)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = rooms;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RoomListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipAdded fires when handler raises"), AutoData]
        public void PrivateRoomMembershipAdded_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomMembershipAdded += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomMembershipAdded += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipAdded does not throw if event not bound"), AutoData]
        public void PrivateRoomMembershipAdded_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomMembershipAdded += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipRemoved fires when handler raises"), AutoData]
        public void PrivateRoomMembershipRemoved_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomMembershipRemoved += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomMembershipRemoved += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomMembershipRemoved does not throw if event not bound"), AutoData]
        public void PrivateRoomMembershipRemoved_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomMembershipRemoved += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationAdded fires when handler raises"), AutoData]
        public void PrivateRoomModerationAdded_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomModerationAdded += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomModerationAdded += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationAdded does not throw if event not bound"), AutoData]
        public void PrivateRoomModerationAdded_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomModerationAdded += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationRemoved fires when handler raises"), AutoData]
        public void PrivateRoomModerationRemoved_Fires_When_Handler_Raises(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;
            string actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomModerationRemoved += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomModerationRemoved += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModerationRemoved does not throw if event not bound"), AutoData]
        public void PrivateRoomModerationRemoved_Does_Not_Throw_If_Event_Not_Bound(string room)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = room;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomModerationRemoved += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomUserListReceived fires when handler raises"), AutoData]
        public void PrivateRoomUserListReceived_Fires_When_Handler_Raises(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;
            RoomInfo actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomUserListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomUserListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomUserListReceived does not throw if event not bound"), AutoData]
        public void PrivateRoomUserListReceived_Does_Not_Throw_If_Event_Not_Bound(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomUserListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModeratedUserListReceived fires when handler raises"), AutoData]
        public void PrivateRoomModeratedUserListReceived_Fires_When_Handler_Raises(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;
            RoomInfo actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.PrivateRoomModeratedUserListReceived += (sender, args) => actualArgs = args;
                mock.Raise(m => m.PrivateRoomModeratedUserListReceived += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "PrivateRoomModeratedUserListReceived does not throw if event not bound"), AutoData]
        public void PrivateRoomModeratedUserListReceived_Does_Not_Throw_If_Event_Not_Bound(RoomInfo info)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = info;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PrivateRoomModeratedUserListReceived += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "PromotedToDistributedBranchRoot fires when handler raises")]
        public void PromotedToDistributedBranchRoot_Fires_When_Handler_Raises()
        {
            var mock = new Mock<IDistributedConnectionManager>();
            var fired = false;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.PromotedToDistributedBranchRoot += (sender, args) => fired = true;
                mock.Raise(m => m.PromotedToBranchRoot += null, mock.Object, EventArgs.Empty);

                Assert.True(fired);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "PromotedToDistributedBranchRoot does not throw if event not bound")]
        public void PromotedToDistributedBranchRoot_Does_Not_Throw_If_Event_Not_Bound()
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.PromotedToBranchRoot += null, mock.Object, EventArgs.Empty));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "DemotedFromDistributedBranchRoot fires when handler raises")]
        public void DemotedFromDistributedBranchRoot_Fires_When_Handler_Raises()
        {
            var mock = new Mock<IDistributedConnectionManager>();
            var fired = false;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.DemotedFromDistributedBranchRoot += (sender, args) => fired = true;
                mock.Raise(m => m.DemotedFromBranchRoot += null, mock.Object, EventArgs.Empty);

                Assert.True(fired);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Fact(DisplayName = "DemotedFromDistributedBranchRoot does not throw if event not bound")]
        public void DemotedFromDistributedBranchRoot_Does_Not_Throw_If_Event_Not_Bound()
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DemotedFromBranchRoot += null, mock.Object, EventArgs.Empty));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedParentAdopted fires when handler raises"), AutoData]
        public void DistributedParentAdopted_Fires_When_Handler_Raises(DistributedParentEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();
            DistributedParentEventArgs actual = default;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.DistributedParentAdopted += (sender, e) => actual = e;
                mock.Raise(m => m.ParentAdopted += null, mock.Object, args);

                Assert.Equal(args, actual);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedParentAdopted does not throw if event not bound"), AutoData]
        public void DistributedParentAdopted_Does_Not_Throw_If_Event_Not_Bound(DistributedParentEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.ParentAdopted += null, mock.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedParentDisconnected fires when handler raises"), AutoData]
        public void DistributedParentDisconnected_Fires_When_Handler_Raises(DistributedParentEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();
            DistributedParentEventArgs actual = default;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.DistributedParentDisconnected += (sender, e) => actual = e;
                mock.Raise(m => m.ParentDisconnected += null, mock.Object, args);

                Assert.Equal(args, actual);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedParentDisconnected does not throw if event not bound"), AutoData]
        public void DistributedParentDisconnected_Does_Not_Throw_If_Event_Not_Bound(DistributedParentEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.ParentDisconnected += null, mock.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedChildAdded fires when handler raises"), AutoData]
        public void DistributedChildAdded_Fires_When_Handler_Raises(DistributedChildEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();
            DistributedChildEventArgs actual = default;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.DistributedChildAdded += (sender, e) => actual = e;
                mock.Raise(m => m.ChildAdded += null, mock.Object, args);

                Assert.Equal(args, actual);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedChildAdded does not throw if event not bound"), AutoData]
        public void DistributedChildAdded_Does_Not_Throw_If_Event_Not_Bound(DistributedChildEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.ChildAdded += null, mock.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedChildDisconnected fires when handler raises"), AutoData]
        public void DistributedChildDisconnected_Fires_When_Handler_Raises(DistributedChildEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();
            DistributedChildEventArgs actual = default;

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                s.DistributedChildDisconnected += (sender, e) => actual = e;
                mock.Raise(m => m.ChildDisconnected += null, mock.Object, args);

                Assert.Equal(args, actual);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "DistributedChildDisconnected does not throw if event not bound"), AutoData]
        public void DistributedChildDisconnected_Does_Not_Throw_If_Event_Not_Bound(DistributedChildEventArgs args)
        {
            var mock = new Mock<IDistributedConnectionManager>();

            using (var s = new SoulseekClient(distributedConnectionManager: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.ChildDisconnected += null, mock.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "ServerMessageHandler DiagnosticGenerated fires when handler raises"), AutoData]
        public void ServerMessageHandler_DiagnosticGenerated_Fires_When_Handler_Raises(DiagnosticLevel level, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(level, message);
            DiagnosticEventArgs actualArgs = null;

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) => actualArgs = args;
                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "ServerMessageHandler Event")]
        [Theory(DisplayName = "ServerMessageHandler DiagnosticGenerated does not throw if event not bound"), AutoData]
        public void ServerMessageHandler_DiagnosticGenerated_Does_Not_Throw_If_Event_Not_Bound(DiagnosticLevel level, string message)
        {
            var mock = new Mock<IServerMessageHandler>();
            var expectedArgs = new DiagnosticEventArgs(level, message);

            using (var s = new SoulseekClient(serverMessageHandler: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchRequestReceived fires when responder raises"), AutoData]
        public void SearchRequestReceived_Fires_When_Responder_Raises(SearchRequestEventArgs args)
        {
            var mock = new Mock<ISearchResponder>();
            SearchRequestEventArgs actual = default;

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                s.SearchRequestReceived += (sender, e) => actual = e;
                mock.Raise(m => m.RequestReceived += null, mock.Object, args);

                Assert.Equal(args, actual);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchRequestReceived does not throw if event not bound"), AutoData]
        public void SearchRequestReceived_Does_Not_Throw_If_Event_Not_Bound(SearchRequestEventArgs args)
        {
            var mock = new Mock<ISearchResponder>();

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.RequestReceived += null, mock.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchRequestReceived fires when responder raises"), AutoData]
        public void SearchResponseDelivered_Fires_When_Responder_Raises(SearchRequestResponseEventArgs args)
        {
            var mock = new Mock<ISearchResponder>();
            SearchRequestResponseEventArgs actual = default;

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                s.SearchResponseDelivered += (sender, e) => actual = e;
                mock.Raise(m => m.ResponseDelivered += null, mock.Object, args);

                Assert.Equal(args, actual);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchResponseDelivered does not throw if event not bound"), AutoData]
        public void SearchResponseDelivered_Does_Not_Throw_If_Event_Not_Bound(SearchRequestResponseEventArgs args)
        {
            var mock = new Mock<ISearchResponder>();

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.ResponseDelivered += null, mock.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchRequestReceived fires when responder raises"), AutoData]
        public void SearchResponseDeliveryFailed_Fires_When_Responder_Raises(SearchRequestResponseEventArgs args)
        {
            var mock = new Mock<ISearchResponder>();
            SearchRequestResponseEventArgs actual = default;

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                s.SearchResponseDeliveryFailed += (sender, e) => actual = e;
                mock.Raise(m => m.ResponseDeliveryFailed += null, mock.Object, args);

                Assert.Equal(args, actual);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchResponseDeliveryFailed does not throw if event not bound"), AutoData]
        public void SearchResponseDeliveryFailed_Does_Not_Throw_If_Event_Not_Bound(SearchRequestResponseEventArgs args)
        {
            var mock = new Mock<ISearchResponder>();

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.ResponseDeliveryFailed += null, mock.Object, args));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchResponder DiagnosticGenerated fires when handler raises"), AutoData]
        public void SearchResponder_DiagnosticGenerated_Fires_When_SearchResponder_Raises(DiagnosticLevel level, string message)
        {
            var mock = new Mock<ISearchResponder>();
            var expectedArgs = new DiagnosticEventArgs(level, message);
            DiagnosticEventArgs actualArgs = null;

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                s.DiagnosticGenerated += (sender, args) => actualArgs = args;
                mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs);

                Assert.NotNull(actualArgs);
                Assert.Equal(expectedArgs, actualArgs);
            }
        }

        [Trait("Category", "SearchResponder Event")]
        [Theory(DisplayName = "SearchResponder DiagnosticGenerated does not throw if event not bound"), AutoData]
        public void SearchResponder_DiagnosticGenerated_Does_Not_Throw_If_Event_Not_Bound(DiagnosticLevel level, string message)
        {
            var mock = new Mock<ISearchResponder>();
            var expectedArgs = new DiagnosticEventArgs(level, message);

            using (var s = new SoulseekClient(searchResponder: mock.Object))
            {
                var ex = Record.Exception(() => mock.Raise(m => m.DiagnosticGenerated += null, mock.Object, expectedArgs));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Diagnostic does not throw if event not bound"), AutoData]
        public void Diagnostic_Does_Not_Throw_If_Event_Not_Bound(string message)
        {
            using (var s = new SoulseekClient())
            {
                DiagnosticFactory d = s.GetProperty<DiagnosticFactory>("Diagnostic");

                var ex = Record.Exception(() => d.Info(message));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Diagnostic raises DiagnosticGenerated"), AutoData]
        public void Diagnostic_Raises_DiagnosticGenerated(string message)
        {
            string actualMessage = null;

            using (var s = new SoulseekClient())
            {
                s.DiagnosticGenerated += (sender, m) => actualMessage = m.Message;
                DiagnosticFactory d = s.GetProperty<DiagnosticFactory>("Diagnostic");

                d.Info(message);

                Assert.Equal(message, actualMessage);
            }
        }

        [Trait("Category", "DistributedNetwork")]
        [Theory(DisplayName = "DistributedNetwork returns info from DistributedConnectionManager"), AutoData]
        public void DistributedNetwork_Returns_Info_From_DistributedConnectionManager(
            int branchLevel,
            string branchRoot,
            bool isBranchRoot,
            int childLimit,
            bool canAcceptChildren,
            string parentName,
            IPEndPoint parentIP,
            bool hasParent,
            double? averageBroadcastLatency,
            List<(string Username, IPEndPoint IPEndPoint)> children)
        {
            var dcm = new Mock<IDistributedConnectionManager>();

            dcm.Setup(m => m.BranchLevel).Returns(branchLevel);
            dcm.Setup(m => m.BranchRoot).Returns(branchRoot);
            dcm.Setup(m => m.IsBranchRoot).Returns(isBranchRoot);
            dcm.Setup(m => m.ChildLimit).Returns(childLimit);
            dcm.Setup(m => m.CanAcceptChildren).Returns(canAcceptChildren);
            dcm.Setup(m => m.Parent).Returns((parentName, parentIP));
            dcm.Setup(m => m.HasParent).Returns(hasParent);
            dcm.Setup(m => m.Children).Returns(children.AsReadOnly());
            dcm.Setup(m => m.AverageBroadcastLatency).Returns(averageBroadcastLatency);

            using (var s = new SoulseekClient(distributedConnectionManager: dcm.Object))
            {
                var info = s.DistributedNetwork;

                Assert.Equal(branchLevel, info.BranchLevel);
                Assert.Equal(branchRoot, info.BranchRoot);
                Assert.Equal(isBranchRoot, info.IsBranchRoot);
                Assert.Equal(childLimit, info.ChildLimit);
                Assert.Equal(canAcceptChildren, info.CanAcceptChildren);
                Assert.Equal(parentName, info.Parent.Username);
                Assert.Equal(parentIP, info.Parent.IPEndPoint);
                Assert.Equal(hasParent, info.HasParent);
                Assert.Equal(averageBroadcastLatency, info.AverageBroadcastLatency);

                foreach (var child in children)
                {
                    Assert.Contains(child, info.Children);
                }
            }
        }

        [Trait("Category", "DistributedNetwork")]
        [Fact(DisplayName = "DistributedNetwork returns info from DistributedConnectionManager")]
        public void DistributedNetwork_Does_Not_Throw_If_Some_Info_Is_Null()
        {
            var dcm = new Mock<IDistributedConnectionManager>();

            dcm.Setup(m => m.BranchLevel).Returns(null);
            dcm.Setup(m => m.IsBranchRoot).Returns(null);
            dcm.Setup(m => m.ChildLimit).Returns(null);
            dcm.Setup(m => m.CanAcceptChildren).Returns(null);
            dcm.Setup(m => m.Parent).Returns(null);
            dcm.Setup(m => m.HasParent).Returns(null);
            dcm.Setup(m => m.Children).Returns<IReadOnlyCollection<(string Username, IPEndPoint IPEndPoint)>>(null);

            using (var s = new SoulseekClient(distributedConnectionManager: dcm.Object))
            {
                var ex = Record.Exception(() => s.DistributedNetwork);

                Assert.Null(ex);
            }
        }
    }
}
