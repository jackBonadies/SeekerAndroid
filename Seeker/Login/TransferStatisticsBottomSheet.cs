using Android.OS;
using Android.Views;
using Android.Widget;
using Common;
using Common.Transfers;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Dialog;
using System;

namespace Seeker
{
    public sealed class TransferStatisticsBottomSheet : BottomSheetDialogFragment
    {
        private View root;

        public static void Show(AndroidX.Fragment.App.FragmentManager fragmentManager)
        {
            new TransferStatisticsBottomSheet().Show(fragmentManager, "TransferStatistics");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            root = inflater.Inflate(Resource.Layout.transfer_statistics_sheet, container, false);
            root.FindViewById<View>(Resource.Id.statsReset).Click += (s, e) => ConfirmReset();
            Render();
            return root;
        }

        public override void OnDestroyView()
        {
            root = null;
            base.OnDestroyView();
        }

        private void Render()
        {
            if (root == null)
            {
                return;
            }
            var stats = TransferStatistics.Instance.GetSnapshot();
            root.FindViewById<TextView>(Resource.Id.statsSince).Text = string.Format(GetString(Resource.String.transfer_statistics_since), FormatSince(stats.SinceUtcTicks));
            root.FindViewById<TextView>(Resource.Id.statsDownloadFiles).Text = stats.CompletedDownloads.ToString("N0");
            root.FindViewById<TextView>(Resource.Id.statsDownloadSize).Text = SimpleHelpers.GetHumanReadableSize(stats.DownloadedBytes);
            root.FindViewById<TextView>(Resource.Id.statsUploadFiles).Text = stats.CompletedUploads.ToString("N0");
            root.FindViewById<TextView>(Resource.Id.statsUploadSize).Text = SimpleHelpers.GetHumanReadableSize(stats.UploadedBytes);
        }

        // locale specific formatter
        private static string FormatSince(long sinceUtcTicks)
        {
            var utc = new DateTime(sinceUtcTicks, DateTimeKind.Utc);
            long unixMs = (long)(utc - DateTime.UnixEpoch).TotalMilliseconds;
            return Java.Text.DateFormat.GetDateInstance(Java.Text.DateFormat.Medium).Format(new Java.Util.Date(unixMs));
        }

        private void ConfirmReset()
        {
            new MaterialAlertDialogBuilder(RequireContext())
                .SetTitle(Resource.String.reset_statistics_confirm_title)
                .SetMessage(Resource.String.reset_statistics_confirm_message)
                .SetNegativeButton(Resource.String.cancel, (s, e) => { })
                .SetPositiveButton(Resource.String.reset, (s, e) =>
                {
                    PreferencesManager.ResetTransferStatistics();
                    Render();
                })
                .Show();
        }
    }
}
