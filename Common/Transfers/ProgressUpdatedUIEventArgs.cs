using System;

namespace Seeker
{
    public class ProgressUpdatedUIEventArgs : EventArgs
    {
        public TransferItem TransferItem;
        public bool WasFailed;
        public double PercentComplete;

        public ProgressUpdatedUIEventArgs(TransferItem transferItem, bool wasFailed, double percentComplete)
        {
            TransferItem = transferItem;
            WasFailed = wasFailed;
            PercentComplete = percentComplete;
        }
    }
}
