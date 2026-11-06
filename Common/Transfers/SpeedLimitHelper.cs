using System;
using System.Threading.Tasks;
using Common;
using Common.Transfers;
using Seeker.Helpers;
using Soulseek;

namespace Seeker
{
    // Total mode is the library's built in token buckets (maximumDownloadSpeed / maximumUploadSpeed, KiB/s).
    // Per transfer mode is a TransferRateLimiter created for each DownloadAsync / UploadAsync call.
    public static class SpeedLimitHelper
    {
        public static int GetTotalDownloadLimitKiB() =>
            ToTotalLimitKiB(PreferencesState.SpeedLimitDownloadOn, PreferencesState.SpeedLimitDownloadIsPerTransfer, PreferencesState.SpeedLimitDownloadBytesSec);

        public static int GetTotalUploadLimitKiB() =>
            ToTotalLimitKiB(PreferencesState.SpeedLimitUploadOn, PreferencesState.SpeedLimitUploadIsPerTransfer, PreferencesState.SpeedLimitUploadBytesSec);

        public static int ToTotalLimitKiB(bool enabled, bool perTransfer, int bytesPerSec)
        {
            if (!enabled || perTransfer)
            {
                return int.MaxValue;
            }
            return Math.Max(1, bytesPerSec / 1024);
        }

        public static TransferRateLimiter CreateDownloadLimiter() =>
            new TransferRateLimiter(() => PreferencesState.SpeedLimitDownloadOn && PreferencesState.SpeedLimitDownloadIsPerTransfer ? PreferencesState.SpeedLimitDownloadBytesSec : 0);

        public static TransferRateLimiter CreateUploadLimiter() =>
            new TransferRateLimiter(() => PreferencesState.SpeedLimitUploadOn && PreferencesState.SpeedLimitUploadIsPerTransfer ? PreferencesState.SpeedLimitUploadBytesSec : 0);

        public static void ApplyTotalLimits(ISoulseekClient client)
        {
            if (client == null)
            {
                return;
            }
            Task reconfigure;
            try
            {
                reconfigure = client.ReconfigureOptionsAsync(new SoulseekClientOptionsPatch(
                    maximumUploadSpeed: GetTotalUploadLimitKiB(),
                    maximumDownloadSpeed: GetTotalDownloadLimitKiB()));
            }
            catch (Exception e)
            {
                reconfigure = Task.FromException(e);
            }
            reconfigure.ContinueWith(t => Logger.Firebase("applying speed limits failed: " + SimpleHelpers.DescribeException(t.Exception)), TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
