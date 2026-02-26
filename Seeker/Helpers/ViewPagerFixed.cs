using Android.Content;
using Android.Runtime;
using Android.Views;
using AndroidX.ViewPager.Widget;
using System;

namespace Seeker
{
    public class ViewPagerFixed : ViewPager
    {
        /// <summary>
        /// Fixes this:
        ///ava.lang.IllegalArgumentException: pointerIndex out of range
        ///at android.view.MotionEvent.nativeGetAxisValue(Native Method)
        ///at android.view.MotionEvent.getX(MotionEvent.java:1981)
        ///atAndroidX.Core.View.MotionEventCompatEclair.getX(MotionEventCompatEclair.java:32)
        ///atAndroidX.Core.View.MotionEventCompat$EclairMotionEventVersionImpl.getX(MotionEventCompat.java:86)
        ///atAndroidX.Core.View.MotionEventCompat.getX(MotionEventCompat.java:184)
        ///at AndroidX.ViewPager.Widget.ViewPager.onInterceptTouchEvent(ViewPager.java:1339)
        /// </summary>

        public ViewPagerFixed(Context context) : base(context)
        {
        }

        public ViewPagerFixed(Context context, Android.Util.IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ViewPagerFixed(IntPtr intPtr, JniHandleOwnership handle) : base(intPtr, handle)
        {
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            try
            {
                return base.OnTouchEvent(ev);
            }
            catch (Exception)
            {
            }
            return false;
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            try
            {
                return base.OnInterceptTouchEvent(ev);
            }
            catch (Exception)
            {
            }
            return false;
        }
    }
}
