using Android.Views;
using AndroidX.Core.View;

namespace Seeker.Helpers
{
    /// <summary>
    /// Applies only the bottom system bar inset as padding, ignoring top/left/right.
    /// Use on fragment roots where the parent activity already handles the top inset.
    /// </summary>
    public class BottomOnlyInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            // adjust for both system bars and IME (keyboard)
            var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            int bottomPadding = System.Math.Max(systemBars.Bottom, ime.Bottom);
            v.SetPadding(v.PaddingLeft, 0, v.PaddingRight, bottomPadding);
            return insets;
        }
    }
}
