using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Google.Android.Material.MaterialSwitch;
using Google.Android.Material.TextField;
using System;

namespace Seeker.Settings.Rows
{
    public sealed class SpeedLimitBottomSheet : BottomSheetDialogFragment
    {
        public sealed class Result
        {
            public bool Enabled;
            public int BytesPerSec;
            public bool PerTransfer;
        }

        private string _title;
        private Func<(bool enabled, int bytesPerSec, bool perTransfer)> _getter;
        private Action<Result> _onSubmit;

        public static void Show(ISettingsHost host, ValueRow row,
            Func<(bool enabled, int bytesPerSec, bool perTransfer)> getter,
            Action<Result> setter)
        {
            var ctx = host.Activity;
            var sheet = new SpeedLimitBottomSheet
            {
                _title = row.ResolveTitle(ctx),
                _getter = getter,
                _onSubmit = r => { setter(r); host.NotifyRowChanged(row.Id); }
            };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "SpeedLimit");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.bottom_sheet_speed_limit, container, false);
            view.FindViewById<TextView>(Resource.Id.speedTitle).Text = _title ?? string.Empty;

            var enableSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.speedEnableSwitch);
            var inputLayout = view.FindViewById<TextInputLayout>(Resource.Id.speedInputLayout);
            var input = view.FindViewById<TextInputEditText>(Resource.Id.speedInput);
            var perTransferSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.speedPerTransferSwitch);

            var (enabled, bytesPerSec, perTransfer) = _getter();
            enableSwitch.Checked = enabled;
            input.Text = Math.Max(1, bytesPerSec / 1024).ToString();
            perTransferSwitch.Checked = perTransfer;
            input.Enabled = enabled;
            inputLayout.Enabled = enabled;
            perTransferSwitch.Enabled = enabled;

            enableSwitch.CheckedChange += (s, e) =>
            {
                input.Enabled = e.IsChecked;
                inputLayout.Enabled = e.IsChecked;
                perTransferSwitch.Enabled = e.IsChecked;
            };

            view.FindViewById<MaterialButton>(Resource.Id.speedCancel).Click += (s, e) => Dismiss();
            view.FindViewById<MaterialButton>(Resource.Id.speedOk).Click += (s, e) =>
            {
                if (!int.TryParse(input.Text, out int kbs) || kbs < 1)
                {
                    inputLayout.Error = Context.GetString(Resource.String.invalid_number);
                    return;
                }
                _onSubmit?.Invoke(new Result
                {
                    Enabled = enableSwitch.Checked,
                    BytesPerSec = kbs * 1024,
                    PerTransfer = perTransferSwitch.Checked,
                });
                Dismiss();
            };
            return view;
        }
    }
}
