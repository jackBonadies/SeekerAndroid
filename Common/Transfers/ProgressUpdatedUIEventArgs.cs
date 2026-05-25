using System;

namespace Seeker
{
    public class ProgressUpdatedUIEventArgs : EventArgs
    {
        public TransferItem TransferItem;
        public bool WasFailed;
        public double PercentComplete;
        public double AverageSpeedBytes;

        public ProgressUpdatedUIEventArgs(TransferItem transferItem, bool wasFailed, double percentComplete, double averageSpeedBytes)
        {
            TransferItem = transferItem;
            WasFailed = wasFailed;
            PercentComplete = percentComplete;
            AverageSpeedBytes = averageSpeedBytes;
        }
    }
}
