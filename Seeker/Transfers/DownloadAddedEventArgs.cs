using System;

namespace Seeker
{
    public class DownloadAddedEventArgs : EventArgs
    {
        public DownloadInfo dlInfo;
        public DownloadAddedEventArgs(DownloadInfo downloadInfo)
        {
            dlInfo = downloadInfo;
        }
    }
}
