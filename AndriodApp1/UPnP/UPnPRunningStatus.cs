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

namespace AndriodApp1.UPnP
{
    // TODO ORG UpnpUnums
    public enum UPnPRunningStatus
    {
        NeverStarted = 0,
        CurrentlyRunning = 1,
        Finished = 2,
        AlreadyMapped = 3
    }
}