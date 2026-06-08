using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using System.Collections.Generic;

namespace Seeker.Settings.Rows
{
    public sealed class AboutBottomSheet : BottomSheetDialogFragment
    {
        private const string SourceUrl = "https://github.com/jackBonadies/SeekerAndroid";
        private const string TranslateUrl = "https://crowdin.com/project/seeker";

        private readonly struct AboutLink
        {
            public AboutLink(int iconRes, string url, int titleRes, int subtitleRes = 0)
            {
                IconRes = iconRes;
                TitleRes = titleRes;
                SubtitleRes = subtitleRes;
                Url = url;
            }

            public int IconRes { get; }
            public int TitleRes { get; }
            public int SubtitleRes { get; } = 0;
            public string Url { get; }
        }

        public static void Show(AndroidX.AppCompat.App.AppCompatActivity host)
        {
            var sheet = new AboutBottomSheet();
            sheet.Show(host.SupportFragmentManager, "About");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.about_bottom_sheet, container, false);

            view.FindViewById<TextView>(Resource.Id.about_version).Text = "v" + CommonHelpers.GetVersionString();

            var links = new List<AboutLink>
            {
                new AboutLink(Resource.Drawable.code_tags, SourceUrl, Resource.String.about_link_source_title),
                new AboutLink(Resource.Drawable.web, TranslateUrl, Resource.String.about_link_translate_title),
            };

            var linkContainer = view.FindViewById<LinearLayout>(Resource.Id.about_links);
            foreach (var link in links)
            {
                var row = inflater.Inflate(Resource.Layout.row_about_link, linkContainer, false);
                row.FindViewById<ImageView>(Resource.Id.rowIcon).SetImageResource(link.IconRes);
                row.FindViewById<TextView>(Resource.Id.rowTitle).SetText(link.TitleRes);
                if (link.SubtitleRes != 0)
                {
                    row.FindViewById<TextView>(Resource.Id.rowSubtitle).SetText(link.SubtitleRes);
                    row.FindViewById<TextView>(Resource.Id.rowSubtitle).Visibility = ViewStates.Visible;
                }
                var url = link.Url;
                row.Click += (s, e) => CommonHelpers.ViewUri(Android.Net.Uri.Parse(url), this.Context);
                linkContainer.AddView(row);
            }

            return view;
        }
    }
}
