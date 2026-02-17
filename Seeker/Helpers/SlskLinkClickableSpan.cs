using Android.Views;
using Seeker.Helpers;

namespace Seeker
{
    public class SlskLinkClickableSpan : Android.Text.Style.ClickableSpan
    {
        private string textClicked;
        public SlskLinkClickableSpan(string _textClicked)
        {
            textClicked = _textClicked;
        }
        public override void OnClick(View widget)
        {
            Logger.Debug("slsk link click");
            SimpleHelpers.SlskLinkClickedData = textClicked;
            SimpleHelpers.ShowSlskLinkContextMenu = true;
            SeekerState.ActiveActivityRef.RegisterForContextMenu(widget);
            SeekerState.ActiveActivityRef.OpenContextMenu(widget);
            SeekerState.ActiveActivityRef.UnregisterForContextMenu(widget);
        }
    }
}
