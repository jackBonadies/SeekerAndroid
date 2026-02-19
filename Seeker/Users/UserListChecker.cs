using SlskHelp;

namespace Seeker
{
    /// <summary>
    /// for the lower assembly
    /// </summary>
    public class UserListChecker : IUserListChecker
    {
        public bool IsInUserList(string user)
        {
            return UserListService.Instance.ContainsUser(user);
        }
    }
}
