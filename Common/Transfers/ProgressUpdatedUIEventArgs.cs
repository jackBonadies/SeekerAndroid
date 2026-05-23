using System;

namespace Seeker
{
    public class ProgressUpdatedUIEventArgs : EventArgs
    {
        public ProgressUpdatedUIEventArgs(TransferItem _ti, bool _wasFailed, double _percentComplete, double _avgspeedBytes)
        {
            ti = _ti;
            wasFailed = _wasFailed;
            percentComplete = _percentComplete;
            avgspeedBytes = _avgspeedBytes;
        }
        public TransferItem ti;
        public bool wasFailed;
        public double percentComplete;
        public double avgspeedBytes;
    }
}
