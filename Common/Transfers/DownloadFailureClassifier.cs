using Soulseek;
using System;

namespace Seeker
{
    public enum DownloadFailureKind
    {
        Unknown,
        Duplicate,
        NotLoggedIn,
        DirectoryNotSet,
        SizeMismatch,
        RejectedNotShared,
        Rejected,
        UserOffline,
        TimedOut,
        ReportedFailed,
        CannotConnect,
        DiskFull,
        NetworkDown,
        ConnectionClosed,
    }

    public static class DownloadFailureClassifier
    {
        // the library either rethrows bare or wraps in SoulseekClientException. Transfer.Exception is always bare.
        public static Exception? GetCause(Exception? e)
        {
            while (e is AggregateException agg && agg.InnerException != null)
            {
                e = agg.InnerException;
            }
            if (e == null)
            {
                return null;
            }
            // get wrapped if applicable
            return e.GetType() == typeof(SoulseekClientException) && e.InnerException != null ? e.InnerException : e;
        }

        public static DownloadFailureKind Classify(Exception? e)
        {
            Exception? cause = GetCause(e);
            if (cause == null)
            {
                return DownloadFailureKind.Unknown;
            }

            // types on the cause only - a cannot connect ConnectionException can carry a TimeoutException
            switch (cause)
            {
                case DuplicateTransferException:
                    return DownloadFailureKind.Duplicate;
                case DownloadDirectoryNotSetException:
                    return DownloadFailureKind.DirectoryNotSet;
                case InvalidOperationException when MessageContains(cause, "logged in"):
                    return DownloadFailureKind.NotLoggedIn;
                case TransferSizeMismatchException:
                    return DownloadFailureKind.SizeMismatch;
                case TransferRejectedException:
                    return MessageContains(cause, "file not shared") ? DownloadFailureKind.RejectedNotShared : DownloadFailureKind.Rejected;
                case UserOfflineException:
                    return DownloadFailureKind.UserOffline;
                case TimeoutException:
                    return DownloadFailureKind.TimedOut;
                case TransferReportedFailedException:
                    return DownloadFailureKind.ReportedFailed;
            }

            if (ChainContains(cause, SimpleHelpers.FailedToEstablishDirectOrIndirectString))
            {
                return DownloadFailureKind.CannotConnect;
            }
            if (ChainContains(cause, "No space left on device") || ChainContains(cause, "Disk full"))
            {
                return DownloadFailureKind.DiskFull;
            }
            if (ChainContains(cause, "network subsystem is down"))
            {
                return DownloadFailureKind.NetworkDown;
            }
            if (ChainContains(cause, "remote connection closed"))
            {
                return DownloadFailureKind.ConnectionClosed;
            }
            return DownloadFailureKind.Unknown;
        }

        private static bool MessageContains(Exception e, string value)
        {
            return e.Message?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private static bool ChainContains(Exception e, string value)
        {
            for (Exception? cur = e; cur != null; cur = cur.InnerException)
            {
                if (MessageContains(cur, value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
