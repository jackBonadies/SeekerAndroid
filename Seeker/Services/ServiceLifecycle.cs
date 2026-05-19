namespace Seeker.Services
{
    /// <summary>
    /// Tracks whether each foreground service (download keep-alive, upload keep-alive,
    /// start-up keep-alive) is currently running, plus startup behavior flags.
    /// </summary>
    public static class ServiceLifecycle
    {
        public static volatile bool DownloadKeepAliveServiceRunning = false;
        public static volatile bool UploadKeepAliveServiceRunning = false;
        public static bool IsStartUpServiceCurrentlyRunning = false;
        public static bool AutoRequeueDownloadsAtStartup = true;
    }
}
