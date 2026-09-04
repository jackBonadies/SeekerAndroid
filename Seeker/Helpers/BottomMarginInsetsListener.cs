using Android.Views;
using AndroidX.Core.View;

namespace Seeker.Helpers
{
    /// <summary>
    /// Adds the bottom system bar inset to the view's bottom margin
    /// Fixes issue where FAB appeared underneath the 3 button nav bar
    /// </summary>
    public class BottomMarginInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly int originalBottomMarginPx;

        public BottomMarginInsetsListener(View view)
        {
            originalBottomMarginPx = (view.LayoutParameters as ViewGroup.MarginLayoutParams)?.BottomMargin ?? 0;
        }

        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
        {
            var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            if (v.LayoutParameters is ViewGroup.MarginLayoutParams marginParams)
            {
                int desiredBottomMargin = originalBottomMarginPx + systemBars.Bottom;
                if (marginParams.BottomMargin != desiredBottomMargin)
                {
                    marginParams.BottomMargin = desiredBottomMargin;
                    v.LayoutParameters = marginParams;
                }
            }
            return insets;
        }
    }
}
