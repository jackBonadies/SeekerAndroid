using System;
using System.Threading;

namespace Common.Transfers
{
    public readonly struct TransferStatisticsSnapshot
    {
        public TransferStatisticsSnapshot(long completedDownloads, long downloadedBytes, long completedUploads, long uploadedBytes, long sinceUtcTicks)
        {
            CompletedDownloads = completedDownloads;
            DownloadedBytes = downloadedBytes;
            CompletedUploads = completedUploads;
            UploadedBytes = uploadedBytes;
            SinceUtcTicks = sinceUtcTicks;
        }

        public long CompletedDownloads { get; }
        public long DownloadedBytes { get; }
        public long CompletedUploads { get; }
        public long UploadedBytes { get; }
        public long SinceUtcTicks { get; }
    }

    public class TransferStatistics
    {
        public static readonly TransferStatistics Instance = new TransferStatistics();

        private long completedDownloads;
        private long downloadedBytes;
        private long completedUploads;
        private long uploadedBytes;
        private long sinceUtcTicks;

        public void Restore(TransferStatisticsSnapshot saved)
        {
            Interlocked.Exchange(ref completedDownloads, saved.CompletedDownloads);
            Interlocked.Exchange(ref downloadedBytes, saved.DownloadedBytes);
            Interlocked.Exchange(ref completedUploads, saved.CompletedUploads);
            Interlocked.Exchange(ref uploadedBytes, saved.UploadedBytes);
            Interlocked.Exchange(ref sinceUtcTicks, saved.SinceUtcTicks);
        }

        // always consider startOffset (otherwise a resume can be double counted)
        public void RecordFinished(bool isUpload, bool succeeded, long bytesTransferred, long startOffset)
        {
            long bytes = Math.Max(0, bytesTransferred - startOffset);
            if (isUpload)
            {
                Interlocked.Add(ref uploadedBytes, bytes);
                if (succeeded)
                {
                    Interlocked.Increment(ref completedUploads);
                }
            }
            else
            {
                Interlocked.Add(ref downloadedBytes, bytes);
                if (succeeded)
                {
                    Interlocked.Increment(ref completedDownloads);
                }
            }
        }

        public void Reset(DateTime utcNow)
        {
            Restore(new TransferStatisticsSnapshot(0, 0, 0, 0, utcNow.Ticks));
        }

        public TransferStatisticsSnapshot GetSnapshot()
        {
            return new TransferStatisticsSnapshot(
                Interlocked.Read(ref completedDownloads),
                Interlocked.Read(ref downloadedBytes),
                Interlocked.Read(ref completedUploads),
                Interlocked.Read(ref uploadedBytes),
                Interlocked.Read(ref sinceUtcTicks));
        }
    }
}
