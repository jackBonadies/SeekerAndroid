using Android.Content;
using Android.Util;
using Android.Views;
using Google.Android.Material.TextField;
using Seeker.Helpers;

namespace Seeker
{
    public class SafeMaterialAutoCompleteTextView : MaterialAutoCompleteTextView
    {
        public SafeMaterialAutoCompleteTextView(Context context) : base(context)
        {
        }

        public SafeMaterialAutoCompleteTextView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public SafeMaterialAutoCompleteTextView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public override void ShowDropDown()
        {
            if (!this.IsAttachedToWindow || this.WindowToken == null)
            {
                // Host window is gone (dialog dismissed / activity finishing); the posted filter result
                // raced past teardown. Nothing valid to anchor the popup to.
                return;
            }

            try
            {
                base.ShowDropDown();
            }
            catch (WindowManagerBadTokenException)
            {
                Logger.Debug("Swallowed BadTokenException in SafeMaterialAutoCompleteTextView.ShowDropDown");
            }
        }
    }
}
