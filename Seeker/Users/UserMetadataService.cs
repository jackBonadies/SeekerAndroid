using System.Collections.Concurrent;

namespace Seeker
{
    /// <summary>
    /// Per-user metadata: notes, online-alert flags, and the recent-users list.
    /// </summary>
    public static class UserMetadataService
    {
        public static ConcurrentDictionary<string, string> UserNotes = null;

        /// <summary>
        /// No concurrent hashset, so use ConcurrentDictionary with a 1-byte sentinel value.
        /// </summary>
        public static ConcurrentDictionary<string, byte> UserOnlineAlerts = null;

        public static RecentUserManager RecentUsersManager = null;
    }
}
