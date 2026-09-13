using Common;
using Seeker.Helpers;
using Soulseek;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Seeker.Services
{
    /// <summary>
    /// Owns the stepped-backoff reconnect retry loop. Single background thread at a time.
    /// </summary>
    public class ReconnectService
    {
        public static ReconnectService Instance { get; set; }

        private static readonly int[] retrySeconds = new int[] { 1, 2, 4, 10, 20 };

        // If the reconnect stepped-backoff thread is sleeping but something happens
        // that makes us want to re-evaluate immediately, set this event to wake it.
        private readonly AutoResetEvent wakeEvent = new AutoResetEvent(false);

        private int isRunning = 0;

        public bool IsRunning => Volatile.Read(ref isRunning) == 1;

        /// <summary>
        /// Start a stepped-backoff reconnect attempt on a background thread. Idempotent: if a loop
        /// is already running this only wakes it, so there is never more than one at a time.
        /// Caller is responsible for deciding whether to start (e.g. AUTO_CONNECT_ON check)
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                wakeEvent.Set();
                return;
            }
            _ = Task.Run(RunLoop);
        }

        public void RequestReconnectNow(string reason)
        {
            if (!SeekerApplication.AUTO_CONNECT_ON || !ShouldWeTryToConnect())
            {
                return;
            }
            Logger.Debug("RequestReconnectNow: " + reason);
            Start();
        }

        /// <summary>
        /// If a backoff loop is currently sleeping, wake it so it re-evaluates immediately.
        /// Returns true if a running loop was signalled; false if no loop was running.
        /// </summary>
        public bool TriggerImmediateRetryIfRunning()
        {
            if (!IsRunning)
            {
                return false;
            }
            wakeEvent.Set();
            return true;
        }

        private void RunLoop()
        {
            try
            {
                for (int i = 0; i < retrySeconds.Length; i++)
                {
                    bool wokenEarly = wakeEvent.WaitOne(retrySeconds[i] * 1000);
                    if (wokenEarly)
                    {
                        Logger.Debug("is woken due to auto reset");
                    }

                    // In case things changed while we were sleeping (i.e. already connected, network blocked)
                    if (!ShouldWeTryToConnect())
                    {
                        return;
                    }

                    try
                    {
                        // A general note for connecting:
                        // whenever you reconnect, if you want the server to tell you the status of users on your user list
                        // you have to re-AddUser them. This is what SoulSeekQt does (wireshark message code 5 for each user in list)
                        // and what Nicotine does (userlist.server_login()).
                        // Reconnecting means every single time, including toggling from wifi to data / vice versa.
                        var t = SessionService.BeginLogin(LoginOrigin.Background,
                            PreferencesState.Username, PreferencesState.Password);
                        if (t != null)
                        {
                            t.Wait();
                            if (t.IsCompletedSuccessfully)
                            {
                                Logger.Debug("RETRY " + i + " SUCCEEDED");
                                return;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    Logger.Debug("RETRY " + i + " FAILED");
                }
            }
            finally
            {
                Volatile.Write(ref isRunning, 0);
            }
        }

        private static bool ShouldWeTryToConnect()
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                // we logged out on purpose
                return false;
            }

            if (SeekerState.SoulseekClient == null)
            {
                // too early
                return false;
            }

            if (NetworkStateService.CurrentConnectionIsBlocked)
            {
                Logger.Debug("Not retrying while our network access is blocked");
                return false;
            }

            var state = SeekerState.SoulseekClient.State;
            if (state.HasFlag(SoulseekClientStates.Connected) && state.HasFlag(SoulseekClientStates.LoggedIn))
            {
                // already connected
                return false;
            }
            return true;
        }
    }
}
