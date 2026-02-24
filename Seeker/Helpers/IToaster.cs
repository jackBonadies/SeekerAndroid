using Android.Widget;

namespace Seeker
{
    public interface IToaster
    {
        void ShowToast(string msg, ToastLength toastLength);
        void ShowToastDebounced(string msg, string debounceKey, string usernameIfApplicable = "", int seconds = 1);
    }
}
