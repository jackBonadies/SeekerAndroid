using Soulseek;
using System;

namespace Seeker.Transfers
{
    /// <summary>
    /// Android-free presentation policy for the transfers rows.
    ///
    /// Kept narrow and pure on purpose: the percent label and the "is an ETA meaningful"
    /// decision are the two places that used to be re-derived inline, so they can be unit
    /// tested without a device or a running transfer engine. The transfer views still own
    /// the actual colour/token resolution and view mutation.
    /// </summary>
    public static class TransferPresentation
    {
        /// <summary>
        /// Integer percent (0-100) to label a row's progress bar.
        ///
        /// When the caller marks the row terminal it reads as fully complete (the transfer views
        /// pass <c>terminal: true</c> once a row has succeeded, so the label agrees with the full
        /// bar). Otherwise the value is byte-derived and clamped, so a stale/failed partial count
        /// never reads &gt; 100% or &lt; 0%. Failed rows still pass <c>false</c> so the label stays
        /// truthful instead of claiming 100%.
        /// </summary>
        public static int ProgressPercent(long bytesTransferred, long sizeBytes, bool terminal)
        {
            if (terminal)
            {
                return 100;
            }
            if (sizeBytes <= 0)
            {
                return 0;
            }
            long clamped = Math.Max(0, Math.Min(bytesTransferred, sizeBytes));
            return (int)(clamped * 100.0 / sizeBytes);
        }

        public static int ProgressPercent(long bytesTransferred, long sizeBytes)
        {
            return ProgressPercent(bytesTransferred, sizeBytes, terminal: false);
        }

        /// <summary>
        /// Remaining time to show, or null when an ETA would be meaningless.
        ///
        /// The transfer engine already returns null when there is no active speed sample.
        /// This additionally rejects a zero/negative estimate and only exposes a time when the
        /// transfer is actually in progress, so queued/paused/finished rows never show a bogus
        /// countdown.
        /// </summary>
        public static TimeSpan? MeaningfulRemainingTime(TimeSpan? remaining, bool inProgress)
        {
            if (!inProgress || remaining == null)
            {
                return null;
            }
            return remaining.Value > TimeSpan.Zero ? remaining : null;
        }

        public static TimeSpan? MeaningfulRemainingTime(TimeSpan? remaining)
        {
            return MeaningfulRemainingTime(remaining, inProgress: true);
        }

        /// <summary>
        /// Whether a row is actually transferring right now, i.e. whether an ETA describes work in
        /// flight rather than a cached speed sample. Paused (cancelled), queued, requested, finished
        /// and failed rows are all excluded: the engine holds the last speed sample for a short
        /// window, so those rows can still hand back a stale estimate.
        /// </summary>
        public static bool IsEtaStateActive(TransferStates state)
        {
            return state.HasFlag(TransferStates.InProgress);
        }

        /// <summary>
        /// Whether the ETA should be queried at all. The display preference alone is not enough:
        /// a caller must not touch the (potentially expensive / stale) engine estimate unless the
        /// row itself is in progress.
        /// </summary>
        public static bool ShouldQueryEta(bool showTimeRemaining, TransferStates state)
        {
            return showTimeRemaining && IsEtaStateActive(state);
        }

        /// <summary>
        /// Remaining time to show, or null when either the user hid the ETA or the row is not
        /// actually transferring. Callers should gate with <see cref="ShouldQueryEta"/> first so
        /// the engine estimate is not even requested on inactive rows.
        /// </summary>
        public static TimeSpan? MeaningfulRemainingTime(TimeSpan? remaining, bool showTimeRemaining, TransferStates state)
        {
            if (!ShouldQueryEta(showTimeRemaining, state))
            {
                return null;
            }
            return MeaningfulRemainingTime(remaining, inProgress: true);
        }

        public static bool ShouldShowEta(TimeSpan? remaining, bool inProgress)
        {
            return MeaningfulRemainingTime(remaining, inProgress) != null;
        }
    }
}
