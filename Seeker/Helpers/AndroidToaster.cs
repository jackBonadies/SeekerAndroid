using System;
using System.Collections.Concurrent;
using Android.OS;
using Android.Widget;
using Common;

namespace Seeker
{
    public class AndroidToaster : IToaster
    {
        private readonly ConcurrentDictionary<string, long> debouncer = new ConcurrentDictionary<string, long>();

        public void ShowToast(string msg, ToastLength toastLength)
        {
            new Handler(Looper.MainLooper).Post(() =>
            {
                Toast.MakeText(SeekerApplication.ApplicationContext, msg, toastLength).Show();
            });
        }

        public void ShowToastDebounced(string msg, string debounceKey, string usernameIfApplicable = "", int seconds = 1)
        {
            long curTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            bool stale = false;
            debouncer.AddOrUpdate(debounceKey + usernameIfApplicable, curTime, (key, oldValue) =>
            {
                stale = (curTime - oldValue) < (seconds * 1000);
                return stale ? oldValue : curTime;
            });
            if (!stale)
            {
                ShowToast(msg, ToastLength.Long);
            }
        }

        public void ShowToastDebounced(StringKey key, string debounceKey, string usernameIfApplicable = "", int seconds = 1)
        {
            ShowToastDebounced(getMessage(key), debounceKey, usernameIfApplicable, seconds);
        }

        public void ShowToastLong(StringKey key)
        {
            ShowToast(getMessage(key), ToastLength.Long);
        }

        public void ShowToastShort(StringKey key)
        {
            ShowToast(getMessage(key), ToastLength.Short);
        }

        private String getMessage(StringKey key)
        {
            return SeekerApplication.ApplicationContext.GetString(translateStringKey(key));
        }

        private int translateStringKey(StringKey key) {
            return key switch
            {
                StringKey.cannot_download_from_self => Resource.String.cannot_download_from_self,
                StringKey.error_duplicate => Resource.String.error_duplicate,
                StringKey.QueuedForDownload => Resource.String.QueuedForDownload,
                StringKey.download_is_starting => Resource.String.download_is_starting,
                StringKey.FailedDownloadDirectoryNotSet => Resource.String.FailedDownloadDirectoryNotSet,
            };
        }
    }
}
