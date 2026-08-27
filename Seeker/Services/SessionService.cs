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
    /// Whether we triggered login by clicking "login" or if it was done automatically
    ///  this matters bc on clicking login for the first time we do not know if the user has 
    ///  a valid user/pass and so we cant show the logged in screen
    /// </summary>
    public enum LoginOrigin
    {
        Interactive,

        /// <summary>
        /// Cold-start auto-login, or a reconnect after a network drop. The user still considers
        /// themselves logged in, so the account screen stays up with a "Connecting" chip.
        /// </summary>
        Background,
    }

    public class LoginCompletedEventArgs : EventArgs
    {
        public LoginCompletedEventArgs(LoginOrigin origin, bool success)
        {
            Origin = origin;
            Success = success;
        }

        public LoginOrigin Origin { get; }
        public bool Success { get; }
    }

    /// <summary>
    /// Session lifecycle: login state, reconnect, status, and client configuration.
    /// </summary>
    public class SessionService : ISessionService
    {
        public static SessionService Instance { get; set; }

        private static readonly object loginPhaseSyncRoot = new object();
        private static LoginOrigin? inFlightOrigin;
        private static Task inFlightLogin;
        private static string pendingLoginError;

        /// <summary>
        /// Origin of the login currently in flight, or null when none is.
        /// </summary>
        public static LoginOrigin? InFlightLoginOrigin
        {
            get
            {
                lock (loginPhaseSyncRoot)
                {
                    return inFlightOrigin;
                }
            }
        }

        public static event EventHandler<LoginCompletedEventArgs> LoginCompleted;

        /// <summary>
        /// Returns the error from the last login that is worth showing the user, and clears it, so
        /// it is shown once no matter how many times the UI re-renders. Null when there is none.
        /// </summary>
        public static string TakePendingLoginError()
        {
            lock (loginPhaseSyncRoot)
            {
                string error = pendingLoginError;
                pendingLoginError = null;
                return error;
            }
        }

        /// <summary>
        /// The single entry point for logging in. Records the origin, applies the credential and
        /// session state changes when the connect finishes, and raises <see cref="LoginCompleted"/>.
        /// </summary>
        /// <returns>
        /// The connect task, or null when there is nothing to wait on — either the login already
        /// failed (synchronously), or a connect is underway that we have no handle for. Callers that
        /// chain onto the result must handle null.
        /// </returns>
        public static Task BeginLogin(LoginOrigin origin, string username, string password)
        {
            lock (loginPhaseSyncRoot)
            {
                if (inFlightLogin != null && !inFlightLogin.IsCompleted)
                {
                    // Already logging in. An interactive request takes over the presentation: the
                    // user is watching this one, so the spinner and any error belong to them.
                    if (origin == LoginOrigin.Interactive)
                    {
                        inFlightOrigin = LoginOrigin.Interactive;
                    }
                    return inFlightLogin;
                }
                inFlightOrigin = origin;
                inFlightLogin = null;
            }

            Task login;
            try
            {
                login = SeekerApplication.ConnectAndPerformPostConnectTasks(username, password);
            }
            catch (InvalidOperationException)
            {
                login = AdoptConnectInProgress();
            }
            catch (AddressException)
            {
                FailLogin(SeekerApplication.GetString(Resource.String.dns_failed_2), clearCreds: false);
                return null;
            }
            catch (Exception e)
            {
                Logger.Firebase("BeginLogin: " + e.Message + e.StackTrace);
                FailLogin(e.Message, clearCreds: false);
                return null;
            }

            if (login == null)
            {
                return null;
            }

            lock (loginPhaseSyncRoot)
            {
                inFlightLogin = login;
            }
            login.ContinueWith(OnLoginTaskCompleted);
            return login;
        }

        /// <summary>
        /// ConnectAsync refused because a connect is already underway (or done). Attach to that one
        /// instead of leaving the caller with nothing to wait on.
        /// </summary>
        private static Task AdoptConnectInProgress()
        {
            if (SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                // Already connected and logged in — the login we were about to start is done.
                SucceedLogin();
                return null;
            }

            Task adopted = SeekerApplication.OurCurrentLoginTask;
            if (adopted == null)
            {
                // Connecting, but there is no task to attach to: SeekerApplication nulls
                // OurCurrentLoginTask on LoggedIn/Disconnected, and it is assigned only after
                // ConnectAsync returns, so this is reachable. Drop the in-flight state rather than
                // leave the UI on a spinner that can never end.
                Logger.Firebase("BeginLogin: connect underway with no task to adopt");
                FinishLogin(success: false);
                return null;
            }

            SeekerApplication.Toaster.ShowToast(
                SeekerApplication.GetString(Resource.String.we_are_already_logging_in), ToastLength.Short);
            return adopted;
        }

        private static void OnLoginTaskCompleted(Task t)
        {
            ReportDnsFallbackIfNeeded(t);

            if (t.IsFaulted)
            {
                var (msg, clearCreds) = ClassifyLoginError(t);
                FailLogin(msg, clearCreds);
            }
            else
            {
                SucceedLogin();
            }
        }

        private static void SucceedLogin()
        {
            Logger.Debug("Login succeeded");
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                PreferencesState.CurrentlyLoggedIn = true;
                PreferencesManager.SaveCredentials();
            }
            FinishLogin(success: true);
        }

        private static void FailLogin(string msg, bool clearCreds)
        {
            LoginOrigin origin;
            lock (loginPhaseSyncRoot)
            {
                origin = inFlightOrigin ?? LoginOrigin.Background;
            }
            Logger.Debug("Login failed: " + msg);

            // clearCreds means the server rejected the credentials themselves, which no amount of
            // retrying fixes — act on it whoever asked. Everything else (network unreachable,
            // timeouts) is only the user's problem when the user is the one waiting: clearing
            // CurrentlyLoggedIn on a background blip would drop them to the login screen and stop
            // ReconnectService, which checks that same flag before every retry.
            if (clearCreds)
            {
                PreferencesState.ClearCredentials();
                PreferencesManager.SaveCredentials();
            }
            else if (origin == LoginOrigin.Interactive)
            {
                PreferencesState.CurrentlyLoggedIn = false;
                PreferencesManager.SaveCredentials();
            }

            if (clearCreds || origin == LoginOrigin.Interactive)
            {
                lock (loginPhaseSyncRoot)
                {
                    pendingLoginError = msg;
                }
            }

            FinishLogin(success: false);
        }

        private static void FinishLogin(bool success)
        {
            LoginOrigin origin;
            lock (loginPhaseSyncRoot)
            {
                origin = inFlightOrigin ?? LoginOrigin.Background;
                inFlightOrigin = null;
                inFlightLogin = null;
            }
            LoginCompleted?.Invoke(null, new LoginCompletedEventArgs(origin, success));
        }

        private static void ReportDnsFallbackIfNeeded(Task t)
        {
            if (!SeekerApplication.DnsLookupFailed)
            {
                return;
            }
            SeekerApplication.DnsLookupFailed = false;
            if (t.IsFaulted)
            {
                // The login failed anyway; its own error is the useful message.
                return;
            }
            Logger.Firebase("DNS Lookup of Server Failed. Falling back on hardcoded IP succeeded.");
            if (InFlightLoginOrigin == LoginOrigin.Interactive)
            {
                SeekerApplication.Toaster.ShowToast(
                    SeekerApplication.GetString(Resource.String.dns_failed), ToastLength.Long);
            }
        }

        /// <summary>
        /// Maps a faulted connect task onto a user-facing message, and whether the credentials
        /// themselves were rejected (as opposed to the connection failing).
        /// </summary>
        private static (string message, bool clearCredentials) ClassifyLoginError(Task t)
        {
            string msg;
            string msgToLog = string.Empty;
            bool clearCreds = true;

            if (t.Exception != null && t.Exception.InnerExceptions != null && t.Exception.InnerExceptions.Count != 0)
            {
                if (t.Exception.InnerExceptions[0] is LoginRejectedException lre)
                {
                    string loginRejectedMessage = lre.Message;
                    if (loginRejectedMessage != null && loginRejectedMessage.Contains("INVALIDUSERNAME"))
                    {
                        msg = SeekerApplication.GetString(Resource.String.invalid_username);
                    }
                    else if (loginRejectedMessage != null && loginRejectedMessage.Contains("INVALIDPASS"))
                    {
                        msg = SeekerApplication.GetString(Resource.String.invalid_password);
                    }
                    else
                    {
                        msg = SeekerApplication.GetString(Resource.String.bad_user_pass);
                    }
                }
                else if (t.Exception.InnerExceptions[0] is SoulseekClientException)
                {
                    clearCreds = false;
                    if (t.Exception.InnerExceptions[0].Message.Contains("Network is unreachable") ||
                        t.Exception.InnerExceptions[0].Message.Contains("Connection refused"))
                    {
                        msg = SeekerApplication.GetString(Resource.String.network_unreachable);
                    }
                    else
                    {
                        msg = SeekerApplication.GetString(Resource.String.cannot_login);
                        msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;
                    }
                }
                else if (t.Exception.InnerExceptions[0].Message != null &&
                    (t.Exception.InnerExceptions[0].Message.Contains("wait timed out") || t.Exception.InnerExceptions[0].Message.ToLower().Contains("operation timed out")))
                {
                    clearCreds = false;
                    msg = SeekerApplication.GetString(Resource.String.cannot_login) + " - Time Out Waiting for Server Response.";
                }
                else
                {
                    msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;
                    clearCreds = false;
                    msg = SeekerApplication.GetString(Resource.String.cannot_login);
                }
            }
            else
            {
                if (t.Exception != null)
                {
                    msgToLog = t.Exception.Message + t.Exception.StackTrace;
                }
                msg = SeekerApplication.GetString(Resource.String.cannot_login);
            }

            if (msgToLog != string.Empty)
            {
                Logger.Debug(msgToLog);
                Logger.Firebase(msgToLog);
            }

            return (msg, clearCreds);
        }

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
                connectTask = BeginLogin(LoginOrigin.Background, PreferencesState.Username, PreferencesState.Password);
                if (connectTask != null)
                {
                    return true;
                }
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
        /// The continutationAction will always get called
        /// </summary>
        public void RunWithReconnect(Action<Task> continuationAction, string loggingInMsg = null, Context contextForMsg = null)
        {
            if (CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!ShowMessageAndCreateReconnectTask(false, out t))
                {
                    Task.FromException(new Exception("could not start reconnect")).ContinueWith(continuationAction);
                    return;
                }
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
            return (!PreferencesState.CurrentlyLoggedIn) || string.IsNullOrEmpty(PreferencesState.Username) || string.IsNullOrEmpty(PreferencesState.Password);
        }

        public void ReconfigureOptions(bool? allowPrivateInvites, bool? enableListener, int? newPort)
        {
            bool requiresConnection = allowPrivateInvites.HasValue;
            if (!PreferencesState.CurrentlyLoggedIn && requiresConnection)
            {
                SeekerApplication.Toaster.ShowToast(
                    SeekerApplication.GetString(Resource.String.must_be_logged_to_toggle_priv_invites),
                    ToastLength.Short);
                if (SeekerState.ActiveActivityRef is SettingsActivity sa)
                {
                    sa.NotifyRowChanged("general.allow_private_invites");
                }
                return;
            }
            if (requiresConnection)
            {
                RunWithReconnect(() => ReconfigureOptionsLogic(allowPrivateInvites, enableListener, newPort));
            }
            else
            {
                ReconfigureOptionsLogic(allowPrivateInvites, enableListener, newPort);
            }
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
                                // Rebind the toggle row from PreferencesState (the unchanged old value,
                                // since the setter no longer persists optimistically).
                                settingsActivity.NotifyRowChanged("general.allow_private_invites");
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
