using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Seeker
{
    public static class TransferState
    {
        public static ConcurrentDictionary<string, CancellationTokenSource> CancellationTokens
            = new ConcurrentDictionary<string, CancellationTokenSource>();

        public static Dictionary<string, byte> UsersWhereDownloadFailedDueToOffline
            = new Dictionary<string, byte>();

        public static void SetupCancellationToken(TransferItem transferItem, CancellationTokenSource cts, out CancellationTokenSource oldToken)
        {
            transferItem.CancellationTokenSource = cts;
            if (!CancellationTokens.TryAdd(ProduceCancellationTokenKey(transferItem), cts))
            {
                //likely old already exists so just replace the old one
                oldToken = CancellationTokens[ProduceCancellationTokenKey(transferItem)];
                CancellationTokens[ProduceCancellationTokenKey(transferItem)] = cts;
            }
            else
            {
                oldToken = null;
            }
        }

        public static string ProduceCancellationTokenKey(TransferItem i)
        {
            return ProduceCancellationTokenKey(i.FullFilename, i.Size, i.Username);
        }

        public static string ProduceCancellationTokenKey(string fullFilename, long size, string username)
        {
            return fullFilename + size.ToString() + username;
        }
    }
}
