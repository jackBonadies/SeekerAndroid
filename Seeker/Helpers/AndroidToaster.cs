using System;
using System.Collections.Concurrent;
using Android.OS;
using Android.Widget;

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
    }
}
