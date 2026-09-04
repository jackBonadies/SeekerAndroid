using Android.App;
using Android.Content;

namespace Seeker.Helpers
{
    public static class BackNavigationHelpers
    {
        /// <summary>
        /// For backing out of an activity that a notification took us straight into.
        /// IsTaskRoot returns false if there is in fact a task behind us (such as the main activity
        /// task), in which case a normal back is what we want.  It is TRUE if Seeker was killed /
        /// swiped from the task list and we then followed a notification - there a normal back just
        /// empties the task and drops the user out of the app entirely.
        /// </summary>
        /// <returns>true if we navigated to the main activity (and finished <paramref name="activity"/>)</returns>
        public static bool GoToMainActivityIfTaskRoot(Activity activity)
        {
            Logger.Debug("IS TASK ROOT: " + activity.IsTaskRoot);
            if (!activity.IsTaskRoot)
            {
                return false;
            }

            Intent intent = new Intent(activity, typeof(MainActivity));
            intent.AddFlags(Android.Content.ActivityFlags.ClearTop);
            activity.StartActivity(intent);
            //without this, pressing back just launches the main activity (this activity will still be
            //behind it) and so you can go back infinitely, it will show this activity behind it, then
            //it will launch main again, then this activity behind it, etc.
            activity.Finish();
            return true;
        }
    }
}
