using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;

namespace Seeker.Settings.Rows
{
    public sealed class PrivilegesBottomSheet : BottomSheetDialogFragment
    {
        private ISettingsHost _host;

        public static void Show(ISettingsHost host)
        {
            var sheet = new PrivilegesBottomSheet { _host = host };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "Privileges");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.privileges_bottom_sheet, container, false);

            view.FindViewById<ImageButton>(Resource.Id.privileges_close).Click += (s, e) => Dismiss();

            view.FindViewById<Button>(Resource.Id.privileges_donate).Click += (s, e) =>
            {
                Dismiss();
                _host?.GetPrivileges();
            };

            view.FindViewById<Button>(Resource.Id.privileges_check).Click += (s, e) =>
            {
                Dismiss();
                _host?.CheckPrivileges();
            };

            return view;
        }
    }
}
