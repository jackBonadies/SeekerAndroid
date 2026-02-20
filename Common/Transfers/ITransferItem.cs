using System;

namespace Seeker
{
    public interface ITransferItem
    {
        public string GetDisplayName();
        public string GetFolderName();
        public string GetUsername();
        public TimeSpan? GetRemainingTime();
        public double GetAvgSpeed();
        public int GetQueueLength();
        public bool IsUpload();
    }
}