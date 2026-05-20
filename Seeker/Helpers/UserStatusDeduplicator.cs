using System;
using Soulseek;

namespace Seeker.Helpers
{
    /// <summary>
    /// Filters consecutive duplicate user-status updates from the Soulseek server.
    /// The server sends the same UserStatusChanged event multiple times when we
    /// share multiple interests with a user (added + N chatrooms); this collapses
    /// those into a single emission. Subscribe to <see cref="Deduplicated"/>.
    /// </summary>
    public sealed class UserStatusDeduplicator
    {
        public static UserStatusDeduplicator Instance { get; } = new UserStatusDeduplicator();

        private string lastUsername;
        private UserPresence lastStatus = UserPresence.Offline;

        public event EventHandler<UserStatus> Deduplicated;

        public void OnUserStatusChanged(object sender, UserStatus e)
        {
            if (lastUsername == e.Username && lastStatus == e.Presence)
            {
                Logger.Debug($"throwing away {e.Username} status changed");
                return;
            }
            Logger.Debug($"handling {e.Username} status changed");
            lastUsername = e.Username;
            lastStatus = e.Presence;
            Deduplicated?.Invoke(sender, e);
        }
    }
}
