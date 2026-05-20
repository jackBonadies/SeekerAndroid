using Soulseek;

namespace Seeker
{
    public interface IUserListService
    {
        bool ContainsUser(string username);
        bool SetDoesNotExist(string username);
        bool AddUser(UserData userData, UserPresence? status = null);
        bool RemoveUser(string username);
        bool AddToIgnoreList(string username);
        bool RemoveFromIgnoreList(string username);
        bool IsUserInIgnoreList(string username);
    }
}
