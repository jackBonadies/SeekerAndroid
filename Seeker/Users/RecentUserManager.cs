using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    /// <summary>
    /// Manages recent users
    /// </summary>
    public class RecentUserManager
    {
        private object recentUserLock = new object();
        private List<string> recentUsers;

        /// <summary>
        /// Called at startup
        /// </summary>
        public void SetRecentUserList(List<string> _recentUsers)
        {
            recentUsers = _recentUsers;
        }

        public List<string> GetRecentUserList()
        {
            lock (recentUserLock)
            {
                return recentUsers.ToList();
            }
        }

        public void AddUserToTop(string user, bool andSave)
        {
            lock (recentUserLock)
            {
                if (recentUsers.Contains(user))
                {
                    recentUsers.Remove(user);
                }
                recentUsers.Insert(0, user);
            }
            if (andSave)
            {
                SeekerApplication.SaveRecentUsers();
            }
        }
    }
}
