using Common.Messages;
using Soulseek;

namespace Seeker
{
    public static class UserDataExtensions
    {
        public static UserData WithStatus(this UserData userData, UserPresence status)
        {
            if (userData is ChatroomUserData cud)
            {
                return new ChatroomUserData(cud.Username, status, cud.AverageSpeed, cud.UploadCount, cud.FileCount, cud.DirectoryCount, cud.CountryCode, cud.SlotsFree)
                {
                    ChatroomUserRole = cud.ChatroomUserRole
                };
            }
            return new UserData(userData.Username, status, userData.AverageSpeed, userData.UploadCount, userData.FileCount, userData.DirectoryCount, userData.CountryCode, userData.SlotsFree);
        }
    }
}
