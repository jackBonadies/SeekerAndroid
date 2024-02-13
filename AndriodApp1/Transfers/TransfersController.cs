﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1.Transfers
{
    public class TransfersController
    {
        private static System.Timers.Timer TransfersTimer = null;
        public static bool IsInitialized = false;
#if DEBUG
        private const int transfersInterval = 2 * 60 * 1000; //2 mins, faster for testing..
#else
        private const int transfersInterval = 5 * 60 * 1000;

#endif

        public static void InitializeService()
        {
            //we run this once after our first login.

            if (IsInitialized)
            {
                return;
            }

            if (SoulSeekState.AutoRequeueDownloadsAtStartup)
            {
                var queuedTransfers = TransfersFragment.TransferItemManagerDL.GetListOfCondition(TransferStates.Queued);
                if (queuedTransfers.Count > 0)
                {
                    MainActivity.LogDebug("TransfersTimerElapsed - Lets redownload and/or get position of queued transfers...");
                    MainActivity.GetDownloadPlaceInQueueBatch(queuedTransfers, true);
                }
            }

            MainActivity.LogDebug("TransfersController InitializeService");
            TransfersTimer = new System.Timers.Timer(transfersInterval);
            TransfersTimer.AutoReset = true;
            TransfersTimer.Elapsed += TransfersTimer_Elapsed;
            TransfersTimer.Start();
            IsInitialized = true;
        }

        private static void TransfersTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MainActivity.LogDebug("TransfersTimerElapsed");
            if (MainActivity.IsNotLoggedIn())
            {
                return;
            }
            var queuedTransfers = TransfersFragment.TransferItemManagerDL.GetListOfCondition(TransferStates.Queued);
            if (queuedTransfers.Count > 0)
            {
                MainActivity.LogDebug("TransfersTimerElapsed - Lets get position of queued transfers...");
                MainActivity.GetDownloadPlaceInQueueBatch(queuedTransfers, false);
            }

        }
    }

}