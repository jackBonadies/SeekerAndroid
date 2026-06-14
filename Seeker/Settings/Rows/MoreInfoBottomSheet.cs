using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seeker.Settings.Rows
{
    public sealed class MoreInfoBottomSheet : BottomSheetDialogFragment
    {
        private SettingRow _row;

        public static void Show(ISettingsHost host, SettingRow row)
        {
            var sheet = new MoreInfoBottomSheet { _row = row };
            sheet.Show(((AndroidX.AppCompat.App.AppCompatActivity)host.Activity).SupportFragmentManager, "MoreInfo");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.more_info_bottom_sheet, container, false);
            // Process death restore
            if (_row == null)
            {
                DismissAllowingStateLoss();
                return view;
            }
            view.FindViewById<TextView>(Resource.Id.more_info_title).Text = this._row.ResolveTitle(this.Context);
            view.FindViewById<TextView>(Resource.Id.more_info_body).Text = this.Context.GetString(_row.MoreInfoTextRes.Value);

            return view;
        }
    }
}
