using System;

namespace Seeker
{
    public class ProgressUpdatedUIEventArgs : EventArgs
    {
        public ProgressUpdatedUIEventArgs(TransferItem _ti, bool _wasFailed, bool _fullRefresh, double _percentComplete, double _avgspeedBytes)
        {
            ti = _ti;
            wasFailed = _wasFailed;
            fullRefresh = _fullRefresh;
            percentComplete = _percentComplete;
            avgspeedBytes = _avgspeedBytes;
        }
        public TransferItem ti;
        public bool wasFailed;
        public bool fullRefresh;
        public double percentComplete;
        public double avgspeedBytes;
    }
}
