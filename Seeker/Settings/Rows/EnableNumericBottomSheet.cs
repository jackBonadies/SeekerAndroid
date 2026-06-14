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
    /// <summary>
    /// Reusable enable toggle + field
    /// </summary>
    public sealed class EnableNumericBottomSheet : BottomSheetDialogFragment
    {
        public sealed class Result
        {
            public bool Enabled;
            public int Value;
        }

        private string _title;
        private string _toggleLabel;
        private string _suffix;
        private int _min;
        private int _max;
        private Func<(bool enabled, int value)> _getter;
        private Action<Result> _onSubmit;

        public static void Show(ISettingsHost host, ValueRow row,
            int min, int max,
            Func<(bool enabled, int value)> getter,
            Action<Result> setter,
            string toggleLabel = null,
            string suffix = null)
        {
            var ctx = host.Activity;
            var sheet = new EnableNumericBottomSheet
            {
                _title = row.ResolveTitle(ctx),
                _toggleLabel = toggleLabel,
                _suffix = suffix,
                _min = min,
                _max = max,
                _getter = getter,
                _onSubmit = r => { setter(r); host.NotifyRowChanged(row.Id); }
            };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "EnableNumeric");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.bottom_sheet_enable_numeric, container, false);
            // Process death restore: the getter/submit delegates are non-serializable
            if (_getter == null)
            {
                DismissAllowingStateLoss();
                return view;
            }
            view.FindViewById<TextView>(Resource.Id.enableNumericTitle).Text = _title ?? string.Empty;

            var toggleLabel = view.FindViewById<TextView>(Resource.Id.enableNumericToggleLabel);
            if (!string.IsNullOrEmpty(_toggleLabel)) toggleLabel.Text = _toggleLabel;

            var enableSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.enableNumericToggle);
            var inputLayout = view.FindViewById<TextInputLayout>(Resource.Id.enableNumericInputLayout);
            var input = view.FindViewById<TextInputEditText>(Resource.Id.enableNumericInput);

            if (!string.IsNullOrEmpty(_suffix)) inputLayout.SuffixText = _suffix;

            var (enabled, value) = _getter();
            enableSwitch.Checked = enabled;
            input.Text = value.ToString();
            input.Enabled = enabled;
            inputLayout.Enabled = enabled;

            enableSwitch.CheckedChange += (s, e) =>
            {
                input.Enabled = e.IsChecked;
                inputLayout.Enabled = e.IsChecked;
            };

            view.FindViewById<MaterialButton>(Resource.Id.enableNumericCancel).Click += (s, e) => Dismiss();
            view.FindViewById<MaterialButton>(Resource.Id.enableNumericOk).Click += (s, e) =>
            {
                if (!int.TryParse(input.Text, out int v) || v < _min || v > _max)
                {
                    inputLayout.Error = string.Format(Context.GetString(Resource.String.value_must_be_between), _min, _max);
                    return;
                }
                _onSubmit?.Invoke(new Result { Enabled = enableSwitch.Checked, Value = v });
                Dismiss();
            };
            return view;
        }
    }
}
