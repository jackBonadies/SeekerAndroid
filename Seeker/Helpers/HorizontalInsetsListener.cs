using Android.Views;
using AndroidX.Core.View;

namespace Seeker.Helpers
{
    public class HorizontalInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            var bars = insets.GetInsets(
                WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());
            v.SetPadding(bars.Left, v.PaddingTop, bars.Right, v.PaddingBottom);
            return insets;
        }
    }
}
