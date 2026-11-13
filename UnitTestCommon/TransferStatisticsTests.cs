using Common.Transfers;
using NUnit.Framework;
using Seeker;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferStatisticsTests
    {
        [Test]
        public void Succeeded_CountsFileAndBytes()
        {
            var stats = new TransferStatistics();
            stats.RecordFinished(isUpload: false, succeeded: true, bytesTransferred: 1000, startOffset: 0);
            stats.RecordFinished(isUpload: true, succeeded: true, bytesTransferred: 300, startOffset: 0);

            var s = stats.GetSnapshot();
            Assert.That(s.CompletedDownloads, Is.EqualTo(1));
            Assert.That(s.DownloadedBytes, Is.EqualTo(1000));
            Assert.That(s.CompletedUploads, Is.EqualTo(1));
            Assert.That(s.UploadedBytes, Is.EqualTo(300));
        }

        [Test]
        public void Failed_CountsBytesButNotFile()
        {
            var stats = new TransferStatistics();
            stats.RecordFinished(isUpload: false, succeeded: false, bytesTransferred: 400, startOffset: 0);

            var s = stats.GetSnapshot();
            Assert.That(s.CompletedDownloads, Is.EqualTo(0));
            Assert.That(s.DownloadedBytes, Is.EqualTo(400));
        }

        [Test]
        public void Resume_CountsOnlyBytesAfterStartOffset()
        {
            var stats = new TransferStatistics();
            stats.RecordFinished(isUpload: false, succeeded: false, bytesTransferred: 400, startOffset: 0);
            stats.RecordFinished(isUpload: false, succeeded: true, bytesTransferred: 1000, startOffset: 400);

            var s = stats.GetSnapshot();
            Assert.That(s.CompletedDownloads, Is.EqualTo(1));
            Assert.That(s.DownloadedBytes, Is.EqualTo(1000));
        }

        [Test]
        public void BytesBelowStartOffset_AddsNothing()
        {
            var stats = new TransferStatistics();
            stats.RecordFinished(isUpload: true, succeeded: false, bytesTransferred: 0, startOffset: 500);

            Assert.That(stats.GetSnapshot().UploadedBytes, Is.EqualTo(0));
        }

        [Test]
        public void RestoreThenRecord_AddsToSaved()
        {
            var stats = new TransferStatistics();
            stats.Restore(new TransferStatisticsSnapshot(5, 5000, 2, 2000, 123));
            stats.RecordFinished(isUpload: false, succeeded: true, bytesTransferred: 10, startOffset: 0);

            var s = stats.GetSnapshot();
            Assert.That(s.CompletedDownloads, Is.EqualTo(6));
            Assert.That(s.DownloadedBytes, Is.EqualTo(5010));
            Assert.That(s.CompletedUploads, Is.EqualTo(2));
            Assert.That(s.UploadedBytes, Is.EqualTo(2000));
            Assert.That(s.SinceUtcTicks, Is.EqualTo(123));
        }

        [Test]
        public void Reset_ZeroesAndStampsSince()
        {
            var stats = new TransferStatistics();
            stats.Restore(new TransferStatisticsSnapshot(5, 5000, 2, 2000, 123));
            var now = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);
            stats.Reset(now);

            var s = stats.GetSnapshot();
            Assert.That(s.CompletedDownloads, Is.EqualTo(0));
            Assert.That(s.DownloadedBytes, Is.EqualTo(0));
            Assert.That(s.CompletedUploads, Is.EqualTo(0));
            Assert.That(s.UploadedBytes, Is.EqualTo(0));
            Assert.That(s.SinceUtcTicks, Is.EqualTo(now.Ticks));
        }

        [Test]
        public void CreateSeed_CountsOnlySucceededRows()
        {
            var downloads = new List<TransferItem>
            {
                new TransferItem { Size = 1000, State = TransferStates.Completed | TransferStates.Succeeded },
                new TransferItem { Size = 2000, State = TransferStates.Completed | TransferStates.Succeeded },
                new TransferItem { Size = 5000, State = TransferStates.Completed | TransferStates.Errored },
                new TransferItem { Size = 7000, State = TransferStates.InProgress },
                new TransferItem { Size = -1, State = TransferStates.Completed | TransferStates.Succeeded },
            };
            var uploads = new List<TransferItem>
            {
                new TransferItem { Size = 300, State = TransferStates.Completed | TransferStates.Succeeded },
                new TransferItem { Size = 400, State = TransferStates.Completed | TransferStates.Cancelled },
            };
            var now = new DateTime(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);

            var s = TransferStatistics.CreateSeed(downloads, uploads, now);

            Assert.That(s.CompletedDownloads, Is.EqualTo(3));
            Assert.That(s.DownloadedBytes, Is.EqualTo(3000));
            Assert.That(s.CompletedUploads, Is.EqualTo(1));
            Assert.That(s.UploadedBytes, Is.EqualTo(300));
            Assert.That(s.SinceUtcTicks, Is.EqualTo(now.Ticks));
        }

        [Test]
        public void CreateSeed_EmptyLists()
        {
            var s = TransferStatistics.CreateSeed(new List<TransferItem>(), new List<TransferItem>(), DateTime.UtcNow);

            Assert.That(s.CompletedDownloads + s.CompletedUploads + s.DownloadedBytes + s.UploadedBytes, Is.EqualTo(0));
        }

        [Test]
        public void ConcurrentRecords_LoseNothing()
        {
            var stats = new TransferStatistics();
            Parallel.For(0, 10000, i => stats.RecordFinished(isUpload: i % 2 == 0, succeeded: true, bytesTransferred: 7, startOffset: 0));

            var s = stats.GetSnapshot();
            Assert.That(s.CompletedDownloads + s.CompletedUploads, Is.EqualTo(10000));
            Assert.That(s.DownloadedBytes + s.UploadedBytes, Is.EqualTo(70000));
        }
    }
}
