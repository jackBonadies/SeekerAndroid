using Soulseek;
using System;
using System.Threading;

namespace Seeker
{
    // in sort order (i.e. Active first)
    public enum TransferBand
    {
        Active = 0,
        Failed = 1,
        Paused = 2,
        Completed = 3,
        // none (we sort these with paused)
        Unset = 4,
    }

    public readonly struct TransferBandKey : IEquatable<TransferBandKey>
    {
        public readonly TransferBand Band;
        public readonly long Stamp;

        public TransferBandKey(TransferBand band, long stamp)
        {
            Band = band;
            Stamp = stamp;
        }

        public bool Equals(TransferBandKey other)
        {
            return Band == other.Band && Stamp == other.Stamp;
        }

        public override bool Equals(object obj)
        {
            return obj is TransferBandKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Band, Stamp);
        }
    }

    public static class TransferBands
    {
        private const TransferStates ActiveStates = TransferStates.InProgress | TransferStates.Initializing
            | TransferStates.Requested | TransferStates.Queued;
        private const TransferStates PeerFailureStates = TransferStates.Rejected | TransferStates.TimedOut
            | TransferStates.UserOffline | TransferStates.CannotConnect;

        private static long lastStamp;

        // follows TransferViewHelper.SetAdditionalStatusText
        public static TransferBand Of(TransferStates state)
        {
            if (state == TransferStates.None)
            {
                return TransferBand.Unset;
            }
            if (state.HasFlag(TransferStates.Succeeded))
            {
                return TransferBand.Completed;
            }
            if ((state & ActiveStates) != 0)
            {
                return TransferBand.Active;
            }
            if (state.HasFlag(TransferStates.Cancelled))
            {
                return TransferBand.Paused;
            }
            if ((state & PeerFailureStates) != 0)
            {
                return TransferBand.Failed;
            }
            if (state.HasFlag(TransferStates.Aborted))
            {
                // re-requesting
                return TransferBand.Active;
            }
            if (state.HasFlag(TransferStates.Errored))
            {
                return TransferBand.Failed;
            }
            return TransferBand.Active;
        }

        public static TransferBandKey KeyOf(ITransferItem item)
        {
            if (item is FolderItem fi)
            {
                TransferBand folderBand = fi.GetBand(out long folderStamp);
                return new TransferBandKey(folderBand, folderStamp);
            }
            var ti = (TransferItem)item;
            TransferBand band = Of(ti.State);
            if (band == TransferBand.Unset)
            {
                band = TransferBand.Paused;
            }
            return new TransferBandKey(band, Interlocked.Read(ref ti.BandStamp));
        }

        // utc ticks, strictly increasing
        public static long NextStamp()
        {
            while (true)
            {
                long last = Interlocked.Read(ref lastStamp);
                long next = Math.Max(DateTime.UtcNow.Ticks, last + 1);
                // i.e. if lastTimestamp has since been changed, then retry
                if (Interlocked.CompareExchange(ref lastStamp, next, last) == last)
                {
                    return next;
                }
            }
        }

        // set a lower bound on timestamps (in the case that a user alters their clock, NTS sync, etc.)
        public static void Observe(long stamp)
        {
            while (true)
            {
                long last = Interlocked.Read(ref lastStamp);
                if (stamp <= last || Interlocked.CompareExchange(ref lastStamp, stamp, last) == last)
                {
                    return;
                }
            }
        }
    }
}
