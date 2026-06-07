using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;

namespace Seeker.Settings.Rows
{
    public sealed class ForceFileSystemMoreInfoSheet : BottomSheetDialogFragment
    {
        private ISettingsHost _host;

        public static void Show(ISettingsHost host)
        {
            var sheet = new ForceFileSystemMoreInfoSheet { _host = host };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "ForceFileSystemMoreInfo");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.force_filesystem_more_info_sheet, container, false);

            view.FindViewById<Button>(Resource.Id.get_izzydroid_build).Click += (s, e) =>
            {
                Dismiss();
                CommonHelpers.ViewUri(Android.Net.Uri.Parse("https://apt.izzysoft.de/fdroid/index/apk/com.companyname.andriodapp1"), this.Context);
            };

            view.FindViewById<Button>(Resource.Id.get_github_build).Click += (s, e) =>
            {
                Dismiss();
                CommonHelpers.ViewUri(Android.Net.Uri.Parse("https://github.com/jackBonadies/SeekerAndroid/releases"), this.Context);
            };

            return view;
        }
    }
}
