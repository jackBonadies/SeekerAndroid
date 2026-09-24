using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnitTestCommon
{
    [TestFixture]
    public class DownloadFailureClassifierTests
    {
        private static SoulseekClientException Wrapped(Exception inner)
        {
            return new SoulseekClientException($"Failed to download file @@root\\folder\\song.mp3 from user test: {inner.Message}", inner);
        }

        private static AggregateException Faulted(Exception e)
        {
            return Task.FromException(e).Exception!;
        }

        private static void AssertKind(DownloadFailureKind expected, Exception e)
        {
            Assert.AreEqual(expected, DownloadFailureClassifier.Classify(Faulted(e)), "faulted task");
            Assert.AreEqual(expected, DownloadFailureClassifier.Classify(e), "raw");
        }

        [Test]
        public void Null_IsUnknown()
        {
            Assert.AreEqual(DownloadFailureKind.Unknown, DownloadFailureClassifier.Classify(null));
        }

        [Test]
        public void Duplicate()
        {
            AssertKind(DownloadFailureKind.Duplicate, new DuplicateTransferException("Duplicate download of x from y aborted"));
            AssertKind(DownloadFailureKind.Duplicate, new DuplicateTransferException("An active or queued download of x from y is already in progress"));
        }

        [Test]
        public void NotLoggedIn()
        {
            AssertKind(DownloadFailureKind.Unknown, new InvalidOperationException("Collection was modified; enumeration operation may not execute."));
            AssertKind(DownloadFailureKind.NotLoggedIn, new InvalidOperationException("The server connection must be connected and logged in to download files (currently: Disconnected)"));
        }

        [Test]
        public void DirectoryNotSet()
        {
            AssertKind(DownloadFailureKind.DirectoryNotSet, new DownloadDirectoryNotSetException());
        }

        [Test]
        public void SizeMismatch()
        {
            AssertKind(DownloadFailureKind.SizeMismatch, new TransferSizeMismatchException("Transfer aborted: the remote size of 10 does not match expected size 20", 20, 10));
        }

        [Test]
        public void Rejected()
        {
            AssertKind(DownloadFailureKind.RejectedNotShared, new TransferRejectedException("Transfer rejected: File not shared."));
            AssertKind(DownloadFailureKind.Rejected, new TransferRejectedException("Transfer rejected: Cancelled"));
            AssertKind(DownloadFailureKind.Rejected, new TransferRejectedException("Too many files"));
        }

        [Test]
        public void UserOffline()
        {
            AssertKind(DownloadFailureKind.UserOffline, new UserOfflineException("User test appears to be offline"));
        }

        [Test]
        public void TimedOut()
        {
            AssertKind(DownloadFailureKind.TimedOut, new TimeoutException("The wait timed out after 15000 milliseconds"));
        }

        [Test]
        public void ReportedFailed_IsWrapped()
        {
            AssertKind(DownloadFailureKind.ReportedFailed, Wrapped(new TransferReportedFailedException("Download reported as failed by remote client")));
        }

        [Test]
        public void CannotConnect()
        {
            var connect = new ConnectionException("Failed to establish a direct or indirect message connection to username (198.1.1.2:8080)");
            AssertKind(DownloadFailureKind.CannotConnect, Wrapped(connect));
            AssertKind(DownloadFailureKind.CannotConnect, connect);
        }

        [Test]
        public void CannotConnect_WithInnerTimeout_IsNotTimedOut()
        {
            var connect = new ConnectionException("Failed to establish a direct or indirect message connection to username (198.1.1.2:8080)",
                new TimeoutException("Operation timed out after 10000 milliseconds"));
            AssertKind(DownloadFailureKind.CannotConnect, Wrapped(connect));
        }

        [Test]
        public void ConnectionClosed()
        {
            AssertKind(DownloadFailureKind.ConnectionClosed, Wrapped(new ConnectionException("Transfer failed: Read error: Remote connection closed")));
        }

        [Test]
        public void DiskFull()
        {
            AssertKind(DownloadFailureKind.DiskFull, Wrapped(new ConnectionException("Transfer failed: Read error: write failed", new IOException("write failed: ENOSPC (No space left on device)"))));
            AssertKind(DownloadFailureKind.DiskFull, Wrapped(new ConnectionException("Transfer failed: Read error: Disk full.", new IOException("Disk full."))));
        }

        [Test]
        public void NetworkDown()
        {
            AssertKind(DownloadFailureKind.NetworkDown, Wrapped(new ConnectionException("Transfer failed: Read error: Network subsystem is down")));
        }

        [Test]
        public void Unknown()
        {
            AssertKind(DownloadFailureKind.Unknown, Wrapped(new TransferStreamException("Requested non-zero start offset but output stream does not support seeking")));
            AssertKind(DownloadFailureKind.Unknown, new DuplicateTokenException("The specified or generated token 5 is already in progress"));
            AssertKind(DownloadFailureKind.Unknown, Wrapped(new NullReferenceException()));
        }

        [Test]
        public void SubclassOfWrapper_IsNotUnwrapped()
        {
            AssertKind(DownloadFailureKind.Unknown, new ConnectionException("Transfer failed: something", new TimeoutException("x")));
        }
    }
}
