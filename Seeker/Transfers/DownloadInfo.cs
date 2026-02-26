using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Seeker
{
    public class DownloadInfo
    {
        public string username;
        public string fullFilename;
        public long Size;
        public int QueueLength;
        public CancellationTokenSource CancellationTokenSource;
        public int RetryCount;
        public Exception PreviousFailureException;
        public Android.Net.Uri IncompleteLocation = null;
        public TransferItem TransferItemReference = null;
        public int Depth = 1;
        /// <summary>
        /// For memory-backed downloads, holds the MemoryStream reference so the
        /// continuation can read the downloaded bytes via ToArray().
        /// </summary>
        public MemoryStream OutputMemoryStream = null;
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount, int depth)
        {
            username = usr; fullFilename = file; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; Depth = depth;
        }
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount, Exception previousFailureException, int depth)
        {
            username = usr; fullFilename = file; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; PreviousFailureException = previousFailureException; Depth = depth;
        }
        public DownloadInfo(string usr, string file, long size, Task task, CancellationTokenSource token, int queueLength, int retryCount, Android.Net.Uri incompleteLocation, int depth)
        {
            username = usr; fullFilename = file; Size = size; CancellationTokenSource = token; QueueLength = queueLength; RetryCount = retryCount; IncompleteLocation = incompleteLocation; Depth = depth;
        }
    }
}
