using Android.Content;
using Android.Views;
using Android.Widget;
using Seeker.Helpers;

namespace Seeker
{
    public class MagnetLinkClickableSpan : Android.Text.Style.ClickableSpan
    {
        private string textClicked;
        public MagnetLinkClickableSpan(string _textClicked)
        {
            textClicked = _textClicked;
        }
        public override void OnClick(View widget)
        {
            Logger.Debug("magnet link click");
            try
            {
                Intent followLink = new Intent(Intent.ActionView);
                followLink.SetData(Android.Net.Uri.Parse(textClicked));
                SeekerState.ActiveActivityRef.StartActivity(followLink);
            }
            catch (Android.Content.ActivityNotFoundException e)
            {
                SeekerApplication.Toaster.ShowToast("No Activity Found to handle Magnet Links.  Please Install a BitTorrent Client.", ToastLength.Long);
            }
        }
    }
}
