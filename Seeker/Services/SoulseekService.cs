using Android.Content;
using Android.OS;
using Android.Widget;
using Common;
using Seeker.Helpers;
using Soulseek;
using System;
using System.Threading.Tasks;

namespace Seeker.Services
{
    /// <summary>
    /// SoulseekClient connection state and status operations.
    /// </summary>
    public static class SoulseekService
    {
        public static bool CurrentlyLoggedInButDisconnectedState()
        {
            return (PreferencesState.CurrentlyLoggedIn &&
                (SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnected) || SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnecting)));
        }

        public static bool ShowMessageAndCreateReconnectTask(Context c, bool silent, out Task connectTask)
        {
            if (c == null)
            {
                c = SeekerState.MainActivityRef;
            }
            if (Looper.MainLooper.Thread == Java.Lang.Thread.CurrentThread()) //tested..
            {
                if (!silent)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                }
            }
            else
            {
                if (!silent)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
                }
            }
            //if we are still not connected then creating the task will throw.
            //also if the async part of the task fails we will get task.faulted.
            try
            {
                connectTask = SeekerApplication.ConnectAndPerformPostConnectTasks(PreferencesState.Username, PreferencesState.Password);
                return true;
            }
            catch
            {
                if (!silent)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                }
            }
            connectTask = null;
            return false;
        }

        public static bool IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(Action<Task> action, string msg = null, Context contextToUseForMessage = null)
        {
            lock (SeekerApplication.OurCurrentLoginTaskSyncObject)
            {
                if (!SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || !SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
                {
                    SeekerApplication.OurCurrentLoginTask = SeekerApplication.OurCurrentLoginTask.ContinueWith(action, System.Threading.CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
                    if (msg != null)
                    {
                        if (contextToUseForMessage == null)
                        {
                            SeekerApplication.Toaster.ShowToast(msg, ToastLength.Short);
                        }
                        else
                        {
                            SeekerApplication.Toaster.ShowToast(msg, ToastLength.Short);
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static void SetStatusApi(bool away)
        {
            if (IsNotLoggedIn())
            {
                return;
            }
            if (!SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) || !SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                //dont log in just for this.
                //but if we later connect while still in the background, it may be best to set a flag.
                //do it when we log in... since we could not set it now...
                SeekerState.PendingStatusChangeToAwayOnline = away ? SeekerState.PendingStatusChange.AwayPending : SeekerState.PendingStatusChange.OnlinePending;
                return;
            }
            try
            {
                SeekerState.SoulseekClient.SetStatusAsync(away ? UserPresence.Away : UserPresence.Online).ContinueWith((Task t) =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        SeekerState.PendingStatusChangeToAwayOnline = SeekerState.PendingStatusChange.NothingPending;
                        SeekerState.OurCurrentStatusIsAway = away;
                        string statusString = away ? "away" : "online"; //not user facing
                        Logger.Debug($"We successfully changed our status to {statusString}");
                    }
                    else
                    {
                        Logger.Debug("SetStatusApi FAILED " + t.Exception?.Message);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Debug("SetStatusApi FAILED " + e.Message + e.StackTrace);
            }
        }

        public static bool IsNotLoggedIn()
        {
            return (!PreferencesState.CurrentlyLoggedIn) || PreferencesState.Username == null || PreferencesState.Password == null || PreferencesState.Username == string.Empty;
        }

        public static void ReconfigureOptionsLogic(bool? allowPrivateInvites, bool? enableTheListener, int? listenerPort)
        {
            Task<bool> reconfigTask = null;
            try
            {
                Soulseek.SoulseekClientOptionsPatch patch = new Soulseek.SoulseekClientOptionsPatch(acceptPrivateRoomInvitations: allowPrivateInvites, enableListener: enableTheListener, listenPort: listenerPort);

                reconfigTask = SeekerState.SoulseekClient.ReconfigureOptionsAsync(patch);
            }
            catch (Exception e)
            {
                Logger.Firebase("reconfigure options: " + e.Message + e.StackTrace);
                Logger.Debug("reconfigure options FAILED" + e.Message + e.StackTrace);
                return;
            }
            Action<Task<bool>> continueWithAction = new Action<Task<bool>>((reconfigTask) =>
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                {
                    if (reconfigTask.IsFaulted)
                    {
                        Logger.Debug("reconfigure options FAILED");
                        if (allowPrivateInvites.HasValue)
                        {
                            string enabledDisabled = allowPrivateInvites.Value ? SeekerState.ActiveActivityRef.GetString(Resource.String.allowed) : SeekerState.ActiveActivityRef.GetString(Resource.String.denied);
                            SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.failed_setting_priv_invites), enabledDisabled), ToastLength.Long);
                            if (SeekerState.ActiveActivityRef is SettingsActivity settingsActivity)
                            {
                                //set the check to false
                                settingsActivity.allowPrivateRoomInvitations.Checked = PreferencesState.AllowPrivateRoomInvitations; //old value
                            }
                        }

                        if (enableTheListener.HasValue)
                        {
                            string enabledDisabled = enableTheListener.Value ? SeekerState.ActiveActivityRef.GetString(Resource.String.allowed) : SeekerState.ActiveActivityRef.GetString(Resource.String.denied);
                            SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.network_error_setting_listener), enabledDisabled), ToastLength.Long);
                        }

                        if (listenerPort.HasValue)
                        {
                            SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.network_error_setting_listener_port), listenerPort.Value), ToastLength.Long);
                        }



                    }
                    else
                    {
                        if (allowPrivateInvites.HasValue)
                        {
                            Logger.Debug("reconfigure options SUCCESS, restart required? " + reconfigTask.Result);
                            PreferencesState.AllowPrivateRoomInvitations = allowPrivateInvites.Value;
                            PreferencesManager.SaveAllowPrivateRoomInvitations();
                        }
                    }
                });
            });
            reconfigTask.ContinueWith(continueWithAction);
        }
    }
}
