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
        // that makes us want to retry immediately, set this event to wake it.
        private readonly AutoResetEvent wakeEvent = new AutoResetEvent(false);
        private volatile bool isRunning = false;

        public bool IsRunning => isRunning;

        /// <summary>
        /// Start a stepped-backoff reconnect attempt on a background thread.
        /// Caller is responsible for deciding whether to start (e.g. AUTO_CONNECT_ON check).
        /// </summary>
        public void Start()
        {
            _ = Task.Run(RunLoop);
        }

        /// <summary>
        /// If a backoff loop is currently sleeping, wake it so it retries immediately.
        /// Returns true if a running loop was signalled; false if no loop was running.
        /// </summary>
        public bool TriggerImmediateRetryIfRunning()
        {
            if (!isRunning)
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
                isRunning = true;
                for (int i = 0; i < retrySeconds.Length; i++)
                {
                    if (!ShouldWeTryToConnect())
                    {
                        return;
                    }

                    bool wokenEarly = wakeEvent.WaitOne(retrySeconds[i] * 1000);
                    if (wokenEarly)
                    {
                        Logger.Debug("is woken due to auto reset");
                    }

                    try
                    {
                        // A general note for connecting:
                        // whenever you reconnect, if you want the server to tell you the status of users on your user list
                        // you have to re-AddUser them. This is what SoulSeekQt does (wireshark message code 5 for each user in list)
                        // and what Nicotine does (userlist.server_login()).
                        // Reconnecting means every single time, including toggling from wifi to data / vice versa.
                        var t = SeekerApplication.ConnectAndPerformPostConnectTasks(
                            PreferencesState.Username, PreferencesState.Password);
                        t.Wait();
                        if (t.IsCompletedSuccessfully)
                        {
                            Logger.Debug("RETRY " + i + " SUCCEEDED");
                            return;
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
                isRunning = false;
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
