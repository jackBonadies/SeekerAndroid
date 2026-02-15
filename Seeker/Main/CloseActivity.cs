using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.AppCompat.App;
using Seeker.Helpers;

namespace Seeker
{
    [Activity(Label = "CloseActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false)]
    public class CloseActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Logger.InfoFirebase("shutting down");

            //stop all soulseek connection.
            if (SeekerState.SoulseekClient != null)
            {
                //closes server socket, distributed connections, and peer connections. cancels searches, stops listener.
                //this shutdown cleanly closes tcp connections. 
                // - ex. say you are downloading from QT, by closing the tcp stream, the person uploading to you will immediately 
                //       know that you are no longer there and set the status to "Aborted".
                //       compared to just killing service and "swiping up" which will uncleanly close the connection, QT will continue
                //       writing bytes with no one receiving them for several seconds.
                SeekerState.SoulseekClient.Dispose();
                SeekerState.SoulseekClient = null;
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