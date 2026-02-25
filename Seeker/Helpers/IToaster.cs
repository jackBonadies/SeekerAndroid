using Android.Widget;
using Common;

namespace Seeker
{
    public interface IToaster
    {
        void ShowToastShort(StringKey key);
        void ShowToastLong(StringKey key);
        void ShowToast(string msg, ToastLength toastLength);
        void ShowToastDebounced(string msg, string debounceKey, string usernameIfApplicable = "", int seconds = 1);
        void ShowToastDebounced(StringKey key, string debounceKey, string usernameIfApplicable = "", int seconds = 1);
    }
}
