using AndriodApp1.Extensions.SearchResponseExtensions;
using AndriodApp1.Helpers;
using Android;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Provider;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Common;
using Java.IO;
using SlskHelp;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using static Android.Provider.DocumentsContract;
using log = Android.Util.Log;

namespace AndriodApp1
{
    [Activity(Label = "CloseActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false)]
    public class CloseActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            MainActivity.LogInfoFirebase("shutting down");

            //stop all soulseek connection.
            if (SoulSeekState.SoulseekClient != null)
            {
                //closes server socket, distributed connections, and peer connections. cancels searches, stops listener.
                //this shutdown cleanly closes tcp connections. 
                // - ex. say you are downloading from QT, by closing the tcp stream, the person uploading to you will immediately 
                //       know that you are no longer there and set the status to "Aborted".
                //       compared to just killing service and "swiping up" which will uncleanly close the connection, QT will continue
                //       writing bytes with no one receiving them for several seconds.
                SoulSeekState.SoulseekClient.Dispose();
                SoulSeekState.SoulseekClient = null;
            }

            //stop the 3 potential foreground services.
            Intent intent = new Intent(this, typeof(UploadForegroundService));
            intent.SetAction(SeekerApplication.ACTION_SHUTDOWN);
            StartService(intent);

            intent = new Intent(this, typeof(DownloadForegroundService));
            intent.SetAction(SeekerApplication.ACTION_SHUTDOWN);
            StartService(intent);

            intent = new Intent(this, typeof(SeekerKeepAliveService));
            intent.SetAction(SeekerApplication.ACTION_SHUTDOWN);
            StartService(intent);

            //remove this final "closing" activity from task list.
            if ((int)Android.OS.Build.VERSION.SdkInt < 21)
            {
                this.FinishAffinity();
            }
            else
            {
                this.FinishAndRemoveTask();
            }

            //actually unload all classes, statics, etc from JVM.
            //the process will still be a "cached background process" that is fine.
            Java.Lang.JavaSystem.Exit(0);
        }
    }

}