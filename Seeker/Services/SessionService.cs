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
    /// Session lifecycle: login state, reconnect, status, and client configuration.
    /// </summary>
    public class SessionService
    {
        public static SessionService Instance { get; set; }

        public bool CurrentlyLoggedInButDisconnectedState()
        {
            return (PreferencesState.CurrentlyLoggedIn &&
                (SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnected) || SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Disconnecting)));
        }

        public bool ShowMessageAndCreateReconnectTask(bool silent, out Task connectTask)
        {
            if (!silent)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.temporary_disconnected), ToastLength.Short);
            }
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

        public bool IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(Action<Task> action, string msg = null, Context contextToUseForMessage = null)
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

        /// <summary>
        /// Standard reconnect-then-act pattern. If disconnected, reconnects and runs action on success.
        /// If already connected, runs action immediately.
        /// </summary>
        /// <returns>true if action was run or will be run after reconnect; false if reconnect could not be started.</returns>
        public bool RunWithReconnect(Action action, bool silent = false)
        {
            if (CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!ShowMessageAndCreateReconnectTask(silent, out t))
                {
                    return false;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        if (!silent)
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        }
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { action(); });
                }));
                return true;
            }
            else
            {
                action();
                return true;
            }
        }

        /// <summary>
        /// Extended reconnect-then-act pattern. Handles disconnected, mid-login, and connected states.
        /// The caller provides a continuation that handles both fault propagation and the real action.
        /// </summary>
        public void RunWithReconnect(Action<Task> continuationAction, string loggingInMsg = null, Context contextForMsg = null)
        {
            if (CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!ShowMessageAndCreateReconnectTask(false, out t))
                    return;
                SeekerApplication.OurCurrentLoginTask = t.ContinueWith(continuationAction);
            }
            else if (IfLoggingInTaskCurrentlyBeingPerformedContinueWithAction(continuationAction, loggingInMsg, contextForMsg))
            {
                // chained onto login task
            }
            else
            {
                continuationAction(Task.CompletedTask);
            }
        }

        public void SetStatusApi(bool away)
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

        public bool IsNotLoggedIn()
        {
            return (!PreferencesState.CurrentlyLoggedIn) || PreferencesState.Username == null || PreferencesState.Password == null || PreferencesState.Username == string.Empty;
        }

        public void ReconfigureOptionsLogic(bool? allowPrivateInvites, bool? enableTheListener, int? listenerPort)
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
