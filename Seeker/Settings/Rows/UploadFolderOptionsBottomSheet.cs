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
    public sealed class UploadFolderOptionsBottomSheet : BottomSheetDialogFragment
    {
        public sealed class Result
        {
            public bool Locked;
            public bool Hidden;
            public bool OverrideName;
            public string CustomName;
        }

        private string _subtitle;
        private bool _locked;
        private bool _hidden;
        private bool _overrideName;
        private string _customName;
        private Action<Result> _onSubmit;

        public static void Show(ISettingsHost host, string folderName,
            bool locked, bool hidden, bool overrideName, string customName,
            Action<Result> onSubmit)
        {
            var sheet = new UploadFolderOptionsBottomSheet
            {
                _subtitle = folderName,
                _locked = locked,
                _hidden = hidden,
                _overrideName = overrideName,
                _customName = customName,
                _onSubmit = onSubmit,
            };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "UploadFolderOptions");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.bottom_sheet_upload_folder_options, container, false);
            view.FindViewById<TextView>(Resource.Id.ufoSubtitle).Text = _subtitle ?? string.Empty;

            var lockedSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.ufoLocked);
            var hiddenSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.ufoHidden);
            var overrideSwitch = view.FindViewById<MaterialSwitch>(Resource.Id.ufoOverrideName);
            var nameLayout = view.FindViewById<TextInputLayout>(Resource.Id.ufoCustomNameLayout);
            var nameInput = view.FindViewById<TextInputEditText>(Resource.Id.ufoCustomName);

            lockedSwitch.Checked = _locked;
            hiddenSwitch.Checked = _hidden;
            overrideSwitch.Checked = _overrideName;
            nameInput.Text = _customName ?? string.Empty;
            nameLayout.Enabled = _overrideName;
            nameLayout.Alpha = _overrideName ? 1.0f : 0.5f;

            overrideSwitch.CheckedChange += (s, e) =>
            {
                nameLayout.Enabled = e.IsChecked;
                nameLayout.Alpha = e.IsChecked ? 1.0f : 0.5f;
            };

            view.FindViewById<MaterialButton>(Resource.Id.ufoCancel).Click += (s, e) => Dismiss();
            view.FindViewById<MaterialButton>(Resource.Id.ufoOk).Click += (s, e) =>
            {
                _onSubmit?.Invoke(new Result
                {
                    Locked = lockedSwitch.Checked,
                    Hidden = hiddenSwitch.Checked,
                    OverrideName = overrideSwitch.Checked,
                    CustomName = nameInput.Text,
                });
                Dismiss();
            };
            return view;
        }
    }
}
