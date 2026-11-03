// <copyright file="UnreadMessageDataTests.cs" company="JP Dillingham">
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

// The tests in this file cover the unread message data reporting added to the message parsers.
// They live together in one file, rather than alongside the tests for each message, so that the
// whole lot can be deleted in one step if the reporting is removed.
#pragma warning disable SA1402 // File may only contain a single type

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using System.Collections.Generic;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    /// <summary>
    ///     Tests for the unread message data reported by the message parsers when
    ///     <see cref="SoulseekClient.ReportUnreadMessageData"/> is enabled.
    /// </summary>
    [Collection(UnreadMessageDataTests.CollectionName)]
    public class UnreadMessageDataTests
    {
        /// <summary>
        ///     The name of the xunit collection to which these tests belong.
        /// </summary>
        public const string CollectionName = "UnreadMessageData";

        // DistributedBranchLevel
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedBranchLevel FromByteArray reports unread data when reporting is enabled")]
        public void DistributedBranchLevel_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(1) // level
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedBranchLevel.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedBranchLevel FromByteArray does not report unread data when there is none")]
        public void DistributedBranchLevel_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(1) // level
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedBranchLevel.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedBranchLevel FromByteArray does not report unread data when reporting is disabled")]
        public void DistributedBranchLevel_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(1) // level
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => DistributedBranchLevel.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // DistributedBranchRoot
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedBranchRoot FromByteArray reports unread data when reporting is enabled")]
        public void DistributedBranchRoot_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchRoot)
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedBranchRoot.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedBranchRoot FromByteArray does not report unread data when there is none")]
        public void DistributedBranchRoot_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchRoot)
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedBranchRoot.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedBranchRoot FromByteArray does not report unread data when reporting is disabled")]
        public void DistributedBranchRoot_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchRoot)
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => DistributedBranchRoot.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // DistributedChildDepth
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedChildDepth FromByteArray reports unread data when reporting is enabled")]
        public void DistributedChildDepth_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(1) // depth
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedChildDepth.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedChildDepth FromByteArray does not report unread data when there is none")]
        public void DistributedChildDepth_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(1) // depth
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedChildDepth.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedChildDepth FromByteArray does not report unread data when reporting is disabled")]
        public void DistributedChildDepth_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.ChildDepth)
                .WriteInteger(1) // depth
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => DistributedChildDepth.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // DistributedPingRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedPingRequest FromByteArray reports unread data when reporting is enabled")]
        public void DistributedPingRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedPingRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedPingRequest FromByteArray does not report unread data when there is none")]
        public void DistributedPingRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedPingRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedPingRequest FromByteArray does not report unread data when reporting is disabled")]
        public void DistributedPingRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => DistributedPingRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // DistributedPingResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedPingResponse FromByteArray reports unread data when reporting is enabled")]
        public void DistributedPingResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .WriteInteger(1) // token
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedPingResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedPingResponse FromByteArray does not report unread data when there is none")]
        public void DistributedPingResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .WriteInteger(1) // token
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedPingResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedPingResponse FromByteArray does not report unread data when reporting is disabled")]
        public void DistributedPingResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.Ping)
                .WriteInteger(1) // token
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => DistributedPingResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // DistributedSearchRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedSearchRequest FromByteArray reports unread data when reporting is enabled")]
        public void DistributedSearchRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.SearchRequest)
                .WriteInteger(0) // unknown
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteString("query")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedSearchRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedSearchRequest FromByteArray does not report unread data when there is none")]
        public void DistributedSearchRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.SearchRequest)
                .WriteInteger(0) // unknown
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteString("query")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => DistributedSearchRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "DistributedSearchRequest FromByteArray does not report unread data when reporting is disabled")]
        public void DistributedSearchRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Distributed.SearchRequest)
                .WriteInteger(0) // unknown
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteString("query")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => DistributedSearchRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PeerInit
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PeerInit TryFromByteArray reports unread data when reporting is enabled")]
        public void PeerInit_TryFromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Initialization.PeerInit)
                .WriteString("username")
                .WriteString("P")
                .WriteInteger(1) // token
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PeerInit.TryFromByteArray(msg, out _));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PeerInit TryFromByteArray does not report unread data when there is none")]
        public void PeerInit_TryFromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Initialization.PeerInit)
                .WriteString("username")
                .WriteString("P")
                .WriteInteger(1) // token
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PeerInit.TryFromByteArray(msg, out _));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PeerInit TryFromByteArray does not report unread data when reporting is disabled")]
        public void PeerInit_TryFromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Initialization.PeerInit)
                .WriteString("username")
                .WriteString("P")
                .WriteInteger(1) // token
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PeerInit.TryFromByteArray(msg, out _));

            Assert.Empty(warnings);
        }

        // PierceFirewall
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PierceFirewall TryFromByteArray reports unread data when reporting is enabled")]
        public void PierceFirewall_TryFromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Initialization.PierceFirewall)
                .WriteInteger(1) // token
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PierceFirewall.TryFromByteArray(msg, out _));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PierceFirewall TryFromByteArray does not report unread data when there is none")]
        public void PierceFirewall_TryFromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Initialization.PierceFirewall)
                .WriteInteger(1) // token
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PierceFirewall.TryFromByteArray(msg, out _));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PierceFirewall TryFromByteArray does not report unread data when reporting is disabled")]
        public void PierceFirewall_TryFromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Initialization.PierceFirewall)
                .WriteInteger(1) // token
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PierceFirewall.TryFromByteArray(msg, out _));

            Assert.Empty(warnings);
        }

        // BrowseResponseFactory
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "BrowseResponseFactory Parse reports unread data when reporting is enabled")]
        public void BrowseResponseFactory_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(0) // directory count
                .WriteInteger(0) // unknown
                .WriteInteger(0) // locked directory count
                .WriteInteger(0) // extra, unread data
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => BrowseResponseFactory.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "BrowseResponseFactory Parse does not report unread data when there is none")]
        public void BrowseResponseFactory_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(0) // directory count
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => BrowseResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "BrowseResponseFactory Parse does not report unread data when reporting is disabled")]
        public void BrowseResponseFactory_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(0) // directory count
                .WriteInteger(0) // unknown
                .WriteInteger(0) // locked directory count
                .WriteInteger(0) // extra, unread data
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => BrowseResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // FolderContentsRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "FolderContentsRequest FromByteArray reports unread data when reporting is enabled")]
        public void FolderContentsRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsRequest)
                .WriteInteger(1) // token
                .WriteString("directory")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => FolderContentsRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "FolderContentsRequest FromByteArray does not report unread data when there is none")]
        public void FolderContentsRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsRequest)
                .WriteInteger(1) // token
                .WriteString("directory")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => FolderContentsRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "FolderContentsRequest FromByteArray does not report unread data when reporting is disabled")]
        public void FolderContentsRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsRequest)
                .WriteInteger(1) // token
                .WriteString("directory")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => FolderContentsRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // FolderContentsResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "FolderContentsResponse Parse reports unread data when reporting is enabled")]
        public void FolderContentsResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(1) // token
                .WriteString("directory")
                .WriteInteger(1) // directory count
                .WriteString("directory")
                .WriteInteger(0) // file count
                .WriteInteger(0) // extra, unread data
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => FolderContentsResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "FolderContentsResponse Parse does not report unread data when there is none")]
        public void FolderContentsResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(1) // token
                .WriteString("directory")
                .WriteInteger(1) // directory count
                .WriteString("directory")
                .WriteInteger(0) // file count
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => FolderContentsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "FolderContentsResponse Parse does not report unread data when reporting is disabled")]
        public void FolderContentsResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(1) // token
                .WriteString("directory")
                .WriteInteger(1) // directory count
                .WriteString("directory")
                .WriteInteger(0) // file count
                .WriteInteger(0) // extra, unread data
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => FolderContentsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PeerSearchRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PeerSearchRequest FromByteArray reports unread data when reporting is enabled")]
        public void PeerSearchRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(1) // token
                .WriteString("query")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PeerSearchRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PeerSearchRequest FromByteArray does not report unread data when there is none")]
        public void PeerSearchRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(1) // token
                .WriteString("query")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PeerSearchRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PeerSearchRequest FromByteArray does not report unread data when reporting is disabled")]
        public void PeerSearchRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchRequest)
                .WriteInteger(1) // token
                .WriteString("query")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PeerSearchRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PlaceInQueueRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PlaceInQueueRequest FromByteArray reports unread data when reporting is enabled")]
        public void PlaceInQueueRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueRequest)
                .WriteString("filename")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PlaceInQueueRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PlaceInQueueRequest FromByteArray does not report unread data when there is none")]
        public void PlaceInQueueRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueRequest)
                .WriteString("filename")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PlaceInQueueRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PlaceInQueueRequest FromByteArray does not report unread data when reporting is disabled")]
        public void PlaceInQueueRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueRequest)
                .WriteString("filename")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PlaceInQueueRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PlaceInQueueResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PlaceInQueueResponse FromByteArray reports unread data when reporting is enabled")]
        public void PlaceInQueueResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString("filename")
                .WriteInteger(1) // place in queue
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PlaceInQueueResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PlaceInQueueResponse FromByteArray does not report unread data when there is none")]
        public void PlaceInQueueResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString("filename")
                .WriteInteger(1) // place in queue
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PlaceInQueueResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PlaceInQueueResponse FromByteArray does not report unread data when reporting is disabled")]
        public void PlaceInQueueResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString("filename")
                .WriteInteger(1) // place in queue
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PlaceInQueueResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // QueueDownloadRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "QueueDownloadRequest FromByteArray reports unread data when reporting is enabled")]
        public void QueueDownloadRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.QueueDownload)
                .WriteString("filename")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => QueueDownloadRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "QueueDownloadRequest FromByteArray does not report unread data when there is none")]
        public void QueueDownloadRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.QueueDownload)
                .WriteString("filename")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => QueueDownloadRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "QueueDownloadRequest FromByteArray does not report unread data when reporting is disabled")]
        public void QueueDownloadRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.QueueDownload)
                .WriteString("filename")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => QueueDownloadRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // SearchResponseFactory
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "SearchResponseFactory Parse reports unread data when reporting is enabled")]
        public void SearchResponseFactory_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteInteger(0) // file count
                .WriteByte(0x1) // has free upload slot
                .WriteInteger(1) // upload speed
                .WriteInteger(0) // queue length
                .WriteInteger(0) // unknown
                .WriteInteger(0) // locked file count
                .WriteInteger(0) // extra, unread data
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => SearchResponseFactory.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "SearchResponseFactory Parse does not report unread data when there is none")]
        public void SearchResponseFactory_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteInteger(0) // file count
                .WriteByte(0x1) // has free upload slot
                .WriteInteger(1) // upload speed
                .WriteInteger(0) // queue length
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => SearchResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "SearchResponseFactory Parse does not report unread data when reporting is disabled")]
        public void SearchResponseFactory_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteInteger(0) // file count
                .WriteByte(0x1) // has free upload slot
                .WriteInteger(1) // upload speed
                .WriteInteger(0) // queue length
                .WriteInteger(0) // unknown
                .WriteInteger(0) // locked file count
                .WriteInteger(0) // extra, unread data
                .Compress()
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => SearchResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // TransferRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "TransferRequest FromByteArray reports unread data when reporting is enabled")]
        public void TransferRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferRequest)
                .WriteInteger(0) // direction
                .WriteInteger(1) // token
                .WriteString("filename")
                .WriteLong(1) // file size
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => TransferRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "TransferRequest FromByteArray does not report unread data when there is none")]
        public void TransferRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferRequest)
                .WriteInteger(0) // direction
                .WriteInteger(1) // token
                .WriteString("filename")
                .WriteLong(1) // file size
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => TransferRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "TransferRequest FromByteArray does not report unread data when reporting is disabled")]
        public void TransferRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferRequest)
                .WriteInteger(0) // direction
                .WriteInteger(1) // token
                .WriteString("filename")
                .WriteLong(1) // file size
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => TransferRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // TransferResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "TransferResponse Parse reports unread data when reporting is enabled")]
        public void TransferResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .WriteInteger(1) // token
                .WriteByte(0x1) // allowed
                .WriteLong(1) // file size
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => TransferResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "TransferResponse Parse does not report unread data when there is none")]
        public void TransferResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .WriteInteger(1) // token
                .WriteByte(0x1) // allowed
                .WriteLong(1) // file size
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => TransferResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "TransferResponse Parse does not report unread data when reporting is disabled")]
        public void TransferResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .WriteInteger(1) // token
                .WriteByte(0x1) // allowed
                .WriteLong(1) // file size
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => TransferResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "TransferResponse Parse reports unread data when not allowed and reporting is enabled")]
        public void TransferResponse_Parse_Reports_Unread_Data_When_Not_Allowed_And_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .WriteInteger(1) // token
                .WriteByte(0x0) // allowed
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => TransferResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        // UploadDenied
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UploadDenied FromByteArray reports unread data when reporting is enabled")]
        public void UploadDenied_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadDenied)
                .WriteString("filename")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UploadDenied.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UploadDenied FromByteArray does not report unread data when there is none")]
        public void UploadDenied_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadDenied)
                .WriteString("filename")
                .WriteString("message")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UploadDenied.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UploadDenied FromByteArray does not report unread data when reporting is disabled")]
        public void UploadDenied_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadDenied)
                .WriteString("filename")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UploadDenied.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // UploadFailed
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UploadFailed FromByteArray reports unread data when reporting is enabled")]
        public void UploadFailed_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString("filename")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UploadFailed.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UploadFailed FromByteArray does not report unread data when there is none")]
        public void UploadFailed_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString("filename")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UploadFailed.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UploadFailed FromByteArray does not report unread data when reporting is disabled")]
        public void UploadFailed_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString("filename")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UploadFailed.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // UserInfoResponseFactory
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserInfoResponseFactory Parse reports unread data when reporting is enabled")]
        public void UserInfoResponseFactory_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString("description")
                .WriteByte(0) // has picture
                .WriteInteger(1) // upload slots
                .WriteInteger(0) // queue length
                .WriteByte(1) // has free upload slot
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserInfoResponseFactory.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserInfoResponseFactory Parse does not report unread data when there is none")]
        public void UserInfoResponseFactory_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString("description")
                .WriteByte(0) // has picture
                .WriteInteger(1) // upload slots
                .WriteInteger(0) // queue length
                .WriteByte(1) // has free upload slot
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserInfoResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserInfoResponseFactory Parse does not report unread data when reporting is disabled")]
        public void UserInfoResponseFactory_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString("description")
                .WriteByte(0) // has picture
                .WriteInteger(1) // upload slots
                .WriteInteger(0) // queue length
                .WriteByte(1) // has free upload slot
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserInfoResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // CannotConnect
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "CannotConnect FromByteArray reports unread data when reporting is enabled")]
        public void CannotConnect_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(1) // token
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => CannotConnect.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "CannotConnect FromByteArray does not report unread data when there is none")]
        public void CannotConnect_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(1) // token
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => CannotConnect.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "CannotConnect FromByteArray does not report unread data when reporting is disabled")]
        public void CannotConnect_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotConnect)
                .WriteInteger(1) // token
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => CannotConnect.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // CannotJoinRoomNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "CannotJoinRoomNotification FromByteArray reports unread data when reporting is enabled")]
        public void CannotJoinRoomNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotJoinRoom)
                .WriteString("room")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => CannotJoinRoomNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "CannotJoinRoomNotification FromByteArray does not report unread data when there is none")]
        public void CannotJoinRoomNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotJoinRoom)
                .WriteString("room")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => CannotJoinRoomNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "CannotJoinRoomNotification FromByteArray does not report unread data when reporting is disabled")]
        public void CannotJoinRoomNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.CannotJoinRoom)
                .WriteString("room")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => CannotJoinRoomNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // ConnectToPeerResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ConnectToPeerResponse FromByteArray reports unread data when reporting is enabled")]
        public void ConnectToPeerResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString("username")
                .WriteString("P")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteInteger(1) // port
                .WriteInteger(1) // token
                .WriteByte(0) // is privileged
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ConnectToPeerResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ConnectToPeerResponse FromByteArray does not report unread data when there is none")]
        public void ConnectToPeerResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString("username")
                .WriteString("P")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteInteger(1) // port
                .WriteInteger(1) // token
                .WriteByte(0) // is privileged
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ConnectToPeerResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ConnectToPeerResponse FromByteArray does not report unread data when reporting is disabled")]
        public void ConnectToPeerResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString("username")
                .WriteString("P")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteInteger(1) // port
                .WriteInteger(1) // token
                .WriteByte(0) // is privileged
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => ConnectToPeerResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // ExcludedSearchPhrases
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ExcludedSearchPhrases Parse reports unread data when reporting is enabled")]
        public void ExcludedSearchPhrases_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ExcludedSearchPhrases)
                .WriteInteger(0) // phrase count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ExcludedSearchPhrasesNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ExcludedSearchPhrases Parse does not report unread data when there is none")]
        public void ExcludedSearchPhrases_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ExcludedSearchPhrases)
                .WriteInteger(0) // phrase count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ExcludedSearchPhrasesNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ExcludedSearchPhrases Parse does not report unread data when reporting is disabled")]
        public void ExcludedSearchPhrases_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ExcludedSearchPhrases)
                .WriteInteger(0) // phrase count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => ExcludedSearchPhrasesNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // GlobalMessageNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "GlobalMessageNotification FromByteArray reports unread data when reporting is enabled")]
        public void GlobalMessageNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GlobalAdminMessage)
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => GlobalMessageNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "GlobalMessageNotification FromByteArray does not report unread data when there is none")]
        public void GlobalMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GlobalAdminMessage)
                .WriteString("message")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => GlobalMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "GlobalMessageNotification FromByteArray does not report unread data when reporting is disabled")]
        public void GlobalMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GlobalAdminMessage)
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => GlobalMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // IntegerResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "IntegerResponse Parse reports unread data when reporting is enabled")]
        public void IntegerResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteInteger(1) // value
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => IntegerResponse.FromByteArray<MessageCode.Server>(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "IntegerResponse Parse does not report unread data when there is none")]
        public void IntegerResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteInteger(1) // value
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => IntegerResponse.FromByteArray<MessageCode.Server>(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "IntegerResponse Parse does not report unread data when reporting is disabled")]
        public void IntegerResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteInteger(1) // value
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => IntegerResponse.FromByteArray<MessageCode.Server>(msg));

            Assert.Empty(warnings);
        }

        // JoinRoomResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "JoinRoomResponse Parse reports unread data when reporting is enabled")]
        public void JoinRoomResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.JoinRoom)
                .WriteString("room")
                .WriteInteger(0) // user count
                .WriteInteger(0) // status count
                .WriteInteger(0) // data count
                .WriteInteger(0) // slots free count
                .WriteInteger(0) // country count
                .WriteString("owner")
                .WriteInteger(0) // operator count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => JoinRoomResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "JoinRoomResponse Parse does not report unread data when there is none")]
        public void JoinRoomResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.JoinRoom)
                .WriteString("room")
                .WriteInteger(0) // user count
                .WriteInteger(0) // status count
                .WriteInteger(0) // data count
                .WriteInteger(0) // slots free count
                .WriteInteger(0) // country count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => JoinRoomResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "JoinRoomResponse Parse does not report unread data when reporting is disabled")]
        public void JoinRoomResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.JoinRoom)
                .WriteString("room")
                .WriteInteger(0) // user count
                .WriteInteger(0) // status count
                .WriteInteger(0) // data count
                .WriteInteger(0) // slots free count
                .WriteInteger(0) // country count
                .WriteString("owner")
                .WriteInteger(0) // operator count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => JoinRoomResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // LeaveRoomResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "LeaveRoomResponse FromByteArray reports unread data when reporting is enabled")]
        public void LeaveRoomResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.LeaveRoom)
                .WriteString("room")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => LeaveRoomResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "LeaveRoomResponse FromByteArray does not report unread data when there is none")]
        public void LeaveRoomResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.LeaveRoom)
                .WriteString("room")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => LeaveRoomResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "LeaveRoomResponse FromByteArray does not report unread data when reporting is disabled")]
        public void LeaveRoomResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.LeaveRoom)
                .WriteString("room")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => LeaveRoomResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // LoginResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "LoginResponse Parse reports unread data when reporting is enabled")]
        public void LoginResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteByte(1) // succeeded
                .WriteString("message")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteString("hash")
                .WriteByte(0) // is supporter
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => LoginResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "LoginResponse Parse does not report unread data when there is none")]
        public void LoginResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteByte(1) // succeeded
                .WriteString("message")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteString("hash")
                .WriteByte(0) // is supporter
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => LoginResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "LoginResponse Parse does not report unread data when reporting is disabled")]
        public void LoginResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteByte(1) // succeeded
                .WriteString("message")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteString("hash")
                .WriteByte(0) // is supporter
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => LoginResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // NetInfo
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "NetInfo Parse reports unread data when reporting is enabled")]
        public void NetInfo_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(0) // parent count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => NetInfoNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "NetInfo Parse does not report unread data when there is none")]
        public void NetInfo_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(0) // parent count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => NetInfoNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "NetInfo Parse does not report unread data when reporting is disabled")]
        public void NetInfo_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(0) // parent count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => NetInfoNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // NewPassword
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "NewPassword FromByteArray reports unread data when reporting is enabled")]
        public void NewPassword_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NewPassword)
                .WriteString("password")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => NewPassword.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "NewPassword FromByteArray does not report unread data when there is none")]
        public void NewPassword_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NewPassword)
                .WriteString("password")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => NewPassword.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "NewPassword FromByteArray does not report unread data when reporting is disabled")]
        public void NewPassword_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NewPassword)
                .WriteString("password")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => NewPassword.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateMessageNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateMessageNotification FromByteArray reports unread data when reporting is enabled")]
        public void PrivateMessageNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(1) // id
                .WriteInteger(0) // timestamp
                .WriteString("username")
                .WriteString("message")
                .WriteByte(1) // is replayed
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateMessageNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateMessageNotification FromByteArray does not report unread data when there is none")]
        public void PrivateMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(1) // id
                .WriteInteger(0) // timestamp
                .WriteString("username")
                .WriteString("message")
                .WriteByte(1) // is replayed
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateMessageNotification FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(1) // id
                .WriteInteger(0) // timestamp
                .WriteString("username")
                .WriteString("message")
                .WriteByte(1) // is replayed
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateRoomAddOperator
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomAddOperator FromByteArray reports unread data when reporting is enabled")]
        public void PrivateRoomAddOperator_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddOperator)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomAddOperator.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomAddOperator FromByteArray does not report unread data when there is none")]
        public void PrivateRoomAddOperator_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddOperator)
                .WriteString("room")
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomAddOperator.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomAddOperator FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateRoomAddOperator_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddOperator)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateRoomAddOperator.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateRoomAddUser
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomAddUser FromByteArray reports unread data when reporting is enabled")]
        public void PrivateRoomAddUser_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddUser)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomAddUser.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomAddUser FromByteArray does not report unread data when there is none")]
        public void PrivateRoomAddUser_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddUser)
                .WriteString("room")
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomAddUser.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomAddUser FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateRoomAddUser_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomAddUser)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateRoomAddUser.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateRoomOwnedListNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomOwnedListNotification FromByteArray reports unread data when reporting is enabled")]
        public void PrivateRoomOwnedListNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOwned)
                .WriteString("room")
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomOwnedListNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomOwnedListNotification FromByteArray does not report unread data when there is none")]
        public void PrivateRoomOwnedListNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOwned)
                .WriteString("room")
                .WriteInteger(0) // user count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomOwnedListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomOwnedListNotification FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateRoomOwnedListNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomOwned)
                .WriteString("room")
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateRoomOwnedListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateRoomRemoveOperator
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomRemoveOperator FromByteArray reports unread data when reporting is enabled")]
        public void PrivateRoomRemoveOperator_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveOperator)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomRemoveOperator.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomRemoveOperator FromByteArray does not report unread data when there is none")]
        public void PrivateRoomRemoveOperator_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveOperator)
                .WriteString("room")
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomRemoveOperator.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomRemoveOperator FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateRoomRemoveOperator_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveOperator)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateRoomRemoveOperator.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateRoomRemoveUser
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomRemoveUser FromByteArray reports unread data when reporting is enabled")]
        public void PrivateRoomRemoveUser_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveUser)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomRemoveUser.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomRemoveUser FromByteArray does not report unread data when there is none")]
        public void PrivateRoomRemoveUser_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveUser)
                .WriteString("room")
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomRemoveUser.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomRemoveUser FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateRoomRemoveUser_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomRemoveUser)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateRoomRemoveUser.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateRoomToggle
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomToggle FromByteArray reports unread data when reporting is enabled")]
        public void PrivateRoomToggle_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomToggle)
                .WriteByte(1) // accept invitations
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomToggle.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomToggle FromByteArray does not report unread data when there is none")]
        public void PrivateRoomToggle_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomToggle)
                .WriteByte(1) // accept invitations
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomToggle.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomToggle FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateRoomToggle_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomToggle)
                .WriteByte(1) // accept invitations
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateRoomToggle.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivateRoomUserListNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomUserListNotification FromByteArray reports unread data when reporting is enabled")]
        public void PrivateRoomUserListNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomUsers)
                .WriteString("room")
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomUserListNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomUserListNotification FromByteArray does not report unread data when there is none")]
        public void PrivateRoomUserListNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomUsers)
                .WriteString("room")
                .WriteInteger(0) // user count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivateRoomUserListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivateRoomUserListNotification FromByteArray does not report unread data when reporting is disabled")]
        public void PrivateRoomUserListNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateRoomUsers)
                .WriteString("room")
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivateRoomUserListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivilegeNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegeNotification FromByteArray reports unread data when reporting is enabled")]
        public void PrivilegeNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NotifyPrivileges)
                .WriteInteger(1) // id
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivilegeNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegeNotification FromByteArray does not report unread data when there is none")]
        public void PrivilegeNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NotifyPrivileges)
                .WriteInteger(1) // id
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivilegeNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegeNotification FromByteArray does not report unread data when reporting is disabled")]
        public void PrivilegeNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.NotifyPrivileges)
                .WriteInteger(1) // id
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivilegeNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivilegedUserList
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegedUserList Parse reports unread data when reporting is enabled")]
        public void PrivilegedUserList_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivilegedUsers)
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivilegedUserListNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegedUserList Parse does not report unread data when there is none")]
        public void PrivilegedUserList_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivilegedUsers)
                .WriteInteger(0) // user count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivilegedUserListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegedUserList Parse does not report unread data when reporting is disabled")]
        public void PrivilegedUserList_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivilegedUsers)
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivilegedUserListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PrivilegedUserNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegedUserNotification FromByteArray reports unread data when reporting is enabled")]
        public void PrivilegedUserNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddPrivilegedUser)
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivilegedUserNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegedUserNotification FromByteArray does not report unread data when there is none")]
        public void PrivilegedUserNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddPrivilegedUser)
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PrivilegedUserNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PrivilegedUserNotification FromByteArray does not report unread data when reporting is disabled")]
        public void PrivilegedUserNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddPrivilegedUser)
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PrivilegedUserNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PublicChatMessageNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PublicChatMessageNotification FromByteArray reports unread data when reporting is enabled")]
        public void PublicChatMessageNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PublicChat)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PublicChatMessageNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PublicChatMessageNotification FromByteArray does not report unread data when there is none")]
        public void PublicChatMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PublicChat)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PublicChatMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PublicChatMessageNotification FromByteArray does not report unread data when reporting is disabled")]
        public void PublicChatMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PublicChat)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PublicChatMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // RoomJoinedNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomJoinedNotification Parse reports unread data when reporting is enabled")]
        public void RoomJoinedNotification_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // status
                .WriteInteger(0) // average speed
                .WriteLong(0) // download count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteInteger(0) // slots free
                .WriteString("US")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserJoinedRoomNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomJoinedNotification Parse does not report unread data when there is none")]
        public void RoomJoinedNotification_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // status
                .WriteInteger(0) // average speed
                .WriteLong(0) // download count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteInteger(0) // slots free
                .WriteString("US")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserJoinedRoomNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomJoinedNotification Parse does not report unread data when reporting is disabled")]
        public void RoomJoinedNotification_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserJoinedRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // status
                .WriteInteger(0) // average speed
                .WriteLong(0) // download count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteInteger(0) // slots free
                .WriteString("US")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserJoinedRoomNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // RoomLeftNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomLeftNotification Parse reports unread data when reporting is enabled")]
        public void RoomLeftNotification_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserLeftRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserLeftRoomNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomLeftNotification Parse does not report unread data when there is none")]
        public void RoomLeftNotification_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserLeftRoom)
                .WriteString("room")
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserLeftRoomNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomLeftNotification Parse does not report unread data when reporting is disabled")]
        public void RoomLeftNotification_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserLeftRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserLeftRoomNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // RoomList
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomList Parse reports unread data when reporting is enabled")]
        public void RoomList_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList)
                .WriteInteger(0) // public room count
                .WriteInteger(0) // public room user count count
                .WriteInteger(0) // owned room count
                .WriteInteger(0) // owned room user count count
                .WriteInteger(0) // private room count
                .WriteInteger(0) // private room user count count
                .WriteInteger(0) // moderated room count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomListResponseFactory.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomList Parse does not report unread data when there is none")]
        public void RoomList_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList)
                .WriteInteger(0) // public room count
                .WriteInteger(0) // public room user count count
                .WriteInteger(0) // owned room count
                .WriteInteger(0) // owned room user count count
                .WriteInteger(0) // private room count
                .WriteInteger(0) // private room user count count
                .WriteInteger(0) // moderated room count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomListResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomList Parse does not report unread data when reporting is disabled")]
        public void RoomList_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList)
                .WriteInteger(0) // public room count
                .WriteInteger(0) // public room user count count
                .WriteInteger(0) // owned room count
                .WriteInteger(0) // owned room user count count
                .WriteInteger(0) // private room count
                .WriteInteger(0) // private room user count count
                .WriteInteger(0) // moderated room count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => RoomListResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // RoomMessageNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomMessageNotification FromByteArray reports unread data when reporting is enabled")]
        public void RoomMessageNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.SayInChatRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomMessageNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomMessageNotification FromByteArray does not report unread data when there is none")]
        public void RoomMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.SayInChatRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomMessageNotification FromByteArray does not report unread data when reporting is disabled")]
        public void RoomMessageNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.SayInChatRoom)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => RoomMessageNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // RoomTickerAddedNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerAddedNotification FromByteArray reports unread data when reporting is enabled")]
        public void RoomTickerAddedNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerAdd)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomTickerAddedNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerAddedNotification FromByteArray does not report unread data when there is none")]
        public void RoomTickerAddedNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerAdd)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomTickerAddedNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerAddedNotification FromByteArray does not report unread data when reporting is disabled")]
        public void RoomTickerAddedNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerAdd)
                .WriteString("room")
                .WriteString("username")
                .WriteString("message")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => RoomTickerAddedNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // RoomTickerListNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerListNotification FromByteArray reports unread data when reporting is enabled")]
        public void RoomTickerListNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickers)
                .WriteString("room")
                .WriteInteger(0) // ticker count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomTickerListNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerListNotification FromByteArray does not report unread data when there is none")]
        public void RoomTickerListNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickers)
                .WriteString("room")
                .WriteInteger(0) // ticker count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomTickerListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerListNotification FromByteArray does not report unread data when reporting is disabled")]
        public void RoomTickerListNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickers)
                .WriteString("room")
                .WriteInteger(0) // ticker count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => RoomTickerListNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // RoomTickerRemovedNotification
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerRemovedNotification FromByteArray reports unread data when reporting is enabled")]
        public void RoomTickerRemovedNotification_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerRemove)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomTickerRemovedNotification.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerRemovedNotification FromByteArray does not report unread data when there is none")]
        public void RoomTickerRemovedNotification_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerRemove)
                .WriteString("room")
                .WriteString("username")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => RoomTickerRemovedNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "RoomTickerRemovedNotification FromByteArray does not report unread data when reporting is disabled")]
        public void RoomTickerRemovedNotification_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomTickerRemove)
                .WriteString("room")
                .WriteString("username")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => RoomTickerRemovedNotification.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // ServerPing
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ServerPing FromByteArray reports unread data when reporting is enabled")]
        public void ServerPing_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Ping)
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ServerPing.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ServerPing FromByteArray does not report unread data when there is none")]
        public void ServerPing_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Ping)
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ServerPing.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ServerPing FromByteArray does not report unread data when reporting is disabled")]
        public void ServerPing_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Ping)
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => ServerPing.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // ServerSearchRequest
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ServerSearchRequest FromByteArray reports unread data when reporting is enabled")]
        public void ServerSearchRequest_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.FileSearch)
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteString("query")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ServerSearchRequest.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ServerSearchRequest FromByteArray does not report unread data when there is none")]
        public void ServerSearchRequest_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.FileSearch)
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteString("query")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => ServerSearchRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "ServerSearchRequest FromByteArray does not report unread data when reporting is disabled")]
        public void ServerSearchRequest_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.FileSearch)
                .WriteString("username")
                .WriteInteger(1) // token
                .WriteString("query")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => ServerSearchRequest.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // StringResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "StringResponse Parse reports unread data when reporting is enabled")]
        public void StringResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString("value")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => StringResponse.FromByteArray<MessageCode.Server>(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "StringResponse Parse does not report unread data when there is none")]
        public void StringResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString("value")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => StringResponse.FromByteArray<MessageCode.Server>(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "StringResponse Parse does not report unread data when reporting is disabled")]
        public void StringResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString("value")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => StringResponse.FromByteArray<MessageCode.Server>(msg));

            Assert.Empty(warnings);
        }

        // UserAddressResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserAddressResponse Parse reports unread data when reporting is enabled")]
        public void UserAddressResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString("username")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteInteger(1) // port
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserAddressResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserAddressResponse Parse does not report unread data when there is none")]
        public void UserAddressResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString("username")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteInteger(1) // port
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserAddressResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserAddressResponse Parse does not report unread data when reporting is disabled")]
        public void UserAddressResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString("username")
                .WriteBytes(new byte[] { 0x0, 0x0, 0x0, 0x0 }) // ip address
                .WriteInteger(1) // port
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserAddressResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // UserPrivilegeResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserPrivilegeResponse Parse reports unread data when reporting is enabled")]
        public void UserPrivilegeResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserPrivileges)
                .WriteString("username")
                .WriteByte(1) // privileged
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserPrivilegeResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserPrivilegeResponse Parse does not report unread data when there is none")]
        public void UserPrivilegeResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserPrivileges)
                .WriteString("username")
                .WriteByte(1) // privileged
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserPrivilegeResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserPrivilegeResponse Parse does not report unread data when reporting is disabled")]
        public void UserPrivilegeResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.UserPrivileges)
                .WriteString("username")
                .WriteByte(1) // privileged
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserPrivilegeResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // UserStatisticsResponseFactory
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserStatisticsResponseFactory Parse reports unread data when reporting is enabled")]
        public void UserStatisticsResponseFactory_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString("username")
                .WriteInteger(0) // average speed
                .WriteLong(0) // upload count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserStatisticsResponseFactory.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserStatisticsResponseFactory Parse does not report unread data when there is none")]
        public void UserStatisticsResponseFactory_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString("username")
                .WriteInteger(0) // average speed
                .WriteLong(0) // upload count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserStatisticsResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserStatisticsResponseFactory Parse does not report unread data when reporting is disabled")]
        public void UserStatisticsResponseFactory_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserStats)
                .WriteString("username")
                .WriteInteger(0) // average speed
                .WriteLong(0) // upload count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserStatisticsResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // UserStatusResponseFactory
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserStatusResponseFactory Parse reports unread data when reporting is enabled")]
        public void UserStatusResponseFactory_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString("username")
                .WriteInteger(0) // presence
                .WriteByte(0) // privileged
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserStatusResponseFactory.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserStatusResponseFactory Parse does not report unread data when there is none")]
        public void UserStatusResponseFactory_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString("username")
                .WriteInteger(0) // presence
                .WriteByte(0) // privileged
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserStatusResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserStatusResponseFactory Parse does not report unread data when reporting is disabled")]
        public void UserStatusResponseFactory_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString("username")
                .WriteInteger(0) // presence
                .WriteByte(0) // privileged
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserStatusResponseFactory.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // WatchUserResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "WatchUserResponse Parse reports unread data when reporting is enabled")]
        public void WatchUserResponse_Parse_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString("username")
                .WriteByte(1) // exists
                .WriteInteger(0) // status
                .WriteInteger(0) // average speed
                .WriteLong(0) // download count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteString("US")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => WatchUserResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "WatchUserResponse Parse does not report unread data when there is none")]
        public void WatchUserResponse_Parse_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString("username")
                .WriteByte(1) // exists
                .WriteInteger(0) // status
                .WriteInteger(0) // average speed
                .WriteLong(0) // download count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteString("US")
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => WatchUserResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "WatchUserResponse Parse does not report unread data when reporting is disabled")]
        public void WatchUserResponse_Parse_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.WatchUser)
                .WriteString("username")
                .WriteByte(1) // exists
                .WriteInteger(0) // status
                .WriteInteger(0) // average speed
                .WriteLong(0) // download count
                .WriteInteger(0) // file count
                .WriteInteger(0) // directory count
                .WriteString("US")
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => WatchUserResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // GlobalRecommendationsResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "GlobalRecommendationsResponse FromByteArray reports unread data when reporting is enabled")]
        public void GlobalRecommendationsResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetGlobalRecommendations)
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // unrecommendation count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => GlobalRecommendationsResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "GlobalRecommendationsResponse FromByteArray does not report unread data when there is none")]
        public void GlobalRecommendationsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetGlobalRecommendations)
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // unrecommendation count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => GlobalRecommendationsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "GlobalRecommendationsResponse FromByteArray does not report unread data when reporting is disabled")]
        public void GlobalRecommendationsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetGlobalRecommendations)
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // unrecommendation count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => GlobalRecommendationsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PersonalRecommendationsResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PersonalRecommendationsResponse FromByteArray reports unread data when reporting is enabled")]
        public void PersonalRecommendationsResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalRecommendations)
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // unrecommendation count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PersonalRecommendationsResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PersonalRecommendationsResponse FromByteArray does not report unread data when there is none")]
        public void PersonalRecommendationsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalRecommendations)
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // unrecommendation count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PersonalRecommendationsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PersonalRecommendationsResponse FromByteArray does not report unread data when reporting is disabled")]
        public void PersonalRecommendationsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalRecommendations)
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // unrecommendation count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PersonalRecommendationsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // InterestRecommendationsResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "InterestRecommendationsResponse FromByteArray reports unread data when reporting is enabled")]
        public void InterestRecommendationsResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestRecommendations)
                .WriteString("interest")
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => InterestRecommendationsResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "InterestRecommendationsResponse FromByteArray does not report unread data when there is none")]
        public void InterestRecommendationsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestRecommendations)
                .WriteString("interest")
                .WriteInteger(0) // recommendation count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => InterestRecommendationsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "InterestRecommendationsResponse FromByteArray does not report unread data when reporting is disabled")]
        public void InterestRecommendationsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestRecommendations)
                .WriteString("interest")
                .WriteInteger(0) // recommendation count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => InterestRecommendationsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // PersonalSimilarUsersResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PersonalSimilarUsersResponse FromByteArray reports unread data when reporting is enabled")]
        public void PersonalSimilarUsersResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalSimilarUsers)
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PersonalSimilarUsersResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PersonalSimilarUsersResponse FromByteArray does not report unread data when there is none")]
        public void PersonalSimilarUsersResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalSimilarUsers)
                .WriteInteger(0) // user count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => PersonalSimilarUsersResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "PersonalSimilarUsersResponse FromByteArray does not report unread data when reporting is disabled")]
        public void PersonalSimilarUsersResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPersonalSimilarUsers)
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => PersonalSimilarUsersResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // InterestSimilarUsersResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "InterestSimilarUsersResponse FromByteArray reports unread data when reporting is enabled")]
        public void InterestSimilarUsersResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestSimilarUsers)
                .WriteString("interest")
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => InterestSimilarUsersResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "InterestSimilarUsersResponse FromByteArray does not report unread data when there is none")]
        public void InterestSimilarUsersResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestSimilarUsers)
                .WriteString("interest")
                .WriteInteger(0) // user count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => InterestSimilarUsersResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "InterestSimilarUsersResponse FromByteArray does not report unread data when reporting is disabled")]
        public void InterestSimilarUsersResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetInterestSimilarUsers)
                .WriteString("interest")
                .WriteInteger(0) // user count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => InterestSimilarUsersResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        // UserInterestsResponse
        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserInterestsResponse FromByteArray reports unread data when reporting is enabled")]
        public void UserInterestsResponse_FromByteArray_Reports_Unread_Data_When_Reporting_Is_Enabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserInterests)
                .WriteString("username")
                .WriteInteger(0) // like count
                .WriteInteger(0) // hate count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserInterestsResponse.FromByteArray(msg));

            Assert.Single(warnings);
            Assert.Contains("unread bytes", warnings[0]);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserInterestsResponse FromByteArray does not report unread data when there is none")]
        public void UserInterestsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_There_Is_None()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserInterests)
                .WriteString("username")
                .WriteInteger(0) // like count
                .WriteInteger(0) // hate count
                .Build();

            var warnings = Capture(reportUnreadMessageData: true, () => UserInterestsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        [Trait("Category", "ReportUnreadMessageData")]
        [Fact(DisplayName = "UserInterestsResponse FromByteArray does not report unread data when reporting is disabled")]
        public void UserInterestsResponse_FromByteArray_Does_Not_Report_Unread_Data_When_Reporting_Is_Disabled()
        {
            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetUserInterests)
                .WriteString("username")
                .WriteInteger(0) // like count
                .WriteInteger(0) // hate count
                .WriteInteger(0) // extra, unread data
                .Build();

            var warnings = Capture(reportUnreadMessageData: false, () => UserInterestsResponse.FromByteArray(msg));

            Assert.Empty(warnings);
        }

        /// <summary>
        ///     Invokes the given <paramref name="parse"/> action with <see cref="SoulseekClient.ReportUnreadMessageData"/>
        ///     set to the given <paramref name="reportUnreadMessageData"/> value, and returns the diagnostic warnings
        ///     raised while doing so.
        /// </summary>
        /// <param name="reportUnreadMessageData">A value indicating whether unread message data should be reported.</param>
        /// <param name="parse">The action which parses the message under test.</param>
        /// <returns>The messages of the diagnostic warnings raised by the given action.</returns>
        private static List<string> Capture(bool reportUnreadMessageData, Action parse)
        {
            var warnings = new List<string>();

            GlobalDiagnostic.Init(new DiagnosticFactory(minimumLevel: DiagnosticLevel.Warning, eventHandler: (e) => warnings.Add(e.Message)));
            SoulseekClient.ReportUnreadMessageData = reportUnreadMessageData;

            try
            {
                parse();
            }
            finally
            {
                SoulseekClient.ReportUnreadMessageData = false;
                GlobalDiagnostic.Init(null);
            }

            return warnings;
        }
    }

    /// <summary>
    ///     Serializes the tests which toggle <see cref="SoulseekClient.ReportUnreadMessageData"/> and swap the
    ///     static diagnostic factory; both are global, so these tests can't run in parallel with anything else.
    /// </summary>
    [CollectionDefinition(UnreadMessageDataTests.CollectionName, DisableParallelization = true)]
    public class UnreadMessageDataTestsCollection
    {
    }
}
