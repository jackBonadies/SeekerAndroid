using Common;

namespace Seeker
{
    public interface IToaster
    {
        void ShowToastShort(StringKey key);
        void ShowToastLong(StringKey key);
        void ShowToastShort(string msg);
        void ShowToastLong(string msg);
        void ShowToastDebounced(string msg, string debounceKey, string usernameIfApplicable = "", int seconds = 1);
        void ShowToastDebounced(StringKey key, string debounceKey, string usernameIfApplicable = "", int seconds = 1);
        string GetString(StringKey key);
    }
}
