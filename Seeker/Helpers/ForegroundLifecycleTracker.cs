/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using Seeker.Helpers;
using System;
using System.Threading.Tasks;

using Common;
namespace Seeker
{
    public class ForegroundLifecycleTracker : Java.Lang.Object, Application.IActivityLifecycleCallbacks
    {
        public static bool HasAppEverStarted = false;

        void Application.IActivityLifecycleCallbacks.OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
        }

        void Application.IActivityLifecycleCallbacks.OnActivityDestroyed(Activity activity)
        {
        }

        void Application.IActivityLifecycleCallbacks.OnActivityPaused(Activity activity)
        {
        }

        void Application.IActivityLifecycleCallbacks.OnActivityResumed(Activity activity)
        {
            if (!HasAppEverStarted)
            {
                HasAppEverStarted = true;
                try
                {
                    if (PreferencesState.StartServiceOnStartup)
                    {
                        Intent seekerKeepAliveService = new Intent(activity, typeof(SeekerKeepAliveService));
                        activity.StartService(seekerKeepAliveService);
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        if (activity is AppCompatActivity appCompatActivity)
                        {
                            bool? foreground = appCompatActivity.IsResumed();
                            if (foreground == null)
                            {
                                Logger.Firebase("Unknown seeker keep alive cannot be started: " + e.Message + e.StackTrace);
                            }
                            else if (foreground.Value)
                            {
                                Logger.Firebase("FOREGROUND seeker keep alive cannot be started: " + e.Message + e.StackTrace);
                            }
                            else
                            {
                                Logger.Firebase("BACKGROUND seeker keep alive cannot be started: " + e.Message + e.StackTrace);
                            }
                        }
                        else
                        {
                            Logger.Firebase("seeker keep alive cannot be started: " + e.Message + e.StackTrace);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        void Application.IActivityLifecycleCallbacks.OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        void Application.IActivityLifecycleCallbacks.OnActivityStarted(Activity activity)
        {
            SeekerState.ActiveActivityRef = activity as FragmentActivity;
            if (SeekerState.ActiveActivityRef == null)
            {
                Logger.Firebase("OnActivityStarted activity is null!");
            }
            DiagLastStarted = activity.GetType().Name.ToString();
            Logger.Debug("OnActivityStarted " + DiagLastStarted);

            NumberOfActiveActivities++;
            if (NumberOfActiveActivities == 1)
            {
                Logger.Debug("We are back!");
                if (AutoAwayTimer != null)
                {
                    AutoAwayTimer.Stop();
                }
            }

            if (SeekerState.PendingStatusChangeToAwayOnline == SeekerState.PendingStatusChange.AwayPending)
            {
                SeekerState.PendingStatusChangeToAwayOnline = SeekerState.PendingStatusChange.NothingPending;
            }

            if (SeekerState.OurCurrentStatusIsAway)
            {
                Logger.Debug("Our current status is away, lets set it back to online!");
                MainActivity.SetStatusApi(false);
            }
        }

        private void TryToReconnect()
        {
            try
            {
                Logger.Debug("! TryToReconnect (on app resume) !");

                if (SeekerApplication.ReconnectSteppedBackOffThreadIsRunning)
                {
                    Logger.Debug("In progress, so .Set to let the next one run.");
                    SeekerApplication.ReconnectAutoResetEvent.Set();
                }
                else
                {
                    System.Threading.ThreadPool.QueueUserWorkItem((object o) =>
                    {
                        Task t = SeekerApplication.ConnectAndPerformPostConnectTasks(PreferencesState.Username, PreferencesState.Password);
#if DEBUG
                        t.ContinueWith((Task t) =>
                        {
                            if (t.IsFaulted)
                            {
                                Logger.Debug("TryToReconnect FAILED");
                            }
                            else
                            {
                                Logger.Debug("TryToReconnect SUCCESSFUL");
                            }
                        });
#endif
                    });
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("TryToReconnect Failed " + e.Message + e.StackTrace);
            }
        }

        void Application.IActivityLifecycleCallbacks.OnActivityStopped(Activity activity)
        {
            DiagLastStopped = activity.GetType().Name.ToString();
            Logger.Debug("OnActivityStopped " + DiagLastStopped);

            NumberOfActiveActivities--;
            if (NumberOfActiveActivities == 0 && PreferencesState.AutoAwayOnInactivity)
            {
                Logger.Debug("We are away!");
                if (AutoAwayTimer == null)
                {
                    AutoAwayTimer = new System.Timers.Timer(1000 * 60 * 5);
                    AutoAwayTimer.AutoReset = false;
                    AutoAwayTimer.Elapsed += AutoAwayTimer_Elapsed;
                }
                AutoAwayTimer.Start();
            }
        }

        private void AutoAwayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.Debug("We were away for the interval specified.  time to set status to away.");
            MainActivity.SetStatusApi(true);
        }

        public static bool IsBackground()
        {
            return NumberOfActiveActivities == 0;
        }

        public volatile static string DiagLastStarted = string.Empty;
        public volatile static string DiagLastStopped = string.Empty;

        public static int NumberOfActiveActivities = 0;
        public static System.Timers.Timer AutoAwayTimer = null;
    }
}
