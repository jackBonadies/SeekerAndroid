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

            StopService(new Intent(this, typeof(UploadForegroundService)));
            StopService(new Intent(this, typeof(DownloadForegroundService)));
            StopService(new Intent(this, typeof(SeekerKeepAliveService)));

            //remove this final "closing" activity from task list.
            this.FinishAndRemoveTask();

            //JavaSystem.Exit runs before the looper drains,
            //so pending main-looper work - service callbacks, activity OnDestroy (and the
            //saves they would do) - never executes. must be a synchronous Commit - queued Apply()
            //disk writes die with the process.
            //This fixes the (user reported and reproduced) issue where if you have finished transfers,
            //and then hit Clear All Complete which will mark them dirty but not save them,
            //and then hit Shutdown, they will reappear.
            TransferPersistenceWrapper.SaveTransferItems(force: false, commit: true);

            //actually unload all classes, statics, etc from JVM.
            //the process will still be a "cached background process" that is fine.
            Java.Lang.JavaSystem.Exit(0);
        }
    }

}