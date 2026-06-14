using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using System;

namespace Seeker.Settings.Rows
{
    public sealed class NumericInputBottomSheet : BottomSheetDialogFragment
    {
        private string _title;
        private string _help;
        private string _suffix;
        private int _min;
        private int _max;
        private int _current;
        private Action<int> _onSubmit;

        public static void Show(ISettingsHost host, ValueRow row, int min, int max,
            Func<int> getter, Action<int> setter, string suffix = null, string help = null)
        {
            var ctx = host.Activity;
            var sheet = new NumericInputBottomSheet
            {
                _title = row.ResolveTitle(ctx),
                _help = help,
                _suffix = suffix,
                _min = min,
                _max = max,
                _current = getter(),
                _onSubmit = v => { setter(v); host.NotifyRowChanged(row.Id); },
            };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "NumericInput");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.bottom_sheet_numeric_input, container, false);
            // Process death restore: the submit delegate is non-serializable
            if (_onSubmit == null)
            {
                DismissAllowingStateLoss();
                return view;
            }
            view.FindViewById<TextView>(Resource.Id.numericTitle).Text = _title ?? string.Empty;
            var help = view.FindViewById<TextView>(Resource.Id.numericHelp);
            if (!string.IsNullOrEmpty(_help)) { help.Text = _help; help.Visibility = ViewStates.Visible; }
            var layout = view.FindViewById<TextInputLayout>(Resource.Id.numericInputLayout);
            layout.SuffixText = _suffix ?? string.Empty;
            var input = view.FindViewById<TextInputEditText>(Resource.Id.numericInput);
            input.Text = _current.ToString();
            input.SetSelection(input.Text.Length);

            view.FindViewById<MaterialButton>(Resource.Id.numericCancel).Click += (s, e) => Dismiss();
            view.FindViewById<MaterialButton>(Resource.Id.numericOk).Click += (s, e) =>
            {
                if (!int.TryParse(input.Text, out int v))
                {
                    layout.Error = Context.GetString(Resource.String.invalid_number);
                    return;
                }
                if (v < _min || v > _max)
                {
                    layout.Error = string.Format(Context.GetString(Resource.String.value_must_be_between), _min, _max);
                    return;
                }
                _onSubmit?.Invoke(v);
                Dismiss();
            };
            return view;
        }
    }
}
