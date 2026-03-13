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
            ShowToastDebounced(GetString(key), debounceKey, usernameIfApplicable, seconds);
        }

        public void ShowToastLong(StringKey key)
        {
            ShowToast(GetString(key), ToastLength.Long);
        }

        public void ShowToastShort(StringKey key)
        {
            ShowToast(GetString(key), ToastLength.Short);
        }

        public void ShowToastShort(string msg)
        {
            ShowToast(msg, ToastLength.Short);
        }

        public void ShowToastLong(string msg)
        {
            ShowToast(msg, ToastLength.Long);
        }

        public string GetString(StringKey key)
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
                StringKey.UserXIsOffline => Resource.String.UserXIsOffline,
                StringKey.CannotConnectUserX => Resource.String.CannotConnectUserX,
                StringKey.TimeoutQueueUserX => Resource.String.TimeoutQueueUserX,
                StringKey.error_ => Resource.String.error_,
                StringKey.MustBeLoggedInToRetryDL => Resource.String.MustBeLoggedInToRetryDL,
                StringKey.timeout_peer => Resource.String.timeout_peer,
                StringKey.transfer_rejected_file_not_shared => Resource.String.transfer_rejected_file_not_shared,
                StringKey.transfer_rejected => Resource.String.transfer_rejected,
                StringKey.failed_to_establish_connection_to_peer => Resource.String.failed_to_establish_connection_to_peer,
                StringKey.failed_to_establish_direct_or_indirect => Resource.String.failed_to_establish_direct_or_indirect,
                StringKey.remote_conn_closed => Resource.String.remote_conn_closed,
                StringKey.network_down => Resource.String.network_down,
                StringKey.reported_as_failed => Resource.String.reported_as_failed,
                StringKey.error_no_space => Resource.String.error_no_space,
                StringKey.error_unspecified => Resource.String.error_unspecified,
                StringKey.FinishedDownloading => Resource.String.FinishedDownloading,
            };
        }
    }
}
