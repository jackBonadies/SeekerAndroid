using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1
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